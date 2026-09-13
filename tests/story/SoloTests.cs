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
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"solo2.json"));var s2=new SM02ZeroDayInjection();World.CollisionReady=true;
  Check(s2.Begin(c)&&c.Cutscenes.IsActive&&s2.TerminalProp!=null&&s2.EndpointKind==MissionEndpoint.EscapeCheckpoint,"SM02 opens on the roof, the bay and a terminal that exists; the fire escape clears nothing by itself");
  c.Cutscenes.Skip();s2.Tick();Use(crew,CrewSlot.Gohan);Interact(s2,c,CrewSlot.Gohan,c.Locations.Position("SM02.StairEntry"),2);Check(s2.CurrentStage==1,"On the roof the guards are the job");
  var annex=World.Created.Where(p=>p.Model.Name=="s_m_m_security_01").ToList();foreach(var g in annex)g.IsBeingStunned=true;s2.Tick();Check(s2.CurrentStage==2&&annex.All(g=>g.IsAlive),"Both guards down and alive, the terminal is the job");
  GTA.UI.Screen.Subtitle=null;Interact(s2,c,CrewSlot.Gohan,c.Locations.Position("SM02.Terminal"),8);
  Check(s2.CurrentStage==3&&c.Cutscenes.IsActive&&s2.TapLive&&c.State.EvidenceOf("cameraArchive")==EvidenceState.CopyHeld,"The tap plays as a scene and the result is recorded as camera archive access, not the dock recording");
  c.Cutscenes.Skip();s2.Tick();Game.GameTime+=SM02ZeroDayInjection.TraceSeconds*1000+1500;s2.Tick();
  Check(s2.Status==MissionStatus.Failed&&s2.FailReason.Contains("traced"),"IT's trace is a clock: too slow down the fire escape and the job is lost");
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"solo2b.json"));s2=new SM02ZeroDayInjection();World.CollisionReady=true;s2.Begin(c);c.Cutscenes.Skip();s2.Tick();
  Use(crew,CrewSlot.Gohan);Interact(s2,c,CrewSlot.Gohan,c.Locations.Position("SM02.StairEntry"),2);foreach(var g in World.Created.Where(p=>p.Model.Name=="s_m_m_security_01"))g.IsBeingStunned=true;s2.Tick();
  Interact(s2,c,CrewSlot.Gohan,c.Locations.Position("SM02.Terminal"),8);c.Cutscenes.Skip();s2.Tick();
  Interact(s2,c,CrewSlot.Gohan,c.Locations.Position("SM02.RoofAccess"),2);c.Dialogue.Clear();s2.Tick();
  Check(s2.Status==MissionStatus.Passed,"Down the fire escape in time, Gohan is clear");
  string s2src=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Solo","SM02ZeroDayInjection.cs"));Check(s2src.Contains("stocks the Marksman Rifle")&&s2src.Contains("The dock recording and the witness are untouched"),"SM02's reward and result are said as they work");

  // ---- SM03: owned car, no cargo, one shared mountain sprint.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"solo3.json"));var s3=new SM03MidnightDrift();
  Check(!s3.Begin(c)&&SM03MidnightDrift.EntryRequirement(c).Contains("Buy a personal car"),"The race refuses a missing personal car with a purchase instruction");
  var owned=new OwnedVehicle{Id=1,ModelName="sultanrs",ModelHash=(uint)Game.GenerateHash("sultanrs"),Garage="bay-guess",Label="My sprint car"};c.State.Vehicles.Add(owned);
  c.Garages=new GarageService(crew,c.State,c.Locations,null);c.Garages.Allowed=()=>true;
  var personal=c.Garages.Retrieve(owned);Check(personal!=null,"The race fixture retrieves a real owned garage car");
  Use(crew,CrewSlot.Guess);Game.Player.Character.Position=personal.Position;Game.Player.Character.SetIntoVehicle(personal,VehicleSeat.Driver);
  s3=new SM03MidnightDrift();Check(s3.Begin(c)&&s3.Coupe==personal&&s3.KJ!=null&&s3.Prize==null,"SM03 uses the actual owned car and spawns no cargo on its tail");
  Interact(s3,c,CrewSlot.Guess,c.Locations.Position("SM03.StartLine"),3,true);Check(s3.CurrentStage==1,"Ready-up starts one sprint");
  var route=c.Locations.All.Where(l=>l.Key.StartsWith("SM03.Sprint",StringComparison.Ordinal)).OrderBy(l=>l.Key).ToArray();
  Check(route.Length==278&&route.Last().Position.Z>750f,"The connected road route reaches the mountain summit");
  foreach(var gate in route){s3.Coupe.Position=gate.Position;Game.Player.Character.Position=gate.Position;s3.Tick();}
  c.Dialogue.Clear();s3.Tick();Check(s3.Status==MissionStatus.Passed&&s3.PrizeHome&&c.State.CargoAt("racePrize")=="SM03.Summit"&&!s3.GunsShown,"Winning finishes at the summit without a cargo delivery or surprise gunfight");
  s3.Cleanup();Check(personal.Exists(),"Cleanup preserves the player's personal car");
  var scenes=File.ReadAllLines(Path.Combine(dataDir,"scenes.tsv"));
  Check(scenes.Count(l=>l.StartsWith("SM01_SCENE_APPROACH"))==2&&scenes.Count(l=>l.StartsWith("SM02_SCENE_APPROACH"))==2&&scenes.Count(l=>l.StartsWith("SM03_SCENE_APPROACH"))==2&&scenes.Any(l=>l.StartsWith("SM03_SCENE_APPROACH_01_KJ")),"Each solo's approach has two authored lines, KJ speaking in his own");
 }
}
