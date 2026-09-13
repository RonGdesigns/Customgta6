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
 static void FinishSoloScene(MissionContext c,bool skip)
 {
  if(skip)c.Cutscenes.Skip();
  else for(int tick=0;tick<50&&c.Cutscenes.IsActive;tick++){c.Dialogue.Clear();Game.GameTime+=3000;c.Cutscenes.Update();}
  Check(!c.Cutscenes.IsActive&&c.Cutscenes.LastOutcome==(skip?SceneOutcome.Skipped:SceneOutcome.Completed),"P5c scene finishes by watching or skipping");
 }
 static void SecondWindowSoloChecks()
 {
  foreach(bool skip in new[]{false,true})
  {
   Reset();var crew=Roster();var c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"solo4-"+skip+".json"));c.State.BeginAttempt("SM04");
   var m=new SM04DeadDropQuarry();Check(m.Begin(c)&&c.Cutscenes.IsActive,"SM04 introduces both physical listening posts");
   var positions=crew.Peds.Values.Select(p=>p.Position).ToArray();FinishSoloScene(c,skip);
   Check(positions.SequenceEqual(crew.Peds.Values.Select(p=>p.Position))&&m.Radios.Count==2&&m.Radios.All(p=>p.Exists()&&p.IsVisible),"SM04 scene preserves cast positions and shows two radios");
   Game.Player.Character.Position=c.Locations.Position("SM04.Overlook");m.Tick();
   var marksmen=World.Created.ToArray();marksmen[1].IsDead=true;m.Tick();Check(m.CurrentStage==1,"Either marksman can be killed first; the other still blocks collection");
   marksmen[0].IsDead=true;m.Tick();Check(m.CurrentStage==2,"Clearing both posts opens radio recovery");
   Interact(m,c,CrewSlot.Ice,m.Radios[0].Position,2);Check(m.RadiosRecovered==1&&!m.Radios[0].IsVisible&&c.State.CargoAt("quarryRadio1")=="Ice","The first physical radio is bagged with named custody");
   Interact(m,c,CrewSlot.Ice,m.Radios[1].Position,2);Check(m.RadiosRecovered==2&&m.CurrentStage==4,"Both separate radio interactions are required");
   Game.Player.Character.Position=c.Locations.Position("SM04.Approach");m.Tick();Check(c.Cutscenes.IsActive&&c.State.CompletedCount==0,"Returning starts confirmation without awarding the mission early");
   FinishSoloScene(c,skip);c.Dialogue.Clear();m.Tick();c.Dialogue.Clear();m.Tick();
   Check(m.Status==MissionStatus.Passed&&c.State.CargoAt("quarryRadio2")=="crewRadioArchive","SM04 completes with both archived sets");
   c.State.MarkComplete("SM04",new MissionCatalog());var lines=new CompletionRewards(CampaignState.Load(Path.Combine(root,"blank4-"+skip+".json"))).Describe(c.State,0);
   Check(c.State.CashOnHand==35000&&lines.Any(l=>l.Contains("Ice unlocked")),"SM04 first completion exposes Ice's actual weapon entitlement and 35000 payout");
  }
  Reset();var roster=Roster();var ctx=Context(roster);var lost=new SM04DeadDropQuarry();lost.Begin(ctx);ctx.Cutscenes.Skip();Game.Player.Character.Position=ctx.Locations.Position("SM04.Overlook");lost.Tick();foreach(var p in World.Created)p.IsDead=true;lost.Tick();lost.Radios[0].Delete();lost.Tick();
  Check(lost.Status==MissionStatus.Failed,"A missing physical radio fails instead of awarding an empty marker");

  foreach(bool skip in new[]{false,true})
  {
   Reset();roster=Roster();ctx=Context(roster);var m=new SM05BlackBoxEstuary();Check(m.Begin(ctx)&&m.Buoy.Model.Name=="prop_dock_bouy_3","SM05 uses a locally verified buoy asset");FinishSoloScene(ctx,skip);
   Game.Player.Character.SetIntoVehicle(m.Boat,VehicleSeat.Driver);m.Boat.IsEngineRunning=true;m.Tick();
   m.Boat.Position=m.Buoy.Position+new Vector3(6,0,0);Game.Player.Character.Position=m.Boat.Position;m.Tick();
   Check(m.CurrentStage==2&&m.Moored&&m.Boat.IsPositionFrozen&&!Game.Player.Character.IsPositionFrozen,"Mooring holds only the boat and leaves Gohan free to exit");
   Interact(m,ctx,CrewSlot.Gohan,m.Interceptor.Position,6);Check(ctx.Cutscenes.IsActive&&!m.Installed,"The service interaction starts verification without claiming telemetry early");
   FinishSoloScene(ctx,skip);Check(m.Installed&&m.Interceptor.IsVisible&&m.Boat.IsPositionFrozen,"Both scene paths reveal the installed interceptor while the boat waits");
   m.Tick();Check(m.CurrentStage==4,"Verified telemetry opens return boarding");Game.Player.Character.SetIntoVehicle(m.Boat,VehicleSeat.Driver);m.Tick();
   Check(!m.Moored&&!m.Boat.IsPositionFrozen&&m.Boat.IsEngineRunning,"Boarding releases the boat and restores its engine state");
   m.Boat.Position=ctx.Locations.Position("SM05.Boat");Game.Player.Character.Position=m.Boat.Position;m.Tick();ctx.Dialogue.Clear();m.Tick();
   Check(m.Status==MissionStatus.Passed&&m.Boat.Exists(),"SM05 finishes by returning in the actual dinghy and preserves occupied transport");
  }
  foreach(bool frozen in new[]{false,true})
  {
   Reset();roster=Roster();ctx=Context(roster);var m=new SM05BlackBoxEstuary();m.Begin(ctx);ctx.Cutscenes.Skip();m.Boat.IsPositionFrozen=frozen;m.Boat.IsEngineRunning=true;Game.Player.Character.SetIntoVehicle(m.Boat,VehicleSeat.Driver);
   PackageCall(m,"Moor");m.Abort();Check(m.Boat.IsPositionFrozen==frozen&&m.Boat.IsEngineRunning&&!Game.Player.Character.IsPositionFrozen,"Abort restores the boat's prior flags without freezing the swimmer");
  }
  Reset();roster=Roster();ctx=Context(roster);var canceled=new SM05BlackBoxEstuary();canceled.Begin(ctx);ctx.Cutscenes.Skip();PackageCall(canceled,"FitInterceptor");ctx.Cutscenes.Stop();Check(!canceled.Installed,"Canceling the interceptor scene cannot confirm telemetry");canceled.Abort();
  Reset();roster=Roster();ctx=Context(roster);var destroyed=new SM05BlackBoxEstuary();destroyed.Begin(ctx);ctx.Cutscenes.Skip();PackageCall(destroyed,"FitInterceptor");destroyed.Interceptor.Delete();ctx.Cutscenes.Skip();Check(!destroyed.Installed&&ctx.Cutscenes.LastOutcome==SceneOutcome.Failed,"Skipping cannot install a missing interceptor");destroyed.Abort();

  foreach(bool skip in new[]{false,true})
  {
   Reset();roster=Roster();ctx=Context(roster);ctx.State=CampaignState.Load(Path.Combine(root,"solo6-"+skip+".json"));var m=new SM06CanyonRunner();Check(m.Begin(ctx)&&ctx.Cutscenes.IsActive,"SM06 introduces its actual coupled fuel rig");FinishSoloScene(ctx,skip);
   Game.Player.Character.SetIntoVehicle(m.Truck,VehicleSeat.Driver);m.Tick();m.Truck.Position=ctx.Locations.Position("SM06.Bend");m.Truck.Speed=15;Game.Player.Character.Position=m.Truck.Position;m.Tick();
   Check(m.CurrentStage==2,"The canyon checkpoint accepts a moving tanker, not an unnecessary stop");
   var bay=ctx.Locations.Position("SM06.Delivery");m.Truck.Position=bay;m.Tanker.Position=bay;m.Truck.Speed=m.Tanker.Speed=0;Game.Player.Character.Position=bay;Function.Trailers[m.Truck.Handle]=m.Tanker;m.Tick();
   Check(m.CurrentStage==3&&!m.Unloaded,"Arrival opens an actual unload operation");
   Game.Accept=true;m.Tick();Game.Accept=false;Game.GameTime+=3000;m.Tick();Function.Trailers.Remove(m.Truck.Handle);Game.GameTime+=4000;m.Tick();
   Check(m.CurrentStage==3&&!m.Unloaded&&m.CurrentObjective.Contains("Reconnect"),"Detaching the tanker during unloading resets the work instead of banking fuel");
   Function.Trailers[m.Truck.Handle]=m.Tanker;m.Tick();Game.GameTime+=7000;m.Tick();Check(m.CurrentStage==3,"Reconnecting requires a fresh interaction and full unload duration");
   Game.Accept=true;m.Tick();Game.Accept=false;Game.GameTime+=6001;m.Tick();Check(ctx.Cutscenes.IsActive&&!m.Unloaded,"A full stationary coupled unload opens the receiving scene");
   FinishSoloScene(ctx,skip);Check(m.Unloaded&&ctx.State.CargoAt("airfieldFuel")==null,"Fuel is verified before persistent delivery is recorded");m.Tick();ctx.Dialogue.Clear();m.Tick();
   Check(m.Status==MissionStatus.Passed&&ctx.State.CargoAt("airfieldFuel")=="SM06.Delivery","SM06 records fuel at the airfield after verified completion");
  }
  Reset();roster=Roster();ctx=Context(roster);var bad=new SM06CanyonRunner();bad.Begin(ctx);ctx.Cutscenes.Skip();bad.Truck.Position=bad.Tanker.Position=ctx.Locations.Position("SM06.Delivery");Function.Trailers.Remove(bad.Truck.Handle);PackageCall(bad,"ReceiveFuel");ctx.Cutscenes.Skip();
  Check(!bad.Unloaded&&ctx.Cutscenes.LastOutcome==SceneOutcome.Failed,"Skipping a detached delivery scene cannot award airfield reserves");bad.Abort();
  Reset();roster=Roster();ctx=Context(roster);var missingRig=new SM06CanyonRunner();missingRig.Begin(ctx);ctx.Cutscenes.Skip();missingRig.Truck.Delete();missingRig.Tick();
  Check(missingRig.Status==MissionStatus.Failed&&missingRig.FailReason.Contains("tractor"),"A missing tractor fails immediately even outside a driving objective");
  Reset();roster=Roster();ctx=Context(roster);var missingBoat=new SM05BlackBoxEstuary();missingBoat.Begin(ctx);ctx.Cutscenes.Skip();PackageCall(missingBoat,"Moor");missingBoat.Boat.IsDead=true;missingBoat.Tick();
  Check(missingBoat.Status==MissionStatus.Failed&&!missingBoat.Moored&&!Game.Player.Character.IsPositionFrozen,"Losing a moored boat fails clearly and releases its temporary hold");
  Reset();roster=Roster();ctx=Context(roster);GameUtils.RoadAvailable=true;var pursuit=new SM06CanyonRunner();pursuit.Begin(ctx);ctx.Cutscenes.Skip();Game.Player.Character.SetIntoVehicle(pursuit.Truck,VehicleSeat.Driver);pursuit.Tick();
  Check(World.Vehicles.Count(v=>v.Model.Name=="sanchez")==3&&World.Created.Count(p=>p.CurrentVehicle!=null)==3,"A valid road approach launches three occupied pursuit bikes");pursuit.Abort();GameUtils.RoadAvailable=false;
 }
}
