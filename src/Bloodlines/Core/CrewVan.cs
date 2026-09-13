using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// What the crew has done to their Granger: mods by slot, paint, livery and the
    /// reinforced tires. Saved with the campaign; applied to every van the game
    /// creates, in free roam at the stash or inside a mission that needs it.
    /// </summary>
    public sealed class CrewVanRecord
    {
        public string Model = "granger";
        public readonly Dictionary<string, OwnedVehicle> Fleet = new Dictionary<string, OwnedVehicle>(StringComparer.OrdinalIgnoreCase);
        public OwnedVehicle Build = new OwnedVehicle();
        public int PrimaryColor = -1, SecondaryColor = -1, Livery = -1;
        public bool TiresReinforced;
        public readonly Dictionary<int, int> Mods = new Dictionary<int, int>();

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "fleet", Fleet.ToDictionary(p=>p.Key,p=>(object)p.Value.ToJson()) }, { "build", Build.ToJson() },
            { "model", Model }, { "primaryColor", PrimaryColor }, { "secondaryColor", SecondaryColor }, { "livery", Livery },
            { "tiresReinforced", TiresReinforced },
            { "mods", Mods.OrderBy(p => p.Key).ToDictionary(p => p.Key.ToString(), p => (object)p.Value) }
        };

        public void FromJson(Dictionary<string, object> map)
        {
            if (map == null) return;
            Model = Json.String(map, "model", Model);
            Build=OwnedVehicle.FromJson(Json.Object(map.TryGetValue("build",out var build)?build:null))??new OwnedVehicle();
            Fleet.Clear();foreach(var pair in Json.Object(map.TryGetValue("fleet",out var fleet)?fleet:null)){var car=OwnedVehicle.FromJson(Json.Object(pair.Value));if(car!=null)Fleet[pair.Key]=car;}
            PrimaryColor = Json.Int(map, "primaryColor", PrimaryColor);
            SecondaryColor = Json.Int(map, "secondaryColor", SecondaryColor);
            Livery = Json.Int(map, "livery", Livery);
            TiresReinforced = Json.Bool(map, "tiresReinforced", TiresReinforced);
            Mods.Clear();
            foreach (var pair in Json.Object(map.TryGetValue("mods", out var mods) ? mods : null))
                if (int.TryParse(pair.Key, out int slot) && pair.Value != null && int.TryParse(pair.Value.ToString(), out int index)) Mods[slot] = index;
        }

        public void Reset()
        {
            Model = "granger"; PrimaryColor = SecondaryColor = Livery = -1;
            TiresReinforced = false; Mods.Clear(); Fleet.Clear(); Build=new OwnedVehicle();
        }

        public string Fingerprint() => Json.Write(ToJson());
    }

    /// <summary>
    /// The crew's own Granger. It lives at the stash beside the Cypress base, shows
    /// up there whenever the crew is deployed in free roam, and is the vehicle a
    /// mission spawns when the story says "the crew's Granger". Anything done to
    /// it at a shop is captured into the campaign save and comes back on the next
    /// van the game creates. The M11 fleet package still applies on top through
    /// <see cref="FleetGarage"/>; this class owns the customization, not the reward.
    /// </summary>
    public sealed class CrewVan
    {
        public sealed class Choice
        {
            public readonly string Model,Name; public readonly int Price;
            public Choice(string model,string name,int price){Model=model;Name=name;Price=price;}
        }
        public static readonly Choice[] FleetChoices={
            new Choice("granger","Granger",0),new Choice("baller2","Baller",40000),
            new Choice("schafter2","Schafter",38000),new Choice("kuruma2","Armored Kuruma",80000),
            new Choice("buffalo4","Buffalo STX",100000),new Choice("jubilee","Jubilee",90000),
            new Choice("nightshark","Nightshark",110000),new Choice("insurgent2","Insurgent",120000)};
        private static OwnedVehicle Clone(OwnedVehicle source)
        {
            var copy=new OwnedVehicle{PrimaryColor=source.PrimaryColor,SecondaryColor=source.SecondaryColor,Livery=source.Livery,
                WheelType=source.WheelType,WindowTint=source.WindowTint,Plate=source.Plate,TiresReinforced=source.TiresReinforced};
            foreach(var pair in source.Mods)copy.Mods[pair.Key]=pair.Value;
            foreach(var pair in source.Finish)copy.Finish[pair.Key]=pair.Value;
            return copy;
        }
        public bool Owns(string model)=>model==Record.Model||model=="granger"||Record.Fleet.ContainsKey(model);
        public Action<string, int> Purchased;
        public bool Select(Choice choice,bool atHideout)
        {
            var player=Game.Player.Character;
            if(!atHideout||!_state.IsUnlocked("cypressFoundry")||Game.Player.WantedLevel!=0||player==null||player.IsDead||
                choice==null||!FleetChoices.Contains(choice)||ShopService.PreviewVehicleHandle!=0)return false;
            if(choice.Model==Record.Model)return false;
            // Never delete a vehicle with a brother or another occupant still in it.
            if(Current!=null&&Current.Occupants.Length>0){GameUtils.Notify("Park the crew car and let everyone out before replacing it.");return false;}
            var model=new Model(choice.Model);
            if(!model.IsValid||!model.IsInCdImage||!model.IsCar||!GameUtils.RequestModel(model,2500))return false;
            bool fits=Function.Call<int>(Hash.GET_VEHICLE_MODEL_NUMBER_OF_SEATS,model.Hash)>=4;
            model.MarkAsNoLongerNeeded();if(!fits){GameUtils.Notify("That vehicle cannot seat the crew.");return false;}
            int price=Owns(choice.Model)?0:choice.Price;
            if(_state.CashOnHand<price){GameUtils.Notify("Need $"+price+" crew cash.");return false;}
            if(Current!=null)Capture(Current);
            Record.Fleet[Record.Model]=Clone(Record.Build);
            // Keep legacy saves' Granger customization on their first fleet change.
            var stored=Record.Fleet[Record.Model];stored.PrimaryColor=Record.PrimaryColor;stored.SecondaryColor=Record.SecondaryColor;
            stored.Livery=Record.Livery;stored.TiresReinforced=Record.TiresReinforced;
            foreach(var pair in Record.Mods)stored.Mods[pair.Key]=pair.Value;
            var next=Record.Fleet.TryGetValue(choice.Model,out var saved)?Clone(saved):new OwnedVehicle();
            Record.Model=choice.Model;Record.Build=next;
            Record.PrimaryColor=next.PrimaryColor;Record.SecondaryColor=next.SecondaryColor;Record.Livery=next.Livery;Record.TiresReinforced=next.TiresReinforced;
            Record.Mods.Clear();foreach(var pair in next.Mods)Record.Mods[pair.Key]=pair.Value;
            Record.Fleet[choice.Model]=next;_state.CashOnHand-=price;_state.Save();
            if(price>0)try{Purchased?.Invoke(choice.Name,price);}catch(Exception ex){Logger.Warn("Fleet receipt: "+ex.Message);}
            GameUtils.SafeDelete(_blip);_blip=null;GameUtils.SafeDelete(_van);_van=null;_wasAboard=false;
            GameUtils.Notify(choice.Name+" is now the crew car. It will be parked outside and used for crew-car mission scenes.");return true;
        }
        public const string StashKey = "Stash.CrewVan";
        public const float SpawnRadius = 220f;

        private readonly CampaignState _state;
        private readonly LocationBook _locations;
        private Vehicle _van;
        private Blip _blip;
        private bool _wasAboard;
        private int _nextCapture;

        public CrewVan(CampaignState state, LocationBook locations) { _state = state; _locations = locations; }

        public Vehicle Current => _van != null && _van.Exists() ? _van : null;
        public CrewVanRecord Record => _state.CrewVan;
        public bool IsVan(Vehicle vehicle) => vehicle != null && _van != null && vehicle.Handle == _van.Handle;

        public Vector3? StashPosition
        {
            get
            {
                var stash = _locations.Get(StashKey) ?? _locations.Get("Base.CypressFlats");
                return stash?.Position;
            }
        }

        /// <summary>Free roam: keep one van at the stash while the crew is out; capture changes when the player gets out of it.</summary>
        public void Update(CrewRoster crew, bool available)
        {
            if (!crew.IsDeployed) return;
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            if (_van != null && !_van.Exists()) { GameUtils.SafeDelete(_blip); _blip = null; _van = null; }

            if (_van != null)
            {
                bool aboard = player.IsInVehicle(_van);
                if (_wasAboard && !aboard && Game.GameTime >= _nextCapture) { Capture(_van); _nextCapture = Game.GameTime + 2000; }
                _wasAboard = aboard;
                return;
            }
            if (!available) return;
            var stash = StashPosition;
            if (!stash.HasValue || !GameUtils.IsWithinFlat(player.Position, stash.Value, SpawnRadius)) return;
            var heading = _locations.Get(StashKey)?.Heading ?? 0f;
            var van = Create(stash.Value, heading);
            if (van == null) return;
            van.IsEngineRunning = false;
            _blip = van.AddBlip();
            if (_blip != null) { _blip.Sprite = BlipSprite.PersonalVehicleCar; _blip.Color = BlipColor.Blue; _blip.Name = "Crew van"; }
            Logger.Info("Crew van parked at the stash " + stash.Value + ".");
        }

        /// <summary>A mission needs the van: create it where the mission wants it, with everything the crew has done to it.</summary>
        public Vehicle Spawn(Vector3 position, float heading)
        {
            if (_van != null && _van.Exists()) { GameUtils.SafeDelete(_blip); _blip = null; GameUtils.SafeDelete(_van); _van = null; }
            var van = Create(position, heading);
            if (van != null) van.IsEngineRunning = true;
            return van;
        }

        private Vehicle Create(Vector3 position, float heading)
        {
            var model = new Model(Record.Model);
            if (!model.IsValid || !GameUtils.RequestModel(model, 2000)) { Logger.Warn("Crew van model unavailable: " + Record.Model); return null; }
            var van = World.CreateVehicle(model, position, heading);
            model.MarkAsNoLongerNeeded();
            if (van == null || !van.Exists()) return null;
            van.IsPersistent = true;
            GameUtils.HoldUntilGrounded(van);
            Apply(van);
            _van = van;
            _wasAboard = false;
            return van;
        }

        /// <summary>Put the saved customization on a vehicle.</summary>
        public void Apply(Vehicle van)
        {
            if (van == null || !van.Exists()) return;
            var record = Record;
            try
            {
                var mods = van.Mods;
                mods.InstallModKit();
                foreach (var pair in record.Mods)
                {
                    var mod = mods[(VehicleModType)pair.Key];
                    if (pair.Value >= -1 && pair.Value < mod.Count) mod.Index = pair.Value;
                }
                if (record.PrimaryColor >= 0) mods.PrimaryColor = (VehicleColor)record.PrimaryColor;
                if (record.SecondaryColor >= 0) mods.SecondaryColor = (VehicleColor)record.SecondaryColor;
                if (record.Livery >= 0) mods.Livery = record.Livery;
                if (record.TiresReinforced) van.CanTiresBurst = false;
                if(record.Build.Finish.Count>0)GarageService.Apply(van,record.Build);
            }
            catch (Exception ex) { Logger.Error("Crew van customization could not be applied", ex); }
        }

        /// <summary>Read a vehicle's customization into the save. True when something changed.</summary>
        public bool Capture(Vehicle van)
        {
            if (van == null || !van.Exists() || van.Handle==ShopService.PreviewVehicleHandle) return false;
            var record = Record;
            string before = record.Fingerprint();
            try
            {
                var mods = van.Mods;
                record.Mods.Clear();
                foreach (VehicleModType type in Enum.GetValues(typeof(VehicleModType)))
                {
                    int index = mods[type].Index;
                    if (index >= 0) record.Mods[(int)type] = index;
                }
                record.PrimaryColor = (int)mods.PrimaryColor;
                record.SecondaryColor = (int)mods.SecondaryColor;
                record.Livery = mods.Livery;
                record.TiresReinforced = !van.CanTiresBurst;
                GarageService.Capture(van,record.Build);
                record.Mods.Clear();foreach(var pair in record.Build.Mods)record.Mods[pair.Key]=pair.Value;
            }
            catch (Exception ex) { Logger.Error("Crew van customization could not be read", ex); return false; }
            if (record.Fingerprint() == before) return false;
            _state.Save();
            Logger.Info("Crew van customization saved (" + record.Mods.Count + " mods, colors " + record.PrimaryColor + "/" + record.SecondaryColor + ").");
            return true;
        }

        /// <summary>Stand-down and teardown: the van stays in the world but is no longer ours to track.</summary>
        public void Release()
        {
            GameUtils.SafeDelete(_blip); _blip = null;
            if (_van != null && _van.Exists()) GameUtils.SafeRelease(_van);
            _van = null; _wasAboard = false;
        }
    }
}
