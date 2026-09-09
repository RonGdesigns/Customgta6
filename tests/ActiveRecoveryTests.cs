using System;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
public static partial class RegressionTests
{
 static void ActiveRecoveryChecks()
 {
  Reset();var roster=new ActiveRecoveryRoster();var car=new Vehicle{Position=new Vector3(500,500,30)};
  var active=Game.Player.Character;active.IsDead=true;active.SetIntoVehicle(car,VehicleSeat.RightFront);
  var driver=new Ped{Health=410,Armor=55};driver.SetIntoVehicle(car,VehicleSeat.Driver);
  var other=new Ped{Position=new Vector3(-300,100,25),Health=220,Armor=13};
  roster.Peds[CrewSlot.Ice]=active;roster.Peds[CrewSlot.Gohan]=driver;roster.Peds[CrewSlot.Guess]=other;
  roster._companions.Life.Wanted.Set(CrewSlot.Gohan,3);var origin=new Vector3(10,20,30);
  Check(roster.ReviveActiveAt(origin,90)&&active.Position==origin&&!active.IsInVehicle()&&active.Health==600,"Actual roster recovery revives and relocates only the active hero");
  Check(GTA.Native.Function.SelfHandovers==0,"Active player revival never issues a redundant self-to-self player handover");
  Check(driver.Position==car.Position&&driver.CurrentVehicle==car&&car.GetPedOnSeat(VehicleSeat.Driver)==driver&&driver.Health==410&&driver.Armor==55&&driver.Task.Clears==0,"Surviving driver retains vehicle seat, location, health, armor and task");
  Check(other.Position==new Vector3(-300,100,25)&&other.Health==220&&other.Armor==13&&other.Task.Clears==0&&roster._companions.Life.Wanted.Get(CrewSlot.Gohan)==3,"Distant teammate and personal heat remain unchanged");
  other.IsDead=true;active.IsDead=true;roster.ReviveActiveAt(origin,90);
  Check(other.IsDead,"Player recovery does not resurrect another dead crew member");
  var ai=new CompanionController(new ModConfig());ai.IndependentFreeRoam=false;ai.SeparateAfterRecovery(CrewSlot.Ice);
  var mate=new Ped{Position=new Vector3(1000,0,0)};Game.Player.Character.Position=Vector3.Zero;Game.Player.Character.ForwardVector=new Vector3(0,1,0);World.Vehicles.Clear();World.NearbyVehicles=new Vehicle[0];World.CollisionReady=true;
  ai.Update(CrewSlot.Gohan,mate,Game.Player.Character);Game.GameTime+=1000;ai.Update(CrewSlot.Gohan,mate,Game.Player.Character);
  Check(mate.Position==new Vector3(1000,0,0)&&World.Vehicles.Count==0&&ai.SeparatedByRecovery(CrewSlot.Gohan),"Post-death following cannot trigger an immediate catch-up vehicle teleport");
  mate.Position=new Vector3(10,0,0);ai.Update(CrewSlot.Gohan,mate,Game.Player.Character);
  Check(!ai.SeparatedByRecovery(CrewSlot.Gohan),"Natural reunion releases the post-death teleport restriction");
  ai.SeparateAfterRecovery(CrewSlot.Ice);ai.IndependentFreeRoam=true;
  Check(!ai.SeparatedByRecovery(CrewSlot.Gohan),"A new explicit free-roam order resets separation restrictions");
 }
}
