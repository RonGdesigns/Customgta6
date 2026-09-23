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
    static void SubterraneanSweepChecks()
    {
        string src = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act3", "M53SubterraneanSweep.cs"));
        string desert = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act2", "DesertOperation.cs"));
        string prep = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act2", "PreparationOperation.cs"));

        // ---- This is a subway, and the proof is the vertical gap. The job is taken at street
        // level and fought twenty meters under it; if those two ever converge, someone has
        // flattened the mission onto the road by accident.
        var entrance = KeyPoint("M53.Entrance");
        var train = KeyPoint("M53.Train");
        Check(entrance.Z > 28f, "The job is taken at street level above the metro entrance");
        Check(train.Z > 12f && train.Z < 15f, "and the carriage is at the tunnel's own height");
        Check(entrance.Z - train.Z > 15f, "which is a real subway, eighteen meters down");

        foreach (var key in new[] { "M53.Start", "M53.IceStart", "M53.GohanStart", "M53.GuessStart",
                                    "M53.Mine1", "M53.Mine2", "M53.Mine3" })
            Check(KeyPoint(key).Z > 12f && KeyPoint(key).Z < 15f, key + " is on the tunnel floor");
        for (int i = 1; i <= M53SubterraneanSweep.SquadSize; i++)
            foreach (var squad in new[] { "A", "B" })
                Check(KeyPoint("M53.Squad" + squad + i).Z > 12f && KeyPoint("M53.Squad" + squad + i).Z < 15f,
                    "M53.Squad" + squad + i + " is in the tunnel, not on the boulevard above it");

        // ---- Every tunnel key is a fixed surface. Ground preparation asks for walkable
        // ground, and twenty meters under Vespucci Boulevard the answer is the boulevard.
        Check(src.Contains("protected override string[] FixedSurfaces =>") &&
              src.Contains(".Concat(MineKeys)") && src.Contains(".Concat(SquadKeys())"),
            "Every tunnel key including the mines and both squads is declared a fixed surface");
        Check(!src.Contains("\"M53.Entrance\"") || !src.Contains("\"M53.Entrance\" }"),
            "and the street-level entrance is left to ground preparation");

        // ---- One probe, not seventeen. The tunnel section origins on this run are all at
        // 13.03, so the floor is flat and one measurement describes it.
        Check(src.Contains("MissionSites.OffsetToSurface(authored, TunnelHeadroom, TunnelFloor"),
            "The tunnel floor is measured once, at the carriage");
        Check(src.Contains("private Vector3 Down(string key) => At(key) + new Vector3(0f, 0f, _floorOffset)"),
            "and every other tunnel point moves by the offset that probe found");
        // Two flat data, two probes: the tunnel's section origins sit at 13.03 and the
        // platform's at 13.64, and moving the platform squads by the tunnel's offset could put
        // them inside the platform slab (Ron, September 22). Still never one per point.
        Check(src.Split(new[] { "OffsetToSurface(" }, StringSplitOptions.None).Length - 1 == 2 &&
              src.Contains("MissionSites.OffsetToSurface(platform, PlatformHeadroom, TunnelFloor") &&
              !src.Contains("MissionSites.OnSurface"),
            "There is one probe per flat datum - the tunnel and the platform - not one per point");

        // ---- A contractor down here is placed, not snapped. Guard accepts a walkable answer
        // up to 35 m away, and the street is inside that, so all eight would have spawned in
        // traffic.
        Check(src.Contains("EnemyAt(OnPlatform(key), key)"),
            "The contractors are created at settled platform positions");
        Check(!src.Contains("Enemy(\"M53."), "and never through the navmesh-snapping overload");
        Check(desert.Contains("bool trustPoint = false") && desert.Contains("if (!trustPoint)"),
            "Guard can be told to trust a point the caller has already settled");
        Check(prep.Contains("protected Ped Enemy(string key) => Register(Guard(At(key)), key);"),
            "and the ordinary Enemy(key) path keeps the navmesh snap every other mission relies on");

        // ---- Eight, because M53_S1_01_GOHAN says a sweep team of eight. A line that counts
        // the enemy is a line that can be wrong.
        Check(M53SubterraneanSweep.SquadSize * 2 == 8, "There are eight contractors, as the authored line states");
        string dialogue = File.ReadAllText(Path.Combine(dataDir, "dialogue.tsv"));
        Check(dialogue.Contains("Sweep team of eight"), "and that line still says eight");

        // ---- Night vision is held by name and let go once. Left on, it is a green screen the
        // player cannot clear from any menu, on a save he keeps playing.
        Check(src.Contains("NightVision.Wear(Goggles)") && src.Contains("NightVision.Remove(Goggles)"),
            "The goggles are held by name and released by name");
        Check(src.Contains("protected override void OnCleanup()") &&
              src.IndexOf("NightVision.Remove", StringComparison.Ordinal) >
              src.IndexOf("protected override void OnCleanup()", StringComparison.Ordinal),
            "and the release is in OnCleanup, which every exit runs through");
        Check(!src.Contains("SET_NIGHTVISION"), "The mission never touches the native itself");
        NightVision.Reset();
        NightVision.Wear("first"); NightVision.Wear("second");
        Check(NightVision.Active, "Two holders keep the goggles on");
        NightVision.Remove("first");
        Check(NightVision.Active, "and one letting go does not take them off the other");
        NightVision.Remove("second");
        Check(!NightVision.Active, "The last holder releasing takes them off");
        NightVision.Wear("stuck"); NightVision.Reset();
        Check(!NightVision.Active, "and Reset drops every claim");

        // ---- The sweep is ordered on a cadence. A fresh task every tick restarts it before
        // the ped can act on it, which is what left M31, M33 and M37's guards standing still.
        Check(src.Contains("_orderAt = Game.GameTime + SweepOrderMs") && M53SubterraneanSweep.SweepOrderMs >= 3000,
            "Sweepers are re-ordered on a slow cadence, never every frame");
        Check(src.Contains("if (ped.IsInCombat) continue;"),
            "and a man already fighting is not walked away from the fight");

        // ---- The way out is a real placed walkway, not a point in a wall.
        var exit = KeyPoint("M53.Exit");
        Check(exit.Z > train.Z + 5f && exit.Z < entrance.Z,
            "The exit is the station walkway, above the track and below the street");

        // ---- A stalled carriage that will not load is thinner scenery, not a dead mission.
        Check(src.Contains("the tunnel fight runs without it"),
            "A carriage that cannot be created is reported and the ambush continues");
        Check(src.Contains("!_wired || !_cleared"),
            "It cannot pass without the tripwires set and both squads down");
    }
}
