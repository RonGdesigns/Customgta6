using System;
using GTA.Math;

namespace Bloodlines.Core
{
    /// <summary>Explicitly wired spawn controls, shared by the editor and mission scripts.</summary>
    public static class MissionPlacement
    {
        public static bool HasGroup(string key) => key == "M05.LightCrew" || key == "M03.DepotGate";
        public static int DefaultCount(string key) => key == "M05.LightCrew" ? 4 : 10;
        public static float DefaultRadius(string key) => key == "M05.LightCrew" ? 6f : 30f;
        public static int ClampCount(int count) => Math.Max(1, Math.Min(16, count));
        public static float ClampRadius(float radius) => float.IsNaN(radius) || float.IsInfinity(radius) ? 6f : Math.Max(1f, Math.Min(60f, radius));
        public static bool HasFormation(MissionLocation location) => location != null && HasGroup(location.Key) && location.SpawnCount > 0;
        public static int Count(LocationBook book, string key, int fallback) => HasFormation(book.Get(key)) ? ClampCount(book.Get(key).SpawnCount) : fallback;
        public static Vector3 GroupPoint(MissionLocation location, int index)
        {
            int count = ClampCount(location.SpawnCount);
            float radius = ClampRadius(location.SpawnRadius) * (float)Math.Sqrt((index + .5f) / count);
            double angle = index * 2.3999632297 + location.Heading * Math.PI / 180;
            return location.Position + new Vector3((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius, 0f);
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
