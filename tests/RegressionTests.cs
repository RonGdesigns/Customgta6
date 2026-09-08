using System;
using System.IO;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Abilities;
using Bloodlines.Missions;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;
public sealed class ProbeObjective:Objective {public ProbeObjective():base("probe"){}public override void Update(MissionContext c){Complete();}}
public static class RegressionTests {
 static int checks;
 static void Check(bool ok,string name){if(!ok)throw new Exception("FAIL: "+name);checks++;Console.WriteLine("PASS: "+name);}
 static void Reset(){Game.GameTime=100;Game.Player=new Player();Game.Arrested=false;Game.TimeScale=1;World.Nearby=new Ped[0];World.CollisionReady=false;Function.Values.Clear();Function.ThrowOnce=null;GameUtils.Faded=false;}
 static CompanionController AI()=>new CompanionController(new ModConfig());
 static void CompanionTests(){
  Reset();var ai=AI();var c=new Ped{RelationshipGroup=1};var leader=new Ped{RelationshipGroup=1};
  ai.Update(CrewSlot.Gohan,c,leader);Game.GameTime=8101;ai.Update(CrewSlot.Gohan,c,leader);int clears=c.Task.Clears;
  Game.GameTime=8102;ai.Update(CrewSlot.Gohan,c,leader);Check(c.Task.Clears==clears,"Follow task does not reset every frame after eight seconds");
  Reset();ai=AI();c=new Ped{RelationshipGroup=1};leader=new Ped{RelationshipGroup=1};var enemy=new Ped{RelationshipGroup=2,CombatTarget=leader};World.Nearby=new[]{enemy};
  ai.Update(CrewSlot.Gohan,c,leader);Check(c.Task.Fights==1&&c.Task.LastTarget==enemy,"Companion engages an attacker targeting the player");
  Game.GameTime+=50;ai.Update(CrewSlot.Gohan,c,leader);Check(c.Task.Fights==1,"Combat task is not cleared each frame");
  Reset();ai=AI();c=new Ped{RelationshipGroup=1};leader=new Ped{RelationshipGroup=1};World.Nearby=new[]{new Ped{RelationshipGroup=2}};
  ai.Update(CrewSlot.Gohan,c,leader);Check(c.Task.Fights==0,"Companions do not attack neutral bystanders");
  ai.TakeControl(CrewSlot.Gohan);ai.Refresh(CrewSlot.Gohan);World.Nearby=new[]{new Ped{RelationshipGroup=2,CombatTarget=leader}};
  ai.Update(CrewSlot.Gohan,c,leader);Check(c.Task.Fights==0&&ai.StateOf(CrewSlot.Gohan)==CompanionState.Scripted,"Refreshing AI preserves mission control");
  Reset();ai=AI();var car=new Vehicle();leader=new Ped{CurrentVehicle=car};c=new Ped();var other=new Ped();
  ai.Update(CrewSlot.Gohan,c,leader);ai.Update(CrewSlot.Guess,other,leader);
  Check(c.Task.Enters==1&&c.Task.Warps==0,"Nearby stopped car uses normal entry, no teleport");
  Check(c.Task.LastSeat!=other.Task.LastSeat,"Two companions reserve different passenger seats");
  Game.GameTime=6200;ai.Update(CrewSlot.Gohan,c,leader);Check(c.Task.Warps==0,"No six-second premature boarding warp");
  Game.GameTime=12500;ai.Update(CrewSlot.Gohan,c,leader);Check(c.Task.Warps==0,"An active door animation gets additional time");
  c.Entering=false;Game.GameTime=20101;ai.Update(CrewSlot.Gohan,c,leader);Check(c.Task.Warps==1,"Failed boarding eventually uses bounded fallback");
  Reset();ai=AI();leader=new Ped{CurrentVehicle=new Vehicle()};c=new Ped{Position=new Vector3(100,0,0)};
  ai.Update(CrewSlot.Gohan,c,leader);Check(c.Task.Warps==1,"Distant companion can use catch-up fallback");
 }
 static void ObjectiveTests(){
  Reset();var ctx=new MissionContext();var probe=new ProbeObjective();probe.Update(ctx);probe.Enter(ctx);Check(!probe.IsFinished,"Objective re-entry clears completed status");
  var target=new Entity{Position=new Vector3(30,0,0)};var shadow=new ShadowTargetObjective("hold",()=>target,60,12,"failed",12,240);
  shadow.Enter(ctx);shadow.Update(ctx);Game.GameTime=4100;target.Position=new Vector3(5,0,0);shadow.Update(ctx);Check(!shadow.IsFinished,"Too-close formation cannot complete");
  Game.GameTime=5000;target.Position=new Vector3(30,0,0);shadow.Update(ctx);Game.GameTime=16000;shadow.Update(ctx);Check(!shadow.IsFinished,"Hold clock resets when formation is broken");
  Game.GameTime=17000;shadow.Update(ctx);Check(shadow.Status==ObjectiveStatus.Complete,"Continuous valid formation completes after twelve seconds");
  Reset();target.Position=new Vector3(-100,5000,700);Game.Player.Character.Position=new Vector3(2130,4790,41);shadow=new ShadowTargetObjective("climb",()=>target,60,12,"failed",12,240);shadow.Enter(ctx);
  Game.GameTime=9000;shadow.Update(ctx);Check(!shadow.IsFinished,"M27 approach does not fail after eight seconds");
  Game.GameTime=240101;shadow.Update(ctx);Check(shadow.Status==ObjectiveStatus.Failed,"Approach timeout remains bounded");
 }
 static void SurveyTests(string dir){
  Reset();Directory.CreateDirectory(dir);File.WriteAllText(Path.Combine(dir,"locations.tsv"),"key\tx\ty\tz\theading\tkind\tstatus\tdistrict_hint\nM01.CraneNest\t1\t2\t3\t0\tland\testimate\tDocks\nM01.ExitPoint\t5\t6\t7\t0\tland\testimate\tDocks\n");
  string user=Path.Combine(dir,"custom.ini"),saved=Path.Combine(dir,"survey.ini");var data=new CampaignData();data.Anchors["Ice: Roost 4"]=new Vector3(10,20,30);
  var book=LocationBook.Load(dir,user,saved,data);Check(book.Position("M01.CraneNest")==new Vector3(10,20,30),"Survey uses the actual M01 anchor by default");
  var survey=new SurveyMode(book,saved);var original=Game.Player.Character.Position;survey.Start("M01");
  Check(World.LastBlip.ShowRoute&&!World.LastBlip.IsShortRange,"Survey selection creates a visible GPS route");
  Check(Game.Player.Character.Position==original,"Selecting a survey location does not unexpectedly teleport");
  survey.TeleportToCurrent();Check(survey.IsTeleporting&&Game.Player.Character.IsPositionFrozen,"Teleport holds player while terrain streams");
  Game.GameTime+=4100;survey.Update();Check(!survey.IsTeleporting&&Game.Player.Character.Position==original&&!Game.Player.Character.IsPositionFrozen&&!Game.Player.Character.IsInvincible,"Terrain timeout rolls back and restores player flags");
  survey.TeleportToCurrent();World.CollisionReady=true;Game.GameTime+=300;survey.Update();Check(!survey.IsTeleporting&&!Game.Player.Character.IsPositionFrozen,"Loaded terrain finishes teleport and unfreezes player");
  string key=survey.Current.Key;Game.Player.Character.Position=new Vector3(44,55,66);survey.Capture();
  var loaded=LocationBook.Load(dir,user,saved,data);Check(loaded.Position(key)==new Vector3(44,55,66),"Capture survives reload and takes precedence over defaults");
  survey.Stop();Check(!SurveyMode.IsSurveyRunning&&!World.LastBlip.Present,"Stopping survey removes its map marker");
 }
 static void DeathTests(){
  Reset();var crew=new CrewRoster{ActivePed=Game.Player.Character};var missions=new MissionManager();var abilities=new AbilityController();var switcher=new SwitchController();var dialogue=new DialogueDirector();var death=new DeathController(new ModConfig(),crew,missions,abilities,switcher,dialogue);
  crew.ActivePed.IsDead=true;Game.Player.IsDead=true;death.Update();Check(death.IsHandling&&abilities.Stops==1&&switcher.Cancels==1&&!Game.Player.CanControlCharacter,"Recovery stops abilities and switches before disabling control");
  Game.GameTime+=901;death.Update();Game.GameTime+=1501;death.Update();Check(!death.IsHandling&&!crew.ActivePed.IsDead&&Game.Player.CanControlCharacter&&!GameUtils.Faded,"Death completes with living controllable player and visible screen");
  Check(missions.Failures==1&&crew.Regroups==1,"Unsupported checkpoint fails mission and regroups instead of fake restore");
  death.Update();Check(death.DeathCount==1,"Successful recovery does not repeat on the next frame");
  Game.Arrested=true;death.Update();Check(death.IsHandling,"Arrest enters the same recovery path");death.Cancel();Check(!death.IsHandling&&Game.Player.CanControlCharacter&&!(bool)Function.Values[Hash.PAUSE_DEATH_ARREST_RESTART],"Cancelling recovery restores restart and control");
  Reset();crew=new CrewRoster{ActivePed=Game.Player.Character,CanRevive=false};death=new DeathController(new ModConfig(),crew,new MissionManager(),new AbilityController(),new SwitchController(),new DialogueDirector());crew.ActivePed.IsDead=true;death.Update();Game.GameTime+=901;death.Update();Game.GameTime+=1501;death.Update();
  Check(crew.Dismissals==1&&!death.IsHandling&&!GameUtils.Faded&&Game.Player.CanControlCharacter,"Revive failure returns to story and opens the screen");
  Reset();crew=new CrewRoster{ActivePed=Game.Player.Character};death=new DeathController(new ModConfig(),crew,new MissionManager(),new AbilityController(),new SwitchController(),new DialogueDirector());Function.ThrowOnce=Hash.SET_FADE_OUT_AFTER_ARREST;death.Update();
  Check(crew.Dismissals==1&&!(bool)Function.Values[Hash.PAUSE_DEATH_ARREST_RESTART],"Partial native failure still releases restart suppression");
 }
 public static int Main(string[] args){try{CompanionTests();ObjectiveTests();SurveyTests(args[0]);DeathTests();Console.WriteLine(checks+" checks passed (stand-ins; live GTA validation still required).");return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
