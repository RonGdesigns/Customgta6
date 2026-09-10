using System;
using System.Collections.Generic;
using System.IO;
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
 static void Round2Checks()
 {
  // ---- 1. Briefing arrival: Guess stands at the start, the others drive up, the lines wait for the car.
  Reset();var crew=Roster();var c=Context(crew);crew.IsDeployed=false;crew.Peds.Clear();crew.ActivePed=null;var story=Game.Player.Character;
  story.Position=new Vector3(100,100,10);story.ForwardVector=new Vector3(0,1,0);World.Created.Clear();World.Vehicles.Clear();GTA.UI.Screen.Subtitle=null;
  Check(c.Cutscenes.Play("M04","intro","Severed Wire"),"M04's briefing plays with no crew deployed");
  var car=World.Vehicles.FirstOrDefault(v=>v.Model.Name=="schafter3");
  Check(car!=null&&car.Position.DistanceTo(story.Position)>60f,"The crew's four-door spawns down the street, not beside the start");
  var gohan=World.Created.FirstOrDefault(p=>p.CurrentVehicle==car);var host=World.Created.FirstOrDefault(p=>p.CurrentVehicle==null);
  Check(gohan!=null&&car.GetPedOnSeat(VehicleSeat.Driver)==gohan&&host!=null&&host.Position.DistanceTo(story.Position)<3f&&!story.IsVisible,"Gohan drives in; Guess stands at the start point in place of the hidden story character");
  Check(GTA.UI.Screen.Subtitle==null,"No line plays before the car has arrived");
  c.Cutscenes.Update();
  Check(!car.IsPositionFrozen&&!gohan.IsPositionFrozen&&gohan.Task.Drives==1&&GTA.UI.Screen.Subtitle==null,"The first frame unfreezes the car and starts the drive; the dialogue still waits");
  car.Position=story.Position;car.Speed=0f;c.Cutscenes.Update();c.Cutscenes.Update();c.Cutscenes.Update();
  Check(GTA.UI.Screen.Subtitle!=null&&GTA.UI.Screen.Subtitle.Contains("GOHAN"),"The briefing's first line starts once the car has pulled up");
  c.Cutscenes.Stop();Check(story.IsVisible&&!car.Present&&!gohan.Present,"The scene ends with the story character visible and the temporary car and cast gone");
  // Skipping lands the car at the curb before the lines run.
  Reset();crew=Roster();c=Context(crew);crew.IsDeployed=false;crew.Peds.Clear();crew.ActivePed=null;story=Game.Player.Character;story.Position=new Vector3(100,100,10);story.ForwardVector=new Vector3(0,1,0);World.Vehicles.Clear();
  c.Cutscenes.Play("M04","intro","Severed Wire");car=World.Vehicles.First(v=>v.Model.Name=="schafter3");c.Cutscenes.Update();c.Cutscenes.Skip();
  Check(car.Position==story.Position&&c.Cutscenes.LastOutcome==SceneOutcome.Skipped,"A skipped arrival puts the car on the mark before the scene ends");
  // With the crew deployed nothing is staged and no car appears.
  Reset();crew=Roster();c=Context(crew);World.Vehicles.Clear();World.Created.Clear();c.Cutscenes.Play("M04","intro","Severed Wire");
  Check(World.Vehicles.Count==0&&World.Created.Count==0,"A deployed crew acts in person: no arrival car, no temporary cast");c.Cutscenes.Stop();

  // ---- 2. M04 is exercised end to end in StoryToPlayTests.RunM04 (both Miller outcomes).

  // ---- 3. M05: an estimated perch snaps to the real ground before the walkable check.
  Reset();crew=Roster();c=Context(crew);var perch=c.Locations.Get("M05.CliffPerch");float authored=perch.Position.Z;World.GroundHeight=authored+40f;World.FailNavigationNear=perch.Position;
  Check(MissionSites.Ground(c.Locations,"M05.CliffPerch")&&Math.Abs(c.Locations.Position("M05.CliffPerch").Z-(authored+40.6f))<0.2f,"An estimated perch is placed on the real ground even 40 m above the authored guess");
  World.GroundHeight=0f;World.FailNavigationNear=null;
  Reset();crew=Roster();c=Context(crew);World.FailNavigation=true;
  Check(!MissionSites.Ground(c.Locations,"M05.CliffPerch")&&GameUtils.Message.Contains("F11"),"No walkable ground still refuses the mission and tells the player to survey the key");World.FailNavigation=false;

  // ---- 4. M06: the second and third waves come in by helicopter; the first aircraft is on camera once.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"round2-m6.json"));var m6=new M06CleanSweep();Check(m6.Begin(c),"M06 sets up");
  var alley=c.Locations.Position("M06.AlleyHold");
  Interact(m6,c,CrewSlot.Gohan,c.Locations.Position("M06.Feeder"),6);Use(crew,CrewSlot.Ice);Game.Player.Character.Position=c.Locations.Position("M06.SallyPort");m6.Tick();Check(m6.CurrentStage==2,"The burn and the siege open together");
  crew.PedFor(CrewSlot.Gohan).Position=c.Locations.Position("M06.ServerRacks");
  int before=World.Created.Count;m6.Tick();Game.GameTime+=6100;m6.Tick();
  var waveOne=World.Created.Skip(before).ToList();
  Check(waveOne.Count==4&&World.Vehicles.Count(v=>v.Model.Name=="polmav")==0&&waveOne.All(t=>t.Task.HatedFights==1),"The first wave is already on the street and fighting");
  foreach(var t in waveOne)t.IsDead=true;m6.Tick();before=World.Created.Count;int vehiclesBefore=World.Vehicles.Count;Game.GameTime+=6100;m6.Tick();
  var helis=World.Vehicles.Skip(vehiclesBefore).Where(v=>v.Model.Name=="polmav").ToList();var waveTwo=World.Created.Skip(before).ToList();
  Check(helis.Count==2&&waveTwo.Count==7,"The second wave is five troopers and two pilots in two Mavericks");
  Check(helis.All(h=>h.GetPedOnSeat(VehicleSeat.Driver)!=null&&h.GetPedOnSeat(VehicleSeat.LeftRear)!=null&&h.GetPedOnSeat(VehicleSeat.RightRear)!=null&&h.GetPedOnSeat(VehicleSeat.RightFront)==null),"Two troopers ride the rappel seats of each aircraft");
  Check(helis.All(h=>h.Position.DistanceTo(alley)>200f&&h.Position.Z>alley.Z+50f&&h.GetPedOnSeat(VehicleSeat.Driver).Task.HeliTasks==1),"Each aircraft starts high on its own approach line with a flight task");
  var ropes=waveTwo.Where(t=>t.CurrentVehicle!=null&&t.SeatIndex!=VehicleSeat.Driver).ToList();var street=waveTwo.Where(t=>t.CurrentVehicle==null).ToList();
  Check(ropes.Count==4&&street.Count==1&&street[0].Task.HatedFights==1&&ropes.All(t=>t.Task.HatedFights==0),"Four ride, one comes by the street; the riders hold their task until they are down");
  c.Cutscenes.Update();
  Check(c.Cutscenes.IsActive&&GTA.UI.Screen.Subtitle!=null&&GTA.UI.Screen.Subtitle.Contains("ICE"),"The first aircraft's arrival is framed as a moment with Ice's call");
  c.Cutscenes.Stop();
  foreach(var h in helis){h.Position=alley+new Vector3(0,0,20f);h.HeightAboveGround=20f;}
  m6.Tick();Check(ropes.All(t=>t.Task.Rappels==1&&!t.IsInVehicle()),"On station the troopers go down the ropes");
  m6.Tick();Check(ropes.All(t=>t.Task.HatedFights==1)&&helis.All(h=>h.GetPedOnSeat(VehicleSeat.Driver).Task.HeliTasks==2),"Landed troopers fight and the aircraft leave");
  foreach(var t in ropes.Concat(street))t.IsDead=true;m6.Tick();before=World.Created.Count;vehiclesBefore=World.Vehicles.Count;Game.GameTime+=6100;m6.Tick();
  var waveThree=World.Created.Skip(before).ToList();var lateHelis=World.Vehicles.Skip(vehiclesBefore).ToList();
  Check(waveThree.Count==8&&lateHelis.Count==2&&!c.Cutscenes.IsActive,"The third wave also flies in, without a second cutscene");
  var lateRopes=waveThree.Where(t=>t.CurrentVehicle!=null&&t.SeatIndex!=VehicleSeat.Driver).ToList();
  Game.GameTime+=46000;m6.Tick();
  Check(lateRopes.All(t=>!t.IsInVehicle()&&t.Task.HatedFights==1&&t.Position.DistanceTo(alley)<15f),"An aircraft that never arrives still puts its troopers on the ground so the wave can end");
  foreach(var t in waveThree.Where(t=>t.SeatIndex!=VehicleSeat.Driver))t.IsDead=true;for(int i=0;i<30&&m6.CurrentStage==2;i++){Game.GameTime+=1000;m6.Tick();}
  Check(m6.CurrentStage==3,"With every wave down the extraction opens");

  // ---- 5. Apartment: readiness is judged from inside the room.
  Reset();crew=Roster();var access=new ApartmentAccess(crew);var player=Game.Player.Character;var outside=player.Position;var inside=new Vector3(347,-999,-99);
  Function.InteriorId=123;Function.InteriorReady=false;World.CollisionReady=false;
  Check(access.Begin(inside,null,true),"Entry begins");Game.GameTime+=300;access.Update();
  Check(player.Position==inside&&access.Busy&&player.IsPositionFrozen&&GameUtils.Faded,"The player is placed inside as soon as the room is pinned, frozen and behind the fade");
  access.Update();Check(access.Busy,"Nothing is released before the room's collision has loaded around the player");
  World.CollisionReady=true;Function.EntityInterior=123;access.Update();
  Check(access.Inside&&!access.Busy&&!player.IsPositionFrozen&&!GameUtils.Faded,"Being in the room with its collision loaded counts as ready even when the readiness native never says so");
  access.Cancel();Function.EntityInterior=null;Function.InteriorReady=true;World.CollisionReady=false;
  Check(player.Position==outside,"Cancel returns to the street");

  // ---- 6. Most jobs pay in weapons; each hero's line is unique; the shop follows.
  Check(WeaponProgression.RewardMissions.Length>=30,"Most jobs carry a weapon reward");
  bool unique=true,every=true;
  foreach(var hero in Protagonist.All){var seen=new HashSet<uint>();foreach(var mission in WeaponProgression.RewardMissions){if(!WeaponProgression.ReceivesReward(mission,hero.Slot))continue;var rewards=WeaponProgression.Rewards(mission);if(rewards.Length!=3||rewards[(int)hero.Slot]==0){every=false;continue;}if(!seen.Add((uint)rewards[(int)hero.Slot]))unique=false;}}
  Check(every&&unique,"Every reward mission names three weapons and no hero is handed the same weapon twice");
  Check(WeaponProgression.ReceivesReward("SM02",CrewSlot.Gohan)&&!WeaponProgression.ReceivesReward("SM02",CrewSlot.Ice)&&WeaponProgression.ReceivesReward("SM03",CrewSlot.Guess)&&!WeaponProgression.ReceivesReward("SM03",CrewSlot.Gohan),"The remaining solo jobs pay their owner only");
  var shopState=CampaignState.Load(Path.Combine(root,"round2-shop.json"));
  Check(WeaponMarket.LockedUntil(shopState,CrewSlot.Ice,(uint)WeaponHash.MG)=="M09"&&WeaponMarket.LockedUntil(shopState,CrewSlot.Ice,(uint)WeaponHash.PumpShotgun)=="M03","The MG is earned in Rolling Thunder and the pump in Cypress Foundry, not bought");
  Check(WeaponMarket.LockedUntil(shopState,CrewSlot.Ice,(uint)WeaponHash.Pistol)==null&&WeaponMarket.LockedUntil(shopState,CrewSlot.Ice,(uint)WeaponHash.AssaultRifle)==null,"The shop's stock guns stay purchasable");
  shopState.MarkComplete("M09",new MissionCatalog());new WeaponProgression(shopState).UnlockRewards();
  Check(shopState.Weapons["Ice"].Contains((uint)WeaponHash.MG)&&shopState.Weapons["Gohan"].Contains((uint)WeaponHash.StickyBomb)&&shopState.Weapons["Guess"].Contains((uint)WeaponHash.CompactRifle),"Finishing M09 puts each hero's weapon in his locker");

  // ---- 7. The QA menu may start any scripted job; the mission key still enforces order.
  Reset();crew=Roster();c=Context(crew);var order=CampaignState.Load(Path.Combine(root,"round2-order.json"));var cat=new MissionCatalog();
  var locked=new MissionDefinition{Info=new MissionInfo{Id="M02",Title="Locked",Prerequisite="M01"},Factory=()=>new ProbeMission()};cat.All.Add(locked);
  var manager=new MissionManager(c,order,cat);
  Check(!manager.CanStart(locked,out string why)&&why.Contains("M01"),"The mission key still refuses a job whose prerequisite is unfinished");
  Check(manager.CanStart(locked,out why,bypassGates:true),"The QA menu may start any scripted job regardless of order");
  string menu=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Core","DevMenu.cs"));
  Check(!menu.Contains("Finish \" + captured.Info.Prerequisite + \" first.")&&menu.Contains("bypassGates: true"),"The dev menu no longer turns a job away for order");
 }
}
