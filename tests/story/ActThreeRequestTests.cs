using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using GTA;
using GTA.Math;
using GTA.Native;

/// <summary>
/// Four Act III changes Ron asked for on September 22, after the act audit: Vance protected
/// from everyone's fire but the player's until the escrow opens (M65), a car of Ice's own on
/// the extraction run (M67), the chase trucks behind the rig instead of in front of it (M68),
/// and the standing freight coupled into one train (M62). Each check drives the mission rather
/// than reading it.
/// </summary>
public static partial class StoryTests
{
    static void ActThreeRequestChecks()
    {
        VanceCrossfireChecks();
        IceEscortChecks();
        IceEscortArrivalChecks();
        DrainChaserChecks();
        StandingFreightChecks();
    }

    /// <summary>Whether a native was called on this entity with this flag.</summary>
    static bool CalledOn(Hash hash, Entity entity, bool flag) =>
        Function.Calls.Any(call => call.Item1 == hash && call.Item2.Length > 1 &&
            ReferenceEquals(call.Item2[0], entity) && call.Item2[1] is bool value && value == flag);

    /// <summary>How far along a heading a point is from an origin, and how far off that line.</summary>
    static void Along(Vector3 origin, Vector3 forward, Vector3 point, out float along, out float across)
    {
        var flat = new Vector3(forward.X, forward.Y, 0f);
        flat = flat * (1f / flat.Length());
        var offset = new Vector3(point.X - origin.X, point.Y - origin.Y, 0f);
        along = offset.X * flat.X + offset.Y * flat.Y;
        across = Math.Abs(offset.X * flat.Y - offset.Y * flat.X);
    }

    static void VanceCrossfireChecks()
    {
        Reset(); var crew = Roster(); var c = TowerContext(crew);
        var m = new M65ExecutivePrivilege();
        Check(m.Begin(c), "M65 starts for the crossfire checks");
        B3Work(m, c, c.Locations.Position("M65.Doors"), M65ExecutivePrivilege.LiftCallSeconds);
        Function.Calls.Clear();
        B3Until(m, c, () => m.Inside);
        Check(m.Inside && m.Vance != null && CalledOn(Hash.SET_ENTITY_ONLY_DAMAGED_BY_PLAYER, m.Vance, true),
            "Until the escrow is open only the player's own fire can hurt Vance, so a guard's or a brother's stray round cannot fail M65");

        foreach (var guard in m.Detail) guard.IsDead = true;
        B3Until(m, c, () => m.DetailDown);
        Use(crew, CrewSlot.Gohan);
        Function.Calls.Clear();
        B3Work(m, c, Field<Vector3>(m, "_terminal"), M65ExecutivePrivilege.BiometricSeconds);
        Check(m.Escrow && CalledOn(Hash.SET_ENTITY_ONLY_DAMAGED_BY_PLAYER, m.Vance, false),
            "Once Gohan has the biometrics the protection comes off, so a brother can take the shot");
        m.Abort();

        // The rule the protection leaves in place: the player shooting him early still fails.
        Reset(); crew = Roster(); c = TowerContext(crew);
        m = new M65ExecutivePrivilege();
        Check(m.Begin(c), "M65 starts again");
        B3Work(m, c, c.Locations.Position("M65.Doors"), M65ExecutivePrivilege.LiftCallSeconds);
        B3Until(m, c, () => m.Inside);
        m.Vance.IsDead = true;
        B3Tick(m, c);
        Check(m.Status == MissionStatus.Failed && m.FailReason != null && m.FailReason.Contains("biometrics"),
            "Vance killed before the escrow still fails M65 with its reason");
    }

