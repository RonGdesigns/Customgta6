using GTA;

namespace Bloodlines.Crew
{
    public enum CrewSlot
    {
        Ice = 0,
        Gohan = 1,
        Guess = 2
    }

    /// <summary>
    /// Static definition of one of the three leads. Model names come straight from
    /// the design bible; they are stock GTA V gang peds so the mod runs on an
    /// unmodified game, and swapping in a custom rigged ped is a one-line change here.
    /// </summary>
    public sealed class Protagonist
    {
        public const CrewSlot StartingSlot = CrewSlot.Guess;
        public CrewSlot Slot { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string Handle { get; }
        public string Role { get; }
        public string ModelName { get; }
        public string AbilityName { get; }
        public BlipColor BlipColor { get; }
        public WeaponHash[] Loadout { get; }

        private Protagonist(CrewSlot slot, string firstName, string lastName, string handle, string role,
            string modelName, string abilityName, BlipColor blipColor, WeaponHash[] loadout)
        {
            Slot = slot;
            FirstName = firstName;
            LastName = lastName;
            Handle = handle;
            Role = role;
            ModelName = modelName;
            AbilityName = abilityName;
            BlipColor = blipColor;
            Loadout = loadout;
        }

        /// <summary>Gameplay uses nicknames. FullName is reserved for deliberate story references.</summary>
        public string DisplayName => Handle;

        public string FullName => FirstName + " " + LastName;

        public Model Model => new Model(ModelName);

        public static readonly Protagonist Ice = new Protagonist(
            CrewSlot.Ice, "Darius", "Vance", "Ice", "The Tactician — Heavy Assault",
            "mp_m_freemode_01", "Overwatch Focus", BlipColor.Blue,
            new[] { WeaponHash.AssaultRifle, WeaponHash.RPG, WeaponHash.Pistol50, WeaponHash.StickyBomb });

        public static readonly Protagonist Gohan = new Protagonist(
            CrewSlot.Gohan, "Devin", "Mercer", "Gohan", "The Inside Man — Breaker",
            "mp_m_freemode_01", "Thermal Pulse", BlipColor.Green,
            new[] { WeaponHash.BullpupRifle, WeaponHash.SMG, WeaponHash.APPistol, WeaponHash.Flashlight, WeaponHash.SmokeGrenade });

        public static readonly Protagonist Guess = new Protagonist(
            CrewSlot.Guess, "Ron", "Ortiz", "Guess", "The Wheelman — Hotfoot",
            "mp_m_freemode_01", "Slipstream Reflex", BlipColor.Orange,
            new[] { (WeaponHash)unchecked((uint)Game.GenerateHash("WEAPON_TACTICALRIFLE")), WeaponHash.MicroSMG, WeaponHash.Pistol, WeaponHash.SawnOffShotgun });

        public static readonly Protagonist[] All = { Ice, Gohan, Guess };

        public static Protagonist Of(CrewSlot slot) => All[(int)slot];
    }
}
