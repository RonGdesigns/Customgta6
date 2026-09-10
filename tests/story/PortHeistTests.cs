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
  var hull=m19.Hull;var authored=c.Locations.Position("M19.HullBreach");
  Check(hull!=null&&hull.Present&&hull.Model.Name==M19UnderwaterBreach.HullModel&&hull.IsPositionFrozen&&hull.IsInvincible&&!hull.IsEngineRunning,"A frozen tug stands in for the Titan Star at the surface");
  Check(Math.Abs(hull.Position.X-authored.X)<0.01f&&Math.Abs(hull.Position.Y-authored.Y)<0.01f&&Math.Abs(hull.Position.Z)<0.01f,"With deep water the hull sits on the surface over the authored breach");
  Check(Math.Abs(m19.Breach.Z-(-(M19UnderwaterBreach.KeelDepth+M19UnderwaterBreach.SubClearance)))<0.01f&&GameUtils.IsWithinFlat(m19.Breach,hull.Position,0.1f),"The work point is under the keel with room for the Kraken");
  Check(m19.Clamps.Count==2&&m19.Clamps.All(p=>Math.Abs(p.Z-m19.Breach.Z)<0.01f&&Math.Abs(p.DistanceTo(m19.Breach)-M19UnderwaterBreach.ClampSpan)<0.05f)&&m19.Clamps[0].DistanceTo(m19.Clamps[1])>2*M19UnderwaterBreach.ClampSpan-0.1f,"The clamps are under the bow and the stern at the same depth");
  m19.Pass();Check(m19.Status==MissionStatus.Passed&&hull.Present&&hull.Released,"Passing leaves the hull in the water for the lift chapter");
  var toM20=c.Handoffs.Peek("PortHeist","M20");Check(toM20!=null&&toM20.Notes.ContainsKey("hull"),"The record notes where the hull is");
  // Shallow water: the site walks out along the approach bearing until there is room.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"heist-m19b.json"));Function.Seabed=-6f;var shallow=new M19UnderwaterBreach();
  Check(shallow.Begin(c)&&shallow.Hull.Position.DistanceTo(new Vector3(authored.X,authored.Y,0f))>4*M19UnderwaterBreach.ShiftStep-0.5f,"A seabed with no room under the keel moves the whole site out to deeper water");
  Check(shallow.Clamps.All(p=>GameUtils.IsWithinFlat(p,shallow.Hull.Position,M19UnderwaterBreach.ClampSpan+0.1f)),"The clamps move with the hull");Function.Seabed=-40f;
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"heist-m19c.json"));Function.SeabedKnown=false;
  Check(new M19UnderwaterBreach().Begin(c),"An unknown seabed does not refuse the mission");Function.SeabedKnown=true;

  // ---- M20 takes over the Kraken chapter one left floating.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"heist-m20.json"));
  var floating=new Vehicle{Model=new Model("submersible2"),Position=c.Locations.Position("M19.Surface")};World.Vehicles.Add(floating);World.NearbyVehicles=new[]{floating};
  var record=OperationHandoff.Capture("PortHeist","M19","M20",crew,floating);c.Handoffs.Record(record);
  int subs=World.Vehicles.Count(v=>v.Model.Name=="submersible2");var m20=new M20SkyHook();Check(m20.Begin(c),"M20 starts from the record");
  Check(World.Vehicles.Count(v=>v.Model.Name=="submersible2")==subs&&crew.PedFor(CrewSlot.Gohan).CurrentVehicle==floating,"Gohan is put back into the same Kraken, not a second one");m20.Abort();
  World.NearbyVehicles=new Vehicle[0];

  // ---- The chapters continue into one another as one operation.
  Reset();crew=Roster();c=Context(crew);var state=CampaignState.Load(Path.Combine(root,"heist-chain.json"));state.Completed.Add("SM01");state.Completed.Add("SM02");state.Completed.Add("SM03");var cat=new MissionCatalog();
  var m19def=new MissionDefinition{Info=new MissionInfo{Id="M19",Title="Breach"},Factory=()=>new ChapterProbe("M19")};
  var m20def=new MissionDefinition{Info=new MissionInfo{Id="M20",Title="Sky Hook",Prerequisite="M19"},Factory=()=>new ChapterProbe("M20")};
  var m21def=new MissionDefinition{Info=new MissionInfo{Id="M21",Title="Open Water",Prerequisite="M20"},Factory=()=>new ChapterProbe("M21")};
  cat.All.Add(m19def);cat.All.Add(m20def);cat.All.Add(m21def);
  var manager=new MissionManager(c,state,cat);
  Check(MissionManager.Continuations["M19"]=="M20"&&MissionManager.Continuations["M21"]=="M22"&&!MissionManager.Continuations.ContainsKey("M22"),"The Port Heist chapters are declared as one operation ending at M22");
  Check(manager.Start(m19def)&&manager.IsRunning,"Chapter one starts");
  c.Cutscenes.Skip();manager.Update();Check(manager.CurrentStage==0,"Chapter one's briefing hands over to its gameplay");
  manager.ForcePass();manager.Update();
  Check(state.IsComplete("M19")&&manager.PendingContinuation==m20def&&GameUtils.Message.Contains("operation continues")&&manager.IsRunning,"Passing chapter one commits it and queues chapter two instead of ending the operation");
  c.Cutscenes.Stop();manager.Update();
  Check(manager.PendingContinuation==null&&manager.IsRunning&&manager.LastAttempted==m20def,"Chapter two starts on its own once the aftermath is over");
  c.Cutscenes.Stop();manager.Update();Check(manager.CurrentStage==0,"Chapter two's briefing hands over to its gameplay");
  manager.ForceFail("The lift went down.");manager.Update();
  Check(!manager.IsRunning&&manager.RetryAvailable&&manager.PendingContinuation==null&&manager.LastAttempted==m20def,"A failed chapter stops the operation and retries that chapter alone");
  Check(manager.Start(m20def)&&manager.IsRunning,"The retry starts chapter two again");manager.Abort();
  Check(manager.PendingContinuation==null,"Abort clears any queued chapter");
  var solo=new MissionDefinition{Info=new MissionInfo{Id="M03",Title="Foundry"},Factory=()=>new ChapterProbe("M03")};cat.All.Add(solo);
  Check(manager.Start(solo),"An ordinary job starts");c.Cutscenes.Skip();manager.Update();manager.ForcePass();manager.Update();
  Check(manager.PendingContinuation==null&&!GameUtils.Message.Contains("continues"),"An ordinary job still ends with the pass notice and no continuation");
 }
}
