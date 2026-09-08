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
        private readonly HashSet<string> _completed =
            new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

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
                    progress._completed.Add(definition.Id);
                }
            }

            Logger.Info("Campaign progress loaded: " + progress._completed.Count + "/" +
                        catalog.All.Count + " complete.");
            return progress;
        }

        public bool IsComplete(string id) => _completed.Contains(id);

        public int CompletedCount => _completed.Count;

        /// <summary>
        /// The next playable mission in campaign order — solo missions included at
        /// the point the expansion slots them into the main line.
        /// </summary>
        public MissionDefinition NextPlayable()
        {
            return _catalog.Playable.FirstOrDefault(m => !IsComplete(m.Id))
                   ?? _catalog.Playable.FirstOrDefault();
        }

        public void MarkComplete(string id)
        {
            if (!_completed.Add(id)) return;
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
                settings.SetValue("Completed", definition.Id, IsComplete(definition.Id));
            }
            settings.Save();
            Logger.Debug("Campaign progress saved to " + _path);
        }
    }
}
