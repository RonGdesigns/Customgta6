using System;
using System.IO;
using System.Linq;
using System.Drawing;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Math;
using GTA.Native;
public static partial class StoryTests
{
 static void CommerceFleetChecks()
 {
  Reset();CrewAppearance.Load(Path.Combine(root,"debug-hair19.ini"));
  var hairPed=Game.Player.Character;int oldHair=CrewAppearance.For(CrewSlot.Guess).Hair,oldTint=CrewAppearance.For(CrewSlot.Guess).HairColor;
  CrewAppearance.Adjust(hairPed,CrewSlot.Guess,"Hair",1);CrewAppearance.Adjust(hairPed,CrewSlot.Guess,"HairColor",1);
  Check(CrewAppearance.For(CrewSlot.Guess).Hair==oldHair&&CrewAppearance.For(CrewSlot.Guess).HairColor==oldTint,"Normal wardrobe calls still cannot change head hair");
  CrewAppearance.Adjust(hairPed,CrewSlot.Guess,"Hair",1,true);CrewAppearance.Adjust(hairPed,CrewSlot.Guess,"HairColor",1,true);
  Check(CrewAppearance.For(CrewSlot.Guess).Hair!=oldHair&&CrewAppearance.For(CrewSlot.Guess).HairColor!=oldTint,"Debug appearance explicitly enables hairstyle and color authoring");
  CrewAppearance.Save();Check(File.ReadAllText(Path.Combine(root,"debug-hair19.ini")).Contains("Hair=1"),"Debug-authored hairstyle is saved in the existing appearance file");
  Reset();var crew=Roster();var state=CampaignState.Load(Path.Combine(root,"commerce19.json"));state.CashOnHand=250000;
  var site=ShopService.Sites.First(s=>s.Kind==ShopKind.Customs);var car=new Vehicle{Position=site.Position,CanTiresBurst=true};Game.Player.Character.Position=site.Position;Game.Player.Character.SetIntoVehicle(car,VehicleSeat.Driver);
  var service=new ShopService(crew,state,new WeaponProgression(state),new CrewMemory(state)){Allowed=()=>true};
  car.Mods[VehicleModType.Spoilers].Index=-1;int cash=state.CashOnHand;
  Check(service.BeginVehiclePreview(site,()=>service.Fit(site,VehicleModType.Spoilers,1))&&car.Mods[VehicleModType.Spoilers].Index==1&&state.CashOnHand==cash,"Cosmetic preview visibly fits a part without charging");
  var unpaid=new OwnedVehicle();Check(!GarageService.Capture(car,unpaid),"Garage auto-save cannot persist an unpaid preview");
  service.CancelVehiclePreview();Check(car.Mods[VehicleModType.Spoilers].Index==-1&&state.CashOnHand==cash&&!service.HasVehiclePreview,"Cancel removes the preview and restores a stock slot free");
  service.BeginVehiclePreview(site,()=>service.Fit(site,VehicleModType.Spoilers,1));Check(service.ConfirmVehiclePreview()&&car.Mods[VehicleModType.Spoilers].Index==1&&state.CashOnHand==cash-1000,"Confirm charges once and retains the part");
  cash=state.CashOnHand;Check(!service.ConfirmVehiclePreview()&&state.CashOnHand==cash,"Repeated confirmation cannot double-charge");
  car.Mods.ClearCustomPrimaryColor();service.BeginVehiclePreview(site,()=>service.Rgb(site,0,Color.Red));service.CancelVehiclePreview();Check(!car.Mods.IsPrimaryColorCustom,"Cancel custom paint restores a standard paint finish");
  car.Mods.NeonLightsColor=Color.Blue;service.BeginVehiclePreview(site,()=>service.Rgb(site,2,Color.Red));Check(car.Mods.NeonLightsColor.ToArgb()==Color.Red.ToArgb(),"Neon color has a visible preview");service.CancelVehiclePreview();Check(car.Mods.NeonLightsColor.ToArgb()==Color.Blue.ToArgb(),"Cancel restores the original neon RGB");
  service.BeginVehiclePreview(site,()=>service.Fit(site,VehicleModType.Spoilers,2));Game.Player.Character.Task.LeaveVehicle();Check(!service.ConfirmVehiclePreview()&&state.CashOnHand==cash&&car.Mods[VehicleModType.Spoilers].Index==1,"Leaving the car invalidates purchase and restores its previous part");
  Game.Player.Character.SetIntoVehicle(car,VehicleSeat.Driver);state.CashOnHand=0;Check(service.BeginVehiclePreview(site,()=>service.Fit(site,VehicleModType.Spoilers,2)),"A player can preview before saving enough to buy");Check(!service.ConfirmVehiclePreview()&&state.CashOnHand==0&&car.Mods[VehicleModType.Spoilers].Index==1,"Unaffordable confirmation restores original appearance");
  var worth=new OwnedVehicle{Price=40000};int stock=VehiclePricing.Sale(worth);worth.Mods[11]=1;Check(VehiclePricing.Sale(worth)>stock,"Installed upgrades increase resale value");worth.Mods.Clear();Check(VehiclePricing.Sale(worth)==stock,"Removing an upgrade also removes its resale contribution");

  Check(WeaponProgression.DlcCatalog.Where(w=>w.Key.EndsWith("_MK2")).All(mk2=>WeaponMarket.Catalog.Any(mk1=>mk1.Key==mk2.Key.Substring(0,mk2.Key.Length-4))),"Every Mk II shop weapon has its separately purchasable original version");
  // Remote shopping remains tied to the selected brother, including upgrades after reload.
  Game.Player.Character.Task.LeaveVehicle();site=ShopService.Sites.First(s=>s.Kind==ShopKind.Weapons);Game.Player.Character.Position=site.Position;service.WeaponCustomer=CrewSlot.Gohan;state.CashOnHand=250000;
  var gun=ShopService.Stock.First(w=>w.Key=="WEAPON_PISTOL");Check(service.BuyWeapon(site,gun)&&crew.PedFor(CrewSlot.Gohan).Weapons.Owned.Contains((WeaponHash)gun.Hash)&&!Game.Player.Character.Weapons.Owned.Contains((WeaponHash)gun.Hash),"Remote purchase goes to Gohan, not the active shopper");
  var target=crew.PedFor(CrewSlot.Gohan);var component=new WeaponComponent{ComponentHash=WeaponComponentHash.AtArSupp};target.Weapons[(WeaponHash)gun.Hash].Components.Add(component);
  Check(service.FitWeaponPart(site,gun.Hash,(uint)component.ComponentHash)&&component.Active&&state.WeaponPartsOwned.ContainsKey(WeaponUpgrades.Key(CrewSlot.Gohan,gun.Hash)),"Compatible attachment is purchased and saved for its recipient");
  cash=state.CashOnHand;Check(!service.FitWeaponPart(site,gun.Hash,(uint)component.ComponentHash)&&state.CashOnHand==cash,"Already fitted gun parts cost nothing");
  Check(service.RemoveWeaponPart(site,gun.Hash,(uint)component.ComponentHash)&&!component.Active&&state.CashOnHand==cash&&WeaponUpgrades.Owned(state,CrewSlot.Gohan,gun.Hash,(uint)component.ComponentHash),"Removing a fitted attachment keeps ownership and never charges or refunds money");
  var unfitted=CampaignState.Load(Path.Combine(root,"commerce19.json"));
  Check(!unfitted.WeaponPartsFitted[WeaponUpgrades.Key(CrewSlot.Gohan,gun.Hash)].Contains((uint)component.ComponentHash),"Removed attachment is persisted as unfitted for the correct brother");
  component.Active=true;WeaponUpgrades.Apply(unfitted,CrewSlot.Gohan,target,gun.Hash);
  Check(!component.Active,"Restoring the saved build removes a stale re-equipped attachment");
  Check(!service.RemoveWeaponPart(site,gun.Hash,(uint)component.ComponentHash)&&state.CashOnHand==cash,"Already removed attachments cannot be removed or charged again");
  component.Active=false;Check(service.FitWeaponPart(site,gun.Hash,(uint)component.ComponentHash)&&state.CashOnHand==cash,"Owned gun attachment can be refitted without paying again");

  uint mk2Hash=Weapon("WEAPON_PISTOL_MK2");target.Weapons.Give((WeaponHash)mk2Hash,12,false,false);new WeaponProgression(state).Capture(CrewSlot.Gohan,target);
  Check(WeaponMarket.Owned(state,CrewSlot.Gohan,gun.Hash)&&WeaponMarket.Owned(state,CrewSlot.Gohan,mk2Hash),"Acquiring Mk II preserves ownership of the original weapon");
  var mk2Part=new WeaponComponent{ComponentHash=WeaponComponentHash.AtArSupp,Active=true};target.Weapons[(WeaponHash)mk2Hash].Components.Add(mk2Part);WeaponUpgrades.Remember(state,CrewSlot.Gohan,target,mk2Hash,(uint)mk2Part.ComponentHash);
  service.RemoveWeaponPart(site,gun.Hash,(uint)component.ComponentHash);
  Check(mk2Part.Active&&state.WeaponPartsFitted[WeaponUpgrades.Key(CrewSlot.Gohan,mk2Hash)].Contains((uint)mk2Part.ComponentHash),"Removing a Mk I attachment leaves the Mk II setup fitted");
  service.FitWeaponPart(site,gun.Hash,(uint)component.ComponentHash);
  var restoredPed=new Ped();new WeaponProgression(state).Apply(CrewSlot.Gohan,restoredPed);
  Check(restoredPed.Weapons.Owned.Contains((WeaponHash)gun.Hash)&&restoredPed.Weapons.Owned.Contains((WeaponHash)mk2Hash),"Restoring a brother gives both owned Mk I and Mk II hashes to the native inventory");
  var special=new WeaponComponent{ComponentHash=WeaponComponentHash.RifleClipExplosive};target.Weapons[(WeaponHash)gun.Hash].Components.Add(special);Check(!service.FitWeaponPart(site,gun.Hash,(uint)special.ComponentHash)&&!special.Active,"Special ammunition upgrade respects M28 progression gate");
  uint second=Weapon("WEAPON_SMG");target.Weapons.Give((WeaponHash)second,1,false,false);new WeaponProgression(state).Capture(CrewSlot.Gohan,target);target.Weapons.Ammo[gun.Hash]=2;int quote=service.RefillAllQuote();cash=state.CashOnHand;
  Check(quote>0&&service.RefillAllAmmo(site)&&target.Weapons.Ammo[gun.Hash]==240&&target.Weapons.Ammo[second]==240&&state.CashOnHand==cash-quote,"Refill-all fills every owned gun for the selected brother at the quoted total");
  cash=state.CashOnHand;Check(!service.RefillAllAmmo(site)&&state.CashOnHand==cash,"Already full inventories are not charged again");
  var restored=CampaignState.Load(Path.Combine(root,"commerce19.json"));Check(restored.WeaponPartsFitted[WeaponUpgrades.Key(CrewSlot.Gohan,gun.Hash)].Contains((uint)component.ComponentHash),"Weapon attachment selection survives save/reload");
  // Fleet selection persists each model's own build and never removes occupied cars.
  Reset();crew=Roster();var ctx=Context(crew);state=CampaignState.Load(Path.Combine(root,"fleet19.json"));state.Safehouses["cypressFoundry"]=true;state.CashOnHand=200000;
  var vans=new CrewVan(state,ctx.Locations);car=vans.Spawn(Vector3.Zero,0);car.Mods.PrimaryColor=(VehicleColor)7;vans.Capture(car);
  var insurgent=CrewVan.FleetChoices.First(c=>c.Model=="insurgent2");Game.Player.Character.SetIntoVehicle(car,VehicleSeat.Driver);
  Check(!vans.Select(insurgent,true)&&car.Exists()&&state.CashOnHand==200000,"Crew fleet cannot replace an occupied car");Game.Player.Character.Task.LeaveVehicle();
  Check(!vans.Select(insurgent,false)&&state.CashOnHand==200000,"Crew vehicle purchase requires hideout access");
  Check(vans.Select(insurgent,true)&&state.CrewVan.Model=="insurgent2"&&state.CashOnHand==80000,"Insurgent can be bought as the crew's four-seat car");
  car=vans.Spawn(Vector3.Zero,0);Check(car.Model.Name=="insurgent2","Mission crew-car factory uses the selected Insurgent");car.Mods.NeonLightsColor=Color.Purple;car.Mods[VehicleModType.Engine].Index=2;vans.Capture(car);
  Check(vans.Select(CrewVan.FleetChoices.First(c=>c.Model=="granger"),true)&&state.CashOnHand==80000,"Returning to the owned Granger costs nothing");car=vans.Spawn(Vector3.Zero,0);Check((int)car.Mods.PrimaryColor==7,"Granger retains its separate saved customization");
  vans.Select(insurgent,true);car=vans.Spawn(Vector3.Zero,0);Check(car.Mods[VehicleModType.Engine].Index==2&&car.Mods.NeonLightsColor.ToArgb()==Color.Purple.ToArgb()&&state.CashOnHand==80000,"Re-selecting the Insurgent restores its upgrades and neon without rebuying");
  restored=CampaignState.Load(Path.Combine(root,"fleet19.json"));Check(restored.CrewVan.Model=="insurgent2"&&restored.CrewVan.Fleet.ContainsKey("granger"),"Crew fleet ownership and active selection survive reload");
  // The Foundry is lost in M22; the bunker must retain the same fleet commerce.
  state.Safehouses["cypressFoundry"]=false;state.Safehouses[BunkerSite.Unlock]=true;state.CashOnHand=200000;
  var baller=CrewVan.FleetChoices.First(x=>x.Model=="baller2");
  Check(vans.Select(baller,true)&&state.CashOnHand==160000&&state.CrewVan.Model=="baller2","Bunker-only owners can buy a crew vehicle at its listed price");
  Check(!vans.Select(baller,true)&&state.CashOnHand==160000,"Selecting the current model never charges twice");
  Check(vans.StashPosition==ctx.Locations.Position("M23.VehicleBay"),"Bunker fleet pickup moves to the exterior vehicle yard");
  Game.Player.Character.Position=vans.StashPosition.Value;vans.Update(crew,true);
  Check(vans.Current!=null&&vans.Current.Model.Name=="baller2"&&vans.Current.Position.DistanceTo(vans.StashPosition.Value)<2f,"Purchased crew vehicle appears at the bunker yard on return outside");
  Game.Player.Character.SetIntoVehicle(vans.Current,VehicleSeat.Driver);
  Check(!vans.Select(insurgent,true)&&vans.Current.Exists()&&state.CashOnHand==160000,"Bunker selection preserves occupied vehicles and cash");Game.Player.Character.Task.LeaveVehicle();
  Check(vans.Select(insurgent,true)&&state.CashOnHand==160000,"Previously owned crew vehicles remain free to select at the bunker");
  state.Safehouses[BunkerSite.Unlock]=false;
  Check(!vans.Select(baller,true)&&state.CashOnHand==160000,"Losing both headquarters blocks fleet purchases even with a stale menu");
  var menu=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Core","DevMenu.Shops.cs"));
  Check(menu.Contains("_homes.CanManageFleet && !_missions.IsRunning")&&!menu.Contains("_homes.FoundryVisit&&_homes.Apartment.Inside"),"The confirmation action uses shared headquarters access, not the old Foundry-only condition");
  var eject=typeof(VehiclePanelDamage).GetMethod("TryEjectCivilian",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static);
  var traffic=new Vehicle();var driver=new Ped();driver.SetIntoVehicle(traffic,VehicleSeat.Driver);
  eject.Invoke(null,new object[]{traffic,new Vector3(0,1,0),20f,new Vector3(0,25,0)});
  Check(!driver.IsInVehicle()&&driver.Health==0,"Severe civilian impact ejects before fatal injury instead of leaving a body at the horn");
  var protectedDriver=new Ped{IsPersistent=true};protectedDriver.SetIntoVehicle(traffic,VehicleSeat.Driver);eject.Invoke(null,new object[]{traffic,new Vector3(0,1,0),20f,new Vector3(0,25,0)});
  Check(protectedDriver.IsInVehicle()&&protectedDriver.Health>0,"Crew and mission actors are excluded from civilian crash ejection");
  var stuck=new Ped{StuckInSeat=true};stuck.SetIntoVehicle(traffic,VehicleSeat.Driver);eject.Invoke(null,new object[]{traffic,new Vector3(0,1,0),20f,new Vector3(0,25,0)});
  Check(stuck.IsInVehicle()&&stuck.Health>0,"Failed seat exit never kills a civilian still seated");
  Check(VehiclePanelDamage.SevereFrontalCrash(new Vector3(0,1,0),20,25)&&!VehiclePanelDamage.SevereFrontalCrash(new Vector3(0,1,0),4,25)&&!VehiclePanelDamage.SevereFrontalCrash(new Vector3(1,0,0),20,25),"Windshield ejection requires a severe frontal collision, not a scrape or side impact");
 }
}
