using Bloodlines.Abilities;
using Bloodlines.Core;
using Bloodlines.Crew;

namespace Bloodlines.Missions
{
    /// <summary>Everything a mission script is allowed to reach for. Passed in, never global.</summary>
    public sealed class MissionContext
    {
        public MissionContext(ModConfig config, LocationBook locations, CampaignData data,
            CrewRoster crew, SwitchController switching, AbilityController abilities,
            DialogueDirector dialogue, CheckpointManager checkpoints)
        {
            Config = config;
            Locations = locations;
            Data = data;
            Crew = crew;
            Switching = switching;
            Abilities = abilities;
            Dialogue = dialogue;
            Checkpoints = checkpoints;
        }

        public ModConfig Config { get; }
        public LocationBook Locations { get; }
        public CampaignData Data { get; }
        public CrewRoster Crew { get; }
        public SwitchController Switching { get; }
        public AbilityController Abilities { get; }
        public DialogueDirector Dialogue { get; }
        public CheckpointManager Checkpoints { get; }
    }
}
