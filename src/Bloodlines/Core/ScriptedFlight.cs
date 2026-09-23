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

        private readonly Vehicle _craft;
        private readonly List<Vector3> _points;
        private int _next;
        private Vector3 _velocity;
        private int _lastTick = -1;

        public ScriptedFlight(Vehicle craft, IEnumerable<Vector3> roadPoints)
        {
            _craft = craft;
            _points = new List<Vector3>();
            foreach (var p in roadPoints) _points.Add(p + new Vector3(0f, 0f, Altitude));
        }

        public IReadOnlyList<Vector3> Points => _points;
        public int Next => _next;
        public bool Finished => _next >= _points.Count;
        public float Speed { get; private set; }

        /// <summary>Take the aircraft off physics: no gravity, so the script's velocity is the whole story.</summary>
        public void Begin()
        {
            if (_craft == null || !_craft.Exists()) return;
            Function.Call(Hash.SET_ENTITY_HAS_GRAVITY, _craft, false);
            _velocity = Vector3.Zero;
            _lastTick = Game.GameTime;
        }

        /// <summary>Give it back to the engine, so a downed aircraft falls.</summary>
        public void Release()
        {
            if (_craft == null || !_craft.Exists()) return;
            try { Function.Call(Hash.SET_ENTITY_HAS_GRAVITY, _craft, true); }
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
            while (!Finished && at.DistanceTo(_points[_next]) <= PointReach) _next++;
            if (Finished) return;

            // Lead is measured along the route, not in a straight line: a truck on the far side
            // of a bend is further behind than it looks.
            float lead = RaceRoute.Remaining(_points, _next, chaser) - Remaining(at);
            Speed = PacedSpeed(lead);

            var toward = _points[_next] - at;
            float distance = toward.Length();
            var wanted = distance > .01f ? toward * (Speed / distance) : Vector3.Zero;
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
