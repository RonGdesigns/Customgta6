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
    public sealed class M33TheInformantsGrave : PreparationOperation
    {
        public override string Id => "M33";public override string Title => "The Informant's Grave";
        protected override MissionEndpoint Endpoint=>MissionEndpoint.SecuredDelivery;
        private Ped _ramos;private Vehicle _halftrack;private int _executionAt;private bool _freed,_transferred;
        public Ped Ramos=>_ramos;public bool Freed=>_freed;public bool ExecutionStarted=>_executionAt>0;
        /// <summary>
        /// The four men on the firing line. The rescue clock is their execution, so it runs
        /// while any of them can still carry it out. It used to run until Gohan had cut the
        /// restraints, which put four kills and his walk of about 135 m from his start under
        /// one ninety-second clock; with the line down there is nobody left to shoot Ramos
        /// (the September 22 audit).
        /// </summary>
        private readonly List<Ped> _firingLine=new List<Ped>();
        /// <summary>Whether the clock is still running: started, Ramos not yet free, and somebody on the line alive.</summary>
        public bool ClockRunning=>!_freed&&_executionAt>0&&_firingLine.Any(p=>p!=null&&p.Exists()&&!p.IsDead);
        /// <summary>How long Ramos may be out of the car on the way to the armor before the rule fails the run.</summary>
        public const int RamosOutGraceMs = 3000;
        private bool _ramosRiding;private int _ramosOutSince=-1;
        private string _fault;
        protected override bool Setup()
        {
            _fault=null;_ramosRiding=false;_ramosOutSince=-1;_firingLine.Clear();
            if(!BeginCrew(CrewSlot.Ice))return false;
            CrewCar=CrewTransport("M33.CrewCar");_ramos=Person("s_m_m_scientist_01","M33.Ramos",false);
            if(!RequireAssets(CrewCar,_ramos))return false;
            RequireSurvivor(_ramos,"Ramos was killed. Restart the rescue.");
            _ramos.Task.HandsUp(-1);
            Station(CrewSlot.Guess,CrewCar,VehicleSeat.Driver);Roles.For(CrewSlot.Guess).Stop();
            for(int i=1;i<=4;i++){var guard=Enemy("M33.Guard"+i);if(guard!=null)_firingLine.Add(guard);}
            Establish("approach","The man on his knees","Ramos is visibly held by the execution squad. Ice approaches from cover, Gohan frees him, and Guess brings the four-seat car. The rescue clock starts when the team reaches the firing line, not during travel.",_ramos,CrewCar);
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Identify the prisoner",new ReachZoneObjective("Ice: reach the yellow observation point; Ramos is the unarmed prisoner. Do not shoot him.",()=>At("M33.Observe"),12)).OwnedBy(CrewSlot.Ice).OnExit(c=>Engage());
            yield return new MissionStage("Stop the execution",new KillTargetsObjective("Stop the four red execution guards before the rescue clock expires. Protect Ramos.",()=>Opposition))
                .WithCues("M33_S1_01_ICE");
            yield return new MissionStage("Free Ramos",new MissionInteraction("Gohan: reach Ramos and cut his restraints",()=>_ramos.Position,4,3f,animation:MissionInteraction.ReachInside)).OwnedBy(CrewSlot.Gohan)
                .OnExit(c=>{_freed=true;Fighting=false;_ramos.Task.ClearAll();_ramos.RelationshipGroup=Ctx.Crew.PedFor(CrewSlot.Guess).RelationshipGroup;Radio("GOHAN","Ramos is free. Guess, bring the car to this marker. We are taking him out in a seat, not asking for codes.","M33_FREE");});
            yield return new MissionStage("Bring the extraction car",new TravelObjective("Guess: drive to the yellow pickup marker beside Ramos and stop",()=>At("M33.Pickup"),10,()=>CrewCar)).OwnedBy(CrewSlot.Guess);
            yield return new MissionStage("All four aboard",new ConditionObjective("Keep the car stopped: Ramos takes the front passenger seat, Ice and Gohan take the back",()=>BoardRamosAndCrew())).OwnedBy(CrewSlot.Guess)
                .OnExit(c=>{DrivingDestination=()=>At("M33.Transfer");Fighting=true;_ramosRiding=true;_ramosOutSince=-1;ResponseCar("M33.Response",CrewCar.Position);}).AfterCues("M33_S1_02_GUESS");
            // "Must remain" is a rule, and a condition objective is not one: it completes the
            // first frame it is true and never looks again. The rule is enforced in OnUpdate
            // for the whole drive (the September 22 audit).
            yield return new MissionStage("Reach armored transport",new TravelObjective("Drive Ramos and both brothers to the yellow armored-transfer marker",()=>At("M33.Transfer"),14,()=>CrewCar),new ConditionObjective("Ramos must remain in the extraction car",()=>_ramos.IsInVehicle(CrewCar)))
                .OnExit(c=>PrepareTransfer());
            yield return new MissionStage("Move Ramos into cover",new ConditionObjective("Stop beside the half-track and wait for Ramos to board its front passenger seat",()=>Board(_ramos,_halftrack,VehicleSeat.Passenger)))
                .OnExit(c=>{_transferred=true;Establish("handoff","Armor for the next road","Ramos gets out of the crew car and into the waiting half-track. The brothers keep their actual positions. His medical evacuation is next; the codes wait.",_halftrack,CrewCar);})
                .AfterCues("M33_S1_03_GOHAN");
        }
        private void Engage(){if(_executionAt>0)return;_executionAt=Game.GameTime;Fighting=true;Radio("ICE","Four on the firing line. Ninety seconds at most. Stop the guards and get Gohan to Ramos.","M33_CLOCK");}
        private bool BoardRamosAndCrew(){bool brothers=BoardBrothers(CrewCar);return Board(_ramos,CrewCar,VehicleSeat.Passenger)&&brothers;}
        private void PrepareTransfer()
        {
            _ramosRiding=false;
            DrivingDestination=null;RetreatResponse();
            _halftrack=Car("halftrack",At("M33.Halftrack"),Ctx.Locations.Heading("M33.Halftrack"));
            // Thrown from the stage exit this was a "Script error"; kept and failed with its
            // reason on the next frame instead (the September 22 audit).
            if(!RequireAssets(_halftrack)){_fault="The rescue half-track failed to load. Retry the transfer.";return;}
            RequireAsset(_halftrack,"The waiting half-track was destroyed.");
            _ramos.Task.LeaveVehicle();
        }
        protected override void OnUpdate()
        {
            if(!_freed&&_executionAt==0&&(Game.Player.Character.Position.DistanceTo(_ramos.Position)<65f||Game.Player.Character.IsShooting&&Game.Player.Character.Position.DistanceTo(_ramos.Position)<180f))Engage();
            if(_fault!=null){Fail(_fault);return;}
            if(ClockRunning)
            {
                int left=Math.Max(0,90-(Game.GameTime-_executionAt)/1000);GameUtils.Subtitle("Rescue Ramos: "+left+"s remaining",1000);
                if(left==0){Fail("The rescue window closed before Ramos was freed. Restart and stop the firing line.");return;}
            }
            if(_ramosRiding&&_ramos!=null&&_ramos.Exists()&&!_ramos.IsDead&&CrewCar!=null&&CrewCar.Exists())
            {
                if(_ramos.IsInVehicle(CrewCar))_ramosOutSince=-1;
                else if(_ramosOutSince<0)_ramosOutSince=Game.GameTime;
                else if(Game.GameTime-_ramosOutSince>RamosOutGraceMs){Fail("Ramos left the extraction car before the armored transfer. Keep him aboard.");return;}
            }
            base.OnUpdate();
        }
        protected override void OnPassed(){if(!_transferred)throw new InvalidOperationException("Ramos has not reached armored transport.");Ctx.State?.SetCargo("ramos","M34.Start");}
    }
}
