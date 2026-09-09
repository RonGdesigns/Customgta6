using Bloodlines.Crew;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>Input-only mission handoff. Never freezes a ped or disables player control.</summary>
    public sealed class MissionHandoff
    {
        public bool IsWaiting { get; private set; }
        public void Update(CrewRoster crew, CrewSlot? required)
        {
            IsWaiting = crew.IsDeployed && required.HasValue && required.Value != crew.ActiveSlot;
            var ped = Game.Player.Character;
            if (!IsWaiting || ped == null || !ped.Exists() || ped.IsDead) { IsWaiting = false; return; }
            // These are frame controls: death, abort, reload and a successful switch
            // release them without a persistent frozen entity to recover later.
            foreach (var control in new[] { Control.MoveLeftRight, Control.MoveUpDown,
                Control.Sprint, Control.Jump, Control.Attack, Control.Attack2, Control.Aim,
                Control.MeleeAttack1, Control.MeleeAttack2, Control.MeleeAttackAlternate,
                Control.Enter, Control.VehicleExit, Control.VehicleAccelerate, Control.VehicleBrake,
                Control.VehicleMoveLeftRight, Control.VehicleAttack, Control.VehicleAttack2,
                Control.VehicleAim, Control.Context, Control.Detonate, Control.ThrowGrenade })
                Game.DisableControlThisFrame(control);
            // Brake only the player's road vehicle. A passenger handoff must not
            // stop Guess's AI chase, aircraft, or another character's assignment.
            var car = ped.CurrentVehicle;
            if (car != null && car.Exists() && car.Model.IsCar && car.GetPedOnSeat(VehicleSeat.Driver) == ped)
                Function.Call(Hash.SET_CONTROL_VALUE_NEXT_FRAME, 0, (int)Control.VehicleHandbrake, 1f);
        }
    }
}
