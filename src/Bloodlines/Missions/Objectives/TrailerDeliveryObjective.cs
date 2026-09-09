using System;
using Bloodlines.Core;
using GTA;
using GTA.Math;
using GTA.Native;
namespace Bloodlines.Missions.Objectives
{
    /// <summary>Delivering the tractor alone cannot bank the trailer's cargo.</summary>
    public sealed class TrailerDeliveryObjective : Objective
    {
        private readonly Func<Vehicle> _truck,_trailer;private readonly Func<Vector3> _destination;
        public TrailerDeliveryObjective(Func<Vehicle> truck,Func<Vehicle> trailer,Func<Vector3> destination):base("Deliver the attached tanker, then stop.")
        {_truck=truck;_trailer=trailer;_destination=destination;}
        public override void Update(MissionContext c)
        {
            var truck=_truck();var trailer=_trailer();
            if(truck==null||!truck.Exists()||truck.IsDead||trailer==null||!trailer.Exists()||trailer.IsDead){Fail("The fuel rig is lost.");return;}
            var coupled = new OutputArgument();
            bool attached=Function.Call<bool>(Hash.GET_VEHICLE_TRAILER_VEHICLE,truck,coupled) && coupled.GetResult<int>() == trailer.Handle;
            if(!attached){Label="Reconnect the orange-marked tanker: reverse the Phantom tractor beneath its hitch.";ObjectiveMarkers.Navigation(trailer.Position,RequiredCharacter);return;}
            Label="Deliver the attached tanker and stop inside the yellow unloading area.";
            if(IsOwnerActive(c)&&Game.Player.Character.IsInVehicle(truck)&&trailer.Position.DistanceTo(_destination())<=35&&truck.Speed<2&&trailer.Speed<2)Complete();
        }
    }
}
