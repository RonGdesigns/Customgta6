using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

public static partial class StoryTests
{
    /// <summary>
    /// SM03, after Ron played it: "a lot of looping around and not a straight path to the
    /// mountain," and "the AI wasn't a challenge." The route is measured here rather than
    /// described, because the loop was a measurable fact and so is its absence.
    /// </summary>
    static void SprintRouteChecks()
    {
        var book = LocationBook.Load(dataDir, Path.Combine(root, "sprint-overrides.ini"), null, CampaignData.Load(dataDir));
        var legs = book.All.Where(l => l.Key.StartsWith("SM03.Leg", StringComparison.Ordinal)).OrderBy(l => l.Key, StringComparer.Ordinal).ToList();
        var trail = book.All.Where(l => l.Key.StartsWith("SM03.Trail", StringComparison.Ordinal)).OrderBy(l => l.Key, StringComparer.Ordinal).ToList();
        var startLine = book.Position("SM03.StartLine");
        var summit = book.Position("SM03.Summit");

        // ---- The route data: shorter, and no longer doubling back.
        Check(!book.All.Any(l => l.Key.StartsWith("SM03.Sprint", StringComparison.Ordinal)),
            "The 278 wandering sprint gates are gone from the location book");
        Check(legs.Count > 8 && trail.Count > 8 && legs.Count + trail.Count < 60,
            "The sprint is a few dozen gates now: a road leg and a mountain climb");
        var chain = new List<Vector3> { startLine };
        chain.AddRange(legs.Select(l => l.Position));
        chain.AddRange(trail.Select(l => l.Position));
        chain.Add(summit);
        int backward = 0;
        float run = 0f;
        for (int i = 1; i < chain.Count; i++)
        {
            run += Flat(chain[i - 1], chain[i]);
            if (Flat(chain[i], summit) > Flat(chain[i - 1], summit) + 1f) backward++;
        }
        Check(backward == 0, "Not one gate sits further from the finish than the gate before it: the looping is gone");
        Check(run < 14000f, "The sprint is about twelve kilometers, not the nineteen it was");
        Check(run > Flat(startLine, summit), "and it is still a road route rather than a straight line drawn over the mountains");
        Check(trail.All(t => t.Position.Z > SM03MidnightDrift.TrailHeight - 5f),
            "Every trail gate is up the mountain, which is what keeps the rivals slowed for them");
        Check(legs.All(l => l.Position.Z <= SM03MidnightDrift.TrailHeight),
            "and every road gate is below it");
        // The climb is the point of the mission, so its gates are tight enough to hold a
        // racer on the switchbacks instead of letting him aim at the summit up a cliff.
        float widest = 0f;
        for (int i = 1; i < trail.Count; i++) widest = Math.Max(widest, Flat(trail[i - 1].Position, trail[i].Position));
        Check(widest < 130f, "No two trail gates are far enough apart to cut a switchback between them");

        // ---- The runtime build: road gates onto real lanes, trail gates left alone.
        GameUtils.RoadAvailable = true;
        try
        {
            var route = RaceRoute.Build(book, "SM03.Leg", "SM03.Trail", "SM03.Summit");
            Check(route.IsUsable && route.Gates.Count == legs.Count + trail.Count + 1,
                "The built route is every road gate, every trail gate and the finish");
            Check(route.Seeds == legs.Count && route.Trail == trail.Count && route.Snapped > 0,
                "The road gates are the ones offered to a vehicle node; the trail gates are not");
            for (int i = 0; i < trail.Count; i++)
                Check(route.Gates[legs.Count + i] == trail[i].Position,
                    "Trail gate " + (i + 1) + " is left exactly where the trail is, never dragged to the road below it");
            Check(route.Report.Contains("on a real lane") && route.Report.Contains("trail gates"),
                "and the build says how much of it stands on a lane rather than on a guess");
        }
        finally { GameUtils.RoadAvailable = false; }

        // A node that would move the race backward is refused, not taken. That is the whole
        // fault this route was rebuilt to remove, and a snap must not reintroduce it.
        GameUtils.RoadAvailable = true;
        // One gate gets a lane two kilometers back down the road. A resolver that answers
        // the same point for every seed proves nothing: each gate would tie with the one
        // before it rather than fall behind it, and a tie is a legitimate correction.
        int answered = 0;
        GameUtils.NodeResolver = p => ++answered == 3 ? new Vector3(startLine.X, startLine.Y - 2000f, startLine.Z) : p;
        try
        {
            var bad = RaceRoute.Build(book, "SM03.Leg", "SM03.Trail", "SM03.Summit");
            Check(bad.Refused == 1 && bad.Snapped == bad.Seeds - 1,
                "The one lane that sat behind the gate before it is refused, and every other gate still snaps");
            Check(bad.Gates[2] == legs[2].Position, "The refused gate keeps its authored seed rather than the bad lane");
            Check(bad.Gates.Count == legs.Count + trail.Count + 1, "and the route still has all of its gates");
            Check(bad.Report.Contains("refused"), "The refusal is reported rather than passed off as a correction");
        }
        finally { GameUtils.NodeResolver = null; GameUtils.RoadAvailable = false; }

        // No node at all is a warning, never a refusal: a race that will not start is worse
        // than gates a few meters off the lane.
        var none = RaceRoute.Build(book, "SM03.Leg", "SM03.Trail", "SM03.Summit");
        Check(none.IsUsable && none.Snapped == 0 && none.Gates.Count == legs.Count + trail.Count + 1,
            "With no road nodes at all the route is still built, on its seeds");

        // ---- Remaining distance, which is how the rivals are paced against the player.
        var gates = new List<Vector3> { new Vector3(0, 0, 0), new Vector3(0, 100, 0), new Vector3(0, 300, 0) };
        Check(Math.Abs(RaceRoute.Remaining(gates, 0, new Vector3(0, -50, 0)) - 350f) < .01f,
            "Remaining distance is the reach to the next gate plus every leg after it");
        Check(RaceRoute.Remaining(gates, 2, new Vector3(0, 250, 0)) < RaceRoute.Remaining(gates, 1, new Vector3(0, 90, 0)),
            "A racer further round the route has less of it left, which is how two of them are compared");
        Check(Math.Abs(RaceRoute.Remaining(gates, 3, Vector3.Zero)) < .01f, "Past the last gate there is nothing left to cover");
        Check(Math.Abs(RaceRoute.Remaining(null, 0, Vector3.Zero)) < .01f, "and an absent route answers zero rather than throwing");

        // ---- The mission: matched cars, a real commanded speed, and the pacing wired up.
        Reset(); var crew = Roster(); var context = Context(crew);
        context.State = CampaignState.Load(Path.Combine(root, "sprint-save.json"));
        var owned = new OwnedVehicle { Id = 1, ModelName = "sultanrs", ModelHash = (uint)Game.GenerateHash("sultanrs"), Garage = "bay-guess", Label = "Sprint car" };
        context.State.Vehicles.Add(owned);
        context.Garages = new GarageService(crew, context.State, context.Locations, null);
        context.Garages.Allowed = () => true;
        var car = context.Garages.Retrieve(owned);
        car.TopSpeed = 74f;
        Game.Player.Character.Position = car.Position;
        Game.Player.Character.SetIntoVehicle(car, VehicleSeat.Driver);
        var sprint = new SM03MidnightDrift();
        Check(sprint.Begin(context), "The sprint starts on an owned car");
        Check(sprint.Route != null && sprint.Route.IsUsable, "and builds its route at runtime rather than reading one list of gates");
        Check(sprint.RivalCars.Count == 2, "Two rivals are on the grid");
        // Ron's correction: a fair grid, never his car. This is the check that refuses the
        // first version of it, which handed both rivals the model he turned up in.
        Check(sprint.RivalCars.All(r => r.Model.Hash != car.Model.Hash),
            "and not one of them is in the player's own car, whatever the matching says");
        Check(sprint.RivalCars.Select(r => r.Model.Hash).Distinct().Count() == sprint.RivalCars.Count,
            "The two of them are two different cars rather than a mirrored pair");
        Check(sprint.Grid.Count == 2 && sprint.Grid.All(m => RivalGrid.Roster.Contains(m)),
            "Both come off KJ's own roster");
        Check(context.Doctor.Lines(40).Any(l => l.Contains("gates") && l.Contains("real lane")),
            "The doctor carries how much of the route landed on a real lane, so a bad run says so in the log");

        // Drive the player to the first gate and the rivals with him, so the pacing runs.
        for (int i = 0; i < 40 && sprint.CurrentStage == 0; i++)
        {
            Game.GameTime += 300; Game.Accept = true;
            if (context.Cutscenes.IsActive) context.Cutscenes.Skip();
            sprint.Tick();
        }
        Check(sprint.CurrentStage == 1, "The grid beat is readied up and the race is open");
        Check(sprint.CommandedSpeed(0) > 39f,
            "A rival is told to drive at his car's own pace, well past the flat 39 m/s that made him harmless");
        // Bounded by the pacing rules for his own car rather than pinned to one number: what
        // the correction is on any given frame depends on where everybody is standing.
        float capable = sprint.RivalCars[0].TopSpeed;
        Check(sprint.CommandedSpeed(0) >= RacePacing.Speed(capable, false, 100000f) - .01f &&
              sprint.CommandedSpeed(0) <= RacePacing.Speed(capable, false, -100000f) + .01f,
            "and that pace is inside the band the pacing rules allow for his own car on asphalt");
        sprint.Abort();

        // ---- The old flat order and the old prefix must not survive anywhere.
        string source = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Solo", "SM03MidnightDrift.cs"));
        Check(!source.Contains("SM03.Sprint"), "The mission no longer reads the wandering gate prefix");
        Check(source.Contains("RivalGrid.For(2,_coupe.Model)") && !source.Contains("var carModel=_coupe.Model;"),
            "The grid is picked from KJ's roster against the player's car rather than cloned from it");
        Check(!source.Contains("target.Z>200f?20f:39f"), "and the flat two-speed order is gone");
        Check(source.Contains("SET_DRIVE_TASK_CRUISE_SPEED"),
            "The pace is trimmed through the cruise speed, so a rival is not re-tasked every review and made to hesitate");
        Check(source.Contains("SET_DRIVER_AGGRESSIVENESS,driver,1f"), "The rivals drive as aggressively as the engine allows");
        Check(!source.Contains("SET_VEHICLE_CHEAT_POWER_INCREASE") && !source.Contains("SET_VEHICLE_MAX_SPEED"),
            "and nothing here writes the power or the speed cap, which WorldTuning owns per car");
    }

    private static float Flat(Vector3 a, Vector3 b) =>
        (float)Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
