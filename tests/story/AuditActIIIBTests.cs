using System;
using System.Collections.Generic;
using System.Linq;
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
/// The second half of the Act III audit, September 22: M60 to M70 and the three late solos.
/// Ron's report was that Act III is where missions spawned in the wrong place or did not
/// start at all, and his log had two of them in writing - "SM07: Ice is not available for his
/// own solo" and "No walkable mission surface: M62.Container". Each check here drives the
/// repaired beat rather than reading for it, except where the thing being checked is the
/// order of two calls.
/// </summary>
public static partial class StoryTests
{
    static void AuditActIIIBChecks()
    {
        SoloDeploymentChecks();
        SuiteEntryChecks();
        BurnerExitChecks();
        ServiceFloorChecks();
        VanceTargetChecks();
        BridgeDeckChecks();
        SiegeAllyChecks();
        DiveGearChecks();
        LateActThreeChecks();
        FinaleDitchChecks();
    }

    /// <summary>One harness frame: a scene is skipped, time moves, the mission ticks.</summary>
    static void B3Tick(Mission m, MissionContext c, int ms = 1000)
    {
        if (c.Cutscenes.IsActive) { Game.GameTime += CutsceneDirector.SkipGraceMs; c.Cutscenes.Skip(); }
        Game.GameTime += ms;
        m.Tick();
    }

    static void B3Until(Mission m, MissionContext c, Func<bool> done, int limit = 40, int ms = 1000)
    {
        for (int i = 0; i < limit && m.Status == MissionStatus.Running && !done(); i++) B3Tick(m, c, ms);
    }

    /// <summary>Stand at a point on foot, press the interaction and hold it for its time.</summary>
    static void B3Work(Mission m, MissionContext c, Vector3 at, int seconds)
    {
        var p = Game.Player.Character;
        p.Task.LeaveVehicle(); p.Position = at;
        Game.Accept = false; B3Tick(m, c);
        p.Position = at; Game.Accept = true; B3Tick(m, c);
        Game.GameTime += seconds * 1000 + 1; B3Tick(m, c, 0);
        Game.Accept = false;
    }

    static MissionContext TowerContext(CrewRoster crew)
    {
        var c = Context(crew);
        // The host wires the access service the same way; collision is loaded in this world.
        c.Interior = new ApartmentAccess(crew);
        World.CollisionReady = true;
        return c;
    }

    static void SoloDeploymentChecks()
    {
        // The crew stood down: nobody is deployed. SM07 used to ask for Ice, find nobody and
        // refuse; SM08 and SM09 had the same missing deployment.
        var solos = new[]
        {
            Tuple.Create<Func<ComposedMission>, CrewSlot>(() => new SM07BloodDebt(), CrewSlot.Ice),
            Tuple.Create<Func<ComposedMission>, CrewSlot>(() => new SM08BurnerProtocol(), CrewSlot.Gohan),
            Tuple.Create<Func<ComposedMission>, CrewSlot>(() => new SM09TheLongExit(), CrewSlot.Guess),
        };
        foreach (var solo in solos)
        {
            Reset(); var crew = new CrewRoster(); var c = TowerContext(crew);
            var m = solo.Item1();
            Check(m.Begin(c), m.Id + " starts with the crew stood down");
            var start = c.Locations.Position(m.Id + ".Start");
            Check(crew.IsSolo && crew.ActiveSlot == solo.Item2 && crew.Peds.ContainsKey(solo.Item2) &&
                  GameUtils.IsWithinFlat(crew.PedFor(solo.Item2).Position, start, 1f),
                m.Id + " puts " + solo.Item2 + " down alone at " + m.Id + ".Start itself, the way SM05 and SM06 do");
            m.Abort();
        }
    }

