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
        /// <summary>Where the campaign records the access codes are the crew's.</summary>
        public const string ServerEvidence = "aegisCommandServer";

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
            if (!MissionSites.Water(Ctx.Locations, "M61.Wreck", "M61.Surface"))
            {
                GameUtils.Notify("~r~The port basin did not check out. See Bloodlines.log.");
                return false;
            }

            var seabed = MarineSites.ResolveOrThrow(Ctx.Locations, "M61.Wreck", RequiredDepth);
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
                var ped = Enemy("M61.Diver" + i);
                if (ped != null) _frogmen.Add(ped);
            }
            if (_frogmen.Count == 0)
                Logger.Warn(Id + ": no Aegis frogman could be placed; the dive is unopposed.");

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
                .OnExit(c => _down = true);

            yield return new MissionStage("Cut the server out",
                new MissionInteraction("Gohan: cut the command server out of the chassis",
                    () => _wreck != null && _wreck.Exists() ? _wreck.Position : At("M61.Wreck"),
                    CutSeconds, 4f, animation: MissionInteraction.ReachInside)
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
                new TravelObjective("Bring the server up to the quay", () => At("M61.Surface"), 12f))
                .AnyBrother()
                .AfterCues("M61_S1_03_GOHAN");
        }

        private void Cutting()
        {
            _cut = true;
            Ctx.State?.SetEvidence(ServerEvidence, EvidenceState.CopyHeld);
            Logger.Info(Id + ": the tactical command server is out, with the Maze Bank access codes on it.");
        }

        protected override void OnPassed()
        {
            if (!_down || !_cut || !_clear)
                throw new InvalidOperationException("The dive, the server and the divers all have to be settled.");
        }
    }
}
