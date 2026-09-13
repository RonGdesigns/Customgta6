using System;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>Physical cargo attachment and descent. Never moves cargo to a landing coordinate.</summary>
    public sealed class SubmarineAirdrop
    {
        private readonly Vehicle _plane, _sub;
        private readonly Ped _pilot;
        private readonly IMarineProbe _probe;
        private Prop[] _canopies;
        private int _releasedAt, _wetSince = -1, _probeAt;
        private bool _waterSafe;
        private float _surface;
        public bool Released { get; private set; }
        public bool Floating { get; private set; }
        public string Failure { get; private set; }
        public SubmarineAirdrop(Vehicle plane, Vehicle sub, Ped pilot, IMarineProbe probe = null)
        { _plane = plane; _sub = sub; _pilot = pilot; _probe = probe ?? MarineSites.Native; }
        private bool Attached(Entity child, Entity parent) => child != null && child.Exists() && parent != null && parent.Exists() && Function.Call<bool>(Hash.IS_ENTITY_ATTACHED_TO_ENTITY, child, parent);
        public bool Secure()
        {
            if (_plane == null || !_plane.Exists() || _sub == null || !_sub.Exists()) return false;
            Function.Call(Hash.SET_ENTITY_COLLISION, _sub, false, false);
            // The Kraken does not fit the Titan's cargo bay: use an external ventral cradle.
            _sub.AttachTo(_plane, new Vector3(0, -1.5f, -5f), Vector3.Zero);
            if (Attached(_sub, _plane)) return true;
            Cleanup(); return false;
        }
        public bool Release(Prop first, Prop second)
        {
            if (Released || !Attached(_sub, _plane) || first == null || second == null || !first.Exists() || !second.Exists()) return false;
            _canopies = new[] { first, second };
            for (int i = 0; i < 2; i++)
            {
                Function.Call(Hash.SET_ENTITY_COLLISION, _canopies[i], false, false);
                _canopies[i].IsPositionFrozen = false;
                _canopies[i].AttachTo(_sub, new Vector3(0, i == 0 ? -2.5f : 2.5f, 7f), Vector3.Zero);
                if (!Attached(_canopies[i], _sub)) { RemoveCanopies(); return false; }
            }
            var velocity = _plane.Velocity;
            _sub.Detach();
            if (Attached(_sub, _plane)) { RemoveCanopies(); return false; }
            Function.Call(Hash.SET_ENTITY_COLLISION, _sub, true, true);
            _sub.IsPositionFrozen = false;
            _sub.Velocity = new Vector3(velocity.X * .2f, velocity.Y * .2f, -4f);
            _releasedAt = Game.GameTime; Released = true; return true;
        }
        public void Update()
        {
            if (Failure != null || Floating) return;
            if (_sub == null || !_sub.Exists() || _sub.IsDead) { Failure = "The cargo submarine was destroyed."; return; }
            if (_pilot == null || !_pilot.Exists() || _pilot.IsDead || !_pilot.IsInVehicle(_sub)) { Failure = "Gohan left the cargo submarine before splashdown."; return; }
            if (!Released)
            { if (!Attached(_sub, _plane)) Failure = "The submarine came loose from its flight cradle."; return; }
            if (_canopies == null || !Attached(_canopies[0], _sub) || !Attached(_canopies[1], _sub)) { Failure = "A cargo parachute failed before splashdown."; return; }
            if (Game.GameTime - _releasedAt > 90000) { Failure = "The sub did not complete its descent. Retry the water drop corridor."; return; }
            var p = _sub.Position;
            var v = _sub.Velocity;
            // The game supplies displacement and collision; only canopy drag is simulated.
            float drag = Math.Max(0, 1f - Game.LastFrameTime * .7f);
            _sub.Velocity = new Vector3(v.X * drag, v.Y * drag, Math.Max(-8f, v.Z));
            Function.Call(Hash.REQUEST_COLLISION_AT_COORD, p.X, p.Y, p.Z);
            if (Game.GameTime >= _probeAt)
            {
                _probeAt = Game.GameTime + 750;
                Function.Call(Hash.SET_FOCUS_POS_AND_VEL, p.X, p.Y, p.Z, 0f, 0f, -8f);
                Vector3 water; string reason;
                _waterSafe = MarineSites.TryResolve(p, 6f, 3.5f, 5f, _sub.Heading, 0, out water, out reason, _probe);
                _surface = _probe.Column(p).Surface;
            }
            if (p.Z < _surface - 5f || (p.Z < _surface + 4f && !_waterSafe))
            { Failure = "The sub missed safe deep water. Release over the offshore corridor."; return; }
            bool wet = _waterSafe && Math.Abs(p.Z - _surface) < 3f && _sub.IsInWater &&
                Math.Abs(v.Z) < 3f && Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY, _sub);
            if (!wet) { _wetSince = -1; return; }
            if (_wetSince < 0) _wetSince = Game.GameTime;
            if (Game.GameTime - _wetSince < 1500) return;
            Floating = true; RemoveCanopies(); Function.Call(Hash.CLEAR_FOCUS);
        }
        private void RemoveCanopies()
        { if (_canopies != null) foreach (var canopy in _canopies) if (canopy != null && canopy.Exists()) { canopy.Detach(); canopy.Delete(); } _canopies = null; }
        public void Cleanup()
        {
            RemoveCanopies();
            Function.Call(Hash.CLEAR_FOCUS);
            if (_sub != null && _sub.Exists()) { _sub.Detach(); Function.Call(Hash.SET_ENTITY_COLLISION, _sub, true, true); _sub.IsPositionFrozen = false; }
        }
    }
}
