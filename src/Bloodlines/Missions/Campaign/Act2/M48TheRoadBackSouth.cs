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
        /// <summary>
        /// How far down the road the cordon stands from wherever the technical is.
        ///
        /// Ron surveyed the technical up onto the road — the authored point was on the pier
        /// with no way to drive it back up — and that put it eighteen meters from the
        /// authored roadblock, so the cordon was spawning on top of the crew. The line is
        /// pushed along the road until it is at least this far from the truck, which keeps
        /// working wherever either key is surveyed to next.
        /// </summary>
        public const float CordonStandoff = 70f;
        /// <summary>How often the gun in the bed is told what to shoot at.</summary>
        public const int GunOrderMs = 1500;
        /// <summary>How far the bed gun will reach.</summary>
        public const float GunRange = 90f;

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
        private int _gunOrderAt;
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
            // Deploying the crew and staging a boat are different things, and they used to
            // be the second and third branches of one else-if chain about the boat. A run
            // that had to stage its own boat therefore never deployed anybody, Guess had no
            // ped at all, and the mission failed on its first tick saying "Guess is down" —
            // which is what ComposedMission reports for a required brother who is missing,
            // not for one who has been shot. Ron read it as the cordon killing him.
            if (!Paleto.IsContinuing(Ctx) &&
                !Ctx.Crew.Deploy(CrewSlot.Guess, At("M48.Technical"), Ctx.Locations.Heading("M48.Technical"))) return false;

            _boat = world?.Get<Vehicle>("boat");
            if (Paleto.IsContinuing(Ctx)) _boat = Track(world.Require<Vehicle>("boat"));
            else if (_boat == null || !_boat.Exists()) _boat = StageBoat();

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
            // A blip on the truck, so the thing the objectives keep naming is the thing with a
            // marker on it. The chapter arrives in M47's boat and that boat is still the crew's
            // ride until something says otherwise; an unmarked truck up on the road is easy to
            // read as "go back to the vehicle you came in".
            var mark = Track(_technical.AddBlip());
            if (mark != null) { mark.Color = BlipColor.Orange; mark.Name = "Technical - load the ledger"; }
            return true;
        }

        /// <summary>
        /// Where the roadblock actually stands: the authored line, pushed on down the road
        /// toward the county line until it is a fight the crew drives into rather than a
        /// fight they spawn inside. Derived from the two keys and the exit, so surveying
        /// any of them keeps the separation instead of breaking it.
        /// </summary>
        private Vector3 CordonLine()
        {
            var truck = At("M48.Technical");
            var line = At("M48.Cordon");
            float gap = line.DistanceTo2D(truck);
            if (gap >= CordonStandoff) return line;
            // Down the road, which is the way out: the run south is the only direction
            // this chapter travels in.
            var south = At("M48.South");
            var away = new Vector3(south.X - truck.X, south.Y - truck.Y, 0f);
            float length = away.Length();
            if (length < 1f) return line;
            var moved = truck + away * (CordonStandoff / length);
            moved = GameUtils.OnGround(new Vector3(moved.X, moved.Y, line.Z));
            Logger.Info(Id + ": the cordon was " + (int)gap + " m from the technical; moved down the road to " +
                moved + ", " + (int)moved.DistanceTo2D(truck) + " m out.");
            return moved;
        }

        private void SpawnCordon()
        {
            var blockerModel = new Model(BlockerModel);
            if (GameUtils.RequestModel(blockerModel))
            {
                _blocker = Track(World.CreateVehicle(blockerModel, CordonLine(), Ctx.Locations.Heading("M48.Cordon")));
                if (_blocker != null && _blocker.Exists()) { _blocker.PlaceOnGround(); _blocker.IsPersistent = true; }
                blockerModel.MarkAsNoLongerNeeded();
            }
            var model = new Model(GuardModel);
            if (!GameUtils.RequestModel(model)) return;
            // Deliberately not Aegis yet. See HoldingGroup.
            var holding = World.AddRelationshipGroup(HoldingGroup);
            var line = CordonLine();
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

        /// <summary>
        /// The gun in the bed, being used.
        ///
        /// Ron reported the gunner never firing from the car, and the reason is that nothing
        /// ever told him to: a companion in a turret seat holds the seat and does not pick
        /// targets. M35 hit this exact problem from the other side - "a gun in the bed is for
        /// using" - and its answer was TASK_VEHICLE_SHOOT_AT_PED on a cooldown rather than
        /// every frame. This is that, for whichever brother the player is not currently being.
        ///
        /// It only runs once the cordon is hostile. Before that there is nothing to shoot and
        /// opening fire early is what used to get Guess killed at the wheel.
        /// </summary>
        private void WorkTheGun()
        {
            if (Game.GameTime < _gunOrderAt) return;
            _gunOrderAt = Game.GameTime + GunOrderMs;
            if (_technical == null || !_technical.Exists()) return;
            var target = _cordon.FirstOrDefault(g => g != null && g.Exists() && !g.IsDead &&
                g.Position.DistanceTo(_technical.Position) < GunRange);
            if (target == null) return;
            foreach (var hero in Protagonist.All)
            {
                var ped = Ctx.Crew.PedFor(hero.Slot);
                if (ped == null || !ped.Exists() || ped.IsDead) continue;
                if (ped == Game.Player.Character) continue;          // he aims for himself
                if (!ped.IsInVehicle(_technical)) continue;
                if (ped.SeatIndex == VehicleSeat.Driver) continue;   // the driver drives
                Function.Call(Hash.TASK_VEHICLE_SHOOT_AT_PED, ped, target, GunRange);
            }
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
            // Not AnyOf. The crew are already in the truck when the cordon drops, so "any one
            // objective" was satisfied by the boarding check on the first frame and the drive
            // never happened - the chapter ended at the roadblock, which is exactly what Ron
            // reported. Both have to be true: at the county line, and everybody in the truck.
            yield return new MissionStage("Run south",
                new TravelObjective("Drive south past the county line", () => At("M48.South"), 30f, () => _technical),
                new ConditionObjective("Everyone rides south in the technical", () => Loaded))
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
            if (_broken || stage >= SouthStage) WorkTheGun();
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