    static void IceEscortChecks()
    {
        Reset(); var crew = Roster(); var c = Context(crew);
        World.Nearby = new Ped[0];
        var m = new M67ScorchedGrid();
        Check(m.Begin(c), "M67 starts with Ice's car");
        Use(crew, CrewSlot.Guess);
        var semi = m.Semi; var car = m.Escort; var ice = crew.PedFor(CrewSlot.Ice);
        Check(car != null && car.Exists() && car.Model.Name == M67ScorchedGrid.EscortModel,
            "Ice has a car of his own on the run: the rig's cab seats only Guess and Gohan");
        Along(semi.Position, semi.ForwardVector, car.Position, out float along, out float across);
        Check(along < -18f && across < 1f,
            "It waits in line behind the rig, clear of the trailer's tail");

        B3Tick(m, c);
        Check(ice.Task.Enters == 1 && ice.Task.LastSeat == VehicleSeat.Driver && ice.Task.LastEnterSpeed >= 2f,
            "Ice is sent to the wheel of his car at a run");
        for (int i = 0; i < 5; i++) B3Tick(m, c);
        Check(ice.Task.Enters == 1, "and the order is given once, not every tick");
        B3Tick(m, c, M67ScorchedGrid.BoardRenewMs);
        Check(ice.Task.Enters == 2, "A boarding that has not happened is ordered again after its interval");
        Check(!crew.CompanionAI.IsRejoining(CrewSlot.Ice),
            "Driving his car is his job, so the idle rule never hands him to a player whose cab has no seat for him");

        ice.SetIntoVehicle(car, VehicleSeat.Driver);
        B3Tick(m, c);
        Check(ice.Task.VehicleMissions == 1 && ice.Task.MissionTarget == semi && m.IceLastOrder == M67ScorchedGrid.IceOrder.Escort,
            "At the wheel, he escorts the rig");
        for (int i = 0; i < 5; i++) B3Tick(m, c);
        Check(ice.Task.VehicleMissions == 1, "and the escort is not reissued while it stands");

        // The rig pulls away and his car does not move: the task was lost.
        semi.Speed = 18f; car.Speed = 0f;
        semi.Position = semi.Position + new Vector3(0f, 200f, 0f);
        B3Tick(m, c);
        B3Tick(m, c, M67ScorchedGrid.EscortStallMs + 1);
        Check(ice.Task.VehicleMissions == 2, "A car left standing while the rig drives away is given the escort again");
        B3Tick(m, c);
        Check(ice.Task.VehicleMissions == 2, "once, not on every tick after that");
        car.Speed = 18f;

        // Something comes for the rig: he goes after it from the wheel, once.
        var cop = new Ped { Position = ice.Position + new Vector3(30f, 0f, 0f), RelationshipGroup = Game.GenerateHash("COP") };
        World.Nearby = new[] { cop };
        Game.Player.WantedLevel = 2;
        Function.Calls.Clear();
        B3Tick(m, c);
        Check(ice.Task.Fights == 1 && ice.Task.LastTarget == cop && m.IceLastOrder == M67ScorchedGrid.IceOrder.Fight &&
              Function.Calls.Any(call => call.Item1 == Hash.SET_PED_COMBAT_ATTRIBUTES && ReferenceEquals(call.Item2[0], ice) &&
                  (int)call.Item2[1] == 2 && (bool)call.Item2[2]),
            "With the police on the rig, Ice pursues and shoots from the wheel");
        for (int i = 0; i < 4; i++) B3Tick(m, c);
        Check(ice.Task.Fights == 1, "and the fight is ordered once while he is in it");
        World.Nearby = new Ped[0];
        Game.Player.WantedLevel = 0;
        int escorts = ice.Task.VehicleMissions;
        B3Tick(m, c);
        Check(ice.Task.VehicleMissions == escorts + 1 && m.IceLastOrder == M67ScorchedGrid.IceOrder.Escort,
            "When the threat is gone he goes back to escorting the rig");

        // The player takes Ice: the car is his, and the mission gives Ice no orders.
        Use(crew, CrewSlot.Ice);
        int enters = ice.Task.Enters, missions = ice.Task.VehicleMissions, fights = ice.Task.Fights;
        for (int i = 0; i < 4; i++) B3Tick(m, c);
        Check(ice.Task.Enters == enters && ice.Task.VehicleMissions == missions && ice.Task.Fights == fights &&
              m.IceLastOrder == M67ScorchedGrid.IceOrder.None,
            "When the player is Ice, the car is his to drive and nothing orders him");
        Check(m.Status == MissionStatus.Running, "and M67 carries on with Ice in his own vehicle");
        m.Abort();
    }

