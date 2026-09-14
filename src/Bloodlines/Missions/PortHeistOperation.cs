using System;
using System.Linq;
using Bloodlines.Missions.Campaign;

namespace Bloodlines.Missions
{
    /// <summary>
    /// The first continuous operation: four existing scripts played as one mission.
    /// Everything about how a sitting runs lives in <see cref="ContinuousOperation"/>;
    /// what is left here is which chapters, which scripts and what finishing pays.
    /// </summary>
    public sealed class PortHeistOperation : ContinuousOperation
    {
        /// <summary>One source of truth for the chapters and which one ends the run.</summary>
        public static readonly OperationSpec Spec = MissionOperations.PortHeist;
        public static readonly string[] PhaseIds = Spec.PhaseIds.ToArray();
        public static readonly string OperationTitle = Spec.Title;
        private readonly CampaignState _state;

        /// <param name="requestedPhase">Legacy caller hint; intentionally ignored. Every attempt starts at M19.</param>
        public PortHeistOperation(string requestedPhase, CampaignState state)
        { _state = state ?? throw new ArgumentNullException(nameof(state)); }

        public override OperationSpec Operation => Spec;
        public PortHeistWorld WorldState => World as PortHeistWorld;
        public static bool Contains(string id) => Spec.Contains(id);

        /// <summary>
        /// Compatibility resolver: an old bookmark or section request never skips the
        /// underwater beginning. Standalone section starts exist only in explicit QA.
        /// </summary>
        public static string ResolveEntry(CampaignState state, string requested) => Spec.EntryId;

        protected override OperationWorld CreateWorld() => new PortHeistWorld(Ctx);

        protected override Mission CreatePhase(string phaseId)
        {
            switch (phaseId)
            {
                case "M19": return new M19UnderwaterBreach();
                case "M20": return new M20SkyHook();
                case "M21": return new M21OpenWater();
                default: return new M22ScorchedBay();
            }
        }

        protected override void OnCommit(MissionCatalog catalog, OperationWorld world) =>
            _state.CompletePortHeist(catalog, world.CargoChanges);

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
    }
}
