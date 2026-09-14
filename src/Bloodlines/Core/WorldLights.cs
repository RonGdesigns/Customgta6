using System;
using System.Collections.Generic;
using System.Linq;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// Who is holding the city's lights off. The engine has one artificial-lights
    /// switch and no way to ask what it is currently set to, so two things that both
    /// want darkness will fight: whichever finishes first turns the lights back on
    /// under the other one. M04 and M16 already black the city out for their own
    /// reasons, and Gohan's blackout can happen during either.
    ///
    /// So the switch is held by name and released by name, and the lights only come
    /// back when the last holder lets go — the same discipline
    /// <see cref="ScriptedMap"/> uses for requested map.
    /// </summary>
    public static class WorldLights
    {
        private static readonly HashSet<string> Holders = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>True while anything is holding the lights off.</summary>
        public static bool Dark => Holders.Count > 0;
        /// <summary>Who is holding them, for a log line or a diagnostic page.</summary>
        public static IEnumerable<string> Held => Holders.ToArray();

        /// <summary>Take the lights down. Calling twice for the same owner changes nothing.</summary>
        public static void Darken(string owner)
        {
            if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("Darkness needs an owner.", nameof(owner));
            bool first = Holders.Count == 0;
            if (!Holders.Add(owner) && !first) return;
            Apply(true);
            if (first) Logger.Info("Artificial lights off, held by " + owner + ".");
        }

        /// <summary>
        /// Let go. The lights return only if nobody else is still holding them, which
        /// is what stops an ability ending from canceling a mission's own blackout.
        /// </summary>
        public static void Restore(string owner)
        {
            if (owner == null || !Holders.Remove(owner)) return;
            if (Holders.Count > 0)
            {
                Logger.Info("Artificial lights stay off: " + string.Join(", ", Holders) + " still holding.");
                return;
            }
            Apply(false);
            Logger.Info("Artificial lights restored; " + owner + " was the last holder.");
        }

        /// <summary>Teardown. Drops every claim and puts the lights back.</summary>
        public static void Reset()
        {
            if (Holders.Count == 0) return;
            Holders.Clear();
            Apply(false);
            Logger.Info("Artificial lights reset: every claim dropped.");
        }

        private static void Apply(bool dark)
        {
            try { Function.Call(Hash.SET_ARTIFICIAL_LIGHTS_STATE, dark); }
            catch (Exception ex) { Logger.Error("Setting the artificial lights state", ex); }
        }
    }
}
