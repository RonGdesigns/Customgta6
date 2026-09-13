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
}
