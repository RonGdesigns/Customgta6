using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Campaign;
using GTA;
using GTA.Math;

public static partial class StoryTests
{
    static string TowerSource(string file) =>
        File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act3", file));

    static void MazeBankChecks()
    {
        string shared = TowerSource("MazeBankTower.cs");
        string m63 = TowerSource("M63TowerOfGlass.cs");
        string m64 = TowerSource("M64TheEightiethFloor.cs");
        string m65 = TowerSource("M65ExecutivePrivilege.cs");
        string m66 = TowerSource("M66TheSpireEvacuation.cs");

        // ---- The tower's stack, straight off the archives. These five numbers are what made
        // four missions buildable, so a change to any of them should have to be deliberate.
        Check(Math.Abs(MazeBank.Garage.Z - 221.00f) < 0.01f && Math.Abs(MazeBank.Garage.X + 84.22f) < 0.01f,
            "The mid-tower floor is the cargarage MLO at its own placement");
        Check(Math.Abs(MazeBank.Office.Z - 242.39f) < 0.01f && Math.Abs(MazeBank.Office.X + 73.80f) < 0.01f,
            "The executive floor is the office MLO at its own placement");
        Check(Math.Abs(MazeBank.Roof.Z - 323.26f) < 0.01f,
            "and the roof is the helipad M54 already established");
        Check(MazeBank.GarageIpl == "imp_dt1_11_cargarage_a" && MazeBank.OfficeIpl == "ex_dt1_11_office_01a",
            "Each floor names the IPL that loads it, and they are this building's own");
        Check(Math.Abs(MazeBank.PlazaHeight - 36.77f) < 0.01f,
            "The plaza deck is dt1_11_dt1_plaza's own height");

        // ---- The plaza is seven meters above the street it overlooks. Every key on it is a
        // fixed surface, or ground preparation hands back the road below.
        var plazaKeys = new[] { "M63.Start", "M63.IceStart", "M63.GohanStart", "M63.GuessStart",
                                "M63.Technical", "M63.Doors", "M63.Core", "M63.Nest1", "M63.Nest2", "M63.Nest3" };
        foreach (var key in plazaKeys)
        {
            var at = KeyPoint(key);
            Check(at.Z > 34f && at.Z < 39f, key + " is on the plaza deck, not the street seven meters below it");
            // The nests are built from a prefix, so the literal key never appears in the
            // source; the prefix and the count are what the mission actually declares.
            Check(m63.Contains("\"" + key + "\"") || m63.Contains("\"M63.Nest\" + i"),
                key + " is named by M63");
        }
        Check(m63.Contains("protected override string[] FixedSurfaces =>") && m63.Contains(".Concat(NestKeys())"),
            "M63 declares the whole plaza a fixed surface, gun nests included");
        Check(m63.Contains("MissionSites.OffsetToSurface(authored, MazeBank.PlazaHeadroom"),
            "and measures the deck once rather than probing every point");
        Check(m63.Split(new[] { "OffsetToSurface(" }, StringSplitOptions.None).Length - 1 == 1,
            "exactly one probe in M63");

        // ---- Nothing inside the tower is authored. Exactly one coordinate exists per floor,
        // its own MLO placement, and nobody has walked either: positions inside come from the
        // arrival point at runtime. This is the rule a day of floating markers bought.
        foreach (var src in new[] { m64, m65 })
        {
            Check(src.Contains("MazeBank.Nearby("),
                "Interior positions are resolved from where the crew actually arrives");
            Check(!src.Contains("new Vector3(-7") && !src.Contains("new Vector3(-8"),
                "and no coordinate inside the tower is hard-coded into the mission");
        }
        Check(shared.Contains("using the arrival point"),
            "A spot the engine refuses falls back to somewhere a man is definitely standing");
        foreach (var key in new[] { "M64.Doors", "M65.Doors" })
            Check(KeyPoint(key).Z < 40f, key + " is the entrance on the plaza, not a floor two hundred meters up");

        // ---- An MLO is opened by the service that owns interiors, and its IPL goes through
        // the one owner of DLC map registration. A plain REQUEST_IPL on DLC map data silently
        // does nothing until those archives are registered.
        Check(shared.Contains("DlcMaps.EnsureRegistered()") && shared.Contains("access.Begin("),
            "MazeBank.Enter registers the DLC archives and lets the access service open the floor");
        foreach (var src in new[] { m63, m64, m65, m66 })
            Check(!src.Contains("REQUEST_IPL") && !src.Contains("Ctx.Interior"),
                "and no mission reaches past it to open a floor itself");

        // ---- M65's one rule. Gohan needs Vance's biometrics, so a Vance shot early is a
        // mission that cannot be finished: it fails now, with a reason, rather than hanging.
        Check(m65.Contains("Vance was killed before Gohan had his biometrics"),
            "Shooting Vance before the escrow is open fails M65 with a reason");
        Check(m65.IndexOf("Open the escrow", StringComparison.Ordinal) <
              m65.IndexOf("Finish it", StringComparison.Ordinal),
            "and the escrow stage comes before the execution stage");

        // ---- M66. The roof is the verified pad, the edge is inside the parapet the lights
        // outline, the gunships are above the rim, and there is no spire to climb.
        var edge = KeyPoint("M66.Edge");
        Check(Math.Abs(KeyPoint("M66.Start").Z - MazeBank.Roof.Z) < 0.1f,
            "M66 starts on the roof helipad itself");
        Check(edge.X > -85f && edge.X < -66f && edge.Y > -828f && edge.Y < -810f,
            "and the jump point is inside the parapet ring the roof lights mark out");
        for (int i = 1; i <= M66TheSpireEvacuation.Gunships; i++)
        {
            Check(KeyKind("M66.Patrol" + i) == "air", "M66.Patrol" + i + " is an air spawn");
            Check(KeyPoint("M66.Patrol" + i).Z > 325.17f,
                "M66.Patrol" + i + " circles above the roof rim rather than through it");
        }
        Check(m66.Contains("AircraftHold.LaunchAirborne(heli)") && m66.Contains("pilot.SetIntoVehicle(heli, VehicleSeat.Driver)"),
            "Its gunships are launched properly and their pilots seated outright");
        Check(m66.Contains("GameUtils.NearestRoadNode(seed, LaneSearch"),
            "The connector landing is resolved from a road node, because roads are baked terrain");
        Check(m66.Contains("WeaponHash.Parachute"), "and everybody gets a parachute");
        Check(!m66.Contains("Spire.") && !m66.Contains("M66.Mast"),
            "Nothing tries to climb an antenna that the archives say is not there");

        // ---- Each mission has a named owner somewhere, which is what lets the failure audit
        // put a required hero down and see the mission fail cleanly.
        foreach (var pair in new[] { Tuple.Create("M63", m63), Tuple.Create("M64", m64),
                                     Tuple.Create("M65", m65), Tuple.Create("M66", m66) })
            Check(pair.Item2.Contains("RequiredCharacter = CrewSlot.") || pair.Item2.Contains(".OwnedBy(CrewSlot."),
                pair.Item1 + " names at least one brother to an objective");
    }
}
