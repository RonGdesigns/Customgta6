using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;

namespace Bloodlines.Core
{
    /// <summary>
    /// What a scene must establish, who is in it, and how it is shot. The director
    /// plays the lines for <c>MissionId:Phase</c> from the authored scene data; the
    /// spec adds the support cast for speakers who are not brothers, the blocking
    /// (actions and camera shots), and the reason the scene exists, which is logged
    /// so a scene that plays a line without performing its action is visible.
    /// </summary>
    public sealed class SceneSpec
    {
        public string MissionId = "";
        public string Phase = "";
        public string Title = "";
        /// <summary>What the player must understand after this scene that they did not before.</summary>
        public string Reason = "";
        /// <summary>Speakers who are not brothers: the name used in the authored lines, and the ped playing them.</summary>
        public readonly Dictionary<string, Ped> Support = new Dictionary<string, Ped>(StringComparer.OrdinalIgnoreCase);
        public SceneBlocking Blocking;
        public Ped ActionActor;
        public Action SceneAction;

        public SceneSpec With(string speaker, Ped ped) { if (ped != null && ped.Exists()) Support[speaker] = ped; return this; }
    }

    /// <summary>
    /// An authored camera shot: where the camera is and what it looks at, each
    /// anchored to an entity (so the shot follows it) or fixed in the world, for a
    /// duration, with an optional slow push. Skipping a shot has nothing to
    /// reproduce; the world is not changed by a camera.
    /// </summary>
    public sealed class ShotStep : SceneStep
    {
        private readonly Entity _fromAnchor, _atAnchor;
        private readonly Vector3 _from, _at, _push;
        private readonly int _durationMs;
        private readonly Action _onStart;

        /// <param name="fromAnchor">Entity the camera position is relative to; null for a world position.</param>
        /// <param name="from">Camera offset from the anchor: forward, right, up in the anchor's frame; or a world position.</param>
        /// <param name="atAnchor">Entity the camera looks at; null for a world point.</param>
        /// <param name="at">Offset from the look-at anchor (up matters most), or a world point.</param>
        /// <param name="push">How far the camera moves along its own offset over the shot (positive is toward the subject).</param>
        public ShotStep(int durationMs, Entity fromAnchor, Vector3 from, Entity atAnchor, Vector3 at, float push = 0f, Action onStart = null)
        {
            _durationMs = durationMs; _fromAnchor = fromAnchor; _from = from; _atAnchor = atAnchor; _at = at;
            _push = new Vector3(push, 0f, 0f); _onStart = onStart; TimeoutMs = durationMs + 500;
        }

        public static ShotStep Wide(int ms, Vector3 point, float back, float up, float side = 0f) => new ShotStep(ms, null, point + new Vector3(side, -back, up), null, point + new Vector3(0f, 0f, 1f));
        public static ShotStep OverShoulder(int ms, Ped behind, Entity subject, float push = 0.4f, Action onStart = null) => new ShotStep(ms, behind, new Vector3(-1.8f, 0.7f, 1.55f), subject, new Vector3(0f, 0f, 1.45f), push, onStart);
        public static ShotStep Low(int ms, Entity subject, float ahead, float side, float up = 0.6f) => new ShotStep(ms, subject, new Vector3(ahead, side, up), subject, new Vector3(0f, 0f, 0.8f));
        public static ShotStep Watching(int ms, Entity from, Entity subject) => new ShotStep(ms, from, new Vector3(-0.6f, 0.9f, 1.6f), subject, new Vector3(0f, 0f, 1f));

        public override Entity CameraTarget => null;

        protected override void OnStart() { RunAction(); }
        public override bool IsComplete => Game.GameTime - StartedAt >= _durationMs;
        /// <summary>A skipped shot still does what the shot did: the action it carries runs once, so skipping leaves the same state as watching.</summary>
        public override void Finish() { if (!HasStarted) RunAction(); }
        private void RunAction() { try { _onStart?.Invoke(); } catch (Exception ex) { Logger.Error("Shot action failed", ex); } }
        public override void Cancel() { }

