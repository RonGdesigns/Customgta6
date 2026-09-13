using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Math;
using GTA.Native;

public static partial class StoryTests
{
 static void NitrousAndShopGarageChecks()
 {
  Reset();var crew=Roster();Use(crew,CrewSlot.Guess);var ctx=Context(crew);var nitro=new Nitrous();
  var car=new Vehicle{Model=new Model("sultan"),IsEngineRunning=true,Velocity=new Vector3(0,15,0),Speed=15};World.Vehicles.Add(car);Game.Player.Character.SetIntoVehicle(car,VehicleSeat.Driver);
  Game.LastFrameTime=.1f;Function.Held[(Control)73]=true;Function.Held[(Control)71]=true;Game.Disabled.Clear();
  nitro.Update(false);Check(!nitro.Boosting&&!Game.Disabled.Contains((Control)73),"Unfitted cars keep normal duck input and receive no nitrous");
  car.Mods[(VehicleToggleModType)17].IsInstalled=true;nitro.Update(false);
  Check(nitro.Boosting&&nitro.MultiplierFor(car)>1&&nitro.Charge<1&&Game.Disabled.Contains((Control)73)&&Game.Disabled.Contains((Control)337),"Purchased nitrous converts A/X into charged boost and suppresses conflicting duck/hydraulics actions");
  var tuning=new WorldTuning();tuning.Nitrous.Update(false);tuning.Update(crew);
  Check(Function.Calls.Any(p=>p.Item1==Hash.SET_VEHICLE_CHEAT_POWER_INCREASE&&p.Item2[0]==car&&(float)p.Item2[1]>=Nitrous.TorqueMultiplier),"First boost registers the car immediately and composes with the single torque writer");
  tuning.Reset();Check(!tuning.Nitrous.Boosting,"World teardown clears nitrous demand");
  float remaining=nitro.Charge;nitro.Update(true);Check(!nitro.Boosting&&nitro.MultiplierFor(car)==1&&nitro.Charge==remaining,"Menus, scenes, switching and recovery can block boost without consuming the bottle");
  foreach(int control in new[]{72,76}){Function.Held[(Control)control]=true;nitro.Update(false);Check(!nitro.Boosting,"Braking or handbraking cancels nitrous");Function.Held[(Control)control]=false;}
  car.IsInAir=true;nitro.Update(false);Check(!nitro.Boosting,"Airborne cars receive no extra thrust");car.IsInAir=false;
  car.Velocity=new Vector3(0,-10,0);nitro.Update(false);Check(!nitro.Boosting,"Reverse travel cannot trigger forward nitrous");car.Velocity=new Vector3(0,15,0);
  for(int i=0;i<60;i++){Game.GameTime+=100;nitro.Update(false);}Check(nitro.Charge==0&&!nitro.Boosting,"A held button exhausts the five-second bottle and cannot pulse free boost");
  Function.Held[(Control)73]=false;Game.GameTime+=2100;for(int i=0;i<50;i++){Game.GameTime+=100;nitro.Update(false);}Check(nitro.Charge>.24f&&nitro.Charge<.26f,"Releasing recharges approximately a quarter of the bottle in five seconds after its delay");
  remaining=nitro.Charge;Game.Player.Character.SetIntoVehicle(car,VehicleSeat.RightFront);Function.Held[(Control)73]=true;nitro.Update(false);Check(!nitro.Boosting&&nitro.Charge==remaining,"Passengers cannot boost the AI driver's car");
  Game.Player.Character.SetIntoVehicle(car,VehicleSeat.Driver);nitro.Update(false);Check(nitro.Charge<remaining&&nitro.Charge<.3f,"Returning to the driver seat preserves the same bottle instead of refilling it");Game.LastFrameTime=.016f;

  Reset();crew=Roster();Use(crew,CrewSlot.Guess);ctx=Context(crew);var state=CampaignState.Load(Path.Combine(root,"shop-garage23.json"));state.CashOnHand=25000;
  var vans=new CrewVan(state,ctx.Locations);var garages=new GarageService(crew,state,ctx.Locations,vans){Allowed=()=>true};var shops=new ShopService(crew,state,new WeaponProgression(state),new CrewMemory(state)){Allowed=()=>true};
  var shop=ShopService.Sites.First(s=>s.Kind==ShopKind.Customs);var bay=garages.HomeSite(CrewSlot.Guess);var other=garages.HomeSite(CrewSlot.Ice);
  car=new Vehicle{Model=new Model("sultan"),Position=shop.Position,IsEngineRunning=true,Speed=0};World.Vehicles.Add(car);Game.Player.Character.Position=shop.Position;Game.Player.Character.SetIntoVehicle(car,VehicleSeat.Driver);car.Mods.PrimaryColor=(VehicleColor)12;
  Check(shops.BeginVehiclePreview(shop,()=>shops.TogglePart(shop,(VehicleToggleModType)17,true)),"Nitrous uses a reversible shop preview");
  Check(!garages.SaveFromShop(shops,shop,bay)&&state.Vehicles.Count==0&&state.CashOnHand==25000,"Preview nitrous cannot be saved or charged before confirmation");shops.CancelVehiclePreview();
  Check(!Nitrous.Installed(car)&&state.CashOnHand==25000,"Canceling restores the unpurchased nitrous toggle");
  shops.BeginVehiclePreview(shop,()=>shops.TogglePart(shop,(VehicleToggleModType)17,true));Check(shops.ConfirmVehiclePreview()&&Nitrous.Installed(car)&&state.CashOnHand==23800,"Confirmed nitrous installs and charges once");
  int cash=state.CashOnHand;
  Check(garages.SaveFromShop(shops,shop,bay)&&car.Exists()&&Game.Player.Character.IsInVehicle(car)&&state.Vehicles.Count==1&&garages.IsOut(state.Vehicles[0]),"Customs saves to an owned garage without removing the car or ejecting its driver");
  var id=state.Vehicles[0].Id;
  Check(state.Vehicles[0].Finish["toggle17"]==1&&state.Vehicles[0].PrimaryColor==12&&state.CashOnHand==cash,"Saved build includes nitrous and paint without another charge");
  car.Mods.PrimaryColor=(VehicleColor)15;Check(garages.SaveFromShop(shops,shop,bay)&&state.Vehicles.Count==1&&state.Vehicles[0].Id==id&&state.Vehicles[0].PrimaryColor==15,"Resaving the same car updates its build even when its one-slot home is full");
  var snapshot=CampaignState.Load(Path.Combine(root,"shop-garage23.json"));var restored=new Vehicle();GarageService.Apply(restored,snapshot.Vehicles[0]);Check(Nitrous.Installed(restored)&&(int)restored.Mods.PrimaryColor==15,"Nitrous and confirmed paint survive save reload and vehicle reconstruction");
  var second=new Vehicle{Model=new Model("primo"),Position=shop.Position};Game.Player.Character.SetIntoVehicle(second,VehicleSeat.Driver);
  Check(!garages.SaveFromShop(shops,shop,bay)&&state.Vehicles.Count==1,"A full garage refuses another car without overwriting the first");
  Game.Player.Character.SetIntoVehicle(car,VehicleSeat.Driver);Check(garages.SaveFromShop(shops,shop,other)&&garages.Used(bay)==0&&garages.Used(other)==1&&state.Vehicles.Count==1,"Choosing a different garage transfers the existing record without duplicating the car");
  Game.Player.WantedLevel=1;Check(!garages.SaveFromShop(shops,shop,bay),"Wanted state blocks shop garage transactions");Game.Player.WantedLevel=0;
  car.Speed=8;Check(!garages.SaveFromShop(shops,shop,bay),"Moving cars cannot be saved through a stale shop menu");car.Speed=0;
  Game.Player.Character.Position=shop.Position+new Vector3(100,0,0);Check(!garages.SaveFromShop(shops,shop,bay),"Shop save rechecks proximity at confirmation");
  Game.Player.Character.Position=shop.Position;Check(!garages.SaveFromShop(shops,shop,GarageService.Site("pillbox")),"Unowned garages cannot receive a shop save");
  var build=state.Vehicles[0].Fingerprint();Function.ThrowOnce=Hash.GET_VEHICLE_MOD;
  Check(!garages.SaveFromShop(shops,shop,other)&&state.Vehicles[0].Fingerprint()==build&&state.Vehicles.Count==1,"A failed native capture leaves the previous build and capacity unchanged");
  Check(!shops.TogglePart(shop,(VehicleToggleModType)19,true)&&state.CashOnHand==cash,"Unsupported raw toggle slots cannot take money");
 }
}
