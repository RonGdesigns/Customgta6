using System;
using System.Collections.Generic;
using System.Text;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// What a car is, read off the model rather than off a spawned example.
    ///
    /// The shop panel already shows performance bars, but it reads the four
    /// <c>GET_VEHICLE_*</c> natives, which need a vehicle in the world. The phone sells
    /// cars the player has never seen: he picked a name and a price out of a list with
    /// nothing else on the page, which is no way to spend forty thousand dollars. The
    /// model-level natives answer the same questions without putting a car on the street,
    /// so the phone can show the same ratings the shop does.
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

        /// <summary>
        /// The model's ratings, or null while it is still streaming in. The natives read
        /// the model's own data, so the model has to be in memory: it is asked for, read
        /// once, cached, and handed straight back to the engine. Nothing waits — a caller
        /// that gets null says so and asks again next frame.
        /// </summary>
        public static Ratings Of(Model model)
        {
            if (!model.IsValid || !model.IsVehicle) return null;
            if (Known.TryGetValue(model.Hash, out var cached)) return cached;

            bool ours = false;
            try
            {
                if (!model.IsLoaded) { model.Request(); ours = true; }
                if (!model.IsLoaded) return null;

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
                // A model that answers zero to everything has not really answered. Do not
                // cache that, or a car is permanently listed as having no engine.
                if (ratings.TopSpeed <= 0f && ratings.Acceleration <= 0f) return null;
                if (Known.Count >= 512) Known.Clear();
                Known[model.Hash] = ratings;
                return ratings;
            }
            catch (Exception ex)
            {
                Logger.Warn("Model ratings unavailable for " + model.Hash + ": " + ex.Message);
                return null;
            }
            finally { if (ours) model.MarkAsNoLongerNeeded(); }
        }

        /// <summary>A rating drawn as a bar, in characters the game's text renderer has.</summary>
        public static string Bar(float value)
        {
            int filled = (int)Math.Round(Math.Max(0f, Math.Min(1f, value)) * BarCells);
            return "[" + new string('=', filled) + new string('.', BarCells - filled) + "]";
        }

        private static string Row(string label, float value) =>
            label.PadRight(9) + " " + Bar(value) + " " + (int)Math.Round(Math.Max(0f, Math.Min(1f, value)) * 100f) + "%";

        /// <summary>
        /// The performance block for a phone page, or a line saying it is not ready yet.
        /// Never returns an empty string: a blank space where the numbers should be reads
        /// as a car with no numbers rather than a page still loading.
        /// </summary>
        public static string Block(Model model)
        {
            var ratings = Of(model);
            if (ratings == null) return "PERFORMANCE\nReading the model's ratings. Give it a moment.";

            var text = new StringBuilder("PERFORMANCE\n");
            text.Append(Row("Top speed", ratings.SpeedFraction)).Append('\n');
            text.Append(Row("Accel", ratings.Acceleration)).Append('\n');
            text.Append(Row("Braking", ratings.Braking)).Append('\n');
            text.Append(Row("Traction", ratings.Traction)).Append('\n');
            text.Append("Est. top speed: ").Append((int)Math.Round(ratings.SpeedMph)).Append(" mph\n");
            if (ratings.Seats > 0) text.Append("Seats: ").Append(ratings.Seats).Append('\n');
            if (ratings.Upgradable)
                text.Append("Fully built: accel ")
                    .Append((int)Math.Round(Math.Min(1f, ratings.BuiltAcceleration) * 100f)).Append("% / braking ")
                    .Append((int)Math.Round(Math.Min(1f, ratings.BuiltBraking) * 100f)).Append("%\n");
            text.Append("Game ratings for the stock model, not a road test.");
            return text.ToString();
        }
    }
}
