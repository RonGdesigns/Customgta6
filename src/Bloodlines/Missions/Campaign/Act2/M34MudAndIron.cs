using System;
using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;
namespace Bloodlines.Missions.Campaign
{
    public sealed class M34MudAndIron : PreparationOperation
    {
        public override string Id => "M34";public override string Title => "Mud & Iron";
        protected override MissionEndpoint Endpoint=>MissionEndpoint.SecuredDelivery;
        private Vehicle _halftrack;private Ped _ramos;private readonly List<Prop> _blocks=new List<Prop>();
        private readonly HashSet<int> _dropped=new HashSet<int>();private bool _safe;private int _escortOrder;private bool _patientWalking;
        public Vehicle Halftrack=>_halftrack;public Ped Ramos=>_ramos;public int RoadblocksDropped=>_dropped.Count;
        protected override bool Setup()
        {
            if(!BeginCrew(CrewSlot.Guess))return false;
            GameUtils.SetWeather("Dust");
            _halftrack=Car("halftrack",At("M34.Halftrack"),Ctx.Locations.Heading("M34.Halftrack"));
            CrewCar=CrewTransport("M34.CrewCar");_ramos=Person("s_m_m_scientist_01","M34.Ramos");
            if(!RequireAssets(_halftrack,CrewCar,_ramos))return false;
            RequireAsset(_halftrack,"The evacuation half-track was destroyed.");RequireSurvivor(_ramos,"Ramos died during evacuation.");
            Station(CrewSlot.Guess,_halftrack,VehicleSeat.Driver);Station(CrewSlot.Ice,_halftrack,VehicleSeat.LeftRear);
            _ramos.SetIntoVehicle(_halftrack,VehicleSeat.Passenger);Station(CrewSlot.Gohan,CrewCar,VehicleSeat.Driver);
            Roles.Release();Ctx.Crew.CompanionsHoldPosition=false;
            for(int i=1;i<=2;i++)_blocks.Add(Equipment("prop_barrier_work05","M34.BarrierPark"+i));
            Establish("departure","A seat for the patient","Ramos occupies the half-track's front passenger seat; Ice has the actual rear turret and Guess drives. Gohan follows in the crew car and handles the prepared road barriers by radio. There is no fourth seat in the half-track.",_halftrack,CrewCar);
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Leave the transfer site",new ConvoyRouteObjective("Drive the half-track carrying Ramos into the wind-farm route. Ice covers the rear; Gohan follows in the crew car.",()=>_halftrack,()=>CrewCar,()=>At("M34.Route1"),18))
                .OnEnter(c=>{DrivingDestination=()=>At("M34.Route1");Fighting=true;ResponseCar("M34.Response1",_halftrack.Position);}).WithCues("M34_S1_01_GUESS");
            yield return new MissionStage("First road barrier",new ConvoyRouteObjective("Continue past the first yellow road marker; Gohan drops the barrier only after both crew vehicles are clear",()=>_halftrack,()=>CrewCar,()=>At("M34.Route2"),18),new ConditionObjective("Wait for Gohan's escort car to clear the first barrier",()=>DropBarrier(0))).OnEnter(c=>DrivingDestination=()=>At("M34.Route2"))
                .AfterCues("M34_S1_02_ICE");
            yield return new MissionStage("Second response",new ConvoyRouteObjective("Follow the second yellow road marker through the gully. Keep Ramos in the half-track.",()=>_halftrack,()=>CrewCar,()=>At("M34.Route3"),18))
                .OnEnter(c=>{DrivingDestination=()=>At("M34.Route3");ResponseCar("M34.Response2",_halftrack.Position);}).WithCues("M34_S1_03_GOHAN");
            yield return new MissionStage("Close the rear route",new ConvoyRouteObjective("Clear the second barrier with both vehicles and follow the shelter road",()=>_halftrack,()=>CrewCar,()=>At("M34.Exit"),18),new ConditionObjective("Both crew vehicles must clear the second road barrier",()=>DropBarrier(1)))
                .OnEnter(c=>DrivingDestination=()=>At("M34.Exit")).AfterCues("M34_S1_04_GOHAN");
            yield return new MissionStage("Lose the police",new LoseWantedObjective("Lose any police pursuit before arriving at the medical shelter.")).OnEnter(c=>RetreatResponse());
            yield return new MissionStage("Medical shelter",new ConvoyRouteObjective("Stop the half-track at the yellow medical shelter marker with Ramos aboard",()=>_halftrack,()=>CrewCar,()=>At("M34.Senora.Shelter"),12))
                .OnEnter(c=>DrivingDestination=()=>At("M34.Senora.Shelter"))
                .OnExit(c=>{DrivingDestination=null;Fighting=false;_ramos.Task.LeaveVehicle();});
            yield return new MissionStage("Patient first",new ConditionObjective("Wait for Ramos to leave the stopped half-track",()=>!_ramos.IsInVehicle()&&_ramos.Position.DistanceTo(At("M34.Senora.MedicalWork"))<7f),new MissionInteraction("Gohan: get out and prepare the marked medical kit for Ramos",()=>At("M34.Senora.MedicalWork"),5,3f,animation:MissionInteraction.ReachInside)).OwnedBy(CrewSlot.Gohan)
                .OnEnter(c=>
                {
                    var table=Equipment("prop_table_03","M34.Senora.MedicalKit");
                    var kit=WorkProp("prop_ld_health_pack",PropPlacement.OnTop(table,table.Model,new Model("prop_ld_health_pack")),false);
                    if(!RequireAssets(kit))throw new InvalidOperationException("The medical kit failed to load.");
                })
                .OnExit(c=>{_safe=true;Establish("medical","He decides when to talk","The crew reaches a real exterior medical station. Ramos is alive and out of the vehicle. Gohan checks him before accepting the access information.",_ramos,_halftrack);}).AfterCues("M34_S1_05_GUESS");
        }
        private bool DropBarrier(int index)
        {
            if(_dropped.Contains(index))return true;
            var p=At("M34.BarrierRoad"+(index+1));
            if(_halftrack.Position.DistanceTo(p)<50f||CrewCar.Position.DistanceTo(p)<35f)return false;
            if(_halftrack.Position.DistanceTo(At(index==0?"M34.Route2":"M34.Exit"))>28f)return false;
            _blocks[index].Position=p;_blocks[index].Heading=Ctx.Locations.Heading("M34.BarrierRoad"+(index+1));
            _dropped.Add(index);Radio("GOHAN","Both vehicles clear. Road barrier is down behind us. Keep moving; it buys distance, not an empty road.","M34_BLOCK_"+index);return true;
        }
        protected override void OnUpdate()
        {
            if(CurrentStage==6&&!_patientWalking&&!_ramos.IsInVehicle())
            { _ramos.Task.GoTo(At("M34.Senora.MedicalWork")); _patientWalking=true; }
            if(!_safe&&CurrentStage<6&&!_ramos.IsInVehicle(_halftrack)){Fail("Ramos is no longer aboard the evacuation half-track.");return;}
            if(Game.GameTime>=_escortOrder&&CurrentStage<6)
            {
                _escortOrder=Game.GameTime+2500;
                var gohan=Ctx.Crew.PedFor(CrewSlot.Gohan);
                if(Ctx.Crew.ActiveSlot!=CrewSlot.Gohan&&gohan.IsInVehicle(CrewCar))
                {Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Gohan);gohan.Task.DriveTo(CrewCar,_halftrack.Position-_halftrack.ForwardVector*15f,10f,34f,(DrivingStyle)CrewDriving.TrafficFlags);}
            }
            base.OnUpdate();
        }
        protected override void OnPassed()
        {
            if(!_safe)throw new InvalidOperationException("Ramos has not reached medical care.");
            Ctx.State?.SetCargo("ramos","M34.Senora.Shelter");Ctx.State?.SetEvidence("rigAccessCodes",EvidenceState.CopyHeld);
            Release(_halftrack);
        }
    }
}
