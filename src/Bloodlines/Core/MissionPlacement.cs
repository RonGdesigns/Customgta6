using System;
using System.Collections.Generic;
using GTA.Math;

namespace Bloodlines.Core
{
    /// <summary>Explicitly wired spawn controls, shared by the editor and mission scripts.</summary>
    public static class MissionPlacement
    {
        /// <summary>
        /// A key that carries a detail rather than one man: how many stand there, and how
        /// wide the editor may spread them. A radius of zero means the mission lays its own
        /// men out and only the count is ours to set.
        /// </summary>
        public struct Group
        {
            public int Count;
            public float Radius;
            public Group(int count, float radius) { Count = count; Radius = radius; }
        }

        /// <summary>
        /// Every key whose detail the placement editor can size, with what the mission uses
        /// until somebody sizes it.
        ///
        /// **This table is the contract, and it used to be two names in an expression.**
        /// Ron went to set the wave sizes in M60, found the enemy-count row answering "not
        /// a group", and said the obvious thing: he had never seen anything that *was* in a
        /// group. Two keys out of one thousand and ninety-one were, because the test was
        /// <c>key == "M05.LightCrew" || key == "M03.DepotGate"</c> and nothing ever added a
        /// third. One table can be added to; an expression buried in a predicate is found
        /// only by the person it fails in game.
        ///
        /// A key belongs here when a mission reads its size through <see cref="Count"/>,
        /// and a story test holds the two lists against each other so the next detail
        /// cannot be written into a mission and forgotten here.
        ///
        /// **A declared key changes nothing until it is edited.** An unedited key has no
        /// count at all, so <see cref="HasFormation"/> is false, the mission falls back to
        /// the constant it was authored with, and the numbers below are only what the
        /// editor offers as a starting point.
        /// </summary>
        private static readonly Dictionary<string, Group> Groups =
            new Dictionary<string, Group>(StringComparer.OrdinalIgnoreCase)
        {
            // The two that were wired by hand. Unchanged, including their defaults.
            { "M03.DepotGate", new Group(10, 30f) },
            { "M05.LightCrew", new Group(4, 6f) },
            // Three waves onto the Davis block and three onto the LSIA apron. A wave is the
            // plainest case there is for an editable size: how many arrive is its whole shape.
            { "M60.Wave1", new Group(5, 7f) },
            { "M60.Wave2", new Group(5, 7f) },
            { "M60.Wave3", new Group(5, 7f) },
            { "M70.Wave1", new Group(6, 8f) },
            { "M70.Wave2", new Group(6, 8f) },
            { "M70.Wave3", new Group(6, 8f) },
            // Two to a nest on the Maze Bank plaza deck.
            { "M63.Nest1", new Group(2, 3f) },
            { "M63.Nest2", new Group(2, 3f) },
            { "M63.Nest3", new Group(2, 3f) },
            // The Paleto deck detail stands in ranks across the beam and probes every post
            // for a deckhead over it, because a downward probe cannot tell a deck from the
            // cabin floor under it. That layout is the mission's and a circle would undo it,
            // so the count is editable here and the spread is not.
            { "M45.Deck", new Group(10, 0f) },
        };

