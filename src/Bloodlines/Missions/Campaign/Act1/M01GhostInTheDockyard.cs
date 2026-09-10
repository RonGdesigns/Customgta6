using System.Collections.Generic;
using System.Drawing;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M01 — "Ghost in the Dockyard". Terminal Island dry-docks, 02:00.
    ///
    /// Three contracts, one dockyard, three characters who do not yet know they are
    /// working the same night. The stage structure follows the bible's cue blocks:
    /// S1 is the three separate approaches, S2 is the recognition and the firefight
    /// it costs them, S3 is Mateo's escape and the gate-smash extraction.
    ///
    /// Exterior positions remain estimates until captured by the in-game surveyor.
    /// ProloguePlacement resolves usable ground before deploying the actors.
    ///
    /// Mateo's escape is seen, not timed: recognition sends him and his technician
    /// running for the launch in the open, the launch waits for him, and it leaves
    /// only once the crew has cleared the yard. There is no clock on the firefight.
    /// </summary>
    public sealed class M01GhostInTheDockyard : Mission
    {
        private const int LedgerRipSeconds = 8;
        /// <summary>How long a clear yard waits for a Mateo the navmesh has stranded before he is brought to the boat.</summary>
        public const int StrandedMs = 45000;

        private static readonly string[] CartelGoons = { "g_m_y_mexgoon_01", "g_m_y_mexgoon_02", "g_m_y_mexgang_01" };

        private readonly List<Ped> _guards = new List<Ped>();

        private Ped _mateo, _technician;
        private Prop _terminal;
        private Vehicle _prototype;
        private Vehicle _approachCar;
        private Vehicle _launch;
        private Blip _objectiveBlip;

        private Vector3 _roost;
        private Vector3 _bilge;
        private Vector3 _bayFloor;
        private Vector3 _slipway;

        private bool _iceHasEyes;
        private bool _ledgerRipped;
        private bool _prototypeTaken;
        private bool _mateoFleeing, _launchStarted, _mateoEscaped;
        private Blip _mateoBlip;
        private bool _exitRouteSet;
        private CrewSlot? _guidanceSlot;
        private CrewSlot? _workingSlot;
        private CrewSlot? _slotBeforeGohan;

        private int _ripStartedAt;
        private int _yardClearedAt;

        public override string Id => "M01";
        public override string Title => "Ghost in the Dockyard";

        protected override bool OnStart()
        {
            // The installed prototype uses accessible dock exteriors. The bible's
            // crane platform and yacht interior are not installed map assets.
            if (!ProloguePlacement.Prepare(Ctx.Locations)) return false;
            _roost = Ctx.Locations.Position("M01.CraneNest");
            _bilge = Ctx.Locations.Position("M01.ServiceTerminal");
            _bayFloor = Ctx.Locations.Position("M01.PrototypeCar");
            _slipway = Ctx.Locations.Position("M01.LaunchEscape");

            // Three separate operations: nobody follows anybody until the collision.
            Ctx.Crew.CompanionsHoldPosition = true;

            var placements = new Dictionary<CrewSlot, PedPlacement>
            {
                { CrewSlot.Ice, new PedPlacement(Ctx.Locations.Position("M01.IceApproach"), Ctx.Locations.Heading("M01.IceApproach")) },
                { CrewSlot.Gohan, new PedPlacement(Ctx.Locations.Position("M01.GohanApproach"), Ctx.Locations.Heading("M01.GohanApproach")) },
                { CrewSlot.Guess, new PedPlacement(Ctx.Locations.Position("M01.GuessApproach"), Ctx.Locations.Heading("M01.GuessApproach")) }
            };

            if (!Ctx.Crew.Deploy(Protagonist.StartingSlot, placements)) return false;
            Ctx.Crew.CompanionsHoldPosition = true;
            Ctx.Crew.AssignCompanionAI();

            SpawnMateo();
            if (!SpawnLedgerStation()) return false;
            SpawnPrototype();
            if (!SpawnApproachCar()) return false;
            if (_mateo == null || !_mateo.Exists() || _prototype == null || !_prototype.Exists()) return false;
            ApplyBibleSetting();
            foreach (var hero in Protagonist.All) Ctx.Crew.CompanionAI.TakeControl(hero.Slot);
            UpdateApproachActors();

            NextApproachObjective();
            DrawApproachGuidance();
            return true;
        }

        protected override void OnUpdate()
        {
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            // Once the launch has left, Mateo is the engine's to stream out; losing
            // him at a distance is not his death.
            if (!_mateoEscaped && (_mateo == null || !_mateo.Exists() || _mateo.IsDead))
            { Fail("Mateo was killed. The ledger must lead us to his buyers."); return; }
            if (Stage == 0 && player.IsShooting)
            { Fail(Ctx.Crew.Active.DisplayName + " blew the cover by firing. Identify Mateo and copy the ledger without shooting."); return; }

            // A skipped or failed escape scene still puts the launch on the water,
            // whichever stage the mission has moved to by then.
            if (_mateoFleeing && !_launchStarted && !Ctx.Cutscenes.IsActive && _mateo != null && _mateo.Exists() && _launch != null && _launch.Exists())
            {
                if (!_mateo.IsInVehicle(_launch)) _mateo.SetIntoVehicle(_launch, VehicleSeat.Driver);
                StartLaunch();
            }

            switch (Stage)
            {
                case 0: UpdateApproaches(player); break;
                case 1: UpdateRecognition(); break;
                case 2: UpdateFirefight(player); break;
                case 3: UpdateExtraction(player); break;
            }

            CheckCrewWipe();

            // The toolkit makes the getaway car a hard failure condition once the
            // crew is committed to it — there is no second way off the island.
            if (_prototype == null || !_prototype.Exists() || !_prototype.IsDriveable)
            {
                Fail("The getaway car is wrecked.");
            }
        }

        // ---------- Stage 0 (bible S1): three approaches ----------

        private void UpdateApproaches(Ped player)
        {
            UpdateApproachActors();
            switch (Ctx.Crew.ActiveSlot)
            {
                case CrewSlot.Ice: UpdateIceOverwatch(player); break;
                case CrewSlot.Gohan: UpdateGohanBilge(player); break;
                case CrewSlot.Guess: UpdateGuessBay(player); break;
            }

            DrawApproachGuidance();
            if (!_iceHasEyes || !_ledgerRipped || !_prototypeTaken) return;

            BeginRecognition();
        }

        private void UpdateIceOverwatch(Ped player)
        {
            if (_iceHasEyes || _mateo == null || !_mateo.Exists()) return;

            GameUtils.DrawObjectiveMarker(_mateo.Position, Color.FromArgb(120, 224, 74, 62), 0.9f);

            if (!GameUtils.IsWithinFlat(player.Position, _roost, 6f)) return;
            bool eyesOn = player.IsAiming && (Game.Player.IsTargeting(_mateo) ||
                          Function.Call<bool>(Hash.IS_PLAYER_FREE_AIMING_AT_ENTITY, Game.Player, _mateo))
                          && player.Position.DistanceTo(_mateo.Position) < 260f;
            if (!eyesOn) return;

            Logger.Info("M01 approach complete: Ice identified Mateo.");
            _iceHasEyes = true;
            Say("M01_S1_01_ICE");
            NextApproachObjective();
        }

        private void UpdateGohanBilge(Ped player)
        {
            if (_ledgerRipped) return;

            GameUtils.DrawObjectiveMarker(_bilge, Color.FromArgb(120, 106, 168, 122));

            if (player.IsInVehicle() || !GameUtils.IsWithin(player.Position, _bilge, 2.5f))
            {
                _ripStartedAt = 0;
                return;
            }

            if (_ripStartedAt == 0)
            {
                if (!Game.IsControlJustPressed(GTA.Control.Context))
                { GameUtils.Subtitle("Gohan: press E / D-pad Right to copy the shipping terminal.", 500); return; }
                _ripStartedAt = Game.GameTime;
                Say("M01_S1_02_GOHAN");
                return;
            }

            int elapsed = (Game.GameTime - _ripStartedAt) / 1000;
            if (elapsed < LedgerRipSeconds)
            {
                GameUtils.Subtitle("Copying the dock ledger... " + (LedgerRipSeconds - elapsed) + "s", 500);
                return;
            }

            Logger.Info("M01 approach complete: Gohan copied the ledger.");
            _ledgerRipped = true;
            NextApproachObjective();
            SwitchAfterTerminal();
        }

        /// <summary>
        /// Gohan's job is done: the player goes straight back to whoever still has
        /// one, or to the brother they came from. Nobody has to open the wheel to
        /// leave a finished terminal.
        /// </summary>
        private void SwitchAfterTerminal()
        {
            var target = !_iceHasEyes ? CrewSlot.Ice : !_prototypeTaken ? CrewSlot.Guess : (_slotBeforeGohan ?? CrewSlot.Ice);
            if (target == CrewSlot.Gohan || Ctx.Switching == null) return;
            GameUtils.Subtitle("~g~Ledger copied.~s~ Back to " + Protagonist.Of(target).DisplayName + ".", 3000);
            if (!Ctx.Switching.TrySwitch(target, missionTransition: true))
                Logger.Warn("M01: the automatic switch to " + target + " after the terminal was refused; the player switches by hand.");
        }

        private void UpdateGuessBay(Ped player)
        {
            if (_prototypeTaken) return;

            if (_prototype == null || !_prototype.Exists())
            {
                Fail("The prototype was destroyed.");
                return;
            }

            GameUtils.DrawObjectiveMarker(_prototype.Position, Color.FromArgb(120, 214, 138, 58));

            if (!player.IsInVehicle(_prototype) || _prototype.GetPedOnSeat(VehicleSeat.Driver) != player) return;

            Logger.Info("M01 approach complete: Guess took the prototype.");
            _prototypeTaken = true;
            Say("M01_S1_03_GUESS");
            NextApproachObjective();
        }

        /// <summary>
        /// Points the player at whichever approach is still open. The mission never
        /// says "press 2" — it states the job, and the job is only doable as the
        /// character who owns it, which is how the switch teaches itself.
        /// </summary>
        private void UpdateApproachActors()
        {
            if (_workingSlot == Ctx.Crew.ActiveSlot) return;
            if (Ctx.Crew.ActiveSlot == CrewSlot.Gohan && _workingSlot.HasValue) _slotBeforeGohan = _workingSlot;
            _workingSlot = Ctx.Crew.ActiveSlot;
            foreach (var hero in Protagonist.All)
            {
                var ped = Ctx.Crew.PedFor(hero.Slot);
                if (ped == null || !ped.Exists()) continue;
                // The mission owns these tasks, so the ordinary follow/boarding AI
                // cannot overwrite them when a different hero becomes the player.
                if (hero.Slot == Ctx.Crew.ActiveSlot) continue;
                // Clearing a seated ped's tasks can eject it during a player handover.
                // Keep the real seat and hold the car while this assignment waits.
                if (ped.IsInVehicle())
                {
                    if (ped.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver) == ped)
                        Function.Call(Hash.TASK_VEHICLE_TEMP_ACTION, ped, ped.CurrentVehicle, 27, 1500);
                    continue;
                }
                ped.Task.ClearAll();
                if (hero.Slot == CrewSlot.Ice && _mateo != null && _mateo.Exists())
                    ped.Task.AimAt(_mateo, -1);
                else if (hero.Slot == CrewSlot.Gohan)
                    ped.Task.StartScenario(GameUtils.IsWithinFlat(ped.Position, _bilge, 3f)
                        ? "WORLD_HUMAN_CLIPBOARD" : "WORLD_HUMAN_STAND_MOBILE", ped.Position, ped.Heading);
                else ped.Task.StartScenario("WORLD_HUMAN_STAND_MOBILE", ped.Position, ped.Heading);
            }
        }

        private void DrawApproachGuidance()
        {
            if (_guidanceSlot != Ctx.Crew.ActiveSlot) { _guidanceSlot = Ctx.Crew.ActiveSlot; NextApproachObjective(); }
            string task;
            if (Ctx.Crew.ActiveSlot == CrewSlot.Ice && !_iceHasEyes)
                task = !GameUtils.IsWithinFlat(Game.Player.Character.Position, _roost, 6f)
                    ? "Ice: walk to the lookout marker. Then aim at Mateo without firing."
                    : "Ice: identify Mateo in your scope. Keep him alive to trace his buyers. No gunfire.";
            else if (Ctx.Crew.ActiveSlot == CrewSlot.Gohan && !_ledgerRipped)
                task = GameUtils.IsWithinFlat(Game.Player.Character.Position, _bilge, 8f)
                    ? "Gohan: stand at the laptop. Press E / D-pad Right, then stay for 8 seconds."
                    : "Gohan: take the west service lane to the green laptop marker, away from Mateo.";
            else if (Ctx.Crew.ActiveSlot == CrewSlot.Guess && !_prototypeTaken)
                task = GameUtils.IsWithinFlat(Game.Player.Character.Position, _prototype.Position, 22f)
                    ? "Guess: park your car, get out and take the orange prototype."
                    : "Guess: drive into the dockyard. The orange marker is the prototype you were hired to steal.";
            else task = !_iceHasEyes ? "Switch to Ice and identify Mateo without firing."
                : !_ledgerRipped ? "Switch to Gohan. Copy the ledger before the getaway."
                : "Switch to Guess and take the orange prototype.";
            bool finishedHere = Ctx.Crew.ActiveSlot == CrewSlot.Ice ? _iceHasEyes : Ctx.Crew.ActiveSlot == CrewSlot.Gohan ? _ledgerRipped : _prototypeTaken;
            RequiredSwitch = finishedHere ? (!_iceHasEyes ? CrewSlot.Ice : !_ledgerRipped ? CrewSlot.Gohan : !_prototypeTaken ? (CrewSlot?)CrewSlot.Guess : null) : null;
            if (Ctx.Crew.ActiveSlot == CrewSlot.Ice && !_iceHasEyes)
            { GameUtils.DrawObjectiveMarker(_roost, Color.Gold); ObjectiveMarkers.Navigation(_roost, CrewSlot.Ice); }
            CurrentObjective = task;
            GameUtils.Subtitle("~y~" + task + " ~s~[" + (_iceHasEyes ? 1 : 0) + "/1 eyes, " +
                (_ledgerRipped ? 1 : 0) + "/1 ledger, " + (_prototypeTaken ? 1 : 0) + "/1 car]", 500);
        }

        private void NextApproachObjective()
        {
            if (Ctx.Crew.ActiveSlot == CrewSlot.Gohan && !_ledgerRipped)
                SetObjectiveBlip(_bilge, "Gohan: dock ledger");
            else if (Ctx.Crew.ActiveSlot == CrewSlot.Guess && !_prototypeTaken)
                SetObjectiveBlip(_prototype.Position, "Guess: prototype");
            else if (!_iceHasEyes)
                SetObjectiveBlip(_mateo.Position, "Switch to Ice: identify Mateo");
            else if (!_ledgerRipped)
                SetObjectiveBlip(_bilge, "Switch to Gohan: dock ledger");
            else if (!_prototypeTaken)
                SetObjectiveBlip(_prototype.Position, "Switch to Guess: prototype");
        }

        // ---------- Stage 1 (bible S2): the fatal recognition ----------

        private void BeginRecognition()
        {

            RequiredSwitch = null;
            // Recognition changes what they know, never where they stand or sit.
            SpawnEscapeLaunch();
            if (_launch == null || !_launch.Exists()) { Fail("The launch could not spawn. Retry the mission."); return; }

            // The whole campaign turns on these three lines. On the last of them
            // Mateo breaks for the slipway with his technician, in the open.
            Ctx.Cutscenes.Play(Id, "recognition", "Fifteen years", _mateo, RunForTheLaunch);

            Objective("Clear the dry-dock. Mateo is running for the launch.");
            SetObjectiveBlip(_slipway, "Armored launch");
            Advance();
        }

        /// <summary>
        /// Mateo and his technician run for the launch on foot, where the player can
        /// see them. Mateo cannot be hurt on the way; the technician can, and it
        /// changes nothing. The launch waits with its engine off until the yard is
        /// clear, so a Mateo who gets there early sits in it.
        /// </summary>
        private void RunForTheLaunch()
        {
            if (_launch == null || !_launch.Exists()) return;
            if (_mateo != null && _mateo.Exists() && !_mateo.IsInVehicle(_launch))
            {
                _mateo.Task.ClearAll();
                _mateo.Task.EnterVehicle(_launch, VehicleSeat.Driver, 90000, 2f, EnterVehicleFlags.None);
                Function.Call(Hash.SET_PED_KEEP_TASK, _mateo, true);
            }
            if (_technician != null && _technician.Exists() && _technician.IsAlive && !_technician.IsInVehicle(_launch))
            {
                _technician.Task.ClearAll();
                _technician.Task.EnterVehicle(_launch, VehicleSeat.RightFront, 90000, 2f, EnterVehicleFlags.None);
                Function.Call(Hash.SET_PED_KEEP_TASK, _technician, true);
            }
            if (_mateoBlip != null && _mateoBlip.Exists()) _mateoBlip.Name = "Mateo: running for the launch";
        }

        private void UpdateRecognition()
        {
            // Mission ticks resume after the scene; no clock starts here.
            if (_launch == null || !_launch.Exists()) { Fail("The escape launch disappeared."); return; }
            // Also runs after a skipped scene: the run is the same either way.
            RunForTheLaunch();
            foreach (var hero in Protagonist.All) Ctx.Crew.CompanionAI.ReleaseControl(hero.Slot);
            Ctx.Crew.CompanionsHoldPosition = false;
            Ctx.Crew.CompanionAI.RequireSharedVehicle = true;
            Ctx.Crew.AssignCompanionAI();
            SpawnGuardWave(Ctx.Locations.Position("M01.RegroupPoint"), 8);
            if (_guards.Count == 0) { Fail("The dockyard guards could not spawn. Retry the mission."); return; }
            Ctx.Crew.OrderCompanionsToFight();
            Advance();
        }

        // ---------- Stage 2: the firefight ----------

        private void UpdateFirefight(Ped player)
        {
            _guards.RemoveAll(guard => guard == null || !guard.Exists() || guard.IsDead);
            bool launchReady = _launch != null && _launch.Exists() && _mateo != null && _mateo.Exists();
            bool aboard = launchReady && _mateo.IsInVehicle(_launch);

            if (!_mateoFleeing)
                GameUtils.Subtitle("~y~Hostiles: " + _guards.Count + "   ~s~" + (aboard ? "Mateo is in the launch." : "Mateo is running for the launch."), 500);

            // No clock: the yard has to be cleared. The escape then plays once Mateo is
            // at the boat, and a Mateo the navmesh has stranded is brought to it after
            // a bounded wait rather than never.
            if (_guards.Count > 0) { _yardClearedAt = 0; return; }
            if (_yardClearedAt == 0) _yardClearedAt = Game.GameTime;

            if (!_mateoFleeing && launchReady)
            {
                bool atLaunch = aboard || _mateo.Position.DistanceTo(_launch.Position) <= 14f;
                if (!atLaunch && Game.GameTime - _yardClearedAt < StrandedMs) return;
                _mateoFleeing = true;
                PlayEscape(!atLaunch);
                Say("M01_S3_07_ICE");
            }

            if (!_mateoFleeing) return;

            Say("M01_S3_08_GUESS");
            Objective("Smash the gates. Get the crew out in the prototype.");
            SetObjectiveBlip(_prototype.Position, "Board the prototype together");
            Game.Player.WantedLevel = 3;
            Advance();
        }

        // ---------- Stage 3 (bible S3): gate smash extraction ----------

        private void UpdateExtraction(Ped player)
        {
            var exit = Ctx.Locations.Position("M01.ExitPoint");
            ObjectiveMarkers.Navigation(exit, null, _prototype);
            GameUtils.DrawObjectiveMarker(exit, Color.FromArgb(120, 106, 168, 122), 4f);

            if (!player.IsInVehicle(_prototype))
            {
                GameUtils.Subtitle("~y~Get into the orange prototype. Bring all three friends.", 500);
                GameUtils.DrawObjectiveMarker(_prototype.Position, Color.Orange, 2f);
                return;
            }
            int aboard = 0;
            foreach (var hero in Protagonist.All)
            {
                var member = Ctx.Crew.PedFor(hero.Slot);
                if (member != null && member.Exists() && member.IsAlive && member.IsInVehicle(_prototype)) aboard++;
            }
            if (aboard < 3)
            {
                GameUtils.Subtitle("~y~Stop for the crew to board: " + aboard + "/3 in the prototype.", 500);
                return;
            }
            if (!_exitRouteSet) { SetObjectiveBlip(exit, "Escape the dockyard"); _exitRouteSet = true; }
            GameUtils.Subtitle("~y~Drive the prototype and all three friends to the yellow exit marker.", 500);
            if (!GameUtils.IsWithinFlat(player.Position, exit, 14f)) return;

            int clear = 0;
            foreach (var protagonist in Protagonist.All)
            {
                var ped = Ctx.Crew.PedFor(protagonist.Slot);
                if (ped != null && ped.IsAlive && GameUtils.IsWithinFlat(ped.Position, exit, 50f)) clear++;
            }

            if (clear < 3)
            {
                GameUtils.Subtitle("~y~Wait for the others — " + clear + "/3 clear.", 1500);
                return;
            }

            Ctx.State?.SetEvidence("ledgerLead", EvidenceState.CopyHeld);
            Game.Player.WantedLevel = 0;
            Say("M01_S3_09_GOHAN");
            Pass();
        }

        // ---------- world building ----------

        private void SpawnMateo()
        {
            var model = new Model("g_m_m_mexboss_01");
            if (!GameUtils.RequestModel(model)) return;

            var stateroom = Ctx.Locations.Position("M01.CapoSpawn");
            _mateo = Track(World.CreatePed(model, stateroom, Ctx.Locations.Heading("M01.CapoSpawn")));
            model.MarkAsNoLongerNeeded();
            if (_mateo == null || !_mateo.Exists()) return;

            // Identification happens before the ambush. A hostile boss provokes
            // companions and ambient combat before the recognition is scripted.
            _mateo.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_TARGET");
            _mateo.IsPersistent = true;
            _mateo.BlockPermanentEvents = true;
            _mateo.Armor = 100;
            _mateo.Health = 500;
            // The lead cannot be lost to a stray round in the firefight: he is the
            // campaign's thread, and his escape is shown, not raced.
            _mateo.IsInvincible = true;
            _mateo.Weapons.Give(WeaponHash.APPistol, 100, false, false);
            _mateo.Task.StartScenario("WORLD_HUMAN_CLIPBOARD", _mateo.Position, 90f);

            _mateoBlip = Track(_mateo.AddBlip());
            _mateoBlip.Sprite = BlipSprite.Enemy;
            _mateoBlip.Color = BlipColor.Yellow;
            _mateoBlip.Name = "Mateo: identify, keep alive";
        }

        private bool SpawnLedgerStation()
        {
            // Mateo is checking shipping records with a technician; the terminal is
            // at a separate west service station, away from their meeting point.
            var laptop = new Model("prop_laptop_01a");
            var table = new Model("prop_table_03");
            var worker = new Model("s_m_m_dockwork_01");
            try
            {
                if (!GameUtils.RequestModel(laptop) || !GameUtils.RequestModel(table) || !GameUtils.RequestModel(worker)) return false;
                var desk = Track(World.CreateProp(table, _bilge, false, true));
                if (desk == null || !desk.Exists()) return false;
                // On the table, not above it: the laptop's base meets the table's top bound.
                _terminal = Track(World.CreateProp(laptop, PropPlacement.OnTop(desk, table, laptop), false, false));
                if (_terminal == null || !_terminal.Exists()) return false;
                desk.IsPositionFrozen = true; _terminal.IsPositionFrozen = true; _terminal.IsInvincible = true;
                if (!ProloguePlacement.TryLand(_mateo.Position + new Vector3(2f, -2f, 0f), out var point)) return false;
                _technician = Track(World.CreatePed(worker, point, 270f));
                if (_technician == null || !_technician.Exists()) return false;
                _technician.IsPersistent = true; _technician.BlockPermanentEvents = true;
                _technician.RelationshipGroup = _mateo.RelationshipGroup;
                _technician.Task.StartScenario("WORLD_HUMAN_CLIPBOARD", point, 270f);
                return true;
            }
            finally { laptop.MarkAsNoLongerNeeded(); table.MarkAsNoLongerNeeded(); worker.MarkAsNoLongerNeeded(); }
        }

        private bool SpawnApproachCar()
        {
            var model = new Model("primo");
            if (!GameUtils.RequestModel(model)) return false;
            _approachCar = Track(World.CreateVehicle(model, Ctx.Locations.Position("M01.GuessApproach"),
                Ctx.Locations.Heading("M01.GuessApproach")));
            model.MarkAsNoLongerNeeded();
            if (_approachCar == null || !_approachCar.Exists()) return false;
            _approachCar.IsPersistent = true;
            _approachCar.IsEngineRunning = true;
            _approachCar.LockStatus = VehicleLockStatus.Unlocked;
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            guess.SetIntoVehicle(_approachCar, VehicleSeat.Driver);
            return guess.IsInVehicle(_approachCar) && _approachCar.GetPedOnSeat(VehicleSeat.Driver) == guess;
        }

        private void SpawnPrototype()
        {
            var model = new Model("schafter3"); // Four-seat prototype test mule: all three must escape.
            if (!GameUtils.RequestModel(model)) return;

            _prototype = Track(World.CreateVehicle(model, _bayFloor,
                Ctx.Locations.Heading("M01.PrototypeCar")));
            model.MarkAsNoLongerNeeded();
            if (_prototype == null || !_prototype.Exists()) return;

            _prototype.IsPersistent = true;
            _prototype.IsEngineRunning = false;
            _prototype.Mods.CustomPrimaryColor = System.Drawing.Color.Black;
            _prototype.LockStatus = VehicleLockStatus.Unlocked;

            var blip = Track(_prototype.AddBlip());
            blip.Sprite = BlipSprite.PersonalVehicleCar;
            blip.Color = BlipColor.Orange;
            blip.Name = "$3M prototype";
        }

        /// <summary>
        /// The escape, shown: the yard is clear, Mateo is at the slipway after the run
        /// the player watched, he boards (or is already aboard), and the launch pulls
        /// away while he has the last word. Skipping warps him aboard and the launch
        /// leaves on the next tick. Only a stranded Mateo is placed beside the boat.
        /// </summary>
        private void PlayEscape(bool bringToLaunch)
        {
            if (_mateo == null || !_mateo.Exists() || _launch == null || !_launch.Exists()) return;
            _mateo.Task.ClearAll();
            if (bringToLaunch && !_mateo.IsInVehicle(_launch))
            {
                var spot = ExitVehicleStep.SafeSpotBeside(_launch);
                Function.Call(Hash.REQUEST_COLLISION_AT_COORD, spot.X, spot.Y, spot.Z);
                _mateo.Position = spot;
                _mateo.Heading = DriveUpStep.HeadingBetween(spot, _launch.Position);
                Logger.Warn("M01: Mateo did not reach the launch on foot within " + StrandedMs / 1000 + " s of the yard clearing; placed beside it for the escape.");
            }
            var blocking = new SceneBlocking { DialogueAfterStep = 1 }
                .Then(new EnterVehicleStep(_mateo, _launch, VehicleSeat.Driver) { TimeoutMs = 12000 })
                .Then(new ShotStep(4500, _launch, new Vector3(-9f, 3f, 2.5f), _launch, new Vector3(0f, 0f, 0.8f), 1.5f, StartLaunch))
                .Then(ShotStep.Wide(2500, _slipway, 14f, 10f, 4f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "escape", Title = "Mateo runs",
                Reason = "Mateo boards the launch and leaves by water; the ledger copy is the crew's only lead now.",
                Blocking = blocking
            }.With("MATEO", _mateo);
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M01 escape scene did not play; Mateo boards and leaves without it.");
        }

        private void StartLaunch()
        {
            if (_launchStarted || _mateo == null || !_mateo.Exists() || _launch == null || !_launch.Exists()) return;
            _launchStarted = true;
            _mateoEscaped = true;
            GameUtils.SafeDelete(_mateoBlip);
            _mateoBlip = null;
            _launch.IsEngineRunning = true;
            _mateo.Task.StartBoatMission(_launch, _slipway + new Vector3(0f, -500f, 0f),
                VehicleMissionType.GoTo, 20f, (VehicleDrivingFlags)786603, 12f, (BoatMissionFlags)7);
        }

        private void SpawnEscapeLaunch()
        {
            if (!MissionSites.Water(Ctx.Locations, "M01.LaunchEscape")) return;
            _slipway = Ctx.Locations.Position("M01.LaunchEscape");
            var model = new Model("tropic");
            if (!GameUtils.RequestModel(model)) return;

            _launch = Track(World.CreateVehicle(model, _slipway,
                Ctx.Locations.Heading("M01.LaunchEscape")));
            model.MarkAsNoLongerNeeded();
            if (_launch == null || !_launch.Exists()) return;

            _launch.IsPersistent = true;


        }

        private void SpawnGuardWave(Vector3 around, int count)
        {
            var cartel = World.AddRelationshipGroup("BLOODLINES_CARTEL");

            for (int i = 0; i < count; i++)
            {
                var model = new Model(CartelGoons[i % CartelGoons.Length]);
                if (!GameUtils.RequestModel(model)) continue;

                float angle = i * (360f / count);
                var position = around + new Vector3(
                    (float)System.Math.Cos(angle * System.Math.PI / 180f) * 28f,
                    (float)System.Math.Sin(angle * System.Math.PI / 180f) * 28f, 0f);

                if (!ProloguePlacement.TryLand(position, out position)) { model.MarkAsNoLongerNeeded(); continue; }
                var guard = World.CreatePed(model, position, 0f);
                model.MarkAsNoLongerNeeded();
                if (guard == null || !guard.Exists()) continue;

                guard.RelationshipGroup = cartel;
                guard.IsPersistent = true;
                guard.BlockPermanentEvents = true;
                guard.Accuracy = 35;
                guard.Armor = 25;
                guard.Weapons.Give(i % 3 == 0 ? WeaponHash.PumpShotgun : WeaponHash.MicroSMG, 200, true, true);
                guard.Task.FightAgainstHatedTargets(120f);

                _guards.Add(Track(guard));
            }

            Logger.Info("M01 spawned " + _guards.Count + " cartel guards.");
        }

        private void SetObjectiveBlip(Vector3 position, string name)
        {
            GameUtils.SafeDelete(_objectiveBlip);
            _objectiveBlip = Track(World.CreateBlip(position));
            if (_objectiveBlip == null) return;

            _objectiveBlip.Sprite = BlipSprite.Standard;
            _objectiveBlip.Color = BlipColor.Yellow;
            _objectiveBlip.ShowRoute = true;
            _objectiveBlip.Name = name;
        }

        private void CheckCrewWipe()
        {


            foreach (var protagonist in Protagonist.All)
            {
                var ped = Ctx.Crew.PedFor(protagonist.Slot);
                if (ped == null || !ped.Exists() || (ped.IsDead && !Ctx.Config.CompanionsRespawnOnDeath))
                {
                    Fail(protagonist.DisplayName + " was killed.");
                    return;
                }
            }
        }

        protected override void OnStageEntered(int stage)
        {
            // A restore into the firefight or the run-out needs the crew together and
            // the recognition timer running again, not the split approach state.
            if (stage < 1) return;

            foreach (var hero in Protagonist.All) Ctx.Crew.CompanionAI.ReleaseControl(hero.Slot);
            Ctx.Crew.CompanionsHoldPosition = false;
            Ctx.Crew.CompanionAI.RequireSharedVehicle = true;
            Ctx.Crew.AssignCompanionAI();
            _iceHasEyes = _ledgerRipped = _prototypeTaken = true;

            if (stage >= 2 && _guards.Count == 0)
            {
                SpawnGuardWave(Game.Player.Character.Position, 6);
            }
        }

        /// <summary>
        /// The aftermath is Gohan's: through the rear side window of the prototype,
        /// the drive in his lap and the problem on it, while he says what it recorded.
        /// </summary>
        public override SceneBlocking OutroBlocking()
        {
            var gohan = Ctx?.Crew.PedFor(CrewSlot.Gohan);
            if (gohan == null || !gohan.Exists() || !gohan.IsInVehicle()) return null;
            return new SceneBlocking()
                .Then(new ShotStep(4500, gohan, new Vector3(0.4f, -1.6f, 0.95f), gohan, new Vector3(0.2f, 0f, 0.45f), 0.35f));
        }

        protected override void OnCleanup()
        {
            foreach (var hero in Protagonist.All) Ctx.Crew.CompanionAI.ReleaseControl(hero.Slot);
            Ctx.Crew.CompanionsHoldPosition = false;
            Ctx.Crew.CompanionAI.RequireSharedVehicle = false;
            GameUtils.SafeDelete(_objectiveBlip);
            _guards.Clear();

            if (GameUtils.IsScreenFadedOut()) GameUtils.FadeIn(600);
        }
    }
}
