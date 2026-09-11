using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Math;
using GTA.Native;
public static partial class StoryTests
{
 static void RoomChecks()
 {
  // ---- The starter room is switched on for the visit and put back after it.
  Reset();var crew=Roster();var access=new ApartmentAccess(crew);var player=Game.Player.Character;var inside=new Vector3(347,-999,-99);
  Function.InteriorId=55042;Function.InteriorReady=true;World.CollisionReady=true;Function.InteriorDisabled=true;Function.InteriorCapped=true;Function.Calls.Clear();
  Check(access.Begin(inside,null,true,null,180f),"Entry begins with a heading for the room");
  Game.GameTime+=300;access.Update();
  Check(Function.Calls.Any(c=>c.Item1==Hash.DISABLE_INTERIOR&&(int)c.Item2[0]==55042&&(bool)c.Item2[1]==false)&&Function.Calls.Any(c=>c.Item1==Hash.CAP_INTERIOR&&(bool)c.Item2[1]==false),"A room Story Mode ships switched off is enabled and uncapped before anyone waits on it");
  Check(Function.Calls.FindIndex(c=>c.Item1==Hash.DISABLE_INTERIOR)<Function.Calls.FindIndex(c=>c.Item1==Hash.PIN_INTERIOR_IN_MEMORY),"The room is enabled before it is pinned");
  access.Update();
  Check(access.Inside&&!access.Busy&&player.Position==inside&&Math.Abs(player.Heading-180f)<0.01f,"Ron stands inside facing the way the room's entry says");
  Function.Calls.Clear();access.Begin(access.ExitPosition,null,false);Game.GameTime+=300;access.Update();access.Update();
  Check(!access.Inside&&Function.Calls.Any(c=>c.Item1==Hash.DISABLE_INTERIOR&&(bool)c.Item2[1]==true)&&Function.Calls.Any(c=>c.Item1==Hash.CAP_INTERIOR&&(bool)c.Item2[1]==true),"Leaving puts the room back the way Story Mode had it");
  Function.InteriorDisabled=false;Function.InteriorCapped=false;Function.Calls.Clear();
  access.Begin(inside,null,true);Game.GameTime+=300;access.Update();access.Update();
  Check(access.Inside&&!Function.Calls.Any(c=>c.Item1==Hash.DISABLE_INTERIOR||c.Item1==Hash.CAP_INTERIOR),"A room that is already on is left alone");
  access.Cancel();

  // ---- The room's spots stay folded into the entry until surveyed on foot.
  var c=Context(crew);var state=CampaignState.Load(Path.Combine(root,"room.json"));var homes=new CrewHomes(crew,state,c.Locations,new WeaponProgression(state));
  Check(CrewHomes.RoomSurveyKeys.Length==6&&CrewHomes.RoomSurveyKeys.All(k=>c.Locations.Get(k)!=null),"Every room survey key exists in the location book");
  Check(homes.RoomSpot("Bed")==null&&homes.RoomSpot("Door")==null&&homes.RoomSpot("Wardrobe")==null&&homes.RoomSpot("Locker")==null,"An estimated spot is not offered: nobody is sent into a wall");
  c.Locations.Record("Apartment.Room.Bed",new Vector3(345.5f,-1001f,-99.2f),270f);
  Check(homes.RoomSpot("Bed")!=null&&homes.RoomSpot("Bed").Position.Y==-1001f&&homes.RoomSpot("Door")==null,"A surveyed spot is offered where it was captured, and only that one");
  string homesSrc=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Core","CrewHomes.cs"));
  Check(homesSrc.Contains("UpdateRoomSpots(player, atEntry)")&&homesSrc.Contains("case \"Bed\": Rest(); break;")&&homesSrc.Contains("case \"Locker\": RestockLocker(); break;")&&homesSrc.Contains("default: ExitApartment(); break;")&&homesSrc.Contains("(OpenWardrobe ?? OpenMenu)?.Invoke()"),"Inside, each surveyed spot has its own prompt: the wardrobe opens, the bed rests, the locker restocks, the door leaves");
  Check(homesSrc.Contains("Apartment.Begin(location.Position, ipl, true, probe, location.Heading)"),"Entering faces the way the entry's heading says");
  string prologueSrc=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Core","PrologueSequence.cs"));
  Check(prologueSrc.Contains("_locations.Get(\"Apartment.Room.Message\")")&&prologueSrc.Contains("spot.Status == LocationStatus.Surveyed ? spot.Position : guess.Position + guess.ForwardVector * 2f"),"The message is read at the surveyed spot, else two meters into the room");
  var rows=File.ReadAllLines(Path.Combine(dataDir,"locations.tsv")).Where(l=>l.StartsWith("Apartment.Room.")).ToList();
  Check(rows.Count==5&&rows.All(r=>r.Split('\t')[5]=="interior"&&r.Split('\t')[6]=="estimate"),"The five room spots ship as interior estimates, to be surveyed inside");

  // ---- The room map reads the floor from collision.
  World.RaycastHandler=(s,t)=>{
   bool vertical=Math.Abs(s.X-t.X)<0.01f&&Math.Abs(s.Y-t.Y)<0.01f;
   if(vertical){ if(Math.Abs(s.X-347f)>3f||Math.Abs(s.Y+999f)>3f) return new RaycastResult(); float top=(s.X>348f&&s.Y<-1000f)?-98.7f:-99.2f; return new RaycastResult{DidHit=true,HitPosition=new Vector3(s.X,s.Y,top)}; }
   bool wall=Math.Abs(t.X-347f)>3f||Math.Abs(t.Y+999f)>3f; return new RaycastResult{DidHit=wall,HitPosition=t}; };
  string map=InteriorMapper.Map(new Vector3(347f,-999f,-99.2f),4f,player);
  Check(map.Contains("@")&&map.Contains("....")&&map.Contains("o")&&map.Contains("|")&&map.Contains("#")&&map.Contains("walkable cells"),"The map marks where you stood, the floor, furniture, wall faces and the void beyond them");
  string mapPath=InteriorMapper.Write(root,"Bloodlines.Room.txt",map);
  Check(File.Exists(mapPath)&&File.ReadAllText(mapPath)==map,"The map is written beside the ini files");
  World.RaycastHandler=null;Function.InteriorId=123;World.CollisionReady=false;

  // ---- A start from the menu begins at the job's marker.
  string menu=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Core","DevMenu.cs"));
  int warp=menu.IndexOf("WarpToStart(captured);",StringComparison.Ordinal),start=menu.IndexOf("_missions.Start(captured, bypassGates: true);",StringComparison.Ordinal);
  Check(warp>0&&start>warp&&start-warp<120&&menu.Contains("PrologueSequence.PlaceForColdOpen(player, point.Position)")&&menu.Contains("player.Heading = point.Heading;"),"A start from the menu moves the player to the job's marker, facing its way, before the briefing plays");
  Check(menu.Contains("_survey.Start(CrewHomes.RoomSurveyKeys)")&&menu.Contains("InteriorMapper.Map(player.Position, 12f, player)"),"Inside the room the dev menu offers the spot survey and the room map");
  string hostSrc=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","BloodlinesMain.cs"));
  Check(hostSrc.Contains("_menu.StartPoint = _missionMarkers.StartPoint;")&&hostSrc.Contains("_homes.OpenWardrobe = _menu.OpenWardrobe;"),"The host hands the menu the markers and the room its wardrobe");
  var catalog=new MissionCatalog();catalog.All.Add(Def("M22","main"));var markers=new MissionMarkers(catalog,state,null,c.Locations,dataDir,"J");
  Check(markers.StartPoint(catalog.All.First(m=>m.Id=="M22"))?.Key=="M22.Beach"&&markers.StartPoint(null)==null,"The marker a job starts from comes from mission_starts.tsv");
 }
}
