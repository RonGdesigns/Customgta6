using System;
using System.Collections.Generic;
using GTA;

namespace Bloodlines.Core
{
    /// <summary>
    /// Blips that belong to people, and that go away when the person does.
    ///
    /// A blip added with <c>ped.AddBlip()</c> is an independent entity. Killing the ped does
    /// not remove it, so the dot stays on the radar over a corpse — Ron reported that across
    /// a lot of missions, and every one of them had added its blips by hand and never looked
    /// at them again. A map full of dots for men who are already down is worse than no dots:
    /// it tells the player there is still a fight where there is not.
    ///
    /// Attach through here and call <see cref="Update"/> each frame. The blip is removed the
    /// first frame its ped is dead or gone.
    /// </summary>
    public sealed class TargetBlips : IDisposable
    {
        private readonly Dictionary<Ped, Blip> _blips = new Dictionary<Ped, Blip>();

        /// <summary>How many dots are still on the map.</summary>
        public int Count => _blips.Count;

        /// <summary>
        /// Put a dot on someone. Returns the blip so a caller can style it further, or null
        /// if the game would not give one.
        /// </summary>
        public Blip Attach(Ped ped, BlipColor color, string name)
        {
            if (ped == null || !ped.Exists() || _blips.ContainsKey(ped)) return null;
            try
            {
                var blip = ped.AddBlip();
                if (blip == null || !blip.Exists()) return null;
                blip.Color = color;
                if (!string.IsNullOrEmpty(name)) blip.Name = name;
                _blips[ped] = blip;
                return blip;
            }
            catch (Exception ex) { Logger.Warn("Could not blip a target: " + ex.Message); return null; }
        }

        /// <summary>Call every frame. A dot over a corpse is removed the frame it becomes one.</summary>
        public void Update()
        {
            if (_blips.Count == 0) return;
            List<Ped> gone = null;
            foreach (var pair in _blips)
            {
                var ped = pair.Key;
                if (ped != null && ped.Exists() && !ped.IsDead) continue;
                (gone ?? (gone = new List<Ped>())).Add(ped);
            }
            if (gone == null) return;
            foreach (var ped in gone)
            {
                GameUtils.SafeDelete(_blips[ped]);
                _blips.Remove(ped);
            }
        }

        /// <summary>Teardown. Every dot this owns goes.</summary>
        public void Dispose()
        {
            foreach (var blip in _blips.Values) GameUtils.SafeDelete(blip);
            _blips.Clear();
        }
    }
}
