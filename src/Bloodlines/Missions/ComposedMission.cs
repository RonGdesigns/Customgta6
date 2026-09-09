using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Missions.Objectives;

namespace Bloodlines.Missions
{
    /// <summary>
    /// A mission written as a list of stages of objectives rather than as a bespoke
    /// state machine.
    ///
    /// The bespoke form (see M01) is still there for set pieces that genuinely need
    /// it, but most missions are the same handful of verbs in a different order, and
    /// writing those by hand 76 more times is how a campaign this size never ships.
    /// A composed mission implements two methods: what to spawn, and the stage list.
    ///
    /// Everything else — objective ticking, the on-screen objective line, stage
    /// advance, checkpoints, dialogue per stage, switch locking, failure with a
    /// reason, teardown — is handled here once.
    /// </summary>
    public abstract class ComposedMission : Mission
    {
        private List<MissionStage> _stages;
        private readonly HashSet<Crew.CrewSlot> _stationed = new HashSet<Crew.CrewSlot>();
        protected bool RequireAssets(params GTA.Entity[] entities)
        {
            if (entities.All(e => e != null && e.Exists())) return true;
            Logger.Error(Id + ": a required actor or vehicle failed to spawn.");
            GameUtils.Notify("Mission assets could not load. Restart this mission.");
            return false;
        }
        protected void Station(Crew.CrewSlot slot, GTA.Math.Vector3 position)
        {
            var ped = Ctx.Crew.PedFor(slot);
            if (ped == null || !ped.Exists()) return;
            Ctx.Crew.CompanionAI.TakeControl(slot);
            ped.Task.ClearAllImmediately(); ped.Position = position;
            ped.Task.GuardCurrentPosition(); _stationed.Add(slot);
        }
        protected void Station(Crew.CrewSlot slot, GTA.Vehicle vehicle, GTA.VehicleSeat seat)
        {
            var ped = Ctx.Crew.PedFor(slot);
            if (ped == null || !ped.Exists() || vehicle == null || !vehicle.Exists()) return;
            Ctx.Crew.CompanionAI.TakeControl(slot);
            ped.Task.ClearAllImmediately(); ped.SetIntoVehicle(vehicle, seat); _stationed.Add(slot);
        }
        private readonly Dictionary<Crew.CrewSlot, int> _assignmentTasks = new Dictionary<Crew.CrewSlot, int>();
        private void MaintainAssignments(MissionStage stage)
        {
            foreach (var objective in stage.Objectives)
            {
                if (objective.IsFinished || !objective.RequiredCharacter.HasValue || !objective.AssignmentPosition.HasValue) continue;
                var slot = objective.RequiredCharacter.Value;
                if (Ctx.Crew.ActiveSlot == slot) { if (_assignmentTasks.ContainsKey(slot)) _assignmentTasks[slot] = 0; continue; }
                var ped = Ctx.Crew.PedFor(slot);
                if (ped == null || !ped.Exists() || ped.IsDead) continue;
                // Explicit set-piece scripts retain ownership of their actors.
                if (!_assignmentTasks.ContainsKey(slot) && Ctx.Crew.CompanionAI.StateOf(slot) == Crew.CompanionState.Scripted) continue;
                if (_assignmentTasks.TryGetValue(slot, out var next) && GTA.Game.GameTime < next) continue;
                Ctx.Crew.CompanionAI.TakeControl(slot);
                var target = objective.AssignmentPosition.Value;
                if (ped.IsInVehicle()) ped.Task.LeaveVehicle();
                else if (ped.Position.DistanceTo(target) > 3f) ped.Task.GoTo(target);
                else ped.Task.GuardCurrentPosition();
                _assignmentTasks[slot] = GTA.Game.GameTime + 5000;
            }
        }

        protected IReadOnlyList<MissionStage> Stages => _stages;

        /// <summary>Spawn the world. Return false to reject the start.</summary>
        protected abstract bool Setup();

        /// <summary>The mission, as stages of objectives.</summary>
        protected abstract IEnumerable<MissionStage> BuildStages();

        protected override bool OnStart()
        {
            if (!Setup()) return false;

            _stages = PrepareStages();
            if (_stages.Count == 0)
            {
                Logger.Error(Id + " built no stages.");
                return false;
            }

            if (!Validate()) return false;

            EnterStage(0);
            return true;
        }

