using System;
using System.IO;
using System.Windows.Forms;
using Bloodlines.Core;
using GTA;

namespace Bloodlines
{
    /// <summary>
    /// Authoring aid, off unless [Dev] Enabled=true in the ini.
    ///
    /// The campaign's coordinates are meant to be surveyed in game, not guessed at
    /// a desk: stand where a mission beat belongs, press the capture key, and the
    /// exact position and heading are appended to Bloodlines.Captures.ini in the
    /// same key format Bloodlines.Locations.ini expects.
    /// </summary>
    public sealed class DevTools : Script
    {
        private readonly ModConfig _config;
        private readonly string _capturePath;
        private int _index;

        public DevTools()
        {
            string root = Path.Combine(BaseDirectory, "Bloodlines");
            Directory.CreateDirectory(root);

            _config = ModConfig.Load(Path.Combine(root, "Bloodlines.ini"));
            _capturePath = Path.Combine(root, "Bloodlines.Captures.ini");

            if (!_config.DevToolsEnabled) return;

            Interval = 50;
            Tick += OnTick;
            KeyDown += OnKeyDown;
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (CutsceneDirector.IsSceneRunning) return;
            if (SurveyMode.OwnsCaptureKey) return;
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            var position = player.Position;
            GTA.UI.Screen.ShowSubtitle(
                string.Format("~s~X {0:0.00}  Y {1:0.00}  Z {2:0.00}  H {3:0.0}",
                    position.X, position.Y, position.Z, player.Heading), 100);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != _config.DevCaptureKey) return;

            if (SurveyMode.OwnsCaptureKey) return;
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            var position = player.Position;
            string key = "CAPTURE_" + (++_index).ToString("00");
            string block = string.Format(
                "; captured {0}{1}{2}.X = {3:0.00}{1}{2}.Y = {4:0.00}{1}{2}.Z = {5:0.00}{1}{2}.Heading = {6:0.0}{1}",
                DateTime.Now.ToString("u"), Environment.NewLine, key,
                position.X, position.Y, position.Z, player.Heading);

            try
            {
                File.AppendAllText(_capturePath, block);
                GTA.UI.Notification.Show("~g~Captured " + key + "~s~ to Bloodlines.Captures.ini");
                Logger.Info("Captured " + key + " at " + position);
            }
            catch (IOException ex)
            {
                Logger.Error("Capture write failed", ex);
            }
        }
    }
}
