using Bloodlines.Core;
using GTA;

namespace Bloodlines.Missions
{
    /// <summary>
    /// Owns the one mission that can be running at a time, plus the pass/fail
    /// handshake with campaign progress.
    /// </summary>
    public sealed class MissionManager
    {
        private readonly MissionContext _context;
        private readonly CampaignState _state;
        private readonly MissionCatalog _catalog;

        private Mission _current;
        private MissionDefinition _pending;
        private MissionDefinition _currentDefinition;
        private bool _standalonePhase;
        public PortHeistOperation ActivePortHeist => _current as PortHeistOperation;

        public MissionManager(MissionContext context, CampaignState state, MissionCatalog catalog)
        {
            _context = context;
            _state = state;
            _catalog = catalog;
        }

        public MissionCatalog Catalog => _catalog;
        public event System.Action<string> Passed;

        /// <summary>Stage of the running mission, or -1. Used by the dev menu.</summary>
        public int CurrentStage => _current?.CurrentStage ?? -1;

        /// <summary>Dev menu: finish the running mission as a pass.</summary>
        public void ForcePass()
        {
            if (_current == null || _context.Cutscenes.IsActive) return;
            _current.Pass();
        }

        /// <summary>Dev menu: finish the running mission as a failure.</summary>
        public void ForceFail(string reason)
        {
            bool briefingOnly = _pending != null && _current == null;
            _context.Cutscenes.Stop();
            _pending = null;
            LastFailureReason = reason;
            if (_current != null) _current.Fail(reason);
            else if (briefingOnly)
            {
                // Failed during the briefing: no mission object exists to fail, but the
                // attempt was accepted and its weapon loan opened. Close it now; the
                // ordinary Update path would never see a mission to finish.
                Logger.Info("Mission failed during its briefing: " + reason);
                Finish();
            }
            RetryAvailable = _currentDefinition != null;
        }

        private MissionDefinition NormalEntry(MissionDefinition definition, bool bypassGates)
        {
            // Legacy ids and direct normal calls cannot bypass the entry gate or clock.
            if (bypassGates || definition == null || !PortHeistOperation.Contains(definition.Id)) return definition;
            foreach (var candidate in _catalog.All)
                if (string.Equals(candidate.Id, "M19", System.StringComparison.OrdinalIgnoreCase)) return candidate;
            return null;
        }

        /// <summary>
        /// Whether a start would be accepted, with no side effects at all. The host
        /// asks this before it stands the crew down or touches the world, and
        /// <see cref="Start"/> asks it again. <paramref name="reason"/> is the
        /// player-facing refusal, null when the mission can start.
        /// </summary>
        public bool CanStart(MissionDefinition definition, out string reason, bool bypassGates = false)
        {
            definition = NormalEntry(definition, bypassGates);
            reason = null;
            if (definition == null) { reason = "No mission selected."; return false; }
            if (SurveyMode.IsSurveyRunning) { reason = "Stop the survey before starting a mission."; return false; }
            if (IsRunning || _current != null) { reason = "A mission is already running. Hold Backspace to abort."; return false; }
            if (!bypassGates && !_state.PrerequisiteMet(definition)) { reason = definition.Id + " needs " + definition.Info.Prerequisite + " finished first."; return false; }
            if (!definition.IsPlayable) { reason = definition.Id + " — " + definition.Title + " is on the campaign spine but has no script yet."; return false; }
            if (!bypassGates && !_state.GateSatisfied(definition, _catalog)) { reason = _state.DescribeGate(definition, _catalog); return false; }
            return true;
        }

        public bool IsRunning => _pending != null || _context.Cutscenes.IsActive ||
            _current != null || PendingContinuation != null;

        public Crew.CrewSlot? RequiredSwitch => _current?.RequiredSwitch;
        public string CurrentObjective => _current?.CurrentObjective ?? (_pending != null ? "Watch the briefing, then follow the objective." : "No mission is running.");

        public string CurrentTitle => _current?.Title ?? _currentDefinition?.Title;

        public MissionDefinition LastAttempted => _currentDefinition;
        public bool RetryAvailable { get; private set; }
        public string LastFailureReason { get; private set; } = "";