    static void SuiteEntryChecks()
    {
        Reset(); var crew = new CrewRoster(); var c = TowerContext(crew);
        var m = new SM07BloodDebt();
        Check(m.Begin(c), "SM07 starts");
        var start = c.Locations.Position("SM07.Start");
        var suite = c.Locations.Position("SM07.Suite");
        Check(m.Sterling == null && m.Detail.Count == 0, "Nobody is placed in the suite before Ice is in it");

        B3Work(m, c, start, SM07BloodDebt.ElevatorSeconds);
        Check(m.Sterling == null && !m.Upstairs && (c.Interior.Busy || c.Interior.Inside),
            "The elevator is the access service's move, and nothing is laid out while it runs");
        B3Until(m, c, () => m.Upstairs);
        var ice = crew.PedFor(CrewSlot.Ice);
        Check(m.Upstairs && c.Interior.Inside && GameUtils.IsWithinFlat(ice.Position, suite, 1f),
            "Ice reaches the suite through the service that pins the room and waits for its collision");
        Check(m.Sterling != null && m.Detail.Count == SM07BloodDebt.Bodyguards &&
              m.Detail.Concat(new[] { m.Sterling }).All(p => p.Position.DistanceTo(suite) < 20f && p.Position.DistanceTo(start) > 20f),
            "Sterling and his detail are laid out on the suite floor from where Ice landed, not on the street below");

        B3Until(m, c, () => m.CurrentStageName == "Take the foyer");
        B3Tick(m, c, SM07BloodDebt.FightReviewMs + 1);
        Check(m.Detail.All(p => p.Task.HatedFights >= 1 && !p.BlockPermanentEvents),
            "The detail is ordered to fight once the scene is over, and can react for itself");
        var quiet = m.Detail[0]; quiet.IsInCombat = true;
        var before = m.Detail.Select(p => p.Task.HatedFights).ToArray();
        B3Tick(m, c, SM07BloodDebt.FightReviewMs + 1);
        Check(quiet.Task.HatedFights == before[0] && Enumerable.Range(1, m.Detail.Count - 1).All(i => m.Detail[i].Task.HatedFights > before[i]),
            "A man already fighting is left alone; only a man who has dropped out of the fight is ordered again");

        m.Abort();
        Check(!c.Interior.Inside && !c.Interior.Busy && GameUtils.IsWithinFlat(ice.Position, start, 1f),
            "However SM07 ends, Ice is taken back to the street: a blimp suite has no door");
    }

    static void BurnerExitChecks()
    {
        Reset(); var crew = new CrewRoster(); var c = TowerContext(crew);
        var m = new SM08BurnerProtocol();
        Check(m.Begin(c), "SM08 starts");
        var start = c.Locations.Position("SM08.Start");
        B3Work(m, c, start, SM08BurnerProtocol.ElevatorSeconds);
        B3Until(m, c, () => m.Inside);
        var floor = c.Locations.Position("SM08.Floor");
        Check(m.Inside && c.Interior.Inside && m.Security.Count > 0 && m.Security.All(p => p.Position.DistanceTo(floor) < 20f),
            "Gohan and the night security are on the firm's floor, opened through the access service");
        B3Tick(m, c, SM08BurnerProtocol.FightReviewMs + 1);
        Check(m.Security.All(p => p.Task.HatedFights == 0), "Night security is not onto him while he is only walking the floor");

        B3Work(m, c, Field<Vector3>(m, "_terminal"), SM08BurnerProtocol.DownloadSeconds);
        B3Tick(m, c, SM08BurnerProtocol.FightReviewMs + 1);
        Check(m.Downloaded && m.Security.All(p => p.Task.HatedFights >= 1),
            "They go for him once he is working the terminal - they used to stand still for the whole job");

        B3Work(m, c, Field<Vector3>(m, "_vault"), SM08BurnerProtocol.ThermiteSeconds);
        Check(m.Armed, "The thermite is running");
        B3Work(m, c, Field<Vector3>(m, "_arrival"), SM08BurnerProtocol.ElevatorSeconds);
        B3Until(m, c, () => m.Out);
        var gohan = crew.PedFor(CrewSlot.Gohan);
        Check(m.Out && m.Status != MissionStatus.Failed && !c.Interior.Inside && GameUtils.IsWithinFlat(gohan.Position, start, 1f),
            "The way out is the service elevator back down to the street, inside the sixty seconds - not a walk out of an office with no stairs");
    }

