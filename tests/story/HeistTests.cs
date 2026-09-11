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
 static void HeistChecks()
 {
  // ---- M19: the sub where it was staged, Ice on the pier, Ron in the lift; the floats as real parts; the container surfaced by the finished work; everything recorded.
  Reset();var crew=Roster();var c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"heist19.json"));c.State.SetCargo("kraken","M18.ChannelMark");c.State.SetCargo("radarPod","M18.SaltHangar");var m19=new M19UnderwaterBreach();
  Check(m19.Begin(c)&&c.Cutscenes.IsActive&&m19.EndpointKind==MissionEndpoint.ContinuousNext,"M19 opens on a cut into the staged operation and continues into M20");
  var channel=c.Locations.Position("M18.ChannelMark");
  Check(m19.StagedAt=="M18.ChannelMark"&&Math.Abs(m19.Kraken.Position.X-channel.X)<0.01f&&Math.Abs(m19.Kraken.Position.Y-channel.Y)<0.01f,"The Kraken is where M18 left it, not at a fresh dive mark");
  Check(m19.Lift!=null&&crew.PedFor(CrewSlot.Guess).IsInVehicle(m19.Lift)&&!m19.Lift.IsEngineRunning&&crew.PedFor(CrewSlot.Ice).Position==c.Locations.Position("M12.PierWatch"),"Ron sits in the lift at the hangar with the engine off; Ice holds the pier");
  c.Cutscenes.Skip();m19.Tick();Use(crew,CrewSlot.Gohan);Game.Player.Character.SetIntoVehicle(m19.Kraken,VehicleSeat.Driver);m19.Tick();Check(m19.CurrentStage==1,"In the sub, the breach is the job");
  Interact(m19,c,CrewSlot.Gohan,m19.Breach,16,afloat:true);Check(m19.CurrentStage==2&&!m19.Floated,"The breach cut, the clamps are next and nothing has surfaced");
  foreach(var clamp in m19.Clamps)Interact(m19,c,CrewSlot.Gohan,clamp,8,afloat:true);
  Check(m19.Floats.Count==2&&m19.Floats.All(f=>f.Exists()&&f.IsPositionFrozen),"Each clamp leaves a real float at its site under the keel");
  m19.Tick();m19.Tick();Check(m19.CurrentStage==3&&m19.Floated&&c.Cutscenes.IsActive&&m19.Container!=null,"The last clamp brings the container up as a scene, not a line");
  c.Cutscenes.Skip();m19.Tick();var floatPoint=PortHeist.ContainerPoint(c.Locations);
  Check(Math.Abs(m19.Container.Position.X-floatPoint.X)<0.01f&&Math.Abs(m19.Container.Position.Y-floatPoint.Y)<0.01f&&m19.Container.IsPositionFrozen&&m19.Floats.All(f=>f.AttachedTo==m19.Container),"Skipped or watched, the container floats beside the mark with both floats on it");
  m19.Kraken.Position=c.Locations.Position("M19.Surface");Game.Player.Character.Position=m19.Kraken.Position;m19.Tick();c.Dialogue.Clear();m19.Tick();
  Check(m19.Status==MissionStatus.Passed&&c.State.CargoAt("kraken")=="M19.Surface"&&c.State.CargoAt(PortHeist.BullionCargo)=="M19.Surface","Surfaced at the support mark: the sub and the container are recorded there");
  var toM20=c.Handoffs.Peek(PortHeist.Operation,"M20");
  Check(toM20!=null&&toM20.Notes["container"].Contains("floating")&&toM20.Notes["lift"].Contains("Ron in the Cargobob")&&toM20.Notes.ContainsKey("hull"),"The record says where the container, the lift and the hull are");
  m19.Cleanup();Check(m19.Kraken.Exists()&&m19.Lift.Exists()&&m19.Container.Exists()&&m19.Hull.Exists(),"The sub, the lift, the container and the freighter are the next chapter's");

  // ---- M20: the same lift, the same sub, the same container; the pod's one job; the hook as an insert; the launch boarded with real seats before M21.
  World.NearbyVehicles=new[]{m19.Kraken,m19.Lift};var m20=new M20SkyHook();
  Check(m20.Begin(c)&&c.Cutscenes.IsActive&&m20.EndpointKind==MissionEndpoint.ContinuousNext,"M20 opens on the live handoff and continues into M21");
  Check(m20.Cargobob==m19.Lift&&m20.Kraken==m19.Kraken&&crew.PedFor(CrewSlot.Gohan).IsInVehicle(m19.Kraken)&&m20.FollowingRecord&&m20.PodLive,"Ron flies the lift he sat in, Gohan is in the sub he surfaced, the pod from M18 is on the aircraft");
  Check(Math.Abs(m20.Container.Position.X-floatPoint.X)<0.01f&&Math.Abs(m20.Container.Position.Y-floatPoint.Y)<0.01f&&m20.Launch!=null&&!m20.Launch.IsEngineRunning,"The container is hooked where M19 floated it; the escort launch waits at its mark");
  c.Cutscenes.Skip();m20.Tick();Use(crew,CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(m20.Cargobob,VehicleSeat.Driver);m20.Tick();
  Check(m20.CurrentStage==1&&m20.PodCalled&&c.Dialogue.HasPending&&crew.PedFor(CrewSlot.Gohan).Task.BoatTasks==1,"Lifting off switches the pod on over the radio and sends Gohan's sub to the pier");
  foreach(var g in World.Created.Where(p=>p.Model.Name=="s_m_y_blackops_01"))g.IsDead=true;Use(crew,CrewSlot.Ice);c.Dialogue.Clear();m20.Tick();Check(m20.CurrentStage==2,"The quay clear, the hover is Ron's");
  var hover=new Vector3(m20.Container.Position.X,m20.Container.Position.Y,c.Locations.Position("M20.HoverPoint").Z);GTA.Native.Function.Calls.Clear();
  Use(crew,CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(m20.Cargobob,VehicleSeat.Driver);Interact(m20,c,CrewSlot.Guess,hover,8,afloat:true);
  Check(m20.CurrentStage==3&&m20.Hooked&&c.Cutscenes.IsActive&&GTA.Native.Function.Calls.Any(call=>call.Item1==GTA.Native.Hash.ATTACH_ENTITY_TO_ENTITY)&&Math.Abs(m20.Cargobob.EnginePowerMultiplier-0.55f)<0.001f,"The hover earns the hook: the container attached as an insert and the aircraft flying heavy");
  c.Cutscenes.Skip();m20.Tick();m20.Cargobob.Position=c.Locations.Position("M20.ClimbOut");Game.Player.Character.Position=m20.Cargobob.Position;m20.Tick();
  Check(m20.Transferred&&c.Cutscenes.IsActive,"The climb-out done, the transfer to the launch plays as a scene");
  c.Cutscenes.Skip();m20.Tick();
  Check(crew.PedFor(CrewSlot.Gohan).IsInVehicle(m20.Launch)&&crew.PedFor(CrewSlot.Gohan).SeatIndex==VehicleSeat.Driver&&crew.PedFor(CrewSlot.Ice).IsInVehicle(m20.Launch)&&crew.PedFor(CrewSlot.Ice).SeatIndex==VehicleSeat.Passenger,"Skipped or watched, Gohan is at the helm and Ice in the other seat: actual seats");
  c.Dialogue.Clear();m20.Tick();c.Dialogue.Clear();m20.Tick();
  var toM21=c.Handoffs.Peek(PortHeist.Operation,"M21");
  Check(m20.Status==MissionStatus.Passed&&toM21!=null&&toM21.CargoAttached&&toM21.Notes["launch"].Contains("helm")&&toM21.Notes["radarPod"].Contains("live")&&c.State.CargoAt("kraken")=="M12.PierWatch","M20 passes with the lift loaded, the escort crewed and the Kraken's storage recorded");
  m20.Cleanup();Check(m20.Launch.Exists()&&m20.Container.Exists()&&m20.Kraken.Exists(),"The launch, the container and the sub survive the chapter");

  // ---- M21: the launch and the lift carried on; M13 and M15 felt in the harbor; the split announced; the shore landing and the Granger boarded on camera.
  World.NearbyVehicles=new[]{m20.Launch,m20.Cargobob};c.State.SetUpgrade("harborPatrolsReduced",true);c.State.SetUpgrade("harborGateAccess",true);c.Vans=new CrewVan(c.State,c.Locations);var m21=new M21OpenWater();
  Check(m21.Begin(c)&&c.Cutscenes.IsActive&&m21.EndpointKind==MissionEndpoint.ContinuousNext,"M21 opens on the escort seen and continues into M22");
  Check(m21.Launch==m20.Launch&&m21.Cargobob==m20.Cargobob&&m21.Granger!=null&&!m21.Granger.IsEngineRunning&&m21.Cargobob.IsPositionFrozen,"The same launch and the same lift; the Granger staged at the road; the lift held until the escort moves");
  Check(m21.PatrolsReduced&&m21.GateAccess,"The harbor upgrades from M13 and M15 are read");
  c.Cutscenes.Skip();m21.Tick();Use(crew,CrewSlot.Gohan);Game.Player.Character.SetIntoVehicle(m21.Launch,VehicleSeat.Driver);m21.Tick();
  Check(m21.CurrentStage==1&&m21.HarborReported&&c.Dialogue.HasPending&&!m21.Cargobob.IsPositionFrozen,"On the water, the harbor as the preparation left it is said and the lift is released");
  Check(m21.HostileCrews.Count==4&&World.Vehicles.Where(v=>v.Model.Name=="predator").All(v=>v.Position.Y>c.Locations.Position("M21.Breakwater").Y+80f),"M13's burn leaves two launches, and M15's gate holds them beyond the breakwater");
  for(int i=0;i<20&&m21.CurrentStage==1;i++){Game.Player.Character.Position=m21.Cargobob.Position;m21.Launch.Position=m21.Cargobob.Position;c.Dialogue.Clear();Game.GameTime+=1000;m21.Tick();}
  Check(m21.CurrentStage==2,"On the lift's wing, the boats are the job");
  foreach(var p in m21.HostileCrews)p.IsDead=true;c.Dialogue.Clear();m21.Tick();Check(m21.CurrentStage==3,"The boats down, the breakwater is the exit");
  int heliTasks=crew.PedFor(CrewSlot.Guess).Task.HeliTasks;m21.Launch.Position=c.Locations.Position("M21.Breakwater");Game.Player.Character.Position=m21.Launch.Position;c.Dialogue.Clear();m21.Tick();
  Check(m21.CurrentStage==4&&m21.Split&&crew.PedFor(CrewSlot.Guess).Task.HeliTasks==heliTasks+1,"Through the breakwater the split is announced: Ron's lift is tasked north, the launch turns for the shore");
  m21.Launch.Position=c.Locations.Position("M21.ShoreLanding");Game.Player.Character.Position=m21.Launch.Position;c.Dialogue.Clear();m21.Tick();
  Check(m21.Transferred&&c.Cutscenes.IsActive,"The launch on the shore, the road transfer plays as a scene");
  c.Cutscenes.Skip();m21.Tick();
  Check(crew.PedFor(CrewSlot.Gohan).IsInVehicle(m21.Granger)&&crew.PedFor(CrewSlot.Gohan).SeatIndex==VehicleSeat.Driver&&crew.PedFor(CrewSlot.Ice).IsInVehicle(m21.Granger)&&crew.PedFor(CrewSlot.Ice).SeatIndex==VehicleSeat.Passenger,"Skipped or watched, Gohan drives the Granger and Ice rides beside him: no boat on an inland lake");
  c.Dialogue.Clear();m21.Tick();c.Dialogue.Clear();m21.Tick();
  var toM22=c.Handoffs.Peek(PortHeist.Operation,"M22");
  Check(m21.Status==MissionStatus.Passed&&toM22!=null&&toM22.CargoAttached&&toM22.Notes["granger"].Contains("driving")&&toM22.Notes["harbor"].Contains("thinned by M13"),"M21 passes with the lift loaded and the road transport recorded");
  m21.Cleanup();Check(m21.Granger.Exists()&&m21.Launch.Exists(),"The Granger and the launch survive the chapter");

  // ---- M22: each brother's arrival, the drop as an insert, a real regroup, the strike learned first, the keys in Ron's hand, the ledger recorded once.
  var m22=new M22ScorchedBay();
  Check(m22.Begin(c)&&c.Cutscenes.IsActive&&m22.EndpointKind==MissionEndpoint.SecuredDelivery,"M22 opens on the arrivals, not a plan");
  Check(m22.Granger!=null&&crew.PedFor(CrewSlot.Gohan).IsInVehicle(m22.Granger)&&crew.PedFor(CrewSlot.Gohan).SeatIndex==VehicleSeat.Driver&&crew.PedFor(CrewSlot.Ice).IsInVehicle(m22.Granger)&&crew.PedFor(CrewSlot.Guess).IsInVehicle(m22.Cargobob)&&m22.Cargobob.IsPositionFrozen&&m22.Cargobob.HeightAboveGround==0f||m22.Cargobob.Position.Z>c.Locations.Position("M22.AlamoDrop").Z+30f,"Ice and Gohan arrive in the Granger, Ron in the held lift over the water");
  Check(c.Handoffs.Peek(PortHeist.Operation,"M22")==null,"M22 consumed the M21 record");
  c.Cutscenes.Skip();m22.Tick();Use(crew,CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(m22.Cargobob,VehicleSeat.Driver);m22.Tick();
  Check(m22.Arrived&&m22.CurrentStage==1&&!m22.Cargobob.IsPositionFrozen,"Everyone arrived, the lift is released and the drop is the job");
  var drop=c.Locations.Position("M22.AlamoDrop");Interact(m22,c,CrewSlot.Guess,drop+new Vector3(0,0,20),3,afloat:true);
  Check(m22.Dropped&&m22.CargoRecorded&&c.Cutscenes.IsActive&&c.State.CargoAt(PortHeist.BullionCargo)=="M22.AlamoDrop"&&m22.Container.Position==drop&&m22.Container.IsPositionFrozen,"The drop plays as an insert and the hidden cargo is recorded in the shallows");
  c.Cutscenes.Skip();m22.Tick();m22.Cargobob.Position=c.Locations.Position("M22.Beach");m22.Cargobob.HeightAboveGround=0f;m22.Cargobob.Speed=0f;Game.Player.Character.Position=m22.Cargobob.Position;c.Dialogue.Clear();m22.Tick();
  Check(m22.Landed&&m22.CurrentStage==3&&!m22.Cargobob.IsEngineRunning,"Landed and shut down, the regroup is on foot");
  Game.Player.Character.Task.LeaveVehicle();Game.Player.Character.CurrentVehicle=null;Game.Player.Character.Position=c.Locations.Position("M22.Beach")+new Vector3(-6,6,0);c.Dialogue.Clear();m22.Tick();
  Check(m22.Struck&&c.Cutscenes.IsActive&&m22.Keys!=null&&m22.Keys.Model.Name==M22ScorchedBay.KeysModel,"Reaching the others, the strike arrives as a scene with the keys from M03 in it");
  c.Cutscenes.Skip();m22.Tick();
  Check(m22.Keys.AttachedTo==m22.Granger,"Skipped or watched, the keys end up in the Granger that is taking them to Senora");
  c.Dialogue.Clear();m22.Tick();c.Dialogue.Clear();m22.Tick();c.Dialogue.Clear();m22.Tick();
  Check(m22.Status==MissionStatus.Passed&&c.State.CargoAt("cargobob")=="M22.Beach","M22 passes with the lift left on the beach");
  m22.Cleanup();Check(m22.Container.Exists()&&m22.Container.IsPositionFrozen&&m22.Granger.Exists(),"The container stays in the shallows; the Granger is the way out");
  // A replay after the first pass does not move the cargo record or reset the salvage ledger.
  var cat=new MissionCatalog();cat.All.Add(Def("M22","main"));c.State.MarkComplete("M22",cat);float dredged=c.State.AlamoGoldDredgedTons=7f;c.State.SetCargo(PortHeist.BullionCargo,"M24.Dredge");
  World.NearbyVehicles=new Vehicle[0];var replay=new M22ScorchedBay();Check(replay.Begin(c),"A replay of M22 starts");c.Cutscenes.Skip();replay.Tick();Use(crew,CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(replay.Cargobob,VehicleSeat.Driver);replay.Tick();
  Interact(replay,c,CrewSlot.Guess,drop+new Vector3(0,0,20),3,afloat:true);
  Check(replay.Dropped&&!replay.CargoRecorded&&c.State.CargoAt(PortHeist.BullionCargo)=="M24.Dredge","The replayed drop leaves the recorded cargo where the later ledger put it");
  c.State.MarkComplete("M22",cat);Check(c.State.AlamoGoldDredgedTons==dredged,"Completing M22 again does not reset the dredging ledger");replay.Abort();
  string m22src=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act1","M22ScorchedBay.cs"));
  Check(m22src.Contains("DialogueAfterStep = 2")&&m22src.IndexOf("new UsePhoneStep(guess")<m22src.IndexOf("Detonate(foundry))")&&m22src.IndexOf("Detonate(foundry))")<m22src.IndexOf("new CarryPropStep(guess, _keys)"),"The strike is learned from the phone and seen before any line explains it; the keys come after");
  string m03src=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act1","M03CypressFoundry.cs"));
  Check(m03src.Contains("KeysModel = \"p_car_keys_01\"")&&m03src.Contains("new StowPropStep(guess, keys, _hauler")&&m22src.Contains("KeysModel = \"p_car_keys_01\""),"The keys are the same object in M03 and M22");
  string m21src=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act1","M21OpenWater.cs"));
  Check(m21src.Contains("int count = _patrolsReduced ? 2 : 3;")&&m21src.Contains("float standoff = _gateAccess ? 90f : 20f;"),"M13 and M15 change what comes at the launch, not only a line");
  var scenes=File.ReadAllLines(Path.Combine(dataDir,"scenes.tsv"));
  Check(new[]{"M19","M20","M21","M22"}.All(id=>scenes.Count(l=>l.StartsWith(id+"_SCENE_APPROACH"))==2),"Each chapter's approach has its authored lines");

  // ---- Cold starts: no record, no upgrades. Each chapter still builds a believable state.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"heist-cold.json"));var cold19=new M19UnderwaterBreach();
  Check(cold19.Begin(c)&&cold19.StagedAt=="M19.DiveStart"&&cold19.Lift!=null,"With nothing staged, M19 puts the sub at the dive mark and still shows the lift");cold19.Abort();
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"heist-cold20.json"));var cold20=new M20SkyHook();
  Check(cold20.Begin(c)&&!cold20.FollowingRecord&&!cold20.PodLive&&Math.Abs(cold20.Container.Position.X-c.Locations.Position("M20.HoverPoint").X)<0.01f,"With no record, M20 puts the container at the hover point and has no pod to switch on");
  c.Cutscenes.Skip();cold20.Tick();Use(crew,CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(cold20.Cargobob,VehicleSeat.Driver);cold20.Tick();
  Check(cold20.PodCalled&&c.Dialogue.HasPending,"No pod is said, not skipped");cold20.Abort();
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"heist-cold21.json"));var cold21=new M21OpenWater();
  Check(cold21.Begin(c)&&!cold21.PatrolsReduced&&!cold21.GateAccess,"Without M13 and M15 the harbor is at full strength");
  c.Cutscenes.Skip();cold21.Tick();Use(crew,CrewSlot.Gohan);Game.Player.Character.SetIntoVehicle(cold21.Launch,VehicleSeat.Driver);cold21.Tick();
  Check(cold21.CurrentStage==1&&cold21.HostileCrews.Count==6&&World.Vehicles.Where(v=>v.Model.Name=="predator").All(v=>v.Position.Y<c.Locations.Position("M21.Breakwater").Y+30f),"Three launches at the breakwater when nothing thinned them or opened the gate");cold21.Abort();
 }
}
