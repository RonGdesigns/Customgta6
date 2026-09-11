using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Math;

public static partial class StoryTests
{
 static void ControlChecks()
 {
  // ---- Ron's September 10 log: control was off when the prologue's briefing began (a ped change the same
  // tick), the scene restored "off", and M01 started with a player who could not move.
  Reset();var crew=Roster();var c=Context(crew);var state=CampaignState.Load(Path.Combine(root,"control.json"));var cat=new MissionCatalog();
  var def=new MissionDefinition{Info=new MissionInfo{Id="M02",Title="Probe"},Factory=()=>new ProbeMission()};cat.All.Add(def);var manager=new MissionManager(c,state,cat);
  Game.Player.CanControlCharacter=false;Check(manager.Start(def)&&c.Cutscenes.IsActive,"The briefing starts with control already off");
  c.Cutscenes.Skip();Check(!Game.Player.CanControlCharacter,"The scene restores what it found, which was off");
  manager.Update();Check(manager.IsRunning&&Game.Player.CanControlCharacter,"Gameplay begins with control on, whatever the hand-off left behind");
  // The same after a scene inside a running mission (Gohan's terminal: switch, then the recognition scene).
  Game.Player.CanControlCharacter=false;Check(c.Cutscenes.Play("M02","outro","Mid-mission"),"A mid-mission scene starts with control off");manager.Update();
  c.Cutscenes.Skip();Check(!Game.Player.CanControlCharacter,"Off again when it ends");
  manager.Update();Check(Game.Player.CanControlCharacter,"The first mission tick after a scene restores control");manager.Abort();
  // Every player-ped change asserts control.
  Reset();crew=Roster();var s=new SwitchController(crew);Game.Player.CanControlCharacter=false;World.CollisionReady=true;
  Check(s.TrySwitch(CrewSlot.Gohan,missionTransition:true)&&Game.Player.CanControlCharacter,"A switch asserts control after the ped change");
  string host=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","BloodlinesMain.cs"));
  int on=host.IndexOf("Game.Player.CanControlCharacter = true;",StringComparison.Ordinal),start=host.IndexOf("StartMission(m01);",StringComparison.Ordinal);
  Check(on>0&&start>on&&start-on<120,"The prologue hand-off hands the briefing control that is on");
  string roster=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Crew","CrewRoster.cs"));
  Check(roster.Contains("GameUtils.AssertPlayerControl(\"solo deployment\")")&&roster.Contains("GameUtils.AssertPlayerControl(\"crew deployment\")")&&roster.Contains("GameUtils.AssertPlayerControl(\"the story character's return\")"),"Deployments and the story character's return assert control after CHANGE_PLAYER_PED");
  // The HUD count in M01 is a count.
  string m01=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act1","M01GhostInTheDockyard.cs"));
  Check(!m01.Contains("SwitchAfterTerminal")&&!m01.Contains("TrySwitch("),"M01 never switches the player by itself after Gohan's terminal");
  Check(m01.Contains("GameUtils.Subtitle(\"~y~Hostiles: \" + _guards.Count, 500);")&&!m01.Contains("Mateo is in the launch."),"The hostiles line is a count, not an announcement about Mateo");
 }
}