    static void ServiceFloorChecks()
    {
        Reset(); var crew = Roster(); var c = TowerContext(crew);
        var m = new M64TheEightiethFloor();
        Check(m.Begin(c), "M64 starts");
        B3Work(m, c, c.Locations.Position("M64.Doors"), M64TheEightiethFloor.LiftCallSeconds);
        Check(!m.Inside && m.Defenders.Count == 0,
            "Calling the lift places nobody: the floor has not been reached yet, so there is nowhere to measure from");
        B3Until(m, c, () => m.Inside);
        Check(m.Inside && m.Defenders.Count > 0 && m.Defenders.All(p => p.Position.Z > 200f),
            "The defenders are on the service floor 221 m up, not on the plaza deck where the crew formed up");
        Check(Protagonist.All.All(h => crew.PedFor(h.Slot).Position.Z > 200f),
            "All three brothers are on the floor: the lift stage is Guess's, and he used to be left on the plaza");
        m.Abort();
        Check(!c.Interior.Inside && Protagonist.All.All(h => crew.PedFor(h.Slot).Position.Z < 60f),
            "Abandoning M64 hands the floor back and brings all three down to the plaza");
    }

    static void VanceTargetChecks()
    {
        Reset(); var crew = Roster(); var c = TowerContext(crew);
        var m = new M65ExecutivePrivilege();
        Check(m.Begin(c), "M65 starts");
        B3Work(m, c, c.Locations.Position("M65.Doors"), M65ExecutivePrivilege.LiftCallSeconds);
        Check(!m.Inside && m.Vance == null, "Vance is not placed until the executive floor is reached");
        B3Until(m, c, () => m.Inside);
        Check(m.Vance != null && m.Vance.Position.Z > 200f && m.Detail.All(p => p.Position.Z > 200f),
            "Vance, his detail and his terminal are in the office with the crew, not on the plaza");

        // Only Vance left standing. The brothers' combat used to pick the nearest opponent
        // within 110 m, which was him, and his death before the biometrics failed the mission.
        foreach (var guard in m.Detail) guard.IsDead = true;
        for (int i = 0; i < 6; i++) B3Tick(m, c, 3000);
        var brothers = Protagonist.All.Select(h => crew.PedFor(h.Slot)).ToArray();
        Check(m.Status == MissionStatus.Running && brothers.All(p => p.Task.LastTarget != m.Vance),
            "Before the escrow is open no brother's combat picks Vance as a target");

        Use(crew, CrewSlot.Gohan);
        B3Work(m, c, Field<Vector3>(m, "_terminal"), M65ExecutivePrivilege.BiometricSeconds);
        for (int i = 0; i < 3; i++) B3Tick(m, c, 3000);
        Check(m.Escrow && brothers.Any(p => p.Task.LastTarget == m.Vance),
            "Once Gohan has the biometrics, Vance is part of the fight like anyone else");
    }

