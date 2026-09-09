using System;
using GTA;
namespace Bloodlines.Crew
{
    public static class CrewDurability
    {
        public const int Health = 900;
        public const int Armor = 100;
        public static void RestoreAfterSwitch(Ped ped, int health, int armor)
        {
            if (ped == null || !ped.Exists() || ped.IsDead) return;
            ped.MaxHealth = Health;
            ped.CanSufferCriticalHits = false;
            ped.Health = Math.Max(1, Math.Min(Health, health));
            ped.Armor = Math.Max(0, Math.Min(Armor, armor));
        }
    }
}
