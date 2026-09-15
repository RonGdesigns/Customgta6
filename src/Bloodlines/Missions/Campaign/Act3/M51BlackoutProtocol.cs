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
    /// M51 — "Blackout Protocol". Palmer-Taylor Power Station, 23:00, overcast.
    ///
    /// Six limpets on the plant's two switchyard banks and the automatic failovers locked
    /// out, so downtown can be put in the dark when the tower job needs it. The charges
    /// are left armed. They are not fired here, and that is Ron's decision, not an
    /// omission: the outage belongs to the downtown offensive the later tower missions
    /// open with, and spending it now would spend it on nothing.
    ///
    /// What the site actually is. The bible asks for six 500kV step-down transformers and
    /// there are no transformer props at Palmer-Taylor — its switchyard is baked map
    /// geometry. What is placed there is a tank farm, and six of its units sit in two
    /// clusters of three at yard level, 125 meters apart, which is the two-section
    /// arrangement the plan asked for. Every charge point below is one of those units at
    /// its own archive height. See docs/ACT3-OPENING-MAP-M49-M53.md.
    ///
    /// Why the three jobs share one stage. Ice takes the west bank, Guess the east, Gohan
    /// the interlocks, and because they are parallel objectives in a single stage the
    /// player can switch freely and do them in any order — which is what Ron asked for in
    /// M35 and what the composed-mission dispatcher already supports. A stage each would
    /// have forced his hand three times.
    /// </summary>
    public sealed class M51BlackoutProtocol : PreparationOperation
    {
        /// <summary>How long a limpet takes to seat on a unit.</summary>
        public const int LimpetSeconds = 4;
        /// <summary>How long the failover lockout takes at the interlock cabinet.</summary>
        public const int LockoutSeconds = 6;
        /// <summary>How long arming the sequence takes once everything is placed.</summary>
        public const int ArmSeconds = 5;
        /// <summary>How close a limpet has to be seated. The east bank's units are six meters apart.</summary>
        public const float LimpetRadius = 2.5f;
        /// <summary>Where the campaign records that Palmer-Taylor is wired and waiting.</summary>
        public const string ReadyCargo = "downtownBlackoutCharges";

        private readonly CrewBoarding _boarding = new CrewBoarding();
        private int _west, _east;
        private bool _lockedOut, _armed;

        public override string Id => "M51";
        public override string Title => "Blackout Protocol";
        protected override MissionEndpoint Endpoint => MissionEndpoint.EscapeCheckpoint;

        /// <summary>Limpets seated on the west bank, for a test and the dev readout.</summary>
        public int West => _west;
        public int East => _east;
        /// <summary>The automatic failovers are locked out.</summary>
        public bool LockedOut => _lockedOut;
        /// <summary>The sequence is armed and waiting. Nothing has been fired.</summary>
        public bool Armed => _armed;
        public Vehicle Transport => CrewCar;

        /// <summary>
        /// Nothing here is a fixed surface, and the first version of this class had all seven
        /// listed as one.
        ///
        /// The reasoning was that a limpet goes on a tank, so the marker should keep the
        /// tank's height. But the marker is not where the charge ends up — it is where Ice
        /// has to stand to place it, and a placed prop's origin is not a floor. Holding these
        /// at their archive heights put markers up in the air on the side of a tank, and Ron
        /// could not reach them: "it tells Ice to reach a place that is in the air, he cannot
        /// go up the side of the building."
        ///
        /// So ground preparation owns them. Every point moves to walkable ground beside its
        /// unit, which is where a man stands to reach one. The units are seventeen meters
        /// apart at the closest, so a correction of a stride or two cannot make two of them
        /// ambiguous.
        /// </summary>
        protected override string[] FixedSurfaces => new string[0];

        private Vector3[] Bank(int from) =>
            Enumerable.Range(from, 3).Select(i => At("M51.Charge" + i)).ToArray();

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Ice)) return false;

            CrewCar = CrewTransport("M51.Crew");
            if (!RequireAssets(CrewCar)) return false;

            for (int i = 1; i <= 3; i++) Enemy("M51.Guard" + i);

            // The six points are reviewed rather than trusted: they came out of the
            // archives, nobody has walked the yard, and a limpet marker inside a tank is
            // the same failure as M43's invisible laptop.
            foreach (var key in FixedSurfaces) Paleto.Review(Ctx, PlacementContract.Interaction(key));

            Establish("approach", "Six on the banks, and nothing lit tonight",
                "Ice takes the west bank and Guess the east while Gohan locks the failovers out. The charges stay armed: the dark is for downtown, not for Palmer-Taylor.",
                CrewCar);
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            var west = new MultiHoldObjective("Ice: seat a limpet on each of the three west units",
                Bank(1), LimpetSeconds, LimpetRadius, "Seating limpet")
            { RequiredCharacter = CrewSlot.Ice, Animation = MissionInteraction.ReachInside };
            west.SiteDone = _ => _west++;

            var east = new MultiHoldObjective("Guess: seat a limpet on each of the three east units",
                Bank(4), LimpetSeconds, LimpetRadius, "Seating limpet")
            { RequiredCharacter = CrewSlot.Guess, Animation = MissionInteraction.ReachInside };
            east.SiteDone = _ => _east++;

            var interlocks = new MissionInteraction("Gohan: lock the automatic failovers out at the interlock cabinet",
                () => At("M51.Control"), LockoutSeconds, 3.5f, animation: MissionInteraction.ReachInside)
            { RequiredCharacter = CrewSlot.Gohan };

            // One stage, three parallel jobs. The dispatcher only demands a switch when the
            // brother the player is holding has nothing left to do here, so all three are
            // open in any order and switching is the player's to spend.
            yield return new MissionStage("Wire both banks", west, east, interlocks)
                .OnExit(c =>
                {
                    if (_west < 3 || _east < 3) throw new InvalidOperationException("Six limpets have to be seated before the sequence is armed.");
                    _lockedOut = true;
                })
                .AfterCues("M51_S1_01_ICE", "M51_S1_02_GUESS");

            yield return new MissionStage("Arm the sequence",
                new MissionInteraction("Gohan: arm the sequence and leave it waiting", () => At("M51.Control"), ArmSeconds, 3.5f,
                    animation: MissionInteraction.ReachInside))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(c => Arm());

            yield return new MissionStage("Get out of the yard",
                new EnterVehicleObjective("All three: get back in the car", () => CrewCar, VehicleSeat.Driver, true),
                new ConditionObjective("Nobody is left in the switchyard", () => Aboard))
                .AnyOf()
                .OnEnter(c => _boarding.Reset());

            yield return new MissionStage("Clear the station",
                new TravelObjective("Drive clear of Palmer-Taylor", () => At("M51.Exit"), 20f, () => CrewCar))
                .AnyOf()
                // M51_S1_03_GOHAN is deliberately not fired. The authored line counts the
                // charges down and calls the blackout, and nothing here detonates: Ron chose
                // to hold the outage for the downtown offensive. Playing it would tell the
                // player the city had just gone dark while every light stayed on. The bible
                // extraction is never edited to follow gameplay, so the divergence is
                // recorded in data/mission_gameplay.tsv and the line waits for the mission
                // that actually fires the sequence.
                ;
        }

        private bool Aboard => CrewCar != null && CrewCar.Exists() &&
            Protagonist.All.All(hero => Ctx.Crew.PedFor(hero.Slot)?.IsInVehicle(CrewCar) == true);

        /// <summary>
        /// Armed and left alone.
        ///
        /// Deliberately no call into <see cref="WorldLights"/>. Ron chose to hold the
        /// outage for the tower missions, so this records that Palmer-Taylor is wired and
        /// stops there. Anyone later tempted to "finish" M51 by darkening the city should
        /// read that as the decision it is: the lights going out is a different mission's
        /// beat, and firing it here spends it on an empty street.
        /// </summary>
        private void Arm()
        {
            _armed = true;
            Ctx.State?.SetCargo(ReadyCargo, "M51.Control");
            Logger.Info("M51: six limpets seated and the failovers locked out. The sequence is armed and waiting for downtown.");
            GameUtils.Subtitle("~g~Sequence armed. Palmer-Taylor stays lit until downtown needs the dark.", 6000);
        }

        protected override void OnUpdate()
        {
            // Whoever is not being played walks to the car rather than being left in the
            // yard; the objective above only ever asked whether they were already in it.
            if (_armed && !Aboard) _boarding.Update(Ctx.Crew, CrewCar, CrewBoarding.Passengers(CrewSlot.Guess), Id);
            base.OnUpdate();
        }

        protected override void OnPassed()
        {
            if (_west < 3 || _east < 3 || !_lockedOut || !_armed)
                throw new InvalidOperationException("Palmer-Taylor is not wired: six limpets, the failover lockout and the arming all have to have happened.");
            Release(CrewCar);
        }
    }
}
