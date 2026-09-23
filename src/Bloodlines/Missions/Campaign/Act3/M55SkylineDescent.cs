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
    ///
    /// **There is no door out of a blimp interior.** Those ymaps are the apartments the game
    /// shows through the windows from the air; they have floors and walls and no way down,
    /// which Ron found by finishing the job and being unable to leave (September 17). The
    /// way out is a service elevator: an interaction at each brother's own arrival point
    /// that takes everybody to street level at the foot of his tower. The street is not
    /// authored - nobody knows which side of the tower the sidewalk is on - it is found at
    /// runtime under the suite's own x and y (<see cref="StreetBelow"/>).
    ///
    /// And every suite has security now, not only Ice's. Ron's note: "we should have
    /// enemies spawn for all the homies". Ice's four are the authored count; Gohan and
    /// Guess each get three, so the terminal and the vault are taken under fire.
    /// </summary>
    public sealed class M55SkylineDescent : PreparationOperation
    {
        /// <summary>The window, exactly as the synopsis sets it.</summary>
        public const int WindowSeconds = 300;
        /// <summary>Executive guards in Ice's penthouse, because his line counts four.</summary>
        public const int SuiteGuards = 4;
        /// <summary>Security on the other two nodes. Not authored; three each is a fight, not a wall.</summary>
        public const int TerminalGuards = 3, VaultGuards = 3;
        /// <summary>How long the service elevator takes.</summary>
        public const int ElevatorSeconds = 3;
        /// <summary>Where street level is guessed to be when nothing solid answers under a tower.</summary>
        public const float StreetGuessZ = 31f;
        /// <summary>How long the terminal and the vault take.</summary>
        public const int TerminalSeconds = 9;
        public const int VaultSeconds = 11;
        /// <summary>How far from a brother's own arrival his work is laid out.</summary>
        public const float SuiteSpread = 7f;
        /// <summary>Where the campaign records the escrow nodes are compromised.</summary>
        public const string NodesEvidence = "aegisEscrowNodes";
        /// <summary>
        /// How far from his arrival point a guard is stood when the walkable query answers
        /// nothing, nearest last. MazeBank.Nearby hands back the arrival point itself in that
        /// case, and in a blimp interior - or a tower 450 m from the player whose navmesh is
        /// not loaded - that stacked every guard on the brother he was guarding (Ron,
        /// September 22). Each distance is only taken if the game has the interior there.
        /// </summary>
        private static readonly float[] FallbackReach = { 4.5f, 3.5f, 2.5f };
        /// <summary>Closer than this to the arrival point and a post counts as stacked.</summary>
        public const float StackedMeters = 1.5f;
        /// <summary>
        /// A suite's security joins the fight when the player is that brother or comes this
        /// close to it. Until then nobody there knows anything is happening.
        /// </summary>
        public const float LiveMeters = 120f;
        /// <summary>Near enough to the regroup that the street under it is streamed and can be asked for.</summary>
        public const float RegroupSettleMeters = 120f;

        private static readonly Dictionary<CrewSlot, string> Suites = new Dictionary<CrewSlot, string>
        {
            { CrewSlot.Ice, "M55.IceStart" },
            { CrewSlot.Gohan, "M55.GohanStart" },
            { CrewSlot.Guess, "M55.GuessStart" },
        };

        private readonly List<Ped> _suiteGuards = new List<Ped>();
        private readonly List<Ped> _terminalGuards = new List<Ped>();
        private readonly List<Ped> _vaultGuards = new List<Ped>();
        private Vector3 _terminal, _vault, _regroup;
        private bool _placed, _suiteClear, _terminalDone, _vaultDone, _regroupSettled;
        /// <summary>Security at a suite the player is not at, kept out of the fight until he is.</summary>
        private readonly Dictionary<CrewSlot, List<Ped>> _held = new Dictionary<CrewSlot, List<Ped>>();

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
        public IReadOnlyList<Ped> TerminalGuardCrew => _terminalGuards;
        public IReadOnlyList<Ped> VaultGuardCrew => _vaultGuards;
        /// <summary>Where each brother came out at the bottom of his tower, once the elevator has run.</summary>
        public readonly Dictionary<CrewSlot, Vector3> StreetExits = new Dictionary<CrewSlot, Vector3>();

        /// <summary>Suites whose security is still being held out of the fight.</summary>
        public IEnumerable<CrewSlot> HeldSuites => _held.Keys;

        /// <summary>
        /// Every penthouse key is seventy to ninety meters up inside a building, so the
        /// engine's walkable query would answer with the street.
        ///
        /// The regroup is on the street, and it is here anyway. It is about 290 m from the
        /// first penthouse, and ground preparation runs in Setup: an estimate with no walkable
        /// answer within twelve meters refuses the whole mission, and navmesh that far from the
        /// player may simply not be loaded yet. It is put on the street when the player gets
        /// near it instead (<see cref="Regroup"/>), and the zone is flat, so its height only
        /// moves the marker.
        /// </summary>
        protected override string[] FixedSurfaces =>
            new[] { "M55.IceStart", "M55.GohanStart", "M55.GuessStart", "M55.Regroup" };

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

            Security(At("M55.IceStart"), SuiteGuards, "suite", _suiteGuards);
            Security(At("M55.GohanStart"), TerminalGuards, "terminal", _terminalGuards);
            Security(At("M55.GuessStart"), VaultGuards, "vault", _vaultGuards);
            if (_suiteGuards.Count == 0 || _terminalGuards.Count == 0 || _vaultGuards.Count == 0)
            {
                Logger.Error(Id + ": a penthouse has no security at all; nothing to suppress there.");
                GameUtils.Notify("~r~The penthouse security could not be placed. See Bloodlines.log.");
                return false;
            }

            _terminal = MazeBank.Nearby(At("M55.GohanStart"), 0.0, SuiteSpread, Id + " escrow terminal");
            _vault = MazeBank.Nearby(At("M55.GuessStart"), 0.0, SuiteSpread, Id + " ledger vault");

            // The fight starts where the player is. Declaring it for all three towers at once
            // radioed every guard in the city onto whichever brother stood nearest, which in a
            // tower the player had never been to was the AI brother he could not help.
            Hold(CrewSlot.Ice, _suiteGuards);
            Hold(CrewSlot.Gohan, _terminalGuards);
            Hold(CrewSlot.Guess, _vaultGuards);
            WakeSuites();
            Fighting = true;

            Establish("approach", "Five minutes, three towers",
                "The escrow is split across three penthouses and the moment one of them notices, the other two lock. Ice takes the suite, Gohan takes the Arcadius terminal, Guess takes the ledger vault, and the clock runs for all of them at once.");
            return true;
        }

        /// <summary>A detail of <paramref name="count"/> around a brother's arrival, spread on the compass.</summary>
        private void Security(Vector3 around, int count, string where, List<Ped> into)
        {
            for (int i = 0; i < count; i++)
            {
                double degrees = 45.0 + i * (360.0 / count);
                string what = Id + " " + where + " guard " + (i + 1);
                var post = MazeBank.Nearby(around, degrees, SuiteSpread, what);
                if (GameUtils.IsWithinFlat(post, around, StackedMeters)) post = OffTheArrival(around, degrees, what);
                var ped = EnemyAt(post, "M55 " + where + " post " + (i + 1));
                if (ped != null) into.Add(ped);
            }
        }

        /// <summary>
        /// A post on the guard's own compass bearing, a few meters out from the arrival point,
        /// when the walkable query gave back the arrival point itself. The farthest distance the
        /// game says is still inside an interior wins; if it will not say, the shortest one is
        /// used, because a guard 2.5 m across a room is a fight and one standing inside the
        /// brother is a point-blank firefight.
        /// </summary>
        private static Vector3 OffTheArrival(Vector3 arrival, double degrees, string what)
        {
            double radians = degrees * Math.PI / 180.0;
            var bearing = new Vector3((float)Math.Cos(radians), (float)Math.Sin(radians), 0f);
            foreach (float reach in FallbackReach)
            {
                var candidate = arrival + bearing * reach;
                if (!MissionSites.InteriorAt(candidate)) continue;
                Logger.Info(what + ": no walkable floor answered; standing him " + reach + " m out from the arrival instead.");
                return candidate;
            }
            float shortest = FallbackReach[FallbackReach.Length - 1];
            Logger.Warn(what + ": no interior answered around the arrival; standing him " + shortest + " m out, unverified.");
            return arrival + bearing * shortest;
        }

        /// <summary>Keep a suite's security out of the fight - no awareness, no role-track threat - until it is live.</summary>
        private void Hold(CrewSlot slot, List<Ped> guards)
        {
            _held[slot] = guards;
            foreach (var guard in guards) Opposition.Remove(guard);
        }

        /// <summary>
        /// Bring a held suite into the fight: the player is that brother now, or he has come
        /// within <see cref="LiveMeters"/> of it. Called every frame; each suite wakes once.
        /// </summary>
        private void WakeSuites()
        {
            if (_held.Count == 0) return;
            var player = Game.Player.Character;
            foreach (var slot in _held.Keys.ToList())
            {
                bool live = Ctx.Crew.ActiveSlot == slot ||
                    (player != null && player.Exists() && player.Position.DistanceTo(At(Suites[slot])) < LiveMeters);
                if (!live) continue;
                foreach (var guard in _held[slot])
                    if (guard != null && guard.Exists() && !Opposition.Contains(guard)) Opposition.Add(guard);
                _held.Remove(slot);
                Logger.Info(Id + ": the " + slot + " suite's security is in the fight now.");
            }
        }

        /// <summary>
        /// The regroup, on the street once the player is near enough for the street to be
        /// streamed; the authored estimate until then. Settled once.
        /// </summary>
        private Vector3 Regroup()
        {
            var authored = At("M55.Regroup");
            if (_regroupSettled) return _regroup;
            var player = Game.Player.Character;
            if (player == null || !player.Exists() || !GameUtils.IsWithinFlat(player.Position, authored, RegroupSettleMeters)) return authored;
            var street = World.GetSafeCoordForPed(authored, true, 0);
            bool usable = street != Vector3.Zero && GameUtils.IsWithinFlat(street, authored, MissionSites.EstimateDrift) &&
                          Math.Abs(street.Z - authored.Z) < MissionSites.EstimateDrop;
            _regroup = usable ? street : authored;
            _regroupSettled = true;
            Logger.Info(Id + ": the regroup is " + (usable ? "on the street at " + street : "kept at its estimate " + authored) + ".");
            return _regroup;
        }

        protected override void OnUpdate()
        {
            WakeSuites();
            base.OnUpdate();
        }

        /// <summary>
        /// Street level at the foot of a tower, found rather than authored. A downward probe
        /// from just under the suite finds whatever the building stands on; the nearest
        /// sidewalk to that is where a man comes out of a lobby. Nothing found at all keeps
        /// the x and y and guesses the downtown street height, and says so.
        /// </summary>
        public static Vector3 StreetBelow(Vector3 suite, string what)
        {
            float? slab = MissionSites.SurfaceHeight(suite, suite.Z - 3f, -5f);
            var foot = new Vector3(suite.X, suite.Y, slab ?? StreetGuessZ);
            if (!slab.HasValue) Logger.Warn(what + ": nothing solid under the tower; guessing street level at " + StreetGuessZ);
            var sidewalk = World.GetSafeCoordForPed(foot, true, 0);
            if (sidewalk == Vector3.Zero || sidewalk.DistanceTo(foot) > 60f) sidewalk = World.GetNextPositionOnStreet(foot);
            if (sidewalk == Vector3.Zero) { Logger.Warn(what + ": no sidewalk or street near the tower foot; using it as is."); return foot; }
            return sidewalk;
        }

        /// <summary>Everybody out at the bottom of his own tower. Runs once, when the elevator stage ends.</summary>
        private void Descend()
        {
            GameUtils.FadeOut(300);
            var player = Ctx.Crew.PedFor(Ctx.Crew.ActiveSlot);
            foreach (var pair in Suites)
            {
                var ped = Ctx.Crew.PedFor(pair.Key);
                if (ped == null || !ped.Exists() || ped.IsDead) continue;
                var exit = StreetBelow(At(pair.Value), Id + " " + pair.Key + " exit");
                StreetExits[pair.Key] = exit;
                ped.Task.ClearAllImmediately();
                GTA.Native.Function.Call(GTA.Native.Hash.REQUEST_COLLISION_AT_COORD, exit.X, exit.Y, exit.Z);
                ped.Position = exit;
                if (player != null && player.Exists() && ped.Handle != player.Handle && player.Position.DistanceTo(exit) > FarPlacement.Meters)
                    FarPlacement.Keep(ped, "came out of his tower " + (int)player.Position.DistanceTo(exit) + " m from the player");
                if (pair.Key != Ctx.Crew.ActiveSlot) ped.Task.GuardCurrentPosition();
                Logger.Info(Id + ": " + pair.Key + " is at street level at " + exit);
            }
            GameUtils.FadeIn(600);
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            // One stage, three brothers, three jobs and one clock. Parallel objectives with
            // different owners is what lets the player move between them freely: the
            // dispatcher only demands a switch when the brother he holds has nothing left.
            var suite = new KillTargetsObjective("Ice: suppress the executive guards in the suite",
                () => _suiteGuards) { RequiredCharacter = CrewSlot.Ice };
            var terminalSecurity = new KillTargetsObjective("Gohan: put down the terminal security",
                () => _terminalGuards) { RequiredCharacter = CrewSlot.Gohan };
            var vaultSecurity = new KillTargetsObjective("Guess: put down the vault security",
                () => _vaultGuards) { RequiredCharacter = CrewSlot.Guess };
            var terminal = new MissionInteraction("Gohan: bypass the offshore escrow terminal",
                () => _terminal, TerminalSeconds, 2.5f, animation: MissionInteraction.Typing)
            { RequiredCharacter = CrewSlot.Gohan };
            var vault = new MissionInteraction("Guess: crack the vault and pull the secondary ledger",
                () => _vault, VaultSeconds, 2.5f, animation: MissionInteraction.ReachInside)
            { RequiredCharacter = CrewSlot.Guess };
            var clock = new TimerObjective(WindowSeconds,
                "The five minutes ran out and the other two nodes locked. All three have to land inside the window.");

            yield return new MissionStage("Three nodes, five minutes", suite, terminalSecurity, vaultSecurity, terminal, vault, clock)
                .OnExit(c => Compromised())
                .WithCues("M55_S1_01_ICE")
                .AfterCues("M55_S1_02_GOHAN", "M55_S1_03_GUESS");

            // The way down. A blimp interior has no door, so each brother's own arrival point
            // is his service elevator; whichever one runs first takes everybody to the street.
            yield return new MissionStage("Down to the street",
                new MissionInteraction("Ice: take the service elevator down", () => At("M55.IceStart"), ElevatorSeconds, 3.5f, animation: MissionInteraction.Operate) { RequiredCharacter = CrewSlot.Ice },
                new MissionInteraction("Gohan: take the service elevator down", () => At("M55.GohanStart"), ElevatorSeconds, 3.5f, animation: MissionInteraction.Operate) { RequiredCharacter = CrewSlot.Gohan },
                new MissionInteraction("Guess: take the service elevator down", () => At("M55.GuessStart"), ElevatorSeconds, 3.5f, animation: MissionInteraction.Operate) { RequiredCharacter = CrewSlot.Guess })
                .AnyOf()
                .OnExit(c => Descend());

            // M55_S1_04_ICE calls a parachute descent to the canal. The nearest canal is eight
            // hundred meters from a ninety-meter roof, so it is not fired; they regroup instead.
            yield return new MissionStage("Off the towers",
                new TravelObjective("Get out of the towers and regroup", Regroup, 20f))
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
