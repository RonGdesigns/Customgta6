using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using GTA;
using GTA.Math;
using GTA.Native;
public static partial class StoryTests
{
 static void MarineDrive(Mission m,MissionContext c,Vehicle v,string key,CrewSlot slot)
 {
  Use(c.Crew,slot);Game.Player.Character.SetIntoVehicle(v,VehicleSeat.Driver);v.Position=c.Locations.Position(key);v.Speed=0;v.HeightAboveGround=0;Game.Player.Character.Position=v.Position;DrainPreparation(m,c);
 }
 static void MarinePreparationChecks()
 {
  Reset();World.FailSmoke=false;World.SmokeBursts=0;Function.TestDamage.Clear();var crew=Roster();var c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"marine36.json"));
  var m36=new M36DeepWellRecon();Check(m36.Begin(c),"M36 starts with a real sub, pickup and three seabed sensor props");DrainPreparation(m36,c);
  Check(World.Props.Count(p=>p.Model.Name=="prop_elecbox_12")==3,"All three underwater scan targets are visible physical housings");
  for(int i=1;i<=3;i++){MarineDrive(m36,c,m36.SurveySub,"M36.Scan"+i,CrewSlot.Gohan);Interact(m36,c,CrewSlot.Gohan,c.Locations.Position("M36.Scan"+i),5,true);DrainPreparation(m36,c);}
  Check(m36.Scans==3&&m36.CurrentStage==3&&World.Vehicles.Any(v=>v.Model.Name=="predator"),"The patrol and pickup change happen only after all scans");
  MarineDrive(m36,c,m36.PickupBoat,"M36.AlternatePickup",CrewSlot.Guess);MarineDrive(m36,c,m36.SurveySub,"M36.AlternatePickup",CrewSlot.Gohan);
  Check(m36.Status==MissionStatus.Passed&&c.State.EvidenceOf("offshoreApproachSurvey")==EvidenceState.CopyHeld,"M36 records the map only after the sub returns to the relocated pickup");

  Reset();crew=Roster();c=Context(crew);var m37=new M37TheGrapeseedHarvest();Check(m37.Begin(c),"M37 loads two separate crop dusters and a destination mechanic");DrainPreparation(m37,c);
  MarineDrive(m37,c,m37.Aircraft[0],"M37.Duster1",CrewSlot.Guess);MarineDrive(m37,c,m37.Aircraft[0],"M37.Land1",CrewSlot.Guess);
  Use(crew,CrewSlot.Ice);ClearPreparationEnemies();DrainPreparation(m37,c);MarineDrive(m37,c,m37.Aircraft[1],"M37.Duster2",CrewSlot.Ice);MarineDrive(m37,c,m37.Aircraft[1],"M37.Land2",CrewSlot.Ice);
  Check(m37.CurrentStage==5,"Both specific aircraft must be landed before fitting can begin");
  for(int i=0;i<2;i++){Interact(m37,c,CrewSlot.Gohan,m37.Aircraft[i].Position+m37.Aircraft[i].RightVector*3f,4);DrainPreparation(m37,c);}
  Check(World.Props.Count(p=>p.Model.Name=="prop_barrel_02a"&&p.AttachedTo!=null)==4,"Four physical smoke canisters attach to the two aircraft");
  for(int i=0;i<2;i++){Interact(m37,c,CrewSlot.Gohan,m37.Aircraft[i].Position+m37.Aircraft[i].RightVector*3f,3);DrainPreparation(m37,c);}
  Check(m37.Status==MissionStatus.Passed&&m37.Tested==2&&World.SmokeBursts==2&&m37.Visible==2,"Both releases are operated and both plumes are seen when the particle assets load");
  // A cosmetic particle call that comes back false used to throw the mission away.
  // The physical facts still have to hold; the unseen plume is reported, not fatal.
  Reset();crew=Roster();c=Context(crew);World.FailSmoke=true;var smokeless=new M37TheGrapeseedHarvest();
  Check(smokeless.Begin(c),"M37 starts for the unverified-plume run");DrainPreparation(smokeless,c);
  MarineDrive(smokeless,c,smokeless.Aircraft[0],"M37.Duster1",CrewSlot.Guess);MarineDrive(smokeless,c,smokeless.Aircraft[0],"M37.Land1",CrewSlot.Guess);
  Use(crew,CrewSlot.Ice);ClearPreparationEnemies();DrainPreparation(smokeless,c);
  MarineDrive(smokeless,c,smokeless.Aircraft[1],"M37.Duster2",CrewSlot.Ice);MarineDrive(smokeless,c,smokeless.Aircraft[1],"M37.Land2",CrewSlot.Ice);
  for(int i=0;i<2;i++){Interact(smokeless,c,CrewSlot.Gohan,smokeless.Aircraft[i].Position+smokeless.Aircraft[i].RightVector*3f,4);DrainPreparation(smokeless,c);}
  for(int i=0;i<2;i++){Interact(smokeless,c,CrewSlot.Gohan,smokeless.Aircraft[i].Position+smokeless.Aircraft[i].RightVector*3f,3);DrainPreparation(smokeless,c);}
  Check(smokeless.Status==MissionStatus.Passed&&smokeless.Tested==2&&smokeless.Visible==0,"A plume that will not render is recorded, not a failed mission");
  Check(c.Doctor.Entries.Any(e=>e.Category=="payload"&&e.Severity==DiagnosticSeverity.Warning),"The unverified optical screen is named per aircraft in the doctor");
  World.FailSmoke=false;
  Check(World.Props.Where(p=>p.Model.Name=="prop_barrel_02a").All(p=>p.Exists()&&p.AttachedTo!=null),"Mission cleanup keeps the fitted aircraft canisters with the retained planes");

  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"marine38.json"));var m38=new M38BloodInTheQuarry();Check(m38.Begin(c),"M38 starts with four physical packages and an intact carrier");DrainPreparation(m38,c);
  MarineDrive(m38,c,m38.Hauler,"M38.Load",CrewSlot.Guess);ClearPreparationEnemies();DrainPreparation(m38,c);Interact(m38,c,CrewSlot.Gohan,c.Locations.Position("M38.CabinetWork"),4);DrainPreparation(m38,c);
  var crates=World.Props.Where(p=>p.Model.Name=="prop_cs_cardbox_01").ToArray();
  for(int i=0;i<4;i++)
  {
   Interact(m38,c,CrewSlot.Gohan,crates[i].Position,3);DrainPreparation(m38,c);Check(crates[i].AttachedTo==crew.PedFor(CrewSlot.Gohan),"Charge package must be carried before it counts as loaded");
   Interact(m38,c,CrewSlot.Gohan,m38.Hauler.Position-m38.Hauler.ForwardVector*5.5f,3);DrainPreparation(m38,c);
  }
  Check(m38.Loaded==4&&crates.All(p=>p.AttachedTo==m38.Hauler),"All four packages stay attached to the required truck");
  // The Benson has two seats. Gohan rides in the box; asking for a third seat is
  // what failed this mission in play, so the walkthrough must not hand him one.
  Check(GTA.Native.Function.Seats(new Model("benson").Hash)==2,"The stand-in reports the Benson's real two seats");
  Use(crew,CrewSlot.Guess);crew.PedFor(CrewSlot.Guess).SetIntoVehicle(m38.Hauler,VehicleSeat.Driver);crew.PedFor(CrewSlot.Ice).SetIntoVehicle(m38.Hauler,VehicleSeat.Passenger);
  crew.PedFor(CrewSlot.Gohan).Task.LeaveVehicle();crew.PedFor(CrewSlot.Gohan).Position=m38.Hauler.Position-m38.Hauler.ForwardVector*4f;
  DrainPreparation(m38,c);
  Check(m38.GohanInTheBack&&crew.PedFor(CrewSlot.Gohan).AttachedTo==m38.Hauler,"Gohan rides in the back of the Benson instead of a seat it does not have");
  MarineDrive(m38,c,m38.Hauler,"M38.Exit",CrewSlot.Guess);MarineDrive(m38,c,m38.Hauler,"M38.Senora.Delivery",CrewSlot.Guess);
  Interact(m38,c,CrewSlot.Gohan,m38.Hauler.Position-m38.Hauler.ForwardVector*5.5f,4);DrainPreparation(m38,c);
  Check(m38.Status==MissionStatus.Passed&&c.State.CargoAt("seismicCharges")=="M38.Senora.Delivery","M38 delivers the same carrier and verifies its attached load");

  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"marine39.json"));var m39=new M39ThePaletoCable();Check(m39.Begin(c),"M39 starts with a real underwater junction");DrainPreparation(m39,c);
  MarineDrive(m39,c,m39.SurveySub,"M39.Route1",CrewSlot.Gohan);MarineDrive(m39,c,m39.SurveySub,"M39.Route2",CrewSlot.Gohan);
  MarineDrive(m39,c,m39.SurveySub,"M39.CutterWork",CrewSlot.Gohan);Interact(m39,c,CrewSlot.Gohan,c.Locations.Position("M39.CutterWork"),6,true);DrainPreparation(m39,c);
  Check(!m39.CableCut&&World.Props.Any(p=>p.Model.Name=="prop_ld_bomb_01"&&p.AttachedTo!=null),"Attaching the cutter does not falsely claim that the cable is already cut");
  Interact(m39,c,CrewSlot.Gohan,c.Locations.Position("M39.CutterWork"),8,true);DrainPreparation(m39,c);
  Check(m39.CableCut&&World.Created.Count(p=>p.Model.Name=="s_m_y_blackops_01")==4,"Completed cutter cycle releases the actual shore response");
  Use(crew,CrewSlot.Ice);ClearPreparationEnemies();DrainPreparation(m39,c);MarineDrive(m39,c,m39.SurveySub,"M39.Return",CrewSlot.Gohan);
  Check(m39.Status==MissionStatus.Passed&&c.State.FleetUpgrades["rigMainlandCableCut"],"The cable consequence commits after Gohan returns");

  Reset();crew=Roster();c=Context(crew);var m40=new M40ThePhantomRigging();Check(m40.Begin(c),"M40 starts two floating Tropics, shore kits, targets and a navigation laptop");DrainPreparation(m40,c);
  for(int i=1;i<=2;i++){Interact(m40,c,CrewSlot.Guess,c.Locations.Position("M40.Kit"+i),5);DrainPreparation(m40,c);}
  Use(crew,CrewSlot.Ice);DrainPreparation(m40,c);Check(m40.CurrentStage==2,"M40 does not skip the two weapon targets on an empty wait");
  Function.TestDamage.Add(m40.Targets[0].Handle);DrainPreparation(m40,c);Check(m40.CurrentStage==2,"One target hit does not count as both weapon checks");
  Function.TestDamage.Add(m40.Targets[1].Handle);DrainPreparation(m40,c);Interact(m40,c,CrewSlot.Gohan,c.Locations.Position("M40.NavWork"),4);DrainPreparation(m40,c);
  for(int i=0;i<2;i++){var slot=i==0?CrewSlot.Guess:CrewSlot.Ice;MarineDrive(m40,c,m40.Boats[i],"M40.Boat"+(i+1),slot);MarineDrive(m40,c,m40.Boats[i],"M40.Trial"+(i+1),slot);MarineDrive(m40,c,m40.Boats[i],"M40.Return"+(i+1),slot);}
  Check(m40.Status==MissionStatus.Passed&&m40.Trials==2,"M40 requires two complete loaded sea trials and returns");
  Check(m40.Boats.All(v=>!v.IsInvincible&&v.MaxHealth==1200),"The reinforcement adds finite durability and never invincibility");

  Reset();crew=Roster();c=Context(crew);m36=new M36DeepWellRecon();m36.Begin(c);DrainPreparation(m36,c);m36.SurveySub.IsDead=true;m36.Tick();Check(m36.Status==MissionStatus.Failed,"Losing the required sub fails before any survey reward");
  Reset();crew=Roster();c=Context(crew);m36=new M36DeepWellRecon();m36.Begin(c);DrainPreparation(m36,c);
  for(int i=1;i<=3;i++){MarineDrive(m36,c,m36.SurveySub,"M36.Scan"+i,CrewSlot.Gohan);Interact(m36,c,CrewSlot.Gohan,c.Locations.Position("M36.Scan"+i),5,true);DrainPreparation(m36,c);}
  var patrol=World.Created.First(p=>p.Model.Name=="s_m_y_blackops_01");m36.SurveySub.Position=patrol.Position+new Vector3(5,0,0);m36.SurveySub.Position=new Vector3(m36.SurveySub.Position.X,m36.SurveySub.Position.Y,0);
  m36.Tick();Game.GameTime+=8100;m36.Tick();Check(m36.Status==MissionStatus.Failed,"Eight seconds of exposed surface contact fails M36 instead of hiding a nonexistent detection system");
  Reset();crew=Roster();c=Context(crew);m39=new M39ThePaletoCable();m39.Begin(c);DrainPreparation(m39,c);
  MarineDrive(m39,c,m39.SurveySub,"M39.Route1",CrewSlot.Gohan);MarineDrive(m39,c,m39.SurveySub,"M39.Route2",CrewSlot.Gohan);MarineDrive(m39,c,m39.SurveySub,"M39.CutterWork",CrewSlot.Gohan);
  Interact(m39,c,CrewSlot.Gohan,c.Locations.Position("M39.CutterWork"),6,true);DrainPreparation(m39,c);World.Props.First(p=>p.Model.Name=="prop_ld_bomb_01").Detach();
  Interact(m39,c,CrewSlot.Gohan,c.Locations.Position("M39.CutterWork"),8,true);DrainPreparation(m39,c);Check(m39.Status==MissionStatus.Failed&&!m39.CableCut,"A detached cutter cannot grant a successful cable cut");
  World.FailSmoke=true;Check(!AircraftSmoke.Emit(new Vehicle()),"Missing smoke assets return failure instead of a successful test");World.FailSmoke=false;
  Reset();crew=Roster();c=Context(crew);World.FailVehicles=true;Check(!new M37TheGrapeseedHarvest().Begin(c),"Missing aircraft rejects startup cleanly");World.FailVehicles=false;
  var state=CampaignState.Load(Path.Combine(root,"marine-rewards.json"));var catalog=new MissionCatalog();foreach(int n in Enumerable.Range(36,5)){string id="M"+n;catalog.All.Add(Def(id,"main"));state.MarkComplete(id,catalog);}int cash=state.CashOnHand;
  Check(cash==610000&&state.FleetUpgrades["aircraftSmokeReady"]&&state.FleetUpgrades["extractionLaunchesReady"],"The five first completions pay 610000 and record tested equipment");foreach(int n in Enumerable.Range(36,5))state.MarkComplete("M"+n,catalog);Check(state.CashOnHand==cash,"The marine block never pays a replay twice");
  var fleet=new FleetGarage(state);var duster=new Vehicle{Model=new Model("duster")};Game.Player.Character.SetIntoVehicle(duster,VehicleSeat.Driver);World.SmokeBursts=0;Game.Accept=true;
  fleet.UpdateSmoke(false);Check(World.SmokeBursts==0,"Menus and mission input gates can block free-roam smoke");fleet.UpdateSmoke(true);fleet.UpdateSmoke(true);Check(World.SmokeBursts==1,"Unlocked Duster smoke releases once and respects its cooldown");Game.GameTime+=12001;Game.Accept=true;fleet.UpdateSmoke(true);Check(World.SmokeBursts==2,"Smoke becomes available again after twelve seconds");Game.Accept=false;
 }
}
