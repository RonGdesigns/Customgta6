using System;
using System.Collections.Generic;
using GTA;
using GTA.Math;

namespace Bloodlines.Core
{
    /// <summary>
    /// The road-handling half of the faster world. <see cref="WorldTuning"/> doubles
    /// gearing and feeds in torque; this gives the same cars the grip, damping,
    /// center of mass and brakes that speed needs, per vehicle class, so a Primo
    /// stays a Primo and a Benson stays a Benson instead of both becoming a race
    /// car that flies on the first curb.
    ///
    /// Shared handling entries are per model, captured once from the original
    /// values and derived from them — never re-multiplied per car or per tick —
    /// and restored on stand-down only where the value is still ours. The numbers
    /// are opening values for the paired road loop in docs/HANDLING-CALIBRATION.md,
    /// not final tuning.
    /// </summary>
    public sealed class RoadHandling
    {
        public enum Class { None, Car, Suv, Truck }

        /// <summary>One shared-handling value: what it was, what we set, how to reach it.</summary>
        private sealed class Change
        {
            public string Name;
            public Func<HandlingData, float> Get;
            public Action<HandlingData, float> Set;
            public float Original, Applied;
        }

        private readonly List<Change> _changes = new List<Change>();
        private Vector3 _comOriginal, _comApplied, _inertiaOriginal, _inertiaApplied;
        private bool _comOwned, _inertiaOwned;

        public Class Applied { get; private set; } = Class.None;
        public string Report { get; private set; } = "";

        /// <summary>Which profile a vehicle takes. Two wheels, hulls, rotors, wings and rails take none.</summary>
        public static Class Classify(Vehicle car)
        {
            if (car == null || !car.Exists()) return Class.None;
            var model = car.Model;
            if (model.IsBike || model.IsBicycle || model.IsBoat || model.IsHelicopter || model.IsPlane || model.IsTrain || model.IsSubmarine) return Class.None;
            switch (car.ClassType)
            {
                case VehicleClass.Compacts:
                case VehicleClass.Sedans:
                case VehicleClass.Coupes:
                case VehicleClass.Muscle:
                case VehicleClass.SportsClassics:
                case VehicleClass.Sports:
                case VehicleClass.Super:
                    return Class.Car;
                case VehicleClass.SUVs:
                case VehicleClass.OffRoad:
                case VehicleClass.Vans:
                case VehicleClass.Utility:
                case VehicleClass.Service:
                case VehicleClass.Emergency:
                    return Class.Suv;
                case VehicleClass.Industrial:
                case VehicleClass.Commercial:
                case VehicleClass.Military:
                    return Class.Truck;
                default:
                    return Class.None;
            }
        }

        // Opening values from the repair plan, §2.2. Steering lock is deliberately
        // absent: the engine already trims lock with speed, and more lock is more twitch.
        public static float TractionFactor(Class c) => c == Class.Car ? 1.15f : c == Class.Suv ? 1.12f : c == Class.Truck ? 1.08f : 1f;
        public static float TractionLossFactor(Class c) => c == Class.Truck ? 0.9f : c == Class.None ? 1f : 0.85f;
        public static float DampingFactor(Class c) => c == Class.Car ? 1.25f : c == Class.Suv ? 1.30f : c == Class.Truck ? 1.20f : 1f;
        public static float CenterOfMassDrop(Class c) => c == Class.Car ? 0.12f : c == Class.Suv ? 0.15f : c == Class.Truck ? 0.10f : 0f;
        public static float BrakeFactor(Class c) => c == Class.Truck ? 1.3f : c == Class.None ? 1f : 1.4f;
        public static float YawInertiaFactor(Class c) => c == Class.Car || c == Class.Suv ? 1.10f : 1f;

        public static float DeformationFactor(Class c, float baseMultiplier)
        {
            if (baseMultiplier <= 0.01f) return 1f;
            float scale = c == Class.Car ? 1.0f : c == Class.Suv ? 0.825f : c == Class.Truck ? 0.675f : 0.5f;
            return 1f + (baseMultiplier - 1f) * scale;
        }

        public static float CollisionFactor(Class c, float baseMultiplier) =>
            c == Class.Car ? baseMultiplier : c == Class.Suv ? Math.Min(1.0f, baseMultiplier * 1.05f) : 1.0f;

        public static float EngineFactor(Class c, float baseMultiplier) =>
            c == Class.Car ? baseMultiplier : c == Class.Suv ? Math.Min(1.0f, baseMultiplier * 1.05f) : 1.0f;

