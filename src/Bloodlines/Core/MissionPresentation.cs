using System;
using System.Drawing;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>Presentation only: never owns mission success, controls, the camera, or reward state.</summary>
    public sealed class MissionPresentation
    {
        private readonly ModConfig _config;
        private string _title;
        private int _shown, _last, _requestAt = -1, _nextPrepare;
        private bool _playing, _attempted;
        private string _ownedStart, _ownedStop;
        public bool BannerQueued => _title != null;
        public bool ScorePlaying => _playing;
        public MissionPresentation(ModConfig config) { _config = config ?? throw new ArgumentNullException(nameof(config)); }

        public static BlipSprite StartSprite(bool solo, bool heist) =>
            (BlipSprite)(heist ? 428 : solo ? 76 : 381); // stock H, S, B glyphs

        public void QueuePassed(string title)
        {
            _title = string.IsNullOrWhiteSpace(title) ? "Bloodlines" : title.Substring(0, Math.Min(80, title.Length));
            _shown = 0; _last = Game.GameTime;
            StopScore();
        }

        public void Update(bool missionGameplay, bool blocked)
        {
            int now = Game.GameTime;
            int dt = Math.Max(0, Math.Min(250, now - _last));
            _last = now;
            var player = Game.Player.Character;
            bool alive = player != null && player.Exists() && !player.IsDead;
            bool paused = Function.Call<bool>(Hash.IS_PAUSE_MENU_ACTIVE);
            bool score = _config.MissionScoreEnabled && missionGameplay && alive && !player.IsInVehicle() && !blocked && !paused;
            UpdateScore(score, now);
            // Aftermath and pause do not consume the banner. A new mission does not
            // inherit the preceding mission's success panel over its opening.
            if (missionGameplay) { _title = null; return; }
            if (_title == null || blocked || paused || !alive) return;
            _shown += dt;
            if (_shown >= 5500) { _title = null; return; }
            int alpha = (int)(220f * Math.Min(1f, Math.Min(_shown / 300f, (5500 - _shown) / 500f)));
            new GTA.UI.TextElement("MISSION PASSED", new PointF(640f, 252f), 1.1f,
                Color.FromArgb(alpha, 240, 192, 70)) { Alignment = GTA.UI.Alignment.Center, Font = GTA.UI.Font.Pricedown }.Draw();
            new GTA.UI.TextElement(_title, new PointF(640f, 345f), .43f,
                Color.FromArgb(alpha, 255, 255, 255)) { Alignment = GTA.UI.Alignment.Center }.Draw();
        }

        private void UpdateScore(bool wanted, int now)
        {
            if (!wanted) { StopScore(); return; }
            if (_playing || _attempted) return;
            if (_requestAt < 0)
            {
                _requestAt = now;
                _ownedStart = _config.MissionScoreEvent;
                _ownedStop = _config.MissionScoreStopEvent;
                if (string.IsNullOrWhiteSpace(_ownedStart) || string.IsNullOrWhiteSpace(_ownedStop))
                { _attempted = true; return; }
            }
            // Debounce on-foot entry and bound requests: a missing installed cue must
            // never throw every frame or become a mission blocker.
            if (now - _requestAt < 600 || now < _nextPrepare) return;
            _nextPrepare = now + 250;
            try
            {
                if (Function.Call<bool>(Hash.PREPARE_MUSIC_EVENT, _ownedStart) &&
                    Function.Call<bool>(Hash.TRIGGER_MUSIC_EVENT, _ownedStart))
                { _playing = true; Logger.Info("Mission score requested: " + _ownedStart + ". Audible playback requires live validation."); }
                else if (now - _requestAt > 5000)
                { _attempted = true; Logger.Warn("Mission score unavailable: " + _ownedStart + "; continuing without music."); }
            }
            catch (Exception e) { _attempted = true; Logger.Error("Mission score unavailable; gameplay continues.", e); }
        }

        private void StopScore()
        {
            if (_ownedStart != null)
            {
                try
                {
                    if (_playing && !string.IsNullOrWhiteSpace(_ownedStop)) Function.Call<bool>(Hash.TRIGGER_MUSIC_EVENT, _ownedStop);
                    Function.Call<bool>(Hash.CANCEL_MUSIC_EVENT, _ownedStart);
                }
                catch (Exception e) { Logger.Error("Mission score cleanup failed.", e); }
            }
            _playing = _attempted = false;
            _requestAt = -1; _nextPrepare = 0; _ownedStart = _ownedStop = null;
        }
        public void Stop() { StopScore(); _title = null; _shown = 0; _last = Game.GameTime; }
    }
}
