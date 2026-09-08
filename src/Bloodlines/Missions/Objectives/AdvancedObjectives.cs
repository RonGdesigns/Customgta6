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

            // Vehicle.Speed is metres per second.
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

        private int _startedAt;
        private int _outOfBandSince;

        public ShadowTargetObjective(string label, Func<Entity> target, float maxDistance,
            int holdSeconds, string failMessage, float minDistance = 0f)
            : base(label)
        {
            _target = target;
            _maxDistance = maxDistance;
            _minDistance = minDistance;
            _holdSeconds = holdSeconds;
            _failMessage = failMessage;
        }

        public override void Enter(MissionContext context)
        {
            _startedAt = Game.GameTime;
        }

        public override void Update(MissionContext context)
        {
            var target = _target();
            if (target == null || !target.Exists())
            {
                Complete();
                return;
            }

            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            float distance = player.Position.DistanceTo(target.Position);
            bool tooFar = distance > _maxDistance;
            bool tooClose = _minDistance > 0f && distance < _minDistance;

            int held = (Game.GameTime - _startedAt) / 1000;
            if (held >= _holdSeconds && !tooFar)
            {
                Complete();
                return;
            }

            string state = tooFar ? "~r~too far" : tooClose ? "~y~too close" : "~g~holding";
            GameUtils.Subtitle(state + "~s~ · " + (int)distance + "m · " +
                               Math.Max(0, _holdSeconds - held) + "s", 400);

            if (!tooFar && !tooClose)
            {
                _outOfBandSince = 0;
                return;
            }

            if (_outOfBandSince == 0) _outOfBandSince = Game.GameTime;
            if ((Game.GameTime - _outOfBandSince) / 1000 >= 8) Fail(_failMessage);
        }
    }

    /// <summary>
    /// Fly under a ceiling. Climb above it and the SAM sites get a lock — the desert
    /// missions are built on hugging the terrain, and an altitude limit is a more
    /// honest expression of that than an invisible wall.
    /// </summary>
    public sealed class AltitudeCeilingObjective : Objective
    {
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
                GameUtils.Subtitle("~s~" + (int)height + " ft above terrain", 400);
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

        public DeliverVehicleObjective(string label, Func<Vehicle> vehicle, Func<Vector3> destination,
            float radius = 12f)
            : base(label)
        {
            _vehicle = vehicle;
            _destination = destination;
            _radius = radius;
        }

        public override void Update(MissionContext context)
        {
            var vehicle = _vehicle();
            var destination = _destination();

            GameUtils.DrawObjectiveMarker(destination, Color.FromArgb(120, 232, 168, 56), _radius * 0.5f);

            if (vehicle == null || !vehicle.Exists())
            {
                Fail("The vehicle is gone.");
                return;
            }

            if (GameUtils.IsWithinFlat(vehicle.Position, destination, _radius)) Complete();
        }
    }

    /// <summary>
    /// Do not be seen. Fails if a guard has line of sight on the player for longer
    /// than the tolerance — the mechanic the infiltration missions need, and the one
    /// that makes Thermal Pulse worth carrying.
    /// </summary>
    public sealed class AvoidDetectionObjective : Objective
    {
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

        public MultiHoldObjective(string label, IEnumerable<Vector3> sites, int secondsEach,
            float radius = 3f, string workText = "Working")
            : base(label)
        {
            _sites = sites.ToList();
            _secondsEach = secondsEach;
            _radius = radius;
            _workText = workText;
        }

        public int Remaining => _sites.Count - _done.Count;

        public override void Update(MissionContext context)
        {
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

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
                return;
            }

            GameUtils.Subtitle(_workText + "... " + (_secondsEach - elapsed) + "s", 500);
        }
    }
}
