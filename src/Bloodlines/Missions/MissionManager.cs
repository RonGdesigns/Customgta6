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
        private readonly CampaignProgress _progress;
        private readonly MissionCatalog _catalog;

        private Mission _current;
        private MissionDefinition _currentDefinition;

        public MissionManager(MissionContext context, CampaignProgress progress, MissionCatalog catalog)
        {
            _context = context;
            _progress = progress;
            _catalog = catalog;
        }

        public MissionCatalog Catalog => _catalog;

        public bool IsRunning => _current != null && _current.Status == MissionStatus.Running;

        public string CurrentTitle => _currentDefinition?.Title;

        public MissionDefinition LastAttempted => _currentDefinition;

        public bool Start(MissionDefinition definition)
        {
            if (definition == null) return false;

            if (IsRunning)
            {
                GameUtils.Subtitle("~r~A mission is already running. Hold Backspace to abort.", 3000);
                return false;
            }

            if (!definition.IsPlayable)
            {
                GameUtils.Notify("~y~" + definition.Id + " — " + definition.Title + "~s~ is on the campaign spine but has no script yet.");
                Logger.Warn("Attempted to start unwritten mission " + definition.Id);
                return false;
            }

            _context.Checkpoints.Clear();
            _context.Dialogue.Clear();

            var mission = definition.Factory();
            if (!mission.Begin(_context))
            {
                GameUtils.Notify("~r~" + definition.Id + " failed to start. Check Bloodlines.log.");
                return false;
            }

            _current = mission;
            _currentDefinition = definition;
            GameUtils.Notify("~b~" + definition.Id + "~s~ — " + definition.Title);
            return true;
        }

        public bool StartNext()
        {
            return Start(_progress.NextPlayable());
        }

        public void Retry()
        {
            if (IsRunning || _currentDefinition == null) return;
            Start(_currentDefinition);
        }

        public void Abort()
        {
            if (!IsRunning) return;
            _current.Abort();
            GameUtils.Subtitle("~r~Mission aborted.", 3000);
            Finish();
        }

        public void Update()
        {
            if (_current == null) return;

            if (_current.Status == MissionStatus.Running)
            {
                _current.Tick();
                return;
            }

            switch (_current.Status)
            {
                case MissionStatus.Passed:
                    _progress.MarkComplete(_currentDefinition.Number);
                    GameUtils.Notify("~g~MISSION PASSED~s~ — " + _currentDefinition.Title);
                    GameUtils.Subtitle("~g~" + _currentDefinition.Id + " complete. " +
                                       _progress.CompletedCount + "/70.", 6000);
                    break;

                case MissionStatus.Failed:
                    GameUtils.Notify("~r~MISSION FAILED~s~ — " + (_current.FailReason ?? "unknown"));
                    GameUtils.Subtitle("~r~" + _current.FailReason + "~s~  (press the mission key to retry)", 6000);
                    break;
            }

            Finish();
        }

        private void Finish()
        {
            _current = null;
        }

        /// <summary>QA harness: commit a checkpoint at the current stage.</summary>
        public void CommitCheckpoint()
        {
            if (!IsRunning) return;
            _context.Checkpoints.Commit(_current.Id, _current.CurrentStage);
            GameUtils.Subtitle("~g~Checkpoint committed — stage " + _current.CurrentStage, 2500);
        }

        /// <summary>QA harness: restore the last checkpoint of the running mission.</summary>
        public void RestoreCheckpoint()
        {
            if (!IsRunning) return;
            int stage = _context.Checkpoints.Restore(_current.Id);
            if (stage >= 0) _current.JumpToStage(stage);
        }

        /// <summary>QA harness: step the running mission forward or back a stage.</summary>
        public void WarpStage(int delta)
        {
            if (!IsRunning) return;
            int stage = System.Math.Max(0, _current.CurrentStage + delta);
            _current.JumpToStage(stage);
            GameUtils.Subtitle("~y~Stage warp -> " + stage, 2500);
        }

        /// <summary>Called on mod teardown so an aborted session leaves no mission peds behind.</summary>
        public void Shutdown()
        {
            if (_current == null) return;
            _current.Abort();
            _current = null;
        }
    }
}
