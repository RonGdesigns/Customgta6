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
    /// Seen, not told: how Ice reaches the deck and how the other two know his route
    /// (Gohan on the rim with it, Ron in the boat under the span) before the first
    /// shot; the pickup established before it is the only answer; the encirclement
    /// closing and the way out shown, down to the water, before the jump is asked
    /// for; the jump itself the player's, talked down over the radio; Ice boarding.
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
        private Vector3 _deckApproach;
        private Vector3 _bridge;
        private Vector3 _riverbed;
        private Vector3 _rim;
        private bool _escapeShown, _talkedDown;

        public override string Id => "M25";
        public override string Title => "Bounty Hunters' Canyon";
        protected override MissionEndpoint Endpoint => MissionEndpoint.EscapeCheckpoint;

        public Vehicle Tanker => _tanker;
        public Vehicle Boat => _boat;
        public bool EscapeShown => _escapeShown;
        public bool TalkedDown => _talkedDown;

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _deckApproach = Ctx.Locations.Position("M25.DeckApproach");
            _bridge = Ctx.Locations.Position("M25.BridgeDeck");
            _riverbed = Ctx.Locations.Position("M25.Riverbed");
            _rim = Ctx.Locations.Position("M25.RimPost");

            // The whole crew is on this: Ice walks the deck, Ron waits in the boat under
            // the span, Gohan holds the rim with the route. Nobody is invented later.
            if (!Ctx.Crew.Deploy(CrewSlot.Ice, _deckApproach, Ctx.Locations.Heading("M25.DeckApproach")))
            {
                return false;
            }

            ApplyBibleSetting();

            var player = Game.Player.Character;
            player.Weapons.Give(WeaponHash.HeavySniper, 60, true, true);
            player.Weapons.Give(WeaponHash.Parachute, 1, false, true);

            SpawnTanker();
            SpawnBoat();
            if (!RequireAssets(_tanker, _boat)) return false;
            RequireAsset(_boat, "The extraction boat was lost. There is no way off the bridge.");
            Game.Player.Character.Weapons.Give(WeaponHash.RPG, 6, false, false);
            Ctx.Crew.CompanionsHoldPosition = true;
            Station(CrewSlot.Guess, _boat, VehicleSeat.Driver);
            Station(CrewSlot.Gohan, _rim);
            PlayApproach();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("High ground",
                    new ReachZoneObjective("Take the bridge deck.", () => _bridge, 8f))
                .PlayedBy(CrewSlot.Ice);

            yield return new MissionStage("Seal the pass",
                    new DestroyVehicleObjective("Detonate the fuel tanker across the southern pass.",
                        () => _tanker))
                .PlayedBy(CrewSlot.Ice)
                .OnEnter(context => GameUtils.Subtitle("~y~Use the RPG on the tanker from at least 30 meters away.", 4000))
                .WithCues("M25_S1_01_ICE");

            yield return new MissionStage("Hold the bridge",
                    new SurviveWavesObjective("Ice: defeat the three red-marked assault waves. Use cover and your rifle.",
                        SpawnHunterWave, 3, 7000))
                .PlayedBy(CrewSlot.Ice)
                .OnExit(context => PlayEscape());

            // The jump is the player's: the direction was shown, the boat is under the
            // span, Ron talks him down.
            yield return new MissionStage("Off the bridge",
                    new ReachZoneObjective("Ice: parachute down toward the marked extraction boat.",
                        () => _riverbed, 12f, flat: false),
                    new ReactionTrigger(() => _escapeShown && !_talkedDown && !Ctx.Cutscenes.IsActive, TalkDown))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(context =>
                    GameUtils.Subtitle("~y~A hundred and ten meters to the water. Jump.", 5000));

            yield return new MissionStage("River extraction",
                    new EnterVehicleObjective("Get in the boat.", () => _boat))
                .OwnedBy(CrewSlot.Ice)
                .OnExit(context =>
                {
                    /* Awarded once by CampaignState.MarkComplete after the mission passes. */
                    GameUtils.Subtitle("~g~In the boat. The sheriff's department doesn't chase anyone down this gorge; their spotters are over the Alamo.", 5000);
                })
                .AfterCues("M25_S1_03_ICE");
        }

        // ---------- beats ----------

        /// <summary>How Ice reaches the deck and who knows it: the route from the north end, Gohan on the rim with it, Ron in the boat under the span, the tanker on the south road.</summary>
        private void PlayApproach()
        {
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            var blocking = new SceneBlocking();
            if (ice != null && ice.Exists()) blocking.Then(new ShotStep(3000, ice, new Vector3(-6f, 2f, 1.8f), null, _bridge + new Vector3(0f, 0f, 1f), 0.8f));
            if (gohan != null && gohan.Exists()) blocking.Then(ShotStep.Watching(2600, gohan, gohan));
            if (_boat != null && _boat.Exists()) blocking.Then(new ShotStep(3000, _boat, new Vector3(-9f, 6f, 3f), _boat, new Vector3(0f, 0f, 0.6f), 0.9f));
            if (_tanker != null && _tanker.Exists()) blocking.Then(new ShotStep(2600, _tanker, new Vector3(-16f, 10f, 4f), _tanker, new Vector3(0f, 0f, 1.5f), 1.0f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "The span",
                Reason = "Ice's route: the deck from the north end, sent before he moves. Gohan on the rim with the route on his screen and the radio. Ron in the boat under the span with the engine warm: the way out, established before it is the only one. The tanker on the south road is the door to close.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M25 approach scene did not play; the deck stands on its own.");
        }

        /// <summary>The encirclement closing and the way out shown: down, to the boat under the span. Ron's line over it. Then the jump is asked for.</summary>
        private void PlayEscape()
        {
            _escapeShown = true;
            var blocking = new SceneBlocking();
            Ped closest = _hunters.Where(h => h != null && h.Exists() && !h.IsDead).OrderBy(h => h.Position.DistanceTo(_bridge)).FirstOrDefault();
            if (closest != null) blocking.Then(ShotStep.Watching(2400, closest, closest));
            else blocking.Then(ShotStep.Wide(2400, _bridge + new Vector3(0f, -40f, 1f), 20f, 8f, 8f));
            if (_boat != null && _boat.Exists())
                blocking.Then(new ShotStep(3400, null, _bridge + new Vector3(2f, 0f, 2f), _boat, new Vector3(0f, 0f, 0.5f), 0.4f))
                    .Then(new ShotStep(2600, _boat, new Vector3(-8f, 5f, 2.5f), _boat, new Vector3(0f, 0f, 0.6f), 0.7f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "escape", Title = "The way out",
                Reason = "The next wave is on the road and the deck has no other end. The way out is down: the boat under the span, engine running, Ron at the helm. Seen before the jump is asked for.",
                Blocking = blocking
            };
            var cue = Ctx.Data?.Cue("M25_S1_02_GUESS");
            if (!Ctx.Cutscenes.PlayStaged(spec, new[] { cue })) { Logger.Warn("M25 escape scene did not play; the line plays as dialogue."); blocking.Complete(); Say("M25_S1_02_GUESS"); }
        }

        /// <summary>Ron talks him down: the one radio line during the jump.</summary>
        private void TalkDown()
        {
            _talkedDown = true;
            Radio("GUESS", "Wind's from the west down the gorge. Pull late, steer at my wake, and I'll come to you. You don't have to land in the boat.", "M25_RADIO_01_GUESS");
        }

        /// <summary>The aftermath: the boat leaving the span behind, Ice aboard.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_boat == null || !_boat.Exists()) return null;
            return new SceneBlocking().Then(new ShotStep(4500, _boat, new Vector3(-10f, 6f, 3f), _boat, new Vector3(0f, 0f, 0.6f), 1.2f));
        }

        // ---------- world building ----------

        private IEnumerable<Ped> SpawnHunterWave(int wave)
        {
            var model = new Model("s_m_y_sheriff_01");
            if (!GameUtils.RequestModel(model)) return Enumerable.Empty<Ped>();

            var law = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            var spawned = new List<Ped>();

            // They come up the access road, which is the only reason a sniper on a
            // bridge is a fair fight rather than a firing range.
            var approach = _bridge + new Vector3(0f, -65f - wave * 10f, 0f);

            for (int i = 0; i < 3 + wave; i++)
            {
                var post = World.GetSafeCoordForPed(approach + new Vector3(-8f + i * 4f, 0f, 0f), false, 0);
                if (post == Vector3.Zero) continue;
                var hunter = World.CreatePed(model, post, 0f);
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

        /// <summary>Ron's boat under the span, engine warm: the pickup, there from the start.</summary>
        private void SpawnBoat()
        {
            var model = new Model("dinghy");
            if (!GameUtils.RequestModel(model)) return;

            _boat = Track(World.CreateVehicle(model, _riverbed, 0f));
            model.MarkAsNoLongerNeeded();
            if (_boat == null || !_boat.Exists()) return;

            _boat.IsPersistent = true;
            _boat.IsEngineRunning = true;

            var blip = Track(_boat.AddBlip());
            blip.Sprite = BlipSprite.Boat;
            blip.Color = BlipColor.Orange;
            blip.Name = "Extraction boat";
        }

        protected override void OnPassed()
        {
            if (_boat != null && _boat.Exists()) Release(_boat);
        }

        protected override void OnCleanup()
        {
            Ctx.Crew.CompanionsHoldPosition = false;
            _hunters.Clear();
        }
    }
}
