using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;

namespace Bloodlines.Missions
{
    /// <summary>
    /// What the Paleto operation considers intact for its one sitting. The
    /// ownership, the named handles, the crew check and the disposal belong to
    /// <see cref="OperationWorld"/>; what is here is the sub, the helicopter, the
    /// boat, the evidence and the structure the whole job stands on.
    ///
    /// The structure is the part that makes this world different from the Port
    /// Heist's. The yacht in Paleto Cove is script-loaded map, so the world owns the
    /// request for the length of the attempt and releases it on every exit. If the
    /// structure goes away mid-operation, people standing on it fall into the sea:
    /// that is a lost requirement, not something to recover from.
    /// </summary>
    public sealed class PaletoWorld : OperationWorld
    {
        /// <summary>
        /// The map files the cove needs. Read out of the installed archives: the
        /// hull, its interior, and the placement fix that puts them at the cove.
        /// All three carry the scripted flag, so none of them exists unasked.
        /// See docs/story-to-play/pass-02/SITE-REPORT.md.
        /// </summary>
        public static readonly string[] YachtMaps =
        {
            "h4_islandx_yacht_03",
            "h4_islandx_yacht_03_int",
            "m26_1_mp2026_01_additions_yachtfix",
        };

        public PaletoWorld(MissionContext context) : base(context)
        {
            Structure = new ScriptedMap(YachtMaps);
        }

        public override string Label => "Paleto operation";

        /// <summary>The yacht, as a request this attempt owns rather than as scenery.</summary>
        public ScriptedMap Structure { get; }
        /// <summary>Set once the sea defenses are actually down, so M45 cannot fly in early.</summary>
        public bool DefensesDown { get; set; }
        /// <summary>Set once the evidence is physically in a brother's hands.</summary>
        public bool EvidenceHeld { get; set; }
        /// <summary>Set once the charges are live. From here the structure is on a clock.</summary>
        public bool ChargesArmed { get; set; }

        /// <summary>
        /// The structure was requested map, not scenery. Whether the operation
        /// passed, failed or was abandoned, the cove goes back to how it was found.
        /// </summary>
        protected override void OnDisposed(bool successful) => Structure.Release();

        public override bool ValidateActive(Mission phase, out string reason)
        {
            if (!CrewReady(out reason)) return false;
            // The structure is required from the first dive to the moment the last
            // brother is off it. After that it is allowed to be gone.
            bool onStructure = !(phase is Campaign.M48TheRoadBackSouth);
            if (onStructure && !Structure.Ready)
            { reason = "The cove structure did not stay loaded. " + RestartNotice; return false; }

            var keys = new List<string>();
            if (phase is Campaign.M44PaletoSubSurface) keys.Add("kraken");
            if (phase is Campaign.M45PaletoBreach) { keys.Add("kraken"); keys.Add("chopper"); }
            if (phase is Campaign.M46PaletoVault) keys.Add("chopper");
            if (phase is Campaign.M47PaletoCollapse) keys.Add("boat");
            if (phase is Campaign.M48TheRoadBackSouth) { keys.Add("boat"); keys.Add("technical"); }
            if (EvidenceHeld) keys.Add("ledger");
            if (!Intact(keys, out reason)) return false;
            reason = null;
            return true;
        }

        public override bool ValidatePhaseEnd(Mission phase, out string reason)
        {
            if (!CrewReady(out reason)) return false;
            var ice = Context.Crew.PedFor(CrewSlot.Ice);
            var gohan = Context.Crew.PedFor(CrewSlot.Gohan);
            var guess = Context.Crew.PedFor(CrewSlot.Guess);

            if (phase is Campaign.M44PaletoSubSurface dive)
            {
                if (!dive.Placed || !DefensesDown || !Seated(gohan, dive.Kraken, VehicleSeat.Driver))
                { reason = "The charges and the sea defenses come first, with Gohan still in the sub."; return false; }
            }
            else if (phase is Campaign.M45PaletoBreach breach)
            {
                // Both brothers physically on the structure, not merely near it.
                if (!breach.Landed || !breach.Aboard || ice == null || gohan == null ||
                    ice.IsInVehicle() || gohan.IsInVehicle() ||
                    ice.Position.Z < Campaign.PaletoSite.DeckFloor || gohan.Position.Z < Campaign.PaletoSite.WaterlineDeck)
                { reason = "Ice and Gohan both have to be aboard before the vault, out of the aircraft and out of the water."; return false; }
            }
            else if (phase is Campaign.M46PaletoVault vault)
            {
                if (!vault.Opened || !EvidenceHeld || !ChargesArmed || Get<Prop>("ledger") == null)
                { reason = "The vault has to be open, the evidence in hand and the charges armed."; return false; }
            }
            else if (phase is Campaign.M47PaletoCollapse collapse)
            {
                var boat = Get<Vehicle>("boat");
                if (!collapse.Jumped || boat == null || !boat.Exists() || !boat.IsDriveable ||
                    !Protagonist.All.All(hero => Context.Crew.PedFor(hero.Slot)?.IsInVehicle(boat) == true))
                { reason = "All three brothers have to be in the boat and clear of the structure."; return false; }
                if (!EvidenceHeld) { reason = "The evidence did not leave with them."; return false; }
            }
            else if (phase is Campaign.M48TheRoadBackSouth road)
            {
                var technical = Get<Vehicle>("technical");
                if (!road.Ashore || !road.Cordon || technical == null || !technical.Exists() || !technical.IsDriveable)
                { reason = "The shore transfer and the outer cordon both have to be behind them."; return false; }
                if (!Protagonist.All.All(hero => Context.Crew.PedFor(hero.Slot)?.IsInVehicle(technical) == true))
                { reason = "Nobody is left behind at the cordon: all three ride south."; return false; }
            }
            reason = null;
            return true;
        }
    }
}
