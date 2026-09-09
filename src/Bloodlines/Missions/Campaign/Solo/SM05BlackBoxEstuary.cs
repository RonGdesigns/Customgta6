using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
namespace Bloodlines.Missions.Campaign
{
    public sealed class SM05BlackBoxEstuary : DesertOperation
    {
        public override string Id => "SM05";public override string Title=>"Black Box Estuary";
        private Vehicle _boat;private Prop _buoy;
        protected override bool Setup()
        {
            if(!MissionSites.Prepare(Ctx.Locations,Id)||!Ctx.Crew.DeploySolo(CrewSlot.Gohan,At("SM05.Shore"),0))return false;
            _boat=Car("dinghy",At("SM05.Boat"));
            var model=new Model("prop_buoy_01");
            try{if(!GameUtils.RequestModel(model))return false;_buoy=Track(World.CreateProp(model,At("SM05.Buoy"),false,false));if(_buoy!=null)_buoy.IsPositionFrozen=true;}
            finally{model.MarkAsNoLongerNeeded();}
            return RequireAssets(_boat,_buoy);
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Into the estuary",new EnterVehicleObjective("Gohan: board the marked dinghy. Follow the yellow route to the monitoring buoy.",()=>_boat,VehicleSeat.Driver)).PlayedBy(CrewSlot.Gohan).AfterCues("SM05_S1_01_GOHAN");
            yield return new MissionStage("Find the service harness",new DeliverVehicleObjective("Gohan: stop the dinghy within 12m of the yellow buoy marker.",()=>_boat,()=>At("SM05.Buoy"),12),new ProtectObjective("",()=>_boat,"The pickup dinghy was destroyed.")).PlayedBy(CrewSlot.Gohan);
            yield return new MissionStage("Fit the interceptor",new MissionInteraction("Gohan: exit the dinghy and swim beside the buoy service harness",()=>At("SM05.Buoy"),10,4),new ProtectObjective("",()=>_boat,"The dinghy was lost during the installation.")).PlayedBy(CrewSlot.Gohan).WithCues("SM05_S1_02_GOHAN").AfterCues("SM05_S2_03_GOHAN");
            yield return new MissionStage("Back aboard",new EnterVehicleObjective("Gohan: climb back into the orange-marked dinghy.",()=>_boat,VehicleSeat.Driver)).PlayedBy(CrewSlot.Gohan);
            yield return new MissionStage("Shore pickup",new DeliverVehicleObjective("Gohan: return the dinghy to the yellow water pickup beside shore.",()=>_boat,()=>At("SM05.Boat"),12)).PlayedBy(CrewSlot.Gohan);
        }
    }
}
