using System;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Campaign;
using GTA;
using GTA.Math;
using GTA.Native;
public static partial class StoryTests
{
 static void PlacementRepairChecks()
 {
  Reset();var crew=Roster();var c=Context(crew);Use(crew,CrewSlot.Ice);
  var car=new Vehicle();Game.Player.Character.SetIntoVehicle(car,VehicleSeat.Driver);
  var dyno=new DynoObjective("Test",()=>car,20,30,8){RequiredCharacter=CrewSlot.Ice};dyno.Enter(c);
  Function.Held[(Control)71]=true;
  for(int i=0;i<6;i++){Game.GameTime+=500;dyno.Update(c);}
  Check(Math.Abs(Field<float>(dyno,"_pressure")-24f)<.01f,"Controller full trigger ramps to 24 PSI over three seconds, without an RPM jump");
  Function.Held[(Control)71]=false;Game.GameTime+=500;dyno.Update(c);
  Check(Math.Abs(Field<float>(dyno,"_pressure")-24f)<.01f,"Releasing the trigger holds the calibrated pressure");
  float earned=Field<float>(dyno,"_held");Function.Held[(Control)71]=true;
  for(int i=0;i<5;i++){Game.GameTime+=500;dyno.Update(c);}
  Check(Field<float>(dyno,"_held")>=earned&&!dyno.IsFinished,"Overshoot pauses progress without deleting earned calibration time");
  Function.Held[(Control)71]=false;Function.Held[(Control)72]=true;
  for(int i=0;i<4;i++){Game.GameTime+=500;dyno.Update(c);}
  Function.Held[(Control)72]=false;
  for(int i=0;i<16&&!dyno.IsFinished;i++){Game.GameTime+=500;dyno.Update(c);}
  Check(dyno.IsFinished,"Controller calibration finishes after correcting with the brake trigger");
  Reset();crew=Roster();c=Context(crew);var m23=new M23GhostInTheSage();
  Check(m23.Begin(c)&&m23.YardGate!=null&&m23.GeneratorProp!=null,"M23 creates the actual gate and generator before briefing");
  Check(World.Props.Any(p=>p.Model.Name=="prop_tool_bench02")&&World.Props.Any(p=>p.Model.Name=="prop_barrel_02a"),"M23 inspection bays contain the objects the dialogue names");
  m23.Abort();m23.Cleanup();Check(!m23.YardGate.Exists()&&!m23.GeneratorProp.Exists(),"Aborting M23 removes its temporary structures for a clean retry");
  var gate=new Prop{Position=new Vector3(10,20,30)};var slide=new SlideYardGateStep(gate,new Vector3(8,0,0));slide.Start();
  Game.GameTime+=1000;Check(!slide.IsComplete&&gate.Position.X>10&&gate.Position.X<18,"The powered gate visibly moves through its opening travel");
  slide.Cancel();float canceled=gate.Position.X;Check(canceled<18,"Canceling the gate scene does not claim a completed opening");
  var skipped=new SlideYardGateStep(gate,new Vector3(8,0,0));skipped.Finish();Check(gate.Position.X==canceled+8,"Skipping the gate scene reaches its explicit open position");
  Check(!M29DustAndDiesel.AtRetreatPoint(1000,251)&&M29DustAndDiesel.AtRetreatPoint(1000,250),"M29 pursuit breaks off at the final quarter of its initial delivery distance");
  Check(!M29DustAndDiesel.AtRetreatPoint(1000,-1)&&!M29DustAndDiesel.AtRetreatPoint(0,0),"Failed or uninitialized road-distance queries cannot dismiss M29 pursuit");
  Reset();crew=Roster();c=Context(crew);var fuel=new M29DustAndDiesel();Check(fuel.Begin(c),"M29 starts for pursuit lifecycle checks");
  var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
  typeof(M29DustAndDiesel).GetMethod("StartPursuit",flags).Invoke(fuel,null);
  GameUtils.RoadAvailable=true;
  try { typeof(M29DustAndDiesel).GetMethod("SpawnPursuitCar",flags).Invoke(fuel,null); }
  finally { GameUtils.RoadAvailable=false; }
  var response=fuel.PursuitCars.Single();var driver=response.GetPedOnSeat(VehicleSeat.Driver);var gunner=response.GetPedOnSeat(VehicleSeat.Passenger);
  Check(driver!=null&&gunner!=null&&gunner.Weapons.Owned.Contains(WeaponHash.MicroSMG)&&!driver.Weapons.Owned.Contains(WeaponHash.MicroSMG),"M29 response uses a driving-only driver and an armed passenger");
  typeof(M29DustAndDiesel).GetMethod("EndPursuit",flags).Invoke(fuel,null);
  Check(fuel.PursuitEnded&&driver.Task.Cruises==1&&gunner.Task.Clears>0&&response.Exists(),"Retreat clears firing tasks and drives away without deleting the pursuing car");
  typeof(M29DustAndDiesel).GetMethod("EndPursuit",flags).Invoke(fuel,null);
  Check(driver.Task.Cruises==1,"M29 retreat runs only once");fuel.Abort();fuel.Cleanup();Check(!response.Exists(),"Aborting M29 cleans up pursuit-owned entities");

 }
}
