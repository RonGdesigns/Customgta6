using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA.Math;

namespace Bloodlines.Missions
{
    /// <summary>
    /// Everything that has to survive between missions and between sessions:
    /// progress, the crew's money and hauls, which safehouses are open, and which
    /// fleet upgrades are installed.
    ///
    /// Written to scripts/Bloodlines/data/savegame.json — deliberately not into the
    /// game's own save. A mod that writes to a story save can cost someone a
    /// playthrough; a flat text file next to the mod can be inspected, edited, backed
    /// up and deleted without touching anything Rockstar owns.
    /// </summary>
    public sealed class CampaignState
    {
        private readonly string _path;

        private CampaignState(string path)
        {
            _path = path;
        }

        // --- campaign ---
        public string CurrentMissionId { get; set; } = "";
        public int ActiveAct { get; private set; } = 1;
        public HashSet<string> Completed { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public CrewSlot LastHero { get; private set; } = Protagonist.StartingSlot;
        public Vector3 LastLocation { get; private set; }

        // --- economy (bible §2 and the heist payouts) ---
        public int CashOnHand { get; set; }
        public float AlamoGoldDredgedTons { get; set; }
        public long OffshoreEscrowBalance { get; set; }

        // --- unlocks ---
        public Dictionary<string, bool> Safehouses { get; } = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            { "cypressFoundry", false },
            { "canalLogisticsLoft", true },
            { "littleSeoulStudio", true },
            { "burroHeightsChopShop", true },
            { "grandSenoraRadarBunker", false },
            { "mckenzieAirfieldHangar", false },
            { "pillboxPenthouse", false }
        };

        public Dictionary<string, bool> FleetUpgrades { get; } = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            { "grangerTurbineInstalled", false },
            { "halfTrackAcquired", false },
            { "krakenSubmarineReinforced", false },
            { "racingTransmissionInstalled", false }
        };

        public Dictionary<string, HashSet<uint>> Weapons { get; } = new Dictionary<string, HashSet<uint>>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, object> CharacterMemory { get; } = new Dictionary<string, object>();

