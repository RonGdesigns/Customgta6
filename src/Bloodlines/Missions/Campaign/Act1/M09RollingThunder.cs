using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M09 — "Rolling Thunder". Route 68 past Harmony, 19:30, dusk.
    ///
    /// Guess flies the ridgelines while Ice takes the escort driver, and the crew
    /// lifts the military IFF transponder that gets them into Zancudo in M16.
    ///
    /// The bible lands the helicopter's skids on a moving truck roof. Physics between
    /// two moving vehicles is where set pieces die, so the truck stops when its driver
    /// does — Ice's shot is what makes the pickup possible, which is the same
    /// causality the scene wants, minus a physics gamble.
    /// </summary>
    public sealed class M09RollingThunder : ComposedMission
    {
        private readonly List<Vehicle> _convoy = new List<Vehicle>();
        private readonly List<Ped> _crews = new List<Ped>();

        private Vehicle _frogger;
        private Ped _escortDriver;
        private Vehicle _escortTruck;
        private Vector3 _convoyStart;
        private Vector3 _bunker;

        public override string Id => "M09";
        public override string Title => "Rolling Thunder";

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _convoyStart = Ctx.Locations.Position("M09.ConvoyStart");
            _bunker = Ctx.Locations.Position("M09.Bunker");

            if (!Ctx.Crew.Deploy(CrewSlot.Guess, Ctx.Locations.Position("M09.HeliSpawn"),
                    Ctx.Locations.Heading("M09.HeliSpawn")))
            {
                return false;
            }

            ApplyBibleSetting();
            Game.Player.Character.Weapons.Give(WeaponHash.SniperRifle, 40, false, true);

            SpawnFrogger();
            SpawnConvoy();
            if (!RequireAssets(_frogger, _escortTruck, _escortDriver)) return false;
            Station(CrewSlot.Ice, Ctx.Locations.Position("M09.AmbushPoint") + new Vector3(0f, -35f, 0f));
            Station(CrewSlot.Gohan, _bunker + new Vector3(8f, 0f, 0f));
            Ctx.Crew.PedFor(CrewSlot.Ice).Weapons.Give(WeaponHash.SniperRifle, 80, true, true);
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Get airborne",
                    new EnterVehicleObjective("Guess — take the Frogger up.", () => _frogger,
                        VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess)
                
                .WithCues("M09_S1_01_ICE");

            // Shadowing, not chasing: too close and the convoy's anti-air sees them.
            yield return new MissionStage("Shadow the convoy",
                    new ShadowTargetObjective("Hold the ridgeline behind the convoy.",
                        () => _escortTruck, 220f, 25, "The convoy spotted the helicopter and scattered.",
                        60f, acquireSeconds: 180))
                .OnEnter(context => StartConvoy())
                .WithCues("M09_S1_02_GUESS");

            yield return new MissionStage("Take the driver",
                    new KillTargetsObjective("Ice: wait at the ambush point and shoot the marked escort driver.",
                        () => new[] { _escortDriver }))
                .OwnedBy(CrewSlot.Ice)
                
                .OnExit(context =>
                {
                    // Truck in the culvert, crew scattering: the pickup is now static.
                    if (_escortTruck != null && _escortTruck.Exists())
                    {
                        _escortTruck.IsDriveable = false;
                        _escortTruck.Speed = 0f;
                    }
                })
                .AfterCues("M09_S2_03_ICE");

            yield return new MissionStage("Rip the transponder",
                    new MissionInteraction("Ice: get out, approach the stopped escort cab, and take its IFF transponder.", () => EscortPosition(), 6, 5f))
                .OnExit(context => GameUtils.Subtitle("~g~Code 7-Echo-Victor. Military clearance, tomorrow.", 5000))
                .WithCues("M09_S2_04_GUESS")
                .AfterCues("M09_S2_05_ICE");

            yield return new MissionStage("Transponder extraction",
                    new ReachZoneObjective("Get the transponder to the temporary drop point.",
                        () => _bunker, 30f, flat: true)); // The permanent bunker unlock belongs to M23.
        }

        private Vector3 EscortPosition()
        {
            return _escortTruck != null && _escortTruck.Exists()
                ? _escortTruck.Position
                : Ctx.Locations.Position("M09.AmbushPoint");
        }

        private void SpawnFrogger()
        {
            var model = new Model("frogger");
            if (!GameUtils.RequestModel(model)) return;

            _frogger = Track(World.CreateVehicle(model, Ctx.Locations.Position("M09.HeliSpawn"),
                Ctx.Locations.Heading("M09.HeliSpawn")));
            model.MarkAsNoLongerNeeded();
            if (_frogger == null || !_frogger.Exists()) return;

            _frogger.IsPersistent = true;

            var blip = Track(_frogger.AddBlip());
            blip.Sprite = BlipSprite.Helicopter;
            blip.Color = BlipColor.Orange;
            blip.Name = "Frogger";
        }

        private void SpawnConvoy()
        {
            var truckModel = new Model("insurgent");
            var crewModel = new Model("s_m_y_marine_01");
            if (!GameUtils.RequestModel(truckModel) || !GameUtils.RequestModel(crewModel)) return;

            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");

            for (int i = 0; i < 3; i++)
            {
                var truck = Track(World.CreateVehicle(truckModel,
                    _convoyStart + new Vector3(0f, i * 18f, 0f),
                    Ctx.Locations.Heading("M09.ConvoyStart")));
                if (truck == null || !truck.Exists()) continue;

                truck.IsPersistent = true;
                _convoy.Add(truck);

                var driver = Track(World.CreatePed(crewModel, truck.Position, 0f));
                if (driver == null || !driver.Exists()) continue;

                driver.RelationshipGroup = aegis;
                driver.IsPersistent = true;
                driver.BlockPermanentEvents = true;
                driver.Weapons.Give(WeaponHash.CarbineRifle, 150, true, true);
                driver.Task.WarpIntoVehicle(truck, VehicleSeat.Driver);
                _crews.Add(driver);

                // The rear vehicle is the escort — the one Ice is told to take.
                if (i == 2)
                {
                    _escortTruck = truck;
                    _escortDriver = driver;

                    var blip = Track(truck.AddBlip());
                    blip.Sprite = BlipSprite.Enemy;
                    blip.Color = BlipColor.Red;
                    blip.Name = "Escort truck";
                }
            }

            truckModel.MarkAsNoLongerNeeded();
            crewModel.MarkAsNoLongerNeeded();
        }

        private void StartConvoy()
        {
            var target = Ctx.Locations.Position("M09.AmbushPoint");

            for (int i = 0; i < _crews.Count && i < _convoy.Count; i++)
            {
                var driver = _crews[i];
                var truck = _convoy[i];
                if (driver == null || !driver.Exists() || truck == null || !truck.Exists()) continue;

                driver.Task.DriveTo(truck, target, 15f, 22f, DrivingStyle.AvoidTrafficExtremely);
            }
        }

        protected override void OnCleanup()
        {
            _convoy.Clear();
            _crews.Clear();
        }
    }
}
