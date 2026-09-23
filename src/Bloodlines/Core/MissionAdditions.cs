using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>What an author can put into a mission from the survey editor.</summary>
    public enum AdditionKind { Enemy, Vehicle, Prop }

    /// <summary>
    /// One extra thing placed into a mission with the survey editor: a group of men, a
    /// vehicle with men in it, or a prop. None of them is part of what the mission needs
    /// to finish. They are opposition and set dressing an author chose to add, and a
    /// mission passes or fails exactly as it did without them.
    /// </summary>
    public sealed class Addition
    {
        public string Mission = "", Id = "";
        public AdditionKind Kind;
        /// <summary>The man for an enemy group or a vehicle's crew; the object for a prop.</summary>
        public string Model = "";
        /// <summary>The vehicle, for a vehicle addition. Empty otherwise.</summary>
        public string Vehicle = "";
        public Vector3 Position;
        public float Heading;
        /// <summary>Men in the group, or men aboard the vehicle.</summary>
        public int Count = 1;
        /// <summary>How far an enemy group spreads around its point.</summary>
        public float Radius = MissionAdditions.DefaultRadius;
        public string Weapon = "carbine";
        public string Side = "cartel";
        /// <summary>Enemies walk the area and a vehicle drives around, instead of holding the spot.</summary>
        public bool Patrol;

        /// <summary>One line for a menu row: what it is and how many.</summary>
        public string Summary
        {
            get
            {
                switch (Kind)
                {
                    case AdditionKind.Vehicle: return MissionAdditions.Label(Vehicle) + " with " + Count + " aboard" + (Patrol ? ", patrolling" : "");
                    case AdditionKind.Prop: return MissionAdditions.Label(Model);
                    default: return Count + (Count == 1 ? " man" : " men") + ", " + Weapon + ", " + Side + (Patrol ? ", patrolling" : "");
                }
            }
        }
    }

    /// <summary>
    /// The additions an author has placed, per mission, and the one place they are spawned.
    ///
    /// They live in their own file beside the survey ini rather than in the location book:
    /// a location key is a point a mission already reads, and an addition is a thing the
    /// mission has never heard of. Mixing the two would make every addition look like a key
    /// the code forgot to use.
    ///
    /// Spawning goes through the running mission's own <c>Track</c>, so the mission owns
    /// what it spawned and its ordinary cleanup takes it away. There is no second pool of
    /// entities here - that is the double-ownership bug this project keeps finding.
    /// </summary>
    public static class MissionAdditions
    {
        public const string FileName = "Bloodlines.Additions.tsv";
        public const int MaxCount = 8;
        public const int MaxPerMission = 40;
        public const float DefaultRadius = 4f, MinRadius = 0f, MaxRadius = 25f;
        /// <summary>How close a crew member gets before a man standing guard is told to fight.</summary>
        public const float EngageMeters = 60f;
        /// <summary>A vehicle crew sees further: they are higher up and moving.</summary>
        public const float VehicleEngageMeters = 90f;
        /// <summary>How far round himself an engaged man looks for someone to fight.</summary>
        public const float CombatSearchMeters = 150f;

        // Every name below is checked against the public model dumps by a story test. The
        // men are ones this campaign already spawns; the vehicles were read out of
        // build/vehicles.json; the props are ones missions already place.
        public static readonly string[] EnemyModels =
        {
            "g_m_y_mexgoon_01", "g_m_y_mexgoon_02", "g_m_y_mexgoon_03", "g_m_m_armgoon_01", "g_m_y_salvagoon_01",
            "g_m_y_lost_01", "g_m_y_ballasout_01", "s_m_y_blackops_01", "s_m_y_blackops_02", "s_m_y_blackops_03",
            "s_m_m_marine_01", "s_m_y_swat_01", "mp_m_securoguard_01", "s_m_m_security_01",
        };
        public static readonly string[] VehicleModels =
        {
            "granger", "baller2", "cavalcade", "dubsta", "mesa3", "patriot", "speedo", "burrito3", "technical",
            "insurgent", "nightshark", "crusader", "kuruma2", "sanchez", "dinghy", "tropic", "seashark",
        };
        public static readonly string[] PropModels =
        {
            "prop_barrier_work05", "prop_mp_barrier_02b", "prop_mb_sandblock_01", "prop_boxpile_07d", "prop_mil_crate_01",
            "prop_box_ammo03a", "prop_box_wood02a", "prop_container_01a", "prop_portacabin01", "prop_generator_03b",
            "prop_worklight_03b", "prop_rub_carwreck_3", "prop_table_03", "prop_laptop_01a", "prop_ld_case_01",
            "prop_tool_bench02", "prop_elecbox_12", "prop_satdish_2_a",
        };
        public static readonly string[] Weapons = { "pistol", "smg", "carbine", "rifle", "shotgun", "mg", "sniper", "rpg" };
        public static readonly string[] Sides = { "cartel", "aegis" };
        private static readonly string[] Boats = { "dinghy", "tropic", "seashark" };

        private static readonly List<Addition> _all = new List<Addition>();
        private static string _path;

        public static IReadOnlyList<Addition> All => _all;
        public static string FilePath => _path;

        public static IReadOnlyList<Addition> For(string mission) =>
            _all.Where(a => string.Equals(a.Mission, mission, StringComparison.OrdinalIgnoreCase)).ToList();

        public static bool IsBoat(string vehicle) => Boats.Contains((vehicle ?? "").ToLowerInvariant());

        /// <summary>A model name as a person would say it: the part after the prefix.</summary>
        public static string Label(string model)
        {
            if (string.IsNullOrEmpty(model)) return "";
            string name = model.StartsWith("prop_", StringComparison.OrdinalIgnoreCase) ? model.Substring(5) : model;
            return name.Replace('_', ' ');
        }

        public static WeaponHash WeaponFor(string name)
        {
            switch ((name ?? "").ToLowerInvariant())
            {
                case "pistol": return WeaponHash.Pistol;
                case "smg": return WeaponHash.SMG;
                case "rifle": return WeaponHash.AssaultRifle;
                case "shotgun": return WeaponHash.PumpShotgun;
                case "mg": return WeaponHash.MG;
                case "sniper": return WeaponHash.SniperRifle;
                case "rpg": return WeaponHash.RPG;
                default: return WeaponHash.CarbineRifle;
            }
        }

        // ---------- the file ----------

        private static readonly string[] Columns =
            { "mission", "id", "kind", "model", "vehicle", "x", "y", "z", "heading", "count", "radius", "weapon", "side", "patrol" };

        /// <summary>Read the additions file. A missing file is no additions, not an error.</summary>
        public static void Load(string path)
        {
            _path = path;
            _all.Clear();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;
            int line = 0;
            foreach (var raw in File.ReadAllLines(path))
            {
                line++;
                if (line == 1 || string.IsNullOrWhiteSpace(raw) || raw.StartsWith("#")) continue;
                var cells = raw.Split('\t');
                if (cells.Length < Columns.Length) { Logger.Warn(FileName + " line " + line + " has " + cells.Length + " columns; skipped."); continue; }
                try
                {
                    var a = new Addition
                    {
                        Mission = cells[0].Trim(), Id = cells[1].Trim(),
                        Kind = (AdditionKind)Enum.Parse(typeof(AdditionKind), cells[2].Trim(), true),
                        Model = cells[3].Trim(), Vehicle = cells[4].Trim(),
                        Position = new Vector3(Float(cells[5]), Float(cells[6]), Float(cells[7])),
                        Heading = Float(cells[8]),
                        Count = Math.Max(1, Math.Min(MaxCount, int.Parse(cells[9].Trim(), CultureInfo.InvariantCulture))),
                        Radius = Math.Max(MinRadius, Math.Min(MaxRadius, Float(cells[10]))),
                        Weapon = cells[11].Trim(), Side = cells[12].Trim(),
                        Patrol = cells[13].Trim() == "1" || cells[13].Trim().Equals("true", StringComparison.OrdinalIgnoreCase),
                    };
                    if (a.Mission.Length == 0 || a.Id.Length == 0) continue;
                    _all.Add(a);
                }
                catch (Exception ex) { Logger.Warn(FileName + " line " + line + " could not be read: " + ex.Message); }
            }
            Logger.Info("Loaded " + _all.Count + " placed addition(s) from " + FileName + ".");
        }

        private static float Float(string cell) => float.Parse(cell.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);

        /// <summary>Write every addition, the way the survey writes: a temporary file swapped in, the old one kept as .bak.</summary>
        public static bool Save()
        {
            if (string.IsNullOrEmpty(_path)) { Logger.Warn("Placed additions have nowhere to be saved."); return false; }
            var lines = new List<string> { string.Join("\t", Columns) };
            foreach (var a in _all.OrderBy(a => a.Mission, StringComparer.Ordinal).ThenBy(a => a.Id, StringComparer.Ordinal))
                lines.Add(string.Join("\t", a.Mission, a.Id, a.Kind.ToString(), a.Model, a.Vehicle,
                    F(a.Position.X), F(a.Position.Y), F(a.Position.Z), a.Heading.ToString("0.0", CultureInfo.InvariantCulture),
                    a.Count.ToString(CultureInfo.InvariantCulture), a.Radius.ToString("0.0", CultureInfo.InvariantCulture),
                    a.Weapon, a.Side, a.Patrol ? "1" : "0"));
            try
            {
                string temp = _path + ".tmp";
                File.WriteAllLines(temp, lines, new UTF8Encoding(false));
                if (File.Exists(_path)) File.Replace(temp, _path, _path + ".bak");
                else File.Move(temp, _path);
                return true;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Logger.Error("Saving placed additions", ex);
                return false;
            }
        }

        private static string F(float value) => value.ToString("0.00", CultureInfo.InvariantCulture);

        /// <summary>Put one in the list and on disk. Refused past the per-mission cap, and rolled back if the write fails.</summary>
        public static bool Add(Addition addition)
        {
            if (addition == null || string.IsNullOrEmpty(addition.Mission)) return false;
            if (For(addition.Mission).Count >= MaxPerMission) return false;
            if (string.IsNullOrEmpty(addition.Id)) addition.Id = NextId(addition.Mission);
            _all.Add(addition);
            if (Save()) return true;
            _all.Remove(addition);
            return false;
        }

        public static bool Remove(Addition addition)
        {
            if (addition == null || !_all.Remove(addition)) return false;
            if (Save()) return true;
            _all.Add(addition);
            return false;
        }

        /// <summary>Move one that is already placed. Rolled back if the write fails.</summary>
        public static bool Move(Addition addition, Vector3 to, float heading)
        {
            if (addition == null || !_all.Contains(addition)) return false;
            var was = addition.Position; float wasHeading = addition.Heading;
            addition.Position = to; addition.Heading = heading;
            if (Save()) return true;
            addition.Position = was; addition.Heading = wasHeading;
            return false;
        }

        public static string NextId(string mission)
        {
            int top = 0;
            foreach (var a in For(mission))
                if (a.Id.StartsWith("A") && int.TryParse(a.Id.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n)) top = Math.Max(top, n);
            return "A" + (top + 1).ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>Where the i-th of n men stands inside a circle: the same sunflower spread the mission groups use.</summary>
        public static Vector3 Spread(Vector3 center, float radius, int index, int count)
        {
            if (count <= 1 || radius <= 0.01f) return center;
            double angle = index * 2.39996323; // the golden angle, in radians
            float r = radius * (float)Math.Sqrt((index + 0.5) / count);
            return center + new Vector3((float)Math.Cos(angle) * r, (float)Math.Sin(angle) * r, 0f);
        }

        // ---------- in the world ----------

        private sealed class Live
        {
            public Ped Ped;
            public bool InVehicle, Engaged;
        }

        /// <summary>The men spawned for the running mission, so they can be told to fight once somebody arrives.</summary>
        private static readonly List<Live> _live = new List<Live>();
        public static int LiveCount => _live.Count;

        /// <summary>
        /// Create everything placed for this mission and hand it back for the mission to own.
        /// One that will not load is logged and skipped; it never stops the mission.
        /// </summary>
        public static List<Entity> Spawn(string mission)
        {
            var made = new List<Entity>();
            foreach (var a in For(mission))
            {
                try
                {
                    switch (a.Kind)
                    {
                        case AdditionKind.Enemy: SpawnGroup(a, made); break;
                        case AdditionKind.Vehicle: SpawnVehicle(a, made); break;
                        case AdditionKind.Prop: SpawnProp(a, made); break;
                    }
                }
                catch (Exception ex) { Logger.Error(mission + " addition " + a.Id + " (" + a.Summary + ")", ex); }
            }
            if (made.Count > 0) Logger.Info(mission + ": spawned " + made.Count + " placed addition entit" + (made.Count == 1 ? "y" : "ies") + ".");
            return made;
        }

        private static void SpawnGroup(Addition a, List<Entity> made)
        {
            var model = new Model(a.Model);
            if (!GameUtils.RequestModel(model)) { Logger.Warn("Addition " + a.Mission + "." + a.Id + ": " + a.Model + " would not load; skipped."); return; }
            try
            {
                for (int i = 0; i < a.Count; i++)
                {
                    var point = Spread(a.Position, a.Radius, i, a.Count);
                    // The center was dropped onto a real surface when it was placed; the
                    // others stand on whatever is under their own spot, found the same way.
                    if (i > 0)
                    {
                        float? floor = MissionSites.SurfaceHeight(point, a.Position.Z + 3f, a.Position.Z - 6f);
                        if (floor.HasValue) point = new Vector3(point.X, point.Y, floor.Value);
                    }
                    var ped = World.CreatePed(model, point, a.Heading);
                    if (ped == null || !ped.Exists()) continue;
                    Arm(ped, a);
                    if (a.Patrol) Function.Call(Hash.TASK_WANDER_IN_AREA, ped, a.Position.X, a.Position.Y, a.Position.Z, Math.Max(8f, a.Radius * 2f), 2f, 4f);
                    else ped.Task.GuardCurrentPosition();
                    _live.Add(new Live { Ped = ped });
                    made.Add(ped);
                }
            }
            finally { model.MarkAsNoLongerNeeded(); }
        }

        private static void SpawnVehicle(Addition a, List<Entity> made)
        {
            var carModel = new Model(a.Vehicle);
            var crewModel = new Model(string.IsNullOrEmpty(a.Model) ? EnemyModels[0] : a.Model);
            if (!GameUtils.RequestModel(carModel) || !GameUtils.RequestModel(crewModel))
            { Logger.Warn("Addition " + a.Mission + "." + a.Id + ": " + a.Vehicle + " or its crew would not load; skipped."); return; }
            try
            {
                bool boat = IsBoat(a.Vehicle);
                // The point was dropped onto the surface; a car's origin sits above its wheels,
                // so it is created a little higher and settled onto them.
                var vehicle = World.CreateVehicle(carModel, boat ? a.Position : a.Position + new Vector3(0f, 0f, 0.5f), a.Heading);
                if (vehicle == null || !vehicle.Exists()) return;
                vehicle.IsPersistent = true;
                if (!boat) Function.Call<bool>(Hash.SET_VEHICLE_ON_GROUND_PROPERLY, vehicle, 5f);
                made.Add(vehicle);
                int seats = Function.Call<int>(Hash.GET_VEHICLE_MODEL_NUMBER_OF_SEATS, carModel.Hash);
                int aboard = Math.Max(1, Math.Min(a.Count, seats > 0 ? seats : 1));
                for (int i = 0; i < aboard; i++)
                {
                    var seat = i == 0 ? VehicleSeat.Driver : (VehicleSeat)(i - 1);
                    var ped = World.CreatePed(crewModel, a.Position + new Vector3(0f, 0f, 1f), a.Heading);
                    if (ped == null || !ped.Exists()) continue;
                    // Seated outright and checked, never a queued warp with another task behind it.
                    ped.SetIntoVehicle(vehicle, seat);
                    Arm(ped, a);
                    made.Add(ped);
                    _live.Add(new Live { Ped = ped, InVehicle = true });
                    if (i == 0 && a.Patrol && !boat)
                        Function.Call(Hash.TASK_VEHICLE_DRIVE_WANDER, ped, vehicle, 14f, 786603);
                }
            }
            finally { carModel.MarkAsNoLongerNeeded(); crewModel.MarkAsNoLongerNeeded(); }
        }

        private static void SpawnProp(Addition a, List<Entity> made)
        {
            var model = new Model(a.Model);
            if (!GameUtils.RequestModel(model)) { Logger.Warn("Addition " + a.Mission + "." + a.Id + ": " + a.Model + " would not load; skipped."); return; }
            try
            {
                // Exactly where it was put: the drop onto the surface happened when it was placed.
                var prop = World.CreateProp(model, a.Position, false, false);
                if (prop == null || !prop.Exists()) return;
                prop.Heading = a.Heading;
                prop.IsPositionFrozen = true;
                made.Add(prop);
            }
            finally { model.MarkAsNoLongerNeeded(); }
        }

        private static void Arm(Ped ped, Addition a)
        {
            ped.IsPersistent = true;
            // Left able to react: nothing here orders him about every frame the way
            // GuardAwareness does for a mission's own guards, so he has to be able to see
            // the crew and answer them himself.
            ped.BlockPermanentEvents = false;
            var group = World.AddRelationshipGroup(string.Equals(a.Side, "aegis", StringComparison.OrdinalIgnoreCase) ? "BLOODLINES_AEGIS" : "BLOODLINES_CARTEL");
            ped.RelationshipGroup = group;
            ped.Accuracy = 35;
            if (string.Equals(a.Side, "aegis", StringComparison.OrdinalIgnoreCase)) ped.Armor = 50;
            ped.Weapons.Give(WeaponFor(a.Weapon), 300, true, true);
        }

        /// <summary>
        /// Tell a placed man to fight once one of the crew comes close. Issued once per man,
        /// never every frame: re-ordering a combat task restarts it before he can act on it,
        /// which is the fault that left M31's guards standing still.
        /// </summary>
        public static void Update(IEnumerable<Ped> crew)
        {
            if (_live.Count == 0) return;
            var near = (crew ?? Enumerable.Empty<Ped>()).Where(p => p != null && p.Exists() && !p.IsDead).ToList();
            var player = Game.Player.Character;
            if (player != null && player.Exists() && !near.Contains(player)) near.Add(player);
            for (int i = _live.Count - 1; i >= 0; i--)
            {
                var live = _live[i];
                var ped = live.Ped;
                if (ped == null || !ped.Exists() || ped.IsDead) { _live.RemoveAt(i); continue; }
                if (live.Engaged) continue;
                float reach = live.InVehicle ? VehicleEngageMeters : EngageMeters;
                if (!near.Any(c => c.Position.DistanceTo(ped.Position) <= reach)) continue;
                live.Engaged = true;
                Function.Call(Hash.TASK_COMBAT_HATED_TARGETS_AROUND_PED, ped, CombatSearchMeters, 0);
            }
        }

        /// <summary>The mission that owned them has ended; its cleanup took the entities.</summary>
        public static void Forget() => _live.Clear();
    }
}
