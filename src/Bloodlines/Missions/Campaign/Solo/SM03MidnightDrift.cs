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
    /// <summary>
    /// KJ's northbound sprint: the owned arrival car, a direct run at Chiliad, a mountain finish.
    ///
    /// Ron played this and reported two things. It was "a lot of looping around and not a
    /// straight path to the mountain," and "the AI wasn't a challenge." Both were true, and
    /// neither was the thing it looked like.
    ///
    /// **The route.** 278 gates covered 18.97 km to travel 7.27 km of straight line. Sixty-nine
    /// of them moved backward along the start-to-summit axis, the run crawled downtown surface
    /// streets rather than the freeway KJ names, and it entered the Mount Chiliad zone four
    /// separate times with a Braddock Tunnel round trip in the middle. That is the loop he
    /// felt, and it is measurable rather than a matter of taste. It is 34 gates over 12.00 km
    /// now, with not one gate further from the finish than the gate before it. The corridor was
    /// not invented: every one of those 278 keys was a real road position off the offline graph,
    /// so the new route is the shortest chain through that same set of positions, which cuts
    /// each loop at the point where the old route passed near itself.
    ///
    /// **Which also clears a quarter of the survey backlog.** Those 278 keys were 27% of the
    /// 1,091 estimates in the whole campaign, for one solo mission.
    ///
    /// **The rivals.** Their driving ability was already at the engine's maximum, so there was
    /// nothing to raise. What made them slow was the order: a flat 39 m/s, about 87 mph, while
    /// the player's car runs to twice its stock redline because <see cref="WorldTuning"/> lifts
    /// every ceiling in the world, theirs included. They were obeying a slow instruction in a
    /// fixed Elegy while Ron arrived in whatever he had built. They drive **his model** now,
    /// with the performance parts fitted, at a speed taken from the car's own capability, with
    /// the bounded correction in <see cref="RacePacing"/> keeping the race close at both ends.
    ///
    /// **The gates on the mountain are not snapped to a road.** <see cref="RaceRoute"/> puts
    /// every drivable gate onto the nearest real lane at runtime, which is the M49 answer to
    /// roads being baked terrain. A trail gate is passed through untouched, because the nearest
    /// vehicle node to a point halfway up Chiliad is the road at the bottom of the mountain,
    /// and snapping there would not correct the climb, it would delete it.
    /// </summary>
    public sealed class SM03MidnightDrift : ComposedMission
    {
        /// <summary>How close is close enough to a gate.</summary>
        public const float GateRadius = 22f;
        /// <summary>Above this height a gate is on the trail, so the rivals slow for it.</summary>
        public const float TrailHeight = 200f;
        /// <summary>How often a rival's pace is reviewed.</summary>
        public const int ReviewMs = 700;
        /// <summary>A rival this stuck, for this long, is re-tasked.</summary>
        public const int StallMs = 5000;
        public const float StallMeters = 4f;

        private readonly List<Ped> _rivals = new List<Ped>();
        private readonly List<Vehicle> _rivalCars = new List<Vehicle>();
        private readonly int[] _next = new int[2], _reviewed = new int[2], _movedAt = new int[2];
        private readonly float[] _commanded = new float[2];
        private readonly Vector3[] _last = new Vector3[2];
        private Ped _kj;
        private Vehicle _coupe;
        private Vector3 _start;
        private RaceRoute.Route _route;
        private bool _won;
        public override string Id => "SM03";
        public override string Title => "Midnight Drift";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;
        public Ped KJ => _kj;
        public Vehicle Coupe => _coupe;
        public Prop Prize => null;
        public bool GunsShown => false;
        public bool PrizeHome => _won;
        /// <summary>The route the sprint is actually run on, once it has been built.</summary>
        public RaceRoute.Route Route => _route;
        public IReadOnlyList<Vehicle> RivalCars => _rivalCars;
        /// <summary>What each rival was last told to drive at, for the doctor and the tests.</summary>
        public float CommandedSpeed(int rival) => rival >= 0 && rival < _commanded.Length ? _commanded[rival] : 0f;

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
            _start=Ctx.Locations.Position("SM03.StartLine");
            if(!MissionSites.Ground(Ctx.Locations,"SM03.StartLine"))return false;
            Ctx.Locations.Get("SM03.StartLine").Position=_start;
            // Road gates go onto real lanes; the mountain trail keeps its own geometry.
            _route=RaceRoute.Build(Ctx.Locations,"SM03.Leg","SM03.Trail","SM03.Summit");
            if(!_route.IsUsable){Logger.Error(Id+": the sprint route has fewer than two gates.");return false;}
            Ctx.Doctor?.Warn("route",Id,_route.Report);
            if(!Ctx.Crew.DeploySolo(CrewSlot.Guess,_start+new Vector3(3f,0f,0f),Ctx.Locations.Heading("SM03.StartLine")))return false;
            ApplyBibleSetting();Ctx.Abilities.Refill();
            _coupe.Position=_start;_coupe.Heading=Ctx.Locations.Heading("SM03.StartLine");_coupe.IsPersistent=true;_coupe.PlaceOnGround();
            // The owned vehicle is never mission-tracked or deleted on abort.
            Game.Player.Character.SetIntoVehicle(_coupe,VehicleSeat.Driver);
            SpawnGrid();
            if(_kj==null||!_kj.Exists()||_rivals.Count!=2)return false;
            Radio("KJ","Your car, your setup, and I put the boys in the same thing so nobody argues after. North out of the city, then the Chiliad trail to the top. First one up takes twenty-five grand and the transmission deal.","SM03_SPRINT_KJ");
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Ready on the grid",new MissionInteraction("Guess: ready up in your own car to start the northbound sprint",()=>_start,3,10f,()=>_coupe,stopVehicle:true))
                .PlayedBy(CrewSlot.Guess).WithCues("SM03_S1_01_GUESS", "SM03_S1_02_GUESS");
            yield return new MissionStage("North to Chiliad",new RaceCheckpointObjective("Guess: beat both rivals to the summit. Run the gates north, then climb the trail.",_route.Gates,GateRadius,1,()=>_coupe),new ProtectObjective("",()=>_coupe,"Your personal car is wrecked. Repair it before retrying the sprint."))
                .PlayedBy(CrewSlot.Guess).OnEnter(context=>{for(int i=0;i<_rivals.Count;i++){_movedAt[i]=Game.GameTime;Order(i,0f);}})
                .OnExit(context=>{_won=true;Ctx.State?.SetCargo("racePrize","SM03.Summit");Radio("KJ","Clean win, and they were on you the whole way. Keep your car. I'm sending the purse and lining up that race transmission for the crew.","SM03_SPRINT_WIN");})
                .AfterCues("SM03_S2_03_ENEMY", "SM03_S2_04_GUESS", "SM03_S2_05_GUESS");
        }

        /// <summary>
        /// The grid. The rivals drive the player's own model, because a fixed Elegy against
        /// whatever he has built is not a race: KJ fielding two of the same car is both fair
        /// and the reason his line says so. The model is already streamed, since the player is
        /// sitting in one. A model that will not load falls back to the Elegy the mission
        /// always used rather than refusing to start.
        /// </summary>
        private void SpawnGrid()
        {
            var kjModel=new Model("a_m_y_stbla_02");var rivalModel=new Model("g_m_y_salvaboss_01");
            var carModel=_coupe.Model;
            if(!GameUtils.RequestModel(carModel)){carModel=new Model("elegy2");Logger.Warn(Id+": the player's model would not load for the rivals; using the stock Elegy grid.");}
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
                FitForRacing(car);
                driver.RelationshipGroup=World.AddRelationshipGroup("BLOODLINES_RACERS");driver.SetIntoVehicle(car,VehicleSeat.Driver);
                driver.AlwaysKeepTask=true;
                // Ability was already the engine's maximum. The aggressiveness was not.
                Function.Call(Hash.SET_DRIVER_ABILITY,driver,1f);Function.Call(Hash.SET_DRIVER_AGGRESSIVENESS,driver,1f);
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES,driver,2,false);
                _rivalCars.Add(car);_rivals.Add(driver);var blip=Track(car.AddBlip());blip.Color=BlipColor.Red;blip.Name="Sprint rival "+(i+1);
            }
            kjModel.MarkAsNoLongerNeeded();rivalModel.MarkAsNoLongerNeeded();carModel.MarkAsNoLongerNeeded();
        }

        /// <summary>
        /// The performance parts, so a rival in the player's model is not a stock one against
        /// his built one. Cosmetics are left alone; this is the engine, the box, the brakes and
        /// the springs, which is what the capability reading reflects.
        /// </summary>
        private static void FitForRacing(Vehicle car)
        {
            try
            {
                car.Mods.InstallModKit();
                foreach (var type in new[] { VehicleModType.Engine, VehicleModType.Transmission, VehicleModType.Brakes, VehicleModType.Suspension })
                {
                    int count = car.Mods[type].Count;
                    if (count > 0) car.Mods[type].Index = count - 1;
                }
                car.IsEngineRunning = true;
            }
            catch (Exception ex) { Logger.Warn("A rival car could not be fitted for racing: " + ex.Message); }
        }

        /// <summary>What this rival can actually do, in meters per second, with its parts on.</summary>
        private static float Capability(Vehicle car)
        {
            float top = Function.Call<float>(Hash.GET_VEHICLE_ESTIMATED_MAX_SPEED, car);
            return top > 1f ? top : Function.Call<float>(Hash.GET_VEHICLE_MODEL_ESTIMATED_MAX_SPEED, car.Model.Hash);
        }

        /// <summary>
        /// Send a rival at his next gate. Re-tasking restarts the drive task, which costs him a
        /// beat every time, so this is called on a changed gate or a genuine stall and the pace
        /// is trimmed through the cruise speed in between. That is the same rule
        /// <c>PreparationOperation.KeepDriving</c> keeps for crew drivers, for the same reason.
        /// </summary>
        private void Order(int i,float metersAhead)
        {
            var target=_route.Gates[_next[i]];
            float speed=RacePacing.Speed(Capability(_rivalCars[i]),target.Z>TrailHeight,metersAhead);
            _commanded[i]=speed;
            _rivals[i].Task.DriveTo(_rivalCars[i],target,6f,speed,DrivingStyle.Rushed);
            _reviewed[i]=Game.GameTime;_last[i]=_rivalCars[i].Position;
        }

        /// <summary>Trim the pace without restarting the drive task.</summary>
        private void Pace(int i,float metersAhead)
        {
            var target=_route.Gates[_next[i]];
            float speed=RacePacing.Speed(Capability(_rivalCars[i]),target.Z>TrailHeight,metersAhead);
            if(Math.Abs(speed-_commanded[i])<1f)return;
            _commanded[i]=speed;
            Function.Call(Hash.SET_DRIVE_TASK_CRUISE_SPEED,_rivals[i],speed);
            if(RacePacing.IsCorrecting(metersAhead))
                Logger.Debug("Sprint rival "+(i+1)+" is "+(int)metersAhead+" m on the player; commanded "+(int)speed+" m/s.");
        }

        protected override void OnUpdate()
        {
            // Check opponents before advancing the player's last gate: no false win
            // when a rival was already across the same finish on this frame.
            if(Stage==1&&!Ctx.Cutscenes.IsActive)
            {
                var player=Game.Player.Character;
                var race=CurrentStageObjectives.OfType<RaceCheckpointObjective>().FirstOrDefault();
                float playerLeft=race==null||player==null||!player.Exists()?0f
                    :RaceRoute.Remaining(_route.Gates,race.NextCheckpoint,player.Position);
                for(int i=0;i<_rivals.Count;i++)
                {
                    var driver=_rivals[i];var car=_rivalCars[i];
                    if(driver==null||!driver.Exists()||driver.IsDead||car==null||!car.Exists()||!car.IsDriveable)continue;
                    float ahead=playerLeft-RaceRoute.Remaining(_route.Gates,_next[i],car.Position);
                    if(car.Position.DistanceTo(_route.Gates[_next[i]])<GateRadius)
                    {
                        if(++_next[i]>=_route.Gates.Count){Fail("A rival reached the mountain finish first. Keep your car and retry KJ's sprint.");return;}
                        Order(i,ahead);
                        continue;
                    }
                    if(Game.GameTime-_reviewed[i]<ReviewMs)continue;
                    _reviewed[i]=Game.GameTime;
                    // Moving at all resets the stall clock; only a car that is genuinely stuck
                    // is re-tasked, because re-tasking is what makes a driver hesitate.
                    if(car.Position.DistanceTo(_last[i])>=StallMeters||car.Speed>=2f){_last[i]=car.Position;_movedAt[i]=Game.GameTime;Pace(i,ahead);continue;}
                    if(Game.GameTime-_movedAt[i]>StallMs){_movedAt[i]=Game.GameTime;Order(i,ahead);}
                }
            }
            base.OnUpdate();
        }
        public override SceneBlocking OutroBlocking()=>_coupe==null||!_coupe.Exists()?null:new SceneBlocking().Then(new ShotStep(3200,_coupe,new Vector3(-5f,3f,2f),_coupe,Vector3.Zero,.5f));
        protected override void OnCleanup(){_rivals.Clear();_rivalCars.Clear();Ctx.RaceVehicle=null;}
    }
}
