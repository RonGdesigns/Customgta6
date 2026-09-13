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
    public sealed class M38BloodInTheQuarry : PreparationOperation
    {
        public override string Id => "M38";
        public override string Title => "Blood in the Quarry";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;
        private readonly List<Prop> _crates=new List<Prop>();private Prop _cabinet;private int _loaded;private bool _delivered;
        public Vehicle Hauler => CrewCar;
        public int Loaded => _loaded;
        protected override bool Setup()
        {
            if(!BeginCrew(CrewSlot.Guess))return false;
            CrewCar=Car("benson",At("M38.Hauler"),Ctx.Locations.Heading("M38.Hauler"));if(!RequireAssets(CrewCar))return false;
            RequireAsset(CrewCar,"The explosive carrier was destroyed.");Station(CrewSlot.Guess,CrewCar,VehicleSeat.Driver);Roles.For(CrewSlot.Guess).Stop();
            _cabinet=Equipment("prop_elecbox_12","M38.Cabinet");
            for(int i=1;i<=4;i++){var crate=Equipment("prop_cs_cardbox_01","M38.Crate"+i);RequireAsset(crate,"A required charge crate was destroyed.");_crates.Add(crate);Enemy("M38.Guard"+i);}
            Establish("approach","Take the measured stock","Four marked charge packages wait beside the quarry stock-control cabinet. Ice clears the loading yard, Gohan releases and carries the packages, Guess brings the Benson into the loading lane.",_cabinet,_crates[0],CrewCar);
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Bring moving cover",new TravelObjective("Guess: drive the Benson into the yellow loading lane under fire. Ice covers the approach; keep the truck moving until you reach the marker",()=>At("M38.Load"),8,()=>CrewCar)).OwnedBy(CrewSlot.Guess).OnEnter(c=>{Fighting=true;Roles.For(CrewSlot.Ice).TakeCover(At("M38.IceStart"));Roles.For(CrewSlot.Gohan).TakeCover(At("M38.GohanStart"));}).WithCues("M38_S1_01_ICE");
            yield return new MissionStage("Clear the loading yard",new KillTargetsObjective("Use Ice or fight as Guess: stop the four red quarry guards, using the positioned truck as cover. Keep the yellow packages intact",()=>Opposition));
            yield return new MissionStage("Release blasting stock",new MissionInteraction("Gohan: unlock the marked stock-control cabinet beside the crates",()=>At("M38.CabinetWork"),4,3f,animation:MissionInteraction.ReachInside,face:()=>_cabinet.Position)).OwnedBy(CrewSlot.Gohan).OnEnter(c=>Fighting=false);
            for(int i=0;i<4;i++)
            {
                int n=i;
                yield return new MissionStage("Collect package "+(n+1),new MissionInteraction("Gohan: pick up marked charge package "+(n+1)+" of 4",()=>_crates[n].Position,3,3f,animation:MissionInteraction.ReachInside)).OwnedBy(CrewSlot.Gohan).OnExit(c=>Carry(_crates[n],CrewSlot.Gohan));
                yield return new MissionStage("Load package "+(n+1),new MissionInteraction("Gohan: carry the package to the back of the stopped Benson",()=>CrewCar.Position-CrewCar.ForwardVector*5.5f,3,3.5f,animation:MissionInteraction.ReachInside)).OwnedBy(CrewSlot.Gohan)
                    .OnExit(c=>{SaveCargo(_crates[n],CrewCar,new Vector3(n%2==0?-.5f:.5f,-2.5f+n/2f,.5f));_loaded++;});
            }
            yield return new MissionStage("All cargo aboard",new EnterVehicleObjective("Guess: take the Benson driver seat",()=>CrewCar,VehicleSeat.Driver),new ConditionObjective("Stop and wait for Ice in front and Gohan in the rear cargo seat",()=>BoardTeam())).OwnedBy(CrewSlot.Guess)
                .OnExit(c=>{Fighting=true;DrivingDestination=()=>At("M38.Exit");ResponseCar("M38.Response",CrewCar.Position);}).AfterCues("M38_S1_02_GOHAN");
            yield return new MissionStage("Escape the quarry",new TravelObjective("Drive the loaded Benson out through the yellow quarry escape marker",()=>At("M38.Exit"),20,()=>CrewCar)).OnExit(c=>{RetreatResponse();DrivingDestination=null;}).AfterCues("M38_S1_03_GUESS");
            yield return new MissionStage("Lose pursuit",new LoseWantedObjective("Lose the police before returning with explosives"));
            yield return new MissionStage("Deliver seismic stock",new TravelObjective("Stop the same loaded Benson at the bunker delivery marker",()=>At("M38.Senora.Delivery"),12,()=>CrewCar)).OnEnter(c=>DrivingDestination=()=>At("M38.Senora.Delivery"));
            yield return new MissionStage("Verify the load",new MissionInteraction("Gohan: inspect all four packages at the back of the stopped truck",()=>CrewCar.Position-CrewCar.ForwardVector*5.5f,4,3.5f,animation:MissionInteraction.ReachInside)).OwnedBy(CrewSlot.Gohan)
                .OnEnter(c=>DrivingDestination=null).OnExit(c=>{_delivered=_loaded==4&&_crates.All(p=>Attached(p,CrewCar));if(!_delivered)throw new InvalidOperationException("Explosives delivery is incomplete.");Establish("delivery","Four accounted for","Gohan verifies each package in the arrived truck. The crew now has measured demolition stock, but the mainland cable remains connected.",CrewCar);});
        }
        private bool BoardTeam(){Roles.Release();Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Ice);Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Gohan);bool ice=Board(Ctx.Crew.PedFor(CrewSlot.Ice),CrewCar,VehicleSeat.Passenger);bool gohan=Board(Ctx.Crew.PedFor(CrewSlot.Gohan),CrewCar,VehicleSeat.LeftRear);return ice&&gohan;}
        protected override void OnUpdate(){for(int i=0;i<_loaded;i++)if(!Attached(_crates[i],CrewCar)){Fail("A charge package came loose from the truck.");return;}base.OnUpdate();}
        protected override void OnPassed(){if(!_delivered)throw new InvalidOperationException("No delivered charge stock.");Ctx.State?.SetCargo("seismicCharges","M38.Senora.Delivery");Release(CrewCar);foreach(var crate in _crates)Release(crate);}
    }
}
