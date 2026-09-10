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
    /// M04 — "Severed Wire". Pillbox Hill and Textile City, 22:00, thunderstorm.
    ///
    /// Detective Miller is selling the dry-dock forensics to an Aegis handler in an
    /// underground garage. Gohan kills the lights, Ice takes the ramp, Miller runs for
    /// the Metro, and Guess chases him down.
    ///
    /// The subway plunge in the bible is a chase along the surface streets here: the
    /// tunnels are drivable but the entrances are not reliably enterable at speed, and
    /// a chase that ends on a curb is worse than one that never claimed to be
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
            if (!MissionSites.Ground(Ctx.Locations, "M04.GarageEntry", "M04.Breaker", "M04.RampGuards", "M04.ChaseCar")) return false;
            _breaker = Ctx.Locations.Position("M04.Breaker");
            _crashSite = Ctx.Locations.Position("M04.TextileCrash");

            var entry = Ctx.Locations.Position("M04.GarageEntry");
            if (!Ctx.Crew.Deploy(CrewSlot.Gohan, new Dictionary<CrewSlot, PedPlacement> { [CrewSlot.Gohan] = new PedPlacement(entry - new Vector3(15,10,0),0), [CrewSlot.Ice] = new PedPlacement(entry + new Vector3(0,18,0),180), [CrewSlot.Guess] = new PedPlacement(Ctx.Locations.Position("M04.ChaseCar"),250) })) return false;

            ApplyBibleSetting();
            Ctx.Abilities.Refill();

            SpawnMiller();
            SpawnBodyguards();
            SpawnChaseCar();
            if (_miller == null || !_miller.Exists() || _millerCar == null || !_millerCar.Exists() || _chaseCar == null || !_chaseCar.Exists() || _bodyguards.Count != 6) return false;
            foreach (var hero in Protagonist.All) Ctx.Crew.CompanionAI.TakeControl(hero.Slot);
            Ctx.Crew.PedFor(CrewSlot.Guess).SetIntoVehicle(_chaseCar, VehicleSeat.Driver);
            _miller.IsInvincible = true;
            _miller.SetIntoVehicle(_millerCar, VehicleSeat.Driver);
            // A getaway driver, not a commuter.
            Function.Call(Hash.SET_DRIVER_ABILITY, _miller, 1f);
            Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, _miller, 1f);
            return true;
        }

        /// <summary>
        /// Miller runs: a flee mission away from Guess at speed, through traffic and
        /// the wrong way down a street if that is what it takes, kept when the task
        /// system would otherwise drop it. The old cruise task obeyed lights.
        /// </summary>
        private void StartFlight()
        {
            if (_miller == null || !_miller.Exists() || _millerCar == null || !_millerCar.Exists()) return;
            var pursuer = Ctx.Crew.PedFor(CrewSlot.Guess);
            if (pursuer == null || !pursuer.Exists()) pursuer = Game.Player.Character;
            _millerCar.IsEngineRunning = true;
            _miller.Task.StartVehicleMission(_millerCar, pursuer, VehicleMissionType.Flee, 40f,
                VehicleDrivingFlags.DrivingModeAvoidVehiclesReckless | VehicleDrivingFlags.AllowGoingWrongWay | VehicleDrivingFlags.UseShortCutLinks, 8f, 40f, true);
            Function.Call(Hash.SET_PED_KEEP_TASK, _miller, true);
        }

        protected override void OnPassed()
        {
            // The chase car is Guess's ride out of Textile City and Miller's wreck is
            // part of the street now; neither vanishes with the mission's teardown.
            if (_chaseCar != null && _chaseCar.Exists()) Release(_chaseCar);
            if (_millerCar != null && _millerCar.Exists()) Release(_millerCar);
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Kill the lights",
                    new MissionInteraction("Gohan: cut the marked surface-lot breaker", () => _breaker, 5, 3f))
                .OwnedBy(CrewSlot.Gohan)
                .OnEnter(context => Say("M04_S1_01_GOHAN"))
                .OnExit(context => Say("M04_S1_02_ICE"));

            yield return new MissionStage("Take the ramp",
                    new KillTargetsObjective("Ice — clear Miller's escort.", () => _bodyguards))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(context =>
                {
                    // Power is cut; gameplay stays with the player.

                    foreach (var guard in _bodyguards)
                    {
                        if (guard != null && guard.Exists()) { guard.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_AEGIS"); guard.Task.FightAgainstHatedTargets(60f); }
                    }


                });

            yield return new MissionStage("Get after him",
                    new EnterVehicleObjective("Guess — get behind the wheel.", () => _chaseCar, VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context => Say("M04_S2_03_GUESS"));

            yield return new MissionStage("Run him down",
                    new PursueTargetObjective("Guess: chase the red marker. Disable Miller's car or stop Miller, then collect his drive.", () => _miller,
                        "Miller reached his handler and the forensics went with him."))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context => { Game.Player.WantedLevel = 2; _miller.IsInvincible = false; StartFlight(); Ctx.Cutscenes.PlayMoment(Id,"Miller breaks cover","GUESS","He is pulling out. Disable the car, then get the drive. I have the wheel.",_miller); Say("M04_S2_04_ICE"); });

            yield return new MissionStage("Recover the drive",
                    new MissionInteraction("Guess: collect Miller's drive", () => MillerPosition(), 3, 6f, animation: MissionInteraction.ReachInside))
                .OnExit(context =>
                {
                    Say("M04_S2_05_ICE");
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

            var aegis = World.AddRelationshipGroup("BLOODLINES_TRAFFIC");
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
