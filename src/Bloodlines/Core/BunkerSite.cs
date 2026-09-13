using GTA.Math;
using GTA.Native;
namespace Bloodlines.Core
{
    /// <summary>One physical headquarters shared by the mission and free-roam access.</summary>
    public static class BunkerSite
    {
        // Preserve the existing save unlock despite moving away from the radar array.
        public const string Unlock = "grandSenoraRadarBunker";
        public const string EntranceKey = "M23.Entrance";
        public const string InteriorKey = "Bunker.Senora.Interior";
        public const string DoorKey = "Bunker.Senora.Room.Door";
        public const string InspectKey = "Bunker.Senora.Room.Inspect";
        public const string ExteriorIpl = "gr_case0_bunkerclosed";
        public const string InteriorIpl = "gr_grdlc_interior_placement_interior_1_grdlc_int_02_milo_";
        public static Residence Residence => new Residence {
            Name = "Grand Senora bunker headquarters", Short = "Senora bunker",
            EntranceKey = EntranceKey, InteriorKey = InteriorKey, RoomPrefix = "Bunker.Senora.Room",
            Ipl = InteriorIpl, Probe = new Vector3(892.6384f,-3245.8664f,-98.265f),
            EntitySets = new[] { "Bunker_Style_A", "standard_bunker_set", "standard_security_set", "gun_wall_blocker", "gun_range_blocker_set" }
        };
        private static bool _registered;
        public static void LoadMaps()
        {
            // Register shipped DLC map data in Story Mode; this does not join Online.
            if (!_registered) { Function.Call((Hash)0x0888C3502DBBEEF5UL); _registered = true; }
            Function.Call(Hash.REQUEST_IPL, ExteriorIpl);
        }
        public static bool Enter(ApartmentAccess access, LocationBook book)
        {
            var room=book.Get(InteriorKey); if(room==null||access.Busy||access.Inside)return false;
            LoadMaps(); var residence=Residence;
            // The shipped YMAP has a trailing underscore; Story Mode registrations
            // also expose the shortened name. Keep this one HQ map registered for
            // repeat visits. The apartment service still requires a real ready room.
            Function.Call(Hash.REQUEST_IPL, InteriorIpl.TrimEnd('_'));
            return access.Begin(room.Position,residence.Ipl,true,residence.Probe,room.Heading,residence.EntitySets,
                new[] { "Bunker_Style_B", "Bunker_Style_C", "upgrade_bunker_set", "security_upgrade", "Office_blocker_set" });
        }
    }
}
