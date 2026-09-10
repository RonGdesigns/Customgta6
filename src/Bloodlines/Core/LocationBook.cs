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

        /// <summary>Moved to the center of the correct district by the audit tool.</summary>
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

        /// <summary>How many per-install override keys were ignored because they were the retired template, not a survey.</summary>
        public int IgnoredStaleOverrides { get; private set; }

        /// <summary>
        /// The override file shipped before September 9, 2026 was not empty: it
        /// carried these bible-era M01 positions, was installed once as the player's
        /// own survey file, and then silently overrode every later correction to
        /// those keys on every install. A per-install file that still holds one of
        /// these values byte-for-byte was never surveyed; the value is ignored and
        /// logged, and anything the player actually captured (which never lands on
        /// these exact numbers) still applies.
        /// </summary>
        private static readonly Dictionary<string, Vector3> RetiredTemplate = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase)
        {
            { "M01.CraneNest", new Vector3(1082f, -3175f, 40f) },
            { "M01.YachtDeck", new Vector3(1017f, -3182f, 6f) },
            { "M01.WarehouseBay", new Vector3(1057f, -3202f, 6f) },
            { "M01.PrototypeCar", new Vector3(1053f, -3205f, 5.9f) },
            { "M01.CapoSpawn", new Vector3(1019f, -3184f, 6f) },
            { "M01.LaunchEscape", new Vector3(1140f, -3320f, 0f) },
            { "M01.RegroupPoint", new Vector3(1073f, -3160f, 5.9f) },
            { "M01.ExitPoint", new Vector3(1180f, -2990f, 5.9f) },
            { "Base.CypressFlats", new Vector3(866f, -2110f, 30.5f) }
        };

        private static readonly Dictionary<string, float> RetiredTemplateHeadings = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
        {
            { "M01.CraneNest", 210f }, { "M01.WarehouseBay", 90f }, { "M01.PrototypeCar", 270f }, { "Base.CypressFlats", 175f }
        };

        private static bool IsRetiredTemplate(string key, Vector3 position) =>
            RetiredTemplate.TryGetValue(key, out var stale) && position.DistanceTo(stale) < 0.05f;

        public static LocationBook Load(string dataDirectory, string overridesPath, string surveyedPath = null, CampaignData data = null)
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
            book.ApplyOverrides(overridesPath, false);
            // Bible anchors describe proposed set pieces, including an absent yacht
            // interior. They are reference material, not verified spawn geometry.
            if (surveyedPath != null && File.Exists(surveyedPath)) book.ApplyOverrides(surveyedPath, true);

            Logger.Info("Loaded " + book._locations.Count + " campaign locations (" +
                        CountOf(book, LocationStatus.Estimate) + " still estimates).");
            return book;
        }

        private void ApplyOverrides(string overridesPath, bool surveyed)
        {
            var settings = ScriptSettings.Load(overridesPath);
            foreach (var location in _locations.Values)
            {
                float x = settings.GetValue<float>("Positions", location.Key + ".X", location.Position.X);
                float y = settings.GetValue<float>("Positions", location.Key + ".Y", location.Position.Y);
                float z = settings.GetValue<float>("Positions", location.Key + ".Z", location.Position.Z);
                var overridden = new Vector3(x, y, z);

                // Only a key the file actually carries can be a stale template entry;
                // a default that merely equals the old template is the default.
                bool present = settings.GetValue<string>("Positions", location.Key + ".X", null) != null;
                bool stale = !surveyed && present && IsRetiredTemplate(location.Key, overridden);
                if (stale)
                {
                    IgnoredStaleOverrides++;
                    Logger.Warn("Ignored stale template override for " + location.Key + " in " + Path.GetFileName(overridesPath) +
                                ": it is the pre-correction shipped value, not a survey. Delete the key or re-survey it.");
                }
                else if (overridden != location.Position || (surveyed && settings.GetValue<string>("Positions", location.Key + ".X", null) != null))
                {
                    location.Position = overridden;
                    location.Status = LocationStatus.Surveyed;
                }

                float heading = settings.GetValue<float>("Headings", location.Key, location.Heading);
                bool staleHeading = stale && RetiredTemplateHeadings.TryGetValue(location.Key, out float retired) && Math.Abs(heading - retired) < 0.01f;
                if (!staleHeading && Math.Abs(heading - location.Heading) > 0.01f)
                {
                    location.Heading = heading;
                    location.Status = LocationStatus.Surveyed;
                }
            }

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
                case "zone-centre": // dialect-ok: accepted input spelling alongside the American one
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
