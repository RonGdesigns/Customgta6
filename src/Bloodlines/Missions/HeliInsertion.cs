using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using GTA;
using GTA.Math;
using GTA.Native;

namespace Bloodlines.Missions
{
    /// <summary>
    /// A police helicopter that flies a pair of troopers to a point, hovers, drops
    /// them on ropes, and leaves. The troopers are the mission's: it spawns and
    /// arms them, hands them here seated, and gets them back on the ground with
    /// their combat task running. Every phase has a clock, so a rope that never
    /// plays or a hover that never settles still ends with the troopers on the
    /// ground and the wave able to finish.
    ///
    /// The rear seats of the Maverick are the ones a ped can rappel from, so a
    /// helicopter carries two. A larger wave is more helicopters, staggered on the
    /// approach, plus the rest on the street.
    /// </summary>
    public sealed class HeliInsertion
    {
        public enum Phase { Inbound, Unloading, Departing, Done }

        public const int Capacity = 2;
        public const float ApproachDistance = 240f;
        public const float ApproachAltitude = 70f;
        public const float HoverHeight = 26f;
        public const int InboundTimeoutMs = 45000;
        public const int UnloadTimeoutMs = 20000;
        public const int DepartMs = 25000;

        public Vehicle Heli { get; private set; }
        public Ped Pilot { get; private set; }
        public IReadOnlyList<Ped> Troops => _troops;
        public Phase Current { get; private set; } = Phase.Inbound;

        private readonly List<Ped> _troops = new List<Ped>();
        private readonly HashSet<int> _landed = new HashSet<int>();
        private Vector3 _target;
        private float _fromHeading;
        private int _phaseStart;

        /// <summary>
        /// Spawn the helicopter on its approach line and seat the troopers. Null when
        /// the models or the vehicle could not be created; the caller then falls back
        /// to a street spawn for those troopers.
        /// </summary>
        public static HeliInsertion Launch(Vector3 target, float fromHeading, IList<Ped> troops, Action<Entity> track)
        {
            if (troops == null || troops.Count == 0) return null;
            var model = new Model("polmav");
            var pilotModel = new Model("s_m_y_pilot_01");
            if (!GameUtils.RequestModel(model, 2000) || !GameUtils.RequestModel(pilotModel, 2000)) return null;
            try
            {
                var spawn = target + Direction(fromHeading) * ApproachDistance + new Vector3(0f, 0f, ApproachAltitude);
                var heli = World.CreateVehicle(model, spawn, HeadingFrom(spawn, target));
                if (heli == null || !heli.Exists()) return null;
                track?.Invoke(heli);
                heli.IsPersistent = true;
                heli.IsEngineRunning = true;
                Function.Call(Hash.SET_HELI_BLADES_FULL_SPEED, heli);

                var pilot = World.CreatePed(pilotModel, spawn, heli.Heading);
                if (pilot == null || !pilot.Exists()) { GameUtils.SafeDelete(heli); return null; }
                track?.Invoke(pilot);
                pilot.IsPersistent = true;
                pilot.BlockPermanentEvents = true;
                pilot.CanBeDraggedOutOfVehicle = false;
                pilot.SetIntoVehicle(heli, VehicleSeat.Driver);

                var insertion = new HeliInsertion { Heli = heli, Pilot = pilot, _target = target, _fromHeading = fromHeading, _phaseStart = Game.GameTime };
                var seats = new[] { VehicleSeat.LeftRear, VehicleSeat.RightRear };
                for (int i = 0; i < troops.Count && i < Capacity; i++)
                {
                    var trooper = troops[i];
                    if (trooper == null || !trooper.Exists()) continue;
                    trooper.SetIntoVehicle(heli, seats[i]);
                    insertion._troops.Add(trooper);
                }
                var hover = target + new Vector3(0f, 0f, HoverHeight);
                pilot.Task.StartHeliMission(heli, hover, VehicleMissionType.GoTo, 45f, 10f, (int)hover.Z, (int)HoverHeight - 6, -1f, 60f, (HeliMissionFlags)(256 | 4096));
                Logger.Info("SWAT helicopter inbound from " + fromHeading + " deg with " + insertion._troops.Count + " on ropes.");
                return insertion;
            }
            finally
            {
                model.MarkAsNoLongerNeeded();
                pilotModel.MarkAsNoLongerNeeded();
            }
        }

