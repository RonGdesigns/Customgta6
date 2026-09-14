using GTA;
using GTA.Math;
namespace Bloodlines.Core
{
    public static class AircraftSmoke
    {
        /// <summary>How many times the particle dictionary is asked for before giving up.</summary>
        public const int RequestAttempts = 5;
        public const int RequestWaitMs = 400;

        public static bool Emit(Vehicle aircraft)
        {
            if (aircraft == null || !aircraft.Exists() || aircraft.IsDead) return false;
            var asset = new ParticleEffectAsset("core");
            try
            {
                // One 1-second request was the whole budget, and a dictionary that had
                // not streamed yet made this return false. M37 turned that into a failed
                // mission. Ask properly instead: a few attempts across frames.
                bool ready = false;
                for (int attempt = 0; attempt < RequestAttempts && !ready; attempt++)
                {
                    ready = asset.Request(RequestWaitMs);
                    if (!ready) Script.Wait(RequestWaitMs);
                }
                if (!ready)
                {
                    Logger.Warn("Smoke: the core particle dictionary did not stream in after " +
                        (RequestAttempts * RequestWaitMs * 2) + " ms.");
                    return false;
                }
                return World.CreateParticleEffectNonLooped(asset, "exp_grd_grenade_smoke",
                    aircraft.Position - aircraft.ForwardVector * 5f, Vector3.Zero, 1.6f);
            }
            finally { asset.MarkAsNoLongerNeeded(); }
        }
    }
}
