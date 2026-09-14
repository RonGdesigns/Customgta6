using System;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Native;

namespace Bloodlines.Abilities
{
    /// <summary>
    /// Gohan — "Blackout". The lights go out, and if he walked into the dark clean,
    /// what he does in it is not seen.
    ///
    /// Ron specified this on September 14, 2026, when Ice took the thermal sight.
    /// The rule that makes it his rather than a damage buff: heat earned during a
    /// blackout is <em>deferred, not canceled</em>. Kill the lights with no stars, do
    /// the work, and the wanted level that work would have earned is held. Leave
    /// nobody able to report it and the held heat is dropped. Be seen afterwards and
    /// it lands on him in full.
    ///
    /// Walking in already wanted means the lights still go out and nothing is
    /// concealed: the city is not going to forget what it is already chasing.
    /// </summary>
    public sealed class Blackout : Ability
    {
        /// <summary>The name this ability holds the city's lights under.</summary>
        public const string LightOwner = "Gohan.Blackout";
        /// <summary>
        /// How long after the dark lifts he can still be caught for what he did in it.
        /// Survive this unseen and the held heat is gone; this is the drain Ron described.
        /// </summary>
        public const int ConcealmentGraceMs = 10000;
        /// <summary>How close someone has to be to recognize him once the lights are back.</summary>
        public const float WitnessRange = 60f;

        private bool _concealing;
        private int _deferred;
        private int _graceUntil;

        public override CrewSlot Slot => CrewSlot.Gohan;
        public override string Name => "Blackout";

        /// <summary>True when he entered clean, so the dark is actually hiding him.</summary>
        public bool Concealing => _concealing;
        /// <summary>The wanted level being held back, waiting to see whether anyone reports it.</summary>
        public int DeferredWanted => _deferred;

        public override void Activate(Ped player)
        {
            WorldLights.Darken(LightOwner);
            // Mission hostiles stop resolving what they see. A bullet still reaches
            // them; see GuardAwareness, which is why that had to be built first.
            GuardAwareness.SuppressAll = true;

            _deferred = 0;
            _concealing = Game.Player.WantedLevel <= 0;
            if (_concealing)
            {
                Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, true);
                Function.Call(Hash.SET_EVERYONE_IGNORE_PLAYER, Game.Player, true);
                GameUtils.Notify("~b~Blackout.~s~ Nothing you do in the dark is being seen.");
            }
            else
            {
                GameUtils.Notify("~y~Blackout.~s~ The lights are out, but they already know who they are looking for.");
            }
        }

        public override void Update(Ped player)
        {
            if (!_concealing) return;
            // Hold back what the dark earned him rather than wiping it. This is the
            // whole mechanic: the heat exists, it just has not been pinned on him.
            int level = Game.Player.WantedLevel;
            if (level > 0)
            {
                _deferred = Math.Max(_deferred, level);
                Game.Player.WantedLevel = 0;
                Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, Game.Player, false);
            }
        }

        public override void Deactivate(Ped player)
        {
            WorldLights.Restore(LightOwner);
            GuardAwareness.SuppressAll = false;
            Function.Call(Hash.SET_POLICE_IGNORE_PLAYER, Game.Player, false);
            Function.Call(Hash.SET_EVERYONE_IGNORE_PLAYER, Game.Player, false);
            _graceUntil = Game.GameTime + ConcealmentGraceMs;
            if (_deferred > 0)
                GameUtils.Notify("~y~The lights are back. Stay out of sight and what you did stays unreported.");
        }

        /// <summary>
        /// The window after the dark. Held heat stays held while nobody can see him;
        /// the moment someone can, it is his. Nothing to hold means nothing to settle.
        /// </summary>
        public override bool Settle(Ped player)
        {
            if (_deferred <= 0) { _concealing = false; return false; }
            if (player == null || !player.Exists())
            { _deferred = 0; _concealing = false; return false; }

            // While the window runs the heat is off him, which is the cooldown draining.
            if (Game.Player.WantedLevel > 0) Game.Player.WantedLevel = 0;

            if (Witnessed(player))
            {
                Game.Player.WantedLevel = _deferred;
                Function.Call(Hash.SET_PLAYER_WANTED_LEVEL_NOW, Game.Player, false);
                GameUtils.Notify("~r~Seen.~s~ Everything from the blackout just caught up with you.");
                Logger.Info("Blackout: witnessed after the lights returned; " + _deferred + " stars applied.");
                _deferred = 0; _concealing = false;
                return false;
            }

            if (Game.GameTime >= _graceUntil)
            {
                GameUtils.Notify("~g~Nobody left to tell it.~s~ The blackout goes unreported.");
                Logger.Info("Blackout: the grace elapsed unseen; " + _deferred + " stars dropped.");
                _deferred = 0; _concealing = false;
                return false;
            }
            return true;
        }

        /// <summary>
        /// Anyone alive nearby who would recognize him and could say so: a cop, or
        /// something that already hates him. Facing is not required — this is being
        /// recognized in the open, not spotted by a guard on a post.
        /// </summary>
        private static bool Witnessed(Ped player)
        {
            foreach (var ped in World.GetNearbyPeds(player, WitnessRange))
            {
                if (ped == null || !ped.Exists() || ped.IsDead) continue;
                if (ped.Handle == player.Handle) continue;
                bool cop = Function.Call<int>(Hash.GET_PED_TYPE, ped) == 6;
                if (!cop && ped.GetRelationshipWithPed(player) != Relationship.Hate) continue;
                if (!Function.Call<bool>(Hash.HAS_ENTITY_CLEAR_LOS_TO_ENTITY, ped, player, 17)) continue;
                return true;
            }
            return false;
        }
    }
}
