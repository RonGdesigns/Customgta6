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
    /// M16 — "The Heavy Lift". Fort Zancudo outer logistics, 03:00, high winds.
    ///
    /// The Cargobob the Port Heist needs. The crew walks into a military base because
    /// of the IFF transponder Ice ripped out of a convoy truck in M09 — so the base
    /// does not light up until they take the helipad, and the mission says so by
    /// suppressing the wanted level until the shooting starts.
    ///
    /// That continuity is the point: M09 is the reason this mission is survivable,
    /// and the player should feel the thing they stole being spent.
    /// </summary>
    public sealed class M16TheHeavyLift : ComposedMission
    {
        private readonly List<Ped> _militaryPolice = new List<Ped>();

        private Vehicle _cargobob;
        private Vector3 _fence;
        private Vector3 _helipad;
        private Vector3 _canyon;
        private Vector3 _terminal;

        public override string Id => "M16";
        public override string Title => "The Heavy Lift";

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _fence = Ctx.Locations.Position("M16.DepotFence");
            _helipad = Ctx.Locations.Position("M16.Helipad");
            _canyon = Ctx.Locations.Position("M16.CanyonRun");
            _terminal = Ctx.Locations.Position("M16.TerminalDrop");

            if (!Ctx.Crew.Deploy(CrewSlot.Ice, _fence, Ctx.Locations.Heading("M16.DepotFence")))
            {
                return false;
            }

            ApplyBibleSetting();

            // The transponder from M09, spent: Zancudo reads them as friendly until
            // they start shooting.
            Function.Call(Hash.SET_MAX_WANTED_LEVEL, 0);
            Game.Player.WantedLevel = 0;

            SpawnMilitaryPolice();
            SpawnCargobob();
            if (!RequireAssets(_cargobob)) return false;
            Station(CrewSlot.Guess, _fence + new Vector3(12f, -10f, 0f));
            Station(CrewSlot.Gohan, _fence + new Vector3(-12f, -10f, 0f));
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Walk in",
                    new ReachZoneObjective("Ice — cross the outer depot on the transponder.",
                        () => _helipad, 30f, flat: true))
                .OwnedBy(CrewSlot.Ice)
                
                .OnEnter(context =>
                    GameUtils.Subtitle("~y~Code 7-Echo-Victor. They read you as one of theirs — until you fire.", 6000));

            yield return new MissionStage("Take the helipad",
                    new KillTargetsObjective("Clear the military police off the pad.",
                        () => _militaryPolice))
                .OnEnter(context =>
                {
                    // The clearance is burnt the moment a shot is fired.
                    Function.Call(Hash.SET_MAX_WANTED_LEVEL, 5);
                    Game.Player.WantedLevel = 4;
                    foreach (var police in _militaryPolice)
                    {
                        if (police != null && police.Exists()) { police.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_AEGIS"); police.Task.FightAgainstHatedTargets(90f); }
                    }
                })
                .AfterCues("M16_S1_01_ICE");

            yield return new MissionStage("Spool the twins",
                    new EnterVehicleObjective("Guess — take the Cargobob.", () => _cargobob,
                        VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess);

            yield return new MissionStage("Raton Canyon",
                    new AltitudeCeilingObjective("Hug the canyon — stay under 60 metres above terrain.", 60f,
                        "A Lazer got a lock in open sky."),
                    new DeliverVehicleObjective("Guess: fly the Cargobob through the marked canyon route.", () => _cargobob, () => _canyon, 120f))
                
                .WithCues("M16_S1_02_GUESS");

            yield return new MissionStage("Terminal Island",
                    new DeliverVehicleObjective("Put the Cargobob down at Terminal Island.",
                        () => _cargobob, () => _terminal, 40f, land: true))
                .OnExit(context =>
                {
                    Game.Player.WantedLevel = 0;
                    GameUtils.Subtitle("~g~Heavy lift secured. Berth 44 is now a question of timing.", 5000);
                })
                .WithCues("M16_S1_03_ICE");
        }

        private void SpawnMilitaryPolice()
        {
            var model = new Model("s_m_y_marine_03");
            if (!GameUtils.RequestModel(model)) return;

            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");

            for (int i = 0; i < 6; i++)
            {
                var trooper = World.CreatePed(model, _helipad + new Vector3(-12f + i * 5f, 8f, 0f), 200f);
                if (trooper == null || !trooper.Exists()) continue;

                trooper.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_TRAFFIC");
                trooper.IsPersistent = true;
                trooper.BlockPermanentEvents = true;
                trooper.Accuracy = 40;
                trooper.Armor = 60;
                trooper.Weapons.Give(WeaponHash.CarbineRifle, 250, true, true);
                trooper.Task.GuardCurrentPosition();

                _militaryPolice.Add(Track(trooper));
            }

            model.MarkAsNoLongerNeeded();
        }

        private void SpawnCargobob()
        {
            var model = new Model("cargobob");
            if (!GameUtils.RequestModel(model)) return;

            _cargobob = Track(World.CreateVehicle(model, Ctx.Locations.Position("M16.CargobobSpawn"),
                Ctx.Locations.Heading("M16.CargobobSpawn")));
            model.MarkAsNoLongerNeeded();
            if (_cargobob == null || !_cargobob.Exists()) return;

            _cargobob.IsPersistent = true;

            var blip = Track(_cargobob.AddBlip());
            blip.Sprite = BlipSprite.Helicopter;
            blip.Color = BlipColor.Orange;
            blip.Name = "Cargobob";
        }

        protected override void OnCleanup()
        {
            // Never leave the player's wanted ceiling where a mission put it.
            Function.Call(Hash.SET_MAX_WANTED_LEVEL, 5);
            _militaryPolice.Clear();
        }
    }
}
