using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;

namespace Bloodlines.Missions
{
    /// <summary>
    /// The live world of one operation attempt. Entity references never enter the
    /// campaign save. Child phases borrow these exact entities; a missing borrowed
    /// entity is an error, not permission to spawn a look-alike beside it. Only the
    /// parent disposes this world, after the operation passes, fails or is aborted.
    ///
    /// Everything here is the part that is the same for every operation: ownership,
    /// the named handles the phases pass between themselves, the crew check and the
    /// single disposal. What differs is what a given operation considers intact, so
    /// <see cref="ValidateActive"/> and <see cref="ValidatePhaseEnd"/> stay abstract.
    /// </summary>
    public abstract class OperationWorld
    {
        private readonly Dictionary<int, Entity> _owned = new Dictionary<int, Entity>();
        private readonly HashSet<int> _keep = new HashSet<int>();
        private readonly Dictionary<string, Entity> _named = new Dictionary<string, Entity>(StringComparer.Ordinal);
        private readonly Dictionary<int, float> _power = new Dictionary<int, float>();
        private readonly Dictionary<int, bool> _invincible = new Dictionary<int, bool>();
        private bool _disposed;

        protected OperationWorld(MissionContext context) { Context = context; }
        protected MissionContext Context { get; }

        /// <summary>The operation's name as the player-facing failure text says it.</summary>
        public abstract string Label { get; }
        /// <summary>The one instruction a lost requirement carries: the whole thing starts over.</summary>
        public string RestartNotice => "Restart the entire " + Label + ".";

        public bool Continuing { get; set; }
        public string PhaseId { get; set; }
        public Dictionary<string, string> CargoChanges { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public int OwnedCount => _owned.Count;

        public void Own(Entity entity)
        {
            if (entity == null || !entity.Exists() || _owned.ContainsKey(entity.Handle)) return;
            _owned.Add(entity.Handle, entity);
            _invincible[entity.Handle] = entity.IsInvincible;
            if (entity is Vehicle vehicle)
            {
                // SHVDN builds differ on whether the public power multiplier is readable.
                // Preserve it when exposed; otherwise restore this script's neutral factor.
                var property = typeof(Vehicle).GetProperty("EnginePowerMultiplier");
                _power[entity.Handle] = property != null && property.CanRead ? Convert.ToSingle(property.GetValue(vehicle)) : 1f;
            }
            entity.IsPersistent = true;
        }

        public bool Owns(Entity entity) => entity != null && _owned.ContainsKey(entity.Handle);
        public void KeepAfterSuccess(Entity entity) { if (entity != null) { Own(entity); _keep.Add(entity.Handle); } }

        public T Bind<T>(string key, T entity) where T : Entity
        {
            if (entity == null || !entity.Exists() || entity.IsDead)
                throw new InvalidOperationException(Label + ": required " + key + " is unavailable.");
            if (_named.TryGetValue(key, out var previous) && previous.Handle != entity.Handle)
                throw new InvalidOperationException(Label + ": attempted to replace live " + key + ".");
            Own(entity);
            _named[key] = entity;
            return entity;
        }

        public T Get<T>(string key) where T : Entity => _named.TryGetValue(key, out var entity) ? entity as T : null;
        public T Require<T>(string key) where T : Entity
        {
            var entity = Get<T>(key);
            if (entity == null || !entity.Exists() || entity.IsDead || (entity is Vehicle vehicle && !vehicle.IsDriveable))
                throw new InvalidOperationException(Label + ": the " + key + " was lost. " + RestartNotice);
            return entity;
        }

        /// <summary>True while every named handle listed is alive and, for vehicles, drivable.</summary>
        protected bool Intact(IEnumerable<string> keys, out string reason)
        {
            foreach (var key in keys)
            {
                var entity = Get<Entity>(key);
                if (entity == null || !entity.Exists() || entity.IsDead || entity is Vehicle v && !v.IsDriveable)
                { reason = "The operation's " + key + " was lost. " + RestartNotice; return false; }
            }
            reason = null;
            return true;
        }

        public void StageCargo(string key, string destination) { CargoChanges[key] = destination; }
        public string CargoAt(string key) => CargoChanges.TryGetValue(key, out var destination) ? destination : Context.State?.CargoAt(key);

        public bool CrewReady(out string reason)
        {
            foreach (var hero in Protagonist.All)
            {
                var ped = Context.Crew.PedFor(hero.Slot);
                if (ped == null || !ped.Exists() || ped.IsDead)
                { reason = hero.Handle + " is down or missing. " + RestartNotice; return false; }
            }
            reason = null;
            return true;
        }

        public static bool Seated(Ped ped, Vehicle vehicle, VehicleSeat seat) =>
            ped != null && ped.Exists() && !ped.IsDead && vehicle != null && vehicle.Exists() && vehicle.IsDriveable &&
            ped.IsInVehicle(vehicle) && vehicle.GetPedOnSeat(seat) == ped;

        public static bool Attached(Prop prop, Entity carrier) =>
            prop != null && prop.Exists() && carrier != null && carrier.Exists() &&
            GTA.Native.Function.Call<bool>(GTA.Native.Hash.IS_ENTITY_ATTACHED_TO_ENTITY, prop, carrier);

        /// <summary>Whether the running phase still has everything it needs, checked every frame.</summary>
        public abstract bool ValidateActive(Mission phase, out string reason);
        /// <summary>Whether the phase that just passed actually left the state the next one loads.</summary>
        public abstract bool ValidatePhaseEnd(Mission phase, out string reason);

        public void Dispose(bool successful)
        {
            if (_disposed) return;
            _disposed = true;
            // Do not delete a player or an occupied vehicle, including after failure.
            // Restore temporary flight/hold state before handing transport back.
            foreach (var entity in _owned.Values.Reverse())
            {
                try
                {
                    if (entity == null || !entity.Exists()) continue;
                    if (entity.Handle == Game.Player.Character?.Handle || Protagonist.All.Any(h => Context.Crew.PedFor(h.Slot)?.Handle == entity.Handle)) continue;
                    if (entity is Vehicle vehicle)
                    {
                        vehicle.IsPositionFrozen = false;
                        if (_invincible.TryGetValue(vehicle.Handle, out bool protectedBefore)) vehicle.IsInvincible = protectedBefore;
                        if (_power.TryGetValue(vehicle.Handle, out var power)) vehicle.EnginePowerMultiplier = power;
                        bool occupied = Game.Player.Character != null && Game.Player.Character.IsInVehicle(vehicle) ||
                            Protagonist.All.Any(h => Context.Crew.PedFor(h.Slot)?.IsInVehicle(vehicle) == true);
                        if (occupied || successful && _keep.Contains(entity.Handle) || entity.IsDead)
                        { GameUtils.SafeRelease(entity); continue; }
                    }
                    if (successful && _keep.Contains(entity.Handle) || entity.IsDead) GameUtils.SafeRelease(entity);
                    else GameUtils.SafeDelete(entity);
                }
                catch (Exception ex) { Logger.Error(Label + " world cleanup", ex); }
            }
            _owned.Clear(); _named.Clear(); _keep.Clear(); _power.Clear(); _invincible.Clear();
        }
    }
}
