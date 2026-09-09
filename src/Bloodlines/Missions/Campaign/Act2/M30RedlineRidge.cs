using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
namespace Bloodlines.Missions.Campaign
{
    public sealed class M30RedlineRidge : DesertOperation
    {
        public override string Id => "M30";public override string Title=>"Redline Ridge";
        private Vehicle _truck,_heli;private Ped _pilot;
        protected override bool Setup()
        {
            if(!MissionSites.Prepare(Ctx.Locations,Id)||!Ctx.Crew.Deploy(CrewSlot.Guess,At("M30.Start"),0))return false;
            _truck=Car("dubsta3",At("M30.Start"));if(!RequireAssets(_truck))return false;
            Station(CrewSlot.Ice,_truck,VehicleSeat.Passenger);Station(CrewSlot.Gohan,_truck,VehicleSeat.LeftRear);return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Collect the satellite parts",new EnterVehicleObjective("Guess: take the Dubsta 6x6. Ice and Gohan ride with the parts.",()=>_truck,VehicleSeat.Driver,true)).OwnedBy(CrewSlot.Guess).OnExit(c=>ReleaseForPickup());
            yield return new MissionStage("Down the ridge",new DeliverVehicleObjective("Guess: drive the loaded 6x6 to the yellow canyon bend. Use the road, not the cliff face.",()=>_truck,()=>At("M30.Bend"),25),new ProtectObjective("",()=>_truck,"The truck and satellite parts were destroyed.")).OwnedBy(CrewSlot.Guess).OnEnter(c=>LaunchPursuit()).WithCues("M30_S1_01_GUESS").AfterCues("M30_S1_02_GOHAN");
            yield return new MissionStage("Sheltered approach",new DeliverVehicleObjective("Guess: follow the yellow road marker out of the canyon. Passengers cover the gunship.",()=>_truck,()=>At("M30.Exit"),30),new ProtectObjective("",()=>_truck,"The parts truck was lost.")).OwnedBy(CrewSlot.Guess);
            yield return new MissionStage("Deliver the parts",new DeliverVehicleObjective("Guess: park the same 6x6 at the bunker unloading marker.",()=>_truck,()=>At("M29.Delivery"),25),new ProtectObjective("",()=>_truck,"The satellite parts never reached the bunker.")).OwnedBy(CrewSlot.Guess);
            yield return new MissionStage("Unload",new MissionInteraction("Guess: unload the satellite parts",()=>At("M29.Delivery"),5,30,()=>_truck,stopVehicle:true)).OwnedBy(CrewSlot.Guess).AfterCues("M30_S1_03_GUESS");
        }
        private void LaunchPursuit()
        {
            _heli=Car("buzzard",_truck.Position-new Vector3(0,180,-65));_pilot=Guard(At("M30.Start"));
            if(!RequireAssets(_heli,_pilot))throw new System.InvalidOperationException("The pursuit helicopter failed to load.");
            _pilot.SetIntoVehicle(_heli,VehicleSeat.Driver);_heli.IsEngineRunning=true;
            _pilot.Task.StartHeliMission(_heli,Game.Player.Character,VehicleMissionType.Attack,35,35,60,25,0,20,HeliMissionFlags.None);
            Ctx.Cutscenes.PlayMoment(Id,"Gunship over the ridge","GOHAN","Gunship behind the truck. Take the canyon road; we will cover you from here.",_pilot);
        }
    }
}
