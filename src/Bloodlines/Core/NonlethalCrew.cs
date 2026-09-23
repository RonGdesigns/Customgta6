using System;
using System.Collections.Generic;
using Bloodlines.Crew;
using GTA;

namespace Bloodlines.Core
{
    /// <summary>
    /// The crew's half of a job where nobody may be killed. <see cref="NonlethalGuards"/> keeps
    /// the guards alive once they are down; this keeps the brothers from putting them down the
    /// wrong way.
    ///
    /// Ron, September 22, on M50: the brothers had stun guns, a dead watchman ended the mission,
    /// "but they automatically shot. They didn't start with stun guns." Only Ice had been issued
    /// one, nobody had it in his hand, and a brother the player was not holding picked his own
    /// weapon for any fight he was given - which, for a man carrying a carbine, is the carbine.
    ///
    /// So for the whole attempt:
    ///
    ///  * every brother carries a stun gun, issued inside the mission's weapon loan, so the gun
    ///    goes back at teardown unless he already owned one;
    ///  * every brother has it in his hand when gameplay starts, after the briefing and the
    ///    establishing scene, and whoever the player switches to has it in his hand too;
    ///  * a brother the player is not holding cannot change weapons (SET_PED_CAN_SWITCH_WEAPON),
    ///    so whatever sends him into a fight - a role track under fire, the companion controller
    ///    after he rejoins the player, a support order - he fights with the stun gun or not at
    ///    all. If anything puts another gun in his hand, the stun gun is put back;
    ///  * the companion controller is told (<see cref="CompanionController.StunGunOnly"/>) so a
    ///    seated brother is never handed a drive-by, which is a Micro SMG.
    ///
    /// The player's own choice is left alone: he starts with the stun gun and can still draw
    /// anything he carries, and a watchman he shoots still fails the mission as designed.
    ///
    /// <see cref="End"/> unlocks every ped this locked and puts back the weapon each one was
    /// holding when the attempt began. It runs from the mission's cleanup, which pass, failure,
    /// abort and death all go through, and <see cref="CompanionController.ReleaseAll"/> clears
    /// the controller's flag again as a backstop.
    /// </summary>
    public sealed class NonlethalCrew
    {
        /// <summary>Charges on each brother's loaned stun gun.</summary>
        public const int DefaultRounds = 100;

        private CrewRoster _crew;
        private int _rounds = DefaultRounds;
        private bool _engaged, _armedForGameplay;
        private CrewSlot? _lastActive;
        /// <summary>Every ped this ever locked, by handle, so the release reaches a ped even if the roster has since moved on.</summary>
        private readonly Dictionary<int, Ped> _locked = new Dictionary<int, Ped>();
        /// <summary>What each brother was holding as the attempt began, keyed by ped handle.</summary>
        private readonly Dictionary<int, WeaponHash> _held = new Dictionary<int, WeaponHash>();

        public bool Engaged => _engaged;

        /// <summary>
        /// Issue the stun gun to every deployed brother, put it in his hand, and lock the ones the
        /// player is not holding to it. Call from Setup, after the crew is deployed; the weapon
        /// loan is already open by then, so the gun is a loan.
        /// </summary>
        public void Begin(CrewRoster crew, int rounds = DefaultRounds)
        {
            _crew = crew;
            _rounds = rounds;
            _engaged = crew != null;
            _armedForGameplay = false;
            _lastActive = null;
            _locked.Clear();
            _held.Clear();
            if (!_engaged) return;
            crew.CompanionAI.StunGunOnly = true;
            foreach (var (slot, ped) in Brothers())
            {
                try { _held[ped.Handle] = ped.Weapons.Current.Hash; } catch (Exception ex) { Logger.Error("Nonlethal crew: reading " + slot + "'s weapon", ex); }
                ped.Weapons.Give(WeaponHash.StunGun, _rounds, true, true);
                Arm(slot, ped);
            }
            Logger.Info("Nonlethal job: every brother carries a stun gun; the brothers the player is not holding cannot change weapons.");
        }

        /// <summary>
        /// Once per mission tick. The first tick after <see cref="Begin"/> is the start of
        /// gameplay - missions do not tick during scenes - so every brother, the player included,
        /// is handed the stun gun then. After that the player is left alone, a switch hands the
        /// man he switched to the stun gun, and the brothers he is not holding are kept on it.
        /// </summary>
        public void Update()
        {
            if (!_engaged || _crew == null) return;
            _crew.CompanionAI.StunGunOnly = true;
            var active = _crew.ActiveSlot;
            bool changed = !_armedForGameplay || _lastActive != active;
            foreach (var (slot, ped) in Brothers())
            {
                if (ped.IsDead) continue;
                if (slot == active)
                {
                    if (changed) { Unlock(ped); Select(ped); }
                    continue;
                }
                // A seated brother cannot use a stun gun, and nothing hands him a drive-by
                // while this is on. He is put back on it the moment he is on his feet.
                if (changed || (!ped.IsInVehicle() && ped.Weapons.Current.Hash != WeaponHash.StunGun)) Arm(slot, ped);
            }
            _armedForGameplay = true;
            _lastActive = active;
        }

        /// <summary>Unlock every ped this locked and hand each back what he was holding. Safe to call twice.</summary>
        public void End()
        {
            if (_crew != null) _crew.CompanionAI.StunGunOnly = false;
            foreach (var ped in _locked.Values)
            {
                if (ped == null || !ped.Exists()) continue;
                // Unlocked even when he is down: a brother the recovery brings back is the same
                // ped, and one left locked would never draw anything but the stun gun again.
                ped.CanSwitchWeapons = true;
            }
            if (_crew != null)
                foreach (var (_, ped) in Brothers())
                {
                    ped.CanSwitchWeapons = true;
                    if (ped.IsDead || !_held.TryGetValue(ped.Handle, out var weapon) || weapon == WeaponHash.StunGun) continue;
                    if (ped.Weapons.HasWeapon(weapon)) ped.Weapons.Select(weapon, true);
                }
            if (_engaged) Logger.Info("Nonlethal job over; the brothers can change weapons again.");
            _engaged = false;
            _armedForGameplay = false;
            _lastActive = null;
            _locked.Clear();
            _held.Clear();
        }

        /// <summary>The deployed brothers, each once. A roster that answers every slot with the same ped (a solo job) yields him once, as the active one.</summary>
        private IEnumerable<(CrewSlot, Ped)> Brothers()
        {
            var active = _crew.PedFor(_crew.ActiveSlot);
            foreach (var hero in Protagonist.All)
            {
                var ped = _crew.PedFor(hero.Slot);
                if (ped == null || !ped.Exists()) continue;
                if (hero.Slot != _crew.ActiveSlot && active != null && ped.Handle == active.Handle) continue;
                yield return (hero.Slot, ped);
            }
        }

        /// <summary>The stun gun in his hand, and, when the player is not holding him, no way to change it.</summary>
        private void Arm(CrewSlot slot, Ped ped)
        {
            if (slot == _crew.ActiveSlot) { Unlock(ped); Select(ped); return; }
            ped.CanSwitchWeapons = false;
            _locked[ped.Handle] = ped;
            Select(ped);
        }

        private static void Unlock(Ped ped) => ped.CanSwitchWeapons = true;

        private void Select(Ped ped)
        {
            if (!ped.Weapons.HasWeapon(WeaponHash.StunGun)) ped.Weapons.Give(WeaponHash.StunGun, _rounds, true, true);
            ped.Weapons.Select(WeaponHash.StunGun, true);
        }
    }
}
