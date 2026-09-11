using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M22 — "The Port Heist: Scorched Bay". Alamo Sea, 04:30, sunrise smoke.
    ///
    /// The end of Act I. The bullion goes into four feet of Alamo water, the crew
    /// stands on the beach, and Aegis answers by putting a cruise missile through
    /// their Los Santos shop. Everything they built in twenty-two missions is gone in
    /// one shot, and the exile to Blaine County is not a choice.
    ///
    /// Seen, not told: the shallow drop point and each brother's arrival (the lift
    /// held over the water, the Granger from M21 coming down the road), not a
    /// planning scene; the drop as an insert the hover earns; a short real regroup on
    /// the beach; the strike learned from the foundry's alarm line on Ron's phone and
    /// seen as a distant cut before anyone explains it; Ron with the foundry keys
    /// from M03 in his hand, one reaction and one practical next step; the hidden
    /// cargo and the foundry's loss recorded once, so a replay never rewrites the
    /// salvage ledger.
    ///
    /// The airstrike is a cutscene, per docs/FEASIBILITY.md: a fade, a detonation at
    /// the foundry's real coordinates, and a smoke column the player can see from the
    /// beach. The mission does not try to simulate a cruise missile and does not need
    /// to — the loss is what has to land, not the ordnance. One strike source: a
    /// contract airframe with a city permit, as Gohan's line has it.
    /// </summary>
    public sealed class M22ScorchedBay : ComposedMission
    {
        /// <summary>The foundry keys Ron put down in M03: the object the loss is felt through.</summary>
        public const string KeysModel = "p_car_keys_01";

        private Vehicle _cargobob;
        private Vehicle _granger;
        private Prop _container;
        private Prop _keys;
        private Prop _laptop;
        private Vector3 _drop;
        private Vector3 _beach;
        private Vector3 _regroup;
        private Vector3 _road;
        private bool _arrived, _dropped, _landed, _struck, _cargoRecorded;
        private bool _arrivalStarted;
        private int _roadStarted, _roadOrder;
        public bool RoadArrivalPending => !_arrivalStarted;

        public override string Id => "M22";
        public override string Title => "The Port Heist: Scorched Bay";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;

        public Vehicle Cargobob => _cargobob;
        public Vehicle Granger => _granger;
        public Prop Container => _container;
        public Prop Keys => _keys;
        public bool Arrived => _arrived;
        public bool Dropped => _dropped;
        public bool Landed => _landed;
        public bool Struck => _struck;
        public bool CargoRecorded => _cargoRecorded;

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _drop = Ctx.Locations.Position("M22.AlamoDrop");
            _beach = Ctx.Locations.Position("M22.Beach");
            _road = Ctx.Locations.Position("M22.RoadArrival");
            _regroup = _beach + new Vector3(-6f, 6f, 0f);

            // On the shore. Deploying sixty meters above the Alamo drops the crew
            // into it.
            if (!PortHeist.IsContinuing(Ctx) && !Ctx.Crew.Deploy(CrewSlot.Guess, _beach, Ctx.Locations.Heading("M22.Beach")))
            {
                return false;
            }

            ApplyBibleSetting();
            var handoff = Ctx.Handoffs.Take(PortHeist.Operation, Id);
            SpawnLift();
            SpawnGranger();
            if (!RequireAssets(_cargobob, _container, _granger)) return false;
            Ctx.PortHeist?.Bind("lift", _cargobob);
            Ctx.PortHeist?.Bind("bullion", _container);
            Ctx.PortHeist?.Bind("granger", _granger);
            RequireAsset(_container, "The bullion was lost before the operation finished.");
            RequireAsset(_granger, "The road team lost its transport.");
            if (handoff != null && !handoff.CargoAttached) Logger.Warn("M22: M21 recorded the lift without cargo; the Alamo drop still uses a container.");
            RequireAsset(_cargobob, "The Cargobob went down. The bullion never reached the Alamo.");
            Ctx.Crew.CompanionsHoldPosition = true;
            if (PortHeist.IsContinuing(Ctx))
            {
                if (!PortHeistWorld.Seated(Ctx.Crew.PedFor(CrewSlot.Guess), _cargobob, VehicleSeat.Driver) ||
                    !PortHeistWorld.Seated(Ctx.Crew.PedFor(CrewSlot.Gohan), _granger, VehicleSeat.Driver) ||
                    !PortHeistWorld.Seated(Ctx.Crew.PedFor(CrewSlot.Ice), _granger, VehicleSeat.Passenger))
                    throw new System.InvalidOperationException("The inland transfer requires the original pilot and road team.");
                Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Guess);
                Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Gohan);
                Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Ice);
                _roadStarted = Game.GameTime;
                _roadOrder = Game.GameTime;
                OrderRoadTeam();
                HoldLift();
                Radio("GOHAN", "We have the road north. You fly the bullion over the ridge; we'll meet you on the beach.", "M22_OPERATION_ROAD");
                return true;
            }
            Station(CrewSlot.Guess, _cargobob, VehicleSeat.Driver);
            if (_granger != null && _granger.Exists())
            {
                Station(CrewSlot.Gohan, _granger, VehicleSeat.Driver);
                Station(CrewSlot.Ice, _granger, VehicleSeat.Passenger);
            }
            else
            {
                Station(CrewSlot.Ice, _beach + new Vector3(-15f, 0f, 0f));
                Station(CrewSlot.Gohan, _beach + new Vector3(0f, 15f, 0f));
            }
            HoldLift();
            PlayApproach();
            return true;
        }

        private void OrderRoadTeam()
        {
            var driver = Ctx.Crew.PedFor(CrewSlot.Gohan);
            if (!PortHeistWorld.Seated(driver, _granger, VehicleSeat.Driver))
                throw new System.InvalidOperationException("Gohan must remain at the wheel of the road pickup.");
            _granger.IsEngineRunning = true;
            driver.Task.DriveTo(_granger, _road, 10f, 27f, DrivingStyle.Rushed);
            _roadOrder = Game.GameTime;
        }

        protected override void OnUpdate()
        {
            if (PortHeist.IsContinuing(Ctx) && !_arrivalStarted)
            {
                if (_granger == null || !_granger.Exists() || !_granger.IsDriveable)
                { Fail("The road team lost the Granger."); return; }
                if (_granger.Position.DistanceTo(_road) < 55f)
                { PlayApproach(); return; }
                if (Game.GameTime - _roadStarted > 600000)
                { Fail("The road team could not reach the Alamo. Retry Scorched Bay."); return; }
                if (Game.GameTime - _roadOrder > 15000) OrderRoadTeam();
            }
            base.OnUpdate();
        }

        private void HoldLift()
        {
            if (_cargobob == null || !_cargobob.Exists()) return;
            _cargobob.IsPositionFrozen = true;
            _cargobob.IsEngineRunning = true;
            Function.Call(Hash.SET_HELI_BLADES_FULL_SPEED, _cargobob);
        }

        private void ReleaseLift()
        {
            if (_cargobob == null || !_cargobob.Exists()) return;
            _cargobob.IsPositionFrozen = false;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Bring it in",
                    new EnterVehicleObjective("Guess — fly the bullion into the Alamo.",
                        () => _cargobob, VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context => ReleaseLift());

            yield return new MissionStage("Drop the container",
                    new MissionInteraction("Guess: hover 20m over the water marker and release the container", () => _drop + new Vector3(0f, 0f, 20f), 3, 10f, () => _cargobob))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context => PlayDrop());

            yield return new MissionStage("The beach",
                    new DeliverVehicleObjective("Guess: land the Cargobob on the marked shore and stop.", () => _cargobob, () => _beach, 35f, land: true))
                .OnExit(context => { _landed = true; if (_cargobob != null && _cargobob.Exists()) _cargobob.IsEngineRunning = false; });

            // A short, real regroup: Ron out of the aircraft and over to the others on
            // foot. The strike arrives while they are checking gear, not in a lineup.
            yield return new MissionStage("Regroup",
                    new ConditionObjective("Guess: on foot to Ice and Gohan on the beach. Wait for their road arrival.", () =>
                        _arrived && Game.Player.Character != null && !Game.Player.Character.IsInVehicle() &&
                        Game.Player.Character.Position.DistanceTo(_regroup) <= 4f &&
                        Ctx.Crew.PedFor(CrewSlot.Ice).Position.DistanceTo(_regroup) <= 8f &&
                        Ctx.Crew.PedFor(CrewSlot.Gohan).Position.DistanceTo(_regroup) <= 8f))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context => PlayStrike());

            yield return new MissionStage("Blaine County",
                    new DialogueFinishedObjective("The foundry is gone. Listen."))
                .OnExit(context =>
                {
                    // Act I closes on a loss, not a payday, and the save records both
                    // once, in CampaignState.MarkComplete: a replay never resets the
                    // salvage ledger M24 starts.
                    GameUtils.Subtitle("~y~When we come back to Los Santos, we come back as an army. Regroup: Blaine County. The radar site is next.", 7000);
                })
                .WithCues("M22_S1_06_GOHAN");
        }

        // ---------- beats ----------

        /// <summary>The shallow drop point, the lift held over the water with the load, and the Granger coming down the road: each brother's arrival, no plan.</summary>
        private void PlayApproach()
        {
            _arrivalStarted = true;
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var blocking = new SceneBlocking();
            blocking.Then(ShotStep.Wide(3000, _drop + new Vector3(0f, 0f, 1f), 30f, 12f, 10f));
            if (_cargobob != null && _cargobob.Exists()) blocking.Then(new ShotStep(3000, _cargobob, new Vector3(-16f, 9f, 2f), _cargobob, new Vector3(0f, 0f, -4f), 1.2f));
            if (_granger != null && _granger.Exists() && gohan != null && gohan.Exists())
            {
                var arrival = _beach + new Vector3(12f, 10f, 0f);
                blocking.Then(new DriveUpStep(gohan, _granger, arrival, DriveUpStep.HeadingBetween(_road, arrival)));
                blocking.Then(new ExitVehicleStep(gohan));
                if (ice != null && ice.Exists()) blocking.Then(new ExitVehicleStep(ice));
                blocking.Then(new WalkToStep(gohan, _regroup + new Vector3(-1.5f, 0f, 0f), 1.5f));
                if (ice != null && ice.Exists()) blocking.Then(new WalkToStep(ice, _regroup + new Vector3(1.5f, 0f, 0f), 1.5f));
            }
            blocking.Then(new VerifySceneStep("The road team reached the beach", () =>
            {
                return gohan != null && gohan.Exists() && !gohan.IsDead && !gohan.IsInVehicle() && gohan.Position.DistanceTo(_regroup) < 8f &&
                    ice != null && ice.Exists() && !ice.IsDead && !ice.IsInVehicle() && ice.Position.DistanceTo(_regroup) < 8f;
            }, () => _arrived = true));
            var spec = new SceneSpec
            {
                RequiresCompletion = true,
                MissionId = Id, Phase = "approach", Title = "The Alamo",
                Reason = "Four feet of water at the drop point, the lift held over the lake with the container under it, and the Granger from the coast coming down the road with Gohan driving and Ice beside him. Everyone arrives the way they left the ocean. The deposit is thirty tons in the shallows, not spendable money.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) PortHeist.RequireFallback(blocking, "The road team's beach arrival");
        }

        /// <summary>The drop as an insert: the container leaves the cable and settles in the shallows. The hidden cargo is recorded once.</summary>
        private void PlayDrop()
        {
            ReleaseContainer();
            if (_container == null || !_container.Exists() || PortHeistWorld.Attached(_container, _cargobob) || _container.Position.DistanceTo(_drop) > 3f)
                throw new System.InvalidOperationException("The bullion drop did not reach the shallows.");
            _dropped = true;
            var blocking = new SceneBlocking();
            if (_container != null && _container.Exists()) blocking.Then(new ShotStep(3800, _container, new Vector3(-12f, 8f, 5f), _container, new Vector3(0f, 0f, 0.5f), 0.8f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "drop", Title = "The shallows",
                Reason = "Thirty tons of bullion in four feet of Alamo water: the campaign's bank until M24 starts dredging it back out. Recorded once; a replay does not move it.",
                Blocking = blocking
            };
            var cue = Ctx.Data?.Cue("M22_S1_01_GUESS");
            if (!Ctx.Cutscenes.PlayStaged(spec, new[] { cue })) { Logger.Warn("M22 drop scene did not play; the line plays as dialogue."); blocking.Complete(); Say("M22_S1_01_GUESS"); }
            if (Ctx.State != null && !Ctx.State.IsComplete(Id))
            {
                PortHeist.RecordCargo(Ctx, PortHeist.BullionCargo, "M22.AlamoDrop");
                _cargoRecorded = true;
            }
            else Logger.Info("M22: the Alamo cargo was recorded on the first pass; the replay leaves the ledger alone.");
        }

        /// <summary>
        /// The strike, learned before it is explained: the foundry's alarm line on
        /// Ron's phone, a distant cut to the detonation at the foundry's real
        /// coordinates, the column over the mountains, and Ron with the keys from M03
        /// in his hand. One reaction, one practical next step; the keys go into the
        /// Granger that is taking them to Senora.
        /// </summary>
        private void PlayStrike()
        {
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var foundry = Ctx.Locations.Position("Base.CypressFlats");
            SpawnKeys();
            SpawnLaptop();
            var blocking = new SceneBlocking { DialogueAfterStep = 2 };
            if (guess != null && guess.Exists()) blocking.Then(new UsePhoneStep(guess, 2600));
            blocking.Then(new ShotStep(3400, null, foundry + new Vector3(-45f, -60f, 30f), null, foundry + new Vector3(0f, 0f, 4f), 0.8f, () => Detonate(foundry)));
            if (ice != null && ice.Exists()) blocking.Then(ShotStep.Low(3000, ice, 2.5f, 1.5f, 1.2f));
            if (gohan != null && gohan.Exists() && _laptop != null && _laptop.Exists()) blocking.Then(new InspectStep(gohan, _laptop.Position + new Vector3(0f, -0.8f, -1f), 2800, "WORLD_HUMAN_CLIPBOARD"));
            if (guess != null && guess.Exists())
            {
                if (_keys != null && _keys.Exists()) blocking.Then(new CarryPropStep(guess, _keys));
                blocking.Then(ShotStep.Low(3600, guess, 1.8f, 0.9f, 1.1f));
                if (_keys != null && _keys.Exists() && _granger != null && _granger.Exists()) blocking.Then(new StowPropStep(guess, _keys, _granger, new Vector3(0.45f, 0.9f, 0.55f)));
            }
            blocking.Then(new VerifySceneStep("The beach aftermath is complete", () =>
                _dropped && _landed && _arrived && guess != null && guess.Exists() && !guess.IsDead,
                () => _struck = true));
            var spec = new SceneSpec
            {
                RequiresCompletion = true,
                MissionId = Id, Phase = "strike", Title = "The foundry",
                Reason = "The foundry's alarm line on Ron's phone, then the strike itself at Cypress Flats and the column over the mountains from the beach: learned before anyone explains it. Ron with the three keys from M03 in his hand; one reaction, one next step; the keys go into the Granger.",
                Blocking = blocking
            };
            var lines = new[]
            {
                Ctx.Data?.Cue("M22_S1_02_ICE"),
                Ctx.Data?.Cue("M22_S1_03_GOHAN"),
                Ctx.Data?.Cue("M22_S1_04_GUESS"),
                Ctx.Data?.Cue("M22_S1_05_ICE")
            };
            if (!Ctx.Cutscenes.PlayStaged(spec, lines))
            {
                Logger.Warn("M22 strike scene did not play; the strike and the lines play directly.");
                PortHeist.RequireFallback(blocking, "The foundry aftermath");
                Say("M22_S1_02_ICE"); Say("M22_S1_03_GOHAN"); Say("M22_S1_04_GUESS"); Say("M22_S1_05_ICE");
            }
        }

        /// <summary>The detonation at the foundry's real coordinates behind a short fade; the fire and the column stand for as long as the engine keeps them.</summary>
        private void Detonate(Vector3 foundry)
        {
            try
            {
                GameUtils.FadeOut(400);
                World.AddExplosion(foundry, ExplosionType.Plane, 25f, 3f, null, true, false);
                World.AddExplosion(foundry + new Vector3(8f, 6f, 0f), ExplosionType.Tanker, 20f, 2.5f, null, true, false);
            }
            finally { GameUtils.FadeIn(1200); }
            GameUtils.PlayFrontendSound("Explosion_Textured", "GTAO_Speed_Convoy_Soundset");
            GameUtils.Subtitle("~r~The foundry alarm went dark. Aegis struck Cypress.", 7000);
            Logger.Info("M22: foundry strike staged at " + foundry + "; distant rendering requires live validation.");
        }

        private void ReleaseContainer()
        {
            if (_container == null || !_container.Exists()) return;

            Function.Call(Hash.DETACH_ENTITY, _container, true, true);
            _container.Position = _drop;
            _container.IsPositionFrozen = true;

            if (_cargobob != null && _cargobob.Exists()) _cargobob.EnginePowerMultiplier = 1f;

            GameUtils.Subtitle("~g~Thirty tons of bullion, sitting in four feet of Alamo water.", 6000);
        }

        /// <summary>The aftermath: the Granger and the lift on the beach, the three of them between them, the column to the south.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_granger != null && _granger.Exists())
                return new SceneBlocking().Then(new ShotStep(4500, _granger, new Vector3(-9f, 6f, 2.5f), _granger, new Vector3(0f, 0f, 0.8f), 1.2f));
            if (_cargobob == null || !_cargobob.Exists()) return null;
            return new SceneBlocking().Then(new ShotStep(4500, _cargobob, new Vector3(-14f, 8f, 3f), _cargobob, new Vector3(0f, 0f, 1f), 1.2f));
        }

        // ---------- world building ----------

        /// <summary>The loaded lift, airborne and held over the water south of the drop: Ron arrives by air, as he left the coast.</summary>
        private void SpawnLift()
        {
            if (PortHeist.IsContinuing(Ctx))
            {
                _cargobob = Track(Ctx.PortHeist.Require<Vehicle>("lift"));
                _container = Track(Ctx.PortHeist.Require<Prop>("bullion"));
                if (!PortHeistWorld.Attached(_container, _cargobob)) throw new System.InvalidOperationException("The bullion separated during the inland flight.");
                return;
            }
            var heliModel = new Model("cargobob");
            var containerModel = new Model("prop_container_01a");
            if (!GameUtils.RequestModel(heliModel)) return;

            _cargobob = Track(World.CreateVehicle(heliModel, _drop + new Vector3(-30f, -80f, 42f),
                DriveUpStep.HeadingBetween(_drop + new Vector3(-30f, -80f, 0f), _drop)));
            heliModel.MarkAsNoLongerNeeded();
            if (_cargobob == null || !_cargobob.Exists()) return;

            _cargobob.IsPersistent = true;
            _cargobob.EnginePowerMultiplier = 0.6f;

            var blip = Track(_cargobob.AddBlip());
            blip.Sprite = BlipSprite.Helicopter;
            blip.Color = BlipColor.Orange;
            blip.Name = "Bullion lift";

            if (!GameUtils.RequestModel(containerModel)) return;

            _container = Track(World.CreateProp(containerModel, _cargobob.Position - new Vector3(0f, 0f, 7f),
                false, false));
            containerModel.MarkAsNoLongerNeeded();
            if (_container == null || !_container.Exists()) return;

            _container.IsPersistent = true;
            Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY, _container, _cargobob, 0,
                0f, 0f, -6.5f, 0f, 0f, 0f, false, false, true, false, 2, true);
        }

        /// <summary>The crew's Granger on the road above the beach with Ice and Gohan in it: the transport M21 put them in, arriving.</summary>
        private void SpawnGranger()
        {
            if (PortHeist.IsContinuing(Ctx)) { _granger = Track(Ctx.PortHeist.Require<Vehicle>("granger")); return; }
            Vehicle granger = Ctx.Vans != null ? Ctx.Vans.Spawn(_road, Ctx.Locations.Heading("M22.RoadArrival")) : null;
            if (granger == null)
            {
                var model = new Model("granger");
                if (!GameUtils.RequestModel(model)) return;
                granger = World.CreateVehicle(model, _road, Ctx.Locations.Heading("M22.RoadArrival"));
                model.MarkAsNoLongerNeeded();
            }
            _granger = Track(granger);
            if (_granger == null || !_granger.Exists()) return;
            _granger.IsPersistent = true;
            _granger.IsEngineRunning = true;
        }

        /// <summary>The keys from M03. If the model is not there, the scene plays without the prop rather than not at all.</summary>
        private void SpawnKeys()
        {
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            if (guess == null || !guess.Exists()) return;
            var model = new Model(KeysModel);
            if (!GameUtils.RequestModel(model)) { Logger.Warn("M22: the keys model did not load; Ron's hands are empty."); return; }
            _keys = Track(World.CreateProp(model, guess.Position + new Vector3(0f, 0f, 1f), false, false));
            model.MarkAsNoLongerNeeded();
            if (_keys != null && _keys.Exists()) _keys.IsPersistent = true;
        }

        /// <summary>Gohan's laptop on the Granger's hood: the feed he reads the damage from.</summary>
        private void SpawnLaptop()
        {
            if (_granger == null || !_granger.Exists()) return;
            var model = new Model("prop_laptop_01a");
            if (!GameUtils.RequestModel(model)) return;
            _laptop = Track(World.CreateProp(model, _granger.Position + new Vector3(0f, 0f, 1.5f), false, false));
            model.MarkAsNoLongerNeeded();
            if (_laptop == null || !_laptop.Exists()) return;
            _laptop.IsPersistent = true;
            StowPropStep.Stow(_laptop, _granger, new Vector3(0f, 2.6f, 0.95f));
        }

        protected override void OnPassed()
        {
            // The container stays in the shallows; the Granger and the lift are how
            // the crew leaves for Senora.
            if (_container != null && _container.Exists()) Release(_container);
            if (_granger != null && _granger.Exists()) Release(_granger);
            if (_cargobob != null && _cargobob.Exists()) Release(_cargobob);
            PortHeist.RecordCargo(Ctx, "cargobob", "M22.Beach");
            if (Ctx.PortHeist != null)
            {
                PortHeist.RecordCargo(Ctx, "kraken", "M12.PierWatch");
                PortHeist.RecordCargo(Ctx, PortHeist.BullionCargo, "M22.AlamoDrop");
            }
        }

        protected override void OnCleanup()
        {
            Ctx.Crew.CompanionsHoldPosition = false;
            ReleaseLift();
            if (Status != MissionStatus.Passed && _container != null && _container.Exists())
            {
                Function.Call(Hash.DETACH_ENTITY, _container, true, true);
            }
        }
    }
}
