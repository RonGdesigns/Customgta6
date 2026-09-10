using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    public enum ShopKind { Weapons, Customs, Guess, Clothing }
    public sealed class ShopSite
    {
        public readonly string Name; public readonly ShopKind Kind; public readonly Vector3 Position;
        public ShopSite(string name, ShopKind kind, float x, float y, float z) { Name=name; Kind=kind; Position=new Vector3(x,y,z); }
    }
    public static class ShopPurchase
    {
        // The apply callback must verify that something actually changed before
        // charging. Failed, unsupported, duplicate and out-of-range purchases cost nothing.
        public static bool Try(CampaignState state, int price, Func<bool> apply)
        {
            if (price < 0 || state.CashOnHand < price || !apply()) return false;
            state.CashOnHand -= price; return true;
        }
    }
    public sealed partial class ShopService
    {
        public static readonly ShopSite[] Sites = {
            new ShopSite("Ammu-Nation - Pillbox",ShopKind.Weapons,22.1f,-1107.0f,29.8f),
            new ShopSite("Ammu-Nation - Little Seoul",ShopKind.Weapons,-662.2f,-935.5f,21.8f),
            new ShopSite("Ammu-Nation - La Mesa",ShopKind.Weapons,810.2f,-2157.3f,29.6f),
            new ShopSite("Ammu-Nation - Hawick",ShopKind.Weapons,252.3f,-50.0f,69.9f),
            new ShopSite("Ammu-Nation - Morningwood",ShopKind.Weapons,-1305.3f,-394.3f,36.7f),
            new ShopSite("Ammu-Nation - Vespucci",ShopKind.Weapons,-1117.8f,-503.0f,35.8f),
            new ShopSite("Ammu-Nation - Chumash",ShopKind.Weapons,-3172.2f,1087.0f,20.8f),
            new ShopSite("Ammu-Nation - Paleto Bay",ShopKind.Weapons,-331.3f,6082.6f,31.5f),
            new ShopSite("Ammu-Nation - Sandy Shores",ShopKind.Weapons,1693.4f,3759.5f,34.7f),
            new ShopSite("Ammu-Nation - Tataviam",ShopKind.Weapons,2567.7f,294.4f,108.7f),
            new ShopSite("Ammu-Nation - Great Chaparral",ShopKind.Weapons,-111.5f,2698.6f,18.6f),
            new ShopSite("Los Santos Customs - Burton",ShopKind.Customs,-337.0f,-136.8f,39.0f),
            new ShopSite("Los Santos Customs - La Mesa",ShopKind.Customs,731.6f,-1088.8f,22.2f),
            new ShopSite("Los Santos Customs - Airport",ShopKind.Customs,-1155.5f,-2007.5f,13.2f),
            new ShopSite("Los Santos Customs - Harmony",ShopKind.Customs,1175.0f,2640.0f,37.8f),
            new ShopSite("Beeker's Garage",ShopKind.Customs,110.8f,6626.0f,31.8f),
            new ShopSite("Binco - Textile City",ShopKind.Clothing,425.1f,-806.5f,29.5f),
            new ShopSite("Binco - Vespucci",ShopKind.Clothing,-822.5f,-1073.3f,11.3f),
            new ShopSite("Discount Store - Strawberry",ShopKind.Clothing,75.1f,-1392.0f,29.4f),
            new ShopSite("Discount Store - Grapeseed",ShopKind.Clothing,1693.4f,4823.3f,42.1f),
            new ShopSite("Discount Store - Great Chaparral",ShopKind.Clothing,1196.4f,2709.8f,38.2f),
            new ShopSite("Discount Store - Paleto Bay",ShopKind.Clothing,4.3f,6512.8f,31.9f),
            new ShopSite("Suburban - Hawick",ShopKind.Clothing,126.5f,-219.2f,54.6f),
            new ShopSite("Suburban - Del Perro",ShopKind.Clothing,-1193.4f,-772.3f,17.3f),
            new ShopSite("Suburban - Harmony",ShopKind.Clothing,617.6f,2759.0f,42.1f),
            new ShopSite("Suburban - Chumash",ShopKind.Clothing,-3170.6f,1044.6f,20.9f),
            new ShopSite("Ponsonbys - Burton",ShopKind.Clothing,-164.6f,-303.4f,39.7f),
            new ShopSite("Ponsonbys - Rockford Hills",ShopKind.Clothing,-709.7f,-153.4f,37.4f),
            new ShopSite("Ponsonbys - Morningwood",ShopKind.Clothing,-1447.8f,-242.5f,49.8f),
            new ShopSite("Guess Customs - Strawberry workshop",ShopKind.Guess,-211.5f,-1324.0f,30.9f)
        };
        public static readonly WeaponProgression.DlcWeapon[] Stock = {
            new WeaponProgression.DlcWeapon("Pistol","WEAPON_PISTOL","Stock"),
            new WeaponProgression.DlcWeapon("Pump Shotgun","WEAPON_PUMPSHOTGUN","Stock"),
            new WeaponProgression.DlcWeapon("SMG","WEAPON_SMG","Stock"),
            new WeaponProgression.DlcWeapon("Assault Rifle","WEAPON_ASSAULTRIFLE","Stock"),
            new WeaponProgression.DlcWeapon("Carbine Rifle","WEAPON_CARBINERIFLE","Stock"),
            new WeaponProgression.DlcWeapon("Bullpup Rifle","WEAPON_BULLPUPRIFLE","Stock"),
            new WeaponProgression.DlcWeapon("Combat Pistol","WEAPON_COMBATPISTOL","Stock"),
            new WeaponProgression.DlcWeapon("Stun Gun","WEAPON_STUNGUN","Stock"),
            new WeaponProgression.DlcWeapon("Combat MG","WEAPON_COMBATMG","Stock"),
            new WeaponProgression.DlcWeapon("Assault SMG","WEAPON_ASSAULTSMG","Stock"),
            new WeaponProgression.DlcWeapon("Heavy Sniper","WEAPON_HEAVYSNIPER","Stock"),
            new WeaponProgression.DlcWeapon("Special Carbine","WEAPON_SPECIALCARBINE","Stock"),
            new WeaponProgression.DlcWeapon("Assault Shotgun","WEAPON_ASSAULTSHOTGUN","Stock"),
            new WeaponProgression.DlcWeapon("Micro SMG","WEAPON_MICROSMG","Stock"),
            new WeaponProgression.DlcWeapon("AP Pistol","WEAPON_APPISTOL","Stock"),
            new WeaponProgression.DlcWeapon("Pistol .50","WEAPON_PISTOL50","Stock"),
            new WeaponProgression.DlcWeapon("Sawed-off Shotgun","WEAPON_SAWNOFFSHOTGUN","Stock"),
            new WeaponProgression.DlcWeapon("RPG","WEAPON_RPG","Stock"),
            new WeaponProgression.DlcWeapon("Sniper Rifle","WEAPON_SNIPERRIFLE","Stock")
        };
        private readonly CrewRoster _crew; private readonly CampaignState _state;
        private readonly WeaponProgression _weapons; private readonly CrewMemory _memory;
        private readonly List<Blip> _blips = new List<Blip>();
        private readonly Dictionary<int, Door> _doors = new Dictionary<int, Door>();
        private int _nextDoors;
        public Func<bool> Allowed;
        public Action<ShopSite> OpenMenu;
        public ShopService(CrewRoster crew, CampaignState state, WeaponProgression weapons, CrewMemory memory)
        { _crew=crew; _state=state; _weapons=weapons; _memory=memory; }
        public bool CanUse(ShopSite site)
        {
            var ped=Game.Player.Character;
            return site != null && _crew.IsDeployed && (Allowed?.Invoke() ?? false) && Game.Player.WantedLevel==0 &&
                ped!=null && ped.Exists() && ped.IsAlive && (site.Kind!=ShopKind.Clothing || !ped.IsInVehicle()) && ped.Position.DistanceTo(site.Position)<22f;
        }
        public void Update(bool canOpen)
        {
            if (!_crew.IsDeployed) { Clear(); return; }
            if (_blips.Count==0)
                foreach(var site in Sites)
                {
                    var blip=World.CreateBlip(site.Position); if(blip==null)continue;
                    blip.Name=site.Name; blip.IsShortRange=true;
                    blip.Sprite=(BlipSprite)(site.Kind==ShopKind.Weapons?110:site.Kind==ShopKind.Clothing?73:72);
                    blip.Color=site.Kind==ShopKind.Guess?BlipColor.Orange:BlipColor.Blue;
                    _blips.Add(blip);
                }
            var player=Game.Player.Character;
            if(player==null||!player.Exists())return;
            var near=Sites.OrderBy(s=>player.Position.DistanceTo(s.Position)).First();
            if(Game.GameTime>=_nextDoors)
            {
                _nextDoors=Game.GameTime+500;
                MaintainDoors(player.Position, near);
            }
            if(player.Position.DistanceTo(near.Position)>35f)return;
            GameUtils.DrawObjectiveMarker(near.Position,near.Kind==ShopKind.Guess?Color.Orange:Color.DodgerBlue);
            if(!canOpen||!CanUse(near))return;
            GameUtils.Subtitle("~b~"+near.Name+"~s~ | E / D-pad Right: shop | Crew cash $"+_state.CashOnHand,500);
            if(Game.IsControlJustPressed(Control.Context))OpenMenu?.Invoke(near);
        }
        private bool Purchase(ShopSite site,int price,Func<bool> apply)
        {
            if(!CanUse(site)){GameUtils.Notify("Shop unavailable: return to the marker outside a mission, with no wanted level.");return false;}
            if(_state.CashOnHand<price){GameUtils.Notify("Not enough crew cash. Need $"+price+".");return false;}
            if(!ShopPurchase.Try(_state,price,apply)){GameUtils.Notify("Nothing changed. No money charged.");return false;}
            _weapons.Capture(_crew.ActiveSlot,Game.Player.Character);_memory.Capture(_crew);_state.Save();
            GameUtils.Notify("Purchase complete: $"+price+". Crew cash $"+_state.CashOnHand+".");return true;
        }
        public int Price(ShopSite site,int standard) => site.Kind==ShopKind.Guess ? standard/2 : standard;
        public bool BuyWeapon(ShopSite site,WeaponProgression.DlcWeapon weapon)
        {
            if (weapon==null) return false;
            string locked=WeaponMarket.LockedUntil(_state,_crew.ActiveSlot,weapon.Hash);
            if(locked!=null){GameUtils.Notify("Story locked: complete "+locked+". Crew cash unchanged.");return false;}
            int price=WeaponMarket.Price(weapon);
            return Purchase(site,price,()=> {
                var ped=Game.Player.Character;
                if(site.Kind!=ShopKind.Weapons||!WeaponProgression.Available(weapon)||Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON,ped,weapon.Hash,false))return false;
                ped.Weapons.Give((WeaponHash)weapon.Hash,WeaponMarket.AmmoCount(weapon.Hash),false,true);
                return Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON,ped,weapon.Hash,false);
            });
        }
        public bool BuyAmmo(ShopSite site)
        {
            uint selected=Function.Call<uint>(Hash.GET_SELECTED_PED_WEAPON,Game.Player.Character);
            return Purchase(site,WeaponMarket.AmmoPrice(selected),()=> {
                var ped=Game.Player.Character; uint weapon=Function.Call<uint>(Hash.GET_SELECTED_PED_WEAPON,ped);
                if(site.Kind!=ShopKind.Weapons||weapon==(uint)WeaponHash.Unarmed||!Function.Call<bool>(Hash.IS_WEAPON_VALID,weapon))return false;
                int before=Function.Call<int>(Hash.GET_AMMO_IN_PED_WEAPON,ped,weapon);
                int count=WeaponMarket.AmmoCount(weapon);
                Function.Call(Hash.ADD_AMMO_TO_PED,ped,weapon,count);
                return Function.Call<int>(Hash.GET_AMMO_IN_PED_WEAPON,ped,weapon)>before;
            });
        }
        public bool BuyArmor(ShopSite site) => Purchase(site,250,()=> {
            if(site.Kind!=ShopKind.Weapons||Game.Player.Character.Armor>=CrewDurability.Armor)return false;
            Game.Player.Character.Armor=CrewDurability.Armor;return true;
        });
        public static bool IsGarage(ShopSite site) => site!=null && (site.Kind==ShopKind.Customs || site.Kind==ShopKind.Guess);
        public Vehicle Car(ShopSite site)
        {
            var p=Game.Player.Character;var v=p?.CurrentVehicle;
            return CanUse(site)&&IsGarage(site)&&v!=null&&v.Exists()&&(v.Model.IsCar||v.Model.IsBike)&&
                v.GetPedOnSeat(VehicleSeat.Driver)==p&&v.Speed<.8f&&!v.IsInAir?v:null;
        }
        public bool Repair(ShopSite site) => Purchase(site,site.Kind==ShopKind.Guess&&_crew.ActiveSlot==CrewSlot.Guess?0:Price(site,500),()=> {
            var car=Car(site);if(car==null)return false;car.Repair();return true;
        });
        public int ModCount(ShopSite site,VehicleModType type)
        {var car=Car(site);if(car==null)return 0;car.Mods.InstallModKit();return car.Mods[type].Count;}
        /// <summary>Set by the host: work done to the crew's Granger is saved with the campaign.</summary>
        public CrewVan Vans { get; set; }
        private void Remember(Vehicle car) { if (Vans != null && Vans.IsVan(car)) Vans.Capture(car); }
        public bool Fit(ShopSite site,VehicleModType type,int index) => Purchase(site,Price(site,1000),()=> {
            var car=Car(site);if(car==null)return false;car.Mods.InstallModKit();var mod=car.Mods[type];
            if(index < -1||index>=mod.Count||mod.Index==index)return false;mod.Index=index;bool ok=mod.Index==index;if(ok)Remember(car);return ok;
        });
        public bool Paint(ShopSite site,VehicleColor color) => Purchase(site,Price(site,400),()=> {
            var car=Car(site);if(car==null)return false;
            if(car.Mods.PrimaryColor==color&&car.Mods.SecondaryColor==color)return false;
            car.Mods.PrimaryColor=color;car.Mods.SecondaryColor=color;
            bool ok=car.Mods.PrimaryColor==color&&car.Mods.SecondaryColor==color;if(ok)Remember(car);return ok;
        });
        public bool ReinforceTires(ShopSite site) => Purchase(site,Price(site,2000),()=> {
            var car=Car(site);if(car==null||site.Kind!=ShopKind.Guess||!car.CanTiresBurst||!_state.IsComplete("M11"))return false;
            car.CanTiresBurst=false;bool ok=!car.CanTiresBurst;if(ok)Remember(car);return ok;
        });
        private sealed class Door {public Prop Prop;public int System;public int State;public float Ratio,Heading;public bool Locked;}
        private static readonly HashSet<int> DoorModels=new HashSet<int>(new[]{"v_ilev_gc_door01","v_ilev_gc_door03","v_ilev_gc_door04","v_ilev_gc_door05",
            "v_ilev_cs_door01","v_ilev_cs_door01_r","v_ilev_clothmiddoor","v_ilev_clothmiddoor2","v_ilev_fh_door01","v_ilev_fh_door02","v_ilev_gc_door02","prop_com_ls_door_01","prop_id2_11_gdoor","prop_cs4_05_tdoor","v_ilev_carmod3door","lr_prop_supermod_door_01"}.Select(Game.GenerateHash));
        private void MaintainDoors(Vector3 player,ShopSite site)
        {
            foreach(var key in _doors.Keys.ToArray())
                if(!_doors[key].Prop.Exists()||player.DistanceTo(_doors[key].Prop.Position)>65f||!CanUse(site))
                {RestoreDoor(_doors[key]);_doors.Remove(key);}
            if(!CanUse(site))return;
            foreach(var prop in World.GetNearbyProps(site.Position,30f))
            {
                if(prop==null||!prop.Exists()||!DoorModels.Contains(prop.Model.Hash))continue;
                if(!_doors.TryGetValue(prop.Handle,out var door))
                {
                    door=new Door{Prop=prop};var pos=prop.Position;var id=new OutputArgument();
                    if(Function.Call<bool>(Hash.DOOR_SYSTEM_FIND_EXISTING_DOOR,pos.X,pos.Y,pos.Z,prop.Model.Hash,id))
                    {door.System=id.GetResult<int>();door.State=Function.Call<int>(Hash.DOOR_SYSTEM_GET_DOOR_STATE,door.System);door.Ratio=Function.Call<float>(Hash.DOOR_SYSTEM_GET_OPEN_RATIO,door.System);}
                    else {var locked=new OutputArgument();var heading=new OutputArgument();Function.Call(Hash.GET_STATE_OF_CLOSEST_DOOR_OF_TYPE,prop.Model.Hash,pos.X,pos.Y,pos.Z,locked,heading);door.Locked=locked.GetResult<bool>();door.Heading=heading.GetResult<float>();}
                    _doors[prop.Handle]=door;
                }
                var p=prop.Position;
                if(door.System!=0)
                {
                    Function.Call(Hash.DOOR_SYSTEM_SET_DOOR_STATE,door.System,0,false,true);
                    if(IsGarage(site))Function.Call(Hash.DOOR_SYSTEM_SET_OPEN_RATIO,door.System,1f,false,true);
                }
                else Function.Call(Hash.SET_STATE_OF_CLOSEST_DOOR_OF_TYPE,prop.Model.Hash,p.X,p.Y,p.Z,false,0f,false);
            }
        }
        private static void RestoreDoor(Door d)
        {
            if(!d.Prop.Exists())return;
            if(d.System!=0){Function.Call(Hash.DOOR_SYSTEM_SET_DOOR_STATE,d.System,d.State,false,true);Function.Call(Hash.DOOR_SYSTEM_SET_OPEN_RATIO,d.System,d.Ratio,false,true);}
            else{var p=d.Prop.Position;Function.Call(Hash.SET_STATE_OF_CLOSEST_DOOR_OF_TYPE,d.Prop.Model.Hash,p.X,p.Y,p.Z,d.Locked,d.Heading,false);}
        }
        public void Clear()
        {
            foreach(var b in _blips)if(b.Exists())b.Delete();_blips.Clear();
            foreach(var door in _doors.Values)try{RestoreDoor(door);}catch(Exception ex){Logger.Error("Restore shop door",ex);}
            _doors.Clear();
        }
    }
}
