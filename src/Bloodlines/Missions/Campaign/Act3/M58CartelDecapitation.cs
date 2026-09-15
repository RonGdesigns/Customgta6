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
    /// M58 — "Cartel Decapitation". A Mirror Park cul-de-sac, 02:00, drizzle.
    ///
    /// What is left of the Cifuentes leadership is in one walled block. Guess puts the hauler
    /// through the gate, Gohan drops gas into the roof vents, Ice covers the street with the
    /// LMG.
    ///
    /// **The vents are real and that is what fixes the site.** Gohan's line disperses tear gas
    /// into the HVAC, and the block at (1072–1139, -777 to -784) carries
    /// <c>prop_roofvent_04a</c> at (1072.4, -778.1, 60.34) and (1138.5, -779.7, 63.23) with
    /// <c>prop_aircon_m_03</c> and <c>prop_aircon_s_01a</c> between them. Two roof vents
    /// sixty-six meters apart on a residential block is a compound with an air system, which is
    /// the one thing this mission needs the geography to provide.
    ///
    /// **Everything here is at street level, so ground preparation does its job.** No fixed
    /// surfaces and no probes: Mirror Park is an ordinary neighborhood at z 56 to 63, the
    /// engine's walkable query is right about it, and overriding that would be inventing a
    /// problem. The two vent keys keep their own archive heights because they are roof fittings
    /// and Gohan reaches up to them rather than standing on them.
    ///
    /// **The gate is a road, so it is found rather than written.** Roads are baked terrain —
    /// M49's problem — so the approach lane comes from <c>GameUtils.NearestRoadNode</c> at
    /// runtime with the authored point as the seed.
    /// </summary>
    public sealed class M58CartelDecapitation : PreparationOperation
    {
        public const string HaulerModel = "insurgent";
        public const string CapoModel = "g_m_m_mexboss_01";
        /// <summary>The leadership council. Three capos and the man at the head of the table.</summary>
        public const int Capos = 4;
        /// <summary>Compound security in the cul-de-sac.</summary>
        public const int Sicarios = 6;
        /// <summary>How long a vent takes to gas.</summary>
        public const int GasSeconds = 6;
        /// <summary>How near the gate the hauler counts as through.</summary>
        public const float BreachRadius = 9f;
        /// <summary>How far a road node may be from the seed before it is refused.</summary>
        public const float LaneSearch = 90f;
        /// <summary>Where the campaign records the council is gone.</summary>
        public const string CouncilEvidence = "cifuentesCouncil";

        private static readonly string[] VentKeys = { "M58.Vent1", "M58.Vent2" };

        private readonly List<Ped> _council = new List<Ped>();
        private readonly List<Ped> _security = new List<Ped>();
        private Vehicle _hauler;
        private Vector3 _gate;
        private bool _breached, _gassed, _councilDown;

        public override string Id => "M58";
        public override string Title => "Cartel Decapitation";
        protected override MissionEndpoint Endpoint => MissionEndpoint.EscapeCheckpoint;

        /// <summary>The hauler is through the gate.</summary>
        public bool Breached => _breached;
        /// <summary>Both vents are gassed.</summary>
        public bool Gassed => _gassed;
        /// <summary>The council is down.</summary>
        public bool CouncilDown => _councilDown;
        public Vehicle Hauler => _hauler;
        public IReadOnlyList<Ped> Council => _council;
        public IReadOnlyList<Ped> Security => _security;
        /// <summary>The lane the gate actually resolved to.</summary>
        public Vector3 Gate => _gate;

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Guess)) return false;

            var seed = At("M58.Gate");
            if (GameUtils.NearestRoadNode(seed, LaneSearch, out var node, out float _))
            {
                _gate = node;
                Logger.Info(Id + ": the cul-de-sac gate resolved to a road node at " + node +
                    ", " + (int)node.DistanceTo(seed) + " m from the seed.");
            }
            else
            {
                _gate = seed;
                Ctx.Doctor?.Warn("placement", "M58.Gate", "no road node within " + LaneSearch + " m; using the seed.");
                Logger.Warn(Id + ": no road node near " + seed + "; using the authored seed as the gate.");
            }

            _hauler = Car(HaulerModel, At("M58.Hauler"), Ctx.Locations.Heading("M58.Hauler"), true);
            if (!RequireAssets(_hauler)) return false;
            _hauler.IsPersistent = true;
            RequireAsset(_hauler, "The armored hauler was destroyed before it reached the gate.");

            var model = new Model(CapoModel);
            if (GameUtils.RequestModel(model))
            {
                for (int i = 1; i <= Capos; i++)
                {
                    var ped = Person(CapoModel, "M58.Capo" + i, false);
                    if (ped == null) continue;
                    _council.Add(ped);
                    Opposition.Add(ped);
                    Blips.Attach(ped, BlipColor.Red, "Cifuentes capo");
                }
                model.MarkAsNoLongerNeeded();
            }
            if (_council.Count == 0)
            {
                Logger.Error(Id + ": no capo could be placed in the compound; there is no council to take.");
                GameUtils.Notify("~r~The cartel council could not be placed. See Bloodlines.log.");
                return false;
            }

            for (int i = 1; i <= Sicarios; i++)
            {
                var ped = Enemy("M58.Guard" + i);
                if (ped != null) _security.Add(ped);
            }

            // An LMG, because the synopsis puts Ice behind one covering the cul-de-sac.
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            if (ice != null && ice.Exists()) ice.Weapons.Give(WeaponHash.MG, 300, false, true);

            Paleto.Review(Ctx, PlacementContract.Ped("M58.Start"), PlacementContract.Ped("M58.Overwatch"),
                PlacementContract.Interaction("M58.Vent1"), PlacementContract.Interaction("M58.Vent2"),
                PlacementContract.Vehicle("M58.Hauler", new Model(HaulerModel)));

            Establish("approach", "One block, one council",
                "Everything left of the Cifuentes command is behind one wall in Mirror Park. Guess takes the gate with the hauler, Gohan puts gas into the roof vents, and Ice holds the street so nobody walks out of it.",
                _hauler);
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Take the gate",
                new DeliverVehicleObjective("Guess: put the hauler through the compound gate",
                    () => _hauler, () => _gate, BreachRadius))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(c => { _breached = true; Fighting = true; Awareness.ReportToAll(Stimulus.RadioCall, _gate); })
                .AfterCues("M58_S1_01_ICE");

            // Both vents, either order, and the overwatch alongside them: parallel objectives
            // with named owners are what let the player move between Gohan and Ice freely.
            var vents = VentKeys
                .Select(key => (Objective)new MissionInteraction("Gohan: put gas into the roof vent",
                    () => At(key), GasSeconds, 2.5f, animation: MissionInteraction.ReachInside)
                { RequiredCharacter = CrewSlot.Gohan })
                .ToArray();

            yield return new MissionStage("Gas the villa", vents)
                .OnExit(c => _gassed = true)
                .AfterCues("M58_S1_02_GOHAN");

            yield return new MissionStage("Take the council",
                new KillTargetsObjective("Take the Cifuentes leadership council", () => _council))
                .AnyBrother()
                .OnExit(c => Finished())
                .AfterCues("M58_S1_03_GUESS");
        }

        private void Finished()
        {
            _councilDown = true;
            Ctx.State?.SetEvidence(CouncilEvidence, EvidenceState.CopyHeld);
            Logger.Info(Id + ": the Cifuentes council is gone and that command chain is broken.");
        }

        protected override void OnPassed()
        {
            if (!_breached || !_gassed || !_councilDown)
                throw new InvalidOperationException("The gate, the vents and the council all have to be taken.");
            Release(_hauler);
        }
    }
}
