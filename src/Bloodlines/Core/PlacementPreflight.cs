using System.Collections.Generic;
using System.Linq;

namespace Bloodlines.Core
{
    /// <summary>
    /// What a mission is about to stand on, and how much of it is still guesswork.
    ///
    /// 1,061 of the campaign's 1,091 coordinates have never been verified in game — every
    /// Act 3 key without exception. They were derived from hull extents, prop clusters and
    /// road nodes read out of the archives while nobody was in the game, and most of the
    /// placement faults of the last week trace straight back to one of them. The
    /// information existed the whole time, in the <c>status</c> column, and only the
    /// offline linter ever read it.
    ///
    /// So the game says it now. When a mission begins it reports which of the points it
    /// actually asked for are still estimates, by name. A bad spawn stops being something
    /// to hunt for and becomes a line in the log naming the key to go and survey.
    ///
    /// It reports the keys the mission <b>used</b>, not the ones whose name starts with
    /// its id. A prefix scan would miss every shared point — a home, a bunker, an
    /// apartment room — which is exactly the kind of place two missions disagree about.
    /// </summary>
    public static class PlacementPreflight
    {
        /// <summary>How many keys the summary names before it stops listing them.</summary>
        public const int NamesListed = 8;

        /// <summary>
        /// The one-line summary, or null when there is nothing to say: no keys used, or
        /// every one of them surveyed. Null is a pass, and a pass is worth saying quietly
        /// rather than warning about.
        /// </summary>
        public static string Summary(LocationBook book, string missionId)
        {
            if (book == null) return null;
            var unverified = book.Unverified.ToList();
            if (unverified.Count == 0) return null;
            int used = book.Used.Count();
            var names = unverified.Take(NamesListed).Select(l => l.Key).ToList();
            string listed = string.Join(", ", names);
            if (unverified.Count > names.Count) listed += " and " + (unverified.Count - names.Count) + " more";
            return (missionId ?? "This mission") + " stands on " + used + " location " + (used == 1 ? "key" : "keys") +
                   " and " + unverified.Count + " of them " + (unverified.Count == 1 ? "has" : "have") +
                   " never been surveyed: " + listed + ".";
        }

        /// <summary>
        /// Say it, in the log and in the mission doctor. A warning rather than a refusal:
        /// an estimate is usually close enough to play, and a mission that would not start
        /// until every point was surveyed would mean no campaign at all.
        /// </summary>
        public static void Run(LocationBook book, MissionDoctor doctor, string missionId)
        {
            if (book == null) return;
            int used = book.Used.Count();
            if (used == 0) return;
            string summary = Summary(book, missionId);
            if (summary == null)
            {
                Logger.Info((missionId ?? "This mission") + ": all " + used + " location " +
                            (used == 1 ? "key has" : "keys have") + " been surveyed.");
                return;
            }
            Logger.Warn(summary + " Survey them with the free camera — dev menu, Survey coordinates — " +
                        "if anything spawns in the wrong place.");
            doctor?.Warn("placement", missionId ?? "placement", summary);
        }

        /// <summary>
        /// How much of one mission's own keys have been walked, for a to-do list. This one
        /// is by prefix on purpose: it is answering "is there survey work left in M53",
        /// which is a question about that mission's own points.
        /// </summary>
        public static void Progress(LocationBook book, string prefix, out int surveyed, out int total)
        {
            surveyed = total = 0;
            if (book == null || string.IsNullOrEmpty(prefix)) return;
            foreach (var location in book.All)
            {
                if (!location.Key.StartsWith(prefix + ".", System.StringComparison.OrdinalIgnoreCase)) continue;
                total++;
                if (location.Status != LocationStatus.Estimate) surveyed++;
            }
        }

        /// <summary>That progress as a menu subtitle: "3/12 surveyed" or "none surveyed".</summary>
        public static string ProgressLabel(LocationBook book, string prefix)
        {
            Progress(book, prefix, out int surveyed, out int total);
            if (total == 0) return "no locations";
            if (surveyed == 0) return total + " locations, none surveyed";
            if (surveyed == total) return total + " locations, all surveyed";
            return total + " locations, " + surveyed + " surveyed";
        }
    }
}