    static void BridgeDeckChecks()
    {
        Reset(); var crew = Roster(); var c = TowerContext(crew);
        // What the engine told Ron: no walkable ground anywhere on the rail bridge.
        World.FailNavigationNear = c.Locations.Position("M62.Container");
        Function.Calls.Clear();
        var m = new M62SteelHorizon();
        Check(m.Begin(c), "M62 starts although the engine offers no walkable ground on the rail bridge");
        var keys = Enumerable.Range(1, M62SteelHorizon.Riders).Select(i => c.Locations.Position("M62.Rider" + i)).ToArray();
        Check(m.Escort.Count == M62SteelHorizon.Riders && m.Escort.All(p => keys.Any(k => GameUtils.IsWithinFlat(p.Position, k, 1f))),
            "The escort stands on its bridge keys, not snapped to the bank beside them");
        var guess = crew.PedFor(CrewSlot.Guess);
        Check(guess.CurrentVehicle == m.Chopper && guess.SeatIndex == VehicleSeat.Driver,
            "Guess is at the Annihilator's controls instead of it hanging sixty meters up with nobody in it");
        Check(Function.Calls.Any(call => call.Item1 == Hash.SET_HELI_BLADES_FULL_SPEED && call.Item2.Length > 0 && call.Item2[0] == (object)m.Chopper),
            "and it is launched with its rotors at speed");
        Check(m.Consist.Count > 0, "A consist stands on the crossing");
        m.Abort();

        string source = Source("src/Bloodlines/Missions/Campaign/Act3/M62SteelHorizon.cs");
        int load = source.IndexOf("GameUtils.RequestModel(model)", StringComparison.Ordinal);
        int create = source.IndexOf("Function.Call<Vehicle>((Hash)CreateMissionTrain", StringComparison.Ordinal);
        Check(load > 0 && create > load && source.Contains("TrainLandingMeters"),
            "The freight models are loaded before the mission train is asked for, and a train that lands off the crossing is refused");
    }

    static void SiegeAllyChecks()
    {
        Reset(); var crew = Roster(); var c = Context(crew);
        var m = new M60SiegeOfDavis();
        Check(m.Begin(c), "M60 starts");
        Use(crew, CrewSlot.Guess);
        Game.Player.Character.Position = c.Locations.Position("M60.Hold");
        B3Until(m, c, () => m.CurrentStageName == "Hold the line");
        B3Until(m, c, () => m.WavesSeen >= 1);
        Check(m.Allies.Count > 0 && m.Allies.All(a => a.Task.HatedFights >= 1 && !a.BlockPermanentEvents),
            "The neighbors holding the alley are told to fight when the first wave hits the block");

        // Nobody within sight of where the second wave comes on, so anything it knows it was told.
        foreach (var hero in Protagonist.All) crew.PedFor(hero.Slot).Position = c.Locations.Position("M60.Hold") + new Vector3(0f, -150f, 0f);
        var first = m.Raiders.ToList();
        foreach (var raider in first) raider.IsDead = true;
        B3Until(m, c, () => m.WavesSeen >= 2, 30, 3000);
        var second = m.Raiders.Except(first).ToList();
        B3Tick(m, c, 500); B3Tick(m, c, 500);
        Check(second.Count > 0 && second.All(p => p.Task.Fights >= 1),
            "The second wave arrives alarmed: the radio call reaches men created after the fight began");
        Check(m.Allies.All(a => a.Task.HatedFights >= 2), "and the neighbors join that wave's fight as well");
    }

    static void DiveGearChecks()
    {
        Reset(); var crew = Roster(); var c = Context(crew);
        Function.Calls.Clear();
        var m = new M61TheBlackBox();
        Check(m.Begin(c), "M61 starts");
        var surface = c.Locations.Position("M61.Wreck");
        Check(m.Wreck.Position.Z < surface.Z - M61TheBlackBox.RequiredDepth * 0.5f,
            "The wreck is created on the seabed the probe measured, not floating at the surface");
        B3Tick(m, c);
        var gohan = crew.PedFor(CrewSlot.Gohan);
        Check(Function.Calls.Any(call => call.Item1 == Hash.SET_ENABLE_SCUBA && call.Item2[0] == (object)gohan && (bool)call.Item2[1]) &&
              Function.Calls.Any(call => call.Item1 == Hash.SET_PED_MAX_TIME_UNDERWATER && call.Item2[0] == (object)gohan),
            "Gohan goes down with dive gear and the air to work a fourteen-second cut at the bottom");
        var cut = Flow(m).First(s => s.Name == "Cut the server out").Objectives.OfType<MissionInteraction>().First();
        Check(Field<string>(cut, "_animation") == null,
            "The cut is not a standing reach-inside pose played on a swimmer");
        Function.Calls.Clear();
        m.Abort();
        Check(Function.Calls.Any(call => call.Item1 == Hash.SET_ENABLE_SCUBA && call.Item2[0] == (object)gohan && !(bool)call.Item2[1]),
            "and the gear comes off again when the mission ends");
    }

