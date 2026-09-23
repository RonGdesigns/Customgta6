using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

/// <summary>
/// Two placement changes Ron asked for on September 22. M52 moves to where a ladder
/// actually is: City Hall's west wing, whose climbable ladders (bh1_21_ladder2) run from the
/// street to its roof, instead of a scaffold nobody had climbed up to bh1_16's roof. M16's
/// "Clear the canyon" gets a canyon exit to reach, because it aimed at the canyon run point
/// itself with a wider radius than the stage before it and so finished the moment it opened.
/// </summary>
public static partial class StoryTests
{
    static void SiteFixChecks()
    {
        JudicialStrikeLadderChecks();
        HeavyLiftCanyonExitChecks();
    }

    static void JudicialStrikeLadderChecks()
    {
        // ---- The keys. The scaffold is gone, and the climb starts at the foot of a real
        // ladder on the same building as the roost.
        var book = File.ReadAllLines(Path.Combine(dataDir, "locations.tsv")).Select(l => l.Split('\t')[0]).ToArray();
        Check(!book.Contains("M52.Scaffold"), "The scaffold nobody climbed is no longer a key");
        var ladder = KeyPoint("M52.Ladder");
        var roost = KeyPoint("M52.Roost");
        var steps = KeyPoint("M52.Harrison");
        var mark = KeyPoint("M52.Walk");
        // bh1_21_ladder2's street ladder: bottom (-587.56, -200.50, 37.67), top 45.67.
        Check(ladder.DistanceTo2D(new Vector3(-587.56f, -200.50f, 0f)) < 1.5f && ladder.Z < 37.67f,
            "M52.Ladder stands at the bottom of the wing's street ladder, at grade");
        // The same model's plaza-facing ladder tops out on the 48.95 roof at (-571.84, -218.77).
        Check(roost.DistanceTo2D(new Vector3(-571.84f, -218.77f, 0f)) < 1.5f && Math.Abs(roost.Z - 48.95f) < 1f,
            "M52.Roost is on the wing roof at the top of its plaza-facing ladder");
        Check(mark.DistanceTo2D(roost) > steps.DistanceTo2D(roost),
            "Harrison walks away from the roost to his clear-shot mark, never toward it");
        Check(mark.DistanceTo2D(roost) < 120f && roost.Z - mark.Z > 10f,
            "and the mark is a rifle range out and well below the roof");

        // ---- Behavior. The roof is not probed in Setup: it is measured once Ice is close
        // enough for its collision to be loaded, and the objective follows the measurement.
        const float slab = 48.95f;
        Reset(); var crew = Roster(); var c = Context(crew);
        World.RaycastHandler = (s, t) => s.X == t.X && s.Y == t.Y && s.Z >= slab && t.Z <= slab
            ? new RaycastResult { DidHit = true, HitPosition = new Vector3(s.X, s.Y, slab) }
            : new RaycastResult();
        var m = new M52JudicialStrike();
        try
        {
            Check(m.Begin(c), "M52 starts");
            var authored = c.Locations.Position("M52.Roost");
            Check(Math.Abs(m.Roost.Z - authored.Z) < .01f && !m.RoostSettled,
                "Setup does not probe the roof");

            World.CollisionReady = true;
            var ice = crew.PedFor(CrewSlot.Ice);
            ice.Position = c.Locations.Position("M52.Start");
            DrainPreparation(m, c);
            Check(!m.RoostSettled && Math.Abs(m.Roost.Z - authored.Z) < .01f,
                "From the street 88 m away the roof is still not measured");

            Use(crew, CrewSlot.Ice);
            Game.Player.Character.Position = c.Locations.Position("M52.Ladder");
            Game.GameTime += M52JudicialStrike.RoostProbeMs + 1;
            DrainPreparation(m, c);
            Check(m.RoostSettled && Math.Abs(m.Roost.Z - slab) < .01f,
                "At the foot of the ladder the roof is measured onto its slab");
            var climb = StageNamed(m, "Get on the roof").Objectives.OfType<ReachZoneObjective>().Single();
            Check(Field<Func<Vector3>>(climb, "_position")().DistanceTo(m.Roost) < .01f,
                "and the climb objective points at the measured roof, not the authored height");
            Check(Regex.IsMatch(climb.Label, @"\bladder\b") && !climb.Label.Contains("scaffold"),
                "The objective tells him to climb the ladder");
        }
        finally { World.RaycastHandler = null; World.CollisionReady = false; m.Abort(); }

        // ---- No probe call anywhere inside Setup. Matched as calls, on text with its line
        // endings flattened.
        string src = Regex.Replace(Source("src/Bloodlines/Missions/Campaign/Act3/M52JudicialStrike.cs"), @"\s+", " ");
        int setup = src.IndexOf("protected override bool Setup()", StringComparison.Ordinal);
        int stages = src.IndexOf("protected override IEnumerable<MissionStage> BuildStages()", StringComparison.Ordinal);
        Check(setup > 0 && stages > setup && !Regex.IsMatch(src.Substring(setup, stages - setup), @"\b(OnSurface|SurfaceHeight)\("),
            "No roof probe runs inside Setup");
        Check(!src.Contains("At(\"M52.Scaffold\")"), "Nothing asks for the scaffold key");

        // ---- The adaptation is recorded in the overlay, never in the extraction.
        Reset(); c = Context(Roster());
        var info = c.Data.Mission("M52");
        Check(info != null && info.Synopsis.Contains("west wing") && info.Synopsis.Contains("bh1_21_ladder2"),
            "The journal describes the west wing and its ladders, not the Union Depository");
    }

