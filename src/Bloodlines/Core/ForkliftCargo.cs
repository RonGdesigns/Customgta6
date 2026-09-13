using System;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>A visual crate rides the forks without entering the vehicle's physics hierarchy.</summary>
    public sealed class ForkliftCarryStep : SceneStep
    {
        private readonly Vehicle _forklift;
        private readonly Prop _crate;
        private readonly Vector3 _offset, _destination;
        private readonly float _heading;
        private Vector3 _original;
        private bool _prepared, _finished;
        public ForkliftCarryStep(Ped driver, Vehicle forklift, Prop crate, Vector3 offset, Vector3 destination, float heading)
        { Actor = driver; _forklift = forklift; _crate = crate; _offset = offset; _destination = destination; _heading = heading; TimeoutMs = 14000; }
        public override Entity CameraTarget => Usable(_crate) ? _crate : null;
        private bool Ready => Usable(Actor) && Usable(_forklift) && !_forklift.IsDead && Usable(_crate);
        private void Prepare()
        {
            if (_prepared || !Ready) return;
            _prepared = true; _original = _crate.Position;
            Function.Call(Hash.SET_ENTITY_COLLISION, _crate, false, false);
            _crate.Detach(); _crate.IsPositionFrozen = true;
        }
        protected override void OnStart()
        {
            if (!Ready || !Actor.IsInVehicle(_forklift)) { Failed = true; return; }
            Prepare(); FollowForks();
            _forklift.IsPositionFrozen = false; Actor.IsPositionFrozen = false;
            _forklift.IsEngineRunning = true;
            Actor.Task.DriveTo(_forklift, _destination, 2f, 5f, DrivingStyle.Normal);
        }
        private void FollowForks()
        {
            // Fork offset is ordinary vehicle right/forward/up; ShotStep uses forward/right/up.
            var point = ShotStep.Local(_forklift, new Vector3(_offset.Y, _offset.X, _offset.Z));
            Function.Call(Hash.SET_ENTITY_COORDS_NO_OFFSET, _crate, point.X, point.Y, point.Z, false, false, false);
            _crate.Heading = _forklift.Heading;
        }
        public override bool IsComplete
        {
            get
            {
                if (_finished) return true;
                if (!Ready) { Failed = true; return true; }
                FollowForks();
                if (Game.GameTime - StartedAt < 1200 || !GameUtils.IsWithinFlat(_forklift.Position, _destination, 4f) || _forklift.Speed > 1.5f) return false;
                Finish(); return true;
            }
        }
        public override void Finish()
        {
            if (_finished) return;
            if (!Ready || !Actor.IsInVehicle(_forklift)) { Failed = true; return; }
            Prepare();
            // A skipped or stalled scene has one deterministic end state, with the
            // driver still seated. Never clear his task immediately inside a vehicle.
            Actor.Task.ClearAll();
            if (!GameUtils.IsWithinFlat(_forklift.Position, _destination, 4f))
            {
                Function.Call(Hash.REQUEST_COLLISION_AT_COORD, _destination.X, _destination.Y, _destination.Z);
                Function.Call(Hash.SET_ENTITY_COORDS_NO_OFFSET, _forklift, _destination.X, _destination.Y, _destination.Z, false, false, false);
                _forklift.Heading = _heading;
            }
            _forklift.Speed = 0f;
            FollowForks(); _finished = true;
        }
        public override void Cancel()
        {
            if (Usable(Actor) && HasStarted) Actor.Task.ClearAll();
            if (_prepared && Usable(_crate))
            {
                _crate.Detach();
                Function.Call(Hash.SET_ENTITY_COORDS_NO_OFFSET, _crate, _original.X, _original.Y, _original.Z, false, false, false);
                _crate.IsPositionFrozen = true;
                Function.Call(Hash.SET_ENTITY_COLLISION, _crate, true, false);
            }
        }
    }

    /// <summary>Secure visual cargo with collision explicitly disabled before unfreezing it.</summary>
    public sealed class SafeCargoStowStep : SceneStep
    {
        private readonly Prop _crate;
        private readonly Vehicle _vehicle;
        private readonly Vector3 _offset;
        private bool _done;
        public SafeCargoStowStep(Prop crate, Vehicle vehicle, Vector3 offset)
        { _crate = crate; _vehicle = vehicle; _offset = offset; TimeoutMs = 2000; }
        public override Entity CameraTarget => Usable(_vehicle) ? _vehicle : null;
        protected override void OnStart() { Finish(); }
        public override bool IsComplete => _done || Failed;
        public override void Finish()
        {
            if (_done) return;
            if (!Usable(_crate) || !Usable(_vehicle) || _vehicle.IsDead) { Failed = true; return; }
            Function.Call(Hash.SET_ENTITY_COLLISION, _crate, false, false);
            _crate.Detach(); _crate.IsPositionFrozen = false;
            Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY, _crate, _vehicle, -1,
                _offset.X, _offset.Y, _offset.Z, 0f, 0f, 0f, false, false, false, false, 2, true);
            _done = Function.Call<bool>(Hash.IS_ENTITY_ATTACHED_TO_ENTITY, _crate, _vehicle);
            Failed = !_done;
            if (Failed) _crate.IsPositionFrozen = true;
        }
        public override void Cancel() { }
    }
}
