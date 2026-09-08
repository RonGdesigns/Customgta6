using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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
                { "M03", () => new M03CypressFoundry() },
                { "SM01", () => new SM01LeadAndKevlar() },
                { "SM02", () => new SM02ZeroDayInjection() },
                { "SM03", () => new SM03MidnightDrift() }
            };

        private readonly List<MissionDefinition> _main = new List<MissionDefinition>();
        private readonly List<MissionDefinition> _solo = new List<MissionDefinition>();
        private readonly List<MissionDefinition> _order = new List<MissionDefinition>();

        public MissionCatalog(CampaignData data, string missionAssemblyDirectory = null)
        {
            _missionAssemblyDirectory = missionAssemblyDirectory;

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

        private readonly string _missionAssemblyDirectory;

        private MissionDefinition Build(MissionInfo info)
        {
            // Missions built into this mod resolve by id; a registry row that names a
            // class resolves by reflection instead, which is how a mission pack can be
            // added without touching the core assembly.
            if (Scripted.TryGetValue(info.Id, out var factory))
            {
                return new MissionDefinition(info, factory);
            }

            var external = ResolveExternal(info);
            return new MissionDefinition(info, external);
        }

        private Func<Mission> ResolveExternal(MissionInfo info)
        {
            if (string.IsNullOrEmpty(info.ClassName)) return null;

            try
            {
                Assembly assembly;
                if (string.IsNullOrEmpty(info.Assembly))
                {
                    assembly = typeof(MissionCatalog).Assembly;
                }
                else
                {
                    string path = Path.Combine(_missionAssemblyDirectory ?? string.Empty, info.Assembly);
                    if (!File.Exists(path))
                    {
                        Logger.Error(info.Id + ": assembly not found at " + path);
                        return null;
                    }

                    assembly = Assembly.LoadFrom(path);
                }

                var type = assembly.GetType(info.ClassName, false, true);
                if (type == null)
                {
                    Logger.Error(info.Id + ": type " + info.ClassName + " not found in " +
                                 assembly.GetName().Name);
                    return null;
                }

                if (!typeof(Mission).IsAssignableFrom(type))
                {
                    Logger.Error(info.Id + ": " + info.ClassName + " does not derive from Mission.");
                    return null;
                }

                Logger.Info(info.Id + " dispatches to " + type.FullName + " in " +
                            assembly.GetName().Name + ".");
                return () => (Mission)Activator.CreateInstance(type);
            }
            catch (Exception ex)
            {
                // A broken mission pack must never stop the rest of the campaign loading.
                Logger.Error("Could not resolve " + info.Id + " from the registry", ex);
                return null;
            }
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
