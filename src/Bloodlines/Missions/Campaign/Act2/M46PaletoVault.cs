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
    /// Chapter three. Gohan opens the command vault with Bradley's card, the two of
    /// them take the escrow ledger and the bonds, and Gohan arms the charges before
    /// they leave. The evidence is the point: the bonds are weight, the ledger is
    /// what makes the people who paid for this afraid.
    ///
    /// What the vault physically is: the vessel's own study, off the interior's main
    /// deck, reached by its stairs. Its rooms are real and furnished — the archive
    /// read found a bridge, a study and 446 fixed objects inside — but their exact
    /// standing positions need a live capture, so every point here is a location key.
    /// </summary>
    public sealed class M46PaletoVault : ComposedMission
    {
        public const string LedgerModel = "prop_cash_case_01";
        public const string BondsModel = "prop_cash_pile_01";
        public const int CardSeconds = 4;
        public const int TimerSeconds = 5;

        private Prop _ledger;
        private Prop _bonds;
        private bool _opened;
        private bool _armed;
        /// <summary>
        /// Guess is still flying the Annihilator while Gohan and Ice are inside. M45 held it
        /// in a circuit every frame and nothing carried that over, so the moment this
        /// chapter began nobody was flying it (the September 22 audit). Same hold, same
        /// circuit, released the instant the player takes it back.
        /// </summary>
        private readonly AircraftHold _hold = new AircraftHold();
        private Vehicle _chopper;
        /// <summary>Whether the vessel's interior has answered as loaded; asked every frame until it does.</summary>
        private bool _inside;

        public override string Id => "M46";
        public override string Title => "Paleto Deep-Sea: Vault Crack";
        protected override MissionEndpoint Endpoint => MissionEndpoint.ContinuousNext;

        public Prop Ledger => _ledger;
        public Prop Bonds => _bonds;
        /// <summary>The vault door is actually open, by the card rather than by a timer running out.</summary>
        public bool Opened => _opened;
        public bool Armed => _armed;

        private Vector3 At(string key) => Ctx.Locations.Position(key);

        protected override bool Setup()
        {
            var world = Paleto.Of(Ctx);
            if (world != null && !world.Structure.Ready)
                throw new InvalidOperationException("M46 opened without the structure loaded.");
            // Bradley's card is what opens this door. M43 will not sign the staging
            // off without it, so a real run always has it; a chapter opened alone in
            // QA is allowed to proceed and say so rather than refusing to load.
            //
            // The operation itself now refuses to start without the card (PaletoOperation),
            // so a sitting can no longer play two chapters and then stop here. This remains
            // as the backstop, and says why rather than throwing (the September 22 audit).
            if (Ctx.State != null && Ctx.State.EvidenceOf("bradleyKeycard") != EvidenceState.CopyHeld)
            {
                if (world != null)
                {
                    GameUtils.Notify("~r~The vault needs Bradley's card. Complete The General's Wire first.");
                    Logger.Error("M46 reached inside the operation without Bradley's card.");
                    return false;
                }
                Logger.Warn("M46 opened on its own without Bradley's card; the reader step will not represent a real entry.");
            }
            if (!Paleto.IsContinuing(Ctx) && !Ctx.Crew.Deploy(CrewSlot.Gohan, At("M46.Stairs"), Ctx.Locations.Heading("M46.Stairs"))) return false;

            // Opened alone in QA there is no M45 before this to have asked for it. The
            // answer used to be ignored; it is asked again every frame until the interior
            // is in, and a sitting that arrives without it says so (the September 22 audit).
            _inside = Paleto.EnsureInside(Ctx);
            if (!_inside && world != null)
            {
                Logger.Warn(Id + ": the vessel interior is not ready as the vault chapter begins; asking again every frame.");
                Ctx.Doctor?.Warn("interior", "Paleto vessel", "the vessel's interior was not loaded when the vault chapter began.");
            }
            _chopper = world?.Get<Vehicle>("chopper");
            _ledger = Track(Equipment(LedgerModel, "M46.Ledger"));
            _bonds = Track(Equipment(BondsModel, "M46.Bonds"));
            if (!RequireAssets(_ledger, _bonds)) return false;
            RequireAsset(_ledger, "The escrow ledger was destroyed. Without it the operation proves nothing.");

            // The two shelf points carry real props, so they are reviewed like every
            // other interaction: without them in the contract Ron cannot move a case
            // that is standing inside a bulkhead, which is what M43's laptop did.
            Paleto.Review(Ctx, PlacementContract.Ped("M46.Stairs"), PlacementContract.Ped("M46.Bridge"),
                PlacementContract.Interaction("M46.Vault"), PlacementContract.Interaction("M46.Console"),
                PlacementContract.Interaction("M46.Ledger"), PlacementContract.Interaction("M46.Bonds"));
            Station(CrewSlot.Gohan, At("M46.Stairs"));
            Station(CrewSlot.Ice, At("M46.Bridge"));
            return true;
        }

        /// <summary>A fixed prop at a location key, placed on the deck it belongs to.</summary>
        private Prop Equipment(string modelName, string key)
        {
            var model = new Model(modelName);
            if (!GameUtils.RequestModel(model)) return null;
            var prop = World.CreateProp(model, At(key), false, false);
            model.MarkAsNoLongerNeeded();
            if (prop == null || !prop.Exists()) return null;
            prop.IsPositionFrozen = true;
            prop.IsPersistent = true;
            return prop;
        }

        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Reach the command deck",
                new ReachZoneObjective("Gohan: take the stairs to the command deck", () => At("M46.Bridge"), 4f))
                .OwnedBy(CrewSlot.Gohan);

            yield return new MissionStage("Use Bradley's card on the vault",
                new MissionInteraction("Gohan: hold Bradley's card against the vault reader", () => At("M46.Vault"), CardSeconds, 3f,
                    animation: MissionInteraction.Operate))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(c => _opened = true);

            yield return new MissionStage("Take the ledger and the bonds",
                new MultiHoldObjective("Ice and Gohan: clear the vault shelves", new[] { At("M46.Ledger"), At("M46.Bonds") }, 3, 2.5f, "Packing") { Animation = MissionInteraction.ReachInside })
                .AnyOf()
                .OnExit(c =>
                {
                    var world = Paleto.Of(c);
                    if (world != null)
                    {
                        world.Bind("ledger", _ledger);
                        world.EvidenceHeld = true;
                    }
                    // Provisional until the operation actually finishes. The attempt is
                    // still open; nothing about this commits to the save yet.
                    c.State?.SetEvidence("aegisEscrowLedger", EvidenceState.CopyHeld);
                })
                .AfterCues("M46_S1_01_GOHAN");

            yield return new MissionStage("Arm the charges",
                new MissionInteraction("Gohan: arm the seismic charges from the command console", () => At("M46.Console"), TimerSeconds, 3f,
                    animation: MissionInteraction.Typing))
                .OwnedBy(CrewSlot.Gohan)
                .OnExit(c =>
                {
                    _armed = true;
                    var world = Paleto.Of(c);
                    if (world != null) world.ChargesArmed = true;
                    Logger.Info("Paleto: charges armed from the command console. The structure is on a clock from here.");
                })
                .AfterCues("M46_S1_02_ICE", "M46_S1_03_GUESS");
        }

        protected override void OnUpdate()
        {
            if (!_inside) _inside = Paleto.EnsureInside(Ctx);
            // Every frame, the way M45 does it: the hold keeps its own cadence and does
            // nothing while the player is flying the aircraft himself.
            if (_chopper != null && _chopper.Exists())
                _hold.Update(Ctx.Crew, CrewSlot.Guess, _chopper, At("M45.Hold"), (int)At("M45.Hold").Z);
            base.OnUpdate();
        }

        protected override void OnPassed()
        {
            if (!_opened || !_armed) throw new InvalidOperationException("The vault was not opened and armed.");
            var world = Paleto.Of(Ctx);
            if (world != null && !world.EvidenceHeld) throw new InvalidOperationException("The evidence was not taken.");
            var record = OperationHandoff.Capture(Paleto.Operation.Title, Id, "M47", Ctx.Crew, null);
            record.Notes["evidence"] = "escrow ledger in hand, bonds packed";
            record.Notes["charges"] = "armed from the command console";
            Ctx.Handoffs.Record(record);
        }
    }
}
