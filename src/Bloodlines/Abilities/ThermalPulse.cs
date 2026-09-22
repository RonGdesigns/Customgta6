using System;
using System.Drawing;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Abilities
{
    /// <summary>
    /// Ice — "Thermal Pulse". See-through vision plus a live outline on every body in
    /// range, so a shot can be taken at something he can actually account for.
    ///
    /// This was Gohan's until Ron moved it on September 14, 2026: Ice is the shooter
    /// and does most of the long-range work, and a thermal sight is a marksman's
    /// instrument rather than a technician's. It replaced "Overwatch Focus", which
    /// described itself as a steadier gun for work off a crane or a rooftop and was
    /// in fact a flat damage multiplier with slow motion — nothing in it steadied
    /// anything. Gohan took <see cref="Blackout"/> in exchange.
    ///
    /// Two authored mission descriptions still say Gohan uses this: M04 and the solo
    /// SM02. The bible extraction is not edited to follow gameplay, so the adaptation
    /// is recorded in data/mission_gameplay.tsv instead.
    /// </summary>
    public sealed class ThermalPulse : Ability
    {
        /// <summary>How far every body is outlined, the crew's own and bystanders included.</summary>
        public const float ScanRadius = 60f;
        /// <summary>
        /// How far a hostile is marked. Ice is the long gun: in M05 the generator crew is about
        /// 270 m below his perch, and a sight that outlined nobody past 60 m was a sight he
        /// could not use on that job (Ron, September 22). Beyond the scan radius only people
        /// who want him dead are marked, so a city block does not turn into a field of cones.
        /// </summary>
        public const float SniperRadius = 300f;

        public override CrewSlot Slot => CrewSlot.Ice;
        public override string Name => "Thermal Pulse";

        public override void Activate(Ped player)
        {
            Function.Call(Hash.SET_SEETHROUGH, true);
            Function.Call(Hash.ANIMPOSTFX_PLAY, "MP_Bull_Tost", 0, false);
        }

        public override void Update(Ped player)
        {
            foreach (var ped in World.GetNearbyPeds(player, SniperRadius))
            {
                if (ped == null || !ped.Exists() || ped.IsDead) continue;
                if (ped.Handle == player.Handle) continue;

                bool hostile = ped.GetRelationshipWithPed(player) == Relationship.Hate;
                float away = ped.Position.DistanceTo(player.Position);
                if (!hostile && away > ScanRadius) continue;
                // A marker the size of a hand is invisible at 270 m, scope or no scope. It
                // grows with range so it reads at the same size on screen.
                float size = 0.35f * Math.Max(1f, away / 40f);

                // Chevron over every tracked body: red for hostile, cool gray for
                // civilians, so a breach can be planned without shooting the crew.
                World.DrawMarker(
                    MarkerType.UpsideDownCone,
                    ped.Position + new Vector3(0f, 0f, 0.9f + size),
                    Vector3.Zero, Vector3.Zero,
                    new Vector3(size, size, size),
                    hostile ? Color.FromArgb(190, 224, 74, 62) : Color.FromArgb(140, 150, 168, 186),
                    false, false, false, null, null, false);
            }
        }

        public override void Deactivate(Ped player)
        {
            Function.Call(Hash.SET_SEETHROUGH, false);
            Function.Call(Hash.ANIMPOSTFX_STOP, "MP_Bull_Tost");
        }
    }
}
