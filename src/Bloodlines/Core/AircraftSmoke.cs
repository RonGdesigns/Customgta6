using GTA;
using GTA.Math;
namespace Bloodlines.Core
{
    public static class AircraftSmoke
    {
        public static bool Emit(Vehicle aircraft)
        {
            if (aircraft == null || !aircraft.Exists() || aircraft.IsDead) return false;
            var asset = new ParticleEffectAsset("core");
            try
            {
                if (!asset.Request(1000)) return false;
                return World.CreateParticleEffectNonLooped(asset, "exp_grd_grenade_smoke",
                    aircraft.Position - aircraft.ForwardVector * 5f, Vector3.Zero, 1.6f);
            }
            finally { asset.MarkAsNoLongerNeeded(); }
        }
    }
}
