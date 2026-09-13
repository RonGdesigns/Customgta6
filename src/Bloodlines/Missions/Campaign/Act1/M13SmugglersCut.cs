using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M13 — "Smuggler's Cut". La Puerta fuel basin, 02:00, drizzle.
    ///
    /// Ice rides the water scooter in and puts limpet charges on three cartel fuel
    /// barges so the Port Heist has fewer pursuit craft. Gohan holds the harbor
    /// alarms; Guess idles on the slipway with the lights off.
    ///
    /// Seen, not told: the three barges and the slipway pickup before Ice goes in,
    /// with the fuel workers named as workers and not Aegis; the harbor alarm as
    /// the reason to leave once the charges are set, shown coming; the charges dark
    /// until Ice is aboard the Granger; the basin burning as a result view from the
    /// slipway; the reduced patrol fleet recorded for M21, which still has boats.
    ///
    /// Three sites, one objective — the player picks the order, and the tension is
    /// that every second on a barge is a second in the open. The detonation is the
    /// reward, not the work.
    /// </summary>
    public sealed class M13SmugglersCut : ComposedMission
    {
        private readonly List<Vector3> _barges = new List<Vector3>();
        private readonly List<Vehicle> _fuelBoats = new List<Vehicle>();
        private readonly List<Ped> _watchmen = new List<Ped>();

        private Vehicle _kayak;
        private Vehicle _granger;
        private Vehicle _alarmBoat;
        private Ped _alarmCrew;
        private readonly List<Vehicle> _pursuitBoats = new List<Vehicle>();
        private readonly List<Ped> _drivers = new List<Ped>(), _gunners = new List<Ped>();
        private int _nextPursuit;
        private Vector3 _launch;
        private Vector3 _slipway;
        private bool _alarmShown, _blown;

        public override string Id => "M13";
        public override string Title => "Smuggler's Cut";
        protected override MissionEndpoint Endpoint => MissionEndpoint.EscapeCheckpoint;

        public Vehicle Kayak => _kayak;
        public Vehicle Granger => _granger;
        public Vehicle AlarmBoat => _alarmBoat;
        public bool AlarmShown => _alarmShown;
        public bool Blown => _blown;

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _launch = Ctx.Locations.Position("M13.KayakLaunch");
            _slipway = Ctx.Locations.Position("M13.CanalSlipway");
            _barges.Add(Ctx.Locations.Position("M13.BargeOne"));
            _barges.Add(Ctx.Locations.Position("M13.BargeTwo"));
            _barges.Add(Ctx.Locations.Position("M13.BargeThree"));

            if (!Ctx.Crew.Deploy(CrewSlot.Ice, _launch, Ctx.Locations.Heading("M13.KayakLaunch")))
            {
                return false;
            }

            ApplyBibleSetting();
            Game.Player.Character.Weapons.Give(WeaponHash.StickyBomb, 6, false, true);

            if (!SpawnFuelBoats()) return false;
            SpawnKayak();
            SpawnGranger();
            SpawnWatchmen();
            if (!RequireAssets(_kayak, _granger)) return false;
            Station(CrewSlot.Ice, _kayak, VehicleSeat.Driver);
            Station(CrewSlot.Guess, _granger, VehicleSeat.Driver);
            Station(CrewSlot.Gohan, _granger, VehicleSeat.Passenger);
            PlayApproach();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Into the basin",
                    new EnterVehicleObjective("Ice — take the water scooter into the basin.", () => _kayak))
                .OwnedBy(CrewSlot.Ice);

            yield return new MissionStage("Limpets",
                    new MultiHoldObjective("Ice: approach the marked side of each fuel boat and press E / D-pad Right to plant a charge.",
                        _barges, 6, 6f, "Arming the charge", () => _kayak),
                    new AvoidDetectionObjective(() => _watchmen,
                        "A dock watchman called it in before the charges were set.", 40f, 4))
                .OwnedBy(CrewSlot.Ice)
                .OnExit(context => SpawnAlarmBoat())
                .AfterCues("M13_S1_01_ICE");

            // The alarm is the reason to leave: a launch turning into the basin,
            // shown once. The charges stay dark until Ice is aboard.
            yield return new MissionStage("Clear the water",
                    new ReachZoneObjective("Ice: return to the southern quay, climb out onto the dock, and reach Guess at the car marker.", () => _slipway, 10f),
                    new ReactionTrigger(() => !_alarmShown && !Ctx.Cutscenes.IsActive, ShowAlarm))
                .OnEnter(context => GameUtils.Subtitle("~y~Guess is idling on the slipway. Move.", 4000))
                .WithCues("M13_S1_02_GUESS");

            yield return new MissionStage("Board Guess's car",
                    new EnterVehicleObjective("Ice: get into an empty passenger seat in Guess's Granger (F / Y or E / D-pad Right).", () => _granger))
                .OwnedBy(CrewSlot.Ice);

            yield return new MissionStage("Blow the basin",
                    new MissionInteraction("Ice: board the Granger, then trigger the planted charges", () => _granger.Position, 1, 5f, () => _granger))
                .OnExit(context =>
                {
                    Detonate();
                    PlayResult();
                    // Fewer boats, not none: M21's opposition reflects this, it is not erased by it.
                    Ctx.State?.SetUpgrade("harborPatrolsReduced", true);
                    GameUtils.Subtitle("~g~Basin's burning. Fewer boats behind the lift, not none. Radar above us is next.", 6000);
                });
        }

        // ---------- beats ----------

        /// <summary>The three barges over the water, the workers on the slipway named as workers, the Granger with its lights off: the exit before the job.</summary>
        private void PlayApproach()
        {
            var blocking = new SceneBlocking()
                .Then(ShotStep.Wide(3400, _barges[1] + new Vector3(0f, 0f, 3f), 45f, 16f, 12f))
                .Then(new ShotStep(3000, null, _barges[0] + new Vector3(-8f, -6f, 3f), null, _barges[0] + new Vector3(0f, 0f, 1f), 0.6f));
            if (_granger != null && _granger.Exists()) blocking.Then(new ShotStep(3000, _granger, new Vector3(-6f, 3f, 1.8f), _granger, new Vector3(0f, 0f, 0.7f), 0.8f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "The basin",
                Reason = "Three fuel barges at their moorings, the men on the slipway who are dock workers and not Aegis, and Ron's Granger dark at the western slipway: the way out before the way in.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M13 approach scene did not play; the basin stands on its own.");
        }

        /// <summary>The harbor alarm turned into a launch: it enters the basin from the moorings, and that is the reason to leave.</summary>
        private void SpawnAlarmBoat()
        {
            var boatModel = new Model("predator");
            var crewModel = new Model("s_m_y_blackops_01");
            if (!GameUtils.RequestModel(boatModel) || !GameUtils.RequestModel(crewModel)) return;
            for (int i=0;i<2;i++)
            {
                string key=i==0?"M13.AlarmSpawn":"M13.AlarmSpawn2";
                var far=MarineSites.ResolveOrThrow(Ctx.Locations,key,3f,3f,6f);
                var boat=Track(World.CreateVehicle(boatModel,far,Ctx.Locations.Heading(key)));
                if(!RequireAssets(boat)){boatModel.MarkAsNoLongerNeeded();crewModel.MarkAsNoLongerNeeded();return;}
                boat.IsPersistent=true;boat.IsEngineRunning=true;
                var driver=Track(World.CreatePed(crewModel,far,0f));
                var gunner=Track(World.CreatePed(crewModel,far,0f));
                if(!RequireAssets(driver,gunner)){boatModel.MarkAsNoLongerNeeded();crewModel.MarkAsNoLongerNeeded();return;}
                foreach(var ped in new[]{driver,gunner})
                {ped.IsPersistent=true;ped.BlockPermanentEvents=true;ped.RelationshipGroup=World.AddRelationshipGroup("BLOODLINES_CARTEL");ped.Accuracy=22;ped.Weapons.Give(WeaponHash.CarbineRifle,300,true,true);}
                driver.SetIntoVehicle(boat,VehicleSeat.Driver);gunner.SetIntoVehicle(boat,VehicleSeat.Passenger);
                _pursuitBoats.Add(boat);_drivers.Add(driver);_gunners.Add(gunner);
                if(i==0){_alarmBoat=boat;_alarmCrew=driver;}
                var blip=Track(boat.AddBlip());blip.Sprite=BlipSprite.Boat;blip.Color=BlipColor.Red;blip.Name="Pursuit launch";
            }
            boatModel.MarkAsNoLongerNeeded();crewModel.MarkAsNoLongerNeeded();
        }

        private void Pursuit()
        {
            if(Game.GameTime<_nextPursuit||_blown)return;_nextPursuit=Game.GameTime+2000;
            var ice=Ctx.Crew.PedFor(CrewSlot.Ice);if(ice==null||!ice.Exists())return;
            for(int i=0;i<_pursuitBoats.Count;i++)
            {
                var boat=_pursuitBoats[i];var driver=_drivers[i];var gunner=_gunners[i];
                if(boat==null||!boat.Exists()||boat.IsDead)continue;
                var destination=ice.IsInVehicle(_kayak)?_kayak.Position:_launch;
                if(driver.Exists()&&!driver.IsDead)driver.Task.StartBoatMission(boat,destination,VehicleMissionType.GoTo,22f,(VehicleDrivingFlags)786603,18f,(BoatMissionFlags)7);
                if(gunner.Exists()&&!gunner.IsDead)gunner.Task.VehicleShootAtPed(ice);
            }
        }

        private void ShowAlarm()
        {
            _alarmShown = true;
            string line = "Harbor alarm's warming up: a launch is turning into the basin. That's your reason to leave. Charges stay dark until you're aboard.";
            if (_alarmCrew != null && _alarmCrew.Exists() && _alarmBoat != null && _alarmBoat.Exists())
                Ctx.Cutscenes.PlayMoment(Id, "The alarm", "GOHAN", line, _alarmCrew);
            else Radio("GOHAN", line, "M13_RADIO_01_GOHAN");
        }

        /// <summary>
        /// The payoff. Explosions at each barge in sequence rather than at once —
        /// simultaneous blasts read as one bug rather than three charges.
        /// </summary>
        private void Detonate()
        {
            _blown = true;
            foreach(var boat in _fuelBoats) if(boat!=null&&boat.Exists()) { boat.IsPositionFrozen=false; GTA.Native.Function.Call(GTA.Native.Hash.SET_BOAT_ANCHOR,boat,false); }
            for (int i = 0; i < _barges.Count; i++)
            {
                World.AddExplosion(_fuelBoats[i].Position, ExplosionType.Tanker, 12f, 1.6f,
                    Game.Player.Character, true, false);
                Script.Wait(450);
            }
        }

        /// <summary>The result, seen from the slipway: the basin burning, Ice's own line over it.</summary>
        private void PlayResult()
        {
            var blocking = new SceneBlocking()
                .Then(ShotStep.Wide(4200, _barges[1] + new Vector3(0f, 0f, 2f), 50f, 20f, 14f));
            if (_granger != null && _granger.Exists()) blocking.Then(new ShotStep(3000, _granger, new Vector3(-5f, 2.5f, 1.6f), null, _barges[1] + new Vector3(0f, 0f, 4f), 0.5f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "result", Title = "The basin burns",
                Reason = "Three barges burning at their moorings, seen from the slipway with the crew aboard the Granger. The harbor has fewer boats for the lift; it still has some.",
                Blocking = blocking
            };
            var cue = Ctx.Data?.Cue("M13_S1_03_ICE");
            if (!Ctx.Cutscenes.PlayStaged(spec, new[] { cue })) { Logger.Warn("M13 result scene did not play; the line plays as dialogue."); blocking.Complete(); Say("M13_S1_03_ICE"); }
        }

        /// <summary>The aftermath: the Granger pulling off the slipway, the glow behind it.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_granger == null || !_granger.Exists()) return null;
            return new SceneBlocking().Then(new ShotStep(4500, _granger, new Vector3(-7f, 3f, 2f), _granger, new Vector3(0f, 0f, 0.8f), 1.2f));
        }

        // ---------- world building ----------

        private bool SpawnFuelBoats()
        {
            var model=new Model("tug");
            if (!GameUtils.RequestModel(model)) return false;
            try
            {
                var keys=new[]{"M13.BargeOne","M13.BargeTwo","M13.BargeThree"};
                var size=model.Dimensions;
                float halfWidth=System.Math.Max(System.Math.Abs(size.Item1.X),System.Math.Abs(size.Item2.X));
                float halfLength=System.Math.Max(System.Math.Abs(size.Item1.Y),System.Math.Abs(size.Item2.Y));
                Logger.Info("M13 tug bounds: min=" + size.Item1 + ", max=" + size.Item2);
                var sites = new Vector3[keys.Length];
                for (int i = 0; i < keys.Length; i++) sites[i] = Ctx.Locations.Position(keys[i]);
                var originals = (Vector3[])sites.Clone();
                // Resolve before spawning: no half-built scene, and each hull has a
                // reserved footprint even if an estimate needs a small adjustment.
                float separation = 2f * (float)System.Math.Sqrt(
                    (halfWidth + 2f) * (halfWidth + 2f) + (halfLength + 2f) * (halfLength + 2f)) + 4f;
                try
                {
                    for (int i = 0; i < keys.Length; i++)
                    {
                        int index = i;
                        sites[i] = MarineSites.ResolveOrThrow(Ctx.Locations, keys[i], 5f,
                            halfWidth + 2f, halfLength + 2f, 30f, 10f, candidate =>
                            {
                                for (int other = 0; other < sites.Length; other++)
                                    if (other != index && GameUtils.IsWithinFlat(candidate, sites[other], separation)) return false;
                                return true;
                            });
                    }
                }
                catch
                {
                    // A later failed site must not ratchet an earlier estimate
                    // another thirty metres away on every startup retry.
                    for (int i = 0; i < keys.Length; i++) Ctx.Locations.Get(keys[i]).Position = originals[i];
                    throw;
                }
                for(int i=0;i<keys.Length;i++)
                {
                    var site=sites[i];
                    var boat=Track(World.CreateVehicle(model,site,Ctx.Locations.Heading(keys[i])));
                    if (!RequireAssets(boat)) return false;
                    boat.IsPersistent=true;boat.IsEngineRunning=false;boat.IsPositionFrozen=false;
                    // Let buoyancy set the waterline. A frozen model origin is not its draft.
                    GTA.Native.Function.Call(GTA.Native.Hash.SET_BOAT_ANCHOR,boat,true);
                    _fuelBoats.Add(boat);
                    // Work beside the hull, where the scooter can actually reach.
                    _barges[i]=site+new Vector3(boat.ForwardVector.Y,-boat.ForwardVector.X,0f)*(halfWidth+2f);
                    var blip=Track(boat.AddBlip());blip.Sprite=BlipSprite.Boat;blip.Color=BlipColor.Yellow;blip.Name="Fuel boat "+(i+1);
                }
                return true;
            }
            finally { model.MarkAsNoLongerNeeded(); }
        }

        private void SpawnKayak()
        {
            var model = new Model("seashark");
            if (!GameUtils.RequestModel(model)) return;

            _kayak = Track(World.CreateVehicle(model, _launch, Ctx.Locations.Heading("M13.KayakLaunch")));
            model.MarkAsNoLongerNeeded();
            if (_kayak == null || !_kayak.Exists()) return;

            _kayak.IsPersistent = true;

            var blip = Track(_kayak.AddBlip());
            blip.Sprite = BlipSprite.Boat;
            blip.Color = BlipColor.Green;
            blip.Name = "Water scooter";
        }

        private void SpawnGranger()
        {
            var spot = _slipway; float heading = Ctx.Locations.Heading("M13.CanalSlipway");
            Vehicle granger = Ctx.Vans != null ? Ctx.Vans.Spawn(spot, heading) : null;
            if (granger == null)
            {
                var model = new Model("granger");
                if (!GameUtils.RequestModel(model)) return;
                granger = World.CreateVehicle(model, spot, heading);
                model.MarkAsNoLongerNeeded();
            }
            _granger = Track(granger);
            if (_granger == null || !_granger.Exists()) return;

            _granger.IsPersistent = true;
            _granger.AreLightsOn = false;

            var blip = Track(_granger.AddBlip());
            blip.Sprite = BlipSprite.PersonalVehicleCar;
            blip.Color = BlipColor.Orange;
            blip.Name = "Guess";
        }

        private void SpawnWatchmen()
        {
            var model = new Model("s_m_m_dockwork_01");
            if (!GameUtils.RequestModel(model)) return;

            // Dock workers, not Aegis: they can raise an alarm, they are not a fight.
            var workers = World.AddRelationshipGroup("BLOODLINES_TRAFFIC");

            for (int i = 0; i < _barges.Count; i++)
            {
                var watchman = World.CreatePed(model, Ctx.Locations.Position("M13.Watchman" + (i + 1)), Ctx.Locations.Heading("M13.Watchman" + (i + 1)));
                if (watchman == null || !watchman.Exists()) continue;

                watchman.RelationshipGroup = workers;
                watchman.IsPersistent = true;
                watchman.BlockPermanentEvents = true;
                watchman.Accuracy = 20;
                watchman.Weapons.Give(WeaponHash.Pistol, 40, true, true);
                watchman.Task.StartScenario("WORLD_HUMAN_GUARD_STAND", watchman.Position, watchman.Heading);

                _watchmen.Add(Track(watchman));
            }

            model.MarkAsNoLongerNeeded();
        }

        protected override void OnPassed()
        {
            if (_granger != null && _granger.Exists()) Release(_granger);
        }

        protected override void OnUpdate()
        {
            if (!_blown && _fuelBoats.Exists(v=>v==null||!v.Exists()||v.IsDead))
            { Fail("A fuel boat was destroyed before the charges were ready. Restart the mission.");return; }
            if(!Ctx.Cutscenes.IsActive)Pursuit();
            base.OnUpdate();
        }

        protected override void OnCleanup()
        {
            _watchmen.Clear();
        }
    }
}
