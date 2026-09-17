using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bloodlines.Core;
using GTA.Math;

public static partial class StoryTests
{
    /// <summary>
    /// The Act III audit. Every main mission from M49 derives from PreparationOperation, and
    /// the flow walker aborted every PreparationOperation before its first stage, so not one
    /// of these had ever been driven through its beats - even in the harness. Three defects
    /// were found by reading, and each of these checks is one of them, written so it cannot
    /// come back.
    /// </summary>
    static void ActThreeAuditChecks()
    {
        string act3 = Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act3");
        string m57 = File.ReadAllText(Path.Combine(act3, "M57VespucciFlak.cs"));
        string m65 = File.ReadAllText(Path.Combine(act3, "M65ExecutivePrivilege.cs"));
        string m61 = File.ReadAllText(Path.Combine(act3, "M61TheBlackBox.cs"));
        string m51 = File.ReadAllText(Path.Combine(act3, "M51BlackoutProtocol.cs"));
        string walker = File.ReadAllText(Path.Combine(Repo, "tests", "story", "CampaignFlowTests.cs"));

        // ---- The walker no longer turns Act III away at the door.
        Check(walker.Contains("int.Parse(m.Id.Substring(1))>=49"),
            "The flow walker drives every main mission from M49 through its stages");

        // ---- M57. Three gunships were three DestroyVehicle objectives under AnyOf: the first
        // one down ended the stage, the crew went back to the sand with two flying, and
        // OnPassed threw "all three have to be down" - a script error, not a failure.
        int water = m57.IndexOf("\"Put them in the water\"", StringComparison.Ordinal);
        int after = m57.IndexOf("yield return", water + 10, StringComparison.Ordinal);
        Check(water > 0 && !m57.Substring(water, after - water).Contains(".AnyOf()"),
            "M57's gunship stage needs every gunship down, not the first one");
        Check(!m57.Contains("throw new InvalidOperationException(\"All three gunships have to be down.\")"),
            "and OnPassed no longer throws about it, because the stage cannot close otherwise");
        Check(m57.Contains("if (_gunships.Count != Gunships)"),
            "Setup refuses with fewer than three gunships, since a missing one is a null target that fails the objective");

        // ---- M65. RequireAsset fails the mission when the entity is dead, and the last stage
        // is Ice killing him. The SM07 fault, in M65.
        Check(!Regex.IsMatch(m65, @"RequireAsset\(\s*_vance"),
            "M65 puts no RequireAsset on the man the mission ends by killing");
        Check(m65.Contains("if (_inside && !_escrow && _vance != null && _vance.Exists() && _vance.IsDead)"),
            "and Vance dead before the biometrics still fails with its own reason");

        // ---- M61. Enemy(key) snaps to walkable ground, which over the basin is the quay:
        // four demolition divers standing on the dock with rifles.
        Check(!m61.Contains("Enemy(\"M61.Diver\""), "M61's divers are not placed through the ground-snapping spawner");
        Check(m61.Contains("surface = MarineSites.ResolveOrThrow(Ctx.Locations, key, DiverDepth") &&
              m61.Contains("var ped = EnemyAt(surface, key);"),
            "They are resolved to the water surface, the way the wreck is, and created there");
        Check(m61.Contains("Hash.TASK_GO_TO_COORD_ANY_MEANS, ped, wreckAt.X") &&
              m61.Contains("Hash.SET_PED_MAX_TIME_UNDERWATER, ped, DiverAirSeconds"),
            "and told to swim for the wreck with enough air to get there");
        Check(m61.Contains("\"M61.Diver1\", \"M61.Diver2\", \"M61.Diver3\", \"M61.Diver4\"))"),
            "A dry diver key refuses the mission the way a dry wreck key does");

        // ---- M51's six charge points are reviewed by name; the old loop iterated an
        // empty FixedSurfaces and reviewed nothing.
        Check(m51.Contains("Paleto.Review(Ctx, PlacementContract.Interaction(\"M51.Charge\" + i))"),
            "M51 reviews its six charge points rather than an empty list");

        // ---- A brother created where the player is not keeps the world built under him.
        string roster = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Crew", "CrewRoster.cs"));
        string composed = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "ComposedMission.cs"));
        string far = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "FarPlacement.cs"));
        Check(far.Contains("Hash.SET_ENTITY_LOAD_COLLISION_FLAG, ped, true") && !far.Contains("IsPositionFrozen"),
            "FarPlacement asks the engine to keep collision under him and never freezes him");
        Check(roster.Contains("> Core.FarPlacement.Meters)") && roster.Contains("Core.FarPlacement.Keep(ped,"),
            "The roster applies it to a brother deployed far from the player");
        Check(composed.Contains("Core.FarPlacement.Keep(ped, \"stationed"),
            "and Station applies it to a brother moved far from the player");

        // Both harnesses stand in for the roster, so the deployment itself cannot be driven
        // here. The helper can: it asks the engine for collision around the man it is given
        // and nothing else, and a man who is not there gets no request at all.
        GTA.Native.Function.Calls.Clear();
        var farMan = new GTA.Ped { Position = new Vector3(400f, 325f, 335f) };
        FarPlacement.Keep(farMan, "test");
        var flags = GTA.Native.Function.Calls.Where(call => call.Item1 == GTA.Native.Hash.SET_ENTITY_LOAD_COLLISION_FLAG).ToList();
        Check(flags.Count == 1 && ((GTA.Ped)flags[0].Item2[0]).Handle == farMan.Handle && (bool)flags[0].Item2[1],
            "FarPlacement asks the engine to keep collision loaded around exactly the man it is given");
        Check(GTA.Native.Function.Calls.Any(call => call.Item1 == GTA.Native.Hash.REQUEST_COLLISION_AT_COORD),
            "and requests the collision at his position now, not only on the flag");
        GTA.Native.Function.Calls.Clear();
        FarPlacement.Keep(null, "nobody");
        Check(GTA.Native.Function.Calls.Count == 0, "A man who is not there gets no request");
    }
}
