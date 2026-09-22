using System;
using System.Collections.Generic;
using Bloodlines.Core;

namespace Bloodlines.Missions
{
    /// <summary>
    /// A single player-facing mission whose authored chapters are internal phases.
    /// The parent owns cleanup, the loan session and the one final award. No
    /// MissionManager.Start/Finish occurs at a live phase boundary, so there is no
    /// intermediate Mission Passed, no payout, no restaging and no midpoint save.
    ///
    /// The Port Heist was the first. This class is the part a second one does not
    /// have to write again: the phase walk, the scene guard, the two validation
    /// gates, the one pass and the reverse-order teardown. A subclass supplies its
    /// chapter list, its world, its scripts and what completing it awards.
    /// </summary>
    public abstract class ContinuousOperation : Mission
    {
        private readonly List<Mission> _phases = new List<Mission>();
        private Mission _phase;
        private OperationWorld _world;
        private int _phaseIndex;
        private int _seenScene;
        private bool _committed;
        private SceneBlocking _outro;

        /// <summary>The chapters, in play order, and which one ends the run.</summary>
        public abstract OperationSpec Operation { get; }
        /// <summary>The live world this attempt owns. Built once, at the start.</summary>
        protected abstract OperationWorld CreateWorld();
        /// <summary>The script for one chapter. Called once per phase, in order.</summary>
        protected abstract Mission CreatePhase(string phaseId);
        /// <summary>Award the operation. Called once, only after a verified final result.</summary>
        protected abstract void OnCommit(MissionCatalog catalog, OperationWorld world);

        public override string Id => Operation.EntryId;
        public override string Title => Operation.Title;
        public string PhaseId => Operation.PhaseIds[_phaseIndex];
        public Mission Phase => _phase;
        public OperationWorld World => _world;
        public override IEnumerable<GTA.Entity> Staged => _world != null ? _world.Entities : base.Staged;
        private string RestartNotice => _world?.RestartNotice ?? "Restart the entire " + Operation.Title + ".";
        // One sitting: neither the parent nor its owned sections record checkpoints.
        public override bool AllowsCheckpointCapture => false;
        public override bool SupportsCheckpointRestore => false;

        protected override bool OnStart()
        {
            if (Ctx.Operation != null)
                throw new InvalidOperationException("Another operation (" + Ctx.Operation.Label + ") is already live.");
            _world = CreateWorld();
            Ctx.Operation = _world;
            _seenScene = Ctx.Cutscenes.FinishedSequence;
            _phaseIndex = 0;
            Ctx.Handoffs.Clear();
            return BeginPhase(false);
        }

        private bool BeginPhase(bool continuing)
        {
            _world.Continuing = continuing;
            _world.PhaseId = PhaseId;
            Ctx.Switching.SetUnlocked();
            _phase = CreatePhase(PhaseId);
            if (_phase == null) return false;
            _phase.OperationOwned = true;
            _phases.Add(_phase);
            if (!_phase.Begin(Ctx)) return false;
            Logger.Info(Operation.Title + " phase entered: " + PhaseId + (continuing ? " (same live world)" : " (new whole-mission attempt)"));
            CurrentObjective = _phase.CurrentObjective;
            return true;
        }

        protected override void OnUpdate()
        {
            if (Ctx.Cutscenes.IsActive) return;
            if (Ctx.Cutscenes.FinishedSequence != _seenScene)
            {
                _seenScene = Ctx.Cutscenes.FinishedSequence;
                if (Ctx.Cutscenes.LastRequired && Ctx.Cutscenes.LastOutcome != SceneOutcome.Completed && Ctx.Cutscenes.LastOutcome != SceneOutcome.Skipped)
                { Fail("A required action did not complete. " + RestartNotice); return; }
            }
            if (!_world.ValidateActive(_phase, out string lost)) { Fail(lost); return; }
            if (_phase.Status == MissionStatus.Running)
            {
                _phase.Tick();
                CurrentObjective = _phase.CurrentObjective;
                RequiredSwitch = _phase.RequiredSwitch;
                int stage = _phaseIndex * 100 + _phase.CurrentStage;
                if (CurrentStage != stage) GoToStage(stage);
                return;
            }
            if (_phase.Status != MissionStatus.Passed)
            { Fail(_phase.FailReason ?? "The operation was interrupted. " + RestartNotice); return; }
            // The last radio line drains only before the final result; an inner join
            // continues at once and the line keeps playing over the next part.
            if (Operation.IsFinal(PhaseId) && Ctx.Dialogue.HasPending) return;
            if (!_world.ValidatePhaseEnd(_phase, out string invalid)) { Fail(invalid); return; }
            _phase.RetireOperationPhase();
            if (Operation.IsFinal(PhaseId))
            {
                _outro = _phase.OutroBlocking();
                Pass();
                return;
            }
            // Continue the live operation. No save, payout, restart point or new
            // mission announcement belongs between these connected objectives.
            _phaseIndex++;
            RequiredSwitch = null;
            if (!BeginPhase(true)) Fail("The next part could not start. " + RestartNotice);
        }

        protected override void OnPassed()
        {
            if (!Operation.IsFinal(PhaseId) || _phase.Status != MissionStatus.Passed || !_world.ValidatePhaseEnd(_phase, out _))
                throw new InvalidOperationException(Operation.Title + " has not reached its final verified result.");
        }

        /// <summary>The one award for the whole sitting, paid once and only on a pass.</summary>
        public void CommitResult(MissionCatalog catalog)
        {
            if (_committed) return;
            if (Status != MissionStatus.Passed) throw new InvalidOperationException("An unfinished operation cannot award progress.");
            OnCommit(catalog, _world);
            _committed = true;
        }

        public override SceneBlocking OutroBlocking() => _outro;
        public override string CompleteCurrentObjective() => _phase?.CompleteCurrentObjective();

        protected override void OnCleanup()
        {
            // A refused start — another operation was already live — runs this path
            // too. It must not clear the slot or the ledger of the run it bounced off.
            bool ours = _world != null && ReferenceEquals(Ctx.Operation, _world);
            try
            {
                // Reverse order: the current phase releases its holds before the
                // older phases retire. Borrowed entities are disposed exactly once.
                for (int i = _phases.Count - 1; i >= 0; i--)
                {
                    try { if (_phases[i].Status == MissionStatus.Running) _phases[i].Abort(); _phases[i].Cleanup(); }
                    catch (Exception ex) { Logger.Error(Operation.Title + " phase cleanup", ex); }
                }
                _world?.Dispose(Status == MissionStatus.Passed);
            }
            finally
            {
                if (ours) { Ctx.Handoffs.Clear(); Ctx.Operation = null; }
                _phases.Clear();
            }
        }
    }
}
