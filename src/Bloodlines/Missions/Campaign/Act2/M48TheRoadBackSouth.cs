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
    /// The last chapter, and the only one that ends anything. The boat comes ashore,
    /// the crew and the evidence move into the technical the preparation block earned,
    /// and they break the outer cordon and run south. Chumash is the second line, not
    /// this one: M49 opens on a road that is still closed.
    ///
    /// This is where the operation's single result is recorded. Everything before it
    /// joined straight through with no payout, no save and no mission passed.
    /// </summary>
    public sealed class M48TheRoadBackSouth : ComposedMission
    {
        public const string TechnicalModel = "technical";
        public const string BlockerModel = "riot";
        public const string GuardModel = "s_m_y_blackops_01";
        public const int CordonGuards = 4;

        /// <summary>The stages that need all three in the truck, and only those: the
        /// transfer off the beach, and the run south once the roadblock is behind them.
        /// Ordering them aboard during the cordon fight would march them into the guns.</summary>
        private const int LoadStage = 1, SouthStage = 3;
        /// <summary>
        /// The group the cordon stands in until the crew reaches it.
        ///
        /// BLOODLINES_AEGIS hates BLOODLINES_CREW for the whole game — the roster sets that
        /// up once — so four riflemen in it with line of sight to a stationary driver
        /// seventy-seven meters away will open fire the instant the chapter loads, which is
        /// how Ron lost Guess at the wheel before he had control. Holding position stopped
        /// them walking into the sea in M45; it does not stop them shooting. A group that
        /// hates nobody does.
        /// </summary>
        public const string HoldingGroup = "BLOODLINES_CORDON_HOLD";

        private readonly List<Ped> _cordon = new List<Ped>();
        private readonly CrewBoarding _boarding = new CrewBoarding();
        private int _boardingStage = -1;
        private Vehicle _boat;
        private Vehicle _technical;
        private Vehicle _blocker;
        private bool _ashore;
        private bool _broken;

        public override string Id => "M48";
        public override string Title => "The Road Back South";
        protected override MissionEndpoint Endpoint => MissionEndpoint.EscapeCheckpoint;

        public Vehicle Technical => _technical;
        /// <summary>The boat is actually on the beach and everyone is out of it.</summary>
        public bool Ashore => _ashore;
        /// <summary>The outer cordon is behind them.</summary>
        public bool Cordon => _broken;

        private Vector3 At(string key) => Ctx.Locations.Position(key);

        protected override bool Setup()
        {
            var world = Paleto.Of(Ctx);
            if (world != null && !world.EvidenceHeld)
                throw new InvalidOperationException("M48 opened without the evidence. The operation must run from M44.");
            _boat = world?.Get<Vehicle>("boat");
            if (Paleto.IsContinuing(Ctx)) _boat = Track(world.Require<Vehicle>("boat"));
            else if (_boat == null || !_boat.Exists()) _boat = StageBoat();
            else if (!Ctx.Crew.Deploy(CrewSlot.Guess, At("M48.Technical"), Ctx.Locations.Heading("M48.Technical"))) return false;

            if (!SpawnTechnical()) return false;
            world?.Bind("technical", _technical);
            RequireAsset(_technical, "The technical was destroyed. There is no way south without it.");
            Paleto.Review(Ctx, PlacementContract.Vehicle("M48.Technical", new Model(TechnicalModel), 20f, null),
                PlacementContract.Vehicle("M48.Cordon", new Model(BlockerModel)));
            SpawnCordon();
            return true;
        }

        /// <summary>The pickup boat, staged offshore when no run carried one in.</summary>
        private Vehicle StageBoat()
        {
            var model = new Model(M47PaletoCollapse.BoatModel);
            if (!GameUtils.RequestModel(model)) return null;
            var boat = Track(World.CreateVehicle(model, MarineSites.ResolveOrThrow(Ctx.Locations, "M47.Clear", 2f), 0f));
            model.MarkAsNoLongerNeeded();
            if (boat != null && boat.Exists()) boat.IsPersistent = true;
            return boat;
        }

        private bool SpawnTechnical()
        {
            var model = new Model(TechnicalModel);
            if (!GameUtils.RequestModel(model)) return false;
            _technical = Track(World.CreateVehicle(model, At("M48.Technical"), Ctx.Locations.Heading("M48.Technical")));
            model.MarkAsNoLongerNeeded();
            if (_technical == null || !_technical.Exists()) return false;
            _technical.PlaceOnGround();
            _technical.IsPersistent = true;
            return true;
        }

        private void SpawnCordon()
        {
            var blockerModel = new Model(BlockerModel);
            if (GameUtils.RequestModel(blockerModel))
            {
                _blocker = Track(World.CreateVehicle(blockerModel, At("M48.Cordon"), Ctx.Locations.Heading("M48.Cordon")));
                if (_blocker != null && _blocker.Exists()) { _blocker.PlaceOnGround(); _blocker.IsPersistent = true; }
                blockerModel.MarkAsNoLongerNeeded();
            }
            var model = new Model(GuardModel);
            if (!GameUtils.RequestModel(model)) return;
            // Deliberately not Aegis yet. See HoldingGroup.
            var holding = World.AddRelationshipGroup(HoldingGroup);
            var line = At("M48.Cordon");
            for (int i = 0; i < CordonGuards; i++)
            {
                var post = GameUtils.OnGround(line + new Vector3(-7f + i * 4.5f, i % 2 == 0 ? 3f : -3f, 0f));
                var guard = World.CreatePed(model, post, 0f);
                if (guard == null || !guard.Exists()) continue;
                guard.RelationshipGroup = holding;
                guard.IsPersistent = true;
                guard.BlockPermanentEvents = true;
                guard.Accuracy = 30;
                guard.Armor = 40;
                guard.Weapons.Give(WeaponHash.CarbineRifle, 180, true, true);
                // Not FightAgainstHatedTargets. The crew loads in 77 meters from this
                // line, well inside a 140-meter engagement, so the cordon used to open
                // fire the instant the chapter started and Guess was shot dead at the
                // wheel before the player had control. They hold the roadblock; the fight
                // starts when the crew drives into it.
                guard.Task.GuardCurrentPosition();
                _cordon.Add(Track(guard));
            }
            model.MarkAsNoLongerNeeded();
        }

        /// <summary>
        /// The roadblock becomes Aegis, and hostile, when the crew drives into it. Until
        /// this runs they are men standing at a barricade who have no opinion about anyone.
        /// </summary>
        private void WakeCordon()
        {
            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            int woken = 0;
            foreach (var guard in _cordon)
            {
                if (guard == null || !guard.Exists() || guard.IsDead) continue;
                guard.RelationshipGroup = aegis;
                guard.BlockPermanentEvents = false;
                guard.Task.GuardCurrentPosition();
                woken++;
            }
            Logger.Info(Id + ": the cordon is hostile now — " + woken + " of " + _cordon.Count + " still standing.");
        }

        private bool Loaded => _technical != null && _technical.Exists() &&
            Protagonist.All.All(hero => Ctx.Crew.PedFor(hero.Slot)?.IsInVehicle(_technical) == true);

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Bring the boat ashore",
                new TravelObjective("Guess: run the boat onto the cove beach", () => At("M48.Shore"), 14f, () => _boat))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(c => _ashore = true);

            yield return new MissionStage("Move the crew and the evidence into the technical",
                new EnterVehicleObjective("All three: get into the technical with the ledger", () => _technical, VehicleSeat.Driver, true),
                new ConditionObjective("Nobody stays at the beach", () => Loaded))
                .OwnedBy(CrewSlot.Guess)
                .AfterCues("M48_S1_01_GUESS");

            yield return new MissionStage("Break the outer cordon",
                new KillTargetsObjective("Clear the roadblock at the cove exit", () => _cordon))
                .AnyOf()
                .OnEnter(c => WakeCordon())
                .OnExit(c => _broken = true)
                .AfterCues("M48_S1_02_ICE");

            // Everyone rides south. Without this the drive could be finished with a
            // brother still standing at the roadblock, and then OnPassed — the one place
            // the whole operation is recorded — would throw on the last objective of a
            // five-chapter sitting.
            yield return new MissionStage("Run south",
                new TravelObjective("Drive south past the county line", () => At("M48.South"), 30f, () => _technical),
                new ConditionObjective("Everyone rides south in the technical", () => Loaded))
                .AnyOf()
                .AfterCues("M48_S1_03_GOHAN");
        }

        /// <summary>
        /// The two stages that want the crew in the truck order them into it. The
        /// objectives only ever checked whether they were already aboard, which on a
        /// beach nobody had told them to leave meant waiting for good.
        /// </summary>
        protected override void OnUpdate()
        {
            int stage = Stage;
            if (stage == LoadStage || stage == SouthStage)
            {
                // Each boarding gets its own patience clock; the cordon fight in between
                // is not part of either one.
                if (_boardingStage != stage) { _boarding.Reset(); _boardingStage = stage; }
                if (!Loaded) _boarding.Update(Ctx.Crew, _technical, Riders, Id);
            }
            base.OnUpdate();
        }

        /// <summary>Guess drives the technical out; the other two take the cab and the bed.</summary>
        private static readonly KeyValuePair<CrewSlot, VehicleSeat>[] Riders =
            CrewBoarding.Passengers(CrewSlot.Guess, VehicleSeat.RightFront, VehicleSeat.LeftRear);

        protected override void OnPassed()
        {
            if (!_ashore || !_broken) throw new InvalidOperationException("The shore transfer and the cordon both have to be behind them.");
            if (!Loaded) throw new InvalidOperationException("All three ride south or the operation is not finished.");
            Preserve(_technical);
        }
    }
}
