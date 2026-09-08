using System.Collections.Generic;
using GTA;
using GTA.Math;

namespace Bloodlines.Core
{
    /// <summary>
    /// Every world coordinate the campaign uses lives here, loaded from
    /// Bloodlines.Locations.ini so mission scripts never hard-code a position.
    ///
    /// The built-in values are APPROXIMATE. They put you in the right district
    /// (the Port of Los Santos / Terminal, standing in for the bible's "Terminal
    /// Island") but they are not surveyed against geometry. Fly to the real spot
    /// in game, press the dev capture key (F11 by default) and paste the logged
    /// line into the ini — that is the intended authoring loop, and it is why the
    /// defaults are marked rather than pretended-precise.
    /// </summary>
    public sealed class LocationBook
    {
        private readonly Dictionary<string, Vector3> _positions = new Dictionary<string, Vector3>();
        private readonly Dictionary<string, float> _headings = new Dictionary<string, float>();

        private static readonly Dictionary<string, Vector3> Defaults = new Dictionary<string, Vector3>
        {
            // --- Mission 01: Ghost in the Dockyard (Port of LS / Terminal, APPROX) ---
            { "M01.CraneNest",        new Vector3(1082.0f, -3175.0f, 40.0f) },
            { "M01.YachtDeck",        new Vector3(1017.0f, -3182.0f, 6.0f)  },
            { "M01.LowerDeckLedger",  new Vector3(1010.0f, -3196.0f, 5.0f)  },
            { "M01.WarehouseBay",     new Vector3(1057.0f, -3202.0f, 6.0f)  },
            { "M01.PrototypeCar",     new Vector3(1053.0f, -3205.0f, 5.9f)  },
            { "M01.CapoSpawn",        new Vector3(1019.0f, -3184.0f, 6.0f)  },
            { "M01.LaunchEscape",     new Vector3(1140.0f, -3320.0f, 0.0f)  },
            { "M01.RegroupPoint",     new Vector3(1073.0f, -3160.0f, 5.9f)  },
            // Toolkit-authored safe zone: the Cypress drainage tunnel run-out.
            { "M01.ExitPoint",        new Vector3(720.50f, -2400.10f, 15.20f) },

            // --- Solo mission SM01: Lead & Kevlar (Terminal Island warehouse 4, APPROX) ---
            { "SM01.WarehouseGate",   new Vector3(1035.0f, -3100.0f, 5.9f)  },
            { "SM01.SergeiOffice",    new Vector3(1046.0f, -3080.0f, 5.9f)  },
            { "SM01.CrateLoad",       new Vector3(1028.0f, -3096.0f, 5.9f)  },

            // --- Mission 02: Loose Strands (Olympic Freeway corridor, APPROX) ---
            { "M02.InterceptStart",   new Vector3(102.0f, -1810.0f, 27.0f)  },
            { "M02.CanalEscape",      new Vector3(285.0f, -1900.0f, 24.0f)  },

            // --- Crew safehouse: Cypress Flats industrial shop (Part I base, APPROX) ---
            { "Base.CypressFlats",    new Vector3(866.0f, -2110.0f, 30.5f)  }
        };

        private static readonly Dictionary<string, float> DefaultHeadings = new Dictionary<string, float>
        {
            { "M01.CraneNest", 210f },
            { "M01.WarehouseBay", 90f },
            { "M01.PrototypeCar", 270f },
            { "M02.InterceptStart", 60f },
            { "SM01.WarehouseGate", 340f },
            { "Base.CypressFlats", 175f }
        };

        public static LocationBook Load(string path)
        {
            var book = new LocationBook();
            var settings = ScriptSettings.Load(path);

            foreach (var pair in Defaults)
            {
                float x = settings.GetValue<float>("Positions", pair.Key + ".X", pair.Value.X);
                float y = settings.GetValue<float>("Positions", pair.Key + ".Y", pair.Value.Y);
                float z = settings.GetValue<float>("Positions", pair.Key + ".Z", pair.Value.Z);
                book._positions[pair.Key] = new Vector3(x, y, z);
            }

            foreach (var pair in DefaultHeadings)
            {
                book._headings[pair.Key] = settings.GetValue<float>("Headings", pair.Key, pair.Value);
            }

            settings.Save();
            Logger.Info("Loaded " + book._positions.Count + " campaign locations from " + path);
            return book;
        }

        public Vector3 Position(string key)
        {
            if (_positions.TryGetValue(key, out var value)) return value;
            Logger.Error("Unknown location key requested: " + key);
            return Vector3.Zero;
        }

        public float Heading(string key)
        {
            return _headings.TryGetValue(key, out var value) ? value : 0f;
        }
    }
}
