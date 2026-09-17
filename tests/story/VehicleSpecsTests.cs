using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using GTA;
using GTA.Native;

public static partial class StoryTests
{
    static void VehicleSpecsChecks()
    {
        string specs = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "VehicleSpecs.cs"));
        string hub = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "CampaignHub.cs"));
        string floor = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "DevMenu.Garages.cs"));

        // ---- There are two places to buy a car and both have to show the ratings. The first
        // pass wired only the phone, so walking into the dealership looked exactly as it had
        // before and the work read as never done. This is the check that refuses that.
        Check(hub.Contains("VehicleSpecs.Block("),
            "The phone's vehicle pages ask for the performance block");

        // ---- The page where a car is CHOSEN is a list, and a list row draws its title and its
        // subtitle and nothing else. An entry with children can never show a body, because
        // selecting it pushes into the folder and returns - so the first pass put the ratings
        // somewhere the phone does not draw, and buying a car looked untouched.
        string phone = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "CampaignPhone.cs"));
        Check(hub.Contains("LiveSubtitle = () => \"$\" + VehiclePricing.Of(choice).ToString(\"N0\") + \" - \" +"),
            "A car's list row carries its price and its comparison number in the subtitle");
        Check(phone.Contains("_entries[i].SubtitleText"),
            "and the renderer reads the live subtitle, or none of it reaches the screen");
        Check(hub.Contains("Id = \"specs:\" + choice.Model") && hub.Contains("LiveBody = () => Showroom(choice)"),
            "Showroom has a row of its own, so the full block is reachable rather than dead code");
        int showroomAt = hub.IndexOf("LiveBody = () => Showroom(choice)", StringComparison.Ordinal);
        int childrenAt = hub.IndexOf("Children = () => VehicleDestinations(choice)", StringComparison.Ordinal);
        Check(showroomAt > childrenAt,
            "and it is not hung off the car entry, which is the place that cannot draw a body");
        // The floor lays its rows out from Labels and asks Value for each one every frame. It
        // used to add Rows at build time, which on a page built while the model was still
        // streaming was no rows at all, with nothing left to fill in when the ratings came.
        Check(floor.Contains("VehicleSpecs.Labels") && floor.Contains("VehicleSpecs.Value(") && floor.Contains("VehicleSpecs.Summary("),
            "and the Premium Deluxe floor in the world menu asks for the same ratings, live, row by row");
        Check(!floor.Contains("foreach (var row in VehicleSpecs.Rows("),
            "rather than copying whatever rows existed the frame the page was built");

        // Every page that can take money for a car names the specs. If a third buying surface
        // is ever added, this fails until it does too.
        foreach (var page in new[] { "BuildDealerCategory", "BuildDealerDestination" })
        {
            // The declaration, not the first mention: the first mention is the call site in
            // the page above it, and slicing from there measures the wrong few lines.
            int at = floor.IndexOf("private Page " + page, StringComparison.Ordinal);
            Check(at > 0, page + " is still the dealer page it was");
            int end = floor.IndexOf("private Page", at + page.Length + 13, StringComparison.Ordinal);
            string body = end > at ? floor.Substring(at, end - at) : floor.Substring(at);
            Check(body.Contains("VehicleSpecs."), page + " shows the ratings rather than a bare price");
        }

        // ---- One source of formatting. Block composes Rows, so the phone and the floor cannot
        // word or round the same rating differently.
        Check(specs.Contains("public static IEnumerable<KeyValuePair<string, string>> Rows(Model model)"),
            "Rows is the one place the wording and rounding live");
        Check(specs.Contains("var rows = Rows(model).ToList();"),
            "and Block composes Rows rather than formatting its own");
        Check(!specs.Contains("private static string Row(string label, float value)"),
            "The second formatter is gone, so there is nothing left to drift");

        // ---- A model still streaming in yields nothing, and every caller copes. A row of
        // zeroes would tell the player a car has no engine.
        Check(specs.Contains("if (ratings == null) yield break;"),
            "Rows yields nothing while the model is still loading");
        Check(specs.Contains("return \"reading ratings\";"),
            "Summary says so rather than reporting a car with no engine");
        Check(specs.Contains("return ratings.TopSpeed <= 0f && ratings.Acceleration <= 0f ? null : ratings;"),
            "and an all-zero answer is never cached");

        // ---- The scale is shared with the shop panel, which is the point of the constant.
        Check(Math.Abs(VehicleSpecs.SpeedCeilingKph - 400f) < 0.01f && VehicleSpecs.BarCells == 10,
            "The normalization and the bar width are the shop panel's, so both agree");

        // ---- The bar is drawable in the game's own text renderer: ASCII only, fixed width.
        string full = VehicleSpecs.Bar(1f), empty = VehicleSpecs.Bar(0f), half = VehicleSpecs.Bar(.5f);
        Check(full.Length == empty.Length && half.Length == full.Length,
            "Every bar is the same width, so a column of them lines up");
        Check(full == "[==========]" && empty == "[..........]",
            "and a full bar reads full and an empty one reads empty");
        Check(full.All(c => c < 128) && half.All(c => c < 128),
            "The bar is ASCII, because GTA's text renderer is not dependable outside it");
        Check(VehicleSpecs.Bar(2f) == full && VehicleSpecs.Bar(-1f) == empty,
            "A rating outside nought to one is clamped rather than drawn off the end of the bar");

        // ---- Ron's report: buying a car on the phone showed no stats at all. The cause was in
        // Of: it requested the model and released it in the same call, every frame. A request
        // is not a load - the engine streams over the frames that follow - and the release
        // canceled it before it arrived, so nothing was ever readable. The stand-in streamed
        // synchronously, which is why the earlier checks passed. It streams like the engine
        // here, and the ratings have to arrive the frame after the request.
        VehicleSpecs.Forget();
        Model.StreamsNextFrame = true; Model.Pending.Clear(); Model.Landed.Clear();
        try
        {
            var slow = new Model("streamed-late") { IsCar = true };
            // The stand-in answers by hash only once the model has landed, as a model whose
            // handling the engine has not read would.
            Function.RatingsNeedModel = true;
            Check(VehicleSpecs.Of(slow) == null, "Nothing is readable the frame the model is asked for");
            Check(Model.Pending.Contains("streamed-late"), "and the request is left open rather than released in the same call");
            Check(VehicleSpecs.Summary(slow) == "reading ratings", "The row says it is reading, not that the car has no engine");
            Check(Model.Pending.Contains("streamed-late"), "Asking again does not cancel the request either");
            Model.Stream();
            Check(VehicleSpecs.Of(slow) != null, "The frame after it lands, the ratings are read");
            Check(!Model.Pending.Contains("streamed-late") && VehicleSpecs.Rows(slow).Any(),
                "and the model is handed back to the engine once they are, with the rows filled");
            Check(VehicleSpecs.Value(slow, "Top speed") != null && VehicleSpecs.Value(slow, "no such row") == null,
                "A named row answers, and a row that does not apply answers null rather than a blank");
        }
        finally { Model.StreamsNextFrame = false; Function.RatingsNeedModel = false; Model.Pending.Clear(); Model.Landed.Clear(); VehicleSpecs.Forget(); }
    }
}
