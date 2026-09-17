using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>
    /// What one attempt at a mission came to: how long it took, who did the shooting, how
    /// much the crew bled, and what it paid.
    ///
    /// Finishing a mission used to be a title and a fade. The numbers that make a replay
    /// feel like it counted were all in the game already - the hostiles a mission tracks,
    /// the brother in play, the cash before and after the reward commits - and nothing
    /// added them up. This does, once a tick, from what the mission already owns; it holds
    /// no entities and issues no natives that change anything.
    ///
    /// **A kill is credited to whoever was in play when it was noticed.** That is an honest
    /// approximation, not a forensic one: a hostile shot by a companion while the player
    /// holds another brother goes to the player's brother. Say "credited", never "scored".
    /// </summary>
    public sealed class MissionTally
    {
        /// <summary>The bone the engine reports for a shot to the head.</summary>
        public const int HeadBone = 31086;
        /// <summary>The relationship groups whose dead are counted as kills.</summary>
        public static readonly string[] HostileGroups =
            { "BLOODLINES_AEGIS", "BLOODLINES_CARTEL", "BLOODLINES_MILITARY", "BLOODLINES_TARGET" };

        public sealed class Result
        {
            public string MissionId = "", Title = "";
            public int Seconds, Kills, Headshots, Switches, DamageTaken, Payout;
            public readonly Dictionary<CrewSlot, int> KillsBy = new Dictionary<CrewSlot, int>();
            public int CampaignCompleted, CampaignTotal, CashOnHand;
            public bool FirstCompletion;

            public string Clock => Seconds / 60 + ":" + (Seconds % 60).ToString("00");

            /// <summary>The credit line, in the crew's fixed order, only naming brothers who have a kill.</summary>
            public string Credits()
            {
                var parts = new List<string>();
                foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Gohan, CrewSlot.Guess })
                {
                    int n;
                    if (KillsBy.TryGetValue(slot, out n) && n > 0) parts.Add(Protagonist.Of(slot).Handle + " " + n);
                }
                return parts.Count == 0 ? "" : string.Join("  ·  ", parts.ToArray());
            }
        }

        private Result _result;
        private int _startedAt, _cashBefore, _lastHealth, _lastArmor;
        private CrewSlot? _lastSlot;
        private readonly HashSet<int> _counted = new HashSet<int>();
        private readonly List<object> _groups = new List<object>();

        public bool IsOpen => _result != null && _startedAt >= 0;
        /// <summary>The last finished result, kept until the next attempt begins.</summary>
        public Result Last { get; private set; }

        public void Begin(string missionId, string title, int cashOnHand)
        {
            _result = new Result { MissionId = missionId ?? "", Title = title ?? "" };
            _startedAt = Game.GameTime;
            _cashBefore = cashOnHand;
            _counted.Clear();
            _lastSlot = null;
            _lastHealth = _lastArmor = -1;
            _groups.Clear();
            try { foreach (var name in HostileGroups) _groups.Add(World.AddRelationshipGroup(name)); }
            catch (Exception ex) { Logger.Warn("Tally could not resolve the hostile groups: " + ex.Message); }
        }

        /// <summary>Once a tick while the mission runs. Reads only; never touches an entity.</summary>
        public void Update(Mission mission, CrewRoster crew)
        {
            if (!IsOpen || mission == null || crew == null) return;
            try
            {
                var slot = crew.ActiveSlot;
                if (_lastSlot.HasValue && _lastSlot.Value != slot) { _result.Switches++; _lastHealth = _lastArmor = -1; }
                _lastSlot = slot;

                var player = Game.Player.Character;
                if (player != null && player.Exists() && !player.IsDead)
                {
                    // Damage is what came off the man in play. The first sample after a switch
                    // is a baseline, never a hit: the new brother's health is his own.
                    if (_lastHealth >= 0 && player.Health < _lastHealth) _result.DamageTaken += _lastHealth - player.Health;
                    if (_lastArmor >= 0 && player.Armor < _lastArmor) _result.DamageTaken += _lastArmor - player.Armor;
                    _lastHealth = player.Health; _lastArmor = player.Armor;
                }

                foreach (var entity in mission.Staged)
                {
                    var ped = entity as Ped;
                    if (ped == null || !ped.Exists() || !ped.IsDead || _counted.Contains(ped.Handle)) continue;
                    if (!Hostile(ped)) continue;
                    _counted.Add(ped.Handle);
                    _result.Kills++;
                    int had;
                    _result.KillsBy.TryGetValue(slot, out had);
                    _result.KillsBy[slot] = had + 1;
                    if (Headshot(ped)) _result.Headshots++;
                }
            }
            catch (Exception ex) { Logger.Warn("Mission tally skipped a tick: " + ex.Message); }
        }

        private bool Hostile(Ped ped)
        {
            object group = ped.RelationshipGroup;
            foreach (var hostile in _groups) if (Equals(group, hostile)) return true;
            return false;
        }

        private static bool Headshot(Ped ped)
        {
            try
            {
                var bone = new OutputArgument();
                return Function.Call<bool>(Hash.GET_PED_LAST_DAMAGE_BONE, ped, bone) && bone.GetResult<int>() == HeadBone;
            }
            catch { return false; }
        }

        /// <summary>Close the attempt with what it paid and where the campaign stands.</summary>
        public Result Finish(int cashOnHand, int completed, int total, bool firstCompletion)
        {
            if (_result == null) return Last;
            _result.Seconds = Math.Max(0, (Game.GameTime - _startedAt) / 1000);
            _result.Payout = Math.Max(0, cashOnHand - _cashBefore);
            _result.CashOnHand = cashOnHand;
            _result.CampaignCompleted = completed;
            _result.CampaignTotal = total;
            _result.FirstCompletion = firstCompletion;
            Last = _result;
            _result = null;
            _startedAt = -1;
            return Last;
        }

        /// <summary>An attempt that did not pass is simply dropped; nothing is shown for it.</summary>
        public void Discard() { _result = null; _startedAt = -1; }
    }
}
