using System;
using GTA;
using GTA.Math;

namespace Bloodlines.Core
{
    /// <summary>
    /// Where a prop goes so that it rests on another prop. The game does not settle
    /// a frozen object onto a surface for you; a laptop created a fixed height above
    /// a table's origin floats or sinks depending on that model's origin. The
    /// surface's top bound plus the item's own base offset is right for any pair.
    /// </summary>
    public static class PropPlacement
    {
        /// <summary>A working surface is about this high when the game will not say.</summary>
        public const float AssumedSurfaceHeight = 0.75f;

        public static Vector3 OnTop(Entity surface, Model surfaceModel, Model itemModel, float forward = 0f, float right = 0f)
        {
            if (surface == null || !surface.Exists())
                throw new ArgumentException("Nothing to put it on.", nameof(surface));
            float top = surfaceModel.Dimensions.Item2.Z;
            float itemBase = itemModel.Dimensions.Item1.Z;
            // A model that has been released reports no dimensions, and the spawn helpers
            // release theirs as soon as the prop exists. Zero here puts the item at the
            // surface's own origin, which is inside it: that is a laptop on a table the
            // player cannot see, which is what Ron found in M43. Assume a working height
            // instead of stacking nothing on nothing, and say so.
            if (top <= 0.01f)
            {
                top = AssumedSurfaceHeight;
                Logger.Warn("A surface reported no dimensions when something was placed on it; " +
                    "assuming " + AssumedSurfaceHeight + " m. The item would otherwise sit inside it.");
            }
            var point = ShotStep.Local(surface, new Vector3(forward, right, 0f));
            return new Vector3(point.X, point.Y, surface.Position.Z + top - itemBase + 0.01f);
        }
    }
}
