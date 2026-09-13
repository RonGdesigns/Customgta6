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
 static void PlaythroughRepairChecks()
 {
  Reset();var crew=Roster();var c=Context(crew);var state=c.State=CampaignState.Load(Path.Combine(root,"playthrough-arrival.json"));
  var car=new Vehicle{Position=new Vector3(50,80,12)};World.Vehicles.Add(car);Use(crew,CrewSlot.Guess);
  Game.Player.Character.SetIntoVehicle(car,VehicleSeat.Driver);crew.PedFor(CrewSlot.Gohan).SetIntoVehicle(car,VehicleSeat.RightFront);
  var probe=new ProbeMission();var def=new MissionDefinition{Info=new MissionInfo{Id="M04",Title="Arrival probe"},Factory=()=>probe};var catalog=new MissionCatalog();catalog.All.Add(def);
  var manager=new MissionManager(c,state,catalog);int deployments=0;manager.BeforeGameplay=()=>deployments++;
  Check(manager.Start(def,true)&&deployments==0&&Game.Player.Character.IsInVehicle(car)&&crew.PedFor(CrewSlot.Gohan).IsInVehicle(car),"Mission start keeps actual car and passengers through the briefing before deployment");
  manager.Abort();Check(deployments==0&&car.Present&&!car.IsPositionFrozen,"Aborting a briefing never deploys gameplay or loses the arriving car");
  manager.Start(def,true);c.Cutscenes.Skip();manager.Update();Check(deployments==1&&probe.Begins==1,"Completing the briefing deploys gameplay once");manager.Abort();

  Reset();var a=new Ped();var b=new Ped();car=new Vehicle();var together=new TogetherStep(new EnterVehicleStep(a,car,VehicleSeat.Driver),new EnterVehicleStep(b,car,VehicleSeat.RightFront));
  var blocking=new SceneBlocking().Then(together);Check(blocking.Actors.Count()==2,"Concurrent boarding exposes both actors for cutscene unfreezing");
  blocking.Update();Check(a.Task.Enters==1&&b.Task.Enters==1&&together.TimeoutMs==6500,"Both boarding animations begin in the same bounded beat");
  blocking.Complete();Check(blocking.Succeeded&&a.IsInVehicle(car)&&b.IsInVehicle(car),"Skipping concurrent boarding reaches both assigned seats");

  ObjectiveMarkers.Clear();ObjectiveMarkers.ActiveSlot=CrewSlot.Guess;
  ObjectiveMarkers.BeginFrame(true);ObjectiveMarkers.Navigation(new Vector3(100,100,10));ObjectiveMarkers.EndFrame();var old=World.LastBlip;
  ObjectiveMarkers.BeginFrame(true);ObjectiveMarkers.Navigation(new Vector3(105,100,10));ObjectiveMarkers.EndFrame();Check(World.LastBlip==old,"Small target motion does not recreate the GPS every frame");
  ObjectiveMarkers.BeginFrame(true);ObjectiveMarkers.Navigation(new Vector3(1500,100,10));ObjectiveMarkers.EndFrame();Check(!old.Present&&World.LastBlip.ShowRoute&&World.LastBlip.Position.X==1500,"A new escape destination replaces GTA's cached route to the chase target");ObjectiveMarkers.Clear();

  Reset();crew=Roster();c=Context(crew);state=c.State=CampaignState.Load(Path.Combine(root,"garage-safety.json"));state.CashOnHand=300000;
  var garage=new GarageService(crew,state,c.Locations,null){Allowed=()=>true};var bay=GarageService.Site("bay-guess");var choice=StoryVehicles.Catalog.First(v=>v.Model=="sultanrs");
  garage.Allowed=()=>false;Check(!garage.BuyFromDealer(choice,bay)&&state.CashOnHand==300000&&state.Vehicles.Count==0,"Mission restrictions are enforced by the purchase itself, not only its menu");
  garage.Allowed=()=>true;garage.BuyFromDealer(choice,bay);var record=state.Vehicles.Single();car=garage.Retrieve(record);Use(crew,CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(car,VehicleSeat.Driver);int cash=state.CashOnHand;
  Check(!garage.Sell(record)&&car.Present&&state.Vehicles.Contains(record)&&state.CashOnHand==cash,"An occupied car cannot be sold or deleted");
  garage.Clear();Check(garage.Retrieve(record)==car,"Crew redeployment cannot duplicate an existing owned car");
  Game.Player.Character.Task.LeaveVehicle();crew.PedFor(CrewSlot.Ice).SetIntoVehicle(car,VehicleSeat.Driver);Game.Player.Character.Position=car.Position+new Vector3(1000,0,0);garage.Update(true);Game.GameTime+=61000;garage.Update(true);
  Check(car.Present&&crew.PedFor(CrewSlot.Ice).IsInVehicle(car),"Automatic garage return never deletes a companion's occupied car");
  crew.PedFor(CrewSlot.Ice).Task.LeaveVehicle();garage.Clear();
  record.Finish["extraColor0"]=42;record.Finish["toggle18"]=1;record.Finish["neon0"]=1;record.Finish["neonColor0"]=15;record.Finish["neonColor1"]=180;record.Finish["neonColor2"]=255;
  state.Save();var loaded=CampaignState.Load(Path.Combine(root,"garage-safety.json"));
  Check(loaded.Vehicles[0].Finish["extraColor0"]==42&&loaded.Vehicles[0].Finish["toggle18"]==1&&loaded.Vehicles[0].Finish["neonColor2"]==255,"Vehicle finish values survive save/load alongside indexed mods");
  Reset();crew=Roster();c=Context(crew);string salesPath=Path.Combine(root,"daily-sales.json");state=CampaignState.Load(salesPath);garage=new GarageService(crew,state,c.Locations,null){Allowed=()=>true};
  var buyer=ShopService.Sites.First(site=>site.Kind==ShopKind.Dealer);Game.Player.Character.Position=buyer.Position;
  for(int i=0;i<10;i++){var saleCar=new Vehicle{Position=buyer.Position};Check(garage.SellStreetVehicle(saleCar),"Free-roam car sale "+(i+1)+" succeeds within the GTA-day limit");}
  var eleventh=new Vehicle{Position=buyer.Position};cash=state.CashOnHand;
  Check(!garage.SellStreetVehicle(eleventh)&&eleventh.Present&&state.CashOnHand==cash&&garage.SalesRemaining==0,"The eleventh sale changes neither the vehicle nor the balance");
  state=CampaignState.Load(salesPath);garage=new GarageService(crew,state,c.Locations,null){Allowed=()=>true};
  Check(garage.SalesRemaining==0&&!garage.SellStreetVehicle(eleventh),"Reloading the save does not reset the daily sales limit");
  World.CurrentDate=World.CurrentDate.AddDays(-1);Check(garage.SalesRemaining==0,"Rewinding the GTA date cannot reset the limit");
  World.CurrentDate=World.CurrentDate.AddDays(2);Check(garage.SalesRemaining==10&&garage.SellStreetVehicle(eleventh)&&garage.SalesRemaining==9,"A later GTA midnight opens the next ten sales");
  var stored=new OwnedVehicle{Id=20,ModelName="primo",Garage="bay-guess",Price=12000};state.Vehicles.Add(stored);Check(garage.Sell(stored)&&garage.SalesRemaining==8,"Stored and direct car sales share the same daily allowance");
  garage.Clear();
  Reset();crew=Roster();c=Context(crew);state=CampaignState.Load(Path.Combine(root,"failed-delivery.json"));state.CashOnHand=5000;garage=new GarageService(crew,state,c.Locations,null){Allowed=()=>true};
  var first=new OwnedVehicle{Id=1,ModelName="primo",Garage="bay-guess",Price=20000};var repair=new OwnedVehicle{Id=2,ModelName="emperor",Garage="bay-ice",Price=20000,InShop=true};state.Vehicles.Add(first);state.Vehicles.Add(repair);
  Check(garage.CallKJ(first),"The first KJ delivery starts normally");var pending=garage.DeliveryVehicle;World.FailVehicles=true;cash=state.CashOnHand;
  Check(!garage.CallKJ(repair)&&garage.DeliveryVehicle==pending&&pending.Present&&state.CashOnHand==cash&&repair.InShop,"A failed replacement delivery preserves the first delivery, repair state and money");
  World.FailVehicles=false;garage.Clear();
  Reset();crew=Roster();Use(crew,CrewSlot.Guess);Function.Held[Control.Aim]=true;Function.Calls.Clear();CombatReticle.Draw(true,true);
  Check(!Function.Calls.Any(call=>call.Item1==Hash.DRAW_RECT),"Menus and scenes suppress the custom crosshair");
  Function.Calls.Clear();CombatReticle.Draw(true,false);Check(Function.Calls.Count(call=>call.Item1==Hash.DRAW_RECT)==8,"Ordinary aiming draws four outlined high-contrast crosshair arms");
  Function.Calls.Clear();CombatReticle.Draw(false,false);Check(Function.Calls.Count==0,"The crosshair setting disables the overlay completely");
 }
}
