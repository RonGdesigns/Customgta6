using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;

namespace Bloodlines.Crew
{
    /// <summary>
    /// What the player can tell a brother to do in free roam. A standing order lives in
    /// <see cref="CompanionController"/> until it is satisfied, replaced or overridden by a
    /// mission; the three at the end are one-shots that change the hangout and clear.
    /// </summary>
    public enum CrewOrder
    {
        None,
        /// <summary>Come with me: hang out, follow on foot, share the car.</summary>
        FollowMe,
        /// <summary>Stay where you are, and fight from there.</summary>
        HoldHere,
        /// <summary>Go do your own thing: the independent free-roam life.</summary>
        OwnThing,
        /// <summary>Get into the vehicle, any seat that is free.</summary>
        GetIn,
        /// <summary>Get out of it once it is safe to, then hold there.</summary>
        GetOut,
        /// <summary>Take the driver's seat and keep it, whatever the player does next.</summary>
        TakeTheWheel,
        /// <summary>Take the turret.</summary>
        ManTheGun,
        /// <summary>Take the wheel and drive to the map waypoint.</summary>
        DriveToWaypoint,
        /// <summary>Stop the vehicle and stay at the wheel.</summary>
        PullOver,
    }

    /// <summary>
    /// The order vocabulary: what each order is called, what a brother says back, which
    /// orders make sense right now. No state lives here; the controller owns the orders.
    /// </summary>
    public static class CrewOrders
    {
        /// <summary>A vehicle the player has just stepped out of still counts as "the truck".</summary>
        public const float SubjectMeters = 18f;

        public static string Label(CrewOrder order)
        {
            switch (order)
            {
                case CrewOrder.FollowMe: return "Follow me";
                case CrewOrder.HoldHere: return "Hold here";
                case CrewOrder.OwnThing: return "Do your own thing";
                case CrewOrder.GetIn: return "Get in";
                case CrewOrder.GetOut: return "Get out";
                case CrewOrder.TakeTheWheel: return "Take the wheel";
                case CrewOrder.ManTheGun: return "Man the gun";
                case CrewOrder.DriveToWaypoint: return "Drive to my waypoint";
                case CrewOrder.PullOver: return "Pull over";
                default: return "";
            }
        }

        /// <summary>What the HUD echoes once the order has landed.</summary>
        public static string Echo(CrewOrder order, string handle)
        {
            switch (order)
            {
                case CrewOrder.FollowMe: return handle + ": on you";
                case CrewOrder.HoldHere: return handle + ": holding here";
                case CrewOrder.OwnThing: return handle + ": heading out";
                case CrewOrder.GetIn: return handle + ": getting in";
                case CrewOrder.GetOut: return handle + ": getting out";
                case CrewOrder.TakeTheWheel: return handle + ": taking the wheel";
                case CrewOrder.ManTheGun: return handle + ": on the gun";
                case CrewOrder.DriveToWaypoint: return handle + ": driving to the waypoint";
                case CrewOrder.PullOver: return handle + ": pulling over";
                default: return handle;
            }
        }

        /// <summary>An order about a vehicle: it names one and holds until it is satisfied.</summary>
        public static bool AboutVehicle(CrewOrder order) =>
            order == CrewOrder.GetIn || order == CrewOrder.GetOut || order == CrewOrder.TakeTheWheel ||
            order == CrewOrder.ManTheGun || order == CrewOrder.DriveToWaypoint || order == CrewOrder.PullOver;

        /// <summary>An order that puts him at the wheel and keeps him there.</summary>
        public static bool HoldsWheel(CrewOrder order) =>
            order == CrewOrder.TakeTheWheel || order == CrewOrder.DriveToWaypoint || order == CrewOrder.PullOver;

        public static bool IsTurretSeat(Vehicle vehicle, VehicleSeat seat)
        {
            if (vehicle == null || !vehicle.Exists() || (int)seat < 0) return false;
            try { return Function.Call<bool>(Hash.IS_TURRET_SEAT, vehicle, (int)seat); }
            catch { return false; }
        }

        /// <summary>The first free turret seat, or None when the vehicle has no gun or it is taken.</summary>
        public static VehicleSeat FreeTurretSeat(Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists()) return VehicleSeat.None;
            int capacity = Function.Call<int>(Hash.GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS, vehicle);
            for (int i = 0; i < capacity; i++)
            {
                var seat = (VehicleSeat)i;
                if (vehicle.IsSeatFree(seat) && IsTurretSeat(vehicle, seat)) return seat;
            }
            return VehicleSeat.None;
        }

        public static bool AnySeatFree(Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists()) return false;
            if (vehicle.IsSeatFree(VehicleSeat.Driver)) return true;
            int capacity = Function.Call<int>(Hash.GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS, vehicle);
            for (int i = 0; i < capacity; i++) if (vehicle.IsSeatFree((VehicleSeat)i)) return true;
            return false;
        }

        /// <summary>
        /// The vehicle an order is about: the one the player is in, or the one he just got
        /// out of and is still standing beside. Ron's case is exactly the second one - out of
        /// the driver's seat, walking around to the gun bed, and the truck is still the truck.
        /// </summary>
        public static Vehicle Subject(Ped leader)
        {
            if (leader == null || !leader.Exists()) return null;
            var current = leader.CurrentVehicle;
            if (current != null && current.Exists()) return current;
            var last = Game.Player.LastVehicle;
            if (last != null && last.Exists() && last.IsDriveable && last.Position.DistanceTo(leader.Position) <= SubjectMeters) return last;
            return null;
        }

        /// <summary>
        /// The orders that make sense for this brother right now, in the order the strip
        /// shows them. Vehicle orders come first because that is where the strip is opened
        /// in a hurry; the three that are always true come last.
        /// </summary>
        public static List<CrewOrder> Available(Ped companion, Ped leader, Vehicle subject, bool hasWaypoint)
        {
            var list = new List<CrewOrder>();
            if (companion == null || !companion.Exists() || companion.IsDead) return list;
            if (subject != null && subject.Exists())
            {
                bool aboard = companion.IsInVehicle(subject);
                bool driving = aboard && subject.GetPedOnSeat(VehicleSeat.Driver)?.Handle == companion.Handle;
                bool leaderDrives = leader != null && leader.Exists() && subject.GetPedOnSeat(VehicleSeat.Driver)?.Handle == leader.Handle;
                bool wheelOpen = !leaderDrives && subject.IsSeatFree(VehicleSeat.Driver);
                if (!driving && wheelOpen) list.Add(CrewOrder.TakeTheWheel);
                if (hasWaypoint && (driving || wheelOpen)) list.Add(CrewOrder.DriveToWaypoint);
                if (driving) list.Add(CrewOrder.PullOver);
                bool onGun = aboard && IsTurretSeat(subject, companion.SeatIndex);
                if (!onGun && FreeTurretSeat(subject) != VehicleSeat.None) list.Add(CrewOrder.ManTheGun);
                if (!aboard && AnySeatFree(subject)) list.Add(CrewOrder.GetIn);
                if (aboard) list.Add(CrewOrder.GetOut);
            }
            list.Add(CrewOrder.FollowMe);
            list.Add(CrewOrder.HoldHere);
            list.Add(CrewOrder.OwnThing);
            return list;
        }
    }
}