        /// <summary>
        /// Automatic chaining for the other campaign sequences. The Port Heist does
        /// not use this table: its sections belong to one parent mission with no
        /// intermediate completions, restart points or ordinary Start/Finish calls.
        /// </summary>
        public static readonly System.Collections.Generic.Dictionary<string, string> Continuations =
            new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase) {
                { "M44", "M45" }, { "M45", "M46" }, { "M46", "M47" }, { "M47", "M48" },
                { "M63", "M64" }, { "M64", "M65" }, { "M65", "M66" }, { "M66", "M67" }, { "M67", "M68" }, { "M68", "M69" }, { "M69", "M70" } };

        /// <summary>The chapter that starts as soon as the current aftermath ends, or null.</summary>
        public MissionDefinition PendingContinuation { get; private set; }

        /// <param name="bypassGates">QA only: start any scripted mission out of order, prerequisites and story gates unmet.</param>
        public bool Start(MissionDefinition definition, bool bypassGates = false)
        {
            definition = NormalEntry(definition, bypassGates);
            if (!CanStart(definition, out string refusal, bypassGates))
            {
                if (definition != null && !definition.IsPlayable) Logger.Warn("Attempted to start unwritten mission " + definition.Id);
                GameUtils.Notify("~y~" + refusal);
                return false;
            }
            if (bypassGates && !_state.GateSatisfied(definition, _catalog)) Logger.Warn("QA bypassed a story gate: " + _state.DescribeGate(definition, _catalog));
            if (bypassGates && !_state.PrerequisiteMet(definition)) Logger.Warn("QA started " + definition.Id + " with " + definition.Info.Prerequisite + " unfinished.");

            bool retrying = RetryAvailable && _currentDefinition == definition;
            _standalonePhase = bypassGates;
            RetryAvailable = false;
            LastFailureReason = "";
            if (retrying) MissionContextCard.Show(definition.Id, recap: true, ms: 6000);
            _context.Abilities.Stop();
            _context.Checkpoints.Clear();
            _context.Dialogue.Clear();
            Game.Player.WantedLevel = 0; // Each fresh mission owns its scripted police response.

            definition.Info.ParseClock(out int hour, out int minute);
            if (hour >= 0) GameUtils.SetClock(hour, minute);
            GameUtils.SetWeather(definition.Info.Weather);
            _currentDefinition = definition;
            // Everything the crew carries from here until teardown is on loan unless
            // the campaign says otherwise; the baseline is what they owned walking in.
            _context.Crew.Arsenal?.BeginLoan(_context.Crew);
            string briefingId = definition.Id;
            string briefingTitle = !_standalonePhase && PortHeistOperation.Contains(definition.Id)
                ? PortHeistOperation.OperationTitle : definition.Title;
            if (_context.Cutscenes.Play(briefingId, "intro", briefingTitle))
            {
                _pending = definition;
                return true;
            }
            return BeginGameplay(definition);
        }

        private bool BeginGameplay(MissionDefinition definition)
        {
            Mission mission;
            try { mission = definition.Factory(); }
            catch (System.Exception ex)
            {
                Logger.Error(definition.Id + " mission factory failed", ex);
                _context.Switching.SetUnlocked();
                _context.Checkpoints.Clear();
                _context.Crew.Arsenal?.EndLoan(_context.Crew);
                RetryAvailable = true;
                GameUtils.Notify("~r~Mission could not load. Retry from the mission menu.");
                return false;
            }
            if (mission == null) { _context.Crew.Arsenal?.EndLoan(_context.Crew); GameUtils.Notify("~r~Mission script was unavailable. Retry from the mission menu."); return false; }
            if (!_standalonePhase && PortHeistOperation.IsPhase(mission))
                mission = new PortHeistOperation(definition.Id, _state);
            if (!mission.Begin(_context))
            {
                _context.Crew.Arsenal?.EndLoan(_context.Crew);
                RetryAvailable = true;
                GameUtils.Notify("~r~" + definition.Id + " failed to start. Check Bloodlines.log.");
                return false;
            }

            _current = mission;
            _currentDefinition = definition;
            // Gameplay owns the player from here. A hand-off that arrived with
            // control off (the prologue's cut to the dock: the briefing captured
            // "off" behind the fade and restored it faithfully) must not leave the
            // player standing in a mission they cannot move in.
            if (!_context.Cutscenes.IsActive && !Game.Player.CanControlCharacter)
            {
                Logger.Warn(definition.Id + ": player control was off when gameplay began; restored.");
                Game.Player.CanControlCharacter = true;
            }
            GameUtils.Notify("~b~" + (mission is PortHeistOperation ? PortHeistOperation.OperationTitle : definition.Id + " — " + definition.Title));
            return true;
        }

