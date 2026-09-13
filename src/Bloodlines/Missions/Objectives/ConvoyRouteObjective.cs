using System;
using System.Drawing;
using Bloodlines.Core;
using GTA;
using GTA.Math;

namespace Bloodlines.Missions.Objectives
{
    /// <summary>Follow the patient vehicle while controlling either crew vehicle.</summary>
    public sealed class ConvoyRouteObjective : Objective
    {
        private readonly Func<Vehicle> _transport, _escort;
        private readonly Func<Vector3> _destination;
        private readonly float _radius;
        private readonly string _action;
        public ConvoyRouteObjective(string label, Func<Vehicle> transport, Func<Vehicle> escort,
            Func<Vector3> destination, float radius) : base(label)
        { _action = label; _transport = transport; _escort = escort; _destination = destination; _radius = radius; }
        public override Vector3? AssignmentPosition => null;
        public override void Update(MissionContext c)
        {
            var transport = _transport(); var escort = _escort(); var player = Game.Player.Character;
            if (transport == null || !transport.Exists() || transport.IsDead || escort == null || !escort.Exists() || escort.IsDead)
            { Fail("An evacuation vehicle was lost. Restart the escort."); return; }
            var destination = _destination();
            ObjectiveMarkers.Navigation(destination, null, transport);
            GameUtils.DrawObjectiveMarker(destination, Color.Yellow, 4f);
            if (player == null || (!player.IsInVehicle(transport) && !player.IsInVehicle(escort)))
            { Label = "Return to the half-track or Gohan's escort car to continue the evacuation."; return; }
            Label = _action;
            if (!GameUtils.IsWithinFlat(transport.Position, destination, _radius)) return;
            if (transport.Speed > 2f) { Label = "Slow the half-track to a stop in the yellow zone."; return; }
            Complete();
        }
    }
}
