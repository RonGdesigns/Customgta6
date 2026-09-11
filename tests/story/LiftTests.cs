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
 static void LiftChecks()
 {
  // ---- M16: the unit on the dash seen, the clearance burned by a challenge, Ice aboard and Gohan by road, the lift landed and recorded.
  Reset();var crew=Roster();var c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"lift16.json"));c.State.SetCargo("iffTransponder","M09.Bunker");c.Vans=new CrewVan(c.State,c.Locations);var m16=new M16TheHeavyLift();
  Check(m16.Begin(c)&&c.Cutscenes.IsActive&&m16.Cargobob!=null&&m16.Granger!=null&&m16.Unit!=null&&m16.Unit.AttachedTo==m16.Granger&&m16.EndpointKind==MissionEndpoint.SecuredDelivery,"M16 opens on the unit on the Granger's dash, the Cargobob on the pad and Ice at the fence; a secured delivery");
  c.Cutscenes.Skip();m16.Tick();Use(crew,CrewSlot.Ice);Game.Player.Character.Position=c.Locations.Position("M16.Helipad");m16.Tick();
  Check(m16.CurrentStage==1&&m16.Challenged&&Game.Player.WantedLevel==4&&c.Dialogue.HasPending&&c.State.CargoAt("iffTransponder")==null,"Crossing the pad burns the clearance: a challenge on the net, the base's heat, the unit spent");
  foreach(var mp in World.Created.Where(p=>p.Model.Name=="s_m_y_marine_03"))mp.IsDead=true;m16.Tick();Check(m16.CurrentStage==2,"The pad clear, the Cargobob is Ron's");
  Use(crew,CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(m16.Cargobob,VehicleSeat.Driver);m16.Tick();
  Check(m16.CurrentStage==3&&m16.CrewMoved&&crew.PedFor(CrewSlot.Ice).Task.Enters==1&&crew.PedFor(CrewSlot.Ice).Task.LastSeat==VehicleSeat.RightFront&&crew.PedFor(CrewSlot.Gohan).Task.Enters==1&&crew.PedFor(CrewSlot.Gohan).Task.LastSeat==VehicleSeat.Driver,"Ron in the lift: Ice comes to board it, Gohan goes for the Granger; nobody is imagined into a seat the aircraft lacks");
  Game.GameTime+=20001;m16.Tick();Check(crew.PedFor(CrewSlot.Ice).IsInVehicle(m16.Cargobob)&&m16.CurrentStage==4,"Ice aboard, the canyon is the job");
  m16.Cargobob.HeightAboveGround=20f;m16.Cargobob.Position=c.Locations.Position("M16.CanyonRun");Game.Player.Character.Position=m16.Cargobob.Position;m16.Tick();Check(m16.CurrentStage==5,"Through the canyon, the pursuit is lost on the way, not at the flats");
  Game.Player.WantedLevel=0;m16.Tick();Check(m16.CurrentStage==6,"Pursuit lost, Terminal is the job");
  m16.Cargobob.HeightAboveGround=0f;m16.Cargobob.Speed=0f;m16.Cargobob.Position=c.Locations.Position("M16.TerminalDrop");Game.Player.Character.Position=m16.Cargobob.Position;m16.Tick();
  Check(m16.Landed&&c.Cutscenes.IsActive&&c.State.CargoAt("cargobob")=="M16.TerminalDrop","Landed on the flats, the lift is shown and recorded where M18 finds it");
  c.Cutscenes.Skip();m16.Tick();c.Dialogue.Clear();m16.Tick();Check(m16.Status==MissionStatus.Passed,"M16 passes");
  m16.Cleanup();Check(m16.Cargobob.Exists()&&m16.Granger.Exists(),"The lift and the Granger stay in the world");
  string m16src=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act1","M16TheHeavyLift.cs"));
  Check(!m16src.Contains("Game.Player.WantedLevel = 0;\n                    GameUtils.Subtitle(\"~g~Heavy lift secured")&&m16src.Contains("new LoseWantedObjective(\"Lose the pursuit before Terminal Island.\")"),"M16 no longer clears the wanted level at its marker; the pursuit is lost first");

  // ---- M17: the sub seen, three parts on the hull, the release asked for and changed, the sub recorded ready.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"lift17.json"));var m17=new M17SubZeroPayload();
  Check(m17.Begin(c)&&c.Cutscenes.IsActive&&m17.Kraken!=null&&m17.EndpointKind==MissionEndpoint.SecuredDelivery,"M17 opens on the sub in the slip with Ron looking it over and Ice at the gear");
  c.Cutscenes.Skip();m17.Tick();Use(crew,CrewSlot.Gohan);
  foreach(var key in new[]{"M17.WeldOne","M17.WeldTwo","M17.WeldThree"}){Interact(m17,c,CrewSlot.Gohan,c.Locations.Position(key),8);}
  Check(m17.Parts.Count==3&&m17.Parts.All(p=>p.AttachedTo==m17.Kraken),"Each weld point leaves a real part on the hull: three sites, three parts");
  m17.Tick();m17.Tick();Check(m17.CurrentStage==1&&m17.ReleaseAsked&&c.Dialogue.HasPending,"The welds done, Ron asks where the release is");
  GTA.UI.Screen.Subtitle=null;Interact(m17,c,CrewSlot.Guess,c.Locations.Position("M17.DrySlip"),10);Check(m17.CurrentStage==2,"The lock tested, the release is Gohan's to change");
  Interact(m17,c,CrewSlot.Gohan,m17.Kraken.Position-m17.Kraken.ForwardVector*3.5f,5);
  Check(m17.CurrentStage==3&&m17.ReleaseChanged&&c.Cutscenes.IsActive&&m17.ReleaseHandle!=null&&m17.ReleaseHandle.AttachedTo==m17.Kraken,"The release moved outside is a handle on the hull, seen");
  c.Cutscenes.Skip();m17.Tick();c.Dialogue.Clear();m17.Tick();c.Dialogue.Clear();m17.Tick();
  Check(m17.Status==MissionStatus.Passed&&c.State.CargoAt("kraken")=="M17.DrySlip","The radio check done, the sub is recorded ready at the slip");
  m17.Cleanup();Check(m17.Kraken.Exists()&&World.Props.Count(p=>p.Exists()&&p.AttachedTo==m17.Kraken)>=4,"The sub, its parts and its handle stay for staging");

  // ---- M18: the plan on the hood, each asset delivered and recorded, the pod fitted, the roll call as a scene, one clock.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"lift18.json"));var m18=new M18TheStagingLine();
  Check(m18.Begin(c)&&c.Cutscenes.IsActive&&m18.Kraken!=null&&m18.Cargobob!=null&&m18.Hauler!=null&&m18.EndpointKind==MissionEndpoint.ContinuousNext,"M18 opens on the plan and ends continuously into M19");
  string m18src=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act1","M18TheStagingLine.cs"));
  Check(m18src.Contains("RemindOpenSolos();")&&m18src.Contains("Open before the heist: "),"Open solo jobs are called out before the operation, not after it");
  c.Cutscenes.Skip();m18.Tick();Use(crew,CrewSlot.Gohan);Game.Player.Character.SetIntoVehicle(m18.Kraken,VehicleSeat.Driver);m18.Kraken.Position=c.Locations.Position("M18.ChannelMark");Game.Player.Character.Position=m18.Kraken.Position;m18.Tick();
  Check(m18.CurrentStage==1&&c.State.CargoAt("kraken")=="M18.ChannelMark","The sub held in the channel is recorded there");
  Use(crew,CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(m18.Cargobob,VehicleSeat.Driver);m18.Cargobob.Position=c.Locations.Position("M18.SaltHangar");m18.Cargobob.HeightAboveGround=0f;m18.Cargobob.Speed=0f;Game.Player.Character.Position=m18.Cargobob.Position;m18.Tick();
  Check(m18.CurrentStage==2&&c.State.CargoAt("cargobob")=="M18.SaltHangar","The lift in the hangar is recorded there");
  GTA.UI.Screen.Subtitle=null;Interact(m18,c,CrewSlot.Guess,m18.Cargobob.Position+new Vector3(3,0,0),6);
  Check(m18.CurrentStage==3&&m18.PodFitted&&m18.Pod!=null&&m18.Pod.AttachedTo==m18.Cargobob&&c.State.CargoAt("radarPod")=="M18.SaltHangar","The pod is fitted to the lift as a real part and recorded on it");
  Use(crew,CrewSlot.Ice);Game.Player.Character.SetIntoVehicle(m18.Hauler,VehicleSeat.Driver);m18.Hauler.Position=c.Locations.Position("M18.HaulerMark");Game.Player.Character.Position=m18.Hauler.Position;m18.Tick();Check(m18.CurrentStage==4,"The hauler on the line, the launchers are the job");
  Interact(m18,c,CrewSlot.Ice,c.Locations.Position("M18.HaulerMark"),10);
  Check(m18.CurrentStage==5&&m18.RollCalled&&c.Cutscenes.IsActive&&c.State.CargoAt("hauler")=="M18.HaulerMark","The launchers loaded, the roll call plays as a scene with each man in his seat");
  c.Cutscenes.Skip();m18.Tick();c.Dialogue.Clear();m18.Tick();c.Dialogue.Clear();m18.Tick();
  Check(m18.Status==MissionStatus.Passed&&c.State.CargoAt("heistClock")=="M18","One clock from here: the staging is recorded and M19 follows");
  m18.Cleanup();Check(m18.Kraken.Exists()&&m18.Cargobob.Exists()&&m18.Hauler.Exists(),"The staged assets are the next chapter's, not the mission's");
  var scenes=File.ReadAllLines(Path.Combine(dataDir,"scenes.tsv"));
  Check(new[]{"M16","M17","M18"}.All(id=>scenes.Count(l=>l.StartsWith(id+"_SCENE_APPROACH"))==2)&&scenes.Count(l=>l.StartsWith("M16_SCENE_LIFT"))==2&&scenes.Count(l=>l.StartsWith("M17_SCENE_RELEASE"))==2,"The approaches, the lift and the release have their authored lines");
 }
}
