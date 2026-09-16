using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using GTA;
using GTA.Math;

public static partial class RegressionTests
{
 static void PlacementEditorChecks(string root)
 {
  FlyModeTeleportChecks(root);
  SweepChecks(root);
  Reset();string dir=Path.Combine(root,"placement");Directory.CreateDirectory(dir);
  File.WriteAllText(Path.Combine(dir,"locations.tsv"),"key\tx\ty\tz\theading\tkind\tstatus\tdistrict_hint\nM05.LightCrew\t100\t200\t10\t90\tland\testimate\tbeach\nM03.DepotGate\t200\t300\t20\t0\tland\testimate\tyard\nM03.HaulerSpawn\t200\t300\t20\t0\tland\testimate\ttruck\nM06.AlleyHold\t300\t400\t30\t0\tland\testimate\talley\n");
  string path=Path.Combine(dir,"survey.ini");var book=LocationBook.Load(dir,Path.Combine(dir,"none.ini"));var editor=new SurveyMode(book,path);
  var original=book.Position("M05.LightCrew");
  Check(editor.BeginPlacement("M05.LightCrew")&&editor.Draft.SpawnCount==4,"Enemy placement opens a separate draft with its actual default quantity");
  Game.Player.Character.Position=new Vector3(110,220,12);editor.PlaceAtPlayer();editor.AdjustPlacement(20,5,45);
  Check(book.Position("M05.LightCrew")==original&&book.Get("M05.LightCrew").SpawnCount==-1,"Moving a preview and adjusting enemies does not change the active location book");
  editor.Stop();Check(!File.Exists(path)&&book.Position("M05.LightCrew")==original,"Cancel discards the entire placement draft without saving");
  editor.BeginPlacement("M05.LightCrew");editor.PlaceAtPlayer();editor.AdjustPlacement(100,100,-450);
  Check(editor.Draft.SpawnCount==16&&editor.Draft.SpawnRadius==60&&editor.Draft.Heading>=0&&editor.Draft.Heading<360,"Held radius/count controls are bounded and heading wraps");
  var draft=editor.Draft;
  Check(Enumerable.Range(0,draft.SpawnCount).All(i=>MissionPlacement.GroupPoint(draft,i).DistanceTo(draft.Position)<=draft.SpawnRadius),"All preview enemy dots lie inside the selected circle");
  Check(editor.SavePlacement()&&!editor.IsEditing,"Saving commits the draft and exits placement mode");
  var reload=LocationBook.Load(dir,Path.Combine(dir,"none.ini"),path);
  Check(reload.Position("M05.LightCrew")==new Vector3(110,220,12)&&reload.Get("M05.LightCrew").SpawnCount==16&&reload.Get("M05.LightCrew").SpawnRadius==60,"Position, facing, radius and count survive a real INI reload");
  Check(editor.UndoPlacement()&&!MissionPlacement.HasFormation(book.Get("M05.LightCrew"))&&book.Position("M05.LightCrew")==original,"Undo restores the pre-edit formation and position");
  reload=LocationBook.Load(dir,Path.Combine(dir,"none.ini"),path);
  Check(reload.Position("M05.LightCrew")==original&&!MissionPlacement.HasFormation(reload.Get("M05.LightCrew")),"Undo also removes the saved override across reload");
  var fallback=new Vector3(999,888,777);
  Check(book.Get("M03.ArrivalSpawn1")!=null&&MissionPlacement.Position(book,"M03.ArrivalSpawn1",fallback)==fallback,"Unsaved vehicle entry slots preserve the mission's dynamic entry behavior");
  editor.BeginPlacement("M03.ArrivalSpawn1");Game.Player.Character.Position=new Vector3(240,360,20);Game.Player.Character.Heading=180;editor.PlaceAtPlayer();editor.SavePlacement();
  reload=LocationBook.Load(dir,Path.Combine(dir,"none.ini"),path);
  Check(MissionPlacement.Position(reload,"M03.ArrivalSpawn1",fallback)==new Vector3(240,360,20)&&MissionPlacement.Heading(reload,"M03.ArrivalSpawn1",0)==180,"Saved incoming vehicle position and heading replace dynamic defaults after reload");
  Check(MissionPlacement.TravelTimeout(reload,"M03.ArrivalSpawn1","M03.ArrivalStop1",Vector3.Zero,new Vector3(900,0,0))==85000,"A distant edited entry receives enough travel time instead of unloading after fifteen seconds");
  editor.BeginPlacement("M03.Crate1");editor.AdjustPlacement(100,100,0,.5f);
  Check(editor.Draft.SpawnCount==-1&&editor.Draft.SpawnRadius==-1,"Prop placement cannot accidentally alter enemy quantities");editor.Stop();
  var failed=new SurveyMode(book,Path.Combine(dir,"missing-directory","survey.ini"));failed.BeginPlacement("M05.LightCrew");failed.PlaceAtPlayer();
  Check(!failed.SavePlacement()&&book.Position("M05.LightCrew")==original&&book.Get("M05.LightCrew").SpawnCount==-1&&failed.IsEditing,"A failed disk write rolls the book back and retains the draft for retry");failed.Stop();
  Check(editor.BeginPlacementTour("M03")&&editor.PlacementProgress.StartsWith("1/"),"Survey all creates an ordered mission placement queue");
  string first=editor.Draft.Key;editor.AdjustPlacement(0,0,5);
  Check(!editor.MovePlacement(1,false)&&editor.Draft.Key==first&&editor.PlacementDirty,"Next cannot discard an unsaved placement");
  Check(editor.SavePlacement(true)&&editor.IsEditing&&!editor.PlacementDirty,"Tour save stays in the editor with committed values");
  Check(editor.MovePlacement(1,false)&&editor.Draft.Key!=first,"Tour advances one spot at a time after save");
  Check(editor.MovePlacement(-1,false)&&editor.Draft.Key==first,"Previous returns to the same saved placement");
  editor.AdjustPlacement(0,0,5);editor.ResetPlacementDraft();Check(!editor.PlacementDirty,"Discard restores the draft without a disk write");
  editor.TeleportToCurrent();Check(editor.IsEditing&&!editor.MovePlacement(1,false),"Teleport retains editing state and blocks navigation until complete");editor.CancelTeleport();
  editor.Stop();
  Check(File.Exists(path+".bak"),"Replacing a saved placement keeps the prior survey file as a backup");
 }

