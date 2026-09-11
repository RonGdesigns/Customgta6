using System;
using System.Collections.Generic;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>Frame-driven apartment entry. Never release a player into unready collision.</summary>
    public sealed class ApartmentAccess
    {
        private readonly CrewRoster _crew;
        private readonly List<CrewSlot> _held = new List<CrewSlot>();
        private Ped _ped;
        private Vector3 _origin, _target, _probe;
        private string _waiting;
        private float _heading;
        private bool _frozen, _invincible, _control, _entering, _moved;
        private bool _wasDisabled, _wasCapped, _refaded;
        private float? _heading2;
        private int _started, _interior;
        private string _ipl;
        private bool _ownsIpl;
        public bool Busy { get; private set; }
        public bool Inside { get; private set; }
        public Vector3 ExitPosition { get; private set; }
        public Vector3 InteriorPosition { get; private set; }
        public ApartmentAccess(CrewRoster crew) { _crew = crew; }

        /// <param name="heading">The way to face once inside; null keeps the heading from the street.</param>
        public bool Begin(Vector3 target, string ipl, bool enter, Vector3? interiorProbe = null, float? heading = null)
        {
            var ped = Game.Player.Character;
            if (Busy || enter == Inside || ped == null || !ped.Exists() || ped.IsDead || ped.IsInVehicle()) return false;
            _ped = ped; _origin = ped.Position; _heading = ped.Heading; _target = target; _heading2 = heading;
            _frozen = ped.IsPositionFrozen; _invincible = ped.IsInvincible; _control = Game.Player.CanControlCharacter;
            _entering = enter; _moved = false; _started = Game.GameTime; Busy = true; _probe = interiorProbe ?? target; _waiting = null; _refaded = false;
            Logger.Info("Apartment: " + (enter ? "entry" : "exit") + " requested; target=" + target + "; IPL=" + (ipl ?? "stock"));
            try
            {
                if (enter)
                {
                    ExitPosition = _origin; InteriorPosition = target; _ipl = ipl; _interior = 0;
                    foreach (var hero in Protagonist.All)
                        if (_crew.CompanionAI.StateOf(hero.Slot) != CompanionState.Scripted)
                        { _held.Add(hero.Slot); _crew.CompanionAI.TakeControl(hero.Slot); }
                    if (!string.IsNullOrEmpty(ipl))
                    {
                        _ownsIpl = !Function.Call<bool>(Hash.IS_IPL_ACTIVE, ipl);
                        Function.Call(Hash.REQUEST_IPL, ipl);
                    }
                }
                ped.IsPositionFrozen = true; ped.IsInvincible = true; Game.Player.CanControlCharacter = false;
                GameUtils.FadeOut(200);
                Function.Call(Hash.SET_FOCUS_POS_AND_VEL, target.X, target.Y, target.Z, 0f, 0f, 0f);
                return true;
            }
            catch { Fail(); throw; }
        }
        public void Update()
        {
            if (!Busy) return;
            try
            {
                if (_ped == null || !_ped.Exists() || _ped.IsDead || Game.Player.Character.Handle != _ped.Handle)
                { Fail(); return; }
                if (Game.GameTime - _started > 12000) { Logger.Warn("Apartment timeout: " + _waiting + "; interior=" + _interior + "; moved=" + _moved + "; wasDisabled=" + _wasDisabled + "; wasCapped=" + _wasCapped + "; target=" + _target); Fail(); GameUtils.Notify("~y~Apartment loading timed out. Returned to your previous position."); return; }
                Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, 0);
                Function.Call(Hash.REQUEST_COLLISION_AT_COORD, _target.X, _target.Y, _target.Z);
                // A scene ending in the same breath can fade the screen back in over
                // a room that is still streaming: the player then watches the void
                // from inside the interior. The fade is ours until the room is ready.
                if (Game.GameTime - _started > 400 && !GameUtils.IsScreenFadedOut())
                {
                    if (!_refaded) { _refaded = true; Logger.Info("Apartment: the screen came back early; holding the fade until the room is ready."); }
                    GameUtils.FadeOut(150);
                }
                if (Game.GameTime - _started < 250) return;
                if (_entering)
                {
                    if (!string.IsNullOrEmpty(_ipl) && !Function.Call<bool>(Hash.IS_IPL_ACTIVE, _ipl))
                    { Waiting("IPL streaming"); return; }
                    if (_interior == 0)
                    {
                        _interior = Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, _target.X, _target.Y, _target.Z);
                        if (_interior == 0 && _probe != _target)
                            _interior = Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, _probe.X, _probe.Y, _probe.Z);
                        if (_interior == 0) { Waiting("interior lookup"); return; }
                        // Story Mode ships the Online apartments switched off: a disabled
                        // or capped interior never reports ready, however long the player
                        // waits inside it (Ron's logs: 12 s at "interior readiness", twice).
                        // Switch it on for the visit and put it back on the way out.
                        _wasDisabled = Function.Call<bool>(Hash.IS_INTERIOR_DISABLED, _interior);
                        _wasCapped = Function.Call<bool>(Hash.IS_INTERIOR_CAPPED, _interior);
                        Logger.Info("Apartment: interior " + _interior + (_wasDisabled ? " was disabled" : " was enabled") + (_wasCapped ? " and capped" : " and uncapped") + "; pinned for the visit.");
                        if (_wasDisabled) Function.Call(Hash.DISABLE_INTERIOR, _interior, false);
                        if (_wasCapped) Function.Call(Hash.CAP_INTERIOR, _interior, false);
                        Function.Call(Hash.PIN_INTERIOR_IN_MEMORY, _interior);
                        Function.Call(Hash.REFRESH_INTERIOR, _interior);
                    }
                }
                // The room streams around the player, not around a focus point on the
                // street: waiting for readiness from outside never ended. The player is
                // frozen, invincible and behind a black screen, so standing inside an
                // interior that is still loading costs nothing.
                if (!_moved)
                {
                    _moved = true; _ped.Position = _target;
                    if (_heading2.HasValue) _ped.Heading = _heading2.Value;
                    Function.Call(Hash.CLEAR_ROOM_FOR_ENTITY, _ped);
                    Waiting("destination collision"); return;
                }
                if (!Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY, _ped)) return;
                if (_entering)
                {
                    int entityInterior = Function.Call<int>(Hash.GET_INTERIOR_FROM_ENTITY, _ped);
                    // Ready by the interior's own report, or by the player already being
                    // in that room with its collision loaded; either is a loaded room.
                    if (!Function.Call<bool>(Hash.IS_INTERIOR_READY, _interior) && entityInterior != _interior) { Waiting("interior readiness"); return; }
                    // Frozen entities can temporarily retain no room association.
                    // Accept the ready, collision-loaded room at the actual target
                    // coordinates; never accept a mismatched nonzero room.
                    if (entityInterior != _interior && (entityInterior != 0 ||
                        Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, _ped.Position.X, _ped.Position.Y, _ped.Position.Z) != _interior))
                    { Waiting("room association (entity=" + entityInterior + ")"); return; }
                }
                Inside = _entering;
                Logger.Info("Apartment: " + (Inside ? "entered" : "exited") + " with ready collision and restored controls.");
                RestoreTransition();
                if (!Inside) ReleaseInterior();
            }
            catch { Fail(); throw; }
        }
        private void Waiting(string stage)
        {
            if (_waiting == stage) return;
            _waiting = stage; Logger.Info("Apartment: waiting for " + stage);
        }
        private static void Attempt(Action action) { try { action(); } catch (Exception ex) { Logger.Error("Apartment cleanup", ex); } }
        private void RestoreTransition()
        {
            Busy = false;
            if (_ped != null && _ped.Exists())
            {
                Attempt(() => _ped.IsPositionFrozen = _frozen);
                Attempt(() => _ped.IsInvincible = _invincible);
            }
            Attempt(() => Game.Player.CanControlCharacter = _control);
            Attempt(() => Function.Call(Hash.CLEAR_FOCUS));
            Attempt(() => GameUtils.FadeIn(250));
        }
        private void ReleaseInterior()
        {
            foreach (var slot in _held) Attempt(() => _crew.CompanionAI.ReleaseControl(slot));
            _held.Clear();
            if (_interior != 0) Attempt(() => Function.Call(Hash.UNPIN_INTERIOR, _interior));
            if (_interior != 0 && _wasCapped) Attempt(() => Function.Call(Hash.CAP_INTERIOR, _interior, true));
            if (_interior != 0 && _wasDisabled) Attempt(() => Function.Call(Hash.DISABLE_INTERIOR, _interior, true));
            if (_ownsIpl && !string.IsNullOrEmpty(_ipl)) Attempt(() => Function.Call(Hash.REMOVE_IPL, _ipl));
            _interior = 0; _ipl = null; _ownsIpl = false; _wasDisabled = _wasCapped = false;
        }
        private void Fail()
        {
            if (_moved && _ped != null && _ped.Exists()) Attempt(() => { _ped.Position = _origin; _ped.Heading = _heading; });
            RestoreTransition();
            if (_entering) { Inside = false; ReleaseInterior(); }
        }
        public void Cancel()
        {
            if (Busy) Fail();
            if (Inside && _ped != null && _ped.Exists())
                Attempt(() => { Function.Call(Hash.REQUEST_COLLISION_AT_COORD, ExitPosition.X, ExitPosition.Y, ExitPosition.Z); _ped.Position = ExitPosition; });
            Inside = false; ReleaseInterior();
        }
    }
}
