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
 static void FoundryConvoyChecks()
 {
  Reset();var crew=Roster();var c=Context(crew);var state=CampaignState.Load(Path.Combine(root,"foundry.json"));
  var homes=new CrewHomes(crew,state,c.Locations,new WeaponProgression(state));var player=Game.Player.Character;
  Check(!homes.FoundryUnlocked&&!homes.FoundryEntrance.HasValue,"Foundry entry respects its campaign ownership flag");
  state.Safehouses["cypressFoundry"]=true;var outside=homes.FoundryEntrance.Value;player.Position=outside;
  Game.Player.WantedLevel=2;homes.EnterFoundry();Check(!homes.Apartment.Busy,"Wanted players cannot evade pursuit through the hideout");Game.Player.WantedLevel=0;
  homes.Allowed=()=>false;homes.EnterFoundry();Check(!homes.Apartment.Busy,"Foundry access obeys shared mission, survey and scene blocking");homes.Allowed=()=>true;
  Function.InteriorReady=true;World.CollisionReady=true;Function.Calls.Clear();Function.Values[Hash.IS_INTERIOR_ENTITY_SET_ACTIVE]=true;homes.EnterFoundry();
  Check(homes.FoundryVisit&&homes.Apartment.Busy&&!Game.Player.CanControlCharacter,"Foundry loads through bounded interior transition with controls held");
  Check(Function.Calls.Any(x=>x.Item1==Hash.REQUEST_IPL&&(string)x.Item2[0]=="bkr_biker_interior_placement_interior_1_biker_dlc_int_02_milo")&&!Function.Calls.Any(x=>x.Item1==Hash.REQUEST_IPL&&((string)x.Item2[0]).EndsWith("milo_")),"Foundry requests the registered runtime IPL rather than the archive's internal underscore name");
  Game.GameTime+=300;homes.UpdateTransition();homes.UpdateTransition();
  Check(homes.Apartment.Inside&&!homes.Apartment.Busy&&Game.Player.CanControlCharacter&&!player.IsPositionFrozen&&homes.ResidenceName.Contains("Foundry"),"Ready Foundry interior restores walking input and correct residence services");
  Check(Function.Calls.Any(x=>x.Item1==Hash.ACTIVATE_INTERIOR_ENTITY_SET&&(string)x.Item2[1]=="gun_locker")&&Function.Calls.Any(x=>x.Item1==Hash.DEACTIVATE_INTERIOR_ENTITY_SET&&(string)x.Item2[1]=="no_gun_locker"),"Foundry enables the visible weapons locker and removes its conflicting closed variant");
  Check(homes.RoomSurveyKeys.All(k=>c.Locations.Get(k)!=null)&&homes.RoomSpot("Locker")==null,"All Foundry room markers are surveyable; unverified furniture coordinates stay at entry services");
  int plans=0;homes.RouteNextLead=()=>plans++;homes.ReviewFoundryPlan();homes.RestockLocker();
  Check(plans==1&&homes.SavePosition==outside,"Planning is usable inside and saves keep the exterior return position");
  homes.ExitApartment();Game.GameTime+=300;homes.UpdateTransition();homes.UpdateTransition();
  Check(!homes.FoundryVisit&&!homes.Apartment.Inside&&player.Position==outside&&crew.CompanionAI.Controlled.Count==0,"Foundry exit returns to actual entrance and releases companions");
  Check(Function.Calls.Last(x=>(x.Item1==Hash.ACTIVATE_INTERIOR_ENTITY_SET||x.Item1==Hash.DEACTIVATE_INTERIOR_ENTITY_SET)&&(string)x.Item2[1]=="no_gun_locker").Item1==Hash.ACTIVATE_INTERIOR_ENTITY_SET,"Exit restores pre-existing interior variants instead of stripping another script's furnishing");
  Function.Values.Remove(Hash.IS_INTERIOR_ENTITY_SET_ACTIVE);
  Function.IplReady=false;homes.EnterFoundry();Game.GameTime+=13000;homes.UpdateTransition();
  Check(!homes.FoundryVisit&&!homes.Apartment.Busy&&Game.Player.CanControlCharacter&&player.Position==outside,"Missing Foundry IPL rolls back without stranding the player");Function.IplReady=true;
  state.Safehouses["cypressFoundry"]=false;Check(!homes.FoundryEntrance.HasValue,"Losing the Foundry in the story removes access");

  Reset();crew=Roster();c=Context(crew);Use(crew,CrewSlot.Guess);var heli=new Vehicle{Position=new Vector3(120,0,50),HeightAboveGround=50};var escort=new Vehicle();player=Game.Player.Character;
  player.SetIntoVehicle(heli,VehicleSeat.Driver);bool arrived=false;var objective=new ConvoyOverwatchObjective(()=>heli,()=>escort,()=>arrived){RequiredCharacter=CrewSlot.Guess};objective.Enter(c);
  for(int i=0;i<2;i++){Game.GameTime+=1000;objective.Update(c);}Check(!objective.IsFinished&&objective.Label.Contains("2/25s")&&objective.Label.Contains("60-350m"),"Convoy HUD displays actual distance band and accumulated tracking progress");
  arrived=true;Game.GameTime+=1000;objective.Update(c);Check(objective.IsFinished,"Convoy reaching Ice advances after three seconds of valid pilot contact instead of waiting indefinitely");
  objective=new ConvoyOverwatchObjective(()=>heli,()=>escort,()=>false){RequiredCharacter=CrewSlot.Guess};objective.Enter(c);heli.HeightAboveGround=0;Game.GameTime+=1000;objective.Update(c);
  Check(!objective.IsFinished&&objective.Label.Contains("8m"),"A parked helicopter cannot satisfy aerial tracking");
  heli.HeightAboveGround=50;Game.GameTime+=1000;objective.Update(c);heli.Position=new Vector3(1,0,0);Game.GameTime+=1000;objective.Update(c);Check(objective.Label.Contains("too close"),"Moving too close tells the pilot how to recover");
  Game.GameTime+=8000;objective.Update(c);Check(objective.Status==ObjectiveStatus.Failed,"Lost safe contact ends with an explicit bounded failure");

  Reset();crew=Roster();c=Context(crew);var m=new M09RollingThunder();m.Begin(c);c.Cutscenes.Skip();m.Tick();Use(crew,CrewSlot.Guess);player=Game.Player.Character;player.SetIntoVehicle(m.Frogger,VehicleSeat.Driver);m.Tick();
  Check(m.CurrentStage==0&&World.Created.Where(x=>x.Model.Name=="s_m_y_marine_01").All(x=>x.Task.Drives==0),"Convoy waits while the pilot is still on the ground");
  m.Frogger.HeightAboveGround=10;m.Frogger.IsInAir=true;m.Tick();
  Check(m.CurrentStage==1&&World.Created.Where(x=>x.Model.Name=="s_m_y_marine_01").Select(x=>x.Task.LastDrivePoint).Distinct().Count()==3,"Convoy starts only after takeoff and has three separate stopping positions");
  m.EscortTruck.Position=c.Locations.Position("M09.AmbushPoint");m.Frogger.Position=m.EscortTruck.Position+new Vector3(0,100,60);
  for(int i=0;i<4;i++){Game.GameTime+=1000;m.Tick();}
  Check(m.CurrentStage==2&&m.ConvoyAtAmbush,"M09 advances to the driver objective when convoy reaches the ambush early");
  Use(crew,CrewSlot.Ice);var guess=crew.PedFor(CrewSlot.Guess);int tasks=guess.Task.HeliTasks;m.Tick();m.Tick();
  Check(guess.Task.HeliTasks==tasks+1&&crew.CompanionAI.Controlled.Contains(CrewSlot.Guess),"Guess receives one hover task while the player controls Ice");
  m.Cleanup();Check(!crew.CompanionAI.Controlled.Contains(CrewSlot.Guess),"Aborting M09 releases its helicopter pilot ownership");
  Reset();crew=Roster();c=Context(crew);m=new M09RollingThunder();m.Begin(c);c.Cutscenes.Skip();m.Tick();Use(crew,CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(m.Frogger,VehicleSeat.Driver);m.Frogger.HeightAboveGround=10;m.Tick();
  var driver=World.Created.First(x=>x.CurrentVehicle==m.EscortTruck);int drives=driver.Task.Drives;Game.GameTime+=12000;m.Tick();
  Check(driver.Task.Drives==drives+1,"Blocked convoy gets a fresh road task after twelve seconds");
  for(int i=0;i<4;i++){Game.GameTime+=12000;m.Tick();}
  Check(m.Status==MissionStatus.Failed&&m.FailReason.Contains("blocked"),"Permanently blocked route ends with a diagnostic retry instruction");
 }
}
