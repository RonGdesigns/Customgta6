using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;
namespace Bloodlines.Core
{
    public static class MissionSites
    {
        private static readonly Dictionary<MissionLocation, float> Depths = new Dictionary<MissionLocation, float>();
        public static bool Prepare(LocationBook book, string mission, params string[] fixedSurfaceKeys)
        {
            var sites = book.All.Where(l => !l.IsEditorSlot && l.Key.StartsWith(mission + ".", StringComparison.OrdinalIgnoreCase)).ToArray();
            // A verified prop/platform surface must not snap to the terrain navmesh beneath it.
            if (!Ground(book, sites.Where(l => l.Kind == "land" && !fixedSurfaceKeys.Contains(l.Key, StringComparer.OrdinalIgnoreCase)).Select(l => l.Key).ToArray())) return false;
            foreach (var location in sites.Where(l => l.Kind == "water"))
            {
                // Negative authored sea coordinates represent depth, not a bad spawn height.
                if (!Depths.TryGetValue(location, out var depth)) Depths[location] = depth = Math.Min(0f, location.Position.Z);
                if (!Water(book, location.Key)) return false;
                if (depth < 0f) location.Position += new Vector3(0f, 0f, depth);
            }
            return true;
        }
        public static bool Ground(LocationBook book, params string[] keys)
        {
            var resolved = new Dictionary<MissionLocation, Vector3>();
            try
            {
                foreach (var key in keys)
                {
                    var location = book.Get(key); if (location == null) return false;
                    bool surveyed = location.Status == LocationStatus.Surveyed;
                    var point = location.Position; Vector3 safe = Vector3.Zero;
                    Function.Call(Hash.SET_FOCUS_POS_AND_VEL, point.X, point.Y, point.Z, 0f,0f,0f);
                    for (int attempt=0;attempt<12;attempt++)
                    {
                        Function.Call(Hash.REQUEST_COLLISION_AT_COORD, point.X,point.Y,point.Z);
                        // The authored height first: a curb under a terminal roof must
                        // stay a curb. Only when nothing walkable is near that height
                        // does an estimate ask the map for the ground, which is how a
                        // hillside perch 40 m above its guess still resolves.
                        safe=World.GetSafeCoordForPed(point,false,0);
                        if (Walkable(safe, point, surveyed)) break;
                        if (location.Status != LocationStatus.Surveyed)
                        {
                            float ground = World.GetGroundHeight(new Vector3(point.X, point.Y, location.Position.Z + 150f));
                            if (ground > 0.5f && Math.Abs(ground - location.Position.Z) <= 150f && Math.Abs(ground - point.Z) > 0.5f)
                            {
                                var snapped = new Vector3(point.X, point.Y, ground + 0.5f);
                                safe = World.GetSafeCoordForPed(snapped, false, 0);
                                if (Walkable(safe, snapped, surveyed)) { point = snapped; break; }
                            }
                        }
                        safe=Vector3.Zero; Script.Wait(50);
                    }
                    if (safe == Vector3.Zero)
                    {
                        // Deliberately still a failure for a surveyed key as well as an
                        // estimate: the location test exists to surface sites that need a
                        // look, and silently keeping a surveyed point would hide a real
                        // problem at it. The caller decides what to do about it.
                        Logger.Error("No walkable mission surface: " + key + " at " + point + " (authored " + location.Position + ", " + location.Status + ")");
                        GameUtils.Subtitle("~r~Cannot load " + key + ": no walkable ground near its coordinates. Survey it (F11) and retry.",6000);
                        return false;
                    }
                    resolved[location]=safe+new Vector3(0,0,.1f);
                }
                foreach(var pair in resolved)pair.Key.Position=pair.Value;
                return true;
            }
            finally { Function.Call(Hash.CLEAR_FOCUS); }
        }
        /// <summary>Reject solid map/prop space even when a nearby navmesh point exists.</summary>
        /// <summary>
        /// The height of the first solid surface under a point, searched downward from
        /// <paramref name="ceiling"/> to <paramref name="floor"/>, or null if the probe
        /// found nothing between them.
        ///
        /// This is for standing on something the world built rather than on terrain.
        /// <see cref="Ground"/> asks the engine for walkable ground, and over water or
        /// on a vessel the honest answer to that is the sea. A deck, a pier or a
        /// platform has to be measured where it is, and the only thing that actually
        /// knows where a deck is at runtime is the geometry.
        ///
        /// Search downward from just above the authored point, never from the top of the
        /// structure: a probe that starts above a superstructure finds its roof, which is
        /// a worse answer than the estimate it was correcting.
        /// </summary>
        public static float? SurfaceHeight(Vector3 at, float ceiling, float floor)
        {
            if (ceiling <= floor) return null;
            try
            {
                Function.Call(Hash.REQUEST_COLLISION_AT_COORD, at.X, at.Y, ceiling);
                var from = new Vector3(at.X, at.Y, ceiling);
                var to = new Vector3(at.X, at.Y, floor);
                var hit = World.Raycast(from, to, IntersectFlags.Map | IntersectFlags.Objects);
                if (!hit.DidHit) return null;
                float z = hit.HitPosition.Z;
                return z <= ceiling + .01f && z >= floor - .01f ? z : (float?)null;
            }
            catch (Exception ex)
            {
                Logger.Warn("A surface probe at " + at + " could not run: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// The same probe, given a few frames for collision to stream in, and the point
        /// it was handed if it never answers. A wrong height is better than a refused
        /// mission, but it is reported either way.
        /// </summary>
        public static Vector3 OnSurface(Vector3 at, float headroom, float floor, string what, int attempts = 8)
        {
            for (int i = 0; i < Math.Max(1, attempts); i++)
            {
                float? found = SurfaceHeight(at, at.Z + headroom, floor);
                if (found.HasValue)
                {
                    float corrected = found.Value;
                    if (Math.Abs(corrected - at.Z) > .25f)
                        Logger.Info(what + ": the surface is at " + corrected.ToString("0.00") +
                            ", not the authored " + at.Z.ToString("0.00") + " — using the geometry.");
                    return new Vector3(at.X, at.Y, corrected);
                }
                if (i < attempts - 1) Script.Wait(125);
            }
            Logger.Warn(what + ": nothing solid found under " + at + " between " +
                (at.Z + headroom).ToString("0.00") + " and " + floor.ToString("0.00") + "; keeping the authored height.");
            return at;
        }

        public static Vector3 Actor(LocationBook book, string key, Vector3 fallback)
        {
            var anchor = MissionPlacement.Position(book, key, fallback);
            for (int ring = 0; ring <= 3; ring++)
                for (int i = 0; i < (ring == 0 ? 1 : 8); i++)
                {
                    double angle = i * Math.PI / 4;
                    var p = anchor + new Vector3((float)Math.Cos(angle) * ring * 2f, (float)Math.Sin(angle) * ring * 2f, 0f);
                    Function.Call(Hash.REQUEST_COLLISION_AT_COORD, p.X, p.Y, p.Z);
                    var safe = World.GetSafeCoordForPed(p, false, 0);
                    if (safe == Vector3.Zero || !GameUtils.IsWithinFlat(safe, anchor, 8f) || Math.Abs(safe.Z-anchor.Z)>2f) continue;
                    bool blocked = false;
                    for (int direction=0; direction<8; direction++)
                    {
                        double a=direction*Math.PI/4;
                        var middle=safe+new Vector3(0f,0f,.7f);
                        var hit=World.Raycast(middle,middle+new Vector3((float)Math.Cos(a)*.65f,(float)Math.Sin(a)*.65f,0f),IntersectFlags.Map|IntersectFlags.Objects);
                        if(hit.DidHit){blocked=true;break;}
                    }
                    if (!blocked) { Logger.Info("Actor placement " + key + ": " + anchor + " -> " + safe); return safe; }
                }
            throw new InvalidOperationException("No clear actor space at " + key + ". Survey this spawn and retry.");
        }

        /// <summary>
        /// How far the engine's idea of walkable ground may be from the authored point
        /// before it stops being the same place. This was 35 meters flat and 25 down for
        /// everything, which is not a correction, it is a different location: on a pier at
        /// z 3 with water at z 0 the safe coord lands beside the pier and the authored
        /// point was rewritten into the sea. That is where Ron found M40's kits.
        /// </summary>
        public const float EstimateDrift = 12f;
        public const float EstimateDrop = 25f;
        /// <summary>A point Ron surveyed himself moves only enough to settle onto its own surface.</summary>
        public const float SurveyedDrift = 3f;
        public const float SurveyedDrop = 2.5f;

        private static bool Walkable(Vector3 safe, Vector3 point, bool surveyed) =>
            safe != Vector3.Zero &&
            GameUtils.IsWithinFlat(safe, point, surveyed ? SurveyedDrift : EstimateDrift) &&
            Math.Abs(safe.Z - point.Z) < (surveyed ? SurveyedDrop : EstimateDrop);

        /// <summary>
        /// Where the crew's car stands at a base: the surveyed car key when there is
        /// one (Ron, September 12: Base.CypressFlatsCar, so the car is neither under
        /// the lot nor on a roof), else the base key plus the offset.
        /// </summary>
        public static Vector3 CrewCarSpot(LocationBook book, string baseKey, Vector3 fallbackOffset, out float heading)
        {
            var car = book.Get(baseKey + "Car");
            if (car != null) { heading = car.Heading; return car.Position; }
            heading = book.Heading(baseKey);
            return book.Position(baseKey) + fallbackOffset;
        }

        /// <summary>Water a boat can float in: the surface must stand this far above whatever ground is under it.</summary>
        public const float MinWaterDepth = 1.2f;
        /// <summary>What a surveyed water key needs under it: real water, not the estimate's clearance.</summary>
        public const float SurveyedWaterDepth = 0.3f;
        /// <summary>How far a surveyed key looks for deeper water when its own point has none.</summary>
        public const int SurveyedSearch = 40;

        /// <summary>
        /// A water key resolves to real water. The ocean's water plane runs under
        /// beaches and coast roads, so the surface height alone put a dinghy on the
        /// Palomino road (Ron, September 11); a point counts only when the ground
        /// under it is below the surface by <see cref="MinWaterDepth"/>. Unloaded
        /// seabed (no ground found) counts as open water. Estimates search outward to
        /// 250 m, nearest ring first. A surveyed key is trusted where it stands and only
/// needs real water under it, because Ron watched boats moor in less than the
/// estimate's depth at M05.GrottoMouth; a step away from a survey needs the full depth.
        /// </summary>
        public static bool Water(LocationBook book, params string[] keys)
        {
            var resolved=new Dictionary<MissionLocation,Vector3>();
            try
            {
                foreach(var key in keys)
                {
                    var location=book.Get(key);if(location==null)return false;
                    Function.Call(Hash.SET_FOCUS_POS_AND_VEL, location.Position.X, location.Position.Y, location.Position.Z, 0f,0f,0f);
                    Function.Call(Hash.REQUEST_COLLISION_AT_COORD, location.Position.X, location.Position.Y, location.Position.Z);
                    Vector3? found=null; bool surfaceSeen=false;
                    bool surveyed=location.Status==LocationStatus.Surveyed;
                    int radiusLimit=surveyed?SurveyedSearch:250; int step=surveyed?10:25;
                    for(int radius=0;radius<=radiusLimit&&!found.HasValue;radius+=step)
                        for(int angle=0;angle<8&&!found.HasValue;angle++)
                        {
                            var p=location.Position+new Vector3((float)Math.Cos(angle*Math.PI/4)*radius,(float)Math.Sin(angle*Math.PI/4)*radius,0);
                            var height=new OutputArgument();
                            if(!Function.Call<bool>(Hash.GET_WATER_HEIGHT,p.X,p.Y,100f,height)) continue;
                            surfaceSeen=true;
                            float surface=height.GetResult<float>();
                            // The survey's own point needs only real water under it; a step
                            // away from it, or an estimate, needs the full depth.
                            if(!DeepEnough(p.X,p.Y,surface,surveyed&&radius==0?SurveyedWaterDepth:MinWaterDepth)) continue;
                            found=new Vector3(p.X,p.Y,surface+.2f);
                            if(radius>0) Logger.Info("Water key "+key+" resolved "+radius+" m from its "+(surveyed?"survey":"estimate")+" to floatable water at "+found.Value);
                            if(radius==0&&surveyed) Logger.Info("Water key "+key+" trusted at its survey, "+found.Value+".");
                        }
                    if(!found.HasValue)
                    {
                        string why=surfaceSeen?"the water there is under land or too shallow to float in":"no water surface";
                        Logger.Error("No usable water: "+key+" ("+why+")");
                        GameUtils.Subtitle("~r~No water deep enough at "+key+". Survey the coast and retry.",6000);
                        return false;
                    }
                    resolved[location]=found.Value;
                }
            }
            finally { Function.Call(Hash.CLEAR_FOCUS); }
            foreach(var pair in resolved)pair.Key.Position=pair.Value;
            return true;
        }

        /// <summary>True when no ground stands within the given depth of the surface at this column (or none is loaded).</summary>
        public static bool DeepEnough(float x, float y, float surface) => DeepEnough(x, y, surface, MinWaterDepth);
        public static bool DeepEnough(float x, float y, float surface, float minDepth)
        {
            var ground=new OutputArgument();
            // A depth probe needs the seabed. Including water returns the surface
            // as ground and makes an otherwise valid boat spawn look too shallow.
            if(!Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD, x, y, surface+30f, ground, false, false)) return true;
            return ground.GetResult<float>() <= surface-minDepth;
        }
    }
}
