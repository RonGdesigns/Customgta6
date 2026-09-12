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
        public static CrewSlot? ActiveSlot { get; set; }
        private static int _used;
        private struct Destination { public Vector3 Position; public CrewSlot? Owner; public int Vehicle; public bool Road; }
        private static readonly List<Destination> Pending = new List<Destination>();
        private static readonly List<Destination> Current = new List<Destination>();
        public static void Navigation(Vector3 position, CrewSlot? owner = null, Vehicle vehicle = null)
        {
            if (_enabled) Pending.Add(new Destination { Position = position, Owner = owner, Vehicle = vehicle?.Handle ?? 0, Road = vehicle == null || !(vehicle.Model.IsBoat || vehicle.Model.IsSubmarine) });
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
                if (_route == null || !_route.Exists())
                {
                    _route = World.CreateBlip(routes[0].Position);
                    if (_route != null) { _route.Color = BlipColor.Yellow; _route.Name = "Next objective"; _route.IsShortRange = false; _route.ShowRoute = true; }
                }
                if (_route != null && _route.Exists()) { _route.Position = routes[0].Position; _route.ShowRoute = routes[0].Road; }
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
