using System;
using System.Drawing;
using Bloodlines.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Missions.Objectives
{
    /// <summary>The player drives the load; the visual crate never collides with the forklift.</summary>
    public sealed class ForkliftDeliveryObjective : Objective
    {
        private readonly string _action;
        private readonly Func<Prop> _crate;
        private readonly Func<Vehicle> _forklift, _hauler;
        private readonly Vector3 _forkOffset, _bedOffset;
        private readonly Action _secured;
        private int _dwell, _lastTick;
        public bool Carrying { get; private set; }
        public Vector3 Target => Carrying ? _hauler().Position - _hauler().ForwardVector * 7f : _crate().Position;
        public ForkliftDeliveryObjective(string action, Func<Prop> crate, Func<Vehicle> forklift, Func<Vehicle> hauler,
            Vector3 forkOffset, Vector3 bedOffset, Action secured) : base(action)
        { _action = action; _crate = crate; _forklift = forklift; _hauler = hauler; _forkOffset = forkOffset; _bedOffset = bedOffset; _secured = secured; }
        public override void Enter(MissionContext c) { base.Enter(c); _lastTick = Game.GameTime; _dwell = 0; Carrying = false; }
        public override void Update(MissionContext c)
        {
            var crate = _crate(); var lift = _forklift(); var bed = _hauler(); var player = Game.Player.Character;
            if (crate == null || !crate.Exists() || lift == null || !lift.Exists() || lift.IsDead || bed == null || !bed.Exists() || bed.IsDead)
            { Fail("The crate, forklift or flatbed was lost. Restart the mission."); return; }
            int delta = Math.Max(0, Math.Min(500, Game.GameTime - _lastTick)); _lastTick = Game.GameTime;
            if (Carrying)
            {
                var p = ShotStep.Local(lift, new Vector3(_forkOffset.Y, _forkOffset.X, _forkOffset.Z));
                Function.Call(Hash.SET_ENTITY_COORDS_NO_OFFSET, crate, p.X, p.Y, p.Z, false, false, false);
                crate.Heading = lift.Heading;
            }
            var target = Target;
            ObjectiveMarkers.Navigation(target, null, lift);
            GameUtils.DrawObjectiveMarker(target, Color.Yellow, 2f);
            Label = _action + (Carrying ? " - drive to the yellow loading marker behind the flatbed and stop." : " - stop the forklift with its forks under the crate.");
            bool driving = IsOwnerActive(c) && player != null && player.Exists() && player.IsInVehicle(lift) && player.SeatIndex == VehicleSeat.Driver;
            if (!driving || lift.Position.DistanceTo(target) > 4.5f || lift.Speed > 1f || (Carrying && bed.Speed > .5f)) { _dwell = 0; return; }
            _dwell += delta;
            GameUtils.DrawProgressBar(_dwell / (Carrying ? 2000f : 1000f));
            if (!Carrying && _dwell >= 1000)
            {
                Function.Call(Hash.SET_ENTITY_COLLISION, crate, false, false);
                crate.Detach(); crate.IsPositionFrozen = true; Carrying = true; _dwell = 0;
                GameUtils.Subtitle("~y~Crate on the forks. Drive it to the rear of the flatbed.", 4000);
            }
            else if (Carrying && _dwell >= 2000)
            {
                var stow = new SafeCargoStowStep(crate, bed, _bedOffset); stow.Finish();
                if (stow.Failed) { Fail("The crate could not be secured on the flatbed. Restart the mission."); return; }
                _secured?.Invoke(); Complete();
            }
        }
    }
}
