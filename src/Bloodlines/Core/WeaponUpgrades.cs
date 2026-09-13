using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Native;
namespace Bloodlines.Core
{
    public static class WeaponUpgrades
    {
        public static string Key(CrewSlot slot,uint weapon)=>slot+":"+weapon;
        public static uint Id(WeaponComponent part)=>unchecked((uint)part.ComponentHash);
        public static IEnumerable<WeaponComponent> Parts(Ped ped,uint weapon)
        {
            if(ped==null||!ped.Exists()||!Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON,ped,weapon,false))return Enumerable.Empty<WeaponComponent>();
            var result=new List<WeaponComponent>();
            foreach(WeaponComponent part in ped.Weapons[(WeaponHash)weapon].Components)if(Function.Call<bool>(Hash.DOES_WEAPON_TAKE_WEAPON_COMPONENT,weapon,Id(part)))result.Add(part);
            return result;
        }
        private static string Kind(WeaponComponent part)=>part.ComponentHash.ToString().ToUpperInvariant();
        public static bool SpecialAmmo(WeaponComponent part)
        {var s=Kind(part);return s.Contains("INCENDIARY")||s.Contains("EXPLOSIVE")||s.Contains("ARMORPIERCING")||s.Contains("FMJ")||s.Contains("HOLLOWPOINT");}
        public static string Gate(WeaponComponent part)=>SpecialAmmo(part)?"M28":Kind(part).Contains("BARREL")||Kind(part).Contains("SCOPEMAX")?"M11":null;
        public static int Price(WeaponComponent part)
        {
            var s=Kind(part);if(SpecialAmmo(part))return 20000;
            if(s.Contains("BARREL"))return 6500;if(s.Contains("SUPP"))return 3500;
            if(s.Contains("SCOPE"))return 2500;if(s.Contains("CLIP"))return 1800;
            if(s.Contains("FLSH")||s.Contains("FLASH"))return 500;return 1200;
        }
        public static bool Owned(CampaignState state,CrewSlot slot,uint weapon,uint part)=>state.WeaponPartsOwned.TryGetValue(Key(slot,weapon),out var set)&&set.Contains(part);
        public static void Remember(CampaignState state,CrewSlot slot,Ped ped,uint weapon,uint purchased)
        {
            var key=Key(slot,weapon);
            if(!state.WeaponPartsOwned.TryGetValue(key,out var owned))state.WeaponPartsOwned[key]=owned=new HashSet<uint>();
            owned.Add(purchased);
            var fitted=new HashSet<uint>(Parts(ped,weapon).Where(p=>p.Active).Select(Id));
            owned.UnionWith(fitted);state.WeaponPartsFitted[key]=fitted;
        }
        public static void Apply(CampaignState state,CrewSlot slot,Ped ped,uint weapon)
        {
            if(!state.WeaponPartsFitted.TryGetValue(Key(slot,weapon),out var fitted)||ped==null||!ped.Exists())return;
            // An owned but removed part must not reappear after a character switch.
            foreach(var part in Parts(ped,weapon))
                if(part.Active && Owned(state,slot,weapon,Id(part)) && !fitted.Contains(Id(part))) part.Active=false;
            foreach(uint part in fitted)
                if(Function.Call<bool>(Hash.DOES_WEAPON_TAKE_WEAPON_COMPONENT,weapon,part)&&!Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON_COMPONENT,ped,weapon,part))
                    Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_PED,ped,weapon,part);
        }
    }
    public sealed partial class ShopService
    {
        public CrewSlot? WeaponCustomer {get;set;}
        public CrewSlot CustomerSlot => WeaponCustomer??_crew.ActiveSlot;
        public Ped CustomerPed => _crew.PedFor(CustomerSlot);
        public string CustomerName => Protagonist.Of(CustomerSlot).Handle;
        public IEnumerable<uint> CustomerWeapons => WeaponMarket.Catalog.Select(w=>w.Hash)
            .Concat(_state.Weapons.TryGetValue(CustomerSlot.ToString(),out var set)?set:Enumerable.Empty<uint>()).Distinct()
            .Where(w=>CustomerPed!=null&&CustomerPed.Exists()&&Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON,CustomerPed,w,false));
        private bool CustomerReady => CustomerPed!=null&&CustomerPed.Exists()&&CustomerPed.IsAlive;
        private void RememberCustomer(){_weapons.Capture(CustomerSlot,CustomerPed);}
        public bool FitWeaponPart(ShopSite site,uint weapon,uint component)
        {
            if(site==null||site.Kind!=ShopKind.Weapons||!CustomerReady)return false;
            var part=WeaponUpgrades.Parts(CustomerPed,weapon).FirstOrDefault(p=>WeaponUpgrades.Id(p)==component);
            if(part==null||part.Active)return false;
            bool owned=WeaponUpgrades.Owned(_state,CustomerSlot,weapon,component);
            string gate=WeaponUpgrades.Gate(part);
            if(!owned&&gate!=null&&!_state.IsComplete(gate)){GameUtils.Notify("Upgrade unlocks after "+gate+".");return false;}
            return Purchase(site,owned?0:WeaponUpgrades.Price(part),()=>
            {
                part.Active=true;if(!part.Active)return false;
                WeaponUpgrades.Remember(_state,CustomerSlot,CustomerPed,weapon,component);RememberCustomer();return true;
            });
        }
        public bool RemoveWeaponPart(ShopSite site,uint weapon,uint component)
        {
            if(site==null||site.Kind!=ShopKind.Weapons||!CustomerReady||!CanUse(site))return false;
            var part=WeaponUpgrades.Parts(CustomerPed,weapon).FirstOrDefault(p=>WeaponUpgrades.Id(p)==component);
            if(part==null||!part.Active)return false;
            part.Active=false;if(part.Active)return false;
            WeaponUpgrades.Remember(_state,CustomerSlot,CustomerPed,weapon,component);
            RememberCustomer();_state.Save();
            GameUtils.Notify("Attachment removed for "+CustomerName+". Refit it free from Owned parts.");
            return true;
        }
        public int MissingAmmo(uint weapon)
        {
            if(!CustomerReady||!Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON,CustomerPed,weapon,false))return 0;
            var result=new OutputArgument();if(!Function.Call<bool>(Hash.GET_MAX_AMMO,CustomerPed,weapon,result))return 0;
            int maximum=Math.Max(0,Math.Min(9999,result.GetResult<int>()));
            return Math.Max(0,maximum-Function.Call<int>(Hash.GET_AMMO_IN_PED_WEAPON,CustomerPed,weapon));
        }
        public int AmmoPackPrice(uint weapon)=>RefillCost(weapon,Math.Min(MissingAmmo(weapon),WeaponMarket.AmmoCount(weapon)));
        public int AmmoUnitPrice(uint weapon) => WeaponUpgrades.Parts(CustomerPed,weapon).Any(p=>p.Active&&WeaponUpgrades.SpecialAmmo(p))?150:0;
        private int RefillCost(uint weapon,int missing)
        {
            int premium=AmmoUnitPrice(weapon);if(premium>0)return missing*premium;
            return (int)Math.Ceiling(missing/(double)WeaponMarket.AmmoCount(weapon))*WeaponMarket.AmmoPrice(weapon);
        }
        // Guns sharing an ammo pool need one refill and one price.
        private IEnumerable<uint> RefillWeapons => CustomerWeapons.GroupBy(w=>
        {uint type=Function.Call<uint>(Hash.GET_PED_AMMO_TYPE_FROM_WEAPON,CustomerPed,w);return type==0?w:type;})
            .Select(g=>g.OrderByDescending(w=>MissingAmmo(w)).ThenBy(w=>RefillCost(w,MissingAmmo(w))).First());
        public int RefillAllQuote()=>RefillWeapons.Sum(w=>RefillCost(w,MissingAmmo(w)));
        public bool RefillAllAmmo(ShopSite site)
        {
            if(site==null||site.Kind!=ShopKind.Weapons||!CustomerReady||!CanUse(site))return false;
            var fills=RefillWeapons.Select(w=>new {Weapon=w,Missing=MissingAmmo(w)}).Where(p=>p.Missing>0).ToArray();
            int quote=fills.Sum(p=>RefillCost(p.Weapon,p.Missing));
            if(quote==0){GameUtils.Notify("All of "+CustomerName+"'s owned weapons are fully stocked.");return false;}
            if(_state.CashOnHand<quote){GameUtils.Notify("Full refill needs $"+quote+". No ammo bought.");return false;}
            int charge=0;
            try { foreach(var fill in fills)
            {
                int before=Function.Call<int>(Hash.GET_AMMO_IN_PED_WEAPON,CustomerPed,fill.Weapon);
                Function.Call(Hash.ADD_AMMO_TO_PED,CustomerPed,fill.Weapon,fill.Missing);
                int gained=Math.Max(0,Math.Min(fill.Missing,Function.Call<int>(Hash.GET_AMMO_IN_PED_WEAPON,CustomerPed,fill.Weapon)-before));
                charge+=RefillCost(fill.Weapon,gained);
            }
            } finally { if(charge>0){_state.CashOnHand-=charge;RememberCustomer();_memory.Capture(_crew);_state.Save();} }
            if(charge==0)return false;
            GameUtils.Notify(CustomerName+" ammo refilled: $"+charge+".");return true;
        }
    }
}
