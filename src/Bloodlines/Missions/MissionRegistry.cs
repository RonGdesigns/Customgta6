using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Missions.Campaign;

namespace Bloodlines.Missions
{
    public enum CampaignPart
    {
        BleedingTrail = 1,
        Squeeze = 2,
        ScorchedEarth = 3
    }

    public sealed class MissionDefinition
    {
        public MissionDefinition(int number, string title, string synopsis, Func<Mission> factory = null)
        {
            Number = number;
            Title = title;
            Synopsis = synopsis;
            Factory = factory;
        }

        public int Number { get; }
        public string Id => "M" + Number.ToString("00");
        public string Title { get; }
        public string Synopsis { get; }
        public Func<Mission> Factory { get; }

        public bool IsPlayable => Factory != null;

        public CampaignPart Part =>
            Number <= 22 ? CampaignPart.BleedingTrail :
            Number <= 48 ? CampaignPart.Squeeze :
            CampaignPart.ScorchedEarth;

        public static string PartTitle(CampaignPart part)
        {
            switch (part)
            {
                case CampaignPart.BleedingTrail: return "Part I — The Bleeding Trail";
                case CampaignPart.Squeeze: return "Part II — The Squeeze";
                default: return "Part III — Scorched Earth";
            }
        }
    }

    /// <summary>
    /// The 70-slot campaign spine from section 4 of the design bible.
    ///
    /// Every slot exists so ordering, pacing and progression are real from day one.
    /// The beats the bible actually specifies carry their titles and synopses; the
    /// slots it leaves open are marked as unwritten rather than invented here, and
    /// a slot only becomes playable when a Mission class is wired into its factory.
    /// </summary>
    public static class MissionRegistry
    {
        private static readonly Dictionary<int, MissionDefinition> Specified = new[]
        {
            new MissionDefinition(1, "Ghost in the Dockyard",
                "Terminal dry-docks, 02:00. Three unrelated contracts cross wires: Ice on the container crane, " +
                "Gohan in the yacht's lower decks after a cold-storage ledger, Guess breaching the warehouse bay " +
                "for a prototype car. A dropped radio callsign burns all three identities.",
                () => new M01GhostInTheDockyard()),

            new MissionDefinition(6, "Clean Sweep",
                "First dynamic tri-switch. Vespucci LSPD evidence depot: Gohan drops building power via the access " +
                "tunnels, Ice holds the alley against SWAT with heavy ordnance, Guess runs the armored van out on " +
                "the freight train's timing."),

            new MissionDefinition(10, "Open Throttle",
                "Combat escort gauntlet down Del Perro Freeway at 95+ mph. Ice repels gunships with an RPG, Gohan " +
                "kills hostile ECUs with a portable EMP, Guess pits the interceptors."),

            new MissionDefinition(19, "The Port of Los Santos Heist — Approach",
                "Phase 1 of the major score: Fort Zancudo Cargobob acquisition and insertion."),
            new MissionDefinition(20, "The Port of Los Santos Heist — Underwater Cut",
                "Phase 2: underwater container cutting under patrol lights."),
            new MissionDefinition(21, "The Port of Los Santos Heist — The Lift",
                "Phase 3: Cargobob lift with the container under active fire."),
            new MissionDefinition(22, "The Port of Los Santos Heist — Maritime Break",
                "Phase 4: high-speed maritime escape under Coast Guard fire."),

            new MissionDefinition(27, "Flight Risk",
                "Mid-air boarding gauntlet. Guess holds a stunt plane over a chartered Shamal at 8,000 feet; Ice " +
                "skydives onto the roof, torches the cabin hatch, clears the cartel crew in freefall and drops to " +
                "Gohan's speedboat in the Pacific."),

            new MissionDefinition(34, "Mud & Iron",
                "Senora Desert half-track escort under anti-materiel snipers on the wind turbines and low-flying " +
                "planes dropping incendiaries."),

            new MissionDefinition(44, "The Paleto Deep-Sea Incursion — Descent",
                "Phase 1: Kraken submersible approach on the Aegis offshore platform."),
            new MissionDefinition(45, "The Paleto Deep-Sea Incursion — Hull Breach",
                "Phase 2: thermite charges on the platform legs and the sub-deck breach."),
            new MissionDefinition(46, "The Paleto Deep-Sea Incursion — Deck Clear",
                "Phase 3: mini-gun Maverick support while the deck is cleared."),
            new MissionDefinition(47, "The Paleto Deep-Sea Incursion — The Bonds",
                "Phase 4: cartel bearer bonds out of the platform vault."),
            new MissionDefinition(48, "The Paleto Deep-Sea Incursion — Exfil",
                "Phase 5: exfil under Aegis rotary response."),

            new MissionDefinition(55, "Skyline Descent",
                "Triple penthouse raid: three simultaneous Downtown high-rise breaches inside a hard five-minute " +
                "timer to zero out cartel escrow."),

            new MissionDefinition(62, "Steel Horizon",
                "Train / sea / air convergence. Ice clears automated flatbed rail turrets, Gohan matches speed in " +
                "an ocean launch while jamming signals, Guess extracts the crew by Annihilator."),

            new MissionDefinition(68, "Blood Brothers — The Canal",
                "Climax stage 1: the Los Santos River canal running battle."),
            new MissionDefinition(69, "Blood Brothers — Maze Bank",
                "Climax stage 2: the Maze Bank Tower ascent."),
            new MissionDefinition(70, "Blood Brothers — Runway 30L",
                "Climax stage 3: an 18-wheeler gun battle down Runway 30L at LSIA, ending in a three-man holdout " +
                "inside a grounded cargo plane.")
        }.ToDictionary(m => m.Number);

        public static readonly IReadOnlyList<MissionDefinition> All = Enumerable.Range(1, 70)
            .Select(number => Specified.TryGetValue(number, out var defined)
                ? defined
                : new MissionDefinition(number, "Unwritten slot " + number.ToString("00"),
                    "Reserved by the campaign architecture; no beat specified in the design bible yet."))
            .ToList();

        public static MissionDefinition Get(int number)
        {
            return number >= 1 && number <= 70 ? All[number - 1] : null;
        }

        public static MissionDefinition Get(string id)
        {
            return All.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        public static IEnumerable<MissionDefinition> Playable => All.Where(m => m.IsPlayable);
    }
}
