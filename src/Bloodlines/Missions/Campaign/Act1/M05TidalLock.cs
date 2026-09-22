using System.Collections.Generic;
using System;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M05 — "Tidal Lock". Palomino Highlands shoreline, 03:30, storm.
    ///
    /// The Act I finale and the mission that turns the campaign: Mateo, run down
    /// on the water and taken aboard the dinghy, says the three contracts were
    /// bought to put the three of them on that dock. It is an allegation, and the
    /// mission records it as one; the proof is M46's.
    ///
    /// Seen, not told: the cove from the cliff and from the boat before anyone
    /// moves; Ice coming down to the water once Mateo is stopped, so the three
    /// voices of the questioning are three men in one place; Mateo pulled aboard
    /// and questioned in the dinghy; the crew leaving with him in the boat.
    ///
    /// Faked per docs/FEASIBILITY.md: there is no sea-cave interior, so the grotto is
    /// the water at the cave mouth, and the floodlights are the men working them —
    /// killing the generator crew reads the same as shooting out the lamps and does
    /// not need a destructible prop the engine will not give us.
    /// </summary>
    public sealed class M05TidalLock : ComposedMission
    {
        private readonly List<Ped> _lightCrew = new List<Ped>();

        private Ped _mateo;
        private Vehicle _mateoBoat;
        private Vehicle _dinghy;
        private RoleTracks _roles;
        private Vector3 _perch;
        private Vector3 _grotto;
        private Vector3 _sandbar;
        private Vector3 _shore;
        private bool _iceDown;
        private Vector3[] _chaseRoute;
        private int _leg, _chaseStarted=-1, _nextBoatOrders, _lastProgress, _weatherAt;
        private Vector3 _lastBoatPosition;
        private bool _weatherOwned;
        private BoatDisableObjective _boatDisable;
        public bool BoatDisabled => _boatDisable != null && _boatDisable.Disabled;
        public float DisableProgress => _boatDisable?.Progress ?? 0f;
        private float _previousSwell;
        private Weather _previousWeather;
        public IReadOnlyList<Vector3> ChaseRoute => _chaseRoute;
        public int ChaseLeg => _leg;
        private readonly List<string> _unverifiedLocations = new List<string>();
        private readonly Dictionary<MissionLocation, Vector3> _originalLocations = new Dictionary<MissionLocation, Vector3>();
        private bool _locationNoticeShown;
        public IReadOnlyList<string> UnverifiedLocations => _unverifiedLocations;
        /// <summary>While Gohan takes Mateo aboard, Guess keeps the dinghy beside Mateo's boat.</summary>
        private bool _holdingAlongside, _dinghyAnchored, _mateoAnchored;
        private int _nextAlongsideOrder;
        /// <summary>How close Guess brings the dinghy before he cuts the engine and drops anchor.</summary>
        public const float AlongsideMeters = 12f;
        public bool HoldingAlongside => _holdingAlongside;
        public bool DinghyAnchored => _dinghyAnchored;

        public override string Id => "M05";
        public override string Title => "Tidal Lock";

        public Ped Mateo => _mateo;
        public Vehicle Dinghy => _dinghy;
        public RoleTracks Roles => _roles;
        public bool IceDown => _iceDown;

        protected override bool Setup()
        {
            _unverifiedLocations.Clear();
            _locationNoticeShown = false;
            foreach (string key in new[] { "M05.CliffPerch", "M05.LightCrew", "M05.GrottoMouth", "M05.Sandbar", "M05.DinghySpawn" })
            {
                var location = Ctx.Locations.Get(key);
                if (location == null) { GameUtils.Notify("M05 cannot start: missing location " + key); return false; }
                _originalLocations[location] = location.Position;
                if (!PrepareLocation(location)) return false;
            }
            _perch = Ctx.Locations.Position("M05.CliffPerch");
            _grotto = Ctx.Locations.Position("M05.GrottoMouth");
            _sandbar = Ctx.Locations.Position("M05.Sandbar");
            _chaseRoute = new[]{
                MissionPlacement.Position(Ctx.Locations,"M05.Chase1",new Vector3(2600,-1300,0)),
                MissionPlacement.Position(Ctx.Locations,"M05.Chase2",new Vector3(2700,-1300,0)),
                MissionPlacement.Position(Ctx.Locations,"M05.Chase3",new Vector3(2750,-1200,0)),
                MissionPlacement.Position(Ctx.Locations,"M05.Chase4",new Vector3(3000,-1200,0)),
                MissionPlacement.Position(Ctx.Locations,"M05.Chase5",new Vector3(3250,-1600,0)),
                MissionPlacement.Position(Ctx.Locations,"M05.Chase6",new Vector3(3250,-1050,0)),
                MissionPlacement.Position(Ctx.Locations,"M05.Chase7",new Vector3(2750,-1150,0)),
                _sandbar};
            // Where Ice comes down to: the nearest ground the navmesh accepts on the
            // cliff side of the sandbar, or the perch itself if the shore refuses.
            _shore = World.GetSafeCoordForPed(_grotto + new Vector3(-20f, 20f, 2f), false, 0);
            if (_shore == Vector3.Zero) _shore = _perch;

            if (!Ctx.Crew.Deploy(CrewSlot.Ice, _perch, Ctx.Locations.Heading("M05.CliffPerch")))
            { GameUtils.Notify("M05 cannot start: the playable crew could not load."); return false; }

            _previousWeather=World.Weather;
            ApplyBibleSetting();
            _previousSwell=Function.Call<float>(Hash.GET_DEEP_OCEAN_SCALER);_weatherOwned=true;
            GameUtils.SetWeather("Thunderstorm / heavy rain");
            Function.Call(Hash.SET_DEEP_OCEAN_SCALER,1.65f);
            Game.Player.Character.Weapons.Give(WeaponHash.SniperRifle, 60, true, true);

            SpawnLightCrew();
            SpawnMateo();
            SpawnDinghy();
            if (_dinghy == null || !_dinghy.Exists() || _mateo == null || !_mateo.Exists() || _mateoBoat == null || !_mateoBoat.Exists() || _lightCrew.Count != MissionPlacement.Count(Ctx.Locations, "M05.LightCrew", 4))
            { Logger.Error("M05 startup assets: dinghy=" + (_dinghy != null && _dinghy.Exists()) + ", Mateo=" + (_mateo != null && _mateo.Exists()) + ", target boat=" + (_mateoBoat != null && _mateoBoat.Exists()) + ", guards=" + _lightCrew.Count);
              GameUtils.Notify("M05 cannot start: an essential boat or actor could not load. See Bloodlines.log."); return false; }
            foreach (var hero in Protagonist.All) Ctx.Crew.CompanionAI.TakeControl(hero.Slot);
            // Ron and Gohan in the dinghy, checked: a seat that did not take left Ron
            // standing by the cliff (Ron, September 11).
            Board(CrewSlot.Guess, VehicleSeat.Driver);
            Board(CrewSlot.Gohan, VehicleSeat.RightFront);
            Function.Call(Hash.REQUEST_WEAPON_ASSET, (uint)WeaponHash.FlareGun, 31, 0);
            _mateo.IsInvincible = true;
            _roles = new RoleTracks(Ctx.Crew, () => _lightCrew);
            PlayShore();
            return true;
        }

        private bool PrepareLocation(MissionLocation location)
        {
            bool water = location.Kind == "water";
            bool verified = water ? MissionSites.Water(Ctx.Locations, location.Key) : MissionSites.Ground(Ctx.Locations, location.Key);
            // A surveyed point the engine will not confirm is kept rather than refused, so
            // the mission loads; the diagnostic still has to name it.
            if (verified && !water && MissionSites.WasKeptOnTrust(location.Key))
            {
                Logger.Warn("M05 location kept on trust: " + location.Key + " at " + location.Position +
                            "; the engine offered no walkable coordinate near it.");
                if (!_unverifiedLocations.Contains(location.Key)) _unverifiedLocations.Add(location.Key);
                return true;
            }
            if (verified)
            {
                Logger.Info("M05 location verified: " + location.Key + " at " + location.Position + "; configured " + _originalLocations[location]);
                return true;
            }
            if (Ctx.Config == null || !Ctx.Config.M05LocationTestMode) return false;

            // Keep the configured X/Y so Ron can inspect and resurvey the problem.
            // Use a measured water height when available; otherwise retain authored Z.
            var point = _originalLocations[location];
            if (water)
            {
                var height = new OutputArgument();
                if (Function.Call<bool>(Hash.GET_WATER_HEIGHT, point.X, point.Y, 100f, height))
                {
                    float surface = height.GetResult<float>();
                    if (!float.IsNaN(surface) && !float.IsInfinity(surface)) point.Z = surface + .2f;
                }
            }
            location.Position = point;
            _unverifiedLocations.Add(location.Key);
            var blip = Track(World.CreateBlip(point));
            blip.Color = BlipColor.Orange;
            blip.Name = "TEST: " + location.Key;
            Logger.Warn("M05 LOCATION TEST: " + location.Key + " failed " + (water ? "water/depth" : "walkable ground") +
                " validation; continuing at " + point + "; configured " + _originalLocations[location] + ". Survey this key; no file was overwritten.");
            return true;
        }

        private void ShowLocationNotice()
        {
            if (_locationNoticeShown || Ctx.Cutscenes.IsActive) return;
            _locationNoticeShown = true;
            if (_unverifiedLocations.Count > 0)
                GameUtils.Notify("~y~M05 LOCATION TEST: unverified spots have orange TEST map markers. " +
                    string.Join(", ", _unverifiedLocations) + ". Record corrections with the surveyor; abort/retry if needed.");
        }

        private void Board(CrewSlot slot, VehicleSeat seat)
        {
            var ped = Ctx.Crew.PedFor(slot);
            if (ped == null || !ped.Exists() || _dinghy == null || !_dinghy.Exists()) return;
            ped.Task.ClearAllImmediately();
            ped.SetIntoVehicle(_dinghy, seat);
            if (ped.IsInVehicle(_dinghy)) return;
            ped.Task.WarpIntoVehicle(_dinghy, seat);
            Logger.Warn("M05: " + Protagonist.Of(slot).Handle + " did not take the dinghy's " + seat + " seat on the first try; warped in." + (ped.IsInVehicle(_dinghy) ? "" : " Still not aboard."));
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Cliff overwatch",
                    new KillTargetsObjective("Ice — take the generator crew off the cave mouth.",
                        () => _lightCrew))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(context => { Say("M05_S1_01_ICE"); ExplainThermal(); });

            yield return new MissionStage("Light the cove",
                    new BoatSignalFlare(() => _dinghy))
                .OwnedBy(CrewSlot.Guess)
                // Keep the remote driver out of generic follow/leash recovery while
                // Ice is still on the cliff, including on a full mission retry.
                .OnEnter(context => context.Crew.CompanionAI.TakeControl(CrewSlot.Guess))
                .OnExit(context =>
                {
                    Say("M05_S1_02_GUESS");
                    GameUtils.Subtitle("~y~Flare away. Approach Mateo's boat; he will run when we get close.", 4000);
                });

            yield return new MissionStage("Breach the grotto",
                    new OccupiedVehicleDestination("Approach Mateo's boat in the dinghy; close within 85m to flush him out.", () => _dinghy, () => _mateoBoat.Position, 85f))
                .PlayedBy(CrewSlot.Guess)
                .OnExit(context =>
                {
                    Say("M05_S1_03_GOHAN");
                    StartChase();
                });

            _boatDisable = new BoatDisableObjective(() => _mateo, () => _mateoBoat, () => _dinghy);
            yield return new MissionStage("Disable Mateo's boat", _boatDisable)
                .PlayedBy(CrewSlot.Guess)
                .OnExit(context => MateoStopped());

            // The questioning happens in the dinghy, not shouted across water: bring
            // it alongside and take him aboard. That is the stage's whole objective.
            yield return new MissionStage("Take him aboard",
                    new MissionInteraction("Gohan: take Mateo aboard the stopped dinghy. Press E / D-pad Right", () => MateoPosition(), 2, 20f, () => _dinghy, stopVehicle: true))
                .OwnedBy(CrewSlot.Gohan)
                // Ron, September 22: a boat will not sit still in that sea, and the moment he
                // switched to Gohan nobody was steering it. Mateo's boat is anchored, and
                // Guess brings the dinghy alongside and anchors it while Gohan does the work.
                .OnEnter(context => BeginAlongside())
                .OnExit(context => { _holdingAlongside = false; PlayAccount(); });

            yield return new MissionStage("Mateo's account", new DialogueFinishedObjective("Hold the dinghy. Mateo is aboard."))
                .AnyBrother()
                .OnExit(context =>
                {
                    // Act I ends on information, not a kill: an allegation the crew
                    // now holds and cannot yet prove. The proof is M46's.
                    Ctx.State?.SetEvidence("mateoAllegation", EvidenceState.Alleged);
                    if (_mateo != null && _mateo.Exists() && _dinghy != null && _dinghy.Exists() && !_mateo.IsInVehicle(_dinghy)) _mateo.SetIntoVehicle(_dinghy, VehicleSeat.LeftRear);
                    /* Awarded once by CampaignState.MarkComplete after the mission passes. */
                    GameUtils.Subtitle("~y~Aegis built this. All of it. Mateo's word is a lead, not proof.", 6000);
                    // The boats are his again to drive away in.
                    WeighAnchors();
                });
        }

        // ---------- beats ----------

        /// <summary>
        /// Ron, September 22: say what Ice's ability does here and how to use it. Thermal
        /// Pulse is the see-through sight, and a storm at 03:30 over a beach under a cliff is
        /// exactly where a marksman needs it.
        /// </summary>
        private void ExplainThermal()
        {
            foreach (string line in ThermalBriefing(Ctx.Config)) GameUtils.Notify(line);
        }

        /// <summary>What the player is told about Thermal Pulse on the cliff, with his own key in it.</summary>
        public static string[] ThermalBriefing(ModConfig config)
        {
            string key = config != null ? KeyName(config.AbilityKey.ToString()) : "Caps Lock";
            float seconds = config != null ? config.AbilityDuration : 8f;
            return new[]
            {
                "~b~Ice's ability: Thermal Pulse.~s~ Heat vision through the rain and the dark, with a red marker over every armed man in range - the whole generator crew from this cliff.",
                "~b~To use it:~s~ press " + key + ", or click both sticks (L3 + R3) on a controller. It runs about " +
                    seconds.ToString("0") + " seconds, then refills; press again to turn it off early. Scope in with the rifle while it is on.",
            };
        }

        /// <summary>A key as it is printed on the keyboard rather than as the enum spells it.</summary>
        public static string KeyName(string key)
        {
            switch (key ?? "")
            {
                case "Capital": case "CapsLock": return "Caps Lock";
                case "Oemtilde": case "Oem3": return "the tilde key";
                case "Menu": case "LMenu": return "Alt";
                case "": return "Caps Lock";
                default: return key;
            }
        }

        private void BeginAlongside()
        {
            _holdingAlongside = true;
            _nextAlongsideOrder = 0;
            SetAnchor(_mateoBoat, true, ref _mateoAnchored);
        }

        /// <summary>
        /// Every frame while Mateo is being taken aboard: when the player is not at the wheel,
        /// Guess closes to <see cref="AlongsideMeters"/> and anchors. A route to a boat that is
        /// already anchored does not move, so he is given it again only on a slow refresh,
        /// never every frame - that restarts the drive before he can make any way.
        /// </summary>
        private void KeepAlongside()
        {
            if (!_holdingAlongside || _dinghy == null || !_dinghy.Exists() || _mateoBoat == null || !_mateoBoat.Exists()) return;
            var player = Game.Player.Character;
            var driver = _dinghy.GetPedOnSeat(VehicleSeat.Driver);
            if (driver == null || !driver.Exists() || driver.IsDead) return;
            // The player at the wheel steers it himself, and an anchor would fight him.
            if (player != null && driver.Handle == player.Handle) { SetAnchor(_dinghy, false, ref _dinghyAnchored); return; }
            if (GameUtils.IsWithinFlat(_dinghy.Position, _mateoBoat.Position, AlongsideMeters))
            {
                if (_dinghyAnchored) return;
                driver.Task.ClearAll();
                Function.Call(Hash.SET_VEHICLE_FORWARD_SPEED, _dinghy, 0f);
                SetAnchor(_dinghy, true, ref _dinghyAnchored);
                Logger.Info("M05: Guess has the dinghy alongside Mateo's boat and anchored.");
                return;
            }
            SetAnchor(_dinghy, false, ref _dinghyAnchored);
            if (Game.GameTime < _nextAlongsideOrder) return;
            _nextAlongsideOrder = Game.GameTime + 6000;
            driver.Task.StartBoatMission(_dinghy, _mateoBoat.Position, VehicleMissionType.GoTo, 8f, (VehicleDrivingFlags)786603, AlongsideMeters - 4f, (BoatMissionFlags)7);
        }

        /// <summary>Drop or weigh a boat's anchor, remembering which so cleanup lifts only what this mission dropped.</summary>
        private static void SetAnchor(Vehicle boat, bool down, ref bool anchored)
        {
            if (boat == null || !boat.Exists() || anchored == down) return;
            try
            {
                if (down && !Function.Call<bool>(Hash.CAN_ANCHOR_BOAT_HERE, boat)) return;
                Function.Call(Hash.SET_BOAT_ANCHOR, boat, down);
                anchored = down;
            }
            catch (Exception ex) { Logger.Warn("M05 could not " + (down ? "anchor" : "weigh the anchor of") + " a boat: " + ex.Message); }
        }

        private void WeighAnchors()
        {
            _holdingAlongside = false;
            SetAnchor(_dinghy, false, ref _dinghyAnchored);
            SetAnchor(_mateoBoat, false, ref _mateoAnchored);
        }

        /// <summary>The cove before anyone moves: from the perch, from the dinghy, his boat at the cave mouth.</summary>
        private void PlayShore()
        {
            var blocking = new SceneBlocking()
                .Then(new ShotStep(3200, null, _perch + new Vector3(2f, 2f, 1.6f), null, _grotto + new Vector3(0f, 0f, 1f), 0f))
                .Then(new ShotStep(3200, _dinghy, new Vector3(-6f, 2.5f, 1.8f), _dinghy, new Vector3(0f, 0f, 0.8f), 1.0f))
                .Then(new ShotStep(3000, _mateoBoat, new Vector3(-10f, 4f, 2.5f), _mateoBoat, new Vector3(0f, 0f, 0.8f), 1.5f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "shore", Title = "The cove",
                Reason = "The recovered location is real: Mateo's boat at the cave mouth under the lamps, Ice above on the cliff, Ron and Gohan in the dinghy below.",
                Blocking = blocking
            }.With("MATEO", _mateo);
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M05 shore scene did not play; the overwatch stands on its own.");
        }

        /// <summary>Mateo is stopped on the water. Ice comes down off the cliff to the shore so the three of them are in one place for what he says.</summary>
        private void MateoStopped()
        {
            if (_mateo != null && _mateo.Exists()) _mateo.Task.ClearAll();
            if (_mateoBoat != null && _mateoBoat.Exists()) { _mateoBoat.IsEngineRunning = false; Function.Call(Hash.SET_VEHICLE_FORWARD_SPEED, _mateoBoat, 0f); }
            if (_dinghy != null && _dinghy.Exists()) Function.Call(Hash.SET_VEHICLE_FORWARD_SPEED, _dinghy, 0f);
            _iceDown = true;
            _roles.For(CrewSlot.Ice).Extract(_shore);
            Radio("ICE", "He's stopped. I'm coming down to the water. Keep him breathing until I'm there.", "M05_RADIO_01_ICE");
        }

        /// <summary>
        /// The questioning, staged from the bible's own stage-two lines: Mateo is
        /// pulled aboard the dinghy, and says it over Gohan's shoulder with Ice
        /// on the shore. Skipping still leaves him in the boat.
        /// </summary>
        private void PlayAccount()
        {
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            if (_mateo == null || !_mateo.Exists() || _dinghy == null || !_dinghy.Exists()) return;
            Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Guess);
            if (guess != null && guess.Exists()) guess.Task.ClearAll();
            _dinghy.IsEngineRunning = false;
            Function.Call(Hash.SET_VEHICLE_FORWARD_SPEED, _dinghy, 0f);
            var blocking = new SceneBlocking { DialogueAfterStep = 1 }
                .Then(new EnterVehicleStep(_mateo, _dinghy, VehicleSeat.LeftRear) { TimeoutMs = 9000 })
                .Then(gohan != null && gohan.Exists() ? ShotStep.OverShoulder(5000, gohan, _mateo, 0.3f) : (SceneStep)new ShotStep(5000, _dinghy, new Vector3(-3f, 2f, 1.6f), _mateo, new Vector3(0f, 0f, 0.7f)))
                .Then(new ShotStep(4500, _dinghy, new Vector3(-7f, 3f, 2f), _dinghy, new Vector3(0f, 0f, 0.9f), 1.2f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "account", Title = "Mateo's account",
                Reason = "Mateo, aboard the dinghy and alive, says the three contracts were bought; Gohan separates what he said from what the data confirms; Ice says what comes next.",
                Blocking = blocking
            }.With("MATEO", _mateo);
            if (!Ctx.Cutscenes.PlayStaged(spec, Ctx.Data?.Stage(Id, 2)))
            {
                Logger.Warn("M05 account scene did not play; the lines play as dialogue and Mateo is put aboard.");
                blocking.Complete();
                SayStage(2);
            }
        }

        protected override void OnUpdate()
        {

            if(_weatherOwned&&Game.GameTime>=_weatherAt){Function.Call(Hash.SET_DEEP_OCEAN_SCALER,1.65f);_weatherAt=Game.GameTime+2000;}
            if(!Ctx.Cutscenes.IsActive) { MaintainChase(); KeepAlongside(); }
            base.OnUpdate();
            _roles?.Update();
            ShowLocationNotice();
        }
        private void StartChase()
        {
            _leg=0;_chaseStarted=Game.GameTime;_lastProgress=Game.GameTime;_lastBoatPosition=_mateoBoat.Position;
            _mateoBoat.IsEngineRunning=true;_dinghy.IsEngineRunning=true;
            _nextBoatOrders=0;OrderMateo();
            Radio("GOHAN","He's running. Guess, keep us within 45 meters. I'll cut his engine remotely. Keep driving until he's disabled, then stop alongside him.","M05_CHASE_START");
        }
        private void OrderMateo()
        {
            if(_leg>=_chaseRoute.Length)return;
            float gap=_dinghy.Position.DistanceTo(_mateoBoat.Position);
            float speed=gap>150f?15f:24f;
            _mateo.Task.StartBoatMission(_mateoBoat,_chaseRoute[_leg],VehicleMissionType.GoTo,speed,(VehicleDrivingFlags)786603,18f,(BoatMissionFlags)7);
            _nextBoatOrders=Game.GameTime+6000;
        }
        private void MaintainChase()
        {
            if(CurrentStage!=3||_chaseStarted<0||_mateoBoat==null||!_mateoBoat.Exists())return;
            if (BoatDisabled) return;
            if(_leg<_chaseRoute.Length&&GameUtils.IsWithinFlat(_mateoBoat.Position,_chaseRoute[_leg],20f))
            {_leg++;_nextBoatOrders=0;_lastProgress=Game.GameTime;}
            if(_mateoBoat.Position.DistanceTo(_lastBoatPosition)>8f){_lastProgress=Game.GameTime;_lastBoatPosition=_mateoBoat.Position;}
            // Hack progress, not finishing a route or a fixed one-minute wait,
            // determines the capture. Keep an escape route if the hack is delayed.
            if(_leg>=_chaseRoute.Length) { _leg=0; _nextBoatOrders=0; }
            if(Game.GameTime>=_nextBoatOrders)OrderMateo();
            if(Game.GameTime-_lastProgress>30000)
            {Fail("Mateo's boat cannot follow this water route. Survey the next M05.Chase point and retry.");}
        }
        /// <summary>The aftermath: the dinghy with the four of them in it, leaving. Custody is seen.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_dinghy == null || !_dinghy.Exists()) return null;
            return new SceneBlocking().Then(new ShotStep(4500, _dinghy, new Vector3(-8f, 3f, 2.2f), _dinghy, new Vector3(0f, 0f, 0.8f), 1.5f));
        }

        private Vector3 MateoPosition()
        {
            return _mateo != null && _mateo.Exists() ? _mateo.Position : _sandbar;
        }

        /// <summary>
        /// The generator crew works the lamps on the beach below the cliff: four
        /// posts spread across Ron's surveyed beach key (September 12: the crew had
        /// been standing inside the mountain), each snapped to walkable ground.
        /// </summary>
        private Vector3 LightCrewPost(int index)
        {
            var beach = Ctx.Locations.Position("M05.LightCrew");
            var formation = Ctx.Locations.Get("M05.LightCrew");
            if (MissionPlacement.HasFormation(formation))
            {
                var point=MissionPlacement.GroupPoint(formation,index);
                var ground=World.GetSafeCoordForPed(point,false,0);
                return ground!=Vector3.Zero&&GameUtils.IsWithinFlat(ground,point,5f)?ground:point;
            }
            var wanted = beach + new Vector3(index * 3.5f - 5.25f, (index % 2) * 2.5f, 0f);
            var safe = World.GetSafeCoordForPed(wanted, false, 0);
            if (safe != Vector3.Zero && GameUtils.IsWithinFlat(safe, beach, 20f)) return safe;
            return wanted;
        }

        private void SpawnLightCrew()
        {
            var model = new Model("g_m_y_mexgoon_02");
            if (!GameUtils.RequestModel(model)) return;

            var cartel = World.AddRelationshipGroup("BLOODLINES_CARTEL");

            for (int i = 0; i < MissionPlacement.Count(Ctx.Locations, "M05.LightCrew", 4); i++)
            {
                var post = LightCrewPost(i);
                var guard = World.CreatePed(model, post, DriveUpStep.HeadingBetween(post, _grotto));
                if (guard == null || !guard.Exists()) continue;

                guard.RelationshipGroup = cartel;
                guard.IsPersistent = true;
                guard.BlockPermanentEvents = true;
                guard.Accuracy = 30;
                guard.Weapons.Give(WeaponHash.MicroSMG, 150, true, true);
                guard.Task.GuardCurrentPosition();

                _lightCrew.Add(Track(guard));
            }
            if (_lightCrew.Count > 0) Logger.Info("M05 generator crew at the cave mouth: first post " + _lightCrew[0].Position + " (grotto " + _grotto + ").");

            model.MarkAsNoLongerNeeded();
        }

        private void SpawnMateo()
        {
            var model = new Model("g_m_m_mexboss_01");
            var boatModel = new Model("tropic");
            if (!GameUtils.RequestModel(model) || !GameUtils.RequestModel(boatModel)) return;

            // His boat sits at the key itself: Ron's surveyed spot off the beach, where the two boats moor.
            float boatHeading = Ctx.Locations.Heading("M05.GrottoMouth");
            _mateoBoat = Track(World.CreateVehicle(boatModel, _grotto, boatHeading));
            _mateo = Track(World.CreatePed(model, _grotto + new Vector3(2f, 0f, 0f), boatHeading));
            model.MarkAsNoLongerNeeded();
            boatModel.MarkAsNoLongerNeeded();

            if (_mateo == null || !_mateo.Exists()) return;

            _mateo.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_TRAFFIC");
            _mateo.IsPersistent = true;
            _mateo.BlockPermanentEvents = true;
            _mateo.Armor = 100;
            _mateo.Weapons.Give(WeaponHash.APPistol, 100, true, true);

            var blip = Track(_mateo.AddBlip());
            blip.Sprite = BlipSprite.Enemy;
            blip.Color = BlipColor.Red;
            blip.Name = "Mateo Cifuentes";

            if (_mateoBoat != null && _mateoBoat.Exists())
            {
                _mateoBoat.IsPersistent = true;
                _mateo.Task.WarpIntoVehicle(_mateoBoat, VehicleSeat.Driver);
            }
        }

        private void SpawnDinghy()
        {
            var model = new Model("dinghy");
            if (!GameUtils.RequestModel(model)) return;

            _dinghy = Track(World.CreateVehicle(model, Ctx.Locations.Position("M05.DinghySpawn"), Ctx.Locations.Heading("M05.DinghySpawn")));
            model.MarkAsNoLongerNeeded();
            if (_dinghy == null || !_dinghy.Exists()) return;

            _dinghy.IsPersistent = true;
            Logger.Info("M05 dinghy on the water at " + _dinghy.Position + ".");

            var blip = Track(_dinghy.AddBlip());
            blip.Sprite = BlipSprite.Boat;
            blip.Color = BlipColor.Green;
            blip.Name = "Stealth dinghy";
        }

        protected override void OnCleanup()
        {
            if(_weatherOwned){Function.Call(Hash.SET_DEEP_OCEAN_SCALER,_previousSwell);World.Weather=_previousWeather;_weatherOwned=false;}
            WeighAnchors();
            foreach (var pair in _originalLocations) pair.Key.Position = pair.Value;
            _originalLocations.Clear();
            _roles?.Release();
            if (_mateo != null && _mateo.Exists()) { _mateo.IsInvincible=false; Release(_mateo); }
            if (_mateoBoat != null && _mateoBoat.Exists()) Release(_mateoBoat);
            Function.Call(Hash.REMOVE_WEAPON_ASSET,(uint)WeaponHash.FlareGun);
            _lightCrew.Clear();
        }
    }
}
