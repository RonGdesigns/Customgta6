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
    /// M54 — "The Pillbox Redoubt". A high-rise penthouse, 16:00, dusk and smog.
    ///
    /// Guess gets them past the private express elevator, Ice sets two heavy roosts with a
    /// line over the district, and Gohan wires an antenna into the building's emergency
    /// transmitter. The crew comes out of this with a nest for the rest of Act III.
    ///
    /// The penthouse is the Eclipse Towers interior. Ron chose that knowingly: the only two
    /// penthouse interiors the installed game will load are the Eclipse tier and the Diamond,
    /// and both are already crew homes, so the flat they break into is a flat they may
    /// already hold the keys to. The alternative was arriving on a roof by helicopter, and he
    /// preferred keeping the walkable interior and the fortify beat.
    ///
    /// What this mission does not do is invent coordinates inside an MLO. Exactly one point
    /// in that penthouse is authored — the arrival spot — and the room keys for the Luxury
    /// tier were never surveyed. Ron has just spent a day finding markers floating in the air
    /// because a number was written down that nobody had stood on, so the three work
    /// positions are offsets from the arrival point resolved to walkable floor at runtime.
    /// They are honestly "three places inside the penthouse" rather than a balcony and a
    /// service panel, and they will be on the floor because the engine puts them there.
    ///
    /// If he walks in and captures three spots with F11, they can become exact. Until then
    /// this is the version that cannot float.
    /// </summary>
    public sealed class M54ThePillboxRedoubt : PreparationOperation
    {
        /// <summary>Whose penthouse. Ice's, because the roosts are his and it is his floor.</summary>
        public const CrewSlot Penthouse = CrewSlot.Ice;
        /// <summary>How long the elevator takes to hotwire.</summary>
        public const int ElevatorSeconds = 8;
        /// <summary>How long a roost takes to set, and the antenna to wire.</summary>
        public const int RoostSeconds = 5;
        public const int AntennaSeconds = 9;
        /// <summary>How far from the arrival point the work positions are spread.</summary>
        public const float WorkSpread = 6f;
        /// <summary>Where the campaign records that the crew has an Act III nest.</summary>
        public const string NestCargo = "actThreeNest";

        private readonly List<Vector3> _roosts = new List<Vector3>();
        private Vector3 _antenna;
        private Residence _residence;
        private bool _elevator, _upstairs, _fortified;

        public override string Id => "M54";
        public override string Title => "The Pillbox Redoubt";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SafehouseArrival;

        /// <summary>The express elevator is hotwired.</summary>
        public bool Elevator => _elevator;
        /// <summary>The crew is inside the penthouse.</summary>
        public bool Upstairs => _upstairs;
        /// <summary>Both roosts and the antenna are in.</summary>
        public bool Fortified => _fortified;
        public IReadOnlyList<Vector3> Roosts => _roosts;

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Guess)) return false;

            _residence = ApartmentTiers.For(Penthouse, ApartmentTier.Luxury);
            if (Ctx.Locations.Get(_residence.EntranceKey) == null || Ctx.Locations.Get(_residence.InteriorKey) == null)
            {
                Logger.Error(Id + ": the Eclipse penthouse has no entrance or interior key; it cannot be breached.");
                GameUtils.Notify("~r~The penthouse location is missing. See Bloodlines.log.");
                return false;
            }

            Establish("approach", "Forty floors and a transmitter on the roof",
                "The penthouse has been foreclosed for a year and its express elevator still runs. Guess takes the elevator, Ice takes the roosts, Gohan takes the antenna.");
            return true;
        }

        /// <summary>
        /// Three places to work inside the penthouse, found rather than written down.
        ///
        /// Only the arrival point is authored, so each position is an offset from where the
        /// crew actually ends up, snapped to walkable floor. A spot the engine refuses is
        /// dropped back onto the arrival point, which is somewhere a man is definitely
        /// standing — better a marker in the wrong corner than one in the air.
        /// </summary>
        private void FindWorkPositions()
        {
            var arrival = Ctx.Crew.PedFor(Ctx.Crew.ActiveSlot)?.Position ?? At(_residence.InteriorKey);
            _roosts.Clear();
            var bearings = new[] { 0.0, 120.0, 240.0 };
            var found = new List<Vector3>();
            foreach (var degrees in bearings)
            {
                double radians = degrees * Math.PI / 180.0;
                var candidate = arrival + new Vector3((float)Math.Cos(radians) * WorkSpread, (float)Math.Sin(radians) * WorkSpread, 0f);
                var safe = World.GetSafeCoordForPed(candidate, false, 0);
                bool usable = safe != Vector3.Zero && Math.Abs(safe.Z - arrival.Z) < 4f && safe.DistanceTo(arrival) < WorkSpread * 2.5f;
                found.Add(usable ? safe : arrival);
                if (!usable) Logger.Warn(Id + ": no walkable floor at " + candidate + " inside the penthouse; using the arrival point.");
            }
            _roosts.Add(found[0]);
            _roosts.Add(found[1]);
            _antenna = found[2];
            Logger.Info(Id + ": roosts at " + found[0] + " and " + found[1] + ", antenna at " + found[2] + ".");
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Hotwire the express elevator",
                new MissionInteraction("Guess: hotwire the private express elevator in the lobby",
                    () => At(_residence.EntranceKey), ElevatorSeconds, 4f, animation: MissionInteraction.ReachInside))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(c => _elevator = true)
                .AfterCues("M54_S1_01_GUESS");

            // Up. ApartmentAccess owns the interior load, the fade and the entity sets, the
            // same way the home menu does; this mission does not open an MLO by hand.
            yield return new MissionStage("Forty floors up",
                new ConditionObjective("Ride the express elevator to the penthouse", () => _upstairs)
                { Marker = () => At(_residence.EntranceKey), MarkerRadius = 4f })
                .AnyOf()
                .OnEnter(c => GoUp())
                .OnExit(c => FindWorkPositions());

            // Deliberately two interactions rather than a MultiHoldObjective. That one copies
            // its site list in its constructor, and BuildStages runs before the crew is
            // upstairs, so it would have captured an empty list and completed the moment the
            // stage opened. These read their positions every frame instead.
            var first = new MissionInteraction("Ice: set the first heavy roost", () => Roost(0),
                RoostSeconds, 2.5f, animation: MissionInteraction.ReachInside)
            { RequiredCharacter = CrewSlot.Ice };
            var second = new MissionInteraction("Ice: set the second heavy roost", () => Roost(1),
                RoostSeconds, 2.5f, animation: MissionInteraction.ReachInside)
            { RequiredCharacter = CrewSlot.Ice };
            var antenna = new MissionInteraction("Gohan: wire the antenna into the building's transmitter feed",
                () => _antenna, AntennaSeconds, 2.5f, animation: MissionInteraction.ReachInside)
            { RequiredCharacter = CrewSlot.Gohan };

            yield return new MissionStage("Fortify the nest", first, second, antenna)
                .OnExit(c => Fortify())
                .AfterCues("M54_S1_02_ICE", "M54_S1_03_GOHAN");
        }

        /// <summary>
        /// Into the penthouse, through the service every home tier already uses. A mission
        /// must not load an interior by hand: the access service owns the fade, the entity
        /// sets and the exit, and it is the thing that knows how to put them back.
        /// </summary>
        private void GoUp()
        {
            var interior = Ctx.Locations.Get(_residence.InteriorKey);
            if (interior == null) { Fail("The penthouse interior is missing. Survey " + _residence.InteriorKey + "."); return; }
            var access = Ctx.Interior;
            if (access == null)
            {
                Logger.Error(Id + ": no apartment access service; the penthouse cannot be entered.");
                Fail("The penthouse could not be opened. See Bloodlines.log.");
                return;
            }
            if (!access.Begin(interior.Position, _residence.Ipl, true, _residence.Probe, interior.Heading, _residence.EntitySets))
            {
                Logger.Error(Id + ": the apartment service refused the penthouse.");
                Fail("The express elevator opened onto nothing. Retry the mission.");
                return;
            }
            _upstairs = true;
        }

        /// <summary>
        /// A roost position, or the penthouse arrival point before they have been found. Never
        /// a zero vector: a marker at the origin is a marker under the map.
        /// </summary>
        private Vector3 Roost(int index) =>
            index < _roosts.Count ? _roosts[index] : At(_residence.InteriorKey);

        private void Fortify()
        {
            _fortified = true;
            Ctx.State?.SetCargo(NestCargo, _residence.InteriorKey);
            Logger.Info(Id + ": the penthouse is the crew's Act III nest — two roosts and the antenna feed.");
            GameUtils.Subtitle("~g~Roosts set and the antenna is on the building's feed. This is the nest now.", 6000);
        }

        protected override void OnPassed()
        {
            if (!_elevator || !_upstairs || !_fortified)
                throw new InvalidOperationException("The elevator, the climb and the fortification all have to have happened.");
        }
    }
}
