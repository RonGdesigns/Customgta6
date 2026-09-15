using System.Collections.Generic;
using Bloodlines.Crew;
using GTA;
using GTA.Math;

namespace Bloodlines.Core
{
    /// <summary>Map counterparts of the world markers drawn by active objectives.</summary>
    public static class ObjectiveMarkers
    {
        private static readonly List<Blip> Blips = new List<Blip>();
        private static bool _enabled;
        private static Blip _route;
        private static bool _routeRoad;
        private static Vector3 _routedPosition;
        private static int _routedAt;
        public static CrewSlot? ActiveSlot { get; set; }
        private static int _used;
        /// <summary>
        /// While true, objective markers and routes are not drawn.
        ///
        /// A stage with parallel jobs used to draw every one of them at once — six limpet
        /// markers and an interlock cabinet in M51 — so Ron could not tell which marker was
        /// the one he was being asked to reach. An objective still updates while this is set,
        /// because progress belongs to whoever owns it; only the drawing is held.
        /// </summary>
        public static bool Suppressed;
        private struct Destination { public Vector3 Position; public CrewSlot? Owner; public int Vehicle; public bool Road; }
        private static readonly List<Destination> Pending = new List<Destination>();
        private static readonly List<Destination> Current = new List<Destination>();
        /// <param name="road">
        /// False for a destination a road route cannot reach — a target in the air, or a
        /// point at sea. An aircraft 1.2 km away needs a waypoint that tracks it, not a
        /// driving route to the water underneath it.
        /// </param>
        public static void Navigation(Vector3 position, CrewSlot? owner = null, Vehicle vehicle = null, bool road = true)
        {
            if (!_enabled || Suppressed) return;
            bool onRoad = road && (vehicle == null || !(vehicle.Model.IsBoat || vehicle.Model.IsSubmarine));
            Pending.Add(new Destination { Position = position, Owner = owner, Vehicle = vehicle?.Handle ?? 0, Road = onRoad });
        }
        public static Vector3? DestinationFor(CrewSlot slot, Vehicle vehicle)
        {
            // Prefer the driver's assigned task; never route to somebody else's job.
            foreach (var d in Current)
                if (d.Owner == slot && (d.Vehicle == 0 || (vehicle != null && d.Vehicle == vehicle.Handle))) return d.Position;
            foreach (var d in Current)
                if (!d.Owner.HasValue && (d.Vehicle == 0 || (vehicle != null && d.Vehicle == vehicle.Handle))) return d.Position;
            return null;
        }
        /// <summary>Re-show the current mission route, without leaving a stale personal waypoint.</summary>
        public static string FocusPlayerDestination(CrewSlot slot)
        {
            var routes = Current.FindAll(d => d.Owner == slot);
            if (routes.Count == 0) routes = Current.FindAll(d => !d.Owner.HasValue);
            if (routes.Count != 1 || _route == null || !_route.Exists() ||
                _route.Position.DistanceTo(routes[0].Position) > 1f)
                return "No single destination. Follow the objective instructions.";
            if (!routes[0].Road) return "Use the yellow objective marker on the water.";
            _route.ShowRoute = false; _route.ShowRoute = true;
            return "Current objective route shown in yellow.";
        }

        public static void BeginFrame(bool enabled)
        {
            _enabled = enabled;
            _used = 0;
            Pending.Clear();
            if (!enabled) Clear();
        }
        public static void Show(Vector3 position, BlipColor color = BlipColor.Yellow)
        {
            if (!_enabled || _used >= 16) return;
            if (_used == Blips.Count) Blips.Add(null);
            var blip = Blips[_used];
            if (blip == null || !blip.Exists())
            {
                blip = World.CreateBlip(position);
                Blips[_used] = blip;
                if (blip == null) return;
                blip.Color = BlipColor.Yellow;
                blip.Sprite = BlipSprite.Standard;
                blip.IsShortRange = false;
                blip.ShowRoute = false;
                blip.Name = "Mission objective";
            }
            blip.Color = color;
            blip.Name = color == BlipColor.Red ? "Mission hostile" : "Mission objective";
            blip.Position = position;
            _used++;
        }
        public static void EndFrame()
        {
            Current.Clear();
            Current.AddRange(Pending);
            // A single next destination gets a GTA mission GPS route automatically.
            var routes = Current.FindAll(d => d.Owner == ActiveSlot);
            if (routes.Count == 0) routes = Current.FindAll(d => !d.Owner.HasValue);
            if (routes.Count == 0 && Current.Count == 1) routes.Add(Current[0]);
            if (routes.Count == 1)
            {
                // GTA can retain the old path after a blip moves. Refresh boundedly
                // for a moving target, immediately for a different stage/location.
                float shift = _routedPosition.DistanceTo(routes[0].Position);
                if (_route != null && (shift > 100f || (shift > 20f && Game.GameTime - _routedAt >= 1000)))
                { GameUtils.SafeDelete(_route); _route = null; }
                if (_route == null || !_route.Exists())
                {
                    _routedPosition = routes[0].Position; _routedAt = Game.GameTime;
                    _route = World.CreateBlip(routes[0].Position);
                    if (_route != null) { _route.Color = BlipColor.Yellow; _route.Name = "Next objective"; _route.IsShortRange = false; _routeRoad = routes[0].Road; _route.ShowRoute = _routeRoad; }
                }
                // The route flag is set only when it changes: re-setting it every
                // frame made the yellow route vanish from the radar while it still
                // showed on the pause map (Ron, September 11).
                if (_route != null && _route.Exists())
                {
                    _route.Position = routes[0].Position;
                    if (_routeRoad != routes[0].Road) { _routeRoad = routes[0].Road; _route.ShowRoute = _routeRoad; }
                }
            }
            else { GameUtils.SafeDelete(_route); _route = null; }
            while (Blips.Count > _used)
            {
                GameUtils.SafeDelete(Blips[Blips.Count - 1]);
                Blips.RemoveAt(Blips.Count - 1);
            }
        }
        public static void Clear()
        {
            GameUtils.SafeDelete(_route); _route = null;
            foreach (var blip in Blips) GameUtils.SafeDelete(blip);
            Blips.Clear();
            Current.Clear();
            Pending.Clear();
            _used = 0;
            _enabled = false;
        }
    }
}
