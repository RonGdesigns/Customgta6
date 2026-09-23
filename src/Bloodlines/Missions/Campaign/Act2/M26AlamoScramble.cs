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
    /// M26 — "The Alamo Scramble". Alamo airspace, 11:00, high wind.
    ///
    /// Cartel spotter planes are quartering the Alamo looking for the sunken bullion.
    /// Guess takes up an armed Lazer interceptor and puts them both in the lake
    /// while Gohan jams their radio calls.
    ///
    /// Seen, not told: the spotter pattern over the lake, the Lazer parked at
    /// McKenzie with its history (towed out of Zancudo under the M16 clearance,
    /// fueled here), Gohan at the laptop on the Granger's hood with the transmission
    /// problem, Ice at the hangar door with the radio; the intercepted traffic as a
    /// reason to hold fire on the second spotter until Gohan has the charter's call
    /// sign; the Lazer parked again at the end beside the aircraft the next job uses,
    /// so the change of aircraft is a thing on the apron and not a line.
    ///
    /// The campaign's only pure air-to-air mission, and it is here for variety as much
    /// as story: after a bunker assault, a dredge and a sniper hold, the fourth mission
    /// of the act should not be another set of men on foot.
    /// </summary>
    public sealed class M26AlamoScramble : ComposedMission
    {
        private readonly List<Vehicle> _spotters = new List<Vehicle>();
        private readonly List<Ped> _pilots = new List<Ped>();
        private readonly List<Blip> _spotterBlips = new List<Blip>();
        /// <summary>
        /// How many times the second spotter may be put back in the air after going down
        /// through no fault of the player's. Bounded, so an aircraft that keeps falling out
        /// of the sky for a reason nobody can see fails with a reason instead of littering
        /// the lake with wrecks.
        /// </summary>
        public const int SpotterRelaunches = 3;
        /// <summary>How far past the patrol center from Ron a replacement spotter comes in.</summary>
        public const float RelaunchStandoff = 300f;
        private int _relaunches;

        /// <summary>How long Gohan needs on the second spotter's traffic before it can go down.</summary>
        public const int ListenMs = 12000;
        /// <summary>
        /// How close Guess has to sit on the second spotter for Gohan to pull the call sign.
        ///
        /// Ron asked why he cannot simply shoot both of them out of the sky, and the answer
        /// is that the second one is transmitting the thing M27 is built on: the charter's
        /// call sign, which is how the crew knows which Shamal to intercept. Kill him early
        /// and that is gone. But "roll up and do nothing for twelve seconds" is not a beat,
        /// so the wait is flying rather than waiting — hold station inside this range and
        /// the clock runs; break off and it stops.
        /// </summary>
        public const float ListenRange = 320f;
        /// <summary>
        /// How wide the spotters quarter the lake, and how fast.
        ///
        /// I have now had this number wrong in both directions. Sixty meters was a
        /// continuous hard bank in one spot that a Lazer could not turn inside. Four
        /// hundred put them somewhere new before Ron was off the ground, so I cut it to
        /// two hundred and twenty to make them findable — and a circle that tight is a
        /// forty-nine degree sustained bank at cruise, which is how a crop duster spins
        /// into the lake. Ron watched one do it.
        ///
        /// The radius is set by what the aircraft can actually fly: at 380 m and 50 m/s a
        /// Mammatus holds about thirty-four degrees, which it will keep all day. Finding
        /// them is not this number's job — that belongs to the waypoint that tracks the
        /// target and to blips that survive the range, both of which exist now.
        /// </summary>
        public const float PatrolRadius = 380f;
        /// <summary>Patrol altitude. Low enough to see them against the lake.</summary>
        public const int PatrolHeight = 170;
        /// <summary>
        /// Cruise for the patrol. A Mammatus sits near 55; forty is close enough to its
        /// stall that a banked turn takes it, and stall speed climbs with bank angle.
        /// </summary>
        public const float PatrolSpeed = 50f;
        /// <summary>
        /// Airspeed they are created with. They are made at over two hundred meters and
        /// then told to descend to 170, and an aircraft handed to the AI already sinking
        /// does not always recover.
        /// </summary>
        public const float SpotterLaunchSpeed = 65f;

        private Vehicle _duster;
        private Vehicle _approachPlane;
        private Vehicle _granger;
        private Prop _laptop;
        private Vector3 _pad;
        private Vector3 _patrolBox;
        private int _listenUntil, _listenLeft = ListenMs, _lastListenTick;
        private bool _listening, _leadHeld, _parked;

        public override string Id => "M26";
        public override string Title => "The Alamo Scramble";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;

        public Vehicle Lazer => _duster;
        public Vehicle ApproachPlane => _approachPlane;
        public Vehicle Granger => _granger;
        public IReadOnlyList<Vehicle> Spotters => _spotters;
        /// <summary>The spotter pilots, so a test can prove they are actually flying their planes.</summary>
        public IReadOnlyList<Ped> Pilots => _pilots;
        public bool Listening => _listening;
        public bool LeadHeld => _leadHeld;
        public bool Parked => _parked;
        /// <summary>How many times the second spotter has been put back in the air.</summary>
        public int Relaunches => _relaunches;

        protected override bool Setup()
        {
            // Do not run aircraft keys through broad pedestrian ground snapping.
            _pad = Ctx.Locations.Position("M26.RunwayStart");
            _patrolBox = Ctx.Locations.Position("M26.PatrolBox");

            if (!Ctx.Crew.Deploy(CrewSlot.Guess, BoundedPlacement.Ped(Ctx.Locations, "M26.CrewStart"),
                    Ctx.Locations.Heading("M26.CrewStart")))
            {
                return false;
            }

            ApplyBibleSetting();
            SpawnDuster();
            SpawnApproachPlane();
            SpawnGranger();
            SpawnSpotters();
            if (!RequireAssets(_duster)) return false;
            if (_spotters.Count != 2 || _pilots.Count != 2) return false;
            Ctx.Crew.CompanionsHoldPosition = true;
            Station(CrewSlot.Ice, BoundedPlacement.Ped(Ctx.Locations, "M26.IcePost"));
            Station(CrewSlot.Gohan, BoundedPlacement.Ped(Ctx.Locations, "M26.GohanPost"));
            PlayApproach();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Scramble",
                    new EnterVehicleObjective("Guess — take off in the marked Lazer.", () => _duster,
                        VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess);

            yield return new MissionStage("First spotter",
                    new DestroyVehicleObjective("Splash the lead spotter.",
                        () => _spotters.Count > 0 ? _spotters[0] : null))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context => GameUtils.Subtitle("~y~Destroy the LEAD spotter first. Keep the second alive until Gohan clears you to fire.", 5000))
                .OnExit(context => HoldForTraffic())
                .WithCues("M26_S1_01_GUESS");

            // The second spotter is calling somebody: what he is calling is the lead.
            // Kill him too early and it goes into the lake with him.
            yield return new MissionStage("The charter",
                    // Losing the second spotter is watched in WatchSecondSpotter, every frame
                    // from the scramble on, rather than by a trigger that fired once.
                    new ConditionObjective("Guess: sit on the second spotter's wing while Gohan pulls the charter's call sign.", () => Listened())
                        { Marker = () => _spotters.Count > 1 && _spotters[1] != null && _spotters[1].Exists() ? _spotters[1].Position : _patrolBox, MarkerRadius = 12f })
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context => HoldTheLead());

            yield return new MissionStage("Second spotter",
                    new DestroyVehicleObjective("The second one is diving for Grapeseed — kill him.",
                        () => _spotters.Count > 1 ? _spotters[1] : null))
                .OwnedBy(CrewSlot.Guess)
                .WithCues("M26_S1_02_GOHAN");

            yield return new MissionStage("Home",
                    new DeliverVehicleObjective("Guess: land the Lazer at McKenzie and stop.", () => _duster, () => _pad, 12f, land: true))
                .OnExit(context => PlayPark());
        }

        // ---------- beats ----------

        /// <summary>The spotter pattern over the lake, the Lazer parked with its history, Gohan at the laptop, Ice at the hangar door: the job and the aircraft before the scramble.</summary>
        private void PlayApproach()
        {
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            var blocking = new SceneBlocking();
            if (_spotters.Count > 0 && _spotters[0].Exists()) blocking.Then(new ShotStep(3000, _spotters[0], new Vector3(-40f, 25f, 8f), _spotters[0], new Vector3(0f, 0f, 0f), 1.5f));
            else blocking.Then(ShotStep.Wide(3000, _patrolBox, 120f, 30f, 40f));
            if (_duster != null && _duster.Exists()) blocking.Then(new ShotStep(3200, _duster, new Vector3(-10f, 6f, 2.5f), _duster, new Vector3(2f, 0f, 0.6f), 0.9f));
            if (gohan != null && gohan.Exists() && _laptop != null && _laptop.Exists()) blocking.Then(new InspectStep(gohan, _laptop.Position + new Vector3(0f, -0.8f, -1f), 2600, "WORLD_HUMAN_CLIPBOARD"));
            if (ice != null && ice.Exists()) blocking.Then(ShotStep.Watching(2400, ice, ice));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "McKenzie",
                Reason = "Two spotters quartering the Alamo over the water where the container sits. The Lazer on the McKenzie apron: towed out of Zancudo under the M16 clearance, fueled and cold until he is in it, the aircraft Ron flies today. Gohan at the laptop on the Granger's hood with their traffic; Ice at the hangar door on the radio. No fourth pilot.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M26 approach scene did not play; the apron stands on its own.");
        }

        /// <summary>The first spotter down: the second is calling somebody, and that call is worth more than the kill for a moment.</summary>
        private void HoldForTraffic()
        {
            _listening = true;
            _listenLeft = ListenMs;
            _lastListenTick = Game.GameTime;
            _listenUntil = Game.GameTime + ListenMs;
            Radio("GOHAN", "Hold the second one. He's calling a fix to somebody with a tail number, and I'm halfway through it. Keep him in sight; don't splash him yet.", "M26_RADIO_01_GOHAN");
        }

        /// <summary>The lead: an executive charter, recorded as evidence the crew holds because they listened.</summary>
        private void HoldTheLead()
        {
            _leadHeld = true;
            Ctx.State?.SetEvidence("charterCallSign", EvidenceState.CopyHeld);
            Radio("GOHAN", "Got it. He's reporting to an Aegis charter: an executive Shamal on a call sign I've got on the board. That's the ledger we want. Now the second one.", "M26_RADIO_02_GOHAN");
            GameUtils.Subtitle("~g~Charter call sign held. The spotter can go down now.", 4000);
        }

        /// <summary>The Lazer parked and Ron out; the aircraft for the next job beside it. The change of aircraft is a thing on the apron.</summary>
        private void PlayPark()
        {
            _parked = true;
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            var blocking = new SceneBlocking();
            if (guess != null && guess.Exists() && _duster != null && _duster.Exists() && guess.IsInVehicle(_duster)) blocking.Then(new ExitVehicleStep(guess));
            if (_duster != null && _duster.Exists()) blocking.Then(new ShotStep(3000, _duster, new Vector3(-9f, 5f, 2.2f), _duster, new Vector3(2f, 0f, 0.6f), 0.8f));
            if (_approachPlane != null && _approachPlane.Exists()) blocking.Then(new ShotStep(3000, _approachPlane, new Vector3(-8f, 5f, 2f), _approachPlane, new Vector3(0f, 0f, 0.8f), 0.8f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "park", Title = "The apron",
                Reason = "The Lazer parked and shut down at McKenzie, Ron out of it. Beside it the Vestra: two seats and quicker than the Shamal, the aircraft for the jet job. The Lazer stays here; nobody flies a fighter onto a jet's wing.",
                Blocking = blocking
            };
            var cue = Ctx.Data?.Cue("M26_S1_03_GUESS");
            if (!Ctx.Cutscenes.PlayStaged(spec, new[] { cue })) { Logger.Warn("M26 park scene did not play; the line plays as dialogue."); blocking.Complete(); Say("M26_S1_03_GUESS"); }
            if (_duster != null && _duster.Exists()) _duster.IsEngineRunning = false;
            Ctx.State?.SetCargo("lazer", "M26.DusterPad");
            GameUtils.Subtitle("~g~Both spotters in the lake; the Alamo stash stays a ghost. The Lazer is parked at McKenzie; the jet job flies the Vestra.", 6000);
        }

        /// <summary>The aftermath: the two aircraft on the apron.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_duster == null || !_duster.Exists()) return null;
            return new SceneBlocking().Then(new ShotStep(4500, _duster, new Vector3(-14f, 9f, 3.5f), _duster, new Vector3(0f, 0f, 1f), 1.2f));
        }

        // ---------- world building ----------

        private void SpawnDuster()
        {
            var model = new Model("lazer");
            if (!GameUtils.RequestModel(model)) return;

            var spot = BoundedPlacement.Vehicle(Ctx.Locations, "M26.RunwayStart", model, departureMeters: 25f);
            _duster = Track(World.CreateVehicle(model, spot, Ctx.Locations.Heading("M26.RunwayStart")));
            model.MarkAsNoLongerNeeded();
            if (_duster == null || !_duster.Exists()) return;

            _duster.IsPersistent = true;
            // Parked cold on purpose, so the scramble is a scramble. KeepInterceptorReady
            // starts it the moment Ron is aboard: a jet left with its engine off lets the
            // player fire the guns and never accelerate, which is what Ron hit.
            _duster.IsEngineRunning = false;

            var blip = Track(_duster.AddBlip());
            blip.Sprite = BlipSprite.Plane;
            blip.Color = BlipColor.Orange;
            blip.Name = "Lazer interceptor";
        }

        /// <summary>
        /// The Vestra beside the Lazer: two seats and faster than the Shamal, the aircraft
        /// M27 flies. It was a Duster, and a Duster tops out at 69 against the Shamal's 91,
        /// so Ron could not catch the jet in M27 because nothing could.
        /// </summary>
        private void SpawnApproachPlane()
        {
            var model = new Model("vestra");
            if (!GameUtils.RequestModel(model)) return;
            var spot = BoundedPlacement.Vehicle(Ctx.Locations, "M26.SparePlane", model);
            _approachPlane = Track(World.CreateVehicle(model, spot, Ctx.Locations.Heading("M26.SparePlane")));
            model.MarkAsNoLongerNeeded();
            if (_approachPlane == null || !_approachPlane.Exists()) return;
            _approachPlane.IsPersistent = true;
            _approachPlane.IsEngineRunning = false;
        }

        /// <summary>The crew's Granger on the apron with Gohan's laptop on the hood: the transmission problem has a place.</summary>
        private void SpawnGranger()
        {
            var carModel = new Model("granger");
            if (!GameUtils.RequestModel(carModel)) return;
            var spot = BoundedPlacement.Vehicle(Ctx.Locations, "M26.CrewCar", carModel);
            carModel.MarkAsNoLongerNeeded();
            Vehicle granger = Ctx.Vans != null ? Ctx.Vans.Spawn(spot, Ctx.Locations.Heading("M26.CrewCar")) : null;
            if (granger == null)
            {
                var model = new Model("granger");
                if (!GameUtils.RequestModel(model)) return;
                granger = World.CreateVehicle(model, spot, Ctx.Locations.Heading("M26.CrewCar"));
                model.MarkAsNoLongerNeeded();
            }
            _granger = Track(granger);
            if (_granger == null || !_granger.Exists()) return;
            _granger.IsPersistent = true;
            _granger.IsEngineRunning = false;
            var laptop = new Model("prop_laptop_01a");
            if (!GameUtils.RequestModel(laptop)) return;
            _laptop = Track(World.CreateProp(laptop, _granger.Position + new Vector3(0f, 0f, 1.5f), false, false));
            laptop.MarkAsNoLongerNeeded();
            if (_laptop == null || !_laptop.Exists()) return;
            _laptop.IsPersistent = true;
            StowPropStep.Stow(_laptop, _granger, new Vector3(0f, 2.6f, 0.95f));
        }

        private void SpawnSpotters()
        {
            var planeModel = new Model("mammatus");
            var pilotModel = new Model("g_m_y_mexgoon_02");
            if (!GameUtils.RequestModel(planeModel) || !GameUtils.RequestModel(pilotModel)) return;

            for (int i = 0; i < 2; i++)
                SpawnSpotter(planeModel, pilotModel, _patrolBox + new Vector3(i * 120f - 60f, i * 80f, i * 40f), -1);

            planeModel.MarkAsNoLongerNeeded();
            pilotModel.MarkAsNoLongerNeeded();
        }

        /// <summary>
        /// One spotter and its pilot, airborne and flying the patrol. With a
        /// <paramref name="replace"/> index it takes that spotter's place in the lists
        /// rather than adding a new one, which is how the second spotter is put back.
        /// </summary>
        private bool SpawnSpotter(Model planeModel, Model pilotModel, Vector3 at, int replace)
        {
            var plane = Track(World.CreateVehicle(planeModel, at, 180f));
            if (plane == null || !plane.Exists()) return false;
            plane.IsPersistent = true;
            AircraftHold.LaunchAirborne(plane, SpotterLaunchSpeed);

            var pilot = Track(World.CreatePed(pilotModel, plane.Position, 0f));
            if (pilot == null || !pilot.Exists()) { GameUtils.SafeDelete(plane); return false; }

            pilot.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_CARTEL");
            pilot.IsPersistent = true;
            pilot.BlockPermanentEvents = true;
            // Seat him, do not ask him to board. WarpIntoVehicle is a queued task
            // and the plane mission below replaces whatever is queued, so the board
            // never happened: the pilot was left loose in the air at the patrol
            // altitude and both he and the unmanned plane fell out of the sky within
            // seconds. That is why these spotters were never there in Ron's run.
            pilot.SetIntoVehicle(plane, VehicleSeat.Driver);
            if (plane.GetPedOnSeat(VehicleSeat.Driver) != pilot)
            {
                // No seat, no spotter. Two falling bodies is worse than one absence.
                Logger.Error("M26: a spotter pilot could not be seated; removing that aircraft.");
                GameUtils.SafeDelete(pilot);
                GameUtils.SafeDelete(plane);
                return false;
            }
            // Quartering the lake, not hunting the player: they are looking for gold.
            pilot.Task.StartPlaneMission(plane, _patrolBox, VehicleMissionType.Circle,
                PatrolSpeed, PatrolRadius, PatrolHeight, 40, 0f, false);

            var blip = Track(plane.AddBlip());
            blip.Sprite = BlipSprite.Plane;
            blip.Color = BlipColor.Red;
            // The chase happens across the whole lake. A short-range blip drops off
            // the minimap at exactly the distance he needs it at.
            blip.IsShortRange = false;
            blip.Name = "Cartel spotter";

            if (replace >= 0 && replace < _spotters.Count)
            {
                // The wreck keeps its place in the tracked world for cleanup; only its
                // dot goes, so the minimap does not point at the lake bed.
                if (replace < _spotterBlips.Count && _spotterBlips[replace] != null && _spotterBlips[replace].Exists()) _spotterBlips[replace].Delete();
                _spotters[replace] = plane;
                if (replace < _pilots.Count) _pilots[replace] = pilot; else _pilots.Add(pilot);
                if (replace < _spotterBlips.Count) _spotterBlips[replace] = blip; else _spotterBlips.Add(blip);
            }
            else { _spotters.Add(plane); _pilots.Add(pilot); _spotterBlips.Add(blip); }
            return true;
        }

        /// <summary>
        /// The second spotter is the one carrying the charter's call sign, and the mission
        /// used to fail the instant it was not flyable as "The charter" opened. It is a
        /// light aircraft on a banked circuit: it can stall into the lake by itself, and a
        /// Lazer missile locked onto the lead in the first stage can take it instead. None
        /// of that is the player choosing to shoot the man Gohan is listening to, so it is
        /// put back in the air rather than ending the job (the September 22 audit).
        ///
        /// What still fails is the authored beat: Ron shooting it down himself while Gohan
        /// is mid-call sign. The damage record says whose it was.
        /// </summary>
        private void WatchSecondSpotter()
        {
            if (_leadHeld || _spotters.Count < 2) return;
            var spotter = _spotters[1];
            if (spotter != null && spotter.Exists() && spotter.IsDriveable && !spotter.IsDead) return;
            if (_listening && ShotDownByRon(spotter))
            {
                Fail("The second spotter went into the lake before Gohan had the charter's call sign. The lead went with him.");
                return;
            }
            if (_relaunches >= SpotterRelaunches)
            {
                Fail("The second spotter kept going down before Gohan had the charter's call sign. Retry the scramble.");
                return;
            }
            _relaunches++;
            var planeModel = new Model("mammatus");
            var pilotModel = new Model("g_m_y_mexgoon_02");
            bool back = false;
            try
            {
                if (GameUtils.RequestModel(planeModel) && GameUtils.RequestModel(pilotModel))
                    back = SpawnSpotter(planeModel, pilotModel, RelaunchPoint(), 1);
            }
            finally { planeModel.MarkAsNoLongerNeeded(); pilotModel.MarkAsNoLongerNeeded(); }
            if (!back) { Fail("The second spotter could not be put back in the air. Retry the scramble."); return; }
            Logger.Info("M26: the second spotter went down before the call sign without Ron firing on it; relaunched it (" + _relaunches + " of " + SpotterRelaunches + ").");
            GameUtils.Subtitle("~y~The second spotter is back over the lake. Hold fire until Gohan has the call sign.", 4000);
        }

        /// <summary>Whether the player, or the aircraft he is flying, did the damage.</summary>
        private static bool ShotDownByRon(Vehicle spotter)
        {
            if (spotter == null || !spotter.Exists()) return false;
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return false;
            if (Function.Call<bool>(Hash.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY, spotter, player, true)) return true;
            var flying = player.CurrentVehicle;
            return flying != null && flying.Exists() && Function.Call<bool>(Hash.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY, spotter, flying, true);
        }

        /// <summary>The far side of the patrol circle from Ron, at the height the first ones were made at.</summary>
        private Vector3 RelaunchPoint()
        {
            var player = Game.Player.Character;
            var away = player != null && player.Exists()
                ? new Vector3(_patrolBox.X - player.Position.X, _patrolBox.Y - player.Position.Y, 0f)
                : Vector3.Zero;
            float length = away.Length();
            var offset = length > 1f ? away * (RelaunchStandoff / length) : new Vector3(60f, 80f, 0f);
            return _patrolBox + offset + new Vector3(0f, 0f, 40f);
        }

        /// <summary>
        /// A jet parked cold has to start when he gets in. GTA leaves an aircraft whose
        /// engine was explicitly switched off exactly that way: the player can fire its
        /// guns and never accelerate, which is what Ron reported. Checked every frame so
        /// a re-entry after any stall behaves the same, and never forced while he is out
        /// of it, because the cold aircraft on the apron is the point of the opening.
        /// </summary>
        private void KeepInterceptorReady()
        {
            if (_duster == null || !_duster.Exists() || _duster.IsDead) return;
            var player = Game.Player.Character;
            if (player == null || !player.Exists() || !player.IsInVehicle(_duster)) return;
            if (_duster.IsEngineRunning) return;
            _duster.IsEngineRunning = true;
            Logger.Info("M26: the interceptor was cold with Ron aboard; started it.");
        }

        /// <summary>
        /// Spotters that went down without being shot. DestroyVehicleObjective completes
        /// on any undriveable target, so a spotter that stalls into the lake by itself is
        /// silently scored as a kill and the mission looks fine while the chase never
        /// happened. This does not change the outcome; it writes down which it was, so the
        /// next report is diagnosable instead of guessable.
        /// </summary>
        private readonly HashSet<int> _reportedDown = new HashSet<int>();

        private void WatchSpotters()
        {
            var player = Game.Player.Character;
            for (int i = 0; i < _spotters.Count; i++)
            {
                var plane = _spotters[i];
                if (plane == null || !plane.Exists()) continue;
                if (plane.IsDriveable && !plane.IsDead) continue;
                if (!_reportedDown.Add(plane.Handle)) continue;
                float away = player != null && player.Exists() ? player.Position.DistanceTo(plane.Position) : -1f;
                Logger.Info("M26: spotter " + (i + 1) + " is down at " + plane.Position +
                            ", " + (int)away + " m from Ron" +
                            (away > 350f ? " — too far to have been him, so it came down on its own." : "."));
            }
        }

        /// <summary>
        /// The listening beat, run as flying. The clock only advances while Guess is inside
        /// <see cref="ListenRange"/> of the second spotter, and the remaining time is on
        /// screen, so the stage reads as a job rather than as dead air.
        /// </summary>
        private bool Listened()
        {
            if (!_listening) return false;
            int now = Game.GameTime;
            int step = System.Math.Max(0, System.Math.Min(1000, now - _lastListenTick));
            _lastListenTick = now;
            var player = Game.Player.Character;
            var spotter = _spotters.Count > 1 ? _spotters[1] : null;
            bool close = player != null && player.Exists() && spotter != null && spotter.Exists() &&
                         player.Position.DistanceTo(spotter.Position) <= ListenRange;
            if (close) _listenLeft -= step;
            if (_listenLeft <= 0) { _listenUntil = now; return true; }
            GameUtils.Subtitle(close
                ? "~g~On his wing. Gohan needs " + ((_listenLeft / 1000) + 1) + "s more."
                : "~o~Too far out. Close inside " + (int)ListenRange + " m or Gohan loses the call sign.", 400);
            return false;
        }

        protected override void OnUpdate()
        {
            KeepInterceptorReady();
            WatchSpotters();
            WatchSecondSpotter();
            if (Status != MissionStatus.Running) return;
            base.OnUpdate();
        }

        protected override void OnPassed()
        {
            // The Lazer and the Duster stay on the apron: M27 starts beside them.
            if (_duster != null && _duster.Exists()) Release(_duster);
            if (_approachPlane != null && _approachPlane.Exists()) Release(_approachPlane);
            if (_granger != null && _granger.Exists()) Release(_granger);
        }

        protected override void OnCleanup()
        {
            Ctx.Crew.CompanionsHoldPosition = false;
            _spotters.Clear();
            _pilots.Clear();
            _spotterBlips.Clear();
        }
    }
}
