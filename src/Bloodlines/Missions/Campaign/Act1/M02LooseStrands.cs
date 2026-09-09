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
        private const float HackRange = 35f;
        private readonly ProximityHack _hack = new ProximityHack(24);
        private readonly List<Ped> _gunners = new List<Ped>();
        private bool _alerted;
        private int _nextGunfire;
        private int _driverStage = -1;

        

        private Vehicle _van;
        private Ped _technician;
        private Vehicle _chase;
        private Blip _vanBlip;

        private int _startedAt;
        private int _breachStarted;
        private bool _driveSeized;
        private bool _chopperCalled;
        private bool _guessDriving;
        private int _nextDriverUpdate;

        public override string Id => "M02";
        public override string Title => "Loose Strands";

        protected override bool OnStart()
        {
            var requested = Ctx.Locations.Position("M02.InterceptStart");
            var start = World.GetNextPositionOnStreet(requested);
            if (start == Vector3.Zero || !GameUtils.IsWithinFlat(start, requested, 80f)) return false;
            float heading = Ctx.Locations.Heading("M02.InterceptStart");

            if (!Ctx.Crew.Deploy(CrewSlot.Guess, start, heading)) return false;

            ApplyBibleSetting();

            if (!SpawnChaseCar(start, heading)) return false;
            if (!SpawnVan(start, heading)) return false;

            Ctx.Crew.CompanionAI.RequireSharedVehicle = true;
            MaintainPassengers();
            _startedAt = Game.GameTime;
            SayStage(1);
            Objective("Catch the Aegis comm-van before the upload finishes.");
            return true;
        }

        protected override void OnUpdate()
        {
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;
            MaintainPassengers();
            MaintainGuessDriving(player);
            MaintainGunfire(player);
            if (_chase == null || !_chase.Exists() || !_chase.IsDriveable) { Fail("The crew's chase car is wrecked."); return; }

            // Once the drives are out of the van, the van itself stops mattering.
            if ((_van == null || !_van.Exists() || _van.IsDead) && !_driveSeized)
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
                case 1: UpdateHack(player); break;
                case 2: UpdateBreach(player); break;
                case 3: UpdateEscape(player); break;
            }
        }

        // ---------- Stage 0: match speed ----------

        private void MaintainPassengers()
        {
            foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Gohan })
            {
                if (slot == Ctx.Crew.ActiveSlot) continue;
                var ped = Ctx.Crew.PedFor(slot);
                if (ped == null || !ped.Exists()) continue;
                if (ped.IsInVehicle(_chase))
                {
                    if (Ctx.Crew.CompanionAI.StateOf(slot) != CompanionState.Scripted) Ctx.Crew.CompanionAI.TakeControl(slot);
                }
                else if (Ctx.Crew.CompanionAI.StateOf(slot) == CompanionState.Scripted) Ctx.Crew.CompanionAI.ReleaseControl(slot);
            }
        }

        private void MaintainGuessDriving(Ped player)
        {
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            bool drive = Ctx.Crew.ActiveSlot != CrewSlot.Guess && guess != null && guess.Exists() && !guess.IsDead &&
                _chase != null && _chase.Exists() && _chase.GetPedOnSeat(VehicleSeat.Driver) == guess;
            if (!drive)
            {
                if (_guessDriving) { Ctx.Crew.CompanionAI.ReleaseControl(CrewSlot.Guess); _guessDriving = false; _driverStage = -1; }
                return;
            }
            if (!_guessDriving) { Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Guess); _guessDriving = true; _nextDriverUpdate = 0; }
            if (Game.GameTime < _nextDriverUpdate) return;
            _nextDriverUpdate = Game.GameTime + 750;
            if (Stage == 3)
            {
                foreach (var hero in Protagonist.All)
                {
                    var member = Ctx.Crew.PedFor(hero.Slot);
                    if (member == null || !member.Exists() || !member.IsInVehicle(_chase))
                    { Function.Call(Hash.TASK_VEHICLE_TEMP_ACTION, guess, _chase, 27, 1500); _driverStage = -1; return; }
                }
            }
            if (_driverStage == Stage) return;
            _driverStage = Stage;
            if (Stage < 2 && _van != null && _van.Exists())
                Function.Call(Hash.TASK_VEHICLE_FOLLOW, guess, _chase, _van, 27f, (int)DrivingStyle.Normal, 18);
            else if (Stage == 2 && _van != null && _van.Exists())
                guess.Task.DriveTo(_chase, _van.Position - _van.ForwardVector * 10f, 5f, 7f, DrivingStyle.Normal);
            else guess.Task.DriveTo(_chase, Ctx.Locations.Position("M02.CanalEscape"), 10f, 22f, DrivingStyle.Normal);
        }

        private void UpdatePursuit(Ped player)
        {
            ObjectiveMarkers.Navigation(_van.Position, null, _chase);
            if (_chase.Position.DistanceTo(_van.Position) > 60f) return;
            Say("M02_S1_02_GUESS");
            Objective("Stay within 35m. Gohan can hack from his passenger seat while Guess drives.");
            Advance();
        }

        private void UpdateHack(Ped player)
        {
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            bool seated = gohan != null && gohan.Exists() && gohan.IsAlive && gohan.IsInVehicle(_chase) &&
                _chase.GetPedOnSeat(VehicleSeat.Driver) != gohan;
            ObjectiveMarkers.Navigation(_van.Position, null, _chase);
            float distance = _chase.Position.DistanceTo(_van.Position);
            bool connected = seated && distance <= HackRange;
            _hack.Update(Game.GameTime, connected);
            int percent = (int)(_hack.Progress * 100f);
            GameUtils.DrawObjectiveMarker(_van.Position + new Vector3(0f,0f,2.2f), Color.Green, .6f);
            GameUtils.Subtitle("~y~Gohan's remote hack: " + percent + "%  ~s~" + (int)distance + "/35m  " +
                (!seated ? "Gohan must be a passenger in the crew car." : !connected ? "Signal lost - close the gap." : "Connected - keep the van in range."), 500);
            if (!_alerted && _hack.Progress >= .5f)
            {
                _alerted = true;
                Say("M02_S2_03_ICE");
                foreach (var gunner in _gunners)
                {
                    if (gunner == null || !gunner.Exists() || gunner.IsDead) continue;
                    gunner.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_AEGIS");
                    gunner.Weapons.Give(WeaponHash.MicroSMG, 300, true, true);
                }
                _nextGunfire = 0;
            }
            if (_hack.Progress >= 1f) DisableVan();
        }

        private void MaintainGunfire(Ped player)
        {
            if (!_alerted || Game.GameTime < _nextGunfire) return;
            _nextGunfire = Game.GameTime + 1200;
            foreach (var gunner in _gunners)
            {
                if (gunner == null || !gunner.Exists() || gunner.IsDead) continue;
                if (gunner.IsInVehicle()) Function.Call(Hash.TASK_VEHICLE_SHOOT_AT_PED, gunner, player, 35f);
                else gunner.Task.FightAgainst(player);
            }
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            if (Ctx.Crew.ActiveSlot != CrewSlot.Ice && ice != null && ice.Exists() && ice.IsInVehicle(_chase))
                foreach (var gunner in _gunners)
                    if (gunner != null && gunner.Exists() && gunner.IsAlive)
                    { Function.Call(Hash.TASK_VEHICLE_SHOOT_AT_PED, ice, gunner, 35f); break; }
        }

        /// <summary>
        /// Kills the van without destroying it — the bible is explicit that the
        /// physical server has to survive, so this stalls the drivetrain rather than
        /// blowing the engine block.
        /// </summary>
        private void DisableVan()
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

            foreach (var gunner in _gunners)
                if (gunner != null && gunner.Exists() && gunner.IsAlive) gunner.Task.LeaveVehicle(LeaveVehicleFlags.None);

            _vanBlip.ShowRoute = false;
            Objective("Switch to Ice, exit the Granger and stand at the marked rear doors of the van for 3 seconds to collect the server drives.");
            Advance();
        }

        // ---------- Stage 2: Ice breaches the rear doors ----------

        private void UpdateBreach(Ped player)
        {
            var rear = _van.Position - _van.ForwardVector * 3.2f;
            ObjectiveMarkers.Navigation(rear, CrewSlot.Ice);
            GameUtils.DrawObjectiveMarker(rear, Color.FromArgb(130, 66, 133, 244), 1.2f);

            if (Ctx.Crew.ActiveSlot != CrewSlot.Ice)
            {
                _breachStarted = 0;
                GameUtils.Subtitle("~y~Switch to Ice, exit the Granger and collect the drives at the van's marked REAR doors.", 1200);
                return;
            }

            if (!GameUtils.IsWithin(player.Position, rear, 3.5f) || player.IsInVehicle())
            {
                _breachStarted = 0;
                GameUtils.Subtitle("~y~Ice: get out and reach the blue marker BEHIND the van. Taking the van is not the objective.", 500);
                return;
            }
            if (_breachStarted == 0)
            {
                _breachStarted = Game.GameTime;
                Function.Call(Hash.SET_VEHICLE_DOOR_OPEN, _van, 2, false, false);
                Function.Call(Hash.SET_VEHICLE_DOOR_OPEN, _van, 3, false, false);
            }
            if (Game.GameTime - _breachStarted < 3000) { GameUtils.Subtitle("~y~Collecting server drives... stay at the rear doors.", 500); return; }

            Say("M02_S2_05_ICE");
            _driveSeized = true;

            if (_technician != null && _technician.Exists() && _technician.IsAlive)
            {
                _technician.Task.HandsUp(20000);
            }

            SpawnChopper();
            Objective("Return all three to the Granger, then follow the GPS to the storm canal with the server drives.");
            Advance();
        }

        // ---------- Stage 3: the aqueduct escape ----------

        private void UpdateEscape(Ped player)
        {

            var canal = Ctx.Locations.Position("M02.CanalEscape");
            ObjectiveMarkers.Navigation(canal, null, _chase);
            GameUtils.DrawObjectiveMarker(canal, Color.FromArgb(120, 106, 168, 122), 5f);

            if (!_chopperCalled && SecondsInStage >= 3)
            {
                _chopperCalled = true;
                Say("M02_S2_06_GUESS");
            }

            if (!player.IsInVehicle(_chase)) { GameUtils.Subtitle("~y~Return to the crew's Granger.", 500); return; }
            foreach (var hero in Protagonist.All)
            {
                var member = Ctx.Crew.PedFor(hero.Slot);
                if (member == null || !member.Exists() || !member.IsAlive || !member.IsInVehicle(_chase))
                { GameUtils.Subtitle("~y~Bring all three back into the Granger before escaping.", 500); return; }
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

            var road = World.GetNextPositionOnStreet(start + _chase.ForwardVector * 90f);
            if (road == Vector3.Zero || road.DistanceTo(start) < 45f || road.DistanceTo(start) > 170f) { vanModel.MarkAsNoLongerNeeded(); return false; }
            _van = Track(World.CreateVehicle(vanModel, road, heading));
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
                    _technician.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_TRAFFIC");
                    _technician.IsPersistent = true;
                    _technician.BlockPermanentEvents = true;
                    _technician.Task.WarpIntoVehicle(_van, VehicleSeat.Driver);
                    // Runs the road rather than a fixed path: the route survives the
                    // start point being approximate.
                    _technician.Task.CruiseWithVehicle(_van, 20f, DrivingStyle.Normal);
                }
            }

            if (_technician == null || !_technician.Exists()) return false;
            var gunnerModel = new Model("s_m_y_blackops_01");
            if (!GameUtils.RequestModel(gunnerModel)) return false;
            for (int i=0; i<2; i++)
            {
                var gunner = Track(World.CreatePed(gunnerModel, _van.Position, heading));
                if (gunner == null || !gunner.Exists()) { gunnerModel.MarkAsNoLongerNeeded(); return false; }
                gunner.IsPersistent = true; gunner.BlockPermanentEvents = true; gunner.Accuracy = 20;
                gunner.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_TRAFFIC");
                gunner.SetIntoVehicle(_van, (VehicleSeat)i);
                _gunners.Add(gunner);
            }
            gunnerModel.MarkAsNoLongerNeeded();
            _vanBlip = Track(_van.AddBlip());
            _vanBlip.Sprite = BlipSprite.ArmoredTruck;
            _vanBlip.Color = BlipColor.Red;
            _vanBlip.Name = "Aegis comm-van";
            _vanBlip.ShowRoute = false;
            return true;
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
            // Restoring past the remote hack means the van must already be dead, or the player
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
            Ctx.Crew.CompanionAI.ReleaseControl(CrewSlot.Guess);
            _guessDriving = false;
            GameUtils.SafeDelete(_vanBlip);
            _gunners.Clear();
            Ctx.Crew.CompanionAI.RequireSharedVehicle = false;
        }
    }
}
