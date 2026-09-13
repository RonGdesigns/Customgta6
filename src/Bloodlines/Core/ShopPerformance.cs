using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>Game HUD ratings, not invented DPS or measured road acceleration.</summary>
    public static class ShopPerformance
    {
        private static readonly Dictionary<uint,int[]> WeaponRatings=new Dictionary<uint,int[]>(), PartRatings=new Dictionary<uint,int[]>();
        public static float[] Vehicle(Vehicle car)
        {
            if(car==null||!car.Exists())return null;
            return new[] {
                Function.Call<float>(Hash.GET_VEHICLE_ESTIMATED_MAX_SPEED,car)*3.6f/400f,
                Function.Call<float>(Hash.GET_VEHICLE_ACCELERATION,car),
                Function.Call<float>(Hash.GET_VEHICLE_MAX_BRAKING,car),
                Function.Call<float>(Hash.GET_VEHICLE_MAX_TRACTION,car)/3f };
        }
        public static int[] Weapon(uint hash, bool component=false)
        {
            if(hash==0)return null;
            var cache=component?PartRatings:WeaponRatings;
            if(cache.TryGetValue(hash,out var cached))return cached;
            // Five native slots, eight-byte aligned. Reserve extra room; never use
            // a single OutputArgument for a structure the native writes through.
            var memory=Marshal.AllocHGlobal(128);
            try
            {
                for(int i=0;i<128;i++)Marshal.WriteByte(memory,i,0);
                if(!Function.Call<bool>(component?Hash.GET_WEAPON_COMPONENT_HUD_STATS:Hash.GET_WEAPON_HUD_STATS,hash,memory)) { Remember(cache,hash,null);return null; }
                var values=new int[5];
                for(int i=0;i<5;i++)values[i]=component?Marshal.ReadInt32(memory,i*8):Marshal.ReadByte(memory,i*8);
                Remember(cache,hash,values);return values;
            }
            catch(Exception ex) { Logger.Warn("Weapon HUD ratings unavailable: "+hash+" / "+ex.Message);Remember(cache,hash,null);return null; }
            finally { Marshal.FreeHGlobal(memory); }
        }
        private static void Remember(Dictionary<uint,int[]> cache,uint hash,int[] values)
        { if(cache.Count>=512)cache.Clear();cache[hash]=values; }
    }
}
