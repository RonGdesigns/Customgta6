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
    /// It opens at the curb where M01 ended: the prototype stays, the crew boards
    /// its own Granger, and only then does the clock start. The van drives a real
    /// route rather than idling at lights. The drives leave the van in Ice's hand
    /// and are stowed when he boards. The canal is an escape checkpoint: the police
    /// have to be lost, nothing clears them at the marker.
    ///
    /// Positions here are approximate (see LocationBook) — the van runs a road
    /// mission from the start point, so the mission tolerates the start being a few
    /// meters off in a way a waypoint list would not.
    /// </summary>
    public sealed class M02LooseStrands : Mission
    {
        private const int UploadWindowSeconds = 180;
        private const float HackRange = 35f;
        private const int StuckMs = 5000;
        private readonly ProximityHack _hack = new ProximityHack(24);
        private readonly List<Ped> _gunners = new List<Ped>();
        private bool _alerted;
        private int _nextGunfire;
        private int _driverStage = -1;

        private Vehicle _prototype;
        private Vehicle _van;
        private Ped _technician;
        private Vehicle _chase;
        private Blip _vanBlip;
        private Prop _drives;

        private int _startedAt;
        private int _breachStarted;
        private bool _stashDone;
        private bool _driveSeized;
        private bool _vanDisabled;
        private bool _chopperCalled;
        private bool _guessDriving;
        private int _nextDriverUpdate;
        private Vector3 _routeTarget;
        private int _nextRouteCheck, _stuckSince, _reissues;

        public override string Id => "M02";
        public override string Title => "Loose Strands";

        /// <summary>The canal is an escape checkpoint: the police are lost, never erased.</summary>
        public MissionEndpoint EndpointKind => MissionEndpoint.EscapeCheckpoint;
        public Prop Drives => _drives;
        public Vehicle Van => _van;
        public Vehicle Prototype => _prototype;
        public bool StashDone => _stashDone;

        protected override bool OnStart()
        {
            var requested = Ctx.Locations.Position("M02.InterceptStart");
            var start = World.GetNextPositionOnStreet(requested);
            if (start == Vector3.Zero || !GameUtils.IsWithinFlat(start, requested, 80f)) return false;
            float heading = Ctx.Locations.Heading("M02.InterceptStart");

            // On foot at the curb, between the car they arrived in and the one they leave in.
            if (!Ctx.Crew.Deploy(CrewSlot.Guess, start + new Vector3(2.5f, -3f, 0f), heading)) return false;

            ApplyBibleSetting();

            if (!SpawnPrototype(start, heading)) return false;
            if (!SpawnChaseCar(start, heading)) return false;
            if (!SpawnVan(start, heading)) return false;

            Ctx.Crew.CompanionAI.RequireSharedVehicle = true;
            foreach (var hero in Protagonist.All) Ctx.Crew.CompanionAI.TakeControl(hero.Slot);
            Preserve(_prototype);
            Preserve(_chase);
            PlayStash();
            Objective("Leave the prototype. Everyone into the Granger.");
            return true;
        }

        protected override void OnUpdate()
        {
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;
            if (!_stashDone) { BeginPursuit(); return; }
            MaintainPassengers();
            MaintainGuessDriving(player);
            MaintainGunfire(player);
            MaintainVanRoute();
            if (_chase == null || !_chase.Exists() || !_chase.IsDriveable) { Fail("The crew's Granger is wrecked."); return; }

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

                GameUtils.Subtitle("~s~Upload completes in ~r~" + remaining / 60 + ":" +
                                   (remaining % 60).ToString("00"), 500);
            }

            RequiredSwitch = Stage == 2 && Ctx.Crew.ActiveSlot != CrewSlot.Ice ? (CrewSlot?)CrewSlot.Ice : null;
            switch (Stage)
            {
                case 0: UpdatePursuit(player); break;
                case 1: UpdateHack(player); break;
                case 2: UpdateBreach(player); break;
                case 3: UpdateEscape(player); break;
            }
        }

        // ---------- The stash beat: the prototype stays, the Granger goes ----------

        /// <summary>
        /// Seen before anyone drives: the two cars at the curb, the crew boarding the
        /// Granger, the van's dome already moving off. Skipping warps them in. The
        /// upload clock does not start until this is over.
        /// </summary>
        private void PlayStash()
        {
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            var blocking = new SceneBlocking()
                .Then(ShotStep.Wide(3000, _chase.Position, 12f, 6f, 5f))
                .Then(new EnterVehicleStep(guess, _chase, VehicleSeat.Driver))
                .Then(new EnterVehicleStep(ice, _chase, VehicleSeat.RightFront))
                .Then(new EnterVehicleStep(gohan, _chase, VehicleSeat.LeftRear))
                .Then(new ShotStep(2500, _chase, new Vector3(-6f, 2.5f, 1.6f), _chase, new Vector3(0f, 0f, 0.8f), 1.2f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "stash", Title = "The car stays",
                Reason = "The prototype is left at the curb; the crew leaves in its own Granger with Guess driving, Ice in front and Gohan in the back with the signal.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) { Logger.Warn("M02 stash scene did not play; the crew boards the Granger directly."); blocking.Complete(); }
        }

        /// <summary>First tick after the stash beat: everyone aboard, the van on its route, the clock running.</summary>
        private void BeginPursuit()
        {
            _stashDone = true;
            var seats = new Dictionary<CrewSlot, VehicleSeat> { [CrewSlot.Guess] = VehicleSeat.Driver, [CrewSlot.Ice] = VehicleSeat.RightFront, [CrewSlot.Gohan] = VehicleSeat.LeftRear };
            foreach (var pair in seats)
            {
                var ped = Ctx.Crew.PedFor(pair.Key);
                if (ped != null && ped.Exists() && _chase != null && _chase.Exists() && !ped.IsInVehicle(_chase)) ped.SetIntoVehicle(_chase, pair.Value);
            }
            _startedAt = Game.GameTime;
            StartVanRoute();
            MaintainPassengers();
            SayStage(1);
            Objective("Catch the Aegis comm-van before the upload finishes.");
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

        // ---------- The van's route ----------

        /// <summary>
        /// The van is on a job of its own: a road mission toward a point far down the
        /// road it is on, reckless about lights and traffic, re-aimed further along
        /// whenever it gets close, so it never sits at an intersection waiting to be
        /// caught. It stops only when Gohan's work kills the drivetrain.
        /// </summary>
        private void StartVanRoute()
        {
            if (_van == null || !_van.Exists() || _technician == null || !_technician.Exists() || _vanDisabled) return;
            _routeTarget = NextRoutePoint();
            _technician.Task.StartVehicleMission(_van, _routeTarget, VehicleMissionType.GoTo, 24f,
                VehicleDrivingFlags.DrivingModeAvoidVehiclesReckless | VehicleDrivingFlags.UseShortCutLinks, 12f, 30f, false);
            Function.Call(Hash.SET_PED_KEEP_TASK, _technician, true);
            Function.Call(Hash.SET_DRIVER_ABILITY, _technician, 1f);
            Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, _technician, 0.6f);
            _stuckSince = 0;
        }

        private Vector3 NextRoutePoint()
        {
            var ahead = _van.Position + _van.ForwardVector * 700f;
            var road = World.GetNextPositionOnStreet(ahead);
            return road != Vector3.Zero && road.DistanceTo(_van.Position) > 150f ? road : ahead;
        }

        private void MaintainVanRoute()
        {
            if (_vanDisabled || _van == null || !_van.Exists() || _technician == null || !_technician.Exists() || !_technician.IsAlive) return;
            if (Game.GameTime < _nextRouteCheck) return;
            _nextRouteCheck = Game.GameTime + 1000;
            if (_van.Position.DistanceTo(_routeTarget) < 90f) { StartVanRoute(); return; }
            // Stuck watchdog: a van that has not moved for five seconds gets its
            // mission re-issued once; a second stall is logged, not hidden.
            if (_van.Speed > 0.5f) { _stuckSince = 0; return; }
            if (_stuckSince == 0) { _stuckSince = Game.GameTime; return; }
            if (Game.GameTime - _stuckSince < StuckMs) return;
            _reissues++;
            if (_reissues <= 1) { Logger.Warn("M02: the van has been stationary for 5 s; re-issuing its route once."); StartVanRoute(); }
            else if (_reissues == 2) Logger.Warn("M02: the van stalled again after a re-issue; leaving it to the engine.");
            _stuckSince = Game.GameTime;
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
            // Detection is the reaction, not a stage: halfway in they find the intrusion
            // and the windows open. Ice covers the car from the passenger seat.
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
        /// Gohan's work is what stops the van: the drivetrain dies under it and it
        /// rolls to a halt on its own. Nothing rams it; the bible is explicit that
        /// the physical server has to survive, so this stalls the drivetrain rather
        /// than blowing the engine block.
        /// </summary>
        private void DisableVan()
        {
            Say("M02_S2_04_GOHAN");
            _vanDisabled = true;

            _van.EngineHealth = 1f;
            _van.IsEngineRunning = false;
            _van.EnginePowerMultiplier = 0f;
            Function.Call(Hash.SET_VEHICLE_UNDRIVEABLE, _van, true);
            _van.IsDriveable = false;
            World.AddExplosion(_van.Position, ExplosionType.Extinguisher, 0.1f, 0.4f, Game.Player.Character, false, true);

            if (_technician != null && _technician.Exists())
            {
                _technician.Task.ClearAll();
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
            // Custody: the drives are in Ice's hand from here, stowed when he boards.
            SpawnDrives(player);
            Ctx.State?.SetEvidence("dockRecording", EvidenceState.CopyHeld);

            if (_technician != null && _technician.Exists() && _technician.IsAlive)
            {
                _technician.Task.HandsUp(20000);
            }

            SpawnChopper();
            Objective("All three back in the Granger. Lose the police and follow the GPS to the storm canal with the drives.");
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

            StowDrives();
            if (!player.IsInVehicle(_chase))
            {
                // The Granger is stopped for him. A press at its door is a seat,
                // whatever the engine makes of the drives in his hand or of the
                // scripted state the pursuit left his ped in (Ron, September 10).
                if (_chase.LockStatus != VehicleLockStatus.Unlocked) _chase.LockStatus = VehicleLockStatus.Unlocked;
                if (Ctx.Crew.CompanionAI.StateOf(Ctx.Crew.ActiveSlot) == CompanionState.Scripted) Ctx.Crew.CompanionAI.ReleaseControl(Ctx.Crew.ActiveSlot);
                if (!player.IsInVehicle() && GameUtils.IsWithinFlat(player.Position, _chase.Position, 4.5f))
                {
                    GameUtils.Subtitle("~y~Press E / D-pad Right to get in the Granger.", 500);
                    if (Game.IsControlJustPressed(GTA.Control.Context)) player.SetIntoVehicle(_chase, FreeSeat());
                    return;
                }
                GameUtils.Subtitle("~y~Return to the crew's Granger.", 500);
                return;
            }
            foreach (var hero in Protagonist.All)
            {
                var member = Ctx.Crew.PedFor(hero.Slot);
                if (member == null || !member.Exists() || !member.IsAlive || !member.IsInVehicle(_chase))
                { GameUtils.Subtitle("~y~Bring all three back into the Granger before escaping.", 500); return; }
            }
            // An escape checkpoint: the canal hides the car, it does not erase the
            // police. Whatever heat the chase earned has to be lost first.
            if (Game.Player.WantedLevel > 0) { GameUtils.Subtitle("~y~Lose the police before the canal.", 500); return; }
            if (!GameUtils.IsWithinFlat(player.Position, canal, 20f)) return;

            GameUtils.Subtitle("~g~Server drives secured. The upload never landed.", 5000);
            Pass();
        }

        private VehicleSeat FreeSeat()
        {
            foreach (var seat in new[] { VehicleSeat.RightFront, VehicleSeat.LeftRear, VehicleSeat.RightRear, VehicleSeat.Driver })
                if (_chase.GetPedOnSeat(seat) == null) return seat;
            return VehicleSeat.Any;
        }

        /// <summary>The drives leave Ice's hand once he is in the Granger: stowed, not carried into the next scene.</summary>
        private void StowDrives()
        {
            if (_drives == null || !_drives.Exists()) { _drives = null; return; }
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            if (ice == null || !ice.Exists() || !ice.IsInVehicle(_chase)) return;
            _drives.Detach();
            GameUtils.SafeDelete(_drives);
            _drives = null;
            GameUtils.Subtitle("~g~Drives stowed in the Granger.", 3000);
        }

        // ---------- world building ----------

        private bool SpawnPrototype(Vector3 start, float heading)
        {
            // The car M01 ended in, parked where it will be found. It stays.
            var model = new Model("schafter3");
            if (!GameUtils.RequestModel(model)) return false;
            _prototype = Track(World.CreateVehicle(model, start + new Vector3(0f, -14f, 0f), heading));
            model.MarkAsNoLongerNeeded();
            if (_prototype == null || !_prototype.Exists()) return false;
            _prototype.IsPersistent = true;
            _prototype.IsEngineRunning = false;
            _prototype.Mods.CustomPrimaryColor = System.Drawing.Color.Black;
            _prototype.LockStatus = VehicleLockStatus.CannotEnter;
            return true;
        }

        private bool SpawnChaseCar(Vector3 start, float heading)
        {
            // The crew's own Granger, with whatever they have done to it at the shops.
            var spot = start + new Vector3(0f, -6f, 0f);
            _chase = Track(Ctx.Vans != null ? Ctx.Vans.Spawn(spot, heading) : StockGranger(spot, heading));
            if (_chase == null || !_chase.Exists()) return false;

            _chase.IsPersistent = true;
            _chase.IsEngineRunning = true;
            return true;
        }

        private static Vehicle StockGranger(Vector3 position, float heading)
        {
            var model = new Model("granger");
            if (!GameUtils.RequestModel(model)) return null;
            var van = World.CreateVehicle(model, position, heading);
            model.MarkAsNoLongerNeeded();
            return van;
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
                    // The route mission starts when the stash beat ends and the clock with it.
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

        private void SpawnDrives(Ped ice)
        {
            var model = new Model("prop_ld_case_01");
            if (!GameUtils.RequestModel(model)) return;
            _drives = Track(World.CreateProp(model, ice.Position + new Vector3(0f, 0f, 1f), false, false));
            model.MarkAsNoLongerNeeded();
            if (_drives == null || !_drives.Exists()) { _drives = null; return; }
            CarryPropStep.Attach(ice, _drives, new Vector3(0.12f, 0.02f, -0.02f), new Vector3(0f, 90f, 0f));
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
            // A warp past the stash beat still needs the crew aboard and the clock running.
            if (stage >= 0 && !_stashDone && Ctx?.Cutscenes != null && !Ctx.Cutscenes.IsActive) BeginPursuit();
            // Restoring past the remote hack means the van must already be dead, or the player
            // resumes chasing a van that cannot be caught again.
            if (stage >= 2 && _van != null && _van.Exists())
            {
                _vanDisabled = true;
                _van.IsDriveable = false;
                _van.IsEngineRunning = false;
            }

            if (stage >= 2) _driveSeized = stage >= 3;
        }

        protected override void OnPassed()
        {
            // The Granger is the crew's ride home; the prototype is part of the street now.
            if (_chase != null && _chase.Exists()) Release(_chase);
            if (_prototype != null && _prototype.Exists()) Release(_prototype);
        }

        protected override void OnCleanup()
        {
            foreach (var hero in Protagonist.All) Ctx.Crew.CompanionAI.ReleaseControl(hero.Slot);
            _guessDriving = false;
            GameUtils.SafeDelete(_vanBlip);
            _gunners.Clear();
            Ctx.Crew.CompanionAI.RequireSharedVehicle = false;
        }
    }
}
