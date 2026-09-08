using System;
using System.Collections.Generic;
using System.IO;
using GTA;
using GTA.Math;

namespace Bloodlines.Core
{
    /// <summary>Where a coordinate came from, and therefore how much to trust it.</summary>
    public enum LocationStatus
    {
        /// <summary>Hand-placed guess. Right district at best; survey before judging a mission.</summary>
        Estimate,

        /// <summary>Moved to the centre of the correct district by the audit tool.</summary>
        ZoneCentre,

        /// <summary>Captured in game with the dev tools.</summary>
        Surveyed,

        /// <summary>From the bible's own coordinate index, or the implementation toolkit.</summary>
        Bible
    }

    public sealed class MissionLocation
    {
        public string Key { get; set; }
        public Vector3 Position { get; set; }
        public float Heading { get; set; }
        public string Kind { get; set; }
        public LocationStatus Status { get; set; }
        public string DistrictHint { get; set; }
    }

    /// <summary>
    /// Every world coordinate the campaign uses, loaded from data/locations.tsv and
    /// overridable per install from Bloodlines.Locations.ini.
    ///
    /// The data file carries each coordinate's provenance, because that is the thing
    /// that actually matters here: a handful come from the bible's surveyed index,
    /// the rest are estimates placed in roughly the right district and verified only
    /// as far as `tools/validate_locations.py` can check them — which is district, not
    /// accuracy. Survey them in game (dev menu → Survey) and their status changes to
    /// Surveyed as they are captured.
    /// </summary>
    public sealed class LocationBook
    {
        private readonly Dictionary<string, MissionLocation> _locations =
            new Dictionary<string, MissionLocation>(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<MissionLocation> All => _locations.Values;

        public int Count => _locations.Count;

        public static LocationBook Load(string dataDirectory, string overridesPath)
        {
            var book = new LocationBook();

            foreach (var row in DataTable.Load(Path.Combine(dataDirectory, "locations.tsv")).Rows)
            {
                string key = row.Text("key");
                if (string.IsNullOrEmpty(key)) continue;

                book._locations[key] = new MissionLocation
                {
                    Key = key,
                    Position = new Vector3(row.Float("x"), row.Float("y"), row.Float("z")),
                    Heading = row.Float("heading"),
                    Kind = string.IsNullOrEmpty(row.Text("kind")) ? "land" : row.Text("kind"),
                    Status = ParseStatus(row.Text("status")),
                    DistrictHint = row.Text("district_hint")
                };
            }

            // Per-install overrides win: someone who has surveyed a position should not
            // lose it to a data-file update.
            var settings = ScriptSettings.Load(overridesPath);
            foreach (var location in book._locations.Values)
            {
                float x = settings.GetValue<float>("Positions", location.Key + ".X", location.Position.X);
                float y = settings.GetValue<float>("Positions", location.Key + ".Y", location.Position.Y);
                float z = settings.GetValue<float>("Positions", location.Key + ".Z", location.Position.Z);
                var overridden = new Vector3(x, y, z);

                if (overridden != location.Position)
                {
                    location.Position = overridden;
                    location.Status = LocationStatus.Surveyed;
                }

                float heading = settings.GetValue<float>("Headings", location.Key, location.Heading);
                if (Math.Abs(heading - location.Heading) > 0.01f)
                {
                    location.Heading = heading;
                    location.Status = LocationStatus.Surveyed;
                }
            }

            Logger.Info("Loaded " + book._locations.Count + " campaign locations (" +
                        CountOf(book, LocationStatus.Estimate) + " still estimates).");
            return book;
        }

        private static int CountOf(LocationBook book, LocationStatus status)
        {
            int count = 0;
            foreach (var location in book._locations.Values)
            {
                if (location.Status == status) count++;
            }
            return count;
        }

        private static LocationStatus ParseStatus(string value)
        {
            switch ((value ?? "").ToLowerInvariant())
            {
                case "bible": return LocationStatus.Bible;
                case "surveyed": return LocationStatus.Surveyed;
                case "zone-centre":
                case "zone-center": return LocationStatus.ZoneCentre;
                default: return LocationStatus.Estimate;
            }
        }

        public MissionLocation Get(string key)
        {
            return _locations.TryGetValue(key, out var location) ? location : null;
        }

        public Vector3 Position(string key)
        {
            var location = Get(key);
            if (location != null) return location.Position;

            Logger.Error("Unknown location key requested: " + key);
            return Vector3.Zero;
        }

        public float Heading(string key)
        {
            return Get(key)?.Heading ?? 0f;
        }

        /// <summary>Records a surveyed position at runtime — the dev menu's survey mode.</summary>
        public void Record(string key, Vector3 position, float heading)
        {
            var location = Get(key);
            if (location == null) return;

            location.Position = position;
            location.Heading = heading;
            location.Status = LocationStatus.Surveyed;
        }
    }
}
