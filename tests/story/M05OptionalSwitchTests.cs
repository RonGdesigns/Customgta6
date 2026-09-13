using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
public static partial class StoryTests
{
 static void M05BoatDisableChecks()
 {
  Reset();var crew=Roster();var c=Context(crew);var m=new M05TidalLock();Check(m.Begin(c),"M05 player-driven chase starts");c.Cutscenes.Skip();
  foreach(var enemy in World.Created.Where(p=>p!=m.Mateo))enemy.IsDead=true;m.Tick();
  Check(crew.CompanionAI.Controlled.Contains(CrewSlot.Guess)&&crew.PedFor(CrewSlot.Guess).IsInVehicle(m.Dinghy),"Flare stage keeps Guess's distant boat station protected from the companion leash");
  Interact(m,c,CrewSlot.Guess,c.Locations.Position("M05.CoveAir"),4,true);m.Tick();
  Check(m.CurrentStage==2&&crew.ActiveSlot==CrewSlot.Guess&&m.RequiredSwitch==null,"Firing the flare keeps Guess driving");
  var target=m.Mateo.CurrentVehicle;m.Dinghy.Position=target.Position;Game.Player.Character.Position=m.Dinghy.Position;m.Tick();
  Check(m.CurrentStage==3&&c.Switching.Locked&&crew.ActiveSlot==CrewSlot.Guess,"The chase stays on player-controlled Guess");
  target.Speed=10;m.Dinghy.Speed=10;
  for(int i=0;i<10;i++){Game.GameTime+=1000;m.Tick();}
  float partial=m.DisableProgress;Check(partial>0&&partial<1,"Gohan works automatically from the passenger seat during Guess's chase");
  m.Dinghy.Position=target.Position+new Vector3(80,0,0);Game.GameTime+=1000;m.Tick();Game.GameTime+=1000;m.Tick();
  Check(m.DisableProgress==partial&&m.CurrentObjective.Contains("paused"),"Moving beyond 45 meters pauses and preserves engine-disable progress");
  m.Dinghy.Position=target.Position;
  for(int i=0;i<26;i++){Game.GameTime+=500;target.Position+=new Vector3(1,0,0);m.Dinghy.Position=target.Position;m.Tick();}
  for(int i=0;i<15&&!m.BoatDisabled;i++){Game.GameTime+=500;m.Tick();}
  Check(m.BoatDisabled&&target.EngineHealth==1&&!target.IsDriveable&&m.Mateo.IsAlive&&m.CurrentStage==3,"A completed remote disable stops engine power, preserves the witness and waits while the boats still move");
  target.Speed=0;m.Dinghy.Speed=0;m.Dinghy.Position=target.Position+new Vector3(30,0,0);m.Tick();Check(m.CurrentStage==3,"Stopping too far away does not hand control to Gohan");
  m.Dinghy.Position=target.Position+new Vector3(10,0,0);Game.Player.Character.Position=m.Dinghy.Position;m.Tick();m.Tick();
  Check(m.CurrentStage==4&&!c.Switching.Locked&&m.RequiredSwitch==CrewSlot.Gohan,"Only the stopped alongside rendezvous opens Gohan's boarding objective");
  Interact(m,c,CrewSlot.Gohan,m.Mateo.Position,2,true);Check(m.CurrentStage==5&&c.Cutscenes.IsActive,"Gohan performs the stopped-boat interaction");
  c.Cutscenes.Skip();m.Tick();Check(m.Status==MissionStatus.Passed,"The new chase-disable-board flow finishes M05");
  for(int retry=0;retry<2;retry++)
  {
   m=new M05TidalLock();Check(m.Begin(c),"M05 starts on a reused roster, attempt "+retry);c.Cutscenes.Skip();
   foreach(var enemy in World.Created.Where(p=>p.Model.Name=="g_m_y_mexgoon_02"))enemy.IsDead=true;m.Tick();
   Check(m.CurrentStage==1&&crew.ActiveSlot==CrewSlot.Ice&&crew.CompanionAI.Controlled.Contains(CrewSlot.Guess)&&crew.PedFor(CrewSlot.Guess).CurrentVehicle==m.Dinghy,"Retry retains Guess in this attempt's dinghy while Ice remains active");m.Abort();
  }
  Reset();crew=Roster();c=Context(crew);Use(crew,CrewSlot.Guess);var chase=new Vehicle();Game.Player.Character.SetIntoVehicle(chase,VehicleSeat.Driver);var witness=new Ped();target=new Vehicle();witness.SetIntoVehicle(target,VehicleSeat.Driver);
  var hack=new BoatDisableObjective(()=>witness,()=>target,()=>chase);hack.Enter(c);
  for(int i=0;i<30;i++){Game.GameTime+=1000;hack.Update(c);}
  Check(hack.Progress==0&&!hack.Disabled,"A missing passenger cannot perform the remote disable");
  witness.IsDead=true;hack.Update(c);Check(hack.Status==ObjectiveStatus.Failed,"A dead witness fails the new disable objective");
 }
}
