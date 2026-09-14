// Test stand-ins only. Never included in Bloodlines.dll or installed in GTA.
using System;
using System.Linq;
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
  public float Length()=>(float)System.Math.Sqrt(X*X+Y*Y+Z*Z);public override int GetHashCode()=>X.GetHashCode()^Y.GetHashCode()^Z.GetHashCode();
 }
}
namespace GTA {
 public enum VehicleToggleModType {TireSmoke=20,Turbo=18,XenonHeadlights=22}
 public enum VehicleWindowTint {Stock=4,PureBlack=1,None=0,LightSmoke=3,Invalid=-1,Limo=5,Green=6,DarkSmoke=2}
 public enum VehicleNeonLight {Front=2,Left=0,Right=1,Back=3}
 public enum LicensePlateStyle {YellowOnBlue=2,BlueOnWhite1=3,BlueOnWhite2=0,BlueOnWhite3=4,YellowOnBlack=1,NorthYankton=5}
 public struct RelationshipGroup {public int Hash;public static implicit operator RelationshipGroup(int value)=>new RelationshipGroup{Hash=value};public static implicit operator int(RelationshipGroup group)=>group.Hash;}
 public static class Game {public static int GenerateHash(string s){unchecked{uint h=0;foreach(char c in s.ToLowerInvariant()){h+=(byte)c;h+=h<<10;h^=h>>6;}h+=h<<3;h^=h>>11;h+=h<<15;return (int)h;}}public static float LastFrameTime=.016f;public static int GameTime; public static float TimeScale=1; public static Player Player=new Player(); public static bool Arrested,Accept,IsPaused; public static HashSet<Control> Disabled=new HashSet<Control>();public static void DisableControlThisFrame(Control c){Disabled.Add(c);} public static bool IsControlPressed(Control c)=>GTA.Native.Function.Held.TryGetValue(c,out var held)&&held;public static HashSet<Control> Pressed=new HashSet<Control>();public static bool IsControlJustPressed(Control c){if(Pressed.Remove(c))return true;bool v=Accept;Accept=false;return v;}}
 public class Player {public Vehicle LastVehicle;public Ped Character=new Ped(); public bool IsDead; public bool CanControlCharacter=true; public int WantedLevel;public bool Targeting;public bool IsTargeting(Entity p)=>Targeting;}
 public enum ForceType {MinForce,MaxForceRot,MinForce2,MaxForceRot2,ForceNoRot,ForceRotPlusForce} public enum VehicleClass {Compacts,Sedans,SUVs,Coupes,Muscle,SportsClassics,Sports,Super,Motorcycles,OffRoad,Industrial,Utility,Vans,Cycles,Boats,Helicopters,Planes,Service,Emergency,Military,Commercial,Trains}
 public enum Bone {SkelRightHand,SkelLeftHand,PHRightHand} public class EntityBone {public int Index;} public class PedBone:EntityBone {} public class PedBoneCollection {public PedBone this[Bone b]=>new PedBone{Index=(int)b+1};}
 public class Entity {public Vector3 RightVector=>new Vector3(1,0,0);public bool CollisionEnabled=true;public bool RejectAttachments;public Entity AttachedTo;public void AttachTo(EntityBone b,Vector3 p,Vector3 r){if(!RejectAttachments)AttachedTo=BoneOwner;}public void AttachTo(Entity e,Vector3 p,Vector3 r){if(!RejectAttachments)AttachedTo=e;}public void Detach(){AttachedTo=null;}public static Entity BoneOwner;public Vector3 ForwardVector;public bool IsVisible=true;public int Forces;public Vector3 LastForce;public void ApplyForce(Vector3 d,Vector3 r,ForceType t){Forces++;LastForce=d;}public bool IsFireProof,IsExplosionProof;public bool IsOnScreen;public Vector3 Rotation;public float HeightAboveGround;public bool IsDead;static int next; public int Handle=++next; public bool Present=true; public bool Exists()=>Present; public Vector3 Position,Velocity; public float Heading; public bool IsPositionFrozen,IsInvincible,IsPersistent,Released;public void Delete(){Present=false;}}
 public enum Relationship {Neutral,Hate}
 public enum WeaponComponentHash { Clip02=101, AtArSupp=102, RifleClipExplosive=103, RifleBarrel=104 }
 public class WeaponComponent {public WeaponComponentHash ComponentHash;public bool Active;public string LocalizedName=>ComponentHash.ToString();public string DisplayName=>LocalizedName;}
 public class WeaponEntry {public System.Collections.Generic.List<WeaponComponent> Components=new System.Collections.Generic.List<WeaponComponent>();}
 public class Weapons {public bool IgnoreRepeatGrant,RejectAmmoWrites;public Dictionary<uint,uint> AmmoTypes=new Dictionary<uint,uint>();public int MaximumAmmo=240;public Dictionary<uint,int> Ammo=new Dictionary<uint,int>();public Dictionary<uint,WeaponEntry> Entries=new Dictionary<uint,WeaponEntry>();public WeaponEntry this[WeaponHash hash]{get{uint h=(uint)hash;if(!Entries.ContainsKey(h))Entries[h]=new WeaponEntry();return Entries[h];}}public int LastAmmo;public HashSet<WeaponHash> Owned=new HashSet<WeaponHash>();public void Give(WeaponHash w,int a,bool e,bool l){bool had=Owned.Contains(w);Owned.Add(w);LastAmmo=a;if(!had||!IgnoreRepeatGrant)Ammo[(uint)w]=a;}public void Remove(WeaponHash w){Owned.Remove(w);}}
 public class Ped:Entity {public PedBoneCollection Bones {get{Entity.BoneOwner=this;return new PedBoneCollection();}}public Model Model=new Model("ped");public bool IsCop;public int Health=300,MaxHealth=300;public bool CanSufferCriticalHits;public VehicleSeat SeatIndex {get {if(CurrentVehicle!=null)foreach(var p in CurrentVehicle.Seats)if(p.Value==this)return p.Key;return VehicleSeat.None;}}
  public bool DamageProof,DiesWhenInjured=true;public int MinStunGroundTime=-1;public uint LastWeaponHit;public bool IsRagdoll,IsCuffed,IsBeingStunned,CanBeDraggedOutOfVehicle;public DrivingStyle DrivingStyle; public void Kill(){IsDead=true;} public bool IsHeadtracking(Ped p)=>false;public bool IsAiming;public bool IsAlive=>!IsDead;public int Armor,Accuracy;public Weapons Weapons=new Weapons();public Blip AddBlip()=>new Blip();public void SetIntoVehicle(Vehicle v,VehicleSeat s){if(CurrentVehicle!=null)CurrentVehicle.Seats.Remove(SeatIndex);CurrentVehicle=v;Position=v.Position;v.Seats[s]=this;}
  public bool IsInCombat,IsShooting,AlwaysKeepTask,BlockPermanentEvents,Entering,Hostile,StuckInSeat,EjectOnTaskClear;
  public Vehicle CurrentVehicle; public int RelationshipGroup; public Ped CombatTarget;
  public Tasks Task; public Ped(){Task=new Tasks(this);} public bool IsInVehicle()=>CurrentVehicle!=null;
  public bool IsInVehicle(Vehicle v)=>v!=null&&CurrentVehicle==v;
  public Relationship GetRelationshipWithPed(Ped p)=>Hostile?Relationship.Hate:Relationship.Neutral;
 }
 public enum VehicleLockStatus {Unlocked,CannotEnter}
 public class FlyingHandlingData {public bool IsValid {get;set;}=true;public IntPtr MemoryAddress {get;set;}=new IntPtr(1234);public float ThrustFallOff {get;set;}=1f;public Vector3 VectorSpeedResistance {get;set;}=new Vector3(2,4,6);}
 public class BoatHandlingData {public bool IsValid {get;set;}=true;public IntPtr MemoryAddress {get;set;}=new IntPtr(2345);public float DragCoefficient {get;set;}=10f;}
 public class HandlingData {public float SteeringLock=35f;public float DeformationDamageMultiplier=1f,CollisionDamageMultiplier=1f,EngineDamageMultiplier=1f;public float TractionCurveMax=2.2f,TractionCurveMin=2f,TractionCurveLateral=22f,TractionLossMultiplier=1f,SuspensionCompressionDamping=1.5f,SuspensionReboundDamping=2f,BrakeForce=1f;public Vector3 CenterOfMassOffset=new Vector3(0,0,0.1f),InertiaMultiplier=new Vector3(1,1,1);public FlyingHandlingData FlyingHandlingData {get;set;}=new FlyingHandlingData();public BoatHandlingData BoatHandlingData {get;set;}=new BoatHandlingData();public static readonly Dictionary<string,HandlingData> Models=new Dictionary<string,HandlingData>();public static HandlingData GetByVehicleModel(Model model)=>Models.TryGetValue(model.Name,out var value)?value:null;static int next;public IntPtr MemoryAddress=new IntPtr(++next);public bool IsValid=true;public float InitialDriveMaxFlatVelocity=60,InitialDriveForce=.3f,DriveInertia=1f;}
 public enum VehicleModType {Hood=7,Ornaments=28,Suspension=15,AirFilter=40,Dashboard=29,Engine=11,VanityPlates=26,RightFender=9,Spoilers=0,RearBumper=2,SideSkirt=3,Frame=5,Brakes=12,SteeringWheels=33,Livery=48,DoorSpeakers=31,DialDesign=30,Plaques=35,Roof=10,FrontWheel=23,Exhaust=4,FrontBumper=1,Transmission=13,Trim=44,TrimDesign=27,Aerials=43,Tank=45,Struts=41,Fender=8,Speakers=36,ColumnShifterLevers=34,Armor=16,Trunk=37,RearWheel=24,Windows=46,PlateHolder=25,EngineBlock=39,Grille=6,Hydraulics=38,Horns=14,Seats=32,ArchCover=42} public class Mod {public int Index=-1,Count=3;public bool Custom;} public class ToggleMod {public bool IsInstalled;}
 public class Mods {readonly Dictionary<VehicleModType,Mod> values=new Dictionary<VehicleModType,Mod>();readonly Dictionary<VehicleToggleModType,ToggleMod> toggles=new Dictionary<VehicleToggleModType,ToggleMod>();
 public void InstallModKit(){}public Mod this[VehicleModType type]{get{if(!values.ContainsKey(type))values[type]=new Mod();return values[type];}}
 public ToggleMod this[VehicleToggleModType type]{get{if(!toggles.ContainsKey(type))toggles[type]=new ToggleMod();return toggles[type];}}
 public int WheelType,Livery=-1,LiveryCount=2;public VehicleWindowTint WindowTint;public LicensePlateStyle LicensePlateStyle;
 public VehicleColor PrimaryColor,SecondaryColor,PearlescentColor,RimColor,TrimColor,DashboardColor;
 Color primary,secondary;public Color CustomPrimaryColor {get=>primary;set{primary=value;IsPrimaryColorCustom=true;}}public Color CustomSecondaryColor {get=>secondary;set{secondary=value;IsSecondaryColorCustom=true;}}
 public Color NeonLightsColor=Color.White,TireSmokeColor=Color.White;public bool IsPrimaryColorCustom,IsSecondaryColorCustom,HasNeonLights=true;
 public void ClearCustomPrimaryColor(){IsPrimaryColorCustom=false;}public void ClearCustomSecondaryColor(){IsSecondaryColorCustom=false;}
 readonly HashSet<VehicleNeonLight> neon=new HashSet<VehicleNeonLight>();public bool HasNeonLight(VehicleNeonLight s)=>HasNeonLights;public bool IsNeonLightsOn(VehicleNeonLight s)=>neon.Contains(s);public void SetNeonLightsOn(VehicleNeonLight s,bool on){if(on)neon.Add(s);else neon.Remove(s);}}

