using System;
using System.Globalization;
using System.IO;
using System.Text;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// A floor map of the room around a point, read from the game's own collision
    /// with ray probes and written as text. Nobody can lay out a room from a desk:
    /// the map says where the floor, the walls and the furniture actually are, so
    /// the room's spots (the bed, the wardrobe, the locker, the door) can be placed
    /// on geometry that exists and then surveyed on foot.
    /// </summary>
    public static class InteriorMapper
    {
        public const float Step = 0.5f;

        /// <summary>The map as text: one character per half meter, north up, east right.</summary>
        public static string Map(Vector3 center, float radius, Entity ignore)
        {
            int cells = (int)Math.Ceiling(radius / Step);
            int interior = Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, center.X, center.Y, center.Z);
            var text = new StringBuilder();
            text.AppendLine("Room map around " + Vec(center) + " (interior " + interior + "); one character = " + Step.ToString("0.0", CultureInfo.InvariantCulture) + " m; north (+Y) is up, east (+X) is right.");
            text.AppendLine("'.' walkable floor   'o' knee-to-head high (a bed, a couch, a table)   '|' a wall face or something taller   '#' no floor   ' ' outside this interior   '@' where you stood");
            text.AppendLine("Rows are labeled with Y; the first column is X = " + Num(center.X - cells * Step) + ", the last X = " + Num(center.X + cells * Step) + ".");
            int open = 0;
            for (int row = cells; row >= -cells; row--)
            {
                float y = center.Y + row * Step;
                var line = new StringBuilder(Num(y).PadLeft(8)).Append(' ');
                for (int col = -cells; col <= cells; col++)
                {
                    float x = center.X + col * Step;
                    char mark = Probe(x, y, center.Z, interior, ignore);
                    if (row == 0 && col == 0) mark = '@';
                    if (mark == '.') open++;
                    line.Append(mark);
                }
                text.AppendLine(line.ToString());
            }
            text.AppendLine(open + " walkable cells.");
            AppendProps(text, center, radius);
            return text.ToString();
        }

        private static char Probe(float x, float y, float z, int interior, Entity ignore)
        {
            if (interior != 0 && Function.Call<int>(Hash.GET_INTERIOR_AT_COORDS, x, y, z) != interior) return ' ';
            var floor = World.Raycast(new Vector3(x, y, z + 1.7f), new Vector3(x, y, z - 1.5f), IntersectFlags.Map | IntersectFlags.Objects, ignore);
            if (!floor.DidHit) return '#';
            float height = floor.HitPosition.Z - z;
            if (height < -0.8f) return '#';
            // A wall has no top to land a ray on from inside it; its faces do stop a
            // probe across the cell at chest height.
            var chest = new Vector3(x, y, z + 1.0f);
            for (int side = 0; side < 4; side++)
            {
                var toward = chest + new Vector3(side == 0 ? Step : side == 1 ? -Step : 0f, side == 2 ? Step : side == 3 ? -Step : 0f, 0f);
                if (World.Raycast(chest, toward, IntersectFlags.Map | IntersectFlags.Objects, ignore).DidHit) return '|';
            }
            if (height > 0.9f) return '|';
            if (height > 0.25f) return 'o';
            return '.';
        }

        private static void AppendProps(StringBuilder text, Vector3 center, float radius)
        {
            Prop[] props;
            try { props = World.GetNearbyProps(center, radius); }
            catch (Exception ex) { Logger.Error("Room map: props", ex); return; }
            if (props == null || props.Length == 0) { text.AppendLine("No movable props nearby; the furniture is part of the interior."); return; }
            text.AppendLine(props.Length + " prop(s) nearby (model hash, position):");
            foreach (var prop in props)
                if (prop != null && prop.Exists())
                    text.AppendLine("  " + prop.Model.Hash + "  " + Vec(prop.Position));
        }

        /// <summary>Writes the map beside the ini files and returns the path.</summary>
        public static string Write(string directory, string name, string map)
        {
            string path = Path.Combine(directory ?? "", name);
            File.WriteAllText(path, map, new UTF8Encoding(false));
            return path;
        }

        private static string Num(float value) => value.ToString("0.0", CultureInfo.InvariantCulture);
        private static string Vec(Vector3 v) => Num(v.X) + ", " + Num(v.Y) + ", " + Num(v.Z);
    }
}
