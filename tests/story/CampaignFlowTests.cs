using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

public static partial class StoryTests
{
 static T Field<T>(object obj,string name) => (T)obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(obj);
 static List<MissionStage> Flow(ComposedMission mission) => (List<MissionStage>)typeof(ComposedMission).GetField("_stages",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(mission);
 static void PositionActor(MissionContext c, Objective objective, Vector3 point, Vehicle vehicle=null)
 {
  if(objective.RequiredCharacter.HasValue) Use(c.Crew,objective.RequiredCharacter.Value);
  var ped=Game.Player.Character;
  if(vehicle!=null) {vehicle.Position=point;vehicle.Speed=0;vehicle.HeightAboveGround=0;ped.SetIntoVehicle(vehicle,VehicleSeat.Driver);}
  else {ped.Task.LeaveVehicle();ped.Position=point;}
 }
 static void CampaignFlowChecks()
 {
  var types=typeof(ComposedMission).Assembly.GetTypes().Where(t=>!t.IsAbstract&&t.IsSubclassOf(typeof(ComposedMission))&&t.Namespace=="Bloodlines.Missions.Campaign")
   .Where(t=>t.Name.StartsWith("SM")||int.Parse(t.Name.Substring(1,2))>=7).OrderBy(t=>t.Name).ToArray();
  Check(types.Length==30,"All 30 later and solo production mission classes are in the flow harness");
  foreach(var type in types)
  {
   Reset();var crew=Roster();var c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,type.Name+".json"));var m=(ComposedMission)Activator.CreateInstance(type);
   Check(m.Begin(c),m.Id+" starts with essential assets and valid stages");
   var flow=Flow(m);var cueIds=flow.SelectMany(s=>s.EntryCues.Concat(s.ExitCues)).ToArray();
   Check(cueIds.Length==cueIds.Distinct().Count()&&cueIds.All(id=>c.Data.Cue(id)!=null),m.Id+" uses unique, valid dialogue cues at gameplay events");
   for(int tick=0;tick<800&&m.Status==MissionStatus.Running;tick++)
   {
    if(c.Cutscenes.IsActive){c.Cutscenes.Skip();if(c.Cutscenes.LastRequired&&c.Cutscenes.LastOutcome==SceneOutcome.Failed)throw new Exception(m.Id+" required scene failed in flow harness");continue;}
    var stage=flow[m.CurrentStage];
    foreach(var objective in stage.Objectives.Where(o=>!o.IsFinished))
    {
     if(objective.RequiredCharacter.HasValue&&!objective.IsPassive)Use(crew,objective.RequiredCharacter.Value);
     string name=objective.GetType().Name;
     if(name=="ConditionObjective"&&m.Id=="M22")PositionActor(c,objective,Field<Vector3>(m,"_regroup"));
     else if(name=="ReachZoneObjective") PositionActor(c,objective,Field<Func<Vector3>>(objective,"_position")());
     else if(name=="MissionInteraction") PositionActor(c,objective,Field<Func<Vector3>>(objective,"_position")(),Field<Func<Vehicle>>(objective,"_vehicle")?.Invoke());
     // SurfaceSubObjective is a real 3D/driver check. Move the simulated craft
     // to the target; do not force-pass it or weaken the production depth rule.
     else if(name=="SurfaceSubObjective") PositionActor(c,objective,Field<Func<Vector3>>(objective,"_target")(),Field<Func<Vehicle>>(objective,"_sub")());
     else if(name=="TechnicalChoiceObjective") {PositionActor(c,objective,Field<Func<Vector3>>(objective,"_position")());Game.Accept=true;}
     else if(name=="EnterVehicleObjective") {var v=Field<Func<Vehicle>>(objective,"_vehicle")();PositionActor(c,objective,v.Position,v); if(Field<bool>(objective,"_requireCrew")) foreach(var hero in Protagonist.All.Where(h=>h.Slot!=crew.ActiveSlot)) crew.PedFor(hero.Slot)?.SetIntoVehicle(v,hero.Slot==CrewSlot.Ice?VehicleSeat.RightFront:VehicleSeat.LeftRear);}
     else if(name=="DeliverVehicleObjective") PositionActor(c,objective,Field<Func<Vector3>>(objective,"_destination")(),Field<Func<Vehicle>>(objective,"_vehicle")());
     else if(name=="KillTargetsObjective") foreach(var ped in Field<Func<IEnumerable<Ped>>>(objective,"_targets")())ped.IsDead=true;
     else if(name=="SubdueTargetsObjective") foreach(var ped in Field<Func<IEnumerable<Ped>>>(objective,"_targets")())ped.IsBeingStunned=true;
     else if(name=="DestroyVehicleObjective") Field<Func<Vehicle>>(objective,"_vehicle")().IsDriveable=false;
     else if(name=="TrailerDeliveryObjective")
     {
      var truck=Field<Func<Vehicle>>(objective,"_truck")();var trailer=Field<Func<Vehicle>>(objective,"_trailer")();var point=Field<Func<Vector3>>(objective,"_destination")();
      PositionActor(c,objective,point,truck);trailer.Position=point;trailer.Speed=0;
      GTA.Native.Function.Trailers[truck.Handle]=trailer;
     }
     else if(name=="MultiHoldObjective")
     {
      var sites=Field<List<Vector3>>(objective,"_sites");var done=Field<HashSet<int>>(objective,"_done");
      if(done.Count<sites.Count)PositionActor(c,objective,sites[Enumerable.Range(0,sites.Count).First(i=>!done.Contains(i))],Field<Func<Vehicle>>(objective,"_vehicle")?.Invoke());
     }
     else if(name=="SurviveWavesObjective")foreach(var ped in ((SurviveWavesObjective)objective).Spawned)ped.IsDead=true;
     else if(name=="ShadowTargetObjective")Game.Player.Character.Position=Field<Func<Entity>>(objective,"_target")().Position+new Vector3((Field<float>(objective,"_minDistance")+Field<float>(objective,"_maxDistance"))/2,0,0);
     else if(name=="AssignedWorkObjective") {var worker=crew.PedFor(Field<CrewSlot>(objective,"_worker"));worker.Task.LeaveVehicle();worker.Position=Field<Func<Vector3>>(objective,"_target")();}
     else if(name=="ForksUnderCrateObjective") {var v=Field<Func<Vehicle>>(objective,"_forklift")();PositionActor(c,objective,Field<Func<Vector3>>(objective,"_pad")(),v);}
     else if(name=="DynoObjective") {var v=Field<Func<Vehicle>>(objective,"_vehicle")();PositionActor(c,objective,v.Position,v);v.CurrentRPM=.625f;}
     else if(name=="BailOutObjective")Game.Player.Character.Task.LeaveVehicle();
     else if(name=="LoseWantedObjective")Game.Player.WantedLevel=0;
     else if(name=="RaceCheckpointObjective") {var sites=Field<IList<Vector3>>(objective,"_checkpoints");PositionActor(c,objective,sites[Field<int>(objective,"_index")],Field<Func<Vehicle>>(objective,"_vehicle")());}
    }
    c.Dialogue.Clear();Game.Accept=true;Game.GameTime+=1000;m.Tick();
   }
   Check(m.Status==MissionStatus.Passed,m.Id+" reaches pass through real objective updates: "+m.FailReason+" / "+m.CurrentObjective);
  }
  Reset();var roster=Roster();var ctx=Context(roster);var car=new Vehicle();var goal=new Vector3(100,100,20);
  var delivery=new DeliverVehicleObjective("Land",()=>car,()=>goal,20,true){RequiredCharacter=CrewSlot.Guess};Use(roster,CrewSlot.Guess);car.Position=goal;delivery.Update(ctx);
  Check(!delivery.IsFinished,"A parked target vehicle cannot satisfy delivery while player is elsewhere");
  Game.Player.Character.SetIntoVehicle(car,VehicleSeat.Driver);car.HeightAboveGround=12;delivery.Update(ctx);Check(!delivery.IsFinished,"Aircraft flyby cannot satisfy landing");
  car.HeightAboveGround=0;car.Speed=10;delivery.Update(ctx);Check(!delivery.IsFinished,"Landing requires slowing the required aircraft");car.Speed=0;delivery.Update(ctx);Check(delivery.IsFinished,"Correct owner completes only after landing and slowing");
  var guard=new Ped();var subdue=new SubdueTargetsObjective("Nonlethal",()=>new[]{guard});guard.IsDead=true;subdue.Update(ctx);Check(subdue.Status==ObjectiveStatus.Failed,"Killing a nonlethal target fails instead of silently completing");
  var missing=new DestroyVehicleObjective("Missing",()=>null);missing.Update(ctx);Check(missing.Status==ObjectiveStatus.Failed,"Missing destruction target rejects progress");
  var wave=new SurviveWavesObjective("Wave",i=>new Ped[0],1,0);wave.Update(ctx);Check(wave.Status==ObjectiveStatus.Failed,"Failed wave spawn cannot auto-pass the fight");
  var heat=roster.CompanionAI.Life.Wanted;heat.Set(CrewSlot.Ice,2);heat.Set(CrewSlot.Gohan,0);Use(roster,CrewSlot.Guess);Game.Player.WantedLevel=0;World.CollisionReady=true;
  var sw=new SwitchController(roster);Game.GameTime+=1000;Check(sw.TrySwitch(CrewSlot.Ice)&&Game.Player.WantedLevel==2,"Switching to an off-duty wanted character restores their stars");
  Game.Player.WantedLevel=3;Game.GameTime+=1000;Check(sw.TrySwitch(CrewSlot.Guess)&&Game.Player.WantedLevel==0&&heat.Get(CrewSlot.Ice)==3,"Switching back restores clean character and remembers the other's escalation");
  var sharedCar=new Vehicle();roster.PedFor(CrewSlot.Guess).SetIntoVehicle(sharedCar,VehicleSeat.Driver);roster.PedFor(CrewSlot.Gohan).SetIntoVehicle(sharedCar,VehicleSeat.Passenger);Game.Player.WantedLevel=3;Game.GameTime+=1000;
  Check(sw.TrySwitch(CrewSlot.Gohan)&&Game.Player.WantedLevel==3,"Passenger handover retains the wanted shared car's pursuit level");
  Use(roster,CrewSlot.Guess);var airborne=new Vehicle{Position=new Vector3(200,0,300),IsInAir=true,HeightAboveGround=300};roster.PedFor(CrewSlot.Ice).SetIntoVehicle(airborne,VehicleSeat.Driver);GTA.Native.Function.EjectOnSwitch=true;Game.GameTime+=1000;
  Check(sw.TrySwitch(CrewSlot.Ice)&&Game.Player.Character.IsInVehicle(airborne)&&Game.Player.Character.SeatIndex==VehicleSeat.Driver,"Airborne handover restores the actual pilot seat after native ejection");GTA.Native.Function.EjectOnSwitch=false;
  Use(roster,CrewSlot.Guess);roster.PedFor(CrewSlot.Ice).CurrentVehicle=airborne;airborne.Seats.Clear();Game.GameTime+=1000;
  Check(!sw.TrySwitch(CrewSlot.Ice)&&Game.Player.Character==roster.PedFor(CrewSlot.Guess),"Missing physical seat rejects an aerial switch before transferring the player");
  var water=ctx.Locations.Get("M19.HullBreach");MissionSites.Prepare(ctx.Locations,"M19");var firstDepth=water.Position.Z;MissionSites.Prepare(ctx.Locations,"M19");Check(water.Position.Z==firstDepth,"Repeated mission preparation does not drift an underwater objective's depth");
  roster.CompanionAI.MissionActive=true;Game.Player.WantedLevel=4;Game.GameTime+=1000;Check(sw.TrySwitch(CrewSlot.Gohan)&&Game.Player.WantedLevel==4,"Mission switching preserves scripted wanted level");
 }
}
