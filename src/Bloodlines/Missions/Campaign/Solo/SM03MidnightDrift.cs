using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// SM03 — "Midnight Drift". Olympic Freeway industrial basin, 01:00.
    ///
    /// Guess's solo and the mission that has to prove the driving identity: a
    /// three-lap pink-slip circuit under the overpass, with two Marabunta racers who
    /// pull guns once they lose. Slipstream Reflex is the intended way to hold the
    /// hairpins, so the meter starts full.
    ///
    /// The last stage is deliberately either/or: shake the shooters or cross the line
    /// first. A race that can only be won by killing everyone is not a race.
    /// </summary>
    public sealed class SM03MidnightDrift : ComposedMission
    {
        private readonly List<Ped> _rivals = new List<Ped>();
        private readonly List<Vehicle> _rivalCars = new List<Vehicle>();

        private Vehicle _coupe;
        private Vector3 _start;
        private List<Vector3> _circuit;

        public override string Id => "SM03";
        public override string Title => "Midnight Drift";

        protected override bool Setup()
        {
            _start = Ctx.Locations.Position("SM03.StartLine");
            _circuit = new List<Vector3>
            {
                Ctx.Locations.Position("SM03.Checkpoint1"),
                Ctx.Locations.Position("SM03.Checkpoint2"),
                Ctx.Locations.Position("SM03.Checkpoint3"),
                Ctx.Locations.Position("SM03.Checkpoint4")
            };

            if (!Ctx.Crew.DeploySolo(CrewSlot.Guess, _start + new Vector3(4f, 0f, 0f),
                    Ctx.Locations.Heading("SM03.StartLine")))
            {
                return false;
            }

            ApplyBibleSetting();
            Ctx.Abilities.Refill();

            if (!SpawnCoupe()) return false;
            SpawnRivals();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Starting line",
                    new EnterVehicleObjective("Get in the drift coupe.", () => _coupe, VehicleSeat.Driver))
                .PlayedBy(CrewSlot.Guess)
                .WithDialogue(1);

            yield return new MissionStage("Three laps",
                    new RaceCheckpointObjective("Win the circuit — three laps.", _circuit, 14f, 3),
                    new ProtectObjective("", () => _coupe, "The coupe is wrecked."))
                .PlayedBy(CrewSlot.Guess)
                .OnEnter(context => StartRivals());

            yield return new MissionStage("They pulled guns",
                    new KillTargetsObjective("Shake the Marabunta shooters.", () => _rivals),
                    new ReachZoneObjective("Cross the line.", () => _start, 15f, flat: true))
                .AnyOf()
                .PlayedBy(CrewSlot.Guess)
                .WithDialogue(2)
                .OnEnter(context =>
                {
                    foreach (var rival in _rivals)
                    {
                        if (rival == null || !rival.Exists()) continue;
                        rival.Task.VehicleShootAtPed(Game.Player.Character);
                    }
                })
                .OnExit(context =>
                {
                    // The bible's payout: the pink slip plus the transmission and clutch
                    // the crew's getaway fleet is waiting on.
                    context.State.CashOnHand += 25000;
                    context.State.SetUpgrade("racingTransmissionInstalled", true);
                    GameUtils.Subtitle("~g~Pink slip and the racing transmission are yours.", 5000);
                });
        }

        private bool SpawnCoupe()
        {
            var model = new Model("futo");
            if (!GameUtils.RequestModel(model)) return false;

            _coupe = Track(World.CreateVehicle(model, _start, Ctx.Locations.Heading("SM03.StartLine")));
            model.MarkAsNoLongerNeeded();
            if (_coupe == null || !_coupe.Exists()) return false;

            _coupe.IsPersistent = true;
            _coupe.Mods.InstallModKit();
            _coupe.Mods[VehicleModType.Engine].Index = 3;
            _coupe.Mods[VehicleModType.Transmission].Index = 2;
            _coupe.Mods[VehicleModType.Suspension].Index = 3;

            var blip = Track(_coupe.AddBlip());
            blip.Sprite = BlipSprite.PersonalVehicleCar;
            blip.Color = BlipColor.Orange;
            blip.Name = "Drift coupe";
            return true;
        }

        private void SpawnRivals()
        {
            var carModel = new Model("elegy2");
            var pedModel = new Model("g_m_y_salvaboss_01");
            if (!GameUtils.RequestModel(carModel) || !GameUtils.RequestModel(pedModel)) return;

            var marabunta = World.AddRelationshipGroup("BLOODLINES_CARTEL");

            for (int i = 0; i < 2; i++)
            {
                var car = Track(World.CreateVehicle(carModel, _start + new Vector3(-4f - i * 4f, 0f, 0f),
                    Ctx.Locations.Heading("SM03.StartLine")));
                if (car == null || !car.Exists()) continue;
                car.IsPersistent = true;

                var driver = Track(World.CreatePed(pedModel, car.Position, 0f));
                if (driver == null || !driver.Exists()) continue;

                driver.RelationshipGroup = marabunta;
                driver.IsPersistent = true;
                driver.BlockPermanentEvents = true;
                driver.DrivingStyle = DrivingStyle.Rushed;
                driver.Weapons.Give(WeaponHash.MicroSMG, 120, false, true);
                driver.Task.WarpIntoVehicle(car, VehicleSeat.Driver);

                _rivals.Add(driver);
                _rivalCars.Add(car);
            }

            carModel.MarkAsNoLongerNeeded();
            pedModel.MarkAsNoLongerNeeded();
        }

        private void StartRivals()
        {
            for (int i = 0; i < _rivals.Count; i++)
            {
                var rival = _rivals[i];
                var car = i < _rivalCars.Count ? _rivalCars[i] : null;
                if (rival == null || !rival.Exists() || car == null || !car.Exists()) continue;

                // They run the circuit rather than chasing: a rival that tails the player
                // is a pursuit, not a race.
                rival.Task.DriveTo(car, _circuit[0], 12f, 35f, DrivingStyle.Rushed);
            }
        }

        protected override void OnCleanup()
        {
            _rivals.Clear();
            _rivalCars.Clear();
        }
    }
}
