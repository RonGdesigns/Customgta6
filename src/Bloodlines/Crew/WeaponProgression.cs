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
        private int _nextCapture, _captureSlot;
        private readonly Dictionary<CrewSlot, HashSet<uint>> _loanBaseline = new Dictionary<CrewSlot, HashSet<uint>>();

        /// <summary>True from mission start until its teardown returns the loans.</summary>
        public bool LoanActive => _loanBaseline.Count > 0;

        /// <summary>
        /// Snapshot what each hero owns as the mission starts. Anything found on a hero
        /// afterward that is not in this set, and not a permanent reward the campaign
        /// has committed, was issued by the mission and goes back with it.
        /// </summary>
        public void BeginLoan(CrewRoster crew)
        {
            _loanBaseline.Clear();
            foreach (var hero in Protagonist.All) _loanBaseline[hero.Slot] = new HashSet<uint>(Owned(hero.Slot));
            Core.Logger.Info("Weapon loan opened; locker baseline captured for the crew.");
        }

        /// <summary>
        /// Return the loans. Runs after a pass has been committed, so rewards for the
        /// mission just finished count as owned. Never removes a weapon that was in the
        /// locker before the mission or that a completed milestone grants. Returns how
        /// many weapons were taken back.
        /// </summary>
        public int EndLoan(CrewRoster crew)
        {
            if (!LoanActive) return 0;
            UnlockRewards();
            int returned = 0;
            foreach (var hero in Protagonist.All)
            {
                var allowed = new HashSet<uint>(_loanBaseline[hero.Slot]);
                foreach (uint reward in RewardHashes(hero.Slot)) allowed.Add(reward);
                foreach (var weapon in hero.Loadout) allowed.Add((uint)weapon);
                // A capture that slipped through during the mission is undone here.
                Owned(hero.Slot).RemoveWhere(hash => !allowed.Contains(hash));
                var ped = crew.PedFor(hero.Slot);
                if (ped == null || !ped.Exists() || ped.IsDead) continue;
                foreach (WeaponHash weapon in ValidWeapons)
                {
                    if (allowed.Contains((uint)weapon) || !Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, ped, (uint)weapon, false)) continue;
                    ped.Weapons.Remove(weapon);
                    returned++;
                }
            }
            _loanBaseline.Clear();
            _state.Save();
            Core.Logger.Info("Weapon loan closed; " + returned + " mission-issued weapon(s) returned.");
            return returned;
        }

        /// <summary>Every milestone weapon this hero has earned so far.</summary>
        private IEnumerable<uint> RewardHashes(CrewSlot slot)
        {
            foreach (string mission in RewardMissions)
                if (_state.IsComplete(mission) && ReceivesReward(mission, slot))
                    yield return (uint)Rewards(mission)[(int)slot];
        }
        private WeaponHash[] _validWeapons;
        private WeaponHash[] ValidWeapons => _validWeapons ?? (_validWeapons = Enum.GetValues(typeof(WeaponHash)).Cast<WeaponHash>()
            .Concat(DlcCatalog.Select(w => (WeaponHash)w.Hash)).Distinct()
            .Where(w => w != WeaponHash.Unarmed && Function.Call<bool>(Hash.IS_WEAPON_VALID, (uint)w)).ToArray());
        public WeaponProgression(CampaignState state) { _state = state; }
        private HashSet<uint> Owned(CrewSlot slot)
        {
            if (!_state.Weapons.TryGetValue(slot.ToString(), out var weapons))
                _state.Weapons[slot.ToString()] = weapons = new HashSet<uint>();
            return weapons;
        }
        /// <summary>
        /// Nearly every job pays in hardware. Each hero's reward line is unique to him
        /// and fits the work: Ice carries the fight, Gohan the precision and the
        /// gadgets, Guess the close-quarters and the wheel. A weapon on this table is
        /// earned, not bought: the shop keeps it locked until the job is done and sells
        /// only its ammunition afterward. The shop's stock guns, the starting rifles and
        /// the premium ray weapons stay purchasable.
        /// </summary>
        public static readonly string[] RewardMissions = {
            "M02", "M03", "M04", "M05", "M06", "M07", "M08", "M09", "M10", "M11", "M12", "M13", "M14", "M15", "M16", "M17", "M18",
            "M19", "M20", "M22", "M23", "M24", "M25", "M27", "M28", "M29", "M30", "SM01", "SM02", "SM03", "SM04", "SM05", "SM06" };
        public static bool ReceivesReward(string mission, CrewSlot slot) => !mission.StartsWith("SM") ||
            ((mission == "SM01" || mission == "SM04") && slot == CrewSlot.Ice) ||
            ((mission == "SM02" || mission == "SM05") && slot == CrewSlot.Gohan) ||
            ((mission == "SM03" || mission == "SM06") && slot == CrewSlot.Guess);
        private static WeaponHash Dlc(string key) => (WeaponHash)Game.GenerateHash(key);
        public static WeaponHash[] Rewards(string mission)
        {
            switch (mission)
            {
                // Act I: sidearms and street guns, one job at a time.
                case "M02": return new[] { WeaponHash.MicroSMG, WeaponHash.SNSPistol, WeaponHash.SawnOffShotgun };
                case "M03": return new[] { WeaponHash.PumpShotgun, WeaponHash.StunGun, WeaponHash.CombatPistol };
                case "M04": return new[] { WeaponHash.HeavyPistol, WeaponHash.MachinePistol, WeaponHash.CombatPDW };
                case "M05": return new[] { WeaponHash.SniperRifle, WeaponHash.FlareGun, WeaponHash.MiniSMG };
                case "M06": return new[] { WeaponHash.CombatMG, WeaponHash.CarbineRifle, WeaponHash.AssaultSMG };
                case "M07": return new[] { WeaponHash.APPistol, WeaponHash.Pistol50, WeaponHash.VintagePistol };
                case "M08": return new[] { WeaponHash.BullpupShotgun, WeaponHash.SmokeGrenade, WeaponHash.HeavyShotgun };
                case "M09": return new[] { WeaponHash.MG, WeaponHash.StickyBomb, WeaponHash.CompactRifle };
                case "M10": return new[] { WeaponHash.AdvancedRifle, WeaponHash.CompactGrenadeLauncher, WeaponHash.Gusenberg };
                case "M11": return new[] { WeaponHash.Grenade, WeaponHash.ProximityMine, WeaponHash.DoubleBarrelShotgun };
                case "M12": return new[] { WeaponHash.MarksmanRifle, WeaponHash.Revolver, WeaponHash.BullpupRifle };
                case "M13": return new[] { WeaponHash.SweeperShotgun, WeaponHash.CombatPDW, WeaponHash.Revolver };
                case "M14": return new[] { WeaponHash.HomingLauncher, Dlc("WEAPON_MILITARYRIFLE"), WeaponHash.DoubleActionRevolver };
                case "M15": return new[] { WeaponHash.HeavySniper, WeaponHash.SpecialCarbine, WeaponHash.AssaultShotgun };
                // Act II: the modern and the exotic.
                case "M16": return new[] { WeaponHash.GrenadeLauncher, Dlc("WEAPON_CERAMICPISTOL"), Dlc("WEAPON_NAVYREVOLVER") };
                case "M17": return new[] { Dlc("WEAPON_HEAVYRIFLE"), Dlc("WEAPON_PRECISIONRIFLE"), Dlc("WEAPON_COMBATSHOTGUN") };
                case "M18": return new[] { WeaponHash.RPG, Dlc("WEAPON_PISTOLXM3"), Dlc("WEAPON_BATTLERIFLE") };
                case "M19": return new[] { Dlc("WEAPON_MARKSMANRIFLE_MK2"), Dlc("WEAPON_SNSPISTOL_MK2"), Dlc("WEAPON_REVOLVER_MK2") };
                case "M20": return new[] { WeaponHash.Minigun, Dlc("WEAPON_GADGETPISTOL"), WeaponHash.SweeperShotgun };
                case "M22": return new[] { WeaponHash.Railgun, Dlc("WEAPON_EMPLAUNCHER"), WeaponHash.MachinePistol };
                case "M23": return new[] { Dlc("WEAPON_ASSAULTRIFLE_MK2"), Dlc("WEAPON_BULLPUPRIFLE_MK2"), Dlc("WEAPON_CARBINERIFLE_MK2") };
                case "M24": return new[] { Dlc("WEAPON_REVOLVER_MK2"), WeaponHash.HeavySniper, WeaponHash.MicroSMG };
                case "M25": return new[] { Dlc("WEAPON_MILITARYRIFLE"), WeaponHash.AdvancedRifle, WeaponHash.Pistol50 };
                case "M27": return new[] { Dlc("WEAPON_COMBATMG_MK2"), Dlc("WEAPON_SPECIALCARBINE_MK2"), Dlc("WEAPON_TECPISTOL") };
                case "M28": return new[] { Dlc("WEAPON_SMG_MK2"), Dlc("WEAPON_MARKSMANRIFLE_MK2"), Dlc("WEAPON_PUMPSHOTGUN_MK2") };
                case "M29": return new[] { WeaponHash.StickyBomb, WeaponHash.HeavyPistol, WeaponHash.APPistol };
                case "M30": return new[] { WeaponHash.Gusenberg, WeaponHash.AssaultSMG, WeaponHash.SpecialCarbine };
                // Solo jobs pay their owner only.
                case "SM01": return new[] { Dlc("WEAPON_PUMPSHOTGUN_MK2"), WeaponHash.StunGun, WeaponHash.CombatPistol };
                case "SM02": return new[] { WeaponHash.StunGun, WeaponHash.MarksmanRifle, WeaponHash.CombatPistol };
                case "SM03": return new[] { WeaponHash.StunGun, WeaponHash.CombatPistol, WeaponHash.HeavyPistol };
                case "SM04": return new[] { Dlc("WEAPON_HEAVYSNIPER_MK2"), WeaponHash.StunGun, WeaponHash.CombatPistol };
                case "SM05": return new[] { WeaponHash.PumpShotgun, Dlc("WEAPON_PISTOL_MK2"), WeaponHash.CombatPistol };
                case "SM06": return new[] { WeaponHash.PumpShotgun, WeaponHash.StunGun, Dlc("WEAPON_SMG_MK2") };
                default: return new WeaponHash[0];
            }
        }
        public bool UnlockRewards()
        {
            bool changed = false;
            foreach (string mission in RewardMissions)
                if (_state.IsComplete(mission))
                {
                    var rewards = Rewards(mission);
                    foreach (var hero in Protagonist.All)
                        if (ReceivesReward(mission, hero.Slot))
                            changed |= Owned(hero.Slot).Add((uint)rewards[(int)hero.Slot]);
                }
            return changed;
        }
        public bool Capture(CrewSlot slot, Ped ped)
        {
            if (ped == null || !ped.Exists() || ped.IsDead) return false;
            bool changed = false;
            foreach (WeaponHash weapon in ValidWeapons)
                if (Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, ped, (uint)weapon, false))
                    changed |= Owned(slot).Add((uint)weapon);
            return changed;
        }
        /// <summary>
        /// SM01's tungsten-core 7.62 is a supply line, not a new gun: once Ice has it,
        /// every locker restock issues his rifles double the usual count. The story
        /// promised the ammunition would matter, so the benefit is real and bounded.
        /// </summary>
        public bool HasArmorPiercingSupply(CrewSlot slot) =>
            slot == CrewSlot.Ice && _state.FleetUpgrades.TryGetValue("armorPiercingSupply", out bool stocked) && stocked;
        public int RestockCount(CrewSlot slot, uint weapon, bool restock)
        {
            int count = Core.WeaponMarket.AmmoCount(weapon);
            return restock && count > 3 && HasArmorPiercingSupply(slot) ? count * 2 : count;
        }
        public void Apply(CrewSlot slot, Ped ped, bool restock = false)
        {
            UnlockRewards();
            if (ped == null || !ped.Exists() || ped.IsDead) return;
            foreach (uint weapon in Owned(slot))
                if (Function.Call<bool>(Hash.IS_WEAPON_VALID, weapon) &&
                    (restock || !Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON, ped, weapon, false)))
                    ped.Weapons.Give((WeaponHash)weapon, RestockCount(slot, weapon, restock), false, true);
        }
        /// <summary>
        /// Ownership capture runs only in free roam. A weapon a mission hands out is a
        /// loan for that job; recording it here would make every mission-issued MG a
        /// permanent locker item and quietly pre-empt the milestone that is supposed
        /// to unlock it.
        /// </summary>
        public void Update(CrewRoster crew, bool captureAllowed = true)
        {
            if (Game.GameTime < _nextCapture || !crew.IsDeployed) return;
            _nextCapture = Game.GameTime + 1700;
            bool changed = UnlockRewards();
            var hero = Protagonist.All[_captureSlot++ % Protagonist.All.Length];
            if (captureAllowed && !LoanActive) changed |= Capture(hero.Slot, crew.PedFor(hero.Slot));
            Apply(hero.Slot, crew.PedFor(hero.Slot));
            if (changed) _state.Save();
        }
        public void SaveCrew(CrewRoster crew)
        {
            foreach (var hero in Protagonist.All) Capture(hero.Slot, crew.PedFor(hero.Slot));
            UnlockRewards(); _state.Save();
        }
    }
}
