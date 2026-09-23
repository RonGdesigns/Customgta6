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
    /// <summary>Shared ownership, road response and custody for M31-M35.</summary>
    public abstract class PreparationOperation : DesertOperation
    {
        protected readonly List<Ped> Opposition = new List<Ped>();
        /// <summary>
        /// The dots over this mission's hostiles. Owned here rather than added by hand,
        /// because a blip added with AddBlip outlives the ped: Ron saw dots sitting over
        /// corpses in mission after mission, each of which told him there was still a fight
        /// where there was not.
        /// </summary>
        protected readonly TargetBlips Blips = new TargetBlips();
        protected RoleTracks Roles;
        protected Vehicle CrewCar;
        private readonly Dictionary<Ped, int> _boardingStarted = new Dictionary<Ped, int>();
        private readonly Dictionary<Ped, int> _boarding = new Dictionary<Ped, int>();
        private readonly List<Tuple<Vehicle, Ped, Ped>> _response = new List<Tuple<Vehicle, Ped, Ped>>();
        private readonly List<Blip> _responseBlips = new List<Blip>();
        private int _orders;
        private int _tracked = -1;
        private bool _wasFighting;
        protected bool Fighting;
        /// <summary>
        /// What this mission's hostiles know. Reaction used to be this one Fighting flag
        /// and a combat task re-issued every tick; between them a guard being shot at
        /// did nothing, which is what Ron reported in M31, M33 and M37. Awareness owns
        /// their tasking now, and missions can report their own stimuli to it.
        /// </summary>
        protected GuardAwareness Awareness { get; private set; }
        protected Func<Vector3> DrivingDestination;
        private readonly Dictionary<CrewSlot, Vector3> _startPoints = new Dictionary<CrewSlot, Vector3>();

        protected override bool RejoinsIdleCrew => true;

        /// <summary>
        /// A role that is on its way, working, taking cover or extracting is his job. So is
        /// watching from a point the mission sent him to. Watching from where he started is not.
        /// </summary>
        protected override bool HasOwnWork(CrewSlot slot)
        {
            var track = Roles?.Peek(slot);
            if (track == null) return false;
            switch (track.State)
            {
                case RoleState.Idle: return false;
                case RoleState.Working: return !track.ActionComplete;
                case RoleState.Observing:
                    return !(_startPoints.TryGetValue(slot, out var start) && track.Point.DistanceTo(start) < 1.5f);
                default: return true;
            }
        }

        protected override void OnRejoin(CrewSlot slot) { Roles?.Peek(slot)?.Stop(); }
        /// <summary>
        /// A man in the opposition the brothers must never be sent to fight: M50's watchmen,
        /// who fail the mission dead. Support orders pass over him even while the mission is
        /// fighting, because a support order is a gun, not a stun gun.
        /// </summary>
        protected virtual bool Spared(Ped ped) => false;
        /// <summary>
        /// Keys that sit on built geometry — a pier deck, a platform, a vessel — rather
        /// than on the terrain. The ground preparation asks the engine for walkable ground
        /// near each point, and over water that answer is the water beside the structure:
        /// Ron found M40's kits and laptop under the pier for exactly that reason. A key
        /// named here keeps its authored height.
        /// </summary>
        protected virtual string[] FixedSurfaces => new string[0];

        protected bool BeginCrew(CrewSlot active)
        {
            string site = Id == "M31" ? "M31.Senora" : Id;
            if (Id == "M31") BunkerSite.LoadMaps();
            if (!MissionSites.Prepare(Ctx.Locations, Id, FixedSurfaces) ||
                !Ctx.Crew.Deploy(active, At(site + ".Start"), Ctx.Locations.Heading(site + ".Start"))) return false;
            ProtectCrew(); ApplyBibleSetting();
            foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Gohan, CrewSlot.Guess })
                Station(slot, At(site + "." + slot + "Start"));
            Ctx.Crew.CompanionsHoldPosition = true;
            Roles = new RoleTracks(Ctx.Crew, () => Opposition);
            Awareness = new GuardAwareness(() => Protagonist.All
                .Select(h => Ctx.Crew.PedFor(h.Slot))
                .Where(p => p != null && p.Exists() && !p.IsDead));
            foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Gohan, CrewSlot.Guess })
            {
                Roles.For(slot).Observe(At(site + "." + slot + "Start"), At(site + "." + slot + "Start"));
                // Where he starts, not a post: once there is nothing for him, he comes to the player.
                Unpost(slot);
                _startPoints[slot] = At(site + "." + slot + "Start");
            }
            return true;
        }
        protected Vehicle CrewTransport(string key)
        {
            var car = Ctx.Vans?.Spawn(At(key), Ctx.Locations.Heading(key));
            if (car == null) car = Car("granger", At(key), Ctx.Locations.Heading(key));
            else Track(car);
            if (car != null) { car.IsPersistent = true; RequireAsset(car, "The crew's extraction car was destroyed."); }
            return car;
        }
        protected Ped Person(string modelName, string key, bool friendly = true)
        {
            var model = new Model(modelName);
            try
            {
                if (!GameUtils.RequestModel(model)) return null;
                var ped = Track(World.CreatePed(model, At(key), Ctx.Locations.Heading(key)));
                if (ped == null || !ped.Exists()) return null;
                ped.IsPersistent = true; ped.BlockPermanentEvents = true;
                ped.MaxHealth = 600; ped.Health = 600;
                ped.RelationshipGroup = friendly ? Ctx.Crew.PedFor(CrewSlot.Guess).RelationshipGroup : World.AddRelationshipGroup("BLOODLINES_RAMOSESCORT");
                return ped;
            }
            finally { model.MarkAsNoLongerNeeded(); }
        }
        protected Ped Enemy(string key) => Register(Guard(At(key)), key);

        /// <summary>
        /// A hostile at a position the caller has already settled onto its real surface - a
        /// tunnel floor, a deck, anything the engine's walkable query would answer with the
        /// ground above or beside it. The navmesh snap is skipped outright rather than
        /// widened, because underground its answer is not merely imprecise, it is the street.
        /// <paramref name="key"/> is only used for the log line.
        /// </summary>
        protected Ped EnemyAt(Vector3 point, string key) =>
            Register(Guard(point, WeaponHash.CarbineRifle, true), key);

        private Ped Register(Ped ped, string key)
        {
            // One guard that will not spawn is a thinner fight, not a dead mission.
            // M32 and M39 both refused to start over a single post (Ron, September 13).
            if (ped == null) { Logger.Error("Could not place guard " + key + "; the encounter continues without him. Survey that key."); return null; }
            Opposition.Add(ped);
            Blips.Attach(ped, BlipColor.Red, "Armed guard");
            return ped;
        }
        protected void Establish(string phase, string title, string reason, params Entity[] subjects)
        {
            var blocking = new SceneBlocking();
            foreach (var subject in subjects)
                if (subject != null && subject.Exists()) blocking.Then(ShotStep.Low(1800, subject, 5, 3, 2));
            RequiredScene(phase, title, reason, blocking);
        }
        protected Prop Equipment(string model, string key)
        {
            var prop = WorkProp(model, At(key));
            if (!RequireAssets(prop)) throw new InvalidOperationException("Cannot create equipment at " + key);
            prop.Heading = Ctx.Locations.Heading(key); return prop;
        }
        protected void SaveCargo(Prop item, Entity carrier, Vector3 offset)
        {
            Function.Call(Hash.SET_ENTITY_COLLISION, item, false, false);
            if (!StowPropStep.Stow(item, carrier, offset)) throw new InvalidOperationException("Cargo could not attach to its carrier.");
        }
        protected bool Attached(Prop item, Entity carrier) => item != null && item.Exists() &&
            carrier != null && carrier.Exists() && Function.Call<bool>(Hash.IS_ENTITY_ATTACHED_TO_ENTITY, item, carrier);
        protected void Carry(Prop item, CrewSlot slot)
        {
            Function.Call(Hash.SET_ENTITY_COLLISION, item, false, false);
            var actor = Ctx.Crew.PedFor(slot);
            if (!CarryPropStep.Attach(actor, item, new Vector3(.12f, .02f, -.02f), new Vector3(0, 90, 0)))
                throw new InvalidOperationException("The case could not be placed in the carrier's hand.");
        }
        protected bool Board(Ped actor, Vehicle vehicle, VehicleSeat seat)
        {
            if (actor == null || !actor.Exists() || actor.IsDead || vehicle == null || !vehicle.Exists()) return false;
            if ((int)seat + 2 > Function.Call<int>(Hash.GET_VEHICLE_MODEL_NUMBER_OF_SEATS, vehicle.Model.Hash))
            { Fail("The extraction vehicle does not have the required seat. Retry with a compatible crew car."); return false; }
            if (actor.IsInVehicle(vehicle) && actor.SeatIndex == seat)
            { _boarding.Remove(actor); _boardingStarted.Remove(actor); return true; }
            // The player sits where he chooses. Asking him for one particular seat, and
            // counting nothing else, soft-locked M41 when Ice took the other rear seat and
            // failed M54 when he took a brother's (the September 22 audit).
            if (actor == Game.Player.Character) { if (actor.IsInVehicle(vehicle)) { _boarding.Remove(actor); _boardingStarted.Remove(actor); return true; } return false; }
            // Told to board, he is the mission's again.
            if (Ctx?.Crew != null)
                foreach (var hero in Protagonist.All)
                    if (Ctx.Crew.PedFor(hero.Slot) == actor && Ctx.Crew.CompanionAI.IsRejoining(hero.Slot)) Ctx.Crew.CompanionAI.TakeControl(hero.Slot);
            if (actor.IsInVehicle(vehicle))
            { actor.Task.LeaveVehicle(); return false; }
            // A man who died in the seat is not a passenger. M35's gunner is killed in the gun
            // seat Ice is about to take, and the corpse used to fail the mission as an
            // occupied seat.
            var occupant = vehicle.GetPedOnSeat(seat);
            if (occupant != null && occupant.Exists() && occupant.IsDead) { GameUtils.SafeDelete(occupant); }
            // And the seat the player took is not his brother's any more: take another.
            if (occupant != null && occupant.Exists() && occupant == Game.Player.Character) seat = OtherFreeSeat(vehicle, seat);
            if (vehicle.Speed > 1.5f) { _boardingStarted.Remove(actor); return false; }
            if (actor.Position.DistanceTo(vehicle.Position) > 25f)
            { _boardingStarted.Remove(actor); if (!_boarding.TryGetValue(actor,out int walked) || Game.GameTime-walked>6000) { CrewBoarding.RunTo(actor, vehicle.Position); _boarding[actor] = Game.GameTime; } return false; }
            if (!_boardingStarted.TryGetValue(actor, out int started)) _boardingStarted[actor] = Game.GameTime;
            else if (Game.GameTime - started > 45000)
            { Fail("A passenger could not reach the extraction seat. Retry with the vehicle stopped clear of obstacles."); return false; }
            if (!_boarding.TryGetValue(actor, out int when) || Game.GameTime - when > 8000)
            {
                if (!vehicle.IsSeatFree(seat)) { Fail("An extraction seat is occupied. Clear the required seats and retry."); return false; }
                CrewBoarding.RunAboard(actor, vehicle, seat); _boarding[actor] = Game.GameTime;
            }
            return actor.IsInVehicle(vehicle);
        }

        private static VehicleSeat OtherFreeSeat(Vehicle vehicle, VehicleSeat taken)
        {
            int seats = Function.Call<int>(Hash.GET_VEHICLE_MODEL_NUMBER_OF_SEATS, vehicle.Model.Hash);
            for (int i = 0; i < Math.Max(0, seats - 1); i++)
                if ((VehicleSeat)i != taken && vehicle.IsSeatFree((VehicleSeat)i)) return (VehicleSeat)i;
            return vehicle.IsSeatFree(VehicleSeat.Driver) ? VehicleSeat.Driver : taken;
        }
        protected bool BoardBrothers(Vehicle car)
        {
            Roles?.Release(); Ctx.Crew.CompanionsHoldPosition = false;
            bool all = true;
            foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Gohan, CrewSlot.Guess })
            {
                Ctx.Crew.CompanionAI.TakeControl(slot);
                var seat = slot == CrewSlot.Guess ? VehicleSeat.Driver : slot == CrewSlot.Ice ? VehicleSeat.LeftRear : VehicleSeat.RightRear;
                if (!Board(Ctx.Crew.PedFor(slot), car, seat)) all = false;
            }
            return all;
        }
        protected void ResponseCar(string key, Vector3 destination)
        {
            var car = Car("mesa", At(key), Ctx.Locations.Heading(key), false);
            if (!RequireAssets(car)) throw new InvalidOperationException("The response vehicle could not load at " + key);
            // Seats, not guard posts: two guards requested at one point can be handed the
            // same standing space and the second create fails, which is what made M35
            // report that its convoy could not load.
            var driver = Occupant(car, VehicleSeat.Driver); var gunner = Occupant(car, VehicleSeat.Passenger);
            if (!RequireAssets(driver, gunner)) throw new InvalidOperationException("The response crew could not be seated at " + key);
            car.IsEngineRunning = true;
            Opposition.Add(driver); Opposition.Add(gunner); _response.Add(Tuple.Create(car, driver, gunner));
            var blip = Track(car.AddBlip()); blip.Color = BlipColor.Red; blip.Name = "Response vehicle"; _responseBlips.Add(blip);
            driver.Task.DriveTo(car, destination, 14f, 26f, (DrivingStyle)CrewDriving.TrafficFlags);
        }
        /// <summary>How long a driver may sit still before the order is treated as lost.</summary>
        public const int DriveStallMs = 3000;
        /// <summary>A slow refresh, so a task something else cleared is eventually reissued.</summary>
        public const int DriveRefreshMs = 9000;
        /// <summary>Cruise the AI aims for on a mission route.</summary>
        public const float DriveCruiseSpeed = 34f;
        /// <summary>What each response car was last sent at, so it is not re-sent every cycle.</summary>
        private readonly Dictionary<Vehicle, Tuple<Ped, Vector3>> _responseOrders = new Dictionary<Vehicle, Tuple<Ped, Vector3>>();
        /// <summary>What each brother was last told to fight, so the order is given once.</summary>
        private readonly Dictionary<CrewSlot, Ped> _supportTargets = new Dictionary<CrewSlot, Ped>();
        private readonly Dictionary<CrewSlot, int> _supportOrderedAt = new Dictionary<CrewSlot, int>();
        /// <summary>
        /// How long a brother's order against the same man stands before it is given again. A
        /// drive-by from a seat need not report as combat, so a clock is the only way to tell a
        /// standing order from a lost one.
        /// </summary>
        public const int SupportRefreshMs = 8000;
        /// <summary>How far a pursued brother may move before a response car is given his new position.</summary>
        public const float ResponseRerouteMeters = 30f;
        private Vector3 _driveOrder;
        private int _driveOrderAt;
        private float _driveRemaining = -1f;

        /// <summary>
        /// Keep Guess going to one place. He used to be handed DriveTo on a fixed clock
        /// whatever he was doing, which restarts the drive task and is a good way to make
        /// a driver hesitate short of the destination. He is reissued only when the route
        /// changes, when he has genuinely stalled, or on a slow refresh that recovers a
        /// task another system cleared.
        /// </summary>
        private void KeepDriving(Ped guess, Vector3 destination)
        {
            var car = guess.CurrentVehicle;
            if (car == null || !car.Exists()) return;
            float remaining = car.Position.DistanceTo(destination);
            bool newRoute = _driveOrderAt == 0 || _driveOrder.DistanceTo(destination) > 6f;
            bool progressing = _driveRemaining < 0f || _driveRemaining - remaining > 4f;
            bool stalled = !newRoute && !progressing && car.Speed < 1.5f && Game.GameTime - _driveOrderAt > DriveStallMs;
            bool stale = Game.GameTime - _driveOrderAt > DriveRefreshMs;
            if (progressing) _driveRemaining = remaining;
            if (!newRoute && !stalled && !stale) return;
            Ctx.Crew.CompanionAI.TakeControl(CrewSlot.Guess);
            CrewDriving.Configure(guess, CrewSlot.Guess, Fighting);
            guess.Task.DriveTo(car, destination, 12f, DriveCruiseSpeed, (DrivingStyle)CrewDriving.TrafficFlags);
            _driveOrder = destination; _driveOrderAt = Game.GameTime; _driveRemaining = remaining;
            if (stalled) Logger.Info(Id + ": the driver had stopped short of his destination; reissued the route.");
        }

        protected void RetreatResponse()
        {
            Fighting = false;
            foreach (var blip in _responseBlips) if (blip != null && blip.Exists()) blip.Delete();
            _responseBlips.Clear();
            foreach (var unit in _response)
            {
                if (unit.Item2 != null && unit.Item2.Exists()) unit.Item2.Task.ClearAll();
                if (unit.Item3 != null && unit.Item3.Exists()) unit.Item3.Task.ClearAll();
                if (!unit.Item1.Exists() || unit.Item1.IsDead || !unit.Item2.Exists() || unit.Item2.IsDead || !unit.Item2.IsInVehicle(unit.Item1)) continue;
                var from = unit.Item1.Position; var player = Game.Player.Character.Position;
                var away = from + new Vector3(from.X >= player.X ? 500f : -500f, from.Y >= player.Y ? 500f : -500f, 0f);
                unit.Item2.Task.DriveTo(unit.Item1, away, 20f, 28f, (DrivingStyle)CrewDriving.TrafficFlags);
            }
        }
        protected static void DriveBy(Ped actor, Ped target)
        {
            if (actor.SeatIndex == VehicleSeat.Driver) return;
            if (actor.SeatIndex == VehicleSeat.LeftRear && actor.CurrentVehicle != null &&
                (actor.CurrentVehicle.Model == new Model("halftrack") || actor.CurrentVehicle.Model == new Model("technical")))
            { actor.Task.VehicleShootAtPed(target); return; }
            Function.Call(Hash.TASK_DRIVE_BY, actor, target, 0, 0f, 0f, 0f, 100f, 30, false,
                Game.GenerateHash("FIRING_PATTERN_BURST_FIRE_DRIVEBY"));
        }
        protected void TickSupport()
        {
            foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Gohan, CrewSlot.Guess })
                if (slot != Ctx.Crew.ActiveSlot && Ctx.Crew.CompanionAI.StateOf(slot) != CompanionState.Scripted &&
                    !Ctx.Crew.CompanionAI.IsRejoining(slot))
                    Ctx.Crew.CompanionAI.TakeControl(slot);
            Roles?.Update();
            if (Awareness != null)
            {
                // Every frame, deliberately above the order throttle below: awareness
                // bounds its own work with a review interval and a per-tick slice, and a
                // hostile who is shot at cannot wait two and a half seconds to notice.
                if (Opposition.Count != _tracked) { Awareness.TrackAll(Opposition); _tracked = Opposition.Count; }
                // A mission declaring a fight means these men are already engaged. That
                // reaches them as a radio call rather than bypassing the model.
                if (Fighting && !_wasFighting)
                {
                    var engaged = Ctx.Crew.PedFor(Ctx.Crew.ActiveSlot);
                    if (engaged != null && engaged.Exists()) Awareness.ReportToAll(Stimulus.RadioCall, engaged.Position);
                }
                _wasFighting = Fighting;
                Awareness.Update();
                Blips.Update();
            }
            if (Game.GameTime < _orders) return;
            _orders = Game.GameTime + 2500;
            if (DrivingDestination != null)
            {
                var guess = Ctx.Crew.PedFor(CrewSlot.Guess);
                if (Ctx.Crew.ActiveSlot != CrewSlot.Guess && guess != null && guess.IsInVehicle() && guess.SeatIndex == VehicleSeat.Driver)
                    KeepDriving(guess, DrivingDestination());
            }
            if (!Fighting) return;
            var targets = Protagonist.All.Select(h => Ctx.Crew.PedFor(h.Slot)).Where(p => p != null && p.Exists() && !p.IsDead).ToArray();
            foreach (var unit in _response)
            {
                if (!unit.Item1.Exists() || unit.Item1.IsDead) continue;
                var target = targets.OrderBy(p => p.Position.DistanceTo(unit.Item1.Position)).FirstOrDefault();
                if (target == null) continue;
                // Re-issued only when the target changes or has moved on, never every cycle:
                // handing DriveTo to a driver on a clock restarts the drive before he makes
                // any way, which is the KeepDriving rule (the September 22 audit).
                _responseOrders.TryGetValue(unit.Item1, out var last);
                bool fresh = last == null || last.Item1 != target || last.Item2.DistanceTo(target.Position) > ResponseRerouteMeters;
                if (unit.Item2.Exists() && !unit.Item2.IsDead && unit.Item2.IsInVehicle(unit.Item1))
                {
                    if (unit.Item1.Position.DistanceTo(target.Position) < 45f && !target.IsInVehicle()) unit.Item2.Task.LeaveVehicle();
                    else if (fresh) unit.Item2.Task.DriveTo(unit.Item1, target.Position, 16f, 27f, (DrivingStyle)CrewDriving.TrafficFlags);
                }
                if (unit.Item3.Exists() && !unit.Item3.IsDead && unit.Item3.IsInVehicle(unit.Item1))
                {
                    if (unit.Item1.Position.DistanceTo(target.Position) < 50f && !target.IsInVehicle()) unit.Item3.Task.LeaveVehicle();
                    else if (fresh) DriveBy(unit.Item3, target);
                }
                if (fresh) _responseOrders[unit.Item1] = Tuple.Create(target, target.Position);
            }
            foreach (var slot in new[] { CrewSlot.Guess, CrewSlot.Gohan, CrewSlot.Ice })
            {
                // A brother who has rejoined the player fights under the companion controller.
                if (slot == Ctx.Crew.ActiveSlot || Ctx.Crew.CompanionAI.IsRejoining(slot)) continue;
                var actor = Ctx.Crew.PedFor(slot);
                if (actor == null || !actor.Exists() || actor.IsDead || (actor.IsInVehicle() && actor.SeatIndex == VehicleSeat.Driver)) continue;
                var threat = Opposition.Where(p => p != null && p.Exists() && !p.IsDead && !Spared(p) && p.Position.DistanceTo(actor.Position) < 110f)
                    .OrderBy(p => p.Position.DistanceTo(actor.Position)).FirstOrDefault();
                if (threat == null) { _supportTargets.Remove(slot); continue; }
                // Once per target, and again only if he has dropped out of the fight. It was
                // re-issued every cycle, which restarts the task before he can act on it: the
                // same fault that left M31's guards standing still (the September 22 audit).
                if (_supportTargets.TryGetValue(slot, out var current) && current == threat &&
                    (actor.IsInCombat || Game.GameTime - (_supportOrderedAt.TryGetValue(slot, out int at) ? at : 0) < SupportRefreshMs)) continue;
                _supportTargets[slot] = threat; _supportOrderedAt[slot] = Game.GameTime;
                if (actor.IsInVehicle()) DriveBy(actor, threat); else actor.Task.FightAgainst(threat);
            }
        }
        protected override void OnUpdate()
        {
            // A subclass that failed the mission earlier in this tick calls down here anyway.
            // Support takes control of every brother, so running it after the release is how
            // Gohan was left held in free roam (BM01, September 22).
            if (Status != MissionStatus.Running) return;
            TickSupport();
            base.OnUpdate();
        }
        protected override void OnCleanup() { _driveOrderAt = 0; _driveRemaining = -1f; _responseOrders.Clear(); _supportTargets.Clear(); _supportOrderedAt.Clear(); Awareness?.Clear(); Roles?.Release(); _boarding.Clear(); _boardingStarted.Clear(); DrivingDestination = null; base.OnCleanup(); }
    }
}
