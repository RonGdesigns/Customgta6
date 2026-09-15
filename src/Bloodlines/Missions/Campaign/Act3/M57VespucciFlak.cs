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
    /// M57 — "Vespucci Flak". The Vespucci sand and the water off it, 18:30, sunset.
    ///
    /// Aegis gunships are sweeping the beach for extraction boats. Guess and Ice take a
    /// Seashark each and put three of them in the water.
    ///
    /// The Stinger pods are not a thing. A stock Seashark has no mounted weapon, and
    /// CAMPAIGN-REMAINDER says so outright: "a weaponized Seashark with a Stinger is not a
    /// stock guarantee. Use an armed boat until a mounted system is implemented and tested."
    /// So the skis are stock and the crew is issued a homing launcher each, fired from the
    /// seat. The authored lines call it a Stinger and lock onto a Maverick, and both of
    /// those survive intact — a homing launcher from a jet ski is the same beat, and it is
    /// one the game can actually do.
    ///
    /// Both riders are playable. Ice and Guess are on separate skis with the same job, so
    /// the stage names neither of them and the player spends the switch as he likes.
    ///
    /// The sand and the water are read from the archives: the beach clutter at Vespucci runs
    /// from x -1587 to -1361 at z 0.7 to 3.3, so the launch point is sand and the ski points
    /// west of it are surf. Their depth is still checked at runtime, because a water key that
    /// is dry is a mission that cannot start.
    /// </summary>
    public sealed class M57VespucciFlak : PreparationOperation
    {
        public const string SkiModel = "seashark";
        public const string GunshipModel = "maverick";
        public const string PilotModel = "s_m_y_blackops_01";
        public const int Gunships = 3;
        /// <summary>Rounds each rider gets. Three gunships, and misses are allowed.</summary>
        public const int Missiles = 8;
        /// <summary>How wide the gunships sweep, and how fast.</summary>
        public const float SweepRadius = 120f;
        public const float SweepSpeed = 28f;
        /// <summary>Their sweep altitude. Low, because they are looking for boats.</summary>
        public const int SweepHeight = 45;

        private readonly List<Vehicle> _gunships = new List<Vehicle>();
        private Vehicle _guessSki, _iceSki;

        public override string Id => "M57";
        public override string Title => "Vespucci Flak";
        protected override MissionEndpoint Endpoint => MissionEndpoint.EscapeCheckpoint;

        public IReadOnlyList<Vehicle> Gunship => _gunships;
        public Vehicle GuessSki => _guessSki;
        public Vehicle IceSki => _iceSki;

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Guess)) return false;

            // A water key that is dry is a mission that cannot start, so both are checked
            // before anything is put on them.
            if (!MissionSites.Water(Ctx.Locations, "M57.SkiGuess", "M57.SkiIce", "M57.Return"))
            {
                GameUtils.Notify("~r~The water off Vespucci did not check out. See Bloodlines.log.");
                return false;
            }

            _guessSki = Ski("M57.SkiGuess");
            _iceSki = Ski("M57.SkiIce");
            if (!RequireAssets(_guessSki, _iceSki)) return false;
            RequireAsset(_guessSki, "Guess's ski was destroyed.");
            RequireAsset(_iceSki, "Ice's ski was destroyed.");

            for (int i = 1; i <= Gunships; i++) SpawnGunship("M57.Patrol" + i);
            if (_gunships.Count == 0)
            {
                Logger.Error(Id + ": no gunship could be put in the air; there is nothing to shoot down.");
                return false;
            }

            // A launcher each, from the seat. The mission loan is opened and closed by
            // MissionManager, so these go back to the arsenal baseline at teardown.
            foreach (var slot in new[] { CrewSlot.Guess, CrewSlot.Ice })
            {
                var ped = Ctx.Crew.PedFor(slot);
                if (ped != null && ped.Exists()) ped.Weapons.Give(WeaponHash.HomingLauncher, Missiles, false, true);
            }

            Establish("approach", "Three of them, low over the surf",
                "The gunships are working the beach for extraction boats. Two skis on the water and a launcher each; Gohan keeps the sand and calls the lines they fly.",
                _guessSki, _iceSki);
            return true;
        }

        private Vehicle Ski(string key)
        {
            var point = MarineSites.ResolveOrThrow(Ctx.Locations, key, 1.2f);
            var ski = Car(SkiModel, point, Ctx.Locations.Heading(key), false);
            if (ski != null) ski.IsPersistent = true;
            return ski;
        }

        /// <summary>
        /// One gunship on a sweep line. Created in the air, so it goes through
        /// LaunchAirborne: an aircraft handed to the AI with its rotors stopped falls while
        /// they spin up, which is what put M45's helicopter in the sea twice.
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

            // Seated, never asked to board: a queued warp is replaced by the mission task
            // below and the pilot is left loose in the air. M26 and M27 both lost aircraft
            // that way.
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
            pilot.Task.StartHeliMission(heli, at, VehicleMissionType.Circle,
                SweepSpeed, SweepRadius, SweepHeight, 20, 0f, 0f, HeliMissionFlags.None);
            Opposition.Add(pilot);
            Blips.Attach(pilot, BlipColor.Red, "Aegis gunship");
            _gunships.Add(heli);
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Get on the water",
                new EnterVehicleObjective("Guess: take your ski out past the surf break", () => _guessSki, VehicleSeat.Driver)
                { RequiredCharacter = CrewSlot.Guess },
                new EnterVehicleObjective("Ice: take the other ski out", () => _iceSki, VehicleSeat.Driver)
                { RequiredCharacter = CrewSlot.Ice })
                .AfterCues("M57_S1_01_GUESS");

            // One stage, three targets, neither rider named: the job is the same for both and
            // the player switches whenever he likes. Each gunship carries the scaling marker
            // and the tracking waypoint, because an aircraft at 45 m over open water is
            // otherwise a dot you lose the moment you turn.
            var kills = new List<Objective>();
            for (int i = 0; i < Gunships; i++)
            {
                int index = i;
                kills.Add(new DestroyVehicleObjective("Shoot down the Aegis gunships — " + Gunships + " over the beach",
                    () => index < _gunships.Count ? _gunships[index] : null).ByAnyone());
            }
            yield return new MissionStage("Put them in the water", kills.ToArray())
                .AnyOf()
                .AfterCues("M57_S1_02_ICE");

            yield return new MissionStage("Back to the sand",
                new TravelObjective("Bring the skis back in to the beach", () => At("M57.Return"), 20f, () => _guessSki))
                .AnyOf()
                .AfterCues("M57_S1_03_GUESS");
        }

        protected override void OnPassed()
        {
            if (_gunships.Any(g => g != null && g.Exists() && g.IsDriveable))
                throw new InvalidOperationException("All three gunships have to be down.");
            Release(_guessSki);
            Release(_iceSki);
        }
    }
}
