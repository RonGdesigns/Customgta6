using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
namespace Bloodlines.Missions.Campaign
{
    public sealed class M35TheChianskiAmbush : PreparationOperation
    {
        public override string Id => "M35";public override string Title => "The Chianski Ambush";
        protected override MissionEndpoint Endpoint=>MissionEndpoint.SecuredDelivery;
        private Vehicle _lead,_technical;private Ped _leadDriver,_technicalDriver;
        private readonly List<Prop> _charges=new List<Prop>();private Prop _table,_device;
        private bool _convoyStarted,_trapped,_delivered;private int _convoyAt,_routeOrder;
        public Vehicle Technical=>_technical;public bool ConvoyStarted=>_convoyStarted;public bool Trapped=>_trapped;
        protected override bool Setup()
        {
            if(!BeginCrew(CrewSlot.Ice))return false;
            CrewCar=CrewTransport("M35.CrewCar");if(!RequireAssets(CrewCar))return false;
            Station(CrewSlot.Guess,CrewCar,VehicleSeat.Driver);Roles.For(CrewSlot.Guess).Stop();
            _table=Equipment("prop_table_03","M35.Table");
            _device=WorkProp("prop_laptop_01a",PropPlacement.OnTop(_table,_table.Model,new Model("prop_laptop_01a")),false);
            if(!RequireAssets(_device))return false;
            Establish("approach","Take the gun truck alive","Ice prepares the marked roadside charges, Guess parks across the far exit, and Gohan identifies the gun truck on the field laptop. The convoy waits until setup is complete; the lead escort is the blast target, not the technical.",_device,CrewCar);
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Prepare the trap",new MultiHoldObjective("Ice: plant both marked roadside charges; press E / D-pad Right",Enumerable.Range(1,2).Select(i=>At("M35.ChargeWork"+i)),4,3f,"Planting charge"){Animation=MissionInteraction.ReachInside,SiteDone=i=>_charges.Add(Equipment("prop_ld_bomb_01","M35.Charge"+(i+1)))}).OwnedBy(CrewSlot.Ice).OnExit(c=>Roles.For(CrewSlot.Ice).Approach(At("M35.IceCover"),At("M35.IceCover")));
            yield return new MissionStage("Close the far exit",new TravelObjective("Guess: park the crew car across the yellow far-exit marker and stop",()=>At("M35.BlockExit"),8,()=>CrewCar)).OwnedBy(CrewSlot.Guess);
            yield return new MissionStage("Identify the target",new MissionInteraction("Gohan: use the laptop on the marked field table to identify the convoy's gun truck",()=>At("M35.DeviceWork"),4,3f,animation:MissionInteraction.ReachInside,face:()=>_device.Position)).OwnedBy(CrewSlot.Gohan).OnExit(c=>StartConvoy());
            yield return new MissionStage("Watch the pass",new ConditionObjective("Wait in cover for the red lead escort to enter the yellow trap. The orange gun truck must stay intact.",()=>_trapped));
            yield return new MissionStage("Capture the technical",new KillTargetsObjective("Stop the escort and gun-truck crew. Shoot the occupants, not the orange technical.",()=>Opposition),new ProtectObjective("",()=>_technical,"The technical was destroyed before capture."))
                .WithCues("M35_S1_01_ICE");
            yield return new MissionStage("Take the driver seat",new EnterVehicleObjective("Guess: take the captured technical's driver seat",()=>_technical,VehicleSeat.Driver)).OwnedBy(CrewSlot.Guess).AfterCues("M35_S1_02_GUESS");
            yield return new MissionStage("Bring both brothers",new ConditionObjective("Stop the technical: Gohan boards the front passenger seat and Ice takes the rear gun seat",()=>BoardTeam())).OwnedBy(CrewSlot.Guess)
                .OnExit(c=>{Fighting=false;DrivingDestination=()=>At("M35.Delivery");});
            yield return new MissionStage("Lose pursuit",new LoseWantedObjective("Lose the police before taking the captured technical to the bunker."));
            yield return new MissionStage("Deliver the gun truck",new TravelObjective("Deliver the same technical with both brothers to the bunker vehicle bay and stop",()=>At("M35.Delivery"),15,()=>_technical));
            yield return new MissionStage("Inspect the capture",new MissionInteraction("Gohan: get out and inspect the gun mount at the rear of the parked technical",()=>_technical.Position-_technical.ForwardVector*2f,5,4f,animation:MissionInteraction.ReachInside)).OwnedBy(CrewSlot.Gohan)
                .OnEnter(c=>DrivingDestination=null).OnExit(c=>{_delivered=true;Establish("delivery","Protection for the way home","The captured technical arrives intact. Gohan checks the real gun mount. It provides machine-gun cover against exposed targets and low aircraft; it is not a missile launcher or an automatic air-defense system.",_technical);}).AfterCues("M35_S1_03_GOHAN");
        }
        private void StartConvoy()
        {
            if(_charges.Count!=2)throw new InvalidOperationException("The pass has not been prepared.");
            _lead=Car("mesa",At("M35.LeadSpawn"),Ctx.Locations.Heading("M35.LeadSpawn"),false);
            _technical=Car("technical",At("M35.TechnicalSpawn"),Ctx.Locations.Heading("M35.TechnicalSpawn"));
            _leadDriver=Guard(At("M35.LeadSpawn"));_technicalDriver=Guard(At("M35.TechnicalSpawn"));
            var gunner=Guard(At("M35.TechnicalSpawn"));var escort=Guard(At("M35.LeadSpawn"));
            if(!RequireAssets(_lead,_technical,_leadDriver,_technicalDriver,gunner,escort))throw new InvalidOperationException("The pass convoy failed to load.");
            _leadDriver.SetIntoVehicle(_lead,VehicleSeat.Driver);escort.SetIntoVehicle(_lead,VehicleSeat.Passenger);
            _technicalDriver.SetIntoVehicle(_technical,VehicleSeat.Driver);gunner.SetIntoVehicle(_technical,VehicleSeat.LeftRear);
            Opposition.AddRange(new[]{_leadDriver,_technicalDriver,gunner,escort});
            RequireAsset(_technical,"The required technical was destroyed. Restart the ambush.");
            var blip=Track(_lead.AddBlip());blip.Color=BlipColor.Red;blip.Name="Lead escort - trap this vehicle";
            _lead.IsEngineRunning=true;_technical.IsEngineRunning=true;_convoyAt=Game.GameTime;_convoyStarted=true;
            Radio("GOHAN","Lead escort first, gun truck behind it. Wait for the lead vehicle in the trap. Guess has the far exit; keep the technical out of the blast.","M35_IDENTIFIED");
        }
        private void Trap()
        {
            if(_trapped)return;
            var point=At("M35.Trap");
            if(Protagonist.All.Any(h=>Ctx.Crew.PedFor(h.Slot).Position.DistanceTo(point)<25f)||_technical.Position.DistanceTo(point)<35f)
            {GameUtils.Subtitle("Clear the trap area. The crew and technical must be outside the blast radius.",2500);return;}
            _trapped=true;_lead.EngineHealth=0f;_lead.IsEngineRunning=false;
            World.AddExplosion(point,ExplosionType.Grenade,1f,.4f,Game.Player.Character,true,false);
            _leadDriver.Task.LeaveVehicle();_technicalDriver.Task.ClearAll();_technical.IsEngineRunning=false;
            foreach(var enemy in Opposition)enemy.Task.LeaveVehicle();
            Fighting=true;
        }
        private bool BoardTeam()
        {
            Roles.Release();Ctx.Crew.CompanionsHoldPosition=false;
            bool gohan=Board(Ctx.Crew.PedFor(CrewSlot.Gohan),_technical,VehicleSeat.Passenger);
            bool ice=Board(Ctx.Crew.PedFor(CrewSlot.Ice),_technical,VehicleSeat.LeftRear);
            return gohan&&ice;
        }
        protected override void OnUpdate()
        {
            if(_convoyStarted&&!_trapped)
            {
                if(Game.GameTime-_convoyAt>150000){Fail("The convoy did not reach the prepared trap. Retry or survey its road approach.");return;}
                if(Game.GameTime>=_routeOrder)
                {
                    _routeOrder=Game.GameTime+3000;
                    if(_leadDriver.Exists()&&!_leadDriver.IsDead)_leadDriver.Task.DriveTo(_lead,At("M35.Trap"),4f,17f,(DrivingStyle)CrewDriving.TrafficFlags);
                    if(_technicalDriver.Exists()&&!_technicalDriver.IsDead)_technicalDriver.Task.DriveTo(_technical,At("M35.TechnicalHold"),10f,15f,(DrivingStyle)CrewDriving.TrafficFlags);
                }
                if(CurrentStage==3&&_lead.Position.DistanceTo(At("M35.Trap"))<8f)Trap();
            }
            base.OnUpdate();
        }
        protected override void OnPassed(){if(!_delivered)throw new InvalidOperationException("The captured technical has not been inspected.");Ctx.State?.SetCargo("antiAirTechnical","M35.Delivery");Release(_technical);}
    }
}
