using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;

namespace Bloodlines.Missions
{
    /// <summary>Compare actual entitlements with the accepted attempt's baseline; replays announce no new unlocks.</summary>
    public sealed class CompletionRewards
    {
        private readonly HashSet<string> _completed;
        private readonly Dictionary<CrewSlot, HashSet<uint>> _weapons = new Dictionary<CrewSlot, HashSet<uint>>();
        private readonly HashSet<string> _homes, _fleet;
        private readonly ApartmentTier _tier;
        public CompletionRewards(CampaignState state)
        {
            _completed = new HashSet<string>(state.Completed);
            _homes = Enabled(state.Safehouses); _fleet = Enabled(state.FleetUpgrades); _tier = ApartmentTiers.Current(state);
            foreach (var hero in Protagonist.All)
            {
                var owned = new HashSet<uint>(hero.Loadout.Select(w => (uint)w));
                if (state.Weapons.TryGetValue(hero.Slot.ToString(), out var saved)) owned.UnionWith(saved);
                foreach (string id in WeaponProgression.RewardMissions)
                    if (state.IsComplete(id) && WeaponProgression.ReceivesReward(id, hero.Slot)) owned.Add((uint)WeaponProgression.Rewards(id)[(int)hero.Slot]);
                _weapons[hero.Slot] = owned;
            }
        }
        private static HashSet<string> Enabled(Dictionary<string, bool> flags) => new HashSet<string>(flags.Where(p => p.Value).Select(p => p.Key));
        public List<string> Describe(CampaignState state, int cashBefore)
        {
            var lines = new List<string>();
            int paid = state.CashOnHand - cashBefore;
            if (paid > 0) lines.Add("~g~+$" + paid.ToString("N0") + " crew cash~s~ (now $" + state.CashOnHand.ToString("N0") + ")");
            foreach (var hero in Protagonist.All)
            {
                var earned = new HashSet<uint>();
                foreach (string id in WeaponProgression.RewardMissions)
                    if (!_completed.Contains(id) && state.IsComplete(id) && WeaponProgression.ReceivesReward(id, hero.Slot))
                    {
                        uint hash = (uint)WeaponProgression.Rewards(id)[(int)hero.Slot];
                        if (!_weapons[hero.Slot].Contains(hash)) earned.Add(hash);
                    }
                if (earned.Count > 0) lines.Add("~b~" + hero.DisplayName + " unlocked~s~: " +
                    string.Join(", ", earned.Select(h => WeaponProgression.NameOf((WeaponHash)h))) + ". At the locker.");
            }
            foreach (string key in Enabled(state.Safehouses).Except(_homes)) lines.Add("~g~Base access~s~: " + Label(key));
            foreach (string key in Enabled(state.FleetUpgrades).Except(_fleet)) lines.Add("~g~Upgrade unlocked~s~: " + Label(key));
            if (ApartmentTiers.Current(state) > _tier)
                lines.Add("~g~New homes~s~: " + ApartmentTiers.For(CrewSlot.Guess, ApartmentTiers.Current(state)).Short);
            return lines;
        }
        private static string Label(string key)
        {
            switch (key)
            {
                case "cypressFoundry": return "Cypress Foundry";
                case "grandSenoraRadarBunker": return "Grand Senora bunker";
                case "mckenzieAirfieldHangar": return "McKenzie airfield hangar";
                case "pillboxPenthouse": return "Penthouse access";
                case "grangerTurbineInstalled": return "Granger turbine";
                case "halfTrackAcquired": return "Half-track";
                case "krakenSubmarineReinforced": return "Reinforced Kraken";
                case "racingTransmissionInstalled": return "Racing transmission at Guess's chop bay";
                case "armorPiercingSupply": return "Ice's double rifle-ammunition supply";
                case "northernRelayDisabled": return "Northern relay access";
                case "bunkerFuelReserves": return "Bunker fuel reserves";
                case "satellitePartsSecured": return "Satellite parts";
                case "quarryRadiosRecovered": return "Quarry radios";
                case "estuaryTelemetry": return "Estuary telemetry";
                case "airfieldFuelReserves": return "Airfield fuel reserves";
                case "surveillanceWormInstalled": return "Municipal camera archive access";
                default: return System.Text.RegularExpressions.Regex.Replace(key, "([a-z])([A-Z])", "$1 $2");
            }
        }
    }
}
