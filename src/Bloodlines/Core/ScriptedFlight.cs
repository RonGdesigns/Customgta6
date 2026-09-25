using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// An aircraft flown along a line of points by the script instead of by the AI.
    ///
    /// Ron wanted BM01's Osprey to "fly along the same path of the road. That way, it can be
    /// shot down." The AI will not promise that. A plane task picks its own line and height,
    /// and this campaign has already lost aircraft to AI pilots (M26's jet that never
    /// started, M27's Shamal with nobody flying it). So the pilot sits in the seat and the
    /// aircraft is moved: gravity off, a velocity toward the next point, turned to face where
    /// it is going. It cannot wander off the road or into a hill it was never pointed at.
    ///
    /// The pace is set by the chase. It keeps about <see cref="LeadMeters"/> ahead of whoever
    /// is hunting it: it opens the gap when they close, and waits when they fall behind, so a
    /// truck that is keeping up always has a shot. <see cref="Finished"/> is the end of the
    /// line, which the mission treats as the aircraft getting away.
    /// </summary>
    public sealed class ScriptedFlight
    {
        /// <summary>How far above each road point it flies. Over the trees, under nothing it cannot see.</summary>
        public const float Altitude = 32f;
        /// <summary>The gap it tries to keep ahead of the chase, in meters along the route.</summary>
        public const float LeadMeters = 110f;
        public const float MinSpeed = 12f, MaxSpeed = 40f;
        /// <summary>How close to a point counts as having passed it.</summary>
        public const float PointReach = 25f;
        /// <summary>How quickly it changes speed and direction, per second. A heavy aircraft does not snap.</summary>
        public const float Response = 1.6f;

        // Looking ahead. Ron, September 24: the Osprey "ended up flying into the trees". The
        // line was flown blind: straight between road points at a fixed height over each one,
        // so ground that rises between two points, or a stand of Paleto pines taller than the
        // road plus Altitude, was flown into. It is proof against collisions, so it did not
        // break; it hung in the branches. It now reads the ground and anything solid ahead.
        /// <summary>At least this far over the ground ahead of it: tall enough for the Paleto pines.</summary>
        public const float GroundClearance = 40f;
        /// <summary>How fast it climbs when something is in the way, meters per second.</summary>
        public const float ClimbRate = 9f;
        /// <summary>How often it looks ahead.</summary>
        public const int LookIntervalMs = 200;
        /// <summary>The wingtip rays sit this far either side of the center one.</summary>
        public const float WingOffset = 12f;
        /// <summary>After seeing something in its way it keeps climbing, slowly, for this long.</summary>
        public const int ObstacleHoldMs = 1500;
        /// <summary>World, objects and vegetation: the native shape-test flags, so a ray stops at a tree.</summary>
        public const int ObstacleFlags = 1 | 16 | 256;

        /// <summary>The top of the ground or structure under a point, or null. Injected so the flight can be tested.</summary>
        public static Func<Vector3, float?> GroundBelow = DefaultGroundBelow;
        /// <summary>Whether a straight line hits anything solid, ignoring the aircraft. Injected likewise.</summary>
        public static Func<Vector3, Vector3, Entity, bool> Blocked = DefaultBlocked;
        /// <summary>Put the real probes back (tests swap them).</summary>
        public static void ResetProbes() { GroundBelow = DefaultGroundBelow; Blocked = DefaultBlocked; }

        private static float? DefaultGroundBelow(Vector3 point)
        {
            var z = new OutputArgument();
            return Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD, point.X, point.Y, point.Z + 300f, z, false, false) ? z.GetResult<float>() : (float?)null;
        }

        private static bool DefaultBlocked(Vector3 from, Vector3 to, Entity ignore) =>
            World.Raycast(from, to, (IntersectFlags)ObstacleFlags, ignore).DidHit;

        private int _nextLook, _climbUntil;
        private float _floorAhead = float.MinValue;
        /// <summary>True while it is climbing over something it saw in its way.</summary>
        public bool Climbing => Game.GameTime < _climbUntil;
        /// <summary>The lowest height the ground ahead allows, or MinValue before it has looked.</summary>
        public float FloorAhead => _floorAhead;

        private readonly Vehicle _craft;
        private readonly List<Vector3> _points;
        private int _next;
        private Vector3 _velocity;
        private int _lastTick = -1;

        /// <param name="floor">
        /// The lowest height it may fly at anywhere on the line. <see cref="Altitude"/> over a
        /// road is not always over the trees beside it: BM01's first point is a junction with
        /// cedars whose trunks stand 34 m above the lane.
        /// </param>
        public ScriptedFlight(Vehicle craft, IEnumerable<Vector3> roadPoints, float floor = float.MinValue)
        {
            _craft = craft;
            _points = new List<Vector3>();
            foreach (var p in roadPoints) _points.Add(new Vector3(p.X, p.Y, Math.Max(p.Z + Altitude, floor)));
        }

        public IReadOnlyList<Vector3> Points => _points;
        public int Next => _next;
        public bool Finished => _next >= _points.Count;
        public float Speed { get; private set; }

        /// <summary>
        /// Take the aircraft off physics: no gravity, so the script's velocity is the whole story.
        /// It is also made proof against collisions, and only against collisions. A line flown
        /// by the script can still clip a treetop or a pole, and in Ron's first run that alone
        /// destroyed the Osprey half a second into the chase. Bullets and explosions still land,
        /// so whatever brings it down is the crew.
        /// </summary>
        public void Begin()
        {
            if (_craft == null || !_craft.Exists()) return;
            Function.Call(Hash.SET_ENTITY_HAS_GRAVITY, _craft, false);
            // Bullet, fire, explosion, collision, melee, steam, (unused), drown.
            Function.Call(Hash.SET_ENTITY_PROOFS, _craft, false, false, false, true, false, false, false, false);
            _velocity = Vector3.Zero;
            _lastTick = Game.GameTime;
            _nextLook = 0; _climbUntil = 0; _floorAhead = float.MinValue;
            // The right way along the line from the start: a point it would have to turn back
            // through the trees to reach is one it has already passed.
            var at = _craft.Position;
            while (_next < _points.Count - 1)
            {
                var toPoint = Flat(_points[_next] - at);
                var leg = Flat(_points[_next + 1] - _points[_next]);
                if (toPoint.X * leg.X + toPoint.Y * leg.Y >= 0f) break;
                _next++;
            }
        }

        private static Vector3 Flat(Vector3 v) => new Vector3(v.X, v.Y, 0f);

        /// <summary>
        /// Reads what is ahead: the ground under two points along its heading, and three rays -
        /// center and each wingtip - angled a little down. Something in a ray's way starts a
        /// slow climb that lasts <see cref="ObstacleHoldMs"/>.
        /// </summary>
        private void Look(Vector3 at, Vector3 heading)
        {
            float reach = Math.Max(60f, Speed * 3f);
            float top = float.MinValue;
            foreach (float f in new[] { 0.5f, 1f })
            {
                var ground = GroundBelow?.Invoke(at + heading * (reach * f));
                if (ground.HasValue) top = Math.Max(top, ground.Value);
            }
            _floorAhead = top == float.MinValue ? float.MinValue : top + GroundClearance;
            var right = new Vector3(heading.Y, -heading.X, 0f);
            foreach (int side in new[] { 0, 1, -1 })
            {
                var from = at + right * (WingOffset * side);
                var to = from + heading * reach - new Vector3(0f, 0f, reach * 0.15f);
                if (Blocked != null && Blocked(from, to, _craft)) { _climbUntil = Game.GameTime + ObstacleHoldMs; break; }
            }
        }

        /// <summary>Give it back to the engine, so a downed aircraft falls.</summary>
        public void Release()
        {
            if (_craft == null || !_craft.Exists()) return;
            try
            {
                Function.Call(Hash.SET_ENTITY_HAS_GRAVITY, _craft, true);
                Function.Call(Hash.SET_ENTITY_PROOFS, _craft, false, false, false, false, false, false, false, false);
            }
            catch (Exception ex) { Logger.Warn("Returning a scripted aircraft to physics: " + ex.Message); }
        }

        /// <summary>How far it still has to fly, along the route.</summary>
        public float Remaining(Vector3 from) => RaceRoute.Remaining(_points, _next, from);

        /// <summary>
        /// The speed that keeps it <see cref="LeadMeters"/> ahead of the chase: faster when
        /// they close, slower when they drop back, never outside the bounds.
        /// </summary>
        public static float PacedSpeed(float leadMeters)
        {
            float wanted = 26f + (LeadMeters - leadMeters) * 0.12f;
            return Math.Max(MinSpeed, Math.Min(MaxSpeed, wanted));
        }

        /// <summary>One frame of flight. <paramref name="chaser"/> is where the hunt is.</summary>
        public void Update(Vector3 chaser)
        {
            if (_craft == null || !_craft.Exists() || _craft.IsDead || Finished) return;
            int now = Game.GameTime;
            float dt = _lastTick < 0 ? 0f : Math.Max(0f, Math.Min(.25f, (now - _lastTick) / 1000f));
            _lastTick = now;

            var at = _craft.Position;
            // Reached on the map, whatever height the ground ahead has it flying at.
            while (!Finished && Flat(at - _points[_next]).Length() <= PointReach) _next++;
            if (Finished) return;

            // Lead is measured along the route, not in a straight line: a truck on the far side
            // of a bend is further behind than it looks.
            float lead = RaceRoute.Remaining(_points, _next, chaser) - Remaining(at);
            Speed = PacedSpeed(lead);

            // Along the line on the map, and at the height the line or the ground ahead needs,
            // whichever is higher; climbing hard and slow over anything in its way.
            var toward = Flat(_points[_next] - at);
            float distance = toward.Length();
            var course = distance > .01f ? toward * (1f / distance) : Vector3.Zero;
            if (now >= _nextLook && distance > .01f) { _nextLook = now + LookIntervalMs; Look(at, course); }
            bool climbing = Climbing;
            float targetZ = Math.Max(_points[_next].Z, _floorAhead);
            float vertical = climbing ? ClimbRate : Math.Max(-ClimbRate * 0.6f, Math.Min(ClimbRate, (targetZ - at.Z) * 0.8f));
            var wanted = course * (climbing ? MinSpeed : Speed) + new Vector3(0f, 0f, vertical);
            float blend = Math.Min(1f, Response * dt);
            _velocity = _velocity + (wanted - _velocity) * blend;
            Function.Call(Hash.SET_ENTITY_VELOCITY, _craft, _velocity.X, _velocity.Y, _velocity.Z);

            // Nose where it is going, level, with a little bank into the turn.
            if (_velocity.X * _velocity.X + _velocity.Y * _velocity.Y > 1f)
            {
                float heading = (float)(Math.Atan2(-_velocity.X, _velocity.Y) * 180.0 / Math.PI);
                float turn = Normalize(heading - _craft.Heading);
                float bank = Math.Max(-20f, Math.Min(20f, turn * 0.6f));
                Function.Call(Hash.SET_ENTITY_ROTATION, _craft, -2f, -bank, _craft.Heading + turn * Math.Min(1f, 2.2f * dt), 2, true);
            }
        }

        private static float Normalize(float degrees)
        {
            while (degrees > 180f) degrees -= 360f;
            while (degrees < -180f) degrees += 360f;
            return degrees;
        }
    }
}
