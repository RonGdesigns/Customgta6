using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;

namespace Bloodlines.Core
{
    /// <summary>
    /// One thing an actor does during a scene, with a completion signal and an
    /// instant equivalent for skipping. The rule that shapes every step: watching
    /// the scene and skipping it must leave the world in the same state. A step
    /// therefore has to know how to finish itself immediately (<see cref="Finish"/>),
    /// not merely stop.
    /// </summary>
    public abstract class SceneStep
    {
        /// <summary>The ped performing this step; null for camera-only or wait steps.</summary>
        public Ped Actor { get; protected set; }

        /// <summary>Where the scene camera should look while this step runs; null keeps the speaker framing.</summary>
        public virtual Entity CameraTarget => Actor;

        /// <summary>Longest this step may run before the scene moves on with the finished state.</summary>
        public int TimeoutMs { get; set; } = 15000;

        public int StartedAt { get; private set; } = -1;
        public bool HasStarted => StartedAt >= 0;
        public bool TimedOut => HasStarted && Game.GameTime - StartedAt > TimeoutMs;

        public void Start()
        {
            StartedAt = Game.GameTime;
            try { OnStart(); }
            catch (Exception ex) { Logger.Error("Scene step failed to start: " + GetType().Name, ex); }
        }

        protected abstract void OnStart();

        /// <summary>True once the action is visibly done — in the vehicle, at the mark, phone away.</summary>
        public abstract bool IsComplete { get; }

        /// <summary>
        /// Put the world in this step's documented end state right now. "Finished"
        /// means the state is already true when this returns, not that a task has
        /// been requested. Used for a deliberate skip and for a timed-out step.
        /// </summary>
        public abstract void Finish();

        /// <summary>
        /// Stop safely without reaching the end state: a mission abort, an error, or
        /// teardown. The actor keeps its current position and simply stops doing
        /// what the step asked.
        /// </summary>
        public virtual void Cancel()
        {
            if (HasStarted && Usable(Actor)) Actor.Task.ClearAll();
        }

        protected static void SettleGround(Vector3 point)
        {
            GTA.Native.Function.Call(GTA.Native.Hash.REQUEST_COLLISION_AT_COORD, point.X, point.Y, point.Z);
        }

        protected static bool Usable(Entity entity) => entity != null && entity.Exists();
        protected static bool Usable(Ped ped) => ped != null && ped.Exists() && !ped.IsDead;
    }

    /// <summary>Walk (or run) to a point. Finishing places the actor on the mark.</summary>
    public sealed class WalkToStep : SceneStep
    {
        private readonly Vector3 _point;
        private readonly float _radius;
        private readonly bool _run;

        public WalkToStep(Ped actor, Vector3 point, float radius = 1.2f, bool run = false)
        {
            Actor = actor; _point = point; _radius = radius; _run = run;
        }

        protected override void OnStart()
        {
            if (!Usable(Actor)) return;
            Actor.IsPositionFrozen = false;
            if (_run) Actor.Task.RunTo(_point, false, TimeoutMs);
            else Actor.Task.GoTo(_point);
        }

        public override bool IsComplete => !Usable(Actor) || Actor.Position.DistanceTo(_point) <= _radius;

        public override void Finish()
        {
            if (!Usable(Actor)) return;
            if (Actor.Position.DistanceTo(_point) <= _radius) return;
            Actor.Task.ClearAllImmediately();
            SettleGround(_point);
            Actor.Position = _point;
        }
    }

    /// <summary>Open the door and get in. Finishing warps the actor into the seat.</summary>
    public sealed class EnterVehicleStep : SceneStep
    {
        private readonly Vehicle _vehicle;
        private readonly VehicleSeat _seat;

        public EnterVehicleStep(Ped actor, Vehicle vehicle, VehicleSeat seat = VehicleSeat.Driver)
        {
            Actor = actor; _vehicle = vehicle; _seat = seat;
        }

        public override Entity CameraTarget => Usable(_vehicle) ? (Entity)_vehicle : Actor;

        protected override void OnStart()
        {
            if (!Usable(Actor) || !Usable(_vehicle)) return;
            Actor.IsPositionFrozen = false;
            _vehicle.IsPositionFrozen = false;
            Actor.Task.EnterVehicle(_vehicle, _seat, TimeoutMs, 1f, EnterVehicleFlags.None);
        }

        public override bool IsComplete => !Usable(Actor) || !Usable(_vehicle) || Actor.IsInVehicle(_vehicle);

        public override void Finish()
        {
            if (!Usable(Actor) || !Usable(_vehicle)) return;
            if (!Actor.IsInVehicle(_vehicle)) { Actor.Task.ClearAllImmediately(); Actor.SetIntoVehicle(_vehicle, _seat); }
        }
    }

    /// <summary>
    /// Get out. Finishing leaves the actor already standing beside the vehicle —
    /// warped out, placed on free ground on the driver's side, facing the way the
    /// car faces — not partway through a door animation.
    /// </summary>
    public sealed class ExitVehicleStep : SceneStep
    {
        public ExitVehicleStep(Ped actor)
        {
            Actor = actor;
        }

        /// <summary>Where a skip puts the actor: beside the vehicle, on ground the navmesh accepts.</summary>
        public static Vector3 SafeSpotBeside(Vehicle vehicle)
        {
            var forward = vehicle.ForwardVector;
            var left = new Vector3(-forward.Y, forward.X, 0f);
            var beside = vehicle.Position + left * 2.2f;
            var safe = World.GetSafeCoordForPed(beside, false, 0);
            return safe != Vector3.Zero && safe.DistanceTo(beside) <= 8f ? safe : beside;
        }

