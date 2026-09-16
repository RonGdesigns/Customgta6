using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// Multi-point routes, drawn on the ground as a line you can follow with your eyes.
    ///
    /// A convoy, a patrol, a boat run or a helicopter approach is a path, and the location
    /// book only knows points. Missions have been storing a route as two keys — a start
    /// and a destination — and hoping the engine's pathing invents something sensible in
    /// between, which is how M35's roadblock ended up 195 m past the ambush it was
    /// supposed to close.
    ///
    /// **The convention is the whole format.** A route is ordered keys:
    /// <c>M09.Convoy.01</c>, <c>M09.Convoy.02</c>, <c>M09.Convoy.03</c>. No second file, no
    /// second schema, nothing to keep in step with <c>locations.tsv</c> — a route is just
    /// points that happen to be numbered, and every tool that already reads the book reads
    /// routes for free. Survey them like any other key and the line appears.
    /// </summary>
    public static class RouteRibbons
    {
        /// <summary>How high above each point the line is drawn, so it is not inside the road.</summary>
        public const float Lift = .5f;
        /// <summary>Routes are only drawn this close, because a line across the map is noise.</summary>
        public const float DrawRange = 400f;

        /// <summary>
        /// Whether a key is one leg of a route: three or more parts with a number last.
        /// <c>M09.Convoy.02</c> is; <c>M09.Convoy</c> and <c>M45.Helipad</c> are not.
        /// </summary>
        public static bool IsLeg(string key, out string route, out int order)
        {
            route = null; order = 0;
            if (string.IsNullOrEmpty(key)) return false;
            int dot = key.LastIndexOf('.');
            if (dot <= 0 || dot == key.Length - 1) return false;
            string tail = key.Substring(dot + 1);
            foreach (char c in tail) if (c < '0' || c > '9') return false;
            string head = key.Substring(0, dot);
            if (head.IndexOf('.') <= 0) return false; // "M09.Convoy" is the route; "M09" alone is not
            route = head;
            return int.TryParse(tail, out order);
        }

        /// <summary>Every route in the book, or only one mission's when a prefix is given.</summary>
        public static IEnumerable<KeyValuePair<string, List<MissionLocation>>> Routes(LocationBook book, string missionPrefix = null)
        {
            if (book == null) yield break;
            var found = new Dictionary<string, List<KeyValuePair<int, MissionLocation>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var location in book.All)
            {
                if (!IsLeg(location.Key, out string route, out int order)) continue;
                if (!string.IsNullOrEmpty(missionPrefix) &&
                    !route.StartsWith(missionPrefix + ".", StringComparison.OrdinalIgnoreCase)) continue;
                if (!found.TryGetValue(route, out var legs)) found[route] = legs = new List<KeyValuePair<int, MissionLocation>>();
                legs.Add(new KeyValuePair<int, MissionLocation>(order, location));
            }
            foreach (var pair in found.OrderBy(p => p.Key, StringComparer.Ordinal))
                yield return new KeyValuePair<string, List<MissionLocation>>(
                    pair.Key, pair.Value.OrderBy(l => l.Key).Select(l => l.Value).ToList());
        }

        /// <summary>
        /// Draw them. Amber for a route whose legs are all still estimates, green once
        /// every leg has been surveyed — so the picture says how much of the path is real.
        /// </summary>
        public static void Draw(LocationBook book, string missionPrefix, GTA.Math.Vector3 viewer)
        {
            foreach (var route in Routes(book, missionPrefix))
            {
                var legs = route.Value;
                if (legs.Count < 2) continue;
                bool verified = legs.All(l => l.Status != LocationStatus.Estimate);
                var color = verified ? Color.FromArgb(200, 120, 220, 140) : Color.FromArgb(200, 232, 168, 56);
                for (int i = 1; i < legs.Count; i++)
                {
                    var a = legs[i - 1].Position; var b = legs[i].Position;
                    if (a.DistanceTo(viewer) > DrawRange && b.DistanceTo(viewer) > DrawRange) continue;
                    try
                    {
                        Function.Call(Hash.DRAW_LINE, a.X, a.Y, a.Z + Lift, b.X, b.Y, b.Z + Lift,
                            color.R, color.G, color.B, color.A);
                    }
                    catch (Exception ex) { Logger.Warn("A route line could not be drawn: " + ex.Message); return; }
                }
                foreach (var leg in legs)
                {
                    if (leg.Position.DistanceTo(viewer) > DrawRange) continue;
                    GameUtils.DrawObjectiveMarker(leg.Position + new GTA.Math.Vector3(0f, 0f, Lift), color, .25f);
                }
            }
        }
    }
}
