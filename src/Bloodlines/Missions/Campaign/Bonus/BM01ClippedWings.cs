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
    /// BM01 - "Clipped Wings". The first bonus mission, authored after the campaign and
    /// offered once the rest of it has been played (M70, and every solo job).
    ///
    /// Ron's design, September 22: drive north up the freeway to one of the Online bunker
    /// sites, take the men at the gate and the guard post, and a pilot gets into the Osprey
    /// and goes. A cutscene covers the takeoff, so gameplay opens with it already in the air.
    /// Guess drives a gun truck, Ice is in the back on the gun with a launcher on him, and
    /// Gohan rides beside Guess. The Osprey flies the road so it can be shot down, and a hull
    /// meter says how close it is. The payoff is cash and the right to buy one.
    ///
    /// Where the pieces come from:
    ///  * The site is the Paleto Forest bunker, <c>gr_case7_bunkerclosed</c>, read from the
    ///    installed archives: its door at (-782.5, 5935.1, 22.0), the apron at
    ///    (-774.7, 5937.1, 19.9) and its road signs on the coast road. The bunker itself is not
    ///    used; its exterior is loaded for the attempt and handed back after.
    ///  * The Osprey is the Mammoth Avenger, <c>avenger</c>, a tilt-rotor in the installed
    ///    game. It is flown by <see cref="ScriptedFlight"/> along road points snapped to lanes at
    ///    runtime, never by the AI.
    ///  * The flight line is a pole line: the telegraph poles beside the highway through Paleto
    ///    Bay and along the north coast, chained from the archives. Every point is an estimate
    ///    until it is surveyed; each is corrected to the nearest lane when the chase starts.
    ///  * The gun truck is a Karin Technical: three seats, the third on the gun. In GTA a
    ///    rocket launcher cannot be fired from a vehicle seat, so in the back Ice works the gun;
    ///    the launcher is his whenever he is on his feet.
    /// </summary>
    public sealed class BM01ClippedWings : PreparationOperation
    {
        public const string TruckModel = "technical";
        public const string OspreyModel = "avenger";
        public const string PilotModel = "s_m_y_blackops_01";
        public const string BunkerMap = "gr_case7_bunkerclosed";
        public const string GuardPostModel = "prop_portacabin01";
        public const string BarrierModel = "prop_barrier_work05";
        /// <summary>Rockets Ice carries. The hull takes seven.</summary>
        public const int Rockets = 10;
        /// <summary>How far a lane may be from its seed before the seed is used instead.</summary>
        public const float LaneSearch = 45f;
        /// <summary>How high it hovers over the apron while the crew gets back in the truck.</summary>
        public const float HoverHeight = 18f;
        /// <summary>Further than this from every brother, for longer than <see cref="EscapeGraceMs"/>, and it is gone.</summary>
        public const float EscapeMeters = 450f;
        public const int EscapeGraceMs = 15000;
        /// <summary>How close the aircraft has to be before a brother who is not being played shoots at it.</summary>
        public const float SupportRange = 160f;
        public const int FlightPoints = 17;

        private static readonly string[] GuardKeys = { "BM01.Guard1", "BM01.Guard2", "BM01.Guard3", "BM01.Guard4", "BM01.Guard5", "BM01.Yard1", "BM01.Yard2", "BM01.Yard3" };

        private readonly List<Ped> _guards = new List<Ped>();
        private readonly CrewBoarding _boarding = new CrewBoarding();
        private ScriptedMap _bunker;
        private Vehicle _truck, _osprey;
        private Ped _pilot;
        private ScriptedFlight _flight;
        private RaceRoute.Route _route;
        private ShootDownObjective _shootDown;
        private Vector3 _pad;
        private bool _liftPending, _hovering, _hunting;
        private int _farSince = -1, _nextSupport;

        public override string Id => "BM01";
        public override string Title => "Clipped Wings";

        public Vehicle Truck => _truck;
        public Vehicle Osprey => _osprey;
        public Ped Pilot => _pilot;
        public IReadOnlyList<Ped> Guards => _guards;
        public ScriptedFlight Flight => _flight;
        public RaceRoute.Route Route => _route;
        public ShootDownObjective Hull => _shootDown;
        public bool Hunting => _hunting;
        /// <summary>True while every brother is out of range and the escape clock is running.</summary>
        public bool Escaping => _farSince >= 0;

        /// <summary>
        /// The flight line is corrected onto lanes at runtime, so ground preparation must not
        /// move it first. The same goes for the pad: the Osprey is too big for the walkable
        /// query's answer, which is for a man.
        /// </summary>
        protected override string[] FixedSurfaces =>
            Enumerable.Range(1, FlightPoints).Select(i => "BM01.Flight" + i.ToString("00")).Concat(new[] { "BM01.Getaway", "BM01.Osprey" }).ToArray();

        protected override bool Setup()
        {
            _bunker = new ScriptedMap(BunkerMap);
            _bunker.Request();
            if (!BeginCrew(CrewSlot.Guess)) return false;

            _truck = Car(TruckModel, Lane("BM01.Truck"), Ctx.Locations.Heading("BM01.Truck"));
            if (!RequireAssets(_truck)) return false;
            RequireAsset(_truck, "The gun truck was destroyed.");
            CrewCar = _truck;
            // Seated outright, all three, and checked: a Technical has exactly three seats,
            // so there is no spare one for a boarding animation to go wrong in.
            foreach (var seat in SeatPlan())
            {
                var ped = Ctx.Crew.PedFor(seat.Key);
                if (ped == null || !ped.Exists()) continue;
                ped.SetIntoVehicle(_truck, seat.Value);
                if (!ped.IsInVehicle(_truck)) Logger.Warn(Id + ": " + Protagonist.Of(seat.Key).Handle + " did not take the truck's " + seat.Value + " seat.");
            }
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            if (ice != null && ice.Exists()) ice.Weapons.Give(WeaponHash.HomingLauncher, Rockets, false, true);

            BuildGate();
            foreach (var key in GuardKeys)
            {
                var guard = Enemy(key);
                if (guard != null) _guards.Add(guard);
            }
            if (_guards.Count == 0) { GameUtils.Notify("~r~BM01: nobody could be placed at the gate. See Bloodlines.log."); return false; }

            _osprey = Car(OspreyModel, At("BM01.Osprey") + new Vector3(0f, 0f, 2f), Ctx.Locations.Heading("BM01.Osprey"), false);
            if (!RequireAssets(_osprey)) { GameUtils.Notify("~r~BM01: the Osprey would not load."); return false; }
            // Down onto its gear, then held there: an aircraft's origin is not its wheels.
            Function.Call<bool>(Hash.SET_VEHICLE_ON_GROUND_PROPERLY, _osprey, 5f);
            _pad = _osprey.Position;
            // Parked, cold, and not the gate fight's to destroy: it is the next beat.
            _osprey.IsPositionFrozen = true;
            _osprey.IsInvincible = true;
            _osprey.IsEngineRunning = false;
            // No RequireAsset here: that fails the mission when the entity dies, and this one
            // dying is how the mission is won. The hull objective fails if it goes missing.

            _pilot = Person(PilotModel, "BM01.Pilot", friendly: false);
            if (!RequireAssets(_pilot)) { GameUtils.Notify("~r~BM01: the pilot would not load."); return false; }
            // He runs for the aircraft when the gate falls; until then nobody can shoot him
            // off the beat, and he does not wander into the fight.
            _pilot.IsInvincible = true;
            _pilot.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            _pilot.Task.StandStill(-1);
            return true;
        }

        /// <summary>The road under a seed, or the seed itself, reported, when no lane is near.</summary>
        private Vector3 Lane(string key)
        {
            var seed = At(key);
            if (GameUtils.NearestRoadNode(seed, LaneSearch, out var node, out _)) return node;
            Logger.Warn(Id + ": no lane within " + LaneSearch + " m of " + key + "; using the authored point.");
            return seed;
        }

        /// <summary>A guard post and two barriers across the bunker road, where the gate is.</summary>
        private void BuildGate()
        {
            var gate = At("BM01.Gate");
            float heading = Ctx.Locations.Heading("BM01.Gate");
            var post = WorkProp(GuardPostModel, GameUtils.OnGround(gate + new Vector3(-4f, 6f, 0f)));
            if (post != null) post.Heading = heading;
            else Logger.Warn(Id + ": the guard post could not be placed at " + gate + ".");
            foreach (var offset in new[] { new Vector3(0f, -2.5f, 0f), new Vector3(0f, 2.5f, 0f) })
            {
                var barrier = WorkProp(BarrierModel, GameUtils.OnGround(gate + offset));
                if (barrier != null) barrier.Heading = heading;
            }
        }

        /// <summary>Who sits where: Guess drives, Ice is on the gun, Gohan is beside Guess.</summary>
        public KeyValuePair<CrewSlot, VehicleSeat>[] SeatPlan()
        {
            var gun = GunSeat();
            return new[]
            {
                new KeyValuePair<CrewSlot, VehicleSeat>(CrewSlot.Guess, VehicleSeat.Driver),
                new KeyValuePair<CrewSlot, VehicleSeat>(CrewSlot.Gohan, VehicleSeat.RightFront),
                new KeyValuePair<CrewSlot, VehicleSeat>(CrewSlot.Ice, gun),
            };
        }

        /// <summary>The seat that is a gun, asked of the truck rather than assumed by number.</summary>
        private VehicleSeat GunSeat()
        {
            if (_truck != null && _truck.Exists())
                for (int i = 0; i < 3; i++)
                    if (Function.Call<bool>(Hash.IS_TURRET_SEAT, _truck, i)) return (VehicleSeat)i;
            return VehicleSeat.LeftRear;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Up the coast",
                new TravelObjective("Guess: drive the gun truck north up the coast highway to the Paleto Forest bunker", () => At("BM01.Approach"), 35f, () => _truck))
                .OwnedBy(CrewSlot.Guess)
                // Guess drives it whoever is being played. Ron switched to Ice on the way north
                // and the truck sat where it was, because nothing told Guess where to go until
                // the chase (September 22).
                .OnEnter(c => { DrivingDestination = () => At("BM01.Approach"); Radio("GOHAN", "Paleto Forest, off the coast road. Aegis never cleared the site. The Osprey's on the apron and the gate is manned.", "BM01_RADIO_01_GOHAN"); })
                .OnExit(c => { DrivingDestination = null; Fighting = true; Radio("ICE", "Gate and guard post. Take them, and keep an eye on that aircraft.", "BM01_RADIO_02_ICE"); });

            yield return new MissionStage("The gate",
                new KillTargetsObjective("Take out the men at the gate and the guard post", () => _guards))
                .AnyBrother()
                .OnExit(c => PlayTakeoff());

            yield return new MissionStage("Back in the truck",
                new ConditionObjective("Everyone in the gun truck: Guess drives, Ice on the gun, Gohan beside him", () => Aboard)
                { Marker = () => _truck != null && _truck.Exists() ? _truck.Position : _pad, MarkerRadius = 3f })
                .AnyBrother()
                // The stage opens in the same tick the takeoff scene starts, with the pilot still
                // walking to the aircraft. The lift waits for the scene to end.
                .OnEnter(c => { _boarding.Reset(); _liftPending = true; })
                .OnExit(c => BeginHunt());

            _shootDown = new ShootDownObjective("Bring down the Osprey - stay under it on the coast road", () => _osprey, Hunters);
            yield return new MissionStage("Bring it down", _shootDown)
                .AnyBrother()
                .OnExit(c => { _hunting = false; _flight?.Release(); });
        }

        /// <summary>Everyone the hull meter credits: the three of them, and the truck the gun is on.</summary>
        private IEnumerable<Entity> Hunters()
        {
            foreach (var hero in Protagonist.All)
            {
                var ped = Ctx.Crew.PedFor(hero.Slot);
                if (ped != null && ped.Exists()) yield return ped;
            }
            if (_truck != null && _truck.Exists()) yield return _truck;
        }

        private bool Aboard => _truck != null && _truck.Exists() &&
            Protagonist.All.All(hero => Ctx.Crew.PedFor(hero.Slot)?.IsInVehicle(_truck) == true);

        /// <summary>
        /// The pilot runs for the aircraft and takes it. Watching and skipping end the same
        /// way: he is in the seat either way, because the scene's own step seats him.
        /// </summary>
        private void PlayTakeoff()
        {
            if (_pilot == null || !_pilot.Exists() || _osprey == null || !_osprey.Exists()) return;
            var blocking = new SceneBlocking()
                .Then(new EnterVehicleStep(_pilot, _osprey, VehicleSeat.Driver) { TimeoutMs = 9000 })
                .Then(new ShotStep(4000, _osprey, new Vector3(-24f, 16f, 7f), _osprey, new Vector3(0f, 0f, 2f), 2f));
            // This runs inside a stage exit, and a throw there is a script error rather than a
            // failure. If the scene cannot finish, the pilot is seated anyway and the chase goes on.
            try
            {
                RequiredScene("takeoff", "The Osprey",
                    "The gate is down and the pilot reaches the Osprey; gameplay resumes with it already lifting off.", blocking);
            }
            catch (Exception ex)
            {
                Logger.Warn(Id + ": the takeoff scene could not complete (" + ex.Message + "); seating the pilot directly.");
                if (!_pilot.IsInVehicle(_osprey)) _pilot.SetIntoVehicle(_osprey, VehicleSeat.Driver);
            }
        }

        /// <summary>
        /// The takeoff the cutscene cut away from: seated, rotors turning, and off the ground.
        /// From here the script owns its position, so it never has to be trusted to fly itself.
        /// </summary>
        private void Lift()
        {
            if (_osprey == null || !_osprey.Exists()) return;
            if (_pilot != null && _pilot.Exists() && !_pilot.IsInVehicle(_osprey))
            {
                _pilot.SetIntoVehicle(_osprey, VehicleSeat.Driver);
                if (!_pilot.IsInVehicle(_osprey)) Logger.Warn(Id + ": the pilot could not be seated; the Osprey flies with its seat empty.");
            }
            if (_pilot != null && _pilot.Exists()) _pilot.BlockPermanentEvents = true;
            _osprey.IsPositionFrozen = false;
            _osprey.IsEngineRunning = true;
            // Rotors up, as the takeoff shot showed. Documented as 1 = hover; not yet seen in game.
            Function.Call(Hash.SET_VEHICLE_FLIGHT_NOZZLE_POSITION_IMMEDIATE, _osprey, 1f);
            Function.Call(Hash.SET_ENTITY_HAS_GRAVITY, _osprey, false);
            _osprey.Position = _pad + new Vector3(0f, 0f, 6f);
            Function.Call(Hash.SET_ENTITY_VELOCITY, _osprey, 0f, 0f, 0f);
            _hovering = true;
        }

        /// <summary>
        /// The chase: the flight line corrected onto its lanes now that the crew is here, the
        /// aircraft given to <see cref="ScriptedFlight"/>, and Guess told where to drive when
        /// somebody else is being played.
        /// </summary>
        private void BeginHunt()
        {
            _hovering = false;
            _route = RaceRoute.Build(Ctx.Locations, "BM01.Flight", "BM01.FlightTrail", "BM01.Getaway");
            _flight = new ScriptedFlight(_osprey, _route.Gates);
            _flight.Begin();
            // Kept a target while it flies: the hull meter decides when it comes down, and the
            // gunners need somebody in the seat to aim at.
            _osprey.IsInvincible = false;
            _hunting = true;
            _farSince = -1;
            DrivingDestination = () => _flight != null && !_flight.Finished
                ? _flight.Points[_flight.Next] - new Vector3(0f, 0f, ScriptedFlight.Altitude)
                : At("BM01.Getaway");
            Radio("GOHAN", "It's following the coast road east. Stay under it, and keep the gun on it.", "BM01_RADIO_03_GOHAN");
        }

        protected override void OnUpdate()
        {
            if (!Ctx.Cutscenes.IsActive)
            {
                if (_liftPending) { _liftPending = false; Lift(); }
                if (_hovering && !Aboard) _boarding.Update(Ctx.Crew, _truck, SeatPlan().Where(s => s.Key != Ctx.Crew.ActiveSlot && !Taken(s.Value)), Id);
                if (_hovering) Hover();
                if (_hunting) Hunt();
            }
            if (Status != MissionStatus.Running) return;
            base.OnUpdate();
        }

        /// <summary>A seat the player is sitting in is not one to send a brother to.</summary>
        private bool Taken(VehicleSeat seat)
        {
            var sitting = _truck != null && _truck.Exists() ? _truck.GetPedOnSeat(seat) : null;
            return sitting != null && sitting.Exists() && sitting == Game.Player.Character;
        }

        /// <summary>It climbs off the apron and holds, rotors turning, while the crew gets in.</summary>
        private void Hover()
        {
            if (_osprey == null || !_osprey.Exists()) return;
            float climb = _osprey.Position.Z < _pad.Z + HoverHeight ? 2.5f : 0f;
            Function.Call(Hash.SET_ENTITY_VELOCITY, _osprey, 0f, 0f, climb);
        }

        private void Hunt()
        {
            if (_flight == null || _osprey == null || !_osprey.Exists()) return;
            if (_osprey.IsDead || (_shootDown != null && _shootDown.Hull <= 0f)) { _flight.Release(); return; }
            var chaser = _truck != null && _truck.Exists() ? _truck.Position : Game.Player.Character.Position;
            _flight.Update(chaser);
            if (_flight.Finished) { Fail("The Osprey made it past the end of the coast road and got away."); return; }

            float nearest = Protagonist.All.Select(h => Ctx.Crew.PedFor(h.Slot)).Where(p => p != null && p.Exists() && !p.IsDead)
                .Select(p => p.Position.DistanceTo(_osprey.Position)).DefaultIfEmpty(float.MaxValue).Min();
            if (nearest > EscapeMeters)
            {
                if (_farSince < 0) { _farSince = Game.GameTime; GameUtils.Notify("~y~It's getting away.~s~ Get the truck back under it."); }
                else if (Game.GameTime - _farSince > EscapeGraceMs) { Fail("The Osprey got away. Stay under it on the coast road."); return; }
            }
            else _farSince = -1;

            // The brothers the player is not playing do their jobs: the gun in the bed and the
            // passenger window, at the man in the seat. Reissued on a slow clock, never every
            // frame, which would restart the task before a shot is fired.
            if (Game.GameTime < _nextSupport || _pilot == null || !_pilot.Exists()) return;
            _nextSupport = Game.GameTime + 2500;
            foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Gohan })
            {
                if (slot == Ctx.Crew.ActiveSlot) continue;
                var actor = Ctx.Crew.PedFor(slot);
                if (actor == null || !actor.Exists() || actor.IsDead || !actor.IsInVehicle()) continue;
                if (actor.Position.DistanceTo(_osprey.Position) > SupportRange) continue;
                if (actor.SeatIndex == GunSeat()) actor.Task.VehicleShootAtPed(_pilot);
                else DriveBy(actor, _pilot);
            }
        }

        protected override void OnPassed()
        {
            Release(_truck);
        }

        protected override void OnCleanup()
        {
            _hunting = false; _hovering = false; _liftPending = false;
            _flight?.Release();
            if (_pilot != null && _pilot.Exists()) _pilot.IsInvincible = false;
            DrivingDestination = null;
            _guards.Clear();
            _bunker?.Release();
            base.OnCleanup();
        }
    }
}
