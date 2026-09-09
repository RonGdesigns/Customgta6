using System;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
using GTA.Native;
public static partial class RegressionTests
{
 static void MilitaryLifecycleChecks()
 {
  MilitaryPressureChecks();MovementFailureChecks();
  Reset();World.Vehicles.Clear();var ai=new CompanionController(new ModConfig());var player=Game.Player.Character;player.ForwardVector=new Vector3(0,1,0);
  ai.Military.SetLevel(CrewSlot.Ice,6);
  // Simulate healthy moving units; stranded replacement is exercised separately.
  for(int i=0;i<600;i++){Game.GameTime+=100;ai.MissionActive=false;ai.Military.Update(CrewSlot.Ice,true,1);foreach(var v in World.Vehicles)v.Speed=10;}
  Check(World.Vehicles.Count==3&&World.Vehicles.TrueForAll(v=>v.Present),"Main-loop MissionActive=false assignments do not reset cooldowns or spawn an army every frame");
  ai.Life.Wanted.Set(CrewSlot.Gohan,6);ai.Military.Update(CrewSlot.Gohan,true,1);
  Check(World.Vehicles.Count==3&&World.Vehicles.TrueForAll(v=>v.Present),"Switching between heroes sharing six-star heat preserves the same pursuit units");
  var convoy=World.Vehicles[1];
  Check(convoy.GetPedOnSeat(VehicleSeat.LeftRear)!=null&&convoy.GetPedOnSeat(VehicleSeat.RightRear)!=null&&Function.DriveBys>0,"Military convoy carries rear-seat gunners with drive-by tasks");
  var tank=World.Vehicles[2];player.Position=tank.Position+new Vector3(0,100,0);Function.TankShots=0;Function.ClearLos=false;
  Game.GameTime+=10000;ai.Military.Update(CrewSlot.Ice,true,1);Check(Function.TankShots==0,"Tank does not fire through obstructed line of sight");
  Function.ClearLos=true;Game.GameTime+=100;ai.Military.Update(CrewSlot.Ice,true,1);Check(Function.TankShots==1,"Seated tank driver selects the mounted cannon and requests a real shot");
  for(int i=0;i<50;i++){Game.GameTime+=100;ai.Military.Update(CrewSlot.Ice,true,1);}Check(Function.TankShots==1,"Tank firing interval prevents per-frame cannon spam");
  Function.Sprites=0;ai.Military.Update(CrewSlot.Ice,true,1,true);Check(Function.Sprites==12&&Function.Values.ContainsKey(Hash.HIDE_HUD_COMPONENT_THIS_FRAME),"Six wanted stars draw once with shadows in place of the native five-star row");
  ai.MissionActive=true;Check(World.Vehicles.TrueForAll(v=>!v.Present),"Entering a mission removes military-owned entities instead of abandoning them");
  Reset();World.Vehicles.Clear();ai=new CompanionController(new ModConfig());Game.Player.Character.ForwardVector=new Vector3(0,1,0);Game.Player.WantedLevel=5;
  for(int i=0;i<900;i++){Game.GameTime+=100;ai.MissionActive=false;ai.Military.Update(CrewSlot.Ice,true,1);}
  Check(World.Vehicles.Count==0,"Repeated main-loop state assignments preserve the ninety-second escalation timer");
  Game.GameTime+=100;ai.Military.Update(CrewSlot.Ice,true,1);Check(World.Vehicles.Count==0&&ai.Military.Level(CrewSlot.Ice)==6,"Sixth star activates before its first dispatch arrives");
  Game.GameTime+=3999;ai.Military.Update(CrewSlot.Ice,true,1);Check(World.Vehicles.Count==0,"First military dispatch respects its four-second delay");
  Game.GameTime++;ai.Military.Update(CrewSlot.Ice,true,1);Check(World.Vehicles.Count==1&&World.Vehicles[0].Model.IsHelicopter,"First arrival is a helicopter, with ground forces still pending");ai.Military.Clear();
  Reset();var crew=new CrewRoster{ActivePed=Game.Player.Character};var recovery=new DeathController(new ModConfig(),crew,new Bloodlines.Missions.MissionManager(),new Bloodlines.Abilities.AbilityController(),new SwitchController(),new DialogueDirector());
  crew.ActivePed.IsDead=true;recovery.Update();Game.GameTime+=901;recovery.Update();Game.GameTime+=1501;recovery.Update();
  World.CollisionReady=false;Game.GameTime+=501;recovery.Update();
  Check(recovery.IsHandling&&crew.ActivePed.IsPositionFrozen,"Recovery waits for destination collision rather than releasing a falling player");
  Game.GameTime+=5100;recovery.Update();
  Check(!recovery.IsHandling&&crew.Dismissals==1&&Game.Player.CanControlCharacter&&!GameUtils.Faded,"A failed recovery verification returns to the story character instead of an endless control lock");
 }

