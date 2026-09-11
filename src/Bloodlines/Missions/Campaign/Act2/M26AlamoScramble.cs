using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

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

        /// <summary>How long Gohan needs on the second spotter's traffic before it can go down.</summary>
        public const int ListenMs = 12000;

        private Vehicle _duster;
        private Vehicle _approachPlane;
        private Vehicle _granger;
        private Prop _laptop;
        private Vector3 _pad;
        private Vector3 _hangar;
        private Vector3 _patrolBox;
        private int _listenUntil;
        private bool _listening, _leadHeld, _parked;

        public override string Id => "M26";
        public override string Title => "The Alamo Scramble";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;

        public Vehicle Lazer => _duster;
        public Vehicle ApproachPlane => _approachPlane;
        public Vehicle Granger => _granger;
        public IReadOnlyList<Vehicle> Spotters => _spotters;
        public bool Listening => _listening;
        public bool LeadHeld => _leadHeld;
        public bool Parked => _parked;

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _pad = Ctx.Locations.Position("M26.DusterPad");
            _hangar = Ctx.Locations.Position("M14.McKenzieHangar");
            _patrolBox = Ctx.Locations.Position("M26.PatrolBox");

            if (!Ctx.Crew.Deploy(CrewSlot.Guess, _pad + new Vector3(6f, 0f, 0f),
                    Ctx.Locations.Heading("M26.DusterPad")))
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
            Station(CrewSlot.Ice, _hangar + new Vector3(4f, -6f, 0f));
            Station(CrewSlot.Gohan, _granger != null && _granger.Exists() ? _granger.Position + new Vector3(0f, 3.5f, 0f) : _pad + new Vector3(0f, -20f, 0f));
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
                .OnEnter(context => GameUtils.Subtitle("~y~Use the Lazer aircraft weapons on the two red plane markers.", 5000))
                .OnExit(context => HoldForTraffic())
                .WithCues("M26_S1_01_GUESS");

            // The second spotter is calling somebody: what he is calling is the lead.
            // Kill him too early and it goes into the lake with him.
            yield return new MissionStage("The charter",
                    new ConditionObjective("Keep the second spotter in sight while Gohan pulls the charter's call sign from his traffic.", () => Game.GameTime >= _listenUntil),
                    new ReactionTrigger(() => _spotters.Count > 1 && (!_spotters[1].Exists() || !_spotters[1].IsDriveable), () => Fail("The second spotter went into the lake before Gohan had the charter's call sign. The lead went with him.")))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context => HoldTheLead());

            yield return new MissionStage("Second spotter",
                    new DestroyVehicleObjective("The second one is diving for Grapeseed — kill him.",
                        () => _spotters.Count > 1 ? _spotters[1] : null))
                .OwnedBy(CrewSlot.Guess)
                .WithCues("M26_S1_02_GOHAN");

            yield return new MissionStage("Home",
                    new DeliverVehicleObjective("Guess: land the Lazer at McKenzie and stop.", () => _duster, () => _pad, 65f, land: true))
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
                Reason = "Two spotters quartering the Alamo over the water where the container sits. The Lazer on the McKenzie apron: towed out of Zancudo under the M16 clearance, fueled here, the aircraft Ron flies today. Gohan at the laptop on the Granger's hood with their traffic; Ice at the hangar door on the radio. No fourth pilot.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M26 approach scene did not play; the apron stands on its own.");
        }

        /// <summary>The first spotter down: the second is calling somebody, and that call is worth more than the kill for a moment.</summary>
        private void HoldForTraffic()
        {
            _listening = true;
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
                Reason = "The Lazer parked and shut down at McKenzie, Ron out of it. Beside it the Duster: two seats, the aircraft for the Shamal job. The Lazer stays here; nobody flies a fighter onto a jet's wing.",
                Blocking = blocking
            };
            var cue = Ctx.Data?.Cue("M26_S1_03_GUESS");
            if (!Ctx.Cutscenes.PlayStaged(spec, new[] { cue })) { Logger.Warn("M26 park scene did not play; the line plays as dialogue."); blocking.Complete(); Say("M26_S1_03_GUESS"); }
            if (_duster != null && _duster.Exists()) _duster.IsEngineRunning = false;
            Ctx.State?.SetCargo("lazer", "M26.DusterPad");
            GameUtils.Subtitle("~g~Both spotters in the lake; the Alamo stash stays a ghost. The Lazer is parked at McKenzie; the Shamal job flies the Duster.", 6000);
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

            _duster = Track(World.CreateVehicle(model, _pad, Ctx.Locations.Heading("M26.DusterPad")));
            model.MarkAsNoLongerNeeded();
            if (_duster == null || !_duster.Exists()) return;

            _duster.IsPersistent = true;
            _duster.IsEngineRunning = false;

            var blip = Track(_duster.AddBlip());
            blip.Sprite = BlipSprite.Plane;
            blip.Color = BlipColor.Orange;
            blip.Name = "Lazer interceptor";
        }

        /// <summary>The Duster beside the Lazer: two seats, radial engine, the aircraft M27 flies. Parked here so the change is visible.</summary>
        private void SpawnApproachPlane()
        {
            var model = new Model("duster");
            if (!GameUtils.RequestModel(model)) return;
            _approachPlane = Track(World.CreateVehicle(model, _pad + new Vector3(24f, 0f, 0f), Ctx.Locations.Heading("M26.DusterPad")));
            model.MarkAsNoLongerNeeded();
            if (_approachPlane == null || !_approachPlane.Exists()) return;
            _approachPlane.IsPersistent = true;
            _approachPlane.IsEngineRunning = false;
        }

        /// <summary>The crew's Granger on the apron with Gohan's laptop on the hood: the transmission problem has a place.</summary>
        private void SpawnGranger()
        {
            var spot = _pad + new Vector3(-30f, 12f, 0f);
            Vehicle granger = Ctx.Vans != null ? Ctx.Vans.Spawn(spot, 90f) : null;
            if (granger == null)
            {
                var model = new Model("granger");
                if (!GameUtils.RequestModel(model)) return;
                granger = World.CreateVehicle(model, spot, 90f);
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

            var cartel = World.AddRelationshipGroup("BLOODLINES_CARTEL");

            for (int i = 0; i < 2; i++)
            {
                var plane = Track(World.CreateVehicle(planeModel,
                    _patrolBox + new Vector3(i * 120f - 60f, i * 80f, i * 40f), 180f));
                if (plane == null || !plane.Exists()) continue;
                plane.IsPersistent = true; plane.IsEngineRunning = true; plane.ForwardSpeed = 40f;
                _spotters.Add(plane);

                var pilot = Track(World.CreatePed(pilotModel, plane.Position, 0f));
                if (pilot == null || !pilot.Exists()) continue;

                pilot.RelationshipGroup = cartel;
                pilot.IsPersistent = true;
                pilot.BlockPermanentEvents = true;
                pilot.Task.WarpIntoVehicle(plane, VehicleSeat.Driver);
                // Quartering the lake, not hunting the player: they are looking for gold.
                pilot.Task.StartPlaneMission(plane, _patrolBox, VehicleMissionType.Circle,
                    40f, 60f, 220, 40, 0f, false);
                _pilots.Add(pilot);

                var blip = Track(plane.AddBlip());
                blip.Sprite = BlipSprite.Plane;
                blip.Color = BlipColor.Red;
                blip.Name = "Cartel spotter";
            }

            planeModel.MarkAsNoLongerNeeded();
            pilotModel.MarkAsNoLongerNeeded();
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
        }
    }
}
