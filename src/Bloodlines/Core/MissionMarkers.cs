using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Bloodlines.Missions;
using GTA;
using GTA.Math;

namespace Bloodlines.Core
{
    /// <summary>Available story jobs have persistent map icons and nearby start cylinders.</summary>
    public sealed class MissionMarkers
    {
        private readonly MissionCatalog _catalog;
        private readonly CampaignState _state;
        private readonly MissionManager _missions;
        private readonly LocationBook _locations;
        private readonly Dictionary<string, string> _keys = new Dictionary<string, string>();
        private readonly Dictionary<string, Blip> _blips = new Dictionary<string, Blip>();
        private readonly string _startKey;
        public MissionDefinition Nearby { get; private set; }

        public MissionMarkers(MissionCatalog catalog, CampaignState state, MissionManager missions,
            LocationBook locations, string dataDirectory, string startKey)
        {
            _catalog = catalog; _state = state; _missions = missions; _locations = locations; _startKey = startKey;
            foreach (var row in DataTable.Load(Path.Combine(dataDirectory, "mission_starts.tsv")).Rows)
                _keys[row.Text("mission")] = row.Text("location_key");
        }

        /// <summary>The marker a job starts from, or null for a job without one.</summary>
        public MissionLocation StartPoint(MissionDefinition mission)
        {
            if (mission == null) return null;
            string id = mission.Id.Equals("M19", StringComparison.OrdinalIgnoreCase) ? PortHeistOperation.ResolveEntry(_state, "M19") : mission.Id;
            return _keys.TryGetValue(id, out var key) ? _locations.Get(key) : null;
        }

        public void RouteNextAvailable()
        {
            // Same answer as the mission key: story first, a solo only when a gate
            // is waiting on it.
            var mission = _state.NextPlayable(_catalog);
            if (StartPoint(mission) is MissionLocation point)
            {
                GTA.Native.Function.Call(GTA.Native.Hash.SET_NEW_WAYPOINT, point.Position.X, point.Position.Y);
                string gate = _state.Progress(_catalog) == CampaignProgress.StoryGated ? " " + _state.DescribeGate(_state.NextStory(_catalog), _catalog) : "";
                GameUtils.Notify("~g~Gohan's intel: " + mission.Title + ". Start location marked." + gate); return;
            }
            GameUtils.Notify("~y~No new playable lead is available. Completed jobs remain in the mission menu.");
        }

        public void Update(bool suspended)
        {
            Nearby = null;
            if (suspended || _missions.IsRunning || SurveyMode.IsSurveyRunning || CutsceneDirector.IsSceneRunning)
            { Clear(); return; }
            var eligible = new HashSet<string>();
            var player = Game.Player.Character;
            float closest = 6f;
            foreach (var mission in _catalog.Playable)
            {
                bool operation = PortHeistOperation.Contains(mission.Id);
                if (operation && (mission.Id != "M19" || _state.IsComplete("M22"))) continue;
                if ((!operation && _state.IsComplete(mission.Id)) || !_state.PrerequisiteMet(mission)) continue;
                string markerId = operation ? PortHeistOperation.ResolveEntry(_state, "M19") : mission.Id;
                if (!_keys.TryGetValue(markerId, out var key)) continue;
                var location = _locations.All;
                MissionLocation point = null;
                foreach (var item in location) if (item.Key.Equals(key, StringComparison.OrdinalIgnoreCase)) { point = item; break; }
                if (point == null) continue;
                eligible.Add(mission.Id);
                bool solo = mission.Info.IsSolo;
                if (!_blips.TryGetValue(mission.Id, out var blip) || blip == null || !blip.Exists())
                {
                    blip = World.CreateBlip(point.Position);
                    if (blip == null) continue;
                    _blips[mission.Id] = blip;
                    blip.Sprite = BlipSprite.Standard;
                    blip.Color = !solo ? BlipColor.Yellow : mission.Info.Owner == "ICE" ? BlipColor.Blue :
                        mission.Info.Owner == "GOHAN" ? BlipColor.Green : BlipColor.Orange;
                    blip.IsShortRange = false;
                    blip.ShowRoute = false;
                    blip.Name = operation ? PortHeistOperation.OperationTitle + (markerId == "M19" ? "" : " — Resume " + PortHeistOperation.PhaseName(markerId)) :
                        mission.Id + " — " + (mission.Id == "SM03" ? "KJ: " : solo ? mission.Info.Owner + ": " : "") + mission.Title;
                }
                blip.Position = point.Position;
                if (player == null || !player.Exists() || player.IsDead) continue;
                float distance = player.Position.DistanceTo(point.Position);
                if (distance < 100f) GameUtils.DrawObjectiveMarker(point.Position,
                    solo ? Color.FromArgb(140, 230, 145, 45) : Color.FromArgb(140, 240, 205, 60), 2f);
                if (distance < closest) { closest = distance; Nearby = mission; }
            }
            foreach (var id in new List<string>(_blips.Keys))
                if (!eligible.Contains(id)) { GameUtils.SafeDelete(_blips[id]); _blips.Remove(id); }
            if (Nearby != null)
                GameUtils.Subtitle("~y~" + (PortHeistOperation.Contains(Nearby.Id) ? PortHeistOperation.OperationTitle : Nearby.Title) + "~s~ — " + _startKey + " or controller D-pad right to start", 200);
        }

        public void Clear()
        {
            foreach (var blip in _blips.Values) GameUtils.SafeDelete(blip);
            _blips.Clear(); Nearby = null;
        }
    }
}
