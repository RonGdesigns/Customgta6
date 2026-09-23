using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

/// <summary>
/// The September 22 audit of Act II and the first six solo jobs: stages that asked for the
/// wrong brother, stage exits that threw, "must remain" conditions that enforced nothing,
/// combat and drive orders handed over on a clock, and a handful of mission-specific faults.
/// </summary>
public static partial class StoryTests
{
    static string ActII(string file) => Source("src/Bloodlines/Missions/Campaign/Act2/" + file);
    static string Solo(string file) => Source("src/Bloodlines/Missions/Campaign/Solo/" + file);
    static string OneLine(string text) => Regex.Replace(text, @"\s+", " ");

    static void AuditActIIChecks()
    {
        AuditOwnershipChecks();
        AuditStageExitChecks();
        AuditRuleChecks();
        AuditOrderChecks();
        AuditMissionSpecificChecks();
    }

    /// <summary>G3: an unowned stage inherits the previous owner, and a kill objective only completes for its owner.</summary>
    static void AuditOwnershipChecks()
    {
        foreach (var file in new[] { "M31TheIronPerimeter.cs", "M35TheChianskiAmbush.cs", "M38BloodInTheQuarry.cs", "M47PaletoCollapse.cs" })
            Check(ActII(file).Contains(".AnyBrother()"), file + " opens its whole-crew stage to any brother");

        // M31's defense is played as Ice, the man on the rifle, and has to be completable as him.
        Reset(); var crew = Roster(); var c = Context(crew);
        c.State = CampaignState.Load(Path.Combine(root, "audit31-" + Guid.NewGuid() + ".json"));
        var m31 = new M31TheIronPerimeter();
        Check(m31.Begin(c), "M31 starts for the ownership check");
        var hold = Flow(m31).First(s => s.Name == "Hold the perimeter");
        Check(hold.AllowsAnyBrother && hold.Objectives.All(o => !o.RequiredCharacter.HasValue),
            "M31's perimeter defense belongs to nobody in particular, so Ice can finish it");
        m31.Abort();

        Reset(); crew = Roster(); c = Context(crew);
        c.State = CampaignState.Load(Path.Combine(root, "audit35-" + Guid.NewGuid() + ".json"));
        var m35 = new M35TheChianskiAmbush();
        Check(m35.Begin(c), "M35 starts for the ownership check");
        foreach (var name in new[] { "Watch the pass", "Capture the technical" })
        {
            var stage = Flow(m35).First(s => s.Name == name);
            Check(stage.AllowsAnyBrother && stage.Objectives.All(o => !o.RequiredCharacter.HasValue),
                "M35's '" + name + "' is not inherited by Gohan from the laptop stage");
        }
        m35.Abort();

        Reset(); crew = Roster(); c = Context(crew);
        c.State = CampaignState.Load(Path.Combine(root, "audit38-" + Guid.NewGuid() + ".json"));
        var m38 = new M38BloodInTheQuarry();
        Check(m38.Begin(c), "M38 starts for the ownership check");
        var yard = Flow(m38).First(s => s.Name == "Clear the loading yard");
        Check(yard.AllowsAnyBrother && yard.Objectives.All(o => !o.RequiredCharacter.HasValue),
            "M38's yard can be cleared as Ice, as its label says, not only as Guess");
        m38.Abort();
    }

