using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
using GTA.Native;
using Bloodlines.Missions;

public static partial class StoryTests
{
    static Ped AwarenessGuard(Vector3 at, float heading = 0f) =>
        new Ped { Position = at, Heading = heading, Health = 220 };

    /// <summary>Advance the awareness clock far enough that a bounded review actually runs.</summary>
    static void AwarenessTick(GuardAwareness awareness, int times = 1, int stepMs = GuardAwareness.ReviewIntervalMs + 20)
    {
        for (int i = 0; i < times; i++) { Game.GameTime += stepMs; awareness.Update(); }
    }

    static void GuardAwarenessChecks()
    {
        // ---- The vision model: facing, range and an actual wall all matter.
        Reset();
        var watcher = AwarenessGuard(Vector3.Zero, 0f);
        var infront = new Ped { Position = new Vector3(0f, 20f, 0f) };
        var behind = new Ped { Position = new Vector3(0f, -20f, 0f) };
        var faraway = new Ped { Position = new Vector3(0f, 200f, 0f) };
        Function.ClearLos = true;
        Check(GuardAwareness.CanSee(watcher, infront, GuardAwareness.VisionRange),
            "A guard sees what is in front of him, in range, with a clear line");
        Check(!GuardAwareness.CanSee(watcher, behind, GuardAwareness.VisionRange),
            "He does not see through the back of his own head");
        Check(!GuardAwareness.CanSee(watcher, faraway, GuardAwareness.VisionRange),
            "Range is a limit, not a suggestion");
        Function.ClearLos = false;
        Check(!GuardAwareness.CanSee(watcher, infront, GuardAwareness.VisionRange),
            "A wall between them means he sees nothing, however well he is facing it");
        Function.ClearLos = true;
        Check(GuardAwareness.InView(watcher, new Vector3(0f, 20f, 0f), 55f) &&
              !GuardAwareness.InView(watcher, new Vector3(0f, -20f, 0f), 55f),
            "A reported position is judged on facing and range, because there is nothing to trace to");

        // ---- Escalation is graded. A noise is not a sighting.
        Reset();
        var hero = Game.Player.Character;
        hero.Position = new Vector3(0f, 300f, 0f);
        var post = AwarenessGuard(Vector3.Zero, 0f);
        var awareness = new GuardAwareness(() => new[] { hero });
        awareness.Track(post);
        Function.ClearLos = true;
        Check(awareness.Tracked == 1 && awareness.StateOf(post) == Alertness.Unaware,
            "A tracked guard starts unaware and doing his job");
        awareness.ReportToAll(Stimulus.SuppressedShot, new Vector3(0f, 12f, 0f));
        AwarenessTick(awareness);
        Check(awareness.StateOf(post) == Alertness.Unaware && awareness.SuspicionOf(post) > 0f,
            "A suppressed shot registers without telling him where anyone is");
        awareness.ReportToAll(Stimulus.GunshotHeard, new Vector3(0f, 30f, 0f));
        AwarenessTick(awareness);
        Check(awareness.StateOf(post) == Alertness.Investigating &&
              awareness.InterestOf(post).Y > 0f,
            "An unsuppressed shot he can hear sends him to look at where it came from");
        Check(awareness.LastStimulusOf(post) == Stimulus.GunshotHeard,
            "What moved him is recorded, so a mission can say why he is walking over");

        // ---- Suspicion decays when nothing feeds it: he goes back to work. It decays
        // over real frames rather than in one jump, because a long frame gap must not
        // be allowed to wipe what a guard knows.
        AwarenessTick(awareness, 34);
        Check(awareness.StateOf(post) == Alertness.Unaware && awareness.SuspicionOf(post) == 0f,
            "Left alone long enough, a guard calms all the way down");

        // ---- Being shot at is unambiguous, and this is the M31/M33/M37 report.
        Reset();
        hero = Game.Player.Character;
        hero.Position = new Vector3(0f, 8f, 0f);
        var shot = AwarenessGuard(Vector3.Zero, 0f);
        awareness = new GuardAwareness(() => new[] { hero });
        awareness.Track(shot);
        Function.ClearLos = false;                       // He cannot even see who hit him.
        Function.TestDamage.Add(shot.Handle);
        AwarenessTick(awareness);
        Check(awareness.StateOf(shot) >= Alertness.Detected,
            "A guard who is shot at reacts, with no mission flag required and no line of sight");
        Check(shot.Task.LastTarget == hero && shot.Task.Fights == 1,
            "He fights the person shooting him, on one order rather than none");

        // ---- The combat task is issued once, not every frame. This is the actual defect.
        int tasked = shot.Task.Fights;
        AwarenessTick(awareness, 6);
        Check(shot.Task.Fights == tasked,
            "An engaged guard is not re-tasked every tick, which is what stopped him fighting at all");

        // ---- A downed guard is found, and one who sees it calls it in.
        Reset();
        hero = Game.Player.Character;
        hero.Position = new Vector3(0f, 400f, 0f);
        var body = AwarenessGuard(new Vector3(0f, 10f, 0f), 0f);
        var finder = AwarenessGuard(Vector3.Zero, 0f);
        var distant = AwarenessGuard(new Vector3(60f, 0f, 0f), 0f);
        awareness = new GuardAwareness(() => new[] { hero });
        awareness.TrackAll(new[] { body, finder, distant });
        Function.ClearLos = true;
        body.IsDead = true;
        AwarenessTick(awareness, 3);
        Check(awareness.StateOf(finder) == Alertness.Alarmed && awareness.Alarmed,
            "Finding one of his own down alarms him outright");
        Check(awareness.StateOf(distant) >= Alertness.Investigating,
            "The alarm reaches the guard across the yard by radio, not by magic");

        // ---- An alarm does not un-ring.
        AwarenessTick(awareness, 40);
        Check(awareness.StateOf(finder) == Alertness.Alarmed,
            "A site that knows stays knowing, however quiet it goes");

        // ---- Suppression: the hook Gohan's blackout needs.
        Reset();
        hero = Game.Player.Character;
        hero.Position = new Vector3(0f, 10f, 0f);
        var blinded = AwarenessGuard(Vector3.Zero, 0f);
        var neighbor = AwarenessGuard(new Vector3(20f, 0f, 0f), 0f);
        awareness = new GuardAwareness(() => new[] { hero }) { Suppressed = true };
        awareness.TrackAll(new[] { blinded, neighbor });
        Function.ClearLos = true;
        AwarenessTick(awareness, 4);
        Check(awareness.StateOf(blinded) == Alertness.Unaware && awareness.SuspicionOf(blinded) == 0f,
            "Suppressed, a guard looking straight at someone resolves nothing");
        Function.TestDamage.Add(blinded.Handle);
        AwarenessTick(awareness);
        Check(awareness.StateOf(blinded) >= Alertness.Detected,
            "A blackout does not make a bullet ambiguous: being hit still reaches him");
        Check(awareness.StateOf(neighbor) == Alertness.Unaware,
            "But he cannot call it in, so the man beside him never learns");

        // ---- Bounded work: a crowd costs the same as a few.
        Reset();
        hero = Game.Player.Character;
        hero.Position = new Vector3(0f, 500f, 0f);
        awareness = new GuardAwareness(() => new[] { hero });
        var crowd = new List<Ped>();
        for (int i = 0; i < 40; i++) crowd.Add(AwarenessGuard(new Vector3(i * 4f, 0f, 0f), 0f));
        awareness.TrackAll(crowd);
        awareness.Track(crowd[0]);
        Check(awareness.Tracked == 40, "Tracking the same guard twice does not double him");
        int before = Function.Calls.Count;
        AwarenessTick(awareness);
        int during = Function.Calls.Count - before;
        Check(during <= GuardAwareness.GuardsPerTick * 6,
            "One frame reviews a bounded slice of the guards rather than all forty [" + during + " native calls]");

        // ---- Releasing and clearing.
        awareness.Release(crowd[0]);
        Check(awareness.Tracked == 39 && awareness.StateOf(crowd[0]) == Alertness.Unaware,
            "A released guard is forgotten and reports as unaware");
        awareness.Clear();
        Check(awareness.Tracked == 0 && !awareness.Alarmed, "Clearing drops every guard and the alarm with them");

        // ---- The weighting itself: distance matters, and nonsense is worth nothing.
        Check(GuardAwareness.Weight(Stimulus.Sighting, 1f) > GuardAwareness.Weight(Stimulus.Sighting, 50f),
            "A sighting up close is worth more than a shape at the edge of his range");
        Check(GuardAwareness.Weight(Stimulus.Sighting, GuardAwareness.VisionRange + 1f) == 0f &&
              GuardAwareness.Weight(Stimulus.GunshotHeard, GuardAwareness.GunshotRange + 1f) == 0f,
            "Out of range is worth nothing at all, rather than a little");
        Check(GuardAwareness.Weight(Stimulus.UnderFire, 999f) >= GuardAwareness.DetectedAt,
            "Taking fire is enough on its own, at any distance");
        Check(Math.Abs(GuardAwareness.Difference(10f, 350f) - 20f) < 0.01f &&
              Math.Abs(GuardAwareness.Difference(350f, 10f) + 20f) < 0.01f,
            "Heading arithmetic wraps around north instead of reading 340 degrees apart");
    }

