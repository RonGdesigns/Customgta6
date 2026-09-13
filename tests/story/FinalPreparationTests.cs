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
 sealed class DropWater : IMarineProbe
 { public float Depth=30; public bool Obstructed; public MarineColumn Column(Vector3 p)=>new MarineColumn{Known=true,Surface=0,Floor=-Depth}; public bool Clear(Vector3 a,Vector3 b)=>!Obstructed; }
 static void FinalPreparationChecks()
 {
  Reset(); var plane=new Vehicle{Position=new Vector3(0,0,250),Velocity=new Vector3(50,0,0)};var sub=new Vehicle{Position=new Vector3(0,0,245)};var pilot=new Ped();pilot.SetIntoVehicle(sub,VehicleSeat.Driver);var water=new DropWater();var drop=new SubmarineAirdrop(plane,sub,pilot,water);
  Check(drop.Secure()&&sub.AttachedTo==plane&&!sub.CollisionEnabled,"Air cargo physically attaches with cradle collision disabled");
  var bad=new Prop{RejectAttachments=true};Check(!drop.Release(new Prop(),bad)&&sub.AttachedTo==plane&&!drop.Released,"A failed second canopy keeps cargo attached and does not start descent");
  var a=new Prop();var b=new Prop();Check(drop.Release(a,b)&&sub.AttachedTo==null&&sub.CollisionEnabled&&a.AttachedTo==sub&&b.AttachedTo==sub,"Release requires two attached canopies and restores physical cargo collision");
  var position=sub.Position;sub.Velocity=new Vector3(15,0,-50);drop.Update();Check(sub.Position==position&&sub.Velocity.Z==-8&&!drop.Floating,"Descent applies drag without teleporting or accepting an elapsed-time landing");
  Game.GameTime+=40000;drop.Update();Check(!drop.Floating&&drop.Failure==null,"Forty seconds in the air never fabricates splashdown");
  sub.Position=Vector3.Zero;sub.Velocity=Vector3.Zero;sub.IsInWater=true;World.CollisionReady=false;drop.Update();Game.GameTime+=2000;drop.Update();Check(!drop.Floating,"Water contact without loaded collision does not hand over the sub");
  World.CollisionReady=true;drop.Update();Game.GameTime+=1600;drop.Update();Check(drop.Floating&&!a.Exists()&&!b.Exists()&&pilot.IsInVehicle(sub),"Stable loaded water contact removes canopies and keeps the real occupant seated");drop.Cleanup();Check(sub.CollisionEnabled&&!sub.IsPositionFrozen,"Cargo cleanup restores collision and freeze state");
  Reset();plane=new Vehicle();sub=new Vehicle();pilot=new Ped();pilot.SetIntoVehicle(sub,VehicleSeat.Driver);water=new DropWater{Depth=1};drop=new SubmarineAirdrop(plane,sub,pilot,water);drop.Secure();drop.Release(new Prop(),new Prop());drop.Update();Check(drop.Failure!=null&&!drop.Floating,"Shallow water cannot pass the physical drop validator");drop.Cleanup();
  Reset();plane=new Vehicle();sub=new Vehicle{Position=new Vector3(0,0,200)};pilot=new Ped();pilot.SetIntoVehicle(sub,VehicleSeat.Driver);water=new DropWater{Obstructed=true};drop=new SubmarineAirdrop(plane,sub,pilot,water);drop.Secure();drop.Release(new Prop(),new Prop());Game.GameTime+=90001;drop.Update();Check(drop.Failure!=null,"A stalled descent has a bounded explicit failure");drop.Cleanup();
  Reset();plane=new Vehicle();sub=new Vehicle();pilot=new Ped();pilot.SetIntoVehicle(sub,VehicleSeat.Driver);drop=new SubmarineAirdrop(plane,sub,pilot);drop.Secure();pilot.Task.LeaveVehicle();drop.Update();Check(drop.Failure!=null,"Losing the sub occupant cannot silently continue the manned delivery");drop.Cleanup();

  Reset();var crew=Roster();var c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"final41.json"));var m41=new M41TheGeneralsWire();Check(m41.Begin(c),"M41 starts the exterior meeting, worker and real extraction car");DrainPreparation(m41,c);
  Interact(m41,c,CrewSlot.Ice,c.Locations.Position("M41.Observe"),3);DrainPreparation(m41,c);Check(m41.CurrentStage==1,"Identification does not skip Bradley's visible walk");
  m41.Bradley.Position=c.Locations.Position("M41.Meeting");DrainPreparation(m41,c);m41.Bradley.IsDead=true;DrainPreparation(m41,c);
  var card=World.Props.First(p=>p.Model.Name=="prop_cs_swipe_card");Interact(m41,c,CrewSlot.Ice,card.Position,2);DrainPreparation(m41,c);Check(card.AttachedTo==crew.PedFor(CrewSlot.Ice),"Bradley's physical card must enter Ice's hand");
  Interact(m41,c,CrewSlot.Ice,m41.Extraction.Position-m41.Extraction.ForwardVector*4f,3);DrainPreparation(m41,c);Check(card.AttachedTo==m41.Extraction,"The verified card stays in the extraction vehicle");
  crew.PedFor(CrewSlot.Ice).SetIntoVehicle(m41.Extraction,VehicleSeat.LeftRear);DrainPreparation(m41,c);MarineDrive(m41,c,m41.Extraction,"M41.Exit",CrewSlot.Guess);
  Check(m41.Status==MissionStatus.Passed&&c.State.EvidenceOf("bradleyKeycard")==EvidenceState.CopyHeld,"M41 passes only after the crew and verified card reach extraction");
  Reset();crew=Roster();c=Context(crew);m41=new M41TheGeneralsWire();m41.Begin(c);DrainPreparation(m41,c);m41.Bradley.IsDead=true;m41.Tick();Check(m41.Status==MissionStatus.Failed,"An unidentified officer kill cannot skip the reconnaissance");
  Reset();crew=Roster();c=Context(crew);m41=new M41TheGeneralsWire();m41.Begin(c);DrainPreparation(m41,c);m41.LodgeWorker.IsDead=true;m41.Tick();Check(m41.Status==MissionStatus.Failed,"Killing the lodge worker fails the mission");
  Reset();crew=Roster();c=Context(crew);m41=new M41TheGeneralsWire();m41.Begin(c);DrainPreparation(m41,c);Game.Player.Character.IsShooting=true;m41.Tick();Game.Player.Character.IsShooting=false;Game.GameTime+=60001;m41.Tick();Check(m41.Status==MissionStatus.Failed,"Bradley escaping after the alarm fails rather than waiting forever");

  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"final42.json"));var m42=new M42SkyfallDelivery();Check(m42.Begin(c),"M42 starts with Gohan in physically attached cargo");DrainPreparation(m42,c);Check(!m42.Titan.IsPositionFrozen&&m42.Titan.Velocity.Y!=0,"The inspected Titan resumes flight before player control");
  m42.Titan.Position=c.Locations.Position("M42.Drop")+new Vector3(0,0,50);Game.Accept=true;m42.Tick();Check(!m42.Drop.Released,"Release input below the altitude corridor is rejected");
  m42.Titan.Position=c.Locations.Position("M42.Drop")+new Vector3(0,0,250);m42.CargoSub.Position=m42.Titan.Position+new Vector3(0,0,-5);Game.Accept=true;m42.Tick();DrainPreparation(m42,c);Check(m42.Drop.Released&&m42.CurrentStage==1,"Correct corridor input releases the cargo but waits for real splashdown");
  m42.CargoSub.Position=c.Locations.Position("M42.Drop");m42.CargoSub.IsInWater=true;m42.CargoSub.Velocity=Vector3.Zero;World.CollisionReady=true;m42.Tick();Game.GameTime+=1600;DrainPreparation(m42,c);Check(m42.Drop.Floating&&m42.CurrentStage==2,"Only verified splashdown opens Gohan's pilot objective");
  MarineDrive(m42,c,m42.CargoSub,"M42.Delivery",CrewSlot.Gohan);Check(m42.Status==MissionStatus.Passed&&c.State.CargoAt("offshoreSub")=="M42.Delivery","M42 delivers the same released sub to the coast");
  Reset();crew=Roster();c=Context(crew);m42=new M42SkyfallDelivery();m42.Begin(c);m42.Abort();Check(!m42.Titan.IsPositionFrozen&&!crew.PedFor(CrewSlot.Gohan).IsInVehicle()&&crew.PedFor(CrewSlot.Gohan).Position==c.Locations.Position("M42.GohanStart"),"Aborting the airborne inspection returns Gohan safely instead of dropping an occupied sub");
  Reset();crew=Roster();c=Context(crew);m42=new M42SkyfallDelivery();m42.Begin(c);crew.PedFor(CrewSlot.Gohan).StuckInSeat=true;m42.Abort();Check(m42.CargoSub.Position==c.Locations.Position("M42.Delivery")&&!m42.Drop.Floating,"A refused unseat uses abort-only safe-water recovery without fabricating a successful drop");
  Reset();crew=Roster();c=Context(crew);m42=new M42SkyfallDelivery();m42.Begin(c);DrainPreparation(m42,c);crew.PedFor(CrewSlot.Guess).Task.LeaveVehicle();m42.Tick();Check(m42.Status==MissionStatus.Failed,"Leaving the pilot seat cannot continue the flight mission unattended");

  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"final43.json"));var catalog=new MissionCatalog();
  foreach(int n in Enumerable.Range(31,12)){string id="M"+n;catalog.All.Add(Def(id,"main"));c.State.MarkComplete(id,catalog);}c.State.SetEvidence("bradleyKeycard",EvidenceState.CopyHeld);c.State.SetCargo("offshoreSub","M42.Delivery");
  var m43=new M43StagingPaleto();Check(m43.Begin(c),"M43 starts with three separately positioned staging assets and a shore ledger");DrainPreparation(m43,c);
  MarineDrive(m43,c,m43.StagingSub,"M43.SubReady",CrewSlot.Gohan);MarineDrive(m43,c,m43.StagingBoat,"M43.BoatReady",CrewSlot.Ice);MarineDrive(m43,c,m43.Helicopter,"M43.Land",CrewSlot.Guess);DrainPreparation(m43,c);
  Check(m43.CurrentStage==4,"The complete preparation ledger allows final physical signoff");
  Interact(m43,c,CrewSlot.Guess,c.Locations.Position("M43.BoardWork"),5);DrainPreparation(m43,c);Check(m43.Status==MissionStatus.Passed&&c.State.CargoAt("offshoreStaging")=="M43.Board","M43 records readiness after checking positions and the physical laptop");
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"final43missing.json"));m43=new M43StagingPaleto();m43.Begin(c);DrainPreparation(m43,c);MarineDrive(m43,c,m43.StagingSub,"M43.SubReady",CrewSlot.Gohan);MarineDrive(m43,c,m43.StagingBoat,"M43.BoatReady",CrewSlot.Ice);MarineDrive(m43,c,m43.Helicopter,"M43.Land",CrewSlot.Guess);DrainPreparation(m43,c);
  Check(m43.CurrentStage==3&&m43.MissingPreparation.Contains("M41")&&m43.MissingPreparation.Contains("Bradley card"),"Debug staging names missing preparations instead of fabricating a completed assault plan");m43.Abort();
  var rewards=CampaignState.Load(Path.Combine(root,"finalrewards.json"));foreach(int n in Enumerable.Range(41,3)){catalog.All.Add(Def("M"+n,"main"));rewards.MarkComplete("M"+n,catalog);}Check(rewards.CashOnHand==375000,"Final preparations pay 375000 on first completion");foreach(int n in Enumerable.Range(41,3))rewards.MarkComplete("M"+n,catalog);Check(rewards.CashOnHand==375000,"Replays cannot farm the new payouts");
 }
}