    static void LateActThreeChecks()
    {
        // ---- M63. The technical was a required asset for the whole mission, so losing it to
        // the gun nests after it had already gone through the doors failed the attempt.
        Reset(); var crew = Roster(); var c = Context(crew);
        var m63 = new M63TowerOfGlass();
        Check(m63.Begin(c), "M63 starts");
        Use(crew, CrewSlot.Guess);
        var doors = c.Locations.Position("M63.Doors") + new Vector3(0f, 0f, m63.DeckOffset);
        m63.Technical.Position = doors; m63.Technical.Speed = 0;
        Game.Player.Character.SetIntoVehicle(m63.Technical, VehicleSeat.Driver);
        B3Until(m63, c, () => m63.Breached);
        m63.Technical.IsDriveable = false;
        B3Tick(m63, c); B3Tick(m63, c);
        Check(m63.Breached && m63.Status == MissionStatus.Running,
            "A technical wrecked after it is through the doors no longer fails M63");

        // ---- M66. The other two were left on the roof under three gunships while all three
        // still had to survive.
        Reset(); crew = Roster(); c = Context(crew);
        var m66 = new M66TheSpireEvacuation();
        Check(m66.Begin(c), "M66 starts");
        Use(crew, CrewSlot.Guess);
        Game.Player.Character.Position = c.Locations.Position("M66.Edge");
        B3Until(m66, c, () => m66.CurrentStageName == "Go off the tower");
        Game.Player.Character.Position = MazeBank.Roof - new Vector3(0f, 0f, M66TheSpireEvacuation.JumpedBelow + 20f);
        B3Until(m66, c, () => m66.Jumped);
        var others = new[] { CrewSlot.Ice, CrewSlot.Gohan }.Select(s => crew.PedFor(s)).ToArray();
        Check(m66.Jumped && others.All(p => p.Position.DistanceTo(m66.Landing) < 15f),
            "When he jumps, the other two are taken off the roof and put down at the connector");
        B3Tick(m66, c, 3000);
        Check(m66.Status == MissionStatus.Running, "and nobody is left on the roof for the gunships");

        // ---- M67. The transfer was an on-foot interaction, and Gohan is riding in the cab.
        Reset(); crew = Roster(); c = Context(crew);
        var m67 = new M67ScorchedGrid();
        Check(m67.Begin(c), "M67 starts");
        var semi = m67.Semi; var gohan = crew.PedFor(CrewSlot.Gohan);
        Use(crew, CrewSlot.Guess); semi.Speed = 18;
        B3Until(m67, c, () => m67.Rolling);
        Use(crew, CrewSlot.Gohan); semi.Speed = 18;
        Game.Accept = false; B3Tick(m67, c); Game.Accept = true; B3Tick(m67, c);
        Game.GameTime += M67ScorchedGrid.TransferSeconds * 1000 + 1; B3Tick(m67, c, 0); Game.Accept = false;
        Check(m67.Transferred && gohan.IsInVehicle(semi) && semi.Speed > M67ScorchedGrid.MovingSpeed,
            "Gohan moves the money from his seat in the moving rig");

        // ---- M68. A gun-truck that did not spawn was a null destruction target, which fails.
        Reset(); crew = Roster(); c = Context(crew); Function.Calls.Clear();
        var m68 = new M68BloodBrothersTheDrain();
        Check(m68.Begin(c), "M68 starts");
        var chasers = (List<Vehicle>)m68.Chasers;
        chasers.RemoveAt(chasers.Count - 1);
        Use(crew, CrewSlot.Guess); m68.Rig.Speed = 18;
        B3Until(m68, c, () => m68.Rolling);
        foreach (var truck in chasers) truck.IsDriveable = false;
        B3Until(m68, c, () => m68.ChaseBroken);
        Check(m68.ChaseBroken && m68.Status == MissionStatus.Running,
            "With a gun-truck that never spawned, the pursuit ends when the trucks that did are wrecked");
        var trailer = Field<Vehicle>(m68, "_trailer"); gohan = crew.PedFor(CrewSlot.Gohan);
        Check(Function.Calls.Any(call => call.Item1 == Hash.ATTACH_ENTITY_TO_ENTITY && call.Item2[0] == (object)gohan && call.Item2[1] == (object)trailer),
            "Gohan rides with the rig in the trailer instead of being left at his start point");
        string m68Source = Source("src/Bloodlines/Missions/Campaign/Act3/M68BloodBrothersTheDrain.cs");
        Check(m68Source.Contains("ice.Weapons.Give(WeaponHash.MicroSMG") && Regex.Matches(m68Source, @"Fighting = true;").Count == 1 &&
              m68Source.Contains("Awareness.Release(ped)"),
            "Ice has a weapon he can fire from the seat, and the chase trucks are ordered by the chase alone, from when the rig rolls");

        // ---- M69. "Up the ramp" was the plane's own center.
        Reset(); crew = Roster(); c = Context(crew);
        var m69 = new M69BloodBrothersRunway();
        Check(m69.Begin(c), "M69 starts");
        var ramp = Flow(m69).First(s => s.Name == "Up the ramp").Objectives.OfType<DeliverVehicleObjective>().First();
        var dest = Field<Func<Vector3>>(ramp, "_destination")();
        Check(dest.DistanceTo(m69.Plane.Position) > M69BloodBrothersRunway.RampRadius + 8f,
            "The ramp is behind the Titan's tail, so reaching it does not mean driving under the wing into the plane");
        m69.Abort();
    }

