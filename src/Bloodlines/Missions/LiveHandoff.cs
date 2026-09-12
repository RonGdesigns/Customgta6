using System;
using System.Collections.Generic;
using Bloodlines.Core;
using GTA;

namespace Bloodlines.Missions
{
    /// <summary>
    /// A handoff's steps run live: no camera, no control lock, no cut. Inside the one
    /// continuous Port Heist the crew boards the launch, walks to the Granger and
    /// arrives on the beach under AI, in the order the scene would have shown, while
    /// the player keeps playing (Ron, September 11: one seamless mission, the action
    /// never stopped). A step whose actor is the player's own ped is left to the
    /// player; the objective that owns the handoff says what to do. A step that runs
    /// past its timeout is finished the way a skipped scene finishes it, so the world
    /// ends in the state the scene would have left.
    /// </summary>
    public sealed class LiveHandoff
    {
        private readonly List<SceneStep> _steps = new List<SceneStep>();
        private readonly string _name;
        private int _index;

        public LiveHandoff(string name) { _name = name; }

        public bool Started { get; private set; }
        public bool Canceled { get; private set; }
        public bool Failed { get; private set; }
        public bool IsFinished => Started && _index >= _steps.Count;
        public SceneStep Current => _index < _steps.Count ? _steps[_index] : null;
        public int StepCount => _steps.Count;

        public LiveHandoff Then(SceneStep step)
        {
            if (step != null) _steps.Add(step);
            return this;
        }

        /// <summary>
        /// Advances as far as it can this tick: a step that is left to the player,
        /// already in its end state, or out of time is passed at once, so a
        /// boarding does not idle a frame between steps; it returns at the first
        /// step that is still being done.
        /// </summary>
        public void Update()
        {
            Started = true;
            for (int guard = 0; guard < 32; guard++)
            {
                var step = Current;
                if (step == null || Canceled) return;
                try
                {
                    var player = Game.Player.Character;
                    if (step.Actor != null && player != null && step.Actor == player)
                    {
                        // The player is this actor now: the part is theirs, not a task.
                        if (step.HasStarted) step.Cancel();
                        Logger.Debug(_name + ": " + step.GetType().Name + " left to the player.");
                        _index++;
                        continue;
                    }
                    if (!step.HasStarted)
                    {
                        step.Start();
                        if (step.Failed) { Fail(step); return; }
                    }
                    if (step.Failed) { Fail(step); return; }
                    bool done = step.IsComplete;
                    if (step.Failed) { Fail(step); return; }
                    if (!done && !step.TimedOut) return;
                    if (!done) { Logger.Warn(_name + ": " + step.GetType().Name + " ran out of time and was finished directly."); step.Finish(); }
                    if (step.Failed) { Fail(step); return; }
                    _index++;
                }
                catch (Exception ex) { Logger.Error(_name + ": live handoff step failed", ex); Cancel(); return; }
            }
        }

        private void Fail(SceneStep step)
        {
            Failed = true;
            Logger.Error(_name + ": " + step.GetType().Name + " could not reach its end state.");
            Cancel();
        }

        /// <summary>Stop without finishing: the running step stands down, nothing after it runs.</summary>
        public void Cancel()
        {
            var step = Current;
            if (step != null)
            {
                try { step.Cancel(); }
                catch (Exception ex) { Logger.Error(_name + ": cancel failed", ex); }
            }
            Canceled = true;
            _index = _steps.Count;
        }
    }
}