    /// <summary>G4: a stage exit or entry that throws is a "Script error"; these keep a reason and fail with it.</summary>
    static void AuditStageExitChecks()
    {
        // The old throws, by the exact call that used to be there.
        var gone = new[]
        {
            Tuple.Create("Act2/M27FlightRisk.cs", "throw new InvalidOperationException(\"No vehicle could be staged"),
            Tuple.Create("Act2/M28OffTheGrid.cs", "throw new System.InvalidOperationException(\"The surge unit could not connect"),
            Tuple.Create("Act2/M30RedlineRidge.cs", "throw new System.InvalidOperationException(\"The pursuit helicopter"),
            Tuple.Create("Act2/M32BlackSiteZancudo.cs", "throw new InvalidOperationException(\"Both EMP cases"),
            Tuple.Create("Act2/M33TheInformantsGrave.cs", "throw new InvalidOperationException(\"The rescue half-track"),
            Tuple.Create("Act2/M34MudAndIron.cs", "throw new InvalidOperationException(\"The medical kit"),
            Tuple.Create("Act2/M36DeepWellRecon.cs", "throw new System.InvalidOperationException(\"The coastal patrol"),
            Tuple.Create("Act2/M38BloodInTheQuarry.cs", "throw new InvalidOperationException(\"Explosives delivery"),
            Tuple.Create("Act2/M39ThePaletoCable.cs", "throw new InvalidOperationException(\"Cable cutter"),
            Tuple.Create("Act2/M39ThePaletoCable.cs", "throw new InvalidOperationException(\"The cutter detached"),
            Tuple.Create("Act2/M41TheGeneralsWire.cs", "throw new InvalidOperationException(\"Bradley's card could not load"),
            Tuple.Create("Act2/M41TheGeneralsWire.cs", "throw new InvalidOperationException(\"The card is not in Ice's custody"),
            Tuple.Create("Act2/M42SkyfallDelivery.cs", "throw new InvalidOperationException(\"The cargo parachutes"),
            Tuple.Create("Act2/M43StagingPaleto.cs", "throw new InvalidOperationException(\"An asset left its holding position"),
            Tuple.Create("Solo/SM02ZeroDayInjection.cs", "throw new System.InvalidOperationException(\"The maintenance stair exit"),
        };
        foreach (var pair in gone)
            Check(!Source("src/Bloodlines/Missions/Campaign/" + pair.Item1).Contains(pair.Item2),
                pair.Item1 + " no longer throws from a stage: " + pair.Item2.Substring(pair.Item2.IndexOf('"') + 1));

        // And each of them fails with its reason on the next frame instead.
        foreach (var file in new[] { "Act2/M27FlightRisk.cs", "Act2/M28OffTheGrid.cs", "Act2/M32BlackSiteZancudo.cs", "Act2/M33TheInformantsGrave.cs",
            "Act2/M34MudAndIron.cs", "Act2/M36DeepWellRecon.cs", "Act2/M38BloodInTheQuarry.cs", "Act2/M39ThePaletoCable.cs", "Act2/M41TheGeneralsWire.cs",
            "Act2/M42SkyfallDelivery.cs", "Act2/M43StagingPaleto.cs" })
            Check(OneLine(Source("src/Bloodlines/Missions/Campaign/" + file)).Contains("if (_fault != null) { Fail(_fault); return; }") ||
                  OneLine(Source("src/Bloodlines/Missions/Campaign/" + file)).Contains("if(_fault!=null){Fail(_fault);return;}"),
                file + " turns a stage's recorded fault into a failure with its reason");

        // M42's release refusal is a failure with the reason, not a script error.
        Reset(); var crew = Roster(); var c = Context(crew);
        c.State = CampaignState.Load(Path.Combine(root, "audit42-" + Guid.NewGuid() + ".json"));
        var m42 = new M42SkyfallDelivery();
        Check(m42.Begin(c), "M42 starts for the release check");
        DrainPreparation(m42, c);
        m42.Titan.Position = c.Locations.Position("M42.Drop") + new Vector3(0, 0, 250); m42.CargoSub.Position = m42.Titan.Position + new Vector3(0, 0, -5);
        World.FailPropModel = "p_parachute1_sp_s";
        Game.Accept = true; m42.Tick(); Game.Accept = false; m42.Tick();
        World.FailPropModel = null;
        Check(m42.Status == MissionStatus.Failed && m42.FailReason != null && m42.FailReason.Contains("parachutes"),
            "A release whose parachutes will not deploy fails M42 with that reason rather than a script error");

        // SM02's stair transfer is the stage's own last objective, so a failure stops the
        // stage from completing rather than being thrown from its exit.
        Check(Solo("SM02ZeroDayInjection.cs").Contains("() => Stairs(serviceDoor, _roof, ref _upStairs,") &&
              Solo("SM02ZeroDayInjection.cs").Contains("() => Stairs(roofAccess, _exit, ref _downStairs,"),
            "SM02 makes both stair transfers inside their stages");
        // A transfer that never streams puts Gohan back and fails with the reason.
        Reset(); crew = Roster(); c = Context(crew);
        c.State = CampaignState.Load(Path.Combine(root, "audit-sm02-" + Guid.NewGuid() + ".json"));
        var sm02 = new SM02ZeroDayInjection();
        Check(sm02.Begin(c), "SM02 starts for the stair check");
        c.Cutscenes.Skip(); sm02.Tick();
        World.CollisionReady = false;
        var door = c.Locations.Position("SM02.StairEntry");
        Interact(sm02, c, CrewSlot.Gohan, door, 2);
        Check(Game.Player.Character.Position == door, "A roof that never streams leaves Gohan at the service door");
        sm02.Tick();
        Check(sm02.Status == MissionStatus.Failed && sm02.FailReason.Contains("stairs"),
            "and the attempt fails saying the stairs did not stream, not \"Script error\"");
    }

