using System;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

public static partial class StoryTests
{
 static void Mission610RepairChecks()
 {
  Reset();Game.TimeScale=1;GameUtils.RoadAvailable=true;var crew=Roster();var c=Context(crew);
  var m9=new M09RollingThunder();Check(m9.Begin(c),"M09 shot-window attempt starts");c.Cutscenes.Skip();m9.Tick();
  Use(crew,CrewSlot.Guess);var guess=Game.Player.Character;guess.SetIntoVehicle(m9.Frogger,VehicleSeat.Driver);m9.Frogger.HeightAboveGround=12;m9.Tick();
  m9.EscortTruck.Position=c.Locations.Position("M09.AmbushPoint");m9.Frogger.Position=m9.EscortTruck.Position+new Vector3(0,100,60);
  for(int i=0;i<4;i++){Game.GameTime+=1000;m9.Tick();}
  Check(m9.CurrentStage==2&&Game.TimeScale==1,"Shot window waits for actual Ice control");
  var driver=World.Created.First(p=>p.CurrentVehicle==m9.EscortTruck);
  Check(driver.Task.LastDrivePoint.DistanceTo(c.Locations.Position("M09.AmbushPoint"))>200f,"Convoy route continues through the shot position");
  Use(crew,CrewSlot.Ice);m9.Tick();Check(Math.Abs(Game.TimeScale-.3f)<.001f,"Ice receives slow motion near the moving escort");
  var wheel=new CharacterWheel("none");wheel.Open(CrewSlot.Ice);Game.GameTime+=5000;m9.Tick();wheel.Close();
  Check(Game.TimeScale==1,"Expired shot window cannot leave slow motion stuck when the switch wheel closes");
  driver.IsDead=true;m9.Tick();Check(Game.TimeScale==1&&m9.CurrentStage==3,"Driver kill advances and restores normal time");m9.Abort();

  Reset();Game.TimeScale=1;crew=Roster();c=Context(crew);m9=new M09RollingThunder();m9.Begin(c);c.Cutscenes.Skip();m9.JumpToStage(2);
  m9.EscortTruck.Position=c.Locations.Position("M09.AmbushPoint");Use(crew,CrewSlot.Ice);m9.Tick();m9.Abort();
  Check(Game.TimeScale==1,"Aborting during the shot restores normal time");
  Reset();crew=Roster();c=Context(crew);m9=new M09RollingThunder();m9.Begin(c);c.Cutscenes.Skip();
  guess=crew.PedFor(CrewSlot.Guess);guess.SetIntoVehicle(m9.Frogger,VehicleSeat.Driver);m9.Frogger.Position=c.Locations.Position("M09.Pickup");m9.Frogger.Speed=0;m9.Frogger.HeightAboveGround=0;
  m9.JumpToStage(5);Use(crew,CrewSlot.Ice);Game.Player.Character.Position=m9.Frogger.Position+new Vector3(2,0,0);Game.Accept=true;
  int flights=guess.Task.HeliTasks;m9.Tick();
  Check(Game.Player.Character.Task.LastSeat==VehicleSeat.RightFront&&Game.Player.Character.Task.Enters==1,"Context button requests a real Frogger passenger entry");
  Check(guess.Task.HeliTasks==flights&&m9.Frogger.LockStatus==VehicleLockStatus.Unlocked&&guess.IsInVehicle(m9.Frogger),"Landed pilot holds his seat without restarting overhead hover");m9.Abort();

  Reset();crew=Roster();c=Context(crew);Use(crew,CrewSlot.Guess);var lift=new Vehicle();var bed=new Vehicle{Position=new Vector3(30,0,0)};var crate=new Prop{Position=lift.Position};int secured=0;
  var load=new ForkliftDeliveryObjective("Guess: crate",()=>crate,()=>lift,()=>bed,new Vector3(0,1.6f,.75f),Vector3.Zero,()=>secured++){RequiredCharacter=CrewSlot.Guess};
  Game.Player.Character.SetIntoVehicle(lift,VehicleSeat.Driver);load.Enter(c);
  for(int i=0;i<2;i++){Game.GameTime+=500;load.Update(c);}
  Check(load.Carrying&&secured==0&&crate.AttachedTo==null,"Picked-up cargo is visual and cannot award delivery on pickup");
  lift.Position=load.Target;lift.Speed=5;for(int i=0;i<5;i++){Game.GameTime+=500;load.Update(c);}
  Check(!load.IsFinished&&secured==0,"Driving through the loading marker cannot secure cargo");
  lift.Speed=0;Use(crew,CrewSlot.Ice);for(int i=0;i<5;i++){Game.GameTime+=500;load.Update(c);}
  Check(!load.IsFinished,"Switching away pauses loading without moving or attaching the cargo");
  Use(crew,CrewSlot.Guess);bed.Speed=2;for(int i=0;i<5;i++){Game.GameTime+=500;load.Update(c);}
  Check(!load.IsFinished,"A moving flatbed cannot accept the crate");
  bed.Speed=0;for(int i=0;i<4;i++){Game.GameTime+=500;load.Update(c);}
  Check(load.Status==ObjectiveStatus.Complete&&secured==1&&crate.AttachedTo==bed,"Stopped player delivery secures one verified crate with no required animation");

  Reset();crew=Roster();c=Context(crew);var m8=new M08SupplyAndSever();m8.Begin(c);c.Cutscenes.Skip();
  Check(m8.Forklift.Position.DistanceTo(m8.Hauler.Position)<20f,"Flatbed starts on the forklift's side of the loading apron");
  m8.JumpToStage(1);Use(crew,CrewSlot.Ice);var sentries=World.Created.Where(p=>p.Model.Name=="s_m_m_security_01").ToList();sentries[0].IsDead=true;
  var gohan=crew.PedFor(CrewSlot.Gohan);gohan.Position=sentries[1].Position+new Vector3(10,0,0);m8.Tick();Game.GameTime+=2500;m8.Tick();
  Check(gohan.Task.LastTarget!=null&&sentries.Contains(gohan.Task.LastTarget)&&gohan.Task.LastTarget.IsAlive,"Inactive brother returns fire against a live mission enemy");m8.Abort();

  Reset();GameUtils.RoadAvailable=true;crew=Roster();c=Context(crew);
  var prior=new Vehicle{Model=new Model("flatbed"),IsPersistent=true,Position=c.Locations.Position("M08.Connector"),LockStatus=VehicleLockStatus.CannotEnter};
  var priorCrates=new[]{new Prop{Model=new Model("prop_mil_crate_01"),AttachedTo=prior},new Prop{Model=new Model("prop_mil_crate_01"),AttachedTo=prior}};
  World.NearbyVehicles=new[]{prior};World.NearbyProps=priorCrates;
  var reuse=new M10OpenThrottle();Check(reuse.Begin(c)&&reuse.Flatbed==prior&&reuse.Crates.Count==2&&reuse.Crates.All(p=>priorCrates.Contains(p)),"M10 reuses the existing loaded stash truck and both crates instead of overlapping it");
  Check(prior.LockStatus==VehicleLockStatus.Unlocked,"M08 stash lock is removed when the reused truck becomes M10's transport");reuse.Abort();
  Reset();GameUtils.RoadAvailable=false;crew=Roster();c=Context(crew);var noRoad=new M10OpenThrottle();noRoad.Begin(c);noRoad.JumpToStage(2);
  Check(noRoad.Status==MissionStatus.Failed&&noRoad.FailReason.Contains("road"),"Missing safe road spawns cannot silently pass the cartel chase");noRoad.Abort();

  Reset();GameUtils.RoadAvailable=true;crew=Roster();c=Context(crew);var m10=new M10OpenThrottle();Check(m10.Begin(c),"M10 safe cargo spawn starts");
  Check(m10.Crates.All(p=>p.AttachedTo==m10.Flatbed&&!p.CollisionEnabled),"Both engine crates are collision-free on the truck");
  m10.JumpToStage(2);var riders=World.Created.Where(p=>p.Model.Name=="g_m_y_mexgoon_03").ToList();
  Check(riders.Count==3&&riders.All(p=>p.IsInVehicle()&&p.SeatIndex==VehicleSeat.Driver&&p.Task.Warps==0&&p.Task.Chases>0),"Pursuit riders are seated synchronously before their chase orders");
  int chases=riders.Sum(p=>p.Task.Chases);Game.GameTime+=3100;m10.Tick();Check(riders.Sum(p=>p.Task.Chases)>chases,"Cartel pursuit refreshes against the cargo driver");
  foreach(var rider in riders)rider.IsDead=true;m10.Tick();
  var pilot=World.Created.First(p=>p.Model.Name=="s_m_y_blackops_01");
  Check(m10.Buzzard.IsEngineRunning&&pilot.IsInVehicle(m10.Buzzard)&&pilot.Task.HeliTasks>0&&pilot.Task.Warps==0,"Gunship has an active engine and verified pilot before attack task");m10.Abort();
 }
}
