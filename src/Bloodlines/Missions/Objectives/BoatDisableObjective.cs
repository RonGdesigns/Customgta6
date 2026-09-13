using System;
using System.Drawing;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Missions.Objectives
{
    /// <summary>Guess drives; Gohan works remotely from the passenger seat. Handoff waits for both boats to stop alongside.</summary>
    public sealed class BoatDisableObjective : Objective
    {
        public const float HackRange = 45f;
        public const int HackSeconds = 24;
        private readonly Func<Ped> _target;
        private readonly Func<Vehicle> _boat, _pursuer;
        private ProximityHack _hack;
        private int _started;
        public bool Disabled { get; private set; }
        public float Progress => _hack?.Progress ?? 0f;
        public BoatDisableObjective(Func<Ped> target, Func<Vehicle> boat, Func<Vehicle> pursuer)
            : base("Guess: stay within 45m of Mateo while Gohan disables his boat.")
        { _target = target; _boat = boat; _pursuer = pursuer; }
        public override void Enter(MissionContext c)
        { base.Enter(c); _hack = new ProximityHack(HackSeconds); _started = Game.GameTime; Disabled = false; }
        public override void Update(MissionContext c)
        {
            var target = _target(); var boat = _boat(); var chase = _pursuer();
            var guess = c.Crew.PedFor(CrewSlot.Guess); var gohan = c.Crew.PedFor(CrewSlot.Gohan);
            if (target == null || !target.Exists() || target.IsDead || boat == null || !boat.Exists() || boat.IsDead)
            { Fail("Mateo and his boat must survive. Restart the mission."); return; }
            if (chase == null || !chase.Exists() || !chase.IsDriveable || chase.IsDead)
            { Fail("The crew's dinghy is lost."); return; }
            if (gohan == null || !gohan.Exists() || gohan.IsDead)
            { Fail("Gohan is down. The engine disable needs him alive."); return; }
            if (!Disabled && Game.GameTime - _started > 300000)
            { Fail("Mateo escaped before Gohan could disable his engine."); return; }
            float gap = boat.Position.DistanceTo(chase.Position);
            ObjectiveMarkers.Navigation(boat.Position, CrewSlot.Guess, chase);
            GameUtils.DrawObjectiveMarker(boat.Position, Color.Yellow, 3f);
            bool driving = c.Crew.ActiveSlot == CrewSlot.Guess && guess != null && guess.Exists() &&
                guess.IsInVehicle(chase) && chase.GetPedOnSeat(VehicleSeat.Driver) == guess;
            bool passenger = gohan.IsInVehicle(chase) && chase.GetPedOnSeat(VehicleSeat.Driver) != gohan;
            if (!Disabled)
            {
                _hack.Update(Game.GameTime, driving && passenger && gap <= HackRange);
                Label = "Guess: Gohan's engine disable " + (int)(Progress * 100f) + "% | " + (int)gap + "/45m. " +
                    (!passenger ? "Gohan must stay aboard." : !driving ? "Drive the dinghy as Guess." : gap > HackRange ? "Close the gap; progress is paused." : "Keep close. Gohan is working automatically.");
                GameUtils.DrawProgressBar(Progress);
                if (Progress < 1f) return;
                // Same engine-disable mechanism as the earlier road mission,
                // without an explosion or forcing the witness out into the water.
                target.Task.ClearAll();
                boat.EngineHealth = 1f; boat.IsEngineRunning = false;
                boat.EnginePowerMultiplier = 0f;
                Function.Call(Hash.SET_VEHICLE_UNDRIVEABLE, boat, true);
                boat.IsDriveable = false;
                Disabled = true;
                Logger.Info("M05: Gohan disabled Mateo's boat; waiting for a stopped rendezvous.");
            }
            // Let the target settle over several seconds rather than snapping to
            // a full stop at hack completion. Keep Guess in control throughout.
            float speed = Math.Max(0f, boat.Speed - 5f * Math.Max(0f, Math.Min(.1f, Game.LastFrameTime)));
            Function.Call(Hash.SET_VEHICLE_FORWARD_SPEED, boat, speed);
            Label = "Guess: engine disabled. Stop the dinghy within 18m of Mateo's stopped boat, then switch to Gohan.";
            if (driving && passenger && gap <= 18f && boat.Speed < 1.5f && chase.Speed < 1.5f) Complete();
        }
    }
}
