using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bloodlines.Core;
using GTA.Math;

public static partial class StoryTests
{
    /// <summary>
    /// The two things Ron found in the placement editor: a teleport that does nothing while
    /// the camera is flying, and an enemy-count row that answered "not a group" on every key
    /// he tried, because two of one thousand and ninety-one keys were declared as groups and
    /// the declaration was an expression rather than a table.
    /// </summary>
    static void SpawnGroupChecks()
    {
        // ======================= the declaration is a table now =======================
        var declared = MissionPlacement.Declared.OrderBy(k => k, StringComparer.Ordinal).ToList();
        Check(declared.Count > 2, "More than the original two keys carry a detail");
        Check(declared.Contains("M03.DepotGate") && declared.Contains("M05.LightCrew"),
            "and the two that were already wired are still wired");
        Check(declared.Contains("M60.Wave1") && declared.Contains("M60.Wave2") && declared.Contains("M60.Wave3"),
            "M60's three waves can be sized, which is the mission Ron was editing");

        var book = LocationBook.Load(dataDir, Path.Combine(root, "none.ini"), Path.Combine(root, "none-survey.ini"));
        foreach (string key in declared)
            Check(book.Get(key) != null, "A declared group is a key the book actually holds: " + key);

        // ---- Every key a mission sizes is declared, and every declared key is sized by a
        // mission. This pair is the whole point of the table: the bug was a mission reading
        // a count for a key nothing had declared, and it was found in game rather than here.
        var read = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string missions = Path.Combine(Repo, "src", "Bloodlines", "Missions");
        foreach (string file in Directory.GetFiles(missions, "*.cs", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(file);
            if (text.IndexOf("MissionPlacement.Count(", StringComparison.Ordinal) < 0 &&
                text.IndexOf("MissionPlacement.PointFor(", StringComparison.Ordinal) < 0) continue;
            foreach (Match match in Regex.Matches(text,
                "MissionPlacement\\.(?:Count|PointFor)\\(\\s*Ctx\\.Locations\\s*,\\s*\"([^\"]+)\""))
                read.Add(match.Groups[1].Value);
            // A wave or a nest numbers its key at runtime, so the literal in the source is
            // the prefix. Those only count from a file that sizes something, which is what
            // keeps this from accepting any concatenation anywhere in the campaign.
            foreach (Match match in Regex.Matches(text, "\"(M[0-9]+\\.[A-Za-z]+)\" \\+"))
                read.Add(match.Groups[1].Value);
        }
        foreach (string key in declared)
            Check(read.Any(r => key.StartsWith(r, StringComparison.OrdinalIgnoreCase)),
                "A declared group is one some mission actually reads: " + key);

        // ---- A declared key changes nothing until it is edited. This is what makes adding
        // to the table safe: an unedited key has no count, so the mission keeps the constant
        // it was authored with.
        var wave = book.Get("M60.Wave1");
        Check(wave.SpawnCount <= 0 && !MissionPlacement.HasFormation(wave),
            "An unedited group carries no formation");
        Check(MissionPlacement.Count(book, "M60.Wave1", 5) == 5,
            "so the mission falls back to the size it was written with");
        var authored = new Vector3(3f, 4f, 20f);
        Check(MissionPlacement.PointFor(book, "M60.Wave1", 2, authored) == authored,
            "and every man stands exactly where the mission put him");

        wave.SpawnCount = 9; wave.SpawnRadius = 10f;
        Check(MissionPlacement.Count(book, "M60.Wave1", 5) == 9, "Edited, the key is what the mission reads");
        Check(MissionPlacement.PointFor(book, "M60.Wave1", 2, authored) != authored,
            "and the formation is laid out from the key rather than from the mission's offsets");

        // ---- Count without spread, for a detail the mission arranges itself.
        Check(MissionPlacement.HasGroup("M45.Deck") && !MissionPlacement.HasRadius("M45.Deck"),
            "The Paleto deck detail is sized but not arranged by the editor");
        var deck = book.Get("M45.Deck");
        deck.SpawnCount = 6; deck.SpawnRadius = 30f;
        Check(MissionPlacement.Count(book, "M45.Deck", 10) == 6, "Its count is read");
        Check(MissionPlacement.PointFor(book, "M45.Deck", 3, authored) == authored,
            "and its men stay in the ranks the mission probes for a deckhead, not in a circle");
        deck.SpawnCount = 0; deck.SpawnRadius = 0f;
        Check(MissionPlacement.HasRadius("M60.Wave1"), "A wave's spread is the editor's");

        // ---- The spiral, which is what a count means on the ground.
        var center = new Vector3(0f, 0f, 20f);
        var spread = Enumerable.Range(0, 8).Select(i => MissionPlacement.GroupPoint(center, 0f, i, 8, 12f)).ToList();
        Check(spread.All(p => p.DistanceTo2D(center) <= 12.01f), "Nobody stands outside the radius");
        Check(spread.Select(p => (int)(Math.Atan2(p.Y, p.X) * 4)).Distinct().Count() >= 7,
            "and a detail fills the circle instead of queueing along one diagonal");
        Check(MissionPlacement.GroupPoint(center, 0f, 0, 8, 12f).Z == center.Z,
            "A formation is flat: the height is the key's, not something the layout invents");

        // ================== the editor says what a key is, not what it is not ==================
        string page = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "DevMenu.Placement.cs"));
        // Only the strings the page draws, not the comment above them explaining why that
        // wording is gone. Three checks on this project have already passed or failed on
        // their own documentation.
        string drawn = string.Join(" ", Regex.Matches(page, "\"[^\"]*\"").Cast<Match>().Select(m => m.Value));
        Check(!drawn.Contains("not a group"),
            "The count row no longer answers 'not a group', which named a state and explained nothing");
        Check(page.Contains("one man at this point"),
            "It says what the key is instead");
        Check(page.Contains("MissionPlacement.HasRadius(_survey.Draft.Key)"),
            "and a detail the mission arranges is told apart from one the editor arranges");
        string editor = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "SurveyMode.Placement.cs"));
        Check(editor.Contains("if (spread) for (int i=0;i<Draft.SpawnCount;i++)"),
            "Only a key whose spread is ours previews a dot per man; drawing a circle the mission ignores is a promise it does not keep");
        Check(editor.Contains("if (MissionPlacement.HasRadius(Draft.Key)) Draft.SpawnRadius"),
            "and no radius is written into a key nothing will read it from");

        // ========================= the teleport takes the camera =========================
        string camera = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "SurveyCamera.cs"));
        Check(camera.Contains("public bool MoveTo(Vector3 target)") && camera.Contains("public void ReturnTo(Vector3 at)"),
            "The camera can be sent to a point and put back");
        string survey = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "SurveyMode.cs"));
        Check(survey.Contains("if (_cameraFrom.HasValue) Camera.MoveTo(p);"),
            "A teleport takes the flying camera with it, or from behind it nothing happened");
        Check(survey.Contains("if (rollback && cameraFrom.HasValue) Camera.ReturnTo(cameraFrom.Value);"),
            "and a rollback brings it back rather than leaving it outside its own leash");

    }
}