        /// <summary>Position and aim the scene camera for this frame. True when the shot drove it.</summary>
        public override bool DriveCamera(Camera camera)
        {
            if (camera == null || !camera.Exists()) return false;
            if (_fromAnchor != null && !_fromAnchor.Exists()) return false;
            if (_atAnchor != null && !_atAnchor.Exists()) return false;
            float t = _durationMs <= 0 ? 1f : Math.Max(0f, Math.Min(1f, (Game.GameTime - StartedAt) / (float)_durationMs));
            var offset = _from + _push * t;
            var position = _fromAnchor != null ? Local(_fromAnchor, offset) : offset;
            var target = _atAnchor != null ? Local(_atAnchor, _at) : _at;
            camera.Position = position;
            camera.PointAt(target);
            return true;
        }

        /// <summary>Anchor position plus an offset given as (forward, right, up) in the anchor's heading frame.</summary>
        public static Vector3 Local(Entity anchor, Vector3 offset)
        {
            var forward = anchor.ForwardVector;
            if (forward.X == 0f && forward.Y == 0f) forward = new Vector3(0f, 1f, 0f);
            var right = new Vector3(forward.Y, -forward.X, 0f);
            return anchor.Position + forward * offset.X + right * offset.Y + new Vector3(0f, 0f, offset.Z);
        }
    }

    /// <summary>Put a prop in a ped's hand. Finishing attaches it at once; the prop is the mission's, so cancel leaves it where it is.</summary>
    public sealed class CarryPropStep : SceneStep
    {
        private readonly Prop _prop;
        private readonly Vector3 _offset, _rotation;

        public CarryPropStep(Ped actor, Prop prop, Vector3? offset = null, Vector3? rotation = null)
        {
            Actor = actor; _prop = prop; _offset = offset ?? new Vector3(0.12f, 0.02f, -0.02f); _rotation = rotation ?? new Vector3(0f, 90f, 0f); TimeoutMs = 1500;
        }

        public static bool Attach(Ped actor, Prop prop, Vector3 offset, Vector3 rotation)
        {
            if (actor == null || !actor.Exists() || prop == null || !prop.Exists()) return false;
            try { prop.AttachTo(actor.Bones[Bone.PHRightHand], offset, rotation); return true; }
            catch (Exception ex) { Logger.Error("Prop could not be attached to the hand", ex); return false; }
        }

        protected override void OnStart() { Attach(Actor, _prop, _offset, _rotation); }
        public override bool IsComplete => true;
        public override void Finish() { if (!HasStarted) Attach(Actor, _prop, _offset, _rotation); }
        public override void Cancel() { }
    }

    /// <summary>
    /// A carried prop is set down on another entity: a crate into a truck bed. The
    /// actor turns to it, holds a beat, and the prop leaves the hand for the offset
    /// on the entity. Finishing places it at once; the prop is the mission's, so
    /// cancel leaves it where it is.
    /// </summary>
    public sealed class StowPropStep : SceneStep
    {
        private readonly Prop _prop;
        private readonly Entity _into;
        private readonly Vector3 _offset;
        private readonly int _holdMs;
        private bool _moved;

        public StowPropStep(Ped actor, Prop prop, Entity into, Vector3 offset, int holdMs = 900)
        {
            Actor = actor; _prop = prop; _into = into; _offset = offset; _holdMs = holdMs; TimeoutMs = holdMs + 2000;
        }

        public static bool Stow(Prop prop, Entity into, Vector3 offset)
        {
            if (prop == null || !prop.Exists() || into == null || !into.Exists()) return false;
            try { prop.Detach(); prop.AttachTo(into, offset, Vector3.Zero); return true; }
            catch (Exception ex) { Logger.Error("Prop could not be stowed", ex); return false; }
        }

        protected override void OnStart()
        {
            if (Usable(Actor) && Usable(_into)) Actor.Heading = DriveUpStep.HeadingBetween(Actor.Position, _into.Position);
        }

        public override bool IsComplete
        {
            get
            {
                if (Game.GameTime - StartedAt < _holdMs) return false;
                Move();
                return true;
            }
        }

        private void Move() { if (_moved) return; _moved = true; Stow(_prop, _into, _offset); }
        public override void Finish() { Move(); }
        public override void Cancel() { }
    }

