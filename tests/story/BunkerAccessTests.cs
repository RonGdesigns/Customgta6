using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;
public static partial class StoryTests
{
 static void BunkerAccessChecks()
 {
  Reset();var crew=Roster();var c=Context(crew);var state=CampaignState.Load(Path.Combine(root,"bunker39.json"));
  var homes=new CrewHomes(crew,state,c.Locations,new WeaponProgression(state));homes.Allowed=()=>true;
  Check(!homes.BunkerUnlocked&&!homes.BunkerEntrance.HasValue,"A fresh campaign cannot enter the bunker before M23");
  state.Unlock(BunkerSite.Unlock);
  Check(homes.BunkerUnlocked&&homes.BunkerEntrance.HasValue,"The existing bunker unlock enables the relocated headquarters");
  var outside=homes.BunkerEntrance.Value;Game.Player.Character.Position=outside;
  Function.InteriorId=0;World.CollisionReady=false;homes.EnterBunker();
  Check(homes.Apartment.Busy&&Game.Player.Character.IsPositionFrozen&&!Game.Player.CanControlCharacter,"Entry protects the player while the underground interior streams");
  Game.GameTime+=2000;homes.UpdateTransition();
  Check(homes.Apartment.Busy&&!homes.Apartment.Inside&&!Game.Player.CanControlCharacter,"Missing interior registration cannot be treated as a successful visit");
  Game.GameTime+=13000;homes.UpdateTransition();
  Check(!homes.Apartment.Busy&&!homes.Apartment.Inside&&!homes.BunkerVisit&&Game.Player.Character.Position==outside&&Game.Player.CanControlCharacter&&!Game.Player.Character.IsPositionFrozen,"A load timeout restores the exterior and movement, with no false bunker visit");
  Function.InteriorId=271617;Function.InteriorReady=true;World.CollisionReady=true;homes.EnterBunker();Game.GameTime+=300;homes.UpdateTransition();homes.UpdateTransition();
  Check(homes.Apartment.Inside&&homes.BunkerVisit&&homes.ResidenceName.Contains("Senora")&&homes.SavePosition==outside,"A ready furnished bunker is usable and saves the outside position");
  Check(Function.Calls.Any(x=>x.Item1==Hash.REQUEST_IPL&&(string)x.Item2[0]==BunkerSite.ExteriorIpl)&&Function.Calls.Any(x=>x.Item1==Hash.ACTIVATE_INTERIOR_ENTITY_SET&&(string)x.Item2[1]=="standard_bunker_set"),"The real exterior and standard interior furnishings are requested");
  Check(homes.CanManageFleet,"Fleet purchases are available inside the held Senora bunker without the Foundry");
  int plans=0;homes.OpenPlanningBoard=()=>plans++;homes.ReviewFoundryPlan();
  Check(plans==1,"The bunker planning action reaches the shared campaign board");
  var planSource=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Core","DevMenu.CampaignPlan.cs"));
  Check(planSource.Contains("_homes.CanManageFleet")&&!planSource.Contains("!_homes.FoundryVisit"),"The planning menu accepts both headquarters, including the bunker");
  Game.Player.Character.Health=100;homes.Rest();Check(Game.Player.Character.Health==CrewDurability.Health,"Rest and save remains functional inside the bunker");
  homes.Allowed=()=>false;Check(!homes.CanManageFleet,"A mission or blocked activity prevents headquarters fleet purchases");homes.Allowed=()=>true;
  Game.Player.WantedLevel=1;Check(!homes.CanManageFleet,"Wanted heat still blocks headquarters fleet purchases");Game.Player.WantedLevel=0;
  homes.ExitApartment();Check(!homes.CanManageFleet,"Fleet confirmation cannot proceed during an exit transition");Game.GameTime+=300;homes.UpdateTransition();homes.UpdateTransition();
  Check(!homes.Apartment.Inside&&!homes.BunkerVisit&&Game.Player.Character.Position==outside&&Game.Player.CanControlCharacter,"Leaving returns to the same entrance without losing controls");
  Check(!homes.CanManageFleet,"A stale fleet menu cannot buy after leaving the headquarters");
  homes.EnterBunker();Game.GameTime+=300;homes.UpdateTransition();homes.UpdateTransition();homes.StopApartment();
  Check(Game.Player.Character.Position==outside&&!homes.Apartment.Inside&&!homes.BunkerVisit,"Cancellation inside returns safely to the exterior");
  var access=new ApartmentAccess(crew);Function.InteriorId=0;World.CollisionReady=false;
  var objective=new BunkerAccessObjective(access,true,outside);objective.Enter(c);Game.Accept=true;objective.Update(c);Game.GameTime+=13001;access.Update();objective.Update(c);
  Check(objective.Status==ObjectiveStatus.Failed&&Game.Player.Character.Position==outside,"M23 reports a failed room load instead of unlocking an unusable bunker");
  Check(c.Locations.Get("M23.BunkerDoor")==null&&c.Locations.Get("M31.Start")==null&&c.Locations.Position(BunkerSite.EntranceKey).X<1000,"Retired radar-yard survey keys cannot relocate the new headquarters");
  foreach(var key in new[]{"M23.ToolBay","M23.FuelBay","M23.VehicleBay","M29.Senora.Delivery","M31.Senora.Start","M32.Senora.Delivery","M34.Senora.Shelter","M35.Senora.Delivery","M38.Senora.Delivery"})
   Check(c.Locations.Position(key).DistanceTo(outside)<65f,key+" belongs to the real bunker yard");
  Function.InteriorId=123;World.CollisionReady=true;
 }
}
