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
        private float _meter = 1f;
        private int _lastTick;

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
            ability.Activate(player);
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
                if (player == null || !player.Exists() || player.IsDead)
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

            DrawMeter();
        }

        /// <summary>Shuts the ability down and undoes its world state. Safe to call twice.</summary>
        public void Stop()
        {
            if (_running == null) return;

            var player = Game.Player.Character;
            try
            {
                _running.Deactivate(player);
            }
            catch (System.Exception ex)
            {
                Logger.Error("Ability deactivate failed for " + _running.Name, ex);
            }
            finally
            {
                // Belt and braces: a stuck time scale ruins the whole session.
                Function.Call(Hash.SET_TIME_SCALE, 1.0f);
                _running = null;
            }
        }

        private void DrawMeter()
        {
            if (!_crew.IsDeployed) return;

            var protagonist = _crew.Active;
            // GTA.UI works in a fixed 1280x720 space regardless of the player's
            // actual resolution, so these are safe absolute positions.
            const float x = 60f;
            float y = Screen.Height - 80f;

            var barBack = new ContainerElement(new PointF(x, y), new SizeF(220f, 9f),
                Color.FromArgb(170, 12, 12, 14));
            var barFill = new ContainerElement(new PointF(x, y), new SizeF(220f * _meter, 9f),
                _running != null
                    ? Color.FromArgb(235, 232, 168, 56)
                    : Color.FromArgb(205, 168, 178, 190));
            var label = new TextElement(protagonist.AbilityName, new PointF(x, y - 24f), 0.30f,
                Color.FromArgb(225, 226, 226, 230));

            barBack.Draw();
            barFill.Draw();
            label.Draw();
        }
    }
}
