using System;
namespace Bloodlines.Missions
{
    /// <summary>Only connected frames count; leaving range pauses the existing work.</summary>
    public sealed class ProximityHack
    {
        private readonly int _durationMs;
        private int _lastTime, _connectedMs;
        private bool _connected;
        public ProximityHack(int durationSeconds) { _durationMs = Math.Max(1, durationSeconds) * 1000; }
        public float Progress => Math.Min(1f, (float)_connectedMs / _durationMs);
        public void Update(int now, bool connected)
        {
            if (connected && _connected) _connectedMs += Math.Max(0, Math.Min(1000, now - _lastTime));
            _connected = connected;
            _lastTime = now;
        }
    }
}
