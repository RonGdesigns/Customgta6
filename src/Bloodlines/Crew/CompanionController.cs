using System.Collections.Generic;
using Bloodlines.Core;
using GTA;
using GTA.Math;

namespace Bloodlines.Crew
{
    public enum CompanionState
    {
        /// <summary>Tailing the active character on foot.</summary>
        Follow,

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
        private const float VehicleTaskRange = 25f;
        private const float VehicleWarpSpeed = 6f;
        private const int VehicleTaskTimeoutMs = 6000;

        private readonly ModConfig _config;
        private readonly Dictionary<CrewSlot, CompanionState> _states = new Dictionary<CrewSlot, CompanionState>();
        private readonly Dictionary<CrewSlot, int> _stateSince = new Dictionary<CrewSlot, int>();
        private readonly HashSet<CrewSlot> _scripted = new HashSet<CrewSlot>();

        public CompanionController(ModConfig config)
        {
            _config = config;
        }

        /// <summary>Companions hold position instead of following (split-approach missions).</summary>
        public bool HoldPosition { get; set; }

        public CompanionState StateOf(CrewSlot slot)
        {
            return _states.TryGetValue(slot, out var state) ? state : CompanionState.Follow;
        }

        /// <summary>A mission takes direct control of one companion until it releases it.</summary>
        public void TakeControl(CrewSlot slot)
        {
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
        }

        public void Update(CrewSlot slot, Ped companion, Ped leader)
        {
            if (companion == null || !companion.Exists()) return;

            var state = Decide(slot, companion, leader);
            if (state != StateOf(slot))
            {
                SetState(slot, state);
                Apply(slot, state, companion, leader);
                return;
            }

            // A few states need upkeep rather than a one-shot task.
            switch (state)
            {
                case CompanionState.Vehicle:
                    MaintainVehicle(slot, companion, leader);
                    break;
                case CompanionState.Follow:
                    if (Game.GameTime - _stateSince[slot] > 8000) Apply(slot, state, companion, leader);
                    break;
            }
        }

        private CompanionState Decide(CrewSlot slot, Ped companion, Ped leader)
        {
            if (companion.IsDead) return CompanionState.Downed;
            if (_scripted.Contains(slot)) return CompanionState.Scripted;

            if (leader != null && leader.Exists() && leader.IsInVehicle())
            {
                var vehicle = leader.CurrentVehicle;
                if (vehicle != null && vehicle.Exists() && HasSeatFor(vehicle, companion)) return CompanionState.Vehicle;
            }

            if (companion.IsInCombat) return CompanionState.Combat;

            if (HoldPosition) return CompanionState.Hold;

            if (leader != null && leader.Exists() &&
                companion.Position.DistanceTo(leader.Position) > _config.CompanionLeashDistance)
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

                case CompanionState.Hold:
                    companion.Task.ClearAll();
                    companion.Task.GuardCurrentPosition();
                    break;

                case CompanionState.Vehicle:
                    BoardVehicle(companion, leader);
                    break;

                case CompanionState.TeleportRecovery:
                    Recover(companion, leader);
                    break;

                case CompanionState.Combat:
                case CompanionState.Scripted:
                case CompanionState.Downed:
                    break;
            }
        }

        private void BoardVehicle(Ped companion, Ped leader)
        {
            var vehicle = leader?.CurrentVehicle;
            if (vehicle == null || !vehicle.Exists()) return;

            var seat = FreeSeat(vehicle, companion);
            if (seat == VehicleSeat.None) return;

            if (companion.IsInVehicle(vehicle)) return;

            // A car already rolling, or a companion across the yard, never catches up.
            // Warping reads worse for a second than a mission failing does for a run.
            if (vehicle.Speed > VehicleWarpSpeed ||
                companion.Position.DistanceTo(vehicle.Position) > VehicleTaskRange)
            {
                companion.Task.ClearAllImmediately();
                companion.Task.WarpIntoVehicle(vehicle, seat);
                Logger.Debug("Warped a companion into " + vehicle.DisplayName + " seat " + seat + ".");
                return;
            }

            companion.Task.ClearAll();
            companion.Task.EnterVehicle(vehicle, seat, VehicleTaskTimeoutMs, 2f, EnterVehicleFlags.None);
        }

        private void MaintainVehicle(CrewSlot slot, Ped companion, Ped leader)
        {
            var vehicle = leader?.CurrentVehicle;
            if (vehicle == null || !vehicle.Exists()) return;
            if (companion.IsInVehicle(vehicle)) return;

            // Retry, then warp — an EnterVehicle task that fails silently is the exact
            // failure this state machine exists to prevent.
            if (Game.GameTime - _stateSince[slot] < VehicleTaskTimeoutMs) return;

            var seat = FreeSeat(vehicle, companion);
            if (seat == VehicleSeat.None) return;

            companion.Task.ClearAllImmediately();
            companion.Task.WarpIntoVehicle(vehicle, seat);
            _stateSince[slot] = Game.GameTime;
            Logger.Debug("Companion missed the boarding window; warped into " + vehicle.DisplayName + ".");
        }

        private static void Recover(Ped companion, Ped leader)
        {
            if (leader == null || !leader.Exists()) return;

            companion.Task.ClearAllImmediately();
            companion.Position = leader.Position - leader.ForwardVector * 3f;
            companion.Heading = leader.Heading;
        }

        private static bool HasSeatFor(Vehicle vehicle, Ped companion)
        {
            return companion.IsInVehicle(vehicle) || FreeSeat(vehicle, companion) != VehicleSeat.None;
        }

        private static VehicleSeat FreeSeat(Vehicle vehicle, Ped companion)
        {
            VehicleSeat[] order =
            {
                VehicleSeat.RightFront, VehicleSeat.LeftRear, VehicleSeat.RightRear
            };

            foreach (var seat in order)
            {
                if (!vehicle.IsSeatFree(seat)) continue;
                return seat;
            }

            return VehicleSeat.None;
        }

        private void SetState(CrewSlot slot, CompanionState state)
        {
            _states[slot] = state;
            _stateSince[slot] = Game.GameTime;
            Logger.Debug(Protagonist.Of(slot).FirstName + " -> " + state);
        }

        public void Forget(CrewSlot slot)
        {
            _states.Remove(slot);
            _stateSince.Remove(slot);
            _scripted.Remove(slot);
        }
    }
}
