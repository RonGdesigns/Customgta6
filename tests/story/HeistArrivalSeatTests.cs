using System;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using GTA;
using GTA.Math;
public static partial class StoryTests
{
 static void HeistArrivalSeatChecks()
 {
  foreach(bool skip in new[]{false,true})
  {
   Reset();var driver=new Ped { EjectOnTaskClear=true };var passenger=new Ped();var car=new Vehicle();
   driver.SetIntoVehicle(car,VehicleSeat.Driver);passenger.SetIntoVehicle(car,VehicleSeat.Passenger);
   var scene=new SceneBlocking().Then(new DriveUpStep(driver,car,new Vector3(100,100,0),90));
   if(skip)scene.Complete();
   else {scene.Update();Game.GameTime+=23000;scene.Update();}
   Check(scene.Succeeded&&PortHeistWorld.Seated(driver,car,VehicleSeat.Driver)&&
    PortHeistWorld.Seated(passenger,car,VehicleSeat.Passenger),
    "Skipped or timed-out driving preserves driver and passenger when task cleanup ejects the driver");
  }
  // Rejected boarding must report failure, not leave a permanent live objective.
  foreach(bool escort in new[]{true,false})
  {
   Reset();var crew=Roster();var c=Context(crew);
   ComposedMission mission=escort?(ComposedMission)new M20SkyHook():new M21OpenWater();
   Check(mission.Begin(c),"A heist section starts before live-transfer fault injection");
   c.Cutscenes.Skip();
   var live=new LiveHandoff("fault injection").Then(new FailedStartSceneStep());
   mission.GetType().GetField("_liveTransfer",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(mission,live);
   mission.Tick();
   Check(mission.Status==MissionStatus.Failed&&mission.FailReason.Contains("could not board"),
    "A failed M20 or M21 live transfer ends with a specific failure instead of hanging");
  }
  Reset();var roadCrew=Roster();var context=Context(roadCrew);var arrival=new M22ScorchedBay();
  Check(arrival.Begin(context),"M22 starts before testing failed beach arrival");context.Cutscenes.Skip();
  var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
  typeof(M22ScorchedBay).GetField("_arrived",flags).SetValue(arrival,false);
  typeof(M22ScorchedBay).GetField("_liveArrival",flags).SetValue(arrival,new LiveHandoff("bad arrival").Then(new FailedStartSceneStep()));
  arrival.Tick();
  Check(arrival.Status==MissionStatus.Failed&&arrival.FailReason.Contains("beach arrival"),
   "A failed live M22 beach arrival does not strand the operation waiting forever");
 }
}