 public class Prop:Entity {public Model Model=new Model("test");public Blip AddBlip()=>new Blip();} public class Vehicle:Entity {public int MaxHealth=1000,Health=1000;public Ped[] Occupants=>System.Linq.Enumerable.ToArray(Seats.Values);public VehicleClass ClassType=VehicleClass.Sedans;public bool IsOnAllWheels=true;public bool IsBulletProof;public float EngineTorqueMultiplier=1f;public bool IsInWater;public HashSet<int> Extras=new HashSet<int>();public bool ExtraExists(int id)=>id==1;public bool IsExtraOn(int id)=>Extras.Contains(id);public void ToggleExtra(int id,bool on){if(on)Extras.Add(id);else Extras.Remove(id);}public bool IsWanted;public HandlingData HandlingData=new HandlingData();public int Repairs;public void Repair(){Repairs++;}public void Wash(){}public void PlaceOnGround(){}public Model Model=new Model("car");public bool CanTiresBurst,AreLightsOn,IsInAir,IsSirenActive;public float CurrentRPM,ForwardSpeed;public int PassengerCapacity=>Capacity;public Vehicle(){ForwardVector=new Vector3(0,1,0);}public float BodyHealth=1000,EngineHealth=1000,EnginePowerMultiplier;public bool IsDriveable=true,IsEngineRunning;public Mods Mods=new Mods();public VehicleLockStatus LockStatus;public Ped GetPedOnSeat(VehicleSeat s)=>Seats.TryGetValue(s,out var p)?p:null;public Blip AddBlip()=>new Blip();public float Speed; public string DisplayName="test car"; public int Capacity=3; public Dictionary<VehicleSeat,Ped> Seats=new Dictionary<VehicleSeat,Ped>(); public bool IsSeatFree(VehicleSeat s)=>(int)s>=0&&(int)s<Capacity&&!Seats.ContainsKey(s);}
 public enum VehicleSeat {None=-3,Any=-2,Driver=-1,Passenger=0,RightFront=0,LeftRear=1,RightRear=2}
 public enum EnterVehicleFlags {None}
 public enum DrivingStyle {Rushed,Normal,AvoidTraffic,AvoidTrafficExtremely} [System.Flags] public enum AnimationFlags {None=0,Loop=1,UpperBodyOnly=16}
 public enum LeaveVehicleFlags {None=0,WarpOut=16}
 public enum VehicleColor {MetallicBlack}
 public enum ExplosionType {Grenade,Extinguisher,Boat,Tanker,Plane}
 public enum HeliMissionFlags {None} public enum VehicleMissionType {GoTo,Circle,Attack,Flee} [System.Flags] public enum VehicleDrivingFlags {None=0,DrivingModeAvoidVehiclesReckless=1,AllowGoingWrongWay=2,UseShortCutLinks=4} public enum BoatMissionFlags {None}
 public class Tasks {public int VehicleMissions;public Vector3 LastMissionPoint;public void StartVehicleMission(Vehicle v,Ped p,VehicleMissionType t,float s,VehicleDrivingFlags f,float r,float straight,bool wrong){VehicleMissions++;}public void StartVehicleMission(Vehicle v,Vector3 p,VehicleMissionType t,float s,VehicleDrivingFlags f,float r,float straight,bool wrong){VehicleMissions++;LastMissionPoint=p;}public void StartHeliMission(Vehicle v,Ped p,VehicleMissionType t,float s,float r,int h,int min,float a,float slow,HeliMissionFlags f){HeliTasks++;}public int Wanders,Chases;public void WanderAround(Vector3 p,float r){Wanders++;}public void VehicleChase(Ped p){Chases++;}public int VehicleShots;public void VehicleShootAtPed(Ped p){VehicleShots++;} public void RunTo(Vector3 p,bool a,int t){}public void StartBoatMission(Vehicle v,Vehicle target,VehicleMissionType t,float s,VehicleDrivingFlags f,float r,BoatMissionFlags b){BoatTasks++;} public int HeliTasks;public void StartHeliMission(Vehicle v,Vector3 p,VehicleMissionType t,float s,float r,int h,int min,float a,float slow,HeliMissionFlags f){HeliTasks++;}public void StartPlaneMission(Vehicle v,Vector3 p,VehicleMissionType t,float s,float r,int h,int min,float a,bool b){}public int BoatTasks;public void StartBoatMission(Vehicle v,Vector3 p,VehicleMissionType t,float s,VehicleDrivingFlags f,float r,BoatMissionFlags b){BoatTasks++;LastMissionPoint=p;}public float LastDriveSpeed;public Vector3 LastDrivePoint;public int Drives,Gotos;public void DriveTo(Vehicle v,Vector3 p,float r,float s,DrivingStyle d){Drives++;LastDrivePoint=p;LastDriveSpeed=s;}public void LeaveVehicle(LeaveVehicleFlags f=LeaveVehicleFlags.None){if(owner.StuckInSeat)return;if(owner.CurrentVehicle!=null)owner.CurrentVehicle.Seats.Remove(owner.SeatIndex);owner.CurrentVehicle=null;}public void GoTo(Vector3 p){Gotos++;}public int Phones,Looks;public void UseMobilePhone(int ms){Phones++;}public void LookAt(Entity e,int ms){Looks++;}public void HandsUp(int t){}public void ChaseWithHelicopter(Ped p,Vector3 v){}public int Aims,Scenarios;public void AimAt(Ped p,int t){Aims++;}public float ScenarioHeading;public void StartScenario(string s,Vector3 p,float h){Scenarios++;ScenarioHeading=h;}public int Cruises;public void CruiseWithVehicle(Vehicle v,float f,DrivingStyle d){Cruises++;}public int HatedFights;public void FightAgainstHatedTargets(float r){HatedFights++;}
  readonly Ped owner; public int Clears,Enters,Warps,Fights; public VehicleSeat LastSeat; public Ped LastTarget;
  public Tasks(Ped p){owner=p;} public void StandStill(int t){} public void ClearAll(){Clears++;} public void ClearAllImmediately(){Clears++;if(owner.EjectOnTaskClear)LeaveVehicle();}public int Animations,Rappels;public void PlayAnimation(string d,string n,float a,float b,int t,AnimationFlags f,float r){Animations++;}public int Flees;public void FleeFrom(Ped p){Flees++;}public void RappelFromHelicopter(){Rappels++;if(owner.CurrentVehicle!=null){owner.CurrentVehicle.Seats.Remove(owner.SeatIndex);owner.CurrentVehicle=null;}}
  public void FollowToOffsetFromEntity(Ped p,Vector3 v,float s,int t,float r,bool b){} public int Guards;public void GuardCurrentPosition(){Guards++;}
  public void FightAgainst(Ped p){Fights++;LastTarget=p;owner.IsInCombat=true;}
  public void EnterVehicle(Vehicle v,VehicleSeat s,int t=-1,float f=1f,EnterVehicleFlags flags=0){Enters++;LastSeat=s;owner.Entering=true;}
  public void WarpIntoVehicle(Vehicle v,VehicleSeat s){Warps++;LastSeat=s;owner.CurrentVehicle=v;v.Seats[s]=owner;}
 }
 public enum BlipSprite {Tank,PoliceCarDot,PoliceHelicopter,Safehouse,Helicopter,Plane,Boat,Standard,Enemy,PersonalVehicleCar,ArmoredTruck} public enum BlipColor {White,Yellow,Blue,Green,Orange,Purple,Red}
 public class Blip:Entity {public float Scale;public bool IsFriendly;public BlipSprite Sprite;public BlipColor Color;public bool IsShortRange,ShowRoute;public string Name;}
 public struct RaycastResult{public bool DidHit;public Vector3 HitPosition;} [Flags]public enum IntersectFlags{None=0,Map=1,Objects=32}
 public enum Weather{Clear,ThunderStorm}
 public class ParticleEffectAsset {public ParticleEffectAsset(string name){} public bool Request(int timeout){return !World.FailSmoke;} public void MarkAsNoLongerNeeded(){} }
 public static class World {public static bool FailSmoke;public static int SmokeBursts;public static bool CreateParticleEffectNonLooped(ParticleEffectAsset asset,string name,Vector3 p,Vector3 rot,float scale){if(FailSmoke)return false;SmokeBursts++;return true;}public static Weather Weather=Weather.Clear;public static Func<Vector3,Vector3,RaycastResult> RaycastHandler;public static RaycastResult Raycast(Vector3 s,Vector3 t,IntersectFlags f,Entity e=null)=>RaycastHandler==null ?
   (s.X==t.X&&s.Y==t.Y&&System.Math.Abs(s.Z-t.Z-3f)<.01f ? new RaycastResult{DidHit=true,HitPosition=new Vector3(s.X,s.Y,(s.Z+t.Z)/2f)} : new RaycastResult()) : RaycastHandler(s,t);public static Vehicle[] GetAllVehicles()=>Vehicles.ToArray();public static Prop[] NearbyProps=new Prop[0];public static Prop[] GetNearbyProps(Vector3 p,float r)=>NearbyProps;public static Vehicle[] NearbyVehicles=new Vehicle[0];public static Vehicle[] GetNearbyVehicles(Vector3 p,float r)=>NearbyVehicles;public static List<Prop> Props=new List<Prop>();public static string FailPropModel;public static Prop CreateProp(Model m,Vector3 p,bool a,bool b){if(m.Name==FailPropModel)return null;var prop=new Prop{Position=p,Model=m};Props.Add(prop);return prop;}public static void AddExplosion(Vector3 v,ExplosionType t,float p,float q,Ped owner,bool a,bool b){}public static Camera RenderingCamera; public static List<Ped> Created=new List<Ped>();public static bool FailCamera;
  public static bool WaterAvailable=true;public static bool FailNavigation,FailPeds;public static List<Vehicle> Vehicles=new List<Vehicle>();
  public static DateTime CurrentDate=new DateTime(2026,9,12);public static TimeSpan CurrentTimeOfDay=TimeSpan.FromHours(13);public static Vector3 GetNextPositionOnStreet(Vector3 p)=>p;public static float GroundHeight;public static float GetGroundHeight(Vector3 p)=>GroundHeight;
  public static Func<Vector3,Vector3> SafeCoordHandler;public static Vector3? FailNavigationNear;public static Vector3 GetSafeCoordForPed(Vector3 p,bool sidewalk,int flags)=>SafeCoordHandler!=null?SafeCoordHandler(p):FailNavigation||(FailNavigationNear.HasValue&&p.DistanceTo(FailNavigationNear.Value)<30f)?Vector3.Zero:p;
  public static int AddRelationshipGroup(string s)=>1;
  public static bool FailVehicles;public static Vehicle CreateVehicle(Model m,Vector3 p,float h){if(FailVehicles)return null;var v=new Vehicle{Position=p,Heading=h,Model=m};Vehicles.Add(v);return v;}
  public static Camera CreateCamera(Vector3 p,Vector3 r,float f)=>FailCamera?null:new Camera{Position=p};public static Ped CreatePed(Model m,Vector3 p,float h){if(FailPeds)return null;var a=new Ped{Position=p,Heading=h,Model=m};Created.Add(a);return a;}
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
 public enum Hash : ulong { GET_TIMECYCLE_MODIFIER_INDEX, GET_TIMECYCLE_TRANSITION_MODIFIER_INDEX, SET_PED_DIES_WHEN_INJURED, SET_PED_MIN_GROUND_TIME_FOR_STUNGUN, SET_ENTITY_PROOFS, DISABLE_CONTROL_ACTION, SET_VEHICLE_CAN_BE_TARGETTED, GET_VEHICLE_ESTIMATED_MAX_SPEED, GET_VEHICLE_ACCELERATION, GET_VEHICLE_MAX_BRAKING, GET_VEHICLE_MAX_TRACTION, GET_WEAPON_HUD_STATS, GET_WEAPON_COMPONENT_HUD_STATS, SET_BOAT_ANCHOR, CLEAR_ENTITY_LAST_DAMAGE_ENTITY, HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY, GET_MAX_WANTED_LEVEL, CLEAR_AREA_OF_PROJECTILES, SET_PED_SUFFERS_CRITICAL_HITS, HAS_PED_BEEN_DAMAGED_BY_WEAPON, SET_ENABLE_HANDCUFFS, SET_ENTITY_COLLISION, SET_ENTITY_COORDS_NO_OFFSET, GET_PED_AMMO_TYPE_FROM_WEAPON, GET_VEHICLE_MODEL_NUMBER_OF_SEATS, HAS_WEAPON_ASSET_LOADED,GET_DEEP_OCEAN_SCALER,DOES_WEAPON_TAKE_WEAPON_COMPONENT,HAS_PED_GOT_WEAPON_COMPONENT,GIVE_WEAPON_COMPONENT_TO_PED,GET_MAX_AMMO,SMASH_VEHICLE_WINDOW,SET_PED_TO_RAGDOLL, CALCULATE_TRAVEL_DISTANCE_BETWEEN_POINTS,TASK_VEHICLE_CHASE, IS_INTERIOR_ENTITY_SET_ACTIVE, DOES_EXTRA_EXIST,GET_IS_VEHICLE_PRIMARY_COLOUR_CUSTOM,GET_IS_VEHICLE_SECONDARY_COLOUR_CUSTOM,GET_VEHICLE_CUSTOM_PRIMARY_COLOUR,GET_VEHICLE_CUSTOM_SECONDARY_COLOUR,GET_VEHICLE_EXTRA_COLOURS,GET_VEHICLE_NEON_COLOUR,GET_VEHICLE_NUMBER_PLATE_TEXT_INDEX,GET_VEHICLE_TYRE_SMOKE_COLOR,IS_TOGGLE_MOD_ON,IS_VEHICLE_EXTRA_TURNED_ON,GET_VEHICLE_NEON_ENABLED,SET_VEHICLE_CUSTOM_PRIMARY_COLOUR,SET_VEHICLE_CUSTOM_SECONDARY_COLOUR,SET_VEHICLE_EXTRA,SET_VEHICLE_EXTRA_COLOURS,SET_VEHICLE_NEON_COLOUR,SET_VEHICLE_NEON_ENABLED,SET_VEHICLE_NUMBER_PLATE_TEXT_INDEX,SET_VEHICLE_TYRE_SMOKE_COLOR,TOGGLE_VEHICLE_MOD, PLACE_OBJECT_ON_GROUND_PROPERLY,IS_AIM_CAM_SNIPER_RUNNING,STOP_ANIM_TASK,REMOVE_ANIM_DICT,HAS_ANIM_DICT_LOADED,TASK_PLAY_ANIM,REQUEST_ANIM_DICT, GET_VEHICLE_NUMBER_PLATE_TEXT,SET_VEHICLE_NUMBER_PLATE_TEXT,GET_VEHICLE_WINDOW_TINT,SET_VEHICLE_WINDOW_TINT,TASK_USE_MOBILE_PHONE,HAS_ENTITY_COLLIDED_WITH_ANYTHING,GET_COLLISION_NORMAL_OF_LAST_HIT_FOR_ENTITY,GET_OFFSET_FROM_ENTITY_GIVEN_WORLD_COORDS,SET_VEHICLE_DAMAGE, IS_ENTITY_UPSIDEDOWN,CREATE_PICKUP_ROTATE,SET_ARTIFICIAL_LIGHTS_STATE,SET_PED_FLEE_ATTRIBUTES,GET_CLOSEST_VEHICLE_NODE_WITH_HEADING,GET_PED_RELATIONSHIP_GROUP_HASH,IS_ENTITY_ATTACHED_TO_ENTITY,GET_FILENAME_FOR_AUDIO_CONVERSATION,GET_MOD_TEXT_LABEL,GET_NUM_VEHICLE_MODS,GET_VEHICLE_MOD,GET_VEHICLE_MODEL_ESTIMATED_MAX_SPEED,GET_VEHICLE_MOD_VARIATION,GET_VEHICLE_WHEEL_TYPE,GET_VEHICLE_XENON_LIGHT_COLOR_INDEX,IS_ENTITY_IN_WATER,SET_VEHICLE_MOD,SET_VEHICLE_WHEEL_TYPE,SET_VEHICLE_XENON_LIGHT_COLOR_INDEX, SET_VEHICLE_CHEAT_POWER_INCREASE, SET_TASK_VEHICLE_CHASE_BEHAVIOR_FLAG,SET_TASK_VEHICLE_CHASE_IDEAL_PURSUIT_DISTANCE,ADD_AMMO_TO_PED,DOOR_SYSTEM_FIND_EXISTING_DOOR,DOOR_SYSTEM_GET_DOOR_STATE,DOOR_SYSTEM_GET_OPEN_RATIO,DOOR_SYSTEM_SET_DOOR_STATE,DOOR_SYSTEM_SET_OPEN_RATIO,GET_PED_TARGET_FROM_COMBAT_PED,GET_PED_TYPE,GET_SELECTED_PED_WEAPON,GET_STATE_OF_CLOSEST_DOOR_OF_TYPE,GET_WEAPONTYPE_GROUP,SET_CONTROL_VALUE_NEXT_FRAME,SET_DISPATCH_TIME_BETWEEN_SPAWN_ATTEMPTS_MULTIPLIER,SET_PED_COMBAT_MOVEMENT,SET_PED_COMBAT_RANGE,SET_PED_MOVE_RATE_OVERRIDE,SET_RUN_SPRINT_MULTIPLIER_FOR_PLAYER,SET_STATE_OF_CLOSEST_DOOR_OF_TYPE,SET_VEHICLE_IS_WANTED,SET_VEHICLE_MAX_SPEED,Unarmed, GET_AMMO_IN_PED_WEAPON,SET_PED_AMMO, GET_VEHICLE_TRAILER_VEHICLE,ATTACH_VEHICLE_TO_TRAILER,IS_VEHICLE_ATTACHED_TO_TRAILER,CLEAR_ROOM_FOR_ENTITY,SET_CAN_ATTACK_FRIENDLY,SET_DRIVE_TASK_CRUISE_SPEED,TASK_DRIVE_BY,HAS_ENTITY_CLEAR_LOS_TO_ENTITY,SET_CURRENT_PED_VEHICLE_WEAPON,IS_HUD_HIDDEN,REQUEST_STREAMED_TEXTURE_DICT,HAS_STREAMED_TEXTURE_DICT_LOADED,HIDE_HUD_COMPONENT_THIS_FRAME,GET_SAFE_ZONE_SIZE,GET_ASPECT_RATIO,ARE_PLAYER_STARS_GREYED_OUT,DRAW_SPRITE,IS_IPL_ACTIVE,REQUEST_IPL,DISABLE_ALL_CONTROL_ACTIONS,GET_INTERIOR_AT_COORDS,PIN_INTERIOR_IN_MEMORY,REFRESH_INTERIOR,IS_INTERIOR_READY,GET_INTERIOR_FROM_ENTITY,UNPIN_INTERIOR,REMOVE_IPL,GET_NUM_PED_HAIR_TINTS,GET_PED_HEAD_OVERLAY_NUM,SET_PED_HEAD_OVERLAY,SET_PED_HEAD_OVERLAY_TINT,GET_PED_DRAWABLE_VARIATION,GET_NUMBER_OF_PED_PROP_DRAWABLE_VARIATIONS,GET_NUMBER_OF_PED_PROP_TEXTURE_VARIATIONS,CLEAR_PED_PROP,SET_PED_PROP_INDEX,ADD_TO_CLOCK_TIME,HAS_PED_GOT_WEAPON,IS_WEAPON_VALID,SET_NEW_WAYPOINT,IS_POSITION_OCCUPIED,SET_VEHICLE_SHOOT_AT_TARGET,SET_HELI_BLADES_FULL_SPEED,SET_RELATIONSHIP_BETWEEN_GROUPS,SET_DRIVER_ABILITY,SET_DRIVER_AGGRESSIVENESS,SET_POLICE_IGNORE_PLAYER,SET_EVERYONE_IGNORE_PLAYER,SET_PLAYER_WANTED_LEVEL_NOW,GIVE_WEAPON_TO_PED,SET_VEHICLE_LIGHTS,SET_MAX_WANTED_LEVEL,SET_PED_COMBAT_ABILITY,SET_PED_COMBAT_ATTRIBUTES,ATTACH_ENTITY_TO_ENTITY,DETACH_ENTITY,SET_PED_INTO_VEHICLE,CAN_PED_SEE_HATED_PED,IS_SPHERE_VISIBLE,GET_WATER_HEIGHT,REQUEST_WEAPON_ASSET,REMOVE_WEAPON_ASSET,SHOOT_SINGLE_BULLET_BETWEEN_COORDS,SET_VEHICLE_FORWARD_SPEED,TASK_VEHICLE_TEMP_ACTION,TASK_VEHICLE_FOLLOW,TASK_VEHICLE_SHOOT_AT_PED,SET_VEHICLE_UNDRIVEABLE,SET_VEHICLE_DOOR_OPEN,SET_VEHICLE_DOOR_SHUT,START_SCRIPT_FIRE,REMOVE_SCRIPT_FIRE,SET_PED_DEFAULT_COMPONENT_VARIATION,CLEAR_ALL_PED_PROPS,SET_PED_HEAD_BLEND_DATA,SET_PED_HAIR_TINT,GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS,GET_NUMBER_OF_PED_TEXTURE_VARIATIONS,SET_PED_COMPONENT_VARIATION,IS_DISABLED_CONTROL_PRESSED,IS_DISABLED_CONTROL_JUST_PRESSED,GET_DISABLED_CONTROL_NORMAL,TASK_LEAVE_VEHICLE,IS_PLAYER_FREE_AIMING_AT_ENTITY,CHANGE_PLAYER_PED,IS_PLAYER_SWITCH_IN_PROGRESS,STOP_PLAYER_SWITCH,SET_FOCUS_POS_AND_VEL,CLEAR_FOCUS,HIDE_HUD_AND_RADAR_THIS_FRAME,IS_PED_IN_COMBAT,IS_PED_GETTING_INTO_A_VEHICLE,GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS,REQUEST_COLLISION_AT_COORD,HAS_COLLISION_LOADED_AROUND_ENTITY,IS_PLAYER_BEING_ARRESTED,SET_CAM_DEATH_FAIL_EFFECT_STATE,RESET_PLAYER_ARREST_STATE,FORCE_GAME_STATE_PLAYING,PAUSE_DEATH_ARREST_RESTART,IGNORE_NEXT_RESTART,SET_FADE_OUT_AFTER_DEATH,SET_FADE_OUT_AFTER_ARREST,SET_FADE_IN_AFTER_DEATH_ARREST,SET_VEHICLE_ENVEFF_SCALE,CLEAR_PED_TASKS_IMMEDIATELY,SET_TIME_SCALE,ANIMPOSTFX_PLAY,ANIMPOSTFX_STOP,SET_VEHICLE_REDUCE_GRIP,SET_PED_KEEP_TASK,SET_VEHICLE_DAMAGE_SCALE,SET_PLAYER_VEHICLE_DAMAGE_MODIFIER,CASCADE_SHADOWS_SET_CASCADE_BOUNDS_SCALE,SET_VEHICLE_HEADLIGHT_SHADOWS,OVERRIDE_LODSCALE_THIS_FRAME,SET_VEHICLE_LOD_MULTIPLIER,SET_PED_LOD_MULTIPLIER,SET_GAMEPLAY_CAM_MOTION_BLUR_SCALING_THIS_UPDATE,SET_DISTANCE_BLUR_STRENGTH_OVERRIDE,SET_ENTITY_USE_MAX_DISTANCE_FOR_WATER_REFLECTION,SET_DEEP_OCEAN_SCALER,RESET_DEEP_OCEAN_SCALER,SET_TIMECYCLE_MODIFIER,SET_TIMECYCLE_MODIFIER_STRENGTH,CLEAR_TIMECYCLE_MODIFIER,GET_GROUND_Z_FOR_3D_COORD,IS_INTERIOR_DISABLED,DISABLE_INTERIOR,IS_INTERIOR_CAPPED,CAP_INTERIOR,DRAW_RECT,REPORT_POLICE_SPOTTED_PLAYER,SET_PLAYER_WANTED_CENTRE_POSITION,ACTIVATE_INTERIOR_ENTITY_SET,DEACTIVATE_INTERIOR_ENTITY_SET,IS_PAUSE_MENU_ACTIVE,PREPARE_MUSIC_EVENT,TRIGGER_MUSIC_EVENT,CANCEL_MUSIC_EVENT,GET_WATER_HEIGHT_NO_WAVES,START_EXPENSIVE_SYNCHRONOUS_SHAPE_TEST_LOS_PROBE,GET_SHAPE_TEST_RESULT,DRAW_MARKER,ARE_PLAYER_FLASHING_STARS_ABOUT_TO_DROP,IS_WANTED_AND_HAS_BEEN_SEEN_BY_COPS}
 public class OutputArgument {public object Value;public ulong? NativeBoolStorage;public T GetResult<T>(){if(NativeBoolStorage.HasValue){if(typeof(T)==typeof(bool))return (T)(object)(NativeBoolStorage.Value!=0);if(typeof(T)==typeof(int))return (T)(object)unchecked((int)NativeBoolStorage.Value);}if(typeof(T)==typeof(int)&&Value is bool)return (T)(object)((bool)Value?1:0);return Value==null?default(T):(T)Value;}}
 public static class Function {
  public static int TimecycleIndex=-1, TimecycleTransition=-1;
  public static string TimecycleName;
  public static float TimecycleStrength;
  public static HashSet<string> RejectedTimecycles=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  public static void ResetTimecycle(){TimecycleIndex=TimecycleTransition=-1;TimecycleName=null;TimecycleStrength=0;RejectedTimecycles.Clear();}

