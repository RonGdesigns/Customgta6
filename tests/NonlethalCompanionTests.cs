using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
using GTA.Native;

/// <summary>
/// The companion controller's half of a nonlethal job (M15, M50). A brother who rejoins the
/// player is fought by this controller, and from a seat its answer was a drive-by with a Micro
/// SMG it put in his hand. With StunGunOnly set, a seated brother is given nothing and one on
/// foot is still sent at the man, with the stun gun NonlethalCrew keeps in his hand.
/// </summary>
public static partial class RegressionTests
{
 static void NonlethalCompanionChecks()
 {
  Reset();var ai=new CompanionController(new ModConfig()){StunGunOnly=true};var car=new Vehicle();var driver=new Ped();var passenger=new Ped();var other=new Ped();
  driver.SetIntoVehicle(car,VehicleSeat.Driver);passenger.SetIntoVehicle(car,VehicleSeat.RightFront);other.SetIntoVehicle(car,VehicleSeat.LeftRear);Game.Player.Character=passenger;
  var watchman=new Ped{RelationshipGroup=2,CombatTarget=passenger,IsInCombat=true};World.Nearby=new[]{watchman};Game.GameTime+=1000;
  int driveBys=Function.DriveBys;
  ai.Update(CrewSlot.Ice,other,passenger);
  Check(Function.DriveBys==driveBys&&other.Task.Shots==0&&other.Task.Fights==0&&other.CurrentVehicle==car,"On a nonlethal job a seated brother under fire is given no drive-by and no mounted gun");
  Game.GameTime+=CompanionController.SeatShotRenewMs+1000;ai.Update(CrewSlot.Ice,other,passenger);
  Check(Function.DriveBys==driveBys&&other.Task.Shots==0,"and the renewal clock does not hand him one later");
  ai.StunGunOnly=false;Game.GameTime+=CompanionController.SeatShotRenewMs+1000;ai.Update(CrewSlot.Ice,other,passenger);
  Check(Function.DriveBys==driveBys+1,"With the job over, the same brother returns fire from his seat again");

  Reset();ai=new CompanionController(new ModConfig()){StunGunOnly=true};var leader=new Ped();var walker=new Ped{Position=new Vector3(3,0,0)};Game.Player.Character=leader;
  watchman=new Ped{RelationshipGroup=2,CombatTarget=leader,IsInCombat=true,Position=new Vector3(8,0,0)};World.Nearby=new[]{watchman};Game.GameTime+=1000;
  ai.Update(CrewSlot.Guess,walker,leader);
  Check(walker.Task.Fights==1&&walker.Task.LastTarget==watchman,"A brother on foot is still sent at the man, with the stun gun the mission keeps in his hand");
  ai.ReleaseAll();Check(!ai.StunGunOnly,"Releasing the whole crew clears the nonlethal flag");
  World.Nearby=new Ped[0];
 }
}
