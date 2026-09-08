using Bloodlines.Abilities;
using Bloodlines.Core;
using Bloodlines.Crew;

namespace Bloodlines.Missions
{
    /// <summary>Everything a mission script is allowed to reach for. Passed in, never global.</summary>
    public sealed class MissionContext
    {
        public MissionContext(ModConfig config, LocationBook locations, CrewRoster crew,
            SwitchController switching, AbilityController abilities)
        {
            Config = config;
            Locations = locations;
            Crew = crew;
            Switching = switching;
            Abilities = abilities;
        }

        public ModConfig Config { get; }
        public LocationBook Locations { get; }
        public CrewRoster Crew { get; }
        public SwitchController Switching { get; }
        public AbilityController Abilities { get; }
    }
}
