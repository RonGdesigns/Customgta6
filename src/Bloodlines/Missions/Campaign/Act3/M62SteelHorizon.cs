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
    /// M62 — "Steel Horizon". The coastal rail crossing at the port basin.
    ///
    /// Aegis is moving its last physical reserves by rail. Ice works the flatbeds, Gohan runs
    /// a boat alongside, Guess brings the Annihilator in for the container.
    ///
    /// **Read this one before changing it: it is the least verifiable mission in the campaign.**
    /// Everything else in Act III stands on placed geometry that can be checked offline. A
    /// train cannot be. What is verified is the line it stands on:
    /// <c>po1_05_railriver1</c>, <c>_2</c> and <c>_3</c> at (439.07, -2737.60, 2.76),
    /// (440.95, -2646.64, 2.27) and (439.53, -2504.07, 2.58) — a rail bridge running north to
    /// south at x ≈ 440, over water, for two hundred and thirty meters. That is coastal rail
    /// with open water beside it and arches under it, which is what the three authored lines
    /// each need: flatbeds to fight along, sixty yards of water for the boat, and arches for
    /// Guess to drop a sling between.
    ///
    /// **The bible says the Pacific Coast Highway and this is the port crossing.** The PCH rail
    /// has no placed entities to survey from; this does. Recorded in
    /// `data/mission_gameplay.tsv`.
    ///
    /// **The train is created and then left standing.** `CREATE_MISSION_TRAIN` puts a real
    /// consist on the rails, and a real consist moves along them on the engine's terms rather
    /// than a mission's. Since nobody can test that here, the mission asks for the train, and
    /// if the native gives nothing back it falls through to ordinary <c>freight</c> and
    /// <c>freightcar</c> vehicles standing on the bridge — Aegis loading rather than Aegis
    /// leaving. Either way the fight happens on the flatbeds. **This is the mission to play
    /// first**: if the consist misbehaves, the fallback is one boolean away.
    /// </summary>
    public sealed class M62SteelHorizon : PreparationOperation
    {
        public const string EngineModel = "freight";
        public const string CarModel = "freightcar";
        public const string BoatModel = "dinghy";
        public const string ChopperModel = "annihilator";
        /// <summary>CREATE_MISSION_TRAIN. Real rails, real consist, the engine's own physics.</summary>
        private const ulong CreateMissionTrain = 0x63C6CCA8E68AE8C8UL;
        /// <summary>Guards riding the flatbeds.</summary>
        public const int Riders = 5;
        /// <summary>How long the container sling takes to hook.</summary>
        public const int HookSeconds = 8;
        /// <summary>How near the consist the boat has to hold. Sixty yards, as the line says.</summary>
        public const float BoatStandoff = 60f;
        /// <summary>Where the campaign records the reserves were taken.</summary>
        public const string ReservesEvidence = "aegisReserves";

        /// <summary>
        /// Every model a freight variation can put on the rails. CREATE_MISSION_TRAIN builds the
        /// consist from these and gives nothing back - or worse - when they are not loaded,
        /// and nothing used to load them.
        /// </summary>
        public static readonly string[] TrainModels =
            { "freight", "freightcar", "freightgrain", "freightcont1", "freightcont2", "tankercar", "freighttrailer" };
        /// <summary>How far from the crossing a mission train may land before it is somebody else's railway.</summary>
        public const float TrainLandingMeters = 60f;
        /// <summary>How far above and below the bridge keys the deck is looked for. The water is under it, and a probe must never answer with the seabed.</summary>
        public const float DeckHeadroom = 4f;
        public const float DeckFloor = 0.5f;
        /// <summary>
        /// Bumper-to-bumper lengths of the standing freight, from the archive dimensions in
        /// build/vehicles.json: <c>freight</c> runs from -8.58 to 8.85 m along its length and
        /// <c>freightcar</c> from -9.24 to 9.19 m. Constants rather than a read of the model,
        /// because a model the spawn helper has already released can report no size at all.
        /// </summary>
        public const float EngineLength = 17.42f;
        public const float CarLength = 18.43f;
        /// <summary>The space left between two coupled cars.</summary>
        public const float CouplingGap = 0.5f;
        /// <summary>Freight cars coupled behind the engine when no mission train comes back.</summary>
        public const int FallbackCars = 2;

        private readonly List<Vehicle> _consist = new List<Vehicle>();
        private readonly List<Ped> _riders = new List<Ped>();
        private readonly AircraftHold _hold = new AircraftHold();
        private Vehicle _boat, _chopper;
        private float _deckOffset;
        private bool _realTrain, _boarded, _ridersDown, _hooked;

        public override string Id => "M62";
        public override string Title => "Steel Horizon";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;

        /// <summary>Whether the engine gave back a real mission train or the fallback stood in.</summary>
        public bool RealTrain => _realTrain;
        /// <summary>Somebody is on the flatbeds.</summary>
        public bool Boarded => _boarded;
        /// <summary>The escort riding the consist is down.</summary>
        public bool RidersDown => _ridersDown;
        /// <summary>The container is on the sling.</summary>
        public bool Hooked => _hooked;
        public IReadOnlyList<Vehicle> Consist => _consist;
        public IReadOnlyList<Ped> Escort => _riders;
        public Vehicle Boat => _boat;
        public Vehicle Chopper => _chopper;

        /// <summary>
        /// Everything that stands on the rail bridge. Ground preparation asks the engine for
        /// walkable ground, and on a bridge over the basin its honest answer is none at all:
        /// Ron's log on September 22, "No walkable mission surface: M62.Container", and the
        /// mission refused to start. These keep their authored heights and are put on the deck
        /// by one probe below.
        /// </summary>
        protected override string[] FixedSurfaces =>
            new[] { "M62.Consist", "M62.Flat1", "M62.Flat2", "M62.Container" }
                .Concat(Enumerable.Range(1, Riders).Select(i => "M62.Rider" + i)).ToArray();

        /// <summary>An authored bridge point, moved onto the deck the probe actually found.</summary>
        private Vector3 Deck(string key) => At(key) + new Vector3(0f, 0f, _deckOffset);

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Ice)) return false;

            if (!MissionSites.Water(Ctx.Locations, "M62.Boat"))
            {
                GameUtils.Notify("~r~The water beside the rail crossing did not check out. See Bloodlines.log.");
                return false;
            }

            // The bridge's section origins are its datum, not its deck, and every bridge key
            // shares it - one probe at the container moves them all. The crew is standing on
            // the bank fifteen meters away, so the bridge's collision is loaded here. The floor
            // is above the water: nothing under the deck but the basin, and the seabed is not
            // somewhere to stand a man.
            var authored = At("M62.Container");
            _deckOffset = MissionSites.OffsetToSurface(authored, DeckHeadroom, DeckFloor, Id + " rail bridge deck");
            Logger.Info(Id + ": the bridge deck is " + (authored.Z + _deckOffset).ToString("0.00") +
                " against the authored " + authored.Z.ToString("0.00") + "; every bridge point moves with it.");

            BuildConsist();
            if (_consist.Count == 0)
            {
                Logger.Error(Id + ": neither a mission train nor the fallback consist could be created.");
                GameUtils.Notify("~r~The freight consist could not be placed. See Bloodlines.log.");
                return false;
            }

            _boat = Car(BoatModel, MarineSites.ResolveOrThrow(Ctx.Locations, "M62.Boat", 1.5f),
                Ctx.Locations.Heading("M62.Boat"), false);
            _chopper = Car(ChopperModel, At("M62.Chopper"), Ctx.Locations.Heading("M62.Chopper"), false);
            if (!RequireAssets(_boat, _chopper)) return false;
            _boat.IsPersistent = true;
            _chopper.IsPersistent = true;
            if (!CrewTheChopper()) return false;

            // On the deck, not the shore. Enemy(key) asks for walkable ground, and beside a
            // bridge over water that answer is the bank: five men with rifles standing on the
            // mud instead of riding the flatbeds.
            for (int i = 1; i <= Riders; i++)
            {
                var ped = EnemyAt(Deck("M62.Rider" + i), "M62.Rider" + i);
                if (ped != null) _riders.Add(ped);
            }
            if (_riders.Count == 0)
                Logger.Warn(Id + ": no escort could be placed on the flatbeds; the consist is unguarded.");

            Establish("approach", "Everything they have left, on rails",
                "The last of the Aegis reserves is going out by rail over the basin. Ice takes the flatbeds, Gohan runs the boat alongside, Guess brings the Annihilator in under the arches for the container.",
                _boat, _chopper);
            return true;
        }

        /// <summary>
        /// A real consist if the engine will give one, and honest scenery if it will not.
        ///
        /// CREATE_MISSION_TRAIN is the only way to put a train on the actual rails. Nobody can
        /// verify its behavior from here, so a null return is not a failure: ordinary freight
        /// vehicles standing on the bridge make this Aegis loading rather than Aegis leaving,
        /// and every objective in the mission still works.
        /// </summary>
        private void BuildConsist()
        {
            var at = Deck("M62.Consist");
            var models = TrainModels.Select(name => new Model(name)).ToList();
            try
            {
                // The carriages first: the native builds the consist out of whatever the
                // variation names and does not load a single one of them itself.
                bool loaded = models.All(model => GameUtils.RequestModel(model));
                if (!loaded) Logger.Warn(Id + ": not every freight model loaded; the mission train is not asked for.");
                var train = loaded ? Function.Call<Vehicle>((Hash)CreateMissionTrain, 3, at.X, at.Y, at.Z, true) : null;
                if (train != null && train.Exists())
                {
                    Track(train);
                    // The native snaps to the nearest track, which is not necessarily this one.
                    // A consist that landed on another line is a fight somewhere else.
                    if (GameUtils.IsWithinFlat(train.Position, at, TrainLandingMeters))
                    {
                        train.IsPersistent = true;
                        _consist.Add(train);
                        _realTrain = true;
                        Logger.Info(Id + ": a mission train is on the rails at " + train.Position + ".");
                        return;
                    }
                    Logger.Warn(Id + ": the mission train landed at " + train.Position + ", " +
                        (int)train.Position.DistanceTo(at) + " m from the crossing; removing it and standing freight on the bridge.");
                    GameUtils.SafeDelete(train);
                }
            }
            catch (Exception ex) { Logger.Error(Id + ": asking for a mission train", ex); }
            finally { foreach (var model in models) model.MarkAsNoLongerNeeded(); }

            Logger.Warn(Id + ": no mission train came back; standing freight is used instead, so Aegis is loading rather than leaving.");
            // One train, coupled end to end behind the engine along the rail line. The cars
            // used to stand at the two outer bridge sections, a hundred and forty and ninety
            // meters either side of the engine, which read as three vehicles abandoned along
            // two hundred and thirty meters of track rather than a consist (Ron, September 22).
            float heading = Ctx.Locations.Heading("M62.Consist");
            var line = CoupledConsist(at, TrackForward(heading), FallbackCars);
            var engine = Car(EngineModel, line[0], heading, false);
            if (engine != null && engine.Exists()) { engine.IsPersistent = true; _consist.Add(engine); }
            for (int i = 1; i < line.Length; i++)
            {
                var car = Car(CarModel, line[i], heading, false);
                if (car == null || !car.Exists()) continue;
                car.IsPersistent = true;
                _consist.Add(car);
            }
            Logger.Info(Id + ": the standing consist is " + _consist.Count + " vehicles coupled behind the engine at " + at + ".");
        }

        /// <summary>
        /// The rail line's direction along the bridge, read from its two outer sections
        /// (<c>M62.Flat2</c> to <c>M62.Flat1</c>) and pointed the way the engine faces, so the
        /// cars trail behind it rather than stand in front of it.
        /// </summary>
        private Vector3 TrackForward(float engineHeading)
        {
            var from = At("M62.Flat2"); var to = At("M62.Flat1");
            var along = new Vector3(to.X - from.X, to.Y - from.Y, 0f);
            double radians = engineHeading * Math.PI / 180.0;
            var facing = new Vector3((float)-Math.Sin(radians), (float)Math.Cos(radians), 0f);
            float length = along.Length();
            if (length < 1f) return facing;
            along = along * (1f / length);
            return along.X * facing.X + along.Y * facing.Y < 0f ? along * -1f : along;
        }

        /// <summary>
        /// The centers of an engine at <paramref name="head"/> and <paramref name="cars"/>
        /// freight cars coupled behind it, each touching the one before with
        /// <see cref="CouplingGap"/> between them, in a straight line against
        /// <paramref name="forward"/>. Index 0 is the engine.
        /// </summary>
        public static Vector3[] CoupledConsist(Vector3 head, Vector3 forward, int cars)
        {
            var line = new Vector3[cars + 1];
            line[0] = head;
            float back = EngineLength / 2f + CouplingGap + CarLength / 2f;
            for (int i = 1; i <= cars; i++)
            {
                line[i] = head - forward * back;
                back += CarLength + CouplingGap;
            }
            return line;
        }

        /// <summary>The consist's head, or its authored point before anything exists.</summary>
        private Vector3 ConsistPoint() =>
            _consist.Count > 0 && _consist[0] != null && _consist[0].Exists() ? _consist[0].Position : Deck("M62.Consist");

        /// <summary>
        /// Guess at the controls of the Annihilator, which is created sixty meters up. It used
        /// to be created there empty, with its rotors stopped and nobody in it, which is an
        /// aircraft that falls onto the crossing. Seated outright and verified, never asked to
        /// board; launched with its rotors at speed; held in a circuit while the player is
        /// somebody else. His role track stops, because a track that clears his tasks to stand
        /// him at a post would take the pilot's hands off the stick.
        /// </summary>
        private bool CrewTheChopper()
        {
            var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
            if (guess == null || !guess.Exists())
            {
                Logger.Error(Id + ": Guess is not there to fly the Annihilator.");
                GameUtils.Notify("~r~The Annihilator has no pilot. See Bloodlines.log.");
                return false;
            }
            AircraftHold.LaunchAirborne(_chopper);
            Roles?.For(CrewSlot.Guess).Stop();
            guess.Task.ClearAllImmediately();
            guess.SetIntoVehicle(_chopper, VehicleSeat.Driver);
            if (_chopper.GetPedOnSeat(VehicleSeat.Driver) != guess)
            {
                Logger.Error(Id + ": Guess could not be seated in the Annihilator.");
                GameUtils.Notify("~r~The Annihilator could not be crewed. Restart the mission.");
                return false;
            }
            Logger.Info(Id + ": Guess is flying the Annihilator over the crossing.");
            return true;
        }

        protected override void OnUpdate()
        {
            // Every frame; the hold paces its own orders and lets go the moment the player
            // takes the stick.
            _hold.Update(Ctx.Crew, CrewSlot.Guess, _chopper, At("M62.Chopper"), (int)At("M62.Chopper").Z);
            base.OnUpdate();
        }

        protected override void OnCleanup()
        {
            _hold.Release();
            base.OnCleanup();
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Get onto the flatbeds",
                new ReachZoneObjective("Ice: get onto the freight consist", ConsistPoint, 12f))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(c => Fighting = true)
                .OnExit(c => _boarded = true)
                .AfterCues("M62_S1_01_ICE");

            yield return new MissionStage("Take the escort",
                new KillTargetsObjective("Take the escort riding the consist", () => _riders))
                .AnyBrother()
                .OnExit(c => _ridersDown = true)
                .AfterCues("M62_S1_02_GOHAN");

            yield return new MissionStage("Hook the container",
                new MissionInteraction("Hook the cargo container onto the chopper sling",
                    () => Deck("M62.Container"), HookSeconds, 3f, animation: MissionInteraction.Repair))
                .AnyBrother()
                .OnExit(c => Hooking())
                .AfterCues("M62_S1_03_GUESS");
        }

        private void Hooking()
        {
            _hooked = true;
            Ctx.State?.SetEvidence(ReservesEvidence, EvidenceState.CopyHeld);
            Logger.Info(Id + ": the reserves container is off the consist.");
        }

        protected override void OnPassed()
        {
            if (!_boarded || !_ridersDown || !_hooked)
                throw new InvalidOperationException("The flatbeds, the escort and the container all have to be taken.");
            Release(_boat);
            Release(_chopper);
        }
    }
}
