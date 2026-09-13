using System.Collections.Generic;
using System.Linq;
using GTA.Native;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>M23 secures the Grand Senora bunker, verifies its loaded interior,
    /// and establishes the shortages that drive the next desert jobs.</summary>
    public sealed class M23GhostInTheSage : ComposedMission
    {
        private readonly List<Ped> _squatters = new List<Ped>();
        private readonly List<Vector3> _bays = new List<Vector3>();
        private readonly List<string> _limits = new List<string>();

        private Vehicle _granger;
        private Prop _generatorProp;
        private ApartmentAccess _interior;
        public ApartmentAccess Interior => _interior;
        private readonly List<Prop> _siteProps = new List<Prop>();
        public Prop GeneratorProp => _generatorProp;
        private RoleTracks _roles;
        private Vector3 _approach;
        private Vector3 _door;
        private Vector3 _secondExit;
        private Vector3 _generator;
        private bool _powered, _limitsShown;
        private bool _engaged;
        private int _nextCombat;

        public override string Id => "M23";
        public override string Title => "Ghost in the Sage";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SafehouseArrival;

        public Vehicle Granger => _granger;
        public RoleTracks Roles => _roles;
        public IReadOnlyList<string> Limits => _limits;
        public bool Powered => _powered;
        public bool LimitsShown => _limitsShown;

        protected override bool Setup()
        {
            BunkerSite.LoadMaps();
            _interior = Ctx.Interior ?? new ApartmentAccess(Ctx.Crew);
            if (!MissionSites.Prepare(Ctx.Locations, Id, BunkerSite.EntranceKey)) return false;
            _approach = Ctx.Locations.Position("M23.Approach");
            _door = Ctx.Locations.Position("M23.Entrance");
            _secondExit = Ctx.Locations.Position("M23.EscapeRoad");
            _generator = Ctx.Locations.Position("M23.PowerPanel");
            _bays.Add(Ctx.Locations.Position("M23.ToolBay"));
            _bays.Add(Ctx.Locations.Position("M23.FuelBay"));
            _bays.Add(Ctx.Locations.Position("M23.VehicleBay"));

            if (!Ctx.Crew.Deploy(CrewSlot.Guess, Ctx.Locations.Position("M23.RoadStart"), Ctx.Locations.Heading("M23.RoadStart")))
            {
                return false;
            }

            ApplyBibleSetting();
            if (!SpawnYard()) return false;
            SpawnSquatters();
            SpawnGranger();
            Ctx.Crew.CompanionsHoldPosition = true;
            if (_granger == null || !_granger.Exists() || _granger.PassengerCapacity < 2) return false;
            Ctx.Crew.PedFor(CrewSlot.Guess).SetIntoVehicle(_granger, VehicleSeat.Driver);
            Ctx.Crew.PedFor(CrewSlot.Ice).SetIntoVehicle(_granger, VehicleSeat.Passenger);
            Ctx.Crew.PedFor(CrewSlot.Gohan).SetIntoVehicle(_granger, VehicleSeat.LeftRear);
            _roles = new RoleTracks(Ctx.Crew, () => _squatters);
            _roles.For(CrewSlot.Ice);
            _roles.For(CrewSlot.Gohan);
            var arrival = new SceneSpec { MissionId = Id, Phase = "road", Title = "The road to shelter",
                Reason = "The crew arrives together by road; Guess must drive to the approach before Ice clears the entrance.",
                Blocking = new SceneBlocking().Then(new ShotStep(2200,_granger,new Vector3(-4f,-6f,2f),_granger,new Vector3(0f,0f,1f),.6f)) };
            Ctx.Cutscenes.Play(arrival);
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Drive to the approach",
                    new DeliverVehicleObjective("Guess: drive the crew to the marked bunker approach and stop. Ice takes point once you arrive.",
                        () => _granger, () => _approach, 10f),
                    new ConditionObjective("Stop the crew car with Ice and Gohan aboard.", () => _granger.Speed < 1.5f &&
                        Ctx.Crew.PedFor(CrewSlot.Ice).IsInVehicle(_granger) && Ctx.Crew.PedFor(CrewSlot.Gohan).IsInVehicle(_granger)))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context => { _granger.IsEngineRunning = false; PlayApproach(); });

            yield return new MissionStage("Approach the bunker",
                    new ReachZoneObjective("Ice: approach the marked bunker entrance. Guess covers the west side; Gohan watches the generator.", () => _door, 10f))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(context =>
                {
                    // The other two cover the approach; nobody teleports into the yard.
                    _roles.For(CrewSlot.Guess).TakeCover(Ctx.Locations.Position("M23.GuessStart"));
                    _roles.For(CrewSlot.Gohan).TakeCover(_bays[2]);
                })
                .WithCues("M23_S1_01_ICE");

            yield return new MissionStage("Clear the bunker yard",
                    new KillTargetsObjective("Clear the cartel squatters out.", () => _squatters))
                .OnEnter(context =>
                {
                    _engaged = true;
                    EngageSquatters();
                });

            // Each bay is looked into for what is there, which is mostly nothing: the
            // limits of the place are learned bay by bay, not announced.
            yield return new MissionStage("Secure the bays",
                    new MultiHoldObjective("Guess: inspect the marked workbench, empty fuel drum, and vehicle storage bay.", _bays, 5, 4f,
                        "Checking the bay") { SiteDone = BayChecked })
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context =>
                {
                    _roles.For(CrewSlot.Ice).Observe(_door, _door);
                    _roles.For(CrewSlot.Gohan).Approach(_generator + new Vector3(-4f, 0f, 0f), _generator);
                })
                .AfterCues("M23_S1_02_GUESS");

            yield return new MissionStage("Power up",
                    new MissionInteraction("Gohan: use the control side of the visible generator to restore bunker access.", () => _generator, 6, 2.5f, animation: MissionInteraction.ReachInside))
                .OwnedBy(CrewSlot.Gohan)
                .OnEnter(context =>
                {
                    _roles.For(CrewSlot.Ice).Observe(_door, _door);
                    _roles.For(CrewSlot.Guess).Observe(_bays[2], _bays[2]);
                })
                .OnExit(context => PlayPower());

            yield return new MissionStage("Enter the bunker",
                    new BunkerAccessObjective(_interior, true, _door))
                .OwnedBy(CrewSlot.Gohan)
                .OnEnter(context =>
                {
                    _roles.For(CrewSlot.Ice).Observe(_door, _door);
                    _roles.For(CrewSlot.Guess).Observe(_bays[2], _bays[2]);
                });

            yield return new MissionStage("Check the interior",
                    new MissionInteraction("Gohan: inspect the bunker entry room at the yellow marker. Stay here while checking the lights and shelter; the crew waits outside.",
                        () => Ctx.Locations.Position(BunkerSite.InspectKey), 4, 2f))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(context => Ctx.Dialogue.Play(new DialogueCue { CueId = "M23_INTERIOR_REPORT", MissionId = Id,
                    Speaker = "GOHAN", Line = "Interior's dry. Lights work. This gives us shelter, but we still need fuel, tools and money." }));

            yield return new MissionStage("Return outside",
                    new BunkerAccessObjective(_interior, false, Ctx.Locations.Position(BunkerSite.DoorKey)))
                .OwnedBy(CrewSlot.Gohan);

            yield return new MissionStage("Regroup at the entrance",
                    new ReachZoneObjective("Gohan: rejoin the crew at the bunker entrance.", () => _door, 4f))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(context => GameUtils.Subtitle("~g~Senora bunker secured. Return to its home marker between jobs to enter, rest and plan.", 6500));
        }

        // ---------- beats ----------

        /// <summary>The exterior survey: the Granger they came in, Ice reading the occupied approach, Ron looking for the second exit, Gohan on the utility problem. Nobody moves in yet.</summary>
        private void PlayApproach()
        {
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            var blocking = new SceneBlocking();
            if (_granger != null && _granger.Exists()) blocking.Then(new ShotStep(3000, _granger, new Vector3(-7f, 4f, 2f), _granger, new Vector3(0f, 0f, 0.8f), 0.8f));
            if (ice != null && ice.Exists()) blocking.Then(ShotStep.Watching(2800, ice, _squatters.Count > 0 && _squatters[0].Exists() ? (Entity)_squatters[0] : ice));
            if (guess != null && guess.Exists()) blocking.Then(ShotStep.Watching(2200, guess, guess));
            if (gohan != null && gohan.Exists()) blocking.Then(ShotStep.Watching(1800,gohan,gohan));
            blocking.Then(ShotStep.Wide(2800, _generator + new Vector3(0f, 0f, 1f), 22f, 9f, 8f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "The sage",
                Reason = "The Granger from the Alamo with what they have left, parked short of the bunker. Ice reads the occupied approach; Guess watches the withdrawal road from beside the car; Gohan identifies the visible trailer generator and the bunker access it powers. The bunker is a place to clear, not a home.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M23 approach scene did not play; the approach stands on its own.");
        }

        /// <summary>A bay looked into: what is there, said once, and what is missing, kept for the list.</summary>
        private void BayChecked(int site)
        {
            string found;
            switch (site)
            {
                case 0: found = "Bay one: a workbench, no tools on it."; _limits.Add("no tools"); break;
                case 1: found = "Bay two: one fuel drum, dry. The generator's tank is what there is."; _limits.Add("no fuel reserve"); break;
                default: found = "Bay three: room for the rigs, nothing in it."; break;
            }
            GameUtils.Subtitle("~y~" + found, 4000);
            Logger.Info("M23 " + found);
        }

        /// <summary>The generator started by hand, seen; then the limits of the place named as the reasons for the next jobs.</summary>
        private void PlayPower()
        {
            _powered = true;
            var blocking = new SceneBlocking();
            // The player already worked the generator controls during the objective.
            blocking.Then(new ShotStep(2200, _generatorProp, new Vector3(-5f,-4f,2f), _generatorProp, new Vector3(0f,0f,1f),0.6f));
            blocking.Then(ShotStep.Wide(1800,_door,12f,4f,5f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "power", Title = "The generator",
                Reason = "Gohan starts the generator by hand and the yard has power for as long as the tank lasts. The access controls respond. Gohan must enter and verify the underground room before anyone claims this base.",
                Blocking = blocking
            };
            var cue = Ctx.Data?.Cue("M23_S1_03_GOHAN");
            if (!Ctx.Cutscenes.PlayStaged(spec, new[] { cue })) { Logger.Warn("M23 power scene did not play; the line plays as dialogue."); blocking.Complete(); Say("M23_S1_03_GOHAN"); }
            ShowLimits();
        }

        /// <summary>The specific reasons for what comes next: power on a tank, no fuel reserve, no tools, no operating cash. Not a shopping list.</summary>
        private void ShowLimits()
        {
            _limitsShown = true;
            _limits.Add("four hours of power on the tank");
            _limits.Add("no operating cash");
            Ctx.State?.SetUpgrade("bunkerGenerator", true);
            GameUtils.Notify("~y~Bunker access powered. Check inside before moving in. Limits: " + string.Join("; ", _limits) + ". Cash first, from the Alamo; fuel and tools after.");
            Logger.Info("M23 limits: " + string.Join("; ", _limits));
        }

        /// <summary>The aftermath: the three at the door, the yard behind them, the lines over it.</summary>
        public override SceneBlocking OutroBlocking()
        {
            var blocking = new SceneBlocking();
            foreach (var slot in new[] { CrewSlot.Guess, CrewSlot.Gohan })
            {
                var ped = Ctx.Crew.PedFor(slot);
                if (ped != null && ped.Exists() && ped.Handle != Game.Player.Character.Handle) blocking.Then(new WalkToStep(ped, _door + new Vector3(slot == CrewSlot.Guess ? 2.5f : -2.5f, -2.5f, 0f), 1.5f));
            }
            return blocking.Then(ShotStep.Wide(4500, _door + new Vector3(0f, 0f, 1f), 12f, 4f, 5f));
        }

        protected override void OnUpdate()
        {
            if (_interior != null && _interior.Busy) { _interior.Update(); return; }
            if (!Ctx.Cutscenes.IsActive && CurrentStage <= 3 && Game.GameTime >= _nextCombat)
            {
                _nextCombat = Game.GameTime + 2000;
                if (_engaged || Game.Player.Character.IsShooting || _squatters.Any(p => p.Exists() && (p.IsDead || p.IsInCombat)))
                { _engaged = true; EngageSquatters(); }
            }
            base.OnUpdate();
            // Outside actors retain their assigned posts while Gohan is underground.
            if (_interior == null || !_interior.Inside) _roles?.Update();
        }

        private void EngageSquatters()
        {
            foreach (var guard in _squatters.Where(p => p != null && p.Exists() && p.IsAlive))
            {
                var target = Protagonist.All.Select(h => Ctx.Crew.PedFor(h.Slot))
                    .Where(p => p != null && p.Exists() && p.IsAlive)
                    .OrderBy(p => p.Position.DistanceTo(guard.Position)).FirstOrDefault();
                if (target == null || target.Position.DistanceTo(guard.Position) > 240f) continue;
                var current = Function.Call<Ped>(Hash.GET_PED_TARGET_FROM_COMBAT_PED, guard, 0);
                if (!guard.IsInCombat || current == null || !current.Exists() || !current.IsAlive || current.RelationshipGroup != Ctx.Crew.CrewGroup)
                    guard.Task.FightAgainst(target);
            }
        }

        // ---------- world building ----------

        private Prop PlaceYardProp(string name, Vector3 point, float heading=0f)
        {
            var model=new Model(name);
            if (!GameUtils.RequestModel(model)) return null;
            try
            {
                var prop=Track(World.CreateProp(model,point,true,true));
                if (prop==null||!prop.Exists()) return null;
                prop.Heading=heading;prop.IsPersistent=true;prop.IsPositionFrozen=true;
                _siteProps.Add(prop);return prop;
            }
            finally { model.MarkAsNoLongerNeeded(); }
        }
        private bool SpawnYard()
        {
            // Models are confirmed in the installed Enhanced archetype catalog.
            // Use the generator's control side as the objective, leaving room to stand.
            _generatorProp=PlaceYardProp("prop_generator_03b",_generator+new Vector3(2.5f,0f,0f),0f);
            var bench=PlaceYardProp("prop_tool_bench02",_bays[0]+new Vector3(0f,1.5f,0f),90f);
            var drum=PlaceYardProp("prop_barrel_02a",_bays[1]+new Vector3(0f,1.5f,0f));
            foreach(var bay in _bays)
            {
                PlaceYardProp("prop_barrier_work05",bay+new Vector3(-3f,0f,0f),90f);
                PlaceYardProp("prop_barrier_work05",bay+new Vector3(3f,0f,0f),90f);
            }
            if (!RequireAssets(_generatorProp,bench,drum)) return false;
            RequireAsset(_generatorProp,"The generator was destroyed. Restart the bunker mission.");
            return true;
        }

        private void SpawnSquatters()
        {
            var models = new[] { "g_m_y_mexgoon_01", "g_m_y_mexgoon_03", "g_m_y_mexgang_01" };
            var cartel = World.AddRelationshipGroup("BLOODLINES_CARTEL");
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS,5,cartel,Ctx.Crew.CrewGroup);
            Function.Call(Hash.SET_RELATIONSHIP_BETWEEN_GROUPS,5,Ctx.Crew.CrewGroup,cartel);

            for (int i = 0; i < 9; i++)
            {
                var model = new Model(models[i % models.Length]);
                if (!GameUtils.RequestModel(model)) continue;

                string key = "M23.Guard" + (i + 1).ToString("00");
                var post = MissionSites.Actor(Ctx.Locations, key, Ctx.Locations.Position(key));

                var squatter = World.CreatePed(model, post, 180f);
                model.MarkAsNoLongerNeeded();
                if (squatter == null || !squatter.Exists()) continue;

                squatter.RelationshipGroup = cartel;
                squatter.IsPersistent = true;
                squatter.BlockPermanentEvents = true;
                squatter.Accuracy = 32; squatter.Health = 220; squatter.Armor = 25;
                foreach (int attribute in new[] { 0, 4, 5, 13, 21, 46 })
                    Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES,squatter,attribute,true);
                Function.Call(Hash.SET_PED_COMBAT_MOVEMENT,squatter,i % 3 == 0 ? 2 : 1);
                Function.Call(Hash.SET_PED_COMBAT_ABILITY,squatter,2);
                Function.Call(Hash.SET_PED_COMBAT_RANGE,squatter,2);
                squatter.Weapons.Give(i % 3 == 0 ? WeaponHash.AssaultRifle : WeaponHash.MicroSMG, 200, true, true);
                squatter.Task.GuardCurrentPosition();

                _squatters.Add(Track(squatter));
            }
        }

        /// <summary>The Granger they came in from the Alamo, parked short of the bunker: the crew and what it has left.</summary>
        private void SpawnGranger()
        {
            var spot = Ctx.Locations.Position("M23.RoadStart");
            Vehicle granger = Ctx.Vans != null ? Ctx.Vans.Spawn(spot, Ctx.Locations.Heading("M23.RoadStart")) : null;
            if (granger == null)
            {
                var model = new Model("granger");
                if (!GameUtils.RequestModel(model)) return;
                granger = World.CreateVehicle(model, spot, Ctx.Locations.Heading("M23.RoadStart"));
                model.MarkAsNoLongerNeeded();
            }
            _granger = Track(granger);
            if (_granger == null || !_granger.Exists()) return;
            _granger.IsPersistent = true;
            _granger.IsEngineRunning = false;
        }

        protected override void OnPassed()
        {
            if (_granger != null && _granger.Exists()) Release(_granger);
            // Let the secured worksite remain in the loaded world after the camera
            // hands control back; a failed attempt still deletes its temporary props.
            foreach(var prop in _siteProps) if(prop!=null&&prop.Exists()) Release(prop);
        }

        protected override void OnCleanup()
        {
            _interior?.Cancel();
            _roles?.Release();
            Ctx.Crew.CompanionsHoldPosition = false;
            _squatters.Clear();
        }
    }
}
