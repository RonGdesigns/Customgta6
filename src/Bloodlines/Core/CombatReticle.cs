using GTA;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>A small high-contrast aiming aid, suppressed outside ordinary armed aiming.</summary>
    public static class CombatReticle
    {
        public static void Draw(bool enabled, bool blocked)
        {
            var ped = Game.Player.Character;
            if (!enabled || blocked || ped == null || !ped.Exists() || ped.IsDead || !Game.Player.CanControlCharacter ||
                Game.IsPaused || !ControllerInput.Pressed(Control.Aim) || ped.IsInVehicle() ||
                Function.Call<uint>(Hash.GET_WEAPONTYPE_GROUP, Function.Call<uint>(Hash.GET_SELECTED_PED_WEAPON, ped)) == unchecked((uint)Game.GenerateHash("GROUP_SNIPER")) ||
                Function.Call<uint>(Hash.GET_SELECTED_PED_WEAPON, ped) == (uint)WeaponHash.Unarmed) return;
            float aspect = Function.Call<float>(Hash.GET_ASPECT_RATIO, false);
            if (aspect < 1f) aspect = 16f / 9f;
            float px = .0012f / aspect, py = .0012f;
            // Outline and arms stay visible against pale skies and dark interiors.
            for (int pass = 0; pass < 2; pass++)
            {
                int color = pass == 0 ? 0 : 255;
                float thickness = pass == 0 ? 3f : 1.3f;
                foreach (int sign in new[] { -1, 1 })
                {
                    Function.Call(Hash.DRAW_RECT, .5f + sign * 5f * px, .5f, 5f * px, thickness * py, color, color, color, 230, false);
                    Function.Call(Hash.DRAW_RECT, .5f, .5f + sign * 5f * py, thickness * px, 5f * py, color, color, color, 230, false);
                }
            }
        }
    }
}
