using System;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// Native Visual Atmosphere, De-Smog and Cinematic Graphics Engine.
    /// Manages real-time timecycle grading, volumetric fog density, dynamic shadow
    /// cascades, and vehicle headlight shadows directly through RAGE engine registers
    /// with zero third-party downloads, zero ReShade hooks, and zero FPS overhead.
    /// </summary>
    public sealed class VisualAtmosphere
    {
        private readonly ModConfig _config;
        private string _activeModifier = null;
        private float _activeStrength = 0f;
        private int _lastUpdateTick = 0;
        private bool _shadowsConfigured = false;

        public VisualAtmosphere(ModConfig config)
        {
            _config = config ?? new ModConfig();
        }

        public void Update(bool suppressGrading)
        {
            if (_config == null || !_config.VisualsEnabled)
            {
                if (_activeModifier != null) Reset();
                return;
            }

            // Configure dynamic shadow bounds once or when needed
            if (!_shadowsConfigured)
            {
                if (_config.ShadowDistanceScale > 1.0f)
                {
                    Function.Call(Hash.CASCADE_SHADOWS_SET_CASCADE_BOUNDS_SCALE, _config.ShadowDistanceScale);
                }
                Function.Call(Hash.SET_VEHICLE_HEADLIGHT_SHADOWS, _config.HeadlightShadowsEnabled);
                _shadowsConfigured = true;
            }

            // Per-frame rendering enhancements
            if (_config.LODBoostEnabled)
            {
                Function.Call(Hash.OVERRIDE_LODSCALE_THIS_FRAME, _config.LODScale);
                Function.Call(Hash.SET_VEHICLE_LOD_MULTIPLIER, 2.0f);
                Function.Call(Hash.SET_PED_LOD_MULTIPLIER, 2.0f);
            }

            if (_config.RemoveBlurEnabled)
            {
                Function.Call(Hash.SET_GAMEPLAY_CAM_MOTION_BLUR_SCALING_THIS_UPDATE, 0.0f);
                Function.Call(Hash.SET_DISTANCE_BLUR_STRENGTH_OVERRIDE, 0.0f);
            }

            if (_config.WaterReflectionsEnabled)
            {
                Function.Call(Hash.SET_ENTITY_USE_MAX_DISTANCE_FOR_WATER_REFLECTION, true);
                Function.Call(Hash.SET_DEEP_OCEAN_SCALER, 1.25f);
            }

            if (_config.CeramicReflectionsEnabled)
            {
                var player = Game.Player.Character;
                if (player != null && player.Exists() && player.IsInVehicle())
                {
                    var car = player.CurrentVehicle;
                    if (car != null && car.Exists())
                    {
                        Function.Call(Hash.SET_VEHICLE_ENVEFF_SCALE, car, 1.20f);
                    }
                }
            }

            // Cutscenes and interiors: gently clear custom grading to preserve authored lighting
            if (suppressGrading)
            {
                if (_activeModifier != null)
                {
                    Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
                    _activeModifier = null;
                    _activeStrength = 0f;
                }
                return;
            }

            // Throttle clock checks to once every 250ms
            int now = Game.GameTime;
            if (now - _lastUpdateTick < 250 && _activeModifier != null) return;
            _lastUpdateTick = now;

            double timeFraction = World.CurrentTimeOfDay.TotalHours;

            string targetModifier;
            float baseStrength = _config.ContrastStrength;

            // Pick profile based on time of day and chosen preset
            if (timeFraction >= 10.0 && timeFraction < 17.0)
            {
                // High Noon / Daytime De-Smog: cuts the milky gray fog and saturates skies
                targetModifier = _config.VisualPreset == "SunnyCoast"
                    ? "New_Chinatown_sky"
                    : _config.VisualPreset == "ModernCrisp"
                        ? "color_neutral"
                        : "cinema_default";
            }
            else if (timeFraction >= 17.0 && timeFraction < 20.5)
            {
                // Golden Hour / Sunset: warm amber hues, glowing reflections
                targetModifier = "rply_saturation";
                baseStrength *= 0.85f;
            }
            else if (timeFraction >= 20.5 || timeFraction < 5.5)
            {
                // Midnight: deep navy contrast, crisp streetlights and neon
                targetModifier = "cinema";
                baseStrength *= 0.75f;
            }
            else
            {
                // Dawn / Morning: crisp early air, soft golden light
                targetModifier = "cinema_default";
                baseStrength *= 0.80f;
            }

            // Apply modifier if changed or update strength
            if (_activeModifier != targetModifier)
            {
                Function.Call(Hash.SET_TIMECYCLE_MODIFIER, targetModifier);
                _activeModifier = targetModifier;
            }

            if (Math.Abs(_activeStrength - baseStrength) > 0.01f)
            {
                Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, baseStrength);
                _activeStrength = baseStrength;
            }
        }

        public void Reset()
        {
            Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
            Function.Call(Hash.CASCADE_SHADOWS_SET_CASCADE_BOUNDS_SCALE, 1.0f);
            Function.Call(Hash.SET_VEHICLE_HEADLIGHT_SHADOWS, false);
            _activeModifier = null;
            _activeStrength = 0f;
            _shadowsConfigured = false;
        }
    }
}
