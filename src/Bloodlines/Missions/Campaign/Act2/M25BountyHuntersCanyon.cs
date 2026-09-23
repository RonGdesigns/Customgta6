using System;
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
    /// (Gohan on the rim with it, Ron in the boat east of the span at the river mouth) before the first
    /// shot; the pickup established before it is the only answer; the encirclement
    /// closing and the way out shown, down to the water, before the jump is asked
    /// for; the jump itself the player's, talked down over the radio; Ice boarding.
    ///
    /// The old bible coordinate missed the span. The railway deck and downstream
    /// extraction now use collision-checked defaults; saved survey keys still win.
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
        private bool _escapeShown, _talkedDown, _departing;
        private Vector3 _departureOrigin, _boatEscape;
        private int _departureStarted, _nextBoatOrder;
        /// <summary>Whether Guess currently holds the run-out order; the order is given on a change, not on a clock.</summary>
        private bool _boatOrdered;
        private int _boatOrderedAt;
        /// <summary>How long the boat may sit still under a standing order before it is treated as lost.</summary>
        public const int BoatStallMs = 5000;
        public const float DepartureDistance = 100f;
        public bool Departing => _departing;
        public Vector3 DepartureOrigin => _departureOrigin;

        public override string Id => "M25";
        public override string Title => "Bounty Hunters' Canyon";
        protected override MissionEndpoint Endpoint => MissionEndpoint.EscapeCheckpoint;

        public Vehicle Tanker => _tanker;
        public Vehicle Boat => _boat;
        public bool EscapeShown => _escapeShown;
        public bool TalkedDown => _talkedDown;

        protected override bool Setup()
        {
            // These keys belong to the rail/tunnel deck, not the gorge floor.
            if (!MissionSites.Prepare(Ctx.Locations, Id, "M25.DeckApproach", "M25.BridgeDeck", "M25.TankerSpot",
                    "M25.HunterApproach", "M25.NorthTunnel", "M25.SouthTunnel")) return false;
            _deckApproach = BoundedPlacement.Ped(Ctx.Locations, "M25.DeckApproach");
            _bridge = BoundedPlacement.Ped(Ctx.Locations, "M25.BridgeDeck");
            _boatEscape = Ctx.Locations.Position("M25.BoatEscape");
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
            foreach (var hero in Protagonist.All) RequireSurvivor(Ctx.Crew.PedFor(hero.Slot), "A brother was lost before the canyon extraction finished.");
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
                    GameUtils.Subtitle("~y~Deploy the parachute immediately. Steer east toward the marked boat at the river mouth.", 5000));

            yield return new MissionStage("River extraction",
                    new EnterVehicleObjective("Ice: board the extraction boat as a passenger.", () => _boat, VehicleSeat.Passenger))
                .OwnedBy(CrewSlot.Ice)
                .OnExit(context =>
                {
                    _departing = true;
                    _departureOrigin = _boat.Position;
                    _departureStarted = Game.GameTime;
                    _nextBoatOrder = 0;
                    Radio("GUESS", "You're aboard. Hold on while I put some water between us and that bridge.", "M25_RADIO_02_GUESS");
                });

            yield return new MissionStage("Clear the pickup",
                    new ConditionObjective("Stay in the boat while Guess takes you at least 100 meters clear of the pickup. You can switch to Guess and drive.", DepartureComplete))
                .AnyBrother()
                .AfterCues("M25_S1_03_ICE");
        }

        private bool DepartureComplete() => _departing && _boat != null && _boat.Exists() && _boat.IsDriveable &&
            Ctx.Crew.PedFor(CrewSlot.Ice).IsInVehicle(_boat) &&
            _boat.GetPedOnSeat(VehicleSeat.Driver) == Ctx.Crew.PedFor(CrewSlot.Guess) &&
            new Vector3(_boat.Position.X, _boat.Position.Y, 0f).DistanceTo(new Vector3(_departureOrigin.X, _departureOrigin.Y, 0f)) >= DepartureDistance;

        protected override void OnUpdate()
        {
            if (_departing && !Ctx.Cutscenes.IsActive)
            {
                if (Game.GameTime - _departureStarted > 120000)
                { Fail("The boat did not clear the pickup. Retry the canyon extraction."); return; }
                var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
                var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
                // Hold for a passenger who falls out; do not drive off and then
                // satisfy distance using an empty boat. Never task the active hero.
                //
                // The run-out used to be handed over again every three seconds whatever the
                // boat was doing, which restarts the boat mission each time. It is given once
                // when Ice is aboard, taken back once when he is not, and only repeated if
                // the boat has plainly stopped under it (the September 22 audit).
                if (Ctx.Crew.ActiveSlot == CrewSlot.Guess) _boatOrdered = false;
                else if (guess != null && guess.Exists())
                {
                    bool ready = ice != null && ice.IsInVehicle(_boat) && _boat.GetPedOnSeat(VehicleSeat.Driver) == guess;
                    bool stalled = _boatOrdered && ready && _boat.Speed < 1f && Game.GameTime - _boatOrderedAt > BoatStallMs;
                    if (ready && (!_boatOrdered || stalled))
                    {
                        Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Guess);
                        guess.Task.StartBoatMission(_boat, _boatEscape, VehicleMissionType.GoTo, 12f, VehicleDrivingFlags.None, 8f, (BoatMissionFlags)0);
                        _boatOrdered = true; _boatOrderedAt = Game.GameTime;
                        if (stalled) Logger.Info(Id + ": the boat had stopped under its run-out order; reissued it.");
                    }
                    else if (!ready && (_boatOrdered || Game.GameTime >= _nextBoatOrder))
                    {
                        // Hold: one clear on the change, and the control reclaimed on a slow
                        // cadence so the companion AI does not drive off with the boat.
                        Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Guess);
                        if (_boatOrdered) guess.Task.ClearAll();
                        _boatOrdered = false; _nextBoatOrder = Game.GameTime + 3000;
                    }
                }
            }
            base.OnUpdate();
        }

        // ---------- beats ----------

        /// <summary>How Ice reaches the deck and who knows it: the route from the north end, Gohan on the rim with it, Ron in the boat east of the span at the river mouth, the tanker on the south road.</summary>
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
                Reason = "Ice's route: the deck from the north end, sent before he moves. Gohan on the rim with the route on his screen and the radio. Ron in the boat east of the span at the river mouth with the engine warm: the way out, established before it is the only one. The tanker on the south road is the door to close.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M25 approach scene did not play; the deck stands on its own.");
        }

        /// <summary>The encirclement closing and the way out shown: down, to the boat east of the span at the river mouth. Ron's line over it. Then the jump is asked for.</summary>
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
                Reason = "The next wave is on the road and the deck has no other end. The way out is down: the boat east of the span at the river mouth, engine running, Ron at the helm. Seen before the jump is asked for.",
                Blocking = blocking
            };
            var cue = Ctx.Data?.Cue("M25_S1_02_GUESS");
            if (!Ctx.Cutscenes.PlayStaged(spec, new[] { cue })) { Logger.Warn("M25 escape scene did not play; the line plays as dialogue."); blocking.Complete(); Say("M25_S1_02_GUESS"); }
        }

        /// <summary>Ron talks him down: the one radio line during the jump.</summary>
        private void TalkDown()
        {
            _talkedDown = true;
            Radio("GUESS", "I'm east of the span at the river mouth. Open the chute early and steer toward my marker. Land beside the boat, then climb aboard.", "M25_RADIO_01_GUESS");
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
            var north = Ctx.Locations.Get("M25.NorthTunnel");
            var south = Ctx.Locations.Get("M25.SouthTunnel");

            for (int i = 0; i < 3 + wave; i++)
            {
                // Keep the formation on the narrow rail deck instead of spreading
                // it across the gorge or accepting a nav point on the ground below.
                var entry = i % 2 == 0 ? north : south;
                // Alternate the two tunnel mouths. Stagger along the rail axis,
                // not outward into the gorge; no lower-level navmesh is accepted.
                var requested = BoundedPlacement.Offset(entry.Position, entry.Heading, (i % 3 - 1) * .6f, -(i / 2) * 3f);
                var post = BoundedPlacement.PedAt(requested, entry.Key + " wave " + wave);
                var hunter = World.CreatePed(model, post, entry.Heading);
                if (hunter == null || !hunter.Exists()) continue;

                hunter.RelationshipGroup = law;
                hunter.IsPersistent = true;
                hunter.BlockPermanentEvents = true;
                hunter.Accuracy = 25 + wave * 5;
                hunter.Weapons.Give(WeaponHash.CarbineRifle, 200, true, true);
                GTA.Native.Function.Call(GTA.Native.Hash.SET_PED_COMBAT_MOVEMENT, hunter, 1);
                hunter.Task.FightAgainst(Ctx.Crew.PedFor(CrewSlot.Ice));

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

        /// <summary>Ron's boat east of the span at the river mouth, engine warm: the pickup, there from the start.</summary>
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
            Ctx.Crew.CompanionAI.ReleaseControl(CrewSlot.Guess);
            _hunters.Clear();
        }
    }
}
