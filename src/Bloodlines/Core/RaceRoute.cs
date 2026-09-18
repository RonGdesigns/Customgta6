using System;
using System.Collections.Generic;
using System.Linq;
using GTA.Math;

namespace Bloodlines.Core
{
    /// <summary>
    /// A race route built from ordered keys, with the drivable part put onto real lanes at
    /// runtime.
    ///
    /// Roads are baked terrain, so no placed-entity survey finds one: this is the same
    /// problem M49 has with the Great Ocean Highway, and the same answer. An authored gate
    /// is a **seed**; the lane comes from the nearest vehicle node when the game is running.
    /// What that buys is a gate the player can actually drive through rather than one
    /// sitting on the sidewalk beside the road it was meant to be on.
    ///
    /// Three rules it keeps, each of them a bug it exists to refuse.
    ///
    /// **A trail is not a lane, so a trail gate is never snapped.** The nearest vehicle node
    /// to a point halfway up Mount Chiliad is the road at the bottom of the mountain, and a
    /// snap there does not correct the gate, it deletes the climb. Trail keys are passed
    /// through untouched, which is the same distinction <c>MissionSites</c> draws when it
    /// grounds only <c>land</c>.
    ///
    /// **A snap may correct a gate; it may never move the race backward.** A node that sits
    /// further from the finish than the gate before it would make the route double back,
    /// which is the exact fault this route was rebuilt to remove. Such an answer is refused
    /// and the seed kept.
    ///
    /// **Nothing here is a refusal.** A missing node means the seed is used and the count is
    /// reported, the way <c>PlacementPreflight</c> only ever warns: a route whose gates are
    /// a few meters off the lane is a race, and a race that will not start is not.
    /// </summary>
    public static class RaceRoute
    {
        /// <summary>How far a seed may be from a lane and still be corrected onto it.</summary>
        public const float LaneSearch = 45f;

        /// <summary>What was built, and how much of it stands on a real lane.</summary>
        public sealed class Route
        {
            public readonly List<Vector3> Gates = new List<Vector3>();
            /// <summary>Drivable gates read from the book.</summary>
            public int Seeds;
            /// <summary>Of those, how many a vehicle node answered for.</summary>
            public int Snapped;
            /// <summary>Of those, how many a node answered for and was refused for doubling back.</summary>
            public int Refused;
            /// <summary>Trail gates, which are never snapped.</summary>
            public int Trail;
            public bool IsUsable => Gates.Count >= 2;
            public string Report =>
                Gates.Count + " gates: " + Snapped + " of " + Seeds + " road gates on a real lane" +
                (Refused > 0 ? ", " + Refused + " node answers refused for doubling back" : "") +
                ", " + Trail + " trail gates on their authored geometry";
        }

        /// <summary>
        /// Build the route. <paramref name="roadPrefix"/> keys are snapped to lanes,
        /// <paramref name="trailPrefix"/> keys are not, and <paramref name="finishKey"/>
        /// is the last gate. Keys are read through the book so the placement preflight
        /// sees every one of them.
        /// </summary>
        public static Route Build(LocationBook book, string roadPrefix, string trailPrefix, string finishKey)
        {
            var route = new Route();
            if (book == null) return route;
            var finish = book.Position(finishKey);

            foreach (var key in Ordered(book, roadPrefix))
            {
                var seed = book.Position(key);
                route.Seeds++;
                var gate = seed;
                if (GameUtils.NearestRoadNode(seed, LaneSearch, out var node, out _))
                {
                    // The correction has to be an improvement, not a step back down the road.
                    if (route.Gates.Count == 0 || node.DistanceTo(finish) <= route.Gates[route.Gates.Count - 1].DistanceTo(finish))
                    { gate = node; route.Snapped++; }
                    else
                    {
                        route.Refused++;
                        Logger.Warn(key + ": the nearest lane sits further from the finish than the gate before it; keeping the authored seed.");
                    }
                }
                route.Gates.Add(gate);
            }

            foreach (var key in Ordered(book, trailPrefix))
            {
                route.Gates.Add(book.Position(key));
                route.Trail++;
            }

            route.Gates.Add(finish);
            Logger.Info("Race route " + roadPrefix + ": " + route.Report);
            return route;
        }

        private static IEnumerable<string> Ordered(LocationBook book, string prefix) =>
            book.All.Where(l => l.Key.StartsWith(prefix, StringComparison.Ordinal))
                    .Select(l => l.Key).OrderBy(k => k, StringComparer.Ordinal).ToList();

        /// <summary>
        /// How far a racer still has to cover, in meters: the distance to the gate he is
        /// driving at, plus every leg after it.
        ///
        /// Measured from the finish rather than from the start, because that needs no
        /// reference to where the grid was and it is the number a race is actually decided
        /// by. Gate index alone is too coarse to pace with: two cars between the same pair
        /// of gates are level by that measure even when one is about to cross and the other
        /// has just left. Comparing two racers is subtracting one of these from the other,
        /// and the smaller number is winning.
        /// </summary>
        public static float Remaining(IList<Vector3> gates, int nextGate, Vector3 at)
        {
            if (gates == null || gates.Count == 0) return 0f;
            if (nextGate >= gates.Count) return 0f;
            int index = Math.Max(0, nextGate);
            float remaining = at.DistanceTo(gates[index]);
            for (int i = index + 1; i < gates.Count; i++) remaining += gates[i - 1].DistanceTo(gates[i]);
            return remaining;
        }
    }
}
