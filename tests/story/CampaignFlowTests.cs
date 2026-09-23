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
   .Where(t=>t.Name.StartsWith("SM")||t.Name.StartsWith("BM")||int.Parse(t.Name.Substring(1,2))>=7).OrderBy(t=>t.Name).ToArray();
  Check(types.Length==74,"All 74 later, solo and bonus production mission classes are covered by the flow harnesses");
  foreach(var type in types)
  {
   Reset();var crew=Roster();var c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,type.Name+".json"));var m=(ComposedMission)Activator.CreateInstance(type);
   // The tower missions open an MLO through the apartment access service, the way the host
   // wires it; without one MazeBank.Enter refuses and the lift beat never opens.
   c.Interior=new ApartmentAccess(crew);
   if(m.Id=="M07")GTA.Native.Function.Seabed=55f; // M07 refuses a world with no roof over the street at its key; this world has one.
   if(m.Id=="M23")World.CollisionReady=true;
   // The tower and suite floors are opened through the access service, which releases the
   // player only onto loaded collision; this world has it.
   if(m.Id=="M64"||m.Id=="M65"||m.Id=="SM07"||m.Id=="SM08")World.CollisionReady=true;
   if(m.Id=="M10")GameUtils.RoadAvailable=true; if(m.Id=="SM02")World.CollisionReady=true;
   if(m.Id=="SM03")
   {
    var owned=new OwnedVehicle{Id=1,ModelName="sultanrs",ModelHash=(uint)Game.GenerateHash("sultanrs"),Garage="bay-guess",Label="Flow race car"};c.State.Vehicles.Add(owned);
    c.Garages=new GarageService(crew,c.State,c.Locations,null);c.Garages.Allowed=()=>true;
    var sprintCar=c.Garages.Retrieve(owned);Game.Player.Character.Position=sprintCar.Position;Game.Player.Character.SetIntoVehicle(sprintCar,VehicleSeat.Driver);
   }
   Check(m.Begin(c),m.Id+" starts with essential assets and valid stages");
   var flow=Flow(m);var cueIds=flow.SelectMany(s=>s.EntryCues.Concat(s.ExitCues)).ToArray();
   Check(cueIds.Length==cueIds.Distinct().Count()&&cueIds.All(id=>c.Data.Cue(id)!=null),m.Id+" uses unique, valid dialogue cues at gameplay events");
   // M31-M40 have dedicated physical custody, boarding, convoy and marine walkthroughs.
   if(m is Bloodlines.Missions.Campaign.PreparationOperation&&!m.Id.StartsWith("BM")&&!(m.Id.StartsWith("M")&&int.Parse(m.Id.Substring(1))>=49)){m.Abort();Check(m.Status==MissionStatus.Aborted,m.Id+" supports clean abort before its dedicated walkthrough");continue;}
   for(int tick=0;tick<800&&m.Status==MissionStatus.Running;tick++)
   {
    if(c.Cutscenes.IsActive){c.Cutscenes.Skip();if(c.Cutscenes.LastRequired&&c.Cutscenes.LastOutcome==SceneOutcome.Failed)throw new Exception(m.Id+" required scene failed in flow harness");continue;}
    var stage=flow[m.CurrentStage];
    // The instrument objectives are worked, not waited out, and the same way whichever
    // mission they turn up in: hold the throttle until the needle is in the band, then let
    // go. Decided for the stage rather than per objective - a Protect sharing the stage
    // would otherwise release the throttle the gauge just asked for - and by type, so the
    // next mission to adopt one needs no case of its own.
    var gauge=stage.Objectives.OfType<Bloodlines.Missions.Objectives.GaugeObjective>().FirstOrDefault(o=>!o.IsFinished);
    var align=stage.Objectives.OfType<Bloodlines.Missions.Objectives.AlignObjective>().FirstOrDefault(o=>!o.IsFinished);
    GTA.Native.Function.Held[(Control)71]=(gauge!=null&&!gauge.InBand)||(align!=null&&!align.Locked);
    GTA.Native.Function.Held[(Control)72]=false;
    foreach(var objective in stage.Objectives.Where(o=>!o.IsFinished))
    {
     if(objective.RequiredCharacter.HasValue&&!objective.IsPassive)Use(crew,objective.RequiredCharacter.Value);
     string name=objective.GetType().Name;
     if(name=="BunkerAccessObjective" && !((Bloodlines.Missions.Campaign.M23GhostInTheSage)m).Interior.Busy) PositionActor(c,objective,objective.AssignmentPosition.Value);
     if(name=="ConditionObjective"&&m.Id=="M25")
     {
      var canyon=(Bloodlines.Missions.Campaign.M25BountyHuntersCanyon)m;
      crew.PedFor(CrewSlot.Guess).SetIntoVehicle(canyon.Boat,VehicleSeat.Driver);
      crew.PedFor(CrewSlot.Ice).SetIntoVehicle(canyon.Boat,VehicleSeat.Passenger);
      canyon.Boat.Position=canyon.DepartureOrigin+new Vector3(101,0,0);
     }
     if(name=="ConditionObjective"&&m.Id=="M15")
     {
      if(m.CurrentStage==0)Game.Player.Character.Task.LeaveVehicle();
      else if(m.CurrentStage==4)
      {var arrivalCar=((Bloodlines.Missions.Campaign.M15Crawlspace)m).Granger;crew.PedFor(CrewSlot.Guess).SetIntoVehicle(arrivalCar,VehicleSeat.Driver);crew.PedFor(CrewSlot.Ice).SetIntoVehicle(arrivalCar,VehicleSeat.LeftRear);crew.PedFor(CrewSlot.Gohan).SetIntoVehicle(arrivalCar,VehicleSeat.RightRear);}
     }
     if(m.Id=="M09"&&name=="ConditionObjective"&&m.CurrentStage==0)
     {var v=Field<Vehicle>(m,"_frogger");v.HeightAboveGround=12;v.IsInAir=true;}
     if(name=="ConvoyOverwatchObjective")
     {var v=Field<Func<Vehicle>>(objective,"_aircraft")();var escort=Field<Func<Vehicle>>(objective,"_escort")();PositionActor(c,objective,escort.Position+new Vector3(0,120,50),v);v.HeightAboveGround=50;v.IsInAir=true;}
     if(name=="ConditionObjective"&&m.Id=="M22")PositionActor(c,objective,Field<Vector3>(m,"_regroup"));
     // M45's step-off: Ice leaves the helicopter and stands on the deck. He used to be
     // asked to reach a zone the helicopter was already inside, which passed while he
     // was still strapped in 24 meters above it.
     // M26 asks Guess to sit on the second spotter rather than wait out a timer: Ron
     // found rolling up and doing nothing unsatisfying, and the clock only runs while he
     // is on the wing.
     // M27's pickup waits for Ice to be in the boat, whoever steered it there.
     if(name=="ConditionObjective"&&m.Id=="M27")
     {
      var boat=((Bloodlines.Missions.Campaign.M27FlightRisk)m).Dinghy;
      var ice=crew.PedFor(CrewSlot.Ice);
      if(boat!=null&&boat.Exists()&&ice!=null&&!ice.IsInVehicle(boat))
      {ice.Task.LeaveVehicle();ice.Position=boat.Position;ice.SetIntoVehicle(boat,VehicleSeat.RightFront);}
     }
     if(name=="ConditionObjective"&&m.Id=="M26")
     {
      var spotters=((Bloodlines.Missions.Campaign.M26AlamoScramble)m).Spotters;
      if(spotters.Count>1&&spotters[1]!=null&&spotters[1].Exists())
      {Game.Player.Character.Position=spotters[1].Position+new Vector3(20,0,0);Game.GameTime+=1000;}
     }
     if(name=="ConditionObjective"&&m.Id=="M45")
     {
      var ice=crew.PedFor(CrewSlot.Ice);ice.Task.LeaveVehicle();
      var pad=c.Locations.Position("M45.Helipad");ice.Position=pad;
      // The boarding beat asks for the real thing now - out of the Kraken, above the
      // waterline, at the stern platform - rather than a zone check that completed while
      // Gohan was still sitting in the boat and then threw out of the stage exit.
      var gohan=crew.PedFor(CrewSlot.Gohan);gohan.Task.LeaveVehicle();
      var board=c.Locations.Position("M45.Board");
      gohan.Position=new Vector3(board.X,board.Y,System.Math.Max(board.Z,Bloodlines.Missions.Campaign.PaletoSite.WaterlineDeck+1f));
     }
     if(name=="ConditionObjective"&&m.Id=="M48")
     {
      var truck=((Bloodlines.Missions.Campaign.M48TheRoadBackSouth)m).Technical;
      if(truck!=null)foreach(var hero in Protagonist.All)
       crew.PedFor(hero.Slot).SetIntoVehicle(truck,hero.Slot==CrewSlot.Guess?VehicleSeat.Driver:hero.Slot==CrewSlot.Ice?VehicleSeat.RightFront:VehicleSeat.LeftRear);
     }
     if(name=="ConditionObjective"&&m.Id=="M47")
     {
      var collapse=(Bloodlines.Missions.Campaign.M47PaletoCollapse)m;
      if(!collapse.Jumped)
       foreach(var slot in new[]{CrewSlot.Ice,CrewSlot.Gohan})
       {var swimmer=crew.PedFor(slot);swimmer.Task.LeaveVehicle();swimmer.Position=new Vector3(collapse.Boat.Position.X,collapse.Boat.Position.Y,0f);}
      else
       foreach(var hero in Protagonist.All)
        crew.PedFor(hero.Slot).SetIntoVehicle(collapse.Boat,hero.Slot==CrewSlot.Guess?VehicleSeat.Driver:hero.Slot==CrewSlot.Ice?VehicleSeat.RightFront:VehicleSeat.LeftRear);
     }
     // Only on the beat that asks for the rig to roll. The airport stage has an arrival
     // condition too, and setting the rig moving there would keep it from ever stopping.
     if(name=="ConditionObjective"&&m.Id=="M67"&&objective.Label.Contains("rig on the road"))
     {
      var rig=((Bloodlines.Missions.Campaign.M67ScorchedGrid)m).Semi;
      if(rig!=null){Game.Player.Character.SetIntoVehicle(rig,VehicleSeat.Driver);rig.Speed=18;}
     }
     if(name=="ConditionObjective"&&m.Id=="M68")
     {
      var rig=((Bloodlines.Missions.Campaign.M68BloodBrothersTheDrain)m).Rig;
      if(rig!=null){Game.Player.Character.SetIntoVehicle(rig,VehicleSeat.Driver);rig.Speed=18;}
     }
     // The Act III condition beats. Each is a state the harness has to produce rather than
     // a place it can stand in: a man dead, or a man three hundred meters below a roof.
     if(name=="ConditionObjective"&&m.Id=="SM07")
     {
      var sterling=((Bloodlines.Missions.Campaign.SM07BloodDebt)m).Sterling;
      if(sterling!=null)sterling.IsDead=true;
     }
     // Only on the beat that asks for it. M65's first condition is the lift, and killing
     // Vance there is exactly what the mission's own rule fails - as it should.
     if(name=="ConditionObjective"&&m.Id=="M65"&&objective.Label.Contains("take Vance"))
     {
      var vance=((Bloodlines.Missions.Campaign.M65ExecutivePrivilege)m).Vance;
      if(vance!=null)vance.IsDead=true;
     }
     // M70 ends when the plane is in the sea, which is a state the harness produces.
     if(name=="ConditionObjective"&&m.Id=="M70")
     {
      var titan=((Bloodlines.Missions.Campaign.M70BloodBrothersGroundedTitan)m).Plane;
      if(titan!=null)titan.IsInWater=true;
     }
     if(name=="ConditionObjective"&&m.Id=="M66")
      Game.Player.Character.Position=Bloodlines.Missions.Campaign.MazeBank.Roof
       -new Vector3(0,0,Bloodlines.Missions.Campaign.M66TheSpireEvacuation.JumpedBelow+20f);
     // The rest of Act III. Every main mission from M49 derives from PreparationOperation
     // and the walker used to abort all of them before their first stage, so none of these
     // beats had ever been driven - which is where M57's first-kill stage and M65's
     // fail-on-success hid. A beat is satisfied by producing its state, never by faking it.
     if(name=="ConditionObjective"&&m.Id=="M49")
      foreach(var tower in ((Bloodlines.Missions.Campaign.M49ReturnToTheConcrete)m).Towers) if(tower!=null)tower.IsDead=true;
     if(name=="ConditionObjective"&&m.Id=="M52")
     {
      var strike=(Bloodlines.Missions.Campaign.M52JudicialStrike)m;
      if(objective.Label.Contains("hold until")) strike.Harrison.Position=c.Locations.Position("M52.Walk");
      else if(strike.Harrison!=null) strike.Harrison.IsDead=true;
     }
     if(name=="ConditionObjective"&&m.Id=="M54")
     {
      var heli=((Bloodlines.Missions.Campaign.M54ThePillboxRedoubt)m).Helicopter;
      if(heli!=null){crew.PedFor(CrewSlot.Ice).SetIntoVehicle(heli,VehicleSeat.LeftRear);crew.PedFor(CrewSlot.Gohan).SetIntoVehicle(heli,VehicleSeat.RightRear);}
     }

     // A travel leg is finished by actually being there, in the named vehicle, stopped.
     if(name=="TravelObjective") PositionActor(c,objective,Field<Func<Vector3>>(objective,"_destination")(),Field<Func<Vehicle>>(objective,"_vehicle")?.Invoke());
     else if(name=="ReachZoneObjective") PositionActor(c,objective,Field<Func<Vector3>>(objective,"_position")());
     else if(name=="MissionInteraction") PositionActor(c,objective,Field<Func<Vector3>>(objective,"_position")(),Field<Func<Vehicle>>(objective,"_vehicle")?.Invoke());
     // SurfaceSubObjective is a real 3D/driver check. Move the simulated craft
     // to the target; do not force-pass it or weaken the production depth rule.
     else if(name=="SurfaceSubObjective") PositionActor(c,objective,Field<Func<Vector3>>(objective,"_target")(),Field<Func<Vehicle>>(objective,"_sub")());
     else if(name=="TechnicalChoiceObjective") {PositionActor(c,objective,Field<Func<Vector3>>(objective,"_position")());Game.GameTime+=CutsceneDirector.SkipGraceMs;Game.Accept=true;}
     else if(name=="EnterVehicleObjective") {var v=Field<Func<Vehicle>>(objective,"_vehicle")();PositionActor(c,objective,v.Position,v);var seat=Field<VehicleSeat>(objective,"_seat");if(seat!=VehicleSeat.Any)Game.Player.Character.SetIntoVehicle(v,seat); if(Field<bool>(objective,"_requireCrew")) foreach(var hero in Protagonist.All.Where(h=>h.Slot!=crew.ActiveSlot)) crew.PedFor(hero.Slot)?.SetIntoVehicle(v,hero.Slot==CrewSlot.Ice?VehicleSeat.RightFront:VehicleSeat.LeftRear);}
     else if(name=="DeliverVehicleObjective"||name=="OccupiedVehicleDestination") PositionActor(c,objective,Field<Func<Vector3>>(objective,"_destination")(),Field<Func<Vehicle>>(objective,"_vehicle")());
     else if(name=="FuelUnload"&&m is Bloodlines.Missions.Campaign.SM06CanyonRunner)
     {
      var solo=(Bloodlines.Missions.Campaign.SM06CanyonRunner)m;var bay=c.Locations.Position("SM06.Delivery");
      PositionActor(c,objective,bay,solo.Truck);solo.Tanker.Position=bay;solo.Tanker.Speed=0;
      GTA.Native.Function.Trailers[solo.Truck.Handle]=solo.Tanker;
     }
     else if(name=="KillTargetsObjective") foreach(var ped in Field<Func<IEnumerable<Ped>>>(objective,"_targets")())ped.IsDead=true;
     else if(name=="SubdueTargetsObjective") foreach(var ped in Field<Func<IEnumerable<Ped>>>(objective,"_targets")())ped.IsBeingStunned=true;
     else if(name=="DestroyVehicleObjective") Field<Func<Vehicle>>(objective,"_vehicle")().IsDriveable=false;
     else if(name=="ShootDownObjective") Field<Func<Vehicle>>(objective,"_target")().IsDriveable=false;
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
     else if(name=="ForkliftDeliveryObjective") {var cargo=(ForkliftDeliveryObjective)objective;PositionActor(c,objective,cargo.Target,Field<Func<Vehicle>>(objective,"_forklift")());}
     else if(name=="ForksUnderCrateObjective") {var v=Field<Func<Vehicle>>(objective,"_forklift")();PositionActor(c,objective,Field<Func<Vector3>>(objective,"_pad")(),v);}
     else if(name=="DynoObjective") {var v=Field<Func<Vehicle>>(objective,"_vehicle")();PositionActor(c,objective,v.Position,v);GTA.Native.Function.Held[(Control)71]=Field<float>(objective,"_pressure")<24f;}
     else if(name=="BailOutObjective")Game.Player.Character.Task.LeaveVehicle();
     else if(name=="LoseWantedObjective")Game.Player.WantedLevel=0;
     else if(name=="RaceCheckpointObjective") {var sites=Field<IList<Vector3>>(objective,"_checkpoints");PositionActor(c,objective,sites[Field<int>(objective,"_index")],Field<Func<Vehicle>>(objective,"_vehicle")());}
    }
    c.Dialogue.Clear();Game.GameTime+=CutsceneDirector.SkipGraceMs;Game.Accept=true;Game.GameTime+=1000;m.Tick();
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
