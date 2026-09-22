using System;
using System.Collections.Generic;
using Bloodlines.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Crew
{
    public enum CompanionState
    {
        /// <summary>Tailing the active character on foot.</summary>
        Follow,
        Driving,
        Convoy,
        Independent,
        Disembarking,

        /// <summary>Fighting; left alone until the fight ends.</summary>
        Combat,

        /// <summary>Holding a position — split-approach missions and set-ups.</summary>
        Hold,

        /// <summary>Getting into, or riding in, the active character's vehicle.</summary>
        Vehicle,

        /// <summary>A mission has taken direct control; the controller keeps its hands off.</summary>
        Scripted,

        /// <summary>Too far behind to path back; being repositioned.</summary>
        TeleportRecovery,

        /// <summary>Dead or dying.</summary>
        Downed
    }

    /// <summary>
    /// Explicit state machine for the two characters the player is not currently
    /// controlling.
    ///
    /// Generic GTA companion AI is not good enough for scripted missions. The failure
    /// that actually ends runs is not a firefight lost — it is a companion who will
    /// not get into the getaway car while the timer runs out. So vehicle boarding is
    /// handled deliberately here: task them in, and if the car is already moving or
    /// they are too far to reach it, warp them into a seat rather than watching them
    /// jog after it.
    /// </summary>
    public sealed class CompanionController
    {
        private const float VehicleTaskRange = 60f;
        private const float VehicleWarpSpeed = 8f;
        private const int VehicleTaskTimeoutMs = 12000;

        private bool _sharedVehicle;
        public bool RequireSharedVehicle
        {
            get => _sharedVehicle;
            set
            {
                if (value && !_sharedVehicle) { Driver.Clear(); Convoy.Clear(); }
                _sharedVehicle = value;
            }
        }
        private readonly HashSet<CrewSlot> _separatedByRecovery = new HashSet<CrewSlot>();
        public void KeepRecoverySeparate(CrewSlot slot) => _separatedByRecovery.Add(slot);
        public bool SeparatedByRecovery(CrewSlot slot) => _separatedByRecovery.Contains(slot);
        public void SeparateAfterRecovery(CrewSlot active)
        {
            foreach (CrewSlot slot in Enum.GetValues(typeof(CrewSlot)))
                if (slot != active) _separatedByRecovery.Add(slot);
            _separatedByRecovery.Remove(active);
        }
        private bool _missionActive;
        public bool MissionActive
        {
            get => _missionActive;
            set
            {
                if (value == _missionActive) return;
                if (value) { Life.Clear(); _separatedByRecovery.Clear(); ClearOrders(); }
                Military.Clear();
                _missionActive = value;
            }
        }
        public CompanionLife Life { get; } = new CompanionLife();
        /// <summary>
        /// What a brother does while he is standing by. Only ever animates a slot a
        /// mission has registered through StandBy, or one this controller is itself
        /// holding, so a scripted task can never be overwritten by ambience.
        /// </summary>
        public CompanionPresence Presence { get; } = new CompanionPresence();
        public MilitaryResponse Military { get; }
        private bool _independent = true;
        private bool _rideAlong = true;
        public bool RideAlong
        {
            get => _rideAlong;
            set { if (_rideAlong == value && _travelChoices.Count == 0) return; _travelChoices.Clear(); _rideAlong = value; ClearOrders(); Driver.Clear(); Convoy.Clear(); _states.Clear(); _stateSince.Clear(); _boarding.Clear(); }
        }
        private readonly Dictionary<CrewSlot, bool> _travelChoices = new Dictionary<CrewSlot, bool>();
        public bool RidesAlong(CrewSlot slot) => _travelChoices.TryGetValue(slot, out var ride) ? ride : _rideAlong;
        public bool SetTravelChoice(CrewSlot slot, bool ride)
        {
            if (!SetHangout(slot, true)) return false;
            _travelChoices[slot] = ride;
            return true;
        }
        private readonly Dictionary<CrewSlot, bool> _hangouts = new Dictionary<CrewSlot, bool>();
        public bool HasIndividualOrders => _hangouts.Count > 0 || _travelChoices.Count > 0;
        public bool IsHangingOut(CrewSlot slot) => _hangouts.TryGetValue(slot, out var follow) ? follow : !_independent;
        public bool SetHangout(CrewSlot slot, bool follow)
        {
            if (MissionActive || HoldPosition || RequireSharedVehicle || _scripted.Contains(slot)) return false;
            // Phone invitations and travel choices replace this brother's standing order.
            ClearOrder(slot);
            _hangouts[slot] = follow;
            _separatedByRecovery.Remove(slot);
            Life.Suspend(slot); Driver.Forget(slot); Convoy.Forget(slot); Refresh(slot);
            return true;
        }
        public bool IndependentFreeRoam
        {
            get => _independent;
            set { _separatedByRecovery.Clear(); if (value == _independent && _hangouts.Count == 0 && _travelChoices.Count == 0) return; _hangouts.Clear(); _travelChoices.Clear(); _independent = value; ClearOrders(); Life.Clear(); Driver.Clear(); Convoy.Clear(); _states.Clear(); _stateSince.Clear(); _threats.Clear(); _lastScan.Clear(); _boarding.Clear(); }
        }
        public CompanionConvoy Convoy { get; } = new CompanionConvoy();
        public CompanionDriver Driver { get; } = new CompanionDriver();
        private readonly ModConfig _config;
        private readonly Dictionary<CrewSlot, CompanionState> _states = new Dictionary<CrewSlot, CompanionState>();
        private readonly Dictionary<CrewSlot, int> _stateSince = new Dictionary<CrewSlot, int>();
        private readonly Dictionary<CrewSlot, int> _flippedSince = new Dictionary<CrewSlot, int>();
        private readonly HashSet<CrewSlot> _scripted = new HashSet<CrewSlot>();

        private readonly Dictionary<CrewSlot, Ped> _threats = new Dictionary<CrewSlot, Ped>();
        private readonly Dictionary<CrewSlot, int> _lastScan = new Dictionary<CrewSlot, int>();
        private readonly Dictionary<CrewSlot, Boarding> _boarding = new Dictionary<CrewSlot, Boarding>();
        private sealed class Boarding { public Vehicle Vehicle; public VehicleSeat Seat; }

        // ---- Standing orders (Ron, September 17: "a way to command our partners").
        private readonly Dictionary<CrewSlot, CrewOrder> _orders = new Dictionary<CrewSlot, CrewOrder>();
        private readonly Dictionary<CrewSlot, Vehicle> _orderVehicles = new Dictionary<CrewSlot, Vehicle>();
        private Ped _leader; private int _leaderAfootSince, _leaderRidingSince, _leaderLastVehicle;
        /// <summary>
        /// How long a seated brother sits tight while the player is on foot beside the
        /// vehicle. Stepping out of the driver's seat to walk around to the gun bed is a
        /// seat change, not a departure, and the driver used to bail out the moment the
        /// player's feet touched the ground.
        /// </summary>
        public const int SeatShuffleMs = 8000;
        public const float SeatShuffleMeters = 10f;
        /// <summary>
        /// The player riding in a seat that is not the driver's, for this long, wants a
        /// driver. Shorter than this is the engine shuffling him across from the passenger
        /// door, and a brother sent for the wheel then would arrive to find it taken.
        /// </summary>
        public const int WantsDriverMs = 1500;

        public CompanionController(ModConfig config)
        {
            _config = config;
            Military = new MilitaryResponse(Life.Wanted);
            Convoy.AllowCatchupTeleport = slot => !SeparatedByRecovery(slot);
            Driver.HasBoardingPassengers = vehicle =>
            {
                foreach (var request in _boarding.Values)
                    if (request.Vehicle.Handle == vehicle.Handle && vehicle.IsSeatFree(request.Seat)) return true;
                return false;
            };
            // An ordered driver holds the vehicle still when told to, and while the player
            // is out of it beside it. A convoy driver has no order and is never held.
            Driver.HoldStill = (slot, vehicle) =>
            {
                var order = OrderOf(slot);
                if (order == CrewOrder.PullOver || order == CrewOrder.GetOut || order == CrewOrder.HoldHere) return true;
                bool leaderAboard = _leader != null && _leader.Exists() && _leader.IsInVehicle(vehicle);
                if (order != CrewOrder.None && order != CrewOrder.DriveToWaypoint && !leaderAboard) return true;
                return LeaderStepping(vehicle, _leader);
            };
        }

        /// <summary>The standing order this brother is on, if any.</summary>
        public CrewOrder OrderOf(CrewSlot slot) => _orders.TryGetValue(slot, out var order) ? order : CrewOrder.None;
        /// <summary>The vehicle a vehicle order is about.</summary>
        public Vehicle OrderVehicle(CrewSlot slot) => _orderVehicles.TryGetValue(slot, out var vehicle) && vehicle != null && vehicle.Exists() ? vehicle : null;
        public bool HasOrders => _orders.Count > 0;

        /// <summary>
        /// Give a brother an order. Every order is also an invitation - a man told to do
        /// something is with you - so the two that change the hangout are applied here and
        /// clear, and the rest stand until they are satisfied or replaced. Refused inside
        /// a mission, a hold, a required shared ride or while a script owns him, the same
        /// gates a phone hangout has.
        /// </summary>
        public bool Order(CrewSlot slot, CrewOrder order, Vehicle subject)
        {
            if (order == CrewOrder.None) { ClearOrder(slot); return true; }
            if (MissionActive || HoldPosition || RequireSharedVehicle || _scripted.Contains(slot)) return false;
            if (CrewOrders.AboutVehicle(order) && (subject == null || !subject.Exists())) return false;
            if (order == CrewOrder.OwnThing) { ClearOrder(slot); return SetHangout(slot, false); }
            if (!SetHangout(slot, true)) return false;
            if (order == CrewOrder.FollowMe) { ClearOrder(slot); return true; }
            _orders[slot] = order;
            if (CrewOrders.AboutVehicle(order)) _orderVehicles[slot] = subject; else _orderVehicles.Remove(slot);
            _boarding.Remove(slot);
            if (!CrewOrders.HoldsWheel(order)) Driver.Forget(slot);
            Convoy.Forget(slot);
            Refresh(slot);
            Logger.Info(Protagonist.Of(slot).Handle + " ordered: " + CrewOrders.Label(order));
            return true;
        }
        public void ClearOrder(CrewSlot slot) { _orders.Remove(slot); _orderVehicles.Remove(slot); }
        public void ClearOrders() { _orders.Clear(); _orderVehicles.Clear(); }

        /// <summary>
        /// The player is on foot beside a vehicle he was just in: changing seats, not leaving.
        /// It has to be the vehicle he was last seen in; a player walking up to a brother's
        /// car for a reunion was never in it, and the driver gets out to meet him as before.
        /// </summary>
        private bool LeaderStepping(Vehicle vehicle, Ped leader)
        {
            if (vehicle == null || !vehicle.Exists() || leader == null || !leader.Exists() || leader.IsInVehicle()) return false;
            if (vehicle.Handle != _leaderLastVehicle) return false;
            if (_leaderAfootSince == 0 || Game.GameTime - _leaderAfootSince > SeatShuffleMs) return false;
            return vehicle.Position.DistanceTo(leader.Position) <= SeatShuffleMeters;
        }

        /// <summary>The player has settled into a seat that is not the driver's.</summary>
        private bool LeaderWantsDriver(Vehicle vehicle, Ped leader)
        {
            if (vehicle == null || !vehicle.Exists() || leader == null || !leader.Exists() || !leader.IsInVehicle(vehicle)) return false;
            if (vehicle.GetPedOnSeat(VehicleSeat.Driver)?.Handle == leader.Handle) return false;
            return _leaderRidingSince != 0 && Game.GameTime - _leaderRidingSince >= WantsDriverMs;
        }

        /// <summary>The vehicle a boarding is about: the ordered one, else the player's.</summary>
        private Vehicle TargetVehicle(CrewSlot slot, Ped leader)
        {
            if (CrewOrders.AboutVehicle(OrderOf(slot)))
            {
                var ordered = OrderVehicle(slot);
                if (ordered != null) return ordered;
            }
            return leader?.CurrentVehicle;
        }

        /// <summary>
        /// What a standing order says he should be doing this tick, or null when the order
        /// has nothing to add and the ordinary decision applies. Orders that are satisfied
        /// clear themselves here.
        /// </summary>
        private CompanionState? DecideOrdered(CrewSlot slot, Ped companion, Ped leader)
        {
            var order = OrderOf(slot);
            if (order == CrewOrder.None || MissionActive) return null;
            var vehicle = OrderVehicle(slot);
            if (CrewOrders.AboutVehicle(order) && (vehicle == null || !vehicle.IsDriveable)) { ClearOrder(slot); return null; }
            bool aboard = vehicle != null && companion.IsInVehicle(vehicle);
            bool driving = aboard && vehicle.GetPedOnSeat(VehicleSeat.Driver)?.Handle == companion.Handle;
            bool leaderAboard = vehicle != null && leader != null && leader.Exists() && leader.IsInVehicle(vehicle);
            var ride = companion.CurrentVehicle;
            bool safeToLeave = ride != null && ride.Exists() && !ride.IsInAir && ride.HeightAboveGround < 3f && ride.Speed < 2f;
            bool rideDriver = ride != null && ride.Exists() && ride.GetPedOnSeat(VehicleSeat.Driver)?.Handle == companion.Handle;
            switch (order)
            {
                case CrewOrder.HoldHere:
                    if (companion.IsInVehicle())
                    {
                        // Stop first if he is driving, then get out; a passenger waits for the stop.
                        if (safeToLeave) return CompanionState.Disembarking;
                        if (rideDriver) { if (!Driver.Owns(slot, companion)) Driver.Arm(slot, companion); return CompanionState.Driving; }
                        return CompanionState.Vehicle;
                    }
                    return FindThreat(slot, companion, leader) != null ? CompanionState.Combat : CompanionState.Hold;
                case CrewOrder.GetOut:
                    if (!aboard) { _orders[slot] = CrewOrder.HoldHere; _orderVehicles.Remove(slot); return FindThreat(slot, companion, leader) != null ? CompanionState.Combat : CompanionState.Hold; }
                    if (safeToLeave) return CompanionState.Disembarking;
                    if (driving) { if (!Driver.Owns(slot, companion)) Driver.Arm(slot, companion); return CompanionState.Driving; }
                    return CompanionState.Vehicle;
                case CrewOrder.GetIn:
                    if (aboard)
                    {
                        // With the player aboard too the ordinary shared-ride rules take over.
                        if (leaderAboard) { ClearOrder(slot); return null; }
                        return FindThreat(slot, companion, leader) != null && !driving ? CompanionState.Combat : CompanionState.Vehicle;
                    }
                    if (HasSeatFor(slot, vehicle, companion, leader)) return CompanionState.Vehicle;
                    ClearOrder(slot); return null;
                case CrewOrder.ManTheGun:
                    if (aboard && CrewOrders.IsTurretSeat(vehicle, companion.SeatIndex))
                    {
                        if (leaderAboard) { ClearOrder(slot); return null; }
                        return FindThreat(slot, companion, leader) != null ? CompanionState.Combat : CompanionState.Vehicle;
                    }
                    // In the wrong seat: out at the next safe moment and round to the gun.
                    if (aboard) return safeToLeave ? CompanionState.Disembarking : CompanionState.Vehicle;
                    if (CrewOrders.FreeTurretSeat(vehicle) != VehicleSeat.None) return CompanionState.Vehicle;
                    ClearOrder(slot); return null;
                case CrewOrder.TakeTheWheel:
                case CrewOrder.DriveToWaypoint:
                case CrewOrder.PullOver:
                    if (driving) { if (!Driver.Owns(slot, companion)) Driver.Arm(slot, companion); return CompanionState.Driving; }
                    if (aboard) return safeToLeave ? CompanionState.Disembarking : CompanionState.Vehicle;
                    if (vehicle.IsSeatFree(VehicleSeat.Driver)) return CompanionState.Vehicle;
                    // Somebody has the wheel. The player taking it back ends the order; anyone
                    // else, and the order stands until the seat opens.
                    var holder = vehicle.GetPedOnSeat(VehicleSeat.Driver);
                    if (holder != null && leader != null && holder.Handle == leader.Handle) { ClearOrder(slot); return null; }
                    return CompanionState.Follow;
            }
            return null;
        }

        /// <summary>Companions hold position instead of following (split-approach missions).</summary>
        public bool HoldPosition { get; set; }
        // Identity survives the engine changing relationship groups on player handover.
        public Func<Ped, bool> IsCrewMember { get; set; }

        private bool IsFriendly(Ped ped, Ped companion, Ped leader)
        {
            return ped == null || !ped.Exists() || ped.IsDead || ped.Handle == companion.Handle ||
                (leader != null && ped.Handle == leader.Handle) ||
                (Game.Player.Character != null && ped.Handle == Game.Player.Character.Handle) ||
                (IsCrewMember != null && IsCrewMember(ped)) || ped.RelationshipGroup == companion.RelationshipGroup;
        }

        public CompanionState StateOf(CrewSlot slot)
        {
            return _states.TryGetValue(slot, out var state) ? state : CompanionState.Follow;
        }

        /// <summary>A mission takes direct control of one companion until it releases it.</summary>
        public void TakeControl(CrewSlot slot)
        {
            Life.Suspend(slot);
            Presence.Release(slot);
            Driver.Forget(slot);
            Convoy.Forget(slot);
            ClearOrder(slot);
            _scripted.Add(slot);
            SetState(slot, CompanionState.Scripted);
        }

        public void ReleaseControl(CrewSlot slot)
        {
            _scripted.Remove(slot);
            Presence.Release(slot);
            SetState(slot, CompanionState.Follow);
        }

        public void ReleaseAll()
        {
            _scripted.Clear();
            Presence.Clear();
            ClearOrders();
            Driver.Clear();
            Convoy.Clear();
            RequireSharedVehicle = false;
        }

        public void Update(CrewSlot slot, Ped companion, Ped leader)
        {
            if (companion == null || !companion.Exists()) return;
            if (leader != null && leader.Exists())
            {
                // Two clocks on the player: how long he has been on foot, and how long he
                // has been riding in a seat that is not the driver's.
                if (_leader == null || !_leader.Exists() || _leader.Handle != leader.Handle) { _leaderAfootSince = 0; _leaderRidingSince = 0; _leaderLastVehicle = 0; }
                _leader = leader;
                if (leader.IsInVehicle()) { _leaderAfootSince = 0; _leaderLastVehicle = leader.CurrentVehicle.Handle; }
                else if (_leaderAfootSince == 0) _leaderAfootSince = Game.GameTime;
                bool riding = leader.IsInVehicle() && leader.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver)?.Handle != leader.Handle;
                if (!riding) _leaderRidingSince = 0; else if (_leaderRidingSince == 0) _leaderRidingSince = Game.GameTime;
            }

            // Stop an already-running native combat task if its victim became the
            // player or another crew member after a switch/respawn.
            var combatTarget = Function.Call<Ped>(Hash.GET_PED_TARGET_FROM_COMBAT_PED, companion, 0);
            if (combatTarget != null && combatTarget.Exists() && !combatTarget.IsDead && IsFriendly(combatTarget, companion, leader))
            {
                companion.Task.ClearAllImmediately();
                Driver.Restart(slot);
                Refresh(slot);
            }
            UprightIfFlipped(slot, companion);
            var state = Decide(slot, companion, leader);

            // A slot with nothing recorded has never been applied. StateOf() reports
            // Follow for it as a convenience, and taking that at face value was a
            // crash: a first tick that decides Follow would match the phantom state,
            // skip SetState, and then read _stateSince for a key never written.
            // Deploying the crew on foot hit this every time.
            CompanionState recorded;
            if (!_states.TryGetValue(slot, out recorded) || state != recorded)
            {
                SetState(slot, state);
                Apply(slot, state, companion, leader);
                return;
            }

            // A few states need upkeep rather than a one-shot task.
            switch (state)
            {
                case CompanionState.Independent:
                    Life.Update(slot, companion);
                    break;
                case CompanionState.Convoy:
                    Convoy.IsCrewMember = IsCrewMember;
                    Convoy.Update(slot, companion, leader, Driver);
                    break;
                case CompanionState.Driving:
                    Driver.Update(slot, companion);
                    break;
                case CompanionState.Vehicle:
                    MaintainVehicle(slot, companion, leader);
                    break;
                case CompanionState.Combat:
                    if (StateAge(slot) >= 1000 && _threats.TryGetValue(slot, out var desired) &&
                        (!companion.IsInCombat || combatTarget == null || !combatTarget.Exists() || combatTarget.Handle != desired.Handle))
                    {
                        Engage(slot, companion, leader);
                        _stateSince[slot] = Game.GameTime;
                    }
                    break;
                case CompanionState.Hold:
                    Presence.IsCrewMember = IsCrewMember;
                    if (!companion.IsInVehicle()) Presence.Update(slot, companion, leader, true);
                    break;
                case CompanionState.Scripted:
                    // Only reaches a slot a mission registered as standing by. Station does
                    // that; a mission handing a brother real work does not, and this is
                    // then a no-op for him.
                    Presence.IsCrewMember = IsCrewMember;
                    if (Presence.IsStandingBy(slot)) Presence.Update(slot, companion, leader, true);
                    break;
                case CompanionState.Follow:
                    Presence.IsCrewMember = IsCrewMember;
                    // A follower settles only while the man he is with is standing still,
                    // and the moment the leader walks off Presence hands him back and the
                    // follow task is re-issued below.
                    if (!companion.IsInVehicle() && Presence.Update(slot, companion, leader, false)) break;
                    if (StateAge(slot) > 8000)
                    {
                        Apply(slot, state, companion, leader);
                        _stateSince[slot] = Game.GameTime;
                    }
                    break;
            }
        }

        private CompanionState Decide(CrewSlot slot, Ped companion, Ped leader)
        {
            if (companion.IsDead) return CompanionState.Downed;
            if (leader != null && leader.Exists() && companion.Position.DistanceTo(leader.Position) < 65f)
                _separatedByRecovery.Remove(slot);
            if (_scripted.Contains(slot)) return CompanionState.Scripted;
            // Separate approach actors must not board the leader's car or abandon
            // their assignment because another character starts a fight.
            if (HoldPosition) return CompanionState.Hold;
            var ordered = DecideOrdered(slot, companion, leader);
            if (ordered.HasValue) return ordered.Value;
            // An individually dismissed or separate-car companion leaves only after a safe stop.
            // Normal independent companions still keep an existing shared ride.
            if (!MissionActive && !RequireSharedVehicle && ((_hangouts.TryGetValue(slot, out var hanging) && !hanging) ||
                (IsHangingOut(slot) && _travelChoices.TryGetValue(slot, out var rideAlong) && !rideAlong)) &&
                leader != null && leader.Exists() && leader.IsInVehicle() && companion.IsInVehicle(leader.CurrentVehicle))
            {
                var ride = leader.CurrentVehicle;
                if ((ride.Model.IsCar || ride.Model.IsBike || ride.Model.IsHelicopter) &&
                    !ride.IsInAir && ride.HeightAboveGround < 3f && ride.Speed < 2f)
                { Driver.Forget(slot); Convoy.Forget(slot); return CompanionState.Disembarking; }
            }
            // A shared ride is a commitment, even while free-roam independence is on.
            if (leader != null && leader.Exists() && leader.IsInVehicle() && companion.IsInVehicle(leader.CurrentVehicle))
            {
                if (leader.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver)?.Handle == companion.Handle)
                { if (!Driver.Owns(slot, companion)) Driver.Arm(slot, companion); return CompanionState.Driving; }
                return FindThreat(slot, companion, leader) != null ? CompanionState.Combat : CompanionState.Vehicle;
            }
            // The player out of the vehicle and still beside it is changing seats, not
            // leaving. Nobody gets out; the driver holds still (Driver.HoldStill).
            if (!MissionActive && companion.IsInVehicle() && LeaderStepping(companion.CurrentVehicle, leader))
            {
                if (companion.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver)?.Handle == companion.Handle)
                { if (!Driver.Owns(slot, companion)) Driver.Arm(slot, companion); return CompanionState.Driving; }
                return FindThreat(slot, companion, leader) != null ? CompanionState.Combat : CompanionState.Vehicle;
            }
            if (!IsHangingOut(slot) && !MissionActive)
            {
                var ride = companion.CurrentVehicle;
                if (ride != null && !(ride.Model.IsCar || ride.Model.IsBike) && ride.GetPedOnSeat(VehicleSeat.Driver)?.Handle == companion.Handle)
                { if (!Driver.Owns(slot, companion)) Driver.Arm(slot, companion); return CompanionState.Driving; }
                if ((ride == null || ride.GetPedOnSeat(VehicleSeat.Driver)?.Handle != companion.Handle) && FindThreat(slot, companion, leader) != null) return CompanionState.Combat;
                return CompanionState.Independent;
            }
            // A completed rendezvous must relinquish the old convoy assignment.
            if (StateOf(slot) == CompanionState.Disembarking && !companion.IsInVehicle())
            { Driver.Forget(slot); Convoy.Forget(slot); }
            // A nearby on-foot follower helps the crew before trying to board a car
            // or returning to an old convoy. Mission holds and required rides win.
            if (!companion.IsInVehicle() && !RequireSharedVehicle && FindThreat(slot, companion, leader) != null)
                return CompanionState.Combat;
            if (!MissionActive && IsHangingOut(slot) && !RequireSharedVehicle && !companion.IsInVehicle() &&
                leader != null && leader.Exists() && leader.IsInVehicle() &&
                (!RidesAlong(slot) || !HasSeatFor(slot, leader.CurrentVehicle, companion, leader))) return CompanionState.Convoy;
            if (!MissionActive && !RequireSharedVehicle && IsHangingOut(slot) && leader != null && leader.Exists())
            {
                if (StateOf(slot) == CompanionState.Disembarking && companion.IsInVehicle() && StateAge(slot) < 15000) return CompanionState.Disembarking;
                var vehicle = companion.CurrentVehicle;
                if (vehicle != null && vehicle.Exists() && vehicle.GetPedOnSeat(VehicleSeat.Driver)?.Handle == companion.Handle)
                {
                    bool canLeave = (vehicle.Model.IsCar || vehicle.Model.IsBike || vehicle.Model.IsHelicopter) &&
                        !vehicle.IsInAir && vehicle.HeightAboveGround < 3f && vehicle.Speed < 2f;
                    if (!leader.IsInVehicle() && canLeave && companion.Position.DistanceTo(leader.Position) < 65f &&
                        !LeaderStepping(vehicle, leader) && !CrewOrders.HoldsWheel(OrderOf(slot)))
                    { Driver.Forget(slot); return CompanionState.Disembarking; }
                    if (!Driver.Owns(slot, companion)) Driver.Arm(slot, companion);
                    return CompanionState.Driving;
                }
                if (!companion.IsInVehicle() && companion.Position.DistanceTo(leader.Position) >= 150f) return CompanionState.Convoy;
            }
            if (!RequireSharedVehicle && leader != null && leader.Exists() &&
                (Convoy.HasRide(slot) || (leader.IsInVehicle() && !companion.IsInVehicle() &&
                    companion.Position.DistanceTo(leader.Position) >= 200f))) return CompanionState.Convoy;
            if (Driver.Owns(slot, companion)) return CompanionState.Driving;

            if (companion.IsInVehicle() && companion.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver)?.Handle != companion.Handle && FindThreat(slot, companion, leader) != null) return CompanionState.Combat;
            if (leader != null && leader.Exists() && leader.IsInVehicle())
            {
                var vehicle = leader.CurrentVehicle;
                if (vehicle != null && vehicle.Exists() && HasSeatFor(slot, vehicle, companion, leader)) return CompanionState.Vehicle;
            }

            if (FindThreat(slot, companion, leader) != null) return CompanionState.Combat;



            if (leader != null && leader.Exists() && (!leader.IsInVehicle() || RequireSharedVehicle) &&
                companion.Position.DistanceTo(leader.Position) > _config.CompanionLeashDistance && !SeparatedByRecovery(slot))
            {
                return CompanionState.TeleportRecovery;
            }

            return CompanionState.Follow;
        }

        private void Apply(CrewSlot slot, CompanionState state, Ped companion, Ped leader)
        {
            switch (state)
            {
                case CompanionState.Follow:
                    companion.Task.ClearAll();
                    companion.AlwaysKeepTask = true;
                    companion.BlockPermanentEvents = true;
                    if (leader != null && leader.Exists())
                    {
                        companion.Task.FollowToOffsetFromEntity(leader, new Vector3(1.5f, -1.5f, 0f),
                            2.0f, -1, 4.0f, true);
                    }
                    break;

                case CompanionState.Independent:
                    Life.IsCrewMember = IsCrewMember;
                    Life.Update(slot, companion);
                    break;
                case CompanionState.Disembarking:
                    companion.Task.LeaveVehicle();
                    break;
                case CompanionState.Hold:
                    // A vehicle station means stay in that seat. An on-foot guard
                    // task implicitly orders a passenger to leave after a handover.
                    if (!companion.IsInVehicle())
                    {
                        Presence.IsCrewMember = IsCrewMember;
                        // A man holding a position for ten minutes is the thing Ron
                        // reported. Standing guard is still the fallback; it is no longer
                        // the only thing that ever happens to him.
                        if (!Presence.Update(slot, companion, leader, true))
                        {
                            companion.Task.ClearAll();
                            companion.Task.GuardCurrentPosition();
                        }
                    }
                    companion.AlwaysKeepTask = true;
                    break;

                case CompanionState.Convoy:
                    Convoy.IsCrewMember = IsCrewMember;
                    Convoy.Update(slot, companion, leader, Driver);
                    break;
                case CompanionState.Driving:
                    Driver.Update(slot, companion);
                    break;
                case CompanionState.Vehicle:
                    BoardVehicle(slot, companion, leader);
                    break;

                case CompanionState.TeleportRecovery:
                    Recover(companion, leader);
                    break;

                case CompanionState.Combat:
                    Engage(slot, companion, leader);
                    break;
                case CompanionState.Scripted:
                case CompanionState.Downed:
                    break;
            }
        }

        private Ped FindThreat(CrewSlot slot, Ped companion, Ped leader)
        {
            if (_lastScan.TryGetValue(slot, out var last) && Game.GameTime - last < 500)
                return _threats.TryGetValue(slot, out var cached) && !IsFriendly(cached, companion, leader) ? cached : null;
            _lastScan[slot] = Game.GameTime;
            _threats.Remove(slot);
            Ped best = null; float bestDistance = float.MaxValue;
            foreach (var ped in World.GetNearbyPeds(companion, 140f))
            {
                if (IsFriendly(ped, companion, leader)) continue;
                var victim = Function.Call<Ped>(Hash.GET_PED_TARGET_FROM_COMBAT_PED, ped, 0);
                bool attacksCrew = victim != null && victim.Exists() && IsCrewMember?.Invoke(victim) == true ||
                    Function.Call<bool>(Hash.IS_PED_IN_COMBAT, ped, companion) ||
                    (leader != null && leader.Exists() && Function.Call<bool>(Hash.IS_PED_IN_COMBAT, ped, leader));
                bool activeHostile = leader != null && leader.Exists() &&
                    ped.GetRelationshipWithPed(leader) == Relationship.Hate &&
                    (leader.IsShooting || leader.IsInCombat || ped.IsShooting || ped.IsInCombat);
                bool engagedPolice = Game.Player.WantedLevel > 0 && leader != null && leader.Exists() && leader.IsShooting &&
                    ped.RelationshipGroup.Hash == Game.GenerateHash("COP") && ped.Position.DistanceTo(leader.Position) <= 100f;
                if (!attacksCrew && !activeHostile && !engagedPolice) continue;
                float distance = ped.Position.DistanceTo(companion.Position);
                if (distance < bestDistance) { best = ped; bestDistance = distance; }
            }
            if (best != null) _threats[slot] = best;
            return best;
        }

        private void Engage(CrewSlot slot, Ped companion, Ped leader)
        {
            if (!_threats.TryGetValue(slot, out var target) || IsFriendly(target, companion, leader)) return;
            if (companion.IsInVehicle())
            {
                if (companion.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver)?.Handle == companion.Handle) return;
                Function.Call(Hash.SET_PED_COMBAT_ATTRIBUTES, companion, 2, true);
                companion.Weapons.Give(WeaponHash.MicroSMG, 300, true, true);
                companion.Task.VehicleShootAtPed(target);
            }
            else
            {
                Driver.Forget(slot); Convoy.Forget(slot);
                companion.Task.ClearAll(); companion.AlwaysKeepTask = true;
                companion.Task.FightAgainst(target);
            }
            Logger.Debug(Protagonist.Of(slot).Handle + " engaging hostile " + target.Handle);
        }

        private void BoardVehicle(CrewSlot slot, Ped companion, Ped leader)
        {
            var vehicle = TargetVehicle(slot, leader);
            if (vehicle == null || !vehicle.Exists() || companion.IsInVehicle(vehicle)) return;
            var seat = FreeSeat(slot, vehicle, companion, leader);
            if (seat == VehicleSeat.None) return;
            _boarding[slot] = new Boarding { Vehicle = vehicle, Seat = seat };
            _stateSince[slot] = Game.GameTime;
            float distance = companion.Position.DistanceTo(vehicle.Position);
            if (!SeparatedByRecovery(slot) && RequireSharedVehicle && (distance > VehicleTaskRange || (distance > 25f && vehicle.Speed > VehicleWarpSpeed)))
            {
                companion.Task.ClearAllImmediately();
                companion.Task.WarpIntoVehicle(vehicle, seat);
                Logger.Debug("Boarding fallback: vehicle already out of reach.");
                return;
            }
            // No warp flags: nearby companions walk to their own reserved door.
            companion.Task.ClearAll();
            companion.Task.EnterVehicle(vehicle, seat, 20000, 2f, EnterVehicleFlags.None);
            Logger.Debug(Protagonist.Of(slot).Handle + " walking to vehicle seat " + seat);
        }

        private void MaintainVehicle(CrewSlot slot, Ped companion, Ped leader)
        {
            var vehicle = TargetVehicle(slot, leader);
            if (vehicle == null || !vehicle.Exists()) return;
            if (companion.IsInVehicle(vehicle)) { _boarding.Remove(slot); return; }
            if (!_boarding.TryGetValue(slot, out var request) || request.Vehicle.Handle != vehicle.Handle ||
                !vehicle.IsSeatFree(request.Seat))
            {
                _boarding.Remove(slot);
                BoardVehicle(slot, companion, leader);
                return;
            }
            int age = StateAge(slot);
            if (age < VehicleTaskTimeoutMs) return;
            if (age < 20000 && Function.Call<bool>(Hash.IS_PED_GETTING_INTO_A_VEHICLE, companion)) return;
            if (!RequireSharedVehicle || SeparatedByRecovery(slot))
            {
                _boarding.Remove(slot);
                companion.Task.FollowToOffsetFromEntity(leader, new Vector3(0f,-5f,0f), 3f, -1, 8f, true);
                _stateSince[slot] = Game.GameTime;
                return;
            }
            companion.Task.ClearAllImmediately();
            companion.Task.WarpIntoVehicle(vehicle, request.Seat);
            _stateSince[slot] = Game.GameTime;
            Logger.Debug("Boarding fallback: normal entry failed after " + age + "ms.");
        }

        /// <summary>
        /// A brother's car on its roof and stopped for three seconds is rolled back
        /// onto its wheels (Ron, September 12: he switched to a brother lying
        /// upside down in a car). The engine gives an AI driver no way to do it.
        /// </summary>
        private void UprightIfFlipped(CrewSlot slot, Ped companion)
        {
            var vehicle = companion.CurrentVehicle;
            if (vehicle == null || !vehicle.Exists() || vehicle.GetPedOnSeat(VehicleSeat.Driver) != companion) { _flippedSince.Remove(slot); return; }
            bool flipped = Function.Call<bool>(Hash.IS_ENTITY_UPSIDEDOWN, vehicle);
            if (!flipped || vehicle.Speed > 1f) { _flippedSince.Remove(slot); return; }
            if (!_flippedSince.TryGetValue(slot, out int since)) { _flippedSince[slot] = Game.GameTime; return; }
            if (Game.GameTime - since < 3000) return;
            _flippedSince.Remove(slot);
            var position = vehicle.Position;
            vehicle.Rotation = new Vector3(0f, 0f, vehicle.Heading);
            vehicle.Position = new Vector3(position.X, position.Y, position.Z + 1.2f);
            vehicle.PlaceOnGround();
            Logger.Info(slot + "'s car was on its roof; rolled back onto its wheels.");
        }

        private static void Recover(Ped companion, Ped leader)
        {
            if (leader == null || !leader.Exists()) return;

            companion.Task.ClearAllImmediately();
            companion.Position = leader.Position - leader.ForwardVector * 3f;
            companion.Heading = leader.Heading;
        }

        private bool HasSeatFor(CrewSlot slot, Vehicle vehicle, Ped companion, Ped leader)
        {
            return companion.IsInVehicle(vehicle) || FreeSeat(slot, vehicle, companion, leader) != VehicleSeat.None;
        }

        private bool Reserved(CrewSlot slot, Vehicle vehicle, VehicleSeat seat)
        {
            foreach (var pair in _boarding)
                if (pair.Key != slot && pair.Value.Vehicle.Handle == vehicle.Handle && pair.Value.Seat == seat)
                    return true;
            return false;
        }

        /// <summary>
        /// Which seat a boarding brother takes. It used to be the lowest free index, which
        /// is never the driver's seat (index minus one) and always the turret last (the
        /// highest): Ron's gunner seat stayed empty while two brothers sat in the cab. Now:
        /// the seat his order names, or nothing; the wheel when the player has settled into
        /// another seat; then a turret before a plain passenger seat, because a gun in the
        /// bed is for using; then the rest by index.
        /// </summary>
        private VehicleSeat FreeSeat(CrewSlot slot, Vehicle vehicle, Ped companion, Ped leader)
        {
            var order = OrderOf(slot);
            var named = CrewOrders.HoldsWheel(order) ? VehicleSeat.Driver
                      : order == CrewOrder.ManTheGun ? CrewOrders.FreeTurretSeat(vehicle) : VehicleSeat.None;
            if (named != VehicleSeat.None) return vehicle.IsSeatFree(named) && !Reserved(slot, vehicle, named) ? named : VehicleSeat.None;
            if (LeaderWantsDriver(vehicle, leader) && vehicle.IsSeatFree(VehicleSeat.Driver) && !Reserved(slot, vehicle, VehicleSeat.Driver))
                return VehicleSeat.Driver;
            int capacity = Function.Call<int>(Hash.GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS, vehicle);
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < capacity; i++)
                {
                    var seat = (VehicleSeat)i;
                    if (!vehicle.IsSeatFree(seat) || Reserved(slot, vehicle, seat)) continue;
                    if (CrewOrders.IsTurretSeat(vehicle, seat) == (pass == 0)) return seat;
                }
            return VehicleSeat.None;
        }

        /// <summary>
        /// Milliseconds since this slot's state was last set. A slot with nothing
        /// recorded reads as infinitely old, so every caller re-applies rather than
        /// indexing a key that is not there.
        /// </summary>
        private int StateAge(CrewSlot slot)
        {
            int since;
            return _stateSince.TryGetValue(slot, out since) ? Game.GameTime - since : int.MaxValue;
        }

        private void SetState(CrewSlot slot, CompanionState state)
        {
            if (state != CompanionState.Vehicle) _boarding.Remove(slot);
            _states[slot] = state;
            _stateSince[slot] = Game.GameTime;
            Logger.Debug(Protagonist.Of(slot).Handle + " -> " + state);
        }

        public void Refresh(CrewSlot slot)
        {
            _states.Remove(slot);
            _stateSince.Remove(slot);
            _boarding.Remove(slot);
            _threats.Remove(slot);
            _lastScan.Remove(slot);
        }

        public void Forget(CrewSlot slot)
        {
            _separatedByRecovery.Remove(slot);
            Life.Suspend(slot);
            Refresh(slot);
            Driver.Forget(slot);
            Convoy.Forget(slot);
            _scripted.Remove(slot);
        }
    }
}
