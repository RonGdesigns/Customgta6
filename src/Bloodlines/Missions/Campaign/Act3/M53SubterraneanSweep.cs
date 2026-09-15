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
    /// M53 — "Subterranean Sweep". The metro tunnel under Downtown, 03:00, in the dark.
    ///
    /// Aegis puts two night-vision sweep teams into the tunnels to find the crew's
    /// underground routes. The crew is already down there: tripwires along a stalled train,
    /// goggles on, and eight contractors walking into it.
    ///
    /// This mission was written off as unbuildable, twice, and the note in CLAUDE.md said so:
    /// "not one placed entity exists below z = 0 under Pillbox Hill". That was true and it was
    /// the wrong question. Downtown street level is around z 31, and the metro runs at
    /// **z 13** — twenty meters under the street and eighteen meters above the sea. Searching
    /// below zero found nothing because there is nothing below zero; the tunnel was never
    /// hidden.
    ///
    /// What is actually there, straight out of the archives, is a complete line:
    ///
    ///   metro_station_3_seoul   (-497.73, -673.53, 13.64)   the platform
    ///   metro_stat3join1        (-437.69, -675.41, 13.64)   where the platform becomes tunnel
    ///   metro_t_join            (-415.17, -673.07, 13.03)   tunnel
    ///   metro_topen_step        (-397.67, -673.07, 13.03)   tunnel
    ///   metro_t_join            (-380.17, -673.07, 13.03)   tunnel, where the train stalls
    ///   metro_t_step_20         (-364.10, -675.01, 13.03)   tunnel
    ///   metro_t_stair           (-341.91, -682.70, 13.03)   the bend east
    ///   metro_newwalk1          (-470.15, -714.52, 22.51)   the station walkway up
    ///   kt1_09_seoul_subway     (-490.29, -714.59, 25.97)   the entrance structure
    ///
    /// So the descent, the platform, ninety meters of straight tunnel and the way out are all
    /// real geometry, and the fight has somewhere to happen. The tunnel is in **Downtown** and
    /// the station end is in **Little Seoul**; the bible says Pillbox Hill, which is the
    /// Downtown core, and the district text in `locations.tsv` is machine-read, so it names
    /// the zones the game actually has.
    ///
    /// Two things this mission does that nothing underground could do before it:
    ///
    /// **Heights come from one probe, not seventeen.** Every tunnel model origin on this run
    /// sits at 13.03, so the floor is flat along it. One downward probe at the train measures
    /// the real slab, and the offset it finds is applied to every other point. Seventeen
    /// separate probes would each be a chance to fail and would spend seconds of waiting in
    /// Setup; one measurement of a flat floor is the honest model of it.
    ///
    /// **A contractor down here is placed, not snapped.** `Guard` asks the engine for walkable
    /// ground and accepts an answer up to 35 meters away. That is fine beside a jetty. Twenty
    /// meters below Vespucci Boulevard it returns the boulevard, well inside the tolerance, and
    /// all eight contractors would have spawned in traffic. `EnemyAt` skips the query for a
    /// point the caller has already settled, and every tunnel key is a fixed surface for the
    /// same reason.
    ///
    /// The authored line says a sweep team of eight, so there are eight: two squads of four.
    /// Nobody has walked this tunnel with an F11 capture yet; the train and the crew are
    /// offset three meters apart across the bore on the assumption that a two-track tunnel has
    /// room for both, which is the one number here that is a judgment rather than a reading.
    /// </summary>
    public sealed class M53SubterraneanSweep : PreparationOperation
    {
        /// <summary>The stalled carriage. RAIL class, so it is created and left sitting.</summary>
        public const string TrainModel = "metrotrain";
        /// <summary>Four and four, because M53_S1_01_GOHAN says a sweep team of eight.</summary>
        public const int SquadSize = 4;
        /// <summary>How long a tripwire takes to set.</summary>
        public const int TripwireSeconds = 4;
        /// <summary>Proximity mines each brother carries out of this, on top of the three set.</summary>
        public const int MineRounds = 4;
        /// <summary>How far above and below the authored tunnel height the slab is looked for.</summary>
        public const float TunnelHeadroom = 5f;
        public const float TunnelFloor = 8f;
        /// <summary>How often a sweeper's walk order is refreshed. Never every frame.</summary>
        public const int SweepOrderMs = 6000;
        /// <summary>How near the train a sweeper gets before the ambush is sprung.</summary>
        public const float ContactMeters = 26f;
        /// <summary>Who holds the goggles, and the cargo flag for routes that stayed secret.</summary>
        public const string Goggles = "M53 tunnel sweep";
        public const string RoutesCargo = "tunnelRoutesHeld";

        private static readonly string[] MineKeys = { "M53.Mine1", "M53.Mine2", "M53.Mine3" };

        private readonly List<Ped> _sweepers = new List<Ped>();
        private Vehicle _train;
        private float _floorOffset;
        private bool _wired, _cleared;
        private int _orderAt;

        public override string Id => "M53";
        public override string Title => "Subterranean Sweep";
        protected override MissionEndpoint Endpoint => MissionEndpoint.EscapeCheckpoint;

        /// <summary>The tripwires are set along the stalled carriage.</summary>
        public bool Wired => _wired;
        /// <summary>Both squads are down.</summary>
        public bool Cleared => _cleared;
        /// <summary>How far the probe moved the authored tunnel height.</summary>
        public float FloorOffset => _floorOffset;
        public Vehicle Train => _train;
        public IReadOnlyList<Ped> Sweepers => _sweepers;

        /// <summary>Every tunnel key. The street is twenty meters up, and that is what the
        /// engine's walkable query would answer with for any one of them.</summary>
        protected override string[] FixedSurfaces =>
            new[] { "M53.Start", "M53.IceStart", "M53.GohanStart", "M53.GuessStart",
                    "M53.Train", "M53.Exit" }
                .Concat(MineKeys)
                .Concat(SquadKeys())
                .ToArray();

        private static IEnumerable<string> SquadKeys()
        {
            for (int i = 1; i <= SquadSize; i++) yield return "M53.SquadA" + i;
            for (int i = 1; i <= SquadSize; i++) yield return "M53.SquadB" + i;
        }

        /// <summary>An authored tunnel point, moved onto the floor the probe actually found.</summary>
        private Vector3 Down(string key) => At(key) + new Vector3(0f, 0f, _floorOffset);

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Ice)) return false;

            // One measurement of a flat floor. Every tunnel section along this run has its
            // origin at 13.03, so the difference between that and the slab is the same
            // difference everywhere, and finding it once costs one probe instead of
            // seventeen chances to fail.
            var authored = At("M53.Train");
            _floorOffset = MissionSites.OffsetToSurface(authored, TunnelHeadroom, TunnelFloor, Id + " tunnel floor");
            Logger.Info(Id + ": the tunnel floor is " + (authored.Z + _floorOffset).ToString("0.00") +
                " against the authored " + authored.Z.ToString("0.00") +
                "; every tunnel point moves by " + _floorOffset.ToString("0.00") + ".");

            // The stalled carriage. A train that will not create is a thinner set piece, not
            // a dead mission: the ambush is a tunnel fight with or without it, and refusing
            // to start over scenery is the mistake M32 and M39 made over a single guard.
            _train = Car(TrainModel, Down("M53.Train"), Ctx.Locations.Heading("M53.Train"), false);
            if (_train == null || !_train.Exists())
                Logger.Warn(Id + ": the stalled carriage could not be created; the tunnel fight runs without it.");
            else
            {
                _train.IsPersistent = true;
                _train.IsEngineRunning = false;
                Logger.Info(Id + ": the stalled carriage is in the tunnel at " + _train.Position + ".");
            }

            foreach (var key in SquadKeys())
            {
                var ped = EnemyAt(Down(key), key);
                if (ped != null) _sweepers.Add(ped);
            }
            if (_sweepers.Count == 0)
            {
                Logger.Error(Id + ": no contractor could be placed in the tunnel; there is nothing to ambush.");
                GameUtils.Notify("~r~The sweep teams could not be placed. See Bloodlines.log.");
                return false;
            }

            // Mines they can actually throw, on top of the three the stage sets. The loan is
            // opened and closed by MissionManager, so these go back at teardown.
            foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Gohan })
            {
                var ped = Ctx.Crew.PedFor(slot);
                if (ped != null && ped.Exists()) ped.Weapons.Give(WeaponHash.ProximityMine, MineRounds, false, true);
            }

            Paleto.Review(Ctx, PlacementContract.Ped("M53.Start"), PlacementContract.Ped("M53.Exit"),
                PlacementContract.Interaction("M53.Mine1"), PlacementContract.Interaction("M53.Mine2"),
                PlacementContract.Interaction("M53.Mine3"));

            Establish("approach", "Track two, three in the morning",
                "Aegis has two teams in the tunnel looking for the way the crew moves under the city. The crew is already past them, at the stalled carriage: tripwires on the doors, goggles down, and eight men walking east into it.",
                _train);
            return true;
        }

        /// <summary>One tripwire, reading its position when it is drawn rather than at
        /// construction: the floor offset is not known until Setup has probed for it.</summary>
        private Objective Wire(string label, string key, CrewSlot owner) =>
            new MissionInteraction(label, () => Down(key), TripwireSeconds, 2.5f,
                animation: MissionInteraction.ReachInside) { RequiredCharacter = owner };

        private Vector3 TrainPoint() =>
            _train != null && _train.Exists() ? _train.Position : Down("M53.Train");

        protected override IEnumerable<MissionStage> BuildStages()
        {
            // The synopsis gives the mines to Ice and Gohan by name, so they are named: two
            // for Ice and the doors for Gohan. Parallel objectives with different owners in
            // one stage, which is M51's arrangement — the dispatcher only demands a switch
            // once the brother the player holds has nothing left, so all three are open in
            // any order, and each brother sees only his own marker.
            var wires = new[]
            {
                Wire("Ice: set a tripwire at the west end of the carriage", MineKeys[0], CrewSlot.Ice),
                Wire("Gohan: set a tripwire on the carriage doors", MineKeys[1], CrewSlot.Gohan),
                Wire("Ice: set a tripwire at the east end of the carriage", MineKeys[2], CrewSlot.Ice),
            };

            yield return new MissionStage("Set the tripwires", wires)
                .OnEnter(c => NightVision.Wear(Goggles))
                .OnExit(c => { _wired = true; _orderAt = 0; })
                .WithCues("M53_S1_01_GOHAN")
                .AfterCues("M53_S1_02_ICE");

            yield return new MissionStage("Clear the tunnel",
                new KillTargetsObjective("Take both sweep teams in the tunnel", () => _sweepers))
                .AnyBrother()
                .OnExit(c => _cleared = true)
                .AfterCues("M53_S1_03_ICE");

            // Out the way they came in. metro_newwalk1 is the station walkway between the
            // platform and the street, so this is a real place rather than a point in a wall.
            yield return new MissionStage("Out through the station",
                new ReachZoneObjective("Get up onto the station walkway and out", () => Down("M53.Exit"), 6f))
                .AnyBrother();
        }

        /// <summary>
        /// The sweep. They walk east toward the carriage until one of them is close enough for
        /// the tripwires to matter, and then it is a fight.
        ///
        /// The order is refreshed on a six-second cadence and never every frame: handing a ped
        /// a fresh task each tick restarts it before he can act on it, which is what left the
        /// guards in M31, M33 and M37 standing still.
        /// </summary>
        private void Sweep()
        {
            var target = TrainPoint();
            if (!Fighting && Game.GameTime >= _orderAt)
            {
                _orderAt = Game.GameTime + SweepOrderMs;
                foreach (var ped in _sweepers)
                {
                    if (ped == null || !ped.Exists() || ped.IsDead) continue;
                    if (ped.IsInCombat) continue;
                    ped.Task.GoTo(target);
                }
            }
            if (Fighting) return;
            bool contact = _sweepers.Any(p => p != null && p.Exists() && !p.IsDead &&
                p.Position.DistanceTo(target) < ContactMeters);
            if (!contact) return;
            Fighting = true;
            Awareness.ReportToAll(Stimulus.RadioCall, target);
            Logger.Info(Id + ": the lead sweeper reached the carriage; the ambush is sprung.");
        }

        protected override void OnUpdate()
        {
            if (_wired && !_cleared) Sweep();
            base.OnUpdate();
        }

        protected override void OnCleanup()
        {
            // Goggles up on every exit — pass, failure, abort and death all come through here.
            // Night vision left on is a green screen the player cannot clear from any menu.
            NightVision.Remove(Goggles);
            _orderAt = 0;
            base.OnCleanup();
        }

        protected override void OnPassed()
        {
            if (!_wired || !_cleared)
                throw new InvalidOperationException("The tripwires have to be set and both squads down.");
            Ctx.State?.SetCargo(RoutesCargo, "M53.Train");
            Logger.Info(Id + ": both sweep teams are down; the underground routes are still the crew's.");
            Release(_train);
        }
    }
}
