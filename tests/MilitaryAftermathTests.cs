using System;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
using GTA.Native;

public static partial class RegressionTests
{
 static void MilitaryAftermathChecks()
 {
  Reset();World.Vehicles.Clear();var player=Game.Player.Character;player.ForwardVector=new Vector3(0,1,0);
  var response=new MilitaryResponse(new PersonalWanted());response.SetLevel(CrewSlot.Ice,6);
  Game.GameTime+=4000;response.Update(CrewSlot.Ice,true,1);var heli=World.Vehicles[0];
  Game.GameTime+=8000;response.Update(CrewSlot.Ice,true,1);var convoy=World.Vehicles[1];
  Game.GameTime+=8000;response.Update(CrewSlot.Ice,true,1);var tank=World.Vehicles[2];
  Check(heli.Indicator.Sprite==BlipSprite.PoliceHelicopter&&convoy.Indicator.Sprite==BlipSprite.PoliceCarDot&&tank.Indicator.Sprite==BlipSprite.Tank,"Military vehicles have distinct entity-attached pursuit icons");
  Check(!tank.Indicator.IsFriendly&&!tank.Indicator.IsShortRange&&tank.Indicator.Name=="Military tank","Military map markers identify hostile units on map and radar");
  Game.GameTime=21100;response.Update(CrewSlot.Ice,true,1);var color=tank.Indicator.Color;
  Game.GameTime+=500;response.Update(CrewSlot.Ice,true,1);Check(color!=tank.Indicator.Color,"Pursuit markers alternate red and blue without allocating replacement blips");
  Func<object,int> group=o=>o is RelationshipGroup?((RelationshipGroup)o).Hash:(int)o;
  Check(Function.Relations.Exists(a=>(int)a[0]==0&&group(a[1])==1&&group(a[2])==Game.GenerateHash("COP"))&&Function.Relations.Exists(a=>(int)a[0]==0&&group(a[2])==1&&group(a[1])==Game.GenerateHash("COP")),"Custom military and native police are allies in both directions");
  heli.IsDead=true;var pilot=heli.GetPedOnSeat(VehicleSeat.Driver);pilot.IsDead=true;
  Game.GameTime+=100;response.Update(CrewSlot.Ice,true,1);
  Check(heli.Present&&pilot.Present&&!heli.Released&&!pilot.Released&&!heli.Indicator.Present,"Destroyed helicopter and pilot remain physically present while their pursuit marker disappears");
  int count=World.Vehicles.Count;Game.GameTime+=3999;response.Update(CrewSlot.Ice,true,1);Check(World.Vehicles.Count==count,"A destroyed unit does not bypass the replacement cooldown");
  Game.GameTime+=4001;response.Update(CrewSlot.Ice,true,1);Check(World.Vehicles.Count==count+1&&heli.Present,"Reinforcement can arrive without deleting the old wreck");
  convoy.Speed=10;tank.Speed=10;heli.IsOnScreen=true;
  Game.GameTime+=60000;response.Update(CrewSlot.Ice,true,1);Check(heli.Present&&!heli.Released,"Visible wreck stays owned beyond the initial sixty-second aftermath window");
  heli.IsOnScreen=false;heli.Position=new Vector3(1000,1000,0);Game.GameTime+=100;response.Update(CrewSlot.Ice,true,1);
  Check(heli.Present&&heli.Released&&pilot.Present&&pilot.Released,"Distant expired wreck and body are released to engine cleanup rather than deleted");
  var driver=convoy.GetPedOnSeat(VehicleSeat.Driver);driver.IsDead=true;Game.GameTime+=100;response.Update(CrewSlot.Ice,true,1);
  Check(convoy.Present&&!convoy.IsDead&&!convoy.Released&&driver.Present,"Killing only the driver does not destroy or delete an intact vehicle");
  response.Clear();Check(convoy.Present&&convoy.Released&&!convoy.Indicator.Present,"Ending pursuit releases aftermath and removes its blip without deleting the vehicle");
  Reset();World.Vehicles.Clear();player=Game.Player.Character;player.ForwardVector=new Vector3(0,1,0);response=new MilitaryResponse(new PersonalWanted());response.SetLevel(CrewSlot.Ice,6);
  Game.GameTime+=4000;response.Update(CrewSlot.Ice,true,1);var first=World.Vehicles[0];
  for(int i=0;i<7;i++) {var current=World.Vehicles[World.Vehicles.Count-1];current.IsDead=true;Game.GameTime+=100;response.Update(CrewSlot.Ice,true,1);Game.GameTime+=8000;response.Update(CrewSlot.Ice,true,1);}
  Check(first.Present&&first.Released&&World.Vehicles.FindAll(v=>v.Present&&v.IsPersistent).Count<=7,"Repeated destruction bounds retained aftermath without forcibly deleting old wrecks");response.Clear();
 }
 static void TeamCombatChecks()
 {
  OverflowTravelChecks();
  CompanionRecoveryChecks();
  Reset();var ai=new CompanionController(new ModConfig());var leader=Game.Player.Character;leader.RelationshipGroup=1;
  var ice=new Ped{RelationshipGroup=1};var gohan=new Ped{RelationshipGroup=1};
  ai.IsCrewMember=p=>p==ice||p==gohan||p==leader;
  var cop=new Ped{RelationshipGroup=2,CombatTarget=gohan,IsInCombat=true};World.Nearby=new[]{cop};
  ai.IndependentFreeRoam=false;ai.Update(CrewSlot.Ice,ice,leader);ai.Update(CrewSlot.Gohan,gohan,leader);
  Check(ice.Task.LastTarget==cop&&gohan.Task.LastTarget==cop,"Both followers defend a brother who is targeted, even when the leader is not the victim");
  var second=new Ped{RelationshipGroup=2,CombatTarget=leader};cop.IsDead=true;World.Nearby=new[]{second};
  Game.GameTime+=1100;ai.Update(CrewSlot.Ice,ice,leader);Check(ice.Task.LastTarget==second&&ice.Task.Fights==2,"An old combat flag cannot prevent retargeting after an enemy dies");
  Game.GameTime+=100;ai.Update(CrewSlot.Ice,ice,leader);Check(ice.Task.Fights==2,"Valid combat tasks are not restarted every frame");
  Reset();ai=new CompanionController(new ModConfig());ai.IndependentFreeRoam=false;leader=Game.Player.Character;leader.RelationshipGroup=1;leader.IsShooting=true;Game.Player.WantedLevel=2;
  ice=new Ped{RelationshipGroup=1};cop=new Ped{RelationshipGroup=Game.GenerateHash("COP")};World.Nearby=new[]{cop};
  ai.Update(CrewSlot.Ice,ice,leader);Check(ice.Task.LastTarget==cop,"Following companion recognizes an ongoing wanted gunfight with nearby police without requiring Hate relationships");
  Reset();ai=new CompanionController(new ModConfig());ai.IndependentFreeRoam=false;leader=Game.Player.Character;leader.RelationshipGroup=1;leader.IsShooting=true;ice=new Ped{RelationshipGroup=1};
  cop=new Ped{RelationshipGroup=Game.GenerateHash("COP")};World.Nearby=new[]{cop};ai.Update(CrewSlot.Ice,ice,leader);Check(ice.Task.Fights==0,"Police presence alone without wanted heat or a crew attack is not a threat");
  var car=new Vehicle();leader.SetIntoVehicle(car,VehicleSeat.Driver);var attacker=new Ped{RelationshipGroup=2,CombatTarget=leader};World.Nearby=new[]{attacker};Game.GameTime+=501;
  ai.Update(CrewSlot.Ice,ice,leader);Check(ai.StateOf(CrewSlot.Ice)==CompanionState.Combat&&ice.Task.Enters==0,"On-foot follower responds to an attacker before idle boarding attempts");
  ai.TakeControl(CrewSlot.Ice);Game.GameTime+=501;int fights=ice.Task.Fights;ai.Update(CrewSlot.Ice,ice,leader);Check(ice.Task.Fights==fights&&ai.StateOf(CrewSlot.Ice)==CompanionState.Scripted,"Team combat changes retain explicit mission ownership");
 }
 static void CompanionRecoveryChecks()
 {
  Reset();var recovery=new CompanionRecovery();var player=Game.Player.Character;player.ForwardVector=new Vector3(0,1,0);
  var downed=new Ped{IsDead=true};Vector3 point;
  Check(!recovery.TryGetDestination(CrewSlot.Ice,downed,player,true,out point),"Downed companion starts their own recovery timer without reviving immediately");
  Game.GameTime+=44999;Check(!recovery.TryGetDestination(CrewSlot.Ice,downed,player,true,out point),"Companion recovery waits forty-five seconds");
  Game.GameTime++;Check(!recovery.TryGetDestination(CrewSlot.Ice,downed,player,false,out point),"Mission ownership or disabled companion respawn prevents automatic revival");
  World.SphereVisible=true;Check(!recovery.TryGetDestination(CrewSlot.Ice,downed,player,true,out point),"Companion recovery cannot spawn a new body in the player's view");
  World.SphereVisible=false;Game.GameTime+=3000;
  Check(recovery.TryGetDestination(CrewSlot.Ice,downed,player,true,out point)&&point.DistanceTo(player.Position)>=100,"Expired recovery finds an off-camera ground location away from the player");
  recovery.Forget(CrewSlot.Ice);Check(!recovery.TryGetDestination(CrewSlot.Ice,downed,player,true,out point),"A later death receives a fresh recovery delay");
  var ai=new CompanionController(new ModConfig());ai.SeparateAfterRecovery(CrewSlot.Guess);Game.GameTime+=45000;
  Check(recovery.TryGetDestination(CrewSlot.Ice,downed,player,true,out point)&&ai.SeparatedByRecovery(CrewSlot.Ice),"Player-death separation no longer blocks the teammate's independent recovery timer");
  Game.GameTime+=3000;Check(recovery.TryGetDestination(CrewSlot.Ice,null,player,true,out point),"Engine cleanup of the dead body cannot strand the hero outside the roster forever");
  var hero=new Ped();CrewDurability.RestoreAfterSwitch(hero,900,100);
  Check(hero.MaxHealth==900&&hero.Health==900&&hero.Armor==100&&!hero.CanSufferCriticalHits,"Player and NPC heroes share nine-hundred health, full armor and critical-hit protection");
 }
 static void OverflowTravelChecks()
 {
  Reset();var leader=Game.Player.Character;var coupe=new Vehicle{Capacity=1};leader.SetIntoVehicle(coupe,VehicleSeat.Driver);
  var first=new Ped();var second=new Ped();var spare=new Vehicle{Position=new Vector3(15,0,0)};World.NearbyVehicles=new[]{spare};
  var ai=new CompanionController(new ModConfig()){IndependentFreeRoam=false};
  ai.IsCrewMember=p=>p==leader||p==first||p==second;
  ai.Update(CrewSlot.Ice,first,leader);ai.Update(CrewSlot.Gohan,second,leader);
  Check(first.Task.Enters==1&&ai.StateOf(CrewSlot.Gohan)==CompanionState.Convoy&&second.Task.Enters==1&&spare.IsPersistent,"A two-seat car reserves its passenger seat once and immediately finds overflow transport for the other brother");
  ai.Driver.IsRendezvous=v=>!leader.IsInVehicle(v);ai.Driver.FollowDestination=v=>leader.Position;
  second.SetIntoVehicle(spare,VehicleSeat.Driver);Game.GameTime+=1000;ai.Update(CrewSlot.Gohan,second,leader);
  Check(Function.Follows.Count==1&&Function.Follows[0][2]==coupe&&second.CurrentVehicle==spare,"Overflow driver follows the actual player vehicle instead of cruising with no destination");
  spare.Position=coupe.Position+new Vector3(0,-14,0);Game.GameTime+=1000;ai.Update(CrewSlot.Gohan,second,leader);
  Check(second.CurrentVehicle==spare&&Function.Follows.Count==1,"Catching up to a moving party preserves the follow task and driver's seat");
  Check(CrewDriving.Speed(CrewSlot.Guess,false)>CrewDriving.Speed(CrewSlot.Ice,false)&&CrewDriving.Speed(CrewSlot.Ice,false)>CrewDriving.Speed(CrewSlot.Gohan,false)&&CrewDriving.Speed(CrewSlot.Gohan,false)>35,"Guess is fastest; Ice and Gohan have distinct faster-than-before cruising profiles");
  Check(CrewDriving.Speed(CrewSlot.Guess,true)>CrewDriving.Speed(CrewSlot.Guess,false),"Pursuit adds urgency to the professional driver's cruise profile");
  Reset();leader=Game.Player.Character;leader.SetIntoVehicle(new Vehicle(),VehicleSeat.Driver);var mate=new Ped();ai=new CompanionController(new ModConfig()){IndependentFreeRoam=false,RideAlong=false};spare=new Vehicle{Position=new Vector3(12,0,0)};World.NearbyVehicles=new[]{spare};ai.Update(CrewSlot.Ice,mate,leader);
  Check(ai.StateOf(CrewSlot.Ice)==CompanionState.Convoy&&mate.Task.LastSeat==VehicleSeat.Driver,"Drive Alongside chooses a separate vehicle even when the leader has spare seats");
  Reset();leader=Game.Player.Character;mate=new Ped();var traffic=new Vehicle{Position=new Vector3(12,0,0),Speed=5};var occupant=new Ped();occupant.SetIntoVehicle(traffic,VehicleSeat.Driver);World.NearbyVehicles=new[]{traffic};var convoy=new CompanionConvoy();var driver=new CompanionDriver();
  convoy.Update(CrewSlot.Ice,mate,leader,driver);
  Check(Function.TrafficShots.Count==1&&Function.TrafficShots[0][1]==occupant&&mate.Task.Enters==0,"Overflow transport can stop a nearby moving car with a bounded burst at its non-crew driver");
  Game.GameTime+=1000;convoy.Update(CrewSlot.Ice,mate,leader,driver);Check(Function.TrafficShots.Count==1,"Traffic interception does not issue a shooting task every update");
  occupant.IsDead=true;Game.GameTime+=1000;convoy.Update(CrewSlot.Ice,mate,leader,driver);Check(mate.Task.Enters==1&&mate.Task.LastSeat==VehicleSeat.Driver,"Once the driver is down, the companion switches to normal driver-seat entry");convoy.Clear();
  Reset();leader=Game.Player.Character;mate=new Ped();traffic=new Vehicle{Position=new Vector3(12,0,0),Speed=5};occupant=new Ped();occupant.SetIntoVehicle(traffic,VehicleSeat.Driver);World.NearbyVehicles=new[]{traffic};convoy=new CompanionConvoy{IsCrewMember=p=>p==occupant};convoy.Update(CrewSlot.Ice,mate,leader,new CompanionDriver());
  Check(Function.TrafficShots.Count==0&&mate.Task.Enters==0,"Crew-occupied vehicles are never stolen or shot to obtain transport");World.NearbyVehicles=new Vehicle[0];
 }
}
