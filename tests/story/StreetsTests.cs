using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Native;

public static partial class StoryTests
{
 static void StreetsChecks()
 {
  Reset();var crew=Roster();var handoff=new MissionHandoff();Game.Disabled.Clear();Function.Calls.Clear();
  handoff.Update(crew,CrewSlot.Gohan);
  Check(handoff.IsWaiting&&Game.Disabled.Contains(Control.Attack)&&Game.Disabled.Contains(Control.MoveLeftRight),"Mandatory handoff blocks movement and gunfire");
  Check(Game.Player.CanControlCharacter&&!Game.Player.Character.IsPositionFrozen&&!Game.Disabled.Contains(Control.CharacterWheel)&&!Game.Disabled.Contains(Control.LookLeftRight),"Handoff leaves wheel, camera and engine player control available");
  Game.Disabled.Clear();handoff.Update(crew,null);Check(!handoff.IsWaiting&&Game.Disabled.Count==0,"No mission handoff leaves free-roam inputs untouched");
  var road=new Vehicle();Game.Player.Character.SetIntoVehicle(road,VehicleSeat.RightFront);Function.Calls.Clear();handoff.Update(crew,CrewSlot.Gohan);
  Check(!Function.Calls.Any(c=>c.Item1==Hash.SET_CONTROL_VALUE_NEXT_FRAME),"Passenger handoff never brakes the AI driver's chase");
  Game.Player.Character.SetIntoVehicle(road,VehicleSeat.Driver);Function.Calls.Clear();handoff.Update(crew,CrewSlot.Gohan);
  Check(Function.Calls.Any(c=>c.Item1==Hash.SET_CONTROL_VALUE_NEXT_FRAME),"Driver handoff requests a frame of braking without persisting handbrake state");
  var switcher=new SwitchController(crew);Check(switcher.TrySwitch(CrewSlot.Gohan),"Manual character switch succeeds during the mission input pause");
  Game.Disabled.Clear();handoff.Update(crew,CrewSlot.Gohan);Check(!handoff.IsWaiting&&Game.Disabled.Count==0,"Correct hero regains movement immediately after switching");
  var c=Context(crew);var m=new AuditMission();crew.SetActive(CrewSlot.Ice);m.Begin(c);m.Tick();Check(m.RequiredSwitch==CrewSlot.Gohan,"Composed mission exposes a required role to input handling");m.Abort();
  Reset();crew=Roster();var tuning=new WorldTuning();var shared=new HandlingData();var a=new Vehicle{HandlingData=shared};var b=new Vehicle{HandlingData=shared};World.Vehicles.Add(a);World.Vehicles.Add(b);
  tuning.Update(crew);Check(shared.InitialDriveMaxFlatVelocity==120,"Two cars sharing handling double the redline once");
  Check(shared.InitialDriveForce==.3f&&shared.DriveInertia==1f,"Top-speed profile does not change drive force or inertia");
  Game.GameTime+=1100;tuning.Update(crew);Check(shared.InitialDriveMaxFlatVelocity==120,"Repeated scans do not compound speed");
  tuning.Reset();Check(shared.InitialDriveMaxFlatVelocity==60,"Stand-down restores the original live gearing value");
  tuning.Update(crew);a.Present=false;b.Present=false;HandlingData.Models[b.Model.Name]=shared;tuning.Reset();
  Check(shared.InitialDriveMaxFlatVelocity==60,"Reload cleanup resolves shared model handling even after its last vehicle despawns");a.Present=true;b.Present=true;
  tuning.Update(crew);shared.InitialDriveMaxFlatVelocity=130;tuning.Reset();Check(shared.InitialDriveMaxFlatVelocity==130,"Cleanup preserves a later handling change from another owner");
  Reset();crew=Roster();var response=new TacticalResponse();var stolen=new Vehicle();Game.Player.Character.SetIntoVehicle(stolen,VehicleSeat.Driver);Game.Player.WantedLevel=3;
  var officer=new Ped{IsCop=true,RelationshipGroup=9};var enemy=new Ped{RelationshipGroup=10,CombatTarget=Game.Player.Character};var neutral=new Ped{RelationshipGroup=11};World.Nearby=new[]{officer,enemy,neutral,crew.PedFor(CrewSlot.Gohan)};Function.Calls.Clear();
  response.Update(crew);Check(stolen.IsWanted,"Newly occupied car inherits the current pursuit's wanted status");
  Check(officer.Task.Fights==0&&officer.Task.Chases==0,"Police tuning does not replace search or arrest tasks");
  Check(Function.Calls.Any(x=>x.Item1==Hash.SET_PED_COMBAT_ATTRIBUTES&&x.Item2[0]==enemy)&&!Function.Calls.Any(x=>x.Item1==Hash.SET_PED_COMBAT_ATTRIBUTES&&x.Item2[0]==neutral),"Combat tuning improves active enemies and ignores neutral bystanders");
  Check(TacticalResponse.DispatchInterval(5)<TacticalResponse.DispatchInterval(3)&&TacticalResponse.DispatchInterval(3)<TacticalResponse.DispatchInterval(1),"Higher wanted levels shorten dispatch replenishment intervals");
  Game.Player.WantedLevel=0;response.Update(crew);Check(!stolen.IsWanted&&Game.Player.WantedLevel==0,"Losing police normally clears the mod's car flag without renewing wanted level");response.Reset();World.Nearby=new Ped[0];
  Reset();crew=Roster();var state=CampaignState.Load(Path.Combine(root,"shopping-test.json"));state.CashOnHand=100;
  int calls=0;Check(!ShopPurchase.Try(state,101,()=>{calls++;return true;})&&calls==0&&state.CashOnHand==100,"Insufficient funds cannot apply an item or spend money");
  Check(!ShopPurchase.Try(state,50,()=>false)&&state.CashOnHand==100,"Failed or duplicate item application costs nothing");
  Check(ShopPurchase.Try(state,50,()=>true)&&state.CashOnHand==50,"Successful purchase deducts exactly its displayed price");
  var service=new ShopService(crew,state,new WeaponProgression(state),new CrewMemory(state)){Allowed=()=>true};var shop=ShopService.Sites[0];Game.Player.Character.Position=shop.Position;
  state.CashOnHand=2000;Check(service.BuyWeapon(shop,ShopService.Stock[0])&&state.CashOnHand==500,"Ammu-Nation sells a real weapon and charges crew cash");
  Check(!service.BuyWeapon(shop,ShopService.Stock[0])&&state.CashOnHand==500,"Buying an owned weapon cannot charge twice");
  Check(CampaignState.Load(Path.Combine(root,"shopping-test.json")).CashOnHand==500,"Shop balance persists to the campaign save");
  Game.Player.WantedLevel=1;Check(!service.CanUse(shop),"Shops close transactions during an active pursuit");Game.Player.WantedLevel=0;
  var workshop=ShopService.Sites.Last();crew.SetActive(CrewSlot.Guess);Game.Player.Character=crew.PedFor(CrewSlot.Guess);var vehicle=new Vehicle{Position=workshop.Position};Game.Player.Character.SetIntoVehicle(vehicle,VehicleSeat.Driver);
  Check(service.Price(workshop,1000)==500&&service.Repair(workshop)&&state.CashOnHand==500,"Guess gets discounted parts and his own labor costs nothing");
  vehicle.Speed=2f;Check(!service.Fit(workshop,VehicleModType.Brakes,0)&&state.CashOnHand==500,"Moving vehicles cannot consume garage purchases");vehicle.Speed=0;
  Check(service.Fit(workshop,VehicleModType.Brakes,0)&&vehicle.Mods[VehicleModType.Brakes].Index==0&&state.CashOnHand==0,"Compatible stopped-car parts apply and charge once");
 }
}
