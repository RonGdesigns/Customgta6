using System;
using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
namespace Bloodlines.Missions.Campaign
{
    public sealed class M32BlackSiteZancudo : PreparationOperation
    {
        public override string Id => "M32";public override string Title => "Black Site Zancudo";
        protected override MissionEndpoint Endpoint=>MissionEndpoint.SecuredDelivery;
        private Vehicle _boat; private Prop _panel,_gate,_caseOne,_caseTwo;
        private int _loaded; private bool _access;
        public int CasesLoaded=>_loaded; public Vehicle Extraction=>CrewCar;
        protected override bool Setup()
        {
            if(!BeginCrew(CrewSlot.Gohan))return false;
            _boat=Car("dinghy",MarineSites.ResolveOrThrow(Ctx.Locations,"M32.Boat",2f,2f,4f),Ctx.Locations.Heading("M32.Boat"));
            CrewCar=CrewTransport("M32.CrewCar");if(!RequireAssets(_boat,CrewCar))return false;
            Station(CrewSlot.Gohan,_boat,VehicleSeat.Driver);Station(CrewSlot.Guess,CrewCar,VehicleSeat.Driver);Roles.For(CrewSlot.Guess).Stop();
            Equipment("prop_portacabin01","M32.Office");
            _panel=Equipment("prop_elecbox_12","M32.Panel");_gate=Equipment("prop_barrier_work05","M32.Gate");
            _caseOne=Equipment("prop_security_case_01","M32.CaseOne");_caseTwo=Equipment("prop_security_case_01","M32.CaseTwo");
            RequireAsset(_caseOne,"The first EMP case was destroyed.");RequireAsset(_caseTwo,"The second EMP case was destroyed.");
            for(int i=1;i<=4;i++)Enemy("M32.Guard"+i);
            Establish("approach","Service route","Gohan approaches the coastal landing by dinghy and disembarks at the bank. The marked outdoor service control opens the ordnance yard approach. Ice covers the yard; Guess waits in the actual extraction car. No underground room is implied.",_boat,_panel,_caseOne);
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Reach the coastal landing",new TravelObjective("Gohan: pilot the dinghy to the yellow coastal landing and stop near shore",()=>At("M32.LandingWater"),15,()=>_boat)).OwnedBy(CrewSlot.Gohan);
            yield return new MissionStage("Open exterior access",new MissionInteraction("Gohan: leave the dinghy, walk up the bank and use the marked exterior electrical cabinet",()=>At("M32.PanelWork"),5,3f,animation:MissionInteraction.ReachInside,face:()=>_panel.Position)).OwnedBy(CrewSlot.Gohan)
                .OnExit(c=>{_access=true;_gate.Heading+=90f;Establish("access","An open service route","The barrier turns aside at the exterior service access. Ice can approach the visible cases; there is no underground bunker or numbered door.",_gate,_caseOne);}).AfterCues("M32_S1_01_GOHAN");
            yield return new MissionStage("Secure the ordnance yard",new KillTargetsObjective("Ice: stop the four marked yard guards. Keep both yellow EMP cases intact.",()=>Opposition)).OwnedBy(CrewSlot.Ice)
                .OnEnter(c=>{Fighting=true;Roles.For(CrewSlot.Gohan).Observe(At("M32.PanelWork"),At("M32.PanelWork"));});
            yield return new MissionStage("Bring the case carrier",new TravelObjective("Guess: drive the crew car to the yellow pickup beside the cleared ordnance post. Gohan and Ice ride over with you",()=>At("M32.Pickup"),10,()=>CrewCar)).OwnedBy(CrewSlot.Guess)
                // The post is cleared and the cases are over there, so the brothers ride
                // across with him rather than being left where the fight was. Asked for,
                // not required: a brother who cannot reach the car must not strand the
                // drive, so the objective still only wants Guess at the marker.
                .OnEnter(c=>{Fighting=false;RideAlong();});
            yield return new MissionStage("First case",new MissionInteraction("Gohan: pick up the first marked EMP case",()=>_caseOne.Position,3,3f,animation:MissionInteraction.ReachInside)).OwnedBy(CrewSlot.Gohan)
                .OnEnter(c=>Fighting=false).OnExit(c=>Carry(_caseOne,CrewSlot.Gohan));
            yield return new MissionStage("Stow first case",new MissionInteraction("Gohan: carry the case to the rear of Guess's car and stow it",()=>CrewCar.Position-CrewCar.ForwardVector*3f,3,3.5f,animation:MissionInteraction.ReachInside)).OwnedBy(CrewSlot.Gohan)
                .OnExit(c=>{SaveCargo(_caseOne,CrewCar,new Vector3(-.35f,-1.6f,.4f));_loaded=1;});
            yield return new MissionStage("Second case",new MissionInteraction("Gohan: return for the second marked EMP case",()=>_caseTwo.Position,3,3f,animation:MissionInteraction.ReachInside)).OwnedBy(CrewSlot.Gohan).OnExit(c=>Carry(_caseTwo,CrewSlot.Gohan));
            yield return new MissionStage("Stow second case",new MissionInteraction("Gohan: stow the second case beside the first in Guess's car",()=>CrewCar.Position-CrewCar.ForwardVector*3f,3,3.5f,animation:MissionInteraction.ReachInside)).OwnedBy(CrewSlot.Gohan)
                .OnExit(c=>{SaveCargo(_caseTwo,CrewCar,new Vector3(.35f,-1.6f,.4f));_loaded=2;}).AfterCues("M32_S1_02_ICE");
            yield return new MissionStage("Extract the team",new EnterVehicleObjective("Guess: take the extraction car's driver seat",()=>CrewCar,VehicleSeat.Driver),new ConditionObjective("Stop the car and wait for Ice and Gohan to board their rear seats",()=>BoardBrothers(CrewCar))).OwnedBy(CrewSlot.Guess)
                .OnExit(c=>{DrivingDestination=()=>At("M32.Exit");Fighting=true;ResponseCar("M32.Response",CrewCar.Position);});
            yield return new MissionStage("Leave the base",new TravelObjective("Drive the case-loaded crew car to the yellow road escape marker",()=>At("M32.Exit"),20,()=>CrewCar)).OnExit(c=>{RetreatResponse();DrivingDestination=null;});
            yield return new MissionStage("Lose pursuit",new LoseWantedObjective("Lose the police before bringing military cargo to the bunker."));
            yield return new MissionStage("Deliver both cases",new TravelObjective("Return the same car with both cases to the bunker unloading marker and stop",()=>At("M32.Senora.Delivery"),15,()=>CrewCar)).OnEnter(c=>DrivingDestination=()=>At("M32.Senora.Delivery"));
            yield return new MissionStage("Secure the warheads",new MissionInteraction("Gohan: get out and secure both cases at the marked bunker work area",()=>At("M32.Senora.Workbench"),4,4f,animation:MissionInteraction.ReachInside)).OwnedBy(CrewSlot.Gohan)
                .OnEnter(c=>{DrivingDestination=null;Fighting=false;}).OnExit(c=>Unload()).AfterCues("M32_S1_03_GUESS");
        }
        private void Unload()
        {
            if(!_access||_loaded!=2||!Attached(_caseOne,CrewCar)||!Attached(_caseTwo,CrewCar))throw new InvalidOperationException("Both EMP cases must arrive in the extraction car.");
            var bench=Equipment("prop_table_03","M32.Senora.Workbench");
            float height=bench.Model.Dimensions.Item2.Z-_caseOne.Model.Dimensions.Item1.Z+.01f;
            SaveCargo(_caseOne,bench,new Vector3(-.4f,0,height));SaveCargo(_caseTwo,bench,new Vector3(.4f,0,height));
            Establish("delivery","Hardware secured","Two physical cases are unloaded onto the bunker table. The warheads provide hardware, not rig access codes.",bench);
        }
        /// <summary>
        /// Put the other two in Guess's car for the run across the base. Best effort by
        /// design: they are ordered in, and nothing waits on them.
        /// </summary>
        private void RideAlong()
        {
            if(CrewCar==null||!CrewCar.Exists())return;
            foreach(var pair in new[]{Tuple.Create(CrewSlot.Gohan,VehicleSeat.Passenger),Tuple.Create(CrewSlot.Ice,VehicleSeat.LeftRear)})
            {
                var brother=Ctx.Crew.PedFor(pair.Item1);
                if(brother==null||!brother.Exists()||brother.IsDead)continue;
                if(Ctx.Crew.ActiveSlot==pair.Item1)continue;
                if(CrewCar.GetPedOnSeat(pair.Item2)!=null)continue;
                Roles.For(pair.Item1).Stop();
                Ctx.Crew.CompanionAI.TakeControl(pair.Item1);
                if(brother.IsInVehicle()&&!brother.IsInVehicle(CrewCar))brother.Task.LeaveVehicle();
                CrewBoarding.RunAboard(brother,CrewCar,pair.Item2);
            }
        }

        protected override void OnUpdate()
        {
            if(_loaded>0&&CurrentStage<12&&(!Attached(_caseOne,CrewCar)||(_loaded==2&&!Attached(_caseTwo,CrewCar)))){Fail("An EMP case came loose from the extraction car.");return;}
            base.OnUpdate();
        }
        protected override void OnPassed(){Ctx.State?.SetCargo("empWarheads","M32.Senora.Workbench");}
    }
}
