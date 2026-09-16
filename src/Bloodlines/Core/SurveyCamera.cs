using System;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// A free camera for surveying, so a coordinate is placed by looking at the spot
    /// rather than by walking a man to it.
    ///
    /// Every point in the location book was captured by putting the avatar on it and
    /// pressing the capture key. That works, slowly, for a doorway. It does not work at
    /// all for the things this campaign is mostly made of: a helicopter hold forty meters
    /// up, an approach heading out over the water, a roost on a roof with no stair, a
    /// point on a deck the player cannot reach until the mission has already placed him
    /// there. Ron said it plainly — it slows him down, and air placements worst of all.
    /// That is why 1,061 of 1,091 keys are still estimates.
    ///
    /// This detaches the view, leaves the man where he stands, and flies. The capture
    /// then reads the camera instead of the ped.
    ///
    /// Three rules it exists to keep:
    ///
    ///  * **Give the view back.** A script camera left rendering is a game the player
    ///    cannot play. Every exit path — stop, survey end, death, teardown — releases it,
    ///    and <see cref="Release"/> is safe to call when nothing was ever taken.
    ///  * **Stream where it is looking.** Collision loads around the player, not the
    ///    camera, so a point captured two hundred meters away can sit over ground the game
    ///    has not built yet and probe as nothing. The focus goes with the camera and is
    ///    cleared with it.
    ///  * **Leave the man alone.** He is frozen where he was standing, not teleported and
    ///    not deleted. Unfreezing him is part of releasing the camera.
    /// </summary>
    public sealed class SurveyCamera
    {
        /// <summary>Meters per second at a normal push of the stick.</summary>
        public const float BaseSpeed = 14f;
        /// <summary>What the sprint control multiplies that by, for crossing a site.</summary>
        public const float FastFactor = 4.5f;
        /// <summary>And what the slow control divides it by, for placing something exactly.</summary>
        public const float SlowFactor = 6f;
        /// <summary>Degrees per second at full stick deflection.</summary>
        public const float LookSpeed = 140f;
        /// <summary>How far the camera may get from the man it left behind.</summary>
        public const float Leash = 600f;
        /// <summary>Pitch is clamped short of straight up and down, where the math folds over.</summary>
        public const float MaxPitch = 88f;

        private Camera _camera;
        private float _pitch, _yaw;
        private bool _frozeMan, _manWasFrozen;

        /// <summary>Whether the camera is up and the survey should read it instead of the ped.</summary>
        public bool IsFlying => _camera != null && _camera.Exists();

        /// <summary>Where a capture should be taken from. Only meaningful while flying.</summary>
        public Vector3 Position => IsFlying ? _camera.Position : Vector3.Zero;
        /// <summary>The direction the camera is facing, as a heading a ped could be given.</summary>
        public float Heading => IsFlying ? Normalize(-_yaw) : 0f;

        public void Toggle() { if (IsFlying) Release(); else Take(); }

        /// <summary>Lift the view off the player. Does nothing if it is already up.</summary>
        public bool Take()
        {
            if (IsFlying) return true;
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return false;
            try
            {
                var from = GameplayCamera.Position;
                var rotation = GameplayCamera.Rotation;
                _camera = World.CreateCamera(from, rotation, GameplayCamera.FieldOfView);
                if (_camera == null || !_camera.Exists()) { _camera = null; return false; }
                _pitch = Math.Max(-MaxPitch, Math.Min(MaxPitch, rotation.X));
                _yaw = rotation.Z;
                _camera.IsActive = true;
                World.RenderingCamera = _camera;
                // The man stays exactly where he was. Remembering whether he was already
                // frozen matters: a cutscene or a handover may have frozen him first, and
                // thawing him on the way out would be this tool moving somebody else's actor.
                _manWasFrozen = player.IsPositionFrozen;
                if (!_manWasFrozen) { player.IsPositionFrozen = true; _frozeMan = true; }
                Logger.Info("Survey camera up at " + from + ".");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Taking the survey camera", ex);
                Release();
                return false;
            }
        }

        /// <summary>
        /// Put the view back and let the man go. Safe at any time, including when the
        /// camera was never taken, which is what makes it safe on every teardown path.
        /// </summary>
        public void Release()
        {
            try { Function.Call(Hash.CLEAR_FOCUS); } catch (Exception ex) { Logger.Error("Clearing the survey focus", ex); }
            if (_frozeMan)
            {
                _frozeMan = false;
                var player = Game.Player.Character;
                try { if (player != null && player.Exists() && !_manWasFrozen) player.IsPositionFrozen = false; }
                catch (Exception ex) { Logger.Error("Releasing the surveyed player", ex); }
            }
            if (_camera == null) return;
            try
            {
                World.RenderingCamera = null;
                if (_camera.Exists()) { _camera.IsActive = false; _camera.Delete(); }
                Logger.Info("Survey camera released.");
            }
            catch (Exception ex) { Logger.Error("Releasing the survey camera", ex); }
            finally { _camera = null; }
        }

        /// <summary>
        /// Fly it. Called once a frame while the survey owns input; the caller is
        /// responsible for having disabled gameplay controls, exactly as the dev menu does.
        /// </summary>
        public void Update()
        {
            if (!IsFlying) return;
            var player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead) { Release(); return; }

            float dt = Game.LastFrameTime;
            if (float.IsNaN(dt) || float.IsInfinity(dt)) dt = 0f;
            dt = Math.Max(0f, Math.Min(.1f, dt));

            // Look. The same two axes the dev menu already leaves enabled, so this works
            // on a stick and on a mouse without knowing which one is in his hands.
            _yaw -= Game.GetControlValueNormalized(GTA.Control.LookLeftRight) * LookSpeed * dt;
            _pitch -= Game.GetControlValueNormalized(GTA.Control.LookUpDown) * LookSpeed * dt;
            _pitch = Math.Max(-MaxPitch, Math.Min(MaxPitch, _pitch));
            _yaw = Normalize(_yaw);

            float speed = BaseSpeed * dt;
            if (Game.IsControlPressed(GTA.Control.Sprint)) speed *= FastFactor;
            if (Game.IsControlPressed(GTA.Control.Duck)) speed /= SlowFactor;

            var rotation = new Vector3(_pitch, 0f, _yaw);
            _camera.Rotation = rotation;
            var forward = Forward(_pitch, _yaw);
            var right = Forward(0f, _yaw - 90f);

            var move = forward * -Game.GetControlValueNormalized(GTA.Control.MoveUpDown) +
                       right * Game.GetControlValueNormalized(GTA.Control.MoveLeftRight);
            // Straight up and down, which is the whole point for an air key: the triggers
            // on a pad, Page Up and Page Down on the keyboard through the survey's keys.
            move.Z += Game.GetControlValueNormalized(GTA.Control.VehicleAccelerate) -
                      Game.GetControlValueNormalized(GTA.Control.VehicleBrake);
            move += new Vector3(0f, 0f, _climb);
            _climb = 0f;

            var next = _camera.Position + move * speed;
            // A leash rather than a limit that stops dead: the game only streams so far
            // from the player, and a capture out past that is a capture of nothing.
            float reach = next.DistanceTo(player.Position);
            if (reach > Leash && reach > .01f) next = player.Position + (next - player.Position) * (Leash / reach);
            _camera.Position = next;

            // Build the world where the camera is looking, not where the man is standing.
            try { Function.Call(Hash.SET_FOCUS_POS_AND_VEL, next.X, next.Y, next.Z, 0f, 0f, 0f); }
            catch (Exception ex) { Logger.Error("Focusing the survey camera", ex); }
        }

        private float _climb;
        /// <summary>One keyboard press worth of climb or descent, applied on the next frame.</summary>
        public void Climb(float amount) { if (IsFlying) _climb += amount; }

        private static Vector3 Forward(float pitch, float yaw)
        {
            double p = pitch * Math.PI / 180.0, y = yaw * Math.PI / 180.0;
            double flat = Math.Cos(p);
            return new Vector3((float)(-Math.Sin(y) * flat), (float)(Math.Cos(y) * flat), (float)Math.Sin(p));
        }

        private static float Normalize(float degrees)
        {
            while (degrees > 180f) degrees -= 360f;
            while (degrees < -180f) degrees += 360f;
            return degrees;
        }
    }
}
