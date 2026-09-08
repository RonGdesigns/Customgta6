using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Missions.Objectives;

namespace Bloodlines.Missions
{
    /// <summary>
    /// A mission written as a list of stages of objectives rather than as a bespoke
    /// state machine.
    ///
    /// The bespoke form (see M01) is still there for set pieces that genuinely need
    /// it, but most missions are the same handful of verbs in a different order, and
    /// writing those by hand 76 more times is how a campaign this size never ships.
    /// A composed mission implements two methods: what to spawn, and the stage list.
    ///
    /// Everything else — objective ticking, the on-screen objective line, stage
    /// advance, checkpoints, dialogue per stage, switch locking, failure with a
    /// reason, teardown — is handled here once.
    /// </summary>
    public abstract class ComposedMission : Mission
    {
        private List<MissionStage> _stages;

        protected IReadOnlyList<MissionStage> Stages => _stages;

        /// <summary>Spawn the world. Return false to reject the start.</summary>
        protected abstract bool Setup();

        /// <summary>The mission, as stages of objectives.</summary>
        protected abstract IEnumerable<MissionStage> BuildStages();

        protected override bool OnStart()
        {
            if (!Setup()) return false;

            _stages = BuildStages().ToList();
            if (_stages.Count == 0)
            {
                Logger.Error(Id + " built no stages.");
                return false;
            }

            if (!Validate()) return false;

            EnterStage(0);
            return true;
        }

        protected override void OnUpdate()
        {
            var stage = CurrentStageOrNull();
            if (stage == null) return;

            foreach (var objective in stage.Objectives)
            {
                if (!objective.IsFinished) objective.Update(Ctx);
            }

            var failure = stage.FirstFailure;
            if (failure != null)
            {
                Fail(failure.FailReason ?? "Objective failed.");
                return;
            }

            var current = stage.Current;
            if (current != null) GameUtils.Subtitle("~y~" + current.Label, 500);

            if (!stage.IsComplete) return;

            ExitStage(stage);

            if (Stage + 1 >= _stages.Count)
            {
                Pass();
                return;
            }

            Advance();
            EnterStage(Stage);
        }

        /// <summary>
        /// Catches the stage that can never finish: one made only of passive
        /// objectives, which can fail but never complete. That shape is invisible to
        /// the compiler and only shows up as a mission hanging at a stage, so it is
        /// worth failing loudly at start instead.
        /// </summary>
        private bool Validate()
        {
            bool valid = true;

            for (int i = 0; i < _stages.Count; i++)
            {
                var stage = _stages[i];

                if (stage.Objectives.Count == 0)
                {
                    Logger.Error(Id + " stage " + i + " (" + stage.Name + ") has no objectives.");
                    valid = false;
                    continue;
                }

                if (stage.Objectives.All(objective => objective.IsPassive))
                {
                    Logger.Error(Id + " stage " + i + " (" + stage.Name +
                                 ") has only passive objectives and could never finish.");
                    valid = false;
                }
            }

            return valid;
        }

        private MissionStage CurrentStageOrNull()
        {
            return _stages != null && Stage >= 0 && Stage < _stages.Count ? _stages[Stage] : null;
        }

        private void EnterStage(int index)
        {
            if (index < 0 || index >= _stages.Count) return;

            var stage = _stages[index];
            Logger.Debug(Id + " stage " + index + ": " + stage.Name);

            if (stage.LockedTo.HasValue)
            {
                var protagonist = Crew.Protagonist.Of(stage.LockedTo.Value);
                Ctx.Switching.SetLocked(protagonist.FirstName + " has this one.");
            }
            else
            {
                Ctx.Switching.SetUnlocked();
            }

            stage.Setup?.Invoke(Ctx);

            foreach (var objective in stage.Objectives) objective.Enter(Ctx);

            if (stage.DialogueStage > 0) SayStage(stage.DialogueStage);

            var first = stage.Current;
            if (first != null) Objective(first.Label);
        }

        private void ExitStage(MissionStage stage)
        {
            foreach (var objective in stage.Objectives) objective.Exit(Ctx);
            stage.Teardown?.Invoke(Ctx);
        }

        /// <summary>A checkpoint restore or QA warp re-enters the stage cleanly.</summary>
        protected override void OnStageEntered(int stage)
        {
            if (_stages == null) return;
            EnterStage(stage);
        }
    }
}
