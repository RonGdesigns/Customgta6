using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
public static partial class RegressionTests
{
 static void IndividualHangoutChecks()
 {
  Reset();var ai=new CompanionController(new ModConfig());
  var leader=new Ped{RelationshipGroup=1};var ice=new Ped{RelationshipGroup=1,Position=new Vector3(10,0,0)};var gohan=new Ped{RelationshipGroup=1,Position=new Vector3(20,0,0)};
  Check(ai.SetHangout(CrewSlot.Ice,true)&&ai.IsHangingOut(CrewSlot.Ice)&&!ai.IsHangingOut(CrewSlot.Gohan),"An Ice invite changes only Ice's free-roam choice");
  ai.Update(CrewSlot.Ice,ice,leader);ai.Update(CrewSlot.Gohan,gohan,leader);
  Check(ai.StateOf(CrewSlot.Ice)==CompanionState.Follow&&ai.StateOf(CrewSlot.Gohan)==CompanionState.Independent,"Invited brother follows while the other keeps his independent activity");
  ai.SetHangout(CrewSlot.Gohan,true);ai.SetHangout(CrewSlot.Ice,false);
  Check(!ai.IsHangingOut(CrewSlot.Ice)&&ai.IsHangingOut(CrewSlot.Gohan),"Ending one hangout leaves the other brother invited");
  ai.IndependentFreeRoam=true;
  Check(!ai.HasIndividualOrders&&!ai.IsHangingOut(CrewSlot.Gohan),"An unchanged global independent order still resets individual invitations");
  ai.IndependentFreeRoam=false;ai.SetHangout(CrewSlot.Ice,false);ai.IndependentFreeRoam=false;
  Check(ai.IsHangingOut(CrewSlot.Ice)&&ai.IsHangingOut(CrewSlot.Gohan)&&!ai.HasIndividualOrders,"A group travel order reunites both brothers after individual dismissal");
  ai.MissionActive=true;Check(!ai.SetHangout(CrewSlot.Ice,false)&&ai.IsHangingOut(CrewSlot.Ice),"Phone hangouts cannot override an active mission");
  ai.MissionActive=false;ai.TakeControl(CrewSlot.Ice);Check(!ai.SetHangout(CrewSlot.Ice,false),"A scripted actor cannot be released by a hangout request");ai.ReleaseAll();
  var car=new Vehicle{Speed=20};leader.CurrentVehicle=car;ice.CurrentVehicle=car;car.Seats[VehicleSeat.Driver]=leader;car.Seats[VehicleSeat.Passenger]=ice;
  ai.SetHangout(CrewSlot.Ice,false);ai.Update(CrewSlot.Ice,ice,leader);
  Check(ice.CurrentVehicle==car&&ai.StateOf(CrewSlot.Ice)==CompanionState.Vehicle,"Dismissed passenger stays seated in a moving car");
  car.Speed=0;car.IsInAir=true;ai.Update(CrewSlot.Ice,ice,leader);
  Check(ice.CurrentVehicle==car,"Dismissed passenger never jumps out while airborne");
  car.IsInAir=false;ai.Update(CrewSlot.Ice,ice,leader);
  Check(ice.CurrentVehicle==null&&ai.StateOf(CrewSlot.Ice)==CompanionState.Disembarking,"Dismissed passenger exits once the shared car safely stops");
  ai.Update(CrewSlot.Ice,ice,leader);Check(ai.StateOf(CrewSlot.Ice)==CompanionState.Independent,"After disembarking the dismissed brother resumes his own activities");
  ai.SetTravelChoice(CrewSlot.Ice,true);ai.SetTravelChoice(CrewSlot.Gohan,false);
  Check(ai.RidesAlong(CrewSlot.Ice)&&!ai.RidesAlong(CrewSlot.Gohan)&&ai.IsHangingOut(CrewSlot.Gohan),"Per-brother travel choices invite only that brother and remain independent");
  car.Seats.Remove(VehicleSeat.Passenger);ai.Update(CrewSlot.Ice,ice,leader);ai.Update(CrewSlot.Gohan,gohan,leader);
  Check(ai.StateOf(CrewSlot.Ice)==CompanionState.Vehicle&&ai.StateOf(CrewSlot.Gohan)==CompanionState.Convoy,"One brother boards the player's car while the other acquires convoy transport");
  ice.CurrentVehicle=car;car.Seats[VehicleSeat.Passenger]=ice;car.Speed=20;ai.SetTravelChoice(CrewSlot.Ice,false);ai.Update(CrewSlot.Ice,ice,leader);
  Check(ice.CurrentVehicle==car,"Changing to drive-along never ejects a moving passenger");
  car.Speed=0;ai.Update(CrewSlot.Ice,ice,leader);Check(ice.CurrentVehicle==null,"Changing to drive-along disembarks the passenger after a safe stop");
  ai.MissionActive=true;Check(!ai.SetTravelChoice(CrewSlot.Gohan,true)&&!ai.RidesAlong(CrewSlot.Gohan),"Individual travel settings cannot override mission transport");
  ai.MissionActive=false;ai.RideAlong=true;Check(ai.RidesAlong(CrewSlot.Ice)&&ai.RidesAlong(CrewSlot.Gohan),"Global ride-along order deliberately resets both individual travel choices");
 }
}
