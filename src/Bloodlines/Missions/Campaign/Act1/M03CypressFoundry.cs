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
    /// M03 — "Cypress Foundry". Cypress Flats and the Murrieta oil fields, 14:00.
    ///
    /// The crew's first joint operation as a crew, from the base they are about to
    /// make defensible. Ice and Gohan take the van to the depot and wait on Ron's
    /// call; Ron drives to the Davis rail junction alone, locks it, and finds out
    /// that somebody paid the block to watch it: a street crew out of the houses,
    /// dogs already loose. Ice's walk to the entry marker is what wakes the depot;
    /// until then the guards patrol and Ice keeps quiet. Ice clears the yard, Gohan
    /// loads three real crates into the Benson where it is parked, Ron collects the
    /// truck and brings it home with the police to lose on the way. At the foundry
    /// he gets out and the Benson locks: the base has its truck.
    ///
    /// Two substitutions, both logged in docs/FEASIBILITY.md: uncoupling a forty-car
    /// freight train is not exposed to script, so the blockade is a hold at the
    /// junction; and the gantry crane is loading by hand rather than a driveable
    /// crane. Both are the same beat from the player's side — be here, do the work,
    /// take the consequences — and neither needs a system the engine lacks.
    /// </summary>
    public sealed class M03CypressFoundry : ComposedMission
    {
        private static readonly string[] DepotGuards = { "g_m_y_mexgoon_01", "g_m_y_mexgang_01", "g_m_m_armboss_01" };
        private static readonly string[] StreetCrew = { "g_m_y_ballasout_01", "g_m_y_ballaeast_01", "g_m_y_ballaorig_01" };
        private static readonly Vector3[] BedSlots = { new Vector3(-0.55f, -2.4f, 0.55f), new Vector3(0.55f, -2.4f, 0.55f), new Vector3(0f, -1.5f, 0.55f) };

        /// <summary>How long after Ron starts the work the block comes out of the houses.</summary>
        public const int AmbushDelayMs = 5000;
        public const int RailHoldSeconds = 6;

        private readonly List<Ped> _guards = new List<Ped>();
        private readonly List<Ped> _ambush = new List<Ped>();
        private readonly List<Ped> _dogs = new List<Ped>();
        private readonly HashSet<int> _loosed = new HashSet<int>();
        private readonly List<Prop> _crates = new List<Prop>();

        private Vehicle _hauler, _van, _car;
        private RoleTracks _roles;
        private Vector3 _junction, _depot, _base, _entry, _iceWatch, _iceCover, _gohanWait, _gohanCover, _depotStage, _pallet;
        private bool _split, _entryFired, _ambushSprung;
        private int _workStartedAt;

        public override string Id => "M03";
        public override string Title => "Cypress Foundry";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SafehouseArrival;

        public Vehicle Hauler => _hauler;
        public Vehicle Van => _van;
        public Vehicle Car => _car;
        public RoleTracks Roles => _roles;
        public IReadOnlyList<Ped> Dogs => _dogs;
        public IReadOnlyList<Ped> Ambush => _ambush;
        public IReadOnlyList<Prop> Crates => _crates;
        public bool EntryFired => _entryFired;
        public bool AmbushSprung => _ambushSprung;

        protected override bool Setup()
        {
            if (!MissionSites.Ground(Ctx.Locations, "M03.RailJunction", "M03.DepotGate", "M03.CraneControls", "M03.HaulerSpawn", "Base.CypressFlats")) return false;
            _junction = Ctx.Locations.Position("M03.RailJunction");
            _depot = Ctx.Locations.Position("M03.DepotGate");
            _base = Ctx.Locations.Position("Base.CypressFlats");
            float baseHeading = Ctx.Locations.Heading("Base.CypressFlats");
            // Where the depot team stands, derived from the surveyed keys so a survey moves the set:
            // Ice's entry marker sits before the first guard; his watch point and Gohan's wait are outside the yard.
            _entry = _depot + new Vector3(0f, -22f, 0f);
            _iceWatch = _depot + new Vector3(-6f, -42f, 0f);
            _iceCover = _iceWatch + new Vector3(-6f, 0f, 0f);
            var haulerSpot = Ctx.Locations.Position("M03.HaulerSpawn");
            _gohanWait = haulerSpot + new Vector3(0f, -14f, 0f);
            _gohanCover = _gohanWait + new Vector3(7f, 0f, 0f);
            _depotStage = _iceWatch + new Vector3(10f, -8f, 0f);

            // Everyone starts at the base; the briefing played there.
            if (!Ctx.Crew.Deploy(CrewSlot.Guess, _base, baseHeading)) return false;
            ApplyBibleSetting();

            SpawnCar(_base + new Vector3(6f, 0f, 0f), baseHeading);
            SpawnVan(_base + new Vector3(-6f, 0f, 0f), baseHeading);
            SpawnDepotGuards();
            SpawnHauler();
            SpawnDogs();
            if (!RequireAssets(_car, _van, _hauler) || _guards.Count != 8) return false;
            _pallet = _hauler.Position - _hauler.ForwardVector * 5f + new Vector3(3f, 0f, 0f);
            SpawnCrates();

            Preserve(_van);
            Preserve(_hauler);
            Preserve(_car);
            foreach (var hero in Protagonist.All) Ctx.Crew.CompanionAI.TakeControl(hero.Slot);
            _roles = new RoleTracks(Ctx.Crew, Hostiles);
            PlaySplit();
            return true;
        }

        private IEnumerable<Ped> Hostiles()
        {
            foreach (var dog in _dogs) yield return dog;
            foreach (var thug in _ambush) yield return thug;
            if (!_entryFired) yield break;
            foreach (var guard in _guards) yield return guard;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            // Ron alone on the road: the rest of the crew is across the city.
            yield return new MissionStage("Drive to the junction",
                    new TravelObjective("Guess: drive to the Davis rail junction.", () => _junction, 14f, () => _car)
                        .Cue(0.5f, () => Radio("ICE", "We're set outside the depot. Gohan's on the truck, I've got the gate. Say when the rail is dead.", "M03_RADIO_01_ICE")))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context => context.Crew.CompanionsHoldPosition = true);

            // Out of the car and onto the mark: the work starts when he is there, no button.
            yield return new MissionStage("Seal the response routes",
                    new HoldZoneObjective("Guess: get out and hold the junction marker.", () => _junction, RailHoldSeconds, 4f, "Locking the junction", onFoot: true),
                    new ReactionTrigger(() => _workStartedAt > 0 && Game.GameTime - _workStartedAt >= AmbushDelayMs, SpringAmbush))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context => Say("M03_S1_01_GUESS"));

            // Ice's walk is the trigger. The guards patrol until it fires; a shot before it wakes the whole yard.
            yield return new MissionStage("Breach the depot",
                    new ReachZoneObjective("Ice: walk to the yellow entry marker at the depot gate.", () => _entry, 6f, flat: true),
                    new QuietRuleObjective("Quiet until the entry marker.", "The depot heard Ice's shot and the whole yard came down on him.",
                        () => Ctx.Crew.ActiveSlot == CrewSlot.Ice && Game.Player.Character.Position.DistanceTo(_depot) < 140f))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(context => Say("M03_S1_02_ICE"))
                .OnExit(context => FireEntry());

            yield return new MissionStage("Clear the yard",
                    new KillTargetsObjective("Ice: eliminate the guards marked RED in the container yard. Gohan waits until it is clear.", () => _guards))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(context => Say("M03_S2_03_ICE"));

            // Loading is seen: Gohan opens the Benson and carries the crates into it.
            yield return new MissionStage("Load the Benson",
                    new MissionInteraction("Gohan: open the Benson and load the crates", () => RearOfHauler(), 1, 3.5f))
                .OwnedBy(CrewSlot.Gohan)
                .OnEnter(context => _roles.For(CrewSlot.Gohan).Work(_gohanWait, _gohanCover))
                .OnExit(context => PlayLoading());

            yield return new MissionStage("Run it home",
                    new EnterVehicleObjective("Guess: travel to the depot and take the orange-marked Benson truck (driver seat).", () => _hauler, VehicleSeat.Driver),
                    new ProtectObjective("", () => _hauler, "The hauler was destroyed."),
                    new ReactionTrigger(() => !Ctx.Cutscenes.IsActive, () => { CloseDoors(); Say("M03_S2_04_GOHAN"); }))
                .OwnedBy(CrewSlot.Guess);

            // The foundry is a safehouse arrival: the police are lost on the way, not at the gate.
            yield return new MissionStage("Cypress Flats",
                    new OccupiedVehicleDestination("Guess: deliver the Benson weapons truck to the yellow foundry marker.", () => _hauler, () => _base, 25f),
                    new LoseWantedObjective("Lose the police before the foundry."),
                    new ProtectObjective("", () => _hauler, "The hauler was destroyed."))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context => { Game.Player.WantedLevel = 2; Say("M03_S2_05_GUESS"); })
                .OnExit(context =>
                {
                    ClearHeatIfSafe();
                    // The delivered truck is the base's: it locks where it stands.
                    if (_hauler != null && _hauler.Exists()) _hauler.LockStatus = VehicleLockStatus.CannotEnter;
                    /* Awarded once by CampaignState.MarkComplete after the mission passes. */
                    GameUtils.Subtitle("~g~The foundry is operational.", 5000);
                });
        }

        // ---------- beats ----------

        /// <summary>The split, seen: Ice and Gohan board the van and pull away for the depot. The cut lands them there.</summary>
        private void PlaySplit()
        {
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            var blocking = new SceneBlocking()
                .Then(new EnterVehicleStep(ice, _van, VehicleSeat.Driver))
                .Then(new EnterVehicleStep(gohan, _van, VehicleSeat.RightFront))
                .Then(new ShotStep(3200, _van, new Vector3(-7f, 2.5f, 1.5f), _van, new Vector3(0f, 0f, 0.8f), 1.0f, PullAway));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "split", Title = "Two jobs",
                Reason = "Ice and Gohan take the van to the depot and wait on Ron's call; Ron takes the rail junction alone in his own car.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) { Logger.Warn("M03 split scene did not play; the depot team is staged directly."); blocking.Complete(); }
        }

        private void PullAway()
        {
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            if (ice == null || !ice.Exists() || _van == null || !_van.Exists() || !ice.IsInVehicle(_van)) return;
            _van.IsEngineRunning = true;
            ice.Task.DriveTo(_van, _depot, 20f, 18f, DrivingStyle.Normal);
        }

        /// <summary>Off camera, after the split: the van and both brothers at the depot, on their tracks.</summary>
        private void StageDepotTeam()
        {
            if (_split) return;
            _split = true;
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            if (_van != null && _van.Exists())
            {
                foreach (var ped in new[] { ice, gohan })
                    if (ped != null && ped.Exists() && ped.IsInVehicle(_van)) ExitVehicleStep.ForceOut(ped);
                Function.Call(Hash.REQUEST_COLLISION_AT_COORD, _depotStage.X, _depotStage.Y, _depotStage.Z);
                _van.Position = _depotStage;
                _van.Heading = DriveUpStep.HeadingBetween(_depotStage, _depot);
                _van.IsEngineRunning = false;
            }
            if (ice != null && ice.Exists()) { Function.Call(Hash.REQUEST_COLLISION_AT_COORD, _iceWatch.X, _iceWatch.Y, _iceWatch.Z); ice.Position = _iceWatch; }
            if (gohan != null && gohan.Exists()) { Function.Call(Hash.REQUEST_COLLISION_AT_COORD, _gohanWait.X, _gohanWait.Y, _gohanWait.Z); gohan.Position = _gohanWait; }
            _roles.For(CrewSlot.Ice).Observe(_iceWatch, _iceCover);
            _roles.For(CrewSlot.Gohan).Observe(_gohanWait, _gohanCover);
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            if (guess != null && guess.Exists() && _car != null && _car.Exists() && !guess.IsInVehicle(_car)) guess.SetIntoVehicle(_car, VehicleSeat.Driver);
            Objective("Drive to the Davis rail junction.");
        }

        /// <summary>The block was paid: a street crew out of the houses along the junction, on Ron.</summary>
        private void SpringAmbush()
        {
            if (_ambushSprung) return;
            _ambushSprung = true;
            var cartel = World.AddRelationshipGroup("BLOODLINES_CARTEL");
            var offsets = new[] { new Vector3(18f, 12f, 0f), new Vector3(-16f, 14f, 0f), new Vector3(20f, -10f, 0f), new Vector3(-18f, -12f, 0f) };
            for (int i = 0; i < offsets.Length; i++)
            {
                var model = new Model(StreetCrew[i % StreetCrew.Length]);
                if (!GameUtils.RequestModel(model)) continue;
                var point = World.GetSafeCoordForPed(_junction + offsets[i], true, 0);
                if (point == Vector3.Zero) point = _junction + offsets[i];
                var thug = World.CreatePed(model, point, DriveUpStep.HeadingBetween(point, _junction));
                model.MarkAsNoLongerNeeded();
                if (thug == null || !thug.Exists()) continue;
                thug.RelationshipGroup = cartel;
                thug.IsPersistent = true;
                thug.BlockPermanentEvents = true;
                thug.Accuracy = 30;
                thug.Weapons.Give(i % 2 == 0 ? WeaponHash.MicroSMG : WeaponHash.Pistol, 120, true, true);
                thug.Task.FightAgainstHatedTargets(90f);
                _ambush.Add(Track(thug));
            }
            Logger.Info("M03 ambush: " + _ambush.Count + " on the junction.");
            if (_ambush.Count > 0)
                Ctx.Cutscenes.PlayMoment(Id, "The block was paid", "GUESS", "Company. Somebody paid this block to watch the junction.", _ambush[0]);
        }

        /// <summary>Ice's walk wakes the depot: from here the guards are a fight.</summary>
        private void FireEntry()
        {
            if (_entryFired) return;
            _entryFired = true;
            var cartel = World.AddRelationshipGroup("BLOODLINES_CARTEL");
            foreach (var guard in _guards)
                if (guard != null && guard.Exists() && !guard.IsDead) { guard.RelationshipGroup = cartel; guard.Task.ClearAll(); guard.Task.FightAgainstHatedTargets(120f); }
        }

        private Vector3 RearOfHauler() => _hauler != null && _hauler.Exists() ? _hauler.Position - _hauler.ForwardVector * 4f : _base;

        private void OpenDoors()
        {
            if (_hauler == null || !_hauler.Exists()) return;
            Function.Call(Hash.SET_VEHICLE_DOOR_OPEN, _hauler, 2, false, false);
            Function.Call(Hash.SET_VEHICLE_DOOR_OPEN, _hauler, 3, false, false);
        }

        private void CloseDoors()
        {
            if (_hauler == null || !_hauler.Exists()) return;
            Function.Call(Hash.SET_VEHICLE_DOOR_SHUT, _hauler, 2, false);
            Function.Call(Hash.SET_VEHICLE_DOOR_SHUT, _hauler, 3, false);
        }

        /// <summary>
        /// Loading, seen: the doors open, Gohan carries each crate from the pallet
        /// into the bed, the doors close. Skipping finishes it in order, so the
        /// crates are in the truck either way; the stage ends on the sequence, not
        /// on a timer.
        /// </summary>
        private void PlayLoading()
        {
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            if (gohan == null || !gohan.Exists() || _hauler == null || !_hauler.Exists()) return;
            OpenDoors();
            var rear = RearOfHauler();
            var blocking = new SceneBlocking()
                .Then(new ShotStep(2600, _hauler, new Vector3(-6f, 3.5f, 1.6f), _hauler, new Vector3(0f, -2.5f, 1f), 0.6f));
            for (int i = 0; i < _crates.Count; i++)
            {
                var crate = _crates[i];
                blocking.Then(new WalkToStep(gohan, _pallet, 1.1f))
                    .Then(new CarryPropStep(gohan, crate, new Vector3(0.25f, 0.1f, 0f), new Vector3(0f, 0f, 0f)))
                    .Then(new WalkToStep(gohan, rear, 1.1f))
                    .Then(new StowPropStep(gohan, crate, _hauler, BedSlots[i % BedSlots.Length]));
            }
            blocking.Then(new ShotStep(2200, _hauler, new Vector3(-4.5f, -3.5f, 1.3f), _hauler, new Vector3(0f, -2.8f, 0.9f), 0.4f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "loading", Title = "Loading",
                Reason = "The depot's weapons go into the Benson by hand: three crates, seen going in, before Ron drives it home.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) { Logger.Warn("M03 loading scene did not play; the crates are placed directly."); blocking.Complete(); }
        }

        /// <summary>The dogs are loose already: any brother inside thirty meters is theirs.</summary>
        private void LooseDogs()
        {
            foreach (var dog in _dogs)
            {
                if (dog == null || !dog.Exists() || dog.IsDead || _loosed.Contains(dog.Handle)) continue;
                Ped nearest = null; float best = 30f;
                foreach (var hero in Protagonist.All)
                {
                    var ped = Ctx.Crew.PedFor(hero.Slot);
                    if (ped == null || !ped.Exists() || ped.IsDead) continue;
                    float distance = ped.Position.DistanceTo(dog.Position);
                    if (distance < best) { best = distance; nearest = ped; }
                }
                if (nearest == null) continue;
                _loosed.Add(dog.Handle);
                dog.Task.ClearAll();
                dog.Task.FightAgainst(nearest);
            }
        }

        protected override void OnUpdate()
        {
            if (!_split && !Ctx.Cutscenes.IsActive) StageDepotTeam();
            base.OnUpdate();
            _roles?.Update();
            LooseDogs();
            if (_workStartedAt == 0 && Stage == 1 && Ctx.Crew.ActiveSlot == CrewSlot.Guess)
            {
                var player = Game.Player.Character;
                if (player != null && player.Exists() && !player.IsInVehicle() && GameUtils.IsWithin(player.Position, _junction, 6f)) _workStartedAt = Game.GameTime;
            }
        }

        /// <summary>Home: Ron out of the truck, the truck locked and framed where it stands. The lines follow.</summary>
        public override SceneBlocking OutroBlocking()
        {
            var guess = Ctx?.Crew.PedFor(CrewSlot.Guess);
            if (guess == null || !guess.Exists() || _hauler == null || !_hauler.Exists()) return null;
            var blocking = new SceneBlocking { DialogueAfterStep = guess.IsInVehicle(_hauler) ? 1 : 0 };
            if (guess.IsInVehicle(_hauler)) blocking.Then(new ExitVehicleStep(guess));
            return blocking.Then(new ShotStep(4000, _hauler, new Vector3(-6f, 3f, 1.6f), _hauler, new Vector3(0f, 0f, 0.8f), 1.0f));
        }

        protected override void OnPassed()
        {
            if (_hauler != null && _hauler.Exists()) Release(_hauler);
            if (_van != null && _van.Exists()) Release(_van);
            if (_car != null && _car.Exists()) Release(_car);
        }

        protected override void OnCleanup()
        {
            _roles?.Release();
            Ctx.Crew.CompanionsHoldPosition = false;
            _guards.Clear();
            _ambush.Clear();
            _dogs.Clear();
            _crates.Clear();
        }

        // ---------- world building ----------

        private void SpawnCar(Vector3 position, float heading)
        {
            var model = new Model("primo");
            if (!GameUtils.RequestModel(model)) return;
            _car = Track(World.CreateVehicle(model, position, heading));
            model.MarkAsNoLongerNeeded();
            if (_car == null || !_car.Exists()) return;
            _car.IsPersistent = true;
            _car.IsEngineRunning = true;
            var blip = Track(_car.AddBlip());
            blip.Sprite = BlipSprite.PersonalVehicleCar;
            blip.Color = BlipColor.Orange;
            blip.Name = "Ron's car";
        }

        private void SpawnVan(Vector3 position, float heading)
        {
            _van = Ctx.Vans != null ? Ctx.Vans.Spawn(position, heading) : null;
            if (_van == null)
            {
                var model = new Model("granger");
                if (!GameUtils.RequestModel(model)) return;
                _van = World.CreateVehicle(model, position, heading);
                model.MarkAsNoLongerNeeded();
            }
            if (_van == null || !_van.Exists()) return;
            Track(_van);
            _van.IsPersistent = true;
            var blip = Track(_van.AddBlip());
            blip.Sprite = BlipSprite.PersonalVehicleCar;
            blip.Color = BlipColor.Blue;
            blip.Name = "Crew van";
        }

        private void SpawnDepotGuards()
        {
            // A patrol until Ice's entry fires: neutral to the crew, guarding their posts.
            var quiet = World.AddRelationshipGroup("BLOODLINES_TRAFFIC");

            for (int i = 0; i < 8; i++)
            {
                var model = new Model(DepotGuards[i % DepotGuards.Length]);
                if (!GameUtils.RequestModel(model)) continue;

                var offset = new Vector3(-12f + i * 4f, 6f + (i % 3) * 9f, 0f);
                var guard = World.CreatePed(model, _depot + offset, 180f);
                model.MarkAsNoLongerNeeded();
                if (guard == null || !guard.Exists()) continue;

                guard.RelationshipGroup = quiet;
                guard.IsPersistent = true;
                guard.BlockPermanentEvents = true;
                guard.Accuracy = 35;
                guard.Armor = 25;
                guard.Weapons.Give(i % 3 == 0 ? WeaponHash.AssaultRifle : WeaponHash.MicroSMG, 200, true, true);
                guard.Task.GuardCurrentPosition();

                _guards.Add(Track(guard));
            }
        }

        private void SpawnDogs()
        {
            // Three, in the block, before Ron gets there: theirs, not the depot's.
            var cartel = World.AddRelationshipGroup("BLOODLINES_CARTEL");
            var model = new Model("a_c_rottweiler");
            if (!GameUtils.RequestModel(model)) return;
            var spots = new[] { new Vector3(12f, 8f, 0f), new Vector3(-14f, 6f, 0f), new Vector3(6f, -16f, 0f) };
            foreach (var offset in spots)
            {
                var point = World.GetSafeCoordForPed(_junction + offset, true, 0);
                if (point == Vector3.Zero) point = _junction + offset;
                var dog = World.CreatePed(model, point, 0f);
                if (dog == null || !dog.Exists()) continue;
                dog.RelationshipGroup = cartel;
                dog.IsPersistent = true;
                dog.BlockPermanentEvents = true;
                dog.Task.WanderAround(point, 8f);
                _dogs.Add(Track(dog));
            }
            model.MarkAsNoLongerNeeded();
            Logger.Info("M03 dogs on the junction block: " + _dogs.Count + ".");
        }

        private void SpawnHauler()
        {
            // A box truck rather than a tractor and trailer: script-attached trailers
            // come apart under fire and take the mission with them.
            var model = new Model("benson");
            if (!GameUtils.RequestModel(model)) return;

            _hauler = Track(World.CreateVehicle(model, Ctx.Locations.Position("M03.HaulerSpawn"),
                Ctx.Locations.Heading("M03.HaulerSpawn")));
            model.MarkAsNoLongerNeeded();
            if (_hauler == null || !_hauler.Exists()) return;

            _hauler.IsPersistent = true;
            _hauler.IsEngineRunning = false;

            var blip = Track(_hauler.AddBlip());
            blip.Sprite = BlipSprite.ArmoredTruck;
            blip.Color = BlipColor.Orange;
            blip.Name = "Weapons hauler";
        }

        private void SpawnCrates()
        {
            // Real crates on a pallet beside the truck, the ones Gohan carries in.
            var model = new Model("prop_box_wood02a");
            if (!GameUtils.RequestModel(model)) return;
            for (int i = 0; i < 3; i++)
            {
                var crate = World.CreateProp(model, _pallet + new Vector3(0f, 0f, 0.4f * i + 0.2f), false, false);
                if (crate == null || !crate.Exists()) continue;
                _crates.Add(Track(crate));
            }
            model.MarkAsNoLongerNeeded();
        }
    }
}
