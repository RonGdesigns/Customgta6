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
        private int _started, _interior;
        private string _ipl;
        private bool _ownsIpl;
        public bool Busy { get; private set; }
        public bool Inside { get; private set; }
        public Vector3 ExitPosition { get; private set; }
        public Vector3 InteriorPosition { get; private set; }
        public ApartmentAccess(CrewRoster crew) { _crew = crew; }

        public bool Begin(Vector3 target, string ipl, bool enter, Vector3? interiorProbe = null)
        {
            var ped = Game.Player.Character;
            if (Busy || enter == Inside || ped == null || !ped.Exists() || ped.IsDead || ped.IsInVehicle()) return false;
            _ped = ped; _origin = ped.Position; _heading = ped.Heading; _target = target;
            _frozen = ped.IsPositionFrozen; _invincible = ped.IsInvincible; _control = Game.Player.CanControlCharacter;
            _entering = enter; _moved = false; _started = Game.GameTime; Busy = true; _probe = interiorProbe ?? target; _waiting = null;
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
                if (Game.GameTime - _started > 12000) { Logger.Warn("Apartment timeout: " + _waiting + "; interior=" + _interior + "; moved=" + _moved + "; target=" + _target); Fail(); GameUtils.Notify("~y~Apartment loading timed out. Returned to your previous position."); return; }
                Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, 0);
                Function.Call(Hash.REQUEST_COLLISION_AT_COORD, _target.X, _target.Y, _target.Z);
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
                        Logger.Info("Apartment: pinned interior " + _interior);
                        Function.Call(Hash.PIN_INTERIOR_IN_MEMORY, _interior);
                        Function.Call(Hash.REFRESH_INTERIOR, _interior);
                    }
                    if (!Function.Call<bool>(Hash.IS_INTERIOR_READY, _interior)) { Waiting("interior readiness"); return; }
                }
                if (!_moved)
                {
                    _moved = true; _ped.Position = _target;
                    Function.Call(Hash.CLEAR_ROOM_FOR_ENTITY, _ped);
                    Waiting("destination collision"); return;
                }
                if (!Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY, _ped)) return;
                if (_entering)
                {
                    int entityInterior = Function.Call<int>(Hash.GET_INTERIOR_FROM_ENTITY, _ped);
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
            if (_ownsIpl && !string.IsNullOrEmpty(_ipl)) Attempt(() => Function.Call(Hash.REMOVE_IPL, _ipl));
            _interior = 0; _ipl = null; _ownsIpl = false;
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
