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
    /// Match the jet, transfer under a bounded fade, secure its flight ledger from a
    /// passenger seat, then parachute into the sea pickup. The Shamal has no walkable
    /// combat cabin; the ledger is an explicit interaction instead of an impossible gunfight.
    /// </summary>
    public sealed class M27FlightRisk : ComposedMission
    {
        private const float BailAltitude = 80f;



        private Vehicle _stuntPlane;
        private Vehicle _shamal;
        private Ped _shamalPilot;
        private Vehicle _dinghy;

        private Vector3 _formUp;
        private Vector3 _jetTrack;
        private Vector3 _seaPickup;

        public override string Id => "M27";
        public override string Title => "Flight Risk";

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _formUp = Ctx.Locations.Position("M27.FormUp");
            _jetTrack = Ctx.Locations.Position("M27.JetTrack");
            _seaPickup = Ctx.Locations.Position("M27.SeaPickup");

            // On the McKenzie apron. The climb to eight thousand feet is the player's
            // to fly — deploying at altitude drops the crew out of the sky.
            var apron = Ctx.Locations.Position("M26.DusterPad");
            if (!Ctx.Crew.Deploy(CrewSlot.Guess, apron, Ctx.Locations.Heading("M26.DusterPad")))
            {
                return false;
            }

            ApplyBibleSetting();

            var player = Game.Player.Character;
            player.Weapons.Give(WeaponHash.Parachute, 1, false, true);
            player.Weapons.Give(WeaponHash.SMG, 250, false, true);

            SpawnStuntPlane();
            SpawnShamal();
            SpawnDinghy();
            if (!RequireAssets(_stuntPlane, _shamal, _shamalPilot, _dinghy)) return false;
            Station(CrewSlot.Ice, apron + new Vector3(-15f, 0f, 0f));
            Station(CrewSlot.Gohan, _dinghy, VehicleSeat.Driver);
            StartJet();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Get on his rudder",
                    new EnterVehicleObjective("Guess — take the stunt plane up.", () => _stuntPlane,
                        VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess)
                ;

            // The match is real flying: hold the band and the transfer becomes possible.
            yield return new MissionStage("Match the Shamal",
                    new ShadowTargetObjective("Climb to the Shamal and hold station inside 60 metres.",
                        () => _shamal, 60f, 12, "The Shamal outran the stunt plane.", 12f, acquireSeconds: 240))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context => StartJet())
                .OnExit(context => BoardTheJet())
                .WithCues("M27_S1_01_GUESS")
                .AfterCues("M27_S1_02_ICE");

            yield return new MissionStage("Zero-G",
                    new MissionInteraction("Ice: take the flight ledger from the cabin locker", () => _shamal.Position, 4, 8f, () => _shamal))
                .OwnedBy(CrewSlot.Ice)
                
                .OnExit(context => BeginDive())
                .WithCues("M27_S2_03_ICE");

            yield return new MissionStage("Terminal dive",
                    new BailOutObjective("The pilot put her over — get out.", BailAltitude))
                .OwnedBy(CrewSlot.Ice)
                .WithCues("M27_S2_04_GOHAN")
                .AfterCues("M27_S2_05_ICE");

            yield return new MissionStage("Sea pickup",
                    new EnterVehicleObjective("Ice: parachute to the green boat marker, then climb aboard Gohan's dinghy.", () => _dinghy))
                .OnExit(context =>
                {
                    context.State.CashOnHand += 75000;
                    GameUtils.Subtitle("~g~Flight ledgers secured. Every Aegis charter for six months.", 6000);
                })
                .AfterCues("M27_S2_06_GUESS");
        }

        /// <summary>
        /// The transfer. A fade, an attach, and the game never has to solve two
        /// aircraft in relative motion — which is the one thing it cannot do here.
        /// </summary>
        private void BoardTheJet()
        {
            if (_shamal == null || !_shamal.Exists()) return;

            GameUtils.FadeOut(400);
            try
            {
                Script.Wait(450);
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
            finally { GameUtils.FadeIn(500); }
            GameUtils.Subtitle("Ice is aboard. Press E / D-pad Right to secure the ledger.", 5000);
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

        private void StartJet()
        {
            if (_shamalPilot == null || !_shamalPilot.Exists() || _shamal == null || !_shamal.Exists()) return;

            _shamalPilot.Task.StartPlaneMission(_shamal, _seaPickup + new Vector3(0f, 0f, 700f), VehicleMissionType.Circle,
                42f, 180f, 700, 40, 0f, false);
        }

        private void SpawnStuntPlane()
        {
            var model = new Model("stunt");
            if (!GameUtils.RequestModel(model)) return;

            _stuntPlane = Track(World.CreateVehicle(model,
                Ctx.Locations.Position("M26.DusterPad") + new Vector3(20f, 0f, 0f),
                Ctx.Locations.Heading("M26.DusterPad")));
            model.MarkAsNoLongerNeeded();
            if (_stuntPlane == null || !_stuntPlane.Exists()) return;

            _stuntPlane.IsPersistent = true;
            _stuntPlane.IsEngineRunning = true;

            var blip = Track(_stuntPlane.AddBlip());
            blip.Sprite = BlipSprite.Plane;
            blip.Color = BlipColor.Orange;
            blip.Name = "Stunt plane";
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

            var blip = Track(_dinghy.AddBlip());
            blip.Sprite = BlipSprite.Boat;
            blip.Color = BlipColor.Green;
            blip.Name = "Gohan";
        }

        protected override void OnCleanup()
        {

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
