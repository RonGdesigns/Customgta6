using System;
using System.Collections.Generic;
using System.Linq;

namespace Bloodlines.Missions
{
    /// <summary>
    /// A multi-part mission the player experiences as one sitting: several authored
    /// chapters that share one entry, one world, one loan and one result.
    ///
    /// This exists because the first one, the Port Heist, spelled its last chapter's
    /// id out in eight places across the campaign state, the composed-mission
    /// dialogue gate and the mission manager's outro. A second operation built by
    /// copying that would keep the first one's finality and misreport rewards and
    /// replays rather than merely losing a radio line. Adding one is a row here.
    /// </summary>
    public sealed class OperationSpec
    {
        public string Title { get; }
        /// <summary>Chapter ids in play order. The first is the only entry; the last is the result.</summary>
        public IReadOnlyList<string> PhaseIds { get; }
        public string EntryId => PhaseIds[0];
        /// <summary>The chapter whose completion means the whole operation is done.</summary>
        public string FinalId => PhaseIds[PhaseIds.Count - 1];

        public OperationSpec(string title, params string[] phaseIds)
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("An operation needs a title.", nameof(title));
            if (phaseIds == null || phaseIds.Length < 2)
                throw new ArgumentException("An operation runs at least two chapters as one mission.", nameof(phaseIds));
            if (phaseIds.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Chapter ids cannot be blank.", nameof(phaseIds));
            if (phaseIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() != phaseIds.Length)
                throw new ArgumentException("A chapter cannot appear twice in one operation.", nameof(phaseIds));
            Title = title;
            PhaseIds = Array.AsReadOnly((string[])phaseIds.Clone());
        }

        public bool Contains(string id) => id != null && PhaseIds.Contains(id, StringComparer.OrdinalIgnoreCase);
        public bool IsFinal(string id) => string.Equals(id, FinalId, StringComparison.OrdinalIgnoreCase);
        /// <summary>A chapter that is part of the run but not the end of it.</summary>
        public bool IsInner(string id) => Contains(id) && !IsFinal(id);
        public int IndexOf(string id) =>
            PhaseIds.ToList().FindIndex(p => string.Equals(p, id, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The operations the campaign knows about. One row per continuous mission.</summary>
    public static class MissionOperations
    {
        public static readonly OperationSpec PortHeist =
            new OperationSpec("The Port Heist", "M19", "M20", "M21", "M22");

        private static readonly OperationSpec[] Registered = { PortHeist };
        public static IReadOnlyList<OperationSpec> All => Array.AsReadOnly(Registered);

        /// <summary>The operation this chapter belongs to, or null for an ordinary mission.</summary>
        public static OperationSpec Owning(string missionId) =>
            missionId == null ? null : Registered.FirstOrDefault(o => o.Contains(missionId));

        /// <summary>True when the id is a chapter of an operation but not the one that ends it.</summary>
        public static bool IsInnerPhase(string missionId) => Owning(missionId)?.IsInner(missionId) ?? false;

        /// <summary>
        /// The id whose completion represents this mission. An operation chapter maps
        /// to its operation's last chapter; anything else is itself. Use this wherever
        /// a durable record, a payout or a replay check is keyed by mission.
        /// </summary>
        public static string ResultIdFor(string missionId) => Owning(missionId)?.FinalId ?? missionId;
    }
}
