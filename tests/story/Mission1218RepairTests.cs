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
 static void Mission1218RepairChecks()
 {
  Reset();var crew=Roster();var c=Context(crew);
  var surveyed=c.Locations.Get("M12.KrakenSpawn");surveyed.Position=new Vector3(1160,-3400,0);surveyed.Status=LocationStatus.Surveyed;
  var m12=new M12BlackTideRecon();Check(m12.Begin(c),"M12 accepts an explicit surveyed Kraken launch");
  Check(GameUtils.IsWithinFlat(m12.Rov.Position,surveyed.Position,.1f)&&crew.PedFor(CrewSlot.Gohan).IsInVehicle(m12.Rov),"Kraken uses its own water spawn and the diver starts aboard without a missing quay ladder");
  Check(c.Locations.Get("M12.KrakenReturn").Kind=="water"&&c.Locations.Get("M12.PatrolSpawn1").Kind=="water","Survey catalog includes return water and individual patrol launches");m12.Abort();

  Reset();crew=Roster();c=Context(crew);var m13=new M13SmugglersCut();Check(m13.Begin(c),"M13 fuel-boat startup passes water preflight");
  var tugs=World.Vehicles.Where(v=>v.Model.Name=="tug").ToArray();
  Check(tugs.Length==3&&tugs.All(v=>v.Heading==315f),"Three fuel hulls follow the diagonal deep-water channel");
  Check(tugs.SelectMany((v,i)=>tugs.Skip(i+1).Select(other=>v.Position.DistanceTo(other.Position))).All(d=>d>45),"Fuel hulls remain separated by more than a boat length");m13.Abort();

  Reset();crew=Roster();c=Context(crew);var original=c.Locations.Position("M13.BargeOne");
  Function.MarineFloor=p=>GameUtils.IsWithinFlat(p,original,1f)?-2f:-40f;
  m13=new M13SmugglersCut();Check(m13.Begin(c),"M13 recovers a blocked estimated center using a nearby full-hull water check");
  tugs=World.Vehicles.Where(v=>v.Model.Name=="tug").ToArray();
  Check(tugs.Length==3&&tugs[0].Position.DistanceTo(original)>5f&&tugs[0].Position.DistanceTo(original)<=31f,"M13 alternate stays inside the local 30m basin limit");
  Check(tugs.SelectMany((v,i)=>tugs.Skip(i+1).Select(other=>v.Position.DistanceTo(other.Position))).All(d=>d>44f),"Relocation preserves separate real-size tug footprints");m13.Abort();

  Reset();crew=Roster();c=Context(crew);original=c.Locations.Position("M13.BargeOne");
  c.Locations.Record("M13.BargeOne",original,315f);
  Function.MarineFloor=p=>GameUtils.IsWithinFlat(p,original,1f)?-2f:-40f;
  m13=new M13SmugglersCut();Check(!m13.Begin(c)&&!World.Vehicles.Any(v=>v.Model.Name=="tug"),"A blocked surveyed barge is reported without relocating it or spawning partial fuel boats");

  Reset();crew=Roster();c=Context(crew);original=c.Locations.Position("M13.BargeOne");var blockedThird=c.Locations.Position("M13.BargeThree");
  Function.MarineFloor=p=>GameUtils.IsWithinFlat(p,original,1f)||GameUtils.IsWithinFlat(p,blockedThird,60f)?-2f:-40f;
  m13=new M13SmugglersCut();Check(!m13.Begin(c)&&!World.Vehicles.Any(v=>v.Model.Name=="tug")&&GameUtils.IsWithinFlat(c.Locations.Position("M13.BargeOne"),original,.01f),"A later failed hull rolls back prior relocation and leaves no partial barges");

  Reset();crew=Roster();c=Context(crew);Function.MarineBlocked=true;
  m13=new M13SmugglersCut();Check(!m13.Begin(c)&&!World.Vehicles.Any(v=>v.Model.Name=="tug"),"M13 still refuses a basin with no clear underwater hull footprint");

  Reset();crew=Roster();c=Context(crew);int waitBefore=Script.Waited;
  Function.MarineObstruction=(from,to)=>Script.Waited<waitBefore+250;
  m13=new M13SmugglersCut();Check(m13.Begin(c)&&Script.Waited>=waitBefore+250,"M13 retries the full hull after a streaming yield even when center seabed is already visible");m13.Abort();

  Reset();crew=Roster();c=Context(crew);var m14=new M14AirspaceBlackout();m14.Begin(c);c.Cutscenes.Skip();Use(crew,CrewSlot.Ice);
  Game.Player.Character.IsShooting=true;m14.Tick();
  var apron=World.Created.Where(p=>p.Model.Name=="s_m_y_blackops_02").ToArray();
  Check(apron.All(p=>p.Task.LastTarget==Game.Player.Character),"Apron guards respond to Ice's shot instead of holding their guard task");
  Game.Player.Character.IsShooting=false;foreach(var enemy in apron)enemy.IsDead=true;m14.Tick();Use(crew,CrewSlot.Guess);
  Game.Player.Character.Position=c.Locations.Position("M14.HangarDoor");m14.Tick();
  var defenders=World.Created.Where(p=>p.Model.Name=="s_m_y_blackops_02"&&!p.IsDead).ToArray();
  Check(defenders.Length==3&&defenders.All(p=>p.Task.LastTarget==Game.Player.Character),"Guess reaching the hangar triggers its separate armed response");
  Game.Player.Character.SetIntoVehicle(m14.Plane,VehicleSeat.Driver);m14.Tick();
  Check(m14.CurrentStage==3&&defenders.All(p=>p.IsAlive),"Taking the aircraft does not require killing the hangar defenders");m14.Abort();

  Reset();crew=Roster();c=Context(crew);var m15=new M15Crawlspace();Check(m15.Begin(c),"M15 has its required service office and cabinet");c.Cutscenes.Skip();m15.JumpToStage(2);Use(crew,CrewSlot.Ice);
  var guards=World.Created.Where(p=>p.Model.Name=="s_m_m_security_01").ToArray();
  Check(guards.Length==3&&guards.All(p=>p.Health==p.MaxHealth&&p.Health>=500),"Dock workers have enough health to survive the stun hit");
  foreach(var guard in guards)guard.LastWeaponHit=(uint)WeaponHash.StunGun;
  m15.Tick();Check(m15.CurrentStage==3&&guards.All(p=>p.IsCuffed&&p.IsInvincible&&!p.IsDead),"A recorded stun hit scores even after the brief stunned flag has cleared");
  foreach(var guard in guards)guard.LastWeaponHit=0;
  int ragdolls=Function.Calls.Count(h=>h.Item1==Hash.SET_PED_TO_RAGDOLL);Game.GameTime+=12000;m15.Tick();
  Check(Function.Calls.Count(h=>h.Item1==Hash.SET_PED_TO_RAGDOLL)>=ragdolls+3,"Incapacitation continues through the splice stage so guards cannot get back up");m15.Abort();
  var dead=new Ped{IsDead=true};var nonlethal=new SubdueTargetsObjective("Alive",()=>new[]{dead});nonlethal.Update(c);
  Check(nonlethal.Status==ObjectiveStatus.Failed,"A dead guard still fails the nonlethal contract");

  Reset();crew=Roster();c=Context(crew);Function.Values[Hash.SET_MAX_WANTED_LEVEL]=3;var m16=new M16TheHeavyLift();m16.Begin(c);c.Cutscenes.Skip();
  Check(new[]{CrewSlot.Guess,CrewSlot.Ice,CrewSlot.Gohan}.All(slot=>crew.PedFor(slot).IsInVehicle(m16.Granger)),"M16 crew starts together in the approach car on the freeway");
  Use(crew,CrewSlot.Ice);m16.JumpToStage(2);var ice=Game.Player.Character;int orders=ice.Task.Enters;m16.Tick();
  Check(ice.Task.Enters==orders,"Mandatory switch does not take the active Ice's controls to board him");
  Use(crew,CrewSlot.Guess);m16.Tick();Check(ice.Task.Enters==orders+1&&!Game.Player.Character.IsInVehicle(m16.Cargobob),"Ice starts boarding while Guess is still approaching the lift");
  Game.Player.Character.SetIntoVehicle(m16.Cargobob,VehicleSeat.Driver);m16.Tick();
  Check(Function.Calls.Any(h=>h.Item1==Hash.CLEAR_AREA_OF_PROJECTILES),"The stolen lift has local departure countermeasures");
  int countermeasures=Function.Calls.Count(h=>h.Item1==Hash.CLEAR_AREA_OF_PROJECTILES);m16.Cargobob.Position+=new Vector3(2000f,0f,0f);m16.Tick();
  Check(Function.Calls.Count(h=>h.Item1==Hash.CLEAR_AREA_OF_PROJECTILES)==countermeasures,"Projectile suppression ends after clearing the base area");m16.Abort();
  Check(Convert.ToInt32(Function.Values[Hash.SET_MAX_WANTED_LEVEL])==3,"Abort restores the wanted maximum that existed before the mission");

  Reset();crew=Roster();c=Context(crew);var weld=c.Locations.Get("M17.WeldTwo");weld.Position+=new Vector3(7,2,0);weld.Status=LocationStatus.Surveyed;var saved=weld.Position;
  var m17=new M17SubZeroPayload();Check(m17.Begin(c),"M17 dock workshop starts with an afloat Kraken");
  Check(GameUtils.IsWithinFlat(weld.Position,saved,.01f),"M17 no longer overwrites the player's surveyed weld point with hull dimensions");
  Check(GameUtils.IsWithinFlat(m17.Kraken.Position,c.Locations.Position("M17.KrakenSpawn"),.1f)&&World.Props.Count(p=>p.Model.Name=="prop_tool_bench02")==4,"Kraken uses water while four work stations remain on the dock");
  c.Cutscenes.Skip();Use(crew,CrewSlot.Gohan);Interact(m17,c,CrewSlot.Gohan,c.Locations.Position("M17.WeldOne"),8);m17.Tick();
  Check(m17.Parts.Count==1&&m17.Parts[0].AttachedTo==m17.Kraken&&!m17.Parts[0].CollisionEnabled,"A completed station transfers a real collision-free module to the sub");m17.Abort();

  Reset();crew=Roster();c=Context(crew);var m18=new M18TheStagingLine();m18.Begin(c);c.Cutscenes.Skip();m18.Tick();
  Check(m18.CurrentStage==0&&m18.Kraken.Position.DistanceTo(c.Locations.Position("M18.ChannelMark"))>75,"M18 cannot finish the sub leg at its starting position");
  Check(m18.Cargobob.Position.DistanceTo(c.Locations.Position("M18.SaltHangar"))>150&&m18.Hauler.Position.DistanceTo(c.Locations.Position("M18.HaulerMark"))>250,"Both land-side staging deliveries require meaningful travel");
  Use(crew,CrewSlot.Guess);m18.Tick();Use(crew,CrewSlot.Ice);m18.Tick();
  Check(crew.CompanionAI.StateOf(CrewSlot.Guess)==CompanionState.Scripted&&crew.CompanionAI.StateOf(CrewSlot.Gohan)==CompanionState.Scripted,"Switching away restores mission ownership rather than free-roam follow behavior");
  Check(m18.Cargobob.IsPositionFrozen&&m18.Kraken.IsPositionFrozen,"Inactive parked staging craft hold their places");
  Use(crew,CrewSlot.Guess);m18.Tick();Check(!m18.Cargobob.IsPositionFrozen,"Switching back releases the selected vehicle for player control");m18.Abort();
  Check(!m18.Kraken.IsPositionFrozen&&!m18.Cargobob.IsPositionFrozen&&!m18.Hauler.IsPositionFrozen,"Abort releases every staging vehicle freeze");
  Reset();crew=Roster();c=Context(crew);m18=new M18TheStagingLine();m18.Begin(c);c.Cutscenes.Skip();m18.JumpToStage(2);m18.Pod.RejectAttachments=true;
  Interact(m18,c,CrewSlot.Guess,c.Locations.Position("M18.PodWork"),6);
  var rear=(Vector3)typeof(M18TheStagingLine).GetMethod("RearWork",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static).Invoke(null,new object[]{m18.Cargobob});
  Interact(m18,c,CrewSlot.Guess,rear,5);
  Check(m18.Status==MissionStatus.Failed&&!m18.PodFitted,"A failed jammer attachment cannot record a successful mount");m18.Cleanup();

 }
}
