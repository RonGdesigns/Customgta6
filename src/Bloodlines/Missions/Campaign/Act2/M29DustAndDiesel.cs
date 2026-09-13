using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;
using System;
namespace Bloodlines.Missions.Campaign
{
    public sealed class M29DustAndDiesel : DesertOperation
    {
        public override string Id => "M29";public override string Title=>"Dust & Diesel";
        private Vehicle _truck,_trailer;private List<Ped> _guards;
        private Prop _controls, _panel;
        private readonly List<Prop> _reserves=new List<Prop>();
        private RoleTracks _roles;
        private bool _fuelLoaded, _unloaded;
        public bool FuelLoaded => _fuelLoaded;
        public bool Unloaded => _unloaded;
        protected override MissionEndpoint Endpoint => MissionEndpoint.SafehouseArrival;
        private readonly List<Vehicle> _pursuitCars=new List<Vehicle>();
        private readonly List<Ped> _pursuers=new List<Ped>();
        private readonly List<Blip> _pursuitBlips=new List<Blip>();
        private float _deliveryDistance;
        private bool _roadDistance, _pursuitStarted, _pursuitEnded;
        private int _nextCar, _nextOrders;
        public bool PursuitEnded => _pursuitEnded;
        public IReadOnlyList<Vehicle> PursuitCars => _pursuitCars;
        internal static bool AtRetreatPoint(float initial,float remaining) =>
            initial>0f&&remaining>=0f&&remaining<=initial*.25f;

