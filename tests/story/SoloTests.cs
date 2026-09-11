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
 static void SoloChecks()
 {
  // ---- SM01: the shipment seen, the broker kept alive, the crates counted into Ice's car and brought home.
  Reset();var crew=Roster();var c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"solo1.json"));var s1=new SM01LeadAndKevlar();
  Check(s1.Begin(c)&&c.Cutscenes.IsActive&&s1.Crates.Count==2&&s1.Car!=null&&s1.Sergei!=null&&s1.EndpointKind==MissionEndpoint.SafehouseArrival,"SM01 opens on Sergei, the two crates and Ice's car; it ends at Ice's own door");
  c.Cutscenes.Skip();s1.Tick();Check(!c.Cutscenes.IsActive&&s1.Status==MissionStatus.Running,"Skipping the approach leaves Ice at the warehouse with the job ahead");
  Use(crew,CrewSlot.Ice);Game.Player.Character.Position=c.Locations.Position("SM01.SergeiOffice");s1.Tick();Check(s1.CurrentStage==1,"Through the side door the floor wakes");
  var guards=World.Created.Where(p=>p!=s1.Sergei&&p.Model.Name!="a_m_y_stbla_02").ToList();foreach(var g in guards)g.IsDead=true;s1.Tick();
  Check(s1.CurrentStage==2&&!s1.Sergei.IsInvincible,"The floor clear, Sergei is the job and he can die, which is the point of not killing him");
  s1.Sergei.IsDead=true;s1.Tick();Check(s1.Status==MissionStatus.Failed&&s1.FailReason.Contains("codes"),"A dead Sergei takes the codes with him");
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"solo1b.json"));s1=new SM01LeadAndKevlar();s1.Begin(c);c.Cutscenes.Skip();s1.Tick();
  Use(crew,CrewSlot.Ice);Game.Player.Character.Position=c.Locations.Position("SM01.SergeiOffice");s1.Tick();foreach(var g in World.Created.Where(p=>p!=s1.Sergei))g.IsDead=true;s1.Tick();
  GTA.UI.Screen.Subtitle=null;Interact(s1,c,CrewSlot.Ice,s1.Sergei.Position,4);
  Check(s1.CurrentStage==3&&s1.CodesGiven&&s1.Sergei.IsAlive&&s1.Sergei.Task.Flees==1&&s1.Sergei.IsInvincible,"The codes given, Sergei runs alive: his outcome is deliberate");
  Interact(s1,c,CrewSlot.Ice,c.Locations.Position("SM01.CrateLoad"),2);
  Check(s1.CurrentStage==4&&c.Cutscenes.IsActive&&s1.Loaded,"Loading plays as a scene, counted");
  c.Cutscenes.Skip();Check(s1.Crates.All(cr=>cr.AttachedTo==s1.Car),"Skipping lands both crates in Ice's car");
  Game.Player.Character.SetIntoVehicle(s1.Car,VehicleSeat.Driver);s1.Car.Position=c.Locations.Position("Apartment.Starter.Ice");Game.Player.WantedLevel=0;s1.Tick();c.Dialogue.Clear();s1.Tick();
  Check(s1.Status==MissionStatus.Passed&&c.State.CargoAt("sergeiCrates")=="Apartment.Starter.Ice","At Ice's door the crates are recorded home");
  string s1src=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Solo","SM01LeadAndKevlar.cs"));Check(s1src.Contains("Pump Shotgun Mk II and issues double rifle ammunition")&&!s1src.Contains("armor-piercing damage"),"SM01's reward is said as it works: the Mk II and double rifle ammunition, no invented damage mechanic");
  s1.Cleanup();Check(s1.Car.Exists()&&World.Props.Any(p=>p.Exists()&&p.AttachedTo==s1.Car),"Ice's car and the crates stay at his door");

  // ---- SM02: the annex seen with the check-in said, the tap on camera, the result named, IT's clock.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"solo2.json"));var s2=new SM02ZeroDayInjection();
  Check(s2.Begin(c)&&c.Cutscenes.IsActive&&s2.TerminalProp!=null&&s2.EndpointKind==MissionEndpoint.EscapeCheckpoint,"SM02 opens on the roof, the bay and a terminal that exists; the fire escape clears nothing by itself");
  c.Cutscenes.Skip();s2.Tick();Use(crew,CrewSlot.Gohan);Game.Player.Character.Position=c.Locations.Position("SM02.RoofAccess");s2.Tick();Check(s2.CurrentStage==1,"On the roof the guards are the job");
  var annex=World.Created.Where(p=>p.Model.Name=="s_m_m_security_01").ToList();foreach(var g in annex)g.IsBeingStunned=true;s2.Tick();Check(s2.CurrentStage==2&&annex.All(g=>g.IsAlive),"Both guards down and alive, the terminal is the job");
  GTA.UI.Screen.Subtitle=null;Interact(s2,c,CrewSlot.Gohan,c.Locations.Position("SM02.Terminal"),8);
  Check(s2.CurrentStage==3&&c.Cutscenes.IsActive&&s2.TapLive&&c.State.EvidenceOf("cameraArchive")==EvidenceState.CopyHeld,"The tap plays as a scene and the result is recorded as camera archive access, not the dock recording");
  c.Cutscenes.Skip();s2.Tick();Game.GameTime+=SM02ZeroDayInjection.TraceSeconds*1000+1500;s2.Tick();
  Check(s2.Status==MissionStatus.Failed&&s2.FailReason.Contains("traced"),"IT's trace is a clock: too slow down the fire escape and the job is lost");
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"solo2b.json"));s2=new SM02ZeroDayInjection();s2.Begin(c);c.Cutscenes.Skip();s2.Tick();
  Use(crew,CrewSlot.Gohan);Game.Player.Character.Position=c.Locations.Position("SM02.RoofAccess");s2.Tick();foreach(var g in World.Created.Where(p=>p.Model.Name=="s_m_m_security_01"))g.IsBeingStunned=true;s2.Tick();
  Interact(s2,c,CrewSlot.Gohan,c.Locations.Position("SM02.Terminal"),8);c.Cutscenes.Skip();s2.Tick();
  Game.Player.Character.Position=c.Locations.Position("SM02.Exit");s2.Tick();c.Dialogue.Clear();s2.Tick();
  Check(s2.Status==MissionStatus.Passed,"Down the fire escape in time, Gohan is clear");
  string s2src=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Solo","SM02ZeroDayInjection.cs"));Check(s2src.Contains("stocks the Marksman Rifle")&&s2src.Contains("The dock recording and the witness are untouched"),"SM02's reward and result are said as they work");

  // ---- SM03: KJ walks the prize, a legitimate result, the guns shown, the prize to the chop bay.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"solo3.json"));var s3=new SM03MidnightDrift();
  Check(s3.Begin(c)&&c.Cutscenes.IsActive&&s3.KJ!=null&&s3.Prize!=null&&s3.Prize.AttachedTo==s3.Coupe,"SM03 opens with KJ walking the prize on the coupe's tail and the coupe itself");
  c.Cutscenes.Skip();s3.Tick();Use(crew,CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(s3.Coupe,VehicleSeat.Driver);s3.Tick();Check(s3.CurrentStage==1,"In the coupe, the race is on");
  var circuit=new[]{"SM03.Checkpoint1","SM03.Checkpoint2","SM03.Checkpoint3","SM03.Checkpoint4"};
  for(int lap=0;lap<3;lap++)foreach(var key in circuit){s3.Coupe.Position=c.Locations.Position(key);Game.Player.Character.Position=s3.Coupe.Position;s3.Tick();}
  Check(s3.CurrentStage==2&&!s3.GunsShown,"Three laps won on the road, the rivals turn: nothing auto-wins a lap");
  s3.Tick();Check(s3.GunsShown&&c.Cutscenes.IsActive,"The guns are shown once as the change of plan");
  c.Cutscenes.Skip();s3.Tick();Check(c.Dialogue.HasPending,"Ron says it: stop them or get the coupe to the finish");
  foreach(var r in World.Created.Where(p=>p.Model.Name=="g_m_y_salvaboss_01"))r.IsDead=true;s3.Tick();Check(s3.CurrentStage==3,"The shooters down, the chop bay is the job");
  s3.Coupe.Position=c.Locations.Position("M11.ChopShop");Game.Player.Character.Position=s3.Coupe.Position;Game.Player.WantedLevel=0;s3.Tick();c.Dialogue.Clear();s3.Tick();
  Check(s3.Status==MissionStatus.Passed&&s3.PrizeHome&&c.State.CargoAt("racePrize")=="M11.ChopShop","At the bay the prize is recorded");
  string s3src=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Solo","SM03MidnightDrift.cs"));Check(s3src.Contains("$25,000 and the race transmission"),"SM03's reward is said as it works: the cash and the transmission the garage fits");
  s3.Cleanup();Check(s3.Coupe.Exists()&&s3.Prize.Exists(),"The coupe and the prize stay at the bay");
  var scenes=File.ReadAllLines(Path.Combine(dataDir,"scenes.tsv"));
  Check(scenes.Count(l=>l.StartsWith("SM01_SCENE_APPROACH"))==2&&scenes.Count(l=>l.StartsWith("SM02_SCENE_APPROACH"))==2&&scenes.Count(l=>l.StartsWith("SM03_SCENE_APPROACH"))==2&&scenes.Any(l=>l.StartsWith("SM03_SCENE_APPROACH_01_KJ")),"Each solo's approach has two authored lines, KJ speaking in his own");
 }
}
