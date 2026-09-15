using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bloodlines.Core;
using GTA;
using GTA.Native;

public static partial class StoryTests
{
    /// <summary>
    /// The customs card and the paint grid Ron asked for after finding a rental
    /// interface that showed a car properly: the make over the model, performance with
    /// numbers on it, and paint you can see. What follows checks that we took the
    /// information out of that reference and none of the rental machinery.
    /// </summary>
    static void CustomsCardChecks()
    {
        string card = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "DevMenu.ShopDetails.cs"));
        string menu = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "DevMenu.cs"));
        string shops = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "DevMenu.Shops.cs"));

        // ---- Where it sits. The old panel began at x 490, which in a 1280-wide layout is
        // the middle of the screen: the car being customized was behind it.
        Check(card.Contains("CardX = 878f"), "The card is on the right-hand edge of the screen");
        Check(!card.Contains("const float x=490f"), "and nothing still draws it over the middle");

        // ---- What it says. Make above model is how a car is named.
        Check(card.Contains("MakeOf(car.Model)") && card.Contains("car.LocalizedName"),
            "The manufacturer sits above the model name");
        Check(card.Contains("GET_MAKE_NAME_FROM_VEHICLE_MODEL"),
            "and the manufacturer is the game's own label, not a guess from the model name");
        Check(card.Contains("car.ClassLocalizedName") && card.Contains("car.PassengerCapacity"),
            "The class and the seat count are on the badge under it");
        Check(card.Contains("_state.CashOnHand"), "What he has to spend with is on every page of the shop");

        // ---- The bars carry their numbers, and the speed is stated in both units rather
        // than left as a rating nobody can convert.
        Check(card.Contains("RightText(((int)Math.Round(value*100f)).ToString()"),
            "Every performance bar is drawn with its own number beside it");
        Check(card.Contains("km/h") && card.Contains("mph estimated"),
            "and the top speed is given in km/h and mph, said to be an estimate");

        // ---- The reference was a rental. We took the card and left the rental behind.
        // Searched in the string literals rather than the whole source: the first
        // version of this read the comment above the card explaining what it
        // deliberately does not draw, and failed on our own prose.
        var drawn = Regex.Matches(card, "\"[^\"]*\"").Cast<Match>().Select(m => m.Value).ToList();
        foreach (string rental in new[] { "uration", "/ hr", "per hour", "otal due", "ental" })
            Check(!drawn.Any(text => text.Contains(rental)),
                "Nothing the card draws rents anything: no text on it contains " + rental);

        // ---- The paint grid, and the two ways it can decline.
        Check(shops.Contains("page.Columns=PaintPalette.Sample(Shops.Car(site))?SwatchColumns:0;"),
            "A paint page becomes a grid only when the game gave up its palette");
        Check(card.Contains("if(selected?.Swatch?.Invoke()==null)return false;"),
            "and a grid with no colors in it refuses, so the names are drawn instead");
        Check(menu.Contains("if (DrawSwatchGrid(page, x, width, ref y))"),
            "The menu draws the grid where it would have drawn the rows");
        Check(shops.Contains("if(selected==current)page.Select(page.Items.Count-1);"),
            "The grid opens on the paint the car is already wearing");
        Check(menu.Contains("page.Move(-page.Stride)") && menu.Contains("if (page.Columns > 0) page.Move(-1);"),
            "Up and down walk a row at a time; left and right walk along one");
        Check(card.Contains("if(!PaintPalette.Ready)return;"),
            "The finish chips are drawn only from colors that were actually read");

        // ---- The palette itself, driven rather than read.
        PaintPalette.Forget();
        Function.FlatPaint = false;

        Check(!PaintPalette.Sample(null), "No car is no palette");
        Check(!PaintPalette.Ready, "and a missing car does not count as the one attempt");

        var car = new Vehicle();
        car.Mods.PrimaryColor = VehicleColor.HotPink;
        car.Mods.SecondaryColor = VehicleColor.MetallicBlack;
        Check(PaintPalette.Sample(car), "A car in the shop gives up the whole palette");
        int paints = Enum.GetValues(typeof(VehicleColor)).Cast<int>().Distinct().Count();
        Check(PaintPalette.Count == paints, "which is every paint the game names, not a handful");
        Check(car.Mods.PrimaryColor == VehicleColor.HotPink && car.Mods.SecondaryColor == VehicleColor.MetallicBlack,
            "and the car is wearing exactly what it arrived in");
        Check(PaintPalette.Of((int)VehicleColor.HotPink).ToArgb() == Function.PaintFor((int)VehicleColor.HotPink).ToArgb(),
            "Each tile is the color the game reported for that paint");
        Check(PaintPalette.Of((int)VehicleColor.HotPink).ToArgb() != PaintPalette.Of((int)VehicleColor.MetallicBlack).ToArgb(),
            "so two different paints are two different tiles");

        int before = Function.Calls.Count;
        PaintPalette.Sample(car);
        Check(Function.Calls.Count == before, "The sweep runs once a session, never once a frame");

        // ---- A custom primary hides the paint index, so it is cleared to read the
        // palette. It has to come back, or he loses the color he mixed himself.
        PaintPalette.Forget();
        var mixed = new Vehicle();
        mixed.Mods.PrimaryColor = VehicleColor.Blue;
        mixed.Mods.CustomPrimaryColor = Color.FromArgb(12, 34, 56);
        Check(PaintPalette.Sample(mixed), "A car wearing a mixed color still gives up the palette");
        Check(mixed.Mods.IsPrimaryColorCustom &&
              mixed.Mods.CustomPrimaryColor.ToArgb() == Color.FromArgb(12, 34, 56).ToArgb(),
            "and gets its mixed color back afterward");
        Check(mixed.Mods.PrimaryColor == VehicleColor.Blue, "along with the paint index underneath it");

        // ---- A native that will not answer. Nobody has watched this run in game, so the
        // sweep is allowed to fail: what it must not do is paint a hundred and sixty
        // identical tiles and call them a palette.
        PaintPalette.Forget();
        Function.FlatPaint = true;
        var stubborn = new Vehicle { };
        stubborn.Mods.PrimaryColor = VehicleColor.Orange;
        Check(!PaintPalette.Sample(stubborn), "One color for every paint is refused, not believed");
        Check(!PaintPalette.Ready && PaintPalette.Count == 0, "Nothing is cached from a refused sweep");
        Check(PaintPalette.Of(7).ToArgb() == PaintPalette.Unknown.ToArgb(),
            "and an unsampled paint asks for a neutral tile rather than a wrong one");
        Check(stubborn.Mods.PrimaryColor == VehicleColor.Orange, "The car is still its own color");
        Function.FlatPaint = false;

        // ---- A native that throws halfway through. The car still comes out as it went in.
        PaintPalette.Forget();
        var interrupted = new Vehicle();
        interrupted.Mods.PrimaryColor = VehicleColor.MetallicRed;
        interrupted.Mods.SecondaryColor = VehicleColor.MetallicSilver;
        Function.ThrowOnce = Hash.GET_VEHICLE_COLOR;
        Check(!PaintPalette.Sample(interrupted), "A native that throws mid-sweep produces no palette");
        Check(interrupted.Mods.PrimaryColor == VehicleColor.MetallicRed &&
              interrupted.Mods.SecondaryColor == VehicleColor.MetallicSilver,
            "and the car is put back anyway, because the restore is in a finally");
        Function.ThrowOnce = null;
        PaintPalette.Forget();
    }
}
