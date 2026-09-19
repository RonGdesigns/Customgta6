using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// The cars KJ puts on his grid, and how he picks which two to field.
    ///
    /// The first attempt at making his racers a challenge gave them **the player's own
    /// model**, and Ron said no outright: "I definitely don't want them to have my car."
    /// He is right, and not only on taste. The point of bringing your own car to a street
    /// race is that it is yours; two identical copies of it on the grid takes the thing the
    /// mission is about and hands it to the opposition.
    ///
    /// So KJ has his own cars. They are a fixed roster spread across the performance range,
    /// and he fields the two whose **model** top speed sits closest to the car the player
    /// turned up in. That is a fair fight without being a mirror: show up in a supercar and
    /// he brings out the fast ones, show up in a tuner and he brings something that can live
    /// with it. What is left of the gap is closed by <see cref="RacePacing"/>, bounded.
    ///
    /// Two rules:
    ///
    /// **The player's model is excluded, always.** Matching by speed could otherwise land on
    /// his exact car by coincidence, which is the thing he asked not to see.
    ///
    /// **Speed is compared model to model.** The player's car may have parts on it and the
    /// rivals get the same parts fitted, so comparing the two models is the like-for-like
    /// comparison; comparing his built car to their stock ones would field a grid that is
    /// always too slow.
    /// </summary>
    public static class RivalGrid
    {
        /// <summary>
        /// KJ's cars. Every one is base-game (no DLC to own), a street-race plausible thing
        /// for his crew to keep, and they span roughly 41 to 52 meters per second stock, so
        /// there is something comparable whatever the player arrives in.
        /// </summary>
        public static readonly string[] Roster =
        {
            "penumbra", "futo", "buffalo", "gauntlet", "phoenix", "elegy2",
            "ninef", "cheetah", "comet2", "coquette", "entityxf", "adder",
        };

        /// <summary>What a model is assumed to do when the engine will not say.</summary>
        public const float FallbackSpeed = 45f;

        /// <summary>A model's own top speed, which answers without a car in the world.</summary>
        public static float ModelSpeed(string model) => SpeedOfHash(new Model(model).Hash);

        /// <summary>The same, by hash, which is all the SDK's Model gives away about a car.</summary>
        public static float SpeedOfHash(int hash)
        {
            try
            {
                float top = Function.Call<float>(Hash.GET_VEHICLE_MODEL_ESTIMATED_MAX_SPEED, hash);
                return float.IsNaN(top) || float.IsInfinity(top) || top <= 0f ? FallbackSpeed : top;
            }
            catch { return FallbackSpeed; }
        }

        /// <summary>
        /// The <paramref name="count"/> cars closest in capability to <paramref name="target"/>,
        /// never the model whose hash is <paramref name="excludeHash"/> and never the same car
        /// twice. Ties break on the name, so a grid is the same grid on a retry.
        /// </summary>
        public static List<string> Pick(int count, float target, int excludeHash, Func<string, float> speedOf)
        {
            var speed = speedOf ?? ModelSpeed;
            return Roster
                .Where(m => new Model(m).Hash != excludeHash)
                .OrderBy(m => Math.Abs(speed(m) - target))
                .ThenBy(m => m, StringComparer.Ordinal)
                .Take(Math.Max(0, count))
                .ToList();
        }

        /// <summary>
        /// The grid for a player who turned up in <paramref name="playerCar"/>: matched to that
        /// car's own model rather than to the car itself, and never that model. A hash is all
        /// the SDK hands over for a model, and it is enough for both jobs.
        /// </summary>
        public static List<string> For(int count, Model playerCar, Func<string, float> speedOf = null)
        {
            float target = playerCar.Hash != 0 ? SpeedOfHash(playerCar.Hash) : FallbackSpeed;
            var grid = Pick(count, target, playerCar.Hash, speedOf);
            Logger.Info("KJ's grid against a car that does " + target.ToString("0.0") + " m/s: " +
                        string.Join(", ", grid.ToArray()));
            return grid;
        }
    }
}
