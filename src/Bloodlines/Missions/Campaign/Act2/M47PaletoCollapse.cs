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
    /// Chapter four. Gohan triggers what he clamped, the two of them go off the side,
    /// and Guess — who has put the helicopter down and taken the boat out, the way
    /// the road-back plan says — picks them out of the water and clears the area.
    ///
    /// The authored beat has the whole rig tilting forty degrees and sinking. A
    /// stationary structure with staged damage is what is actually supported: the
    /// charges go off at the mooring points that were clamped in M44, the structure
    /// burns and is finished, and nobody is asked to run across geometry that is
    /// rotating under them. That was recorded as decision D04 before this was built.
    /// </summary>
    public sealed class M47PaletoCollapse : ComposedMission
    {
        public const string BoatModel = "tropic";
        /// <summary>How far from the structure counts as out of the demolition area.</summary>
        public const float ClearRange = 120f;

        private Vehicle _boat;
        private Vehicle _chopper;
        private bool _triggered;
        private bool _jumped;

        public override string Id => "M47";
        public override string Title => "Paleto Deep-Sea: Collapse";
        protected override MissionEndpoint Endpoint => MissionEndpoint.ContinuousNext;

        public Vehicle Boat => _boat;
        public bool Triggered => _triggered;
        /// <summary>Both brothers actually went into the water rather than being teleported off.</summary>
        public bool Jumped => _jumped;

        private Vector3 At(string key) => Ctx.Locations.Position(key);

        protected override bool Setup()
        {
            var world = Paleto.Of(Ctx);
            if (world != null && (!world.ChargesArmed || !world.EvidenceHeld))
                throw new InvalidOperationException("M47 opened before the charges were armed and the evidence taken.");
            if (!Paleto.IsContinuing(Ctx) && !Ctx.Crew.Deploy(CrewSlot.Gohan, At("M47.Jump"), Ctx.Locations.Heading("M47.Jump"))) return false;

            _chopper = world?.Get<Vehicle>("chopper");
            // Opened alone there is no helicopter to inherit: stage one on the strip
            // Guess is being asked to put it down on.
            if (_chopper == null || !_chopper.Exists()) _chopper = StageHelicopter();
            if (!SpawnBoat()) return false;
            Paleto.Review(Ctx, PlacementContract.Ped("M47.Jump"), PlacementContract.Interaction("M47.Trigger"),
                PlacementContract.Aircraft("M47.Landing", new Model(M45PaletoBreach.HelicopterModel), 30f));
            world?.Bind("boat", _boat);
            RequireAsset(_boat, "The pickup boat was lost. Nobody comes off the structure without it.");
            return true;
        }

        private bool SpawnBoat()
        {
            var model = new Model(BoatModel);
            if (!GameUtils.RequestModel(model)) return false;
            // M43 left the Tropic on its holding marker; it is taken from there, not
            // conjured beside the swimmers at the moment they need it.
            // M40 records this under the plural key it actually writes.
            string staged = Paleto.CargoAt(Ctx, "extractionLaunches");
            string key = !string.IsNullOrEmpty(staged) && Ctx.Locations.Get(staged)?.Kind == "water" ? staged : "M47.BoatStart";
            var point = MarineSites.ResolveOrThrow(Ctx.Locations, key, 2f);
            _boat = Track(World.CreateVehicle(model, point, Ctx.Locations.Heading(key)));
            model.MarkAsNoLongerNeeded();
            if (_boat == null || !_boat.Exists()) return false;
            _boat.IsPersistent = true;
            return true;
        }

        private Vehicle StageHelicopter()
        {
            var model = new Model(M45PaletoBreach.HelicopterModel);
            if (!GameUtils.RequestModel(model)) return null;
            var heli = Track(World.CreateVehicle(model, At("M45.Hold"), Ctx.Locations.Heading("M45.Hold")));
            model.MarkAsNoLongerNeeded();
            if (heli == null || !heli.Exists()) return null;
            heli.IsPersistent = true;
            // Staged at the hold point, which is 40 meters up: same reason as M45.
            AircraftHold.LaunchAirborne(heli);
            return heli;
        }

        private void FireCharges()
        {
            _triggered = true;
            foreach (var key in new[] { "M44.Clamp1", "M44.Clamp2", "M44.Clamp3" })
            {
                var point = At(key);
                World.AddExplosion(new Vector3(point.X, point.Y, PaletoSite.Keel), ExplosionType.Boat, 4f, 1.4f, null, true, false);
            }
            Logger.Info("Paleto: charges fired at the clamped mooring points. The structure stays where it is and burns.");
        }

        private bool InWater(CrewSlot slot)
        {
            var ped = Ctx.Crew.PedFor(slot);
            return ped != null && ped.Exists() && !ped.IsDead && !ped.IsInVehicle() && ped.Position.Z < PaletoSite.WaterlineDeck;
        }

        private bool Recovered =>
            _boat != null && _boat.Exists() && _boat.IsDriveable &&
            Protagonist.All.All(hero => Ctx.Crew.PedFor(hero.Slot)?.IsInVehicle(_boat) == true);

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Put the helicopter down and take the boat",
                new DeliverVehicleObjective("Guess: land the Annihilator on the cove strip", () => _chopper, () => At("M47.Landing"), 6f, true),
                new EnterVehicleObjective("Guess: take the Tropic out to the pickup marker", () => _boat, VehicleSeat.Driver))
                .OwnedBy(CrewSlot.Guess);

            yield return new MissionStage("Trigger the charges",
                new MissionInteraction("Gohan: trigger the charges from the rail", () => At("M47.Trigger"), 3, 3f,
                    animation: MissionInteraction.ReachInside))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(c => FireCharges())
                .AfterCues("M47_S1_01_GOHAN");

            yield return new MissionStage("Go off the side",
                new ConditionObjective("Ice and Gohan: get to the edge and go into the water", () => InWater(CrewSlot.Ice) && InWater(CrewSlot.Gohan)))
                .AnyOf()
                .OnExit(c => _jumped = true)
                .AfterCues("M47_S1_02_ICE");

            yield return new MissionStage("Pick them up",
                new ConditionObjective("Guess: bring the boat onto both swimmers until all three are aboard", () => Recovered))
                .OwnedBy(CrewSlot.Guess)
                .AfterCues("M47_S1_03_GUESS");

            yield return new MissionStage("Clear the demolition area",
                new TravelObjective("Guess: take the boat clear of the burning vessel", () => At("M47.Clear"), 20f, () => _boat))
                .OwnedBy(CrewSlot.Guess);
        }

        protected override void OnPassed()
        {
            if (!_triggered || !_jumped) throw new InvalidOperationException("The charges and the jump both have to have happened.");
            if (!Recovered) throw new InvalidOperationException("All three have to be in the boat.");
            var record = OperationHandoff.Capture(Paleto.Operation.Title, Id, "M48", Ctx.Crew, _boat);
            record.Notes["boat"] = "all three aboard, clear of the demolition area";
            record.Notes["structure"] = "burning at its moorings; nobody left on it";
            Ctx.Handoffs.Record(record);
            Release(_boat);
        }
    }
}
