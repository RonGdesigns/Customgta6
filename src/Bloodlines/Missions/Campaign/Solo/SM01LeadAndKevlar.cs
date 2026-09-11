using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// SM01 — "Lead &amp; Kevlar". Terminal Island warehouse 4, 23:00, drizzle.
    ///
    /// Ice goes after Sergei — an arms broker who took his money and shipped him
    /// civilian brass — for the crates the crew was owed. No crew, no switching:
    /// solo missions are where a character's discipline stands alone, and the
    /// other two are voices on the radio.
    ///
    /// Seen, not told: Sergei and the crates on the warehouse floor before the
    /// side door, so the dispute is a real shipment and not a generic room; the
    /// broker alive with his hands up, and left that way, because the codes are
    /// what the job needs; the crates counted into Ice's own car; the material
    /// brought back to Ice's door, and the reward named for what it actually is.
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
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _warehouse = Ctx.Locations.Position("SM01.WarehouseGate");
            _office = Ctx.Locations.Position("SM01.SergeiOffice");
            _trunk = Ctx.Locations.Position("SM01.CrateLoad");
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
            PlayApproach();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Breach",
                    new ReachZoneObjective("Breach the side entrance.", () => _office, 30f, flat: true))
                .PlayedBy(CrewSlot.Ice)
                .OnExit(context =>
                {
                    foreach (var guard in _guards)
                    {
                        if (guard != null && guard.Exists()) guard.Task.FightAgainstHatedTargets(80f);
                    }
                })
                .WithCues("SM01_S1_01_ICE");

            yield return new MissionStage("Clear the floor",
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

            // Counted into his own car: two crates, seen going in.
            yield return new MissionStage("The crates",
                    new MissionInteraction("Ice: load the two marked crates into your car", () => _trunk, 2, 3.5f))
                .PlayedBy(CrewSlot.Ice)
                .OnExit(context => PlayLoading());

            // Back to his own door with the material; the reward is what the locker actually stocks.
            yield return new MissionStage("Bring it back",
                    new DeliverVehicleObjective("Ice: drive the crates back to your door.", () => _car, () => _home, 25f),
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

        /// <summary>Sergei and the crates on the floor, the guards, Ice's car at the gate: the shipment is real before the door.</summary>
        private void PlayApproach()
        {
            var blocking = new SceneBlocking();
            if (_sergei != null && _sergei.Exists()) blocking.Then(new ShotStep(3200, _sergei, new Vector3(-2.5f, -2.5f, 1.4f), _sergei, new Vector3(0f, 0f, 0.7f), 0.4f));
            if (_crates.Count > 0 && _crates[0].Exists()) blocking.Then(new ShotStep(3000, _crates[0], new Vector3(-2f, -2.5f, 1.2f), _crates[0], new Vector3(0f, 0f, 0.3f), 0.5f));
            blocking.Then(ShotStep.Wide(3000, _warehouse, 12f, 6f, 5f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "The shipment",
                Reason = "Sergei in his office, the two crates he shorted Ice on the floor beside it, six men on the warehouse floor, Ice's car at the gate. Ron and Gohan are on the radio, not here.",
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

        /// <summary>Two crates into the trunk, counted: the material seen going in.</summary>
        private void PlayLoading()
        {
            _loaded = true;
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            if (ice == null || !ice.Exists() || _car == null || !_car.Exists()) return;
            var rear = _car.Position - _car.ForwardVector * 3.6f;
            var blocking = new SceneBlocking();
            for (int i = 0; i < _crates.Count; i++)
            {
                var crate = _crates[i];
                if (crate == null || !crate.Exists()) continue;
                blocking.Then(new WalkToStep(ice, crate.Position + new Vector3(0.8f, 0f, 0f), 1.1f))
                    .Then(new CarryPropStep(ice, crate))
                    .Then(new WalkToStep(ice, rear, 1.1f))
                    .Then(new StowPropStep(ice, crate, _car, TrunkSlots[i % TrunkSlots.Length]));
            }
            blocking.Then(new ShotStep(2400, _car, new Vector3(-4f, -4f, 1.4f), _car, new Vector3(0f, -2f, 0.7f), 0.5f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "crates", Title = "Two crates",
                Reason = "Both crates go into Ice's car, one after the other, counted. The material is his now.",
                Blocking = blocking
            };
            var cue = Ctx.Data?.Cue("SM01_S2_05_ICE");
            if (!Ctx.Cutscenes.PlayStaged(spec, new[] { cue })) { Logger.Warn("SM01 crates scene did not play; the crates are placed in the car directly."); blocking.Complete(); }
            GameUtils.Subtitle("~g~2 of 2 crates in the car.", 4000);
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

            _sergei = Track(World.CreatePed(model, _office, 180f));
            model.MarkAsNoLongerNeeded();
            if (_sergei == null || !_sergei.Exists()) return;

            _sergei.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_TRAFFIC");
            _sergei.IsInvincible = true;
            _sergei.IsPersistent = true;
            _sergei.BlockPermanentEvents = true;
            _sergei.Armor = 50;
            _sergei.Weapons.Give(WeaponHash.Pistol, 60, true, true);
            _sergei.Task.StartScenario("WORLD_HUMAN_CLIPBOARD", _office, 180f);

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

                var offset = new Vector3(-6f + i * 3f, 8f + (i % 3) * 6f, 0f);
                var guard = World.CreatePed(model, _warehouse + offset, 0f);
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

        /// <summary>The two crates Sergei shorted him: real props on the floor by the office.</summary>
        private void SpawnCrates()
        {
            var model = new Model("prop_box_ammo03a");
            if (!GameUtils.RequestModel(model)) return;
            for (int i = 0; i < 2; i++)
            {
                var crate = Track(World.CreateProp(model, _trunk + new Vector3(i * 1.2f, 1.5f, 0f), true, false));
                if (crate == null || !crate.Exists()) continue;
                crate.IsPersistent = true;
                _crates.Add(crate);
            }
            model.MarkAsNoLongerNeeded();
        }

        /// <summary>Ice's own car at the gate: where the crates go, and how they get home.</summary>
        private void SpawnCar()
        {
            var model = new Model("baller");
            if (!GameUtils.RequestModel(model)) return;
            _car = Track(World.CreateVehicle(model, _warehouse + new Vector3(-6f, -6f, 0f), Ctx.Locations.Heading("SM01.WarehouseGate")));
            model.MarkAsNoLongerNeeded();
            if (_car == null || !_car.Exists()) return;
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