    static void HeavyLiftCanyonExitChecks()
    {
        var run = KeyPoint("M16.CanyonRun");
        var exit = KeyPoint("M16.CanyonExit");
        // The run stage passes inside 120 m of the run point; this one has to be further on
        // than that plus its own radius, or it is finished the moment it opens.
        Check(exit.DistanceTo(run) > 120f + M16TheHeavyLift.CanyonExitRadius + 100f,
            "The canyon exit is well beyond anywhere the canyon run stage can end");
        var kind = File.ReadAllLines(Path.Combine(dataDir, "locations.tsv")).Select(l => l.Split('\t'))
            .First(col => col[0] == "M16.CanyonExit")[5];
        Check(kind == "air", "and it is an air point, so ground preparation leaves it over the water");

        Reset(); var crew = Roster(); var c = Context(crew);
        // Reset does not clear this, so it is put back the way it was found.
        bool road = GameUtils.RoadAvailable; GameUtils.RoadAvailable = true;
        var m = new M16TheHeavyLift();
        try
        {
            Check(m.Begin(c), "M16 starts");
            c.Cutscenes.Skip(); m.Tick();
            int stage = Flow(m).FindIndex(s => s.Name == "Clear the canyon");
            Check(stage > 0 && Flow(m)[stage - 1].Name == "Raton Canyon", "Clearing the canyon follows the canyon run");

            // Where the run stage ends: at the run point, and as far toward the exit as its
            // 120 m radius allows.
            Use(crew, CrewSlot.Guess);
            Game.Player.Character.SetIntoVehicle(m.Cargobob, VehicleSeat.Driver);
            m.Cargobob.Position = run;
            m.JumpToStage(stage); m.Tick();
            Check(m.CurrentStage == stage, "Arriving at the canyon run point does not clear the canyon");
            var toward = (exit - run) * (1f / exit.DistanceTo(run));
            m.Cargobob.Position = run + toward * 119f; m.Tick();
            Check(m.CurrentStage == stage, "nor does the far edge of the run point's circle");

            // Standing at the exit on foot is not the lift getting out.
            Game.Player.Character.Task.LeaveVehicle();
            Game.Player.Character.Position = exit; m.Tick();
            Check(m.CurrentStage == stage, "The player at the exit without the lift does not clear it");

            Game.Player.Character.SetIntoVehicle(m.Cargobob, VehicleSeat.Driver);
            m.Cargobob.Position = exit; m.Tick();
            Check(m.CurrentStage == stage + 1 && Flow(m)[m.CurrentStage].Name == "Terminal Island",
                "The lift out of the canyon's east mouth with Guess flying it clears the canyon");
        }
        finally { m.Abort(); GameUtils.RoadAvailable = road; }
    }
}
