using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
namespace Bloodlines.Missions.Campaign
{
    public sealed class SM06CanyonRunner : DesertOperation
    {
        public override string Id => "SM06";public override string Title=>"Canyon Runner";
        private Vehicle _truck,_trailer;private readonly List<Ped> _riders=new List<Ped>();
        protected override bool Setup()
        {
            if(!MissionSites.Prepare(Ctx.Locations,Id)||!Ctx.Crew.DeploySolo(CrewSlot.Guess,At("SM06.Approach"),0))return false;
            _truck=FuelRig("SM06.Truck",out _trailer);return RequireAssets(_truck,_trailer);
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Take the fuel rig",new EnterVehicleObjective("Guess: take the marked Phantom tractor. Keep its aviation-fuel tanker attached.",()=>_truck,VehicleSeat.Driver)).PlayedBy(CrewSlot.Guess).AfterCues("SM06_S1_01_GUESS");
            yield return new MissionStage("Canyon run",new DeliverVehicleObjective("Guess: take the fuel rig through the yellow canyon-road checkpoint. Slow for corners.",()=>_truck,()=>At("SM06.Bend"),35),new ProtectObjective("",()=>_trailer,"The aviation-fuel tanker was destroyed.")).PlayedBy(CrewSlot.Guess).OnEnter(c=>StartBikes()).WithCues("SM06_S1_02_GUESS").AfterCues("SM06_S2_03_GUESS");
            yield return new MissionStage("Airfield reserves",new DeliverVehicleObjective("Guess: deliver the tractor and attached tanker to McKenzie's yellow fuel marker.",()=>_truck,()=>At("SM06.Delivery"),25),new TrailerDeliveryObjective(()=>_truck,()=>_trailer,()=>At("SM06.Delivery")),new ProtectObjective("",()=>_trailer,"The airfield fuel was lost.")).PlayedBy(CrewSlot.Guess);
            yield return new MissionStage("Unload safely",new MissionInteraction("Guess: unload the tanker at the airfield connection",()=>At("SM06.Delivery"),6,30,()=>_truck,stopVehicle:true),new ProtectObjective("",()=>_trailer,"The tanker exploded before unloading finished.")).PlayedBy(CrewSlot.Guess).AfterCues("SM06_S2_04_GUESS");
        }
        private void StartBikes()
        {
            for(int i=0;i<3;i++)
            {
                var point=World.GetNextPositionOnStreet(_truck.Position-_truck.ForwardVector*(65+i*12)+new Vector3(i*4,0,0));
                var bike=Car("sanchez",point);var rider=Guard(point,WeaponHash.MicroSMG);
                if(!RequireAssets(bike,rider))throw new System.InvalidOperationException("A pursuit bike failed to load.");
                rider.SetIntoVehicle(bike,VehicleSeat.Driver);rider.Task.VehicleChase(Game.Player.Character);rider.Task.VehicleShootAtPed(Game.Player.Character);_riders.Add(rider);
            }
        }
    }
}
