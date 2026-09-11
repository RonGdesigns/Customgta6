using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// SM03 — "Midnight Drift". Olympic Freeway industrial basin, 01:00.
    ///
    /// Guess's solo and the mission that has to prove the driving identity: a
    /// three-lap pink-slip circuit under the overpass, with two Marabunta racers who
    /// pull guns once they lose. Slipstream Reflex is the intended way to hold the
    /// hairpins, so the meter starts full.
    ///
    /// Seen, not told: KJ walking the prize and the coupe before the grid, with the
    /// warning about the racers said out loud; the rivals' guns shown as a change of
    /// plan after a legitimate result, not confused with losing; the prize driven to
    /// the chop bay, where it becomes the crew's next transmission, and KJ checked
    /// in with over the phone. KJ is a contact, outside the combat path.
    ///
    /// The ambush stage is deliberately either/or: shake the shooters or cross the
    /// line first. A race that can only be won by killing everyone is not a race.
    /// </summary>
    public sealed class SM03MidnightDrift : ComposedMission
    {
        private readonly List<Ped> _rivals = new List<Ped>();
        private readonly List<Vehicle> _rivalCars = new List<Vehicle>();

        private Ped _kj;
        private Vehicle _coupe;
        private Prop _prize;
        private readonly int[] _rivalCheckpoint = new int[2];
        private readonly int[] _rivalLaps = new int[2];
        private Vector3 _start;
        private Vector3 _bay;
        private List<Vector3> _circuit;
        private bool _gunsShown, _gunsCalled, _prizeHome;

        public override string Id => "SM03";
        public override string Title => "Midnight Drift";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SafehouseArrival;

        public Ped KJ => _kj;
        public Vehicle Coupe => _coupe;
        public Prop Prize => _prize;
        public bool GunsShown => _gunsShown;
        public bool PrizeHome => _prizeHome;

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            if (!MissionSites.Ground(Ctx.Locations, "M11.ChopShop")) return false;
            _start = Ctx.Locations.Position("SM03.StartLine");
            _bay = Ctx.Locations.Position("M11.ChopShop");
            _circuit = new List<Vector3>
            {
                Ctx.Locations.Position("SM03.Checkpoint1"),
                Ctx.Locations.Position("SM03.Checkpoint2"),
                Ctx.Locations.Position("SM03.Checkpoint3"),
                Ctx.Locations.Position("SM03.Checkpoint4")
            };

            if (!Ctx.Crew.DeploySolo(CrewSlot.Guess, _start + new Vector3(4f, 0f, 0f),
                    Ctx.Locations.Heading("SM03.StartLine")))
            {
                return false;
            }

            ApplyBibleSetting();
            Ctx.Abilities.Refill();

            if (!SpawnCoupe()) return false;
            SpawnKJ();
            SpawnRivals();
            SpawnPrize();
            if (_kj == null || !_kj.Exists() || _rivals.Count != 2) return false;
            PlayApproach();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Starting line",
                    new EnterVehicleObjective("Get in the drift coupe.", () => _coupe, VehicleSeat.Driver))
                .PlayedBy(CrewSlot.Guess)
                .WithCues("SM03_S1_01_GUESS");

            yield return new MissionStage("Three laps",
                    new RaceCheckpointObjective("Win the circuit — three laps.", _circuit, 14f, 3, () => _coupe),
                    new ProtectObjective("", () => _coupe, "The coupe is wrecked."))
                .PlayedBy(CrewSlot.Guess)
                .OnEnter(context => StartRivals())
                .WithCues("SM03_S1_02_GUESS");

            // A legitimate result first; then the guns, shown as the change of plan.
            yield return new MissionStage("They pulled guns",
                    new KillTargetsObjective("Stop the marked shooters OR drive the coupe to the yellow finish marker.", () => _rivals),
                    new DeliverVehicleObjective("Escape in the drift coupe to the marked finish, or stop the shooters.", () => _coupe, () => _start, 15f),
                    new ProtectObjective("", () => _coupe, "The coupe is wrecked."),
                    new ReactionTrigger(() => !_gunsShown && !Ctx.Cutscenes.IsActive, ShowGuns),
                    new ReactionTrigger(() => _gunsShown && !_gunsCalled && !Ctx.Cutscenes.IsActive, CallGuns))
                .AnyOf()
                .PlayedBy(CrewSlot.Guess)
                .OnEnter(context => ArmRivals());

            // The prize is earned, then delivered: the chop bay is where it becomes the crew's.
            yield return new MissionStage("The chop bay",
                    new DeliverVehicleObjective("Guess: drive the coupe and the prize to the Burro Heights chop bay.", () => _coupe, () => _bay, 25f),
                    new ProtectObjective("", () => _coupe, "The coupe is wrecked with the prize in it."))
                .PlayedBy(CrewSlot.Guess)
                .OnExit(context => PrizeDelivered())
                .AfterCues("SM03_S2_05_GUESS");
        }

        // ---------- beats ----------

        /// <summary>KJ walks the prize and the coupe; the rivals' Elegys on the grid; the warning said before the start.</summary>
        private void PlayApproach()
        {
            var blocking = new SceneBlocking();
            if (_kj != null && _kj.Exists() && _coupe != null && _coupe.Exists())
            {
                blocking.Then(new WalkToStep(_kj, _coupe.Position - _coupe.ForwardVector * 3.2f, 1.0f))
                    .Then(_prize != null && _prize.Exists() ? new InspectStep(_kj, _prize.Position, 2600) : (SceneStep)new WaitStep(2600, _kj))
                    .Then(new WalkToStep(_kj, _coupe.Position + new Vector3(2.4f, 0f, 0f), 1.0f))
                    .Then(ShotStep.Watching(2600, _kj, _coupe));
            }
            if (_rivalCars.Count > 0 && _rivalCars[0].Exists())
                blocking.Then(new ShotStep(3000, _rivalCars[0], new Vector3(-5f, 4f, 1.6f), _rivalCars[0], new Vector3(0f, 0f, 0.7f), 0.8f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "The grid",
                Reason = "KJ walks the prize crate on the coupe's tail and the coupe itself, and looks at the two tuned Elegys on the grid. The prize is real; the drivers are not clean. KJ stays off the grid.",
                Blocking = blocking
            }.With("KJ", _kj);
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("SM03 approach scene did not play; the grid stands on its own.");
        }

        private void ArmRivals()
        {
            foreach (var rival in _rivals)
            {
                if (rival == null || !rival.Exists()) continue;
                rival.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_CARTEL");
                rival.Task.VehicleShootAtPed(Game.Player.Character);
            }
        }

        /// <summary>The guns, shown once on the first rival: a change of plan after a legitimate result.</summary>
        private void ShowGuns()
        {
            _gunsShown = true;
            string line = Ctx.Data?.Cue("SM03_S2_03_ENEMY")?.Line ?? "We lost the race! Shoot his tires before he leaves!";
            Ped shooter = null;
            foreach (var rival in _rivals) if (rival != null && rival.Exists() && !rival.IsDead) { shooter = rival; break; }
            if (shooter != null) Ctx.Cutscenes.PlayMoment(Id, "They pulled guns", "RIVAL RACER", line, shooter);
            else Say("SM03_S2_03_ENEMY");
        }

        private void CallGuns()
        {
            _gunsCalled = true;
            Say("SM03_S2_04_GUESS");
        }

        /// <summary>The prize at the bay: recorded there for the fleet; the real reward said plainly.</summary>
        private void PrizeDelivered()
        {
            _prizeHome = true;
            ClearHeatIfSafe();
            Ctx.State?.SetCargo("racePrize", "M11.ChopShop");
            if (_coupe != null && _coupe.Exists()) { _coupe.IsEngineRunning = false; Release(_coupe); }
            if (_prize != null && _prize.Exists()) Release(_prize);
            // Awarded once by CampaignState.MarkComplete after the mission passes: the
            // cash and the race transmission the fleet garage fits to the next build.
            GameUtils.Subtitle("~g~Prize at the bay: $25,000 and the race transmission, fitted to the crew's next build. KJ gets the call.", 6000);
        }

        /// <summary>The aftermath: Ron out of the coupe at the bay, the prize on its tail.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_coupe == null || !_coupe.Exists()) return null;
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            var blocking = new SceneBlocking();
            if (guess != null && guess.Exists() && guess.IsInVehicle(_coupe)) blocking.Then(new ExitVehicleStep(guess));
            return blocking.Then(new ShotStep(4500, _coupe, new Vector3(-4.5f, -3.5f, 1.4f), _coupe, new Vector3(0f, -1.5f, 0.7f), 0.7f));
        }

        // ---------- world building ----------

        private void SpawnKJ()
        {
            var model = new Model("a_m_y_stbla_02");
            if (!GameUtils.RequestModel(model)) return;
            _kj = Track(World.CreatePed(model, _start + new Vector3(6f, 3f, 0f), 180f));
            model.MarkAsNoLongerNeeded();
            if (_kj == null || !_kj.Exists()) return;
            _kj.IsPersistent = true; _kj.BlockPermanentEvents = true;
            _kj.IsInvincible = true; _kj.RelationshipGroup = Ctx.Crew.CrewGroup;
            _kj.Task.StandStill(-1);
            var blip = Track(_kj.AddBlip());
            blip.Name = "KJ"; blip.Color = BlipColor.Purple; blip.IsShortRange = false;
        }

        /// <summary>The prize, real: a crate on the coupe's tail that rides with it.</summary>
        private void SpawnPrize()
        {
            var model = new Model("prop_box_wood02a");
            if (!GameUtils.RequestModel(model) || _coupe == null || !_coupe.Exists()) return;
            _prize = Track(World.CreateProp(model, _coupe.Position + new Vector3(0f, 0f, 2f), false, false));
            model.MarkAsNoLongerNeeded();
            if (_prize == null || !_prize.Exists()) { _prize = null; return; }
            _prize.IsPersistent = true;
            StowPropStep.Stow(_prize, _coupe, new Vector3(0f, -1.9f, 0.75f));
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (Status != MissionStatus.Running || Stage != 1) return;
            for (int i = 0; i < _rivals.Count; i++)
            {
                var rival = _rivals[i]; var car = _rivalCars[i];
                if (rival == null || !rival.Exists() || rival.IsDead || car == null || !car.Exists() || !car.IsDriveable) continue;
                if (car.Position.DistanceTo(_circuit[_rivalCheckpoint[i]]) > 16f) continue;
                _rivalCheckpoint[i]++;
                if (_rivalCheckpoint[i] == _circuit.Count)
                {
                    _rivalCheckpoint[i] = 0;
                    if (++_rivalLaps[i] >= 3) { Fail("A rival finished the circuit first."); return; }
                }
                rival.Task.DriveTo(car, _circuit[_rivalCheckpoint[i]], 8f, 30f, DrivingStyle.Rushed);
            }
        }

        private bool SpawnCoupe()
        {
            var model = new Model("futo");
            if (!GameUtils.RequestModel(model)) return false;

            _coupe = Track(World.CreateVehicle(model, _start, Ctx.Locations.Heading("SM03.StartLine")));
            model.MarkAsNoLongerNeeded();
            if (_coupe == null || !_coupe.Exists()) return false;

            _coupe.IsPersistent = true;
            _coupe.Mods.InstallModKit();
            // Drift setup keeps ordinary engine power and acceleration modifiers.
            _coupe.Mods[VehicleModType.Suspension].Index = 3;

            var blip = Track(_coupe.AddBlip());
            blip.Sprite = BlipSprite.PersonalVehicleCar;
            blip.Color = BlipColor.Orange;
            blip.Name = "Drift coupe";
            return true;
        }

        private void SpawnRivals()
        {
            var carModel = new Model("elegy2");
            var pedModel = new Model("g_m_y_salvaboss_01");
            if (!GameUtils.RequestModel(carModel) || !GameUtils.RequestModel(pedModel)) return;

            var marabunta = World.AddRelationshipGroup("BLOODLINES_RACERS"); // Neutral until the post-race ambush.

            for (int i = 0; i < 2; i++)
            {
                var car = Track(World.CreateVehicle(carModel, _start + new Vector3(-4f - i * 4f, 0f, 0f),
                    Ctx.Locations.Heading("SM03.StartLine")));
                if (car == null || !car.Exists()) continue;
                car.IsPersistent = true;

                var driver = Track(World.CreatePed(pedModel, car.Position, 0f));
                if (driver == null || !driver.Exists()) continue;

                driver.RelationshipGroup = marabunta;
                driver.IsPersistent = true;
                driver.BlockPermanentEvents = true;
                driver.DrivingStyle = DrivingStyle.Rushed;
                driver.Weapons.Give(WeaponHash.MicroSMG, 120, false, true);
                driver.Task.WarpIntoVehicle(car, VehicleSeat.Driver);

                _rivals.Add(driver);
                _rivalCars.Add(car);
            }

            carModel.MarkAsNoLongerNeeded();
            pedModel.MarkAsNoLongerNeeded();
        }

        private void StartRivals()
        {
            for (int i = 0; i < _rivals.Count; i++)
            {
                var rival = _rivals[i];
                var car = i < _rivalCars.Count ? _rivalCars[i] : null;
                if (rival == null || !rival.Exists() || car == null || !car.Exists()) continue;

                // They run the circuit rather than chasing: a rival that tails the player
                // is a pursuit, not a race.
                rival.Task.DriveTo(car, _circuit[0], 12f, 35f, DrivingStyle.Rushed);
            }
        }

        protected override void OnCleanup()
        {
            _rivals.Clear();
            _rivalCars.Clear();
        }
    }
}
