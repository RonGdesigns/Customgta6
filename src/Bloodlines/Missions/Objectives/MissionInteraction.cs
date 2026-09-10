using System;
using System.Drawing;
using Bloodlines.Core;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Objectives
{
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
        private int _started = -1;
        private readonly bool _stopVehicle;
        private readonly string _animation;
        private bool _animating;
        /// <summary>Bent over, both hands inside something at waist height: a car window, a bin, a crate.</summary>
        public const string ReachInside = "amb@prop_human_bum_bin@idle_a|idle_a";
        public MissionInteraction(string action, Func<Vector3> position, int seconds, float radius = 3f, Func<Vehicle> vehicle = null, bool stopVehicle = false, string animation = null) : base(action)
        { _action = action; _position = position; _duration = seconds * 1000; _radius = radius; _vehicle = vehicle; _stopVehicle = stopVehicle; _animation = animation; }
        private void StopAnimation(Ped ped)
        {
            if (!_animating) return;
            _animating = false;
            if (ped != null && ped.Exists()) ped.Task.ClearAll();
        }
        private void StartAnimation(Ped ped, Vector3 point)
        {
            if (string.IsNullOrEmpty(_animation) || ped == null || !ped.Exists()) return;
            var parts = _animation.Split('|');
            if (parts.Length != 2) return;
            ped.Heading = Core.DriveUpStep.HeadingBetween(ped.Position, point);
            ped.Task.PlayAnimation(parts[0], parts[1], 4f, -4f, -1, AnimationFlags.Loop, 0f);
            _animating = true;
        }
        public override void Exit(MissionContext c) { StopAnimation(Game.Player.Character); base.Exit(c); }
        public override Vector3? AssignmentPosition => _vehicle == null ? (Vector3?)_position() : null;
        public override void Enter(MissionContext c) { base.Enter(c); _started = -1; Label = _action + " — go to the yellow marker; press E / D-pad Right."; }
        public override void Update(MissionContext c)
        {
            var requiredVehicle = _vehicle?.Invoke();
            if (_vehicle != null && (requiredVehicle == null || !requiredVehicle.Exists() || requiredVehicle.IsDead))
            { Fail("The required work vehicle is lost. Restart this mission."); return; }
            var point = _position(); var ped = Game.Player.Character;
            ObjectiveMarkers.Navigation(point, _vehicle == null ? RequiredCharacter : null, _vehicle?.Invoke());
            GameUtils.DrawObjectiveMarker(point, Color.Yellow, Math.Max(1f, _radius * .4f));
            if (!IsOwnerActive(c)) { _started = -1; StopAnimation(ped); Label = "Switch to " + Crew.Protagonist.Of(RequiredCharacter.Value).Handle + ": " + _action; return; }
            bool seated = _vehicle != null && ped != null && ped.IsInVehicle(_vehicle());
            bool near = ped != null && ped.Exists() && (_vehicle != null ? seated && ped.Position.DistanceTo(point) <= _radius : !ped.IsInVehicle() && ped.Position.DistanceTo(point) <= _radius);
            if (!near) { _started = -1; StopAnimation(ped); Label = _action + (_vehicle == null ? " — get out and reach the yellow marker." : " — take the marked vehicle to the yellow marker."); return; }
            if (_stopVehicle && requiredVehicle != null && requiredVehicle.Speed > 1f) { _started = -1; Label = _action + " — stop the vehicle to begin unloading."; return; }
            if (_started < 0)
            {
                Label = _action + " — press E / D-pad Right to start.";
                if (!Game.IsControlJustPressed(GTA.Control.Context)) return;
                _started = Game.GameTime;
                if (_vehicle == null) StartAnimation(ped, point);
            }
            int remaining = Math.Max(0, (_duration - (Game.GameTime - _started) + 999) / 1000);
            Label = _action + " — stay in the marker: " + remaining + "s.";
            if (remaining == 0) { StopAnimation(ped); Complete(); }
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
        public CaptureBoatObjective(Func<Ped> target, Func<Vehicle> boat, Func<Vehicle> pursuer) : base("Gohan: stay aboard the dinghy. Close within 25m of Mateo for 5 seconds; take him alive.")
        { _target = target; _boat = boat; _pursuer = pursuer; }
        public override void Enter(MissionContext c) { base.Enter(c); _started = Game.GameTime; _close = -1; }
        public override void Update(MissionContext c)
        {
            var target = _target(); var boat = _boat(); var chase = _pursuer();
            if (target == null || !target.Exists() || target.IsDead || boat == null || !boat.Exists() || !boat.IsDriveable) { Fail("Mateo must survive to explain the setup. Restart the mission."); return; }
            if (chase == null || !chase.Exists() || !chase.IsDriveable) { Fail("The crew's dinghy is lost."); return; }
            if (Game.GameTime - _started > 180000) { Fail("Mateo escaped into open water."); return; }
            ObjectiveMarkers.Navigation(boat.Position, null, chase); GameUtils.DrawObjectiveMarker(boat.Position, Color.Yellow, 3f);
            if (!IsOwnerActive(c) || !Game.Player.Character.IsInVehicle(chase) || chase.Position.DistanceTo(boat.Position) > 25f) { _close = -1; return; }
            if (_close < 0) _close = Game.GameTime;
            if (Game.GameTime - _close >= 5000) Complete();
        }
    }
}
