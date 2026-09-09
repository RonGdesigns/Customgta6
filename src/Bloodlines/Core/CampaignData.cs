using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GTA.Math;

namespace Bloodlines.Core
{
    /// <summary>One mission's entry in the bible: setting, objective, synopsis.</summary>
    public sealed class MissionInfo
    {
        public int Number { get; set; }
        public string Id { get; set; }

        /// <summary>"main" for the 70-mission campaign, "solo" for the SM expansion.</summary>
        public string Kind { get; set; }

        /// <summary>ICE / GOHAN / GUESS for a solo mission; empty for the main campaign.</summary>
        public string Owner { get; set; }

        /// <summary>Main-campaign mission number this solo slots in after; 0 for main missions.</summary>
        public int InsertAfter { get; set; }

        /// <summary>"Trio" or "Solo" — the dispatcher's play type.</summary>
        public string Type { get; set; }

        /// <summary>Mission id that must be complete before this one unlocks; empty for M01.</summary>
        public string Prerequisite { get; set; }

        /// <summary>Audio bank for this mission's cues, e.g. "audio/Act1/M01".</summary>
        public string AudioDirectory { get; set; }

        /// <summary>Optional external assembly + type name for a mission built outside this mod.</summary>
        public string Assembly { get; set; }

        public string ClassName { get; set; }

        public bool IsSolo => string.Equals(Kind, "solo", StringComparison.OrdinalIgnoreCase);
        public string Title { get; set; }
        public string Act { get; set; }
        public string Location { get; set; }
        public string Time { get; set; }
        public string Weather { get; set; }
        public string Hud { get; set; }
        public string Synopsis { get; set; }

        /// <summary>Bible clock as (hour, minute); (-1, -1) when it could not be read.</summary>
        public void ParseClock(out int hour, out int minute)
        {
            hour = -1;
            minute = -1;
            if (string.IsNullOrEmpty(Time)) return;

            var match = System.Text.RegularExpressions.Regex.Match(Time, @"(\d{1,2}):(\d{2})");
            if (!match.Success) return;

            hour = int.Parse(match.Groups[1].Value);
            minute = int.Parse(match.Groups[2].Value);
        }
    }

    /// <summary>One line of scripted dialogue, keyed by the bible's cue id.</summary>
    public sealed class DialogueCue
    {
        public string CueId { get; set; }
        public string MissionId { get; set; }
        public int Stage { get; set; }
        public string Speaker { get; set; }
        public string Direction { get; set; }
        public string Line { get; set; }
        public string Trigger { get; set; }
    }

    /// <summary>
    /// The campaign as data. Everything here is generated from the design bible by
    /// tools/parse_bible.py, so a revised bible is a re-run and a rebuild of the
    /// data files — never a code change.
    /// </summary>
    public sealed class CampaignData
    {
        private readonly Dictionary<string, MissionInfo> _missions =
            new Dictionary<string, MissionInfo>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, DialogueCue> _cues = new Dictionary<string, DialogueCue>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Vector3> _anchors = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, float> _anchorHeadings = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        public bool IsLoaded { get; private set; }

