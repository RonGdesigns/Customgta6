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
    public sealed partial class SurveyMode
    {
        private readonly LocationBook _book;
        /// <summary>The book being surveyed, so a menu can report progress against it.</summary>
        public LocationBook Book => _book;
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

        /// <summary>How long a refused capture waits for a deliberate second press.</summary>
        public const int ConfirmWindowMs = 6000;

        private string _confirmKey;
        private int _confirmUntil;

        /// <summary>
        /// How much clear room there is at a spot, and the point below which no vehicle
        /// the campaign spawns will fit. Reached through these two members rather than
        /// directly so the recovery test harness, which compiles this file without the
        /// placement helpers, still builds.
        /// </summary>
        public static Func<Vector3, float> ClearanceProbe;
        /// <summary>
        /// Looks down from a point for the first surface under it, for a land key captured
        /// from the air. Injected at startup beside <see cref="ClearanceProbe"/> rather
        /// than called directly, so the survey stays testable without the whole
        /// site-probing module behind it. Null means no drop: the point stands as flown.
        /// </summary>
        public static Func<Vector3, float, float?> SurfaceProbe;
        /// <summary>
        /// Under this much room no vehicle the campaign spawns will fit. Set from
        /// MissionSites at startup, where the number lives; the default here only has to
        /// be sane for a harness that compiles this file on its own.
        /// </summary>
        public static float TightRoom = 6f;

        private static float Clearance(Vector3 at)
        {
            try { return ClearanceProbe != null ? ClearanceProbe(at) : float.MaxValue; }
            catch (Exception ex) { Logger.Warn("A clearance probe could not run: " + ex.Message); return float.MaxValue; }
        }
        /// <summary>True while this key's refused capture is still waiting to be confirmed.</summary>
        private bool Confirming(string key) =>
            _confirmKey == key && Game.GameTime < _confirmUntil;

        public SurveyMode(LocationBook book, string outputPath, string captureKey = "F11", string teleportKey = "F7")
        {
            _book = book;
            _outputPath = outputPath;
            _captureKey = captureKey;
            _teleportKey = teleportKey;
        }

        /// <summary>
        /// The free camera. A capture reads it instead of the ped while it is up, which
        /// is the difference between surveying a helicopter hold and not surveying it.
        /// </summary>
        public SurveyCamera Camera { get; } = new SurveyCamera();
        /// <summary>The translucent stand-in that rides the camera while placing.</summary>
        public PlacementGhost Ghost { get; } = new PlacementGhost();

        /// <summary>
        /// Show or hide the stand-in. It needs the camera: hanging a ghost in front of a
        /// man standing on the ground would put it in the wall he is facing.
        /// </summary>
        public bool ToggleGhost()
        {
            if (Ghost.IsShowing) { Ghost.Hide(); GameUtils.Notify("~y~Ghost off."); return false; }
            if (!Camera.IsFlying && !ToggleCamera()) return false;
            var here = IsEditing ? Draft : Current;
            if (!Ghost.Show(PlacementGhost.ShapeFor(here?.Kind), here?.Heading ?? 0f))
            { GameUtils.Notify("~r~The stand-in could not be created."); return false; }
            GameUtils.Notify("~g~Ghost on.~s~ Fly it into place, then save the placement.");
            return true;
        }

        /// <summary>
        /// Take the placement from the stand-in. The one motion that replaces walk,
        /// capture, adjust height, adjust facing, save.
        /// </summary>
        public bool PlaceAtGhost()
        {
            if (!IsEditing || Draft == null || !Ghost.IsShowing) return false;
            Draft.Position = Ghost.Commit(Draft.Kind, out float dropped);
            Draft.Heading = Ghost.Heading;
            GameUtils.Notify("~g~Placed from the stand-in" +
                (dropped > .05f ? ", dropped " + dropped.ToString("0.0") + " m onto the surface" : "") + ".");
            return true;
        }

        /// <summary>
        /// Where the next capture comes from, and whether it was flown to or walked to.
        /// A point captured from the air for a key that stands on the ground is dropped
        /// onto the first surface under it, the way a map editor drops what you place:
        /// the whole point of flying is that he is looking at the spot from above it.
        /// </summary>
        private Vector3 CapturePoint(MissionLocation location, out float dropped, out bool flown)
        {
            dropped = 0f; flown = Camera.IsFlying;
            if (!flown) return Game.Player.Character.Position;
            var point = Camera.Position;
            if (!string.Equals(location?.Kind, "land", StringComparison.OrdinalIgnoreCase)) return point;
            // Air, water, channel, interior and underground keys are taken exactly where
            // the camera is. They are the ones that have no ground to sit on, and pulling
            // them down to the sea floor would be worse than the estimate.
            float? surface = SurfaceProbe != null ? SurfaceProbe(point, CameraDrop) : null;
            if (!surface.HasValue) return point;
            dropped = point.Z - surface.Value;
            return new Vector3(point.X, point.Y, surface.Value);
        }
        /// <summary>How far under the camera a land capture will look for ground.</summary>
        public const float CameraDrop = 120f;

        public bool IsActive { get; private set; }
        public static bool IsSurveyRunning { get; private set; }
        public static bool OwnsCaptureKey => IsSurveyRunning || _lastCaptureFrame == Game.GameTime;
        public bool IsTeleporting => _moving != null;
        /// <summary>Where the flying camera was before a teleport took it, so a rollback can undo both.</summary>
        private Vector3? _cameraFrom;
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

        /// <summary>Lift or drop the free camera, and say which it did.</summary>
        public bool ToggleCamera()
        {
            if (Camera.IsFlying) { Camera.Release(); GameUtils.Notify("~y~Survey camera down.~s~ Captures come from where you stand again."); return false; }
            if (!Camera.Take()) { GameUtils.Notify("~r~The survey camera could not be created."); return false; }
            GameUtils.Notify("~g~Survey camera up.~s~ Fly it and capture from where you are looking.");
            return true;
        }

        public void Stop()
        {
            bool wasActive = IsActive;
            bool wasEditing = IsEditing;
            Draft = null;
            CancelTeleport();
            IsActive = IsSurveyRunning = false;
            Ghost.Hide();
            Camera.Release();
            GameUtils.SafeDelete(_destination);
            _destination = null;
            if (wasActive && !wasEditing && Write()) GameUtils.Notify("~g~Survey saved.~s~ Captures will load next session.");
        }

        public void Capture()
        {
            if (IsEditing) { SavePlacement(true); return; }
            var location = Current;
            if (location == null || IsTeleporting) return;
            _lastCaptureFrame = Game.GameTime;
            var player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) return;
            // Capture the surface under the avatar, not the vehicle's model origin. This
            // does not apply to the camera: it is not standing anywhere, and where the man
            // happens to be sitting while he flies it is beside the point.
            if (!Camera.IsFlying && player.IsInVehicle())
            {
                GameUtils.Subtitle("~y~Exit the vehicle and stand on the intended spot to capture it.", 3500);
                return;
            }
            var taken = CapturePoint(location, out float dropped, out bool flown);
            float facing = flown ? Camera.Heading : player.Heading;
            // A capture is made on foot, and a man fits where a truck does not. The two
            // things Ron cannot see from where he is standing get measured for him:
            // whether this is even the right part of the map, and how much room is here.
            float away = LocationBook.FlatDistance(taken, location.Authored);
            if (LocationBook.Displaced(location, taken) && !Confirming(location.Key))
            {
                _confirmKey = location.Key;
                _confirmUntil = Game.GameTime + ConfirmWindowMs;
                GameUtils.Notify("~r~" + location.Key + " belongs " + (int)away + " m from here.~s~\n" +
                    "That reads as the wrong key rather than a correction. Press " + _captureKey +
                    " again within " + (ConfirmWindowMs / 1000) + "s to save it here anyway.");
                Logger.Warn("Refused a survey capture of " + location.Key + " at " + taken + ": " +
                            (int)away + " m from where that key belongs. Waiting for a deliberate confirmation.");
                return;
            }

            float room = Clearance(taken);
            _book.Record(location.Key, taken, facing);
            _captured.Add(location.Key);
            if (!Write()) return;
            _confirmKey = null;
            Logger.Info("Surveyed " + location.Key + " = " + taken + " heading " + facing.ToString("0.0") +
                        "; " + room.ToString("0.0") + " m of clear room, " + (int)away + " m from the authored point" +
                        (flown ? ", flown" + (dropped > .05f ? " and dropped " + dropped.ToString("0.0") + " m onto the surface" : "") : "") + ".");
            if (room < TightRoom)
            {
                GameUtils.Notify("~o~Saved " + location.Key + "~s~ with only " + room.ToString("0.0") +
                    " m of room. A car or a truck will not fit here.");
                Logger.Warn(location.Key + " was surveyed with " + room.ToString("0.0") +
                            " m of clear room. If a mission spawns a vehicle here it will not fit.");
            }
            else GameUtils.Notify("~g~Saved " + location.Key + "~s~ (" + room.ToString("0.0") + " m clear). Next destination marked.");
            Next();
        }

        public void Next()
        {
            if (IsEditing) { MovePlacement(1); return; }
            if (!IsActive || IsTeleporting) return;
            if (_index + 1 >= _queue.Count) { Stop(); return; }
            Select(_index + 1);
        }
        public void Previous() { if (IsEditing) { MovePlacement(-1); return; } if (IsActive && !IsTeleporting && _index > 0) Select(_index - 1); }
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
            // The distance is on this notification because the mistake it prevents is
            // pressing capture without having gone to the next key at all.
            float away = Game.Player.Character != null && Game.Player.Character.Exists()
                ? LocationBook.FlatDistance(Game.Player.Character.Position, location.Position) : 0f;
            GameUtils.Notify("~y~Survey destination: " + location.Key + "~s~\n" + location.DistrictHint +
                " - " + location.Kind + "\n" + (int)away + " m away. Follow the yellow GPS route, or press " + _teleportKey + " to teleport.");
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
                // A capture in fly mode reads the camera, so the camera is the thing that has
                // to arrive. Left behind it looks at where he was standing, which is a
                // teleport that appears not to have happened - and it holds the streaming
                // focus, so the collision this teleport is waiting on never loads and the
                // whole thing rolls back. Ron reported the symptom: in fly mode it does not
                // teleport.
                _cameraFrom = Camera.IsFlying ? Camera.Position : (Vector3?)null;
                if (_cameraFrom.HasValue) Camera.MoveTo(p);
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
            // The view goes back with the man. Leaving it at a destination he was not moved
            // to is a camera outside its own leash, looking at somewhere he is not.
            var cameraFrom = _cameraFrom;
            _cameraFrom = null;
            if (rollback && cameraFrom.HasValue) Camera.ReturnTo(cameraFrom.Value);
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
            // The camera flies whenever it is up, including while the placement menu has
            // focus: moving the view is how he chooses the spot the menu will save.
            Camera.Update();
            Ghost.Update(Camera);
            // Any numbered keys in the mission being surveyed are a path; draw it.
            if (IsActive && Current != null)
            {
                int dot = Current.Key.IndexOf('.');
                var viewer = Camera.IsFlying ? Camera.Position : (Game.Player.Character?.Position ?? Current.Position);
                if (dot > 0) RouteRibbons.Draw(_book, Current.Key.Substring(0, dot), viewer);
            }
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
            if (IsEditing) { DrawPlacement(); return; }
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
            lines.Add("[SpawnGroups]");
            foreach (var l in _book.All.Where(MissionPlacement.HasFormation).OrderBy(l => l.Key, StringComparer.Ordinal))
            {
                lines.Add(l.Key + ".Count = " + l.SpawnCount.ToString(CultureInfo.InvariantCulture));
                lines.Add(l.Key + ".Radius = " + l.SpawnRadius.ToString("0.00", CultureInfo.InvariantCulture));
            }
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
