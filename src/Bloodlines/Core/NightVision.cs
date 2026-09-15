using System;
using System.Collections.Generic;
using System.Linq;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// Who is holding night vision on. Same shape as <see cref="WorldLights"/>, and for the
    /// same reason: the engine has one switch and no way to ask what it is currently set to,
    /// so two things that both want the goggles will fight and whichever finishes first turns
    /// them off under the other one.
    ///
    /// It matters more here than it looks. Night vision left on is not a cosmetic slip — it
    /// is a green screen the player cannot clear from any menu, on a save he then keeps
    /// playing. A mission that toggles the native itself has to get every exit right: pass,
    /// failure, abort, death, and the teardown that runs after all of them. Holding by name
    /// and releasing by name means the mission only has to let go once.
    ///
    /// M53 is the only holder today. It is written this way because the artificial-lights
    /// switch was not, and that cost a real bug.
    /// </summary>
    public static class NightVision
    {
        private static readonly HashSet<string> Holders = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>True while anything is holding the goggles on.</summary>
        public static bool Active => Holders.Count > 0;
        /// <summary>Who is holding them, for a log line or a diagnostic page.</summary>
        public static IEnumerable<string> Held => Holders.ToArray();

        /// <summary>Goggles down. Calling twice for the same owner changes nothing.</summary>
        public static void Wear(string owner)
        {
            if (string.IsNullOrWhiteSpace(owner)) throw new ArgumentException("Night vision needs an owner.", nameof(owner));
            bool first = Holders.Count == 0;
            if (!Holders.Add(owner) && !first) return;
            Apply(true);
            if (first) Logger.Info("Night vision on, held by " + owner + ".");
        }

        /// <summary>Let go. The goggles come up only if nobody else is still holding them.</summary>
        public static void Remove(string owner)
        {
            if (owner == null || !Holders.Remove(owner)) return;
            if (Holders.Count > 0)
            {
                Logger.Info("Night vision stays on: " + string.Join(", ", Holders) + " still holding.");
                return;
            }
            Apply(false);
            Logger.Info("Night vision off; " + owner + " was the last holder.");
        }

        /// <summary>Teardown. Drops every claim and takes the goggles off.</summary>
        public static void Reset()
        {
            if (Holders.Count == 0) return;
            Holders.Clear();
            Apply(false);
            Logger.Info("Night vision reset: every claim dropped.");
        }

        private static void Apply(bool on)
        {
            try { Function.Call(Hash.SET_NIGHTVISION, on); }
            catch (Exception ex) { Logger.Error("Setting night vision", ex); }
        }
    }
}
