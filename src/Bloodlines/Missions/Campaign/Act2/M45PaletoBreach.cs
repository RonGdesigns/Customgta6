using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// Chapter two. Guess flies Ice over the rail now that the defenses are down,
    /// Ice takes the upper deck, and Gohan brings the Kraken to the stern platform
    /// and climbs aboard. The chapter is finished when both brothers are physically
    /// on the structure — not near it, not still in a seat.
    ///
    /// The authored beat has Ice breaching an elevator shaft and Gohan surfacing in
    /// a moonpool. Neither exists on a vessel: the deck levels are connected by the
    /// interior's own stairs, and Gohan comes up the stern platform at the
    /// waterline. Both heights are read out of the archives; see PaletoSite.
    /// </summary>
    public sealed class M45PaletoBreach : ComposedMission
    {
        public const string HelicopterModel = "annihilator";
        public const string GuardModel = "s_m_y_blackops_01";
        /// <summary>
        /// How many hold the upper deck. Four were spawned and Ron found one: they were
        /// tasked to fight the moment they were created, and a man told to close on a
        /// target he cannot reach walks off a deck fifteen meters above the sea. This is a
        /// heist, so it is ten, and none of them go over the side.
        /// </summary>
        public const int DeckGuards = 10;
        /// <summary>
        /// How far apart the ranks of the deck detail stand, along the vessel.
        ///
        /// The detail is laid out around its own key now, not measured off the landing
        /// zone. Ron asked for the landing zone to move to the far end of the ship - ten
        /// men standing in the open watching a helicopter arrive and not firing on it
        /// reads wrong however far away they are - and a detail pinned to the pad would
        /// have walked to the bow with it and off the front of the boat.
        /// </summary>
        public const float DeckRankSpacing = 7f;
        /// <summary>How far off the centerline each man of a rank stands.</summary>
        public const float DeckBeam = 5f;
        /// <summary>How many times a roofed or unsupported post steps outboard looking for open deck.</summary>
        public const int PostRetries = 4;
        /// <summary>How far outboard each of those steps moves it, toward the rail.</summary>
        public const float PostStepOut = 1.5f;
        /// <summary>
        /// How much open air a post needs over it before a man is put there.
        ///
        /// This is the check the first repair was missing. A downward probe answers
        /// "something solid is under this point", which inside a hull is just as true as
        /// out on the deck: an interior floor is a surface. Two of the ten were still
        /// standing inside the vessel where Ron could not shoot them. A man is about 1.9
        /// meters; anything closer than this overhead is a deckhead, not the sky.
        /// </summary>
        public const float DeckClearance = 2.2f;
        /// <summary>
        /// How far the detail has to be from where Ice steps off before it is worth saying
        /// so. Both ends are surveyable keys, and a survey that puts them together brings
        /// back the landing zone Ron asked to get away from.
        /// </summary>
        public const float DetailStandoff = 25f;
        /// <summary>How often a deck guard's combat order is refreshed once the fight is on.</summary>
        public const int DeckOrderMs = 5000;
        /// <summary>How far a deck guard will engage. The deck is about this long.</summary>
        public const float DeckEngageRange = 70f;
        /// <summary>
        /// How high above the deck Ice steps off. The hold marker is 40 meters up and
        /// the deck is at 15.5: stepping off up there is a 24-meter fall onto steel,
        /// which is how this chapter killed Ice every time it was opened. Five meters
        /// is a drop a man walks away from, and it is low enough that he lands on the
        /// deck rather than in the sea beside it.
        /// </summary>
        public const float InsertionHeight = 5f;

        private readonly List<Ped> _guards = new List<Ped>();
        private readonly AircraftHold _hold = new AircraftHold();
        /// <summary>
        /// The upper deck where it really is. Probed once at setup, because a raycast is
        /// not something to do every frame, and because everything on this chapter hangs
        /// off it: the marker, the zone, the hover above it and the guards standing on it.
        /// </summary>
        private Vector3 _deck;
        private Vehicle _chopper;
        private Vehicle _kraken;
        private bool _landed;
        private bool _aboard;
        private int _deckOrderAt;

        public override string Id => "M45";
        public override string Title => "Paleto Deep-Sea: Breach";
        protected override MissionEndpoint Endpoint => MissionEndpoint.ContinuousNext;

        public Vehicle Chopper => _chopper;
        public Vehicle Kraken => _kraken;
        /// <summary>Ice is on the upper deck with the guards down.</summary>
        public bool Landed => _landed;
        /// <summary>Gohan is out of the water and on the structure.</summary>
        public bool Aboard => _aboard;
        public IReadOnlyList<Ped> Guards => _guards;

        private Vector3 At(string key) => Ctx.Locations.Position(key);
        /// <summary>
        /// Where the helicopter holds for the step-off: directly over the deck point,
        /// not over the circuit marker 50 meters away from it. Derived rather than
        /// keyed, so surveying M45.Helipad moves the hover with it.
        /// </summary>
        private Vector3 Insertion => new Vector3(_deck.X, _deck.Y, _deck.Z + InsertionHeight);
        /// <summary>Ice is on the structure on his own feet rather than in a seat.</summary>
        private bool IceOnDeck
        {
            get
            {
                var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
                // Deliberately a flat distance plus a floor, not a sphere. A sphere on an
                // authored height is exactly what failed: he was on the ship and the check
                // was measuring to a point above his head.
                return ice != null && ice.Exists() && !ice.IsDead && !ice.IsInVehicle() &&
                    ice.Position.Z > PaletoSite.WaterlineDeck &&
                    ice.Position.DistanceTo2D(_deck) < 25f;
            }
        }

        protected override bool Setup()
        {
            var world = Paleto.Of(Ctx);
            if (world != null && !world.DefensesDown)
                throw new InvalidOperationException("M45 opened before the sea defenses were down. The operation must run from M44.");

            // Continuing: the sub and the men are where the dive left them.
            if (!Paleto.IsContinuing(Ctx) && !Ctx.Crew.Deploy(CrewSlot.Ice, At("M45.Board"), Ctx.Locations.Heading("M45.Board"))) return false;
            ApplyBibleSetting();

            // Before anything is put on the deck, find out where the deck is. The
            // authored height was above the real surface, which floated the marker and
            // meant the zone under it never registered however Ron stood on the ship.
            _deck = PaletoSite.OnDeck(At("M45.Helipad"), Id + " upper deck");

            _kraken = world?.Get<Vehicle>("kraken");
            if (Paleto.IsContinuing(Ctx)) _kraken = Track(world.Require<Vehicle>("kraken"));
            // Opened alone in QA there is no dive to inherit, so the sub is staged
            // where M44 would have surfaced it rather than left missing.
            else if (_kraken == null || !_kraken.Exists()) _kraken = StageSub();
            // The guards first, deliberately. Everything slow in this Setup happens before
            // the aircraft exists, so the gap between creating a helicopter at sixty meters
            // and the first frame that flies it is as short as it can be.
            SpawnDeckGuards();

            if (!SpawnHelicopter()) return false;
            world?.Bind("chopper", _chopper);
            RequireAsset(_chopper, "The extraction helicopter was lost before the boarding.");

            // The interior is asked for here rather than in M46, because it needs to be in
            // by the time Gohan reaches a door and M45 is several minutes of deck fight
            // ahead of that. Ensure is called again every frame until it answers.
            Paleto.EnsureInside(Ctx);
            // The deck and platform points are the least proven in the operation.
            Paleto.Review(Ctx, PlacementContract.Ped("M45.Helipad"), PlacementContract.Ped("M45.Board"));
            Station(CrewSlot.Guess, _chopper, VehicleSeat.Driver);
            Station(CrewSlot.Ice, _chopper, VehicleSeat.Passenger);
            if (_kraken != null && _kraken.Exists()) Station(CrewSlot.Gohan, _kraken, VehicleSeat.Driver);
            return true;
        }

        private bool SpawnHelicopter()
        {
            if (Paleto.IsContinuing(Ctx))
            {
                var carried = Paleto.Of(Ctx)?.Get<Vehicle>("chopper");
                if (carried != null && carried.Exists()) { _chopper = Track(carried); return true; }
            }
            var model = new Model(HelicopterModel);
            if (!GameUtils.RequestModel(model)) return false;
            // M43 landed it at the Paleto strip; it starts this chapter in the air on
            // the approach, which is where the authored flight begins.
            _chopper = Track(World.CreateVehicle(model, At("M45.Approach"), Ctx.Locations.Heading("M45.Approach")));
            model.MarkAsNoLongerNeeded();
            if (_chopper == null || !_chopper.Exists()) return false;
            _chopper.IsPersistent = true;
            // It is created 60 meters over open water. Rotors at speed and a little
            // airspeed, or it is in the sea before the blades spin up.
            AircraftHold.LaunchAirborne(_chopper);
            return true;
        }

        private Vehicle StageSub()
        {
            var model = new Model(M44PaletoSubSurface.SubModel);
            if (!GameUtils.RequestModel(model)) return null;
            var sub = Track(World.CreateVehicle(model, MarineSites.ResolveOrThrow(Ctx.Locations, "M44.Surface", 4f), 0f));
            model.MarkAsNoLongerNeeded();
            if (sub != null && sub.Exists()) sub.IsPersistent = true;
            return sub;
        }

        /// <summary>
        /// The deck detail, on the part of the vessel it defends rather than around the
        /// spot Ice arrives at.
        ///
        /// Ron played the repaired version and reported two things: two men were still
        /// inside the hull and could not be shot, and the arrival looked wrong - a detail
        /// standing in the open, not firing, while a helicopter puts someone on their
        /// deck. Both are answered here. Every post has to be open to the sky before
        /// anybody stands on it, and the detail is laid out around its own key amidships
        /// while the landing zone moved to the bow.
        /// </summary>
        private void SpawnDeckGuards()
        {
            var model = new Model(GuardModel);
            if (!GameUtils.RequestModel(model)) return;
            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            // One attempt: the landing-zone probe in Setup already paid for this collision,
            // and ten waiting probes here stalled Setup long enough for the helicopter
            // created just before them to fly itself into the sea.
            var hub = PaletoSite.OnDeck(At("M45.Deck"), Id + " deck detail", 1);
            if (hub.DistanceTo2D(_deck) < DetailStandoff)
                Logger.Warn(Id + ": the deck detail stands " + hub.DistanceTo2D(_deck).ToString("0.0") +
                    " m from where Ice steps off. Survey M45.Deck and M45.Helipad apart, or the detail is standing in the landing zone again.");
            // The count comes off M45.Deck when he has set one. The spread does not: these
            // men stand in ranks across the beam and every post is probed for a deckhead
            // over it, so a circle laid out by the editor would undo the fix that stopped
            // two of them ending up inside the hull.
            int guards = MissionPlacement.Count(Ctx.Locations, "M45.Deck", DeckGuards);
            int ranks = Math.Max(1, guards / 2);
            int placed = 0, empty = 0, unverified = 0;
            for (int i = 0; i < guards; i++)
            {
                // Ranks along the vessel either side of the detail's own key, two men to a
                // rank off each beam, so ten men are a detail rather than a pile.
                float along = ((i / 2) - (ranks - 1) / 2f) * DeckRankSpacing;
                float beam = i % 2 == 0 ? DeckBeam : -DeckBeam;
                var post = hub + new Vector3(along, beam, 0f);
                // A post has to be standing on something AND have sky over it. The first
                // condition on its own is what put two men inside the hull: a deck is a
                // surface and so is the floor of the room under it, and a probe looking
                // down cannot tell them apart. A refused post steps outboard toward the
                // rail, because on a vessel the enclosed space is down the middle and the
                // open deck is along the sides.
                Vector3? stand = null;
                bool answered = false;
                for (int step = 0; step <= PostRetries && stand == null; step++)
                {
                    var tried = post + new Vector3(0f, Math.Sign(beam) * step * PostStepOut, 0f);
                    float? deck = MissionSites.SurfaceHeight(tried, tried.Z + PaletoSite.DeckHeadroom, PaletoSite.WaterlineDeck - 1f);
                    if (!deck.HasValue) continue;
                    answered = true;
                    var candidate = new Vector3(tried.X, tried.Y, deck.Value);
                    if (!MissionSites.OpenAbove(candidate, DeckClearance)) continue;
                    stand = candidate;
                }
                if (stand == null && answered)
                {
                    // Every point on that line is under cover. He is left out rather than put
                    // there: eight men Ron can fight is a thinner detail, two he cannot reach
                    // is a mission he cannot finish. This is the case that was wrong.
                    empty++;
                    Logger.Warn(Id + ": post " + (i + 1) + " at " + post + " has a deckhead over every point on its line and is left empty.");
                    continue;
                }
                if (stand == null)
                {
                    // Nothing answered at all, which is not the same thing. That is a vessel
                    // whose collision has not streamed, or a chapter opened alone in QA with
                    // no structure under it — and leaving the deck empty there is a mission
                    // that cannot be completed. He stands at the height the detail's own probe
                    // measured, and the log says the post is unverified.
                    stand = new Vector3(post.X, post.Y, hub.Z);
                    unverified++;
                    Logger.Warn(Id + ": nothing solid answered under post " + (i + 1) + " at " + post +
                        "; standing him at the detail's measured height " + hub.Z.ToString("0.00") + ".");
                }
                var guard = World.CreatePed(model, stand.Value, 180f);
                if (guard == null || !guard.Exists())
                { Logger.Warn(Id + ": deck guard " + (i + 1) + " could not be created at " + stand.Value + "."); continue; }
                placed++;
                guard.RelationshipGroup = aegis;
                guard.IsPersistent = true;
                guard.BlockPermanentEvents = true;
                guard.Accuracy = 35;
                guard.Armor = 50;
                guard.Weapons.Give(WeaponHash.CarbineRifle, 200, true, true);
                // They hold the deck. FightAgainstHatedTargets sends a man toward whatever
                // he hates, and from this deck that is over the rail into the water — which
                // is why three of the original four were gone before Ice arrived. Guarding
                // the spot keeps them on the ship and still shooting.
                //
                // BlockPermanentEvents stays on only until the fight starts. Ron reported
                // ten guards who would not attack him, and this was why: a ped with
                // permanent events blocked does not react to seeing an enemy, and unlike
                // PreparationOperation this chapter has no GuardAwareness to order them
                // about. M48 already clears the same flag in WakeCordon; M45 never did.
                guard.Task.GuardCurrentPosition();
                _guards.Add(Track(guard));
            }
            model.MarkAsNoLongerNeeded();
            Logger.Info(Id + ": " + placed + " of " + DeckGuards + " deck guards are on the upper deck" +
                (empty > 0 ? "; " + empty + " posts were under cover and left empty" : "") +
                (unverified > 0 ? "; " + unverified + " stand at an unverified height" : "") + ".");
        }

        /// <summary>
        /// Whether Gohan is genuinely on the structure: out of the boat, above the
        /// waterline, and at the stern platform rather than swimming past it.
        /// </summary>
        private bool OnTheStructure()
        {
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            if (gohan == null || !gohan.Exists() || gohan.IsDead || gohan.IsInVehicle()) return false;
            if (gohan.Position.Z < PaletoSite.WaterlineDeck) return false;
            return gohan.Position.DistanceTo(At("M45.Board")) < 6f;
        }

        /// <summary>
        /// The deck fights back. Clearing BlockPermanentEvents is what lets a guard react
        /// to seeing somebody at all, and the order goes out on a cadence rather than every
        /// frame: re-issuing a combat task each tick restarts it before the ped can act on
        /// it, which is the defect behind the motionless guards in M31, M33 and M37.
        /// </summary>
        private void WakeDeck()
        {
            int woken = 0;
            foreach (var guard in _guards)
            {
                if (guard == null || !guard.Exists() || guard.IsDead) continue;
                guard.BlockPermanentEvents = false;
                woken++;
            }
            _deckOrderAt = 0;
            Logger.Info(Id + ": the deck detail is reactive now — " + woken + " of " + _guards.Count + " still standing.");
        }

        /// <summary>Keep the deck detail shooting at whoever is on their deck.</summary>
        private void PressTheDeck()
        {
            if (Game.GameTime < _deckOrderAt) return;
            _deckOrderAt = Game.GameTime + DeckOrderMs;
            var target = Game.Player.Character;
            if (target == null || !target.Exists() || target.IsDead) return;
            foreach (var guard in _guards)
            {
                if (guard == null || !guard.Exists() || guard.IsDead) continue;
                if (guard.Position.DistanceTo(target.Position) > DeckEngageRange) continue;
                if (guard.IsInCombat) continue;
                guard.Task.FightAgainst(target);
            }
        }

        private bool GuardsDown => _guards.Count == 0 || _guards.All(g => g == null || !g.Exists() || g.IsDead);

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Bring the helicopter over the rail",
                new TravelObjective("Guess: hold the Annihilator low over the vessel's upper deck", () => Insertion, 10f, () => _chopper))
                .OwnedBy(CrewSlot.Guess)
                .AfterCues("M45_S1_02_GUESS");

            // A zone objective on the pad completes while Ice is still in his seat —
            // the helicopter is over it. What this stage is actually waiting for is
            // Ice standing on the deck, so that is what it asks.
            yield return new MissionStage("Put Ice on the upper deck",
                new ConditionObjective("Ice: step off onto the upper deck", () => IceOnDeck) { Marker = () => _deck })
                .OwnedBy(CrewSlot.Ice);

            yield return new MissionStage("Clear the upper deck",
                new KillTargetsObjective("Ice: clear the deck detail", () => _guards))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(c => WakeDeck())
                .OnExit(c =>
                {
                    if (!GuardsDown) throw new InvalidOperationException("The deck detail is still up.");
                    _landed = true;
                })
                .AfterCues("M45_S1_01_ICE");

            yield return new MissionStage("Bring the Kraken to the stern",
                new TravelObjective("Gohan: take the Kraken alongside the stern platform", () => At("M45.Stern"), 12f, () => _kraken))
                .OwnedBy(CrewSlot.Gohan);

            // The condition belongs to the objective, not to an exit check behind it.
            // A ReachZone within four meters of the boarding point completes while Gohan is
            // still sitting in the Kraken below it, and the exit then threw — which is not a
            // failed mission, it is a script error that ends a five-chapter sitting. Ron lost
            // a whole run to it. Ask for the real thing up front and there is nothing left to
            // throw about.
            yield return new MissionStage("Get Gohan aboard",
                new ConditionObjective("Gohan: get out of the Kraken and climb onto the vessel", OnTheStructure)
                { Marker = () => At("M45.Board"), MarkerRadius = 4f })
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(c => _aboard = true)
                .AfterCues("M45_S1_03_GOHAN");
        }

        /// <summary>
        /// Guess keeps flying for the whole of Ice's and Gohan's work. Without this
        /// nobody is flying it while the player is someone else, and it comes down.
        ///
        /// Which pattern depends on what is happening. During the step-off he holds the
        /// spot over the deck, because Ice is aiming at a landing area a few meters
        /// wide; once Ice is down there is nothing to be precise about and the circuit
        /// at the hold marker keeps him clear of the fighting.
        /// </summary>
        protected override void OnUpdate()
        {
            Paleto.EnsureInside(Ctx);
            // Ten men who will not shoot are not a fight. Ordered on a cadence once the deck
            // stage has woken them; before that they are standing at their posts.
            if (Stage >= DeckFightStage && !GuardsDown) PressTheDeck();
            if (Stage <= InsertionStage && !IceOnDeck)
                _hold.Update(Ctx.Crew, CrewSlot.Guess, _chopper, Insertion, (int)Insertion.Z, true);
            else
                _hold.Update(Ctx.Crew, CrewSlot.Guess, _chopper, At("M45.Hold"), (int)At("M45.Hold").Z);
            base.OnUpdate();
        }

        /// <summary>The step-off stage: the last one that needs the helicopter held on a spot.</summary>
        private const int InsertionStage = 1;
        /// <summary>The stage the deck detail becomes reactive on.</summary>
        private const int DeckFightStage = 2;

        protected override void OnPassed()
        {
            if (!_landed || !_aboard) throw new InvalidOperationException("Both brothers have to be aboard before the vault.");
            var record = OperationHandoff.Capture(Paleto.Operation.Title, Id, "M46", Ctx.Crew, _chopper);
            record.Notes["deck"] = "upper deck clear; Ice holding the stair head";
            record.Notes["gohan"] = "aboard by the stern platform, out of the sub";
            record.Notes["chopper"] = "Guess holding station off the beam";
            // Whether the vessel's interior is genuinely there. Ron boarded at the stern and
            // could not get inside, and from outside the hull the two causes look identical:
            // an interior that never loaded, and a route nobody has walked. The game knows
            // which, and this is the moment to ask it.
            bool inside = Paleto.EnsureInside(Ctx);
            record.Notes["interior"] = inside ? "vessel interior pinned and ready" : "vessel interior NOT ready";
            if (!inside)
            {
                Ctx.Doctor?.Warn("interior", "Paleto vessel",
                    "the vessel's interior is not loaded, so the command deck cannot be walked to.");
                Logger.Warn(Id + ": the vessel interior is not ready as Gohan comes aboard. M46's first marker will be unreachable.");
            }
            Ctx.Handoffs.Record(record);
            Release(_chopper);
        }
    }
}
