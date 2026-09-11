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
 static void HarborChecks()
 {
  // ---- M12: the hold and the craft seen, the scan as work, the launches shown, the survey recorded.
  Reset();var crew=Roster();var c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"harbor12.json"));Function.ClearLos=false;var m12=new M12BlackTideRecon();
  Check(m12.Begin(c)&&c.Cutscenes.IsActive&&m12.Rov!=null&&m12.EndpointKind==MissionEndpoint.SecuredDelivery,"M12 opens on the sub, the freighter and the pier; a secured delivery, no police involved");
  c.Cutscenes.Skip();m12.Tick();Use(crew,CrewSlot.Gohan);Game.Player.Character.SetIntoVehicle(m12.Rov,VehicleSeat.Driver);m12.Tick();Check(m12.CurrentStage==1,"In the sub, the dive is the job");
  m12.Rov.Position=c.Locations.Position("M12.SonarBuoy");Game.Player.Character.Position=m12.Rov.Position;m12.Tick();Check(m12.CurrentStage==2,"Under the sonar, the hull is the job");
  GTA.UI.Screen.Subtitle=null;Interact(m12,c,CrewSlot.Gohan,c.Locations.Position("M12.FreighterHull"),14,true);
  Check(m12.CurrentStage==3&&m12.Surveyed&&c.Cutscenes.IsActive,"The scan plays as work at the hull from Gohan's own line");
  c.Cutscenes.Skip();m12.Tick();Check(m12.PatrolsShown&&c.Cutscenes.IsActive,"The launches are shown once as the next problem");
  c.Cutscenes.Skip();m12.Rov.Position=c.Locations.Position("M12.SouthJetty")+new Vector3(0,-8,-4);Game.Player.Character.Position=m12.Rov.Position;m12.Tick();c.Dialogue.Clear();m12.Tick();
  Check(m12.Status==MissionStatus.Passed&&c.State.EvidenceOf("hullSurvey")==EvidenceState.CopyHeld&&m12.OutroBlocking()!=null,"Back at the jetty the survey is recorded: reachable hull, not ready heist");

  // ---- M13: the basin seen, workers not Aegis, the alarm as the reason to leave, charges dark until aboard, patrols reduced.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"harbor13.json"));c.Vans=new CrewVan(c.State,c.Locations);Function.ClearLos=false;var m13=new M13SmugglersCut();
  Check(m13.Begin(c)&&c.Cutscenes.IsActive&&m13.Kayak!=null&&m13.Granger!=null&&m13.EndpointKind==MissionEndpoint.EscapeCheckpoint,"M13 opens on the barges, the slipway and the crew's own Granger; an escape checkpoint");
  var workers=World.Created.Where(p=>p.Model.Name=="s_m_m_dockwork_01").ToList();string m13src=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act1","M13SmugglersCut.cs"));Check(workers.Count==3&&m13src.Contains("var workers = World.AddRelationshipGroup(\"BLOODLINES_TRAFFIC\");"),"The slipway men are dock workers, not a cartel fight");
  c.Cutscenes.Skip();m13.Tick();Use(crew,CrewSlot.Ice);Game.Player.Character.SetIntoVehicle(m13.Kayak,VehicleSeat.Driver);m13.Tick();Check(m13.CurrentStage==1,"On the water, the barges are the job");
  GameUtils.LastProgress=-1f;foreach(var key in new[]{"M13.BargeOne","M13.BargeTwo","M13.BargeThree"}){Interact(m13,c,CrewSlot.Ice,c.Locations.Position(key),6,true);}m13.Tick();
  Check(m13.CurrentStage==2&&m13.AlarmBoat!=null&&!m13.AlarmShown,"Three charges planted with the meter; a launch turns into the basin behind them");
  m13.Tick();Check(m13.AlarmShown&&c.Cutscenes.IsActive,"The alarm is shown once as the reason to leave");
  c.Cutscenes.Skip();Game.Player.Character.Position=c.Locations.Position("M13.CanalSlipway");m13.Tick();Check(m13.CurrentStage==3&&!m13.Blown,"At the slipway the charges are still dark");
  GTA.UI.Screen.Subtitle=null;Game.Player.Character.SetIntoVehicle(m13.Granger,VehicleSeat.RightRear);Interact(m13,c,CrewSlot.Ice,m13.Granger.Position,1,true);
  Check(m13.Blown&&c.Cutscenes.IsActive&&c.State.FleetUpgrades["harborPatrolsReduced"],"Aboard, the charges blow and the basin burns as a result view; reduced patrols are recorded for M21");
  c.Cutscenes.Skip();m13.Tick();c.Dialogue.Clear();m13.Tick();Check(m13.Status==MissionStatus.Passed,"M13 passes with the crew in the Granger");

  // ---- M14: the pod and its destination seen, the ridge left by road, the pod recorded at McKenzie.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"harbor14.json"));var m14=new M14AirspaceBlackout();
  Check(m14.Begin(c)&&c.Cutscenes.IsActive&&m14.Plane!=null&&m14.Granger!=null&&m14.EndpointKind==MissionEndpoint.SecuredDelivery,"M14 opens on the aircraft, the apron and the ridge, with the crew's Granger on the ridge road");
  c.Cutscenes.Skip();m14.Tick();Use(crew,CrewSlot.Ice);foreach(var g in World.Created.Where(p=>p.Model.Name=="s_m_y_blackops_02"))g.IsDead=true;m14.Tick();Check(m14.CurrentStage==1,"The apron clear, Ron goes for the hangar");
  Use(crew,CrewSlot.Guess);Game.Player.Character.Position=c.Locations.Position("M14.HangarDoor");m14.Tick();Check(m14.CurrentStage==2,"At the hangar the aircraft is the job");
  Game.Player.Character.SetIntoVehicle(m14.Plane,VehicleSeat.Driver);m14.Tick();Check(m14.CurrentStage==3,"In the aircraft the low route begins");
  m14.Tick();Check(m14.RidgeLeft&&m14.Roles.For(CrewSlot.Ice).State==RoleState.Extracting&&m14.Roles.For(CrewSlot.Gohan).State==RoleState.Extracting&&c.Dialogue.HasPending,"Ice and Gohan leave the ridge for the Granger and say so; nobody shares a seat the plane lacks");
  m14.Plane.HeightAboveGround=0f;m14.Plane.Position=c.Locations.Position("M14.McKenzieHangar");Game.Player.Character.Position=m14.Plane.Position;m14.Plane.Speed=0f;m14.Tick();
  Check(m14.CurrentStage==4&&m14.PodStored&&c.Cutscenes.IsActive&&c.State.CargoAt("radarPod")=="M14.McKenzieHangar","Landed and stopped, the pod is shown under the wing and recorded at McKenzie");
  c.Cutscenes.Skip();m14.Tick();c.Dialogue.Clear();m14.Tick();Check(m14.Status==MissionStatus.Passed&&m14.Plane.Exists(),"M14 passes with the plane parked at McKenzie");

  // ---- M15: the access and the rounds seen, the splice as work, one gate answering, gate access recorded.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"harbor15.json"));c.Vans=new CrewVan(c.State,c.Locations);Function.ClearLos=false;var m15=new M15Crawlspace();
  Check(m15.Begin(c)&&c.Cutscenes.IsActive&&m15.Panel!=null&&m15.Granger!=null&&crew.PedFor(CrewSlot.Guess).IsInVehicle(m15.Granger)&&m15.EndpointKind==MissionEndpoint.EscapeCheckpoint,"M15 opens on the access, a watchman, a real cable point and Ron in the Granger at the exit");
  c.Cutscenes.Skip();m15.Tick();Use(crew,CrewSlot.Gohan);Game.Player.Character.Position=c.Locations.Position("M15.MaintenanceVault");m15.Tick();Check(m15.CurrentStage==1,"At the maintenance access the rounds are Ice's");
  Use(crew,CrewSlot.Ice);var rounds=World.Created.Where(p=>p.Model.Name=="s_m_m_security_01").ToList();foreach(var w in rounds)w.IsBeingStunned=true;m15.Tick();Check(m15.CurrentStage==2&&rounds.All(w=>w.IsAlive),"Three watchmen down and alive, the splice is Gohan's");
  GTA.UI.Screen.Subtitle=null;Interact(m15,c,CrewSlot.Gohan,c.Locations.Position("M15.FiberSplice"),12);
  Check(m15.CurrentStage==3&&m15.Tapped&&c.Cutscenes.IsActive,"The splice plays as work at the cable point from Gohan's own line");
  c.Cutscenes.Skip();m15.Tick();Check(m15.Acknowledged&&c.Dialogue.HasPending,"One gate answers: the acknowledgment, not a harbor gone dark");
  Game.Player.Character.Position=c.Locations.Position("M15.Exit");m15.Tick();c.Dialogue.Clear();m15.Tick();
  Check(m15.Status==MissionStatus.Passed&&c.State.FleetUpgrades["harborGateAccess"],"Out clean, the lock-gate access is recorded for M21");
  Function.ClearLos=true;
  var scenes=File.ReadAllLines(Path.Combine(dataDir,"scenes.tsv"));
  Check(new[]{"M12","M13","M14","M15"}.All(id=>scenes.Count(l=>l.StartsWith(id+"_SCENE_APPROACH"))==2),"Each preparation job's approach has two authored lines");
  string advanced=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Objectives","AdvancedObjectives.cs"));
  Check(!advanced.Contains("stay at this marker")&&advanced.Contains("DrawProgressBar"),"The multi-site hold shows the meter, not a countdown");
 }
}
