using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Crew;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>Shared handling entries are adjusted once, never multiplied per car or tick.</summary>
    public sealed class WorldTuning
    {
        public ModConfig Config { get; set; }
        public Nitrous Nitrous { get; } = new Nitrous();
        private readonly HashSet<int> _protected = new HashSet<int>();
        private bool _playerProtected;
        /// <summary>Vehicles currently carrying the crew's damage protection, for tests and the log.</summary>
        public IEnumerable<int> ProtectedVehicles => _protected;

        public WorldTuning(ModConfig config = null)
        {
            Config = config;
        }

        private sealed class Profile
        {
            public HandlingData Data;
            public float Original, Applied;
            public bool Gearing;
            public TravelHandling Travel = new TravelHandling();
            public RoadHandling Road = new RoadHandling();
            public Vehicle Witness;
            public Model Model;
        }
        private readonly Dictionary<IntPtr, Profile> _profiles = new Dictionary<IntPtr, Profile>();
        private readonly Dictionary<int, Vehicle> _cars = new Dictionary<int, Vehicle>();
        private readonly Dictionary<int, float> _stockLimits = new Dictionary<int, float>();
        /// <summary>The raised ceiling each instance normally runs under.</summary>
        private readonly Dictionary<int, float> _appliedLimits = new Dictionary<int, float>();
        /// <summary>The ceiling last written to each instance, so the native is not hammered.</summary>
        private readonly Dictionary<int, float> _ceilings = new Dictionary<int, float>();
        private readonly Dictionary<int, float> _power = new Dictionary<int, float>();
        private readonly Dictionary<int, int> _models = new Dictionary<int, int>();
        private int _nextScan, _lastPowerTime;
        private bool _running;
        /// <summary>
        /// How many cars may take their handling pass in one tick.
        /// Registering a car writes into its model's shared handling entry, so doing
        /// every car in the world at once is a burst of memory writes in a single
        /// frame. The frame the crew deploys is the worst moment for that, because the
        /// game is streaming the new peds and their transport in at the same time.
        /// The work is not reduced, only spread over the frames that follow.
        /// </summary>
        public const int RegistrationsPerTick = 6;
        private readonly Queue<Vehicle> _pending = new Queue<Vehicle>();
        /// <summary>Cars still waiting for their handling pass.</summary>
        public int PendingRegistrations => _pending.Count;
        private readonly VehiclePanelDamage _panels = new VehiclePanelDamage();
        public void Update(CrewRoster crew)
        {
            if (!crew.IsDeployed) { if (_running) Reset(); return; }
            _running = true;
            Function.Call(Hash.SET_RUN_SPRINT_MULTIPLIER_FOR_PLAYER, Game.Player, 1.3f);
            foreach (var hero in Protagonist.All)
            {
                var ped = crew.PedFor(hero.Slot);
                if (ped != null && ped.Exists() && ped.IsAlive && ped != Game.Player.Character)
                    Function.Call(Hash.SET_PED_MOVE_RATE_OVERRIDE, ped, 1.3f);
            }
            // One switch that stops every write to shared handling, so a crash can be
            // pinned to this pass in a single launch: turn it off, repeat what crashed,
            // and the answer is unambiguous. Registration is what torque assistance,
            // crew damage protection and panel dents all hang off, so this turns those
            // off with it. Walking and running speed are unaffected.
            if (Config != null && !Config.HandlingTuningEnabled)
            {
                if (_profiles.Count > 0 || _cars.Count > 0 || _pending.Count > 0) { Reset(); _running = true; }
                return;
            }
            var driven = Game.Player.Character?.CurrentVehicle;
            if (Nitrous.Boosting && driven != null && driven.Exists() && !_cars.ContainsKey(driven.Handle)) Register(driven, crew);
            UpdatePower(crew);
            _panels.Update(_cars.Values, Config);
            Drain(crew);
            // A sweep that is still draining never queues a second copy of itself.
            if (Game.GameTime < _nextScan || _pending.Count > 0) return;
            _nextScan = Game.GameTime + 1000;
            foreach (var key in _cars.Where(p => !p.Value.Exists() || p.Value.Model.Hash!=_models[p.Key]).Select(p => p.Key).ToArray())
            { _cars.Remove(key); _stockLimits.Remove(key); _power.Remove(key); _models.Remove(key); }
            foreach (var car in World.GetAllVehicles())
                if (car != null && car.Exists() && !car.IsDead) _pending.Enqueue(car);
            Drain(crew);
        }

        /// <summary>
        /// Take the next few cars off the queue and register them. Cars that died or
        /// despawned while queued cost nothing and do not use up the budget, but the
        /// number of entries looked at in one tick is bounded either way.
        /// </summary>
        private void Drain(CrewRoster crew)
        {
            int registered = 0, examined = 0;
            while (_pending.Count > 0 && registered < RegistrationsPerTick && examined < RegistrationsPerTick * 8)
            {
                examined++;
                var car = _pending.Dequeue();
                if (car == null || !car.Exists() || car.IsDead) continue;
                registered++;
                try { Register(car, crew); }
                catch (Exception ex) { Logger.Error("Vehicle travel tuning", ex); }
            }
        }

        private static bool IsCrewVehicle(Vehicle car, CrewRoster crew)
        {
            if (car == null || !car.Exists()) return false;
            var player = Game.Player.Character;
            if (player != null && player.Exists() && player.IsInVehicle() && player.CurrentVehicle == car) return true;
            if (crew != null && crew.IsDeployed)
            {
                foreach (var hero in Protagonist.All)
                {
                    var ped = crew.PedFor(hero.Slot);
                    if (ped != null && ped.Exists() && ped.IsInVehicle() && ped.CurrentVehicle == car) return true;
                }
            }
            return false;
        }

        private void Register(Vehicle car, CrewRoster crew)
        {
                // Helicopters fly stock (Ron, September 10: the tuned rotorcraft was
                // "just horrible"). No ceiling, no cruise force, no handling edits.
                if (car.Model.IsHelicopter) return;
                var handling = car.HandlingData;
                if (handling == null || !handling.IsValid) return;
                var address = handling.MemoryAddress;
                if (!_profiles.TryGetValue(address, out var profile))
                {
                    bool gearing=!car.Model.IsPlane&&!car.Model.IsHelicopter&&!car.Model.IsBoat&&!car.Model.IsSubmarine&&!car.Model.IsTrain;
                    float original=gearing?handling.InitialDriveMaxFlatVelocity:Function.Call<float>(Hash.GET_VEHICLE_MODEL_ESTIMATED_MAX_SPEED,car.Model.Hash);
                    if (float.IsNaN(original) || float.IsInfinity(original) || original < 1f || original > 400f || _profiles.Count >= 1024) return;
                    profile = new Profile { Data = handling, Original = original, Applied = original * 2f, Gearing=gearing, Witness = car, Model = car.Model };
                    _profiles[address] = profile;
                    if(gearing)handling.InitialDriveMaxFlatVelocity = profile.Applied;
                    profile.Travel.Apply(handling,car.Model);
                    if (car.Model.IsPlane || car.Model.IsHelicopter || car.Model.IsBoat) Logger.Info(car.DisplayName + ": " + profile.Travel.Report);
                    // The grip, damping, weight, brakes and realistic deformation the doubled gearing needs.
                    if(gearing)profile.Road.Apply(handling, car, Config);
                }
                else
                {
                    // Another handling mod or a recycled address owns this value now.
                    if (profile.Gearing && Math.Abs(handling.InitialDriveMaxFlatVelocity - profile.Applied) > .01f) return;
                    profile.Witness = car;
                }
                if (_cars.ContainsKey(car.Handle)) return;
                // MODIFY_VEHICLE_TOP_SPEED (the SDK's EnginePowerMultiplier setter)
                // rebuilds cached transmission data after handling edits. Zero
                // refreshes the gearbox without a percentage boost to power/drag.
                if(profile.Gearing)car.EnginePowerMultiplier = 0f;
                // Remove the entity speed ceiling too; this does not add engine force.
                Function.Call(Hash.SET_VEHICLE_MAX_SPEED, car, profile.Applied);
                _cars[car.Handle] = car;
                _stockLimits[car.Handle] = profile.Original;
                // The ceiling this instance is actually running under. Nitrous raises it
                // while the bottle is open and puts it straight back, and both writes need
                // this number to work from.
                _appliedLimits[car.Handle] = profile.Applied;
                _ceilings[car.Handle] = profile.Applied;
                _power[car.Handle] = 1f;
                _models[car.Handle] = car.Model.Hash;
        }
        /// <summary>
        /// The entity speed cap for one car, raised while its bottle is open.
        ///
        /// The boost used to be torque and nothing else, and the cap was the same number
        /// open or shut - so whatever the extra torque was worth, the car met the same
        /// ceiling either way, which is exactly what "it only feels like acceleration"
        /// describes. This is per instance, never the shared handling data, and it is
        /// written only when the number actually changes.
        /// </summary>
        private void ApplyCeiling(Vehicle car, bool boosting)
        {
            if (!_appliedLimits.TryGetValue(car.Handle, out float applied) || applied <= 0f) return;
            // A final drive raises this car's own ceiling and nothing else's. It multiplies
            // the per-instance limit, never InitialDriveMaxFlatVelocity, which is shared by
            // every car of the model including the ones traffic spawns.
            applied *= VehicleStages.DriveFactor(VehicleStages.FittedOn(car, VehicleStages.Stage.FinalDrive));
            float want = boosting ? applied * Nitrous.SpeedMultiplier : applied;
            if (_ceilings.TryGetValue(car.Handle, out float current) && Math.Abs(current - want) < .01f) return;
            _ceilings[car.Handle] = want;
            Function.Call(Hash.SET_VEHICLE_MAX_SPEED, car, want);
        }

        public static float PowerTarget(float forwardSpeed, float stockLimit)
        {
            if (stockLimit <= 0f || float.IsNaN(stockLimit) || float.IsInfinity(stockLimit) || float.IsNaN(forwardSpeed) || float.IsInfinity(forwardSpeed)) return 1f;
            // Ordinary launch; smoothly feed extra torque in from 25% to 100%
            // of the original redline speed. No stepped turbo/nitrous kick.
            float t = Math.Max(0f, Math.Min(1f, (forwardSpeed / stockLimit - .25f) / .75f));
            return 1f + .8f * t * t * (3f - 2f * t);
        }
        public static float RampPower(float current, float target, float seconds)
        {
            // Back off immediately when braking/launching again. Increases take
            // at least four seconds from neutral, even after switching into a fast car.
            if (target <= current) return target;
            return Math.Min(target, current + .2f * Math.Max(0f, Math.Min(.1f, seconds)));
        }
        public static bool CanAssist(Vehicle vehicle)
        {
            if(vehicle.IsDead||!vehicle.IsDriveable||(!vehicle.IsEngineRunning&&!vehicle.Model.IsBicycle))return false;
            if(vehicle.Model.IsPlane||vehicle.Model.IsHelicopter)return vehicle.IsInAir;
            if(vehicle.Model.IsBoat||vehicle.Model.IsSubmarine)return Function.Call<bool>(Hash.IS_ENTITY_IN_WATER,vehicle);
            return !vehicle.IsInAir;
        }
        private void UpdatePower(CrewRoster crew)
        {
            if (Game.GameTime - _lastPowerTime > 250)
                foreach (var key in _power.Keys.ToArray()) _power[key] = 1f;
            _lastPowerTime = Game.GameTime;
            foreach (var pair in _cars)
            {
                var car = pair.Value;
                if (!car.Exists() || car.Model.Hash!=_models[pair.Key]) continue;
                UpdateProtection(car, crew);
                float target = 1f;
                if (CanAssist(car))
                {
                    var velocity = car.Velocity; var forward = car.ForwardVector;
                    float speed = velocity.X * forward.X + velocity.Y * forward.Y + velocity.Z * forward.Z;
                    // Cruise assistance only: aircraft retain normal taxi,
                    // takeoff and hover thrust. Boats must actually be in water.
                    float limit=_stockLimits[pair.Key];
                    bool flight=car.Model.IsPlane||car.Model.IsHelicopter;
                    target = PowerTarget(flight?Math.Max(0f,speed-limit*.3f):speed, flight?limit*.7f:limit);
                }
                float power = RampPower(_power[pair.Key], target, Game.LastFrameTime);
                _power[pair.Key] = power;
                Function.Call(Hash.SET_VEHICLE_CHEAT_POWER_INCREASE, car,
                    power * Nitrous.MultiplierFor(car) *
                    VehicleStages.PowerFactor(VehicleStages.FittedOn(car, VehicleStages.Stage.Engine)));
                ApplyCeiling(car, Nitrous.MultiplierFor(car) > 1f);
            }
        }
        /// <summary>
        /// The crew's damage protection, per vehicle instance: applied when a brother
        /// is aboard, removed the moment the car is empty of them, and the player's
        /// own modifier follows the car the player is in. Nothing here touches the
        /// model's shared handling.
        /// </summary>
        private void UpdateProtection(Vehicle car, CrewRoster crew)
        {
            bool enabled = Config != null && Config.VehicleDamageEnabled;
            bool occupied = enabled && IsCrewVehicle(car, crew);
            if (occupied && !_protected.Contains(car.Handle))
            {
                Function.Call(Hash.SET_VEHICLE_DAMAGE_SCALE, car, Config.CrewProtectionMultiplier);
                _protected.Add(car.Handle);
            }
            else if (!occupied && _protected.Contains(car.Handle))
            {
                Function.Call(Hash.SET_VEHICLE_DAMAGE_SCALE, car, 1f);
                _protected.Remove(car.Handle);
            }
            var player = Game.Player.Character;
            bool playerAboard = occupied && player != null && player.Exists() && player.CurrentVehicle == car;
            if (playerAboard && !_playerProtected)
            {
                Function.Call(Hash.SET_PLAYER_VEHICLE_DAMAGE_MODIFIER, Game.Player, Config.CrewProtectionMultiplier);
                _playerProtected = true;
            }
            else if (_playerProtected && !playerAboard && (player == null || !player.Exists() || player.CurrentVehicle == null || !_protected.Contains(player.CurrentVehicle.Handle)))
            {
                Function.Call(Hash.SET_PLAYER_VEHICLE_DAMAGE_MODIFIER, Game.Player, 1f);
                _playerProtected = false;
            }
        }

        public void Reset()
        {
            Nitrous.Reset();
            _panels.Reset();
            Function.Call(Hash.SET_RUN_SPRINT_MULTIPLIER_FOR_PLAYER, Game.Player, 1f);
            if (_playerProtected) { Function.Call(Hash.SET_PLAYER_VEHICLE_DAMAGE_MODIFIER, Game.Player, 1f); _playerProtected = false; }
            foreach (var car in _cars.Values)
                if (car.Exists() && _protected.Contains(car.Handle)) Function.Call(Hash.SET_VEHICLE_DAMAGE_SCALE, car, 1f);
            _protected.Clear();
            var restored = new List<IntPtr>();
            foreach (var pair in _profiles)
                try
                {
                    var profile = pair.Value;
                    var witness = profile.Witness;
                    // Re-resolve through the live model table if its last vehicle
                    // despawned. Never dereference an orphan's stored address.
                    var live = witness != null && witness.Exists() ? witness.HandlingData : HandlingData.GetByVehicleModel(profile.Model);
                    if (live != null && live.IsValid && live.MemoryAddress == profile.Data.MemoryAddress)
                    {
                        if(profile.Gearing && Math.Abs(live.InitialDriveMaxFlatVelocity-profile.Applied)<.01f)
                            live.InitialDriveMaxFlatVelocity=profile.Original;
                        bool roadRestored = profile.Road.Restore(live);
                        if(profile.Travel.Restore(live) && roadRestored)restored.Add(pair.Key);
                    }
                }
                catch (Exception ex) { Logger.Error("Restore car gearing", ex); }
            foreach (var car in _cars.Values)
                try
                {
                    if (!car.Exists()) continue;
                    Function.Call(Hash.SET_VEHICLE_CHEAT_POWER_INCREASE, car, 1f);
                    var live = car.HandlingData;
                    if (live != null && live.IsValid && restored.Contains(live.MemoryAddress) &&
                        _profiles.TryGetValue(live.MemoryAddress,out var profile) && profile.Gearing) car.EnginePowerMultiplier = 0f;
                    Function.Call(Hash.SET_VEHICLE_MAX_SPEED, car, 0f);
                }
                catch (Exception ex) { Logger.Error("Restore car speed ceiling", ex); }
            foreach (var key in restored) _profiles.Remove(key);
            // Retain orphan baselines across stand-down. The shared handling may
            // outlive the last car, and must not be doubled again on redeployment.
            _pending.Clear();
            _cars.Clear(); _stockLimits.Clear(); _power.Clear(); _models.Clear(); _running = false; _nextScan = 0; _lastPowerTime = 0;
        }
    }
}
