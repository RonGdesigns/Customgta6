using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M09 — "Rolling Thunder". Route 68 past Harmony, 19:30, dusk.
    ///
    /// Guess flies the ridgelines while Ice takes the escort driver, and the crew
    /// lifts the military IFF transponder that gets them past one checkpoint in M16.
    ///
    /// Seen, not told: the convoy on the road, the escort that carries the unit,
    /// Ice's rock and the flat past the culvert where Ron lands, before anyone
    /// moves; the colonel's vehicles pulling away while the escort sits dead in
    /// the road, which is why nobody chases him; the unit itself out of the cab
    /// and in Ice's hand, read by Gohan for what it is and is not.
    ///
    /// The bible lands the helicopter's skids on a moving truck roof. Physics between
    /// two moving vehicles is where set pieces die, so the truck stops when its driver
    /// does and Ron lands on the flat beside the road. The unit is the point: the
    /// escort must stop, not burn.
    /// </summary>
    public sealed class M09RollingThunder : ComposedMission
    {
        private readonly List<Vehicle> _convoy = new List<Vehicle>();
        private readonly List<Ped> _crews = new List<Ped>();

        private Vehicle _frogger;
        private Ped _escortDriver;
        private Vehicle _escortTruck;
        private Prop _unit;
        private Vector3 _convoyStart;
        private Vector3 _ambush;
        private Vector3 _pickup;
        private Vector3 _bunker;
        private bool _withdrawn, _withdrawalShown, _unitRead;

        public override string Id => "M09";
        public override string Title => "Rolling Thunder";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;

        public Vehicle Frogger => _frogger;
        public Vehicle EscortTruck => _escortTruck;
        public Prop Unit => _unit;
        public bool Withdrawn => _withdrawn;
        public bool WithdrawalShown => _withdrawalShown;

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _convoyStart = Ctx.Locations.Position("M09.ConvoyStart");
            _ambush = Ctx.Locations.Position("M09.AmbushPoint");
            _pickup = Ctx.Locations.Position("M09.Pickup");
            _bunker = Ctx.Locations.Position("M09.Bunker");

            if (!Ctx.Crew.Deploy(CrewSlot.Guess, Ctx.Locations.Position("M09.HeliSpawn"),
                    Ctx.Locations.Heading("M09.HeliSpawn")))
            {
                return false;
            }

            ApplyBibleSetting();
            Game.Player.Character.Weapons.Give(WeaponHash.SniperRifle, 40, false, true);

            SpawnFrogger();
            SpawnConvoy();
            if (!RequireAssets(_frogger, _escortTruck, _escortDriver)) return false;
            Station(CrewSlot.Ice, _ambush + new Vector3(0f, -35f, 0f));
            Station(CrewSlot.Gohan, _bunker + new Vector3(8f, 0f, 0f));
            Ctx.Crew.PedFor(CrewSlot.Ice).Weapons.Give(WeaponHash.SniperRifle, 80, true, true);
            PlayApproach();
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Get airborne",
                    new EnterVehicleObjective("Guess — take the Frogger up.", () => _frogger,
                        VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess)
                .WithCues("M09_S1_01_ICE");

            // Shadowing, not chasing: too close and the convoy's anti-air sees them.
            yield return new MissionStage("Shadow the convoy",
                    new ShadowTargetObjective("Hold the ridgeline behind the convoy.",
                        () => _escortTruck, 220f, 25, "The convoy spotted the helicopter and scattered.",
                        60f, acquireSeconds: 180))
                .OnEnter(context => StartConvoy())
                .WithCues("M09_S1_02_GUESS");

            // The unit is in the escort's cab: the truck has to stop, not burn.
            yield return new MissionStage("Take the driver",
                    new KillTargetsObjective("Ice: wait at the ambush point and shoot the marked escort driver.",
                        () => new[] { _escortDriver }),
                    EscortIntact())
                .OwnedBy(CrewSlot.Ice)
                .OnExit(context => EscortStopped())
                .AfterCues("M09_S2_03_ICE");

            // The colonel's vehicles pull away while the escort sits dead: seen once,
            // so the player knows why nobody chases the more important passenger.
            yield return new MissionStage("Rip the transponder",
                    new MissionInteraction("Ice: get out, approach the stopped escort cab, and take its IFF transponder.", () => EscortPosition(), 6, 5f),
                    EscortIntact(),
                    new ReactionTrigger(() => _withdrawn && !_withdrawalShown && !Ctx.Cutscenes.IsActive, ShowWithdrawal))
                .OwnedBy(CrewSlot.Ice)
                .OnExit(context => PlayUnit())
                .WithCues("M09_S2_04_GUESS");

            // Ron lands on the flat past the culvert, not on the cab roof; Ice walks to it.
            yield return new MissionStage("The pickup",
                    new DeliverVehicleObjective("Guess: land the Frogger on the marked flat past the culvert.", () => _frogger, () => _pickup, 14f, land: true),
                    new ProtectObjective("", () => _frogger, "The Frogger is gone."),
                    new ReactionTrigger(() => !_unitRead && !Ctx.Cutscenes.IsActive, ReadUnit))
                .OwnedBy(CrewSlot.Guess);

            yield return new MissionStage("Ice aboard",
                    new EnterVehicleObjective("Ice — board the Frogger.", () => _frogger, VehicleSeat.Any),
                    new ProtectObjective("", () => _frogger, "The Frogger is gone."))
                .OwnedBy(CrewSlot.Ice);

            yield return new MissionStage("Transponder extraction",
                    new ReachZoneObjective("Get the transponder to the temporary drop point.",
                        () => _bunker, 30f, flat: true), // The permanent bunker unlock belongs to M23.
                    new ProtectObjective("", () => _frogger, "The Frogger is gone."))
                .OnExit(context =>
                {
                    // One gate, one code: the unit's use is limited and said so.
                    Ctx.State?.SetCargo("iffTransponder", "M09.Bunker");
                    GameUtils.Subtitle("~g~7-Echo-Victor: one checkpoint, once. The engines are still parked at the stash.", 6000);
                });
        }

        /// <summary>The unit is in the cab: a burned escort is a lost unit. A stopped one is the point.</summary>
        private Objective EscortIntact() =>
            new ReactionTrigger(() => _escortTruck == null || !_escortTruck.Exists() || _escortTruck.IsDead,
                () => Fail("The escort burned with the IFF unit in it."));

        // ---------- beats ----------

        /// <summary>The convoy, the escort, Ice's rock, Ron's flat: the whole job in three shots before anyone moves.</summary>
        private void PlayApproach()
        {
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var blocking = new SceneBlocking()
                .Then(_escortTruck != null && _escortTruck.Exists()
                    ? new ShotStep(3400, _escortTruck, new Vector3(-12f, 6f, 3f), _escortTruck, new Vector3(0f, 0f, 1f), 1.6f)
                    : new ShotStep(3400, null, _convoyStart + new Vector3(-12f, 6f, 3f), null, _convoyStart, 0f))
                .Then(ice != null && ice.Exists() ? ShotStep.Watching(3000, ice, ice) : (SceneStep)new ShotStep(3000, null, _ambush + new Vector3(4f, -4f, 2f), null, _ambush, 0f))
                .Then(ShotStep.Wide(3000, _pickup, 14f, 8f, 5f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "The convoy",
                Reason = "Three Insurgents on the road with the rear one marked; Ice on his rock above the culvert; the flat past it where Ron will land. The colonel is in the middle vehicle and is not the job.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M09 approach scene did not play; the ridge stands on its own.");
        }

        /// <summary>The escort stops; the colonel's vehicles do not. They pull away, and that is the reason nobody chases him.</summary>
        private void EscortStopped()
        {
            if (_escortTruck != null && _escortTruck.Exists())
            {
                _escortTruck.IsDriveable = false;
                _escortTruck.Speed = 0f;
            }
            for (int i = 0; i < _crews.Count && i < _convoy.Count; i++)
            {
                var truck = _convoy[i]; var driver = _crews[i];
                if (truck == null || !truck.Exists() || truck == _escortTruck || driver == null || !driver.Exists() || driver.IsDead) continue;
                driver.Task.DriveTo(truck, _convoyStart + new Vector3(-600f, 900f, 0f), 30f, 30f, DrivingStyle.Rushed);
                Function.Call(Hash.SET_PED_KEEP_TASK, driver, true);
            }
            _withdrawn = true;
        }

        private void ShowWithdrawal()
        {
            _withdrawalShown = true;
            Ped vip = null;
            for (int i = 0; i < _crews.Count && i < _convoy.Count; i++)
                if (_convoy[i] != _escortTruck && _crews[i] != null && _crews[i].Exists() && !_crews[i].IsDead) { vip = _crews[i]; break; }
            if (vip != null) Ctx.Cutscenes.PlayMoment(Id, "The colonel leaves", "GUESS", "There goes the colonel, and he can go. The unit is in the truck that stopped.", vip);
            else Radio("GUESS", "The colonel's gone up the road. Let him. The unit is in the truck that stopped.", "M09_RADIO_01_GUESS");
        }

        /// <summary>The unit out of the cab and in Ice's hand: Ice's own stage line as a scene, and Gohan reading it remotely for what it is.</summary>
        private void PlayUnit()
        {
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            SpawnUnit(ice);
            var blocking = new SceneBlocking();
            if (ice != null && ice.Exists())
            {
                // In his hand before the first shot: skipping changes nothing about who holds it.
                if (_unit != null && _unit.Exists()) CarryPropStep.Attach(ice, _unit, new Vector3(0.12f, 0.02f, -0.02f), new Vector3(0f, 90f, 0f));
                blocking.Then(ShotStep.OverShoulder(4200, ice, _unit != null && _unit.Exists() ? (Entity)_unit : ice, 0.2f));
            }
            if (_escortTruck != null && _escortTruck.Exists()) blocking.Then(new ShotStep(3000, _escortTruck, new Vector3(-8f, -5f, 2.2f), _escortTruck, new Vector3(0f, 0f, 1f), 0.8f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "unit", Title = "The unit",
                Reason = "The IFF unit is out of the escort's cab and in Ice's hand; the code is 7-Echo-Victor and it opens one checkpoint, once.",
                Blocking = blocking
            };
            var cue = Ctx.Data?.Cue("M09_S2_05_ICE");
            if (!Ctx.Cutscenes.PlayStaged(spec, new[] { cue })) { Logger.Warn("M09 unit scene did not play; the line plays as dialogue."); blocking.Complete(); Say("M09_S2_05_ICE"); }
        }

        /// <summary>Gohan reads the unit remotely, once the scene has ended: what it opens and what it does not.</summary>
        private void ReadUnit()
        {
            _unitRead = true;
            Radio("GOHAN", "Reading it from here: 7-Echo-Victor is a checkpoint code, one gate, one use. It does not make anyone invisible.", "M09_RADIO_02_GOHAN");
        }

        /// <summary>The aftermath: the Frogger at the drop with the unit aboard.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_frogger == null || !_frogger.Exists()) return null;
            return new SceneBlocking().Then(new ShotStep(4500, _frogger, new Vector3(-9f, 4f, 2.5f), _frogger, new Vector3(0f, 0f, 1f), 1.4f));
        }

        // ---------- world building ----------

        private Vector3 EscortPosition()
        {
            return _escortTruck != null && _escortTruck.Exists()
                ? _escortTruck.Position
                : _ambush;
        }

        private void SpawnUnit(Ped ice)
        {
            var model = new Model("prop_box_ammo03a");
            if (!GameUtils.RequestModel(model)) return;
            var at = ice != null && ice.Exists() ? ice.Position + new Vector3(0f, 0f, 1f) : EscortPosition();
            _unit = Track(World.CreateProp(model, at, false, false));
            model.MarkAsNoLongerNeeded();
            if (_unit == null || !_unit.Exists()) { _unit = null; return; }
            _unit.IsPersistent = true;
        }

        private void SpawnFrogger()
        {
            var model = new Model("frogger");
            if (!GameUtils.RequestModel(model)) return;

            _frogger = Track(World.CreateVehicle(model, Ctx.Locations.Position("M09.HeliSpawn"),
                Ctx.Locations.Heading("M09.HeliSpawn")));
            model.MarkAsNoLongerNeeded();
            if (_frogger == null || !_frogger.Exists()) return;

            _frogger.IsPersistent = true;

            var blip = Track(_frogger.AddBlip());
            blip.Sprite = BlipSprite.Helicopter;
            blip.Color = BlipColor.Orange;
            blip.Name = "Frogger";
        }

        private void SpawnConvoy()
        {
            var truckModel = new Model("insurgent");
            var crewModel = new Model("s_m_y_marine_01");
            if (!GameUtils.RequestModel(truckModel) || !GameUtils.RequestModel(crewModel)) return;

            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");

            for (int i = 0; i < 3; i++)
            {
                var truck = Track(World.CreateVehicle(truckModel,
                    _convoyStart + new Vector3(0f, i * 18f, 0f),
                    Ctx.Locations.Heading("M09.ConvoyStart")));
                if (truck == null || !truck.Exists()) continue;

                truck.IsPersistent = true;
                _convoy.Add(truck);

                var driver = Track(World.CreatePed(crewModel, truck.Position, 0f));
                if (driver == null || !driver.Exists()) continue;

                driver.RelationshipGroup = aegis;
                driver.IsPersistent = true;
                driver.BlockPermanentEvents = true;
                driver.Weapons.Give(WeaponHash.CarbineRifle, 150, true, true);
                driver.Task.WarpIntoVehicle(truck, VehicleSeat.Driver);
                _crews.Add(driver);

                // The rear vehicle is the escort — the one Ice is told to take.
                if (i == 2)
                {
                    _escortTruck = truck;
                    _escortDriver = driver;

                    var blip = Track(truck.AddBlip());
                    blip.Sprite = BlipSprite.Enemy;
                    blip.Color = BlipColor.Red;
                    blip.Name = "Escort truck";
                }
            }

            truckModel.MarkAsNoLongerNeeded();
            crewModel.MarkAsNoLongerNeeded();
        }

        private void StartConvoy()
        {
            for (int i = 0; i < _crews.Count && i < _convoy.Count; i++)
            {
                var driver = _crews[i];
                var truck = _convoy[i];
                if (driver == null || !driver.Exists() || truck == null || !truck.Exists()) continue;

                driver.Task.DriveTo(truck, _ambush, 15f, 22f, DrivingStyle.AvoidTrafficExtremely);
            }
        }

        protected override void OnCleanup()
        {
            _convoy.Clear();
            _crews.Clear();
        }
    }
}
