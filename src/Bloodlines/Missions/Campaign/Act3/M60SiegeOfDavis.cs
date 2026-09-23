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
    /// M60 — "Siege of Davis". The block the three of them grew up on, 17:00, smoke.
    ///
    /// Aegis sends death squads into Davis to burn it. Three waves come down the street and
    /// the crew, with whoever still lives there, does not let them.
    ///
    /// This one is a defense, so it is built as one: <see cref="SurviveWavesObjective"/> with
    /// three waves, which is the objective the campaign already has for exactly this shape.
    /// The mission's job is to place the pocket, the allies and the line, and then get out of
    /// the way.
    ///
    /// **Everything here is at street level and stays there.** Davis is an ordinary
    /// neighborhood at z 19 to 22 — the survey band puts 559 of 572 entities under 26 — so
    /// ground preparation is right about all of it and there are no fixed surfaces and no
    /// probes. Overriding the walkable query on a street would be inventing a problem.
    ///
    /// **Two authored beats are not reproduced**, and the reason in both cases is that the
    /// mission would be lying about the world:
    ///
    /// `M60_S1_03_GOHAN` sets EMP charges under the streetlamps to kill APC engines. There is
    /// no EMP in the game and no engine-kill that reads as one; faking it by setting
    /// `IsDriveable = false` on a healthy vehicle is a wrecked APC with no cause on screen.
    ///
    /// `M60_S1_05_GOHAN` detonates an underground gas main. There is no gas main under Davis
    /// in the archives and no placed geometry to blow, and an explosion with nothing under it
    /// is a flash on a road.
    ///
    /// The other four lines fire. `M60_S1_04_ICE` puts Ice on a church roof with the LMG; the
    /// roof is not modeled either, so he gets the LMG and picks his own ground, which is what a
    /// street defense is. All of it is recorded in `data/mission_gameplay.tsv`.
    /// </summary>
    public sealed class M60SiegeOfDavis : PreparationOperation
    {
        public const string RaiderModel = "s_m_y_blackops_01";
        public const string AllyModel = "g_m_y_famca_01";
        /// <summary>Three waves, as the synopsis sets them.</summary>
        public const int Waves = 3;
        /// <summary>How many contractors come in each wave.</summary>
        public const int PerWave = 5;
        /// <summary>How long the block gets between waves.</summary>
        public const int WaveGapMs = 9000;
        /// <summary>Neighbors who stayed and are holding the alley.</summary>
        public const int AllyPosts = 3;
        /// <summary>How far a wave spawns from its post, so five men are not stacked on one spot.</summary>
        public const float WaveSpread = 5f;
        /// <summary>Where the campaign records Davis held.</summary>
        public const string HeldCargo = "davisHeld";
        /// <summary>How far a neighbor holding the alley will reach for a contractor.</summary>
        public const float AllyReach = 90f;

        private readonly List<Ped> _allies = new List<Ped>();
        private readonly List<Ped> _raiders = new List<Ped>();
        private int _wavesSeen;
        private bool _held;

        public override string Id => "M60";
        public override string Title => "Siege of Davis";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SafehouseArrival;

        /// <summary>How many waves have been put on the street so far.</summary>
        public int WavesSeen => _wavesSeen;
        /// <summary>The block held.</summary>
        public bool Held => _held;
        public IReadOnlyList<Ped> Allies => _allies;
        public IReadOnlyList<Ped> Raiders => _raiders;

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Ice)) return false;

            // The neighbors. They are friendly, they are armed, and they are the reason this
            // is a siege rather than three men in a street.
            for (int i = 1; i <= AllyPosts; i++)
            {
                var ped = Person(AllyModel, "M60.Ally" + i);
                if (ped == null) continue;
                ped.Weapons.Give(WeaponHash.MicroSMG, 200, false, true);
                ped.Task.GuardCurrentPosition();
                _allies.Add(ped);
            }
            if (_allies.Count == 0)
                Logger.Warn(Id + ": nobody from the block could be placed; the crew holds it alone.");

            // An LMG for Ice, because M60_S1_04_ICE is firing one, and rifles for the others.
            foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Gohan, CrewSlot.Guess })
            {
                var ped = Ctx.Crew.PedFor(slot);
                if (ped == null || !ped.Exists()) continue;
                ped.Weapons.Give(slot == CrewSlot.Ice ? WeaponHash.MG : WeaponHash.CarbineRifle, 400, false, true);
            }

            Paleto.Review(Ctx, PlacementContract.Ped("M60.Start"), PlacementContract.Ped("M60.Hold"),
                PlacementContract.Ped("M60.Wave1"), PlacementContract.Ped("M60.Wave2"), PlacementContract.Ped("M60.Wave3"));

            Establish("approach", "Not while we breathe",
                "Aegis is putting death squads into Davis to burn the block the three of them grew up on. Whoever still lives here is already out on the street.");
            return true;
        }

        /// <summary>
        /// One wave, spread along its approach so five men are not asked for the same square
        /// meter — which is what made M35 refuse to load. The index the objective passes in is
        /// one-based across the three waves.
        /// </summary>
        private IEnumerable<Ped> Wave(int index)
        {
            _wavesSeen = Math.Max(_wavesSeen, index);
            string key = "M60.Wave" + Math.Min(Math.Max(index, 1), Waves);
            var post = At(key);
            // How many arrive is the whole shape of a wave, so the wave keys are declared
            // groups and the placement editor can size them. PerWave is what an unedited
            // key falls back to, which is exactly what this mission did before.
            int men = MissionPlacement.Count(Ctx.Locations, key, PerWave);
            var wave = new List<Ped>();
            for (int i = 0; i < men; i++)
            {
                var ped = Guard(MissionPlacement.PointFor(Ctx.Locations, key, i,
                    post + new Vector3(i * WaveSpread, i % 2 * WaveSpread, 0f)));
                if (ped == null) continue;
                Opposition.Add(ped);
                Blips.Attach(ped, BlipColor.Red, "Aegis contractor");
                _raiders.Add(ped);
                wave.Add(ped);
                // A wave arrives knowing what it came for. The fight's one radio call goes out
                // when the siege begins, to whoever is on the street then; waves two and three
                // were created after it and walked onto the block unaware (Ron, September 22).
                if (Awareness != null)
                {
                    Awareness.Track(ped);
                    Awareness.Report(ped, Stimulus.RadioCall, At("M60.Hold"));
                }
            }
            if (wave.Count == 0) Logger.Error(Id + ": wave " + index + " could not be placed at " + post + ".");
            Logger.Info(Id + ": wave " + index + " is on the street with " + wave.Count + " men.");
            RallyTheBlock();
            return wave;
        }

        /// <summary>
        /// The neighbors join each wave's fight. They were created with their permanent events
        /// blocked and given one guard order, and nothing ever told them to shoot: the people
        /// the siege is named for stood in the alley while the block was burned. One order per
        /// wave, which is the change of state that matters to them; between waves the order
        /// runs out with nobody left to fight.
        /// </summary>
        private void RallyTheBlock()
        {
            foreach (var ally in _allies)
            {
                if (ally == null || !ally.Exists() || ally.IsDead) continue;
                ally.BlockPermanentEvents = false;
                ally.Task.FightAgainstHatedTargets(AllyReach);
            }
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Set the pocket",
                new ReachZoneObjective("Guess: set the roadblock and hold the corner", () => At("M60.Hold"), 8f))
                .OwnedBy(CrewSlot.Guess)
                .WithCues("M60_S1_01_ICE")
                .AfterCues("M60_S1_02_GUESS");

            yield return new MissionStage("Hold the line",
                new SurviveWavesObjective("Hold Davis. Three waves of Aegis contractors.", Wave, Waves, WaveGapMs))
                .AnyBrother()
                .OnEnter(c => Fighting = true)
                .OnExit(c => Stood())
                .AfterCues("M60_S1_04_ICE", "M60_S1_06_GUESS");
        }

        private void Stood()
        {
            _held = true;
            Ctx.State?.SetCargo(HeldCargo, "M60.Hold");
            Logger.Info(Id + ": all three waves are down and the block is still standing.");
            GameUtils.Subtitle("~g~Davis stands.", 6000);
        }

        protected override void OnPassed()
        {
            if (!_held || _wavesSeen < Waves)
                throw new InvalidOperationException("All three waves have to have come and been stopped.");
        }
    }
}
