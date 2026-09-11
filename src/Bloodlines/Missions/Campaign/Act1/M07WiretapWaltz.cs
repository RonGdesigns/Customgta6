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
    /// M07 — "Wiretap Waltz". Rockford Hills, 11:30, clear.
    ///
    /// Ice puts a packet sniffer on an Aegis microwave dish and comes off the roof
    /// with a helicopter on him, into Guess's waiting sedan.
    ///
    /// Seen, not told: the relay, the roof and the pickup lane before the climb,
    /// so the return path exists before anyone goes up; the sniffer itself, clamped
    /// to the dish and left there (the dish stays live: it is how the crew reads
    /// Aegis, not something to break); the manifests read out over the clamp, with
    /// the two engines and Elysian named, so the player knows how this job found
    /// M08's cargo; the helicopter shown coming before control returns.
    ///
    /// Faked per docs/FEASIBILITY.md: there is no climbing animation set worth
    /// building a mission on, so the mast is the accessible platform on the garage
    /// roof rather than a 25-meter ascent, and the drone is a Buzzard held at
    /// altitude. The exit is the parachute or the stairs; the novel's mid-fall
    /// entry into a moving sedan is not attempted.
    /// </summary>
    public sealed class M07WiretapWaltz : ComposedMission
    {
        private Vehicle _drone;
        private Vehicle _sedan;
        private Ped _dronePilot;
        private Prop _sniffer;
        private Vector3 _roof;
        private Vector3 _mast;
        private Vector3 _landing;
        private bool _heliShown, _pickupCalled;

        public override string Id => "M07";
        public override string Title => "Wiretap Waltz";

        public Vehicle Sedan => _sedan;
        public Vehicle Drone => _drone;
        public Prop Sniffer => _sniffer;
        public bool HeliShown => _heliShown;

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            // The estimates carry the building; the roof's real height comes from the
            // world, and the pickup lane is the nearest street, not a wall (Ron,
            // September 10: Ice was not on the building; the sedan was against it).
            _roof = RoofTop(Ctx.Locations.Position("M07.GarageRoof"));
            _mast = RoofTop(Ctx.Locations.Position("M07.MastTop"));
            var lane = World.GetNextPositionOnStreet(Ctx.Locations.Position("M07.LandingZone"));
            _landing = lane == Vector3.Zero ? Ctx.Locations.Position("M07.LandingZone") : lane;

            if (!Ctx.Crew.Deploy(CrewSlot.Ice, _roof + new Vector3(0f, -8f, 0f),
                    Ctx.Locations.Heading("M07.GarageRoof")))
            {
                return false;
            }

            ApplyBibleSetting();

            // No parachute, no mission: the exit from this roof is the jump.
            Function.Call(Hash.GIVE_WEAPON_TO_PED, Game.Player.Character,
                Game.GenerateHash("GADGET_PARACHUTE"), 1, false, false);

            SpawnSedan();
            if (!RequireAssets(_sedan)) return false;
            Station(CrewSlot.Guess, _sedan, VehicleSeat.Driver);
            Station(CrewSlot.Gohan, _sedan, VehicleSeat.Passenger);
            PlayApproach();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("The mast",
                    new ReachZoneObjective("Ice — get up to the antenna platform.", () => _mast, 5f))
                .OwnedBy(CrewSlot.Ice)
                .WithCues("M07_S1_01_GOHAN");

            yield return new MissionStage("Clamp the receiver",
                    new MissionInteraction("Clamp the packet sniffer to the dish.", () => _mast, 7, 4f))
                .OwnedBy(CrewSlot.Ice)
                .OnExit(context =>
                {
                    // The manifests are a copy the crew now holds; the dish stays live.
                    Ctx.State?.SetEvidence("aegisManifests", EvidenceState.CopyHeld);
                    SpawnDrone();
                    PlayClamp();
                });

            // The helicopter is shown coming, once, before the roof is Ice's problem.
            yield return new MissionStage("Off the roof",
                    new ReachZoneObjective("Descend from the roof, then reach Guess's marked pickup. Use the parachute only if there is clearance.", () => _landing, 25f,
                        flat: false),
                    new ReactionTrigger(() => !_heliShown && !Ctx.Cutscenes.IsActive, ShowHelicopter),
                    new ReactionTrigger(() => _heliShown && !_pickupCalled && !Ctx.Cutscenes.IsActive, CallPickup))
                .OnEnter(context => GameUtils.Subtitle("~r~Aegis helicopter approaching. Reach the pickup on the ground.", 4000));

            yield return new MissionStage("Moving pickup",
                    new EnterVehicleObjective("Get in behind Guess.", () => _sedan))
                .OnExit(context =>
                {
                    // The manifests are the payout, and they set up M08.
                    GameUtils.Subtitle("~g~Manifests decrypted. Elysian warehouse, tomorrow.", 5000);
                });
        }

        /// <summary>The highest surface under the sky at the estimate's X and Y, when it is at least roughly as high as the estimate: the roof, not the street below it.</summary>
        private static Vector3 RoofTop(Vector3 estimate)
        {
            var z = new OutputArgument();
            if (Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD, estimate.X, estimate.Y, estimate.Z + 120f, z, false, false))
            {
                float top = z.GetResult<float>();
                if (top > estimate.Z - 3f) return new Vector3(estimate.X, estimate.Y, top + 0.1f);
            }
            return estimate;
        }

        // ---------- beats ----------

        /// <summary>The relay, the roof and the pickup lane before the climb: the return path exists first.</summary>
        private void PlayApproach()
        {
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var blocking = new SceneBlocking()
                .Then(new ShotStep(3200, null, _roof + new Vector3(-6f, -14f, 3f), null, _mast + new Vector3(0f, 0f, 1.5f), 0.8f))
                .Then(ice != null && ice.Exists() ? ShotStep.Watching(3000, ice, ice) : (SceneStep)new ShotStep(3000, null, _roof + new Vector3(3f, -4f, 1.7f), null, _roof, 0f))
                .Then(_sedan != null && _sedan.Exists()
                    ? new ShotStep(3200, _sedan, new Vector3(-7f, 4f, 2.2f), _sedan, new Vector3(0f, 0f, 0.8f), 1.2f)
                    : new ShotStep(3200, null, _landing + new Vector3(-7f, 4f, 2.2f), null, _landing, 0f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "The relay",
                Reason = "The dish on the garage roof, Ice below it, and Ron's sedan in the pickup lane: the relay, the job and the way back down, seen before the climb.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M07 approach scene did not play; the roof stands on its own.");
        }

        /// <summary>
        /// The sniffer on the dish, and the manifests read over it: the bible's own
        /// stage lines (the engines, Paleto) played as a scene. Skipping leaves the
        /// device clamped where it is.
        /// </summary>
        private void PlayClamp()
        {
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            SpawnSniffer();
            var blocking = new SceneBlocking();
            if (_sniffer != null && _sniffer.Exists())
                blocking.Then(new ShotStep(3600, _sniffer, new Vector3(-1.2f, -1.6f, 0.9f), _sniffer, new Vector3(0f, 0f, 0.1f), 0.3f));
            if (ice != null && ice.Exists())
                blocking.Then(ShotStep.OverShoulder(4200, ice, _sniffer != null && _sniffer.Exists() ? (Entity)_sniffer : ice, 0.2f));
            blocking.Then(new ShotStep(3200, null, _mast + new Vector3(6f, -8f, 2.5f), null, _mast + new Vector3(0f, 0f, 1f), 0.6f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "clamp", Title = "The receiver",
                Reason = "The sniffer is on the dish and the dish is still live; Gohan reads the manifests off it: two turbine engines, routed to Paleto. This is how the job found M08's cargo.",
                Blocking = blocking
            };
            var lines = new List<DialogueCue>();
            var a = Ctx.Data?.Cue("M07_S1_02_ICE"); var b = Ctx.Data?.Cue("M07_S1_03_GOHAN");
            if (a != null) lines.Add(a);
            if (b != null) lines.Add(b);
            if (!Ctx.Cutscenes.PlayStaged(spec, lines))
            {
                Logger.Warn("M07 clamp scene did not play; the lines play as dialogue.");
                blocking.Complete();
                Say("M07_S1_02_ICE"); Say("M07_S1_03_GOHAN");
            }
            GameUtils.Subtitle("~g~Two military turbine engines, routed to Paleto. That's the target.", 5000);
        }

        /// <summary>The helicopter, shown coming, once; then Ice's roof and Ron's radio.</summary>
        private void ShowHelicopter()
        {
            _heliShown = true;
            string line = Ctx.Data?.Cue("M07_S2_04_ICE")?.Line ?? "Aegis helicopter incoming. I need a way off this roof.";
            if (_dronePilot != null && _dronePilot.Exists() && _drone != null && _drone.Exists())
                Ctx.Cutscenes.PlayMoment(Id, "Aegis helicopter", "ICE", line, _dronePilot);
            else Say("M07_S2_04_ICE");
        }

        /// <summary>Ron's radio, once the helicopter has been seen: the pickup is under the roof.</summary>
        private void CallPickup()
        {
            _pickupCalled = true;
            Radio("GUESS", Ctx.Data?.Cue("M07_S2_05_GUESS")?.Line ?? "I'm underneath you, Ice. Find a safe way down and I'll be there.", "M07_S2_05_GUESS");
        }

        /// <summary>The aftermath: the sedan with the three of them, leaving.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_sedan == null || !_sedan.Exists()) return null;
            return new SceneBlocking().Then(new ShotStep(4500, _sedan, new Vector3(-3.8f, 1.2f, 1.1f), _sedan, new Vector3(0f, 0f, 0.7f), 0.8f));
        }

        // ---------- cast and props ----------

        private void SpawnSniffer()
        {
            var model = new Model("prop_ld_case_01");
            if (!GameUtils.RequestModel(model)) return;
            _sniffer = Track(World.CreateProp(model, _mast + new Vector3(0.4f, 0.4f, 0.3f), false, false));
            model.MarkAsNoLongerNeeded();
            if (_sniffer == null || !_sniffer.Exists()) return;
            _sniffer.IsPersistent = true;
            _sniffer.IsPositionFrozen = true;
        }

        private void SpawnDrone()
        {
            var model = new Model("buzzard");
            var pilotModel = new Model("s_m_y_blackops_01");
            if (!GameUtils.RequestModel(model) || !GameUtils.RequestModel(pilotModel)) return;

            // Far enough out to be seen coming: the approach is the beat.
            _drone = Track(World.CreateVehicle(model, _roof + new Vector3(0f, 140f, 45f), 180f));
            if (_drone == null || !_drone.Exists()) return;

            _dronePilot = Track(World.CreatePed(pilotModel, _drone.Position, 0f));
            model.MarkAsNoLongerNeeded();
            pilotModel.MarkAsNoLongerNeeded();
            if (_dronePilot == null || !_dronePilot.Exists()) return;

            _dronePilot.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            _dronePilot.IsPersistent = true;
            _dronePilot.Task.WarpIntoVehicle(_drone, VehicleSeat.Driver);
            _dronePilot.Task.ChaseWithHelicopter(Game.Player.Character, new Vector3(0f, 0f, 25f));

            var blip = Track(_drone.AddBlip());
            blip.Sprite = BlipSprite.Helicopter;
            blip.Color = BlipColor.Red;
            blip.Name = "Aegis helicopter";
        }

        private void SpawnSedan()
        {
            var model = new Model("schafter2");
            if (!GameUtils.RequestModel(model)) return;

            _sedan = Track(World.CreateVehicle(model, _landing, 0f));
            model.MarkAsNoLongerNeeded();
            if (_sedan == null || !_sedan.Exists()) return;

            _sedan.IsPersistent = true;
            _sedan.IsEngineRunning = true;

            var blip = Track(_sedan.AddBlip());
            blip.Sprite = BlipSprite.PersonalVehicleCar;
            blip.Color = BlipColor.Orange;
            blip.Name = "Guess";
        }
    }
}