  public static Dictionary<int,Vehicle> Trailers=new Dictionary<int,Vehicle>(); public static Dictionary<Control,bool> Held=new Dictionary<Control,bool>();public static Dictionary<Control,float> Axes=new Dictionary<Control,float>();
  public static bool WantedSeen=true, StarsFlashing, ScoreAvailable=true, Paused, MarineBlocked;
  public static Func<Vector3, float> MarineFloor; public static Func<Vector3, Vector3, bool> MarineObstruction;
  private static object[] LastRay;
  public static int Sprites, TankShots, DriveBys;public static uint WeaponGroup;public static bool ClearLos=true;public static bool InteriorReady=true;public static int InteriorId=123;public static int? EntityInterior;public static bool IplReady=true;public static bool InteriorDisabled,InteriorCapped,StarsGreyed; public static bool ResetVitalsOnSwitch;public static bool EjectOnSwitch;public static Hash? ThrowOnce;public static float Seabed=-40f;public static bool SeabedKnown=true; public static Dictionary<Hash,object> Values=new Dictionary<Hash,object>();
  public static readonly List<Tuple<Hash,object[]>> Calls=new List<Tuple<Hash,object[]>>();public static void Call(Hash h,params object[] args){
   if(h==Hash.SET_TIMECYCLE_MODIFIER&&!RejectedTimecycles.Contains((string)args[0])){TimecycleName=(string)args[0];TimecycleIndex=Game.GenerateHash(TimecycleName)&0x7fffffff;}
   if(h==Hash.SET_TIMECYCLE_MODIFIER_STRENGTH)TimecycleStrength=(float)args[0];
   if(h==Hash.CLEAR_TIMECYCLE_MODIFIER){TimecycleIndex=-1;TimecycleName=null;TimecycleStrength=0;}
Calls.Add(Tuple.Create(h,args));
   if(args.Length>0&&args[0] is Vehicle cv){
    if(h==Hash.GET_VEHICLE_NEON_COLOUR||h==Hash.GET_VEHICLE_TYRE_SMOKE_COLOR||h==Hash.GET_VEHICLE_CUSTOM_PRIMARY_COLOUR||h==Hash.GET_VEHICLE_CUSTOM_SECONDARY_COLOUR){
     Color c=h==Hash.GET_VEHICLE_NEON_COLOUR?cv.Mods.NeonLightsColor:h==Hash.GET_VEHICLE_TYRE_SMOKE_COLOR?cv.Mods.TireSmokeColor:h==Hash.GET_VEHICLE_CUSTOM_PRIMARY_COLOUR?cv.Mods.CustomPrimaryColor:cv.Mods.CustomSecondaryColor;
     ((OutputArgument)args[1]).Value=(int)c.R;((OutputArgument)args[2]).Value=(int)c.G;((OutputArgument)args[3]).Value=(int)c.B;}
    if(h==Hash.SET_VEHICLE_NEON_COLOUR)cv.Mods.NeonLightsColor=Color.FromArgb((int)args[1],(int)args[2],(int)args[3]);
    if(h==Hash.SET_VEHICLE_CUSTOM_PRIMARY_COLOUR)cv.Mods.CustomPrimaryColor=Color.FromArgb((int)args[1],(int)args[2],(int)args[3]);
    if(h==Hash.SET_VEHICLE_CUSTOM_SECONDARY_COLOUR)cv.Mods.CustomSecondaryColor=Color.FromArgb((int)args[1],(int)args[2],(int)args[3]);
    if(h==Hash.SET_VEHICLE_WINDOW_TINT)cv.Mods.WindowTint=(VehicleWindowTint)(int)args[1];
    if(h==Hash.SET_VEHICLE_NUMBER_PLATE_TEXT_INDEX)cv.Mods.LicensePlateStyle=(LicensePlateStyle)(int)args[1];
    if(h==Hash.TOGGLE_VEHICLE_MOD)cv.Mods[(VehicleToggleModType)(int)args[1]].IsInstalled=(bool)args[2];
    if(h==Hash.SET_VEHICLE_NEON_ENABLED)cv.Mods.SetNeonLightsOn((VehicleNeonLight)(int)args[1],(bool)args[2]);
    if(h==Hash.SET_VEHICLE_EXTRA)cv.ToggleExtra((int)args[1],!(bool)args[2]);
   }

   if(h==Hash.SET_PED_DIES_WHEN_INJURED)((Ped)args[0]).DiesWhenInjured=(bool)args[1];
   if(h==Hash.SET_PED_MIN_GROUND_TIME_FOR_STUNGUN)((Ped)args[0]).MinStunGroundTime=(int)args[1];
   if(h==Hash.SET_ENTITY_PROOFS)((Ped)args[0]).DamageProof=(bool)args[1];
   if(h==Hash.SET_PED_AMMO){var w=((Ped)args[0]).Weapons;if(!w.RejectAmmoWrites){uint weapon=(uint)args[1];uint pool=w.AmmoTypes.TryGetValue(weapon,out var t)?t:weapon;foreach(var owned in w.Owned){uint type=w.AmmoTypes.TryGetValue((uint)owned,out var mt)?mt:(uint)owned;if(type==pool)w.Ammo[(uint)owned]=(int)args[2];}}}
   if(h==Hash.ADD_AMMO_TO_PED){var ammo=((Ped)args[0]).Weapons.Ammo;uint w=(uint)args[1];ammo[w]=(ammo.TryGetValue(w,out int before)?before:17)+(int)args[2];}
   if(h==Hash.GIVE_WEAPON_COMPONENT_TO_PED){var part=((Ped)args[0]).Weapons[(WeaponHash)(uint)args[1]].Components.FirstOrDefault(p=>(uint)p.ComponentHash==(uint)args[2]);if(part!=null)part.Active=true;}

   if(h==Hash.GIVE_WEAPON_TO_PED)((Ped)args[0]).Weapons.Give((WeaponHash)unchecked((uint)Convert.ToInt64(args[1])),(int)args[2],false,false);
   if(h==Hash.SET_ENABLE_HANDCUFFS)((Ped)args[0]).IsCuffed=(bool)args[1];
   if(h==Hash.SET_ENTITY_COLLISION)((Entity)args[0]).CollisionEnabled=(bool)args[1];
   if(h==Hash.SET_ENTITY_COORDS_NO_OFFSET)((Entity)args[0]).Position=new Vector3((float)args[1],(float)args[2],(float)args[3]);
   if(h==Hash.ATTACH_ENTITY_TO_ENTITY && !((Entity)args[0]).RejectAttachments)((Entity)args[0]).AttachedTo=(Entity)args[1];
   if(h==Hash.DETACH_ENTITY)((Entity)args[0]).AttachedTo=null;
   if(h==Hash.SET_VEHICLE_WHEEL_TYPE)((Vehicle)args[0]).Mods.WheelType=(int)args[1];
   if(h==Hash.SET_VEHICLE_MOD){var mod=((Vehicle)args[0]).Mods[(VehicleModType)(int)args[1]];mod.Index=(int)args[2];mod.Custom=(bool)args[3];}
if(h==Hash.SET_VEHICLE_IS_WANTED)((Vehicle)args[0]).IsWanted=(bool)args[1];if(ThrowOnce==h){ThrowOnce=null;throw new InvalidOperationException("Injected native failure");}if(h==Hash.ATTACH_VEHICLE_TO_TRAILER)Trailers[((Vehicle)args[0]).Handle]=(Vehicle)args[1];if(h==Hash.TASK_LEAVE_VEHICLE&&!((Ped)args[0]).StuckInSeat)((Ped)args[0]).CurrentVehicle=null;if(h==Hash.DRAW_SPRITE)Sprites++;if(h==Hash.SET_VEHICLE_SHOOT_AT_TARGET)TankShots++;if(h==Hash.TASK_DRIVE_BY)DriveBys++;if(args.Length>0)Values[h]=args[0];if(h==Hash.RESET_PLAYER_ARREST_STATE)Game.Arrested=false;if(h==Hash.CHANGE_PLAYER_PED){var old=Game.Player.Character;Game.Player.Character=(Ped)args[1];if(ResetVitalsOnSwitch){old.Health=200;old.Armor=0;old.MaxHealth=200;Game.Player.Character.Health=200;Game.Player.Character.Armor=0;Game.Player.Character.MaxHealth=200;}if(EjectOnSwitch)Game.Player.Character.Task.LeaveVehicle();}}
  public static System.Collections.Generic.HashSet<int> TestDamage = new System.Collections.Generic.HashSet<int>();
  public static T Call<T>(Hash h,params object[] args){
   if(h==Hash.GET_TIMECYCLE_MODIFIER_INDEX)return (T)(object)TimecycleIndex;
   if(h==Hash.GET_TIMECYCLE_TRANSITION_MODIFIER_INDEX)return (T)(object)TimecycleTransition;
   if(h==Hash.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY)return (T)(object)TestDamage.Contains(((GTA.Entity)args[0]).Handle);

   if(h==Hash.HAS_PED_BEEN_DAMAGED_BY_WEAPON)return (T)(object)(((Ped)args[0]).LastWeaponHit==(uint)args[1]);
   if(h==Hash.GET_MAX_WANTED_LEVEL)return (T)(object)(Values.TryGetValue(Hash.SET_MAX_WANTED_LEVEL,out var limit)?Convert.ToInt32(limit):5);

   if(args.Length>0&&args[0] is Vehicle cv){
    if(h==Hash.GET_IS_VEHICLE_PRIMARY_COLOUR_CUSTOM)return (T)(object)cv.Mods.IsPrimaryColorCustom;
    if(h==Hash.GET_IS_VEHICLE_SECONDARY_COLOUR_CUSTOM)return (T)(object)cv.Mods.IsSecondaryColorCustom;
    if(h==Hash.GET_VEHICLE_WINDOW_TINT)return (T)(object)(int)cv.Mods.WindowTint;
    if(h==Hash.GET_VEHICLE_NUMBER_PLATE_TEXT_INDEX)return (T)(object)(int)cv.Mods.LicensePlateStyle;
    if(h==Hash.IS_TOGGLE_MOD_ON)return (T)(object)cv.Mods[(VehicleToggleModType)(int)args[1]].IsInstalled;
    if(h==Hash.GET_VEHICLE_NEON_ENABLED)return (T)(object)cv.Mods.IsNeonLightsOn((VehicleNeonLight)(int)args[1]);
    if(h==Hash.DOES_EXTRA_EXIST)return (T)(object)cv.ExtraExists((int)args[1]);
    if(h==Hash.IS_VEHICLE_EXTRA_TURNED_ON)return (T)(object)cv.IsExtraOn((int)args[1]);
   }
if(h==Hash.IS_INTERIOR_ENTITY_SET_ACTIVE)return (T)(object)(Values.TryGetValue(h,out var setActive)&&(bool)setActive);if(h==Hash.HAS_ENTITY_COLLIDED_WITH_ANYTHING)return (T)(object)(Values.TryGetValue(h,out var collision)&& (bool)collision);if(h==Hash.GET_COLLISION_NORMAL_OF_LAST_HIT_FOR_ENTITY||h==Hash.GET_OFFSET_FROM_ENTITY_GIVEN_WORLD_COORDS)return (T)(Values.TryGetValue(h,out var vector)?vector:(object)Vector3.Zero);
   if(h==Hash.PREPARE_MUSIC_EVENT||h==Hash.TRIGGER_MUSIC_EVENT||h==Hash.CANCEL_MUSIC_EVENT){Calls.Add(Tuple.Create(h,args));
   if(h==Hash.SET_PED_DIES_WHEN_INJURED)((Ped)args[0]).DiesWhenInjured=(bool)args[1];
   if(h==Hash.SET_PED_MIN_GROUND_TIME_FOR_STUNGUN)((Ped)args[0]).MinStunGroundTime=(int)args[1];
   if(h==Hash.SET_ENTITY_PROOFS)((Ped)args[0]).DamageProof=(bool)args[1];
   if(h==Hash.SET_PED_AMMO){var w=((Ped)args[0]).Weapons;if(!w.RejectAmmoWrites){uint weapon=(uint)args[1];uint pool=w.AmmoTypes.TryGetValue(weapon,out var t)?t:weapon;foreach(var owned in w.Owned){uint type=w.AmmoTypes.TryGetValue((uint)owned,out var mt)?mt:(uint)owned;if(type==pool)w.Ammo[(uint)owned]=(int)args[2];}}}
   if(h==Hash.ADD_AMMO_TO_PED){var ammo=((Ped)args[0]).Weapons.Ammo;uint w=(uint)args[1];ammo[w]=(ammo.TryGetValue(w,out int before)?before:17)+(int)args[2];}
   if(h==Hash.GIVE_WEAPON_COMPONENT_TO_PED){var part=((Ped)args[0]).Weapons[(WeaponHash)(uint)args[1]].Components.FirstOrDefault(p=>(uint)p.ComponentHash==(uint)args[2]);if(part!=null)part.Active=true;}
if(ThrowOnce==h){ThrowOnce=null;throw new InvalidOperationException("Injected audio failure");}return (T)(object)ScoreAvailable;}
   if(h==Hash.CALCULATE_TRAVEL_DISTANCE_BETWEEN_POINTS)return (T)(object)(-1f);
   if(h==Hash.IS_PAUSE_MENU_ACTIVE)return (T)(object)Paused;
   if(h==Hash.IS_WANTED_AND_HAS_BEEN_SEEN_BY_COPS)return (T)(object)WantedSeen;
   if(h==Hash.ARE_PLAYER_FLASHING_STARS_ABOUT_TO_DROP)return (T)(object)StarsFlashing;
   if(h==Hash.CREATE_PICKUP_ROTATE)return (T)(object)0;
   if(h==Hash.GET_VEHICLE_NUMBER_PLATE_TEXT)return (T)(object)"";
   if(h==Hash.GET_VEHICLE_WINDOW_TINT)return (T)(object)0;
   if(h==Hash.GET_WATER_HEIGHT_NO_WAVES){((OutputArgument)args[3]).Value=0f;return (T)(object)World.WaterAvailable;}
   if(h==Hash.START_EXPENSIVE_SYNCHRONOUS_SHAPE_TEST_LOS_PROBE){LastRay=args;return (T)(object)91;}
   if(h==Hash.GET_SHAPE_TEST_RESULT){
    if(!SeabedKnown)return (T)(object)1; // pending is not an unobstructed result
    bool vertical=(float)LastRay[0]==(float)LastRay[3]&&(float)LastRay[1]==(float)LastRay[4];
    ((OutputArgument)args[1]).Value=vertical||MarineBlocked||(MarineObstruction!=null&&MarineObstruction(new Vector3((float)LastRay[0],(float)LastRay[1],(float)LastRay[2]),new Vector3((float)LastRay[3],(float)LastRay[4],(float)LastRay[5])));
    var boolResult=(OutputArgument)args[1];
    boolResult.NativeBoolStorage=0x1234567800000000UL|((bool)boolResult.Value?1UL:0UL);
    ((OutputArgument)args[2]).Value=new Vector3((float)LastRay[0],(float)LastRay[1],MarineFloor==null?Seabed:MarineFloor(new Vector3((float)LastRay[0],(float)LastRay[1],0)));
    return (T)(object)2;
   }
if(ThrowOnce==h){ThrowOnce=null;throw new InvalidOperationException("Injected native failure");}object value=typeof(T)==typeof(bool)?(object)false:typeof(T)==typeof(string)?"":typeof(T)==typeof(float)?(object)0f:(object)0;
   if(h==Hash.IS_ENTITY_ATTACHED_TO_ENTITY)return (T)(object)(((Entity)args[0]).AttachedTo==(Entity)args[1]);
   if(h==Hash.GET_VEHICLE_MODEL_ESTIMATED_MAX_SPEED)return (T)(object)60f;
   if(h==Hash.IS_ENTITY_IN_WATER)return (T)(object)((Vehicle)args[0]).IsInWater;
   if(h==Hash.GET_VEHICLE_WHEEL_TYPE)return (T)(object)((Vehicle)args[0]).Mods.WheelType;
   if(h==Hash.GET_VEHICLE_MOD)return (T)(object)((Vehicle)args[0]).Mods[(VehicleModType)(int)args[1]].Index;
   if(h==Hash.GET_NUM_VEHICLE_MODS)return (T)(object)((Vehicle)args[0]).Mods[(VehicleModType)(int)args[1]].Count;
   if(h==Hash.GET_VEHICLE_MOD_VARIATION)return (T)(object)((Vehicle)args[0]).Mods[(VehicleModType)(int)args[1]].Custom;
   if(h==Hash.GET_PED_RELATIONSHIP_GROUP_HASH)return (T)(object)((Ped)args[0]).RelationshipGroup;
   if(h==Hash.GET_PED_TYPE)return (T)(object)(((Ped)args[0]).IsCop?6:4);if(h==Hash.GET_PED_TARGET_FROM_COMBAT_PED)return (T)(object)((Ped)args[0]).CombatTarget;if(h==Hash.GET_SELECTED_PED_WEAPON)return (T)(object)(uint)WeaponHash.Pistol;if(h==Hash.GET_WEAPONTYPE_GROUP)return (T)(object)WeaponGroup;if(h==Hash.GET_VEHICLE_TRAILER_VEHICLE){var found=Trailers.TryGetValue(((Vehicle)args[0]).Handle,out var trailer);((OutputArgument)args[1]).Value=found?trailer.Handle:0;return (T)(object)found;} if(h==Hash.IS_IPL_ACTIVE)value=IplReady;if(h==Hash.GET_INTERIOR_FROM_ENTITY)return (T)(object)(EntityInterior??InteriorId);if(h==Hash.GET_INTERIOR_AT_COORDS)return (T)(object)InteriorId;if(h==Hash.START_SCRIPT_FIRE)return (T)(object)7;if(h==Hash.IS_INTERIOR_READY)value=InteriorReady;if(h==Hash.IS_INTERIOR_DISABLED)value=InteriorDisabled;if(h==Hash.ARE_PLAYER_STARS_GREYED_OUT)value=StarsGreyed;if(h==Hash.IS_INTERIOR_CAPPED)value=InteriorCapped;
   if(h==Hash.GET_PED_AMMO_TYPE_FROM_WEAPON){var w=((Ped)args[0]).Weapons;return (T)(object)(w.AmmoTypes.TryGetValue((uint)args[1],out var type)?type:(uint)args[1]);}
   if(h==Hash.GET_MAX_AMMO){((OutputArgument)args[2]).Value=((Ped)args[0]).Weapons.MaximumAmmo;return (T)(object)true;}
   if(h==Hash.GET_VEHICLE_MODEL_NUMBER_OF_SEATS)return (T)(object)((Convert.ToInt32(args[0])==new GTA.Model("halftrack").Hash||Convert.ToInt32(args[0])==new GTA.Model("technical").Hash)?3:4);
   if(h==Hash.HAS_WEAPON_ASSET_LOADED)return (T)(object)true;
   if(h==Hash.GET_DEEP_OCEAN_SCALER)return (T)(object)1f;
   if(h==Hash.DOES_WEAPON_TAKE_WEAPON_COMPONENT)return (T)(object)true;
   if(h==Hash.HAS_PED_GOT_WEAPON_COMPONENT)return (T)(object)((Ped)args[0]).Weapons[(WeaponHash)(uint)args[1]].Components.Any(p=>(uint)p.ComponentHash==(uint)args[2]&&p.Active);
   if(h==Hash.GET_AMMO_IN_PED_WEAPON)return (T)(object)(((Ped)args[0]).Weapons.Ammo.TryGetValue((uint)args[1],out int ammoCount)?ammoCount:17);if(h==Hash.GET_ASPECT_RATIO)return (T)(object)(16f/9f);
   if(h==Hash.GET_SAFE_ZONE_SIZE)return (T)(object)1f;
   if(h==Hash.HAS_STREAMED_TEXTURE_DICT_LOADED||h==Hash.SET_CURRENT_PED_VEHICLE_WEAPON)value=true;
   if(h==Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY)value=ClearLos;if(h==Hash.IS_WEAPON_VALID)value=true;if(h==Hash.HAS_PED_GOT_WEAPON)value=((Ped)args[0]).Weapons.Owned.Contains((WeaponHash)(uint)args[1]);if(h==Hash.GET_WATER_HEIGHT)value=World.WaterAvailable;if(h==Hash.GET_GROUND_Z_FOR_3D_COORD){((OutputArgument)args[3]).Value=args.Length>4 && (bool)args[4] && World.WaterAvailable ? System.Math.Max(Seabed,0f) : Seabed;return (T)(object)SeabedKnown;}
   if(h==Hash.GET_NUM_PED_HAIR_TINTS)return (T)(object)64;
   if(h==Hash.GET_PED_HEAD_OVERLAY_NUM)return (T)(object)29;
   if(h==Hash.GET_PED_DRAWABLE_VARIATION)return (T)(object)0;
   if(h==Hash.GET_NUMBER_OF_PED_PROP_DRAWABLE_VARIATIONS)return (T)(object)3;
   if(h==Hash.GET_NUMBER_OF_PED_PROP_TEXTURE_VARIATIONS)return (T)(object)2;
   if(h==Hash.GET_NUMBER_OF_PED_DRAWABLE_VARIATIONS)return (T)(object)20;
   if(h==Hash.GET_NUMBER_OF_PED_TEXTURE_VARIATIONS)return (T)(object)12;
   if(h==Hash.GET_DISABLED_CONTROL_NORMAL)return (T)(object)(Axes.TryGetValue((Control)args[1],out var axis)?axis:0f);
   if(h==Hash.IS_DISABLED_CONTROL_PRESSED)value=Held.TryGetValue((Control)args[1],out var held)&&held;
   if(h==Hash.IS_DISABLED_CONTROL_JUST_PRESSED)value=Game.Pressed.Remove((Control)(int)args[1]);
   if(h==Hash.IS_PLAYER_FREE_AIMING_AT_ENTITY&&Values.ContainsKey(h))value=Values[h];
   if(h==Hash.IS_PED_IN_COMBAT)value=((Ped)args[0]).CombatTarget==(Ped)args[1];
   if(h==Hash.IS_PED_GETTING_INTO_A_VEHICLE)value=((Ped)args[0]).Entering;
   if(h==Hash.GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS)value=((Vehicle)args[0]).Capacity;
   if(h==Hash.HAS_COLLISION_LOADED_AROUND_ENTITY)value=World.CollisionReady;
   if(h==Hash.IS_PLAYER_BEING_ARRESTED)value=Game.Arrested;return (T)value;}
 }
}
namespace GTA.UI {
 public enum Font {ChaletLondon,Pricedown=7} public enum Alignment {Center,Right,Left}
 public class CustomSprite {
  public PointF Position; public SizeF Size; public Color Color=Color.White; private string path;
  public static int Created; public static readonly List<Tuple<string,PointF,SizeF,Color>> Drawn=new List<Tuple<string,PointF,SizeF,Color>>();
  public CustomSprite(string p,SizeF s,PointF f){path=p;Size=s;Position=f;Created++;}
  public void Draw(){Drawn.Add(Tuple.Create(path,Position,Size,Color));}
 }
 public static class Screen {public static string Subtitle;public static void ShowSubtitle(string s,int t){Subtitle=s;}}
 public class ContainerElement {public ContainerElement(PointF p,SizeF s,Color c){}public void Draw(){}}
 public class TextElement {public static int Draws;public Alignment Alignment;public Font Font;public TextElement(string t,PointF p,float s,Color c){}public void Draw(){Draws++;}}
}
namespace Bloodlines.Core {
 public static class Logger {public static void Debug(string s){} public static void Info(string s){} public static void Warn(string s){} public static void Error(string s,Exception e=null){Console.WriteLine("LOGERR: "+s+(e==null?"":" :: "+e.GetType().Name+": "+e.Message));} }
 public static class GameUtils {public static Vector3 OnGround(Vector3 point,float lift=1f)=>new Vector3(point.X,point.Y,World.GroundHeight);public static bool RoadAvailable;public static bool NearestRoadNode(Vector3 near,float maxDistance,out Vector3 point,out float heading){point=near;heading=0f;return RoadAvailable;}public static int Holds;public static void HoldUntilGrounded(Vehicle v,int maxMs=3000){Holds++;if(v!=null)v.PlaceOnGround();}public static void SetOnGround(Vehicle v){if(v!=null)v.PlaceOnGround();}public static void SettleHeld(){}public static void AssertPlayerControl(string w){if(!Game.Player.CanControlCharacter)Game.Player.CanControlCharacter=true;}public static bool IsWithin(Vector3 a,Vector3 b,float r)=>a.DistanceTo(b)<=r;public static void SafeRelease(GTA.Entity e){e.Released=true;} public static bool IsWithinFlat(Vector3 a,Vector3 b,float r)=>new Vector3(a.X,a.Y,0).DistanceTo(new Vector3(b.X,b.Y,0))<=r;public static string FailModel;public static bool RequestModel(GTA.Model m,int t=5000)=>m.Name!="prop_buoy_01"&&m.Name!=FailModel; public static void PlayFrontendSound(string a,string b){} public static int ClockCalls,WeatherCalls;public static void SetClock(int h,int m){ClockCalls++;}public static void SetWeather(string s){WeatherCalls++;}public static bool IsScreenFadedOut()=>Faded;public static void SafeDelete(GTA.Entity e){if(e!=null)e.Present=false;} public static bool Faded; public static string Message;public static void Subtitle(string s,int ms){Message=s;}public static void Notify(string s){Message=s;}public static void SafeDelete(GTA.Blip b){if(b!=null)b.Present=false;}public static void DrawObjectiveMarker(Vector3 p,Color c,float r=1f){}public static void FadeOut(int ms){Faded=true;}public static void FadeIn(int ms){Faded=false;}public static float LastProgress=-1f;public static void DrawProgressBar(float f){LastProgress=f;}}
 public class ModConfig {public bool M05LocationTestMode;public bool MissionScoreEnabled=true;public string MissionScoreEvent="DHP1_START",MissionScoreStopEvent="DHP1_STOP";public bool HandlingTuningEnabled=true;public bool VehicleDamageEnabled=true,PanelDamageEnabled=true;public float PanelDamageStrength=1f;public float DeformationMultiplier=2f,CollisionDamageMultiplier=.9f,EngineDamageMultiplier=.8f,CrewProtectionMultiplier=.55f;public bool VisualsEnabled=true,DeSmogEnabled=true,HeadlightShadowsEnabled=true,LODBoostEnabled=true,RemoveBlurEnabled=true,WaterReflectionsEnabled=true,OceanSwellEnabled=true;public float ContrastStrength=.35f,ShadowDistanceScale=1.5f,LODScale=1.75f;public string VisualPreset="NaturalCinematic",DayModifier="",DawnModifier="",DuskModifier="",NightModifier="";public string AbilityKey="CapsLock";public bool AbilitiesEnabled=true;public bool ControllerSwitchEnabled=true,SuppressVanillaSwitch=true,DevToolsEnabled=true;public bool CompanionsRespawnOnDeath;public float CompanionLeashDistance=180;public bool DeathHandlingEnabled=true,RestoreCheckpointOnDeath=true;public int DeathFadeOutMs=800,DeathHoldMs=1500,DeathFadeInMs=1200;}


}
namespace Bloodlines.Crew {


