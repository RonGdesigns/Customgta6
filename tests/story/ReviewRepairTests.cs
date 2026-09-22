using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Native;

public static partial class StoryTests
{
    private sealed class ReviewOperation : ContinuousOperation
    {
        private readonly OperationSpec _spec = new OperationSpec("Review operation", "R1", "R2");
        public override OperationSpec Operation => _spec;
        protected override OperationWorld CreateWorld() => new ProbeWorld(Ctx);
        protected override Mission CreatePhase(string id) => id == "R1" ? (Mission)new HudMission() : new SplitProbe();
        protected override void OnCommit(MissionCatalog catalog, OperationWorld world) { }
    }

    private sealed class TimeReleaseFailureAbility : Bloodlines.Abilities.Ability
    {
        public override CrewSlot Slot => CrewSlot.Guess;
        public override string Name => "Release failure probe";
        public override void Activate(Ped player) { }
        public override void Update(Ped player) { }
        public override void Deactivate(Ped player) { throw new InvalidOperationException("Simulated ability cleanup failure"); }
    }

    static void ReviewRepairChecks()
    {
        Reset(); var crew = Roster();
        var strip = new CrewOrderStrip();
        var wheel = new CharacterWheel(root);
        strip.Open(crew); wheel.Open(crew.ActiveSlot); strip.Close();
        Check(Game.TimeScale == .2f, "Closing crew orders leaves the character wheel's time claim active");
        wheel.Close();
        Check(Game.TimeScale == 1f && !SlowMotion.Slowed, "Overlapping menus closed in opening order restore normal time");
        strip.Open(crew); wheel.Open(crew.ActiveSlot); wheel.Close();
        Check(Game.TimeScale == .2f, "Closing the wheel leaves an overlapping order strip slowed");
        strip.Close();
        Check(Game.TimeScale == 1f, "Overlapping menus also restore normal time in reverse order");

        SlowMotion.Hold(Bloodlines.Abilities.SlipstreamReflex.TimeOwner, .45f);
        strip.Open(crew);
        SlowMotion.Release(Bloodlines.Abilities.SlipstreamReflex.TimeOwner);
        Check(Game.TimeScale == .2f, "An ability expiring under crew orders cannot speed the menu up");
        strip.Close();
        Check(Game.TimeScale == 1f, "Closing orders cannot resurrect an expired ability's slow motion");
        strip.Open(crew);
        SlowMotion.Hold(Bloodlines.Abilities.SlipstreamReflex.TimeOwner, .45f);
        strip.Close();
        Check(Game.TimeScale == .45f, "An ability that starts under a menu retains its own time after menu closure");
        SlowMotion.Release(Bloodlines.Abilities.SlipstreamReflex.TimeOwner);
        strip.Open(crew); SlowMotion.Reset(); strip.Close();
        Check(Game.TimeScale == 1f && !SlowMotion.Slowed, "Emergency reset followed by menu cleanup cannot restore stale slow motion");

        SlowMotion.Hold("ReviewScene", .3f);
        SlowMotion.Hold(Bloodlines.Abilities.SlipstreamReflex.TimeOwner, .45f);
        strip.Open(crew);
        var abilityStop = new AbilityStopHarness { _running = new TimeReleaseFailureAbility() };
        abilityStop.Stop();
        Check(Game.TimeScale == .2f && SlowMotion.Held.Contains("ReviewScene") &&
              !SlowMotion.Held.Contains(Bloodlines.Abilities.SlipstreamReflex.TimeOwner),
            "Production ability shutdown releases only its claim even if deactivation throws");
        strip.Close();
        Check(Game.TimeScale == .3f, "Ability shutdown preserves a mission's time claim after the menu closes");
        SlowMotion.Release("ReviewScene");

        var controller = new ControllerHarness { _crew = crew, _switching = new SwitchController(crew) };
        controller._config.ControllerSwitchEnabled = true;
        controller._orders.Open(crew);
        Function.Held[Control.CharacterWheel] = true;
        controller.Tick();
        Check(!controller._characterWheel.IsOpen && controller._orders.IsOpen && Game.Disabled.Contains(Control.CharacterWheel),
            "Production controller switching cannot open its wheel over keyboard crew orders");
        controller._orders.Close(); Function.Held.Clear(); controller.Tick();
        Check(Game.TimeScale == 1f, "Releasing the blocked switch and order inputs leaves normal time");

        // Real operation parents must expose the initial chapter through the manager.
        foreach (string entry in new[] { "M19", "M44" })
        {
            Reset(); crew = Roster(); var c = Context(crew);
            var state = c.State = CampaignState.Load(Path.Combine(root, "review-hud-" + entry + ".json"));
            for (int i = 1; i <= 9; i++) state.Completed.Add("SM" + i.ToString("00"));
            var def = new MissionDefinition { Info = new MissionInfo { Id = entry, Title = entry }, Factory = () => new ChapterProbe(entry) };
            var cat = new MissionCatalog(); cat.All.Add(def);
            var manager = new MissionManager(c, state, cat);
            Check(manager.Start(def), entry + " starts through its ordinary operation dispatcher");
            for (int i = 0; i < 8 && manager.ActiveOperation == null; i++) { c.Cutscenes.Skip(); manager.Update(); }
            var phase = manager.ActiveOperation?.Phase as ComposedMission;
            Check(phase != null && phase.CurrentStageObjectives != null && phase.CurrentStageObjectives.Count > 0,
                entry + " has a running composed phase");
            Check(ReferenceEquals(manager.CurrentObjectives, phase.CurrentStageObjectives) && manager.CurrentStageName == phase.CurrentStageName,
                entry + " exposes its actual phase objectives and stage name");
            Check(MissionHud.Compose(manager, crew).OwnerSlot.HasValue, entry + " HUD includes the active objective owner");
            manager.Abort();
        }

        // Advance a real two-phase operation with production tracking and tally updates.
        Reset(); crew = Roster(); var context = Context(crew);
        var save = context.State = CampaignState.Load(Path.Combine(root, "review-tally.json"));
        var operation = new ReviewOperation();
        var definition = new MissionDefinition { Info = new MissionInfo { Id = "R1", Title = "Review" }, Factory = () => operation };
        var catalog = new MissionCatalog(); catalog.All.Add(definition);
        var dispatcher = new MissionManager(context, save, catalog);
        Check(dispatcher.Start(definition), "Presentation test operation starts through the real manager");
        var first = (HudMission)operation.Phase;
        var frame = MissionHud.Compose(dispatcher, crew);
        Check(frame.Timer == "5:00" && frame.ProgressFraction >= 0f && frame.OwnerSlot == CrewSlot.Gohan,
            "An operation HUD carries its phase timer, gauge and role");
        var dead = first.Hostiles[0]; dead.IsDead = true;
        operation.World.Own(dead);
        dispatcher.Update(); dispatcher.Update();
        first.Pass(); context.Dialogue.Clear(); dispatcher.Update();
        Check(operation.Phase is SplitProbe && dispatcher.CurrentStageName == "Terminal",
            "The HUD switches to the next phase without keeping the old stage");
        Check(MissionHud.Compose(dispatcher, crew).Timer == "", "A later phase does not inherit the previous phase's timer");
        first.Hostiles[1].IsDead = true;
        dispatcher.Update(); dispatcher.Update();
        var tally = dispatcher.Tally.Finish(0, 0, 79, false);
        Check(tally.Kills == 2 && operation.Staged.Count(e => e.Handle == dead.Handle) == 1,
            "Heist results include deaths from earlier chapters exactly once across phase changes");
        dispatcher.Abort();
        Check(!operation.Staged.Any() && dead.Present && dead.Released, "Operation cleanup releases the inspection view without deleting its dead actors");
    }
}
