using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Objectives;
using GTA;

public sealed class ExitAuditObjective : Objective
{
 public int Exits; public bool Throw;
 public ExitAuditObjective():base("Finish the terminal"){}
 public override void Update(MissionContext c){}
 public override void Exit(MissionContext c){Exits++;if(Throw)throw new Exception("cleanup probe");}
}
public sealed class AuditMission : ComposedMission
{
 public ExitAuditObjective First=new ExitAuditObjective{Throw=true},Second=new ExitAuditObjective();
 public override string Id=>"M03";public override string Title=>"Audit";
 protected override bool Setup()=>true;
 protected override IEnumerable<MissionStage> BuildStages(){yield return new MissionStage("Work",First,Second).OwnedBy(CrewSlot.Gohan);}
}
public static partial class StoryTests
{
 static void CampaignAuditChecks()
 {
  Reset();var crew=Roster();var c=Context(crew);var m=new AuditMission();m.Begin(c);m.Tick();
  Check(m.CurrentObjective.StartsWith("Switch to Gohan"),"Persistent objective explicitly names the required character");
  m.Fail("test");m.Cleanup();
  Check(m.First.Exits==1&&m.Second.Exits==1,"Failure exits every active objective exactly once despite another cleanup throwing");
  Check(crew.CompanionAI.Controlled.Count==0,"Failed objective teardown releases all mission-owned crew tasks");
  var stage=new MissionStage("Protect and deliver",new ProtectObjective("Protect the car",()=>new Vehicle(),"Lost"),new ExitAuditObjective());
  Check(stage.Current is ExitAuditObjective,"Actionable work takes HUD priority over a passive guard condition");
  var path=Path.Combine(root,"rewards-audit.json");var state=CampaignState.Load(path);var cat=new MissionCatalog();
  Check(state.LastHero==CrewSlot.Guess,"Fresh campaign defaults to Guess");
  state.MarkComplete("M24",cat);state.MarkComplete("M24",cat);
  Check(state.CashOnHand==200000&&state.AlamoGoldDredgedTons==5,"Replaying a completed heist cannot duplicate cash or gold");
  var loaded=CampaignState.Load(path);loaded.MarkComplete("M24",cat);
  Check(loaded.CashOnHand==200000&&loaded.Completed.Contains("M24"),"Payout and completion survive reload together and remain idempotent");
  loaded.MarkComplete("M03",cat);Check(loaded.IsUnlocked("cypressFoundry"),"Foundry access unlocks on committed mission completion");
  var locked=new MissionDefinition{Info=new MissionInfo{Id="M29",Prerequisite="M28"},Factory=()=>new AuditMission()};cat.All.Add(locked);
  Check(loaded.NextPlayable(cat)==null,"No next-job fallback bypasses an unmet prerequisite");
  loaded.Reset();Check(loaded.LastHero==CrewSlot.Guess&&loaded.Weapons.Count==0,"Reset returns to Guess and clears the earned arsenal");
  Check(Protagonist.All.Select(h=>h.Loadout[0]).Distinct().Count()==3,"All three protagonists have different primary rifles");
  Check((uint)Protagonist.Guess.Loadout[0]==unchecked((uint)Game.GenerateHash("WEAPON_TACTICALRIFLE")),"Guess starts with the Service Carbine hash");
  var memory=new CrewMemory(loaded);
  crew.PedFor(CrewSlot.Ice).Health=420;crew.PedFor(CrewSlot.Ice).Armor=12;
  crew.PedFor(CrewSlot.Guess).Health=710;crew.PedFor(CrewSlot.Guess).Armor=80;
  crew.PedFor(CrewSlot.Guess).Weapons.Give(Protagonist.Guess.Loadout[0],17,false,true);
  memory.Capture(crew);loaded.Save();var memorySave=CampaignState.Load(path);
  crew.PedFor(CrewSlot.Ice).Health=900;crew.PedFor(CrewSlot.Guess).Health=900;
  new CrewMemory(memorySave).Restore(crew);
  Check(crew.PedFor(CrewSlot.Ice).Health==420&&crew.PedFor(CrewSlot.Guess).Health==710&&crew.PedFor(CrewSlot.Ice).Armor==12,"Save/reload preserves each hero's distinct injuries and armor");
  Check(GTA.Native.Function.Values.ContainsKey(GTA.Native.Hash.SET_PED_AMMO),"Free-roam restore reapplies saved ammunition rather than refilling every weapon");
  Check(Json.Object(Json.Object(memorySave.CharacterMemory["Guess"])["ammo"]).Count>0,"Ammunition is persisted in the owning hero's record");
  var target=new GTA.Math.Vector3(100,100,0);var tractor=new Vehicle{Position=target};var tanker=new Vehicle{Position=target};var other=new Vehicle{Position=target};
  Use(crew,CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(tractor,VehicleSeat.Driver);
  var cargo=new TrailerDeliveryObjective(()=>tractor,()=>tanker,()=>target);cargo.Enter(c);
  GTA.Native.Function.Trailers[tractor.Handle]=other;cargo.Update(c);
  Check(!cargo.IsFinished,"A different coupled trailer cannot satisfy a fuel delivery");
  GTA.Native.Function.Trailers[tractor.Handle]=tanker;tractor.Speed=10;cargo.Update(c);Check(!cargo.IsFinished,"Fuel delivery cannot complete while the rig is moving");
  tractor.Speed=0;cargo.Update(c);Check(cargo.Status==ObjectiveStatus.Complete,"The specified attached tanker completes delivery when both vehicles stop at the destination");
  var unloading=new MissionInteraction("Unload",()=>target,2,30,()=>tractor,true);unloading.Enter(c);tractor.Speed=5;Game.GameTime+=CutsceneDirector.SkipGraceMs;Game.Accept=true;unloading.Update(c);Game.GameTime+=5000;unloading.Update(c);
  Check(!unloading.IsFinished&&unloading.Label.Contains("stop"),"Driving through an unloading marker cannot complete the work timer");
  var arsenal=new WeaponProgression(loaded);loaded.Completed.Add("M23");arsenal.UnlockRewards();
  Check(Protagonist.All.All(h=>loaded.Weapons[h.Slot.ToString()].Contains((uint)WeaponProgression.Rewards("M23")[(int)h.Slot])),"Bunker completion grants each hero a distinct Mk II locker rifle");
  Reset();crew=Roster();c=Context(crew);state=CampaignState.Load(Path.Combine(root,"retry-audit.json"));cat=new MissionCatalog();
  int attempts=0;var def=new MissionDefinition{Info=new MissionInfo{Id="X01",Title="Retry probe"},Factory=()=>{attempts++;return new AuditMission();}};cat.All.Add(def);
  var manager=new MissionManager(c,state,cat);manager.Start(def);manager.ForceFail("Lost vehicle");manager.Update();
  Check(manager.RetryAvailable&&!manager.IsRunning&&state.CompletedCount==0,"Failure offers full retry without awarding progress");
  manager.Retry();Check(attempts==2&&manager.CurrentStage==0&&manager.IsRunning,"Retry constructs a fresh mission at stage zero");manager.Abort();
  var broken=new MissionDefinition{Info=new MissionInfo{Id="X02",Title="Broken factory"},Factory=()=>{throw new Exception("factory failure");}};
  Check(!manager.Start(broken)&&!manager.IsRunning&&manager.RetryAvailable,"A throwing mission factory returns cleanly to retryable free roam");
  foreach(var type in typeof(ComposedMission).Assembly.GetTypes().Where(t=>!t.IsAbstract&&t.IsSubclassOf(typeof(ComposedMission))&&t.Namespace=="Bloodlines.Missions.Campaign"))
  {
   Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"abort-"+type.Name+".json"));var mission=(ComposedMission)Activator.CreateInstance(type);
   if(mission.Id=="M07")GTA.Native.Function.Seabed=55f; // M07 refuses a world with no roof over the street at its key; this world has one.
   Check(mission.Begin(c),mission.Id+" builds a fresh attempt for failure/retry audit");
   var required=Flow(mission).SelectMany(s=>s.Objectives).Where(o=>o.RequiredCharacter.HasValue).Select(o=>o.RequiredCharacter.Value).First();
   crew.PedFor(required).IsDead=true;mission.Tick();
   Check(mission.Status==MissionStatus.Failed&&c.State.CompletedCount==0,mission.Id+" fails clearly if a required hero is down");
   var retry=(ComposedMission)Activator.CreateInstance(type);crew.PedFor(required).IsDead=false;
   Check(retry.Begin(c)&&retry.CurrentStage==0,mission.Id+" full restart rebuilds valid initial objectives");retry.Abort();
   Check(crew.CompanionAI.Controlled.Count==0,mission.Id+" abort releases assigned crew AI");
  }
 }
}
