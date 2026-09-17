using System.Drawing;
using Bloodlines.Crew;

namespace Bloodlines.Core
{
    /// <summary>
    /// The one color each brother is drawn in, everywhere the interface names him.
    ///
    /// The phone has used these three since it was built - Ice in blue, Guess in amber,
    /// Gohan in green - and the mission HUD, the title card and the results card now use
    /// the same three, so a chip in the corner of the screen means the same man as a row on
    /// the phone. A story test holds the phone's literals to these.
    /// </summary>
    public static class CrewColors
    {
        public static readonly Color Ice = Color.FromArgb(120, 191, 249);
        public static readonly Color Guess = Color.FromArgb(242, 180, 110);
        public static readonly Color Gohan = Color.FromArgb(110, 220, 185);

        public static Color Of(CrewSlot slot) =>
            slot == CrewSlot.Ice ? Ice : slot == CrewSlot.Guess ? Guess : Gohan;

        /// <summary>The same color at another opacity, for a bar or a chip on a dark panel.</summary>
        public static Color Of(CrewSlot slot, int alpha)
        {
            var c = Of(slot);
            return Color.FromArgb(alpha, c.R, c.G, c.B);
        }
    }
}
