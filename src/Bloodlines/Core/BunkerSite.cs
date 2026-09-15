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
        /// <summary>How near the entrance the bunker's map data is worth loading.</summary>
        public const float LoadRange = 150f;
        /// <summary>Whether the DLC map registration has already happened this session.</summary>
        public static bool Registered => _registered;

        /// <summary>
        /// Register the shipped DLC map data and ask for the bunker's exterior. This does
        /// not join Online; it only makes map data the game already has available in Story
        /// Mode.
        ///
        /// It is expensive and visible: the registration native stops to load, and Ron had
        /// a loading screen on every startup because the free-roam tick called this the
        /// first frame simply to put a blip on the map. A blip needs no map data at all.
        /// Call it when the bunker's geometry is actually about to be needed — walking up to
        /// it, or entering it, or a mission that opens there.
        /// </summary>
        public static void LoadMaps()
        {
            if (!_registered)
            {
                Function.Call((Hash)0x0888C3502DBBEEF5UL);
                _registered = true;
                Logger.Info("Registered the shipped DLC map data for the bunker (Story Mode; this is map data, not Online).");
            }
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
