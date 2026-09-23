using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    public sealed class PhoneNotice
    {
        public string Id, Sender, Title, Text, Time;
        public bool Read;
        public Dictionary<string, object> ToJson() => new Dictionary<string, object> {
            { "id", Id }, { "sender", Sender }, { "title", Title }, { "text", Text }, { "time", Time }, { "read", Read } };
        public static PhoneNotice FromJson(Dictionary<string, object> row) => new PhoneNotice {
            Id = Json.String(row, "id"), Sender = Json.String(row, "sender"), Title = Json.String(row, "title"),
            Text = Json.String(row, "text"), Time = Json.String(row, "time"), Read = Json.Bool(row, "read") };
    }

    public sealed class PhoneEntry
    {
        public string Id, Title, Subtitle, Body, Button;
        public Func<string> Action;
        public Action Read;
        public string Quote;
        public Func<List<PhoneEntry>> Children;
        public bool ArrangeHome;
        /// <summary>
        /// A body worked out when the page is drawn rather than when the list was built.
        /// Vehicle ratings are read off a model that has to stream in first, so the page
        /// has to be able to fill in a moment after it opens; every other entry leaves
        /// this null and keeps its fixed <see cref="Body"/>.
        /// </summary>
        public Func<string> LiveBody;
        /// <summary>
        /// A subtitle computed each frame. A list row is the only thing drawn for an entry
        /// that has children, so anything the player needs while choosing belongs here —
        /// and it has to be live, because a model streaming in has no ratings yet and a
        /// subtitle captured once would stay "reading ratings" forever.
        /// </summary>
        public Func<string> LiveSubtitle;
        /// <summary>
        /// A picture at the top of the reading pane - "car-cars", "car-boats" - drawn from
        /// the ui folder by the same sprite path the home screen uses. Null for the plain
        /// page every other entry is.
        /// </summary>
        public string Art;
        /// <summary>
        /// Bars under the picture, label and fill, asked for each frame so a model still
        /// streaming in fills them a moment after the page opens. Null for no bars.
        /// </summary>
        public Func<IList<KeyValuePair<string, float>>> LiveBars;
        /// <summary>What to actually show. Never null, so a missing body is still a sentence.</summary>
        public string Text => LiveBody != null ? LiveBody() : Body;
        /// <summary>What the list row shows under the title.</summary>
        public string SubtitleText => LiveSubtitle != null ? LiveSubtitle() : Subtitle;
    }

    /// <summary>Shared phone/Foundry views. Actions always revalidate the live service at execution.</summary>
    public sealed class CampaignHub
    {
        private readonly CampaignState _state;
        private readonly CrewRoster _crew;
        private readonly MissionCatalog _catalog;
        private readonly MissionManager _missions;
        private readonly GarageService _garages;
        private readonly HashSet<string> _completed;
        private int _nextToast;
        private string _toast;
        public Func<bool> CanCommand;
        public Func<MissionDefinition, string> RouteMission;
        public int Unread => _state.PhoneHistory.Count(n => !n.Read);
        public CampaignHub(CampaignState state, CrewRoster crew, MissionCatalog catalog, MissionManager missions, GarageService garages)
        { _state = state; _crew = crew; _catalog = catalog; _missions = missions; _garages = garages; _completed = new HashSet<string>(state.Completed); }

        public void Log(string sender, string title, string text)
        {
            var notice = new PhoneNotice { Id = Guid.NewGuid().ToString("N"), Sender = sender,
                Title = title, Text = text, Time = World.CurrentDate.ToString("MMM d") + " " + World.CurrentTimeOfDay.ToString(@"hh\:mm") };
            _state.PhoneHistory.Add(notice);
            while (_state.PhoneHistory.Count > 60) _state.PhoneHistory.RemoveAt(0);
            _state.Save(); _toast = notice.Id;
        }
        public void Update(bool notify)
        {
            // Loading an old save never floods the phone with historical completion alerts.
            _completed.RemoveWhere(id => !_state.IsComplete(id));
            foreach (string id in _state.Completed.Where(id => !_completed.Contains(id)).ToArray())
            {
                _completed.Add(id);
                var mission = _catalog.All.FirstOrDefault(m => m.Id == id);
                Log("Crew journal", "Job completed", (mission?.Title ?? id) + ". Review the journal and progression apps for the recorded outcome and rewards.");
                foreach (var message in CampaignDispatches.All.Where(m => m.Mission == id)) Log(message.Sender, message.Title, message.Text);
            }
            var pending = _state.PhoneHistory.FirstOrDefault(n => n.Id == _toast && !n.Read);
            if (pending == null) _toast = null;
            if (notify && pending != null && Game.GameTime >= _nextToast)
            { GameUtils.Notify("~b~Phone~s~ - " + pending.Sender + ": " + pending.Title + "\nD-pad Up / phone key: Alerts"); _toast = null; _nextToast = Game.GameTime + 10000; }
        }
        private bool Free => _crew.IsDeployed && CanCommand?.Invoke() == true && !_missions.IsRunning && !_crew.CompanionAI.MissionActive;
        private bool Flag(string key) => _state.FleetUpgrades.TryGetValue(key, out bool value) && value;
        private string Title(string id) => _catalog.All.FirstOrDefault(m => m.Id == id)?.Title ?? id;
        private string Route(string id)
        {
            var mission = _catalog.All.FirstOrDefault(m => m.Id == id);
            if (_missions.IsRunning || mission == null || !mission.IsPlayable || !_state.PrerequisiteMet(mission) || !_state.GateSatisfied(mission, _catalog))
                return "This lead is not available for routing yet.";
            return RouteMission?.Invoke(mission) ?? "No start location is available.";
        }
        public List<PhoneEntry> Entries(CampaignPhone.App page)
        {
            switch (page)
            {
                case CampaignPhone.App.Garage: return Garage();
                case CampaignPhone.App.Commands: return Commands();
                case CampaignPhone.App.Journal: return Journal();
                case CampaignPhone.App.Progression: return Progression();
                case CampaignPhone.App.Alerts: return Alerts();
                case CampaignPhone.App.Planning: return Planning();
                case CampaignPhone.App.Vehicles: return Vehicles();
                case CampaignPhone.App.Properties: return Properties();
                case CampaignPhone.App.Settings: return Settings();
                case CampaignPhone.App.Crew: return Crew();
                default: return new List<PhoneEntry>();
            }
        }
        private StoryVehicles.Choice[] _roadChoices;
        private List<PhoneEntry> Vehicles()
        {
            if (_roadChoices == null) _roadChoices = StoryVehicles.Catalog.Where(GarageService.RoadModel).ToArray();
            var rows = _roadChoices.Select(v => v.Category).Distinct().Select(category => new PhoneEntry {
                Id = "category:" + category, Title = category, Subtitle = "Browse vehicles / choose a garage",
                Children = () => _roadChoices.Where(v => v.Category == category).OrderBy(VehiclePricing.Of).Select(choice => new PhoneEntry {
                    Id = "model:" + choice.Model, Title = choice.Name,
                    // The subtitle, not the body. A row in a list draws its title and its
                    // subtitle and nothing else: an entry with Children can never show a body,
                    // because selecting it pushes straight into the folder and returns. So the
                    // number a buyer compares cars by has to live here, on the page where he is
                    // actually comparing them.
                    LiveSubtitle = () => "$" + VehiclePricing.Of(choice).ToString("N0") + " - " +
                        VehicleSpecs.Summary(new Model(choice.Model)),
                    Children = () => VehicleDestinations(choice)
                }).ToList()
            }).ToList();
            rows.Add(RecoveryFolder());return rows;
        }
        /// <summary>
        /// The full ratings for one car, shown on its own Performance row.
        ///
        /// This used to be the car entry's LiveBody, which is a place the phone never draws:
        /// an entry with children pushes into its folder and returns, so its body is dead.
        /// The comparison number lives in the row's subtitle instead, and this is what opens
        /// when the player asks for the detail.
        /// </summary>
        private string Showroom(StoryVehicles.Choice choice) =>
            choice.Name + "\n$" + VehiclePricing.Of(choice).ToString("N0") + "\n" + choice.Category +
            "\n\n" + VehicleSpecs.Facts(new Model(choice.Model)) +
            "\n\nChoose a garage to store it in.";

        private PhoneEntry RecoveryFolder() => new PhoneEntry { Id = "recovery", Title = "Recover vehicle", Subtitle = "Return an owned car to its garage", Children = Recoveries };
        private List<PhoneEntry> Recoveries()
        {
            var rows = _state.Vehicles.Select(RecoveryEntry).ToList();
            if (rows.Count == 0) rows.Add(new PhoneEntry { Id = "none", Title = "No owned vehicles", Body = "Purchase or save a vehicle before using recovery." });
            return rows;
        }
        private PhoneEntry RecoveryEntry(OwnedVehicle car)
        {
                int fee = _garages.RecoveryPrice(car);
                bool delivery = _garages.DeliveryActive && _garages.DeliveryCarId == car.Id;
                return new PhoneEntry { Id = "recover:" + car.Id, Title = car.Label,
                    Subtitle = delivery ? "KJ delivery in progress" : fee == 0 ? "Already stored" : "$" + fee.ToString("N0") + " recovery",
                    Body = car.Label + "\n\n" + (delivery ? "Wait for delivery or cancel it in Garage first." : fee == 0 ? "Already stored. Use Garage to request KJ or collect it yourself." :
                        "Recovery: $" + fee.ToString("N0") + "\nReturns to: " + (GarageService.Site(car.Garage)?.Name ?? car.Garage) + "\n\nAn abandoned driveable car costs $500. Wreck recovery includes repair at 10% of purchase price, minimum $500. Everyone must leave the vehicle. Saved upgrades remain. Request KJ separately after recovery."),
                    Button = fee == 0 || delivery ? null : "Recover vehicle", Quote = fee == 0 || delivery ? null : car.Id + ":" + fee + ":" + car.Garage,
                    Action = fee == 0 || delivery ? (Func<string>)null : () => {
                        if (!Free || Game.Player.WantedLevel > 0) return "Recover outside missions after losing the police.";
                        return _garages.Recover(car, fee) ? "Recovered to your garage. Request KJ from Garage when ready." : "Recovery unavailable. Check funds, passengers, delivery and the current quote.";
                    }
                };
        }

        private List<PhoneEntry> VehicleDestinations(StoryVehicles.Choice choice)
        {
            int price = VehiclePricing.Of(choice);
            // Showroom used to hang off the car entry itself, where it could never be drawn.
            // It is a row now: no children and no action, so opening it shows the reading pane
            // with the full ratings, and the garages below it still take the money.
            // The car page draws the class silhouette and the four ratings as bars above the
            // text; the text carries only what a bar cannot, so nothing is said twice.
            var rows = new List<PhoneEntry> { new PhoneEntry {
                Id = "specs:" + choice.Model, Title = "Performance",
                LiveSubtitle = () => VehicleSpecs.Summary(new Model(choice.Model)),
                Art = VehicleSpecs.ArtFor(choice.Category),
                LiveBars = () => VehicleSpecs.Fractions(new Model(choice.Model)),
                LiveBody = () => Showroom(choice) } };
            rows.AddRange(_garages.OwnedSites.Select(site => new PhoneEntry {
                Id = "buy:" + choice.Model + ":" + site.Id, Title = site.Name, Subtitle = _garages.Summary(site),
                Art = VehicleSpecs.ArtFor(choice.Category),
                LiveBars = () => VehicleSpecs.Fractions(new Model(choice.Model)),
                LiveBody = () => choice.Name + "\n$" + price.ToString("N0") + "\n\nStore at: " + site.Name + "\nBays: " + _garages.Used(site) + "/" + site.Capacity +
                    "\nCrew funds: $" + _state.CashOnHand.ToString("N0") +
                    "\n\n" + VehicleSpecs.Facts(new Model(choice.Model)) +
                    "\n\nPurchase stores the car here. Collect it at the garage or request KJ in the Garage app. Delivery does not spawn it beside you.",
                Button = "Purchase vehicle", Quote = choice.Model + ":" + price + ":" + site.Id + ":" + _garages.Used(site) + ":" + _state.NextVehicleId,
                Action = () => {
                    if (!Free || Game.Player.WantedLevel > 0) return "Shop outside missions after losing the police.";
                    if (!_garages.HasFreeSlot(site)) return "That garage is full or unavailable. Choose another destination.";
                    if (_state.CashOnHand < VehiclePricing.Of(choice)) return "Not enough crew funds for this vehicle.";
                    return _garages.BuyFromDealer(choice, site) ? "Purchased. Stored at " + site.Name + ". Open Garage to request KJ." : "Purchase unavailable. No vehicle was ordered.";
                }
            }));
            return rows;
        }
        private List<PhoneEntry> Properties()
        {
            var rows = GarageService.Sites.Where(site => site.Price > 0).Select(site => {
                bool owned = _garages.Owned(site);
                return new PhoneEntry {
                    Id = "property:" + site.Id, Title = site.Name, Subtitle = owned ? "Owned / " + _garages.Summary(site) : "$" + site.Price.ToString("N0") + " / " + site.Capacity + " bays",
                    Body = site.Name + "\n" + site.Capacity + " vehicle bays\n\n" + (owned ? "Owned by the crew." : "Purchase price: $" + site.Price.ToString("N0")) +
                        "\nCrew funds: $" + _state.CashOnHand.ToString("N0") + "\n\nGarage storage property; this purchase does not include a residential interior.",
                    Button = owned ? "Show location" : "Purchase property", Quote = owned ? null : site.Id + ":" + site.Price,
                    Action = () => {
                        if (!Free || Game.Player.WantedLevel > 0) return "Use property services outside missions after losing the police.";
                        if (owned) { var point = _garages.Position(site); if (!point.HasValue) return "No property location is available."; Function.Call(Hash.SET_NEW_WAYPOINT, point.Value.X, point.Value.Y); return "Property location marked."; }
                        if (_state.CashOnHand < site.Price) return "Not enough crew funds for this property.";
                        return _garages.Buy(site) ? "Purchased. This garage can now receive vehicle orders." : "Property is already owned or unavailable.";
                    }
                };
            }).ToList();
            foreach (var hero in Protagonist.All)
                rows.Add(new PhoneEntry { Id = "home:" + hero.Slot, Title = hero.Handle + " / Home", Subtitle = "Story-earned residence",
                    Body = ApartmentTiers.For(hero.Slot, ApartmentTiers.Current(_state)).Name + "\n\n" + ApartmentTiers.Progression(ApartmentTiers.Current(_state)) + "\n\nHomes progress through the story. They are not sold by this garage-property service." });
            return rows;
        }
        private List<PhoneEntry> Garage()
        {
            var rows = new List<PhoneEntry>();
            if (_garages.DeliveryActive)
            {
                rows.Add(new PhoneEntry { Id = "track", Title = "Track KJ", Subtitle = _garages.DeliveryLabel,
                    Body = "KJ is bringing " + _garages.DeliveryLabel + ".\n\nThe delivery marker follows his actual vehicle. Traffic can delay him; no estimated arrival time is invented.", Button = "Track delivery",
                    Action = () => Track(_garages.DeliveryVehicle) });
                rows.Add(new PhoneEntry { Id = "cancel", Title = "Cancel delivery", Subtitle = "Put the delivery car away",
                    Body = "Cancel KJ's current delivery. An occupied car stays in the world. Already completed repairs are not refunded.", Button = "Cancel delivery", Quote = "cancel:" + _garages.DeliveryToken,
                    Action = () => { if (!Free || !_garages.DeliveryActive) return "No cancellable delivery is available outside a mission."; _garages.RecallDelivery(); return "Delivery canceled."; } });
            }
            foreach (var car in _state.Vehicles.ToArray())
            {
                var entry = CarEntry(car);
                if (_garages.IsOut(car) || _garages.DeliveryActive && _garages.DeliveryCarId == car.Id)
                {
                    entry.Subtitle += " / Options";
                    entry.Children = () => CarOptions(car);
                }
                rows.Add(entry);
            }
            if (rows.Count == 0) rows.Add(new PhoneEntry { Id = "empty", Title = "Your garage is empty", Subtitle = "Store a car first",
                Body = "Save an eligible vehicle at an owned garage or from a customization shop. It will then appear here with its saved modifications. Phone deliveries use that exact owned vehicle record." });
            rows.Add(new PhoneEntry { Id = "crew-car", Title = "Crew vehicle", Subtitle = CrewCarName,
                Body = "Current crew vehicle: " + CrewCarName + ".\n\nThis is the shared mission/fleet vehicle. Change it at the crew headquarters fleet service; it is separate from personally stored cars and cannot be duplicated through KJ delivery." });
            // The Osprey, once BM01 has put one in the trees: bought and called in here.
            rows.AddRange(OspreyHangar.Rows(_state, () => Free,
                at => MissionSites.SurfaceHeight(at, at.Z, at.Z - 140f), at => MissionSites.FreeRadius(at, OspreyHangar.Clearance + 2f)));
            rows.Add(RecoveryFolder());
            return rows;
        }
        private PhoneEntry CarEntry(OwnedVehicle car)
        {
                bool delivering = _garages.DeliveryActive && _garages.DeliveryCarId == car.Id;
                string status = delivering ? "KJ is on the way" : _garages.IsOut(car) ? "Out in the city" : car.InShop ? "Repair required" : "Stored";
                var site = GarageService.Site(car.Garage);
                int fee = car.InShop ? Math.Max(500, car.Price / 10) : 0;
                bool isOut = _garages.IsOut(car);
                return new PhoneEntry { Id = "car:" + car.Id, Title = car.Label, Subtitle = status,
                    Body = car.Label + "\n" + (site?.Name ?? "Unknown garage") + "\nOwner: " + (string.IsNullOrEmpty(car.Owner) ? "Crew" : car.Owner) +
                        "\nStatus: " + status + "\n\n" + (delivering || isOut ? "Track this car's current position." : "KJ delivery: " + (fee == 0 ? "free" : "$" + fee.ToString("N0") + " including repair") + ".\nDelivery is available in free roam after losing the police. A new order replaces the current delivery."),
                    Button = delivering ? "Track delivery" : isOut ? "Locate vehicle" : "Request KJ", Quote = delivering || isOut ? null : car.Id + ":" + fee + ":" + car.Garage + ":" + _garages.DeliveryToken,
                    Action = () => { if (delivering) return Track(_garages.DeliveryVehicle); if (isOut) return Track(_garages.OutVehicle(car)); if (!Free) return "Finish the mission or transition before ordering.";
                        return _garages.CallKJ(car) ? "KJ delivery accepted. See Alerts or Track KJ." : "Delivery unavailable. Check the service message."; } };
        }
        private List<PhoneEntry> CarOptions(OwnedVehicle car)
        {
            var main = CarEntry(car); main.Title = main.Button;
            var recovery = RecoveryEntry(car); recovery.Title = "Recover vehicle";
            return new List<PhoneEntry> { main, recovery };
        }

        private string CrewCarName => CrewVan.FleetChoices.FirstOrDefault(c => c.Model == _state.CrewVan.Model)?.Name ?? _state.CrewVan.Model;
        private string Track(Vehicle car)
        {
            if (!Free || car == null || !car.Exists() || car.IsDead) return "No live vehicle can be tracked right now.";
            Function.Call(Hash.SET_NEW_WAYPOINT, car.Position.X, car.Position.Y); return "Vehicle position marked. Its vehicle blip stays live.";
        }
        private List<PhoneEntry> Crew()
        {
            var rows = new List<PhoneEntry>();
            foreach (var hero in Protagonist.All.Where(h => h.Slot != _crew.ActiveSlot))
                rows.Add(new PhoneEntry { Id = "contact:" + hero.Slot, Title = hero.Handle,
                    Subtitle = (_crew.CompanionAI.IsHangingOut(hero.Slot) ? "Hanging out / " : "Independent / ") + (_crew.CompanionAI.RidesAlong(hero.Slot) ? "Ride along" : "Drive alongside"),
                    Children = () => CrewContact(hero) });
            var active = Protagonist.Of(_crew.ActiveSlot);
            rows.Add(new PhoneEntry { Id = "active-profile", Title = active.Handle, Subtitle = "Currently playing",
                Body = active.Handle + "\n" + active.Role + "\n\nAbility: " + active.AbilityName });
            rows.Add(new PhoneEntry { Id = "kj-contact", Title = "KJ", Subtitle = "Racing / vehicle delivery",
                Body = "Guess's friend and racing contact. Open Garage, choose your vehicle and request KJ. An out car has Locate and Recover options; recovery returns it to storage before another delivery." });
            return rows;
        }
        private List<PhoneEntry> CrewContact(Protagonist hero)
        {
                var slot = hero.Slot;
                bool invite = !_crew.CompanionAI.IsHangingOut(slot);
                var hangout = new PhoneEntry {
                    Id = "hangout:" + slot, Title = invite ? "Invite " + hero.Handle : "Let " + hero.Handle + " head out",
                    Subtitle = invite ? "Doing their own thing" : "Hanging out with you",
                    Body = invite ? "Ask " + hero.Handle + " to come hang out. He will travel to you and use the current ride-along preference. Your other brother keeps his own plans.\n\nFree roam only. Missions take priority."
                        : "Let " + hero.Handle + " return to his own plans. Your other brother keeps his current choice. If you share a vehicle, he waits for a safe stop before getting out.\n\nThese choices last for this play session. Sending a group order applies to everyone again.",
                    Button = invite ? "Invite to hang out" : "End hangout",
                    Action = () => {
                        if (!Free || slot == _crew.ActiveSlot || !_crew.CompanionAI.SetHangout(slot, invite))
                            return "Hangout requests are unavailable during missions or transitions.";
                        string message = invite ? hero.Handle + " is making his way to you." : hero.Handle + " will head out when it is safe.";
                        Log(hero.Handle, invite ? "Hangout accepted" : "See you later", message); return message;
                    }
                };
            var rows = new List<PhoneEntry> { hangout };
            foreach (bool ride in new[] { true, false })
            {
                bool choice = ride;
                string title = ride ? "Ride with me" : "Drive alongside me";
                rows.Add(new PhoneEntry { Id = "travel:" + slot + ":" + ride, Title = title,
                    Subtitle = _crew.CompanionAI.RidesAlong(slot) == ride ? "Current travel preference" : "Change only " + hero.Handle,
                    Body = (ride ? "Invite " + hero.Handle + " to share your vehicle when there is a free seat. If it is full, he uses another vehicle."
                        : "Invite " + hero.Handle + " to follow in his own vehicle. If he is currently in your car, he waits for a safe stop before leaving to find another ride.") + "\n\nYour other brother keeps his current travel choice. Free roam only; required mission vehicles take priority.",
                    Button = title, Action = () => {
                        if (!Free || slot == _crew.ActiveSlot || !_crew.CompanionAI.SetTravelChoice(slot, choice)) return "Travel choices are unavailable during missions or transitions.";
                        string message = hero.Handle + (choice ? " will ride with you when there is room." : " will follow in another vehicle.");
                        Log(hero.Handle, "Travel preference", message); return message;
                    }
                });
            }
            rows.Add(new PhoneEntry { Id = "profile:" + slot, Title = "About " + hero.Handle, Subtitle = hero.Role, Body = hero.Handle + "\n" + hero.Role + "\n\nAbility: " + hero.AbilityName });
            return rows;
        }

        private List<PhoneEntry> Settings()
        {
            var rows = new List<PhoneEntry>();
            rows.Add(new PhoneEntry { Id = "layout-help", Title = "Arrange home icons", Subtitle = "A: begin / R: home shortcut", ArrangeHome = true, Button = "Arrange home icons",
                Body = "Press A to begin arranging your home icons. R on keyboard also picks up the selected home icon. Move it with the D-pad / arrows, then A / Enter to save or B / Backspace to cancel.\n\nYour layout is saved for all three brothers." });
            return rows;
        }

        private List<PhoneEntry> Commands()
        {
            var rows = new List<PhoneEntry>();
            string current = _crew.CompanionAI.HasIndividualOrders ? "Individual hangouts" : _crew.CompanionAI.IndependentFreeRoam ? "Independent" : _crew.CompanionAI.RideAlong ? "Travel together" : "Drive alongside";
            string[] names = { "Meet me", "Travel together", "Drive alongside", "Do your own thing" };
            string[] descriptions = { "The brothers travel toward you using the existing companion AI and available transport.", "Share your vehicle when there are seats; use another vehicle when there are not.", "Follow your vehicle using separate transport.", "Resume off-duty activities and independent travel." };
            for (int i = 0; i < names.Length; i++)
            {
                int choice = i;
                rows.Add(new PhoneEntry { Id = "order:" + i, Title = names[i], Subtitle = "Current: " + current,
                    Body = descriptions[i] + "\n\nFree roam only. Mission roles, recovery and apartment transitions take priority. This request does not teleport the crew.", Button = "Send crew request",
                    Action = () => { if (!Free) return "Crew orders are unavailable during missions or transitions.";
                        _crew.CompanionAI.RideAlong = choice != 2; _crew.CompanionAI.IndependentFreeRoam = choice == 3;
                        foreach (var hero in Protagonist.All) _crew.CompanionAI.Refresh(hero.Slot);
                        Log("Crew", names[choice], descriptions[choice]); return "Crew request sent: " + names[choice] + "."; } });
            }
            return rows;
        }
        public List<PhoneEntry> Journal()
        {
            var rows = new List<PhoneEntry>();
            if (_missions.IsRunning && _missions.LastAttempted != null)
            {
                string id = _missions.LastAttempted.Id;
                rows.Add(new PhoneEntry { Id = "current", Title = "Current assignment", Subtitle = _missions.CurrentTitle,
                    Body = _missions.CurrentTitle + "\n\nNOW\n" + _missions.CurrentObjective + "\n\n" + Context(id) });
            }
            var next = _state.NextPlayable(_catalog);
            if (!_missions.IsRunning && next != null) rows.Add(new PhoneEntry { Id = "next", Title = "Next lead", Subtitle = next.Title,
                Body = next.Title + "\n\n" + _state.DescribeProgress(_catalog), Button = "Show start location", Action = () => Route(next.Id) });
            foreach (var mission in _catalog.All.Where(m => _state.IsComplete(m.Id)).OrderByDescending(m => m.IsSolo ? m.Info.InsertAfter + .5f : m.Info.Number))
                rows.Add(new PhoneEntry { Id = mission.Id, Title = mission.Title, Subtitle = mission.Id + " / Completed",
                    Body = mission.Title + "\n\n" + mission.Info.Synopsis + "\n\n" + Context(mission.Id) });
            return rows;
        }
        private static string Context(string id) => MissionContextCard.Cards.TryGetValue(id, out var card) ?
            "WHY\n" + card.Reason + "\n\nROLES\n" + card.Roles + "\n\nTARGET\n" + card.Target : "Follow the current objective and crew dialogue.";

        private sealed class Unlock
        {
            public string Id, Key, Name; public bool Home;
            public Unlock(string id, string key, string name, bool home = false) { Id = id; Key = key; Name = name; Home = home; }
        }
        private static readonly Unlock[] Unlocks = {
            new Unlock("M03","cypressFoundry","Cypress Foundry",true), new Unlock("M11","burroHeightsChopShop","Guess's chop bay",true),
            new Unlock("M14","mckenzieAirfieldHangar","McKenzie hangar",true), new Unlock("M23","grandSenoraRadarBunker","Senora bunker",true),
            new Unlock("M11","grangerTurbineInstalled","Granger turbine"), new Unlock("M17","krakenSubmarineReinforced","Reinforced Kraken"),
            new Unlock("SM01","armorPiercingSupply","Ice's rifle ammo supply"), new Unlock("SM02","surveillanceWormInstalled","Camera archive access"),
            new Unlock("SM03","racingTransmissionInstalled","Racing transmission"), new Unlock("M28","northernRelayDisabled","Northern relay access"),
            new Unlock("M29","bunkerFuelReserves","Bunker fuel"), new Unlock("M30","satellitePartsSecured","Satellite parts"),
            new Unlock("SM04","quarryRadiosRecovered","Quarry radios"), new Unlock("SM05","estuaryTelemetry","Estuary telemetry"), new Unlock("SM06","airfieldFuelReserves","Airfield fuel"),
            new Unlock("M31","bunkerPerimeterReady","Bunker perimeter"), new Unlock("M32","empCasesSecured","EMP cases"),
            new Unlock("M33","ramosRescued","Ramos rescue"), new Unlock("M34","armoredEscortReady","Armored escort"),
            new Unlock("M35","technicalSupportReady","Technical support"), new Unlock("M36","offshoreSurveyReady","Offshore survey"),
            new Unlock("M37","aircraftSmokeReady","Aircraft smoke kits"), new Unlock("M38","seismicStockReady","Demolition stock"),
            new Unlock("M39","rigMainlandCableCut","Mainland cable cut"), new Unlock("M40","extractionLaunchesReady","Extraction launches"),
            new Unlock("M41","bradleyAccessReady","Command access"), new Unlock("M42","subAirdropReady","Submarine insertion"),
            new Unlock("M43","offshoreStagingReady","Offshore staging") };
        public List<PhoneEntry> Progression()
        {
            var rows = new List<PhoneEntry>();
            foreach (var hero in Protagonist.All)
            {
                var owned = new HashSet<uint>(hero.Loadout.Select(w => (uint)w));
                if (_state.Weapons.TryGetValue(hero.Slot.ToString(), out var saved)) owned.UnionWith(saved);
                foreach (var id in WeaponProgression.RewardMissions.Where(id => _state.IsComplete(id) && WeaponProgression.ReceivesReward(id, hero.Slot))) owned.Add((uint)WeaponProgression.Rewards(id)[(int)hero.Slot]);
                rows.Add(new PhoneEntry { Id = "locker:" + hero.Slot, Title = hero.Handle + " / Locker", Subtitle = owned.Count + " owned weapons",
                    Body = string.Join("\n", owned.Select(h => WeaponProgression.NameOf((WeaponHash)h)).OrderBy(n => n)) + "\n\nTemporary mission equipment does not count as a permanent reward." });
            }
            var tier = ApartmentTiers.Current(_state);
            rows.Add(new PhoneEntry { Id = "homes", Title = "Homes", Subtitle = tier.ToString(), Body = string.Join("\n", Protagonist.All.Select(h => h.Handle + ": " + ApartmentTiers.For(h.Slot, tier).Name)) + "\n\n" + ApartmentTiers.Progression(tier) + "\nLater rewards require their missions to have playable scripts." });
            rows.Add(new PhoneEntry { Id = "fleet", Title = "Owned fleet", Subtitle = _state.Vehicles.Count + " vehicles",
                Body = "Crew car: " + CrewCarName + "\nCrew fleet: " + string.Join(", ", CrewVan.FleetChoices.Where(v => v.Model == "granger" || v.Model == _state.CrewVan.Model || _state.CrewVan.Fleet.ContainsKey(v.Model)).Select(v => v.Name)) +
                    "\n\nAvailable garages: " + string.Join(", ", _garages.OwnedSites.Select(g => g.Name)) + "\n\nSaved vehicles:\n" + string.Join("\n", _state.Vehicles.Select(v => v.Label)) + "\n\nBuy vehicles and upgrades at their normal services. This screen does not grant purchases." });
            foreach (var item in Unlocks)
            {
                bool held = _state.IsComplete(item.Id) && !_state.AttemptActive && (item.Home ? _state.IsUnlocked(item.Key) : Flag(item.Key));
                rows.Add(new PhoneEntry { Id = "upgrade:" + item.Key, Title = item.Name, Subtitle = held ? "Available" : "Requires " + item.Id,
                    Body = item.Name + "\n\n" + (held ? "Recorded in your campaign." : _state.IsComplete(item.Id) ? "The supplier job is complete, but this entitlement is no longer held or was not recorded." : "Complete " + item.Id + ": " + Title(item.Id) + ".") });
            }
            foreach (var id in WeaponProgression.RewardMissions.Where(id => !_state.IsComplete(id)))
                rows.Add(new PhoneEntry { Id = "reward:" + id, Title = id + " / Weapon rewards", Subtitle = "Complete the supplier job",
                    Body = "Requires " + id + ".\n\n" + string.Join("\n", Protagonist.All.Where(h => WeaponProgression.ReceivesReward(id, h.Slot)).Select(h => h.Handle + ": " + WeaponProgression.NameOf(WeaponProgression.Rewards(id)[(int)h.Slot]))) + "\n\nEarned weapons appear in the locker. Story locks also apply at shops." });
            rows.Add(new PhoneEntry { Id = "other-flags", Title = "Other recorded upgrades", Subtitle = _state.AttemptActive ? "Provisional during mission" : "Campaign records",
                Body = string.Join("\n", _state.FleetUpgrades.Where(p => p.Value && !Unlocks.Any(u => u.Key == p.Key)).Select(p => System.Text.RegularExpressions.Regex.Replace(p.Key, "([a-z])([A-Z])", "$1 $2"))) });
            return rows;
        }
        private string AlertSnapshot => string.Join(":", _state.PhoneHistory.Select(n => n.Id));
        private List<PhoneEntry> Alerts()
        {
            int count = _state.PhoneHistory.Count;
            string snapshot = AlertSnapshot;
            var rows = new List<PhoneEntry> { new PhoneEntry {
                Id = "clear-alerts", Title = "Clear all alerts", Subtitle = count == 0 ? "No saved alerts" : count + " saved alerts / confirm first",
                Body = count == 0 ? "There are no alerts to clear. New alerts will appear here."
                    : "Clear all " + count + " saved alerts and receipts from this phone? This removes the current list. New alerts will still arrive.",
                Button = count == 0 ? null : "Clear all alerts", Quote = count == 0 ? null : snapshot,
                Action = count == 0 ? (Func<string>)null : () => {
                    if (snapshot != AlertSnapshot) return "New alerts arrived. Review the list before clearing it.";
                    _state.PhoneHistory.Clear(); _toast = null; _state.Save();
                    return "All alerts cleared.";
                }
            } };
            rows.AddRange(_state.PhoneHistory.AsEnumerable().Reverse().Select(n => new PhoneEntry {
                Id = n.Id, Title = n.Title, Subtitle = (n.Read ? "" : "NEW / ") + n.Sender,
                Body = n.Sender + "\n" + n.Time + "\n\n" + n.Text, Read = () => { if (!n.Read) { n.Read = true; _state.Save(); } } }));
            return rows;
        }

        public List<PhoneEntry> Planning()
        {
            var rows = new List<PhoneEntry>();
            bool desert = _state.IsComplete("M22");
            rows.Add(new PhoneEntry { Id = "overview", Title = desert ? "Offshore preparation" : "Harbor preparation", Subtitle = "Recorded campaign inventory",
                Body = "This board reads completed work and saved assets. It does not spawn equipment or certify that a vehicle is physically staged now.\n\n" + (_missions.IsRunning ? "A mission is active. Complete it to commit preparation." : _state.DescribeProgress(_catalog)) + "\n\n" + (desert ? "The Foundry is no longer held. This portable plan continues at the Senora bunker. The operation must still verify the physical staging." : "Review this plan at the Foundry planning table after securing the headquarters.") });
            Action<string, string, bool, string> add = (id, name, held, evidence) => rows.Add(new PhoneEntry {
                Id = "plan:" + name, Title = name, Subtitle = _state.AttemptActive ? "Verify after current job" : held && _state.IsComplete(id) ? "Recorded" : "Missing / " + id,
                Body = name + "\n\nSupplier: " + id + " - " + Title(id) + "\n\n" + (_state.AttemptActive ? "Mission work is provisional until the job ends. " : held && _state.IsComplete(id) ? "Recorded in the committed campaign. " : "Not yet recorded as completed preparation. ") + evidence,
                Button = "Show supplier location", Action = () => Route(id) });
            if (!desert)
            {
                add("M12", "Hull survey", _state.EvidenceOf("hullSurvey") != EvidenceState.None, "Survey evidence is checked.");
                add("M13", "Reduced harbor patrols", Flag("harborPatrolsReduced"), "The alarm/patrol preparation flag is checked.");
                add("M14", "Radar pod", !string.IsNullOrEmpty(_state.CargoAt("radarPod")), "Saved cargo location: " + (_state.CargoAt("radarPod") ?? "none"));
                add("M15", "Harbor gate access", Flag("harborGateAccess"), "The gate-access flag is checked.");
                add("M16", "Cargobob", !string.IsNullOrEmpty(_state.CargoAt("cargobob")), "Saved cargo location: " + (_state.CargoAt("cargobob") ?? "none"));
                add("M17", "Kraken", !string.IsNullOrEmpty(_state.CargoAt("kraken")), "Saved cargo location: " + (_state.CargoAt("kraken") ?? "none"));
                add("M18", "Staging roll call", !string.IsNullOrEmpty(_state.CargoAt("heistClock")), "The roll-call handoff must be committed before the port operation.");
            }
            else
            {
                foreach (var item in Unlocks.Where(u => !u.Home && u.Id.StartsWith("M") && string.CompareOrdinal(u.Id, "M29") >= 0)) add(item.Id, item.Name, Flag(item.Key), "The campaign entitlement is checked.");
                add("M40", "Delivered launches", !string.IsNullOrEmpty(_state.CargoAt("extractionLaunches")), "Actual saved cargo record is required.");
                add("M41", "Bradley credential", _state.EvidenceOf("bradleyKeycard") == EvidenceState.CopyHeld, "The copied credential must still be held.");
                add("M42", "Delivered offshore sub", !string.IsNullOrEmpty(_state.CargoAt("offshoreSub")), "Actual saved cargo record is required.");
            }
            return rows;
        }
    }
}
