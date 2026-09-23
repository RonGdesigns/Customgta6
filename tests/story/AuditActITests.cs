using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using GTA;
using GTA.Math;
using GTA.Native;

/// <summary>
/// The Act I audit, September 22: the fixes that came out of reading the code against
/// Ron's playtest log. Each check drives the mission where the harness can, and reads
/// the source only for what cannot be staged without the game.
/// </summary>
public static partial class StoryTests
{
    static void AuditSet(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    /// <summary>Source text with its line endings and runs of whitespace flattened, so a check never depends on how a file was checked out.</summary>
    static string AuditSource(string relative) => Regex.Replace(Source(relative), @"\s+", " ");

    static void AuditActIChecks()
    {
        AuditM01Placement();
        AuditM02AndM06AndM08();
        AuditM05Approach();
        AuditM10Run();
        AuditM12Hunt();
        AuditM13Basin();
        AuditM16Pad();
        AuditM20Deck();
        AuditM21Escort();
        AuditHeistExits();
    }

    static void AuditM01Placement()
    {
        // Every probe drifts ten meters east: the engine's snap answering from a moved point.
        Reset(); var crew = Roster(); var c = Context(crew);
        World.SafeCoordHandler = p => p + new Vector3(10f, 0f, 0f);
        var authored = c.Locations.Position("M01.GuessApproach");
        Check(ProloguePlacement.Prepare(c.Locations), "M01's dock resolves on the cold open");
        var first = c.Locations.Position("M01.GuessApproach");
        var terminal = c.Locations.Position("M01.ServiceTerminal");
        Check(ProloguePlacement.Prepare(c.Locations) && c.Locations.Position("M01.GuessApproach") == first && c.Locations.Position("M01.ServiceTerminal") == terminal,
            "The mission's own start reuses the cold open's answer instead of snapping the snapped points again (Ron, September 22)");
        ProloguePlacement.Restore();
        Check(c.Locations.Position("M01.GuessApproach") == authored, "Restoring puts every corrected key back where the author left it");
        Check(ProloguePlacement.Prepare(c.Locations) && c.Locations.Position("M01.GuessApproach") == first,
            "A second attempt resolves from the authored points, so the start no longer walks further every retry");
        ProloguePlacement.Restore();

        // A surveyed key edited between the two passes is his, and the new value is used.
        Reset(); crew = Roster(); c = Context(crew);
        Check(ProloguePlacement.Prepare(c.Locations), "M01 resolves before a survey edit");
        var edited = c.Locations.Get("M01.RegroupPoint"); edited.Position += new Vector3(3f, 0f, 0f); var capture = edited.Position;
        Check(ProloguePlacement.Prepare(c.Locations) && GameUtils.IsWithinFlat(edited.Position, capture, 0.5f),
            "A key edited since the last pass is resolved from its new value, not reset to the old authored one");
        ProloguePlacement.Restore();

        // Snapping that closes the gap to Mateo pushes the terminal out rather than refusing.
        Reset(); crew = Roster(); c = Context(crew);
        var mateo = c.Locations.Position("M01.CapoSpawn");
        c.Locations.Get("M01.ServiceTerminal").Position = mateo + new Vector3(-20f, 0f, 0f);
        Check(ProloguePlacement.Prepare(c.Locations), "A terminal snapped inside 30 m of Mateo is pushed out, not refused");
        Check(!GameUtils.IsWithinFlat(c.Locations.Position("M01.ServiceTerminal"), c.Locations.Position("M01.CapoSpawn"), 30f),
            "The pushed terminal keeps Gohan's hack out of Mateo's sight");
        ProloguePlacement.Restore();

        // A whole attempt: the mission's cleanup hands the keys back.
        Reset(); crew = Roster(); c = Context(crew);
        World.SafeCoordHandler = p => p + new Vector3(4f, 0f, 0f);
        authored = c.Locations.Position("M01.GuessApproach");
        var m1 = new M01GhostInTheDockyard();
        Check(m1.Begin(c), "M01 starts with a drifting ground snap");
        m1.Abort();
        Check(c.Locations.Position("M01.GuessApproach") == authored, "M01's cleanup restores the dock keys it corrected");
        string placement = AuditSource("src/Bloodlines/Core/ProloguePlacement.cs");
        Check(placement.Contains("\", Mateo \" + resolved[mateo]"), "A refused terminal logs both resolved points");
    }

    static void AuditM02AndM06AndM08()
    {
        string m2 = AuditSource("src/Bloodlines/Missions/Campaign/Act1/M02LooseStrands.cs");
        Check(!m2.Contains("_technician.Task.WarpIntoVehicle(") && m2.Contains("_technician.SetIntoVehicle(_van, VehicleSeat.Driver);") && m2.Contains("if (!_technician.IsInVehicle(_van))"),
            "M02's van driver is seated outright and checked, never a queued warp his route would replace");
        string m6 = AuditSource("src/Bloodlines/Missions/Campaign/Act1/M06CleanSweep.cs");
        Check(m6.Contains("if (ordered && target == ice && trooper.IsInCombat) continue;") && m6.Contains("if (!moved) trooper.Task.RunTo(entry, false, 20000);"),
            "M06's troopers are ordered to fight once and re-sent to run only when stalled");
        Check(!m6.Contains("CurrentStage>=4") && m6.Contains("if(_boardingOpen&&AllAboard())"), "M06's boarding unlock follows the boarding stage's own entry, not its index");
        string m8 = AuditSource("src/Bloodlines/Missions/Campaign/Act1/M08SupplyAndSever.cs");
        Check(m8.Contains("current == enemy && ped.IsInCombat) continue;"), "M08's defending brothers are not handed a fresh fight every two seconds");
        string m15 = AuditSource("src/Bloodlines/Missions/Campaign/Act1/M15Crawlspace.cs");
        Check(m15.Contains("!Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE, brother)"), "M15's withdrawal leaves an entry already under way to finish");
    }

    static void AuditM05Approach()
    {
        Check(M05TidalLock.FlushRadiusFor(115f) == 85f && M05TidalLock.FlushRadiusFor(60f) == 30f && M05TidalLock.FlushRadiusFor(20f) == 25f,
            "The flush ring is 85 m from far out, and always leaves thirty meters of approach when the flare goes up inside it");
        Reset(); var crew = Roster(); var c = Context(crew); var m = new M05TidalLock();
        Check(m.Begin(c), "M05 starts"); c.Cutscenes.Skip();
        var lamps = World.Created.Where(p => p != m.Mateo).ToList();
        m.Tick(); Check(lamps.All(p => p.Task.Fights == 0) && !m.LightCrewAwake, "The generator crew work their lamps until the shooting starts");
        lamps[0].Health -= 50; m.Tick();
        Check(m.LightCrewAwake && lamps.All(p => p.Task.Fights == 1 && !p.BlockPermanentEvents), "A hit on the generator crew turns every one of them on Ice (Ron, September 22)");
        for (int i = 0; i < 4; i++) { Game.GameTime += M05TidalLock.CrewReviewMs + 100; m.Tick(); }
        Check(lamps.All(p => p.Task.Fights == 1), "A man already fighting is never handed the order again");
        foreach (var lamp in lamps) lamp.IsDead = true; m.Tick();
        var grotto = m.Mateo.CurrentVehicle.Position;
        Interact(m, c, CrewSlot.Guess, grotto + new Vector3(60f, 0f, 0f), 4, true); m.Tick();
        Check(m.CurrentStage == 2 && Math.Abs(m.ApproachRadius - 30f) < .5f, "A flare fired 60 m out no longer skips the approach: Mateo runs at 30 m");
        m.Dinghy.Position = grotto + new Vector3(20f, 0f, 0f); Game.Player.Character.Position = m.Dinghy.Position; m.Tick();
        Check(m.CurrentStage == 3, "Closing the gap flushes him and the chase starts");
        m.Abort();
    }

    static void AuditM10Run()
    {
        Reset(); GameUtils.RoadAvailable = true; var crew = Roster(); var c = Context(crew); var m = new M10OpenThrottle();
        Check(m.Begin(c), "M10 starts"); c.Cutscenes.Skip();
        m.JumpToStage(2);
        var riders = World.Created.Where(p => p.Model.Name == "g_m_y_mexgoon_03").ToList();
        var ice = crew.PedFor(CrewSlot.Ice); var guess = crew.PedFor(CrewSlot.Guess);
        Use(crew, CrewSlot.Guess); guess.SetIntoVehicle(m.Flatbed, VehicleSeat.Driver); ice.SetIntoVehicle(m.Flatbed, VehicleSeat.RightFront);
        m.Flatbed.Speed = 20f; riders[0].Position = ice.Position + new Vector3(12f, 0f, 0f);
        m.Tick();
        Check(ice.Task.VehicleShots == 1 && m.IceShooting, "AI Ice beside Guess works the window on the nearest bike");
        Game.GameTime += 1000; m.Tick();
        Check(ice.Task.VehicleShots == 1, "His drive-by is not re-issued every frame");
        Game.GameTime += M10OpenThrottle.IceShotCooldownMs; m.Tick();
        Check(ice.Task.VehicleShots == 2, "It is renewed on its cooldown");
        int chases = riders.Sum(p => p.Task.Chases);
        for (int i = 0; i < 2; i++) { Game.GameTime += 3100; m.Tick(); }
        Check(riders.Sum(p => p.Task.Chases) == chases, "A mounted rider's chase is not re-issued on every three-second review");
        Use(crew, CrewSlot.Ice); m.Tick();
        Check(m.CurrentStage == 2 && m.RequiredSwitch == null, "Switching to Ice does not lock him out of the run");
        foreach (var rider in riders) rider.IsDead = true; m.Tick();
        Check(m.CurrentStage == 3, "The bikes Ice kills count while he is the player (Ron, September 22)");
        Check(m.Buzzard != null && m.Buzzard.ForwardSpeed > 0f && Function.Calls.Any(x => x.Item1 == Hash.SET_HELI_BLADES_FULL_SPEED && x.Item2[0] == m.Buzzard),
            "The Buzzard created sixty meters up is launched airborne, rotors at speed");
        var pilot = World.Created.First(p => p.Model.Name == "s_m_y_blackops_01");
        int heli = pilot.Task.HeliTasks;
        for (int i = 0; i < 3; i++) { Game.GameTime += 3100; m.Tick(); }
        Check(pilot.Task.HeliTasks == heli, "The gunship's attack run is not restarted every three seconds");
        m.Abort();
    }

    static void AuditM12Hunt()
    {
        Reset(); var crew = Roster(); var c = Context(crew); var m = new M12BlackTideRecon();
        Check(m.Begin(c), "M12 starts"); c.Cutscenes.Skip();
        AuditSet(m, "_hunting", true);
        foreach (var boat in World.Vehicles.Where(v => v.Model.Name == "predator")) boat.Speed = 10f;
        Use(crew, CrewSlot.Gohan); Game.GameTime += 2100; m.Tick();
        int orders = m.HuntOrders;
        Check(orders > 0, "The hunt orders the launches, their gunners and Ice");
        for (int i = 0; i < 3; i++) { Game.GameTime += 2100; m.Tick(); }
        Check(m.HuntOrders == orders, "Nothing is re-issued on the two-second review while nothing has changed (Ron, September 22)");
        m.Rov.Position += new Vector3(60f, 0f, 0f); Game.GameTime += 2100; m.Tick();
        Check(m.HuntOrders == orders + 2, "The launches are sent again when the ROV has moved on");
        m.Abort();
    }

    static void AuditM13Basin()
    {
        Reset(); var crew = Roster(); var c = Context(crew); var m = new M13SmugglersCut();
        Check(m.Begin(c), "M13 starts"); c.Cutscenes.Skip();
        var tug = World.Vehicles.First(v => v.Model.Name == "tug");
        AuditSet(m, "_planted", true); tug.IsDead = true; m.Tick();
        Check(m.Status == MissionStatus.Running, "A fuel boat going up after all three charges are planted is not a failure");
        tug.IsDead = false; AuditSet(m, "_planted", false); tug.IsDead = true; m.Tick();
        Check(m.Status == MissionStatus.Failed, "A fuel boat lost before the charges are on it still fails the job");
        m.Abort();

        Reset(); crew = Roster(); c = Context(crew); m = new M13SmugglersCut();
        Check(m.Begin(c), "M13 starts again"); c.Cutscenes.Skip();
        m.JumpToStage(4); int waited = Script.Waited;
        m.CompleteCurrentObjective(); m.Tick();
        Check(Script.Waited == waited, "Blowing the basin no longer stalls the script inside the stage exit");
        c.Cutscenes.Skip(); m.Tick();
        Check(m.ChargesFired == 3, "Skipping the result scene still sets off all three charges");
        m.Abort();
        Check(!AuditSource("src/Bloodlines/Missions/Campaign/Act1/M13SmugglersCut.cs").Contains("Script.Wait("), "M13 has no Script.Wait left in it");
    }

    static void AuditM16Pad()
    {
        Reset(); var crew = Roster(); var c = Context(crew); World.FailPeds = true;
        Check(!new M16TheHeavyLift().Begin(c), "An empty helipad refuses the mission rather than finishing the fight the moment it opens");
        World.FailPeds = false;
        Reset(); GameUtils.RoadAvailable = true; crew = Roster(); c = Context(crew); var m = new M16TheHeavyLift();
        Check(m.Begin(c), "M16 starts"); c.Cutscenes.Skip();
        m.JumpToStage(2);
        for (int i = 0; i < 16 && m.Tanks.Count == 0; i++) { Game.GameTime += 1000; m.Tick(); }
        Game.GameTime += 4100; m.Tick();
        Check(m.Tanks.Count == 1 && m.TankOrders == 1, "The first tank rolls out with one attack order");
        Game.GameTime += 4100; m.Tick(); Game.GameTime += 4100; m.Tick();
        Check(m.TankOrders == 1, "The tank's attack is not restarted every four seconds");
        m.Abort();
    }

    static void AuditM20Deck()
    {
        Reset(); var crew = Roster(); var c = Context(crew); var m = new M20SkyHook();
        Check(m.Begin(c), "M20 starts"); c.Cutscenes.Skip(); m.Tick();
        Check(m.Gunners.Count == 5 && !m.DeckAwake && m.Gunners.All(g => g.Task.HatedFights == 0),
            "The quayside gunners hold their posts while Guess walks to a parked lift (Ron, September 22)");
        m.JumpToStage(1);
        Check(m.DeckAwake && m.Gunners.All(g => g.Task.HatedFights == 1), "They open up when Ice is sent to suppress the deck");
        m.Abort();
        Reset(); crew = Roster(); c = Context(crew); m = new M20SkyHook();
        Check(m.Begin(c), "M20 starts again"); c.Cutscenes.Skip();
        m.Cargobob.IsInAir = true; m.Tick();
        Check(m.DeckAwake, "The lift leaving the apron wakes the deck too");
        m.Abort();
    }

    static void AuditM21Escort()
    {
        Reset(); var crew = Roster(); var c = Context(crew);
        var breakwater = c.Locations.Position("M21.Breakwater");
        var m = new M21OpenWater();
        Check(m.Begin(c), "M21 starts"); c.Cutscenes.Skip();
        var wanted = c.Locations.Position("M21.Breakwater") + new Vector3(-40f, 20f, 0f);
        Function.MarineFloor = p => GameUtils.IsWithinFlat(p, wanted, 12f) ? 5f : -40f;
        m.JumpToStage(1); Function.MarineFloor = null;
        Check(m.HostileBoats.Count == 3 && m.HostileBoats.All(b => !GameUtils.IsWithinFlat(b.Position, wanted, 12f)),
            "An Aegis launch whose offset lands on the breakwater is moved to open water");
        m.JumpToStage(2);
        var ice = crew.PedFor(CrewSlot.Ice); var gohan = crew.PedFor(CrewSlot.Gohan);
        Use(crew, CrewSlot.Gohan); gohan.SetIntoVehicle(m.Launch, VehicleSeat.Driver); ice.SetIntoVehicle(m.Launch, VehicleSeat.Passenger);
        m.HostileCrews[0].Position = ice.Position + new Vector3(20f, 0f, 0f);
        m.Tick();
        Check(ice.Task.VehicleShots == 1 && m.IceShooting, "AI Ice on the launch shoots at the Aegis boats while Gohan drives");
        Game.GameTime += 1000; m.Tick();
        Check(ice.Task.VehicleShots == 1, "His fire is renewed on a cooldown, not every frame");
        Use(crew, CrewSlot.Ice); m.Tick();
        Check(m.CurrentStage == 2 && m.RequiredSwitch == null, "Switching to Ice does not lock him out of the speedboat fight");
        foreach (var hostile in m.HostileCrews) hostile.IsDead = true; m.Tick();
        Check(m.CurrentStage == 3, "The crews Ice kills count while he is the player (Ron, September 22)");
        m.Abort();

        // The road north: the played brother left in the water at the landing is put on the road.
        Reset(); crew = Roster(); c = Context(crew); m = new M21OpenWater();
        Check(m.Begin(c), "M21 starts for the road north"); c.Cutscenes.Skip();
        AuditSet(m, "_liveTransfer", new LiveHandoff("audit"));
        Use(crew, CrewSlot.Gohan); var player = Game.Player.Character; player.Task.LeaveVehicle();
        player.Position = c.Locations.Position("M21.ShoreLanding"); player.IsInWater = true;
        m.Tick(); Game.GameTime += M21OpenWater.StrandedInWaterMs - 1000; m.Tick();
        Check(GameUtils.IsWithinFlat(player.Position, c.Locations.Position("M21.ShoreLanding"), 1f), "A few seconds in the water is left to the player");
        Game.GameTime += 2000; m.Tick();
        Check(GameUtils.IsWithinFlat(player.Position, m.Granger.Position, 5f), "A brother stuck in the water at the landing is put on the road beside the Granger");
        m.Abort();
    }

    static void AuditHeistExits()
    {
        foreach (var file in new[] { "M19UnderwaterBreach", "M20SkyHook", "M22ScorchedBay" })
        {
            string source = AuditSource("src/Bloodlines/Missions/Campaign/Act1/" + file + ".cs");
            Check(!source.Contains("throw new InvalidOperationException(\"The container and both fitted floats") &&
                  !source.Contains("throw new System.InvalidOperationException(\"The cable did not secure") &&
                  !source.Contains("throw new System.InvalidOperationException(\"The bullion drop did not reach") &&
                  !source.Contains("throw new System.InvalidOperationException(\"Gohan must remain at the wheel"),
                file + ": no stage exit or road order throws; each fails the attempt with its reason");
        }
        string m22 = AuditSource("src/Bloodlines/Missions/Campaign/Act1/M22ScorchedBay.cs");
        Check(m22.Contains("if (!MaintainRoadTeam()) return;") && !m22.Contains("Game.GameTime - _roadOrder > 15000) OrderRoadTeam();"),
            "M22's road leg re-orders Gohan on a stall or a lost seat, not on a fifteen-second clock");
        foreach (var file in new[] { "M19UnderwaterBreach", "M20SkyHook", "M21OpenWater" })
            Check(AuditSource("src/Bloodlines/Missions/Campaign/Act1/" + file + ".cs").Contains(", this)"),
                file + ": a failed fallback in a stage exit fails the mission instead of throwing");
    }
}
