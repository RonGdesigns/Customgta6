using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using GTA;

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
        Check(floor.Contains("VehicleSpecs.Rows(") && floor.Contains("VehicleSpecs.Summary("),
            "and the Premium Deluxe floor in the world menu asks for the same ratings");

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
        Check(specs.Contains("if (ratings.TopSpeed <= 0f && ratings.Acceleration <= 0f) return null;"),
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
    }
}
