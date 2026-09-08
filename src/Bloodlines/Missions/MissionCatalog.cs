using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
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
        public bool IsSolo => Info.IsSolo;

        /// <summary>The character a solo mission belongs to; null for main missions.</summary>
        public CrewSlot? Owner
        {
            get
            {
                if (string.IsNullOrEmpty(Info.Owner)) return null;
                switch (Info.Owner.ToUpperInvariant())
                {
                    case "ICE": return CrewSlot.Ice;
                    case "GOHAN": return CrewSlot.Gohan;
                    case "GUESS": return CrewSlot.Guess;
                    default: return null;
                }
            }
        }

        public CampaignAct Act
        {
            get
            {
                if (IsSolo)
                {
                    return Info.InsertAfter <= 22 ? CampaignAct.BleedingTrail :
                        Info.InsertAfter <= 48 ? CampaignAct.Squeeze : CampaignAct.ScorchedEarth;
                }

                return Number <= 22 ? CampaignAct.BleedingTrail :
                    Number <= 48 ? CampaignAct.Squeeze : CampaignAct.ScorchedEarth;
            }
        }

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
    /// The campaign: 70 main missions plus the 9 solo character missions, assembled
    /// from the bible data at load time.
    ///
    /// Titles, settings, objectives and synopses are never typed into code — they
    /// come from data/missions.tsv. A slot becomes playable only when a Mission class
    /// is registered against its id in <see cref="Scripted"/>, which keeps "written"
    /// and "playable" honestly separate.
    /// </summary>
    public sealed class MissionCatalog
    {
        private static readonly Dictionary<string, Func<Mission>> Scripted =
            new Dictionary<string, Func<Mission>>(StringComparer.OrdinalIgnoreCase)
            {
                { "M01", () => new M01GhostInTheDockyard() },
                { "M02", () => new M02LooseStrands() },
                { "SM01", () => new SM01LeadAndKevlar() }
            };

        private readonly List<MissionDefinition> _main = new List<MissionDefinition>();
        private readonly List<MissionDefinition> _solo = new List<MissionDefinition>();
        private readonly List<MissionDefinition> _order = new List<MissionDefinition>();

        public MissionCatalog(CampaignData data)
        {
            for (int number = 1; number <= 70; number++)
            {
                var info = data.MainMission(number) ?? new MissionInfo
                {
                    Number = number,
                    Id = "M" + number.ToString("00"),
                    Kind = "main",
                    Title = "Mission " + number.ToString("00"),
                    Synopsis = "No bible entry loaded for this slot."
                };

                _main.Add(Build(info));
            }

            foreach (var info in data.SoloMissions) _solo.Add(Build(info));

            // Play order: the 70 in sequence, with each act's solo missions dropped in
            // at the window the expansion specifies rather than bolted on at the end.
            foreach (var mission in _main)
            {
                _order.Add(mission);
                _order.AddRange(_solo.Where(solo => solo.Info.InsertAfter == mission.Number));
            }
            _order.AddRange(_solo.Where(solo => !_order.Contains(solo)));

            Logger.Info("Catalog built: " + _main.Count + " main + " + _solo.Count + " solo, " +
                        _order.Count(m => m.IsPlayable) + " playable.");
        }

        private static MissionDefinition Build(MissionInfo info)
        {
            Scripted.TryGetValue(info.Id, out var factory);
            return new MissionDefinition(info, factory);
        }

        public IReadOnlyList<MissionDefinition> All => _order;

        public IReadOnlyList<MissionDefinition> Main => _main;

        public IReadOnlyList<MissionDefinition> Solo => _solo;

        public IEnumerable<MissionDefinition> Playable => _order.Where(m => m.IsPlayable);

        public MissionDefinition Get(string id)
        {
            return _order.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public MissionDefinition GetMain(int number)
        {
            return number >= 1 && number <= _main.Count ? _main[number - 1] : null;
        }
    }
}
