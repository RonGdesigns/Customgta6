using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using GTA;
using GTA.Math;
using GTA.Native;

public sealed class FailedStartSceneStep : SceneStep
{
    protected override void OnStart() { throw new InvalidOperationException("test start failure"); }
    public override bool IsComplete => false;
    public override void Finish() { }
}

public static partial class StoryTests
{
    static MissionCatalog PortCatalog()
    {
        var catalog = new MissionCatalog();
        catalog.All.Add(new MissionDefinition { Info = new MissionInfo { Id = "M19", Title = "Underwater Breach" }, Factory = () => new M19UnderwaterBreach() });
        catalog.All.Add(new MissionDefinition { Info = new MissionInfo { Id = "M20", Title = "Sky Hook", Prerequisite = "M19" }, Factory = () => new M20SkyHook() });
        catalog.All.Add(new MissionDefinition { Info = new MissionInfo { Id = "M21", Title = "Open Water", Prerequisite = "M20" }, Factory = () => new M21OpenWater() });
        catalog.All.Add(new MissionDefinition { Info = new MissionInfo { Id = "M22", Title = "Scorched Bay", Prerequisite = "M21" }, Factory = () => new M22ScorchedBay() });
        catalog.All.Add(new MissionDefinition { Info = new MissionInfo { Id = "M23", Title = "The Bunker", Prerequisite = "M22" }, Factory = () => new ChapterProbe("M23") });
        return catalog;
    }

    static void MovePortVehicle(Vehicle vehicle, Vector3 point)
    {
        // Physics is not emulated: this is the explicit test driver moving a vehicle.
        vehicle.Position = point;
        foreach (var rider in vehicle.Occupants) rider.Position = point;
    }

    static void FinishPortPhase(MissionManager manager, MissionContext context, string next)
    {
        for (int i = 0; i < 15 && manager.ActivePortHeist != null && manager.ActivePortHeist.PhaseId != next; i++)
        {
            if (context.Cutscenes.IsActive) context.Cutscenes.Skip();
            context.Dialogue.Clear();
            manager.Update();
        }
        Check(manager.ActivePortHeist != null && manager.ActivePortHeist.PhaseId == next,
            "The parent advances internally to " + next + " without an ordinary mission restart [" + manager.CurrentObjective + "; " + manager.LastFailureReason + "]");
    }

