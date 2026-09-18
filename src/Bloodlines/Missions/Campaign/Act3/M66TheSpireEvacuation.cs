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
    /// M66 — "The Spire Evacuation". The Maze Bank roof, 22:30, and then the air.
    ///
    /// Aegis gunships are working the top of the tower. The three of them go off the roof and
    /// steer down through the towers to the Del Perro connector.
    ///
    /// The roof is <c>dt1_11_heliport</c> at (-75.20, -818.95, **323.26**), the same pad M54
    /// established exists. See <see cref="MazeBank"/> for the tower's stack.
    ///
    /// **There is no antenna spire.** A survey of everything above the roof deck returns
    /// forty-eight entities and the tallest is 325.17 — a ring of <c>prop_air_lights_02b</c> on
    /// the parapet. Nothing to climb. So the crew jumps from the roof, and the authored line
    /// survives the change without being touched: `M66_S1_02_ICE` opens his chute at eight
    /// hundred feet, and the roof is 323 m, about 1,060 feet, so that is a real number taken
    /// from a real height. `M66_S1_01_GUESS` says "only way off this tower is into the sky",
    /// which a roof edge serves as well as a mast. The adaptation is in
    /// `data/mission_gameplay.tsv`.
    ///
    /// **The landing is a road, so it is found rather than written.** Roads are baked terrain
    /// and no placed-entity survey can locate a freeway lane — that is M49's problem exactly.
    /// `M66.Landing` is a seed near the Del Perro connector and the real lane comes from
    /// <c>GameUtils.NearestRoadNode</c> at runtime. A missing node is reported and the seed
    /// used: a landing zone slightly off the asphalt is recoverable, a refused mission is not.
    ///
    /// Ice's line names the Arcadius towers on the way down. They are real and they are on the
    /// line: the Arcadius block is at (-139, -629), between this roof and Del Perro.
    /// </summary>
    public sealed class M66TheSpireEvacuation : PreparationOperation
    {
        public const string GunshipModel = "savage";
        public const string PilotModel = "s_m_y_blackops_01";
        /// <summary>QRF gunships working the roof.</summary>
        public const int Gunships = 3;
        /// <summary>How wide and fast they circle the spire.</summary>
        public const float SweepRadius = 90f;
        public const float SweepSpeed = 34f;
        /// <summary>Their altitude. Above the roof, so they are looking down at it.</summary>
        public const int SweepHeight = 360;
        /// <summary>How far the drop is before the mission calls it a jump.</summary>
        public const float JumpedBelow = 260f;
        /// <summary>How near the connector counts as down.</summary>
        public const float LandingRadius = 30f;
        /// <summary>How far a road node may be from the seed before it is refused.</summary>
        public const float LaneSearch = 160f;
        /// <summary>Where the campaign records the crew is off the tower.</summary>
        public const string ClearCargo = "mazeBankCleared";

        private readonly List<Vehicle> _gunships = new List<Vehicle>();
        private Vector3 _landing;
        private bool _jumped, _down;

        public override string Id => "M66";
        public override string Title => "The Spire Evacuation";
        protected override MissionEndpoint Endpoint => MissionEndpoint.EscapeCheckpoint;

        /// <summary>Somebody has gone off the roof.</summary>
        public bool Jumped => _jumped;
        /// <summary>They are down on the connector.</summary>
        public bool Down => _down;
        public IReadOnlyList<Vehicle> Gunship => _gunships;
        /// <summary>The lane the landing actually resolved to.</summary>
        public Vector3 Landing => _landing;

        /// <summary>
        /// The roof is three hundred and twenty meters over the street, so the walkable query
        /// would hand back the street for any of these.
        /// </summary>
        protected override string[] FixedSurfaces =>
            new[] { "M66.Start", "M66.IceStart", "M66.GohanStart", "M66.GuessStart", "M66.Edge" };

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Guess)) return false;

            // A parachute each, because this mission is a jump and nothing else will do. The
            // loan is opened and closed by MissionManager, so they go back at teardown.
            foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Gohan, CrewSlot.Guess })
            {
                var ped = Ctx.Crew.PedFor(slot);
                if (ped != null && ped.Exists()) ped.Weapons.Give(WeaponHash.Parachute, 1, false, true);
            }

            // Roads are baked terrain. The seed is a coordinate near the connector; the lane
            // is whatever the engine says is actually drivable near it.
            var seed = At("M66.Landing");
            if (GameUtils.NearestRoadNode(seed, LaneSearch, out var node, out float _))
            {
                _landing = node;
                Logger.Info(Id + ": the Del Perro connector resolved to a road node at " + node +
                    ", " + (int)node.DistanceTo(seed) + " m from the seed.");
            }
            else
            {
                _landing = seed;
                Ctx.Doctor?.Warn("placement", "M66.Landing",
                    "no road node within " + LaneSearch + " m; using the authored seed.");
                Logger.Warn(Id + ": no road node within " + LaneSearch + " m of " + seed +
                    "; using the seed as the landing zone. Resurvey M66.Landing on the connector.");
            }

            for (int i = 1; i <= Gunships; i++) SpawnGunship("M66.Patrol" + i);
            if (_gunships.Count == 0)
                Logger.Warn(Id + ": no gunship could be put over the roof; the jump is unopposed.");

            Establish("approach", "Off the top of it",
                "Aegis has the roof surrounded and is putting rockets into it. Three hundred and twenty meters down is the only way that is not through them.");
            return true;
        }

        /// <summary>
        /// One gunship on a circuit over the roof. Created in the air, so it goes through
        /// LaunchAirborne: an aircraft handed to the AI with its rotors stopped falls while
        /// they spin up.
        /// </summary>
        private void SpawnGunship(string key)
        {
            var model = new Model(GunshipModel);
            var pilotModel = new Model(PilotModel);
            if (!GameUtils.RequestModel(model) || !GameUtils.RequestModel(pilotModel)) return;
            var at = At(key);
            var heli = Track(World.CreateVehicle(model, at, Ctx.Locations.Heading(key)));
            if (heli == null || !heli.Exists()) { Logger.Warn(Id + ": a gunship could not be created at " + at + "."); return; }
            heli.IsPersistent = true;
            AircraftHold.LaunchAirborne(heli);

            var pilot = Track(World.CreatePed(pilotModel, at, 0f));
            model.MarkAsNoLongerNeeded();
            pilotModel.MarkAsNoLongerNeeded();
            if (pilot == null || !pilot.Exists()) { GameUtils.SafeDelete(heli); return; }

            // Seated outright, never asked to board: a queued warp is replaced by the mission
            // task below and the pilot is left loose in the air.
            pilot.SetIntoVehicle(heli, VehicleSeat.Driver);
            if (heli.GetPedOnSeat(VehicleSeat.Driver) != pilot)
            {
                Logger.Error(Id + ": a gunship pilot could not be seated; removing that aircraft.");
                GameUtils.SafeDelete(pilot); GameUtils.SafeDelete(heli);
                return;
            }
            pilot.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            pilot.IsPersistent = true;
            pilot.BlockPermanentEvents = true;
            pilot.Task.StartHeliMission(heli, MazeBank.Roof, VehicleMissionType.Circle,
                SweepSpeed, SweepRadius, SweepHeight, 40, 0f, 0f, HeliMissionFlags.None);
            Opposition.Add(pilot);
            Blips.Attach(pilot, BlipColor.Red, "Aegis QRF gunship");
            _gunships.Add(heli);
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            // Guess by name: M66_S1_01_GUESS is the man who calls the jump, and only the held
            // brother actually goes off the roof under canopy — the other two come back through
            // companion recovery, which is recorded in data/mission_gameplay.tsv.
            yield return new MissionStage("Get to the edge",
                new ReachZoneObjective("Guess: get to the roof edge, past the gunships", () => At("M66.Edge"), 6f))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(c => Fighting = true);

            // There is no spire. The jump is off the roof, and a brother two hundred and sixty
            // meters below it has unambiguously gone.
            yield return new MissionStage("Go off the tower",
                new ConditionObjective("Jump. Open the chute on the way down.", Airborne))
                .AnyBrother()
                .OnExit(c => _jumped = true)
                .AfterCues("M66_S1_01_GUESS", "M66_S1_02_ICE");

            yield return new MissionStage("Down on the connector",
                new TravelObjective("Steer to the Del Perro connector and get down", () => _landing, LandingRadius))
                .AnyBrother()
                .OnExit(c => Landed())
                .AfterCues("M66_S1_03_GOHAN");
        }

        /// <summary>Whether the held brother is off the tower and falling.</summary>
        private bool Airborne()
        {
            var ped = Ctx.Crew.PedFor(Ctx.Crew.ActiveSlot);
            return ped != null && ped.Exists() && !ped.IsDead && ped.Position.Z < MazeBank.Roof.Z - JumpedBelow;
        }

        private void Landed()
        {
            _down = true;
            Ctx.State?.SetCargo(ClearCargo, "M66.Landing");
            Logger.Info(Id + ": all three are down on the connector and off the tower.");
        }

        protected override void OnPassed()
        {
            if (!_jumped || !_down)
                throw new InvalidOperationException("They have to have gone off the tower and reached the connector.");
        }
    }
}
