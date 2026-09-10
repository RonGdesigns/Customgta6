using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

public static partial class StoryTests
{
 static MissionDefinition Def(string id,string kind,string prereq="",bool playable=true)=>new MissionDefinition{Info=new MissionInfo{Id=id,Title=id,Kind=kind,Prerequisite=prereq,Number=id.StartsWith("SM")?int.Parse(id.Substring(2)):int.Parse(id.Substring(1))},Factory=()=>new AuditMission(),IsPlayable=playable};

 static void CorrectionPassChecks()
 {
  // ---- 1. Solo story gates, central and grandfathered.
  Reset();var cat=new MissionCatalog();
  foreach(var id in new[]{"M17","M18"})cat.All.Add(Def(id,"main"));
  foreach(var id in new[]{"SM01","SM02","SM03"})cat.All.Add(Def(id,"solo","M03"));
  cat.All.Add(Def("M19","main","M18"));cat.All.Add(Def("M20","main","M19"));
  var state=CampaignState.Load(Path.Combine(root,"gates.json"));state.Completed.Add("M03");state.Completed.Add("M17");
  Check(state.NextPlayable(cat).Id=="M18","Normal progression prefers the next main mission over an unlocked solo");
  Check(state.Progress(cat)==CampaignProgress.StoryAvailable,"An open story mission reports as available");
  state.MarkComplete("M18",cat);
  Check(state.Progress(cat)==CampaignProgress.StoryGated&&state.NextStory(cat).Id=="M19","Reaching M19 with solos unfinished reports a gate, not a missing mission");
  Check(state.NextPlayable(cat).Id=="SM01"&&state.CurrentMissionId=="SM01","At the gate the next job is the first outstanding solo");
  Check(state.DescribeGate(state.NextStory(cat),cat)=="M19 needs SM01, SM02, SM03 finished first.","The gate names every remaining job");
  var crew=Roster();var c=Context(crew);var manager=new MissionManager(c,state,cat);
  Check(!manager.Start(cat.All.First(m=>m.Id=="M19"))&&!manager.IsRunning&&GameUtils.Message.Contains("SM01, SM02, SM03"),"Starting a gated story mission refuses and tells the player which jobs remain");
  Check(manager.Start(cat.All.First(m=>m.Id=="M19"),bypassGates:true)&&manager.IsRunning,"QA can explicitly bypass a gate");manager.Abort();
  state.MarkComplete("SM01",cat);state.MarkComplete("SM03",cat);
  Check(state.DescribeGate(state.NextStory(cat),cat)=="M19 needs SM02 finished first."&&state.NextPlayable(cat).Id=="SM02","Partial progress narrows the gate to what is left");
  state.MarkComplete("SM02",cat);
  Check(state.GateSatisfied(cat.All.First(m=>m.Id=="M19"),cat)&&state.NextPlayable(cat).Id=="M19","All required solos done reopens the story");
  Check(manager.Start(cat.All.First(m=>m.Id=="M19")),"An open gate starts normally");manager.Abort();
  // Old save already past the gate.
  var old=CampaignState.Load(Path.Combine(root,"gates-old.json"));foreach(var id in new[]{"M03","M17","M18","M19"})old.Completed.Add(id);
  Check(old.GateGrandfathered("M19")&&old.NextPlayable(cat).Id=="M20"&&old.Progress(cat)==CampaignProgress.StoryAvailable,"A save that already finished M19 is not sent back to the solo jobs");
  var later=CampaignState.Load(Path.Combine(root,"gates-later.json"));foreach(var id in new[]{"M03","M17","M18","M20"})later.Completed.Add(id);
  Check(later.GateGrandfathered("M19")&&later.GateSatisfied(cat.All.First(m=>m.Id=="M19"),cat),"A save past a later main mission is grandfathered through an earlier gate it never saw");
  Check(!state.GateGrandfathered("M44"),"Grandfathering is per gate: M44 is still ahead of this save");
  // A required solo with no script blocks the story and says so; it is never silently waived.
  var thin=new MissionCatalog();thin.All.Add(Def("M62","main"));thin.All.Add(Def("SM07","solo","M50",playable:false));thin.All.Add(Def("SM08","solo","M50"));thin.All.Add(Def("M63","main","M62"));
  var future=CampaignState.Load(Path.Combine(root,"gates-thin.json"));future.Completed.Add("M50");future.Completed.Add("M62");
  var m63=thin.All.First(m=>m.Id=="M63");
  Check(future.OutstandingGateJobs(m63,thin).SequenceEqual(new[]{"SM07","SM08"})&&future.UnavailableGateJobs(m63,thin).SequenceEqual(new[]{"SM07"}),"An unscripted required solo stays outstanding and is reported as unavailable");
  Check(future.Progress(thin)==CampaignProgress.StoryBlocked&&future.NextPlayable(thin).Id=="SM08","The story reports blocked by unavailable content while the playable required job is still offered");
  Check(future.DescribeGate(m63,thin)=="M63 needs SM08 finished first; SM07 has no script in this build.","The gate text separates what to play from what the build lacks");
  future.MarkComplete("SM08",thin);
  Check(future.Progress(thin)==CampaignProgress.StoryBlocked&&future.NextPlayable(thin)==null&&future.CurrentMissionId==""&&future.DescribeProgress(thin).Contains("SM07")&&future.DescribeProgress(thin).Contains("no script"),"With only unscripted required content left, nothing is offered and the block names the missing job");
  var blockedManager=new MissionManager(Context(Roster()),future,thin);
  Check(!blockedManager.Start(m63)&&GameUtils.Message.Contains("SM07")&&!blockedManager.IsRunning,"Normal play cannot start the gated mission past unavailable required content");
  Check(blockedManager.Start(m63,bypassGates:true)&&blockedManager.IsRunning,"Dev mode may bypass the unavailable-content block");blockedManager.Abort();
  var past=CampaignState.Load(Path.Combine(root,"gates-thin-past.json"));foreach(var id in new[]{"M50","M62","M63"})past.Completed.Add(id);
  Check(past.GateSatisfied(m63,thin)&&past.Progress(thin)!=CampaignProgress.StoryBlocked,"A save already past the gate is still grandfathered through unavailable required content");
  // Markers route to the same answer as the mission key.
  Check(CampaignState.StoryGates.Count==4&&CampaignState.StoryGates["M68"].SequenceEqual(new[]{"SM09"}),"All four story gates are declared centrally");

  // ---- 2. Weapon loan lifecycle across a whole mission and back into free roam.
  Reset();crew=Roster();c=Context(crew);var loanState=CampaignState.Load(Path.Combine(root,"loan-cycle.json"));var arsenal=new WeaponProgression(loanState);crew.Arsenal=arsenal;
  var loanCat=new MissionCatalog();var probe=Def("X01","main");loanCat.All.Add(probe);var rewardDef=Def("M03","main");loanCat.All.Add(rewardDef);
  var ice=crew.PedFor(CrewSlot.Ice);ice.Weapons.Give(WeaponHash.Pistol,60,false,true);arsenal.Capture(CrewSlot.Ice,ice);
  Check(loanState.Weapons["Ice"].Contains((uint)WeaponHash.Pistol),"Ice legitimately owns a pistol before the job");
  manager=new MissionManager(c,loanState,loanCat);
  Check(manager.Start(probe)&&arsenal.LoanActive,"Mission start opens the loan");
  ice.Weapons.Give(WeaponHash.MG,400,false,true);
  for(int i=0;i<4;i++){Game.GameTime+=5000;arsenal.Update(crew,captureAllowed:true);}
  Check(!loanState.Weapons["Ice"].Contains((uint)WeaponHash.MG),"Even a capture tick that is allowed cannot record a loan while the mission runs");
  manager.ForcePass();manager.Update();
  Check(!manager.IsRunning&&!arsenal.LoanActive,"Pass closes the loan");
  Check(!ice.Weapons.Owned.Contains(WeaponHash.MG)&&ice.Weapons.Owned.Contains(WeaponHash.Pistol),"The mission MG is taken back; the pre-owned pistol stays on the hero");
  for(int i=0;i<6;i++){Game.GameTime+=5000;arsenal.Update(crew,captureAllowed:true);}
  arsenal.Apply(CrewSlot.Ice,ice,restock:true);
  Check(!loanState.Weapons["Ice"].Contains((uint)WeaponHash.MG)&&!ice.Weapons.Owned.Contains(WeaponHash.MG),"Free-roam capture ticks and a locker restock never resurrect the loan");
  Check(loanState.Weapons["Ice"].Contains((uint)WeaponHash.Pistol),"The pre-owned pistol is still in the locker");
  // Reward path: the mission's own permanent reward survives the return.
  Check(manager.Start(rewardDef)&&c.Cutscenes.IsActive&&arsenal.LoanActive,"Reward probe opens its loan before the briefing");
  c.Cutscenes.Skip();manager.Update();Check(manager.IsRunning&&manager.CurrentStage==0,"Briefing skipped, gameplay begins");
  ice.Weapons.Give(WeaponHash.MG,400,false,true);manager.ForcePass();manager.Update();c.Cutscenes.Stop();
  Check(loanState.IsComplete("M03")&&loanState.Weapons["Ice"].Contains((uint)WeaponProgression.Rewards("M03")[(int)CrewSlot.Ice]),"The mission's committed reward is owned after the loan closes");
  Check(!ice.Weapons.Owned.Contains(WeaponHash.MG)&&!loanState.Weapons["Ice"].Contains((uint)WeaponHash.MG),"The loan is still returned on a rewarding pass");
  // Abort path.
  Check(manager.Start(probe),"Abort probe starts");ice.Weapons.Give(WeaponHash.SniperRifle,10,false,true);manager.Abort();
  Check(!arsenal.LoanActive&&!ice.Weapons.Owned.Contains(WeaponHash.SniperRifle)&&!loanState.Weapons["Ice"].Contains((uint)WeaponHash.SniperRifle),"Abort returns the loan too");
  // Fail path.
  Check(manager.Start(probe),"Fail probe starts");ice.Weapons.Give(WeaponHash.Minigun,300,false,true);manager.ForceFail("probe");manager.Update();
  Check(!ice.Weapons.Owned.Contains(WeaponHash.Minigun)&&ice.Weapons.Owned.Contains(WeaponHash.Pistol),"Failure returns the loan and keeps what was owned");
  // Starting loadout is never treated as a loan.
  var guess=crew.PedFor(CrewSlot.Guess);foreach(var w in Protagonist.Guess.Loadout)guess.Weapons.Give(w,100,false,true);
  Check(manager.Start(probe),"Loadout probe starts");manager.Abort();
  Check(Protagonist.Guess.Loadout.All(w=>guess.Weapons.Owned.Contains(w)),"A hero's standard loadout survives the loan return");

  // ---- 3. Technical choice: arrival frame and Detonate suppression.
  Reset();crew=Roster();c=Context(crew);int applied=-1;
  var choice=new TechnicalChoiceObjective("Cut a system",()=>new Vector3(5,5,0),new[]{new TechnicalOption("A","a",x=>applied=0),new TechnicalOption("B","b",x=>applied=1)});
  choice.RequiredCharacter=CrewSlot.Gohan;choice.Enter(c);Use(crew,CrewSlot.Gohan);Game.Player.Character.Position=new Vector3(5,5,0);
  Game.Accept=true;Game.Pressed.Add(GTA.Control.Detonate);choice.Update(c);
  Check(!choice.IsFinished&&choice.SelectedIndex==0&&!Game.Accept&&!Game.Pressed.Contains(GTA.Control.Detonate),"The arrival frame consumes both buttons and commits nothing");
  Check(Game.Disabled.Contains(GTA.Control.Detonate),"The Detonate control is disabled while the panel owns it, so cycling cannot throw a detonator");
  choice.Update(c);Check(!choice.IsFinished,"No input on the next frame leaves the choice open");
  Game.Pressed.Add(GTA.Control.Detonate);choice.Update(c);Check(choice.SelectedIndex==1&&!choice.IsFinished,"Cycling reads the disabled control, not the live one");
  Game.Accept=true;choice.Update(c);Check(choice.IsFinished&&applied==1,"A fresh confirm press commits the highlighted option");
  var dialogue=File.ReadAllText(Path.Combine(dataDir,"dialogue.tsv"));
  Check(!Regex.IsMatch(dialogue,@"M28_S1_0\d_\w+\t.*\t(?:[^\t]*\t){2}[^\t]*(?:D-pad|press E|Detonate)",RegexOptions.IgnoreCase),"M28's spoken lines carry no button instructions; the panel HUD does");

  // ---- 4. Skip versus cancel in the cutscene director.
  Reset();crew=Roster();c=Context(crew);var ron=Game.Player.Character;var car=new Vehicle{Position=new Vector3(30,0,0)};
  SceneBlocking Walk(Ped a,Vehicle v)=>new SceneBlocking().Then(new WalkToStep(a,v.Position,1f)).Then(new EnterVehicleStep(a,v,VehicleSeat.Driver));
  var next=typeof(CutsceneDirector).GetMethod("NextLine",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
  // deliberate skip
  c.Cutscenes.Play("M01","prologue","Arrival",null,null,Walk(ron,car));c.Cutscenes.Update();c.Cutscenes.Skip();
  Check(!c.Cutscenes.IsActive&&ron.IsInVehicle(car)&&Game.Player.CanControlCharacter,"Deliberate skip finishes the blocking: seated, in control");
  // cancel (abort/teardown) mid-walk
  Reset();crew=Roster();c=Context(crew);ron=Game.Player.Character;car=new Vehicle{Position=new Vector3(30,0,0)};var walk=Walk(ron,car);
  c.Cutscenes.Play("M01","prologue","Arrival",null,null,walk);c.Cutscenes.Update();int clears=ron.Task.Clears;c.Cutscenes.Stop();
  Check(!c.Cutscenes.IsActive&&!ron.IsInVehicle()&&ron.Position!=car.Position&&Game.Player.CanControlCharacter&&walk.Canceled&&ron.Task.Clears>clears,"Cancel stops the walk where it is, restores control and warps nobody");
  // director error mid-scene
  Reset();crew=Roster();c=Context(crew);ron=Game.Player.Character;car=new Vehicle{Position=new Vector3(30,0,0)};walk=Walk(ron,car);
  c.Cutscenes.Play("M01","prologue","Arrival",null,null,walk);c.Cutscenes.Update();GTA.Native.Function.ThrowOnce=GTA.Native.Hash.SET_FOCUS_POS_AND_VEL;c.Cutscenes.Update();
  Check(!c.Cutscenes.IsActive&&!ron.IsInVehicle()&&Game.Player.CanControlCharacter&&walk.Canceled,"A script error halfway through the walk is not a skip: no warp into the car, control restored");
  // mission abort while its intro scene runs
  Reset();crew=Roster();c=Context(crew);ron=Game.Player.Character;car=new Vehicle{Position=new Vector3(30,0,0)};walk=Walk(ron,car);
  var abortState=CampaignState.Load(Path.Combine(root,"scene-abort.json"));var abortCat=new MissionCatalog();var m02=Def("M02","main");abortCat.All.Add(m02);
  manager=new MissionManager(c,abortState,abortCat);Check(manager.Start(m02)&&c.Cutscenes.IsActive,"M02's intro plays through the manager");
  // give the running intro a walk, then abort the mission
  typeof(CutsceneDirector).GetField("_blocking",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(c.Cutscenes,walk);walk.Update();
  manager.Abort();
  Check(!c.Cutscenes.IsActive&&!manager.IsRunning&&!ron.IsInVehicle(car)&&walk.Canceled,"Mission abort cancels the scene's blocking rather than completing it");
  // watchdog timeout is a cancel
  Reset();crew=Roster();c=Context(crew);ron=Game.Player.Character;car=new Vehicle{Position=new Vector3(30,0,0)};walk=Walk(ron,car);
  c.Cutscenes.Play("M01","prologue","Arrival",null,null,walk);c.Cutscenes.Update();Game.GameTime+=240001;c.Cutscenes.Update();
  Check(!c.Cutscenes.IsActive&&!ron.IsInVehicle()&&walk.Canceled,"The scene watchdog cancels; it does not fake a skip");
  // natural completion has nothing to cancel
  Reset();crew=Roster();c=Context(crew);ron=Game.Player.Character;car=new Vehicle{Position=new Vector3(30,0,0)};walk=Walk(ron,car);
  c.Cutscenes.Play("M01","prologue","Arrival",null,null,walk);
  for(int i=0;i<30&&c.Cutscenes.IsActive;i++){c.Cutscenes.Update();ron.Position=car.Position;if(ron.Task.Enters>0)ron.SetIntoVehicle(car,VehicleSeat.Driver);c.Dialogue.Clear();}
  Check(!c.Cutscenes.IsActive&&walk.IsFinished&&!walk.Canceled&&ron.IsInVehicle(car),"A scene that plays out ends with the blocking finished, not canceled");

  // ---- 5. Deterministic Finish for exit-vehicle and the phone.
  Reset();var driver=new Ped();var ride=new Vehicle{Position=new Vector3(100,100,10),Heading=90,ForwardVector=new Vector3(1,0,0)};driver.SetIntoVehicle(ride,VehicleSeat.Driver);
  var exit=new ExitVehicleStep(driver);exit.Finish();
  Check(!driver.IsInVehicle()&&driver.Position!=ride.Position&&driver.Position.DistanceTo(ride.Position)<4f&&driver.Heading==90&&driver.Task.Clears>0,"Finishing an exit leaves the actor already outside, beside the car, facing its way");
  Check(GTA.Native.Function.Calls.Any(call=>call.Item1==GTA.Native.Hash.REQUEST_COLLISION_AT_COORD),"The skip position asks for collision before placing the actor");
  var expected=ExitVehicleStep.SafeSpotBeside(ride);Check(driver.Position==expected,"The actor stands on the computed safe spot");
  // Fail-safe: an engine that will not unseat the actor fails the step and stops the skip there.
  var stuck=new Ped{StuckInSeat=true};var stuckRide=new Vehicle{Position=new Vector3(200,200,10)};stuck.SetIntoVehicle(stuckRide,VehicleSeat.Driver);var bystander=new Ped{Position=Vector3.Zero};
  var chain=new SceneBlocking().Then(new ExitVehicleStep(stuck)).Then(new WalkToStep(bystander,new Vector3(9,9,0),1f));
  chain.Complete();
  Check(chain.Steps[0].Failed&&stuck.IsInVehicle(stuckRide)&&stuck.Position==stuckRide.Position,"A refused warp-out marks the exit step failed instead of pretending the actor is outside");
  Check(chain.Canceled&&chain.IsFinished&&bystander.Position==Vector3.Zero,"A failed step cancels the rest of the skip; later steps do not run against a false state");
  Check(!ExitVehicleStep.ForceOut(stuck)&&ExitVehicleStep.ForceOut(new Ped()),"ForceOut reports honestly: false when still seated, true when already out");
  // Prologue cold-open placement: out first, then the dock.
  var dockPoint=new Vector3(1073,-3160,5.9f);var traveler=new Ped();var cab=new Vehicle{Position=new Vector3(291,-1078,29)};traveler.SetIntoVehicle(cab,VehicleSeat.Driver);
  Check(PrologueSequence.PlaceForColdOpen(traveler,dockPoint)&&!traveler.IsInVehicle()&&traveler.Position==dockPoint&&cab.Position!=dockPoint,"The cold open unseats the player before moving them; the car stays behind");
  var glued=new Ped{StuckInSeat=true};var gluedCab=new Vehicle{Position=new Vector3(291,-1078,29)};glued.SetIntoVehicle(gluedCab,VehicleSeat.Driver);
  Check(!PrologueSequence.PlaceForColdOpen(glued,dockPoint)&&glued.IsInVehicle(gluedCab)&&gluedCab.Position==dockPoint&&glued.Position==dockPoint,"If the player cannot be unseated, the vehicle travels with them: a known state, reported as such");
  Check(PrologueSequence.PlaceForColdOpen(new Ped{Position=Vector3.Zero},dockPoint),"A player on foot is simply placed");
  var caller=new Ped();var phone=new UsePhoneStep(caller,3000);phone.Start();phone.Finish();Check(caller.Task.Clears>0,"Finishing a phone step puts the phone away immediately");
  var walker=new Ped();var to=new WalkToStep(walker,new Vector3(7,7,0),1f);to.Start();to.Finish();Check(walker.Position==new Vector3(7,7,0)&&walker.Task.Clears>0,"Finishing a walk places the actor on the mark with tasks cleared");
  var rider=new Ped();var seat=new Vehicle();var enter=new EnterVehicleStep(rider,seat,VehicleSeat.Driver);enter.Start();enter.Finish();Check(rider.IsInVehicle(seat)&&rider.SeatIndex==VehicleSeat.Driver,"Finishing an entry seats the actor now");
  // Cancel mid-step stands the actor down without moving it.
  var stroller=new Ped{Position=new Vector3(1,1,0)};var stroll=new WalkToStep(stroller,new Vector3(50,50,0),1f);stroll.Start();stroll.Cancel();
  Check(stroller.Position==new Vector3(1,1,0)&&stroller.Task.Clears>0,"Canceling a walk clears the task and leaves the actor where it stood");

  // ---- 6. M21's lift waits for the escort stage.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"m21-hold.json"));var m21=new M21OpenWater();
  Check(m21.Begin(c),"M21 starts");var pilot=crew.PedFor(CrewSlot.Guess);var lift=pilot.CurrentVehicle;
  Check(pilot.Task.HeliTasks==0&&lift!=null&&lift.IsPositionFrozen,"During setup the loaded lift is held in place, rotors up, no flight task");
  var flow=Flow(m21);Drive(c,crew,m21,flow);m21.Tick();
  Check(m21.CurrentStage==1&&pilot.Task.HeliTasks==1&&!lift.IsPositionFrozen,"The flight to the breakwater starts only when the escort stage begins");
  m21.Abort();Check(!lift.IsPositionFrozen,"Teardown never leaves a held lift pinned in the air");
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"m21-hold-fail.json"));m21=new M21OpenWater();m21.Begin(c);lift=crew.PedFor(CrewSlot.Guess).CurrentVehicle;m21.Fail("probe");
  Check(!lift.IsPositionFrozen,"A failure during the boarding stage releases the held lift");

  // ---- 8 / 9 / 10 / 11. Data.
  string scenes=File.ReadAllText(Path.Combine(dataDir,"scenes.tsv"));
  Check(scenes.Contains("Guess I'll find out if either one still works.")&&scenes.Contains("Voicemail. Both of them."),"The prologue tries the old numbers and nobody answers, matching M01's reunion");
  Check(!scenes.Contains("Not tonight. Tonight I find out where I'm sleeping."),"The contradicted prologue line is gone");
  Check(CampaignDispatches.All.First(m=>m.Mission=="M27").Text.StartsWith("You got me out."),"Ice's M27 dispatch speaks as the one who was extracted");
  Check(scenes.Contains("Breakfast. Three seats. I'm driving."),"The quiet M70 aftermath is preserved");
  Check(dialogue.Contains("Told y'all it still had engines.")&&!dialogue.Contains("Guess never misses an exit"),"M70's gameplay sets the exit up without the payoff line");
  var m70Outro=File.ReadAllLines(Path.Combine(dataDir,"scenes.tsv")).Skip(1).Select(l=>l.Split('\t')).Where(f=>f[1]=="M70"&&f[2]=="outro").ToArray();
  Check(m70Outro.Last()[5]=="Told y'all... Guess never misses an exit."&&m70Outro.Last()[3]=="GUESS","The locked line is the final spoken line of M70");
  Check(Array.IndexOf(m70Outro.Select(f=>f[5]).ToArray(),"Breakfast. Three seats. I'm driving.")==m70Outro.Length-2,"The breakfast exchange comes right before it and nothing follows it");
  Check(scenes.IndexOf("M70_SCENE_OUTRO",StringComparison.Ordinal)>=0&&!scenes.Substring(scenes.LastIndexOf("Guess never misses an exit",StringComparison.Ordinal)).Contains("\tM70\t"),"No M70 cue is written after the final line");
 }
}