        public static CampaignData Load(string dataDirectory)
        {
            var data = new CampaignData();

            foreach (var row in DataTable.Load(Path.Combine(dataDirectory, "missions.tsv")).Rows)
            {
                var info = new MissionInfo
                {
                    Number = row.Int("number"),
                    Id = row.Text("id"),
                    Kind = string.IsNullOrEmpty(row.Text("kind")) ? "main" : row.Text("kind"),
                    Owner = row.Text("owner"),
                    InsertAfter = row.Int("insert_after"),
                    Type = row.Text("type"),
                    Prerequisite = row.Text("prerequisite"),
                    AudioDirectory = row.Text("audio_dir"),
                    Assembly = row.Text("assembly"),
                    ClassName = row.Text("class_name"),
                    Title = row.Text("title"),
                    Act = row.Text("act"),
                    Location = row.Text("location"),
                    Time = row.Text("time"),
                    Weather = row.Text("weather"),
                    Hud = row.Text("hud"),
                    Synopsis = row.Text("synopsis")
                };
                if (!string.IsNullOrEmpty(info.Id)) data._missions[info.Id] = info;
            }

            // Authored implementation descriptions override proposed bible geometry.
            // The original extracted data remains available as design source material.
            foreach (var row in DataTable.Load(Path.Combine(dataDirectory, "mission_gameplay.tsv")).Rows)
                if (data._missions.TryGetValue(row.Text("mission"), out var implemented))
                { implemented.Hud = row.Text("summary"); implemented.Synopsis = row.Text("summary"); }

            foreach (var row in DataTable.Load(Path.Combine(dataDirectory, "dialogue.tsv")).Rows)
            {
                var cue = new DialogueCue
                {
                    CueId = row.Text("cue_id"),
                    MissionId = row.Text("mission"),
                    Stage = row.Int("stage"),
                    Speaker = row.Text("speaker"),
                    Direction = row.Text("direction"),
                    Line = row.Text("line"),
                    Trigger = row.Text("trigger")
                };
                if (!string.IsNullOrEmpty(cue.CueId)) data._cues[cue.CueId] = cue;
            }

            foreach (var row in DataTable.Load(Path.Combine(dataDirectory, "scenes.tsv")).Rows)
            {
                var cue = new DialogueCue { CueId = row.Text("cue_id"), MissionId = row.Text("mission"),
                    Stage = -1, Speaker = row.Text("speaker"), Direction = row.Text("direction"),
                    Line = row.Text("line"), Trigger = row.Text("phase") };
                if (!string.IsNullOrEmpty(cue.CueId)) data._cues[cue.CueId] = cue;
            }

            foreach (var row in DataTable.Load(Path.Combine(dataDirectory, "anchors.tsv")).Rows)
            {
                string key = row.Text("key");
                if (string.IsNullOrEmpty(key)) continue;
                data._anchors[key] = new Vector3(row.Float("x"), row.Float("y"), row.Float("z"));
                data._anchorHeadings[key] = row.Float("heading");
            }

            data.IsLoaded = data._missions.Count > 0;
            if (!data.IsLoaded)
            {
                Logger.Error("No campaign data loaded from " + dataDirectory +
                             ". Copy the repo's data/ folder into scripts/Bloodlines/data/.");
            }

            return data;
        }

        /// <summary>Looks a mission up by its bible id — "M07", "SM03".</summary>
        public MissionInfo Mission(string id)
        {
            return id != null && _missions.TryGetValue(id, out var info) ? info : null;
        }

        public MissionInfo MainMission(int number)
        {
            return Mission("M" + number.ToString("00"));
        }

        public IEnumerable<MissionInfo> Missions =>
            _missions.Values.OrderBy(m => m.IsSolo).ThenBy(m => m.Number);

        public IEnumerable<MissionInfo> SoloMissions =>
            _missions.Values.Where(m => m.IsSolo).OrderBy(m => m.Number);

        public DialogueCue Cue(string cueId)
        {
            return _cues.TryGetValue(cueId, out var cue) ? cue : null;
        }

        /// <summary>Every cue for one stage of one mission, in script order.</summary>
        public IEnumerable<DialogueCue> Stage(string missionId, int stage)
        {
            return _cues.Values
                .Where(cue => string.Equals(cue.MissionId, missionId, StringComparison.OrdinalIgnoreCase)
                              && cue.Stage == stage)
                .OrderBy(cue => cue.CueId, StringComparer.Ordinal);
        }

        /// <summary>
        /// A surveyed position from the bible's coordinate index. These are the only
        /// coordinates in the project that were authored against the real map rather
        /// than estimated, so missions prefer them over LocationBook defaults.
        /// </summary>
        public bool TryAnchor(string key, out Vector3 position, out float heading)
        {
            heading = 0f;
            if (!_anchors.TryGetValue(key, out position)) return false;
            _anchorHeadings.TryGetValue(key, out heading);
            return true;
        }

        public Vector3 Anchor(string key, Vector3 fallback)
        {
            return TryAnchor(key, out var position, out _) ? position : fallback;
        }

        public float AnchorHeading(string key, float fallback)
        {
            return TryAnchor(key, out _, out float heading) ? heading : fallback;
        }
    }
}
