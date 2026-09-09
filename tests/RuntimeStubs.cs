// Test stand-ins only. Never included in Bloodlines.dll or installed in GTA.
using System;
using Bloodlines.Core;
using GTA;
using System.Collections.Generic;
using System.Drawing;
using GTA.Math;
namespace GTA.Math {
 public struct Vector3 {
  public float X,Y,Z; public Vector3(float x,float y,float z){X=x;Y=y;Z=z;}
  public static Vector3 Zero => new Vector3();
  public float DistanceTo(Vector3 v){return (float)System.Math.Sqrt((X-v.X)*(X-v.X)+(Y-v.Y)*(Y-v.Y)+(Z-v.Z)*(Z-v.Z));}
  public static Vector3 operator +(Vector3 a,Vector3 b)=>new Vector3(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
  public static Vector3 operator -(Vector3 a,Vector3 b)=>new Vector3(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
  public static Vector3 operator *(Vector3 a,float b)=>new Vector3(a.X*b,a.Y*b,a.Z*b);
  public static bool operator ==(Vector3 a,Vector3 b)=>a.X==b.X&&a.Y==b.Y&&a.Z==b.Z;
  public static bool operator !=(Vector3 a,Vector3 b)=>!(a==b);
  public override bool Equals(object o)=>o is Vector3 v&&this==v;
  public override int GetHashCode()=>X.GetHashCode()^Y.GetHashCode()^Z.GetHashCode();
 }
}
namespace GTA {
 public static class Script {public static void Wait(int ms){Game.GameTime+=ms;}}
 public struct RelationshipGroup {public int Hash;public static implicit operator RelationshipGroup(int v)=>new RelationshipGroup{Hash=v};public static implicit operator int(RelationshipGroup g)=>g.Hash;}
 public static class Game {public static bool IsPaused;public static int GenerateHash(string s)=>s.GetHashCode();public static int GameTime; public static float TimeScale=1; public static Player Player=new Player(); public static bool Arrested;}
 public class Player {public bool IsInvincible;public Vehicle LastVehicle;public Ped Character=new Ped(); public bool IsDead; public bool CanControlCharacter=true; public int WantedLevel;}
 public class Entity {public bool IsDead;public bool IsPersistent,IsOnScreen,Released;static int next; public int Handle=++next; public bool Present=true; public bool Exists()=>Present; public Vector3 Position,Velocity; public float Heading; public bool IsPositionFrozen,IsCollisionEnabled=true,IsVisible=true;}
 public enum Relationship {Neutral,Hate}
 public enum WeaponHash {MicroSMG,CarbineRifle}public class Weapons {public void Give(WeaponHash w,int ammo,bool equip,bool loaded){}} public class Ped:Entity {public VehicleSeat SeatIndex {get{if(CurrentVehicle!=null)foreach(var pair in CurrentVehicle.Seats)if(pair.Value==this)return pair.Key;return VehicleSeat.None;}}public Weapons Weapons=new Weapons();public int MaxHealth;public bool CanSufferCriticalHits;public void ClearBloodDamage(){}public void ClearLastWeaponDamage(){}public int Health,Armor,Accuracy;public bool IsAlive=>!IsDead;public void SetIntoVehicle(Vehicle v,VehicleSeat s){CurrentVehicle=v;Position=v.Position;v.Seats[s]=this;}
  public bool IsInCombat,IsShooting,IsInvincible,AlwaysKeepTask,BlockPermanentEvents,Entering,Hostile;
  public Vector3 ForwardVector; public Vehicle CurrentVehicle; public int RelationshipGroup; public Ped CombatTarget;
  public Tasks Task; public Ped(){Task=new Tasks(this);} public bool IsInVehicle()=>CurrentVehicle!=null;
  public bool IsInVehicle(Vehicle v)=>v!=null&&CurrentVehicle==v;
  public Relationship GetRelationshipWithPed(Ped p)=>Hostile?Relationship.Hate:Relationship.Neutral;
 }
 public class Model {public bool IsValid=true,IsInCdImage=true;public Model(){}public Model(string s){IsHelicopter=s=="buzzard";IsCar=!IsHelicopter;}public void MarkAsNoLongerNeeded(){}public bool IsCar=true,IsBike;public bool IsTrain,IsSubmarine,IsHelicopter,IsPlane,IsBoat;}
 public enum DrivingStyle {Normal,AvoidTraffic,AvoidTrafficExtremely} public enum VehicleMissionType {GoTo,Cruise,Circle,Attack}
 public enum HeliMissionFlags {None} public enum BoatMissionFlags {None} public enum VehicleDrivingFlags {None}
 public class Vehicle:Entity {public void PlaceOnGround(){}public Vector3 ForwardVector=new Vector3(0,1,0);public int PassengerCapacity=>Capacity;public bool IsSirenActive;public bool IsEngineRunning;public Model Model=new Model();public bool IsDriveable=true,IsInAir;public float HeightAboveGround;public Ped GetPedOnSeat(VehicleSeat s)=>Seats.TryGetValue(s,out var p)?p:null;public float Speed; public string DisplayName="test car"; public int Capacity=3; public Dictionary<VehicleSeat,Ped> Seats=new Dictionary<VehicleSeat,Ped>(); public bool IsSeatFree(VehicleSeat s)=>(int)s>=0&&(int)s<Capacity&&!Seats.ContainsKey(s);}
 public enum VehicleSeat {None=-3,Driver=-1,Passenger=0,RightFront=0,LeftRear=1,RightRear=2}
 public enum EnterVehicleFlags {None}
 public class Tasks {public VehicleMissionType LastMission;public int VehicleMissions;public void StartVehicleMission(Vehicle v,Ped p,VehicleMissionType t,float s,VehicleDrivingFlags f,float r,float straight,bool wrong){VehicleMissions++;LastMission=t;}public void StartScenario(string s,Vector3 p,float h){}public void VehicleShootAtPed(Ped p){Shots++;LastTarget=p;}public int Shots;public void StartHeliMission(Vehicle v,Ped p,VehicleMissionType m,float s,float r,int h,int min,float a,float slow,HeliMissionFlags f){DriveKind="heli-attack";}public int Gotos;public void GoTo(Vector3 p){Gotos++;} public void LeaveVehicle(){owner.CurrentVehicle=null;}public void VehicleChase(Ped p){}public int Wanders;public void WanderAround(Vector3 p,float r){Wanders++;}public void StandStill(int t){}
  public string DriveKind;public Vector3 DriveTarget;public int DriveCalls;public float DriveSpeed;
  public void DriveTo(Vehicle v,Vector3 p,float r,float s,DrivingStyle f){DriveKind="road";DriveTarget=p;DriveSpeed=s;DriveCalls++;}
  public void CruiseWithVehicle(Vehicle v,float s,DrivingStyle f){DriveKind="cruise";DriveSpeed=s;DriveCalls++;}
  public void StartHeliMission(Vehicle v,Vector3 p,VehicleMissionType m,float s,float r,int h,int min,float angle,float slow,HeliMissionFlags f){DriveKind="heli";DriveTarget=p;DriveCalls++;}
  public void StartPlaneMission(Vehicle v,Vector3 p,VehicleMissionType m,float s,float r,int h,int min,float angle,bool precise){DriveKind="plane-"+m;DriveTarget=p;DriveCalls++;}
  public void StartBoatMission(Vehicle v,Vector3 p,VehicleMissionType m,float s,VehicleDrivingFlags f,float r,BoatMissionFlags b){DriveKind="boat";DriveTarget=p;DriveCalls++;}
  readonly Ped owner; public int Clears,Enters,Warps,Fights; public VehicleSeat LastSeat; public Ped LastTarget;
  public Tasks(Ped p){owner=p;} public void ClearAll(){Clears++;owner.IsInCombat=false;owner.CombatTarget=null;} public void ClearAllImmediately(){ClearAll();}
  public void FollowToOffsetFromEntity(Ped p,Vector3 v,float s,int t,float r,bool b){} public void GuardCurrentPosition(){}
  public void FightAgainst(Ped p){Fights++;LastTarget=p;owner.IsInCombat=true;owner.CombatTarget=p;}
  public void EnterVehicle(Vehicle v,VehicleSeat s,int t,float f,EnterVehicleFlags flags){Enters++;LastSeat=s;owner.Entering=true;}
  public void WarpIntoVehicle(Vehicle v,VehicleSeat s){Warps++;LastSeat=s;owner.CurrentVehicle=v;v.Seats[s]=owner;}
 }
 public enum BlipSprite {Standard} public enum BlipColor {Yellow}
 public class Blip:Entity {public BlipSprite Sprite;public BlipColor Color;public bool IsShortRange,ShowRoute;public string Name;}
 public static class World {public static Vector3 GetSafeCoordForPed(Vector3 p,bool b,int f)=>p;public static int AddRelationshipGroup(string s)=>1;public static Ped CreatePed(Model m,Vector3 p,float h)=>new Ped{Position=p,Heading=h};public static bool SphereVisible;public static Vehicle[] NearbyVehicles=new Vehicle[0];public static List<Vehicle> Vehicles=new List<Vehicle>();public static Vehicle[] GetNearbyVehicles(Vector3 p,float r)=>NearbyVehicles;public static Func<Vector3,Vector3> StreetResolver;public static Vector3 GetNextPositionOnStreet(Vector3 p)=>StreetResolver==null?p:StreetResolver(p);public static Vehicle CreateVehicle(Model m,Vector3 p,float h){var v=new Vehicle{Position=p,Heading=h,Model=m};Vehicles.Add(v);return v;}public static Blip WaypointBlip;public static object RenderingCamera; public static Ped[] Nearby=new Ped[0];public static Blip LastBlip;public static bool CollisionReady;
  public static Ped[] GetNearbyPeds(Ped p,float r)=>Nearby; public static Blip CreateBlip(Vector3 p){return LastBlip=new Blip{Position=p};}}
 public class ScriptSettings {
  readonly Dictionary<string,string> values=new Dictionary<string,string>();
  public static ScriptSettings Load(string path){var s=new ScriptSettings();string section="";if(!System.IO.File.Exists(path))return s;
   foreach(var line in System.IO.File.ReadAllLines(path)){var v=line.Trim();if(v.StartsWith(";")||v.Length==0)continue;
    if(v.StartsWith("[")){section=v.Trim('[',']');continue;}int eq=v.IndexOf('=');if(eq>0)s.values[section+"/"+v.Substring(0,eq).Trim()]=v.Substring(eq+1).Trim();}return s;}
  public T GetValue<T>(string section,string key,T fallback){if(!values.TryGetValue(section+"/"+key,out var value))return fallback;return (T)Convert.ChangeType(value,typeof(T),System.Globalization.CultureInfo.InvariantCulture);}
 }
}
namespace GTA.Native {
 public enum Hash {GET_DISABLED_CONTROL_NORMAL,RESURRECT_PED,TASK_LEAVE_VEHICLE,CHANGE_PLAYER_PED,SET_DRIVE_TASK_CRUISE_SPEED,TASK_DRIVE_BY,HAS_ENTITY_CLEAR_LOS_TO_ENTITY,SET_CURRENT_PED_VEHICLE_WEAPON,IS_HUD_HIDDEN,REQUEST_STREAMED_TEXTURE_DICT,HAS_STREAMED_TEXTURE_DICT_LOADED,HIDE_HUD_COMPONENT_THIS_FRAME,GET_SAFE_ZONE_SIZE,GET_ASPECT_RATIO,ARE_PLAYER_STARS_GREYED_OUT,DRAW_SPRITE,SET_POLICE_IGNORE_PLAYER,SET_EVERYONE_IGNORE_PLAYER,SET_PLAYER_WANTED_LEVEL_NOW,SET_VEHICLE_SHOOT_AT_TARGET,SET_HELI_BLADES_FULL_SPEED,SET_RELATIONSHIP_BETWEEN_GROUPS,SET_PED_COMBAT_ATTRIBUTES,IS_POSITION_OCCUPIED,IS_SPHERE_VISIBLE,SET_DRIVER_ABILITY,SET_DRIVER_AGGRESSIVENESS,SET_TRAIN_CRUISE_SPEED,TASK_SUBMARINE_GOTO_AND_STOP,TASK_VEHICLE_TEMP_ACTION,GET_PED_TARGET_FROM_COMBAT_PED,SET_PLAYER_CONTROL,CLEAR_FOCUS,UNCUFF_PED,SET_ENABLE_HANDCUFFS,RESET_PED_MOVEMENT_CLIPSET,RESET_PED_STRAFE_CLIPSET,RESET_PED_WEAPON_MOVEMENT_CLIPSET,IS_PED_IN_COMBAT,IS_PED_GETTING_INTO_A_VEHICLE,GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS,REQUEST_COLLISION_AT_COORD,HAS_COLLISION_LOADED_AROUND_ENTITY,IS_PLAYER_BEING_ARRESTED,SET_CAM_DEATH_FAIL_EFFECT_STATE,RESET_PLAYER_ARREST_STATE,FORCE_GAME_STATE_PLAYING,PAUSE_DEATH_ARREST_RESTART,IGNORE_NEXT_RESTART,SET_FADE_OUT_AFTER_DEATH,SET_FADE_OUT_AFTER_ARREST,SET_FADE_IN_AFTER_DEATH_ARREST}
 public static class Function {
  public static float MovementInput;public static int SelfHandovers;public static int Sprites, TankShots, DriveBys;public static bool ClearLos=true; public static Hash? ThrowOnce; public static Dictionary<Hash,object> Values=new Dictionary<Hash,object>();
  public static void Call(Hash h,params object[] args){if(ThrowOnce==h){ThrowOnce=null;throw new InvalidOperationException("Injected native failure");}if(h==Hash.DRAW_SPRITE)Sprites++;if(h==Hash.SET_VEHICLE_SHOOT_AT_TARGET)TankShots++;if(h==Hash.TASK_DRIVE_BY)DriveBys++;if(h==Hash.RESURRECT_PED)((Ped)args[0]).IsDead=false;if(h==Hash.CHANGE_PLAYER_PED){if(Game.Player.Character==(Ped)args[1])SelfHandovers++;Game.Player.Character=(Ped)args[1];Game.Player.IsDead=false;}if(h==Hash.TASK_LEAVE_VEHICLE){var ped=(Ped)args[0];var vehicle=ped.CurrentVehicle;if(vehicle!=null){var keys=new List<VehicleSeat>();foreach(var pair in vehicle.Seats)if(pair.Value==ped)keys.Add(pair.Key);foreach(var key in keys)vehicle.Seats.Remove(key);}ped.CurrentVehicle=null;}if(args.Length>0)Values[h]=args[0];if(h==Hash.SET_PLAYER_CONTROL)Game.Player.CanControlCharacter=(bool)args[1];if(h==Hash.RESET_PLAYER_ARREST_STATE)Game.Arrested=false;}
  public static T Call<T>(Hash h,params object[] args){object value=false;
   if(h==Hash.GET_DISABLED_CONTROL_NORMAL)return (T)(object)MovementInput;
   if(h==Hash.GET_ASPECT_RATIO)return (T)(object)(16f/9f);
   if(h==Hash.GET_SAFE_ZONE_SIZE)return (T)(object)1f;
   if(h==Hash.HAS_STREAMED_TEXTURE_DICT_LOADED||h==Hash.SET_CURRENT_PED_VEHICLE_WEAPON)value=true;
   if(h==Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY)value=ClearLos;if(h==Hash.IS_SPHERE_VISIBLE)value=World.SphereVisible;
   if(h==Hash.GET_PED_TARGET_FROM_COMBAT_PED)return (T)(object)((Ped)args[0]).CombatTarget;
   if(h==Hash.IS_PED_IN_COMBAT)value=((Ped)args[0]).CombatTarget==(Ped)args[1];
   if(h==Hash.IS_PED_GETTING_INTO_A_VEHICLE)value=((Ped)args[0]).Entering;
   if(h==Hash.GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS)value=((Vehicle)args[0]).Capacity;
   if(h==Hash.HAS_COLLISION_LOADED_AROUND_ENTITY)value=World.CollisionReady;
   if(h==Hash.IS_PLAYER_BEING_ARRESTED)value=Game.Arrested;return (T)value;}
 }
}
namespace GTA.UI {
 public class ContainerElement {public ContainerElement(PointF p,SizeF s,Color c){}public void Draw(){}}
 public class TextElement {public TextElement(string t,PointF p,float s,Color c){}public void Draw(){}}
}
namespace Bloodlines.Core {
 public static class Logger {public static void Debug(string s){} public static void Info(string s){} public static void Warn(string s){} public static void Error(string s,Exception e=null){} }
 public static class GameUtils {public static bool RequestModel(Model m,int t)=>true;public static void SafeDelete(Entity e){if(e!=null)e.Present=false;}public static void SafeRelease(Entity e){if(e!=null){e.Released=true;e.IsPersistent=false;}}public static bool IsWithinFlat(Vector3 a,Vector3 b,float r)=>new Vector3(a.X,a.Y,0).DistanceTo(new Vector3(b.X,b.Y,0))<=r;public static bool Faded; public static string Message;public static void Subtitle(string s,int ms){Message=s;}public static void Notify(string s){Message=s;}public static void SafeDelete(GTA.Blip b){if(b!=null)b.Present=false;}public static void DrawObjectiveMarker(Vector3 p,Color c,float r){}public static void FadeOut(int ms){Faded=true;}public static void FadeIn(int ms){Faded=false;}}
 public class ModConfig {public float CompanionLeashDistance=180;public bool DeathHandlingEnabled=true,RestoreCheckpointOnDeath=true;public int DeathFadeOutMs=800,DeathHoldMs=1500,DeathFadeInMs=1200;}
 public class DialogueDirector {public int Clears;public void Clear(){Clears++;}}
 public class CampaignData {public Dictionary<string,Vector3> Anchors=new Dictionary<string,Vector3>();public bool TryAnchor(string key,out Vector3 p,out float h){h=90;return Anchors.TryGetValue(key,out p);}}
}
namespace Bloodlines.Crew {
 public enum CrewSlot {Ice,Gohan,Guess}
 public class Protagonist {public string DisplayName=>Handle;public string FirstName="Ice",Handle="Ice";public static Protagonist Of(CrewSlot s)=>new Protagonist();}
 public struct PedPlacement {public Vector3 Position;public float Heading;public PedPlacement(Vector3 p,float h){Position=p;Heading=h;}}
 public class CrewRoster {public CompanionController CompanionAI=new CompanionController(new ModConfig());public bool IsDeployed=true,CompanionsHoldPosition,CanRevive=true;public int Dismissals,Regroups,ActiveRecoveries;public CrewSlot ActiveSlot;public Protagonist Active=new Protagonist();public GTA.Ped ActivePed;
  public PedPlacement? RecoveryOrigin=new PedPlacement(new Vector3(10,20,30),0);public PedPlacement? DeployOrigin;
  public GTA.Ped PedFor(CrewSlot s)=>ActivePed;
  public bool ReviveActiveAt(Vector3 p,float h){if(!ReviveAll())return false;ActiveRecoveries++;ActivePed.Position=p;return true;}
  public bool ReviveAll(){if(!CanRevive)return false;ActivePed.IsDead=false;GTA.Game.Player.IsDead=false;GTA.Game.Player.Character=ActivePed;return true;}
  public void RegroupAt(Vector3 p,float h){Regroups++;ActivePed.Position=p;}
  public void ReturnToStoryOrigin(){}public void Dismiss(){Dismissals++;IsDeployed=false;GTA.Game.Player.Character=new GTA.Ped();GTA.Game.Player.IsDead=false;}}
 public class SwitchController {public int Cancels;public void Cancel(){Cancels++;}}
}
namespace Bloodlines.Abilities {public class AbilityController {public int Stops;public void Stop(){Stops++;}}}
namespace Bloodlines.Missions {
 public class MissionContext {public Crew.CrewRoster Crew=new Crew.CrewRoster();}
 public class MissionManager {public bool IsRunning=true,CheckpointSupported;public int Failures;public bool TryRestoreCheckpoint()=>CheckpointSupported;public void ForceFail(string reason){if(IsRunning)Failures++;IsRunning=false;}}
}

namespace Bloodlines.Core {public static class ObjectiveMarkers {public static void Navigation(GTA.Math.Vector3 p, Bloodlines.Crew.CrewSlot? s=null){}}}
