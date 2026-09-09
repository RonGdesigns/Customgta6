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
public static partial class RegressionTests {
 static int checks;
 static void Check(bool ok,string name){if(!ok)throw new Exception("FAIL: "+name);checks++;Console.WriteLine("PASS: "+name);}
 static void Reset(){Game.GameTime=100;Function.MovementInput=0;Function.SelfHandovers=0;World.StreetResolver=null;Function.Relations.Clear();Function.Follows.Clear();Function.TrafficShots.Clear();World.SphereVisible=false;Function.ClearLos=true;Game.Player=new Player();Game.Arrested=false;Game.TimeScale=1;World.WaypointBlip=null;World.Nearby=new Ped[0];World.CollisionReady=false;Function.Values.Clear();Function.ThrowOnce=null;GameUtils.Faded=false;}
 static CompanionController AI()=>new CompanionController(new ModConfig()){IndependentFreeRoam=false,RequireSharedVehicle=true};
 static void ConvoyAndIndependentTests(){
  Reset();var ai=new CompanionController(new ModConfig());var p=new Ped{Position=new Vector3(0,600,0)};var leader=new Ped{Position=new Vector3(0,0,0),ForwardVector=new Vector3(0,1,0)};var car=new Vehicle();leader.CurrentVehicle=car;
  ai.Update(CrewSlot.Ice,p,leader);Check(ai.StateOf(CrewSlot.Ice)==CompanionState.Independent&&p.Task.Gotos==1&&p.Position.Y==600,"Independent free roam sends remote crew on their own trip without moving them to the player");
  ai.TakeControl(CrewSlot.Ice);ai.Update(CrewSlot.Ice,p,leader);Check(ai.StateOf(CrewSlot.Ice)==CompanionState.Scripted,"Mission assignment takes precedence over independent free roam");
  ai.ReleaseAll();ai.IndependentFreeRoam=false;ai.Refresh(CrewSlot.Ice);World.NearbyVehicles=new[]{new Vehicle{Position=new Vector3(0,220,0)}};p.Position=new Vector3(0,210,0);ai.Update(CrewSlot.Ice,p,leader);
  Check(ai.StateOf(CrewSlot.Ice)==CompanionState.Convoy&&p.Task.Enters==1&&p.Task.Warps==0&&p.Task.LastSeat==VehicleSeat.Driver,"At 200m a travelling companion enters a separate nearby car normally");
  p.Position=new Vector3(0,600,0);p.IsOnScreen=true;World.CollisionReady=true;World.Vehicles.Clear();Game.GameTime+=1001;ai.Update(CrewSlot.Ice,p,leader);
  Check(World.Vehicles.Count==0,"Catch-up recovery never relocates a visible companion");
  p.IsOnScreen=false;World.SphereVisible=true;Game.GameTime+=1001;ai.Update(CrewSlot.Ice,p,leader);Check(World.Vehicles.Count==0,"Catch-up recovery also rejects visible destination road");
  World.SphereVisible=false;Game.GameTime+=1001;ai.Update(CrewSlot.Ice,p,leader);
  Check(World.Vehicles.Count==1&&p.CurrentVehicle!=car&&p.Position.DistanceTo(leader.Position)==100f,"Beyond 500m unseen companion rejoins behind player in a separate vehicle");
  ai.RequireSharedVehicle=true;Check(!ai.Convoy.HasRide(CrewSlot.Ice)&&!ai.Driver.Owns(CrewSlot.Ice,p),"A required shared mission vehicle cancels obsolete convoy ownership");
  World.NearbyVehicles=new Vehicle[0];World.Vehicles.Clear();World.SphereVisible=false;
 }
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
 static void FriendlyFireTests(){
  Reset();var ai=AI();var leader=new Ped{RelationshipGroup=99};var c=new Ped{RelationshipGroup=1};var brother=new Ped{RelationshipGroup=3,Hostile=true,CombatTarget=leader};
  ai.IsCrewMember=p=>p.Handle==brother.Handle||p.Handle==leader.Handle||p.Handle==c.Handle;
  World.Nearby=new[]{brother,leader};ai.Update(CrewSlot.Ice,c,leader);
  Check(c.Task.Fights==0,"Crew identity excludes brothers even when engine groups disagree");
  var enemy=new Ped{RelationshipGroup=4,Hostile=true,CombatTarget=leader};World.Nearby=new[]{enemy};Game.GameTime+=501;ai.Update(CrewSlot.Ice,c,leader);
  Check(c.Task.LastTarget==enemy,"Roster guard still permits real enemies");
  // Simulate engine handle reuse or character reassignment inside the scan cache window.
  brother=enemy;Game.GameTime+=20;ai.Update(CrewSlot.Ice,c,leader);
  Check(c.Task.Fights==1&&c.CombatTarget==null&&ai.StateOf(CrewSlot.Ice)!=CompanionState.Combat,"Cached enemy becoming crew cancels combat without firing again");
  ai.IsCrewMember=null;World.Nearby=new[]{enemy};Game.Player.Character=enemy;c.CombatTarget=enemy;c.IsInCombat=true;Game.GameTime+=501;ai.Update(CrewSlot.Ice,c,leader);
  Check(c.CombatTarget==null&&c.Task.Fights==1,"Current player identity is protected independently of the roster callback");
 }
 static void DriverTests(){
  Reset();var ai=AI();var car=new Vehicle();var driver=new Ped{CurrentVehicle=car};car.Seats[VehicleSeat.Driver]=driver;var passenger=new Ped{CurrentVehicle=car};
  ai.Driver.Arm(CrewSlot.Guess,driver);ai.Update(CrewSlot.Guess,driver,passenger);
  Check(ai.StateOf(CrewSlot.Guess)==CompanionState.Driving&&driver.Task.DriveKind=="cruise"&&driver.Task.DriveSpeed==50,"Former driver continues cautiously while new player rides as passenger");
  Game.GameTime+=1000;ai.Refresh(CrewSlot.Guess);ai.Update(CrewSlot.Guess,driver,passenger);
  Check(driver.Task.DriveCalls==1,"Companion refresh does not restart the driver's task or lose the trip");
  World.WaypointBlip=new Blip{Position=new Vector3(100,200,5)};Game.GameTime+=1000;ai.Update(CrewSlot.Guess,driver,passenger);
  Check(driver.Task.DriveKind=="road"&&driver.Task.DriveTarget==World.WaypointBlip.Position&&driver.Task.DriveSpeed==50,"Map waypoint gives the driver a cautious road destination");
  ai.Driver.MissionDestination=(s,v)=>new Vector3(300,400,5);Game.GameTime+=1000;ai.Update(CrewSlot.Guess,driver,passenger);
  Check(driver.Task.DriveTarget==new Vector3(300,400,5),"Assigned mission destination takes precedence over a waypoint");
  ai.Driver.MissionDestination=null;World.WaypointBlip=null;Game.GameTime+=1000;ai.Update(CrewSlot.Guess,driver,new Ped());
  Check(driver.Task.DriveKind=="cruise"&&ai.StateOf(CrewSlot.Guess)==CompanionState.Driving,"Driver keeps the vehicle when player switches to a character on foot");
  ai.Driver.HasBoardingPassengers=v=>true;int beforeBoarding=driver.Task.DriveCalls;Game.GameTime+=1000;ai.Update(CrewSlot.Guess,driver,passenger);
  Check(driver.Task.DriveCalls==beforeBoarding&&Function.Values.ContainsKey(Hash.TASK_VEHICLE_TEMP_ACTION),"Driver brakes while a companion is still entering a reserved door");
  ai.Driver.HasBoardingPassengers=v=>false;Game.GameTime+=1000;ai.Update(CrewSlot.Guess,driver,passenger);
  Check(driver.Task.DriveCalls==beforeBoarding+1,"Driver resumes its route after boarding finishes");
  ai.TakeControl(CrewSlot.Guess);int calls=driver.Task.DriveCalls;Game.GameTime+=1000;ai.Update(CrewSlot.Guess,driver,passenger);
  Check(ai.StateOf(CrewSlot.Guess)==CompanionState.Scripted&&driver.Task.DriveCalls==calls,"Scripted chases retain priority over generic driving");
  ai.ReleaseControl(CrewSlot.Guess);ai.Driver.Arm(CrewSlot.Guess,driver);car.Seats.Remove(VehicleSeat.Driver);
  Check(!ai.Driver.Owns(CrewSlot.Guess,driver),"Losing the driver seat cancels automatic driving");
  car.Seats[VehicleSeat.Driver]=driver;ai.Driver.Arm(CrewSlot.Guess,driver);ai.Forget(CrewSlot.Guess);
  Check(!ai.Driver.Owns(CrewSlot.Guess,driver),"Crew removal and respawn discard old driving sessions");
  foreach(string kind in new[]{"boat","heli","plane"}){
   car.Model=new Model{IsBoat=kind=="boat",IsHelicopter=kind=="heli",IsPlane=kind=="plane"};car.IsInAir=kind!="boat";car.Position=new Vector3(0,0,150);
   ai.Driver.Arm(CrewSlot.Guess,driver);ai.Driver.Update(CrewSlot.Guess,driver);
   Check(driver.Task.DriveKind.StartsWith(kind),kind+" handover uses the vehicle-specific navigation task");
  }
  car.IsInAir=false;car.HeightAboveGround=0;calls=driver.Task.DriveCalls;ai.Driver.Arm(CrewSlot.Guess,driver);ai.Driver.Update(CrewSlot.Guess,driver);
  Check(driver.Task.DriveCalls==calls&&Function.Values.ContainsKey(Hash.TASK_VEHICLE_TEMP_ACTION),"Grounded aircraft do not automatically take off toward a street waypoint");
 }
 static void HandoverTests(){
  Reset();var car=new Vehicle();var old=new Ped{CurrentVehicle=car};car.Seats[VehicleSeat.Driver]=old;var next=new Ped{CurrentVehicle=car};
  var roster=new RosterHandover{ActiveSlot=CrewSlot.Guess};roster.Peds[CrewSlot.Guess]=old;roster.Peds[CrewSlot.Gohan]=next;
  Game.Player.Character=next;roster.SetActive(CrewSlot.Gohan);
  Check(roster._companions.Driver.Owns(CrewSlot.Guess,old),"Production SetActive hands the former driver to the driving controller");
  Game.Player.Character=old;roster.SetActive(CrewSlot.Guess);
  Check(!roster._companions.Driver.Owns(CrewSlot.Guess,old)&&old.Task.Clears==0&&old.IsInVehicle(car),"Switching back removes autopilot without clearing a seated ped's tasks");
  roster._companions.TakeControl(CrewSlot.Guess);roster.SetActive(CrewSlot.Gohan);
  Check(!roster._companions.Driver.Owns(CrewSlot.Guess,old),"Production handover preserves a mission's ownership of its driver");
 }
 static void HoldTests(){
  Reset();var ai=AI();ai.HoldPosition=true;var leader=new Ped{CurrentVehicle=new Vehicle()};var c=new Ped{Position=new Vector3(100,0,0)};
  ai.Update(CrewSlot.Gohan,c,leader);Check(ai.StateOf(CrewSlot.Gohan)==CompanionState.Hold&&c.Task.Enters==0&&c.Task.Warps==0,"Split approach Hold prevents both boarding and distant vehicle warp");
  ai.HoldPosition=false;ai.Update(CrewSlot.Gohan,c,leader);Check(ai.StateOf(CrewSlot.Gohan)==CompanionState.Vehicle,"Releasing Hold restores vehicle behavior for the getaway");
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
  var book=LocationBook.Load(dir,user,saved,data);Check(book.Position("M01.CraneNest")==new Vector3(1,2,3),"Bible-only set-piece anchors do not overwrite playable location defaults");
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
  crew.ActivePed.IsDead=true;crew.ActivePed.IsPositionFrozen=true;crew.ActivePed.IsCollisionEnabled=false;crew.ActivePed.IsVisible=false;crew.ActivePed.AlwaysKeepTask=true;World.RenderingCamera=new object();Game.Player.IsDead=true;death.Update();Check(death.IsHandling&&abilities.Stops==1&&switcher.Cancels==1&&!Game.Player.CanControlCharacter,"Recovery stops abilities and switches before disabling control");
  Game.GameTime+=901;death.Update();Game.GameTime+=1501;death.Update();World.CollisionReady=true;Game.GameTime+=501;death.Update();Check(death.IsHandling,"Restored flags alone do not declare recovery successful or unlock switches");
  Function.MovementInput=1;crew.ActivePed.Position+=new Vector3(1,0,0);Game.GameTime+=100;death.Update();Check(!death.IsHandling&&!crew.ActivePed.IsDead&&Game.Player.CanControlCharacter&&!GameUtils.Faded,"Death completes with living controllable player and visible screen");
  Check(!crew.ActivePed.IsPositionFrozen&&crew.ActivePed.IsCollisionEnabled&&crew.ActivePed.IsVisible&&!crew.ActivePed.AlwaysKeepTask&&World.RenderingCamera==null,"Respawn clears physical/NPC movement locks and releases the scripted camera");
  Check(Function.Values.ContainsKey(Hash.SET_PLAYER_CONTROL)&&Function.Values.ContainsKey(Hash.RESET_PED_MOVEMENT_CLIPSET),"Respawn explicitly resets engine control and movement animation state");
  Check(missions.Failures==1&&crew.Regroups==0&&crew.ActiveRecoveries==1,"Unsupported checkpoint fails the mission and recovers only the active hero");
  death.Update();Check(death.DeathCount==1,"Successful recovery does not repeat on the next frame");
  Game.Arrested=true;death.Update();Check(death.IsHandling,"Arrest enters the same recovery path");death.Cancel();Check(!death.IsHandling&&Game.Player.CanControlCharacter&&!(bool)Function.Values[Hash.PAUSE_DEATH_ARREST_RESTART],"Cancelling recovery restores restart and control");
  Reset();crew=new CrewRoster{ActivePed=Game.Player.Character,CanRevive=false};death=new DeathController(new ModConfig(),crew,new MissionManager(),new AbilityController(),new SwitchController(),new DialogueDirector());crew.ActivePed.IsDead=true;death.Update();Game.GameTime+=901;death.Update();Game.GameTime+=1501;death.Update();
  Check(crew.Dismissals==1&&!death.IsHandling&&!GameUtils.Faded&&Game.Player.CanControlCharacter,"Revive failure returns to story and opens the screen");
  Reset();crew=new CrewRoster{ActivePed=Game.Player.Character};death=new DeathController(new ModConfig(),crew,new MissionManager(),new AbilityController(),new SwitchController(),new DialogueDirector());Function.ThrowOnce=Hash.SET_FADE_OUT_AFTER_ARREST;death.Update();
  Check(crew.Dismissals==1&&!(bool)Function.Values[Hash.PAUSE_DEATH_ARREST_RESTART],"Partial native failure still releases restart suppression");
 }
 public static int Main(string[] args){try{MilitaryAftermathChecks();TeamCombatChecks();ActiveRecoveryChecks();MilitaryLifecycleChecks();ReunionAndChaseTests();ConvoyAndIndependentTests();CompanionTests();FriendlyFireTests();DriverTests();HandoverTests();HoldTests();ObjectiveTests();SurveyTests(args[0]);DeathTests();Console.WriteLine(checks+" checks passed (stand-ins; live GTA validation still required).");return 0;}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
