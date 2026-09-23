using System;
using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M52 — "Judicial Strike". City Hall plaza, high noon.
    ///
    /// Harrison signed Aegis its immunity. Ice takes him from the roof of City Hall's own
    /// west wing, Gohan confirms who he is over the radio, and Guess is waiting on the
    /// superbike.
    ///
    /// The bible puts Ice on the Union Depository roof "across the plaza" from City Hall.
    /// Those two buildings are 716 meters apart, which is not a plaza and not a shot. The
    /// first replacement, bh1_16's roof and its bh1_16_ladder_mission_fizz, turned out to
    /// have no way up: that ladder climbs from the roof slab to a plant deck and never
    /// touches the street (Ron, September 22: "change to where a ladder actually is").
    ///
    /// The ladders here are the game's own climbable ones, read out of the archetype's
    /// CExtensionDefLadder entries rather than guessed from a prop's name. City Hall's west
    /// wing, bh1_21_ladder2 in bh1_21_strm_0, carries five, and three of them are the way up:
    ///
    ///   street to ledge   bottom (-587.56, -200.50, 37.67)  top 45.67  facing WSW, the wing's outer wall
    ///   ledge to roof     bottom (-582.68, -199.12, 45.45)  top 48.95  at the wing's rear end
    ///   roof front edge   bottom (-571.84, -218.77, 45.45)  top 48.95  facing SSE, over the plaza
    ///
    /// The foot of the first stands on the planted ground beside the wing, grade 36.6 to 36.8
    /// (prop_bush_med_03 at 36.77, prop_veg_grass_01_b at 36.64). The roost is the top of
    /// the third, a meter back from its edge on the 48.95 roof: 47.6 m from where Harrison
    /// comes out and 52.0 m from his clear-shot mark, so he walks away from the muzzle, and
    /// eleven meters above the plaza. Every get-off flag on all three is set. See
    /// docs/ACT3-OPENING-MAP-M49-M53.md for the earlier survey.
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

        /// <summary>How far above the authored roost the roof's own slab is looked for.</summary>
        public const float RoostHeadroom = 3f;
        /// <summary>
        /// The lowest answer the probe accepts. The plaza is at 36 to 38 and the wing's lower
        /// front roof at 45.45, so anything above this is the building rather than the street.
        /// </summary>
        public const float RoostFloor = 40f;
        /// <summary>How close Ice has to be before the roof is measured: his collision, not the start's.</summary>
        public const float RoostProbeReach = 60f;
        /// <summary>How often the probe is tried while the roof's collision streams in.</summary>
        public const int RoostProbeMs = 1000;
        /// <summary>How many tries within reach before the authored height is kept and reported.</summary>
        public const int RoostProbeTries = 20;

        /// <summary>
        /// The roost is a roof, so ground preparation must not touch it — it would find the
        /// plaza eleven meters below and put Ice there. Its height is still not trusted: it is
        /// probed down onto the slab once Ice is near enough for that collision to be loaded,
        /// never in Setup, where a shape test answers only around wherever he started.
        /// </summary>
        protected override string[] FixedSurfaces => new[] { "M52.Roost" };

        /// <summary>The roof, at the height the geometry actually puts it once it has been measured.</summary>
        private Vector3 _roost;
        private bool _roostSettled;
        private int _roostTries, _roostProbeAt;

        /// <summary>Where Ice takes the shot from: the authored point until the slab under it is measured.</summary>
        public Vector3 Roost => _roost;
        /// <summary>The slab under the roost has been probed, whatever it answered.</summary>
        public bool RoostSettled => _roostSettled;

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Ice)) return false;

            // Not probed here. Ice starts on the street 88 m from the wing, and a probe only
            // answers where collision is loaded; SettleRoost measures it on his way up.
            _roost = At("M52.Roost");

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
                "Ice goes up the service ladders on City Hall's west wing to the edge of its roof over the plaza. Gohan reads the detail from the street and confirms the man; Guess holds the bike at the foot of the stairs. Harrison comes out at noon.",
                Harrison, _bike);
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            // The way up first, at street level: a GPS route to a point on a roof follows the
            // streets and never says which wall to climb. M52.Ladder is where a man stands to
            // take the street ladder, 0.8 m out from its bottom along the ladder's own facing
            // (-0.87, -0.50). An estimate: nobody has climbed it yet.
            yield return new MissionStage("Find the way up",
                new ReachZoneObjective("Ice: get to the service ladder on the outer wall of City Hall's west wing", () => At("M52.Ladder"), 4f))
                .OwnedBy(CrewSlot.Ice);

            yield return new MissionStage("Get on the roof",
                new ReachZoneObjective("Ice: climb the ladder to the ledge, take the next ladder up onto the wing's roof, and cross to its front edge over the plaza", () => _roost, 6f))
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

        /// <summary>
        /// The roost's real height, measured once Ice is within <see cref="RoostProbeReach"/>
        /// of it and the collision around him has loaded. Nothing found keeps the authored
        /// height and says so; the player is never moved, because he is the one climbing.
        /// </summary>
        private void SettleRoost()
        {
            if (_roostSettled || Game.GameTime < _roostProbeAt) return;
            _roostProbeAt = Game.GameTime + RoostProbeMs;
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var authored = At("M52.Roost");
            if (ice == null || !ice.Exists() || !GameUtils.IsWithinFlat(ice.Position, authored, RoostProbeReach)) return;
            if (!Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY, ice) && ++_roostTries < RoostProbeTries) return;
            _roostSettled = true;
            float? surface = MissionSites.SurfaceHeight(authored, authored.Z + RoostHeadroom, RoostFloor);
            if (!surface.HasValue)
            {
                Logger.Warn(Id + ": nothing solid under the roost at " + authored + "; keeping the authored height. Survey M52.Roost.");
                return;
            }
            _roost = new Vector3(authored.X, authored.Y, surface.Value);
            Logger.Info(Id + ": the wing roof is at " + surface.Value.ToString("0.00") + ", not the authored " + authored.Z.ToString("0.00") + ".");
        }

        protected override void OnUpdate()
        {
            SettleRoost();
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
