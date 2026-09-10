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
        public int PrimaryColor = -1, SecondaryColor = -1, Livery = -1;
        public bool TiresReinforced;
        public readonly Dictionary<int, int> Mods = new Dictionary<int, int>();

        public Dictionary<string, object> ToJson() => new Dictionary<string, object>
        {
            { "model", Model }, { "primaryColor", PrimaryColor }, { "secondaryColor", SecondaryColor }, { "livery", Livery },
            { "tiresReinforced", TiresReinforced },
            { "mods", Mods.OrderBy(p => p.Key).ToDictionary(p => p.Key.ToString(), p => (object)p.Value) }
        };

        public void FromJson(Dictionary<string, object> map)
        {
            if (map == null) return;
            Model = Json.String(map, "model", Model);
            PrimaryColor = Json.Int(map, "primaryColor", PrimaryColor);
            SecondaryColor = Json.Int(map, "secondaryColor", SecondaryColor);
            Livery = Json.Int(map, "livery", Livery);
            TiresReinforced = Json.Bool(map, "tiresReinforced", TiresReinforced);
            Mods.Clear();
            foreach (var pair in Json.Object(map.TryGetValue("mods", out var mods) ? mods : null))
                if (int.TryParse(pair.Key, out int slot) && pair.Value != null && int.TryParse(pair.Value.ToString(), out int index)) Mods[slot] = index;
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
            van.PlaceOnGround();
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
            }
            catch (Exception ex) { Logger.Error("Crew van customization could not be applied", ex); }
        }

        /// <summary>Read a vehicle's customization into the save. True when something changed.</summary>
        public bool Capture(Vehicle van)
        {
            if (van == null || !van.Exists()) return false;
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
