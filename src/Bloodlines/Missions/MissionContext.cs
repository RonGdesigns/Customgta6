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
            DialogueDirector dialogue, CheckpointManager checkpoints, CampaignState state)
        {
            Config = config;
            Locations = locations;
            Data = data;
            Crew = crew;
            Switching = switching;
            Abilities = abilities;
            Dialogue = dialogue;
            Checkpoints = checkpoints;
            State = state;
        }

        public ApartmentAccess Interior { get; set; }
        public CutsceneDirector Cutscenes { get; set; }
        public PortHeistWorld PortHeist { get; set; }

        /// <summary>
        /// Shared session-only state for a generic one-sitting operation. Current
        /// Port Heist code keeps its specialized PortHeistWorld until it is migrated;
        /// new operation phases use this property instead of a global singleton.
        /// </summary>
        public ContinuousOperationState ActiveOperation { get; set; }

        /// <summary>Structured QA facts for placement/startup/runtime inspection. Never saved.</summary>
        public MissionDoctor Doctor { get; } = new MissionDoctor();

        /// <summary>Chapter-to-chapter state for continuous operations (Port Heist first).</summary>
        public HandoffLedger Handoffs { get; } = new HandoffLedger();
        /// <summary>The crew's own Granger; null in hosts without one (missions then spawn a stock Granger).</summary>
        public CrewVan Vans { get; set; }
        public GarageService Garages { get; set; }
        public GTA.Vehicle RaceVehicle { get; set; }
        public ModConfig Config { get; }
        public LocationBook Locations { get; }
        public CampaignData Data { get; }
        public CrewRoster Crew { get; }
        public SwitchController Switching { get; }
        public AbilityController Abilities { get; }
        public DialogueDirector Dialogue { get; }
        public CheckpointManager Checkpoints { get; }

        /// <summary>Campaign save state: progress, economy, safehouses, fleet upgrades.</summary>
        public CampaignState State { get; }
    }
}
