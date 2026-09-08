using Bloodlines.Core;

namespace Bloodlines.Missions.Objectives
{
    public enum ObjectiveStatus
    {
        Active,
        Complete,
        Failed
    }

    /// <summary>
    /// One thing the player has to do. Objectives are the reusable half of a mission:
    /// reach a place, kill a set, hold a position, survive waves, keep something alive.
    ///
    /// The point is that a mission stops being 400 lines of bespoke C# and becomes a
    /// list of these. Bespoke code is still available for the set pieces that earn it,
    /// but the routine 80% of every mission should be composition.
    /// </summary>
    public abstract class Objective
    {
        protected Objective(string label)
        {
            Label = label;
        }

        /// <summary>HUD line shown while this objective is active.</summary>
        public string Label { get; protected set; }

        public ObjectiveStatus Status { get; private set; } = ObjectiveStatus.Active;

        public string FailReason { get; private set; }

        /// <summary>Set by a stage when this objective only counts for one character.</summary>
        public Crew.CrewSlot? RequiredCharacter { get; set; }

        public bool IsFinished => Status != ObjectiveStatus.Active;

        /// <summary>
        /// A passive objective can only fail, never complete — keeping something alive,
        /// not being seen, a countdown running underneath the work. Stages ignore them
        /// when deciding whether they are finished, because a stage that waits for
        /// "did not lose the car yet" to turn green waits forever.
        /// </summary>
        public virtual bool IsPassive => false;

        public virtual void Enter(MissionContext context)
        {
        }

        public abstract void Update(MissionContext context);

        public virtual void Exit(MissionContext context)
        {
        }

        protected void Complete()
        {
            if (Status == ObjectiveStatus.Active) Status = ObjectiveStatus.Complete;
        }

        protected void Fail(string reason)
        {
            if (Status != ObjectiveStatus.Active) return;
            Status = ObjectiveStatus.Failed;
            FailReason = reason;
            Logger.Debug("Objective failed: " + Label + " — " + reason);
        }

        /// <summary>
        /// True when the active character is the one this objective belongs to. An
        /// objective owned by a character simply does not progress for the others,
        /// which is how a mission teaches switching without telling the player to switch.
        /// </summary>
        protected bool IsOwnerActive(MissionContext context)
        {
            return RequiredCharacter == null || context.Crew.ActiveSlot == RequiredCharacter.Value;
        }
    }
}
