using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>A garage the crew can own: a door on the map, a capacity, a price, or the tier or home that brings it for free.</summary>
    public sealed class GarageSite
    {
        public string Id, Name, Key;
        public int Capacity, Price;
        /// <summary>Owned as soon as the crew's residences reach this tier.</summary>
        public ApartmentTier? Tier;
        /// <summary>A starter home's free street bay: reached through that home's menu, no door of its own.</summary>
        public CrewSlot? StarterOf;
    }

    /// <summary>Placeholder prices until Ron sets the scale with the airfields in (September 12).</summary>
    public static class VehiclePricing
    {
        public const int StreetValue = 12000;
        public static int Of(StoryVehicles.Choice choice)
        {
            if (choice == null) return StreetValue;
            switch (choice.Category)
            {
                case "Motorcycles": return 20000;
                case "Off-road": return 35000;
                case "Boats": return 45000;
                case "Helicopters": return 250000;
                case "Planes": return 300000;
                case "Jets": return 400000;
                case "Special": return 350000;
                default: return 40000;
            }
        }
        public static int Sale(OwnedVehicle car) => Math.Max(500, car.Price / 2);
    }

    /// <summary>
    /// The crew's garages, the cars in them, the dealer's sales and KJ's drops.
    /// Garages and cars are the crew's with an optional owner tag; any car driven
    /// into an owned garage is kept, off the street or not; KJ drives a requested
    /// car to whoever called (Ron, September 12). Not a Script: the host ticks it.
    /// </summary>
    public sealed class GarageService
    {
        public const float DoorRadius = 12f;
        public const float ReturnDistance = 300f;
        public const int ReturnAfterMs = 60000;
        public const string KJModel = "a_m_y_stbla_02";

        public static readonly GarageSite[] Sites =
        {
            new GarageSite { Id = "bay-ice", Name = "Little Seoul street bay", Key = "Apartment.Starter.Ice", Capacity = 1, StarterOf = CrewSlot.Ice },
            new GarageSite { Id = "bay-gohan", Name = "Richards Majestic street bay", Key = "Apartment.Starter.Gohan", Capacity = 1, StarterOf = CrewSlot.Gohan },
            new GarageSite { Id = "bay-guess", Name = "Mission Row street bay", Key = "Apartment.Starter.Guess", Capacity = 1, StarterOf = CrewSlot.Guess },
            new GarageSite { Id = "eclipse", Name = "Eclipse Towers garage", Key = "Garage.Eclipse", Capacity = 10, Tier = ApartmentTier.Luxury },
            new GarageSite { Id = "diamond", Name = "The Diamond garage", Key = "Garage.Diamond", Capacity = 10, Tier = ApartmentTier.Top },
            new GarageSite { Id = "mission-row", Name = "Mission Row lot garage", Key = "Garage.MissionRow", Capacity = 2, Price = 18000 },
            new GarageSite { Id = "little-seoul", Name = "Little Seoul lockup", Key = "Garage.LittleSeoul", Capacity = 2, Price = 18000 },
            new GarageSite { Id = "richards", Name = "Richards Majestic underground", Key = "Garage.Richards", Capacity = 2, Price = 22000 },
            new GarageSite { Id = "popular-street", Name = "Popular Street unit, La Mesa", Key = "Garage.PopularStreet", Capacity = 6, Price = 60000 },
            new GarageSite { Id = "rancho", Name = "Roy Lowenstein Blvd, Rancho", Key = "Garage.Rancho", Capacity = 6, Price = 55000 },
            new GarageSite { Id = "del-perro", Name = "South Rockford Drive, Del Perro", Key = "Garage.DelPerro", Capacity = 6, Price = 70000 },
            new GarageSite { Id = "vinewood", Name = "Vinewood Boulevard garage", Key = "Garage.Vinewood", Capacity = 10, Price = 130000 },
            new GarageSite { Id = "pillbox", Name = "Elgin Avenue garage, Pillbox", Key = "Garage.Pillbox", Capacity = 10, Price = 150000 }
        };

        private sealed class Delivery { public OwnedVehicle Car; public Vehicle Vehicle; public Ped KJ; public int Ordered, LastOrder, DoneAt; public Vector3 LastTarget; public bool Arrived; }

        private readonly CrewRoster _crew;
        private readonly CampaignState _state;
        private readonly LocationBook _locations;
        private readonly CrewVan _vans;
        private readonly List<Blip> _blips = new List<Blip>();
        private readonly Dictionary<int, Vehicle> _out = new Dictionary<int, Vehicle>();
        private readonly Dictionary<int, Blip> _outBlips = new Dictionary<int, Blip>();
        private readonly Dictionary<int, int> _awaySince = new Dictionary<int, int>();
        private Delivery _delivery;
        public Func<bool> Allowed;
        public Action<GarageSite> OpenMenu;

        public GarageService(CrewRoster crew, CampaignState state, LocationBook locations, CrewVan vans)
        { _crew = crew; _state = state; _locations = locations; _vans = vans; }

        public bool DeliveryActive => _delivery != null && !_delivery.Arrived;
        public Vehicle DeliveryVehicle => _delivery?.Vehicle;
        public Ped KJ => _delivery?.KJ;

        // ---------- ownership ----------

        public bool Owned(GarageSite site)
        {
            if (site == null) return false;
            if (site.StarterOf.HasValue) return true;
            if (site.Tier.HasValue) return ApartmentTiers.Current(_state) >= site.Tier.Value;
            return _state.Garages.Contains(site.Id);
        }
        public static GarageSite Site(string id) => Sites.FirstOrDefault(s => string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase));
        public IEnumerable<GarageSite> OwnedSites => Sites.Where(Owned);
        public IEnumerable<OwnedVehicle> Parked(GarageSite site) => _state.Vehicles.Where(v => string.Equals(v.Garage, site.Id, StringComparison.OrdinalIgnoreCase));
        public int Used(GarageSite site) => Parked(site).Count();
        public bool HasFreeSlot(GarageSite site) => Owned(site) && Used(site) < site.Capacity;
        public bool IsOut(OwnedVehicle car) => car != null && _out.TryGetValue(car.Id, out var vehicle) && vehicle != null && vehicle.Exists();
        public Vehicle OutVehicle(OwnedVehicle car) => car != null && _out.TryGetValue(car.Id, out var vehicle) && vehicle != null && vehicle.Exists() ? vehicle : null;
        public Vector3? Position(GarageSite site) => site == null ? (Vector3?)null : _locations.Get(site.Key)?.Position;
        public float Heading(GarageSite site) => site == null ? 0f : (_locations.Get(site.Key)?.Heading ?? 0f);
        /// <summary>The home garage for the brother whose home menu is open: his street bay, or the tier's garage once it is his.</summary>
        public GarageSite HomeSite(CrewSlot slot)
        {
            var tier = ApartmentTiers.Current(_state);
            if (tier == ApartmentTier.Top) return Site("diamond");
            if (tier == ApartmentTier.Luxury) return Site("eclipse");
            return Sites.First(s => s.StarterOf == slot);
        }
        public string Summary(GarageSite site) => site == null ? "" : Used(site) + "/" + site.Capacity + (Owned(site) ? "" : " - $" + site.Price.ToString("N0"));

        public bool Buy(GarageSite site)
        {
            if (site == null || Owned(site) || site.Price <= 0) return false;
            if (_state.CashOnHand < site.Price) { GameUtils.Notify("~y~Not enough crew cash for " + site.Name + ": $" + site.Price.ToString("N0") + "."); return false; }
            _state.CashOnHand -= site.Price;
            _state.Garages.Add(site.Id);
            _state.Save();
            GameUtils.Notify("~g~" + site.Name + " is the crew's.~s~ " + site.Capacity + " bays. Crew cash $" + _state.CashOnHand.ToString("N0") + ".");
            Logger.Info("Garage bought: " + site.Id + " for $" + site.Price + ".");
            RefreshBlips();
            return true;
        }

        // ---------- the door ----------

        /// <summary>The car the player arrived in: the one under him, else the nearest within the door's radius. Never the crew van, which keeps itself.</summary>
        public Vehicle ArrivalVehicle()
        {
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return null;
            var current = player.CurrentVehicle;
            if (current != null && current.Exists() && !(_vans?.IsVan(current) ?? false)) return current;
            foreach (var vehicle in World.GetNearbyVehicles(player.Position, DoorRadius))
                if (vehicle != null && vehicle.Exists() && vehicle.IsDriveable && !(_vans?.IsVan(vehicle) ?? false)) return vehicle;
            return null;
        }

        /// <summary>Put a car away: its build read into a record (a new one for a car off the street), the car gone from the world.</summary>
        public bool Store(GarageSite site, Vehicle vehicle)
        {
            if (site == null || vehicle == null || !vehicle.Exists()) return false;
            if (!Owned(site)) { GameUtils.Notify("~y~" + site.Name + " is not the crew's yet."); return false; }
            if (_vans != null && _vans.IsVan(vehicle)) { GameUtils.Notify("~y~The crew van keeps itself; it does not go in a garage."); return false; }
            var record = _out.FirstOrDefault(p => p.Value != null && p.Value.Exists() && p.Value.Handle == vehicle.Handle).Key is int id && id > 0
                ? _state.Vehicles.FirstOrDefault(v => v.Id == id) : null;
            if (record == null && Used(site) >= site.Capacity) { GameUtils.Notify("~y~" + site.Name + " is full (" + site.Capacity + " bays)."); return false; }
            if (record != null && !string.Equals(record.Garage, site.Id, StringComparison.OrdinalIgnoreCase) && Used(site) >= site.Capacity)
            { GameUtils.Notify("~y~" + site.Name + " is full (" + site.Capacity + " bays)."); return false; }
            if (record == null)
            {
                record = new OwnedVehicle { Id = _state.NextVehicleId++, ModelHash = unchecked((uint)vehicle.Model.Hash), Label = SafeLabel(vehicle), Stolen = true, Price = VehiclePricing.StreetValue };
                _state.Vehicles.Add(record);
            }
            Capture(vehicle, record);
            record.Garage = site.Id;
            record.InShop = false;
            var player = Game.Player.Character;
            if (player != null && player.Exists() && player.IsInVehicle(vehicle)) ExitVehicleStep.ForceOut(player);
            Forget(record.Id);
            GameUtils.SafeDelete(vehicle);
            _state.Save();
            GameUtils.Notify("~g~" + record.Label + " is in " + site.Name + ".~s~ " + Used(site) + "/" + site.Capacity + " bays used.");
            Logger.Info("Garage: stored vehicle " + record.Id + " (" + record.Label + ") in " + site.Id + ".");
            return true;
        }

        /// <summary>Bring a car out of its garage onto the forecourt, its build on it.</summary>
        public Vehicle Retrieve(OwnedVehicle car)
        {
            if (car == null) return null;
            var existing = OutVehicle(car);
            if (existing != null) { GameUtils.Notify("~y~" + car.Label + " is already out."); return existing; }
            if (car.InShop) { GameUtils.Notify("~y~" + car.Label + " is in the shop. Ask KJ to fetch it."); return null; }
            var site = Site(car.Garage);
            var door = Position(site);
            if (site == null || !door.HasValue) { GameUtils.Notify("~y~" + car.Label + "'s garage is missing from the map."); return null; }
            float heading = Heading(site);
            var forward = new Vector3(-(float)Math.Sin(heading * Math.PI / 180f), (float)Math.Cos(heading * Math.PI / 180f), 0f);
            var wanted = door.Value + forward * 7f;
            var spot = World.GetNextPositionOnStreet(wanted);
            if (spot == Vector3.Zero || spot.DistanceTo(wanted) > 25f) spot = wanted;
            var vehicle = Create(car, spot, heading);
            if (vehicle == null) { GameUtils.Notify("~y~" + car.Label + " could not be brought out (model not loaded)."); return null; }
            TakeOut(car, vehicle, "Garage: " + car.Label);
            GameUtils.Notify("~g~" + car.Label + " is out front of " + site.Name + ".");
            return vehicle;
        }

        public bool Sell(OwnedVehicle car)
        {
            if (car == null || !_state.Vehicles.Contains(car)) return false;
            var live = OutVehicle(car);
            if (live != null) GameUtils.SafeDelete(live);
            Forget(car.Id);
            int paid = VehiclePricing.Sale(car);
            _state.Vehicles.Remove(car);
            _state.CashOnHand += paid;
            _state.Save();
            GameUtils.Notify("~g~" + car.Label + " sold for $" + paid.ToString("N0") + ".~s~ Crew cash $" + _state.CashOnHand.ToString("N0") + ".");
            return true;
        }

        public void Tag(OwnedVehicle car, CrewSlot? owner)
        {
            if (car == null) return;
            car.Owner = owner.HasValue ? owner.Value.ToString() : "";
            _state.Save();
        }

        // ---------- the dealer ----------

        public bool BuyFromDealer(StoryVehicles.Choice choice, GarageSite site)
        {
            if (choice == null || site == null) return false;
            int price = VehiclePricing.Of(choice);
            if (!Owned(site)) { GameUtils.Notify("~y~" + site.Name + " is not the crew's."); return false; }
            if (Used(site) >= site.Capacity) { GameUtils.Notify("~y~" + site.Name + " is full; pick another garage."); return false; }
            if (_state.CashOnHand < price) { GameUtils.Notify("~y~Not enough crew cash: " + choice.Name + " is $" + price.ToString("N0") + "."); return false; }
            var record = new OwnedVehicle
            {
                Id = _state.NextVehicleId++, ModelName = choice.Model, ModelHash = unchecked((uint)Game.GenerateHash(choice.Model)),
                Label = choice.Name, Garage = site.Id, Price = price
            };
            _state.CashOnHand -= price;
            _state.Vehicles.Add(record);
            _state.Save();
            GameUtils.Notify("~g~" + choice.Name + " bought for $" + price.ToString("N0") + ".~s~ Delivered to " + site.Name + "; call KJ or pick it up there. Crew cash $" + _state.CashOnHand.ToString("N0") + ".");
            Logger.Info("Dealer: bought " + choice.Model + " into " + site.Id + " for $" + price + ".");
            return true;
        }

        // ---------- KJ ----------

        /// <summary>KJ brings a car from its garage to whoever called: he starts well out on the road and drives it in.</summary>
        public bool CallKJ(OwnedVehicle car)
        {
            if (car == null) return false;
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return false;
            if (_delivery != null && !_delivery.Arrived && _delivery.Car == car) { GameUtils.Notify("~y~KJ is already on his way with the " + car.Label + "."); return false; }
            if (_delivery != null) RecallDelivery();
            var already = OutVehicle(car);
            if (already != null) { GameUtils.Notify("~y~" + car.Label + " is already out; it is marked on the map."); return false; }
            if (car.InShop)
            {
                int fee = Math.Max(500, car.Price / 10);
                if (_state.CashOnHand < fee) { GameUtils.Notify("~y~" + car.Label + " is in the shop: $" + fee.ToString("N0") + " to bring it back."); return false; }
                _state.CashOnHand -= fee; car.InShop = false; _state.Save();
                GameUtils.Notify("~y~KJ: shop bill for the " + car.Label + " paid, $" + fee.ToString("N0") + ".");
            }
            var spawn = KJStart(player);
            var vehicle = Create(car, spawn, DriveUpStep.HeadingBetween(spawn, player.Position));
            if (vehicle == null) { GameUtils.Notify("~y~KJ could not get the " + car.Label + " started (model not loaded)."); return false; }
            var model = new Model(KJModel);
            Ped kj = null;
            if (GameUtils.RequestModel(model)) { kj = World.CreatePed(model, spawn, 0f); model.MarkAsNoLongerNeeded(); }
            if (kj == null || !kj.Exists())
            {
                // No KJ: the car is left at the roadside and marked; better than no car.
                TakeOut(car, vehicle, "KJ's drop: " + car.Label);
                GameUtils.Notify("~y~KJ could not come; the " + car.Label + " is at the roadside, marked on the map.");
                return true;
            }
            kj.IsPersistent = true;
            kj.BlockPermanentEvents = true;
            kj.RelationshipGroup = _crew.CrewGroup;
            kj.SetIntoVehicle(vehicle, VehicleSeat.Driver);
            vehicle.IsEngineRunning = true;
            Function.Call(Hash.SET_DRIVER_ABILITY, kj, 1f);
            Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, kj, 0.3f);
            _delivery = new Delivery { Car = car, Vehicle = vehicle, KJ = kj, Ordered = Game.GameTime, LastOrder = Game.GameTime, LastTarget = player.Position };
            kj.Task.DriveTo(vehicle, player.Position, 8f, 20f, DrivingStyle.Normal);
            var blip = vehicle.AddBlip();
            if (blip != null) { blip.Sprite = BlipSprite.PersonalVehicleCar; blip.Color = BlipColor.Yellow; blip.Name = "KJ: " + car.Label; _outBlips[car.Id] = blip; }
            GameUtils.Subtitle("~b~KJ:~s~ On my way with the " + car.Label + ". Stay where I can find you.", 5000);
            Logger.Info("KJ drop: " + car.Label + " from " + spawn + " to " + player.Position + ".");
            return true;
        }

        private static Vector3 KJStart(Ped player)
        {
            var back = player.ForwardVector * -1f; back.Z = 0f;
            for (int i = 0; i < 8; i++)
            {
                float angle = (float)(i * Math.PI / 4);
                var dir = i == 0 ? back : new Vector3((float)Math.Cos(angle), (float)Math.Sin(angle), 0f);
                var candidate = player.Position + dir * 150f;
                var road = World.GetNextPositionOnStreet(candidate);
                if (road != Vector3.Zero && road.DistanceTo(player.Position) >= 80f) return road;
            }
            return player.Position + back * 120f;
        }

        /// <summary>The delivery, each tick: KJ re-aimed when the player moves, the arrival, the hand-over, and his walk away.</summary>
        private void MaintainDelivery()
        {
            var delivery = _delivery;
            if (delivery == null) return;
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;
            if (delivery.Vehicle == null || !delivery.Vehicle.Exists() || delivery.Vehicle.IsDead)
            {
                delivery.Car.InShop = true; _state.Save();
                if (delivery.KJ != null && delivery.KJ.Exists()) GameUtils.SafeRelease(delivery.KJ);
                _delivery = null;
                GameUtils.Notify("~r~The " + delivery.Car.Label + " was wrecked on the way. KJ can bring it back from the shop for a fee.");
                return;
            }
            if (delivery.Arrived)
            {
                if (delivery.KJ != null && delivery.KJ.Exists() && Game.GameTime - delivery.DoneAt > 20000) { GameUtils.SafeRelease(delivery.KJ); _delivery = null; }
                return;
            }
            float distance = delivery.Vehicle.Position.DistanceTo(player.Position);
            bool there = distance < 14f && delivery.Vehicle.Speed < 2f;
            bool late = Game.GameTime - delivery.Ordered > 90000;
            if (!there && late)
            {
                // Ninety seconds is enough: the car is set on the road beside the player.
                var near = World.GetNextPositionOnStreet(player.Position + player.ForwardVector * 12f);
                if (near == Vector3.Zero) near = player.Position + player.ForwardVector * 8f;
                delivery.Vehicle.Position = near; GameUtils.SetOnGround(delivery.Vehicle);
                there = true;
                Logger.Warn("KJ drop: the road took too long; the " + delivery.Car.Label + " was set beside the player.");
            }
            if (!there)
            {
                if (Game.GameTime - delivery.LastOrder > 6000 && player.Position.DistanceTo(delivery.LastTarget) > 40f && delivery.KJ != null && delivery.KJ.Exists())
                {
                    delivery.KJ.Task.DriveTo(delivery.Vehicle, player.Position, 8f, 20f, DrivingStyle.Normal);
                    delivery.LastOrder = Game.GameTime; delivery.LastTarget = player.Position;
                }
                return;
            }
            delivery.Arrived = true; delivery.DoneAt = Game.GameTime;
            if (delivery.KJ != null && delivery.KJ.Exists())
            {
                delivery.KJ.Task.LeaveVehicle();
                delivery.KJ.Task.WanderAround(player.Position, 30f);
            }
            delivery.Vehicle.IsEngineRunning = true;
            _out[delivery.Car.Id] = delivery.Vehicle;
            if (_outBlips.TryGetValue(delivery.Car.Id, out var blip) && blip != null && blip.Exists()) blip.Name = "Garage: " + delivery.Car.Label;
            GameUtils.Subtitle("~b~KJ:~s~ " + delivery.Car.Label + ", like you asked. Bring it back with all four wheels; I'm not your valet.", 6000);
            Logger.Info("KJ drop: the " + delivery.Car.Label + " handed over.");
        }

        /// <summary>A delivery on the road or a car out front is put away before another is asked for.</summary>
        public void RecallDelivery()
        {
            var delivery = _delivery;
            if (delivery != null)
            {
                if (delivery.KJ != null && delivery.KJ.Exists()) GameUtils.SafeRelease(delivery.KJ);
                if (!delivery.Arrived && delivery.Vehicle != null && delivery.Vehicle.Exists()) { Capture(delivery.Vehicle, delivery.Car); GameUtils.SafeDelete(delivery.Vehicle); }
                if (_outBlips.TryGetValue(delivery.Car.Id, out var blip)) { GameUtils.SafeDelete(blip); _outBlips.Remove(delivery.Car.Id); }
                _delivery = null;
            }
        }

        /// <summary>A car left far behind for a minute goes home on its own; a wrecked one goes to the shop.</summary>
        private void MaintainReturns()
        {
            var player = Game.Player.Character;
            foreach (int id in _out.Keys.ToList())
            {
                var vehicle = _out[id];
                var car = _state.Vehicles.FirstOrDefault(v => v.Id == id);
                if (car == null) { Forget(id); continue; }
                if (vehicle == null || !vehicle.Exists()) { Forget(id); continue; }
                if (vehicle.IsDead) { car.InShop = true; _state.Save(); Forget(id); GameUtils.SafeRelease(vehicle); GameUtils.Notify("~r~The " + car.Label + " is wrecked. KJ can bring it back from the shop for a fee."); continue; }
                if (player == null || !player.Exists()) continue;
                if (player.Position.DistanceTo(vehicle.Position) <= ReturnDistance) { _awaySince.Remove(id); continue; }
                if (!_awaySince.TryGetValue(id, out int since)) { _awaySince[id] = Game.GameTime; continue; }
                if (Game.GameTime - since < ReturnAfterMs) continue;
                Capture(vehicle, car);
                GameUtils.SafeDelete(vehicle);
                Forget(id);
                _state.Save();
                Logger.Info("Garage: the " + car.Label + " left behind went back to " + car.Garage + ".");
            }
        }

        // ---------- the tick ----------

        public void Update(bool canOpen)
        {
            if (!_crew.IsDeployed) { Clear(); return; }
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;
            if (_blips.Count == 0) RefreshBlips();
            MaintainDelivery();
            MaintainReturns();
            GarageSite near = null; float best = 35f;
            foreach (var site in Sites)
            {
                if (site.StarterOf.HasValue) continue;
                var door = Position(site);
                if (!door.HasValue) continue;
                float distance = player.Position.DistanceTo(door.Value);
                if (distance < best) { best = distance; near = site; }
            }
            if (near == null) return;
            var point = Position(near).Value;
            bool owned = Owned(near);
            GameUtils.DrawObjectiveMarker(point, owned ? Color.LimeGreen : Color.Gold, 2.5f);
            if (!canOpen || !(Allowed?.Invoke() ?? false) || Game.Player.WantedLevel > 0) return;
            if (best > DoorRadius) return;
            GameUtils.Subtitle("~g~" + near.Name + "~s~ | " + (owned ? Used(near) + "/" + near.Capacity + " bays | E / D-pad Right: garage" : "$" + near.Price.ToString("N0") + " | E / D-pad Right: buy"), 500);
            if (Game.IsControlJustPressed(Control.Context)) OpenMenu?.Invoke(near);
        }

        private void RefreshBlips()
        {
            foreach (var blip in _blips) GameUtils.SafeDelete(blip);
            _blips.Clear();
            foreach (var site in Sites)
            {
                if (site.StarterOf.HasValue) continue;
                var door = Position(site);
                if (!door.HasValue) continue;
                if (site.Tier.HasValue && !Owned(site)) continue;
                var blip = World.CreateBlip(door.Value);
                if (blip == null) continue;
                bool owned = Owned(site);
                blip.Sprite = (BlipSprite)357;
                blip.Color = owned ? BlipColor.Green : BlipColor.Yellow;
                blip.IsShortRange = true;
                blip.Name = owned ? site.Name : site.Name + " - $" + site.Price.ToString("N0");
                _blips.Add(blip);
            }
        }

        /// <summary>Stand-down and teardown: KJ and the cars out front are released to the world; nothing is deleted from the save.</summary>
        public void Clear()
        {
            RecallDelivery();
            foreach (var blip in _blips) GameUtils.SafeDelete(blip);
            _blips.Clear();
            foreach (var blip in _outBlips.Values) GameUtils.SafeDelete(blip);
            _outBlips.Clear();
            foreach (var vehicle in _out.Values) if (vehicle != null && vehicle.Exists()) GameUtils.SafeRelease(vehicle);
            _out.Clear();
            _awaySince.Clear();
        }

        // ---------- the build ----------

        private void TakeOut(OwnedVehicle car, Vehicle vehicle, string blipName)
        {
            _out[car.Id] = vehicle;
            _awaySince.Remove(car.Id);
            if (_outBlips.TryGetValue(car.Id, out var old)) GameUtils.SafeDelete(old);
            var blip = vehicle.AddBlip();
            if (blip != null) { blip.Sprite = BlipSprite.PersonalVehicleCar; blip.Color = OwnerColor(car); blip.Name = blipName; _outBlips[car.Id] = blip; }
        }

        private void Forget(int id)
        {
            _out.Remove(id);
            _awaySince.Remove(id);
            if (_outBlips.TryGetValue(id, out var blip)) { GameUtils.SafeDelete(blip); _outBlips.Remove(id); }
        }

        private static BlipColor OwnerColor(OwnedVehicle car) =>
            car.Owner == CrewSlot.Ice.ToString() ? BlipColor.Blue : car.Owner == CrewSlot.Gohan.ToString() ? BlipColor.Green : car.Owner == CrewSlot.Guess.ToString() ? BlipColor.Orange : BlipColor.White;

        private static string SafeLabel(Vehicle vehicle)
        {
            string name;
            try { name = vehicle.DisplayName; } catch { name = null; }
            if (string.IsNullOrWhiteSpace(name)) return "Car";
            name = name.ToLowerInvariant();
            return char.ToUpperInvariant(name[0]) + name.Substring(1);
        }

        private Vehicle Create(OwnedVehicle car, Vector3 position, float heading)
        {
            var model = string.IsNullOrEmpty(car.ModelName) ? new Model((int)car.ModelHash) : new Model(car.ModelName);
            if (!GameUtils.RequestModel(model, 2500)) { Logger.Warn("Garage: model not loaded for " + car.Label + "."); return null; }
            var vehicle = World.CreateVehicle(model, position, heading);
            model.MarkAsNoLongerNeeded();
            if (vehicle == null || !vehicle.Exists()) return null;
            vehicle.IsPersistent = true;
            GameUtils.HoldUntilGrounded(vehicle);
            Apply(vehicle, car);
            return vehicle;
        }

        /// <summary>Put a record's build on a car.</summary>
        public static void Apply(Vehicle vehicle, OwnedVehicle car)
        {
            if (vehicle == null || !vehicle.Exists() || car == null) return;
            try
            {
                var mods = vehicle.Mods;
                mods.InstallModKit();
                if (car.WheelType >= 0) Function.Call(Hash.SET_VEHICLE_WHEEL_TYPE, vehicle, car.WheelType);
                foreach (var pair in car.Mods)
                {
                    var mod = mods[(VehicleModType)pair.Key];
                    if (pair.Value >= -1 && pair.Value < mod.Count) mod.Index = pair.Value;
                }
                if (car.PrimaryColor >= 0) mods.PrimaryColor = (VehicleColor)car.PrimaryColor;
                if (car.SecondaryColor >= 0) mods.SecondaryColor = (VehicleColor)car.SecondaryColor;
                if (car.Livery >= 0) mods.Livery = car.Livery;
                if (car.WindowTint >= 0) Function.Call(Hash.SET_VEHICLE_WINDOW_TINT, vehicle, car.WindowTint);
                if (!string.IsNullOrEmpty(car.Plate)) Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, vehicle, car.Plate);
                if (car.TiresReinforced) vehicle.CanTiresBurst = false;
            }
            catch (Exception ex) { Logger.Error("Garage: the build could not be applied to " + car.Label, ex); }
        }

        /// <summary>Read a car's build into its record. True when something changed.</summary>
        public static bool Capture(Vehicle vehicle, OwnedVehicle car)
        {
            if (vehicle == null || !vehicle.Exists() || car == null) return false;
            string before = car.Fingerprint();
            try
            {
                var mods = vehicle.Mods;
                car.Mods.Clear();
                foreach (VehicleModType type in Enum.GetValues(typeof(VehicleModType)))
                {
                    int index = mods[type].Index;
                    if (index >= 0) car.Mods[(int)type] = index;
                }
                car.PrimaryColor = (int)mods.PrimaryColor;
                car.SecondaryColor = (int)mods.SecondaryColor;
                car.Livery = mods.Livery;
                car.WheelType = Function.Call<int>(Hash.GET_VEHICLE_WHEEL_TYPE, vehicle);
                car.WindowTint = Function.Call<int>(Hash.GET_VEHICLE_WINDOW_TINT, vehicle);
                car.Plate = Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, vehicle) ?? "";
                car.TiresReinforced = !vehicle.CanTiresBurst;
            }
            catch (Exception ex) { Logger.Error("Garage: the build could not be read from " + car.Label, ex); return false; }
            return car.Fingerprint() != before;
        }
    }
}
