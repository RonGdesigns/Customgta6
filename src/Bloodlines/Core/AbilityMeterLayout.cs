using System;
namespace Bloodlines.Core
{
    /// <summary>Normalized slot at the right-hand end of the regular minimap bars.</summary>
    public struct AbilityMeterLayout
    {
        public float X, Y, Width, Height;
        public static AbilityMeterLayout Calculate(float aspect, float safeZone)
        {
            aspect = Math.Max(1f, aspect);
            safeZone = Math.Max(.8f, Math.Min(1f, safeZone));
            float margin = (1f - safeZone) * .5f;
            float horizontalScale = (16f / 9f) / aspect;
            return new AbilityMeterLayout { X = margin + .121f * horizontalScale,
                Y = 1f - margin - .033f, Width = .041f * horizontalScale, Height = .007f };
        }
    }
}
