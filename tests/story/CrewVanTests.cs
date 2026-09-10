using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using GTA;
using GTA.Math;
using GTA.Native;

public static partial class StoryTests
{
 static void CrewVanChecks()
 {
  // ---- The record round-trips through the save.
  Reset();var path=Path.Combine(root,"van.json");var state=CampaignState.Load(path);
  state.CrewVan.Mods[(int)VehicleModType.Armor]=2;state.CrewVan.Mods[(int)VehicleModType.Engine]=1;state.CrewVan.PrimaryColor=7;state.CrewVan.SecondaryColor=7;state.CrewVan.Livery=1;state.CrewVan.TiresReinforced=true;state.Save();
  var loaded=CampaignState.Load(path);
  Check(loaded.CrewVan.Mods.Count==2&&loaded.CrewVan.Mods[(int)VehicleModType.Armor]==2&&loaded.CrewVan.PrimaryColor==7&&loaded.CrewVan.Livery==1&&loaded.CrewVan.TiresReinforced&&loaded.CrewVan.Model=="granger","The van's mods, paint, livery and tires survive a reload");
  Check(CampaignState.Load(Path.Combine(root,"van-fresh.json")).CrewVan.Mods.Count==0,"A fresh campaign starts with a stock van");

  // ---- Free roam: the van appears at the stash when the crew is out and nearby, once, and is captured when the player gets out.
  Reset();var crew=Roster();var c=Context(crew);var vans=new CrewVan(loaded,c.Locations);var stash=c.Locations.Position(CrewVan.StashKey);
  Check(c.Locations.Get(CrewVan.StashKey)!=null&&stash.DistanceTo(c.Locations.Position("Base.CypressFlats"))<30f,"The stash is a location key beside the Cypress base");
  var player=Game.Player.Character;player.Position=stash+new Vector3(400,0,0);vans.Update(crew,true);
  Check(vans.Current==null,"Far from the stash nothing is spawned");
  player.Position=stash+new Vector3(50,0,0);vans.Update(crew,false);Check(vans.Current==null,"During a mission the free-roam van is not spawned");
  vans.Update(crew,true);var van=vans.Current;
  Check(van!=null&&van.Model.Name=="granger"&&van.IsPersistent&&!van.IsEngineRunning&&van.Position.DistanceTo(stash)<0.5f,"Near the stash with the crew out, the crew's Granger is parked there");
  Check(van.Mods[VehicleModType.Armor].Index==2&&van.Mods[VehicleModType.Engine].Index==1&&(int)van.Mods.PrimaryColor==7&&van.Mods.Livery==1&&!van.CanTiresBurst,"The parked van carries the saved customization");
  vans.Update(crew,true);Check(World.Vehicles.Count(v=>v.Model.Name=="granger")==1,"Only one van is kept");
  player.SetIntoVehicle(van,VehicleSeat.Driver);vans.Update(crew,true);van.Mods[VehicleModType.Spoilers].Index=1;van.Mods.SecondaryColor=(VehicleColor)3;player.Task.LeaveVehicle();vans.Update(crew,true);
  Check(loaded.CrewVan.Mods[(int)VehicleModType.Spoilers]==1&&loaded.CrewVan.SecondaryColor==3,"Getting out of the van saves what was done to it");
  Check(CampaignState.Load(path).CrewVan.SecondaryColor==3,"The capture went to disk");
  vans.Release();Check(vans.Current==null&&van.Present&&van.Released,"Stand-down leaves the van in the world and stops tracking it");

  // ---- Missions spawn the same van, not a plain Granger.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"van-m02.json"));c.State.CrewVan.PrimaryColor=5;c.State.CrewVan.Mods[(int)VehicleModType.Armor]=2;
  c.Vans=new CrewVan(c.State,c.Locations);var m2=new M02LooseStrands();Check(m2.Begin(c),"M02 sets up");
  var chase=World.Vehicles.First(v=>v.Model.Name=="granger");
  Check((int)chase.Mods.PrimaryColor==5&&chase.Mods[VehicleModType.Armor].Index==2&&c.Vans.Current==chase,"M02's chase car is the crew van with its customization");m2.Abort();
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"van-m06.json"));c.State.CrewVan.Livery=1;c.Vans=new CrewVan(c.State,c.Locations);
  var m6=new M06CleanSweep();Check(m6.Begin(c),"M06 sets up");
  Check(World.Vehicles.First(v=>v.Model.Name=="granger").Mods.Livery==1,"M06's Granger is the crew van too");m6.Abort();
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"van-none.json"));c.Vans=null;
  Check(new M02LooseStrands().Begin(c),"Without a van service the missions still spawn a stock Granger");

  // ---- The ground probe: the authored height wins when it is walkable; the snap is only a fallback.
  Reset();crew=Roster();c=Context(crew);var curb=c.Locations.Get("Prologue.LSIACar");float authored=curb.Position.Z;World.GroundHeight=authored+9f;
  Check(MissionSites.Ground(c.Locations,"Prologue.LSIACar")&&Math.Abs(c.Locations.Position("Prologue.LSIACar").Z-(authored+0.1f))<0.2f,"A walkable curb is not moved up onto the terminal roof by the ground snap");
  World.GroundHeight=0f;

  // ---- The next operations are declared.
  Check(MissionManager.Continuations["M44"]=="M45"&&MissionManager.Continuations["M47"]=="M48"&&!MissionManager.Continuations.ContainsKey("M48"),"Paleto Deep-Sea runs M44 through M48");
  Check(MissionManager.Continuations["M63"]=="M64"&&MissionManager.Continuations["M66"]=="M67"&&MissionManager.Continuations["M69"]=="M70"&&!MissionManager.Continuations.ContainsKey("M70"),"Blood Brothers runs M63 through M70");
  Check(!MissionManager.Continuations.ContainsKey("M18")&&!MissionManager.Continuations.ContainsKey("M43")&&!MissionManager.Continuations.ContainsKey("M04"),"Staging links and separate jobs are not chained");
 }
}
