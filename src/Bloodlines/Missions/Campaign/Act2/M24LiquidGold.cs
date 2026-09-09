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
    /// M24 — "Liquid Gold". Alamo Sea shallows, 14:00, harsh sun.
    ///
    /// The bullion has been sitting in four feet of water since M22. The crew needs
    /// operating capital, so five tons of it comes back out on a crane truck while
    /// Blaine County sheriff's deputies decide this looks like a shakedown worth
    /// making.
    ///
    /// The save is the continuity here: M22 put thirty tons in the Alamo, this takes
    /// five, and AlamoGoldDredgedTons carries the number forward for the rest of the
    /// campaign.
    /// </summary>
    public sealed class M24LiquidGold : ComposedMission
    {
        private readonly List<Ped> _deputies = new List<Ped>();

        private Vehicle _crane;
        private Vector3 _dredge;
        private Vector3 _ridge;
        private Vector3 _bunker;

        public override string Id => "M24";
        public override string Title => "Liquid Gold";

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _dredge = Ctx.Locations.Position("M24.CraneSpawn") + new Vector3(-12f, 0f, 0f);
            _ridge = Ctx.Locations.Position("M24.RidgeLine");
            _bunker = Ctx.Locations.Position("M23.BunkerDoor");

            if (!Ctx.Crew.Deploy(CrewSlot.Guess, Ctx.Locations.Position("M24.CraneSpawn"),
                    Ctx.Locations.Heading("M24.CraneSpawn")))
            {
                return false;
            }

            ApplyBibleSetting();
            SpawnCrane();
            if (!RequireAssets(_crane)) return false;
            Station(CrewSlot.Ice, _ridge + new Vector3(0f, -25f, 0f));
            Station(CrewSlot.Gohan, _dredge + new Vector3(15f, -10f, 0f));
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Into the shallows",
                    new DeliverVehicleObjective("Guess — park the recovery truck at the dry shoreline marker.",
                        () => _crane, () => _dredge, 18f))
                .OwnedBy(CrewSlot.Guess)
                ;

            // The dredge and the shakedown run together: Ice is holding the ridge for
            // exactly as long as the cable takes.
            yield return new MissionStage("Dredge the crates",
                    new AssignedWorkObjective("Guess works the recovery cable. Ice: cover him from the ridge.", CrewSlot.Guess, () => _dredge, 22),
                    new SurviveWavesObjective("Ice — keep the deputies off the haul.", SpawnDeputyWave, 2, 5000) { RequiredCharacter = CrewSlot.Ice },
                    new ProtectObjective("", () => _crane, "The crane truck was destroyed."))
                
                .OnExit(context =>
                {
                    /* Awarded once by CampaignState.MarkComplete after the mission passes. */
                    /* Awarded once by CampaignState.MarkComplete after the mission passes. */
                    GameUtils.Subtitle("~g~Five tons up. Twenty-five still in the mud.", 5000);
                })
                .AfterCues("M24_S1_01_GUESS")
                .WithCues("M24_S1_02_ICE");

            yield return new MissionStage("Back to the bunker",
                    new DeliverVehicleObjective("Get the haul to the radar base.",
                        () => _crane, () => _bunker, 35f) { RequiredCharacter = CrewSlot.Guess },
                    new ProtectObjective("", () => _crane, "The crane truck was destroyed."))
                .OnExit(context => GameUtils.Subtitle("~g~Cash reserves replenished for Blaine operations.", 5000))
                .AfterCues("M24_S1_03_GOHAN");
        }

        private IEnumerable<Ped> SpawnDeputyWave(int wave)
        {
            var pedModel = new Model("s_m_y_sheriff_01");
            var carModel = new Model("sheriff2");
            if (!GameUtils.RequestModel(pedModel)) return Enumerable.Empty<Ped>();

            var law = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            var spawned = new List<Ped>();

            for (int i = 0; i < 2 + wave; i++)
            {
                var post = _ridge + new Vector3(-10f + i * 6f, 4f, 0f);
                var deputy = World.CreatePed(pedModel, post, 200f);
                if (deputy == null || !deputy.Exists()) continue;

                deputy.RelationshipGroup = law;
                deputy.IsPersistent = true;
                deputy.BlockPermanentEvents = true;
                deputy.Accuracy = 30;
                deputy.Armor = 30;
                deputy.Weapons.Give(WeaponHash.PumpShotgun, 120, true, true);
                deputy.Task.FightAgainstHatedTargets(120f);

                spawned.Add(Track(deputy));
                _deputies.Add(deputy);
            }

            if (GameUtils.RequestModel(carModel))
            {
                var cruiser = Track(World.CreateVehicle(carModel, _ridge + new Vector3(0f, 14f, 0f), 200f));
                if (cruiser != null) cruiser.IsPersistent = true;
                carModel.MarkAsNoLongerNeeded();
            }

            pedModel.MarkAsNoLongerNeeded();
            return spawned;
        }

        private void SpawnCrane()
        {
            var model = new Model("flatbed");
            if (!GameUtils.RequestModel(model)) return;

            _crane = Track(World.CreateVehicle(model, Ctx.Locations.Position("M24.CraneSpawn"),
                Ctx.Locations.Heading("M24.CraneSpawn")));
            model.MarkAsNoLongerNeeded();
            if (_crane == null || !_crane.Exists()) return;

            _crane.IsPersistent = true;

            var blip = Track(_crane.AddBlip());
            blip.Sprite = BlipSprite.ArmoredTruck;
            blip.Color = BlipColor.Orange;
            blip.Name = "Crane truck";
        }

        protected override void OnCleanup()
        {
            _deputies.Clear();
        }
    }
}
