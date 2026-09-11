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

public static partial class StoryTests
{
 static void OpenSliceChecks()
 {
  // ---- The laptop sits on the table: the item's base meets the surface's top bound, in the scene and in the mission.
  var table=new Prop{Position=new Vector3(10,20,5),ForwardVector=new Vector3(0,1,0)};
  Check(PropPlacement.OnTop(table,new Model("prop_table_03"),new Model("prop_laptop_01a"))==new Vector3(10,20,5.76f),"On top means the table's top bound plus the laptop's base offset, not a fixed height above the table's origin");
  Reset();var crew=Roster();var c=Context(crew);c.Cutscenes.Play("M01","intro","Opening");
  var desk=World.Props.First(p=>p.Model.Name=="prop_table_03");var laptop=World.Props.First(p=>p.Model.Name=="prop_laptop_01a");
  Check(Math.Abs(laptop.Position.Z-desk.Position.Z-0.76f)<0.001f,"The cold open's laptop rests on its table");c.Cutscenes.Stop();
  Reset();crew=Roster();c=Context(crew);var m1=new M01GhostInTheDockyard();Check(m1.Begin(c),"M01 sets up");
  desk=World.Props.First(p=>p.Model.Name=="prop_table_03");laptop=World.Props.First(p=>p.Model.Name=="prop_laptop_01a");
  Check(Math.Abs(laptop.Position.Z-desk.Position.Z-0.76f)<0.001f,"The mission's laptop rests on the same table the same way");
  string m01=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act1","M01GhostInTheDockyard.cs"));
  Check(!m01.Contains("RecognitionWindowSeconds")&&!m01.Contains("_recognitionStartedAt")&&!m01.Contains("_mateoCover"),"The firefight clock and the cover hold are gone from the source");
  Check(m1.OutroBlocking()==null,"With Gohan on foot the aftermath keeps the speaker framing");
  var car=new Vehicle();crew.PedFor(CrewSlot.Gohan).SetIntoVehicle(car,VehicleSeat.LeftRear);var outro=m1.OutroBlocking();
  Check(outro!=null&&outro.Steps.Count==1&&outro.Steps[0] is ShotStep,"With the crew in the prototype the aftermath opens on a shot through Gohan's window");m1.Abort();
  string manager=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","MissionManager.cs"));
  int ask=manager.IndexOf("outro = _current.OutroBlocking()",StringComparison.Ordinal);
  int finish=ask<0?-1:manager.IndexOf("Finish();",ask,StringComparison.Ordinal);
  int play=manager.IndexOf("_context.Cutscenes.Play(outroId, \"outro\", \"Aftermath: \" + outcomeTitle, null, null, outro)",StringComparison.Ordinal);
  Check(ask>=0&&finish>ask&&play>finish,"The manager captures aftermath blocking before teardown and passes it to the final scene");

  // ---- Prologue: the message is read inside the starter apartment.
  Reset();crew=Roster();c=Context(crew);var save=CampaignState.Load(Path.Combine(root,"prologue-room.json"));var access=new ApartmentAccess(crew);int handoffs=0;var home=new Vector3(291,-1078,29);
  var prologue=new PrologueSequence(crew,c.Cutscenes,c.Locations,save,()=>home,access){Finished=()=>handoffs++};
  prologue.Begin();c.Cutscenes.Skip();prologue.Update();var ride=Game.Player.Character.CurrentVehicle;ride.Position=home;Game.Player.Character.Position=home;ride.Speed=0;prologue.Update();
  Check(prologue.Current==PrologueSequence.Phase.Homecoming&&c.Cutscenes.IsActive,"Arriving starts the homecoming");
  c.Cutscenes.Skip();prologue.Update();
  Check(prologue.Current==PrologueSequence.Phase.Interior&&access.Busy&&!Game.Player.CanControlCharacter&&handoffs==0,"After the door the home system streams the starter room with Ron held behind the fade");
  Function.InteriorReady=true;World.CollisionReady=true;Game.GameTime+=300;access.Update();access.Update();
  Check(access.Inside&&!access.Busy,"The room is ready and Ron is in it");
  GTA.UI.Screen.Subtitle=null;prologue.Update();
  Check(c.Cutscenes.IsActive&&prologue.Current==PrologueSequence.Phase.Interior&&handoffs==0,"Inside, the message arrives: the call scene plays in the room");
  c.Cutscenes.Update();Check(string.IsNullOrEmpty(GTA.UI.Screen.Subtitle),"No line plays before Ron has crossed the room and stopped");
  c.Cutscenes.Skip();prologue.Update();
  Check(handoffs==1&&save.PrologueComplete&&!prologue.IsActive&&access.Inside,"Skipping lands the job as read; the prologue hands off from inside the room and leaves the host to cut to the dock");
  access.Cancel();
  // The room never loads: control comes back at the door and the message is read there.
  Reset();crew=Roster();c=Context(crew);save=CampaignState.Load(Path.Combine(root,"prologue-noroom.json"));access=new ApartmentAccess(crew);handoffs=0;
  prologue=new PrologueSequence(crew,c.Cutscenes,c.Locations,save,()=>home,access){Finished=()=>handoffs++};
  prologue.Begin();c.Cutscenes.Skip();prologue.Update();ride=Game.Player.Character.CurrentVehicle;ride.Position=home;Game.Player.Character.Position=home;ride.Speed=0;prologue.Update();c.Cutscenes.Skip();prologue.Update();
  Function.InteriorReady=false;World.CollisionReady=false;Game.GameTime+=12100;access.Update();
  Check(!access.Busy&&!access.Inside&&Game.Player.Character.Position==home&&Game.Player.CanControlCharacter,"The timed-out room returns Ron to the door with control");
  prologue.Update();
  Check(c.Cutscenes.IsActive&&GameUtils.Message.Contains("did not load")&&handoffs==0,"The message is read at the door instead, and the player is told why");
  c.Cutscenes.Skip();prologue.Update();Check(handoffs==1&&save.PrologueComplete,"An unloaded room still ends with the job read and the hand-off to M01");
  string host=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","BloodlinesMain.cs"));
  int stop=host.IndexOf("_homes.StopApartment();",StringComparison.Ordinal),place=host.IndexOf("var placement = PrologueSequence.PlaceForColdOpen",StringComparison.Ordinal);
  Check(host.Contains("_homes.Position(CrewSlot.Guess), _homes.Apartment)")&&stop>0&&place>stop&&host.IndexOf("GameUtils.FadeOut(900);",StringComparison.Ordinal)<stop&&place-stop<400,"The host gives the prologue the home system and leaves the room behind the fade before the cut to the dock");

  // ---- Data: the call and the stash beat are generated; Ron is alone for the call.
  var scenes=File.ReadAllLines(Path.Combine(dataDir,"scenes.tsv"));
  var call=scenes.Where(l=>l.StartsWith("M01_SCENE_CALL_")).ToList();var arrival=scenes.Where(l=>l.StartsWith("M01_SCENE_ARRIVAL_")).ToList();var stash=scenes.Where(l=>l.StartsWith("M02_SCENE_STASH_")).ToList();
  Check(call.Count==3&&call.All(l=>l.Contains("\tGUESS\t"))&&arrival.Count==1,"The job is read in the call, by Ron alone; the door is only the door");
  Check(stash.Count==3&&stash[0].Contains("\tGUESS\t")&&stash[1].Contains("\tGOHAN\t")&&stash[2].Contains("\tICE\t"),"The stash beat has its three lines in order");
 }
}
