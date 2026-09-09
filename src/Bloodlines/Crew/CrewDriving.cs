using GTA;
using GTA.Native;
namespace Bloodlines.Crew
{
    internal static class CrewDriving
    {
        // Swerve around traffic, parked vehicles, peds and objects; change lanes
        // around obstructions. Keep road pathfinding; do not force wrong-way travel.
        public const int TrafficFlags = 4 | 8 | 16 | 32 | 524288;
        public static float Speed(CrewSlot slot, bool urgent) => slot == CrewSlot.Guess
            ? (urgent ? 60f : 50f) : slot == CrewSlot.Ice ? (urgent ? 50f : 42f) : (urgent ? 48f : 40f);
        public static void Configure(Ped ped, CrewSlot slot, bool urgent)
        {
            ped.AlwaysKeepTask = true; ped.BlockPermanentEvents = true;
            Function.Call(Hash.SET_DRIVER_ABILITY, ped, 1f);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 2, false);
            Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, ped,
                slot == CrewSlot.Guess ? (urgent ? .8f : .6f) : (urgent ? .6f : .4f));
        }
    }
}