    /// <summary>G5: "must remain" conditions completed once and enforced nothing afterward.</summary>
    static void AuditRuleChecks()
    {
        // M23: the car has to be stopped at the approach, not merely stopped.
        Reset(); var crew = Roster(); var c = Context(crew); World.CollisionReady = true;
        c.State = CampaignState.Load(Path.Combine(root, "audit23-" + Guid.NewGuid() + ".json"));
        var m23 = new M23GhostInTheSage();
        Check(m23.Begin(c), "M23 starts for the arrival check");
        c.Cutscenes.Skip(); m23.Tick(); m23.Tick();
        var stop = m23.CurrentStageObjectives.OfType<ConditionObjective>().First();
        Check(!stop.IsFinished, "M23's stop-with-both-aboard is not satisfied by the car parked at the road start");

        // M33 and M41: the rule is enforced on the drive, in the mission.
        Check(OneLine(ActII("M33TheInformantsGrave.cs")).Contains("Fail(\"Ramos left the extraction car"),
            "M33 fails the drive if Ramos leaves the car");
        Check(ActII("M41TheGeneralsWire.cs").Contains("if (!BrothersAboard()) { Fail("),
            "M41 fails the extraction if a brother leaves the car");
        Check(ActII("M36DeepWellRecon.cs").Contains("()=>PickupHeldAtArrival()"),
            "M36 checks the pickup boat when the sub arrives, not once when the stage opens");

        // M33: the boat rule, played. Ramos out of the car on the drive fails after the grace.
        Reset(); crew = Roster(); c = Context(crew);
        c.State = CampaignState.Load(Path.Combine(root, "audit33b-" + Guid.NewGuid() + ".json"));
        var m33 = new M33TheInformantsGrave(); m33.Begin(c); DrainPreparation(m33, c);
        Use(crew, CrewSlot.Ice); Game.Player.Character.Position = c.Locations.Position("M33.Observe"); DrainPreparation(m33, c);
        ClearPreparationEnemies(); DrainPreparation(m33, c); Interact(m33, c, CrewSlot.Gohan, m33.Ramos.Position, 4); DrainPreparation(m33, c);
        var car = World.Vehicles.First(v => v.Model.Name == "granger"); ParkPreparation(m33, c, car, "M33.Pickup");
        BoardTestCrew(c, car); m33.Ramos.SetIntoVehicle(car, VehicleSeat.Passenger); DrainPreparation(m33, c);
        Check(m33.CurrentStage == 5 && m33.Status == MissionStatus.Running, "M33 is on the drive to the armor");
        m33.Ramos.Task.LeaveVehicle(); m33.Tick();
        Game.GameTime += M33TheInformantsGrave.RamosOutGraceMs + 100; m33.Tick();
        Check(m33.Status == MissionStatus.Failed && m33.FailReason.Contains("Ramos left"), "Ramos leaving the car on the drive fails the rescue with the reason");
    }

