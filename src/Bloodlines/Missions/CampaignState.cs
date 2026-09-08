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
        public CrewSlot LastHero { get; private set; } = CrewSlot.Ice;
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
                    if (Enum.TryParse(Json.String(location, "hero", "Ice"), true, out CrewSlot hero))
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

            Completed.Add(missionId);

            var next = NextPlayable(catalog);
            CurrentMissionId = next?.Id ?? "";
            if (next != null) ActiveAct = (int)next.Act;

            Save();
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
                       !IsComplete(mission.Id) && PrerequisiteMet(mission))
                   ?? catalog.Playable.FirstOrDefault(mission => !IsComplete(mission.Id))
                   ?? catalog.Playable.FirstOrDefault();
        }

        public bool PrerequisiteMet(MissionDefinition mission)
        {
            string prerequisite = mission.Info.Prerequisite;
            return string.IsNullOrEmpty(prerequisite) || IsComplete(prerequisite);
        }

        public void Reset()
        {
            Completed.Clear();
            CurrentMissionId = "";
            ActiveAct = 1;
            CashOnHand = 0;
            AlamoGoldDredgedTons = 0f;
            OffshoreEscrowBalance = 0;
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
                { "fleetUpgrades", FleetUpgrades.ToDictionary(p => p.Key, p => (object)p.Value) }
            };

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_path) ?? ".");
                // Write beside the target then move, so a crash mid-write cannot leave
                // a half-written save where the real one was.
                string temporary = _path + ".tmp";
                File.WriteAllText(temporary, Json.Write(document) + Environment.NewLine);
                if (File.Exists(_path)) File.Delete(_path);
                File.Move(temporary, _path);
                Logger.Debug("Save written to " + _path);
            }
            catch (IOException ex)
            {
                Logger.Error("Could not write the save file", ex);
            }
        }
    }
}
