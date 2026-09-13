using System;
using System.Linq;
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
    /// M08 — "Supply &amp; Sever". Elysian Island, 16:45, overcast.
    ///
    /// The two turbine engines the whole fleet is built around. Gohan buys a
    /// window on the cameras, Ice drops the sentries, Guess moves both crates onto
    /// the flatbed with the forklift, and an Aegis technical turns up between
    /// crate one and crate two.
    ///
    /// Seen, not told: the two crates, the flatbed, the camera room and the
    /// sentries before anyone moves; each crate lifted on the forks, driven to the
    /// bed and set down, counted; the technical coming up the ramp before it is a
    /// fight; the crates delivered to a temporary stash and recorded there, so
    /// M09 can find them where they were left and M10 can collect the same
    /// shipment. Nothing installs an engine; that is M11's.
    ///
    /// The camera loop is a window, not a switch: it runs out. The forklift is
    /// driven by the player for pickup and delivery. Cargo rides visually without
    /// fork collisions; a stopped delivery verifies its attachment to the bed.
    /// </summary>
    public sealed class M08SupplyAndSever : ComposedMission
    {
        public const int LoopSeconds = 420;
        private static readonly Vector3 ForkOffset = new Vector3(0f, 1.6f, 0.75f);
        private static readonly Vector3[] BedSlots = { new Vector3(0f, -0.9f, 1.05f), new Vector3(0f, -3.1f, 1.05f) };

        private readonly List<Ped> _sentries = new List<Ped>();
        private readonly List<Prop> _crates = new List<Prop>();

        private Prop _cameraPanel, _securityCabin;
        public Prop SecurityCabin => _securityCabin;
        public Prop CameraPanel => _cameraPanel;
        private int _nextDefense;
        private bool _liftHeld;
        private Vehicle _hauler;
        private Vehicle _forklift;
        private Vehicle _granger;
        private Vehicle _technical;
        private readonly List<Ped> _responseCrew = new List<Ped>();
        private ForkliftDeliveryObjective _crateTwo;
        private bool _alerted;
        private Vector3 _gate;
        private Vector3 _cameras;
        private Vector3 _padOne;
        private Vector3 _padTwo;
        private Vector3 _stash;
        private int _loopStarted;
        private int _loaded;
        private bool _technicalShown, _technicalDown, _crewCalled, _stashed;

        public override string Id => "M08";
        public override string Title => "Supply & Sever";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;

        public Vehicle Hauler => _hauler;
        public Vehicle Forklift => _forklift;
        public Vehicle Granger => _granger;
        public Vehicle Technical => _technical;
        public IReadOnlyList<Prop> Crates => _crates;
        public int Loaded => _loaded;
        public bool TechnicalShown => _technicalShown;
        public bool Stashed => _stashed;
        public bool LoopRunning => _loopStarted > 0;

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _gate = Ctx.Locations.Position("M08.WarehouseGate");
            _cameras = Ctx.Locations.Position("M08.CameraRoom");
            _padOne = Ctx.Locations.Position("M08.CratePadOne");
            _padTwo = Ctx.Locations.Position("M08.CratePadTwo");
            _stash = Ctx.Locations.Position("M08.Connector");

            if (!Ctx.Crew.Deploy(CrewSlot.Gohan, _gate, 0f)) return false;

            ApplyBibleSetting();
            Game.Player.Character.Weapons.Give(WeaponHash.APPistol, 120, false, true);

            SpawnCameraPanel();
            SpawnSentries();
            SpawnForklift();
            SpawnHauler();
            SpawnCrates();
            SpawnGranger();
            if (!RequireAssets(_hauler, _forklift, _cameraPanel, _securityCabin) || _crates.Count != 2) return false;
            Station(CrewSlot.Gohan, _cameras + new Vector3(0f, -12f, 0f));
            Station(CrewSlot.Ice, _gate + new Vector3(-18f, 0f, 0f));
            Station(CrewSlot.Guess, _padOne + new Vector3(-20f, 0f, 0f));
            Ctx.Crew.PedFor(CrewSlot.Ice).Weapons.Give(WeaponHash.RPG, 8, false, false);
            Ctx.Crew.CompanionsHoldPosition = true;
            PlayApproach();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Loop the cameras",
                    new MissionInteraction("Gohan: use the security cabinet outside the port security cabin to loop CCTV. Press E / D-pad Right.", () => _cameras, 5, 2f, animation: M07WiretapWaltz.RelayAnimation))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(context =>
                {
                    _loopStarted = Game.GameTime;
                    GameUtils.Subtitle("~g~Camera loop is running: " + LoopSeconds + " seconds. Ice, clear the marked sentries.", 4000);
                })
                .WithCues("M08_S1_01_GUESS")
                .AfterCues("M08_S1_02_GOHAN");

            yield return new MissionStage("Drop the sentries",
                    new KillTargetsObjective("Ice — drop the sentries at the marked warehouse posts.", () => _sentries),
                    new ReactionTrigger(() => !_alerted && _sentries.Any(s => s != null && s.Exists() && (s.IsDead || s.IsInCombat)), AlertSentries),
                    LoopWindow())
                .OwnedBy(CrewSlot.Ice);

            yield return new MissionStage("Take the forklift",
                    new EnterVehicleObjective("Guess — take the forklift.", () => _forklift, VehicleSeat.Driver),
                    LoopWindow())
                .OwnedBy(CrewSlot.Guess);

            // The forks under the crate is the whole action: drive in, stop, and the
            // crate is on the forks. No button, no physics lift (Ron, September 11).
            yield return new MissionStage("Crate one",
                    Delivery(0, "first turbine crate"),
                    LoopWindow())
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context =>
                {
                    SpawnTechnical();

                });

            // The technical is called over radio without taking control. Ice handles it;
            // Ron finishes the loading. Either brother's job can be done first. The
            // owners are named one by one: a stage with none of its own inherits the
            // last owner (Ron) for every job, and the fight demanded Ron too. The
            // forks are Ron's; the technical is Ice's in name and anyone's kill.
            _crateTwo = Delivery(1, "second crate");
            yield return new MissionStage("Crate two, the technical",
                    _crateTwo,
                    new DestroyVehicleObjective("Ice — put the Aegis technical down.", () => _technical).ByAnyone(),
                    new ReactionTrigger(() => !_technicalShown && !Ctx.Cutscenes.IsActive, ShowTechnical),
                    new ReactionTrigger(() => !_technicalDown && _technical != null && _technical.Exists() && _technical.IsDead, TechnicalDown),
                    new ConditionObjective("Secure both turbine crates on the flatbed.", () => _loaded == 2),
                    LoopWindow());

            yield return new MissionStage("Take the hauler",
                    new EnterVehicleObjective("Guess — take the flatbed. Ice rides with you; Gohan brings the Granger.", () => _hauler, VehicleSeat.Driver),
                    new ProtectObjective("", () => _hauler, "The hauler and the engines are gone."),
                    new ReactionTrigger(() => !_crewCalled && Game.Player.Character.IsInVehicle(_hauler), CallCrewAboard))
                .OwnedBy(CrewSlot.Guess);

            yield return new MissionStage("Lose the police",
                    new LoseWantedObjective("Lose the police before the stash."),
                    new ProtectObjective("", () => _hauler, "The hauler and the engines are gone."));

            yield return new MissionStage("The stash",
                    new DeliverVehicleObjective("Guess: bring the loaded flatbed to the connector stash.", () => _hauler, () => _stash, 25f),
                    new ProtectObjective("", () => _hauler, "The hauler and the engines are gone."))
                .OnExit(context => Stash())
                .WithCues("M08_S2_05_GUESS");
        }

        // ---------- beats ----------

        /// <summary>The two crates, the flatbed, the camera room and the sentries: the job seen before anyone moves.</summary>
        private void PlayApproach()
        {
            var blocking = new SceneBlocking()
                .Then(new ShotStep(3200, null, _padOne + new Vector3(-6f, -7f, 2.4f), null, (_padOne + _padTwo) * 0.5f + new Vector3(0f, 0f, 0.6f), 0.9f))
                .Then(_hauler != null && _hauler.Exists()
                    ? new ShotStep(3000, _hauler, new Vector3(-7f, 4f, 2.2f), _hauler, new Vector3(0f, -1.5f, 1f), 1.0f)
                    : new ShotStep(3000, null, _padOne + new Vector3(-9f, 3f, 2f), null, _padOne, 0f))
                .Then(new ShotStep(3000, null, _cameras + new Vector3(-5f, -9f, 2.2f), null, _cameras + new Vector3(0f, 0f, 1f), 0.6f))
                .Then(new ShotStep(3000, null, _gate + new Vector3(-8f, 2f, 2.4f), null, _gate + new Vector3(8f, 12f, 1f), 0.8f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "The warehouse",
                Reason = "Two turbine crates on their pads, the flatbed that takes them, the camera room Gohan opens and the sentries on the gate: the whole job in four shots.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M08 approach scene did not play; the warehouse stands on its own.");
        }

        /// <summary>
        /// A playable pickup and delivery, counted only once attachment succeeds.
        /// </summary>
        private ForkliftDeliveryObjective Delivery(int index, string name) =>
            new ForkliftDeliveryObjective("Guess: " + name, () => _crates[index], () => _forklift, () => _hauler,
                ForkOffset, BedSlots[index], () => { _loaded = index + 1; if (index == 0) Say("M08_SCENE_LOADING_01_GUESS"); else Say("M08_SCENE_LOADING_02_GUESS"); })
            { RequiredCharacter = CrewSlot.Guess };

        private void ShowTechnical()
        {
            _technicalShown = true;
            // Keep the forklift and the battle playable while the warning is heard.
            Say("M08_S2_03_GUESS");
        }

        private void DefendLoadingCrew()
        {
            if (CurrentStage < 1 || CurrentStage > 4 || Game.GameTime < _nextDefense) return;
            _nextDefense = Game.GameTime + 2000;
            var threats = _sentries.Concat(_responseCrew).Where(p => p != null && p.Exists() && !p.IsDead).ToList();
            if (!_alerted && !threats.Any(p => p.IsInCombat)) return;
            foreach (var hero in Protagonist.All)
            {
                if (hero.Slot == Ctx.Crew.ActiveSlot) continue;
                var ped = Ctx.Crew.PedFor(hero.Slot);
                if (ped == null || !ped.Exists() || ped.IsDead || ped.IsInVehicle()) continue;
                var enemy = threats.Where(p => p.Position.DistanceTo(ped.Position) < 160f).OrderBy(p => p.Position.DistanceTo(ped.Position)).FirstOrDefault();
                if (enemy != null) { Ctx.Crew.CompanionAI.TakeControl(hero.Slot); ped.Task.FightAgainst(enemy); }
                else if (ped.IsInCombat) ped.Task.GuardCurrentPosition();
            }
        }

        private void TechnicalDown()
        {
            _technicalDown = true;
            Say("M08_S2_04_ICE");
        }

        /// <summary>Ice into the flatbed's other seat; Gohan leaves his panel for the Granger and follows.</summary>
        private void CallCrewAboard()
        {
            _crewCalled = true;
            Ctx.Crew.CompanionsHoldPosition = false;
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            if (ice != null && ice.Exists() && _hauler != null && _hauler.Exists())
            {
                Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Ice);
                ice.Task.EnterVehicle(_hauler, VehicleSeat.RightFront, 20000, 2f, EnterVehicleFlags.None);
            }
            if (gohan != null && gohan.Exists() && _granger != null && _granger.Exists())
            {
                Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Gohan);
                gohan.Task.EnterVehicle(_granger, VehicleSeat.Driver, 20000, 2f, EnterVehicleFlags.None);
            }
            GameUtils.Subtitle("~y~Ice is boarding. Gohan takes the Granger behind you.", 4000);
        }

        /// <summary>The crates stay where they are put: the stash is recorded for M09 and M10, the flatbed locked.</summary>
        private void Stash()
        {
            _stashed = true;
            Ctx.State?.SetCargo("turbineEngines", "M08.Connector");
            // The flatbed and its crates stay in the world where they were left.
            if (_hauler != null && _hauler.Exists())
            {
                _hauler.LockStatus = VehicleLockStatus.CannotEnter;
                _hauler.IsEngineRunning = false;
                Release(_hauler);
            }
            foreach (var crate in _crates) if (crate != null && crate.Exists()) Release(crate);
            GameUtils.Subtitle("~g~Both crates at the stash. They stay here until the run north; M11 talks horsepower.", 6000);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (CurrentStage >= 2 && CurrentStage <= 4)
            {
                var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
                if (Ctx.Crew.ActiveSlot == CrewSlot.Guess) _liftHeld = false;
                else if (guess != null && guess.Exists() && guess.IsInVehicle(_forklift))
                {
                    Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Guess);
                    if (!_liftHeld) { Function.Call(Hash.TASK_VEHICLE_TEMP_ACTION, guess, _forklift, 27, 2000); _liftHeld = true; }
                }
            }
            DefendLoadingCrew();
            if (_loopStarted > 0 && Stage >= 1 && Stage <= 4 && !Ctx.Cutscenes.IsActive)
            {
                int left = LoopSeconds - (Game.GameTime - _loopStarted) / 1000;
                if (left >= 0) GameUtils.Subtitle("~y~Camera loop: " + left + "s", 500);
            }
            // Re-arm the NPC role after a switch, but never task the active player.
            if (Ctx.Crew.ActiveSlot == CrewSlot.Gohan) _gohanFollowing = false;
            if (_crewCalled && Ctx.Crew.ActiveSlot != CrewSlot.Gohan && !_gohanFollowing && _granger != null && _granger.Exists() && _hauler != null && _hauler.Exists())
            {
                var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
                if (gohan != null && gohan.Exists() && gohan.IsInVehicle(_granger))
                {
                    _gohanFollowing = true;
                    Function.Call(Hash.TASK_VEHICLE_FOLLOW, gohan, _granger, _hauler, 22f, (int)DrivingStyle.Normal, 10f);
                }
            }
        }
        private bool _gohanFollowing;

        private Objective LoopWindow() =>
            new ReactionTrigger(() => _loopStarted > 0 && Game.GameTime - _loopStarted > LoopSeconds * 1000 && !Ctx.Cutscenes.IsActive,
                () => Fail("The camera loop dropped with the crates still on the ground."));

        /// <summary>The aftermath: the flatbed at the stash with both crates on it.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_hauler == null || !_hauler.Exists()) return null;
            return new SceneBlocking().Then(new ShotStep(4500, _hauler, new Vector3(-6f, -5f, 2f), _hauler, new Vector3(0f, -2f, 1f), 1.0f));
        }

        // ---------- cast and props ----------

        /// <summary>Five posts around the gate and the pads, each its own place: the gate, the camera room, the two pads and the hauler (Ron, September 11: they stood in a bunch).</summary>
        private Vector3 SentryPost(int index)
        {
            Vector3 wanted;
            switch (index)
            {
                case 0: wanted = _gate + new Vector3(0f, 12f, 0f); break;
                case 1: wanted = _cameras + new Vector3(6f, 4f, 0f); break;
                case 2: wanted = _padOne + new Vector3(-9f, 8f, 0f); break;
                case 3: wanted = _padTwo + new Vector3(9f, 8f, 0f); break;
                default: wanted = Ctx.Locations.Position("M08.HaulerSpawn") + new Vector3(0f, 10f, 0f); break;
            }
            var safe = World.GetSafeCoordForPed(wanted, false, 0);
            return safe != Vector3.Zero && GameUtils.IsWithinFlat(safe, wanted, 20f) ? safe : wanted;
        }

        private void SpawnCameraPanel()
        {
            var cabinModel = new Model("prop_portacabin01");
            if (!GameUtils.RequestModel(cabinModel)) return;
            _securityCabin = Track(World.CreateProp(cabinModel, _cameras + new Vector3(0f, 4f, -1f), false, false));
            cabinModel.MarkAsNoLongerNeeded();
            if (_securityCabin != null && _securityCabin.Exists())
            { _securityCabin.IsPersistent = true; _securityCabin.IsPositionFrozen = true; }
            var model = new Model("prop_elecbox_12");
            if (!GameUtils.RequestModel(model)) return;
            var point = _cameras + new Vector3(0f, 1.2f, -0.9f);
            _cameraPanel = Track(World.CreateProp(model, point, false, false));
            model.MarkAsNoLongerNeeded();
            if (_cameraPanel == null || !_cameraPanel.Exists()) return;
            _cameraPanel.IsPositionFrozen = true; _cameraPanel.IsPersistent = true;
            _cameraPanel.Heading = Ctx.Locations.Heading("M08.CameraRoom");
        }

        private void SpawnSentries()
        {
            var model = new Model("s_m_m_security_01");
            if (!GameUtils.RequestModel(model)) return;

            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");

            for (int i = 0; i < 5; i++)
            {
                var post = SentryPost(i);
                var guard = World.CreatePed(model, post, DriveUpStep.HeadingBetween(post, _gate));
                if (guard == null || !guard.Exists()) continue;

                guard.RelationshipGroup = aegis;
                guard.IsPersistent = true;
                guard.BlockPermanentEvents = true;
                guard.Accuracy = 30;
                guard.Weapons.Give(WeaponHash.Pistol, 60, true, true);
                guard.Task.StartScenario("WORLD_HUMAN_GUARD_STAND", guard.Position, guard.Heading);

                _sentries.Add(Track(guard));
            }
            if (_sentries.Count > 1) Logger.Info("M08 sentries at five posts; the first two are " + _sentries[0].Position.DistanceTo(_sentries[1].Position).ToString("0") + " m apart.");

            model.MarkAsNoLongerNeeded();
        }

        /// <summary>The first sentry hit or fighting wakes the rest: they come for the shooter instead of standing at their posts (Ron, September 11).</summary>
        private void AlertSentries()
        {
            _alerted = true;
            foreach (var guard in _sentries)
                if (guard != null && guard.Exists() && !guard.IsDead) { guard.Task.ClearAll(); guard.Task.FightAgainstHatedTargets(120f); }
            Logger.Info("M08 sentries alerted.");
        }

        private void SpawnHauler()
        {
            var model = new Model("flatbed");
            if (!GameUtils.RequestModel(model)) return;

            // Stage the bed in the forklift's apron, not across the sheds/fence.
            if (_forklift == null || !_forklift.Exists()) return;
            var spot = _forklift.Position + _forklift.ForwardVector * 14f;
            float heading = _forklift.Heading;
            if (Ctx.Locations.Get("M08.HaulerSpawn")?.Status == LocationStatus.Surveyed)
            { spot = Ctx.Locations.Position("M08.HaulerSpawn"); heading = Ctx.Locations.Heading("M08.HaulerSpawn"); }
            _hauler = Track(World.CreateVehicle(model, spot, heading));
            model.MarkAsNoLongerNeeded();
            if (_hauler == null || !_hauler.Exists()) return;

            _hauler.IsPersistent = true;
            GameUtils.HoldUntilGrounded(_hauler);

            var blip = Track(_hauler.AddBlip());
            blip.Sprite = BlipSprite.ArmoredTruck;
            blip.Color = BlipColor.Orange;
            blip.Name = "Turbine hauler";
        }

        private void SpawnCrates()
        {
            var model = new Model("prop_mil_crate_01");
            if (!GameUtils.RequestModel(model)) return;
            foreach (var pad in new[] { _padOne, _padTwo })
            {
                var crate = Track(World.CreateProp(model, pad, true, true));
                if (crate == null || !crate.Exists()) continue;
                crate.IsPersistent = true;
                // Static until the forks take it: a bump cannot knock it over or push it off its pad.
                crate.IsPositionFrozen = true;
                _crates.Add(crate);
            }
            model.MarkAsNoLongerNeeded();
        }

        private void SpawnForklift()
        {
            var model = new Model("forklift");
            if (!GameUtils.RequestModel(model)) return;
            // In the open, on the apron's street, not against the sheds (Ron, September 10).
            var spot = World.GetNextPositionOnStreet(_gate + new Vector3(22f, -52f, 0f));
            if (spot == Vector3.Zero) spot = _gate + new Vector3(22f, -52f, 0f);
            _forklift = Track(World.CreateVehicle(model, spot, 90f));
            model.MarkAsNoLongerNeeded();
            if (_forklift == null || !_forklift.Exists()) return;
            _forklift.IsPersistent = true;
            GameUtils.HoldUntilGrounded(_forklift);
            var blip = Track(_forklift.AddBlip());
            blip.Sprite = BlipSprite.Standard;
            blip.Color = BlipColor.Orange;
            blip.Name = "Forklift";
        }

        private void SpawnGranger()
        {
            // The crew's own Granger, parked outside the gate: Gohan's seat out.
            var spot = _gate + new Vector3(-10f, -16f, 0f);
            Vehicle granger = Ctx.Vans != null ? Ctx.Vans.Spawn(spot, 0f) : null;
            if (granger == null)
            {
                var model = new Model("granger");
                if (!GameUtils.RequestModel(model)) return;
                granger = World.CreateVehicle(model, spot, 0f);
                model.MarkAsNoLongerNeeded();
            }
            _granger = Track(granger);
            if (_granger == null || !_granger.Exists()) return;
            _granger.IsPersistent = true;
            _granger.IsEngineRunning = false;
        }

        private void SpawnTechnical()
        {
            var model = new Model("technical");
            var crewModel = new Model("s_m_y_blackops_01");
            if (!GameUtils.RequestModel(model) || !GameUtils.RequestModel(crewModel)) return;

            // Far enough out to be seen coming up the ramp.
            var start = World.GetNextPositionOnStreet(_padOne + new Vector3(0f, 110f, 0f));
            if (start == Vector3.Zero) start = _padOne + new Vector3(0f, 90f, 0f);
            _technical = Track(World.CreateVehicle(model, start, 180f));
            if (_technical == null || !_technical.Exists()) return;
            _technical.IsPersistent = true;
            _technical.IsEngineRunning = true;
            GameUtils.HoldUntilGrounded(_technical);

            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            for (int seat = 0; seat < 2; seat++)
            {
                var crew = Track(World.CreatePed(crewModel, _technical.Position + new Vector3(4f + seat * 2f, 0f, 1f), 0f));
                if (crew == null || !crew.Exists()) continue;

                crew.RelationshipGroup = aegis;
                crew.IsPersistent = true;
                crew.BlockPermanentEvents = true;
                crew.Weapons.Give(WeaponHash.CarbineRifle, 200, true, true);
                crew.SetIntoVehicle(_technical, seat == 0 ? VehicleSeat.Driver : VehicleSeat.LeftRear);
                if (!crew.IsInVehicle(_technical)) { Logger.Warn("M08: response crew could not seat; leaving this actor out of the chase."); continue; }
                crew.AlwaysKeepTask = true;
                _responseCrew.Add(crew);
                if (seat == 0) { crew.Task.VehicleChase(Game.Player.Character); }
                else crew.Task.VehicleShootAtPed(Game.Player.Character);
            }

            model.MarkAsNoLongerNeeded();
            crewModel.MarkAsNoLongerNeeded();

            var blip = Track(_technical.AddBlip());
            blip.Sprite = BlipSprite.Enemy;
            blip.Color = BlipColor.Red;
            blip.Name = "Aegis technical";
        }

        protected override void OnCleanup()
        {
            Ctx.Crew.CompanionsHoldPosition = false;
            _sentries.Clear();
            _responseCrew.Clear();
            _crates.Clear();
        }
    }

}
