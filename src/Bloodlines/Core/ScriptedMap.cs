using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// Map the game does not place on its own. Some structures exist in the
    /// installed archives but carry the scripted flag, so nothing of them is in the
    /// world until something asks for them by name. The yacht anchored in Paleto
    /// Cove is one: its hull and its interior are both script-loaded.
    ///
    /// This owns such a request for as long as one mission needs it. Two rules it
    /// exists to keep:
    ///
    ///  * Release exactly what we turned on. If a place was already active when we
    ///    arrived, something else owns it and we leave it alone. Removing another
    ///    owner's structure deletes the ground a player is standing on.
    ///  * Release on every exit. A mission that passes, fails, is aborted or torn
    ///    down gives the world back the way it found it, the same contract the
    ///    atmosphere and apartment overrides already follow.
    ///
    /// Requesting is not instant: the structure streams. <see cref="Ready"/> only
    /// says the game reports each name active, which is why callers wait on it
    /// before placing anyone.
    /// </summary>
    public sealed class ScriptedMap
    {
        private readonly string[] _names;
        private readonly HashSet<string> _owned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private int _requestedAt;
        private bool _released;

        public ScriptedMap(params string[] names)
        {
            if (names == null || names.Length == 0) throw new ArgumentException("A scripted map needs at least one name.", nameof(names));
            _names = (string[])names.Clone();
        }

        public IReadOnlyList<string> Names => Array.AsReadOnly(_names);
        /// <summary>The names this instance turned on, and is therefore responsible for.</summary>
        public IEnumerable<string> Owned => _owned;
        public bool Requested => _requestedAt != 0;

        /// <summary>
        /// Ask for every name. Safe to call repeatedly: the engine drops a duplicate
        /// request, and ownership is decided once, on the first pass, before anything
        /// of ours could have made a name active.
        /// </summary>
        public void Request()
        {
            if (_released) throw new InvalidOperationException("A released scripted map cannot be requested again.");
            bool first = _requestedAt == 0;
            if (first) _requestedAt = Game.GameTime;
            foreach (var name in _names)
            {
                try
                {
                    if (first && !Function.Call<bool>(Hash.IS_IPL_ACTIVE, name)) _owned.Add(name);
                    Function.Call(Hash.REQUEST_IPL, name);
                }
                catch (Exception ex) { Logger.Error("Requesting map " + name, ex); }
            }
            if (first) Logger.Info("Scripted map requested: " + string.Join(", ", _names) +
                (_owned.Count == _names.Length ? "" : " (some were already active and stay under their own owner)"));
        }

        /// <summary>True once the game reports every requested name active.</summary>
        public bool Ready
        {
            get
            {
                if (!Requested || _released) return false;
                foreach (var name in _names)
                {
                    bool active;
                    try { active = Function.Call<bool>(Hash.IS_IPL_ACTIVE, name); }
                    catch (Exception ex) { Logger.Error("Checking map " + name, ex); return false; }
                    if (!active) return false;
                }
                return true;
            }
        }

        /// <summary>How long the wait has run, for a caller that gives up and fails cleanly.</summary>
        public int WaitedMilliseconds => Requested ? Game.GameTime - _requestedAt : 0;

        /// <summary>
        /// Hand the world back. Only the names this instance turned on are removed,
        /// and only once, however many times teardown paths call it.
        /// </summary>
        public void Release()
        {
            if (_released) return;
            _released = true;
            foreach (var name in _owned)
            {
                try { Function.Call(Hash.REMOVE_IPL, name); }
                catch (Exception ex) { Logger.Error("Releasing map " + name, ex); }
            }
            if (_owned.Count > 0) Logger.Info("Scripted map released: " + string.Join(", ", _owned));
            _owned.Clear();
        }
    }
}
