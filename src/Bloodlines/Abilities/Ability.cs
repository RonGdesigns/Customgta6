using Bloodlines.Crew;
using GTA;

namespace Bloodlines.Abilities
{
    /// <summary>
    /// One protagonist ability. Activate/Update/Deactivate are called by
    /// <see cref="AbilityController"/>; an ability never owns its own timer or
    /// keybind so that all three drain the same meter on the same rules.
    /// </summary>
    public abstract class Ability
    {
        public abstract CrewSlot Slot { get; }
        public abstract string Name { get; }

        /// <summary>Applied once when the ability comes up.</summary>
        public abstract void Activate(Ped player);

        /// <summary>Applied every frame the ability is up (per-frame natives live here).</summary>
        public abstract void Update(Ped player);

        /// <summary>Must undo everything Activate touched, including on mission abort.</summary>
        public abstract void Deactivate(Ped player);

        /// <summary>
        /// Called every frame after Deactivate until it returns false. For an ability
        /// that must blend its effect out rather than drop it. Default: nothing to do.
        /// </summary>
        public virtual bool Settle(Ped player) => false;
    }
}
