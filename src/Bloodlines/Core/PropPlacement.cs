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
        public static Vector3 OnTop(Entity surface, Model surfaceModel, Model itemModel, float forward = 0f, float right = 0f)
        {
            float top = surfaceModel.Dimensions.Item2.Z;
            float itemBase = itemModel.Dimensions.Item1.Z;
            var point = ShotStep.Local(surface, new Vector3(forward, right, 0f));
            return new Vector3(point.X, point.Y, surface.Position.Z + top - itemBase + 0.01f);
        }
    }
}
