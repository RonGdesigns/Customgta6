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
                if (value) { Life.Clear(); _separatedByRecovery.Clear(); }
                Military.Clear();
                _missionActive = value;
            }
        }
        public CompanionLife Life { get; } = new CompanionLife();
        public MilitaryResponse Military { get; }
        private bool _independent = true;
        public bool IndependentFreeRoam
        {
            get => _independent;
            set { _separatedByRecovery.Clear(); if (value == _independent) return; _independent = value; Life.Clear(); Driver.Clear(); Convoy.Clear(); _states.Clear(); _stateSince.Clear(); }
        }
        public CompanionConvoy Convoy { get; } = new CompanionConvoy();
        public CompanionDriver Driver { get; } = new CompanionDriver();
        private readonly ModConfig _config;
        private readonly Dictionary<CrewSlot, CompanionState> _states = new Dictionary<CrewSlot, CompanionState>();
        private readonly Dictionary<CrewSlot, int> _stateSince = new Dictionary<CrewSlot, int>();
        private readonly HashSet<CrewSlot> _scripted = new HashSet<CrewSlot>();

        private readonly Dictionary<CrewSlot, Ped> _threats = new Dictionary<CrewSlot, Ped>();
        private readonly Dictionary<CrewSlot, int> _lastScan = new Dictionary<CrewSlot, int>();
        private readonly Dictionary<CrewSlot, Boarding> _boarding = new Dictionary<CrewSlot, Boarding>();
        private sealed class Boarding { public Vehicle Vehicle; public VehicleSeat Seat; }

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
            Driver.Forget(slot);
            Convoy.Forget(slot);
            _scripted.Add(slot);
            SetState(slot, CompanionState.Scripted);
        }

        public void ReleaseControl(CrewSlot slot)
        {
            _scripted.Remove(slot);
            SetState(slot, CompanionState.Follow);
        }

        public void ReleaseAll()
        {
            _scripted.Clear();
            Driver.Clear();
            Convoy.Clear();
            RequireSharedVehicle = false;
        }

        public void Update(CrewSlot slot, Ped companion, Ped leader)
        {
            if (companion == null || !companion.Exists()) return;

            // Stop an already-running native combat task if its victim became the
            // player or another crew member after a switch/respawn.
            var combatTarget = Function.Call<Ped>(Hash.GET_PED_TARGET_FROM_COMBAT_PED, companion, 0);
            if (combatTarget != null && combatTarget.Exists() && IsFriendly(combatTarget, companion, leader))
            {
                companion.Task.ClearAllImmediately();
                Driver.Restart(slot);
                Refresh(slot);
            }
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
                    if (!companion.IsInCombat && StateAge(slot) >= 1000)
                    {
                        Engage(slot, companion, leader);
                        _stateSince[slot] = Game.GameTime;
                    }
                    break;
                case CompanionState.Follow:
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
            // A shared ride is a commitment, even while free-roam independence is on.
            if (leader != null && leader.Exists() && leader.IsInVehicle() && companion.IsInVehicle(leader.CurrentVehicle))
            {
                if (leader.CurrentVehicle.GetPedOnSeat(VehicleSeat.Driver)?.Handle == companion.Handle)
                { if (!Driver.Owns(slot, companion)) Driver.Arm(slot, companion); return CompanionState.Driving; }
                return FindThreat(slot, companion, leader) != null ? CompanionState.Combat : CompanionState.Vehicle;
            }
            if (IndependentFreeRoam && !MissionActive)
            {
                var ride = companion.CurrentVehicle;
                if (ride != null && !(ride.Model.IsCar || ride.Model.IsBike) && ride.GetPedOnSeat(VehicleSeat.Driver)?.Handle == companion.Handle)
                { if (!Driver.Owns(slot, companion)) Driver.Arm(slot, companion); return CompanionState.Driving; }
                if ((ride == null || ride.GetPedOnSeat(VehicleSeat.Driver)?.Handle != companion.Handle) && FindThreat(slot, companion, leader) != null) return CompanionState.Combat;
                return CompanionState.Independent;
            }
            if (!MissionActive && !RequireSharedVehicle && !IndependentFreeRoam && leader != null && leader.Exists())
            {
                if (StateOf(slot) == CompanionState.Disembarking && companion.IsInVehicle() && StateAge(slot) < 15000) return CompanionState.Disembarking;
                var vehicle = companion.CurrentVehicle;
                if (vehicle != null && vehicle.Exists() && vehicle.GetPedOnSeat(VehicleSeat.Driver)?.Handle == companion.Handle)
                {
                    bool canLeave = (vehicle.Model.IsCar || vehicle.Model.IsBike || vehicle.Model.IsHelicopter) &&
                        !vehicle.IsInAir && vehicle.HeightAboveGround < 3f && vehicle.Speed < 2f;
                    if (!leader.IsInVehicle() && canLeave && companion.Position.DistanceTo(leader.Position) < 65f)
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
                if (vehicle != null && vehicle.Exists() && HasSeatFor(slot, vehicle, companion)) return CompanionState.Vehicle;
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
                    companion.Task.ClearAll();
                    companion.Task.GuardCurrentPosition();
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
            foreach (var ped in World.GetNearbyPeds(companion, 100f))
            {
                if (IsFriendly(ped, companion, leader)) continue;
                bool attacksCrew = Function.Call<bool>(Hash.IS_PED_IN_COMBAT, ped, companion) ||
                    (leader != null && leader.Exists() && Function.Call<bool>(Hash.IS_PED_IN_COMBAT, ped, leader));
                bool activeHostile = leader != null && leader.Exists() &&
                    ped.GetRelationshipWithPed(leader) == Relationship.Hate &&
                    (leader.IsShooting || leader.IsInCombat || ped.IsShooting || ped.IsInCombat);
                if (!attacksCrew && !activeHostile) continue;
                _threats[slot] = ped;
                return ped;
            }
            return null;
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
            else { companion.Task.ClearAll(); companion.Task.FightAgainst(target); }
            Logger.Debug(Protagonist.Of(slot).Handle + " engaging hostile " + target.Handle);
        }

        private void BoardVehicle(CrewSlot slot, Ped companion, Ped leader)
        {
            var vehicle = leader?.CurrentVehicle;
            if (vehicle == null || !vehicle.Exists() || companion.IsInVehicle(vehicle)) return;
            var seat = FreeSeat(slot, vehicle, companion);
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
            var vehicle = leader?.CurrentVehicle;
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

        private static void Recover(Ped companion, Ped leader)
        {
            if (leader == null || !leader.Exists()) return;

            companion.Task.ClearAllImmediately();
            companion.Position = leader.Position - leader.ForwardVector * 3f;
            companion.Heading = leader.Heading;
        }

        private bool HasSeatFor(CrewSlot slot, Vehicle vehicle, Ped companion)
        {
            return companion.IsInVehicle(vehicle) || FreeSeat(slot, vehicle, companion) != VehicleSeat.None;
        }

        private VehicleSeat FreeSeat(CrewSlot slot, Vehicle vehicle, Ped companion)
        {
            int capacity = Function.Call<int>(Hash.GET_VEHICLE_MAX_NUMBER_OF_PASSENGERS, vehicle);
            for (int i = 0; i < capacity; i++)
            {
                var seat = (VehicleSeat)i;
                if (!vehicle.IsSeatFree(seat)) continue;
                bool reserved = false;
                foreach (var pair in _boarding)
                    if (pair.Key != slot && pair.Value.Vehicle.Handle == vehicle.Handle && pair.Value.Seat == seat)
                        reserved = true;
                if (!reserved) return seat;
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
