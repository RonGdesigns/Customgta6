using System;
using System.Collections.Generic;
using Bloodlines.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Crew
{
    /// <summary>Acquire separate road transport and recover it only outside the camera.</summary>
    public sealed class CompanionConvoy
    {
        private sealed class Ride { public Vehicle Vehicle; public int NextAttempt, EntryStarted; }
        private readonly Dictionary<CrewSlot, Ride> _rides = new Dictionary<CrewSlot, Ride>();
        public Func<CrewSlot, bool> AllowCatchupTeleport { get; set; }
        public Func<Ped, bool> IsCrewMember { get; set; }
        public bool HasRide(CrewSlot slot) => _rides.ContainsKey(slot);
        public void Forget(CrewSlot slot)
        {
            if (_rides.TryGetValue(slot, out var ride) && ride.Vehicle != null && ride.Vehicle.Exists()) GameUtils.SafeRelease(ride.Vehicle);
            _rides.Remove(slot);
        }
        public void Clear() { foreach (var slot in new List<CrewSlot>(_rides.Keys)) Forget(slot); }
        public void Update(CrewSlot slot, Ped ped, Ped leader, CompanionDriver driver)
        {
            if (leader == null || !leader.Exists() || ped == null || !ped.Exists() || ped.IsDead) return;
            if (!_rides.TryGetValue(slot, out var ride)) _rides[slot] = ride = new Ride();
            if (Game.GameTime < ride.NextAttempt) return;
            ride.NextAttempt = Game.GameTime + 1000;
            float distance = ped.Position.DistanceTo(leader.Position);
            if (AllowCatchupTeleport?.Invoke(slot) != false && distance > 500f && !ped.IsOnScreen && (ride.Vehicle == null || !ride.Vehicle.Exists() || !ride.Vehicle.IsOnScreen))
            {
                var behind = World.GetNextPositionOnStreet(leader.Position - leader.ForwardVector * 100f);
                float separation = behind.DistanceTo(leader.Position);
                if (behind != Vector3.Zero && separation >= 65f && separation <= 160f &&
                    !Function.Call<bool>(Hash.IS_SPHERE_VISIBLE, behind.X, behind.Y, behind.Z, 12f))
                {
                    Function.Call(Hash.REQUEST_COLLISION_AT_COORD, behind.X, behind.Y, behind.Z);
                    var model = new Model("primo");
                    if (GameUtils.RequestModel(model, 500))
                    {
                        var car = World.CreateVehicle(model, behind, leader.Heading);
                        model.MarkAsNoLongerNeeded();
                        if (car != null && car.Exists())
                        {
                            // Validate collision before transferring anyone into the replacement.
                            if (!Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY, car)) { GameUtils.SafeDelete(car); return; }
                            if (ride.Vehicle != null && ride.Vehicle.Exists()) GameUtils.SafeRelease(ride.Vehicle);
                            ride.Vehicle = car; car.IsPersistent = true; car.IsEngineRunning = true;
                            ped.SetIntoVehicle(car, VehicleSeat.Driver);
                            driver.Arm(slot, ped);
                            Logger.Debug(Protagonist.Of(slot).Handle + " caught up in a vehicle outside the camera.");
                            return;
                        }
                    }
                }
            }
            if (ride.Vehicle != null && ride.Vehicle.Exists() && ride.Vehicle.IsDriveable)
            {
                if (ped.IsInVehicle(ride.Vehicle) && ride.Vehicle.GetPedOnSeat(VehicleSeat.Driver)?.Handle == ped.Handle)
                {
                    if (!driver.Owns(slot, ped)) driver.Arm(slot, ped);
                    driver.Update(slot, ped);
                    return;
                }
                if (Game.GameTime - ride.EntryStarted < 20000) return;
                GameUtils.SafeRelease(ride.Vehicle); ride.Vehicle = null;
            }
            foreach (var vehicle in World.GetNearbyVehicles(ped.Position, 90f))
            {
                if (vehicle == null || !vehicle.Exists() || !vehicle.IsDriveable || vehicle.IsPersistent ||
                    !(vehicle.Model.IsCar || vehicle.Model.IsBike) || vehicle.Model.IsHelicopter || vehicle.Model.IsPlane) continue;
                if (Game.Player.Character.IsInVehicle(vehicle) || Game.Player.LastVehicle?.Handle == vehicle.Handle) continue;
                bool crewAboard = false;
                int capacity = Function.Call<int>(Hash.GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS, vehicle);
                for (int seat = -1; seat < capacity; seat++)
                {
                    var rider = vehicle.GetPedOnSeat((VehicleSeat)seat);
                    if (rider != null && IsCrewMember != null && IsCrewMember(rider)) { crewAboard = true; break; }
                }
                if (crewAboard) continue;
                var occupant = vehicle.GetPedOnSeat(VehicleSeat.Driver);
                if (occupant != null && (occupant.Handle == Game.Player.Character.Handle || (IsCrewMember != null && IsCrewMember(occupant)))) continue;
                ride.Vehicle = vehicle; vehicle.IsPersistent = true; ride.EntryStarted = Game.GameTime;
                ped.Task.EnterVehicle(vehicle, VehicleSeat.Driver, 20000, 2f, EnterVehicleFlags.None);
                Logger.Debug(Protagonist.Of(slot).Handle + " taking nearby transport to catch up.");
                return;
            }
            // Keep moving if there is no car nearby; no visible teleport into the player's car.
            ped.Task.FollowToOffsetFromEntity(leader, new Vector3(0f,-5f,0f), 3f, -1, 8f, true);
            ride.NextAttempt = Game.GameTime + 5000;
        }
    }
}
