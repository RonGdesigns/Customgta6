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
    /// M24 — "Liquid Gold". Alamo Sea shallows, 14:00, harsh sun.
    ///
    /// The bullion has been sitting in four feet of water since M22. The crew needs
    /// operating capital, so five tons of it comes back out on a crane truck while
    /// Blaine County sheriff's deputies decide this looks like a shakedown worth
    /// making.
    ///
    /// Seen, not told: the truck looked over and the container still in the water
    /// before anyone moves, with Ron on the truck, Ice on the ridge and Gohan's work
    /// position and his route to the truck named; the recovery as two crates lifted
    /// out of the container onto the bed; the deputies arriving by road in cruisers
    /// after a warning call, bought men and not a police force; Gohan aboard before
    /// the truck moves; the crates unloaded into bay one at the bunker with what is
    /// left in the Alamo said once.
    ///
    /// The save is the continuity here: M22 put thirty tons in the Alamo, this takes
    /// five, and AlamoGoldDredgedTons carries the number forward for the rest of the
    /// campaign. It commits once, in MarkComplete, however many times the job is run.
    /// </summary>
    public sealed class M24LiquidGold : ComposedMission
    {
        private readonly List<Ped> _deputies = new List<Ped>();
        private readonly List<Vehicle> _cruisers = new List<Vehicle>();
        private readonly List<Prop> _crates = new List<Prop>();
        private readonly HashSet<int> _engaged = new HashSet<int>();

        private Vehicle _crane;
        private Prop _container;
        private Vector3 _dredge;
        private Vector3 _drop;
        private Vector3 _workPoint;
        private Vector3 _ridge;
        private Vector3 _ridgeRoad;
        private Vector3 _bunker;
        private Vector3 _bay;
        private int _boardBy;
        private bool _hoisted, _called, _boarded, _unloaded;

        public override string Id => "M24";
        public override string Title => "Liquid Gold";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SafehouseArrival;

        public Vehicle Crane => _crane;
        public Prop Container => _container;
        public IReadOnlyList<Prop> Crates => _crates;
        public IReadOnlyList<Vehicle> Cruisers => _cruisers;
        public bool Hoisted => _hoisted;
        public bool Boarded => _boarded;
        public bool Unloaded => _unloaded;

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _dredge = Ctx.Locations.Position("M24.CraneSpawn") + new Vector3(-12f, 0f, 0f);
            _drop = Ctx.Locations.Position("M22.AlamoDrop");
            _ridge = Ctx.Locations.Position("M24.RidgeLine");
            _ridgeRoad = Ctx.Locations.Position("M24.RidgeRoad");
            _bunker = Ctx.Locations.Position("M23.BunkerDoor");
            _bay = Ctx.Locations.Position("M23.BayOne");
            // Gohan works at the container's shore side, in the shallows, not on the truck.
            var toShore = _dredge - _drop; toShore.Z = 0f;
            float run = (float)System.Math.Sqrt(toShore.X * toShore.X + toShore.Y * toShore.Y);
            _workPoint = run < 1f ? _drop : _drop + toShore * (8f / run);

            if (!Ctx.Crew.Deploy(CrewSlot.Guess, Ctx.Locations.Position("M24.CraneSpawn"),
                    Ctx.Locations.Heading("M24.CraneSpawn")))
            {
                return false;
            }

            ApplyBibleSetting();
            SpawnCrane();
            SpawnContainer();
            if (!RequireAssets(_crane)) return false;
            RequireAsset(_crane, "The crane truck was destroyed. Nothing comes out of the Alamo without it.");
            Ctx.Crew.CompanionsHoldPosition = true;
            Station(CrewSlot.Ice, _ridge + new Vector3(0f, -25f, 0f));
            Station(CrewSlot.Gohan, _workPoint);
            PlayApproach();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Into the shallows",
                    new DeliverVehicleObjective("Guess — park the recovery truck at the dry shoreline marker.",
                        () => _crane, () => _dredge, 18f))
                .OwnedBy(CrewSlot.Guess);

            // The dredge and the shakedown run together: Ice is holding the ridge for
            // exactly as long as the cable takes. The deputies come by road, after
            // Ice's warning, not out of the ground.
            yield return new MissionStage("Dredge the crates",
                    new AssignedWorkObjective("Guess works the recovery cable. Ice: cover him from the ridge.", CrewSlot.Guess, () => _dredge, 22),
                    new SurviveWavesObjective("Ice — keep the deputies off the haul.", SpawnDeputyWave, 2, 5000) { RequiredCharacter = CrewSlot.Ice },
                    new ProtectObjective("", () => _crane, "The crane truck was destroyed."))
                .OnExit(context => PlayHoist())
                .WithCues("M24_S1_02_ICE");

            // Gohan cannot be in the water and on the moving truck at the same instant:
            // he boards, seen, before it rolls.
            yield return new MissionStage("Gohan aboard",
                    new ConditionObjective("Gohan is boarding the truck. Hold here until he is in.", GohanAboard),
                    new ReactionTrigger(() => !_called && !Ctx.Cutscenes.IsActive, CallGohanAboard))
                .OnEnter(context => _boardBy = Game.GameTime + 20000);

            yield return new MissionStage("Back to the bunker",
                    new DeliverVehicleObjective("Get the haul to the radar base.",
                        () => _crane, () => _bunker, 35f) { RequiredCharacter = CrewSlot.Guess },
                    new ProtectObjective("", () => _crane, "The crane truck was destroyed."))
                .OnExit(context => PlayUnload());
        }

        // ---------- beats ----------

        /// <summary>The truck looked over, the container in the water, Ice on the ridge, Gohan's work position and his route: why only the first portion, before anyone moves.</summary>
        private void PlayApproach()
        {
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            var blocking = new SceneBlocking();
            if (guess != null && guess.Exists() && _crane != null && _crane.Exists())
                blocking.Then(new InspectStep(guess, _crane.Position + new Vector3(3f, -2f, 0f), 2600, "WORLD_HUMAN_CLIPBOARD"))
                    .Then(new ShotStep(2600, _crane, new Vector3(-6f, 4f, 1.8f), _crane, new Vector3(0f, 0f, 0.9f), 0.6f));
            if (_container != null && _container.Exists()) blocking.Then(new ShotStep(3000, _container, new Vector3(-14f, 9f, 5f), _container, new Vector3(0f, 0f, 0.5f), 1.0f));
            if (ice != null && ice.Exists()) blocking.Then(ShotStep.Watching(2400, ice, ice));
            if (gohan != null && gohan.Exists() && _crane != null && _crane.Exists()) blocking.Then(ShotStep.Watching(2600, gohan, _crane));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "The shallows",
                Reason = "The crane truck looked over by Ron, the container still in four feet of Alamo water, Ice on the ridge with the road below him, Gohan in the shallows at the container with the truck as his way out. Five tons, the first portion only: enough to keep the lights on, not a convoy.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M24 approach scene did not play; the shore stands on its own.");
        }

        /// <summary>The recovery as a staged lift: two crates out of the container onto the bed. No simulated pallets.</summary>
        private void PlayHoist()
        {
            _hoisted = true;
            SpawnCrates();
            var blocking = new SceneBlocking();
            if (_crane != null && _crane.Exists())
            {
                for (int i = 0; i < _crates.Count; i++)
                    blocking.Then(new TransferPropStep(_crates[i], _crane, new Vector3(i == 0 ? -0.7f : 0.7f, -2.4f, 1.1f), 1400, _crane));
                blocking.Then(new ShotStep(2600, _crane, new Vector3(-6f, -5f, 2.2f), _crane, new Vector3(0f, -2.4f, 1.2f), 0.5f));
            }
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "hoist", Title = "The first portion",
                Reason = "Two crates out of the container and onto the bed: five tons, seen on the truck. The rest stays in the water.",
                Blocking = blocking
            };
            var cue = Ctx.Data?.Cue("M24_S1_01_GUESS");
            if (!Ctx.Cutscenes.PlayStaged(spec, new[] { cue })) { Logger.Warn("M24 hoist scene did not play; the crates are placed directly."); blocking.Complete(); Say("M24_S1_01_GUESS"); }
            GameUtils.Subtitle("~g~Five tons on the bed. Twenty-five still in the mud.", 5000);
        }

        private void CallGohanAboard()
        {
            _called = true;
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            if (gohan == null || !gohan.Exists() || _crane == null || !_crane.Exists()) return;
            Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Gohan);
            gohan.Task.ClearAll();
            gohan.Task.EnterVehicle(_crane, VehicleSeat.Passenger, 15000, 2f, EnterVehicleFlags.None);
            Radio("GOHAN", "Out of the water. Give me a second to get on the truck; don't roll with me on the step.", "M24_RADIO_01_GOHAN");
        }

        private bool GohanAboard()
        {
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            if (gohan == null || !gohan.Exists() || _crane == null || !_crane.Exists()) return true;
            if (_called && gohan.IsInVehicle(_crane)) { _boarded = true; return true; }
            // A bounded wait from the stage, whatever the scene did: he is seated directly after it.
            if (Game.GameTime < _boardBy) return false;
            gohan.SetIntoVehicle(_crane, VehicleSeat.Passenger);
            _boarded = true;
            Logger.Warn("M24: Gohan did not reach the truck in time; seated directly.");
            return true;
        }

        /// <summary>The crates into bay one at the bunker, seen; what is left in the Alamo said once. The reward itself commits with completion.</summary>
        private void PlayUnload()
        {
            _unloaded = true;
            var blocking = new SceneBlocking();
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            if (gohan != null && gohan.Exists() && gohan.IsInVehicle()) blocking.Then(new ExitVehicleStep(gohan));
            blocking.Then(new ShotStep(3400, null, _bay + new Vector3(-6f, -5f, 2.2f), null, _bay + new Vector3(0f, 0f, 0.8f), 0.5f, PlaceCrates));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "unload", Title = "Bay one",
                Reason = "The two crates off the truck and into bay one: the first portion stored where the crew can guard it. What is left in the Alamo is said once.",
                Blocking = blocking
            };
            var cue = Ctx.Data?.Cue("M24_S1_03_GOHAN");
            if (!Ctx.Cutscenes.PlayStaged(spec, new[] { cue })) { Logger.Warn("M24 unload scene did not play; the crates are placed directly."); blocking.Complete(); PlaceCrates(); Say("M24_S1_03_GOHAN"); }
            float dredged = Ctx.State?.AlamoGoldDredgedTons ?? 0f;
            float remaining = 30f - dredged - (Ctx.State != null && !Ctx.State.IsComplete(Id) ? 5f : 0f);
            Ctx.State?.SetCargo("recoveredGold", "M23.BayOne");
            GameUtils.Subtitle("~g~Five tons in bay one. " + remaining.ToString("0") + " tons still in the Alamo. The deputies knew where to look; that is the next problem.", 6000);
            Logger.Info("M24: first portion in bay one; " + remaining.ToString("0") + " t remain hidden in the Alamo.");
        }

        private void PlaceCrates()
        {
            for (int i = 0; i < _crates.Count; i++)
            {
                var crate = _crates[i];
                if (crate == null || !crate.Exists()) continue;
                crate.Detach();
                crate.Position = _bay + new Vector3(i * 1.4f - 0.7f, 1.5f, 0f);
                crate.IsPositionFrozen = true;
            }
        }

        /// <summary>The aftermath: the truck at the bunker with the bay behind it.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_crane == null || !_crane.Exists()) return null;
            return new SceneBlocking().Then(new ShotStep(4500, _crane, new Vector3(-7f, 4f, 2f), _crane, new Vector3(0f, 0f, 0.9f), 1.0f));
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            EngageArrivals();
        }

        /// <summary>A cruiser that has reached the shore lets its driver out to fight; until then he drives, which is the visible route.</summary>
        private void EngageArrivals()
        {
            foreach (var cruiser in _cruisers)
            {
                if (cruiser == null || !cruiser.Exists() || _engaged.Contains(cruiser.Handle)) continue;
                if (cruiser.Position.DistanceTo(_ridge) > 25f) continue;
                _engaged.Add(cruiser.Handle);
                foreach (var occupant in cruiser.Occupants)
                    if (occupant != null && occupant.Exists() && !occupant.IsDead) occupant.Task.FightAgainstHatedTargets(120f);
            }
        }

        // ---------- world building ----------

        /// <summary>Bought deputies in cruisers, by the ridge road: a visible route, not a spawn on the ridge. Not a police force; men on a bounty.</summary>
        private IEnumerable<Ped> SpawnDeputyWave(int wave)
        {
            var pedModel = new Model("s_m_y_sheriff_01");
            var carModel = new Model("sheriff2");
            if (!GameUtils.RequestModel(pedModel)) return Enumerable.Empty<Ped>();

            var law = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            var spawned = new List<Ped>();
            bool cars = GameUtils.RequestModel(carModel);
            // Waves are numbered from one: one cruiser, then two.
            int cruisersThisWave = System.Math.Max(1, wave);

            for (int car = 0; car < cruisersThisWave; car++)
            {
                Vehicle cruiser = null;
                if (cars)
                {
                    cruiser = Track(World.CreateVehicle(carModel, _ridgeRoad + new Vector3(0f, car * 12f, 0f), Ctx.Locations.Heading("M24.RidgeRoad")));
                    if (cruiser != null && cruiser.Exists()) { cruiser.IsPersistent = true; cruiser.IsEngineRunning = true; _cruisers.Add(cruiser); }
                    else cruiser = null;
                }
                for (int seat = 0; seat < 2; seat++)
                {
                    var post = cruiser != null ? cruiser.Position : _ridge + new Vector3(-10f + (car * 2 + seat) * 6f, 4f, 0f);
                    var deputy = World.CreatePed(pedModel, post, 200f);
                    if (deputy == null || !deputy.Exists()) continue;

                    deputy.RelationshipGroup = law;
                    deputy.IsPersistent = true;
                    deputy.BlockPermanentEvents = true;
                    deputy.Accuracy = 30;
                    deputy.Armor = 30;
                    deputy.Weapons.Give(WeaponHash.PumpShotgun, 120, true, true);
                    if (cruiser != null)
                    {
                        deputy.Task.WarpIntoVehicle(cruiser, seat == 0 ? VehicleSeat.Driver : VehicleSeat.Passenger);
                        if (seat == 0) deputy.Task.StartVehicleMission(cruiser, _ridge + new Vector3(0f, 14f, 0f), VehicleMissionType.GoTo, 22f, (VehicleDrivingFlags)786603, 8f, 20f, true);
                    }
                    else deputy.Task.FightAgainstHatedTargets(120f);

                    spawned.Add(Track(deputy));
                    _deputies.Add(deputy);
                }
                if (cruiser != null)
                {
                    var blip = Track(cruiser.AddBlip());
                    blip.Sprite = BlipSprite.PoliceCarDot;
                    blip.Color = BlipColor.Red;
                    blip.Name = "Bought deputies";
                }
            }
            Logger.Info("M24: wave " + wave + ", " + cruisersThisWave + " cruiser(s) from the ridge road.");

            if (cars) carModel.MarkAsNoLongerNeeded();
            pedModel.MarkAsNoLongerNeeded();
            return spawned;
        }

        private void SpawnCrane()
        {
            var model = new Model("flatbed");
            if (!GameUtils.RequestModel(model)) return;

            _crane = Track(World.CreateVehicle(model, Ctx.Locations.Position("M24.CraneSpawn"),
                Ctx.Locations.Heading("M24.CraneSpawn")));
            model.MarkAsNoLongerNeeded();
            if (_crane == null || !_crane.Exists()) return;

            _crane.IsPersistent = true;

            var blip = Track(_crane.AddBlip());
            blip.Sprite = BlipSprite.ArmoredTruck;
            blip.Color = BlipColor.Orange;
            blip.Name = "Crane truck";
        }

        /// <summary>The hidden cargo where M22 put it: the container in the shallows, frozen, seen before the first portion comes out.</summary>
        private void SpawnContainer()
        {
            var model = new Model("prop_container_01a");
            if (!GameUtils.RequestModel(model)) return;
            _container = Track(PortHeist.NearbyProp(model, _drop, 15f) ?? World.CreateProp(model, _drop, false, false));
            model.MarkAsNoLongerNeeded();
            if (_container == null || !_container.Exists()) return;
            _container.IsPersistent = true;
            _container.IsPositionFrozen = true;
        }

        /// <summary>Two crates at the container: the first portion, as things the crane can lift.</summary>
        private void SpawnCrates()
        {
            var model = new Model("prop_mil_crate_01");
            if (!GameUtils.RequestModel(model)) return;
            for (int i = 0; i < 2; i++)
            {
                var crate = Track(World.CreateProp(model, _drop + new Vector3(i * 1.6f - 0.8f, 0f, 1.6f), false, false));
                if (crate == null || !crate.Exists()) continue;
                crate.IsPersistent = true;
                _crates.Add(crate);
            }
            model.MarkAsNoLongerNeeded();
        }

        protected override void OnPassed()
        {
            // The container stays in the shallows for the rest of the haul; the crates stay in the bay.
            if (_container != null && _container.Exists()) Release(_container);
            foreach (var crate in _crates) if (crate != null && crate.Exists()) Release(crate);
        }

        protected override void OnCleanup()
        {
            Ctx.Crew.CompanionsHoldPosition = false;
            _deputies.Clear();
            _cruisers.Clear();
            _crates.Clear();
        }
    }
}