    static void IceEscortArrivalChecks()
    {
        // The whole run, played as Ice in his own car from the airport stage on. The travel leg
        // alone waited for the player to be in the rig's cab.
        Reset(); var crew = Roster(); var c = Context(crew);
        var m = new M67ScorchedGrid();
        Check(m.Begin(c), "M67 starts for the arrival check");
        var semi = m.Semi;
        Use(crew, CrewSlot.Guess); semi.Speed = 18;
        B3Until(m, c, () => m.Rolling);
        Use(crew, CrewSlot.Gohan); semi.Speed = 18;
        Game.Accept = false; B3Tick(m, c); Game.Accept = true; B3Tick(m, c);
        Game.GameTime += M67ScorchedGrid.TransferSeconds * 1000 + 1; B3Tick(m, c, 0); Game.Accept = false;
        B3Until(m, c, () => m.CurrentStageName == "Make the airport", 10);
        Check(m.Transferred && m.CurrentStageName == "Make the airport", "The money is moved and the rig is heading for the airport");

        var ice = crew.PedFor(CrewSlot.Ice);
        Use(crew, CrewSlot.Ice);
        ice.SetIntoVehicle(m.Escort, VehicleSeat.Driver);
        for (int i = 0; i < 3; i++) B3Tick(m, c);
        Check(m.Status == MissionStatus.Running && m.CurrentStageName == "Make the airport",
            "Driving Ice's car does not end the run before the rig is there");
        semi.Position = c.Locations.Position("M67.Airport"); semi.Speed = 0;
        B3Tick(m, c); B3Tick(m, c);
        Check(m.Status == MissionStatus.Running && m.CurrentStageName == "Radio debrief",
            "The rig stopped at the airport ends the run while the player drives Ice's car; nobody has to switch into the cab");
        // The harness has no dialogue clock; the call is heard by clearing it, as the flow walker does.
        for (int i = 0; i < 10 && m.Status == MissionStatus.Running; i++) { c.Dialogue.Clear(); B3Tick(m, c); }
        Check(m.Status == MissionStatus.Passed, "and M67 passes after the crew's radio call");
    }

