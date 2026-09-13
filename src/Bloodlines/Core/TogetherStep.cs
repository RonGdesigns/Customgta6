using System.Collections.Generic;
using System.Linq;
using GTA;

namespace Bloodlines.Core
{
    /// <summary>Independent actions share one bounded scene beat, including skip and abort.</summary>
    public sealed class TogetherStep : SceneStep
    {
        private readonly SceneStep[] _children;
        public TogetherStep(params SceneStep[] children)
        { _children = children; Actor = children.FirstOrDefault()?.Actor; TimeoutMs = 6500; }
        public override IEnumerable<Ped> Movers => _children.SelectMany(s => s.Movers).Distinct();
        public override Entity CameraTarget => _children.FirstOrDefault()?.CameraTarget;
        protected override void OnStart()
        {
            foreach (var step in _children)
            { step.TimeoutMs = TimeoutMs; step.Start(); if (step.Failed) { Failed = true; break; } }
        }
        public override bool IsComplete
        {
            get
            {
                bool done = true;
                foreach (var step in _children) { if (!step.IsComplete) done = false; if (step.Failed) Failed = true; }
                return done || Failed;
            }
        }
        public override void Finish()
        {
            foreach (var step in _children)
            { if (!step.IsComplete) step.Finish(); if (step.Failed) { Failed = true; Cancel(); return; } }
        }
        public override void Cancel() { foreach (var step in _children) if (step.HasStarted) step.Cancel(); }
    }
}
