using System;
using Bloodlines.Abilities;
using Bloodlines.Core;
using Bloodlines.Missions;
using GTA;
using GTA.Native;
using GTA.Math;

namespace Bloodlines.Crew
{
    /// <summary>Bounded, frame-driven recovery while custom characters are deployed.</summary>
    public sealed class DeathController
    {
        private enum Phase { Idle, FadingOut, Holding, Settling, CheckingMovement }
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
        private string _recoveryResult;
        private Vector3 _movementOrigin;
        private int _movementInputMs, _movementTick;
        private bool _movementRepair;

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
                    _name = _crew.Active.Handle;
                    _cause = dead ? "was killed" : "was arrested";
                    _phase = Phase.FadingOut;
                    _phaseStarted = Game.GameTime;
                    _abilities.Stop();
                    _switching.Cancel();
                    _dialogue.Clear();
                    _crew.CompanionAI.Military.Clear();
                    _crew.CompanionAI.Life.Wanted.Set(_crew.ActiveSlot, 0);
                    Game.TimeScale = 1f;
                    Game.Player.CanControlCharacter = false;
                    GameUtils.FadeOut(_config.DeathFadeOutMs);
                    Logger.Warn(_name + " " + _cause + "; recovery " + DeathCount + " started.");
                    return;
                }

                if (_phase == Phase.CheckingMovement) { CheckMovement(); return; }
                if (_phase == Phase.Settling) { Settle(); return; }
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
            var origin = _crew.RecoveryOrigin ?? _crew.DeployOrigin;
            if (!origin.HasValue) throw new InvalidOperationException("No recovery position was recorded.");
            _crew.CompanionAI.SeparateAfterRecovery(_crew.ActiveSlot);
            if (!_crew.ReviveActiveAt(origin.Value.Position, origin.Value.Heading))
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
                // Mission cleanup may release assignments, but surviving heroes keep
                // their existing bodies, health, positions and occupied vehicles.
                result = missionFailed ? "Mission ended. Use the mission key to retry." : "Recovered as " + _name + ".";
            }

            _crew.CompanionAI.Military.Clear();
            _crew.CompanionAI.Life.Wanted.Set(_crew.ActiveSlot, 0);
            Game.Player.WantedLevel = 0;
            _recoveryResult = result;
            _phase = Phase.Settling;
            _phaseStarted = Game.GameTime;
            RestorePresentation(_config.DeathFadeInMs);
            Logger.Info("Recovery: verifying player control and destination collision.");
        }

        private void Settle()
        {
            var ped = Game.Player.Character;
            if (ped == null || !ped.Exists() || ped.IsDead) throw new InvalidOperationException("Recovery lost its living player.");
            var point = ped.Position;
            Function.Call(Hash.REQUEST_COLLISION_AT_COORD, point.X, point.Y, point.Z);
            bool collision = Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY, ped);
            ped.IsPositionFrozen = !collision;
            // Re-enable through the engine's post-death frames, before normal input resumes.
            Function.Call(Hash.SET_PLAYER_CONTROL, Game.Player, true, 128);
            int elapsed = Game.GameTime - _phaseStarted;
            if (collision && !Game.Player.IsDead && Game.Player.CanControlCharacter && elapsed >= 500)
            {
                RecoveryMobility.Restore(ped);
                _phase = Phase.CheckingMovement; _movementOrigin = ped.Position;
                _movementInputMs = 0; _movementTick = Game.GameTime; _movementRepair = false;
                GameUtils.Notify("~y~Move a few steps to finish recovering.");
                Logger.Info("Recovery: control flags restored; awaiting actual walking before allowing switches.");
            }
            else if (elapsed > 5000)
                throw new InvalidOperationException("Recovery did not settle: playerDead=" + Game.Player.IsDead + ", control=" + Game.Player.CanControlCharacter + ", collision=" + collision);
        }

        private void CheckMovement()
        {
            var ped = Game.Player.Character;
            if (ped == null || !ped.Exists() || ped.IsDead || Game.Player.IsDead)
                throw new InvalidOperationException("Player became invalid during recovery movement check.");
            int elapsed = Math.Min(100, Math.Max(0, Game.GameTime - _movementTick)); _movementTick = Game.GameTime;
            float input = Math.Max(Math.Abs(Function.Call<float>(Hash.GET_DISABLED_CONTROL_NORMAL, 0, 30)),
                Math.Abs(Function.Call<float>(Hash.GET_DISABLED_CONTROL_NORMAL, 0, 31)));
            if (input < .2f || Game.IsPaused) return;
            if (!ped.IsPositionFrozen && Game.Player.CanControlCharacter &&
                new Vector3(ped.Position.X, ped.Position.Y, 0f).DistanceTo(new Vector3(_movementOrigin.X, _movementOrigin.Y, 0f)) >= .5f)
            {
                _phase = Phase.Idle;
                GameUtils.Notify("~y~" + _recoveryResult);
                Logger.Info("Recovery finished: actual movement verified, switches unlocked. " + _recoveryResult);
                return;
            }
            _movementInputMs += elapsed;
            if (!_movementRepair && _movementInputMs >= 1500)
            {
                _movementRepair = true; RestorePresentation(0);
                Logger.Warn("Recovery: walking input without movement; retrying control restoration once.");
            }
            if (_movementInputMs >= 4000)
                throw new InvalidOperationException("Walking remained blocked after control restoration; returning to the story character.");
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
            Attempt(() => World.RenderingCamera = null);
            Attempt(() => Function.Call(Hash.CLEAR_FOCUS));
            Attempt(() => Function.Call(Hash.RESET_PLAYER_ARREST_STATE, Game.Player));
            Attempt(() => Function.Call(Hash.FORCE_GAME_STATE_PLAYING));
            Attempt(() => RecoveryMobility.Restore(Game.Player.Character));
            // Re-enable control after death as well as clearing the old NPC task.
            Attempt(() => Function.Call(Hash.SET_PLAYER_CONTROL, Game.Player, true, 4 | 128));
            Attempt(() => GameUtils.FadeIn(fadeMs));
        }

        private static bool Attempt(Action action)
        {
            try { action(); return true; }
            catch (Exception ex) { Logger.Error("Recovery cleanup failed", ex); return false; }
        }
    }
}
