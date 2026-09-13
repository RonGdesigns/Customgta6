using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

public static partial class StoryTests
{
 static void SoloHarborFollowupChecks()
 {
  Reset();var crew=Roster();Use(crew,CrewSlot.Ice);
  var car=new Vehicle{Position=Game.Player.Character.Position,Capacity=3};
  crew.PedFor(CrewSlot.Guess).SetIntoVehicle(car,VehicleSeat.Driver);
  crew.PedFor(CrewSlot.Gohan).SetIntoVehicle(car,VehicleSeat.RightFront);
  int next=0;Game.Pressed.Add(Control.Enter);MissionBoarding.Update(car,VehicleSeat.Any,ref next);
  Check(Game.Player.Character.Task.LastSeat==VehicleSeat.LeftRear&&Game.Player.Character.Task.Enters==1,"Normal enter chooses the empty rear seat instead of jacking the protected crew driver");
  Check(crew.PedFor(CrewSlot.Guess).IsInVehicle(car)&&crew.PedFor(CrewSlot.Gohan).IsInVehicle(car),"Boarding leaves the driver and existing passenger seated");
  Check(!Game.Player.Character.IsInVehicle()&&Game.Disabled.Contains(Control.Enter),"Boarding requests a door animation and consumes the default driver-jack input; it never warps the player");
  Game.Pressed.Add(Control.Enter);MissionBoarding.Update(car,VehicleSeat.Any,ref next);Check(Game.Player.Character.Task.Enters==1,"Repeated enter presses do not restart the same door animation during its cooldown");
  Check(MissionBoarding.FreeSeat(car,VehicleSeat.Driver)==VehicleSeat.None,"A mission requiring the driver seat cannot silently board a passenger seat");
  Game.Pressed.Clear();Game.GameTime+=3000;car.Speed=15;Game.Pressed.Add(Control.Enter);MissionBoarding.Update(car,VehicleSeat.Any,ref next);Check(Game.Player.Character.Task.Enters==1,"An enter request cannot grab an AI vehicle moving at speed");

  Reset();crew=Roster();var c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"stairs-stream-failure.json"));
  var solo=new SM02ZeroDayInjection();solo.Begin(c);c.Cutscenes.Skip();solo.Tick();Use(crew,CrewSlot.Gohan);
  var entrance=c.Locations.Position("SM02.StairEntry");World.CollisionReady=false;
  Interact(solo,c,CrewSlot.Gohan,entrance,2);
  Check(solo.Status==MissionStatus.Failed&&!GameUtils.Faded&&!Game.Player.Character.IsPositionFrozen&&Game.Player.CanControlCharacter,"An unloaded stair exit fails clearly and restores camera fade and player movement");
  Check(Game.Player.Character.Position.DistanceTo(entrance)<.1f,"A failed roof transition returns Gohan to his safe entrance rather than dropping him through the map");solo.Abort();

  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"tank-delay.json"));GameUtils.RoadAvailable=true;
  var lift=new M16TheHeavyLift();lift.Begin(c);c.Cutscenes.Skip();lift.Tick();
  Check(Convert.ToBoolean(Function.Values[Hash.SET_ARTIFICIAL_LIGHTS_STATE])&&!World.Vehicles.Any(v=>v.Model.Name=="rhino"),"The base starts blacked out with no tanks on top of the crew");
  Game.GameTime+=60000;lift.Tick();Check(!World.Vehicles.Any(v=>v.Model.Name=="rhino"),"Waiting outside cannot start armor before the hangar-cleared pickup step");
  Use(crew,CrewSlot.Guess);Game.Player.Character.Position=lift.Cargobob.Position;lift.JumpToStage(2);lift.Tick();
  Game.GameTime+=13999;lift.Tick();Check(!World.Vehicles.Any(v=>v.Model.Name=="rhino"),"The first tank waits its full mobilization delay after pickup begins");
  Game.GameTime+=2;lift.Tick();Check(World.Vehicles.Count(v=>v.Model.Name=="rhino")==1,"The first delayed tank arrives alone");
  Game.GameTime+=16001;lift.Tick();Check(World.Vehicles.Count(v=>v.Model.Name=="rhino")==2,"The second armor response is staggered instead of appearing simultaneously");
  lift.Abort();Check(!Convert.ToBoolean(Function.Values[Hash.SET_ARTIFICIAL_LIGHTS_STATE]),"Aborting restores normal world lighting");

  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"tug-buoyancy.json"));
  Function.ClearLos=false;var water=new M13SmugglersCut();water.Begin(c);c.Cutscenes.Skip();water.Tick();
  var tugs=World.Vehicles.Where(v=>v.Model.Name=="tug").ToArray();
  Check(tugs.Length==3&&tugs.All(v=>!v.IsPositionFrozen),"All three tugs retain water physics so their hulls settle to their actual waterline");
  Check(tugs[0].Position.DistanceTo(tugs[2].Position)>250f,"Tugs span a meaningful length of channel instead of clustering by the start");
  Use(crew,CrewSlot.Ice);Game.Player.Character.SetIntoVehicle(water.Kayak,VehicleSeat.Driver);
  water.Tick();foreach(var tug in tugs)Interact(water,c,CrewSlot.Ice,tug.Position+new Vector3(tug.ForwardVector.Y,-tug.ForwardVector.X,0)*7.141756f,6,true);
  water.Tick();Check(World.Vehicles.Count(v=>v.Model.Name=="predator")>=2,"The charge alarm brings at least two actual pursuit boats");water.Abort();Function.ClearLos=true;
 }
}
