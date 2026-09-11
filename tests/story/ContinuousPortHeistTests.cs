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

        // New bookmarks and legacy completion are separate, backward-compatible concepts.
        Reset(); var file = Path.Combine(root, "continuous-port-state.json"); var state = CampaignState.Load(file); var catalog = PortCatalog();
        state.SavePortHeistBoundary("M21");
        var reloaded = CampaignState.Load(file);
        Check(reloaded.PortHeistResumePhase == "M21" && reloaded.CompletedCount == 0 && reloaded.CashOnHand == 0,
            "A phase bookmark survives reload without granting completion or cash");
        Check(PortHeistOperation.ResolveEntry(reloaded, "M19") == "M21", "Normal operation entry resumes the recorded safe phase");
        var points = new MissionMarkers(catalog, state, null, Context(Roster()).Locations, dataDir, "J");
        Check(points.StartPoint(catalog.All[0])?.Key == "M21.LaunchSpawn", "The one normal heist entry routes to its saved phase");
        Check(points.StartPoint(catalog.All[3])?.Key == "M22.Beach", "An explicit QA phase retains its own authored start point");
        bool rejected = false; try { state.SavePortHeistBoundary("M99"); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "An invalid phase bookmark is rejected");
        state.Reset(); state.Completed.Add("M19"); state.Completed.Add("M20"); state.Save();
        Check(PortHeistOperation.ResolveEntry(CampaignState.Load(file), "M19") == "M21", "A legacy save between chapters resumes without fabricating historical rewards");
        state.CompletePortHeist(catalog, new Dictionary<string, string> { { "bullion", "M22.AlamoDrop" } });
        Check(PortHeistOperation.PhaseIds.All(state.IsComplete) && state.CashOnHand == 150000 && state.PortHeistResumePhase == "", "Final operation completion commits the remaining ids and one payout");
        state.AlamoGoldDredgedTons = 7; state.SetCargo("bullion", "M24.Dredge");
        state.CompletePortHeist(catalog, new Dictionary<string, string> { { "bullion", "M22.AlamoDrop" } });
        Check(state.CashOnHand == 150000 && state.AlamoGoldDredgedTons == 7 && state.CargoAt("bullion") == "M24.Dredge", "Replaying the operation cannot reset later salvage or duplicate the payout");

        // Actual four-phase operation: mission classes and manager, not a string-id probe.
        Reset(); var crew = Roster(); var c = Context(crew); state = c.State = CampaignState.Load(Path.Combine(root, "continuous-port-full.json"));
        foreach (var solo in new[] { "SM01", "SM02", "SM03" }) state.Completed.Add(solo);
        c.Vans = new CrewVan(state, c.Locations); crew.Arsenal = new WeaponProgression(state);
        catalog = PortCatalog(); var manager = new MissionManager(c, state, catalog);
        Check(manager.Start(catalog.All[0]) && c.Cutscenes.IsActive && crew.Arsenal.LoanActive, "The operation opens one briefing and one weapon-loan session");
        c.Cutscenes.Skip(); manager.Update();
        Check(manager.ActivePortHeist != null && !Game.Player.CanControlCharacter, "The parent is running and does not unlock controls under the phase's opening scene");
        var operation = manager.ActivePortHeist; var m19 = (M19UnderwaterBreach)operation.Phase;
        var actors = Protagonist.All.ToDictionary(h => h.Slot, h => crew.PedFor(h.Slot));
        actors[CrewSlot.Ice].Health = 233; actors[CrewSlot.Gohan].Armor = 41;
        var helicopter = m19.Lift; helicopter.EngineHealth = 733f;
        int clocks = GameUtils.ClockCalls, weather = GameUtils.WeatherCalls;
        c.Cutscenes.Skip(); manager.Update();
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
        Check(!state.IsComplete("M19") && state.PortHeistResumePhase == "M20" && state.CargoAt("bullion") == null, "Underwater success saves only the retry boundary, not permanent rewards or cargo history");
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
        Check(state.CashOnHand == 150000 && state.CargoAt("bullion") == "M22.AlamoDrop" && state.PortHeistResumePhase == "" && !crew.Arsenal.LoanActive, "One final payout, final cargo ledger, and one closed loan session");
        Check(bullion.Exists() && bullion.Position == drop && !PortHeistWorld.Attached(bullion, helicopter) && helicopter.Exists() && roadCar.Exists(), "The delivered cargo and extraction vehicles survive the final cleanup");
        Check(actors.All(p => crew.PedFor(p.Key) == p.Value), "All three original character objects survived the complete live operation");
        c.Cutscenes.Stop();

        // A failed essential transfer aborts the current operation without awards.
        Reset(); crew = Roster(); c = Context(crew); state = c.State = CampaignState.Load(Path.Combine(root, "continuous-port-failed.json"));
        state.SavePortHeistBoundary("M20"); foreach (var solo in new[] { "SM01", "SM02", "SM03" }) state.Completed.Add(solo);
        crew.Arsenal = new WeaponProgression(state); catalog = PortCatalog(); manager = new MissionManager(c, state, catalog);
        Check(manager.Start(catalog.All[0]), "A saved Sky Hook boundary starts through the ordinary operation entry");
        c.Cutscenes.Skip(); manager.Update(); c.Cutscenes.Skip();
        operation = manager.ActivePortHeist; m20 = (M20SkyHook)operation.Phase;
        crew.PedFor(CrewSlot.Gohan).StuckInSeat = true;
        typeof(M20SkyHook).GetMethod("PlayTransfer", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(m20, null);
        c.Cutscenes.Skip(); manager.Update(); manager.Update();
        Check(operation.Status == MissionStatus.Failed && !state.IsComplete("M20") && state.CashOnHand == 0 && state.PortHeistResumePhase == "M20" && !crew.Arsenal.LoanActive, "A refused essential exit fails the operation, preserves its retry boundary and awards nothing");
        Check(Game.Player.CanControlCharacter && !GameUtils.Faded && c.PortHeist == null, "Failure releases scene controls and the operation world");
        manager.Retry(); c.Cutscenes.Skip(); manager.Update();
        Check(manager.ActivePortHeist?.PhaseId == "M20" && !manager.ActivePortHeist.SupportsCheckpointRestore, "Retry reconstructs the failed phase, not M19 or an unsupported arbitrary checkpoint");
        manager.Abort();

        // Explicit developer starts still exercise individual source modules.
        Reset(); crew = Roster(); c = Context(crew); state = c.State = CampaignState.Load(Path.Combine(root, "continuous-port-qa.json"));
        catalog = PortCatalog(); manager = new MissionManager(c, state, catalog);
        Check(manager.Start(catalog.All[1], bypassGates: true), "QA may start a standalone Port Heist phase explicitly");
        c.Cutscenes.Skip(); manager.Update();
        Check(manager.ActivePortHeist == null && manager.IsRunning && c.PortHeist == null, "The QA phase is not silently wrapped into the parent operation");
        manager.Abort();
    }
}