        /// <summary>
        /// Apply the class profile to a model's shared handling. Returns false when the
        /// vehicle takes no profile or the handling is not usable. Safe to call once per
        /// profile only; the caller owns that rule.
        /// </summary>
        public bool Apply(HandlingData handling, Vehicle car, ModConfig config = null, bool isCrewVehicle = false)
        {
            if (handling == null || !handling.IsValid || Applied != Class.None) return false;
            var cls = Classify(car);
            if (cls == Class.None) { Report = car != null && car.Exists() ? car.DisplayName + ": no road profile (" + car.ClassType + ")" : "no vehicle"; return false; }

            Scale(handling, "TractionCurveMax", h => h.TractionCurveMax, (h, v) => h.TractionCurveMax = v, TractionFactor(cls));
            Scale(handling, "TractionCurveMin", h => h.TractionCurveMin, (h, v) => h.TractionCurveMin = v, TractionFactor(cls));
            Scale(handling, "TractionLossMultiplier", h => h.TractionLossMultiplier, (h, v) => h.TractionLossMultiplier = v, TractionLossFactor(cls));
            Scale(handling, "SuspensionCompressionDamping", h => h.SuspensionCompressionDamping, (h, v) => h.SuspensionCompressionDamping = v, DampingFactor(cls));
            Scale(handling, "SuspensionReboundDamping", h => h.SuspensionReboundDamping, (h, v) => h.SuspensionReboundDamping = v, DampingFactor(cls));
            Scale(handling, "BrakeForce", h => h.BrakeForce, (h, v) => h.BrakeForce = v, BrakeFactor(cls));

            float defFactor = 1f;
            float colFactor = 1f;
            float engFactor = 1f;
            if (config != null && config.VehicleDamageEnabled)
            {
                defFactor = DeformationFactor(cls, config.DeformationMultiplier);
                colFactor = CollisionFactor(cls, config.CollisionDamageMultiplier);
                engFactor = EngineFactor(cls, config.EngineDamageMultiplier);

                if (isCrewVehicle)
                {
                    colFactor *= config.CrewProtectionMultiplier;
                    engFactor *= config.CrewProtectionMultiplier;
                }

                Scale(handling, "DeformationDamageMultiplier", h => h.DeformationDamageMultiplier, (h, v) => h.DeformationDamageMultiplier = v, defFactor);
                Scale(handling, "CollisionDamageMultiplier", h => h.CollisionDamageMultiplier, (h, v) => h.CollisionDamageMultiplier = v, colFactor);
                Scale(handling, "EngineDamageMultiplier", h => h.EngineDamageMultiplier, (h, v) => h.EngineDamageMultiplier = v, engFactor);
            }

            var com = handling.CenterOfMassOffset;
            if (Finite(com))
            {
                _comOriginal = com;
                _comApplied = new Vector3(com.X, com.Y, com.Z - CenterOfMassDrop(cls));
                handling.CenterOfMassOffset = _comApplied;
                _comOwned = true;
            }
            var inertia = handling.InertiaMultiplier;
            if (Finite(inertia) && inertia.Z > 0f && YawInertiaFactor(cls) != 1f)
            {
                _inertiaOriginal = inertia;
                _inertiaApplied = new Vector3(inertia.X, inertia.Y, inertia.Z * YawInertiaFactor(cls));
                handling.InertiaMultiplier = _inertiaApplied;
                _inertiaOwned = true;
            }

            Applied = cls;
            string damageInfo = (config != null && config.VehicleDamageEnabled)
                ? ", deform x" + defFactor.ToString("0.00") + ", collision x" + colFactor.ToString("0.00")
                : "";
            Report = car.DisplayName + ": road profile " + cls + " (traction x" + TractionFactor(cls).ToString("0.00") + ", damping x" + DampingFactor(cls).ToString("0.00") +
                     ", brakes x" + BrakeFactor(cls).ToString("0.00") + ", CoM -" + CenterOfMassDrop(cls).ToString("0.00") + damageInfo + ")";
            Logger.Info(Report);
            return true;
        }

        private void Scale(HandlingData handling, string name, Func<HandlingData, float> get, Action<HandlingData, float> set, float factor)
        {
            if (Math.Abs(factor - 1f) < 0.0001f) return;
            float original = get(handling);
            if (float.IsNaN(original) || float.IsInfinity(original) || original <= 0f) return;
            var change = new Change { Name = name, Get = get, Set = set, Original = original, Applied = original * factor };
            _changes.Add(change);
            set(handling, change.Applied);
        }

        /// <summary>Put back every value that is still ours. True when nothing of ours remains applied.</summary>
        public bool Restore(HandlingData live)
        {
            if (live == null || !live.IsValid) return _changes.Count == 0 && !_comOwned && !_inertiaOwned;
            foreach (var change in _changes.ToArray())
            {
                // Another mod owns a value that no longer matches our edit; leave it.
                if (Math.Abs(change.Get(live) - change.Applied) < 0.0001f) change.Set(live, change.Original);
                _changes.Remove(change);
            }
            if (_comOwned) { if (live.CenterOfMassOffset == _comApplied) live.CenterOfMassOffset = _comOriginal; _comOwned = false; }
            if (_inertiaOwned) { if (live.InertiaMultiplier == _inertiaApplied) live.InertiaMultiplier = _inertiaOriginal; _inertiaOwned = false; }
            Applied = Class.None;
            return true;
        }

        private static bool Finite(Vector3 v) =>
            !(float.IsNaN(v.X) || float.IsNaN(v.Y) || float.IsNaN(v.Z) || float.IsInfinity(v.X) || float.IsInfinity(v.Y) || float.IsInfinity(v.Z));
    }
}
