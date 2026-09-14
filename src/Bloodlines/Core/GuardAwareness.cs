using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>What a mission guard currently believes.</summary>
    public enum Alertness
    {
        /// <summary>Nothing has happened. He is doing his job.</summary>
        Unaware,
        /// <summary>Something registered. He turns toward it but does not leave his post.</summary>
        Suspicious,
        /// <summary>Enough to walk over and look.</summary>
        Investigating,
        /// <summary>He has the player. He fights.</summary>
        Detected,
        /// <summary>He has told everyone. The site knows, and it stays knowing.</summary>
        Alarmed,
    }

    /// <summary>Why a guard's suspicion moved.</summary>
    public enum Stimulus
    {
        /// <summary>Seen directly: in the cone, in range, with line of sight.</summary>
        Sighting,
        /// <summary>A shot he could hear but not place. No line of sight needed.</summary>
        GunshotHeard,
        /// <summary>A suppressed shot. Carries a fraction of the distance.</summary>
        SuppressedShot,
        /// <summary>He took a hit, or one landed beside him. Nothing is ambiguous about this.</summary>
        UnderFire,
        /// <summary>He found one of his own down.</summary>
        BodyFound,
        /// <summary>Another guard called it in. This is what a radio is for.</summary>
        RadioCall,
        /// <summary>Something is where it should not be: a vehicle inside the line, a door open.</summary>
        Trespass,
    }

    /// <summary>
    /// Awareness for mission guards, which the campaign did not have. Enemy reaction
    /// used to be one mission-wide <c>Fighting</c> flag: with it off a guard being shot
    /// at did nothing at all, and with it on every guard was handed
    /// <c>Task.FightAgainst</c> on every tick, which restarts the combat task before
    /// the ped can act on it. Between them that is why the guards in M31, M33 and M37
    /// stood still while Ice shot at them.
    ///
    /// What this owns: how much each tracked guard knows, what moved it, and — the part
    /// that actually fixes the reports — issuing a combat task only when his state
    /// changes or his target goes stale, never every frame.
    ///
    /// What it deliberately does not own: police. <see cref="Crew.TacticalResponse"/>
    /// and the military dispatch keep the wanted system; this is the mission's own
    /// hostiles, who have no dispatch and no stars.
    ///
    /// Bounded on purpose: at most <see cref="GuardsPerTick"/> guards are re-evaluated
    /// in a frame and each one no more often than <see cref="ReviewIntervalMs"/>, so a
    /// compound full of guards costs the same as a handful.
    /// </summary>
    public sealed class GuardAwareness
    {
        /// <summary>Suspicion at which each state begins. Falling out of a state needs a margin.</summary>
        public const float SuspiciousAt = 20f;
        public const float InvestigatingAt = 50f;
        public const float DetectedAt = 80f;
        /// <summary>Hysteresis: he does not flicker back the instant he dips below a line.</summary>
        public const float CalmMargin = 8f;
        /// <summary>Suspicion lost per second with nothing feeding it.</summary>
        public const float DecayPerSecond = 9f;
        /// <summary>How wide his attention is, in degrees either side of where he faces.</summary>
        public const float VisionHalfAngle = 62f;
        public const float VisionRange = 55f;
        /// <summary>How far an ordinary shot carries as something worth turning around for.</summary>
        public const float GunshotRange = 90f;
        public const float SuppressedRange = 22f;
        public const int GuardsPerTick = 8;
        public const int ReviewIntervalMs = 250;
        /// <summary>How stale a combat target may get before it is refreshed.</summary>
        public const int RetargetIntervalMs = 2500;

        private sealed class Watch
        {
            public Ped Guard;
            public float Suspicion;
            public Alertness State;
            public Stimulus LastStimulus;
            public Vector3 Interest;
            public int ReviewedAt;
            public int TaskedAt;
            public Ped Target;
            public bool Reported;
        }

        private readonly List<Watch> _watches = new List<Watch>();
        private readonly Func<IEnumerable<Ped>> _heroes;
        private int _cursor;
        private int _lastTick;

        /// <param name="heroes">The people a guard can catch sight of. Usually the deployed crew.</param>
        public GuardAwareness(Func<IEnumerable<Ped>> heroes)
        {
            _heroes = heroes ?? throw new ArgumentNullException(nameof(heroes));
        }

        /// <summary>
        /// While this is set, a guard's suspicion decays instead of climbing and no
        /// radio call propagates: he may be looking straight at someone and still not
        /// resolve it. This is the hook Gohan's blackout uses, and it is why the
        /// framework had to exist before that ability could.
        /// </summary>
        public bool Suppressed { get; set; }

        /// <summary>True once any guard has called it in. An alarm does not un-ring.</summary>
        public bool Alarmed => _watches.Any(w => w.State == Alertness.Alarmed);
        public int Tracked => _watches.Count(w => Alive(w.Guard));

        public void Track(Ped guard)
        {
            if (guard == null || !guard.Exists()) return;
            if (_watches.Any(w => w.Guard != null && w.Guard.Handle == guard.Handle)) return;
            _watches.Add(new Watch { Guard = guard, State = Alertness.Unaware, Interest = guard.Position });
        }

        public void TrackAll(IEnumerable<Ped> guards)
        {
            if (guards == null) return;
            foreach (var guard in guards) Track(guard);
        }

        public void Release(Ped guard)
        {
            if (guard == null) return;
            _watches.RemoveAll(w => w.Guard == null || w.Guard.Handle == guard.Handle);
        }

        public void Clear() { _watches.Clear(); _cursor = 0; }

        public Alertness StateOf(Ped guard) => Find(guard)?.State ?? Alertness.Unaware;
        public float SuspicionOf(Ped guard) => Find(guard)?.Suspicion ?? 0f;
        public Stimulus? LastStimulusOf(Ped guard) => Find(guard)?.LastStimulus;
        /// <summary>Where he last thought something was, which is where he walks when investigating.</summary>
        public Vector3 InterestOf(Ped guard) => Find(guard)?.Interest ?? Vector3.Zero;

        private Watch Find(Ped guard) =>
            guard == null ? null : _watches.FirstOrDefault(w => w.Guard != null && w.Guard.Handle == guard.Handle);

        private static bool Alive(Ped ped) => ped != null && ped.Exists() && !ped.IsDead;

        /// <summary>How much one stimulus is worth at this distance, before suppression.</summary>
        public static float Weight(Stimulus stimulus, float distance)
        {
            switch (stimulus)
            {
                case Stimulus.UnderFire: return 100f;
                case Stimulus.BodyFound: return 100f;
                case Stimulus.RadioCall: return 85f;
                case Stimulus.Sighting:
                    if (distance > VisionRange) return 0f;
                    // Close enough to be certain; far enough to be a shape that needs a second look.
                    return 26f + 44f * (1f - Math.Min(1f, distance / VisionRange));
                case Stimulus.GunshotHeard:
                    if (distance > GunshotRange) return 0f;
                    return 30f + 40f * (1f - Math.Min(1f, distance / GunshotRange));
                case Stimulus.SuppressedShot:
                    if (distance > SuppressedRange) return 0f;
                    return 14f * (1f - Math.Min(1f, distance / SuppressedRange));
                case Stimulus.Trespass: return distance > VisionRange ? 0f : 22f;
                default: return 0f;
            }
        }

        /// <summary>
        /// Tell one guard something happened. Mission code calls this for the things
        /// only it knows about — a charge going off, a door opened, its own alarm.
        /// </summary>
        public void Report(Ped guard, Stimulus stimulus, Vector3 at)
        {
            var watch = Find(guard);
            if (watch == null || !Alive(guard)) return;
            Feed(watch, stimulus, at, guard.Position.DistanceTo(at));
        }

        /// <summary>Tell every tracked guard in earshot. A shot is not addressed to anyone.</summary>
        public void ReportToAll(Stimulus stimulus, Vector3 at)
        {
            foreach (var watch in _watches.Where(w => Alive(w.Guard)).ToArray())
                Feed(watch, stimulus, at, watch.Guard.Position.DistanceTo(at));
        }

        private void Feed(Watch watch, Stimulus stimulus, Vector3 at, float distance)
        {
            float weight = Weight(stimulus, distance);
            if (weight <= 0f) return;
            // Suppressed: he registers nothing new. Being shot is the exception — a
            // blackout does not make a bullet ambiguous.
            if (Suppressed && stimulus != Stimulus.UnderFire) return;
            watch.Suspicion = Math.Min(100f, watch.Suspicion + weight);
            watch.LastStimulus = stimulus;
            watch.Interest = at;
        }

        /// <summary>
        /// One frame. Reviews a bounded slice of the guards, decays the rest, and
        /// gives a combat task only to a guard whose state or target actually changed.
        /// </summary>
        public void Update()
        {
            int now = Game.GameTime;
            float seconds = _lastTick == 0 ? 0f : Math.Max(0f, Math.Min(1f, (now - _lastTick) / 1000f));
            _lastTick = now;

            _watches.RemoveAll(w => w.Guard == null || !w.Guard.Exists());
            if (_watches.Count == 0) return;

            var heroes = (_heroes() ?? Enumerable.Empty<Ped>()).Where(Alive).ToArray();

            // Decay first, for everyone: a guard nobody is feeding calms down whether or
            // not his slice came up this frame.
            foreach (var watch in _watches)
            {
                if (!Alive(watch.Guard)) continue;
                if (watch.State == Alertness.Alarmed) continue;
                float loss = DecayPerSecond * seconds * (Suppressed ? 2.5f : 1f);
                watch.Suspicion = Math.Max(0f, watch.Suspicion - loss);
            }

            int reviewed = 0;
            for (int i = 0; i < _watches.Count && reviewed < GuardsPerTick; i++)
            {
                _cursor = (_cursor + 1) % _watches.Count;
                var watch = _watches[_cursor];
                if (!Alive(watch.Guard)) continue;
                if (now - watch.ReviewedAt < ReviewIntervalMs) continue;
                watch.ReviewedAt = now;
                reviewed++;
                Review(watch, heroes, now);
            }
        }

        private void Review(Watch watch, Ped[] heroes, int now)
        {
            // Did he take fire? Nothing else needs deciding if so.
            foreach (var hero in heroes)
                if (Function.Call<bool>(Hash.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY, watch.Guard, hero, true))
                {
                    Function.Call(Hash.CLEAR_ENTITY_LAST_DAMAGE_ENTITY, watch.Guard);
                    Feed(watch, Stimulus.UnderFire, hero.Position, watch.Guard.Position.DistanceTo(hero.Position));
                    break;
                }

            // One of his own on the ground, in view.
            if (!Suppressed && watch.State < Alertness.Detected)
                foreach (var other in _watches)
                {
                    if (other == watch || other.Guard == null || !other.Guard.Exists() || !other.Guard.IsDead) continue;
                    if (!CanSee(watch.Guard, other.Guard, 28f)) continue;
                    Feed(watch, Stimulus.BodyFound, other.Guard.Position, watch.Guard.Position.DistanceTo(other.Guard.Position));
                    break;
                }

            // Sight. The cone and the line of sight both have to hold.
            var seen = heroes.FirstOrDefault(h => CanSee(watch.Guard, h, VisionRange));
            if (seen != null) Feed(watch, Stimulus.Sighting, seen.Position, watch.Guard.Position.DistanceTo(seen.Position));

            var before = watch.State;
            watch.State = Resolve(watch);

            // A guard who reaches Detected calls it in once, and that is what brings the
            // rest. Radio is the only way his knowledge leaves his own head.
            if (watch.State >= Alertness.Detected && !watch.Reported)
            {
                watch.Reported = true;
                if (!Suppressed)
                {
                    watch.State = Alertness.Alarmed;
                    foreach (var other in _watches)
                        if (other != watch && Alive(other.Guard) && other.Guard.Position.DistanceTo(watch.Interest) < 120f)
                            Feed(other, Stimulus.RadioCall, watch.Interest, 0f);
                }
            }

            Act(watch, heroes, now, before);
        }

        private Alertness Resolve(Watch watch)
        {
            if (watch.State == Alertness.Alarmed) return Alertness.Alarmed;
            float s = watch.Suspicion;
            // Climbing uses the thresholds; falling needs to clear them by the margin.
            if (s >= DetectedAt) return Alertness.Detected;
            if (s >= InvestigatingAt) return Alertness.Investigating;
            if (s >= SuspiciousAt) return Alertness.Suspicious;
            if (watch.State == Alertness.Suspicious && s > SuspiciousAt - CalmMargin) return Alertness.Suspicious;
            if (watch.State == Alertness.Investigating && s > InvestigatingAt - CalmMargin) return Alertness.Investigating;
            if (watch.State == Alertness.Detected && s > DetectedAt - CalmMargin) return Alertness.Detected;
            return Alertness.Unaware;
        }

        /// <summary>
        /// Give him a task, but only when something changed. Re-issuing a combat task
        /// every frame is what stopped the old guards fighting at all.
        /// </summary>
        private void Act(Watch watch, Ped[] heroes, int now, Alertness before)
        {
            var guard = watch.Guard;
            bool fighting = watch.State >= Alertness.Detected;
            if (fighting)
            {
                var target = heroes.OrderBy(h => h.Position.DistanceTo(guard.Position)).FirstOrDefault();
                if (target == null) return;
                // Our own record of what he was told decides this, not the engine's
                // combat flag: the order is authoritative and the native is a hint.
                // Re-issue only when the target actually changed, or when the order has
                // gone stale and he is visibly not fighting, which recovers a task some
                // other system cleared. Anything else and he keeps the order he has.
                bool sameTarget = watch.Target != null && Alive(watch.Target) && watch.Target.Handle == target.Handle;
                bool fresh = now - watch.TaskedAt < RetargetIntervalMs;
                bool engaged = Function.Call<bool>(Hash.IS_PED_IN_COMBAT, guard, target);
                if (sameTarget && (fresh || engaged)) return;
                watch.Target = target;
                watch.TaskedAt = now;
                guard.Task.FightAgainst(target);
                return;
            }

            if (watch.State == before) return;
            watch.Target = null;
            switch (watch.State)
            {
                case Alertness.Suspicious:
                    // He looks. He does not abandon his post over a noise.
                    guard.Task.ClearAll();
                    guard.Heading = HeadingTo(guard.Position, watch.Interest);
                    guard.Task.GuardCurrentPosition();
                    break;
                case Alertness.Investigating:
                    guard.Task.ClearAll();
                    guard.Task.GoTo(watch.Interest);
                    break;
                case Alertness.Unaware:
                    guard.Task.ClearAll();
                    guard.Task.GuardCurrentPosition();
                    break;
            }
        }

        /// <summary>
        /// In the cone, inside the range, and with a real line of sight to the thing.
        /// Line of sight is an entity-to-entity question, which is why this takes the
        /// entity rather than its coordinates: a wall between them has to count.
        /// </summary>
        public static bool CanSee(Ped guard, Entity target, float range)
        {
            if (target == null || !target.Exists()) return false;
            if (!InView(guard, target.Position, range)) return false;
            return Function.Call<bool>(Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY, guard, target, 17);
        }

        /// <summary>
        /// Facing and range only, with no line-of-sight test. For a position rather
        /// than a thing — a reported noise, a marker — where there is nothing to trace
        /// to. Never use this to decide that a guard can see a person.
        /// </summary>
        public static bool InView(Ped guard, Vector3 point, float range)
        {
            if (guard == null || !guard.Exists() || guard.IsDead) return false;
            float distance = (point - guard.Position).Length();
            if (distance > range) return false;
            if (distance <= 1.5f) return true;
            return Math.Abs(Difference(HeadingTo(guard.Position, point), guard.Heading)) <= VisionHalfAngle;
        }

        private static float HeadingTo(Vector3 from, Vector3 to)
        {
            var d = to - from;
            float heading = (float)(Math.Atan2(-d.X, d.Y) * 180.0 / Math.PI);
            return heading < 0f ? heading + 360f : heading;
        }

        /// <summary>Signed degrees between two headings, in the range -180 to 180.</summary>
        public static float Difference(float a, float b)
        {
            float d = (a - b) % 360f;
            if (d > 180f) d -= 360f;
            if (d < -180f) d += 360f;
            return d;
        }
    }
}
