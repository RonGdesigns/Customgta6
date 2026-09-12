using System;
using System.Collections.Generic;
using Bloodlines.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Crew
{
    /// <summary>Local free-roam trips, stops and occasional witnessed vehicle thefts.</summary>
    public sealed class CompanionLife
    {
        private sealed class Day
        {
            public Vector3 Destination;
            public Vehicle Ride, PoliceCar;
            public Ped Officer;
            public int NextTick, StopUntil, NextCrime, EnteredAt, LastProgress, CoolSince;
            public Vector3 LastPosition;
            public bool Started, Traveling, Entering, Theft, Activity;
        }
        private readonly Dictionary<CrewSlot, Day> _days = new Dictionary<CrewSlot, Day>();
        private readonly Random _random = new Random();
        public PersonalWanted Wanted { get; } = new PersonalWanted();
        public Func<Ped, bool> IsCrewMember { get; set; }
        public Func<CrewSlot, Vector3?> HomeDestination { get; set; }
        public bool CrimesEnabled { get; set; } = true;

        public void PlayerTookControl(CrewSlot slot)
        {
            if (_days.TryGetValue(slot, out var day))
            {
                // The incoming player keeps the vehicle and any pursuer. A fresh trip
                // is chosen only when this character becomes an NPC again.
                day.Started = false; day.Entering = false; day.NextTick = 0;
            }
        }
        /// <summary>A brother riding with the player shares the player's heat exactly and has no pursuit of his own while he rides.</summary>
        public void RideAlong(CrewSlot slot, int level)
        {
            Wanted.Set(slot, level);
            if (_days.TryGetValue(slot, out var day)) ReleasePolice(day);
        }
        public void Suspend(CrewSlot slot)
        {
            if (!_days.TryGetValue(slot, out var day)) return;
            ReleasePolice(day); ReleaseRide(day); _days.Remove(slot);
        }
        public void Clear(bool clearHeat = false)
        {
            foreach (var slot in new List<CrewSlot>(_days.Keys)) Suspend(slot);
            if (clearHeat) Wanted.Clear();
        }
        public void Update(CrewSlot slot, Ped ped)
        {
            if (ped == null || !ped.Exists() || ped.IsDead) return;
            if (!_days.TryGetValue(slot, out var day))
                _days[slot] = day = new Day { NextCrime = Game.GameTime + _random.Next(180000, 420000), LastPosition = ped.Position };
            if (Game.GameTime < day.NextTick) return;
            day.NextTick = Game.GameTime + 1000;
            UpdateHeat(slot, ped, day);
            if (day.Entering)
            {
                if (day.Ride != null && day.Ride.Exists() && ped.IsInVehicle(day.Ride))
                {
                    day.Entering = false;
                    if (day.Theft) { Wanted.Set(slot, Math.Max(2, Wanted.Get(slot))); day.Theft = false; SpawnPolice(ped, day); }
                    Drive(slot, ped, day); return;
                }
                if (Game.GameTime - day.EnteredAt < 20000) return;
                ReleaseRide(day); day.Entering = false; day.Theft = false;
                ped.Task.GoTo(day.Destination); day.LastProgress = Game.GameTime;
            }
            if (!day.Started)
            {
                ChooseTrip(slot, ped, day); return;
            }
            if (!day.Traveling)
            {
                if (!day.Activity && !ped.IsInVehicle())
                {
                    ped.Task.StartScenario(slot == CrewSlot.Gohan ? "WORLD_HUMAN_CLIPBOARD" : "WORLD_HUMAN_STAND_MOBILE", ped.Position, ped.Heading);
                    day.Activity = true;
                }
                if (Game.GameTime >= day.StopUntil) ChooseTrip(slot, ped, day);
                return;
            }
            // Air and water craft retain their seats and use their appropriate autopilot.
            var vehicle = ped.CurrentVehicle;
            if (vehicle != null && vehicle.Exists() && !(vehicle.Model.IsCar || vehicle.Model.IsBike)) return;
            if (GameUtils.IsWithinFlat(ped.Position, day.Destination, 18f))
            {
                day.Traveling = false; day.Activity = false; day.StopUntil = Game.GameTime + _random.Next(30000, 75000);
                if (vehicle != null && vehicle.Exists()) ped.Task.LeaveVehicle();
                else ped.Task.WanderAround(day.Destination, 12f);
                day.NextTick = Game.GameTime + 5000;
                return;
            }
            if (ped.Position.DistanceTo(day.LastPosition) > 8f)
            { day.LastPosition = ped.Position; day.LastProgress = Game.GameTime; }
            if (Game.GameTime - day.LastProgress > 30000)
            {
                // A blocked door or street gets a new route. Never freeze in a failed task.
                day.Started = false; day.LastProgress = Game.GameTime;
            }
        }
        private void ChooseTrip(CrewSlot slot, Ped ped, Day day)
        {
            ped.Task.ClearAll(); ped.AlwaysKeepTask = true; ped.BlockPermanentEvents = true;
            double angle = _random.NextDouble() * Math.PI * 2;
            bool driving = ped.IsInVehicle() && ped.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver)?.Handle == ped.Handle;
            float distance = driving ? _random.Next(450, 1000) : _random.Next(160, 300);
            var point = ped.Position + new Vector3((float)Math.Cos(angle) * distance, (float)Math.Sin(angle) * distance, 0f);
            var home = HomeDestination?.Invoke(slot);
            if (Wanted.Get(slot) == 0 && home.HasValue && _random.Next(3) == 0 &&
                home.Value.DistanceTo(ped.Position) > 100f && home.Value.DistanceTo(ped.Position) < 4000f)
                point = home.Value;
            var road = World.GetNextPositionOnStreet(point);
            if (road == Vector3.Zero || road.DistanceTo(point) > 250f)
            { ped.Task.WanderAround(ped.Position, 120f); day.NextTick = Game.GameTime + 15000; return; }
            day.Destination = road; day.Started = day.Traveling = true; day.LastPosition = ped.Position; day.LastProgress = Game.GameTime;
            if (driving) { day.Ride = ped.CurrentVehicle; Drive(slot, ped, day); return; }
            // Passengers can carry on with their existing driver until it is safe to leave.
            if (ped.IsInVehicle())
            {
                if (ped.CurrentVehicle.Speed < 2f) ped.Task.LeaveVehicle();
                day.Started = false; day.NextTick = Game.GameTime + 5000; return;
            }
            bool theft = CrimesEnabled && Game.GameTime >= day.NextCrime && Wanted.Get(slot) == 0;
            foreach (var car in World.GetNearbyVehicles(ped.Position, 80f))
            {
                if (!Eligible(car, theft)) continue;
                ReleaseRide(day); day.Ride = car; car.IsPersistent = true;
                day.Entering = true; day.Theft = theft; day.EnteredAt = Game.GameTime;
                if (theft) day.NextCrime = Game.GameTime + _random.Next(360000, 720000);
                ped.Task.EnterVehicle(car, VehicleSeat.Driver, 20000, 1.5f, EnterVehicleFlags.None);
                return;
            }
            // Walking is a real trip too: companions leave immediately without waiting
            // for the player to provide a vehicle or travel a leash distance away.
            var safe = World.GetSafeCoordForPed(road, false, 0);
            if (safe != Vector3.Zero) day.Destination = safe;
            ped.Task.GoTo(day.Destination);
        }
        private bool Eligible(Vehicle car, bool theft)
        {
            if (car == null || !car.Exists() || !car.IsDriveable || car.IsPersistent || !(car.Model.IsCar || car.Model.IsBike) || car.Speed > 2f) return false;
            if (Game.Player.Character.IsInVehicle(car) || Game.Player.LastVehicle?.Handle == car.Handle) return false;
            for (int seat = -1; seat < car.PassengerCapacity; seat++)
            {
                var rider = car.GetPedOnSeat((VehicleSeat)seat);
                if (rider != null && rider.Exists() && (IsCrewMember?.Invoke(rider) == true || !theft)) return false;
            }
            return true;
        }
        private void Drive(CrewSlot slot, Ped ped, Day day)
        {
            if (day.Ride == null || !day.Ride.Exists() || !day.Ride.IsDriveable) { day.Started = false; return; }
            day.Ride.IsEngineRunning = true;
            CrewDriving.Configure(ped, slot, false);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 2, false);
            ped.Task.DriveTo(day.Ride, day.Destination, 10f, CrewDriving.Speed(slot, false),
                (DrivingStyle)CrewDriving.TrafficFlags);
            day.LastProgress = Game.GameTime;
        }
        private void SpawnPolice(Ped target, Day day)
        {
            if (day.Officer != null && day.Officer.Exists()) return;
            var point = World.GetNextPositionOnStreet(target.Position - target.ForwardVector * 100f);
            if (point == Vector3.Zero || point.DistanceTo(target.Position) < 65f || point.DistanceTo(target.Position) > 180f ||
                Function.Call<bool>(Hash.IS_SPHERE_VISIBLE, point.X, point.Y, point.Z, 10f)) return;
            var carModel = new Model("police"); var copModel = new Model("s_m_y_cop_01");
            if (!GameUtils.RequestModel(carModel, 500) || !GameUtils.RequestModel(copModel, 500)) return;
            try
            {
                day.PoliceCar = World.CreateVehicle(carModel, point, target.Heading);
                if (day.PoliceCar == null || !day.PoliceCar.Exists()) return;
                day.PoliceCar.IsPersistent = true; day.PoliceCar.IsSirenActive = true;
                day.Officer = World.CreatePed(copModel, point, target.Heading);
                if (day.Officer == null || !day.Officer.Exists()) { ReleasePolice(day); return; }
                day.Officer.IsPersistent = true; day.Officer.BlockPermanentEvents = true;
                day.Officer.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_LIFE_POLICE");
                MilitaryResponse.AllyWithPolice(day.Officer.RelationshipGroup);
                Function.Call(Hash.SET_CAN_ATTACK_FRIENDLY, day.Officer, false, false);
                day.Officer.SetIntoVehicle(day.PoliceCar, VehicleSeat.Driver);
                day.Officer.Task.VehicleChase(target);
            }
            finally { carModel.MarkAsNoLongerNeeded(); copModel.MarkAsNoLongerNeeded(); }
        }
        private void UpdateHeat(CrewSlot slot, Ped ped, Day day)
        {
            if (Wanted.Get(slot) == 0) { ReleasePolice(day); return; }
            bool pursuing = day.Officer != null && day.Officer.Exists() && day.Officer.IsAlive && day.Officer.Position.DistanceTo(ped.Position) < 220f;
            if (pursuing) { day.CoolSince = 0; return; }
            if (day.CoolSince == 0) day.CoolSince = Game.GameTime;
            if (Game.GameTime - day.CoolSince < 90000) { SpawnPolice(ped, day); return; }
            Wanted.Set(slot, Math.Max(0, Wanted.Get(slot) - 1)); day.CoolSince = Game.GameTime;
            ReleasePolice(day);
        }
        private static void ReleaseRide(Day day)
        { if (day.Ride != null && day.Ride.Exists()) GameUtils.SafeRelease(day.Ride); day.Ride = null; }
        private static void ReleasePolice(Day day)
        {
            if (day.Officer != null && day.Officer.Exists()) { day.Officer.Task.ClearAll(); GameUtils.SafeRelease(day.Officer); }
            if (day.PoliceCar != null && day.PoliceCar.Exists()) { day.PoliceCar.IsSirenActive = false; GameUtils.SafeRelease(day.PoliceCar); }
            day.Officer = null; day.PoliceCar = null;
        }
    }
}
