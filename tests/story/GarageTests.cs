using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Math;

public static partial class StoryTests
{
 static void GarageChecks()
 {
  // ---- Garages, the dealer and KJ's drops (Ron, September 12).
  Reset();var crew=Roster();var c=Context(crew);var path=Path.Combine(root,"garages.json");var state=c.State=CampaignState.Load(path);state.CashOnHand=100000;
  var garages=new GarageService(crew,state,c.Locations,null);garages.Allowed=()=>true;
  var bay=GarageService.Sites.First(s=>s.Id=="bay-guess");var eclipse=GarageService.Sites.First(s=>s.Id=="eclipse");var lot=GarageService.Sites.First(s=>s.Id=="mission-row");
  Check(GarageService.Sites.Length==13&&garages.Owned(bay)&&!garages.Owned(eclipse)&&!garages.Owned(lot)&&garages.Position(lot).HasValue,"The three street bays are the crew's from the start, the Eclipse garage waits for its tier, a city garage is bought, and every door has a place on the map");
  Check(garages.HomeSite(CrewSlot.Guess)==bay,"A starter home's garage is its street bay");
  Check(garages.Buy(lot)&&garages.Owned(lot)&&state.CashOnHand==82000&&state.Garages.Contains(lot.Id),"Buying a garage takes its price from crew cash and records it");
  Check(!garages.Buy(lot)&&state.CashOnHand==82000,"A garage is bought once");
  state.CashOnHand=1000;Check(!garages.Buy(GarageService.Sites.First(s=>s.Id=="pillbox"))&&state.CashOnHand==1000,"A garage the crew cannot afford stays on the market");state.CashOnHand=82000;
  // Store the car the player drove in: off the street, with its build.
  Use(crew,CrewSlot.Guess);var car=new Vehicle{Model=new Model("sultanrs"),DisplayName="SULTANRS"};car.Mods.PrimaryColor=(VehicleColor)12;car.Mods.Livery=2;World.Vehicles.Add(car);
  var door=garages.Position(lot).Value;Game.Player.Character.Position=door;car.Position=door;Game.Player.Character.SetIntoVehicle(car,VehicleSeat.Driver);
  Check(garages.ArrivalVehicle()==car,"The car under the player is the one the door offers to store");
  Check(garages.Store(lot,car)&&state.Vehicles.Count==1&&state.Vehicles[0].Garage==lot.Id&&state.Vehicles[0].Stolen&&state.Vehicles[0].PrimaryColor==12&&state.Vehicles[0].Livery==2&&state.Vehicles[0].Label=="Sultanrs"&&!car.Present&&!Game.Player.Character.IsInVehicle(),"A car driven into an owned garage is kept, off the street or not, with its build; the car itself is gone from the world and the player is out of it");
  var kept=state.Vehicles[0];
  var second=new Vehicle{Model=new Model("primo")};var third=new Vehicle{Model=new Model("emperor")};
  Check(garages.Store(lot,second)&&!garages.Store(lot,third)&&state.Vehicles.Count==2&&garages.Used(lot)==2&&!garages.HasFreeSlot(lot),"A two-bay garage takes two; the third is refused");
  // Out front, and back on its own when left behind.
  var outCar=garages.Retrieve(kept);
  Check(outCar!=null&&outCar.Exists()&&(int)outCar.Mods.PrimaryColor==12&&outCar.Mods.Livery==2&&garages.IsOut(kept)&&outCar.Position.DistanceTo(door)<12f,"Taking a car out spawns it at the door with its build");
  Check(garages.Retrieve(kept)==outCar,"A car already out is not spawned twice");
  Game.Player.Character.Position=door+new Vector3(500,0,0);garages.Update(true);Game.GameTime+=GarageService.ReturnAfterMs+1;garages.Update(true);
  Check(!outCar.Present&&!garages.IsOut(kept)&&kept.Garage==lot.Id,"A car left more than 300 m behind for a minute goes back to its garage");
  // KJ brings a car to the player.
  Game.Player.Character.Position=door;Game.Player.Character.ForwardVector=new Vector3(0,1,0);
  Check(garages.CallKJ(kept)&&garages.DeliveryActive&&garages.KJ!=null&&garages.DeliveryVehicle!=null,"KJ answers: a car and its driver on the road");
  var drop=garages.DeliveryVehicle;var kj=garages.KJ;
  Check(kj.IsInVehicle(drop)&&kj.Task.Drives>=1&&drop.Position.DistanceTo(Game.Player.Character.Position)>=80f&&kj.Model.Name==GarageService.KJModel&&(int)drop.Mods.PrimaryColor==12,"KJ starts well out of sight, driving the car in with its build on it");
  Check(!garages.CallKJ(kept)&&garages.DeliveryActive,"Calling for the same car again is not a second car");
  Game.Player.Character.Position=door+new Vector3(25,0,0);drop.Position=Game.Player.Character.Position+new Vector3(6,0,0);drop.Speed=0f;garages.Update(true);
  Check(!kj.IsInVehicle()&&!garages.DeliveryActive&&garages.IsOut(kept)&&GameUtils.Message.Contains("KJ"),"At the player KJ gets out, says his piece, and the car is the crew's to drive");
  Game.GameTime+=21000;garages.Update(true);Check(kj.Released,"KJ walks off and is released to the world");
  // Selling and tagging.
  int cash=state.CashOnHand;int sale=VehiclePricing.Sale(kept);
  Check(garages.Sell(kept)&&state.Vehicles.Count==1&&state.CashOnHand==cash+sale&&!drop.Present,"Selling pays half the car's value, clears the bay and takes the car off the street");
  // The dealer delivers to a garage with a bay.
  var choice=StoryVehicles.Catalog.First(v=>v.Model=="sultanrs");state.CashOnHand=100000;
  Check(garages.BuyFromDealer(choice,lot)&&state.Vehicles.Count==2&&state.Vehicles.Last().ModelName=="sultanrs"&&state.Vehicles.Last().Garage==lot.Id&&!state.Vehicles.Last().Stolen&&state.CashOnHand==100000-VehiclePricing.Of(choice),"A dealer purchase records the car in the chosen garage and charges the crew");
  Check(!garages.BuyFromDealer(choice,lot),"A full garage takes no delivery");
  Check(garages.BuyFromDealer(choice,bay)&&!garages.BuyFromDealer(choice,bay),"The free street bay holds one car");
  state.CashOnHand=100;Check(!garages.BuyFromDealer(choice,GarageService.Sites.First(s=>s.Id=="bay-ice")),"No cash, no car");
  garages.Tag(state.Vehicles.Last(),CrewSlot.Ice);
  var bought=state.Vehicles.Last();var shown=garages.Retrieve(bought);Check(shown!=null&&shown.Model.Name=="sultanrs","A bought car comes out as the model that was bought");
  // The save keeps all of it.
  state.Save();var reloaded=CampaignState.Load(path);
  Check(reloaded.Garages.Contains(lot.Id)&&reloaded.Vehicles.Count==state.Vehicles.Count&&reloaded.Vehicles.Last().Owner=="Ice"&&reloaded.Vehicles[0].Garage==lot.Id&&reloaded.Vehicles[0].ModelHash==state.Vehicles[0].ModelHash&&reloaded.NextVehicleId==state.NextVehicleId,"Garages, cars, builds, tags and the id counter survive a save and load");
  // The Eclipse garage arrives with its tier.
  reloaded.Completed.Add("M27");var later=new GarageService(crew,reloaded,c.Locations,null);
  Check(later.Owned(eclipse)&&later.HomeSite(CrewSlot.Guess)==eclipse&&!later.Owned(GarageService.Sites.First(s=>s.Id=="diamond")),"After M27 the Eclipse garage is the crew's and the home menu's garage; the Diamond's waits for M47");
  later.Clear();garages.Clear();
 }
}
