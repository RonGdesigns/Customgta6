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

        /// <summary>The cargo key the bullion container is recorded under from the moment it surfaces.</summary>
        public const string BullionCargo = "bullion";

        public static void RecordCargo(MissionContext context, string key, string destination)
        {
            if (context.PortHeist != null) context.PortHeist.StageCargo(key, destination);
            else context.State?.SetCargo(key, destination);
        }

        public static string CargoAt(MissionContext context, string key) =>
            context.PortHeist != null ? context.PortHeist.CargoAt(key) : context.State?.CargoAt(key);

        public static bool IsContinuing(MissionContext context) => context.PortHeist?.Continuing == true;

        public static void RequireFallback(SceneBlocking blocking, string action)
        {
            blocking.Complete();
            if (!blocking.Succeeded) throw new System.InvalidOperationException(action + " failed. Restart the mission.");
        }

        /// <summary>
        /// A chapter's released asset, taken over by the next chapter instead of a
        /// second copy spawned beside it: the Kraken chapter one left floating, the
        /// lift M18 parked, the launch M20 boarded.
        /// </summary>
        public static Vehicle Nearby(Model model, Vector3 point, float radius)
        {
            foreach (var nearby in World.GetNearbyVehicles(point, radius))
                if (nearby != null && nearby.Exists() && nearby.Model.Hash == model.Hash) return nearby;
            return null;
        }

        /// <summary>The same for a prop: the surfaced container M20 hooks is the one M19 floated.</summary>
        public static Prop NearbyProp(Model model, Vector3 point, float radius)
        {
            foreach (var nearby in World.GetNearbyProps(point, radius))
                if (nearby != null && nearby.Exists() && nearby.Model.Hash == model.Hash) return nearby;
            return null;
        }

        /// <summary>Where the container floats once M19 has clamped it: beside the surfacing mark, derived the same way in M19 and M20.</summary>
        public static Vector3 ContainerPoint(LocationBook book)
        {
            return book.Position("M19.BullionSurface");
        }
    }

    /// <summary>
    /// M19 — "The Port Heist: Underwater Breach". Berth 44, 03:00, storm.
    ///
    /// Part one of four. Gohan takes the Kraken under the Titan Star, burns a
    /// rectangular breach into hold 3, and clamps ballast floats to the bullion
    /// container while Aegis drops depth charges on the water above him.
    ///
    /// Seen, not told: a brief cut into the sub at its actual staged location, Ice
    /// on the pier and Ron in the lift checking it, before anything moves; the
    /// breach and the floats as work under the keel; the container becoming
    /// recoverable as an insert the player's finished work causes, not a line; the
    /// sub surfaced at the support mark and every state recorded for the lift.
    ///
    /// Faked per docs/FEASIBILITY.md: the container does not float up on physics, it
    /// is a prop placed at the surface once the clamps are set. What the player does —
    /// hold position at depth, in the dark, while the water keeps detonating — is
    /// real, and that is the part the mission is actually about.
    /// </summary>
    public sealed class M19UnderwaterBreach : ComposedMission
    {
        private readonly List<Vector3> _clamps = new List<Vector3>();
        private readonly List<Prop> _floats = new List<Prop>();

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
        private Vehicle _lift;
        private Prop _container;
        private Vector3 _dive;
        private Vector3 _breach;
        private Vector3 _surface;
        private Vector3 _containerPoint;
        private Vector3 _submergedCargo;
        private Vector3 _hullBearing;
        private string _stagedAt = "";
        private bool _floated;

        public override string Id => "M19";
        public override string Title => "The Port Heist: Underwater Breach";
        protected override MissionEndpoint Endpoint => MissionEndpoint.ContinuousNext;

        public Vehicle Hull => _hull;
        public Vehicle Kraken => _kraken;
        public Vehicle Lift => _lift;
        public Prop Container => _container;
        public IReadOnlyList<Prop> Floats => _floats;
        public Vector3 Breach => _breach;
        public IReadOnlyList<Vector3> Clamps => _clamps;
        public bool Floated => _floated;
        /// <summary>The location key the Kraken was taken from: M18's channel, M17's slip, or the dive mark when nothing was staged.</summary>
        public string StagedAt => _stagedAt;

        protected override bool Setup()
        {
            // Do not independently snap three water markers to unrelated points.
            MissionSites.Ground(Ctx.Locations, "M12.PierWatch");
            MissionSites.Ground(Ctx.Locations, "M18.SaltHangar");
            _dive = Ctx.Locations.Position("M19.DiveStart");
            _breach = Ctx.Locations.Position("M19.HullBreach");
            _surface = Ctx.Locations.Position("M19.Surface");
            _containerPoint = PortHeist.ContainerPoint(Ctx.Locations);
            // The authored breach and clamp points were three markers hanging in
            // open water, sometimes under the seabed. The site is now built from a
            // hull/cargo group: reachable work centers lie beside the actual model
            // bounds instead of inside collision. A bounded sampled-water preflight
            // rejects unknown or shallow sites; live approach testing is still owed.
            if (!BuildSiteFromHull()) return false;
            SpawnContainer();
            if (!RequireAssets(_hull, _container)) return false;
            RequireAsset(_hull, "The worksite hull disappeared. Restart the heist.");
            RequireAsset(_container, "The bullion container disappeared. Restart the heist.");

            // The whole crew is on the operation: Gohan in the sub, Ice on the pier,
            // Ron at the lift. Nobody is deployed onto open water.
            if (!Ctx.Crew.Deploy(CrewSlot.Gohan, Ctx.Locations.Position("M12.PierWatch"), Ctx.Locations.Heading("M12.PierWatch")))
            {
                return false;
            }

            ApplyBibleSetting();
            SpawnKraken();
            SpawnLift();
            if (!RequireAssets(_kraken, _lift)) return false;
            Ctx.PortHeist?.Bind("hull", _hull);
            Ctx.PortHeist?.Bind("kraken", _kraken);
            Ctx.PortHeist?.Bind("lift", _lift);
            RequireAsset(_kraken, "The Kraken was lost. The breach cannot be cut without it.");
            Ctx.Crew.CompanionsHoldPosition = true;
            Station(CrewSlot.Gohan, _kraken, VehicleSeat.Driver);
            Station(CrewSlot.Ice, Ctx.Locations.Position("M12.PierWatch"));
            if (_lift != null && _lift.Exists()) Station(CrewSlot.Guess, _lift, VehicleSeat.Driver);
            else Station(CrewSlot.Guess, Ctx.Locations.Position("M18.SaltHangar"));
            PlayApproach();
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
            record.Notes["container"] = _floated ? "floating beside M19.Surface on " + _floats.Count + " clamped floats" : "still in hold 3";
            record.Notes["lift"] = _lift != null && _lift.Exists() ? "Ron in the Cargobob at M18.SaltHangar, engine off" : "no lift staged";
            if (_hull != null && _hull.Exists()) record.Notes["hull"] = "Titan Star stand-in at " + _hull.Position;
            Ctx.Handoffs.Record(record);
            PortHeist.RecordCargo(Ctx, "kraken", "M19.Surface");
            if (_floated) PortHeist.RecordCargo(Ctx, PortHeist.BullionCargo, "M19.Surface");
            // The freighter does not vanish because chapter one ended; the lift in
            // chapter two hovers over the same water, hooks the same container and
            // takes off in the same aircraft.
            if (_hull != null && _hull.Exists()) Release(_hull);
            if (_container != null && _container.Exists()) Release(_container);
            foreach (var f in _floats) if (f != null && f.Exists()) Release(f);
            if (_lift != null && _lift.Exists()) Release(_lift);
            if (_kraken != null && _kraken.Exists()) Release(_kraken);
        }

        private bool BuildSiteFromHull()
        {
            // A tug is the stock visible hull stand-in; do not promise a walkable
            // freighter interior or send the Kraken through solid hull collision.
            var model = new Model(HullModel);
            var subModel = new Model("submersible2");
            var cargoModel = new Model("prop_container_01a");
            if (!GameUtils.RequestModel(model) || !GameUtils.RequestModel(subModel) || !GameUtils.RequestModel(cargoModel))
                return false;
            try
            {
                var bounds = model.Dimensions;
                var subBounds = subModel.Dimensions;
                var cargoBounds = cargoModel.Dimensions;
                float hullHalfWidth = Math.Max(Math.Abs(bounds.Item1.X), Math.Abs(bounds.Item2.X));
                float hullHalfLength = Math.Max(Math.Abs(bounds.Item1.Y), Math.Abs(bounds.Item2.Y));
                float subHalfWidth = Math.Max(2f, Math.Max(Math.Abs(subBounds.Item1.X), Math.Abs(subBounds.Item2.X)));
                float cargoHalfWidth = Math.Max(2f, Math.Max(Math.Abs(cargoBounds.Item1.X), Math.Abs(cargoBounds.Item2.X)));
                float cargoHalfLength = Math.Max(5f, Math.Max(Math.Abs(cargoBounds.Item1.Y), Math.Abs(cargoBounds.Item2.Y)));
                float workDepth = KeelDepth + SubClearance;
                float cargoSide = hullHalfWidth + cargoHalfWidth + 3f;
                float workSide = cargoSide + cargoHalfWidth + subHalfWidth + 2f;
                float heading = Ctx.Locations.Heading("M19.Worksite");
                float radians = heading * (float)Math.PI / 180f;
                _hullBearing = new Vector3(-(float)Math.Sin(radians), (float)Math.Cos(radians), 0f);
                var right = new Vector3(_hullBearing.Y, -_hullBearing.X, 0f);
                var site = MarineSites.ResolveOrThrow(Ctx.Locations, "M19.Worksite",
                    workDepth + SeabedMargin + 2f, workSide + subHalfWidth + 2f,
                    Math.Max(hullHalfLength + 3f, cargoHalfLength + 10f));
                _hull = Track(World.CreateVehicle(model, site, heading));
                if (_hull == null || !_hull.Exists()) return false;
                _hull.IsPersistent = true; _hull.IsEngineRunning = false;
                _hull.IsPositionFrozen = true; _hull.IsInvincible = true;
                var blip = Track(_hull.AddBlip());
                if (blip != null) { blip.Sprite = BlipSprite.Boat; blip.Color = BlipColor.Blue; blip.Name = "Titan Star - worksite hull"; }
                // Work alongside the hull, outside both ship and cargo collision.
                _breach = site + right * workSide - new Vector3(0f, 0f, workDepth);
                _submergedCargo = site + right * cargoSide - new Vector3(0f, 0f, workDepth + 1f);
                _clamps.Clear();
                _clamps.Add(_breach + _hullBearing * (cargoHalfLength - 1f));
                _clamps.Add(_breach - _hullBearing * (cargoHalfLength - 1f));
                _surface = site + right * workSide;
                _dive = _surface + _hullBearing * (cargoHalfLength + 5f);
                _containerPoint = site + right * cargoSide;
                // All runtime consumers (the HUD and the helicopter) see this same
                // resolved layout. Personal survey files are never overwritten.
                SetPoint("M19.HullBreach", _breach);
                SetPoint("M19.ClampOne", _clamps[0]); SetPoint("M19.ClampTwo", _clamps[1]);
                SetPoint("M19.Surface", _surface);
                SetPoint("M19.BullionSurface", _containerPoint); SetPoint("M19.DiveStart", _dive);
                Logger.Info("M19 coherent site: hull=" + site + ", work=" + _breach + ", cargo=" + _submergedCargo + ", surface=" + _surface);
                return true;
            }
            finally { model.MarkAsNoLongerNeeded(); subModel.MarkAsNoLongerNeeded(); cargoModel.MarkAsNoLongerNeeded(); }
        }

        private void SetPoint(string key, Vector3 point)
        {
            var location = Ctx.Locations.Get(key);
            if (location != null) location.Position = point;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Dive",
                    new EnterVehicleObjective("Take the Kraken down.", () => _kraken, VehicleSeat.Driver))
                .PlayedBy(CrewSlot.Gohan);

            yield return new MissionStage("Cut the bulkhead",
                    new MissionInteraction("Burn the breach into hold 3.", () => _breach, 16, 8f, () => _kraken),
                    new DepthChargeHazard(() => _breach))
                .PlayedBy(CrewSlot.Gohan)
                .OnExit(context => GameUtils.Subtitle("~g~Hull breached. Hold 3 is flooding.", 4000))
                .AfterCues("M19_S1_01_GOHAN");

            // Each clamp leaves a real float at the site; the last one brings the
            // container up, on camera, because the work is finished and not because a
            // line says so.
            yield return new MissionStage("Clamp the floats",
                    new MultiHoldObjective("Clamp the ballast floats to the container.",
                        _clamps, 8, 5f, "Clamping", () => _kraken) { SiteDone = FitFloat },
                    new DepthChargeHazard(() => _breach))
                .PlayedBy(CrewSlot.Gohan)
                .OnExit(context => PlayFloat());

            yield return new MissionStage("Surface",
                    new SurfaceSubObjective(() => _kraken, () => _surface))
                .PlayedBy(CrewSlot.Gohan)
                .OnExit(context =>
                    GameUtils.Subtitle("~g~Kraken surfaced at the support mark. The container is on the water; the lift is Ron's.", 6000))
                .WithCues("M19_S1_03_GOHAN");
        }

        // ---------- beats ----------

        /// <summary>A brief cut into the operation as staged: the sub where M18 left it, Ice on the pier, Ron in the lift checking it over. No second briefing.</summary>
        private void PlayApproach()
        {
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            var blocking = new SceneBlocking();
            if (_kraken != null && _kraken.Exists()) blocking.Then(new ShotStep(3200, _kraken, new Vector3(-7f, 4f, 2.2f), _kraken, new Vector3(0f, 0f, 0.5f), 0.8f));
            if (_hull != null && _hull.Exists()) blocking.Then(new ShotStep(2800, _hull, new Vector3(-30f, 24f, 9f), _hull, new Vector3(0f, 0f, 2f), 1.2f));
            if (ice != null && ice.Exists()) blocking.Then(ShotStep.Watching(2600, ice, ice));
            if (_lift != null && _lift.Exists()) blocking.Then(new ShotStep(2800, _lift, new Vector3(-6f, 3f, 2f), _lift, new Vector3(0f, 1.5f, 1.2f), 0.6f));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "Berth 44, 03:00",
                Reason = "The Kraken at the staged mark (" + _stagedAt + "), the Titan Star over the breach, Ice on the pier with the launches in view, Ron in the lift at the salt hangar checking it over. One clock, already running; nobody gets a second briefing.",
                Blocking = blocking
            };
            if (!Ctx.Cutscenes.Play(spec)) Logger.Warn("M19 approach scene did not play; the channel stands on its own.");
        }

        /// <summary>A clamp set: a real float at the site, seen under the keel.</summary>
        private void FitFloat(int site)
        {
            if (site < 0 || site >= _clamps.Count) return;
            var model = new Model("prop_buoy_01");
            if (!GameUtils.RequestModel(model)) return;
            var floatPoint = _submergedCargo + _hullBearing * (site == 0 ? -3f : 3f) + new Vector3(0f, 0f, 2f);
            var f = Track(World.CreateProp(model, floatPoint, false, false));
            model.MarkAsNoLongerNeeded();
            if (f == null || !f.Exists()) throw new InvalidOperationException("A ballast float could not be created.");
            f.IsPersistent = true;
            f.IsPositionFrozen = true;
            _floats.Add(f);
            GameUtils.Subtitle("~y~Float " + _floats.Count + " of " + _clamps.Count + " clamped.", 2500);
        }

        /// <summary>
        /// The container becoming recoverable: an insert over the dark water, then the
        /// container on the surface beside the mark with the floats on it. The
        /// player's finished clamps cause it; the buoyancy is not simulated.
        /// </summary>
        private void PlayFloat()
        {
            if (_container == null || !_container.Exists() || _floats.Count != _clamps.Count)
                throw new InvalidOperationException("The container and both fitted floats are required for the lift.");
            Ctx.PortHeist?.Bind("bullion", _container);
            var blocking = new SceneBlocking()
                .Then(new ShotStep(2600, null, _breach + new Vector3(0f, 0f, KeelDepth + SubClearance + 6f), null, _breach + new Vector3(0f, 0f, 2f), 0.5f));
            if (_container != null && _container.Exists())
                blocking.Then(new ShotStep(3800, _container, new Vector3(-10f, 7f, 3.5f), _container, new Vector3(0f, 0f, 0.8f), 0.7f, SurfaceContainer));
            var spec = new SceneSpec
            {
                MissionId = Id, Phase = "float", Title = "The floats", RequiresCompletion = true,
                Reason = "Two ballast floats on a thirty-ton container: the hold that was flooding gives it up and it comes to the surface beside the mark. The lift has something to hook because the clamps are on, not because the chapter ended.",
                Blocking = blocking
            };
            var cue = Ctx.Data?.Cue("M19_S1_02_ICE");
            if (!Ctx.Cutscenes.PlayStaged(spec, new[] { cue })) { Logger.Warn("M19 float scene did not play; the container surfaces directly."); PortHeist.RequireFallback(blocking, "Surfacing the bullion"); Say("M19_S1_02_ICE"); }
        }

        private void SurfaceContainer()
        {
            if (_container == null || !_container.Exists()) throw new InvalidOperationException("The bullion container disappeared.");
            _container.Position = _containerPoint + new Vector3(0f, 0f, 0.6f);
            _container.IsPositionFrozen = true;
            for (int i = 0; i < _floats.Count; i++)
            {
                var f = _floats[i];
                if (f == null || !f.Exists()) throw new InvalidOperationException("A fitted float disappeared.");
                f.IsPositionFrozen = false;
                if (!StowPropStep.Stow(f, _container, new Vector3(i == 0 ? -3.2f : 3.2f, 0f, 1.6f)))
                    throw new InvalidOperationException("A float could not be secured to the surfaced container.");
            }
            _floated = true;
        }

        /// <summary>The aftermath: the sub at the support mark, the container floating beside it, the pier and the lift beyond.</summary>
        public override SceneBlocking OutroBlocking()
        {
            if (_container != null && _container.Exists())
                return new SceneBlocking().Then(new ShotStep(4500, _container, new Vector3(-16f, 10f, 5f), _container, new Vector3(0f, 0f, 1f), 1.4f));
            if (_kraken == null || !_kraken.Exists()) return null;
            return new SceneBlocking().Then(new ShotStep(4500, _kraken, new Vector3(-8f, 5f, 3f), _kraken, new Vector3(0f, 0f, 0.5f), 1.0f));
        }

        // ---------- world building ----------

        /// <summary>The Kraken where it was staged: M18's channel mark if that was played, otherwise the slip or the dive mark. An existing sub is taken over, not duplicated.</summary>
        private void SpawnKraken()
        {
            var model = new Model("submersible2");
            if (!GameUtils.RequestModel(model)) return;

            string staged = PortHeist.CargoAt(Ctx, "kraken");
            var point = _dive + new Vector3(0f, 0f, -3f);
            _stagedAt = "M19.DiveStart";
            if (!string.IsNullOrEmpty(staged) && Ctx.Locations.Get(staged) != null && Ctx.Locations.Get(staged).Kind == "water")
            {
                point = MarineSites.ResolveOrThrow(Ctx.Locations, staged, 6f) + new Vector3(0f, 0f, -1.4f);
                _stagedAt = staged;
            }
            var existing = PortHeist.Nearby(model, point, 30f);
            if (existing != null && !MarineSites.IsWaterborne(existing))
                throw new InvalidOperationException("The staged Kraken is not in usable water. Re-stage it before starting the heist.");
            _kraken = Track(existing ?? World.CreateVehicle(model, point, 180f));
            model.MarkAsNoLongerNeeded();
            if (_kraken == null || !_kraken.Exists()) return;
            Logger.Info("M19: Kraken " + (existing != null ? "taken over" : "spawned") + " at " + _stagedAt + ".");

            _kraken.IsPersistent = true;

            var blip = Track(_kraken.AddBlip());
            blip.Sprite = BlipSprite.Boat;
            blip.Color = BlipColor.Green;
            blip.Name = "Kraken";
        }

        /// <summary>Ron's lift in the salt hangar, engine off: the aircraft M18 parked, or one where it would be. M20 flies this one.</summary>
        private void SpawnLift()
        {
            var model = new Model("cargobob");
            if (!GameUtils.RequestModel(model)) return;
            var hangar = Ctx.Locations.Position("M18.SaltHangar");
            var existing = PortHeist.Nearby(model, hangar, 60f);
            _lift = Track(existing ?? World.CreateVehicle(model, hangar + new Vector3(18f, 0f, 0f), Ctx.Locations.Heading("M18.SaltHangar")));
            model.MarkAsNoLongerNeeded();
            if (_lift == null || !_lift.Exists()) return;
            _lift.IsPersistent = true;
            _lift.IsEngineRunning = false;
        }

        /// <summary>The container is visible at depth before the work starts; the verified float action brings that same object up.</summary>
        private void SpawnContainer()
        {
            var model = new Model("prop_container_01a");
            if (!GameUtils.RequestModel(model)) return;
            _container = Track(World.CreateProp(model, _submergedCargo, false, false));
            model.MarkAsNoLongerNeeded();
            if (_container == null || !_container.Exists()) return;
            _container.IsPersistent = true;
            _container.Heading = _hull == null ? 0f : _hull.Heading;
            _container.IsPositionFrozen = true;
            var blip = Track(_container.AddBlip());
            blip.Sprite = BlipSprite.Standard;
            blip.Color = BlipColor.Yellow;
            blip.Name = "Bullion container";
        }

        protected override void OnCleanup()
        {
            Ctx.Crew.CompanionsHoldPosition = false;
            _floats.Clear();
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
