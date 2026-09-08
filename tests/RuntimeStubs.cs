// Test stand-ins only. Never included in Bloodlines.dll or installed in GTA.
using System;
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
 public static class Game {public static int GameTime; public static float TimeScale=1; public static Player Player=new Player(); public static bool Arrested;}
 public class Player {public Ped Character=new Ped(); public bool IsDead; public bool CanControlCharacter=true; public int WantedLevel;}
 public class Entity {static int next; public int Handle=++next; public bool Present=true; public bool Exists()=>Present; public Vector3 Position,Velocity; public float Heading; public bool IsPositionFrozen;}
 public enum Relationship {Neutral,Hate}
 public class Ped:Entity {
  public bool IsDead,IsInCombat,IsShooting,IsInvincible,AlwaysKeepTask,BlockPermanentEvents,Entering,Hostile;
  public Vector3 ForwardVector; public Vehicle CurrentVehicle; public int RelationshipGroup; public Ped CombatTarget;
  public Tasks Task; public Ped(){Task=new Tasks(this);} public bool IsInVehicle()=>CurrentVehicle!=null;
  public bool IsInVehicle(Vehicle v)=>v!=null&&CurrentVehicle==v;
  public Relationship GetRelationshipWithPed(Ped p)=>Hostile?Relationship.Hate:Relationship.Neutral;
 }
 public class Vehicle:Entity {public float Speed; public string DisplayName="test car"; public int Capacity=3; public Dictionary<VehicleSeat,Ped> Seats=new Dictionary<VehicleSeat,Ped>(); public bool IsSeatFree(VehicleSeat s)=>(int)s>=0&&(int)s<Capacity&&!Seats.ContainsKey(s);}
 public enum VehicleSeat {None=-3,Driver=-1,RightFront=0,LeftRear=1,RightRear=2}
 public enum EnterVehicleFlags {None}
 public class Tasks {
  readonly Ped owner; public int Clears,Enters,Warps,Fights; public VehicleSeat LastSeat; public Ped LastTarget;
  public Tasks(Ped p){owner=p;} public void ClearAll(){Clears++;} public void ClearAllImmediately(){Clears++;}
  public void FollowToOffsetFromEntity(Ped p,Vector3 v,float s,int t,float r,bool b){} public void GuardCurrentPosition(){}
  public void FightAgainst(Ped p){Fights++;LastTarget=p;owner.IsInCombat=true;}
  public void EnterVehicle(Vehicle v,VehicleSeat s,int t,float f,EnterVehicleFlags flags){Enters++;LastSeat=s;owner.Entering=true;}
  public void WarpIntoVehicle(Vehicle v,VehicleSeat s){Warps++;LastSeat=s;owner.CurrentVehicle=v;v.Seats[s]=owner;}
 }
 public enum BlipSprite {Standard} public enum BlipColor {Yellow}
 public class Blip:Entity {public BlipSprite Sprite;public BlipColor Color;public bool IsShortRange,ShowRoute;public string Name;}
 public static class World {public static Ped[] Nearby=new Ped[0];public static Blip LastBlip;public static bool CollisionReady;
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
 public enum Hash {IS_PED_IN_COMBAT,IS_PED_GETTING_INTO_A_VEHICLE,GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS,REQUEST_COLLISION_AT_COORD,HAS_COLLISION_LOADED_AROUND_ENTITY,IS_PLAYER_BEING_ARRESTED,SET_CAM_DEATH_FAIL_EFFECT_STATE,RESET_PLAYER_ARREST_STATE,FORCE_GAME_STATE_PLAYING,PAUSE_DEATH_ARREST_RESTART,IGNORE_NEXT_RESTART,SET_FADE_OUT_AFTER_DEATH,SET_FADE_OUT_AFTER_ARREST,SET_FADE_IN_AFTER_DEATH_ARREST}
 public static class Function {
  public static Hash? ThrowOnce; public static Dictionary<Hash,object> Values=new Dictionary<Hash,object>();
  public static void Call(Hash h,params object[] args){if(ThrowOnce==h){ThrowOnce=null;throw new InvalidOperationException("Injected native failure");}if(args.Length>0)Values[h]=args[0];if(h==Hash.RESET_PLAYER_ARREST_STATE)Game.Arrested=false;}
  public static T Call<T>(Hash h,params object[] args){object value=false;
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
 public static class GameUtils {public static bool Faded; public static string Message;public static void Subtitle(string s,int ms){Message=s;}public static void Notify(string s){Message=s;}public static void SafeDelete(GTA.Blip b){if(b!=null)b.Present=false;}public static void DrawObjectiveMarker(Vector3 p,Color c,float r){}public static void FadeOut(int ms){Faded=true;}public static void FadeIn(int ms){Faded=false;}}
 public class ModConfig {public float CompanionLeashDistance=180;public bool DeathHandlingEnabled=true,RestoreCheckpointOnDeath=true;public int DeathFadeOutMs=800,DeathHoldMs=1500,DeathFadeInMs=1200;}
 public class DialogueDirector {public int Clears;public void Clear(){Clears++;}}
 public class CampaignData {public Dictionary<string,Vector3> Anchors=new Dictionary<string,Vector3>();public bool TryAnchor(string key,out Vector3 p,out float h){h=90;return Anchors.TryGetValue(key,out p);}}
}
namespace Bloodlines.Crew {
 public enum CrewSlot {Ice,Gohan,Guess}
 public class Protagonist {public string FirstName="Ice";public static Protagonist Of(CrewSlot s)=>new Protagonist();}
 public struct PedPlacement {public Vector3 Position;public float Heading;public PedPlacement(Vector3 p,float h){Position=p;Heading=h;}}
 public class CrewRoster {public bool IsDeployed=true,CompanionsHoldPosition,CanRevive=true;public int Dismissals,Regroups;public CrewSlot ActiveSlot;public Protagonist Active=new Protagonist();public GTA.Ped ActivePed;
  public PedPlacement? RecoveryOrigin=new PedPlacement(new Vector3(10,20,30),0);public PedPlacement? DeployOrigin;
  public GTA.Ped PedFor(CrewSlot s)=>ActivePed;
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
