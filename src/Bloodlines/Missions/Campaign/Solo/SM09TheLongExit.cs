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
    /// SM09 — "The Long Exit". Guess alone, and a delivery.
    ///
    /// A foreign captain wants an impounded prototype on his cargo deck before dawn, and the
    /// passports for all three families come with it. Guess takes it out of the pound and
    /// drives it across the city with everything the LSPD has behind him.
    ///
    /// The berth is real: the Terminal Island quay runs along y = -2899 with
    /// <c>prop_dock_moor_04</c> and <c>prop_dock_moor_01</c> from x 966 to 1225 and
    /// <c>port_xr_lifep</c> cranes standing over it at (981.4, -2865.4, 11.02) and
    /// (981.4, -2896.8, 11.02). The container the car goes into sits on that quay among three
    /// hundred and fifty-eight <c>prop_container_01d</c>, which is what a container terminal
    /// looks like from the archives.
    ///
    /// **This is a driving mission and it is built as one.** The pursuit is the campaign's own
    /// wanted machinery rather than scripted chase cars — Guess starts at five stars and the
    /// mission ends when the car is in the box, not when the stars are gone. That is the honest
    /// reading of "a five-star police gauntlet": the police are the obstacle, not a set of
    /// spawned props.
    ///
    /// **Two beats are not reproduced.** `SM09_S1_02_GUESS` has SWAT laying spike strips on a
    /// named bridge and `SM09_S2_03_GUESS` launches the car off a construction ramp over a
    /// cruiser. Both need placed geometry on a route the player chooses for himself, and a
    /// spike strip on a road he may never take is a prop in an empty lane. The Slipstream
    /// Reflex his line calls for is his own ability and is available throughout. Recorded in
    /// `data/mission_gameplay.tsv`.
    ///
    /// The hypercar is a <c>t20</c>: a prototype hypercar the game actually has, rather than a
    /// name invented for the bible.
    /// </summary>
    public sealed class SM09TheLongExit : DesertOperation
    {
        public const string HyperModel = "t20";
        /// <summary>What the LSPD brings, from the moment he takes it.</summary>
        public const int Stars = 5;
        /// <summary>How long the hotwire takes.</summary>
        public const int HotwireSeconds = 7;
        /// <summary>How near the container counts as delivered.</summary>
        public const float BerthRadius = 8f;
        /// <summary>Where the campaign records the passages are secured.</summary>
        public const string PassageEvidence = "exitPassages";

        private Vehicle _hyper;
        private bool _taken, _delivered;

        public override string Id => "SM09";
        public override string Title => "The Long Exit";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;

        /// <summary>The car is his.</summary>
        public bool Taken => _taken;
        /// <summary>It is in the container on the quay.</summary>
        public bool Delivered => _delivered;
        public Vehicle Hypercar => _hyper;

        protected override bool Setup()
        {
            _hyper = Car(HyperModel, At("SM09.Pound"), Ctx.Locations.Heading("SM09.Pound"), true);
            if (!RequireAssets(_hyper)) return false;
            _hyper.IsPersistent = true;
            _hyper.IsEngineRunning = false;
            RequireAsset(_hyper, "The prototype was destroyed. There is no passage without it.");

            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            if (guess == null || !guess.Exists()) { Logger.Error(Id + ": Guess is not available for his own solo."); return false; }

            Ctx.Cutscenes.Play(new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "Before dawn, on his deck",
                Reason = "Show the impounded prototype before Guess takes it. He is on his own for this one.",
                Blocking = new SceneBlocking().Then(ShotStep.Low(2200, _hyper, 6, 4, 2))
            });
            RequireSurvivor(guess, "Guess is down. Restart this solo mission.");
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Take it out of the pound",
                new MissionInteraction("Guess: hotwire the prototype", () => At("SM09.Pound"),
                    HotwireSeconds, 3f, animation: MissionInteraction.ReachInside)
                { RequiredCharacter = CrewSlot.Guess })
                .OnExit(c => Hotwired())
                .WithCues("SM09_S1_01_GUESS");

            // SM09_S1_02_GUESS lays spike strips on a named bridge and SM09_S2_03_GUESS jumps a
            // construction ramp. Both need geometry on a route the player picks himself, so
            // neither is fired; the gauntlet is the real wanted response instead.
            yield return new MissionStage("Get it to the terminal",
                new DeliverVehicleObjective("Guess: get the prototype to the container on the quay",
                    () => _hyper, () => At("SM09.Berth"), BerthRadius),
                new ProtectObjective("", () => _hyper, "The prototype was wrecked before it reached the ship."))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(c => Landed())
                .AfterCues("SM09_S2_04_GUESS");
        }

        private void Hotwired()
        {
            _taken = true;
            if (_hyper != null && _hyper.Exists()) _hyper.IsEngineRunning = true;
            // The gauntlet is the police, not a set of spawned cars.
            Game.Player.WantedLevel = Stars;
            Logger.Info(Id + ": the prototype is running and the city knows.");
        }

        private void Landed()
        {
            _delivered = true;
            Ctx.State?.SetEvidence(PassageEvidence, EvidenceState.CopyHeld);
            Logger.Info(Id + ": the prototype is on the deck and the passages are stamped.");
            GameUtils.Subtitle("~g~Passports and sea passage, for all three families.", 6000);
        }

        protected override void OnPassed()
        {
            if (!_taken || !_delivered)
                throw new InvalidOperationException("The prototype has to be taken and delivered.");
            Release(_hyper);
        }
    }
}