        protected override void OnStart()
        {
            if (!Usable(Actor) || !Actor.IsInVehicle()) return;
            var vehicle = Actor.CurrentVehicle;
            Actor.IsPositionFrozen = false;
            if (vehicle != null && vehicle.Exists()) vehicle.IsPositionFrozen = false;
            Actor.Task.LeaveVehicle();
        }

        public override bool IsComplete => !Usable(Actor) || !Actor.IsInVehicle();

        public override void Finish()
        {
            if (!Usable(Actor) || !Actor.IsInVehicle()) return;
            var vehicle = Actor.CurrentVehicle;
            var spot = vehicle != null && vehicle.Exists() ? SafeSpotBeside(vehicle) : Actor.Position;
            float heading = vehicle != null && vehicle.Exists() ? vehicle.Heading : Actor.Heading;
            Actor.Task.ClearAllImmediately();
            Actor.Task.LeaveVehicle(LeaveVehicleFlags.WarpOut);
            SettleGround(spot);
            Actor.Position = spot;
            Actor.Heading = heading;
            if (Actor.IsInVehicle()) Logger.Error("ExitVehicleStep.Finish: the actor is still seated after a warp-out; the next step may misbehave.");
        }
    }

    /// <summary>Check the phone for a moment. Finishing simply puts it away.</summary>
    public sealed class UsePhoneStep : SceneStep
    {
        private readonly int _durationMs;

        public UsePhoneStep(Ped actor, int durationMs = 2500)
        {
            Actor = actor; _durationMs = durationMs; TimeoutMs = durationMs + 4000;
        }

        protected override void OnStart()
        {
            if (!Usable(Actor)) return;
            Actor.Task.UseMobilePhone(_durationMs);
        }

        public override bool IsComplete => !Usable(Actor) || Game.GameTime - StartedAt >= _durationMs;

        public override void Finish()
        {
            // Phone away, now: the immediate clear, not the queued one.
            if (Usable(Actor) && !IsComplete) Actor.Task.ClearAllImmediately();
        }
    }

    /// <summary>Turn to face something for a beat. Finishing is immediate; a look has no end state to reproduce.</summary>
    public sealed class LookAtStep : SceneStep
    {
        private readonly Entity _target;
        private readonly int _durationMs;

        public LookAtStep(Ped actor, Entity target, int durationMs = 1500)
        {
            Actor = actor; _target = target; _durationMs = durationMs; TimeoutMs = durationMs + 2000;
        }

        protected override void OnStart()
        {
            if (!Usable(Actor) || !Usable(_target)) return;
            Actor.Task.LookAt(_target, _durationMs);
        }

        public override bool IsComplete => !Usable(Actor) || Game.GameTime - StartedAt >= _durationMs;

        public override void Finish()
        {
        }
    }

    /// <summary>Hold a beat, optionally on a camera subject. Nothing to reproduce on skip.</summary>
    public sealed class WaitStep : SceneStep
    {
        private readonly int _durationMs;
        private readonly Entity _subject;

        public WaitStep(int durationMs, Entity subject = null)
        {
            _durationMs = durationMs; _subject = subject; TimeoutMs = durationMs + 1000;
        }

        public override Entity CameraTarget => _subject;

        protected override void OnStart()
        {
        }

        public override bool IsComplete => Game.GameTime - StartedAt >= _durationMs;

        public override void Finish()
        {
        }
    }

    /// <summary>
    /// A queue of steps the scene director runs beside the dialogue. Steps start
    /// when the previous one completes or times out; the scene camera follows the
    /// current step's subject. <see cref="Complete"/> finishes every remaining step
    /// instantly, which is what a skip calls.
    /// </summary>
    public sealed class SceneBlocking
    {
        private readonly List<SceneStep> _steps = new List<SceneStep>();
        private int _index;

        /// <summary>Set by <see cref="Cancel"/>: the blocking stopped without reaching its end state.</summary>
        public bool Canceled { get; private set; }

        public SceneBlocking Then(SceneStep step)
        {
            if (step != null) _steps.Add(step);
            return this;
        }

        public IReadOnlyList<SceneStep> Steps => _steps;
        public SceneStep Current => _index < _steps.Count ? _steps[_index] : null;
        public bool IsFinished => _index >= _steps.Count;

        /// <summary>Every ped that moves during the scene: the director leaves these unfrozen.</summary>
        public IEnumerable<Ped> Actors => _steps.Select(s => s.Actor).Where(a => a != null).Distinct();

        public void Update()
        {
            var step = Current;
            if (step == null) return;
            if (!step.HasStarted) { step.Start(); return; }
            if (!step.IsComplete && !step.TimedOut) return;
            if (step.TimedOut && !step.IsComplete)
            {
                Logger.Warn("Scene step timed out; finishing it instantly: " + step.GetType().Name);
                step.Finish();
            }
            _index++;
        }

        /// <summary>Finish everything that has not happened yet, in order, so a skip lands on the same state.</summary>
        public void Complete()
        {
            for (; _index < _steps.Count; _index++)
            {
                var step = _steps[_index];
                try { if (!step.HasStarted || !step.IsComplete) step.Finish(); }
                catch (Exception ex) { Logger.Error("Scene step finish failed: " + step.GetType().Name, ex); }
            }
        }

        /// <summary>
        /// Stop without finishing. The running step is told to stand down; nothing
        /// after it runs. This is what an abort, an error or a teardown calls — a
        /// scene that broke halfway through a walk must not warp the actor into the
        /// car as if the player had chosen to skip it.
        /// </summary>
        public void Cancel()
        {
            var step = Current;
            if (step != null)
            {
                try { step.Cancel(); }
                catch (Exception ex) { Logger.Error("Scene step cancel failed: " + step.GetType().Name, ex); }
            }
            Canceled = true;
            _index = _steps.Count;
        }
    }
}