 /// <summary>
 /// The teleport, with the survey camera up. Ron reported it plainly: in fly mode it does
 /// not teleport. Two faults reading as one - the camera is a separate entity and stayed
 /// looking at where he had been standing, and it holds the streaming focus, so the
 /// collision this teleport waits on loaded nowhere near the man and the four-second
 /// timeout put him back.
 /// </summary>
 static void FlyModeTeleportChecks(string root)
 {
  Reset();string dir=Path.Combine(root,"flyteleport");Directory.CreateDirectory(dir);
  File.WriteAllText(Path.Combine(dir,"locations.tsv"),"key\tx\ty\tz\theading\tkind\tstatus\tdistrict_hint\nM60.Wave1\t110\t-1830\t21\t180\tland\testimate\tDavis\nM60.Wave2\t60\t-1870\t20\t140\tland\testimate\tDavis\n");
  var book=LocationBook.Load(dir,Path.Combine(dir,"none.ini"));
  var editor=new SurveyMode(book,Path.Combine(dir,"survey.ini"));
  Game.Player.Character.Position=new Vector3(0,0,30);
  GameplayCamera.Position=new Vector3(0,0,40);GameplayCamera.Rotation=new Vector3(-30,0,0);

  // ---- The camera on its own: sent to a point, it arrives looking at it rather than
  // inside it, and never below it - underground or on a deck, below is the floor.
  var flying=new SurveyCamera();
  Check(!flying.MoveTo(new Vector3(100,100,20)),"A camera that is not up cannot be sent anywhere");
  Check(flying.Take(),"The survey camera comes up");
  var target=new Vector3(120,-40,18);
  Check(flying.MoveTo(target),"and can be sent to a key");
  Check(flying.Position.DistanceTo(target)<=SurveyCamera.Standoff+.01f,"It arrives within the standoff of the point, not on top of it");
  Check(flying.Position.Z>=target.Z,"and never below it");
  var parked=flying.Position;flying.ReturnTo(parked+new Vector3(5,0,0));
  Check(Math.Abs(flying.Position.X-(parked.X+5f))<.01f,"and it can be put back where it was");
  flying.Release();Check(!flying.IsFlying,"The view goes back");

  // ---- And through the survey, which is where the fault was.
  Check(editor.BeginPlacement("M60.Wave1"),"A wave opens in the placement editor");
  Check(editor.ToggleCamera()&&editor.Camera.IsFlying,"with the camera up");
  var before=editor.Camera.Position;
  var key=book.Position("M60.Wave1");
  World.CollisionReady=false;Game.GameTime=100000;
  editor.TeleportToCurrent();
  Check(editor.IsTeleporting,"The teleport starts");
  Check(editor.Camera.Position!=before,"and the camera goes with it rather than staying where he was");
  Check(editor.Camera.Position.DistanceTo(key)<=SurveyCamera.Standoff+.01f,"looking at the key it was sent to");
  World.CollisionReady=true;Game.GameTime+=400;editor.Update();
  Check(!editor.IsTeleporting,"It lands once the destination has collision");
  Check(Game.Player.Character.Position.DistanceTo(key)<2f,"with the man at the key");
  Check(editor.Camera.Position.DistanceTo(key)<=SurveyCamera.Standoff+.01f,"and the camera still on it");

  // ---- A rollback undoes both. A camera left at a destination he was not moved to is
  // outside its own leash, looking at somewhere he is not. The camera is flown off the key
  // first, within the leash, so there is something for the rollback to restore.
  Game.Player.Character.Position=new Vector3(90,-1810,20);
  editor.Camera.ReturnTo(new Vector3(70,-1800,44));
  var away=editor.Camera.Position;var stood=Game.Player.Character.Position;
  World.CollisionReady=false;Game.GameTime+=1000;
  editor.TeleportToCurrent();
  Check(editor.Camera.Position!=away,"A second teleport moves the camera again");
  Game.GameTime+=5000;editor.Update();
  Check(!editor.IsTeleporting,"Terrain that never loads gives up");
  Check(Game.Player.Character.Position.DistanceTo(stood)<.01f,"and puts the man back");
  Check(editor.Camera.Position.DistanceTo(away)<.01f,"and the view with him");
  editor.Stop();
  Check(!editor.Camera.IsFlying,"Finishing the survey lands the camera");
 }