        protected override void OnUpdate()
        {
            var stage = CurrentStageOrNull();
            if (stage == null) return;

            // A player-controlled station becomes normal crew AI, including driver handover.
            if (_stationed.Remove(Ctx.Crew.ActiveSlot)) Ctx.Crew.CompanionAI.ReleaseControl(Ctx.Crew.ActiveSlot);
            MaintainAssignments(stage);
            foreach (var objective in stage.Objectives)
            {
                if (!objective.IsFinished) objective.Update(Ctx);
            }

            var failure = stage.FirstFailure;
            if (failure != null)
            {
                Fail(failure.FailReason ?? "Objective failed.");
                return;
            }

            var current = stage.Current;
            if (current != null) { CurrentObjective = current.RequiredCharacter.HasValue && current.RequiredCharacter.Value != Ctx.Crew.ActiveSlot
                    ? "Switch to " + Crew.Protagonist.Of(current.RequiredCharacter.Value).Handle + " — " + current.Label : current.Label; GameUtils.Subtitle("~y~" + current.Label, 500); }

            if (!stage.IsComplete) return;

            ExitStage(stage);

            if (Stage + 1 >= _stages.Count)
            {
                Pass();
                return;
            }

            Advance();
            EnterStage(Stage);
        }

        /// <summary>
        /// Catches the stage that can never finish: one made only of passive
        /// objectives, which can fail but never complete. That shape is invisible to
        /// the compiler and only shows up as a mission hanging at a stage, so it is
        /// worth failing loudly at start instead.
        /// </summary>
        private List<MissionStage> PrepareStages()
        {
            var stages = BuildStages().ToList();
            // The first six missions have bespoke role handoffs. Later composed jobs
            // retain their last declared owner until the script explicitly hands off.
            if (Id.StartsWith("SM") || (int.TryParse(Id.Substring(1), out var number) && number >= 7))
            {
                Crew.CrewSlot owner = Ctx.Crew.ActiveSlot;
                foreach (var stage in stages)
                {
                    var assigned = stage.Objectives.FirstOrDefault(o => !o.IsPassive && o.RequiredCharacter.HasValue);
                    if (assigned != null) owner = assigned.RequiredCharacter.Value;
                    foreach (var objective in stage.Objectives)
                        if (!objective.RequiredCharacter.HasValue && !objective.IsPassive) objective.RequiredCharacter = owner;
                }
                stages.Add(new MissionStage("Radio debrief", new DialogueFinishedObjective("Listen to the crew's final radio call.")));
            }
            return stages;
        }

        private bool Validate()
        {
            bool valid = true;

            for (int i = 0; i < _stages.Count; i++)
            {
                var stage = _stages[i];

                if (stage.Objectives.Count == 0)
                {
                    Logger.Error(Id + " stage " + i + " (" + stage.Name + ") has no objectives.");
                    valid = false;
                    continue;
                }

                if (stage.Objectives.All(objective => objective.IsPassive))
                {
                    Logger.Error(Id + " stage " + i + " (" + stage.Name +
                                 ") has only passive objectives and could never finish.");
                    valid = false;
                }
            }

            return valid;
        }

        private MissionStage CurrentStageOrNull()
        {
            return _stages != null && Stage >= 0 && Stage < _stages.Count ? _stages[Stage] : null;
        }

        private void EnterStage(int index)
        {
            if (index < 0 || index >= _stages.Count) return;

            var stage = _stages[index];
            Logger.Debug(Id + " stage " + index + ": " + stage.Name);
            foreach (var slot in _assignmentTasks.Keys) Ctx.Crew.CompanionAI.ReleaseControl(slot);
            _assignmentTasks.Clear();

            if (stage.LockedTo.HasValue)
            {
                var protagonist = Crew.Protagonist.Of(stage.LockedTo.Value);
                Ctx.Switching.SetUnlocked();
                if (Ctx.Crew.ActiveSlot != stage.LockedTo.Value && !Ctx.Switching.TrySwitch(stage.LockedTo.Value, missionTransition: true))
                    throw new System.InvalidOperationException("Could not activate " + protagonist.Handle + " for this stage.");
                Ctx.Switching.SetLocked(protagonist.Handle + " has this one.");
            }
            else
            {
                Ctx.Switching.SetUnlocked();
            }

            stage.Setup?.Invoke(Ctx);

            foreach (var objective in stage.Objectives) objective.Enter(Ctx);

            if (stage.DialogueStage > 0) SayStage(stage.DialogueStage);
            foreach (var cue in stage.EntryCues) Say(cue);

            var first = stage.Current;
            if (first != null) Objective(first.Label);
            MaintainAssignments(stage);
        }

        private void ExitStage(MissionStage stage)
        {
            foreach (var objective in stage.Objectives) objective.Exit(Ctx);
            stage.Teardown?.Invoke(Ctx);
            foreach (var cue in stage.ExitCues) Say(cue);
        }

        /// <summary>A checkpoint restore or QA warp re-enters the stage cleanly.</summary>
        protected override void OnStageEntered(int stage)
        {
            if (_stages == null) return;
            _stages = PrepareStages();
            if (!Validate()) throw new System.InvalidOperationException("Invalid rebuilt stages for " + Id);
            EnterStage(stage);
        }
    }
}