    static void ContinuousPortHeistChecks()
    {
        // Failed physical results must stop both watched and skipped blocking.
        Reset(); int applied = 0;
        var badStart = new SceneBlocking().Then(new FailedStartSceneStep()).Then(new VerifySceneStep("tail", () => true, () => applied++));
        badStart.Update();
        Check(badStart.IsFinished && !badStart.Succeeded && applied == 0, "A throwing scene start cancels later success actions");
        var actor = new Ped(); var van = new Vehicle(); var cargo = new Prop { RejectAttachments = true };
        var failedCarry = new SceneBlocking().Then(new CarryPropStep(actor, cargo)).Then(new VerifySceneStep("tail", () => true, () => applied++));
        failedCarry.Complete();
        Check(!failedCarry.Succeeded && applied == 0 && cargo.AttachedTo == null, "A refused carried-prop attachment cannot complete a skipped action");
        var failedTransfer = new SceneBlocking().Then(new TransferPropStep(cargo, van, Vector3.Zero, 5)).Then(new VerifySceneStep("tail", () => true, () => applied++));
        failedTransfer.Update(); Game.GameTime += 10; failedTransfer.Update();
        Check(!failedTransfer.Succeeded && applied == 0, "A refused transfer fails during watched playback too");
        var occupied = new Ped(); occupied.SetIntoVehicle(van, VehicleSeat.Driver);
        var seat = new EnterVehicleStep(actor, van, VehicleSeat.Driver); seat.Finish();
        Check(seat.Failed && van.GetPedOnSeat(VehicleSeat.Driver) == occupied && !actor.IsInVehicle(van), "A skipped entry does not evict another actor to fake the requested seat");
        var shot = new ShotStep(1, null, Vector3.Zero, null, Vector3.Zero, 0, () => applied++);
        shot.Finish(); shot.Finish(); Check(applied == 1, "A shot's state callback runs at most once even when finalized twice");

        // A pre-existing bookmark is input from the superseded preview, not permission
        // to resume. Migration keeps earned money, ownership and completion intact.
        Reset(); var file = Path.Combine(root, "single-sitting-legacy.json"); var catalog = PortCatalog();
        foreach (var resume in new[] { "M20", "M21", "M22", "invalid" })
        {
            File.WriteAllText(file, "{\"campaign\":{\"currentMissionId\":\"M21\",\"portHeistResumePhase\":\"" + resume +
                "\",\"completedMissions\":[\"M19\",\"M20\"]},\"economy\":{\"cashOnHand\":4500},\"weaponLockers\":{\"Guess\":[123]}}");
            var legacy = CampaignState.Load(file);
            Check(legacy.CurrentMissionId == "M19" && legacy.NextStory(catalog).Id == "M19" &&
                PortHeistOperation.ResolveEntry(legacy, resume) == "M19", "Old " + resume + " bookmark cannot resume or select a later heist section");
            Check(legacy.IsComplete("M19") && legacy.IsComplete("M20") && !legacy.IsComplete("M21") &&
                legacy.CashOnHand == 4500 && legacy.Weapons["Guess"].Contains(123), "Migration preserves legitimate historical completion, cash and ownership");
            var points = new MissionMarkers(catalog, legacy, null, Context(Roster()).Locations, dataDir, "J");
            Check(points.StartPoint(catalog.All[0])?.Key == "M18.SaltHangar", "The normal map entry stays at the underwater operation start");
            Check(points.StartPoint(catalog.All[3])?.Key == "M22.Beach", "Only an explicit QA section keeps its isolated authored location");
            legacy.Save();
            Check(!File.ReadAllText(file).Contains("portHeistResumePhase"), "The next ordinary save discards the obsolete bookmark field");
        }
        var state = CampaignState.Load(file);
        state.Reset(); state.Completed.Add("M19"); state.Completed.Add("M20");
        state.CompletePortHeist(catalog, new Dictionary<string, string> { { "bullion", "M22.AlamoDrop" } });
        Check(PortHeistOperation.PhaseIds.All(state.IsComplete) && state.CashOnHand == 150000 && state.NextStory(catalog).Id == "M23",
            "Final success alone commits remaining legacy ids and one payout, then offers M23");
        state.AlamoGoldDredgedTons = 7; state.SetCargo("bullion", "M24.Dredge");
        state.CompletePortHeist(catalog, new Dictionary<string, string> { { "bullion", "M22.AlamoDrop" } });
        Check(state.CashOnHand == 150000 && state.AlamoGoldDredgedTons == 7 && state.CargoAt("bullion") == "M24.Dredge",
            "Replaying the whole heist cannot reset later salvage or duplicate the payout");
        var completed = CampaignState.Load(file);
        Check(completed.NextStory(catalog).Id == "M23", "An already completed heist is not forced on an existing save");

        // Every ordinary section request uses M19's gate and starts from the beginning.
        for (int alias = 1; alias <= 3; alias++)
        {
            Reset(); var roster = Roster(); var context = Context(roster);
            var fresh = context.State = CampaignState.Load(Path.Combine(root, "single-sitting-alias-" + alias + ".json"));
            var entries = PortCatalog(); var dispatcher = new MissionManager(context, fresh, entries);
            entries.All[0].Info.Prerequisite = "M18";
            Check(!dispatcher.CanStart(entries.All[alias], out var why) && why.Contains("M18") && GameUtils.ClockCalls == 0,
                "A normal later-section alias cannot bypass M19's preparation prerequisite");
            fresh.Completed.Add("M18");
            Check(!dispatcher.Start(entries.All[alias]) && GameUtils.Message.Contains("SM01") && GameUtils.ClockCalls == 0,
                "A normal later-section alias cannot bypass the required solo gate or mutate the world");
            foreach (var solo in new[] { "SM01", "SM02", "SM03" }) fresh.Completed.Add(solo);
            Check(dispatcher.Start(entries.All[alias]) && dispatcher.LastAttempted == entries.All[0], "The accepted normal alias is canonicalized to M19");
            context.Cutscenes.Skip(); dispatcher.Update();
            Check(dispatcher.ActivePortHeist?.PhaseId == "M19", "A normal later-section request starts underwater, never in the middle");
            dispatcher.Abort();
        }

        RunWholePortAttempt(null, "success");
        RunWholePortAttempt("M19", "abort");
        RunWholePortAttempt("M20", "failed-transfer");
        RunWholePortAttempt("M20", "teammate-death");
        RunWholePortAttempt("M21", "cargo-loss");
        RunWholePortAttempt("M22", "cargo-loss");
        RunWholePortAttempt("M22", "abort");
        RunWholePortAttempt("M21", "shutdown");
        RunWholePortAttempt("M22", "shutdown");

        // Diagnostics are explicitly opt-in; normal progression never offers these starts.
        Reset(); var qaCrew = Roster(); var qa = Context(qaCrew);
        qa.State = CampaignState.Load(Path.Combine(root, "continuous-port-qa.json"));
        catalog = PortCatalog(); var qaManager = new MissionManager(qa, qa.State, catalog);
        Check(qaManager.Start(catalog.All[1], bypassGates: true), "QA may explicitly isolate a section for debugging");
        qa.Cutscenes.Skip(); qaManager.Update();
        Check(qaManager.ActivePortHeist == null && qaManager.IsRunning && qa.PortHeist == null,
            "Explicit QA is not a hidden normal-play resume path");
        qaManager.Abort();
    }