        public bool StartNext()
        {
            return Start(_state.NextPlayable(_catalog));
        }

        public void Retry()
        {
            if (IsRunning || _currentDefinition == null) return;
            Start(_currentDefinition);
        }

        public void Abort()
        {
            if (!IsRunning) return;
            PendingContinuation = null;
            _context.Cutscenes.Stop();
            _pending = null;
            _current?.Abort();
            RetryAvailable = _currentDefinition != null;
            GameUtils.Subtitle("~r~Mission aborted. Mission key retries from the beginning.", 3000);
            Finish();
        }

        private bool _sceneWasActive;

        public void Update()
        {
            MissionContextCard.Draw();
            if (_context.Cutscenes.IsActive) { _sceneWasActive = true; return; }
            if (_sceneWasActive)
            {
                // The scene restored the control state it found. Mission ticks resume
                // now; if that state was "off" (a ped change the same tick the scene
                // began), gameplay would otherwise resume with the player unable to move.
                _sceneWasActive = false;
                if (_current != null && _current.Status == MissionStatus.Running && _context.Cutscenes.LastRequired &&
                    _context.Cutscenes.LastOutcome != SceneOutcome.Completed && _context.Cutscenes.LastOutcome != SceneOutcome.Skipped)
                    _current.Fail("A required scene action was interrupted. Retry the mission.");
                if (_current != null || _pending != null) GameUtils.AssertPlayerControl("a scene in " + (_currentDefinition?.Id ?? _pending?.Id ?? "the mission"));
            }
            if (_pending != null)
            {
                var pending = _pending;
                _pending = null;
                if (_context.Cutscenes.LastOutcome != SceneOutcome.Completed && _context.Cutscenes.LastOutcome != SceneOutcome.Skipped)
                {
                    LastFailureReason = "The briefing was interrupted. Retry the mission.";
                    RetryAvailable = true;
                    Finish();
                    GameUtils.Notify("~y~" + LastFailureReason);
                    return;
                }
                // A skipped briefing still leaves the player knowing the target, the
                // reason, the roles and the first destination.
                if (_context.Cutscenes.LastOutcome == SceneOutcome.Skipped) MissionContextCard.Show(pending.Id, recap: false);
                BeginGameplay(pending);
                return;
            }
            if (_current == null)
            {
                if (PendingContinuation == null) return;
                var next = PendingContinuation;
                PendingContinuation = null;
                if (CanStart(next, out string why)) { Logger.Info("Operation continues: " + next.Id); Start(next); }
                else GameUtils.Notify("~y~" + why);
                return;
            }

            if (_current.Status == MissionStatus.Running)
            {
                _current.Tick();
                return;
            }

            switch (_current.Status)
            {
                case MissionStatus.Passed:
                    // Finish the last gameplay line before the aftermath takes over.
                    if (_context.Dialogue.HasPending) return;
                    if (_current is PortHeistOperation operation) operation.CommitResult(_catalog);
                    else _state.MarkComplete(_currentDefinition.Id, _catalog);
                    PendingContinuation = _current is PortHeistOperation || (_standalonePhase && PortHeistOperation.Contains(_currentDefinition.Id)) ? null : ContinuationOf(_currentDefinition);
                    if (PendingContinuation != null)
                    {
                        GameUtils.Notify("~g~CHAPTER COMPLETE~s~ — " + _currentDefinition.Title + "~n~Continuing: " + PendingContinuation.Title);
                        GameUtils.Subtitle("~g~" + _currentDefinition.Id + " complete. The operation continues.", 5000);
                    }
                    else
                    {
                        try { Passed?.Invoke(_current.Title); }
                        catch (System.Exception e) { Logger.Error("Mission passed presentation failed; completion remains committed.", e); }
                        GameUtils.Notify("~g~MISSION PASSED~s~ — " + _current.Title);
                        GameUtils.Subtitle("~g~" + (_current is PortHeistOperation ? PortHeistOperation.OperationTitle : _currentDefinition.Id) + " complete. " +
                                           _state.CompletedCount + "/" + _catalog.All.Count + ".", 6000);
                    }
                    break;

                case MissionStatus.Failed:
                    LastFailureReason = _current.FailReason ?? "Mission failed.";
                    RetryAvailable = true;
                    GameUtils.Notify("~r~MISSION FAILED~s~ — " + (_current.FailReason ?? "unknown"));
                    GameUtils.Subtitle("~r~" + _current.FailReason + "~s~  (mission key: restart from the beginning)", 6000);
                    break;
            }

            bool passed = _current.Status == MissionStatus.Passed;
            SceneBlocking outro = null;
            if (passed) { try { outro = _current.OutroBlocking(); } catch (System.Exception ex) { Logger.Error(_current.Id + " outro blocking", ex); } }
            string outroId = _current is PortHeistOperation ? "M22" : _currentDefinition.Id;
            string outcomeTitle = _current.Title;
            Finish();
            if (passed) _context.Cutscenes.Play(outroId, "outro", "Aftermath: " + outcomeTitle, null, null, outro);
        }

