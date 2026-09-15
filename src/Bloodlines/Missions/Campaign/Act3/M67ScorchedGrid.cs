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
    /// M67 — "Scorched Grid". Inside the extraction semi, on the move.
    ///
    /// Gohan routes the five hundred million out of the Aegis escrow and into accounts nobody
    /// can follow, while Guess drives and Ice watches the road behind.
    ///
    /// This is the quiet one between two sieges, and it is deliberately not a fight. The
    /// synopsis is a man at a laptop in a moving truck, so the mission's whole job is to keep
    /// the truck moving while he works and to make the work take long enough to matter. Guess
    /// keeps a standing destination so the rig does not stop the moment the player switches to
    /// Gohan, which is the `DrivingDestination` contract M35 established.
    ///
    /// **There is no sub-station.** The bible files this under "Del Perro Industrial
    /// Sub-Station" and no such model exists in the archives. It matters less than it would
    /// anywhere else, because every beat of the mission happens inside a moving vehicle; the
    /// route runs from Del Perro to the airport, which is where `M67_S1_03_ICE` says they are
    /// going next. Recorded in `data/mission_gameplay.tsv`.
    ///
    /// The transfer is not a win, and the authored line is careful about that — "the money's
    /// accessible, it isn't invisible" — so the mission records the money as moved and nothing
    /// more. Whether they keep it is M68 to M70.
    /// </summary>
    public sealed class M67ScorchedGrid : PreparationOperation
    {
        public const string SemiModel = "phantom";
        public const string TrailerModel = "trailers";
        /// <summary>How long the transfer takes. Five hundred million across a lot of accounts.</summary>
        public const int TransferSeconds = 25;
        /// <summary>Under this the rig has stopped.</summary>
        public const float MovingSpeed = 4f;
        /// <summary>How long it may sit still before the mission says something.</summary>
        public const int StallPatienceMs = 20000;
        /// <summary>Where the campaign records the money is out.</summary>
        public const string FundsEvidence = "escrowDistributed";

        private Vehicle _semi, _trailer;
        private int _stalledAt;
        private bool _rolling, _transferred;

        public override string Id => "M67";
        public override string Title => "Scorched Grid";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;

        /// <summary>The rig is on the road with everybody aboard.</summary>
        public bool Rolling => _rolling;
        /// <summary>The money is distributed.</summary>
        public bool Transferred => _transferred;
        public Vehicle Semi => _semi;
        public Vehicle Trailer => _trailer;

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Guess)) return false;

            _semi = Car(SemiModel, At("M67.Semi"), Ctx.Locations.Heading("M67.Semi"), true);
            _trailer = _semi == null ? null
                : Car(TrailerModel, _semi.Position - _semi.ForwardVector * 11f, Ctx.Locations.Heading("M67.Semi"), false);
            if (!RequireAssets(_semi, _trailer)) return false;
            _semi.IsPersistent = true;
            _trailer.IsPersistent = true;
            Function.Call(Hash.ATTACH_VEHICLE_TO_TRAILER, _semi, _trailer, 2f);
            RequireAsset(_semi, "The extraction rig was destroyed with the money still in escrow.");

            // Guess drives, Gohan works, Ice watches the road behind. Nobody is asked for a
            // seat the cab has not got.
            Station(CrewSlot.Guess, _semi, VehicleSeat.Driver);
            Station(CrewSlot.Gohan, _semi, VehicleSeat.Passenger);

            Paleto.Review(Ctx, PlacementContract.Vehicle("M67.Semi", new Model(SemiModel)),
                PlacementContract.Ped("M67.Start"));

            Establish("approach", "Five hundred million, one laptop",
                "Gohan has the authorizations and the rig has the road. The transfer takes as long as it takes, and the truck does not stop while it runs.",
                _semi);
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Get the rig rolling",
                new ConditionObjective("Guess: get the rig on the road to the airport", Moving)
                { Marker = () => _semi != null && _semi.Exists() ? _semi.Position : At("M67.Semi"), MarkerRadius = 6f })
                .OwnedBy(CrewSlot.Guess)
                .OnExit(c => { _rolling = true; DrivingDestination = () => At("M67.Airport"); });

            yield return new MissionStage("Move the money",
                new MissionInteraction("Gohan: route the escrow into the offshore accounts",
                    () => _semi != null && _semi.Exists() ? _semi.Position : At("M67.Semi"),
                    TransferSeconds, 6f, animation: MissionInteraction.ReachInside)
                { RequiredCharacter = CrewSlot.Gohan },
                new ProtectObjective("", () => _semi, "The rig was wrecked with the money still in escrow."))
                .OnExit(c => Moved())
                .AfterCues("M67_S1_01_GOHAN", "M67_S1_02_GUESS");

            yield return new MissionStage("Make the airport",
                new TravelObjective("Take the rig to the airport perimeter", () => At("M67.Airport"), 30f, () => _semi))
                .AnyBrother()
                .OnExit(c => DrivingDestination = null)
                .AfterCues("M67_S1_03_ICE");
        }

        /// <summary>Whether the rig is actually under way.</summary>
        private bool Moving() => _semi != null && _semi.Exists() && _semi.Speed > MovingSpeed;

        private void Moved()
        {
            _transferred = true;
            // Accessible, not invisible. The line is careful about that and so is this.
            Ctx.State?.SetEvidence(FundsEvidence, EvidenceState.CopyHeld);
            Logger.Info(Id + ": the escrow is distributed. The money is reachable; it is not hidden.");
        }

        protected override void OnUpdate()
        {
            // A transfer in a stopped truck is a transfer at a roadblock. Say so rather than
            // letting the player wonder why the clock is not moving.
            if (_rolling && !_transferred && _semi != null && _semi.Exists())
            {
                if (_semi.Speed > MovingSpeed) _stalledAt = Game.GameTime;
                else if (_stalledAt > 0 && Game.GameTime - _stalledAt > StallPatienceMs)
                {
                    _stalledAt = Game.GameTime;
                    GameUtils.Subtitle("~y~Keep the rig moving. A parked truck on this road is a roadblock.", 4000);
                }
            }
            base.OnUpdate();
        }

        protected override void OnPassed()
        {
            if (!_rolling || !_transferred)
                throw new InvalidOperationException("The rig has to have rolled and the money has to have moved.");
            Release(_semi);
            Release(_trailer);
        }
    }
}
