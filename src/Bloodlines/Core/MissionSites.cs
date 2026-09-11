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
        public static bool Prepare(LocationBook book, string mission)
        {
            var sites = book.All.Where(l => l.Key.StartsWith(mission + ".", StringComparison.OrdinalIgnoreCase)).ToArray();
            if (!Ground(book, sites.Where(l => l.Kind == "land").Select(l => l.Key).ToArray())) return false;
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
                        if (Walkable(safe, point)) break;
                        if (location.Status != LocationStatus.Surveyed)
                        {
                            float ground = World.GetGroundHeight(new Vector3(point.X, point.Y, location.Position.Z + 150f));
                            if (ground > 0.5f && Math.Abs(ground - location.Position.Z) <= 150f && Math.Abs(ground - point.Z) > 0.5f)
                            {
                                var snapped = new Vector3(point.X, point.Y, ground + 0.5f);
                                safe = World.GetSafeCoordForPed(snapped, false, 0);
                                if (Walkable(safe, snapped)) { point = snapped; break; }
                            }
                        }
                        safe=Vector3.Zero; Script.Wait(50);
                    }
                    if (safe == Vector3.Zero)
                    {
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
        private static bool Walkable(Vector3 safe, Vector3 point) =>
            safe != Vector3.Zero && GameUtils.IsWithinFlat(safe, point, 35f) && Math.Abs(safe.Z - point.Z) < 25f;

        /// <summary>Water a boat can float in: the surface must stand this far above whatever ground is under it.</summary>
        public const float MinWaterDepth = 1.2f;

        /// <summary>
        /// A water key resolves to real water. The ocean's water plane runs under
        /// beaches and coast roads, so the surface height alone put a dinghy on the
        /// Palomino road (Ron, September 11); a point counts only when the ground
        /// under it is below the surface by <see cref="MinWaterDepth"/>. Unloaded
        /// seabed (no ground found) counts as open water. Estimates search outward to
        /// 250 m, nearest ring first; a surveyed key is trusted as it stands.
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
                    int radiusLimit=location.Status==LocationStatus.Surveyed?0:250;
                    for(int radius=0;radius<=radiusLimit&&!found.HasValue;radius+=25)
                        for(int angle=0;angle<8&&!found.HasValue;angle++)
                        {
                            var p=location.Position+new Vector3((float)Math.Cos(angle*Math.PI/4)*radius,(float)Math.Sin(angle*Math.PI/4)*radius,0);
                            var height=new OutputArgument();
                            if(!Function.Call<bool>(Hash.GET_WATER_HEIGHT,p.X,p.Y,100f,height)) continue;
                            surfaceSeen=true;
                            float surface=height.GetResult<float>();
                            if(!DeepEnough(p.X,p.Y,surface)) continue;
                            found=new Vector3(p.X,p.Y,surface+.2f);
                            if(radius>0) Logger.Info("Water key "+key+" resolved "+radius+" m from its estimate to floatable water at "+found.Value);
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

        /// <summary>True when no ground stands within <see cref="MinWaterDepth"/> of the surface at this column (or none is loaded).</summary>
        public static bool DeepEnough(float x, float y, float surface)
        {
            var ground=new OutputArgument();
            if(!Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD, x, y, surface+30f, ground, true, false)) return true;
            return ground.GetResult<float>() <= surface-MinWaterDepth;
        }
    }
}
