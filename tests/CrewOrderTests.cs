using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
public static partial class RegressionTests
{
 /// <summary>
 /// Ron, September 17: in a fight he got into the driver's seat of a truck and both
 /// brothers took the cab seats, never the gun; he switched, took the gun seat, and the
 /// brother at the wheel got out. Two rules and no way to give an order. This covers the
 /// seat picker, the driver holding the wheel through a seat change, and the orders.
 /// </summary>
 static void CrewOrderChecks()
 {
  // ---- The seat picker: turret before a plain seat, never the lowest index first.
  Reset();var ai=new CompanionController(new ModConfig());
  var leader=new Ped{RelationshipGroup=1};var ice=new Ped{RelationshipGroup=1,Position=new Vector3(4,0,0)};var gohan=new Ped{RelationshipGroup=1,Position=new Vector3(6,0,0)};
  var truck=new Vehicle{Capacity=3};truck.TurretSeats.Add(VehicleSeat.RightRear);
  leader.SetIntoVehicle(truck,VehicleSeat.Driver);Game.Player.Character=leader;
  ai.SetHangout(CrewSlot.Ice,true);ai.SetHangout(CrewSlot.Gohan,true);
  ai.Update(CrewSlot.Ice,ice,leader);
  Check(ai.StateOf(CrewSlot.Ice)==CompanionState.Vehicle&&ice.Task.LastSeat==VehicleSeat.RightRear,"The first brother to board a gun truck takes the turret, not the passenger seat beside the driver");
  ai.Update(CrewSlot.Gohan,gohan,leader);
  Check(gohan.Task.LastSeat==VehicleSeat.Passenger,"and the second takes the cab seat the turret's reservation leaves him");
  Check(CrewOrders.FreeTurretSeat(truck)==VehicleSeat.RightRear&&CrewOrders.IsTurretSeat(truck,VehicleSeat.RightRear)&&!CrewOrders.IsTurretSeat(truck,VehicleSeat.Passenger),"The turret is read from the vehicle, not assumed from a seat index");

  // ---- The player settled in a passenger seat wants a driver.
  Reset();ai=new CompanionController(new ModConfig());
  leader=new Ped{RelationshipGroup=1};ice=new Ped{RelationshipGroup=1,Position=new Vector3(4,0,0)};
  var car=new Vehicle{Capacity=3};leader.SetIntoVehicle(car,VehicleSeat.Passenger);Game.Player.Character=leader;
  ai.SetHangout(CrewSlot.Ice,true);ai.HoldPosition=true;ai.Update(CrewSlot.Ice,ice,leader);
  Game.GameTime+=CompanionController.WantsDriverMs+100;ai.HoldPosition=false;ai.Refresh(CrewSlot.Ice);ai.Update(CrewSlot.Ice,ice,leader);
  Check(ai.StateOf(CrewSlot.Ice)==CompanionState.Vehicle&&ice.Task.LastSeat==VehicleSeat.Driver,"With the player riding in a seat that is not the driver's, a brother takes the wheel");
  Reset();ai=new CompanionController(new ModConfig());
  leader=new Ped{RelationshipGroup=1};ice=new Ped{RelationshipGroup=1,Position=new Vector3(4,0,0)};
  car=new Vehicle{Capacity=3};leader.SetIntoVehicle(car,VehicleSeat.Passenger);Game.Player.Character=leader;
  ai.SetHangout(CrewSlot.Ice,true);ai.Update(CrewSlot.Ice,ice,leader);
  Check(ice.Task.LastSeat!=VehicleSeat.Driver,"but not in the first second and a half, which is the engine shuffling him across from the passenger door");

  // ---- The driver holds the wheel while the player is out beside the truck.
  Reset();ai=new CompanionController(new ModConfig());
  leader=new Ped{RelationshipGroup=1,Position=new Vector3(2,0,0)};ice=new Ped{RelationshipGroup=1};gohan=new Ped{RelationshipGroup=1};
  truck=new Vehicle{Capacity=3,Position=new Vector3(0,0,0)};truck.TurretSeats.Add(VehicleSeat.RightRear);
  ice.SetIntoVehicle(truck,VehicleSeat.Driver);gohan.SetIntoVehicle(truck,VehicleSeat.Passenger);leader.SetIntoVehicle(truck,VehicleSeat.RightRear);Game.Player.Character=leader;
  ai.SetHangout(CrewSlot.Ice,true);ai.SetHangout(CrewSlot.Gohan,true);
  ai.Update(CrewSlot.Ice,ice,leader);ai.Update(CrewSlot.Gohan,gohan,leader);
  Check(ai.StateOf(CrewSlot.Ice)==CompanionState.Driving&&ai.StateOf(CrewSlot.Gohan)==CompanionState.Vehicle,"Riding with a brother at the wheel: he drives and the other sits");
  truck.Seats.Remove(VehicleSeat.RightRear);leader.CurrentVehicle=null;leader.Position=new Vector3(3,0,0);
  Game.GameTime+=500;ai.Update(CrewSlot.Ice,ice,leader);ai.Update(CrewSlot.Gohan,gohan,leader);
  Check(ice.CurrentVehicle==truck&&ai.StateOf(CrewSlot.Ice)==CompanionState.Driving,"The player stepping out beside the truck does not put the driver on the sidewalk");
  Check(gohan.CurrentVehicle==truck&&ai.StateOf(CrewSlot.Gohan)==CompanionState.Vehicle,"and the passenger stays in his seat too");
  Check(ai.Driver.HoldStill(CrewSlot.Ice,truck),"and the truck is held still while he is out beside it");
  Game.GameTime+=CompanionController.SeatShuffleMs+500;ai.Update(CrewSlot.Ice,ice,leader);
  Check(ai.StateOf(CrewSlot.Ice)==CompanionState.Disembarking,"Still on foot beside it after the grace, the driver gets out as he always did");

  // ---- Orders.
  Reset();ai=new CompanionController(new ModConfig());
  leader=new Ped{RelationshipGroup=1,Position=new Vector3(3,0,0)};ice=new Ped{RelationshipGroup=1,Position=new Vector3(5,0,0)};
  truck=new Vehicle{Capacity=3};truck.TurretSeats.Add(VehicleSeat.RightRear);Game.Player.Character=leader;Game.Player.LastVehicle=truck;
  Check(CrewOrders.Subject(leader)==truck,"The truck the player just got out of is still the truck an order is about");
  var offered=CrewOrders.Available(ice,leader,truck,false);
  Check(offered.Contains(CrewOrder.TakeTheWheel)&&offered.Contains(CrewOrder.ManTheGun)&&offered.Contains(CrewOrder.GetIn)&&!offered.Contains(CrewOrder.GetOut)&&!offered.Contains(CrewOrder.DriveToWaypoint),"Beside an empty gun truck he can be told to take the wheel, man the gun or get in; there is no waypoint to drive to");
  Check(ai.Order(CrewSlot.Ice,CrewOrder.TakeTheWheel,truck)&&ai.OrderOf(CrewSlot.Ice)==CrewOrder.TakeTheWheel&&ai.IsHangingOut(CrewSlot.Ice),"An order is taken, and it is an invitation too");
  ai.Update(CrewSlot.Ice,ice,leader);
  Check(ai.StateOf(CrewSlot.Ice)==CompanionState.Vehicle&&ice.Task.LastSeat==VehicleSeat.Driver,"Told to take the wheel, he walks to the driver's door and no other");
  ice.SetIntoVehicle(truck,VehicleSeat.Driver);ai.Update(CrewSlot.Ice,ice,leader);
  Check(ai.StateOf(CrewSlot.Ice)==CompanionState.Driving&&ai.Driver.HoldStill(CrewSlot.Ice,truck),"At the wheel with the player still outside he holds the truck still");
  leader.SetIntoVehicle(truck,VehicleSeat.RightRear);Game.GameTime+=CompanionController.SeatShuffleMs+1000;ai.Update(CrewSlot.Ice,ice,leader);
  Check(ai.StateOf(CrewSlot.Ice)==CompanionState.Driving&&!ai.Driver.HoldStill(CrewSlot.Ice,truck),"With the player in the gun seat he drives");
  truck.Seats.Remove(VehicleSeat.RightRear);leader.CurrentVehicle=null;leader.Position=new Vector3(30,0,0);Game.GameTime+=CompanionController.SeatShuffleMs+1000;ai.Update(CrewSlot.Ice,ice,leader);
  Check(ai.StateOf(CrewSlot.Ice)==CompanionState.Driving&&ice.CurrentVehicle==truck,"and an ordered driver keeps the wheel however far the player walks, until told otherwise");
  Check(ai.Order(CrewSlot.Ice,CrewOrder.PullOver,truck)&&ai.Driver.HoldStill(CrewSlot.Ice,truck),"Pull over stops him at the wheel");
  Check(ai.Order(CrewSlot.Ice,CrewOrder.GetOut,truck),"Get out is taken");truck.Speed=15;ai.Update(CrewSlot.Ice,ice,leader);
  Check(ice.CurrentVehicle==truck,"but nobody leaves a moving vehicle");
  truck.Speed=0;ai.Update(CrewSlot.Ice,ice,leader);
  Check(ai.StateOf(CrewSlot.Ice)==CompanionState.Disembarking,"Stopped, he gets out");
  ai.Update(CrewSlot.Ice,ice,leader);
  Check(ai.OrderOf(CrewSlot.Ice)==CrewOrder.HoldHere&&ai.StateOf(CrewSlot.Ice)==CompanionState.Hold,"and holds where he got out rather than chasing the player on foot");
  leader.Position=new Vector3(400,0,0);ai.Update(CrewSlot.Ice,ice,leader);
  Check(ai.StateOf(CrewSlot.Ice)==CompanionState.Hold,"A brother told to hold is not leashed back to a player four hundred meters away");

  Check(ai.Order(CrewSlot.Ice,CrewOrder.ManTheGun,truck),"Man the gun is taken");ai.Update(CrewSlot.Ice,ice,leader);
  Check(ai.StateOf(CrewSlot.Ice)==CompanionState.Vehicle&&ice.Task.LastSeat==VehicleSeat.RightRear,"and sends him to the turret and nowhere else");
  Check(ai.Order(CrewSlot.Ice,CrewOrder.FollowMe,null)&&ai.OrderOf(CrewSlot.Ice)==CrewOrder.None&&ai.IsHangingOut(CrewSlot.Ice),"Follow me clears the standing order and keeps him with you");
  Check(ai.Order(CrewSlot.Ice,CrewOrder.OwnThing,null)&&!ai.IsHangingOut(CrewSlot.Ice),"Do your own thing sends him off");
  Check(!ai.Order(CrewSlot.Ice,CrewOrder.GetIn,null),"A vehicle order with no vehicle is refused");
  ai.Order(CrewSlot.Ice,CrewOrder.HoldHere,null);ai.MissionActive=true;
  Check(ai.OrderOf(CrewSlot.Ice)==CrewOrder.None&&!ai.Order(CrewSlot.Ice,CrewOrder.HoldHere,null),"A mission clears standing orders and takes no new ones");
  ai.MissionActive=false;ai.TakeControl(CrewSlot.Ice);
  Check(!ai.Order(CrewSlot.Ice,CrewOrder.HoldHere,null),"nor does a script that owns him");ai.ReleaseAll();
  Check(ai.Order(CrewSlot.Ice,CrewOrder.HoldHere,null)&&ai.HasOrders,"Released, he takes orders again");
  ai.RideAlong=false;Check(!ai.HasOrders,"and a group travel request from the phone resets them, the way it resets the individual choices");
  // Phone choices use these same public entry points after a quick order.
  ai.Order(CrewSlot.Ice,CrewOrder.HoldHere,null);
  ai.Order(CrewSlot.Gohan,CrewOrder.HoldHere,null);
  Check(ai.SetHangout(CrewSlot.Ice,false),"A phone dismissal replaces a standing quick order");
  ai.Update(CrewSlot.Ice,ice,leader);
  Check(ai.OrderOf(CrewSlot.Ice)==CrewOrder.None&&!ai.IsHangingOut(CrewSlot.Ice)&&ai.StateOf(CrewSlot.Ice)!=CompanionState.Hold,
   "Dismissed brother resumes his own behavior instead of holding forever");
  Check(ai.OrderOf(CrewSlot.Gohan)==CrewOrder.HoldHere,"Changing one brother leaves the other brother's order alone");
  ai.Order(CrewSlot.Ice,CrewOrder.HoldHere,null);
  Check(ai.SetTravelChoice(CrewSlot.Ice,false)&&ai.OrderOf(CrewSlot.Ice)==CrewOrder.None&&!ai.RidesAlong(CrewSlot.Ice),
   "Drive alongside replaces a quick hold with the selected travel preference");
  ai.Order(CrewSlot.Ice,CrewOrder.HoldHere,null);
  ai.HoldPosition=true;
  Check(!ai.SetHangout(CrewSlot.Ice,false)&&ai.OrderOf(CrewSlot.Ice)==CrewOrder.HoldHere,
   "A refused phone change cannot clear a mission-held order");
  ai.HoldPosition=false;

 }
}
