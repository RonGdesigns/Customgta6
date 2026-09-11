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
    /// M10 — "Open Throttle". From the connector stash to Burro Heights, 23:00, rain.
    ///
    /// The turbine engines have to reach the shop, and the whole of Act I's escalation
    /// is riding on the flatbed. Guess keeps the cargo moving, Ice covers from the
    /// passenger seat and steps out with the launcher when there is somewhere to
    /// step out to, Gohan calls the route from the shop.
    ///
    /// Seen, not told: the same two crates M08 left at the stash, checked and
    /// chained before the run; the gunship shown coming, and the tunnel mouth
    /// named as the firing window before Ice is asked to fight from it; the
    /// engines shut down and looked over at the shop, which is where M11 finds them.
    ///
    /// The purest set piece in the campaign — no interiors, no faked physics, just a
    /// road, a speed floor and three characters with different jobs on the same
    /// vehicle. The truck never freezes in traffic to make a fight possible: the
    /// window is a place.
    /// </summary>
    public sealed class M10OpenThrottle : ComposedMission
    {
        private static readonly Vector3[] BedSlots = { new Vector3(0f, -0.9f, 1.05f), new Vector3(0f, -3.1f, 1.05f) };
        private readonly List<Ped> _bikers = new List<Ped>();
        private readonly List<Vehicle> _bikes = new List<Vehicle>();
        private readonly List<Prop> _crates = new List<Prop>();

        private Vehicle _flatbed;
        private Vehicle _buzzard;
        private Ped _pilot;
        private Vector3 _stash;
        private Vector3 _window;
        private Vector3 _shop;
        private bool _gunshipShown, _delivered;

        public override string Id => "M10";
        public override string Title => "Open Throttle";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SafehouseArrival;

        public Vehicle Flatbed => _flatbed;
        public Vehicle Buzzard => _buzzard;
        public IReadOnlyList<Prop> Crates => _crates;
        public bool GunshipShown => _gunshipShown;
        public bool Delivered => _delivered;

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            if (!MissionSites.Ground(Ctx.Locations, "M08.Connector", "M11.ChopShop")) return false;
            _stash = Ctx.Locations.Position("M08.Connector");
            _window = Ctx.Locations.Position("M10.TunnelMouth");
            _shop = Ctx.Locations.Position("M11.ChopShop");

            if (!Ctx.Crew.Deploy(CrewSlot.Guess, _stash + new Vector3(6f, -8f, 0f), Ctx.Locations.Heading("M08.Connector")))
            {
                return false;
            }

            ApplyBibleSetting();

            var player = Game.Player.Character;
            player.Weapons.Give(WeaponHash.RPG, 10, false, true);

            if (!SpawnFlatbed()) return false;
            SpawnCrates();
            if (_crates.Count != 2) return false;
            Station(CrewSlot.Ice, _stash + new Vector3(-4f, -6f, 0f));
            Station(CrewSlot.Gohan, _shop + new Vector3(0f, 10f, 0f));
            Ctx.Crew.PedFor(CrewSlot.Ice).Weapons.Give(WeaponHash.RPG, 12, false, true);
            if (Ctx.State?.CargoAt("turbineEngines") != "M08.Connector")
                Logger.Warn("M10: the campaign does not record the engines at the stash; the crates are placed there anyway.");
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            // The load first: both crates, where M08 left them, chained and checked.
            yield return new MissionStage("Check the load",
                    new MissionInteraction("Guess: check the crates on the flatbed", () => BedPosition(), 3, 4f))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context => PlayInspect());

            yield return new MissionStage("Roll out",
                    new EnterVehicleObjective("Guess — take the flatbed. Ice rides beside you.", () => _flatbed, VehicleSeat.Driver),
                    new ProtectObjective("", () => _flatbed, "The flatbed and the engines are gone."))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(context => SeatIce());

            // The speed floor and the bikes run together: dropping below the floor is what
            // lets them box the flatbed in, which is exactly what the bible says.
            yield return new MissionStage("The run",
                    new SpeedFloorObjective("Keep the flatbed above 35 mph; use drive-by weapons on the bikes.", 35f,
                        "The chase boxed the flatbed in and popped the slicks.", graceSeconds: 20),
                    new KillTargetsObjective("Clear the cartel bikes.", () => _bikers, 0, false),
                    new ProtectObjective("", () => _flatbed, "The flatbed and the engines are gone."))
                .OnEnter(context => SpawnBikes())
                .WithCues("M10_S1_01_GUESS", "M10_S1_02_ICE", "M10_S1_03_GOHAN");

            // The gunship is shown coming; the tunnel mouth is named as the place to
            // fight it from. The truck goes there; it is not stopped in traffic.
            yield return new MissionStage("The firing window",
                    new DeliverVehicleObjective("Guess: get the flatbed to the tunnel mouth. That is Ice's firing window.", () => _flatbed, () => _window, 22f),
                    new ProtectObjective("", () => _flatbed, "The flatbed and the engines are gone."),
                    new ReactionTrigger(() => !_gunshipShown && !Ctx.Cutscenes.IsActive, ShowGunship))
                .OwnedBy(CrewSlot.Guess)
                .OnEnter(context => SpawnBuzzard());

            yield return new MissionStage("Gunship",
                    new DestroyVehicleObjective("Ice: out of the cab, launcher on the marked Buzzard from the tunnel mouth.", () => _buzzard),
                    new ProtectObjective("", () => _flatbed, "The flatbed and the engines are gone."))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(context => HoldAtWindow())
                .AfterCues("M10_S2_04_ICE");

            yield return new MissionStage("Burro Heights",
                    new DeliverVehicleObjective("Guess: get back in the flatbed and deliver the engines to the shop.", () => _flatbed, () => _shop, 25f) { RequiredCharacter = CrewSlot.Guess },
                    new LoseWantedObjective("Lose the police before the shop."),
                    new ProtectObjective("", () => _flatbed, "The flatbed and the engines are gone."))
                .OnExit(context => DeliverEngines())
                .WithCues("M10_S2_05_GUESS")
                .AfterCues("M10_S2_06_GOHAN");
        }

        // ---------- beats ----------

        /// <summary>The same two crates M08 left here, chained and checked; Ice's seat and Gohan's role said in the yard.</summary>
        private void PlayInspect()
        {
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            var blocking = new SceneBlocking();
            if (_flatbed != null && _flatbed.Exists())
                blocking.Then(new ShotStep(3000, _flatbed, new Vector3(-4.5f, -4f, 1.6f), _flatbed, new Vector3(0f, -2f, 1f), 0.5f));
            if (guess != null && guess.Exists() && _crates.Count > 0 && _crates[0].Exists())
                blocking.Then(ShotStep.OverShoulder(3400, guess, _crates[0], 0.3f));
            blocking.Then(ShotStep.Wide(2800, _stash, 12f, 7f, 5f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "inspect", Title = "The load",
                Reason = "Both turbine crates are on the flatbed where M08 left them, chained; Ice takes the passenger seat with the launcher, Gohan calls the route from the shop.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M10 inspect scene did not play; the load stands on its own.");
        }

        private void SeatIce()
        {
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            if (ice == null || !ice.Exists() || _flatbed == null || !_flatbed.Exists()) return;
            Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Ice);
            if (!ice.IsInVehicle(_flatbed)) ice.SetIntoVehicle(_flatbed, VehicleSeat.RightFront);
        }

        /// <summary>The gunship shown coming, once, and the window named over the radio.</summary>
        private void ShowGunship()
        {
            _gunshipShown = true;
            if (_pilot != null && _pilot.Exists() && _buzzard != null && _buzzard.Exists())
                Ctx.Cutscenes.PlayMoment(Id, "Gunship", "GOHAN", "Aegis put a Buzzard up behind you. The tunnel mouth ahead is cover: get the truck there and Ice has a window.", _pilot);
            else Radio("GOHAN", "Aegis put a Buzzard up behind you. The tunnel mouth ahead is cover: get the truck there and Ice has a window.", "M10_RADIO_01_GOHAN");
        }

        /// <summary>At the window the truck is stopped by Ron's own AI, not by the mission freezing it; Ice steps out.</summary>
        private void HoldAtWindow()
        {
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            if (guess != null && guess.Exists() && guess.Handle != Game.Player.Character.Handle && _flatbed != null && _flatbed.Exists())
            {
                Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Guess);
                if (!guess.IsInVehicle(_flatbed)) guess.SetIntoVehicle(_flatbed, VehicleSeat.Driver);
                Function.Call(Hash.TASK_VEHICLE_TEMP_ACTION, guess, _flatbed, 27, 4000);
            }
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            if (ice != null && ice.Exists() && ice.IsInVehicle(_flatbed) && ice.Handle == Game.Player.Character.Handle)
                GameUtils.Subtitle("~y~Tunnel mouth. Get out and take the Buzzard with the launcher.", 4000);
        }

        /// <summary>The engines at the shop: recorded there, the truck shut down, the load looked over.</summary>
        private void DeliverEngines()
        {
            _delivered = true;
            Ctx.State?.SetCargo("turbineEngines", "M11.ChopShop");
            if (_flatbed != null && _flatbed.Exists())
            {
                _flatbed.IsEngineRunning = false;
                Release(_flatbed);
            }
            foreach (var crate in _crates) if (crate != null && crate.Exists()) Release(crate);
            ClearHeatIfSafe();
            GameUtils.Subtitle("~g~Engines at the shop. Both crates, shut down and checked. Burro Heights, tomorrow.", 6000);
        }

        /// <summary>The aftermath: Ron out of the cab, the crates on the bed at the shop.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_flatbed == null || !_flatbed.Exists()) return null;
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            var blocking = new SceneBlocking();
            if (guess != null && guess.Exists() && guess.IsInVehicle(_flatbed)) blocking.Then(new ExitVehicleStep(guess));
            return blocking.Then(new ShotStep(4500, _flatbed, new Vector3(-5f, -5f, 1.8f), _flatbed, new Vector3(0f, -2f, 1f), 0.8f));
        }

        // ---------- world building ----------

        private Vector3 BedPosition() => _flatbed != null && _flatbed.Exists() ? _flatbed.Position - _flatbed.ForwardVector * 5f : _stash;

        private bool SpawnFlatbed()
        {
            var model = new Model("flatbed");
            if (!GameUtils.RequestModel(model)) return false;

            _flatbed = Track(World.CreateVehicle(model, _stash, Ctx.Locations.Heading("M08.Connector")));
            model.MarkAsNoLongerNeeded();
            if (_flatbed == null || !_flatbed.Exists()) return false;

            _flatbed.IsPersistent = true;
            _flatbed.IsEngineRunning = false;
            // Preserve the flatbed's engine force; top-speed tuning is shared.
            _flatbed.CanTiresBurst = false;

            var blip = Track(_flatbed.AddBlip());
            blip.Sprite = BlipSprite.ArmoredTruck;
            blip.Color = BlipColor.Orange;
            blip.Name = "Engine transport";
            return true;
        }

        /// <summary>The same shipment: two crates on the bed, in M08's slots.</summary>
        private void SpawnCrates()
        {
            var model = new Model("prop_mil_crate_01");
            if (!GameUtils.RequestModel(model) || _flatbed == null || !_flatbed.Exists()) return;
            for (int i = 0; i < BedSlots.Length; i++)
            {
                var crate = Track(World.CreateProp(model, _flatbed.Position + new Vector3(0f, 0f, 2f), false, false));
                if (crate == null || !crate.Exists()) continue;
                crate.IsPersistent = true;
                StowPropStep.Stow(crate, _flatbed, BedSlots[i]);
                _crates.Add(crate);
            }
            model.MarkAsNoLongerNeeded();
        }

        private void SpawnBikes()
        {
            var bikeModel = new Model("sanchez");
            var riderModel = new Model("g_m_y_mexgoon_03");
            if (!GameUtils.RequestModel(bikeModel) || !GameUtils.RequestModel(riderModel)) return;

            var cartel = World.AddRelationshipGroup("BLOODLINES_CARTEL");
            var player = Game.Player.Character;

            for (int i = 0; i < 3; i++)
            {
                var behind = player.Position - player.ForwardVector * (40f + i * 12f)
                             + new Vector3(i * 4f - 4f, 0f, 0f);

                var bike = Track(World.CreateVehicle(bikeModel, behind, player.Heading));
                if (bike == null || !bike.Exists()) continue;
                bike.IsPersistent = true;
                _bikes.Add(bike);

                var rider = Track(World.CreatePed(riderModel, behind, player.Heading));
                if (rider == null || !rider.Exists()) continue;

                rider.RelationshipGroup = cartel;
                rider.IsPersistent = true;
                rider.BlockPermanentEvents = true;
                rider.Accuracy = 20;
                rider.Weapons.Give(WeaponHash.MicroSMG, 200, true, true);
                rider.Task.WarpIntoVehicle(bike, VehicleSeat.Driver);
                rider.Task.VehicleChase(player);
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, rider, 52, true);

                _bikers.Add(rider);
            }

            bikeModel.MarkAsNoLongerNeeded();
            riderModel.MarkAsNoLongerNeeded();
        }

        private void SpawnBuzzard()
        {
            var model = new Model("buzzard");
            var pilotModel = new Model("s_m_y_blackops_01");
            if (!GameUtils.RequestModel(model) || !GameUtils.RequestModel(pilotModel)) return;

            var player = Game.Player.Character;
            // Behind and high: seen coming, not already overhead.
            _buzzard = Track(World.CreateVehicle(model, player.Position - player.ForwardVector * 160f
                                                       + new Vector3(0f, 0f, 45f), player.Heading));
            if (_buzzard == null || !_buzzard.Exists()) return;
            _buzzard.IsPersistent = true;

            _pilot = Track(World.CreatePed(pilotModel, _buzzard.Position, 0f));
            model.MarkAsNoLongerNeeded();
            pilotModel.MarkAsNoLongerNeeded();
            if (_pilot == null || !_pilot.Exists()) return;

            _pilot.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            _pilot.IsPersistent = true;
            _pilot.Task.WarpIntoVehicle(_buzzard, VehicleSeat.Driver);
            _pilot.Task.ChaseWithHelicopter(player, new Vector3(0f, 0f, 30f));

            var blip = Track(_buzzard.AddBlip());
            blip.Sprite = BlipSprite.Helicopter;
            blip.Color = BlipColor.Red;
            blip.Name = "Aegis Buzzard";
        }

        protected override void OnCleanup()
        {
            _bikers.Clear();
            _bikes.Clear();
            _crates.Clear();
        }
    }
}
