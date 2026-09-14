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
    /// Chapter two. Guess flies Ice over the rail now that the defenses are down,
    /// Ice takes the upper deck, and Gohan brings the Kraken to the stern platform
    /// and climbs aboard. The chapter is finished when both brothers are physically
    /// on the structure — not near it, not still in a seat.
    ///
    /// The authored beat has Ice breaching an elevator shaft and Gohan surfacing in
    /// a moonpool. Neither exists on a vessel: the deck levels are connected by the
    /// interior's own stairs, and Gohan comes up the stern platform at the
    /// waterline. Both heights are read out of the archives; see PaletoSite.
    /// </summary>
    public sealed class M45PaletoBreach : ComposedMission
    {
        public const string HelicopterModel = "annihilator";
        public const string GuardModel = "s_m_y_blackops_01";
        public const int DeckGuards = 4;

        private readonly List<Ped> _guards = new List<Ped>();
        private readonly AircraftHold _hold = new AircraftHold();
        private Vehicle _chopper;
        private Vehicle _kraken;
        private bool _landed;
        private bool _aboard;

        public override string Id => "M45";
        public override string Title => "Paleto Deep-Sea: Breach";
        protected override MissionEndpoint Endpoint => MissionEndpoint.ContinuousNext;

        public Vehicle Chopper => _chopper;
        public Vehicle Kraken => _kraken;
        /// <summary>Ice is on the upper deck with the guards down.</summary>
        public bool Landed => _landed;
        /// <summary>Gohan is out of the water and on the structure.</summary>
        public bool Aboard => _aboard;
        public IReadOnlyList<Ped> Guards => _guards;

        private Vector3 At(string key) => Ctx.Locations.Position(key);

        protected override bool Setup()
        {
            var world = Paleto.Of(Ctx);
            if (world != null && !world.DefensesDown)
                throw new InvalidOperationException("M45 opened before the sea defenses were down. The operation must run from M44.");

            // Continuing: the sub and the men are where the dive left them.
            if (!Paleto.IsContinuing(Ctx) && !Ctx.Crew.Deploy(CrewSlot.Ice, At("M45.Board"), Ctx.Locations.Heading("M45.Board"))) return false;
            ApplyBibleSetting();

            _kraken = world?.Get<Vehicle>("kraken");
            if (Paleto.IsContinuing(Ctx)) _kraken = Track(world.Require<Vehicle>("kraken"));
            // Opened alone in QA there is no dive to inherit, so the sub is staged
            // where M44 would have surfaced it rather than left missing.
            else if (_kraken == null || !_kraken.Exists()) _kraken = StageSub();
            if (!SpawnHelicopter()) return false;
            world?.Bind("chopper", _chopper);
            RequireAsset(_chopper, "The extraction helicopter was lost before the boarding.");
            SpawnDeckGuards();

            // The deck and platform points are the least proven in the operation.
            Paleto.Review(Ctx, PlacementContract.Ped("M45.Helipad"), PlacementContract.Ped("M45.Board"));
            Station(CrewSlot.Guess, _chopper, VehicleSeat.Driver);
            Station(CrewSlot.Ice, _chopper, VehicleSeat.Passenger);
            if (_kraken != null && _kraken.Exists()) Station(CrewSlot.Gohan, _kraken, VehicleSeat.Driver);
            return true;
        }

        private bool SpawnHelicopter()
        {
            if (Paleto.IsContinuing(Ctx))
            {
                var carried = Paleto.Of(Ctx)?.Get<Vehicle>("chopper");
                if (carried != null && carried.Exists()) { _chopper = Track(carried); return true; }
            }
            var model = new Model(HelicopterModel);
            if (!GameUtils.RequestModel(model)) return false;
            // M43 landed it at the Paleto strip; it starts this chapter in the air on
            // the approach, which is where the authored flight begins.
            _chopper = Track(World.CreateVehicle(model, At("M45.Approach"), Ctx.Locations.Heading("M45.Approach")));
            model.MarkAsNoLongerNeeded();
            if (_chopper == null || !_chopper.Exists()) return false;
            _chopper.IsPersistent = true;
            // It is created 60 meters over open water. Rotors at speed and a little
            // airspeed, or it is in the sea before the blades spin up.
            AircraftHold.LaunchAirborne(_chopper);
            return true;
        }

        private Vehicle StageSub()
        {
            var model = new Model(M44PaletoSubSurface.SubModel);
            if (!GameUtils.RequestModel(model)) return null;
            var sub = Track(World.CreateVehicle(model, MarineSites.ResolveOrThrow(Ctx.Locations, "M44.Surface", 4f), 0f));
            model.MarkAsNoLongerNeeded();
            if (sub != null && sub.Exists()) sub.IsPersistent = true;
            return sub;
        }

        private void SpawnDeckGuards()
        {
            var model = new Model(GuardModel);
            if (!GameUtils.RequestModel(model)) return;
            var aegis = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            var pad = At("M45.Helipad");
            for (int i = 0; i < DeckGuards; i++)
            {
                var post = pad + new Vector3(-6f + i * 4f, i % 2 == 0 ? 4f : -4f, 0f);
                var guard = World.CreatePed(model, GameUtils.OnGround(post), 180f);
                if (guard == null || !guard.Exists()) continue;
                guard.RelationshipGroup = aegis;
                guard.IsPersistent = true;
                guard.BlockPermanentEvents = true;
                guard.Accuracy = 35;
                guard.Armor = 50;
                guard.Weapons.Give(WeaponHash.CarbineRifle, 200, true, true);
                guard.Task.FightAgainstHatedTargets(120f);
                _guards.Add(Track(guard));
            }
            model.MarkAsNoLongerNeeded();
        }

        private bool GuardsDown => _guards.Count == 0 || _guards.All(g => g == null || !g.Exists() || g.IsDead);

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Bring the helicopter over the rail",
                new TravelObjective("Guess: fly the Annihilator to the hold marker above the vessel", () => At("M45.Hold"), 22f, () => _chopper))
                .OwnedBy(CrewSlot.Guess)
                .AfterCues("M45_S1_02_GUESS");

            yield return new MissionStage("Put Ice on the upper deck",
                new ReachZoneObjective("Ice: get out onto the upper deck", () => At("M45.Helipad"), 5f))
                .OwnedBy(CrewSlot.Ice);

            yield return new MissionStage("Clear the upper deck",
                new KillTargetsObjective("Ice: clear the deck detail", () => _guards))
                .OwnedBy(CrewSlot.Ice)
                .OnExit(c =>
                {
                    if (!GuardsDown) throw new InvalidOperationException("The deck detail is still up.");
                    _landed = true;
                })
                .AfterCues("M45_S1_01_ICE");

            yield return new MissionStage("Bring the Kraken to the stern",
                new TravelObjective("Gohan: take the Kraken alongside the stern platform", () => At("M45.Stern"), 12f, () => _kraken))
                .OwnedBy(CrewSlot.Gohan);

            yield return new MissionStage("Get Gohan aboard",
                new ReachZoneObjective("Gohan: climb the stern platform onto the vessel", () => At("M45.Board"), 4f))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(c =>
                {
                    var gohan = c.Crew.PedFor(CrewSlot.Gohan);
                    if (gohan == null || gohan.IsInVehicle() || gohan.Position.Z < PaletoSite.WaterlineDeck)
                        throw new InvalidOperationException("Gohan is not out of the water and on the structure.");
                    _aboard = true;
                })
                .AfterCues("M45_S1_03_GOHAN");
        }

        /// <summary>
        /// Guess holds over the vessel for the whole of Ice's and Gohan's work. Without
        /// this nobody is flying it while the player is someone else, and it comes down.
        /// </summary>
        protected override void OnUpdate()
        {
            _hold.Update(Ctx.Crew, CrewSlot.Guess, _chopper, At("M45.Hold"), (int)At("M45.Hold").Z);
            base.OnUpdate();
        }

        protected override void OnPassed()
        {
            if (!_landed || !_aboard) throw new InvalidOperationException("Both brothers have to be aboard before the vault.");
            var record = OperationHandoff.Capture(Paleto.Operation.Title, Id, "M46", Ctx.Crew, _chopper);
            record.Notes["deck"] = "upper deck clear; Ice holding the stair head";
            record.Notes["gohan"] = "aboard by the stern platform, out of the sub";
            record.Notes["chopper"] = "Guess holding station off the beam";
            Ctx.Handoffs.Record(record);
            Release(_chopper);
        }
    }
}
