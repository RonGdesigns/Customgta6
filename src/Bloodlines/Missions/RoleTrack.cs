using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions
{
    public enum RoleState { Idle, Approaching, Observing, Working, Threatened, Covering, Extracting }

    /// <summary>
    /// A brother's job while the player is someone else. Not a new AI: the mission
    /// owns the ped through the companion controller and this issues its tasks and
    /// reacts to danger. Approaching walks to a point and becomes Observing there;
    /// Observing and Working hold the point; any of those become Threatened when the
    /// ped is in combat, loses health, or an enemy closes in, which sends him to his
    /// cover point to fight; six clear seconds resume what he was doing. Extracting
    /// walks to the regroup point and waits. The player switching into this brother
    /// picks up wherever the track is; switching away hands it back.
    /// </summary>
    public sealed class RoleTrack
    {
        public const float ThreatRadius = 35f;
        public const int ClearMs = 6000;

        private readonly Func<IEnumerable<Ped>> _enemies;
        private Vector3 _point, _cover;
        private RoleState _resume = RoleState.Idle;
        private int _lastHealth = -1, _clearSince, _lastOrder;
        private bool _orderIssued;

        public CrewSlot Slot { get; }
        public Ped Ped { get; }
        public RoleState State { get; private set; } = RoleState.Idle;
        public Vector3 Point => _point;
        public Vector3 Cover => _cover;
        public bool Arrived => Ped != null && Ped.Exists() && Ped.Position.DistanceTo(_point) <= 2.5f;

        public RoleTrack(CrewSlot slot, Ped ped, Func<IEnumerable<Ped>> enemies) { Slot = slot; Ped = ped; _enemies = enemies; }

        public void Approach(Vector3 point, Vector3 cover) { _point = point; _cover = cover; Enter(RoleState.Approaching); }
        public void Observe(Vector3 point, Vector3 cover) { _point = point; _cover = cover; Enter(RoleState.Observing); }
        public void Work(Vector3 point, Vector3 cover) { _point = point; _cover = cover; Enter(RoleState.Working); }
        public void TakeCover(Vector3 cover) { _cover = cover; Enter(RoleState.Covering); }
        public void Extract(Vector3 point) { _point = point; Enter(RoleState.Extracting); }
        public void Stop() { Enter(RoleState.Idle); }

        private void Enter(RoleState state)
        {
            State = state; _orderIssued = false; _clearSince = 0;
            if (Ped != null && Ped.Exists()) _lastHealth = Ped.Health;
        }

        /// <summary>Called every tick while the player is someone else.</summary>
        public void Update()
        {
            if (Ped == null || !Ped.Exists() || Ped.IsDead || State == RoleState.Idle) return;
            if (!_orderIssued) { Order(); _orderIssued = true; }

            if (State == RoleState.Approaching && Arrived) { State = RoleState.Observing; Order(); return; }
            if (State == RoleState.Extracting && Arrived && Game.GameTime - _lastOrder > 500) { Ped.Task.StandStill(-1); _lastOrder = Game.GameTime + 60000; return; }

            bool threatened = Threatened();
            if (State == RoleState.Approaching || State == RoleState.Observing || State == RoleState.Working)
            {
                if (threatened) { _resume = State; State = RoleState.Threatened; Order(); Logger.Debug(Slot + " threatened; taking cover at " + _cover); }
                return;
            }
            if (State == RoleState.Threatened)
            {
                if (threatened) { _clearSince = 0; return; }
                if (_clearSince == 0) _clearSince = Game.GameTime;
                if (Game.GameTime - _clearSince < ClearMs) return;
                State = _resume == RoleState.Idle ? RoleState.Observing : _resume; _clearSince = 0; Order();
                Logger.Debug(Slot + " clear; resuming " + State);
            }
        }

        private bool Threatened()
        {
            int health = Ped.Health;
            bool hurt = _lastHealth >= 0 && health < _lastHealth;
            _lastHealth = health;
            if (hurt || Ped.IsInCombat) return true;
            var enemies = _enemies?.Invoke();
            if (enemies == null) return false;
            foreach (var enemy in enemies)
                if (enemy != null && enemy.Exists() && !enemy.IsDead && enemy.Position.DistanceTo(Ped.Position) <= ThreatRadius) return true;
            return false;
        }

        private void Order()
        {
            _lastOrder = Game.GameTime;
            var task = Ped.Task;
            switch (State)
            {
                case RoleState.Approaching: task.ClearAll(); task.GoTo(_point); break;
                case RoleState.Observing: task.ClearAll(); Ped.Heading = DriveUpStep.HeadingBetween(Ped.Position, _cover == Vector3.Zero ? _point : _cover); task.GuardCurrentPosition(); break;
                case RoleState.Working: task.ClearAll(); task.StandStill(-1); break;
                case RoleState.Threatened:
                case RoleState.Covering: task.ClearAll(); task.RunTo(_cover, false, 8000); task.FightAgainstHatedTargets(60f); break;
                case RoleState.Extracting: task.ClearAll(); task.RunTo(_point, false, 20000); break;
            }
        }
    }

    /// <summary>The tracks of one mission, keyed by brother; the active player's track is left alone.</summary>
    public sealed class RoleTracks
    {
        private readonly Dictionary<CrewSlot, RoleTrack> _tracks = new Dictionary<CrewSlot, RoleTrack>();
        private readonly CrewRoster _crew;
        private readonly Func<IEnumerable<Ped>> _enemies;

        public RoleTracks(CrewRoster crew, Func<IEnumerable<Ped>> enemies) { _crew = crew; _enemies = enemies; }

        public RoleTrack For(CrewSlot slot)
        {
            if (_tracks.TryGetValue(slot, out var track)) return track;
            var ped = _crew.PedFor(slot);
            _crew.CompanionAI.TakeControl(slot);
            return _tracks[slot] = new RoleTrack(slot, ped, _enemies);
        }

        public bool AllIn(RoleState state, params CrewSlot[] slots) =>
            slots.All(slot => _tracks.TryGetValue(slot, out var track) && (track.State == state || (state == RoleState.Observing && track.State == RoleState.Threatened && track.Arrived)));

        public void Update()
        {
            foreach (var track in _tracks.Values)
                if (track.Slot != _crew.ActiveSlot) track.Update();
        }

        public void Release()
        {
            foreach (var slot in _tracks.Keys.ToList()) _crew.CompanionAI.ReleaseControl(slot);
            _tracks.Clear();
        }
    }
}
