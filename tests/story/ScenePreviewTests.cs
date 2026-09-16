using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using GTA;
using GTA.Math;

public static partial class StoryTests
{
    /// <summary>
    /// Staging a mission's world without running it, and the two placing tools that go
    /// with it. The thing that must be true before anything else: a preview leaves
    /// nothing behind. A Cargobob still standing when the real attempt spawns its own is
    /// the exact conflict this project keeps hitting.
    /// </summary>
    static void ScenePreviewChecks()
    {
        // ---- Staging is the mission's own Setup, and nothing after it.
        Reset();
        var crew = Roster();
        var context = Context(crew);
        Mission staged = null;
        var definition = new MissionDefinition
        {
            Info = new MissionInfo { Id = "M06", Number = 6, Kind = "main", Title = "Clean Sweep" },
            Factory = () => staged = new M06CleanSweep()
        };

        var studio = new ScenePreview();
        var elsewhere = crew.PedFor(CrewSlot.Guess).Position;
        Check(studio.Open(definition, context, context.Locations, null),
            "A mission stages its world on request");
        Check(studio.IsActive && studio.MissionId == "M06", "and the preview says what is standing");
        Check(staged != null && staged.Status != MissionStatus.Running,
            "It is not running: Tick refuses it, so no objective can advance");
        var spawned = staged.Staged.Where(e => e != null && e.Exists()).ToList();
        Check(spawned.Count > 0, "Its Setup put things in the world");
        Check(studio.Report.Any(line => line.Contains("Staging preview: M06")),
            "The report names what was staged");
        Check(studio.Report.Count(l => l.Contains("(") && l.Contains(")")) >= spawned.Count,
            "with a line for every entity, including the ones no key accounts for");

        // ---- And it all goes away. No parallel pool, no second owner: the path every
        // attempt already uses, plus taking the crew out of anything it would spare.
        studio.Close();
        var survivors = spawned.Where(e => e.Exists() && e.Handle != Game.Player.Character.Handle).ToList();
        Check(survivors.Count == 0, "Closing the preview deletes every entity the staging created" +
            (survivors.Count == 0 ? "" : " — left: " + string.Join(", ", survivors.Select(e =>
                (e is Ped ? "ped" : e is Vehicle ? "vehicle" : "prop") + "#" + e.Handle))));
        Check(!studio.IsActive, "and the preview is closed");
        Check(crew.PedFor(CrewSlot.Guess).Position.DistanceTo(elsewhere) < 2f,
            "A brother the staging deployed is put back where he was standing");

        // ---- A mission that cannot be staged is reported rather than half-opened. A
        // mission with no script, and one whose Setup throws, are both this path.
        var refused = new ScenePreview();
        Check(!refused.Open(new MissionDefinition { Info = definition.Info, Factory = () => null },
                  context, context.Locations, null) && refused.Refusal != null,
            "A mission with no script leaves no preview open and says why");
        Check(!refused.IsActive, "and nothing is left standing from the attempt");
        Check(!refused.Open(new MissionDefinition { Info = definition.Info, Factory = () => throw new InvalidOperationException("no") },
                  context, context.Locations, null) && refused.Refusal != null,
            "and a script that throws on construction is caught rather than taking the menu down");

        // ---- A composed mission stages through Setup rather than through OnStart, so the
        // preview shows what an attempt would build and not something built for looking at.
        string composed = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "ComposedMission.cs"));
        Check(composed.Contains("protected override bool OnStage() => Setup();"),
            "Staging is exactly the Setup an attempt runs");
        string baseMission = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Mission.cs"));
        Check(baseMission.Contains("public bool StageForPreview(MissionContext context)") &&
              !baseMission.Contains("Status = MissionStatus.Running;\r\n            return OnStage"),
            "and staging never marks the mission running");
        string preview = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "ScenePreview.cs"));
        Check(preview.Contains("mission.Cleanup();") && !preview.Contains("new List<Entity>()"),
            "The preview owns no entity pool of its own; teardown is the mission's");
        Check(preview.Contains("RestoreCrew();"),
            "and the brothers are put back where they were standing");
        string menu = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "DevMenu.cs"));
        Check(menu.Contains("Preview.Close();"), "Closing the dev menu closes the staged world with it");

        // ---- Routes are ordered keys, and nothing else. No second file to keep in step.
        Check(RouteRibbons.IsLeg("M09.Convoy.02", out string route, out int order) && route == "M09.Convoy" && order == 2,
            "A numbered key is one leg of a named route");
        Check(!RouteRibbons.IsLeg("M45.Helipad", out _, out _), "An ordinary key is not");
        Check(!RouteRibbons.IsLeg("M09.Convoy", out _, out _), "and neither is the route's own name");
        Check(!RouteRibbons.IsLeg("M09", out _, out _), "nor a mission id on its own");
        Check(RouteRibbons.IsLeg("M09.Convoy.10", out _, out int tenth) && tenth == 10,
            "Two digits order correctly rather than alphabetically");

        var book = LocationBook.Load(dataDir, Path.Combine(root, "none.ini"));
        Check(!RouteRibbons.Routes(book).Any(),
            "Nothing in the book is a route yet, so nothing is drawn until one is authored");

        // ---- The stand-in that rides the camera, instead of a gizmo nothing can grab.
        // The ghost itself is not compiled into this suite: it exists to be looked at
        // through a camera, and a stand-in reporting that a translucent helicopter moved
        // would prove nothing about whether it reads. Its contracts are checked instead.
        string ghost = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "PlacementGhost.cs"));
        Check(ghost.Contains("case \"air\": return Shape.Helicopter;") &&
              ghost.Contains("case \"water\": case \"channel\": return Shape.Boat;") &&
              ghost.Contains("default: return Shape.Person;"),
            "A key's kind picks the shape that is most likely to belong there, and anything else is a man");
        Check(ghost.Contains("SET_ENTITY_ALPHA") && ghost.Contains("_entity.IsCollisionEnabled = false;"),
            "The stand-in is see-through and cannot be walked into");
        Check(ghost.Contains("!string.Equals(kind, \"land\", StringComparison.OrdinalIgnoreCase)) return at;"),
            "Committing it follows the same rule as a capture: only a land key is dropped");
        Check(ghost.Contains("shows a **shape**"),
            "and it says in writing that it is a shape, not a claim about which model a mission spawns");
    }
}
