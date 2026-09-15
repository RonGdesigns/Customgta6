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
    /// M68 — "Blood Brothers: The Drain". The storm channel, and the climax starts.
    ///
    /// Guess takes the rig down the flood channel with everything they own in the trailer. Ice
    /// works the roof gun. Gohan drops mines behind them.
    ///
    /// **The channel is M56's, measured the same way and reused deliberately.** The centerline
    /// runs from <c>sp1_12_riv_08</c> (-333.4, -1709.4) to <c>sp1_12_riv_09</c> (-120.9,
    /// -1883.5); the floor is the debris median at **-0.40**, from a hundred and fifty pieces
    /// of litter, cardpiles, trolleys and wrecks lying on it; the channel is about
    /// thirty-eight meters wide and the rim is at 28. Every key here carries the `channel` kind
    /// for the same reasons M56's do, and one probe at the rig moves all of them.
    ///
    /// Running the finale through a place the campaign already established is the point. The
    /// player has fought here; now he is driving eighteen wheels through it with everything to
    /// lose.
    ///
    /// **The bridge-support demolition in `M68_S1_03_GOHAN` is not reproduced.**
    /// <c>sp1_12_bridge_1fc</c> at (-191.7, -1813.8, 29.63) is a single map entity: it cannot
    /// be partly destroyed, and blowing a support that holds nothing up is a bang with no
    /// consequence. The magnetic trail mines are real — Gohan carries proximity mines and drops
    /// them — so the first half of that line happens and the second does not. Recorded in
    /// `data/mission_gameplay.tsv`.
    /// </summary>
    public sealed class M68BloodBrothersTheDrain : PreparationOperation
    {
        public const string RigModel = "phantom";
        public const string TrailerModel = "trailers";
        public const string ChaserModel = "insurgent";
        /// <summary>Gun-trucks coming in off the bridge ramps.</summary>
        public const int ChaserCount = 3;
        /// <summary>Mines Gohan carries for the trail.</summary>
        public const int Mines = 8;
        /// <summary>How far above and below the authored channel floor the slab is looked for.</summary>
        public const float FloorHeadroom = 4f;
        public const float FloorSearch = -6f;
        /// <summary>How often the chasers are re-ordered. Never every frame.</summary>
        public const int OrderMs = 5000;
        public const float ChaseSpeed = 26f;
        /// <summary>Where the campaign records the rig is through the channel.</summary>
        public const string DrainCargo = "drainRunClear";

        private readonly List<Vehicle> _chasers = new List<Vehicle>();
        private Vehicle _rig, _trailer;
        private float _floorOffset;
        private int _orderAt;
        private bool _rolling, _chaseBroken;

        public override string Id => "M68";
        public override string Title => "Blood Brothers: The Drain";
        protected override MissionEndpoint Endpoint => MissionEndpoint.ContinuousNext;

        /// <summary>The rig is under way down the channel.</summary>
        public bool Rolling => _rolling;
        /// <summary>The pursuing gun-trucks are wrecked.</summary>
        public bool ChaseBroken => _chaseBroken;
        public Vehicle Rig => _rig;
        public IReadOnlyList<Vehicle> Chasers => _chasers;
        /// <summary>How far the probe moved the authored channel floor.</summary>
        public float FloorOffset => _floorOffset;

        /// <summary>Same reasoning as M56: the floor is twenty-eight meters under the street.</summary>
        protected override string[] FixedSurfaces =>
            new[] { "M68.Start", "M68.IceStart", "M68.GohanStart", "M68.GuessStart", "M68.Rig", "M68.Mouth" }
                .Concat(Enumerable.Range(1, ChaserCount).Select(i => "M68.Chaser" + i)).ToArray();

        /// <summary>An authored channel point, on the floor the probe actually found.</summary>
        private Vector3 Floor(string key) => At(key) + new Vector3(0f, 0f, _floorOffset);

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Guess)) return false;

            var authored = At("M68.Rig");
            _floorOffset = MissionSites.OffsetToSurface(authored, FloorHeadroom, FloorSearch, Id + " channel floor");
            Logger.Info(Id + ": the channel floor is " + (authored.Z + _floorOffset).ToString("0.00") +
                " against the authored " + authored.Z.ToString("0.00") + ".");

            _rig = Car(RigModel, Floor("M68.Rig"), Ctx.Locations.Heading("M68.Rig"), true);
            _trailer = _rig == null ? null
                : Car(TrailerModel, _rig.Position - _rig.ForwardVector * 11f, Ctx.Locations.Heading("M68.Rig"), false);
            if (!RequireAssets(_rig, _trailer)) return false;
            _rig.IsPersistent = true;
            _trailer.IsPersistent = true;
            Function.Call(Hash.ATTACH_VEHICLE_TO_TRAILER, _rig, _trailer, 2f);
            RequireAsset(_rig, "The rig was destroyed in the channel with everything in the trailer.");

            Station(CrewSlot.Guess, _rig, VehicleSeat.Driver);
            Station(CrewSlot.Ice, _rig, VehicleSeat.Passenger);

            for (int i = 1; i <= ChaserCount; i++)
            {
                var truck = Car(ChaserModel, Floor("M68.Chaser" + i), Ctx.Locations.Heading("M68.Chaser" + i), false);
                if (truck == null || !truck.Exists()) continue;
                truck.IsPersistent = true;
                var driver = Occupant(truck, VehicleSeat.Driver);
                var gunner = Occupant(truck, VehicleSeat.Passenger);
                if (driver == null) { GameUtils.SafeDelete(truck); continue; }
                foreach (var ped in new[] { driver, gunner }) if (ped != null) Opposition.Add(ped);
                var blip = Track(truck.AddBlip());
                if (blip != null) { blip.Color = BlipColor.Red; blip.Name = "PMC gun-truck"; }
                _chasers.Add(truck);
            }
            if (_chasers.Count == 0)
            {
                Logger.Error(Id + ": no gun-truck could be placed in the channel; there is no pursuit.");
                GameUtils.Notify("~r~The pursuit could not be placed. See Bloodlines.log.");
                return false;
            }

            // Real mines for the trail. The bridge demolition in the same line is not
            // reproduced, but the mines are exactly what the line says they are.
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            if (gohan != null && gohan.Exists()) gohan.Weapons.Give(WeaponHash.ProximityMine, Mines, false, true);
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            if (ice != null && ice.Exists()) ice.Weapons.Give(WeaponHash.MG, 500, false, true);

            Fighting = true;
            Establish("approach", "Eighteen wheels through the drain",
                "Everything the three of them own is in that trailer and the only road out of the city that Aegis has not closed is a concrete ditch. Guess drives it; Ice has the gun; Gohan puts mines behind them.",
                _rig);
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Get the rig moving",
                new ConditionObjective("Guess: get the rig rolling down the channel",
                    () => _rig != null && _rig.Exists() && _rig.Speed > 6f)
                { Marker = () => _rig != null && _rig.Exists() ? _rig.Position : Floor("M68.Rig"), MarkerRadius = 8f })
                .OwnedBy(CrewSlot.Guess)
                .OnExit(c => { _rolling = true; DrivingDestination = () => Floor("M68.Mouth"); })
                .AfterCues("M68_S1_01_GUESS");

            var kills = Enumerable.Range(0, ChaserCount).Select(i => (Objective)new DestroyVehicleObjective(
                "Break the pursuit — PMC gun-trucks in the channel",
                () => i < _chasers.Count ? _chasers[i] : null)).ToArray();

            yield return new MissionStage("Break the pursuit",
                kills.Concat(new Objective[]
                {
                    new ProtectObjective("", () => _rig, "The rig was wrecked in the channel."),
                }).ToArray())
                .AnyBrother()
                .OnExit(c => _chaseBroken = true)
                .AfterCues("M68_S1_02_ICE", "M68_S1_03_GOHAN");

            yield return new MissionStage("Out of the channel",
                new TravelObjective("Take the rig out of the channel mouth toward the airport",
                    () => Floor("M68.Mouth"), 30f, () => _rig))
                .AnyBrother()
                .OnExit(c => Through());
        }

        /// <summary>
        /// The pursuit, ordered on a cadence rather than every frame: a fresh task each tick
        /// restarts it before the driver can act on it.
        /// </summary>
        private void PressTheChase()
        {
            if (Game.GameTime < _orderAt) return;
            _orderAt = Game.GameTime + OrderMs;
            if (_rig == null || !_rig.Exists()) return;
            var driver = Ctx.Crew.PedFor(CrewSlot.Guess);
            foreach (var truck in _chasers)
            {
                if (truck == null || !truck.Exists() || !truck.IsDriveable) continue;
                var pilot = truck.GetPedOnSeat(VehicleSeat.Driver);
                if (pilot == null || !pilot.Exists() || pilot.IsDead) continue;
                pilot.Task.StartVehicleMission(truck, _rig, VehicleMissionType.Ram, ChaseSpeed,
                    VehicleDrivingFlags.DrivingModeAvoidVehiclesReckless, 8f, 0f, true);
                var gunner = truck.GetPedOnSeat(VehicleSeat.Passenger);
                if (gunner != null && gunner.Exists() && gunner.IsAlive && driver != null && driver.Exists())
                    Function.Call(Hash.TASK_VEHICLE_SHOOT_AT_PED, gunner, driver, 80f);
            }
        }

        private void Through()
        {
            Ctx.State?.SetCargo(DrainCargo, "M68.Mouth");
            Logger.Info(Id + ": the rig is out of the channel and pointed at the airport.");
        }

        protected override void OnUpdate()
        {
            if (_rolling && !_chaseBroken) PressTheChase();
            base.OnUpdate();
        }

        protected override void OnCleanup() { _orderAt = 0; base.OnCleanup(); }

        protected override void OnPassed()
        {
            if (!_rolling || !_chaseBroken)
                throw new InvalidOperationException("The rig has to roll and the pursuit has to be broken.");
            Release(_rig);
            Release(_trailer);
        }
    }
}
