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
        private Prop _container;
        private Ped _cargobobPilot;
        private OperationHandoff _handoff;
        private Vector3 _spawn;
        private Vector3 _breakwater;
        private Vector3 _ridge;

        public override string Id => "M21";
        public override string Title => "The Port Heist: Open Water";

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _spawn = Ctx.Locations.Position("M21.LaunchSpawn");
            _breakwater = Ctx.Locations.Position("M21.Breakwater");
            _ridge = Ctx.Locations.Position("M21.RidgeCross");

            // On the pier, not in the water: a deployment onto a water coordinate
            // starts the mission with everyone swimming.
            if (!Ctx.Crew.Deploy(CrewSlot.Gohan, Ctx.Locations.Position("M12.PierWatch"),
                    Ctx.Locations.Heading("M12.PierWatch")))
            {
                return false;
            }

            ApplyBibleSetting();
            Game.Player.Character.Weapons.Give(WeaponHash.MG, 400, false, true);

            _handoff = Ctx.Handoffs.Take(PortHeist.Operation, Id);
            SpawnLaunch();
            SpawnCargobob();
            if (!RequireAssets(_launch, _cargobob, _cargobobPilot, _container)) return false;
            RequireAsset(_cargobob, "The Cargobob went down with the bullion.");
            RequireAsset(_container, "The bullion container was lost.");
            RequireAsset(_launch, "The armed launch was destroyed. The lift has no escort.");
            Station(CrewSlot.Gohan, _launch, VehicleSeat.Driver);
            Station(CrewSlot.Ice, _launch, VehicleSeat.Passenger);
            Ctx.Crew.PedFor(CrewSlot.Ice).Weapons.Give(WeaponHash.MicroSMG, 500, true, true);
            // The lift waits, airborne and rotors turning, until the escort is actually
            // on the water. Nothing flies toward the breakwater while Gohan is still
            // boarding the launch.
            HoldLift();
            return true;
        }

        private void HoldLift()
        {
            if (_cargobob == null || !_cargobob.Exists()) return;
            _cargobob.IsPositionFrozen = true;
            _cargobob.IsEngineRunning = true;
            Function.Call(Hash.SET_HELI_BLADES_FULL_SPEED, _cargobob);
        }

        private void ReleaseLift()
        {
            if (_cargobob == null || !_cargobob.Exists()) return;
            _cargobob.IsPositionFrozen = false;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Get on the water",
                    new EnterVehicleObjective("Gohan — take the armed launch.", () => _launch,
                        VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Gohan)
                ;

            yield return new MissionStage("Draw the locks",
                    new ShadowTargetObjective("Stay on the Cargobob's wing.", () => _cargobob, 240f, 15,
                        "The launch lost contact with the Cargobob.", acquireSeconds: 60),
                    new ProtectObjective("", () => _cargobob, "The Cargobob went down with the bullion."))
                .OnEnter(context => { ReleaseLift(); StartCargobob(); SpawnHostileBoats(); })
                .WithCues("M21_S1_01_GOHAN");

            yield return new MissionStage("Kill the speedboats",
                    new KillTargetsObjective("Clear the Aegis boats before they close.",
                        () => _hostileCrews),
                    new ProtectObjective("", () => _cargobob, "The Cargobob went down with the bullion."))
                
                .AfterCues("M21_S1_02_ICE");

            yield return new MissionStage("Over the ridge",
                    new DeliverVehicleObjective("Gohan: take the launch through the yellow breakwater exit. Guess will continue inland by air.", () => _launch, () => _breakwater, 50f),
                    new ProtectObjective("", () => _cargobob, "The Cargobob went down with the bullion."))
                .OnExit(context =>
                    GameUtils.Subtitle("~g~Water route clear. Guess is taking the bullion north to the Alamo.", 5000))
                .AfterCues("M21_S1_03_GUESS");
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

            // With a fresh M20 record the lift starts where the climb-out ended and
            // still flies heavy; otherwise the default staging above the launch.
            var start = _handoff != null && _handoff.VehicleModel.Length > 0 ? _handoff.VehiclePosition : _spawn + new Vector3(-40f, 30f, 45f);
            float heading = _handoff != null && _handoff.VehicleModel.Length > 0 ? _handoff.VehicleHeading : 20f;
            _cargobob = Track(World.CreateVehicle(model, start, heading));
            if (_cargobob == null || !_cargobob.Exists()) return;
            _cargobob.IsPersistent = true;
            AttachContainer();

            _cargobobPilot = Ctx.Crew.PedFor(CrewSlot.Guess);
            model.MarkAsNoLongerNeeded();
            pilotModel.MarkAsNoLongerNeeded();
            if (_cargobobPilot == null || !_cargobobPilot.Exists()) return;

            _cargobobPilot.RelationshipGroup = Ctx.Crew.CrewGroup;
            _cargobobPilot.IsPersistent = true;
            _cargobobPilot.BlockPermanentEvents = true;
            Station(CrewSlot.Guess, _cargobob, VehicleSeat.Driver);
            _cargobob.IsEngineRunning = true;

            var blip = Track(_cargobob.AddBlip());
            blip.Sprite = BlipSprite.Helicopter;
            blip.Color = BlipColor.Blue;
            blip.Name = "Bullion lift";
        }

        /// <summary>
        /// The story says this is the bullion lift, so the bullion has to be visible
        /// under it. M20 attached the container to the aircraft; the same attachment
        /// is rebuilt here so the escort protects something the player can see.
        /// </summary>
        private void AttachContainer()
        {
            var containerModel = new Model("prop_container_01a");
            if (!GameUtils.RequestModel(containerModel)) return;
            _container = Track(World.CreateProp(containerModel, _cargobob.Position - new Vector3(0f, 0f, 7f), false, false));
            containerModel.MarkAsNoLongerNeeded();
            if (_container == null || !_container.Exists()) return;
            _container.IsPersistent = true;
            Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY, _container, _cargobob, 0,
                0f, 0f, -6.5f, 0f, 0f, 0f, false, false, true, false, 2, true);
            Logger.Info("M21: bullion container attached under the escorted lift" + (_handoff != null ? " (continuing M20's lift)." : " (default staging)."));
        }

        /// <summary>The lift crosses the ridge with the bullion; M22 starts from that state.</summary>
        protected override void OnPassed()
        {
            var record = OperationHandoff.Capture(PortHeist.Operation, Id, "M22", Ctx.Crew, _cargobob);
            record.CargoAttached = _container != null && _container.Exists();
            record.CargoModel = "prop_container_01a";
            Ctx.Handoffs.Record(record);
        }

        private void StartCargobob()
        {
            if (_cargobobPilot == null || !_cargobobPilot.Exists()) return;
            if (_cargobob == null || !_cargobob.Exists()) return;

            // Heavy and slow on purpose: the escort has to be able to keep up with it,
            // and the player has to feel why it needs protecting.
            _cargobob.EnginePowerMultiplier = 0.6f;
            var target = _breakwater + new Vector3(0f, 0f, 45f);
            _cargobobPilot.Task.StartHeliMission(_cargobob, target, VehicleMissionType.GoTo, 12f, 25f,
                (int)target.Z, 35, -1f, 50f, (HeliMissionFlags)(256 | 4096));
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
                crew.Task.StartBoatMission(boat, _launch, VehicleMissionType.GoTo, 14f, (VehicleDrivingFlags)786603, 20f, (BoatMissionFlags)7);
                var gunner = Track(World.CreatePed(crewModel, boat.Position, 0f));
                if (gunner != null && gunner.Exists())
                {
                    gunner.RelationshipGroup = aegis; gunner.IsPersistent = true; gunner.BlockPermanentEvents = true;
                    gunner.Accuracy = 20; gunner.Weapons.Give(WeaponHash.MicroSMG, 500, true, true);
                    gunner.SetIntoVehicle(boat, VehicleSeat.Passenger); gunner.Task.VehicleShootAtPed(Game.Player.Character);
                    _hostileCrews.Add(gunner);
                }

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
            // A held lift that outlives the attempt (occupied transports are released,
            // not deleted) must not stay pinned in the air.
            ReleaseLift();
            if (_container != null && _container.Exists()) Function.Call(Hash.DETACH_ENTITY, _container, true, true);
            _hostileCrews.Clear();
        }
    }
}
