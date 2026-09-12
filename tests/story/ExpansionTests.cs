using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Math;

public static partial class StoryTests
{
 static void ExpansionChecks()
 {
  Reset();var vehicles=new StoryVehicles();var player=Game.Player.Character;
  Check(StoryVehicles.Catalog.Length==118&&StoryVehicles.Catalog.Select(v=>v.Model).Distinct().Count()==118,"Expanded vehicle catalog has 118 distinct models, the jets, the jetpack, the Oppressors and the newer cars among them");
  Check(StoryVehicles.Catalog.Select(v=>v.Category).Distinct().Count()==8,"Vehicle browsing separates 8 transport categories");
  player.Position=new Vector3(100,100,0);var boat=StoryVehicles.Catalog.First(v=>v.Model=="longfin");World.WaterAvailable=false;
  Check(!vehicles.Spawn(boat)&&World.Vehicles.Count==0,"Boat request refuses dry land without creating an entity");
  World.WaterAvailable=true;Check(vehicles.Spawn(boat)&&World.Vehicles.Last().Model.IsBoat,"Boat request uses a water footprint");vehicles.Clear();
  var plane=StoryVehicles.Catalog.First(v=>v.Model=="vestra");Check(vehicles.Spawn(plane)&&World.Vehicles.Last().Model.IsPlane,"Away from a runway a plane is set on the clear road ahead");vehicles.Clear();
  player.Position=new Vector3(1730,3230,41);Check(vehicles.Spawn(plane)&&!World.Vehicles.Last().IsEngineRunning,"Plane is created at a runway with its engine off");vehicles.Clear();
  var heli=StoryVehicles.Catalog.First(v=>v.Model=="supervolito");World.FailNavigation=true;Check(vehicles.Spawn(heli)&&World.Vehicles.Last().Model.IsHelicopter,"With no ground query a helicopter is still set down ahead when the spot is clear");vehicles.Clear();
  World.FailNavigation=false;Check(vehicles.Spawn(heli)&&!World.Vehicles.Last().IsEngineRunning,"Helicopter request starts grounded with its engine off");vehicles.Clear();
  var bike=StoryVehicles.Catalog.First(v=>v.Model=="shinobi");Check(vehicles.Spawn(bike)&&World.Vehicles.Last().Model.IsBike,"Motorcycle request uses road placement");vehicles.Clear();
  Reset();var crew=Roster();var c=Context(crew);var path=Path.Combine(root,"dispatches.json");var state=CampaignState.Load(path);var feed=new CampaignDispatches(state);
  Game.GameTime=50000;feed.Update(true);Check(state.ReadDispatches.Count==0,"Unfinished missions never leak their aftermath messages");
  state.Completed.Add("M01");feed.Update(false);Check(state.ReadDispatches.Count==0,"Mission/menu suppression keeps pending messages unread");
  Game.Player.WantedLevel=2;feed.Update(true);Check(state.ReadDispatches.Count==0,"Messages wait while police are chasing the player");
  Game.Player.WantedLevel=0;feed.Update(true);Check(state.ReadDispatches.Contains("M01")&&GameUtils.Message.Contains("Gohan"),"Completed mission delivers the relevant crew message");
  var loaded=CampaignState.Load(path);Check(loaded.ReadDispatches.Contains("M01"),"Dispatch history persists across reloads");
  state.Completed.Add("SM03");feed.Update(true);Check(!state.ReadDispatches.Contains("SM03"),"Dispatch cooldown prevents multiple messages arriving together");
  Game.GameTime+=90000;feed.Update(true);Check(state.ReadDispatches.Contains("SM03")&&feed.Inbox.Any(m=>m.Sender=="KJ"),"KJ follows up after Guess's solo race without becoming playable");
  var locker=new WeaponProgression(state);var homes=new CrewHomes(crew,state,c.Locations,locker);Game.Player.Character.Position=homes.Position(crew.ActiveSlot).Value;
  Game.Player.Character.Armor=0;homes.UseWorkbench();Check(Game.Player.Character.Armor==CrewDurability.Armor,"Ice's home bench replaces armor without resting");
  int opens=0;homes.OpenMenu=()=>opens++;Game.GameTime+=CutsceneDirector.SkipGraceMs;Game.Accept=true;homes.Update(true);Check(opens==1,"Home interaction opens the home menu instead of forcing rest");
  homes.Allowed=()=>false;Game.Player.Character.Armor=0;homes.UseWorkbench();Check(Game.Player.Character.Armor==0,"Workbench refuses actions while mission context blocks home use");homes.Allowed=()=>true;
  crew.ActiveSlot=CrewSlot.Gohan;Game.Player.Character=crew.Peds[CrewSlot.Gohan];Game.Player.Character.Position=homes.Position(CrewSlot.Gohan).Value;int leads=0;homes.RouteNextLead=()=>leads++;homes.UseWorkbench();Check(leads==1,"Gohan's workstation requests a real available mission lead");
  crew.ActiveSlot=CrewSlot.Guess;Game.Player.Character=crew.Peds[CrewSlot.Guess];Game.Player.Character.Position=homes.Position(CrewSlot.Guess).Value;
  var car=new Vehicle{Position=Game.Player.Character.Position};Game.Player.LastVehicle=car;homes.UseWorkbench();Check(car.Repairs==1,"Guess repairs the actual nearby parked vehicle");
  car.Position+=new Vector3(100,0,0);homes.UseWorkbench();Check(car.Repairs==1,"Chop bay cannot remotely repair a distant vehicle");
  state.Reset();Check(CampaignState.Load(path).ReadDispatches.Count==0,"Campaign reset clears message delivery history");
 }
}
