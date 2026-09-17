using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// What a car is, read off the model rather than off a spawned example.
    ///
    /// The shop panel already shows performance bars, but it reads the four
    /// <c>GET_VEHICLE_*</c> natives, which need a vehicle in the world. Buying a car is
    /// the opposite situation: he is choosing from a list of cars he has never seen, on a
    /// page with a name and a price and nothing else, which is no way to spend forty
    /// thousand dollars. The model-level natives answer the same questions without putting
    /// a car on the street.
    ///
    /// **There are two places to buy a car and both of them use this.** The phone's
    /// Vehicles app and the Premium Deluxe floor in the world menu. The first pass wired
    /// only the phone, so Ron walked into the dealership and saw exactly what he saw
    /// before — which is why <see cref="Rows"/> exists: one source of formatting, so a
    /// surface cannot be updated and the other quietly left behind. A story test now
    /// refuses a car-buying page that does not ask for these.
    ///
    /// The ratings are the game's own, on the game's own scale. Nothing here is a
    /// measured road test, and the top speed is the engine's estimate — it says
    /// "estimated" in the native's name, and it is reported that way.
    /// </summary>
    public static class VehicleSpecs
    {
        /// <summary>The normalization the shop panel uses, so both places agree.</summary>
        public const float SpeedCeilingKph = 400f;
        /// <summary>How many cells a text bar is drawn with.</summary>
        public const int BarCells = 10;

        public sealed class Ratings
        {
            /// <summary>Estimated top speed, in meters per second, as the model reports it.</summary>
            public float TopSpeed;
            public float Acceleration, Braking, Traction;
            /// <summary>The same two the game lets a full build raise. Zero where it reports none.</summary>
            public float BuiltAcceleration, BuiltBraking;
            public int Seats;

            public float SpeedMph => TopSpeed * 2.236936f;
            public float SpeedFraction => TopSpeed * 3.6f / SpeedCeilingKph;
            /// <summary>True where a full build actually raises something worth saying.</summary>
            public bool Upgradable => BuiltAcceleration > Acceleration + .001f || BuiltBraking > Braking + .001f;
        }

        private static readonly Dictionary<int, Ratings> Known = new Dictionary<int, Ratings>();
        /// <summary>Models this class has asked the engine for and not yet handed back.</summary>
        private static readonly HashSet<int> Requested = new HashSet<int>();

        /// <summary>
        /// The model's ratings, or null while they are not readable yet. A caller that gets
        /// null says so and asks again next frame.
        ///
        /// **The first version requested the model and released it in the same call, every
        /// frame.** <c>Request()</c> is a request, not a load: the engine streams the model
        /// in over the frames that follow, and <c>MarkAsNoLongerNeeded()</c> on the way out
        /// of the same call canceled it before it ever arrived. So the ratings were never
        /// readable, the phone's every car row said "reading ratings" for as long as Ron
        /// looked at it, and the harness passed because its stand-in streams synchronously.
        /// The stand-in streams like the engine now, and this keeps the request open until
        /// the ratings have actually been read.
        ///
        /// The natives are asked by hash first. They read handling data, which does not
        /// need the model streamed, so most cars answer without a request at all.
        /// </summary>
        public static Ratings Of(Model model)
        {
            if (!model.IsValid || !model.IsVehicle) return null;
            if (Known.TryGetValue(model.Hash, out var cached)) return cached;
            try
            {
                var ratings = Read(model);
                if (ratings == null)
                {
                    // Nothing by hash. Ask for the model, once, and keep the request open;
                    // whichever later frame finds it loaded reads it and hands it back.
                    if (!model.IsLoaded)
                    {
                        if (!Requested.Contains(model.Hash)) { model.Request(); Requested.Add(model.Hash); }
                        return null;
                    }
                    ratings = Read(model);
                    if (ratings == null) return null;
                }
                if (Known.Count >= 512) Known.Clear();
                Known[model.Hash] = ratings;
                if (Requested.Remove(model.Hash)) model.MarkAsNoLongerNeeded();
                return ratings;
            }
            catch (Exception ex)
            {
                Logger.Warn("Model ratings unavailable for " + model.Hash + ": " + ex.Message);
                return null;
            }
        }

        /// <summary>The natives, or null when they answer zero to everything: that is a model
        /// that has not really answered, and caching it lists a car as having no engine.</summary>
        private static Ratings Read(Model model)
        {
            var ratings = new Ratings
            {
                TopSpeed = Function.Call<float>(Hash.GET_VEHICLE_MODEL_ESTIMATED_MAX_SPEED, model.Hash),
                Acceleration = Function.Call<float>(Hash.GET_VEHICLE_MODEL_ACCELERATION, model.Hash),
                Braking = Function.Call<float>(Hash.GET_VEHICLE_MODEL_MAX_BRAKING, model.Hash),
                Traction = Function.Call<float>(Hash.GET_VEHICLE_MODEL_MAX_TRACTION, model.Hash) / 3f,
                BuiltAcceleration = Function.Call<float>(Hash.GET_VEHICLE_MODEL_ACCELERATION_MAX_MODS, model.Hash),
                BuiltBraking = Function.Call<float>(Hash.GET_VEHICLE_MODEL_MAX_BRAKING_MAX_MODS, model.Hash),
                Seats = Function.Call<int>(Hash.GET_VEHICLE_MODEL_NUMBER_OF_SEATS, model.Hash)
            };
            return ratings.TopSpeed <= 0f && ratings.Acceleration <= 0f ? null : ratings;
        }

        /// <summary>Forget everything read, for a harness that changes what the engine answers.</summary>
        public static void Forget() { Known.Clear(); Requested.Clear(); }

        /// <summary>
        /// The ratings as label/value pairs, which is the one place their wording and their
        /// rounding live. <see cref="Block"/> joins these for a phone page and the world
        /// menu adds them as its own rows; neither formats anything itself, so the two
        /// cannot drift apart the way they already did once.
        ///
        /// Empty while the model is still streaming in. A caller that gets nothing says so.
        /// </summary>
        public static IEnumerable<KeyValuePair<string, string>> Rows(Model model)
        {
            var ratings = Of(model);
            if (ratings == null) yield break;
            yield return Pair("Top speed", Bar(ratings.SpeedFraction) + " " + Percent(ratings.SpeedFraction));
            yield return Pair("Acceleration", Bar(ratings.Acceleration) + " " + Percent(ratings.Acceleration));
            yield return Pair("Braking", Bar(ratings.Braking) + " " + Percent(ratings.Braking));
            yield return Pair("Traction", Bar(ratings.Traction) + " " + Percent(ratings.Traction));
            yield return Pair("Est. top speed", ((int)Math.Round(ratings.SpeedMph)) + " mph");
            if (ratings.Seats > 0) yield return Pair("Seats", ratings.Seats.ToString());
            if (ratings.Upgradable)
                yield return Pair("Fully built", "accel " + Percent(ratings.BuiltAcceleration) +
                    " / braking " + Percent(ratings.BuiltBraking));
        }

        /// <summary>The four ratings as fractions, for a page that draws them as bars. Empty while the model is not readable.</summary>
        public static IList<KeyValuePair<string, float>> Fractions(Model model)
        {
            var bars = new List<KeyValuePair<string, float>>();
            var ratings = Of(model);
            if (ratings == null) return bars;
            bars.Add(new KeyValuePair<string, float>("Top speed", ratings.SpeedFraction));
            bars.Add(new KeyValuePair<string, float>("Acceleration", ratings.Acceleration));
            bars.Add(new KeyValuePair<string, float>("Braking", ratings.Braking));
            bars.Add(new KeyValuePair<string, float>("Traction", ratings.Traction));
            return bars;
        }

        /// <summary>The rows a bar cannot carry - the speed in mph, the seats, the built figures - for the text under the bars.</summary>
        public static string Facts(Model model)
        {
            var rows = Rows(model).Where(row => !row.Value.StartsWith("[")).ToList();
            if (rows.Count == 0) return "PERFORMANCE\nReading the model's ratings. Give it a moment.";
            var text = new StringBuilder("PERFORMANCE\n");
            foreach (var row in rows) text.Append(row.Key.PadRight(14)).Append(' ').Append(row.Value).Append('\n');
            text.Append("Game ratings for the stock model, not a road test.");
            return text.ToString();
        }

        /// <summary>The silhouette for a catalog category: "car-cars", "car-off-road". One per class, not per model.</summary>
        public static string ArtFor(string category) =>
            "car-" + (category ?? "cars").Trim().ToLowerInvariant().Replace(' ', '-');

        /// <summary>The row labels in the order Rows yields them, for a page that lays its rows
        /// out before the model has answered and fills the values in as they come.</summary>
        public static readonly string[] Labels = { "Top speed", "Acceleration", "Braking", "Traction", "Est. top speed", "Seats", "Fully built" };

        /// <summary>One row's value now, or null while the model is not readable or the row does not apply.</summary>
        public static string Value(Model model, string label)
        {
            foreach (var row in Rows(model)) if (row.Key == label) return row.Value;
            return null;
        }

        /// <summary>
        /// One line for a list row, where there is no room for bars: the number a buyer
        /// actually compares cars by, and a note while the model is still loading.
        /// </summary>
        public static string Summary(Model model)
        {
            var ratings = Of(model);
            if (ratings == null) return "reading ratings";
            return ((int)Math.Round(ratings.SpeedMph)) + " mph - accel " + Percent(ratings.Acceleration);
        }

        private static KeyValuePair<string, string> Pair(string label, string value) =>
            new KeyValuePair<string, string>(label, value);

        private static string Percent(float value) =>
            ((int)Math.Round(Math.Max(0f, Math.Min(1f, value)) * 100f)) + "%";

        /// <summary>A rating drawn as a bar, in characters the game's text renderer has.</summary>
        public static string Bar(float value)
        {
            int filled = (int)Math.Round(Math.Max(0f, Math.Min(1f, value)) * BarCells);
            return "[" + new string('=', filled) + new string('.', BarCells - filled) + "]";
        }

        /// <summary>
        /// The performance block for a phone page, or a line saying it is not ready yet.
        /// Never returns an empty string: a blank space where the numbers should be reads
        /// as a car with no numbers rather than a page still loading.
        /// </summary>
        public static string Block(Model model)
        {
            var rows = Rows(model).ToList();
            if (rows.Count == 0) return "PERFORMANCE\nReading the model's ratings. Give it a moment.";

            var text = new StringBuilder("PERFORMANCE\n");
            foreach (var row in rows) text.Append(row.Key.PadRight(14)).Append(' ').Append(row.Value).Append('\n');
            text.Append("Game ratings for the stock model, not a road test.");
            return text.ToString();
        }
    }
}
