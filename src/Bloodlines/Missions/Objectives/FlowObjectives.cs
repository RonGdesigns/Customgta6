using System;
using System.Collections.Generic;
using System.Drawing;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Objectives
{
    /// <summary>
    /// The playable approach: drive (or walk) to the job, with radio lines fired at
    /// fractions of the distance so the crew can say where they will split before
    /// anyone is dropped off. Arrival means inside the zone and, in a vehicle,
    /// stopped. Leaving the required vehicle is told, not failed.
    /// </summary>
    public sealed class TravelObjective : Objective
    {
        private readonly Func<Vector3> _destination;
        private readonly Func<Vehicle> _vehicle;
        private readonly float _radius;
        private readonly List<KeyValuePair<float, Action>> _cues = new List<KeyValuePair<float, Action>>();
        private readonly HashSet<int> _fired = new HashSet<int>();
        private float _startDistance = -1f;

        public TravelObjective(string label, Func<Vector3> destination, float radius = 12f, Func<Vehicle> vehicle = null) : base(label)
        { _destination = destination; _radius = radius; _vehicle = vehicle; }

        /// <summary>Fire an action once the remaining distance drops below this fraction of the start distance.</summary>
        public TravelObjective Cue(float remainingFraction, Action action) { _cues.Add(new KeyValuePair<float, Action>(remainingFraction, action)); return this; }

        public override Vector3? AssignmentPosition => null;
        public override void Enter(MissionContext c) { base.Enter(c); _startDistance = -1f; _fired.Clear(); }

        public override void Update(MissionContext c)
        {
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;
            var vehicle = _vehicle?.Invoke();
            if (_vehicle != null && (vehicle == null || !vehicle.Exists() || vehicle.IsDead)) { Fail("The crew's vehicle is lost. Restart this mission."); return; }
            var destination = _destination();
            ObjectiveMarkers.Navigation(destination, RequiredCharacter, vehicle);
            GameUtils.DrawObjectiveMarker(destination, Color.Yellow, Math.Max(2f, _radius * 0.5f));
            if (!IsOwnerActive(c)) return;
            float distance = player.Position.DistanceTo(destination);
            if (_startDistance < 0f) _startDistance = Math.Max(1f, distance);
            for (int i = 0; i < _cues.Count; i++)
            {
                if (_fired.Contains(i) || distance / _startDistance > _cues[i].Key) continue;
                _fired.Add(i);
                try { _cues[i].Value(); } catch (Exception ex) { Logger.Error("Travel cue failed", ex); }
            }
            if (vehicle != null && !player.IsInVehicle(vehicle)) { Label = "Get back in the crew's vehicle."; return; }
            if (!GameUtils.IsWithinFlat(player.Position, destination, _radius)) return;
            if (vehicle != null && vehicle.Speed > 2f) { Label = "Stop inside the yellow zone."; return; }
            Complete();
        }
    }

    /// <summary>
    /// The player is told the job has moved to another brother and is offered the
    /// switch for a window while that brother's own AI keeps the job going. If the
    /// window passes, the switch is required through the ordinary hand-off, which
    /// never freezes anyone. Ownership stays open until then, so the stage does not
    /// force the switch the moment it begins.
    /// </summary>
    public sealed class SwitchWindowObjective : Objective
    {
        private readonly CrewSlot _target;
        private readonly int _windowMs;
        private readonly Action _shadow, _onSwitched;
        private int _openedAt;
        private bool _forced;

        public SwitchWindowObjective(string label, CrewSlot target, int windowMs, Action shadow = null, Action onSwitched = null) : base(label)
        { _target = target; _windowMs = windowMs; _shadow = shadow; _onSwitched = onSwitched; }

        public override bool KeepsOwnerOpen => true;
        public bool Forced => _forced;
        public CrewSlot Target => _target;

        public override void Enter(MissionContext c)
        {
            base.Enter(c);
            _openedAt = Game.GameTime; _forced = false;
            try { _shadow?.Invoke(); } catch (Exception ex) { Logger.Error("Switch window shadow task failed", ex); }
        }

        public override void Update(MissionContext c)
        {
            if (c.Crew.ActiveSlot == _target)
            {
                try { _onSwitched?.Invoke(); } catch (Exception ex) { Logger.Error("Switch window hand-over failed", ex); }
                Complete();
                return;
            }
            int remaining = Math.Max(0, (_windowMs - (Game.GameTime - _openedAt) + 999) / 1000);
            if (remaining > 0) { Label = Label.Split('—')[0].Trim() + " — " + remaining + "s"; return; }
            if (!_forced)
            {
                _forced = true;
                RequiredCharacter = _target;
                Label = "Switch to " + Protagonist.Of(_target).Handle + " now.";
                Logger.Info("Switch window closed; requiring " + _target + ".");
            }
        }
    }

    /// <summary>A passive trigger: watches a condition and fires a mission callback once. It never completes the stage on its own.</summary>
    public sealed class ReactionTrigger : Objective
    {
        private readonly Func<bool> _condition;
        private readonly Action _fire;
        public bool Fired { get; private set; }

        public ReactionTrigger(Func<bool> condition, Action fire) : base("") { _condition = condition; _fire = fire; }
        public override bool IsPassive => true;

        public override void Update(MissionContext c)
        {
            if (Fired) return;
            bool hit;
            try { hit = _condition(); } catch (Exception ex) { Logger.Error("Reaction condition failed", ex); return; }
            if (!hit) return;
            Fired = true;
            try { _fire(); } catch (Exception ex) { Logger.Error("Reaction failed", ex); }
        }
    }

    /// <summary>Hold until the named brothers' tracks reach a state, or a deadline passes (logged, not failed).</summary>
    public sealed class WaitForRolesObjective : Objective
    {
        private readonly RoleTracks _tracks;
        private readonly CrewSlot[] _slots;
        private readonly RoleState _state;
        private readonly int _timeoutMs;
        private int _startedAt;

        public WaitForRolesObjective(string label, RoleTracks tracks, RoleState state, int timeoutMs, params CrewSlot[] slots) : base(label)
        { _tracks = tracks; _state = state; _timeoutMs = timeoutMs; _slots = slots; }

        public override void Enter(MissionContext c) { base.Enter(c); _startedAt = Game.GameTime; }

        public override void Update(MissionContext c)
        {
            if (_tracks.AllIn(_state, _slots)) { Complete(); return; }
            if (Game.GameTime - _startedAt < _timeoutMs) return;
            Logger.Warn("Roles did not all reach " + _state + " within " + _timeoutMs + " ms; continuing.");
            Complete();
        }
    }
}
