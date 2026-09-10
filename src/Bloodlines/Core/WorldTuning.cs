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
        private readonly Dictionary<int, float> _power = new Dictionary<int, float>();
        private readonly Dictionary<int, int> _models = new Dictionary<int, int>();
        private int _nextScan, _lastPowerTime;
        private bool _running;
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
            UpdatePower();
            if (Game.GameTime < _nextScan) return;
            _nextScan = Game.GameTime + 1000;
            foreach (var key in _cars.Where(p => !p.Value.Exists() || p.Value.Model.Hash!=_models[p.Key]).Select(p => p.Key).ToArray())
            { _cars.Remove(key); _stockLimits.Remove(key); _power.Remove(key); _models.Remove(key); }
            foreach (var car in World.GetAllVehicles())
            {
                if (car == null || !car.Exists() || car.IsDead) continue;
                try { Register(car); }
                catch (Exception ex) { Logger.Error("Vehicle travel tuning",ex); }
            }
        }
        private void Register(Vehicle car)
        {
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
                    // The grip, damping, weight and brakes the doubled gearing needs.
                    if(gearing)profile.Road.Apply(handling,car);
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
                _power[car.Handle] = 1f;
                _models[car.Handle] = car.Model.Hash;
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
        /// <summary>Helicopter cruise assist, m/s² along the nose. Zero until 8 m/s; capped at 3.</summary>
        public const float HeliCruiseCap = 3f;
        public static float HeliCruise(float forwardSpeed, float power)
        {
            if (forwardSpeed < 8f || float.IsNaN(forwardSpeed) || power <= 1f) return 0f;
            // Follow the same ramp the road torque uses: 1.0..1.8 maps to 0..cap.
            return HeliCruiseCap * Math.Max(0f, Math.Min(1f, (power - 1f) / .8f));
        }

        public static bool CanAssist(Vehicle vehicle)
        {
            if(vehicle.IsDead||!vehicle.IsDriveable||(!vehicle.IsEngineRunning&&!vehicle.Model.IsBicycle))return false;
            if(vehicle.Model.IsPlane||vehicle.Model.IsHelicopter)return vehicle.IsInAir;
            if(vehicle.Model.IsBoat||vehicle.Model.IsSubmarine)return Function.Call<bool>(Hash.IS_ENTITY_IN_WATER,vehicle);
            return !vehicle.IsInAir;
        }
        private void UpdatePower()
        {
            if (Game.GameTime - _lastPowerTime > 250)
                foreach (var key in _power.Keys.ToArray()) _power[key] = 1f;
            _lastPowerTime = Game.GameTime;
            foreach (var pair in _cars)
            {
                var car = pair.Value;
                if (!car.Exists() || car.Model.Hash!=_models[pair.Key]) continue;
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
                Function.Call(Hash.SET_VEHICLE_CHEAT_POWER_INCREASE, car, power);
                // Rotors do not answer the torque native. A helicopter in forward
                // flight gets a bounded push along its nose instead; hover, takeoff
                // and landing sit below the speed floor and get nothing.
                if (car.Model.IsHelicopter && CanAssist(car))
                {
                    var velocity = car.Velocity; var forward = car.ForwardVector;
                    float forwardSpeed = velocity.X * forward.X + velocity.Y * forward.Y + velocity.Z * forward.Z;
                    float assist = HeliCruise(forwardSpeed, power);
                    if (assist > 0.01f) car.ApplyForce(forward * assist, GTA.Math.Vector3.Zero, ForceType.MaxForceRot2);
                }
            }
        }
        public void Reset()
        {
            Function.Call(Hash.SET_RUN_SPRINT_MULTIPLIER_FOR_PLAYER, Game.Player, 1f);
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
            _cars.Clear(); _stockLimits.Clear(); _power.Clear(); _models.Clear(); _running = false; _nextScan = 0; _lastPowerTime = 0;
        }
    }
}
