using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using GTA;
using GTA.Native;

namespace Bloodlines.Crew
{
    /// <summary>Tunes real responding actors, preserving the engine's search and arrest tasks.</summary>
    public sealed class TacticalResponse
    {
        private readonly Dictionary<int, Ped> _tuned = new Dictionary<int, Ped>();
        private readonly Dictionary<int, Vehicle> _wantedCars = new Dictionary<int, Vehicle>();
        private int _nextScan, _tier = -1, _nextSighting, _lastSightingLog;
        /// <summary>How far a cop can be and still count as having seen the player.</summary>
        public const float SightingRange = 70f;
        public const float AircraftSightingRange = 240f;
        public const float AircraftHorizontalRange = 170f;
        public string LastSightingDiagnostic { get; private set; } = "No sighting scan yet.";
        private static readonly int[] Dispatch = { 1, 2, 4, 6, 8, 13, 14 };
        public static float DispatchInterval(int stars) => stars <= 0 ? 1f : Math.Max(.52f, 1.12f - Math.Min(5, stars) * .12f);
        public void Update(CrewRoster crew)
        {
            if (!crew.IsDeployed) { if (_tier != -1) Reset(); return; }
            int stars = Game.Player.WantedLevel;
            if (_tier != stars)
            {
                _tier = stars;
                foreach (int service in Dispatch)
                    Function.Call(Hash.SET_DISPATCH_TIME_BETWEEN_SPAWN_ATTEMPTS_MULTIPLIER, service, DispatchInterval(stars));
            }
            var player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) return;
            var car = player.CurrentVehicle;
            if (stars > 0 && car != null && car.Exists())
            {
                // Flag the newly occupied car, without reporting a sighting or
                // resetting the hidden search timer. Escaping on foot still works.
                if (!_wantedCars.ContainsKey(car.Handle) && !car.IsWanted)
                    _wantedCars[car.Handle] = car;
                Function.Call(Hash.SET_VEHICLE_IS_WANTED, car, true);
            }
            if (stars == 0) ClearWantedCars();
            // Smarter, not unlosable: use a separate 3D aerial range for police
            // helicopter crews. A distant ground cop does not gain the aerial range.
            // Break LOS and the engine's hidden search timer is left alone.
            if (stars > 0 && Game.GameTime >= _nextSighting)
            {
                _nextSighting = Game.GameTime + 1500;
                // A fresh scripted wanted level can be unseen without yet using the
                // gray-star presentation. Cover both states, not just one HUD flag.
                bool searching = Function.Call<bool>(Hash.ARE_PLAYER_STARS_GREYED_OUT, Game.Player) ||
                    Function.Call<bool>(Hash.ARE_PLAYER_FLASHING_STARS_ABOUT_TO_DROP, Game.Player) ||
                    !Function.Call<bool>(Hash.IS_WANTED_AND_HAS_BEEN_SEEN_BY_COPS, Game.Player);
                if (searching) ReportSightings(player, crew);
            }
            if (Game.GameTime < _nextScan) return;
            _nextScan = Game.GameTime + 1000;
            foreach (var key in _tuned.Where(p => !p.Value.Exists() || p.Value.IsDead).Select(p => p.Key).ToArray()) _tuned.Remove(key);
            foreach (var key in _wantedCars.Where(p => !p.Value.Exists()).Select(p => p.Key).ToArray()) _wantedCars.Remove(key);
            foreach (var ped in World.GetNearbyPeds(player, 180f))
            {
                if (ped == null || !ped.Exists() || ped.IsDead || ped == player ||
                    ped.RelationshipGroup == crew.CrewGroup || _tuned.ContainsKey(ped.Handle) || _tuned.Count >= 160) continue;
                bool cop = Function.Call<int>(Hash.GET_PED_TYPE, ped) == 6;
                var target = Function.Call<Ped>(Hash.GET_PED_TARGET_FROM_COMBAT_PED, ped, 0);
                bool fightingCrew = target != null && target.Exists() && (target == player || target.RelationshipGroup == crew.CrewGroup);
                if (!(cop && stars > 0) && !fightingCrew) continue;
                // No target assignment here: neutral NPCs, scripted escape actors,
                // police searches and one-star arrests retain their own tasks.
                foreach (int attribute in new[] { 0, 4, 12, 21, 28, 34, 42, 43, 44 })
                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, attribute, true);
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 27, false); // no perfect aim
                Function.Call(Hash.SET_PED_COMBAT_MOVEMENT, ped, ped.Handle % 3 == 0 ? 2 : 1);
                Function.Call(Hash.SET_PED_COMBAT_RANGE, ped, ped.Handle % 3 == 0 ? 0 : 1);
                if (cop)
                {
                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 41, true);
                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 64, false); // retain blocking maneuvers
                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 65, false); // retain PIT attempts
                    Function.Call(Hash.SET_DRIVER_ABILITY, ped, 1f);
                    Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, ped, .5f + Math.Min(stars,5) * .06f);
                    var patrol = ped.CurrentVehicle;
                    if (patrol != null && patrol.Exists() && patrol.Model.IsCar && ped.SeatIndex == VehicleSeat.Driver)
                    {
                        Function.Call(Hash.SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG, ped, 8, true); // moderate boxing/PIT
                        Function.Call(Hash.SET_TASK_VEHICLE_CHASE_IDEAL_PURSUIT_DISTANCE, ped, stars >= 4 ? 14f : 22f);
                    }
                }
                _tuned[ped.Handle] = ped;
            }
        }
        private void ReportSightings(Ped player, CrewRoster crew)
        {
            LastSightingDiagnostic = "No eligible police observer had a clear sight line.";
            foreach (var ped in World.GetNearbyPeds(player, AircraftSightingRange))
            {
                if (ped == null || !ped.Exists() || ped.IsDead || ped == player || ped.RelationshipGroup == crew.CrewGroup) continue;
                // Scripted helicopter crews can use a pilot model (not PED_TYPE_COP)
                // while legitimately belonging to COP. A civilian in a police-model
                // helicopter alone does not qualify as a law-enforcement observer.
                if (Function.Call<int>(Hash.GET_PED_TYPE, ped) != 6 &&
                    Function.Call<int>(Hash.GET_PED_RELATIONSHIP_GROUP_HASH, ped) != Game.GenerateHash("COP")) continue;
                var vehicle = ped.CurrentVehicle;
                bool air = vehicle != null && vehicle.Exists() && !vehicle.IsDead && vehicle.Model.IsHelicopter;
                float distance = ped.Position.DistanceTo(player.Position);
                if (!air && distance > SightingRange) continue;
                if (air && (distance > AircraftSightingRange ||
                    !GameUtils.IsWithinFlat(ped.Position, player.Position, AircraftHorizontalRange) ||
                    ped.Position.Z < player.Position.Z - 15f)) continue;
                // Real geometry remains authoritative. The larger aerial search is
                // not permission to see through a roof, cliff, tunnel or pier.
                if (!Function.Call<bool>(Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY, ped, player, 17))
                { LastSightingDiagnostic = (air ? "Air" : "Ground") + " unit: sight line obstructed."; continue; }
                LastSightingDiagnostic = (air ? "Air" : "Ground") + " unit reported a clear sighting at " + distance.ToString("0") + " m.";
                var at = player.Position;
                Function.Call(Hash.REPORT_POLICE_SPOTTED_PLAYER, Game.Player);
                Function.Call(Hash.SET_PLAYER_WANTED_CENTRE_POSITION, Game.Player, at.X, at.Y, at.Z);
                if (Game.GameTime - _lastSightingLog > 10000) { _lastSightingLog = Game.GameTime; Logger.Info("Police sighting: " + LastSightingDiagnostic); }
                return;
            }
        }
        private void ClearWantedCars()
        {
            foreach (var car in _wantedCars.Values)
                if (car.Exists()) Function.Call(Hash.SET_VEHICLE_IS_WANTED, car, false);
            _wantedCars.Clear();
        }
        public void Reset()
        {
            foreach (int service in Dispatch) Function.Call(Hash.SET_DISPATCH_TIME_BETWEEN_SPAWN_ATTEMPTS_MULTIPLIER, service, 1f);
            ClearWantedCars(); _tuned.Clear(); _tier = -1; _nextScan = _nextSighting = _lastSightingLog = 0;
            LastSightingDiagnostic = "Sighting service reset.";
        }
    }
}
