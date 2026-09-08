using Bloodlines.Crew;
using GTA;
using GTA.Native;

namespace Bloodlines.Abilities
{
    /// <summary>
    /// Guess — "Slipstream Reflex". Driving-only time dilation with added grip and
    /// torque, so the drift arcs the bible asks for are actually holdable instead
    /// of just slow-motion understeer.
    /// </summary>
    public sealed class SlipstreamReflex : Ability
    {
        public override CrewSlot Slot => CrewSlot.Guess;
        public override string Name => "Slipstream Reflex";

        public override void Activate(Ped player)
        {
            Function.Call(Hash.SET_TIME_SCALE, 0.45f);
            Function.Call(Hash.ANIMPOSTFX_PLAY, "RaceTurbo", 0, false);
        }

        public override void Update(Ped player)
        {
            var vehicle = player.CurrentVehicle;
            if (vehicle == null || !vehicle.Exists()) return;

            // Per-frame handling cheats: dropped the moment the ability ends.
            Function.Call(Hash.SET_VEHICLE_CHEAT_POWER_INCREASE, vehicle, 1.35f);
            Function.Call(Hash.SET_VEHICLE_REDUCE_GRIP, vehicle, false);
            vehicle.CanTiresBurst = false;
        }

        public override void Deactivate(Ped player)
        {
            Function.Call(Hash.SET_TIME_SCALE, 1.0f);
            Function.Call(Hash.ANIMPOSTFX_STOP, "RaceTurbo");

            var vehicle = player.CurrentVehicle;
            if (vehicle != null && vehicle.Exists())
            {
                Function.Call(Hash.SET_VEHICLE_CHEAT_POWER_INCREASE, vehicle, 1.0f);
                vehicle.CanTiresBurst = true;
            }
        }
    }
}
