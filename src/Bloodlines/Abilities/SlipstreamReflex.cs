using System;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Abilities
{
    /// <summary>
    /// Guess — "Slipstream Reflex". Franklin-inspired driving control: time slows,
    /// and the car he is driving becomes planted and precise. The baseline road
    /// profile (<see cref="RoadHandling"/>) makes every car drivable at the faster
    /// world's speeds; this is the extra layer on top, on his car only.
    ///
    /// Two overlays while he is the driver of a road vehicle:
    ///  - a bounded press toward the road on that vehicle instance while it is on
    ///    its wheels: a constant quarter g of weight plus downforce that grows with
    ///    speed squared to a 0.6 g cap (the SDK has no per-vehicle gravity, so the
    ///    weight is applied as force through the same bounded channel);
    ///  - a temporary grip lift on the model's shared handling, restored on exit.
    /// The press blends in over half a second and blends out over half a second
    /// after the ability ends, so ending it mid-corner does not drop the car.
    /// Nothing assigns velocity, snaps heading or pins an airborne car; ramps and
    /// jumps behave as they would without the ability.
    ///
    /// The shared grip lift is per model, so a same-model car nearby grips better
    /// for the duration. That is the stated tradeoff: there is no per-instance
    /// traction control in this SDK, and without it steering would not change.
    /// </summary>
    public sealed class SlipstreamReflex : Ability
    {
        public const float TimeScale = 0.45f;
        /// <summary>Constant weight press in m/s² (0.25 g) while on the wheels.</summary>
        public const float WeightPress = 2.45f;
        /// <summary>Downforce cap in m/s² (0.6 g), reached at <see cref="DownforceReferenceSpeed"/>.</summary>
        public const float DownforceCap = 5.9f;
        public const float DownforceReferenceSpeed = 50f;
        public const float GripLift = 1.50f;
        /// <summary>The steering lock widens while the ability runs: the car turns in the way Franklin's does (Ron, September 12).</summary>
        public const float SteeringLift = 1.30f;
        private const float BlendPerSecond = 2f; // half a second each way

        private Vehicle _vehicle;
        private bool _tiresCouldBurst;
        private float _blend;
        private int _lastTick;
        private bool _settling;
        private readonly GripOverlay _grip = new GripOverlay();

        public override CrewSlot Slot => CrewSlot.Guess;
        public override string Name => "Slipstream Reflex";

        /// <summary>Current strength of the press, 0..1, for tests and the dev readout.</summary>
        public float Blend => _blend;
        public Vehicle Vehicle => _vehicle;

        public override void Activate(Ped player)
        {
            Function.Call(Hash.SET_TIME_SCALE, TimeScale);
            Function.Call(Hash.ANIMPOSTFX_PLAY, "RaceTurbo", 0, false);
            _settling = false;
            _lastTick = Game.GameTime;
        }

        public override void Update(Ped player)
        {
            var vehicle = player?.CurrentVehicle;
            if (_vehicle != null && (vehicle == null || vehicle.Handle != _vehicle.Handle)) RestoreVehicle();
            if (!Supported(vehicle, player)) return;
            if (_vehicle == null) Acquire(vehicle);
            Step(vehicle, targetBlend: Grounded(vehicle) ? 1f : 0f);

            // Per-frame cheats: dropped the moment the ability ends.
            Function.Call(Hash.SET_VEHICLE_REDUCE_GRIP, vehicle, false);
            vehicle.CanTiresBurst = false;
        }

        public override void Deactivate(Ped player)
        {
            Function.Call(Hash.SET_TIME_SCALE, 1.0f);
            Function.Call(Hash.ANIMPOSTFX_STOP, "RaceTurbo");
            _grip.Restore();
            if (_vehicle != null && _vehicle.Exists())
            {
                _vehicle.CanTiresBurst = _tiresCouldBurst;
                // The press comes off over the next half second in Settle, not on
                // this frame; a corner is the usual moment the meter runs out.
                _settling = true;
                _lastTick = Game.GameTime;
                return;
            }
            RestoreVehicle();
        }

        /// <summary>Wind the press down after deactivation. True while still settling.</summary>
        public override bool Settle(Ped player)
        {
            if (!_settling) return false;
            if (_vehicle == null || !_vehicle.Exists() || player == null || !player.IsInVehicle(_vehicle)) { RestoreVehicle(); return false; }
            Step(_vehicle, targetBlend: 0f);
            if (_blend > 0.001f) return true;
            RestoreVehicle();
            return false;
        }

        private static bool Supported(Vehicle vehicle, Ped player)
        {
            if (vehicle == null || !vehicle.Exists() || player == null) return false;
            if (vehicle.GetPedOnSeat(VehicleSeat.Driver) != player) return false;
            var model = vehicle.Model;
            return !(model.IsPlane || model.IsHelicopter || model.IsBoat || model.IsSubmarine || model.IsTrain || model.IsBicycle);
        }

        public static bool Grounded(Vehicle vehicle) => vehicle != null && vehicle.Exists() && !vehicle.IsInAir && vehicle.IsOnAllWheels;

        private void Acquire(Vehicle vehicle)
        {
            _vehicle = vehicle;
            _tiresCouldBurst = vehicle.CanTiresBurst;
            _blend = 0f;
            _lastTick = Game.GameTime;
            _grip.Apply(vehicle);
        }

        private void Step(Vehicle vehicle, float targetBlend)
        {
            int now = Game.GameTime;
            float seconds = Math.Max(0f, Math.Min(0.1f, (now - _lastTick) / 1000f));
            _lastTick = now;
            float step = BlendPerSecond * seconds;
            _blend = targetBlend > _blend ? Math.Min(targetBlend, _blend + step) : Math.Max(targetBlend, _blend - step);

            float accel = Press(ForwardSpeed(vehicle)) * _blend;
            if (accel > 0.01f) vehicle.ApplyForce(new Vector3(0f, 0f, -accel), Vector3.Zero, ForceType.MaxForceRot2);
        }

        /// <summary>Total press in m/s² on the wheels at a forward speed: the weight term plus downforce.</summary>
        public static float Press(float forwardSpeed) => WeightPress + Downforce(forwardSpeed);

        /// <summary>Downforce in m/s² for a forward speed: speed-squared up to the cap.</summary>
        public static float Downforce(float forwardSpeed)
        {
            if (forwardSpeed <= 0f || float.IsNaN(forwardSpeed)) return 0f;
            float t = forwardSpeed / DownforceReferenceSpeed;
            return Math.Min(DownforceCap, DownforceCap * t * t);
        }

        private static float ForwardSpeed(Vehicle vehicle)
        {
            var v = vehicle.Velocity; var f = vehicle.ForwardVector;
            return v.X * f.X + v.Y * f.Y + v.Z * f.Z;
        }

        private void RestoreVehicle()
        {
            _grip.Restore();
            if (_vehicle != null && _vehicle.Exists()) _vehicle.CanTiresBurst = _tiresCouldBurst;
            _vehicle = null;
            _blend = 0f;
            _settling = false;
        }

        /// <summary>The shared-handling grip lift, restored only where the value is still ours.</summary>
        private sealed class GripOverlay
        {
            private HandlingData _data;
            private float _maxOriginal, _maxApplied, _lateralOriginal, _lateralApplied, _steerOriginal, _steerApplied;
            public bool Active { get; private set; }

            public void Apply(Vehicle vehicle)
            {
                var data = vehicle.HandlingData;
                if (Active || data == null || !data.IsValid) return;
                float max = data.TractionCurveMax, lateral = data.TractionCurveLateral;
                if (max <= 0f || float.IsNaN(max) || lateral <= 0f || float.IsNaN(lateral)) return;
                _data = data;
                _maxOriginal = max; _maxApplied = max * GripLift;
                _lateralOriginal = lateral; _lateralApplied = lateral * GripLift;
                data.TractionCurveMax = _maxApplied;
                data.TractionCurveLateral = _lateralApplied;
                float steer = data.SteeringLock;
                _steerOriginal = steer; _steerApplied = steer > 0f && !float.IsNaN(steer) ? steer * SteeringLift : steer;
                if (_steerApplied != steer) data.SteeringLock = _steerApplied;
                Active = true;
            }

            public void Restore()
            {
                if (!Active) return;
                Active = false;
                if (_data == null || !_data.IsValid) return;
                if (Math.Abs(_data.TractionCurveMax - _maxApplied) < 0.0001f) _data.TractionCurveMax = _maxOriginal;
                if (Math.Abs(_data.TractionCurveLateral - _lateralApplied) < 0.0001f) _data.TractionCurveLateral = _lateralOriginal;
                if (_steerApplied != _steerOriginal && Math.Abs(_data.SteeringLock - _steerApplied) < 0.0001f) _data.SteeringLock = _steerOriginal;
                _data = null;
            }
        }
    }
}
