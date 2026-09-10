using System;
using System.Collections.Generic;
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

public sealed class PreserveProbe : Mission
{
 public override string Id=>"M02";public override string Title=>"Keep";protected override bool OnStart()=>true;protected override void OnUpdate(){}
 public void Register(Entity e)=>Track(e);public void Keep(Entity e)=>Preserve(e);
}

public static partial class StoryTests
{
 static void StoryToPlayChecks()
 {
  // ---- Scene spec: support cast speaks in place, authored shots drive the camera, skip lands the same state.
  Reset();var crew=Roster();var c=Context(crew);var miller=new Ped{Position=new Vector3(240,-805,30),ForwardVector=new Vector3(0,1,0)};var buyer=new Ped{Position=new Vector3(239,-805,30),ForwardVector=new Vector3(0,-1,0)};
  var prop=World.CreateProp(new Model("prop_ld_case_01"),miller.Position,false,false);
  var blocking=new SceneBlocking{DialogueAfterStep=1}.Then(ShotStep.Wide(3000,miller.Position,16f,9f)).Then(new CarryPropStep(miller,prop)).Then(ShotStep.OverShoulder(3000,miller,buyer)).Then(new HandoverStep(miller,buyer,prop,1000)).Then(new InspectStep(buyer,miller.Position,2000));
  var spec=new SceneSpec{MissionId="M04",Phase="transaction",Title="The exchange",Reason="test",Blocking=blocking}.With("MILLER",miller).With("BUYER",buyer);
  GTA.UI.Screen.Subtitle=null;Check(c.Cutscenes.Play(spec),"A specified scene with a support cast plays");
  Check(string.IsNullOrEmpty(GTA.UI.Screen.Subtitle)&&miller.IsInvincible&&!miller.IsPositionFrozen,"The support cast is protected, left movable because the blocking moves it, and no line plays before the first shot ends");
  var cam=World.RenderingCamera;c.Cutscenes.Update();var during=cam.Position;
  Check(Math.Abs(during.Z-(miller.Position.Z+9f))<0.01f&&Math.Abs(during.Y-(miller.Position.Y-16f))<0.01f,"The wide shot positions the scene camera itself");
  Game.GameTime+=3100;c.Cutscenes.Update();c.Cutscenes.Update();
  Check(prop.AttachedTo==miller,"The case goes into Miller's hand");
  c.Cutscenes.Update();c.Cutscenes.Update();
  Check(GTA.UI.Screen.Subtitle!=null&&GTA.UI.Screen.Subtitle.Contains("MILLER"),"Miller's line plays over the shoulder shot, spoken by the support ped");
  c.Cutscenes.Skip();
  Check(prop.AttachedTo==buyer&&c.Cutscenes.LastOutcome==SceneOutcome.Skipped&&!miller.IsPositionFrozen,"Skipping finishes the handover so the case is where the scene said it would be, and releases the cast");
  Check(ShotStep.Local(miller,new Vector3(2,1,0.5f))==new Vector3(241,-803,30.5f),"Shot offsets are forward, right, up in the anchor's frame");

  // ---- Role tracks: a brother approaches, holds, reacts to a threat, and resumes.
  Reset();crew=Roster();var hostile=new List<Ped>();var tracks=new RoleTracks(crew,()=>hostile);var ice=crew.PedFor(CrewSlot.Ice);Use(crew,CrewSlot.Guess);
  var watch=new Vector3(100,100,10);var cover=new Vector3(96,104,10);tracks.For(CrewSlot.Ice).Approach(watch,cover);tracks.Update();
  Check(crew.CompanionAI.StateOf(CrewSlot.Ice)==CompanionState.Scripted&&ice.Task.Gotos==1&&tracks.For(CrewSlot.Ice).State==RoleState.Approaching,"Approaching takes the ped under mission ownership and walks him to the point");
  ice.Position=watch;tracks.Update();Check(tracks.For(CrewSlot.Ice).State==RoleState.Observing&&tracks.AllIn(RoleState.Observing,CrewSlot.Ice),"At the point he holds and observes");
  var enemy=new Ped{Position=watch+new Vector3(20,0,0)};hostile.Add(enemy);tracks.Update();
  Check(tracks.For(CrewSlot.Ice).State==RoleState.Threatened&&ice.Task.HatedFights==1,"An enemy inside the threat radius sends him to cover, fighting");
  hostile.Clear();tracks.Update();Game.GameTime+=RoleTrack.ClearMs+100;tracks.Update();
  Check(tracks.For(CrewSlot.Ice).State==RoleState.Observing,"Six clear seconds resume the job");
  ice.Health-=50;tracks.Update();Check(tracks.For(CrewSlot.Ice).State==RoleState.Threatened,"Taking damage is a threat even with no enemy in sight");
  Use(crew,CrewSlot.Ice);int fights=ice.Task.HatedFights;Game.GameTime+=RoleTrack.ClearMs+100;tracks.Update();tracks.Update();
  Check(ice.Task.HatedFights==fights,"The active player's own track is left alone");
  tracks.Release();Check(crew.CompanionAI.StateOf(CrewSlot.Ice)==CompanionState.Follow,"Release hands the brothers back");

  // ---- Travel: radio cues at fractions of the distance; arrival means stopped in the zone.
  Reset();crew=Roster();c=Context(crew);Use(crew,CrewSlot.Guess);var player=Game.Player.Character;var van=new Vehicle{Model=new Model("granger")};player.SetIntoVehicle(van,VehicleSeat.Driver);
  var dest=new Vector3(1000,0,0);player.Position=Vector3.Zero;van.Position=Vector3.Zero;int cueA=0,cueB=0;
  var travel=new TravelObjective("Drive",()=>dest,12f,()=>van).Cue(0.6f,()=>cueA++).Cue(0.3f,()=>cueB++);travel.RequiredCharacter=CrewSlot.Guess;travel.Enter(c);
  travel.Update(c);Check(cueA==0&&cueB==0&&!travel.IsFinished,"Nothing fires at the start");
  player.Position=new Vector3(450,0,0);travel.Update(c);travel.Update(c);Check(cueA==1&&cueB==0,"The first call fires once past 60% of the way");
  player.Position=new Vector3(750,0,0);travel.Update(c);Check(cueB==1,"The second call fires past 70%");
  player.Task.LeaveVehicle();player.Position=new Vector3(995,0,0);travel.Update(c);Check(!travel.IsFinished&&travel.Label.Contains("Get back"),"On foot in the zone is not arrival");
  van.Position=new Vector3(995,0,0);player.SetIntoVehicle(van,VehicleSeat.Driver);van.Speed=10f;travel.Update(c);Check(!travel.IsFinished&&travel.Label.Contains("Stop"),"Rolling through the zone is not arrival");
  van.Speed=0f;travel.Update(c);Check(travel.IsFinished,"Stopped inside the zone arrives");

  // ---- Switch window: offered, shadowed, then required.
  Reset();crew=Roster();c=Context(crew);Use(crew,CrewSlot.Gohan);int shadowed=0,switched=0;
  var window=new SwitchWindowObjective("Take Guess",CrewSlot.Guess,8000,()=>shadowed++,()=>switched++);
  Check(window.KeepsOwnerOpen&&!window.RequiredCharacter.HasValue,"The window starts with no required character");
  window.Enter(c);window.Update(c);Check(shadowed==1&&!window.Forced&&window.Label.Contains("8s"),"Opening the window starts the shadow task and counts down");
  Game.GameTime+=8100;window.Update(c);Check(window.Forced&&window.RequiredCharacter==CrewSlot.Guess&&window.Label.Contains("Switch to Guess"),"The closed window requires the switch through the ordinary hand-off");
  Use(crew,CrewSlot.Guess);window.Update(c);Check(window.IsFinished&&switched==1,"Switching completes it and hands over");
  var trigger=new ReactionTrigger(()=>Game.GameTime>500,()=>switched++);Check(trigger.IsPassive,"A reaction trigger is passive");
  Game.GameTime=100;trigger.Update(c);Check(!trigger.Fired,"Not yet");Game.GameTime=600;trigger.Update(c);trigger.Update(c);Check(trigger.Fired&&switched==2,"It fires exactly once");

  // ---- Evidence and custody.
  Reset();var path=Path.Combine(root,"evidence.json");var state=CampaignState.Load(path);
  Check(state.EvidenceOf("millerDrive")==EvidenceState.None,"A fresh campaign holds no evidence");
  state.SetEvidence("millerDrive",EvidenceState.CopyHeld);Check(CampaignState.Load(path).EvidenceOf("millerDrive")==EvidenceState.CopyHeld,"Evidence survives a reload");
  Reset();crew=Roster();c=Context(crew);var probe=new PreserveProbe();probe.Begin(c);var kept=new Vehicle();probe.Register(kept);probe.Keep(kept);var gone=new Vehicle();probe.Register(gone);probe.Abort();
  Check(kept.Present&&kept.Released&&!gone.Present,"A preserved entity survives cleanup; an ordinary one is deleted");

  // ---- Context card after a skipped briefing and on a retry.
  Reset();crew=Roster();c=Context(crew);var cardState=CampaignState.Load(Path.Combine(root,"card.json"));var cat=new MissionCatalog();
  var def=new MissionDefinition{Info=new MissionInfo{Id="M04",Title="Severed Wire"},Factory=()=>new ChapterProbe("M04")};cat.All.Add(def);var manager=new MissionManager(c,cardState,cat);
  Check(manager.Start(def)&&c.Cutscenes.IsActive,"The briefing plays");c.Cutscenes.Skip();manager.Update();
  Check(MissionContextCard.IsShowing&&manager.CurrentStage==0,"Skipping the briefing shows the card as gameplay begins");
  MissionContextCard.Clear();manager.ForceFail("test");manager.Update();Check(manager.RetryAvailable,"Failed");manager.Start(def);
  Check(MissionContextCard.IsShowing,"A retry shows the recap");MissionContextCard.Clear();manager.Abort();

  // ---- M04 end to end: the reference mission.
  RunM04(killMiller:false);
  RunM04(killMiller:true);
 }