    static void FinaleDitchChecks()
    {
        // The ending used to fail at the moment it happened: a plane in the sea is not
        // driveable, which both the required asset and the delivery objective treated as lost.
        foreach (bool reachesWater in new[] { true, false })
        {
            Reset(); var crew = Roster(); var c = Context(crew);
            var m = new M70BloodBrothersGroundedTitan();
            Check(m.Begin(c), "M70 starts");
            for (int i = 0; i < 80 && m.Status == MissionStatus.Running && m.CurrentStageName == "Hold the fuselage"; i++)
            {
                foreach (var waves in m.CurrentStageObjectives.OfType<SurviveWavesObjective>())
                    foreach (var ped in waves.Spawned) ped.IsDead = true;
                B3Tick(m, c, 3000);
            }
            Use(crew, CrewSlot.Guess);
            Game.Player.Character.SetIntoVehicle(m.Plane, VehicleSeat.Driver);
            B3Until(m, c, () => m.CurrentStageName == "Off the seawall");
            Check(m.CurrentStageName == "Off the seawall", "M70 reaches the run for the water");
            m.Plane.IsDriveable = false; m.Plane.Speed = 0;
            if (reachesWater) m.Plane.IsInWater = true;
            B3Tick(m, c); B3Tick(m, c);
            if (reachesWater)
                Check(m.Away && m.Status != MissionStatus.Failed,
                    "The plane in the water is the ending, not a lost vehicle");
            else
                Check(m.Status == MissionStatus.Failed && m.FailReason.Contains("short of the water"),
                    "A plane that dies on the runway short of the sea fails with a reason instead of waiting forever");
        }
    }
}
