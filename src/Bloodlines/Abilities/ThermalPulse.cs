using System.Drawing;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Abilities
{
    /// <summary>
    /// Gohan — "Thermal Pulse". See-through vision plus a live outline on every
    /// hostile in range, which is what makes the breaching missions playable
    /// without the player memorising guard patrols.
    /// </summary>
    public sealed class ThermalPulse : Ability
    {
        private const float ScanRadius = 60f;

        public override CrewSlot Slot => CrewSlot.Gohan;
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

                // Chevron over every tracked body: red for hostile, cool grey for
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
