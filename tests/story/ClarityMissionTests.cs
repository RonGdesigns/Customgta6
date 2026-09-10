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
  Reset();var crew=Roster();var c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"clarity3.json"));var m3=new M03CypressFoundry();
  Check(m3.Begin(c),"M03 setup requires its guards and weapons truck");
  Check(crew.PedFor(CrewSlot.Guess).Position.DistanceTo(crew.PedFor(CrewSlot.Ice).Position)>500&&crew.PedFor(CrewSlot.Guess).IsInVehicle(),"M03 starts Guess with approach transport and stages the depot team separately");
  Use(crew,CrewSlot.Guess);Game.Player.Character.Task.LeaveVehicle();Game.Player.Character.Position=c.Locations.Position("M03.RailJunction");m3.Tick();Game.GameTime+=7000;m3.Tick();
  Check(m3.CurrentStage==0&&m3.CurrentObjective.Contains("D-pad Right"),"M03 waits for explicit interaction and names the controller and keyboard buttons");
  Interact(m3,c,CrewSlot.Guess,c.Locations.Position("M03.RailJunction"),6);Check(m3.CurrentStage==1,"M03 rail interaction advances after six seconds");
  Use(crew,CrewSlot.Ice);m3.Tick();Check(m3.CurrentObjective.Contains("yellow ENTRY")&&m3.CurrentObjective.Contains("No ability"),"Switching to Ice explains the walking trigger and rules out an ability requirement");Game.Player.Character.Position=c.Locations.Position("M03.DepotGate");m3.Tick();Check(m3.CurrentStage==2,"M03 depot approach leads into the yard fight");
  m3.Tick();Check(m3.CurrentObjective.Contains("RED")&&m3.CurrentObjective.Contains("Remaining: 8"),"M03 yard combat names the red targets and keeps the count in the objective HUD");foreach(var enemy in World.Created)enemy.IsDead=true;m3.Tick();Check(m3.CurrentStage==3,"M03 loading unlocks only after the yard is clear");
  Interact(m3,c,CrewSlot.Gohan,c.Locations.Position("M03.CraneControls"),8);Check(m3.CurrentStage==4,"Gohan cargo-terminal interaction performs the eight-second load");
  var hauler=World.Vehicles[0];Use(crew,CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(hauler,VehicleSeat.Driver);m3.Tick();Check(m3.CurrentStage==5,"Guess takes the required Benson before delivery");
  var destination=c.Locations.Position("Base.CypressFlats");Game.Player.Character.SetIntoVehicle(new Vehicle{Position=destination},VehicleSeat.Driver);m3.Tick();Check(m3.Status==MissionStatus.Running,"M03 cannot finish by arriving in another car");
  hauler.Position=destination;Game.Player.Character.SetIntoVehicle(hauler,VehicleSeat.Driver);m3.Tick();Check(m3.Status==MissionStatus.Passed,"M03 complete production flow delivers the weapons truck");

  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"clarity4.json"));var m4=new M04SeveredWire();Check(m4.Begin(c),"M04 stages its surface operation with all essential actors");
  Check(c.Locations.Position("M04.Breaker").Z>0&&c.Locations.Position("M04.RampGuards").Z>0,"M04 no longer depends on the absent B3 interior");
  m4.Abort(); // The M04 flow is covered end to end by StoryToPlayTests.RunM04.

  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"clarity5.json"));var m5=new M05TidalLock();Check(m5.Begin(c),"M05 validates coast staging and creates both boats");
  var mateo=World.Created.Last();var dinghy=World.Vehicles[1];Check(crew.PedFor(CrewSlot.Guess).IsInVehicle(dinghy)&&crew.PedFor(CrewSlot.Gohan).IsInVehicle(dinghy),"M05 gives Guess and Gohan actual boat transport while Ice holds shore overwatch");
  foreach(var enemy in World.Created.Where(p=>p!=mateo))enemy.IsDead=true;m5.Tick();Check(m5.CurrentStage==1,"Shore clearance opens Guess's explicit flare interaction");
  Interact(m5,c,CrewSlot.Guess,c.Locations.Position("M05.CoveAir"),1,true);Check(m5.CurrentStage==2&&GTA.Native.Function.Values.ContainsKey(GTA.Native.Hash.SHOOT_SINGLE_BULLET_BETWEEN_COORDS),"M05 launches an actual flare after the player interaction");
  dinghy.Position=c.Locations.Position("M05.GrottoMouth");Use(crew,CrewSlot.Gohan);Game.Player.Character.Position=dinghy.Position;m5.Tick();Check(m5.CurrentStage==3&&mateo.Task.BoatTasks==1,"Gohan's boat approach starts Mateo's boat escape");
  dinghy.Position=World.Vehicles[0].Position;Game.Player.Character.Position=dinghy.Position;m5.Tick();Game.GameTime+=5001;m5.Tick();Check(m5.CurrentStage==4&&mateo.IsAlive,"Sustained close pursuit captures Mateo without killing him");
  Interact(m5,c,CrewSlot.Gohan,mateo.Position,3,true);Check(m5.CurrentStage==5&&m5.Status==MissionStatus.Running&&mateo.Exists(),"Mateo remains present while his revelation dialogue plays");
  c.Dialogue.Clear();m5.Tick();Check(m5.Status==MissionStatus.Passed&&mateo.Exists()&&!mateo.IsInvincible,"M05 ends after the dialogue and releases the surviving witness");
  var capture=new CaptureBoatObjective(()=>mateo,()=>World.Vehicles[0],()=>dinghy);capture.Enter(c);mateo.IsDead=true;capture.Update(c);Check(capture.Status==ObjectiveStatus.Failed,"A dead witness cannot incorrectly satisfy the capture objective");

  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"clarity6.json"));var m6=new M06CleanSweep();Check(m6.Begin(c),"M06 validates and deploys its separate work stations");
  Interact(m6,c,CrewSlot.Gohan,c.Locations.Position("M06.Feeder"),6);Use(crew,CrewSlot.Ice);Game.Player.Character.Position=c.Locations.Position("M06.SallyPort");m6.Tick();Check(m6.CurrentStage==2,"M06 power interaction and Ice's breach open the parallel burn and siege");
  crew.PedFor(CrewSlot.Gohan).Position=c.Locations.Position("M06.ServerRacks");
  for(int i=0;i<45&&m6.CurrentStage==2;i++){foreach(var enemy in World.Created)enemy.IsDead=true;Game.GameTime+=1000;m6.Tick();}
  Check(m6.CurrentStage==3,"M06 requires both the completed burn and all response waves before extraction");
  Use(crew,CrewSlot.Guess);m6.Tick();Check(m6.CurrentStage==3,"Guess cannot leave the extraction stage without his teammates");
  crew.PedFor(CrewSlot.Ice).SetIntoVehicle(World.Vehicles[0],VehicleSeat.RightFront);crew.PedFor(CrewSlot.Gohan).SetIntoVehicle(World.Vehicles[0],VehicleSeat.LeftRear);m6.Tick();Game.Player.WantedLevel=0;m6.Tick();Check(m6.Status==MissionStatus.Passed,"M06 completes with both teammates aboard and the police lost");

  Reset();crew=Roster();c=Context(crew);World.FailNavigation=true;Check(!new M04SeveredWire().Begin(c)&&World.Created.Count==0,"Unavailable walkable surfaces reject setup before spawning actors underground");World.FailNavigation=false;
  var interaction=new MissionInteraction("Terminal",()=>new Vector3(5,0,0),3);interaction.RequiredCharacter=CrewSlot.Gohan;interaction.Enter(c);Use(crew,CrewSlot.Gohan);Game.Player.Character.Position=new Vector3(5,0,0);Game.Accept=true;interaction.Update(c);Game.GameTime+=1500;Game.Player.Character.Position=Vector3.Zero;interaction.Update(c);Game.Player.Character.Position=new Vector3(5,0,0);Game.GameTime+=5000;interaction.Update(c);Check(!interaction.IsFinished&&interaction.Label.Contains("press"),"Leaving an interaction resets its timer and requires a new button press");
  var lines=MissionObjectiveHud.Wrap(new string('a',80)+" objective description").ToArray();Check(lines.All(l=>l.Length<=68)&&lines.Length>=2,"Long objective text wraps without overflowing the HUD");
  var injured=new Ped{Health=200,MaxHealth=200,Armor=0,CanSufferCriticalHits=true};CrewDurability.RestoreAfterSwitch(injured,430,65);Check(injured.MaxHealth==900&&injured.Health==430&&injured.Armor==65&&!injured.CanSufferCriticalHits,"Handover restores the larger health cap and existing injuries without a free heal");
  CrewDurability.RestoreAfterSwitch(injured,430,65);Check(injured.Health==430&&CrewDurability.Armor==100,"Repeated switches preserve damage and the increased armor limit");
 }
}
