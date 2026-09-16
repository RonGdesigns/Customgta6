using System;
using System.Collections.Generic;
using Bloodlines.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Crew
{
    /// <summary>
    /// What a brother does with himself while he is standing by.
    ///
    /// Ron's report: "my teammates just sit around while I'm doing other tasks — it would
    /// be more immersive if they didn't just stand stagnant for most of the missions,
    /// unless they were standing by and it was nothing else to do." The second half of
    /// that sentence is the design. Standing by is fine; standing by like a mannequin is
    /// not.
    ///
    /// The cause was structural rather than an oversight. <c>Station</c> calls
    /// <c>TakeControl</c>, clears the ped's tasks, walks him to his post — and from that
    /// moment the companion controller keeps its hands off him, because the mission owns
    /// him. Nothing ever speaks to him again. He stands at his post for the rest of the
    /// chapter, facing whichever way he arrived.
    ///
    /// Who may be animated is decided by the caller, and there are exactly three ways in.
    /// A mission that parks a brother registers him through <see cref="StandBy"/>; a
    /// mission that hands him a real task never registers him, and nothing here can touch
    /// him. The controller animates a slot it is itself holding, which previously got a
    /// bare <c>GuardCurrentPosition</c> and nothing else for the rest of the chapter. And
    /// a follower settles only while the man he is with has stopped walking. That is the
    /// safety argument: <b>ambience only ever replaces standing still.</b>
    ///
    /// Two behaviors, chosen by what is around him:
    ///
    ///  * **Alert** — a hostile within <see cref="AlertRange"/>, or the man he is covering
    ///    is in a fight. He stands guard with his weapon, scanning. Nobody smokes a
    ///    cigarette next to a firefight.
    ///  * **Relaxed** — nothing going on. A posture that belongs to that brother: Ice
    ///    watches the ground through his optic, Gohan works a phone, Guess leans and
    ///    smokes. Small, but it is the difference between three men and three props.
    ///
    /// A posture is issued **once**, on a change of mood or a genuinely stale order — the
    /// same rule <see cref="GuardAwareness"/> exists to enforce for hostiles. Re-issuing a
    /// scenario every tick restarts the animation before it plays, which looks worse than
    /// the statue it replaced.
    /// </summary>
    public sealed class CompanionPresence
    {
        /// <summary>How long a posture is left alone before it is reconsidered.</summary>
        public const int ReviewMs = 11000;
        /// <summary>How long a scenario gets to start before it is treated as refused.</summary>
        public const int SettleMs = 2500;
        /// <summary>A hostile inside this is a reason to stand guard rather than relax.</summary>
        public const float AlertRange = 50f;
        /// <summary>How far the man he is with may move before a follower stops posing and follows again.</summary>
        public const float LeaderDrift = 3f;
        /// <summary>How close a follower has to be to the leader before he is allowed to settle at all.</summary>
        public const float FollowSettle = 9f;

        /// <summary>Weapon out, watching. The one posture that is right next to trouble.</summary>
        public const string AlertScenario = "WORLD_HUMAN_GUARD_STAND";

        /// <summary>
        /// Who each brother is when he has nothing to do. These are the game's own ambient
        /// scenarios; an unknown name, or one the spot will not take — leaning needs a wall
        /// — simply does not start, which is what the verification below is for.
        /// </summary>
        private static readonly Dictionary<CrewSlot, string[]> Relaxed = new Dictionary<CrewSlot, string[]>
        {
            { CrewSlot.Ice, new[] { "WORLD_HUMAN_BINOCULARS", "WORLD_HUMAN_GUARD_STAND", "WORLD_HUMAN_SMOKING" } },
            { CrewSlot.Gohan, new[] { "WORLD_HUMAN_STAND_MOBILE", "WORLD_HUMAN_CLIPBOARD", "WORLD_HUMAN_STAND_IMPATIENT" } },
            { CrewSlot.Guess, new[] { "WORLD_HUMAN_SMOKING", "WORLD_HUMAN_LEANING", "WORLD_HUMAN_HANG_OUT_STREET" } },
        };
        private static readonly string[] Anyone = { "WORLD_HUMAN_STAND_IMPATIENT", "WORLD_HUMAN_SMOKING" };

        private sealed class Post
        {
            public bool Registered;
            public int NextReview, IssuedAt;
            public bool Alert, Verified;
            public string Scenario;
            public Vector3 LeaderAt;
        }

        private readonly Dictionary<CrewSlot, Post> _posts = new Dictionary<CrewSlot, Post>();
        private readonly Random _random = new Random();

        /// <summary>Told by the host so a crew member is never mistaken for a hostile.</summary>
        public Func<Ped, bool> IsCrewMember { get; set; }
        /// <summary>Off switch, for a playtest that wants the old statues back.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// A mission says: this brother is parked here with nothing to do. Until it says
        /// otherwise he may be given something to look like he is doing.
        /// </summary>
        public void StandBy(CrewSlot slot) { Get(slot).Registered = true; Get(slot).NextReview = 0; }
        /// <summary>He has a job again, or the mission is over. Nothing here touches him.</summary>
        public void Release(CrewSlot slot) { if (_posts.TryGetValue(slot, out var post)) { post.Registered = false; post.Scenario = null; } }
        public void Clear() { _posts.Clear(); }
        /// <summary>Whether a brother is currently registered as standing by.</summary>
        public bool IsStandingBy(CrewSlot slot) => _posts.TryGetValue(slot, out var post) && post.Registered;
        /// <summary>What he is doing, for the log and for a test.</summary>
        public string PostureOf(CrewSlot slot) => _posts.TryGetValue(slot, out var post) ? post.Scenario : null;

        private Post Get(CrewSlot slot)
        {
            if (!_posts.TryGetValue(slot, out var post)) _posts[slot] = post = new Post();
            return post;
        }

        /// <summary>
        /// Give him something to do, or leave him alone. Returns true when this owns his
        /// posture right now, so the caller does not re-issue a follow or a guard over it.
        /// </summary>
        /// <param name="parked">
        /// Whether he is meant to be standing still: a mission station, or the controller's
        /// own hold. A follower is only allowed to settle once the man he is with has
        /// stopped moving, and goes straight back to following the moment he starts again.
        /// </param>
        public bool Update(CrewSlot slot, Ped companion, Ped leader, bool parked)
        {
            if (!Enabled) return false;
            if (companion == null || !companion.Exists() || companion.IsDead) return false;
            // Anything real beats ambience: a fight, a car, a ragdoll, a swim.
            if (companion.IsInVehicle() || companion.IsInCombat || companion.IsInWater || companion.IsRagdoll) { Release(slot); return false; }

            var post = Get(slot);
            // Who may be animated is the caller's decision, not this class's: the
            // controller calls for a slot it is itself holding, and for a scripted slot
            // only when a mission registered it through StandBy. A follower is the one
            // case decided here, because it depends on what the leader is doing.
            if (!parked)
            {
                // A follower poses only while the man he is with is standing still, and
                // stops the moment he walks off. Without this he would stand smoking while
                // the player leaves the building.
                if (leader == null || !leader.Exists()) return false;
                if (companion.Position.DistanceTo(leader.Position) > FollowSettle) { post.Scenario = null; return false; }
                if (post.Scenario != null && leader.Position.DistanceTo(post.LeaderAt) > LeaderDrift)
                { post.Scenario = null; post.NextReview = 0; return false; }
            }

            bool alert = NearTrouble(companion, leader);
            bool moodChanged = post.Scenario != null && alert != post.Alert;
            if (post.Scenario != null && !moodChanged && Game.GameTime < post.NextReview)
            {
                Verify(slot, companion, post);
                return true;
            }

            string scenario = alert ? AlertScenario : Pick(slot);
            post.Alert = alert;
            post.Scenario = scenario;
            post.IssuedAt = Game.GameTime;
            post.NextReview = Game.GameTime + ReviewMs + _random.Next(0, 4000);
            post.Verified = false;
            post.LeaderAt = leader != null && leader.Exists() ? leader.Position : companion.Position;
            try
            {
                companion.Task.ClearAll();
                // In place: he keeps the spot the mission put him on. A scenario that moved
                // him would be this system relocating somebody else's actor.
                companion.Task.StartScenario(scenario, companion.Position, companion.Heading);
                companion.AlwaysKeepTask = true;
            }
            catch (Exception ex)
            {
                Logger.Warn("Companion posture " + scenario + " for " + slot + " could not start: " + ex.Message);
                post.Scenario = null;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Did the scenario actually take? Some will not start where he is standing, and a
        /// posture that silently refused leaves him exactly as stagnant as before. He gets
        /// a guard stance instead, which works anywhere a man can stand.
        /// </summary>
        private void Verify(CrewSlot slot, Ped companion, Post post)
        {
            if (post.Verified || Game.GameTime - post.IssuedAt < SettleMs) return;
            post.Verified = true;
            bool playing;
            try { playing = Function.Call<bool>(Hash.IS_PED_USING_ANY_SCENARIO, companion); }
            catch { return; }
            if (playing) return;
            Logger.Info("Companion posture " + post.Scenario + " would not start for " + slot + "; standing guard instead.");
            post.Scenario = AlertScenario;
            try { companion.Task.ClearAll(); companion.Task.GuardCurrentPosition(); }
            catch (Exception ex) { Logger.Error("Falling back to a guard stance for " + slot, ex); }
        }

        private string Pick(CrewSlot slot)
        {
            var set = Relaxed.TryGetValue(slot, out var his) ? his : Anyone;
            return set[_random.Next(set.Length)];
        }

        /// <summary>
        /// Is there anything to be alert about: somebody hostile close by, or the man he is
        /// covering already in a fight. Deliberately cheap — this runs on the review clock,
        /// not every frame.
        /// </summary>
        private bool NearTrouble(Ped companion, Ped leader)
        {
            if (leader != null && leader.Exists() && leader.IsInCombat) return true;
            try
            {
                foreach (var other in World.GetNearbyPeds(companion, AlertRange))
                {
                    if (other == null || !other.Exists() || other.IsDead || other.Handle == companion.Handle) continue;
                    if (IsCrewMember != null && IsCrewMember(other)) continue;
                    if (other.RelationshipGroup == companion.RelationshipGroup) continue;
                    if (other.GetRelationshipWithPed(companion) == Relationship.Hate ||
                        companion.GetRelationshipWithPed(other) == Relationship.Hate) return true;
                }
            }
            catch (Exception ex) { Logger.Warn("Looking for trouble near a companion: " + ex.Message); }
            return false;
        }
    }
}
