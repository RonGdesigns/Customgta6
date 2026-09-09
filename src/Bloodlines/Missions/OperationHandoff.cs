using System;
using System.Collections.Generic;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions
{
    /// <summary>
    /// What one chapter of a continuous operation hands to the next: who was
    /// active, where everybody was, which vehicle they were in and whether the
    /// cargo was still attached to it.
    ///
    /// Entity persistence across a mission boundary is not something the mission
    /// framework can promise — every mission tears its world down and the next one
    /// builds its own — so the record is a description, and the receiving mission
    /// reconstructs the same apparent state from it. The Port Heist (M19 -> M22) is
    /// the first user; the offshore rig, the tower and the finale reuse the shape.
    /// </summary>
    public sealed class OperationHandoff
    {
        public string Operation { get; set; } = "";
        public string FromMission { get; set; } = "";
        public string ToMission { get; set; } = "";
        public CrewSlot ActiveHero { get; set; }
        public Dictionary<CrewSlot, Vector3> Positions { get; } = new Dictionary<CrewSlot, Vector3>();
        public Dictionary<CrewSlot, VehicleSeat> Seats { get; } = new Dictionary<CrewSlot, VehicleSeat>();
        public string VehicleModel { get; set; } = "";
        public Vector3 VehiclePosition { get; set; }
        public float VehicleHeading { get; set; }
        public float VehicleHealth { get; set; } = 1f;
        public bool CargoAttached { get; set; }
        public string CargoModel { get; set; } = "";
        public int Hour { get; set; } = -1;
        public int Minute { get; set; }
        public string Weather { get; set; } = "";
        public Dictionary<string, string> Notes { get; } = new Dictionary<string, string>();
        public int RecordedAt { get; set; }

        /// <summary>Snapshot the crew and one essential vehicle as the passing mission sees them.</summary>
        public static OperationHandoff Capture(string operation, string from, string to, CrewRoster crew, Vehicle vehicle)
        {
            var record = new OperationHandoff { Operation = operation, FromMission = from, ToMission = to, ActiveHero = crew.ActiveSlot, RecordedAt = Game.GameTime };
            foreach (var hero in Protagonist.All)
            {
                var ped = crew.PedFor(hero.Slot);
                if (ped == null || !ped.Exists()) continue;
                record.Positions[hero.Slot] = ped.Position;
                if (vehicle != null && vehicle.Exists() && ped.IsInVehicle(vehicle)) record.Seats[hero.Slot] = ped.SeatIndex;
            }
            if (vehicle != null && vehicle.Exists())
            {
                record.VehicleModel = vehicle.DisplayName;
                record.VehiclePosition = vehicle.Position;
                record.VehicleHeading = vehicle.Heading;
                record.VehicleHealth = Math.Max(0f, Math.Min(1f, vehicle.EngineHealth / 1000f));
            }
            return record;
        }
    }

    /// <summary>
    /// The in-memory ledger of chapter handoffs. Not written to the save: a record
    /// describes live entities from the same session, and a mission that starts
    /// after a restart must build its default staging rather than trust positions
    /// from a world that no longer exists. Missions log whether they consumed one.
    /// </summary>
    public sealed class HandoffLedger
    {
        private readonly Dictionary<string, OperationHandoff> _records = new Dictionary<string, OperationHandoff>(StringComparer.OrdinalIgnoreCase);

        public int Count => _records.Count;

        public void Record(OperationHandoff record)
        {
            if (record == null || string.IsNullOrEmpty(record.Operation) || string.IsNullOrEmpty(record.ToMission)) return;
            _records[record.Operation] = record;
            Logger.Info("Handoff recorded: " + record.Operation + " " + record.FromMission + " -> " + record.ToMission +
                        " (active " + record.ActiveHero + ", vehicle " + (record.VehicleModel.Length > 0 ? record.VehicleModel : "none") +
                        (record.CargoAttached ? ", cargo attached" : "") + ")");
        }

        /// <summary>The record waiting for this mission, or null. Peeking does not consume it.</summary>
        public OperationHandoff Peek(string operation, string mission)
        {
            return _records.TryGetValue(operation, out var record) &&
                   string.Equals(record.ToMission, mission, StringComparison.OrdinalIgnoreCase) ? record : null;
        }

        /// <summary>Consume the record addressed to this mission. A retry of the same mission gets a fresh default staging.</summary>
        public OperationHandoff Take(string operation, string mission)
        {
            var record = Peek(operation, mission);
            if (record != null)
            {
                _records.Remove(operation);
                Logger.Info("Handoff consumed by " + mission + ": " + operation + " from " + record.FromMission + ".");
            }
            else Logger.Info(mission + " starts from default staging; no " + operation + " handoff was recorded this session.");
            return record;
        }

        public void Clear()
        {
            _records.Clear();
        }
    }
}
