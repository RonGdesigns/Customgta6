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
    /// This is the one mission whose positions come from the bible's own surveyed
    /// coordinate index (Track 2) rather than from estimates.
    /// </summary>
    public sealed class M01GhostInTheDockyard : Mission
    {
        private const int LedgerRipSeconds = 8;
        private const int RecognitionWindowSeconds = 90;

        private static readonly string[] CartelGoons = { "g_m_y_mexgoon_01", "g_m_y_mexgoon_02", "g_m_y_mexgang_01" };

        private readonly List<Ped> _guards = new List<Ped>();

        private Ped _mateo;
        private Vehicle _prototype;
        private Vehicle _launch;
        private Blip _objectiveBlip;

        private Vector3 _roost;
        private Vector3 _bilge;
        private Vector3 _bayFloor;
        private Vector3 _slipway;

        private bool _iceHasEyes;
        private bool _ledgerRipped;
        private bool _prototypeTaken;
        private bool _mateoFleeing;

        private int _ripStartedAt;
        private int _recognitionStartedAt;

        public override string Id => "M01";
        public override string Title => "Ghost in the Dockyard";

        protected override bool OnStart()
        {
            // Track 2 of the omnibus bible — surveyed positions, used verbatim.
            _roost = Ctx.Data.Anchor("Ice: Roost 4", Ctx.Locations.Position("M01.CraneNest"));
            _bilge = Ctx.Data.Anchor("Gohan: Bilge Hatch", Ctx.Locations.Position("M01.LowerDeckLedger"));
            _bayFloor = Ctx.Data.Anchor("Guess: Bay 2", Ctx.Locations.Position("M01.PrototypeCar"));
            _slipway = Ctx.Data.Anchor("Mateo: Escape Boat", Ctx.Locations.Position("M01.LaunchEscape"));

            // Three separate operations: nobody follows anybody until the collision.
            Ctx.Crew.CompanionsHoldPosition = true;

            var placements = new Dictionary<CrewSlot, PedPlacement>
            {
                { CrewSlot.Ice, new PedPlacement(_roost, Ctx.Data.AnchorHeading("Ice: Roost 4", 180.5f)) },
                { CrewSlot.Gohan, new PedPlacement(_bilge, Ctx.Data.AnchorHeading("Gohan: Bilge Hatch", 270f)) },
                { CrewSlot.Guess, new PedPlacement(_bayFloor + new Vector3(2f, 0f, 0f), 90f) }
            };

            if (!Ctx.Crew.Deploy(CrewSlot.Ice, placements)) return false;

            SpawnMateo();
            SpawnPrototype();
            ApplyBibleSetting();

            Objective("Ice — set the shot on Mateo from Roost 4.");
            SetObjectiveBlip(_mateo != null && _mateo.Exists() ? _mateo.Position : _bilge, "Mateo Cifuentes");
            return true;
        }

        protected override void OnUpdate()
        {
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            switch (Stage)
            {
                case 0: UpdateApproaches(player); break;
                case 1: UpdateRecognition(); break;
                case 2: UpdateFirefight(player); break;
                case 3: UpdateExtraction(player); break;
            }

            if (Stage >= 1) CheckCrewWipe();
        }

        // ---------- Stage 0 (bible S1): three approaches ----------

        private void UpdateApproaches(Ped player)
        {
            switch (Ctx.Crew.ActiveSlot)
            {
                case CrewSlot.Ice: UpdateIceOverwatch(player); break;
                case CrewSlot.Gohan: UpdateGohanBilge(player); break;
                case CrewSlot.Guess: UpdateGuessBay(player); break;
            }

            if (!_iceHasEyes || !_ledgerRipped || !_prototypeTaken) return;

            BeginRecognition();
        }

        private void UpdateIceOverwatch(Ped player)
        {
            if (_iceHasEyes || _mateo == null || !_mateo.Exists()) return;

            GameUtils.DrawObjectiveMarker(_mateo.Position, Color.FromArgb(120, 224, 74, 62), 0.9f);

            bool eyesOn = player.IsAiming && Game.Player.IsTargeting(_mateo)
                          && player.Position.DistanceTo(_mateo.Position) < 260f;
            if (!eyesOn) return;

            _iceHasEyes = true;
            Say("M01_S1_01_ICE");
            NextApproachObjective();
        }

        private void UpdateGohanBilge(Ped player)
        {
            if (_ledgerRipped) return;

            GameUtils.DrawObjectiveMarker(_bilge, Color.FromArgb(120, 106, 168, 122));

            if (!GameUtils.IsWithin(player.Position, _bilge, 2.5f))
            {
                _ripStartedAt = 0;
                return;
            }

            if (_ripStartedAt == 0)
            {
                _ripStartedAt = Game.GameTime;
                Say("M01_S1_02_GOHAN");
                return;
            }

            int elapsed = (Game.GameTime - _ripStartedAt) / 1000;
            if (elapsed < LedgerRipSeconds)
            {
                GameUtils.Subtitle("Cloning the stateroom safe... " + (LedgerRipSeconds - elapsed) + "s", 500);
                return;
            }

            _ledgerRipped = true;
            NextApproachObjective();
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

            if (!player.IsInVehicle(_prototype)) return;

            _prototypeTaken = true;
            Say("M01_S1_03_GUESS");
            NextApproachObjective();
        }

        /// <summary>
        /// Points the player at whichever approach is still open. The mission never
        /// says "press 2" — it states the job, and the job is only doable as the
        /// character who owns it, which is how the switch teaches itself.
        /// </summary>
        private void NextApproachObjective()
        {
            if (!_iceHasEyes)
            {
                Objective("Ice — set the shot on Mateo from Roost 4.");
                SetObjectiveBlip(_roost, "Roost 4");
            }
            else if (!_ledgerRipped)
            {
                Objective("Gohan — clone the stateroom safe from the bilge.");
                SetObjectiveBlip(_bilge, "Bilge hatch");
            }
            else if (!_prototypeTaken)
            {
                Objective("Guess — get the prototype out of bay 2.");
                SetObjectiveBlip(_bayFloor, "Warehouse bay 2");
            }
        }

        // ---------- Stage 1 (bible S2): the fatal recognition ----------

        private void BeginRecognition()
        {
            GameUtils.FadeOut(800);
            Script.Wait(900);

            var regroup = Ctx.Locations.Position("M01.RegroupPoint");

            foreach (var protagonist in Protagonist.All)
            {
                var ped = Ctx.Crew.PedFor(protagonist.Slot);
                if (ped == null) continue;
                ped.Task.ClearAllImmediately();
                ped.Position = regroup + new Vector3((int)protagonist.Slot * 2.2f - 2.2f, 0f, 0f);
                ped.Heading = 180f;
            }

            SpawnEscapeLaunch();
            SpawnGuardWave(regroup, 8);

            Ctx.Crew.CompanionsHoldPosition = false;
            Ctx.Crew.AssignCompanionAI();

            GameUtils.FadeIn(1200);
            _recognitionStartedAt = Game.GameTime;

            // The whole campaign turns on these three lines.
            SayStage(2);

            Objective("Hold the dry-dock. Mateo is running for the launch.");
            SetObjectiveBlip(_slipway, "Armored launch");
            Advance();
        }

        private void UpdateRecognition()
        {
            // A beat of scripted stillness — the ninety seconds they lose to each other.
            if (SecondsInStage < 4) return;

            Ctx.Crew.OrderCompanionsToFight();
            Advance();
        }

        // ---------- Stage 2: the firefight ----------

        private void UpdateFirefight(Ped player)
        {
            int remaining = RecognitionWindowSeconds - (Game.GameTime - _recognitionStartedAt) / 1000;

            _guards.RemoveAll(guard => guard == null || !guard.Exists() || guard.IsDead);

            if (remaining > 0)
            {
                GameUtils.Subtitle("~y~Hostiles: " + _guards.Count + "   ~s~Mateo clear in ~r~" + remaining + "s", 500);
            }

            if (!_mateoFleeing && remaining <= 0 &&
                _launch != null && _launch.Exists() && _mateo != null && _mateo.Exists() && _mateo.IsAlive)
            {
                // Mateo is scripted to get away. The campaign hangs off him living
                // through tonight, and the player's job was never to stop him.
                _mateoFleeing = true;
                _mateo.Task.CruiseWithVehicle(_launch, 30f, DrivingStyle.Rushed);
                Say("M01_S3_07_ICE");
            }

            if (_guards.Count > 2 && remaining > -20) return;

            Say("M01_S3_08_GUESS");
            Objective("Smash the gates. Get the crew out in the prototype.");
            SetObjectiveBlip(Ctx.Locations.Position("M01.ExitPoint"), "Exfil");
            Game.Player.WantedLevel = 2;
            Advance();
        }

        // ---------- Stage 3 (bible S3): gate smash extraction ----------

        private void UpdateExtraction(Ped player)
        {
            var exit = Ctx.Locations.Position("M01.ExitPoint");
            GameUtils.DrawObjectiveMarker(exit, Color.FromArgb(120, 106, 168, 122), 4f);

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

            Game.Player.WantedLevel = 0;
            Say("M01_S3_09_GOHAN");
            Pass();
        }

        // ---------- world building ----------

        private void SpawnMateo()
        {
            var model = new Model("g_m_m_mexboss_01");
            if (!GameUtils.RequestModel(model)) return;

            var stateroom = _bilge + new Vector3(0f, 6f, 6f);
            _mateo = Track(World.CreatePed(model, stateroom, 90f));
            model.MarkAsNoLongerNeeded();
            if (_mateo == null || !_mateo.Exists()) return;

            _mateo.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_CARTEL");
            _mateo.IsPersistent = true;
            _mateo.BlockPermanentEvents = true;
            _mateo.Armor = 100;
            _mateo.Weapons.Give(WeaponHash.APPistol, 100, true, true);
            _mateo.Task.StartScenario("WORLD_HUMAN_DRINKING", _mateo.Position, 90f);

            var blip = Track(_mateo.AddBlip());
            blip.Sprite = BlipSprite.Enemy;
            blip.Color = BlipColor.Red;
            blip.Name = "Mateo Cifuentes";
        }

        private void SpawnPrototype()
        {
            var model = new Model("t20");
            if (!GameUtils.RequestModel(model)) return;

            _prototype = Track(World.CreateVehicle(model, _bayFloor,
                Ctx.Data.AnchorHeading("Guess: Bay 2", 90f)));
            model.MarkAsNoLongerNeeded();
            if (_prototype == null || !_prototype.Exists()) return;

            _prototype.IsPersistent = true;
            _prototype.IsEngineRunning = false;
            _prototype.LockStatus = VehicleLockStatus.Unlocked;

            var blip = Track(_prototype.AddBlip());
            blip.Sprite = BlipSprite.PersonalVehicleCar;
            blip.Color = BlipColor.Orange;
            blip.Name = "$3M prototype";
        }

        private void SpawnEscapeLaunch()
        {
            var model = new Model("tropic");
            if (!GameUtils.RequestModel(model)) return;

            _launch = Track(World.CreateVehicle(model, _slipway,
                Ctx.Data.AnchorHeading("Mateo: Escape Boat", 225f)));
            model.MarkAsNoLongerNeeded();
            if (_launch == null || !_launch.Exists()) return;

            _launch.IsPersistent = true;

            if (_mateo != null && _mateo.Exists())
            {
                _mateo.Task.ClearAllImmediately();
                _mateo.Task.EnterVehicle(_launch, VehicleSeat.Driver, 25000, 2f, EnterVehicleFlags.None);
            }
        }

        private void SpawnGuardWave(Vector3 around, int count)
        {
            var cartel = World.AddRelationshipGroup("BLOODLINES_CARTEL");

            for (int i = 0; i < count; i++)
            {
                var model = new Model(CartelGoons[i % CartelGoons.Length]);
                if (!GameUtils.RequestModel(model)) continue;

                float angle = i * (360f / count);
                var offset = new Vector3(
                    (float)System.Math.Cos(angle * System.Math.PI / 180f) * 28f,
                    (float)System.Math.Sin(angle * System.Math.PI / 180f) * 28f,
                    0f);

                var guard = World.CreatePed(model, around + offset, 0f);
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
            if (Ctx.Config.CompanionsRespawnOnDeath) return;

            foreach (var protagonist in Protagonist.All)
            {
                var ped = Ctx.Crew.PedFor(protagonist.Slot);
                if (ped == null || ped.IsDead)
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

            Ctx.Crew.CompanionsHoldPosition = false;
            Ctx.Crew.AssignCompanionAI();
            _iceHasEyes = _ledgerRipped = _prototypeTaken = true;
            _recognitionStartedAt = Game.GameTime;

            if (stage >= 2 && _guards.Count == 0)
            {
                SpawnGuardWave(Game.Player.Character.Position, 6);
            }
        }

        protected override void OnCleanup()
        {
            Ctx.Crew.CompanionsHoldPosition = false;
            GameUtils.SafeDelete(_objectiveBlip);
            _guards.Clear();

            if (GameUtils.IsScreenFadedOut()) GameUtils.FadeIn(600);
        }
    }
}
