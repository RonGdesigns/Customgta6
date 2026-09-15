using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// What the game's paints actually look like, read out of the game.
    ///
    /// The paint pages used to be a hundred and sixty rows of text — "Ultra Blue",
    /// "Pacific Blue", "Bright Blue" — which tells you nothing about a color. A swatch
    /// does, and the swatch has to be the real one: a table of RGB values typed from
    /// memory would be a hundred and sixty chances to put the wrong blue on a car.
    ///
    /// So the game is asked. Every paint index is set on the car and the rendered color
    /// read straight back, in one pass inside a single frame — nothing is drawn between
    /// the first write and the last, so the car never flickers — and the car's own finish
    /// is captured first and put back in a <c>finally</c>, custom primary included. The
    /// answers are cached for the session; paint indices are the game's, not the car's.
    ///
    /// **It is allowed to fail.** <c>GET_VEHICLE_COLOR</c> reading back what
    /// <c>SET_VEHICLE_COLOURS</c> just wrote is an assumption about the engine, not a
    /// contract, and nobody has watched this run yet. A sweep that comes back with fewer
    /// than <see cref="MinimumDistinct"/> different colors is refused rather than
    /// believed, <see cref="Ready"/> stays false, and the caller draws the list of names
    /// it drew before. A wrong swatch is worse than no swatch: it sells a color the car
    /// will not be painted.
    /// </summary>
    public static class PaintPalette
    {
        /// <summary>
        /// How many different colors a sweep has to come back with before it is believed.
        /// The real palette has well over a hundred; a dozen is far enough below that to
        /// never refuse a good sample, and far enough above one to catch a native that
        /// answers the same thing every time.
        /// </summary>
        public const int MinimumDistinct = 12;

        /// <summary>What a tile shows for a paint that was never sampled.</summary>
        public static readonly Color Unknown = Color.FromArgb(255, 58, 62, 70);

        private static readonly Dictionary<int, Color> Colors = new Dictionary<int, Color>();
        private static bool _attempted, _usable;

        /// <summary>Whether the swatches can be drawn, or the names have to do the work.</summary>
        public static bool Ready => _usable;

        /// <summary>How many paints came back, for a test or a log line.</summary>
        public static int Count => Colors.Count;

        /// <summary>The color of one paint index, or a neutral tile for one we never read.</summary>
        public static Color Of(int index) =>
            Colors.TryGetValue(index, out var color) ? color : Unknown;

        /// <summary>The paint indices the game answered for, in order.</summary>
        public static IEnumerable<int> Indices => Colors.Keys.OrderBy(i => i);

        /// <summary>
        /// Read the palette off this car, once per session. Returns whether the swatches
        /// are usable, so a page can decide between a grid and a list on the spot.
        /// </summary>
        public static bool Sample(Vehicle car)
        {
            if (_attempted) return _usable;
            if (car == null || !car.Exists()) return false;
            _attempted = true;

            var indices = Enum.GetValues(typeof(VehicleColor)).Cast<int>()
                .Where(i => i >= 0).Distinct().OrderBy(i => i).ToList();

            int primary = 0, secondary = 0, red = 0, green = 0, blue = 0;
            bool custom = false;
            try
            {
                var a = new OutputArgument(); var b = new OutputArgument();
                Function.Call(Hash.GET_VEHICLE_COLOURS, car, a, b);
                primary = a.GetResult<int>(); secondary = b.GetResult<int>();
                custom = Function.Call<bool>(Hash.GET_IS_VEHICLE_PRIMARY_COLOUR_CUSTOM, car);
                if (custom)
                {
                    var r = new OutputArgument(); var g = new OutputArgument(); var s = new OutputArgument();
                    Function.Call(Hash.GET_VEHICLE_CUSTOM_PRIMARY_COLOUR, car, r, g, s);
                    red = r.GetResult<int>(); green = g.GetResult<int>(); blue = s.GetResult<int>();
                    // A custom primary wins over the paint index, so the sweep would read
                    // the same custom color a hundred and sixty times.
                    Function.Call(Hash.CLEAR_VEHICLE_CUSTOM_PRIMARY_COLOUR, car);
                }

                foreach (int index in indices)
                {
                    Function.Call(Hash.SET_VEHICLE_COLOURS, car, index, secondary);
                    var cr = new OutputArgument(); var cg = new OutputArgument(); var cb = new OutputArgument();
                    Function.Call(Hash.GET_VEHICLE_COLOR, car, cr, cg, cb);
                    Colors[index] = Color.FromArgb(255,
                        Clamp(cr.GetResult<int>()), Clamp(cg.GetResult<int>()), Clamp(cb.GetResult<int>()));
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Paint palette could not be read: " + ex.Message);
                Colors.Clear();
            }
            finally
            {
                // The car leaves this exactly as it arrived, whatever happened above.
                try
                {
                    Function.Call(Hash.SET_VEHICLE_COLOURS, car, primary, secondary);
                    if (custom) Function.Call(Hash.SET_VEHICLE_CUSTOM_PRIMARY_COLOUR, car, red, green, blue);
                }
                catch (Exception ex) { Logger.Error("Paint palette restore", ex); }
            }

            int distinct = Colors.Values.Select(c => c.ToArgb()).Distinct().Count();
            _usable = distinct >= MinimumDistinct;
            if (_usable) Logger.Info("Paint palette: " + Colors.Count + " paints, " + distinct + " distinct colors.");
            else
            {
                Logger.Warn("Paint palette: only " + distinct + " distinct colors came back from " +
                    Colors.Count + " paints. Showing the names instead of swatches.");
                Colors.Clear();
            }
            return _usable;
        }

        private static int Clamp(int channel) => channel < 0 ? 0 : channel > 255 ? 255 : channel;

        /// <summary>Teardown, so a fresh session reads the palette again.</summary>
        public static void Forget() { Colors.Clear(); _attempted = false; _usable = false; }
    }
}