 /// <summary>
 /// The sweep, and accepting a spot in place. 1,062 of 1,091 keys are estimates and the
 /// median mission has nine of them, so the slow half is flying to all nine to find the two
 /// that are wrong. This visits them and says which two.
 /// </summary>
 static void SweepChecks(string root)
 {
  Reset();string dir=Path.Combine(root,"sweep");Directory.CreateDirectory(dir);
  File.WriteAllText(Path.Combine(dir,"locations.tsv"),"key\tx\ty\tz\theading\tkind\tstatus\tdistrict_hint\nM60.Ally1\t100\t-1820\t21\t0\tland\testimate\tDavis\nM60.Wave1\t110\t-1830\t21\t180\tland\testimate\tDavis\nM60.Wave2\t60\t-1870\t20\t140\tland\testimate\tDavis\n");
  var book=LocationBook.Load(dir,Path.Combine(dir,"none.ini"));
  string ini=Path.Combine(dir,"survey.ini");
  var editor=new SurveyMode(book,ini);
  Game.Player.Character.Position=new Vector3(100,-1815,21);
  GameplayCamera.Position=new Vector3(100,-1815,31);GameplayCamera.Rotation=new Vector3(-40,0,0);
  Game.GameTime=200000;World.CollisionReady=true;GTA.Native.Function.ZoneName="DAVIS";GTA.Native.Function.WaterHeight=null;

  // ---- The probes, injected the way the host injects them. Two of the three spots are
  // standing on the street; M60.Wave2 has a deckhead a meter over it, which is the fault
  // that put two of M45's men inside the hull.
  SurveyMode.SurfaceProbe=(at,reach)=>at.Z-0.3f;
  SurveyMode.HeadroomProbe=(at,height)=>!(Math.Abs(at.X-60f)<1f&&height>1f);
  SurveyMode.InteriorProbe=at=>false;
  SurveyMode.ClearanceProbe=at=>18f;

  var clean=editor.Read(book.Get("M60.Wave1"),book.Position("M60.Wave1"));
  Check(clean.Fits&&clean.Zone=="DAVIS","A reading gathers the probes and the zone");
  Check(clean.NearestKey=="Ally1","and names the nearest key of the same mission");
  Check(!editor.Read(book.Get("M60.Wave2"),book.Position("M60.Wave2")).Fits,
   "A spot with a deckhead a meter over it does not check out");

  // ---- The sweep itself. It teleports through the keys, so each frame is one step.
  Check(editor.BeginSweep("M60")&&editor.IsSweeping,"The sweep opens on a mission");
  Check(editor.Camera.IsFlying||editor.ToggleCamera(),"with the camera available");
  for(int i=0;i<80&&editor.IsSweeping;i++){Game.GameTime+=300;editor.Update();}
  Check(!editor.IsSweeping,"and finishes on its own");
  Check(editor.SweepResults.Count()==3,"having read every spot in the mission");
  Check(editor.SweepResults.Count(r=>!r.Fits)==1,"with exactly the one that needs a look flagged");
  Check(editor.SweepResults.First(r=>!r.Fits).Key=="M60.Wave2","and it is the right one");
  Check(File.Exists(Path.Combine(dir,"Bloodlines.Survey-Check.txt")),"A worklist is written beside the survey ini");
  string report=File.ReadAllText(Path.Combine(dir,"Bloodlines.Survey-Check.txt"));
  Check(report.Contains("M60.Wave2")&&report.Contains("CHECK"),"naming the spot to visit");
  Check(report.Contains("still yours to say"),
   "and saying plainly that placeable is not the same as the right place for the beat");

  // ---- Accepting in place. A capture that does not move the point, which is what having
  // looked at it actually means.
  int taken=editor.AcceptClean();
  Check(taken==2,"Every spot that checked out is accepted where it is");
  Check(book.Get("M60.Wave1").Status==LocationStatus.Surveyed,"and stops being called a guess");
  Check(book.Get("M60.Wave2").Status!=LocationStatus.Surveyed,"while the one that did not is left alone");
  Check(book.Position("M60.Wave1")==new Vector3(110,-1830,21),"Accepting moves nothing");
  Check(File.ReadAllText(ini).Contains("M60.Wave1"),"and it is written to the survey file");
  Check(editor.AcceptClean()==0,"Accepting twice takes nothing the second time");

  // ---- And the one spot that did not check out cannot be accepted by pressing the same row.
  editor.Start(new[]{"M60.Wave2"});
  Check(!editor.AcceptCurrent(),"A spot that has not checked out is refused in place");
  Check(book.Get("M60.Wave2").Status!=LocationStatus.Surveyed,"and stays an estimate");
  SurveyMode.HeadroomProbe=(at,height)=>true;
  Check(editor.AcceptCurrent(),"Once it reads clean it can be accepted");
  Check(book.Get("M60.Wave2").Status==LocationStatus.Surveyed,"and it is recorded");

  // ---- A capture that contradicts its own key holds for a second press rather than
  // writing quietly. The kind is the thing nothing used to check.
  Reset();Game.GameTime=400000;
  File.WriteAllText(Path.Combine(dir,"locations.tsv"),"key\tx\ty\tz\theading\tkind\tstatus\tdistrict_hint\nM45.Approach\t-1600\t5300\t60\t0\tair\testimate\tPaleto Cove\n");
  var air=LocationBook.Load(dir,Path.Combine(dir,"none2.ini"));
  var second=new SurveyMode(air,Path.Combine(dir,"survey2.ini"));
  SurveyMode.SurfaceProbe=(at,reach)=>at.Z-1f;   // standing on something: not an air spawn
  SurveyMode.HeadroomProbe=(at,height)=>true;
  second.Start("M45");
  Game.Player.Character.Position=new Vector3(-1600,5300,60);
  second.Capture();
  Check(air.Get("M45.Approach").Status!=LocationStatus.Surveyed,
   "An air key captured a meter off the deck is held rather than written");
  Game.GameTime+=1000;second.Capture();
  Check(air.Get("M45.Approach").Status==LocationStatus.Surveyed,
   "and a deliberate second press saves it anyway, because the author may know better than the probe");

  SurveyMode.SurfaceProbe=null;SurveyMode.HeadroomProbe=null;SurveyMode.InteriorProbe=null;SurveyMode.ClearanceProbe=null;
 }
}
