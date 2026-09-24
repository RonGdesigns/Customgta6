using System;

namespace Bloodlines.Core
{
    /// <summary>
    /// Which surface a freshly spawned vehicle belongs on. Kept free of the game so it can be
    /// tested; <see cref="GameUtils.SetOnGround"/> supplies the probe.
    ///
    /// The ground used to be looked for once, from 40 m above the spawn point. That finds the
    /// first surface going down, and over the crew van's stash in Cypress Flats that is the
    /// warehouse roof 13 m up: Ron found the van parked on it (September 23). The probe exists
    /// for spawns whose estimate lies under the terrain (M03's Primo, September 11), so it has
    /// to be able to reach up, but the surface a spawn stands on is the one nearest above it.
    /// So the probe starts just above the point and only climbs when nothing belongs to it.
    /// </summary>
    public static class VehicleGround
    {
        /// <summary>Heights above the spawn point the ground is looked for from, lowest first.</summary>
        public static readonly float[] ProbeLifts = { 2.5f, 6f, 12f, 25f, 40f };
        /// <summary>A surface further below the spawn point than this is not the one it was meant for.</summary>
        public const float BelowTolerance = 1.5f;

        /// <summary>
        /// The ground for a vehicle spawned at <paramref name="spawnZ"/>, or null when no probe
        /// answers. <paramref name="groundBelow"/> returns the first surface under a height, or
        /// null.
        /// </summary>
        public static float? Choose(float spawnZ, Func<float, float?> groundBelow)
        {
            if (groundBelow == null) return null;
            foreach (float lift in ProbeLifts)
            {
                float from = spawnZ + lift;
                var ground = groundBelow(from);
                if (!ground.HasValue || ground.Value > from) continue;
                // Far under the point is a probe that started inside the terrain and fell
                // through it; climb and look again.
                if (ground.Value < spawnZ - BelowTolerance) continue;
                return ground.Value;
            }
            return null;
        }
    }
}
