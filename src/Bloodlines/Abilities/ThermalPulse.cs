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
        private const float ScanRadius = 60f;

        public override CrewSlot Slot => CrewSlot.Ice;
        public override string Name => "Thermal Pulse";

        public override void Activate(Ped player)
        {
            Function.Call(Hash.SET_SEETHROUGH, true);
            Function.Call(Hash.ANIMPOSTFX_PLAY, "MP_Bull_Tost", 0, false);
        }

        public override void Update(Ped player)
        {
            foreach (var ped in World.GetNearbyPeds(player, ScanRadius))
            {
                if (ped == null || !ped.Exists() || ped.IsDead) continue;
                if (ped.Handle == player.Handle) continue;

                bool hostile = ped.GetRelationshipWithPed(player) == Relationship.Hate;

                // Chevron over every tracked body: red for hostile, cool gray for
                // civilians, so a breach can be planned without shooting the crew.
                World.DrawMarker(
                    MarkerType.UpsideDownCone,
                    ped.Position + new Vector3(0f, 0f, 1.25f),
                    Vector3.Zero, Vector3.Zero,
                    new Vector3(0.35f, 0.35f, 0.35f),
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
