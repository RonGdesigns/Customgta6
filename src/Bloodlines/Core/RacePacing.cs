using System;

namespace Bloodlines.Core
{
    /// <summary>
    /// How fast a rival driver is told to go. Pure arithmetic, so the numbers that decide
    /// whether a race is a contest can be read off a test instead of a play session.
    ///
    /// Ron's report was that the rivals were no challenge, and the cause was not their
    /// ability, which was already at the maximum the engine takes. It was the commanded
    /// speed: a flat 39 meters per second, about 87 mph, on a route where the player's own
    /// car runs to twice its stock redline because <c>WorldTuning</c> lifts every ceiling in
    /// the world. The rivals were not driving badly. They were obeying a slow order.
    ///
    /// So the speed comes from the car's own capability, and the only thing this adds on top
    /// is a bounded, symmetric correction so the race stays a race:
    ///
    ///  * a rival far enough behind is told to drive harder, up to <see cref="AssistCeiling"/>;
    ///  * a rival far enough ahead eases off, down to <see cref="LiftFloor"/>;
    ///  * between those bands he simply drives.
    ///
    /// Both ends are capped and both are logged, because an assist nobody can see the edge
    /// of is indistinguishable from cheating. The correction is applied to the **commanded
    /// speed only**. It never touches <c>SET_VEHICLE_CHEAT_POWER_INCREASE</c> or the entity
    /// speed cap, which <c>WorldTuning</c> owns per car: a second writer on those is the
    /// double-ownership bug this project keeps finding, and it is not needed here, because a
    /// car whose ceiling is already doubled can reach any speed this asks of it.
    /// </summary>
    public static class RacePacing
    {
        /// <summary>Fraction of its own top speed a rival is told to hold on asphalt.</summary>
        public const float RoadFraction = .94f;
        /// <summary>The same on the mountain trail, where the surface is dirt and the bends are blind.</summary>
        public const float TrailFraction = .5f;
        /// <summary>The most a trailing rival may be helped.</summary>
        public const float AssistCeiling = 1.35f;
        /// <summary>The least a runaway leader is eased back to.</summary>
        public const float LiftFloor = .85f;
        /// <summary>Meters behind the player before any help begins.</summary>
        public const float AssistFrom = 220f;
        /// <summary>Meters ahead of the player before easing off begins.</summary>
        public const float LiftFrom = 320f;
        /// <summary>Over how many further meters each correction reaches its limit.</summary>
        public const float Band = 400f;
        /// <summary>Never command less than this, whatever the car and the correction say.</summary>
        public const float FloorSpeed = 12f;

        /// <summary>
        /// The correction for a rival <paramref name="metersAhead"/> in front of the player.
        /// Negative means behind. One inside the dead band, up to the ceiling below it, down
        /// to the floor above it.
        /// </summary>
        public static float Correction(float metersAhead)
        {
            if (float.IsNaN(metersAhead) || float.IsInfinity(metersAhead)) return 1f;
            if (metersAhead < -AssistFrom)
            {
                float into = Math.Min(1f, (-metersAhead - AssistFrom) / Band);
                return 1f + (AssistCeiling - 1f) * into;
            }
            if (metersAhead > LiftFrom)
            {
                float into = Math.Min(1f, (metersAhead - LiftFrom) / Band);
                return 1f - (1f - LiftFloor) * into;
            }
            return 1f;
        }

        /// <summary>Whether a correction is doing anything worth writing down.</summary>
        public static bool IsCorrecting(float metersAhead) => Math.Abs(Correction(metersAhead) - 1f) > .001f;

        /// <summary>
        /// The speed to command, from the car's own capability in meters per second.
        /// A capability the engine will not report falls back to a road speed that is quick
        /// without pretending to be a measurement.
        /// </summary>
        public static float Speed(float capability, bool onTrail, float metersAhead)
        {
            float top = capability > 1f && !float.IsNaN(capability) && !float.IsInfinity(capability) ? capability : 45f;
            float wanted = top * (onTrail ? TrailFraction : RoadFraction) * Correction(metersAhead);
            return Math.Max(FloorSpeed, wanted);
        }
    }
}
