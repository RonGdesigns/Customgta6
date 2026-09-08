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
    /// M01 — "Ghost in the Dockyard". The inciting collision from section 3.
    ///
    /// This is the campaign's teaching mission, so the structure is deliberate: the
    /// first three stages force one job per character, each solvable only by that
    /// character's discipline, which means the player learns the switch by needing
    /// it rather than by being told about it. The collision then puts all three in
    /// the same firefight, and the last stage hands the escape to Guess.
    /// </summary>
    public sealed class M01GhostInTheDockyard : Mission
    {
        private const int LedgerRipSeconds = 8;
        private const int RecognitionWindowSeconds = 90;

        private static readonly string[] CartelGoons = { "g_m_y_mexgoon_01", "g_m_y_mexgoon_02", "g_m_y_mexgang_01" };

        private readonly List<Ped> _guards = new List<Ped>();

        private Ped _capo;
        private Vehicle _prototype;
        private Vehicle _launch;
        private Blip _objectiveBlip;

        private int _ripStartedAt;
        private int _recognitionStartedAt;
        private bool _capoFleeing;

        public override string Id => "M01";
        public override string Title => "Ghost in the Dockyard";

        protected override bool OnStart()
        {
            var crane = Ctx.Locations.Position("M01.CraneNest");
            var lower = Ctx.Locations.Position("M01.LowerDeckLedger");
            var bay = Ctx.Locations.Position("M01.WarehouseBay");

            // Three separate operations: nobody follows anybody until the collision.
            Ctx.Crew.CompanionsHoldPosition = true;

            var placements = new Dictionary<CrewSlot, PedPlacement>
            {
                { CrewSlot.Ice, new PedPlacement(crane, Ctx.Locations.Heading("M01.CraneNest")) },
                { CrewSlot.Gohan, new PedPlacement(lower, 0f) },
                { CrewSlot.Guess, new PedPlacement(bay, Ctx.Locations.Heading("M01.WarehouseBay")) }
            };

            if (!Ctx.Crew.Deploy(CrewSlot.Ice, placements)) return false;

            SpawnCapo();
            SpawnPrototype();

            Function.Call(Hash.SET_CLOCK_TIME, 2, 0, 0);
            Function.Call(Hash.SET_WEATHER_TYPE_NOW, "CLEARING");

            Objective("Ice — get eyes on the capo from the crane nest.");
            SetObjectiveBlip(Ctx.Locations.Position("M01.YachtDeck"), "Cartel capo");
            return true;
        }

        protected override void OnUpdate()
        {
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            switch (Stage)
            {
                case 0: UpdateIceOverwatch(player); break;
                case 1: UpdateGohanLedger(player); break;
                case 2: UpdateGuessPrototype(player); break;
                case 3: UpdateRecognition(); break;
                case 4: UpdateFirefight(player); break;
                case 5: UpdateEscape(player); break;
            }

            if (Stage >= 3 && Stage <= 5) CheckCrewWipe();
        }

        // ---------- Stage 0: Ice on the crane ----------

        private void UpdateIceOverwatch(Ped player)
        {
            if (Ctx.Crew.ActiveSlot != CrewSlot.Ice)
            {
                GameUtils.Subtitle("~r~Ice is still setting the shot. Switch back with " +
                                   Ctx.Config.SwitchIceKey + ".", 2500);
                return;
            }

            if (_capo == null || !_capo.Exists()) return;

            GameUtils.DrawObjectiveMarker(_capo.Position, Color.FromArgb(120, 224, 74, 62), 0.9f);

            bool hasEyes = player.IsAiming && Game.Player.IsTargeting(_capo);
            bool closeEnough = player.Position.DistanceTo(_capo.Position) < 220f;

            if (!hasEyes || !closeEnough) return;

            GameUtils.Subtitle("~y~Target confirmed. Hold — the ledger comes first.", 4000);
            Objective("Switch to Gohan (" + Ctx.Config.SwitchGohanKey + ") and rip the cold-storage ledger.");
            SetObjectiveBlip(Ctx.Locations.Position("M01.LowerDeckLedger"), "Cold-storage ledger");
            Advance();
        }

        // ---------- Stage 1: Gohan on the ledger ----------

        private void UpdateGohanLedger(Ped player)
        {
            var ledger = Ctx.Locations.Position("M01.LowerDeckLedger");
            GameUtils.DrawObjectiveMarker(ledger, Color.FromArgb(120, 106, 168, 122));

            if (Ctx.Crew.ActiveSlot != CrewSlot.Gohan)
            {
                _ripStartedAt = 0;
                return;
            }

            if (!GameUtils.IsWithin(player.Position, ledger, 2.2f))
            {
                _ripStartedAt = 0;
                return;
            }

            if (_ripStartedAt == 0)
            {
                _ripStartedAt = Game.GameTime;
                GameUtils.Subtitle("~y~Ripping cold storage — hold position.", 2000);
                return;
            }

            int elapsed = (Game.GameTime - _ripStartedAt) / 1000;
            if (elapsed < LedgerRipSeconds)
            {
                GameUtils.Subtitle("Ripping ledger... " + (LedgerRipSeconds - elapsed) + "s", 500);
                return;
            }

            GameUtils.Subtitle("~g~Ledger copied.", 3000);
            Objective("Switch to Guess (" + Ctx.Config.SwitchGuessKey + ") and take the prototype out of the bay.");
            SetObjectiveBlip(Ctx.Locations.Position("M01.PrototypeCar"), "Prototype");
            Advance();
        }

        // ---------- Stage 2: Guess on the prototype ----------

        private void UpdateGuessPrototype(Ped player)
        {
            if (_prototype == null || !_prototype.Exists())
            {
                Fail("The prototype was destroyed.");
                return;
            }

            GameUtils.DrawObjectiveMarker(_prototype.Position, Color.FromArgb(120, 214, 138, 58));

            if (Ctx.Crew.ActiveSlot != CrewSlot.Guess) return;

            if (!player.IsInVehicle(_prototype)) return;

            BeginRecognition();
        }

        // ---------- Stage 3: the fatal recognition ----------

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

            GameUtils.Subtitle("~y~A dropped callsign. Three faces, one job, ninety seconds of hesitation.", 6000);
            Objective("Hold the dry-dock. The capo is running for the launch.");
            SetObjectiveBlip(Ctx.Locations.Position("M01.LaunchEscape"), "Armored launch");
            Advance();
        }

        private void UpdateRecognition()
        {
            // A beat of scripted stillness before the port turns on them.
            if (SecondsInStage < 3) return;

            Ctx.Crew.OrderCompanionsToFight();
            GameUtils.Subtitle("~r~Cartel security is on top of you.", 4000);
            Advance();
        }

        // ---------- Stage 4: the firefight ----------

        private void UpdateFirefight(Ped player)
        {
            int remaining = RecognitionWindowSeconds - (Game.GameTime - _recognitionStartedAt) / 1000;

            _guards.RemoveAll(guard => guard == null || !guard.Exists() || guard.IsDead);

            if (remaining > 0)
            {
                GameUtils.Subtitle("~y~Hostiles: " + _guards.Count + "   ~s~Capo clear in ~r~" + remaining + "s", 500);
            }

            if (!_capoFleeing && remaining <= 0 &&
                _launch != null && _launch.Exists() && _capo != null && _capo.Exists() && _capo.IsAlive)
            {
                // The capo is scripted to get away — the whole campaign hangs off him
                // living through tonight. The player's job was never to stop him.
                _capoFleeing = true;
                _capo.Task.CruiseWithVehicle(_launch, 30f, DrivingStyle.Rushed);
            }

            if (_guards.Count > 2 && remaining > -20) return;

            GameUtils.Subtitle("~y~The capo is gone. Aegis and LSPD are inbound — get out.", 5000);
            Objective("Get clear of the port. Guess drives.");
            SetObjectiveBlip(Ctx.Locations.Position("M01.ExitPoint"), "Exfil");
            Game.Player.WantedLevel = 2;
            Advance();
        }

        // ---------- Stage 5: exfil ----------

        private void UpdateEscape(Ped player)
        {
            var exit = Ctx.Locations.Position("M01.ExitPoint");
            GameUtils.DrawObjectiveMarker(exit, Color.FromArgb(120, 106, 168, 122), 4f);

            if (!GameUtils.IsWithinFlat(player.Position, exit, 12f)) return;

            int crewPresent = 0;
            foreach (var protagonist in Protagonist.All)
            {
                var ped = Ctx.Crew.PedFor(protagonist.Slot);
                if (ped != null && ped.IsAlive && GameUtils.IsWithinFlat(ped.Position, exit, 45f)) crewPresent++;
            }

            if (crewPresent < 3)
            {
                GameUtils.Subtitle("~y~Wait for the others — " + crewPresent + "/3 clear.", 1500);
                return;
            }

            Game.Player.WantedLevel = 0;
            GameUtils.Subtitle("~g~Three strangers, one crime scene, three burned identities.", 6000);
            Pass();
        }

        // ---------- world building ----------

        private void SpawnCapo()
        {
            var model = new Model("g_m_m_mexboss_01");
            if (!GameUtils.RequestModel(model)) return;

            _capo = Track(World.CreatePed(model, Ctx.Locations.Position("M01.CapoSpawn"), 90f));
            model.MarkAsNoLongerNeeded();
            if (_capo == null || !_capo.Exists()) return;

            _capo.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_CARTEL");
            _capo.IsPersistent = true;
            _capo.BlockPermanentEvents = true;
            _capo.Armor = 100;
            _capo.Weapons.Give(WeaponHash.APPistol, 100, true, true);
            _capo.Task.StartScenario("WORLD_HUMAN_SMOKING", _capo.Position, 90f);
        }

        private void SpawnPrototype()
        {
            var model = new Model("t20");
            if (!GameUtils.RequestModel(model)) return;

            _prototype = Track(World.CreateVehicle(model, Ctx.Locations.Position("M01.PrototypeCar"),
                Ctx.Locations.Heading("M01.PrototypeCar")));
            model.MarkAsNoLongerNeeded();
            if (_prototype == null || !_prototype.Exists()) return;

            _prototype.IsPersistent = true;
            _prototype.IsEngineRunning = false;
            _prototype.LockStatus = VehicleLockStatus.Unlocked;

            var blip = Track(_prototype.AddBlip());
            blip.Sprite = BlipSprite.PersonalVehicleCar;
            blip.Color = BlipColor.Orange;
            blip.Name = "Prototype";
        }

        private void SpawnEscapeLaunch()
        {
            var model = new Model("dinghy");
            if (!GameUtils.RequestModel(model)) return;

            _launch = Track(World.CreateVehicle(model, Ctx.Locations.Position("M01.LaunchEscape"), 45f));
            model.MarkAsNoLongerNeeded();
            if (_launch == null || !_launch.Exists()) return;

            _launch.IsPersistent = true;

            if (_capo != null && _capo.Exists())
            {
                _capo.Task.ClearAllImmediately();
                _capo.Task.EnterVehicle(_launch, VehicleSeat.Driver, 20000, 2f, EnterVehicleFlags.None);
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
            foreach (var protagonist in Protagonist.All)
            {
                var ped = Ctx.Crew.PedFor(protagonist.Slot);
                if (ped == null || ped.IsDead)
                {
                    if (!Ctx.Config.CompanionsRespawnOnDeath)
                    {
                        Fail(protagonist.DisplayName + " was killed.");
                        return;
                    }
                }
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
