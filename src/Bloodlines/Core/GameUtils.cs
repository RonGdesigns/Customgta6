using System;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    public static class GameUtils
    {
        /// <summary>
        /// Streams a model in. Returns false instead of hanging forever when the
        /// model name is wrong — a typo'd ped name is the single most common way a
        /// mission script deadlocks on load.
        /// </summary>
        public static bool RequestModel(Model model, int timeoutMs = 5000)
        {
            if (!model.IsValid)
            {
                Logger.Error("Invalid model requested: " + model.Hash);
                return false;
            }

            if (model.IsLoaded) return true;

            model.Request();
            int waited = 0;
            while (!model.IsLoaded && waited < timeoutMs)
            {
                Script.Wait(50);
                waited += 50;
            }

            if (!model.IsLoaded) Logger.Error("Model timed out loading: " + model.Hash);
            return model.IsLoaded;
        }

        public static void SafeDelete(Entity entity)
        {
            if (entity == null || !entity.Exists()) return;
            try
            {
                entity.Delete();
            }
            catch (Exception ex)
            {
                Logger.Warn("Failed to delete entity " + entity.Handle + ": " + ex.Message);
            }
        }

        public static void SafeRelease(Entity entity)
        {
            if (entity == null || !entity.Exists()) return;
            entity.MarkAsNoLongerNeeded();
        }

        public static void SafeDelete(Blip blip)
        {
            if (blip != null && blip.Exists()) blip.Delete();
        }

        /// <summary>Ground-hugging objective cylinder, drawn per frame.</summary>
        public static void DrawObjectiveMarker(Vector3 position, Color color, float radius = 1.5f)
        {
            ObjectiveMarkers.Show(position);
            World.DrawMarker(
                MarkerType.VerticalCylinder,
                position - new Vector3(0f, 0f, 0.95f),
                Vector3.Zero,
                Vector3.Zero,
                new Vector3(radius, radius, 1.4f),
                color,
                false, false, false, null, null, false);
        }

        public static bool IsWithin(Vector3 a, Vector3 b, float radius)
        {
            return a.DistanceToSquared(b) <= radius * radius;
        }

        /// <summary>Horizontal distance only — useful when the player is on a crane or a rooftop.</summary>
        public static bool IsWithinFlat(Vector3 a, Vector3 b, float radius)
        {
            var flatA = new Vector3(a.X, a.Y, 0f);
            var flatB = new Vector3(b.X, b.Y, 0f);
            return flatA.DistanceToSquared(flatB) <= radius * radius;
        }

        public static void Subtitle(string text, int durationMs = 4000)
        {
            GTA.UI.Screen.ShowSubtitle(text, durationMs);
        }

        public static void Notify(string text)
        {
            GTA.UI.Notification.Show(text);
        }

        public static void PlayFrontendSound(string sound, string set)
        {
            Function.Call(Hash.PLAY_SOUND_FRONTEND, -1, sound, set, true);
        }

        public static void SetClock(int hour, int minute)
        {
            Function.Call(Hash.SET_CLOCK_TIME, hour, minute, 0);
        }

        /// <summary>
        /// Maps the bible's prose weather ("Storm / Breakers", "Foggy Sunrise") onto a
        /// RAGE weather type. Unmatched descriptions leave the weather alone rather
        /// than guessing, so an unrecognised phrase never overrides a deliberate one.
        /// </summary>
        public static void SetWeather(string description)
        {
            if (string.IsNullOrEmpty(description)) return;

            string text = description.ToUpperInvariant();
            string weather = null;

            if (text.Contains("THUNDER") || text.Contains("STORM") || text.Contains("TYPHOON")) weather = "THUNDER";
            else if (text.Contains("RAIN") || text.Contains("DRIZZLE")) weather = "RAIN";
            else if (text.Contains("FOG") || text.Contains("MIST") || text.Contains("SALT FOG")) weather = "FOGGY";
            else if (text.Contains("SMOG") || text.Contains("SMOKE") || text.Contains("DUST") || text.Contains("SAND")) weather = "SMOG";
            else if (text.Contains("OVERCAST") || text.Contains("SHALE")) weather = "OVERCAST";
            else if (text.Contains("SUNRISE") || text.Contains("CLEARING")) weather = "CLEARING";
            else if (text.Contains("BLIZZARD") || text.Contains("SNOW")) weather = "XMAS";
            else if (text.Contains("CLEAR") || text.Contains("SUN") || text.Contains("GLARE") || text.Contains("HEAT")) weather = "EXTRASUNNY";

            if (weather == null) return;

            Function.Call(Hash.SET_WEATHER_TYPE_NOW, weather);
        }

        public static void FadeOut(int ms)
        {
            Function.Call(Hash.DO_SCREEN_FADE_OUT, ms);
        }

        public static void FadeIn(int ms)
        {
            Function.Call(Hash.DO_SCREEN_FADE_IN, ms);
        }

        public static bool IsScreenFadedOut()
        {
            return Function.Call<bool>(Hash.IS_SCREEN_FADED_OUT);
        }
    }
}
