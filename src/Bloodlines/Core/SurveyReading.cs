using System;
using System.Collections.Generic;
using System.Globalization;
using GTA.Math;

namespace Bloodlines.Core
{
    /// <summary>
    /// What the survey can see about one point, and whether it suits the key that point
    /// belongs to.
    ///
    /// The location book has always carried a <c>kind</c> per key — land, water, interior,
    /// air, channel, underground — and until now the survey used it for exactly one thing:
    /// whether to drop a <c>land</c> capture onto the surface under it. Nothing noticed a
    /// <c>land</c> key with no ground below it, an <c>interior</c> key where no interior
    /// loads, a <c>water</c> key twenty meters above the waterline, or an <c>air</c> key
    /// captured standing on a deck.
    ///
    /// Every complaint below is a bug this campaign actually shipped and found weeks later
    /// in game: M45's two men inside the hull, M40's hull kits under the pier, M43's laptop
    /// inside the surface it was stacked on, M53 searched below sea level for a tunnel at
    /// z 13, M55 filed unbuildable because a man dropped into an interior that never loaded
    /// is a man in open sky.
    ///
    /// **Judging is separate from probing on purpose.** This class holds numbers somebody
    /// else measured and decides what they mean, so the decision is testable without a game
    /// running. <see cref="SurveyMode"/> gathers them through its injected probes.
    ///
    /// **And a complaint is never a refusal.** An estimate is usually close enough to play,
    /// the same reason `PlacementPreflight` only ever warns. What this buys is the warning
    /// arriving while he is still standing there and can fix it, rather than six missions
    /// later.
    /// </summary>
    public sealed class SurveyReading
    {
        /// <summary>A man needs this much over his head to stand up.</summary>
        public const float PedHeadroom = 1.9f;
        /// <summary>How far a point may float over the ground before that is worth saying.</summary>
        public const float LandDrop = 3f;
        /// <summary>And how far above the surface a point has to be before it is genuinely in the air.</summary>
        public const float AirFloor = 5f;
        /// <summary>How near the waterline a water key belongs.</summary>
        public const float WaterBand = 2.5f;

        public string Key = "", Kind = "", Zone = "";
        public Vector3 Point;
        /// <summary>The first solid surface under the point, or null when nothing answered.</summary>
        public float? Surface;
        /// <summary>
        /// Whether anything actually looked for that surface. **Not measured is not the same
        /// as measured and found nothing**, and conflating the two calls every point in a
        /// harness broken - which is how the first version of this failed its own suite.
        /// </summary>
        public bool SurfaceProbed;
        /// <summary>
        /// The lowest height the probe found something solid at over the point, or a large
        /// number for open sky. Blocked *at* head height already means he cannot stand up,
        /// which is why the land check reads this as less-than-or-equal.
        /// </summary>
        public float Headroom = float.MaxValue;
        public bool HeadroomProbed;
        /// <summary>Whether an interior loads here, or null when nothing asked.</summary>
        public bool? Interior;
        /// <summary>The walkable radius around the point.</summary>
        public float Room = float.MaxValue;
        /// <summary>The water surface at this column, or null where there is none.</summary>
        public float? Water;
        /// <summary>The nearest other key of the same mission, which is how a site's shape is checked.</summary>
        public string NearestKey = "";
        public float NearestMeters;

        /// <summary>How far the point floats over the surface under it.</summary>
        public float Drop => Surface.HasValue ? Point.Z - Surface.Value : 0f;
        /// <summary>Whether anything at all answered under this point.</summary>
        public bool Grounded => Surface.HasValue;
        /// <summary>Something looked for ground and did not find it, which is a real fault.</summary>
        public bool Missing => SurfaceProbed && !Surface.HasValue;

        /// <summary>Whether the point suits its key's kind. Set by <see cref="Judge"/>.</summary>
        public bool Fits { get; private set; } = true;
        /// <summary>Everything wrong with it, in the order it was noticed.</summary>
        public List<string> Complaints { get; } = new List<string>();
        public string Complaint => Complaints.Count == 0 ? null : string.Join(" ", Complaints.ToArray());

