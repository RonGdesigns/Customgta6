using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;

public static partial class StoryTests
{
    /// <summary>
    /// The pre-flight report: what a mission is standing on, and how much of it nobody
    /// has ever checked.
    ///
    /// The status column has existed for as long as the location book has, and until now
    /// only the offline linter read it. Every placement fault of the last week — guards
    /// in a hull, a cordon eighteen meters from the truck, a marker on a deck nobody had
    /// walked — was a key whose status said "estimate" and which the game never mentioned.
    /// </summary>
    static void PreflightChecks()
    {
        var book = LocationBook.Load(dataDir, Path.Combine(root, "preflight-overrides.ini"));

        // ---- It reports the keys a mission asked for, not the ones that share its name.
        // A prefix scan would miss every shared point, which is exactly the kind of place
        // two missions disagree about.
        book.BeginUse();
        Check(!book.Used.Any(), "Nothing has been looked up yet, so nothing is reported");
        Check(PlacementPreflight.Summary(book, "M45") == null, "and a mission that stands on nothing has nothing to say");

        book.Position("M45.Helipad");
        book.Position("M45.Deck");
        book.Heading("M45.Board");
        Check(book.Used.Count() == 3, "Every lookup is noted, whichever accessor made it");
        book.Position("M45.Helipad");
        Check(book.Used.Count() == 3, "and asking twice is still one key");

        string summary = PlacementPreflight.Summary(book, "M45");
        Check(summary != null, "Unsurveyed keys are reported");
        Check(summary.Contains("M45.Helipad") && summary.Contains("M45.Deck") && summary.Contains("M45.Board"),
            "by name, so there is something to go and survey");
        Check(summary.Contains("3 of them"), "with a count of how much of the mission is guesswork");
        Check(summary.StartsWith("M45 "), "and the mission named up front");

        // ---- A fresh attempt is a fresh list. A report that accumulated across missions
        // would name points the mission being started does not use.
        book.BeginUse();
        book.Position("M45.Helipad");
        Check(book.Used.Count() == 1, "Beginning a mission clears what the last one was standing on");

        // ---- A surveyed key drops off the list. This is the whole point: the report
        // shrinks as the survey work gets done.
        book.BeginUse();
        book.Position("M45.Helipad");
        Check(PlacementPreflight.Summary(book, "M45") != null, "An estimate is reported");
        book.Record("M45.Helipad", new GTA.Math.Vector3(-1793f, 5331f, 15.5f), 90f);
        book.BeginUse();
        book.Position("M45.Helipad");
        Check(PlacementPreflight.Summary(book, "M45") == null,
            "and once it has been surveyed it is not mentioned again");
        Check(book.Used.Count() == 1 && !book.Unverified.Any(),
            "The key is still in use; it is just no longer a guess");

        // ---- The to-do list the placement menu shows.
        PlacementPreflight.Progress(book, "M53", out int surveyed, out int total);
        Check(total > 0, "A mission's own keys can be counted for a to-do list");
        Check(surveyed == 0 && PlacementPreflight.ProgressLabel(book, "M53").Contains("none surveyed"),
            "M53's are all still estimates, and the menu says so rather than just counting them");
        Check(PlacementPreflight.ProgressLabel(book, "NoSuchMission") == "no locations",
            "A prefix nothing answers to reads as no locations rather than as finished work");

        // ---- Wiring: recorded from the frame the mission starts, reported once it has,
        // and a warning rather than a refusal.
        string manager = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "MissionManager.cs"));
        Check(manager.Contains("_context.Locations?.BeginUse();"),
            "The manager starts recording before the mission resolves a single key");
        Check(manager.Contains("PlacementPreflight.Run(_context.Locations, _context.Doctor, definition.Id);"),
            "and reports once the mission has started");
        int beginAt = manager.IndexOf("_context.Locations?.BeginUse();", StringComparison.Ordinal);
        int reportAt = manager.IndexOf("PlacementPreflight.Run(", StringComparison.Ordinal);
        Check(beginAt > 0 && reportAt > beginAt, "in that order, or the report would be empty");
        string preflight = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "PlacementPreflight.cs"));
        Check(!preflight.Contains("return false") && !preflight.Contains("throw "),
            "Nothing here can stop a mission: an estimate is usually close enough to play");
        string placementMenu = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "DevMenu.Placement.cs"));
        Check(placementMenu.Contains("PlacementPreflight.ProgressLabel(_survey.Book,mission)"),
            "and the placement editor's mission list is the survey to-do list");
    }
}
