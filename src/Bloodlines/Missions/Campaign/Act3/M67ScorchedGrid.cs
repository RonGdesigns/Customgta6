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
    /// M67 — "Scorched Grid". Inside the extraction semi, on the move.
    ///
    /// Gohan routes the five hundred million out of the Aegis escrow and into accounts nobody
    /// can follow, while Guess drives and Ice watches the road behind.
    ///
    /// This is the quiet one between two sieges, and it is deliberately not a fight. The
    /// synopsis is a man at a laptop in a moving truck, so the mission's whole job is to keep
    /// the truck moving while he works and to make the work take long enough to matter. Guess
    /// keeps a standing destination so the rig does not stop the moment the player switches to
    /// Gohan, which is the `DrivingDestination` contract M35 established.
    ///
    /// **There is no sub-station.** The bible files this under "Del Perro Industrial
    /// Sub-Station" and no such model exists in the archives. It matters less than it would
    /// anywhere else, because every beat of the mission happens inside a moving vehicle; the
    /// route runs from Del Perro to the airport, which is where `M67_S1_03_ICE` says they are
    /// going next. Recorded in `data/mission_gameplay.tsv`.
    ///
    /// The transfer is not a win, and the authored line is careful about that — "the money's
    /// accessible, it isn't invisible" — so the mission records the money as moved and nothing
    /// more. Whether they keep it is M68 to M70.
    ///
    /// **Ice has his own car.** The Phantom's cab seats two, Guess and Gohan, and Ice used to
    /// be left standing at his start point when the rig drove off (Ron, September 22). He
    /// drives a Buffalo S behind it now: escorting the rig while nothing is after it, and
    /// pursuing and shooting from the wheel when something is. When the player is Ice, the car
    /// is simply his to drive, and nothing in the mission waits on which vehicle he is in.
    /// </summary>
    public sealed class M67ScorchedGrid : PreparationOperation
    {
        public const string SemiModel = "phantom";
        public const string TrailerModel = "trailers";
        /// <summary>
        /// Ice's car. Four seats and an estimated 46 m/s in build/vehicles.json against the
        /// Phantom's 34, so it can hold station behind the rig and still close on anything
        /// chasing it; and ordinary glass, so a drive-by from it is possible, which the armored
        /// Kuruma's is not.
        /// </summary>
        public const string EscortModel = "buffalo2";
        /// <summary>
        /// Where Ice's car waits: in line behind the rig's center. The trailer's tail is about
        /// seventeen meters back, which leaves six meters between it and his front bumper.
        /// </summary>
        public const float EscortParkBehind = 26f;
        /// <summary>How close the escort mission holds him to the back of the rig.</summary>
        public const float EscortGap = 12f;
        /// <summary>How near a hostile has to be before Ice stops escorting and goes after him.</summary>
        public const float EngageMeters = 90f;
        /// <summary>How long an order to get into his car stands before it is given again.</summary>
        public const int BoardRenewMs = 8000;
        /// <summary>How long his car may sit still while the rig pulls away before the escort is given again.</summary>
        public const int EscortStallMs = 4000;
        /// <summary>Beyond this from a moving rig, a stopped escort has lost it rather than parked behind it.</summary>
        public const float EscortCatchUp = 40f;
        /// <summary>How near the airport the rig has to stop for the run to be over.</summary>
        public const float ArrivalRadius = 30f;
        /// <summary>How long the transfer takes. Five hundred million across a lot of accounts.</summary>
        public const int TransferSeconds = 25;
        /// <summary>Under this the rig has stopped.</summary>
        public const float MovingSpeed = 4f;
        /// <summary>How long it may sit still before the mission says something.</summary>
        public const int StallPatienceMs = 20000;
        /// <summary>Where the campaign records the money is out.</summary>
        public const string FundsEvidence = "escrowDistributed";

        private Vehicle _semi, _trailer, _escort;
        private int _stalledAt;
        private bool _rolling, _transferred;

        /// <summary>What Ice was last told to do, so an order is given once and again only on a change.</summary>
        public enum IceOrder { None, Board, Escort, Fight }
        private IceOrder _iceOrder;
        private Ped _iceTarget;
        private int _iceOrderedAt, _escortStillSince;

        public override string Id => "M67";
        public override string Title => "Scorched Grid";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;

        /// <summary>The rig is on the road with everybody aboard.</summary>
        public bool Rolling => _rolling;
        /// <summary>The money is distributed.</summary>
        public bool Transferred => _transferred;
        public Vehicle Semi => _semi;
        public Vehicle Trailer => _trailer;
        /// <summary>Ice's car.</summary>
        public Vehicle Escort => _escort;
        /// <summary>What Ice was last ordered to do while he was not the player.</summary>
        public IceOrder IceLastOrder => _iceOrder;

        private bool EscortUsable => _escort != null && _escort.Exists() && !_escort.IsDead && _escort.IsDriveable;

        /// <summary>
        /// Driving his car is Ice's job for the whole mission. Without this he counts as a man
        /// with nothing to do, and the idle rule would hand him to the player - whose vehicle
        /// is a cab with no seat for him.
        /// </summary>
        protected override bool HasOwnWork(CrewSlot slot) =>
            (slot == CrewSlot.Ice && EscortUsable) || base.HasOwnWork(slot);

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Guess)) return false;

            _semi = Car(SemiModel, At("M67.Semi"), Ctx.Locations.Heading("M67.Semi"), true);
            _trailer = _semi == null ? null
                : Car(TrailerModel, _semi.Position - _semi.ForwardVector * 11f, Ctx.Locations.Heading("M67.Semi"), false);
            if (!RequireAssets(_semi, _trailer)) return false;
            _semi.IsPersistent = true;
            _trailer.IsPersistent = true;
            Function.Call(Hash.ATTACH_VEHICLE_TO_TRAILER, _semi, _trailer, 2f);
            RequireAsset(_semi, "The extraction rig was destroyed with the money still in escrow.");

            // Guess drives, Gohan works, Ice watches the road behind. Nobody is asked for a
            // seat the cab has not got.
            Station(CrewSlot.Guess, _semi, VehicleSeat.Driver);
            Station(CrewSlot.Gohan, _semi, VehicleSeat.Passenger);

            // Ice's own car, parked in line behind the rig. Not a required asset: losing it
            // costs him his ride, never the mission.
            _escort = Car(EscortModel, BehindTheRig(_semi.Position, _semi.ForwardVector, EscortParkBehind), _semi.Heading, false);
            if (_escort != null && _escort.Exists())
            {
                _escort.IsPersistent = true;
                var blip = Track(_escort.AddBlip());
                if (blip != null) { blip.Color = BlipColor.Blue; blip.Name = "Ice's car"; }
                // His start-point watch would walk him back to where he began and out of his
                // car; he has a job of his own now, run from KeepIce.
                Roles?.For(CrewSlot.Ice).Stop();
            }
            else Logger.Warn(Id + ": Ice's car could not be placed; he has no vehicle of his own on this run.");

            Paleto.Review(Ctx, PlacementContract.Vehicle("M67.Semi", new Model(SemiModel)),
                PlacementContract.Ped("M67.Start"));

            Establish("approach", "Five hundred million, one laptop",
                "Gohan has the authorizations and the rig has the road. The transfer takes as long as it takes, and the truck does not stop while it runs.",
                _semi);
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Get the rig rolling",
                new ConditionObjective("Guess: get the rig on the road to the airport", Moving)
                { Marker = () => _semi != null && _semi.Exists() ? _semi.Position : At("M67.Semi"), MarkerRadius = 6f })
                .OwnedBy(CrewSlot.Guess)
                .OnExit(c => { _rolling = true; DrivingDestination = () => At("M67.Airport"); });

            // The work is done from his seat in the moving cab, so the interaction is attached
            // to the rig: seated in it counts. As an on-foot interaction it asked Gohan to be
            // standing within six meters of a truck he was riding in and could never start
            // (Ron, September 22). The rig is not asked to stop - the whole point is that it
            // does not.
            yield return new MissionStage("Move the money",
                new MissionInteraction("Gohan: route the escrow into the offshore accounts",
                    () => _semi != null && _semi.Exists() ? _semi.Position : At("M67.Semi"),
                    TransferSeconds, 6f, vehicle: () => _semi)
                { RequiredCharacter = CrewSlot.Gohan },
                new ProtectObjective("", () => _semi, "The rig was wrecked with the money still in escrow."))
                .OnExit(c => Moved())
                .AfterCues("M67_S1_01_GOHAN", "M67_S1_02_GUESS");

            // The rig stopped at the perimeter is the end of the run, whoever is at its wheel.
            // With the travel leg alone, a player driving Ice's car beside it was told to get
            // back in the crew's vehicle while Guess sat parked at the destination, and the
            // stage waited on a switch nothing asked him for. Neither objective is true before
            // the rig has actually arrived, so either may end the stage.
            yield return new MissionStage("Make the airport",
                new TravelObjective("Take the rig to the airport perimeter", () => At("M67.Airport"), ArrivalRadius, () => _semi),
                new ConditionObjective("", RigArrived))
                .AnyOf()
                .AnyBrother()
                .OnExit(c => DrivingDestination = null)
                .AfterCues("M67_S1_03_ICE");
        }

        /// <summary>Whether the rig is actually under way.</summary>
        private bool Moving() => _semi != null && _semi.Exists() && _semi.Speed > MovingSpeed;

        /// <summary>The rig is stopped inside the airport perimeter, the same test the travel leg makes.</summary>
        private bool RigArrived() => _semi != null && _semi.Exists() && !_semi.IsDead && _semi.Speed <= 2f &&
            GameUtils.IsWithinFlat(_semi.Position, At("M67.Airport"), ArrivalRadius);

        /// <summary>A point <paramref name="meters"/> behind a vehicle along its flattened heading.</summary>
        private static Vector3 BehindTheRig(Vector3 rig, Vector3 forward, float meters)
        {
            var along = new Vector3(forward.X, forward.Y, 0f);
            float length = along.Length();
            along = length < 0.01f ? new Vector3(0f, 1f, 0f) : along * (1f / length);
            return rig - along * meters;
        }

        /// <summary>
        /// Ice while somebody else is the player: into his car, then behind the rig, then after
        /// anything that comes for it. Every order is given once, on a change of what he should
        /// be doing or of whom he is after, and again only when the last one has visibly been
        /// lost - a boarding that has not happened, a fight he has dropped out of, a car that
        /// stopped while the rig drove away. Handing a driver a task every tick restarts it
        /// before he has done anything with it, which is the KeepDriving rule.
        /// </summary>
        private void KeepIce()
        {
            // His car is his to drive when he is the player. The next time he is not, he is
            // ordered afresh.
            if (Ctx.Crew.ActiveSlot == CrewSlot.Ice) { ForgetIce(); return; }
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            if (ice == null || !ice.Exists() || ice.IsDead || !EscortUsable) { ForgetIce(); return; }
            int now = Game.GameTime;

            if (!ice.IsInVehicle(_escort))
            {
                if (_iceOrder == IceOrder.Board && now - _iceOrderedAt < BoardRenewMs) return;
                // Already at the door and getting in: a fresh order would start him over.
                if (ice.Position.DistanceTo(_escort.Position) < CrewBoarding.AtTheDoor &&
                    Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE, ice)) return;
                var seat = IceSeat(ice);
                if (seat == VehicleSeat.None) return;
                Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Ice);
                if (ice.IsInVehicle()) ice.Task.LeaveVehicle();
                else CrewBoarding.RunAboard(ice, _escort, seat);
                OrderIce(IceOrder.Board, null);
                return;
            }

            // The player took the wheel of Ice's car and Ice rides beside him: a passenger's
            // drive-by is the shared support pass's to give.
            if (ice.SeatIndex != VehicleSeat.Driver) { ForgetIce(); return; }

            var threat = IceThreat(ice);
            if (threat != null)
            {
                bool fresh = _iceOrder != IceOrder.Fight || _iceTarget != threat;
                bool dropped = !ice.IsInCombat && now - _iceOrderedAt > SupportRefreshMs;
                if (!fresh && !dropped) return;
                Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Ice);
                CrewDriving.Configure(ice, CrewSlot.Ice, true);
                // Configure keeps a driver from leaning out on an ordinary run. This one is
                // meant to: shoot from the wheel (2) and use the car in the fight (52).
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ice, 2, true);
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, ice, 52, true);
                ice.Task.FightAgainst(threat);
                OrderIce(IceOrder.Fight, threat);
                return;
            }

            // A car that sits still while the rig pulls away has lost its task; a car parked
            // behind a parked rig has not.
            bool rigAway = _semi != null && _semi.Exists() && _semi.Speed > MovingSpeed &&
                _escort.Position.DistanceTo(_semi.Position) > EscortCatchUp;
            if (_escort.Speed < 1.5f && rigAway) { if (_escortStillSince == 0) _escortStillSince = now; }
            else _escortStillSince = 0;
            bool stalled = _escortStillSince != 0 && now - _escortStillSince > EscortStallMs;
            if (_iceOrder == IceOrder.Escort && !stalled) return;
            if (stalled) Logger.Info(Id + ": Ice's car had stopped while the rig drove on; reissued the escort.");
            Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Ice);
            bool urgent = Fighting || Game.Player.WantedLevel > 0;
            CrewDriving.Configure(ice, CrewSlot.Ice, urgent);
            ice.Task.StartVehicleMission(_escort, _semi, VehicleMissionType.Escort,
                CrewDriving.Speed(CrewSlot.Ice, urgent, _escort),
                (VehicleDrivingFlags)(urgent ? CrewDriving.EscapeFlags : CrewDriving.TrafficFlags), EscortGap, 0f, false);
            _escortStillSince = 0;
            OrderIce(IceOrder.Escort, null);
        }

        private void OrderIce(IceOrder order, Ped target) { _iceOrder = order; _iceTarget = target; _iceOrderedAt = Game.GameTime; }

        private void ForgetIce() { _iceOrder = IceOrder.None; _iceTarget = null; _escortStillSince = 0; }

        /// <summary>The wheel of his own car, or beside the player if the player took it.</summary>
        private VehicleSeat IceSeat(Ped ice)
        {
            var driver = _escort.GetPedOnSeat(VehicleSeat.Driver);
            if (driver == null || !driver.Exists() || driver == ice || driver.IsDead) return VehicleSeat.Driver;
            return _escort.IsSeatFree(VehicleSeat.Passenger) ? VehicleSeat.Passenger : VehicleSeat.None;
        }

        /// <summary>
        /// The nearest man worth leaving the escort for: one of this mission's hostiles, or,
        /// with the police on the crew, the nearest officer.
        /// </summary>
        private Ped IceThreat(Ped ice)
        {
            Ped best = null; float nearest = EngageMeters;
            foreach (var ped in Opposition)
            {
                if (ped == null || !ped.Exists() || ped.IsDead) continue;
                float distance = ped.Position.DistanceTo(ice.Position);
                if (distance < nearest) { best = ped; nearest = distance; }
            }
            if (best == null && Game.Player.WantedLevel > 0) best = CrewDriving.NearestPolice(ice, EngageMeters);
            return best;
        }

        private void Moved()
        {
            _transferred = true;
            // Accessible, not invisible. The line is careful about that and so is this.
            Ctx.State?.SetEvidence(FundsEvidence, EvidenceState.CopyHeld);
            Logger.Info(Id + ": the escrow is distributed. The money is reachable; it is not hidden.");
        }

        protected override void OnUpdate()
        {
            // A transfer in a stopped truck is a transfer at a roadblock. Say so rather than
            // letting the player wonder why the clock is not moving.
            if (_rolling && !_transferred && _semi != null && _semi.Exists())
            {
                if (_semi.Speed > MovingSpeed) _stalledAt = Game.GameTime;
                else if (_stalledAt > 0 && Game.GameTime - _stalledAt > StallPatienceMs)
                {
                    _stalledAt = Game.GameTime;
                    GameUtils.Subtitle("~y~Keep the rig moving. A parked truck on this road is a roadblock.", 4000);
                }
            }
            base.OnUpdate();
            // After the shared support pass, which takes control of every brother but never
            // orders a driver; and never once the mission has ended inside this update.
            if (Status == MissionStatus.Running) KeepIce();
        }

        protected override void OnCleanup()
        {
            ForgetIce();
            base.OnCleanup();
        }

        protected override void OnPassed()
        {
            if (!_rolling || !_transferred)
                throw new InvalidOperationException("The rig has to have rolled and the money has to have moved.");
            Release(_semi);
            Release(_trailer);
            // Ice may be sitting in it, and a car is not deleted under a brother.
            if (_escort != null && _escort.Exists()) Release(_escort);
        }
    }
}