    static bool StopWholePortAttempt(MissionManager manager, MissionContext c, CampaignState state,
        MissionCatalog catalog, string stopAt, string exitKind, string savePath)
    {
        var operation = manager.ActivePortHeist;
        Check(c.Checkpoints.CommitCalls == 0 && !c.Checkpoints.HasCheckpointFor("M19") &&
            !c.Checkpoints.HasCheckpointFor(operation.PhaseId), "No hidden checkpoint is recorded at " + operation.PhaseId);
        Check(!operation.AllowsCheckpointCapture && !operation.Phase.AllowsCheckpointCapture && !manager.TryRestoreCheckpoint(),
            "Neither the parent nor its owned section permits checkpoint recovery");
        var disk = CampaignState.Load(savePath);
        Check(!PortHeistOperation.PhaseIds.Any(disk.IsComplete) && disk.CashOnHand == 0 && disk.CargoAt("bullion") == null &&
            !File.ReadAllText(savePath).Contains("portHeistResumePhase") && disk.NextStory(catalog).Id == "M19",
            "A disk reload during " + operation.PhaseId + " contains no partial success or reentry point");
        if (operation.PhaseId != stopAt) return false;
        if (c.Cutscenes.IsActive) c.Cutscenes.Skip();
        manager.CommitCheckpoint(); manager.RestoreCheckpoint();
        Check(c.Checkpoints.CommitCalls == 0 && manager.ActivePortHeist == operation,
            "The manual checkpoint keys cannot create or restore a mid-heist checkpoint");
        // Even an unrelated save while the heist runs must not persist its current section.
        state.Save(); string savedDuringAttempt = File.ReadAllText(savePath);
        if (exitKind == "failed-transfer")
        {
            c.Crew.PedFor(CrewSlot.Gohan).StuckInSeat = true;
            typeof(M20SkyHook).GetMethod("PlayTransfer", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(operation.Phase, null);
            c.Cutscenes.Skip(); manager.Update(); manager.Update();
        }
        else if (exitKind == "cargo-loss")
        {
            operation.WorldState.Get<Prop>("bullion").Delete(); manager.Update(); manager.Update();
        }
        else if (exitKind == "teammate-death")
        {
            c.Crew.PedFor(CrewSlot.Ice).IsDead = true; manager.Update(); manager.Update();
        }
        else if (exitKind == "shutdown") manager.Shutdown();
        else manager.Abort();
        Check(operation.Status == (exitKind == "abort" || exitKind == "shutdown" ? MissionStatus.Aborted : MissionStatus.Failed) &&
            !manager.IsRunning && c.PortHeist == null && !c.Crew.Arsenal.LoanActive,
            exitKind + " at " + stopAt + " ends the entire attempt and releases its resources");
        Check(!PortHeistOperation.PhaseIds.Any(state.IsComplete) && state.CashOnHand == 0 && state.CargoAt("bullion") == null,
            "An unsuccessful whole attempt awards no chapter results, money or cargo");
        Check(Game.Player.CanControlCharacter && !GameUtils.Faded, "Failure and abort return usable controls and visibility");
        if (exitKind == "shutdown")
        {
            // The file copied while still running also models a crash before graceful shutdown.
            File.WriteAllText(savePath, savedDuringAttempt);
            Reset(); var roster = Roster(); c = Context(roster); c.State = CampaignState.Load(savePath);
            c.Crew.Arsenal = new WeaponProgression(c.State);
            manager = new MissionManager(c, c.State, catalog);
            Check(manager.StartNext(), "A new process offers a fresh whole heist after a quit or crash");
        }
        else manager.Retry();
        c.Cutscenes.Skip(); manager.Update();
        Check(manager.ActivePortHeist != null && manager.ActivePortHeist != operation &&
            manager.ActivePortHeist.PhaseId == "M19" && manager.LastAttempted == catalog.All[0],
            exitKind + " at " + stopAt + " restarts the underwater beginning, not the failed section");
        Check(!manager.ActivePortHeist.SupportsCheckpointRestore && c.Checkpoints.CommitCalls == 0,
            "The restarted attempt still has no checkpoint capture or restore");
        manager.Abort();
        return true;
    }

    static void RunWholePortAttempt(string stopAt, string exitKind)
    {
        // Actual four-phase operation: mission classes and manager, not a string-id probe.
        Reset(); var crew = Roster(); var c = Context(crew);
        var savePath = Path.Combine(root, "continuous-port-" + (stopAt ?? "whole") + "-" + exitKind + ".json");
        var state = c.State = CampaignState.Load(savePath);
        foreach (var solo in new[] { "SM01", "SM02", "SM03" }) state.Completed.Add(solo);
        c.Vans = new CrewVan(state, c.Locations); crew.Arsenal = new WeaponProgression(state);
        var catalog = PortCatalog(); var manager = new MissionManager(c, state, catalog);
        state.Save();
        Check(manager.Start(catalog.All[0]) && c.Cutscenes.IsActive && crew.Arsenal.LoanActive, "The operation opens one briefing and one weapon-loan session");
        c.Cutscenes.Skip(); manager.Update();
        Check(manager.ActivePortHeist != null && !Game.Player.CanControlCharacter, "The parent is running and does not unlock controls under the phase's opening scene");
        var operation = manager.ActivePortHeist; var m19 = (M19UnderwaterBreach)operation.Phase;
        var actors = Protagonist.All.ToDictionary(h => h.Slot, h => crew.PedFor(h.Slot));
        actors[CrewSlot.Ice].Health = 233; actors[CrewSlot.Gohan].Armor = 41;
        var helicopter = m19.Lift; helicopter.EngineHealth = 733f;
        int clocks = GameUtils.ClockCalls, weather = GameUtils.WeatherCalls;
        c.Cutscenes.Skip(); manager.Update();
        if (StopWholePortAttempt(manager, c, state, catalog, stopAt, exitKind, savePath)) return;
        Interact(m19, c, CrewSlot.Gohan, m19.Breach, 16, afloat: true);
        foreach (var clamp in m19.Clamps) Interact(m19, c, CrewSlot.Gohan, clamp, 8, afloat: true);
        manager.Update(); // Evaluate the now-complete multi-site objective.
        Check(c.Cutscenes.IsActive && !m19.Floated, "Starting the float scene does not prematurely certify its physical result");
        c.Cutscenes.Skip(); manager.Update(); var bullion = m19.Container;
        MovePortVehicle(m19.Kraken, c.Locations.Position("M19.Surface")); c.Dialogue.Clear(); m19.Tick(); c.Dialogue.Clear(); m19.Tick();
        Game.Player.WantedLevel = 3;
        FinishPortPhase(manager, c, "M20"); var m20 = (M20SkyHook)operation.Phase;
        Check(m20.Cargobob == helicopter && m20.Container == bullion && m20.Kraken == m19.Kraken && actors.All(p => crew.PedFor(p.Key) == p.Value), "M19 to M20 keeps the exact aircraft, cargo, sub and three character objects");
        Check(helicopter.EngineHealth == 733f && actors[CrewSlot.Ice].Health == 233 && actors[CrewSlot.Gohan].Armor == 41, "Live handoff preserves actual vehicle damage and distinct crew injuries");
        Check(Game.Player.WantedLevel == 3 && GameUtils.ClockCalls == clocks && GameUtils.WeatherCalls == weather && crew.Arsenal.LoanActive, "The phase boundary does not reset pursuit, clock, weather or the loan session");
        Check(!state.IsComplete("M19") && state.CargoAt("bullion") == null, "Underwater success is only in this live attempt, not permanent progress");
        if (StopWholePortAttempt(manager, c, state, catalog, stopAt, exitKind, savePath)) return;
        c.Cutscenes.Skip(); Use(crew, CrewSlot.Guess); m20.Tick();
        foreach (var guard in World.Created.Where(p => p.Model.Name == "s_m_y_blackops_01")) guard.IsDead = true;
        Use(crew, CrewSlot.Ice); c.Dialogue.Clear(); m20.Tick();
        Use(crew, CrewSlot.Guess); Game.Player.Character.SetIntoVehicle(helicopter, VehicleSeat.Driver);
        var hover = new Vector3(bullion.Position.X, bullion.Position.Y, c.Locations.Position("M20.HoverPoint").Z);
        Interact(m20, c, CrewSlot.Guess, hover, 8, afloat: true);
        Check(PortHeistWorld.Attached(bullion, helicopter) && c.Cutscenes.IsActive, "The actual hover locks the same bullion prop to the original aircraft");
        c.Cutscenes.Skip(); MovePortVehicle(helicopter, c.Locations.Position("M20.ClimbOut")); c.Dialogue.Clear(); m20.Tick();
        Check(!m20.Transferred && c.Cutscenes.IsActive, "The launch transfer remains pending while its scene is running");
        c.Cutscenes.Skip();
        Check(m20.Transferred && PortHeistWorld.Seated(actors[CrewSlot.Gohan], m20.Launch, VehicleSeat.Driver) && PortHeistWorld.Seated(actors[CrewSlot.Ice], m20.Launch, VehicleSeat.Passenger), "Only verified launch seats commit the transfer");
        FinishPortPhase(manager, c, "M21"); var m21 = (M21OpenWater)operation.Phase;
        Check(m21.Cargobob == helicopter && m21.Launch == m20.Launch && PortHeistWorld.Attached(bullion, helicopter) && !bullion.Released, "M20 to M21 neither releases nor replaces the loaded aircraft");
        if (StopWholePortAttempt(manager, c, state, catalog, stopAt, exitKind, savePath)) return;
        c.Cutscenes.Skip(); Use(crew, CrewSlot.Gohan); m21.Tick();
        for (int i = 0; i < 20 && m21.CurrentStage == 1; i++) { MovePortVehicle(m21.Launch, helicopter.Position); Game.GameTime += 1000; c.Dialogue.Clear(); m21.Tick(); }
        foreach (var guard in m21.HostileCrews) guard.IsDead = true;
        c.Dialogue.Clear(); m21.Tick();
        MovePortVehicle(m21.Launch, c.Locations.Position("M21.Breakwater")); c.Dialogue.Clear(); m21.Tick();
        MovePortVehicle(m21.Launch, c.Locations.Position("M21.ShoreLanding")); c.Dialogue.Clear(); m21.Tick();
        Check(c.Cutscenes.IsActive && !m21.Transferred, "Shore arrival starts a real pending road transfer");
        c.Cutscenes.Skip(); var roadCar = m21.Granger; var coastPosition = roadCar.Position; var airPosition = helicopter.Position;
        FinishPortPhase(manager, c, "M22"); var m22 = (M22ScorchedBay)operation.Phase;
        Check(m22.Cargobob == helicopter && m22.Container == bullion && m22.Granger == roadCar && helicopter.Position == airPosition && roadCar.Position == coastPosition, "M21 to M22 retains both transports at their actual positions, not replacements at the lake");
        Check(!m22.Arrived && m22.RoadArrivalPending && actors[CrewSlot.Gohan].Task.Drives > 0, "The road team must travel inland before the beach arrival can run");
        Check(GameUtils.ClockCalls == clocks && GameUtils.WeatherCalls == weather && Game.Player.WantedLevel == 3 && crew.Arsenal.LoanActive && !state.IsComplete("M20"), "All three live boundaries preserve environment and defer permanent rewards");
        if (StopWholePortAttempt(manager, c, state, catalog, stopAt, exitKind, savePath)) return;
        Use(crew, CrewSlot.Guess); m22.Tick();
        MovePortVehicle(roadCar, c.Locations.Position("M22.RoadArrival")); m22.Tick();
        Check(c.Cutscenes.IsActive && !m22.Arrived, "Reaching the Alamo starts the last short road arrival, not a pre-awarded state");
        c.Cutscenes.Skip(); Check(m22.Arrived, "Both road passengers are genuinely out and at the regroup point");
        var drop = c.Locations.Position("M22.AlamoDrop"); Interact(m22, c, CrewSlot.Guess, drop + new Vector3(0, 0, 20), 3, afloat: true);
        Check(m22.Dropped && !PortHeistWorld.Attached(bullion, helicopter) && state.CargoAt("bullion") == null, "The deposit detaches the real container but remains temporary until operation success");
        c.Cutscenes.Skip(); MovePortVehicle(helicopter, c.Locations.Position("M22.Beach")); helicopter.Speed = 0; helicopter.IsInAir = false; c.Dialogue.Clear(); m22.Tick();
        actors[CrewSlot.Guess].Task.LeaveVehicle(); actors[CrewSlot.Guess].Position = c.Locations.Position("M22.Beach") + new Vector3(-6, 6, 0); c.Dialogue.Clear(); m22.Tick();
        Check(c.Cutscenes.IsActive && !m22.Struck, "The final required aftermath is pending, not already successful");
        c.Cutscenes.Skip();
        for (int i = 0; i < 12 && manager.ActivePortHeist != null; i++) { c.Dialogue.Clear(); manager.Update(); }
        Check(operation.Status == MissionStatus.Passed && manager.ActivePortHeist == null && PortHeistOperation.PhaseIds.All(state.IsComplete), "The parent completes all internal ids at one final result");
        Check(state.CashOnHand == 150000 && state.CargoAt("bullion") == "M22.AlamoDrop" && !crew.Arsenal.LoanActive, "One final payout, final cargo ledger, and one closed loan session");
        Check(bullion.Exists() && bullion.Position == drop && !PortHeistWorld.Attached(bullion, helicopter) && helicopter.Exists() && roadCar.Exists(), "The delivered cargo and extraction vehicles survive the final cleanup");
        Check(actors.All(p => crew.PedFor(p.Key) == p.Value), "All three original character objects survived the complete live operation");
        c.Cutscenes.Stop();

        Check(c.Checkpoints.CommitCalls == 0 && !File.ReadAllText(savePath).Contains("portHeistResumePhase"),
            "The complete successful mission never recorded a checkpoint or persistent resume position");
    }
}
