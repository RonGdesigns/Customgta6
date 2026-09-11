using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Bloodlines.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Missions.Objectives
{
    /// <summary>
    /// Keep a vehicle above a speed. The bible's escort missions are built on this —
    /// drop below the floor and the chase closes in — and it is a different kind of
    /// pressure from a timer: the player controls it every second rather than watching
    /// it drain.
    /// </summary>
    public sealed class SpeedFloorObjective : Objective
    {
        public override bool IsPassive => true;

        private readonly float _floorMph;
        private readonly int _graceSeconds;
        private readonly string _failMessage;

        private int _slowSince;

        public SpeedFloorObjective(string label, float floorMph, string failMessage, int graceSeconds = 6)
            : base(label)
        {
            _floorMph = floorMph;
            _failMessage = failMessage;
            _graceSeconds = graceSeconds;
        }

        public override void Update(MissionContext context)
        {
            var player = Game.Player.Character;
            var vehicle = player?.CurrentVehicle;
            if (vehicle == null || !vehicle.Exists())
            {
                _slowSince = 0;
                return;
            }

            // Vehicle.Speed is meters per second.
            float mph = vehicle.Speed * 2.23694f;
            bool slow = mph < _floorMph;

            if (!slow)
            {
                _slowSince = 0;
                GameUtils.Subtitle("~s~" + (int)mph + " mph", 400);
                return;
            }

            if (_slowSince == 0) _slowSince = Game.GameTime;

            int held = (Game.GameTime - _slowSince) / 1000;
            GameUtils.Subtitle("~r~" + (int)mph + " mph — keep it above " + (int)_floorMph +
                               "  (" + (_graceSeconds - held) + ")", 400);

            if (held >= _graceSeconds) Fail(_failMessage);
        }
    }

    /// <summary>
    /// Stay with a moving target without being on top of it — convoy shadowing,
    /// aerial tailing, escorting something that drives itself. The inverse of a
    /// pursuit: too far fails, and optionally too close does too.
    /// </summary>
    public sealed class ShadowTargetObjective : Objective
    {
        private readonly Func<Entity> _target;
        private readonly float _maxDistance;
        private readonly float _minDistance;
        private readonly int _holdSeconds;
        private readonly string _failMessage;
        private readonly int _acquireSeconds;
        private int _enteredAt;
        private int _inBandSince = -1;
        private int _outOfBandSince = -1;
        private bool _acquired;

        public ShadowTargetObjective(string label, Func<Entity> target, float maxDistance,
            int holdSeconds, string failMessage, float minDistance = 0f, int acquireSeconds = 0) : base(label)
        {
            _target = target; _maxDistance = maxDistance; _holdSeconds = holdSeconds;
            _failMessage = failMessage; _minDistance = minDistance; _acquireSeconds = acquireSeconds;
        }
        public override void Enter(MissionContext context)
        {
            base.Enter(context);
            _enteredAt = Game.GameTime;
            _inBandSince = _outOfBandSince = -1;
            _acquired = false;
        }
        public override void Update(MissionContext context)
        {
            var target = _target();
            if (target == null || !target.Exists()) { Fail("The target is gone."); return; }
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;
            ObjectiveMarkers.Navigation(target.Position, RequiredCharacter);
            float distance = player.Position.DistanceTo(target.Position);
            bool inBand = IsOwnerActive(context) && distance <= _maxDistance && distance >= _minDistance;
            int now = Game.GameTime;
            if (inBand)
            {
                _acquired = true;
                _outOfBandSince = -1;
                if (_inBandSince < 0) _inBandSince = now;
                int held = (now - _inBandSince) / 1000;
                if (held >= _holdSeconds) Complete();
                else GameUtils.Subtitle("~g~Holding~s~ - " + (_holdSeconds - held) + "s", 400);
                return;
            }
            _inBandSince = -1;
            if (!_acquired && _acquireSeconds > 0)
            {
                int left = _acquireSeconds - (now - _enteredAt) / 1000;
                if (left <= 0) Fail(_failMessage);
                else GameUtils.Subtitle("~y~Approach target~s~ - " + (int)distance + "m, " + left + "s", 400);
                return;
            }
            if (_outOfBandSince < 0) _outOfBandSince = now;
            if (now - _outOfBandSince >= 8000) Fail(_failMessage);
            else GameUtils.Subtitle("~y~Return to formation~s~ - " + (int)distance + "m", 400);
        }
    }

    /// <summary>
    /// Fly under a ceiling. Climb above it and the SAM sites get a lock — the desert
    /// missions are built on hugging the terrain, and an altitude limit is a more
    /// honest expression of that than an invisible wall.
    /// </summary>
    public sealed class AltitudeCeilingObjective : Objective
    {
        public override bool IsPassive => true;

        private readonly float _ceiling;
        private readonly int _graceSeconds;
        private readonly string _failMessage;

        private int _highSince;

        public AltitudeCeilingObjective(string label, float ceiling, string failMessage, int graceSeconds = 6)
            : base(label)
        {
            _ceiling = ceiling;
            _failMessage = failMessage;
            _graceSeconds = graceSeconds;
        }

        public override void Update(MissionContext context)
        {
            var player = Game.Player.Character;
            var vehicle = player?.CurrentVehicle;
            if (vehicle == null || !vehicle.Exists())
            {
                _highSince = 0;
                return;
            }

            float height = vehicle.HeightAboveGround;
            if (height <= _ceiling)
            {
                _highSince = 0;
                GameUtils.Subtitle("~s~" + (int)height + " m above terrain", 400);
                return;
            }

            if (_highSince == 0) _highSince = Game.GameTime;

            int held = (Game.GameTime - _highSince) / 1000;
            GameUtils.Subtitle("~r~SAM LOCK~s~ — get below " + (int)_ceiling + "  (" +
                               Math.Max(0, _graceSeconds - held) + ")", 400);

            if (held >= _graceSeconds) Fail(_failMessage);
        }
    }

    /// <summary>
    /// Put a specific vehicle somewhere — staging, deliveries, drop-offs. Unlike
    /// reaching a zone, what matters is where the vehicle ends up, so a character can
    /// park it and switch away without losing the objective.
    /// </summary>
    public sealed class DeliverVehicleObjective : Objective
    {
        private readonly Func<Vehicle> _vehicle;
        private readonly Func<Vector3> _destination;
        private readonly float _radius;
        private readonly bool _land;
        public DeliverVehicleObjective(string label, Func<Vehicle> vehicle, Func<Vector3> destination,
            float radius = 12f, bool land = false) : base(label)
        { _vehicle = vehicle; _destination = destination; _radius = radius; _land = land; }
        public override void Update(MissionContext context)
        {
            var vehicle = _vehicle(); var destination = _destination();
            if (vehicle == null || !vehicle.Exists() || !vehicle.IsDriveable) { Fail("The required vehicle is lost."); return; }
            var player = Game.Player.Character;
            // Locate the vehicle first, then route that vehicle to its destination.
            bool aboard = player != null && player.IsInVehicle(vehicle);
            ObjectiveMarkers.Navigation(aboard ? destination : vehicle.Position, aboard ? null : RequiredCharacter, aboard ? vehicle : null);
            GameUtils.DrawObjectiveMarker(destination, Color.Yellow, Math.Max(2f, _radius * .3f));
            if (!IsOwnerActive(context) || !aboard) return;
            if (vehicle.Position.DistanceTo(destination) > _radius) return;
            if (_land && (vehicle.HeightAboveGround > 3f || vehicle.Speed > 3f))
            { GameUtils.Subtitle("Land the marked aircraft and slow to a stop.", 500); return; }
            Complete();
        }
    }

    /// <summary>
    /// Do not be seen. Fails if a guard has line of sight on the player for longer
    /// than the tolerance — the mechanic the infiltration missions need, and the one
    /// that makes Thermal Pulse worth carrying.
    /// </summary>
    public sealed class AvoidDetectionObjective : Objective
    {
        public override bool IsPassive => true;

        private readonly Func<IEnumerable<Ped>> _watchers;
        private readonly float _sightRange;
        private readonly int _toleranceSeconds;
        private readonly string _failMessage;

        private int _seenSince;

        public AvoidDetectionObjective(Func<IEnumerable<Ped>> watchers, string failMessage,
            float sightRange = 35f, int toleranceSeconds = 3)
            : base("")
        {
            _watchers = watchers;
            _failMessage = failMessage;
            _sightRange = sightRange;
            _toleranceSeconds = toleranceSeconds;
        }

        public override void Update(MissionContext context)
        {
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            bool seen = _watchers()
                .Where(watcher => watcher != null && watcher.Exists() && watcher.IsAlive)
                .Any(watcher => watcher.Position.DistanceTo(player.Position) < _sightRange
                                && IsAlerted(watcher, player));

            if (!seen)
            {
                _seenSince = 0;
                return;
            }

            if (_seenSince == 0) _seenSince = Game.GameTime;

            int held = (Game.GameTime - _seenSince) / 1000;
            GameUtils.Subtitle("~r~SPOTTED~s~ — break line of sight (" +
                               Math.Max(0, _toleranceSeconds - held) + ")", 400);

            if (held >= _toleranceSeconds) Fail(_failMessage);
        }

        /// <summary>
        /// "Has this guard noticed you." CAN_PED_SEE_HATED_PED is the engine's own
        /// perception check — cone of vision and obstruction included — which is a
        /// better answer than a hand-rolled ray cast and stays consistent with how the
        /// AI itself decides to react.
        /// </summary>
        private static bool IsAlerted(Ped watcher, Ped player)
        {
            if (watcher.IsInCombat || watcher.IsShooting) return true;
            if (Function.Call<bool>(Hash.CAN_PED_SEE_HATED_PED, watcher, player)) return true;
            return watcher.IsHeadtracking(player);
        }
    }

    /// <summary>
    /// Visit several places and do a piece of work at each — limpet charges, thermite
    /// strips, seismic charges on pylons. One objective instead of a stage per site,
    /// and the player picks the order.
    /// </summary>
    public sealed class MultiHoldObjective : Objective
    {
        private readonly List<Vector3> _sites;
        private readonly int _secondsEach;
        private readonly float _radius;
        private readonly string _workText;
        private readonly HashSet<int> _done = new HashSet<int>();

        private int _activeSite = -1;
        private int _startedAt;
        private readonly Func<Vehicle> _vehicle;
        private readonly string _action;

        public MultiHoldObjective(string label, IEnumerable<Vector3> sites, int secondsEach,
            float radius = 3f, string workText = "Working", Func<Vehicle> vehicle = null)
            : base(label)
        {
            _action = label; _vehicle = vehicle;
            _sites = sites.ToList();
            _secondsEach = secondsEach;
            _radius = radius;
            _workText = workText;
        }

        public int Remaining => _sites.Count - _done.Count;
        /// <summary>Called with the site index when a site's hold completes: a part fitted, a charge set.</summary>
        public Action<int> SiteDone { get; set; }

        public override void Update(MissionContext context)
        {
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;
            var required = _vehicle?.Invoke();
            if (_vehicle != null && (required == null || !required.Exists() || required.IsDead))
            { Fail("The required work vehicle was lost. Restart the mission."); return; }

            for (int i = 0; i < _sites.Count; i++)
            {
                if (_done.Contains(i)) continue;
                GameUtils.DrawObjectiveMarker(_sites[i], Color.FromArgb(120, 232, 168, 56), _radius);
            }

            if (_done.Count >= _sites.Count)
            {
                Complete();
                return;
            }

            int nearest = Enumerable.Range(0, _sites.Count).Where(i => !_done.Contains(i)).OrderBy(i => player.Position.DistanceTo(_sites[i])).First();
            ObjectiveMarkers.Navigation(_sites[nearest], _vehicle == null ? RequiredCharacter : null, _vehicle?.Invoke());
            Label = _action + " — " + Remaining + " sites left; press E / D-pad Right at a marker.";
            if (!IsOwnerActive(context) || (_vehicle == null ? player.IsInVehicle() : !player.IsInVehicle(_vehicle()))) { _activeSite = -1; return; }
            int near = -1;
            for (int i = 0; i < _sites.Count; i++)
            {
                if (_done.Contains(i)) continue;
                if (GameUtils.IsWithin(player.Position, _sites[i], _radius)) { near = i; break; }
            }

            if (near < 0)
            {
                _activeSite = -1;
                GameUtils.Subtitle("~s~" + Remaining + " left", 400);
                return;
            }

            if (_activeSite != near)
            {
                if (!Game.IsControlJustPressed(GTA.Control.Context)) return;
                _activeSite = near;
                _startedAt = Game.GameTime;
                return;
            }

            int elapsed = (Game.GameTime - _startedAt) / 1000;
            if (elapsed >= _secondsEach)
            {
                _done.Add(near);
                _activeSite = -1;
                GameUtils.Subtitle("~g~Set. " + Remaining + " left.", 2000);
                SiteDone?.Invoke(near);
                return;
            }

            Label = _workText + " — " + Remaining + " left.";
            GameUtils.DrawProgressBar((Game.GameTime - _startedAt) / (_secondsEach * 1000f));
        }
    }
}
