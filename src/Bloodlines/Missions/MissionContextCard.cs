using System.Collections.Generic;
using System.Drawing;
using Bloodlines.Core;
using GTA;

namespace Bloodlines.Missions
{
    /// <summary>
    /// The four facts a player who skipped the briefing still needs: the target, why
    /// it matters, who does what, and where to go first. Shown for a few seconds
    /// after a skipped briefing and as a one-line recap on a retry. Presentation
    /// over the existing mission state, not a second quest database.
    /// </summary>
    public static class MissionContextCard
    {
        public sealed class Card
        {
            public string Target = "", Reason = "", Roles = "", Destination = "";
            public string Recap => Target + ". " + Destination;
        }

        public static readonly Dictionary<string, Card> Cards = new Dictionary<string, Card>(System.StringComparer.OrdinalIgnoreCase)
        {
            { "BM01", new Card { Target = "Bring down the Aegis Osprey", Reason = "The last aircraft Aegis has up north is leaving the Paleto Forest bunker", Roles = "Guess drives the gun truck; Ice works the gun; Gohan calls the hull", Destination = "Guess: drive north up the coast highway to the bunker gate" } },
            { "M31", new Card { Target = "Protect the desert refuge", Reason = "Prepare warning and a usable withdrawal road", Roles = "Ice scouts; Gohan arms barriers; Guess proves the escape road", Destination = "Ice: inspect the three marked approaches" } },
            { "M32", new Card { Target = "Two EMP cases at the Zancudo coast post", Reason = "Acquire hardware for the offshore defenses", Roles = "Gohan approaches by water and handles cases; Ice covers; Guess extracts", Destination = "Gohan: pilot the dinghy to the river-mouth landing" } },
            { "M33", new Card { Target = "Ramos at the execution site", Reason = "Rescue the person before asking for intelligence", Roles = "Ice stops the guards; Gohan frees Ramos; Guess picks up all four", Destination = "Ice: approach the yellow observation point" } },
            { "M34", new Card { Target = "Ramos in the evacuation half-track", Reason = "Deliver him alive to medical shelter", Roles = "Guess drives; Ice uses the turret; Gohan escorts and closes road barriers", Destination = "Follow the wind-farm road with Ramos aboard" } },
            { "M36", new Card { Target = "Three seabed survey sensors", Reason = "Map the offshore approach before committing the crew", Roles = "Gohan scans; Guess relocates pickup; Ice watches the cove", Destination = "Gohan: take the sub to the first underwater marker" } },
            { "M37", new Card { Target = "Two crop dusters", Reason = "Prepare a tested smoke screen for extraction", Roles = "Guess flies the first; Ice secures and flies the second; Gohan fits and tests the kits", Destination = "Guess: board the first orange crop duster" } },
            { "M38", new Card { Target = "Four charge packages", Reason = "Acquire the demolition stock the survey requires", Roles = "Ice clears; Gohan carries; Guess positions and drives the carrier", Destination = "Guess: bring the Benson into the loading lane under fire" } },
            { "M39", new Card { Target = "The underwater cable junction", Reason = "Interrupt the rig mainland link without losing the return team", Roles = "Gohan operates the sub cutter; Ice clears shore; Guess holds pickup", Destination = "Gohan: follow the first underwater route marker" } },
            { "M41", new Card { Target = "General Bradley and his access card", Reason = "Remove rig support and bring back the command credential", Roles = "Ice identifies and recovers; Gohan verifies; Guess extracts", Destination = "Ice: use the yellow observation position" } },
            { "M42", new Card { Target = "The Kraken on the Titan's external cradle", Reason = "Deliver Gohan and the sub beyond the watched coast", Roles = "Guess flies and releases; Gohan pilots after splashdown; Ice awaits arrival", Destination = "Guess: fly the offshore ring at 150-350m, then E / D-pad Right" } },
            { "M43", new Card { Target = "The three offshore staging assets", Reason = "Verify the entrance, support and exit before committing", Roles = "Gohan positions sub; Ice positions launch; Guess lands helicopter and checks ledger", Destination = "Gohan: stop the Kraken inside its yellow holding marker" } },
            { "M40", new Card { Target = "Two extraction Tropics", Reason = "Prove the loaded boats can complete the return route", Roles = "Guess fits hull kits; Ice checks weapons; Gohan verifies navigation; both pilots sea-test", Destination = "Guess: prepare the first marked shore kit" } },
            { "M35", new Card { Target = "The convoy gun truck", Reason = "Provide surface cover for the offshore extraction", Roles = "Ice plants charges; Guess blocks the exit; Gohan identifies the truck", Destination = "Ice: plant both marked roadside charges" } },
            { "M07", new Card { Target = "Port manifests at the roof relay", Reason = "Trace the engine shipment before it leaves Paleto", Roles = "Ice fits the sniffer; Guess provides the pickup", Destination = "Ice: reach the marked roof antenna" } },
            { "M08", new Card { Target = "Two turbine crates in Elysian Island", Reason = "Move the engines to a temporary stash", Roles = "Gohan loops cameras; Ice covers; Guess loads and drives", Destination = "Gohan: reach the camera room" } },
            { "M09", new Card { Target = "The convoy transponder", Reason = "Acquire one clearance for a later military checkpoint", Roles = "Guess shadows by helicopter; Ice takes the unit; Gohan reads it", Destination = "Guess: follow the marked rear escort 60-350m away" } },
            { "M10", new Card { Target = "The stashed turbine engines", Reason = "Bring the same cargo to the shop for installation", Roles = "Guess drives the flatbed; Ice handles the aerial threat", Destination = "Guess: check the crates and take the loaded flatbed" } },
            { "M11", new Card { Target = "The Granger turbine installation", Reason = "Turn the recovered engines into an escape vehicle", Roles = "Guess fits mounts; Ice tests pressure; Gohan reads berth intel", Destination = "Guess: work at the marked engine bay" } },
            { "M12", new Card { Target = "The freighter hull", Reason = "Locate a reachable underwater breach", Roles = "Gohan surveys by sub; Ice and Guess coordinate the next steps", Destination = "Gohan: board the sub and follow the submerged markers" } },
            { "M13", new Card { Target = "The harbor fuel barges", Reason = "Reduce the patrol response before the heist", Roles = "Ice plants charges; Guess waits at extraction", Destination = "Ice: board the water scooter; avoid the workers" } },
            { "M14", new Card { Target = "A jammer pod on the Besra", Reason = "Prepare a limited radar window for the lift", Roles = "Ice clears the apron; Guess flies; Gohan leaves by road", Destination = "Ice: clear the marked apron guards" } },
            { "M15", new Card { Target = "The harbor lock-gate cable", Reason = "Prepare access without killing the watchmen", Roles = "Gohan splices; Ice stuns; Guess waits at the exit", Destination = "Gohan: reach the maintenance access" } },
            { "M16", new Card { Target = "The military Cargobob", Reason = "Acquire the lift aircraft; the IFF clearance works once", Roles = "Ice clears and boards; Guess flies; Gohan drives out", Destination = "Ice: cross the outer yard using the transponder" } },
            { "M17", new Card { Target = "The survey submarine", Reason = "Reinforce the hull and fit a usable outside release", Roles = "Gohan reinforces; Guess tests the lock; Ice assists", Destination = "Gohan: work at each marked hull station" } },
            { "M18", new Card { Target = "The harbor preparations", Reason = "Put the sub, lift and hauler in position", Roles = "Gohan stages the sub; Guess stages the lift; Ice loads the hauler", Destination = "Stage the vehicles; SM01-SM03 must finish before M19" } },
            { "M23", new Card { Target = "The Grand Senora bunker", Reason = "Replace the lost foundry with a desert base", Roles = "Guess drives the crew in; Ice secures the entrance; Guess checks the yard; Gohan powers access and inspects inside", Destination = "Guess: drive the crew to the bunker approach" } },
            { "M24", new Card { Target = "The bullion beneath Alamo Sea", Reason = "Recover the buried score under pressure", Roles = "Guess works the cable and drives; Ice defeats the response", Destination = "Guess: park the recovery truck at the shoreline marker" } },
            { "M25", new Card { Target = "The bridge fuel tanker", Reason = "Break the pursuit and leave by water", Roles = "Ice destroys the tanker and holds the bridge", Destination = "Ice: reach the bridge and fire from a safe distance" } },
            { "M26", new Card { Target = "The airfield spotter response", Reason = "Clear McKenzie airspace and recover the fighter", Roles = "Guess pilots the Lazer; follow the radio identification", Destination = "Guess: board the marked Lazer; identify targets before firing" } },
            { "M27", new Card { Target = "The ledger aboard the Shamal", Reason = "Recover the record that advances the investigation", Roles = "Guess flies the Duster; Ice transfers; Gohan waits in a dinghy", Destination = "Guess: board the Duster and hold 12-60m from the Shamal" } },
            { "M28", new Card { Target = "The northern relay", Reason = "Prepare the next operation with a working splice", Roles = "Gohan splices; Ice covers; Guess extracts in the Granger", Destination = "Gohan: approach the marked relay" } },
            { "M29", new Card { Target = "The rail depot fuel tanker", Reason = "Stock the bunker for the next operation", Roles = "Ice clears and opens the valve; Guess drives the tanker", Destination = "Survey the transfer depot, then clear its guards as Ice" } },
            { "M30", new Card { Target = "The satellite parts in the Dubsta", Reason = "Deliver the equipment needed for the next preparations", Roles = "Guess drives; Ice and Gohan ride and cover", Destination = "Guess: take the loaded Dubsta through both canyon markers" } },
            { "SM01", new Card { Target = "Sergei's codes and two crates", Reason = "Establish an ammunition supply for Ice", Roles = "Ice works alone; Guess and Gohan stay on the radio", Destination = "Ice: clear the guards; keep Sergei alive" } },
            { "SM02", new Card { Target = "The municipal camera archive", Reason = "Acquire access before IT traces the intrusion", Roles = "Gohan works alone; stun both guards without killing", Destination = "Gohan: reach the roof access" } },
            { "SM03", new Card { Target = "KJ's race and the prize coupe", Reason = "Earn cash and a race transmission for the shop", Roles = "Guess races; KJ organizes and warns about the rivals", Destination = "Guess: board the coupe and win the three-lap circuit" } },
            { "SM04", new Card { Target = "The quarry marksmen and their radios", Reason = "Collect communications for the next operation", Roles = "Ice clears both nests and collects each radio", Destination = "Ice: reach the quarry overlook" } },
            { "SM05", new Card { Target = "The estuary buoy service harness", Reason = "Install an interceptor for water-route telemetry", Roles = "Gohan travels by dinghy and works in the water", Destination = "Gohan: take the dinghy to the marked buoy" } },
            { "SM06", new Card { Target = "The aviation-fuel tanker", Reason = "Supply McKenzie while protecting the trailer", Roles = "Guess drives the Phantom and attached tanker", Destination = "Guess: board the Phantom and follow the canyon route" } },
            { "M19", new Card { Target = "The Port Heist: the bullion beneath the hull", Reason = "The preparations opened one route through the harbor", Roles = "Gohan cuts and floats the load; Ice covers the quay; Guess flies the lift", Destination = "Start the underwater breach. The lift and escort follow in this operation" } },
            { "M20", new Card { Target = "The surfaced bullion", Reason = "The breach is complete; the load must leave the harbor", Roles = "Ice clears the quay, Guess hooks the load, Gohan brings the sub to the launch", Destination = "Resume Sky Hook from its phase start" } },
            { "M21", new Card { Target = "The loaded helicopter and escort", Reason = "Protect the cargo until it clears the coast", Roles = "Gohan drives the launch, Ice covers him, Guess flies the bullion", Destination = "Clear the breakwater, then transfer the water team to the road pickup" } },
            { "M22", new Card { Target = "The Alamo fallback", Reason = "Conceal the load and regroup before treating it as a score", Roles = "Guess flies inland; Gohan drives the Granger with Ice", Destination = "Resume the inland phase, drop the bullion, land and meet on foot" } },
            { "M01", new Card { Target = "Mateo's ledger and the prototype at Terminal Island", Reason = "Three separate jobs on one dock; nobody knows the others are there", Roles = "Ice: lookout and identification. Gohan: the ledger copy. Guess: the four-door", Destination = "Each brother's own approach" } },
            { "M02", new Card { Target = "The Aegis van carrying the dock recording", Reason = "The recording puts all three faces on one screen", Roles = "Guess drives. Gohan cuts the upload from the passenger seat. Ice takes the drives", Destination = "Follow the van's route" } },
            { "M03", new Card { Target = "The depot's weapons and the Benson", Reason = "Cypress Foundry is empty; the base needs armor and equipment", Roles = "Guess: delay the rail response. Ice: clear the depot. Gohan: load the truck", Destination = "Guess to the rail junction; Ice and Gohan to the depot" } },
            { "M04", new Card { Target = "Miller's copy of the dock forensics", Reason = "He is selling it to an Aegis buyer tonight", Roles = "Gohan: the surface-lot breaker. Ice: cover the exchange. Guess: the exit, then the chase", Destination = "Guess: drive to the Pillbox lot; Ice and Gohan approach separately" } },
            { "M05", new Card { Target = "Mateo, alive", Reason = "He can say who paid for the dock setup", Roles = "Ice: cliff cover. Guess and Gohan: the boat", Destination = "The Palomino cove" } },
            { "M06", new Card { Target = "The LSPD depot's backup of the dock recording", Reason = "A second copy exists; the van was not the only one", Roles = "Gohan: the feeder and the racks. Ice: hold the alley. Guess: the Granger", Destination = "The Vespucci canal depot" } },
        };

