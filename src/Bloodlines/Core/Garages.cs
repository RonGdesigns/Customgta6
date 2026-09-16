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
        private static readonly Dictionary<string, int> ModelPrices = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "virgo", 18000 }, { "dukes", 22000 }, { "tampa", 24000 }, { "remus", 28000 },
            { "kuruma2", 80000 }, { "buffalo4", 100000 }, { "jubilee", 90000 }, { "champion", 140000 },
            { "ignus", 160000 }, { "krieger", 180000 }, { "emerus", 175000 }, { "thrax", 155000 },
            { "italirsx", 140000 }, { "italigto", 110000 }, { "nero", 125000 }, { "t20", 135000 },
            { "osiris", 130000 }, { "zentorno", 110000 }, { "vagner", 145000 }, { "xa21", 145000 },
            { "banshee2", 75000 }, { "tempesta", 100000 }, { "nightshark", 110000 },
            { "insurgent", 150000 }, { "insurgent2", 120000 }, { "dune3", 85000 }, { "shotaro", 65000 },
            { "khanjali", 450000 }, { "scramjet", 350000 }, { "vigilante", 375000 }, { "toreador", 280000 },
            { "deluxo", 300000 }, { "stromberg", 220000 }, { "apc", 320000 }, { "ruiner2", 325000 },
            { "barrage", 160000 }, { "patriot3", 125000 }, { "menacer", 140000 }, { "halftrack", 180000 },
            { "scarab", 260000 }, { "rhino", 350000 }, { "oppressor", 220000 }, { "oppressor2", 300000 },
            { "buffalo5", 115000 }, { "cyclone", 190000 }, { "cyclone2", 240000 }, { "tezeract", 210000 },
            { "raiden", 95000 }, { "neon", 110000 }, { "iwagen", 85000 }, { "coureur", 75000 },
            { "imorgon", 90000 }, { "voltic", 60000 }, { "powersurge", 45000 },
            { "dune4", 200000 }, { "phantom2", 220000 }, { "openwheel1", 340000 }, { "openwheel2", 360000 },
            { "sultan3", 45000 }, { "rt3000", 48000 }, { "driftfr36", 52000 }, { "vorschlaghammer", 60000 },
            { "drifttampa", 58000 }, { "pariah", 130000 }, { "polgauntlet", 95000 }
        };
        public static int Of(StoryVehicles.Choice choice)
        {
            if (choice == null) return StreetValue;
            if (ModelPrices.TryGetValue(choice.Model, out int price)) return price;
            switch (choice.Category)
            {
                case "Motorcycles": return 20000;
                case "Off-road": return 35000;
                case "Boats": return 45000;
                case "Helicopters": return 250000;
                case "Planes": return 300000;
                case "Jets": return 400000;
                case "Special": return 350000;
                case "Weaponized": return 250000;
                case "Electric": return 120000;
                default: return 40000;
            }
        }
        public static int UpgradeValue(OwnedVehicle car)
        {
            int value=car.Mods.Count(p=>p.Value>=0)*250;
            if(car.Finish.TryGetValue("toggle17",out int nitrous)&&nitrous!=0)value+=300;
            if(car.Finish.TryGetValue("toggle18",out int turbo)&&turbo!=0)value+=1500;
            foreach(int slot in new[]{20,22})if(car.Finish.TryGetValue("toggle"+slot,out int on)&&on!=0)value+=300;
            for(int side=0;side<4;side++)if(car.Finish.TryGetValue("neon"+side,out int on)&&on!=0)value+=200;
            if(car.TiresReinforced)value+=500;
            if(car.Livery>=0)value+=375;
            if(car.WindowTint>0)value+=125;
            for(int i=0;i<=20;i++)if(car.Finish.TryGetValue("extra"+i,out int extra)&&extra!=0)value+=125;
            return value;
        }
        public static int Sale(OwnedVehicle car) => Math.Max(500, car.Price / 2)+UpgradeValue(car);
        public static int StreetWorth(Vehicle vehicle)
        {
            var known = StoryVehicles.Catalog.FirstOrDefault(c => Game.GenerateHash(c.Model) == vehicle.Model.Hash);
            return known == null ? StreetValue : Math.Min(30000, Of(known));
        }
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
        public const float DeliverySpeed = 36f;
        public const int DeliveryDeadlineMs = 45000;
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

        private sealed class Delivery { public OwnedVehicle Car; public Vehicle Vehicle; public Ped KJ; public int Ordered, LastOrder, DoneAt; public Vector3 LastTarget; public bool Arrived, Walking; }

        private readonly CrewRoster _crew;
        private readonly CampaignState _state;
        private readonly LocationBook _locations;
        private readonly CrewVan _vans;
        private readonly List<Blip> _blips = new List<Blip>();
        private readonly Dictionary<int, Vehicle> _out = new Dictionary<int, Vehicle>();
        private readonly Dictionary<int, Blip> _outBlips = new Dictionary<int, Blip>();
        private readonly Dictionary<int, int> _awaySince = new Dictionary<int, int>();
        private Delivery _delivery;
        private int _nextBuildCapture;
        public Func<bool> Allowed;
        public Action<GarageSite> OpenMenu;
        private bool CanManage()
        {
            if ((Allowed?.Invoke() ?? false) && Game.Player.WantedLevel == 0) return true;
            GameUtils.Notify("~y~Garage services are available outside in free roam, after losing the police."); return false;
        }
        public static bool RoadModel(StoryVehicles.Choice choice)
        { if (choice == null) return false; var model = new Model(choice.Model); return model.IsValid && model.IsInCdImage && (model.IsCar || model.IsBike) && !model.IsBoat && !model.IsPlane && !model.IsHelicopter; }
        private static bool Occupied(Vehicle car, Ped except = null)
        {
            if (car == null || !car.Exists()) return false;
            int seats = Math.Max(3, Function.Call<int>(Hash.GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS, car));
            for (int i = -1; i < seats; i++)
            { var ped = car.GetPedOnSeat((VehicleSeat)i); if (ped != null && ped.Exists() && ped != except) return true; }
            return false;
        }

        public GarageService(CrewRoster crew, CampaignState state, LocationBook locations, CrewVan vans)
        { _crew = crew; _state = state; _locations = locations; _vans = vans; }

        public Action<string, string> OnActivity;
        private void Report(string title, string body) { try { OnActivity?.Invoke(title, body); } catch (Exception ex) { Logger.Warn("Garage notification: " + ex.Message); } }
        public string DeliveryLabel => _delivery?.Car?.Label ?? "No delivery";
        public string DeliveryToken => _delivery == null ? "none" : _delivery.Car.Id + ":" + _delivery.Ordered;
        public int DeliveryCarId => _delivery?.Car?.Id ?? 0;
        public bool DeliveryActive => _delivery != null && !_delivery.Arrived;
        public Vehicle DeliveryVehicle => _delivery?.Vehicle;
        public Ped KJ => _delivery?.KJ;

        public const int DailySaleLimit = 10;
        private void RefreshSalesDay()
        {
            var today = World.CurrentDate.Date;
            if (!DateTime.TryParseExact(_state.VehicleSalesDay, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var previous) || today > previous.Date)
            { _state.VehicleSalesDay = today.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture); _state.VehicleSalesCount = 0; }
        }
        public int SalesRemaining { get { RefreshSalesDay(); return Math.Max(0, DailySaleLimit - _state.VehicleSalesCount); } }
        private bool SaleAllowed()
        {
            if (SalesRemaining > 0) return true;
            GameUtils.Notify("~y~Ten cars sold today. The buyer returns after the next GTA midnight."); return false;
        }
        private void RecordSale(int paid)
        { _state.VehicleSalesCount++; _state.CashOnHand += paid; _state.Save(); Report("Vehicle sale receipt", "$" + paid.ToString("N0") + " received. " + SalesRemaining + "/10 sales remaining today."); }
        public int StreetSaleQuote(Vehicle vehicle)
        {
            if (vehicle == null) return 0;
            var live = _out.FirstOrDefault(p => p.Value == vehicle);
            var record = _state.Vehicles.FirstOrDefault(v => v.Id == live.Key);
            if(record!=null){Capture(vehicle,record);return VehiclePricing.Sale(record);}
            var street=new OwnedVehicle{Price=VehiclePricing.StreetWorth(vehicle)};Capture(vehicle,street);return VehiclePricing.Sale(street);
        }
        public bool SellStreetVehicle(Vehicle vehicle)
        {
            if (!CanManage() || !SaleAllowed() || vehicle == null || !vehicle.Exists() || !vehicle.IsDriveable || vehicle.Speed > 1f) return false;
            var player = Game.Player.Character;
            if (player == null || !player.Exists() || player.Position.DistanceTo(vehicle.Position) > DoorRadius) return false;
            bool atBuyer = ShopService.Sites.Any(site => (site.Kind == ShopKind.Dealer || site.Kind == ShopKind.Guess || site.Kind == ShopKind.Customs) && site.Position.DistanceTo(player.Position) < 25f) ||
                OwnedSites.Any(site => Position(site).HasValue && Position(site).Value.DistanceTo(player.Position) < DoorRadius);
            if (!atBuyer) { GameUtils.Notify("~y~Bring the car to a garage, Los Santos Customs, Guess Customs or the dealer."); return false; }
            if (_vans?.IsVan(vehicle) ?? false) { GameUtils.Notify("~y~The crew's mission Granger is not for sale."); return false; }
            if ((!vehicle.Model.IsCar && !vehicle.Model.IsBike) || vehicle.Model.IsBoat || vehicle.Model.IsPlane || vehicle.Model.IsHelicopter || Occupied(vehicle, player))
            { GameUtils.Notify("~y~Park a road vehicle and let the passengers step out before selling."); return false; }
            if (DeliveryActive && _delivery.Vehicle == vehicle) return false;
            if (player.IsInVehicle(vehicle) && !ExitVehicleStep.ForceOut(player)) return false;
            var owned = _out.FirstOrDefault(p => p.Value == vehicle);
            var record = _state.Vehicles.FirstOrDefault(v => v.Id == owned.Key);
            if (record != null) return Sell(record);
            int paid = StreetSaleQuote(vehicle);
            GameUtils.SafeDelete(vehicle); if (vehicle.Exists()) return false; RecordSale(paid);
            GameUtils.Notify("~g~Car sold for $" + paid.ToString("N0") + ".~s~ " + SalesRemaining + "/10 sales left this GTA day."); return true;
        }

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
            if (!CanManage() || site == null || Owned(site) || site.Price <= 0) return false;
            if (_state.CashOnHand < site.Price) { GameUtils.Notify("~y~Not enough crew cash for " + site.Name + ": $" + site.Price.ToString("N0") + "."); return false; }
            _state.CashOnHand -= site.Price;
            _state.Garages.Add(site.Id);
            _state.Save();
            GameUtils.Notify("~g~" + site.Name + " is the crew's.~s~ " + site.Capacity + " bays. Crew cash $" + _state.CashOnHand.ToString("N0") + ".");
            Report("Garage purchased", site.Name + " - $" + site.Price.ToString("N0") + ".");
            Logger.Info("Garage bought: " + site.Id + " for $" + site.Price + ".");
            RefreshBlips();
            return true;
        }

        // ---------- the door ----------

        /// <summary>Register the fitted build at Customs, reserving a home bay while the same car stays out.</summary>
        public bool SaveFromShop(ShopService shops, ShopSite shop, GarageSite destination)
        {
            if (!CanManage() || shops == null || !ShopService.IsGarage(shop) || !Owned(destination)) return false;
            var vehicle = shops.Car(shop);
            if (vehicle == null || shops.HasVehiclePreview || vehicle.Handle == ShopService.PreviewVehicleHandle)
            { GameUtils.Notify("~y~Confirm or cancel the preview, then save your stopped car from the driver's seat."); return false; }
            if (_vans != null && _vans.IsVan(vehicle))
            { GameUtils.Notify("~y~Your crew car's confirmed upgrades are saved automatically in the crew fleet."); return false; }
            if (DeliveryActive && _delivery.Vehicle == vehicle) return false;
            var live = _out.FirstOrDefault(p => p.Value != null && p.Value.Exists() && p.Value.Handle == vehicle.Handle);
            var previous = _state.Vehicles.FirstOrDefault(v => v.Id == live.Key);
            if ((previous == null || !string.Equals(previous.Garage, destination.Id, StringComparison.OrdinalIgnoreCase)) && !HasFreeSlot(destination))
            { GameUtils.Notify("~y~" + destination.Name + " is full. Choose another garage; no saved car was replaced."); return false; }
            var record = previous == null ? new OwnedVehicle { Id = _state.NextVehicleId, ModelHash = unchecked((uint)vehicle.Model.Hash),
                Label = SafeLabel(vehicle), Stolen = true, Price = VehiclePricing.StreetWorth(vehicle), Owner = _crew.ActiveSlot.ToString() }
                : OwnedVehicle.FromJson(Json.Object(Json.Read(previous.Fingerprint())));
            // Capture into a temporary record so a native read failure cannot damage the previous saved build.
            if (!TryCapture(vehicle, record, out var changed))
            { GameUtils.Notify("~y~The fitted build could not be read. Your previous garage save is unchanged."); return false; }
            record.Garage = destination.Id; record.InShop = false;
            if (previous == null) { _state.NextVehicleId++; _state.Vehicles.Add(record); }
            else
            {
                _state.Vehicles[_state.Vehicles.IndexOf(previous)] = record;
                if (_delivery != null && _delivery.Car.Id == record.Id) _delivery.Car = record;
            }
            vehicle.IsPersistent = true;
            TakeOut(record, vehicle, record.Label + " - saved to " + destination.Name);
            _state.Save();
            GameUtils.Notify("~g~" + record.Label + " saved to " + destination.Name + ".~s~ Keep driving; your fitted upgrades are saved. " + Summary(destination));
            return true;
        }

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
            if (!CanManage() || site == null || vehicle == null || !vehicle.Exists() || !vehicle.IsDriveable) return false;
            if ((!vehicle.Model.IsCar && !vehicle.Model.IsBike) || vehicle.Model.IsBoat || vehicle.Model.IsPlane || vehicle.Model.IsHelicopter)
            { GameUtils.Notify("~y~This garage stores road vehicles. Boats and aircraft need surveyed berth/hangar storage."); return false; }
            if (Occupied(vehicle, Game.Player.Character) || vehicle.Speed > 1f)
            { GameUtils.Notify("~y~Park and let the passengers step out before storing the car."); return false; }
            var door = Position(site);
            if (!door.HasValue || Game.Player.Character.Position.DistanceTo(door.Value) > DoorRadius || vehicle.Position.DistanceTo(door.Value) > DoorRadius)
            { GameUtils.Notify("~y~Bring the car to this garage's door before storing it."); return false; }
            if (!Owned(site)) { GameUtils.Notify("~y~" + site.Name + " is not the crew's yet."); return false; }
            if (_vans != null && _vans.IsVan(vehicle)) { GameUtils.Notify("~y~The crew van keeps itself; it does not go in a garage."); return false; }
            var record = _out.FirstOrDefault(p => p.Value != null && p.Value.Exists() && p.Value.Handle == vehicle.Handle).Key is int id && id > 0
                ? _state.Vehicles.FirstOrDefault(v => v.Id == id) : null;
            if (record == null && Used(site) >= site.Capacity) { GameUtils.Notify("~y~" + site.Name + " is full (" + site.Capacity + " bays)."); return false; }
            if (record != null && !string.Equals(record.Garage, site.Id, StringComparison.OrdinalIgnoreCase) && Used(site) >= site.Capacity)
            { GameUtils.Notify("~y~" + site.Name + " is full (" + site.Capacity + " bays)."); return false; }
            if (Game.Player.Character.IsInVehicle(vehicle) && !ExitVehicleStep.ForceOut(Game.Player.Character)) return false;
            if (record == null)
            {
                record = new OwnedVehicle { Id = _state.NextVehicleId++, ModelHash = unchecked((uint)vehicle.Model.Hash), Label = SafeLabel(vehicle), Stolen = true, Price = VehiclePricing.StreetWorth(vehicle) };
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
            if (!CanManage() || car == null || !_state.Vehicles.Contains(car) || DeliveryActive && _delivery.Car == car) return null;
            var existing = OutVehicle(car);
            if (existing != null) { GameUtils.Notify("~y~" + car.Label + " is already out."); return existing; }
            if (car.InShop) { GameUtils.Notify("~y~" + car.Label + " is in the shop. Ask KJ to fetch it."); return null; }
            var site = Site(car.Garage);
            var door = Position(site);
            if (site == null || !door.HasValue) { GameUtils.Notify("~y~" + car.Label + "'s garage is missing from the map."); return null; }
            float heading = Heading(site);
            var forward = new Vector3(-(float)Math.Sin(heading * Math.PI / 180f), (float)Math.Cos(heading * Math.PI / 180f), 0f);
            var wanted = _locations.Get(site.Key+".VehicleSpawn")?.Position ?? door.Value + forward * 7f;
            var spot = World.GetNextPositionOnStreet(wanted);
            if (spot == Vector3.Zero || spot.DistanceTo(wanted) > 25f)
            { GameUtils.Notify("~y~The garage forecourt has no loaded road space. Move closer and try again."); return null; }
            if (Function.Call<bool>(Hash.IS_POSITION_OCCUPIED, spot.X, spot.Y, spot.Z, 3f, false, true, false, false, false, 0, false))
            { GameUtils.Notify("~y~Clear the garage forecourt before taking another car out."); return null; }
            var vehicle = Create(car, spot, heading);
            if (vehicle == null) { GameUtils.Notify("~y~" + car.Label + " could not be brought out (model not loaded)."); return null; }
            TakeOut(car, vehicle, "Garage: " + car.Label);
            GameUtils.Notify("~g~" + car.Label + " is out front of " + site.Name + ".");
            return vehicle;
        }

        public int RecoveryPrice(OwnedVehicle car)
        {
            if (car == null || !_state.Vehicles.Contains(car)) return 0;
            var live = OutVehicle(car);
            if (car.InShop || live != null && (live.IsDead || !live.IsDriveable)) return Math.Max(500, car.Price / 10);
            return live != null ? 500 : 0;
        }
        public bool Recover(OwnedVehicle car, int agreedPrice)
        {
            if (!CanManage() || car == null || !_state.Vehicles.Contains(car) || DeliveryActive && DeliveryCarId == car.Id) return false;
            var site = Site(car.Garage);var live = OutVehicle(car);
            if (live != null && ShopService.PreviewVehicleHandle == live.Handle) return false;
            int fee = RecoveryPrice(car);
            if (site == null || !Owned(site) || fee == 0 || fee != agreedPrice || _state.CashOnHand < fee || Occupied(live)) return false;
            if (live != null)
            {
                if (!live.IsDead && !Capture(live, car)) return false;
                GameUtils.SafeDelete(live);
                if (live.Exists()) return false;
            }
            Forget(car.Id);
            car.InShop = false; _state.CashOnHand -= fee; _state.Save();
            Report("Vehicle recovered", car.Label + " - $" + fee.ToString("N0") + ". Returned to " + site.Name + ".");
            return true;
        }

        public bool Sell(OwnedVehicle car)
        {
            if (!CanManage() || !SaleAllowed() || car == null || !_state.Vehicles.Contains(car) || DeliveryActive && _delivery.Car == car) return false;
            var live = OutVehicle(car);
            if (Occupied(live)) { GameUtils.Notify("~y~Everyone must leave the car before it can be sold."); return false; }
            if (live != null) { Capture(live,car); GameUtils.SafeDelete(live); if (live.Exists()) return false; }
            Forget(car.Id);
            int paid = VehiclePricing.Sale(car);
            _state.Vehicles.Remove(car);
            RecordSale(paid);
            GameUtils.Notify("~g~" + car.Label + " sold for $" + paid.ToString("N0") + ".~s~ Crew cash $" + _state.CashOnHand.ToString("N0") + ".");
            return true;
        }

        public void Tag(OwnedVehicle car, CrewSlot? owner)
        {
            if (!CanManage() || car == null || !_state.Vehicles.Contains(car)) return;
            car.Owner = owner.HasValue ? owner.Value.ToString() : "";
            _state.Save();
        }

        // ---------- the dealer ----------

        public bool BuyFromDealer(StoryVehicles.Choice choice, GarageSite site)
        {
            if (!CanManage() || choice == null || site == null || !RoadModel(choice)) return false;
            var checkedModel = new Model(choice.Model);
            bool available = GameUtils.RequestModel(checkedModel, 1000); checkedModel.MarkAsNoLongerNeeded();
            if (!available) { GameUtils.Notify("~y~This model is unavailable. No money was taken."); return false; }
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
            Report("Vehicle purchased", choice.Name + " - $" + price.ToString("N0") + ". Stored at " + site.Name + ".");
            Logger.Info("Dealer: bought " + choice.Model + " into " + site.Id + " for $" + price + ".");
            return true;
        }

        // ---------- KJ ----------

        /// <summary>KJ brings a car from its garage to whoever called: he starts well out on the road and drives it in.</summary>
        public bool CallKJ(OwnedVehicle car)
        {
            if (!CanManage() || car == null || !_state.Vehicles.Contains(car)) return false;
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return false;
            if (_delivery != null && !_delivery.Arrived && _delivery.Car == car) { GameUtils.Notify("~y~KJ is already on his way with the " + car.Label + "."); return false; }
            var already = OutVehicle(car);
            if (already != null) { GameUtils.Notify("~y~" + car.Label + " is already out; it is marked on the map."); return false; }
            int fee = car.InShop ? Math.Max(500, car.Price / 10) : 0;
            if (car.InShop)
            {
                if (_state.CashOnHand < fee) { GameUtils.Notify("~y~" + car.Label + " is in the shop: $" + fee.ToString("N0") + " to bring it back."); return false; }
            }
            var spawn = KJStart(player);
            if (spawn == Vector3.Zero) { GameUtils.Notify("~y~KJ needs a clear road nearby. Move toward a street and call again."); return false; }
            var vehicle = Create(car, spawn, DriveUpStep.HeadingBetween(spawn, player.Position));
            if (vehicle == null) { GameUtils.Notify("~y~KJ could not get the " + car.Label + " started (model not loaded)."); return false; }
            // Commit the repair only after a valid vehicle exists. A failed new
            // request leaves both money and any previous delivery intact.
            if (_delivery != null) RecallDelivery();
            if (fee > 0) { _state.CashOnHand -= fee; car.InShop = false; _state.Save(); }
            var model = new Model(KJModel);
            Ped kj = null;
            if (GameUtils.RequestModel(model)) { kj = World.CreatePed(model, spawn, 0f); model.MarkAsNoLongerNeeded(); }
            if (kj == null || !kj.Exists())
            {
                // No KJ: the car is left at the roadside and marked; better than no car.
                TakeOut(car, vehicle, "KJ's drop: " + car.Label);
                Report("Roadside drop ready", car.Label + " is marked for collection. Repair fee: $" + fee.ToString("N0") + ".");
                GameUtils.Notify("~y~KJ could not come; the " + car.Label + " is at the roadside, marked on the map.");
                return true;
            }
            kj.IsPersistent = true;
            kj.BlockPermanentEvents = true;
            kj.RelationshipGroup = _crew.CrewGroup;
            kj.SetIntoVehicle(vehicle, VehicleSeat.Driver);
            vehicle.IsEngineRunning = true;
            Function.Call(Hash.SET_DRIVER_ABILITY, kj, 1f);
            Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, kj, 0.55f);
            kj.AlwaysKeepTask = true;
            _delivery = new Delivery { Car = car, Vehicle = vehicle, KJ = kj, Ordered = Game.GameTime, LastOrder = Game.GameTime, LastTarget = player.Position };
            kj.Task.DriveTo(vehicle, DeliveryTarget(player), 7f, DeliverySpeed, (DrivingStyle)CrewDriving.TrafficFlags);
            var blip = vehicle.AddBlip();
            if (blip != null) { blip.Sprite = BlipSprite.PersonalVehicleCar; blip.Color = BlipColor.Yellow; blip.Name = "KJ: " + car.Label; _outBlips[car.Id] = blip; }
            GameUtils.Subtitle("~b~KJ:~s~ On my way with the " + car.Label + ". Stay where I can find you.", 5000);
            Report("Delivery accepted", car.Label + ". Repair fee: $" + fee.ToString("N0") + ". KJ is driving over.");
            Logger.Info("KJ drop: " + car.Label + " from " + spawn + " to " + player.Position + ".");
            return true;
        }

        private static Vector3 DeliveryTarget(Ped player)
        {
            var road = World.GetNextPositionOnStreet(player.Position);
            return road != Vector3.Zero && road.DistanceTo(player.Position) < 45f ? road : player.Position;
        }

        private static Vector3 KJStart(Ped player)
        {
            var back = player.ForwardVector * -1f; back.Z = 0f;
            for (int i = 0; i < 8; i++)
            {
                float angle = (float)(i * Math.PI / 4);
                var dir = i == 0 ? back : new Vector3((float)Math.Cos(angle), (float)Math.Sin(angle), 0f);
                var candidate = player.Position + dir * 110f;
                var road = World.GetNextPositionOnStreet(candidate);
                if (road != Vector3.Zero && road.DistanceTo(player.Position) >= 80f && road.DistanceTo(player.Position) < 260f && !Function.Call<bool>(Hash.IS_SPHERE_VISIBLE, road.X, road.Y, road.Z, 5f)) return road;
            }
            return Vector3.Zero;
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
                Report("Delivery vehicle lost", delivery.Car.Label + " needs repair. No replacement was ordered automatically.");
                if (delivery.KJ != null && delivery.KJ.Exists()) GameUtils.SafeRelease(delivery.KJ);
                _delivery = null;
                GameUtils.Notify("~r~The " + delivery.Car.Label + " was wrecked on the way. KJ can bring it back from the shop for a fee.");
                return;
            }
            if (delivery.Arrived)
            {
                if (delivery.KJ == null || !delivery.KJ.Exists()) { _delivery = null; return; }
                if (!delivery.Walking && !delivery.KJ.IsInVehicle())
                { delivery.KJ.Task.WanderAround(delivery.Vehicle.Position, 30f); delivery.Walking = true; }
                if (Game.GameTime - delivery.DoneAt > 20000) { GameUtils.SafeRelease(delivery.KJ); _delivery = null; }
                return;
            }
            float distance = delivery.Vehicle.Position.DistanceTo(player.Position);
            bool there = (distance < 14f || delivery.Vehicle.Position.DistanceTo(DeliveryTarget(player)) < 9f) && delivery.Vehicle.Speed < 2f;
            bool late = Game.GameTime - delivery.Ordered > DeliveryDeadlineMs;
            if (!there && late)
            {
                // Traffic can delay him; never pop an occupied car into the camera.
                // Leave the existing car marked where it reached, ready for pickup.
                there = delivery.Vehicle.Speed < 2f;
                if (!there && delivery.KJ != null && delivery.KJ.Exists())
                    Function.Call(Hash.TASK_VEHICLE_TEMP_ACTION, delivery.KJ, delivery.Vehicle, 27, 1500);
            }
            if (!there)
            {
                if (Game.GameTime - delivery.LastOrder > 3000 && (player.Position.DistanceTo(delivery.LastTarget) > 12f || delivery.Vehicle.Speed < 1f) && delivery.KJ != null && delivery.KJ.Exists())
                {
                    delivery.KJ.Task.DriveTo(delivery.Vehicle, DeliveryTarget(player), 7f, distance < 25f ? 10f : DeliverySpeed, (DrivingStyle)CrewDriving.TrafficFlags);
                    delivery.LastOrder = Game.GameTime; delivery.LastTarget = player.Position;
                }
                return;
            }
            delivery.Arrived = true; delivery.DoneAt = Game.GameTime;
            if (delivery.KJ != null && delivery.KJ.Exists())
            {
                delivery.KJ.Task.LeaveVehicle();
            }
            delivery.Vehicle.IsEngineRunning = true;
            _out[delivery.Car.Id] = delivery.Vehicle;
            if (_outBlips.TryGetValue(delivery.Car.Id, out var blip) && blip != null && blip.Exists()) blip.Name = "Garage: " + delivery.Car.Label;
            GameUtils.Subtitle("~b~KJ:~s~ " + delivery.Car.Label + ", like you asked. Bring it back with all four wheels; I'm not your valet.", 6000);
            Report("Vehicle ready for pickup", delivery.Car.Label + " is at the marked handover position.");
            Logger.Info("KJ drop: the " + delivery.Car.Label + " handed over.");
        }

        /// <summary>A delivery on the road or a car out front is put away before another is asked for.</summary>
        public void RecallDelivery()
        {
            var delivery = _delivery;
            if (delivery != null)
            {
                if (!delivery.Arrived) Report("Delivery recalled", delivery.Car.Label + ". Existing repairs remain paid.");
                if (delivery.KJ != null && delivery.KJ.Exists()) GameUtils.SafeRelease(delivery.KJ);
                if (!delivery.Arrived && delivery.Vehicle != null && delivery.Vehicle.Exists())
                {
                    if (delivery.KJ != null && delivery.KJ.Exists() && delivery.KJ.IsInVehicle(delivery.Vehicle)) ExitVehicleStep.ForceOut(delivery.KJ);
                    if (Occupied(delivery.Vehicle)) TakeOut(delivery.Car, delivery.Vehicle, "Garage: " + delivery.Car.Label);
                    else { Capture(delivery.Vehicle, delivery.Car); GameUtils.SafeDelete(delivery.Vehicle); _state.Save(); }
                }
                if (!IsOut(delivery.Car) && _outBlips.TryGetValue(delivery.Car.Id, out var blip)) { GameUtils.SafeDelete(blip); _outBlips.Remove(delivery.Car.Id); }
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
                if (Occupied(vehicle) || player.Position.DistanceTo(vehicle.Position) <= ReturnDistance) { _awaySince.Remove(id); continue; }
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
            if (Allowed?.Invoke() ?? false) { MaintainDelivery(); MaintainReturns(); }
            if (Game.GameTime >= _nextBuildCapture)
            {
                _nextBuildCapture = Game.GameTime + 5000;
                var current = player.CurrentVehicle;
                var known = _out.FirstOrDefault(pair => pair.Value == current);
                var record = _state.Vehicles.FirstOrDefault(car => car.Id == known.Key);
                if (record != null && Capture(current, record)) _state.Save();
            }
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
            bool changed = false;
            foreach (var pair in _out)
            {
                var vehicle = pair.Value;
                if (vehicle == null || !vehicle.Exists()) continue;
                var car = _state.Vehicles.FirstOrDefault(record => record.Id == pair.Key);
                if (car != null) changed |= Capture(vehicle, car);
                GameUtils.SafeRelease(vehicle);
            }
            if (changed) _state.Save();
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
            if (!model.IsValid || !model.IsInCdImage || (!model.IsCar && !model.IsBike) || model.IsBoat || model.IsPlane || model.IsHelicopter) return null;
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
                mods.Livery = car.Livery;
                if (car.WindowTint >= 0) Function.Call(Hash.SET_VEHICLE_WINDOW_TINT, vehicle, car.WindowTint);
                if (!string.IsNullOrEmpty(car.Plate)) Function.Call(Hash.SET_VEHICLE_NUMBER_PLATE_TEXT, vehicle, car.Plate);
                vehicle.CanTiresBurst = !car.TiresReinforced;
                VehicleFinish.Apply(vehicle, car.Finish, car.Mods);
            }
            catch (Exception ex) { Logger.Error("Garage: the build could not be applied to " + car.Label, ex); }
        }

        /// <summary>Read a car's build into its record. True when something changed.</summary>
        public static bool Capture(Vehicle vehicle, OwnedVehicle car)
        { return TryCapture(vehicle, car, out bool changed) && changed; }
        private static bool TryCapture(Vehicle vehicle, OwnedVehicle car, out bool changed)
        {
            changed = false;
            if (vehicle == null || !vehicle.Exists() || car == null || vehicle.Handle == ShopService.PreviewVehicleHandle) return false;
            string before = car.Fingerprint();
            try
            {
                var mods = vehicle.Mods;
                car.Mods.Clear();
                for(int type=0;type<50;type++)
                {
                    int index=Function.Call<int>(Hash.GET_VEHICLE_MOD,vehicle,type);
                    if(index>=0)car.Mods[type]=index;
                }
                car.PrimaryColor = (int)mods.PrimaryColor;
                car.SecondaryColor = (int)mods.SecondaryColor;
                car.Livery = mods.Livery;
                car.WheelType = Function.Call<int>(Hash.GET_VEHICLE_WHEEL_TYPE, vehicle);
                car.WindowTint = Function.Call<int>(Hash.GET_VEHICLE_WINDOW_TINT, vehicle);
                car.Plate = Function.Call<string>(Hash.GET_VEHICLE_NUMBER_PLATE_TEXT, vehicle) ?? "";
                car.TiresReinforced = !vehicle.CanTiresBurst;
                VehicleFinish.Capture(vehicle, car.Finish);
            }
            catch (Exception ex) { Logger.Error("Garage: the build could not be read from " + car.Label, ex); return false; }
            changed = car.Fingerprint() != before;
            return true;
        }
    }
}
