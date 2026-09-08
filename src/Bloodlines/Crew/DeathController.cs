using System;
using Bloodlines.Abilities;
using Bloodlines.Core;
using Bloodlines.Missions;
using GTA;
using GTA.Native;

namespace Bloodlines.Crew
{
    /// <summary>Bounded, frame-driven recovery while custom characters are deployed.</summary>
    public sealed class DeathController
    {
        private enum Phase { Idle, FadingOut, Holding }
        private readonly ModConfig _config;
        private readonly CrewRoster _crew;
        private readonly MissionManager _missions;
        private readonly AbilityController _abilities;
        private readonly SwitchController _switching;
        private readonly DialogueDirector _dialogue;
        private Phase _phase;
        private int _phaseStarted;
        private bool _suppressionApplied;
        private string _cause;
        private string _name;

        public DeathController(ModConfig config, CrewRoster crew, MissionManager missions,
            AbilityController abilities, SwitchController switching, DialogueDirector dialogue)
        {
            _config = config;
            _crew = crew;
            _missions = missions;
            _abilities = abilities;
            _switching = switching;
            _dialogue = dialogue;
        }

        public int DeathCount { get; private set; }
        public bool IsHandling => _phase != Phase.Idle;

        public void Update()
        {
            try
            {
                if (!_config.DeathHandlingEnabled || !_crew.IsDeployed)
                {
                    Cancel();
                    return;
                }

                ApplySuppression();
                if (_phase == Phase.Idle)
                {
                    var player = Game.Player.Character;
                    var active = _crew.PedFor(_crew.ActiveSlot);
                    bool dead = active == null || active.IsDead || player == null ||
                                !player.Exists() || player.IsDead || Game.Player.IsDead;
                    bool busted = !dead && Function.Call<bool>(Hash.IS_PLAYER_BEING_ARRESTED, Game.Player, false);
                    if (!dead && !busted) return;

                    DeathCount++;
                    _name = _crew.Active.FirstName;
                    _cause = dead ? "was killed" : "was arrested";
                    _phase = Phase.FadingOut;
                    _phaseStarted = Game.GameTime;
                    _abilities.Stop();
                    _switching.Cancel();
                    _dialogue.Clear();
                    Game.TimeScale = 1f;
                    Game.Player.CanControlCharacter = false;
                    GameUtils.FadeOut(_config.DeathFadeOutMs);
                    Logger.Warn(_name + " " + _cause + "; recovery " + DeathCount + " started.");
                    return;
                }

                Game.Player.CanControlCharacter = false;
                int elapsed = Game.GameTime - _phaseStarted;
                if (_phase == Phase.FadingOut && elapsed >= _config.DeathFadeOutMs + 100)
                {
                    Function.Call(Hash.SET_CAM_DEATH_FAIL_EFFECT_STATE, 0);
                    Function.Call(Hash.RESET_PLAYER_ARREST_STATE, Game.Player);
                    Function.Call(Hash.FORCE_GAME_STATE_PLAYING);
                    Game.TimeScale = 1f;
                    _phase = Phase.Holding;
                    _phaseStarted = Game.GameTime;
                }
                else if (_phase == Phase.Holding && elapsed >= _config.DeathHoldMs)
                {
                    Recover();
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Recovery failed; returning to the story character", ex);
                EmergencyReturn();
            }
        }

        private void Recover()
        {
            if (!_crew.ReviveAll())
                throw new InvalidOperationException("The active crew ped could not be revived.");

            string result;
            if (_config.RestoreCheckpointOnDeath && _missions.TryRestoreCheckpoint())
            {
                result = "Checkpoint restored.";
            }
            else
            {
                bool missionFailed = _missions.IsRunning;
                if (missionFailed) _missions.ForceFail(_name + " " + _cause + ". Restart this mission.");
                // Use the actual pre-deployment player position, not an estimated
                // crane/roof/underwater mission spawn that may have caused the death.
                var origin = _crew.RecoveryOrigin ?? _crew.DeployOrigin;
                if (!origin.HasValue) throw new InvalidOperationException("No recovery position was recorded.");
                _crew.CompanionsHoldPosition = false;
                _crew.RegroupAt(origin.Value.Position, origin.Value.Heading);
                result = missionFailed ? "Mission ended. Use the mission key to retry." : "Crew regrouped.";
            }

            Game.Player.WantedLevel = 0;
            _phase = Phase.Idle;
            RestorePresentation(_config.DeathFadeInMs);
            GameUtils.Notify("~y~" + result);
            Logger.Info("Recovery finished: " + result);
        }

        private void ApplySuppression()
        {
            // Record ownership before native calls so partial failures still unwind.
            _suppressionApplied = true;
            Function.Call(Hash.PAUSE_DEATH_ARREST_RESTART, true);
            Function.Call(Hash.IGNORE_NEXT_RESTART, true);
            Function.Call(Hash.SET_FADE_OUT_AFTER_DEATH, false);
            Function.Call(Hash.SET_FADE_OUT_AFTER_ARREST, false);
            Function.Call(Hash.SET_FADE_IN_AFTER_DEATH_ARREST, false);
        }

        public void ReleaseSuppression()
        {
            if (!_suppressionApplied) return;
            bool released = true;
            released &= Attempt(() => Function.Call(Hash.PAUSE_DEATH_ARREST_RESTART, false));
            released &= Attempt(() => Function.Call(Hash.IGNORE_NEXT_RESTART, false));
            released &= Attempt(() => Function.Call(Hash.SET_FADE_OUT_AFTER_DEATH, true));
            released &= Attempt(() => Function.Call(Hash.SET_FADE_OUT_AFTER_ARREST, true));
            released &= Attempt(() => Function.Call(Hash.SET_FADE_IN_AFTER_DEATH_ARREST, true));
            if (released) _suppressionApplied = false;
        }

        public void Cancel()
        {
            bool wasHandling = IsHandling;
            _phase = Phase.Idle;
            ReleaseSuppression();
            if (wasHandling) RestorePresentation(500);
        }

        private void EmergencyReturn()
        {
            _phase = Phase.Idle;
            Attempt(() => _missions.ForceFail("Recovery failed. Restart the mission."));
            Attempt(_abilities.Stop);
            Attempt(_switching.Cancel);
            Attempt(() => _crew.ReturnToStoryOrigin());
            Attempt(_crew.Dismiss);
            ReleaseSuppression();
            RestorePresentation(500);
        }

        private static void RestorePresentation(int fadeMs)
        {
            Attempt(() => Game.TimeScale = 1f);
            Attempt(() => Game.Player.CanControlCharacter = true);
            Attempt(() => GameUtils.FadeIn(fadeMs));
        }

        private static bool Attempt(Action action)
        {
            try { action(); return true; }
            catch (Exception ex) { Logger.Error("Recovery cleanup failed", ex); return false; }
        }
    }
}
