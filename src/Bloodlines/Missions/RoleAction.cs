using System;
using GTA;

namespace Bloodlines.Missions
{
    /// <summary>
    /// Mission-owned work performed by an inactive brother. The action owns its
    /// progress; RoleTrack only starts, suspends, resumes and ticks it. That means a
    /// threat or character switch can interrupt the NPC task without silently
    /// resetting the work already achieved.
    /// </summary>
    public abstract class RoleAction
    {
        protected RoleAction(string name)
        {
            Name = string.IsNullOrWhiteSpace(name) ? "work" : name.Trim();
        }

        public string Name { get; }
        public bool Started { get; private set; }
        public bool Suspended { get; private set; }
        public bool Canceled { get; private set; }
        public virtual bool IsComplete { get; protected set; }
        /// <summary>-1 means the action does not expose a percentage.</summary>
        public virtual float Progress => -1f;

        internal void EnsureStarted(Ped actor)
        {
            if (Canceled || IsComplete || Started) return;
            Started = true;
            Suspended = false;
            OnStart(actor);
        }

        internal void Tick(Ped actor)
        {
            if (Canceled || IsComplete || Suspended) return;
            EnsureStarted(actor);
            if (!Canceled && !IsComplete && !Suspended) OnUpdate(actor);
        }

        internal void Suspend(Ped actor)
        {
            if (Canceled || IsComplete || !Started || Suspended) return;
            Suspended = true;
            OnSuspend(actor);
        }

        internal void Resume(Ped actor)
        {
            if (Canceled || IsComplete) return;
            if (!Started) { EnsureStarted(actor); return; }
            if (!Suspended) return;
            Suspended = false;
            OnResume(actor);
        }

        internal void Cancel(Ped actor)
        {
            if (Canceled || IsComplete) return;
            Canceled = true;
            if (Started) OnCancel(actor);
        }

        protected abstract void OnStart(Ped actor);
        protected abstract void OnUpdate(Ped actor);
        protected virtual void OnSuspend(Ped actor) { }
        protected virtual void OnResume(Ped actor) { }
        protected virtual void OnCancel(Ped actor) { }
    }

    /// <summary>
    /// Adapter for mission-specific work without creating another objective type.
    /// Real mission logic supplies the physical start/update behavior and the done
    /// predicate; progress can come from a device, animation or authored state.
    /// </summary>
    public sealed class DelegateRoleAction : RoleAction
    {
        private readonly Action<Ped> _start;
        private readonly Action<Ped> _update;
        private readonly Action<Ped> _suspend;
        private readonly Action<Ped> _resume;
        private readonly Action<Ped> _cancel;
        private readonly Func<bool> _done;
        private readonly Func<float> _progress;

        public DelegateRoleAction(string name, Action<Ped> start, Action<Ped> update,
            Func<bool> done, Func<float> progress = null, Action<Ped> suspend = null,
            Action<Ped> resume = null, Action<Ped> cancel = null) : base(name)
        {
            _start = start;
            _update = update;
            _done = done;
            _progress = progress;
            _suspend = suspend;
            _resume = resume;
            _cancel = cancel;
        }

        public override bool IsComplete
        {
            get => base.IsComplete || (_done?.Invoke() ?? false);
            protected set => base.IsComplete = value;
        }

        public override float Progress
        {
            get
            {
                if (_progress == null) return -1f;
                float value = _progress();
                if (float.IsNaN(value) || float.IsInfinity(value)) return -1f;
                return Math.Max(0f, Math.Min(1f, value));
            }
        }

        protected override void OnStart(Ped actor) { _start?.Invoke(actor); }
        protected override void OnUpdate(Ped actor) { _update?.Invoke(actor); }
        protected override void OnSuspend(Ped actor) { _suspend?.Invoke(actor); }
        protected override void OnResume(Ped actor) { _resume?.Invoke(actor); }
        protected override void OnCancel(Ped actor) { _cancel?.Invoke(actor); }
    }
}
