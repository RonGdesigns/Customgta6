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
    /// M69 — "Blood Brothers: Runway 30L". LSIA, at speed.
    ///
    /// The rig goes through the perimeter and straight down the runway to the waiting C-130.
    /// Aegis has a barricade on the taxiway and there is other traffic on the ground.
    ///
    /// **The runway line is measured.** Sixty-three <c>prop_roadpole_01a</c> run in a straight
    /// line across LSIA from (-1398, -3183, 13.15) to (-1121, -2690, 12.95) — five hundred and
    /// sixty-six meters of airport surface at a constant z 13. That is the lane this mission
    /// drives, taken from the poles rather than guessed at, and it is why the run has a real
    /// length instead of an arbitrary one.
    ///
    /// **The 747 is not reproduced.** `M69_S1_01_GUESS` has a commercial jet touching down in
    /// front of the rig. Landing an airliner on a schedule, on a runway, in front of a moving
    /// truck is a set piece that needs a flight path this mission cannot verify, and a jet
    /// that lands in the wrong place is worse than no jet. It is not fired. The barricade and
    /// the ramp both are, and both are real. Recorded in `data/mission_gameplay.tsv`.
    ///
    /// **The fuel bowser is real**, because `M69_S1_02_ICE` tells Guess to ram it into the
    /// Aegis line: a <c>airtug</c>-class tanker on the taxiway, placed at the barricade, and
    /// destroying it is the objective that clears the line.
    /// </summary>
    public sealed class M69BloodBrothersRunway : PreparationOperation
    {
        public const string RigModel = "phantom";
        public const string TrailerModel = "trailers";
        public const string BowserModel = "airtug";
        public const string BarricadeModel = "riot";
        public const string PlaneModel = "titan";
        /// <summary>Aegis vehicles across the taxiway.</summary>
        public const int Barricades = 2;
        /// <summary>SWAT holding the barricade.</summary>
        public const int SwatPosts = 5;
        /// <summary>How near the ramp counts as aboard.</summary>
        public const float RampRadius = 10f;
        /// <summary>
        /// How far behind the Titan's center its cargo ramp is. The delivery used to target the
        /// plane's own spawn point, so "up the ramp" meant within fourteen meters of the middle
        /// of the fuselage - reachable only by driving the rig under the wing into the one
        /// vehicle the mission could not afford to lose (Ron, September 22).
        /// </summary>
        public const float RampBehind = 22f;
        /// <summary>Where the campaign records the rig is on the plane.</summary>
        public const string AboardCargo = "rigAboardTitan";

        private readonly List<Vehicle> _barricade = new List<Vehicle>();
        private readonly List<Ped> _defenders = new List<Ped>();
        private Vehicle _rig, _trailer, _bowser, _plane;
        private bool _onTheRunway, _lineBroken, _aboard;

        public override string Id => "M69";
        public override string Title => "Blood Brothers: Runway 30L";
        protected override MissionEndpoint Endpoint => MissionEndpoint.ContinuousNext;

        /// <summary>The rig is through the perimeter and on the runway.</summary>
        public bool OnTheRunway => _onTheRunway;
        /// <summary>The taxiway barricade is gone.</summary>
        public bool LineBroken => _lineBroken;
        /// <summary>The rig is up the C-130's ramp.</summary>
        public bool Aboard => _aboard;
        public Vehicle Rig => _rig;
        public Vehicle Bowser => _bowser;
        public Vehicle Plane => _plane;
        public IReadOnlyList<Ped> Defenders => _defenders;

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Guess)) return false;

            _rig = Car(RigModel, At("M69.Rig"), Ctx.Locations.Heading("M69.Rig"), true);
            _trailer = _rig == null ? null
                : Car(TrailerModel, _rig.Position - _rig.ForwardVector * 11f, Ctx.Locations.Heading("M69.Rig"), false);
            if (!RequireAssets(_rig, _trailer)) return false;
            _rig.IsPersistent = true;
            _trailer.IsPersistent = true;
            GTA.Native.Function.Call(GTA.Native.Hash.ATTACH_VEHICLE_TO_TRAILER, _rig, _trailer, 2f);
            RequireAsset(_rig, "The rig was destroyed on the runway with everything aboard.");
            Station(CrewSlot.Guess, _rig, VehicleSeat.Driver);
            Station(CrewSlot.Ice, _rig, VehicleSeat.Passenger);

            _plane = Car(PlaneModel, At("M69.Ramp"), Ctx.Locations.Heading("M69.Ramp"), false);
            if (!RequireAssets(_plane)) return false;
            _plane.IsPersistent = true;
            _plane.IsEngineRunning = false;
            // Car() settles only cars onto their wheels; a plane created at the authored height
            // is left with its gear sunk into the apron.
            _plane.PlaceOnGround();
            var planeBlip = Track(_plane.AddBlip());
            if (planeBlip != null) { planeBlip.Color = BlipColor.Green; planeBlip.Name = "C-130 cargo ramp"; }
            RequireAsset(_plane, "The cargo plane was destroyed. There is nothing left to leave on.");

            // The bowser Ice tells Guess to ram, and the line it goes into.
            _bowser = Car(BowserModel, At("M69.Bowser"), Ctx.Locations.Heading("M69.Bowser"), false);
            if (_bowser != null && _bowser.Exists()) _bowser.IsPersistent = true;
            for (int i = 1; i <= Barricades; i++)
            {
                var car = Car(BarricadeModel, At("M69.Block" + i), Ctx.Locations.Heading("M69.Block" + i), false);
                if (car == null || !car.Exists()) continue;
                car.IsPersistent = true;
                _barricade.Add(car);
            }
            for (int i = 1; i <= SwatPosts; i++)
            {
                var ped = Enemy("M69.Swat" + i);
                if (ped != null) _defenders.Add(ped);
            }
            if (_defenders.Count == 0)
                Logger.Warn(Id + ": no Aegis SWAT could be placed at the taxiway; the barricade is unmanned.");

            Fighting = true;
            Establish("approach", "Straight down the middle of it",
                "The perimeter is one fence and the runway is five hundred meters of open ground with a C-130 at the end of it. Aegis has a line across the taxiway and there is nothing to do but go through it.",
                _rig, _plane);
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            // M69_S1_01_GUESS has a 747 landing in front of the rig; the mission cannot put an
            // airliner on a verified approach, so that line is not fired and the run stands on
            // the barricade and the ramp instead.
            yield return new MissionStage("Through the perimeter",
                new TravelObjective("Guess: take the rig through the perimeter and onto the runway",
                    () => At("M69.Runway"), 30f, () => _rig))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(c => { _onTheRunway = true; DrivingDestination = RampPoint; });

            yield return new MissionStage("Break the taxiway line",
                new DestroyVehicleObjective("Put the fuel bowser into the Aegis line", () => _bowser),
                new KillTargetsObjective("Clear the Aegis barricade", () => _defenders),
                new ProtectObjective("", () => _rig, "The rig was wrecked short of the plane."))
                .AnyBrother()
                .OnExit(c => _lineBroken = true)
                .AfterCues("M69_S1_02_ICE");

            yield return new MissionStage("Up the ramp",
                new DeliverVehicleObjective("Guess: drive the rig up into the C-130's cargo hold",
                    () => _rig, RampPoint, RampRadius))
                .OwnedBy(CrewSlot.Guess)
                .OnExit(c => Boarded())
                .AfterCues("M69_S1_03_GOHAN");
        }

        /// <summary>The foot of the Titan's cargo ramp, behind its tail, wherever the plane actually is.</summary>
        private Vector3 RampPoint() =>
            _plane != null && _plane.Exists()
                ? _plane.Position - _plane.ForwardVector * RampBehind
                : At("M69.Ramp");

        private void Boarded()
        {
            _aboard = true;
            DrivingDestination = null;
            Ctx.State?.SetCargo(AboardCargo, "M69.Ramp");
            Logger.Info(Id + ": the rig is in the cargo hold.");
        }

        protected override void OnPassed()
        {
            if (!_onTheRunway || !_lineBroken || !_aboard)
                throw new InvalidOperationException("The runway, the barricade and the ramp all have to be taken.");
            Release(_rig);
            Release(_trailer);
            Release(_plane);
        }
    }
}
