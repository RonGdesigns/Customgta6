using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.UI;

namespace Bloodlines.Core
{
    /// <summary>
    /// Guided coordinate survey.
    ///
    /// Almost every position in this campaign is an estimate, and no mission's pacing
    /// can be judged until they are real. Doing that by hand — fly there, note the
    /// numbers, alt-tab, paste into an ini — is slow enough that it does not get done.
    ///
    /// So: the survey walks the list. It teleports you to the current estimate, names
    /// the key and what it is for, and one key captures where you are standing and
    /// moves to the next. At the end it writes a complete Bloodlines.Locations.ini
    /// you can drop straight in, and the same block into the log.
    /// </summary>
    public sealed class SurveyMode
    {
        private readonly LocationBook _book;
        private readonly string _outputPath;
        private readonly List<MissionLocation> _queue = new List<MissionLocation>();
        private readonly HashSet<string> _captured = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private int _index;

        public SurveyMode(LocationBook book, string outputPath)
        {
            _book = book;
            _outputPath = outputPath;
        }

        public bool IsActive { get; private set; }

        public MissionLocation Current =>
            IsActive && _index >= 0 && _index < _queue.Count ? _queue[_index] : null;

        /// <summary>Starts a survey over every location, or just one mission's keys.</summary>
        public void Start(string missionPrefix = null)
        {
            _queue.Clear();
            _captured.Clear();
            _index = 0;

            var candidates = _book.All
                .Where(location => missionPrefix == null ||
                                   location.Key.StartsWith(missionPrefix + ".", StringComparison.OrdinalIgnoreCase))
                // Estimates first: they are the ones that need a human standing on them.
                .OrderBy(location => location.Status == LocationStatus.Surveyed ||
                                     location.Status == LocationStatus.Bible)
                .ThenBy(location => location.Key, StringComparer.Ordinal);

            _queue.AddRange(candidates);

            if (_queue.Count == 0)
            {
                GameUtils.Subtitle("~r~Nothing to survey.", 3000);
                return;
            }

            IsActive = true;
            Logger.Info("Survey started over " + _queue.Count + " locations.");
            GoTo(0);
        }

        public void Stop()
        {
            if (!IsActive) return;

            IsActive = false;
            Write();
            GameUtils.Notify("~g~Survey ended.~s~ " + _captured.Count + " captured — written to " +
                             Path.GetFileName(_outputPath) + ".");
        }

        /// <summary>Captures the player's current position for the current key.</summary>
        public void Capture()
        {
            var location = Current;
            if (location == null) return;

            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            _book.Record(location.Key, player.Position, player.Heading);
            _captured.Add(location.Key);
            Logger.Info("Surveyed " + location.Key + " = " + player.Position + " heading " + player.Heading);
            GameUtils.Subtitle("~g~" + location.Key + " captured.", 2000);

            // Writing after every capture means a crash costs one position, not a session.
            Write();
            Next();
        }

        public void Next()
        {
            if (!IsActive) return;

            if (_index + 1 >= _queue.Count)
            {
                GameUtils.Subtitle("~y~That was the last one.", 3000);
                Stop();
                return;
            }

            GoTo(_index + 1);
        }

        public void Previous()
        {
            if (IsActive && _index > 0) GoTo(_index - 1);
        }

        /// <summary>Skips without capturing — for keys that are fine as they are.</summary>
        public void Skip()
        {
            Next();
        }

        private void GoTo(int index)
        {
            _index = index;
            var location = Current;
            if (location == null) return;

            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            // Put the player just above the estimate: falling a metre onto the spot is
            // better than spawning inside whatever is already there.
            var target = location.Position + new Vector3(0f, 0f, 1.5f);

            if (player.IsInVehicle())
            {
                player.CurrentVehicle.Position = target;
                player.CurrentVehicle.Heading = location.Heading;
            }
            else
            {
                player.Position = target;
                player.Heading = location.Heading;
            }

            GameUtils.Notify("~b~" + location.Key + "~s~  (" + (_index + 1) + "/" + _queue.Count + ")~n~" +
                             location.DistrictHint + " · " + location.Kind + " · " + location.Status);
        }

        /// <summary>Per-frame HUD while the survey is running.</summary>
        public void Update()
        {
            var location = Current;
            if (!IsActive || location == null) return;

            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            GameUtils.DrawObjectiveMarker(location.Position, Color.FromArgb(120, 232, 168, 56), 1.2f);

            float drift = player.Position.DistanceTo(location.Position);
            string status = location.Status == LocationStatus.Surveyed ? "~g~surveyed" : "~y~" + location.Status;

            new ContainerElement(new PointF(40f, 500f), new SizeF(400f, 54f),
                Color.FromArgb(225, 18, 20, 24)).Draw();
            new TextElement(location.Key + "   (" + (_index + 1) + "/" + _queue.Count + ")",
                new PointF(50f, 506f), 0.32f, Color.FromArgb(235, 232, 168, 56)).Draw();
            new TextElement(status + "~s~ · " + (int)drift + "m from the estimate · " +
                            _captured.Count + " captured this session",
                new PointF(50f, 530f), 0.26f, Color.FromArgb(215, 200, 204, 210)).Draw();
        }

        /// <summary>
        /// Writes every location as a complete ini. Surveyed values are written for
        /// real; the rest are commented, so the file is a checklist of what is left.
        /// </summary>
        public void Write()
        {
            var lines = new List<string>
            {
                "; Bloodlines campaign coordinates.",
                "; Written by the in-game survey on " + DateTime.Now.ToString("u") + ".",
                ";",
                "; Uncommented entries have been stood on and captured. Commented ones are",
                "; still estimates from data/locations.tsv — survey them and they become real.",
                "",
                "[Positions]"
            };

            foreach (var location in _book.All.OrderBy(l => l.Key, StringComparer.Ordinal))
            {
                bool real = location.Status == LocationStatus.Surveyed || location.Status == LocationStatus.Bible;
                string prefix = real ? "" : "; ";

                lines.Add(prefix + location.Key + ".X = " + location.Position.X.ToString("0.00"));
                lines.Add(prefix + location.Key + ".Y = " + location.Position.Y.ToString("0.00"));
                lines.Add(prefix + location.Key + ".Z = " + location.Position.Z.ToString("0.00"));
            }

            lines.Add("");
            lines.Add("[Headings]");

            foreach (var location in _book.All.OrderBy(l => l.Key, StringComparer.Ordinal))
            {
                bool real = location.Status == LocationStatus.Surveyed || location.Status == LocationStatus.Bible;
                lines.Add((real ? "" : "; ") + location.Key + " = " + location.Heading.ToString("0.0"));
            }

            try
            {
                File.WriteAllLines(_outputPath, lines);
            }
            catch (IOException ex)
            {
                Logger.Error("Could not write the survey output", ex);
            }
        }
    }
}
