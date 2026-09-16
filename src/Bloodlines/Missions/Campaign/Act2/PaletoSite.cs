using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// The heights the Paleto operation is built on, read out of the installed
    /// archives rather than chosen. See docs/story-to-play/pass-02/SITE-REPORT.md for
    /// how each number was obtained and what is still unverified.
    ///
    /// The authored offshore rig is not in the game. What is at Paleto Cove is a
    /// 122-meter vessel with a furnished three-level interior, and these are its
    /// real extents. Horizontal positions live in the location book as ordinary
    /// estimates so a live survey can move them without touching this code; the
    /// vertical structure is here because the mission logic reasons about it.
    /// </summary>
    public static class PaletoSite
    {
        /// <summary>The hull's placed origin: (-1748.00, 5328.75, 5.43) in the cove.</summary>
        public static readonly Vector3 Hull = new Vector3(-1748f, 5328.75f, 5.43f);

        /// <summary>Hull extents, from h4_islandx_yacht_03.ymap.</summary>
        public static readonly Vector3 HullMin = new Vector3(-1810.1f, 5312.54f, -4.57f);
        public static readonly Vector3 HullMax = new Vector3(-1687.87f, 5349.36f, 16.93f);

        /// <summary>How far the hull reaches below the surface.</summary>
        public const float Keel = -4.57f;
        /// <summary>The stern platform, a step above the water: where a swimmer comes aboard.</summary>
        public const float WaterlineDeck = 1.2f;
        /// <summary>The interior's main deck floor, from the placed interior's own height.</summary>
        public const float DeckFloor = 4.89f;
        /// <summary>The top of the interior: three deck levels fit under this.</summary>
        public const float InteriorTop = 14.76f;
        /// <summary>The highest hull geometry, which is what an aircraft has to clear.</summary>
        public const float HullTop = 16.93f;

        /// <summary>
        /// The seabed under the cove, from two stock underwater pieces 30 to 60 meters
        /// out. An estimate from nearby geometry, not a probe at the dive point: the
        /// mission still asks the water itself before it puts a submarine anywhere.
        /// </summary>
        public const float SeabedEstimate = -10.4f;
        /// <summary>Working depth for the dive: under the keel, above that seabed.</summary>
        public const float DiveDepth = 8f;
        /// <summary>Room the Kraken needs between the keel and its work point.</summary>
        public const float SubClearance = 3f;

        /// <summary>How far above an authored deck point the probe starts looking down.</summary>
        public const float DeckHeadroom = 2.5f;

        /// <summary>
        /// An authored deck point moved onto the deck that is actually there.
        ///
        /// Every vertical number here was read out of the archives, but which of the
        /// vessel's levels a given standing point belongs to was a judgment, and Ron
        /// found the first one wrong by playing it: M45's helipad marker floated above
        /// the surface he was standing on, so the zone under it never registered no
        /// matter where he stood. The geometry knows where its own deck is. Ask it.
        ///
        /// Searched downward from a little above the authored point rather than from the
        /// hull top, so a point under the superstructure finds the deck he walks on and
        /// not the roof over it. If nothing answers — the vessel has not streamed, or a
        /// chapter is being run alone in QA with no structure at all — the authored
        /// height stands and the log says so.
        /// </summary>
        /// <param name="attempts">
        /// How many frames the probe may wait for collision. The first probe on a site pays
        /// for the streaming; every probe after it should pass 1, because each waiting probe
        /// stalls the script for up to a second and a Setup that stalls is a Setup with an
        /// unmanned helicopter in the air.
        /// </param>
        public static Vector3 OnDeck(Vector3 authored, string what, int attempts = 8) =>
            Core.MissionSites.OnSurface(authored, DeckHeadroom, WaterlineDeck - 1f, what, attempts);

        /// <summary>True for a position that is genuinely on the structure rather than beside it.</summary>
        public static bool Aboard(Vector3 point) =>
            point.Z >= WaterlineDeck && point.X >= HullMin.X - 6f && point.X <= HullMax.X + 6f &&
            point.Y >= HullMin.Y - 6f && point.Y <= HullMax.Y + 6f;
    }

    /// <summary>
    /// The Paleto operation as its chapters see it, the way <c>PortHeist</c> serves
    /// the four harbor chapters. A chapter run on its own in QA has no world, so
    /// every accessor tolerates null rather than requiring the parent.
    /// </summary>
    public static class Paleto
    {
        public static OperationSpec Operation => MissionOperations.Paleto;
        public static PaletoWorld Of(MissionContext context) => context?.Operation as PaletoWorld;
        public static bool IsContinuing(MissionContext context) => Of(context)?.Continuing == true;
        public static void StageCargo(MissionContext context, string key, string destination) =>
            Of(context)?.StageCargo(key, destination);
        /// <summary>
        /// Hold the vessel's interior open. Safe to call every frame and safe with no
        /// parent world, so a chapter opened alone in QA does not have to know.
        /// </summary>
        public static bool EnsureInside(MissionContext context) => Of(context)?.Inside.Ensure() == true;
        /// <summary>Whether the interior the game loaded actually covers a point.</summary>
        public static bool InsideCovers(MissionContext context, Vector3 point) =>
            Of(context)?.Inside.Covers(point) == true;
        /// <summary>
        /// Ask the geometry check what it thinks of the authored points a chapter
        /// stands on, and record each answer under the running mission.
        ///
        /// These points are estimates derived from verified hull extents, not
        /// surveys, so a refusal is a named warning rather than a refused mission: a
        /// playtester reads the key in the mission doctor and fixes it with F11. A
        /// diagnostic must never be the reason a mission cannot start, so a probe
        /// that throws is reported and swallowed.
        /// </summary>
        public static void Review(MissionContext context, params Core.PlacementContract[] contracts)
        {
            if (context?.Locations == null || contracts == null) return;
            foreach (var contract in contracts)
            {
                if (contract == null) continue;
                try { contract.Inspect(context.Locations, context.Doctor); }
                catch (System.Exception ex)
                {
                    context.Doctor?.Warn("placement", contract.Key, "the check itself could not run: " + ex.Message);
                    Core.Logger.Warn("Paleto placement review failed for " + contract.Key + ": " + ex.Message);
                }
            }
        }

        public static string CargoAt(MissionContext context, string key)
        {
            var world = Of(context);
            return world != null ? world.CargoAt(key) : context?.State?.CargoAt(key);
        }
    }
}
