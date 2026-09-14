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
    /// state across a reload. Registers without getters return to documented
    /// defaults, not to an unknowable third-party value; disable overlapping features.
    ///
    /// Measure frame times for all options. The
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
        private Ped _lodPed;
        private Vehicle _lodVehicle, _shadowVehicle;
        private float _entityLodScale;
        private readonly TimecycleGrade _grade;
        private bool _comparisonOff;
        private bool _shadowsConfigured, _lodConfigured, _oceanApplied;

        public string ActiveModifier => _grade.Name;
        public float ActiveStrength => _grade.Strength;
        public bool OceanApplied => _oceanApplied;
        public bool LodConfigured => _lodConfigured;

        public VisualAtmosphere(ModConfig config)
        {
            _config = config ?? new ModConfig();
            _grade = new TimecycleGrade(new NativeTimecyclePort(), Logger.Info);
        }

        public bool GradingComparisonOff => _comparisonOff;
        public string GradingStatus => _comparisonOff ? "baseline grading / " + _grade.Status : _grade.Status;

        /// <summary>Session-only A/B. Does not alter time, weather, LOD, forests, config or saves.</summary>
        public void ToggleGradingComparison()
        {
            _comparisonOff = !_comparisonOff;
            Logger.Info("Visuals: grading comparison " + (_comparisonOff ? "baseline (only our grade fades out)" : "configured (fade back in)") + ". Other visual controls remain unchanged.");
        }

        /// <summary>Release our grade before host paths that bypass the ordinary visuals update.</summary>
        public void SuspendGrading() { _grade.Release(); }

        /// <param name="suppressGrading">A scene or the apartment: release the grade, keep the rest.</param>
        /// <param name="missionRunning">Any mission or the prologue: no ocean swell, so boat objectives keep their authored water.</param>
        public void Update(bool suppressGrading, bool missionRunning)
        {
            if (!_config.VisualsEnabled)
            {
                if (ActiveModifier != null || _shadowsConfigured || _lodConfigured || _oceanApplied || _shadowVehicle != null || _reflecting.Count > 0) Reset();
                return;
            }

            if (!_shadowsConfigured)
            {
                Function.Call(Hash.CASCADE_SHADOWS_SET_CASCADE_BOUNDS_SCALE, _config.ShadowDistanceScale);
                _shadowsConfigured = true;
            }

            if (_config.LODBoostEnabled)
            {
                float scale = Math.Min(MaxLodScale, _config.LODScale);
                _lodConfigured = true;
                // The scene override is per frame; the host's tick already skips
                // this step while the apartment or a recovery is streaming.
                Function.Call(Hash.OVERRIDE_LODSCALE_THIS_FRAME, scale);
            }

            UpdateEntityVisuals();
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

            string target = ModifierFor(World.CurrentTimeOfDay.TotalHours, out float strength);
            _grade.Update(_comparisonOff ? null : target, strength, Game.GameTime, suppressGrading, Game.IsPaused);
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
                // Read out of the installed archives on September 13: the game defines
                // 1,119 timecycle modifiers and none of them is cinema_default,
                // color_neutral or New_Chinatown_sky. Those three never applied, so
                // the default daylight on screen has always been the game's own and
                // stays that way. The two named presets now ask for modifiers that
                // exist; DayModifier in the ini still overrides any of it.
                if (_config.VisualPreset == "SunnyCoast") return "cinema_001";
                if (_config.VisualPreset == "ModernCrisp") return "NeutralColorCode";
                return null;
            }
            if (hours >= 17.0 && hours < 20.5)
            {
                // No grade at dusk unless the ini names one: the saturation filter
                // made the sunset garish (Ron, September 12); the game's own is right.
                strength = contrast * 0.85f;
                return string.IsNullOrWhiteSpace(_config.DuskModifier) ? null : _config.DuskModifier;
            }
            if (hours >= 20.5 || hours < 5.5)
            {
                strength = contrast * 0.75f;
                return string.IsNullOrWhiteSpace(_config.NightModifier) ? "cinema" : _config.NightModifier;
            }
            strength = contrast * 0.80f;
            // Same missing name as the day grade had: dawn was never graded either.
            return string.IsNullOrWhiteSpace(_config.DawnModifier) ? null : _config.DawnModifier;
        }

        // These natives are per entity, not global switches. Own at most the
        // current player and vehicle; restore default values when they leave scope.
        // GTA exposes no getter for these settings, so another graphics mod should
        // own them instead when these options are disabled.
        private void UpdateEntityVisuals()
        {
            var player = Game.Player.Character;
            if (player != null && (!player.Exists() || player.IsDead)) player = null;
            var car = player?.CurrentVehicle;
            if (car != null && (!car.Exists() || car.IsDead)) car = null;
            float scale = _config.LODBoostEnabled ? Math.Max(1f, Math.Min(MaxLodScale, _config.LODScale)) : 1f;
            var nextPed = _config.LODBoostEnabled ? player : null;
            var nextCar = _config.LODBoostEnabled ? car : null;
            if (_lodPed != nextPed || _entityLodScale != scale)
            {
                if (_lodPed != null && _lodPed.Exists()) Function.Call(Hash.SET_PED_LOD_MULTIPLIER, _lodPed, 1f);
                _lodPed = null;
                if (nextPed != null) { Function.Call(Hash.SET_PED_LOD_MULTIPLIER, nextPed, scale); _lodPed = nextPed; }
            }
            if (_lodVehicle != nextCar || _entityLodScale != scale)
            {
                if (_lodVehicle != null && _lodVehicle.Exists()) Function.Call(Hash.SET_VEHICLE_LOD_MULTIPLIER, _lodVehicle, 1f);
                _lodVehicle = null;
                if (nextCar != null) { Function.Call(Hash.SET_VEHICLE_LOD_MULTIPLIER, nextCar, scale); _lodVehicle = nextCar; }
            }
            _entityLodScale = scale;
            _lodConfigured = _config.LODBoostEnabled;
            var nextShadow = _config.HeadlightShadowsEnabled ? car : null;
            if (_shadowVehicle != nextShadow)
            {
                if (_shadowVehicle != null && _shadowVehicle.Exists()) Function.Call(Hash.SET_VEHICLE_HEADLIGHT_SHADOWS, _shadowVehicle, 0);
                _shadowVehicle = null;
                if (nextShadow != null) { Function.Call(Hash.SET_VEHICLE_HEADLIGHT_SHADOWS, nextShadow, 3); _shadowVehicle = nextShadow; }
            }
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
            _grade.Release();
            if (_shadowsConfigured) Function.Call(Hash.CASCADE_SHADOWS_SET_CASCADE_BOUNDS_SCALE, 1.0f);
            if (_shadowVehicle != null && _shadowVehicle.Exists()) Function.Call(Hash.SET_VEHICLE_HEADLIGHT_SHADOWS, _shadowVehicle, 0);
            if (_lodVehicle != null && _lodVehicle.Exists()) Function.Call(Hash.SET_VEHICLE_LOD_MULTIPLIER, _lodVehicle, 1f);
            if (_lodPed != null && _lodPed.Exists()) Function.Call(Hash.SET_PED_LOD_MULTIPLIER, _lodPed, 1f);
            _shadowVehicle = _lodVehicle = null; _lodPed = null; _entityLodScale = 0f;
            if (_oceanApplied) Function.Call(Hash.RESET_DEEP_OCEAN_SCALER);
            foreach (var entity in _reflecting.Values)
                if (entity != null && entity.Exists()) Function.Call(Hash.SET_ENTITY_USE_MAX_DISTANCE_FOR_WATER_REFLECTION, entity, false);
            _reflecting.Clear();
            _shadowsConfigured = false;
            _lodConfigured = false;
            _oceanApplied = false;
        }
    }
}
