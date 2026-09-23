using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// Riding in the back of a box truck. A Benson has two seats, so a third
    /// brother cannot be seated in it at all: asking for LeftRear fails the seat
    /// check and the mission reports that the vehicle has no valid crew seat, which
    /// is what M38 did in Ron's September 13 run.
    ///
    /// M03 already solved this the honest way — walk him to the rear doors, attach
    /// him inside the box, shut the doors — and that solution is here so a second
    /// mission does not have to copy it. Attachment is the mechanism because the
    /// cargo area is not a seat: the engine has nothing to put him in.
    ///
    /// Everything is bounded and reversible. Nothing here deletes a ped, and the
    /// unload always detaches before it moves anyone.
    /// </summary>
    public static class CargoRide
    {
        /// <summary>Where a passenger sits inside the box, relative to the truck.</summary>
        public static readonly Vector3 InsideTheBox = new Vector3(0f, -3.2f, 1.3f);
        /// <summary>How far behind the truck the rear doors are.</summary>
        public const float RearDoorMeters = 4f;
        /// <summary>Close enough to the doors to be counted as having walked there.</summary>
        public const float AtTheDoors = 3f;

        /// <summary>The standing spot at the rear doors, or the fallback when the truck is gone.</summary>
        public static Vector3 RearOf(Vehicle truck, Vector3 fallback) =>
            truck != null && truck.Exists() ? truck.Position - truck.ForwardVector * RearDoorMeters : fallback;

        public static void OpenDoors(Vehicle truck)
        {
            if (truck == null || !truck.Exists()) return;
            Function.Call(Hash.SET_VEHICLE_DOOR_OPEN, truck, 2, false, false);
            Function.Call(Hash.SET_VEHICLE_DOOR_OPEN, truck, 3, false, false);
        }

        public static void CloseDoors(Vehicle truck)
        {
            if (truck == null || !truck.Exists()) return;
            Function.Call(Hash.SET_VEHICLE_DOOR_SHUT, truck, 2, false);
            Function.Call(Hash.SET_VEHICLE_DOOR_SHUT, truck, 3, false);
        }

        /// <summary>True once this passenger is actually riding in this truck's box.</summary>
        public static bool Aboard(Ped passenger, Vehicle truck) =>
            passenger != null && passenger.Exists() && truck != null && truck.Exists() &&
            Function.Call<bool>(Hash.IS_ENTITY_ATTACHED_TO_ENTITY, passenger, truck);

        /// <summary>Send him to the doors, which open for him. The caller owns his AI.</summary>
        public static void CallToTheDoors(Ped passenger, Vehicle truck, Vector3 fallback)
        {
            if (passenger == null || !passenger.Exists() || truck == null || !truck.Exists()) return;
            OpenDoors(truck);
            if (passenger.IsInVehicle()) ExitVehicleStep.ForceOut(passenger);
            passenger.Task.RunTo(RearOf(truck, fallback), false, -1);
        }

        /// <summary>
        /// Put him in the box. Returns true when he is riding, whether he walked
        /// there or the deadline passed and he was loaded where he stood: a brother
        /// who cannot path to the doors must not strand the mission forever.
        /// </summary>
        public static bool Load(Ped passenger, Vehicle truck, Vector3 fallback, bool deadlinePassed, string missionId)
        {
            if (passenger == null || !passenger.Exists() || truck == null || !truck.Exists()) return true;
            if (Aboard(passenger, truck)) return true;
            bool atTheDoors = passenger.Position.DistanceTo(RearOf(truck, fallback)) <= AtTheDoors;
            if (!atTheDoors && !deadlinePassed) return false;
            if (!atTheDoors) Logger.Warn(missionId + ": a passenger did not reach the back of the truck in time; loaded directly.");
            if (passenger.IsInVehicle()) ExitVehicleStep.ForceOut(passenger);
            passenger.Task.ClearAllImmediately();
            Function.Call(Hash.ATTACH_ENTITY_TO_ENTITY, passenger, truck, 0,
                InsideTheBox.X, InsideTheBox.Y, InsideTheBox.Z, 0f, 0f, 0f, false, false, false, true, 2, true);
            CloseDoors(truck);
            Logger.Info(missionId + ": a passenger is riding in the back of the truck.");
            return true;
        }

        /// <summary>Out of the box and standing at the rear doors, on his own feet again.</summary>
        public static void Unload(Ped passenger, Vehicle truck, Vector3 fallback)
        {
            if (!Aboard(passenger, truck)) return;
            Function.Call(Hash.DETACH_ENTITY, passenger, true, true);
            passenger.Position = RearOf(truck, fallback);
            passenger.Task.ClearAllImmediately();
        }
    }
}