        public static bool HasGroup(string key) => key != null && Groups.ContainsKey(key);
        /// <summary>Whether the spread is the editor's to set, or the mission's own business.</summary>
        public static bool HasRadius(string key)
        {
            Group group;
            return key != null && Groups.TryGetValue(key, out group) && group.Radius > 0f;
        }
        /// <summary>Every declared key, for the report and for the test that checks this table.</summary>
        public static IEnumerable<string> Declared => Groups.Keys;
        public static int DefaultCount(string key)
        {
            Group group;
            return key != null && Groups.TryGetValue(key, out group) ? group.Count : 1;
        }
        public static float DefaultRadius(string key)
        {
            Group group;
            return key != null && Groups.TryGetValue(key, out group) && group.Radius > 0f ? group.Radius : 6f;
        }
        public static int ClampCount(int count) => Math.Max(1, Math.Min(16, count));
        public static float ClampRadius(float radius) => float.IsNaN(radius) || float.IsInfinity(radius) ? 6f : Math.Max(1f, Math.Min(60f, radius));
        public static bool HasFormation(MissionLocation location) => location != null && HasGroup(location.Key) && location.SpawnCount > 0;
        public static int Count(LocationBook book, string key, int fallback) => HasFormation(book?.Get(key)) ? ClampCount(book.Get(key).SpawnCount) : fallback;
        /// <summary>
        /// Where the nth man of a detail stands: the edited formation when the key has one
        /// whose spread is ours, and the position the mission authored when it does not. A
        /// detail keeps exactly the shape it was written with until he edits the key.
        /// </summary>
        public static Vector3 PointFor(LocationBook book, string key, int index, Vector3 authored)
        {
            var formation = book?.Get(key);
            return HasRadius(key) && HasFormation(formation) ? GroupPoint(formation, index) : authored;
        }
        public static Vector3 GroupPoint(MissionLocation location, int index) =>
            GroupPoint(location.Position, location.Heading, index, ClampCount(location.SpawnCount), ClampRadius(location.SpawnRadius));
        /// <summary>
        /// A sunflower spiral out from the point, which fills a circle evenly at any count.
        /// The alternative every mission reached for on its own - index times a spacing -
        /// puts a detail in a diagonal line, which is a queue rather than a position.
        /// </summary>
        public static Vector3 GroupPoint(Vector3 center, float heading, int index, int count, float radius)
        {
            count = ClampCount(count);
            float reach = ClampRadius(radius) * (float)Math.Sqrt((Math.Max(0, index) + .5f) / count);
            double angle = index * 2.3999632297 + heading * Math.PI / 180;
            return center + new Vector3((float)Math.Cos(angle) * reach, (float)Math.Sin(angle) * reach, 0f);
        }
        public static Vector3 Position(LocationBook book, string key, Vector3 fallback)
        {
            var point = book.Get(key);
            return point != null && point.Status == LocationStatus.Surveyed ? point.Position : fallback;
        }
        public static float Heading(LocationBook book, string key, float fallback)
        {
            var point = book.Get(key);
            return point != null && point.Status == LocationStatus.Surveyed ? point.Heading : fallback;
        }
        public static int TravelTimeout(LocationBook book, string entry, string destination, Vector3 from, Vector3 to)
        {
            if (book.Get(entry)?.Status != LocationStatus.Surveyed && book.Get(destination)?.Status != LocationStatus.Surveyed) return 15000;
            return (int)Math.Max(15000,Math.Min(120000,10000+from.DistanceTo(to)/12f*1000f));
        }
        public static void AddEditorLocations(LocationBook book)
        {
            for (int i = 1; i <= 3; i++)
            {
                book.AddEditable("M03.ArrivalSpawn" + i, "M03.DepotGate", new Vector3(0, -60 - (i - 1) * 18, 0), "Vehicle entry " + i);
                book.AddEditable("M03.ArrivalStop" + i, "M03.DepotGate", new Vector3(0, -8 - (i - 1) * 7, 0), "Vehicle destination " + i);
                book.AddEditable("M03.Crate" + i, "M03.HaulerSpawn", new Vector3(3 + (i - 2) * 1.2f, -5, 0), "Prop: cargo crate " + i);
            }
            book.AddEditable("M11.Workbench", "M11.DynoPad", new Vector3(0, 3, 0), "Prop: motor-mount workbench");
            if (book.Get("M11.Workbench") != null) book.Get("M11.Workbench").Heading = 0f;
            for (int i = 1; i <= 4; i++)
            {
                string anchor = i == 4 ? "M17.ReleasePoint" : "M17.Weld" + new[] { "One", "Two", "Three" }[i - 1];
                book.AddEditable("M17.Workbench" + i, anchor, new Vector3(-1.5f, 0, 0), "Prop: dock workbench " + i);
                if (book.Get("M17.Workbench" + i) != null) book.Get("M17.Workbench" + i).Heading = 0f;
            }
            var alley = book.Get("M06.AlleyHold");
            if (alley == null) return;
            var axis = new Vector3(-10f, 7f, 0f) * (1f / (float)Math.Sqrt(149));
            for (int i = 1; i <= 2; i++)
            {
                float sign = i == 1 ? 1f : -1f;
                book.AddEditable("M06.ConvoySpawn" + i, "M06.AlleyHold", axis * (sign * 85f), "Vehicle entry: SWAT convoy " + i);
                book.AddEditable("M06.ConvoyStop" + i, "M06.AlleyHold", axis * (sign * 14f), "Vehicle destination: SWAT convoy " + i);
            }
        }
        public static string Label(MissionLocation location)
        {
            switch (location.Key)
            {
                case "M01.IceApproach": return "Player: Ice starting position";
                case "M01.GohanApproach": return "Player: Gohan starting position";
                case "M01.GuessApproach": return "Player: Guess starting position";
                case "M05.CliffPerch": return "Player: Ice cliff start";
                case "M05.DinghySpawn": return "Players / vehicle: Guess and Gohan dinghy";
                case "M05.GrottoMouth": return "Enemy / vehicle: Mateo boat";
                case "M05.LightCrew": return "Enemies: generator crew";
                case "M03.DepotGate": return "Enemies / objective: depot yard";
                case "M06.GrangerSpawn": return "Player / vehicle: Guess Granger";
                case "M06.ServerRacks": return "Props / objective: server bench";
            }
            if (location.IsEditorSlot) return location.DistrictHint;
            string name = location.Key.Substring(location.Key.IndexOf('.') + 1);
            return name + " (" + location.Kind + ")";
        }
    }
}
