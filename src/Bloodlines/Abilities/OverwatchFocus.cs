using Bloodlines.Crew;
using GTA;
using GTA.Native;

namespace Bloodlines.Abilities
{
    /// <summary>
    /// Ice — "Overwatch Focus". Steadier gun, harder-hitting rounds, and a light
    /// time dilation so long-range work off a crane or a rooftop stays readable.
    /// </summary>
    public sealed class OverwatchFocus : Ability
    {
        public override CrewSlot Slot => CrewSlot.Ice;
        public override string Name => "Overwatch Focus";

        public override void Activate(Ped player)
        {
            Function.Call(Hash.SET_PLAYER_WEAPON_DAMAGE_MODIFIER, Game.Player, 1.75f);
            Function.Call(Hash.SET_PLAYER_WEAPON_DEFENSE_MODIFIER, Game.Player, 0.7f);
            Function.Call(Hash.SET_TIME_SCALE, 0.65f);
            Function.Call(Hash.ANIMPOSTFX_PLAY, "FocusIn", 0, false);
            player.CanSufferCriticalHits = false;
        }

        public override void Update(Ped player)
        {
            // Recoil and sway are per-frame values; setting them once does nothing.
            Function.Call(Hash.SET_PLAYER_WEAPON_DAMAGE_MODIFIER, Game.Player, 1.75f);
            Function.Call(Hash.SPECIAL_ABILITY_FILL_METER, Game.Player, true);
            Function.Call(Hash.SET_PED_SHOOT_RATE, player, 150);
        }

        public override void Deactivate(Ped player)
        {
            Function.Call(Hash.SET_PLAYER_WEAPON_DAMAGE_MODIFIER, Game.Player, 1.0f);
            Function.Call(Hash.SET_PLAYER_WEAPON_DEFENSE_MODIFIER, Game.Player, 1.0f);
            Function.Call(Hash.SET_TIME_SCALE, 1.0f);
            Function.Call(Hash.ANIMPOSTFX_STOP, "FocusIn");
            Function.Call(Hash.SET_PED_SHOOT_RATE, player, 100);
        }
    }
}
