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
    /// M63 — "Tower of Glass". The Maze Bank plaza, 21:00. The finale begins.
    ///
    /// Guess runs the technical at the tower entrance, Ice takes the contractor gun nests off
    /// the plaza, and Gohan bypasses the security core so the ascent can start.
    ///
    /// The plaza is <c>dt1_11_dt1_plaza</c> at (-76.6, -825.6, 36.77) and the way in is the
    /// carpark portal at (-84.13, -821.35, 36.71) — the tower's real vehicle entrance. That
    /// deck is **seven meters above the street around it**, which is the whole reason every key
    /// here is a fixed surface: ground preparation would ask for walkable ground and be handed
    /// the road below. One probe at the entrance measures the deck and moves the rest, the way
    /// <see cref="MissionSites.OffsetToSurface"/> describes. See <see cref="MazeBank"/> for the
    /// tower's full stack.
    ///
    /// The plaza is genuinely open — sixty-eight placed things in the whole deck band — so the
    /// gun nests are placed for firing lines rather than squeezed into gaps.
    /// </summary>
    public sealed class M63TowerOfGlass : PreparationOperation
    {
        public const string TechnicalModel = "technical";
        /// <summary>Contractor gun nests covering the plaza approach.</summary>
        public const int Nests = 3;
        /// <summary>Two men to a nest.</summary>
        public const int PerNest = 2;
        /// <summary>How close the technical has to be to count as through the doors.</summary>
        public const float BreachRadius = 8f;
        /// <summary>How long the security core takes to bypass.</summary>
        public const int CoreSeconds = 8;
        /// <summary>Where the campaign records that the tower is open.</summary>
        public const string TowerCargo = "mazeBankGroundHeld";

        private readonly List<Ped> _nestCrews = new List<Ped>();
        private Vehicle _technical;
        private float _deckOffset;
        private bool _breached, _nestsDown, _coreDown;

        public override string Id => "M63";
        public override string Title => "Tower of Glass";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;

        /// <summary>The technical is through the entrance.</summary>
        public bool Breached => _breached;
        /// <summary>Every gun nest on the plaza is down.</summary>
        public bool NestsDown => _nestsDown;
        /// <summary>The security core is bypassed and the freight lift is live.</summary>
        public bool CoreDown => _coreDown;
        public Vehicle Technical => _technical;
        public IReadOnlyList<Ped> NestCrews => _nestCrews;
        /// <summary>How far the probe moved the authored plaza height.</summary>
        public float DeckOffset => _deckOffset;

        /// <summary>
        /// The plaza deck is seven meters above the street it overlooks, so the engine's
        /// walkable query would answer with the street for any of these.
        /// </summary>
        protected override string[] FixedSurfaces =>
            new[] { "M63.Start", "M63.IceStart", "M63.GohanStart", "M63.GuessStart",
                    "M63.Technical", "M63.Doors", "M63.Core" }
                .Concat(NestKeys()).ToArray();

        private static IEnumerable<string> NestKeys()
        {
            for (int i = 1; i <= Nests; i++) yield return "M63.Nest" + i;
        }

        /// <summary>An authored plaza point, moved onto the deck the probe actually found.</summary>
        private Vector3 Deck(string key) => At(key) + new Vector3(0f, 0f, _deckOffset);

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Guess)) return false;

            var authored = At("M63.Doors");
            _deckOffset = MissionSites.OffsetToSurface(authored, MazeBank.PlazaHeadroom, MazeBank.PlazaFloor,
                Id + " plaza deck");
            Logger.Info(Id + ": the plaza deck is " + (authored.Z + _deckOffset).ToString("0.00") +
                " against the authored " + authored.Z.ToString("0.00") +
                "; every plaza point moves by " + _deckOffset.ToString("0.00") + ".");

            _technical = Car(TechnicalModel, Deck("M63.Technical"), Ctx.Locations.Heading("M63.Technical"), true);
            if (!RequireAssets(_technical)) return false;
            _technical.IsPersistent = true;
            // No RequireAsset. That contract lasts the whole mission, and once the technical is
            // through the doors it is a wreck in a gunfight nobody needs any more: losing it to
            // the nests after the breach failed the mission (Ron, September 22). Its delivery
            // objective already fails the breach if it is lost on the way in, which is the only
            // stretch it matters.

            // Two to a nest, each on his own settled point: the plaza deck is a built surface,
            // so these go in through the path that does not ask the engine for ground.
            foreach (var key in NestKeys())
            {
                // The deck offset is measured at the doors, and the nests stand up to sixty
                // meters north of them - past the edge of the raised deck, some of them, where
                // that height is seven meters of air over the street. Each post is settled on
                // whatever is actually under it.
                var post = MissionSites.OnSurface(Deck(key), MazeBank.PlazaHeadroom, MazeBank.PlazaFloor, Id + " " + key, 2);
                int men = MissionPlacement.Count(Ctx.Locations, key, PerNest);
                for (int i = 0; i < men; i++)
                {
                    // Sized from the nest key. The deck offset is already in `post`, and
                    // PointFor returns the authored offset unless the key has been edited,
                    // so an unsurveyed plaza puts the same two men in the same places.
                    var ped = EnemyAt(MissionPlacement.PointFor(Ctx.Locations, key, i,
                        post + new Vector3(i * 2.5f, i % 2 * 2f, 0f)), key);
                    if (ped != null) _nestCrews.Add(ped);
                }
            }
            if (_nestCrews.Count == 0)
            {
                Logger.Error(Id + ": no contractor could be placed on the plaza; there is nothing to suppress.");
                GameUtils.Notify("~r~The plaza defense could not be placed. See Bloodlines.log.");
                return false;
            }

            // A grenade launcher, because M63_S1_02_ICE says he clears a heavy gunner with one.
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            if (ice != null && ice.Exists()) ice.Weapons.Give(WeaponHash.GrenadeLauncher, 12, false, true);

            Paleto.Review(Ctx, PlacementContract.Ped("M63.Start"), PlacementContract.Interaction("M63.Core"),
                PlacementContract.Vehicle("M63.Technical", new Model(TechnicalModel)));

            Establish("approach", "The glass at the bottom of it",
                "Aegis Tactical runs out of the Maze Bank Tower and the lobby is barricaded. Guess puts the technical through the entrance, Ice takes the gun nests off the plaza, and Gohan bypasses the security core so the lift will run.",
                _technical);
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Put the technical through the entrance",
                new DeliverVehicleObjective("Guess: ram the technical through the tower entrance",
                    () => _technical, () => Deck("M63.Doors"), BreachRadius))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(c => { _breached = true; Fighting = true; Awareness.ReportToAll(Stimulus.RadioCall, Deck("M63.Doors")); })
                .AfterCues("M63_S1_01_GUESS");

            yield return new MissionStage("Clear the plaza",
                new KillTargetsObjective("Take the contractor gun nests off the plaza", () => _nestCrews))
                .AnyBrother()
                .OnExit(c => _nestsDown = true)
                .AfterCues("M63_S1_02_ICE");

            yield return new MissionStage("Bypass the security core",
                new MissionInteraction("Gohan: bypass the security core and release the freight lift",
                    () => Deck("M63.Core"), CoreSeconds, 2.5f, animation: MissionInteraction.Typing)
                { RequiredCharacter = CrewSlot.Gohan })
                .OnExit(c => Fortify())
                .AfterCues("M63_S1_03_GOHAN");
        }

        private void Fortify()
        {
            _coreDown = true;
            Ctx.State?.SetCargo(TowerCargo, "M63.Doors");
            Logger.Info(Id + ": the Maze Bank ground level is open and the freight lift is live.");
        }

        protected override void OnPassed()
        {
            if (!_breached || !_nestsDown || !_coreDown)
                throw new InvalidOperationException("The entrance, the plaza and the security core all have to be taken.");
            Release(_technical);
        }
    }
}
