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
    /// M62 — "Steel Horizon". The coastal rail crossing at the port basin.
    ///
    /// Aegis is moving its last physical reserves by rail. Ice works the flatbeds, Gohan runs
    /// a boat alongside, Guess brings the Annihilator in for the container.
    ///
    /// **Read this one before changing it: it is the least verifiable mission in the campaign.**
    /// Everything else in Act III stands on placed geometry that can be checked offline. A
    /// train cannot be. What is verified is the line it stands on:
    /// <c>po1_05_railriver1</c>, <c>_2</c> and <c>_3</c> at (439.07, -2737.60, 2.76),
    /// (440.95, -2646.64, 2.27) and (439.53, -2504.07, 2.58) — a rail bridge running north to
    /// south at x ≈ 440, over water, for two hundred and thirty meters. That is coastal rail
    /// with open water beside it and arches under it, which is what the three authored lines
    /// each need: flatbeds to fight along, sixty yards of water for the boat, and arches for
    /// Guess to drop a sling between.
    ///
    /// **The bible says the Pacific Coast Highway and this is the port crossing.** The PCH rail
    /// has no placed entities to survey from; this does. Recorded in
    /// `data/mission_gameplay.tsv`.
    ///
    /// **The train is created and then left standing.** `CREATE_MISSION_TRAIN` puts a real
    /// consist on the rails, and a real consist moves along them on the engine's terms rather
    /// than a mission's. Since nobody can test that here, the mission asks for the train, and
    /// if the native gives nothing back it falls through to ordinary <c>freight</c> and
    /// <c>freightcar</c> vehicles standing on the bridge — Aegis loading rather than Aegis
    /// leaving. Either way the fight happens on the flatbeds. **This is the mission to play
    /// first**: if the consist misbehaves, the fallback is one boolean away.
    /// </summary>
    public sealed class M62SteelHorizon : PreparationOperation
    {
        public const string EngineModel = "freight";
        public const string CarModel = "freightcar";
        public const string BoatModel = "dinghy";
        public const string ChopperModel = "annihilator";
        /// <summary>CREATE_MISSION_TRAIN. Real rails, real consist, the engine's own physics.</summary>
        private const ulong CreateMissionTrain = 0x63C6CCA8E68AE8C8UL;
        /// <summary>Guards riding the flatbeds.</summary>
        public const int Riders = 5;
        /// <summary>How long the container sling takes to hook.</summary>
        public const int HookSeconds = 8;
        /// <summary>How near the consist the boat has to hold. Sixty yards, as the line says.</summary>
        public const float BoatStandoff = 60f;
        /// <summary>Where the campaign records the reserves were taken.</summary>
        public const string ReservesEvidence = "aegisReserves";

        private readonly List<Vehicle> _consist = new List<Vehicle>();
        private readonly List<Ped> _riders = new List<Ped>();
        private Vehicle _boat, _chopper;
        private bool _realTrain, _boarded, _ridersDown, _hooked;

        public override string Id => "M62";
        public override string Title => "Steel Horizon";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;

        /// <summary>Whether the engine gave back a real mission train or the fallback stood in.</summary>
        public bool RealTrain => _realTrain;
        /// <summary>Somebody is on the flatbeds.</summary>
        public bool Boarded => _boarded;
        /// <summary>The escort riding the consist is down.</summary>
        public bool RidersDown => _ridersDown;
        /// <summary>The container is on the sling.</summary>
        public bool Hooked => _hooked;
        public IReadOnlyList<Vehicle> Consist => _consist;
        public IReadOnlyList<Ped> Escort => _riders;
        public Vehicle Boat => _boat;
        public Vehicle Chopper => _chopper;

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Ice)) return false;

            if (!MissionSites.Water(Ctx.Locations, "M62.Boat"))
            {
                GameUtils.Notify("~r~The water beside the rail crossing did not check out. See Bloodlines.log.");
                return false;
            }

            BuildConsist();
            if (_consist.Count == 0)
            {
                Logger.Error(Id + ": neither a mission train nor the fallback consist could be created.");
                GameUtils.Notify("~r~The freight consist could not be placed. See Bloodlines.log.");
                return false;
            }

            _boat = Car(BoatModel, MarineSites.ResolveOrThrow(Ctx.Locations, "M62.Boat", 1.5f),
                Ctx.Locations.Heading("M62.Boat"), false);
            _chopper = Car(ChopperModel, At("M62.Chopper"), Ctx.Locations.Heading("M62.Chopper"), false);
            if (!RequireAssets(_boat, _chopper)) return false;
            _boat.IsPersistent = true;
            _chopper.IsPersistent = true;

            for (int i = 1; i <= Riders; i++)
            {
                var ped = Enemy("M62.Rider" + i);
                if (ped != null) _riders.Add(ped);
            }
            if (_riders.Count == 0)
                Logger.Warn(Id + ": no escort could be placed on the flatbeds; the consist is unguarded.");

            Establish("approach", "Everything they have left, on rails",
                "The last of the Aegis reserves is going out by rail over the basin. Ice takes the flatbeds, Gohan runs the boat alongside, Guess brings the Annihilator in under the arches for the container.",
                _boat, _chopper);
            return true;
        }

        /// <summary>
        /// A real consist if the engine will give one, and honest scenery if it will not.
        ///
        /// CREATE_MISSION_TRAIN is the only way to put a train on the actual rails. Nobody can
        /// verify its behavior from here, so a null return is not a failure: ordinary freight
        /// vehicles standing on the bridge make this Aegis loading rather than Aegis leaving,
        /// and every objective in the mission still works.
        /// </summary>
        private void BuildConsist()
        {
            var at = At("M62.Consist");
            try
            {
                var train = Function.Call<Vehicle>((Hash)CreateMissionTrain, 3, at.X, at.Y, at.Z, true);
                if (train != null && train.Exists())
                {
                    Track(train);
                    train.IsPersistent = true;
                    _consist.Add(train);
                    _realTrain = true;
                    Logger.Info(Id + ": a mission train is on the rails at " + at + ".");
                    return;
                }
            }
            catch (Exception ex) { Logger.Error(Id + ": asking for a mission train", ex); }

            Logger.Warn(Id + ": no mission train came back; standing freight is used instead, so Aegis is loading rather than leaving.");
            var engine = Car(EngineModel, at, Ctx.Locations.Heading("M62.Consist"), false);
            if (engine != null && engine.Exists()) { engine.IsPersistent = true; _consist.Add(engine); }
            for (int i = 1; i <= 2; i++)
            {
                var flat = Car(CarModel, At("M62.Flat" + i), Ctx.Locations.Heading("M62.Flat" + i), false);
                if (flat == null || !flat.Exists()) continue;
                flat.IsPersistent = true;
                _consist.Add(flat);
            }
        }

        /// <summary>The consist's head, or its authored point before anything exists.</summary>
        private Vector3 ConsistPoint() =>
            _consist.Count > 0 && _consist[0] != null && _consist[0].Exists() ? _consist[0].Position : At("M62.Consist");

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Get onto the flatbeds",
                new ReachZoneObjective("Ice: get onto the freight consist", ConsistPoint, 12f))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(c => Fighting = true)
                .OnExit(c => _boarded = true)
                .AfterCues("M62_S1_01_ICE");

            yield return new MissionStage("Take the escort",
                new KillTargetsObjective("Take the escort riding the consist", () => _riders))
                .AnyBrother()
                .OnExit(c => _ridersDown = true)
                .AfterCues("M62_S1_02_GOHAN");

            yield return new MissionStage("Hook the container",
                new MissionInteraction("Hook the cargo container onto the chopper sling",
                    () => At("M62.Container"), HookSeconds, 3f, animation: MissionInteraction.ReachInside))
                .AnyBrother()
                .OnExit(c => Hooking())
                .AfterCues("M62_S1_03_GUESS");
        }

        private void Hooking()
        {
            _hooked = true;
            Ctx.State?.SetEvidence(ReservesEvidence, EvidenceState.CopyHeld);
            Logger.Info(Id + ": the reserves container is off the consist.");
        }

        protected override void OnPassed()
        {
            if (!_boarded || !_ridersDown || !_hooked)
                throw new InvalidOperationException("The flatbeds, the escort and the container all have to be taken.");
            Release(_boat);
            Release(_chopper);
        }
    }
}