    static void DrainChaserChecks()
    {
        Reset(); var crew = Roster(); var c = Context(crew);
        GameUtils.RoadAvailable = false; GameUtils.NodeResolver = null;
        var m = new M68BloodBrothersTheDrain();
        Check(m.Begin(c), "M68 starts for the chase placement checks");
        var rig = m.Rig;
        Check(m.Chasers.Count == M68BloodBrothersTheDrain.ChaserCount, "Every gun-truck is placed");
        bool behind = true, onLine = true, facing = true;
        foreach (var truck in m.Chasers)
        {
            Along(rig.Position, rig.ForwardVector, truck.Position, out float along, out float across);
            behind &= along < -40f;
            onLine &= across <= 7f;
            facing &= Math.Abs(truck.Heading - rig.Heading) < 0.01f;
        }
        Check(behind && onLine && facing,
            "The gun-trucks start behind the rig on its own line down the channel, facing the way it faces, not in front of it");
        float floor = c.Locations.Position("M68.Rig").Z + m.FloorOffset;
        Check(m.Chasers.All(t => Math.Abs(t.Position.Z - floor) < 0.01f),
            "With no road node on the channel floor they stand on the floor the probe measured");
        m.Abort();

        // A node on the floor beside the derived point is used.
        Reset(); crew = Roster(); c = Context(crew);
        GameUtils.RoadAvailable = true; GameUtils.NodeResolver = p => p + new Vector3(1.5f, 0f, 0f);
        m = new M68BloodBrothersTheDrain();
        Check(m.Begin(c), "M68 starts with road nodes in the channel");
        floor = c.Locations.Position("M68.Rig").Z + m.FloorOffset;
        bool snapped = true;
        for (int i = 0; i < m.Chasers.Count; i++)
        {
            var derived = M68BloodBrothersTheDrain.BehindTheRig(m.Rig.Position, m.Rig.ForwardVector, floor, i);
            snapped &= m.Chasers[i].Position.DistanceTo(derived + new Vector3(1.5f, 0f, 0f)) < 0.01f;
        }
        Check(snapped, "A vehicle node on the channel floor near the derived start is where the truck is put");
        m.Abort();

        // The street twenty-eight meters overhead is not the channel.
        Reset(); crew = Roster(); c = Context(crew);
        GameUtils.RoadAvailable = true; GameUtils.NodeResolver = p => p + new Vector3(0f, 0f, 28f);
        m = new M68BloodBrothersTheDrain();
        Check(m.Begin(c), "M68 starts with only the street's nodes nearby");
        floor = c.Locations.Position("M68.Rig").Z + m.FloorOffset;
        Check(m.Chasers.All(t => Math.Abs(t.Position.Z - floor) < 0.01f),
            "A node on the street above the channel is refused, and the truck stays on the channel floor");
        m.Abort();
        GameUtils.RoadAvailable = false; GameUtils.NodeResolver = null;

        // The derivation follows the rig's real heading, not the harness's default one.
        double radians = 230.0 * Math.PI / 180.0;
        var forward = new Vector3((float)-Math.Sin(radians), (float)Math.Cos(radians), 0f);
        var origin = new Vector3(-306.4f, -1731.7f, 0f);
        bool alwaysBehind = Enumerable.Range(0, M68BloodBrothersTheDrain.ChaserCount).All(i =>
        {
            Along(origin, forward, M68BloodBrothersTheDrain.BehindTheRig(origin, forward, -0.4f, i), out float a, out float x);
            return a <= -M68BloodBrothersTheDrain.ChaserBehind[0] + 0.01f && x <= 7f;
        });
        Check(alwaysBehind, "At the authored heading of 230 the starts lie up the channel, behind the rig");
    }

    static void StandingFreightChecks()
    {
        Reset(); var crew = Roster(); var c = TowerContext(crew);
        World.FailNavigationNear = c.Locations.Position("M62.Container");
        var m = new M62SteelHorizon();
        Check(m.Begin(c), "M62 starts for the standing freight checks");
        Check(!m.RealTrain && m.Consist.Count == 1 + M62SteelHorizon.FallbackCars,
            "With no mission train, an engine and its freight cars stand on the crossing");
        var head = m.Consist[0].Position;
        var crossing = c.Locations.Position("M62.Consist");
        double radians = c.Locations.Heading("M62.Consist") * Math.PI / 180.0;
        var facing = new Vector3((float)-Math.Sin(radians), (float)Math.Cos(radians), 0f);
        bool coupled = true, straight = true, trailing = true;
        float expected = M62SteelHorizon.EngineLength / 2f + M62SteelHorizon.CouplingGap + M62SteelHorizon.CarLength / 2f;
        for (int i = 1; i < m.Consist.Count; i++)
        {
            float gap = m.Consist[i].Position.DistanceTo(m.Consist[i - 1].Position);
            coupled &= Math.Abs(gap - expected) < 0.05f;
            expected = M62SteelHorizon.CarLength + M62SteelHorizon.CouplingGap;
            Along(head, facing, m.Consist[i].Position, out float along, out float across);
            trailing &= along < 0f;
            straight &= across < 0.5f;
        }
        Check(coupled, "Each car stands one car length from the one before it: coupled end to end, not scattered");
        Check(straight && trailing, "They run in one straight line along the rail, behind the engine");
        Check(m.Consist.All(v => GameUtils.IsWithinFlat(v.Position, crossing, 60f)),
            "The whole consist is at the crossing, not spread over two hundred and thirty meters of bridge");
        m.Abort();
    }
}
