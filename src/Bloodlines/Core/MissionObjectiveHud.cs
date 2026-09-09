using System;
using System.Collections.Generic;
using System.Drawing;
namespace Bloodlines.Core
{
    public static class MissionObjectiveHud
    {
        public static IEnumerable<string> Wrap(string text, int width = 68)
        {
            text = text ?? "";
            while (text.Length > width)
            {
                int split = text.LastIndexOf(' ', width); if (split < 1) split = width;
                yield return text.Substring(0, split); text = text.Substring(split).TrimStart();
            }
            if (text.Length > 0) yield return text;
        }
        public static void Draw(string text)
        {
            float y = 98;
            foreach (var line in Wrap(text))
            { new GTA.UI.TextElement(line, new PointF(24, y), .29f, Color.Yellow).Draw(); y += 23; if (y > 167) break; }
        }
    }
}
