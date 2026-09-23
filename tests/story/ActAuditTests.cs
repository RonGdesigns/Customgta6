using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

/// <summary>
/// The September 22 act-by-act audit: each repair here is a failure Ron reported or the
/// audit found in code, held so it cannot come back.
/// </summary>
public static partial class StoryTests
{
    static void ActAuditChecks()
    {
        M03YardChecks();
        WholeCrewBoardingChecks();
        SeatedBrotherChecks();
        BoardSeatChecks();
        SharedOrderSourceChecks();
    }

    /// <summary>M51 on September 22: Guess was never told to get in, and the crew waited for him until Ron aborted.</summary>
    static void WholeCrewBoardingChecks()
    {
        Reset(); var crew = Roster(); Context(crew);
        var car = new Vehicle { Model = new Model("granger"), Position = Vector3.Zero, Capacity = 3 };
        foreach (var hero in Protagonist.All) crew.PedFor(hero.Slot).Position = new Vector3(5f, 0f, 0f);
        Use(crew, CrewSlot.Gohan);
        var plan = CrewBoarding.Crew(CrewSlot.Guess);
        Check(plan.Length == 3 && plan[0].Key == CrewSlot.Guess && plan[0].Value == VehicleSeat.Driver, "The whole-crew plan puts the named driver at the wheel");
        var boarding = new CrewBoarding();
        boarding.Update(crew, car, plan, "TEST");
        Check(crew.PedFor(CrewSlot.Guess).Task.Enters == 1 && crew.PedFor(CrewSlot.Guess).Task.LastSeat == VehicleSeat.Driver,
            "With Gohan being played, Guess is told to get in and drive");
        Check(crew.PedFor(CrewSlot.Gohan).Task.Enters == 0, "and the player is still never ordered");
        Check(CrewBoarding.IsBoarding(CrewSlot.Guess) && !CrewBoarding.IsBoarding(CrewSlot.Gohan), "A brother being boarded is marked as such");
        Game.GameTime += CrewBoarding.BoardingHoldMs + 1;
        Check(!CrewBoarding.IsBoarding(CrewSlot.Guess), "and the mark expires by itself, so a boarding that stopped never holds him");

        // The player sat in the driver's seat as Gohan: Guess takes another seat rather than waiting for it.
        Reset(); crew = Roster(); Context(crew);
        car = new Vehicle { Model = new Model("granger"), Position = Vector3.Zero, Capacity = 3 };
        foreach (var hero in Protagonist.All) crew.PedFor(hero.Slot).Position = new Vector3(5f, 0f, 0f);
        Use(crew, CrewSlot.Gohan); crew.PedFor(CrewSlot.Gohan).SetIntoVehicle(car, VehicleSeat.Driver);
        boarding = new CrewBoarding();
        boarding.Update(crew, car, CrewBoarding.Crew(CrewSlot.Guess), "TEST");
        var guessSeat = crew.PedFor(CrewSlot.Guess).Task.LastSeat;
        Check(crew.PedFor(CrewSlot.Guess).Task.Enters == 1 && guessSeat != VehicleSeat.Driver && car.IsSeatFree(guessSeat),
            "With the player at the wheel, Guess is sent to a free seat instead");
    }

    /// <summary>M56's gunner and M49's crew were pulled out of their seats by the threat response.</summary>
    static void SeatedBrotherChecks()
    {
        Reset(); var crew = Roster(); Context(crew);
        var ice = crew.PedFor(CrewSlot.Ice);
        var enemy = new Ped { Position = ice.Position + new Vector3(10f, 0f, 0f) };
        var track = new RoleTrack(CrewSlot.Ice, ice, () => new[] { enemy });
        track.Observe(ice.Position, ice.Position);
        var truck = new Vehicle { Model = new Model("halftrack"), Position = ice.Position, Capacity = 3 };
        ice.SetIntoVehicle(truck, VehicleSeat.LeftRear);
        int clears = ice.Task.Clears;
        track.Update(); track.Update();
        Check(track.State == RoleState.Observing && ice.Task.Clears == clears && ice.IsInVehicle(truck),
            "A seated brother with a hostile close by is left in his seat, not sent running for cover");
        ice.Task.LeaveVehicle(); track.Update();
        Check(track.State == RoleState.Threatened, "On his feet, the same threat is answered as before");
    }

    private sealed class BoardProbe : PreparationOperation
    {
        public override string Id => "M31";
        public override string Title => "Board probe";
        protected override bool Setup() => true;
        protected override System.Collections.Generic.IEnumerable<MissionStage> BuildStages() { yield break; }
        public bool Try(Ped actor, Vehicle vehicle, VehicleSeat seat) => Board(actor, vehicle, seat);
    }

