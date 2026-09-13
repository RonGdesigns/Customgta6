using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>Bounded local dents from measured side/roof impacts. No damage while merely resting upside down.</summary>
    public sealed class VehiclePanelDamage
    {
        private sealed class Sample
        {
            public int Model, Time, DentAt = -1000;
            public Vector3 Position, Velocity;
        }
        private readonly Dictionary<int, Sample> _samples = new Dictionary<int, Sample>();
        private int _next;
        public void Reset() { _samples.Clear(); _next = 0; }

        public void Update(IEnumerable<Vehicle> vehicles, ModConfig config)
        {
            var player = Game.Player.Character;
            if (config == null || !config.VehicleDamageEnabled || !config.PanelDamageEnabled || player == null || !player.Exists())
            { Reset(); return; }
            int now = Game.GameTime;
            if (now < _next) return;
            _next = now + 75;
            var live = new HashSet<int>();
            foreach (var car in vehicles.Where(v => v != null && v.Exists() && !v.IsDead && v.Model.IsCar &&
                !v.IsInvincible && v.Position.DistanceTo(player.Position) < 90f).OrderBy(v => v.Position.DistanceTo(player.Position)).Take(32))
            {
                live.Add(car.Handle);
                try { SampleCar(car, now, config.PanelDamageStrength); }
                catch (Exception ex) { _samples.Remove(car.Handle); Logger.Error("Vehicle panel collision", ex); }
            }
            foreach (int key in _samples.Keys.Where(k => !live.Contains(k)).ToArray()) _samples.Remove(key);
        }

        private void SampleCar(Vehicle car, int now, float strength)
        {
            Vector3 position = car.Position, velocity = car.Velocity;
            if (!_samples.TryGetValue(car.Handle, out var old) || old.Model != car.Model.Hash)
                _samples[car.Handle] = old = new Sample { Model = car.Model.Hash, Time = now, Position = position, Velocity = velocity };
            int elapsed = now - old.Time;
            Vector3 previousVelocity=old.Velocity;
            Vector3 delta = velocity - previousVelocity;
            bool continuous = elapsed > 0 && elapsed <= 250 && position.DistanceTo(old.Position) < 40f;
            old.Time = now; old.Position = position; old.Velocity = velocity;
            if (!continuous || now - old.DentAt < 750 || !Function.Call<bool>(Hash.HAS_ENTITY_COLLIDED_WITH_ANYTHING, car)) return;
            var normal = Function.Call<Vector3>(Hash.GET_COLLISION_NORMAL_OF_LAST_HIT_FOR_ENTITY, car);
            float length = normal.Length();
            if (float.IsNaN(length) || float.IsInfinity(length) || length < .5f || length > 1.5f) return;
            normal *= 1f / length;
            float impulse = Math.Abs(delta.X * normal.X + delta.Y * normal.Y + delta.Z * normal.Z);
            var point = position + normal;
            var localNormal = Function.Call<Vector3>(Hash.GET_OFFSET_FROM_ENTITY_GIVEN_WORLD_COORDS, car, point.X, point.Y, point.Z);
            TryEjectCivilian(car,localNormal*-1f,impulse,previousVelocity);
            var bounds = car.Model.Dimensions;
            if (!TryDent(localNormal * -1f, bounds.Item1, bounds.Item2, impulse, strength, out var offset, out float damage)) return;
            // Preserve health already lost to the real collision. This supplements
            // deformation only; it does not multiply engine/body damage a second time.
            float body = car.BodyHealth, engine = car.EngineHealth;
            Function.Call(Hash.SET_VEHICLE_DAMAGE, car, offset.X, offset.Y, offset.Z, damage, .9f, true);
            car.BodyHealth = body; car.EngineHealth = engine;
            old.DentAt = now;
        }

        internal static bool SevereFrontalCrash(Vector3 contact,float impulse,float previousSpeed) =>
            contact.Y>.6f&&Math.Abs(contact.X)<contact.Y&&Math.Abs(contact.Z)<contact.Y&&
            impulse>=16f&&impulse<=100f&&previousSpeed>=18f;
        private static void TryEjectCivilian(Vehicle car,Vector3 contact,float impulse,Vector3 previousVelocity)
        {
            if(!SevereFrontalCrash(contact,impulse,previousVelocity.Length()))return;
            var driver=car.GetPedOnSeat(VehicleSeat.Driver);
            if(driver==null||!driver.Exists()||!driver.IsAlive||driver.IsPersistent||driver.IsInvincible||driver==Game.Player.Character)return;
            int type=Function.Call<int>(Hash.GET_PED_TYPE,driver);if(type!=4&&type!=5)return;
            // Mission actors/crew are persistent. Do not kill anybody while still seated.
            if(!ExitVehicleStep.ForceOut(driver))return;
            Function.Call(Hash.SMASH_VEHICLE_WINDOW,car,6);
            var bounds=car.Model.Dimensions;
            driver.Position=car.Position+car.ForwardVector*(bounds.Item2.Y+.5f)+new Vector3(0,0,.8f);
            Function.Call(Hash.SET_PED_TO_RAGDOLL,driver,3000,3000,0,false,false,false);
            driver.Velocity=car.ForwardVector*Math.Min(25f,previousVelocity.Length())+new Vector3(0,0,3f);
            driver.Health=0;
        }
        internal static bool TryDent(Vector3 contact, Vector3 min, Vector3 max, float impulse, float strength, out Vector3 offset, out float damage)
        {
            offset = Vector3.Zero; damage = 0f;
            if (float.IsNaN(impulse) || float.IsInfinity(impulse) || impulse < 3.5f || impulse > 100f ||
                float.IsNaN(strength) || float.IsInfinity(strength) || max.X <= min.X || max.Z <= min.Z) return false;
            float x = Math.Abs(contact.X), y = Math.Abs(contact.Y), z = Math.Abs(contact.Z);
            if (contact.Z > .6f && z > x && z > y)
                offset = new Vector3((min.X + max.X) * .5f, (min.Y + max.Y) * .5f, max.Z - .05f);
            else if (x > .6f && x > y && x > z)
                offset = new Vector3(contact.X < 0f ? min.X + .05f : max.X - .05f,
                    (min.Y + max.Y) * .5f, min.Z + (max.Z - min.Z) * .45f);
            else return false; // Engine already handles front/rear; do not crush the underbody.
            damage = Math.Min(180f, Math.Max(25f, (impulse - 2f) * 12f * Math.Max(.25f, Math.Min(2f, strength))));
            return true;
        }
    }
}
