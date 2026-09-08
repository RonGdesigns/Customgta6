using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using GTA;

namespace Bloodlines.Missions
{
    /// <summary>
    /// Campaign completion, kept in its own ini beside the script. Deliberately not
    /// written into the game's own save: a mod that touches save files can cost
    /// someone a story playthrough, and a flat text file is trivially recoverable.
    /// </summary>
    public sealed class CampaignProgress
    {
        private readonly string _path;
        private readonly MissionCatalog _catalog;
        private readonly HashSet<int> _completed = new HashSet<int>();

        private CampaignProgress(string path, MissionCatalog catalog)
        {
            _path = path;
            _catalog = catalog;
        }

        public static CampaignProgress Load(string path, MissionCatalog catalog)
        {
            var progress = new CampaignProgress(path, catalog);
            var settings = ScriptSettings.Load(path);

            foreach (var definition in catalog.All)
            {
                if (settings.GetValue<bool>("Completed", definition.Id, false))
                {
                    progress._completed.Add(definition.Number);
                }
            }

            Logger.Info("Campaign progress loaded: " + progress._completed.Count + "/70 complete.");
            return progress;
        }

        public bool IsComplete(int number) => _completed.Contains(number);

        public int CompletedCount => _completed.Count;

        /// <summary>Lowest-numbered mission that is playable and not yet finished.</summary>
        public MissionDefinition NextPlayable()
        {
            return _catalog.Playable.FirstOrDefault(m => !IsComplete(m.Number))
                   ?? _catalog.Playable.FirstOrDefault();
        }

        public void MarkComplete(int number)
        {
            if (!_completed.Add(number)) return;
            Save();
        }

        public void Reset()
        {
            _completed.Clear();
            Save();
        }

        private void Save()
        {
            var settings = ScriptSettings.Load(_path);
            foreach (var definition in _catalog.All)
            {
                settings.SetValue("Completed", definition.Id, IsComplete(definition.Number));
            }
            settings.Save();
            Logger.Debug("Campaign progress saved to " + _path);
        }
    }
}