        public void Update()
        {
            if (Current == Phase.Done) return;
            bool heliUsable = Heli != null && Heli.Exists() && !Heli.IsDead && Pilot != null && Pilot.Exists() && !Pilot.IsDead;
            if (!heliUsable)
            {
                // Shot down or the pilot is gone: whoever is still seated is put on the
                // ground so the wave can still end.
                ForceOut();
                Current = Phase.Done;
                return;
            }
            int elapsed = Game.GameTime - _phaseStart;
            switch (Current)
            {
                case Phase.Inbound:
                    bool arrived = GameUtils.IsWithinFlat(Heli.Position, _target, 24f) && Heli.HeightAboveGround < HoverHeight + 16f;
                    if (!arrived && elapsed < InboundTimeoutMs) return;
                    if (!arrived) { Logger.Warn("SWAT helicopter never reached its hover; unloading where it is."); ForceOut(); Depart(); return; }
                    foreach (var trooper in _troops.Where(Usable)) trooper.Task.RappelFromHelicopter();
                    Current = Phase.Unloading; _phaseStart = Game.GameTime;
                    return;
                case Phase.Unloading:
                    foreach (var trooper in _troops.Where(Usable))
                    {
                        if (trooper.IsInVehicle() || _landed.Contains(trooper.Handle)) continue;
                        if (trooper.HeightAboveGround > 2.5f) continue;
                        _landed.Add(trooper.Handle);
                        trooper.Task.FightAgainstHatedTargets(90f);
                    }
                    bool allOut = _troops.Where(Usable).All(t => !t.IsInVehicle() && _landed.Contains(t.Handle));
                    if (allOut) { Depart(); return; }
                    if (elapsed > UnloadTimeoutMs) { Logger.Warn("SWAT rope drop timed out; placing the remaining troopers."); ForceOut(); Depart(); }
                    return;
                case Phase.Departing:
                    if (elapsed > DepartMs) Current = Phase.Done;
                    return;
            }
        }

        private void Depart()
        {
            Current = Phase.Departing; _phaseStart = Game.GameTime;
            if (Pilot == null || !Pilot.Exists() || Heli == null || !Heli.Exists()) return;
            var away = _target + Direction(_fromHeading + 40f) * 600f + new Vector3(0f, 0f, 110f);
            Pilot.Task.StartHeliMission(Heli, away, VehicleMissionType.GoTo, 50f, 40f, (int)away.Z, 60, -1f, 80f, (HeliMissionFlags)(256 | 4096));
        }

        /// <summary>Everyone still aboard goes to the ground beside the target, fighting.</summary>
        private void ForceOut()
        {
            int i = 0;
            foreach (var trooper in _troops.Where(Usable))
            {
                if (trooper.IsInVehicle()) ExitVehicleStep.ForceOut(trooper);
                if (_landed.Contains(trooper.Handle)) continue;
                var spot = _target + new Vector3(-3f + i * 3f, 4f, 0f);
                var safe = World.GetSafeCoordForPed(spot, false, 0);
                Function.Call(Hash.REQUEST_COLLISION_AT_COORD, spot.X, spot.Y, spot.Z);
                trooper.Position = safe != Vector3.Zero && safe.DistanceTo(spot) < 12f ? safe : spot;
                _landed.Add(trooper.Handle);
                trooper.Task.FightAgainstHatedTargets(90f);
                i++;
            }
        }

        private static bool Usable(Ped ped) => ped != null && ped.Exists() && !ped.IsDead;

        public static Vector3 Direction(float headingDegrees)
        {
            double radians = headingDegrees * Math.PI / 180.0;
            // GTA headings: 0 is north (+Y), increasing counterclockwise toward west (-X).
            return new Vector3((float)-Math.Sin(radians), (float)Math.Cos(radians), 0f);
        }

        public static float HeadingFrom(Vector3 from, Vector3 to)
        {
            float heading = (float)(Math.Atan2(-(to.X - from.X), to.Y - from.Y) * 180.0 / Math.PI);
            return heading < 0f ? heading + 360f : heading;
        }
    }
}
