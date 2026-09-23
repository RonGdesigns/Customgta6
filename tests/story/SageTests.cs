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
 static void SageChecks()
 {
  // ---- M23: the survey before anyone moves, the bays looked into, the generator by hand, the limits named, the walk to the door.
  Reset();var crew=Roster();var c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"sage23.json"));c.Vans=new CrewVan(c.State,c.Locations);var m23=new M23GhostInTheSage();var entranceBefore=c.Locations.Position(BunkerSite.EntranceKey);
  Check(m23.Begin(c)&&c.Cutscenes.IsActive&&m23.Granger!=null&&m23.EndpointKind==MissionEndpoint.SafehouseArrival,"M23 opens on the exterior survey with the Granger they came in; the bunker is a safehouse endpoint");
  Check(c.Locations.Position(BunkerSite.EntranceKey)==entranceBefore&&entranceBefore.Z>45f,"Preparing M23 preserves the live hatch surface instead of snapping it below the entrance");
  Check(crew.ActiveSlot==CrewSlot.Guess&&Protagonist.All.All(h=>crew.PedFor(h.Slot).IsInVehicle(m23.Granger))&&m23.Granger.Position.DistanceTo(c.Locations.Position("M23.Entrance"))>90f,"M23 starts with all three in their crew car on the approach road");
  c.Cutscenes.Skip();m23.Tick();Check(m23.CurrentStage==0,"The parked starting car cannot satisfy the approach automatically");
  m23.Granger.Position=c.Locations.Position("M23.Approach");m23.Granger.Speed=0;Game.Player.Character.Position=m23.Granger.Position;m23.Tick();
  Check(m23.CurrentStage==1&&c.Cutscenes.IsActive,"Driving to the approach triggers the site survey before Ice takes point");
  c.Cutscenes.Skip();m23.Tick();Use(crew,CrewSlot.Ice);Game.Player.Character.Task.LeaveVehicle();Game.Player.Character.Position=c.Locations.Position("M23.Entrance");m23.Tick();
  Check(m23.CurrentStage==2&&m23.Roles.For(CrewSlot.Guess).State==RoleState.Covering&&m23.Roles.For(CrewSlot.Gohan).State==RoleState.Covering,"On the approach the other two take cover; nobody teleports into the yard");
  Check(World.Created.Where(p=>p.Model.Name.StartsWith("g_m_y_mex")&&p.IsAlive).All(p=>p.Task.Fights>0),"Every surviving squatter receives a direct combat target when Ice breaches");
  foreach(var p in World.Created.Where(p=>p.Model.Name.StartsWith("g_m_y_mex")))p.IsDead=true;m23.Tick();Check(m23.CurrentStage==3,"The yard clear, the bays are Ron's");
  foreach(var key in new[]{"M23.ToolBay","M23.FuelBay","M23.VehicleBay"})Interact(m23,c,CrewSlot.Guess,c.Locations.Position(key),5);
  Check(m23.Limits.Count==2&&m23.Limits.Contains("no tools")&&m23.Limits.Contains("no fuel reserve"),"Each bay checked says what is there and adds what is missing to the list");
  m23.Tick();m23.Tick();Check(m23.CurrentStage==4,"The bays checked, the generator is Gohan's");
  GTA.UI.Screen.Subtitle=null;Interact(m23,c,CrewSlot.Gohan,c.Locations.Position("M23.PowerPanel"),10);
  Check(m23.CurrentStage==5&&m23.Powered&&c.Cutscenes.IsActive&&m23.LimitsShown&&m23.Limits.Count==4&&c.State.FleetUpgrades["bunkerGenerator"],"The generator started by hand plays as a scene and the four limits are named as the next jobs");
  c.Cutscenes.Skip();m23.Tick();c.Dialogue.Clear();World.CollisionReady=true;
  Game.Player.Character.Position=c.Locations.Position(BunkerSite.EntranceKey);Game.Accept=true;m23.Tick();
  Check(m23.Interior.Busy&&m23.CurrentStage==5,"M23 waits for the real room rather than passing at an exterior gate");
  Game.GameTime+=300;m23.Tick();m23.Tick();m23.Tick();
  Check(m23.Interior.Inside&&m23.CurrentStage==6,"The loaded room is a required playable stage");
  var arrivalInside=Game.Player.Character.Position;
  Check(arrivalInside.DistanceTo(c.Locations.Position(BunkerSite.InspectKey))<1f,"Bunker inspection is reachable on the actual loaded entry floor, without a test teleport across partitions");
  Game.Accept=false;Game.GameTime+=5000;c.Dialogue.Clear();m23.Tick();
  Check(m23.CurrentStage==6,"Entering the bunker does not automatically inspect it; Gohan must press the interaction button");
  Game.Accept=true;m23.Tick();Game.Accept=false;Game.GameTime+=2000;m23.Tick();
  Check(m23.CurrentStage==6,"Bunker inspection takes time after the explicit button press");
  Game.GameTime+=2500;m23.Tick();
  Check(Game.Player.Character.Position==arrivalInside,"Inspection completes from the arrival room without moving or teleporting the player");
  Check(m23.CurrentStage==7,"Inspecting the bunker asks Gohan to return outside");
  Check(Game.Player.Character.Position.DistanceTo(c.Locations.Position(BunkerSite.DoorKey))<2.5f,"The return marker is reachable in the same entry room after inspection");
  Game.Accept=true;m23.Tick();
  Game.GameTime+=300;m23.Tick();m23.Tick();m23.Tick();c.Dialogue.Clear();m23.Tick();
  for(int finish=0;finish<4&&m23.Status==MissionStatus.Running;finish++){c.Dialogue.Clear();m23.Tick();}
  Check(m23.Status==MissionStatus.Passed&&!m23.Interior.Inside&&Game.Player.Character.Position.DistanceTo(c.Locations.Position(BunkerSite.EntranceKey))<3f,"M23 passes only after a successful return to the actual entrance");
  m23.Cleanup();Check(m23.Granger.Exists(),"The Granger stays at the bunker");

  // ---- M24: the truck and the container seen, the hoist as a lift, the deputies by road, Gohan aboard before it rolls, the crates into bay one, the ledger once.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"sage24.json"));var m24=new M24LiquidGold();
  Check(m24.Begin(c)&&c.Cutscenes.IsActive&&m24.Container!=null&&m24.Container.IsPositionFrozen&&m24.Container.Position==PortHeist.HiddenContainerPoint(c.Locations.Position("M22.AlamoDrop")),"M24 opens on the truck and the container still in the Alamo water");
  var gohanWork=crew.PedFor(CrewSlot.Gohan).Position;Check(gohanWork==c.Locations.Position("M24.GohanWork")&&!crew.PedFor(CrewSlot.Gohan).IsInVehicle(),"Gohan starts at the dry cable controls instead of in the deep cargo water");
  c.Cutscenes.Skip();m24.Tick();Use(crew,CrewSlot.Guess);var dredge=c.Locations.Position("M24.RecoveryPad");Game.Player.Character.SetIntoVehicle(m24.Crane,VehicleSeat.Driver);m24.Crane.Position=dredge;Game.Player.Character.Position=dredge;m24.Tick();
  Check(m24.CurrentStage==1,"The truck parked, the dredge and the ridge run together");
  var flow24=Flow(m24);int guard=0;
  while(m24.CurrentStage==1&&guard++<80)
  {
   foreach(var o in flow24[1].Objectives.Where(o=>!o.IsFinished))
   {
    if(o is AssignedWorkObjective){crew.PedFor(CrewSlot.Guess).Task.LeaveVehicle();crew.PedFor(CrewSlot.Guess).Position=dredge;}
    else if(o is SurviveWavesObjective w){foreach(var ped in w.Spawned)ped.IsDead=true;}
   }
   c.Dialogue.Clear();Game.GameTime+=1000;m24.Tick();
  }
  Check(m24.CurrentStage==2&&m24.Hoisted&&c.Cutscenes.IsActive&&m24.Cruisers.Count==3&&m24.Crates.Count==2,"Two waves come as three cruisers by the ridge road; the hoist plays as a scene with two crates");
  c.Cutscenes.Skip();m24.Tick();Check(m24.Crates.All(cr=>cr.AttachedTo==m24.Crane),"Skipped or watched, both crates are on the bed");
  m24.Tick();Check(crew.PedFor(CrewSlot.Gohan).Task.Enters==1&&c.Dialogue.HasPending&&m24.CurrentStage==2,"Once the scene is over Gohan is called aboard and walks to the truck; the truck waits");
  crew.PedFor(CrewSlot.Gohan).SetIntoVehicle(m24.Crane,VehicleSeat.Passenger);c.Dialogue.Clear();m24.Tick();Check(m24.CurrentStage==3&&m24.Boarded,"Gohan in the cab, the truck may roll");
  Use(crew,CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(m24.Crane,VehicleSeat.Driver);m24.Crane.Position=c.Locations.Position("M23.VehicleBay");Game.Player.Character.Position=m24.Crane.Position;c.Dialogue.Clear();m24.Tick();
  Check(m24.Unloaded&&c.Cutscenes.IsActive,"At the bunker the unloading plays as a scene");
  c.Cutscenes.Skip();m24.Tick();var bay=c.Locations.Position("M23.ToolBay");
  Check(m24.Crates.All(cr=>cr.AttachedTo==null&&cr.IsPositionFrozen&&cr.Position.DistanceTo(bay)<3f)&&c.State.CargoAt("recoveredGold")=="M23.ToolBay","Skipped or watched, the crates stand in bay one and the first portion is recorded there");
  c.Dialogue.Clear();m24.Tick();c.Dialogue.Clear();m24.Tick();Check(m24.Status==MissionStatus.Passed&&c.State.AlamoGoldDredgedTons==0f,"M24 passes; the ledger moves only when completion commits");
  string m24src=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act2","M24LiquidGold.cs"));
  Check(m24src.Contains("StartVehicleMission(cruiser, _ridge + new Vector3(0f, 14f, 0f)")&&m24src.Contains("Bought deputies")&&!m24src.Contains("all police"),"The deputies drive in on a route and are named as bought men, not a police force");

  // ---- M25: the route and the pickup established first, the way out shown before the jump, Ron talking him down, Ice aboard.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"sage25.json"));var m25=new M25BountyHuntersCanyon();
  Check(m25.Begin(c)&&c.Cutscenes.IsActive&&m25.Boat!=null&&crew.PedFor(CrewSlot.Guess).IsInVehicle(m25.Boat)&&m25.Boat.IsEngineRunning&&crew.PedFor(CrewSlot.Gohan).Position==c.Locations.Position("M25.RimPost"),"M25 opens with Ron in the boat under the span and Gohan on the rim: the pickup exists before the jump does");
  Check(Game.Player.Character.Position==c.Locations.Position("M25.DeckApproach"),"Ice starts at the north end of the deck and walks it");
  c.Cutscenes.Skip();m25.Tick();Game.Player.Character.Position=c.Locations.Position("M25.BridgeDeck");m25.Tick();Check(m25.CurrentStage==1,"On the deck, the tanker is the job");
  m25.Tanker.IsDriveable=false;c.Dialogue.Clear();m25.Tick();Check(m25.CurrentStage==2,"The pass sealed, the waves come");
  var flow25=Flow(m25);guard=0;
  while(m25.CurrentStage==2&&guard++<80){foreach(var o in flow25[2].Objectives.OfType<SurviveWavesObjective>())foreach(var ped in o.Spawned)ped.IsDead=true;c.Dialogue.Clear();Game.GameTime+=1000;m25.Tick();}
  Check(m25.CurrentStage==3&&m25.EscapeShown&&c.Cutscenes.IsActive,"The waves held, the way out is shown as a scene before the jump is asked for");
  c.Cutscenes.Skip();m25.Tick();m25.Tick();Check(m25.TalkedDown&&c.Dialogue.HasPending,"Ron talks him down over the radio once the scene is over");
  Game.Player.Character.Position=c.Locations.Position("M25.Riverbed");c.Dialogue.Clear();m25.Tick();Check(m25.CurrentStage==4,"Down at the water, the boat is the job");
  Game.Player.Character.SetIntoVehicle(m25.Boat,VehicleSeat.Passenger);m25.Tick();c.Dialogue.Clear();m25.Tick();
  Check(m25.Status==MissionStatus.Running&&m25.Departing,"Boarding alone no longer passes M25");
  m25.Boat.Position=m25.DepartureOrigin+new Vector3(101,0,0);c.Dialogue.Clear();m25.Tick();c.Dialogue.Clear();m25.Tick();
  Check(m25.Status==MissionStatus.Passed,"Ice and Guess actually leave the pickup before M25 passes");

  // ---- M26: the spotters, the parked Lazer with its history, Gohan at the laptop; the lead held by listening; the Lazer parked beside the Duster.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"sage26.json"));c.Vans=new CrewVan(c.State,c.Locations);var m26=new M26AlamoScramble();
  Check(m26.Begin(c)&&c.Cutscenes.IsActive&&m26.Lazer!=null&&!m26.Lazer.IsEngineRunning&&m26.ApproachPlane!=null&&m26.ApproachPlane.Model.Name=="vestra"&&m26.Granger!=null,"M26 opens on the parked Lazer, the Vestra beside it and Gohan's Granger");
  // The spotters were spawned in the air and then asked to board by a task the next
  // line replaced, so pilot and plane both fell. They are seated outright now.
  Check(m26.Spotters.Count==2&&m26.Pilots.Count==2&&m26.Pilots.All(p=>p.IsInVehicle())&&
        m26.Spotters.All(v=>v.GetPedOnSeat(VehicleSeat.Driver)!=null),"Both spotter planes actually have a pilot in the driver seat");
  // The interceptor is parked cold so the scramble means something, but it has to
  // start when he is in it: Ron could fire its guns and never accelerate.
  c.Cutscenes.Skip();m26.Tick();Use(crew,CrewSlot.Guess);
  Check(!m26.Lazer.IsEngineRunning,"The interceptor sits cold while nobody is in it");
  Game.Player.Character.SetIntoVehicle(m26.Lazer,VehicleSeat.Driver);m26.Tick();
  Check(m26.Lazer.IsEngineRunning,"It starts the moment Ron is aboard");
  m26.Tick();Check(m26.CurrentStage==1,"Airborne, the first spotter is the job");
  m26.Spotters[0].IsDriveable=false;c.Dialogue.Clear();m26.Tick();Check(m26.CurrentStage==2&&m26.Listening&&c.Dialogue.HasPending,"The first spotter down, Gohan asks for the second one held while he listens");
  c.Dialogue.Clear();m26.Tick();Check(m26.CurrentStage==2&&!m26.LeadHeld,"Too early: the call sign is not in yet");
  // The wait is flying now. Time alone does nothing: Guess has to be on the second
  // spotter's wing, because Ron found rolling up and doing nothing unsatisfying and the
  // story still needs the transmission.
  Game.Player.Character.Position=m26.Spotters[1].Position+new Vector3(2000,0,0);
  for(int i=0;i<20;i++){Game.GameTime+=1000;c.Dialogue.Clear();m26.Tick();}
  Check(m26.CurrentStage==2&&!m26.LeadHeld,"Out of range the clock does not run, however long he waits");
  Game.Player.Character.Position=m26.Spotters[1].Position+new Vector3(20,0,0);
  for(int i=0;i<20&&m26.CurrentStage==2;i++){Game.GameTime+=1000;c.Dialogue.Clear();m26.Tick();}
  Check(m26.CurrentStage==3&&m26.LeadHeld&&c.State.EvidenceOf("charterCallSign")==EvidenceState.CopyHeld,"On his wing long enough, the charter's call sign is held as evidence");
  m26.Spotters[1].IsDriveable=false;c.Dialogue.Clear();m26.Tick();Check(m26.CurrentStage==4,"The second spotter down, home is the job");
  m26.Lazer.Position=c.Locations.Position("M26.RunwayStart");m26.Lazer.HeightAboveGround=0f;m26.Lazer.Speed=0f;Game.Player.Character.Position=m26.Lazer.Position;c.Dialogue.Clear();m26.Tick();
  Check(m26.Parked&&c.Cutscenes.IsActive&&c.State.CargoAt("lazer")=="M26.DusterPad","Landed, the Lazer is parked as a scene beside the Duster and recorded at McKenzie");
  c.Cutscenes.Skip();m26.Tick();c.Dialogue.Clear();m26.Tick();c.Dialogue.Clear();m26.Tick();Check(m26.Status==MissionStatus.Passed,"M26 passes");
  m26.Cleanup();Check(m26.Lazer.Exists()&&m26.ApproachPlane.Exists(),"Both aircraft stay on the apron for M27");
  // Killing the second spotter during the listen loses the lead.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"sage26b.json"));var early=new M26AlamoScramble();early.Begin(c);c.Cutscenes.Skip();early.Tick();Use(crew,CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(early.Lazer,VehicleSeat.Driver);early.Tick();
  early.Spotters[0].IsDriveable=false;c.Dialogue.Clear();early.Tick();Function.TestDamage.Add(early.Spotters[1].Handle);early.Spotters[1].IsDriveable=false;c.Dialogue.Clear();early.Tick();early.Tick();
  Check(early.Status==MissionStatus.Failed&&early.FailReason.Contains("call sign"),"Splashing the second spotter before the call sign is in fails the job with the reason");

  // ---- M27: Ice in the Duster's second seat beside the parked Lazer, the transfer as a cut, the ledger in hand, Ron home on his own route, the boat.
  Reset();crew=Roster();c=Context(crew);World.NearbyVehicles=new[]{m26.Lazer,m26.ApproachPlane};c.State=CampaignState.Load(Path.Combine(root,"sage27.json"));c.State.SetCargo("lazer","M26.DusterPad");var m27=new M27FlightRisk();
  Check(m27.Begin(c)&&c.Cutscenes.IsActive&&m27.ApproachPlane==m26.ApproachPlane&&m27.Lazer==m26.Lazer&&!m27.Lazer.IsEngineRunning&&m27.EndpointKind==MissionEndpoint.EscapeCheckpoint,"M27 opens on the same Duster and the same parked Lazer");
  Check(crew.PedFor(CrewSlot.Gohan).IsInVehicle(m27.Dinghy)&&m27.Dinghy.IsEngineRunning,"Gohan is already at sea in the boat");
  c.Cutscenes.Skip();m27.Tick();Check(crew.PedFor(CrewSlot.Ice).IsInVehicle(m27.ApproachPlane)&&crew.PedFor(CrewSlot.Ice).SeatIndex==VehicleSeat.Passenger,"Skipped or watched, Ice is in the Duster's second seat");
  Use(crew,CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(m27.ApproachPlane,VehicleSeat.Driver);m27.Tick();Check(m27.CurrentStage==1,"Ron flying, the match is the job");
  guard=0;while(m27.CurrentStage==1&&guard++<40){Game.Player.Character.Position=m27.Shamal.Position+new Vector3(30,0,0);m27.ApproachPlane.Position=Game.Player.Character.Position;c.Dialogue.Clear();Game.GameTime+=1000;m27.Tick();}
  Check(m27.CurrentStage==2&&m27.Transferred&&c.Cutscenes.IsActive,"Holding station, the transfer plays as a staged cut");
  c.Cutscenes.Skip();m27.Tick();
  Check(Game.Player.Character==crew.PedFor(CrewSlot.Ice)&&crew.PedFor(CrewSlot.Ice).IsInVehicle(m27.Shamal)&&!GameUtils.Faded,"Skipped or watched, Ice is the player in a real Shamal seat and the fade is lifted");
  GTA.UI.Screen.Subtitle=null;Interact(m27,c,CrewSlot.Ice,m27.Shamal.Position,4,afloat:true);
  Check(m27.CurrentStage==3&&m27.LedgerTaken&&m27.Ledger!=null&&m27.Ledger.AttachedTo==crew.PedFor(CrewSlot.Ice)&&c.State.EvidenceOf("flightLedger")==EvidenceState.CopyHeld,"The ledger is a case in Ice's hand and evidence held");
  Check(m27.RonReturned&&crew.PedFor(CrewSlot.Guess).IsInVehicle(m27.ApproachPlane)&&crew.PedFor(CrewSlot.Guess).Task.HeliTasks==0,"Ron turns for home in the Duster on his own route");
  Game.Player.Character.Task.LeaveVehicle();Game.Player.Character.CurrentVehicle=null;c.Dialogue.Clear();m27.Tick();Check(m27.CurrentStage==4&&!m27.Ledger.IsVisible,"Out of the jet, the case rides hidden under the canopy");
  Game.Player.Character.Position=m27.Dinghy.Position;Game.Player.Character.SetIntoVehicle(m27.Dinghy,VehicleSeat.Passenger);c.Dialogue.Clear();m27.Tick();
  Check(m27.Aboard&&m27.Ledger.IsVisible&&m27.Ledger.AttachedTo==m27.Dinghy,"In the boat, the ledger is stowed where Gohan can see it");
  // Reaching Gohan used to be the end of it, which left the boat in open water with
  // nowhere to go. The ledger goes ashore and up the coast to the Grapeseed depot.
  var m27shore=c.Locations.Position("M27.Shore");var m27land=c.Locations.Position("M27.Landing");
  // Gohan is at the wheel, so the run ashore is his leg. Asking Ice to drive forced a
  // switch back to the passenger the moment Ron took the man actually steering.
  Use(crew,CrewSlot.Gohan);
  m27.Dinghy.Position=m27shore;m27.Dinghy.Speed=0;Game.Player.Character.Position=m27shore;c.Dialogue.Clear();m27.Tick();
  Check(m27.Ashore&&m27.RoadCar!=null&&m27.RoadCar.Exists(),"In under the lighthouse, with a vehicle waiting on the headland");
  Check(m27.Ledger.AttachedTo==m27.RoadCar,"and the ledger moves out of the boat and into it");
  var m27depot=c.Locations.Position("M27.Depot");
  // The ledger is Ice's, so the road leg is his.
  Use(crew,CrewSlot.Ice);
  Game.Player.Character.Task.LeaveVehicle();Game.Player.Character.SetIntoVehicle(m27.RoadCar,VehicleSeat.Driver);
  m27.RoadCar.Position=m27depot;m27.RoadCar.Speed=0;Game.Player.Character.Position=m27depot;c.Dialogue.Clear();m27.Tick();
  Check(m27.Delivered,"and the ledger reaches the depot shed at Grapeseed");
  c.Dialogue.Clear();m27.Tick();c.Dialogue.Clear();m27.Tick();Check(m27.Status==MissionStatus.Passed,"M27 passes");World.NearbyVehicles=new Vehicle[0];
  // The pickup has to sit under the flight path. It used to be 5.6 km away at the other
  // end of the north coast, which is why Ron could not glide to it.
  Check(c.Locations.Position("M27.SeaPickup").DistanceTo2D(c.Locations.Position("M27.Shore")) < 400f &&
        c.Locations.Position("M27.Shore").DistanceTo2D(m27land) < 80f,
        "The boat and the beach are the same piece of coast");
  Check(c.Locations.Position("M27.Depot").DistanceTo2D(c.Locations.Position("M27.Landing")) < 2200f,
        "and the drive to the depot is a short run, not a trip across the map");
  string m27src=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act2","M27FlightRisk.cs"));
  // A Duster tops out at 69 and the Shamal at 91, so the old approach aircraft could
  // never hold station. The Vestra has two seats and beats the jet at 97.
  Check(!m27src.Contains("new Model(\"stunt\")")&&!m27src.Contains("new Model(\"duster\")")&&m27src.Contains("new Model(\"vestra\")")&&!m27src.Contains("Script.Wait("),"The approach aircraft has two seats, outruns the target and does not block the script thread");
  Check(m27src.Contains("SetIntoVehicle(_shamal, VehicleSeat.Driver)")&&!m27src.Contains("WarpIntoVehicle(_shamal"),"The Shamal pilot is seated outright, so the jet is actually flown and its marker tracks it");
  var scenes=File.ReadAllLines(Path.Combine(dataDir,"scenes.tsv"));
  Check(new[]{"M23","M24","M25","M26","M27"}.All(id=>scenes.Count(l=>l.StartsWith(id+"_SCENE_APPROACH"))==2),"Each desert chapter's approach has its authored lines");
 }
}
