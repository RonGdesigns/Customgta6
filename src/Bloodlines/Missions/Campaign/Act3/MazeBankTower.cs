using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// The Maze Bank Tower, which turns out to be a walkable building from the plaza to the
    /// roof. M63 to M66 all happen in it, and this is the one place that records what is
    /// actually there, so four missions do not each re-derive it.
    ///
    /// The campaign had written these off, because an earlier pass established that the mod's
    /// MP apartment tiers all resolve to a single interior location and concluded that
    /// downtown interiors were therefore unavailable. That was true of those apartment tiers
    /// and false of everything else. The executive offices and the tower garages are placed at
    /// their own buildings, each with its own IPL:
    ///
    ///   hei_dt1_11_carpark        (-84.13, -821.35,  36.71)  the ground carpark
    ///   imp_dt1_11_cargarage_a    (-84.22, -823.09, 221.00)  a mid-tower floor
    ///   ex_dt1_11_office_01a      (-73.80, -818.96, 242.39)  the executive floor
    ///   imp_dt1_11_modgarage      (-73.90, -821.62, 284.00)  an upper floor
    ///   dt1_11_heliport           (-75.20, -818.95, 323.26)  the roof
    ///
    /// with the plaza deck itself at <c>dt1_11_dt1_plaza</c> (-76.6, -825.6, **36.77**), seven
    /// meters above the street around it — which is why every plaza key is a fixed surface.
    ///
    /// Two things the archives settle against the authored text, both recorded in
    /// `data/mission_gameplay.tsv` rather than by editing the extraction:
    ///
    /// **There is no antenna spire.** The tower stops at 325.17, which is a ring of
    /// <c>prop_air_lights_02b</c> on the roof parapet. M66's crew jumps from the roof rather
    /// than climbing a mast. Ice's "pulling chute at eight hundred feet" survives intact: the
    /// roof is 323 m, about 1,060 feet, so opening at 800 is a real number from a real height.
    ///
    /// **The executive floor is 242 m, not the hundredth.** At roughly three meters a floor
    /// that is somewhere around the seventieth. The authored "100th-Floor Boardroom" is the
    /// top of the building in spirit and the office MLO in fact.
    /// </summary>
    public static class MazeBank
    {
        /// <summary>The plaza deck. Seven meters above the street, so never ground-snapped.</summary>
        public const float PlazaHeight = 36.77f;
        /// <summary>How far above and below a plaza key the deck is looked for.</summary>
        public const float PlazaHeadroom = 4f;
        public const float PlazaFloor = 28f;

        /// <summary>A mid-tower floor, and the IPL that loads it.</summary>
        public const string GarageIpl = "imp_dt1_11_cargarage_a";
        public static readonly Vector3 Garage = new Vector3(-84.22f, -823.09f, 221.00f);

        /// <summary>The executive floor, and the IPL that loads it.</summary>
        public const string OfficeIpl = "ex_dt1_11_office_01a";
        public static readonly Vector3 Office = new Vector3(-73.80f, -818.96f, 242.39f);

        /// <summary>The roof, at the helipad this tower really has.</summary>
        public static readonly Vector3 Roof = new Vector3(-75.20f, -818.95f, 323.26f);

        /// <summary>
        /// Open an interior of this tower. Nothing loads an MLO by hand: the access service
        /// owns the fade, the entity sets and the exit, and it is the thing that knows how to
        /// put them back. The IPL goes through <see cref="DlcMaps"/>, because the office and
        /// garage floors are DLC map data and a plain REQUEST_IPL on those quietly does
        /// nothing until the archives are registered.
        /// </summary>
        public static bool Enter(MissionContext context, Vector3 target, string ipl, out string failure)
        {
            failure = null;
            var access = context?.Interior;
            if (access == null)
            {
                failure = "The tower interior service is missing. See Bloodlines.log.";
                Logger.Error("MazeBank: no apartment access service; " + ipl + " cannot be opened.");
                return false;
            }
            DlcMaps.EnsureRegistered();
            if (!access.Begin(target, ipl, true, target, null, new string[0]))
            {
                failure = "The floor did not open. Retry the mission.";
                Logger.Error("MazeBank: the access service refused " + ipl + " at " + target + ".");
                return false;
            }
            Logger.Info("MazeBank: opened " + ipl + " at " + target + ".");
            return true;
        }

        /// <summary>
        /// Somewhere to stand near a point inside an interior nobody has surveyed.
        ///
        /// Exactly one coordinate per floor is authored — the MLO's own placement — so a
        /// position inside it is an offset from where the crew actually ends up, resolved to
        /// walkable floor. A spot the engine refuses falls back to the arrival point, which is
        /// somewhere a man is definitely standing: better a marker in the wrong corner than one
        /// in the air. This is M54's rule, and the reason it exists is a day Ron spent walking
        /// up to markers that were not there.
        /// </summary>
        public static Vector3 Nearby(Vector3 arrival, double degrees, float spread, string what)
        {
            double radians = degrees * Math.PI / 180.0;
            var candidate = arrival + new Vector3((float)Math.Cos(radians) * spread, (float)Math.Sin(radians) * spread, 0f);
            var safe = World.GetSafeCoordForPed(candidate, false, 0);
            bool usable = safe != Vector3.Zero && Math.Abs(safe.Z - arrival.Z) < 4f && safe.DistanceTo(arrival) < spread * 2.5f;
            if (usable) return safe;
            Logger.Warn(what + ": no walkable floor at " + candidate + "; using the arrival point.");
            return arrival;
        }
    }
}
