using System;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
using GTA.Native;
public static partial class RegressionTests
{
 /// <summary>
 /// Ron, September 18: "make them drive a little bit faster and a little bit better and
 /// with a little bit more intent... if I tell them take the wheel they should just drive
 /// when police is on them and you're not the driver." The speed comes from the car, the
 /// style crosses medians when it matters, and a getaway is the engine's own flee mission
 /// pointed at the nearest officer rather than a point ahead on the road.
 /// </summary>
 static void CrewDrivingChecks()
 {
  // ---- The commanded speed is the car's, floored at the old flat numbers.
  var quick=new Vehicle{TopSpeed=120f}; var slow=new Vehicle{TopSpeed=30f};
  Check(CrewDriving.Speed(CrewSlot.Guess,true,quick)>=114f,"In a car that will do 120, Guess is told to hold nearly all of it under pressure");
  Check(CrewDriving.Speed(CrewSlot.Guess,true,quick)>CrewDriving.Speed(CrewSlot.Ice,true,quick)&&CrewDriving.Speed(CrewSlot.Ice,true,quick)>CrewDriving.Speed(CrewSlot.Gohan,true,quick),
   "Guess is the driver of the three and it shows; Ice and Gohan are quick without being him");
  Check(Math.Abs(CrewDriving.Speed(CrewSlot.Guess,true,slow)-60f)<.01f&&Math.Abs(CrewDriving.Speed(CrewSlot.Gohan,false,slow)-40f)<.01f,
   "A slow car never drops a brother below the flat floor he always had");
  Check(Math.Abs(CrewDriving.Speed(CrewSlot.Guess,false,null)-50f)<.01f,"and no car at all is the floor, not a crash");
  Check(CrewDriving.Speed(CrewSlot.Guess,false,quick)<CrewDriving.Speed(CrewSlot.Guess,true,quick),"Nothing urgent means an easier pace in the same car");
  Check((CrewDriving.EscapeFlags&512)!=0&&(CrewDriving.EscapeFlags&262144)!=0&&(CrewDriving.TrafficFlags&512)==0,
   "The escape style allows the wrong side of the road and shortcut links; the everyday style does not");
  Check(CrewDriving.RacingModifier(CrewSlot.Guess,true)>CrewDriving.RacingModifier(CrewSlot.Ice,true)&&CrewDriving.RacingModifier(CrewSlot.Guess,false)==0f,
   "He commits to corners under pressure, Guess hardest, and not at all on a commute");

  // ---- The getaway. Player in the passenger seat, police on the car, a brother at the wheel.
  Reset();var ai=new CompanionController(new ModConfig());
  var car=new Vehicle{TopSpeed=100f};var driver=new Ped{RelationshipGroup=1};var rider=new Ped{RelationshipGroup=1};
  driver.SetIntoVehicle(car,VehicleSeat.Driver);rider.SetIntoVehicle(car,VehicleSeat.Passenger);Game.Player.Character=rider;
  ai.SetHangout(CrewSlot.Gohan,true);
  ai.Update(CrewSlot.Gohan,driver,rider);
  Check(ai.StateOf(CrewSlot.Gohan)==CompanionState.Driving&&driver.Task.DriveKind=="cruise","With nowhere to be and no heat, he drives on");
  var officer=new Ped{RelationshipGroup=Game.GenerateHash("COP"),Position=new Vector3(40,0,0)};
  var farOfficer=new Ped{RelationshipGroup=Game.GenerateHash("COP"),Position=new Vector3(120,0,0)};
  World.Nearby=new[]{farOfficer,officer};Game.Player.WantedLevel=2;Game.GameTime+=1000;
  ai.Update(CrewSlot.Gohan,driver,rider);
  Check(driver.Task.DriveKind=="flee"&&driver.Task.LastMission==VehicleMissionType.Flee&&driver.Task.MissionTarget==officer,
   "With the police on the car he flees the nearest officer, with the engine's own flee mission rather than a point ahead on the road");
  Check(driver.Task.MissionSpeed>=CrewDriving.Speed(CrewSlot.Gohan,true,car)-.01f&&driver.Task.MissionSpeed>60f,"and at the car's own escape pace, well past the old flat number");
  Check((driver.Task.MissionFlags&512)!=0,"in the escape style, so a median is not a wall");
  Check(Function.Values.ContainsKey(Hash.SET_DRIVER_RACING_MODIFIER),"and committing to his corners");
  int flees=driver.Task.VehicleMissions;Game.GameTime+=1000;ai.Update(CrewSlot.Gohan,driver,rider);
  Check(driver.Task.VehicleMissions==flees,"The flee task is not re-issued every review against the same officer, which would make him hesitate");
  farOfficer.Position=new Vector3(30,0,0);Game.GameTime+=750;ai.Update(CrewSlot.Gohan,driver,rider);
  Check(driver.Task.VehicleMissions==flees&&driver.Task.MissionTarget==officer,
   "A different nearer officer cannot reset a valid flee task before five seconds");
  officer.Position=new Vector3(200,0,0);Game.GameTime+=CompanionDriver.FleeRefreshMs+100;ai.Update(CrewSlot.Gohan,driver,rider);
  Check(driver.Task.VehicleMissions==flees+1&&driver.Task.MissionTarget==farOfficer,"but once the refresh is due a nearer officer becomes the one he flees");
  farOfficer.IsDead=true;officer.Position=new Vector3(40,0,0);Game.GameTime+=750;ai.Update(CrewSlot.Gohan,driver,rider);
  Check(driver.Task.VehicleMissions==flees+2&&driver.Task.MissionTarget==officer,
   "A dead flee target is replaced immediately even inside the cooldown");
  World.WaypointBlip=new Blip{Position=new Vector3(500,500,5)};Game.GameTime+=1000;ai.Update(CrewSlot.Gohan,driver,rider);
  Check(driver.Task.DriveKind=="road"&&driver.Task.DriveTarget==World.WaypointBlip.Position&&(driver.Task.LastDriveStyle&512)!=0,
   "A map waypoint set during the chase is driven to, at escape pace and in the escape style, rather than fled from");
  World.WaypointBlip=null;Game.Player.WantedLevel=0;Game.GameTime+=1000;ai.Update(CrewSlot.Gohan,driver,rider);
  Check(driver.Task.DriveKind!="flee"&&driver.Task.DriveKind!="",
   "Heat gone, the flee task is replaced rather than left running against an officer who has lost interest");
  Check((driver.Task.LastDriveStyle&512)==0,"and the everyday style is back, on the right side of the road");

  // ---- The player at the wheel himself is nobody's getaway; and a mission's destination wins.
  Reset();ai=new CompanionController(new ModConfig());
  car=new Vehicle{TopSpeed=100f};driver=new Ped{RelationshipGroup=1};rider=new Ped{RelationshipGroup=1};
  driver.SetIntoVehicle(car,VehicleSeat.Driver);rider.SetIntoVehicle(car,VehicleSeat.Passenger);Game.Player.Character=rider;
  ai.SetHangout(CrewSlot.Ice,true);World.Nearby=new[]{new Ped{RelationshipGroup=Game.GenerateHash("COP"),Position=new Vector3(30,0,0)}};Game.Player.WantedLevel=3;
  ai.Driver.MissionDestination=(s,v)=>new Vector3(900,900,5);
  ai.Update(CrewSlot.Ice,driver,rider);
  Check(driver.Task.DriveKind=="road"&&driver.Task.DriveTarget==new Vector3(900,900,5),"A mission's destination is still driven to under pursuit: the getaway never overrides where a chapter is sending him");
  ai.Driver.MissionDestination=null;
  var onFoot=new Ped{RelationshipGroup=1,Position=new Vector3(0,0,0)};Game.Player.Character=onFoot;
  ai.Refresh(CrewSlot.Ice);Game.GameTime+=CompanionController.SeatShuffleMs+1000;ai.Update(CrewSlot.Ice,driver,onFoot);
  Check(driver.Task.DriveKind!="flee","With the player out of the car there is nobody aboard to get away, and he does not flee on his own account");
  World.Nearby=new Ped[0];Game.Player.WantedLevel=0;
 }
}
