using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// Native visual atmosphere: time-of-day grading through the game's own
    /// timecycle modifiers, longer shadow cascades, headlight shadows, a longer
    /// level-of-detail range, camera blur off, water reflection distance on the
    /// player and their car, and a deeper ocean swell outside missions. Every
    /// register it touches is put back by <see cref="Reset"/>, which the host runs
    /// on abort, on stand-down and on script reload, because the engine keeps this
    /// state across a reload and nothing else would ever clear it.
    ///
    /// Costs, stated honestly: the timecycle and blur settings are free. The
    /// cascade scale, headlight shadows and the level-of-detail range are real
    /// GPU and streaming work; they are the keys to turn off first when the game
    /// stutters at speed. Grading is released during scenes and inside the
    /// apartment so authored lighting stays as authored.
    /// </summary>
    public sealed class VisualAtmosphere
    {
        public const float OceanSwell = 1.25f;
        public const float MaxLodScale = 2.0f;

        private readonly ModConfig _config;
        private readonly Dictionary<int, Entity> _reflecting = new Dictionary<int, Entity>();
        private string _activeModifier;
        private float _activeStrength;
        private int _lastClockCheck;
        private bool _shadowsConfigured, _lodConfigured, _oceanApplied;

        public string ActiveModifier => _activeModifier;
        public float ActiveStrength => _activeStrength;
        public bool OceanApplied => _oceanApplied;
        public bool LodConfigured => _lodConfigured;

        public VisualAtmosphere(ModConfig config)
        {
            _config = config ?? new ModConfig();
        }

        /// <param name="suppressGrading">A scene or the apartment: release the grade, keep the rest.</param>
        /// <param name="missionRunning">Any mission or the prologue: no ocean swell, so boat objectives keep their authored water.</param>
        public void Update(bool suppressGrading, bool missionRunning)
        {
            if (!_config.VisualsEnabled)
            {
                if (_activeModifier != null || _shadowsConfigured || _lodConfigured || _oceanApplied) Reset();
                return;
            }

            if (!_shadowsConfigured)
            {
                Function.Call(Hash.CASCADE_SHADOWS_SET_CASCADE_BOUNDS_SCALE, _config.ShadowDistanceScale);
                Function.Call(Hash.SET_VEHICLE_HEADLIGHT_SHADOWS, _config.HeadlightShadowsEnabled);
                _shadowsConfigured = true;
            }

            if (_config.LODBoostEnabled)
            {
                float scale = Math.Min(MaxLodScale, _config.LODScale);
                if (!_lodConfigured)
                {
                    // Persistent settings: set once, put back in Reset.
                    Function.Call(Hash.SET_VEHICLE_LOD_MULTIPLIER, scale);
                    Function.Call(Hash.SET_PED_LOD_MULTIPLIER, scale);
                    _lodConfigured = true;
                }
                // The scene override is per frame; the host's tick already skips
                // this step while the apartment or a recovery is streaming.
                Function.Call(Hash.OVERRIDE_LODSCALE_THIS_FRAME, scale);
            }

            if (_config.RemoveBlurEnabled)
            {
                Function.Call(Hash.SET_GAMEPLAY_CAM_MOTION_BLUR_SCALING_THIS_UPDATE, 0f);
                Function.Call(Hash.SET_DISTANCE_BLUR_STRENGTH_OVERRIDE, 0f);
            }

            if (_config.WaterReflectionsEnabled) UpdateWaterReflections();

            if (_config.OceanSwellEnabled && !missionRunning)
            {
                if (!_oceanApplied) { Function.Call(Hash.SET_DEEP_OCEAN_SCALER, OceanSwell); _oceanApplied = true; }
            }
            else if (_oceanApplied)
            {
                Function.Call(Hash.RESET_DEEP_OCEAN_SCALER);
                _oceanApplied = false;
            }

            if (suppressGrading) { ClearGrade(); return; }

            int now = Game.GameTime;
            if (_activeModifier != null && now - _lastClockCheck < 250) return;
            _lastClockCheck = now;

            string target = ModifierFor(World.CurrentTimeOfDay.TotalHours, out float strength);
            if (string.IsNullOrEmpty(target)) { ClearGrade(); return; }
            if (_activeModifier != target)
            {
                Function.Call(Hash.SET_TIMECYCLE_MODIFIER, target);
                _activeModifier = target;
                _activeStrength = -1f;
                Logger.Info("Visuals: timecycle modifier '" + target + "' at " + strength.ToString("0.00") + " (a name the game does not know applies nothing; see docs/VISUALS.md).");
            }
            if (Math.Abs(_activeStrength - strength) > 0.01f)
            {
                Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, strength);
                _activeStrength = strength;
            }
        }

        /// <summary>
        /// The modifier for an hour of the day under the configured preset, or null
        /// for none. Daytime is the de-smog grade and honors the DeSmog key; the
        /// four ini overrides win over the preset when set.
        /// </summary>
        public string ModifierFor(double hours, out float strength)
        {
            float contrast = _config.ContrastStrength;
            if (hours >= 10.0 && hours < 17.0)
            {
                strength = contrast;
                if (!_config.DeSmogEnabled) return null;
                if (!string.IsNullOrWhiteSpace(_config.DayModifier)) return _config.DayModifier;
                return _config.VisualPreset == "SunnyCoast" ? "New_Chinatown_sky" : _config.VisualPreset == "ModernCrisp" ? "color_neutral" : "cinema_default";
            }
            if (hours >= 17.0 && hours < 20.5)
            {
                strength = contrast * 0.85f;
                return string.IsNullOrWhiteSpace(_config.DuskModifier) ? "rply_saturation" : _config.DuskModifier;
            }
            if (hours >= 20.5 || hours < 5.5)
            {
                strength = contrast * 0.75f;
                return string.IsNullOrWhiteSpace(_config.NightModifier) ? "cinema" : _config.NightModifier;
            }
            strength = contrast * 0.80f;
            return string.IsNullOrWhiteSpace(_config.DawnModifier) ? "cinema_default" : _config.DawnModifier;
        }

        private void ClearGrade()
        {
            if (_activeModifier == null) return;
            Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
            _activeModifier = null;
            _activeStrength = 0f;
        }

        /// <summary>The reflection-distance flag is per entity: the player and whatever they are driving, once each.</summary>
        private void UpdateWaterReflections()
        {
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;
            Reflect(player);
            var car = player.CurrentVehicle;
            if (car != null && car.Exists()) Reflect(car);
            if (_reflecting.Count > 24)
                foreach (var stale in new List<int>(_reflecting.Keys))
                    if (!_reflecting[stale].Exists()) _reflecting.Remove(stale);
        }

        private void Reflect(Entity entity)
        {
            if (_reflecting.ContainsKey(entity.Handle)) return;
            Function.Call(Hash.SET_ENTITY_USE_MAX_DISTANCE_FOR_WATER_REFLECTION, entity, true);
            _reflecting[entity.Handle] = entity;
        }

        public void Reset()
        {
            Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
            Function.Call(Hash.CASCADE_SHADOWS_SET_CASCADE_BOUNDS_SCALE, 1.0f);
            Function.Call(Hash.SET_VEHICLE_HEADLIGHT_SHADOWS, false);
            Function.Call(Hash.SET_VEHICLE_LOD_MULTIPLIER, 1.0f);
            Function.Call(Hash.SET_PED_LOD_MULTIPLIER, 1.0f);
            Function.Call(Hash.RESET_DEEP_OCEAN_SCALER);
            foreach (var entity in _reflecting.Values)
                if (entity != null && entity.Exists()) Function.Call(Hash.SET_ENTITY_USE_MAX_DISTANCE_FOR_WATER_REFLECTION, entity, false);
            _reflecting.Clear();
            _activeModifier = null;
            _activeStrength = 0f;
            _shadowsConfigured = false;
            _lodConfigured = false;
            _oceanApplied = false;
        }
    }
}
