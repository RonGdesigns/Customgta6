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
 static uint Weapon(string key)=>unchecked((uint)Game.GenerateHash(key));
 static void MarketChecks()
 {
  Reset();var crew=Roster();var state=CampaignState.Load(Path.Combine(root,"market.json"));state.CashOnHand=250000;
  var shop=ShopService.Sites.First();Game.Player.Character.Position=shop.Position;
  var service=new ShopService(crew,state,new WeaponProgression(state),new CrewMemory(state)){Allowed=()=>true};
  var pump=ShopService.Stock.First(w=>w.Key=="WEAPON_PUMPSHOTGUN");
  Check(WeaponMarket.LockedUntil(state,CrewSlot.Ice,pump.Hash)=="M03","Shop derives story lock from the actual M03 reward");
  Check(!service.BuyWeapon(shop,pump)&&state.CashOnHand==250000&&!Game.Player.Character.Weapons.Owned.Contains((WeaponHash)pump.Hash),"Locked purchase neither gives a gun nor charges money");
  Check(WeaponMarket.LockedUntil(state,CrewSlot.Gohan,Weapon("WEAPON_PISTOL_MK2"))=="SM05","Solo reward gate excludes filler rewards for the other heroes");
  Check(WeaponMarket.LockedUntil(state,CrewSlot.Guess,Weapon("WEAPON_TACTICALRIFLE"))==null,"Guess's starting M16 remains available");
  var hell=WeaponProgression.DlcCatalog.First(w=>w.Key=="WEAPON_RAYCARBINE");
  Check(WeaponMarket.LockedUntil(state,CrewSlot.Ice,hell.Hash)==null&&WeaponMarket.Price(hell)==100000,"Powerful extra DLC gun is purchasable at its premium campaign price");
  Check(service.BuyWeapon(shop,hell)&&state.CashOnHand==150000&&WeaponMarket.Owned(state,crew.ActiveSlot,hell.Hash),"Extra weapon purchase records ownership for the purchasing hero");
  Check(!WeaponMarket.Owned(state,CrewSlot.Gohan,hell.Hash),"One hero's purchased weapon does not unlock another's inventory");
  Check(!service.BuyWeapon(shop,hell)&&state.CashOnHand==150000,"Premium duplicate purchase is not charged");
  state.Completed.Add("M03");Check(service.BuyWeapon(shop,pump)&&state.CashOnHand==145500,"Completed mission opens the previously locked gun at the listed price");
  var rail=WeaponProgression.DlcCatalog.First(w=>w.Key=="WEAPON_RAILGUNXM3");
  Check(!service.BuyWeapon(shop,rail)&&state.CashOnHand==145500,"Insufficient cash blocks the strongest DLC weapon");
  state.CashOnHand=200000;Check(service.BuyWeapon(shop,rail)&&Game.Player.Character.Weapons.LastAmmo==3&&state.CashOnHand==20000,"Railgun sale includes three shots rather than 120 explosive rounds");
  Check(WeaponMarket.AmmoPrice(rail.Hash)>WeaponMarket.AmmoPrice(pump.Hash)&&WeaponMarket.AmmoCount(rail.Hash)==3,"Special ammo has a premium price and bounded pack size");
  var clothing=ShopService.Sites.First(s=>s.Kind==ShopKind.Clothing);Game.Player.Character.Position=clothing.Position;
  Check(service.CanUse(clothing)&&ShopService.Sites.Count(s=>s.Kind==ShopKind.Clothing)==13,"Thirteen clothing storefronts offer on-foot access");
  var vehicle=new Vehicle{Position=clothing.Position};Game.Player.Character.SetIntoVehicle(vehicle,VehicleSeat.Driver);
  Check(!service.CanUse(clothing)&&service.Car(clothing)==null,"Clothing store cannot open in a vehicle or become a garage");
  CrewAppearance.Load(Path.Combine(root,"market-looks.ini"));foreach(var hero in Protagonist.All){int hair=CrewAppearance.For(hero.Slot).Hair,color=CrewAppearance.For(hero.Slot).HairColor;CrewAppearance.Adjust(Game.Player.Character,hero.Slot,"Hair",1);CrewAppearance.Adjust(Game.Player.Character,hero.Slot,"HairColor",1);Check(CrewAppearance.For(hero.Slot).Hair==hair&&CrewAppearance.For(hero.Slot).HairColor==color,"Hair identity is locked for "+hero.Handle);}
  var garage=ShopService.Sites.Last();Game.Player.Character.Position=vehicle.Position=garage.Position;state.CashOnHand=10000;
  vehicle.Mods.WheelType=3;vehicle.Mods[VehicleModType.FrontWheel].Index=1;vehicle.Mods[VehicleModType.RearWheel].Index=2;vehicle.Mods[VehicleModType.FrontWheel].Custom=true;
  Check(service.WheelCount(garage,11,false)==3&&vehicle.Mods.WheelType==3&&vehicle.Mods[VehicleModType.FrontWheel].Index==1&&vehicle.Mods[VehicleModType.RearWheel].Index==2&&vehicle.Mods[VehicleModType.FrontWheel].Custom&&state.CashOnHand==10000,"Browsing Street wheels restores both axles, family and custom tires without charging");
  Check(!service.Wheel(garage,11,false,99,false)&&vehicle.Mods.WheelType==3&&vehicle.Mods[VehicleModType.FrontWheel].Index==1&&state.CashOnHand==10000,"Unsupported wheel purchase rolls back its temporary family change");
  Check(service.Wheel(garage,11,false,0,true)&&vehicle.Mods.WheelType==11&&vehicle.Mods[VehicleModType.FrontWheel].Index==0&&vehicle.Mods[VehicleModType.RearWheel].Index==2&&state.CashOnHand==9400,"Valid Street wheels charge Guess Customs price and preserve car hydraulics in slot 24");
  Check(!service.Wheel(garage,11,true,0,false)&&service.WheelCount(garage,11,true)==0,"A car's hydraulic mod slot is never offered as a motorcycle rear wheel");
  Check(service.TogglePart(garage,VehicleToggleModType.Turbo,true)&&!service.TogglePart(garage,VehicleToggleModType.Turbo,true)&&state.CashOnHand==6400,"Turbo can be bought once at the shown discounted price");
  Check(!service.Extra(garage,19,true)&&state.CashOnHand==6400,"Unsupported vehicle extra costs nothing");
  Check(service.Extra(garage,1,true)&&vehicle.IsExtraOn(1),"Supported online/model extra actually switches on");
  Check(!service.Rgb(garage,3,Color.Red),"Tire smoke color requires the purchased smoke kit");
  Check(service.Rgb(garage,0,Color.Red)&&vehicle.Mods.IsPrimaryColorCustom&&!vehicle.Mods.IsSecondaryColorCustom,"Custom primary paint does not recolor the secondary channel");
  Reset();crew=Roster();var tune=new WorldTuning();var plane=new Vehicle{Model=new Model("vestra"),IsEngineRunning=true,IsInAir=true,Velocity=new Vector3(0,60,0)};
  var boat=new Vehicle{Model=new Model("longfin"),IsEngineRunning=true,IsInWater=true,Velocity=new Vector3(0,60,0)};
  var heli=new Vehicle{Model=new Model("supervolito"),IsEngineRunning=true,IsInAir=true};var bike=new Vehicle{Model=new Model("shinobi"),IsEngineRunning=true};
  var sub=new Vehicle{Model=new Model("sub"){IsCar=false,IsSubmarine=true},IsEngineRunning=true,IsInWater=true};
  var train=new Vehicle{Model=new Model("train"){IsCar=false,IsTrain=true},IsEngineRunning=true};
  World.Vehicles.AddRange(new[]{plane,boat,heli,bike,sub,train});tune.Update(crew);
  Check(Function.Calls.Count(c=>c.Item1==Hash.SET_VEHICLE_MAX_SPEED&&(float)c.Item2[1]==120f)==5,"Planes, boats, bikes, submarines and trains enter the doubled-ceiling system; helicopters stay stock");
  Check(plane.HandlingData.InitialDriveMaxFlatVelocity==60&&boat.HandlingData.InitialDriveMaxFlatVelocity==60&&bike.HandlingData.InitialDriveMaxFlatVelocity==120,"Aircraft and boats never receive car gearing edits");
  Check(plane.HandlingData.FlyingHandlingData.ThrustFallOff==.5f&&plane.HandlingData.FlyingHandlingData.VectorSpeedResistance==new Vector3(2,2,6),"Flight adapter reduces longitudinal losses while preserving lateral and vertical damping");
  Check(boat.HandlingData.BoatHandlingData.DragCoefficient==5f,"Marine adapter adjusts water resistance through the runtime public API");
  Game.LastFrameTime=.1f;for(int i=0;i<25;i++){Game.GameTime+=100;tune.Update(crew);}
  Check((float)Function.Calls.Last(c=>c.Item1==Hash.SET_VEHICLE_CHEAT_POWER_INCREASE&&c.Item2[0]==plane).Item2[1]>1.4f,"Airborne forward flight receives gradual cruise power");
  Check(!Function.Calls.Any(c=>(c.Item1==Hash.SET_VEHICLE_CHEAT_POWER_INCREASE||c.Item1==Hash.SET_VEHICLE_MAX_SPEED)&&c.Item2[0]==heli)&&heli.HandlingData.FlyingHandlingData.ThrustFallOff==1f,"A helicopter is left stock: no power, no ceiling, no flight handling edit");
  plane.IsInAir=false;boat.IsInWater=false;Check(!WorldTuning.CanAssist(plane)&&!WorldTuning.CanAssist(boat),"Taxiing aircraft and beached boats do not receive cruise power");
  Check(boat.HandlingData.BoatHandlingData.DragCoefficient==5f,"Repeated marine scans do not compound resistance reduction");
  tune.Reset();Check(plane.HandlingData.FlyingHandlingData.ThrustFallOff==1f&&plane.HandlingData.FlyingHandlingData.VectorSpeedResistance==new Vector3(2,4,6)&&boat.HandlingData.BoatHandlingData.DragCoefficient==10f,"Stand-down restores aircraft and marine handling");
  tune.Update(crew);boat.HandlingData.BoatHandlingData.DragCoefficient=7f;tune.Reset();Check(boat.HandlingData.BoatHandlingData.DragCoefficient==7f,"Cleanup preserves another mod's later marine handling edit");Game.LastFrameTime=.016f;
 }
}
