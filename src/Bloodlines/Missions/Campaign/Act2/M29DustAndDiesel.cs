using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
namespace Bloodlines.Missions.Campaign
{
    public sealed class M29DustAndDiesel : DesertOperation
    {
        public override string Id => "M29";public override string Title=>"Dust & Diesel";
        private Vehicle _truck,_trailer;private List<Ped> _guards;
        protected override bool Setup()
        {
            if(!MissionSites.Prepare(Ctx.Locations,Id)||!Ctx.Crew.Deploy(CrewSlot.Guess,At("M29.Approach"),0))return false;
            _truck=FuelRig("M29.Truck",out _trailer);_guards=Squad(At("M29.Valve"),5);
            if(!RequireAssets(_truck,_trailer)||_guards.Count!=5)return false;
            Station(CrewSlot.Ice,At("M29.Cover"));Station(CrewSlot.Gohan,At("M29.Approach"));Ctx.Crew.CompanionsHoldPosition=true;return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Survey the transfer depot",new ReachZoneObjective("Guess: reach the yellow rail-depot entrance. The fuel will leave by road.",()=>At("M29.Cover"),10)).OwnedBy(CrewSlot.Guess);
            yield return new MissionStage("Secure the loading valve",new KillTargetsObjective("Ice: clear the five red guards. Keep the tanker intact.",()=>_guards),new ProtectObjective("",()=>_trailer,"The fuel tanker was destroyed.")).OwnedBy(CrewSlot.Ice).OnEnter(c=>Attack(_guards)).AfterCues("M29_S1_01_GUESS");
            yield return new MissionStage("Transfer the fuel",new MissionInteraction("Ice: open the marked transfer valve and fill the tanker",()=>At("M29.Valve"),12),new ProtectObjective("",()=>_trailer,"The tanker was lost before loading finished.")).OwnedBy(CrewSlot.Ice).AfterCues("M29_S1_02_ICE");
            yield return new MissionStage("Take the tractor",new EnterVehicleObjective("Guess: take the orange-marked Phantom tractor attached to the fuel tanker.",()=>_truck,VehicleSeat.Driver)).OwnedBy(CrewSlot.Guess).OnEnter(c=>{c.Crew.CompanionsHoldPosition=false;c.Crew.CompanionAI.ReleaseAll();});
            yield return new MissionStage("Fuel for the bunker",new DeliverVehicleObjective("Guess: deliver the Phantom AND its tanker to the bunker. Reconnect if you detach it.",()=>_truck,()=>At("M29.Delivery"),25),new TrailerDeliveryObjective(()=>_truck,()=>_trailer,()=>At("M29.Delivery")),new ProtectObjective("",()=>_trailer,"The fuel tanker was destroyed.")).OwnedBy(CrewSlot.Guess);
            yield return new MissionStage("Unload the reserves",new MissionInteraction("Guess: stop beside the bunker fuel connection and unload",()=>At("M29.Delivery"),6,30,()=>_truck,stopVehicle:true),new ProtectObjective("",()=>_trailer,"The tanker was destroyed before unloading.")).OwnedBy(CrewSlot.Guess).AfterCues("M29_S1_03_GUESS");
        }
    }
}
