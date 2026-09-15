using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// M55 — "Skyline Descent". Three downtown penthouses at once, 22:00, fog and thunder.
    ///
    /// Five minutes, three towers, one brother in each. The player moves between them as he
    /// likes and the clock does not stop for him.
    ///
    /// This was the mission the campaign could not build, and the reason was wrong. The note
    /// said every MP apartment interior resolves to one location, so three simultaneous
    /// penthouses were impossible. That is true of the **MP apartment tiers the mod's home
    /// system uses** and false of the game's own high-end apartments, which sit at five
    /// different real downtown buildings:
    ///
    ///   v_apartment_high  (-13.08, -593.62,  93.03)  Pillbox Hill, north
    ///   v_apartment_high  (-32.17, -579.02,  82.91)
    ///   v_apartment_high  (-260.88, -953.56, 70.02)
    ///   v_apartment_high  (-282.30, -954.78, 85.30)  Pillbox Hill, south
    ///   v_apartment_high  (-460.61, -691.56, 69.88)  Little Seoul, west
    ///
    /// and — this is the part that makes the mission work — they are **stock base map**, in
    /// <c>hw1_blimp_interior_*</c> ymaps with no DLC prefix. Nothing has to be requested, no
    /// IPL has to be juggled, and no interior has to be swapped when the player changes
    /// brother. Three of them are simply already there, six hundred and four hundred meters
    /// apart, and the existing switch machinery carries him between them. The mission is
    /// simultaneous because the map is.
    ///
    /// It still asks the engine first. <see cref="MissionSites.InteriorAt"/> checks all three
    /// before anybody is placed: a missing MLO would drop a man into open sky ninety meters
    /// up, and that is worth one native call to avoid.
    ///
    /// **Nothing inside is authored but the three placements.** Nobody has walked these. The
    /// guards, the terminal and the vault are offsets from where each brother actually stands,
    /// resolved to walkable floor — the same rule as the Maze Bank floors.
    ///
    /// One authored line does not fire. `M55_S1_04_ICE` calls a parachute descent "to the
    /// canal"; the nearest canal is eight hundred meters from a ninety-meter roof, which is not
    /// a glide. They leave by the regroup instead and the divergence is in
    /// `data/mission_gameplay.tsv`.
    /// </summary>
    public sealed class M55SkylineDescent : PreparationOperation
    {
        /// <summary>The window, exactly as the synopsis sets it.</summary>
        public const int WindowSeconds = 300;
        /// <summary>Executive guards in Ice's penthouse, because his line counts four.</summary>
        public const int SuiteGuards = 4;
        /// <summary>How long the terminal and the vault take.</summary>
        public const int TerminalSeconds = 9;
        public const int VaultSeconds = 11;
        /// <summary>How far from a brother's own arrival his work is laid out.</summary>
        public const float SuiteSpread = 7f;
        /// <summary>Where the campaign records the escrow nodes are compromised.</summary>
        public const string NodesEvidence = "aegisEscrowNodes";

        private static readonly Dictionary<CrewSlot, string> Suites = new Dictionary<CrewSlot, string>
        {
            { CrewSlot.Ice, "M55.IceStart" },
            { CrewSlot.Gohan, "M55.GohanStart" },
            { CrewSlot.Guess, "M55.GuessStart" },
        };

        private readonly List<Ped> _suiteGuards = new List<Ped>();
        private Vector3 _terminal, _vault;
        private bool _placed, _suiteClear, _terminalDone, _vaultDone;

        public override string Id => "M55";
        public override string Title => "Skyline Descent";
        protected override MissionEndpoint Endpoint => MissionEndpoint.EscapeCheckpoint;

        /// <summary>All three are in their towers.</summary>
        public bool Placed => _placed;
        /// <summary>Ice's suite is clear.</summary>
        public bool SuiteClear => _suiteClear;
        /// <summary>Gohan has the Arcadius authorizations.</summary>
        public bool TerminalDone => _terminalDone;
        /// <summary>Guess has the secondary ledger.</summary>
        public bool VaultDone => _vaultDone;
        public IReadOnlyList<Ped> SuiteGuardCrew => _suiteGuards;

        /// <summary>
        /// Every penthouse key is seventy to ninety meters up inside a building, so the
        /// engine's walkable query would answer with the street. The regroup on the ground is
        /// deliberately not in this list.
        /// </summary>
        protected override string[] FixedSurfaces =>
            new[] { "M55.IceStart", "M55.GohanStart", "M55.GuessStart" };

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Ice)) return false;

            // Ask the engine whether the buildings are there before putting anybody in them.
            var missing = Suites.Values.Where(key => !MissionSites.InteriorAt(At(key))).ToList();
            if (missing.Count > 0)
            {
                Logger.Error(Id + ": no interior at " + string.Join(", ", missing) +
                    "; three men would be dropped into open sky. Refusing to start.");
                GameUtils.Notify("~r~A penthouse interior did not load. See Bloodlines.log.");
                return false;
            }

            foreach (var pair in Suites) Station(pair.Key, At(pair.Value));
            _placed = true;

            var iceAt = At("M55.IceStart");
            for (int i = 0; i < SuiteGuards; i++)
            {
                var post = MazeBank.Nearby(iceAt, 45.0 + i * 90.0, SuiteSpread, Id + " suite guard " + (i + 1));
                var ped = EnemyAt(post, "M55 suite post " + (i + 1));
                if (ped != null) _suiteGuards.Add(ped);
            }
            if (_suiteGuards.Count == 0)
            {
                Logger.Error(Id + ": no executive guard could be placed in the first penthouse.");
                GameUtils.Notify("~r~The penthouse security could not be placed. See Bloodlines.log.");
                return false;
            }

            _terminal = MazeBank.Nearby(At("M55.GohanStart"), 0.0, SuiteSpread, Id + " escrow terminal");
            _vault = MazeBank.Nearby(At("M55.GuessStart"), 0.0, SuiteSpread, Id + " ledger vault");
            Fighting = true;

            Establish("approach", "Five minutes, three towers",
                "The escrow is split across three penthouses and the moment one of them notices, the other two lock. Ice takes the suite, Gohan takes the Arcadius terminal, Guess takes the ledger vault, and the clock runs for all of them at once.");
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            // One stage, three brothers, three jobs and one clock. Parallel objectives with
            // different owners is what lets the player move between them freely: the
            // dispatcher only demands a switch when the brother he holds has nothing left.
            var suite = new KillTargetsObjective("Ice: suppress the executive guards in the suite",
                () => _suiteGuards) { RequiredCharacter = CrewSlot.Ice };
            var terminal = new MissionInteraction("Gohan: bypass the offshore escrow terminal",
                () => _terminal, TerminalSeconds, 2.5f, animation: MissionInteraction.ReachInside)
            { RequiredCharacter = CrewSlot.Gohan };
            var vault = new MissionInteraction("Guess: crack the vault and pull the secondary ledger",
                () => _vault, VaultSeconds, 2.5f, animation: MissionInteraction.ReachInside)
            { RequiredCharacter = CrewSlot.Guess };
            var clock = new TimerObjective(WindowSeconds,
                "The five minutes ran out and the other two nodes locked. All three have to land inside the window.");

            yield return new MissionStage("Three nodes, five minutes", suite, terminal, vault, clock)
                .OnExit(c => Compromised())
                .WithCues("M55_S1_01_ICE")
                .AfterCues("M55_S1_02_GOHAN", "M55_S1_03_GUESS");

            // M55_S1_04_ICE calls a parachute descent to the canal. The nearest canal is eight
            // hundred meters from a ninety-meter roof, so it is not fired; they regroup instead.
            yield return new MissionStage("Off the towers",
                new TravelObjective("Get out of the towers and regroup", () => At("M55.Regroup"), 20f))
                .AnyBrother();
        }

        private void Compromised()
        {
            _suiteClear = true; _terminalDone = true; _vaultDone = true;
            Ctx.State?.SetEvidence(NodesEvidence, EvidenceState.CopyHeld);
            Logger.Info(Id + ": all three escrow nodes are compromised inside the window.");
            GameUtils.Subtitle("~g~All three nodes inside the window. Get off the towers.", 6000);
        }

        protected override void OnPassed()
        {
            if (!_placed || !_suiteClear || !_terminalDone || !_vaultDone)
                throw new InvalidOperationException("All three nodes have to be taken inside the window.");
        }
    }
}
