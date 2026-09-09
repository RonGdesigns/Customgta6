using System;
using System.Collections.Generic;

namespace Bloodlines.Crew
{
    /// <summary>Free-roam heat belongs to a character, not to the global player handle.</summary>
    public sealed class PersonalWanted
    {
        private readonly Dictionary<CrewSlot, int> _levels = new Dictionary<CrewSlot, int>();
        public int Get(CrewSlot slot) => _levels.TryGetValue(slot, out var value) ? value : 0;
        public void Set(CrewSlot slot, int level) { _levels[slot] = Math.Max(0, Math.Min(6, level)); }
        public void Capture(CrewSlot slot, int nativeLevel) { Set(slot, Get(slot) == 6 && nativeLevel == 5 ? 6 : nativeLevel); }
        public void Clear() { _levels.Clear(); }
    }
}
