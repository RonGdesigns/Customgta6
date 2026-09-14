using System;
using System.Collections.Generic;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// Owns only a script timecycle acquired from an empty slot. This is not a
    /// renderer, a mod priority stack, or proof of an artistically valid preset.
    /// An equal index cannot identify a different writer of the same modifier.
    /// </summary>
    public sealed class TimecycleGrade
    {
        public const float DefaultFadeSeconds = 1.0f;
        private readonly ITimecyclePort _port;
        private readonly Action<string> _log;
        private readonly HashSet<string> _rejected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private int _owned = -1, _lastTime;
        private bool _hasClock, _faulted;
        private float _from, _goal, _elapsed;
        public string Name { get; private set; }
        public float Strength { get; private set; }
        public string Status { get; private set; } = "idle";
        public bool Faulted => _faulted;
        public float FadeSeconds { get; set; } = DefaultFadeSeconds;

        public TimecycleGrade(ITimecyclePort port, Action<string> log = null)
        {
            _port = port ?? throw new ArgumentNullException(nameof(port));
            _log = log;
        }

        public void Update(string requested, float strength, int now, bool suspended, bool paused)
        {
            float dt = Delta(now, paused);
            if (_faulted) return;
            string target = string.IsNullOrWhiteSpace(requested) ? null : requested.Trim();
            float desired = FiniteUnit(strength);
            if (desired == 0f) target = null;
            try
            {
                int active = _port.ActiveIndex;
                int transition = _port.TransitionIndex;
                if (_owned >= 0 && (active != _owned || transition >= 0))
                {
                    Forget();
                    Status = "yielded to another timecycle";
                }
                if (suspended)
                {
                    ReleaseCore();
                    Status = "suspended by scene, interior, ability or recovery";
                    return;
                }
                if (paused) { Status = "paused"; return; }
                if (_owned < 0)
                {
                    if (active >= 0 || transition >= 0) { Status = "waiting for another timecycle"; return; }
                    if (target == null) { Status = "no configured grade"; return; }
                    if (_rejected.Contains(target)) { Status = "rejected name: " + target; return; }
                    _port.Set(target);
                    int accepted = _port.ActiveIndex;
                    if (accepted < 0)
                    {
                        _rejected.Add(target);
                        Status = "rejected name: " + target;
                        _log?.Invoke("Visuals: no active index after requesting '" + target + "'; leaving grading idle. Check installed timecycle data; retry requires reload.");
                        return;
                    }
                    // Never erase an incoming transition, even if it arrived in
                    // the short interval between the preceding read and Set.
                    if (_port.TransitionIndex >= 0) { Status = "yielded to another transition"; return; }
                    _owned = accepted; Name = target;
                    Strength = _from = _elapsed = 0f; _goal = desired;
                    _port.SetStrength(0f);
                    Status = "fading in: " + target;
                    _log?.Invoke("Visuals: engine accepted '" + target + "' at index " + accepted + "; visible quality is not verified.");
                    return;
                }

                bool same = string.Equals(Name, target, StringComparison.OrdinalIgnoreCase);
                float destination = same ? desired : 0f;
                if (Math.Abs(_goal - destination) > 0.00001f)
                {
                    _from = Strength; _goal = destination; _elapsed = 0f;
                }
                float seconds = FadeSeconds;
                if (float.IsNaN(seconds) || float.IsInfinity(seconds)) seconds = DefaultFadeSeconds;
                seconds = Math.Max(0.05f, Math.Min(10f, seconds));
                _elapsed = Math.Min(seconds, _elapsed + dt);
                float t = _elapsed / seconds;
                float eased = t * t * (3f - 2f * t);
                float next = _elapsed >= seconds ? _goal : _from + (_goal - _from) * eased;
                if (Math.Abs(next - Strength) > 0.00001f)
                {
                    _port.SetStrength(next);
                    Strength = next;
                }
                Status = same ? (_elapsed >= seconds ? "active: " : "fading in: ") + Name : "fading out: " + Name;
                // A changed name fades THROUGH neutral. It is not an unverified
                // two-modifier crossfade. The next update acquires the next name.
                if (!same && Strength <= 0.00001f && _elapsed >= seconds)
                {
                    ReleaseCore();
                    Status = "neutral between grades";
                }
            }
            catch (Exception ex)
            {
                // A native failure must not flood retries or keep writing the
                // global slot. Attempt cleanup only with evidence of ownership.
                try { ReleaseCore(); } catch { Forget(); }
                _faulted = true; Status = "grading disabled after native error";
                _log?.Invoke("Visuals: grading disabled for this script instance: " + ex.Message);
            }
        }

        public void Release()
        {
            _hasClock = false;
            try { ReleaseCore(); if (!_faulted) Status = "released"; }
            catch (Exception ex)
            {
                Forget(); _faulted = true; Status = "grading disabled after release error";
                _log?.Invoke("Visuals: grade release failed; reload and inspect the game state: " + ex.Message);
            }
        }

        private void ReleaseCore()
        {
            // No unconditional clear, no restoring an unknown previous name,
            // and no popping a stack owned by another script.
            if (_owned >= 0 && _port.ActiveIndex == _owned && _port.TransitionIndex < 0) _port.Clear();
            Forget();
        }

        private void Forget() { _owned = -1; Name = null; Strength = _from = _goal = _elapsed = 0f; }
        private float Delta(int now, bool paused)
        {
            uint ms = _hasClock ? unchecked((uint)(now - _lastTime)) : 0u;
            _lastTime = now; _hasClock = true;
            // Handles signed GameTime wrap. Discontinuous clocks/load stalls do
            // not jump a fade to completion; ordinary elapsed time is bounded.
            return paused || ms > 1000u ? 0f : Math.Min(0.25f, ms / 1000f);
        }
        private static float FiniteUnit(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : Math.Max(0f, Math.Min(1f, value));
    }

    public interface ITimecyclePort
    {
        int ActiveIndex { get; }
        int TransitionIndex { get; }
        void Set(string name);
        void SetStrength(float strength);
        void Clear();
    }

    internal sealed class NativeTimecyclePort : ITimecyclePort
    {
        public int ActiveIndex => Function.Call<int>(Hash.GET_TIMECYCLE_MODIFIER_INDEX);
        public int TransitionIndex => Function.Call<int>(Hash.GET_TIMECYCLE_TRANSITION_MODIFIER_INDEX);
        public void Set(string name) => Function.Call(Hash.SET_TIMECYCLE_MODIFIER, name);
        public void SetStrength(float strength) => Function.Call(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH, strength);
        public void Clear() => Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);
    }
}
