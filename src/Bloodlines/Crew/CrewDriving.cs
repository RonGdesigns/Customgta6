using System;
using GTA;
using GTA.Native;
namespace Bloodlines.Crew
{
    /// <summary>
    /// How a brother drives: the speed he is told to hold, the style he holds it in, and
    /// how hard he commits to a corner. One place, because four callers each used to reach
    /// for their own numbers.
    ///
    /// Ron, September 18: "make them drive a little bit faster and a little bit better and
    /// with a little bit more intent." The numbers here used to be flat meters per second,
    /// 40 to 60, set when every car in the world ran to its stock redline. <c>WorldTuning</c>
    /// lifts every ceiling to twice that now, the player's included, so a brother told to
    /// hold sixty in a car that will do a hundred and twenty is a brother who looks like he
    /// is not trying. The speed is a fraction of the car's own capability now, with the old
    /// flat numbers kept as a floor so a slow car is never told to crawl.
    /// </summary>
    internal static class CrewDriving
    {
        // Swerve around traffic, parked vehicles, peds and objects; change lanes
        // around obstructions. Keep road pathfinding; do not force wrong-way travel.
        public const int TrafficFlags = 4 | 8 | 16 | 32 | 524288;
        /// <summary>
        /// The same, plus the two things a getaway needs and a commute does not: the wrong
        /// side of the road is allowed (512) and the map's shortcut links are used (262144).
        /// A driver with the police on him who will not cross a median is not escaping.
        /// </summary>
        public const int EscapeFlags = TrafficFlags | 512 | 262144;
        /// <summary>How far a fleeing driver looks for the police car he is fleeing from.</summary>
        public const float PoliceSearchMeters = 160f;

        /// <summary>The flat floor a brother is never told to go slower than, in meters per second.</summary>
        public static float Speed(CrewSlot slot, bool urgent) => slot == CrewSlot.Guess
            ? (urgent ? 60f : 50f) : slot == CrewSlot.Ice ? (urgent ? 50f : 42f) : (urgent ? 48f : 40f);

        /// <summary>
        /// The share of the car's own top speed each brother holds. Guess is the driver of
        /// the three and it shows; the others are quick without being him.
        /// </summary>
        public static float Fraction(CrewSlot slot, bool urgent) => slot == CrewSlot.Guess
            ? (urgent ? .95f : .7f) : slot == CrewSlot.Ice ? (urgent ? .85f : .6f) : (urgent ? .8f : .55f);

        /// <summary>
        /// The speed to command in this car: its capability times the brother's share, never
        /// below the flat floor. A car the engine will not report a speed for gets the floor.
        /// </summary>
        public static float Speed(CrewSlot slot, bool urgent, Vehicle vehicle)
        {
            float floor = Speed(slot, urgent);
            float top = Capability(vehicle);
            return top > 1f ? Math.Max(floor, top * Fraction(slot, urgent)) : floor;
        }

        /// <summary>What this car can actually do, with its parts on; zero when the engine will not say.</summary>
        public static float Capability(Vehicle vehicle)
        {
            if (vehicle == null || !vehicle.Exists()) return 0f;
            try
            {
                float top = Function.Call<float>(Hash.GET_VEHICLE_ESTIMATED_MAX_SPEED, vehicle);
                return float.IsNaN(top) || float.IsInfinity(top) || top < 0f ? 0f : top;
            }
            catch { return 0f; }
        }

        /// <summary>
        /// How hard he commits to a corner. The engine's racing modifier tightens an AI
        /// driver's lines and lets him carry speed through bends; off it, he brakes for every
        /// one the way traffic does. Guess drives at the limit when it matters; the others
        /// push without pretending to be him. Zero when nothing is urgent.
        /// </summary>
        public static float RacingModifier(CrewSlot slot, bool urgent) =>
            !urgent ? 0f : slot == CrewSlot.Guess ? 1f : slot == CrewSlot.Ice ? .7f : .6f;

        public static void Configure(Ped ped, CrewSlot slot, bool urgent)
        {
            ped.AlwaysKeepTask = true; ped.BlockPermanentEvents = true;
            Function.Call(Hash.SET_DRIVER_ABILITY, ped, 1f);
            Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ped, 2, false);
            Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS, ped,
                slot == CrewSlot.Guess ? (urgent ? .9f : .6f) : (urgent ? .75f : .4f));
            Function.Call(Hash.SET_DRIVER_RACING_MODIFIER, ped, RacingModifier(slot, urgent));
        }

        /// <summary>
        /// The nearest living police officer to a driver, or null. What a getaway is driven
        /// away from: the engine's flee mission wants an entity, and the closest cop is the
        /// one whose pursuit line matters.
        /// </summary>
        public static Ped NearestPolice(Ped driver, float within)
        {
            if (driver == null || !driver.Exists()) return null;
            int cop = Game.GenerateHash("COP");
            Ped best = null; float bestDistance = within;
            foreach (var ped in World.GetNearbyPeds(driver, within))
            {
                if (ped == null || !ped.Exists() || ped.IsDead || ped.Handle == driver.Handle) continue;
                // Read through the native rather than the wrapper: the group is what the
                // engine says it is, and the same line compiles in both harnesses.
                if (Function.Call<int>(Hash.GET_PED_RELATIONSHIP_GROUP_HASH, ped) != cop) continue;
                float distance = ped.Position.DistanceTo(driver.Position);
                if (distance < bestDistance) { best = ped; bestDistance = distance; }
            }
            return best;
        }
    }
}
