using System;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using GTA.UI;

namespace Bloodlines.Core
{
    /// <summary>All phone content uses ScriptHookV's external overlay, including text.
    /// Native TextElement/ContainerElement draw underneath CustomSprite artwork.
    /// A fixed alphabet bounds the native texture cache as balances and messages change.</summary>
    public sealed partial class PhoneOverlay
    {
        private readonly string _directory;
        private readonly CustomSprite[] _glyphs = new CustomSprite[95];
        private CustomSprite _pixel;
        private bool _attempted, _ready;
        public PhoneOverlay(string directory) { _directory = directory; }

        // Preflight before any artwork draws. An incomplete install uses a wholly native
        // text-and-rectangle phone, never an opaque overlay with hidden native text.
        public bool TryBegin()
        {
            if (_attempted) return _ready;
            _attempted = true;
            if (string.IsNullOrEmpty(_directory)) return false;
            try
            {
                if (!File.Exists(Path.Combine(_directory, "phone-pixel.png")))
                    throw new FileNotFoundException("phone-pixel.png");
                for (int i = 0; i < _glyphs.Length; i++)
                    if (!File.Exists(Path.Combine(_directory, GlyphName(i + 32))))
                        throw new FileNotFoundException(GlyphName(i + 32));
                _pixel = new CustomSprite(Path.Combine(_directory, "phone-pixel.png"), new SizeF(1, 1), PointF.Empty);
                for (int i = 0; i < _glyphs.Length; i++)
                    _glyphs[i] = new CustomSprite(Path.Combine(_directory, GlyphName(i + 32)), SizeF.Empty, PointF.Empty);
                _ready = true;
            }
            catch (Exception ex) { Logger.Warn("Phone font unavailable; using readable native fallback: " + ex.Message); }
            return _ready;
        }

        public static string GlyphName(int code) => "phone-glyph-" + code.ToString("D3") + ".png";
        public static string Plain(string text) => Regex.Replace(text ?? "", "~[^~]*~", "")
            .Replace('\u2019', '\'').Replace('\u2018', '\'').Replace('\u201c', '"').Replace('\u201d', '"')
            .Replace('\u2013', '-').Replace('\u2014', '-').Replace("\u2026", "...").Replace('\u00a0', ' ');

        // Shared by the runtime and image preview: exact glyphs, sizes, baseline and advances.
        public static void LayoutText(string text, float x, float y, float size, Color color,
            Action<int, float, float, float, float, Color> glyph)
        {
            float origin = x;
            foreach (char ch in Plain(text))
            {
                if (ch == '\r') continue;
                if (ch == '\n') { x = origin; y += 64 * size; continue; }
                int code = ch >= 32 && ch <= 126 ? ch : '?';
                float advance = Advances[code - 32];
                if (code != 32)
                    glyph(code, x - 4 * size, y, ((float)Math.Ceiling(advance) + 8) * size, 64 * size, color);
                x += advance * size;
            }
        }

        public static float MeasureText(string text, float size)
        {
            float width = 0;
            foreach (char ch in text ?? "") width += Advances[(ch >= 32 && ch <= 126 ? ch : '?') - 32] * size;
            return width;
        }

        public void Text(string text, float x, float y, float size, Color color) =>
            LayoutText(text, x, y, size, color, (code, gx, gy, w, h, tint) => Draw(_glyphs[code - 32], gx, gy, w, h, tint));

        public void Box(float x, float y, float width, float height, Color color) => Draw(_pixel, x, y, width, height, color);

        private static void Draw(CustomSprite sprite, float x, float y, float width, float height, Color color)
        {
            sprite.Position = new PointF(x, y);
            sprite.Size = new SizeF(width, height);
            sprite.Color = color;
            sprite.Draw();
        }
    }
}
