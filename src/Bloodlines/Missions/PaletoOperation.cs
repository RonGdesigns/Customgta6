using System;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Missions.Campaign;

namespace Bloodlines.Missions
{
    /// <summary>
    /// The second continuous operation: five authored chapters played as one
    /// uninterrupted mission, from the dive under the hull to the county line. Start
    /// once at M44; one Mission Passed after M48; failure restarts the whole thing.
    ///
    /// Everything about how a sitting runs belongs to <see cref="ContinuousOperation"/>.
    /// What is here is which chapters, which scripts, and what finishing pays.
    /// </summary>
    public sealed class PaletoOperation : ContinuousOperation
    {
        public static readonly OperationSpec Spec = MissionOperations.Paleto;
        public static readonly string[] PhaseIds = Spec.PhaseIds.ToArray();
        public static readonly string OperationTitle = Spec.Title;
        private readonly CampaignState _state;

        /// <param name="requestedPhase">Legacy caller hint; intentionally ignored. Every attempt starts at M44.</param>
        public PaletoOperation(string requestedPhase, CampaignState state)
        { _state = state ?? throw new ArgumentNullException(nameof(state)); }

        public override OperationSpec Operation => Spec;
        public PaletoWorld WorldState => World as PaletoWorld;
        public static bool Contains(string id) => Spec.Contains(id);

        protected override OperationWorld CreateWorld() => new PaletoWorld(Ctx);

        /// <summary>
        /// The vault in M46 opens with Bradley's card and nothing else. A run without it
        /// used to play the dive and the whole deck fight and then stop at the vault with a
        /// thrown exception, which is two chapters of a five-chapter sitting thrown away. It
        /// is checked here, before anything is staged, and refused with the reason
        /// (the September 22 audit). A real run always has it: M43 will not sign the staging
        /// off without it.
        /// </summary>
        protected override bool OnStart()
        {
            if (_state.EvidenceOf("bradleyKeycard") != EvidenceState.CopyHeld)
            {
                GameUtils.Notify("~r~" + Spec.Title + " needs Bradley's access card for the vault. Complete The General's Wire first.");
                Logger.Warn(Spec.Title + " refused to start: Bradley's card is not held.");
                return false;
            }
            return base.OnStart();
        }

        protected override Mission CreatePhase(string phaseId)
        {
            switch (phaseId)
            {
                case "M44": return new M44PaletoSubSurface();
                case "M45": return new M45PaletoBreach();
                case "M46": return new M46PaletoVault();
                case "M47": return new M47PaletoCollapse();
                default: return new M48TheRoadBackSouth();
            }
        }

        protected override void OnCommit(MissionCatalog catalog, OperationWorld world) =>
            _state.CompleteOperation(Spec, catalog, world.CargoChanges);

        public static string PhaseName(string id)
        {
            switch (id)
            {
                case "M44": return "Sub-Surface";
                case "M45": return "Breach";
                case "M46": return "Vault Crack";
                case "M47": return "Collapse";
                case "M48": return "The Road Back South";
                default: return "";
            }
        }
    }
}