    /// <summary>
    /// A prop moves from one carrier to another with no actor animation: a crate
    /// off the forks and onto a bed. The camera watches the subject given (the
    /// bed, usually). Finishing moves it at once; cancel leaves it where it is.
    /// </summary>
    public sealed class TransferPropStep : SceneStep
    {
        private readonly Prop _prop;
        private readonly Entity _into, _subject;
        private readonly Vector3 _offset;
        private readonly int _holdMs;
        private bool _moved;

        public TransferPropStep(Prop prop, Entity into, Vector3 offset, int holdMs = 1200, Entity subject = null)
        {
            _prop = prop; _into = into; _offset = offset; _holdMs = holdMs; _subject = subject ?? into; TimeoutMs = holdMs + 2000;
        }

        public override Entity CameraTarget => Usable(_subject) ? _subject : null;
        protected override void OnStart() { }
        public override bool IsComplete
        {
            get
            {
                if (Game.GameTime - StartedAt < _holdMs) return false;
                Move();
                return true;
            }
        }
        private void Move() { if (_moved) return; _moved = true; StowPropStep.Stow(_prop, _into, _offset); }
        public override void Finish() { Move(); }
        public override void Cancel() { }
    }

    /// <summary>The prop moves from one hand to another. Finishing moves it at once.</summary>
    public sealed class HandoverStep : SceneStep
    {
        private readonly Ped _to;
        private readonly Prop _prop;
        private readonly int _holdMs;

        public HandoverStep(Ped from, Ped to, Prop prop, int holdMs = 1200)
        {
            Actor = from; _to = to; _prop = prop; _holdMs = holdMs; TimeoutMs = holdMs + 2000;
        }

        protected override void OnStart()
        {
            if (Usable(Actor) && Usable(_to))
            {
                Actor.Heading = DriveUpStep.HeadingBetween(Actor.Position, _to.Position);
                _to.Heading = DriveUpStep.HeadingBetween(_to.Position, Actor.Position);
            }
        }

        public override bool IsComplete
        {
            get
            {
                if (Game.GameTime - StartedAt < _holdMs) return false;
                Move();
                return true;
            }
        }

        private void Move() { CarryPropStep.Attach(_to, _prop, new Vector3(0.12f, 0.02f, -0.02f), new Vector3(0f, 90f, 0f)); }
        public override void Finish() { Move(); }
        public override void Cancel() { }
    }

    /// <summary>Turn to a point and look it over for a moment, as an in-place scenario. Finishing simply ends it.</summary>
    public sealed class InspectStep : SceneStep
    {
        private readonly Vector3 _point;
        private readonly string _scenario;
        private readonly int _durationMs;

        public InspectStep(Ped actor, Vector3 point, int durationMs = 3000, string scenario = "WORLD_HUMAN_CLIPBOARD")
        {
            Actor = actor; _point = point; _durationMs = durationMs; _scenario = scenario; TimeoutMs = durationMs + 1500;
        }

        protected override void OnStart()
        {
            if (!Usable(Actor)) return;
            Actor.Heading = DriveUpStep.HeadingBetween(Actor.Position, _point);
            Actor.Task.StartScenario(_scenario, Actor.Position, Actor.Heading);
        }

        public override bool IsComplete => !Usable(Actor) || Game.GameTime - StartedAt >= _durationMs;
        public override void Finish() { if (Usable(Actor) && HasStarted && !IsComplete) Actor.Task.ClearAllImmediately(); }
    }

    /// <summary>Move to a cover point and stay. Finishing places the actor there.</summary>
    public sealed class TakeCoverStep : SceneStep
    {
        private readonly Vector3 _point;

        public TakeCoverStep(Ped actor, Vector3 point) { Actor = actor; _point = point; TimeoutMs = 12000; }

        protected override void OnStart()
        {
            if (!Usable(Actor)) return;
            Actor.IsPositionFrozen = false;
            Actor.Task.RunTo(_point, false, TimeoutMs);
        }

        public override bool IsComplete => !Usable(Actor) || Actor.Position.DistanceTo(_point) <= 1.5f;

        public override void Finish()
        {
            if (!Usable(Actor) || Actor.Position.DistanceTo(_point) <= 1.5f) return;
            Actor.Task.ClearAllImmediately();
            SettleGround(_point);
            Actor.Position = _point;
        }
    }
}
