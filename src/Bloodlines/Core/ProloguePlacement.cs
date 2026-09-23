using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>Exterior M01 staging, verified against loaded pedestrian navigation.</summary>
    public static class ProloguePlacement
    {
        /// <summary>
        /// How far Ice's lookout has to stay from where he starts. The marker sits
        /// close by on purpose — the walk is a beat, not a hike — so this is what
        /// stops the two collapsing into one spot and skipping the approach.
        /// </summary>
        private const float IceApproachMinimum = 12f;

        /// <summary>How far the service terminal has to stay from Mateo, so Gohan's hack is not done in front of him.</summary>
        private const float TerminalMateoMinimum = 30f;

        // One attempt asks for the dock twice: the cold open resolves it before its
        // first shot, and the mission's own start asks again. The second pass used to
        // snap the already-snapped points, each up to 30 m further, and on Ron's
        // September 22 run that walked the terminal inside 30 m of Mateo and refused
        // M01 three times. The dock is now resolved once, from the authored points,
        // and every later call in the same attempt reuses that answer.
        private static LocationBook _book;
        private static readonly Dictionary<MissionLocation, Vector3> Authored = new Dictionary<MissionLocation, Vector3>();
        private static readonly Dictionary<MissionLocation, Vector3> Applied = new Dictionary<MissionLocation, Vector3>();

        /// <summary>
        /// Puts every key this placement moved back where the author left it, so one
        /// attempt's correction is never the next attempt's starting point. A key
        /// somebody has edited since (a survey capture) is his, and is left alone.
        /// </summary>
        public static void Restore()
        {
            foreach (var pair in Applied)
                if (pair.Key.Position == pair.Value && Authored.TryGetValue(pair.Key, out var authored))
                    pair.Key.Position = authored;
            Applied.Clear();
            Authored.Clear();
            _book = null;
        }

        public static bool Prepare(LocationBook book)
        {
            if (book == null) return false;
            if (!ReferenceEquals(book, _book)) { Applied.Clear(); Authored.Clear(); _book = book; }
            // Already resolved this attempt, and nothing has moved a key since.
            if (Applied.Count > 0 && Applied.All(pair => pair.Key.Position == pair.Value))
            {
                Logger.Info("M01 dock placement already resolved for this attempt; reusing it.");
                return true;
            }
            // Validate all placements before moving anyone or changing any live keys.
            var resolved = new Dictionary<MissionLocation, Vector3>();
            var sources = new Dictionary<MissionLocation, Vector3>();
            try
            {
                // The exit is not here: it is a drive-to marker at the far gate, reached
                // in a car in the last stage, and nothing stands on it. Requiring its
                // navmesh at the cold open refused M01 after the prologue hand-off,
                // when the player had just been placed at the dock and nothing 200 m
                // up the road had streamed yet.
                foreach (string key in new[] { "M01.IceApproach", "M01.GuessApproach", "M01.GohanApproach", "M01.CraneNest", "M01.ServiceTerminal", "M01.PrototypeCar", "M01.CapoSpawn", "M01.RegroupPoint" })
                {
                    var location = book.Get(key);
                    if (location == null) { Logger.Error("M01 could not find a loaded walkable surface for " + key); return false; }
                    // Resolve from what the author wrote, never from an earlier pass's
                    // correction; a key edited since that pass is taken as it now reads.
                    var source = Applied.TryGetValue(location, out var applied) && location.Position == applied &&
                                 Authored.TryGetValue(location, out var authored) ? authored : location.Position;
                    sources[location] = source;
                    if (!TryLand(source, out var point))
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
                    GameUtils.IsWithinFlat(resolved[book.Get("M01.GohanApproach")], resolved[book.Get("M01.ServiceTerminal")], 40f))
                { Logger.Error("M01 approach positions are too close to their objectives. Check surveyed overrides."); return false; }
                // The lookout is deliberately a short walk from Ice's start, so the
                // dock's own ground snapping can close the remaining gap. That is a
                // geometry problem, not a reason to refuse the mission: push the
                // marker back along the same bearing and land it again. Only a dock
                // with no free ground along that line gives up.
                var iceApproach = book.Get("M01.IceApproach");
                var lookout = book.Get("M01.CraneNest");
                if (GameUtils.IsWithinFlat(resolved[iceApproach], resolved[lookout], IceApproachMinimum))
                {
                    var from = resolved[iceApproach];
                    var bearing = Flat(from, resolved[lookout]);
                    // Collapsed onto the same spot: fall back to the line toward Mateo,
                    // which is the direction the lookout has to face anyway.
                    if (bearing == Vector3.Zero) bearing = Flat(from, resolved[book.Get("M01.CapoSpawn")]);
                    if (bearing == Vector3.Zero)
                    { Logger.Error("M01 lookout, Ice's approach and Mateo all resolved to one point."); return false; }
                    if (!TryLand(from + bearing * (IceApproachMinimum + 4f), out var pushed))
                    { Logger.Error("M01 lookout collapsed onto Ice's approach and no ground was free further along the bearing."); return false; }
                    Logger.Warn("M01 lookout resolved too close to Ice's approach; pushed it out to " + pushed + ".");
                    resolved[lookout] = pushed;
                }
                // Ground snapping and saved overrides must never put the hacker
                // beside Mateo. Check after resolving both positions, before spawning.
                // Snapping closing the gap is the same geometry problem as the
                // lookout's, so the terminal is pushed away from Mateo along the line
                // between them; only a terminal sitting on Mateo himself, or a push
                // that lands on Gohan's own start, still refuses.
                var terminal = book.Get("M01.ServiceTerminal");
                var mateo = book.Get("M01.CapoSpawn");
                if (GameUtils.IsWithinFlat(resolved[terminal], resolved[mateo], TerminalMateoMinimum))
                {
                    var bearing = Flat(resolved[mateo], resolved[terminal]);
                    if (bearing == Vector3.Zero || !TryLand(resolved[mateo] + bearing * (TerminalMateoMinimum + 4f), out var pushed) ||
                        GameUtils.IsWithinFlat(pushed, resolved[mateo], TerminalMateoMinimum) ||
                        GameUtils.IsWithinFlat(resolved[book.Get("M01.GohanApproach")], pushed, 40f))
                    {
                        Logger.Error("M01 service terminal resolved too close to Mateo: terminal " + resolved[terminal] +
                                     ", Mateo " + resolved[mateo] + ", and no free ground further out along that line.");
                        GameUtils.Notify("M01 terminal is too close to Mateo. Re-survey M01.ServiceTerminal at least 30m away, then retry.");
                        return false;
                    }
                    Logger.Warn("M01 service terminal resolved at " + resolved[terminal] + ", too close to Mateo at " +
                                resolved[mateo] + "; pushed it out to " + pushed + ".");
                    resolved[terminal] = pushed;
                }
                foreach (var pair in resolved)
                {
                    Logger.Info("M01 surface " + pair.Key.Key + ": " + sources[pair.Key] + " -> " + pair.Value);
                    if (!Authored.ContainsKey(pair.Key) || !Applied.TryGetValue(pair.Key, out var was) || pair.Key.Position != was)
                        Authored[pair.Key] = sources[pair.Key];
                    pair.Key.Position = pair.Value; // Session adjustment, not a claimed manual survey.
                    Applied[pair.Key] = pair.Value;
                }
                return true;
            }
            finally { Function.Call(Hash.CLEAR_FOCUS); }
        }

        /// <summary>Unit vector from one point to another, ignoring height. Zero when they share a spot.</summary>
        private static Vector3 Flat(Vector3 from, Vector3 to)
        {
            float x = to.X - from.X, y = to.Y - from.Y;
            float length = (float)System.Math.Sqrt(x * x + y * y);
            return length < 0.5f ? Vector3.Zero : new Vector3(x / length, y / length, 0f);
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
