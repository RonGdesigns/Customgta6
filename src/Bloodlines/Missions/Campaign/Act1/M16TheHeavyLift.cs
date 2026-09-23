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
    /// M16 — "The Heavy Lift". Fort Zancudo outer logistics, 03:00, high winds.
    ///
    /// The Cargobob the Port Heist needs. The crew walks into a military base because
    /// of the IFF transponder Ice ripped out of a convoy truck in M09 — so the base
    /// does not light up until they take the helipad, and the mission says so by
    /// suppressing the wanted level until the shooting starts.
    ///
    /// Seen, not told: the unit itself on the Granger's dash at the fence, the
    /// Cargobob on the pad, Ice's cover and the canyon exit named, before a step
    /// is taken; the loss of the clearance as a challenge on the net, not
    /// unexplained heat; Ice boarding the lift and Gohan taking the Granger out by
    /// road, because the aircraft has the seats it has; the Lazers lost before
    /// Terminal, not erased by a marker; the lift landed and recorded where M18
    /// finds it, and the transponder spent.
    ///
    /// That continuity is the point: M09 is the reason this mission is survivable,
    /// and the player should feel the thing they stole being spent.
    /// </summary>
    public sealed class M16TheHeavyLift : ComposedMission
    {
        private readonly List<Ped> _militaryPolice = new List<Ped>();

        private Vehicle _cargobob;
        private Vehicle _granger;
        private Prop _unit;
        private Vector3 _fence;
        private Vector3 _helipad;
        private Vector3 _canyon;
        private Vector3 _canyonExit;
        /// <summary>
        /// How close the lift has to come to the canyon's east mouth. The mouth is 415 m on
        /// from the canyon run point and the run stage passes inside 120 m of that, so this
        /// cannot already be true the moment the stage opens.
        /// </summary>
        public const float CanyonExitRadius = 100f;
        private Vector3 _terminal;
        private int _boardingSince, _departureSince = -1, _previousWantedMaximum = 5;
        private bool _boardingOrdered, _roadOrdered, _departureOver;
        private bool _blackout;
        private int _tankResponseAt = -1, _tankCount, _nextTankOrder;
        private readonly List<Vehicle> _tanks = new List<Vehicle>();
        private readonly List<Ped> _tankDrivers = new List<Ped>();
        /// <summary>How long a tank's attack order stands with nothing changed before it is given again.</summary>
        public const int TankRefreshMs = 15000;
        /// <summary>How many military police hold the pad; the stage that clears them needs every one.</summary>
        public const int PadGuards = 6;
        private readonly Dictionary<int, int> _tankTarget = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _tankOrderedAt = new Dictionary<int, int>();
        /// <summary>How many attack orders the armor has been given, for the harness.</summary>
        public int TankOrders { get; private set; }
        public IReadOnlyList<Vehicle> Tanks => _tanks;
        private bool _challenged, _crewMoved, _landed;

        /// <summary>This mission's claim on the city lights; see Core/WorldLights.</summary>
        private const string LightOwner = "M16.Blackout";
        public override string Id => "M16";
        public override string Title => "The Heavy Lift";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;

        public Vehicle Cargobob => _cargobob;
        public Vehicle Granger => _granger;
        public Prop Unit => _unit;
        public bool Challenged => _challenged;
        public bool CrewMoved => _crewMoved;
        public bool Landed => _landed;

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _fence = Ctx.Locations.Position("M16.DepotFence");
            _helipad = Ctx.Locations.Position("M16.Helipad");
            _canyon = Ctx.Locations.Position("M16.CanyonRun");
            _canyonExit = Ctx.Locations.Position("M16.CanyonExit");
            _terminal = Ctx.Locations.Position("M16.TerminalDrop");

            if (!Ctx.Crew.Deploy(CrewSlot.Ice, _fence, Ctx.Locations.Heading("M16.DepotFence")))
            {
                return false;
            }

            ApplyBibleSetting();

            // The transponder from M09, spent: Zancudo reads them as friendly until
            // they start shooting.
            _previousWantedMaximum = Function.Call<int>(Hash.GET_MAX_WANTED_LEVEL);
            Function.Call(Hash.SET_MAX_WANTED_LEVEL, 0);
            Game.Player.WantedLevel = 0;
            if (Ctx.State?.CargoAt("iffTransponder") == null)
                Logger.Warn("M16: the campaign does not record the IFF unit; the approach is played as if it were on the dash anyway.");

            SpawnMilitaryPolice();
            // An empty pad is a "clear the pad" stage that finishes the moment it opens.
            if (_militaryPolice.Count != PadGuards)
            { Logger.Error("M16: only " + _militaryPolice.Count + " of " + PadGuards + " military police loaded on the pad."); return false; }
            SpawnCargobob();
            SpawnGranger();
            if (!RequireAssets(_cargobob, _granger)) return false;
            Station(CrewSlot.Ice, _granger, VehicleSeat.Driver);
            Station(CrewSlot.Guess, _granger, VehicleSeat.RightFront);
            Station(CrewSlot.Gohan, _granger, VehicleSeat.RightRear);
            _blackout=true;
            WorldLights.Darken(LightOwner);
            Radio("GUESS","Gohan, kill the base grid before we reach that gate. We need time to take the hangar.","M16_BLACKOUT_GUESS");
            Radio("GOHAN","Grid down. Anti-air targeting is blind. Their tank crews have to mobilize manually; keep moving once the hangar is clear.","M16_BLACKOUT_GOHAN");
            PlayApproach();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Walk in",
                    new ReachZoneObjective("Ice: drive from the freeway approach to the hangar, then clear its guards.",
                        () => _helipad, 30f, flat: true))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(context =>
                    GameUtils.Subtitle("~y~Code 7-Echo-Victor. The transponder gets you through the outer yard. Crossing the inner pad starts the fight.", 6000));

            // The clearance is burnt the moment they cross the pad: a challenge on
            // the net says so, then the heat.
            yield return new MissionStage("Take the helipad",
                    new KillTargetsObjective("Clear the military police off the pad.",
                        () => _militaryPolice))
                .OnEnter(context => Challenge())
                .AfterCues("M16_S1_01_ICE");

            yield return new MissionStage("Spool the twins",
                    new EnterVehicleObjective("Guess — take the Cargobob.", () => _cargobob,
                        VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context => MoveCrew());

            // Ice boards the lift; Gohan takes the Granger out by road. The aircraft
            // has the seats it has, and nobody is imagined into one it lacks.
            yield return new MissionStage("Ice aboard",
                    new ConditionObjective("Hold the pad while Ice boards.", IceAboard),
                    new ProtectObjective("", () => _cargobob, "The Cargobob is gone."))
                .OwnedBy(CrewSlot.Guess);

            yield return new MissionStage("Raton Canyon",
                    new AltitudeCeilingObjective("Hug the canyon — stay under 60 meters above terrain.", 60f,
                        "You exposed the heavy lift above the canyon route."),
                    new DeliverVehicleObjective("Guess: fly the Cargobob through the marked canyon route.", () => _cargobob, () => _canyon, 120f))
                .WithCues("M16_S1_02_GUESS");

            // The base's heat is lost on the way, not at the flats. This used to aim at the
            // canyon run point itself, with a wider radius than the stage before it, so it was
            // already true the moment it opened. The lift now has to reach the canyon's east
            // mouth, M16.CanyonExit, with the player aboard it.
            yield return new MissionStage("Clear the canyon",
                    new DeliverVehicleObjective("Guess: fly the lift out of the canyon's east mouth. Gohan keeps base anti-air offline until the lift is delivered.",
                        () => _cargobob, () => _canyonExit, CanyonExitRadius),
                    new ProtectObjective("", () => _cargobob, "The Cargobob is gone."));

            yield return new MissionStage("Terminal Island",
                    new DeliverVehicleObjective("Put the Cargobob down at Terminal Island.",
                        () => _cargobob, () => _terminal, 40f, land: true))
                .OnExit(context => PlayLift())
                .WithCues("M16_S1_03_ICE");
        }

        // ---------- beats ----------

        /// <summary>The unit on the dash, the Cargobob on the pad, Ice at the fence: the theft and its exit before a step.</summary>
        private void PlayApproach()
        {
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var blocking = new SceneBlocking();
            if (_unit != null && _unit.Exists()) blocking.Then(new ShotStep(3000, _unit, new Vector3(-0.9f, -1.2f, 0.6f), _unit, new Vector3(0f, 0f, 0.1f), 0.2f));
            else if (_granger != null && _granger.Exists()) blocking.Then(new ShotStep(3000, _granger, new Vector3(-5f, 3f, 1.6f), _granger, new Vector3(0f, 0f, 0.8f), 0.6f));
            if (_cargobob != null && _cargobob.Exists()) blocking.Then(new ShotStep(3400, _cargobob, new Vector3(-16f, 8f, 4f), _cargobob, new Vector3(0f, 0f, 1.5f), 1.4f));
            if (ice != null && ice.Exists()) blocking.Then(ShotStep.Watching(2800, ice, ice));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "Zancudo",
                Reason = "The IFF unit from the convoy on the Granger's dash: it opens the outer yard once. The Cargobob on the pad is Ron's; Ice clears the pad; the way out is Raton Canyon, low, to Terminal Island.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M16 approach scene did not play; the fence stands on its own.");
        }

        /// <summary>The clearance no longer covers them: a challenge on the net, then the base's heat.</summary>
        private void Challenge()
        {
            _challenged = true;
            // Scripted guards fight immediately; the blackout holds ambient dispatch.
            Function.Call(Hash.SET_MAX_WANTED_LEVEL, 0);
            Game.Player.WantedLevel = 0;
            foreach (var police in _militaryPolice)
            {
                if (police != null && police.Exists()) { police.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_AEGIS"); police.Task.FightAgainstHatedTargets(90f); }
            }
            Radio("GOHAN", "Challenge on the net: they're asking 7-Echo-Victor to confirm and it can't. The code's burned. That's the alarm.", "M16_RADIO_01_GOHAN");
            Ctx.State?.SetCargo("iffTransponder", null);
        }

        /// <summary>Ron in the lift: Ice comes to board it; Gohan takes the Granger out by the road to Terminal.</summary>
        private void MoveCrew()
        {
            _crewMoved = true; _tankResponseAt = Game.GameTime + 14000;
            _boardingSince = Game.GameTime;
            // Ice is still the player on this frame. Order boarding after the switch,
            // while Guess is approaching, never clear the current player's task here.
            Radio("ICE", "The hangar's clear. I'll board while Ron comes over. Gohan takes the Granger to Terminal by road.", "M16_RADIO_02_ICE");
        }

        private bool IceAboard()
        {
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            if (ice == null || !ice.Exists() || ice.IsDead || _cargobob == null || !_cargobob.Exists()) return false;
            if (ice.IsInVehicle(_cargobob)) return true;
            // A pad the navmesh refuses: after a bounded wait he is put in the seat, logged.
            if (_boardingOrdered && Game.GameTime - _boardingSince > 8000 && _cargobob.Speed < 1f && _cargobob.GetPedOnSeat(VehicleSeat.RightFront) == null)
            {
                Logger.Warn("M16: Ice could not finish the door entry in 8 s; recovering into the empty, stopped passenger seat.");
                ice.SetIntoVehicle(_cargobob, VehicleSeat.RightFront);
                return ice.IsInVehicle(_cargobob);
            }
            return false;
        }

        /// <summary>The lift landed on the flats: its job is one container, once. Recorded where M18 finds it.</summary>
        private void PlayLift()
        {
            _landed = true;
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            var blocking = new SceneBlocking();
            if (guess != null && guess.Exists() && _cargobob != null && _cargobob.Exists() && guess.IsInVehicle(_cargobob)) blocking.Then(new ExitVehicleStep(guess));
            if (_cargobob != null && _cargobob.Exists()) blocking.Then(new ShotStep(4200, _cargobob, new Vector3(-14f, 7f, 3.5f), _cargobob, new Vector3(0f, 0f, 1.5f), 1.2f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "lift", Title = "The lift",
                Reason = "The Cargobob on the Terminal Island flats with its rotors winding down: its job is one container out of Berth 44, once. It waits here until Gohan cuts the hull. The transponder is spent.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) { Logger.Warn("M16 lift scene did not play; the aircraft is parked directly."); blocking.Complete(); }
            Ctx.State?.SetCargo("cargobob", "M16.TerminalDrop");
            if (_cargobob != null && _cargobob.Exists()) { _cargobob.IsEngineRunning = false; Release(_cargobob); }
            GameUtils.Subtitle("~g~Heavy lift on the flats. The clearance trick is burned; Berth 44 is a question of timing now.", 6000);
        }

        /// <summary>The aftermath: the lift on the flats, the crew around it.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_cargobob == null || !_cargobob.Exists()) return null;
            return new SceneBlocking().Then(new ShotStep(4500, _cargobob, new Vector3(-18f, 9f, 4f), _cargobob, new Vector3(0f, 0f, 1.5f), 1.6f));
        }

        // ---------- world building ----------

        private void SpawnMilitaryPolice()
        {
            var model = new Model("s_m_y_marine_03");
            if (!GameUtils.RequestModel(model)) return;

            for (int i = 0; i < 6; i++)
            {
                var trooper = World.CreatePed(model, MissionSites.Actor(Ctx.Locations, "M16.Marine" + (i + 1), _helipad), Ctx.Locations.Heading("M16.Marine" + (i + 1)));
                if (trooper == null || !trooper.Exists()) continue;

                trooper.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_TRAFFIC");
                trooper.IsPersistent = true;
                trooper.BlockPermanentEvents = true;
                trooper.Accuracy = 40;
                trooper.Armor = 60;
                trooper.Weapons.Give(WeaponHash.CarbineRifle, 250, true, true);
                trooper.Task.GuardCurrentPosition();

                _militaryPolice.Add(Track(trooper));
            }

            model.MarkAsNoLongerNeeded();
        }

        private void SpawnCargobob()
        {
            var model = new Model("cargobob");
            if (!GameUtils.RequestModel(model)) return;

            _cargobob = Track(World.CreateVehicle(model, Ctx.Locations.Position("M16.CargobobSpawn"),
                Ctx.Locations.Heading("M16.CargobobSpawn")));
            model.MarkAsNoLongerNeeded();
            if (_cargobob == null || !_cargobob.Exists()) return;

            _cargobob.IsPersistent = true;
            _cargobob.PlaceOnGround();

            var blip = Track(_cargobob.AddBlip());
            blip.Sprite = BlipSprite.Helicopter;
            blip.Color = BlipColor.Orange;
            blip.Name = "Cargobob";
        }

        /// <summary>The crew's Granger at the fence with the IFF unit on its dash: the approach, seen.</summary>
        private void SpawnGranger()
        {
            var spot = Ctx.Locations.Position("M16.GrangerSpawn");
            Vehicle granger = Ctx.Vans != null ? Ctx.Vans.Spawn(spot, Ctx.Locations.Heading("M16.DepotFence")) : null;
            if (granger == null)
            {
                var model = new Model("granger");
                if (!GameUtils.RequestModel(model)) return;
                granger = World.CreateVehicle(model, spot, Ctx.Locations.Heading("M16.DepotFence"));
                model.MarkAsNoLongerNeeded();
            }
            _granger = Track(granger);
            if (_granger == null || !_granger.Exists()) return;
            _granger.IsPersistent = true;
            _granger.IsEngineRunning = false;
            var unitModel = new Model("prop_box_ammo03a");
            if (!GameUtils.RequestModel(unitModel)) return;
            _unit = Track(World.CreateProp(unitModel, _granger.Position + new Vector3(0f, 0f, 2f), false, false));
            unitModel.MarkAsNoLongerNeeded();
            if (_unit == null || !_unit.Exists()) { _unit = null; return; }
            StowPropStep.Stow(_unit, _granger, new Vector3(0.45f, 0.9f, 0.55f));
        }

        protected override void OnUpdate()
        {
            if(_blackout)
            {
                WorldLights.Darken(LightOwner);
                Function.Call(Hash.SET_MAX_WANTED_LEVEL,0);
                if(Game.Player.WantedLevel>0)Game.Player.WantedLevel=0;
                if(_cargobob!=null&&_cargobob.Exists())Function.Call(Hash.SET_VEHICLE_CAN_BE_TARGETTED,_cargobob,false);
            }
            if (_crewMoved && !Ctx.Cutscenes.IsActive)
            {
                var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
                var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
                if (Ctx.Crew.ActiveSlot == CrewSlot.Ice) _boardingOrdered = false;
                if (Ctx.Crew.ActiveSlot == CrewSlot.Gohan) _roadOrdered = false;
                if (Ctx.Crew.ActiveSlot != CrewSlot.Ice && ice != null && ice.Exists())
                {
                    Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Ice);
                    if (!_boardingOrdered)
                    {
                        _boardingOrdered = true; _boardingSince = Game.GameTime;
                        ice.Task.EnterVehicle(_cargobob, VehicleSeat.RightFront, 8000, 2f, EnterVehicleFlags.None);
                    }
                }
                if (Ctx.Crew.ActiveSlot != CrewSlot.Gohan && gohan != null && gohan.Exists())
                {
                    Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Gohan);
                    if (!_roadOrdered)
                    {
                        if (_granger.GetPedOnSeat(VehicleSeat.Driver) == gohan)
                        { gohan.Task.DriveTo(_granger, _terminal, 15f, 30f, DrivingStyle.Normal); _roadOrdered = true; }
                        else if (!Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE, gohan))
                            gohan.Task.EnterVehicle(_granger, VehicleSeat.Driver, 8000, 2f, EnterVehicleFlags.None);
                    }
                }
                UpdateDepartureCorridor(); UpdateTankResponse();
            }
            base.OnUpdate();
        }

        // Mission-local countermeasures. The vanilla armybase script independently
        // fires Stingers even with wanted suppression; never terminate it or patch
        // version-dependent script memory. Clear incoming projectiles near this lift
        // only, for a bounded departure, then leave normal pursuit untouched.
        private void UpdateDepartureCorridor()
        {
            if (!_blackout || _departureOver || _cargobob == null || !_cargobob.Exists()) return;
            var pilot = Ctx.Crew.PedFor(CrewSlot.Guess);
            if (pilot == null || !pilot.Exists() || !pilot.IsInVehicle(_cargobob)) return;
            if (_departureSince < 0) _departureSince = Game.GameTime;
            if (_cargobob.Position.DistanceTo(_helipad) > 1800f)
            { _departureOver = true; return; }
            var p = _cargobob.Position;
            Function.Call(Hash.CLEAR_AREA_OF_PROJECTILES, p.X, p.Y, p.Z, 180f, false);
        }

        private void UpdateTankResponse()
        {
            if(_cargobob==null||!_cargobob.Exists()||_cargobob.Position.DistanceTo(_helipad)>1200f)return;
            if(_tankCount<2&&_tankResponseAt>=0&&Game.GameTime>=_tankResponseAt)
            {
                _tankResponseAt=Game.GameTime+16000;
                var wanted=Ctx.Locations.Position("M16.TankEntry"+(_tankCount+1));
                Vector3 spot;float heading;
                if(!GameUtils.NearestRoadNode(wanted,35f,out spot,out heading))return;
                if(Function.Call<bool>(Hash.IS_POSITION_OCCUPIED,spot.X,spot.Y,spot.Z,5f,false,true,true,false,false,0,false))return;
                var model=new Model("rhino");var marine=new Model("s_m_y_marine_03");
                if(!GameUtils.RequestModel(model)||!GameUtils.RequestModel(marine))return;
                var tank=Track(World.CreateVehicle(model,spot,heading));
                var driver=Track(World.CreatePed(marine,spot,heading));
                model.MarkAsNoLongerNeeded();marine.MarkAsNoLongerNeeded();
                if(tank==null||!tank.Exists()||driver==null||!driver.Exists())return;
                tank.IsPersistent=true;tank.PlaceOnGround();driver.IsPersistent=true;driver.BlockPermanentEvents=true;
                driver.RelationshipGroup=World.AddRelationshipGroup("BLOODLINES_AEGIS");driver.SetIntoVehicle(tank,VehicleSeat.Driver);
                _tanks.Add(tank);_tankDrivers.Add(driver);_tankCount++;
                var blip=Track(tank.AddBlip());blip.Color=BlipColor.Red;blip.Name="Delayed armor response";
                Radio("GOHAN","Armor's rolling manually. Grid is still down. Get the lift out of there.","M16_TANK_"+_tankCount);
            }
            // Reviewed every four seconds, ordered only when the tank has somebody new to
            // attack or on a slow refresh: an attack mission handed over on every review
            // restarts before the tank can act on it (Ron, September 22).
            if(Game.GameTime<_nextTankOrder)return;_nextTankOrder=Game.GameTime+4000;
            var target=Game.Player.Character;
            for(int i=0;i<_tanks.Count;i++)
            {
                if(!_tanks[i].Exists()||_tanks[i].IsDead||!_tankDrivers[i].Exists()||_tankDrivers[i].IsDead||target==null)continue;
                int handle=_tankDrivers[i].Handle;
                if(_tankTarget.TryGetValue(handle,out var last)&&last==target.Handle&&
                   _tankOrderedAt.TryGetValue(handle,out var at)&&Game.GameTime-at<TankRefreshMs)continue;
                _tankDrivers[i].Task.StartVehicleMission(_tanks[i],target,VehicleMissionType.Attack,20f,(VehicleDrivingFlags)786603,25f,35f,true);
                _tankTarget[handle]=target.Handle;_tankOrderedAt[handle]=Game.GameTime;TankOrders++;
            }
        }

        protected override void OnPassed()
        {
            if (_granger != null && _granger.Exists()) Release(_granger);
        }

        protected override void OnCleanup()
        {
            _blackout=false;
            WorldLights.Restore(LightOwner);
            if(_cargobob!=null&&_cargobob.Exists())Function.Call(Hash.SET_VEHICLE_CAN_BE_TARGETTED,_cargobob,true);
            _tanks.Clear();_tankDrivers.Clear();_tankTarget.Clear();_tankOrderedAt.Clear();
            // Never leave the player's wanted ceiling where a mission put it.
            Function.Call(Hash.SET_MAX_WANTED_LEVEL, _previousWantedMaximum);
            foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Gohan }) Ctx.Crew.CompanionAI.ReleaseControl(slot);
            _departureOver = true;
            _militaryPolice.Clear();
        }
    }
}