    /// <summary>
    /// A real preparation mission with one planted hostile, so the wiring can be proved
    /// rather than the framework alone. Exposes only what the checks need to see.
    /// </summary>
    sealed class AwarenessProbe : Bloodlines.Missions.Campaign.PreparationOperation
    {
        public override string Id => "M31";
        public override string Title => "Awareness probe";
        protected override bool Setup() => BeginCrew(CrewSlot.Guess);
        protected override IEnumerable<Bloodlines.Missions.Objectives.MissionStage> BuildStages()
        { yield return new Bloodlines.Missions.Objectives.MissionStage("Hold", new Bloodlines.Missions.Objectives.ConditionObjective("Hold", () => false)); }
        public Ped Plant(Ped guard) { Opposition.Add(guard); return guard; }
        public GuardAwareness Hostiles => Awareness;
        public bool IsFighting => Fighting;
        public void Declare(bool value) => Fighting = value;
    }

    static void GuardAwarenessWiringChecks()
    {
        // ---- A planted hostile in a real mission reacts to being shot at, with no
        // mission flag set. This is exactly what M31, M33 and M37 did not do.
        Reset();
        var crew = Roster();
        var c = Context(crew);
        c.State = CampaignState.Load(System.IO.Path.Combine(root, "awareness-probe.json"));
        World.CollisionReady = true;
        var probe = new AwarenessProbe();
        Check(probe.Begin(c), "The probe mission starts");
        Check(!probe.IsFighting, "It has not declared a fight, which is the condition the old code required");
        var guard = probe.Plant(new Ped { Position = crew.PedFor(CrewSlot.Guess).Position + new Vector3(0f, 6f, 0f), Health = 220 });
        Function.ClearLos = false;
        Function.TestDamage.Add(guard.Handle);
        for (int i = 0; i < 4; i++) { Game.GameTime += GuardAwareness.ReviewIntervalMs + 20; probe.Tick(); }
        Check(probe.Hostiles.StateOf(guard) >= Alertness.Detected,
            "A hostile who is shot at fights back even though the mission never declared combat");
        Check(guard.Task.Fights == 1,
            "And he is given that order once, not once per frame");

        // ---- A mission declaring a fight still engages everyone, through the model.
        Reset();
        crew = Roster();
        c = Context(crew);
        c.State = CampaignState.Load(System.IO.Path.Combine(root, "awareness-declared.json"));
        World.CollisionReady = true;
        var declared = new AwarenessProbe();
        Check(declared.Begin(c), "The probe mission starts for the declared-fight case");
        var unseen = declared.Plant(new Ped { Position = crew.PedFor(CrewSlot.Guess).Position + new Vector3(0f, 40f, 0f), Health = 220 });
        Function.ClearLos = false;
        Game.GameTime += GuardAwareness.ReviewIntervalMs + 20; declared.Tick();
        Check(declared.Hostiles.StateOf(unseen) == Alertness.Unaware,
            "Before the fight is declared he is unaware, because he cannot see anyone");
        declared.Declare(true);
        for (int i = 0; i < 3; i++) { Game.GameTime += GuardAwareness.ReviewIntervalMs + 20; declared.Tick(); }
        Check(declared.Hostiles.StateOf(unseen) >= Alertness.Detected && unseen.Task.Fights >= 1,
            "A mission that declares a fight reaches its hostiles as a radio call, so they engage");
    }
}
