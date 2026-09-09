using System;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
using GTA.Native;

public static partial class RegressionTests
{
 static void ReunionAndChaseTests()
 {
  Reset();var ai=new CompanionController(new ModConfig());var car=new Vehicle();var driver=new Ped();var passenger=new Ped();var other=new Ped();
  driver.SetIntoVehicle(car,VehicleSeat.Driver);passenger.SetIntoVehicle(car,VehicleSeat.RightFront);other.SetIntoVehicle(car,VehicleSeat.LeftRear);Game.Player.Character=passenger;
  ai.Update(CrewSlot.Guess,driver,passenger);ai.Update(CrewSlot.Ice,other,passenger);
  Check(ai.StateOf(CrewSlot.Guess)==CompanionState.Driving&&driver.CurrentVehicle==car,"Independent mode preserves the driver when player takes a passenger seat");
  Check(ai.StateOf(CrewSlot.Ice)==CompanionState.Vehicle&&other.CurrentVehicle==car,"Other passenger stays in the shared vehicle during handover");
  Check(driver.Task.DriveSpeed==50,"Guess uses the fastest fifty-metre-per-second cruise profile");
  Game.Player.WantedLevel=2;Game.GameTime+=1000;ai.Update(CrewSlot.Guess,driver,passenger);
  Check(driver.Task.DriveSpeed==60&&driver.Task.Shots==0,"Wanted shared car triggers escape pace without making driver shoot");
  var attacker=new Ped{RelationshipGroup=2,CombatTarget=passenger,IsInCombat=true};World.Nearby=new[]{attacker};Game.GameTime+=1000;
  ai.Update(CrewSlot.Ice,other,passenger);Check(other.Task.Shots>0&&other.CurrentVehicle==car,"Passenger returns fire with a drive-by task and retains their seat");
  ai.Update(CrewSlot.Guess,driver,passenger);Check(driver.Task.Shots==0&&driver.CurrentVehicle==car,"Driver remains driving through the same attack");
  World.Nearby=new Ped[0];Game.Player.WantedLevel=0;var leader=new Ped{Position=new Vector3(400,0,0)};Game.Player.Character=leader;
  ai.IndependentFreeRoam=false;ai.Driver.IsRendezvous=v=>true;ai.Driver.FollowDestination=v=>leader.Position;
  ai.Update(CrewSlot.Guess,driver,leader);Check(driver.Task.DriveTarget==leader.Position&&driver.CurrentVehicle==car,"Independent-to-follow reuses the existing car and routes to an on-foot player");
  driver.Position=leader.Position+new Vector3(10,0,0);car.Position=driver.Position;car.Speed=0;Game.GameTime+=1000;ai.Update(CrewSlot.Guess,driver,leader);
  Check(driver.CurrentVehicle==null,"Arrived road driver gets out to complete an on-foot reunion");
  var heli=new Vehicle{Model=new Model{IsCar=false,IsHelicopter=true},IsInAir=true,HeightAboveGround=100,Position=new Vector3(0,0,100)};driver.SetIntoVehicle(heli,VehicleSeat.Driver);
  ai.Refresh(CrewSlot.Guess);Game.GameTime+=20000;ai.Update(CrewSlot.Guess,driver,leader);
  Check(driver.Task.DriveKind=="heli"&&driver.CurrentVehicle==heli,"Helicopter reunion uses an aircraft landing task without ejecting its pilot");
  var heat=new PersonalWanted();heat.Set(CrewSlot.Ice,6);heat.Capture(CrewSlot.Ice,5);Check(heat.Get(CrewSlot.Ice)==6,"Native five-star capture preserves the scripted sixth tier");
  heat.Capture(CrewSlot.Ice,0);Check(heat.Get(CrewSlot.Ice)==0,"Actually losing police heat clears the custom tier");
  Game.Player.Character.ForwardVector=new Vector3(0,1,0);var response=new MilitaryResponse(heat);World.Vehicles.Clear();Game.Player.WantedLevel=5;Game.GameTime=100;response.Update(CrewSlot.Ice,true,1);
  Game.GameTime=90099;response.Update(CrewSlot.Ice,true,1);Check(heat.Get(CrewSlot.Ice)==0,"Military escalation waits for ninety continuous seconds at five stars");
  Game.GameTime=90100;response.Update(CrewSlot.Ice,true,1);Check(heat.Get(CrewSlot.Ice)==6&&World.Vehicles.Count==0,"Sustained five stars escalates without an instant spawn");
  Game.GameTime+=10000;response.Update(CrewSlot.Ice,true,1);Game.GameTime+=20000;response.Update(CrewSlot.Ice,true,1);Game.GameTime+=20000;response.Update(CrewSlot.Ice,true,1);Game.GameTime+=4001;response.Update(CrewSlot.Ice,true,1);
  Check(World.Vehicles.Count==3,"Military response caps forces at one tank, one helicopter and one convoy");
  var tank=World.Vehicles[2];var pilot=tank.GetPedOnSeat(VehicleSeat.Driver);
  Check(pilot.Task.VehicleMissions>0,"Tank uses its dedicated vehicle attack mission after the seating delay");
  response.SetLevel(CrewSlot.Ice,5);Check(heat.Get(CrewSlot.Ice)==5&&Game.Player.WantedLevel==5,"Lowering six to five actually clears the custom tier");
  World.Vehicles.Clear();Game.GameTime=100000;response.Update(CrewSlot.Ice,true,1);
  Game.GameTime=145000;response.Update(CrewSlot.Ice,true,1);
  Game.GameTime=175000;response.Update(CrewSlot.Ice,true,1,true);
  Game.GameTime=219999;response.Update(CrewSlot.Ice,true,1);Check(heat.Get(CrewSlot.Ice)==5,"Opening the menu pauses rather than resets or advances escalation");
  Game.GameTime=220000;response.Update(CrewSlot.Ice,true,1);Check(heat.Get(CrewSlot.Ice)==6,"Five-star progression resumes after the menu closes");
  response.SetLevel(CrewSlot.Ice,0);Check(response.Level(CrewSlot.Ice)==0&&World.Vehicles.TrueForAll(v=>!v.Present||v.Released),"Clearing the wanted level stands down military units and clears personal heat");
  response.Update(CrewSlot.Guess,false,1);Check(World.Vehicles.TrueForAll(v=>!v.Present||v.Released),"Mission or cleanup releases all scripted military units");
  World.Vehicles.Clear();World.Nearby=new Ped[0];World.NearbyVehicles=new Vehicle[0];
 }
}
