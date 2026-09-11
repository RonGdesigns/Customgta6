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

        /// <summary>
        /// The nearest road node to a point, within a bound: a road node is never in
        /// a wall, which an estimated key can be. False when none is close enough.
        /// </summary>
        public static bool NearestRoadNode(Vector3 near, float maxDistance, out Vector3 point, out float heading)
        {
            point = near; heading = 0f;
            var node = new OutputArgument(); var nodeHeading = new OutputArgument();
            if (!Function.Call<bool>(Hash.GET_CLOSEST_VEHICLE_NODE_WITH_HEADING, near.X, near.Y, near.Z, node, nodeHeading, 1, 3f, 0f)) return false;
            var found = node.GetResult<Vector3>();
            if (found.DistanceTo(near) > maxDistance) return false;
            point = found; heading = nodeHeading.GetResult<float>();
            return true;
        }

        private sealed class HeldVehicle { public Vehicle Vehicle; public int Until; }
        private static readonly System.Collections.Generic.List<HeldVehicle> Held = new System.Collections.Generic.List<HeldVehicle>();

        /// <summary>
        /// A vehicle created before the ground under it has streamed in falls through
        /// the map and pops back up when the collision arrives (Ron, September 11:
        /// Ron's start car). Hold it frozen until the collision is loaded around it,
        /// then set it on the ground; a bounded wait so nothing stays pinned.
        /// </summary>
        public static void HoldUntilGrounded(Vehicle vehicle, int maxMs = 3000)
        {
            if (vehicle == null || !vehicle.Exists()) return;
            var p = vehicle.Position;
            Function.Call(Hash.REQUEST_COLLISION_AT_COORD, p.X, p.Y, p.Z);
            if (Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY, vehicle)) { vehicle.PlaceOnGround(); return; }
            vehicle.IsPositionFrozen = true;
            Held.Add(new HeldVehicle { Vehicle = vehicle, Until = Game.GameTime + maxMs });
            Logger.Info("Holding a fresh " + vehicle.DisplayName + " at " + p + " until the ground under it loads.");
        }

        /// <summary>Each tick: release held vehicles whose ground has loaded (or whose wait is up) onto the ground.</summary>
        public static void SettleHeld()
        {
            for (int i = Held.Count - 1; i >= 0; i--)
            {
                var held = Held[i]; var vehicle = held.Vehicle;
                if (vehicle == null || !vehicle.Exists()) { Held.RemoveAt(i); continue; }
                bool loaded = Function.Call<bool>(Hash.HAS_COLLISION_LOADED_AROUND_ENTITY, vehicle);
                if (!loaded && Game.GameTime < held.Until)
                {
                    var p = vehicle.Position;
                    Function.Call(Hash.REQUEST_COLLISION_AT_COORD, p.X, p.Y, p.Z);
                    continue;
                }
                vehicle.IsPositionFrozen = false;
                vehicle.PlaceOnGround();
                Held.RemoveAt(i);
                Logger.Info((loaded ? "Ground loaded under " : "Wait over for ") + vehicle.DisplayName + "; set on the ground at " + vehicle.Position + ".");
            }
        }

        public static void SafeRelease(Entity entity)
        {
            try { if (entity != null && entity.Exists()) entity.MarkAsNoLongerNeeded(); }
            catch (Exception ex) { Logger.Warn("Could not release mission entity: " + ex.Message); }
        }

        public static void SafeDelete(Blip blip)
        {
            try { if (blip != null && blip.Exists()) blip.Delete(); }
            catch (Exception ex) { Logger.Warn("Could not release map marker: " + ex.Message); }
        }

        /// <summary>Ground-hugging objective cylinder, drawn per frame.</summary>
        public static void DrawObjectiveMarker(Vector3 position, Color color, float radius = 1.5f)
        {
            ObjectiveMarkers.Show(position, color.R > 150 && color.G < 110 ? BlipColor.Red : BlipColor.Yellow);
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

        /// <summary>
        /// A small progress meter above the subtitle line for a hold or a piece of
        /// work: the game's own timer-bar backing with a fill. Replaces "stay in the
        /// marker: Ns" text (Ron, September 10).
        /// </summary>
        public static void DrawProgressBar(float fraction)
        {
            fraction = Math.Max(0f, Math.Min(1f, fraction));
            const float width = 0.085f, height = 0.007f, x = 0.5f, y = 0.888f;
            if (!Function.Call<bool>(Hash.HAS_STREAMED_TEXTURE_DICT_LOADED, "timerbars")) Function.Call(Hash.REQUEST_STREAMED_TEXTURE_DICT, "timerbars", false);
            else Function.Call(Hash.DRAW_SPRITE, "timerbars", "all_black_bg", x, y, width + 0.014f, height + 0.014f, 0f, 255, 255, 255, 170);
            Function.Call(Hash.DRAW_RECT, x, y, width, height, 38, 38, 38, 210);
            if (fraction > 0f) Function.Call(Hash.DRAW_RECT, x - width * 0.5f + width * fraction * 0.5f, y, width * fraction, height, 240, 205, 60, 235);
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

        /// <summary>
        /// Player control on, stated. Right after a player-ped change the engine can
        /// report control as off; a scene that starts in that same tick captures
        /// "off" and faithfully restores it, and the player stands in a mission they
        /// cannot move in. Every ped change asserts control; callers that hold it off
        /// on purpose (death recovery, the apartment fade) do not go through here.
        /// </summary>
        public static void AssertPlayerControl(string where)
        {
            if (Game.Player.CanControlCharacter) return;
            Game.Player.CanControlCharacter = true;
            Logger.Info("Player control was off after " + where + "; restored.");
        }
    }
}
