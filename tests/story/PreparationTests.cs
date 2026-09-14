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
    static void DrainPreparation(Mission m, MissionContext c)
    {
        for (int i=0;i<4;i++) { if(c.Cutscenes.IsActive)c.Cutscenes.Skip();c.Dialogue.Clear();m.Tick(); }
    }
    static void ParkPreparation(Mission m,MissionContext c,Vehicle car,string key,CrewSlot slot=CrewSlot.Guess)
    {
        Use(c.Crew,slot);Game.Player.Character.SetIntoVehicle(car,slot==CrewSlot.Guess?VehicleSeat.Driver:VehicleSeat.LeftRear);
        car.Position=c.Locations.Position(key);car.Speed=0;
        foreach(var ped in car.Seats.Values)ped.Position=car.Position;
        DrainPreparation(m,c);
    }
    static void BoardTestCrew(MissionContext c,Vehicle car)
    {
        c.Crew.PedFor(CrewSlot.Guess).SetIntoVehicle(car,VehicleSeat.Driver);
        c.Crew.PedFor(CrewSlot.Ice).SetIntoVehicle(car,VehicleSeat.LeftRear);
        c.Crew.PedFor(CrewSlot.Gohan).SetIntoVehicle(car,VehicleSeat.RightRear);
    }
    static void ClearPreparationEnemies()
    { foreach(var p in World.Created.Where(p=>p.Model.Name=="s_m_y_blackops_01"))p.IsDead=true; }
    sealed class PreparationBoardProbe : PreparationOperation
    {
        public override string Id => "M31";
        public override string Title => "Boarding probe";
        protected override bool Setup() => BeginCrew(CrewSlot.Guess);
        protected override System.Collections.Generic.IEnumerable<MissionStage> BuildStages()
        { yield return new MissionStage("Wait", new ConditionObjective("Wait", () => false)); }
        public bool TryBoard(Ped actor, Vehicle car, VehicleSeat seat) => Board(actor, car, seat);
    }
    static void PreparationChecks()
    {
        Reset();var crew=Roster();var c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"p31.json"));
        var m31=new M31TheIronPerimeter();Check(m31.Begin(c),"M31 starts with physical perimeter equipment and separate crew roles");
        Check(!m31.ProbeStarted&&!World.Created.Any(p=>p.Model.Name=="s_m_y_blackops_01"),"The probe convoy does not exist during the preparation scene");
        DrainPreparation(m31,c);
        foreach(int i in new[]{3,1,2})Interact(m31,c,CrewSlot.Ice,c.Locations.Position("M31.Senora.Work"+i),2);
        DrainPreparation(m31,c);Check(m31.CurrentStage==1,"M31 accepts scouting the approaches in any order");
        foreach(int i in new[]{2,3,1})Interact(m31,c,CrewSlot.Gohan,c.Locations.Position("M31.Senora.Work"+i),4);
        DrainPreparation(m31,c);Check(m31.ArmedCount==3&&m31.CurrentStage==2,"Three completed arming actions create three real charges");
        var car=World.Vehicles.First(v=>v.Model.Name=="granger");ParkPreparation(m31,c,car,"M31.Senora.Retreat");
        Check(m31.ProbeStarted&&World.Vehicles.Count(v=>v.Model.Name=="mesa")==2,"The two-car probe arrives only after Guess proves the retreat road");
        var fighters=World.Created.Where(p=>p.Model.Name=="s_m_y_blackops_01").ToArray();Check(fighters.Length==4,"Both response cars have their own driver and passenger");
        Game.GameTime+=3000;m31.Tick();Check(fighters.Where(p=>p.SeatIndex==VehicleSeat.Driver).All(p=>p.Task.Drives>0),"Response drivers retain driving assignments");
        ClearPreparationEnemies();DrainPreparation(m31,c);Interact(m31,c,CrewSlot.Gohan,c.Locations.Position("M31.Senora.GeneratorWork"),4);DrainPreparation(m31,c);
        Check(m31.Status==MissionStatus.Passed&&c.State.FleetUpgrades["bunkerPerimeterReady"],"M31 passes only after combat and the generator inspection");

        Reset();crew=Roster();c=Context(crew);m31=new M31TheIronPerimeter();m31.Begin(c);DrainPreparation(m31,c);
        World.Props.First(p=>p.Model.Name=="prop_generator_03b").IsDead=true;m31.Tick();
        Check(m31.Status==MissionStatus.Failed,"Destroying the required generator fails M31 instead of granting defenses");

        // September 13: M32 and M39 both refused to start because one guard post had
        // no pedestrian navmesh. A post without navmesh is still a post, and a guard
        // that will not spawn is a thinner fight rather than a dead mission.
        Reset();World.GroundHeight=14f;
        Check(Math.Abs(GameUtils.OnGround(new Vector3(10,20,90)).Z-14f)<.01f&&GameUtils.OnGround(new Vector3(10,20,90)).X==10f,
            "A point with no navmesh is dropped onto its own ground, keeping its X and Y");
        World.GroundHeight=0f;
        string prep=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act2","PreparationOperation.cs"));
        Check(!prep.Contains("throw new InvalidOperationException(\"Cannot place guard"),
            "One unplaceable guard no longer throws the whole mission away");
        string desert=File.ReadAllText(Path.Combine(Repo,"src","Bloodlines","Missions","Campaign","Act2","DesertOperation.cs"));
        Check(desert.Contains("GameUtils.OnGround(point)"),
            "A guard post without navmesh falls back to the authored point's ground");

        Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"p32.json"));var m32=new M32BlackSiteZancudo();
        Check(m32.Begin(c),"M32 starts at an actual water craft and exterior service post");DrainPreparation(m32,c);
        var dinghy=World.Vehicles.First(v=>v.Model.Name=="dinghy");Use(crew,CrewSlot.Gohan);Game.Player.Character.SetIntoVehicle(dinghy,VehicleSeat.Driver);
        dinghy.Position=c.Locations.Position("M32.LandingWater");Game.Player.Character.Position=dinghy.Position;DrainPreparation(m32,c);
        Interact(m32,c,CrewSlot.Gohan,c.Locations.Position("M32.PanelWork"),5);DrainPreparation(m32,c);
        Check(m32.CurrentStage==2,"Exterior control work opens the access scene and sends Ice to the yard");
        Use(crew,CrewSlot.Ice);ClearPreparationEnemies();DrainPreparation(m32,c);ParkPreparation(m32,c,m32.Extraction,"M32.Pickup");
        Check(m32.CurrentStage==4,"Guess must bring the case carrier to the cleared pickup before Gohan loads it");
        var cases=World.Props.Where(p=>p.Model.Name=="prop_security_case_01").ToArray();
        for(int i=0;i<2;i++)
        {
            Interact(m32,c,CrewSlot.Gohan,cases[i].Position,3);DrainPreparation(m32,c);
            Check(cases[i].AttachedTo==crew.PedFor(CrewSlot.Gohan),"EMP case is visibly in Gohan's hand before loading");
            Interact(m32,c,CrewSlot.Gohan,m32.Extraction.Position-m32.Extraction.ForwardVector*3f,3);DrainPreparation(m32,c);
            Check(m32.CasesLoaded==i+1&&cases[i].AttachedTo==m32.Extraction,"Each case counts only after attachment to the extraction car");
        }
        Use(crew,CrewSlot.Guess);BoardTestCrew(c,m32.Extraction);DrainPreparation(m32,c);
        var responseDriver=World.Created.First(p=>p.Model.Name=="s_m_y_blackops_01"&&!p.IsDead&&p.SeatIndex==VehicleSeat.Driver);
        ParkPreparation(m32,c,m32.Extraction,"M32.Exit");
        Check(responseDriver.Task.LastDrivePoint.DistanceTo(Game.Player.Character.Position)>400f,"M32 response retreats at the road exit before delivery work begins");
        ParkPreparation(m32,c,m32.Extraction,"M32.Senora.Delivery");
        Interact(m32,c,CrewSlot.Gohan,c.Locations.Position("M32.Senora.Workbench"),4);DrainPreparation(m32,c);
        Check(m32.Status==MissionStatus.Passed&&c.State.CargoAt("empWarheads")=="M32.Senora.Workbench","M32 secures both delivered cases before its completion consequence");

        Reset();crew=Roster();c=Context(crew);var m33=new M33TheInformantsGrave();Check(m33.Begin(c),"M33 starts with an identifiable living prisoner");DrainPreparation(m33,c);
        Game.GameTime+=180000;m33.Tick();Check(!m33.ExecutionStarted&&m33.Status==MissionStatus.Running,"Three minutes of approach time do not consume the execution window");
        Use(crew,CrewSlot.Ice);Game.Player.Character.Position=c.Locations.Position("M33.Observe");DrainPreparation(m33,c);
        Check(m33.ExecutionStarted,"Reaching the firing line starts the rescue clock");
        Game.GameTime+=91000;m33.Tick();Check(m33.Status==MissionStatus.Failed,"Missing the live rescue deadline fails with a bounded retry path");

        Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"p33.json"));m33=new M33TheInformantsGrave();m33.Begin(c);DrainPreparation(m33,c);
        Use(crew,CrewSlot.Ice);Game.Player.Character.Position=c.Locations.Position("M33.Observe");DrainPreparation(m33,c);
        ClearPreparationEnemies();DrainPreparation(m33,c);Interact(m33,c,CrewSlot.Gohan,m33.Ramos.Position,4);DrainPreparation(m33,c);
        Check(m33.Freed&&m33.CurrentStage==3,"Rescue requires Gohan's physical restraint interaction after the guards");
        car=World.Vehicles.First(v=>v.Model.Name=="granger");ParkPreparation(m33,c,car,"M33.Pickup");
        BoardTestCrew(c,car);m33.Ramos.SetIntoVehicle(car,VehicleSeat.Passenger);DrainPreparation(m33,c);
        Check(m33.Ramos.SeatIndex==VehicleSeat.Passenger&&m33.CurrentStage==5,"Ramos has his own front seat while the brothers use two separate rear seats");
        ParkPreparation(m33,c,car,"M33.Transfer");var half=World.Vehicles.First(v=>v.Model.Name=="halftrack");
        m33.Ramos.SetIntoVehicle(half,VehicleSeat.Passenger);DrainPreparation(m33,c);
        Check(m33.Status==MissionStatus.Passed&&c.State.CargoAt("ramos")=="M34.Start","M33 ends with Ramos aboard armored transport, with no rig codes granted early");
        Check(c.State.EvidenceOf("rigAccessCodes")==EvidenceState.None,"Ramos's rescue does not manufacture access codes");

        Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"p34.json"));var m34=new M34MudAndIron();Check(m34.Begin(c),"M34 starts its armored evacuation");DrainPreparation(m34,c);
        var escort=World.Vehicles.First(v=>v.Model.Name=="granger");
        Check(m34.Ramos.SeatIndex==VehicleSeat.Passenger&&crew.PedFor(CrewSlot.Ice).SeatIndex==VehicleSeat.LeftRear&&crew.PedFor(CrewSlot.Gohan).IsInVehicle(escort),"Half-track has only three assigned seats; Gohan occupies a separate escort car");
        Use(crew,CrewSlot.Gohan);Game.GameTime+=3000;m34.Tick();
        var nearbyThreat=World.Created.First(p=>p.Model.Name=="s_m_y_blackops_01");nearbyThreat.Position=crew.PedFor(CrewSlot.Ice).Position+new Vector3(5,0,0);
        Game.GameTime+=3000;m34.Tick();
        Check(crew.PedFor(CrewSlot.Ice).Task.VehicleShots>0&&crew.PedFor(CrewSlot.Ice).IsInVehicle(m34.Halftrack),"Ice receives a turret firing task and keeps his half-track seat");
        Check(crew.PedFor(CrewSlot.Guess).Task.Drives>0&&crew.PedFor(CrewSlot.Guess).IsInVehicle(m34.Halftrack),"Selecting Gohan keeps Guess driving the occupied half-track");
        var convoyObjective=new ConvoyRouteObjective("Escort",()=>m34.Halftrack,()=>escort,()=>new Vector3(500,500,0),10);
        convoyObjective.Enter(c);m34.Halftrack.Position=new Vector3(500,500,0);m34.Halftrack.Speed=0;convoyObjective.Update(c);
        Check(convoyObjective.IsFinished,"Gohan can complete an evacuation route leg from his escort car when the patient vehicle arrives");
        ParkPreparation(m34,c,m34.Halftrack,"M34.Route1");
        escort.Position=c.Locations.Position("M34.Route2");ParkPreparation(m34,c,m34.Halftrack,"M34.Route2");
        Check(m34.RoadblocksDropped==1,"First road barrier activates after both crew vehicles pass it");
        ParkPreparation(m34,c,m34.Halftrack,"M34.Route3");escort.Position=c.Locations.Position("M34.Exit");ParkPreparation(m34,c,m34.Halftrack,"M34.Exit");
        Check(m34.RoadblocksDropped==2,"Second barrier has its own clearance and route trigger");
        ParkPreparation(m34,c,m34.Halftrack,"M34.Senora.Shelter");m34.Ramos.Position=c.Locations.Position("M34.Senora.MedicalWork");Interact(m34,c,CrewSlot.Gohan,c.Locations.Position("M34.Senora.MedicalWork"),5);DrainPreparation(m34,c);
        Check(m34.Status==MissionStatus.Passed&&c.State.EvidenceOf("rigAccessCodes")==EvidenceState.CopyHeld&&c.State.CargoAt("ramos")=="M34.Senora.Shelter","Medical arrival precedes Ramos's access-code consequence");

        Reset();crew=Roster();c=Context(crew);c.State=CampaignState.Load(Path.Combine(root,"p35.json"));var m35=new M35TheChianskiAmbush();Check(m35.Begin(c),"M35 opens before the moving convoy is created");DrainPreparation(m35,c);
        Check(!m35.ConvoyStarted,"No convoy escape clock runs during trap preparation");
        foreach(int i in new[]{2,1})Interact(m35,c,CrewSlot.Ice,c.Locations.Position("M35.ChargeWork"+i),4);DrainPreparation(m35,c);
        car=World.Vehicles.First(v=>v.Model.Name=="granger");ParkPreparation(m35,c,car,"M35.BlockExit");
        Interact(m35,c,CrewSlot.Gohan,c.Locations.Position("M35.DeviceWork"),4);DrainPreparation(m35,c);
        Check(m35.ConvoyStarted&&m35.Technical!=null,"Laptop identification releases the prepared convoy");
        var lead=World.Vehicles.First(v=>v.Model.Name=="mesa");lead.Position=c.Locations.Position("M35.Trap");
        crew.PedFor(CrewSlot.Ice).Position=lead.Position;m35.Tick();Check(!m35.Trapped&&m35.CurrentStage==3,"The trap cannot detonate on a nearby brother or advance without the blast result");
        crew.PedFor(CrewSlot.Ice).Position=c.Locations.Position("M35.IceCover");DrainPreparation(m35,c);
        Check(m35.Trapped&&m35.CurrentStage==4,"A clear trap stops the lead escort without claiming an unstaged capture");
        // Ron's report: nothing shot back from the truck, and the block sent Guess 195 m
        // past everything and back again.
        Check(c.Locations.Position("M35.BlockExit").DistanceTo(c.Locations.Position("M35.Trap"))<70f,
            "The road is closed at the end of the kill zone, not two hundred meters past it");
        Check(m35.TruckGunner!=null&&m35.TruckGunner.IsInVehicle(m35.Technical),
            "The gun truck's gunner stays on the bed gun instead of standing in the road");
        m35.Tick();
        Check(m35.TruckGunner.Task.VehicleShots>0,
            "And he uses it, so something actually shoots back at the ambush");
        ClearPreparationEnemies();DrainPreparation(m35,c);Use(crew,CrewSlot.Guess);Game.Player.Character.SetIntoVehicle(m35.Technical,VehicleSeat.Driver);DrainPreparation(m35,c);
        crew.PedFor(CrewSlot.Gohan).SetIntoVehicle(m35.Technical,VehicleSeat.Passenger);crew.PedFor(CrewSlot.Ice).SetIntoVehicle(m35.Technical,VehicleSeat.LeftRear);DrainPreparation(m35,c);
        // The run home is a fight now: Ice works the gun, Gohan answers from the cab, a
        // pursuit follows, and either brother is playable while Guess drives.
        Check(m35.CrewFighting,"The crew keep fighting on the way to the bunker instead of riding along");
        Check(!c.Switching.Locked,"Either brother can be taken while Guess drives");
        ParkPreparation(m35,c,m35.Technical,"M35.Senora.Delivery");Interact(m35,c,CrewSlot.Gohan,m35.Technical.Position-m35.Technical.ForwardVector*2f,5);DrainPreparation(m35,c);
        Check(m35.Status==MissionStatus.Passed&&c.State.CargoAt("antiAirTechnical")=="M35.Senora.Delivery","M35 requires the same captured technical at its destination and a real gun-mount inspection");

        Reset();crew=Roster();c=Context(crew);World.FailVehicles=true;
        Check(!new M34MudAndIron().Begin(c),"Missing essential armored transport rejects M34 startup without a fake completion");
        World.FailVehicles=false;
        Reset();crew=Roster();c=Context(crew);var probe=new PreparationBoardProbe();probe.Begin(c);
        car=new Vehicle{Model=new Model("halftrack"),Position=crew.PedFor(CrewSlot.Ice).Position};
        Check(!probe.TryBoard(crew.PedFor(CrewSlot.Ice),car,VehicleSeat.RightRear)&&probe.Status==MissionStatus.Failed,"A nonexistent fourth half-track seat fails clearly instead of waiting forever");
        Reset();crew=Roster();c=Context(crew);probe=new PreparationBoardProbe();probe.Begin(c);
        car=new Vehicle{Model=new Model("granger"),Position=crew.PedFor(CrewSlot.Ice).Position};
        probe.TryBoard(crew.PedFor(CrewSlot.Ice),car,VehicleSeat.LeftRear);Game.GameTime+=46000;
        Check(!probe.TryBoard(crew.PedFor(CrewSlot.Ice),car,VehicleSeat.LeftRear)&&probe.Status==MissionStatus.Failed,"A stalled nearby boarding attempt has a bounded failure and retry");
        Reset();crew=Roster();c=Context(crew);probe=new PreparationBoardProbe();probe.Begin(c);
        car=new Vehicle{Model=new Model("granger"),Position=crew.PedFor(CrewSlot.Ice).Position};
        crew.PedFor(CrewSlot.Ice).SetIntoVehicle(car,VehicleSeat.Passenger);
        Check(!probe.TryBoard(crew.PedFor(CrewSlot.Ice),car,VehicleSeat.LeftRear)&&!crew.PedFor(CrewSlot.Ice).IsInVehicle(),"A companion in the wrong seat is asked to leave for normal reboarding");probe.Abort();
        var state=CampaignState.Load(Path.Combine(root,"prep-rewards.json"));var catalog=new MissionCatalog();
        foreach(int n in Enumerable.Range(31,5)){var id="M"+n;catalog.All.Add(Def(id,"main"));state.MarkComplete(id,catalog);}
        int cash=state.CashOnHand;Check(cash==535000&&state.FleetUpgrades["technicalSupportReady"],"The five first completions grant the documented 535000 cash and support-vehicle entitlements");
        foreach(int n in Enumerable.Range(31,5))state.MarkComplete("M"+n,catalog);
        Check(state.CashOnHand==cash,"Replaying the new block never pays twice");
        state.BeginAttempt("M32");state.SetCargo("empWarheads","temporary");state.DiscardAttempt();
        Check(state.CargoAt("empWarheads")==null,"Discarding an attempt removes provisional new mission cargo");
    }
}