        /// <summary>
        /// Decide what the numbers mean for this key's kind. Every branch names the shape of
        /// a real failure rather than a tolerance somebody liked.
        /// </summary>
        public SurveyReading Judge()
        {
            Complaints.Clear();
            switch ((Kind ?? "").Trim().ToLowerInvariant())
            {
                case "interior":
                    // A missing MLO is a man dropped into open sky at that height. One native
                    // call is cheaper than finding that out the other way.
                    if (Interior == false) Say("No interior loads here, so a man placed at this height is in open sky.");
                    if (Missing) Say("Nothing solid is under it.");
                    break;

                case "water":
                    if (!Water.HasValue) Say("There is no water in this column.");
                    else if (Point.Z - Water.Value > WaterBand)
                        Say("It sits " + Meters(Point.Z - Water.Value) + " above the waterline.");
                    break;

                case "air":
                    // An air key is a spawn at altitude, and LaunchAirborne depends on it
                    // actually being at one: a helicopter created on a deck has nowhere to go.
                    if (Grounded && Drop < AirFloor)
                        Say("It is only " + Meters(Drop) + " over the surface, which is not an air spawn.");
                    break;

                case "underground":
                    // The metro is twenty meters under a street. Open sky overhead means the
                    // point came up through the ceiling, which is the M53 failure inverted.
                    if (HeadroomProbed && Headroom > 50f) Say("There is open sky over it, so this is not under anything.");
                    if (Missing) Say("Nothing solid is under it.");
                    break;

                case "channel":
                    if (Missing) Say("Nothing solid is under it.");
                    else if (Grounded && Drop > LandDrop) Say("It floats " + Meters(Drop) + " over the channel floor.");
                    break;

                default: // land, and anything the book has not named
                    if (Missing) Say("Nothing solid is under it within reach of the probe.");
                    else if (Grounded && Drop > LandDrop) Say("It floats " + Meters(Drop) + " over the ground.");
                    // Cover is not a fault on land — plenty of these are inside a warehouse or
                    // under a bridge. Not being able to stand up is.
                    if (HeadroomProbed && Headroom <= PedHeadroom)
                        Say("There is a deckhead " + Meters(Headroom) + " over it; nobody can stand here.");
                    if (Interior == true) Say("An interior loads here, so this is a room rather than a street.");
                    break;
            }
            Fits = Complaints.Count == 0;
            return this;
        }

        private void Say(string complaint) { Complaints.Add(complaint); }

        private static string Meters(float value) =>
            value.ToString("0.0", CultureInfo.InvariantCulture) + " m";

        /// <summary>The short form the survey draws while he is flying.</summary>
        public string Hud()
        {
            var parts = new List<string>();
            parts.Add(Grounded
                ? "ground " + Meters(Math.Abs(Drop)) + (Drop < 0f ? " above" : " below")
                : SurfaceProbed ? "no ground under it" : "ground not measured");
            if (HeadroomProbed) parts.Add(Headroom > 50f ? "open sky" : "headroom " + Meters(Headroom));
            if (Room < 100f) parts.Add("clear " + Meters(Room));
            if (Interior == true) parts.Add("interior");
            if (Water.HasValue) parts.Add("water " + Water.Value.ToString("0.0", CultureInfo.InvariantCulture));
            if (!string.IsNullOrEmpty(Zone)) parts.Add(Zone);
            if (!string.IsNullOrEmpty(NearestKey))
                parts.Add(NearestKey + " " + Meters(NearestMeters) + " away");
            return string.Join(" | ", parts.ToArray());
        }

        /// <summary>One line for the sweep report.</summary>
        public string Line() =>
            (Fits ? "ok      " : "CHECK   ") + Key.PadRight(28) + (Kind ?? "").PadRight(12) +
            Hud() + (Fits ? "" : "  -- " + Complaint);
    }
}
