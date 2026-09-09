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
            if (!MissionSites.Ground(Ctx.Locations, "M03.RailJunction", "M03.DepotGate", "M03.CraneControls", "M03.HaulerSpawn")) return false;
            _junction = Ctx.Locations.Position("M03.RailJunction");
            _depot = Ctx.Locations.Position("M03.DepotGate");
            _crane = Ctx.Locations.Position("M03.CraneControls");
            _base = Ctx.Locations.Position("Base.CypressFlats");

            if (!Ctx.Crew.Deploy(CrewSlot.Guess, new Dictionary<CrewSlot, PedPlacement> { [CrewSlot.Guess] = new PedPlacement(_junction - new Vector3(0,20,0),90), [CrewSlot.Ice] = new PedPlacement(_depot - new Vector3(0,35,0),0), [CrewSlot.Gohan] = new PedPlacement(_crane - new Vector3(0,25,0),0) }))
            {
                return false;
            }

            ApplyBibleSetting();
            SpawnDepotGuards();
            SpawnHauler();
            var approachModel=new Model("primo");
            if(!GameUtils.RequestModel(approachModel)) return false;
            var road=World.GetNextPositionOnStreet(_junction-new Vector3(0,25,0));
            if(road==Vector3.Zero || road.DistanceTo(_junction)>60f){approachModel.MarkAsNoLongerNeeded();return false;}
            var approach=Track(World.CreateVehicle(approachModel,road,90));approachModel.MarkAsNoLongerNeeded();
            if(approach==null||!approach.Exists())return false;
            approach.IsPersistent=true;Ctx.Crew.PedFor(CrewSlot.Guess).SetIntoVehicle(approach,VehicleSeat.Driver);
            foreach (var hero in Protagonist.All) Ctx.Crew.CompanionAI.TakeControl(hero.Slot);
            return _hauler != null && _hauler.Exists() && _guards.Count == 8;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            // Guess, alone at the junction: the rest of the crew is across the city.
            yield return new MissionStage("Seal the response routes",
                    new MissionInteraction("Guess: lock the rail junction", () => _junction, 6, 4f))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context => context.Crew.CompanionsHoldPosition = true)
                .OnExit(context => Say("M03_S1_01_GUESS"));

            yield return new MissionStage("Breach the depot",
                    new ReachZoneObjective("Ice: walk into the yellow ENTRY marker at the depot. No ability or button is needed.", () => _depot, 12f, flat: true))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(context => Say("M03_S1_02_ICE"));

            yield return new MissionStage("Clear the yard",
                    new KillTargetsObjective("Ice: eliminate the guards marked RED in the container yard. Gohan waits until it is clear.", () => _guards))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(context =>
                {
                    Say("M03_S2_03_ICE");
                    foreach (var guard in _guards)
                    {
                        if (guard != null && guard.Exists()) { guard.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_CARTEL"); guard.Task.FightAgainstHatedTargets(120f); }
                    }
                });

            yield return new MissionStage("Hoist the container",
                    new MissionInteraction("Gohan: load the weapons at the yellow cargo terminal", () => _crane, 8, 3.5f))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(context => { Say("M03_S2_04_GOHAN"); GameUtils.Subtitle("~g~Weapons loaded into the orange-marked Benson.", 4000); });

            yield return new MissionStage("Run it home",
                    new EnterVehicleObjective("Guess: travel to the depot and take the orange-marked Benson truck (driver seat).", () => _hauler, VehicleSeat.Driver),
                    new ProtectObjective("", () => _hauler, "The hauler was destroyed."))
                .OwnedBy(CrewSlot.Guess);

            yield return new MissionStage("Cypress Flats",
                    new OccupiedVehicleDestination("Guess: deliver the Benson weapons truck to the yellow foundry marker.", () => _hauler, () => _base, 25f),
                    new ProtectObjective("", () => _hauler, "The hauler was destroyed."))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context => { Game.Player.WantedLevel = 2; Say("M03_S2_05_GUESS"); })
                .OnExit(context =>
                {
                    Game.Player.WantedLevel = 0;
                    // The foundry is the crew's Act I base — opening it here is what the
                    // mission is actually for.
                    /* Awarded once by CampaignState.MarkComplete after the mission passes. */
                    GameUtils.Subtitle("~g~The foundry is operational.", 5000);
                });
        }

        private void SpawnDepotGuards()
        {
            var cartel = World.AddRelationshipGroup("BLOODLINES_TRAFFIC");

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
