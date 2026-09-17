using System;
using System.Collections.Generic;
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
        private MissionTally.Result _result;
        private string _cardId, _cardTitle, _cardAct, _cardTarget, _cardRoles;
        private int _cardShown;
        private int _shown, _last, _requestAt = -1, _nextPrepare;
        /// <summary>How long the passed panel stays up. Longer than the old title alone: there are numbers to read now.</summary>
        public const int PassedMs = 9000;
        /// <summary>And the title card at the start of gameplay.</summary>
        public const int CardMs = 5000;
        private bool _playing, _attempted;
        private string _ownedStart, _ownedStop;
        public bool BannerQueued => _title != null;
        public bool ScorePlaying => _playing;
        public MissionPresentation(ModConfig config) { _config = config ?? throw new ArgumentNullException(nameof(config)); }

        public static BlipSprite StartSprite(bool solo, bool heist) =>
            (BlipSprite)(heist ? 428 : solo ? 76 : 381); // stock H, S, B glyphs

        public void QueuePassed(string title) => QueuePassed(title, null);

        /// <summary>The passed panel: the title, and under it what the attempt came to.</summary>
        public void QueuePassed(string title, MissionTally.Result result)
        {
            _title = string.IsNullOrWhiteSpace(title) ? "Bloodlines" : title.Substring(0, Math.Min(80, title.Length));
            _result = result;
            _shown = 0; _last = Game.GameTime;
            StopScore();
        }

        /// <summary>
        /// The title card at the start of gameplay: the mission's number and act, its name,
        /// the target line from its context card and who is on it. Five seconds, then gone.
        /// </summary>
        public void QueueTitle(Missions.MissionDefinition definition)
        {
            if (definition == null) return;
            _cardId = definition.Id;
            _cardTitle = (definition.Title ?? "").ToUpperInvariant();
            _cardAct = Missions.MissionDefinition.ActTitle(definition.Act).ToUpperInvariant();
            Missions.MissionContextCard.Card card;
            _cardTarget = Missions.MissionContextCard.Cards.TryGetValue(definition.Id, out card) ? card.Target : "";
            _cardRoles = card != null ? card.Roles : "";
            _cardShown = 0;
        }

        /// <summary>The lines the passed panel draws under the title, for a test to read without a screen.</summary>
        public static List<KeyValuePair<string, string>> ResultLines(MissionTally.Result r)
        {
            var lines = new List<KeyValuePair<string, string>>();
            if (r == null) return lines;
            lines.Add(new KeyValuePair<string, string>("TIME", r.Clock));
            lines.Add(new KeyValuePair<string, string>("KILLS", r.Kills + (r.Headshots > 0 ? "  (" + r.Headshots + " headshots)" : "")));
            string credits = r.Credits();
            if (credits.Length > 0) lines.Add(new KeyValuePair<string, string>("CREDITED", credits));
            lines.Add(new KeyValuePair<string, string>("SWITCHES", r.Switches.ToString()));
            lines.Add(new KeyValuePair<string, string>("DAMAGE TAKEN", r.DamageTaken.ToString()));
            lines.Add(new KeyValuePair<string, string>("PAYOUT", r.Payout > 0 ? "$" + r.Payout.ToString("N0") : (r.FirstCompletion ? "$0" : "replay - no payout")));
            lines.Add(new KeyValuePair<string, string>("CAMPAIGN", r.CampaignCompleted + " / " + r.CampaignTotal + "  -  $" + r.CashOnHand.ToString("N0") + " on hand"));
            return lines;
        }

        public bool TitleCardShowing => _cardId != null;

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
            if (missionGameplay) { _title = null; _result = null; DrawTitleCard(dt, blocked || paused || !alive); return; }
            _cardId = null;
            if (_title == null || blocked || paused || !alive) return;
            _shown += dt;
            if (_shown >= PassedMs) { _title = null; _result = null; return; }
            int alpha = (int)(220f * Math.Min(1f, Math.Min(_shown / 300f, (PassedMs - _shown) / 500f)));
            new GTA.UI.TextElement("MISSION PASSED", new PointF(640f, 212f), 1.1f,
                Color.FromArgb(alpha, 240, 192, 70)) { Alignment = GTA.UI.Alignment.Center, Font = GTA.UI.Font.Pricedown }.Draw();
            new GTA.UI.TextElement(_title, new PointF(640f, 305f), .43f,
                Color.FromArgb(alpha, 255, 255, 255)) { Alignment = GTA.UI.Alignment.Center }.Draw();
            var lines = ResultLines(_result);
            if (lines.Count == 0) return;
            // What the attempt came to, in two columns under the title. Numbers, not
            // adjectives: a replay that says "3:12, 9 kills, $0" says all it needs to.
            float y = 350f;
            new GTA.UI.ContainerElement(new PointF(400f, y - 8f), new SizeF(480f, lines.Count * 24f + 16f), Color.FromArgb(Math.Min(alpha, 150), 12, 14, 18)).Draw();
            foreach (var line in lines)
            {
                new GTA.UI.TextElement(line.Key, new PointF(420f, y), .27f, Color.FromArgb(alpha, 178, 195, 211)).Draw();
                new GTA.UI.TextElement(line.Value, new PointF(860f, y), .27f, Color.FromArgb(alpha, 255, 255, 255)) { Alignment = GTA.UI.Alignment.Right }.Draw();
                y += 24f;
            }
        }

        private void DrawTitleCard(int dt, bool held)
        {
            if (_cardId == null || held) return;
            _cardShown += dt;
            if (_cardShown >= CardMs) { _cardId = null; return; }
            int alpha = (int)(230f * Math.Min(1f, Math.Min(_cardShown / 250f, (CardMs - _cardShown) / 600f)));
            new GTA.UI.TextElement(_cardId + "   ·   " + _cardAct, new PointF(640f, 214f), .30f,
                Color.FromArgb(alpha, 178, 195, 211)) { Alignment = GTA.UI.Alignment.Center }.Draw();
            new GTA.UI.TextElement(_cardTitle, new PointF(640f, 236f), .95f,
                Color.FromArgb(alpha, 240, 192, 70)) { Alignment = GTA.UI.Alignment.Center, Font = GTA.UI.Font.Pricedown }.Draw();
            if (!string.IsNullOrEmpty(_cardTarget))
                new GTA.UI.TextElement(_cardTarget, new PointF(640f, 318f), .40f,
                    Color.FromArgb(alpha, 255, 255, 255)) { Alignment = GTA.UI.Alignment.Center }.Draw();
            if (!string.IsNullOrEmpty(_cardRoles))
                new GTA.UI.TextElement(_cardRoles, new PointF(640f, 348f), .27f,
                    Color.FromArgb(alpha, 178, 195, 211)) { Alignment = GTA.UI.Alignment.Center }.Draw();
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
        public void Stop() { StopScore(); _title = null; _result = null; _cardId = null; _shown = 0; _last = Game.GameTime; }
    }
}
