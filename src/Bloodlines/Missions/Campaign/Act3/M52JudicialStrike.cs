using System;
using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M52 — "Judicial Strike". City Hall plaza, high noon.
    ///
    /// Harrison signed Aegis its immunity. Ice takes him from a roof across the plaza,
    /// Gohan confirms who he is over the radio, and Guess is waiting on the superbike.
    ///
    /// The bible puts Ice on the Union Depository roof "across the plaza" from City Hall.
    /// Those two buildings are 716 meters apart, which is not a plaza and not a shot. The
    /// roof this uses is 46 meters out and 15 above the plaza, and Rockstar already put a
    /// ladder on it — bh1_16_ladder_mission_fizz, a ladder placed for a mission — so the
    /// climb up and the way down both exist without inventing either. See
    /// docs/ACT3-OPENING-MAP-M49-M53.md.
    ///
    /// The machinery is M41's, deliberately. Bradley's chapter already runs identify, wait
    /// for a clear shot, eliminate, extract, with working failure paths for the wrong
    /// target and for firing too early, and it was written against a live report. Doing it
    /// again from scratch would only find the same bugs a second time.
    ///
    /// The superbike seats two, and that is all this needs: only Ice and Guess are at the
    /// plaza. Gohan is on the radio, which is what the synopsis says and what an earlier
    /// planning pass mistook for a conflict with a three-man extraction.
    /// </summary>
    public sealed class M52JudicialStrike : PreparationOperation
    {
        public const string BikeModel = "hakuchou2";
        public const string HarrisonModel = "a_m_m_business_01";
        public const int DetailGuards = 3;
        /// <summary>How long Ice spends on the glass before Gohan will confirm the man.</summary>
        public const int IdentifySeconds = 3;
        /// <summary>How close Harrison has to get to his clear-shot mark.</summary>
        public const float ClearShotRadius = 4f;
        /// <summary>If his walk is obstructed this long, the placement is wrong rather than the player.</summary>
        public const int WalkPatienceMs = 60000;
        /// <summary>How long he gets to reach cover once he knows.</summary>
        public const int EscapeMs = 45000;

        public Ped Harrison { get; private set; }
        private Vehicle _bike;
        private bool _identified, _clear, _struck;
        private int _alarmAt = -1, _walkAt, _orderAt;

        public override string Id => "M52";
        public override string Title => "Judicial Strike";
        protected override MissionEndpoint Endpoint => MissionEndpoint.EscapeCheckpoint;

        public Vehicle Bike => _bike;
        /// <summary>Gohan has confirmed the man on the steps is Harrison.</summary>
        public bool Identified => _identified;
        /// <summary>He is clear of his detail and the shot is allowed.</summary>
        public bool ClearShot => _clear;
        public bool Struck => _struck;

        /// <summary>The roost is a roof. Ground preparation would put the marker on the street.</summary>
        protected override string[] FixedSurfaces => new[] { "M52.Roost" };

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Ice)) return false;

            Harrison = Person(HarrisonModel, "M52.Harrison", false);
            _bike = Car(BikeModel, At("M52.Bike"), Ctx.Locations.Heading("M52.Bike"), false);
            if (!RequireAssets(Harrison, _bike)) return false;
            Harrison.Health = 200;
            RequireAsset(_bike, "The extraction bike was destroyed. Ice has no way off the plaza.");

            for (int i = 1; i <= DetailGuards; i++) Enemy("M52.Guard" + i);

            // Guess waits on it rather than walking to it when the shot lands: the authored
            // beat is "hop on", and a rider who has to cross the plaza first is not that.
            Station(CrewSlot.Guess, _bike, VehicleSeat.Driver);
            Roles.For(CrewSlot.Guess).Stop();

            Paleto.Review(Ctx, PlacementContract.Interaction("M52.Roost"),
                PlacementContract.Ped("M52.Harrison"), PlacementContract.Ped("M52.Walk"),
                PlacementContract.Vehicle("M52.Bike", new Model(BikeModel)));

            Establish("approach", "The man who signed it",
                "Ice goes up the service ladder to the roof across the plaza. Gohan reads the detail from the street and confirms the man; Guess holds the bike at the foot of the stairs. Harrison comes out at noon.",
                Harrison, _bike);
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Get on the roof",
                new ReachZoneObjective("Ice: take the service ladder to the roof across the plaza", () => At("M52.Roost"), 6f))
                .OwnedBy(CrewSlot.Ice);

            yield return new MissionStage("Identify Harrison",
                new MissionInteraction("Ice: glass the steps and let Gohan confirm the man", () => At("M52.Roost"),
                    IdentifySeconds, 6f, face: () => Harrison.Position))
                .OwnedBy(CrewSlot.Ice)
                .OnExit(c =>
                {
                    _identified = true;
                    _walkAt = Game.GameTime;
                    Harrison.Task.GoTo(At("M52.Walk"));
                    var blip = Track(Harrison.AddBlip());
                    blip.Color = BlipColor.Red;
                    blip.Name = "Chief Prosecutor Harrison";
                })
                .AfterCues("M52_S1_01_ICE");

            yield return new MissionStage("Wait for a clear shot",
                new ConditionObjective("Ice: hold until Harrison walks clear of his detail; do not fire into the escort",
                    () => Harrison.Position.DistanceTo(At("M52.Walk")) < ClearShotRadius || _alarmAt >= 0)
                { Marker = () => At("M52.Walk"), MarkerRadius = ClearShotRadius })
                .OwnedBy(CrewSlot.Ice)
                .OnExit(c => _clear = true);

            yield return new MissionStage("Take the shot",
                new ConditionObjective("Ice: take Harrison", () => Harrison.IsDead))
                .OwnedBy(CrewSlot.Ice)
                .OnExit(c => { _struck = true; Fighting = true; })
                .AfterCues("M52_S1_02_ICE");

            yield return new MissionStage("Get to the bike",
                new EnterVehicleObjective("Ice: get down off the roof and onto the back of Guess's bike", () => _bike, VehicleSeat.Passenger))
                .OwnedBy(CrewSlot.Ice)
                .AfterCues("M52_S1_03_GUESS");

            // No chase coordinate. An assassination ends when the response loses you, and
            // that is a state rather than a place.
            yield return new MissionStage("Lose the response",
                new LoseWantedObjective("Ride clear of Downtown and lose the Aegis cruisers"),
                new ProtectObjective("", () => _bike, "The bike was destroyed before they got clear."))
                .AnyOf();
        }

        protected override void OnUpdate()
        {
            if (Harrison != null && Harrison.Exists())
            {
                if (!_identified && Harrison.IsDead)
                { Fail("Harrison was shot before Gohan confirmed him. The wrong man in a suit is the wrong man."); return; }
                if (_identified && !_clear && Harrison.IsDead)
                { Fail("Harrison was shot standing in his escort. Wait for the clear-shot signal."); return; }

                // Anything loud, or Ice on the plaza rather than the roof, and Harrison knows.
                if (!Harrison.IsDead && _alarmAt < 0 && _identified &&
                    (Game.Player.Character.IsShooting || Game.Player.Character.Position.DistanceTo(Harrison.Position) < 12f))
                {
                    _alarmAt = Game.GameTime;
                    Fighting = true;
                    Awareness.ReportToAll(Stimulus.RadioCall, Harrison.Position);
                }
                if (_identified && !_clear && _alarmAt < 0 && Game.GameTime - _walkAt > WalkPatienceMs)
                { Fail("Harrison's walk across the plaza was obstructed. Retry, and resurvey M52.Walk if it repeats."); return; }
                if (_alarmAt >= 0 && !Harrison.IsDead)
                {
                    if (Game.GameTime - _alarmAt > EscapeMs) { Fail("Harrison reached cover. He signs the next renewal from inside a federal building."); return; }
                    if (Game.GameTime >= _orderAt)
                    { _orderAt = Game.GameTime + 5000; Harrison.Task.RunTo(At("M52.Harrison"), false, 10000); }
                }
            }
            base.OnUpdate();
        }

        protected override void OnPassed()
        {
            if (!_identified || !_clear || !_struck)
                throw new InvalidOperationException("Harrison has to be confirmed, taken clear of his detail, and taken.");
            Ctx.State?.SetEvidence("harrisonRemoved", EvidenceState.CopyHeld);
            Release(_bike);
        }
    }
}