        private MissionDefinition ContinuationOf(MissionDefinition definition)
        {
            if (definition == null || !Continuations.TryGetValue(definition.Id, out string nextId)) return null;
            foreach (var candidate in _catalog.All)
                if (candidate.Id == nextId) return candidate.IsPlayable ? candidate : null;
            return null;
        }

        private void Finish()
        {
            _current = null;
            _context.Checkpoints.Clear();
            _context.Abilities.Stop();
            _context.Switching.SetUnlocked();
            ObjectiveMarkers.Clear();
            // Pass, fail or abort: loans go back. Completion was committed before
            // this on a pass, so the mission's own permanent rewards are kept.
            _context.Crew.Arsenal?.EndLoan(_context.Crew);
        }

        /// <summary>QA harness: complete the objective the player is on; the stage advances on the next tick with its exit effects.</summary>
        public string CompleteObjective()
        {
            if (_current == null || _context.Cutscenes.IsActive) return null;
            var label = _current.CompleteCurrentObjective();
            GameUtils.Subtitle(label == null ? "~y~No objective to complete." : "~g~QA completed: " + label, 3000);
            return label;
        }

        /// <summary>QA harness: commit a checkpoint at the current stage.</summary>
        public void CommitCheckpoint()
        {
            if (_current == null || _context.Cutscenes.IsActive) return;
            if (!_current.AllowsCheckpointCapture)
            {
                GameUtils.Subtitle("~y~The Port Heist has no checkpoints. Retry restarts the entire mission.", 4000);
                return;
            }
            _context.Checkpoints.Commit(_current.Id, _current.CurrentStage);
            GameUtils.Subtitle("~g~Checkpoint committed — stage " + _current.CurrentStage, 2500);
        }

        /// <summary>QA harness: restore the last checkpoint of the running mission.</summary>
        public void RestoreCheckpoint()
        {
            if (_current == null || _context.Cutscenes.IsActive) return;
            if (!_current.SupportsCheckpointRestore)
            {
                GameUtils.Subtitle("~y~This mission needs a full restart. Hold Backspace, then retry.", 4000);
                return;
            }
            int stage = _context.Checkpoints.Restore(_current.Id);
            if (stage >= 0) _current.JumpToStage(stage);
        }

        /// <summary>
        /// Death handling: resume the running mission from its last checkpoint. False
        /// means there was nothing to resume from — no mission, or no checkpoint for
        /// the one running — and the caller has to decide what happens instead.
        /// Unlike <see cref="RestoreCheckpoint"/> this says nothing to the player when
        /// it misses, because the caller is about to.
        /// </summary>
        public bool TryRestoreCheckpoint()
        {
            if (_current == null || !IsRunning || !_current.SupportsCheckpointRestore) return false;
            if (!_context.Checkpoints.HasCheckpointFor(_current.Id)) return false;

            int stage = _context.Checkpoints.Restore(_current.Id);
            if (stage < 0) return false;

            _current.JumpToStage(stage);
            return true;
        }

        /// <summary>QA harness: step the running mission forward or back a stage.</summary>
        public void WarpStage(int delta)
        {
            if (_current == null || _context.Cutscenes.IsActive) return;
            if (!_current.SupportsCheckpointRestore)
            {
                GameUtils.Notify("~y~Stage skipping cannot rebuild this mission. Use full retry.");
                return;
            }
            int stage = System.Math.Max(0, _current.CurrentStage + delta);
            _current.JumpToStage(stage);
            GameUtils.Subtitle("~y~Stage warp -> " + stage, 2500);
        }

        /// <summary>Called on mod teardown so an aborted session leaves no mission peds behind.</summary>
        public void Shutdown()
        {
            PendingContinuation = null;
            _context.Cutscenes.Stop();
            _pending = null;
            if (_current != null) { _current.Abort(); _current = null; }
            _context.Crew.Arsenal?.EndLoan(_context.Crew);
        }
    }
}
