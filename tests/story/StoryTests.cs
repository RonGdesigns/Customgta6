using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Math;
using GTA.Native;
public sealed class ProbeMission:Mission {public int Begins,Ticks;public override string Id=>"M02";public override string Title=>"Test";protected override bool OnStart(){Begins++;return true;}protected override void OnUpdate(){Ticks++;}public void Register(Entity entity){Track(entity);}}
public sealed class AssignedProbe:Bloodlines.Missions.Objectives.Objective {
 public AssignedProbe():base("Gohan: go to the terminal"){RequiredCharacter=CrewSlot.Gohan;}
 public override Vector3? AssignmentPosition=>new Vector3(100,0,0);
 public override void Update(MissionContext c){}
}
public sealed class SplitProbe:ComposedMission {
 public override string Id=>"M06";public override string Title=>"Split";
 protected override bool Setup()=>true;
 protected override System.Collections.Generic.IEnumerable<Bloodlines.Missions.Objectives.MissionStage> BuildStages(){yield return new Bloodlines.Missions.Objectives.MissionStage("Terminal",new AssignedProbe());}
}
public static partial class StoryTests
{
 static int checks;
 static string dataDir,root;
 static void Check(bool ok,string name){if(!ok)throw new Exception("FAIL: "+name);checks++;Console.WriteLine("PASS: "+name);}
 static void Reset(){Game.GameTime=100;Game.Player=new Player();Game.Accept=false;Game.Pressed.Clear();World.Created.Clear();World.Vehicles.Clear();World.Props.Clear();World.FailNavigation=false;World.FailNavigationNear=null;Function.Seabed=-40f;Function.SeabedKnown=true;World.NearbyVehicles=new Vehicle[0];World.GroundHeight=0f;World.WaterAvailable=true;World.FailPeds=false;World.RenderingCamera=null;World.FailCamera=false;World.CollisionReady=false;GameUtils.Faded=false;Function.Values.Clear();Function.Held.Clear();Function.Axes.Clear();Function.ThrowOnce=null;Script.Waited=0;}
 static CrewRoster Roster(){var c=new CrewRoster();c.Peds[CrewSlot.Ice]=Game.Player.Character;c.Peds[CrewSlot.Gohan]=new Ped{Position=new Vector3(10,0,0)};c.Peds[CrewSlot.Guess]=new Ped{Position=new Vector3(500,0,0)};return c;}
 static void AssignmentAndRouteChecks(){
  Reset();var crew=Roster();var c=Context(crew);var mission=new SplitProbe();Check(mission.Begin(c),"Composed split mission starts");
  Check(crew.CompanionAI.Controlled.Contains(CrewSlot.Gohan)&&crew.PedFor(CrewSlot.Gohan).Task.Gotos==1,"Character-owned objective sends inactive Gohan to his own location");
  crew.SetActive(CrewSlot.Gohan);mission.Tick();crew.SetActive(CrewSlot.Ice);mission.Tick();
  Check(crew.PedFor(CrewSlot.Gohan).Task.Gotos==2,"Switching to a worker and back resumes his assignment");mission.Abort();
  Check(crew.CompanionAI.Controlled.Count==0,"Aborting releases character-specific task ownership");
  ObjectiveMarkers.Clear();ObjectiveMarkers.ActiveSlot=CrewSlot.Gohan;ObjectiveMarkers.BeginFrame(true);
  ObjectiveMarkers.Navigation(new Vector3(100,0,0),CrewSlot.Ice);ObjectiveMarkers.Navigation(new Vector3(200,0,0),CrewSlot.Gohan);ObjectiveMarkers.EndFrame();var route=World.LastBlip;
  Check(route.ShowRoute&&route.Position.X==200,"Parallel objectives automatically route to the active character's assigned job");
  ObjectiveMarkers.ActiveSlot=CrewSlot.Ice;ObjectiveMarkers.BeginFrame(true);ObjectiveMarkers.Navigation(new Vector3(100,0,0),CrewSlot.Ice);ObjectiveMarkers.Navigation(new Vector3(200,0,0),CrewSlot.Gohan);ObjectiveMarkers.EndFrame();
  Check(route.Position.X==100,"Switching characters updates the mission GPS route");ObjectiveMarkers.Clear();Check(!route.Present,"Mission cleanup removes its GPS route");ObjectiveMarkers.ActiveSlot=null;
  Reset();crew=Roster();c=Context(crew);var pickup=new Vehicle();crew.SetActive(CrewSlot.Guess);Game.Player.Character=crew.PedFor(CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(pickup,VehicleSeat.Driver);
  var boarding=new Bloodlines.Missions.Objectives.EnterVehicleObjective("Pickup",()=>pickup,VehicleSeat.Driver,true);boarding.Enter(c);boarding.Update(c);Check(!boarding.IsFinished,"Shared extraction waits for both teammates before advancing");
  crew.PedFor(CrewSlot.Ice).SetIntoVehicle(pickup,VehicleSeat.RightFront);crew.PedFor(CrewSlot.Gohan).SetIntoVehicle(pickup,VehicleSeat.LeftRear);boarding.Update(c);Check(boarding.IsFinished,"Shared extraction advances with driver and both teammates aboard");
  Reset();crew=Roster();var car=new Vehicle();var target=crew.PedFor(CrewSlot.Gohan);target.SetIntoVehicle(car,VehicleSeat.LeftRear);Function.EjectOnSwitch=true;
  Check(new SwitchController(crew).TrySwitch(CrewSlot.Gohan)&&target.IsInVehicle(car)&&target.SeatIndex==VehicleSeat.LeftRear,"Character handover restores the original passenger seat if the engine ejects it");Function.EjectOnSwitch=false;
 }
 static void NewMissionBehavior(){
  Reset();var crew=Roster();var c=Context(crew);var car=new Vehicle();
  foreach(var hero in Protagonist.All)crew.PedFor(hero.Slot).SetIntoVehicle(car,hero.Slot==CrewSlot.Guess?VehicleSeat.Driver:hero.Slot==CrewSlot.Ice?VehicleSeat.RightFront:VehicleSeat.LeftRear);
  var positions=crew.Peds.Values.Select(p=>p.Position).ToArray();int created=World.Created.Count;
  Check(c.Cutscenes.Play("M02","intro","Chase briefing"),"M02 briefing starts with existing cast");
  Check(World.Created.Count==created&&crew.Peds.Values.All(p=>p.IsInVehicle(car)),"Briefing preserves all seats and creates no replacement group");
  c.Cutscenes.Stop();Check(crew.Peds.Values.Select(p=>p.Position).SequenceEqual(positions)&&!car.IsPositionFrozen,"Scene exit restores vehicle state without relocating the cast");
  Reset();crew=Roster();c=Context(crew);var work=new Bloodlines.Missions.Objectives.AssignedWorkObjective("Servers",CrewSlot.Gohan,()=>new Vector3(100,0,0),2);work.Enter(c);work.Update(c);
  Check(crew.CompanionAI.Controlled.Contains(CrewSlot.Gohan)&&crew.PedFor(CrewSlot.Gohan).Task.Gotos==1,"Assigned worker goes to his own objective while another hero is active");
  Game.GameTime+=3000;work.Update(c);Check(!work.IsFinished,"Assigned work cannot finish from across the map");
  crew.PedFor(CrewSlot.Gohan).Position=new Vector3(100,0,0);work.Update(c);Game.GameTime+=1000;work.Update(c);Game.GameTime+=1000;work.Update(c);
  Check(work.Status==Bloodlines.Missions.Objectives.ObjectiveStatus.Complete,"NPC completes work at the assigned site while player handles another job");work.Exit(c);
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"m02-open.json"));var m=new Bloodlines.Missions.Campaign.M02LooseStrands();Check(m.Begin(c)&&c.Cutscenes.IsActive,"M02 opens on the stash beat: the prototype at the curb, the Granger beside it, the crew on foot");
  var chase=World.Vehicles.First(v=>v.Model.Name=="granger");var van=World.Vehicles.First(v=>v.Model.Name=="rumpo");var proto=World.Vehicles.First(v=>v.Model.Name=="schafter3");var technician=World.Created.First(p=>p.CurrentVehicle==van);
  Check(crew.ActiveSlot==CrewSlot.Guess&&crew.Peds.Values.All(p=>!p.IsInVehicle())&&proto.LockStatus==VehicleLockStatus.CannotEnter&&!m.StashDone&&technician.Task.VehicleMissions==0&&m.EndpointKind==MissionEndpoint.EscapeCheckpoint,"Nobody is seated yet, the prototype is locked, the van waits and the clock has not started");
  c.Cutscenes.Skip();
  Check(crew.PedFor(CrewSlot.Guess).SeatIndex==VehicleSeat.Driver&&crew.PedFor(CrewSlot.Ice).IsInVehicle(chase)&&crew.PedFor(CrewSlot.Gohan).IsInVehicle(chase),"Skipping the stash beat seats the crew in the Granger with Guess driving");
  m.Tick();Check(m.StashDone&&m.CurrentStage==0&&technician.Task.VehicleMissions==1&&technician.Task.LastMissionPoint.DistanceTo(van.Position)>600f,"The first tick after the beat starts the clock and puts the van on a road mission far down its road");
  chase.Position=van.Position-new Vector3(0,20,0);m.Tick();m.Tick();
  for(int i=0;i<11;i++){Game.GameTime+=1000;m.Tick();}
  Check(technician.Task.VehicleMissions==2,"A van that sits still for five seconds gets its route re-issued once, and once only");
  Check(m.CurrentStage==1&&!Function.Values.ContainsKey(Hash.TASK_VEHICLE_SHOOT_AT_PED),"Van crew holds fire before the hack reaches halfway");
  chase.Position=van.Position-new Vector3(0,80,0);for(int i=0;i<5;i++){Game.GameTime+=1000;m.Tick();}
  Check(m.CurrentStage==1&&!Function.Values.ContainsKey(Hash.TASK_VEHICLE_SHOOT_AT_PED),"Losing proximity pauses hacking and delays the ambush");
  chase.Position=van.Position-new Vector3(0,20,0);for(int i=0;i<3;i++){Game.GameTime+=1000;m.Tick();}
  Check(Function.Values.ContainsKey(Hash.TASK_VEHICLE_SHOOT_AT_PED),"Enemies fire after the passenger hack reaches fifty percent");
  crew.SetActive(CrewSlot.Gohan);Game.Player.Character=crew.PedFor(CrewSlot.Gohan);Game.GameTime+=1000;m.Tick();
  Check(Function.Values.ContainsKey(Hash.TASK_VEHICLE_FOLLOW)&&crew.PedFor(CrewSlot.Guess).IsInVehicle(chase),"Guess continues following the van when Gohan becomes the player");
  for(int i=0;i<13;i++){Game.GameTime+=1000;m.Tick();}
  Check(m.CurrentStage==2&&!van.IsDriveable&&m.CurrentObjective.Contains("rear doors"),"Hack disables van and exposes precise drive-retrieval instructions");
  var ice=crew.PedFor(CrewSlot.Ice);crew.SetActive(CrewSlot.Ice);Game.Player.Character=ice;ice.SetIntoVehicle(van,VehicleSeat.Driver);Game.GameTime+=4000;m.Tick();
  Check(m.CurrentStage==2,"Stealing the target van does not incorrectly complete drive retrieval");
  ice.Task.LeaveVehicle();ice.Position=van.Position-van.ForwardVector*3.2f;Game.GameTime+=100;m.Tick();Game.GameTime+=3001;m.Tick();
  Check(m.CurrentStage==3&&m.Drives!=null&&m.Drives.AttachedTo==ice&&c.State.EvidenceOf("dockRecording")==EvidenceState.CopyHeld,"Ice collects the drives after three seconds at the rear doors: they are in his hand and the evidence is recorded");
  ice.Position=c.Locations.Position("M02.CanalEscape");m.Tick();Check(m.Status==MissionStatus.Running&&m.Drives!=null,"Escape cannot pass on foot without the crew car, and the drives stay in hand");
  ice.SetIntoVehicle(chase,VehicleSeat.RightFront);ice.Position=c.Locations.Position("M02.CanalEscape");Game.Player.WantedLevel=2;Game.GameTime+=1000;m.Tick();
  Check(m.Drives==null&&m.Status==MissionStatus.Running&&Game.Player.WantedLevel==2&&GameUtils.Message.Contains("Lose the police"),"Boarding stows the drives; the canal is an escape checkpoint and does not clear the police");
  Game.Player.WantedLevel=0;m.Tick();
  Check(m.Status==MissionStatus.Passed&&crew.CompanionAI.Controlled.Count==0&&!crew.CompanionAI.RequireSharedVehicle&&chase.Present&&chase.Released&&proto.Present&&proto.Released,"M02 passes with all three in the Granger and the police lost; the Granger and the prototype remain in the world");
 }
 static void Switches(){
  Reset();var crew=Roster();var s=new SwitchController(crew);int stopped=0;s.BeforeSwitch=()=>stopped++;
  Check(s.TrySwitch(CrewSlot.Gohan)&&Game.Player.Character==crew.Peds[CrewSlot.Gohan]&&Script.Waited==0&&!GameUtils.Faded,"Nearby switch transfers the existing ped with no wait or fade");
  Check(stopped==1&&crew.ActiveSlot==CrewSlot.Gohan,"Switch stops ability and updates roster ownership");
  Game.GameTime+=401;Game.Player.Character.IsPositionFrozen=true;
  Check(!s.TrySwitch(CrewSlot.Ice)&&stopped==1,"Frozen current hero cannot enter native handover");Game.Player.Character.IsPositionFrozen=false;
  Game.Player.CanControlCharacter=false;Check(!s.TrySwitch(CrewSlot.Ice)&&stopped==1,"Unavailable player control blocks switching before native calls");Game.Player.CanControlCharacter=true;
  Game.GameTime+=401;var old=Game.Player.Character;Check(!s.TrySwitch(CrewSlot.Guess)&&Game.Player.Character==old&&Script.Waited==1650&&!GameUtils.Faded&&!s.IsSwitching,"Distant collision timeout preserves hero and restores screen within bounded wait");
  Game.GameTime+=401;World.CollisionReady=true;Script.Waited=0;Check(s.TrySwitch(CrewSlot.Guess)&&Script.Waited==150&&!GameUtils.Faded,"Loaded distant target needs only the short fade delay");
  Game.GameTime+=401;s.SetLocked("test");Check(!s.TrySwitch(CrewSlot.Ice),"Scripted switch locks remain enforced");s.SetUnlocked();
  crew.IsSolo=true;Check(!s.TrySwitch(CrewSlot.Ice),"Solo deployment refuses character switch");crew.IsSolo=false;
  crew.Peds[CrewSlot.Ice].IsDead=true;Check(!s.TrySwitch(CrewSlot.Ice),"Dead target never takes player control");
  Reset();crew=Roster();s=new SwitchController(crew);World.CollisionReady=true;Function.ThrowOnce=Hash.CHANGE_PLAYER_PED;
  Check(!s.TrySwitch(CrewSlot.Guess)&&!GameUtils.Faded&&!s.IsSwitching&&crew.ActiveSlot==CrewSlot.Ice,"Native handover error restores fade and leaves roster unchanged");
  Check(Protagonist.All.Select(p=>p.DisplayName).SequenceEqual(new[]{"Ice","Gohan","Guess"}),"All gameplay display names use nicknames");
  Check(Protagonist.Ice.FullName=="Darius Vance","Story identity remains available separately");
  Reset();crew=Roster();s=new SwitchController(crew);World.CollisionReady=true;GameUtils.Faded=true;
  Check(s.TrySwitch(CrewSlot.Guess,missionTransition:true)&&GameUtils.Faded,"M27 can transfer during its own fade without switch cleanup stealing that fade");
 }
 static MissionContext Context(CrewRoster crew){var data=CampaignData.Load(dataDir);var dialogue=new DialogueDirector(data,root);var book=LocationBook.Load(dataDir,Path.Combine(root,"none.ini"),Path.Combine(root,"none-survey.ini"),data);return new MissionContext{Crew=crew,Config=new ModConfig(),Switching=new SwitchController(crew),Data=data,Locations=book,Dialogue=dialogue,Cutscenes=new CutsceneDirector(crew,dialogue,book,dataDir)};}
 static void Scenes(){
  Reset();var crew=Roster();var c=Context(crew);var player=Game.Player.Character;var camera=new Camera();World.RenderingCamera=camera;
  Check(c.Cutscenes.Play("M01","intro","Opening")&&!Game.Player.CanControlCharacter&&player.IsInvincible,"Opening protects player and owns camera controls");
  Check(World.Created.Count==5&&World.Created.Select(p=>p.Position).Distinct().Count()==5,"M01 cold open separates the leads and shows Mateo working with a technician");
  c.Cutscenes.Update();Check(GTA.UI.Screen.Subtitle.Contains("GUESS"),"Silent opening begins with Guess's private channel");
  c.Cutscenes.Stop();Check(!CutsceneDirector.IsSceneRunning&&Game.Player.CanControlCharacter&&!player.IsInvincible&&!player.IsPositionFrozen&&World.RenderingCamera==camera&&World.Created.All(p=>!p.Present),"Skipping restores original camera, player flags and removes all temporary actors");
  c.Cutscenes.Stop();Check(Game.Player.CanControlCharacter,"Scene cleanup is idempotent");
  var actionPed=new Ped{Position=new Vector3(500,500,10)};int actions=0;
  var before=crew.Peds.Values.Select(p=>p.Position).ToArray();
  c.Cutscenes.Play("M01","recognition","Recognition",actionPed,()=>actions++);
  var next=typeof(CutsceneDirector).GetMethod("NextLine",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
  for(int i=0;i<8;i++)next.Invoke(c.Cutscenes,null);
  Check(actions==1&&!actionPed.IsPositionFrozen&&crew.Peds.Values.Select(p=>p.Position).SequenceEqual(before),"Animated scene action fires once without freezing its actor or relocating the crew");
  c.Cutscenes.Stop();
  var car=new Vehicle{IsPositionFrozen=true,IsInvincible=true};player.CurrentVehicle=car;
  c.Cutscenes.Play("M02","outro","Aftermath");Function.ThrowOnce=Hash.CLEAR_FOCUS;c.Cutscenes.Stop();
  Check(Game.Player.CanControlCharacter&&car.IsInvincible&&car.IsPositionFrozen,"Cleanup failure in one native does not skip control or original vehicle state restoration");
  World.FailCamera=true;Check(!c.Cutscenes.Play("M02","intro","Setup failure")&&!c.Cutscenes.IsActive&&Game.Player.CanControlCharacter&&World.Created.All(p=>!p.Present),"Camera creation failure unwinds partially spawned scene");World.FailCamera=false;
  Check(!c.Cutscenes.Play("UNKNOWN","intro","Missing")&&Game.Player.CanControlCharacter,"Missing scene data leaves gameplay available");
  c.Cutscenes.Play("M02","outro","Aftermath");Game.Accept=true;c.Cutscenes.Update();Check(!c.Cutscenes.IsActive,"Controller accept skips scene");
  c.Cutscenes.Play("M02","outro","Aftermath");Game.GameTime+=240001;c.Cutscenes.Update();Check(!c.Cutscenes.IsActive&&Game.Player.CanControlCharacter,"Scene watchdog restores controls on a stalled timeline");
  World.Created.Clear();c.Cutscenes.Play("SM03","intro","KJ");c.Cutscenes.Update();
  Check(World.Created.Count==1&&GTA.UI.Screen.Subtitle.Contains("KJ")&&Protagonist.All.Length==3,"KJ gets a speaking supporting actor beside Guess without a fourth playable slot");c.Cutscenes.Stop();
 }
 static void M01Regression(){
  Reset();var crew=Roster();var c=Context(crew);World.RenderingCamera=new Camera{Present=false};
  Check(c.Cutscenes.Play("M01","intro","Invalid prior camera"),"Opening accepts the invalid camera wrapper returned by GET_RENDERING_CAM");
  c.Cutscenes.Stop();Check(World.RenderingCamera==null&&Game.Player.CanControlCharacter,"Invalid prior camera returns to gameplay instead of keeping script rendering enabled");
  Reset();crew=Roster();c=Context(crew);World.FailNavigation=true;var old=c.Locations.Position("M01.CraneNest");
  Check(!ProloguePlacement.Prepare(c.Locations)&&c.Locations.Position("M01.CraneNest")==old&&Script.Waited==600,"Unloaded navmesh rejects M01 within a bound without moving location keys");
  Reset();crew=Roster();c=Context(crew);
  var iceStart=c.Locations.Position("M01.IceApproach");var lookoutStart=c.Locations.Position("M01.CraneNest");
  Check(GameUtils.IsWithinFlat(iceStart,lookoutStart,20f)&&!GameUtils.IsWithinFlat(iceStart,lookoutStart,12f),"Ice's lookout is a short walk from his approach, not a hike, and not on top of it");
  Check(lookoutStart.DistanceTo(c.Locations.Position("M01.CapoSpawn"))<260f,"Ice can still identify Mateo from the lookout");
  Reset();crew=Roster();c=Context(crew);c.Locations.Get("M01.CraneNest").Position=c.Locations.Position("M01.IceApproach");
  Check(ProloguePlacement.Prepare(c.Locations),"A lookout that resolves onto Ice's approach is nudged out, not refused");
  Check(!GameUtils.IsWithinFlat(c.Locations.Position("M01.IceApproach"),c.Locations.Position("M01.CraneNest"),12f),"The nudged lookout still leaves Ice an approach to walk");
  Reset();crew=Roster();c=Context(crew);c.Locations.Get("M01.ServiceTerminal").Position=c.Locations.Position("M01.CapoSpawn");
  Check(!ProloguePlacement.Prepare(c.Locations),"M01 rejects terminal overrides that overlap Mateo before moving actors");
  var legacy=Path.Combine(root,"old-terminal.ini");File.WriteAllText(legacy,"[Positions]\nM01.LowerDeckLedger.X = 1019\nM01.LowerDeckLedger.Y = -3184\nM01.LowerDeckLedger.Z = 6\n");
  var migrated=LocationBook.Load(dataDir,legacy);
  Check(migrated.Position("M01.ServiceTerminal").DistanceTo(migrated.Position("M01.CapoSpawn"))>30f,"Legacy terminal overrides cannot restore the old Mateo placement");
  Reset();crew=Roster();c=Context(crew);var mission=new Bloodlines.Missions.Campaign.M01GhostInTheDockyard();
  Check(mission.Begin(c)&&crew.CompanionsHoldPosition,"M01 starts with actors held at their independent assignments");
  Check(crew.ActiveSlot==CrewSlot.Guess&&crew.Peds[CrewSlot.Gohan].Task.Scenarios>0&&crew.Peds[CrewSlot.Ice].Task.Aims>0&&crew.CompanionAI.Controlled.Count==3,"Guess starts playable while Gohan watches the service entrance and Ice covers Mateo");
  Check(crew.Peds.Values.Select(p=>p.Position).Distinct().Count()==3,"M01 deployment keeps three distinct task positions");
  Check(Game.Player.Character.IsInVehicle(World.Vehicles[1])&&World.Vehicles[1].GetPedOnSeat(VehicleSeat.Driver)==Game.Player.Character,"Guess starts seated in his approach car, never on its hood");
  Check(Game.Player.Character.Position.DistanceTo(c.Locations.Position("M01.PrototypeCar"))>80&&crew.Peds[CrewSlot.Gohan].Position.DistanceTo(c.Locations.Position("M01.ServiceTerminal"))>40,"Guess and Gohan must travel to their contracts from separated entry points");
  Check(c.Locations.Position("M01.ServiceTerminal").DistanceTo(World.Created[0].Position)>30f,"M01 laptop interaction is separated from Mateo after navigation correction");
  var car=World.Vehicles[0];crew.ActiveSlot=CrewSlot.Guess;Game.Player.Character=crew.Peds[CrewSlot.Guess];Game.Player.Character.SetIntoVehicle(car,VehicleSeat.Driver);Game.Player.Character.Position=c.Locations.Position("M01.ExitPoint");mission.Tick();
  Check(mission.CurrentStage==0&&mission.Status==MissionStatus.Running,"Driving the prototype to the exit early cannot skip Ice and Gohan assignments");
  Check(GameUtils.Message.Contains("Switch to Ice"),"Early driver sees the missing character task instead of a silent marker");
  crew.ActiveSlot=CrewSlot.Ice;Game.Player.Character=crew.Peds[CrewSlot.Ice];Game.Player.Character.IsAiming=true;Function.Values[Hash.IS_PLAYER_FREE_AIMING_AT_ENTITY]=true;mission.Tick();
  Check(GameUtils.Message.Contains("walk to the lookout")&&mission.CurrentStage==0,"Ice cannot identify Mateo before walking from his approach to the lookout");
  Game.Player.Character.Position=c.Locations.Position("M01.CraneNest");mission.Tick();
  Check(GameUtils.Message.Contains("Switch to Gohan"),"Free aim identifies Mateo from the lookout without lock-on targeting");
  crew.ActiveSlot=CrewSlot.Gohan;Game.Player.Character=crew.Peds[CrewSlot.Gohan];mission.Tick();Game.GameTime+=8001;mission.Tick();
  Check(mission.CurrentStage==0,"Waiting at Gohan's entry point does not copy the distant ledger");
  Game.Player.Character.Position=c.Locations.Position("M01.ServiceTerminal");Game.Accept=true;World.CollisionReady=true;mission.Tick();Game.GameTime+=8001;mission.Tick();
  Check(mission.CurrentStage==1&&c.Cutscenes.IsActive,"All three assignments trigger the recognition scene exactly once");
  Check(crew.ActiveSlot==CrewSlot.Gohan&&Game.Player.Character==crew.Peds[CrewSlot.Gohan]&&crew.Peds[CrewSlot.Gohan].Position==c.Locations.Position("M01.ServiceTerminal")&&crew.Peds[CrewSlot.Ice].Position==c.Locations.Position("M01.CraneNest")&&crew.Peds[CrewSlot.Guess].IsInVehicle(car),"Copying the ledger leaves the player on Gohan, with no automatic switch; recognition preserves split positions and the actual driver's seat");
  c.Cutscenes.Stop();mission.Tick();
  Check(mission.CurrentStage==2&&World.Created.Count>=9,"Combat spawns after recognition resumes gameplay");
  var mateo=World.Created[0];var technician=World.Created[1];var launch=World.Vehicles.Last();
  Check(mateo.Task.Enters==1&&mateo.Task.LastSeat==VehicleSeat.Driver&&technician.Task.Enters==1&&mateo.IsInvincible&&!technician.IsInvincible,"Recognition sends Mateo and his technician running for the launch on foot; only Mateo is protected");
  Game.GameTime+=100000;mission.Tick();
  Check(mission.CurrentStage==2&&GameUtils.Message.Contains("Hostiles")&&!GameUtils.Message.Contains("clear in"),"There is no clock: with guards alive nothing happens however long it takes");
  foreach(var guard in World.Created.Skip(2))guard.IsDead=true;mission.Tick();
  Check(mission.CurrentStage==3&&c.Cutscenes.IsActive&&mateo.Position.DistanceTo(launch.Position)<10f,"The yard clear, the escape plays at once, with a Mateo short of the launch brought to it");
  c.Cutscenes.Skip();mission.Tick();Check(mateo.IsInVehicle(launch)&&mateo.Task.BoatTasks==1,"Skipping the escape still puts Mateo in the launch and the launch on the water");
  mateo.IsDead=true;mission.Tick();Check(mission.Status==MissionStatus.Running,"Once the launch has left, losing Mateo at a distance is not his death");
  Game.Player.Character.Position=c.Locations.Position("M01.ExitPoint");mission.Tick();Check(mission.Status==MissionStatus.Running,"On-foot arrival at the final marker cannot complete the car extraction");
  car.Position=c.Locations.Position("M01.ExitPoint");Game.Player.Character.SetIntoVehicle(car,VehicleSeat.Driver);mission.Tick();Check(mission.Status==MissionStatus.Running,"Extraction waits for missing companions");
  foreach(var member in crew.Peds.Values){member.CurrentVehicle=car;member.Position=Game.Player.Character.Position;}mission.Tick();
  Check(mission.Status==MissionStatus.Passed,"The full M01 flow passes once all three reach the exit in the prototype");
  Reset();crew=Roster();c=Context(crew);mission=new Bloodlines.Missions.Campaign.M01GhostInTheDockyard();mission.Begin(c);Use(crew,CrewSlot.Ice);Game.Player.Character.IsShooting=true;mission.Tick();
  Check(mission.Status==MissionStatus.Failed&&mission.FailReason.Contains("Ice blew the cover"),"Breaking the quiet approach fails clearly instead of ignoring shots at Mateo");
  Reset();crew=Roster();c=Context(crew);World.FailPeds=true;mission=new Bloodlines.Missions.Campaign.M01GhostInTheDockyard();Check(!mission.Begin(c),"Missing essential actor rejects M01 setup instead of leaving an impossible objective");
 }
 static void PadSwitchTests(){
  Reset();var crew=Roster();World.CollisionReady=true;var host=new ControllerHarness{_crew=crew,_switching=new SwitchController(crew)};
  Function.Held[Control.CharacterWheel]=true;Function.Axes[Control.LookLeftRight]=1;host.Tick();
  Check(crew.ActiveSlot==CrewSlot.Ice,"Controller previews the choice while D-pad Down is held");
  Function.Held[Control.CharacterWheel]=false;host.Tick();Check(crew.ActiveSlot==CrewSlot.Guess,"Releasing D-pad Down switches to the right-stick selection");
  Game.GameTime+=401;Function.Held[Control.CharacterWheel]=true;Function.Axes[Control.LookLeftRight]=-1;host.Tick();host._menu.IsOpen=true;host.Tick();Function.Held[Control.CharacterWheel]=false;host._menu.IsOpen=false;host.Tick();
  Check(crew.ActiveSlot==CrewSlot.Guess,"Opening the debug menu cancels a pending character selection");
  Function.Held[Control.CharacterWheel]=true;Function.Axes[Control.LookLeftRight]=0;Function.Axes[Control.LookUpDown]=-1;host.Tick();Function.Held[Control.CharacterWheel]=false;host.Tick();Check(crew.ActiveSlot==CrewSlot.Gohan,"Right-stick up selects Gohan using disabled-input values");
  Game.GameTime+=401;crew.IsSolo=true;Function.Held[Control.CharacterWheel]=true;Function.Axes[Control.LookUpDown]=0;Function.Axes[Control.LookLeftRight]=-1;host.Tick();Function.Held[Control.CharacterWheel]=false;host.Tick();Check(crew.ActiveSlot==CrewSlot.Gohan,"Controller switching respects solo mission locks");
 }
 static void WheelAndLooks(){
  Reset();var wheel=new CharacterWheel(root);Game.TimeScale=.7f;wheel.Open(CrewSlot.Guess);wheel.Open(CrewSlot.Ice);
  Check(wheel.IsOpen&&Game.TimeScale==.2f&&wheel.Selected==CrewSlot.Guess,"Character wheel slows time without overwriting its original state on repeated open");
  wheel.Close();wheel.Close();Check(Game.TimeScale==.7f&&!wheel.IsOpen,"Wheel restores the previous speed exactly once");
  CrewAppearance.Load(Path.Combine(root,"looks.ini"));Check(Protagonist.StartingSlot==CrewSlot.Guess&&CrewAppearance.For(CrewSlot.Ice).Hair==14&&CrewAppearance.For(CrewSlot.Gohan).Hair==1&&CrewAppearance.For(CrewSlot.Guess).Hair==0,"Guess starts the story and each brother has a fixed requested hairstyle");
  var ped=new Ped();CrewAppearance.Apply(ped,CrewSlot.Guess);Game.GameTime+=120001;
  Check(!CrewAppearance.ChangeAfterAbsence(ped,CrewSlot.Guess,false),"Mission transitions cannot randomize clothes");
  Check(CrewAppearance.ChangeAfterAbsence(ped,CrewSlot.Guess,true)&&CrewAppearance.For(CrewSlot.Guess).Hair==0,"Distant free-roam outfit refresh preserves bald identity");
  Check(!CrewAppearance.ChangeAfterAbsence(ped,CrewSlot.Guess,true),"Outfit cooldown prevents rapid repeated changes");
  CrewAppearance.Save();CrewAppearance.Load(Path.Combine(root,"looks.ini"));Check(CrewAppearance.For(CrewSlot.Guess).Outfit==1,"Appearance choices survive save and reload");
 }
 static void NewControlsAndRoutes(){
  var chord=new AbilityChord();Check(!chord.Update(true,false,false)&&chord.Update(true,true,false),"Ability requires both stick clicks together");
  Check(!chord.Update(true,true,false)&&!chord.Update(false,true,false)&&!chord.Update(true,true,false),"Holding or partially releasing the sticks does not double toggle");
  chord.Update(false,false,false);Check(!chord.Update(true,true,true)&&!chord.Update(true,true,false),"Closing a menu with held sticks cannot accidentally activate an ability");
  chord.Update(false,false,false);Check(chord.Update(true,true,false),"Full release rearms the next ability activation");
  var regular=AbilityMeterLayout.Calculate(16f/9f,1f);var wide=AbilityMeterLayout.Calculate(21f/9f,.9f);
  Check(regular.X<.2f&&regular.Y>.9f&&regular.Width<.05f&&wide.Width<regular.Width&&wide.Y<regular.Y,"Ability meter occupies compact minimap slot and follows aspect ratio and safe-zone margin");
  CrewAppearance.Load(Path.Combine(root,"new-looks.ini"));Check(Protagonist.All.All(p=>CrewAppearance.For(p.Slot).Skin==19)&&CrewAppearance.For(CrewSlot.Gohan).Face==6&&CrewAppearance.For(CrewSlot.Guess).Face==1,"All three leads use the corrected skin preset and distinct face defaults");
  var car=new Vehicle();ObjectiveMarkers.BeginFrame(true);ObjectiveMarkers.Show(new Vector3(10,10,0));ObjectiveMarkers.EndFrame();
  Check(!ObjectiveMarkers.DestinationFor(CrewSlot.Guess,car).HasValue,"Combat/display markers never become accidental driving destinations");
  ObjectiveMarkers.BeginFrame(true);ObjectiveMarkers.Navigation(new Vector3(20,20,0),CrewSlot.Ice);ObjectiveMarkers.Navigation(new Vector3(50,50,0),CrewSlot.Guess,car);ObjectiveMarkers.EndFrame();
  Check(ObjectiveMarkers.DestinationFor(CrewSlot.Guess,car)==new Vector3(50,50,0)&&!ObjectiveMarkers.DestinationFor(CrewSlot.Gohan,car).HasValue,"Navigation respects the assigned hero and vehicle");
  ObjectiveMarkers.BeginFrame(true);ObjectiveMarkers.EndFrame();Check(!ObjectiveMarkers.DestinationFor(CrewSlot.Guess,car).HasValue,"Finished stage destinations disappear on the next frame");
 }
 static void AbilityBindingTests(){
  Reset();var ability=new AbilityInputHarness();Function.Held[Control.ScriptLS]=true;ability.HandleController(false);Check(ability.Toggles==0,"Production controller binding ignores a lone left stick click");
  Function.Held[Control.ScriptRS]=true;ability.HandleController(false);ability.HandleController(false);Check(ability.Toggles==1,"Production binding toggles once for L3 plus R3 held together");
  Function.Held.Clear();ability.HandleController(false);Function.Held[Control.ScriptLS]=Function.Held[Control.ScriptRS]=true;ability.HandleController(true);ability.HandleController(false);
  Check(ability.Toggles==1,"Production ability input stays blocked through menu/wheel closure until sticks release");
  Function.Held.Clear();ability.HandleController(false);Game.IsPaused=true;Function.Held[Control.ScriptLS]=Function.Held[Control.ScriptRS]=true;ability.HandleController(false);Game.IsPaused=false;
  Check(ability.Toggles==1,"Pause menu cannot activate the custom ability");
 }
 static void StickTests(){
  var pad=new ControllerNavigation();Check(pad.Update(0,0,0,up:true)==MenuDirection.Up&&pad.Update(0,0,100,up:true)==MenuDirection.None&&pad.Update(0,0,401,up:true)==MenuDirection.Up,"D-pad navigation uses the same controlled repeat as the right stick");
  pad.Update(0,0,410);Check(pad.Update(1,0,420,down:true)==MenuDirection.Down,"D-pad takes precedence over incidental stick movement");
  pad.Update(0,0,430);Check(pad.Update(0,0,440,left:true)==MenuDirection.Left,"D-pad left/right can adjust menu settings");
  var stick=new ControllerNavigation();Check(stick.Update(.2f,-.2f,0)==MenuDirection.None,"Menu ignores stick drift");
  Check(stick.Update(0,1,0)==MenuDirection.Down&&stick.Update(0,1,399)==MenuDirection.None,"Menu deflection moves once before repeat delay");
  Check(stick.Update(0,1,400)==MenuDirection.Down&&stick.Update(0,1,539)==MenuDirection.None&&stick.Update(0,1,540)==MenuDirection.Down,"Held stick repeats at a controlled rate");
  stick.Update(0,0,541);Check(stick.Update(-1,0,542)==MenuDirection.Left,"Releasing and deflecting immediately selects the new direction");
 }
 static void Markers(){
  Reset();var crew=Roster();var c=Context(crew);var state=CampaignState.Load(Path.Combine(root,"markers.json"));var cat=new MissionCatalog();
  var def=new MissionDefinition{Info=new MissionInfo{Id="M01",Title="Opening"},Factory=()=>new ProbeMission()};
  var locked=new MissionDefinition{Info=new MissionInfo{Id="M02",Title="Locked",Prerequisite="M01"},Factory=()=>new ProbeMission()};cat.All.Add(def);cat.All.Add(locked);
  var manager=new MissionManager(c,state,cat);var book=LocationBook.Load(dataDir,Path.Combine(root,"none.ini"));
  Game.Player.Character.Position=book.Position("M01.RegroupPoint");var markers=new MissionMarkers(cat,state,manager,book,dataDir,"J");markers.Update(false);
  Check(markers.Nearby==def&&World.LastBlip.Name=="M01 — Opening"&&!World.LastBlip.ShowRoute&&!World.LastBlip.IsShortRange,"Available mission has a named map icon and proximity start without forced GPS");
  var first=World.LastBlip;state.Completed.Add("M01");Game.Player.Character.Position=book.Position("M02.InterceptStart");markers.Update(false);
  Check(!first.Present&&markers.Nearby==locked,"Completion removes old marker and reveals the newly unlocked job");
  var second=World.LastBlip;markers.Update(true);Check(!second.Present&&markers.Nearby==null,"Menu or recovery suspension removes actionable mission markers");
  ObjectiveMarkers.BeginFrame(true);ObjectiveMarkers.Show(new Vector3(1,2,3));ObjectiveMarkers.EndFrame();var objective=World.LastBlip;
  ObjectiveMarkers.BeginFrame(true);ObjectiveMarkers.Show(new Vector3(4,5,6));ObjectiveMarkers.EndFrame();
  Check(World.LastBlip==objective&&objective.Position==new Vector3(4,5,6)&&!objective.ShowRoute,"Objective map marker follows a moving destination without allocating one per frame");
  ObjectiveMarkers.BeginFrame(true);ObjectiveMarkers.EndFrame();Check(!objective.Present,"Finished objectives remove obsolete map markers");ObjectiveMarkers.Clear();
 }
 static void Dispatch(){
  Reset();var crew=Roster();var c=Context(crew);var state=CampaignState.Load(Path.Combine(root,"save.json"));var mission=new ProbeMission();var def=new MissionDefinition{Info=new MissionInfo{Id="M02",Title="Test",Time="05:30",Weather="Fog"},Factory=()=>mission};var catalog=new MissionCatalog();catalog.All.Add(def);var manager=new MissionManager(c,state,catalog);
  Check(manager.Start(def)&&manager.IsRunning&&mission.Begins==0,"Mission setup and timers do not start during briefing");manager.Update();Check(mission.Begins==0,"Dispatch cannot advance while scene owns control");
  manager.Abort();Check(!manager.IsRunning&&mission.Begins==0&&!c.Cutscenes.IsActive,"Abort during opening cancels pending mission without spawning it");
  manager.Start(def);c.Cutscenes.Stop();manager.Update();Check(mission.Begins==1&&mission.Ticks==0,"Skipping briefing starts gameplay exactly once on next dispatch");manager.Update();Check(mission.Ticks==1,"Gameplay resumes normal ticking after scene");
  mission.Pass();c.Dialogue.Play(new DialogueCue{CueId="ending",MissionId="M02",Speaker="ICE",Line="We made it."});manager.Update();Check(manager.IsRunning&&!state.IsComplete("M02"),"Final gameplay dialogue drains before aftermath and blocks a replacement mission");
  c.Dialogue.Clear();manager.Update();Check(state.IsComplete("M02")&&c.Cutscenes.IsActive,"Mission progress is saved before skippable aftermath");
  manager.Shutdown();Check(!manager.IsRunning&&!c.Cutscenes.IsActive&&Game.Player.CanControlCharacter,"Script shutdown releases aftermath camera and controls");
  state.SetUpgrade("grangerTurbineInstalled",true);state.Unlock("grandSenoraRadarBunker");state.Reset();var restored=CampaignState.Load(Path.Combine(root,"save.json"));
  Check(!restored.FleetUpgrades["grangerTurbineInstalled"]&&!restored.Safehouses["grandSenoraRadarBunker"]&&restored.CompletedCount==0,"Campaign reset clears progress, equipment and later-act safehouses");
  Check(File.Exists(Path.Combine(root,"save.json.bak")),"Replacing an existing save retains its previous version");
 }
 static void TransportAndRace(){
  Reset();var crew=Roster();var context=Context(crew);var mission=new ProbeMission();mission.Begin(context);var car=new Vehicle();var empty=new Vehicle();Game.Player.Character.CurrentVehicle=car;mission.Register(car);mission.Register(empty);mission.Pass();
  Check(car.Present&&car.Released&&!empty.Present,"Mission completion preserves occupied transport but removes unused mission vehicles");
  Reset();crew=Roster();context=Context(crew);crew.ActiveSlot=CrewSlot.Guess;var raceCar=new Vehicle();var target=Game.Player.Character.Position;var race=new Bloodlines.Missions.Objectives.RaceCheckpointObjective("race",new[]{target},12f,1,()=>raceCar);race.RequiredCharacter=CrewSlot.Guess;race.Enter(context);race.Update(context);
  Check(!race.IsFinished,"Race checkpoints cannot be completed on foot");Game.Player.Character.CurrentVehicle=new Vehicle();race.Update(context);Check(!race.IsFinished,"Race checkpoints reject a substituted vehicle");Game.Player.Character.CurrentVehicle=raceCar;race.Update(context);Check(race.IsFinished,"Required hero in the race car can complete the circuit");
 }
 static void Waves(){
  var file=Path.Combine(root,"timing.wav");using(var w=new BinaryWriter(File.Create(file))){w.Write(0x46464952u);w.Write(40u+16000u);w.Write(0x45564157u);w.Write(0x20746d66u);w.Write(16u);w.Write((ushort)1);w.Write((ushort)1);w.Write(8000u);w.Write(16000u);w.Write((ushort)2);w.Write((ushort)16);w.Write(0x61746164u);w.Write(16000u);w.Write(new byte[16000]);}
  Check(WaveTiming.DurationMs(file)==1000,"WAV timing uses actual recording byte rate and data length");File.WriteAllText(file,"broken");Check(WaveTiming.DurationMs(file)==0&&WaveTiming.DurationMs(null)==0,"Missing and invalid audio retain subtitle-only timing");
 }
 public static int Main(string[] args){try{dataDir=args[0];root=args[1];MarketChecks();PowerChecks();StreetsChecks();CampaignAuditChecks();ApartmentWeaponChecks();PersonalChecks();ExpansionChecks();HomeAndVehicleChecks();ClarityMissionChecks(); CampaignFlowChecks();AssignmentAndRouteChecks();NewMissionBehavior();Switches();Scenes();M01Regression();PadSwitchTests();WheelAndLooks();NewControlsAndRoutes();AbilityBindingTests();StickTests();Dispatch();Markers();TransportAndRace();Waves();HandoffPassChecks();CorrectionPassChecks();FixBranchChecks();PackageABChecks();Round2Checks();VisualsDamageChecks();M01ExitChecks();ShieldChecks();PortHeistChecks();CrewVanChecks();StoryToPlayChecks();OpenSliceChecks();ControlChecks();RoomChecks();RelayChecks();Round3Checks();TierChecks();Console.WriteLine(checks+" story/runtime checks passed.");return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}

