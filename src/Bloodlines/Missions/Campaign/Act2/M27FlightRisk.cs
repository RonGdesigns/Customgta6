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
    /// M27 — "Flight Risk". McKenzie apron to the Paleto sea, 06:00.
    ///
    /// Match the jet, transfer under an intentional cut, secure its flight ledger from
    /// a passenger seat, then parachute into the sea pickup. The Shamal has no walkable
    /// combat cabin; the ledger is an explicit interaction instead of an impossible gunfight.
    ///
    /// Seen, not told: at the airfield, Ice checking his parachute and boarding the
    /// Duster's second seat beside Ron (two seats, so no fourth pilot), the Lazer
    /// parked beside them from M26, the Shamal's track and Gohan's boat at sea before
    /// takeoff; the transfer as a staged cut from the two aircraft in formation to
    /// Ice in the Shamal's cabin; the ledger as a thing in Ice's hand; the bail-out
    /// the player's; Ron's aircraft turning for home on its own route; Ice boarding
    /// the boat with the ledger, stowed where Gohan can see it. The aftermath speaks
    /// from Ice's rescued perspective and presents the northern lead.
    /// </summary>
    public sealed class M27FlightRisk : ComposedMission
    {
        private const float BailAltitude = 80f;

        private Vehicle _stuntPlane;
        private Vehicle _lazer;
        private Vehicle _shamal;
        private Ped _shamalPilot;
        private Vehicle _dinghy;
        private Prop _ledger;

        private Vector3 _apron;
        private Vector3 _formUp;
        private Vector3 _jetTrack;
        private Vector3 _seaPickup;
        private bool _transferred, _ledgerTaken, _ronReturned, _aboard;

        public override string Id => "M27";
        public override string Title => "Flight Risk";
        protected override MissionEndpoint Endpoint => MissionEndpoint.EscapeCheckpoint;

        public Vehicle ApproachPlane => _stuntPlane;
        public Vehicle Lazer => _lazer;
        public Vehicle Shamal => _shamal;
        public Vehicle Dinghy => _dinghy;
        public Prop Ledger => _ledger;
        public bool Transferred => _transferred;
        public bool LedgerTaken => _ledgerTaken;
        public bool RonReturned => _ronReturned;
        public bool Aboard => _aboard;

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _formUp = Ctx.Locations.Position("M27.FormUp");
            _jetTrack = Ctx.Locations.Position("M27.JetTrack");
            _seaPickup = Ctx.Locations.Position("M27.SeaPickup");

            // On the McKenzie apron. The climb to eight thousand feet is the player's
            // to fly — deploying at altitude drops the crew out of the sky.
            _apron = Ctx.Locations.Position("M26.DusterPad");
            if (!Ctx.Crew.Deploy(CrewSlot.Guess, _apron, Ctx.Locations.Heading("M26.DusterPad")))
            {
                return false;
            }

            ApplyBibleSetting();

            var player = Game.Player.Character;
            player.Weapons.Give(WeaponHash.Parachute, 1, false, true);
            player.Weapons.Give(WeaponHash.SMG, 250, false, true);

            SpawnStuntPlane();
            SpawnLazer();
            SpawnShamal();
            SpawnDinghy();
            if (!RequireAssets(_stuntPlane, _shamal, _shamalPilot, _dinghy)) return false;
            RequireAsset(_dinghy, "Gohan's boat was lost. There is no pickup under the jump.");
            Ctx.Crew.CompanionsHoldPosition = true;
            Station(CrewSlot.Ice, _apron + new Vector3(-8f, 4f, 0f));
            Station(CrewSlot.Gohan, _dinghy, VehicleSeat.Driver);
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            if (ice != null && ice.Exists()) ice.Weapons.Give(WeaponHash.Parachute, 1, false, false);
            StartJet();
            PlayApproach();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Get on his rudder",
                    new EnterVehicleObjective("Guess — take the Duster up with Ice in the second seat.", () => _stuntPlane,
                        VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context => SeatIce());

            // The match is real flying: hold the band and the transfer becomes possible.
            yield return new MissionStage("Match the Shamal",
                    new ShadowTargetObjective("Climb to the Shamal and hold station inside 60 meters.",
                        () => _shamal, 60f, 12, "The Shamal outran the Duster.", 12f, acquireSeconds: 240))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context => StartJet())
                .OnExit(context => PlayTransfer())
                .WithCues("M27_S1_01_GUESS");

            yield return new MissionStage("The locker",
                    new MissionInteraction("Ice: take the flight ledger from the cabin locker", () => _shamal.Position, 4, 8f, () => _shamal))
                .OwnedBy(CrewSlot.Ice)
                .OnExit(context => { TakeLedger(); BeginDive(); ReturnRon(); })
                .WithCues("M27_S2_03_ICE");

            yield return new MissionStage("Terminal dive",
                    new BailOutObjective("The pilot put her over — get out.", BailAltitude))
                .OwnedBy(CrewSlot.Ice)
                .OnExit(context => { if (_ledger != null && _ledger.Exists()) _ledger.IsVisible = false; })
                .WithCues("M27_S2_04_GOHAN")
                .AfterCues("M27_S2_05_ICE");

            yield return new MissionStage("Sea pickup",
                    new EnterVehicleObjective("Ice: parachute to the green boat marker, then climb aboard Gohan's dinghy.", () => _dinghy))
                .OwnedBy(CrewSlot.Ice)
                .OnExit(context => BoardWithLedger())
                .AfterCues("M27_S2_06_GUESS");
        }

        // ---------- beats ----------

        /// <summary>At the airfield: Ice checks his parachute and boards the second seat, the Lazer parked beside them, the Shamal's track and Gohan's boat at sea. No fourth pilot.</summary>
        private void PlayApproach()
        {
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var blocking = new SceneBlocking();
            if (_lazer != null && _lazer.Exists() && _stuntPlane != null && _stuntPlane.Exists()) blocking.Then(new ShotStep(2800, _lazer, new Vector3(-8f, 6f, 2.2f), _stuntPlane, new Vector3(0f, 0f, 0.8f), 0.8f));
            if (ice != null && ice.Exists() && _stuntPlane != null && _stuntPlane.Exists())
            {
                blocking.Then(new InspectStep(ice, _stuntPlane.Position + new Vector3(-3f, -2f, 0f), 2600, "WORLD_HUMAN_CLIPBOARD"));
                blocking.Then(new EnterVehicleStep(ice, _stuntPlane, VehicleSeat.Passenger));
            }
            blocking.Then(ShotStep.Wide(2600, _jetTrack, 80f, 20f, 30f));
            if (_dinghy != null && _dinghy.Exists()) blocking.Then(new ShotStep(2800, _dinghy, new Vector3(-9f, 6f, 3f), _dinghy, new Vector3(0f, 0f, 0.6f), 0.9f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "The apron",
                Reason = "The Lazer parked from the scramble; beside it the Duster, two seats. Ice checks his parachute and takes the second seat; Ron flies. The Shamal's track over Chiliad, and Gohan already at sea in the boat under it: the pickup exists before the jump does.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) { Logger.Warn("M27 approach scene did not play; Ice takes his seat directly."); SeatIce(); }
        }

        /// <summary>Ice in the Duster's second seat, however the approach ended.</summary>
        private void SeatIce()
        {
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            if (ice == null || !ice.Exists() || _stuntPlane == null || !_stuntPlane.Exists() || ice.IsInVehicle(_stuntPlane)) return;
            Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Ice);
            ice.Task.ClearAllImmediately();
            ice.SetIntoVehicle(_stuntPlane, VehicleSeat.Passenger);
        }

        /// <summary>
        /// The transfer as an intentional cut: the two aircraft in formation, a fade,
        /// and Ice in the Shamal's cabin with the camera on him. The game never has
        /// to solve two aircraft in relative motion, which is the one thing it cannot
        /// do here; the cut conceals only that.
        /// </summary>
        private void PlayTransfer()
        {
            _transferred = true;
            var blocking = new SceneBlocking { DialogueAfterStep = 2 };
            if (_stuntPlane != null && _stuntPlane.Exists() && _shamal != null && _shamal.Exists())
                blocking.Then(new ShotStep(2800, _stuntPlane, new Vector3(-6f, 4f, 1.5f), _shamal, new Vector3(0f, 0f, 0.5f), 0.6f));
            blocking.Then(new ShotStep(500, null, Game.Player.Character.Position + new Vector3(0f, -4f, 1.5f), null, Game.Player.Character.Position, 0f, () => GameUtils.FadeOut(400)));
            blocking.Then(new WaitStep(450));
            if (_shamal != null && _shamal.Exists())
                blocking.Then(new ShotStep(3400, _shamal, new Vector3(-3f, 1.2f, 1.2f), _shamal, new Vector3(1.5f, 0f, 0.6f), 0.3f, BoardTheJet));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "transfer", Title = "The wing",
                Reason = "The Duster on the Shamal's wing, close enough; a cut; Ice in the Shamal's cabin with the locker in front of him. The illusion is only the mechanical crossing. Ron peels off with an empty second seat.",
                Blocking = blocking
            };
            var cue = Ctx.Data?.Cue("M27_S1_02_ICE");
            if (!Ctx.Cutscenes.PlayStaged(spec, new[] { cue })) { Logger.Warn("M27 transfer scene did not play; the transfer runs directly."); blocking.Complete(); Say("M27_S1_02_ICE"); }
            GameUtils.Subtitle("Ice is aboard. Press E / D-pad Right to secure the ledger.", 5000);
        }

        /// <summary>The state after the cut: Ice in a real Shamal seat, the player, with a parachute; the fade lifted whatever happened.</summary>
        private void BoardTheJet()
        {
            if (_shamal == null || !_shamal.Exists()) { GameUtils.FadeIn(500); return; }
            try
            {
                var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
                if (ice == null || !ice.Exists()) throw new System.InvalidOperationException("Ice is unavailable.");
                Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Ice);
                ice.Task.ClearAllImmediately(); ice.SetIntoVehicle(_shamal, (VehicleSeat)2);
                if (Ctx.Crew.ActiveSlot != CrewSlot.Ice && !Ctx.Switching.TrySwitch(CrewSlot.Ice, missionTransition: true))
                    throw new System.InvalidOperationException("Could not switch to Ice for the transfer.");
                ice.SetIntoVehicle(_shamal, (VehicleSeat)2);
                ice.Weapons.Give(WeaponHash.Parachute, 1, false, false);
                if (!ice.IsInVehicle(_shamal)) throw new System.InvalidOperationException("The aircraft transfer failed.");
            }
            finally { GameUtils.FadeIn(600); }
        }

        /// <summary>The ledger as a thing: a flight case out of the locker and into Ice's hand, recorded as evidence held.</summary>
        private void TakeLedger()
        {
            _ledgerTaken = true;
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var model = new Model("prop_ld_case_01");
            if (ice != null && ice.Exists() && GameUtils.RequestModel(model))
            {
                _ledger = Track(World.CreateProp(model, ice.Position + new Vector3(0f, 0f, 1f), false, false));
                model.MarkAsNoLongerNeeded();
                if (_ledger != null && _ledger.Exists()) { _ledger.IsPersistent = true; CarryPropStep.Attach(ice, _ledger, new Vector3(0.12f, 0.02f, -0.02f), new Vector3(0f, 90f, 0f)); }
            }
            Ctx.State?.SetEvidence("flightLedger", EvidenceState.CopyHeld);
            GameUtils.Subtitle("~g~The flight ledger: six months of Aegis charters, in hand.", 4000);
        }

        /// <summary>The pilot kicks the stick forward — a scripted attitude, not a hope.</summary>
        private void BeginDive()
        {
            if (_shamal == null || !_shamal.Exists()) return;

            if (_shamalPilot != null && _shamalPilot.Exists()) _shamalPilot.Kill();

            _shamal.Heading = (_seaPickup - _shamal.Position).ToHeading();
            _shamal.Rotation = new Vector3(-20f, _shamal.Rotation.Y, _shamal.Rotation.Z);
            _shamal.Speed = 90f;
            GameUtils.Subtitle("~r~She's over. Terminal dive toward the Pacific.", 5000);
        }

        /// <summary>Ron's aircraft returns by its own route: the Duster, empty second seat, tasked home to McKenzie.</summary>
        private void ReturnRon()
        {
            _ronReturned = true;
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            if (guess == null || !guess.Exists() || _stuntPlane == null || !_stuntPlane.Exists()) return;
            if (!guess.IsInVehicle(_stuntPlane)) guess.SetIntoVehicle(_stuntPlane, VehicleSeat.Driver);
            Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Guess);
            guess.Task.StartPlaneMission(_stuntPlane, _apron + new Vector3(0f, 0f, 80f), VehicleMissionType.GoTo, 40f, 60f, 80, 40, 0f, false);
            Radio("GUESS", "Peeling off. Empty seat beside me and the Duster's going home to McKenzie on her own route. Gohan has you from here.", "M27_RADIO_01_GUESS");
        }

        /// <summary>Ice in the boat with the ledger, stowed where Gohan can see it.</summary>
        private void BoardWithLedger()
        {
            _aboard = true;
            if (_ledger != null && _ledger.Exists() && _dinghy != null && _dinghy.Exists())
            {
                _ledger.IsVisible = true;
                StowPropStep.Stow(_ledger, _dinghy, new Vector3(0f, 0.6f, 0.7f));
            }
            /* Awarded once by CampaignState.MarkComplete after the mission passes. */
            GameUtils.Subtitle("~g~Flight ledgers secured. Every Aegis charter for six months, in the boat.", 6000);
        }

        /// <summary>The aftermath: the boat with the two of them and the case, the jet gone.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_dinghy == null || !_dinghy.Exists()) return null;
            return new SceneBlocking().Then(new ShotStep(4500, _dinghy, new Vector3(-8f, 5f, 2.5f), _dinghy, new Vector3(0f, 0f, 0.7f), 1.0f));
        }

        private void StartJet()
        {
            if (_shamalPilot == null || !_shamalPilot.Exists() || _shamal == null || !_shamal.Exists()) return;

            _shamalPilot.Task.StartPlaneMission(_shamal, _seaPickup + new Vector3(0f, 0f, 700f), VehicleMissionType.Circle,
                42f, 180f, 700, 40, 0f, false);
        }

        // ---------- world building ----------

        /// <summary>The Duster: two seats and a radial engine, the aircraft parked beside the Lazer at the end of M26. Taken over when it is still there.</summary>
        private void SpawnStuntPlane()
        {
            var model = new Model("duster");
            if (!GameUtils.RequestModel(model)) return;

            var spot = _apron + new Vector3(24f, 0f, 0f);
            var existing = PortHeist.Nearby(model, spot, 30f);
            _stuntPlane = Track(existing ?? World.CreateVehicle(model, spot, Ctx.Locations.Heading("M26.DusterPad")));
            model.MarkAsNoLongerNeeded();
            if (_stuntPlane == null || !_stuntPlane.Exists()) return;

            _stuntPlane.IsPersistent = true;
            _stuntPlane.IsEngineRunning = true;

            var blip = Track(_stuntPlane.AddBlip());
            blip.Sprite = BlipSprite.Plane;
            blip.Color = BlipColor.Orange;
            blip.Name = "Duster";
        }

        /// <summary>The Lazer where M26 parked it: seen, engine off, not flown today.</summary>
        private void SpawnLazer()
        {
            if (Ctx.State?.CargoAt("lazer") != "M26.DusterPad") return;
            var model = new Model("lazer");
            if (!GameUtils.RequestModel(model)) return;
            var existing = PortHeist.Nearby(model, _apron, 30f);
            _lazer = Track(existing ?? World.CreateVehicle(model, _apron + new Vector3(-6f, 0f, 0f), Ctx.Locations.Heading("M26.DusterPad")));
            model.MarkAsNoLongerNeeded();
            if (_lazer == null || !_lazer.Exists()) return;
            _lazer.IsPersistent = true;
            _lazer.IsEngineRunning = false;
        }

        private void SpawnShamal()
        {
            var model = new Model("shamal");
            var pilotModel = new Model("s_m_m_pilot_01");
            if (!GameUtils.RequestModel(model) || !GameUtils.RequestModel(pilotModel)) return;

            _shamal = Track(World.CreateVehicle(model, _formUp, 250f));
            if (_shamal == null || !_shamal.Exists()) return;

            _shamal.IsPersistent = true;
            _shamal.IsEngineRunning = true;
            _shamal.ForwardSpeed = 45f;

            _shamalPilot = Track(World.CreatePed(pilotModel, _shamal.Position, 0f));
            model.MarkAsNoLongerNeeded();
            pilotModel.MarkAsNoLongerNeeded();
            if (_shamalPilot == null || !_shamalPilot.Exists()) return;

            _shamalPilot.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            _shamalPilot.IsPersistent = true;
            _shamalPilot.BlockPermanentEvents = true;
            _shamalPilot.Task.WarpIntoVehicle(_shamal, VehicleSeat.Driver);

            var blip = Track(_shamal.AddBlip());
            blip.Sprite = BlipSprite.Plane;
            blip.Color = BlipColor.Red;
            blip.Name = "Aegis Shamal";
        }

        private void SpawnDinghy()
        {
            var model = new Model("dinghy");
            if (!GameUtils.RequestModel(model)) return;

            _dinghy = Track(World.CreateVehicle(model, _seaPickup, 200f));
            model.MarkAsNoLongerNeeded();
            if (_dinghy == null || !_dinghy.Exists()) return;

            _dinghy.IsPersistent = true;
            _dinghy.IsEngineRunning = true;

            var blip = Track(_dinghy.AddBlip());
            blip.Sprite = BlipSprite.Boat;
            blip.Color = BlipColor.Green;
            blip.Name = "Gohan";
        }

        protected override void OnPassed()
        {
            if (_dinghy != null && _dinghy.Exists()) Release(_dinghy);
            if (_ledger != null && _ledger.Exists()) Release(_ledger);
            if (_lazer != null && _lazer.Exists()) Release(_lazer);
        }

        protected override void OnCleanup()
        {
            Ctx.Crew.CompanionsHoldPosition = false;
            GameUtils.FadeIn(500);
        }
    }

    /// <summary>
    /// Get out of a falling aircraft before it hits. Completes when the player is out
    /// and under canopy; fails if they ride it down past the floor.
    /// </summary>
    internal sealed class BailOutObjective : Objective
    {
        private readonly float _floor;

        public BailOutObjective(string label, float floor) : base(label)
        {
            _floor = floor;
        }

        public override void Update(MissionContext context)
        {
            var player = Game.Player.Character;
            if (player == null || !player.Exists() || !IsOwnerActive(context)) return;

            float height = player.HeightAboveGround;

            if (!player.IsInVehicle())
            {
                Complete();
                return;
            }

            GameUtils.Subtitle("~r~" + (int)height + " m — exit the aircraft and deploy your parachute", 400);

            if (height < _floor) Fail("Ice rode the jet into the water.");
        }
    }
}
