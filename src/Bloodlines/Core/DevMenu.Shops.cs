using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using Bloodlines.Crew;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    public sealed partial class DevMenu
    {
        public ShopService Shops;
        private ShopSite _shopping;
        public void OpenShop(ShopSite site)
        {
            if(Shops==null||!Shops.CanUse(site))return;
            Close();Shops.WeaponCustomer=_crew.ActiveSlot;_shopping=site;_stick.Reset();IsOpen=true;_openedAt=Game.GameTime;
            _shopReceipts.Clear(); GameUtils.MenuNotice = ShopNotice;
            _stack.Push(BuildShop(site));
        }
        private static string PartName(object value) => Regex.Replace(value.ToString(),"([a-z])([A-Z])","$1 $2");
        private Page BuildShop(ShopSite site)
        {
            if(site.Kind==ShopKind.Dealer)return BuildDealer(site);
            var page=new Page(site.Name);
            page.Add("Crew cash",()=>"$"+_state.CashOnHand);
            if(site.Kind==ShopKind.Weapons)
            {
                page.Add("Shop for",()=>Shops.CustomerName,()=>_stack.Push(BuildWeaponCustomers(site)));
                page.Add("Refill ALL owned guns",()=>"$"+Shops.RefillAllQuote()+" - "+Shops.CustomerName,()=>Shops.RefillAllAmmo(site));
                page.Add("Attachments and upgrades",()=>"owned guns / compatible parts",()=>_stack.Push(BuildWeaponUpgrades(site)));
                page.Add("Ammo - equipped weapon",()=> {if(Shops.CustomerPed==null||!Shops.CustomerPed.Exists())return "Brother unavailable";uint w=Function.Call<uint>(Hash.GET_SELECTED_PED_WEAPON,Shops.CustomerPed);return "$"+Shops.AmmoPackPrice(w)+" / "+WeaponMarket.AmmoCount(w)+" rounds";},()=>Shops.BuyAmmo(site));
                page.Add("Body armor",()=>"$250",()=>Shops.BuyArmor(site));
                page.Add("Unlocked weapons",()=>"starting kit / earned / owned",()=>_stack.Push(BuildShopWeapons(site,0)));
                page.Add("Additional weapons",()=>"buy extras, including DLC",()=>_stack.Push(BuildShopWeapons(site,1)));
                page.Add("Story unlocks",()=>"required mission shown",()=>_stack.Push(BuildShopWeapons(site,2)));
            }
            else if(site.Kind==ShopKind.Clothing)
            {
                page.Add("Choose outfit",()=>"clothes / shoes / accessories",()=>_stack.Push(BuildWardrobe(_crew.ActiveSlot,true)));
                page.Add("Fitting room",()=>"No charge; save your chosen look");
                page.Add("Hairstyle",()=>"fixed for each character");
            }
            else
            {
                if (Garages != null)
                {
                    page.Add("Save car to garage", () => "Choose a garage / keep driving", () => _stack.Push(BuildShopGarageDestination(site)));
                    page.Add("Car sales left today", () => Garages.SalesRemaining + "/10");
                    page.Add("Sell this car", () => "$" + Garages.StreetSaleQuote(Garages.ArrivalVehicle()).ToString("N0"),
                        () => { if (Garages.SellStreetVehicle(Garages.ArrivalVehicle())) Close(); });
                }
                page.Add("Service requirement",()=>"Stopped car or bike, you driving");
                page.Add("Purchased parts",()=>"Fitted to this vehicle");
                page.Add("Repair",()=>site.Kind==ShopKind.Guess&&_crew.ActiveSlot==CrewSlot.Guess?"Free - Guess's own work":"$"+Shops.Price(site,500),()=>Shops.Repair(site));
                page.Add("Performance / body / interior",()=>"compatible upgrades",()=>_stack.Push(BuildShopParts(site)));
                page.Add("Paint and colors",()=>"body / trim / dashboard / RGB",()=>_stack.Push(BuildShopColors(site)));
                page.Add("Wheels and tires",()=>"street / track / Benny's / more",()=>_stack.Push(BuildWheelFamilies(site)));
                page.Add("Lights / extras / windows / plates",()=>"compatible equipment",()=>_stack.Push(BuildShopEquipment(site)));
                if(site.Kind==ShopKind.Guess)page.Add("Reinforced tires",()=>_state.IsComplete("M11")?"$1000":"Unlocks after M11",()=>Shops.ReinforceTires(site));
            }
            return page;
        }
        private Page BuildShopWeapons(ShopSite site,int tab)
        {
            var page=new Page((tab==0?"Unlocked":tab==1?"Additional":"Story locks")+" - "+Shops.CustomerName);
            foreach(var weapon in WeaponMarket.Catalog)
            {
                var item=weapon;bool unlocked=WeaponMarket.IsUnlocked(_state,Shops.CustomerSlot,item.Hash);
                bool locked=WeaponMarket.LockedUntil(_state,Shops.CustomerSlot,item.Hash)!=null;
                if(tab==0?!unlocked:tab==1?unlocked||locked:!locked)continue;
                page.Add(item.Name,()=> {
                    string mission=WeaponMarket.LockedUntil(_state,Shops.CustomerSlot,item.Hash);
                    if(mission!=null)return "Complete "+mission;
                    if(Shops.CustomerPed==null||!Shops.CustomerPed.Exists())return "Brother unavailable";
                    return !WeaponProgression.Available(item)?"Unavailable on this build":Function.Call<bool>(Hash.HAS_PED_GOT_WEAPON,Shops.CustomerPed,item.Hash,false)?"Owned":"$"+WeaponMarket.Price(item);
                },()=>Shops.BuyWeapon(site,item));
                page.Items.Last().StatWeapon=item.Hash;
            }
            if(page.Items.Count==0)page.Add("Nothing listed",()=>tab==2?"All story weapons unlocked":"Check the other weapon tabs");
            return page;
        }
        private Page BuildWeaponCustomers(ShopSite site)
        {
            var page=new Page("Shop for a brother");
            foreach(var hero in Protagonist.All)
            {
                var slot=hero.Slot;
                page.Add(hero.Handle,()=>Shops.CustomerSlot==slot?"Selected":"Weapons, ammo, armor and upgrades",()=>
                {Shops.WeaponCustomer=slot;_stack.Pop();_stack.Pop();_stack.Push(BuildShop(site));});
            }
            return page;
        }
        private Page BuildWeaponUpgrades(ShopSite site)
        {
            var page=new Page(Shops.CustomerName+" - weapon upgrades");
            foreach(uint hash in Shops.CustomerWeapons)
            {
                uint weapon=hash;var item=WeaponMarket.Catalog.FirstOrDefault(w=>w.Hash==weapon);
                page.Add(item?.Name??((WeaponHash)weapon).ToString(),()=>"Compatible attachments",()=>_stack.Push(BuildWeaponParts(site,weapon)));
            }
            if(page.Items.Count==0)page.Add("No owned weapons",()=>"Buy or earn a weapon first");return page;
        }
        private Page BuildWeaponParts(ShopSite site,uint weapon)
        {
            var page=new Page("Weapon parts - "+Shops.CustomerName);
            foreach(var component in WeaponUpgrades.Parts(Shops.CustomerPed,weapon))
            {
                var part=component;uint id=WeaponUpgrades.Id(part);
                page.Add(string.IsNullOrWhiteSpace(part.LocalizedName)?PartName(part.ComponentHash):part.LocalizedName,()=>
                {
                    if(part.Active)return "Fitted - select to remove";
                    if(WeaponUpgrades.Owned(_state,Shops.CustomerSlot,weapon,id))return "Owned - refit free";
                    var gate=WeaponUpgrades.Gate(part);
                    return gate!=null&&!_state.IsComplete(gate)?"Complete "+gate:"$"+WeaponUpgrades.Price(part);
                },()=>{if(part.Active)Shops.RemoveWeaponPart(site,weapon,id);else Shops.FitWeaponPart(site,weapon,id);});
                page.Items.Last().StatWeapon=weapon;page.Items.Last().StatComponent=id;
            }
            if(page.Items.Count==0)page.Add("No compatible upgrades",()=>"This weapon has no supported attachments");return page;
        }
        private Page BuildCrewFleet()
        {
            var page=new Page("Foundry - crew car fleet");
            foreach(var item in CrewVan.FleetChoices)
            {
                var choice=item;
                page.Add(choice.Name,()=>Shops.Vans.Record.Model==choice.Model?"Current crew car":Shops.Vans.Owns(choice.Model)?"Owned - select free":"$"+choice.Price,()=>
                {
                    var confirm=new Page(choice.Name+" - crew car");
                    confirm.Add("Confirm selection",()=>Shops.Vans.Owns(choice.Model)?"Free":"$"+choice.Price,()=>
                    {Shops.Vans.Select(choice,_homes.FoundryVisit&&_homes.Apartment.Inside&&!_missions.IsRunning);_stack.Pop();});
                    confirm.Add("Cancel",()=>"keep current crew car",()=>_stack.Pop());_stack.Push(confirm);
                });
            }
            page.Add("Customize your crew car",()=>"Drive it to any Customs shop");return page;
        }
        private void PreviewCar(ShopSite site,Func<bool> buy)
        {
            if(!Shops.BeginVehiclePreview(site,buy))return;
            var page=new Page("Vehicle preview - no charge");
            page.Add("Confirm purchase",()=>"$"+Shops.PreviewPrice,()=>{Shops.ConfirmVehiclePreview();_stack.Pop();});
            page.Add("Cancel / restore original",()=>"B / Back also cancels",()=>{Shops.CancelVehiclePreview();_stack.Pop();});
            _stack.Push(page);
        }
        private Page BuildShopParts(ShopSite site)
        {
            var page=new Page("Compatible vehicle parts");
            foreach(var type in Enum.GetValues(typeof(VehicleModType)).Cast<VehicleModType>().Concat(new[]{(VehicleModType)47,(VehicleModType)49}).Distinct())
            {
                var chosen=type;if(type==VehicleModType.FrontWheel||(type==VehicleModType.RearWheel&&Shops.Car(site)?.Model.IsBike==true)||Shops.ModCount(site,type)==0)continue;
                string label=(int)type==24?"Hydraulics":(int)type==47?"Right door":(int)type==49?"Light bar":PartName(type);
                page.Add(label,()=>Shops.ModCount(site,chosen)+" options",()=>_stack.Push(BuildShopMods(site,chosen)));
            }
            foreach(VehicleToggleModType type in Enum.GetValues(typeof(VehicleToggleModType)))
            {
                if ((int)type != 17 && type != VehicleToggleModType.Turbo && type != VehicleToggleModType.TireSmoke && type != VehicleToggleModType.XenonHeadlights) continue;
                var chosen=type;int price=type==VehicleToggleModType.Turbo?6000:1200;
                page.Add((int)type==17?"Nitrous boost - "+Nitrous.Controls:PartName(type),()=> (Shops.Car(site)?.Mods[chosen].IsInstalled==true?"Installed":"Off")+" / $"+Shops.Price(site,price),()=> {
                    var car=Shops.Car(site);if(car!=null){bool enabled=!car.Mods[chosen].IsInstalled;PreviewCar(site,()=>Shops.TogglePart(site,chosen,enabled));}
                });
            }
            return page;
        }
        private Page BuildShopGarageDestination(ShopSite shop)
        {
            var page = new Page("Save this build - choose garage");
            foreach (var garage in Garages.OwnedSites)
            {
                var destination = garage;
                page.Add(destination.Name, () => Garages.Summary(destination), () => {
                    if (Garages.SaveFromShop(Shops, shop, destination)) _stack.Pop();
                });
            }
            return page;
        }
        private Page BuildShopMods(ShopSite site,VehicleModType type)
        {
            var page=new Page(PartName(type)+" - preview parts");int count=Shops.ModCount(site,type);
            for(int i=-1;i<count;i++){int index=i;page.Add(Shops.ModName(site,type,i),()=>Shops.ModIndex(site,type)==index?"Fitted":"$"+Shops.Price(site,1000),()=>PreviewCar(site,()=>Shops.Fit(site,type,index)));}
            return page;
        }
        private Page BuildShopColors(ShopSite site)
        {
            var page=new Page("Paint and colors");string[] names={"Primary","Secondary","Pearlescent","Rims","Interior trim","Dashboard"};
            for(int i=0;i<names.Length;i++){int channel=i;string name=names[i];page.Add(name,()=>"$"+Shops.Price(site,400),()=>_stack.Push(BuildShopPaint(site,channel,name)));}
            string[] rgb={"Custom primary","Custom secondary","Neon color","Tire smoke color"};
            for(int i=0;i<rgb.Length;i++){int channel=i;string name=rgb[i];page.Add(name,()=>"RGB / $"+Shops.Price(site,600),()=>_stack.Push(BuildShopRgb(site,channel,name)));}
            return page;
        }
        private Page BuildShopPaint(ShopSite site,int channel,string name)
        {
            var page=new Page(name+" paint");
            foreach(VehicleColor color in Enum.GetValues(typeof(VehicleColor)))
            {var selected=color;page.Add(PartName(color),()=>"$"+Shops.Price(site,400),()=>PreviewCar(site,()=>Shops.PaintChannel(site,channel,selected)));}
            return page;
        }
        private Page BuildShopRgb(ShopSite site,int channel,string name)
        {
            var page=new Page(name);int red=255,green=255,blue=255;
            if(channel==2)foreach(var preset in new[]{Color.White,Color.Red,Color.Blue,Color.Cyan,Color.Lime,Color.Yellow,Color.Orange,Color.HotPink,Color.Purple})
            {var color=preset;page.Add(color.Name,()=>"Preview / $"+Shops.Price(site,600),()=>PreviewCar(site,()=>Shops.Rgb(site,channel,color)));}
            page.Add("Red",()=>red.ToString(),adjust:d=>red=Math.Max(0,Math.Min(255,red+d*5)));
            page.Add("Green",()=>green.ToString(),adjust:d=>green=Math.Max(0,Math.Min(255,green+d*5)));
            page.Add("Blue",()=>blue.ToString(),adjust:d=>blue=Math.Max(0,Math.Min(255,blue+d*5)));
            page.Add("Preview color",()=>"$"+Shops.Price(site,600),()=>PreviewCar(site,()=>Shops.Rgb(site,channel,Color.FromArgb(red,green,blue))));
            return page;
        }
        private Page BuildShopEquipment(ShopSite site)
        {
            var page=new Page("Vehicle equipment");var car=Shops.Car(site);if(car==null)return page;
            page.Add("Window tint",()=>"$"+Shops.Price(site,500),()=> {
                var p=new Page("Window tint");foreach(VehicleWindowTint t in Enum.GetValues(typeof(VehicleWindowTint))){var tint=t;if(t==VehicleWindowTint.Invalid)continue;p.Add(PartName(t),()=>"$"+Shops.Price(site,500),()=>PreviewCar(site,()=>Shops.Tint(site,tint)));}_stack.Push(p);
            });
            page.Add("Plate style",()=>"$"+Shops.Price(site,400),()=> {
                var p=new Page("Plate style");foreach(LicensePlateStyle s in Enum.GetValues(typeof(LicensePlateStyle))){var style=s;p.Add(PartName(s),()=>"$"+Shops.Price(site,400),()=>PreviewCar(site,()=>Shops.Plate(site,style)));}_stack.Push(p);
            });
            for(int i=0;i<car.Mods.LiveryCount;i++){int index=i;page.Add("Livery "+(i+1),()=>"$"+Shops.Price(site,1500),()=>PreviewCar(site,()=>Shops.Livery(site,index)));}
            for(int i=0;i<=20;i++){int id=i;if(car.ExtraExists(id))page.Add("Extra "+id,()=>Shops.Car(site)?.IsExtraOn(id)==true?"Fitted":"$"+Shops.Price(site,500),()=>{var v=Shops.Car(site);if(v!=null){bool enabled=!v.IsExtraOn(id);PreviewCar(site,()=>Shops.Extra(site,id,enabled));}});}
            foreach(VehicleNeonLight side in Enum.GetValues(typeof(VehicleNeonLight)))
            {var chosen=side;if(car.Mods.HasNeonLight(side))page.Add(PartName(side)+" neon",()=>"$"+Shops.Price(site,800),()=>{var v=Shops.Car(site);if(v!=null){bool enabled=!v.Mods.IsNeonLightsOn(chosen);PreviewCar(site,()=>Shops.Neon(site,chosen,enabled));}});}
            page.Add("Neon underglow color",()=>"presets / RGB / preview",()=>_stack.Push(BuildShopRgb(site,2,"Neon underglow")));
            page.Add("Xenon color",()=>"Install xenon headlights first",()=> {
                var p=new Page("Xenon color");string[] names={"Default","White","Blue","Electric blue","Mint green","Lime green","Yellow","Golden shower","Orange","Red","Pony pink","Hot pink","Purple","Blacklight"};
                for(int i=-1;i<=12;i++){int color=i;p.Add(names[i+1],()=>"$"+Shops.Price(site,500),()=>PreviewCar(site,()=>Shops.XenonColor(site,color)));}_stack.Push(p);
            });
            return page;
        }
        private Page BuildWheelFamilies(ShopSite site)
        {
            var page=new Page("Wheel families");string[] names={"Sport","Muscle","Lowrider","SUV","Off-road","Tuner","Motorcycle","High-end","Benny's originals","Benny's bespoke","Open wheel","Street","Track"};
            for(int i=0;i<names.Length;i++){int family=i;string name=names[i];page.Add(name,()=>"browse / $"+Shops.Price(site,1200),()=> {
                var p=new Page(name+" wheels");bool custom=false;
                p.Add("Custom tires",()=>custom?"Yes":"No",()=>custom=!custom);
                p.Add("Bike family changes",()=>"Other axle returns to stock");
                foreach(bool rear in new[]{false,true}){bool back=rear;int count=Shops.WheelCount(site,family,rear);for(int n=-1;n<count;n++){int index=n;if(count==0)break;p.Add((rear?"Rear":"Front")+" "+(n<0?"stock":"rim "+(n+1)),()=>"$"+Shops.Price(site,1200),()=>PreviewCar(site,()=>Shops.Wheel(site,family,back,index,custom)));}}
                _stack.Push(p);
            });}
            return page;
        }
    }
}
