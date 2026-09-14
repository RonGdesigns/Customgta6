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
    /// Chapter one of the Paleto operation. Gohan takes the Kraken under the hull,
    /// clamps the seismic charges on the mooring points and cuts the sensor line that
    /// runs the vessel's automated defenses, so that the helicopter Ice is in can
    /// come over the rail at all.
    ///
    /// The authored beat is charges on a rig's stabilizer legs. There is no rig in
    /// the installed game; the work happens under a real hull whose keel, seabed and
    /// extents were read out of the archives. See PaletoSite.
    ///
    /// Nothing here passes a mission. The parent joins straight into M45 with Gohan
    /// still sitting in the surfaced Kraken.
    /// </summary>
    public sealed class M44PaletoSubSurface : ComposedMission
    {
        public const string SubModel = "submersible2";
        /// <summary>How long a clamp takes to bite, in seconds.</summary>
        public const int ClampSeconds = 6;
        /// <summary>How long the structure gets to stream in before the chapter refuses to start.</summary>
        public const int StructureWaitMs = 9000;

        private readonly List<Vector3> _clamps = new List<Vector3>();
        private Vehicle _kraken;
        private Vector3 _sensor;
        private Vector3 _surface;
        private int _placed;
        private bool _cut;

        public override string Id => "M44";
        public override string Title => "Paleto Deep-Sea: Sub-Surface";
        protected override MissionEndpoint Endpoint => MissionEndpoint.ContinuousNext;

        public Vehicle Kraken => _kraken;
        public IReadOnlyList<Vector3> Clamps => _clamps;
        /// <summary>Every charge clamped and the sensor line cut: the sea defenses are actually down.</summary>
        public bool Placed => _clamps.Count > 0 && _placed >= _clamps.Count && _cut;

        private Vector3 At(string key) => Ctx.Locations.Position(key);
        /// <summary>The work depth under a surface key: below the keel, above the seabed.</summary>
        private Vector3 Working(string key)
        {
            var point = At(key);
            return new Vector3(point.X, point.Y, -PaletoSite.DiveDepth);
        }

        protected override bool Setup()
        {
            var world = Paleto.Of(Ctx);
            if (!LoadStructure(world)) return false;

            // Gohan dives; the other two wait ashore where Guess can still pull away.
            // Nobody is deployed onto water.
            if (!Ctx.Crew.Deploy(CrewSlot.Gohan, At("M44.Start"), Ctx.Locations.Heading("M44.Start"))) return false;
            ApplyBibleSetting();

            if (!SpawnKraken()) return false;
            world?.Bind("kraken", _kraken);
            RequireAsset(_kraken, "The Kraken was lost. The charges cannot be clamped without it.");

            _clamps.Clear();
            foreach (var key in new[] { "M44.Clamp1", "M44.Clamp2", "M44.Clamp3" }) _clamps.Add(Working(key));
            _sensor = Working("M44.Sensor");
            _surface = At("M44.Surface");

            Paleto.Review(Ctx, PlacementContract.Ped("M44.Start"));
            Ctx.Crew.CompanionsHoldPosition = true;
            Station(CrewSlot.Gohan, _kraken, VehicleSeat.Driver);
            Station(CrewSlot.Ice, At("M44.Start"));
            Station(CrewSlot.Guess, At("M44.Start"));
            return true;
        }

        /// <summary>
        /// The cove structure is script-loaded map. Ask for it, give it a bounded
        /// wait to stream, and refuse the chapter rather than dive at empty water.
        /// </summary>
        private bool LoadStructure(PaletoWorld world)
        {
            if (world == null) return true; // A chapter run alone in QA has no structure to own.
            world.Structure.Request();
            while (!world.Structure.Ready && world.Structure.WaitedMilliseconds < StructureWaitMs) Script.Wait(250);
            if (!world.Structure.Ready)
            {
                Logger.Error("Paleto: the cove structure did not stream in within " + StructureWaitMs + " ms.");
                GameUtils.Notify("~r~The vessel at the cove did not load. Check Bloodlines.log.");
                return false;
            }
            return true;
        }

        private bool SpawnKraken()
        {
            var model = new Model(SubModel);
            if (!GameUtils.RequestModel(model)) return false;
            // M43 parked it; restage it where that staging recorded, not somewhere new.
            string staged = Paleto.CargoAt(Ctx, "offshoreSub");
            string key = !string.IsNullOrEmpty(staged) && Ctx.Locations.Get(staged)?.Kind == "water" ? staged : "M44.Sub";
            var point = MarineSites.ResolveOrThrow(Ctx.Locations, key, PaletoSite.DiveDepth) + new Vector3(0f, 0f, -1.4f);
            _kraken = Track(World.CreateVehicle(model, point, Ctx.Locations.Heading(key)));
            model.MarkAsNoLongerNeeded();
            if (_kraken == null || !_kraken.Exists()) return false;
            _kraken.IsPersistent = true;
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Take the Kraken under the hull",
                new TravelObjective("Gohan: dive the Kraken to the marker under the hull", () => _clamps[0], 14f, () => _kraken))
                .OwnedBy(CrewSlot.Gohan);

            var clamping = new MultiHoldObjective("Gohan: hold the Kraken at each mooring point until the charge bites",
                _clamps, ClampSeconds, 6f, "Clamping charge", () => _kraken) { SiteDone = _ => _placed++ };
            yield return new MissionStage("Clamp the seismic charges", clamping)
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(c => { if (_placed < _clamps.Count) throw new InvalidOperationException("A charge was not clamped."); })
                .AfterCues("M44_S1_01_GOHAN");

            yield return new MissionStage("Cut the sensor line",
                new MultiHoldObjective("Gohan: hold at the sensor junction until the line is cut",
                    new[] { _sensor }, ClampSeconds, 5f, "Cutting sensor line", () => _kraken))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(c =>
                {
                    _cut = true;
                    var world = Paleto.Of(c);
                    if (world != null) world.DefensesDown = true;
                    Logger.Info("Paleto: sensor line cut; automated defenses down. The charges are clamped but not armed.");
                })
                .AfterCues("M44_S1_02_ICE");

            yield return new MissionStage("Surface clear of the hull",
                new SurfaceSubObjective(() => _kraken, () => _surface))
                .OwnedBy(CrewSlot.Gohan)
                .AfterCues("M44_S1_03_GUESS");
        }

        protected override void OnPassed()
        {
            if (!Placed) throw new InvalidOperationException("The charges and the sensor line were not both finished.");
            var world = Paleto.Of(Ctx);
            if (world != null && !world.DefensesDown)
                throw new InvalidOperationException("The sea defenses were not recorded as down.");
            // Entities stay under the operation's ownership: the next chapter borrows
            // this exact submarine with Gohan still in it.
            Paleto.StageCargo(Ctx, "kraken", "M44.Surface");
            var record = OperationHandoff.Capture(Paleto.Operation.Title, Id, "M45", Ctx.Crew, _kraken);
            record.Notes["kraken"] = "surfaced at M44.Surface with Gohan aboard";
            record.Notes["defenses"] = "sensor line cut; automated defenses down";
            record.Notes["charges"] = _placed + " clamped, not armed";
            Ctx.Handoffs.Record(record);
            Release(_kraken);
        }
    }
}
