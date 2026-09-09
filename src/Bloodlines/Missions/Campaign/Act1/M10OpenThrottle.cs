using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M10 — "Open Throttle". Del Perro Freeway, 23:00, rain.
    ///
    /// The turbine engines have to reach the shop, and the whole of Act I's escalation
    /// is riding on the flatbed. Guess keeps the cargo moving, Ice steps out to use the RPG beside
    /// the bed, Gohan fries the bikes' electronics.
    ///
    /// The purest Green set piece in the campaign — no interiors, no faked physics,
    /// just a road, a speed floor and three characters with different jobs on the same
    /// vehicle. If the switch mechanic does not feel good here, it will not feel good
    /// anywhere.
    /// </summary>
    public sealed class M10OpenThrottle : ComposedMission
    {
        private readonly List<Ped> _bikers = new List<Ped>();
        private readonly List<Vehicle> _bikes = new List<Vehicle>();

        private Vehicle _flatbed;
        private Vehicle _buzzard;
        private Vector3 _start;
        private Vector3 _tunnel;

        public override string Id => "M10";
        public override string Title => "Open Throttle";

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _start = Ctx.Locations.Position("M10.ConvoyStart");
            _tunnel = Ctx.Locations.Position("M10.TunnelMouth");

            if (!Ctx.Crew.Deploy(CrewSlot.Guess, _start, Ctx.Locations.Heading("M10.ConvoyStart")))
            {
                return false;
            }

            ApplyBibleSetting();

            var player = Game.Player.Character;
            player.Weapons.Give(WeaponHash.RPG, 10, false, true);

            if (!SpawnFlatbed()) return false;
            Station(CrewSlot.Ice, _flatbed, VehicleSeat.Passenger);
            Station(CrewSlot.Gohan, _start + new Vector3(15f, -10f, 0f));
            Ctx.Crew.PedFor(CrewSlot.Ice).Weapons.Give(WeaponHash.RPG, 12, false, true);
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Roll out",
                    new EnterVehicleObjective("Guess — take the flatbed.", () => _flatbed,
                        VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess)
                ;

            // The speed floor and the bikes run together: dropping below eighty is what
            // lets them box the flatbed in, which is exactly what the bible says.
            yield return new MissionStage("Del Perro run",
                    new SpeedFloorObjective("Keep the flatbed above 35 mph; use drive-by weapons on the bikes.", 35f,
                        "The chase boxed the flatbed in and popped the slicks.", graceSeconds: 20),
                    new KillTargetsObjective("Clear the cartel bikes.", () => _bikers, 0, false),
                    new ProtectObjective("", () => _flatbed, "The flatbed and the engines are gone."))
                .OnEnter(context => SpawnBikes())
                .WithCues("M10_S1_01_GUESS", "M10_S1_02_ICE", "M10_S1_03_GOHAN");

            yield return new MissionStage("Gunship",
                    new DestroyVehicleObjective("Ice: have Guess stop, get out, and use the RPG on the marked Buzzard.", () => _buzzard),
                    new ProtectObjective("", () => _flatbed, "The flatbed and the engines are gone."))
                .OwnedBy(CrewSlot.Ice)
                
                .OnEnter(context => { SpawnBuzzard(); Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Guess); Ctx.Crew.PedFor(CrewSlot.Guess).Task.ClearAllImmediately(); _flatbed.Speed = 0f; })
                .AfterCues("M10_S2_04_ICE");

            yield return new MissionStage("Tunnel mouth",
                    new DeliverVehicleObjective("Guess: get back in the flatbed and deliver the engines to the tunnel.", () => _flatbed, () => _tunnel, 30f) { RequiredCharacter = CrewSlot.Guess },
                    new ProtectObjective("", () => _flatbed, "The flatbed and the engines are gone."))
                .OnExit(context =>
                {
                    // M11 owns installation; these engines are still cargo.
                    GameUtils.Subtitle("~g~Engines delivered. Burro Heights, tomorrow.", 5000);
                })
                .WithCues("M10_S2_05_GUESS")
                .AfterCues("M10_S2_06_GOHAN");
        }

        private bool SpawnFlatbed()
        {
            var model = new Model("flatbed");
            if (!GameUtils.RequestModel(model)) return false;

            _flatbed = Track(World.CreateVehicle(model, _start, Ctx.Locations.Heading("M10.ConvoyStart")));
            model.MarkAsNoLongerNeeded();
            if (_flatbed == null || !_flatbed.Exists()) return false;

            _flatbed.IsPersistent = true;
            // Preserve the flatbed's engine force; top-speed tuning is shared.
            _flatbed.CanTiresBurst = false;

            var blip = Track(_flatbed.AddBlip());
            blip.Sprite = BlipSprite.ArmoredTruck;
            blip.Color = BlipColor.Orange;
            blip.Name = "Engine transport";
            return true;
        }

        private void SpawnBikes()
        {
            var bikeModel = new Model("sanchez");
            var riderModel = new Model("g_m_y_mexgoon_03");
            if (!GameUtils.RequestModel(bikeModel) || !GameUtils.RequestModel(riderModel)) return;

            var cartel = World.AddRelationshipGroup("BLOODLINES_CARTEL");
            var player = Game.Player.Character;

            for (int i = 0; i < 3; i++)
            {
                var behind = player.Position - player.ForwardVector * (40f + i * 12f)
                             + new Vector3(i * 4f - 4f, 0f, 0f);

                var bike = Track(World.CreateVehicle(bikeModel, behind, player.Heading));
                if (bike == null || !bike.Exists()) continue;
                bike.IsPersistent = true;
                _bikes.Add(bike);

                var rider = Track(World.CreatePed(riderModel, behind, player.Heading));
                if (rider == null || !rider.Exists()) continue;

                rider.RelationshipGroup = cartel;
                rider.IsPersistent = true;
                rider.BlockPermanentEvents = true;
                rider.Accuracy = 20;
                rider.Weapons.Give(WeaponHash.MicroSMG, 200, true, true);
                rider.Task.WarpIntoVehicle(bike, VehicleSeat.Driver);
                rider.Task.VehicleChase(player);
                GTA.Native.Function.Call(GTA.Native.Hash.SET_PED_COMBAT_ATTRIBUTES, rider, 52, true);

                _bikers.Add(rider);
            }

            bikeModel.MarkAsNoLongerNeeded();
            riderModel.MarkAsNoLongerNeeded();
        }

        private void SpawnBuzzard()
        {
            var model = new Model("buzzard");
            var pilotModel = new Model("s_m_y_blackops_01");
            if (!GameUtils.RequestModel(model) || !GameUtils.RequestModel(pilotModel)) return;

            var player = Game.Player.Character;
            _buzzard = Track(World.CreateVehicle(model, player.Position + player.ForwardVector * 120f
                                                       + new Vector3(0f, 0f, 40f), player.Heading + 180f));
            if (_buzzard == null || !_buzzard.Exists()) return;
            _buzzard.IsPersistent = true;

            var pilot = Track(World.CreatePed(pilotModel, _buzzard.Position, 0f));
            model.MarkAsNoLongerNeeded();
            pilotModel.MarkAsNoLongerNeeded();
            if (pilot == null || !pilot.Exists()) return;

            pilot.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            pilot.IsPersistent = true;
            pilot.Task.WarpIntoVehicle(_buzzard, VehicleSeat.Driver);
            pilot.Task.ChaseWithHelicopter(player, new Vector3(0f, 0f, 30f));

            var blip = Track(_buzzard.AddBlip());
            blip.Sprite = BlipSprite.Helicopter;
            blip.Color = BlipColor.Red;
            blip.Name = "Aegis Buzzard";
        }

        protected override void OnCleanup()
        {
            _bikers.Clear();
            _bikes.Clear();
        }
    }
}
