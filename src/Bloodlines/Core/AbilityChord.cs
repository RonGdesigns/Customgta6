namespace Bloodlines.Core
{
    /// <summary>One activation per two-stick press; both must release to rearm.</summary>
    public sealed class AbilityChord
    {
        private bool _latched;
        public bool Update(bool left, bool right, bool blocked)
        {
            if (!left && !right) { _latched = false; return false; }
            if (blocked) { _latched = true; return false; }
            if (_latched || !left || !right) return false;
            _latched = true;
            return true;
        }
    }
}