        public HashSet<string> ReadDispatches { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public int CompletedCount => Completed.Count;

        public bool IsComplete(string missionId) => Completed.Contains(missionId);

        public bool IsUnlocked(string safehouse) =>
            Safehouses.TryGetValue(safehouse, out bool open) && open;

        public void Unlock(string safehouse)
        {
            Safehouses[safehouse] = true;
            Save();
        }

        public void SetUpgrade(string upgrade, bool installed)
        {
            FleetUpgrades[upgrade] = installed;
            Save();
        }

        public static CampaignState Load(string path)
        {
            var state = new CampaignState(path);

            if (!File.Exists(path))
            {
                Logger.Info("No save file yet; starting a fresh campaign.");
                state.Save();
                return state;
            }

            try
            {
                var root = Json.Object(Json.Read(File.ReadAllText(path)));

                var campaign = Json.Object(root.TryGetValue("campaign", out var c) ? c : null);
                state.CurrentMissionId = Json.String(campaign, "currentMissionId");
                state.ActiveAct = Math.Max(1, Json.Int(campaign, "activeAct", 1));
                foreach (var entry in Json.Array(campaign, "completedMissions"))
                {
                    if (entry != null) state.Completed.Add(entry.ToString());
                }

                var location = Json.Object(campaign.TryGetValue("lastKnownLocation", out var l) ? l : null);
                if (location.Count > 0)
                {
                    state.LastLocation = new Vector3(Json.Float(location, "x"), Json.Float(location, "y"),
                        Json.Float(location, "z"));
                    if (Enum.TryParse(Json.String(location, "hero", "Guess"), true, out CrewSlot hero) && Enum.IsDefined(typeof(CrewSlot), hero))
                    {
                        state.LastHero = hero;
                    }
                }

                var economy = Json.Object(root.TryGetValue("economy", out var e) ? e : null);
                state.CashOnHand = Json.Int(economy, "cashOnHand");
                state.AlamoGoldDredgedTons = Json.Float(economy, "alamoGoldDredgedTons");
                state.OffshoreEscrowBalance = (long)Json.Float(economy, "offshoreEscrowBalance");

                Merge(state.Safehouses, Json.Object(root.TryGetValue("unlockedSafehouses", out var s) ? s : null));
                Merge(state.FleetUpgrades, Json.Object(root.TryGetValue("fleetUpgrades", out var f) ? f : null));

                var memory = Json.Object(root.TryGetValue("characterMemory", out var m) ? m : null);
                foreach (var hero in Protagonist.All)
                    if (memory.TryGetValue(hero.Slot.ToString(), out var record)) state.CharacterMemory[hero.Slot.ToString()] = record;
                var lockers = Json.Object(root.TryGetValue("weaponLockers", out var w) ? w : null);
                foreach (var hero in Protagonist.All)
                {
                    var owned = new HashSet<uint>();
                    foreach (var value in Json.Array(lockers, hero.Slot.ToString()))
                        if (uint.TryParse(value?.ToString(), out uint hash)) owned.Add(hash);
                    state.Weapons[hero.Slot.ToString()] = owned;
                }
                foreach (var entry in Json.Array(root, "readDispatches")) if (entry != null) state.ReadDispatches.Add(entry.ToString());
                Logger.Info("Save loaded: " + state.CompletedCount + " missions complete, act " + state.ActiveAct + ".");
            }
            catch (Exception ex)
            {
                // A corrupt save must not brick the mod. Keep the file for inspection
                // and carry on from defaults rather than overwriting it silently.
                Logger.Error("Could not read " + path + " — continuing from a fresh state", ex);
                TryBackup(path);
            }

            return state;
        }

        private static void Merge(Dictionary<string, bool> target, Dictionary<string, object> source)
        {
            foreach (var pair in source)
            {
                if (pair.Value is bool flag) target[pair.Key] = flag;
            }
        }

        private static void TryBackup(string path)
        {
            try
            {
                File.Copy(path, path + ".corrupt", true);
                Logger.Warn("Unreadable save copied to " + path + ".corrupt");
            }
            catch (IOException)
            {
            }
        }

        public void MarkComplete(string missionId, MissionCatalog catalog)
        {
            if (string.IsNullOrEmpty(missionId)) return;

            if (!Completed.Add(missionId)) return;
            AwardCompletion(missionId);

            var next = NextPlayable(catalog);
            CurrentMissionId = next?.Id ?? "";
            if (next != null) ActiveAct = (int)next.Act;

            Save();
        }

        private void AwardCompletion(string id)
        {
            switch (id)
            {
                case "M03": Safehouses["cypressFoundry"] = true; break;
                case "M05": CashOnHand += 50000; break;
                case "M11": FleetUpgrades["grangerTurbineInstalled"] = true; Safehouses["burroHeightsChopShop"] = true; break;
                case "M14": Safehouses["mckenzieAirfieldHangar"] = true; break;
                case "M15": CashOnHand += 15000; break;
                case "M17": FleetUpgrades["krakenSubmarineReinforced"] = true; break;
                case "M22": Safehouses["cypressFoundry"] = false; AlamoGoldDredgedTons = 0; CashOnHand += 150000; break;
                case "M23": Safehouses["grandSenoraRadarBunker"] = true; break;
                case "M24": AlamoGoldDredgedTons += 5; CashOnHand += 200000; break;
                case "M25": CashOnHand += 40000; break;
                case "M27": CashOnHand += 75000; break;
                case "M28": CashOnHand += 20000; FleetUpgrades["northernRelayDisabled"] = true; break;
                case "M29": CashOnHand += 35000; FleetUpgrades["bunkerFuelReserves"] = true; break;
                case "M30": CashOnHand += 45000; FleetUpgrades["satellitePartsSecured"] = true; break;
                case "SM04": CashOnHand += 15000; FleetUpgrades["quarryRadiosRecovered"] = true; break;
                case "SM05": CashOnHand += 15000; FleetUpgrades["estuaryTelemetry"] = true; break;
                case "SM06": CashOnHand += 25000; FleetUpgrades["airfieldFuelReserves"] = true; break;
                case "SM02": FleetUpgrades["surveillanceWormInstalled"] = true; break;
                case "SM03": CashOnHand += 25000; FleetUpgrades["racingTransmissionInstalled"] = true; break;
            }
        }

        /// <summary>Records where the crew was, for the save's last-known-location field.</summary>
        public void RecordPosition(CrewSlot hero, Vector3 position)
        {
            LastHero = hero;
            LastLocation = position;
        }

        /// <summary>
        /// The next mission to play: campaign order, playable, not yet done, and with
        /// its prerequisite satisfied. Prerequisites come from the registry, which is
        /// what keeps solo missions from opening before the act that sets them up.
        /// </summary>
        public MissionDefinition NextPlayable(MissionCatalog catalog)
        {
            return catalog.Playable.FirstOrDefault(mission =>
                       !IsComplete(mission.Id) && PrerequisiteMet(mission));
        }

        public bool PrerequisiteMet(MissionDefinition mission)
        {
            string prerequisite = mission.Info.Prerequisite;
            return string.IsNullOrEmpty(prerequisite) || IsComplete(prerequisite);
        }

        public void Reset()
        {
            Completed.Clear();
            ReadDispatches.Clear();
            Weapons.Clear();
            CharacterMemory.Clear();
            CurrentMissionId = "";
            ActiveAct = 1;
            CashOnHand = 0;
            AlamoGoldDredgedTons = 0f;
            OffshoreEscrowBalance = 0;
            var defaults = new CampaignState(_path);
            Safehouses.Clear();
            foreach (var pair in defaults.Safehouses) Safehouses[pair.Key] = pair.Value;
            FleetUpgrades.Clear();
            foreach (var pair in defaults.FleetUpgrades) FleetUpgrades[pair.Key] = pair.Value;
            LastHero = Protagonist.StartingSlot;
            LastLocation = Vector3.Zero;
            Save();
        }

        public void Save()
        {
            var document = new Dictionary<string, object>
            {
                {
                    "campaign", new Dictionary<string, object>
                    {
                        { "currentMissionId", CurrentMissionId },
                        { "completedMissions", Completed.OrderBy(id => id, StringComparer.Ordinal).ToList() },
                        { "activeAct", ActiveAct },
                        {
                            "lastKnownLocation", new Dictionary<string, object>
                            {
                                { "hero", LastHero.ToString() },
                                { "x", LastLocation.X },
                                { "y", LastLocation.Y },
                                { "z", LastLocation.Z }
                            }
                        }
                    }
                },
                {
                    "economy", new Dictionary<string, object>
                    {
                        { "cashOnHand", CashOnHand },
                        { "alamoGoldDredgedTons", AlamoGoldDredgedTons },
                        { "offshoreEscrowBalance", OffshoreEscrowBalance }
                    }
                },
                { "unlockedSafehouses", Safehouses.ToDictionary(p => p.Key, p => (object)p.Value) },
                { "fleetUpgrades", FleetUpgrades.ToDictionary(p => p.Key, p => (object)p.Value) },
                { "readDispatches", ReadDispatches.OrderBy(id => id).ToList() },
                { "characterMemory", CharacterMemory },
                { "weaponLockers", Weapons.ToDictionary(p => p.Key, p => (object)p.Value.OrderBy(h => h).Select(h => h.ToString()).ToList()) }
            };

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? ".");
                // Write beside the target then move, so a crash mid-write cannot leave
                // a half-written save where the real one was.
                string temporary = _path + ".tmp";
                File.WriteAllText(temporary, Json.Write(document) + Environment.NewLine);
                if (File.Exists(_path)) File.Replace(temporary, _path, _path + ".bak");
                else File.Move(temporary, _path);
                Logger.Debug("Save written to " + _path);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                Logger.Error("Could not write the save file", ex);
            }
        }
    }
}