 public struct PedPlacement {public Vector3 Position;public float Heading;public PedPlacement(Vector3 p,float h){Position=p;Heading=h;}}
 public enum CompanionState {Follow,Scripted}
 public class CompanionController {public Dictionary<CrewSlot,bool> TravelChoices=new Dictionary<CrewSlot,bool>();public bool RidesAlong(CrewSlot s)=>TravelChoices.ContainsKey(s)?TravelChoices[s]:RideAlong;public bool SetTravelChoice(CrewSlot s,bool ride){if(!SetHangout(s,true))return false;TravelChoices[s]=ride;return true;}public Dictionary<CrewSlot,bool> Hangouts=new Dictionary<CrewSlot,bool>();public bool HasIndividualOrders=>Hangouts.Count>0;public bool IsHangingOut(CrewSlot s)=>Hangouts.ContainsKey(s)?Hangouts[s]:!IndependentFreeRoam;public bool SetHangout(CrewSlot s,bool value){if(MissionActive||Controlled.Contains(s))return false;Hangouts[s]=value;return true;}public bool IndependentFreeRoam=true,RideAlong=true;public int Refreshes;public void Refresh(CrewSlot slot){Refreshes++;}public bool MissionActive;public CompanionLife Life=new CompanionLife();public bool RequireSharedVehicle;public void ReleaseAll(){Controlled.Clear();RequireSharedVehicle=false;}public CompanionState StateOf(CrewSlot s)=>Controlled.Contains(s)?CompanionState.Scripted:CompanionState.Follow;public System.Collections.Generic.HashSet<CrewSlot> Controlled=new System.Collections.Generic.HashSet<CrewSlot>();public void TakeControl(CrewSlot s){Controlled.Add(s);}public void ReleaseControl(CrewSlot s){Controlled.Remove(s);}}
 public class CrewRoster {public WeaponProgression Arsenal;public bool DeploySolo(CrewSlot slot,Vector3 p,float h){IsSolo=true;Peds.Clear();Peds[slot]=new Ped{Position=p};ActiveSlot=slot;Game.Player.Character=Peds[slot];return true;}public bool Deploy(CrewSlot s,Vector3 p,float h){var d=new Dictionary<CrewSlot,PedPlacement>();foreach(var hero in Protagonist.All)d[hero.Slot]=new PedPlacement(p,h);return Deploy(s,d);}public CompanionController CompanionAI=new CompanionController();
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
 public enum Control {VehicleFlyAttack,VehicleFlyAttack2,Phone,FrontendUp,FrontendDown,FrontendLeft,FrontendRight,FrontendCancel,VehicleDuck,VehicleFlyDuck,VehicleSubAscend,VehicleSubDescend,VehicleCinCam,NextCamera,PhoneSelect,PhoneCancel,SelectWeapon,FrontendPause,FrontendPauseAlternate,Aim,Attack2,Detonate,Enter,Jump,MeleeAttack2,MeleeAttackAlternate,MoveLeftRight,MoveUpDown,Sprint,ThrowGrenade,VehicleAccelerate,VehicleAim,VehicleAttack,VehicleAttack2,VehicleBrake,VehicleHandbrake,VehicleMoveLeftRight,Context,ScriptLS,ScriptRS,SpecialAbility,SpecialAbilitySecondary,SpecialAbilityPC,VehicleSpecialAbilityFranklin,Duck,VehicleHorn,LookBehind,VehicleLookBehind,CharacterWheel,Attack,FrontendAccept,SelectCharacterMichael,SelectCharacterFranklin,SelectCharacterTrevor,SelectCharacterMultiplayer,LookLeftRight,LookUpDown,MeleeAttack1,VehicleExit, FrontendX, Reload, WeaponWheelNext, WeaponWheelPrev, SkipCutscene, MultiplayerInfo, SniperZoomInSecondary, SniperZoomOutSecondary, Talk, HUDSpecial, ContextSecondary, WeaponSpecial2, DropAmmo, VehicleHeadlight, VehicleRadioWheel, VehicleRoof, VehicleGrapplingHook, VehicleShuffle, VehicleDropProjectile, VehicleFlySelectNextWeapon, VehicleFlyVerticalFlightMode, VehiclePushbikePedal, VehiclePushbikeSprint, MeleeAttackLight, MeleeAttackHeavy, ParachuteSmoke, SaveReplayClip, PhoneUp, PhoneDown, PhoneLeft, PhoneRight, FrontendRdown, FrontendRright, FrontendEndscreenAccept, ScriptRDown, ScriptRRight, ScriptPadUp, ScriptPadDown, ScriptPadLeft, ScriptPadRight, CreatorAccept, RappelJump, PrevWeapon, NextWeapon, ReplayStartStopRecording, ReplayPause, ReplayNewmarker, ReplayScreenshot, ReplayAdvance, ReplayBack, ReplayTools, ReplayShowhotkey, ReplayCycleMarkerLeft, ReplayCycleMarkerRight, VehicleHydraulicsControlToggle, SwitchVisor, VehicleMeleeHold, VehicleParachute, VehicleBikeWings, VehicleFlyBombBay, VehicleFlyCounter, VehicleFlyTransform}
 public class Camera:Entity {public void PointAt(Vector3 p){}}
 public class Model {public static bool operator==(Model a,Model b)=>ReferenceEquals(a,b)||(!(a is null)&&!(b is null)&&a.Name==b.Name);public static bool operator!=(Model a,Model b)=>!(a==b);public override bool Equals(object o)=>o is Model m&&m==this;public override int GetHashCode()=>Name.GetHashCode();public int Hash=>Game.GenerateHash(Name);public bool IsInCdImage=true,IsValid=true;public (Vector3,Vector3) Dimensions=>Name=="lazer" ? (new Vector3(-5f,-7.5f,-1f),new Vector3(5f,7.5f,3f)) : Name=="duster" ? (new Vector3(-7.5f,-5f,-1.2f),new Vector3(7.5f,5f,2.5f)) : (Name=="granger"||Name=="baller") ? (new Vector3(-1f,-2.5f,-.6f),new Vector3(1f,2.5f,1.7f)) : Name=="tug"?(new Vector3(-5.141756f,-16.78415f,-3.719979f),new Vector3(5.141756f,14.29196f,10.97245f)):(new Vector3(-0.6f,-0.4f,0f),new Vector3(0.6f,0.4f,Name=="prop_table_03"?0.75f:0.05f));public bool IsSubmarine,IsTrain,IsBicycle;public bool IsCar=true,IsBike,IsHelicopter,IsPlane,IsBoat;public string Name;public Model(int hash){Name="model#"+hash;}public Model(string s){Name=s;IsSubmarine=s=="submersible2";IsBoat=s=="longfin"||s=="tug"||s=="tropic"||s=="dinghy";IsHelicopter=s=="supervolito"||s=="cargobob"||s=="polmav";IsPlane=s=="vestra";IsBike=s=="shinobi";IsCar=!IsBoat&&!IsHelicopter&&!IsPlane&&!IsBike&&!IsSubmarine;}public void MarkAsNoLongerNeeded(){}}
 public enum WeaponHash : uint {MilitaryRifle=2636060646,PumpShotgunMk2=1432025498,MarksmanRifle=3342088282,PetrolCan=883325847,CompactRifle=1649403952,SpecialCarbine=3231910285,UpNAtomizer=2939590305,RevolverMk2=3415619887,PericoPistol=1470379660,APPistol=584646201,Hammer=1317494643,WM29Pistol=465894841,CeramicPistol=727643628,Bottle=4192643659,UnholyHellbringer=1198256469,ProximityMine=2874559379,MarksmanPistol=3696079510,SwitchBlade=3756226112,FertilizerCan=406929569,SweeperShotgun=317205821,Pistol50=2578377531,CarbineRifle=2210333304,AdvancedRifle=2937143193,AssaultrifleMk2=961495388,Bat=2508868239,PoolCue=2484171525,Flashlight=2343591895,HazardousJerryCan=3126027122,FlareGun=1198879012,CombatPistol=1593441988,Dagger=2460120199,SmokeGrenade=4256991824,SNSPistolMk2=2285322324,Molotov=615608432,MG=2634544996,MicroSMG=324215364,VintagePistol=137902532,GrenadeLauncher=2726580491,PistolMk2=3219281620,CarbineRifleMk2=4208062921,SniperRifle=100416529,Parachute=4222310262,CombatShotgun=94989220,Firework=2138347493,Nightstick=1737195953,StunGunMultiplayer=1171102963,Grenade=2481070269,GolfClub=1141786504,Gusenberg=1627465347,Wrench=419712736,PumpShotgun=487013001,MachinePistol=3675956304,SawnOffShotgun=2017895192,BZGas=2694266206,BullpupRifle=2132975508,Revolver=3249783761,PipeBomb=3125143736,MarksmanRifleMk2=1785463520,AssaultShotgun=3800352039,ServiceCarbine=3520460075,SpecialCarbineMk2=2526821735,RailgunXmas3=4272043364,KnuckleDuster=3638508604,HeavyPistol=3523564046,AssaultSMG=4024951519,BullpupRifleMk2=2228681469,SMGMk2=2024373456,CombatMGMk2=3686625920,FireExtinguisher=101631238,StoneHatchet=940833800,Ball=600439132,DoubleBarrelShotgun=4019527611,HeavyShotgun=984333226,SMG=736523883,HeavySniper=205991906,SNSPistol=3218215474,HomingLauncher=1672152130,HeavySniperMk2=177293209,StunGun=911657153,BattleAxe=3441901897,AcidPackage=4159824478,Pistol=453432689,CompactGrenadeLauncher=125959754,Hatchet=4191993645,CompactEMPLauncher=3676729658,Flare=1233104067,CombatMG=2144741730,Snowball=126349499,AssaultRifle=3220176749,MetalDetector=3684886537,Minigun=1119849093,Unarmed=2725352035,RPG=2982836145,PrecisionRifle=1853742572,HeavyRifle=3347935668,Crowbar=2227010557,NavyRevolver=2441047180,Musket=2828843422,CandyCane=1703483498,GrenadeLauncherSmoke=1305664598,Widowmaker=3056410471,MiniSMG=3173288789,DoubleActionRevolver=2548703416,NightVision=2803906140,StickyBomb=741814745,Machete=3713923289,Knife=2578778090,CombatPDW=171789620,Railgun=1834241177,BullpupShotgun=2640438543}
 public static class Script {public static int Waited;public static void Wait(int ms){Waited+=ms;Game.GameTime+=ms;}}
}
namespace Bloodlines.Core { public static class SurveyMode {public static bool IsSurveyRunning;} }
namespace Bloodlines.Missions {

