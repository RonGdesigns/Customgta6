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
    /// driven by the player to each crate; the lift and the set-down are staged,
    /// because scripted forks that reliably land a crate on a bed do not exist.
    /// </summary>
    public sealed class M08SupplyAndSever : ComposedMission
    {
        public const int LoopSeconds = 240;
        private static readonly Vector3 ForkOffset = new Vector3(0f, 1.6f, 0.35f);
        private static readonly Vector3[] BedSlots = { new Vector3(0f, -0.9f, 1.05f), new Vector3(0f, -3.1f, 1.05f) };

        private readonly List<Ped> _sentries = new List<Ped>();
        private readonly List<Prop> _crates = new List<Prop>();

        private Vehicle _hauler;
        private Vehicle _forklift;
        private Vehicle _granger;
        private Vehicle _technical;
        private Ped _technicalDriver;
        private ForksUnderCrateObjective _crateTwo;
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

            SpawnSentries();
            SpawnHauler();
            SpawnCrates();
            SpawnForklift();
            SpawnGranger();
            if (!RequireAssets(_hauler, _forklift) || _crates.Count != 2) return false;
            Station(CrewSlot.Gohan, _cameras + new Vector3(0f, -12f, 0f));
            Station(CrewSlot.Ice, _gate + new Vector3(-18f, 0f, 0f));
            Station(CrewSlot.Guess, _padOne + new Vector3(-20f, 0f, 0f));
            Ctx.Crew.PedFor(CrewSlot.Ice).Weapons.Give(WeaponHash.RPG, 8, false, false);
            PlayApproach();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Loop the cameras",
                    new MissionInteraction("Gohan — loop the CCTV feed.", () => _cameras, 5, 3f))
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
                    new ForksUnderCrateObjective("Guess — drive the forks under the first turbine crate and stop.", () => _padOne, () => _forklift),
                    LoopWindow())
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context =>
                {
                    SpawnTechnical();
                    PlayLoading(0);
                });

            // The technical is shown coming before it is a fight. Ice handles it;
            // Ron finishes the loading. Either brother's job can be done first.
            _crateTwo = new ForksUnderCrateObjective("Guess — drive the forks under the second crate and stop.", () => _padTwo, () => _forklift);
            yield return new MissionStage("Crate two, the technical",
                    new DestroyVehicleObjective("Ice — put the Aegis technical down.", () => _technical),
                    _crateTwo,
                    new ReactionTrigger(() => !_technicalShown && !Ctx.Cutscenes.IsActive, ShowTechnical),
                    new ReactionTrigger(() => !_technicalDown && _technical != null && _technical.Exists() && _technical.IsDead, TechnicalDown),
                    new ReactionTrigger(() => _loaded == 1 && !Ctx.Cutscenes.IsActive && _crateTwo.Status == ObjectiveStatus.Complete, () => PlayLoading(1)),
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
        /// A crate goes onto the bed: lifted on the forks, driven to the rear of the
        /// flatbed, set down in its slot, counted. Skipping lands it in the slot.
        /// </summary>
        private void PlayLoading(int index)
        {
            if (index < 0 || index >= _crates.Count) return;
            var crate = _crates[index];
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            if (crate == null || !crate.Exists() || _hauler == null || !_hauler.Exists() || _forklift == null || !_forklift.Exists()) return;
            _loaded = index + 1;
            crate.IsPositionFrozen = false;
            StowPropStep.Stow(crate, _forklift, ForkOffset);
            var rear = _hauler.Position - _hauler.ForwardVector * 7f;
            float heading = DriveUpStep.HeadingBetween(rear, _hauler.Position);
            var blocking = new SceneBlocking()
                .Then(new ShotStep(2400, _forklift, new Vector3(-4f, 2f, 1.5f), crate, new Vector3(0f, 0f, 0.4f), 0.4f));
            if (guess != null && guess.Exists()) blocking.Then(new DriveUpStep(guess, _forklift, rear, heading) { TimeoutMs = 16000 });
            blocking.Then(new TransferPropStep(crate, _hauler, BedSlots[index % BedSlots.Length], 1600, _hauler))
                .Then(new ShotStep(2400, _hauler, new Vector3(-4.5f, -4f, 1.6f), _hauler, new Vector3(0f, -2f, 1f), 0.5f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "loading", Title = "Crate " + _loaded + " of 2",
                Reason = "The turbine crate on the forks, across the yard and onto the flatbed: counted, and seen secured.",
                Blocking = blocking
            };
            // One authored line per crate, so the second lift does not repeat the first.
            var cue = Ctx.Data?.Cue(Id + "_SCENE_LOADING_0" + _loaded + "_GUESS");
            if (!Ctx.Cutscenes.PlayStaged(spec, new[] { cue })) { Logger.Warn("M08 loading scene did not play; the crate is placed on the bed directly."); blocking.Complete(); }
            GameUtils.Subtitle("~g~Crate " + _loaded + " of 2 on the bed.", 4000);
        }

        /// <summary>The technical, shown coming up the ramp, once; Guess's line over it.</summary>
        private void ShowTechnical()
        {
            _technicalShown = true;
            string line = Ctx.Data?.Cue("M08_S2_03_GUESS")?.Line ?? "Crate one seated! Ice, armored technical coming up the boat ramp!";
            if (_technicalDriver != null && _technicalDriver.Exists() && _technical != null && _technical.Exists())
                Ctx.Cutscenes.PlayMoment(Id, "Aegis technical", "GUESS", line, _technicalDriver);
            else Say("M08_S2_03_GUESS");
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
            if (_loopStarted > 0 && Stage >= 1 && Stage <= 4 && !Ctx.Cutscenes.IsActive)
            {
                int left = LoopSeconds - (Game.GameTime - _loopStarted) / 1000;
                if (left >= 0) GameUtils.Subtitle("~y~Camera loop: " + left + "s", 500);
            }
            // Gohan follows in the Granger once he is in it.
            if (_crewCalled && !_gohanFollowing && _granger != null && _granger.Exists() && _hauler != null && _hauler.Exists())
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

            _hauler = Track(World.CreateVehicle(model, Ctx.Locations.Position("M08.HaulerSpawn"),
                Ctx.Locations.Heading("M08.HaulerSpawn")));
            model.MarkAsNoLongerNeeded();
            if (_hauler == null || !_hauler.Exists()) return;

            _hauler.IsPersistent = true;

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
                var crate = Track(World.CreateProp(model, pad, true, false));
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

            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            for (int seat = 0; seat < 2; seat++)
            {
                var crew = Track(World.CreatePed(crewModel, _technical.Position, 0f));
                if (crew == null || !crew.Exists()) continue;

                crew.RelationshipGroup = aegis;
                crew.IsPersistent = true;
                crew.BlockPermanentEvents = true;
                crew.Weapons.Give(WeaponHash.CarbineRifle, 200, true, true);
                crew.Task.WarpIntoVehicle(_technical, seat == 0 ? VehicleSeat.Driver : VehicleSeat.Passenger);
                if (seat == 0) { _technicalDriver = crew; crew.Task.VehicleChase(Game.Player.Character); }
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
            _sentries.Clear();
            _crates.Clear();
        }
    }

    /// <summary>
    /// The forks under a crate: the required driver brings the forklift to the pad
    /// and stops; after a short dwell the crate is theirs. No button and no physics
    /// lift, because a real forklift lift is a fight the crate wins (Ron, September 11).
    /// </summary>
    internal sealed class ForksUnderCrateObjective : Objective
    {
        public const float Radius = 4.5f;
        public const int DwellMs = 1000;
        private readonly string _action;
        private readonly Func<Vector3> _pad;
        private readonly Func<Vehicle> _forklift;
        private int _dwell, _lastTick;

        public ForksUnderCrateObjective(string action, Func<Vector3> pad, Func<Vehicle> forklift) : base(action)
        { _action = action; _pad = pad; _forklift = forklift; }

        public override void Enter(MissionContext c) { base.Enter(c); _dwell = 0; _lastTick = Game.GameTime; }

        public override void Update(MissionContext c)
        {
            var forklift = _forklift();
            if (forklift == null || !forklift.Exists() || forklift.IsDead) { Fail("The forklift is lost. Restart this mission."); return; }
            var pad = _pad(); var ped = Game.Player.Character;
            ObjectiveMarkers.Navigation(pad, null, forklift);
            GameUtils.DrawObjectiveMarker(pad, System.Drawing.Color.Yellow, 2f);
            int delta = Math.Max(0, Math.Min(1000, Game.GameTime - _lastTick)); _lastTick = Game.GameTime;
            if (!IsOwnerActive(c)) { _dwell = 0; Label = "Switch to " + Crew.Protagonist.Of(RequiredCharacter.Value).Handle + ": " + _action; return; }
            bool seated = ped != null && ped.Exists() && ped.IsInVehicle(forklift);
            bool near = seated && forklift.Position.DistanceTo(pad) <= Radius;
            if (!near) { _dwell = 0; Label = _action + (seated ? " — drive the forks under the crate." : " — get in the forklift."); return; }
            if (forklift.Speed > 1f) { _dwell = 0; Label = _action + " — stop with the forks under it."; return; }
            _dwell += delta;
            Label = _action;
            GameUtils.DrawProgressBar(_dwell / (float)DwellMs);
            if (_dwell >= DwellMs) Complete();
        }
    }
}
