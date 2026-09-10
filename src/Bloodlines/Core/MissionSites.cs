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
                        // An estimate's height is a guess; a hillside perch can sit 40 m
                        // above it and the navmesh check would call that "no surface".
                        // Ask the map where the ground is first, once collision answers.
                        if (location.Status != LocationStatus.Surveyed)
                        {
                            float ground = World.GetGroundHeight(new Vector3(point.X, point.Y, location.Position.Z + 150f));
                            if (ground > 0.5f && Math.Abs(ground - location.Position.Z) <= 150f) point = new Vector3(point.X, point.Y, ground + 0.5f);
                        }
                        safe=World.GetSafeCoordForPed(point,false,0);
                        if (safe != Vector3.Zero && GameUtils.IsWithinFlat(safe,point,35f) && Math.Abs(safe.Z-point.Z)<25f) break;
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
        public static bool Water(LocationBook book, params string[] keys)
        {
            var resolved=new Dictionary<MissionLocation,Vector3>();
            foreach(var key in keys)
            {
                var location=book.Get(key);if(location==null)return false;
                Vector3? found=null;
                int radiusLimit=location.Status==LocationStatus.Surveyed?0:150;
                for(int radius=0;radius<=radiusLimit&&!found.HasValue;radius+=25)
                    for(int angle=0;angle<8&&!found.HasValue;angle++)
                    {
                        var p=location.Position+new Vector3((float)Math.Cos(angle*Math.PI/4)*radius,(float)Math.Sin(angle*Math.PI/4)*radius,0);
                        var height=new OutputArgument();
                        if(Function.Call<bool>(Hash.GET_WATER_HEIGHT,p.X,p.Y,100f,height)) found=new Vector3(p.X,p.Y,height.GetResult<float>()+.2f);
                    }
                if(!found.HasValue){Logger.Error("No water surface: "+key);GameUtils.Subtitle("~r~No water at "+key+". Survey the coast and retry.",6000);return false;}
                resolved[location]=found.Value;
            }
            foreach(var pair in resolved)pair.Key.Position=pair.Value;
            return true;
        }
    }
}
