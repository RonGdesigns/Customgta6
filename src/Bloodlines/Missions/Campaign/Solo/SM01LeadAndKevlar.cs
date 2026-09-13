using System;
using System.Collections.Generic;
using System.Linq;
using GTA.Native;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// SM01 — "Lead &amp; Kevlar". Terminal Island, 23:00, drizzle.
    ///
    /// Ice goes after Sergei — an arms broker who took his money and shipped him
    /// civilian brass — for the crates the crew was owed. No crew, no switching:
    /// solo missions are where a character's discipline stands alone, and the
    /// other two are voices on the radio.
    ///
    /// Seen, not told: Sergei and the cases in the open lane before the fight, so
    /// the dispute is a real shipment and not a generic room; the broker alive
    /// with his hands up, and left that way, because the codes are what the job
    /// needs; the cases counted into Ice's own car; the material brought back to
    /// Ice's door, and the reward named for what it actually is.
    ///
    /// Every position here is a key Ron placed in game on September 13, 2026. The
    /// bounded placement check still runs on them, but a refusal is a warning and
    /// the authored point stands: a checked layout must not stop the mission from
    /// starting, and the loading must end with the cases in the car whether the
    /// scene is watched, skipped, or falls over on a step.
    /// </summary>
    public sealed class SM01LeadAndKevlar : ComposedMission
    {
        private static readonly string[] GuardModels = { "g_m_m_armboss_01", "g_m_y_strpunk_01", "s_m_y_dealer_01" };
        private static readonly Vector3[] TrunkSlots = { new Vector3(0f, -2.0f, 0.55f), new Vector3(0f, -2.0f, 0.95f) };

        private readonly List<Ped> _guards = new List<Ped>();
        private readonly List<Prop> _crates = new List<Prop>();

        private Ped _sergei;
        private Vehicle _car;
        private Vector3 _warehouse;
        private Vector3 _office;
        private Vector3 _trunk;
        private Vector3 _home;
        private bool _codesGiven, _loaded;
        private int _loadingSequence;
        private bool _loadingStarted;
        public IReadOnlyList<Ped> Guards => _guards;

        public override string Id => "SM01";
        public override string Title => "Lead & Kevlar";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SafehouseArrival;

        public Ped Sergei => _sergei;
        public Vehicle Car => _car;
        public IReadOnlyList<Prop> Crates => _crates;
        public bool CodesGiven => _codesGiven;
        public bool Loaded => _loaded;

        protected override bool Setup()
        {
            _warehouse = Site("SM01.WarehouseGate");
            _office = Site("SM01.SergeiOffice");
            _trunk = Site("SM01.CrateLoad");
            var door = Ctx.Locations.Get(ApartmentTiers.For(CrewSlot.Ice, ApartmentTier.Starter).EntranceKey);
            _home = door != null ? door.Position : _warehouse;

            if (!Ctx.Crew.DeploySolo(CrewSlot.Ice, _warehouse, Ctx.Locations.Heading("SM01.WarehouseGate")))
            {
                return false;
            }

            ApplyBibleSetting();
            Game.Player.Character.Weapons.Give(WeaponHash.PumpShotgun, 120, true, true);

            SpawnSergei();
            SpawnGuards();
            SpawnCrates();
            SpawnCar();
            if (!RequireAssets(_sergei, _car)) return false;
            if (_guards.Count != 6 || _crates.Count != 2) return false;
            RequireAsset(_car, "Ice's loading vehicle was lost.");
            foreach (var crate in _crates) RequireAsset(crate, "A required ammunition case was lost.");
            PlayApproach();
            return true;
        }

        /// <summary>
        /// A placed key, checked for standing space on its own level. The check
        /// refusing is logged, and the point Ron captured is used as it is.
        /// </summary>
        private Vector3 Site(string key)
        {
            if (Ctx.Locations.Get(key) == null) throw new InvalidOperationException("Missing placement: " + key);
            BoundedPlacement.TryPed(Ctx.Locations, key, out var point, BoundedPlacement.OutsideSoloFreight);
            return point;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Breach",
                    new ReachZoneObjective("Ice: enter the open loading lane and confront Sergei's men.", () => _office, 17f, flat: true))
                .PlayedBy(CrewSlot.Ice)
                .OnExit(context =>
                {
                    foreach (var guard in _guards)
                    {
                        if (guard != null && guard.Exists()) guard.Task.FightAgainstHatedTargets(80f);
                    }
                })
                .WithCues("SM01_S1_01_ICE");

            yield return new MissionStage("Clear the loading lane",
                    new KillTargetsObjective("Clear Sergei's men.", () => _guards))
                .PlayedBy(CrewSlot.Ice)
                .WithCues("SM01_S1_02_ICE");

            // The broker alive, hands up: the codes are the job, not his life.
            yield return new MissionStage("Sergei",
                    new MissionInteraction("Ice: approach Sergei and demand the crate codes", () => SergeiPosition(), 4, 4f),
                    new ReactionTrigger(() => _sergei == null || !_sergei.Exists() || _sergei.IsDead, () => Fail("Sergei died with the crate codes. The job needed him talking.")))
                .PlayedBy(CrewSlot.Ice)
                .OnEnter(context =>
                {
                    if (_sergei != null && _sergei.Exists()) { _sergei.IsInvincible = false; _sergei.Task.HandsUp(60000); }
                })
                .OnExit(context => CodesGivenNow())
                .WithCues("SM01_S2_03_ENEMY", "SM01_S2_04_ICE");

            // Counted into his own car: two cases, seen going in.
            yield return new MissionStage("The crates",
                    new MissionInteraction("Ice: collect the two cases at the outside loading point", () => _trunk, 2, 3.5f))
                .PlayedBy(CrewSlot.Ice)
                .OnExit(context => PlayLoading());

            // Back to his own door with the material; the reward is what the locker actually stocks.
            yield return new MissionStage("Bring it back",
                    new DeliverVehicleObjective("Ice: drive the crates back to your door.", () => _car, () => _home, 25f),
                    new ConditionObjective("Secure both cases at your door and lose the police.", () =>
                        _loaded && CargoAttached() && Game.Player.WantedLevel == 0 &&
                        Game.Player.Character.IsInVehicle(_car) && GameUtils.IsWithinFlat(_car.Position, _home, 25f) && _car.Speed < 2f),
                    new ProtectObjective("", () => _car, "The car went with the crates in it."))
                .PlayedBy(CrewSlot.Ice)
                .OnExit(context =>
                {
                    ClearHeatIfSafe();
                    Ctx.State?.SetCargo("sergeiCrates", ApartmentTiers.For(CrewSlot.Ice, ApartmentTier.Starter).EntranceKey);
                    // Awarded once by CampaignState.MarkComplete after the mission passes:
                    // the Pump Shotgun Mk II in Ice's locker, and the supply line that doubles
                    // his rifle ammunition on every restock. Nothing here claims more.
                    GameUtils.Subtitle("~g~Crates home. From the next restock Ice's locker stocks the Pump Shotgun Mk II and issues double rifle ammunition.", 6000);
                })
                .AfterCues("SM01_S2_05_ICE");
        }

        // ---------- beats ----------

        /// <summary>Sergei and the cases in the lane, the guards, Ice's car clear of the rows: the shipment is real before the fight.</summary>
        private void PlayApproach()
        {
            var blocking = new SceneBlocking();
            if (_sergei != null && _sergei.Exists()) blocking.Then(new ShotStep(3200, _sergei, new Vector3(-2.5f, -2.5f, 1.4f), _sergei, new Vector3(0f, 0f, 0.7f), 0.4f));
            if (_crates.Count > 0 && _crates[0].Exists()) blocking.Then(new ShotStep(3000, _crates[0], new Vector3(-2f, -2.5f, 1.2f), _crates[0], new Vector3(0f, 0f, 0.3f), 0.5f));
            blocking.Then(ShotStep.Wide(3000, _warehouse, 12f, 6f, 5f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "The shipment",
                Reason = "Sergei in his office, the two crates he shorted Ice on the floor beside it, six men in the exterior lane, Ice's car clear of the freight rows. Ron and Gohan are on the radio, not here.",
                Blocking = blocking
            }.With("SERGEI", _sergei);
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("SM01 approach scene did not play; the warehouse stands on its own.");
        }

        /// <summary>The codes are given: Sergei is done with, alive. He runs; the crates are the point.</summary>
        private void CodesGivenNow()
        {
            _codesGiven = true;
            if (_sergei == null || !_sergei.Exists() || _sergei.IsDead) return;
            _sergei.IsInvincible = true;
            _sergei.Task.ClearAll();
            _sergei.Task.FleeFrom(Game.Player.Character);
            GameUtils.Subtitle("~y~Codes given. Sergei runs; the crates are what Ice came for.", 4000);
        }

        /// <summary>Both cases physically attached to the car that is still drivable.</summary>
        private bool CargoAttached() => _car != null && _car.Exists() && _car.IsDriveable &&
            _crates.Count == 2 && _crates.All(crate => crate != null && crate.Exists() &&
                Function.Call<bool>(Hash.IS_ENTITY_ATTACHED_TO_ENTITY, crate, _car));

        /// <summary>
        /// The cases go in the car no matter how the scene went: whatever the walk
        /// and carry did not attach is stowed directly. True only when both are
        /// really attached; nothing is claimed for a car that is gone.
        /// </summary>
        private bool EnsureCargo()
        {
            if (_car == null || !_car.Exists() || !_car.IsDriveable) return false;
            for (int i = 0; i < _crates.Count && i < TrunkSlots.Length; i++)
            {
                var crate = _crates[i];
                if (crate == null || !crate.Exists()) return false;
                if (!Function.Call<bool>(Hash.IS_ENTITY_ATTACHED_TO_ENTITY, crate, _car)) StowPropStep.Stow(crate, _car, TrunkSlots[i]);
            }
            return CargoAttached();
        }

        private void MarkLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            GameUtils.Subtitle("~g~2 of 2 cases secured in the car.", 4000);
        }

        /// <summary>
        /// Two cases into the car, counted: Ice walks to each, carries it to the back
        /// of the car and sets it in. Peds path around what is in the lane, so no
        /// straight-line corridor is tested first; the only thing verified is the
        /// result, and a result the scene could not reach is reached by hand.
        /// </summary>
        private void PlayLoading()
        {
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            if (ice == null || !ice.Exists() || _car == null || !_car.Exists())
            { Fail("Ice or his loading vehicle is missing."); return; }
            _loadingStarted = true;
            BoundedPlacement.TryPedAt(_car.Position - BoundedPlacement.Forward(_car.Heading) * 4f, "SM01 vehicle rear",
                out var rear, BoundedPlacement.OutsideSoloFreight);
            var blocking = new SceneBlocking { DialogueAfterStep = 9 };
            for (int i = 0; i < _crates.Count; i++)
            {
                var crate = _crates[i];
                var pickup = Site("SM01.Case" + (i + 1) + "Access");
                blocking.Then(new WalkToStep(ice, pickup, 1.1f))
                    .Then(new CarryPropStep(ice, crate))
                    .Then(new WalkToStep(ice, rear, 1.1f))
                    .Then(new StowPropStep(ice, crate, _car, TrunkSlots[i]));
            }
            blocking.Then(new VerifySceneStep("Both SM01 cases secured in Ice's car", EnsureCargo, MarkLoaded))
                .Then(new ShotStep(2400, _car, new Vector3(-4f, -4f, 1.4f), _car, new Vector3(0f, -2f, .7f), .5f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "crates", Title = "Two cases",
                Reason = "Ice collects two portable cases from the outside loading point and secures them in his car. Nobody enters the closed freight containers.",
                Blocking = blocking
            };
            _loadingSequence = Ctx.Cutscenes.FinishedSequence;
            var cue = Ctx.Data?.Cue("SM01_S2_05_ICE");
            if (!Ctx.Cutscenes.PlayStaged(spec, new[] { cue }))
            {
                Logger.Warn("SM01 crates scene did not play; the cases are secured in the car directly.");
                if (EnsureCargo()) MarkLoaded();
                else Fail("The ammunition cases could not be secured in Ice's car.");
            }
        }

        protected override void OnUpdate()
        {
            if (_loadingStarted && !_loaded && !Ctx.Cutscenes.IsActive && Ctx.Cutscenes.FinishedSequence != _loadingSequence)
            {
                // The scene ended before its verified step: canceled, or a walk or
                // carry fell over and the rest was dropped. The cases still go in.
                if (EnsureCargo()) MarkLoaded();
                else { Fail("The ammunition cases could not be secured in Ice's car."); return; }
            }
            if (_loaded && !CargoAttached()) { Fail("The ammunition cases came loose or were lost."); return; }
            base.OnUpdate();
        }

        /// <summary>The aftermath: the car at Ice's door with the crates in it.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_car == null || !_car.Exists()) return null;
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var blocking = new SceneBlocking();
            if (ice != null && ice.Exists() && ice.IsInVehicle(_car)) blocking.Then(new ExitVehicleStep(ice));
            return blocking.Then(new ShotStep(4500, _car, new Vector3(-4.5f, 3f, 1.5f), _car, new Vector3(0f, 0f, 0.7f), 0.8f));
        }

        // ---------- world building ----------

        private Vector3 SergeiPosition() => _sergei != null && _sergei.Exists() ? _sergei.Position : _office;

        private void SpawnSergei()
        {
            var model = new Model("g_m_m_armboss_01");
            if (!GameUtils.RequestModel(model)) return;

            _sergei = Track(World.CreatePed(model, _office, Ctx.Locations.Heading("SM01.SergeiOffice")));
            model.MarkAsNoLongerNeeded();
            if (_sergei == null || !_sergei.Exists()) return;

            _sergei.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_TRAFFIC");
            _sergei.IsInvincible = true;
            _sergei.IsPersistent = true;
            _sergei.BlockPermanentEvents = true;
            _sergei.Armor = 50;
            _sergei.Weapons.Give(WeaponHash.Pistol, 60, true, true);
            _sergei.Task.StartScenario("WORLD_HUMAN_CLIPBOARD", _office, Ctx.Locations.Heading("SM01.SergeiOffice"));

            var blip = Track(_sergei.AddBlip());
            blip.Sprite = BlipSprite.Enemy;
            blip.Color = BlipColor.Red;
            blip.Name = "Sergei";
        }

        private void SpawnGuards()
        {
            var cartel = World.AddRelationshipGroup("BLOODLINES_CARTEL");

            for (int i = 0; i < 6; i++)
            {
                var model = new Model(GuardModels[i % GuardModels.Length]);
                if (!GameUtils.RequestModel(model)) continue;

                string key = "SM01.Guard" + (i + 1);
                var post = Site(key);
                var guard = World.CreatePed(model, post, Ctx.Locations.Heading(key));
                model.MarkAsNoLongerNeeded();
                if (guard == null || !guard.Exists()) continue;

                guard.RelationshipGroup = cartel;
                guard.IsPersistent = true;
                guard.BlockPermanentEvents = true;
                guard.Accuracy = 30;
                guard.Weapons.Give(i % 2 == 0 ? WeaponHash.MicroSMG : WeaponHash.Pistol, 150, true, true);
                guard.Task.GuardCurrentPosition();

                _guards.Add(Track(guard));
            }
        }

        /// <summary>The two cases Sergei shorted him: real props at the outside loading point.</summary>
        private void SpawnCrates()
        {
            var model = new Model("prop_box_ammo03a");
            if (!GameUtils.RequestModel(model)) return;
            for (int i = 0; i < 2; i++)
            {
                var point = Site("SM01.Case" + (i + 1));
                var crate = Track(World.CreateProp(model, point - new Vector3(0, 0, model.Dimensions.Item1.Z), false, false));
                if (crate == null || !crate.Exists()) continue;
                crate.IsPersistent = true; crate.IsPositionFrozen = true;
                Function.Call(Hash.SET_ENTITY_COLLISION, crate, false, false);
                _crates.Add(crate);
            }
            model.MarkAsNoLongerNeeded();
        }

        /// <summary>Ice's own car where Ron parked it: where the cases go, and how they get home.</summary>
        private void SpawnCar()
        {
            var model = new Model("baller");
            if (!GameUtils.RequestModel(model)) return;
            if (Ctx.Locations.Get("SM01.Car") == null) throw new InvalidOperationException("Missing placement: SM01.Car");
            bool placed = BoundedPlacement.TryVehicle(Ctx.Locations, "SM01.Car", model, out var spot,
                allowed: BoundedPlacement.OutsideSoloFreight, departureMeters: 6f);
            _car = Track(World.CreateVehicle(model, spot, Ctx.Locations.Heading("SM01.Car")));
            model.MarkAsNoLongerNeeded();
            if (_car == null || !_car.Exists()) return;
            if (!placed) GameUtils.SetOnGround(_car);
            _car.IsPersistent = true;
            _car.IsEngineRunning = false;
            var blip = Track(_car.AddBlip());
            blip.Sprite = BlipSprite.PersonalVehicleCar;
            blip.Color = BlipColor.Blue;
            blip.Name = "Ice's car";
        }

        protected override void OnPassed()
        {
            if (_car != null && _car.Exists()) Release(_car);
            foreach (var crate in _crates) if (crate != null && crate.Exists()) Release(crate);
        }

        protected override void OnCleanup()
        {
            if (_sergei != null && _sergei.Exists()) _sergei.IsInvincible = false;
            _guards.Clear();
            _crates.Clear();
        }
    }
}
