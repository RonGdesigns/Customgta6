using System;
using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
namespace Bloodlines.Missions.Campaign
{
    public sealed class M42SkyfallDelivery : CoastalOperation
    {
        public override string Id => "M42";
        public override string Title => "Skyfall Delivery";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;
        public Vehicle Titan { get; private set; }
        public Vehicle CargoSub => Sub;
        public SubmarineAirdrop Drop { get; private set; }
        private Vector3 _water;
        private bool _flightStarted;
        private int _planeOrders;
        /// <summary>
        /// Why the release could not happen. It used to throw from inside the corridor
        /// check, which is an objective's condition, and a throw there ends the attempt as a
        /// "Script error"; the reason is kept and fails it on the next frame instead
        /// (the September 22 audit).
        /// </summary>
        private string _fault;
        protected override bool Setup()
        {
            _fault = null;
            if (!BeginCrew(CrewSlot.Guess)) return false;
            _water = MarineSites.ResolveOrThrow(Ctx.Locations, "M42.Drop", 6f, 4f, 7f, 0);
            MarineSites.ResolveOrThrow(Ctx.Locations, "M42.Delivery", 3f, 3.5f, 5f, 0);
            Titan = Car("titan", At("M42.Titan"), Ctx.Locations.Heading("M42.Titan"));
            Sub = Car("submersible2", At("M42.Titan") + new Vector3(0, 0, -5f), Ctx.Locations.Heading("M42.Titan"));
            if (!RequireAssets(Titan, Sub)) return false;
            RequireAsset(Titan, "The Titan was lost before the crew completed the delivery."); RequireAsset(Sub, "The cargo submarine was destroyed.");
            Titan.IsEngineRunning = true; Titan.IsPositionFrozen = true;
            Station(CrewSlot.Guess, Titan, VehicleSeat.Driver); Station(CrewSlot.Gohan, Sub, VehicleSeat.Driver);
            Roles.For(CrewSlot.Guess).Stop(); Roles.For(CrewSlot.Gohan).Stop();
            Drop = new SubmarineAirdrop(Titan, Sub, Ctx.Crew.PedFor(CrewSlot.Gohan));
            if (!Drop.Secure()) throw new InvalidOperationException("The external submarine cradle could not attach.");
            // Show the actual attached cargo before beginning the airborne approach.
            Establish("cradle", "Trust outside the cargo bay", "Guess flies the Titan. Gohan stays seated in the Kraken on an external ventral cradle. Ice waits at the coastal rendezvous. The flight begins after this inspection; there is no off-screen drop teleport.", Titan, Sub);
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Fly the drop corridor", new ConditionObjective("Guess: fly over the offshore yellow ring, 150-350m above the sea, under 85m/s; press E / D-pad Right to release the sub", () => ReleaseInCorridor())).PlayedBy(CrewSlot.Guess).AfterCues("M42_S1_01_GUESS");
            yield return new MissionStage("Watch the cargo descent", new ConditionObjective("Guess: circle within 1.2km of the descending sub; keep flying until Gohan confirms a stable splashdown", () => Drop.Floating)).PlayedBy(CrewSlot.Guess).AfterCues("M42_S1_02_GOHAN");
            yield return new MissionStage("Pilot the delivered sub", new DeliverVehicleObjective("Gohan: take the floating Kraken to the yellow coastal rendezvous; Guess holds the Titan offshore", () => Sub, () => At("M42.Delivery"), 18f)).OwnedBy(CrewSlot.Gohan)
                .OnEnter(c => _planeOrders = 0).AfterCues("M42_S1_03_ICE");
        }
        private bool ReleaseInCorridor()
        {
            if (!_flightStarted) return false;
            GameUtils.DrawObjectiveMarker(new Vector3(_water.X, _water.Y, Titan.Position.Z), System.Drawing.Color.Yellow, 60f);
            ObjectiveMarkers.Navigation(_water, CrewSlot.Guess);
            bool ready = GameUtils.IsWithinFlat(Titan.Position, _water, 100f) && Titan.Position.Z - _water.Z >= 150f && Titan.Position.Z - _water.Z <= 350f && Titan.Speed < 85f && Math.Abs(Titan.Rotation.X) < 15f && Math.Abs(Titan.Rotation.Y) < 15f;
            GameUtils.Subtitle("Drop corridor: " + (ready ? "~g~READY - E / D-pad Right" : "~y~Level flight, 150-350m, inside ring") + " ~s~| Altitude " + (int)(Titan.Position.Z - _water.Z) + "m", 500);
            if (_fault != null || !ready || !Game.IsControlJustPressed(Control.Context)) return false;
            var first = WorkProp("p_parachute1_sp_s", Sub.Position, false);
            var second = WorkProp("p_parachute1_sp_s", Sub.Position, false);
            if (!Drop.Release(first, second)) { _fault = "The cargo parachutes failed to deploy. The sub was not released."; return false; }
            return true;
        }
        protected override void OnUpdate()
        {
            if (_fault != null) { Fail(_fault); return; }
            if (!_flightStarted && !Ctx.Cutscenes.IsActive)
            { Titan.IsPositionFrozen = false; Titan.Velocity = Titan.ForwardVector * 55f; _flightStarted = true; }
            if (Ctx.Cutscenes.IsActive) { base.OnUpdate(); return; }
            if (!Ctx.Crew.PedFor(CrewSlot.Guess).IsInVehicle(Titan) || Ctx.Crew.PedFor(CrewSlot.Guess).SeatIndex != VehicleSeat.Driver) { Fail("Guess left the Titan's pilot seat before the delivery was complete."); return; }
            Drop.Update();
            if (Drop.Failure != null) { Fail(Drop.Failure); return; }
            if (Drop.Released && !Drop.Floating)
            {
                GameUtils.Subtitle("Cargo descent: " + (int)Math.Max(0, Sub.Position.Z - _water.Z) + "m | Stay near the orange sub blip", 500);
                if (Titan.Position.DistanceTo(Sub.Position) > 1200f) { Fail("The Titan left the cargo observation area. Circle the descending sub on retry."); return; }
            }
            if (Drop.Floating && Ctx.Crew.ActiveSlot != CrewSlot.Guess && Game.GameTime >= _planeOrders)
            { _planeOrders = Game.GameTime + 12000; Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Guess); Ctx.Crew.PedFor(CrewSlot.Guess).Task.StartPlaneMission(Titan, At("M42.Hold"), VehicleMissionType.Circle, 55f, 300f, 280, 180, 0, true); }
            base.OnUpdate();
        }
        protected override void OnPassed()
        { if (!Drop.Floating) throw new InvalidOperationException("There was no verified splashdown."); Ctx.State?.SetCargo("offshoreSub", "M42.Delivery"); Release(Sub); Release(Titan); }
        protected override void OnCleanup()
        {
            // A failed/aborted inspection must not leave an NPC falling in detached cargo.
            if (Drop != null && !Drop.Floating)
            {
                var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
                if (gohan != null && gohan.Exists() && !gohan.IsDead)
                {
                    if (ExitVehicleStep.ForceOut(gohan)) Station(CrewSlot.Gohan, At("M42.GohanStart"));
                    else if (Sub != null && Sub.Exists())
                    {
                        // Last-resort abort recovery moves the occupied vehicle to already-validated water.
                        // This path never marks the drop delivered or advances an objective.
                        Sub.Detach(); Sub.Position = At("M42.Delivery"); Sub.Velocity = Vector3.Zero;
                        Logger.Warn("M42 abort: Gohan could not unseat; recovered the occupied sub at the safe cove.");
                    }
                }
            }
            Drop?.Cleanup();
            if (Titan != null && Titan.Exists()) { Titan.IsPositionFrozen = false; if (!_flightStarted) Titan.Velocity = Titan.ForwardVector * 55f; }
            base.OnCleanup();
        }
    }
}
