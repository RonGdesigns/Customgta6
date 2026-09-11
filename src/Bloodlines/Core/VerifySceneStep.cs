using System;

namespace Bloodlines.Core
{
    /// <summary>
    /// A required scene result, checked after its physical steps. A failed test
    /// cancels the remaining blocking; success callbacks never run speculatively.
    /// Watching and deliberate skipping use the same check. Cancel does nothing.
    /// </summary>
    public sealed class VerifySceneStep : SceneStep
    {
        private readonly string _label;
        private readonly Func<bool> _check;
        private readonly Action _commit;
        private bool _attempted;
        public VerifySceneStep(string label, Func<bool> check, Action commit = null)
        {
            _label = label;
            _check = check ?? throw new ArgumentNullException(nameof(check));
            _commit = commit;
        }
        protected override void OnStart() { Verify(); }
        public override bool IsComplete => _attempted;
        public override void Finish() { Verify(); }
        public override void Cancel() { }
        private void Verify()
        {
            if (_attempted) return;
            _attempted = true;
            try
            {
                if (!_check()) { Failed = true; Logger.Error("Required scene result failed: " + _label); return; }
                _commit?.Invoke();
            }
            catch (Exception ex) { Failed = true; Logger.Error("Required scene result failed: " + _label, ex); }
        }
    }
}
