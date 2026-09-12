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

public sealed class ChapterProbe : Mission
{
 private readonly string _id; public ChapterProbe(string id){_id=id;}
 public override string Id=>_id;public override string Title=>"Chapter "+_id;protected override bool OnStart()=>true;protected override void OnUpdate(){}
}

public static partial class StoryTests
{
 static void PortHeistChecks()
 {
  // ---- The dive site is built from a hull the Kraken can work under.
  Reset();var crew=Roster();var c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"heist-m19.json"));var m19=new M19UnderwaterBreach();
  Check(m19.Begin(c),"M19 sets up");
  var hull=m19.Hull;var authored=c.Locations.Position("M19.Worksite");
  Check(hull!=null&&hull.Present&&hull.Model.Name==M19UnderwaterBreach.HullModel&&hull.IsPositionFrozen&&hull.IsInvincible&&!hull.IsEngineRunning,"A frozen tug stands in for the Titan Star at the surface");
  Check(Math.Abs(hull.Position.X-authored.X)<0.01f&&Math.Abs(hull.Position.Y-authored.Y)<0.01f&&Math.Abs(hull.Position.Z)<0.01f,"With deep water the hull sits on the surface over the authored breach");
  Check(Math.Abs(m19.Breach.Z-(-(M19UnderwaterBreach.KeelDepth+M19UnderwaterBreach.SubClearance)))<0.01f&&m19.Breach.X>hull.Position.X+6f,"The work point is beside the visible cargo and outside solid hull collision");
  Check(m19.Clamps.Count==2&&m19.Clamps.All(p=>Math.Abs(p.Z-m19.Breach.Z)<0.01f&&p.DistanceTo(m19.Breach)>2f)&&m19.Clamps[0].DistanceTo(m19.Clamps[1])>5f,"The two reachable clamps follow the cargo dimensions at a shared depth");
  m19.Pass();Check(m19.Status==MissionStatus.Passed&&hull.Present&&hull.Released,"Passing leaves the hull in the water for the lift chapter");
  var toM20=c.Handoffs.Peek("PortHeist","M20");Check(toM20!=null&&toM20.Notes.ContainsKey("hull"),"The record notes where the hull is");
  // Shallow water: the site walks out along the approach bearing until there is room.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"heist-m19b.json"));Function.Seabed=-6f;var shallow=new M19UnderwaterBreach();
  Check(!shallow.Begin(c),"A whole search area too shallow for the sub is rejected, not accepted after arbitrary shifts");Function.Seabed=-40f;
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"heist-m19c.json"));Function.SeabedKnown=false;
  Check(new M19UnderwaterBreach().Begin(c)&&Script.Waited>=8,"A seabed that has not streamed in is waited for, then treated as open water: the mission starts rather than refusing");Function.SeabedKnown=true;

  // ---- M20 takes over the Kraken chapter one left floating.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"heist-m20.json"));
  var floating=new Vehicle{Model=new Model("submersible2"),Position=c.Locations.Position("M19.Surface")};World.Vehicles.Add(floating);World.NearbyVehicles=new[]{floating};
  var record=OperationHandoff.Capture("PortHeist","M19","M20",crew,floating);c.Handoffs.Record(record);
  int subs=World.Vehicles.Count(v=>v.Model.Name=="submersible2");var m20=new M20SkyHook();Check(m20.Begin(c),"M20 starts from the record");
  Check(World.Vehicles.Count(v=>v.Model.Name=="submersible2")==subs&&crew.PedFor(CrewSlot.Gohan).CurrentVehicle==floating,"Gohan is put back into the same Kraken, not a second one");m20.Abort();
  World.NearbyVehicles=new Vehicle[0];

  // ---- Other campaign chains keep their existing behavior; the Port Heist is a parent operation.
  Reset();crew=Roster();c=Context(crew);var state=CampaignState.Load(Path.Combine(root,"heist-chain.json"));state.Completed.Add("SM04");state.Completed.Add("SM05");state.Completed.Add("SM06");var cat=new MissionCatalog();
  var m44def=new MissionDefinition{Info=new MissionInfo{Id="M44",Title="Breach"},Factory=()=>new ChapterProbe("M44")};
  var m45def=new MissionDefinition{Info=new MissionInfo{Id="M45",Title="Sky Hook",Prerequisite="M44"},Factory=()=>new ChapterProbe("M45")};
  var m46def=new MissionDefinition{Info=new MissionInfo{Id="M46",Title="Open Water",Prerequisite="M45"},Factory=()=>new ChapterProbe("M46")};
  cat.All.Add(m44def);cat.All.Add(m45def);cat.All.Add(m46def);
  var manager=new MissionManager(c,state,cat);
  Check(!MissionManager.Continuations.ContainsKey("M19")&&!MissionManager.Continuations.ContainsKey("M20")&&!MissionManager.Continuations.ContainsKey("M21")&&!MissionManager.Continuations.ContainsKey("M22"),"The Port Heist is not implemented as four automatically restarted missions");
  Check(manager.Start(m44def)&&manager.IsRunning,"Chapter one starts");
  c.Cutscenes.Skip();manager.Update();Check(manager.CurrentStage==0,"Chapter one's briefing hands over to its gameplay");
  manager.ForcePass();manager.Update();
  Check(state.IsComplete("M44")&&manager.PendingContinuation==m45def&&GameUtils.Message.Contains("operation continues")&&manager.IsRunning,"Passing chapter one commits it and queues chapter two instead of ending the operation");
  c.Cutscenes.Stop();manager.Update();
  Check(manager.PendingContinuation==null&&manager.IsRunning&&manager.LastAttempted==m45def,"Chapter two starts on its own once the aftermath is over");
  c.Cutscenes.Skip();manager.Update();Check(manager.CurrentStage==0,"Chapter two's briefing hands over to its gameplay");
  manager.ForceFail("The lift went down.");manager.Update();
  Check(!manager.IsRunning&&manager.RetryAvailable&&manager.PendingContinuation==null&&manager.LastAttempted==m45def,"A failed chapter stops the operation and retries that chapter alone");
  Check(manager.Start(m45def)&&manager.IsRunning,"The retry starts chapter two again");manager.Abort();
  Check(manager.PendingContinuation==null,"Abort clears any queued chapter");
  var solo=new MissionDefinition{Info=new MissionInfo{Id="M03",Title="Foundry"},Factory=()=>new ChapterProbe("M03")};cat.All.Add(solo);
  Check(manager.Start(solo),"An ordinary job starts");c.Cutscenes.Skip();manager.Update();manager.ForcePass();manager.Update();
  Check(manager.PendingContinuation==null&&!GameUtils.Message.Contains("continues"),"An ordinary job still ends with the pass notice and no continuation");
 }
}
