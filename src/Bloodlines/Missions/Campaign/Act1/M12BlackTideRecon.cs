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
    /// The ROV drone is the submersible with its lights off, per
    /// docs/FEASIBILITY.md — a camera-only drone would need a vehicle the game does
    /// not have, and a small sub reads the same at night.
    /// </summary>
    public sealed class M12BlackTideRecon : ComposedMission
    {
        private readonly List<Ped> _patrols = new List<Ped>();

        private Vehicle _rov;
        private Vector3 _jetty;
        private Vector3 _hull;
        private Vector3 _buoy;

        public override string Id => "M12";
        public override string Title => "Black Tide Recon";

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
                
                .OnExit(context =>
                    GameUtils.Subtitle("~g~Eight inches of reinforced steel. We need acoustic torches.", 5000))
                .AfterCues("M12_S1_03_GOHAN");

            yield return new MissionStage("Back to the jetty",
                    new DeliverVehicleObjective("Gohan: surface in the sub beside the jetty.", () => _rov, () => _jetty + new Vector3(0f, -8f, -4f), 10f))
                .OnExit(context =>
                {
                    // What this mission actually produces is the breach point for M19.
                    context.State.CashOnHand += 0;
                    GameUtils.Subtitle("~g~Breach coordinates tagged. Berth 44 is mapped.", 5000);
                });
        }

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
        }
    }
}