 public enum Act {I=1,II=2,III=3}

 public class MissionDefinition {public Core.MissionInfo Info;public System.Func<Mission> Factory;public bool IsPlayable=true;public bool IsSolo=>Info.IsSolo;public string Id=>Info.Id;public string Title=>Info.Title;public Act Act=>Act.I;}
 public class MissionCatalog {public List<MissionDefinition> All=new List<MissionDefinition>();public IEnumerable<MissionDefinition> Playable=>System.Linq.Enumerable.Where(All,m=>m.IsPlayable);}
 public class CheckpointManager {
  public int CommitCalls {get;private set;} private string _mission; private int _stage;
  public void Clear(){_mission=null;}
  public void Commit(string s,int n){CommitCalls++;_mission=s;_stage=n;}
  public int Restore(string s)=>HasCheckpointFor(s)?_stage:-1;
  public bool HasCheckpointFor(string s)=>_mission!=null&&_mission==s;
 }
 public class MissionContext {public Core.ApartmentAccess Interior; public Core.GarageService Garages;public Vehicle RaceVehicle;public OperationWorld Operation;public PortHeistWorld PortHeist{get=>Operation as PortHeistWorld;set=>Operation=value;}public Core.MissionDoctor Doctor=new Core.MissionDoctor();public HandoffLedger Handoffs=new HandoffLedger();public Core.CrewVan Vans;public CampaignState State;public Core.ModConfig Config;public Crew.CrewRoster Crew;public Crew.SwitchController Switching;public Core.CampaignData Data;public Core.LocationBook Locations;public Core.CutsceneDirector Cutscenes;public Core.DialogueDirector Dialogue;public Abilities.AbilityController Abilities=new Abilities.AbilityController();public CheckpointManager Checkpoints=new CheckpointManager();}
}
