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
    /// roof this uses is 96 meters out, and Ice stands at the top of a ladder Rockstar
    /// placed for a mission — bh1_16_ladder_mission_fizz — so the climb up and the way down
    /// both exist without inventing either. Standing on the ladder's own spot rather than
    /// somewhere else on the same roof is deliberate: it is the one point that is certainly
    /// reachable. See docs/ACT3-OPENING-MAP-M49-M53.md.
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
        /// <summary>
        /// How far from the steps the ride takes them, away from the plaza on the side the bike
        /// waited. The getaway was a lone LoseWantedObjective, which passes the instant Ice is
        /// on the bike when nobody has called it in, and when somebody had, Ice was a passenger
        /// on a bike whose rider had no order to go anywhere (Ron, September 22).
        /// </summary>
        public const float EscapeMeters = 450f;
        public const float EscapeRadius = 40f;
        /// <summary>How far from the derived escape point a real road node is accepted.</summary>
        public const float EscapeRoadSearch = 120f;

        public Ped Harrison { get; private set; }
        private Vehicle _bike;
        private bool _identified, _clear, _struck;
        private Vector3 _escape;
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

        /// <summary>
        /// The roost is a roof, so ground preparation must not touch it — it would find the
        /// street eighty feet below and put Ice there. But the authored height came off a
        /// ladder prop's origin, and a prop origin is not a floor: Ron found the marker
        /// floating and no way up the side of the building. Fixed against the ground snap,
        /// and probed down onto the actual slab in Setup.
        /// </summary>
        protected override string[] FixedSurfaces => new[] { "M52.Roost" };

        /// <summary>The roof, at the height the geometry actually puts it.</summary>
        private Vector3 _roost;

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Ice)) return false;

            // The key sits at the top of the ladder; the slab under it is where Ice stands.
            _roost = MissionSites.OnSurface(At("M52.Roost"), 3f, 38f, Id + " roof roost");

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
            // The way up first. Ron's only attempt ended 54 m west of the roost with nothing
            // telling him how to get there: a GPS route to a point on a roof follows the
            // streets, and the ladder under the roost does not start at the street. The
            // archives put bh1_16_ladder_mission_fizz on the roof itself, climbing from the
            // slab at about 50.3 (roof vents and air handlers stand round it at that height),
            // and the only way onto that roof they show is bh1_16_scaff on the block's south
            // corner: tool boxes and paint benches at 40.7, 41.5, 44.1, 46.8 and 49.3, which
            // is a scaffold with working platforms from the street to the roof. M52.Scaffold
            // is the sidewalk at its foot. An estimate: nobody has climbed it yet.
            yield return new MissionStage("Find the way up",
                new ReachZoneObjective("Ice: get to the scaffolding on the south corner of the roost's block", () => At("M52.Scaffold"), 4f))
                .OwnedBy(CrewSlot.Ice);

            yield return new MissionStage("Get on the roof",
                new ReachZoneObjective("Ice: climb the scaffolding, cross the roof north-west and take the service ladder up to the roost", () => _roost, 6f))
                .OwnedBy(CrewSlot.Ice);

            yield return new MissionStage("Identify Harrison",
                new MissionInteraction("Ice: glass the steps and let Gohan confirm the man", () => _roost,
                    IdentifySeconds, 6f, face: () => Harrison.Position))
                .OwnedBy(CrewSlot.Ice)
                .OnExit(c =>
                {
                    _identified = true;
                    _walkAt = Game.GameTime;
                    Harrison.Task.GoTo(At("M52.Walk"));
                    Blips.Attach(Harrison, BlipColor.Red, "Chief Prosecutor Harrison");
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

            // An assassination ends when the response loses you, and that is still the rule:
            // the stage does not close while anyone is after them. But losing heat nobody
            // raised is not a getaway, so they also have to be clear of the plaza, and Guess
            // has somewhere to ride while the player sits pillion as Ice. The point is derived
            // from the steps and the bike, never authored, and snapped to a real road.
            yield return new MissionStage("Lose the response",
                new TravelObjective("Guess: ride Ice clear of the plaza", Escape, EscapeRadius, () => _bike),
                new LoseWantedObjective("Ride clear of Downtown and lose the Aegis cruisers"),
                new ProtectObjective("", () => _bike, "The bike was destroyed before they got clear."))
                .AnyBrother()
                .OnEnter(c => DrivingDestination = Escape)
                .OnExit(c => DrivingDestination = null);
        }

        /// <summary>
        /// Where the ride ends: <see cref="EscapeMeters"/> from the steps, on the far side of
        /// the bike from them, moved onto the nearest road node when the game has one there.
        /// Worked out once, the first time it is asked for.
        /// </summary>
        private Vector3 Escape()
        {
            if (_escape != Vector3.Zero) return _escape;
            var steps = At("M52.Harrison");
            var away = At("M52.Bike") - steps;
            away = new Vector3(away.X, away.Y, 0f);
            if (away.Length() < 1f) away = new Vector3(-1f, 0f, 0f);
            var seed = steps + away * (EscapeMeters / away.Length());
            if (GameUtils.NearestRoadNode(seed, EscapeRoadSearch, out var node, out float _)) _escape = node;
            else
            {
                _escape = GameUtils.OnGround(seed);
                Logger.Warn(Id + ": no road node near the escape point " + seed + "; riding for the point itself.");
            }
            return _escape;
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
