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
    private sealed class MarineProbe : IMarineProbe
    {
        public Func<Vector3, MarineColumn> At = p => new MarineColumn { Known = true, Surface = 0, Floor = -40 };
        public bool ClearLine = true;
        public int Samples;
        public MarineColumn Column(Vector3 p) { Samples++; return At(p); }
        public bool Clear(Vector3 from, Vector3 to) => ClearLine;
    }
    static void HarborRepairChecks()
    {
        var probe = new MarineProbe(); Vector3 found; string error;
        Check(MarineSites.TryResolve(Vector3.Zero, 12, 8, 10, 0, 0, out found, out error, probe), "Marine: one deep clear footprint is accepted");
        probe.At = p => new MarineColumn { Known = true, Surface = 0, Floor = 15 };
        Check(!MarineSites.TryResolve(Vector3.Zero, 12, 8, 10, 0, 160, out found, out error, probe), "Marine: an ocean water plane beneath a freeway does not make the freeway a launch");
        probe.At = p => new MarineColumn { Known = p.X <= 0, Surface = 0, Floor = -40 };
        Check(!MarineSites.TryResolve(Vector3.Zero, 12, 8, 10, 0, 0, out found, out error, probe), "Marine: wet center and missing starboard footprint is not a valid hull placement");
        probe.At = p => new MarineColumn { Known = true, Surface = 0, Floor = Math.Abs(p.X) < 1 ? -40 : -3 };
        Check(!MarineSites.TryResolve(Vector3.Zero, 12, 8, 10, 0, 0, out found, out error, probe), "Marine: one deep ray cannot certify shallow adjacent work positions");
        probe.At = p => new MarineColumn { Known = true, Surface = 0, Floor = p.X < 20 ? 15 : -40 };
        Check(MarineSites.TryResolve(Vector3.Zero, 12, 8, 10, 0, 80, out found, out error, probe) && found.X >= 40, "Marine: a bounded search can select a genuinely clear alternate, with the entire footprint");
        Check(!MarineSites.TryResolve(Vector3.Zero, 12, 8, 10, 0, 0, out found, out error, probe), "Marine: zero-radius surveyed location is never silently moved");
        probe.At = p => default; probe.Samples=0;
        Check(!MarineSites.TryResolve(Vector3.Zero, 12, 8, 10, 0, 9999, out found, out error, probe) && probe.Samples<=33, "Marine: unknown results fail after a finite bounded search");
        probe.At = p => new MarineColumn { Known = true, Surface = 0, Floor = -40 }; probe.ClearLine=false;
        Check(!MarineSites.TryResolve(Vector3.Zero, 12, 8, 10, 0, 0, out found, out error, probe), "Marine: horizontal obstructions reject otherwise deep water");

        Reset();var crew=Roster();var c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"harbor-repair.json"));
        var m19=new M19UnderwaterBreach();Check(m19.Begin(c),"M19: visible geometry is built before the first objective");
        Check(m19.Hull.Exists()&&m19.Container.Exists()&&m19.Container.Position.Z<0&&!m19.Floated,"M19: a real ship stand-in and submerged container exist BEFORE welding or clamping");
        var site=m19.Hull.Position;m19.Abort();
        var retry=new M19UnderwaterBreach();Check(retry.Begin(c)&&retry.Hull.Position.DistanceTo(site)<.01f,"M19: repeated setup does not drift its anchor by reusing last run's derived work marker");retry.Abort();
        Reset();crew=Roster();c=Context(crew);Function.Seabed=15f;var stage=new M18TheStagingLine();
        Check(!stage.Begin(c)&&!World.Vehicles.Any(v=>v.Model.Name=="submersible2"),"M18: the sub is not released onto a freeway after a failed launch probe");

        Reset();crew=Roster();c=Context(crew);Use(crew,CrewSlot.Gohan);var sub=new Vehicle {Model=new Model("submersible2"),Position=new Vector3(10,20,-8)};
        Game.Player.Character.SetIntoVehicle(sub,VehicleSeat.Driver);Game.Player.Character.Position=sub.Position+new Vector3(0,0,5);
        var work=new MissionInteraction("Cut the seam",()=>sub.Position,1,2,()=>sub);work.RequiredCharacter=CrewSlot.Gohan;work.Enter(c);
        Game.Accept=true;work.Update(c);Game.Accept=false;Game.GameTime+=1100;work.Update(c);
        Check(work.Status==ObjectiveStatus.Complete,"Underwater hold measures the craft center, not the offset pilot origin");
        Check(UnderwaterGuidance.DistanceText(new Vector3(0,0,-3),new Vector3(0,0,-8)).Contains("dive 5"),"Underwater HUD explains the depth difference");
        ObjectiveMarkers.Clear();ObjectiveMarkers.BeginFrame(true);ObjectiveMarkers.ActiveSlot=CrewSlot.Gohan;
        UnderwaterGuidance.Draw(sub,sub.Position,2);ObjectiveMarkers.EndFrame();
        Check(World.LastBlip!=null&&!World.LastBlip.ShowRoute,"Submarine objectives keep map guidance without fictitious road GPS");ObjectiveMarkers.Clear();
        var surface=new SurfaceSubObjective(()=>sub,()=>new Vector3(10,20,0));surface.Enter(c);surface.Update(c);
        Check(!surface.IsFinished,"Being under the surface marker is not the same as surfacing");sub.Position=new Vector3(10,20,-1);surface.Update(c);
        Check(surface.Status==ObjectiveStatus.Complete,"Surface arrival checks the actual sub's three-dimensional position");

        Reset();crew=Roster();crew.IsDeployed=true;var response=new TacticalResponse();var player=Game.Player.Character;
        var heli=new Vehicle {Model=new Model("polmav"),Position=player.Position+new Vector3(0,0,140)};
        var pilot=new Ped {IsCop=true,RelationshipGroup=77,Position=heli.Position};pilot.SetIntoVehicle(heli,VehicleSeat.Driver);World.Nearby=new[]{pilot};
        Game.Player.WantedLevel=3;Function.StarsGreyed=true;Function.ClearLos=true;Function.Calls.Clear();response.Update(crew);
        Check(Function.Calls.Any(x=>x.Item1==Hash.REPORT_POLICE_SPOTTED_PLAYER),"A genuine police pilot 140 m overhead can reacquire a visible wanted player");
        Function.Calls.Clear();Function.ClearLos=false;Game.GameTime+=2000;response.Update(crew);
        Check(!Function.Calls.Any(x=>x.Item1==Hash.REPORT_POLICE_SPOTTED_PLAYER),"The larger helicopter scan cannot report through obstructing geometry");
        Function.ClearLos=true;pilot.Task.LeaveVehicle();Function.Calls.Clear();Game.GameTime+=2000;response.Update(crew);
        Check(!Function.Calls.Any(x=>x.Item1==Hash.REPORT_POLICE_SPOTTED_PLAYER),"A far ground cop does not inherit helicopter sighting range");
        pilot.SetIntoVehicle(heli,VehicleSeat.Driver);Function.StarsGreyed=false;Function.WantedSeen=false;Function.Calls.Clear();Game.GameTime+=2000;response.Update(crew);
        Check(Function.Calls.Any(x=>x.Item1==Hash.REPORT_POLICE_SPOTTED_PLAYER),"Fresh scripted wanted state is reacquired even before the gray-star HUD phase");
        pilot.IsCop=false;Function.Calls.Clear();Game.GameTime+=2000;response.Update(crew);
        Check(!Function.Calls.Any(x=>x.Item1==Hash.REPORT_POLICE_SPOTTED_PLAYER),"A civilian helicopter occupant cannot become a police observer by proximity");
        pilot.RelationshipGroup=Game.GenerateHash("COP");Function.Calls.Clear();Game.GameTime+=2000;response.Update(crew);
        Check(Function.Calls.Any(x=>x.Item1==Hash.REPORT_POLICE_SPOTTED_PLAYER),"A legitimate COP-group pilot model is recognized even without ped type 6");
        response.Reset();World.Nearby=new Ped[0];

        Reset();var presentation=new MissionPresentation(new ModConfig());Function.Calls.Clear();
        presentation.Update(true,false);Game.GameTime+=700;presentation.Update(true,false);
        Check(presentation.ScorePlaying&&Function.Calls.Count(x=>x.Item1==Hash.TRIGGER_MUSIC_EVENT&&(string)x.Item2[0]=="DHP1_START")==1,"The stock score is requested once on stable on-foot mission entry");
        for(int i=0;i<30;i++){Game.GameTime+=16;presentation.Update(true,false);}
        Check(Function.Calls.Count(x=>x.Item1==Hash.TRIGGER_MUSIC_EVENT&&(string)x.Item2[0]=="DHP1_START")==1,"Music does not restart on every game tick");
        Game.Player.Character.SetIntoVehicle(new Vehicle(),VehicleSeat.Driver);presentation.Update(true,false);
        Check(!presentation.ScorePlaying&&Function.Calls.Any(x=>x.Item1==Hash.TRIGGER_MUSIC_EVENT&&(string)x.Item2[0]=="DHP1_STOP"),"Entering a car stops only the mission score we own");
        Game.Player.Character.Task.LeaveVehicle();presentation.QueuePassed("The Port Heist");presentation.Update(false,true);Game.GameTime+=9000;presentation.Update(false,true);
        Check(presentation.BannerQueued,"The success panel waits while aftermath owns the screen");
        for(int i=0;i<25;i++){Game.GameTime+=250;presentation.Update(false,false);}
        Check(!presentation.BannerQueued,"The centered success banner expires without freezing gameplay");
        Check((int)MissionPresentation.StartSprite(false,false)==381&&(int)MissionPresentation.StartSprite(true,false)==76&&(int)MissionPresentation.StartSprite(false,true)==428,"Mission starts use stock B, S and H letter sprites, not generic circles");
        Reset();Function.ScoreAvailable=false;presentation=new MissionPresentation(new ModConfig());
        for(int i=0;i<30;i++){Game.GameTime+=250;presentation.Update(true,false);}
        Check(!presentation.ScorePlaying,"Unavailable installed music does not block a mission");presentation.Stop();
        Reset();presentation=new MissionPresentation(new ModConfig { MissionScoreEnabled=false });
        presentation.Update(true,false);Game.GameTime+=1000;presentation.Update(true,false);
        Check(!presentation.ScorePlaying,"Mission score can be disabled without changing gameplay");
        presentation.Stop();
        Reset();
    }
}
