using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloodlines.Abilities;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

public static partial class StoryTests
{
 static Vehicle Car(VehicleClass cls,string name="car"){return new Vehicle{ClassType=cls,DisplayName=name,Model=new Model(name)};}

 static void PackageABChecks()
 {
  // ---- B1. Road profile: per class, once, derived from originals, restored where still ours.
  Reset();var sedan=Car(VehicleClass.Sedans,"primo");var h=sedan.HandlingData;float tMax=h.TractionCurveMax,brake=h.BrakeForce,comZ=h.CenterOfMassOffset.Z,damp=h.SuspensionCompressionDamping;
  var road=new RoadHandling();
  Check(RoadHandling.Classify(sedan)==RoadHandling.Class.Car&&RoadHandling.Classify(Car(VehicleClass.SUVs))==RoadHandling.Class.Suv&&RoadHandling.Classify(Car(VehicleClass.Industrial))==RoadHandling.Class.Truck,"Vehicle classes map to car, SUV and truck profiles");
  Check(RoadHandling.Classify(new Vehicle{Model=new Model("shinobi")})==RoadHandling.Class.None&&RoadHandling.Classify(new Vehicle{Model=new Model("longfin")})==RoadHandling.Class.None&&RoadHandling.Classify(new Vehicle{Model=new Model("supervolito")})==RoadHandling.Class.None,"Bikes, boats and helicopters take no road profile");
  Check(road.Apply(h,sedan)&&road.Applied==RoadHandling.Class.Car,"The sedan takes the car profile");
  Check(Math.Abs(h.TractionCurveMax-tMax*1.15f)<1e-4&&Math.Abs(h.BrakeForce-brake*1.4f)<1e-4&&Math.Abs(h.CenterOfMassOffset.Z-(comZ-0.12f))<1e-4&&Math.Abs(h.SuspensionCompressionDamping-damp*1.25f)<1e-4&&Math.Abs(h.TractionLossMultiplier-0.85f)<1e-4&&Math.Abs(h.InertiaMultiplier.Z-1.1f)<1e-4,"Car profile: grip, brakes, damping up; center of mass down; traction loss down; yaw inertia up");
  Check(!road.Apply(h,sedan)&&Math.Abs(h.TractionCurveMax-tMax*1.15f)<1e-4,"A second apply is refused: values are never multiplied twice");
  Check(road.Restore(h)&&Math.Abs(h.TractionCurveMax-tMax)<1e-4&&Math.Abs(h.BrakeForce-brake)<1e-4&&Math.Abs(h.CenterOfMassOffset.Z-comZ)<1e-4&&Math.Abs(h.InertiaMultiplier.Z-1f)<1e-4,"Restore puts every original back");
  road=new RoadHandling();road.Apply(h,sedan);h.BrakeForce=99f;road.Restore(h);
  Check(Math.Abs(h.BrakeForce-99f)<1e-4&&Math.Abs(h.TractionCurveMax-tMax)<1e-4,"A value another mod changed is left alone; the rest is restored");
  var truck=Car(VehicleClass.Industrial,"benson");var th=truck.HandlingData;float tBrake=th.BrakeForce;new RoadHandling().Apply(th,truck);
  Check(Math.Abs(th.BrakeForce-tBrake*1.3f)<1e-4&&Math.Abs(th.InertiaMultiplier.Z-1f)<1e-4&&Math.Abs(th.CenterOfMassOffset.Z-(0.1f-0.10f))<1e-4,"Truck profile is gentler and leaves yaw inertia alone");
  // Through WorldTuning: one profile per model, restored on stand-down.
  Reset();var crew=Roster();var tuning=new WorldTuning();var granger=Car(VehicleClass.SUVs,"granger");var gh=granger.HandlingData;float gMax=gh.TractionCurveMax;World.Vehicles.Add(granger);
  tuning.Update(crew);
  Check(Math.Abs(gh.TractionCurveMax-gMax*1.12f)<1e-4&&Math.Abs(gh.InitialDriveMaxFlatVelocity-120f)<1e-3,"WorldTuning applies the SUV road profile alongside the doubled gearing");
  Game.GameTime+=1500;tuning.Update(crew);Check(Math.Abs(gh.TractionCurveMax-gMax*1.12f)<1e-4,"A rescan does not stack the profile");
  crew.IsDeployed=false;tuning.Update(crew);
  Check(Math.Abs(gh.TractionCurveMax-gMax)<1e-4&&Math.Abs(gh.InitialDriveMaxFlatVelocity-60f)<1e-3,"Stand-down restores grip with gearing");

  // ---- B2. Guess overlay: planted on wheels, off in the air, restored on exit and vehicle change, never on aircraft.
  Reset();crew=Roster();var guess=crew.PedFor(CrewSlot.Guess);var coupe=Car(VehicleClass.Coupes,"schafter3");coupe.Velocity=new Vector3(0,40,0);coupe.ForwardVector=new Vector3(0,1,0);guess.SetIntoVehicle(coupe,VehicleSeat.Driver);
  var reflex=new SlipstreamReflex();float lat0=coupe.HandlingData.TractionCurveLateral,max0=coupe.HandlingData.TractionCurveMax;
  reflex.Activate(guess);Check(GTA.Native.Function.Values[GTA.Native.Hash.SET_TIME_SCALE].Equals(0.45f),"Activation slows time to the recognizable 0.45");
  reflex.Update(guess);Check(reflex.Vehicle==coupe&&Math.Abs(coupe.HandlingData.TractionCurveMax-max0*1.2f)<1e-4&&Math.Abs(coupe.HandlingData.TractionCurveLateral-lat0*1.2f)<1e-4,"On the first frame the grip lift is on the car's shared handling");
  for(int i=0;i<10;i++){Game.GameTime+=100;reflex.Update(guess);}
  Check(reflex.Blend>0.99f&&coupe.Forces>0&&coupe.LastForce.Z<0&&Math.Abs(coupe.LastForce.Z+SlipstreamReflex.Press(40f))<1e-3&&coupe.LastForce.X==0&&coupe.LastForce.Y==0,"Half a second later the full press is on the instance: weight plus speed-squared downforce, straight down");
  Check(Math.Abs(SlipstreamReflex.Press(0f)-2.45f)<1e-4&&Math.Abs(SlipstreamReflex.Press(50f)-(2.45f+5.9f))<1e-4,"At rest the press is the quarter-g weight alone; it never exceeds 0.85 g");
  Check(Math.Abs(SlipstreamReflex.Downforce(50f)-5.9f)<1e-4&&Math.Abs(SlipstreamReflex.Downforce(100f)-5.9f)<1e-4&&SlipstreamReflex.Downforce(25f)<1.5f&&SlipstreamReflex.Downforce(0f)==0f,"Downforce is capped at 0.6 g and zero at rest");
  coupe.IsInAir=true;for(int i=0;i<10;i++){Game.GameTime+=100;reflex.Update(guess);}
  int airborneForces=coupe.Forces;Game.GameTime+=100;reflex.Update(guess);
  Check(reflex.Blend<0.01f&&coupe.Forces==airborneForces,"Airborne, the press blends out and stops: jumps are not pinned to the road");
  coupe.IsInAir=false;for(int i=0;i<10;i++){Game.GameTime+=100;reflex.Update(guess);}
  Check(reflex.Blend>0.99f,"Back on the wheels it blends in again");
  int forcesBefore=coupe.Forces;reflex.Deactivate(guess);
  Check(Math.Abs(coupe.HandlingData.TractionCurveMax-max0)<1e-4&&reflex.Blend>0.99f,"Deactivation drops the shared grip lift at once but not the press");
  int settleFrames=0;while(reflex.Settle(guess)){Game.GameTime+=100;settleFrames++;if(settleFrames>50)break;}
  Check(settleFrames>=4&&settleFrames<=7&&coupe.Forces>forcesBefore&&reflex.Blend==0f&&reflex.Vehicle==null,"The press winds down over about half a second after the meter ends");
  Check(!reflex.Settle(guess),"Once settled there is nothing more to do");
  // vehicle change mid-ability
  coupe.CanTiresBurst=true;reflex.Activate(guess);reflex.Update(guess);var other=Car(VehicleClass.Sedans,"primo");other.CanTiresBurst=true;float otherMax=other.HandlingData.TractionCurveMax;guess.SetIntoVehicle(other,VehicleSeat.Driver);reflex.Update(guess);
  Check(coupe.CanTiresBurst&&Math.Abs(coupe.HandlingData.TractionCurveMax-max0)<1e-4&&reflex.Vehicle==other&&!other.CanTiresBurst,"Changing cars restores the old one and acquires the new one");
  reflex.Deactivate(guess);while(reflex.Settle(guess))Game.GameTime+=100;Check(other.CanTiresBurst&&Math.Abs(other.HandlingData.TractionCurveMax-otherMax)<1e-4,"The new car is restored too");
  // passenger and aircraft: nothing
  var heli=new Vehicle{Model=new Model("supervolito")};guess.SetIntoVehicle(heli,VehicleSeat.Driver);reflex.Activate(guess);reflex.Update(guess);
  Check(reflex.Vehicle==null&&heli.Forces==0,"A helicopter gets no road overlay");reflex.Deactivate(guess);
  var ride=Car(VehicleClass.Sedans);var ice=crew.PedFor(CrewSlot.Ice);ice.SetIntoVehicle(ride,VehicleSeat.Driver);guess.SetIntoVehicle(ride,VehicleSeat.RightFront);reflex.Activate(guess);reflex.Update(guess);
  Check(reflex.Vehicle==null&&ride.Forces==0,"A passenger gets no overlay");reflex.Deactivate(guess);

  // ---- B3. Helicopters fly stock (Ron, September 10); the adapter report for planes.
  Check(!System.IO.File.ReadAllText(System.IO.Path.Combine(Repo,"src","Bloodlines","Core","WorldTuning.cs")).Contains("HeliCruise"),"The helicopter cruise assist is gone");
  Reset();crew=Roster();tuning=new WorldTuning();var buzzard=new Vehicle{Model=new Model("supervolito"),DisplayName="buzzard",IsInAir=true,IsEngineRunning=true,Velocity=new Vector3(0,50,0),ForwardVector=new Vector3(0,1,0)};World.Vehicles.Add(buzzard);
  Game.LastFrameTime=.1f;tuning.Update(crew);for(int i=0;i<60;i++){Game.GameTime+=100;tuning.Update(crew);}
  Check(buzzard.Forces==0&&!GTA.Native.Function.Calls.Any(c=>c.Item1==GTA.Native.Hash.SET_VEHICLE_MAX_SPEED&&c.Item2[0]==buzzard),"An airborne helicopter in forward flight is left stock: no push, no ceiling");
  buzzard.IsInAir=false;buzzard.Forces=0;for(int i=0;i<5;i++){Game.GameTime+=100;tuning.Update(crew);}
  Check(buzzard.Forces==0,"On the ground the helicopter gets no push");
  var adapter=new TravelHandling();adapter.Apply(new HandlingData(),new Model("supervolito"){IsHelicopter=false,IsPlane=true});
  Check(adapter.Report.StartsWith("flight handling:")&&(adapter.Report.Contains("->")||adapter.Report.Contains("unsupported")),"The adapter says what it found and applied instead of staying silent");
  var noFlight=new HandlingData{FlyingHandlingData=null};var silent=new TravelHandling();silent.Apply(noFlight,new Model("supervolito"){IsHelicopter=false,IsPlane=true});
  Check(silent.Report.Contains("unsupported on this runtime"),"A runtime without the flight handling type is reported as unsupported");

  // ---- A1. Briefing cast: no crew deployed -> temporary heroes, story ped hidden, no radio framing.
  Reset();crew=Roster();var c=Context(crew);crew.IsDeployed=false;crew.Peds.Clear();crew.ActivePed=null;var story=Game.Player.Character;World.Created.Clear();story.ForwardVector=new Vector3(0,1,0);story.Position=new Vector3(120,80,12);World.Vehicles.Clear();
  Check(c.Cutscenes.Play("M03","intro","Cypress"),"M03's briefing plays with no crew deployed");
  Check(World.Created.Count==2&&!story.IsVisible&&World.Created.Any(p=>p.Position.DistanceTo(story.Position)<3f)&&World.Created.Any(p=>p.CurrentVehicle!=null),"Guess is staged at the start point in place of the hidden story character and Gohan is in the arriving car");
  var arriving=World.Vehicles.Last();arriving.Position=story.Position;arriving.Speed=0f;c.Cutscenes.Update();c.Cutscenes.Update();c.Cutscenes.Update();
  Check(GTA.UI.Screen.Subtitle.Contains("GUESS"),"The first line is framed on a real actor once the car has pulled up");
  var radio=typeof(CutsceneDirector).GetField("_radioScene",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
  Check(!(bool)radio.GetValue(c.Cutscenes),"The scene is not a radio call");
  c.Cutscenes.Stop();Check(story.IsVisible&&World.Created.All(p=>!p.Present),"The story character is visible again and the temporary cast is gone");
  Reset();crew=Roster();c=Context(crew);World.Created.Clear();c.Cutscenes.Play("M03","intro","Cypress");
  Check(World.Created.Count==0&&Game.Player.Character.IsVisible,"With the crew deployed the real peds act and nothing is staged");c.Cutscenes.Stop();

  // ---- A2. Marker names carry the mission id.
  Reset();crew=Roster();c=Context(crew);var state=CampaignState.Load(Path.Combine(root,"markers-id.json"));var cat=new MissionCatalog();
  cat.All.Add(new MissionDefinition{Info=new MissionInfo{Id="M01",Title="Ghost in the Dockyard"},Factory=()=>new ProbeMission()});
  var book=LocationBook.Load(dataDir,Path.Combine(root,"none.ini"));Game.Player.Character.Position=book.Position("M01.RegroupPoint");
  var markers=new MissionMarkers(cat,state,new MissionManager(c,state,cat),book,dataDir,"J");markers.Update(false);
  Check(World.LastBlip.Name=="M01 — Ghost in the Dockyard","Map icons are named by mission id");markers.Clear();

  // ---- A3. QA: complete the current objective through its own completion.
  Reset();crew=Roster();c=Context(crew);var probe=new AuditMission();probe.First.Throw=false;probe.Begin(c);probe.Tick();
  Use(crew,CrewSlot.Gohan);
  Check(probe.CompleteCurrentObjective()=="Finish the terminal"&&probe.First.IsFinished&&!probe.Second.IsFinished,"The objective the player is on is completed by name, one at a time");
  probe.Tick();Check(probe.Status==MissionStatus.Running,"One finished objective does not end a stage that still has work");
  Check(probe.CompleteCurrentObjective()=="Finish the terminal"&&probe.Second.IsFinished,"The next objective in the stage follows");
  probe.Tick();Check(probe.Status==MissionStatus.Passed,"The stage and mission advance through the normal tick, exit effects and all");
  Check(probe.CompleteCurrentObjective()==null,"A finished mission has nothing to complete");
  var mgr=new MissionManager(c,CampaignState.Load(Path.Combine(root,"qa-complete.json")),new MissionCatalog());
  Check(mgr.CompleteObjective()==null,"With no mission running there is nothing to complete");

  // ---- A4. Diagnostics never throw and describe the state.
  Reset();crew=Roster();c=Context(crew);var homes=new CrewHomes(crew,CampaignState.Load(Path.Combine(root,"diag.json")),c.Locations,new WeaponProgression(CampaignState.Load(Path.Combine(root,"diag2.json"))));
  var seatCar=new Vehicle();Game.Player.Character.SetIntoVehicle(seatCar,VehicleSeat.Driver);
  string line=ControlDiagnostics.Snapshot("test",crew,c.Cutscenes,new MissionHandoff(),homes,"M02");
  Check(line.Contains("CONTROL [test] mission=M02")&&line.Contains("control=True")&&line.Contains("driver=True")&&line.Contains("frozen=False")&&line.Contains("scene=None"),"The snapshot names the player, seat, flags, gate and scene outcome");
  Game.Player.Character=null;Check(ControlDiagnostics.Snapshot("nobody",crew,c.Cutscenes,null,null,null).Contains("player=none"),"A missing player is reported, not thrown");

  // ---- A5. Readout at the top right, not in the subtitle strip.
  string dev=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","DevTools.cs"));
  Check(dev.Contains("Alignment = GTA.UI.Alignment.Right")&&dev.Contains("new System.Drawing.PointF(1270f, 8f)")&&!dev.Contains("ShowSubtitle(\n                string.Format(\"~s~X"),"The coordinate readout is drawn top right with speed, not as a subtitle");
 }
}
