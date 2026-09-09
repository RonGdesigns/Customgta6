using GTA;
using GTA.Math;
using GTA.Native;
namespace Bloodlines.Crew
{
    internal static class RecoveryMobility
    {
        public static void Restore(Ped ped)
        {
            if (ped == null || !ped.Exists()) return;
            ped.IsPositionFrozen = false;
            ped.IsCollisionEnabled = true;
            ped.IsVisible = true;
            ped.Velocity = Vector3.Zero;
            ped.AlwaysKeepTask = false;
            ped.Task.ClearAllImmediately();
            Function.Call(Hash.UNCUFF_PED, ped);
            Function.Call(Hash.SET_ENABLE_HANDCUFFS, ped, false);
            Function.Call(Hash.RESET_PED_MOVEMENT_CLIPSET, ped, 0f);
            Function.Call(Hash.RESET_PED_STRAFE_CLIPSET, ped);
            Function.Call(Hash.RESET_PED_WEAPON_MOVEMENT_CLIPSET, ped);
        }
    }
}
