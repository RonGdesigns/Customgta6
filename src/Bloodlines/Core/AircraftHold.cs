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
        /// <summary>The lowest the AI is allowed to take it while holding a circle.</summary>
        public const int MinimumHeight = 25;
        /// <summary>How tight a hover is allowed to drift from the point it is holding.</summary>
        public const float HoverRadius = 6f;
        /// <summary>Approach speed for a hover: slow, because he is stopping on a spot.</summary>
        public const float HoverSpeed = 12f;
        /// <summary>How far out he starts slowing for the hover.</summary>
        public const float HoverSlowdown = 30f;

        /// <summary>Approach speed given to a helicopter that is created already flying.</summary>
        public const float AirborneSpeed = 25f;

        /// <summary>
        /// An aircraft created in the air begins with its rotors stopped. The engine
        /// being on is not lift: a helicopter spawned at altitude falls while the blades
        /// spin up, and from 60 meters over water it is in the sea before they reach
        /// speed. Ron watched M45's Annihilator do exactly that on every attempt at the
        /// heist. Call this immediately after creating one above the ground.
        /// </summary>
        /// <summary>
        /// An aircraft the player is flying has a running engine.
        ///
        /// World.CreateVehicle leaves an engine off, and the free-roam spawner
        /// deliberately creates planes and helicopters cold — but nothing ever started
        /// them, so a jet requested from the dev menu let Ron aim its guns and never
        /// accelerate. That was M26's Lazer as well, fixed there by hand. This is the same
        /// check in one place, so a cold aircraft is a thing standing on the apron rather
        /// than a thing that never flies.
        ///
        /// Only while he is in the driver's seat. A parked aircraft stays cold, which is
        /// what makes M26's scramble read as a scramble, and a passenger does not reach
        /// past the pilot to start it.
        /// </summary>
        public static void KeepPlayerAircraftRunning()
        {
            try
            {
                var player = Game.Player.Character;
                if (player == null || !player.Exists() || player.IsDead || !player.IsInVehicle()) return;
                var aircraft = player.CurrentVehicle;
                if (aircraft == null || !aircraft.Exists() || aircraft.IsDead || !aircraft.IsDriveable) return;
                if (!aircraft.Model.IsPlane && !aircraft.Model.IsHelicopter) return;
                if (player.SeatIndex != VehicleSeat.Driver || aircraft.IsEngineRunning) return;
                aircraft.IsEngineRunning = true;
                Logger.Info("Started a cold aircraft the player is flying: " + aircraft.DisplayName + ".");
            }
            catch (Exception ex) { Logger.Error("Starting the aircraft the player is flying", ex); }
        }
        public static void LaunchAirborne(Vehicle aircraft, float forwardSpeed = AirborneSpeed)
        {
            if (aircraft == null || !aircraft.Exists()) return;
            aircraft.IsEngineRunning = true;
            try { Function.Call(Hash.SET_HELI_BLADES_FULL_SPEED, aircraft); }
            catch (Exception ex) { Logger.Error("Spinning up an aircraft created in the air", ex); }
            // Flying, not hanging: a little airspeed is what an approach looks like and it
            // keeps the aircraft behaving as airborne the moment the player takes it.
            if (forwardSpeed > 0f) aircraft.ForwardSpeed = forwardSpeed;
            Logger.Info("Aircraft created airborne at " + aircraft.Position + "; rotors at speed.");
        }

        private int _next;
        private bool _ordered;
        private bool _hovering;

        /// <summary>Whether a holding pattern is currently in force.</summary>
        public bool Holding => _ordered;
        /// <summary>Whether the pattern in force is a hover on a spot rather than a circuit.</summary>
        public bool Hovering => _ordered && _hovering;

        /// <summary>
        /// Call every frame. Does nothing while the player is flying it himself, and
        /// nothing if the pilot is not in it — a mission that wants him landed keeps him
        /// landed by not calling this. An aircraft already sitting on something is left
        /// sitting on it: a parked helicopter cannot fall, and ordering one into a
        /// pattern would lift it off a deck somebody is standing on.
        /// </summary>
        /// <param name="hover">
        /// True to hold the spot instead of flying a circuit. An insertion needs the
        /// aircraft to stay over the point the man is stepping onto; a wait while the
        /// player is elsewhere does not care, and a circuit looks better.
        /// </param>
        public void Update(CrewRoster crew, CrewSlot slot, Vehicle aircraft, Vector3 at, int height, bool hover = false)
        {
            if (crew == null || aircraft == null || !aircraft.Exists() || aircraft.IsDead || !aircraft.IsDriveable)
            { _ordered = false; return; }
            var pilot = crew.PedFor(slot);
            if (pilot == null || !pilot.Exists() || pilot.IsDead || !pilot.IsInVehicle(aircraft))
            { _ordered = false; return; }
            if (crew.ActiveSlot == slot) { Release(); return; }
            if (!aircraft.IsInAir) { Release(); return; }
            // A change of pattern takes effect now rather than at the next cadence.
            if (_ordered && hover != _hovering) _next = 0;
            if (Game.GameTime < _next) return;

            _next = Game.GameTime + OrderIntervalMs;
            crew.CompanionAI.TakeControl(slot);
            aircraft.IsEngineRunning = true;
            try { Function.Call(Hash.SET_HELI_BLADES_FULL_SPEED, aircraft); }
            catch (Exception ex) { Logger.Error("Spinning up a held aircraft", ex); }
            if (hover)
                // GoTo holds the destination once it is reached: the AI flies to the point
                // and stays on it. A slowdown distance stops him arriving at speed and
                // sailing past the spot a man is trying to step onto.
                pilot.Task.StartHeliMission(aircraft, at, VehicleMissionType.GoTo,
                    HoverSpeed, HoverRadius, height, height, 0f, HoverSlowdown, HeliMissionFlags.None);
            else
                pilot.Task.StartHeliMission(aircraft, at, VehicleMissionType.Circle,
                    HoldSpeed, HoldRadius, height, MinimumHeight, 0f, 0f, HeliMissionFlags.None);
            if (!_ordered || hover != _hovering)
                Logger.Info(slot + (hover ? " is hovering his aircraft over " : " is circling his aircraft over ") + at + " at " + height + " m while the player is elsewhere.");
            _ordered = true;
            _hovering = hover;
        }

        /// <summary>
        /// Stop holding, so the next order takes effect at once. Called when the player
        /// takes the aircraft back, and by a mission that wants it to land.
        /// </summary>
        public void Release()
        {
            _ordered = false;
            _hovering = false;
            _next = 0;
        }
    }
}
