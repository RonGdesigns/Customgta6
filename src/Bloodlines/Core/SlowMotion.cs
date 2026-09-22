using System;
using System.Collections.Generic;
using System.Linq;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// Who is holding time down, and at what rate.
    ///
    /// The engine has one time scale shared by abilities, mission beats and menus, so
    /// two things that both slow time will fight: whichever finishes first puts the clock
    /// back to normal underneath the other one. A mission beat or a menu can overlap
    /// Guess's <c>SlipstreamReflex</c>, so each holds its own claim.
    ///
    /// So it is held by name and released by name, the slowest holder wins, and time only
    /// returns to its prior rate when the last holder lets go — the same discipline
    /// <see cref="WorldLights"/> uses for the city's lights.
    /// </summary>
    public static class SlowMotion
    {
        /// <summary>The engine's own rate. Anything at or above this is not slow motion.</summary>
        public const float Normal = 1f;
        /// <summary>As slow as anything is allowed to ask for. Below this the game stops feeling responsive.</summary>
        public const float Floor = 0.2f;

        private static readonly Dictionary<string, float> Holders = new Dictionary<string, float>(StringComparer.Ordinal);
        private static float _applied = Normal;
        private static float _baseline = Normal;

        /// <summary>True while anything is holding time down.</summary>
        public static bool Slowed => Holders.Count > 0;
        /// <summary>The rate currently in force.</summary>
        public static float Rate => _applied;
        /// <summary>Who is holding it, for a log line or a diagnostic page.</summary>
        public static IEnumerable<string> Held => Holders.Keys.ToArray();

        /// <summary>
        /// Slow time down for this owner. Calling again with a different rate changes this
        /// owner's request. The slowest request in force is the one that applies, so a
        /// mission beat cannot speed an ability's slow motion back up.
        /// </summary>
        public static void Hold(string owner, float rate)
        {
            if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("Slow motion needs an owner.", nameof(owner));
            if (rate >= Normal) { Release(owner); return; }
            if (Holders.Count == 0) _baseline = Game.TimeScale;
            Holders[owner] = Math.Max(Floor, rate);
            Apply();
        }

        /// <summary>
        /// Let go. Time returns to its prior rate only when nobody else is holding it, which is
        /// what stops a mission beat ending from canceling an ability that is still running.
        /// </summary>
        public static void Release(string owner)
        {
            if (owner == null || !Holders.Remove(owner)) return;
            Apply();
        }

        /// <summary>Teardown. Drops every claim and puts the clock back.</summary>
        public static void Reset()
        {
            Holders.Clear();
            _baseline = Normal;
            Apply();
        }

        private static void Apply()
        {
            float rate = Holders.Count == 0 ? _baseline : Math.Min(_baseline, Holders.Values.Min());
            if (Math.Abs(rate - _applied) < 0.0001f && Math.Abs(Game.TimeScale - rate) < 0.0001f) return;
            try { Function.Call(Hash.SET_TIME_SCALE, rate); _applied = rate; }
            catch (Exception ex) { Logger.Error("Setting the time scale", ex); }
            Logger.Info(Holders.Count == 0
                ? "Time restored to " + rate.ToString("0.00") + "; the last holder let go."
                : "Time at " + rate.ToString("0.00") + ", held by " + string.Join(", ", Holders.Keys) + ".");
        }
    }
}
