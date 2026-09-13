using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>KJ's northbound sprint: the owned arrival car, one shared road route, a mountain finish.</summary>
    public sealed class SM03MidnightDrift : ComposedMission
    {
        private readonly List<Ped> _rivals = new List<Ped>();
        private readonly List<Vehicle> _rivalCars = new List<Vehicle>();
        private readonly int[] _next = new int[2], _orders = new int[2];
        private readonly Vector3[] _last = new Vector3[2];
        private Ped _kj;
        private Vehicle _coupe;
        private Vector3 _start;
        private List<Vector3> _route;
        private bool _won;
        public override string Id => "SM03";
        public override string Title => "Midnight Drift";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;
        public Ped KJ => _kj;
        public Vehicle Coupe => _coupe;
        public Prop Prize => null;
        public bool GunsShown => false;
        public bool PrizeHome => _won;

        public static Vehicle PersonalCar(MissionContext context)
        {
            if(context.Garages==null||context.State==null)return null;
            var player=Game.Player.Character;
            return context.State.Vehicles.Select(context.Garages.OutVehicle)
                .Where(v=>v!=null&&v.Exists()&&v.IsDriveable&&v.Model.IsCar&&player!=null&&v.Position.DistanceTo(player.Position)<70f)
                .OrderBy(v=>player.IsInVehicle(v)?0f:v.Position.DistanceTo(player.Position)).FirstOrDefault();
        }
        public static string EntryRequirement(MissionContext context)
        {
            if(context.State==null||context.State.Vehicles.Count==0)return "Buy a personal car at a dealership before starting KJ's sprint. Your own upgrades carry into the race.";
            if(PersonalCar(context)==null)return "Bring an owned road car to KJ first. Retrieve it from your garage or request KJ delivery in the phone Garage app.";
            return null;
        }
        protected override bool Setup()
        {
            _coupe=Ctx.RaceVehicle??PersonalCar(Ctx);
            if(_coupe==null||!_coupe.Exists()||!_coupe.IsDriveable){GameUtils.Notify(EntryRequirement(Ctx)??"Your race car is unavailable.");return false;}
            // Stream only the grid. The road graph is surveyed data; loading 278
            // distant checkpoints up front would stall setup and flatten overpasses.
            _start=Ctx.Locations.Position("SM03.StartLine");
            if(!MissionSites.Ground(Ctx.Locations,"SM03.StartLine"))return false;
            Ctx.Locations.Get("SM03.StartLine").Position=_start;
            _route=Ctx.Locations.All.Where(l=>l.Key.StartsWith("SM03.Sprint",StringComparison.Ordinal)).OrderBy(l=>l.Key,StringComparer.Ordinal).Select(l=>l.Position).ToList();
            if(_route.Count<2)return false;
            if(!Ctx.Crew.DeploySolo(CrewSlot.Guess,_start+new Vector3(3f,0f,0f),Ctx.Locations.Heading("SM03.StartLine")))return false;
            ApplyBibleSetting();Ctx.Abilities.Refill();
            _coupe.Position=_start;_coupe.Heading=Ctx.Locations.Heading("SM03.StartLine");_coupe.IsPersistent=true;_coupe.PlaceOnGround();
            // The owned vehicle is never mission-tracked or deleted on abort.
            Game.Player.Character.SetIntoVehicle(_coupe,VehicleSeat.Driver);
            SpawnGrid();
            if(_kj==null||!_kj.Exists()||_rivals.Count!=2)return false;
            Radio("KJ","Your car, your setup. Sprint up the freeway, then climb Chiliad on the marked trail. Follow every gate. First to the mountain finish takes twenty-five grand and the transmission deal.","SM03_SPRINT_KJ");
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Ready on the grid",new MissionInteraction("Guess: ready up in your own car to start the northbound sprint",()=>_start,3,10f,()=>_coupe,stopVehicle:true))
                .PlayedBy(CrewSlot.Guess).WithCues("SM03_S1_01_GUESS", "SM03_S1_02_GUESS");
            yield return new MissionStage("Freeway to Chiliad",new RaceCheckpointObjective("Guess: beat both rivals to the summit. Follow the street, freeway and mountain trail gates.",_route,18f,1,()=>_coupe),new ProtectObjective("",()=>_coupe,"Your personal car is wrecked. Repair it before retrying the sprint."))
                .PlayedBy(CrewSlot.Guess).OnEnter(context=>{for(int i=0;i<_rivals.Count;i++)Order(i);})
                .OnExit(context=>{_won=true;Ctx.State?.SetCargo("racePrize","SM03.Summit");Radio("KJ","Clean win. Keep your car. I'm sending the purse and lining up that race transmission for the crew.","SM03_SPRINT_WIN");})
                .AfterCues("SM03_S2_03_ENEMY", "SM03_S2_04_GUESS", "SM03_S2_05_GUESS");
        }
        private void SpawnGrid()
        {
            var kjModel=new Model("a_m_y_stbla_02");var rivalModel=new Model("g_m_y_salvaboss_01");var carModel=new Model("elegy2");
            if(!GameUtils.RequestModel(kjModel)||!GameUtils.RequestModel(rivalModel)||!GameUtils.RequestModel(carModel))return;
            var side=new Vector3(_coupe.ForwardVector.Y,-_coupe.ForwardVector.X,0f);
            _kj=Track(World.CreatePed(kjModel,_start+side*6f, _coupe.Heading+90f));
            if(_kj!=null&&_kj.Exists()){_kj.IsPersistent=true;_kj.IsInvincible=true;_kj.BlockPermanentEvents=true;_kj.RelationshipGroup=Ctx.Crew.CrewGroup;_kj.Task.StandStill(-1);}
            for(int i=0;i<2;i++)
            {
                var point=MissionPlacement.Position(Ctx.Locations,"SM03.RivalGrid"+(i+1),_start-_coupe.ForwardVector*(7f+i*6f));
                var car=Track(World.CreateVehicle(carModel,point,_coupe.Heading));var driver=Track(World.CreatePed(rivalModel,point,_coupe.Heading));
                if(car==null||!car.Exists()||driver==null||!driver.Exists())continue;
                car.IsPersistent=true;car.PlaceOnGround();driver.IsPersistent=true;driver.BlockPermanentEvents=true;
                driver.RelationshipGroup=World.AddRelationshipGroup("BLOODLINES_RACERS");driver.SetIntoVehicle(car,VehicleSeat.Driver);
                Function.Call(Hash.SET_DRIVER_ABILITY,driver,1f);Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS,driver,.65f);
                _rivalCars.Add(car);_rivals.Add(driver);var blip=Track(car.AddBlip());blip.Color=BlipColor.Red;blip.Name="Sprint rival "+(i+1);
            }
            kjModel.MarkAsNoLongerNeeded();rivalModel.MarkAsNoLongerNeeded();carModel.MarkAsNoLongerNeeded();
        }
        private void Order(int i)
        {
            var target=_route[_next[i]];float speed=target.Z>200f?20f:39f;
            _rivals[i].Task.DriveTo(_rivalCars[i],target,5f,speed,DrivingStyle.Rushed);
            _orders[i]=Game.GameTime;_last[i]=_rivalCars[i].Position;
        }
        protected override void OnUpdate()
        {
            // Check opponents before advancing the player's last gate: no false win
            // when a rival was already across the same finish on this frame.
            if(Stage==1&&!Ctx.Cutscenes.IsActive)
                for(int i=0;i<_rivals.Count;i++)
                {
                    var driver=_rivals[i];var car=_rivalCars[i];
                    if(driver==null||!driver.Exists()||driver.IsDead||car==null||!car.Exists()||!car.IsDriveable)continue;
                    if(car.Position.DistanceTo(_route[_next[i]])<18f)
                    {
                        if(++_next[i]>=_route.Count){Fail("A rival reached the mountain finish first. Keep your car and retry KJ's sprint.");return;}
                        Order(i);
                    }
                    else if(Game.GameTime-_orders[i]>6000)
                    {if(car.Position.DistanceTo(_last[i])<4f||car.Speed<2f)Order(i);else{_last[i]=car.Position;_orders[i]=Game.GameTime;}}
                }
            base.OnUpdate();
        }
        public override SceneBlocking OutroBlocking()=>_coupe==null||!_coupe.Exists()?null:new SceneBlocking().Then(new ShotStep(3200,_coupe,new Vector3(-5f,3f,2f),_coupe,Vector3.Zero,.5f));
        protected override void OnCleanup(){_rivals.Clear();_rivalCars.Clear();Ctx.RaceVehicle=null;}
    }
}
