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
    public sealed class M43StagingPaleto : CoastalOperation
    {
        public override string Id => "M43";
        public override string Title => "Staging Paleto";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;
        public Vehicle StagingSub => Sub;
        public Vehicle StagingBoat => Launch;
        public Vehicle Helicopter { get; private set; }
        private Prop _table, _board;
        private bool _subReady, _boatReady, _airReady, _committed;
        public string MissingPreparation { get; private set; }
        /// <summary>The preparation chapters this save has not finished, as read at the start.</summary>
        public string MissingAtStart { get; private set; }
        /// <summary>Why the sign-off could not be committed, failed on the next frame rather than thrown from the stage exit.</summary>
        private string _fault;
        private static readonly string[] RequiredUpgrades = { "bunkerPerimeterReady", "empCasesSecured", "ramosRescued", "armoredEscortReady", "technicalSupportReady", "offshoreSurveyReady", "aircraftSmokeReady", "seismicStockReady", "rigMainlandCableCut", "extractionLaunchesReady" };
        /// <summary>Departure room an Annihilator needs in front of its landing point.</summary>
        public const float AircraftClearance = 30f;
        protected override bool Setup()
        {
            _fault = null;
            // Said up front. The ledger stage waits for every chapter from M31 to M42, and a
            // save that has not finished them (a QA start, or a chapter played out of order)
            // used to find that out only after positioning three vehicles, from a subtitle
            // that never went away. It is a notice rather than a refusal: the staging itself
            // is still worth checking, and the ledger names what is missing when he gets
            // there (the September 22 audit).
            var unfinished = Enumerable.Range(31, 12).Select(n => "M" + n)
                .Where(id => Ctx.State == null || !Ctx.State.Completed.Contains(id)).ToList();
            MissingAtStart = string.Join(", ", unfinished);
            if (unfinished.Count > 0)
            {
                GameUtils.Notify("~o~Staging Paleto cannot be signed off yet. Finish first: " + MissingAtStart + ".");
                Logger.Warn(Id + " started with preparation chapters unfinished: " + MissingAtStart + ". The ledger stage will wait for them.");
            }
            if (!BeginCrew(CrewSlot.Gohan)) return false;
            // This used to capture both aircraft keys, deploy the crew, then read the
            // same keys again and refuse the mission if either had moved more than
            // two meters. It always refused for the wrong reason: the generic ground
            // preparation calls GetSafeCoordForPed, which is meant to shift a point
            // sideways onto walkable ground, and writes the result back. A legitimate
            // snap therefore read as "the aircraft site moved outside its footprint",
            // and that is what stopped this mission starting in Ron's September 13 run.
            //
            // Ground that is walkable for a person is also not rotor clearance, which
            // is what the old message claimed to be checking. The real footprint check
            // runs here instead, against the actual model, and reports by key. It
            // warns rather than refuses: these are estimates, the helicopter still
            // spawns and lands, and the named verdict is what a survey pass acts on.
            var annihilator = new Model("annihilator");
            foreach (var key in new[] { "M43.Land", "M43.Helicopter" })
                PlacementContract.Aircraft(key, annihilator, AircraftClearance).Inspect(Ctx.Locations, Ctx.Doctor);
            Sub = Boat("submersible2", "M43.Sub", 3f, 3.5f, 5f);
            Launch = Boat("tropic", "M43.Boat", 2f, 1.5f, 4.5f);
            Helicopter = Car("annihilator", At("M43.Helicopter"), Ctx.Locations.Heading("M43.Helicopter"));
            if (!RequireAssets(Sub, Launch, Helicopter)) return false;
            Helicopter.PlaceOnGround(); RequireAsset(Helicopter, "The extraction helicopter was destroyed before staging was signed off.");
            Station(CrewSlot.Gohan, Sub, VehicleSeat.Driver); Station(CrewSlot.Ice, Launch, VehicleSeat.Driver); Station(CrewSlot.Guess, Helicopter, VehicleSeat.Driver);
            foreach (var s in new[] { CrewSlot.Guess, CrewSlot.Gohan, CrewSlot.Ice }) Roles.For(s).Stop();
            _table = Equipment("prop_table_03", "M43.Board"); _board = WorkProp("prop_laptop_01a", PropPlacement.OnTop(_table, _table.Model, new Model("prop_laptop_01a")), false);
            if (!RequireAssets(_board)) return false;
            CrewCar = CrewTransport("M43.ShoreCar");
            if (!RequireAssets(CrewCar)) return false;
            Establish("approach", "Three positions, one exit", "The brothers check in by radio from the submarine, surface launch and helicopter. Each must put his own vehicle in its actual holding position before Guess uses the shore planning laptop.", Sub, Launch, Helicopter);
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Position the sub", new TravelObjective("Gohan: move the Kraken to its yellow offshore holding marker and stop", () => At("M43.SubReady"), 12f, () => Sub)).OwnedBy(CrewSlot.Gohan).OnExit(c => { _subReady = true; Function.Call(Hash.SET_BOAT_ANCHOR, Sub, true); });
            yield return new MissionStage("Position the launch", new TravelObjective("Ice: move the Tropic to the yellow surface holding marker and stop", () => At("M43.BoatReady"), 12f, () => Launch)).OwnedBy(CrewSlot.Ice).OnExit(c => { _boatReady = true; Function.Call(Hash.SET_BOAT_ANCHOR, Launch, true); }).AfterCues("M43_S1_03_GOHAN");
            yield return new MissionStage("Land the extraction helicopter", new DeliverVehicleObjective("Guess: fly the Annihilator to the yellow Paleto coastal staging lot and land at its center", () => Helicopter, () => At("M43.Land"), 4f, true)).OwnedBy(CrewSlot.Guess).OnExit(c => _airReady = true).AfterCues("M43_S1_02_GUESS");
            yield return new MissionStage("Check the preparation ledger", new ConditionObjective("Guess: all three vehicles must stay in their holding positions; complete any missing preparation missions shown below", () => ReadyToCommit())).OwnedBy(CrewSlot.Guess);
            yield return new MissionStage("Commit the staging plan", new MissionInteraction("Guess: walk to the laptop beside the Paleto landing area and confirm the offshore plan", () => At("M43.BoardWork"), 5, 3f, animation: MissionInteraction.ReachInside, face: () => _board.Position)).OwnedBy(CrewSlot.Guess)
                // A vehicle that drifted off its mark during the walk to the laptop used to
                // throw here, a "Script error"; it fails with the reason instead
                // (the September 22 audit).
                .OnExit(c => { if (!ReadyToCommit()) { _fault = "An asset left its holding position before the plan was confirmed: " + MissingPreparation + "."; return; } _committed = true; Establish("ready", "We leave as three", "All three assets are in position. Ramos's rescue, the EMP, survey, smoke, charges, cable cut and verified card are in the ledger. This records preparation for the future offshore assault; it does not create an offshore rig or award its vault.", _board); }).AfterCues("M43_S1_01_ICE");
        }
        private bool ReadyToCommit()
        {
            var missing = new List<string>();
            for (int n = 31; n <= 42; n++) if (Ctx.State == null || !Ctx.State.Completed.Contains("M" + n)) missing.Add("M" + n);
            if (Ctx.State != null)
            {
                missing.AddRange(RequiredUpgrades.Where(k => !Ctx.State.FleetUpgrades.TryGetValue(k, out bool set) || !set));
                if (Ctx.State.EvidenceOf("bradleyKeycard") != EvidenceState.CopyHeld) missing.Add("Bradley card");
                if (string.IsNullOrEmpty(Ctx.State.CargoAt("offshoreSub"))) missing.Add("delivered submarine");
            }
            if (!_subReady || Sub.Position.DistanceTo(At("M43.SubReady")) > 25f || Sub.Speed > 3f) missing.Add("sub position");
            if (!_boatReady || Launch.Position.DistanceTo(At("M43.BoatReady")) > 25f || Launch.Speed > 3f) missing.Add("launch position");
            if (!_airReady || Helicopter.Position.DistanceTo(At("M43.Land")) > 18f || Helicopter.HeightAboveGround > 3f || Helicopter.Speed > 3f) missing.Add("helicopter landing");
            MissingPreparation = string.Join(", ", missing);
            if (missing.Count != 0) GameUtils.Subtitle("Preparation missing: " + MissingPreparation, 500);
            return missing.Count == 0;
        }
        protected override void OnUpdate()
        {
            if (_fault != null) { Fail(_fault); return; }
            base.OnUpdate();
        }
        protected override void OnPassed()
        { if (!_committed) throw new InvalidOperationException("The staging ledger was not confirmed."); Ctx.State?.SetCargo("offshoreStaging", "M43.Board"); Release(Sub); Release(Launch); Release(Helicopter); }
        protected override void OnCleanup()
        { if (Sub != null && Sub.Exists()) Function.Call(Hash.SET_BOAT_ANCHOR, Sub, false); if (Launch != null && Launch.Exists()) Function.Call(Hash.SET_BOAT_ANCHOR, Launch, false); base.OnCleanup(); }
    }
}
