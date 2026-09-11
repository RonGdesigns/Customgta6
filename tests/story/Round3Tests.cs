using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;
public static partial class StoryTests
{
 static void Round3Checks()
 {
  // ---- Police: a cop who sees a wanted player says so; out of sight the search runs down as before.
  Reset();var crew=Roster();var player=Game.Player.Character;var response=new TacticalResponse();
  crew.IsDeployed=true;var cop=new Ped{IsCop=true,Position=player.Position+new Vector3(20,0,0),RelationshipGroup=77};World.Nearby=new[]{cop};
  Game.Player.WantedLevel=2;Function.StarsGreyed=true;Function.ClearLos=true;Function.Calls.Clear();Game.GameTime+=2000;response.Update(crew);
  Check(Function.Calls.Any(c=>c.Item1==Hash.REPORT_POLICE_SPOTTED_PLAYER)&&Function.Calls.Any(c=>c.Item1==Hash.SET_PLAYER_WANTED_CENTRE_POSITION),"A cop with line of sight inside seventy meters reports the wanted player and the search re-centers");
  Function.Calls.Clear();Function.ClearLos=false;Game.GameTime+=2000;response.Update(crew);
  Check(!Function.Calls.Any(c=>c.Item1==Hash.REPORT_POLICE_SPOTTED_PLAYER),"No line of sight, no report: the police are smarter, not unlosable");
  Function.ClearLos=true;Function.StarsGreyed=false;Game.GameTime+=2000;response.Update(crew);
  Check(!Function.Calls.Any(c=>c.Item1==Hash.REPORT_POLICE_SPOTTED_PLAYER),"An active pursuit needs no report");
  Game.Player.WantedLevel=0;Function.StarsGreyed=true;Game.GameTime+=2000;response.Update(crew);
  Check(!Function.Calls.Any(c=>c.Item1==Hash.REPORT_POLICE_SPOTTED_PLAYER),"Nothing is reported when nobody is wanted");
  response.Reset();World.Nearby=new Ped[0];Function.StarsGreyed=false;

  // ---- The progress meter replaces "stay in the marker: Ns".
  Reset();crew=Roster();var c=Context(crew);player=Game.Player.Character;var point=new Vector3(100,100,10);player.Position=point;
  var work=new MissionInteraction("Copy the ledger",()=>point,4,3f);work.Enter(c);GameUtils.LastProgress=-1f;
  work.Update(c);Check(work.Label.Contains("press E"),"Before the press the prompt says what to press");
  Game.Accept=true;work.Update(c);Game.Accept=false;Game.GameTime+=2000;work.Update(c);
  Check(work.Label=="Copy the ledger"&&Math.Abs(GameUtils.LastProgress-0.5f)<0.05f&&!work.Label.Contains("stay in"),"During the work the label is the work and a small meter shows the progress");
  Game.GameTime+=2100;work.Update(c);Check(work.Status==ObjectiveStatus.Complete,"The meter fills and the work completes");
  var hold=new HoldZoneObjective("Hold the junction",()=>point,4,3f,"Locking the junction");hold.Enter(c);GameUtils.LastProgress=-1f;hold.Update(c);Game.GameTime+=2000;hold.Update(c);
  Check(GameUtils.Message=="Locking the junction"&&Math.Abs(GameUtils.LastProgress-0.5f)<0.05f,"A hold shows its name and the meter, no countdown text");
  string utils=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Core","GameUtils.cs"));
  Check(utils.Contains("\"timerbars\", \"all_black_bg\"")&&utils.Contains("const float width = 0.085f, height = 0.007f"),"The meter is drawn on the game's own timer-bar backing and stays very small");

  // ---- Source contracts for the mission fixes Ron reported on September 10.
  string m1=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act1","M01GhostInTheDockyard.cs"));
  Check(m1.Contains("mark.Sprite = BlipSprite.Enemy")&&m1.Contains("_guardBlips")&&!m1.Contains("StrandedMs"),"M01's hostiles carry red map marks that go when they do, and there is no stranded wait before the escape");
  string m2=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act1","M02LooseStrands.cs"));
  Check(m2.Contains("Press E / D-pad Right to get in the Granger")&&m2.Contains("player.SetIntoVehicle(_chase, FreeSeat())")&&m2.Contains("_chase.LockStatus = VehicleLockStatus.Unlocked"),"M02's Granger takes Ice back at its door whatever the engine makes of the drives in his hand");
  string m3=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act1","M03CypressFoundry.cs"));
  Check(m3.Contains("new ConditionObjective(\"Guess: put the street crew down.\"")&&m3.Contains("UnseatCrew();")&&m3.Contains("GameUtils.IsWithinFlat(post, truck, 7f)"),"M03 clears the block before any switch, never spawns a guard in the Benson, and gets everyone out before it locks");
  string m4=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act1","M04SeveredWire.cs"));
  Check(m4.Contains("Station(CrewSlot.Ice, _iceWatch);")&&m4.Contains("Station(CrewSlot.Gohan, _gohanApproach);")&&!m4.Contains("Station(CrewSlot.Ice, _van"),"M04's Ron pulls up alone; Ice and Gohan are already in place");
  string m7=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act1","M07WiretapWaltz.cs"));
  Check(m7.Contains("RoofTop(Ctx.Locations.Position(\"M07.GarageRoof\"))")&&m7.Contains("World.GetNextPositionOnStreet(Ctx.Locations.Position(\"M07.LandingZone\"))"),"M07 puts Ice on the roof the world has and the sedan on the street");
  string m8=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act1","M08SupplyAndSever.cs"));
  Check(m8.Contains("World.GetNextPositionOnStreet(_gate + new Vector3(22f, -52f, 0f))"),"M08's forklift starts in the open");
  var rows=File.ReadAllLines(Path.Combine(dataDir,"locations.tsv")).Where(l=>l.StartsWith("M08.")).ToDictionary(l=>l.Split('\t')[0],l=>l.Split('\t'));
  float dx=float.Parse(rows["M08.CratePadTwo"][1])-float.Parse(rows["M08.CratePadOne"][1]);
  Check(dx>=12f&&Math.Abs(float.Parse(rows["M08.HaulerSpawn"][2])-float.Parse(rows["M08.CratePadOne"][2]))>=20f,"M08's crates are a truck-length apart and the flatbed is clear of them");
 }
}
