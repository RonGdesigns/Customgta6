using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Crew;

namespace Bloodlines.Missions.Objectives
{
    /// <summary>
    /// One stage of a composed mission: a name, some objectives, and the rules for
    /// finishing them.
    /// </summary>
    public sealed class MissionStage
    {
        public MissionStage(string name, params Objective[] objectives)
        {
            Name = name;
            Objectives = objectives.ToList();
        }

        public string Name { get; }

        public List<Objective> Objectives { get; }

        /// <summary>Bible cue stage played on entry (the S-number in the cue ids). 0 for none.</summary>
        public int DialogueStage { get; private set; }

        /// <summary>Character this whole stage belongs to; switching is locked to them.</summary>
        public CrewSlot? LockedTo { get; private set; }

        /// <summary>When false, the stage ends as soon as any one objective completes.</summary>
        public bool RequireAll { get; private set; } = true;

        public Action<MissionContext> Setup { get; private set; }

        public Action<MissionContext> Teardown { get; private set; }

        // --- fluent configuration, so a mission reads as a script ---

        public MissionStage WithDialogue(int stage)
        {
            DialogueStage = stage;
            return this;
        }

        public MissionStage PlayedBy(CrewSlot slot)
        {
            LockedTo = slot;
            foreach (var objective in Objectives) objective.RequiredCharacter = slot;
            return this;
        }

        /// <summary>Marks objectives as owned by a character without locking the switch.</summary>
        public MissionStage OwnedBy(CrewSlot slot)
        {
            foreach (var objective in Objectives) objective.RequiredCharacter = slot;
            return this;
        }

        public MissionStage AnyOf()
        {
            RequireAll = false;
            return this;
        }

        public MissionStage OnEnter(Action<MissionContext> setup)
        {
            Setup = setup;
            return this;
        }

        public MissionStage OnExit(Action<MissionContext> teardown)
        {
            Teardown = teardown;
            return this;
        }

        // --- state ---

        public bool IsComplete => RequireAll
            ? Objectives.All(objective => objective.Status == ObjectiveStatus.Complete)
            : Objectives.Any(objective => objective.Status == ObjectiveStatus.Complete);

        public Objective FirstFailure =>
            Objectives.FirstOrDefault(objective => objective.Status == ObjectiveStatus.Failed);

        /// <summary>The objective whose label should be on screen right now.</summary>
        public Objective Current =>
            Objectives.FirstOrDefault(objective => !objective.IsFinished && !string.IsNullOrEmpty(objective.Label));
    }
}