    /// <summary>Orders given once on a change, never re-issued on a short clock.</summary>
    static void AuditOrderChecks()
    {
        Check(!ActII("M29DustAndDiesel.cs").Contains("_nextOrders"), "M29's pursuit is not re-tasked on a three-second clock");
        Check(!ActII("M34MudAndIron.cs").Contains("_escortOrder"), "M34's escort is not re-tasked on a clock");
        Check(ActII("M34MudAndIron.cs").Contains("Function.Call(Hash.TASK_VEHICLE_FOLLOW,gohan,CrewCar,_halftrack"), "M34's escort follows the half-track with one follow task");
        Check(!ActII("M35TheChianskiAmbush.cs").Contains("_routeOrder"), "M35's convoy is not re-routed on a three-second clock");
        Check(!Solo("SM02ZeroDayInjection.cs").Contains("_nextGuardOrder"), "SM02's guards are not cleared and re-sent on a clock");
        foreach (var file in new[] { "M23GhostInTheSage.cs", "M29DustAndDiesel.cs", "M32BlackSiteZancudo.cs", "M34MudAndIron.cs", "M35TheChianskiAmbush.cs", "M36DeepWellRecon.cs" })
            Check(!ActII(file).Contains("CurrentStage"), file + " decides by what has happened, not by the stage number");
        Check(!ActII("M45PaletoBreach.cs").Contains("InsertionStage") && !ActII("M45PaletoBreach.cs").Contains("DeckFightStage"),
            "M45 has no stage-index constants left");

        // M35: one route order per convoy driver, held while the car is moving.
        Reset(); var crew = Roster(); var c = Context(crew);
        c.State = CampaignState.Load(Path.Combine(root, "audit35b-" + Guid.NewGuid() + ".json"));
        var m35 = new M35TheChianskiAmbush(); m35.Begin(c); DrainPreparation(m35, c);
        foreach (int i in new[] { 2, 1 }) Interact(m35, c, CrewSlot.Ice, c.Locations.Position("M35.ChargeWork" + i), 4); DrainPreparation(m35, c);
        var car = World.Vehicles.First(v => v.Model.Name == "granger"); ParkPreparation(m35, c, car, "M35.BlockExit");
        Interact(m35, c, CrewSlot.Gohan, c.Locations.Position("M35.DeviceWork"), 4); DrainPreparation(m35, c);
        var lead = World.Vehicles.First(v => v.Model.Name == "mesa"); var driver = lead.GetPedOnSeat(VehicleSeat.Driver);
        lead.Speed = 10f; int before = driver.Task.Drives;
        for (int i = 0; i < 5; i++) { Game.GameTime += 3100; m35.Tick(); }
        Check(driver.Task.Drives == before, "A convoy driver who is moving keeps the route he was given");
        lead.Speed = 0f; m35.Tick(); Game.GameTime += M35TheChianskiAmbush.ConvoyStallMs + 100; m35.Tick();
        Check(driver.Task.Drives == before + 1, "and is re-routed once when he has genuinely stalled short of the trap");
        m35.Abort();

        // SM02: the partner is sent once, and not cleared again while the order is fresh.
        Reset(); crew = Roster(); c = Context(crew); World.CollisionReady = true;
        c.State = CampaignState.Load(Path.Combine(root, "audit-sm02b-" + Guid.NewGuid() + ".json"));
        var sm02 = new SM02ZeroDayInjection(); sm02.Begin(c); c.Cutscenes.Skip(); sm02.Tick();
        var first = sm02.Guards[0]; var partner = sm02.Guards[1];
        first.IsRagdoll = true; first.IsBeingStunned = true; first.LastWeaponHit = (uint)WeaponHash.StunGun;
        for (int i = 0; i < 4; i++) { Game.GameTime += 1000; sm02.Tick(); }
        int clears = partner.Task.Clears;
        for (int i = 0; i < 4; i++) { Game.GameTime += 1000; partner.CombatTarget = Game.Player.Character; sm02.Tick(); }
        Check(partner.Task.Clears == clears, "SM02's partner is not cleared and re-tasked while he is fighting");
        sm02.Abort();
    }

