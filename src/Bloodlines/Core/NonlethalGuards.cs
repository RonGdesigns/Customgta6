using System;
using System.Collections.Generic;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>Mission-owned stun targets: alive, cuffed and down until cleanup.
    /// Protection is configured BEFORE the shot. No global weapon damage changes,
    /// no resurrection, and no dead target counted as a nonlethal success.</summary>
    public sealed class NonlethalGuards : IDisposable
    {
        private readonly List<Ped> _guards = new List<Ped>();
        private readonly HashSet<int> _down = new HashSet<int>();
        public int DownCount => _down.Count;
        public bool IsDown(Ped ped) => ped != null && ped.Exists() && !ped.IsDead && _down.Contains(ped.Handle);

        public void Add(Ped guard)
        {
            if (guard == null || !guard.Exists()) throw new ArgumentException("A nonlethal guard must exist.", nameof(guard));
            guard.MaxHealth = Math.Max(guard.MaxHealth, 1000); guard.Health = guard.MaxHealth;
            Function.Call(Hash.SET_PED_SUFFERS_CRITICAL_HITS, guard, false);
            Function.Call(Hash.SET_PED_DIES_WHEN_INJURED, guard, false);
            // This setting only affects future hits; set it at spawn, not after stun.
            Function.Call(Hash.SET_PED_MIN_GROUND_TIME_FOR_STUNGUN, guard, 600000);
            _guards.Add(guard);
        }
        public void Update()
        {
            foreach (var guard in _guards)
            {
                if (guard == null || !guard.Exists() || guard.IsDead) continue;
                bool hit = guard.IsBeingStunned || Function.Call<bool>(Hash.HAS_PED_BEEN_DAMAGED_BY_WEAPON, guard, (uint)WeaponHash.StunGun, 0);
                if (hit && _down.Add(guard.Handle))
                {
                    // Native invincibility disables the stun/ragdoll animation.
                    // Protect the already-subdued actor by damage type instead.
                    Function.Call(Hash.SET_ENTITY_PROOFS, guard, true, true, true, true, true, true, true, true);
                    Function.Call(Hash.SET_ENABLE_HANDCUFFS, guard, true);
                    guard.Task.ClearAll();
                }
                if (_down.Contains(guard.Handle))
                {
                    // Keep incapacitation alive after the subdue objective ends,
                    // including later terminal/splice and withdrawal stages.
                    Function.Call(Hash.SET_PED_TO_RAGDOLL, guard, 2500, 2500, 0, false, false, false);
                }
            }
        }
        public void Dispose()
        {
            foreach (var guard in _guards)
            {
                if (guard == null || !guard.Exists()) continue;
                if (_down.Contains(guard.Handle))
                {
                    Function.Call(Hash.SET_ENTITY_PROOFS, guard, false, false, false, false, false, false, false, false);
                    Function.Call(Hash.SET_ENABLE_HANDCUFFS, guard, false);
                }
                Function.Call(Hash.SET_PED_MIN_GROUND_TIME_FOR_STUNGUN, guard, -1);
                Function.Call(Hash.SET_PED_DIES_WHEN_INJURED, guard, true);
                Function.Call(Hash.SET_PED_SUFFERS_CRITICAL_HITS, guard, true);
            }
            _guards.Clear(); _down.Clear();
        }
    }
}
