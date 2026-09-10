using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
using GTA.Native;

public static partial class StoryTests
{
 static int Calls(Hash h)=>Function.Calls.Count(c=>c.Item1==h);
 static object[] LastCall(Hash h)=>Function.Calls.Last(c=>c.Item1==h).Item2;

 static void VisualsDamageChecks()
 {
  // ---- Deformation: per class, on the shared handling, restored; never the crew's protection.
  Reset();var config=new ModConfig();var sedan=Car(VehicleClass.Sedans,"primo");var h=sedan.HandlingData;
  Check(typeof(RoadHandling).GetMethod("Apply").GetParameters().Length==3,"The shared-handling profile no longer takes a per-crew flag");
  var road=new RoadHandling();Check(road.Apply(h,sedan,config)&&Math.Abs(h.DeformationDamageMultiplier-2f)<1e-4&&Math.Abs(h.CollisionDamageMultiplier-.9f)<1e-4&&Math.Abs(h.EngineDamageMultiplier-.8f)<1e-4,"A car takes the full deformation and the softer collision and engine damage");
  Check(road.Restore(h)&&Math.Abs(h.DeformationDamageMultiplier-1f)<1e-4&&Math.Abs(h.CollisionDamageMultiplier-1f)<1e-4,"Restore puts the damage fields back with the rest");
  var suv=Car(VehicleClass.SUVs,"granger");new RoadHandling().Apply(suv.HandlingData,suv,config);var truck=Car(VehicleClass.Industrial,"benson");new RoadHandling().Apply(truck.HandlingData,truck,config);
  Check(Math.Abs(suv.HandlingData.DeformationDamageMultiplier-1.825f)<1e-3&&Math.Abs(truck.HandlingData.DeformationDamageMultiplier-1.675f)<1e-3&&Math.Abs(truck.HandlingData.CollisionDamageMultiplier-1f)<1e-4,"SUVs and trucks crumple less and trucks keep stock collision damage");
  var off=new ModConfig{VehicleDamageEnabled=false};var plain=Car(VehicleClass.Sedans,"primo2");new RoadHandling().Apply(plain.HandlingData,plain,off);
  Check(Math.Abs(plain.HandlingData.DeformationDamageMultiplier-1f)<1e-4,"Damage off leaves the damage fields untouched while the road profile still applies");

  // ---- Crew protection: per vehicle instance, follows the occupant, restored on exit and stand-down.
  Reset();var crew=Roster();var tuning=new WorldTuning{Config=config};var ride=Car(VehicleClass.Sedans,"schafter3");float col=ride.HandlingData.CollisionDamageMultiplier;World.Vehicles.Add(ride);
  tuning.Update(crew);
  Check(Calls(Hash.SET_VEHICLE_DAMAGE_SCALE)==0&&Calls(Hash.SET_PLAYER_VEHICLE_DAMAGE_MODIFIER)==0,"An empty car gets no protection");
  Game.Player.Character.SetIntoVehicle(ride,VehicleSeat.Driver);tuning.Update(crew);tuning.Update(crew);
  Check(Calls(Hash.SET_VEHICLE_DAMAGE_SCALE)==1&&(float)LastCall(Hash.SET_VEHICLE_DAMAGE_SCALE)[1]==.55f&&Calls(Hash.SET_PLAYER_VEHICLE_DAMAGE_MODIFIER)==1&&(float)LastCall(Hash.SET_PLAYER_VEHICLE_DAMAGE_MODIFIER)[1]==.55f,"A brother aboard puts the protection on that vehicle and on the player, once");
  Check(Math.Abs(ride.HandlingData.CollisionDamageMultiplier-col*.9f)<1e-4,"The model's shared collision damage carries only the class factor, not the crew's protection");
  Game.Player.Character.Task.LeaveVehicle();tuning.Update(crew);
  Check((float)LastCall(Hash.SET_VEHICLE_DAMAGE_SCALE)[1]==1f&&(float)LastCall(Hash.SET_PLAYER_VEHICLE_DAMAGE_MODIFIER)[1]==1f&&!tuning.ProtectedVehicles.Any(),"Getting out removes the protection from the car and the player");
  Game.Player.Character.SetIntoVehicle(ride,VehicleSeat.Driver);tuning.Update(crew);crew.IsDeployed=false;tuning.Update(crew);
  Check((float)LastCall(Hash.SET_VEHICLE_DAMAGE_SCALE)[1]==1f&&(float)LastCall(Hash.SET_PLAYER_VEHICLE_DAMAGE_MODIFIER)[1]==1f,"Stand-down restores the vehicle and the player's damage scale");

  // ---- Visuals: grading by time band, persistent settings once, per-frame ones every frame, all restored.
  Reset();var visuals=new VisualAtmosphere(config);World.CurrentTimeOfDay=TimeSpan.FromHours(13);
  visuals.Update(false,false);visuals.Update(false,false);visuals.Update(false,false);
  Check(visuals.ActiveModifier=="cinema_default"&&Math.Abs(visuals.ActiveStrength-.35f)<1e-4&&Calls(Hash.SET_TIMECYCLE_MODIFIER)==1&&Calls(Hash.SET_TIMECYCLE_MODIFIER_STRENGTH)==1,"At noon the daytime grade is set once at the configured strength");
  Check(Calls(Hash.SET_VEHICLE_LOD_MULTIPLIER)==1&&Calls(Hash.SET_PED_LOD_MULTIPLIER)==1&&(float)LastCall(Hash.SET_VEHICLE_LOD_MULTIPLIER)[0]==1.75f&&Calls(Hash.OVERRIDE_LODSCALE_THIS_FRAME)==3,"Persistent level-of-detail multipliers are set once; the scene override is per frame");
  Check(Calls(Hash.CASCADE_SHADOWS_SET_CASCADE_BOUNDS_SCALE)==1&&Calls(Hash.SET_VEHICLE_HEADLIGHT_SHADOWS)==1&&Calls(Hash.SET_GAMEPLAY_CAM_MOTION_BLUR_SCALING_THIS_UPDATE)==3,"Shadows are configured once; blur is stripped every frame");
  var reflect=Function.Calls.Where(c=>c.Item1==Hash.SET_ENTITY_USE_MAX_DISTANCE_FOR_WATER_REFLECTION).ToList();
  Check(reflect.Count==1&&reflect[0].Item2[0]==Game.Player.Character&&(bool)reflect[0].Item2[1],"Water reflection distance is set on the player entity, with the flag, once");
  Check(visuals.OceanApplied&&Calls(Hash.SET_DEEP_OCEAN_SCALER)==1&&(float)LastCall(Hash.SET_DEEP_OCEAN_SCALER)[0]==1.25f,"The ocean swell is applied once in free roam");
  visuals.Update(false,true);
  Check(!visuals.OceanApplied&&Calls(Hash.RESET_DEEP_OCEAN_SCALER)==1,"A running mission puts the authored water back");
  var boat=new Vehicle{Model=new Model("longfin")};Game.Player.Character.SetIntoVehicle(boat,VehicleSeat.Driver);visuals.Update(false,false);visuals.Update(false,false);
  Check(Function.Calls.Count(c=>c.Item1==Hash.SET_ENTITY_USE_MAX_DISTANCE_FOR_WATER_REFLECTION)==2&&Function.Calls.Last(c=>c.Item1==Hash.SET_ENTITY_USE_MAX_DISTANCE_FOR_WATER_REFLECTION).Item2[0]==boat,"The vehicle the player is in gets the reflection flag once too");
  Game.GameTime+=300;World.CurrentTimeOfDay=TimeSpan.FromHours(18);visuals.Update(false,false);
  Check(visuals.ActiveModifier=="rply_saturation"&&Math.Abs(visuals.ActiveStrength-.35f*.85f)<1e-4,"Dusk switches to the warm grade at its reduced strength");
  Game.GameTime+=300;World.CurrentTimeOfDay=TimeSpan.FromHours(23);visuals.Update(false,false);Check(visuals.ActiveModifier=="cinema","Night takes the night grade");
  visuals.Update(true,false);Check(visuals.ActiveModifier==null&&Calls(Hash.CLEAR_TIMECYCLE_MODIFIER)==1&&Calls(Hash.OVERRIDE_LODSCALE_THIS_FRAME)>3,"A scene releases the grade and keeps the rest");
  Game.GameTime+=300;World.CurrentTimeOfDay=TimeSpan.FromHours(13);var noSmog=new VisualAtmosphere(new ModConfig{DeSmogEnabled=false});noSmog.Update(false,false);
  Check(noSmog.ActiveModifier==null,"DeSmog off means no daytime grade");
  var named=new VisualAtmosphere(new ModConfig{DayModifier="my_daytime",VisualPreset="SunnyCoast"});named.Update(false,false);
  Check(named.ActiveModifier=="my_daytime","An ini modifier override wins over the preset");
  var sunny=new VisualAtmosphere(new ModConfig{VisualPreset="SunnyCoast"});sunny.Update(false,false);Check(sunny.ActiveModifier=="New_Chinatown_sky","Presets pick the daytime modifier");
  var capped=new VisualAtmosphere(new ModConfig{LODScale=2.5f});Function.Calls.Clear();capped.Update(false,false);
  Check((float)LastCall(Hash.SET_VEHICLE_LOD_MULTIPLIER)[0]==2f&&(float)LastCall(Hash.OVERRIDE_LODSCALE_THIS_FRAME)[0]==2f,"Level of detail is capped at 2.0");
  Function.Calls.Clear();visuals.Reset();
  Check(Calls(Hash.CLEAR_TIMECYCLE_MODIFIER)==1&&(float)LastCall(Hash.CASCADE_SHADOWS_SET_CASCADE_BOUNDS_SCALE)[0]==1f&&(bool)LastCall(Hash.SET_VEHICLE_HEADLIGHT_SHADOWS)[0]==false&&(float)LastCall(Hash.SET_VEHICLE_LOD_MULTIPLIER)[0]==1f&&(float)LastCall(Hash.SET_PED_LOD_MULTIPLIER)[0]==1f&&Calls(Hash.RESET_DEEP_OCEAN_SCALER)==1&&Function.Calls.Count(c=>c.Item1==Hash.SET_ENTITY_USE_MAX_DISTANCE_FOR_WATER_REFLECTION&&!(bool)c.Item2[1])==2,"Reset puts back the grade, shadows, headlights, level of detail, ocean and both reflection flags");
  var offVisuals=new VisualAtmosphere(new ModConfig{VisualsEnabled=false});Function.Calls.Clear();offVisuals.Update(false,false);
  Check(Function.Calls.Count==0,"Visuals off touches nothing");

  // ---- The shipped ini can no longer replace the player's file.
  string pkg=File.ReadAllText(Path.Combine(Repo,"tools","package.py"));string bat=File.ReadAllText(Path.Combine(Repo,"tools","windows","install-bloodlines.bat"));string cfg=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Core","ModConfig.cs"));
  Check(pkg.Contains("name + '.example'")&&!pkg.Contains("copy_into(os.path.join(REPO, 'config', name), root)"),"The package ships the ini files as examples");
  Check(bat.Contains("%%F.example")&&cfg.Contains("path + \".example\""),"The installer and the mod itself seed a missing ini from the example and never overwrite one that exists");
  string ini=File.ReadAllText(Path.Combine(Repo,"config","Bloodlines.ini"));
  Check(!ini.Contains("CeramicReflections")&&ini.Contains("OceanSwell")&&ini.Contains("DayModifier")&&!ini.Contains("0 FPS loss"),"The ini drops the misnamed clearcoat key, adds the swell and modifier keys, and stops claiming zero cost");
 }
}
