using System;
using System.Drawing;
using Bloodlines.Core;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Objectives
{
    /// <summary>Board a mission ride without attempting to drag its protected crew driver out.</summary>
    public static class MissionBoarding
    {
        public static VehicleSeat FreeSeat(Vehicle car, VehicleSeat desired)
        {
            if(desired!=VehicleSeat.Any)return car.IsSeatFree(desired)?desired:VehicleSeat.None;
            // A crew driver keeps his seat. Front passenger is index zero, not one.
            for(int i=0;i<car.PassengerCapacity;i++)if(car.IsSeatFree((VehicleSeat)i))return (VehicleSeat)i;
            return car.IsSeatFree(VehicleSeat.Driver)?VehicleSeat.Driver:VehicleSeat.None;
        }
        public static void Update(Vehicle vehicle, VehicleSeat desired, ref int nextBoard)
        {
            var player=Game.Player.Character;
            if(vehicle==null||!vehicle.Exists()||vehicle.IsDead||player==null||!player.Exists()||player.IsInVehicle()||vehicle.Speed>3f)return;
            // Aircraft and subs have larger hulls than a sedan; measure the nearby
            // hatch area rather than demanding that the player stand in its origin.
            float range=vehicle.Model.IsHelicopter?12f:7f;
            if(player.Position.DistanceTo(vehicle.Position)>range)return;
            vehicle.LockStatus=VehicleLockStatus.Unlocked;
            bool pressed=Game.IsControlJustPressed(GTA.Control.Enter)||Game.IsControlJustPressed(GTA.Control.Context);
            if(!pressed||Game.GameTime<nextBoard)return;
            var seat=FreeSeat(vehicle,desired);if(seat==VehicleSeat.None)return;
            Game.DisableControlThisFrame(GTA.Control.Enter);
            player.Task.ClearAll();
            player.Task.EnterVehicle(vehicle,seat,8000,2f,EnterVehicleFlags.None);
            nextBoard=Game.GameTime+2000;
            Logger.Info("Mission boarding: normal door entry to seat "+seat+"; existing crew seats retained.");
        }
    }

    public sealed class DialogueFinishedObjective : Objective
    {
        public DialogueFinishedObjective(string label = "Listen to Mateo. Stay with the boats."):base(label){}
        public override void Update(MissionContext c){if(!c.Dialogue.HasPending)Complete();}
    }

    /// <summary>Explicit context-button interaction with a visible, interruptible work timer.</summary>
    public sealed class MissionInteraction : Objective
    {
        private readonly string _action;
        private readonly Func<Vector3> _position;
        private readonly Func<Vehicle> _vehicle;
        private readonly float _radius;
        private readonly int _duration;
        private int _started = -1, _nextBoard, _lastTick = -1, _lastSteady = -1;
        /// <summary>
        /// How fast a boat may be moving and still count as stopped. Storm swell alone moves an
        /// idle dinghy past the 1 m/s a car is held to, so M05's "take him aboard" could not be
        /// started and, once started, kept restarting (Ron, September 22).
        /// </summary>
        public const float AfloatStopSpeed = 3.5f;
        /// <summary>How long a boat may be pushed out of reach or over that speed before a hold in progress is lost. The bar pauses meanwhile; it does not restart.</summary>
        public const int AfloatGraceMs = 2500;
        private readonly bool _stopVehicle;
        private readonly string _animation;
        private readonly Func<Vector3> _face;
        private bool _animating;
        private Ped _worker;
        /// <summary>Bent over, both hands inside something at waist height: a car window, a bin, a crate.</summary>
        public const string ReachInside = "amb@prop_human_bum_bin@idle_a|idle_a";
        public MissionInteraction(string action, Func<Vector3> position, int seconds, float radius = 3f, Func<Vehicle> vehicle = null, bool stopVehicle = false, string animation = null, Func<Vector3> face = null) : base(action)
        { _action = action; _position = position; _duration = seconds * 1000; _radius = radius; _vehicle = vehicle; _stopVehicle = stopVehicle; _animation = animation; _face = face; }
        private void StopAnimation(Ped ped)
        {
            if (!_animating) return;
            _animating = false;
            if (_worker != null && _worker.Exists() && !string.IsNullOrEmpty(_animation))
            {
                var parts = _animation.Split('|');
                if (parts.Length == 2) GTA.Native.Function.Call(GTA.Native.Hash.STOP_ANIM_TASK, _worker, parts[0], parts[1], 2f);
            }
            _worker = null;
        }
        private void StartAnimation(Ped ped, Vector3 point)
        {
            if (string.IsNullOrEmpty(_animation) || ped == null || !ped.Exists()) return;
            var parts = _animation.Split('|');
            if (parts.Length != 2) return;
            ped.Heading = Core.DriveUpStep.HeadingBetween(ped.Position, _face?.Invoke() ?? point);
            ped.Task.PlayAnimation(parts[0], parts[1], 4f, -4f, -1, AnimationFlags.Loop, 0f);
            _animating = true; _worker = ped;
        }
        public override void Exit(MissionContext c) { StopAnimation(Game.Player.Character); base.Exit(c); }
        public override Vector3? AssignmentPosition => _vehicle == null ? (Vector3?)_position() : null;
        public override void Enter(MissionContext c) { base.Enter(c); _started = -1; _lastTick = -1; _lastSteady = -1; Label = _action + " — go to the yellow marker; press E / D-pad Right."; }
        public override void Update(MissionContext c)
        {
            int now = Game.GameTime;
            int dt = _lastTick < 0 ? 0 : Math.Max(0, Math.Min(250, now - _lastTick));
            _lastTick = now;
            var requiredVehicle = _vehicle?.Invoke();
            if (_vehicle != null && (requiredVehicle == null || !requiredVehicle.Exists() || requiredVehicle.IsDead))
            { Fail("The required work vehicle is lost. Restart this mission."); return; }
            var point = _position(); var ped = Game.Player.Character;
            bool underwater = UnderwaterGuidance.IsSub(requiredVehicle);
            if (underwater) UnderwaterGuidance.Draw(requiredVehicle, point, _radius);
            else { ObjectiveMarkers.Navigation(point, _vehicle == null ? RequiredCharacter : null, requiredVehicle); GameUtils.DrawObjectiveMarker(point, Color.Yellow, Math.Max(1f, _radius * .4f)); }
            if (!IsOwnerActive(c)) { _started = -1; StopAnimation(ped); Label = "Switch to " + Crew.Protagonist.Of(RequiredCharacter.Value).Handle + ": " + _action; return; }
            if (requiredVehicle != null) MissionBoarding.Update(requiredVehicle, VehicleSeat.Any, ref _nextBoard);
            bool seated = _vehicle != null && ped != null && ped.IsInVehicle(_vehicle());
            // Afloat, the reach is measured flat: the swell lifts one boat past the other and
            // a three-dimensional distance counted that as drifting apart.
            bool afloat = requiredVehicle != null && requiredVehicle.Model.IsBoat;
            bool near = ped != null && ped.Exists() && (_vehicle != null
                ? seated && (afloat ? GameUtils.IsWithinFlat(requiredVehicle.Position, point, _radius) : requiredVehicle.Position.DistanceTo(point) <= _radius)
                : !ped.IsInVehicle() && ped.Position.DistanceTo(point) <= _radius);
            bool steady = !_stopVehicle || requiredVehicle == null || requiredVehicle.Speed <= (afloat ? AfloatStopSpeed : 1f);
            if (near && steady) _lastSteady = now;
            else if (afloat && seated && _started >= 0 && now - _lastSteady <= AfloatGraceMs)
            {
                // A wave is not the player letting go. Hold the bar where it is for a moment
                // rather than throwing away the work.
                _started += dt;
                Label = _action + " — hold steady; the swell moved the boat.";
                GameUtils.DrawProgressBar((now - _started) / (float)_duration);
                return;
            }
            if (!near) { _started = -1; StopAnimation(ped); Label = _action + (_vehicle == null ? " — get out and reach the yellow marker." : (seated ? " — take the marked vehicle to the yellow marker." : " — board the marked vehicle first (F / Y or E / D-pad Right).")); return; }
            if (!steady) { _started = -1; Label = _action + (afloat ? " — ease off the throttle and let the boat settle." : " — stop the vehicle to begin unloading."); return; }
            if (_started < 0)
            {
                Label = _action + " — press E / D-pad Right to start.";
                if (!Game.IsControlJustPressed(GTA.Control.Context)) return;
                _started = now;
                if (_vehicle == null) StartAnimation(ped, point);
            }
            int elapsed = now - _started;
            Label = _action;
            GameUtils.DrawProgressBar(elapsed / (float)_duration);
            if (elapsed >= _duration) { StopAnimation(ped); Complete(); }
        }
    }

    public sealed class OccupiedVehicleDestination : Objective
    {
        private readonly Func<Vehicle> _vehicle;
        private readonly Func<Vector3> _destination;
        private readonly float _radius;
        public OccupiedVehicleDestination(string label, Func<Vehicle> vehicle, Func<Vector3> destination, float radius = 20f) : base(label)
        { _vehicle = vehicle; _destination = destination; _radius = radius; }
        public override void Update(MissionContext c)
        {
            var vehicle = _vehicle();
            if (vehicle == null || !vehicle.Exists() || !vehicle.IsDriveable) { Fail("The required vehicle is lost."); return; }
            var target = _destination();
            ObjectiveMarkers.Navigation(target, null, vehicle); GameUtils.DrawObjectiveMarker(target, Color.Yellow, 4f);
            var player = Game.Player.Character;
            if (IsOwnerActive(c) && player != null && player.IsInVehicle(vehicle) && GameUtils.IsWithinFlat(vehicle.Position, target, _radius)) Complete();
        }
    }

    /// <summary>Capture by sustained close pursuit, with a living target required for the next scene.</summary>
    public sealed class CaptureBoatObjective : Objective
    {
        private readonly Func<Ped> _target;
        private readonly Func<Vehicle> _boat, _pursuer;
        private int _started, _close = -1;
        public Func<bool> CaptureReady { get; set; }
        public Func<string> DrivingHint { get; set; }
        public int DeadlineMs { get; set; } = 180000;
        public CaptureBoatObjective(Func<Ped> target, Func<Vehicle> boat, Func<Vehicle> pursuer) : base("Stay aboard the dinghy. Close within 25m of Mateo for 5 seconds; take him alive.")
        { _target = target; _boat = boat; _pursuer = pursuer; }
        public override void Enter(MissionContext c) { base.Enter(c); _started = Game.GameTime; _close = -1; }
        public override void Update(MissionContext c)
        {
            var target = _target(); var boat = _boat(); var chase = _pursuer();
            if (target == null || !target.Exists() || target.IsDead || boat == null || !boat.Exists() || !boat.IsDriveable) { Fail("Mateo must survive to explain the setup. Restart the mission."); return; }
            if (chase == null || !chase.Exists() || !chase.IsDriveable) { Fail("The crew's dinghy is lost."); return; }
            if (Game.GameTime - _started > DeadlineMs) { Fail("Mateo escaped into open water."); return; }
            ObjectiveMarkers.Navigation(boat.Position, null, chase); GameUtils.DrawObjectiveMarker(boat.Position, Color.Yellow, 3f);
            if(CaptureReady!=null&&!CaptureReady()) { _close=-1;Label=("Follow Mateo through the offshore run. Stay aboard. " + DrivingHint?.Invoke()).Trim();return; }
            Label="Stay aboard and close within 25m of Mateo for 5 seconds to stop him alive.";
            if (!IsOwnerActive(c) || !Game.Player.Character.IsInVehicle(chase) || chase.Position.DistanceTo(boat.Position) > 25f) { _close = -1; return; }
            if (_close < 0) _close = Game.GameTime;
            if (Game.GameTime - _close >= 5000) Complete();
        }
    }
}
