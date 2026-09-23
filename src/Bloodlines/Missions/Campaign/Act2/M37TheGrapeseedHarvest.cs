using System;
using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
namespace Bloodlines.Missions.Campaign
{
    public sealed class M37TheGrapeseedHarvest : PreparationOperation
    {
        public override string Id => "M37";
        public override string Title => "The Grapeseed Harvest";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;
        private readonly List<Vehicle> _planes=new List<Vehicle>();
        private int _fitted,_tested,_visible;private readonly List<Tuple<Prop,Vehicle>> _tanks=new List<Tuple<Prop,Vehicle>>();
        public IList<Vehicle> Aircraft => _planes;
        public int Tested => _tested;
        /// <summary>How many of the two releases actually produced a visible plume.</summary>
        public int Visible => _visible;
        protected override bool Setup()
        {
            if(!BeginCrew(CrewSlot.Guess))return false;
            for(int i=1;i<=2;i++)
            {
                var plane=Car("duster",At("M37.Duster"+i),Ctx.Locations.Heading("M37.Duster"+i));
                if(!RequireAssets(plane))return false;
                plane.PlaceOnGround();
                RequireAsset(plane,"A required crop duster was destroyed. Both aircraft must reach the test apron.");_planes.Add(plane);
            }
            for(int i=1;i<=3;i++)Enemy("M37.Guard"+i);
            Establish("approach","Two aircraft, two pilots","Guess takes the first aircraft to Sandy Shores. Ice stays at Grapeseed to secure and fly the second. Gohan waits on the destination apron with the payload plan.",_planes[0],_planes[1]);
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("First aircraft",new EnterVehicleObjective("Guess: board the first orange crop duster in its pilot seat",()=>_planes[0],VehicleSeat.Driver)).OwnedBy(CrewSlot.Guess);
            yield return new MissionStage("First landing",new DeliverVehicleObjective("Guess: land the first crop duster at Sandy Shores and stop in its yellow apron marker",()=>_planes[0],()=>At("M37.Land1"),8,true)).OwnedBy(CrewSlot.Guess).OnExit(c=>Roles.For(CrewSlot.Guess).Stop());
            yield return new MissionStage("Secure the second strip",new KillTargetsObjective("Ice: clear the three red guards around the second aircraft",()=>Opposition)).OwnedBy(CrewSlot.Ice).OnEnter(c=>Fighting=true).AfterCues("M37_S1_02_ICE");
            yield return new MissionStage("Second aircraft",new EnterVehicleObjective("Ice: take the second crop duster's pilot seat",()=>_planes[1],VehicleSeat.Driver)).OwnedBy(CrewSlot.Ice).OnEnter(c=>Fighting=false);
            yield return new MissionStage("Second landing",new DeliverVehicleObjective("Ice: land the second crop duster at its separate Sandy Shores apron marker and stop",()=>_planes[1],()=>At("M37.Land2"),8,true)).OwnedBy(CrewSlot.Ice).OnExit(c=>Roles.For(CrewSlot.Ice).Stop());
            for(int i=0;i<2;i++)
            {
                int n=i;
                yield return new MissionStage("Fit smoke kit "+(n+1),new MissionInteraction("Gohan: fit the smoke canisters beside parked aircraft "+(n+1),()=>_planes[n].Position+_planes[n].RightVector*3f,4,3f,animation:MissionInteraction.Repair,face:()=>_planes[n].Position)).OwnedBy(CrewSlot.Gohan).OnExit(c=>Fit(n));
            }
            yield return new MissionStage("Check both releases",new MissionInteraction("Gohan: test the smoke release at the first parked aircraft",()=>_planes[0].Position+_planes[0].RightVector*3f,3,3f,animation:MissionInteraction.Operate,face:()=>_planes[0].Position)).OwnedBy(CrewSlot.Gohan)
                .OnExit(c=>Test(0)).AfterCues("M37_S1_01_GUESS");
            yield return new MissionStage("Check second release",new MissionInteraction("Gohan: test the second aircraft's smoke release",()=>_planes[1].Position+_planes[1].RightVector*3f,3,3f,animation:MissionInteraction.Operate,face:()=>_planes[1].Position)).OwnedBy(CrewSlot.Gohan)
                .OnExit(c=>{Test(1);Establish("payload","A tested screen","Both releases were operated on the parked aircraft and their canisters stay attached. Where a plume was actually seen is recorded per aircraft; an optical screen is no guarantee against radar or thermal detection.",_planes[0],_planes[1]);}).AfterCues("M37_S1_03_GOHAN");
        }
        private void Fit(int n)
        {
            for(int side=-1;side<=1;side+=2)
            {
                var tank=WorkProp("prop_barrel_02a",_planes[n].Position,false);
                if(!RequireAssets(tank))throw new InvalidOperationException("A smoke canister could not load.");
                SaveCargo(tank,_planes[n],new Vector3(side*2f,0,-.2f));RequireAsset(tank,"A required smoke canister was destroyed.");_tanks.Add(Tuple.Create(tank,_planes[n]));
            }
            _fitted++;
        }
        /// <summary>
        /// Operate one release. What this mission is actually about is physical: both
        /// canisters fitted and attached, and both releases worked. The plume is the
        /// point of the test and it is reported by aircraft, but a particle call that
        /// comes back false is not a reason to throw the mission away — that is what
        /// stopped M37 in Ron's September 13 run. The doctor carries the verdict so a
        /// playtester knows whether he saw smoke or only operated the valve.
        /// </summary>
        private void Test(int n)
        {
            if(_fitted!=2)throw new InvalidOperationException("Both smoke kits have to be fitted before either release is tested.");
            if(AircraftSmoke.Emit(_planes[n]))
            {
                _visible++;
                Ctx.Doctor?.Info("payload","M37.Plane"+(n+1),"smoke release produced a visible plume");
            }
            else
            {
                Ctx.Doctor?.Warn("payload","M37.Plane"+(n+1),"the release was operated but no plume was produced; the optical screen is unverified on this run");
                Logger.Warn("M37: release "+(n+1)+" produced no visible plume; the canister is fitted and the valve worked.");
                GameUtils.Subtitle("~y~Release "+(n+1)+" operated, but no plume was seen. Check the mission doctor.",4000);
            }
            _tested++;
        }
        protected override void OnUpdate(){foreach(var tank in _tanks)if(!Attached(tank.Item1,tank.Item2)){Fail("A smoke canister came loose before the payload test.");return;}base.OnUpdate();}
        protected override void OnPassed(){if(_tested!=2)throw new InvalidOperationException("Both releases must be tested.");Ctx.State?.SetCargo("smokeAircraft","M37.Land1");foreach(var p in _planes)Release(p);foreach(var tank in _tanks)Release(tank.Item1);}
    }
}
