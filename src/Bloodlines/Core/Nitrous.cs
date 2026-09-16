using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>Purchased slot 17 drives a bounded boost; WorldTuning remains the sole torque writer.</summary>
    public sealed class Nitrous
    {
        public const int ModSlot = 17;
        public const float BoostSeconds = 5f, RechargeSeconds = 20f, TorqueMultiplier = 1.65f;
        /// <summary>
        /// How much of the car's own raised ceiling the boost unlocks.
        ///
        /// Torque alone was the whole boost, which is why it felt like acceleration and
        /// nothing else: WorldTuning already lifts every car's entity speed cap to its
        /// doubled redline, so the cap was identical with the bottle open and shut. Raising
        /// it further while boosting means the cap is definitely not what the car is
        /// running into. Whether the terminal speed visibly moves is a road test, not a
        /// number anyone can read off this file.
        /// </summary>
        public const float SpeedMultiplier = 1.25f;
        public const string Controls = "hold A / keyboard X";
        private sealed class Bottle { public Vehicle Car; public int Model, LastUsed; public float Charge = 1f; }
        private readonly Dictionary<int, Bottle> _bottles = new Dictionary<int, Bottle>();
        private Vehicle _boosted;
        public float Charge { get; private set; } = 1f;
        public bool Boosting => _boosted != null;
        public static bool Installed(Vehicle car) => car != null && car.Exists() && Function.Call<bool>(Hash.IS_TOGGLE_MOD_ON, car, ModSlot);
        public float MultiplierFor(Vehicle car) => _boosted == car ? TorqueMultiplier : 1f;
        public void Update(bool blocked)
        {
            _boosted = null;
            var ped = Game.Player.Character; var car = ped?.CurrentVehicle;
            if (blocked || Game.IsPaused || !Game.Player.CanControlCharacter || ped == null || !ped.Exists() || ped.IsDead || ped.IsPositionFrozen ||
                car == null || !car.Exists() || car.IsDead || !car.IsDriveable || car.GetPedOnSeat(VehicleSeat.Driver) != ped ||
                !(car.Model.IsCar || car.Model.IsBike) || car.IsPositionFrozen || !Installed(car) || car.Handle == ShopService.PreviewVehicleHandle) return;
            if (!_bottles.TryGetValue(car.Handle, out var bottle) || bottle.Model != car.Model.Hash)
            {
                foreach (var key in _bottles.Where(p => !p.Value.Car.Exists()).Select(p => p.Key).ToArray()) _bottles.Remove(key);
                if (_bottles.Count >= 128) _bottles.Remove(_bottles.OrderBy(p => p.Value.LastUsed).First().Key);
                bottle = new Bottle { Car = car, Model = car.Model.Hash }; _bottles[car.Handle] = bottle;
            }
            bool held = ControllerInput.Pressed((Control)73);
            // These share A on a controller. Duck/hydraulics/secondary fire must not play with a nitrous press.
            foreach (int control in new[] { 73, 337, 70 }) Game.DisableControlThisFrame((Control)control);
            float dt = Game.LastFrameTime;
            if (float.IsNaN(dt) || float.IsInfinity(dt)) dt = 0;
            dt = Math.Max(0f, Math.Min(.1f, dt));
            var velocity = car.Velocity; var forward = car.ForwardVector;
            float speed = velocity.X * forward.X + velocity.Y * forward.Y + velocity.Z * forward.Z;
            bool moving = car.IsEngineRunning && !car.IsInAir && speed > 1f &&
                ControllerInput.Pressed((Control)71) && !ControllerInput.Pressed((Control)72) && !ControllerInput.Pressed((Control)76);
            if (held && moving && bottle.Charge > 0f)
            {
                _boosted = car; bottle.Charge = Math.Max(0f, bottle.Charge - dt / BoostSeconds); bottle.LastUsed = Game.GameTime;
            }
            else if (!held && Game.GameTime - bottle.LastUsed >= 2000)
                bottle.Charge = Math.Min(1f, bottle.Charge + dt / RechargeSeconds);
            Charge = bottle.Charge;
            Function.Call(Hash.DRAW_RECT, .09f, .775f, .14f, .009f, 20, 20, 20, 210, false);
            Function.Call(Hash.DRAW_RECT, .02f + .07f * Charge, .775f, .14f * Charge, .005f, 40, 220, 240, 240, false);
            new GTA.UI.TextElement("NITRO " + (int)(Charge * 100) + "% | " + (Boosting ? "BOOST" : held && Charge <= 0 ? "Release to recharge" : Controls),
                new PointF(24, 530), .27f, Boosting ? Color.Cyan : Color.White).Draw();
        }
        public void Reset() { _boosted = null; _bottles.Clear(); Charge = 1f; }
    }
}
