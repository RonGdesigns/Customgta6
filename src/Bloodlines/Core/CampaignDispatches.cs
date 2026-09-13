using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Missions;
using GTA;

namespace Bloodlines.Core
{
    /// <summary>Authored follow-ups reflect completed jobs, with a persistent read history.</summary>
    public sealed class CampaignDispatches
    {
        public sealed class Message
        {
            public string Mission, Sender, Title, Text;
            public Message(string mission, string sender, string title, string text)
            { Mission = mission; Sender = sender; Title = title; Text = text; }
        }
        public static readonly Message[] All = {
            new Message("M01", "Gohan", "The recording", "The ledger copy survived. So did the recording of all three of us. Don't take tonight's car straight home. I'll find where the next payment goes."),
            new Message("M02", "Ice", "Keep the van quiet", "That van is evidence, not a trophy. Guess, change your route. Gohan, tell us what you find before you decide to handle it alone."),
            new Message("M03", "Guess", "Three keys", "Foundry has room for all three of us. That doesn't settle fifteen years, but it means neither of you has to disappear tonight."),
            new Message("M05", "Gohan", "What Mateo gave us", "I've separated Mateo's claims from what the records prove. We follow the names we can verify. Nobody risks their neck for his version of events."),
            new Message("M06", "Weazel News", "Industrial incident", "Authorities are investigating a violent incident in the industrial district. Residents reported gunfire and vehicles leaving before officers secured the area."),
            new Message("M11", "Guess", "Turbine ready", "The Granger is ready. Bring it by the chop bay if you need repairs. Power won't fix bad judgment, so yes, I expect Ice to say that to me all week."),
            new Message("M15", "Ice", "They lived", "The guards walked away. That's a choice we can still make. The next job doesn't get to erase it just because we're rattled."),
            new Message("M22", "Gohan", "Don't go back", "Cypress is burned. Don't circle the foundry looking for something we forgot. I'm alive. Say you're both alive before we discuss what comes next."),
            new Message("M23", "Guess", "A place to breathe", "Senora bunker isn't home yet. Put your things down anyway. I'm tired of every conversation happening with an engine running."),
            new Message("M27", "Ice", "After the landing", "You got me out. Next time I tell you both the whole plan before the plane leaves the ground. Gohan arranged three Eclipse suites under clean names. The bunker stays our base."),
            new Message("SM03", "KJ", "Still got it", "You can still drive, Guess. Next time we meet, let's keep it to the race. Call me when you get back in one piece."),
            new Message("M29", "Guess", "Enough to stay", "Fuel is in. We can stop measuring every conversation by how long the engine has left. Ice, eat something. Gohan, the equipment can wait ten minutes."),
            new Message("M31", "Ice", "A way back out", "The perimeter gives us warning, not permission to stop paying attention. Guess checked the withdrawal road. If either of you calls it, we leave together."),
            new Message("M33", "Gohan", "Ramos first", "Ramos is out. We don't turn what he survived into another interrogation tonight. Get him somewhere safe; the questions will still be there."),
            new Message("M35", "Guess", "Count the people", "We brought the support back. I'm checking seats before cargo from now on. If I ask twice, answer twice. Humor me."),
            new Message("M38", "Ice", "No shortcuts", "The demolition stock is secured. Gohan checks it before anyone loads it for the next job. We worked too hard getting everyone back to rush the last part."),
            new Message("M41", "Gohan", "The credential", "The card is verified. It gives us access, not certainty. I'll tell you both exactly what it can open before we commit. No more keeping the useful part to myself."),
            new Message("M42", "Guess", "Water under you", "Hearing Gohan answer after that splashdown was enough for me. We can discuss my flying later. Preferably somewhere with a floor that doesn't move."),
            new Message("M43", "Ice", "Before we go", "The staging is recorded. One more count before we leave: people, equipment, way home. The plan only works if all three of us understand it.")
        };
        private readonly CampaignState _state;
        private int _nextAt;
        public CampaignDispatches(CampaignState state) { _state = state; _nextAt = Game.GameTime + 45000; }
        public IEnumerable<Message> Inbox => All.Where(m => _state.IsComplete(m.Mission)).Reverse();
        public void Update(bool available)
        {
            if (!available || Game.GameTime < _nextAt) return;
            var player = Game.Player.Character;
            if (player == null || !player.Exists() || player.IsDead || player.IsInCombat || Game.Player.WantedLevel > 0) return;
            var message = Inbox.FirstOrDefault(m => !_state.ReadDispatches.Contains(m.Mission));
            if (message == null) return;
            GameUtils.Notify("~b~" + message.Sender + "~s~ - " + message.Title + "\n" + message.Text);
            _state.ReadDispatches.Add(message.Mission); _state.Save();
            _nextAt = Game.GameTime + 90000;
        }
    }
}
