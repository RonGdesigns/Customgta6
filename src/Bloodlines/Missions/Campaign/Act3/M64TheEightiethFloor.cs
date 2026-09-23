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
    /// M64 — "The 80th Floor". Inside the Maze Bank Tower, 21:30.
    ///
    /// Aegis has cut the elevators. The crew goes up through a mid-tower floor —
    /// <c>imp_dt1_11_cargarage_a</c> at (-84.22, -823.09, **221.00**), a real interior inside
    /// the real building — clears the security holding it, and reaches the lift to the
    /// executive level. See <see cref="MazeBank"/> for the tower's full stack.
    ///
    /// Two honest adaptations, both in `data/mission_gameplay.tsv`:
    ///
    /// **The falling elevator cars are not reproduced.** "Dodging falling elevator cars dropped
    /// by enemies above" needs a shaft with cars in it, and no such geometry exists to drop
    /// anything down. Faking it with a spawned prop falling through an interior that has no
    /// shaft would be a set piece that does not land anywhere. `M64_S1_01_ICE` calls the
    /// falling car, so it is the one line of the three that goes unplayed.
    ///
    /// **One floor, not thirty.** The bible climbs from 50 to 80. There is one mid-tower
    /// interior between the carpark and the executive floor, so the ascent is that floor, and
    /// the mission ends at the lift with `M64_S1_03_GUESS` — "we are on the executive floor" —
    /// handing straight to M65.
    ///
    /// **Nothing inside is authored but the floor itself.** Exactly one coordinate exists for
    /// that interior, its own MLO placement, and nobody has walked it. Every position in here
    /// is an offset from where the crew actually arrives, resolved to walkable floor by
    /// <see cref="MazeBank.Nearby"/> and falling back to the arrival point. That is M54's rule,
    /// and it is why this mission cannot put a marker in the air.
    /// </summary>
    public sealed class M64TheEightiethFloor : PreparationOperation
    {
        /// <summary>Executive security holding the floor.</summary>
        public const int DefenderPosts = 6;
        /// <summary>How far from the arrival point the fight is spread.</summary>
        public const float FloorSpread = 9f;
        /// <summary>How long the lift release takes.</summary>
        public const int LiftSeconds = 6;
        /// <summary>How near the lift counts as reached.</summary>
        public const float LiftRadius = 2.5f;
        /// <summary>Where the campaign records that the executive floor is reachable.</summary>
        public const string AscentCargo = "mazeBankAscent";
        /// <summary>How long calling the freight lift takes, and how near the entrance counts.</summary>
        public const int LiftCallSeconds = 2;
        public const float DoorsRadius = 3.5f;

        private readonly List<Ped> _defenders = new List<Ped>();
        private readonly FloorEntry _entry = new FloorEntry();
        private Vector3 _arrival, _lift, _doors;
        private bool _inside, _cleared, _atLift;

        public override string Id => "M64";
        public override string Title => "The 80th Floor";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;

        /// <summary>The crew is on the mid-tower floor.</summary>
        public bool Inside => _inside;
        /// <summary>The floor's security is down.</summary>
        public bool Cleared => _cleared;
        /// <summary>They have the lift to the executive level.</summary>
        public bool AtLift => _atLift;
        public IReadOnlyList<Ped> Defenders => _defenders;

        /// <summary>
        /// The plaza start is the same raised deck M63 fought on, seven meters over the street.
        /// Nothing inside the tower is in this list, because nothing inside the tower is
        /// authored: the interior positions are found at runtime.
        /// </summary>
        protected override string[] FixedSurfaces =>
            new[] { "M64.Start", "M64.IceStart", "M64.GohanStart", "M64.GuessStart", "M64.Doors" };

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Ice)) return false;

            // The entrance is on the raised plaza deck, a fixed surface nobody has measured for
            // this mission. One probe puts the lift call on the slab; the slab is loaded, because
            // the crew is standing twenty meters from it.
            _doors = MissionSites.OnSurface(At("M64.Doors"), MazeBank.PlazaHeadroom, MazeBank.PlazaFloor, Id + " tower entrance", 3);

            Establish("approach", "Fifty floors with the power off",
                "The elevators stopped at fifty and Aegis is holding the floors above on foot. The way up is the service level, and the executive lift is on the other side of whoever is standing in it.");
            return true;
        }

        private Vector3 DoorsPoint() => _doors == Vector3.Zero ? At("M64.Doors") : _doors;

        /// <summary>
        /// The freight lift. This only asks the access service for the floor; the floor is laid
        /// out in <see cref="OnUpdate"/> once the service says the player is standing on it.
        /// Reading his position in this same call gave the plaza, and every defender and the
        /// lift were placed out there while Ice alone went up (Ron, September 22).
        /// </summary>
        private void GoUp() => _entry.Request(Ctx, MazeBank.Garage, MazeBank.GarageIpl);

        /// <summary>
        /// The floor's own layout, found rather than written down, from where the player
        /// landed. The other two come up with him: the lift is Guess's to blow and the fight
        /// is everybody's.
        /// </summary>
        private void OnTheFloor()
        {
            _arrival = _entry.Arrival;
            _lift = MazeBank.Nearby(_arrival, 0.0, FloorSpread, Id + " executive lift");
            int n = 0;
            foreach (var hero in Protagonist.All)
            {
                if (hero.Slot == Ctx.Crew.ActiveSlot) continue;
                var spot = MazeBank.BringAlongside(Ctx, hero.Slot, _arrival, 200.0 + 60.0 * n++, Id + " lift");
                if (spot.HasValue) Roles?.For(hero.Slot).Observe(spot.Value, spot.Value);
            }

            for (int i = 0; i < DefenderPosts; i++)
            {
                var post = MazeBank.Nearby(_arrival, 40.0 + i * 50.0, FloorSpread * 0.8f, Id + " defender " + (i + 1));
                var ped = EnemyAt(post, "M64 floor post " + (i + 1));
                if (ped != null) _defenders.Add(ped);
            }
            if (_defenders.Count == 0)
            {
                // An unopposed ascent is not this mission. Six posts all refusing means the
                // floor did not stream, which is a retry rather than a walk-through.
                Logger.Error(Id + ": no defender could be placed on the service floor.");
                Fail("The service floor did not load its security. Retry the mission.");
                return;
            }
            _inside = true;
            Fighting = true;
            Logger.Info(Id + ": on the service floor at " + _arrival + " with " + _defenders.Count + " defenders and the lift at " + _lift + ".");
        }

        /// <summary>The lift, or the floor's own arrival point before it has been found.
        /// Never a zero vector: a marker at the origin is a marker under the map.</summary>
        private Vector3 LiftPoint() => _lift == Vector3.Zero ? MazeBank.Garage : _lift;

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Get into the tower",
                new MissionInteraction("Call the freight lift up to the service floor", DoorsPoint, LiftCallSeconds, DoorsRadius, animation: MissionInteraction.Operate))
                .AnyBrother()
                .OnExit(c => GoUp());

            // A real wait, not a formality: the lift is the access service's fade and load,
            // and the floor is only laid out once it reports the player standing on it.
            yield return new MissionStage("Up to the service floor",
                new ConditionObjective("Riding the freight lift up to the service floor", () => _inside))
                .AnyBrother();

            // M64_S1_01_ICE calls a falling elevator car. There is no shaft to drop one down,
            // so it is not fired; the adaptation is recorded rather than faked.
            yield return new MissionStage("Clear the service floor",
                // The list is read every frame, never captured: BuildStages runs before the
                // crew is upstairs, so anything counted here would be counted at zero.
                new KillTargetsObjective("Take the executive security holding the floor", () => _defenders))
                .AnyBrother()
                .OnExit(c => _cleared = true)
                .AfterCues("M64_S1_02_GOHAN");

            // Guess by name, because M64_S1_03_GUESS is the man who blows the door: "Door blown
            // open! We are on the executive floor!"
            yield return new MissionStage("Take the executive lift",
                new MissionInteraction("Guess: blow the executive lift open", LiftPoint,
                    LiftSeconds, LiftRadius, animation: MissionInteraction.ReachInside))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(c => Reached())
                .AfterCues("M64_S1_03_GUESS");
        }

        private void Reached()
        {
            _atLift = true;
            Ctx.State?.SetCargo(AscentCargo, "M64.Doors");
            Logger.Info(Id + ": the executive floor is reachable.");
        }

        protected override void OnUpdate()
        {
            if (_entry.Update(Ctx)) OnTheFloor();
            if (_entry.Refused) { Fail(_entry.Failure); return; }
            if (Status != MissionStatus.Running) return;
            base.OnUpdate();
        }

        protected override void OnCleanup()
        {
            _entry.Release(Ctx, Protagonist.All.Select(h => Ctx.Crew.PedFor(h.Slot)));
            base.OnCleanup();
        }

        protected override void OnPassed()
        {
            if (!_inside || !_cleared || !_atLift)
                throw new InvalidOperationException("The tower has to be entered, the floor cleared and the lift taken.");
        }
    }
}
