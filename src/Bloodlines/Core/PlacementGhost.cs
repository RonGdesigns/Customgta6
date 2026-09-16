using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// A translucent stand-in that rides the survey camera, so a placement is flown into
    /// position instead of typed into it.
    ///
    /// The alternative everybody proposes is a 3D gizmo — red, green and blue axis handles
    /// you grab and drag. A script menu in this game has no mouse cursor over the world,
    /// so there is nothing to grab with; drawn axes would be decoration over the same
    /// key-by-key adjustment as before. The camera is already the pointing device. Hang
    /// the thing being placed in front of it and the problem is solved with what exists:
    /// fly it where you want it, drop it, done.
    ///
    /// It shows a **shape**, not the exact vehicle a mission will spawn. Nothing in the
    /// location book says which model belongs at a key — that knowledge lives in mission
    /// code — so guessing one would be a confident lie about clearance. What the shape
    /// honestly answers is the question that actually goes wrong: does a thing of roughly
    /// this size fit here, level, with its wheels or feet on the surface, facing that way.
    /// For the exact article, stage the mission and look at what it really spawns.
    /// </summary>
    public sealed class PlacementGhost
    {
        /// <summary>How see-through the stand-in is. Solid enough to read against terrain.</summary>
        public const int Alpha = 170;
        /// <summary>How far in front of the camera it hangs by default.</summary>
        public const float DefaultStandoff = 7f;
        public const float MinStandoff = 2f, MaxStandoff = 40f;
        /// <summary>How far under the ghost a drop will look for a surface.</summary>
        public const float DropReach = 120f;

        /// <summary>
        /// What the stand-in is shaped like. Every model here is one the campaign already
        /// spawns somewhere, so nothing new has to be verified against the model dumps.
        /// </summary>
        public enum Shape { Person, Car, Truck, Helicopter, Boat }

        private static readonly Dictionary<Shape, string> Models = new Dictionary<Shape, string>
        {
            { Shape.Person, "s_m_y_blackops_01" },
            { Shape.Car, "granger" },
            { Shape.Truck, "flatbed" },
            { Shape.Helicopter, "cargobob" },
            { Shape.Boat, "submersible2" },
        };

        private Entity _entity;
        private Shape _shape = Shape.Person;
        private float _standoff = DefaultStandoff;
        private float _heading;

        public bool IsShowing => _entity != null && _entity.Exists();
        public Shape Current => _shape;
        public float Standoff => _standoff;
        /// <summary>Where it is, for a caller about to write a placement.</summary>
        public Vector3 Position => IsShowing ? _entity.Position : Vector3.Zero;
        public float Heading => _heading;

        /// <summary>The shape a key of this kind is most likely to be, as a starting guess.</summary>
        public static Shape ShapeFor(string kind)
        {
            switch ((kind ?? "").ToLowerInvariant())
            {
                case "air": return Shape.Helicopter;
                case "water": case "channel": return Shape.Boat;
                default: return Shape.Person;
            }
        }

        public void Cycle(int delta)
        {
            var values = (Shape[])Enum.GetValues(typeof(Shape));
            int index = Array.IndexOf(values, _shape) + delta;
            while (index < 0) index += values.Length;
            _shape = values[index % values.Length];
            if (IsShowing) Respawn();
        }

        public void PushOut(float delta)
        {
            _standoff = Math.Max(MinStandoff, Math.Min(MaxStandoff, _standoff + delta));
        }

        public void Turn(float degrees)
        {
            _heading = (_heading + degrees) % 360f;
            if (_heading < 0f) _heading += 360f;
            if (IsShowing) _entity.Heading = _heading;
        }

        /// <summary>Put a stand-in of this shape in the world, facing this way.</summary>
        public bool Show(Shape shape, float heading)
        {
            _shape = shape; _heading = heading;
            return Respawn();
        }

        private bool Respawn()
        {
            Hide();
            var model = new Model(Models.TryGetValue(_shape, out var name) ? name : Models[Shape.Person]);
            try
            {
                if (!GameUtils.RequestModel(model)) { Logger.Warn("The placement ghost's model would not load: " + model.Hash); return false; }
                var at = Game.Player.Character?.Position ?? Vector3.Zero;
                _entity = _shape == Shape.Person
                    ? (Entity)World.CreatePed(model, at, _heading)
                    : World.CreateVehicle(model, at, _heading);
                if (_entity == null || !_entity.Exists()) { _entity = null; return false; }
                // Seen through, walked through, and immune to whatever it is hovering over.
                Function.Call(Hash.SET_ENTITY_ALPHA, _entity, Alpha, false);
                _entity.IsCollisionEnabled = false;
                _entity.IsPositionFrozen = true;
                if (_entity is Ped ped) { ped.IsInvincible = true; ped.BlockPermanentEvents = true; ped.Task.StandStill(-1); }
                if (_entity is Vehicle vehicle) { vehicle.IsInvincible = true; vehicle.IsEngineRunning = false; }
                return true;
            }
            catch (Exception ex) { Logger.Error("Creating the placement ghost", ex); Hide(); return false; }
            finally { model.MarkAsNoLongerNeeded(); }
        }

        /// <summary>Hang it in front of the camera. Called every frame while it is up.</summary>
        public void Update(SurveyCamera camera)
        {
            if (!IsShowing) return;
            if (camera == null || !camera.IsFlying) return;
            try
            {
                var facing = Forward(camera);
                _entity.Position = camera.Position + facing * _standoff;
                _entity.Heading = _heading;
            }
            catch (Exception ex) { Logger.Warn("The placement ghost could not be moved: " + ex.Message); }
        }

        /// <summary>
        /// Where it should be written down. A key that stands on the ground is dropped onto
        /// the surface under the stand-in; everything else keeps the height it was flown
        /// to, which is the same rule the capture itself follows.
        /// </summary>
        public Vector3 Commit(string kind, out float dropped)
        {
            dropped = 0f;
            var at = Position;
            if (!IsShowing) return at;
            if (!string.Equals(kind, "land", StringComparison.OrdinalIgnoreCase)) return at;
            float? surface = SurveyMode.SurfaceProbe != null ? SurveyMode.SurfaceProbe(at, DropReach) : null;
            if (!surface.HasValue) return at;
            dropped = at.Z - surface.Value;
            return new Vector3(at.X, at.Y, surface.Value);
        }

        public void Hide()
        {
            if (_entity == null) return;
            GameUtils.SafeDelete(_entity);
            _entity = null;
        }

        private static Vector3 Forward(SurveyCamera camera)
        {
            double yaw = -camera.Heading * Math.PI / 180.0;
            return new Vector3((float)(-Math.Sin(yaw)), (float)Math.Cos(yaw), 0f);
        }
    }
}
