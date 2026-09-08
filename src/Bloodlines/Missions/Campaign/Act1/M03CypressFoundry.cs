using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M03 — "Cypress Foundry". Cypress Flats and the Murrieta oil fields, 14:00.
    ///
    /// The crew's first joint operation as a crew: Guess seals the police response
    /// routes at the Davis rail spur while Ice and Gohan take a cartel container depot
    /// and put its contents on a hauler.
    ///
    /// Two substitutions, both logged in docs/FEASIBILITY.md: uncoupling a forty-car
    /// freight train is not exposed to script, so the blockade is a hold at the
    /// junction; and the gantry crane is a hold at its controls rather than a
    /// driveable crane. Both are the same beat from the player's side — be here, do
    /// the work, take the consequences — and neither needs a system the engine lacks.
    /// </summary>
    public sealed class M03CypressFoundry : ComposedMission
    {
        private static readonly string[] DepotGuards = { "g_m_y_mexgoon_01", "g_m_y_mexgang_01", "g_m_m_armboss_01" };

        private readonly List<Ped> _guards = new List<Ped>();

        private Vehicle _hauler;
        private Vector3 _junction;
        private Vector3 _depot;
        private Vector3 _crane;
        private Vector3 _base;

        public override string Id => "M03";
        public override string Title => "Cypress Foundry";

        protected override bool Setup()
        {
            _junction = Ctx.Locations.Position("M03.RailJunction");
            _depot = Ctx.Locations.Position("M03.DepotGate");
            _crane = Ctx.Locations.Position("M03.CraneControls");
            _base = Ctx.Locations.Position("Base.CypressFlats");

            if (!Ctx.Crew.Deploy(CrewSlot.Guess, _junction, Ctx.Locations.Heading("M03.RailJunction")))
            {
                return false;
            }

            ApplyBibleSetting();
            SpawnDepotGuards();
            SpawnHauler();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            // Guess, alone at the junction: the rest of the crew is across the city.
            yield return new MissionStage("Seal the response routes",
                    new HoldZoneObjective("Guess — trip the junction switch.", () => _junction, 6, 4f,
                        "Decoupling"))
                .OwnedBy(CrewSlot.Guess)
                .WithDialogue(1)
                .OnEnter(context => context.Crew.CompanionsHoldPosition = true);

            yield return new MissionStage("Breach the depot",
                    new ReachZoneObjective("Ice — breach the Murrieta depot gate.", () => _depot, 12f, flat: true))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(context =>
                {
                    // The crew converges: from here they are working the same ground.
                    context.Crew.CompanionsHoldPosition = false;
                    foreach (var protagonist in Protagonist.All)
                    {
                        var ped = context.Crew.PedFor(protagonist.Slot);
                        if (ped == null || protagonist.Slot == CrewSlot.Guess) continue;
                        ped.Position = _depot + new Vector3((int)protagonist.Slot * 3f - 3f, -14f, 0f);
                    }
                });

            yield return new MissionStage("Clear the yard",
                    new KillTargetsObjective("Clear the container yard.", () => _guards))
                .WithDialogue(2)
                .OnEnter(context =>
                {
                    foreach (var guard in _guards)
                    {
                        if (guard != null && guard.Exists()) guard.Task.FightAgainstHatedTargets(120f);
                    }
                });

            yield return new MissionStage("Hoist the container",
                    new HoldZoneObjective("Gohan — work the gantry crane.", () => _crane, 8, 3.5f,
                        "Hoisting the container"))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(context => GameUtils.Subtitle("~g~Container seated on the hauler.", 4000));

            yield return new MissionStage("Run it home",
                    new EnterVehicleObjective("Guess — take the hauler.", () => _hauler, VehicleSeat.Driver),
                    new ProtectObjective("", () => _hauler, "The hauler was destroyed."))
                .OwnedBy(CrewSlot.Guess);

            yield return new MissionStage("Cypress Flats",
                    new ReachZoneObjective("Get the hauler back to Cypress Flats.", () => _base, 25f,
                        flat: true, requireVehicle: true),
                    new ProtectObjective("", () => _hauler, "The hauler was destroyed."))
                .OnEnter(context => Game.Player.WantedLevel = 2)
                .OnExit(context =>
                {
                    Game.Player.WantedLevel = 0;
                    // The foundry is the crew's Act I base — opening it here is what the
                    // mission is actually for.
                    context.State.Unlock("cypressFoundry");
                    GameUtils.Subtitle("~g~The foundry is operational.", 5000);
                });
        }

        private void SpawnDepotGuards()
        {
            var cartel = World.AddRelationshipGroup("BLOODLINES_CARTEL");

            for (int i = 0; i < 8; i++)
            {
                var model = new Model(DepotGuards[i % DepotGuards.Length]);
                if (!GameUtils.RequestModel(model)) continue;

                var offset = new Vector3(-12f + i * 4f, 6f + (i % 3) * 9f, 0f);
                var guard = World.CreatePed(model, _depot + offset, 180f);
                model.MarkAsNoLongerNeeded();
                if (guard == null || !guard.Exists()) continue;

                guard.RelationshipGroup = cartel;
                guard.IsPersistent = true;
                guard.BlockPermanentEvents = true;
                guard.Accuracy = 35;
                guard.Armor = 25;
                guard.Weapons.Give(i % 3 == 0 ? WeaponHash.AssaultRifle : WeaponHash.MicroSMG, 200, true, true);
                guard.Task.GuardCurrentPosition();

                _guards.Add(Track(guard));
            }
        }

        private void SpawnHauler()
        {
            // A box truck rather than a tractor and trailer: script-attached trailers
            // come apart under fire and take the mission with them.
            var model = new Model("benson");
            if (!GameUtils.RequestModel(model)) return;

            _hauler = Track(World.CreateVehicle(model, Ctx.Locations.Position("M03.HaulerSpawn"),
                Ctx.Locations.Heading("M03.HaulerSpawn")));
            model.MarkAsNoLongerNeeded();
            if (_hauler == null || !_hauler.Exists()) return;

            _hauler.IsPersistent = true;
            _hauler.IsEngineRunning = false;

            var blip = Track(_hauler.AddBlip());
            blip.Sprite = BlipSprite.ArmoredTruck;
            blip.Color = BlipColor.Orange;
            blip.Name = "Weapons hauler";
        }

        protected override void OnCleanup()
        {
            Ctx.Crew.CompanionsHoldPosition = false;
            _guards.Clear();
        }
    }
}
