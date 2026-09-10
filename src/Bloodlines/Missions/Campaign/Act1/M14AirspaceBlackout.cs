using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M14 — "Airspace Blackout". Sandy Shores auxiliary field, 18:00, sunset.
    ///
    /// Guess steals an Aegis ground-attack plane with a radar-jamming pod on the
    /// pylon; Ice covers the apron from a ridge with a thermal rifle. Getting out
    /// means staying under the SAM envelope all the way to McKenzie.
    ///
    /// The escape is the mission. Flying low is a skill the game already has and the
    /// campaign has never asked for — a ceiling turns cruising altitude into a
    /// decision instead of a default.
    /// </summary>
    public sealed class M14AirspaceBlackout : ComposedMission
    {
        private readonly List<Ped> _apronGuards = new List<Ped>();

        private Vehicle _plane;
        private Vector3 _ridge;
        private Vector3 _hangar;
        private Vector3 _mckenzie;

        public override string Id => "M14";
        public override string Title => "Airspace Blackout";

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _ridge = Ctx.Locations.Position("M14.OverwatchRidge");
            _hangar = Ctx.Locations.Position("M14.HangarDoor");
            _mckenzie = Ctx.Locations.Position("M14.McKenzieHangar");

            if (!Ctx.Crew.Deploy(CrewSlot.Ice, _ridge, Ctx.Locations.Heading("M14.OverwatchRidge")))
            {
                return false;
            }

            ApplyBibleSetting();
            Game.Player.Character.Weapons.Give(WeaponHash.HeavySniper, 40, true, true);

            SpawnApronGuards();
            SpawnPlane();
            if (!RequireAssets(_plane)) return false;
            Station(CrewSlot.Guess, _hangar + new Vector3(0f, -30f, 0f));
            Station(CrewSlot.Gohan, _ridge + new Vector3(15f, 0f, 0f));
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Overwatch",
                    new KillTargetsObjective("Ice — clear the apron from the ridge.", () => _apronGuards))
                .OwnedBy(CrewSlot.Ice)
                ;

            yield return new MissionStage("The hangar",
                    new ReachZoneObjective("Guess — get to the hangar door.", () => _hangar, 8f))
                .OwnedBy(CrewSlot.Guess);

            yield return new MissionStage("Hotwire",
                    new EnterVehicleObjective("Guess: take the marked jammer aircraft.",
                        () => _plane, VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess)
                .WithCues("M14_S1_01_GUESS");

            // The whole flight home is the objective: climb and the SAMs get a lock.
            yield return new MissionStage("Under the radar",
                    new AltitudeCeilingObjective("Hug the terrain — stay under 50 meters above the terrain.", 50f,
                        "A SAM battery locked on and took the plane down."),
                    new DeliverVehicleObjective("Guess: land the jammer aircraft at McKenzie and stop.", () => _plane, () => _mckenzie, 60f, land: true))
                
                .OnExit(context =>
                {
                    /* Awarded once by CampaignState.MarkComplete after the mission passes. */
                    GameUtils.Subtitle("~g~Pod jammer secured. Aegis radar has a hole in it now.", 5000);
                })
                .WithCues("M14_S1_02_ICE")
                .AfterCues("M14_S1_03_GUESS");
        }

        private void SpawnApronGuards()
        {
            var model = new Model("s_m_y_blackops_02");
            if (!GameUtils.RequestModel(model)) return;

            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");

            for (int i = 0; i < 5; i++)
            {
                var guard = World.CreatePed(model, _hangar + new Vector3(-10f + i * 5f, 6f, 0f), 300f);
                if (guard == null || !guard.Exists()) continue;

                guard.RelationshipGroup = aegis;
                guard.IsPersistent = true;
                guard.BlockPermanentEvents = true;
                guard.Accuracy = 35;
                guard.Armor = 50;
                guard.Weapons.Give(WeaponHash.CarbineRifle, 200, true, true);
                guard.Task.GuardCurrentPosition();

                _apronGuards.Add(Track(guard));
            }

            model.MarkAsNoLongerNeeded();
        }

        private void SpawnPlane()
        {
            var model = new Model("besra");
            if (!GameUtils.RequestModel(model)) return;

            _plane = Track(World.CreateVehicle(model, Ctx.Locations.Position("M14.PlaneSpawn"),
                Ctx.Locations.Heading("M14.PlaneSpawn")));
            model.MarkAsNoLongerNeeded();
            if (_plane == null || !_plane.Exists()) return;

            _plane.IsPersistent = true;

            var blip = Track(_plane.AddBlip());
            blip.Sprite = BlipSprite.Plane;
            blip.Color = BlipColor.Orange;
            blip.Name = "Jammer aircraft";
        }

        protected override void OnCleanup()
        {
            _apronGuards.Clear();
        }
    }
}
