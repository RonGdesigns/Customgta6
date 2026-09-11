using System;
using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>The four-chapter operation M19–M22 hands state through <see cref="HandoffLedger"/>.</summary>
    public static class PortHeist
    {
        public const string Operation = "PortHeist";
    }

    /// <summary>
    /// M19 — "The Port Heist: Underwater Breach". Berth 44, 03:00, storm.
    ///
    /// Part one of four. Gohan takes the Kraken under the Titan Star, burns a
    /// rectangular breach into hold 3, and clamps ballast floats to the bullion
    /// container while Aegis drops depth charges on the water above him.
    ///
    /// Faked per docs/FEASIBILITY.md: the container does not float up on physics, it
    /// is a prop moved on a curve once the clamps are set. What the player does —
    /// hold position at depth, in the dark, while the water keeps detonating — is
    /// real, and that is the part the mission is actually about.
    /// </summary>
    public sealed class M19UnderwaterBreach : ComposedMission
    {
        private readonly List<Vector3> _clamps = new List<Vector3>();

        /// <summary>The Titan Star stand-in: a tug hull, frozen at the surface, that the Kraken works under.</summary>
        public const string HullModel = "tug";
        /// <summary>How far the hull reaches below the surface (an estimate for the loop, not a measured draft).</summary>
        public const float KeelDepth = 4.5f;
        /// <summary>Room the Kraken needs between the keel and the work point.</summary>
        public const float SubClearance = 4f;
        /// <summary>Water the site needs below the work point before it counts as deep enough.</summary>
        public const float SeabedMargin = 3f;
        public const float ClampSpan = 12f;
        public const float ShiftStep = 40f;

        private Vehicle _hull;
        private Vehicle _kraken;
        private Vector3 _dive;
        private Vector3 _breach;
        private Vector3 _surface;

        public override string Id => "M19";
        public override string Title => "The Port Heist: Underwater Breach";

        protected override bool Setup()
        {
            if (!MissionSites.Prepare(Ctx.Locations, Id)) return false;
            _dive = Ctx.Locations.Position("M19.DiveStart");
            _breach = Ctx.Locations.Position("M19.HullBreach");
            _surface = Ctx.Locations.Position("M19.Surface");
            // The authored breach and clamp points were three markers hanging in
            // open water, sometimes under the seabed. The site is now built from a
            // hull: the work point is under its keel, the clamps under its bow and
            // stern, and the whole thing moves out to deeper water if the seabed is
            // too close. Gohan has something to push against and can reach every mark.
            if (!BuildSiteFromHull()) return false;

            if (!Ctx.Crew.DeploySolo(CrewSlot.Gohan, _dive, Ctx.Locations.Heading("M19.DiveStart")))
            {
                return false;
            }

            ApplyBibleSetting();
            SpawnKraken();
            if (!RequireAssets(_kraken)) return false;
            RequireAsset(_kraken, "The Kraken was lost. The breach cannot be cut without it.");
            Station(CrewSlot.Gohan, _kraken, VehicleSeat.Driver);
            return true;
        }

        /// <summary>
        /// Chapter one of four. The next mission opens with Guess at the hangar, but
        /// Gohan is still sitting in the surfaced Kraken; the record lets M20 put
        /// him there instead of on the apron as if the dive never happened.
        /// </summary>
        protected override void OnPassed()
        {
            var record = OperationHandoff.Capture(PortHeist.Operation, Id, "M20", Ctx.Crew, _kraken);
            record.Notes["kraken"] = "surfaced at M19.Surface with Gohan aboard";
            if (_hull != null && _hull.Exists()) record.Notes["hull"] = "Titan Star stand-in at " + _hull.Position;
            Ctx.Handoffs.Record(record);
            // The freighter does not vanish because chapter one ended; the lift in
            // chapter two hovers over the same water.
            if (_hull != null && _hull.Exists()) Release(_hull);
        }

        public Vehicle Hull => _hull;
        public Vector3 Breach => _breach;
        public IReadOnlyList<Vector3> Clamps => _clamps;

        private bool BuildSiteFromHull()
        {
            var authored = Ctx.Locations.Position("M19.HullBreach");
            float surface = WaterSurface(authored, _surface.Z);
            var site = new Vector3(authored.X, authored.Y, surface);
            float dx = authored.X - _dive.X, dy = authored.Y - _dive.Y;
            float run = (float)Math.Sqrt(dx * dx + dy * dy);
            var bearing = run < 0.5f ? new Vector3(0f, -1f, 0f) : new Vector3(dx / run, dy / run, 0f);
            float workDepth = KeelDepth + SubClearance;
            for (int attempt = 0; attempt < 4; attempt++)
            {
                if (!TrySeabed(site, out float seabed) || seabed <= surface - workDepth - SeabedMargin) break;
                Logger.Warn("M19 site at " + site + " has the seabed at " + seabed.ToString("0.0") + "; moving " + ShiftStep + " m out.");
                site += bearing * ShiftStep;
            }

            var model = new Model(HullModel);
            if (!GameUtils.RequestModel(model)) return false;
            float heading = Core.DriveUpStep.HeadingBetween(site, site + bearing);
            _hull = Track(World.CreateVehicle(model, site, heading));
            model.MarkAsNoLongerNeeded();
            if (_hull == null || !_hull.Exists()) return false;
            _hull.IsPersistent = true;
            _hull.IsEngineRunning = false;
            _hull.IsPositionFrozen = true;
            _hull.IsInvincible = true;
            var blip = Track(_hull.AddBlip());
            blip.Sprite = BlipSprite.Boat;
            blip.Color = BlipColor.Blue;
            blip.Name = "Titan Star";

            _breach = site + new Vector3(0f, 0f, -workDepth);
            _clamps.Clear();
            _clamps.Add(_breach + bearing * ClampSpan);
            _clamps.Add(_breach - bearing * ClampSpan);
            Logger.Info("M19 site: hull at " + site + ", breach " + _breach + ", clamps " + ClampSpan + " m fore and aft.");
            return true;
        }

        private static float WaterSurface(Vector3 point, float fallback)
        {
            var height = new OutputArgument();
            return Function.Call<bool>(Hash.GET_WATER_HEIGHT, point.X, point.Y, 100f, height) ? height.GetResult<float>() : fallback;
        }

        private static bool TrySeabed(Vector3 point, out float seabed)
        {
            var z = new OutputArgument();
            bool found = Function.Call<bool>(Hash.GET_GROUND_Z_FOR_3D_COORD, point.X, point.Y, point.Z + 5f, z, true, false);
            seabed = found ? z.GetResult<float>() : 0f;
            return found;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Dive",
                    new EnterVehicleObjective("Take the Kraken down.", () => _kraken, VehicleSeat.Driver))
                .PlayedBy(CrewSlot.Gohan)
                ;

            yield return new MissionStage("Cut the bulkhead",
                    new MissionInteraction("Burn the breach into hold 3.", () => _breach, 16, 8f, () => _kraken),
                    new DepthChargeHazard(() => _breach))
                .PlayedBy(CrewSlot.Gohan)
                .OnExit(context => GameUtils.Subtitle("~g~Hull breached. Hold 3 is flooding.", 4000))
                .AfterCues("M19_S1_01_GOHAN");

            yield return new MissionStage("Clamp the floats",
                    new MultiHoldObjective("Clamp the ballast floats to the container.",
                        _clamps, 8, 5f, "Clamping", () => _kraken),
                    new DepthChargeHazard(() => _breach))
                .PlayedBy(CrewSlot.Gohan)
                
                .AfterCues("M19_S1_02_ICE");

            yield return new MissionStage("Surface",
                    new DeliverVehicleObjective("Gohan: surface in the Kraken at the yellow marker.", () => _kraken, () => _surface, 8f))
                .PlayedBy(CrewSlot.Gohan)
                .OnExit(context =>
                    GameUtils.Subtitle("~g~She's rising. Guess — bring the Cargobob in now.", 6000))
                .WithCues("M19_S1_03_GOHAN");
        }

        private void SpawnKraken()
        {
            var model = new Model("submersible2");
            if (!GameUtils.RequestModel(model)) return;

            _kraken = Track(World.CreateVehicle(model, _dive + new Vector3(0f, -6f, -3f), 180f));
            model.MarkAsNoLongerNeeded();
            if (_kraken == null || !_kraken.Exists()) return;

            _kraken.IsPersistent = true;

            var blip = Track(_kraken.AddBlip());
            blip.Sprite = BlipSprite.Boat;
            blip.Color = BlipColor.Green;
            blip.Name = "Kraken";
        }
    }

    /// <summary>
    /// Depth charges going off around the work. Passive: it cannot be completed, only
    /// survived, and it exists to make holding a position at depth cost something.
    /// Charges land near the player rather than on them — the threat is the pressure
    /// to leave, not a coin flip.
    /// </summary>
    internal sealed class DepthChargeHazard : Objective
    {
        private static readonly Random Random = new Random();

        private readonly Func<Vector3> _center;
        private int _nextDrop;

        public DepthChargeHazard(Func<Vector3> center) : base("")
        {
            _center = center;
        }

        public override bool IsPassive => true;

        public override void Enter(MissionContext context)
        {
            _nextDrop = Game.GameTime + 4000;
        }

        public override void Update(MissionContext context)
        {
            if (Game.GameTime < _nextDrop) return;

            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            var offset = new Vector3((Random.Next(2) == 0 ? -1 : 1) * Random.Next(22, 32), Random.Next(-15, 15), Random.Next(2, 8));
            World.AddExplosion(player.Position + offset, ExplosionType.Boat, 0.7f, 1.2f, null, true, false);

            GameUtils.Subtitle("~r~Depth charges.", 1200);
            _nextDrop = Game.GameTime + Random.Next(5000, 9000);
        }
    }
}