        protected override bool Setup()
        {
            if(!MissionSites.Prepare(Ctx.Locations,Id)||!Ctx.Crew.Deploy(CrewSlot.Guess,At("M29.Approach"),0))return false;
            ProtectCrew();
            _truck=FuelRig("M29.Truck",out _trailer);_guards=Squad(At("M29.Valve"),5);
            if(!RequireAssets(_truck,_trailer)||_guards.Count!=5)return false;
            _controls=WorkProp("prop_table_03",At("M29.Valve")+new Vector3(0,1,0));
            if(!RequireAssets(_controls))return false;
            _panel=WorkProp("prop_laptop_01a",PropPlacement.OnTop(_controls,_controls.Model,new Model("prop_laptop_01a")),false);
            if(!RequireAssets(_panel))return false;
            RequireAsset(_truck,"The Phantom tractor was destroyed.");
            RequireAsset(_trailer,"The fuel tanker was destroyed.");
            RequireAsset(_panel,"The depot transfer controls were destroyed.");
            Station(CrewSlot.Ice,At("M29.Cover"));Station(CrewSlot.Gohan,At("M29.Approach"));Ctx.Crew.CompanionsHoldPosition=true;
            _roles=new RoleTracks(Ctx.Crew,()=>_guards);
            _roles.For(CrewSlot.Gohan).Observe(At("M29.Approach"),At("M29.Approach"));
            Ctx.Cutscenes.Play(new SceneSpec {MissionId=Id,Phase="approach",Title="One loaded tanker",
                Reason="Show the coupled road rig and its remote depot transfer controls. Ice clears and operates them; Guess takes the tractor; Gohan watches their approach.",
                Blocking=new SceneBlocking().Then(ShotStep.Low(2000,_truck,7,5,3))
                    .Then(ShotStep.Low(2000,_trailer,7,-5,3)).Then(ShotStep.Low(2000,_panel,3,2,2))});
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Survey the transfer depot",new ReachZoneObjective("Guess: reach the yellow rail-depot entrance. The fuel will leave by road.",()=>At("M29.Cover"),10)).OwnedBy(CrewSlot.Guess)
                .OnExit(c=>_roles.For(CrewSlot.Guess).Approach(_truck.Position+new Vector3(3,0,0),At("M29.Cover")));
            yield return new MissionStage("Secure the loading valve",new KillTargetsObjective("Ice: clear the five red guards. Keep the tanker intact.",()=>_guards),new ProtectObjective("",()=>_trailer,"The fuel tanker was destroyed.")).OwnedBy(CrewSlot.Ice).OnEnter(c=>Attack(_guards)).AfterCues("M29_S1_01_GUESS");
            yield return new MissionStage("Transfer the fuel",new MissionInteraction("Ice: use the laptop on the marked transfer table to fill the coupled tanker",()=>At("M29.Valve"),12,animation:MissionInteraction.ReachInside),new ProtectObjective("",()=>_trailer,"The tanker was lost before loading finished.")).OwnedBy(CrewSlot.Ice)
                .OnEnter(c=>_roles.For(CrewSlot.Gohan).Observe(At("M29.Approach"),At("M29.Approach")))
                .OnExit(c=>{_fuelLoaded=true;Radio("ICE","Transfer complete. The tanker is full. Guess, take the tractor; Gohan, watch the road.","M29_FILLED");}).AfterCues("M29_S1_02_ICE");
            yield return new MissionStage("Take the tractor",new EnterVehicleObjective("Guess: take the orange-marked Phantom tractor attached to the fuel tanker. The brothers will ride or follow in another car.",()=>_truck,VehicleSeat.Driver)).OwnedBy(CrewSlot.Guess).OnEnter(c=>{_roles.Release();c.Crew.CompanionsHoldPosition=false;c.Crew.CompanionAI.ReleaseAll();c.Crew.AssignCompanionAI();});
            yield return new MissionStage("Fuel for the bunker",new DeliverVehicleObjective("Guess: deliver the Phantom AND its tanker to the bunker. Reconnect if you detach it.",()=>_truck,()=>At("M29.Senora.Delivery"),25),new TrailerDeliveryObjective(()=>_truck,()=>_trailer,()=>At("M29.Senora.Delivery")),new ProtectObjective("",()=>_trailer,"The fuel tanker was destroyed.")).OwnedBy(CrewSlot.Guess).OnEnter(c=>StartPursuit()).OnExit(c=>EndPursuit());
            yield return new MissionStage("Unload the reserves",new MissionInteraction("Guess: keep the rig stopped at the bunker and start unloading the fuel reserves",()=>At("M29.Senora.Delivery"),6,30,()=>_truck,stopVehicle:true),new TrailerDeliveryObjective(()=>_truck,()=>_trailer,()=>At("M29.Senora.Delivery")),new ProtectObjective("",()=>_trailer,"The tanker was destroyed before unloading.")).OwnedBy(CrewSlot.Guess).OnExit(c=>UnloadFuel());
            yield return new MissionStage("Reserves received",new ConditionObjective("The fuel is being secured in the bunker yard.",()=>_unloaded&&!Ctx.Cutscenes.IsActive)).OwnedBy(CrewSlot.Guess).AfterCues("M29_S1_03_GUESS");
        }
        private void UnloadFuel()
        {
            if(!_fuelLoaded)throw new InvalidOperationException("The tanker never completed its transfer.");
            var bay=At("M23.FuelBay");
            for(int i=0;i<2;i++)
            {
                var drum=WorkProp("prop_barrel_02a",bay+new Vector3(i==0?-1.1f:1.1f,1.5f,0),reuse:true);
                if(!RequireAssets(drum))throw new InvalidOperationException("The receiving fuel drums could not load.");
                _reserves.Add(drum);
            }
            RequiredScene("reserves","Fuel in reserve","The tanker has arrived attached. Show the secured reserve drums at the radar yard before awarding generator fuel.",
                new SceneBlocking().Then(ShotStep.Low(2200,_trailer,8,5,3))
                    .Then(ShotStep.Low(2200,_reserves[0],4,2,2))
                    .Then(new VerifySceneStep("Fuel received",()=>_fuelLoaded&&RigAtDelivery()&&_reserves.TrueForAll(p=>p!=null&&p.Exists()),()=>_unloaded=true)));
        }
        private bool RigAtDelivery()
        {
            if(_truck==null||!_truck.Exists()||_trailer==null||!_trailer.Exists())return false;
            var coupled=new OutputArgument();
            return Function.Call<bool>(Hash.GET_VEHICLE_TRAILER_VEHICLE,_truck,coupled)&&coupled.GetResult<int>()==_trailer.Handle
                &&_truck.Position.DistanceTo(At("M29.Senora.Delivery"))<=30f&&_trailer.Position.DistanceTo(At("M29.Senora.Delivery"))<=35f
                &&_truck.Speed<2f&&_trailer.Speed<2f;
        }
        private float Remaining(bool preferRoad)
        {
            if (_truck==null||!_truck.Exists()) return 0f;
            var from=_truck.Position;var goal=At("M29.Senora.Delivery");
            if (preferRoad)
            {
                float distance=Function.Call<float>(Hash.CALCULATE_TRAVEL_DISTANCE_BETWEEN_POINTS,from.X,from.Y,from.Z,goal.X,goal.Y,goal.Z);
                if (distance>0f&&distance<90000f) return distance;
                return -1f;
            }
            return from.DistanceTo(goal);
        }
        private void StartPursuit()
        {
            _pursuitStarted=true;_pursuitEnded=false;
            _deliveryDistance=Remaining(true);_roadDistance=_deliveryDistance>0f;
            if (!_roadDistance) _deliveryDistance=Remaining(false);
            _deliveryDistance=Math.Max(100f,_deliveryDistance);
            _nextCar=Game.GameTime+4000;_nextOrders=0;
            Radio("ICE","Depot response is coming after the tanker. Keep rolling; they won't follow us all the way into the desert.","M29_PURSUIT_START");
        }
        protected override void OnUpdate()
        {
            if(Ctx.Cutscenes.IsActive)return;
            _roles?.Update();
            base.OnUpdate();
            if (Status!=MissionStatus.Running||!_pursuitStarted||_pursuitEnded||CurrentStage!=4) return;
            float remaining=Remaining(_roadDistance);
            // An unavailable route query never counts as zero distance or a retreat.
            if (AtRetreatPoint(_deliveryDistance,remaining)) { EndPursuit();return; }
            int now=Game.GameTime;
            if (_pursuitCars.Count<2&&now>=_nextCar) { SpawnPursuitCar();_nextCar=now+8000; }
            if (now<_nextOrders) return;_nextOrders=now+3000;
            var target=_truck.GetPedOnSeat(VehicleSeat.Driver);
            if (target==null||!target.Exists()) return;
            for(int i=0;i<_pursuitCars.Count;i++)
            {
                var car=_pursuitCars[i];if(car==null||!car.Exists()||!car.IsDriveable)continue;
                var driver=car.GetPedOnSeat(VehicleSeat.Driver);
                if(driver!=null&&driver.Exists()&&!driver.IsDead) Function.Call(Hash.TASK_VEHICLE_CHASE,driver,target);
                var gunner=car.GetPedOnSeat(VehicleSeat.Passenger);
                if(gunner!=null&&gunner.Exists()&&!gunner.IsDead) Function.Call(Hash.TASK_VEHICLE_SHOOT_AT_PED,gunner,target,120f);
            }
        }
        private void SpawnPursuitCar()
        {
            if (_truck==null||!_truck.Exists()) return;
            var behind=_truck.Position-_truck.ForwardVector*(100f+_pursuitCars.Count*30f);
            if (!GameUtils.NearestRoadNode(behind,60f,out var road,out float heading)||Math.Abs(road.Z-_truck.Position.Z)>25f) return;
            var model=new Model("s_m_y_blackops_01");if(!GameUtils.RequestModel(model))return;
            try
            {
                var car=Car("granger",road,heading,false);if(car==null)return;
                var occupants=new List<Ped>();
                for(int seat=0;seat<2;seat++)
                {
                    var ped=Track(World.CreatePed(model,road,heading));if(ped==null||!ped.Exists())continue;
                    ped.IsPersistent=true;ped.BlockPermanentEvents=true;ped.RelationshipGroup=World.AddRelationshipGroup("BLOODLINES_AEGIS");
                    ped.Health=220;ped.Armor=30;ped.Accuracy=15;
                    if(seat==1)ped.Weapons.Give(WeaponHash.MicroSMG,160,true,true);
                    ped.SetIntoVehicle(car,seat==0?VehicleSeat.Driver:VehicleSeat.Passenger);
                    occupants.Add(ped);_pursuers.Add(ped);
                }
                if(car.GetPedOnSeat(VehicleSeat.Driver)==null) { foreach(var ped in occupants)ped.Delete();car.Delete();return; }
                _pursuitCars.Add(car);car.IsEngineRunning=true;
                var blip=Track(car.AddBlip());if(blip!=null){blip.Color=BlipColor.Red;blip.Name="Depot pursuit";_pursuitBlips.Add(blip);}
                Function.Call(Hash.SET_DRIVER_ABILITY,car.GetPedOnSeat(VehicleSeat.Driver),1f);
            }
            finally { model.MarkAsNoLongerNeeded(); }
        }
        private void EndPursuit()
        {
            if (!_pursuitStarted||_pursuitEnded) return;_pursuitEnded=true;
            var neutral=World.AddRelationshipGroup("BLOODLINES_WITHDRAWN");
            foreach(var hero in Protagonist.All)
            {
                var ped=Ctx.Crew.PedFor(hero.Slot);if(ped==null||!ped.Exists())continue;
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS,3,neutral,ped.RelationshipGroup);
                Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS,3,ped.RelationshipGroup,neutral);
            }
            foreach(var ped in _pursuers)
                if(ped!=null&&ped.Exists()&&!ped.IsDead){ped.Task.ClearAll();ped.RelationshipGroup=neutral;}
            foreach(var car in _pursuitCars)
            {
                if(car==null||!car.Exists()||!car.IsDriveable)continue;
                var driver=car.GetPedOnSeat(VehicleSeat.Driver);
                if(driver!=null&&driver.Exists()&&!driver.IsDead)driver.Task.CruiseWithVehicle(car,20f,DrivingStyle.Normal);
            }
            foreach(var blip in _pursuitBlips)if(blip!=null&&blip.Exists())blip.Delete();
            Radio("ICE","They're backing off. Last quarter of the run is ours; bring the fuel home.","M29_PURSUIT_END");
        }
        protected override void OnCleanup()
        {
            _roles?.Release();
            _pursuitStarted=false;Ctx.Crew.CompanionsHoldPosition=false;
            _pursuitCars.Clear();_pursuers.Clear();_pursuitBlips.Clear();
        }
        protected override void OnPassed()
        {
            if(!_fuelLoaded||!_unloaded)throw new InvalidOperationException("Fuel reserves were not received.");
            Ctx.State?.SetCargo("bunkerFuel","M23.FuelBay");
            foreach(var drum in _reserves)if(drum!=null&&drum.Exists())Release(drum);
        }

    }
}
