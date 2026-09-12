using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>A sampled vertical column, not merely an ocean-height lookup under a road.</summary>
    public struct MarineColumn
    {
        public float Surface, Floor;
        public bool Known;
        /// <summary>Water with nothing under the probe: the seabed had not streamed in or lies deeper than the ray. Open water, not a verdict against it.</summary>
        public bool Assumed;
        public bool HasDepth(float depth) => Known && IsFinite(Surface) && IsFinite(Floor) && Floor <= Surface - depth;
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public interface IMarineProbe
    {
        MarineColumn Column(Vector3 point);
        bool Clear(Vector3 from, Vector3 to);
    }

    /// <summary>
    /// Resolves an entire small water footprint together. Land, unknown collision,
    /// bridge decks and shallow bottom are rejected; an exhausted search is failure.
    /// Only used at setup/placement, never every frame. Does not edit personal INIs.
    /// </summary>
    public static class MarineSites
    {
        public const float SearchLimit = 160f;
        public static readonly IMarineProbe Native = new NativeMarineProbe();

        public static bool TryResolve(Vector3 anchor, float depth, float halfWidth, float halfLength,
            float heading, float searchRadius, out Vector3 surface, out string reason, IMarineProbe probe = null)
        {
            probe = probe ?? Native;
            surface = Vector3.Zero;
            reason = "No loaded, unobstructed water column with sufficient depth.";
            if (!Finite(anchor.X) || !Finite(anchor.Y) || !Finite(anchor.Z) || !Finite(depth) ||
                !Finite(halfWidth) || !Finite(halfLength) || !Finite(heading) || !Finite(searchRadius) ||
                depth < 1f || halfWidth < 0f || halfLength < 0f) return false;
            float radians = heading * (float)Math.PI / 180f;
            var forward = new Vector3(-(float)Math.Sin(radians), (float)Math.Cos(radians), 0f);
            var right = new Vector3(forward.Y, -forward.X, 0f);
            var offsets = new[] { Vector3.Zero, right * halfWidth, right * -halfWidth,
                forward * halfLength, forward * -halfLength,
                right * halfWidth + forward * halfLength, right * -halfWidth + forward * halfLength,
                right * halfWidth - forward * halfLength, right * -halfWidth - forward * halfLength };
            foreach (var candidate in Candidates(anchor, Math.Max(0f, Math.Min(SearchLimit, searchRadius))))
            {
                var center = probe.Column(candidate);
                if (!center.HasDepth(depth)) continue;
                bool valid = true;
                foreach (var offset in offsets)
                {
                    var column = probe.Column(candidate + offset);
                    if (!column.HasDepth(depth) || Math.Abs(column.Surface - center.Surface) > 1f)
                    { valid = false; break; }
                    var start = new Vector3(candidate.X, candidate.Y, center.Surface - depth * .5f);
                    if (!probe.Clear(start, start + offset)) { valid = false; break; }
                }
                if (!valid) continue;
                surface = new Vector3(candidate.X, candidate.Y, center.Surface);
                reason = null;
                return true;
            }
            return false;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        public static IEnumerable<Vector3> Candidates(Vector3 anchor, float radius)
        {
            yield return anchor;
            for (float ring = 40f; ring <= radius; ring += 40f)
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * (float)Math.PI / 4f;
                    yield return anchor + new Vector3((float)Math.Cos(angle) * ring, (float)Math.Sin(angle) * ring, 0f);
                }
        }

        public static Vector3 ResolveOrThrow(LocationBook book, string key, float depth,
            float halfWidth = 5f, float halfLength = 8f, float searchRadius = SearchLimit)
        {
            var location = book.Get(key);
            if (location == null) throw new InvalidOperationException("Missing marine location: " + key);
            var original = location.Position;
            // A real user survey is not silently moved around the harbor.
            float radius = location.Status == LocationStatus.Surveyed ? 0f : Math.Max(0f, Math.Min(SearchLimit, searchRadius));
            Vector3 water; string error;
            var native = Native as NativeMarineProbe;
            // The harbor's seabed streams in around the focus, not around a player
            // standing at a hangar: focus the site, ask for its collision and give it
            // a bounded moment to load before judging it (Ron, September 11: M18,
            // M19 and M20 refused to start at Terminal Island on an unloaded seabed).
            Function.Call(Hash.SET_FOCUS_POS_AND_VEL, original.X, original.Y, original.Z, 0f, 0f, 0f);
            try
            {
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    Function.Call(Hash.REQUEST_COLLISION_AT_COORD, original.X, original.Y, original.Z);
                    if (native == null || native.SeabedSeen(original)) break;
                    Script.Wait(300);
                }
                if (!TryResolve(original, depth, halfWidth, halfLength, location.Heading, radius, out water, out error))
                {
                    Logger.Error("Marine preflight " + key + " at " + original + " (" + location.Status + "): " + error);
                    throw new InvalidOperationException("Water placement unavailable at " + key + ". Survey open water with enough depth; see Bloodlines.log.");
                }
                var chosen = Native.Column(water);
                if (chosen.Assumed) Logger.Warn("Marine preflight " + key + ": the seabed under " + water + " had not streamed in; open water assumed. Survey the key if the site is wrong.");
            }
            finally { Function.Call(Hash.CLEAR_FOCUS); }
            // Runtime adjustment retains estimate provenance; it is not a survey.
            location.Position = water;
            Logger.Info("Marine preflight " + key + ": " + original + " -> " + water +
                ", depth >= " + depth + ", footprint " + halfWidth + " x " + halfLength + ", source " + location.Status);
            return water;
        }

        public static bool IsWaterborne(Vehicle vehicle, float minimumDepth = 2f)
        {
            if (vehicle == null || !vehicle.Exists() || vehicle.IsDead) return false;
            var c = Native.Column(vehicle.Position);
            return c.HasDepth(minimumDepth) && vehicle.Position.Z < c.Surface + 1.5f && vehicle.Position.Z > c.Floor + .5f;
        }

        private sealed class NativeMarineProbe : IMarineProbe
        {
            /// <summary>True once a probe under the water at this point actually hits the seabed: the site's collision has streamed in.</summary>
            public bool SeabedSeen(Vector3 point)
            {
                var height = new OutputArgument();
                if (!Function.Call<bool>(Hash.GET_WATER_HEIGHT_NO_WAVES, point.X, point.Y, 1000f, height)) return false;
                float water = height.GetResult<float>();
                return Trace(new Vector3(point.X, point.Y, water + 80f), new Vector3(point.X, point.Y, water - 120f), out bool hit, out _) && hit;
            }

            public MarineColumn Column(Vector3 point)
            {
                var height = new OutputArgument();
                if (!Function.Call<bool>(Hash.GET_WATER_HEIGHT_NO_WAVES, point.X, point.Y, 1000f, height)) return default;
                float water = height.GetResult<float>();
                if (float.IsNaN(water) || float.IsInfinity(water)) return default;
                // Start ABOVE possible piers/freeways. The first solid hit must be
                // underwater; querying a water plane beneath dry terrain is not enough.
                Function.Call(Hash.REQUEST_COLLISION_AT_COORD, point.X, point.Y, water);
                if (!Trace(new Vector3(point.X, point.Y, water + 80f),
                    new Vector3(point.X, point.Y, water - 120f), out bool hit, out var bottom) || !hit)
                    // Nothing under the ray: open water whose seabed has not streamed
                    // in, or water deeper than the probe. A deck over the water or a
                    // shallow bottom is a hit and is still rejected.
                    return new MarineColumn { Known = true, Assumed = true, Surface = water, Floor = water - 500f };
                return new MarineColumn { Known = true, Surface = water, Floor = bottom.Z };
            }
            public bool Clear(Vector3 from, Vector3 to)
            {
                if (from.DistanceTo(to) < .05f) return true;
                // A probe with no result has nothing loaded to hit: clear, not blocked.
                if (!Trace(from, to, out bool hit, out _)) return true;
                return !hit;
            }
            private static bool Trace(Vector3 from, Vector3 to, out bool hit, out Vector3 end)
            {
                // Bounded setup-only synchronous collision probe. No water flag:
                // surface water must not be mistaken for the seabed. Map + objects.
                int ray = Function.Call<int>(Hash.START_EXPENSIVE_SYNCHRONOUS_SHAPE_TEST_LOS_PROBE,
                    from.X, from.Y, from.Z, to.X, to.Y, to.Z, 17, 0, 4);
                var didHit = new OutputArgument(); var at = new OutputArgument();
                var normal = new OutputArgument(); var entity = new OutputArgument();
                int state = Function.Call<int>(Hash.GET_SHAPE_TEST_RESULT, ray, didHit, at, normal, entity);
                hit = state == 2 && didHit.GetResult<bool>();
                end = state == 2 ? at.GetResult<Vector3>() : Vector3.Zero;
                return state == 2;
            }
        }
    }
}