 static void RunM04(bool killMiller)
 {
  Reset();var crew=Roster();var c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"s2p-m04-"+killMiller+".json"));c.Vans=new CrewVan(c.State,c.Locations);
  var m4=new M04SeveredWire();Check(m4.Begin(c),"M04 sets up at the base with the crew in the van");
  var van=m4.Van;var guess=crew.PedFor(CrewSlot.Guess);var ice=crew.PedFor(CrewSlot.Ice);var gohan=crew.PedFor(CrewSlot.Gohan);
  Check(van!=null&&van.Model.Name=="granger"&&guess.CurrentVehicle==van&&ice.CurrentVehicle==van&&gohan.CurrentVehicle==van&&Game.Player.Character==guess,"The crew leaves the base together in the crew van with Guess driving");
  Check(m4.Miller!=null&&m4.Case!=null&&m4.Case.AttachedTo==m4.Miller&&m4.EndpointKind==MissionEndpoint.EscapeCheckpoint,"Miller stands at the meeting with the case in his hand; the mission ends at an escape checkpoint");
  var exit=c.Locations.Position("M04.ChaseCar");var basePoint=c.Locations.Position("Base.CypressFlats");
  guess.Position=basePoint;van.Position=basePoint;m4.Tick();Check(m4.CurrentStage==0&&!c.Dialogue.HasPending,"The drive begins with the lot far off and no call yet");
  var half=basePoint+(exit-basePoint)*0.5f;guess.Position=half;van.Position=half;m4.Tick();m4.Tick();
  Check(c.Dialogue.HasPending,"Halfway there Gohan calls where he will be dropped");
  guess.Position=exit;van.Position=exit;van.Speed=0f;m4.Tick();m4.Tick();
  Check(m4.CurrentStage==1&&ice.CurrentVehicle==null&&gohan.CurrentVehicle==null&&m4.Roles.For(CrewSlot.Ice).State==RoleState.Approaching,"Arrival drops Ice and Gohan off and sends them to their positions while Guess holds the exit");
  ice.Position=m4.Roles.For(CrewSlot.Ice).Point;gohan.Position=m4.Roles.For(CrewSlot.Gohan).Point;m4.Tick();m4.Tick();
  Check(m4.CurrentStage==2&&c.Cutscenes.IsActive,"With both in position the exchange plays before anyone touches it");
  c.Cutscenes.Update();c.Cutscenes.Skip();Check(c.Cutscenes.LastOutcome==SceneOutcome.Skipped&&m4.Case.AttachedTo==m4.Miller,"Skipping the exchange leaves Miller holding the case");
  m4.Tick();Check(m4.RequiredSwitch==CrewSlot.Gohan,"The breaker is Gohan's job: the switch is explained, not forced by a freeze");
  Interact(m4,c,CrewSlot.Gohan,c.Locations.Position("M04.Breaker"),5);
  Check(m4.CurrentStage==3&&m4.FlightStarted&&m4.Miller.Task.VehicleMissions==1&&!m4.Miller.IsInvincible,"Cutting the power is what makes Miller run: the escort turns, the buyer ducks, Miller goes for his car");
  Check(guess.Task.Chases==1&&m4.Roles.For(CrewSlot.Gohan).State==RoleState.Covering,"Ron's own AI is already on Miller while Gohan takes cover");
  c.Cutscenes.Stop();m4.Tick();Check(m4.CurrentStage==3&&!m4.RequiredSwitch.HasValue,"The switch is offered, not required, while the window is open");
  Game.GameTime+=M04SeveredWire.SwitchWindowMs+200;m4.Tick();Check(m4.RequiredSwitch==CrewSlot.Guess,"When the window closes the switch is required through the hand-off");
  Use(crew,CrewSlot.Guess);m4.Tick();m4.Tick();Check(m4.CurrentStage==4,"Taking Guess opens the pursuit");
  var millerCar=World.Vehicles.First(v=>v.Model.Name=="fugitive");
  if(killMiller)m4.Miller.IsDead=true;else millerCar.IsDriveable=false;
  m4.Tick();Check(m4.CurrentStage==5,killMiller?"A dead Miller ends the chase":"A disabled car ends the chase");
  Interact(m4,c,CrewSlot.Guess,m4.Miller.Position,3);
  Check(m4.CurrentStage==6&&m4.Case==null&&c.State.EvidenceOf("millerDrive")==EvidenceState.CopyHeld,"Recovering the drive stores it and records the evidence");
  if(!killMiller)Check(m4.Miller.Task.Flees==1,"A living Miller runs; nothing asks for his death");
  Game.Player.WantedLevel=2;m4.Tick();Check(Game.Player.WantedLevel==2&&m4.CurrentStage==6,"The heat is not cleared at a marker: the crew has to lose it");
  Game.Player.WantedLevel=0;Game.Player.Character.SetIntoVehicle(van,VehicleSeat.Driver);ice.SetIntoVehicle(van,VehicleSeat.RightFront);gohan.SetIntoVehicle(van,VehicleSeat.LeftRear);m4.Tick();m4.Tick();
  Check(m4.Status==MissionStatus.Passed&&van.Present&&van.Released&&millerCar.Present,"With the police lost and the crew aboard the mission passes; the van and Miller's car remain");
 }
}
