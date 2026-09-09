// Test stand-ins only. Never included in Bloodlines.dll or installed in GTA.
using System;
using GTA;
using System.Collections.Generic;
using System.Drawing;
using GTA.Math;
namespace GTA.Math {
 public struct Vector3 { public float ToHeading()=>0f;
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
 public struct RelationshipGroup {public int Hash;public static implicit operator RelationshipGroup(int value)=>new RelationshipGroup{Hash=value};public static implicit operator int(RelationshipGroup group)=>group.Hash;}
 public static class Game {public static int GenerateHash(string s)=>s.GetHashCode();public static int GameTime; public static float TimeScale=1; public static Player Player=new Player(); public static bool Arrested,Accept,IsPaused; public static void DisableControlThisFrame(Control c){} public static bool IsControlPressed(Control c)=>false;public static bool IsControlJustPressed(Control c){bool v=Accept;Accept=false;return v;}}
 public class Player {public Vehicle LastVehicle;public Ped Character=new Ped(); public bool IsDead; public bool CanControlCharacter=true; public int WantedLevel;public bool Targeting;public bool IsTargeting(Entity p)=>Targeting;}
 public class Entity {public bool IsOnScreen;public Vector3 Rotation;public float HeightAboveGround;public bool IsDead;static int next; public int Handle=++next; public bool Present=true; public bool Exists()=>Present; public Vector3 Position,Velocity; public float Heading; public bool IsPositionFrozen,IsInvincible,IsPersistent,Released;public void Delete(){Present=false;}}
 public enum Relationship {Neutral,Hate}
 public class Weapons {public HashSet<WeaponHash> Owned=new HashSet<WeaponHash>();public void Give(WeaponHash w,int a,bool e,bool l){Owned.Add(w);}}
 public class Ped:Entity {public int Health=300,MaxHealth=300;public bool CanSufferCriticalHits;public VehicleSeat SeatIndex {get {if(CurrentVehicle!=null)foreach(var p in CurrentVehicle.Seats)if(p.Value==this)return p.Key;return VehicleSeat.None;}}
  public bool IsRagdoll,IsCuffed,IsBeingStunned,CanBeDraggedOutOfVehicle;public DrivingStyle DrivingStyle; public void Kill(){IsDead=true;} public bool IsHeadtracking(Ped p)=>false;public bool IsAiming;public bool IsAlive=>!IsDead;public int Armor,Accuracy;public Weapons Weapons=new Weapons();public Blip AddBlip()=>new Blip();public void SetIntoVehicle(Vehicle v,VehicleSeat s){CurrentVehicle=v;Position=v.Position;v.Seats[s]=this;}
  public bool IsInCombat,IsShooting,AlwaysKeepTask,BlockPermanentEvents,Entering,Hostile;
  public Vector3 ForwardVector; public Vehicle CurrentVehicle; public int RelationshipGroup; public Ped CombatTarget;
  public Tasks Task; public Ped(){Task=new Tasks(this);} public bool IsInVehicle()=>CurrentVehicle!=null;
  public bool IsInVehicle(Vehicle v)=>v!=null&&CurrentVehicle==v;
  public Relationship GetRelationshipWithPed(Ped p)=>Hostile?Relationship.Hate:Relationship.Neutral;
 }
 public enum VehicleLockStatus {Unlocked}
 public enum VehicleModType {Engine,Transmission,Suspension} public class Mod {public int Index;} public class Mods {public void InstallModKit(){}public Mod this[VehicleModType type]=>new Mod();public VehicleColor PrimaryColor;public Color CustomPrimaryColor;}
 public class Prop:Entity {public Blip AddBlip()=>new Blip();} public class Vehicle:Entity {public int Repairs;public void Repair(){Repairs++;}public void Wash(){}public void PlaceOnGround(){}public Model Model=new Model("car");public bool CanTiresBurst,AreLightsOn,IsInAir,IsSirenActive;public float CurrentRPM,ForwardSpeed;public int PassengerCapacity=>Capacity;public Vector3 ForwardVector=new Vector3(0,1,0);public float EngineHealth=1000,EnginePowerMultiplier;public bool IsDriveable=true,IsEngineRunning;public Mods Mods=new Mods();public VehicleLockStatus LockStatus;public Ped GetPedOnSeat(VehicleSeat s)=>Seats.TryGetValue(s,out var p)?p:null;public Blip AddBlip()=>new Blip();public float Speed; public string DisplayName="test car"; public int Capacity=3; public Dictionary<VehicleSeat,Ped> Seats=new Dictionary<VehicleSeat,Ped>(); public bool IsSeatFree(VehicleSeat s)=>(int)s>=0&&(int)s<Capacity&&!Seats.ContainsKey(s);}
 public enum VehicleSeat {None=-3,Any=-2,Driver=-1,Passenger=0,RightFront=0,LeftRear=1,RightRear=2}
 public enum EnterVehicleFlags {None}
 public enum DrivingStyle {Rushed,Normal,AvoidTraffic,AvoidTrafficExtremely}
 public enum LeaveVehicleFlags {None}
 public enum VehicleColor {MetallicBlack}
 public enum ExplosionType {Extinguisher,Boat,Tanker,Plane}
 public enum HeliMissionFlags {None} public enum VehicleMissionType {GoTo,Circle,Attack} public enum VehicleDrivingFlags {None} public enum BoatMissionFlags {None}
 public class Tasks {public int VehicleMissions;public void StartVehicleMission(Vehicle v,Ped p,VehicleMissionType t,float s,VehicleDrivingFlags f,float r,float straight,bool wrong){VehicleMissions++;}public void StartHeliMission(Vehicle v,Ped p,VehicleMissionType t,float s,float r,int h,int min,float a,float slow,HeliMissionFlags f){HeliTasks++;}public int Wanders,Chases;public void WanderAround(Vector3 p,float r){Wanders++;}public void VehicleChase(Ped p){Chases++;}public void VehicleShootAtPed(Ped p){} public void RunTo(Vector3 p,bool a,int t){}public void StartBoatMission(Vehicle v,Vehicle target,VehicleMissionType t,float s,VehicleDrivingFlags f,float r,BoatMissionFlags b){BoatTasks++;} public int HeliTasks;public void StartHeliMission(Vehicle v,Vector3 p,VehicleMissionType t,float s,float r,int h,int min,float a,float slow,HeliMissionFlags f){HeliTasks++;}public void StartPlaneMission(Vehicle v,Vector3 p,VehicleMissionType t,float s,float r,int h,int min,float a,bool b){}public int BoatTasks;public void StartBoatMission(Vehicle v,Vector3 p,VehicleMissionType t,float s,VehicleDrivingFlags f,float r,BoatMissionFlags b){BoatTasks++;}public int Drives,Gotos;public void DriveTo(Vehicle v,Vector3 p,float r,float s,DrivingStyle d){Drives++;}public void LeaveVehicle(LeaveVehicleFlags f=LeaveVehicleFlags.None){if(owner.CurrentVehicle!=null)owner.CurrentVehicle.Seats.Remove(owner.SeatIndex);owner.CurrentVehicle=null;}public void GoTo(Vector3 p){Gotos++;}public void HandsUp(int t){}public void ChaseWithHelicopter(Ped p,Vector3 v){}public int Aims,Scenarios;public void AimAt(Ped p,int t){Aims++;}public void StartScenario(string s,Vector3 p,float h){Scenarios++;}public int Cruises;public void CruiseWithVehicle(Vehicle v,float f,DrivingStyle d){Cruises++;}public void FightAgainstHatedTargets(float r){}
  readonly Ped owner; public int Clears,Enters,Warps,Fights; public VehicleSeat LastSeat; public Ped LastTarget;
  public Tasks(Ped p){owner=p;} public void StandStill(int t){} public void ClearAll(){Clears++;} public void ClearAllImmediately(){Clears++;}
  public void FollowToOffsetFromEntity(Ped p,Vector3 v,float s,int t,float r,bool b){} public void GuardCurrentPosition(){}
  public void FightAgainst(Ped p){Fights++;LastTarget=p;owner.IsInCombat=true;}
  public void EnterVehicle(Vehicle v,VehicleSeat s,int t,float f,EnterVehicleFlags flags){Enters++;LastSeat=s;owner.Entering=true;}
  public void WarpIntoVehicle(Vehicle v,VehicleSeat s){Warps++;LastSeat=s;owner.CurrentVehicle=v;v.Seats[s]=owner;}
 }
 public enum BlipSprite {Safehouse,Helicopter,Plane,Boat,Standard,Enemy,PersonalVehicleCar,ArmoredTruck} public enum BlipColor {Yellow,Blue,Green,Orange,Purple,Red}
 public class Blip:Entity {public BlipSprite Sprite;public BlipColor Color;public bool IsShortRange,ShowRoute;public string Name;}
 public static class World {public static Vehicle[] NearbyVehicles=new Vehicle[0];public static Vehicle[] GetNearbyVehicles(Vector3 p,float r)=>NearbyVehicles;public static Prop CreateProp(Model m,Vector3 p,bool a,bool b)=>new Prop{Position=p};public static void AddExplosion(Vector3 v,ExplosionType t,float p,float q,Ped owner,bool a,bool b){}public static Camera RenderingCamera; public static List<Ped> Created=new List<Ped>();public static bool FailCamera;
  public static bool WaterAvailable=true;public static bool FailNavigation,FailPeds;public static List<Vehicle> Vehicles=new List<Vehicle>();
  public static Vector3 GetNextPositionOnStreet(Vector3 p)=>p;
  public static Vector3 GetSafeCoordForPed(Vector3 p,bool sidewalk,int flags)=>FailNavigation?Vector3.Zero:p;
  public static int AddRelationshipGroup(string s)=>1;
  public static Vehicle CreateVehicle(Model m,Vector3 p,float h){var v=new Vehicle{Position=p,Heading=h,Model=m};Vehicles.Add(v);return v;}
  public static Camera CreateCamera(Vector3 p,Vector3 r,float f)=>FailCamera?null:new Camera{Position=p};public static Ped CreatePed(Model m,Vector3 p,float h){if(FailPeds)return null;var a=new Ped{Position=p,Heading=h};Created.Add(a);return a;}
  public static Ped[] Nearby=new Ped[0];public static Blip LastBlip;public static bool CollisionReady;
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
 public enum Hash {SET_DRIVE_TASK_CRUISE_SPEED,TASK_DRIVE_BY,HAS_ENTITY_CLEAR_LOS_TO_ENTITY,SET_CURRENT_PED_VEHICLE_WEAPON,IS_HUD_HIDDEN,REQUEST_STREAMED_TEXTURE_DICT,HAS_STREAMED_TEXTURE_DICT_LOADED,HIDE_HUD_COMPONENT_THIS_FRAME,GET_SAFE_ZONE_SIZE,GET_ASPECT_RATIO,ARE_PLAYER_STARS_GREYED_OUT,DRAW_SPRITE,IS_IPL_ACTIVE,REQUEST_IPL,DISABLE_ALL_CONTROL_ACTIONS,GET_INTERIOR_AT_COORDS,PIN_INTERIOR_IN_MEMORY,REFRESH_INTERIOR,IS_INTERIOR_READY,GET_INTERIOR_FROM_ENTITY,UNPIN_INTERIOR,REMOVE_IPL,GET_NUM_PED_HAIR_TINTS,GET_PED_HEAD_OVERLAY_NUM,SET_PED_HEAD_OVERLAY,SET_PED_HEAD_OVERLAY_TINT,GET_PED_DRAWABLE_VARIATION,GET_NUMBER_OF_PED_PROP_DRAWABLE_VARIATIONS,GET_NUMBER_OF_PED_PROP_TEXTURE_VARIATIONS,CLEAR_PED_PROP,SET_PED_PROP_INDEX,ADD_TO_CLOCK_TIME,HAS_PED_GOT_WEAPON,IS_WEAPON_VALID,SET_NEW_WAYPOINT,IS_POSITION_OCCUPIED,SET_VEHICLE_SHOOT_AT_TARGET,SET_HELI_BLADES_FULL_SPEED,SET_RELATIONSHIP_BETWEEN_GROUPS,SET_DRIVER_ABILITY,SET_DRIVER_AGGRESSIVENESS,SET_POLICE_IGNORE_PLAYER,SET_EVERYONE_IGNORE_PLAYER,SET_PLAYER_WANTED_LEVEL_NOW,GIVE_WEAPON_TO_PED,SET_VEHICLE_LIGHTS,SET_MAX_WANTED_LEVEL,SET_PED_COMBAT_ATTRIBUTES,ATTACH_ENTITY_TO_ENTITY,DETACH_ENTITY,SET_PED_INTO_VEHICLE,CAN_PED_SEE_HATED_PED,IS_SPHERE_VISIBLE,GET_WATER_HEIGHT,REQUEST_WEAPON_ASSET,REMOVE_WEAPON_ASSET,SHOOT_SINGLE_BULLET_BETWEEN_COORDS,SET_VEHICLE_FORWARD_SPEED,TASK_VEHICLE_TEMP_ACTION,TASK_VEHICLE_FOLLOW,TASK_VEHICLE_SHOOT_AT_PED,SET_VEHICLE_UNDRIVEABLE,SET_VEHICLE_DOOR_OPEN,SET_PED_DEFAULT_COMPONENT_VARIATION,CLEAR_ALL_PED_PROPS,SET_PED_HEAD_BLEND_DATA,SET_PED_HAIR_TINT,GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS,GET_NUMBER_OF_PED_TEXTURE_VARIATIONS,SET_PED_COMPONENT_VARIATION,IS_DISABLED_CONTROL_PRESSED,IS_DISABLED_CONTROL_JUST_PRESSED,GET_DISABLED_CONTROL_NORMAL,TASK_LEAVE_VEHICLE,IS_PLAYER_FREE_AIMING_AT_ENTITY,CHANGE_PLAYER_PED,IS_PLAYER_SWITCH_IN_PROGRESS,STOP_PLAYER_SWITCH,SET_FOCUS_POS_AND_VEL,CLEAR_FOCUS,HIDE_HUD_AND_RADAR_THIS_FRAME,IS_PED_IN_COMBAT,IS_PED_GETTING_INTO_A_VEHICLE,GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS,REQUEST_COLLISION_AT_COORD,HAS_COLLISION_LOADED_AROUND_ENTITY,IS_PLAYER_BEING_ARRESTED,SET_CAM_DEATH_FAIL_EFFECT_STATE,RESET_PLAYER_ARREST_STATE,FORCE_GAME_STATE_PLAYING,PAUSE_DEATH_ARREST_RESTART,IGNORE_NEXT_RESTART,SET_FADE_OUT_AFTER_DEATH,SET_FADE_OUT_AFTER_ARREST,SET_FADE_IN_AFTER_DEATH_ARREST}
 public class OutputArgument {public T GetResult<T>()=>default(T);}
 public static class Function {
  public static Dictionary<Control,bool> Held=new Dictionary<Control,bool>();public static Dictionary<Control,float> Axes=new Dictionary<Control,float>();
  public static int Sprites, TankShots, DriveBys;public static bool ClearLos=true;public static bool InteriorReady=true;public static int InteriorId=123; public static bool ResetVitalsOnSwitch;public static bool EjectOnSwitch;public static Hash? ThrowOnce; public static Dictionary<Hash,object> Values=new Dictionary<Hash,object>();
  public static void Call(Hash h,params object[] args){if(ThrowOnce==h){ThrowOnce=null;throw new InvalidOperationException("Injected native failure");}if(h==Hash.TASK_LEAVE_VEHICLE)((Ped)args[0]).CurrentVehicle=null;if(h==Hash.DRAW_SPRITE)Sprites++;if(h==Hash.SET_VEHICLE_SHOOT_AT_TARGET)TankShots++;if(h==Hash.TASK_DRIVE_BY)DriveBys++;if(args.Length>0)Values[h]=args[0];if(h==Hash.RESET_PLAYER_ARREST_STATE)Game.Arrested=false;if(h==Hash.CHANGE_PLAYER_PED){var old=Game.Player.Character;Game.Player.Character=(Ped)args[1];if(ResetVitalsOnSwitch){old.Health=200;old.Armor=0;old.MaxHealth=200;Game.Player.Character.Health=200;Game.Player.Character.Armor=0;Game.Player.Character.MaxHealth=200;}if(EjectOnSwitch)Game.Player.Character.Task.LeaveVehicle();}}
  public static T Call<T>(Hash h,params object[] args){if(ThrowOnce==h){ThrowOnce=null;throw new InvalidOperationException("Injected native failure");}object value=false; if(h==Hash.GET_INTERIOR_AT_COORDS||h==Hash.GET_INTERIOR_FROM_ENTITY)return (T)(object)InteriorId;if(h==Hash.IS_INTERIOR_READY)value=InteriorReady;
   if(h==Hash.GET_ASPECT_RATIO)return (T)(object)(16f/9f);
   if(h==Hash.GET_SAFE_ZONE_SIZE)return (T)(object)1f;
   if(h==Hash.HAS_STREAMED_TEXTURE_DICT_LOADED||h==Hash.SET_CURRENT_PED_VEHICLE_WEAPON)value=true;
   if(h==Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY)value=ClearLos;if(h==Hash.IS_WEAPON_VALID)value=true;if(h==Hash.HAS_PED_GOT_WEAPON)value=((Ped)args[0]).Weapons.Owned.Contains((WeaponHash)(uint)args[1]);if(h==Hash.GET_WATER_HEIGHT)value=World.WaterAvailable;
   if(h==Hash.GET_NUM_PED_HAIR_TINTS)return (T)(object)64;
   if(h==Hash.GET_PED_HEAD_OVERLAY_NUM)return (T)(object)29;
   if(h==Hash.GET_PED_DRAWABLE_VARIATION)return (T)(object)0;
   if(h==Hash.GET_NUMBER_OF_PED_PROP_DRAWABLE_VARIATIONS)return (T)(object)3;
   if(h==Hash.GET_NUMBER_OF_PED_PROP_TEXTURE_VARIATIONS)return (T)(object)2;
   if(h==Hash.GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS)return (T)(object)20;
   if(h==Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS)return (T)(object)12;
   if(h==Hash.GET_DISABLED_CONTROL_NORMAL)return (T)(object)(Axes.TryGetValue((Control)args[1],out var axis)?axis:0f);
   if(h==Hash.IS_DISABLED_CONTROL_PRESSED)value=Held.TryGetValue((Control)args[1],out var held)&&held;
   if(h==Hash.IS_PLAYER_FREE_AIMING_AT_ENTITY&&Values.ContainsKey(h))value=Values[h];
   if(h==Hash.IS_PED_IN_COMBAT)value=((Ped)args[0]).CombatTarget==(Ped)args[1];
   if(h==Hash.IS_PED_GETTING_INTO_A_VEHICLE)value=((Ped)args[0]).Entering;
   if(h==Hash.GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS)value=((Vehicle)args[0]).Capacity;
   if(h==Hash.HAS_COLLISION_LOADED_AROUND_ENTITY)value=World.CollisionReady;
   if(h==Hash.IS_PLAYER_BEING_ARRESTED)value=Game.Arrested;return (T)value;}
 }
}
namespace GTA.UI {
 public enum Alignment {Center} public class CustomSprite {public CustomSprite(string p,SizeF s,PointF f){}public void Draw(){}}
 public static class Screen {public static string Subtitle;public static void ShowSubtitle(string s,int t){Subtitle=s;}}
 public class ContainerElement {public ContainerElement(PointF p,SizeF s,Color c){}public void Draw(){}}
 public class TextElement {public Alignment Alignment;public TextElement(string t,PointF p,float s,Color c){}public void Draw(){}}
}
namespace Bloodlines.Core {
 public static class Logger {public static void Debug(string s){} public static void Info(string s){} public static void Warn(string s){} public static void Error(string s,Exception e=null){} }
 public static class GameUtils {public static bool IsWithin(Vector3 a,Vector3 b,float r)=>a.DistanceTo(b)<=r;public static void SafeRelease(GTA.Entity e){e.Released=true;} public static bool IsWithinFlat(Vector3 a,Vector3 b,float r)=>new Vector3(a.X,a.Y,0).DistanceTo(new Vector3(b.X,b.Y,0))<=r;public static bool RequestModel(GTA.Model m,int t=5000)=>true; public static void PlayFrontendSound(string a,string b){} public static void SetClock(int h,int m){}public static void SetWeather(string s){}public static bool IsScreenFadedOut()=>Faded;public static void SafeDelete(GTA.Entity e){if(e!=null)e.Present=false;} public static bool Faded; public static string Message;public static void Subtitle(string s,int ms){Message=s;}public static void Notify(string s){Message=s;}public static void SafeDelete(GTA.Blip b){if(b!=null)b.Present=false;}public static void DrawObjectiveMarker(Vector3 p,Color c,float r=1f){}public static void FadeOut(int ms){Faded=true;}public static void FadeIn(int ms){Faded=false;}}
 public class ModConfig {public string AbilityKey="CapsLock";public bool AbilitiesEnabled=true;public bool ControllerSwitchEnabled=true,SuppressVanillaSwitch=true,DevToolsEnabled=true;public bool CompanionsRespawnOnDeath;public float CompanionLeashDistance=180;public bool DeathHandlingEnabled=true,RestoreCheckpointOnDeath=true;public int DeathFadeOutMs=800,DeathHoldMs=1500,DeathFadeInMs=1200;}


}
namespace Bloodlines.Crew {


