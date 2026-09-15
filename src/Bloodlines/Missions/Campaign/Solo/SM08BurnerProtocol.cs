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

        private readonly TargetBlips _blips = new TargetBlips();
        private readonly List<Ped> _security = new List<Ped>();
        private Vector3 _arrival, _terminal, _vault;
        private bool _inside, _downloaded, _armed, _out;

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

        protected override bool Setup()
        {
            var floor = At("SM08.Floor");

            // The office floors are DLC map data. A plain REQUEST_IPL on those quietly does
            // nothing until the shipped archives are registered, and then the interior is
            // simply absent — which is a man standing in open sky at 167 m.
            DlcMaps.RequestIpl("ex_dt1_02_office_01a");
            if (!MissionSites.InteriorAt(floor))
            {
                Logger.Error(Id + ": no interior at " + floor + " after requesting ex_dt1_02_office_01a.");
                GameUtils.Notify("~r~The firm's floor did not load. See Bloodlines.log.");
                return false;
            }

            var gohan = Ctx.Crew.PedFor(CrewSlot.Gohan);
            if (gohan == null || !gohan.Exists()) { Logger.Error(Id + ": Gohan is not available for his own solo."); return false; }
            gohan.Position = floor;
            _arrival = gohan.Position;
            _inside = true;

            _terminal = MazeBank.Nearby(_arrival, 0.0, FloorSpread, Id + " client-file terminal");
            _vault = MazeBank.Nearby(_arrival, 150.0, FloorSpread, Id + " physical file vault");

            for (int i = 0; i < NightGuards; i++)
            {
                var post = MazeBank.Nearby(_arrival, 60.0 + i * 100.0, FloorSpread * 0.8f, Id + " night guard " + (i + 1));
                var ped = Guard(post, WeaponHash.Pistol, true);
                if (ped == null) continue;
                _security.Add(ped);
                _blips.Attach(ped, BlipColor.Red, "Night security");
            }
            if (_security.Count == 0)
                Logger.Warn(Id + ": no night security could be placed; the floor is empty, which at three in the morning is not impossible.");

            // Establish belongs to PreparationOperation, which owns a crew; a solo plays its
            // own scene the way SM05 and SM06 do.
            Ctx.Cutscenes.Play(new SceneSpec
            {
                MissionId = Id, Phase = "approach", Title = "The suits who wrote the ledger",
                Reason = "Show the floor and the paper vault before Gohan starts. He is alone up here.",
                Blocking = new SceneBlocking().Then(ShotStep.Low(2200, gohan, 6, 4, 2))
            });
            RequireSurvivor(gohan, "Gohan is down. Restart this solo mission.");
            return true;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Take the client files",
                new MissionInteraction("Gohan: bypass the biometric lock and copy the client files",
                    () => _terminal, DownloadSeconds, 2.5f, animation: MissionInteraction.ReachInside)
                { RequiredCharacter = CrewSlot.Gohan })
                .OnExit(c => Copied())
                .WithCues("SM08_S1_01_GOHAN")
                .AfterCues("SM08_S1_02_GOHAN");

            yield return new MissionStage("Burn the vault",
                new MissionInteraction("Gohan: run thermite along the filing cabinets",
                    () => _vault, ThermiteSeconds, 2.5f, animation: MissionInteraction.ReachInside)
                { RequiredCharacter = CrewSlot.Gohan })
                .OnExit(c => Ignite())
                .AfterCues("SM08_S2_03_GOHAN");

            // Sixty seconds, because the line says sixty seconds. The clock is the objective's,
            // so it fails with a reason rather than burning him quietly.
            yield return new MissionStage("Get out before it goes",
                new TravelObjective("Get out of the building", () => At("SM08.Exit"), 12f),
                new TimerObjective(BurnSeconds,
                    "The thermite went while Gohan was still on the floor. Sixty seconds means sixty seconds."))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(c => Clear())
                .AfterCues("SM08_S2_04_GOHAN");
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
            _blips.Update();
            base.OnUpdate();
        }

        protected override void OnCleanup()
        {
            _blips.Dispose();
            base.OnCleanup();
        }

        protected override void OnPassed()
        {
            if (!_inside || !_downloaded || !_armed || !_out)
                throw new InvalidOperationException("The files, the thermite and the way out all have to have happened.");
        }
    }
}
