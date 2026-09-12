using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions
{
    /// <summary>
    /// One attempt's live world. Entity references never enter the campaign save.
    /// Child phases borrow these exact entities; a missing borrowed entity is an
    /// error, not permission to spawn a look-alike beside it. Only the parent
    /// disposes this world, after the operation passes, fails or is aborted.
    /// </summary>
    public sealed class PortHeistWorld
    {
        private readonly MissionContext _context;
        private readonly Dictionary<int, Entity> _owned = new Dictionary<int, Entity>();
        private readonly HashSet<int> _keep = new HashSet<int>();
        private readonly Dictionary<string, Entity> _named = new Dictionary<string, Entity>(StringComparer.Ordinal);
        private readonly Dictionary<int, float> _power = new Dictionary<int, float>();
        private readonly Dictionary<int, bool> _invincible = new Dictionary<int, bool>();
        private bool _disposed;

        public PortHeistWorld(MissionContext context) { _context = context; }
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
                throw new InvalidOperationException("Port Heist: required " + key + " is unavailable.");
            if (_named.TryGetValue(key, out var previous) && previous.Handle != entity.Handle)
                throw new InvalidOperationException("Port Heist: attempted to replace live " + key + ".");
            Own(entity);
            _named[key] = entity;
            return entity;
        }

        public T Get<T>(string key) where T : Entity => _named.TryGetValue(key, out var entity) ? entity as T : null;
        public T Require<T>(string key) where T : Entity
        {
            var entity = Get<T>(key);
            if (entity == null || !entity.Exists() || entity.IsDead || (entity is Vehicle vehicle && !vehicle.IsDriveable))
                throw new InvalidOperationException("Port Heist: the " + key + " was lost. Restart the entire Port Heist.");
            return entity;
        }

        public void StageCargo(string key, string destination) { CargoChanges[key] = destination; }
        public string CargoAt(string key) => CargoChanges.TryGetValue(key, out var destination) ? destination : _context.State?.CargoAt(key);

        public bool CrewReady(out string reason)
        {
            foreach (var hero in Protagonist.All)
            {
                var ped = _context.Crew.PedFor(hero.Slot);
                if (ped == null || !ped.Exists() || ped.IsDead)
                { reason = hero.Handle + " is down or missing. Restart the entire Port Heist."; return false; }
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

        public bool ValidateActive(Mission phase, out string reason)
        {
            if (!CrewReady(out reason)) return false;
            var keys = new List<string> { "lift" };
            if (phase is Campaign.M19UnderwaterBreach) keys.Add("kraken");
            if (!(phase is Campaign.M19UnderwaterBreach) || Get<Prop>("bullion") != null) keys.Add("bullion");
            if (phase is Campaign.M20SkyHook hook && !hook.Transferred) { keys.Add("kraken"); keys.Add("launch"); }
            if (phase is Campaign.M21OpenWater escort) { if (!escort.Transferred) keys.Add("launch"); keys.Add("granger"); }
            if (phase is Campaign.M22ScorchedBay) keys.Add("granger");
            foreach (var key in keys)
            {
                var entity = Get<Entity>(key);
                if (entity == null || !entity.Exists() || entity.IsDead || entity is Vehicle v && !v.IsDriveable)
                { reason = "The operation's " + key + " was lost. Restart the entire Port Heist."; return false; }
            }
            bool cableRequired = phase is Campaign.M20SkyHook h && h.Hooked || phase is Campaign.M21OpenWater ||
                phase is Campaign.M22ScorchedBay b && !b.Dropped;
            if (cableRequired && !Attached(Get<Prop>("bullion"), Get<Vehicle>("lift")))
            { reason = "The bullion separated from the lift before its planned drop."; return false; }
            reason = null;
            return true;
        }

        public bool ValidatePhaseEnd(Mission phase, out string reason)
        {
            if (!CrewReady(out reason)) return false;
            var lift = Get<Vehicle>("lift");
            var cargo = Get<Prop>("bullion");
            if (lift == null || !lift.Exists() || !lift.IsDriveable || cargo == null || !cargo.Exists())
            { reason = "The loaded operation state is incomplete. Restart the entire Port Heist."; return false; }
            var guess = _context.Crew.PedFor(CrewSlot.Guess);
            var ice = _context.Crew.PedFor(CrewSlot.Ice);
            var gohan = _context.Crew.PedFor(CrewSlot.Gohan);
            if (phase is Campaign.M19UnderwaterBreach breach)
            {
                if (!breach.Floated || !Seated(gohan, breach.Kraken, VehicleSeat.Driver) ||
                    breach.Kraken.Position.DistanceTo(_context.Locations.Position("M19.Surface")) > 16f)
                { reason = "Surface the Kraken with the floated container ready before the lift."; return false; }
            }
            else if (phase is Campaign.M20SkyHook hook)
            {
                if (!hook.Hooked || !hook.Transferred || !Attached(cargo, lift) ||
                    !Seated(guess, lift, VehicleSeat.Driver) || !Seated(gohan, hook.Launch, VehicleSeat.Driver) ||
                    !Seated(ice, hook.Launch, VehicleSeat.Passenger))
                { reason = "The cable and escort seats are not ready. Restart the entire Port Heist."; return false; }
            }
            else if (phase is Campaign.M21OpenWater escort)
            {
                if (!escort.Transferred || !Attached(cargo, lift) || !Seated(guess, lift, VehicleSeat.Driver) ||
                    !Seated(gohan, escort.Granger, VehicleSeat.Driver) || !Seated(ice, escort.Granger, VehicleSeat.Passenger))
                { reason = "The shore transfer did not finish with the crew and bullion intact."; return false; }
            }
            else if (phase is Campaign.M22ScorchedBay bay)
            {
                var beach = _context.Locations.Position("M22.Beach");
                var drop = _context.Locations.Position("M22.AlamoDrop");
                if (!bay.Arrived || !bay.Dropped || !bay.Landed || !bay.Struck || Attached(cargo, lift) ||
                    cargo.Position.DistanceTo(drop) > 15f || lift.IsInAir || lift.Speed > 2f ||
                    Protagonist.All.Any(hero => { var ped = _context.Crew.PedFor(hero.Slot); return ped.IsInVehicle() || ped.Position.DistanceTo(beach) > 45f; }))
                { reason = "The cargo must be deposited, the aircraft landed and all three men on the beach."; return false; }
            }
            reason = null;
            return true;
        }

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
                    if (entity.Handle == Game.Player.Character?.Handle || Protagonist.All.Any(h => _context.Crew.PedFor(h.Slot)?.Handle == entity.Handle)) continue;
                    if (entity is Vehicle vehicle)
                    {
                        vehicle.IsPositionFrozen = false;
                        if (_invincible.TryGetValue(vehicle.Handle, out bool protectedBefore)) vehicle.IsInvincible = protectedBefore;
                        if (_power.TryGetValue(vehicle.Handle, out var power)) vehicle.EnginePowerMultiplier = power;
                        bool occupied = Game.Player.Character != null && Game.Player.Character.IsInVehicle(vehicle) ||
                            Protagonist.All.Any(h => _context.Crew.PedFor(h.Slot)?.IsInVehicle(vehicle) == true);
                        if (occupied || successful && _keep.Contains(entity.Handle) || entity.IsDead)
                        { GameUtils.SafeRelease(entity); continue; }
                    }
                    if (successful && _keep.Contains(entity.Handle) || entity.IsDead) GameUtils.SafeRelease(entity);
                    else GameUtils.SafeDelete(entity);
                }
                catch (Exception ex) { Logger.Error("Port Heist world cleanup", ex); }
            }
            _owned.Clear(); _named.Clear(); _keep.Clear(); _power.Clear(); _invincible.Clear();
        }
    }
}
