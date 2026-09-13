using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using GTA;
using GTA.Math;
using GTA.Native;

public static partial class StoryTests
{
 static void PackageCall(object mission,string method)
 { mission.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(mission,null); }
 static void WatchPackageScene(MissionContext c)
 {
  for(int tick=0;tick<50&&c.Cutscenes.IsActive;tick++)
  {c.Dialogue.Clear();Game.GameTime+=3000;c.Cutscenes.Update();}
  Check(!c.Cutscenes.IsActive&&c.Cutscenes.LastOutcome==SceneOutcome.Completed,"P5b staged action finishes naturally, without a skip");
 }
 static void RelayFuelPackageChecks()
 {
  foreach(bool skip in new[]{false,true})
  {
   Reset();var crew=Roster();var c=Context(crew);var relay=new M28OffTheGrid();
   Check(relay.Begin(c)&&relay.Panel!=null&&c.Cutscenes.IsActive,"M28 introduces a real relay worktable before control");
   var driver=crew.PedFor(CrewSlot.Guess);var pickup=driver.CurrentVehicle;
   var positions=crew.Peds.Values.Select(p=>p.Position).ToArray();
   if(skip)c.Cutscenes.Skip();else WatchPackageScene(c);
   Check(driver.IsInVehicle(pickup)&&driver.SeatIndex==VehicleSeat.Driver&&positions.SequenceEqual(crew.Peds.Values.Select(p=>p.Position)),"M28 briefing preserves split positions and the extraction driver's seat");
   Flow(relay)[3].Teardown(c);PackageCall(relay,"FinishSplice");
   Check(!relay.Spliced,"M28 does not claim a verified splice when the finish scene only starts");
   if(skip)c.Cutscenes.Skip();else WatchPackageScene(c);
   Check(relay.Spliced,"Watching and skipping M28 both verify the connected surge unit");
   relay.Abort();Check(!relay.Panel.Exists()&&crew.CompanionAI.Controlled.Count==0,"M28 abort removes its worktable assets and releases crew roles");
  }
  Reset();var roster=Roster();var ctx=Context(roster);var failedRelay=new M28OffTheGrid();failedRelay.Begin(ctx);ctx.Cutscenes.Skip();
  PackageCall(failedRelay,"FinishSplice");ctx.Cutscenes.Skip();
  Check(!failedRelay.Spliced&&ctx.Cutscenes.LastOutcome==SceneOutcome.Failed,"M28 cannot skip past a surge case that was never connected");failedRelay.Abort();

  Reset();roster=Roster();ctx=Context(roster);ctx.State=CampaignState.Load(Path.Combine(root,"p5b-fuel.json"));var fuel=new M29DustAndDiesel();
  Check(fuel.Begin(ctx)&&ctx.Cutscenes.IsActive&&!fuel.FuelLoaded,"M29 opens with unloaded fuel and visible transfer controls");ctx.Cutscenes.Skip();
  var truck=Field<Vehicle>(fuel,"_truck");var trailer=Field<Vehicle>(fuel,"_trailer");var delivery=ctx.Locations.Position("M29.Senora.Delivery");
  Flow(fuel)[2].Teardown(ctx);Check(fuel.FuelLoaded&&ctx.State.CargoAt("bunkerFuel")==null,"Finishing the depot transfer fills only this attempt; it does not bank bunker fuel");
  truck.Position=delivery;trailer.Position=delivery;Function.Trailers[truck.Handle]=trailer;
  PackageCall(fuel,"UnloadFuel");ctx.Cutscenes.Stop();
  Check(!fuel.Unloaded&&ctx.State.CargoAt("bunkerFuel")==null,"Canceling the receiving scene cannot award fuel reserves");fuel.Abort();
  Check(World.Props.All(p=>!p.Exists()),"A canceled fuel attempt removes its temporary equipment and drums");

  Reset();roster=Roster();ctx=Context(roster);fuel=new M29DustAndDiesel();fuel.Begin(ctx);ctx.Cutscenes.Skip();Flow(fuel)[2].Teardown(ctx);
  truck=Field<Vehicle>(fuel,"_truck");trailer=Field<Vehicle>(fuel,"_trailer");truck.Position=ctx.Locations.Position("M29.Senora.Delivery");trailer.Position=truck.Position;
  Function.Trailers.Remove(truck.Handle);PackageCall(fuel,"UnloadFuel");ctx.Cutscenes.Skip();
  Check(!fuel.Unloaded&&ctx.Cutscenes.LastOutcome==SceneOutcome.Failed,"A detached tanker fails final receipt verification even if both vehicles are in the bay");fuel.Abort();

  foreach(bool skip in new[]{false,true})
  {
   Reset();roster=Roster();ctx=Context(roster);ctx.State=CampaignState.Load(Path.Combine(root,"p5b-reserves-"+skip+".json"));fuel=new M29DustAndDiesel();
   fuel.Begin(ctx);ctx.Cutscenes.Skip();Flow(fuel)[2].Teardown(ctx);
   truck=Field<Vehicle>(fuel,"_truck");trailer=Field<Vehicle>(fuel,"_trailer");truck.Position=ctx.Locations.Position("M29.Senora.Delivery");trailer.Position=truck.Position;
   Function.Trailers[truck.Handle]=trailer;PackageCall(fuel,"UnloadFuel");
   if(skip)ctx.Cutscenes.Skip();else WatchPackageScene(ctx);
   Check(fuel.Unloaded&&ctx.State.CargoAt("bunkerFuel")==null,"Watching or skipping verifies the attached fuel rig before persistent reward");
   fuel.Pass();Check(fuel.Status==MissionStatus.Passed&&ctx.State.CargoAt("bunkerFuel")=="M23.FuelBay","A successful fuel delivery records the reserves in the receiving bay");
  }

  foreach(bool skip in new[]{false,true})
  {
   Reset();roster=Roster();ctx=Context(roster);ctx.State=CampaignState.Load(Path.Combine(root,"p5b-parts-"+skip+".json"));var ridge=new M30RedlineRidge();
   Check(ridge.Begin(ctx)&&ridge.Parts!=null&&ridge.Parts.AttachedTo!=null,"M30 starts with a physical satellite case attached to its truck");
   truck=Field<Vehicle>(ridge,"_truck");var ice=roster.PedFor(CrewSlot.Ice);var gohan=roster.PedFor(CrewSlot.Gohan);
   Check(Math.Abs(truck.Heading-ctx.Locations.Heading("M30.Start"))<.01f,"M30 uses the authored starting vehicle heading");
   ctx.Cutscenes.Skip();Check(ice.IsInVehicle(truck)&&gohan.IsInVehicle(truck),"M30's opening keeps both passengers in their actual seats");
   PackageCall(ridge,"LaunchPursuit");Check(!ctx.Cutscenes.IsActive,"The gunship warning uses radio and never takes the driving camera");
   Check(Field<Blip>(ridge,"_gunshipBlip").Color==BlipColor.Red&&Function.Values.ContainsKey(Hash.SET_HELI_BLADES_FULL_SPEED),"The ridge gunship starts with spinning rotors and an enemy marker");
   PackageCall(ridge,"LoseGunship");
   Check(!Field<Blip>(ridge,"_gunshipBlip").Exists()&&Field<Vehicle>(ridge,"_heli").Exists(),"The canyon exit removes the gunship's threat marker without deleting its aircraft");
   truck.Position=ctx.Locations.Position("M29.Senora.Delivery");PackageCall(ridge,"UnloadParts");
   Check(!ridge.Unloaded&&ctx.State.CargoAt("satelliteParts")==null,"Starting the unload scene does not award parts or receiver intelligence");
   if(skip)ctx.Cutscenes.Skip();else WatchPackageScene(ctx);
   Check(ridge.Unloaded&&ridge.Parts.AttachedTo==Field<Prop>(ridge,"_bench")&&ice.IsInVehicle(truck)&&gohan.IsInVehicle(truck),"The same case reaches the table without ejecting either passenger");
   Use(roster,CrewSlot.Gohan);gohan.Task.LeaveVehicle();gohan.Position=ctx.Locations.Position("M23.ToolBay");
   PackageCall(ridge,"ReceiveSignal");if(skip)ctx.Cutscenes.Skip();else WatchPackageScene(ctx);
   Check(ridge.Received&&ctx.State.CargoAt("satelliteParts")==null,"Receiver check completes physically; persistent cargo still waits for mission success");
   ridge.Pass();Check(ridge.Status==MissionStatus.Passed&&ctx.State.CargoAt("satelliteParts")=="M23.ToolBay"&&ridge.Parts.Exists(),"Only a verified successful M30 stores the receiver parts and leaves them at the workbench");
  }
  Reset();roster=Roster();ctx=Context(roster);var lost=new M30RedlineRidge();lost.Begin(ctx);ctx.Cutscenes.Skip();lost.Parts.Detach();lost.Tick();
  Check(lost.Status==MissionStatus.Failed&&lost.FailReason.Contains("parts came off"),"M30 fails explicitly if its physical cargo detaches en route");
  Reset();roster=Roster();ctx=Context(roster);lost=new M30RedlineRidge();lost.Begin(ctx);ctx.Cutscenes.Skip();PackageCall(lost,"UnloadParts");ctx.Cutscenes.Stop();
  Check(!lost.Unloaded&&!lost.Received,"Canceling M30's cargo transfer cannot claim delivery or receiver access");lost.Abort();
  Check(World.Props.All(p=>!p.Exists()),"Canceling M30 removes its temporary case and receiving equipment");
 }
}
