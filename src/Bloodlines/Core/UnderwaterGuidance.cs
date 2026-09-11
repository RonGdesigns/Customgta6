using System;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Core
{
    /// <summary>Three-dimensional guidance: a sphere at the reachable vehicle-center position, not a road GPS.</summary>
    public static class UnderwaterGuidance
    {
        public static bool IsSub(Vehicle vehicle) => vehicle != null && vehicle.Exists() && vehicle.Model.IsSubmarine;
        public static string DistanceText(Vector3 vehicle, Vector3 target)
        {
            float horizontal = new Vector3(vehicle.X, vehicle.Y, 0f).DistanceTo(new Vector3(target.X, target.Y, 0f));
            float vertical = target.Z - vehicle.Z;
            return horizontal.ToString("0") + " m across | " + (Math.Abs(vertical) < 1f ? "depth aligned" :
                (vertical < 0f ? "dive " : "rise ") + Math.Abs(vertical).ToString("0.0") + " m");
        }
        public static void Draw(Vehicle submarine, Vector3 target, float radius)
        {
            ObjectiveMarkers.Navigation(target, null, submarine);
            ObjectiveMarkers.Show(target, BlipColor.Yellow);
            // Marker type 28 is the stock debug sphere; its three-dimensional volume
            // communicates the hold radius without inventing a visible road below it.
            Function.Call(Hash.DRAW_MARKER, 28, target.X, target.Y, target.Z,
                0f, 0f, 0f, 0f, 0f, 0f, radius * 2f, radius * 2f, radius * 2f,
                255, 200, 40, 75, false, false, 2, false, null, null, false);
            new GTA.UI.TextElement(DistanceText(submarine.Position, target), new PointF(640f, 608f), .34f, Color.Yellow)
                { Alignment = GTA.UI.Alignment.Center }.Draw();
        }
    }
}

namespace Bloodlines.Missions.Objectives
{
    public sealed class SurfaceSubObjective : Objective
    {
        private readonly Func<Vehicle> _sub;
        private readonly Func<Vector3> _target;
        public SurfaceSubObjective(Func<Vehicle> sub, Func<Vector3> target) : base("Gohan: surface the Kraken at the support marker.")
        { _sub = sub; _target = target; RequiredCharacter = Crew.CrewSlot.Gohan; }
        public override void Update(MissionContext context)
        {
            var sub = _sub(); var point = _target();
            if (sub == null || !sub.Exists() || sub.IsDead || !sub.IsDriveable) { Fail("The Kraken was lost."); return; }
            Core.UnderwaterGuidance.Draw(sub, point, 4f);
            if (IsOwnerActive(context) && Game.Player.Character != null && Game.Player.Character.IsInVehicle(sub) &&
                sub.GetPedOnSeat(VehicleSeat.Driver) == Game.Player.Character &&
                Core.GameUtils.IsWithinFlat(sub.Position, point, 8f) && Math.Abs(sub.Position.Z - point.Z) <= 2.5f)
                Complete();
        }
    }
}
