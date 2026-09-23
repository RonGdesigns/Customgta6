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
    /// M65 — "Executive Privilege". The Maze Bank executive floor, 22:00.
    ///
    /// Colonel Vance and his bodyguards make their stand. Ice takes Vance; Gohan uses his
    /// biometrics to open the escrow.
    ///
    /// The floor is <c>ex_dt1_11_office_01a</c> at (-73.80, -818.96, **242.39**) — a real
    /// executive office interior inside the real Maze Bank Tower, one of nine decor variants
    /// sharing that placement. See <see cref="MazeBank"/>.
    ///
    /// The bible calls it the hundredth floor; 242 meters is nearer the seventieth. The
    /// building's own top interior is this one, so this is the boardroom. Recorded in
    /// `data/mission_gameplay.tsv` rather than by editing the extraction.
    ///
    /// **Vance must be taken alive to his terminal, then taken.** Gohan needs the biometrics,
    /// and a corpse at the far end of the room is a mission that cannot finish — so shooting
    /// Vance before the escrow is open fails the mission with a reason, the same shape as M52's
    /// two failure paths. That is the one rule this fight has.
    ///
    /// Nothing inside is authored but the floor itself: every position is an offset from where
    /// the crew actually arrives, resolved to walkable floor by <see cref="MazeBank.Nearby"/>.
    /// </summary>
    public sealed class M65ExecutivePrivilege : PreparationOperation
    {
        public const string VanceModel = "a_m_m_business_01";
        /// <summary>Vance's elite detail.</summary>
        public const int Bodyguards = 5;
        /// <summary>How far from the arrival point the boardroom is laid out.</summary>
        public const float RoomSpread = 8f;
        /// <summary>How long the biometric transfer takes.</summary>
        public const int BiometricSeconds = 10;
        /// <summary>He is a colonel behind bulletproof glass, not a passer-by.</summary>
        public const int VanceHealth = 600;
        /// <summary>Where the campaign records the escrow is open.</summary>
        public const string EscrowEvidence = "aegisEscrowOpen";

        /// <summary>How long calling the lift takes, and how near the entrance counts.</summary>
        public const int LiftCallSeconds = 2;
        public const float DoorsRadius = 3.5f;
        /// <summary>
        /// Vance's own relationship group until the escrow is open. In the Aegis group the
        /// brothers' combat logic, which takes the nearest hated man within a hundred and ten
        /// meters, shot him before Gohan reached the terminal and failed the mission for the
        /// player (Ron, September 22).
        /// </summary>
        public const string VanceGroup = "BLOODLINES_VANCE";

        private readonly List<Ped> _detail = new List<Ped>();
        private readonly FloorEntry _entry = new FloorEntry();
        private Ped _vance;
        private Vector3 _arrival, _terminal, _doors;
        private bool _inside, _detailDown, _escrow, _vanceDown;

        public override string Id => "M65";
        public override string Title => "Executive Privilege";
        protected override MissionEndpoint Endpoint => MissionEndpoint.SecuredDelivery;

        /// <summary>The crew is on the executive floor.</summary>
        public bool Inside => _inside;
        /// <summary>The bodyguard detail is down.</summary>
        public bool DetailDown => _detailDown;
        /// <summary>Gohan has the escrow authorizations.</summary>
        public bool Escrow => _escrow;
        /// <summary>Vance is dead.</summary>
        public bool VanceDown => _vanceDown;
        public Ped Vance => _vance;
        public IReadOnlyList<Ped> Detail => _detail;

        /// <summary>The plaza approach only. Nothing inside the tower is authored.</summary>
        protected override string[] FixedSurfaces =>
            new[] { "M65.Start", "M65.IceStart", "M65.GohanStart", "M65.GuessStart", "M65.Doors" };

        protected override bool Setup()
        {
            if (!BeginCrew(CrewSlot.Ice)) return false;
            // The entrance is on the raised plaza deck; one probe puts the lift call on the slab.
            _doors = MissionSites.OnSurface(At("M65.Doors"), MazeBank.PlazaHeadroom, MazeBank.PlazaFloor, Id + " tower entrance", 3);
            Establish("approach", "The man who signed the contracts",
                "Vance is on the executive floor with what is left of his detail. Gohan needs him at his own terminal before anyone shoots him: the escrow opens on his biometrics and on nothing else.");
            return true;
        }

        private Vector3 DoorsPoint() => _doors == Vector3.Zero ? At("M65.Doors") : _doors;

        /// <summary>
        /// The lift. This only asks the access service for the floor; the boardroom is laid out
        /// in <see cref="OnUpdate"/> once the service reports the player standing in it. Reading
        /// his position in this same call gave the plaza, and Vance, his detail and the terminal
        /// were all placed out there while Ice was in the office (Ron, September 22).
        /// </summary>
        private void GoUp() => _entry.Request(Ctx, MazeBank.Office, MazeBank.OfficeIpl);

        /// <summary>Into the boardroom, and the room's layout found rather than written down.</summary>
        private void OnTheFloor()
        {
            _arrival = _entry.Arrival;
            _terminal = MazeBank.Nearby(_arrival, 0.0, RoomSpread, Id + " escrow terminal");
            // Gohan's biometrics and Ice's shot both happen up here, so both brothers come up.
            int n = 0;
            foreach (var hero in Protagonist.All)
            {
                if (hero.Slot == Ctx.Crew.ActiveSlot) continue;
                var spot = MazeBank.BringAlongside(Ctx, hero.Slot, _arrival, 300.0 + 60.0 * n++, Id + " lift");
                if (spot.HasValue) Roles?.For(hero.Slot).Observe(spot.Value, spot.Value);
            }

            var model = new Model(VanceModel);
            if (GameUtils.RequestModel(model))
            {
                var stand = MazeBank.Nearby(_arrival, 180.0, RoomSpread, Id + " Vance");
                _vance = Track(World.CreatePed(model, stand, 0f));
                model.MarkAsNoLongerNeeded();
            }
            if (_vance == null || !_vance.Exists())
            {
                Logger.Error(Id + ": Colonel Vance could not be placed on the executive floor.");
                Fail("The boardroom did not load. Retry the mission.");
                return;
            }
            _vance.IsPersistent = true;
            _vance.BlockPermanentEvents = true;
            _vance.MaxHealth = VanceHealth;
            _vance.Health = VanceHealth;
            // Not Opposition and not Aegis until the escrow is open. Opposition is what the
            // brothers pick targets from and what guard awareness orders into combat, and the
            // Aegis group is what they hate: either one had the AI shoot him before Gohan
            // reached the terminal. A group nobody has set a relationship with is hated by
            // nobody, so the brothers leave him standing. The player shooting him early still
            // fails, below.
            _vance.RelationshipGroup = World.AddRelationshipGroup(VanceGroup);
            // The group keeps the brothers from choosing him, but it does not stop a bullet
            // meant for somebody else: his own detail's crossfire, a brother's burst at the
            // guard beside him, or a grenade could still kill him and fail the mission for
            // something the player did not do (Ron, September 22). Until the escrow is open
            // only the player's own fire can hurt him, so the one failure left is the one
            // the rule is about. Opened() lifts it.
            Function.Call(Hash.SET_ENTITY_ONLY_DAMAGED_BY_PLAYER, _vance, true);
            Blips.Attach(_vance, BlipColor.Red, "Colonel Vance");
            // No RequireAsset on him. That contract fails the mission the moment the entity is
            // dead, and the last stage is Ice killing him - so it failed at the moment of
            // success, the way SM07 once did. The one rule that matters, dead before the
            // biometrics, is enforced in OnUpdate with its own reason.

            for (int i = 0; i < Bodyguards; i++)
            {
                var post = MazeBank.Nearby(_arrival, 120.0 + i * 48.0, RoomSpread * 0.85f, Id + " bodyguard " + (i + 1));
                var ped = EnemyAt(post, "M65 detail post " + (i + 1));
                if (ped != null) _detail.Add(ped);
            }
            if (_detail.Count == 0)
            {
                Logger.Error(Id + ": no bodyguard could be placed on the executive floor.");
                Fail("The boardroom did not load its detail. Retry the mission.");
                return;
            }
            _inside = true;
            Fighting = true;
            Logger.Info(Id + ": the boardroom is at " + _arrival + "; Vance at " + _vance.Position +
                ", terminal at " + _terminal + ", " + _detail.Count + " on the detail.");
        }

        /// <summary>The terminal, or the arrival point before the room has been read.</summary>
        private Vector3 TerminalPoint() => _terminal == Vector3.Zero ? MazeBank.Office : _terminal;

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("To the executive lift",
                new MissionInteraction("Call the lift to the executive floor", DoorsPoint, LiftCallSeconds, DoorsRadius, animation: MissionInteraction.Operate))
                .AnyBrother()
                .OnExit(c => GoUp());

            // The ride is the access service's fade and load; the room is laid out only once
            // it reports the player standing in it.
            yield return new MissionStage("Reach the executive floor",
                new ConditionObjective("Riding the lift to the executive floor", () => _inside))
                .AnyBrother()
                .AfterCues("M65_S1_01_ENEMY");

            yield return new MissionStage("Break the detail",
                new KillTargetsObjective("Take Vance's bodyguard detail", () => _detail))
                .AnyBrother()
                .OnExit(c => _detailDown = true);

            // The escrow first. A dead Vance is a dead end, so the biometrics come before the
            // execution and the failure path below enforces it.
            yield return new MissionStage("Open the escrow",
                new MissionInteraction("Gohan: force Vance's biometrics at the escrow terminal",
                    TerminalPoint, BiometricSeconds, 2.5f, animation: MissionInteraction.Typing)
                { RequiredCharacter = CrewSlot.Gohan })
                .OnExit(c => Opened())
                .AfterCues("M65_S1_03_GOHAN");

            yield return new MissionStage("Finish it",
                new ConditionObjective("Ice: take Vance", () => _vance != null && _vance.Exists() && _vance.IsDead)
                { Marker = () => _vance != null && _vance.Exists() ? _vance.Position : TerminalPoint(), MarkerRadius = 2.5f })
                .OwnedBy(CrewSlot.Ice)
                .OnExit(c => _vanceDown = true)
                .AfterCues("M65_S1_02_ICE");
        }

        private void Opened()
        {
            _escrow = true;
            // Now he is fair game: back in the Aegis group and on the target list, so the
            // brothers' combat logic and guard awareness both treat him as the fight.
            if (_vance != null && _vance.Exists() && !_vance.IsDead)
            {
                _vance.RelationshipGroup = World.AddRelationshipGroup("BLOODLINES_AEGIS");
                Opposition.Add(_vance);
                // And anyone's fire can hurt him again: a brother taking the shot counts.
                Function.Call(Hash.SET_ENTITY_ONLY_DAMAGED_BY_PLAYER, _vance, false);
            }
            Ctx.State?.SetEvidence(EscrowEvidence, EvidenceState.CopyHeld);
            Logger.Info(Id + ": the escrow authorizations are Gohan's.");
        }

        protected override void OnCleanup()
        {
            _entry.Release(Ctx, Protagonist.All.Select(h => Ctx.Crew.PedFor(h.Slot)));
            base.OnCleanup();
        }

        protected override void OnUpdate()
        {
            if (_entry.Update(Ctx)) OnTheFloor();
            if (_entry.Refused) { Fail(_entry.Failure); return; }
            if (Status != MissionStatus.Running) return;
            // The one rule this fight has. Vance dead before the biometrics is a mission that
            // cannot be finished, so it fails now with a reason rather than hanging later.
            if (_inside && !_escrow && _vance != null && _vance.Exists() && _vance.IsDead)
            { Fail("Vance was killed before Gohan had his biometrics. The escrow closes with him."); return; }
            base.OnUpdate();
        }

        protected override void OnPassed()
        {
            if (!_inside || !_detailDown || !_escrow || !_vanceDown)
                throw new InvalidOperationException("The floor, the detail, the escrow and Vance all have to be settled.");
        }
    }
}
