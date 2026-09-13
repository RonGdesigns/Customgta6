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
        public int Scans => _scans;
        public Vehicle SurveySub => Sub;
        public Vehicle PickupBoat => Launch;
        protected override bool Setup()
        {
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
                .WithCues("M36_S1_01_GOHAN","M36_S1_02_ICE");
            yield return new MissionStage("Bring the survey home",new DeliverVehicleObjective("Gohan: surface in the sub beside Guess's relocated dinghy",()=>Sub,()=>At("M36.AlternatePickup"),12),new ConditionObjective("The pickup boat must remain at the alternate marker",()=>Launch.Position.DistanceTo(At("M36.AlternatePickup"))<20))
                .OwnedBy(CrewSlot.Gohan).OnExit(c=>{Recorded=_scans==3;Establish("survey","A route with limits","The sub and pickup boat arrive together. Three recorded sensor sites provide an approach map; the crew still needs smoke aircraft and demolition stock.",Sub,Launch);}).AfterCues("M36_S1_03_GOHAN");
        }
        private void ShowPatrol()
        {
            _patrolBoat=Boat("predator","M36.Patrol",2f,2f,5f);
            _patrol=Person("s_m_y_blackops_01","M36.IceStart",false);
            if(!RequireAssets(_patrolBoat,_patrol))throw new System.InvalidOperationException("The coastal patrol could not load.");
            _patrol.SetIntoVehicle(_patrolBoat,VehicleSeat.Driver);
            var b=Track(_patrolBoat.AddBlip());b.Color=BlipColor.Red;b.Name="Surface patrol";
            _patrol.Task.StartBoatMission(_patrolBoat,At("M36.PatrolGoal"),VehicleMissionType.GoTo,9f,(VehicleDrivingFlags)786603,15f,(BoatMissionFlags)7);
            Establish("patrol","The first pickup is exposed","A real surface patrol is moving toward the original pickup. Guess relocates; Gohan stays submerged and returns to the new marker.",_patrolBoat,Launch);
        }
        protected override void OnUpdate()
        {
            if(_patrol!=null&&!Recorded)CheckSurfaceExposure(_patrol);
            if(Ctx.Crew.ActiveSlot==CrewSlot.Gohan)
            {
                if(CurrentStage<3)UnderwaterGuidance.Draw(Sub,At("M36.Scan"+(CurrentStage+1)),6f);
                else if(CurrentStage==4)UnderwaterGuidance.Draw(Sub,At("M36.AlternatePickup"),12f);
            }
            base.OnUpdate();
        }
        protected override void OnPassed(){if(!Recorded)throw new System.InvalidOperationException("Survey delivery incomplete.");Ctx.State?.SetEvidence("offshoreApproachSurvey",EvidenceState.CopyHeld);Release(Sub);Release(Launch);}
    }
}
