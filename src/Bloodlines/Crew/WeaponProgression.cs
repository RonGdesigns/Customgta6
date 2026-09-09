using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Missions;
using GTA;
using GTA.Native;

namespace Bloodlines.Crew
{
    /// <summary>Each hero keeps acquired weapon ownership; campaign jobs expand the home locker.</summary>
    public sealed class WeaponProgression
    {
        public sealed class DlcWeapon
        {
            public readonly string Name, Key, Category;
            public uint Hash => unchecked((uint)Game.GenerateHash(Key));
            public DlcWeapon(string name, string key, string category) { Name = name; Key = key; Category = category; }
        }
        public static readonly DlcWeapon[] DlcCatalog = {
            new DlcWeapon("Pistol Mk II", "WEAPON_PISTOL_MK2", "Mk II"),
            new DlcWeapon("SNS Pistol Mk II", "WEAPON_SNSPISTOL_MK2", "Mk II"),
            new DlcWeapon("Heavy Revolver Mk II", "WEAPON_REVOLVER_MK2", "Mk II"),
            new DlcWeapon("SMG Mk II", "WEAPON_SMG_MK2", "Mk II"),
            new DlcWeapon("Pump Shotgun Mk II", "WEAPON_PUMPSHOTGUN_MK2", "Mk II"),
            new DlcWeapon("Assault Rifle Mk II", "WEAPON_ASSAULTRIFLE_MK2", "Mk II"),
            new DlcWeapon("Carbine Rifle Mk II", "WEAPON_CARBINERIFLE_MK2", "Mk II"),
            new DlcWeapon("Special Carbine Mk II", "WEAPON_SPECIALCARBINE_MK2", "Mk II"),
            new DlcWeapon("Bullpup Rifle Mk II", "WEAPON_BULLPUPRIFLE_MK2", "Mk II"),
            new DlcWeapon("Combat MG Mk II", "WEAPON_COMBATMG_MK2", "Mk II"),
            new DlcWeapon("Heavy Sniper Mk II", "WEAPON_HEAVYSNIPER_MK2", "Mk II"),
            new DlcWeapon("Marksman Rifle Mk II", "WEAPON_MARKSMANRIFLE_MK2", "Mk II"),
            new DlcWeapon("Ceramic Pistol", "WEAPON_CERAMICPISTOL", "DLC firearms"),
            new DlcWeapon("Navy Revolver", "WEAPON_NAVYREVOLVER", "DLC firearms"),
            new DlcWeapon("Perico Pistol", "WEAPON_GADGETPISTOL", "DLC firearms"),
            new DlcWeapon("Military Rifle", "WEAPON_MILITARYRIFLE", "DLC firearms"),
            new DlcWeapon("Heavy Rifle", "WEAPON_HEAVYRIFLE", "DLC firearms"),
            new DlcWeapon("Service Carbine", "WEAPON_TACTICALRIFLE", "DLC firearms"),
            new DlcWeapon("Precision Rifle", "WEAPON_PRECISIONRIFLE", "DLC firearms"),
            new DlcWeapon("Combat Shotgun", "WEAPON_COMBATSHOTGUN", "DLC firearms"),
            new DlcWeapon("WM 29 Pistol", "WEAPON_PISTOLXM3", "DLC firearms"),
            new DlcWeapon("Tactical SMG", "WEAPON_TECPISTOL", "DLC firearms"),
            new DlcWeapon("Battle Rifle", "WEAPON_BATTLERIFLE", "DLC firearms"),
            new DlcWeapon("Compact EMP Launcher", "WEAPON_EMPLAUNCHER", "Special weapons"),
            new DlcWeapon("Railgun (DLC)", "WEAPON_RAILGUNXM3", "Special weapons"),
            new DlcWeapon("Up-n-Atomizer", "WEAPON_RAYPISTOL", "Special weapons"),
            new DlcWeapon("Unholy Hellbringer", "WEAPON_RAYCARBINE", "Special weapons"),
            new DlcWeapon("Widowmaker", "WEAPON_RAYMINIGUN", "Special weapons")
        };
        public static bool Available(DlcWeapon weapon) => weapon != null && Function.Call<bool>(GTA.Native.Hash.IS_WEAPON_VALID, weapon.Hash);
        public bool GiveDlc(CrewSlot slot, Ped ped, DlcWeapon weapon)
        {
            if (ped == null || !ped.Exists() || ped.IsDead || !Available(weapon)) return false;
            ped.Weapons.Give((WeaponHash)weapon.Hash, weapon.Category == "Special weapons" ? 20 : 120, false, true);
            if (!Function.Call<bool>(GTA.Native.Hash.HAS_PED_GOT_WEAPON, ped, weapon.Hash, false)) return false;
            Owned(slot).Add(weapon.Hash); _state.Save();
            return true;
        }
        private readonly CampaignState _state;
        private int _nextCapture;
        public WeaponProgression(CampaignState state) { _state = state; }
        private HashSet<uint> Owned(CrewSlot slot)
        {
            if (!_state.Weapons.TryGetValue(slot.ToString(), out var weapons))
                _state.Weapons[slot.ToString()] = weapons = new HashSet<uint>();
            return weapons;
        }
        public static WeaponHash[] Rewards(string mission)
        {
            switch (mission)
            {
                case "M03": return new[] { WeaponHash.PumpShotgun, WeaponHash.StunGun, WeaponHash.CombatPistol };
                case "M06": return new[] { WeaponHash.CombatMG, WeaponHash.CarbineRifle, WeaponHash.AssaultSMG };
                case "M15": return new[] { WeaponHash.HeavySniper, WeaponHash.SpecialCarbine, WeaponHash.AssaultShotgun };
                default: return new WeaponHash[0];
            }
        }
        public bool UnlockRewards()
        {
            bool changed = false;
            foreach (string mission in new[] { "M03", "M06", "M15" })
                if (_state.IsComplete(mission))
                {
                    var rewards = Rewards(mission);
                    foreach (var hero in Protagonist.All) changed |= Owned(hero.Slot).Add((uint)rewards[(int)hero.Slot]);
                }
            return changed;
        }
        public bool Capture(CrewSlot slot, Ped ped)
        {
            if (ped == null || !ped.Exists() || ped.IsDead) return false;
            bool changed = false;
            foreach (WeaponHash weapon in Enum.GetValues(typeof(WeaponHash)).Cast<WeaponHash>()
                .Concat(DlcCatalog.Select(w => (WeaponHash)w.Hash)).Distinct())
                if (weapon != WeaponHash.Unarmed && Function.Call<bool>(Hash.IS_WEAPON_VALID, (uint)weapon) &&
                    Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, ped, (uint)weapon, false))
                    changed |= Owned(slot).Add((uint)weapon);
            return changed;
        }
        public void Apply(CrewSlot slot, Ped ped, bool restock = false)
        {
            UnlockRewards();
            if (ped == null || !ped.Exists() || ped.IsDead) return;
            foreach (uint weapon in Owned(slot))
                if (Function.Call<bool>(Hash.IS_WEAPON_VALID, weapon) &&
                    (restock || !Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, ped, weapon, false)))
                    ped.Weapons.Give((WeaponHash)weapon, 60, false, true);
        }
        public void Update(CrewRoster crew)
        {
            if (Game.GameTime < _nextCapture || !crew.IsDeployed) return;
            _nextCapture = Game.GameTime + 5000;
            bool changed = UnlockRewards();
            foreach (var hero in Protagonist.All) changed |= Capture(hero.Slot, crew.PedFor(hero.Slot));
            if (changed) _state.Save();
        }
        public void SaveCrew(CrewRoster crew)
        {
            foreach (var hero in Protagonist.All) Capture(hero.Slot, crew.PedFor(hero.Slot));
            UnlockRewards(); _state.Save();
        }
    }
}
