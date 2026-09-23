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
    public sealed class M41TheGeneralsWire : PreparationOperation
    {
        public override string Id => "M41";
        public override string Title => "The General's Wire";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;
        public Ped Bradley { get; private set; }
        public Ped LodgeWorker { get; private set; }
        public Vehicle Extraction => CrewCar;
        private Vehicle _escape; private Prop _card; private bool _identified, _safeShot, _held, _verified;
        private int _alarmAt = -1, _walkAt, _orderAt;
        /// <summary>
        /// Why a stage exit could not finish. Two exits here used to throw, which ends the
        /// attempt as a "Script error"; the reason is kept and fails it on the next frame
        /// instead (the September 22 audit).
        /// </summary>
        private string _fault;
        /// <summary>The drive to the trail exit is under way, so the brothers-aboard rule applies.</summary>
        private bool _extracting;
        private int _brotherOutSince = -1;
        /// <summary>How long a brother may be out of the extraction car on the way out before the rule fails the run.</summary>
        public const int AboardGraceMs = 3000;
        protected override bool Setup()
        {
            _fault = null; _extracting = false; _brotherOutSince = -1;
            if (!BeginCrew(CrewSlot.Ice)) return false;
            Bradley = Person("s_m_y_marine_01", "M41.Bradley", false);
            LodgeWorker = Person("a_m_m_hillbilly_01", "M41.Worker", false);
            _escape = Car("mesa", At("M41.EscapeCar"), Ctx.Locations.Heading("M41.EscapeCar"), false);
            CrewCar = CrewTransport("M41.Pickup");
            if (!RequireAssets(Bradley, LodgeWorker, _escape, CrewCar)) return false;
            Bradley.Health = 180; RequireSurvivor(LodgeWorker, "The lodge worker was killed. Identify Bradley before taking the shot.");
            Station(CrewSlot.Guess, CrewCar, VehicleSeat.Driver); Station(CrewSlot.Gohan, CrewCar, VehicleSeat.Passenger);
            Roles.For(CrewSlot.Guess).Stop(); Roles.For(CrewSlot.Gohan).Stop();
            for (int i = 1; i <= 2; i++) Enemy("M41.Guard" + i);
            Establish("approach", "A card worth coming back for", "Ice watches Bradley's exterior lodge meeting. The worker is not a target. Guess and Gohan hold the extraction car down the access road.", Bradley, LodgeWorker, CrewCar);
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Identify Bradley", new MissionInteraction("Ice: observe from the yellow lookout and identify the marine officer; spare the lodge worker", () => At("M41.Observe"), 3, 3f, animation: MissionInteraction.Watch, face: () => Bradley.Position)).OwnedBy(CrewSlot.Ice)
                .OnExit(c => { _identified = true; _walkAt = Game.GameTime; Bradley.Task.GoTo(At("M41.Meeting")); var b = Track(Bradley.AddBlip()); b.Color = BlipColor.Red; b.Name = "General Bradley"; });
            yield return new MissionStage("Wait for a clear shot", new ConditionObjective("Ice: watch Bradley walk clear of the lodge worker; do not fire until he reaches the meeting point", () => Bradley.Position.DistanceTo(At("M41.Meeting")) < 3f || _alarmAt >= 0)).OwnedBy(CrewSlot.Ice).OnExit(c => _safeShot = true);
            yield return new MissionStage("Stop Bradley", new ConditionObjective("Ice: eliminate Bradley before he escapes; the red officer carries the access card", () => Bradley.IsDead)).OwnedBy(CrewSlot.Ice)
                .OnExit(c => { Fighting = true; _card = WorkProp("prop_cs_swipe_card", Bradley.Position, true); if (!RequireAssets(_card)) { _fault = "Bradley's card could not load beside his body. Retry the lodge."; return; } RequireAsset(_card, "Bradley's card was destroyed before extraction."); });
            yield return new MissionStage("Recover the card", new MissionInteraction("Ice: recover the access card beside Bradley's body", () => _card != null && _card.Exists() ? _card.Position : Bradley.Position, 2, 3f, animation: MissionInteraction.Kneel)).OwnedBy(CrewSlot.Ice)
                .OnExit(c => { Carry(_card, CrewSlot.Ice); _held = true; }).AfterCues("M41_S1_01_ICE");
            yield return new MissionStage("Bring the card to extraction", new MissionInteraction("Ice: bring the card to the yellow rear-of-car marker for Gohan to verify", () => CrewCar.Position - CrewCar.ForwardVector * 4f, 3, 3f, animation: MissionInteraction.Handover)).OwnedBy(CrewSlot.Ice)
                .OnExit(c => VerifyCard()).AfterCues("M41_S1_02_GOHAN");
            yield return new MissionStage("Board the extraction car", new ConditionObjective("Guess: hold the crew car still while Ice takes a rear seat; Gohan stays in the front passenger seat", () => Board(Ctx.Crew.PedFor(CrewSlot.Guess), CrewCar, VehicleSeat.Driver) && Board(Ctx.Crew.PedFor(CrewSlot.Gohan), CrewCar, VehicleSeat.Passenger) && Board(Ctx.Crew.PedFor(CrewSlot.Ice), CrewCar, VehicleSeat.LeftRear))).AnyBrother()
                .OnEnter(c => { Roles.For(CrewSlot.Ice).Stop(); DrivingDestination = null; });
            // "Must remain aboard" is a rule, and a condition objective completes the first
            // frame it is true and never looks again, so it enforced nothing on the drive.
            // The rule itself is in OnUpdate for as long as the drive lasts (the September 22 audit).
            yield return new MissionStage("Extract the card", new TravelObjective("Guess: drive the whole crew and Bradley's card to the yellow trail exit and stop", () => At("M41.Exit"), 12f, () => CrewCar),
                new ConditionObjective("All three brothers must remain aboard the extraction car", () => Ctx.Crew.PedFor(CrewSlot.Ice).IsInVehicle(CrewCar) && Ctx.Crew.PedFor(CrewSlot.Gohan).IsInVehicle(CrewCar))).OwnedBy(CrewSlot.Guess)
                .OnEnter(c => { _extracting = true; _brotherOutSince = -1; }).OnExit(c => _extracting = false).AfterCues("M41_S1_03_GUESS");
        }
        /// <summary>The card handed from Ice to the car for Gohan to verify.</summary>
        private void VerifyCard()
        {
            if (!Attached(_card, Ctx.Crew.PedFor(CrewSlot.Ice))) { _fault = "The card is not in Ice's custody. Retry the handover at the car."; return; }
            try { SaveCargo(_card, CrewCar, new Vector3(0, 0, .5f)); _verified = true; }
            catch (Exception ex) { Logger.Error(Id + " card handover", ex); _fault = "The card could not be secured in the extraction car. Retry the handover."; }
        }

        /// <summary>Both brothers stay in the car while Guess drives out, with a short grace for a seat change.</summary>
        private bool BrothersAboard()
        {
            if (!_extracting || CrewCar == null || !CrewCar.Exists()) { _brotherOutSince = -1; return true; }
            bool aboard = new[] { CrewSlot.Ice, CrewSlot.Gohan }.All(s => { var p = Ctx.Crew.PedFor(s); return p != null && p.Exists() && p.IsInVehicle(CrewCar); });
            if (aboard) { _brotherOutSince = -1; return true; }
            if (_brotherOutSince < 0) _brotherOutSince = Game.GameTime;
            GameUtils.Subtitle("~r~Everyone back in the extraction car.", 500);
            return Game.GameTime - _brotherOutSince <= AboardGraceMs;
        }

        protected override void OnUpdate()
        {
            if (_fault != null) { Fail(_fault); return; }
            if (!BrothersAboard()) { Fail("A brother left the extraction car before the trail exit. All three ride out with the card."); return; }
            if (!_identified && Bradley.IsDead) { Fail("Bradley was shot before his identity was confirmed."); return; }
            if (_identified && !_safeShot && Bradley.IsDead) { Fail("Bradley was shot before the clear-shot signal. Let him walk away from the worker."); return; }
            if (!Bradley.IsDead && _alarmAt < 0 && (Game.Player.Character.IsShooting || Game.Player.Character.Position.DistanceTo(Bradley.Position) < 9f))
            { _alarmAt = Game.GameTime; Fighting = true; Bradley.Task.RunTo(_escape.Position, false, 15000); }
            if (_identified && !_safeShot && _alarmAt < 0 && Game.GameTime - _walkAt > 45000) { Fail("Bradley's walk was obstructed. Retry the exterior meeting placement."); return; }
            if (_alarmAt >= 0 && !Bradley.IsDead)
            {
                if (Game.GameTime - _alarmAt > 60000 || Bradley.Position.DistanceTo(At("M41.Bradley")) > 450f) { Fail("Bradley escaped with the card."); return; }
                if (Game.GameTime >= _orderAt)
                { _orderAt = Game.GameTime + 5000; if (Bradley.IsInVehicle(_escape)) Bradley.Task.DriveTo(_escape, At("M41.EscapeEnd"), 10f, 30f, DrivingStyle.Rushed); else if (Bradley.Position.DistanceTo(_escape.Position) < 10f) Bradley.Task.EnterVehicle(_escape, VehicleSeat.Driver); else Bradley.Task.RunTo(_escape.Position, false, 10000); }
            }
            if (_held && !Attached(_card, _verified ? (Entity)CrewCar : Ctx.Crew.PedFor(CrewSlot.Ice))) { Fail("The access card was lost before delivery."); return; }
            base.OnUpdate();
        }
        protected override void OnPassed()
        { if (!_verified) throw new InvalidOperationException("Bradley's card was not verified."); Ctx.State?.SetEvidence("bradleyKeycard", EvidenceState.CopyHeld); Release(CrewCar); Release(_card); }
    }
}
