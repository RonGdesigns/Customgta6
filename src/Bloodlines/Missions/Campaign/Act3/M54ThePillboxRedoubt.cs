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
    /// M54 — "The Pillbox Redoubt". A Pillbox Hill tower roof, 16:00, dusk and smog.
    ///
    /// Guess flies the crew up in an Annihilator and puts it down on the roof helipad. Ice
    /// sets two heavy roosts along the parapet; Gohan wires an antenna into the roof's
    /// electrical feed. The crew comes off that roof holding a forward post for the rest of
    /// Act III.
    ///
    /// Ron chose the roof over the penthouse, and the archives are the reason it works. The
    /// authored beat is a private express elevator into a foreclosed flat, and the only two
    /// penthouse interiors the installed game will load are already crew homes, so that
    /// version had to put three work positions inside an MLO nobody has ever surveyed. This
    /// one stands on placed geometry the whole way up:
    ///
    ///   dt1_02_helipad        (-142.67, -593.35, 206.31)   the landing pad
    ///   dt1_02_w01_rail       (-144.74, -593.61, 204.13)   the parapet rail beside it
    ///   prop_elecbox_23       (-146.19, -598.05, 206.76)   the cabinet Gohan taps
    ///   prop_elecbox_18       (-136.58, -593.24, 207.36)   the east side of the same deck
    ///   prop_wall_light_03a   a ring at 209.15 spanning x -138..-150.5, y -587.5..-599.2
    ///
    /// That light ring is the deck's own outline: roughly twelve meters by twelve, with the
    /// pad in the middle of it. Every position this mission uses is inside that rectangle and
    /// takes its x and y from something Rockstar placed on the deck. What it does not take
    /// from those props is a standing height — a prop's origin is not a floor, which is the
    /// mistake that left M51's marker in the air and M52's roost off the side of a building.
    /// Each one is probed down onto the slab in <see cref="FindWorkPositions"/>, and that
    /// probe runs when the crew is standing on the roof rather than in Setup, because the
    /// collision two hundred meters over a building sixteen hundred meters away is not
    /// streamed while they are still in the yard.
    ///
    /// The tower is not Maze Bank, which matters: Maze Bank's roof (dt1_11_heliport, 323.26)
    /// is the only other helipad in the city core, and Ice's authored line claims a firing
    /// line on Maze Bank Tower. Standing on a different Pillbox Hill roof keeps that line
    /// true. The lower tiers of this same building — a deck at 199.13 with solar panels and
    /// prop_radiomast01, three more pads at 175.52 — are deliberately unused: a radio mast is
    /// a better-sounding antenna than a junction box, and there is no evidence in the archives
    /// that a man can walk down to it from the pad deck.
    ///
    /// They leave from the Cypress Flats foundry, which is their own yard and real open
    /// ground: the widest clear spot within a hundred meters of the apron, 15.7 m to the
    /// nearest placed object.
    /// </summary>
    public sealed class M54ThePillboxRedoubt : PreparationOperation
    {
        /// <summary>Four seats, and the helicopter the crew already flies.</summary>
        public const string HelicopterModel = "annihilator";
        /// <summary>How long a roost takes to set, and the antenna to wire.</summary>
        public const int RoostSeconds = 5;
        public const int AntennaSeconds = 9;
        /// <summary>How near the pad the skids have to be down. A twelve-meter deck, so this is the deck.</summary>
        public const float PadRadius = 14f;
        /// <summary>Altitude Guess circles at if the player leaves him flying. Above the tower, clear of Maze Bank.</summary>
        public const int HoldAltitude = 250;
        /// <summary>How far above and below an authored roof point the slab is looked for.</summary>
        public const float RoofHeadroom = 4f;
        public const float RoofFloor = 196f;
        /// <summary>Where the campaign records that the crew has an Act III nest.</summary>
        public const string NestCargo = "actThreeNest";

        private readonly AircraftHold _hold = new AircraftHold();
        private readonly List<Vector3> _roosts = new List<Vector3>();
        private Vehicle _helicopter;
        private Vector3 _antenna;
        private bool _aboard, _landed, _fortified;

        public override string Id => "M54";
        public override string Title => "The Pillbox Redoubt";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SafehouseArrival;

        /// <summary>All three are in the Annihilator.</summary>
        public bool Aboard => _aboard;
        /// <summary>The skids are on the roof pad.</summary>
        public bool Landed => _landed;
        /// <summary>Both roosts and the antenna are in.</summary>
        public bool Fortified => _fortified;
        public Vehicle Helicopter => _helicopter;
        public IReadOnlyList<Vector3> Roosts => _roosts;
        public Vector3 AntennaAt => _antenna;

        /// <summary>
        /// The pad and the three work positions are two hundred meters of air above the
        /// street, so ground preparation must not touch them: asking the engine for walkable
        /// ground near a roof gets the sidewalk underneath it.
        /// </summary>
        protected override string[] FixedSurfaces =>
            new[] { "M54.Pad", "M54.RoostNorth", "M54.RoostSouth", "M54.Antenna" };

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Guess)) return false;

            // On the ground, cold. BloodlinesMain runs KeepPlayerAircraftRunning every
            // frame, so it starts when Guess is aboard and stays off while nobody is —
            // which is M26's fix, and the reason this is not LaunchAirborne: an aircraft
            // created on a deck has nothing to fall out of.
            _helicopter = Car(HelicopterModel, At("M54.Lift"), Ctx.Locations.Heading("M54.Lift"), false);
            if (!RequireAssets(_helicopter)) return false;
            _helicopter.IsPersistent = true;
            RequireAsset(_helicopter, "The Annihilator was destroyed. There is no other way onto that roof.");

            Paleto.Review(Ctx,
                PlacementContract.Aircraft("M54.Lift", new Model(HelicopterModel), 0f),
                PlacementContract.Ped("M54.Start"), PlacementContract.Ped("M54.IceStart"),
                PlacementContract.Ped("M54.GohanStart"), PlacementContract.Ped("M54.GuessStart"));

            Establish("approach", "Two hundred meters of nobody's business",
                "Nobody watches a roof at dusk. Guess flies them up and puts the Annihilator on the pad; Ice sets the roosts along the parapet and Gohan takes the roof's own feed for the antenna.",
                _helicopter);
            return true;
        }

        /// <summary>Whether a brother is in the helicopter, in any seat.</summary>
        private bool Seated(CrewSlot slot)
        {
            var ped = Ctx.Crew.PedFor(slot);
            return ped != null && ped.Exists() && _helicopter != null && _helicopter.Exists() &&
                ped.IsInVehicle(_helicopter);
        }

        /// <summary>
        /// The three work positions, taken off the roof rather than out of the air.
        ///
        /// Each x and y belongs to something placed on that deck; each height comes from a
        /// downward probe onto the slab under it. This runs on arrival, not in Setup: the
        /// probe is a shape test, and a shape test only answers where collision is loaded.
        /// </summary>
        private void FindWorkPositions()
        {
            _roosts.Clear();
            _roosts.Add(Probe("M54.RoostNorth", "north roost"));
            _roosts.Add(Probe("M54.RoostSouth", "south roost"));
            _antenna = Probe("M54.Antenna", "antenna cabinet");
            Logger.Info(Id + ": roosts at " + _roosts[0] + " and " + _roosts[1] + ", antenna at " + _antenna + ".");
        }

        private Vector3 Probe(string key, string what) =>
            MissionSites.OnSurface(At(key), RoofHeadroom, RoofFloor, Id + " " + what, 3);

        /// <summary>
        /// A roost position, or the pad before they have been found. Never a zero vector: a
        /// marker at the origin is a marker under the map.
        /// </summary>
        private Vector3 Roost(int index) => index < _roosts.Count ? _roosts[index] : At("M54.Pad");

        private Vector3 AntennaPoint() => _antenna == Vector3.Zero ? At("M54.Antenna") : _antenna;

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Get the crew aboard",
                new EnterVehicleObjective("Guess: get in the Annihilator", () => _helicopter, VehicleSeat.Driver),
                new ConditionObjective("Ice and Gohan: in the back",
                    () => Seated(CrewSlot.Ice) && Seated(CrewSlot.Gohan))
                {
                    Marker = () => _helicopter != null && _helicopter.Exists() ? _helicopter.Position : At("M54.Lift"),
                    MarkerRadius = 6f
                })
                .OwnedBy(CrewSlot.Guess);

            // The authored M54_S1_01_GUESS clones an express-elevator keycard, which this
            // version does not have. It goes unplayed and the approach is called over the
            // radio instead; data/mission_gameplay.tsv records the swap.
            yield return new MissionStage("Put it on the roof",
                new DeliverVehicleObjective("Guess: land on the tower helipad",
                    () => _helicopter, () => At("M54.Pad"), PadRadius, true),
                new ProtectObjective("", () => _helicopter,
                    "The Annihilator came down before it reached the roof."))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(c => Radio("GUESS",
                    "Forget the elevator. Nobody watches a roof at dusk - I will set us down on the pad and we step off the skids.",
                    "M54_RADIO_01_GUESS"))
                .OnExit(c => { _landed = true; Disembark(); FindWorkPositions(); });

            // Three interactions in one stage rather than a MultiHoldObjective, which copies
            // its site list in its constructor: BuildStages runs long before the roof has
            // been probed, so that list would be captured empty and complete instantly.
            // These read their positions every frame, and being parallel they let the player
            // move between Ice and Gohan as he likes.
            var north = new MissionInteraction("Ice: set the north roost on the parapet", () => Roost(0),
                RoostSeconds, 2.5f, animation: MissionInteraction.ReachInside)
            { RequiredCharacter = CrewSlot.Ice };
            var south = new MissionInteraction("Ice: set the south roost, facing Maze Bank", () => Roost(1),
                RoostSeconds, 2.5f, animation: MissionInteraction.ReachInside)
            { RequiredCharacter = CrewSlot.Ice };
            var antenna = new MissionInteraction("Gohan: wire the antenna into the roof's feed", AntennaPoint,
                AntennaSeconds, 2.5f, animation: MissionInteraction.ReachInside)
            { RequiredCharacter = CrewSlot.Gohan };

            yield return new MissionStage("Fortify the nest", north, south, antenna)
                .OnExit(c => Fortify())
                .AfterCues("M54_S1_02_ICE", "M54_S1_03_GOHAN");
        }

        /// <summary>
        /// Off the skids. The engine is left alone — KeepPlayerAircraftRunning owns it while
        /// the player is in it, and fighting that would flicker the rotors while he climbs
        /// out.
        /// </summary>
        private void Disembark()
        {
            _hold.Release();
            if (_helicopter == null || !_helicopter.Exists()) return;
            foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Gohan, CrewSlot.Guess })
            {
                var ped = Ctx.Crew.PedFor(slot);
                if (ped != null && ped.Exists() && ped != Game.Player.Character && ped.IsInVehicle(_helicopter))
                    ped.Task.LeaveVehicle();
            }
        }

        private void Fortify()
        {
            _fortified = true;
            Ctx.State?.SetCargo(NestCargo, "M54.Pad");
            Logger.Info(Id + ": the tower roof is the crew's Act III nest — two roosts and the antenna on the roof feed.");
            GameUtils.Subtitle("~g~Roosts set and the antenna is on the building's feed. This roof is the nest now.", 6000);
        }

        protected override void OnUpdate()
        {
            if (_helicopter != null && _helicopter.Exists() && !_landed)
            {
                if (!_aboard)
                {
                    _aboard = Seated(CrewSlot.Ice) && Seated(CrewSlot.Gohan) && Seated(CrewSlot.Guess);
                    if (!_aboard) BoardBrothers(_helicopter);
                }
                // Nobody flies a helicopter the player has walked away from. Switching out of
                // Guess in the air used to mean an aircraft with no pilot; the shared hold
                // circles it over the pad until he comes back to it.
                else _hold.Update(Ctx.Crew, CrewSlot.Guess, _helicopter, At("M54.Pad"), HoldAltitude);
            }
            base.OnUpdate();
        }

        protected override void OnCleanup()
        {
            _hold.Release();
            base.OnCleanup();
        }

        protected override void OnPassed()
        {
            if (!_landed || !_fortified)
                throw new InvalidOperationException("The roof has to have been reached and the nest fortified.");
            // Handed back to the world rather than deleted: the nest is two hundred meters
            // up and this is the way down.
            Release(_helicopter);
        }
    }
}
