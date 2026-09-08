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
    /// M06 — "Clean Sweep". Vespucci canals LSPD evidence depot, 01:30, fog and rain.
    ///
    /// The biometric backups have to burn. Gohan comes up through the drainage
    /// culverts and cuts the 480-volt feeder, Ice holds the alley against SWAT while
    /// the racks melt, Guess reverses the Granger through the cruisers to get everyone
    /// out.
    ///
    /// This is the mission the wave objective was written for: Ice's job is not to
    /// clear the alley, it is to hold it for as long as the thermite takes. Two
    /// objectives running at once in the same stage — one a burn timer, one a siege.
    /// </summary>
    public sealed class M06CleanSweep : ComposedMission
    {
        private readonly List<Ped> _swat = new List<Ped>();

        private Vehicle _granger;
        private Vector3 _feeder;
        private Vector3 _sallyPort;
        private Vector3 _racks;
        private Vector3 _alley;

        public override string Id => "M06";
        public override string Title => "Clean Sweep";

        protected override bool Setup()
        {
            _feeder = Ctx.Locations.Position("M06.Feeder");
            _sallyPort = Ctx.Locations.Position("M06.SallyPort");
            _racks = Ctx.Locations.Position("M06.ServerRacks");
            _alley = Ctx.Locations.Position("M06.AlleyHold");

            if (!Ctx.Crew.Deploy(CrewSlot.Gohan, Ctx.Locations.Position("M06.Culvert"), 0f)) return false;

            ApplyBibleSetting();
            SpawnGranger();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Cut the power",
                    new HoldZoneObjective("Gohan — sever the 480-volt feeder.", () => _feeder, 6, 3f,
                        "Cutting the feeder"))
                .OwnedBy(CrewSlot.Gohan)
                .WithDialogue(1);

            yield return new MissionStage("Sally port",
                    new ReachZoneObjective("Ice — breach the sally port.", () => _sallyPort, 5f))
                .OwnedBy(CrewSlot.Ice);

            // The burn and the siege run together: the thermite does not care how the
            // alley is going, and the alley does not stop when the racks are slag.
            yield return new MissionStage("Burn the racks",
                    new HoldZoneObjective("Gohan — thermite the server racks.", () => _racks, 20, 3f,
                        "Thermite burning"),
                    new SurviveWavesObjective("Ice — hold the alley.", SpawnSwatWave, 3, 6000))
                .WithDialogue(2)
                .OnEnter(context => Game.Player.WantedLevel = 3)
                .OnExit(context => GameUtils.Subtitle("~g~Core is slag. The biometrics are gone.", 4000));

            yield return new MissionStage("Reverse extraction",
                    new EnterVehicleObjective("Get in the Granger.", () => _granger))
                .OnEnter(context =>
                {
                    if (_granger != null && _granger.Exists()) _granger.IsEngineRunning = true;
                });

            yield return new MissionStage("Out of Vespucci",
                    new LoseWantedObjective("Lose the police."),
                    new ProtectObjective("", () => _granger, "The Granger was destroyed."))
                .OnExit(context => GameUtils.Subtitle("~g~Depot clean. Nothing left to match a face to.", 5000));
        }

        private IEnumerable<Ped> SpawnSwatWave(int wave)
        {
            var model = new Model("s_m_y_swat_01");
            if (!GameUtils.RequestModel(model)) return Enumerable.Empty<Ped>();

            var police = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            var spawned = new List<Ped>();
            int count = 3 + wave;

            for (int i = 0; i < count; i++)
            {
                var offset = new Vector3(-8f + i * 3f, 22f + (i % 2) * 5f, 0f);
                var trooper = World.CreatePed(model, _alley + offset, 180f);
                if (trooper == null || !trooper.Exists()) continue;

                trooper.RelationshipGroup = police;
                trooper.IsPersistent = true;
                trooper.BlockPermanentEvents = true;
                trooper.Accuracy = 30 + wave * 5;
                trooper.Armor = 50;
                trooper.Weapons.Give(wave >= 2 ? WeaponHash.CarbineRifle : WeaponHash.SMG, 200, true, true);
                trooper.Task.FightAgainstHatedTargets(90f);

                spawned.Add(Track(trooper));
                _swat.Add(trooper);
            }

            model.MarkAsNoLongerNeeded();
            return spawned;
        }

        private void SpawnGranger()
        {
            var model = new Model("granger");
            if (!GameUtils.RequestModel(model)) return;

            _granger = Track(World.CreateVehicle(model, Ctx.Locations.Position("M06.GrangerSpawn"),
                Ctx.Locations.Heading("M06.GrangerSpawn")));
            model.MarkAsNoLongerNeeded();
            if (_granger == null || !_granger.Exists()) return;

            _granger.IsPersistent = true;
            _granger.IsEngineRunning = false;

            var blip = Track(_granger.AddBlip());
            blip.Sprite = BlipSprite.PersonalVehicleCar;
            blip.Color = BlipColor.Orange;
            blip.Name = "Armored Granger";
        }

        protected override void OnCleanup()
        {
            _swat.Clear();
        }
    }
}
