using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M21 — "The Port Heist: Open Water". Outer breakwater, 03:45.
    ///
    /// Part three. The Cargobob is slow and heavy and cannot defend itself, so Gohan
    /// runs an armed launch alongside it, pulling missile locks onto his own wake and
    /// killing the Aegis speedboats before they get in range.
    ///
    /// The mission is an escort played from the escort's side — the thing being
    /// protected is flown by an ally, not by the player, which is the only way the
    /// bible's three-vehicle convergence works without three players.
    /// </summary>
    public sealed class M21OpenWater : ComposedMission
    {
        private readonly List<Ped> _hostileCrews = new List<Ped>();

        private Vehicle _launch;
        private Vehicle _cargobob;
        private Ped _cargobobPilot;
        private Vector3 _spawn;
        private Vector3 _breakwater;
        private Vector3 _ridge;

        public override string Id => "M21";
        public override string Title => "The Port Heist: Open Water";

        protected override bool Setup()
        {
            _spawn = Ctx.Locations.Position("M21.LaunchSpawn");
            _breakwater = Ctx.Locations.Position("M21.Breakwater");
            _ridge = Ctx.Locations.Position("M21.RidgeCross");

            if (!Ctx.Crew.Deploy(CrewSlot.Gohan, _spawn + new Vector3(6f, 0f, 0f),
                    Ctx.Locations.Heading("M21.LaunchSpawn")))
            {
                return false;
            }

            ApplyBibleSetting();
            Game.Player.Character.Weapons.Give(WeaponHash.MG, 400, false, true);

            SpawnLaunch();
            SpawnCargobob();
            SpawnHostileBoats();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Get on the water",
                    new EnterVehicleObjective("Gohan — take the armed launch.", () => _launch,
                        VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Gohan)
                .WithDialogue(1);

            yield return new MissionStage("Draw the locks",
                    new ShadowTargetObjective("Stay on the Cargobob's wing.", () => _cargobob, 160f, 20,
                        "The Cargobob was left alone and took a missile."),
                    new ProtectObjective("", () => _cargobob, "The Cargobob went down with the bullion."))
                .OnEnter(context => StartCargobob());

            yield return new MissionStage("Kill the speedboats",
                    new KillTargetsObjective("Clear the Aegis boats before they close.",
                        () => _hostileCrews),
                    new ProtectObjective("", () => _cargobob, "The Cargobob went down with the bullion."))
                .WithDialogue(1);

            yield return new MissionStage("Over the ridge",
                    new ReachZoneObjective("See the bullion over the mountain ridge.", () => _ridge, 300f,
                        flat: true),
                    new ProtectObjective("", () => _cargobob, "The Cargobob went down with the bullion."))
                .OnExit(context =>
                    GameUtils.Subtitle("~g~Ridge cleared. We're crossing into Blaine County.", 5000));
        }

        private void SpawnLaunch()
        {
            var model = new Model("dinghy4");
            if (!GameUtils.RequestModel(model)) return;

            _launch = Track(World.CreateVehicle(model, _spawn, Ctx.Locations.Heading("M21.LaunchSpawn")));
            model.MarkAsNoLongerNeeded();
            if (_launch == null || !_launch.Exists()) return;

            _launch.IsPersistent = true;
            _launch.EnginePowerMultiplier = 6f;

            var blip = Track(_launch.AddBlip());
            blip.Sprite = BlipSprite.Boat;
            blip.Color = BlipColor.Green;
            blip.Name = "Armed launch";
        }

        private void SpawnCargobob()
        {
            var model = new Model("cargobob");
            var pilotModel = new Model("g_m_y_famca_01");
            if (!GameUtils.RequestModel(model) || !GameUtils.RequestModel(pilotModel)) return;

            _cargobob = Track(World.CreateVehicle(model, _spawn + new Vector3(-40f, 30f, 45f), 20f));
            if (_cargobob == null || !_cargobob.Exists()) return;
            _cargobob.IsPersistent = true;

            _cargobobPilot = Track(World.CreatePed(pilotModel, _cargobob.Position, 0f));
            model.MarkAsNoLongerNeeded();
            pilotModel.MarkAsNoLongerNeeded();
            if (_cargobobPilot == null || !_cargobobPilot.Exists()) return;

            _cargobobPilot.RelationshipGroup = Ctx.Crew.CrewGroup;
            _cargobobPilot.IsPersistent = true;
            _cargobobPilot.BlockPermanentEvents = true;
            _cargobobPilot.Task.WarpIntoVehicle(_cargobob, VehicleSeat.Driver);

            var blip = Track(_cargobob.AddBlip());
            blip.Sprite = BlipSprite.Helicopter;
            blip.Color = BlipColor.Blue;
            blip.Name = "Bullion lift";
        }

        private void StartCargobob()
        {
            if (_cargobobPilot == null || !_cargobobPilot.Exists()) return;
            if (_cargobob == null || !_cargobob.Exists()) return;

            // Heavy and slow on purpose: the escort has to be able to keep up with it,
            // and the player has to feel why it needs protecting.
            _cargobob.EnginePowerMultiplier = 0.6f;
            _cargobobPilot.Task.DriveTo(_cargobob, _ridge, 60f, 28f, DrivingStyle.Rushed);
        }

        private void SpawnHostileBoats()
        {
            var boatModel = new Model("predator");
            var crewModel = new Model("s_m_y_blackops_01");
            if (!GameUtils.RequestModel(boatModel) || !GameUtils.RequestModel(crewModel)) return;

            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");

            for (int i = 0; i < 3; i++)
            {
                var boat = Track(World.CreateVehicle(boatModel,
                    _breakwater + new Vector3(-40f + i * 40f, 20f, 0f), 200f));
                if (boat == null || !boat.Exists()) continue;
                boat.IsPersistent = true;

                var crew = Track(World.CreatePed(crewModel, boat.Position, 0f));
                if (crew == null || !crew.Exists()) continue;

                crew.RelationshipGroup = aegis;
                crew.IsPersistent = true;
                crew.BlockPermanentEvents = true;
                crew.Accuracy = 35;
                crew.Weapons.Give(WeaponHash.CarbineRifle, 250, true, true);
                crew.Task.WarpIntoVehicle(boat, VehicleSeat.Driver);
                crew.Task.VehicleChase(Game.Player.Character);

                _hostileCrews.Add(crew);

                var blip = Track(boat.AddBlip());
                blip.Sprite = BlipSprite.Boat;
                blip.Color = BlipColor.Red;
                blip.Name = "Aegis launch";
            }

            boatModel.MarkAsNoLongerNeeded();
            crewModel.MarkAsNoLongerNeeded();
        }

        protected override void OnCleanup()
        {
            _hostileCrews.Clear();
        }
    }
}
