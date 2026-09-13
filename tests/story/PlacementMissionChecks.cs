using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using GTA;
using GTA.Math;

partial class StoryTests
{
 static void PlacementMissionChecks()
 {
  Reset();var c=Context(Roster());var group=c.Locations.Get("M05.LightCrew");group.SpawnCount=7;group.SpawnRadius=18;
  var m5=new M05TidalLock();Check(m5.Begin(c),"M05 startup accepts the edited generator-crew count");
  var guards=World.Created.Where(p=>p.Model.Name=="g_m_y_mexgoon_02").ToArray();
  Check(guards.Length==7&&guards.All(p=>p.Position.DistanceTo(group.Position)<=18),"M05 actually spawns seven guards inside the edited circle");
  c.Cutscenes.Skip();foreach(var guard in guards)guard.IsDead=true;m5.Tick();
  Check(m5.CurrentStage==1,"The kill objective counts the edited group and advances when it is cleared");m5.Abort();
  Reset();c=Context(Roster());group=c.Locations.Get("M03.DepotGate");group.SpawnCount=6;group.SpawnRadius=22;
  var spawn=new Vector3(830,-1850,29);var stop=new Vector3(820,-1800,29);var crate=new Vector3(810,-1790,29);
  c.Locations.Record("M03.ArrivalSpawn1",spawn,125);c.Locations.Record("M03.ArrivalStop1",stop,0);c.Locations.Record("M03.Crate1",crate,88);
  var m3=new M03CypressFoundry();Check(m3.Begin(c),"M03 validates its edited guard count instead of requiring the old ten");
  guards=((List<Ped>)typeof(M03CypressFoundry).GetField("_guards",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(m3)).ToArray();
  Check(guards.Length==6,"M03 actually creates the configured six yard guards");
  Check(m3.Crates[0].Position==crate&&m3.Crates[0].Heading==88,"An individual M03 crate uses its saved position and facing");
  c.Cutscenes.Skip();typeof(M03CypressFoundry).GetMethod("SpringArrivals",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(m3,null);
  var car=m3.Arrivals[0];Check(car.Position==spawn&&car.Heading==125&&car.GetPedOnSeat(VehicleSeat.Driver).Task.LastDrivePoint==stop,"M03 incoming car uses both the edited entry and actual AI drive destination");
  c.Cutscenes.Stop();m3.Abort();
  Reset();c=Context(Roster());spawn=c.Locations.Position("M06.AlleyHold")+new Vector3(65,0,0);stop=c.Locations.Position("M06.AlleyHold")+new Vector3(12,0,0);
  c.Locations.Record("M06.ConvoySpawn1",spawn,250);c.Locations.Record("M06.ConvoyStop1",stop,0);
  var m6=new M06CleanSweep();Check(m6.Begin(c),"M06 still starts with edited convoy routes");c.Cutscenes.Skip();
  var troopers=new List<Ped>{new Ped(),new Ped(),new Ped(),new Ped()};
  typeof(M06CleanSweep).GetMethod("LaunchConvoys",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(m6,new object[]{troopers});
  Check(troopers.All(p=>!p.IsInVehicle())&&troopers[0].Position==spawn&&m6.Convoys.Count==0,"M06 uses edited entries for foot reinforcements without distant vehicle detours");
  troopers[0].Position=stop;Game.GameTime+=3001;
  typeof(M06CleanSweep).GetMethod("MaintainConvoys",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(m6,null);
  Check(troopers[0].Task.LastTarget==c.Crew.PedFor(Bloodlines.Crew.CrewSlot.Ice),"SWAT entering the alley attack Ice explicitly");m6.Abort();
 }
}