 public struct PedPlacement {public Vector3 Position;public float Heading;public PedPlacement(Vector3 p,float h){Position=p;Heading=h;}}
 public enum CompanionState {Follow,Scripted}
 public class CompanionController {public bool MissionActive;public CompanionLife Life=new CompanionLife();public bool RequireSharedVehicle;public void ReleaseAll(){Controlled.Clear();RequireSharedVehicle=false;}public CompanionState StateOf(CrewSlot s)=>Controlled.Contains(s)?CompanionState.Scripted:CompanionState.Follow;public System.Collections.Generic.HashSet<CrewSlot> Controlled=new System.Collections.Generic.HashSet<CrewSlot>();public void TakeControl(CrewSlot s){Controlled.Add(s);}public void ReleaseControl(CrewSlot s){Controlled.Remove(s);}}
 public class CrewRoster {public bool DeploySolo(CrewSlot slot,Vector3 p,float h){IsSolo=true;Peds.Clear();Peds[slot]=new Ped{Position=p};ActiveSlot=slot;Game.Player.Character=Peds[slot];return true;}public bool Deploy(CrewSlot s,Vector3 p,float h){var d=new Dictionary<CrewSlot,PedPlacement>();foreach(var hero in Protagonist.All)d[hero.Slot]=new PedPlacement(p,h);return Deploy(s,d);}public CompanionController CompanionAI=new CompanionController();
  public bool Deploy(CrewSlot slot,IDictionary<CrewSlot,PedPlacement> positions){foreach(var pair in positions)Peds[pair.Key]=new GTA.Ped{Position=pair.Value.Position};ActiveSlot=slot;GTA.Game.Player.Character=Peds[slot];return true;}
  public void AssignCompanionAI(){}public void OrderCompanionsToFight(){}public int CrewGroup;public bool IsSolo; public bool IsDeployed=true,CompanionsHoldPosition,CanRevive=true;public int Dismissals,Regroups;public CrewSlot ActiveSlot;public Protagonist Active=>Protagonist.Of(ActiveSlot);public GTA.Ped ActivePed;
  public PedPlacement? RecoveryOrigin=new PedPlacement(new Vector3(10,20,30),0);public PedPlacement? DeployOrigin;
  public Dictionary<CrewSlot,GTA.Ped> Peds=new Dictionary<CrewSlot,GTA.Ped>(); public GTA.Ped PedFor(CrewSlot s)=>Peds.TryGetValue(s,out var p)?p:ActivePed; public void SetActive(CrewSlot s){ActiveSlot=s;}
  public bool ReviveAll(){if(!CanRevive)return false;ActivePed.IsDead=false;GTA.Game.Player.IsDead=false;GTA.Game.Player.Character=ActivePed;return true;}
  public void RegroupAt(Vector3 p,float h){Regroups++;ActivePed.Position=p;}
  public void ReturnToStoryOrigin(){}public void Dismiss(){Dismissals++;IsDeployed=false;GTA.Game.Player.Character=new GTA.Ped();GTA.Game.Player.IsDead=false;}}

}
namespace Bloodlines.Abilities {public class AbilityController {public void Refill(){}public int Stops;public void Stop(){Stops++;}}}
namespace Bloodlines.Missions {


}

namespace GTA {
 public enum Control {Context,ScriptLS,ScriptRS,SpecialAbility,SpecialAbilitySecondary,SpecialAbilityPC,VehicleSpecialAbilityFranklin,Duck,VehicleHorn,LookBehind,VehicleLookBehind,CharacterWheel,Attack,FrontendAccept,SelectCharacterMichael,SelectCharacterFranklin,SelectCharacterTrevor,SelectCharacterMultiplayer,LookLeftRight,LookUpDown,MeleeAttack1,VehicleExit}
 public class Camera:Entity {public void PointAt(Vector3 p){}}
 public class Model {public bool IsInCdImage=true,IsValid=true;public bool IsCar=true,IsBike,IsHelicopter,IsPlane,IsBoat;public string Name;public Model(string s){Name=s;IsBoat=s=="longfin";IsHelicopter=s=="supervolito";IsPlane=s=="vestra";IsBike=s=="shinobi";IsCar=!IsBoat&&!IsHelicopter&&!IsPlane&&!IsBike;}public void MarkAsNoLongerNeeded(){}}
 public enum WeaponHash {AssaultSMG,AssaultShotgun,CombatMG,CombatPistol,SpecialCarbine,Unarmed,HeavySniper,StunGun,Parachute,MG,Nightstick,FlareGun,SniperRifle,AssaultRifle,PumpShotgun,CarbineRifle,RPG,Pistol50,StickyBomb,SMG,APPistol,Flashlight,SmokeGrenade,MicroSMG,Pistol,SawnOffShotgun}
 public static class Script {public static int Waited;public static void Wait(int ms){Waited+=ms;Game.GameTime+=ms;}}
}
namespace Bloodlines.Core { public static class SurveyMode {public static bool IsSurveyRunning;} }
namespace Bloodlines.Missions {

 public enum Act {I=1,II=2,III=3}

 public class MissionDefinition {public Core.MissionInfo Info;public System.Func<Mission> Factory;public bool IsPlayable=true;public string Id=>Info.Id;public string Title=>Info.Title;public Act Act=>Act.I;}
 public class MissionCatalog {public List<MissionDefinition> All=new List<MissionDefinition>();public IEnumerable<MissionDefinition> Playable=>All;}
 public class CheckpointManager {public void Clear(){}public void Commit(string s,int n){}public int Restore(string s)=>-1;public bool HasCheckpointFor(string s)=>false;}
 public class MissionContext {public CampaignState State;public Core.ModConfig Config;public Crew.CrewRoster Crew;public Crew.SwitchController Switching;public Core.CampaignData Data;public Core.LocationBook Locations;public Core.CutsceneDirector Cutscenes;public Core.DialogueDirector Dialogue;public Abilities.AbilityController Abilities=new Abilities.AbilityController();public CheckpointManager Checkpoints=new CheckpointManager();}
}

