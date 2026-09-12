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
    /// M21 — "The Port Heist: Open Water". Outer breakwater, 03:45.
    ///
    /// Part three. The Cargobob is slow and heavy and cannot defend itself, so Gohan
    /// runs an armed launch alongside it, pulling missile locks onto his own wake and
    /// killing the Aegis speedboats before they get in range.
    ///
    /// Seen, not told: Gohan at the helm and Ice in the launch's other seat, Ron's
    /// loaded lift held over the water until the escort is on it; the harbor as the
    /// preparation missions left it (M13's burn thins the launches, M15's tap opens
    /// the breakwater gate) said on the radio and felt in the spawn, not assumed;
    /// the split after the breakwater announced; the two men landing at the shore
    /// and boarding the staged Granger on camera, so nobody arrives at an inland
    /// lake in an ocean boat.
    ///
    /// The mission is an escort played from the escort's side — the thing being
    /// protected is flown by an ally, not by the player, which is the only way the
    /// bible's three-vehicle convergence works without three players.
    /// </summary>
    public sealed class M21OpenWater : ComposedMission
    {
        private readonly List<Ped> _hostileCrews = new List<Ped>();

        private Vehicle _launch;
        private Vehicle _cargobob;
        private Vehicle _granger;
        private Prop _container;
        private Ped _cargobobPilot;
        private OperationHandoff _handoff;
        private Vector3 _spawn;
        private Vector3 _breakwater;
        private Vector3 _ridge;
        private Vector3 _shore;
        private Vector3 _pickup;
        private bool _patrolsReduced, _gateAccess, _harborReported, _split, _transferred;
        private LiveHandoff _liveTransfer;

        public override string Id => "M21";
        public override string Title => "The Port Heist: Open Water";
        protected override MissionEndpoint Endpoint => MissionEndpoint.ContinuousNext;

        public Vehicle Launch => _launch;
        public Vehicle Cargobob => _cargobob;
        public Vehicle Granger => _granger;
        public IReadOnlyList<Ped> HostileCrews => _hostileCrews;
        public bool PatrolsReduced => _patrolsReduced;
        public bool GateAccess => _gateAccess;
        public bool HarborReported => _harborReported;
        public bool Split => _split;
        public bool Transferred => _transferred;

        protected override bool Setup()
        {
            MissionSites.Ground(Ctx.Locations, "M21.RoadPickup");
            _spawn = MarineSites.ResolveOrThrow(Ctx.Locations, "M21.LaunchSpawn", 3f, 3f, 6f);
            _breakwater = MarineSites.ResolveOrThrow(Ctx.Locations, "M21.Breakwater", 3f, 3f, 6f);
            _ridge = Ctx.Locations.Position("M21.RidgeCross");
            _shore = MarineSites.ResolveOrThrow(Ctx.Locations, "M21.ShoreLanding", 2f, 2f, 4f, 40f);
            _pickup = Ctx.Locations.Position("M21.RoadPickup");

            // On the pier, not in the water: a deployment onto a water coordinate
            // starts the mission with everyone swimming.
            if (!PortHeist.IsContinuing(Ctx) && !Ctx.Crew.Deploy(CrewSlot.Gohan, Ctx.Locations.Position("M12.PierWatch"),
                    Ctx.Locations.Heading("M12.PierWatch")))
            {
                return false;
            }

            ApplyBibleSetting();
            Ctx.Crew.PedFor(CrewSlot.Gohan).Weapons.Give(WeaponHash.MG, 400, false, true);

            _handoff = Ctx.Handoffs.Take(PortHeist.Operation, Id);
            _patrolsReduced = Ctx.State != null && Ctx.State.FleetUpgrades.TryGetValue("harborPatrolsReduced", out var thinned) && thinned;
            _gateAccess = Ctx.State != null && Ctx.State.FleetUpgrades.TryGetValue("harborGateAccess", out var gate) && gate;
            SpawnLaunch();
            SpawnCargobob();
            SpawnGranger();
            if (!RequireAssets(_launch, _cargobob, _cargobobPilot, _container, _granger)) return false;
            Ctx.PortHeist?.Bind("launch", _launch);
            Ctx.PortHeist?.Bind("lift", _cargobob);
            Ctx.PortHeist?.Bind("bullion", _container);
            Ctx.PortHeist?.Bind("granger", _granger);
            RequireAsset(_granger, "The road pickup was destroyed. The crew cannot make the inland transfer.");
            RequireAsset(_cargobob, "The Cargobob went down with the bullion.");
            RequireAsset(_container, "The bullion container was lost.");
            RequireAsset(_launch, "The armed launch was destroyed. The lift has no escort.");
            if (!PortHeist.IsContinuing(Ctx))
            {
                Station(CrewSlot.Gohan, _launch, VehicleSeat.Driver);
                Station(CrewSlot.Ice, _launch, VehicleSeat.Passenger);
            }
            else
            {
                if (!PortHeistWorld.Seated(Ctx.Crew.PedFor(CrewSlot.Gohan), _launch, VehicleSeat.Driver) ||
                    !PortHeistWorld.Seated(Ctx.Crew.PedFor(CrewSlot.Ice), _launch, VehicleSeat.Passenger))
                    throw new System.InvalidOperationException("The escort cannot start before Ice and Gohan board it.");
                Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Gohan);
                Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Ice);
            }
            Ctx.Crew.PedFor(CrewSlot.Ice).Weapons.Give(WeaponHash.MicroSMG, 500, true, true);
            // The lift waits, airborne and rotors turning, until the escort is actually
            // on the water. Nothing flies toward the breakwater while Gohan is still
            // boarding the launch.
            HoldLift();
            // Inside the one continuous heist nothing stops the action at the join.
            if (!PortHeist.IsContinuing(Ctx)) PlayApproach();
            return true;
        }

        private void HoldLift()
        {
            if (_cargobob == null || !_cargobob.Exists()) return;
            _cargobob.IsPositionFrozen = true;
            _cargobob.IsEngineRunning = true;
            Function.Call(Hash.SET_HELI_BLADES_FULL_SPEED, _cargobob);
        }

        private void ReleaseLift()
        {
            if (_cargobob == null || !_cargobob.Exists()) return;
            _cargobob.IsPositionFrozen = false;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Get on the water",
                    new EnterVehicleObjective("Gohan — take the armed launch.", () => _launch,
                        VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Gohan);

            yield return new MissionStage("Draw the locks",
                    new ShadowTargetObjective("Stay on the Cargobob's wing.", () => _cargobob, 240f, 15,
                        "The launch lost contact with the Cargobob.", acquireSeconds: 60),
                    new ProtectObjective("", () => _cargobob, "The Cargobob went down with the bullion."))
                .OnEnter(context => { ReleaseLift(); StartCargobob(); SpawnHostileBoats(); ReportHarbor(); })
                .WithCues("M21_S1_01_GOHAN");

            yield return new MissionStage("Kill the speedboats",
                    new KillTargetsObjective("Clear the Aegis boats before they close.",
                        () => _hostileCrews),
                    new ProtectObjective("", () => _cargobob, "The Cargobob went down with the bullion."))
                .AfterCues("M21_S1_02_ICE");

            // The breakwater is where the operation splits: the lift goes north by air,
            // the launch turns back for the shore.
            yield return new MissionStage("The breakwater",
                    new DeliverVehicleObjective("Gohan: take the launch through the yellow breakwater exit.", () => _launch, () => _breakwater, 50f),
                    new ProtectObjective("", () => _cargobob, "The Cargobob went down with the bullion."))
                .OnExit(context => AnnounceSplit())
                .AfterCues("M21_S1_03_GUESS");

            yield return new MissionStage("Shore transfer",
                    new DeliverVehicleObjective("Gohan: bring the launch in to the marked shore landing.", () => _launch, () => _shore, 15f))
                .OnExit(context => { if (!PortHeist.IsContinuing(Ctx)) PlayTransfer(); });

            // In the one continuous heist the road transfer is not a cut: whoever is
            // played walks to the Granger and takes a seat, the other boards under AI,
            // and the part ends on their real seats (Ron, September 11).
            if (PortHeist.IsContinuing(Ctx))
                yield return new MissionStage("The road north",
                        new ConditionObjective("Gohan and Ice: out of the launch and into the Granger at the road, Gohan at the wheel.", RoadTeamAboard),
                        new ReactionTrigger(() => _liveTransfer == null, StartLiveTransfer))
                    .OnExit(context =>
                    {
                        _transferred = true;
                        _liveTransfer?.Cancel(); _liveTransfer = null;
                        GameUtils.Subtitle("~g~Ice and Gohan on the road north in the Granger. The lift is over the mountains; the Alamo is next.", 6000);
                    });
        }

        private bool RoadTeamAboard() =>
            PortHeistWorld.Seated(Ctx.Crew.PedFor(CrewSlot.Gohan), _granger, VehicleSeat.Driver) &&
            PortHeistWorld.Seated(Ctx.Crew.PedFor(CrewSlot.Ice), _granger, VehicleSeat.Passenger);

        /// <summary>The road pickup crewed live: out of the launch, up to the Granger, into its seats, under AI for whoever is not being played; the lines over the radio.</summary>
        private void StartLiveTransfer()
        {
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Gohan);
            Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Ice);
            var live = new LiveHandoff("M21 road pickup");
            bool granger = _granger != null && _granger.Exists();
            foreach (var rider in new[] { gohan, ice })
            {
                if (rider == null || !rider.Exists()) continue;
                if (rider.IsInVehicle()) live.Then(new ExitVehicleStep(rider));
                if (granger) live.Then(new WalkToStep(rider, _granger.Position + new Vector3(rider == gohan ? -2f : 2f, 0f, 0f), 1.5f, true));
            }
            if (granger)
            {
                if (gohan != null && gohan.Exists()) live.Then(new EnterVehicleStep(gohan, _granger, VehicleSeat.Driver));
                if (ice != null && ice.Exists()) live.Then(new EnterVehicleStep(ice, _granger, VehicleSeat.Passenger));
            }
            _liveTransfer = live;
            Radio("GOHAN", "Launch is on the sand. We're in the Granger; taking the road north. Keep the lift in sight until we clear the coast.", "M21_RADIO_03_GOHAN");
            Radio("GUESS", "I'm taking the ridge toward the shallows. Call when you reach the beach; I'll bring the load down then.", "M21_RADIO_04_GUESS");
            Logger.Info("M21: the road pickup boards live; no cut at the join.");
        }

        protected override void OnUpdate()
        {
            _liveTransfer?.Update();
            base.OnUpdate();
        }

        // ---------- beats ----------

        /// <summary>Gohan at the helm, Ice in the other seat, Ron's loaded lift held over the water: the three roles seen before any of them move.</summary>
        private void PlayApproach()
        {
            var blocking = new SceneBlocking();
            if (_launch != null && _launch.Exists()) blocking.Then(new ShotStep(3200, _launch, new Vector3(-5f, 3.5f, 1.8f), _launch, new Vector3(0f, 0f, 0.8f), 0.6f));
            if (_cargobob != null && _cargobob.Exists()) blocking.Then(new ShotStep(3200, _cargobob, new Vector3(-18f, 10f, 2f), _cargobob, new Vector3(0f, 0f, -4f), 1.2f));
            blocking.Then(ShotStep.Wide(2800, _breakwater + new Vector3(0f, 0f, 2f), 60f, 22f, 20f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "The escort",
                Reason = "Gohan at the launch's helm, Ice in its other seat with the gun, Ron's lift held over the basin with the container under it. The breakwater is the exit: the lift goes north from there by air, the launch turns back. No swap, no fourth man.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M21 approach scene did not play; the pier stands on its own.");
        }

        /// <summary>The harbor as the preparation left it: M13's burn and M15's tap, each said once and each reflected in what comes at the launch.</summary>
        private void ReportHarbor()
        {
            _harborReported = true;
            if (_patrolsReduced) Radio("ICE", "Two launches left in the basin after the slipway burn. Not none: two.", "M21_RADIO_01_ICE");
            else Radio("ICE", "Three launches with full tanks. Nothing we did thinned them; they come as they are.", "M21_RADIO_01_ICE");
            if (_gateAccess) Radio("GOHAN", "Gate one is answering the tap. The breakwater opens for us and closes behind; they come the long way round.", "M21_RADIO_02_GOHAN");
            else Radio("GOHAN", "No gate access. The breakwater is theirs; we go through it under fire.", "M21_RADIO_02_GOHAN");
        }

        /// <summary>After the breakwater: Ron's inland flight announced, the launch turned for the shore.</summary>
        private void AnnounceSplit()
        {
            _split = true;
            if (_cargobobPilot != null && _cargobobPilot.Exists() && _cargobob != null && _cargobob.Exists())
            {
                var north = _ridge + new Vector3(0f, 0f, 40f);
                _cargobobPilot.Task.StartHeliMission(_cargobob, north, VehicleMissionType.GoTo, 20f, 30f,
                    (int)north.Z, 60, -1f, 60f, (HeliMissionFlags)(256 | 4096));
            }
            GameUtils.Subtitle("~g~Water route clear. Ron takes the bullion north by air; the launch turns for the shore and the Granger.", 6000);
        }

        /// <summary>Ice and Gohan land, walk to the staged Granger, board with real seats and pull away: the road north, compressed but seen.</summary>
        private void PlayTransfer()
        {
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var blocking = new SceneBlocking { DialogueAfterStep = 0 };
            bool granger = _granger != null && _granger.Exists();
            foreach (var rider in new[] { gohan, ice })
            {
                if (rider == null || !rider.Exists()) continue;
                if (rider.IsInVehicle()) blocking.Then(new ExitVehicleStep(rider));
                if (granger) blocking.Then(new WalkToStep(rider, _granger.Position + new Vector3(rider == gohan ? -2f : 2f, 0f, 0f), 1.5f, true));
            }
            if (granger)
            {
                if (gohan != null && gohan.Exists()) blocking.Then(new EnterVehicleStep(gohan, _granger, VehicleSeat.Driver));
                if (ice != null && ice.Exists()) blocking.Then(new EnterVehicleStep(ice, _granger, VehicleSeat.Passenger));
                var away = World.GetNextPositionOnStreet(_pickup + new Vector3(0f, 45f, 0f));
                if (gohan != null && gohan.Exists()) blocking.Then(new DriveUpStep(gohan, _granger, away, DriveUpStep.HeadingBetween(_pickup, away)));
                else blocking.Then(new ShotStep(3200, _granger, new Vector3(-6f, 3f, 1.8f), _granger, new Vector3(0f, 0f, 0.8f), 0.7f));
            }
            blocking.Then(new VerifySceneStep("The road pickup is crewed", () =>
                PortHeistWorld.Seated(gohan, _granger, VehicleSeat.Driver) &&
                PortHeistWorld.Seated(ice, _granger, VehicleSeat.Passenger), () => _transferred = true));
            var spec = new SceneSpec
            {
                RequiresCompletion = true,
                MissionId = Id, Phase = "transfer", Title = "The road north",
                Reason = "The launch on the shore, Gohan and Ice out of it and into the crew's Granger at the road, Gohan driving, Ice beside him, pulling away north. The boat stays on the coast; the Granger arrives at the Alamo.",
                Blocking = blocking
            };
            var lines = new[]
            {
                new DialogueCue { CueId = "M21_RADIO_03_GOHAN", MissionId = Id, Speaker = "GOHAN", Line = "Launch is on the sand. We're in the Granger; taking the road north. Keep the lift in sight until we clear the coast." },
                new DialogueCue { CueId = "M21_RADIO_04_GUESS", MissionId = Id, Speaker = "GUESS", Line = "I'm taking the ridge toward the shallows. Call when you reach the beach; I'll bring the load down then." }
            };
            if (!Ctx.Cutscenes.PlayStaged(spec, lines))
            {
                Logger.Warn("M21 transfer scene did not play; the seats are taken directly.");
                PortHeist.RequireFallback(blocking, "Boarding the road pickup");
            }
            GameUtils.Subtitle("~g~Ice and Gohan on the road north in the Granger. The lift is over the mountains; the Alamo is next.", 6000);
        }

        /// <summary>The aftermath: the Granger leaving the coast, the launch left on the shore.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_granger != null && _granger.Exists())
                return new SceneBlocking().Then(new ShotStep(4500, _granger, new Vector3(-7f, -4f, 2f), _granger, new Vector3(0f, 0f, 0.8f), 1.0f));
            if (_launch == null || !_launch.Exists()) return null;
            return new SceneBlocking().Then(new ShotStep(4500, _launch, new Vector3(-8f, 5f, 3f), _launch, new Vector3(0f, 0f, 0.5f), 1.0f));
        }

        // ---------- world building ----------

        /// <summary>The launch M20 boarded on camera, taken over at its mark; or one there.</summary>
        private void SpawnLaunch()
        {
            if (PortHeist.IsContinuing(Ctx)) { _launch = Track(Ctx.PortHeist.Require<Vehicle>("launch")); return; }
            var model = new Model("dinghy4");
            if (!GameUtils.RequestModel(model)) return;

            var existing = PortHeist.Nearby(model, _spawn, 40f);
            _launch = Track(existing ?? World.CreateVehicle(model, _spawn, Ctx.Locations.Heading("M21.LaunchSpawn")));
            model.MarkAsNoLongerNeeded();
            if (_launch == null || !_launch.Exists()) return;

            _launch.IsPersistent = true;
            _launch.EnginePowerMultiplier = 6f;
            Logger.Info("M21: launch " + (existing != null ? "taken over from M20" : "spawned") + " at M21.LaunchSpawn.");

            var blip = Track(_launch.AddBlip());
            blip.Sprite = BlipSprite.Boat;
            blip.Color = BlipColor.Green;
            blip.Name = "Armed launch";
        }

        private void SpawnCargobob()
        {
            if (PortHeist.IsContinuing(Ctx))
            {
                _cargobob = Track(Ctx.PortHeist.Require<Vehicle>("lift"));
                _container = Track(Ctx.PortHeist.Require<Prop>("bullion"));
                _cargobobPilot = Ctx.Crew.PedFor(CrewSlot.Guess);
                if (!PortHeistWorld.Attached(_container, _cargobob) || !PortHeistWorld.Seated(_cargobobPilot, _cargobob, VehicleSeat.Driver))
                    throw new System.InvalidOperationException("The lift must continue with its pilot and attached bullion.");
                Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Guess);
                return;
            }
            var model = new Model("cargobob");
            var pilotModel = new Model("g_m_y_famca_01");
            if (!GameUtils.RequestModel(model) || !GameUtils.RequestModel(pilotModel)) return;

            // With a fresh M20 record the lift starts where the climb-out ended and
            // still flies heavy; otherwise the default staging above the launch.
            var start = _handoff != null && _handoff.VehicleModel.Length > 0 ? _handoff.VehiclePosition : _spawn + new Vector3(-40f, 30f, 45f);
            float heading = _handoff != null && _handoff.VehicleModel.Length > 0 ? _handoff.VehicleHeading : 20f;
            var existing = _handoff != null ? PortHeist.Nearby(model, start, 30f) : null;
            _cargobob = Track(existing ?? World.CreateVehicle(model, start, heading));
            if (_cargobob == null || !_cargobob.Exists()) return;
            _cargobob.IsPersistent = true;
            AttachContainer();

            _cargobobPilot = Ctx.Crew.PedFor(CrewSlot.Guess);
            model.MarkAsNoLongerNeeded();
            pilotModel.MarkAsNoLongerNeeded();
            if (_cargobobPilot == null || !_cargobobPilot.Exists()) return;

            _cargobobPilot.RelationshipGroup = Ctx.Crew.CrewGroup;
            _cargobobPilot.IsPersistent = true;
            _cargobobPilot.BlockPermanentEvents = true;
            Station(CrewSlot.Guess, _cargobob, VehicleSeat.Driver);
            _cargobob.IsEngineRunning = true;

            var blip = Track(_cargobob.AddBlip());
            blip.Sprite = BlipSprite.Helicopter;
            blip.Color = BlipColor.Blue;
            blip.Name = "Bullion lift";
        }

        /// <summary>
        /// The story says this is the bullion lift, so the bullion has to be visible
        /// under it. M20 attached the container to the aircraft; the same prop is
        /// re-attached here when it is still under the lift, and rebuilt when it is
        /// not, so the escort protects something the player can see.
        /// </summary>
        private void AttachContainer()
        {
            var containerModel = new Model("prop_container_01a");
            if (!GameUtils.RequestModel(containerModel)) return;
            var existing = PortHeist.NearbyProp(containerModel, _cargobob.Position, 12f);
            _container = Track(existing ?? World.CreateProp(containerModel, _cargobob.Position - new Vector3(0f, 0f, 7f), false, false));
            containerModel.MarkAsNoLongerNeeded();
            if (_container == null || !_container.Exists()) return;
            _container.IsPersistent = true;
            Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY, _container, _cargobob, 0,
                0f, 0f, -6.5f, 0f, 0f, 0f, false, false, true, false, 2, true);
            Logger.Info("M21: bullion container " + (existing != null ? "carried on" : "rebuilt") + " under the escorted lift" + (_handoff != null ? " (continuing M20's lift)." : " (default staging)."));
        }

        /// <summary>The crew's Granger at the road above the shore landing: the transport that takes two men to an inland lake.</summary>
        private void SpawnGranger()
        {
            Vehicle granger = Ctx.Vans != null ? Ctx.Vans.Spawn(_pickup, Ctx.Locations.Heading("M21.RoadPickup")) : null;
            if (granger == null)
            {
                var model = new Model("granger");
                if (!GameUtils.RequestModel(model)) return;
                granger = World.CreateVehicle(model, _pickup, Ctx.Locations.Heading("M21.RoadPickup"));
                model.MarkAsNoLongerNeeded();
            }
            _granger = Track(granger);
            if (_granger == null || !_granger.Exists()) return;
            _granger.IsPersistent = true;
            _granger.IsEngineRunning = false;
        }

        /// <summary>The lift crosses the ridge with the bullion and the other two are on the road; M22 starts from that state.</summary>
        protected override void OnPassed()
        {
            var record = OperationHandoff.Capture(PortHeist.Operation, Id, "M22", Ctx.Crew, _cargobob);
            record.CargoAttached = PortHeistWorld.Attached(_container, _cargobob);
            record.CargoModel = "prop_container_01a";
            record.Notes["granger"] = _transferred ? "Gohan driving, Ice beside him, north from M21.RoadPickup" : "not boarded on camera";
            record.Notes["launch"] = "left at M21.ShoreLanding";
            record.Notes["harbor"] = (_patrolsReduced ? "launches thinned by M13" : "launches at full strength") + "; " + (_gateAccess ? "breakwater gate opened by M15" : "no gate access");
            Ctx.Handoffs.Record(record);
            if (_container != null && _container.Exists()) Release(_container);
            if (_cargobob != null && _cargobob.Exists()) Release(_cargobob);
            if (_granger != null && _granger.Exists()) Release(_granger);
            if (_launch != null && _launch.Exists()) Release(_launch);
        }

        private void StartCargobob()
        {
            if (_cargobobPilot == null || !_cargobobPilot.Exists()) return;
            if (_cargobob == null || !_cargobob.Exists()) return;

            // Heavy and slow on purpose: the escort has to be able to keep up with it,
            // and the player has to feel why it needs protecting.
            _cargobob.EnginePowerMultiplier = 0.6f;
            var target = _breakwater + new Vector3(0f, 0f, 45f);
            _cargobobPilot.Task.StartHeliMission(_cargobob, target, VehicleMissionType.GoTo, 12f, 25f,
                (int)target.Z, 35, -1f, 50f, (HeliMissionFlags)(256 | 4096));
        }

        /// <summary>The Aegis launches: two after M13's burn, three without it; from beyond the gate when M15 opened it, from the breakwater when it did not.</summary>
        private void SpawnHostileBoats()
        {
            var boatModel = new Model("predator");
            var crewModel = new Model("s_m_y_blackops_01");
            if (!GameUtils.RequestModel(boatModel) || !GameUtils.RequestModel(crewModel)) return;

            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            int count = _patrolsReduced ? 2 : 3;
            float standoff = _gateAccess ? 90f : 20f;

            for (int i = 0; i < count; i++)
            {
                var boat = Track(World.CreateVehicle(boatModel,
                    _breakwater + new Vector3(-40f + i * 40f, standoff, 0f), 200f));
                if (boat == null || !boat.Exists()) continue;
                boat.IsPersistent = true;

                var crew = Track(World.CreatePed(crewModel, boat.Position, 0f));
                if (crew == null || !crew.Exists()) continue;

                crew.RelationshipGroup = aegis;
                crew.IsPersistent = true;
                crew.BlockPermanentEvents = true;
                crew.Accuracy = 35;
                crew.Weapons.Give(WeaponHash.CarbineRifle, 250, true, true);
                crew.Task.WarpIntoVehicle(boat, VehicleSeat.Driver);
                crew.Task.StartBoatMission(boat, _launch, VehicleMissionType.GoTo, 14f, (VehicleDrivingFlags)786603, 20f, (BoatMissionFlags)7);
                var gunner = Track(World.CreatePed(crewModel, boat.Position, 0f));
                if (gunner != null && gunner.Exists())
                {
                    gunner.RelationshipGroup = aegis; gunner.IsPersistent = true; gunner.BlockPermanentEvents = true;
                    gunner.Accuracy = 20; gunner.Weapons.Give(WeaponHash.MicroSMG, 500, true, true);
                    gunner.SetIntoVehicle(boat, VehicleSeat.Passenger); gunner.Task.VehicleShootAtPed(Game.Player.Character);
                    _hostileCrews.Add(gunner);
                }

                _hostileCrews.Add(crew);

                var blip = Track(boat.AddBlip());
                blip.Sprite = BlipSprite.Boat;
                blip.Color = BlipColor.Red;
                blip.Name = "Aegis launch";
            }
            Logger.Info("M21: " + count + " Aegis launches" + (_patrolsReduced ? " (thinned by M13)" : "") + (_gateAccess ? ", held beyond the gate M15 opened." : " at the breakwater."));

            boatModel.MarkAsNoLongerNeeded();
            crewModel.MarkAsNoLongerNeeded();
        }

        protected override void OnCleanup()
        {
            // A held lift that outlives the attempt (occupied transports are released,
            // not deleted) must not stay pinned in the air.
            ReleaseLift();
            if (Status != MissionStatus.Passed && _container != null && _container.Exists()) Function.Call(Hash.DETACH_ENTITY, _container, true, true);
            _hostileCrews.Clear();
        }
    }
}
