using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;

namespace Bloodlines.Core
{
    /// <summary>GPS-guided survey with explicit teleport and persistent captures.</summary>
    public sealed class SurveyMode
    {
        private readonly LocationBook _book;
        private readonly string _outputPath;
        private readonly string _captureKey;
        private readonly string _teleportKey;
        private readonly List<MissionLocation> _queue = new List<MissionLocation>();
        private readonly HashSet<string> _captured = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private int _index;
        private Blip _destination;
        private Entity _moving;
        private Ped _movingPlayer;
        private Vector3 _oldPosition;
        private float _oldHeading;
        private bool _wasFrozen;
        private bool _wasInvincible;
        private int _warpStarted;
        private static int _lastCaptureFrame = -1;

        public SurveyMode(LocationBook book, string outputPath, string captureKey = "F11", string teleportKey = "F7")
        {
            _book = book;
            _outputPath = outputPath;
            _captureKey = captureKey;
            _teleportKey = teleportKey;
        }

        public bool IsActive { get; private set; }
        public static bool IsSurveyRunning { get; private set; }
        public static bool OwnsCaptureKey => IsSurveyRunning || _lastCaptureFrame == Game.GameTime;
        public bool IsTeleporting => _moving != null;
        public MissionLocation Current => IsActive && _index >= 0 && _index < _queue.Count ? _queue[_index] : null;

        public void Start(string missionPrefix = null)
        {
            if (IsActive) Stop();
            _queue.Clear();
            _captured.Clear();
            _index = 0;
            _queue.AddRange(_book.All
                .Where(l => missionPrefix == null || l.Key.StartsWith(missionPrefix + ".", StringComparison.OrdinalIgnoreCase))
                .OrderBy(l => l.Status == LocationStatus.Surveyed || l.Status == LocationStatus.Bible)
                .ThenBy(l => l.Key, StringComparer.Ordinal));
            if (_queue.Count == 0) { GameUtils.Subtitle("~y~No locations to survey.", 3000); return; }
            IsActive = IsSurveyRunning = true;
            Select(0);
        }

        /// <summary>A named set of spots in the order given: the room survey, walked rather than driven.</summary>
        public void Start(IEnumerable<string> keys)
        {
            if (IsActive) Stop();
            _queue.Clear();
            _captured.Clear();
            _index = 0;
            foreach (var key in keys) { var location = _book.Get(key); if (location != null) _queue.Add(location); }
            if (_queue.Count == 0) { GameUtils.Subtitle("~y~No locations to survey.", 3000); return; }
            IsActive = IsSurveyRunning = true;
            Select(0);
        }

        /// <summary>The folder the survey ini lives in; other survey files go beside it.</summary>
        public string OutputDirectory => Path.GetDirectoryName(_outputPath);

        public void Stop()
        {
            bool wasActive = IsActive;
            CancelTeleport();
            IsActive = IsSurveyRunning = false;
            GameUtils.SafeDelete(_destination);
            _destination = null;
            if (wasActive && Write()) GameUtils.Notify("~g~Survey saved.~s~ Captures will load next session.");
        }

        public void Capture()
        {
            var location = Current;
            if (location == null || IsTeleporting) return;
            _lastCaptureFrame = Game.GameTime;
            var player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) return;
            // Capture the surface under the avatar, not the vehicle's model origin.
            if (player.IsInVehicle())
            {
                GameUtils.Subtitle("~y~Exit the vehicle and stand on the intended spot to capture it.", 3500);
                return;
            }
            _book.Record(location.Key, player.Position, player.Heading);
            _captured.Add(location.Key);
            if (!Write()) return;
            Logger.Info("Surveyed " + location.Key + " = " + player.Position + " heading " + player.Heading);
            GameUtils.Notify("~g~Saved " + location.Key + "~s~. Next destination marked.");
            Next();
        }

        public void Next()
        {
            if (!IsActive || IsTeleporting) return;
            if (_index + 1 >= _queue.Count) { Stop(); return; }
            Select(_index + 1);
        }
        public void Previous() { if (IsActive && !IsTeleporting && _index > 0) Select(_index - 1); }
        public void Skip() { Next(); }

        private void Select(int index)
        {
            _index = index;
            var location = Current;
            GameUtils.SafeDelete(_destination);
            _destination = World.CreateBlip(location.Position);
            if (_destination != null && _destination.Exists())
            {
                _destination.Sprite = BlipSprite.Standard;
                _destination.Color = BlipColor.Yellow;
                _destination.IsShortRange = false;
                _destination.Name = "Survey: " + location.Key;
                _destination.ShowRoute = true;
            }
            // The owned routed blip stays visible on the map at any distance and
            // can be removed on stop without clearing the player's personal waypoint.
            GameUtils.Notify("~y~Survey destination: " + location.Key + "~s~\n" + location.DistrictHint +
                " - " + location.Kind + "\nFollow the yellow GPS route, or press " + _teleportKey + " to teleport.");
        }

