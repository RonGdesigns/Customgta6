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
    /// <summary>The five things "what next" can mean, so none of them falls back to M01.</summary>
    public enum CampaignProgress
    {
        StoryAvailable,
        /// <summary>The next story mission exists but a story gate holds it until named solo jobs are done.</summary>
        StoryGated,
        SideContentOnly,
        StoryBlocked,
        ImplementedContentComplete
    }

    public enum EvidenceState { None, Alleged, CopyHeld, Proven, Distributed, Destroyed }

    public sealed class CampaignState
    {
        private readonly string _path;
        private string _attemptId;
        private bool _attemptReplay;
        private Dictionary<string, string> _savedEvidence, _savedCargo;
        private Dictionary<string, bool> _savedUpgrades, _savedSafehouses;
        public bool AttemptActive => _attemptId != null;

        // Runtime dictionaries show the current attempt. Serialization continues
        // to use the last committed story until the manager verifies success.
        public void BeginAttempt(string id)
        {
            if (AttemptActive) throw new InvalidOperationException("A mission attempt already owns story state.");
            _attemptId = id ?? throw new ArgumentNullException(nameof(id));
            _attemptReplay = IsComplete(PortHeistOperation.Contains(id) ? "M22" : id);
            _savedEvidence = new Dictionary<string, string>(Evidence, StringComparer.OrdinalIgnoreCase);
            _savedCargo = new Dictionary<string, string>(Cargo, StringComparer.OrdinalIgnoreCase);
            _savedUpgrades = new Dictionary<string, bool>(FleetUpgrades, StringComparer.OrdinalIgnoreCase);
            _savedSafehouses = new Dictionary<string, bool>(Safehouses, StringComparer.OrdinalIgnoreCase);
        }

        private static void Restore<T>(Dictionary<string, T> target, Dictionary<string, T> saved)
        { if (saved == null) return; target.Clear(); foreach (var pair in saved) target[pair.Key] = pair.Value; }

        public void DiscardAttempt() { EndAttempt(false); }
        private void EndAttempt(bool success)
        {
            if (!AttemptActive) return;
            if (!success || _attemptReplay)
            {
                Restore(Evidence, _savedEvidence); Restore(Cargo, _savedCargo);
                Restore(FleetUpgrades, _savedUpgrades); Restore(Safehouses, _savedSafehouses);
            }
            _attemptId = null; _savedEvidence = _savedCargo = null;
            _savedUpgrades = _savedSafehouses = null;
        }

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

        /// <summary>
        /// Ron's arrival drive before M01. A save that already finished M01 counts as
        /// having played it, so existing campaigns are never sent back to the airport.
        /// </summary>
        public bool PrologueComplete { get; set; }
        private int _saveBatch;
        // There is deliberately no heist resume field: only final success is saved.
        public void CompletePortHeist(MissionCatalog catalog, IDictionary<string, string> cargo)
        {
            bool firstPass = !IsComplete("M22");
            if (AttemptActive && PortHeistOperation.Contains(_attemptId)) EndAttempt(firstPass);
            _saveBatch++;
            try
            {
                foreach (var phase in PortHeistOperation.PhaseIds) MarkComplete(phase, catalog);
                if (firstPass && cargo != null)
                    foreach (var pair in cargo) { if (string.IsNullOrEmpty(pair.Value)) Cargo.Remove(pair.Key); else Cargo[pair.Key] = pair.Value; }
            }
            finally { _saveBatch--; }
            Save();
        }
        public bool PrologueDue => !PrologueComplete && !IsComplete("M01");

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

        /// <summary>
        /// The evidence chain, separate from cargo and money: what the crew holds and
        /// how far it has been taken. "Alleged" is a claim, "CopyHeld" a copy in hand,
        /// "Proven" authenticated, "Distributed" published. Later chapters read these;
        /// no chapter reads ahead.
        /// </summary>
        public Dictionary<string, string> Evidence { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public void SetEvidence(string key, EvidenceState state) { Evidence[key] = state.ToString(); Save(); Logger.Info("Evidence " + key + " -> " + state); }
        public EvidenceState EvidenceOf(string key) => Evidence.TryGetValue(key, out var value) && Enum.TryParse(value, out EvidenceState state) ? state : EvidenceState.None;
        /// <summary>Where a shipment was left (a location key), by cargo name: the engines at their stash until M10 collects them.</summary>
        public Dictionary<string, string> Cargo { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public void SetCargo(string key, string locationKey) { if (string.IsNullOrEmpty(locationKey)) Cargo.Remove(key); else Cargo[key] = locationKey; Save(); Logger.Info("Cargo " + key + " -> " + (locationKey ?? "collected")); }
        public string CargoAt(string key) => Cargo.TryGetValue(key, out var where) ? where : null;

        /// <summary>What the crew has done to their Granger. See <see cref="CrewVan"/>.</summary>
        public CrewVanRecord CrewVan { get; } = new CrewVanRecord();
        /// <summary>Garages the crew has bought, by site id. See <see cref="Core.GarageService"/>.</summary>
        public HashSet<string> Garages { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        /// <summary>The crew's owned vehicles and their builds.</summary>
        public List<Core.OwnedVehicle> Vehicles { get; } = new List<Core.OwnedVehicle>();
        public int NextVehicleId { get; set; } = 1;
        public string VehicleSalesDay { get; set; } = "";
        public int VehicleSalesCount { get; set; }
        public Dictionary<string, bool> FleetUpgrades { get; } = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            { "grangerTurbineInstalled", false },
            { "halfTrackAcquired", false },
            { "krakenSubmarineReinforced", false },
            { "racingTransmissionInstalled", false },
            { "armorPiercingSupply", false }
        };

        public Dictionary<string, HashSet<uint>> WeaponPartsOwned { get; } = new Dictionary<string, HashSet<uint>>();
        public Dictionary<string, HashSet<uint>> WeaponPartsFitted { get; } = new Dictionary<string, HashSet<uint>>();
        public Dictionary<string, HashSet<uint>> Weapons { get; } = new Dictionary<string, HashSet<uint>>(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, object> CharacterMemory { get; } = new Dictionary<string, object>();

        public List<Core.PhoneNotice> PhoneHistory { get; } = new List<Core.PhoneNotice>();
        public List<string> PhoneAppOrder { get; } = new List<string>();
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
                // Ignore the superseded portHeistResumePhase field, valid or not.
                // Older saves keep earned progress; their unfinished attempt restarts whole.
                state.ActiveAct = Math.Max(1, Json.Int(campaign, "activeAct", 1));
                state.PrologueComplete = campaign.TryGetValue("prologueComplete", out var prologue) && prologue is bool played && played;
                foreach (var entry in Json.Array(campaign, "completedMissions"))
                {
                    if (entry != null) state.Completed.Add(entry.ToString());
                }

                if (!state.IsComplete("M22") && PortHeistOperation.Contains(state.CurrentMissionId))
                    state.CurrentMissionId = "M19";

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
                state.CrewVan.FromJson(Json.Object(root.TryGetValue("crewVan", out var van) ? van : null));
                foreach (var entry in Json.Array(root, "garages")) if (entry != null) state.Garages.Add(entry.ToString());
                foreach (var entry in Json.Array(root, "vehicles")) { var car = Core.OwnedVehicle.FromJson(Json.Object(entry)); if (car != null && car.Id > 0 && !state.Vehicles.Any(v => v.Id == car.Id)) state.Vehicles.Add(car); }
                state.VehicleSalesDay = Json.String(root, "vehicleSalesDay");
                state.VehicleSalesCount = Math.Max(0, Math.Min(10, Json.Int(root, "vehicleSalesCount")));
                state.NextVehicleId = Math.Max(state.Vehicles.Count == 0 ? 1 : state.Vehicles.Max(v => v.Id) + 1, Json.Int(root, "nextVehicleId", 1));
                foreach (var pair in Json.Object(root.TryGetValue("evidence", out var ev) ? ev : null)) if (pair.Value != null) state.Evidence[pair.Key] = pair.Value.ToString();
                foreach (var pair in Json.Object(root.TryGetValue("cargo", out var cg) ? cg : null)) if (pair.Value != null) state.Cargo[pair.Key] = pair.Value.ToString();

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
                foreach(var field in new[]{"weaponPartsOwned","weaponPartsFitted"})
                {
                    var target=field=="weaponPartsOwned"?state.WeaponPartsOwned:state.WeaponPartsFitted;
                    foreach(var pair in Json.Object(root.TryGetValue(field,out var parts)?parts:null))
                    {
                        var set=new HashSet<uint>();foreach(var value in (pair.Value as System.Collections.Generic.List<object> ?? new System.Collections.Generic.List<object>()))if(uint.TryParse(value?.ToString(),out uint hash))set.Add(hash);
                        target[pair.Key]=set;
                    }
                }
                foreach (var entry in Json.Array(root, "phoneAppOrder").Take(64)) if (entry is string app) state.PhoneAppOrder.Add(app);
                foreach (var entry in Json.Array(root, "phoneHistory").AsEnumerable().Reverse().Take(60).Reverse())
                {
                    var notice = Core.PhoneNotice.FromJson(Json.Object(entry));
                    if (!string.IsNullOrWhiteSpace(notice.Id) && !string.IsNullOrWhiteSpace(notice.Title)) state.PhoneHistory.Add(notice);
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

            if (string.Equals(_attemptId, missionId, StringComparison.OrdinalIgnoreCase)) EndAttempt(!IsComplete(missionId));
            if (!Completed.Add(missionId)) return;
            AwardCompletion(missionId);

            var next = NextPlayable(catalog);
            CurrentMissionId = next?.Id ?? "";
            if (next != null) ActiveAct = (int)next.Act;
            else Logger.Info("Every scripted mission is complete. The save now reports end of implemented content, not M01.");

            Save();
        }

        /// <summary>
        /// What the campaign can offer next. Kept separate from <see cref="NextPlayable"/>
        /// because "no next mission" used to be indistinguishable from "start over": the
        /// player who finished the last scripted job must be told the build has run out
        /// of story, never pointed back at M01.
        /// </summary>
        public CampaignProgress Progress(MissionCatalog catalog)
        {
            var story = NextStory(catalog);
            if (story != null)
            {
                if (GateSatisfied(story, catalog)) return CampaignProgress.StoryAvailable;
                return UnavailableGateJobs(story, catalog).Any() ? CampaignProgress.StoryBlocked : CampaignProgress.StoryGated;
            }
            var side = catalog.Playable.FirstOrDefault(m => m.IsSolo && !IsComplete(m.Id) && PrerequisiteMet(m));
            if (side != null) return CampaignProgress.SideContentOnly;
            bool anyStoryLeft = catalog.Playable.Any(IsUnfinishedStory);
            return anyStoryLeft ? CampaignProgress.StoryBlocked : CampaignProgress.ImplementedContentComplete;
        }

        /// <summary>Human-readable form of <see cref="Progress"/> for the mission key and the menu.</summary>
        public string DescribeProgress(MissionCatalog catalog)
        {
            switch (Progress(catalog))
            {
                case CampaignProgress.StoryAvailable: return "Next story mission: " + NextPlayable(catalog)?.Id;
                case CampaignProgress.StoryGated: return DescribeGate(NextStory(catalog), catalog);
                case CampaignProgress.SideContentOnly: return "Story is caught up for this build. Optional solo jobs remain: " + NextPlayable(catalog)?.Id;
                case CampaignProgress.StoryBlocked:
                {
                    var story = NextStory(catalog);
                    return story != null && UnavailableGateJobs(story, catalog).Any()
                        ? DescribeGate(story, catalog) + " The story is blocked until that content exists."
                        : "A story mission is waiting on a prerequisite that cannot be met in this build.";
                }
                default: return "All " + catalog.Playable.Count() + " scripted missions are complete. Later chapters are not in this build; replay any job from the mission menu.";
            }
        }

        /// <summary>
        /// What a finished job pays when the bible names no sum (Ron, September 12:
        /// nothing had paid): a main mission's scale by its number, a solo job a
        /// flat fee. The Port Heist's inner parts pay through M22 alone.
        /// </summary>
        public static int DefaultPayout(string id)
        {
            if (string.IsNullOrEmpty(id)) return 0;
            if (PortHeistOperation.Contains(id) && !string.Equals(id, "M22", StringComparison.OrdinalIgnoreCase)) return 0;
            if (id.StartsWith("SM", StringComparison.OrdinalIgnoreCase)) return 20000;
            int number;
            if (id.Length <= 1 || !int.TryParse(id.Substring(1), out number)) return 0;
            // First three jobs fund a starter garage/car choice plus basic parts.
            if (number == 1) return 12000;
            if (number == 2) return 18000;
            if (number == 3) return 25000;
            if (number == 4) return 30000;
            if (number <= 10) return 32000 + 4000 * (number - 6);
            if (number <= 18) return 50000 + 4000 * (number - 11);
            if (number <= 30) return 80000 + 5000 * (number - 23);
            return 4000 + 1500 * number;
        }

        private void AwardCompletion(string id)
        {
            int before = CashOnHand;
            AwardNamed(id);
            if (CashOnHand == before) CashOnHand += DefaultPayout(id);
        }

        private void AwardNamed(string id)
        {
            switch (id)
            {
                case "M03": Safehouses["cypressFoundry"] = true; break;
                case "M05": CashOnHand += 60000; break;
                case "M11": FleetUpgrades["grangerTurbineInstalled"] = true; Safehouses["burroHeightsChopShop"] = true; break;
                case "M14": Safehouses["mckenzieAirfieldHangar"] = true; break;
                case "M15": CashOnHand += 66000; break;
                case "M17": FleetUpgrades["krakenSubmarineReinforced"] = true; break;
                case "M22": Safehouses["cypressFoundry"] = false; AlamoGoldDredgedTons = 0; CashOnHand += 250000; break;
                case "M23": Safehouses["grandSenoraRadarBunker"] = true; break;
                case "M24": AlamoGoldDredgedTons += 5; CashOnHand += 250000; break;
                case "M25": CashOnHand += 85000; break;
                case "M27": CashOnHand += 100000; break;
                case "M28": CashOnHand += 90000; FleetUpgrades["northernRelayDisabled"] = true; break;
                case "M29": CashOnHand += 100000; FleetUpgrades["bunkerFuelReserves"] = true; break;
                case "M30": CashOnHand += 110000; FleetUpgrades["satellitePartsSecured"] = true; break;
                case "M31": CashOnHand += 70000; FleetUpgrades["bunkerPerimeterReady"] = true; break;
                case "M32": CashOnHand += 125000; FleetUpgrades["empCasesSecured"] = true; break;
                case "M33": CashOnHand += 75000; FleetUpgrades["ramosRescued"] = true; break;
                case "M34": CashOnHand += 125000; FleetUpgrades["armoredEscortReady"] = true; break;
                case "M35": CashOnHand += 140000; FleetUpgrades["technicalSupportReady"] = true; break;
                case "M36": CashOnHand += 85000; FleetUpgrades["offshoreSurveyReady"] = true; break;
                case "M37": CashOnHand += 140000; FleetUpgrades["aircraftSmokeReady"] = true; break;
                case "M38": CashOnHand += 160000; FleetUpgrades["seismicStockReady"] = true; break;
                case "M39": CashOnHand += 100000; FleetUpgrades["rigMainlandCableCut"] = true; break;
                case "M40": CashOnHand += 125000; FleetUpgrades["extractionLaunchesReady"] = true; break;
                case "M41": CashOnHand += 110000; FleetUpgrades["bradleyAccessReady"] = true; break;
                case "M42": CashOnHand += 175000; FleetUpgrades["subAirdropReady"] = true; break;
                case "M43": CashOnHand += 90000; FleetUpgrades["offshoreStagingReady"] = true; break;
                case "SM04": CashOnHand += 35000; FleetUpgrades["quarryRadiosRecovered"] = true; break;
                case "SM05": CashOnHand += 35000; FleetUpgrades["estuaryTelemetry"] = true; break;
                case "SM06": CashOnHand += 45000; FleetUpgrades["airfieldFuelReserves"] = true; break;
                case "SM01": FleetUpgrades["armorPiercingSupply"] = true; break;
                case "SM02": FleetUpgrades["surveillanceWormInstalled"] = true; break;
                case "SM03": CashOnHand += 30000; FleetUpgrades["racingTransmissionInstalled"] = true; break;
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
            // Story first. A solo job becoming available does not make it "next";
            // it becomes next only when a gate is waiting on it.
            var story = NextStory(catalog);
            if (story != null)
            {
                var outstanding = OutstandingGateJobs(story, catalog).ToList();
                if (outstanding.Count == 0) return story;
                // The playable required jobs come first. If only unscripted ones
                // remain, there is nothing to offer: the story is blocked and
                // Progress() says why, rather than handing the gate mission over.
                return catalog.Playable.FirstOrDefault(m => outstanding.Contains(m.Id, StringComparer.OrdinalIgnoreCase));
            }
            return catalog.Playable.FirstOrDefault(m => m.IsSolo && !IsComplete(m.Id) && PrerequisiteMet(m));
        }

        /// <summary>The next main mission in order, gate or no gate. Null when none is playable.</summary>
        public MissionDefinition NextStory(MissionCatalog catalog) =>
            catalog.Playable.FirstOrDefault(m => IsUnfinishedStory(m) && PrerequisiteMet(m));

        private bool IsUnfinishedStory(MissionDefinition mission) => !mission.IsSolo &&
            (PortHeistOperation.Contains(mission.Id)
                ? string.Equals(mission.Id, "M19", StringComparison.OrdinalIgnoreCase) && !IsComplete("M22")
                : !IsComplete(mission.Id));

        public bool PrerequisiteMet(MissionDefinition mission)
        {
            string prerequisite = mission.Info.Prerequisite;
            return string.IsNullOrEmpty(prerequisite) || IsComplete(prerequisite);
        }

        // --- story gates ---

        /// <summary>
        /// Solo jobs are optional inside their window and mandatory before the story
        /// event they set up. This is the whole rule, in one place: the main mission
        /// on the left cannot start until every solo on the right is complete.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string[]> StoryGates = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "M19", new[] { "SM01", "SM02", "SM03" } },
            { "M44", new[] { "SM04", "SM05", "SM06" } },
            { "M63", new[] { "SM07", "SM08" } },
            { "M68", new[] { "SM09" } }
        };

        /// <summary>
        /// An older save that is already past a gate is never trapped behind it: the
        /// gate is treated as satisfied once its own mission, or any later main
        /// mission, is complete. The solos stay available as optional content.
        /// </summary>
        public bool GateGrandfathered(string gateMission)
        {
            if (IsComplete(gateMission)) return true;
            if (!TryMainNumber(gateMission, out int gate)) return false;
            return Completed.Any(id => TryMainNumber(id, out int done) && done > gate);
        }

        private static bool TryMainNumber(string id, out int number)
        {
            number = 0;
            return !string.IsNullOrEmpty(id) && id.Length == 3 && (id[0] == 'M' || id[0] == 'm') && int.TryParse(id.Substring(1), out number);
        }

        /// <summary>
        /// The required solo jobs still standing between the player and this mission,
        /// scripted or not. A required job with no script in this build still counts:
        /// the story is then blocked by unavailable content and says so, rather than
        /// quietly opening a gate the design closed.
        /// </summary>
        public IEnumerable<string> OutstandingGateJobs(MissionDefinition mission, MissionCatalog catalog)
        {
            if (mission == null || !StoryGates.TryGetValue(mission.Id, out var required) || GateGrandfathered(mission.Id)) yield break;
            foreach (string solo in required)
                if (!IsComplete(solo)) yield return solo;
        }

        /// <summary>Outstanding required jobs that this build cannot offer because they have no script.</summary>
        public IEnumerable<string> UnavailableGateJobs(MissionDefinition mission, MissionCatalog catalog)
        {
            foreach (string solo in OutstandingGateJobs(mission, catalog))
                if (catalog == null || !catalog.Playable.Any(m => string.Equals(m.Id, solo, StringComparison.OrdinalIgnoreCase)))
                    yield return solo;
        }

        public bool GateSatisfied(MissionDefinition mission, MissionCatalog catalog) =>
            !OutstandingGateJobs(mission, catalog).Any();

        /// <summary>
        /// "M19 needs SM01, SM02 finished first." — or, when a required job cannot be
        /// played in this build, "M63 needs SM08 finished first; SM07 has no script in
        /// this build." Empty when the gate is open.
        /// </summary>
        public string DescribeGate(MissionDefinition mission, MissionCatalog catalog)
        {
            var unavailable = UnavailableGateJobs(mission, catalog).ToList();
            var playable = OutstandingGateJobs(mission, catalog).Where(id => !unavailable.Contains(id, StringComparer.OrdinalIgnoreCase)).ToList();
            if (playable.Count == 0 && unavailable.Count == 0) return "";
            string text = mission.Id + " needs ";
            if (playable.Count > 0) text += string.Join(", ", playable) + " finished first";
            if (unavailable.Count > 0)
                text += (playable.Count > 0 ? "; " : "") + string.Join(", ", unavailable) + (unavailable.Count == 1 ? " has" : " have") + " no script in this build";
            return text + ".";
        }

        public void Reset()
        {
            DiscardAttempt();
            Evidence.Clear(); Cargo.Clear(); CrewVan.Reset();
            Completed.Clear();
            ReadDispatches.Clear(); PhoneHistory.Clear();
            Weapons.Clear(); WeaponPartsOwned.Clear(); WeaponPartsFitted.Clear();
            CharacterMemory.Clear();
            CurrentMissionId = "";
            ActiveAct = 1;
            PrologueComplete = false;
            CashOnHand = 0;
            AlamoGoldDredgedTons = 0f;
            OffshoreEscrowBalance = 0;
            var defaults = new CampaignState(_path);
            Safehouses.Clear();
            foreach (var pair in defaults.Safehouses) Safehouses[pair.Key] = pair.Value;
            FleetUpgrades.Clear();
            Garages.Clear();
            Vehicles.Clear();
            NextVehicleId = 1; VehicleSalesDay = ""; VehicleSalesCount = 0;
            foreach (var pair in defaults.FleetUpgrades) FleetUpgrades[pair.Key] = pair.Value;
            LastHero = Protagonist.StartingSlot;
            LastLocation = Vector3.Zero;
            Save();
        }

        public void Save()
        {
            if (_saveBatch > 0) return;
            var document = new Dictionary<string, object>
            {
                {
                    "campaign", new Dictionary<string, object>
                    {
                        { "currentMissionId", CurrentMissionId },
                        { "completedMissions", Completed.OrderBy(id => id, StringComparer.Ordinal).ToList() },
                        { "activeAct", ActiveAct },
                        { "prologueComplete", PrologueComplete },
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
                { "unlockedSafehouses", (_savedSafehouses ?? Safehouses).ToDictionary(p => p.Key, p => (object)p.Value) },
                { "fleetUpgrades", (_savedUpgrades ?? FleetUpgrades).ToDictionary(p => p.Key, p => (object)p.Value) },
                { "crewVan", CrewVan.ToJson() },
                { "garages", Garages.OrderBy(g => g, StringComparer.Ordinal).Select(g => (object)g).ToList() },
                { "vehicles", Vehicles.Select(v => (object)v.ToJson()).ToList() },
                { "nextVehicleId", NextVehicleId },
                { "vehicleSalesDay", VehicleSalesDay }, { "vehicleSalesCount", VehicleSalesCount },
                { "evidence", (_savedEvidence ?? Evidence).ToDictionary(p => p.Key, p => (object)p.Value) },
                { "cargo", (_savedCargo ?? Cargo).ToDictionary(p => p.Key, p => (object)p.Value) },
                { "phoneHistory", PhoneHistory.Select(n => (object)n.ToJson()).ToList() },
                { "phoneAppOrder", PhoneAppOrder.ToList() },
                { "readDispatches", ReadDispatches.OrderBy(id => id).ToList() },
                { "characterMemory", CharacterMemory },
                { "weaponPartsOwned", WeaponPartsOwned.ToDictionary(p=>p.Key,p=>(object)p.Value.Select(h=>h.ToString()).ToList()) },
                { "weaponPartsFitted", WeaponPartsFitted.ToDictionary(p=>p.Key,p=>(object)p.Value.Select(h=>h.ToString()).ToList()) },
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
