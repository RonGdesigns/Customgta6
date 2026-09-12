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
        protected override void OnCleanup() { Ctx.Crew.CompanionsHoldPosition = false; }

        private Vehicle _drone;
        private Vehicle _sedan;
        private Ped _dronePilot;
        private Prop _sniffer;
        private Vector3 _roof;
        private Vector3 _mast;
        private Vector3 _landing;
        private Vector3 _base;
        private Vector3 _iceStart, _chuteSpot;
        private Prop _laptop;
        private bool _heliShown, _pickupCalled, _roofFound, _chuteGiven;
        public Vector3 IceStart => _iceStart;

        public override string Id => "M07";
        public override string Title => "Wiretap Waltz";

        public Vehicle Sedan => _sedan;
        public Vehicle Drone => _drone;
        public Prop Sniffer => _sniffer;
        public Prop Laptop => _laptop;
        public bool HeliShown => _heliShown;
        public bool RoofFound => _roofFound;
        /// <summary>The roof the world has at the key stands below the estimate's height: Ice is on it, and the key wants a survey.</summary>
        public bool RoofLowerThanEstimate { get; private set; }
        public Vector3 Roof => _roof;
        public Vector3 BuildingBase => _base;

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            // The estimates carry the building; the roof's real height comes from the
            // world, and the pickup lane is the nearest street, not a wall (Ron,
            // September 10: Ice was not on the building; the sedan was against it).
            var roofKey = Ctx.Locations.Position("M07.GarageRoof");
            var mastKey = Ctx.Locations.Position("M07.MastTop");
            // Gohan at the building's base on the street, reading the feed: where Ice
            // used to stand (Ron, September 11). Ice starts on the roof itself.
            _base = StreetBase(roofKey);
            // The roof is whatever the world has under the sky at the key, and it is
            // a roof only when it stands clear of the street at the base. The old
            // check certified the estimate against its own height, so a key with no
            // roof put Ice in the air over whatever stood below it. The mission
            // always starts (Ron, September 11): a surveyed key is trusted at its own
            // height, a key with no roof takes the nearest roof, and with no roof
            // anywhere near Ice starts at street level, the key named for a survey.
            _roofFound = TryRoofTop(roofKey, _base.Z, out _roof);
            if (!_roofFound && Ctx.Locations.Get("M07.GarageRoof")?.Status == LocationStatus.Surveyed)
            {
                _roof = new Vector3(roofKey.X, roofKey.Y, roofKey.Z + 0.1f); _roofFound = true;
                Logger.Warn("M07: the probes saw no roof at the surveyed key; its own height " + roofKey.Z.ToString("0.0") + " is the roof.");
            }
            if (!_roofFound && TryRoofNear(roofKey, _base.Z, out var nearby))
            {
                _roof = nearby; _roofFound = true;
                Logger.Warn("M07: no roof at M07.GarageRoof itself (" + roofKey + "); the nearest roof " + nearby.DistanceTo(roofKey).ToString("0") + " m away at " + nearby + " is used. Survey the key from the real roof (F11) to correct it.");
                GameUtils.Notify("~y~M07.GarageRoof has no roof at its point; the nearest roof is used. Survey it (F11) to correct the key.");
            }
            if (!_roofFound)
            {
                _roof = new Vector3(roofKey.X, roofKey.Y, _base.Z);
                Logger.Error("M07: no roof under the sky at or near M07.GarageRoof (" + roofKey + "); the street at the base is at " + _base.Z.ToString("0.0") + ". Ice starts at street level. Survey the key from the real roof (F11).");
                GameUtils.Notify("~y~M07.GarageRoof has no roof near it; Ice starts at street level. Survey it from the real roof (F11).");
            }
            RoofLowerThanEstimate = _roofFound && _roof.Z < roofKey.Z - 3f;
            if (RoofLowerThanEstimate)
            {
                Logger.Warn("M07: the roof at M07.GarageRoof stands at " + _roof.Z.ToString("0.0") + ", below the estimate's " + roofKey.Z.ToString("0.0") + "; Ice starts on the real roof. Survey the roof (F11) to correct the key.");
                GameUtils.Notify("~y~M07.GarageRoof is lower than its estimate; Ice is on the real roof. Survey it (F11) to correct the key.");
            }
            // The platform is on the same roof: the mast key's own surface when it
            // has one, else the mast's spot carried onto the roof that was found.
            if (!TryRoofTop(mastKey, _base.Z, out _mast)) _mast = new Vector3(mastKey.X, mastKey.Y, _roof.Z);
            // Ice starts a few steps back from the platform, on the roof, not past its edge.
            var iceStart = _roof + new Vector3(0f, -8f, 0f);
            if (!TryRoofTop(iceStart, _base.Z, out var iceOnRoof) || iceOnRoof.Z < _roof.Z - 2f) iceOnRoof = _roof;
            var lane = World.GetNextPositionOnStreet(Ctx.Locations.Position("M07.LandingZone"));
            _landing = lane == Vector3.Zero ? Ctx.Locations.Position("M07.LandingZone") : lane;
            // Ice starts on the Maze Bank Tower roof and parachutes to the relay roof
            // (Ron, September 12); with no jump-off key he starts on the roof itself.
            var jumpOff = Ctx.Locations.Get("M07.IceStart");
            _iceStart = jumpOff != null ? jumpOff.Position : iceOnRoof;
            float iceHeading = jumpOff != null ? jumpOff.Heading : Ctx.Locations.Heading("M07.GarageRoof");

            if (!Ctx.Crew.Deploy(CrewSlot.Ice, new Dictionary<CrewSlot, PedPlacement>
            {
                [CrewSlot.Ice] = new PedPlacement(_iceStart, iceHeading),
                [CrewSlot.Gohan] = new PedPlacement(_landing + new Vector3(-2f, 0f, 0f), 0f),
                [CrewSlot.Guess] = new PedPlacement(_landing + new Vector3(2f, 0f, 0f), 0f)
            })) return false;

            ApplyBibleSetting();

            // No parachute, no mission: the exit from this roof is the jump.
            Function.Call(Hash.GIVE_WEAPON_TO_PED, Game.Player.Character,
                Game.GenerateHash("GADGET_PARACHUTE"), 1, false, false);

            SpawnSedan();
            if (!RequireAssets(_sedan)) return false;
            Ctx.Crew.CompanionsHoldPosition = true;
            // Ron and Gohan wait in the sedan they leave in (Ron, September 12): Gohan reads the feed from the passenger seat.
            Station(CrewSlot.Guess, _sedan, VehicleSeat.Driver);
            Station(CrewSlot.Gohan, _sedan, VehicleSeat.RightFront);
            PlayApproach();
            return true;
        }

        /// <summary>The street at the foot of the building the roof estimate names: the navmesh's nearest ground point below it.</summary>
        private static Vector3 StreetBase(Vector3 roofEstimate)
        {
            var street = World.GetNextPositionOnStreet(new Vector3(roofEstimate.X, roofEstimate.Y - 18f, roofEstimate.Z - 40f));
            if (street != Vector3.Zero) { var walk = World.GetSafeCoordForPed(street, true, 16); if (walk != Vector3.Zero) return walk; return street; }
            var ground = World.GetSafeCoordForPed(new Vector3(roofEstimate.X, roofEstimate.Y - 18f, roofEstimate.Z - 40f), false, 0);
            return ground != Vector3.Zero ? ground : roofEstimate + new Vector3(0f, -18f, -40f);
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("The mast",
                    new ReachZoneObjective("Ice — parachute from the tower to the marked roof and reach the antenna platform.", () => _mast, 5f))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(context => GameUtils.Subtitle("~y~Ice: the dish is on the roof below you. Jump, open the chute, land by the antenna platform (yellow marker) and clamp the sniffer to it. Gohan reads the feed from the sedan; Ron waits in the lane below the roof.", 8000))
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
            // A second chute lies by the platform (Ron, September 12): the way down is the jump to Ron's lane.
            yield return new MissionStage("Off the roof",
                    new ReachZoneObjective("Grab the chute by the platform, jump, and reach Guess's marked pickup below.", () => _landing, 25f,
                        flat: false),
                    new ReactionTrigger(() => !_heliShown && !Ctx.Cutscenes.IsActive, ShowHelicopter),
                    new ReactionTrigger(() => _heliShown && !_pickupCalled && !Ctx.Cutscenes.IsActive, CallPickup),
                    new ReactionTrigger(() => !_chuteGiven && Game.Player.Character != null && Game.Player.Character.Position.DistanceTo(_chuteSpot) < 3f, GiveSecondChute))
                .OnEnter(context => { DropSecondChute(); GameUtils.Subtitle("~r~Aegis helicopter approaching. The chute is by the platform; the pickup is below.", 5000); });

            yield return new MissionStage("Moving pickup",
                    new EnterVehicleObjective("Get in behind Guess.", () => _sedan))
                .OnExit(context =>
                {
                    // The manifests are the payout, and they set up M08.
                    GameUtils.Subtitle("~g~Manifests decrypted. Elysian warehouse, tomorrow.", 5000);
                });
        }

        /// <summary>A surface is a roof when it stands this far over the street at the building's base.</summary>
        public const float MinRoofRise = 4f;

        /// <summary>
        /// The highest surface at the point's X and Y, probed from several heights
        /// above it (a probe from far up can miss a thin roof and report the street),
        /// when it stands <see cref="MinRoofRise"/> over the street: a roof, not the
        /// street below it. False when nothing is loaded there or the only surface is
        /// the street; the point itself is never handed back as if it had been found.
        /// </summary>
        private static bool TryRoofTop(Vector3 point, float street, out Vector3 top)
        {
            top = point;
            Function.Call(Hash.REQUEST_COLLISION_AT_COORD, point.X, point.Y, point.Z);
            float best = float.MinValue;
            foreach (float rise in new[] { 3f, 15f, 40f, 120f })
            {
                var z = new OutputArgument();
                if (!Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD, point.X, point.Y, point.Z + rise, z, false, false)) continue;
                float surface = z.GetResult<float>();
                if (surface > best) best = surface;
            }
            if (best == float.MinValue || best < street + MinRoofRise) return false;
            top = new Vector3(point.X, point.Y, best + 0.1f);
            return true;
        }

        /// <summary>The nearest roof around the key, rings of ten meters out to sixty: the building the key was meant for when its point falls beside it.</summary>
        private static bool TryRoofNear(Vector3 key, float street, out Vector3 roof)
        {
            roof = key;
            for (float radius = 10f; radius <= 60f; radius += 10f)
                for (int i = 0; i < 8; i++)
                {
                    var p = key + new Vector3((float)System.Math.Cos(i * System.Math.PI / 4) * radius, (float)System.Math.Sin(i * System.Math.PI / 4) * radius, 0f);
                    if (TryRoofTop(p, street, out roof)) return true;
                }
            return false;
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

        /// <summary>A parachute by the platform for the jump down. The pickup is the game's; reaching its spot hands the chute over directly as well, so the jump never depends on the pickup streaming in.</summary>
        private void DropSecondChute()
        {
            _chuteSpot = _mast + new Vector3(2.5f, 0f, 0f);
            int pickup = 0;
            try { pickup = Function.Call<int>(Hash.CREATE_PICKUP_ROTATE, 1735599485u, _chuteSpot.X, _chuteSpot.Y, _chuteSpot.Z + 0.4f, 0f, 0f, 0f, 512, 1, 0, true, 0); }
            catch (System.Exception e) { Logger.Error("M07: the parachute pickup call failed; the chute is handed over at its spot instead.", e); }
            if (pickup == 0) Logger.Warn("M07: the parachute pickup was not created; the chute is handed over at its spot instead.");
            else Logger.Info("M07: a second chute lies at " + _chuteSpot + ".");
        }

        private void GiveSecondChute()
        {
            _chuteGiven = true;
            Function.Call(Hash.GIVE_WEAPON_TO_PED, Game.Player.Character, Game.GenerateHash("GADGET_PARACHUTE"), 1, false, false);
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
            _dronePilot.BlockPermanentEvents = true;
            _dronePilot.Task.WarpIntoVehicle(_drone, VehicleSeat.Driver);
            // An attack, not a shadow: the Buzzard works the roof with its guns (Ron, September 11).
            _dronePilot.Task.StartHeliMission(_drone, Game.Player.Character, VehicleMissionType.Attack, 30f, 40f, 30, 20, -1f, 30f, (HeliMissionFlags)0);

            var blip = Track(_drone.AddBlip());
            blip.Sprite = BlipSprite.Helicopter;
            blip.Color = BlipColor.Red;
            blip.Name = "Aegis helicopter";
        }

        /// <summary>Gohan's laptop on a case at the building's base: where the feed is read.</summary>
        private void SpawnLaptop()
        {
            var caseModel = new Model("prop_ld_case_01");
            var laptopModel = new Model("prop_laptop_01a");
            if (!GameUtils.RequestModel(caseModel) || !GameUtils.RequestModel(laptopModel)) return;
            var stand = Track(World.CreateProp(caseModel, _base + new Vector3(0.9f, 0.4f, 0f), false, true));
            caseModel.MarkAsNoLongerNeeded();
            if (stand == null || !stand.Exists()) { laptopModel.MarkAsNoLongerNeeded(); return; }
            stand.IsPersistent = true; stand.IsPositionFrozen = true;
            _laptop = Track(World.CreateProp(laptopModel, stand.Position + new Vector3(0f, 0f, 0.6f), false, false));
            laptopModel.MarkAsNoLongerNeeded();
            if (_laptop == null || !_laptop.Exists()) return;
            _laptop.IsPersistent = true;
            StowPropStep.Stow(_laptop, stand, new Vector3(0f, 0f, 0.45f));
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
