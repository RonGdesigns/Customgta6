using System.Collections.Generic;
using Bloodlines.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Crew
{
    /// <summary>Independent, delayed NPC recovery; never runs the player's death path.</summary>
    public sealed class CompanionRecovery
    {
        private readonly Dictionary<CrewSlot, int> _downedAt = new Dictionary<CrewSlot, int>();
        private readonly Dictionary<CrewSlot, int> _retryAt = new Dictionary<CrewSlot, int>();
        public void Forget(CrewSlot slot) { _downedAt.Remove(slot); _retryAt.Remove(slot); }
        public void Clear() { _downedAt.Clear(); _retryAt.Clear(); }
        public bool TryGetDestination(CrewSlot slot, Ped downed, Ped player, bool allowed, out Vector3 point)
        {
            point = Vector3.Zero;
            if (downed != null && downed.Exists() && !downed.IsDead) { Forget(slot); return false; }
            if (!_downedAt.TryGetValue(slot, out var since))
            {
                _downedAt[slot] = Game.GameTime;
                GameUtils.Notify("~o~" + Protagonist.Of(slot).DisplayName + " is down. Recovery takes about 45 seconds in free roam.");
                return false;
            }
            if (!allowed || player == null || !player.Exists() || player.IsDead || Game.GameTime - since < 45000 ||
                _retryAt.TryGetValue(slot, out var retry) && Game.GameTime < retry) return false;
            _retryAt[slot] = Game.GameTime + 3000;
            var forward = player.ForwardVector; var right = new Vector3(forward.Y, -forward.X, 0f);
            foreach (var offset in new[] { forward * -160f, right * 160f, right * -160f, forward * 180f })
            {
                var road = World.GetNextPositionOnStreet(player.Position + offset);
                if (road == Vector3.Zero) continue;
                var safe = World.GetSafeCoordForPed(road, false, 0);
                float distance = safe.DistanceTo(player.Position);
                if (safe == Vector3.Zero || distance < 100f || distance > 250f ||
                    Function.Call<bool>(Hash.IS_SPHERE_VISIBLE, safe.X, safe.Y, safe.Z, 10f) ||
                    Function.Call<bool>(Hash.IS_POSITION_OCCUPIED, safe.X, safe.Y, safe.Z, 2f, false, true, true, false, false, 0, false)) continue;
                Function.Call(Hash.REQUEST_COLLISION_AT_COORD, safe.X, safe.Y, safe.Z);
                point = safe; return true;
            }
            return false;
        }
    }
}
