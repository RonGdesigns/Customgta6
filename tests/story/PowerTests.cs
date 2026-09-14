using System;
using System.Linq;
using Bloodlines.Core;
using GTA;
using GTA.Math;
using GTA.Native;

public static partial class StoryTests
{
 static void PowerChecks()
 {
  Check(WorldTuning.PowerTarget(0f,60f)==1f&&WorldTuning.PowerTarget(15f,60f)==1f,"No added torque from rest through the launch band");
  float previous=1f;
  for(int speed=16;speed<=90;speed++) {float next=WorldTuning.PowerTarget(speed,60f);if(next<previous||next>1.80001f)throw new Exception("Non-monotonic or excessive power ramp");previous=next;}
  Check(previous>1.79f,"Speed-based assistance increases smoothly to a bounded 80-percent boost");
  Check(WorldTuning.PowerTarget(-50f,60f)==1f&&WorldTuning.PowerTarget(30f,float.NaN)==1f,"Reverse and invalid gearing receive no assistance");
  Check(WorldTuning.RampPower(1f,1.8f,5f)<=1.021f,"A slow frame cannot dump full power into the car");
  Check(WorldTuning.RampPower(1.8f,1f,.016f)==1f,"Returning to launch speed immediately removes accumulated boost");
  Reset();var crew=Roster();var tuning=new WorldTuning();var car=new Vehicle{IsEngineRunning=true,Velocity=new Vector3(0,60,0)};World.Vehicles.Add(car);tuning.Update(crew);
  Game.LastFrameTime=.1f;
  for(int i=0;i<20;i++){Game.GameTime+=100;tuning.Update(crew);}
  float power=(float)Function.Calls.Last(c=>c.Item1==Hash.SET_VEHICLE_CHEAT_POWER_INCREASE).Item2[1];
  Check(power>1.39f&&power<1.41f,"A fast-moving car receives only 40-percent assistance after two seconds");
  car.Velocity=Vector3.Zero;Game.GameTime+=100;tuning.Update(crew);
  Check((float)Function.Calls.Last(c=>c.Item1==Hash.SET_VEHICLE_CHEAT_POWER_INCREASE).Item2[1]==1f,"Braking to a stop restores an unboosted relaunch");
  car.Velocity=new Vector3(0,60,0);car.IsInAir=true;Game.GameTime+=100;tuning.Update(crew);
  Check((float)Function.Calls.Last(c=>c.Item1==Hash.SET_VEHICLE_CHEAT_POWER_INCREASE).Item2[1]==1f,"Airborne cars receive no speed assistance");
  car.IsInAir=false;Game.GameTime+=100;tuning.Update(crew);Game.GameTime+=2000;tuning.Update(crew);
  Check((float)Function.Calls.Last(c=>c.Item1==Hash.SET_VEHICLE_CHEAT_POWER_INCREASE).Item2[1]<=1.021f,"Resuming after a cutscene restarts the power ramp without a surge");
  tuning.Reset();Check((float)Function.Calls.Last(c=>c.Item1==Hash.SET_VEHICLE_CHEAT_POWER_INCREASE).Item2[1]==1f,"Stand-down explicitly releases torque assistance");Game.LastFrameTime=.016f;

  // September 13: the game asserted one second after a crew deploy, in the middle of
  // this pass. The pass must not write every car's shared handling inside one frame,
  // whatever the root cause turns out to be.
  Reset();crew=Roster();var throttled=new WorldTuning();
  for(int i=0;i<20;i++)World.Vehicles.Add(new Vehicle{IsEngineRunning=true});
  Function.Calls.Clear();throttled.Update(crew);
  Check(Function.Calls.Count(c=>c.Item1==Hash.SET_VEHICLE_MAX_SPEED)==WorldTuning.RegistrationsPerTick&&
        throttled.PendingRegistrations==20-WorldTuning.RegistrationsPerTick,
    "A deploy registers a bounded number of cars in its first tick and queues the rest");
  for(int i=0;i<12&&throttled.PendingRegistrations>0;i++){Game.GameTime+=16;throttled.Update(crew);}
  Check(throttled.PendingRegistrations==0&&Function.Calls.Count(c=>c.Item1==Hash.SET_VEHICLE_MAX_SPEED)==20,
    "Every queued car still takes its handling pass over the frames that follow");
  int settled=Function.Calls.Count(c=>c.Item1==Hash.SET_VEHICLE_MAX_SPEED);
  Game.GameTime+=1200;throttled.Update(crew);
  Check(Function.Calls.Count(c=>c.Item1==Hash.SET_VEHICLE_MAX_SPEED)==settled,
    "A later sweep does not re-register cars it already holds");
  throttled.Reset();Check(throttled.PendingRegistrations==0,"Stand-down drops the queue");

  // The switch that takes this pass out of the picture for one launch.
  Reset();crew=Roster();var disabled=new WorldTuning(new ModConfig{HandlingTuningEnabled=false});
  var untouched=new Vehicle{IsEngineRunning=true};float stock=untouched.HandlingData.InitialDriveMaxFlatVelocity;
  World.Vehicles.Add(untouched);Function.Calls.Clear();
  for(int i=0;i<4;i++){Game.GameTime+=1200;disabled.Update(crew);}
  Check(disabled.PendingRegistrations==0&&!Function.Calls.Any(c=>c.Item1==Hash.SET_VEHICLE_MAX_SPEED)&&
        untouched.HandlingData.InitialDriveMaxFlatVelocity==stock,
    "Handling tuning off writes no shared handling and leaves the car as the game ships it");
  Check(!Function.Calls.Any(c=>c.Item1==Hash.SET_VEHICLE_CHEAT_POWER_INCREASE),
    "Handling tuning off also withholds the torque assistance that rides on it");
 }
}
