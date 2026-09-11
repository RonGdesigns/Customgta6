using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;
public static partial class StoryTests
{
 static void IroncladChecks()
 {
  // ---- M09: the convoy seen, the escort stopped not burned, the colonel let go on camera, the unit in hand and read.
  Reset();var crew=Roster();var c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"iron9.json"));var m9=new M09RollingThunder();
  Check(m9.Begin(c)&&c.Cutscenes.IsActive&&m9.Frogger!=null&&m9.EscortTruck!=null&&m9.EndpointKind==MissionEndpoint.SecuredDelivery,"M09 opens on the convoy, the escort, Ice's rock and Ron's flat; the drop is a secured delivery, not a safehouse");
  c.Cutscenes.Skip();m9.Tick();Check(m9.CurrentStage==0,"Skipping the approach leaves Ron at the Frogger");
  Use(crew,CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(m9.Frogger,VehicleSeat.Driver);m9.Tick();Check(m9.CurrentStage==1,"Airborne, the convoy rolls and the shadow begins");
  var escort=m9.EscortTruck;var escortDriver=World.Created.First(p=>p.CurrentVehicle==escort);
  m9.Frogger.Position=escort.Position+new Vector3(0,-120,50);Game.Player.Character.Position=m9.Frogger.Position;
  for(int i=0;i<27;i++){Game.GameTime+=1000;m9.Tick();}
  Check(m9.CurrentStage==2,"Held on the ridgeline behind the convoy, the escort is Ice's to take");
  Use(crew,CrewSlot.Ice);escort.IsDead=true;m9.Tick();Check(m9.Status==MissionStatus.Failed&&m9.FailReason.Contains("burned"),"Destroying the escort loses the unit with it");
  // Again, the right way.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"iron9b.json"));m9=new M09RollingThunder();m9.Begin(c);c.Cutscenes.Skip();m9.Tick();
  Use(crew,CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(m9.Frogger,VehicleSeat.Driver);m9.Tick();escort=m9.EscortTruck;escortDriver=World.Created.First(p=>p.CurrentVehicle==escort);
  m9.Frogger.Position=escort.Position+new Vector3(0,-120,50);Game.Player.Character.Position=m9.Frogger.Position;for(int i=0;i<27;i++){Game.GameTime+=1000;m9.Tick();}
  Use(crew,CrewSlot.Ice);escortDriver.IsDead=true;m9.Tick();
  Check(m9.CurrentStage==3&&m9.Withdrawn&&!escort.IsDriveable&&World.Created.Where(p=>p.CurrentVehicle!=null&&p.CurrentVehicle!=escort&&p.Model.Name=="s_m_y_marine_01").All(p=>p.Task.Drives>=2),"The escort stops where its driver died; the colonel's vehicles are sent away");
  m9.Tick();Check(m9.WithdrawalShown&&c.Cutscenes.IsActive,"The colonel leaving is shown once, so the player knows why nobody chases him");
  c.Cutscenes.Skip();m9.Tick();GTA.UI.Screen.Subtitle=null;Interact(m9,c,CrewSlot.Ice,escort.Position,6);
  Check(m9.CurrentStage==4&&c.Cutscenes.IsActive&&m9.Unit!=null&&m9.Unit.Exists()&&m9.Unit.AttachedTo==Game.Player.Character,"The unit comes out of the cab into Ice's hand, as a scene from his own line");
  c.Cutscenes.Skip();m9.Tick();Check(c.Dialogue.HasPending,"Gohan reads the unit remotely: one gate, one use");
  Use(crew,CrewSlot.Guess);m9.Frogger.Position=c.Locations.Position("M09.Pickup");m9.Frogger.HeightAboveGround=0f;m9.Frogger.Speed=0f;Game.Player.Character.SetIntoVehicle(m9.Frogger,VehicleSeat.Driver);m9.Tick();
  Check(m9.CurrentStage==5,"Ron lands on the flat past the culvert, not on the cab roof");
  Use(crew,CrewSlot.Ice);Game.Player.Character.SetIntoVehicle(m9.Frogger,VehicleSeat.RightFront);m9.Tick();Check(m9.CurrentStage==6,"Ice aboard, the drop is the job");
  m9.Frogger.Position=c.Locations.Position("M09.Bunker");Game.Player.Character.Position=m9.Frogger.Position;m9.Tick();c.Dialogue.Clear();m9.Tick();
  Check(m9.Status==MissionStatus.Passed&&c.State.CargoAt("iffTransponder")=="M09.Bunker","The unit is recorded at the drop; M16 knows where it is");
  string m9src=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act1","M09RollingThunder.cs"));
  Check(m9src.Contains("Phase = \"approach\"")&&m9src.Contains("Phase = \"unit\"")&&m9src.Contains("PlayMoment(Id, \"The colonel leaves\"")&&m9src.Contains("land: true"),"M09 has the approach, the unit scene, the withdrawal moment and a landed pickup");

  // ---- M10: the same crates from the stash, the window named, the engines at the shop.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"iron10.json"));c.State.SetCargo("turbineEngines","M08.Connector");var m10=new M10OpenThrottle();
  Check(m10.Begin(c)&&m10.Crates.Count==2&&m10.Crates.All(cr=>cr.AttachedTo==m10.Flatbed)&&m10.Flatbed.Position==c.Locations.Position("M08.Connector")&&m10.EndpointKind==MissionEndpoint.SafehouseArrival,"M10 starts at the stash with both crates on the flatbed where M08 left them");
  GTA.UI.Screen.Subtitle=null;Interact(m10,c,CrewSlot.Guess,m10.Flatbed.Position-m10.Flatbed.ForwardVector*5f,3);
  Check(m10.CurrentStage==1&&c.Cutscenes.IsActive,"Checking the load plays the inspection");c.Cutscenes.Skip();m10.Tick();
  Game.Player.Character.SetIntoVehicle(m10.Flatbed,VehicleSeat.Driver);m10.Tick();
  Check(m10.CurrentStage==2&&crew.PedFor(CrewSlot.Ice).IsInVehicle(m10.Flatbed)&&World.Created.Count(p=>p.Model.Name=="g_m_y_mexgoon_03")==3,"Rolling out seats Ice beside Ron and the bikes come");
  m10.Flatbed.Speed=22f;foreach(var rider in World.Created.Where(p=>p.Model.Name=="g_m_y_mexgoon_03"))rider.IsDead=true;m10.Tick();
  Check(m10.CurrentStage==3&&m10.Buzzard!=null&&!m10.GunshipShown,"The bikes down, the window is the job and the gunship is in the air behind");
  m10.Tick();Check(m10.GunshipShown&&c.Cutscenes.IsActive,"The gunship is shown coming and the tunnel mouth named as the window");
  c.Cutscenes.Skip();m10.Flatbed.Position=c.Locations.Position("M10.TunnelMouth");m10.Flatbed.Speed=0f;m10.Tick();Check(m10.CurrentStage==4,"At the tunnel mouth the fight is Ice's");
  Use(crew,CrewSlot.Ice);m10.Buzzard.IsDead=true;m10.Tick();Check(m10.CurrentStage==5,"The Buzzard down, the shop is the job");
  Use(crew,CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(m10.Flatbed,VehicleSeat.Driver);Game.Player.WantedLevel=0;m10.Flatbed.Position=c.Locations.Position("M11.ChopShop");m10.Tick();c.Dialogue.Clear();m10.Tick();
  Check(m10.Status==MissionStatus.Passed&&m10.Delivered&&c.State.CargoAt("turbineEngines")=="M11.ChopShop"&&m10.OutroBlocking()!=null,"The engines are recorded at the shop, the truck shut down, the aftermath on the load");
  m10.Cleanup();Check(m10.Flatbed.Exists()&&World.Props.Any(p=>p.Exists()&&p.AttachedTo==m10.Flatbed),"The flatbed and its crates stay at the shop for M11");
  var starts=File.ReadAllLines(Path.Combine(dataDir,"mission_starts.tsv"));Check(starts.Any(l=>l=="M10\tM08.Connector"),"M10 starts from the stash marker");

  // ---- M11: the shop mid-work, the calibration, Berth 44 in the room, the truck shown.
  Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"iron11.json"));var m11=new M11IroncladDyno();
  Check(m11.Begin(c)&&c.Cutscenes.IsActive&&m11.Flatbed!=null&&m11.Crate!=null&&m11.Crate.AttachedTo==m11.Flatbed,"M11 opens on the shop mid-work, with the second crate still on the flatbed");
  c.Cutscenes.Skip();m11.Tick();Check(m11.CurrentStage==0,"No briefing: the mounts are the first job");
  GTA.UI.Screen.Subtitle=null;Interact(m11,c,CrewSlot.Guess,c.Locations.Position("M11.DynoPad"),10);Check(m11.CurrentStage==1,"Mounts fabricated, Ice is called to the dyno");
  Use(crew,CrewSlot.Ice);Game.Player.Character.SetIntoVehicle(m11.Granger,VehicleSeat.Driver);m11.Tick();Check(m11.CurrentStage==2,"In the seat, the calibration is the mechanic");
  m11.Granger.CurrentRPM=0.62f;GameUtils.LastProgress=-1f;for(int i=0;i<30&&m11.CurrentStage==2;i++){Game.GameTime+=500;m11.Tick();}
  Check(m11.CurrentStage==3&&m11.BerthPlayed&&c.Cutscenes.IsActive&&GameUtils.LastProgress>0f,"Held in the band with the meter filling, the engine is done and Gohan comes to the window with Berth 44");
  c.Cutscenes.Skip();m11.Tick();c.Dialogue.Clear();m11.Tick();
  Check(m11.Status==MissionStatus.Passed&&c.State.CargoAt("turbineEngines")=="M11.ChopShop"&&m11.OutroBlocking()!=null,"M11 ends with the second engine kept at the shop and the upgraded Granger shown");
  string m11src=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act1","M11IroncladDyno.cs"));
  Check(m11src.Contains("Phase = \"shop\"")&&m11src.Contains("Phase = \"berth\"")&&m11src.Contains("Ctx.Data?.Cue(\"M11_S1_05_GOHAN\")")&&m11src.Contains("A claim, not cash"),"M11 has the shop scene and Berth 44 staged from the stage-four lines; three billion is a figure, not money");
  var scenes=File.ReadAllLines(Path.Combine(dataDir,"scenes.tsv"));
  Check(scenes.Count(l=>l.StartsWith("M09_SCENE_APPROACH"))==2&&scenes.Count(l=>l.StartsWith("M10_SCENE_INSPECT"))==2&&scenes.Count(l=>l.StartsWith("M11_SCENE_SHOP"))==2,"The three new phases have two authored lines each in the scene data");
 }
}
