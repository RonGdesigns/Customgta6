using System;
using System.Drawing;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Objectives
{
    /// <summary>An explicitly assigned job that continues when its owner is not the player.</summary>
    public sealed class AssignedWorkObjective : Objective
    {
        private readonly CrewSlot _worker;
        private readonly Func<Vector3> _target;
        private readonly int _duration;
        private int _lastTick, _worked, _nextTask;
        private bool _wasWorking, _wasPlayer;
        public AssignedWorkObjective(string label, CrewSlot worker, Func<Vector3> target, int seconds) : base(label)
        { _worker = worker; _target = target; _duration = seconds * 1000; }
        public override void Enter(MissionContext context)
        {
            base.Enter(context);
            _lastTick = Game.GameTime; _worked = 0; _nextTask = 0; _wasWorking = false;
            context.Crew.CompanionAI.TakeControl(_worker);
        }
        public override void Update(MissionContext context)
        {
            var ped = context.Crew.PedFor(_worker);
            int delta = Math.Max(0, Math.Min(1000, Game.GameTime - _lastTick)); _lastTick = Game.GameTime;
            if (ped == null || !ped.Exists() || ped.IsDead) { Fail(Protagonist.Of(_worker).Handle + " cannot finish the assigned job."); return; }
            bool player = context.Crew.ActiveSlot == _worker;
            var target = _target();
            bool working = !ped.IsInVehicle() && ped.Position.DistanceTo(target) <= 3f;
            GameUtils.DrawObjectiveMarker(target, Color.Cyan, 2f);
            if (player) ObjectiveMarkers.Navigation(target, _worker);
            if (!player && (Game.GameTime >= _nextTask || player != _wasPlayer || working != _wasWorking))
            {
                if (ped.IsInVehicle()) ped.Task.LeaveVehicle();
                else if (working) ped.Task.StartScenario("WORLD_HUMAN_WELDING", ped.Position, ped.Heading);
                else ped.Task.GoTo(target);
                _nextTask = Game.GameTime + 10000;
            }
            if (working && _wasWorking) _worked += delta;
            _wasWorking = working; _wasPlayer = player;
            if (_worked >= _duration) Complete();
        }
        public override void Exit(MissionContext context)
        {
            var ped = context.Crew.PedFor(_worker);
            if (ped != null && ped.Exists() && context.Crew.ActiveSlot != _worker) ped.Task.ClearAll();
            // The mission retains this actor until its extraction stage releases the crew.
        }
    }
}
