using System;
using System.Linq;
using System.IO;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
public static partial class StoryTests
{
 static void Use(CrewRoster crew,CrewSlot slot){crew.SetActive(slot);Game.Player.Character=crew.PedFor(slot);}
 static void Interact(Mission mission,MissionContext c,CrewSlot slot,Vector3 p,int seconds,bool afloat=false)
 {
  Use(c.Crew,slot);if(!afloat)Game.Player.Character.Task.LeaveVehicle();
  Game.Player.Character.Position=p;
  if(afloat)Game.Player.Character.CurrentVehicle.Position=p;
  Game.Accept=false;mission.Tick();Game.Accept=true;mission.Tick();Game.GameTime+=seconds*1000+1;mission.Tick();Game.Accept=false;
 }
 static void ClarityMissionChecks()
 {
  Reset();var crew=Roster();var c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"clarity3.json"));c.Vans=new CrewVan(c.State,c.Locations);var m3=new M03CypressFoundry();
  Check(m3.Begin(c)&&c.Cutscenes.IsActive&&m3.EndpointKind==MissionEndpoint.SafehouseArrival,"M03 sets up at the base with its guards, truck, dogs and van, and opens on the split");
  var car=m3.Car;var van=m3.Van;var hauler=m3.Hauler;var ice=crew.PedFor(CrewSlot.Ice);var gohan=crew.PedFor(CrewSlot.Gohan);var guess=crew.PedFor(CrewSlot.Guess);var junction=c.Locations.Position("M03.RailJunction");var depot=c.Locations.Position("M03.DepotGate");
  Check(m3.Dogs.Count==M03CypressFoundry.JunctionDogs&&m3.Dogs.Count>=3&&m3.Dogs.All(d=>d.Model.Name=="a_c_rottweiler"&&!d.BlockPermanentEvents&&d.Position.DistanceTo(junction)<16f)&&m3.Crates.Count==3,"Four fighting dogs are close in on the junction block before Ron arrives and three crates wait beside the Benson");
  c.Cutscenes.Skip();Check(ice.IsInVehicle(van)&&gohan.IsInVehicle(van),"Skipping the split seats the depot team in the van");
  m3.Tick();
  Check(!ice.IsInVehicle()&&ice.Position.DistanceTo(depot)<60f&&gohan.Position.DistanceTo(hauler.Position)<20f&&m3.Roles.For(CrewSlot.Ice).State==RoleState.Observing&&guess.IsInVehicle(car)&&m3.CurrentStage==0,"The cut lands Ice and Gohan outside the depot on their tracks; Ron is in his car at the base");
  Use(crew,CrewSlot.Guess);var basePoint=c.Locations.Position("Base.CypressFlats");var half=basePoint+(junction-basePoint)*0.6f;guess.Position=half;car.Position=half;m3.Tick();m3.Tick();
  Check(c.Dialogue.HasPending,"Halfway there Ice calls that the depot team is set");
  guess.Position=junction;car.Position=junction;car.Speed=8f;m3.Tick();Check(m3.CurrentStage==0,"Rolling through the junction is not arrival");
  car.Speed=0f;m3.Tick();m3.Tick();Check(m3.CurrentStage==1&&!m3.CurrentObjective.Contains("D-pad"),"Stopped in the zone the work is next, and it needs no button");
  Game.GameTime+=3000;m3.Tick();Check(m3.CurrentStage==1&&!m3.AmbushSprung,"In the car on the mark the work does not start");
  Check(m3.Dogs.All(d=>d.Task.Fights>=1&&d.Task.LastTarget==guess),"The dogs go for Ron the moment he is on the block, car or not");
  guess.Task.LeaveVehicle();guess.Position=junction;m3.Tick();Game.GameTime+=1000;m3.Tick();Check(!m3.AmbushSprung,"The block waits a beat after the work starts");
  Game.GameTime+=M03CypressFoundry.AmbushDelayMs-1000;m3.Tick();
  Check(m3.AmbushSprung&&m3.Ambush.Count==M03CypressFoundry.StreetCrewSize&&m3.Ambush.Count==6&&m3.Ambush.All(t=>t.Task.HatedFights==1)&&c.Cutscenes.IsActive&&m3.CurrentStage==1,"Five seconds into the work the street crew comes out of the houses, with a moment on the first of them, and the hold is not done");
  c.Cutscenes.Stop();Game.GameTime+=2000;m3.Tick();Check(m3.CurrentStage==1&&m3.CurrentObjective.Contains("street crew"),"The hold done, the block's crew is still Ron's fight before any switch");
  foreach(var thug in m3.Ambush)thug.IsDead=true;m3.Tick();Check(m3.CurrentStage==2,"With the street crew down the rail is locked and the depot is Ice's");
  var guards=World.Created.Where(g=>g.Model.Name.StartsWith("g_m_y_mex")||g.Model.Name=="g_m_m_armboss_01").ToList();
  Game.Player.Character.Position=depot-new Vector3(0,50,0);Game.Player.Character.IsShooting=true;m3.Tick();
  Check(m3.Status==MissionStatus.Running&&guards.All(g=>g.Task.HatedFights==0),"Ron shooting near the depot is not Ice's shot: the quiet rule is scoped to Ice and the guards keep patrolling");
  Game.Player.Character.IsShooting=false;Use(crew,CrewSlot.Ice);m3.Tick();
  Check(m3.CurrentObjective.Contains("entry marker")&&m3.CurrentObjective.Contains("Quiet")&&!m3.CurrentObjective.Contains("No ability")&&guards.Count==M03CypressFoundry.DepotGuardCount&&guards.Count==10&&guards.All(g=>g.Task.HatedFights==0),"Switching to Ice explains the walk and the quiet rule with no ability wording; ten guards patrol until his entry fires");
  Game.Player.Character.Position=depot-new Vector3(0,22,0);m3.Tick();
  Check(m3.CurrentStage==3&&m3.EntryFired&&guards.All(g=>g.Task.HatedFights==1),"Ice reaching the entry marker is what wakes the yard");
  m3.Tick();Check(m3.CurrentObjective.Contains("RED")&&m3.CurrentObjective.Contains("Remaining: 10"),"M03 yard combat names the red targets and keeps the count in the objective HUD");
  foreach(var guard in guards)guard.IsDead=true;m3.Tick();Check(m3.CurrentStage==4,"M03 loading unlocks only after the yard is clear");
  Interact(m3,c,CrewSlot.Gohan,hauler.Position-hauler.ForwardVector*4f,1);
  Check(c.Cutscenes.IsActive&&m3.CurrentStage==5&&GTA.Native.Function.Values.ContainsKey(GTA.Native.Hash.SET_VEHICLE_DOOR_OPEN),"Gohan opening the Benson starts the loading, seen: the doors are open and the crates go in on camera");
  c.Cutscenes.Skip();Check(m3.Crates.All(crate=>crate.AttachedTo==hauler)&&gohan.IsInVehicle(hauler),"Skipping the loading leaves all three crates in the bed and Gohan in the cab");
  m3.Tick();Check(GTA.Native.Function.Values.ContainsKey(GTA.Native.Hash.SET_VEHICLE_DOOR_SHUT)&&c.Dialogue.HasPending&&m3.Reinforced&&m3.CurrentStage==5,"After the loading the doors close, Gohan calls Ron, and the depot answers");
  Check(m3.Reinforcements.Count==M03CypressFoundry.ReinforcementGunmen+M03CypressFoundry.ReinforcementDogs&&m3.Reinforcements.Count(r=>r.Model.Name=="a_c_rottweiler")==3&&!gohan.IsInVehicle()&&m3.Roles.For(CrewSlot.Gohan).State==RoleState.Covering&&m3.Roles.For(CrewSlot.Ice).State==RoleState.Covering,"Seven gunmen and three dogs come through the gate; Gohan is out of the cab and both take cover");
  Check(Flow(m3)[5].LockedTo==CrewSlot.Guess&&Flow(m3)[6].LockedTo==null,"Ron alone on the road back; the depot fight is anyone's");
  Use(crew,CrewSlot.Guess);Game.Player.Character.Position=depot;c.Dialogue.Clear();m3.Tick();Check(m3.CurrentStage==6&&m3.CurrentObjective.Contains("RED"),"Ron in close, the depot fight is on with the targets marked");
  foreach(var r in m3.Reinforcements)r.IsDead=true;m3.Tick();Check(m3.CurrentStage==7,"The depot cleared, the truck is the job");
  Game.Player.Character.SetIntoVehicle(hauler,VehicleSeat.Driver);m3.Tick();Check(m3.CurrentStage==7&&ice.Task.Enters>=1&&c.Dialogue.HasPending&&!c.Crew.CompanionsHoldPosition,"Ron in the cab: Ice is called aboard and Gohan is back on his own AI for the van");
  ice.SetIntoVehicle(hauler,VehicleSeat.RightFront);c.Dialogue.Clear();m3.Tick();Check(m3.CurrentStage==8&&Game.Player.WantedLevel==2,"Ice aboard with Ron, the police come");
  var destination=basePoint;Game.Player.Character.SetIntoVehicle(new Vehicle{Position=destination},VehicleSeat.Driver);m3.Tick();Check(m3.Status==MissionStatus.Running,"M03 cannot finish by arriving in another car");
  hauler.Position=destination;Game.Player.Character.SetIntoVehicle(hauler,VehicleSeat.Driver);m3.Tick();Check(m3.Status==MissionStatus.Running&&m3.CurrentObjective.Contains("Lose the police"),"Arriving with the police on the truck is not delivery: the foundry is a safehouse, the heat is lost on the way");
  var outro=m3.OutroBlocking();Check(outro!=null&&outro.Steps.Count==5&&outro.Steps[0] is ExitVehicleStep&&outro.Steps[1] is ExitVehicleStep&&outro.Steps[2] is CarryPropStep&&outro.Steps[3] is StowPropStep&&outro.DialogueAfterStep==1&&m3.FoundryKeys!=null&&m3.FoundryKeys.Model.Name==M03CypressFoundry.KeysModel,"The aftermath gets Ron and Ice out of the truck before the lines, then the three keys go down on it: the object M22 picks back up");
  gohan.SetIntoVehicle(hauler,VehicleSeat.RightFront);Game.Player.WantedLevel=0;m3.Tick();
  Check(!gohan.IsInVehicle(hauler),"Gohan is out of the Benson before it locks");
  Check(m3.Status==MissionStatus.Passed&&hauler.LockStatus==VehicleLockStatus.CannotEnter&&hauler.Present&&hauler.Released&&van.Present&&car.Present,"Delivered with the police lost: the Benson locks at the foundry and the crew's vehicles remain");
  Reset();crew=Roster();c=Context(crew);bool scope=false;var quiet=new QuietRuleObjective("Quiet","Heard",()=>scope);quiet.Enter(c);Game.Player.Character.IsShooting=true;quiet.Update(c);
  Check(quiet.IsPassive&&!quiet.IsFinished,"Out of its scope the quiet rule allows the shot");scope=true;quiet.Update(c);
  Check(quiet.Status==ObjectiveStatus.Failed&&quiet.FailReason=="Heard","In scope one shot fails with the reason");

  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"clarity4.json"));var m4=new M04SeveredWire();Check(m4.Begin(c),"M04 stages its surface operation with all essential actors");
  Check(c.Locations.Position("M04.Breaker").Z>0&&c.Locations.Position("M04.RampGuards").Z>0,"M04 no longer depends on the absent B3 interior");
  m4.Abort(); // The M04 flow is covered end to end by StoryToPlayTests.RunM04.

  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"clarity5.json"));var m5=new M05TidalLock();Check(m5.Begin(c)&&c.Cutscenes.IsActive,"M05 validates coast staging, creates both boats and opens on the cove");
  var mateo=m5.Mateo;var dinghy=m5.Dinghy;Check(crew.PedFor(CrewSlot.Guess).IsInVehicle(dinghy)&&crew.PedFor(CrewSlot.Gohan).IsInVehicle(dinghy)&&mateo.IsInvincible,"M05 gives Guess and Gohan actual boat transport while Ice holds shore overwatch; Mateo cannot be killed");
  c.Cutscenes.Skip();
  foreach(var enemy in World.Created.Where(p=>p!=mateo))enemy.IsDead=true;m5.Tick();Check(m5.CurrentStage==1,"Shore clearance opens Guess's explicit flare interaction");
  Interact(m5,c,CrewSlot.Guess,c.Locations.Position("M05.CoveAir"),1,true);Check(m5.CurrentStage==2&&GTA.Native.Function.Values.ContainsKey(GTA.Native.Hash.SHOOT_SINGLE_BULLET_BETWEEN_COORDS),"M05 launches an actual flare after the player interaction");
  dinghy.Position=c.Locations.Position("M05.GrottoMouth");Use(crew,CrewSlot.Gohan);Game.Player.Character.Position=dinghy.Position;m5.Tick();Check(m5.CurrentStage==3&&mateo.Task.BoatTasks==1,"Gohan's boat approach starts Mateo's boat escape");
  var tropic=World.Vehicles.First(v=>v.Model.Name=="tropic");dinghy.Position=tropic.Position;Game.Player.Character.Position=dinghy.Position;m5.Tick();Game.GameTime+=5001;m5.Tick();
  Check(m5.CurrentStage==4&&mateo.IsAlive&&m5.IceDown&&m5.Roles.For(CrewSlot.Ice).State==RoleState.Extracting&&c.Dialogue.HasPending,"Sustained close pursuit stops Mateo alive; Ice starts down to the shore and says so");
  GTA.UI.Screen.Subtitle=null;Interact(m5,c,CrewSlot.Gohan,mateo.Position,2,true);
  Check(m5.CurrentStage==5&&c.Cutscenes.IsActive&&m5.Status==MissionStatus.Running,"Taking him aboard plays the account as a scene from the bible's own stage-two lines");
  c.Cutscenes.Update();Check(string.IsNullOrEmpty(GTA.UI.Screen.Subtitle),"Nobody speaks until Mateo is in the boat");
  c.Cutscenes.Skip();Check(mateo.IsInVehicle(dinghy)&&c.Cutscenes.LastOutcome==SceneOutcome.Skipped,"Skipping the account leaves Mateo aboard the dinghy");
  m5.Tick();Check(m5.Status==MissionStatus.Passed&&c.State.EvidenceOf("mateoAllegation")==EvidenceState.Alleged&&mateo.Exists()&&!mateo.IsInvincible&&mateo.IsInVehicle(dinghy),"M05 ends on an allegation recorded as one, with Mateo alive in the crew's boat: custody is seen");
  var capture=new CaptureBoatObjective(()=>mateo,()=>World.Vehicles[0],()=>dinghy);capture.Enter(c);mateo.IsDead=true;capture.Update(c);Check(capture.Status==ObjectiveStatus.Failed,"A dead witness cannot incorrectly satisfy the capture objective");

  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"clarity6.json"));var m6=new M06CleanSweep();Check(m6.Begin(c)&&c.Cutscenes.IsActive,"M06 validates and deploys its separate work stations and opens on the three positions");
  c.Cutscenes.Skip();var granger=m6.Granger;
  Interact(m6,c,CrewSlot.Gohan,c.Locations.Position("M06.Feeder"),6);Use(crew,CrewSlot.Ice);Game.Player.Character.Position=c.Locations.Position("M06.SallyPort");m6.Tick();Check(m6.CurrentStage==2&&!m6.CurrentObjective.Contains("No ability"),"M06 power interaction and Ice's breach open the parallel burn and siege, with no ability wording");
  crew.PedFor(CrewSlot.Gohan).Position=c.Locations.Position("M06.ServerRacks");
  for(int i=0;i<45&&m6.CurrentStage==2;i++){foreach(var enemy in World.Created)enemy.IsDead=true;Game.GameTime+=1000;m6.Tick();}
  Check(m6.CurrentStage==3&&m6.PickupCalled&&crew.PedFor(CrewSlot.Guess).Task.Drives>=1,"M06 requires both the completed burn and all response waves; the rotors turned Ron's wait into a pickup and his own AI moved the truck");
  Check(m6.FireBurning&&c.State.EvidenceOf("vespucciBackup")==EvidenceState.Destroyed,"The burn leaves a real fire at the racks and the record's destruction on the books");
  Use(crew,CrewSlot.Guess);m6.Tick();Check(m6.CurrentStage==3&&m6.Roles.For(CrewSlot.Ice).State==RoleState.Extracting,"Guess has to bring the Granger to the alley mouth; Ice and Gohan are coming to it");
  granger.Position=m6.Pickup;Game.Player.Character.Position=m6.Pickup;m6.Tick();Check(m6.CurrentStage==4,"At the alley mouth the boarding opens");
  m6.Tick();Check(m6.CurrentStage==4,"Guess cannot leave without his teammates");
  crew.PedFor(CrewSlot.Ice).SetIntoVehicle(granger,VehicleSeat.RightFront);crew.PedFor(CrewSlot.Gohan).SetIntoVehicle(granger,VehicleSeat.LeftRear);m6.Tick();Game.Player.WantedLevel=2;m6.Tick();Check(m6.CurrentStage==5&&m6.Status==MissionStatus.Running,"With everyone aboard the escape is on the crew: the police are not cleared");
  Game.Player.WantedLevel=0;m6.Tick();Check(m6.Status==MissionStatus.Passed&&!m6.FireBurning,"M06 completes with both teammates aboard and the police lost, and the fire is put out with the mission");

  Reset();crew=Roster();c=Context(crew);World.FailNavigation=true;Check(!new M04SeveredWire().Begin(c)&&World.Created.Count==0,"Unavailable walkable surfaces reject setup before spawning actors underground");World.FailNavigation=false;
  var interaction=new MissionInteraction("Terminal",()=>new Vector3(5,0,0),3);interaction.RequiredCharacter=CrewSlot.Gohan;interaction.Enter(c);Use(crew,CrewSlot.Gohan);Game.Player.Character.Position=new Vector3(5,0,0);Game.Accept=true;interaction.Update(c);Game.GameTime+=1500;Game.Player.Character.Position=Vector3.Zero;interaction.Update(c);Game.Player.Character.Position=new Vector3(5,0,0);Game.GameTime+=5000;interaction.Update(c);Check(!interaction.IsFinished&&interaction.Label.Contains("press"),"Leaving an interaction resets its timer and requires a new button press");
  var lines=MissionObjectiveHud.Wrap(new string('a',80)+" objective description").ToArray();Check(lines.All(l=>l.Length<=68)&&lines.Length>=2,"Long objective text wraps without overflowing the HUD");
  var injured=new Ped{Health=200,MaxHealth=200,Armor=0,CanSufferCriticalHits=true};CrewDurability.RestoreAfterSwitch(injured,430,65);Check(injured.MaxHealth==900&&injured.Health==430&&injured.Armor==65&&!injured.CanSufferCriticalHits,"Handover restores the larger health cap and existing injuries without a free heal");
  CrewDurability.RestoreAfterSwitch(injured,430,65);Check(injured.Health==430&&CrewDurability.Armor==100,"Repeated switches preserve damage and the increased armor limit");
 }
}
