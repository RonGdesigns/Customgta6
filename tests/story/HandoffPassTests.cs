using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

/// <summary>A mission whose one required vehicle has no stage-level protect objective.</summary>
public sealed class AssetProbeMission : ComposedMission
{
 public Vehicle Truck=new Vehicle{Position=new Vector3(50,50,0)};
 public override string Id=>"M24";public override string Title=>"Asset probe";
 protected override bool Setup(){RequireAsset(Truck,"The truck is gone.");return true;}
 protected override IEnumerable<MissionStage> BuildStages(){yield return new MissionStage("Wait",new ReachZoneObjective("Reach",()=>new Vector3(9999,9999,0),2f)).OwnedBy(CrewSlot.Guess);}
}

public static partial class StoryTests
{
 static void HandoffPassChecks()
 {
  // ---- 1. End of implemented content is its own state; nothing falls back to M01.
  Reset();var path=Path.Combine(root,"progress-audit.json");var state=CampaignState.Load(path);var cat=new MissionCatalog();
  MissionDefinition Def(string id,string kind,string prereq="")=>new MissionDefinition{Info=new MissionInfo{Id=id,Title=id,Kind=kind,Prerequisite=prereq},Factory=()=>new AuditMission()};
  cat.All.Add(Def("M01","main"));cat.All.Add(Def("M02","main","M01"));cat.All.Add(Def("SM01","solo","M01"));
  Check(state.Progress(cat)==CampaignProgress.StoryAvailable&&state.DescribeProgress(cat).Contains("M01"),"Fresh campaign reports the next story mission");
  state.MarkComplete("M01",cat);state.MarkComplete("M02",cat);
  Check(state.Progress(cat)==CampaignProgress.SideContentOnly&&state.NextPlayable(cat).Id=="SM01","Story caught up reports optional side content, not M01");
  state.MarkComplete("SM01",cat);
  Check(state.Progress(cat)==CampaignProgress.ImplementedContentComplete&&state.NextPlayable(cat)==null&&state.CurrentMissionId=="","All scripted content complete reports completion and never points at M01");
  Check(state.DescribeProgress(cat).Contains("complete")&&!state.DescribeProgress(cat).Contains("M01"),"Completion text names the state rather than the first mission");
  cat.All.Add(Def("M31","main","M30"));
  Check(state.Progress(cat)==CampaignProgress.StoryBlocked,"A story mission behind an unmet prerequisite is reported as blocked, not offered");
  Check(state.PrologueDue==false,"A save that finished M01 never owes the prologue");
  var fresh=CampaignState.Load(Path.Combine(root,"prologue-flag.json"));
  Check(fresh.PrologueDue,"A fresh save owes the prologue before M01");
  fresh.PrologueComplete=true;fresh.Save();
  Check(!CampaignState.Load(Path.Combine(root,"prologue-flag.json")).PrologueDue,"Prologue completion survives reload");
  fresh.Reset();Check(fresh.PrologueDue,"Campaign reset owes the prologue again");

  // ---- 2. Weapon loan policy and the SM01 supply line.
  Reset();var crew=Roster();var c=Context(crew);var loans=CampaignState.Load(Path.Combine(root,"loans.json"));var arsenal=new WeaponProgression(loans);
  var ice=crew.PedFor(CrewSlot.Ice);ice.Weapons.Give(WeaponHash.MG,100,false,true);
  Game.GameTime+=5000;arsenal.Update(crew,captureAllowed:false);
  Check(!loans.Weapons.TryGetValue("Ice",out var owned)||!owned.Contains((uint)WeaponHash.MG),"A mission-issued weapon is a loan: not captured to the locker while a mission runs");
  for(int i=0;i<3;i++){Game.GameTime+=5000;arsenal.Update(crew,captureAllowed:true);}
  Check(loans.Weapons["Ice"].Contains((uint)WeaponHash.MG),"Free-roam capture still records what the hero actually carries");
  ice.Weapons.LastAmmo=0;arsenal.Apply(CrewSlot.Ice,ice,restock:true);int before=ice.Weapons.LastAmmo;
  loans.MarkComplete("SM01",cat);
  Check(loans.FleetUpgrades["armorPiercingSupply"]&&arsenal.HasArmorPiercingSupply(CrewSlot.Ice)&&!arsenal.HasArmorPiercingSupply(CrewSlot.Guess),"SM01 commits Ice's armor-piercing supply line once");
  arsenal.Apply(CrewSlot.Ice,ice,restock:true);
  Check(ice.Weapons.LastAmmo==before*2&&arsenal.RestockCount(CrewSlot.Ice,(uint)WeaponHash.MG,false)==before,"The supply doubles Ice's rifle restock and leaves ordinary top-ups alone");

  // ---- 3. Required assets fail the whole mission, not just a protected stage.
  Reset();crew=Roster();c=Context(crew);var probe=new AssetProbeMission();
  Check(probe.Begin(c)&&probe.Status==MissionStatus.Running,"Asset probe starts");
  probe.Tick();Check(probe.Status==MissionStatus.Running,"A living required vehicle keeps the mission running");
  probe.Truck.IsDriveable=false;probe.Tick();
  Check(probe.Status==MissionStatus.Failed&&probe.FailReason=="The truck is gone.","Losing a required vehicle fails with its reason even in a stage without a protect objective");

  // ---- 4. Handoff ledger semantics.
  var ledger=new HandoffLedger();
  Check(ledger.Take("PortHeist","M21")==null,"No record means default staging");
  var lift=new Vehicle{Position=new Vector3(1,2,3),Heading=45,DisplayName="cargobob"};
  Reset();crew=Roster();crew.PedFor(CrewSlot.Guess).SetIntoVehicle(lift,VehicleSeat.Driver);crew.ActiveSlot=CrewSlot.Guess;
  var record=OperationHandoff.Capture("PortHeist","M20","M21",crew,lift);record.CargoAttached=true;
  Check(record.ActiveHero==CrewSlot.Guess&&record.Seats[CrewSlot.Guess]==VehicleSeat.Driver&&record.VehiclePosition==lift.Position&&record.Positions.Count==3,"Capture records the active hero, seats, vehicle and every position");
  ledger.Record(record);
  Check(ledger.Peek("PortHeist","M22")==null&&ledger.Peek("PortHeist","M21")==record,"A record is addressed to one receiving mission");
  Check(ledger.Take("PortHeist","M21")==record&&ledger.Take("PortHeist","M21")==null,"Taking a record consumes it so a retry rebuilds default staging");

  // ---- 5. Port Heist continuity: M20 records the lift, M21 escorts a visible container from it, M22 consumes.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"port-m20.json"));
  var m20=new M20SkyHook();Check(m20.Begin(c),"M20 starts");
  var m20Flow=Flow(m20);for(int tick=0;tick<600&&m20.Status==MissionStatus.Running;tick++){Drive(c,crew,m20,m20Flow);m20.Tick();}
  Check(m20.Status==MissionStatus.Passed,"M20 completes in the harness");
  var toM21=c.Handoffs.Peek("PortHeist","M21");
  Check(toM21!=null&&toM21.CargoAttached&&toM21.VehicleModel.Length>0,"M20 passing records an airborne lift with the container attached");
  GTA.Native.Function.Calls.Clear();
  var m21=new M21OpenWater();Check(m21.Begin(c),"M21 starts from the M20 record");
  Check(c.Handoffs.Peek("PortHeist","M21")==null,"M21 consumed the M20 record");
  Check(GTA.Native.Function.Calls.Any(call=>call.Item1==GTA.Native.Hash.ATTACH_ENTITY_TO_ENTITY),"M21's escorted lift carries a visible bullion container");
  var lifted=World.Vehicles.LastOrDefault(v=>v.Position==toM21.VehiclePosition);
  Check(lifted!=null,"M21's Cargobob starts where M20's climb-out ended");
  var m21Flow=Flow(m21);for(int tick=0;tick<800&&m21.Status==MissionStatus.Running;tick++){Drive(c,crew,m21,m21Flow);m21.Tick();}
  Check(m21.Status==MissionStatus.Passed&&c.Handoffs.Peek("PortHeist","M22")!=null&&c.Handoffs.Peek("PortHeist","M22").CargoAttached,"M21 passing hands a loaded lift to M22");
  var m22=new M22ScorchedBay();Check(m22.Begin(c)&&c.Handoffs.Peek("PortHeist","M22")==null,"M22 consumes the M21 record");m22.Abort();
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"port-m21-cold.json"));GTA.Native.Function.Calls.Clear();
  var cold=new M21OpenWater();Check(cold.Begin(c)&&GTA.Native.Function.Calls.Any(call=>call.Item1==GTA.Native.Hash.ATTACH_ENTITY_TO_ENTITY),"Without a record M21 still shows the bullion under the lift");cold.Abort();

  // ---- 6. Scene blocking: watched and skipped scenes reach the same state.
  Reset();crew=Roster();c=Context(crew);var ron=Game.Player.Character;var car=new Vehicle{Position=new Vector3(10,0,0)};
  SceneBlocking Build(Ped actor,Vehicle ride)=>new SceneBlocking().Then(new UsePhoneStep(actor,1000)).Then(new WalkToStep(actor,ride.Position,1f)).Then(new EnterVehicleStep(actor,ride,VehicleSeat.Driver));
  var watched=Build(ron,car);ron.IsPositionFrozen=true;
  watched.Update();Check(ron.Task.Phones==1&&watched.Current is UsePhoneStep,"First step starts the phone");
  Game.GameTime+=1100;watched.Update();watched.Update();Check(watched.Current is WalkToStep&&ron.Task.Gotos==1&&!ron.IsPositionFrozen,"Phone done, the walk starts and the actor is unfrozen");
  ron.Position=car.Position;watched.Update();watched.Update();Check(watched.Current is EnterVehicleStep&&ron.Task.Enters==1,"Arriving at the door starts the vehicle entry");
  ron.SetIntoVehicle(car,VehicleSeat.Driver);watched.Update();watched.Update();Check(watched.IsFinished,"Seated actor finishes the blocking");
  var ron2=new Ped{Position=Vector3.Zero};var car2=new Vehicle{Position=new Vector3(10,0,0)};var skipped=Build(ron2,car2);skipped.Complete();
  Check(skipped.IsFinished&&ron2.IsInVehicle(car2)&&ron2.SeatIndex==VehicleSeat.Driver,"Skipping completes every step: the actor ends seated exactly as if watched");
  var slow=new SceneBlocking().Then(new WalkToStep(new Ped(),new Vector3(100,0,0),1f){TimeoutMs=500});slow.Update();Game.GameTime+=600;slow.Update();
  Check(slow.IsFinished&&slow.Steps[0].Actor.Position==new Vector3(100,0,0),"A stuck step times out into its finished state");
  // Director integration: the scene waits for blocking after the last line and finishes it on skip.
  Reset();crew=Roster();c=Context(crew);ron=Game.Player.Character;car=new Vehicle{Position=new Vector3(3,0,0)};
  var blocking=new SceneBlocking().Then(new WalkToStep(ron,car.Position,1f)).Then(new EnterVehicleStep(ron,car,VehicleSeat.Driver));
  Check(c.Cutscenes.Play("M01","prologue","Arrival",null,null,blocking)&&!ron.IsPositionFrozen,"A scene with blocking leaves its mover unfrozen");
  var next=typeof(CutsceneDirector).GetMethod("NextLine",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
  for(int i=0;i<6;i++)next.Invoke(c.Cutscenes,null);
  Check(c.Cutscenes.IsActive,"Dialogue exhausted but the actor still walking keeps the scene open");
  Game.Accept=true;c.Cutscenes.Update();
  Check(!c.Cutscenes.IsActive&&ron.IsInVehicle(car)&&Game.Player.CanControlCharacter,"Skipping the scene seats the actor and returns control");
  Reset();crew=Roster();c=Context(crew);ron=Game.Player.Character;car=new Vehicle{Position=new Vector3(3,0,0)};
  blocking=new SceneBlocking().Then(new EnterVehicleStep(ron,car,VehicleSeat.Driver));
  c.Cutscenes.Play("M01","prologue","Arrival",null,null,blocking);ron.IsDead=true;c.Cutscenes.Stop();
  Check(!ron.IsInVehicle(car),"A dead player is not warped into a car by scene cleanup");

  // ---- 7. Prologue: LSIA -> drive -> door -> handoff, with skip equivalence.
  Reset();crew=Roster();c=Context(crew);var save=CampaignState.Load(Path.Combine(root,"prologue-run.json"));
  var home=new Vector3(291.5f,-1078.7f,29.4f);int handoffs=0;
  var prologue=new PrologueSequence(crew,c.Cutscenes,c.Locations,save,()=>home){Finished=()=>handoffs++};
  Check(prologue.Begin()&&prologue.IsActive&&prologue.Current==PrologueSequence.Phase.Arrival&&crew.IsSolo&&crew.ActiveSlot==CrewSlot.Guess,"Prologue deploys Ron alone at the airport and opens the arrival scene");
  Check(c.Cutscenes.IsActive&&prologue.Car!=null&&World.Vehicles.Contains(prologue.Car),"Ron's car exists for the scene");
  prologue.Update();Check(prologue.Current==PrologueSequence.Phase.Arrival,"The prologue waits while the arrival scene plays");
  Game.Accept=true;c.Cutscenes.Update();
  Check(!c.Cutscenes.IsActive&&Game.Player.Character.IsInVehicle(prologue.Car),"Skipping the arrival leaves Ron seated in his car");
  prologue.Update();Check(prologue.Current==PrologueSequence.Phase.Drive,"Control returns for the drive home");
  prologue.Update();Check(GameUtils.Message!=null&&GameUtils.Message.Contains("Drive"),"HUD, not dialogue, carries the drive instruction");
  var ride=Game.Player.Character.CurrentVehicle;ride.Position=home;Game.Player.Character.Position=home;ride.Speed=5;prologue.Update();
  Check(prologue.Current==PrologueSequence.Phase.Drive,"Rolling past the door does not end the drive");
  ride.Speed=0;prologue.Update();
  Check(prologue.Current==PrologueSequence.Phase.Homecoming&&c.Cutscenes.IsActive,"Stopping at the apartment starts the homecoming scene");
  for(int i=0;i<5;i++)next.Invoke(c.Cutscenes,null);
  Check(c.Cutscenes.IsActive&&handoffs==0,"The homecoming holds until Ron is out of the car and at the door");
  Game.Accept=true;c.Cutscenes.Update();prologue.Update();
  Check(!Game.Player.Character.IsInVehicle()&&Game.Player.Character.Position==home,"Skipping the homecoming puts Ron at his door, out of the car");
  Check(handoffs==1&&!prologue.IsActive&&save.PrologueComplete&&CampaignState.Load(Path.Combine(root,"prologue-run.json")).PrologueComplete,"The prologue commits its flag and hands off to M01 exactly once");
  Check(prologue.Car==null&&ride.Released,"The arrival car is handed back to the world, not deleted under the player");
  // Abort hold skips the whole thing but still counts.
  Reset();crew=Roster();c=Context(crew);save=CampaignState.Load(Path.Combine(root,"prologue-skip.json"));handoffs=0;
  prologue=new PrologueSequence(crew,c.Cutscenes,c.Locations,save,()=>home){Finished=()=>handoffs++};
  prologue.Begin();prologue.Skip();
  Check(handoffs==1&&save.PrologueComplete&&!prologue.IsActive&&!c.Cutscenes.IsActive,"Holding abort skips the arrival, marks it played and starts M01");
  Reset();crew=Roster();c=Context(crew);save=CampaignState.Load(Path.Combine(root,"prologue-nohome.json"));
  prologue=new PrologueSequence(crew,c.Cutscenes,c.Locations,save,()=>null);
  Check(!prologue.Begin()&&!prologue.IsActive&&!save.PrologueComplete,"A missing home location refuses the prologue so M01 can start normally");

  // ---- 8. Gohan's technical choice: cycle, commit, consequence.
  Reset();crew=Roster();c=Context(crew);int applied=-1;
  var choice=new TechnicalChoiceObjective("Cut a system",()=>new Vector3(5,5,0),new[]{
   new TechnicalOption("Feed","slower response",ctx=>applied=0),new TechnicalOption("Dispatch","fewer responders",ctx=>applied=1)});
  choice.RequiredCharacter=CrewSlot.Gohan;choice.Enter(c);Use(crew,CrewSlot.Ice);choice.Update(c);
  Check(choice.Label.StartsWith("Switch to Gohan")&&!choice.IsFinished,"The panel belongs to Gohan");
  Use(crew,CrewSlot.Gohan);Game.Player.Character.Position=new Vector3(5,5,0);Game.Accept=true;choice.Update(c);
  Check(!choice.IsFinished&&choice.SelectedIndex==0,"Arriving at the panel swallows a stale confirm press");
  Game.Pressed.Add(GTA.Control.Detonate);choice.Update(c);
  Check(choice.SelectedIndex==1&&choice.Label.Contains("Dispatch")&&!choice.IsFinished,"D-pad Left / G cycles to the next option without committing");
  Game.Accept=true;choice.Update(c);
  Check(choice.Status==ObjectiveStatus.Complete&&choice.Chosen.Title=="Dispatch"&&applied==1,"Commit applies exactly the chosen consequence");
  // M28 wires the choice into its response clock and squad size.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"m28-choice.json"));var m28=new M28OffTheGrid();
  Check(m28.Begin(c)&&Flow(m28)[2].Objectives[0] is TechnicalChoiceObjective,"M28 asks Gohan to choose before the splice");
  var gap=Field<int>(m28,"_responseGapMs");var size=Field<int>(m28,"_responseSize");
  ((TechnicalChoiceObjective)Flow(m28)[2].Objectives[0]).Options[1].Apply(c);
  Check(Field<int>(m28,"_responseGapMs")<gap&&Field<int>(m28,"_responseSize")<size,"Cutting dispatch first means fewer, faster responders");
  var waves=(SurviveWavesObjective)Flow(m28)[4].Objectives.First(o=>o is SurviveWavesObjective);
  Check(waves.GapProvider()==Field<int>(m28,"_responseGapMs"),"The wave clock reads the decision after it is made, not at build time");
  m28.Abort();

  // ---- 9. Chop-bay race transmission consumes SM03's flag.
  Reset();var garageState=CampaignState.Load(Path.Combine(root,"garage.json"));var garage=new FleetGarage(garageState);var coupe=new Vehicle();
  Check(garage.FitChopBay(coupe)==null&&coupe.Mods[VehicleModType.Transmission].Index==-1,"Before SM03 the chop bay fits nothing");
  garageState.MarkComplete("SM03",cat);
  Check(garage.FitChopBay(coupe)!=null&&coupe.Mods[VehicleModType.Transmission].Index==2&&garage.FitChopBay(coupe)==null,"After SM03 the chop bay fits the race transmission once per vehicle");
  Check(garage.FitChopBay(new Vehicle{Model=new Model("longfin")})==null,"Boats do not take a race transmission");

  // ---- 10. Locked wording and HUD-free speech in the data itself.
  string scenes=File.ReadAllText(Path.Combine(dataDir,"scenes.tsv"));string dialogue=File.ReadAllText(Path.Combine(dataDir,"dialogue.tsv"));
  Check(scenes.Contains("I joke when I'm nervous too. Learn the difference.")&&!scenes.Contains("I joke when I'm scared"),"M10 locked wording is in the scene data");
  Check(scenes.Contains("Sub deployed. That one had my nerves up. Nobody put that in the flight log."),"M42 locked wording is in the scene data");
  Check(scenes.Contains("hear the pressure in my voice")&&!scenes.Contains("hear me scared"),"M25 uses pressure, not fear");
  Check(scenes.Contains("M01\tprologue\tGUESS")&&scenes.Contains("M01\tarrival\tGUESS"),"Prologue and arrival scenes exist for Ron alone");
  var spoken=File.ReadAllLines(Path.Combine(dataDir,"dialogue.tsv")).Skip(1).Select(l=>l.Split('\t')).Where(f=>f.Length>5&&new[]{"M02","M03","M04","M05","M06","M07","M10","M14","M16","M27","M30","SM02","SM03"}.Contains(f[1])).Select(f=>f[5]).ToArray();
  string[] hud={"Switch to","yellow marker","orange marker","marked ","press E","D-pad","No ability needed","thirty-five metres","checkpoints"}; // dialect-ok: matches the bible extraction spelling
  var offenders=spoken.Where(line=>hud.Any(h=>line.IndexOf(h,StringComparison.OrdinalIgnoreCase)>=0)).ToArray();
  Check(offenders.Length==0,"Early-campaign gameplay dialogue carries no HUD or controller instructions: "+string.Join(" | ",offenders));
  Check(dialogue.Contains("They wanted all three of you there.")&&!dialogue.Contains("forty-million-dollar defense contract"),"M05 gives an allegation and a lead, not the whole conspiracy");
  Check(scenes.Contains("Breakfast. Three seats. I'm driving.")&&!scenes.Contains("I let shame decide"),"M70 ends on the quiet boat, not a confession");
 }

 /// <summary>Drive a composed mission through the harness one tick, as CampaignFlowChecks does.</summary>
 static void Drive(MissionContext c,CrewRoster crew,ComposedMission m,List<MissionStage> flow)
 {
  var stage=flow[m.CurrentStage];
  foreach(var objective in stage.Objectives.Where(o=>!o.IsFinished))
  {
   if(objective.RequiredCharacter.HasValue&&!objective.IsPassive)Use(crew,objective.RequiredCharacter.Value);
   string name=objective.GetType().Name;
   if(name=="ReachZoneObjective") PositionActor(c,objective,Field<Func<Vector3>>(objective,"_position")());
   else if(name=="MissionInteraction") {PositionActor(c,objective,Field<Func<Vector3>>(objective,"_position")(),Field<Func<Vehicle>>(objective,"_vehicle")?.Invoke());Game.Accept=true;}
   else if(name=="EnterVehicleObjective") {var v=Field<Func<Vehicle>>(objective,"_vehicle")();PositionActor(c,objective,v.Position,v);}
   else if(name=="DeliverVehicleObjective") PositionActor(c,objective,Field<Func<Vector3>>(objective,"_destination")(),Field<Func<Vehicle>>(objective,"_vehicle")());
   else if(name=="KillTargetsObjective") foreach(var ped in Field<Func<IEnumerable<Ped>>>(objective,"_targets")())ped.IsDead=true;
   else if(name=="ShadowTargetObjective") {var target=Field<Func<Entity>>(objective,"_target")();Game.Player.Character.Position=target.Position;Game.GameTime+=1000;}
  }
  c.Dialogue.Clear();Game.GameTime+=1000;
 }
}
