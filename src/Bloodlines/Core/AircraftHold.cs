using System;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// Keeps a brother's aircraft flying while the player is someone else.
    ///
    /// Handing a helicopter to a crew member and then switching away leaves nobody
    /// flying it: the companion controller is not a pilot, so it descends and lands
    /// or crashes, and when the player switches back he arrives in an aircraft on its
    /// way to the ground. Ron hit this switching from Gohan to Guess with Guess in
    /// the helicopter. M42 already solved it for its Titan with a bespoke circling
    /// order; this is that idea where every mission can reach it.
    ///
    /// The order is reissued on a cadence rather than every frame, for the same reason
    /// a guard's combat order is: re-tasking restarts the task.
    /// </summary>
    public sealed class AircraftHold
    {
        /// <summary>How often the holding pattern is reissued.</summary>
        public const int OrderIntervalMs = 9000;
        /// <summary>How wide a circle the pilot flies around the point he is holding at.</summary>
        public const float HoldRadius = 90f;
        /// <summary>Cruise for a holding pattern: unhurried, because he is waiting.</summary>
        public const float HoldSpeed = 22f;
        /// <summary>The lowest the AI is allowed to take it while holding.</summary>
        public const int MinimumHeight = 25;

        private int _next;
        private bool _ordered;

        /// <summary>Whether a holding pattern is currently in force.</summary>
        public bool Holding => _ordered;

        /// <summary>
        /// Call every frame. Does nothing while the player is flying it himself, and
        /// nothing if the pilot is not in it — a mission that wants him landed keeps him
        /// landed by not calling this.
        /// </summary>
        public void Update(CrewRoster crew, CrewSlot slot, Vehicle aircraft, Vector3 at, int height)
        {
            if (crew == null || aircraft == null || !aircraft.Exists() || aircraft.IsDead || !aircraft.IsDriveable)
            { _ordered = false; return; }
            var pilot = crew.PedFor(slot);
            if (pilot == null || !pilot.Exists() || pilot.IsDead || !pilot.IsInVehicle(aircraft))
            { _ordered = false; return; }
            if (crew.ActiveSlot == slot) { Release(); return; }
            if (Game.GameTime < _next) return;

            _next = Game.GameTime + OrderIntervalMs;
            crew.CompanionAI.TakeControl(slot);
            aircraft.IsEngineRunning = true;
            try { Function.Call(Hash.SET_HELI_BLADES_FULL_SPEED, aircraft); }
            catch (Exception ex) { Logger.Error("Spinning up a held aircraft", ex); }
            pilot.Task.StartHeliMission(aircraft, at, VehicleMissionType.Circle,
                HoldSpeed, HoldRadius, height, MinimumHeight, 0f, 0f, HeliMissionFlags.None);
            if (!_ordered) Logger.Info(slot + " is holding his aircraft over " + at + " at " + height + " m while the player is elsewhere.");
            _ordered = true;
        }

        /// <summary>
        /// Stop holding, so the next order takes effect at once. Called when the player
        /// takes the aircraft back, and by a mission that wants it to land.
        /// </summary>
        public void Release()
        {
            _ordered = false;
            _next = 0;
        }
    }
}
