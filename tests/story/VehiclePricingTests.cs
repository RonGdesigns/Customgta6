using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloodlines.Core;

public static partial class StoryTests
{
    /// <summary>
    /// What the catalog costs, against what the campaign pays.
    ///
    /// The expansion to 202 vehicles left 137 of them falling through to a category
    /// default, which meant seventy cars all cost $40,000 — on a page the phone sorts by
    /// price, so seventy cars in no order at all. These checks are about the two things
    /// that made the shop worth having: every vehicle carries its own number, and the
    /// numbers sit on the ladder the campaign's own payouts define.
    /// </summary>
    static void VehiclePricingChecks()
    {
        var catalog = StoryVehicles.Catalog;
        var priced = catalog.ToDictionary(v => v.Model, VehiclePricing.Of, StringComparer.OrdinalIgnoreCase);

        // ---- Every one of them has a real price rather than its category's fallback.
        string garages = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "Garages.cs"));
        string table = garages.Split(new[] { "ModelPrices = new Dictionary<string, int>" }, StringSplitOptions.None)[1]
                              .Split(new[] { "};" }, StringSplitOptions.None)[0];
        var listed = new HashSet<string>(
            System.Text.RegularExpressions.Regex.Matches(table, "\\{\\s*\"([A-Za-z0-9_]+)\"")
                .Cast<System.Text.RegularExpressions.Match>().Select(m => m.Groups[1].Value),
            StringComparer.OrdinalIgnoreCase);
        var unlisted = catalog.Where(v => !listed.Contains(v.Model)).ToList();
        Check(unlisted.Count == 0,
            "Every vehicle in the catalog has its own price" +
            (unlisted.Count == 0 ? "" : " — missing: " + string.Join(", ", unlisted.Take(6).Select(v => v.Model))));

        // ---- The ladder. The campaign pays $55,000 across its first three missions, so
        // something has to be buyable then; it pays $3,722,000 in total, so nothing should
        // cost a seventh of everything the player will ever earn.
        int cheapest = priced.Values.Min(), dearest = priced.Values.Max();
        Check(cheapest <= 20000, "There is a car an Act I crew can afford on their first three payouts");
        Check(dearest <= 500000, "and nothing costs more than half a million, which is a seventh of the campaign");
        Check(dearest >= 300000, "while the top of the catalog is still a trophy rather than a purchase");

        // ---- Spread, which is the whole point. A page sorted by price needs prices.
        foreach (var page in catalog.GroupBy(v => v.Category))
        {
            var prices = page.Select(v => priced[v.Model]).ToList();
            int distinct = prices.Distinct().Count();
            Check(distinct >= (int)(prices.Count * .8),
                page.Key + " has " + distinct + " distinct prices across " + prices.Count +
                " vehicles, so its list has an order");
        }

        // ---- A sale is capped well under what the expensive end costs, which is what
        // makes the top of the catalog a commitment rather than a rental.
        Check(VehiclePricing.StreetValue < cheapest * 2 && VehiclePricing.StreetValue <= 30000,
            "Street value stays below the catalog, so buying high is a one-way door");

        // ---- And the generator behind it is honest about what it will not do.
        string tool = File.ReadAllText(Path.Combine(Repo, "tools", "price_vehicles.py"));
        Check(tool.Contains("never moves") || tool.Contains("are anchors"),
            "The pricing tool leaves a hand-set price alone");
        Check(tool.Contains("40 to 45 per cent out"),
            "and records why a runtime performance formula was measured and rejected");
    }
}
