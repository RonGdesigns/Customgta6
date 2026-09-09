using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    public static class WeaponMarket
    {
        public static IEnumerable<WeaponProgression.DlcWeapon> Catalog => ShopService.Stock.Concat(WeaponProgression.DlcCatalog);
        public static string[] Milestones(uint hash) => WeaponProgression.RewardMissions.Where(m => Protagonist.All.Any(h =>
            WeaponProgression.ReceivesReward(m,h.Slot) && (uint)WeaponProgression.Rewards(m)[(int)h.Slot]==hash)).ToArray();
        public static bool Owned(CampaignState state,CrewSlot slot,uint hash) => state.Weapons.TryGetValue(slot.ToString(),out var owned) && owned.Contains(hash);
        public static string LockedUntil(CampaignState state,CrewSlot slot,uint hash)
        {
            // Existing pickups/purchases and a hero's starting kit remain usable.
            if(Owned(state,slot,hash) || Protagonist.Of(slot).Loadout.Any(w=>(uint)w==hash)) return null;
            var missions=Milestones(hash);
            return missions.Length==0 || missions.Any(state.IsComplete) ? null : string.Join(" or ",missions);
        }
        public static bool IsUnlocked(CampaignState state,CrewSlot slot,uint hash) => Owned(state,slot,hash) ||
            Protagonist.Of(slot).Loadout.Any(w=>(uint)w==hash) || Milestones(hash).Any(state.IsComplete);
        // Campaign-economy prices, not GTA Online's unrelated economy.
        private static readonly Dictionary<string,int> Prices=new Dictionary<string,int> {
            {"WEAPON_PISTOL",1500},{"WEAPON_COMBATPISTOL",2500},{"WEAPON_PISTOL50",5000},{"WEAPON_APPISTOL",6500},
            {"WEAPON_STUNGUN",5000},{"WEAPON_PUMPSHOTGUN",4500},{"WEAPON_SAWNOFFSHOTGUN",3500},
            {"WEAPON_MICROSMG",5000},{"WEAPON_SMG",6500},{"WEAPON_ASSAULTSMG",9000},{"WEAPON_ASSAULTRIFLE",10000},
            {"WEAPON_CARBINERIFLE",12000},{"WEAPON_BULLPUPRIFLE",14000},{"WEAPON_SPECIALCARBINE",18000},
            {"WEAPON_ASSAULTSHOTGUN",20000},{"WEAPON_SNIPERRIFLE",16000},{"WEAPON_HEAVYSNIPER",30000},
            {"WEAPON_COMBATMG",28000},{"WEAPON_RPG",45000},
            {"WEAPON_CERAMICPISTOL",6000},{"WEAPON_NAVYREVOLVER",10000},{"WEAPON_GADGETPISTOL",16000},
            {"WEAPON_PISTOLXM3",8500},{"WEAPON_MILITARYRIFLE",26000},{"WEAPON_HEAVYRIFLE",32000},
            {"WEAPON_TACTICALRIFLE",30000},{"WEAPON_PRECISIONRIFLE",24000},{"WEAPON_COMBATSHOTGUN",28000},
            {"WEAPON_TECPISTOL",22000},{"WEAPON_BATTLERIFLE",35000},
            {"WEAPON_PISTOL_MK2",20000},{"WEAPON_SNSPISTOL_MK2",18000},{"WEAPON_REVOLVER_MK2",26000},
            {"WEAPON_SMG_MK2",25000},{"WEAPON_PUMPSHOTGUN_MK2",32000},{"WEAPON_ASSAULTRIFLE_MK2",38000},
            {"WEAPON_CARBINERIFLE_MK2",40000},{"WEAPON_BULLPUPRIFLE_MK2",38000},{"WEAPON_SPECIALCARBINE_MK2",44000},
            {"WEAPON_COMBATMG_MK2",60000},{"WEAPON_HEAVYSNIPER_MK2",75000},{"WEAPON_MARKSMANRIFLE_MK2",45000},
            {"WEAPON_EMPLAUNCHER",45000},{"WEAPON_RAYPISTOL",60000},{"WEAPON_RAYCARBINE",100000},
            {"WEAPON_RAYMINIGUN",160000},{"WEAPON_RAILGUNXM3",180000}
        };
        public static int Price(WeaponProgression.DlcWeapon weapon) => Prices.TryGetValue(weapon.Key,out int price)?price:25000;
        private static bool Is(uint hash,string key) => hash==unchecked((uint)Game.GenerateHash(key));
        public static int AmmoCount(uint hash)
        {
            if(Is(hash,"WEAPON_RAILGUNXM3") || Is(hash,"WEAPON_EMPLAUNCHER") || Is(hash,"WEAPON_RPG")) return 3;
            uint group=Function.Call<uint>(Hash.GET_WEAPONTYPE_GROUP,hash);
            return Is(group,"GROUP_HEAVY") || Is(group,"GROUP_THROWN") ? 3 : 60;
        }
        public static int AmmoPrice(uint hash)
        {
            if(Is(hash,"WEAPON_RAILGUNXM3"))return 1800;
            if(Is(hash,"WEAPON_RAYMINIGUN") || Is(hash,"WEAPON_RAYCARBINE"))return 600;
            if(Is(hash,"WEAPON_EMPLAUNCHER") || AmmoCount(hash)==3)return 600;
            return 60;
        }
    }
}