    /// <summary>M35's dead gunner, and the player in a seat that was not the one asked for (M41, M54).</summary>
    static void BoardSeatChecks()
    {
        Reset(); var crew = Roster(); Context(crew);
        var probe = new BoardProbe();
        var technical = new Vehicle { Model = new Model("technical"), Position = Vector3.Zero, Capacity = 3 };
        var ice = crew.PedFor(CrewSlot.Ice); ice.Position = new Vector3(3f, 0f, 0f);
        var corpse = new Ped { IsDead = true }; corpse.SetIntoVehicle(technical, VehicleSeat.LeftRear);
        Use(crew, CrewSlot.Guess);
        probe.Try(ice, technical, VehicleSeat.LeftRear);
        Check(!corpse.Present && probe.Status != MissionStatus.Failed, "A dead man in the gun seat is taken out of it instead of failing the mission");

        Use(crew, CrewSlot.Ice); ice.SetIntoVehicle(technical, VehicleSeat.RightFront);
        Check(probe.Try(ice, technical, VehicleSeat.LeftRear), "The player in another seat of the right vehicle counts as aboard");
    }

    static void SharedOrderSourceChecks()
    {
        string prep = Source("src/Bloodlines/Missions/Campaign/Act2/PreparationOperation.cs");
        Check(prep.Contains("if (_supportTargets.TryGetValue(slot, out var current) && current == threat &&") &&
              prep.Contains("else if (fresh) unit.Item2.Task.DriveTo(unit.Item1, target.Position"),
            "Brothers and response cars are ordered once per target, not every cycle");
        string hull = Source("src/Bloodlines/Missions/Objectives/ShootDownObjective.cs");
        Check(hull.Contains("Function.Call(Hash.CLEAR_ENTITY_LAST_WEAPON_DAMAGE, target);"), "One rocket counts once on the hull meter");
        // M51's log: a Guess who was not the player was never told to get in, and the crew
        // waited for him until the mission was aborted.
        foreach (var file in new[] { "M49ReturnToTheConcrete", "M50TheRedactedVault", "M51BlackoutProtocol" })
        {
            string text = Source("src/Bloodlines/Missions/Campaign/Act3/" + file + ".cs");
            Check(text.Contains("_boarding.Update(Ctx.Crew, CrewCar, CrewBoarding.Crew(CrewSlot.Guess), Id)") && !text.Contains("CrewBoarding.Passengers("),
                file + " boards the whole crew, the driver included, when Guess is not the one being played");
        }
    }

    /// <summary>M03's depot: a raised yard at z 42 to 43, and a lower level past its north railing at z 37 to 38.</summary>
    static Vector3 M03TwoLevels(Vector3 p) => p.Y > -1921f ? new Vector3(p.X, p.Y, 37.5f) : new Vector3(p.X, p.Y, 42.5f);

    static void M03YardChecks()
    {
        var yard = new Vector3(1269f, -1952f, 43.2f);
        var lower = M03CypressFoundry.OnYard(new Vector3(1255f, -1914f, 40f), yard, M03TwoLevels);
        Check(Math.Abs(lower.Z - 42.5f) < .01f && lower.Y <= -1921f,
            "A spawn asked for past the north railing is pulled back up onto the yard");
        var upper = M03CypressFoundry.OnYard(new Vector3(1280f, -1935f, 40f), yard, M03TwoLevels);
        Check(upper.X == 1280f && upper.Y == -1935f && Math.Abs(upper.Z - 42.5f) < .01f, "A spawn already on the yard stays where it was asked for");
        var nowhere = M03CypressFoundry.OnYard(new Vector3(1269f, -1900f, 40f), yard, p => Vector3.Zero);
        Check(nowhere.Z == yard.Z && nowhere.DistanceTo(yard) > 5f, "With no walkable answer it still stands at yard height, and never on the truck");
        var edge = new Vector3(1254f, -1928f, 42.6f);
        var close = M03CypressFoundry.OnYard(new Vector3(1256f, -1915f, 40f), edge, M03TwoLevels);
        Check(Math.Abs(close.Z - 42.5f) < .01f && close.Y <= -1921f && close.DistanceTo(edge) >= M03CypressFoundry.TruckClearance - .01f,
            "With the truck right by the railing, the man goes round it onto the yard, never over the edge and never into the truck");

        Reset(); var crew = Roster(); var c = Context(crew);
        c.State = CampaignState.Load(Path.Combine(root, "m03-yard-" + Guid.NewGuid().ToString("N") + ".json"));
        c.Vans = new CrewVan(c.State, c.Locations);
        World.SafeCoordHandler = M03TwoLevels;
        var m = new M03CypressFoundry();
        Check(m.Begin(c), "M03 starts over a two-level depot");
        float level = m.Hauler.Position.Z;
        Check(m.YardGuards.Count == M03CypressFoundry.DepotGuardCount && m.YardGuards.All(g => Math.Abs(g.Position.Z - 42.5f) < .01f),
            "Every depot guard stands on the yard the Benson is on, none on the level below (hauler " + m.Hauler.Position.X + "," + m.Hauler.Position.Y + "," + m.Hauler.Position.Z + ": " + string.Join(" ", m.YardGuards.Select(g => g.Position.X.ToString("0") + "," + g.Position.Y.ToString("0") + "," + g.Position.Z.ToString("0.0"))) + ")");
        m.Abort();
        World.SafeCoordHandler = null;

        string source = Source("src/Bloodlines/Missions/Campaign/Act1/M03CypressFoundry.cs");
        Check(source.Contains("var point = OnYard(_depot + offset, Yard, at => World.GetSafeCoordForPed(at, false, 0));"),
            "The reinforcements that come back through the gate arrive on the yard too");
    }
}
