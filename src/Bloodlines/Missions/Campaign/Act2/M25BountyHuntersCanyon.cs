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
    /// M25 — "Bounty Hunters' Canyon". Raton Canyon railway bridge, 17:30.
    ///
    /// Aegis puts five million on the crew and every corrupt badge in Blaine County
    /// comes looking. Ice takes the high ground on the suspension bridge with a
    /// thermal rifle, drops a fuel tanker across the access road, and goes off the
    /// bridge into the river when they get close enough to matter.
    ///
    /// One of the two missions that use the bible's own surveyed coordinates — the
    /// Raton bridge is in the Track 2 index — so this is the closest thing in Act II
    /// to a position we can trust before the survey pass.
    /// </summary>
    public sealed class M25BountyHuntersCanyon : ComposedMission
    {
        private readonly List<Ped> _hunters = new List<Ped>();

        private Vehicle _tanker;
        private Vehicle _boat;
        private Vector3 _bridge;
        private Vector3 _riverbed;

        public override string Id => "M25";
        public override string Title => "Bounty Hunters' Canyon";

        protected override bool Setup()
        {
            _bridge = Ctx.Locations.Position("M25.BridgeDeck");
            _riverbed = Ctx.Locations.Position("M25.Riverbed");

            if (!Ctx.Crew.DeploySolo(CrewSlot.Ice, _bridge, Ctx.Locations.Heading("M25.BridgeDeck")))
            {
                return false;
            }

            ApplyBibleSetting();

            var player = Game.Player.Character;
            player.Weapons.Give(WeaponHash.HeavySniper, 60, true, true);
            player.Weapons.Give(WeaponHash.Parachute, 1, false, true);

            SpawnTanker();
            SpawnBoat();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("High ground",
                    new ReachZoneObjective("Take the bridge deck.", () => _bridge, 8f))
                .PlayedBy(CrewSlot.Ice)
                .WithDialogue(1);

            yield return new MissionStage("Seal the pass",
                    new DestroyVehicleObjective("Detonate the fuel tanker across the southern pass.",
                        () => _tanker))
                .PlayedBy(CrewSlot.Ice)
                .OnEnter(context => GameUtils.Subtitle("~y~One round into the tank seals the road.", 4000));

            yield return new MissionStage("Hold the bridge",
                    new SurviveWavesObjective("Hold the bridge — thermal scope, they have no cover.",
                        SpawnHunterWave, 3, 7000))
                .PlayedBy(CrewSlot.Ice);

            yield return new MissionStage("Off the bridge",
                    new ReachZoneObjective("Go over the rail — Guess is on the sandbar.",
                        () => _riverbed, 25f, flat: true))
                .WithDialogue(1)
                .OnEnter(context =>
                    GameUtils.Subtitle("~y~A hundred and ten metres to the water. Jump.", 5000));

            yield return new MissionStage("River extraction",
                    new EnterVehicleObjective("Get in the boat.", () => _boat))
                .OnExit(context =>
                {
                    context.State.CashOnHand += 40000;
                    GameUtils.Subtitle("~g~The sheriff's department doesn't chase anyone down this gorge.", 5000);
                });
        }

        private IEnumerable<Ped> SpawnHunterWave(int wave)
        {
            var model = new Model("s_m_y_sheriff_01");
            if (!GameUtils.RequestModel(model)) return Enumerable.Empty<Ped>();

            var law = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            var spawned = new List<Ped>();

            // They come up the access road, which is the only reason a sniper on a
            // bridge is a fair fight rather than a firing range.
            var approach = _bridge + new Vector3(0f, -120f - wave * 30f, -20f);

            for (int i = 0; i < 3 + wave; i++)
            {
                var hunter = World.CreatePed(model, approach + new Vector3(-8f + i * 4f, 0f, 0f), 0f);
                if (hunter == null || !hunter.Exists()) continue;

                hunter.RelationshipGroup = law;
                hunter.IsPersistent = true;
                hunter.BlockPermanentEvents = true;
                hunter.Accuracy = 25 + wave * 5;
                hunter.Weapons.Give(WeaponHash.CarbineRifle, 200, true, true);
                hunter.Task.RunTo(_bridge, true, -1);

                spawned.Add(Track(hunter));
                _hunters.Add(hunter);
            }

            model.MarkAsNoLongerNeeded();
            return spawned;
        }

        private void SpawnTanker()
        {
            var model = new Model("tanker");
            if (!GameUtils.RequestModel(model)) return;

            _tanker = Track(World.CreateVehicle(model, Ctx.Locations.Position("M25.TankerSpot"),
                Ctx.Locations.Heading("M25.TankerSpot")));
            model.MarkAsNoLongerNeeded();
            if (_tanker == null || !_tanker.Exists()) return;

            _tanker.IsPersistent = true;

            var blip = Track(_tanker.AddBlip());
            blip.Sprite = BlipSprite.Standard;
            blip.Color = BlipColor.Red;
            blip.Name = "Fuel tanker";
        }

        private void SpawnBoat()
        {
            var model = new Model("dinghy");
            if (!GameUtils.RequestModel(model)) return;

            _boat = Track(World.CreateVehicle(model, _riverbed, 0f));
            model.MarkAsNoLongerNeeded();
            if (_boat == null || !_boat.Exists()) return;

            _boat.IsPersistent = true;

            var blip = Track(_boat.AddBlip());
            blip.Sprite = BlipSprite.Boat;
            blip.Color = BlipColor.Orange;
            blip.Name = "Guess";
        }

        protected override void OnCleanup()
        {
            _hunters.Clear();
        }
    }
}
