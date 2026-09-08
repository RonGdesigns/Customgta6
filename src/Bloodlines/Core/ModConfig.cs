using System.Windows.Forms;
using GTA;

namespace Bloodlines.Core
{
    /// <summary>
    /// Reads Bloodlines.ini next to the script. Every key has a working default,
    /// so a missing or half-written ini never stops the mod from loading.
    /// </summary>
    public sealed class ModConfig
    {
        // Defaults follow the implementation toolkit's [Keybinds] block. NumPad also
        // keeps the switch off 1/2/3, which the game already uses for weapon slots.
        public Keys SwitchIceKey { get; private set; } = Keys.NumPad1;
        public Keys SwitchGohanKey { get; private set; } = Keys.NumPad2;
        public Keys SwitchGuessKey { get; private set; } = Keys.NumPad3;
        public Keys AbilityKey { get; private set; } = Keys.Capital;
        public Keys MissionStartKey { get; private set; } = Keys.J;
        public Keys AbortKey { get; private set; } = Keys.Back;
        public Keys DevCaptureKey { get; private set; } = Keys.F11;
        public Keys DeployCrewKey { get; private set; } = Keys.F10;
        public Keys DevMenuKey { get; private set; } = Keys.F8;

        /// <summary>Companions are damage-capped rather than invincible; 0 disables the cap.</summary>
        public int CompanionHealthFloor { get; private set; } = 150;

        public bool CompanionsRespawnOnDeath { get; private set; } = true;

        /// <summary>
        /// Metres a companion may fall behind before it is repositioned on the active
        /// character. The toolkit calls this the companion leash.
        /// </summary>
        public float CompanionLeashDistance { get; private set; } = 180f;
        public bool AbilitiesEnabled { get; private set; } = true;
        public bool DevToolsEnabled { get; private set; } = false;
        public bool VerboseLogging { get; private set; } = false;

        /// <summary>Seconds of ability time; the meter refills at <see cref="AbilityRechargeRate"/> per second.</summary>
        public float AbilityDuration { get; private set; } = 8f;

        public float AbilityRechargeRate { get; private set; } = 0.25f;

        public static ModConfig Load(string path)
        {
            var config = new ModConfig();
            var settings = ScriptSettings.Load(path);

            config.SwitchIceKey = settings.GetValue<Keys>("Keys", "SwitchIce", config.SwitchIceKey);
            config.SwitchGohanKey = settings.GetValue<Keys>("Keys", "SwitchGohan", config.SwitchGohanKey);
            config.SwitchGuessKey = settings.GetValue<Keys>("Keys", "SwitchGuess", config.SwitchGuessKey);
            config.AbilityKey = settings.GetValue<Keys>("Keys", "Ability", config.AbilityKey);
            config.MissionStartKey = settings.GetValue<Keys>("Keys", "MissionStart", config.MissionStartKey);
            config.AbortKey = settings.GetValue<Keys>("Keys", "AbortMission", config.AbortKey);
            config.DevCaptureKey = settings.GetValue<Keys>("Keys", "DevCapture", config.DevCaptureKey);
            config.DeployCrewKey = settings.GetValue<Keys>("Keys", "DeployCrew", config.DeployCrewKey);
            config.DevMenuKey = settings.GetValue<Keys>("Keys", "DevMenu", config.DevMenuKey);

            config.CompanionHealthFloor = settings.GetValue<int>("Crew", "CompanionHealthFloor", config.CompanionHealthFloor);
            config.CompanionsRespawnOnDeath = settings.GetValue<bool>("Crew", "RespawnOnDeath", config.CompanionsRespawnOnDeath);
            config.CompanionLeashDistance = settings.GetValue<float>("Crew", "CompanionLeashDistance", config.CompanionLeashDistance);

            config.AbilitiesEnabled = settings.GetValue<bool>("Abilities", "Enabled", config.AbilitiesEnabled);
            config.AbilityDuration = settings.GetValue<float>("Abilities", "DurationSeconds", config.AbilityDuration);
            config.AbilityRechargeRate = settings.GetValue<float>("Abilities", "RechargePerSecond", config.AbilityRechargeRate);

            config.DevToolsEnabled = settings.GetValue<bool>("Dev", "Enabled", config.DevToolsEnabled);
            config.VerboseLogging = settings.GetValue<bool>("Dev", "VerboseLogging", config.VerboseLogging);

            // Writes back any key the ini was missing, so the file self-documents after first run.
            settings.Save();
            return config;
        }
    }
}
