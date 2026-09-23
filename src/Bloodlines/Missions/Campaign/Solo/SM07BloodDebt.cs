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
    /// SM07 — "Blood Debt". Ice alone, 23:00, in the rain.
    ///
    /// Major Sterling sold Ice's logistics unit for a kickback fifteen years ago and is now an
    /// Aegis vice president. Ice goes up, through his detail, and settles it.
    ///
    /// The suite is <c>v_apartment_high</c> at (-260.88, -953.56, **70.02**) — one of the
    /// game's own high-end apartments, stock base map, at a real Pillbox Hill tower. Nothing
    /// is requested and nothing is swapped.
    ///
    /// **The district moved and that is worth saying plainly.** The bible puts Sterling in a
    /// Del Perro hotel. There is no hotel interior in Del Perro, or anywhere else — a sweep of
    /// every MLO in Los Santos returns apartments, offices, garages, a barber, a tattoo parlor,
    /// a coroner, a chop shop, a police hub and the finale bank, and no hotel. So the suite is
    /// a high-rise apartment in Pillbox Hill. It is a penthouse with private security and a
    /// view, which is what the mission needs it to be; it is not Del Perro. Recorded in
    /// `data/mission_gameplay.tsv`.
    ///
    /// **Nothing inside is authored but the placement.** Sterling and his four bodyguards are
    /// offsets from where Ice actually arrives, resolved to walkable floor. Nobody has walked
    /// this suite.
    ///
    /// One thing for the author rather than the code: `SM07_S2_03_ENEMY` has Sterling shout
    /// "Vance!" at Ice, and Vance is the Aegis CEO Ice kills in M65. That reads as a slip in
    /// the source. The extraction is never edited to follow gameplay, so the line fires as
    /// written; a revision belongs in `data/dialogue_edits.json` if it is wanted.
    /// </summary>
    public sealed class SM07BloodDebt : DesertOperation
    {
        public const string SterlingModel = "a_m_m_business_01";
        /// <summary>His detail, because SM07_S1_02_ICE counts four in the foyer.</summary>
        public const int Bodyguards = 4;
        /// <summary>How far from Ice's arrival the suite is laid out.</summary>
        public const float SuiteSpread = 7f;
        /// <summary>He is a major with a bodyguard detail, not a passer-by.</summary>
        public const int SterlingHealth = 400;
        /// <summary>Where the campaign records the debt is settled.</summary>
        public const string DebtEvidence = "sterlingSettled";

        /// <summary>How long the service elevator takes to call, and how near its door counts.</summary>
        public const int ElevatorSeconds = 2;
        public const float ElevatorRadius = 3.5f;
        /// <summary>How often a guard who has dropped out of the fight is checked and re-ordered. Never every frame.</summary>
        public const int FightReviewMs = 3000;

        private readonly TargetBlips _blips = new TargetBlips();
        private readonly List<Ped> _detail = new List<Ped>();
        private readonly FloorEntry _entry = new FloorEntry();
        private Ped _sterling;
        private Vector3 _arrival;
        private int _fightReviewAt;
        private bool _upstairs, _engaged, _foyerClear, _settled;

        public override string Id => "SM07";
        public override string Title => "Blood Debt";
        protected override MissionEndpoint Endpoint => MissionEndpoint.EscapeCheckpoint;

        /// <summary>Ice is in the suite.</summary>
        public bool Upstairs => _upstairs;
        /// <summary>The foyer detail is down.</summary>
        public bool FoyerClear => _foyerClear;
        /// <summary>Sterling is dead.</summary>
        public bool Settled => _settled;
        public Ped Sterling => _sterling;
        public IReadOnlyList<Ped> Detail => _detail;

        // No FixedSurfaces here: that override belongs to PreparationOperation, and a
        // solo does not use it. The location book carries the same fact better anyway -
        // the key's kind is "interior", and MissionSites.Prepare only grounds "land", so
        // the walkable query never gets a chance to answer with the street below.

        protected override bool Setup()
        {
            var suite = At("SM07.Suite");
            if (!MissionSites.InteriorAt(suite))
            {
                Logger.Error(Id + ": no interior at " + suite + "; Ice would be dropped into open sky.");
                GameUtils.Notify("~r~The suite interior did not load. See Bloodlines.log.");
                return false;
            }

            // Ice on the street below the tower, alone, the way SM05 and SM06 put their man
            // down. Nothing deployed anybody here, so with the crew stood down there was no
            // Ice to send up and the mission refused itself (Ron's log, September 22).
            if (!MissionSites.Prepare(Ctx.Locations, Id) ||
                !Ctx.Crew.DeploySolo(CrewSlot.Ice, At("SM07.Start"), Ctx.Locations.Heading("SM07.Start")))
            {
                Logger.Error(Id + ": Ice could not be put down at SM07.Start.");
                GameUtils.Notify("~r~Ice could not be placed below the tower. See Bloodlines.log.");
                return false;
            }
            var ice = Ctx.Crew.PedFor(CrewSlot.Ice);
            if (ice == null || !ice.Exists())
            {
                Logger.Error(Id + ": Ice is not available for his own solo.");
                GameUtils.Notify("~r~Ice is not available for this solo. See Bloodlines.log.");
                return false;
            }

            // A suppressed rifle, because SM07_S1_02_ICE breaches the foyer with one.
            ice.Weapons.Give(WeaponHash.CarbineRifle, 250, false, true);
            RequireSurvivor(ice, "Ice is down. Restart this solo mission.");
            return true;
        }

        /// <summary>
        /// Up to the suite through the access service, the way every interior in this campaign
        /// is opened: it owns the fade, pins the room and waits for its collision. Placing
        /// Sterling waits for it to say Ice is standing in the suite.
        /// </summary>
        private void GoUp() => _entry.Request(Ctx, At("SM07.Suite"), null);

        /// <summary>
        /// Sterling and his detail, from where Ice actually landed. Returns false when the
        /// suite could not be laid out, having failed the mission with the reason.
        /// </summary>
        private bool LayOutSuite()
        {
            _arrival = _entry.Arrival;
            var model = new Model(SterlingModel);
            if (GameUtils.RequestModel(model))
            {
                var stand = MazeBank.Nearby(_arrival, 180.0, SuiteSpread, Id + " Sterling");
                _sterling = Track(World.CreatePed(model, stand, 0f));
                model.MarkAsNoLongerNeeded();
            }
            if (_sterling == null || !_sterling.Exists())
            {
                Logger.Error(Id + ": Major Sterling could not be placed in the suite.");
                GameUtils.Notify("~r~The suite did not load. Restart this mission.");
                return false;
            }
            _sterling.IsPersistent = true;
            _sterling.BlockPermanentEvents = true;
            _sterling.MaxHealth = SterlingHealth;
            _sterling.Health = SterlingHealth;
            _sterling.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            _blips.Attach(_sterling, BlipColor.Red, "Major Sterling");
            // No RequireAsset on Sterling. That contract fails the mission when the entity
            // dies, and killing him is the objective — it cannot tell the intended death
            // from a despawn, so it would fail the mission at the moment it succeeds.

            for (int i = 0; i < Bodyguards; i++)
            {
                var post = MazeBank.Nearby(_arrival, 40.0 + i * 70.0, SuiteSpread * 0.85f, Id + " bodyguard " + (i + 1));
                var ped = Guard(post, WeaponHash.CarbineRifle, true);
                if (ped == null) continue;
                _detail.Add(ped);
                _blips.Attach(ped, BlipColor.Red, "Executive bodyguard");
            }
            if (_detail.Count == 0)
            {
                Logger.Error(Id + ": no bodyguard could be placed in the foyer.");
                GameUtils.Notify("~r~The suite security could not be placed. See Bloodlines.log.");
                return false;
            }

            _upstairs = true;

            // Establish belongs to PreparationOperation, which owns a crew; a solo plays its
            // own scene the way SM05 and SM06 do. It plays here rather than in Setup because
            // this is the first moment Sterling exists to be shown.
            Ctx.Cutscenes.Play(new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "Fifteen years, one name",
                Reason = "Show Sterling and his detail before Ice moves. Nobody else is in this one; the crew is not here.",
                Blocking = new SceneBlocking().Then(ShotStep.Low(2200, _sterling, 5, 3, 2))
            });
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Up to the suite",
                new MissionInteraction("Ice: take the service elevator up to Sterling's floor",
                    () => At("SM07.Start"), ElevatorSeconds, ElevatorRadius, animation: MissionInteraction.Operate)
                { RequiredCharacter = CrewSlot.Ice })
                .OnExit(c => GoUp());

            // The ride is the access service's fade and load. Nothing is placed until it
            // reports Ice standing in the suite.
            yield return new MissionStage("Riding up",
                new ConditionObjective("Ice: riding up to Sterling's floor", () => _upstairs))
                .OwnedBy(CrewSlot.Ice);

            yield return new MissionStage("Take the foyer",
                new KillTargetsObjective("Ice: take the executive detail in the foyer", () => _detail))
                .OwnedBy(CrewSlot.Ice)
                .OnEnter(c => _engaged = true)
                .WithCues("SM07_S1_01_ICE")
                .AfterCues("SM07_S1_02_ICE");

            yield return new MissionStage("Settle it",
                new ConditionObjective("Ice: Sterling", () => _sterling != null && _sterling.Exists() && _sterling.IsDead)
                { Marker = () => _sterling != null && _sterling.Exists() ? _sterling.Position : _arrival, MarkerRadius = 2.5f })
                .OwnedBy(CrewSlot.Ice)
                .OnExit(c => Done())
                .WithCues("SM07_S2_03_ENEMY")
                .AfterCues("SM07_S2_04_ICE", "SM07_S2_05_ICE");
        }

        /// <summary>
        /// The detail knows the moment the door goes. The only order used to be one
        /// FightAgainstHatedTargets on stage entry, given while the approach scene was still
        /// playing and to men whose permanent events were blocked, so a guard whose task
        /// lapsed simply stood there. Orders go out once the scene is over, and again only to
        /// a man who has dropped out of combat - a state change, never a timer on everybody.
        /// </summary>
        private void KeepFighting()
        {
            if (!_engaged || Ctx.Cutscenes.IsActive || Game.GameTime < _fightReviewAt) return;
            _fightReviewAt = Game.GameTime + FightReviewMs;
            foreach (var ped in _detail.Concat(new[] { _sterling }))
            {
                if (ped == null || !ped.Exists() || ped.IsDead || ped.IsInCombat) continue;
                ped.BlockPermanentEvents = false;
                ped.Task.FightAgainstHatedTargets(60f);
            }
        }

        private void Done()
        {
            _foyerClear = true;
            _settled = true;
            Ctx.State?.SetEvidence(DebtEvidence, EvidenceState.CopyHeld);
            Logger.Info(Id + ": Sterling is dead and Ice's ledger is closed.");
        }

        protected override void OnUpdate()
        {
            if (_entry.Update(Ctx) && !LayOutSuite())
            {
                Fail("The suite could not be laid out. Restart this solo mission.");
                return;
            }
            if (_entry.Refused) { Fail(_entry.Failure); return; }
            _blips.Update();
            KeepFighting();
            base.OnUpdate();
        }

        protected override void OnCleanup()
        {
            _blips.Dispose();
            // A blimp interior has no door (M55's lesson). Whatever way this ends, Ice goes
            // back to the street he came up from rather than being left in the suite.
            _entry.Release(Ctx, null);
            base.OnCleanup();
        }

        protected override void OnPassed()
        {
            if (!_upstairs || !_settled)
                throw new InvalidOperationException("Ice has to reach the suite and Sterling has to be dead.");
        }
    }
}
