using System;
using System.Linq;
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
