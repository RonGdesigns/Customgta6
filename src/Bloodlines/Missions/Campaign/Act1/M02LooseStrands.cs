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
    /// M02 — "Loose Strands". Olympic Freeway, 05:30, fog.
    ///
    /// An Aegis comm-van is uploading the trio's biometrics and the bible gives the
    /// mission a hard three-minute timer. Where M01 taught the switch by separating
    /// the crew, M02 teaches it under pressure with all three in one car: Guess has
    /// to hold the match, Gohan is the only one who can kill the drivetrain, and Ice
    /// is the only one who takes the doors. Miss the window and the upload lands.
    ///
    /// Positions here are approximate (see LocationBook) — the van drives a live
    /// traffic route rather than a scripted spline, so the mission tolerates the
    /// start point being a few metres off in a way a waypoint list would not.
    /// </summary>
    public sealed class M02LooseStrands : Mission
    {
        private const int UploadWindowSeconds = 180;
        private const float EmpRange = 30f;
        private const int EmpLockSeconds = 2;

        private readonly List<Ped> _escort = new List<Ped>();

        private Vehicle _van;
        private Ped _technician;
        private Vehicle _chase;
        private Blip _vanBlip;

        private int _startedAt;
        private int _empLockedAt;
        private bool _driveSeized;
        private bool _chopperCalled;

        public override string Id => "M02";
        public override string Title => "Loose Strands";

        protected override bool OnStart()
        {
            var start = Ctx.Locations.Position("M02.InterceptStart");
            float heading = Ctx.Locations.Heading("M02.InterceptStart");

            if (!Ctx.Crew.Deploy(CrewSlot.Guess, start, heading)) return false;

            ApplyBibleSetting();

            if (!SpawnChaseCar(start, heading)) return false;
            if (!SpawnVan(start, heading)) return false;

            _startedAt = Game.GameTime;
            SayStage(1);
            Objective("Catch the Aegis comm-van before the upload finishes.");
            return true;
        }

        protected override void OnUpdate()
        {
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            // Once the drives are out of the van, the van itself stops mattering.
            if ((_van == null || !_van.Exists()) && !_driveSeized)
            {
                Fail("The van and its servers are gone.");
                return;
            }

            // The upload clock runs until the drivetrain is dead, and only until then.
            if (!_driveSeized && Stage < 2)
            {
                int remaining = UploadWindowSeconds - (Game.GameTime - _startedAt) / 1000;
                if (remaining <= 0)
                {
                    Fail("The upload completed. Your faces are on federal wiretaps.");
                    return;
                }

                if (Stage < 2)
                {
                    GameUtils.Subtitle("~s~Upload completes in ~r~" + remaining / 60 + ":" +
                                       (remaining % 60).ToString("00"), 500);
                }
            }

            switch (Stage)
            {
                case 0: UpdatePursuit(player); break;
                case 1: UpdateEmpLock(player); break;
                case 2: UpdateBreach(player); break;
                case 3: UpdateEscape(player); break;
            }
        }

        // ---------- Stage 0: match speed ----------

        private void UpdatePursuit(Ped player)
        {
            float distance = player.Position.DistanceTo(_van.Position);
            if (distance > 60f) return;

            Say("M02_S1_02_GUESS");
            Objective("Gohan — put an EMP dart into the drivetrain.");
            Advance();
        }

        // ---------- Stage 1: Gohan's EMP dart ----------

        private void UpdateEmpLock(Ped player)
        {
            GameUtils.DrawObjectiveMarker(_van.Position + new Vector3(0f, 0f, 2.2f),
                Color.FromArgb(130, 106, 168, 122), 0.6f);

            if (Ctx.Crew.ActiveSlot != CrewSlot.Gohan)
            {
                _empLockedAt = 0;
                GameUtils.Subtitle("~y~Only Gohan carries the EMP harness.", 1200);
                return;
            }

            bool inRange = player.Position.DistanceTo(_van.Position) < EmpRange;
            bool aiming = player.IsAiming && Game.Player.IsTargeting(_van);

            if (!inRange || !aiming)
            {
                _empLockedAt = 0;
                return;
            }

            if (_empLockedAt == 0)
            {
                _empLockedAt = Game.GameTime;
                Say("M02_S2_03_ICE");
                return;
            }

            if ((Game.GameTime - _empLockedAt) / 1000 < EmpLockSeconds)
            {
                GameUtils.Subtitle("~g~EMP lock...", 400);
                return;
            }

            FireEmp();
        }

        /// <summary>
        /// Kills the van without destroying it — the bible is explicit that the
        /// physical server has to survive, so this stalls the drivetrain rather than
        /// blowing the engine block.
        /// </summary>
        private void FireEmp()
        {
            Say("M02_S2_04_GOHAN");

            _van.EngineHealth = 1f;
            _van.IsEngineRunning = false;
            _van.EnginePowerMultiplier = 0f;
            Function.Call(Hash.SET_VEHICLE_UNDRIVEABLE, _van, true);
            _van.IsDriveable = false;
            World.AddExplosion(_van.Position, ExplosionType.Extinguisher, 0.1f, 0.4f, Game.Player.Character, false, true);

            if (_technician != null && _technician.Exists())
            {
                _technician.Task.LeaveVehicle(LeaveVehicleFlags.None);
            }

            SpawnEscort();

            Objective("Ice — take the rear doors and pull the drives.");
            Advance();
        }

        // ---------- Stage 2: Ice breaches the rear doors ----------

        private void UpdateBreach(Ped player)
        {
            var rear = _van.Position - _van.ForwardVector * 3.2f;
            GameUtils.DrawObjectiveMarker(rear, Color.FromArgb(130, 66, 133, 244), 1.2f);

            if (Ctx.Crew.ActiveSlot != CrewSlot.Ice)
            {
                GameUtils.Subtitle("~y~Ice takes the doors.", 1200);
                return;
            }

            if (!GameUtils.IsWithin(player.Position, rear, 3.5f) || player.IsInVehicle()) return;

            Say("M02_S2_05_ICE");
            _driveSeized = true;

            if (_technician != null && _technician.Exists())
            {
                _technician.Task.HandsUp(20000);
            }

            SpawnChopper();
            Objective("Lose the Aegis chopper down the storm canal.");
            Advance();
        }

        // ---------- Stage 3: the aqueduct escape ----------

        private void UpdateEscape(Ped player)
        {

            var canal = Ctx.Locations.Position("M02.CanalEscape");
            GameUtils.DrawObjectiveMarker(canal, Color.FromArgb(120, 106, 168, 122), 5f);

            if (!_chopperCalled && SecondsInStage >= 3)
            {
                _chopperCalled = true;
                Say("M02_S2_06_GUESS");
            }

            if (!GameUtils.IsWithinFlat(player.Position, canal, 20f)) return;

            Game.Player.WantedLevel = 0;
            GameUtils.Subtitle("~g~Server drives secured. The upload never landed.", 5000);
            Pass();
        }

        // ---------- world building ----------

        private bool SpawnChaseCar(Vector3 start, float heading)
        {
            var model = new Model("granger");
            if (!GameUtils.RequestModel(model)) return false;

            _chase = Track(World.CreateVehicle(model, start + new Vector3(0f, -6f, 0f), heading));
            model.MarkAsNoLongerNeeded();
            if (_chase == null || !_chase.Exists()) return false;

            _chase.IsPersistent = true;
            _chase.IsEngineRunning = true;

            // Everyone starts in the car: Guess driving, the other two riding, which is
            // the shape the mission's dialogue assumes.
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);

            guess?.Task.WarpIntoVehicle(_chase, VehicleSeat.Driver);
            ice?.Task.WarpIntoVehicle(_chase, VehicleSeat.RightFront);
            gohan?.Task.WarpIntoVehicle(_chase, VehicleSeat.LeftRear);
            return true;
        }

        private bool SpawnVan(Vector3 start, float heading)
        {
            var vanModel = new Model("rumpo");
            var techModel = new Model("s_m_m_highsec_01");
            if (!GameUtils.RequestModel(vanModel)) return false;

            _van = Track(World.CreateVehicle(vanModel, start + new Vector3(0f, 90f, 0f), heading));
            vanModel.MarkAsNoLongerNeeded();
            if (_van == null || !_van.Exists()) return false;

            _van.IsPersistent = true;
            _van.Mods.PrimaryColor = VehicleColor.MetallicBlack;
            _van.IsEngineRunning = true;

            if (GameUtils.RequestModel(techModel))
            {
                _technician = Track(World.CreatePed(techModel, _van.Position + new Vector3(2f, 0f, 0f), heading));
                techModel.MarkAsNoLongerNeeded();

                if (_technician != null && _technician.Exists())
                {
                    _technician.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_AEGIS");
                    _technician.IsPersistent = true;
                    _technician.BlockPermanentEvents = true;
                    _technician.Task.WarpIntoVehicle(_van, VehicleSeat.Driver);
                    // Runs the road rather than a fixed path: the route survives the
                    // start point being approximate.
                    _technician.Task.CruiseWithVehicle(_van, 28f, DrivingStyle.Rushed);
                }
            }

            _vanBlip = Track(_van.AddBlip());
            _vanBlip.Sprite = BlipSprite.ArmoredTruck;
            _vanBlip.Color = BlipColor.Red;
            _vanBlip.Name = "Aegis comm-van";
            _vanBlip.ShowRoute = true;
            return true;
        }

        private void SpawnEscort()
        {
            var model = new Model("s_m_y_blackops_01");
            if (!GameUtils.RequestModel(model)) return;

            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");

            for (int i = 0; i < 3; i++)
            {
                var guard = World.CreatePed(model, _van.Position + new Vector3(4f + i * 2f, 3f, 0f), 0f);
                if (guard == null || !guard.Exists()) continue;

                guard.RelationshipGroup = aegis;
                guard.IsPersistent = true;
                guard.BlockPermanentEvents = true;
                guard.Accuracy = 40;
                guard.Armor = 50;
                guard.Weapons.Give(WeaponHash.CarbineRifle, 200, true, true);
                guard.Task.FightAgainstHatedTargets(90f);
                _escort.Add(Track(guard));
            }

            model.MarkAsNoLongerNeeded();
        }

        private void SpawnChopper()
        {
            var model = new Model("buzzard2");
            if (!GameUtils.RequestModel(model)) return;

            var chopper = Track(World.CreateVehicle(model, _van.Position + new Vector3(0f, 60f, 45f), 0f));
            model.MarkAsNoLongerNeeded();
            if (chopper == null || !chopper.Exists()) return;

            var pilotModel = new Model("s_m_y_blackops_01");
            if (!GameUtils.RequestModel(pilotModel)) return;

            var pilot = Track(World.CreatePed(pilotModel, chopper.Position, 0f));
            pilotModel.MarkAsNoLongerNeeded();
            if (pilot == null || !pilot.Exists()) return;

            pilot.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            pilot.IsPersistent = true;
            pilot.Task.WarpIntoVehicle(chopper, VehicleSeat.Driver);
            pilot.Task.ChaseWithHelicopter(Game.Player.Character, new Vector3(0f, 0f, 30f));
        }

        protected override void OnStageEntered(int stage)
        {
            // Restoring past the EMP means the van must already be dead, or the player
            // resumes chasing a van that cannot be caught again.
            if (stage >= 2 && _van != null && _van.Exists())
            {
                _van.IsDriveable = false;
                _van.IsEngineRunning = false;
            }

            if (stage >= 2) _driveSeized = stage >= 3;
        }

        protected override void OnCleanup()
        {
            GameUtils.SafeDelete(_vanBlip);
            _escort.Clear();
        }
    }
}
