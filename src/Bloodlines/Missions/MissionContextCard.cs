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
            { "M01", new Card { Target = "Mateo's ledger and the prototype at Terminal Island", Reason = "Three separate jobs on one dock; nobody knows the others are there", Roles = "Ice: lookout and identification. Gohan: the ledger copy. Guess: the four-door", Destination = "Each brother's own approach" } },
            { "M02", new Card { Target = "The Aegis van carrying the dock recording", Reason = "The recording puts all three faces on one screen", Roles = "Guess drives. Gohan cuts the upload from the passenger seat. Ice takes the drives", Destination = "Follow the van's route" } },
            { "M03", new Card { Target = "The depot's weapons and the Benson", Reason = "Cypress Foundry is empty; the base needs armor and equipment", Roles = "Guess: delay the rail response. Ice: clear the depot. Gohan: load the truck", Destination = "Guess to the rail junction; Ice and Gohan to the depot" } },
            { "M04", new Card { Target = "Miller's copy of the dock forensics", Reason = "He is selling it to an Aegis buyer tonight", Roles = "Gohan: the surface-lot breaker. Ice: cover the exchange. Guess: the exit, then the chase", Destination = "Drive the crew to the Pillbox Hill lot" } },
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
            new GTA.UI.ContainerElement(new PointF(640f, 120f), new SizeF(780f, 118f), Color.FromArgb(170, 0, 0, 0)).Draw();
            new GTA.UI.TextElement("TARGET  " + card.Target, new PointF(270f, 84f), 0.36f, Color.White).Draw();
            new GTA.UI.TextElement("WHY  " + card.Reason, new PointF(270f, 110f), 0.36f, Color.White).Draw();
            new GTA.UI.TextElement("ROLES  " + card.Roles, new PointF(270f, 136f), 0.36f, Color.White).Draw();
            new GTA.UI.TextElement("FIRST  " + card.Destination, new PointF(270f, 162f), 0.36f, Color.White).Draw();
        }
    }
}
