using System;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// An MLO interior a mission needs to be able to walk into, held open for as long
    /// as that mission runs.
    ///
    /// <see cref="ScriptedMap"/> asks for the ymap, which places the interior's shell in
    /// the world. That is not the same as the interior existing: an MLO's rooms and
    /// portals are streamed separately, and until the interior is pinned the doorway
    /// into it is a wall. The Paleto yacht had the map request and nothing else, which
    /// is why Ron could board the vessel at the stern and never get inside it, and why
    /// M46's first marker sat in a room he could not reach.
    ///
    /// The apartments have done this correctly since the prologue —
    /// <see cref="ApartmentAccess"/> un-disables, un-caps, pins and refreshes, then waits
    /// on <c>IS_INTERIOR_READY</c>. This is that sequence with the same ownership rule
    /// the scripted map already follows: whatever state the interior was found in is put
    /// back, and an interior somebody else had already opened is left to them.
    /// </summary>
    public sealed class ScriptedInterior
    {
        private readonly Vector3[] _probes;
        private readonly string _what;
        private int _interior;
        private bool _wasDisabled, _wasCapped, _pinned, _released, _announced;

        /// <param name="what">What to call it in the log.</param>
        /// <param name="probes">
        /// Points to look for the interior at, in order. More than one because the
        /// instance origin is an estimate like every other coordinate here: if the first
        /// point is a meter outside a bulkhead the game answers zero, and a second point
        /// deeper inside costs one native call.
        /// </param>
        public ScriptedInterior(string what, params Vector3[] probes)
        {
            if (probes == null || probes.Length == 0) throw new ArgumentException("An interior needs somewhere to look for it.", nameof(what));
            _what = what; _probes = (Vector3[])probes.Clone();
        }

        /// <summary>The interior the game found, or zero while it has not been located.</summary>
        public int Id => _interior;
        /// <summary>Whether the interior is located and the game says its contents are in.</summary>
        public bool Ready
        {
            get
            {
                if (_interior == 0) return false;
                try { return Function.Call<bool>(Hash.IS_INTERIOR_READY, _interior); }
                catch { return false; }
            }
        }

        /// <summary>
        /// Find it and hold it open. Safe to call every frame: it looks until it finds
        /// something and then does nothing. Returns whether the interior is open and in.
        /// </summary>
        public bool Ensure()
        {
            if (_released) return false;
            if (_interior == 0)
            {
                foreach (var probe in _probes)
                {
                    try
                    {
                        _interior = Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, probe.X, probe.Y, probe.Z);
                        if (_interior != 0) break;
                    }
                    catch (Exception ex) { Logger.Warn("Looking for the " + _what + " interior at " + probe + ": " + ex.Message); }
                }
                // Not an error. The map is still streaming in on the frames right after the
                // request, and this is called again next frame.
                if (_interior == 0) return false;
            }
            if (!_pinned)
            {
                try
                {
                    _wasDisabled = Function.Call<bool>(Hash.IS_INTERIOR_DISABLED, _interior);
                    _wasCapped = Function.Call<bool>(Hash.IS_INTERIOR_CAPPED, _interior);
                    if (_wasDisabled) Function.Call(Hash.DISABLE_INTERIOR, _interior, false);
                    if (_wasCapped) Function.Call(Hash.CAP_INTERIOR, _interior, false);
                    Function.Call(Hash.PIN_INTERIOR_IN_MEMORY, _interior);
                    Function.Call(Hash.REFRESH_INTERIOR, _interior);
                    _pinned = true;
                    Logger.Info("Interior " + _interior + " (" + _what + ") pinned" +
                        (_wasDisabled ? ", it was disabled" : "") + (_wasCapped ? ", it was capped" : "") + ".");
                }
                catch (Exception ex) { Logger.Error("Pinning the " + _what + " interior", ex); return false; }
            }
            bool ready = Ready;
            if (ready && !_announced) { _announced = true; Logger.Info("Interior " + _interior + " (" + _what + ") is in and walkable."); }
            return ready;
        }

        /// <summary>
        /// Whether the game reports this interior at a point — the honest way to ask
        /// whether a marker a player has to reach is inside the part of the ship that
        /// actually loaded.
        /// </summary>
        public bool Covers(Vector3 point)
        {
            if (_interior == 0) return false;
            try { return Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, point.X, point.Y, point.Z) == _interior; }
            catch { return false; }
        }

        /// <summary>
        /// Let it go, and put back whatever it was found as. Called on pass, failure,
        /// abort and teardown alike, the same contract the scripted map keeps.
        /// </summary>
        public void Release()
        {
            if (_released) return;
            _released = true;
            if (_interior == 0 || !_pinned) return;
            Attempt(() => Function.Call(Hash.UNPIN_INTERIOR, _interior));
            if (_wasCapped) Attempt(() => Function.Call(Hash.CAP_INTERIOR, _interior, true));
            if (_wasDisabled) Attempt(() => Function.Call(Hash.DISABLE_INTERIOR, _interior, true));
            Logger.Info("Interior " + _interior + " (" + _what + ") released.");
        }

        private void Attempt(Action action)
        {
            try { action(); } catch (Exception ex) { Logger.Error("Releasing the " + _what + " interior", ex); }
        }
    }
}
