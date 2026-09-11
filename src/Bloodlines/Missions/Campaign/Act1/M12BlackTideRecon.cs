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
    /// M12 — "Black Tide Recon". Berth 44, Port of Los Santos, 21:30, night fog.
    ///
    /// Gohan maps the freighter's hull for the breach point while Ice watches the
    /// patrol launches from the pier. No shooting: getting seen is the failure state,
    /// which makes this the mission that teaches the stealth rules before the Port
    /// Heist depends on them.
    ///
    /// Seen, not told: the hold number on the manifest and the survey craft on the
    /// jetty before the dive, with Gohan named as the one going under and Ice and
    /// Ron where they will stay; the scan itself as work at the hull; the patrol
    /// launches, full-tanked, shown once as the next problem; the sub back at the
    /// jetty with the route recorded. The result is "reachable hull", not "ready
    /// heist": that is what the crew learns here.
    ///
    /// The ROV drone is the submersible with its lights off, per
    /// docs/FEASIBILITY.md — a camera-only drone would need a vehicle the game does
    /// not have, and a small sub reads the same at night.
    /// </summary>
    public sealed class M12BlackTideRecon : ComposedMission
    {
        private readonly List<Ped> _patrols = new List<Ped>();
        private readonly List<Vehicle> _launches = new List<Vehicle>();

        private Vehicle _rov;
        private Vector3 _jetty;
        private Vector3 _hull;
        private Vector3 _buoy;
        private bool _surveyed, _patrolsShown;

        public override string Id => "M12";
        public override string Title => "Black Tide Recon";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;

        public Vehicle Rov => _rov;
        public bool Surveyed => _surveyed;
        public bool PatrolsShown => _patrolsShown;

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _jetty = Ctx.Locations.Position("M12.SouthJetty");
            _hull = Ctx.Locations.Position("M12.FreighterHull");
            _buoy = Ctx.Locations.Position("M12.SonarBuoy");

            if (!Ctx.Crew.Deploy(CrewSlot.Gohan, _jetty, Ctx.Locations.Heading("M12.SouthJetty")))
            {
                return false;
            }

            ApplyBibleSetting();
            Ctx.Abilities.Refill();

            SpawnRov();
            SpawnPatrols();
            if (!RequireAssets(_rov)) return false;
            Station(CrewSlot.Ice, Ctx.Locations.Position("M12.PierWatch"));
            Station(CrewSlot.Guess, _jetty + new Vector3(12f, 0f, 0f));
            PlayApproach();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Launch the ROV",
                    new EnterVehicleObjective("Gohan — take the ROV out from the south jetty.",
                        () => _rov, VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Gohan)
                .WithCues("M12_S1_01_GOHAN");

            // Detection runs alongside the work for the rest of the mission: the patrols
            // are not an obstacle to shoot through, they are the clock.
            yield return new MissionStage("Under the sonar",
                    new DeliverVehicleObjective("Gohan: descend in the sub to the underwater yellow marker.", () => _rov, () => _buoy, 6f),
                    new AvoidDetectionObjective(() => _patrols,
                        "A patrol launch caught the ROV on the surface.", 45f, 4))
                .OwnedBy(CrewSlot.Gohan)
                .OnEnter(context => GameUtils.Subtitle("~y~Dive toward the underwater marker. Stay clear of patrols and break their sight line.", 5000))
                .WithCues("M12_S1_02_ICE");

            yield return new MissionStage("Map the hull",
                    new MissionInteraction("Acoustic-scan hold 3's bulkhead.", () => _hull, 14, 8f, () => _rov),
                    new AvoidDetectionObjective(() => _patrols,
                        "A patrol launch caught the ROV on the surface.", 45f, 4))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(context => PlaySurvey());

            // The launches are the next problem, seen once; then the sub goes home.
            yield return new MissionStage("Back to the jetty",
                    new DeliverVehicleObjective("Gohan: surface in the sub beside the jetty.", () => _rov, () => _jetty + new Vector3(0f, -8f, -4f), 10f),
                    new ReactionTrigger(() => _surveyed && !_patrolsShown && !Ctx.Cutscenes.IsActive, ShowPatrols))
                .OnExit(context =>
                {
                    // What this mission actually produces is the breach point for M19,
                    // and the knowledge that the launches will box the extraction in.
                    Ctx.State?.SetEvidence("hullSurvey", EvidenceState.CopyHeld);
                    GameUtils.Subtitle("~g~Breach coordinates tagged: the hull can be opened. The launches are the next problem, not the hull.", 6000);
                });
        }

        // ---------- beats ----------

        /// <summary>The hold number and the craft: the ROV on the jetty, the freighter over the water, Ice on the pier. Gohan is the one going under.</summary>
        private void PlayApproach()
        {
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var blocking = new SceneBlocking();
            if (_rov != null && _rov.Exists()) blocking.Then(new ShotStep(3200, _rov, new Vector3(-5f, 4f, 2.2f), _rov, new Vector3(0f, 0f, 0.5f), 0.8f));
            blocking.Then(ShotStep.Wide(3400, _hull + new Vector3(0f, 0f, 12f), 40f, 18f, 10f));
            if (ice != null && ice.Exists()) blocking.Then(ShotStep.Watching(3000, ice, ice));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "Berth 44",
                Reason = "The manifest gives a hold number; the sub on the jetty is what tells the crew whether a container can come out through that hull. Gohan goes under; Ice watches the launches from the pier; Ron holds the jetty.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M12 approach scene did not play; the jetty stands on its own.");
        }

        /// <summary>The scan, seen: the sub at the hull, and Gohan's own line over it. The route is recorded.</summary>
        private void PlaySurvey()
        {
            _surveyed = true;
            var blocking = new SceneBlocking();
            if (_rov != null && _rov.Exists())
                blocking.Then(new ShotStep(3600, _rov, new Vector3(-6f, 3f, 1.5f), _rov, new Vector3(0f, 0f, 0.4f), 0.6f))
                    .Then(new ShotStep(3000, _rov, new Vector3(4f, -8f, 4f), null, _hull + new Vector3(0f, 0f, 2f), 0.4f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "survey", Title = "The hull",
                Reason = "The bulkhead scanned from the sub: eight inches of steel that a torch can open. The breach point is tagged; nothing is cut tonight.",
                Blocking = blocking
            };
            var cue = Ctx.Data?.Cue("M12_S1_03_GOHAN");
            if (!Ctx.Cutscenes.PlayStaged(spec, new[] { cue })) { Logger.Warn("M12 survey scene did not play; the line plays as dialogue."); blocking.Complete(); Say("M12_S1_03_GOHAN"); }
        }

        /// <summary>The launches, shown once: full tanks at their moorings. Whatever comes out of the hull, they box it in.</summary>
        private void ShowPatrols()
        {
            _patrolsShown = true;
            Ped crew = null;
            foreach (var p in _patrols) if (p != null && p.Exists() && !p.IsDead) { crew = p; break; }
            if (crew != null) Ctx.Cutscenes.PlayMoment(Id, "The launches", "GOHAN", "Two launches with full tanks at the moorings. Whatever comes up through that hull, they box it in. Their fuel is the next problem.", crew);
            else Radio("GOHAN", "Two launches with full tanks at the moorings. Whatever comes up through that hull, they box it in. Their fuel is the next problem.", "M12_RADIO_01_GOHAN");
        }

        /// <summary>The aftermath: the sub back at the jetty, the three of them in one place.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_rov == null || !_rov.Exists()) return null;
            return new SceneBlocking().Then(new ShotStep(4500, _rov, new Vector3(-7f, 4f, 2.5f), _rov, new Vector3(0f, 0f, 0.5f), 1.2f));
        }

        // ---------- world building ----------

        private void SpawnRov()
        {
            var model = new Model("submersible2");
            if (!GameUtils.RequestModel(model)) return;

            _rov = Track(World.CreateVehicle(model, _jetty + new Vector3(0f, -8f, -2f), 0f));
            model.MarkAsNoLongerNeeded();
            if (_rov == null || !_rov.Exists()) return;

            _rov.IsPersistent = true;
            Function.Call(Hash.SET_VEHICLE_LIGHTS, _rov, 1);

            var blip = Track(_rov.AddBlip());
            blip.Sprite = BlipSprite.Boat;
            blip.Color = BlipColor.Green;
            blip.Name = "ROV";
        }

        private void SpawnPatrols()
        {
            var boatModel = new Model("predator");
            var crewModel = new Model("s_m_y_blackops_01");
            if (!GameUtils.RequestModel(boatModel) || !GameUtils.RequestModel(crewModel)) return;

            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");

            for (int i = 0; i < 2; i++)
            {
                var boat = Track(World.CreateVehicle(boatModel,
                    _hull + new Vector3(-30f + i * 60f, 25f, 8f), 90f));
                if (boat == null || !boat.Exists()) continue;
                boat.IsPersistent = true;
                _launches.Add(boat);

                var crew = Track(World.CreatePed(crewModel, boat.Position, 0f));
                if (crew == null || !crew.Exists()) continue;

                crew.RelationshipGroup = aegis;
                crew.IsPersistent = true;
                crew.BlockPermanentEvents = true;
                crew.Weapons.Give(WeaponHash.CarbineRifle, 150, true, true);
                crew.Task.WarpIntoVehicle(boat, VehicleSeat.Driver);
                crew.Task.StartBoatMission(boat, _buoy + new Vector3(-30f + i * 60f, 0f, 4f), VehicleMissionType.GoTo, 8f, (VehicleDrivingFlags)786603, 15f, (BoatMissionFlags)7);

                _patrols.Add(crew);

                var blip = Track(boat.AddBlip());
                blip.Sprite = BlipSprite.Boat;
                blip.Color = BlipColor.Red;
                blip.Name = "Patrol launch";
                blip.IsShortRange = true;
            }

            boatModel.MarkAsNoLongerNeeded();
            crewModel.MarkAsNoLongerNeeded();
        }

        protected override void OnCleanup()
        {
            _patrols.Clear();
            _launches.Clear();
        }
    }
}
