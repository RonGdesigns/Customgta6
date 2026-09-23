using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Bloodlines.Core;
using GTA;
using GTA.Native;

namespace Bloodlines.Missions.Objectives
{
    /// <summary>
    /// Bring an aircraft down by wearing out a hull meter the HUD draws, instead of by
    /// whatever its handling data says it can absorb.
    ///
    /// Ron, September 22: shooting down the Osprey from a gun truck "would take a lot, but
    /// maybe we should have like a health meter." The Avenger's own armor is an unknown
    /// number from an Online vehicle; a mission balanced on it is balanced on a guess. So the
    /// meter counts <em>hits</em>, not damage: every frame the crew's gun, the truck's
    /// turret or a brother's rifle has registered a hit takes <see cref="BulletHit"/>, and a
    /// rocket or grenade takes <see cref="HeavyHit"/>. Until the meter is empty the aircraft is
    /// kept flying - its engine, body and tank are topped back up each frame and a single
    /// explosion cannot finish it - and when the meter reaches zero it is blown up. Whatever
    /// the numbers turn out to be in play, they are two constants in one place.
    /// </summary>
    public sealed class ShootDownObjective : Objective
    {
        /// <summary>One frame of gunfire landing on it. About 170 of those empty the meter.</summary>
        public const float BulletHit = 0.006f;
        /// <summary>A rocket or a grenade. Seven of those empty it.</summary>
        public const float HeavyHit = 0.15f;

        private static readonly WeaponHash[] Heavy =
        {
            WeaponHash.RPG, WeaponHash.HomingLauncher, WeaponHash.GrenadeLauncher, WeaponHash.CompactGrenadeLauncher,
            WeaponHash.StickyBomb, WeaponHash.Grenade, WeaponHash.PipeBomb,
        };

        private readonly Func<Vehicle> _target;
        private readonly Func<IEnumerable<Entity>> _hunters;
        private bool _downed;

        /// <summary>How much of the hull is left, from 1 to 0.</summary>
        public float Hull { get; private set; } = 1f;
        public int Hits { get; private set; }
        public int HeavyHits { get; private set; }

        public ShootDownObjective(string label, Func<Vehicle> target, Func<IEnumerable<Entity>> hunters) : base(label)
        { _target = target; _hunters = hunters; }

        /// <summary>Any brother's hit counts; nobody has to switch to be credited with it.</summary>
        public override bool KeepsOwnerOpen => true;

        public override void Enter(MissionContext c)
        {
            base.Enter(c);
            Hull = 1f; Hits = 0; HeavyHits = 0; _downed = false;
            var target = _target();
            // One rocket must not end it before the meter says so.
            if (target != null && target.Exists())
                Function.Call(Hash.SET_VEHICLE_EXPLODES_ON_HIGH_EXPLOSION_DAMAGE, target, false);
        }

        public override void Update(MissionContext c)
        {
            var target = _target();
            if (target == null || !target.Exists()) { Fail("The aircraft failed to load. Restart the mission."); return; }
            if (target.IsDead || !target.IsDriveable) { Hull = 0f; Complete(); return; }

            GameUtils.DrawObjectiveMarker(target.Position, Color.FromArgb(120, 224, 74, 62), DestroyVehicleObjective.MarkerRadius(target));
            ObjectiveMarkers.Navigation(target.Position, RequiredCharacter, null, road: false);

            bool heavy = Heavy.Any(weapon => Function.Call<bool>(Hash.HAS_ENTITY_BEEN_DAMAGED_BY_WEAPON, target, (uint)weapon, 0));
            bool hit = heavy;
            if (!hit)
                foreach (var hunter in _hunters?.Invoke() ?? Enumerable.Empty<Entity>())
                    if (hunter != null && hunter.Exists() && Function.Call<bool>(Hash.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY, target, hunter, true))
                    { hit = true; break; }
            if (hit)
            {
                Hull = Math.Max(0f, Hull - (heavy ? HeavyHit : BulletHit));
                if (heavy) HeavyHits++; else Hits++;
                Function.Call(Hash.CLEAR_ENTITY_LAST_DAMAGE_ENTITY, target);
                // The weapon record is separate and is not cleared by the line above. Left
                // set, one rocket read as a rocket every frame and emptied the meter in seven
                // (the September 22 audit).
                Function.Call(Hash.CLEAR_ENTITY_LAST_WEAPON_DAMAGE, target);
            }

            if (Hull > 0f)
            {
                // Kept in the air until the meter says otherwise.
                target.EngineHealth = 1000f;
                target.BodyHealth = 1000f;
                Function.Call(Hash.SET_VEHICLE_PETROL_TANK_HEALTH, target, 1000f);
                return;
            }
            if (_downed) return;
            _downed = true;
            Logger.Info("Aircraft brought down: " + Hits + " gunfire hits and " + HeavyHits + " explosive hits.");
            Function.Call(Hash.SET_VEHICLE_EXPLODES_ON_HIGH_EXPLOSION_DAMAGE, target, true);
            Function.Call(Hash.EXPLODE_VEHICLE, target, true, false);
        }
    }
}
