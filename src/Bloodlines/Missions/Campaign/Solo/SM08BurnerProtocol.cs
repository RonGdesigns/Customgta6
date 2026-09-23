using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Missions.Campaign
{
    /// <summary>
    /// SM08 — "Burner Protocol". Gohan alone, 03:00, in a Pillbox Hill office tower.
    ///
    /// Vanderbilt &amp; Cole fabricated the fraud that put Gohan on the street fifteen years
    /// ago. He takes their client files and burns the rest of it.
    ///
    /// The firm is <c>ex_dt1_02_office_01a</c> at (-139.54, -629.08, **167.82**) — a real
    /// executive office interior at the top of a real Pillbox Hill tower. That is the same
    /// building whose roof deck M54 uses as the crew's nest, a hundred and sixty meters
    /// further down, which is a coincidence of geography rather than a shared set.
    ///
    /// **This one does fire its charges**, unlike M51. The difference is not a change of
    /// heart: M51 holds the Palmer-Taylor outage because it belongs to a later mission's beat
    /// and firing it there spends it on an empty street. Here the fire *is* the mission — the
    /// synopsis burns the vault and `SM08_S2_03_GOHAN` counts sixty seconds to ash — so it
    /// burns, and the sixty seconds are a real escape clock rather than a line.
    ///
    /// **Nothing inside is authored but the placement.** The vault, the terminal and the
    /// night security are offsets from where Gohan actually arrives, resolved to walkable
    /// floor. Nobody has walked this floor.
    /// </summary>
    public sealed class SM08BurnerProtocol : DesertOperation
    {
        /// <summary>Night security on the floor. A white-collar firm at three in the morning.</summary>
        public const int NightGuards = 3;
        /// <summary>How far from Gohan's arrival the floor is laid out.</summary>
        public const float FloorSpread = 8f;
        /// <summary>How long the download and the thermite take.</summary>
        public const int DownloadSeconds = 12;
        public const int ThermiteSeconds = 8;
        /// <summary>The escape clock, exactly as SM08_S2_03_GOHAN counts it.</summary>
        public const int BurnSeconds = 60;
        /// <summary>Where the campaign records the files are his.</summary>
        public const string FilesEvidence = "vanderbiltFiles";

        /// <summary>The service elevator: how long it takes, and how near its door counts.</summary>
        public const int ElevatorSeconds = 2;
        public const float ElevatorRadius = 3.5f;
        /// <summary>How near the terminal the night security notices him working it.</summary>
        public const float NoticeMeters = 4f;
        /// <summary>How often a guard who has dropped out of the fight is re-ordered. Never every frame.</summary>
        public const int FightReviewMs = 3000;

        private readonly TargetBlips _blips = new TargetBlips();
        private readonly List<Ped> _security = new List<Ped>();
        private readonly FloorEntry _entry = new FloorEntry();
        /// <summary>Each guard's health when he was posted, so a wound is a change of state.</summary>
        private readonly Dictionary<int, int> _postedHealth = new Dictionary<int, int>();
        private Vector3 _arrival, _terminal, _vault;
        private int _fightReviewAt;
        private bool _inside, _engaged, _downloaded, _armed, _out;

        public override string Id => "SM08";
        public override string Title => "Burner Protocol";
        protected override MissionEndpoint Endpoint => MissionEndpoint.EscapeCheckpoint;

        /// <summary>Gohan is on the firm's floor.</summary>
        public bool Inside => _inside;
        /// <summary>The client files are copied.</summary>
        public bool Downloaded => _downloaded;
        /// <summary>The thermite is set and burning.</summary>
        public bool Armed => _armed;
        /// <summary>He is clear of the building.</summary>
        public bool Out => _out;
        public IReadOnlyList<Ped> Security => _security;

        // No FixedSurfaces here: that override belongs to PreparationOperation, and a
        // solo does not use it. The location book carries the same fact better anyway -
        // the key's kind is "interior", and MissionSites.Prepare only grounds "land", so
        // the walkable query never gets a chance to answer with the street below.

        /// <summary>The firm's office MLO. DLC map data, so it is opened through the tower helper.</summary>
        public const string FloorIpl = "ex_dt1_02_office_01a";

        protected override bool Setup()
        {
            // Gohan on the street below the tower, alone, the way SM05 and SM06 put their man
            // down. Nothing deployed anybody here, so with the crew stood down there was no
            // Gohan to send up - the same fault that stopped SM07 (Ron, September 22).
            if (!MissionSites.Prepare(Ctx.Locations, Id) ||
                !Ctx.Crew.DeploySolo(CrewSlot.Gohan, At("SM08.Start"), Ctx.Locations.Heading("SM08.Start")))
            {
                Logger.Error(Id + ": Gohan could not be put down at SM08.Start.");
                GameUtils.Notify("~r~Gohan could not be placed below the tower. See Bloodlines.log.");
                return false;
            }
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            if (gohan == null || !gohan.Exists())
            {
                Logger.Error(Id + ": Gohan is not available for his own solo.");
                GameUtils.Notify("~r~Gohan is not available for this solo. See Bloodlines.log.");
                return false;
            }
            // No interior check here. The office is DLC map data that some builds only
            // register once the player is near it; the access service asks for the IPL,
            // looks the room up with him there and rolls him back if it never loads, and
            // the ride-up stage turns that into a failure with a reason.
            RequireSurvivor(gohan, "Gohan is down. Restart this solo mission.");
            return true;
        }

        /// <summary>
        /// Up to the firm's floor through the access service: it owns the fade, the IPL, the
        /// room pin and the collision wait. He used to be set down in the MLO directly, never
        /// enabled, pinned or checked ready, with everybody placed in the same frame.
        /// </summary>
        private void GoUp() => _entry.Request(Ctx, At("SM08.Floor"), FloorIpl);

        /// <summary>The floor, laid out from where Gohan actually landed.</summary>
        private void LayOutFloor()
        {
            _arrival = _entry.Arrival;
            _inside = true;

            _terminal = MazeBank.Nearby(_arrival, 0.0, FloorSpread, Id + " client-file terminal");
            _vault = MazeBank.Nearby(_arrival, 150.0, FloorSpread, Id + " physical file vault");

            for (int i = 0; i < NightGuards; i++)
            {
                var post = MazeBank.Nearby(_arrival, 60.0 + i * 100.0, FloorSpread * 0.8f, Id + " night guard " + (i + 1));
                var ped = Guard(post, WeaponHash.Pistol, true);
                if (ped == null) continue;
                _security.Add(ped);
                _postedHealth[ped.Handle] = ped.Health;
                _blips.Attach(ped, BlipColor.Red, "Night security");
            }
            if (_security.Count == 0)
                Logger.Warn(Id + ": no night security could be placed; the floor is empty, which at three in the morning is not impossible.");

            // Establish belongs to PreparationOperation, which owns a crew; a solo plays its
            // own scene the way SM05 and SM06 do - here, where the floor it shows exists.
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            if (gohan != null && gohan.Exists())
                Ctx.Cutscenes.Play(new SceneSpec
                {
                    MissionId = Id, Phase = "approach", Title = "The suits who wrote the ledger",
                    Reason = "Show the floor and the paper vault before Gohan starts. He is alone up here.",
                    Blocking = new SceneBlocking().Then(ShotStep.Low(2200, gohan, 6, 4, 2))
                });
        }

        /// <summary>
        /// Night security goes for him the moment he is working the terminal, or the moment a
        /// shot is fired or one of them is hurt. They used to be placed and never told anything,
        /// with their permanent events blocked, so they stood still for the whole job. After
        /// that, an order goes only to a man who has dropped out of combat.
        /// </summary>
        private void KeepFighting()
        {
            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            if (!_engaged && _inside && gohan != null && gohan.Exists())
            {
                bool working = _terminal != Vector3.Zero && gohan.Position.DistanceTo(_terminal) <= NoticeMeters;
                bool loud = gohan.IsShooting || _security.Any(p => p != null && p.Exists() &&
                    (p.IsDead || (_postedHealth.TryGetValue(p.Handle, out int posted) && p.Health < posted)));
                if (working || loud)
                {
                    _engaged = true;
                    _fightReviewAt = 0;
                    Logger.Info(Id + ": night security is onto Gohan" + (working ? " at the terminal." : "."));
                }
            }
            if (!_engaged || Ctx.Cutscenes.IsActive || Game.GameTime < _fightReviewAt) return;
            _fightReviewAt = Game.GameTime + FightReviewMs;
            foreach (var ped in _security)
            {
                if (ped == null || !ped.Exists() || ped.IsDead || ped.IsInCombat) continue;
                ped.BlockPermanentEvents = false;
                ped.Task.FightAgainstHatedTargets(60f);
            }
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Up to the firm",
                new MissionInteraction("Gohan: take the service elevator up to the Vanderbilt and Cole floor",
                    () => At("SM08.Start"), ElevatorSeconds, ElevatorRadius, animation: MissionInteraction.Operate)
                { RequiredCharacter = CrewSlot.Gohan })
                .OnExit(c => GoUp());

            // The ride is the access service's fade and load. Nothing is placed until it
            // reports Gohan standing on the floor.
            yield return new MissionStage("Riding up",
                new ConditionObjective("Gohan: riding up to the firm's floor", () => _inside))
                .OwnedBy(CrewSlot.Gohan);

            yield return new MissionStage("Take the client files",
                new MissionInteraction("Gohan: bypass the biometric lock and copy the client files",
                    () => _terminal, DownloadSeconds, 2.5f, animation: MissionInteraction.Typing)
                { RequiredCharacter = CrewSlot.Gohan })
                .OnExit(c => Copied())
                .WithCues("SM08_S1_01_GOHAN")
                .AfterCues("SM08_S1_02_GOHAN");

            yield return new MissionStage("Burn the vault",
                new MissionInteraction("Gohan: run thermite along the filing cabinets",
                    () => _vault, ThermiteSeconds, 2.5f, animation: MissionInteraction.Welding)
                { RequiredCharacter = CrewSlot.Gohan })
                .OnExit(c => Ignite())
                .AfterCues("SM08_S2_03_GOHAN");

            // Sixty seconds, because the line says sixty seconds. The clock is the objective's,
            // so it fails with a reason rather than burning him quietly. The way out is the
            // service elevator he came up in: an office MLO a hundred and thirty meters up has
            // no walkable way down, and the street exit this used to ask for was unreachable
            // from the floor (M55's lesson, Ron, September 22).
            yield return new MissionStage("Get out before it goes",
                new MissionInteraction("Gohan: back to the service elevator and down",
                    () => _arrival, ElevatorSeconds, ElevatorRadius, animation: MissionInteraction.Operate)
                { RequiredCharacter = CrewSlot.Gohan },
                new TimerObjective(BurnSeconds,
                    "The thermite went while Gohan was still on the floor. Sixty seconds means sixty seconds."))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(c => GoDown());

            yield return new MissionStage("Riding down",
                new ConditionObjective("Gohan: riding down to the street", () => _entry.Left))
                .OwnedBy(CrewSlot.Gohan);

            yield return new MissionStage("Clear of the building",
                new TravelObjective("Get clear of the building", () => At("SM08.Exit"), 12f))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(c => Clear())
                .AfterCues("SM08_S2_04_GOHAN");
        }

        /// <summary>Down through the access service's own exit, with its fade, to the street he came up from.</summary>
        private void GoDown()
        {
            if (!_entry.RequestExit(Ctx) && !_entry.Refused)
                Logger.Warn(Id + ": the service elevator could not be called down; the floor was not open.");
        }

        private void Copied()
        {
            _downloaded = true;
            Ctx.State?.SetEvidence(FilesEvidence, EvidenceState.CopyHeld);
            Logger.Info(Id + ": the client extortion files are Gohan's.");
        }

        /// <summary>
        /// The thermite. A script fire at the vault is the honest version of the beat: it is
        /// visible, it is where the cabinets are, and it goes out with the mission.
        /// </summary>
        private void Ignite()
        {
            _armed = true;
            try { Function.Call(Hash.START_SCRIPT_FIRE, _vault.X, _vault.Y, _vault.Z, 20, true); }
            catch (Exception ex) { Logger.Error(Id + ": starting the vault fire", ex); }
            GameUtils.Subtitle("~r~Thermite running. Sixty seconds.", 5000);
        }

        private void Clear()
        {
            _out = true;
            Logger.Info(Id + ": Gohan is clear and the paper history is burning.");
        }

        protected override void OnUpdate()
        {
            if (_entry.Update(Ctx) && _entry.Ready) LayOutFloor();
            if (_entry.Refused) { Fail(_entry.Failure); return; }
            _blips.Update();
            KeepFighting();
            base.OnUpdate();
        }

        protected override void OnCleanup()
        {
            _blips.Dispose();
            // Whatever way this ends, Gohan is not left on a burning floor with no door.
            _entry.Release(Ctx, null);
            base.OnCleanup();
        }

        protected override void OnPassed()
        {
            if (!_inside || !_downloaded || !_armed || !_out)
                throw new InvalidOperationException("The files, the thermite and the way out all have to have happened.");
        }
    }
}
