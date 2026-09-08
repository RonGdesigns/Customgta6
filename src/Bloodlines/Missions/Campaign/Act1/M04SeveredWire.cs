using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M04 — "Severed Wire". Pillbox Hill and Textile City, 22:00, thunderstorm.
    ///
    /// Detective Miller is selling the dry-dock forensics to an Aegis handler in an
    /// underground garage. Gohan kills the lights, Ice takes the ramp, Miller runs for
    /// the Metro, and Guess chases him down.
    ///
    /// The subway plunge in the bible is a chase along the surface streets here: the
    /// tunnels are drivable but the entrances are not reliably enterable at speed, and
    /// a chase that ends on a kerb is worse than one that never claimed to be
    /// underground. The beat that matters — Miller runs, Guess catches him — survives.
    /// </summary>
    public sealed class M04SeveredWire : ComposedMission
    {
        private readonly List<Ped> _bodyguards = new List<Ped>();

        private Ped _miller;
        private Vehicle _millerCar;
        private Vehicle _chaseCar;
        private Vector3 _breaker;
        private Vector3 _crashSite;

        public override string Id => "M04";
        public override string Title => "Severed Wire";

        protected override bool Setup()
        {
            _breaker = Ctx.Locations.Position("M04.Breaker");
            _crashSite = Ctx.Locations.Position("M04.TextileCrash");

            var entry = Ctx.Locations.Position("M04.GarageEntry");
            if (!Ctx.Crew.Deploy(CrewSlot.Gohan, entry, 0f)) return false;

            ApplyBibleSetting();
            Ctx.Abilities.Refill();

            SpawnMiller();
            SpawnBodyguards();
            SpawnChaseCar();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Kill the lights",
                    new HoldZoneObjective("Gohan — cut the transformer breaker on B3.", () => _breaker, 5, 3f,
                        "Cutting the feeder"))
                .OwnedBy(CrewSlot.Gohan)
                .WithDialogue(1)
                .OnEnter(context =>
                    GameUtils.Subtitle("~y~Thermal Pulse (" + context.Config.AbilityKey + ") sees through the dark.", 5000));

            yield return new MissionStage("Take the ramp",
                    new KillTargetsObjective("Ice — clear Miller's escort.", () => _bodyguards))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(context =>
                {
                    GameUtils.FadeOut(400);
                    Script.Wait(450);
                    GameUtils.FadeIn(600);

                    foreach (var guard in _bodyguards)
                    {
                        if (guard != null && guard.Exists()) guard.Task.FightAgainstHatedTargets(60f);
                    }

                    // Miller does not stay for the firefight — that is the whole mission.
                    if (_miller != null && _miller.Exists() && _millerCar != null && _millerCar.Exists())
                    {
                        _miller.Task.WarpIntoVehicle(_millerCar, VehicleSeat.Driver);
                        _miller.Task.CruiseWithVehicle(_millerCar, 35f, DrivingStyle.Rushed);
                    }
                });

            yield return new MissionStage("Get after him",
                    new EnterVehicleObjective("Guess — get behind the wheel.", () => _chaseCar, VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess)
                .WithDialogue(2);

            yield return new MissionStage("Run him down",
                    new PursueTargetObjective("Run Miller off the road.", () => _miller,
                        "Miller reached his handler and the forensics went with him."))
                .OnEnter(context => Game.Player.WantedLevel = 2);

            yield return new MissionStage("Recover the drive",
                    new ReachZoneObjective("Take the drive off Miller.", () => MillerPosition(), 6f))
                .OnExit(context =>
                {
                    Game.Player.WantedLevel = 0;
                    GameUtils.Subtitle("~g~Drive secured. Miller won't be talking to Aegis again.", 5000);
                });
        }

        private Vector3 MillerPosition()
        {
            return _miller != null && _miller.Exists() ? _miller.Position : _crashSite;
        }

        private void SpawnMiller()
        {
            var model = new Model("s_m_m_ciasec_01");
            if (!GameUtils.RequestModel(model)) return;

            var spot = Ctx.Locations.Position("M04.RampGuards") + new Vector3(4f, 0f, 0f);
            _miller = Track(World.CreatePed(model, spot, 180f));
            model.MarkAsNoLongerNeeded();
            if (_miller == null || !_miller.Exists()) return;

            _miller.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            _miller.IsPersistent = true;
            _miller.BlockPermanentEvents = true;
            _miller.Armor = 60;
            _miller.Weapons.Give(WeaponHash.Pistol, 60, true, true);

            var blip = Track(_miller.AddBlip());
            blip.Sprite = BlipSprite.Enemy;
            blip.Color = BlipColor.Red;
            blip.Name = "Det. Miller";

            var carModel = new Model("fugitive");
            if (!GameUtils.RequestModel(carModel)) return;

            _millerCar = Track(World.CreateVehicle(carModel, spot + new Vector3(6f, 0f, 0f), 180f));
            carModel.MarkAsNoLongerNeeded();
            if (_millerCar != null) _millerCar.IsPersistent = true;
        }

        private void SpawnBodyguards()
        {
            var model = new Model("s_m_m_highsec_02");
            if (!GameUtils.RequestModel(model)) return;

            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            var post = Ctx.Locations.Position("M04.RampGuards");

            for (int i = 0; i < 6; i++)
            {
                var guard = World.CreatePed(model, post + new Vector3(-6f + i * 2.5f, (i % 2) * 4f, 0f), 180f);
                if (guard == null || !guard.Exists()) continue;

                guard.RelationshipGroup = aegis;
                guard.IsPersistent = true;
                guard.BlockPermanentEvents = true;
                guard.Accuracy = 35;
                guard.Armor = 40;
                guard.Weapons.Give(WeaponHash.CarbineRifle, 150, true, true);
                guard.Task.GuardCurrentPosition();

                _bodyguards.Add(Track(guard));
            }

            model.MarkAsNoLongerNeeded();
        }

        private void SpawnChaseCar()
        {
            var model = new Model("buffalo3");
            if (!GameUtils.RequestModel(model)) return;

            _chaseCar = Track(World.CreateVehicle(model, Ctx.Locations.Position("M04.ChaseCar"),
                Ctx.Locations.Heading("M04.ChaseCar")));
            model.MarkAsNoLongerNeeded();
            if (_chaseCar == null || !_chaseCar.Exists()) return;

            _chaseCar.IsPersistent = true;

            var blip = Track(_chaseCar.AddBlip());
            blip.Sprite = BlipSprite.PersonalVehicleCar;
            blip.Color = BlipColor.Orange;
            blip.Name = "Chase car";
        }

        protected override void OnCleanup()
        {
            _bodyguards.Clear();
        }
    }
}
