using System;
using GTA;
using GTA.Math;

namespace Bloodlines.Core
{
    /// <summary>
    /// The flame out of a car's exhaust, for as long as something is holding it on.
    ///
    /// This is the effect the game already fires when a car changes gear — <c>veh_backfire</c>
    /// out of the <c>core</c> particle dictionary — asked for repeatedly instead of once. Ron
    /// asked for the nitrous to shoot fire continuously and pointed at that gear-change flame
    /// as the thing to extend, which is exactly what this does: same effect, same dictionary,
    /// on a cadence.
    ///
    /// **It never waits.** <see cref="AircraftSmoke"/> asks for the dictionary with
    /// <c>Script.Wait</c> between attempts, which is right for a mission calling it once and
    /// completely wrong here: this runs inside the per-frame input step, and stalling the
    /// script thread for two seconds to load a particle asset would hitch the whole mod. It
    /// asks every frame and emits once the asset is ready — a flame that starts a few frames
    /// late is a flame; a stalled script is a bug report.
    ///
    /// **The pipes are found geometrically, not from a bone.** A vehicle's <c>exhaust</c> bone
    /// exists on most cars and not all, and a missing bone would mean no flame with nothing
    /// said about it. The rear face of the car and its own width give two positions that are
    /// right for a twin-pipe car and close enough for a single, on every model.
    /// </summary>
    public static class ExhaustFlame
    {
        /// <summary>The gear-change flame, in the dictionary the mod already streams.</summary>
        public const string Asset = "core";
        public const string Effect = "veh_backfire";
        /// <summary>How often a burst goes out. Short enough to read as continuous fire.</summary>
        public const int BurstIntervalMs = 70;
        /// <summary>How big. The gear-change flame is about this, and bigger reads as an explosion.</summary>
        public const float Scale = 1.35f;
        /// <summary>How far apart the two pipes are, as a fraction of the car's width.</summary>
        public const float PipeSpread = .28f;

        private static int _nextBurst;
        private static bool _ready, _warned;

        /// <summary>Whether the dictionary has streamed in and bursts are actually going out.</summary>
        public static bool Ready => _ready;

        /// <summary>
        /// Hold the flame on. Call every frame while it should be burning; stop calling and it
        /// stops. Returns true on the frames a burst actually went out, so a caller can say so.
        /// </summary>
        public static bool Hold(Vehicle car)
        {
            if (car == null || !car.Exists() || car.IsDead) return false;
            if (!_ready)
            {
                // Asked for, never waited on. Ready on a later frame is fine.
                var asset = new ParticleEffectAsset(Asset);
                try { _ready = asset.Request(0); }
                catch (Exception ex)
                {
                    if (!_warned) { _warned = true; Logger.Warn("Exhaust flame: the " + Asset + " dictionary could not be requested: " + ex.Message); }
                    return false;
                }
                if (!_ready) return false;
                Logger.Info("Exhaust flame: the " + Asset + " dictionary is in; " + Effect + " is available.");
            }
            if (Game.GameTime < _nextBurst) return false;
            _nextBurst = Game.GameTime + BurstIntervalMs;

            var asset2 = new ParticleEffectAsset(Asset);
            try
            {
                bool any = false;
                foreach (var at in Pipes(car))
                    any |= World.CreateParticleEffectNonLooped(asset2, Effect, at, Vector3.Zero, Scale);
                return any;
            }
            catch (Exception ex) { Logger.Error("Exhaust flame", ex); return false; }
            finally { asset2.MarkAsNoLongerNeeded(); }
        }

        /// <summary>
        /// Where the pipes are: off the back of the car, one either side of center. Derived
        /// from the model's own dimensions so it lands on the bumper of a hatchback and the
        /// bumper of a semi rather than inside one and behind the other.
        /// </summary>
        private static Vector3[] Pipes(Vehicle car)
        {
            Vector3 back = car.Position - car.ForwardVector * Length(car) - new Vector3(0f, 0f, .35f);
            var side = car.RightVector * (Width(car) * PipeSpread);
            return new[] { back - side, back + side };
        }

        private static float Length(Vehicle car)
        {
            try
            {
                var size = car.Model.Dimensions;
                float length = (size.frontTopRight.Y - size.rearBottomLeft.Y) * .5f;
                // A model that will not report its size is assumed to be a car, rather than
                // having its flame put at its own origin - which is the mistake that made
                // M43's laptop invisible inside the surface it was stacked on.
                return length > .5f ? length : 2.4f;
            }
            catch { return 2.4f; }
        }

        private static float Width(Vehicle car)
        {
            try
            {
                var size = car.Model.Dimensions;
                float width = size.frontTopRight.X - size.rearBottomLeft.X;
                return width > .5f ? width : 1.8f;
            }
            catch { return 1.8f; }
        }

        /// <summary>Teardown, so a fresh session asks for the dictionary again.</summary>
        public static void Reset() { _nextBurst = 0; _ready = false; _warned = false; }
    }
}
