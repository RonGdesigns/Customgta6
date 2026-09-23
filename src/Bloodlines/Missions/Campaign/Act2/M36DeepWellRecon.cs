using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
namespace Bloodlines.Missions.Campaign
{
    public sealed class M36DeepWellRecon : CoastalOperation
    {
        public override string Id => "M36";
        public override string Title => "Deep Well Recon";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;
        private readonly List<Prop> _sensors = new List<Prop>();
        private Vehicle _patrolBoat; private Ped _patrol; private int _scans;
        /// <summary>Guess has moved the pickup; what "stage four" used to mean for the guidance.</summary>
        private bool _pickupMoved;
        /// <summary>Why a stage exit could not finish, failed on the next frame instead of thrown.</summary>
        private string _fault;
        public int Scans => _scans;
        public Vehicle SurveySub => Sub;
        public Vehicle PickupBoat => Launch;
        protected override bool Setup()
        {
            _fault = null; _pickupMoved = false;
            if (!BeginCoast()) return false;
            for (int i=1;i<=3;i++){var sensor=SeabedEquipment("M36.Survey"+i);RequireAsset(sensor,"A required survey sensor was destroyed.");_sensors.Add(sensor);}
            Establish("approach", "Survey the approach", "Gohan starts in the sub. Three visible seabed sensor housings mark the coastal approach; Ice watches shore and Guess holds the pickup boat. The rig itself is farther offshore and is not represented by an invisible structure.", Sub, _sensors[0], Launch);
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            for (int i=1;i<=3;i++)
            {
                int site=i;
                yield return new MissionStage("Survey band "+site,
                    new DeliverVehicleObjective("Gohan: descend to underwater survey marker "+site+" in the sub",()=>Sub,()=>At("M36.Scan"+site),6f),
                    new MissionInteraction("Gohan: hold beside the visible seabed sensor and record survey "+site,()=>At("M36.Scan"+site),5,7f,()=>Sub,true))
                    .OwnedBy(CrewSlot.Gohan).OnExit(c=>{_scans++;if(site==3)ShowPatrol();});
            }
            yield return new MissionStage("Move the surface pickup",new TravelObjective("Guess: move the dinghy to the alternate yellow pickup, away from the patrol, and stop",()=>At("M36.AlternatePickup"),12,()=>Launch)).OwnedBy(CrewSlot.Guess)
                .OnExit(c=>_pickupMoved=true)
                .WithCues("M36_S1_01_GOHAN","M36_S1_02_ICE");
            // The boat being there is checked when the sub arrives, not once. A condition
            // completes the first frame it is true, which was the moment this stage opened
            // with Guess already on the marker, so a boat that had drifted or been moved by
            // the time Gohan surfaced still counted (the September 22 audit).
            yield return new MissionStage("Bring the survey home",new DeliverVehicleObjective("Gohan: surface in the sub beside Guess's relocated dinghy",()=>Sub,()=>At("M36.AlternatePickup"),12),new ConditionObjective("The pickup boat must remain at the alternate marker",()=>PickupHeldAtArrival()))
                .OwnedBy(CrewSlot.Gohan).OnExit(c=>{Recorded=_scans==3;Establish("survey","A route with limits","The sub and pickup boat arrive together. Three recorded sensor sites provide an approach map; the crew still needs smoke aircraft and demolition stock.",Sub,Launch);}).AfterCues("M36_S1_03_GOHAN");
        }
        /// <summary>Both craft at the alternate pickup together: the sub surfaced beside the boat, and the boat still on its marker.</summary>
        private bool PickupHeldAtArrival()
        {
            if(Sub==null||!Sub.Exists()||Launch==null||!Launch.Exists())return false;
            var pickup=At("M36.AlternatePickup");
            return Launch.Position.DistanceTo(pickup)<20f&&Sub.Position.DistanceTo(pickup)<=12f;
        }
        /// <summary>
        /// The patrol that makes the first pickup unsafe. Resolving its water can throw, and
        /// from the stage exit that was a "Script error"; the reason is kept and fails the
        /// attempt on the next frame instead (the September 22 audit).
        /// </summary>
        private void ShowPatrol()
        {
            try { SpawnPatrol(); }
            catch(System.Exception ex){Logger.Error(Id+" coastal patrol",ex);_fault="The coastal patrol could not be placed on open water. Survey M36.Patrol and retry.";}
        }
        private void SpawnPatrol()
        {
            _patrolBoat=Boat("predator","M36.Patrol",2f,2f,5f);
            _patrol=Person("s_m_y_blackops_01","M36.IceStart",false);
            if(!RequireAssets(_patrolBoat,_patrol)){_fault="The coastal patrol could not load. Retry the survey.";return;}
            _patrol.SetIntoVehicle(_patrolBoat,VehicleSeat.Driver);
            var b=Track(_patrolBoat.AddBlip());b.Color=BlipColor.Red;b.Name="Surface patrol";
            _patrol.Task.StartBoatMission(_patrolBoat,At("M36.PatrolGoal"),VehicleMissionType.GoTo,9f,(VehicleDrivingFlags)786603,15f,(BoatMissionFlags)7);
            Establish("patrol","The first pickup is exposed","A real surface patrol is moving toward the original pickup. Guess relocates; Gohan stays submerged and returns to the new marker.",_patrolBoat,Launch);
        }
        protected override void OnUpdate()
        {
            if(_fault!=null){Fail(_fault);return;}
            if(_patrol!=null&&!Recorded)CheckSurfaceExposure(_patrol);
            if(Ctx.Crew.ActiveSlot==CrewSlot.Gohan)
            {
                // Guided by what has been done, not by the stage number (the September 22 audit).
                if(_scans<3)UnderwaterGuidance.Draw(Sub,At("M36.Scan"+(_scans+1)),6f);
                else if(_pickupMoved&&!Recorded)UnderwaterGuidance.Draw(Sub,At("M36.AlternatePickup"),12f);
            }
            base.OnUpdate();
        }
        protected override void OnPassed(){if(!Recorded)throw new System.InvalidOperationException("Survey delivery incomplete.");Ctx.State?.SetEvidence("offshoreApproachSurvey",EvidenceState.CopyHeld);Release(Sub);Release(Launch);}
    }
}