        private static string _missionId;
        private static int _until;
        private static bool _recap;

        public static bool IsShowing => Game.GameTime < _until && _missionId != null;

        /// <summary>Show the full card (after a skipped briefing) or the one-line recap (on a retry).</summary>
        public static void Show(string missionId, bool recap, int ms = 7000)
        {
            if (missionId == null || !Cards.ContainsKey(missionId)) { _missionId = null; return; }
            _missionId = missionId; _recap = recap; _until = Game.GameTime + ms;
            Logger.Info("Context card (" + (recap ? "recap" : "full") + ") for " + missionId + ".");
        }

        public static void Clear() { _missionId = null; _until = 0; }

        /// <summary>Draw for this frame while active.</summary>
        public static void Draw()
        {
            if (!IsShowing) return;
            var card = Cards[_missionId];
            if (_recap)
            {
                new GTA.UI.TextElement(_missionId + " — " + card.Recap, new PointF(640f, 80f), 0.38f, Color.White) { Alignment = GTA.UI.Alignment.Center }.Draw();
                return;
            }
            var lines = new List<string>();
            foreach (string row in new[] { "TARGET  " + card.Target, "WHY  " + card.Reason, "ROLES  " + card.Roles, "FIRST  " + card.Destination })
                lines.AddRange(Wrap(row, 84));
            new GTA.UI.ContainerElement(new PointF(245f, 68f), new SizeF(790f, lines.Count * 23f + 24f), Color.FromArgb(170, 0, 0, 0)).Draw();
            for (int i = 0; i < lines.Count; i++)
                new GTA.UI.TextElement(lines[i], new PointF(270f, 80f + i * 23f), 0.34f, Color.White).Draw();
        }

        private static IEnumerable<string> Wrap(string text, int width)
        {
            string line = "";
            foreach (string word in text.Split(' '))
            {
                if (line.Length > 0 && line.Length + word.Length + 1 > width) { yield return line; line = ""; }
                line += (line.Length > 0 ? " " : "") + word;
            }
            if (line.Length > 0) yield return line;
        }
    }
}
