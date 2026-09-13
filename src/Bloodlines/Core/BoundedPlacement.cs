using System;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// Placement for authored lanes, bridge decks and runway starts. A nearby
    /// pedestrian navmesh is NOT permission to change floors or move a plane.
    /// No estimates are persisted and no personal survey is overwritten.
    /// </summary>
    public static class BoundedPlacement
    {
        public static Vector3 Forward(float heading)
        {
            double a = heading * Math.PI / 180d;
            return new Vector3(-(float)Math.Sin(a), (float)Math.Cos(a), 0f);
        }
        public static Vector3 Offset(Vector3 origin, float heading, float right, float forward) =>
            origin + Forward(heading) * forward + Forward(heading - 90f) * right;

        public static Vector3 Ped(LocationBook book, string key, Func<Vector3, bool> allowed = null)
        {
            var site = book.Get(key) ?? throw new InvalidOperationException("Missing placement: " + key);
            return PedAt(site.Position, key, allowed);
        }

        public static Vector3 PedAt(Vector3 anchor, string label, Func<Vector3, bool> allowed = null)
        {
            RequireFinite(anchor, label);
            if (allowed != null && !allowed(anchor)) throw Refused(label, "inside the excluded freight stack");
            try
            {
                Focus(anchor);
                for (int attempt = 0; attempt < 25; attempt++)
                {
                    Function.Call(Hash.REQUEST_COLLISION_AT_COORD, anchor.X, anchor.Y, anchor.Z);
                    var safe = World.GetSafeCoordForPed(anchor, false, 0);
                    if (safe != Vector3.Zero && GameUtils.IsWithinFlat(safe, anchor, 1.5f) && Math.Abs(safe.Z - anchor.Z) <= 1.1f &&
                        (allowed == null || allowed(safe)) && ClearBody(safe, .6f, 1.8f)) return safe;
                    // Raised rail decks and roofs need not have pedestrian navmesh.
                    // Only accept a real supporting surface beside the authored Z.
                    if (Floor(anchor, out float z) && Math.Abs(z - anchor.Z) <= 1.1f)
                    {
                        var grounded = new Vector3(anchor.X, anchor.Y, z + .05f);
                        if ((allowed == null || allowed(grounded)) && ClearBody(grounded, .6f, 1.8f)) return grounded;
                    }
                    Script.Wait(50);
                }
            }
            finally { Function.Call(Hash.CLEAR_FOCUS); }
            throw Refused(label, "no clear standing space on the authored level");
        }

        // User's September 13 report identifies the OLD freight yard as unusable.
        // This conservative exclusion is intentionally independent of collision:
        // decorative containers may have no queried collision. It is not an export
        // of individual CodeWalker container bounds. New layout stays north of it.
        public static bool OutsideSoloFreight(Vector3 p) =>
            !(p.X >= 1000f && p.X <= 1120f && p.Y >= -3140f && p.Y <= -3062.5f);

        public static void ClearWalk(Vector3 from, Vector3 to, string label)
        {
            // Validate the corridor at waist/chest height before a scene can move
            // its actor. In particular skip/finalization must not enter a freight.
            for (int i = 0; i <= 12; i++)
            {
                var p = from + (to - from) * (i / 12f);
                if (!OutsideSoloFreight(p)) throw Refused(label, "walk crosses the excluded freight stack");
            }
            foreach (float z in new[] { .65f, 1.4f })
                if (World.Raycast(from + new Vector3(0, 0, z), to + new Vector3(0, 0, z), IntersectFlags.Map | IntersectFlags.Objects).DidHit)
                    throw Refused(label, "loading walk is obstructed");
        }

        /// <summary>
        /// Fixed XY/heading, full model footprint, narrow floor-height band. Never
        /// snap an aircraft to pedestrian navmesh or search from above a hangar.
        /// Returns the model origin above its supporting floor, not a ped position.
        /// </summary>
        public static Vector3 Vehicle(LocationBook book, string key, Model model, Entity ignore = null,
            Func<Vector3, bool> allowed = null, float departureMeters = 0f)
        {
            var site = book.Get(key) ?? throw new InvalidOperationException("Missing placement: " + key);
            return VehicleAt(site.Position, site.Heading, key, model, ignore, allowed, departureMeters);
        }

        public static Vector3 VehicleAt(Vector3 anchor, float heading, string label, Model model, Entity ignore = null,
            Func<Vector3, bool> allowed = null, float departureMeters = 0f)
        {
            RequireFinite(anchor, label);
            var min = model.Dimensions.Item1; var max = model.Dimensions.Item2;
            if (max.X <= min.X || max.Y <= min.Y || max.Z <= min.Z) throw Refused(label, "model bounds unavailable");
            float left = min.X - .5f, right = max.X + .5f, back = min.Y - .5f, front = max.Y + .5f;
            try
            {
                Focus(anchor);
                for (int attempt = 0; attempt < 30; attempt++)
                {
                    Function.Call(Hash.REQUEST_COLLISION_AT_COORD, anchor.X, anchor.Y, anchor.Z);
                    bool clear = true; float lowest = float.MaxValue, highest = float.MinValue;
                    for (int x = 0; x < 5 && clear; x++) for (int y = 0; y < 5 && clear; y++)
                    {
                        var p = Offset(anchor, heading, left + (right - left) * x / 4f, back + (front - back) * y / 4f);
                        if ((allowed != null && !allowed(p)) || !Floor(p, out float z, ignore) || Math.Abs(z - anchor.Z) > 1.25f)
                        { clear = false; break; }
                        lowest = Math.Min(lowest, z); highest = Math.Max(highest, z);
                    }
                    if (clear && highest - lowest <= .9f)
                    {
                        // Rasterize the whole clearance box, including wings/tail.
                        foreach (float height in new[] { .45f, 1.3f, Math.Max(2f, max.Z - min.Z) })
                            for (int i = 0; i <= 6 && clear; i++)
                            {
                                var a = Offset(anchor, heading, left + (right - left) * i / 6f, back);
                                var b = Offset(anchor, heading, left + (right - left) * i / 6f, front + departureMeters);
                                a.Z = b.Z = highest + height;
                                if (World.Raycast(a, b, IntersectFlags.Map | IntersectFlags.Objects, ignore).DidHit) clear = false;
                            }
                        foreach (var vehicle in World.GetNearbyVehicles(anchor, Math.Max(25f, (max - min).Length() + departureMeters)))
                        {
                            if (vehicle == null || !vehicle.Exists() || vehicle == ignore) continue;
                            var center = vehicle.Position - anchor; var f = Forward(heading); var r = Forward(heading - 90f);
                            float along = center.X * f.X + center.Y * f.Y, across = center.X * r.X + center.Y * r.Y;
                            var size = vehicle.Model.Dimensions.Item2 - vehicle.Model.Dimensions.Item1;
                            float radius = (float)Math.Sqrt(size.X * size.X + size.Y * size.Y) / 2f;
                            if (Math.Abs(center.Z) < 6f && across + radius >= left && across - radius <= right &&
                                along + radius >= back && along - radius <= front + departureMeters) clear = false;
                        }
                        if (clear) return new Vector3(anchor.X, anchor.Y, highest - min.Z + .04f);
                    }
                    Script.Wait(50);
                }
            }
            finally { Function.Call(Hash.CLEAR_FOCUS); }
            throw Refused(label, "vehicle footprint or initial departure lane is not clear on the authored level");
        }

        private static bool Floor(Vector3 p, out float height, Entity ignore = null)
        {
            var hit = World.Raycast(p + new Vector3(0, 0, 1.5f), p - new Vector3(0, 0, 1.5f), IntersectFlags.Map | IntersectFlags.Objects, ignore);
            height = hit.HitPosition.Z;
            return hit.DidHit;
        }
        private static bool ClearBody(Vector3 p, float radius, float height)
        {
            foreach (float z in new[] { .35f, height * .55f, height })
                for (int i = 0; i < 8; i++)
                {
                    var from = p + new Vector3(0, 0, z);
                    var to = from + Forward(i * 45f) * radius;
                    if (World.Raycast(from, to, IntersectFlags.Map | IntersectFlags.Objects).DidHit) return false;
                }
            return true;
        }
        private static void Focus(Vector3 p) => Function.Call(Hash.SET_FOCUS_POS_AND_VEL, p.X, p.Y, p.Z, 0f, 0f, 0f);
        private static void RequireFinite(Vector3 p, string label)
        {
            if (float.IsNaN(p.X) || float.IsInfinity(p.X) || float.IsNaN(p.Y) || float.IsInfinity(p.Y) || float.IsNaN(p.Z) || float.IsInfinity(p.Z))
                throw Refused(label, "invalid coordinates");
        }
        private static InvalidOperationException Refused(string key, string reason)
        {
            string message = key + ": " + reason + ". Move/clear this placement in the survey editor and retry; no alternate floor was used.";
            Logger.Error(message); GameUtils.Subtitle("~r~" + message, 8000);
            return new InvalidOperationException(message);
        }
    }
}
