using System;
using System.Collections.Generic;
using Bloodlines.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Crew
{
    /// <summary>Owns travel tasks only while a former player remains in the driver seat.</summary>
    public sealed class CompanionDriver
    {
        private sealed class Trip
        {
            public Ped Driver;
            public Vehicle Vehicle;
            public Vector3? Destination;
            public Vector3 Anchor;
            public int NextCheck;
            public bool Started, Arrived, Rendezvous, Urgent;
        }
        private readonly Dictionary<CrewSlot, Trip> _trips = new Dictionary<CrewSlot, Trip>();
        public Func<CrewSlot, Vehicle, Vector3?> MissionDestination { get; set; }
        public Func<Vehicle, Vector3?> FollowDestination { get; set; }
        public Func<Vehicle, bool> IsRendezvous { get; set; }
        public Func<Vehicle, bool> HasBoardingPassengers { get; set; }
        public void Arm(CrewSlot slot, Ped ped)
        {
            var vehicle = ped?.CurrentVehicle;
            if (ped == null || !ped.Exists() || ped.IsDead || vehicle == null || !vehicle.Exists() ||
                vehicle.GetPedOnSeat(VehicleSeat.Driver)?.Handle != ped.Handle) return;
            vehicle.IsPersistent = true;
            _trips[slot] = new Trip { Driver = ped, Vehicle = vehicle, Anchor = vehicle.Position };
        }
        public void Forget(CrewSlot slot) { _trips.Remove(slot); }
        public void Restart(CrewSlot slot) { if (_trips.TryGetValue(slot, out var trip)) { trip.Started = false; trip.NextCheck = 0; } }
        public void Clear() { _trips.Clear(); }
        public bool Owns(CrewSlot slot, Ped ped)
        {
            if (!_trips.TryGetValue(slot, out var trip)) return false;
            if (ped == null || !ped.Exists() || ped.IsDead || ped.Handle != trip.Driver.Handle ||
                !trip.Vehicle.Exists() || !trip.Vehicle.IsDriveable || !ped.IsInVehicle(trip.Vehicle) ||
                trip.Vehicle.GetPedOnSeat(VehicleSeat.Driver)?.Handle != ped.Handle)
            { Forget(slot); return false; }
            return true;
        }
        public void Update(CrewSlot slot, Ped ped)
        {
            if (!Owns(slot, ped)) return;
            var trip = _trips[slot];
            if (Game.GameTime < trip.NextCheck) return;
            trip.NextCheck = Game.GameTime + 750;
            if (HasBoardingPassengers != null && HasBoardingPassengers(trip.Vehicle))
            {
                Function.Call(Hash.TASK_VEHICLE_TEMP_ACTION, ped, trip.Vehicle, 27, 1500);
                trip.Started = false;
                return;
            }
            var active = Game.Player.Character;
            bool urgent = ped.IsInCombat || (active != null && active.IsInVehicle(trip.Vehicle) && (Game.Player.WantedLevel > 0 || active.IsInCombat));
            if (!urgent)
                foreach (var threat in World.GetNearbyPeds(ped, 90f))
                    if (threat != null && threat.Exists() && threat.IsAlive && threat.Handle != ped.Handle &&
                        (Function.Call<bool>(Hash.IS_PED_IN_COMBAT, threat, ped) ||
                         (active != null && active.IsInVehicle(trip.Vehicle) && Function.Call<bool>(Hash.IS_PED_IN_COMBAT, threat, active))))
                    { urgent = true; break; }
            bool urgencyChanged = urgent != trip.Urgent; trip.Urgent = urgent;
            trip.Rendezvous = IsRendezvous?.Invoke(trip.Vehicle) == true;
            var destination = trip.Rendezvous ? FollowDestination?.Invoke(trip.Vehicle) : MissionDestination?.Invoke(slot, trip.Vehicle);
            if (!destination.HasValue && !trip.Rendezvous)
            {
                var waypoint = World.WaypointBlip;
                if (waypoint != null && waypoint.Exists()) destination = waypoint.Position;
            }
            if (!destination.HasValue) destination = FollowDestination?.Invoke(trip.Vehicle);
            var model = trip.Vehicle.Model;
            if (trip.Urgent && (model.IsCar || model.IsBike) && (!destination.HasValue || GameUtils.IsWithinFlat(trip.Vehicle.Position, destination.Value, 20f)))
                destination = trip.Urgent && trip.Destination.HasValue && !GameUtils.IsWithinFlat(trip.Vehicle.Position, trip.Destination.Value, 30f) ? trip.Destination : (Vector3?)World.GetNextPositionOnStreet(trip.Vehicle.Position + trip.Vehicle.ForwardVector * 700f);
            bool changed = urgencyChanged || destination.HasValue != trip.Destination.HasValue ||
                (destination.HasValue && trip.Destination.HasValue && destination.Value.DistanceTo(trip.Destination.Value) > 6f);
            float arrivalRadius = model.IsPlane ? 175f : model.IsHelicopter ? 35f : 12f;
            bool arrived = destination.HasValue && GameUtils.IsWithinFlat(trip.Vehicle.Position, destination.Value, arrivalRadius);
            if (trip.Rendezvous && model.IsHelicopter && arrived && !trip.Vehicle.IsInAir && trip.Vehicle.HeightAboveGround < 3f)
            { trip.Arrived = true; trip.Started = true; return; }
            bool groundedAircraft = (model.IsPlane || model.IsHelicopter) && !trip.Vehicle.IsInAir && trip.Vehicle.HeightAboveGround < 5f;
            if (trip.Started && !changed && arrived == trip.Arrived && !groundedAircraft) return;
            if (!trip.Started || changed) trip.Anchor = trip.Vehicle.Position;
            trip.Destination = destination;
            trip.Arrived = arrived;
            Drive(trip);
            trip.Started = true;
        }
        private static Vector3? LandingSite(Vector3 target, Vehicle heli)
        {
            for (int i = 0; i < 8; i++)
            {
                var point = target + new Vector3((float)Math.Cos(i * Math.PI / 4) * 35f, (float)Math.Sin(i * Math.PI / 4) * 35f, 0f);
                var safe = World.GetSafeCoordForPed(point, false, 0);
                if (safe == Vector3.Zero || safe.DistanceTo(point) > 12f) continue;
                bool level = true;
                for (int j = 0; j < 4; j++)
                {
                    var edge = safe + new Vector3((float)Math.Cos(j * Math.PI / 2) * 12f, (float)Math.Sin(j * Math.PI / 2) * 12f, 0f);
                    var ground = World.GetSafeCoordForPed(edge, false, 0);
                    if (ground == Vector3.Zero || ground.DistanceTo(edge) > 3f || Math.Abs(ground.Z - safe.Z) > 1.5f) { level = false; break; }
                }
                if (!level || Function.Call<bool>(Hash.IS_POSITION_OCCUPIED, safe.X, safe.Y, safe.Z + 2f, 12f, false, true, true, false, false, heli, false)) continue;
                return safe;
            }
            return null; // The helicopter holds nearby when no open landing area is found.
        }
        private static void Drive(Trip trip)
        {
            var ped = trip.Driver; var vehicle = trip.Vehicle; var model = vehicle.Model;
            var target = trip.Destination ?? trip.Anchor;
            ped.AlwaysKeepTask = true;
            ped.BlockPermanentEvents = true;
            Function.Call(Hash.SET_DRIVER_ABILITY, ped, 1f);
            Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, ped, trip.Urgent ? .65f : .25f);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 2, false);
            if (model.IsTrain)
            {
                // Rail vehicles follow their track, never a road waypoint.
                Function.Call(Hash.SET_TRAIN_CRUISE_SPEED, vehicle, 12f);
            }
            else if (model.IsSubmarine)
            {
                // A map pin supplies no safe depth. Hold the current submerged position.
                Function.Call(Hash.TASK_SUBMARINE_GOTO_AND_STOP, 0, vehicle,
                    trip.Anchor.X, trip.Anchor.Y, trip.Anchor.Z, true);
            }
            else if (model.IsHelicopter || model.IsPlane)
            {
                if (trip.Rendezvous && model.IsHelicopter && trip.Destination.HasValue)
                {
                    var landing = LandingSite(target, vehicle);
                    if (landing.HasValue)
                    {
                        var point = landing.Value;
                        ped.Task.StartHeliMission(vehicle, point, (VehicleMissionType)19, 18f, 8f,
                            (int)point.Z + 20, 0, -1f, 60f, (HeliMissionFlags)(32 | 128 | 256));
                        return;
                    }
                }
                if (!vehicle.IsInAir && vehicle.HeightAboveGround < 5f)
                {
                    ped.Task.ClearAll();
                    Function.Call(Hash.TASK_VEHICLE_TEMP_ACTION, ped, vehicle, 27, 2000);
                    return; // Takeoff and landing require the player or a mission script.
                }
                float clearance = model.IsPlane ? 100f : 45f;
                target.Z = Math.Max(target.Z + clearance, vehicle.Position.Z);
                if (model.IsHelicopter)
                    ped.Task.StartHeliMission(vehicle, target, VehicleMissionType.GoTo, 22f, 25f,
                        (int)target.Z, (int)clearance, -1f, 80f, (HeliMissionFlags)(256 | 4096));
                else
                    ped.Task.StartPlaneMission(vehicle, target,
                        !trip.Destination.HasValue || trip.Arrived ? VehicleMissionType.Circle : VehicleMissionType.GoTo,
                        60f, 150f, (int)target.Z, (int)clearance, -1f, true);
            }
            else if (model.IsBoat)
            {
                ped.Task.StartBoatMission(vehicle, target,
                    trip.Destination.HasValue ? VehicleMissionType.GoTo : VehicleMissionType.Cruise,
                    10f, (VehicleDrivingFlags)786603, 12f, (BoatMissionFlags)7);
            }
            else if (trip.Destination.HasValue)
                ped.Task.DriveTo(vehicle, target, 8f, trip.Arrived && !trip.Urgent ? 6f : trip.Urgent ? 45f : 35f, DrivingStyle.AvoidTrafficExtremely);
            else
                ped.Task.CruiseWithVehicle(vehicle, trip.Urgent ? 45f : 30f, DrivingStyle.AvoidTrafficExtremely);
            Logger.Debug("Companion driver " + ped.Handle + (trip.Destination.HasValue ? " navigating to " + target : " continuing cautiously"));
        }
    }
}
