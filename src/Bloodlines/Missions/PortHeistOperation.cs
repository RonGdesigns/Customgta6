using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Missions.Campaign;
using GTA;

namespace Bloodlines.Missions
{
    /// <summary>
    /// A single player-facing mission whose four existing scripts are internal
    /// phases. The parent owns cleanup, the loan session and the one final award.
    /// No MissionManager.Start/Finish occurs at a live phase boundary.
    /// </summary>
    public sealed class PortHeistOperation : Mission
    {
        public static readonly string[] PhaseIds = { "M19", "M20", "M21", "M22" };
        public const string OperationTitle = "The Port Heist";
        private readonly CampaignState _state;
        private readonly List<Mission> _phases = new List<Mission>();
        private Mission _phase;
        private PortHeistWorld _world;
        private int _phaseIndex;
        private int _seenScene;
        private bool _committed;
        private SceneBlocking _outro;

        /// <param name="requestedPhase">Legacy caller hint; intentionally ignored. Every attempt starts at M19.</param>
        public PortHeistOperation(string requestedPhase, CampaignState state)
        { _state = state ?? throw new ArgumentNullException(nameof(state)); }
        public override string Id => PhaseIds[0];
        public override string Title => OperationTitle;
        public string PhaseId => PhaseIds[_phaseIndex];
        public Mission Phase => _phase;
        public PortHeistWorld WorldState => _world;
        // One sitting: neither the parent nor its owned sections record checkpoints.
        public override bool AllowsCheckpointCapture => false;
        public override bool SupportsCheckpointRestore => false;
        public static bool Contains(string id) => PhaseIds.Contains(id, StringComparer.OrdinalIgnoreCase);
        public static bool IsPhase(Mission mission) => mission is M19UnderwaterBreach || mission is M20SkyHook || mission is M21OpenWater || mission is M22ScorchedBay;

        /// <summary>
        /// Compatibility resolver: an old bookmark or section request never skips the
        /// underwater beginning. Standalone section starts exist only in explicit QA.
        /// </summary>
        public static string ResolveEntry(CampaignState state, string requested) => "M19";

        protected override bool OnStart()
        {
            if (Ctx.PortHeist != null) throw new InvalidOperationException("Another Port Heist world is already active.");
            _world = new PortHeistWorld(Ctx);
            Ctx.PortHeist = _world;
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
            switch (PhaseId)
            {
                case "M19": _phase = new M19UnderwaterBreach(); break;
                case "M20": _phase = new M20SkyHook(); break;
                case "M21": _phase = new M21OpenWater(); break;
                default: _phase = new M22ScorchedBay(); break;
            }
            _phase.OperationOwned = true;
            _phases.Add(_phase);
            if (!_phase.Begin(Ctx)) return false;
            Logger.Info("Port Heist phase entered: " + PhaseId + (continuing ? " (same live world)" : " (new whole-mission attempt)"));
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
                { Fail("A required heist action did not complete. Restart the entire Port Heist."); return; }
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
            { Fail(_phase.FailReason ?? "The heist was interrupted. Restart the entire Port Heist."); return; }
            if (Ctx.Dialogue.HasPending) return;
            if (!_world.ValidatePhaseEnd(_phase, out string invalid)) { Fail(invalid); return; }
            _phase.RetireOperationPhase();
            if (_phaseIndex == PhaseIds.Length - 1)
            {
                _outro = _phase.OutroBlocking();
                Pass();
                return;
            }
            // Continue the live operation. No save, payout, restart point or new
            // mission announcement belongs between these connected objectives.
            _phaseIndex++;
            RequiredSwitch = null;
            if (!BeginPhase(true)) Fail("The next part could not start. Restart the entire Port Heist.");
        }

        public static string PhaseName(string id)
        {
            switch (id)
            {
                case "M19": return "Underwater Breach";
                case "M20": return "Sky Hook";
                case "M21": return "Open Water";
                case "M22": return "Scorched Bay";
                default: return "";
            }
        }

        protected override void OnPassed()
        {
            if (PhaseId != "M22" || _phase.Status != MissionStatus.Passed || !_world.ValidatePhaseEnd(_phase, out _))
                throw new InvalidOperationException("The Port Heist has not reached its final verified result.");
        }

        public void CommitResult(MissionCatalog catalog)
        {
            if (_committed) return;
            if (Status != MissionStatus.Passed) throw new InvalidOperationException("An unfinished operation cannot award progress.");
            _state.CompletePortHeist(catalog, _world.CargoChanges);
            _committed = true;
        }

        public override SceneBlocking OutroBlocking() => _outro;
        public override string CompleteCurrentObjective() => _phase?.CompleteCurrentObjective();

        protected override void OnCleanup()
        {
            try
            {
                // Reverse order: the current phase releases its holds before the
                // older phases retire. Borrowed entities are disposed exactly once.
                for (int i = _phases.Count - 1; i >= 0; i--)
                {
                    try { if (_phases[i].Status == MissionStatus.Running) _phases[i].Abort(); _phases[i].Cleanup(); }
                    catch (Exception ex) { Logger.Error("Port Heist phase cleanup", ex); }
                }
                _world?.Dispose(Status == MissionStatus.Passed);
            }
            finally
            {
                Ctx.Handoffs.Clear();
                Ctx.PortHeist = null;
                _phases.Clear();
            }
        }
    }
}
