using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M13 — "Smuggler's Cut". La Puerta fuel basin, 02:00, drizzle.
    ///
    /// Ice kayaks in and puts limpet charges on three cartel fuel barges so the Port
    /// Heist has no pursuit craft. Gohan holds the harbour alarms; Guess idles on the
    /// slipway with the lights off.
    ///
    /// Three sites, one objective — the player picks the order, and the tension is
    /// that every second on a barge is a second in the open. The detonation is the
    /// reward, not the work.
    /// </summary>
    public sealed class M13SmugglersCut : ComposedMission
    {
        private readonly List<Vector3> _barges = new List<Vector3>();
        private readonly List<Ped> _watchmen = new List<Ped>();

        private Vehicle _kayak;
        private Vehicle _granger;
        private Vector3 _launch;
        private Vector3 _slipway;

        public override string Id => "M13";
        public override string Title => "Smuggler's Cut";

        protected override bool Setup()
        {
            _launch = Ctx.Locations.Position("M13.KayakLaunch");
            _slipway = Ctx.Locations.Position("M13.CanalSlipway");
            _barges.Add(Ctx.Locations.Position("M13.BargeOne"));
            _barges.Add(Ctx.Locations.Position("M13.BargeTwo"));
            _barges.Add(Ctx.Locations.Position("M13.BargeThree"));

            if (!Ctx.Crew.Deploy(CrewSlot.Ice, _launch, Ctx.Locations.Heading("M13.KayakLaunch")))
            {
                return false;
            }

            ApplyBibleSetting();
            Game.Player.Character.Weapons.Give(WeaponHash.StickyBomb, 6, false, true);

            SpawnKayak();
            SpawnGranger();
            SpawnWatchmen();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Into the basin",
                    new EnterVehicleObjective("Ice — take the kayak into the basin.", () => _kayak))
                .OwnedBy(CrewSlot.Ice)
                .WithDialogue(1);

            yield return new MissionStage("Limpets",
                    new MultiHoldObjective("Plant limpet charges on all three barges.",
                        _barges, 6, 4f, "Arming the charge"),
                    new AvoidDetectionObjective(() => _watchmen,
                        "A dock watchman called it in before the charges were set.", 40f, 4))
                .OwnedBy(CrewSlot.Ice);

            yield return new MissionStage("Clear the water",
                    new ReachZoneObjective("Get to the western slipway.", () => _slipway, 10f))
                .WithDialogue(1)
                .OnEnter(context => GameUtils.Subtitle("~y~Guess is idling on the slipway. Move.", 4000));

            yield return new MissionStage("Blow the basin",
                    new EnterVehicleObjective("Get in the Granger and hit the clacker.",
                        () => _granger))
                .OnExit(context =>
                {
                    Detonate();
                    GameUtils.Subtitle("~g~Basin's an inferno. Nothing in the water follows us now.", 6000);
                });
        }

        /// <summary>
        /// The payoff. Explosions at each barge in sequence rather than at once —
        /// simultaneous blasts read as one bug rather than three charges.
        /// </summary>
        private void Detonate()
        {
            for (int i = 0; i < _barges.Count; i++)
            {
                World.AddExplosion(_barges[i], ExplosionType.Tanker, 12f, 1.6f,
                    Game.Player.Character, true, false);
                Script.Wait(450);
            }
        }

        private void SpawnKayak()
        {
            var model = new Model("seashark");
            if (!GameUtils.RequestModel(model)) return;

            _kayak = Track(World.CreateVehicle(model, _launch, Ctx.Locations.Heading("M13.KayakLaunch")));
            model.MarkAsNoLongerNeeded();
            if (_kayak == null || !_kayak.Exists()) return;

            _kayak.IsPersistent = true;

            var blip = Track(_kayak.AddBlip());
            blip.Sprite = BlipSprite.Boat;
            blip.Color = BlipColor.Green;
            blip.Name = "Kayak";
        }

        private void SpawnGranger()
        {
            var model = new Model("granger");
            if (!GameUtils.RequestModel(model)) return;

            _granger = Track(World.CreateVehicle(model, _slipway, Ctx.Locations.Heading("M13.CanalSlipway")));
            model.MarkAsNoLongerNeeded();
            if (_granger == null || !_granger.Exists()) return;

            _granger.IsPersistent = true;
            _granger.AreLightsOn = false;

            var blip = Track(_granger.AddBlip());
            blip.Sprite = BlipSprite.PersonalVehicleCar;
            blip.Color = BlipColor.Orange;
            blip.Name = "Guess";
        }

        private void SpawnWatchmen()
        {
            var model = new Model("s_m_m_dockwork_01");
            if (!GameUtils.RequestModel(model)) return;

            var cartel = World.AddRelationshipGroup("BLOODLINES_CARTEL");

            for (int i = 0; i < _barges.Count; i++)
            {
                var watchman = World.CreatePed(model, _barges[i] + new Vector3(6f, 4f, 1f), 180f);
                if (watchman == null || !watchman.Exists()) continue;

                watchman.RelationshipGroup = cartel;
                watchman.IsPersistent = true;
                watchman.BlockPermanentEvents = true;
                watchman.Accuracy = 20;
                watchman.Weapons.Give(WeaponHash.Pistol, 40, true, true);
                watchman.Task.StartScenario("WORLD_HUMAN_GUARD_STAND", watchman.Position, 180f);

                _watchmen.Add(Track(watchman));
            }

            model.MarkAsNoLongerNeeded();
        }

        protected override void OnCleanup()
        {
            _watchmen.Clear();
        }
    }
}