    static void AuditMissionSpecificChecks()
    {
        // M26: a spotter that goes down without Ron firing on it is put back up, not a failed job.
        Reset(); var crew = Roster(); var c = Context(crew);
        c.State = CampaignState.Load(Path.Combine(root, "audit26-" + Guid.NewGuid() + ".json"));
        var m26 = new M26AlamoScramble(); m26.Begin(c); c.Cutscenes.Skip(); m26.Tick();
        var fallen = m26.Spotters[1]; fallen.IsDriveable = false; m26.Tick();
        Check(m26.Status == MissionStatus.Running && m26.Relaunches == 1 && m26.Spotters[1] != fallen && m26.Spotters[1].IsDriveable,
            "M26 relaunches a second spotter that went down on its own");
        Check(m26.Pilots[1].IsInVehicle(m26.Spotters[1]) && m26.Pilots[1].SeatIndex == VehicleSeat.Driver,
            "and the replacement has a pilot in the seat");
        Check(ActII("M26AlamoScramble.cs").Contains("AircraftHold.LaunchAirborne(plane, SpotterLaunchSpeed);"), "Every spotter, first or replacement, is launched airborne");
        m26.Abort();

        // M27: no seated pilot, no Shamal, and no start.
        string m27 = ActII("M27FlightRisk.cs");
        Check(m27.Contains("GameUtils.SafeDelete(_shamal);") && m27.Contains("AircraftHold.LaunchAirborne(_shamal, ShamalLaunchSpeed);"),
            "M27 removes an unpiloted Shamal so the start refuses, and launches the one it keeps airborne");

        // M28: the unalerted response has awareness rather than a guard post and nothing else.
        Check(ActII("M28OffTheGrid.cs").Contains("_searching=new GuardAwareness(") && ActII("M28OffTheGrid.cs").Contains("_searching.Update();"),
            "M28's searching squads can see, hear and be shot, and fight once they find the crew");

        // M30: its gunship is made in the air the shared way.
        Check(ActII("M30RedlineRidge.cs").Contains("AircraftHold.LaunchAirborne(_heli);"), "M30's gunship is launched airborne");

        // M32: the ride over uses the seats the extraction asks for.
        Check(ActII("M32BlackSiteZancudo.cs").Contains("Tuple.Create(CrewSlot.Gohan,VehicleSeat.RightRear)"), "M32 seats Gohan where BoardBrothers will want him");

        // M33: with the firing line down there is nobody left to shoot Ramos, so the clock stops.
        Reset(); crew = Roster(); c = Context(crew);
        c.State = CampaignState.Load(Path.Combine(root, "audit33-" + Guid.NewGuid() + ".json"));
        var m33 = new M33TheInformantsGrave(); m33.Begin(c); DrainPreparation(m33, c);
        Use(crew, CrewSlot.Ice); Game.Player.Character.Position = c.Locations.Position("M33.Observe"); DrainPreparation(m33, c);
        Check(m33.ExecutionStarted && m33.ClockRunning, "Reaching the firing line starts M33's clock");
        ClearPreparationEnemies(); DrainPreparation(m33, c);
        Check(!m33.ClockRunning, "Stopping all four men on the line stops the clock");
        Game.GameTime += 120000; m33.Tick();
        Check(m33.Status == MissionStatus.Running, "Gohan's walk to Ramos after the line is down is not on the execution clock");
        m33.Abort();

        // M43: a save missing preparation chapters is told at the start.
        Reset(); crew = Roster(); c = Context(crew);
        c.State = CampaignState.Load(Path.Combine(root, "audit43-" + Guid.NewGuid() + ".json"));
        var m43 = new M43StagingPaleto();
        Check(m43.Begin(c) && m43.MissingAtStart.Contains("M31") && m43.MissingAtStart.Contains("M42"),
            "M43 names the unfinished preparation chapters when it starts, not only at the ledger");
        m43.Abort();

        // M45: an empty deck detail is a clear deck, not a missing hostile.
        string m45 = ActII("M45PaletoBreach.cs");
        Check(m45.Contains("_guards.Count > 0") && m45.Contains("new ConditionObjective(\"Ice: the deck detail is not on the upper deck"),
            "M45 does not ask a kill objective to count an empty detail");

        // M46: the operation refuses without Bradley's card, before any chapter is staged.
        Reset(); crew = Roster(); c = Context(crew);
        c.State = CampaignState.Load(Path.Combine(root, "audit46-" + Guid.NewGuid() + ".json"));
        var paleto = new PaletoOperation("M44", c.State);
        Check(!paleto.Begin(c) && c.Operation == null && GameUtils.Message != null && GameUtils.Message.Contains("Bradley"),
            "The Paleto operation refuses to start without Bradley's card and says why");
        string m46 = ActII("M46PaletoVault.cs");
        Check(m46.Contains("_hold.Update(Ctx.Crew, CrewSlot.Guess, _chopper,") && m46.Contains("_inside = Paleto.EnsureInside(Ctx);"),
            "M46 keeps Guess's helicopter flying and keeps asking for the interior until it answers");
        Check(!OneLine(m46).Contains("if (world != null) throw new InvalidOperationException(\"The vault needs"), "M46 no longer throws inside the operation over the card");

        // M47: whoever the player is not playing is sent over the side.
        Check(ActII("M47PaletoCollapse.cs").Contains("if (_triggered && !_jumped) SendOverTheSide();"), "M47 sends the AI brother over the side");

        // M48: the bed gun works while the cordon is standing, not after it has fallen.
        string m48 = ActII("M48TheRoadBackSouth.cs");
        Check(m48.Contains("if (_cordonAwake) WorkTheGun();") && m48.Contains("_cordonAwake = true;"), "M48's gun is switched on when the cordon wakes");

        // SM01: shooting from range brings Sergei's men.
        Reset(); crew = Roster(); c = Context(crew);
        c.State = CampaignState.Load(Path.Combine(root, "audit-sm01-" + Guid.NewGuid() + ".json"));
        var sm01 = new SM01LeadAndKevlar(); sm01.Begin(c); c.Cutscenes.Skip(); sm01.Tick(); Use(crew, CrewSlot.Ice);
        var guard = sm01.Guards[0];
        Game.Player.Character.Position = guard.Position + new Vector3(60f, 0f, 0f);
        Game.Player.Character.IsShooting = true; sm01.Tick(); Game.Player.Character.IsShooting = false;
        Check(sm01.GuardsEngaged && sm01.Status == MissionStatus.Running && sm01.CurrentStage == 0, "SM01's guards react to Ice shooting from the street");
        Check(sm01.Guards.All(g => !g.BlockPermanentEvents), "and are no longer blocked from reacting");
        sm01.Abort();

        // SM02: the hint names Gohan's actual ability.
        Check(!Solo("SM02ZeroDayInjection.cs").Contains("Thermal Pulse (") && Solo("SM02ZeroDayInjection.cs").Contains("Blackout (\" + context.Config.AbilityKey"),
            "SM02's hint names Blackout, which is what Gohan holds");

        // SM06: the bikes chase; a passenger shoots.
        Reset(); crew = Roster(); c = Context(crew);
        c.State = CampaignState.Load(Path.Combine(root, "audit-sm06-" + Guid.NewGuid() + ".json"));
        var sm06 = new SM06CanyonRunner(); sm06.Begin(c); c.Cutscenes.Skip();
        int shotsBefore = Function.Calls.Count(call => call.Item1 == Hash.TASK_DRIVE_BY);
        GameUtils.RoadAvailable = true;
        try { PackageCall(sm06, "StartBikes"); } finally { GameUtils.RoadAvailable = false; }
        var bikes = World.Vehicles.Where(v => v.Model.Name == "sanchez" && v.Exists()).ToList();
        Check(bikes.Count == 3, "SM06 puts three hijacker bikes on the road");
        Check(bikes.All(b => b.GetPedOnSeat(VehicleSeat.Driver) != null && b.GetPedOnSeat(VehicleSeat.Driver).Task.Chases == 1 &&
                             b.GetPedOnSeat(VehicleSeat.Driver).Task.VehicleShots == 0),
            "Each rider holds his chase; nothing replaces it with a shooting order");
        Check(bikes.All(b => b.GetPedOnSeat(VehicleSeat.Passenger) != null) &&
              Function.Calls.Count(call => call.Item1 == Hash.TASK_DRIVE_BY) - shotsBefore == 3,
            "and a man on the back seat does the shooting");
        sm06.Abort();
    }
}
