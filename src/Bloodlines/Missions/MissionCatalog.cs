using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Missions.Campaign;

namespace Bloodlines.Missions
{
    public enum CampaignAct
    {
        BleedingTrail = 1,
        Squeeze = 2,
        ScorchedEarth = 3
    }

    public sealed class MissionDefinition
    {
        public MissionDefinition(MissionInfo info, Func<Mission> factory)
        {
            Info = info;
            Factory = factory;
        }

        public MissionInfo Info { get; }
        public Func<Mission> Factory { get; }

        public int Number => Info.Number;
        public string Id => Info.Id;
        public string Title => Info.Title;
        public bool IsPlayable => Factory != null;

        public CampaignAct Act =>
            Number <= 22 ? CampaignAct.BleedingTrail :
            Number <= 48 ? CampaignAct.Squeeze :
            CampaignAct.ScorchedEarth;

        public static string ActTitle(CampaignAct act)
        {
            switch (act)
            {
                case CampaignAct.BleedingTrail: return "Act I — The Bleeding Trail";
                case CampaignAct.Squeeze: return "Act II — The Squeeze";
                default: return "Act III — Scorched Earth";
            }
        }
    }

    /// <summary>
    /// The 70-mission campaign, assembled from the bible data at load time.
    ///
    /// Titles, settings, objectives and synopses are never typed into code — they
    /// come from data/missions.tsv, generated from the bible by tools/parse_bible.py.
    /// A slot becomes playable only when a Mission class is registered against its id
    /// in <see cref="Scripted"/>, which keeps "written" and "playable" honestly separate.
    /// </summary>
    public sealed class MissionCatalog
    {
        private static readonly Dictionary<string, Func<Mission>> Scripted =
            new Dictionary<string, Func<Mission>>(StringComparer.OrdinalIgnoreCase)
            {
                { "M01", () => new M01GhostInTheDockyard() },
                { "M02", () => new M02LooseStrands() }
            };

        private readonly List<MissionDefinition> _all = new List<MissionDefinition>();

        public MissionCatalog(CampaignData data)
        {
            for (int number = 1; number <= 70; number++)
            {
                var info = data.Mission(number) ?? new MissionInfo
                {
                    Number = number,
                    Id = "M" + number.ToString("00"),
                    Title = "Mission " + number.ToString("00"),
                    Synopsis = "No bible entry loaded for this slot."
                };

                Scripted.TryGetValue(info.Id, out var factory);
                _all.Add(new MissionDefinition(info, factory));
            }

            Logger.Info("Catalog built: " + _all.Count(m => m.IsPlayable) + " playable of " + _all.Count + ".");
        }

        public IReadOnlyList<MissionDefinition> All => _all;

        public IEnumerable<MissionDefinition> Playable => _all.Where(m => m.IsPlayable);

        public MissionDefinition Get(int number)
        {
            return number >= 1 && number <= _all.Count ? _all[number - 1] : null;
        }

        public MissionDefinition Get(string id)
        {
            return _all.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));
        }
    }
}
