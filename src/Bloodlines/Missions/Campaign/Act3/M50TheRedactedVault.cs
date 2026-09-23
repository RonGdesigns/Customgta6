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
    /// M50 — "The Redacted Vault". Rockford Hills, 01:30, fog.
    ///
    /// Aegis has pushed shoot-on-sight warrants to every agency on one authenticated
    /// municipal feed. Gohan splices the street conduit that carries it, Ice keeps the
    /// private security off him without killing anyone, and Guess holds the van.
    ///
    /// It happens on the street on purpose. There is no municipal archive in Rockford
    /// Hills — the zone is mansions — and no walkable records interior anywhere in the
    /// installed game. But the synopsis is already a conduit tap: "Gohan hacks an
    /// underground optical conduit into the Rockford Municipal Records archive." Forty-four
    /// street cabinets are placed across the zone; this uses the one standing in the most
    /// built-up part of it, 88 placed things within forty meters, so the splice happens on
    /// a street rather than in a field. See docs/ACT3-OPENING-MAP-M49-M53.md.
    ///
    /// What it does not do is erase anything. The feed is invalidated; local and physical
    /// copies remain, which is what the authored consequence says and what the treatment
    /// insisted on. Nobody walks away acquitted.
    ///
    /// The private security are contained, not killed. They are private guards outside a
    /// residential block at half past one in the morning, and the story wants the crew to
    /// have been here without leaving bodies in Rockford Hills.
    /// </summary>
    public sealed class M50TheRedactedVault : PreparationOperation
    {
        public const string GuardModel = "s_m_m_highsec_01";
        public const int Sentries = 3;
        /// <summary>How long the optical splice takes.</summary>
        public const int SpliceSeconds = 8;
        /// <summary>And the administrative action once the splice is live.</summary>
        public const int WipeSeconds = 10;
        /// <summary>Where the campaign records that the coordinated feed is down.</summary>
        public const string FeedEvidence = "municipalWarrantFeed";
        /// <summary>
        /// The watchmen's own relationship group. They are not BLOODLINES_AEGIS, because the
        /// crew hates that group for the whole game, and a brother told to fight "hated
        /// targets" shot a watchman dead within seconds of the start (Ron, September 22).
        /// </summary>
        public const string WatchGroup = "BLOODLINES_WATCHMEN";
        /// <summary>Charges on each brother's loaned stun gun. The gun itself goes back at teardown.</summary>
        public const int StunRounds = NonlethalCrew.DefaultRounds;

        private readonly CrewBoarding _boarding = new CrewBoarding();
        private NonlethalGuards _contained;
        /// <summary>The brothers' side of it: a stun gun in every hand, and no other gun for the ones the player is not holding.</summary>
        private readonly NonlethalCrew _stunOnly = new NonlethalCrew();
        private bool _spliced, _invalidated;

        public override string Id => "M50";
        public override string Title => "The Redacted Vault";
        protected override MissionEndpoint Endpoint => MissionEndpoint.EscapeCheckpoint;

        public Vehicle Van => CrewCar;
        /// <summary>The optical splice is live on the conduit.</summary>
        public bool Spliced => _spliced;
        /// <summary>The coordinated feed is invalidated. Copies still exist.</summary>
        public bool Invalidated => _invalidated;
        public IReadOnlyList<Ped> Sentry => Opposition;

        /// <summary>
        /// Not a fixed surface. The conduit key is where Gohan stands to work the cabinet,
        /// and a placed prop's origin is not a floor — the same mistake that put M51's limpet
        /// markers in the air. Ground preparation puts it on the pavement beside the cabinet.
        /// </summary>
        protected override string[] FixedSurfaces => new string[0];

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Gohan)) return false;

            // The brothers the player is not holding never go looking for the watchmen. Ron
            // played this twice on September 22 and failed at 16 and 10 seconds with "keep the
            // watchmen alive": a watchman inside the role tracks' threat radius sent Guess and
            // Ice after him with carbines, and a man with 1,000 health lasts about that long.
            // Keeping them contained is the player's job with a stun gun, so the tracks see no
            // threats here. Being shot at still sends a brother to cover.
            Roles = new RoleTracks(Ctx.Crew, () => Enumerable.Empty<Ped>());
            foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Gohan, CrewSlot.Guess })
                Roles.For(slot).Observe(At("M50." + slot + "Start"), At("M50." + slot + "Start"));

            CrewCar = CrewTransport("M50.Van");
            if (!RequireAssets(CrewCar)) return false;

            // The watchmen hate the crew, so they still fight back; the crew is only neutral
            // toward them, so "fight hated targets" - what a brother under fire is told - can
            // never pick one of them.
            var watch = World.AddRelationshipGroup(WatchGroup);
            var crew = Ctx.Crew.CrewGroup;
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 5, watch, crew);
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 3, crew, watch);
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS, 5, watch, Game.GenerateHash("PLAYER"));

            _contained = new NonlethalGuards();
            for (int i = 1; i <= Sentries; i++)
            {
                var guard = Enemy("M50.Guard" + i);
                // Contained rather than killed: private security outside a residential
                // block, and the story wants the crew to have been here without leaving
                // bodies in Rockford Hills.
                if (guard == null) continue;
                guard.RelationshipGroup = watch;
                _contained.Add(guard);
            }

            // "Use the stun gun" was the failure message, and only Ice had been handed one, not
            // in his hand. Ron, September 22: "they automatically shot. They didn't start with
            // stun guns." Every brother carries one now, as a mission loan that MissionManager
            // returns at teardown, has it in his hand when gameplay starts, and the two the
            // player is not holding cannot draw anything else until the attempt is over. The
            // watchmen keep their carbines; that was Ron's call too.
            _stunOnly.Begin(Ctx.Crew, StunRounds);

            Paleto.Review(Ctx, PlacementContract.Interaction("M50.Conduit"),
                PlacementContract.Vehicle("M50.Van", new Model("granger")));

            Establish("approach", "One feed, forty-four cabinets",
                "The warrants ride one authenticated municipal feed, and the conduit that carries it runs under this street. Gohan splices the cabinet, Ice keeps the security off him without killing them, Guess keeps the van running.",
                CrewCar);
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            // Parallel again: Gohan's splice and Ice's containment are the same stretch of
            // time, and the player picks which of them he wants to be.
            var splice = new MissionInteraction("Gohan: splice the optical conduit at the street cabinet",
                () => At("M50.Conduit"), SpliceSeconds, 3f, animation: MissionInteraction.ReachInside)
            { RequiredCharacter = CrewSlot.Gohan };

            var contain = new SubdueTargetsObjective("Ice: put the private security down without killing them",
                () => Opposition) { RequiredCharacter = CrewSlot.Ice };

            yield return new MissionStage("Splice the conduit", splice, contain,
                new ProtectObjective("", () => CrewCar, "The surveillance van was destroyed. There is nothing to carry the tap."))
                .OnExit(c => _spliced = true)
                // M50_S1_01_GOHAN and M50_S1_02_ICE are not fired. Their first clause fits —
                // the splice comes online — but the rest describes a vault sub-level with six
                // automated turrets, four guards and a left corridor, and there is no such
                // interior in the installed game. Narrating a place the player is not standing
                // in is worse than silence. The extraction is never edited to follow gameplay,
                // so the divergence is in data/mission_gameplay.tsv and both lines wait for an
                // interior, or for an authored revision through data/dialogue_edits.json.
                ;

            yield return new MissionStage("Invalidate the feed",
                new MissionInteraction("Gohan: push the invalidation onto the feed", () => At("M50.Conduit"),
                    WipeSeconds, 3f, animation: MissionInteraction.ReachInside))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(c => Invalidate())
                .AfterCues("M50_S1_03_GOHAN");

            yield return new MissionStage("Back to the van",
                new EnterVehicleObjective("All three: get back in the van", () => CrewCar, VehicleSeat.Driver, true),
                new ConditionObjective("Nobody is left on the street", () => Aboard))
                .AnyOf()
                // Whoever the player is holding. It used to inherit Gohan from the stage
                // before, so the HUD said "Switch to Gohan" to get into a van (Ron, September 22).
                .AnyBrother()
                .OnEnter(c => _boarding.Reset());

            yield return new MissionStage("Leave Rockford Hills",
                new TravelObjective("Guess: drive the crew clear of the block", () => At("M50.Exit"), 20f, () => CrewCar))
                .OwnedBy(CrewSlot.Guess);
        }

        private bool Aboard => CrewCar != null && CrewCar.Exists() &&
            Protagonist.All.All(hero => Ctx.Crew.PedFor(hero.Slot)?.IsInVehicle(CrewCar) == true);

        /// <summary>
        /// The feed is invalidated and nothing is erased.
        ///
        /// The authored line for this beat says it plainly — "that buys us time, not an
        /// acquittal. Their backups still exist" — and the result recorded here has to match
        /// it. Do not widen this into clearing wanted levels or deleting records: one
        /// coordinated distribution channel stops being trusted, and that is all.
        /// </summary>
        private void Invalidate()
        {
            _invalidated = true;
            Ctx.State?.SetEvidence(FeedEvidence, EvidenceState.CopyHeld);
            Logger.Info("M50: the coordinated municipal warrant feed is invalidated. Local and physical copies are untouched.");
            GameUtils.Subtitle("~g~The coordinated feed is down. Local copies still exist; this bought time, not an acquittal.", 6000);
        }

        /// <summary>A support order never picks a watchman, even if this mission ever declares a fight.</summary>
        protected override bool Spared(Ped ped) => _contained != null && _contained.Contains(ped);

        protected override void OnUpdate()
        {
            _stunOnly.Update();
            _contained?.Update();
            if (_invalidated && !Aboard)
                _boarding.Update(Ctx.Crew, CrewCar, CrewBoarding.Crew(CrewSlot.Guess), Id);
            base.OnUpdate();
        }

        protected override void OnCleanup()
        {
            // Each on its own: a brother left locked to the stun gun would never draw anything
            // else again, whatever else in teardown went wrong.
            try { _stunOnly.End(); } catch (Exception ex) { Logger.Error("M50 stun gun release", ex); }
            try { _contained?.Dispose(); } catch (Exception ex) { Logger.Error("M50 watchman release", ex); }
            base.OnCleanup();
        }

        protected override void OnPassed()
        {
            if (!_spliced || !_invalidated)
                throw new InvalidOperationException("The conduit has to be spliced and the feed invalidated.");
            Release(CrewCar);
        }
    }
}
