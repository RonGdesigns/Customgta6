using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

public static partial class StoryTests
{
 static string Repo => Path.GetFullPath(Path.Combine(dataDir,".."));
 static string Source(string relative) => File.ReadAllText(Path.Combine(Repo,relative.Replace('/',Path.DirectorySeparatorChar)));

 static void FixBranchChecks()
 {
  // ---- N1. The retired override template is ignored; real surveys still apply.
  Reset();var ini=Path.Combine(root,"stale-template.ini");
  File.WriteAllText(ini,"[Positions]\nM01.CraneNest.X = 1082.00\nM01.CraneNest.Y = -3175.00\nM01.CraneNest.Z = 40.00\nM01.ExitPoint.X = 1180.00\nM01.ExitPoint.Y = -2990.00\nM01.ExitPoint.Z = 5.90\nM01.IceApproach.X = 900.00\nM01.IceApproach.Y = -3200.00\nM01.IceApproach.Z = 6.00\n[Headings]\nM01.CraneNest = 210\nM01.IceApproach = 77\n");
  var book=LocationBook.Load(dataDir,ini);
  Check(book.Get("M01.CraneNest").Position==new Vector3(976.6f,-3239.5f,6f)&&book.Get("M01.CraneNest").Status==LocationStatus.Estimate,"A template-identical lookout override is ignored; the repository position stands");
  Check(Math.Abs(book.Get("M01.CraneNest").Heading-35f)<0.01f,"The template's lookout heading is ignored with its position");
  Check(book.Get("M01.ExitPoint").Position==new Vector3(720.5f,-2400.1f,15.2f),"The template's old exit point does not override the bible exit");
  Check(book.Get("M01.IceApproach").Position==new Vector3(900f,-3200f,6f)&&book.Get("M01.IceApproach").Status==LocationStatus.Surveyed&&Math.Abs(book.Get("M01.IceApproach").Heading-77f)<0.01f,"A value the player actually captured still overrides, with its heading");
  Check(book.IgnoredStaleOverrides==2,"Both stale keys were counted and logged (got "+book.IgnoredStaleOverrides+")");
  var template=Source("config/Bloodlines.Locations.ini").Split('\n').Select(l=>l.Trim()).Where(l=>l.Length>0&&!l.StartsWith(";")&&!l.StartsWith("["));
  Check(!template.Any(),"The shipped override template carries no positions or headings");

  // ---- R01. The prologue's route is produced inside the marker frame.
  string host=Source("src/Bloodlines/BloodlinesMain.cs");
  int begin=host.IndexOf("ObjectiveMarkers.BeginFrame(",StringComparison.Ordinal),step=host.IndexOf("Step(\"prologue\"",StringComparison.Ordinal),end=host.IndexOf("ObjectiveMarkers.EndFrame()",StringComparison.Ordinal);
  Check(begin>0&&step>begin&&end>step,"In the host tick the prologue runs after BeginFrame and before EndFrame");
  Reset();var crew=Roster();var c=Context(crew);ObjectiveMarkers.Clear();var save=CampaignState.Load(Path.Combine(root,"route.json"));var home=new Vector3(291.5f,-1078.7f,29.4f);
  var prologue=new PrologueSequence(crew,c.Cutscenes,c.Locations,save,()=>home);prologue.Begin();c.Cutscenes.Skip();
  var routeField=typeof(ObjectiveMarkers).GetField("_route",BindingFlags.NonPublic|BindingFlags.Static);
  // the old order: prologue before the frame opens
  prologue.Update();prologue.Update();ObjectiveMarkers.BeginFrame(true);ObjectiveMarkers.EndFrame();
  Check(routeField.GetValue(null)==null,"A route submitted before BeginFrame is discarded (the defect the order fix removes)");
  // the fixed order
  ObjectiveMarkers.BeginFrame(true);prologue.Update();ObjectiveMarkers.EndFrame();
  var route=routeField.GetValue(null) as Blip;
  Check(route!=null&&route.Present&&route.ShowRoute&&route.Position==home,"Inside the frame the prologue's drive-home route becomes the GPS route");
  ObjectiveMarkers.Clear();

  // ---- R03. Eligibility is answered before anything is stood down.
  int canStart=host.IndexOf("_missions.CanStart(next",StringComparison.Ordinal),standDown=host.IndexOf("if (_crew.IsDeployed) StandDown();",host.IndexOf("private void StartMission",StringComparison.Ordinal),StringComparison.Ordinal);
  Check(canStart>0&&standDown>canStart,"StartMission checks eligibility before it stands the crew down");
  Reset();crew=Roster();c=Context(crew);var cat=new MissionCatalog();
  cat.All.Add(Def("M18","main"));foreach(var id in new[]{"SM01","SM02","SM03"})cat.All.Add(Def(id,"solo","M03"));cat.All.Add(Def("M19","main","M18"));
  var gated=CampaignState.Load(Path.Combine(root,"canstart.json"));gated.Completed.Add("M03");gated.Completed.Add("M18");
  var manager=new MissionManager(c,gated,cat);var arsenal=new WeaponProgression(gated);crew.Arsenal=arsenal;int dismissals=crew.Dismissals;GameUtils.Message=null;
  Check(!manager.CanStart(cat.All.First(m=>m.Id=="M19"),out var why)&&why.Contains("SM01, SM02, SM03")&&crew.Dismissals==dismissals&&!arsenal.LoanActive&&GameUtils.Message==null&&!manager.IsRunning,"CanStart refuses the gated mission with the reason and touches nothing");
  Check(manager.CanStart(cat.All.First(m=>m.Id=="M19"),out why,bypassGates:true)&&why==null,"QA bypass is answered the same way, still without side effects");
  Check(!manager.CanStart(null,out why)&&why!=null,"A missing definition is a refusal, not an exception");
  Check(manager.CanStart(cat.All.First(m=>m.Id=="SM01"),out why)&&why==null,"An open job is startable");
  manager.Start(cat.All.First(m=>m.Id=="SM01"));
  Check(!manager.CanStart(cat.All.First(m=>m.Id=="SM02"),out why)&&why.Contains("already running"),"A running mission refuses a second start");manager.Abort();

  // ---- R04. Failing during the briefing closes the loan.
  Reset();crew=Roster();c=Context(crew);var loanState=CampaignState.Load(Path.Combine(root,"briefing-loan.json"));arsenal=new WeaponProgression(loanState);crew.Arsenal=arsenal;
  var loanCat=new MissionCatalog();var m02=Def("M02","main");loanCat.All.Add(m02);manager=new MissionManager(c,loanState,loanCat);
  Check(manager.Start(m02)&&c.Cutscenes.IsActive&&arsenal.LoanActive&&manager.IsRunning,"A briefing is pending with its loan open");
  manager.ForceFail("qa");
  Check(!manager.IsRunning&&!c.Cutscenes.IsActive&&!arsenal.LoanActive&&manager.RetryAvailable&&loanState.CompletedCount==0,"ForceFail during the briefing ends the attempt once, closes the loan and awards nothing");
  var ice=crew.PedFor(CrewSlot.Ice);ice.Weapons.Give(WeaponHash.Pistol,60,false,true);
  for(int i=0;i<4;i++){Game.GameTime+=5000;arsenal.Update(crew,captureAllowed:true);}
  Check(loanState.Weapons["Ice"].Contains((uint)WeaponHash.Pistol),"Ordinary free-roam capture works again afterward");
  manager.Update();Check(!manager.IsRunning&&!arsenal.LoanActive,"Nothing lingers for the next tick to pick up");

  // ---- R02. Scene outcomes are distinct and a failed step ends the blocking.
  Reset();var stuck=new Ped{StuckInSeat=true};var stuckRide=new Vehicle{Position=new Vector3(50,50,0)};stuck.SetIntoVehicle(stuckRide,VehicleSeat.Driver);var other=new Ped{Position=Vector3.Zero};
  var watched=new SceneBlocking().Then(new ExitVehicleStep(stuck){TimeoutMs=100}).Then(new WalkToStep(other,new Vector3(9,9,0),1f));
  watched.Update();Game.GameTime+=200;watched.Update();
  Check(watched.Steps[0].Failed&&watched.Canceled&&watched.IsFinished&&!watched.Succeeded&&other.Position==Vector3.Zero,"A watched step that times out and cannot reach its end state cancels the rest instead of advancing");
  var next=typeof(CutsceneDirector).GetMethod("NextLine",BindingFlags.NonPublic|BindingFlags.Instance);
  Reset();crew=Roster();c=Context(crew);c.Cutscenes.Play("M02","outro","Aftermath");for(int i=0;i<12&&c.Cutscenes.IsActive;i++)next.Invoke(c.Cutscenes,null);
  Check(!c.Cutscenes.IsActive&&c.Cutscenes.LastOutcome==SceneOutcome.Completed,"A scene that plays out reports Completed");
  c.Cutscenes.Play("M02","outro","Aftermath");c.Cutscenes.Skip();Check(c.Cutscenes.LastOutcome==SceneOutcome.Skipped,"A deliberate skip reports Skipped");
  c.Cutscenes.Play("M02","outro","Aftermath");c.Cutscenes.Stop();Check(c.Cutscenes.LastOutcome==SceneOutcome.Canceled,"Stop reports Canceled");
  var ron=Game.Player.Character;var car=new Vehicle{Position=new Vector3(30,0,0)};ron.SetIntoVehicle(car,VehicleSeat.Driver);ron.StuckInSeat=true;
  c.Cutscenes.Play("M01","arrival","Home",null,null,new SceneBlocking().Then(new ExitVehicleStep(ron)).Then(new WalkToStep(ron,home,1f)));c.Cutscenes.Skip();
  Check(c.Cutscenes.LastOutcome==SceneOutcome.Failed&&ron.IsInVehicle(car),"A skip whose blocking could not reach its end state reports Failed, not Skipped");ron.StuckInSeat=false;
  // Homecoming: a canceled scene is not a homecoming; the drive resumes and retries directly.
  Reset();crew=Roster();c=Context(crew);World.CollisionReady=true;save=CampaignState.Load(Path.Combine(root,"homecoming.json"));int handoffs=0;
  prologue=new PrologueSequence(crew,c.Cutscenes,c.Locations,save,()=>home){Finished=()=>handoffs++};
  prologue.Begin();c.Cutscenes.Skip();prologue.Update();var ride=Game.Player.Character.CurrentVehicle;ride.Position=home;Game.Player.Character.Position=home;ride.Speed=0;prologue.Update();
  Check(prologue.Current==PrologueSequence.Phase.Homecoming&&c.Cutscenes.IsActive,"Arriving starts the homecoming scene");
  c.Cutscenes.Stop();prologue.Update();
  Check(prologue.Current==PrologueSequence.Phase.Drive&&handoffs==0&&!save.PrologueComplete,"A canceled homecoming does not complete the prologue; the drive resumes");
  prologue.Update();
  Check(prologue.Current==PrologueSequence.Phase.Homecoming&&!c.Cutscenes.IsActive&&!Game.Player.Character.IsInVehicle()&&Game.Player.Character.Position==home,"The retry places Ron at the door directly instead of trusting the scene again");
  prologue.Update();Check(handoffs==1&&save.PrologueComplete&&!prologue.IsActive,"The direct retry completes the prologue exactly once");
  prologue.Update();Check(handoffs==1,"Finish cannot fire twice");
  // A Ron the engine will not unseat is asked to get out; the prologue waits.
  Reset();crew=Roster();c=Context(crew);World.CollisionReady=true;save=CampaignState.Load(Path.Combine(root,"homecoming-stuck.json"));handoffs=0;
  prologue=new PrologueSequence(crew,c.Cutscenes,c.Locations,save,()=>home){Finished=()=>handoffs++};
  prologue.Begin();c.Cutscenes.Skip();prologue.Update();ride=Game.Player.Character.CurrentVehicle;ride.Position=home;Game.Player.Character.Position=home;ride.Speed=0;prologue.Update();c.Cutscenes.Stop();prologue.Update();
  Game.Player.Character.StuckInSeat=true;prologue.Update();
  Check(prologue.Current==PrologueSequence.Phase.Drive&&handoffs==0&&GameUtils.Message.Contains("Get out"),"When Ron cannot be unseated the prologue stays in the drive and asks him to get out");
  Game.Player.Character.StuckInSeat=false;Game.Player.Character.Task.LeaveVehicle();Game.Player.Character.Position=home;Game.GameTime+=5000;prologue.Update();prologue.Update();
  Check(handoffs==1&&save.PrologueComplete,"Once he is out on foot at the door the homecoming completes");
  // Cold-open placement: nothing moves unless both halves succeed.
  Reset();World.CollisionReady=true;var dock=new Vector3(1073,-3160,5.9f);var seated=new Ped();var cab=new Vehicle{Position=new Vector3(291,-1078,29)};seated.SetIntoVehicle(cab,VehicleSeat.Driver);
  Check(PrologueSequence.PlaceForColdOpen(seated,dock)==PrologueSequence.Placement.Placed&&!seated.IsInVehicle()&&seated.Position==dock&&cab.Position!=dock,"Seated player: unseated, then placed; the car stays behind");
  var glued=new Ped{StuckInSeat=true};var gluedCab=new Vehicle{Position=new Vector3(291,-1078,29)};glued.SetIntoVehicle(gluedCab,VehicleSeat.Driver);
  Check(PrologueSequence.PlaceForColdOpen(glued,dock)==PrologueSequence.Placement.StillSeated&&glued.IsInVehicle(gluedCab)&&gluedCab.Position==new Vector3(291,-1078,29)&&glued.Position==new Vector3(291,-1078,29),"A player who cannot be unseated is not moved, and neither is the vehicle");
  World.CollisionReady=false;var walker=new Ped{Position=new Vector3(5,5,5),Heading=33};
  Check(PrologueSequence.PlaceForColdOpen(walker,dock)==PrologueSequence.Placement.NoGround&&walker.Position==new Vector3(5,5,5)&&walker.Heading==33,"A dock that never streams collision returns the player to where they were");
  World.CollisionReady=true;Check(PrologueSequence.PlaceForColdOpen(walker,dock)==PrologueSequence.Placement.Placed&&walker.Position==dock,"On foot with ground loaded the player is placed");
  int tryIdx=host.IndexOf("var placement = PrologueSequence.PlaceForColdOpen",StringComparison.Ordinal),finallyIdx=host.IndexOf("finally { GameUtils.FadeIn(1200); }",StringComparison.Ordinal);
  Check(tryIdx>0&&finallyIdx>tryIdx&&host.IndexOf("if (placement != PrologueSequence.Placement.Placed)",StringComparison.Ordinal)>tryIdx,"The host acts on the placement result and always fades back in");

  // ---- R05. M28's camera option changes how the response behaves.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"m28-cams.json"));var m28=new M28OffTheGrid();m28.Begin(c);
  var options=((TechnicalChoiceObjective)Flow(m28)[2].Objectives[0]).Options;var wave=typeof(M28OffTheGrid).GetMethod("Wave",BindingFlags.NonPublic|BindingFlags.Instance);
  options[2].Apply(c);var quiet=((IEnumerable<Ped>)wave.Invoke(m28,new object[]{1})).ToList();
  Check(!Field<bool>(m28,"_responseAlerted")&&quiet.Count>0&&quiet.All(p=>p.Task.HatedFights==0),"Cameras first: the response arrives on guard, not sent straight at the crew");
  Check(options[2].Consequence.Contains("searching"),"The option's promise describes what it actually does");
  m28.Abort();Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"m28-feed.json"));m28=new M28OffTheGrid();m28.Begin(c);
  ((TechnicalChoiceObjective)Flow(m28)[2].Objectives[0]).Options[0].Apply(c);var loud=((IEnumerable<Ped>)wave.Invoke(m28,new object[]{1})).ToList();
  Check(Field<bool>(m28,"_responseAlerted")&&loud.All(p=>p.Task.HatedFights==1),"Feed first: the response is already hunting when it arrives");m28.Abort();
  Reset();crew=Roster();c=Context(crew);
  var broken=new TechnicalChoiceObjective("Cut",()=>new Vector3(5,5,0),new[]{new TechnicalOption("A","a",x=>throw new InvalidOperationException("no")),new TechnicalOption("B","b",x=>{})});
  broken.RequiredCharacter=CrewSlot.Gohan;broken.Enter(c);Use(crew,CrewSlot.Gohan);Game.Player.Character.Position=new Vector3(5,5,0);broken.Update(c);Game.Accept=true;broken.Update(c);
  Check(broken.Status==ObjectiveStatus.Failed&&broken.Chosen==null&&broken.FailReason.Contains("A"),"A consequence that fails to apply fails the objective rather than completing on a promise");

  // ---- R06. The generated map binds the choice to the stage that yields it and shows gates.
  string map=Source("docs/PLAYABLE-MISSION-MAP.md");
  var m28Rows=map.Substring(map.IndexOf("## M28",StringComparison.Ordinal)).Split('\n').Where(l=>l.StartsWith("| ")).ToArray();
  var cabinet=m28Rows.First(l=>l.Contains("**Read the cabinet**"));var clear=m28Rows.First(l=>l.Contains("**Clear the transformer yard**"));
  Check(cabinet.Contains("| Gohan |")&&cabinet.Contains("TechnicalChoiceObjective")&&!clear.Contains("TechnicalChoiceObjective"),"The map attributes Gohan's cabinet choice to his stage, not Ice's");
  Check(map.Substring(map.IndexOf("## M19",StringComparison.Ordinal),300).Contains("Story gate: SM01, SM02, SM03"),"The map states the central story gate on M19");

  // ---- N2. QA keys are off ScriptHookVDotNet's reload key.
  Check(!host.Contains("Keys.Insert")&&!host.Contains("Keys.Delete")&&host.Contains("Keys.OemOpenBrackets"),"The checkpoint QA keys no longer collide with the script reload key");
 }
}
