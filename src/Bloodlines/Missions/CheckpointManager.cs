using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions
{
    /// <summary>
    /// The bible's BloodlinesCheckpointManager: preserves the stage index, restores
    /// character positions and health, purges wreckage, and respawns the vehicles a
    /// mission depends on.
    ///
    /// Long missions with no checkpoint are the reason people stop replaying them,
    /// and a mission that respawns you next to your own burning getaway car is worse
    /// than no checkpoint at all — hence the wreck purge on restore.
    /// </summary>
    public sealed class CheckpointManager
    {
        private readonly CrewRoster _crew;

        private Snapshot _snapshot;

        public CheckpointManager(CrewRoster crew)
        {
            _crew = crew;
        }

        public bool HasCheckpoint => _snapshot != null;

        public string CheckpointLabel => _snapshot == null
            ? "none"
            : _snapshot.MissionId + " stage " + _snapshot.Stage;

        /// <summary>Takes a snapshot. Called on every stage advance and by the QA key.</summary>
        public void Commit(string missionId, int stage)
        {
            var snapshot = new Snapshot
            {
                MissionId = missionId,
                Stage = stage,
                ActiveSlot = _crew.ActiveSlot,
                WantedLevel = Game.Player.WantedLevel
            };

            foreach (var protagonist in Protagonist.All)
            {
                var ped = _crew.PedFor(protagonist.Slot);
                if (ped == null || !ped.Exists()) continue;

                snapshot.Positions[protagonist.Slot] = new PedState
                {
                    Position = ped.Position,
                    Heading = ped.Heading,
                    Health = ped.Health,
                    Armor = ped.Armor
                };
            }

            _snapshot = snapshot;
            Logger.Info("Checkpoint committed: " + CheckpointLabel);
        }

        /// <summary>
        /// Restores the last snapshot. Returns the stage to resume at, or -1 when
        /// there is nothing to restore or it belongs to a different mission.
        /// </summary>
        public int Restore(string missionId)
        {
            if (_snapshot == null || _snapshot.MissionId != missionId)
            {
                GameUtils.Subtitle("~r~No checkpoint for this mission.", 2500);
                return -1;
            }

            PurgeWreckage();

            foreach (var pair in _snapshot.Positions)
            {
                var ped = _crew.PedFor(pair.Key);
                if (ped == null || !ped.Exists()) continue;

                ped.Task.ClearAllImmediately();
                ped.Position = pair.Value.Position;
                ped.Heading = pair.Value.Heading;
                ped.Health = pair.Value.Health;
                ped.Armor = pair.Value.Armor;
            }

            Game.Player.WantedLevel = _snapshot.WantedLevel;
            _crew.AssignCompanionAI();

            Logger.Info("Checkpoint restored: " + CheckpointLabel);
            GameUtils.Subtitle("~y~Checkpoint restored — stage " + _snapshot.Stage, 3000);
            return _snapshot.Stage;
        }

        public void Clear()
        {
            _snapshot = null;
        }

        /// <summary>
        /// Deletes burnt-out and abandoned vehicles around the restore point. Without
        /// this, retrying a vehicle mission leaves the canal full of the wrecks from
        /// every previous attempt and the frame rate goes with them.
        /// </summary>
        private void PurgeWreckage()
        {
            var player = Game.Player.Character;
            if (player == null || !player.Exists()) return;

            var origin = _snapshot.Positions.TryGetValue(_snapshot.ActiveSlot, out var state)
                ? state.Position
                : player.Position;

            foreach (var vehicle in World.GetNearbyVehicles(origin, 220f))
            {
                if (vehicle == null || !vehicle.Exists()) continue;
                if (player.CurrentVehicle != null && vehicle.Handle == player.CurrentVehicle.Handle) continue;

                bool wrecked = vehicle.IsDead || vehicle.EngineHealth <= 0f || vehicle.IsOnFire;
                if (!wrecked) continue;

                GameUtils.SafeDelete(vehicle);
            }
        }

        private sealed class Snapshot
        {
            public string MissionId;
            public int Stage;
            public CrewSlot ActiveSlot;
            public int WantedLevel;
            public readonly Dictionary<CrewSlot, PedState> Positions = new Dictionary<CrewSlot, PedState>();
        }

        private struct PedState
        {
            public Vector3 Position;
            public float Heading;
            public int Health;
            public int Armor;
        }
    }
}
