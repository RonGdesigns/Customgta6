using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>Exterior M01 staging, verified against loaded pedestrian navigation.</summary>
    public static class ProloguePlacement
    {
        public static bool Prepare(LocationBook book)
        {
            // Validate all placements before moving anyone or changing any live keys.
            var resolved = new Dictionary<MissionLocation, Vector3>();
            try
            {
                foreach (string key in new[] { "M01.GuessApproach", "M01.GohanApproach", "M01.CraneNest", "M01.LowerDeckLedger", "M01.PrototypeCar", "M01.CapoSpawn", "M01.RegroupPoint", "M01.ExitPoint" })
                {
                    var location = book.Get(key);
                    if (location == null || !TryLand(location.Position, out var point))
                    {
                        Logger.Error("M01 could not find a loaded walkable surface for " + key);
                        return false;
                    }
                    resolved[location] = point;
                }
                var approach = book.Get("M01.GuessApproach");
                var road = World.GetNextPositionOnStreet(resolved[approach]);
                if (road == Vector3.Zero || road.Z < 1f || road.Z > 20f ||
                    !GameUtils.IsWithinFlat(road, resolved[approach], 60f))
                { Logger.Error("M01 approach road is not loaded."); return false; }
                resolved[approach] = road + new Vector3(0f, 0f, .15f);
                if (GameUtils.IsWithinFlat(resolved[approach], resolved[book.Get("M01.PrototypeCar")], 80f) ||
                    GameUtils.IsWithinFlat(resolved[book.Get("M01.GohanApproach")], resolved[book.Get("M01.LowerDeckLedger")], 40f))
                { Logger.Error("M01 approach positions are too close to their objectives. Check surveyed overrides."); return false; }
                foreach (var pair in resolved)
                {
                    Logger.Info("M01 surface " + pair.Key.Key + ": " + pair.Key.Position + " -> " + pair.Value);
                    pair.Key.Position = pair.Value; // Session adjustment, not a claimed manual survey.
                }
                return true;
            }
            finally { Function.Call(Hash.CLEAR_FOCUS); }
        }

        public static bool TryLand(Vector3 requested, out Vector3 position)
        {
            position = Vector3.Zero;
            // M01's stock dock floor is near sea level. The missing yacht's sublevel
            // and crane height must not be accepted just because a PDF lists them.
            var probe = new Vector3(requested.X, requested.Y, 6f);
            Function.Call(Hash.SET_FOCUS_POS_AND_VEL, probe.X, probe.Y, probe.Z, 0f, 0f, 0f);
            try
            {
                for (int attempt = 0; attempt < 12; attempt++)
                {
                    Function.Call(Hash.REQUEST_COLLISION_AT_COORD, probe.X, probe.Y, probe.Z);
                    var safe = World.GetSafeCoordForPed(probe, false, 0);
                    if (safe != Vector3.Zero && safe.Z >= 1f && safe.Z <= 20f &&
                        GameUtils.IsWithinFlat(safe, probe, 30f))
                    {
                        position = safe + new Vector3(0f, 0f, 0.1f);
                        return true;
                    }
                    Script.Wait(50);
                }
                return false;
            }
            finally { Function.Call(Hash.CLEAR_FOCUS); }
        }
    }
}