 static void MilitaryPressureChecks()
 {
  Reset();World.Vehicles.Clear();var player=Game.Player.Character;player.ForwardVector=new Vector3(0,1,0);
  var response=new MilitaryResponse(new PersonalWanted());response.SetLevel(CrewSlot.Ice,6);
  Game.GameTime+=3999;response.Update(CrewSlot.Ice,true,1);Check(World.Vehicles.Count==0,"Faster dispatch remains staggered, with no immediate army");
  Game.GameTime++;response.Update(CrewSlot.Ice,true,1);Check(World.Vehicles.Count==1,"Helicopter arrives four seconds after authorization");
  Game.GameTime+=7999;response.Update(CrewSlot.Ice,true,1);Check(World.Vehicles.Count==1,"Ground units do not arrive before their own dispatch time");
  Game.GameTime++;response.Update(CrewSlot.Ice,true,1);Check(World.Vehicles.Count==2,"Convoy arrives after twelve seconds");
  Game.GameTime+=8000;response.Update(CrewSlot.Ice,true,1);Check(World.Vehicles.Count==3,"Tank arrives after twenty seconds instead of fifty");
  var tank=World.Vehicles[2];var driver=tank.GetPedOnSeat(VehicleSeat.Driver);
  Game.GameTime+=1100;response.Update(CrewSlot.Ice,true,1);Check(driver.Task.LastMission==VehicleMissionType.GoTo&&driver.Task.VehicleMissions>0,"Distant tank receives a road approach task instead of waiting to attack from afar");
  tank.Position=player.Position+new Vector3(0,90,0);Game.GameTime+=4001;response.Update(CrewSlot.Ice,true,1);
  Check(driver.Task.LastMission==VehicleMissionType.Attack,"Tank transitions to attack when it reaches clear firing range");
  Function.ClearLos=false;Game.GameTime+=4001;response.Update(CrewSlot.Ice,true,1);Check(driver.Task.LastMission==VehicleMissionType.GoTo,"Obstructed nearby target makes the tank seek a new approach");
  tank.IsOnScreen=true;Game.GameTime+=31000;response.Update(CrewSlot.Ice,true,1);Check(tank.Present,"Stranded-unit cleanup cannot visibly remove a tank in front of the player");
  tank.IsOnScreen=false;tank.Position=new Vector3(0,160,0);Game.GameTime+=100;response.Update(CrewSlot.Ice,true,1);
  Game.GameTime+=30100;response.Update(CrewSlot.Ice,true,1);int before=World.Vehicles.Count;
  Check(!tank.Present,"A stationary tank stuck outside useful range is retired off camera");
  for(int n=0;n<39;n++){Game.GameTime+=100;response.Update(CrewSlot.Ice,true,1);}Check(World.Vehicles.Count==before,"Replacement cooldown prevents cleanup from creating a per-frame spawn loop");
  Game.GameTime+=100;response.Update(CrewSlot.Ice,true,1);Check(World.Vehicles.Count==before+1&&World.Vehicles.FindAll(v=>v.Present).Count<=3,"Replacement arrives after its cooldown and respects the three-unit cap");response.Clear();
  Reset();World.Vehicles.Clear();player=Game.Player.Character;player.ForwardVector=new Vector3(0,1,0);response=new MilitaryResponse(new PersonalWanted());response.SetLevel(CrewSlot.Ice,6);
  Game.GameTime+=4000;response.Update(CrewSlot.Ice,true,1);
  int candidates=0;World.StreetResolver=p=>{candidates++;return candidates==1?p+new Vector3(0,0,50):p;};
  Game.GameTime+=8000;response.Update(CrewSlot.Ice,true,1);Check(candidates==2&&World.Vehicles.Count==2&&World.Vehicles[1].Position.Z==player.Position.Z,"Ground dispatch rejects the wrong road elevation and tries another street");
  World.StreetResolver=p=>Vector3.Zero;Game.GameTime+=8000;response.Update(CrewSlot.Ice,true,1);Check(World.Vehicles.Count==2,"No valid road postpones a tank without placing it at the world origin");
  World.StreetResolver=null;Game.GameTime+=2999;response.Update(CrewSlot.Ice,true,1);Check(World.Vehicles.Count==2,"Failed road placement observes a bounded retry delay");
  Game.GameTime++;response.Update(CrewSlot.Ice,true,1);Check(World.Vehicles.Count==3,"A newly available approach is retried after three seconds");response.Clear();
 }
 static void MovementFailureChecks()
 {
  Reset();var crew=new CrewRoster{ActivePed=Game.Player.Character};var recovery=new DeathController(new ModConfig(),crew,new Bloodlines.Missions.MissionManager(),new Bloodlines.Abilities.AbilityController(),new SwitchController(),new DialogueDirector());
  crew.ActivePed.IsDead=true;recovery.Update();Game.GameTime+=901;recovery.Update();Game.GameTime+=1501;recovery.Update();World.CollisionReady=true;Game.GameTime+=501;recovery.Update();
  Game.GameTime+=10000;recovery.Update();Check(recovery.IsHandling&&crew.Dismissals==0,"Idle player is allowed to wait at respawn without being mistaken for a movement failure");
  Function.MovementInput=1;for(int n=0;n<16;n++){Game.GameTime+=100;recovery.Update();}
  Check(recovery.IsHandling&&crew.Dismissals==0,"Blocked movement receives one control-repair attempt while switches stay blocked");
  for(int n=0;n<24;n++){Game.GameTime+=100;recovery.Update();}
  Check(!recovery.IsHandling&&crew.Dismissals==1&&Game.Player.CanControlCharacter,"Four seconds of blocked walking input returns safely to the story character");
 }
}
