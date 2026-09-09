using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M08 — "Supply &amp; Sever". Elysian Island, 16:45, overcast.
    ///
    /// The two 700-horsepower turbine engines the whole fleet is built around. Gohan
    /// loops the cameras, Ice drops the sentries quietly, Guess runs the forklift, and
    /// an Aegis technical turns up between crate one and crate two.
    ///
    /// The forklift is a hold-zone rather than a drivable loader: the vehicle exists,
    /// but scripted forks that reliably lift a crate onto a flatbed do not, and the
    /// beat is the ninety seconds of exposure, not the hydraulics.
    /// </summary>
    public sealed class M08SupplyAndSever : ComposedMission
    {
        private readonly List<Ped> _sentries = new List<Ped>();

        private Vehicle _hauler;
        private Vehicle _technical;
        private Vector3 _gate;
        private Vector3 _cameras;
        private Vector3 _padOne;
        private Vector3 _padTwo;
        private Vector3 _connector;

        public override string Id => "M08";
        public override string Title => "Supply & Sever";

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _gate = Ctx.Locations.Position("M08.WarehouseGate");
            _cameras = Ctx.Locations.Position("M08.CameraRoom");
            _padOne = Ctx.Locations.Position("M08.CratePadOne");
            _padTwo = Ctx.Locations.Position("M08.CratePadTwo");
            _connector = Ctx.Locations.Position("M08.Connector");

            if (!Ctx.Crew.Deploy(CrewSlot.Gohan, _gate, 0f)) return false;

            ApplyBibleSetting();
            Game.Player.Character.Weapons.Give(WeaponHash.APPistol, 120, false, true);

            SpawnSentries();
            SpawnHauler();
            if (!RequireAssets(_hauler)) return false;
            Station(CrewSlot.Gohan, _cameras + new Vector3(0f, -12f, 0f));
            Station(CrewSlot.Ice, _gate + new Vector3(-18f, 0f, 0f));
            Station(CrewSlot.Guess, _padOne + new Vector3(-20f, 0f, 0f));
            Ctx.Crew.PedFor(CrewSlot.Ice).Weapons.Give(WeaponHash.RPG, 8, false, false);
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Loop the cameras",
                    new MissionInteraction("Gohan — loop the CCTV feed.", () => _cameras, 5, 3f))
                .OwnedBy(CrewSlot.Gohan)
                
                .OnExit(context => GameUtils.Subtitle("~g~Camera loop is running. Ice, clear the marked sentries.", 4000))
                .WithCues("M08_S1_01_GUESS")
                .AfterCues("M08_S1_02_GOHAN");

            yield return new MissionStage("Drop the sentries",
                    new KillTargetsObjective("Ice — drop the sentries at the marked warehouse posts.",
                        () => _sentries))
                .OwnedBy(CrewSlot.Ice);

            yield return new MissionStage("Crate one",
                    new MissionInteraction("Guess — load the first turbine crate.", () => _padOne, 8, 3.5f))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context => SpawnTechnical());

            yield return new MissionStage("Technical",
                    new DestroyVehicleObjective("Ice — put the Aegis technical down.", () => _technical))
                .OwnedBy(CrewSlot.Ice)
                
                .WithCues("M08_S2_03_GUESS")
                .AfterCues("M08_S2_04_ICE");

            yield return new MissionStage("Crate two",
                    new MissionInteraction("Guess — load the second crate.", () => _padTwo, 8, 3.5f))
                .OwnedBy(CrewSlot.Guess);

            yield return new MissionStage("Del Perro connector",
                    new EnterVehicleObjective("Take the hauler.", () => _hauler, VehicleSeat.Driver),
                    new ProtectObjective("", () => _hauler, "The hauler and the engines are gone."))
                .OwnedBy(CrewSlot.Guess);

            yield return new MissionStage("Deliver",
                    new DeliverVehicleObjective("Guess: deliver the loaded flatbed to the connector.", () => _hauler, () => _connector, 25f),
                    new ProtectObjective("", () => _hauler, "The hauler and the engines are gone."))
                .OnExit(context =>
                {
                    // The engines are the fleet: M11 puts one of them in the Granger.
                    // M11 owns installation; these engines are still cargo.
                    GameUtils.Subtitle("~g~Engines secured. Get them to the shop before we talk horsepower.", 6000);
                })
                .WithCues("M08_S2_05_GUESS");
        }

        private void SpawnSentries()
        {
            var model = new Model("s_m_m_security_01");
            if (!GameUtils.RequestModel(model)) return;

            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");

            for (int i = 0; i < 5; i++)
            {
                var guard = World.CreatePed(model, _gate + new Vector3(6f + i * 5f, 10f + (i % 2) * 8f, 0f), 180f);
                if (guard == null || !guard.Exists()) continue;

                guard.RelationshipGroup = aegis;
                guard.IsPersistent = true;
                guard.BlockPermanentEvents = true;
                guard.Accuracy = 30;
                guard.Weapons.Give(WeaponHash.Pistol, 60, true, true);
                guard.Task.StartScenario("WORLD_HUMAN_GUARD_STAND", guard.Position, 180f);

                _sentries.Add(Track(guard));
            }

            model.MarkAsNoLongerNeeded();
        }

        private void SpawnHauler()
        {
            var model = new Model("flatbed");
            if (!GameUtils.RequestModel(model)) return;

            _hauler = Track(World.CreateVehicle(model, Ctx.Locations.Position("M08.HaulerSpawn"),
                Ctx.Locations.Heading("M08.HaulerSpawn")));
            model.MarkAsNoLongerNeeded();
            if (_hauler == null || !_hauler.Exists()) return;

            _hauler.IsPersistent = true;

            var blip = Track(_hauler.AddBlip());
            blip.Sprite = BlipSprite.ArmoredTruck;
            blip.Color = BlipColor.Orange;
            blip.Name = "Turbine hauler";
        }

        private void SpawnTechnical()
        {
            var model = new Model("technical");
            var crewModel = new Model("s_m_y_blackops_01");
            if (!GameUtils.RequestModel(model) || !GameUtils.RequestModel(crewModel)) return;

            _technical = Track(World.CreateVehicle(model, _padOne + new Vector3(0f, 45f, 0f), 180f));
            if (_technical == null || !_technical.Exists()) return;
            _technical.IsPersistent = true;

            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            for (int seat = 0; seat < 2; seat++)
            {
                var crew = Track(World.CreatePed(crewModel, _technical.Position, 0f));
                if (crew == null || !crew.Exists()) continue;

                crew.RelationshipGroup = aegis;
                crew.IsPersistent = true;
                crew.BlockPermanentEvents = true;
                crew.Weapons.Give(WeaponHash.CarbineRifle, 200, true, true);
                crew.Task.WarpIntoVehicle(_technical, seat == 0 ? VehicleSeat.Driver : VehicleSeat.Passenger);
                if (seat == 0) crew.Task.VehicleChase(Game.Player.Character);
                else crew.Task.VehicleShootAtPed(Game.Player.Character);
            }

            model.MarkAsNoLongerNeeded();
            crewModel.MarkAsNoLongerNeeded();

            var blip = Track(_technical.AddBlip());
            blip.Sprite = BlipSprite.Enemy;
            blip.Color = BlipColor.Red;
            blip.Name = "Aegis technical";
        }

        protected override void OnCleanup()
        {
            _sentries.Clear();
        }
    }
}
