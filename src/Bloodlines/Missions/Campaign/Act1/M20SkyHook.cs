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
    /// M20 — "The Port Heist: Sky Hook". Port outer basin, 03:20, under AA fire.
    ///
    /// Part two, and the campaign's first Red-tier set piece: thirty tons of bullion
    /// lifted out of the water by a Cargobob while Aegis gunners work the freighter
    /// deck.
    ///
    /// Seen, not told: a short live handoff showing the surfaced load and the actual
    /// gunner positions before Ron lifts off; the jammer pod from M14 and M18 given
    /// its one job, switched on from the channel for the window of the lift; the
    /// hook as an insert that changes the flight profile; Ice and Gohan reaching the
    /// escort launch with actual seats, on camera, before M21 starts; the Kraken's
    /// storage plan said once. Shore cover stays the selected plan: Ice suppresses
    /// from the pier, not from a door gun.
    ///
    /// Faked exactly as docs/FEASIBILITY.md prescribes — attach, do not simulate. The
    /// container is attached to the helicopter rather than slung on a rope with a
    /// weight the physics would have to solve; the hover is a hold, the lift is an
    /// attach, and the climb-out is a flight the player actually flies. Everything
    /// the player controls is real. The only thing that is faked is the one thing the
    /// engine cannot do.
    /// </summary>
    public sealed class M20SkyHook : ComposedMission
    {
        private readonly List<Ped> _gunners = new List<Ped>();

        private Vehicle _cargobob;
        private Vehicle _kraken;
        private Vehicle _launch;
        private Prop _container;
        private Vector3 _hover;
        private Vector3 _deck;
        private Vector3 _climbOut;
        private Vector3 _launchMark;
        private bool _followingRecord, _podLive, _podCalled, _hooked, _transferred;

        public override string Id => "M20";
        public override string Title => "The Port Heist: Sky Hook";
        protected override MissionEndpoint Endpoint => MissionEndpoint.ContinuousNext;

        public Vehicle Cargobob => _cargobob;
        public Vehicle Kraken => _kraken;
        public Vehicle Launch => _launch;
        public Prop Container => _container;
        public bool FollowingRecord => _followingRecord;
        public bool PodLive => _podLive;
        public bool PodCalled => _podCalled;
        public bool Hooked => _hooked;
        public bool Transferred => _transferred;

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _hover = Ctx.Locations.Position("M20.HoverPoint");
            _deck = Ctx.Locations.Position("M20.DeckGunners");
            _climbOut = Ctx.Locations.Position("M20.ClimbOut");
            _launchMark = MarineSites.ResolveOrThrow(Ctx.Locations, "M21.LaunchSpawn", 3f, 3f, 6f);

            // Start on the ground at the crew's own staging hangar from M18: a
            // deployment over open water drops three people into the harbor.
            var apron = Ctx.Locations.Position("M18.SaltHangar");
            if (!PortHeist.IsContinuing(Ctx) && !Ctx.Crew.Deploy(CrewSlot.Guess, apron, Ctx.Locations.Heading("M18.SaltHangar")))
            {
                return false;
            }

            ApplyBibleSetting();
            Ctx.Crew.PedFor(CrewSlot.Guess).Weapons.Give(WeaponHash.MG, 400, false, true);

            // Chapter continuity: if M19 just ended, Gohan is still in the Kraken on
            // the surface, the container is floating beside the mark, and the lift is
            // the aircraft Ron was sitting in.
            var handoff = Ctx.Handoffs.Take(PortHeist.Operation, Id);
            _followingRecord = handoff != null || PortHeist.CargoAt(Ctx, PortHeist.BullionCargo) == "M19.Surface";
            _podLive = PortHeist.CargoAt(Ctx, "radarPod") == "M18.SaltHangar";

            SpawnCargobob(apron);
            SpawnContainer();
            SpawnDeckGunners();
            SpawnLaunch();
            if (!RequireAssets(_cargobob, _container, _launch)) return false;
            Ctx.PortHeist?.Bind("lift", _cargobob);
            Ctx.PortHeist?.Bind("bullion", _container);
            Ctx.PortHeist?.Bind("launch", _launch);
            RequireAsset(_cargobob, "The Cargobob went down.");
            RequireAsset(_container, "The bullion container was lost.");
            Ctx.Crew.CompanionsHoldPosition = true;
            var pier = Ctx.Locations.Position("M12.PierWatch");
            if (handoff != null && handoff.Positions.TryGetValue(CrewSlot.Ice, out var icePost)) pier = icePost;
            if (!PortHeist.IsContinuing(Ctx)) Station(CrewSlot.Ice, pier);
            else Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Ice);
            if (!SpawnKraken(handoff)) return false;
            Ctx.PortHeist?.Bind("kraken", _kraken);
            if (!PortHeist.IsContinuing(Ctx)) Station(CrewSlot.Gohan, _kraken, VehicleSeat.Driver);
            else if (!PortHeistWorld.Seated(Ctx.Crew.PedFor(CrewSlot.Gohan), _kraken, VehicleSeat.Driver))
                throw new System.InvalidOperationException("Gohan did not surface in the operation's Kraken.");
            Ctx.Crew.PedFor(CrewSlot.Ice).Weapons.Give(WeaponHash.HeavySniper, 100, true, true);
            PlayApproach();
            return true;
        }

        private bool SpawnKraken(OperationHandoff handoff)
        {
            if (PortHeist.IsContinuing(Ctx)) { _kraken = Track(Ctx.PortHeist.Require<Vehicle>("kraken")); return true; }
            var model = new Model("submersible2");
            if (!GameUtils.RequestModel(model)) return false;
            var point = handoff != null && handoff.VehicleModel.Length > 0 ? handoff.VehiclePosition : Ctx.Locations.Position("M19.Surface");
            // In a continuous run the Kraken chapter one released is still floating
            // there; take it over rather than spawning a second one beside it.
            _kraken = PortHeist.Nearby(model, point, 25f);
            if (_kraken != null) _kraken = Track(_kraken);
            if (_kraken == null) _kraken = Track(World.CreateVehicle(model, point, handoff?.VehicleHeading ?? 180f));
            model.MarkAsNoLongerNeeded();
            if (_kraken == null || !_kraken.Exists()) return false;
            _kraken.IsPersistent = true;
            return true;
        }

        /// <summary>The lift is airborne with the container under it and the escort is crewed: that is what M21 must show.</summary>
        protected override void OnPassed()
        {
            var record = OperationHandoff.Capture(PortHeist.Operation, Id, "M21", Ctx.Crew, _cargobob);
            record.CargoAttached = PortHeistWorld.Attached(_container, _cargobob);
            record.CargoModel = "prop_container_01a";
            record.Notes["launch"] = _transferred ? "Gohan at the helm, Ice in the passenger seat, at M21.LaunchSpawn" : "not boarded on camera";
            record.Notes["kraken"] = "tied off at the pier; retrieved after the operation";
            record.Notes["radarPod"] = _podLive ? "live for the lift window" : "not fitted";
            Ctx.Handoffs.Record(record);
            PortHeist.RecordCargo(Ctx, "kraken", "M12.PierWatch");
            // The launch and the sub are the next chapter's; the lift flies on.
            if (_launch != null && _launch.Exists()) Release(_launch);
            if (_kraken != null && _kraken.Exists()) Release(_kraken);
            // The container stays under the aircraft through the aftermath and into
            // M21, which re-attaches the same prop rather than building a second one.
            if (_container != null && _container.Exists()) Release(_container);
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Get on the water",
                    new EnterVehicleObjective("Guess — take the Cargobob over the basin.",
                        () => _cargobob, VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context => { SwitchOnPod(); MoveKrakenToPier(); });

            // Ice's job while Guess holds the hover: the two objectives are the two
            // characters, running at once, which is the whole argument for the switch.
            yield return new MissionStage("Suppress the deck",
                    new KillTargetsObjective("Ice — clear the marked quayside gunners from the pier.",
                        () => _gunners),
                    new ProtectObjective("", () => _cargobob, "The Cargobob went down."))
                .OwnedBy(CrewSlot.Ice)
                .AfterCues("M20_S1_02_ICE");

            // The pilot aligns the aircraft; the hook is an insert the hover earns.
            yield return new MissionStage("Lock the cable",
                    new MissionInteraction("Guess — hold the hover over the container.", () => _hover, 8, 14f, () => _cargobob),
                    new ProtectObjective("", () => _cargobob, "The Cargobob went down."))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context => PlayHook());

            yield return new MissionStage("Climb out",
                    new DeliverVehicleObjective("Guess: climb in the Cargobob to the elevated yellow marker.", () => _cargobob, () => _climbOut, 30f),
                    new ProtectObjective("", () => _cargobob, "The Cargobob went down."))
                .OnExit(context => PlayTransfer());
        }

        // ---------- beats ----------

        /// <summary>The live handoff: the container on the water with its floats, the gunners where they stand, Ice on the pier, the lift on the apron.</summary>
        private void PlayApproach()
        {
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var blocking = new SceneBlocking();
            if (_container != null && _container.Exists()) blocking.Then(new ShotStep(3200, _container, new Vector3(-12f, 8f, 4f), _container, new Vector3(0f, 0f, 0.8f), 0.9f));
            if (_gunners.Count > 0 && _gunners[0].Exists()) blocking.Then(ShotStep.Watching(2800, _gunners[0], _gunners[0]));
            else blocking.Then(ShotStep.Wide(2800, _deck, 14f, 6f, 6f));
            if (ice != null && ice.Exists()) blocking.Then(ShotStep.Watching(2600, ice, ice));
            if (_cargobob != null && _cargobob.Exists()) blocking.Then(new ShotStep(2800, _cargobob, new Vector3(-14f, 7f, 3.5f), _cargobob, new Vector3(0f, 0f, 1.5f), 1.0f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "The load on the water",
                Reason = "The container floating on its clamps beside the mark, five gunners on the quay with the rotor disc in their sights, Ice on the pier with the rifle, the lift on the apron with the pod on it. Shore cover, not a door gun: Ice clears the quay, Ron flies.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M20 approach scene did not play; the apron stands on its own.");
        }

        /// <summary>The pod's one job: switched on from the channel as the lift takes off, a hole in Aegis radar for the window of the lift. Or no pod, and the whole flight seen.</summary>
        private void SwitchOnPod()
        {
            _podCalled = true;
            if (_podLive)
            {
                Radio("GOHAN", "Pod's live from the channel. Their radar has a hole over the basin for as long as the lift takes. Not a blind Aegis: a window.", "M20_RADIO_01_GOHAN");
                GameUtils.Subtitle("~g~Jammer pod active: no radar lock over the basin for the lift window.", 5000);
            }
            else
            {
                Radio("GOHAN", "No pod on the lift. Their radar sees you the whole way. Stay low over the water and make it quick.", "M20_RADIO_01_GOHAN");
                GameUtils.Subtitle("~y~No jammer pod fitted: Aegis radar sees the lift the whole way.", 5000);
            }
        }

        /// <summary>Gohan brings the Kraken to the pier while Ron flies: the move to the escort launch starts as a task, so the transfer later is a step and not a teleport.</summary>
        private void MoveKrakenToPier()
        {
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            if (gohan == null || !gohan.Exists() || _kraken == null || !_kraken.Exists() || !gohan.IsInVehicle(_kraken)) return;
            gohan.Task.StartBoatMission(_kraken, _launchMark + new Vector3(-12f, 0f, 0f), VehicleMissionType.GoTo, 8f, (VehicleDrivingFlags)786603, 6f, (BoatMissionFlags)7);
        }

        /// <summary>
        /// The lift. Attaching the container to the aircraft is the whole trick: a
        /// thirty-ton slung load is a physics problem the game will lose, and an
        /// attached prop under the fuselage is one it always wins. The insert shows
        /// the cable take the strain; the aircraft flies heavy from here.
        /// </summary>
        private void PlayHook()
        {
            AttachContainer();
            if (!PortHeistWorld.Attached(_container, _cargobob)) throw new System.InvalidOperationException("The cable did not secure the bullion.");
            _hooked = true;
            var blocking = new SceneBlocking();
            if (_cargobob != null && _cargobob.Exists() && _container != null && _container.Exists())
                blocking.Then(new ShotStep(3600, _cargobob, new Vector3(-9f, -5f, 1.5f), _container, new Vector3(0f, 0f, 1f), 0.5f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "hook", Title = "The hook",
                Reason = "The cable locks on the container and the aircraft takes the weight: the flight profile changes here, seen from the cabin. The strain is authored presentation, not a simulated mass.",
                Blocking = blocking
            };
            var cue = Ctx.Data?.Cue("M20_S1_01_GUESS");
            if (!Ctx.Cutscenes.PlayStaged(spec, new[] { cue })) { Logger.Warn("M20 hook scene did not play; the line plays as dialogue."); blocking.Complete(); Say("M20_S1_01_GUESS"); }
        }

        private void AttachContainer()
        {
            if (_container == null || !_container.Exists() || _cargobob == null || !_cargobob.Exists()) return;

            _container.IsPositionFrozen = false;
            Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY, _container, _cargobob, 0,
                0f, 0f, -6.5f, 0f, 0f, 0f, false, false, true, false, 2, true);

            // The aircraft flies heavy from here — the weight is a handling change, not
            // a simulated load.
            _cargobob.EnginePowerMultiplier = 0.55f;
            GameUtils.Subtitle("~y~Cable locked. She's heavy — nurse her.", 5000);
        }

        /// <summary>Ice and Gohan reach the escort launch with actual seats, on camera, and the Kraken's storage plan is said once. M21 starts from those seats.</summary>
        private void PlayTransfer()
        {
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var blocking = new SceneBlocking();
            if (_launch != null && _launch.Exists())
            {
                if (gohan != null && gohan.Exists()) { if (gohan.IsInVehicle()) blocking.Then(new ExitVehicleStep(gohan)); blocking.Then(new EnterVehicleStep(gohan, _launch, VehicleSeat.Driver)); }
                if (ice != null && ice.Exists()) blocking.Then(new EnterVehicleStep(ice, _launch, VehicleSeat.Passenger));
                blocking.Then(new ShotStep(3200, _launch, new Vector3(-6f, 4f, 2f), _launch, new Vector3(0f, 0f, 0.8f), 0.7f));
            }
            if (_cargobob != null && _cargobob.Exists()) blocking.Then(new ShotStep(3000, _cargobob, new Vector3(-16f, 10f, 4f), _cargobob, new Vector3(0f, 0f, -3f), 1.0f));
            blocking.Then(new VerifySceneStep("The escort launch is crewed", () =>
                PortHeistWorld.Seated(gohan, _launch, VehicleSeat.Driver) &&
                PortHeistWorld.Seated(ice, _launch, VehicleSeat.Passenger), () => _transferred = true));
            var spec = new SceneSpec
            {
                RequiresCompletion = true,
                MissionId = Id, Phase = "transfer", Title = "The escort",
                Reason = "Gohan out of the Kraken and into the launch at the helm, Ice down from the pier into the passenger seat: the escort crewed with the seats the boat has. The Kraken stays tied at the pier until the operation is over. The lift is airborne with the container under it.",
                Blocking = blocking
            };
            var lines = new[]
            {
                Ctx.Data?.Cue("M20_S1_03_GUESS"),
                new DialogueCue { CueId = "M20_RADIO_02_GOHAN", MissionId = Id, Speaker = "GOHAN", Line = "Kraken's tied off at the pier; she stays until this is over. I'm on the launch, Ice is on the gun. We're under you from here." },
                new DialogueCue { CueId = "M20_RADIO_03_ICE", MissionId = Id, Speaker = "ICE", Line = "Two seats, two of us. Fly the water route and don't look back for us; that's what the boat is for." }
            };
            if (!Ctx.Cutscenes.PlayStaged(spec, lines))
            {
                Logger.Warn("M20 transfer scene did not play; the seats are taken directly.");
                PortHeist.RequireFallback(blocking, "Boarding the escort launch");
                Say("M20_S1_03_GUESS");
            }
            GameUtils.Subtitle("~g~Thirty tons airborne. Gohan and Ice are on the launch; the Kraken stays at the pier.", 5000);
        }

        /// <summary>The aftermath: the lift airborne with the load, the launch on the water under it.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_cargobob == null || !_cargobob.Exists()) return null;
            return new SceneBlocking().Then(new ShotStep(4500, _cargobob, new Vector3(-20f, 12f, 3f), _cargobob, new Vector3(0f, 0f, -4f), 1.4f));
        }

        // ---------- world building ----------

        /// <summary>The lift M18 parked and M19 showed Ron sitting in, taken over; or one on the apron where it would be.</summary>
        private void SpawnCargobob(Vector3 apron)
        {
            if (PortHeist.IsContinuing(Ctx)) { _cargobob = Track(Ctx.PortHeist.Require<Vehicle>("lift")); return; }
            var model = new Model("cargobob");
            if (!GameUtils.RequestModel(model)) return;

            var existing = PortHeist.Nearby(model, apron, 60f);
            _cargobob = Track(existing ?? World.CreateVehicle(model, apron + new Vector3(18f, 0f, 0f), Ctx.Locations.Heading("M18.SaltHangar")));
            model.MarkAsNoLongerNeeded();
            if (_cargobob == null || !_cargobob.Exists()) return;

            _cargobob.IsPersistent = true;
            _cargobob.EnginePowerMultiplier = 1f;
            Logger.Info("M20: Cargobob " + (existing != null ? "taken over on the apron" : "spawned on the apron") + ".");

            var blip = Track(_cargobob.AddBlip());
            blip.Sprite = BlipSprite.Helicopter;
            blip.Color = BlipColor.Orange;
            blip.Name = "Cargobob";
        }

        /// <summary>The surfaced load where M19 floated it (the same prop when it is still there), or the authored hover point's water on a cold start.</summary>
        private void SpawnContainer()
        {
            if (PortHeist.IsContinuing(Ctx))
            {
                _container = Track(Ctx.PortHeist.Require<Prop>("bullion"));
                _hover = new Vector3(_container.Position.X, _container.Position.Y, _hover.Z);
                return;
            }
            var model = new Model("prop_container_01a");
            if (!GameUtils.RequestModel(model)) return;

            var point = _followingRecord ? PortHeist.ContainerPoint(Ctx.Locations) + new Vector3(0f, 0f, 0.6f) : _hover - new Vector3(0f, 0f, 34f);
            if (_followingRecord) _hover = new Vector3(point.X, point.Y, _hover.Z);
            var existing = PortHeist.NearbyProp(model, point, 20f);
            _container = Track(existing ?? World.CreateProp(model, point, false, false));
            model.MarkAsNoLongerNeeded();
            if (_container == null || !_container.Exists()) return;

            _container.IsPersistent = true;
            _container.IsPositionFrozen = true;
            Logger.Info("M20: container " + (existing != null ? "taken over" : "placed") + (_followingRecord ? " beside M19.Surface as M19 left it." : " at the hover point (cold start)."));

            var blip = Track(_container.AddBlip());
            blip.Sprite = BlipSprite.Standard;
            blip.Color = BlipColor.Yellow;
            blip.Name = "Bullion container";
        }

        private void SpawnDeckGunners()
        {
            var model = new Model("s_m_y_blackops_01");
            if (!GameUtils.RequestModel(model)) return;

            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");

            for (int i = 0; i < 5; i++)
            {
                var gunner = World.CreatePed(model, _deck + new Vector3(-10f + i * 5f, 0f, 0f), 180f);
                if (gunner == null || !gunner.Exists()) continue;

                gunner.RelationshipGroup = aegis;
                gunner.IsPersistent = true;
                gunner.BlockPermanentEvents = true;
                gunner.Accuracy = 40;
                gunner.Armor = 60;
                gunner.Weapons.Give(WeaponHash.MG, 300, true, true);
                gunner.Task.FightAgainstHatedTargets(200f);

                _gunners.Add(Track(gunner));
            }

            model.MarkAsNoLongerNeeded();
        }

        /// <summary>The escort launch at its mark by the pier, with the seats the transfer needs. M21 takes this boat over.</summary>
        private void SpawnLaunch()
        {
            var model = new Model("dinghy4");
            if (!GameUtils.RequestModel(model)) return;
            var existing = PortHeist.Nearby(model, _launchMark, 40f);
            _launch = Track(existing ?? World.CreateVehicle(model, _launchMark, Ctx.Locations.Heading("M21.LaunchSpawn")));
            model.MarkAsNoLongerNeeded();
            if (_launch == null || !_launch.Exists()) return;
            _launch.IsPersistent = true;
            _launch.IsEngineRunning = false;
        }

        protected override void OnCleanup()
        {
            Ctx.Crew.CompanionsHoldPosition = false;
            // A passed chapter hands the attached container on; only an abandoned
            // attempt drops it back into the water.
            if (Status != MissionStatus.Passed && _container != null && _container.Exists())
            {
                Function.Call(Hash.DETACH_ENTITY, _container, true, true);
            }

            _gunners.Clear();
        }
    }
}
