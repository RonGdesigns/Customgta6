using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M49 — "Return to the Concrete". The Chumash county line, 21:00, torrential rain.
    ///
    /// Chumash is the second barricade, not the first: M48 broke the cove cordon and said
    /// so, and this road is still closed. Ice kills the spotlight generators, Gohan buys
    /// ninety seconds from dispatch, and the lane opens because the defense lost rather
    /// than because a Granger is harder than concrete.
    ///
    /// The ram is deliberately gone. CAMPAIGN-REMAINDER refuses it — "avoid requiring a
    /// 90-mph collision against an indestructible prop" — and a barricade that only yields
    /// to a crash is a barricade the player cannot fail at honestly.
    ///
    /// Everything about the roadblock is derived, not authored. Roads are baked terrain, so
    /// no placed-entity survey can find the Great Ocean Highway: M49.Checkpoint is a seed,
    /// the real lane comes from the nearest vehicle node at runtime, and the barriers, the
    /// APCs, the towers and Ice's lookout are all offsets from that lane and its heading.
    /// Get the seed roughly right and the checkpoint builds itself across the actual road;
    /// author them all separately and every one of them is a guess that can be wrong on its
    /// own. See docs/ACT3-OPENING-MAP-M49-M53.md.
    /// </summary>
    public sealed class M49ReturnToTheConcrete : PreparationOperation
    {
        public const string ApcModel = "insurgent";
        public const string BarrierModel = "prop_barier_conc_02a";
        public const string TowerModel = "prop_generator_03b";
        /// <summary>How far from the seed a real road node is accepted.</summary>
        public const float LaneSearch = 140f;
        /// <summary>Half the width of the blocked road. The seam is what is left of it.</summary>
        public const float RoadHalfWidth = 7.5f;
        /// <summary>How far back up the road the crew holds before the towers go down.</summary>
        public const float HoldBack = 130f;
        /// <summary>How long Gohan's scrambler holds dispatch off. The authored line says ninety seconds.</summary>
        public const int JamSeconds = 6;

        private readonly List<Prop> _barriers = new List<Prop>();
        private readonly List<Prop> _towers = new List<Prop>();
        private readonly CrewBoarding _boarding = new CrewBoarding();
        private Vector3 _lane, _forward, _right;
        private bool _laneFound, _towersDown, _jammed, _through;

        public override string Id => "M49";
        public override string Title => "Return to the Concrete";
        protected override MissionEndpoint Endpoint => MissionEndpoint.EscapeCheckpoint;

        public Vehicle Granger => CrewCar;
        /// <summary>A real road node was found, so the checkpoint is across an actual lane.</summary>
        public bool LaneFound => _laneFound;
        public IReadOnlyList<Prop> Towers => _towers;
        public bool TowersDown => _towersDown;
        public bool Jammed => _jammed;
        public bool Through => _through;
        /// <summary>The lane the checkpoint was built across.</summary>
        public Vector3 Lane => _lane;
        /// <summary>The drivable seam the crew goes through, once the towers are out.</summary>
        public Vector3 Seam => _lane + _right * (RoadHalfWidth * 0.6f);

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Ice)) return false;

            ResolveLane();
            BuildCheckpoint();

            CrewCar = CrewTransport("M49.Crew");
            if (!RequireAssets(CrewCar)) return false;

            Establish("approach", "The second line",
                "The cove cordon is behind them and this road is still closed: concrete, two armored cars and two spotlight towers. Ice takes the towers from the shoulder, Gohan jams the dispatch, and the seam opens because the towers went out.",
                CrewCar);
            return true;
        }

        /// <summary>
        /// Where the road actually is. The seed is an estimate on the coast highway; this
        /// asks the game for the nearest vehicle node and takes its heading, so the
        /// barricade is laid across a real lane rather than across an authored guess. A
        /// failure is reported and the seed is used, because a checkpoint slightly off the
        /// road is recoverable and a refused mission is not.
        /// </summary>
        private void ResolveLane()
        {
            var seed = At("M49.Checkpoint");
            float heading = Ctx.Locations.Heading("M49.Checkpoint");
            if (GameUtils.NearestRoadNode(seed, LaneSearch, out var node, out float nodeHeading))
            {
                _lane = node; heading = nodeHeading; _laneFound = true;
                Logger.Info(Id + ": the checkpoint lane resolved to " + node + " heading " + heading.ToString("0") +
                            ", " + (int)node.DistanceTo(seed) + " m from the seed.");
            }
            else
            {
                _lane = seed;
                Logger.Warn(Id + ": no road node within " + LaneSearch + " m of " + seed +
                            "; the checkpoint is being built on the authored seed. Resurvey M49.Checkpoint on the highway.");
                Ctx.Doctor?.Warn("placement", "M49.Checkpoint", "no vehicle node near this point; the barricade may not be across the road");
            }
            double radians = heading * Math.PI / 180.0;
            _forward = new Vector3(-(float)Math.Sin(radians), (float)Math.Cos(radians), 0f);
            _right = new Vector3(_forward.Y, -_forward.X, 0f);
        }

        /// <summary>
        /// Concrete across the lane with one gap, an armored car behind each half, and the
        /// two spotlight towers set back off the shoulder where a rifle can reach them.
        /// </summary>
        private void BuildCheckpoint()
        {
            // Five blocks across, and the sixth position left open: that gap is the seam the
            // authored dialogue calls out, and it exists before Ron arrives rather than being
            // punched through something solid.
            for (int i = -2; i <= 2; i++)
            {
                if (i == 1) continue;
                var at = _lane + _right * (i * 3.2f);
                var block = WorkProp(BarrierModel, GameUtils.OnGround(at), false);
                if (block != null) _barriers.Add(block);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                var towerAt = _lane + _forward * 14f + _right * (RoadHalfWidth + 4f) * side;
                var tower = WorkProp(TowerModel, GameUtils.OnGround(towerAt), false);
                if (tower != null) _towers.Add(tower);
                else Logger.Warn(Id + ": a spotlight generator could not be placed at " + towerAt + ".");

                var apc = Car(ApcModel, GameUtils.OnGround(_lane + _forward * 24f + _right * (5f * side)),
                    Ctx.Locations.Heading("M49.Checkpoint") + 180f, false);
                if (apc != null) apc.IsPersistent = true;
            }

            // The detail stands at the barricade. Awareness owns them; nobody is told to go
            // looking for a target across a rain-soaked highway.
            for (int i = -1; i <= 1; i++)
            {
                var post = GameUtils.OnGround(_lane + _forward * 8f + _right * (i * 5f));
                var guard = Guard(post);
                if (guard != null) { Opposition.Add(guard); Track(guard); }
            }
        }

        private bool TowersDestroyed => _towers.Count > 0 &&
            _towers.All(t => t == null || !t.Exists() || t.IsDead);

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Come up on the line",
                new TravelObjective("Guess: bring the Granger up short of the checkpoint", () => _lane - _forward * HoldBack, 25f, () => CrewCar))
                .OwnedBy(CrewSlot.Guess)
                .AfterCues("M49_S1_01_GUESS");

            // Parallel: Ice shoots, Gohan jams, and the player takes whichever he wants.
            var towers = new ConditionObjective("Ice: put out both spotlight generators from the shoulder",
                () => TowersDestroyed)
            {
                RequiredCharacter = CrewSlot.Ice,
                Marker = () => _towers.FirstOrDefault(t => t != null && t.Exists() && !t.IsDead)?.Position ?? _lane,
                MarkerRadius = 4f
            };
            var jam = new MissionInteraction("Gohan: point the scrambler at the checkpoint and hold it",
                () => _lane - _forward * HoldBack, JamSeconds, 22f, animation: MissionInteraction.ReachInside)
            { RequiredCharacter = CrewSlot.Gohan };

            yield return new MissionStage("Take the towers and the radio", towers, jam)
                .OnExit(c => { _towersDown = true; _jammed = true; Fighting = true; })
                .AfterCues("M49_S1_02_ICE", "M49_S1_03_GOHAN");

            yield return new MissionStage("Everyone in the Granger",
                new EnterVehicleObjective("All three: get into the Granger before the run", () => CrewCar, VehicleSeat.Driver, true),
                new ConditionObjective("Nobody is left on the shoulder", () => Aboard))
                .AnyOf()
                .OnEnter(c => _boarding.Reset());

            yield return new MissionStage("Run the seam",
                new TravelObjective("Guess: take the Granger through the gap in the concrete", () => Seam, 12f, () => CrewCar))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(c => _through = true);

            yield return new MissionStage("Into the county",
                new TravelObjective("Drive north past the county line", () => At("M49.South"), 30f, () => CrewCar))
                .AnyOf();
        }

        private bool Aboard => CrewCar != null && CrewCar.Exists() &&
            Protagonist.All.All(hero => Ctx.Crew.PedFor(hero.Slot)?.IsInVehicle(CrewCar) == true);

        protected override void OnUpdate()
        {
            // Ice comes down off the shoulder rather than being left at his firing position,
            // which is the bug M47 and M48 both had before CrewBoarding existed.
            if (_towersDown && !_through && !Aboard)
                _boarding.Update(Ctx.Crew, CrewCar, CrewBoarding.Passengers(CrewSlot.Guess), Id);
            base.OnUpdate();
        }

        protected override void OnPassed()
        {
            if (!_towersDown || !_jammed || !_through)
                throw new InvalidOperationException("The towers, the jam and the run through the seam all have to have happened.");
            Release(CrewCar);
        }
    }
}
