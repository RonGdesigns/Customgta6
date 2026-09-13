using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Math;
using GTA.Native;

public static partial class StoryTests
{
 static void SeptemberRepairChecks()
 {
  Reset(); var crew=Roster();var c=Context(crew);string path=Path.Combine(root,"repair-state.json");var state=CampaignState.Load(path);c.State=state;
  state.SetCargo("engines","later-shop");state.SetEvidence("millerDrive",EvidenceState.Proven);state.CrewVan.Mods[11]=3;state.CrewVan.TiresReinforced=true;state.Save();
  state.BeginAttempt("M08");state.SetCargo("engines","early-stash");state.SetEvidence("millerDrive",EvidenceState.CopyHeld);state.SetUpgrade("iffTransponder",true);state.Safehouses["cypressFoundry"]=true;state.Save();
  var disk=CampaignState.Load(path);
  Check(disk.CargoAt("engines")=="later-shop"&&disk.EvidenceOf("millerDrive")==EvidenceState.Proven&&!disk.FleetUpgrades.ContainsKey("iffTransponder")&&!disk.Safehouses["cypressFoundry"],"Attempt state is visible in memory but cannot escape into a mid-mission save");
  state.DiscardAttempt();Check(state.CargoAt("engines")=="later-shop"&&state.EvidenceOf("millerDrive")==EvidenceState.Proven&&!state.FleetUpgrades.ContainsKey("iffTransponder")&&!state.Safehouses["cypressFoundry"],"Failure/abort rolls evidence, cargo, fleet and base flags back together");
  var catalog=new MissionCatalog();catalog.All.Add(Def("M08","main"));
  state.BeginAttempt("M08");state.SetCargo("engines","first-stash");state.MarkComplete("M08",catalog);disk=CampaignState.Load(path);
  Check(disk.IsComplete("M08")&&disk.CargoAt("engines")=="first-stash"&&!state.AttemptActive,"Successful completion commits the story transaction");
  state.SetCargo("engines","installed");state.BeginAttempt("M08");state.SetCargo("engines","first-stash");state.MarkComplete("M08",catalog);
  Check(state.CargoAt("engines")=="installed"&&CampaignState.Load(path).CargoAt("engines")=="installed","A successful early-mission replay cannot undo later cargo progress");
  state.Reset();disk=CampaignState.Load(path);Check(disk.Evidence.Count==0&&disk.Cargo.Count==0&&disk.CrewVan.Mods.Count==0&&!disk.CrewVan.TiresReinforced&&disk.CompletedCount==0,"Full reset clears the newer story collections and the old van customization");

  foreach(bool skip in new[]{false,true})
  {
   Reset();crew=Roster();c=Context(crew);state=CampaignState.Load(Path.Combine(root,"required-"+skip+".json"));c.State=state;
   var probe=new ProbeMission();var def=new MissionDefinition{Info=new MissionInfo{Id="M02",Title="Probe"},Factory=()=>probe};catalog=new MissionCatalog();catalog.All.Add(def);var manager=new MissionManager(c,state,catalog);
   manager.Start(def);c.Cutscenes.Skip();manager.Update();int ticks=probe.Ticks;
   c.Cutscenes.Play(new SceneSpec{MissionId="M04",Phase="transaction",Title="Required",RequiresCompletion=true,Blocking=new SceneBlocking().Then(new VerifySceneStep("fails",()=>false))});
   if(skip)c.Cutscenes.Skip();else c.Cutscenes.Stop();
   manager.Update();Check(probe.Status==MissionStatus.Failed&&probe.Ticks==ticks&&manager.RetryAvailable&&!state.AttemptActive,"Host skips active-scene mission ticks: interrupted/failed required result still fails before gameplay resumes (skip="+skip+")");
  }
  Reset();crew=Roster();c=Context(crew);state=CampaignState.Load(Path.Combine(root,"brief-abort.json"));c.State=state;catalog=new MissionCatalog();var d=new MissionDefinition{Info=new MissionInfo{Id="M02",Title="Probe"},Factory=()=>new ProbeMission()};catalog.All.Add(d);var mgr=new MissionManager(c,state,catalog);
  mgr.Start(d);state.SetCargo("probe","uncommitted");mgr.Abort();Check(!state.AttemptActive&&state.CargoAt("probe")==null,"Aborting the opening briefing releases its story transaction");
  mgr.Start(d);c.Cutscenes.Skip();mgr.Update();state.SetCargo("probe","pending");mgr.ResetCampaignContext();Check(!mgr.IsRunning&&!mgr.RetryAvailable&&mgr.LastAttempted==null&&state.CargoAt("probe")==null,"Reset stops the running mission and clears retry/continuation context before the save is wiped");

  Reset();crew=Roster();var roles=new RoleTracks(crew,()=>new Ped[0]);var worker=crew.PedFor(CrewSlot.Gohan);var track=roles.For(CrewSlot.Gohan);track.Approach(new Vector3(200,0,0),new Vector3(210,0,0));roles.Update();int go=worker.Task.Gotos;
  crew.SetActive(CrewSlot.Gohan);worker.Task.ClearAllImmediately();roles.Update();Check(worker.Task.Gotos==go,"Player-controlled mission role receives no new AI walk task");
  crew.SetActive(CrewSlot.Ice);roles.Update();roles.Update();Check(worker.Task.Gotos==go+1,"Handback reissues the cleared approach exactly once");roles.Release();

  Reset();Function.Calls.Clear();var config=new ModConfig();var visuals=new VisualAtmosphere(config);visuals.Update(false,false);var first=new Vehicle();Game.Player.Character.SetIntoVehicle(first,VehicleSeat.Driver);visuals.Update(false,false);
  foreach(var h in new[]{Hash.SET_PED_LOD_MULTIPLIER,Hash.SET_VEHICLE_LOD_MULTIPLIER,Hash.SET_VEHICLE_HEADLIGHT_SHADOWS})Check(Function.Calls.Where(x=>x.Item1==h).All(x=>x.Item2.Length==2&&x.Item2[0] is Entity),"Visual native has an entity plus its setting: "+h);
  var second=new Vehicle();Game.Player.Character.SetIntoVehicle(second,VehicleSeat.Driver);Function.Calls.Clear();visuals.Update(false,false);
  Check(Function.Calls.Any(x=>x.Item1==Hash.SET_VEHICLE_LOD_MULTIPLIER&&x.Item2[0]==first&&(float)x.Item2[1]==1f)&&Function.Calls.Any(x=>x.Item1==Hash.SET_VEHICLE_HEADLIGHT_SHADOWS&&x.Item2[0]==second&&(int)x.Item2[1]==3),"Vehicle switching restores the old graphics target and configures the new one");visuals.Reset();

  Reset();state=CampaignState.Load(Path.Combine(root,"delta-rewards.json"));catalog=new MissionCatalog();foreach(string id in new[]{"M19","M20","M21","M22","SM01","M27"})catalog.All.Add(Def(id,id.StartsWith("SM")?"solo":"main"));
  var rewards=new CompletionRewards(state);state.CompletePortHeist(catalog,new Dictionary<string,string>());var notices=rewards.Describe(state,0);
  Check(notices.Count(x=>x.Contains("unlocked"))==3&&notices.Any(x=>x.Contains("Ice"))&&notices.All(x=>!x.Contains("a DLC weapon")),"One heist completion announces all three phases' weapon entitlements with real names");
  rewards=new CompletionRewards(state);int cash=state.CashOnHand;state.CompletePortHeist(catalog,new Dictionary<string,string>());Check(rewards.Describe(state,cash).Count==0,"Replaying a completed heist announces no cash or new weapons");
  rewards=new CompletionRewards(state);cash=state.CashOnHand;state.MarkComplete("SM01",catalog);notices=rewards.Describe(state,cash);Check(!notices.Any(x=>x.Contains("Gohan unlocked")||x.Contains("Guess unlocked")),"A solo reward is only announced for its owner");
  rewards=new CompletionRewards(state);cash=state.CashOnHand;state.MarkComplete("M27",catalog);Check(rewards.Describe(state,cash).Any(x=>x.Contains("New homes")&&x.Contains("Eclipse")),"Housing tier unlocks are included in the completion notice");
  Check(WeaponProgression.NameOf((WeaponHash)Game.GenerateHash("WEAPON_TECPISTOL"))!="a DLC weapon","The DLC catalog names a weapon not present in an older SDK enum");
  Check(MissionContextCard.Cards.Count==49&&Enumerable.Range(1,43).All(i=>MissionContextCard.Cards.ContainsKey("M"+i.ToString("00")))&&Enumerable.Range(1,6).All(i=>MissionContextCard.Cards.ContainsKey("SM"+i.ToString("00"))),"Every playable mission has a skipped-briefing target/reason/roles/destination card");

  Reset();Function.Calls.Clear();var ped=Game.Player.Character;bool spoken=false;var phone=new UsePhoneStep(ped,1000);var block=new SceneBlocking().Then(phone);block.BindDialogue(()=>spoken);block.Update();Game.GameTime+=5000;block.Update();
  Check(block.Current==phone&&!Function.Calls.Any(x=>x.Item1==Hash.TASK_USE_MOBILE_PHONE&&!(bool)x.Item2[1]),"Phone stays raised past the old fixed timer while dialogue is unfinished");
  spoken=true;block.Update();Check(!block.IsFinished&&Function.Calls.Any(x=>x.Item1==Hash.TASK_USE_MOBILE_PHONE&&!(bool)x.Item2[1]),"Last line requests phone lowering, allowing its animation to play");Game.GameTime+=901;block.Update();Check(block.Succeeded,"Natural call completion waits for the lowering beat");
  Function.Calls.Clear();var unstarted=new SceneBlocking().Then(new UsePhoneStep(ped));unstarted.Complete();Check(Function.Calls.Count==0&&unstarted.Succeeded,"Skipping an unstarted call never flashes the phone");
  var gated=new SceneBlocking{DialogueAfterStep=1}.Then(new UsePhoneStep(ped,500));gated.BindDialogue(()=>false);gated.Update();Game.GameTime+=501;gated.Update();Game.GameTime+=901;gated.Update();Check(!gated.HoldsDialogue&&gated.Succeeded,"A phone before the dialogue gate cannot deadlock on its own future conversation");

  Reset();crew=Roster();c=Context(crew);var home=ApartmentTiers.For(CrewSlot.Guess,ApartmentTier.Starter);Check(home.InteriorKey=="Apartment.Starter.Interior.Guess"&&home.RoomPrefix=="Apartment.Room.Guess"&&ApartmentTiers.For(CrewSlot.Ice,ApartmentTier.Starter).InteriorKey!=home.InteriorKey&&ApartmentTiers.For(CrewSlot.Gohan,ApartmentTier.Starter).InteriorKey!=home.InteriorKey,"Guess returns to the requested modest house shell with distinct room keys from both brothers");
  var apt=new ApartmentAccess(crew);ped=Game.Player.Character;var door=c.Locations.Position(home.EntranceKey);ped.Position=door;Function.InteriorId=141313;Function.EntityInterior=null;Function.InteriorReady=true;World.CollisionReady=true;
  Check(apt.Begin(c.Locations.Position(home.InteriorKey),home.Ipl,true,home.Probe,0f),"Guess apartment transition accepts its shell and probe");Game.GameTime+=300;apt.Update();apt.Update();
  state=CampaignState.Load(Path.Combine(root,"prologue-exit.json"));int handoffs=0;var prologue=new PrologueSequence(crew,c.Cutscenes,c.Locations,state,()=>door,apt){Finished=()=>handoffs++};
  typeof(PrologueSequence).GetMethod("BeginExit",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(prologue,null);prologue.Update();prologue.Update();Game.GameTime+=300;apt.Update();apt.Update();prologue.Update();
  Check(!apt.Inside&&!apt.Busy&&ped.Position==door&&Game.Player.Character==ped&&handoffs==1&&state.PrologueComplete,"After the inside call, the prologue exits to Guess's own street door without replacing his ped");

  Vector3 min=new Vector3(-1,-2,-.4f),max=new Vector3(1,2,1.2f),offset;float damage;
  Check(VehiclePanelDamage.TryDent(new Vector3(-1,0,0),min,max,12f,1f,out offset,out damage)&&offset.X<0&&damage<=180f,"A left-side impact places a bounded dent on the left door region");
  Check(VehiclePanelDamage.TryDent(new Vector3(1,0,0),min,max,12f,1f,out offset,out damage)&&offset.X>0,"A right-side impact dents the right side");
  Check(VehiclePanelDamage.TryDent(new Vector3(0,0,1),min,max,12f,1f,out offset,out damage)&&offset.Z>1f,"A roof impact dents the roof rather than the floor");
  Check(!VehiclePanelDamage.TryDent(new Vector3(0,0,-1),min,max,12f,1f,out offset,out damage)&&!VehiclePanelDamage.TryDent(new Vector3(0,1,0),min,max,12f,1f,out offset,out damage),"Underbody and ordinary front impacts are left to native collision deformation");
  Check(!VehiclePanelDamage.TryDent(new Vector3(0,0,1),min,max,0f,1f,out offset,out damage)&&!VehiclePanelDamage.TryDent(new Vector3(1,0,0),min,max,float.NaN,1f,out offset,out damage),"Resting overturned and invalid physics cannot trigger additional panel damage");
  Reset();Function.Calls.Clear();var dentCar=new Vehicle{Velocity=new Vector3(12f,0,0),BodyHealth=740,EngineHealth=620};var panel=new VehiclePanelDamage();config=new ModConfig();
  Function.Values[Hash.HAS_ENTITY_COLLIDED_WITH_ANYTHING]=true;Function.Values[Hash.GET_COLLISION_NORMAL_OF_LAST_HIT_FOR_ENTITY]=new Vector3(1,0,0);Function.Values[Hash.GET_OFFSET_FROM_ENTITY_GIVEN_WORLD_COORDS]=new Vector3(1,0,0);
  panel.Update(new[]{dentCar},config);Game.GameTime+=80;dentCar.Velocity=Vector3.Zero;panel.Update(new[]{dentCar},config);
  Check(Calls(Hash.SET_VEHICLE_DAMAGE)==1&&dentCar.BodyHealth==740&&dentCar.EngineHealth==620,"Measured side collision invokes the dent native once without applying a second health penalty");
  for(int i=0;i<6;i++){Game.GameTime+=80;dentCar.Velocity=new Vector3(i%2==0?12:0,0,0);panel.Update(new[]{dentCar},config);}
  Check(Calls(Hash.SET_VEHICLE_DAMAGE)==1,"Continuous collision contact cannot repeatedly crush a panel inside the cooldown");
  Game.GameTime+=1000;dentCar.Velocity=new Vector3(40,0,0);panel.Update(new[]{dentCar},config);Check(Calls(Hash.SET_VEHICLE_DAMAGE)==1,"A long scene/frame gap invalidates the previous velocity sample");
  Game.GameTime+=80;dentCar.Position=new Vector3(500,0,0);Game.Player.Character.Position=dentCar.Position;dentCar.Velocity=Vector3.Zero;panel.Update(new[]{dentCar},config);Check(Calls(Hash.SET_VEHICLE_DAMAGE)==1,"A teleport is not a collision dent");
  config.PanelDamageEnabled=false;panel.Update(new[]{dentCar},config);config.PanelDamageEnabled=true;Game.GameTime+=80;dentCar.Velocity=new Vector3(20,0,0);panel.Update(new[]{dentCar},config);Check(Calls(Hash.SET_VEHICLE_DAMAGE)==1,"Toggling panel damage clears velocity history before re-enabling");
  Function.Values.Remove(Hash.HAS_ENTITY_COLLIDED_WITH_ANYTHING);Function.Values.Remove(Hash.GET_COLLISION_NORMAL_OF_LAST_HIT_FOR_ENTITY);Function.Values.Remove(Hash.GET_OFFSET_FROM_ENTITY_GIVEN_WORLD_COORDS);
 }
}
