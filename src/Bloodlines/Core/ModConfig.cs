using System;
using System.Collections.Generic;
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
        // Tenkeyless keyboards have no numpad at all, which is why the cycle keys
        // below exist as well: every board has [ and ].
        public Keys SwitchIceKey { get; private set; } = Keys.NumPad1;
        public Keys SwitchGohanKey { get; private set; } = Keys.NumPad2;
        public Keys SwitchGuessKey { get; private set; } = Keys.NumPad3;
        public Keys SwitchNextKey { get; private set; } = Keys.OemCloseBrackets;
        public Keys SwitchPrevKey { get; private set; } = Keys.OemOpenBrackets;
        public Keys AbilityKey { get; private set; } = Keys.Capital;
        public Keys MissionStartKey { get; private set; } = Keys.J;
        public Keys AbortKey { get; private set; } = Keys.Back;
        public Keys DevCaptureKey { get; private set; } = Keys.F11;
        public Keys DeployCrewKey { get; private set; } = Keys.F10;
        public Keys DevMenuKey { get; private set; } = Keys.F8;
        public Keys SurveyTeleportKey { get; private set; } = Keys.F7;

        /// <summary>Companions are damage-capped rather than invincible; 0 disables the cap.</summary>
        public int CompanionHealthFloor { get; private set; } = 150;

        public bool CompanionsRespawnOnDeath { get; private set; } = true;

        /// <summary>
        /// Metres a companion may fall behind before it is repositioned on the active
        /// character. The toolkit calls this the companion leash.
        /// </summary>
        public float CompanionLeashDistance { get; private set; } = 180f;
        /// <summary>
        /// Route the game's own character-select controls to the crew, so a
        /// controller switches without any binding: they are already on the
        /// character wheel. Michael/Franklin/Trevor map to Ice/Gohan/Guess.
        /// </summary>
        public bool ControllerSwitchEnabled { get; private set; } = true;

        /// <summary>
        /// Take the vanilla character wheel's controls over while the crew is
        /// deployed. Left alone, the wheel would swap the player to Michael,
        /// Franklin or Trevor and strand every ped this mod is tracking.
        /// </summary>
        public bool SuppressVanillaSwitch { get; private set; } = true;

        /// <summary>
        /// Handle the player's death instead of the engine, for as long as the crew is
        /// deployed. Turning this off restores the vanilla path, which on a ped
        /// installed by CHANGE_PLAYER_PED fades out and never comes back; the switch
        /// exists so a bad interaction with another mod can be isolated, not because
        /// off is a reasonable way to play.
        /// </summary>
        public bool DeathHandlingEnabled { get; private set; } = true;

        /// <summary>Come back at the running mission's last checkpoint rather than the deployment point.</summary>
        public bool RestoreCheckpointOnDeath { get; private set; } = true;

        public int DeathFadeOutMs { get; private set; } = 800;

        /// <summary>How long the screen stays black — the beat that reads as a death rather than a stutter.</summary>
        public int DeathHoldMs { get; private set; } = 1500;

        public int DeathFadeInMs { get; private set; } = 1200;

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

            config.SwitchIceKey = ReadKey(settings, "SwitchIce", config.SwitchIceKey);
            config.SwitchGohanKey = ReadKey(settings, "SwitchGohan", config.SwitchGohanKey);
            config.SwitchGuessKey = ReadKey(settings, "SwitchGuess", config.SwitchGuessKey);
            config.SwitchNextKey = ReadKey(settings, "SwitchNext", config.SwitchNextKey);
            config.SwitchPrevKey = ReadKey(settings, "SwitchPrev", config.SwitchPrevKey);
            config.AbilityKey = ReadKey(settings, "Ability", config.AbilityKey);
            config.MissionStartKey = ReadKey(settings, "MissionStart", config.MissionStartKey);
            config.AbortKey = ReadKey(settings, "AbortMission", config.AbortKey);
            config.DevCaptureKey = ReadKey(settings, "DevCapture", config.DevCaptureKey);
            config.DeployCrewKey = ReadKey(settings, "DeployCrew", config.DeployCrewKey);
            config.DevMenuKey = ReadKey(settings, "DevMenu", config.DevMenuKey);
            config.SurveyTeleportKey = ReadKey(settings, "SurveyTeleport", config.SurveyTeleportKey);
            config.ControllerSwitchEnabled = settings.GetValue<bool>("Keys", "ControllerSwitch", config.ControllerSwitchEnabled);
            config.SuppressVanillaSwitch = settings.GetValue<bool>("Keys", "SuppressVanillaSwitch", config.SuppressVanillaSwitch);

            config.CompanionHealthFloor = settings.GetValue<int>("Crew", "CompanionHealthFloor", config.CompanionHealthFloor);
            config.CompanionsRespawnOnDeath = settings.GetValue<bool>("Crew", "RespawnOnDeath", config.CompanionsRespawnOnDeath);
            config.CompanionLeashDistance = settings.GetValue<float>("Crew", "CompanionLeashDistance", config.CompanionLeashDistance);

            config.DeathHandlingEnabled = settings.GetValue<bool>("Death", "Enabled", config.DeathHandlingEnabled);
            config.RestoreCheckpointOnDeath = settings.GetValue<bool>("Death", "RestoreCheckpoint", config.RestoreCheckpointOnDeath);
            config.DeathFadeOutMs = settings.GetValue<int>("Death", "FadeOutMs", config.DeathFadeOutMs);
            config.DeathFadeOutMs = Math.Max(0, Math.Min(3000, config.DeathFadeOutMs));
            config.DeathHoldMs = settings.GetValue<int>("Death", "HoldMs", config.DeathHoldMs);
            config.DeathHoldMs = Math.Max(0, Math.Min(5000, config.DeathHoldMs));
            config.DeathFadeInMs = settings.GetValue<int>("Death", "FadeInMs", config.DeathFadeInMs);
            config.DeathFadeInMs = Math.Max(0, Math.Min(3000, config.DeathFadeInMs));

            config.AbilitiesEnabled = settings.GetValue<bool>("Abilities", "Enabled", config.AbilitiesEnabled);
            config.AbilityDuration = settings.GetValue<float>("Abilities", "DurationSeconds", config.AbilityDuration);
            config.AbilityRechargeRate = settings.GetValue<float>("Abilities", "RechargePerSecond", config.AbilityRechargeRate);

            config.DevToolsEnabled = settings.GetValue<bool>("Dev", "Enabled", config.DevToolsEnabled);
            config.VerboseLogging = settings.GetValue<bool>("Dev", "VerboseLogging", config.VerboseLogging);

            // Writes back any key the ini was missing, so the file self-documents after first run.
            settings.Save();
            return config;
        }

        /// <summary>
        /// Aliases for keys nobody spells the way the <see cref="Keys"/> enum does.
        /// Someone rebinding to the bracket keys writes "[", not "OemOpenBrackets".
        /// </summary>
        private static readonly Dictionary<string, Keys> KeyAliases =
            new Dictionary<string, Keys>(StringComparer.OrdinalIgnoreCase)
            {
                { "[", Keys.OemOpenBrackets }, { "]", Keys.OemCloseBrackets },
                { "\\", Keys.OemPipe }, { ";", Keys.OemSemicolon }, { "'", Keys.OemQuotes },
                { ",", Keys.Oemcomma }, { ".", Keys.OemPeriod }, { "/", Keys.OemQuestion },
                { "-", Keys.OemMinus }, { "=", Keys.Oemplus }, { "`", Keys.Oemtilde },
                { "0", Keys.D0 }, { "1", Keys.D1 }, { "2", Keys.D2 }, { "3", Keys.D3 },
                { "4", Keys.D4 }, { "5", Keys.D5 }, { "6", Keys.D6 }, { "7", Keys.D7 },
                { "8", Keys.D8 }, { "9", Keys.D9 },
                { "CapsLock", Keys.Capital }, { "Backspace", Keys.Back },
                { "Esc", Keys.Escape }, { "Ctrl", Keys.ControlKey },
                { "PageUp", Keys.PageUp }, { "PageDown", Keys.Next }, { "PgUp", Keys.PageUp },
                { "PgDn", Keys.Next }, { "Ins", Keys.Insert }, { "Del", Keys.Delete },
                { "Num1", Keys.NumPad1 }, { "Num2", Keys.NumPad2 }, { "Num3", Keys.NumPad3 },
            };

        /// <summary>
        /// Parses a key name ourselves rather than through the type converter, which
        /// only recognises its own localised display names -- "D1" and
        /// "OemOpenBrackets" are perfectly good <see cref="Keys"/> values that it
        /// rejects. A name we cannot parse is logged and the default kept, because a
        /// typo in an ini should cost one binding, not the whole mod.
        /// </summary>
        private static Keys ReadKey(ScriptSettings settings, string name, Keys fallback)
        {
            var raw = settings.GetValue<string>("Keys", name, null);
            if (string.IsNullOrWhiteSpace(raw)) return fallback;

            raw = raw.Trim();
            Keys alias;
            if (KeyAliases.TryGetValue(raw, out alias)) return alias;

            Keys parsed;
            if (Enum.TryParse(raw, true, out parsed) && Enum.IsDefined(typeof(Keys), parsed))
                return parsed;

            Logger.Warn("Bloodlines.ini: [Keys] " + name + " = \"" + raw +
                        "\" is not a key name; keeping " + fallback + ".");
            return fallback;
        }
    }
}
