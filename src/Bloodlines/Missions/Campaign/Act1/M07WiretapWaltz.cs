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
    /// under fire from a minigun drone, landing behind Guess's moving sedan.
    ///
    /// Faked per docs/FEASIBILITY.md: there is no climbing animation set worth
    /// building a mission on, so the mast is the accessible platform on the garage
    /// roof rather than a 25-metre ascent, and the drone is a Buzzard held at
    /// altitude. The beat — tap the dish, get off the roof with something shooting at
    /// you — is intact; the climb was never the interesting part.
    /// </summary>
    public sealed class M07WiretapWaltz : ComposedMission
    {
        private Vehicle _drone;
        private Vehicle _sedan;
        private Ped _dronePilot;
        private Vector3 _roof;
        private Vector3 _mast;
        private Vector3 _landing;

        public override string Id => "M07";
        public override string Title => "Wiretap Waltz";

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _roof = Ctx.Locations.Position("M07.GarageRoof");
            _mast = Ctx.Locations.Position("M07.MastTop");
            _landing = Ctx.Locations.Position("M07.LandingZone");

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
                    GameUtils.Subtitle("~g~Two military turbine engines, routed to Paleto. That's the target.", 5000);
                    SpawnDrone();
                })
                .AfterCues("M07_S1_02_ICE", "M07_S1_03_GOHAN");

            yield return new MissionStage("Off the roof",
                    new ReachZoneObjective("Descend from the roof, then reach Guess's marked pickup. Use the parachute only if there is clearance.", () => _landing, 25f,
                        flat: false))
                
                .OnEnter(context => GameUtils.Subtitle("~r~Aegis helicopter approaching. Reach the pickup on the ground.", 4000))
                .WithCues("M07_S2_04_ICE", "M07_S2_05_GUESS");

            yield return new MissionStage("Moving pickup",
                    new EnterVehicleObjective("Get in behind Guess.", () => _sedan))
                .OnExit(context =>
                {
                    // The manifests are the payout, and they set up M08.
                    GameUtils.Subtitle("~g~Manifests decrypted. Elysian warehouse, tomorrow.", 5000);
                });
        }

        private void SpawnDrone()
        {
            var model = new Model("buzzard");
            var pilotModel = new Model("s_m_y_blackops_01");
            if (!GameUtils.RequestModel(model) || !GameUtils.RequestModel(pilotModel)) return;

            _drone = Track(World.CreateVehicle(model, _roof + new Vector3(0f, 40f, 30f), 180f));
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
            blip.Name = "Aegis drone";
        }

        private void SpawnSedan()
        {
            var model = new Model("schafter2");
            if (!GameUtils.RequestModel(model)) return;

            _sedan = Track(World.CreateVehicle(model, _landing, 0f));
            model.MarkAsNoLongerNeeded();
            if (_sedan == null || !_sedan.Exists()) return;

            _sedan.IsPersistent = true;

            var blip = Track(_sedan.AddBlip());
            blip.Sprite = BlipSprite.PersonalVehicleCar;
            blip.Color = BlipColor.Orange;
            blip.Name = "Guess";
        }
    }
}
