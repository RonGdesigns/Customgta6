using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

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
            Logger.Info("MazeBank: opened " + (ipl ?? "a stock interior") + " at " + target + ".");
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
            // The full spread first, then half of it. A small room refuses a point eight
            // meters out that it would take at four, and every refusal used to land on the
            // arrival point - which is how a whole detail ended up standing on the man it
            // was meant to be fighting (Ron, September 22).
            foreach (float reach in new[] { spread, spread * 0.5f })
            {
                var candidate = arrival + new Vector3((float)Math.Cos(radians) * reach, (float)Math.Sin(radians) * reach, 0f);
                var safe = World.GetSafeCoordForPed(candidate, false, 0);
                bool usable = safe != Vector3.Zero && Math.Abs(safe.Z - arrival.Z) < 4f && safe.DistanceTo(arrival) < spread * 2.5f;
                if (usable) return safe;
            }
            Logger.Warn(what + ": no walkable floor within " + spread + " m at " + degrees + " degrees; using the arrival point.");
            return arrival;
        }

        /// <summary>How far from the player a brother brought up with him is put.</summary>
        public const float AlongsideMeters = 2.5f;

        /// <summary>
        /// Put a brother who is not the player on a floor the player has just reached. The
        /// access service moves the player alone; everybody else is still standing where the
        /// mission began, which for M64 and M65 is the plaza two hundred meters below.
        /// Returns where he was put, or null when there was nobody to move.
        /// </summary>
        public static Vector3? BringAlongside(MissionContext context, CrewSlot slot, Vector3 arrival, double degrees, string what)
        {
            var ped = context?.Crew?.PedFor(slot);
            if (ped == null || !ped.Exists() || ped.IsDead) return null;
            var spot = Nearby(arrival, degrees, AlongsideMeters, what);
            // Immediately, which also takes him out of any seat he was in.
            ped.Task.ClearAllImmediately();
            ped.Position = spot;
            // What the access service does for the player once he is moved: let the engine
            // work out which room of the interior he is now standing in.
            Function.Call(Hash.CLEAR_ROOM_FOR_ENTITY, ped);
            Logger.Info(what + ": " + slot + " is on the floor at " + spot + ".");
            return spot;
        }
    }

    /// <summary>
    /// Going up into an interior, frame by frame, and back out again.
    ///
    /// <see cref="MazeBank.Enter"/> only starts the access service's move: the fade, the IPL,
    /// the room pin and the collision wait all happen over the frames after it, and the host
    /// does not tick the mission while they do. M64 and M65 read the player's position in the
    /// same call that asked for the floor, got the plaza, and laid the whole floor out on it -
    /// defenders, lift, Vance and his terminal on the deck while Ice alone went two hundred
    /// meters up (Ron, September 22). SM07 and SM08 skipped the service altogether, dropped a
    /// man into the MLO and placed everybody in the same frame, before the floor had streamed.
    ///
    /// The order that works: ask, wait for the service to report the player inside and no
    /// longer busy, give him a moment to land and the floor a moment to stream, and only then
    /// lay anything out, from where he actually stands.
    /// </summary>
    public sealed class FloorEntry
    {
        /// <summary>
        /// How long after the service reports the room ready before anybody is placed. The
        /// player is released at the MLO's own origin and drops onto the floor, and the
        /// walkable-floor query <see cref="MazeBank.Nearby"/> relies on answers only once the
        /// navmesh around him has streamed in.
        /// </summary>
        public const int SettleMs = 1500;

        public enum Phase { Waiting, Riding, Settling, Ready, Leaving, Left, Refused }

        private int _arrivedAt;
        private bool _opened;

        public Phase State { get; private set; } = Phase.Waiting;
        /// <summary>Where the player stands once the floor is ready. Zero before that.</summary>
        public Vector3 Arrival { get; private set; }
        /// <summary>Why the floor could not be reached or left, for the mission to fail with.</summary>
        public string Failure { get; private set; }
        public bool Ready => State == Phase.Ready;
        public bool Refused => State == Phase.Refused;
        public bool Left => State == Phase.Left;

        /// <summary>Ask for the floor. False, with <see cref="Failure"/> set, if the service refused.</summary>
        public bool Request(MissionContext context, Vector3 target, string ipl)
        {
            if (State != Phase.Waiting) return State != Phase.Refused;
            string failure;
            if (!MazeBank.Enter(context, target, ipl, out failure)) { Refuse(failure); return false; }
            _opened = true;
            State = Phase.Riding;
            return true;
        }

        /// <summary>
        /// Call every mission tick. True on the one tick the floor is ready to be laid out,
        /// and on the one tick the player is back outside after <see cref="RequestExit"/>.
        /// </summary>
        public bool Update(MissionContext context)
        {
            var access = context?.Interior;
            switch (State)
            {
                case Phase.Riding:
                    if (access == null) { Refuse("The tower interior service is missing. See Bloodlines.log."); return false; }
                    // In the game the host drives the service and holds the mission while it is
                    // busy, so this only matters where nothing else drives it. M23 does the same.
                    if (access.Busy) { access.Update(); if (access.Busy) return false; }
                    if (!access.Inside)
                    {
                        Refuse("The floor did not finish loading and you were put back where you started. Retry the mission.");
                        return false;
                    }
                    _arrivedAt = Game.GameTime;
                    State = Phase.Settling;
                    Logger.Info("MazeBank: the access service reports the floor ready; settling before anything is placed.");
                    return false;
                case Phase.Settling:
                    if (Game.GameTime - _arrivedAt < SettleMs) return false;
                    var player = Game.Player.Character;
                    Arrival = player != null && player.Exists() ? player.Position : access != null ? access.InteriorPosition : Vector3.Zero;
                    State = Phase.Ready;
                    Logger.Info("MazeBank: on the floor at " + Arrival + ".");
                    return true;
                case Phase.Leaving:
                    if (access == null) { Refuse("The tower interior service is missing. See Bloodlines.log."); return false; }
                    if (access.Busy) { access.Update(); if (access.Busy) return false; }
                    if (access.Inside)
                    {
                        Refuse("The way down did not finish loading. Retry the mission.");
                        return false;
                    }
                    _opened = false;
                    State = Phase.Left;
                    Logger.Info("MazeBank: back at street level.");
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Out through the service's own exit, with its fade and its collision wait, to where
        /// the player stood when he went in.
        /// </summary>
        public bool RequestExit(MissionContext context)
        {
            var access = context?.Interior;
            if (State != Phase.Ready || access == null || access.Busy || !access.Inside) return false;
            if (!access.Begin(access.ExitPosition, null, false)) { Refuse("The way down was refused. Retry the mission."); return false; }
            State = Phase.Leaving;
            return true;
        }

        /// <summary>
        /// On every exit path. A floor this entry opened and never left is handed back, the
        /// player with it, and anybody brought up with him is put beside him on the street:
        /// these floors have no door, and a pass, a failure and an abort all ended with
        /// somebody still standing in one otherwise.
        /// </summary>
        public void Release(MissionContext context, IEnumerable<Ped> others)
        {
            var access = context?.Interior;
            if (_opened && access != null && (access.Inside || access.Busy))
            {
                var exit = access.ExitPosition;
                try { access.Cancel(); }
                catch (Exception ex) { Logger.Error("MazeBank: handing the floor back", ex); }
                int i = 0;
                foreach (var ped in others ?? Enumerable.Empty<Ped>())
                {
                    var player = Game.Player.Character;
                    if (ped == null || !ped.Exists() || ped.IsDead || (player != null && ped.Handle == player.Handle)) continue;
                    try
                    {
                        i++;
                        Function.Call(Hash.REQUEST_COLLISION_AT_COORD, exit.X, exit.Y, exit.Z);
                        ped.Position = exit + new Vector3(i * 1.5f, i % 2 * 1.5f, 0f);
                    }
                    catch (Exception ex) { Logger.Error("MazeBank: bringing a brother back down", ex); }
                }
                Logger.Info("MazeBank: the floor is handed back and everybody is at " + exit + ".");
            }
            _opened = false;
            State = Phase.Waiting;
            Arrival = Vector3.Zero;
            Failure = null;
        }

        private void Refuse(string failure)
        {
            Failure = failure;
            State = Phase.Refused;
            Logger.Error("MazeBank: " + failure);
        }
    }
}
