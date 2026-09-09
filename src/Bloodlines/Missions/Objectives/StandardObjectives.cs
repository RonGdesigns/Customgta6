using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Bloodlines.Core;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Objectives
{
    /// <summary>Get to a place. The most common objective in any mission.</summary>
    public sealed class ReachZoneObjective : Objective
    {
        private readonly Func<Vector3> _position;
        private readonly float _radius;
        private readonly bool _flat;
        private readonly bool _inVehicle;
        private readonly Color _colour;
        private bool _announced;

        public ReachZoneObjective(string label, Func<Vector3> position, float radius = 3f,
            bool flat = false, bool requireVehicle = false, Color? colour = null)
            : base(label)
        {
            _position = position;
            _radius = radius;
            _flat = flat;
            _inVehicle = requireVehicle;
            _colour = colour ?? Color.FromArgb(160, 240, 205, 60);
        }

        public ReachZoneObjective(string label, Vector3 position, float radius = 3f,
            bool flat = false, bool requireVehicle = false)
            : this(label, () => position, radius, flat, requireVehicle)
        {
        }

        public override Vector3? AssignmentPosition => _inVehicle ? (Vector3?)null : _position();

        public override void Update(MissionContext context)
        {
            var target = _position();
            GameUtils.DrawObjectiveMarker(target, _colour, Math.Max(1f, _radius * 0.6f));
            ObjectiveMarkers.Navigation(target, RequiredCharacter);

            if (!IsOwnerActive(context)) return;

            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;
            if (!_announced) { GameUtils.Notify("~y~" + Label); _announced = true; }
            if (_inVehicle && !player.IsInVehicle()) return;

            bool arrived = _flat
                ? GameUtils.IsWithinFlat(player.Position, target, _radius)
                : GameUtils.IsWithin(player.Position, target, _radius);

            if (arrived) Complete();
        }
    }

    /// <summary>Stay in a zone for a set time — data rips, thermite burns, holds.</summary>
    public sealed class HoldZoneObjective : Objective
    {
        private readonly Func<Vector3> _position;
        private readonly float _radius;
        private readonly int _seconds;
        private readonly string _progressText;

        private int _startedAt;

        public HoldZoneObjective(string label, Func<Vector3> position, int seconds,
            float radius = 2.5f, string progressText = null)
            : base(label)
        {
            _position = position;
            _seconds = seconds;
            _radius = radius;
            _progressText = progressText ?? label;
        }

        public override Vector3? AssignmentPosition => _position();

        public override void Update(MissionContext context)
        {
            var target = _position();
            GameUtils.DrawObjectiveMarker(target, Color.FromArgb(120, 106, 168, 122), _radius);

            ObjectiveMarkers.Navigation(target, RequiredCharacter);
            var player = Game.Player.Character;
            if (!IsOwnerActive(context) || player == null || !player.Exists() ||
                !GameUtils.IsWithin(player.Position, target, _radius))
            {
                // Leaving the zone restarts the hold; a progress bar that survives the
                // player walking away is a progress bar nobody has to defend.
                _startedAt = 0;
                return;
            }

            if (_startedAt == 0)
            {
                _startedAt = Game.GameTime;
                return;
            }

            int elapsed = (Game.GameTime - _startedAt) / 1000;
            if (elapsed >= _seconds)
            {
                Complete();
                return;
            }

            GameUtils.Subtitle(_progressText + "... " + (_seconds - elapsed) + "s", 500);
        }
    }

    /// <summary>Kill a specific set of peds.</summary>
    public sealed class KillTargetsObjective : Objective
    {
        private readonly Func<IEnumerable<Ped>> _targets;
        private readonly int _allowedSurvivors;
        private readonly bool _showCount;
        private readonly string _instruction;

        public KillTargetsObjective(string label, Func<IEnumerable<Ped>> targets,
            int allowedSurvivors = 0, bool showCount = true)
            : base(label)
        {
            _targets = targets;
            _allowedSurvivors = allowedSurvivors;
            _showCount = showCount; _instruction = label;
        }

        public override void Update(MissionContext context)
        {
            var targets = _targets().ToList();
            if (targets.Count == 0 || targets.Any(p => p == null || !p.Exists())) { Fail("A required hostile failed to load. Restart the mission."); return; }
            int alive = targets.Count(ped => ped.IsAlive);
            if (_showCount) Label = _instruction + " Remaining: " + alive;

            foreach (var target in _targets()) if (target != null && target.Exists() && target.IsAlive) ObjectiveMarkers.Show(target.Position, BlipColor.Red);
            if (alive <= _allowedSurvivors && IsOwnerActive(context))
            {
                Complete();
                return;
            }

            if (_showCount) GameUtils.Subtitle("~s~Hostiles: ~r~" + alive, 500);
        }
    }

    /// <summary>
    /// Put a set of guards down without requiring them dead. A stun gun does not kill,
    /// so a stealth mission scored on kills would be unfinishable as written.
    /// </summary>
    public sealed class SubdueTargetsObjective : Objective
    {
        private readonly HashSet<int> _subdued = new HashSet<int>();
        private readonly Func<IEnumerable<Ped>> _targets;

        public SubdueTargetsObjective(string label, Func<IEnumerable<Ped>> targets) : base(label)
        {
            _targets = targets;
        }

        public override void Update(MissionContext context)
        {
            var targets = _targets().ToList();
            if (targets.Count == 0 || targets.Any(p => p == null || !p.Exists())) { Fail("A required guard is missing. Restart the mission."); return; }
            if (targets.Any(p => p.IsDead)) { Fail("Keep the watchmen alive. Use the stun gun."); return; }
            foreach (var ped in targets)
            {
                if (ped.IsBeingStunned || ped.IsCuffed) _subdued.Add(ped.Handle);
                if (!_subdued.Contains(ped.Handle)) ObjectiveMarkers.Show(ped.Position, BlipColor.Yellow);
            }
            int left = targets.Count(p => !_subdued.Contains(p.Handle));
            Label = "Stun the marked guards; keep them alive. Remaining: " + left;
            if (left == 0 && IsOwnerActive(context)) Complete();
        }
    }

    /// <summary>Hold out against spawned waves. The spawner owns placement and models.</summary>
    public sealed class SurviveWavesObjective : Objective
    {
        private readonly Func<int, IEnumerable<Ped>> _spawnWave;
        private readonly int _waves;
        private readonly int _gapMs;

        private readonly List<Ped> _current = new List<Ped>();
        private int _wave;
        private int _clearedAt;

        public SurviveWavesObjective(string label, Func<int, IEnumerable<Ped>> spawnWave,
            int waves, int gapMs = 4000)
            : base(label)
        {
            _spawnWave = spawnWave;
            _waves = waves;
            _gapMs = gapMs;
        }

        /// <summary>Optional late-bound gap, for a mission whose earlier decision changes the response clock.</summary>
        public Func<int> GapProvider { get; set; }

        public override void Update(MissionContext context)
        {
            if (_current.Any(p => p == null || !p.Exists())) { Fail("A wave lost a required hostile. Restart the mission."); return; }
            _current.RemoveAll(ped => ped.IsDead);
            foreach (var ped in _current) ObjectiveMarkers.Show(ped.Position, BlipColor.Red);

            if (_current.Count > 0)
            {
                GameUtils.Subtitle("~s~Wave " + _wave + "/" + _waves + "   ~r~" + _current.Count + " left", 500);
                _clearedAt = 0;
                return;
            }

            if (_wave >= _waves)
            {
                Complete();
                return;
            }

            // A beat between waves, so the player gets to reload and breathe.
            if (_clearedAt == 0) _clearedAt = Game.GameTime;
            if (Game.GameTime - _clearedAt < (GapProvider?.Invoke() ?? _gapMs)) return;

            _wave++;
            _clearedAt = 0;
            _current.AddRange(_spawnWave(_wave).Where(ped => ped != null && ped.Exists()));
            if (_current.Count == 0) { Fail("The enemy wave failed to load. Restart the mission."); return; }
            Logger.Debug("Wave " + _wave + " spawned with " + _current.Count + " hostiles.");
        }

        public IEnumerable<Ped> Spawned => _current;
    }

    /// <summary>Get into a specific vehicle.</summary>
    public sealed class EnterVehicleObjective : Objective
    {
        private readonly Func<Vehicle> _vehicle;
        private readonly VehicleSeat _seat;
        private readonly bool _requireCrew;

        public EnterVehicleObjective(string label, Func<Vehicle> vehicle, VehicleSeat seat = VehicleSeat.Any, bool requireCrew = false)
            : base(label)
        {
            _vehicle = vehicle;
            _seat = seat;
            _requireCrew = requireCrew;
        }

        public override void Update(MissionContext context)
        {
            var vehicle = _vehicle();
            if (vehicle == null || !vehicle.Exists() || vehicle.IsDead || !vehicle.IsDriveable)
            {
                Fail("The required vehicle is gone or destroyed.");
                return;
            }

            GameUtils.DrawObjectiveMarker(vehicle.Position, Color.FromArgb(120, 214, 138, 58), 1.5f);

            ObjectiveMarkers.Navigation(vehicle.Position, RequiredCharacter);
            if (!IsOwnerActive(context)) return;

            var player = Game.Player.Character;
            if (player == null || !player.IsInVehicle(vehicle)) return;
            if (_seat != VehicleSeat.Any && vehicle.GetPedOnSeat(_seat) != player) return;

            if (_requireCrew)
                foreach (var hero in Crew.Protagonist.All)
                {
                    var ped = context.Crew.PedFor(hero.Slot);
                    if (ped == null || !ped.Exists() || ped.IsDead || !ped.IsInVehicle(vehicle)) return;
                }
            Complete();
        }
    }

    /// <summary>
    /// Run someone down. Completes when the target is dead or their vehicle is
    /// finished; fails if they stay out of range long enough to have genuinely
    /// escaped. The grace period matters — a chase where one corner ends the mission
    /// is a checkpoint reload, not a chase.
    /// </summary>
    public sealed class PursueTargetObjective : Objective
    {
        private readonly Func<Ped> _target;
        private readonly float _loseDistance;
        private readonly int _graceSeconds;
        private readonly string _failMessage;

        private int _outOfRangeSince;

        public PursueTargetObjective(string label, Func<Ped> target, string failMessage,
            float loseDistance = 260f, int graceSeconds = 10)
            : base(label)
        {
            _target = target;
            _failMessage = failMessage;
            _loseDistance = loseDistance;
            _graceSeconds = graceSeconds;
        }

        public override void Update(MissionContext context)
        {
            var target = _target();
            if (target == null || !target.Exists())
            {
                Fail("The target is missing. Restart this mission.");
                return;
            }

            if (target.IsDead)
            {
                Complete();
                return;
            }

            var vehicle = target.CurrentVehicle;
            if (vehicle != null && vehicle.Exists() && !vehicle.IsDriveable)
            {
                Complete();
                return;
            }

            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            ObjectiveMarkers.Navigation(target.Position);
            float distance = player.Position.DistanceTo(target.Position);
            GameUtils.Subtitle("~s~Distance: ~y~" + (int)distance + "m", 400);

            if (distance <= _loseDistance)
            {
                _outOfRangeSince = 0;
                return;
            }

            if (_outOfRangeSince == 0)
            {
                _outOfRangeSince = Game.GameTime;
                return;
            }

            if ((Game.GameTime - _outOfRangeSince) / 1000 >= _graceSeconds) Fail(_failMessage);
        }
    }

    /// <summary>Wreck a specific vehicle.</summary>
    public sealed class DestroyVehicleObjective : Objective
    {
        private readonly Func<Vehicle> _vehicle;

        public DestroyVehicleObjective(string label, Func<Vehicle> vehicle) : base(label)
        {
            _vehicle = vehicle;
        }

        public override void Update(MissionContext context)
        {
            var vehicle = _vehicle();
            if (vehicle == null || !vehicle.Exists()) { Fail("The target vehicle failed to load. Restart the mission."); return; }
            if (vehicle.IsDead || !vehicle.IsDriveable)
            {
                if (IsOwnerActive(context)) Complete();
                return;
            }
            GameUtils.DrawObjectiveMarker(vehicle.Position, Color.FromArgb(120, 224, 74, 62), 1.5f);
        }
    }

    /// <summary>Keep something alive. Passive — pair it with the objective that has the work.</summary>
    public sealed class ProtectObjective : Objective
    {
        public override bool IsPassive => true;

        private readonly Func<Entity> _entity;
        private readonly string _failMessage;
        private readonly float _healthFloor;

        public ProtectObjective(string label, Func<Entity> entity, string failMessage, float healthFloor = 0f)
            : base(label)
        {
            _entity = entity;
            _failMessage = failMessage;
            _healthFloor = healthFloor;
        }

        public override void Update(MissionContext context)
        {
            var entity = _entity();
            if (entity == null || !entity.Exists() || entity.IsDead)
            {
                Fail(_failMessage);
                return;
            }

            if (entity is Vehicle vehicle && (!vehicle.IsDriveable || vehicle.EngineHealth <= _healthFloor))
            {
                Fail(_failMessage);
            }
        }
    }

    /// <summary>Shake the police.</summary>
    public sealed class LoseWantedObjective : Objective
    {
        public LoseWantedObjective(string label = "Lose the cops.") : base(label)
        {
        }

        public override void Update(MissionContext context)
        {
            if (Game.Player.WantedLevel == 0) Complete();
        }
    }

    /// <summary>
    /// Switch to a named character. Used where the mission genuinely needs the handoff
    /// to be the beat — the tri-switch missions the campaign is built on.
    /// </summary>
    public sealed class SwitchCharacterObjective : Objective
    {
        private readonly Crew.CrewSlot _slot;

        public SwitchCharacterObjective(Crew.CrewSlot slot, string label)
            : base(label)
        {
            _slot = slot;
        }

        public override void Update(MissionContext context)
        {
            if (context.Crew.ActiveSlot == _slot) Complete();
        }
    }

    /// <summary>Hold a weapon on a target — sniper set-ups, EMP locks, "get eyes on".</summary>
    public sealed class AimAtObjective : Objective
    {
        private readonly Func<Entity> _target;
        private readonly int _seconds;
        private readonly float _range;

        private int _lockedAt;

        public AimAtObjective(string label, Func<Entity> target, int seconds = 1, float range = 250f)
            : base(label)
        {
            _target = target;
            _seconds = seconds;
            _range = range;
        }

        public override void Update(MissionContext context)
        {
            var target = _target();
            if (target == null || !target.Exists())
            {
                Fail("The target is gone.");
                return;
            }

            GameUtils.DrawObjectiveMarker(target.Position, Color.FromArgb(120, 224, 74, 62), 0.9f);

            if (!IsOwnerActive(context))
            {
                _lockedAt = 0;
                return;
            }

            var player = Game.Player.Character;
            bool lockedOn = player != null && player.IsAiming && Game.Player.IsTargeting(target)
                            && player.Position.DistanceTo(target.Position) < _range;

            if (!lockedOn)
            {
                _lockedAt = 0;
                return;
            }

            if (_lockedAt == 0)
            {
                _lockedAt = Game.GameTime;
                return;
            }

            if ((Game.GameTime - _lockedAt) / 1000 >= _seconds) Complete();
        }
    }

    /// <summary>
    /// A countdown the stage runs under. Fails the mission when it expires — the
    /// upload window in M02, the 300 seconds in M55.
    /// </summary>
    public sealed class TimerObjective : Objective
    {
        public override bool IsPassive => true;

        private readonly int _seconds;
        private readonly string _failMessage;
        private readonly bool _showClock;

        private int _startedAt;

        public TimerObjective(int seconds, string failMessage, bool showClock = true)
            : base("")
        {
            _seconds = seconds;
            _failMessage = failMessage;
            _showClock = showClock;
        }

        public int Remaining => Math.Max(0, _seconds - (Game.GameTime - _startedAt) / 1000);

        public override void Enter(MissionContext context)
        {
            _startedAt = Game.GameTime;
        }

        public override void Update(MissionContext context)
        {
            int remaining = Remaining;
            if (remaining <= 0)
            {
                Fail(_failMessage);
                return;
            }

            if (_showClock)
            {
                GameUtils.Subtitle("~s~" + remaining / 60 + ":" + (remaining % 60).ToString("00"), 400);
            }
        }
    }

    /// <summary>Drive through a sequence of checkpoints — races and set routes.</summary>
    public sealed class RaceCheckpointObjective : Objective
    {
        private readonly IList<Vector3> _checkpoints;
        private readonly float _radius;
        private readonly int _laps;

        private int _index;
        private int _lap = 1;

        private readonly Func<Vehicle> _vehicle;
        public RaceCheckpointObjective(string label, IList<Vector3> checkpoints, float radius = 12f, int laps = 1, Func<Vehicle> vehicle = null)
            : base(label)
        {
            _checkpoints = checkpoints;
            _radius = radius;
            _laps = laps;
            _vehicle = vehicle;
        }

        public override void Update(MissionContext context)
        {
            if (_checkpoints.Count == 0)
            {
                Complete();
                return;
            }

            var target = _checkpoints[_index];
            ObjectiveMarkers.Navigation(target, RequiredCharacter, _vehicle?.Invoke());
            GameUtils.DrawObjectiveMarker(target, Color.FromArgb(120, 232, 168, 56), _radius * 0.5f);
            GameUtils.Subtitle("~s~Lap " + _lap + "/" + _laps + "   checkpoint " + (_index + 1) + "/" + _checkpoints.Count, 500);

            ObjectiveMarkers.Navigation(target, RequiredCharacter);
            var player = Game.Player.Character;
            if (!IsOwnerActive(context) || player == null || !player.Exists() || !player.IsInVehicle() ||
                (_vehicle != null && !player.IsInVehicle(_vehicle())) ||
                !GameUtils.IsWithinFlat(player.Position, target, _radius)) return;

            _index++;
            if (_index < _checkpoints.Count) return;

            _index = 0;
            _lap++;
            if (_lap > _laps) Complete();
        }
    }
}
