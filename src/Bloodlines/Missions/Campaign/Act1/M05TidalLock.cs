using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M05 — "Tidal Lock". Palomino Highlands shoreline, 03:30, storm.
    ///
    /// The Act I finale and the mission that turns the campaign: Mateo, cornered on a
    /// sandbar, gives up that Aegis engineered the whole thing to justify a
    /// forty-million-dollar defence contract.
    ///
    /// Faked per docs/FEASIBILITY.md: there is no sea-cave interior, so the grotto is
    /// the water at the cave mouth, and the floodlights are the men working them —
    /// killing the generator crew reads the same as shooting out the lamps and does
    /// not need a destructible prop the engine will not give us.
    /// </summary>
    public sealed class M05TidalLock : ComposedMission
    {
        private readonly List<Ped> _lightCrew = new List<Ped>();

        private Ped _mateo;
        private Vehicle _mateoBoat;
        private Vehicle _dinghy;
        private Vector3 _perch;
        private Vector3 _cove;
        private Vector3 _grotto;
        private Vector3 _sandbar;

        public override string Id => "M05";
        public override string Title => "Tidal Lock";

        protected override bool Setup()
        {
            _perch = Ctx.Locations.Position("M05.CliffPerch");
            _cove = Ctx.Locations.Position("M05.CoveAir");
            _grotto = Ctx.Locations.Position("M05.GrottoMouth");
            _sandbar = Ctx.Locations.Position("M05.Sandbar");

            if (!Ctx.Crew.Deploy(CrewSlot.Ice, _perch, Ctx.Locations.Heading("M05.CliffPerch"))) return false;

            ApplyBibleSetting();
            Game.Player.Character.Weapons.Give(WeaponHash.SniperRifle, 60, true, true);

            SpawnLightCrew();
            SpawnMateo();
            SpawnDinghy();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Cliff overwatch",
                    new KillTargetsObjective("Ice — take the generator crew off the cave mouth.",
                        () => _lightCrew))
                .OwnedBy(CrewSlot.Ice)
                .WithDialogue(1);

            yield return new MissionStage("Light the cove",
                    new ReachZoneObjective("Guess — get over the cove and drop flares.", () => _cove, 45f,
                        flat: true))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context =>
                {
                    // The flares are the light change, not a prop: the storm lifting over
                    // the cove is what the player actually reads.
                    GameUtils.SetWeather("CLEARING");
                    GameUtils.Subtitle("~y~Magnesium flares away. The cove is lit up like midday.", 4000);
                });

            yield return new MissionStage("Breach the grotto",
                    new ReachZoneObjective("Gohan — punch through the surf into the grotto.", () => _grotto, 12f))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(context =>
                {
                    if (_mateo != null && _mateo.Exists() && _mateoBoat != null && _mateoBoat.Exists())
                    {
                        _mateo.Task.CruiseWithVehicle(_mateoBoat, 30f, DrivingStyle.Rushed);
                    }
                });

            yield return new MissionStage("Run him to the sandbar",
                    new PursueTargetObjective("Run Mateo down before he clears the point.", () => _mateo,
                        "Mateo made open water."))
                .OnEnter(context => GameUtils.Subtitle("~y~He's running for open water.", 3000));

            yield return new MissionStage("The revelation",
                    new ReachZoneObjective("Get to Mateo.", () => MateoPosition(), 8f))
                .WithDialogue(2)
                .OnExit(context =>
                {
                    // Act I ends on information, not a kill: Aegis bought the city council
                    // and needed a three-man ghost squad to justify the contract.
                    context.State.CashOnHand += 50000;
                    GameUtils.Subtitle("~y~Aegis built this. All of it. And they're not finished.", 6000);
                });
        }

        private Vector3 MateoPosition()
        {
            return _mateo != null && _mateo.Exists() ? _mateo.Position : _sandbar;
        }

        private void SpawnLightCrew()
        {
            var model = new Model("g_m_y_mexgoon_02");
            if (!GameUtils.RequestModel(model)) return;

            var cartel = World.AddRelationshipGroup("BLOODLINES_CARTEL");

            for (int i = 0; i < 4; i++)
            {
                var guard = World.CreatePed(model, _grotto + new Vector3(-6f + i * 4f, 6f, 1.5f), 0f);
                if (guard == null || !guard.Exists()) continue;

                guard.RelationshipGroup = cartel;
                guard.IsPersistent = true;
                guard.BlockPermanentEvents = true;
                guard.Accuracy = 30;
                guard.Weapons.Give(WeaponHash.MicroSMG, 150, true, true);
                guard.Task.GuardCurrentPosition();

                _lightCrew.Add(Track(guard));
            }

            model.MarkAsNoLongerNeeded();
        }

        private void SpawnMateo()
        {
            var model = new Model("g_m_m_mexboss_01");
            var boatModel = new Model("tropic");
            if (!GameUtils.RequestModel(model) || !GameUtils.RequestModel(boatModel)) return;

            _mateoBoat = Track(World.CreateVehicle(boatModel, _grotto + new Vector3(0f, -14f, 0f), 45f));
            _mateo = Track(World.CreatePed(model, _grotto + new Vector3(2f, -12f, 0f), 45f));
            model.MarkAsNoLongerNeeded();
            boatModel.MarkAsNoLongerNeeded();

            if (_mateo == null || !_mateo.Exists()) return;

            _mateo.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_CARTEL");
            _mateo.IsPersistent = true;
            _mateo.BlockPermanentEvents = true;
            _mateo.Armor = 100;
            _mateo.Weapons.Give(WeaponHash.APPistol, 100, true, true);

            var blip = Track(_mateo.AddBlip());
            blip.Sprite = BlipSprite.Enemy;
            blip.Color = BlipColor.Red;
            blip.Name = "Mateo Cifuentes";

            if (_mateoBoat != null && _mateoBoat.Exists())
            {
                _mateoBoat.IsPersistent = true;
                _mateo.Task.WarpIntoVehicle(_mateoBoat, VehicleSeat.Driver);
            }
        }

        private void SpawnDinghy()
        {
            var model = new Model("dinghy");
            if (!GameUtils.RequestModel(model)) return;

            _dinghy = Track(World.CreateVehicle(model, Ctx.Locations.Position("M05.DinghySpawn"), 45f));
            model.MarkAsNoLongerNeeded();
            if (_dinghy == null || !_dinghy.Exists()) return;

            _dinghy.IsPersistent = true;

            var blip = Track(_dinghy.AddBlip());
            blip.Sprite = BlipSprite.Boat;
            blip.Color = BlipColor.Green;
            blip.Name = "Stealth dinghy";
        }

        protected override void OnCleanup()
        {
            _lightCrew.Clear();
        }
    }
}