        public void TeleportToCurrent()
        {
            if (Current == null || IsTeleporting) return;
            var player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) return;
            Entity moving = player.CurrentVehicle != null ? (Entity)player.CurrentVehicle : player;
            _moving = moving;
            _movingPlayer = player;
            _oldPosition = moving.Position;
            _oldHeading = moving.Heading;
            _wasFrozen = moving.IsPositionFrozen;
            _wasInvincible = player.IsInvincible;
            _warpStarted = Game.GameTime;
            try
            {
                moving.IsPositionFrozen = true;
                player.IsInvincible = true;
                var p = Current.Position;
                Function.Call(Hash.REQUEST_COLLISION_AT_COORD, p.X, p.Y, p.Z);
                moving.Position = p + new Vector3(0f, 0f, 0.5f);
                moving.Heading = Current.Heading;
                moving.Velocity = Vector3.Zero;
            }
            catch { CancelTeleport(); throw; }
        }

        public void CancelTeleport()
        {
            FinishTeleport(true);
        }

        private void FinishTeleport(bool rollback)
        {
            var moving = _moving;
            var player = _movingPlayer;
            _moving = null;
            _movingPlayer = null;
            try
            {
                if (moving != null && moving.Exists())
                {
                    try
                    {
                        if (rollback) { moving.Position = _oldPosition; moving.Heading = _oldHeading; }
                    }
                    finally { moving.IsPositionFrozen = _wasFrozen; }
                }
            }
            finally
            {
                if (player != null && player.Exists()) player.IsInvincible = _wasInvincible;
            }
        }

        public void Update()
        {
            var location = Current;
            if (location == null) return;
            if (IsTeleporting)
            {
                try
                {
                    if (!_moving.Exists() || _movingPlayer == null || !_movingPlayer.Exists() || _movingPlayer.IsDead)
                        CancelTeleport();
                    else
                    {
                        var p = location.Position;
                        Function.Call(Hash.REQUEST_COLLISION_AT_COORD, p.X, p.Y, p.Z);
                        if (Game.GameTime - _warpStarted > 250 && Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY, _moving))
                        {
                            FinishTeleport(false);
                            GameUtils.Notify("~y~At " + location.Key + ".~s~ Verify the surface before capturing.");
                        }
                        else if (Game.GameTime - _warpStarted >= 4000)
                        {
                            CancelTeleport();
                            GameUtils.Notify("~y~Terrain did not load. Returned you to your previous position; use the GPS route.");
                        }
                    }
                }
                catch { CancelTeleport(); throw; }
            }
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;
            GameUtils.DrawObjectiveMarker(location.Position, Color.FromArgb(120, 232, 168, 56), 1.2f);
            float distance = player.Position.DistanceTo(location.Position);
            new ContainerElement(new PointF(40f, 480f), new SizeF(580f, 108f), Color.FromArgb(225, 18, 20, 24)).Draw();
            new TextElement("Survey " + (_index + 1) + "/" + _queue.Count + " - " + location.Key,
                new PointF(50f, 487f), 0.32f, Color.White).Draw();
            new TextElement(location.DistrictHint + " | " + location.Kind + " | " + location.Status,
                new PointF(50f, 513f), 0.27f, Color.Gold).Draw();
            new TextElement((int)distance + "m away | target Z " + location.Position.Z.ToString("0.0", CultureInfo.InvariantCulture),
                new PointF(50f, 537f), 0.27f, Color.White).Draw();
            new TextElement(_teleportKey + " teleport | " + _captureKey + " capture | End skip | Home previous",
                new PointF(50f, 561f), 0.27f, Color.White).Draw();
        }

        public bool Write()
        {
            var lines = new List<string> { "; Bloodlines survey captures. Loaded automatically on startup.", "[Positions]" };
            var surveyed = _book.All.Where(l => l.Status == LocationStatus.Surveyed).OrderBy(l => l.Key, StringComparer.Ordinal).ToList();
            foreach (var l in surveyed)
            {
                lines.Add(l.Key + ".X = " + l.Position.X.ToString("0.00", CultureInfo.InvariantCulture));
                lines.Add(l.Key + ".Y = " + l.Position.Y.ToString("0.00", CultureInfo.InvariantCulture));
                lines.Add(l.Key + ".Z = " + l.Position.Z.ToString("0.00", CultureInfo.InvariantCulture));
            }
            lines.Add("[Headings]");
            foreach (var l in surveyed) lines.Add(l.Key + " = " + l.Heading.ToString("0.0", CultureInfo.InvariantCulture));
            try
            {
                string temp = _outputPath + ".tmp";
                File.WriteAllLines(temp, lines, new UTF8Encoding(false));
                if (File.Exists(_outputPath)) File.Replace(temp, _outputPath, _outputPath + ".bak");
                else File.Move(temp, _outputPath);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Logger.Error("Survey save failed", ex);
                GameUtils.Notify("~r~Capture was not saved.~s~ Check Bloodlines.log and retry.");
                return false;
            }
        }
    }
}
