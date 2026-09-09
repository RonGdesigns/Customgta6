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
    /// The campaign's only pure air-to-air mission, and it is here for variety as much
    /// as story: after a bunker assault, a dredge and a sniper hold, the fourth mission
    /// of the act should not be another set of men on foot.
    /// </summary>
    public sealed class M26AlamoScramble : ComposedMission
    {
        private readonly List<Vehicle> _spotters = new List<Vehicle>();
        private readonly List<Ped> _pilots = new List<Ped>();

        private Vehicle _duster;
        private Vector3 _pad;
        private Vector3 _patrolBox;

        public override string Id => "M26";
        public override string Title => "The Alamo Scramble";

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _pad = Ctx.Locations.Position("M26.DusterPad");
            _patrolBox = Ctx.Locations.Position("M26.PatrolBox");

            if (!Ctx.Crew.Deploy(CrewSlot.Guess, _pad + new Vector3(6f, 0f, 0f),
                    Ctx.Locations.Heading("M26.DusterPad")))
            {
                return false;
            }

            ApplyBibleSetting();
            Station(CrewSlot.Ice, _pad + new Vector3(-15f, 0f, 0f));
            Station(CrewSlot.Gohan, _pad + new Vector3(0f, -20f, 0f));
            SpawnDuster();
            SpawnSpotters();
            if (!RequireAssets(_duster)) return false;
            if (_spotters.Count != 2 || _pilots.Count != 2) return false;
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Scramble",
                    new EnterVehicleObjective("Guess — take off in the marked Lazer.", () => _duster,
                        VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess)
                ;

            yield return new MissionStage("First spotter",
                    new DestroyVehicleObjective("Splash the lead spotter.",
                        () => _spotters.Count > 0 ? _spotters[0] : null))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context => GameUtils.Subtitle("~y~Use the Lazer aircraft weapons on the two red plane markers.", 5000))
                .WithCues("M26_S1_01_GUESS");

            yield return new MissionStage("Second spotter",
                    new DestroyVehicleObjective("The second one is diving for Grapeseed — kill him.",
                        () => _spotters.Count > 1 ? _spotters[1] : null))
                .OwnedBy(CrewSlot.Guess)
                
                .WithCues("M26_S1_02_GOHAN");

            yield return new MissionStage("Home",
                    new DeliverVehicleObjective("Guess: land the Lazer at McKenzie and stop.", () => _duster, () => _pad, 65f, land: true))
                .OnExit(context =>
                    GameUtils.Subtitle("~g~Both spotters in the lake. The Alamo stash stays a ghost.", 5000))
                .AfterCues("M26_S1_03_GUESS");
        }

        private void SpawnDuster()
        {
            var model = new Model("lazer");
            if (!GameUtils.RequestModel(model)) return;

            _duster = Track(World.CreateVehicle(model, _pad, Ctx.Locations.Heading("M26.DusterPad")));
            model.MarkAsNoLongerNeeded();
            if (_duster == null || !_duster.Exists()) return;

            _duster.IsPersistent = true;

            var blip = Track(_duster.AddBlip());
            blip.Sprite = BlipSprite.Plane;
            blip.Color = BlipColor.Orange;
            blip.Name = "Lazer interceptor";
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

        protected override void OnCleanup()
        {
            _spotters.Clear();
            _pilots.Clear();
        }
    }
}
