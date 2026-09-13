using System;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using Bloodlines.Missions.Objectives;
using Bloodlines.Abilities;
using GTA;
using GTA.Math;
using GTA.Native;

public static partial class StoryTests
{
 static void Mission78RepairChecks()
 {
  Reset();var crew=Roster();var c=Context(crew);Function.Seabed=55f;var m7=new M07WiretapWaltz();
  Check(m7.Begin(c)&&m7.Dish!=null&&m7.Dish.Model.Name=="prop_satdish_2_a"&&m7.Dish.IsPositionFrozen,"M07 creates a persistent physical relay before its opening shot");
  c.Cutscenes.Skip();m7.Tick();Use(crew,CrewSlot.Ice);Game.Player.Character.Position=c.Locations.Position("M07.MastTop");m7.Tick();
  Game.Accept=true;m7.Tick();Check(Game.Player.Character.Task.Animations>0,"Pressing the relay prompt starts a work animation");
  Game.GameTime+=7100;m7.Tick();c.Cutscenes.Skip();m7.Tick();c.Cutscenes.Skip();m7.Tick();
  Check(m7.Drone.IsEngineRunning&&m7.Drone.GetPedOnSeat(VehicleSeat.Driver)!=null&&!m7.Drone.GetPedOnSeat(VehicleSeat.Driver).IsDead,"M07 response has a running aircraft with a synchronously seated living pilot");
  var bag=m7.ParachuteBag;Check(bag!=null&&bag.Exists(),"The second parachute is a visible world object");
  Use(crew,CrewSlot.Gohan);Game.Player.Character.Task.LeaveVehicle();Game.Player.Character.Position=bag.Position;m7.Tick();
  Check(!m7.ChuteCollected&&bag.Exists(),"Gohan cannot consume Ice's exit parachute");
  Use(crew,CrewSlot.Ice);Game.Player.Character.Position=bag.Position;m7.Tick();
  Check(m7.ChuteCollected&&!bag.Exists()&&Game.Player.Character.Weapons.Owned.Contains((WeaponHash)unchecked((uint)Game.GenerateHash("GADGET_PARACHUTE"))),"Walking onto the bag issues Ice's chute and removes the collected bag without a button");
  m7.Abort();

  Reset();crew=Roster();c=Context(crew);var car=new Vehicle();crew.PedFor(CrewSlot.Guess).SetIntoVehicle(car,VehicleSeat.Driver);crew.PedFor(CrewSlot.Gohan).SetIntoVehicle(car,VehicleSeat.RightFront);
  var boarding=new EnterVehicleObjective("Rear seat",()=>car){ContextBoarding=true};boarding.Enter(c);Game.Player.Character.Position=car.Position;Game.Pressed.Add(Control.Context);boarding.Update(c);
  Check(Game.Player.Character.Task.LastSeat==VehicleSeat.LeftRear&&Game.Player.Character.Task.Enters==1&&!boarding.IsFinished,"Context boarding requests a free rear seat through the normal door animation");
  Game.Player.Character.SetIntoVehicle(car,VehicleSeat.LeftRear);boarding.Update(c);Check(boarding.IsFinished,"Boarding completes only after the player actually occupies the car");

  Reset();crew=Roster();c=Context(crew);var m8=new M08SupplyAndSever();Check(m8.Begin(c),"M08 repair starts");
  Check(m8.CameraPanel!=null&&m8.SecurityCabin!=null&&m8.CameraPanel.Position.DistanceTo(m8.SecurityCabin.Position)<5f,"CCTV work is attached to a visible port security cabin");
  Check(crew.PedFor(CrewSlot.Gohan).Task.Guards==0,"M08 never starts the active player in an NPC guard task");
  c.Cutscenes.Skip();m8.Tick();Check(Game.Player.CanControlCharacter&&!Game.Player.Character.IsPositionFrozen&&crew.CompanionsHoldPosition,"M08 opening returns player control while the separate crew stations remain held");
  var before=crew.PedFor(CrewSlot.Guess).Position;Use(crew,CrewSlot.Guess);m8.Tick();Use(crew,CrewSlot.Gohan);m8.Tick();
  Check(crew.CompanionsHoldPosition&&crew.PedFor(CrewSlot.Guess).Position==before,"Switching during the split approach retains the mission's hold policy");
  m8.Abort();Check(!crew.CompanionsHoldPosition,"Abort releases M08's temporary split-crew policy");

  Reset();var driver=Game.Player.Character;var fork=new Vehicle();var crate=new Prop{IsPositionFrozen=true,Position=new Vector3(0,3,1)};driver.SetIntoVehicle(fork,VehicleSeat.Driver);
  var destination=new Vector3(0,12,0);var carry=new ForkliftCarryStep(driver,fork,crate,new Vector3(0,1.6f,.75f),destination,0);
  carry.Start();Check(!crate.CollisionEnabled&&crate.AttachedTo==null&&crate.IsPositionFrozen,"Fork carry disables collisions and never attaches a crate to forklift physics");
  fork.Position=destination;fork.Speed=0;Game.GameTime+=1600;Check(carry.IsComplete&&driver.IsInVehicle(fork),"Natural fork carry finish preserves its driver seat");
  var bed=new Vehicle();var stow=new SafeCargoStowStep(crate,bed,new Vector3(0,-1,1));stow.Finish();Check(!stow.Failed&&crate.AttachedTo==bed&&!crate.CollisionEnabled&&!crate.IsPositionFrozen,"Bed transfer attaches only after collision is disabled");
  var rejected=new Prop{RejectAttachments=true};int commits=0;var blocked=new SceneBlocking().Then(new SafeCargoStowStep(rejected,bed,Vector3.Zero)).Then(new VerifySceneStep("must not commit",()=>true,()=>commits++));blocked.Complete();
  Check(blocked.Canceled&&!blocked.Succeeded&&commits==0&&rejected.IsPositionFrozen,"Failed cargo attachment cancels the scene without counting a crate or leaving it in physics");
  var abortCrate=new Prop{Position=new Vector3(20,20,2),IsPositionFrozen=true};var original=abortCrate.Position;carry=new ForkliftCarryStep(driver,fork,abortCrate,Vector3.Zero,destination,0);carry.Start();carry.Cancel();
  Check(abortCrate.Position==original&&abortCrate.IsPositionFrozen&&abortCrate.CollisionEnabled,"Cancel restores the uncommitted crate to its original pad");
  carry=new ForkliftCarryStep(driver,fork,new Prop(),Vector3.Zero,new Vector3(0,40,0),0);carry.Finish();Check(!carry.Failed&&driver.IsInVehicle(fork)&&fork.Position==new Vector3(0,40,0),"Skipping fork transport reaches the same delivery position without ejecting Guess");
  var missing=new Prop{Present=false};carry=new ForkliftCarryStep(driver,fork,missing,Vector3.Zero,destination,0);carry.Start();Check(carry.Failed,"Missing cargo rejects the lift before any attachment native");

  Reset();driver=Game.Player.Character;car=new Vehicle();driver.SetIntoVehicle(car,VehicleSeat.Driver);car.Velocity=new Vector3(6,35,0);var handling=car.HandlingData;float min=handling.TractionCurveMin,angle=handling.TractionCurveLateral;var ability=new SlipstreamReflex();ability.Activate(driver);ability.Update(driver);Game.GameTime+=50;ability.Update(driver);
  Check(ability.Blend>.99f&&car.LastForce.X<0&&car.LastForce.Y==0&&Math.Abs(car.LastForce.X)<=8.01f,"Guess engages quickly and opposes sideways drift without adding forward acceleration");
  Check(handling.TractionCurveMin>min&&handling.TractionCurveLateral<angle,"The ability raises sliding grip while narrowing the slip angle");
  int forces=car.Forces;car.IsInAir=true;Game.GameTime+=20;ability.Update(driver);Check(car.Forces==forces,"The first airborne frame gets no pinning or lateral force");
  ability.Deactivate(driver);while(ability.Settle(driver))Game.GameTime+=100;Check(Math.Abs(handling.TractionCurveMin-min)<.0001f&&Math.Abs(handling.TractionCurveLateral-angle)<.0001f,"Ending the ability restores both added handling values");
 }
}
