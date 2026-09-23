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
    /// M61 — "The Black Box". The port basin off Terminal Island.
    ///
    /// An Aegis command gunship went into the water during the port fighting. Gohan goes down
    /// to the flooded bridge and cuts the tactical server out of it before their frogmen can
    /// scuttle the wreck.
    ///
    /// The basin is real and measured: the quay edge runs along y = -2899 with
    /// <c>prop_dock_moor_04</c> and <c>prop_dock_moor_01</c> from x 966 to 1225, and the
    /// container stacks start at x 958. West of that is open water, which is where the wreck
    /// is. Its depth is checked at runtime, because a water key that is dry is a mission that
    /// cannot start — that check is why M13 and M57 have one.
    ///
    /// **The wreck is a helicopter the mission puts in the water, not a modeled hulk.** There
    /// is no half-submerged gunship in the archives. A <c>maverick</c> placed on the seabed,
    /// dead and undriveable, is the honest version: it is the right object, it is where the
    /// synopsis says, and the server comes out of it. Recorded in
    /// `data/mission_gameplay.tsv`.
    ///
    /// This mission leans on the dive machinery M36 to M43 already built rather than inventing
    /// a second one.
    /// </summary>
    public sealed class M61TheBlackBox : PreparationOperation
    {
        public const string WreckModel = "maverick";
        public const string DiverModel = "s_m_y_blackops_01";
        /// <summary>Aegis demolition frogmen coming for the wreck.</summary>
        public const int Frogmen = 4;
        /// <summary>How long the server takes to cut out.</summary>
        public const int CutSeconds = 14;
        /// <summary>How deep the water has to be before anything is put in it.</summary>
        public const float RequiredDepth = 6f;
        /// <summary>Water a diver is put into has to be at least this deep under him.</summary>
        public const float DiverDepth = 1.5f;
        /// <summary>How long a frogman may stay under. The default is a drowning in a minute.</summary>
        public const float DiverAirSeconds = 600f;
        /// <summary>The swim task's speed: a diver making way, not a man treading water.</summary>
        public const float DiverSpeed = 2f;
        /// <summary>Where the campaign records the access codes are the crew's.</summary>
        public const string ServerEvidence = "aegisCommandServer";
        /// <summary>How far above the measured seabed the wreck is created, so it settles rather than clips.</summary>
        public const float WreckClearance = 1.5f;
        /// <summary>
        /// How long Gohan can stay down with the dive gear on. The cut alone is fourteen
        /// seconds at the wreck, after a swim down to it; a player's own breath does not cover
        /// that, and nothing gave him more (Ron, September 22).
        /// </summary>
        public const float GohanAirSeconds = 600f;
        /// <summary>How near the wreck the cut can be worked from. Underwater a swimmer drifts.</summary>
        public const float CutRadius = 6f;

        private readonly List<Ped> _frogmen = new List<Ped>();
        private Vehicle _wreck;
        private bool _down, _cut, _clear;

        public override string Id => "M61";
        public override string Title => "The Black Box";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;

        /// <summary>Gohan has reached the wreck.</summary>
        public bool Down => _down;
        /// <summary>The server is out.</summary>
        public bool Cut => _cut;
        /// <summary>The frogmen are dealt with.</summary>
        public bool Clear => _clear;
        public Vehicle Wreck => _wreck;
        public IReadOnlyList<Ped> Divers => _frogmen;

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Gohan)) return false;

            // A water key that is dry is a mission that cannot start.
            if (!MissionSites.Water(Ctx.Locations, "M61.Wreck", "M61.Surface",
                    "M61.Diver1", "M61.Diver2", "M61.Diver3", "M61.Diver4"))
            {
                GameUtils.Notify("~r~The port basin did not check out. See Bloodlines.log.");
                return false;
            }

            // ResolveOrThrow answers with the water's surface, not the bottom: the wreck used to
            // be created floating there and left to sink on its own. It goes on the seabed the
            // probe measured, and stays at the surface to sink only where no bottom was seen.
            var water = MarineSites.ResolveOrThrow(Ctx.Locations, "M61.Wreck", RequiredDepth);
            var column = MarineSites.Native.Column(water);
            var seabed = column.Known && !column.Assumed && column.HasDepth(RequiredDepth)
                ? new Vector3(water.X, water.Y, column.Floor + WreckClearance)
                : water;
            if (seabed == water) Logger.Warn(Id + ": no seabed was measured under " + water + "; the wreck is left to sink from the surface.");
            _wreck = Car(WreckModel, seabed, Ctx.Locations.Heading("M61.Wreck"), false);
            if (!RequireAssets(_wreck)) return false;
            _wreck.IsPersistent = true;
            // Dead where it lies. A driveable helicopter on a seabed is a helicopter that
            // tries to fly, and this one has been shot down.
            _wreck.IsEngineRunning = false;
            _wreck.IsDriveable = false;
            var blip = Track(_wreck.AddBlip());
            if (blip != null) { blip.Color = BlipColor.Blue; blip.Name = "Aegis command gunship"; }

            for (int i = 1; i <= Frogmen; i++)
            {
                string key = "M61.Diver" + i;
                // In the water, not on the quay. Enemy(key) asks the engine for walkable ground
                // and over the basin that answer is the dock within thirty-five meters: four
                // demolition divers standing on the moorings with rifles. The key is resolved
                // to the water surface the way the wreck's is, and the man is created there
                // and told to make for the wreck under his own power.
                Vector3 surface;
                try { surface = MarineSites.ResolveOrThrow(Ctx.Locations, key, DiverDepth, 1f, 1f); }
                catch (InvalidOperationException ex)
                { Logger.Warn(Id + ": " + key + " did not resolve to water: " + ex.Message); continue; }
                var ped = EnemyAt(surface, key);
                if (ped == null) continue;
                Function.Call(Hash.SET_PED_MAX_TIME_UNDERWATER, ped, DiverAirSeconds);
                var wreckAt = _wreck.Position;
                Function.Call(Hash.TASK_GO_TO_COORD_ANY_MEANS, ped, wreckAt.X, wreckAt.Y, wreckAt.Z,
                    DiverSpeed, 0, false, 786603, -1f);
                _frogmen.Add(ped);
            }
            if (_frogmen.Count == 0)
            {
                // An unopposed dive is not this mission. Four keys all refusing means the basin
                // did not stream, which is a retry rather than a walk-through.
                Logger.Error(Id + ": no Aegis frogman could be put in the water.");
                GameUtils.Notify("~r~The Aegis divers could not be placed. See Bloodlines.log.");
                return false;
            }

            Establish("approach", "Whatever is still on that bridge",
                "The command gunship went into the basin with its tactical server aboard. Aegis has demolition divers on the way to scuttle it, which means the crew has until they get there.",
                _wreck);
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Get down to the wreck",
                new ReachZoneObjective("Gohan: dive to the flooded bridge",
                    () => _wreck != null && _wreck.Exists() ? _wreck.Position : At("M61.Wreck"), 8f))
                .OwnedBy(CrewSlot.Gohan)
                .OnEnter(c => DiveGear(true))
                .OnExit(c => _down = true);

            // No reach-inside animation: it is a standing pose, and played on a swimmer it
            // takes him out of the swim at the bottom of the basin.
            yield return new MissionStage("Cut the server out",
                new MissionInteraction("Gohan: cut the command server out of the chassis",
                    () => _wreck != null && _wreck.Exists() ? _wreck.Position : At("M61.Wreck"),
                    CutSeconds, CutRadius)
                { RequiredCharacter = CrewSlot.Gohan })
                .OnEnter(c => Fighting = true)
                .OnExit(c => Cutting())
                .AfterCues("M61_S1_01_GOHAN");

            yield return new MissionStage("Deal with their divers",
                new KillTargetsObjective("Take the Aegis demolition divers", () => _frogmen))
                .AnyBrother()
                .OnExit(c => _clear = true)
                .AfterCues("M61_S1_02_ICE");

            yield return new MissionStage("Surface with it",
                new TravelObjective("Bring the server up to the surface by the quay", () => At("M61.Surface"), 12f))
                .AnyBrother()
                .AfterCues("M61_S1_03_GOHAN");
        }

        private void Cutting()
        {
            _cut = true;
            Ctx.State?.SetEvidence(ServerEvidence, EvidenceState.CopyHeld);
            Logger.Info(Id + ": the tactical command server is out, with the Maze Bank access codes on it.");
        }

        /// <summary>
        /// Gohan's dive gear: the scuba set and the air to go with it. Taken off again on every
        /// exit path. The longer air is left with him - the engine has no getter for what he had
        /// before, and guessing a number to write back would be worse than a generous lung.
        /// </summary>
        private void DiveGear(bool on)
        {
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            if (gohan == null || !gohan.Exists()) return;
            try
            {
                Function.Call(Hash.SET_ENABLE_SCUBA, gohan, on);
                if (on) Function.Call(Hash.SET_PED_MAX_TIME_UNDERWATER, gohan, GohanAirSeconds);
            }
            catch (Exception ex) { Logger.Error(Id + ": " + (on ? "fitting" : "removing") + " Gohan's dive gear", ex); }
        }

        protected override void OnCleanup()
        {
            DiveGear(false);
            base.OnCleanup();
        }

        protected override void OnPassed()
        {
            if (!_down || !_cut || !_clear)
                throw new InvalidOperationException("The dive, the server and the divers all have to be settled.");
        }
    }
}
