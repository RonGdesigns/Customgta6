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
    /// M56 — "Iron in the Drain". The Los Santos River storm channel, 15:00, concrete heat.
    ///
    /// Guess drives the half-track down the flood channel, Ice works the mount, and an Aegis
    /// armored group comes north at them: two Scarabs and an attack helicopter.
    ///
    /// The channel is baked terrain, the same problem M49 has with the Great Ocean Highway —
    /// no placed-entity survey can find a road or a riverbed. What the archives do have is the
    /// channel's own section models, and they draw the whole thing:
    ///
    ///   sp1_12_riv_01 / river_end_1  (-825.0, -1614.0)   the west end
    ///   sp1_12_riv_03                (-704.4, -1538.6)
    ///   sp1_12_riv_06                (-450.7, -1603.2)
    ///   sp1_12_riv_08                (-333.4, -1709.4)   this mission starts here
    ///   sp1_12_riv_09                (-120.9, -1883.5)
    ///   sp1_12_riv_11 / river_end_2  (  54.1, -2135.4)   the harbor end
    ///
    /// Their own z values are useless — a section model's origin is the middle of a
    /// twenty-eight-meter box, which is why one reads 22.52 and its neighbor -0.28. The floor
    /// comes from what people threw into it: a hundred and fifty pieces of
    /// <c>prop_rub_litter</c>, <c>prop_rub_cardpile</c>, shopping trolleys and car wrecks
    /// sitting between z -0.77 and 1.36, median **-0.40**, in a band nineteen meters either
    /// side of the line from riv_08 to riv_09. So the floor is at -0.40, the channel is about
    /// thirty-eight meters wide, and the rim is at 28: a concrete canyon, which is exactly the
    /// arena the synopsis asks for.
    ///
    /// Everything here is placed along that measured centerline rather than guessed at. An
    /// earlier pass surveyed (400, -1500) looking for this channel and found a neighborhood.
    ///
    /// Three details the authored lines settle:
    ///
    /// **"Two armored APCs coming north under the 4th Street bridge."** There is a bridge —
    /// <c>sp1_12_bridge_1fc</c> at (-191.7, -1813.8, 29.63) — between the two Scarab positions,
    /// so they genuinely drive under it on the way up. The crew comes from the north-west and
    /// the armor comes at them, which is what "coming north" means.
    ///
    /// **"Watch the overpass gunners!"** Two of them, on the bridge. Those are the only points
    /// in this mission that are *not* fixed surfaces: they are at street level, where the
    /// engine's walkable query is right, and a gunner snapped from the parapet to the road
    /// beside it is still an overpass gunner.
    ///
    /// **"Ice fires the dual .50-cal turret to destroy two Aegis Scarab APCs and an attack
    /// chopper."** The mount is the half-track's own, no weapon is loaned, and `scarab` is a
    /// real model — the bible named a vehicle the game actually ships. Taking a Savage down
    /// with a .50 takes a while on purpose; that is what the synopsis asks for.
    ///
    /// Two things to be honest about. The zones this stretch sits in are **La Puerta** and
    /// **Maze Bank Arena**, not the east-side river the name "Los Santos River" brings to mind:
    /// this is the western storm channel, and it is the one the archives describe end to end
    /// with a floor that can be measured. The district text in `locations.tsv` is machine-read,
    /// so it names those zones. And the floor is genuinely below sea level, so these keys carry
    /// a `channel` kind: `MissionSites.Prepare` only grounds `land`, which keeps the walkable
    /// query — whose answer here is the street twenty-eight meters up — away from them, and
    /// `validate_locations` knows not to call a sunken channel a mistake.
    ///
    /// Nobody has driven this channel with an F11 capture. The floor height is one number
    /// measured from debris and corrected by one probe at runtime, in the shared way
    /// <see cref="MissionSites.OffsetToSurface"/> describes.
    /// </summary>
    public sealed class M56IronInTheDrain : PreparationOperation
    {
        public const string HalftrackModel = "halftrack";
        /// <summary>The bible names this one, and the game has it.</summary>
        public const string ApcModel = "scarab";
        public const string ChopperModel = "savage";
        public const string PilotModel = "s_m_y_blackops_01";
        /// <summary>Two on the bridge, as the authored line says.</summary>
        public const int OverpassGunners = 2;
        /// <summary>How far above and below the authored channel floor the slab is looked for.</summary>
        public const float FloorHeadroom = 4f;
        public const float FloorSearch = -6f;
        /// <summary>How near the crew the armor has to be before the fight opens.</summary>
        public const float ContactMeters = 110f;
        /// <summary>How often the armor's orders are refreshed. Never every frame.</summary>
        public const int OrderMs = 5000;
        /// <summary>How fast a Scarab comes up a concrete channel.</summary>
        public const float ChargeSpeed = 18f;
        /// <summary>The Savage's attack pattern.</summary>
        public const float ChopperSpeed = 38f;
        public const float ChopperRadius = 40f;
        /// <summary>Where the campaign records the channel is the crew's again.</summary>
        public const string ChannelCargo = "riverChannelHeld";

        private readonly List<Vehicle> _apcs = new List<Vehicle>();
        private readonly List<Ped> _crews = new List<Ped>();
        private Vehicle _halftrack, _chopper;
        private Ped _chopperPilot;
        private float _floorOffset;
        private bool _engaged, _armorDown, _chopperDown;
        private int _orderAt;

        public override string Id => "M56";
        public override string Title => "Iron in the Drain";
        protected override MissionEndpoint Endpoint => MissionEndpoint.EscapeCheckpoint;

        /// <summary>The armor has been sighted and the fight is open.</summary>
        public bool Engaged => _engaged;
        /// <summary>Both Scarabs are wrecked.</summary>
        public bool ArmorDown => _armorDown;
        /// <summary>The Savage is in the concrete.</summary>
        public bool ChopperDown => _chopperDown;
        /// <summary>How far the probe moved the authored channel floor.</summary>
        public float FloorOffset => _floorOffset;
        public Vehicle Halftrack => _halftrack;
        public Vehicle Chopper => _chopper;
        public IReadOnlyList<Vehicle> Apcs => _apcs;

        /// <summary>
        /// The channel floor is twenty-eight meters below the street, so the engine's walkable
        /// query would answer with the street for any of these. The two overpass gunners are
        /// deliberately absent: they stand at street level, where that query is right.
        /// </summary>
        protected override string[] FixedSurfaces =>
            new[] { "M56.Start", "M56.IceStart", "M56.GohanStart", "M56.GuessStart",
                    "M56.Halftrack", "M56.ApcOne", "M56.ApcTwo", "M56.Exit" };

        /// <summary>An authored channel point, moved onto the floor the probe actually found.</summary>
        private Vector3 Floor(string key) => At(key) + new Vector3(0f, 0f, _floorOffset);

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Guess)) return false;

            // One measurement. The whole run was authored from one datum — the debris median
            // at -0.40 — so one probe at the half-track describes the floor for all of it.
            var authored = At("M56.Halftrack");
            _floorOffset = MissionSites.OffsetToSurface(authored, FloorHeadroom, FloorSearch, Id + " channel floor");
            Logger.Info(Id + ": the channel floor is " + (authored.Z + _floorOffset).ToString("0.00") +
                " against the authored " + authored.Z.ToString("0.00") +
                "; every channel point moves by " + _floorOffset.ToString("0.00") + ".");

            _halftrack = Car(HalftrackModel, Floor("M56.Halftrack"), Ctx.Locations.Heading("M56.Halftrack"), true);
            if (!RequireAssets(_halftrack)) return false;
            _halftrack.IsPersistent = true;
            RequireAsset(_halftrack, "The half-track was destroyed. There is no armor left in the channel.");

            // Three seats is all a half-track has, which is why M31 to M35 had to leave
            // somebody behind. Here there is no fourth passenger, so all three ride: Guess on
            // the wheel, Ice on the mount, Gohan in the cab. BoardBrothers would ask for
            // RightRear and be refused, so the seats are assigned outright.
            Station(CrewSlot.Guess, _halftrack, VehicleSeat.Driver);
            Station(CrewSlot.Ice, _halftrack, VehicleSeat.LeftRear);
            Station(CrewSlot.Gohan, _halftrack, VehicleSeat.Passenger);

            foreach (var key in new[] { "M56.ApcOne", "M56.ApcTwo" })
            {
                var apc = Scarab(key);
                if (apc != null) _apcs.Add(apc);
            }
            if (_apcs.Count == 0)
            {
                Logger.Error(Id + ": no Scarab could be put in the channel; there is nothing to fight.");
                GameUtils.Notify("~r~The Aegis armor could not be placed. See Bloodlines.log.");
                return false;
            }

            SpawnChopper("M56.Chopper");
            for (int i = 1; i <= OverpassGunners; i++) Enemy("M56.BridgeGunner" + i);

            Paleto.Review(Ctx, PlacementContract.Ped("M56.Start"),
                PlacementContract.Vehicle("M56.Halftrack", new Model(HalftrackModel)),
                PlacementContract.Vehicle("M56.ApcOne", new Model(ApcModel)),
                PlacementContract.Vehicle("M56.ApcTwo", new Model(ApcModel)));

            Establish("approach", "Twenty-eight meters of concrete and no way around",
                "Aegis has put an armored group into the storm channel to flush the crew out of the city. Guess takes the half-track down the drain, Ice has the mount, and the only way past two Scarabs and a Savage is through them.",
                _halftrack);
            return true;
        }

        /// <summary>One Scarab with a driver and a gunner, facing up-channel at the crew.</summary>
        private Vehicle Scarab(string key)
        {
            var apc = Car(ApcModel, Floor(key), Ctx.Locations.Heading(key), false);
            if (apc == null || !apc.Exists()) { Logger.Warn(Id + ": a Scarab could not be created at " + key + "."); return null; }
            apc.IsPersistent = true;
            var driver = Occupant(apc, VehicleSeat.Driver);
            var gunner = Occupant(apc, VehicleSeat.Passenger);
            if (driver == null)
            {
                Logger.Error(Id + ": the Scarab at " + key + " has no driver; removing it rather than parking a wreck.");
                GameUtils.SafeDelete(apc);
                return null;
            }
            foreach (var ped in new[] { driver, gunner })
            {
                if (ped == null) continue;
                Opposition.Add(ped);
                _crews.Add(ped);
            }
            var blip = Track(apc.AddBlip());
            if (blip != null) { blip.Color = BlipColor.Red; blip.Name = "Aegis Scarab"; }
            return apc;
        }

        /// <summary>
        /// The Savage, created above the rim. An aircraft handed to the AI with its rotors
        /// stopped falls while they spin up, which is what put M45's helicopter in the sea
        /// twice, so this goes through LaunchAirborne.
        /// </summary>
        private void SpawnChopper(string key)
        {
            var model = new Model(ChopperModel);
            var pilotModel = new Model(PilotModel);
            if (!GameUtils.RequestModel(model) || !GameUtils.RequestModel(pilotModel)) return;
            var at = At(key);
            _chopper = Track(World.CreateVehicle(model, at, Ctx.Locations.Heading(key)));
            if (_chopper == null || !_chopper.Exists())
            { Logger.Warn(Id + ": the Savage could not be created at " + at + "."); return; }
            _chopper.IsPersistent = true;
            AircraftHold.LaunchAirborne(_chopper);

            _chopperPilot = Track(World.CreatePed(pilotModel, at, 0f));
            model.MarkAsNoLongerNeeded();
            pilotModel.MarkAsNoLongerNeeded();
            if (_chopperPilot == null || !_chopperPilot.Exists())
            { GameUtils.SafeDelete(_chopper); _chopper = null; return; }

            // Seated outright, never asked to board: a queued warp is replaced by the next
            // task and the pilot is left loose in the air.
            _chopperPilot.SetIntoVehicle(_chopper, VehicleSeat.Driver);
            if (_chopper.GetPedOnSeat(VehicleSeat.Driver) != _chopperPilot)
            {
                Logger.Error(Id + ": the Savage pilot could not be seated; removing that aircraft.");
                GameUtils.SafeDelete(_chopperPilot); GameUtils.SafeDelete(_chopper);
                _chopperPilot = null; _chopper = null;
                return;
            }
            _chopperPilot.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            _chopperPilot.IsPersistent = true;
            _chopperPilot.BlockPermanentEvents = true;
            Opposition.Add(_chopperPilot);
            Blips.Attach(_chopperPilot, BlipColor.Red, "Aegis Savage");
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Take the half-track down the drain",
                new TravelObjective("Guess: drive the half-track down the channel", () => Floor("M56.ApcOne"), 120f, () => _halftrack))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(c => Engage())
                .AfterCues("M56_S1_01_GUESS");

            var kills = _apcs.Count == 0
                ? new Objective[] { new ConditionObjective("Wreck the Aegis Scarabs", () => true) }
                : Enumerable.Range(0, 2).Select(i => (Objective)new DestroyVehicleObjective(
                    "Wreck the Aegis Scarabs — two in the channel",
                    () => i < _apcs.Count ? _apcs[i] : null)).ToArray();

            yield return new MissionStage("Break the armor",
                kills.Concat(new Objective[]
                {
                    new ProtectObjective("", () => _halftrack, "The half-track was wrecked in the channel."),
                }).ToArray())
                .AnyBrother()
                .OnExit(c => _armorDown = true)
                .AfterCues("M56_S1_02_ICE");

            yield return new MissionStage("Put the Savage in the concrete",
                new DestroyVehicleObjective("Shoot the Aegis Savage down", () => _chopper),
                new ProtectObjective("", () => _halftrack, "The half-track was wrecked under the Savage."))
                .AnyBrother()
                .OnExit(c => _chopperDown = true)
                .AfterCues("M56_S1_03_GOHAN");

            // Back up the channel the way they came in. The harbor end is full of wrecks and
            // pallets — good scenery, no room to put a half-track through — so the run-out is
            // north-west, where the floor is clear for thirty meters.
            yield return new MissionStage("Clear the channel",
                new TravelObjective("Bring the half-track back up the channel", () => Floor("M56.Exit"), 25f, () => _halftrack))
                .AnyBrother();
        }

        /// <summary>The armor comes north. This is the moment the authored line is spoken at.</summary>
        private void Engage()
        {
            _engaged = true;
            Fighting = true;
            _orderAt = 0;
            Awareness.ReportToAll(Stimulus.RadioCall, _halftrack != null && _halftrack.Exists()
                ? _halftrack.Position : Floor("M56.Halftrack"));
        }

        /// <summary>
        /// Drive at the half-track and shoot at it. Orders go out on a five-second cadence,
        /// never every frame: a fresh task each tick restarts it before the driver can act on
        /// it, which is the defect behind M31, M33 and M37's motionless guards and behind a
        /// driver hesitating short of where he was sent.
        /// </summary>
        private void PressTheAttack()
        {
            if (Game.GameTime < _orderAt) return;
            _orderAt = Game.GameTime + OrderMs;
            var target = _halftrack != null && _halftrack.Exists() ? _halftrack : null;
            if (target == null) return;
            var driver = Ctx.Crew.PedFor(CrewSlot.Guess);

            foreach (var apc in _apcs)
            {
                if (apc == null || !apc.Exists() || !apc.IsDriveable) continue;
                var pilot = apc.GetPedOnSeat(VehicleSeat.Driver);
                if (pilot == null || !pilot.Exists() || pilot.IsDead) continue;
                pilot.Task.StartVehicleMission(apc, target, VehicleMissionType.Ram, ChargeSpeed,
                    VehicleDrivingFlags.DrivingModeAvoidVehiclesReckless, 6f, 0f, true);
                var gunner = apc.GetPedOnSeat(VehicleSeat.Passenger);
                if (gunner != null && gunner.Exists() && gunner.IsAlive && driver != null && driver.Exists())
                    Function.Call(Hash.TASK_VEHICLE_SHOOT_AT_PED, gunner, driver, 90f);
            }

            if (_chopper != null && _chopper.Exists() && _chopper.IsDriveable &&
                _chopperPilot != null && _chopperPilot.Exists() && !_chopperPilot.IsDead &&
                _chopperPilot.IsInVehicle(_chopper) && driver != null && driver.Exists())
                _chopperPilot.Task.StartHeliMission(_chopper, driver, VehicleMissionType.Attack,
                    ChopperSpeed, ChopperRadius,
                    (int)Math.Max(_chopper.Position.Z, driver.Position.Z + 45f), 30, -1f, 70f, (HeliMissionFlags)0);
        }

        protected override void OnUpdate()
        {
            // Guess keeps driving while the player is on the mount as Ice. Without a standing
            // destination the half-track stops the moment the player switches away from him.
            DrivingDestination = _armorDown && _chopperDown ? (Func<Vector3>)(() => Floor("M56.Exit")) : null;
            if (_engaged && !(_armorDown && _chopperDown)) PressTheAttack();
            base.OnUpdate();
        }

        protected override void OnCleanup()
        {
            _orderAt = 0;
            base.OnCleanup();
        }

        protected override void OnPassed()
        {
            if (!_engaged || !_armorDown || !_chopperDown)
                throw new InvalidOperationException("Both Scarabs and the Savage have to be down.");
            Ctx.State?.SetCargo(ChannelCargo, "M56.Exit");
            Logger.Info(Id + ": the channel is clear from the bridge to the harbor end.");
            Release(_halftrack);
        }
    }
}
