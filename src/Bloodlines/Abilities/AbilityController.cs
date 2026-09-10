using System.Collections.Generic;
using System.Drawing;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Native;
using GTA.UI;

namespace Bloodlines.Abilities
{
    /// <summary>
    /// Single shared ability meter. Whoever is active spends the same pool, which
    /// keeps the switch decision honest: burning Ice's focus leaves Guess without
    /// slipstream for the getaway.
    /// </summary>
    public sealed class AbilityController
    {
        private readonly ModConfig _config;
        private readonly CrewRoster _crew;
        private readonly Dictionary<CrewSlot, Ability> _abilities;

        private Ability _running;
        private Ability _settling;
        private Ped _owner;
        private float _meter = 1f;
        private int _lastTick;
        private readonly AbilityChord _chord = new AbilityChord();

        public AbilityController(ModConfig config, CrewRoster crew)
        {
            _config = config;
            _crew = crew;
            _abilities = new Dictionary<CrewSlot, Ability>
            {
                { CrewSlot.Ice, new OverwatchFocus() },
                { CrewSlot.Gohan, new ThermalPulse() },
                { CrewSlot.Guess, new SlipstreamReflex() }
            };
            _lastTick = Game.GameTime;
        }

        public void HandleController(bool blocked)
        {
            bool left = ControllerInput.Pressed(GTA.Control.ScriptLS);
            bool right = ControllerInput.Pressed(GTA.Control.ScriptRS);
            if (_crew.IsDeployed)
            {
                Game.DisableControlThisFrame(GTA.Control.SpecialAbility);
                Game.DisableControlThisFrame(GTA.Control.SpecialAbilitySecondary);
                Game.DisableControlThisFrame(GTA.Control.SpecialAbilityPC);
                Game.DisableControlThisFrame(GTA.Control.VehicleSpecialAbilityFranklin);
                if (left && right)
                {
                    Game.DisableControlThisFrame(GTA.Control.Duck);
                    Game.DisableControlThisFrame(GTA.Control.VehicleHorn);
                    Game.DisableControlThisFrame(GTA.Control.LookBehind);
                    Game.DisableControlThisFrame(GTA.Control.VehicleLookBehind);
                }
            }
            if (_chord.Update(left, right, blocked || Game.IsPaused || !_crew.IsDeployed || !_config.AbilitiesEnabled)) Toggle();
        }

        public bool IsActive => _running != null;

        public float Meter => _meter;

        public void Toggle()
        {
            if (!_config.AbilitiesEnabled || !_crew.IsDeployed) return;

            if (_running != null)
            {
                Stop();
                return;
            }

            if (_meter < 0.15f)
            {
                GameUtils.Subtitle("~r~Ability meter is empty.", 2000);
                return;
            }

            var player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) return;

            if (!_abilities.TryGetValue(_crew.ActiveSlot, out var ability)) return;

            _running = ability;
            _owner = player;
            try { ability.Activate(player); }
            catch { Stop(); throw; }
            GameUtils.Subtitle("~y~" + ability.Name, 2000);
            Logger.Debug("Ability up: " + ability.Name);
        }

        /// <summary>Dev menu: top the shared meter back up.</summary>
        public void Refill()
        {
            _meter = 1f;
        }

        public void Update()
        {
            int now = Game.GameTime;
            float delta = (now - _lastTick) / 1000f;
            _lastTick = now;
            if (delta <= 0f || delta > 1f) delta = 0.016f;

            var player = Game.Player.Character;

            if (_running != null)
            {
                if (!_crew.IsDeployed || player == null || !player.Exists() || player.IsDead ||
                    _owner == null || !_owner.Exists() || player.Handle != _owner.Handle)
                {
                    Stop();
                }
                else
                {
                    _running.Update(player);
                    _meter -= delta / _config.AbilityDuration;
                    if (_meter <= 0f)
                    {
                        _meter = 0f;
                        Stop();
                        GameUtils.Subtitle("~r~Ability burned out.", 2000);
                    }
                }
            }
            else if (_meter < 1f)
            {
                _meter = System.Math.Min(1f, _meter + delta * _config.AbilityRechargeRate);
            }
            if (_settling != null && _running == null)
            {
                try { if (player == null || !player.Exists() || !_settling.Settle(player)) _settling = null; }
                catch (System.Exception ex) { Logger.Error("Ability settle failed for " + _settling.Name, ex); _settling = null; }
            }

            DrawMeter();
        }

        /// <summary>Shuts the ability down and undoes its world state. Safe to call twice.</summary>
        public void Stop()
        {
            if (_running == null) return;

            var player = Game.Player.Character;
            try
            {
                _running.Deactivate(_owner != null && _owner.Exists() ? _owner : player);
            }
            catch (System.Exception ex)
            {
                Logger.Error("Ability deactivate failed for " + _running.Name, ex);
            }
            finally
            {
                // Belt and braces: a stuck time scale ruins the whole session.
                _settling = _running;
                _running = null;
                _owner = null;
                Function.Call(Hash.SET_TIME_SCALE, 1.0f);
            }
        }

        private void DrawMeter()
        {
            if (!_crew.IsDeployed) return;

            if (!_config.AbilitiesEnabled || Game.IsPaused || Function.Call<bool>(Hash.IS_RADAR_HIDDEN) ||
                Function.Call<bool>(Hash.IS_HUD_HIDDEN)) return;
            var slot = AbilityMeterLayout.Calculate(Function.Call<float>(Hash.GET_ASPECT_RATIO, false),
                Function.Call<float>(Hash.GET_SAFE_ZONE_SIZE));
            // Compact yellow overlay in the original ability-bar area; health/armor stay visible.
            Function.Call(Hash.DRAW_RECT, slot.X + slot.Width * .5f, slot.Y, slot.Width, slot.Height, 20, 17, 7, 230);
            float fill = slot.Width * System.Math.Max(0f, System.Math.Min(1f, _meter));
            if (fill > 0f) Function.Call(Hash.DRAW_RECT, slot.X + fill * .5f, slot.Y, fill, slot.Height,
                232, _running != null ? 194 : 168, 56, 240);
        }
    }
}
