using System;
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
 static void RelayChecks()
 {
  // ---- M07: the relay seen, the sniffer clamped, the manifests read, the helicopter shown.
  Reset();var crew=Roster();var c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"relay7.json"));GTA.Native.Function.Seabed=55f;var m7=new M07WiretapWaltz();
  Check(m7.Begin(c)&&c.Cutscenes.IsActive&&m7.Sedan!=null&&crew.PedFor(CrewSlot.Guess).IsInVehicle(m7.Sedan),"M07 opens on the relay, the roof and the pickup lane with Ron already in the lane");
  Check(!crew.PedFor(CrewSlot.Gohan).IsInVehicle()&&crew.PedFor(CrewSlot.Gohan).Position==m7.BuildingBase&&m7.Laptop!=null&&m7.BuildingBase.DistanceTo(c.Locations.Position("M07.GarageRoof"))<60f&&Game.Player.Character.Position.DistanceTo(m7.Roof)<10f,"Ice starts on the roof; Gohan reads the feed at the building's base on the street with a laptop; Ron is in the lane");
  string m7pre=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act1","M07WiretapWaltz.cs"));
  Check(m7pre.Contains("VehicleMissionType.Attack")&&!m7pre.Contains("ChaseWithHelicopter")&&m7pre.Contains("Survey the roof (F11)"),"The helicopter attacks the roof instead of shadowing it, and a missing roof is reported with the survey key");
  c.Cutscenes.Skip();m7.Tick();Check(m7.CurrentStage==0&&!c.Cutscenes.IsActive,"Skipping the approach leaves Ice on the roof at stage one");
  Use(crew,CrewSlot.Ice);Game.Player.Character.Position=c.Locations.Position("M07.MastTop");m7.Tick();Check(m7.CurrentStage==1,"Reaching the mast opens the clamp");
  GTA.UI.Screen.Subtitle=null;Interact(m7,c,CrewSlot.Ice,c.Locations.Position("M07.MastTop"),7);
  Check(m7.CurrentStage==2&&c.Cutscenes.IsActive&&m7.Sniffer!=null&&m7.Sniffer.Exists()&&m7.Sniffer.IsPositionFrozen,"The clamp plays as a scene with a real sniffer left on the dish");
  Check(c.State.EvidenceOf("aegisManifests")==EvidenceState.CopyHeld&&m7.Drone!=null&&m7.Drone.Exists(),"The manifests are recorded as a copy held and the helicopter is already in the air");
  c.Cutscenes.Skip();Check(m7.Sniffer.Exists()&&!m7.HeliShown,"Skipping the clamp leaves the sniffer where it is; the helicopter has not been shown yet");
  m7.Tick();Check(m7.HeliShown&&c.Cutscenes.IsActive,"The helicopter is shown coming, once, before the roof is Ice's problem");
  c.Cutscenes.Skip();m7.Tick();Check(!c.Cutscenes.IsActive&&m7.CurrentStage==2&&c.Dialogue.HasPending,"After the moment Ron's radio line is on its way and control is back on the roof");
  Game.Player.Character.Position=c.Locations.Position("M07.LandingZone");m7.Tick();Check(m7.CurrentStage==3,"Reaching the pickup on the ground opens the boarding");
  Game.Player.Character.SetIntoVehicle(m7.Sedan,VehicleSeat.LeftRear);m7.Tick();Check(m7.CurrentStage==4&&m7.Status==MissionStatus.Running,"Boarding behind Guess leaves the radio debrief to finish");c.Dialogue.Clear();m7.Tick();Check(m7.Status==MissionStatus.Passed&&m7.OutroBlocking()!=null,"Boarding behind Guess passes M07 with an aftermath shot on the sedan");
  string m7src=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act1","M07WiretapWaltz.cs"));
  Check(m7src.Contains("Phase = \"approach\"")&&m7src.Contains("Phase = \"clamp\"")&&m7src.Contains("PlayMoment(Id, \"Aegis helicopter\"")&&!m7src.Contains("_drone.Delete"),"M07 has the approach, the clamp and the helicopter moment; the dish is never destroyed");
  Check(!m7.RoofLowerThanEstimate&&Math.Abs(m7.Roof.Z-55.1f)<0.01f,"The roof is the surface the world reported, not the estimate handed back");
  // The roof is a surface the world has over the street, never the estimate certified against its own height.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"relay7b.json"));GTA.Native.Function.SeabedKnown=false;var m7b=new M07WiretapWaltz();
  Check(m7b.Begin(c)&&!m7b.RoofFound&&Math.Abs(m7b.Roof.Z-15f)<0.2f&&Math.Abs(Game.Player.Character.Position.Z-15f)<0.2f,"No surface at or near the roof key: M07 still starts, Ice at street level, never in the air");
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"relay7b.json"));GTA.Native.Function.Seabed=16f;m7b=new M07WiretapWaltz();
  Check(m7b.Begin(c)&&!m7b.RoofFound&&Math.Abs(m7b.Roof.Z-15f)<0.2f,"Only the street under and around the roof key: M07 starts at street level instead of certifying the estimate");
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"relay7b.json"));GTA.Native.Function.Seabed=30f;m7b=new M07WiretapWaltz();
  Check(m7b.Begin(c)&&m7b.RoofFound&&m7b.RoofLowerThanEstimate&&Math.Abs(m7b.Roof.Z-30.1f)<0.01f&&Math.Abs(Game.Player.Character.Position.Z-30.1f)<0.01f,"A real roof lower than the estimate is used as found, Ice on it, and the key is reported for a survey");
  GTA.Native.Function.Seabed=-40f;

  // ---- M08: the job seen, the loop window, the forklift, two crates counted, the technical shown, the stash recorded.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"relay8.json"));var m8=new M08SupplyAndSever();
  Check(m8.Begin(c)&&c.Cutscenes.IsActive&&m8.Crates.Count==2&&m8.Forklift!=null&&m8.Hauler!=null&&m8.Granger!=null,"M08 opens on the crates, the flatbed, the camera room and the gate, with two real crates, a forklift and the crew's Granger");
  c.Cutscenes.Skip();m8.Tick();Check(m8.CurrentStage==0,"Skipping the approach leaves Gohan at the gate");
  Interact(m8,c,CrewSlot.Gohan,c.Locations.Position("M08.CameraRoom"),5);Check(m8.CurrentStage==1&&m8.LoopRunning,"The camera loop opens a stated window");
  var sentries=World.Created.Where(p=>p.Model.Name=="s_m_m_security_01").ToList();Check(sentries.Count==5,"Five sentries stand the gate");
  Check(sentries.Count==5&&sentries.Select(s=>s.Position).Distinct().Count()==5&&sentries.Min(s=>sentries.Where(o=>o!=s).Min(o=>o.Position.DistanceTo(s.Position)))>6f,"Five sentries at five posts, none within six meters of another");
  Use(crew,CrewSlot.Ice);sentries[0].IsDead=true;m8.Tick();m8.Tick();Check(sentries.Skip(1).All(s=>s.Task.HatedFights==1),"The first sentry down wakes the rest to fight");
  foreach(var s in sentries)s.IsDead=true;m8.Tick();Check(m8.CurrentStage==2,"The sentries down, Guess is sent to the forklift");
  Use(crew,CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(m8.Forklift,VehicleSeat.Driver);m8.Tick();Check(m8.CurrentStage==3,"In the forklift, the first crate is the job");
  GTA.UI.Screen.Subtitle=null;Interact(m8,c,CrewSlot.Guess,c.Locations.Position("M08.CratePadOne"),3,true);
  Check(m8.CurrentStage==4&&c.Cutscenes.IsActive&&m8.Loaded==1&&m8.Crates[0].AttachedTo==m8.Forklift,"The first crate is on the forks and its loading plays as a scene; the technical is on its way");
  Check(Flow(m8)[3].Objectives[0].GetType().Name=="ForksUnderCrateObjective"&&m8.Crates[1].IsPositionFrozen&&!m8.Crates[0].IsPositionFrozen,"The forks under the crate is a stop at the pad, no button; a crate is static until the forks take it");
  Check(m8.Technical!=null&&m8.Technical.Exists()&&!m8.TechnicalShown,"The technical exists before it is shown");
  c.Cutscenes.Skip();Check(m8.Crates[0].AttachedTo==m8.Hauler,"Skipping the loading lands crate one on the bed");
  m8.Tick();Check(m8.TechnicalShown&&c.Cutscenes.IsActive,"The technical is shown coming up the ramp, once");
  c.Cutscenes.Skip();m8.Tick();Check(m8.CurrentStage==4&&!c.Cutscenes.IsActive,"After the moment the stage is Ice's fight and Ron's second crate, in either order");
  var mixed=Flow(m8)[4].Objectives;Check(mixed[0].GetType().Name=="ForksUnderCrateObjective"&&mixed[0].RequiredCharacter==CrewSlot.Guess&&mixed[1].GetType().Name=="DestroyVehicleObjective"&&!mixed[1].RequiredCharacter.HasValue&&mixed[1].KeepsOwnerOpen,"The forks are Ron's and the technical is under Ice's name for anyone's kill: the stage no longer hands both jobs to Ron");
  Check(m8.CurrentObjective.StartsWith("Guess")&&m8.RequiredSwitch==null,"With Ron active the HUD line is his crate");
  Use(crew,CrewSlot.Ice);m8.Tick();Check(m8.CurrentObjective.StartsWith("Ice")&&m8.RequiredSwitch==null,"With Ice active the HUD line is his fight and no switch is demanded");Use(crew,CrewSlot.Guess);m8.Tick();
  Interact(m8,c,CrewSlot.Guess,c.Locations.Position("M08.CratePadTwo"),3,true);
  Check(m8.CurrentStage==4&&m8.Loaded==2&&c.Cutscenes.IsActive&&m8.Crates[1].AttachedTo==m8.Forklift,"Crate two loads while the technical is still up");
  c.Cutscenes.Skip();Check(m8.Crates[1].AttachedTo==m8.Hauler,"Skipping lands crate two on the bed: both counted");
  m8.Technical.IsDead=true;m8.Tick();Check(m8.CurrentStage==5&&c.Dialogue.HasPending,"The technical down, Ice says so and the flatbed is the job");
  Game.Player.Character.SetIntoVehicle(m8.Hauler,VehicleSeat.Driver);m8.Tick();
  Check(crew.PedFor(CrewSlot.Ice).Task.Enters==1&&crew.PedFor(CrewSlot.Ice).Task.LastSeat==VehicleSeat.RightFront&&crew.PedFor(CrewSlot.Gohan).Task.Enters==1&&crew.PedFor(CrewSlot.Gohan).Task.LastSeat==VehicleSeat.Driver,"Ice boards the flatbed's other seat; Gohan leaves his panel for the Granger");
  m8.Tick();Check(m8.CurrentStage>=6,"With Ron in the flatbed the run to the stash begins");
  Game.Player.WantedLevel=0;m8.Tick();m8.Hauler.Position=c.Locations.Position("M08.Connector");Game.Player.Character.Position=m8.Hauler.Position;m8.Tick();c.Dialogue.Clear();m8.Tick();
  Check(m8.Status==MissionStatus.Passed&&m8.Stashed&&c.State.CargoAt("turbineEngines")=="M08.Connector"&&m8.Hauler.LockStatus==VehicleLockStatus.CannotEnter,"Delivery records the engines at the stash and locks the flatbed there");
  m8.Cleanup();Check(m8.Hauler.Exists()&&m8.Crates.Count==0&&World.Props.Any(p=>p.Exists()&&p.AttachedTo==m8.Hauler),"The stashed flatbed and its crates survive teardown for M09 and M10");
  var reloaded=CampaignState.Load(Path.Combine(root,"relay8.json"));Check(reloaded.CargoAt("turbineEngines")=="M08.Connector","The stash is save data");
  // The loop runs out.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"relay8b.json"));m8=new M08SupplyAndSever();m8.Begin(c);c.Cutscenes.Skip();m8.Tick();
  Interact(m8,c,CrewSlot.Gohan,c.Locations.Position("M08.CameraRoom"),5);Game.GameTime+=M08SupplyAndSever.LoopSeconds*1000+1;m8.Tick();
  Check(m8.Status==MissionStatus.Failed,"The camera loop is a window: when it drops with crates on the ground the job is lost");
  // Ice first: the technical down before the second crate, then Ron's forks finish the stage with no switch demanded.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"relay8c.json"));m8=new M08SupplyAndSever();m8.Begin(c);c.Cutscenes.Skip();m8.Tick();
  Interact(m8,c,CrewSlot.Gohan,c.Locations.Position("M08.CameraRoom"),5);
  Use(crew,CrewSlot.Ice);foreach(var s in World.Created.Where(p=>p.Model.Name=="s_m_m_security_01"))s.IsDead=true;m8.Tick();
  Use(crew,CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(m8.Forklift,VehicleSeat.Driver);m8.Tick();
  Interact(m8,c,CrewSlot.Guess,c.Locations.Position("M08.CratePadOne"),3,true);c.Cutscenes.Skip();m8.Tick();c.Cutscenes.Skip();m8.Tick();
  Check(m8.CurrentStage==4&&!c.Cutscenes.IsActive&&m8.Loaded==1,"The second run reaches the mixed stage");
  Use(crew,CrewSlot.Ice);m8.Technical.IsDead=true;m8.Tick();
  Check(m8.CurrentStage==4&&Flow(m8)[4].Objectives[1].IsFinished,"Ice puts the technical down first: his job is done and the stage waits for Ron's crate");
  Check(m8.RequiredSwitch==CrewSlot.Guess&&m8.CurrentObjective.Contains("second crate"),"With Ice's job done the HUD points to Ron's crate, the one job left");
  Interact(m8,c,CrewSlot.Guess,c.Locations.Position("M08.CratePadTwo"),3,true);if(c.Cutscenes.IsActive)c.Cutscenes.Skip();m8.Tick();
  Check(m8.Loaded==2&&m8.CurrentStage==5,"Ron's second crate then finishes the stage: both orders work");
  string m8src=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act1","M08SupplyAndSever.cs"));
  Check(m8src.Contains("Phase = \"approach\"")&&m8src.Contains("Phase = \"loading\"")&&m8src.Contains("PlayMoment(Id, \"Aegis technical\"")&&m8src.Contains("MissionEndpoint.SecuredDelivery")&&!m8src.Contains("Game.Player.WantedLevel = 0"),"M08 has the approach, the counted loading, the technical moment and a secured delivery that clears nothing by itself");
  var beats=File.ReadAllText(Path.Combine(dataDir,"story_beats.txt"));var scenes=File.ReadAllLines(Path.Combine(dataDir,"scenes.tsv"));
  Check(scenes.Count(l=>l.StartsWith("M07_SCENE_APPROACH"))==2&&scenes.Count(l=>l.StartsWith("M08_SCENE_APPROACH"))==2,"Both approaches have two authored lines compiled into the scene data");
 }
}
