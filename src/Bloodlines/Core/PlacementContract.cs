using System;
using GTA;
using GTA.Math;

namespace Bloodlines.Core
{
    public enum PlacementKind
    {
        Ped,
        Interaction,
        Vehicle,
        Aircraft
    }

    /// <summary>The result of resolving one authored placement contract.</summary>
    public sealed class PlacementResult
    {
        public string Key { get; internal set; }
        public PlacementKind Kind { get; internal set; }
        public bool Passed { get; internal set; }
        public Vector3 Authored { get; internal set; }
        public Vector3 Resolved { get; internal set; }
        public string Reason { get; internal set; }
        public string ModelName { get; internal set; }
        public float DepartureMeters { get; internal set; }

        public string Describe()
        {
            string model = string.IsNullOrEmpty(ModelName) ? "" : " model=" + ModelName;
            string lane = DepartureMeters <= 0f ? "" : " departure=" + DepartureMeters.ToString("0.#") + "m";
            return Kind + " " + Key + model + lane + " — " + (Passed ? "valid" : "invalid: " + Reason);
        }
    }

    /// <summary>
    /// A named placement expectation that resolves through the existing bounded
    /// geometry checks and reports the result to MissionDoctor. This does not write
    /// survey data, pick another floor or silently delete blockers. Mission authors
    /// decide whether a failed contract is fatal or whether an authored/surveyed
    /// point should remain usable with a warning.
    /// </summary>
    public sealed class PlacementContract
    {
        private readonly Func<Vector3, bool> _allowed;
        private readonly Model _model;

        private PlacementContract(string key, PlacementKind kind, Model model,
            float departureMeters, Func<Vector3, bool> allowed)
        {
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException("Placement key is required.", nameof(key));
            Key = key;
            Kind = kind;
            _model = model;
            DepartureMeters = Math.Max(0f, departureMeters);
            _allowed = allowed;
        }

        public string Key { get; }
        public PlacementKind Kind { get; }
        public float DepartureMeters { get; }

        public static PlacementContract Ped(string key, Func<Vector3, bool> allowed = null) =>
            new PlacementContract(key, PlacementKind.Ped, default(Model), 0f, allowed);

        public static PlacementContract Interaction(string key, Func<Vector3, bool> allowed = null) =>
            new PlacementContract(key, PlacementKind.Interaction, default(Model), 0f, allowed);

        public static PlacementContract Vehicle(string key, Model model, float departureMeters = 0f,
            Func<Vector3, bool> allowed = null) =>
            new PlacementContract(key, PlacementKind.Vehicle, model, departureMeters, allowed);

        public static PlacementContract Aircraft(string key, Model model, float departureMeters,
            Func<Vector3, bool> allowed = null) =>
            new PlacementContract(key, PlacementKind.Aircraft, model, departureMeters, allowed);

        public PlacementResult Inspect(LocationBook book, MissionDoctor doctor = null, Entity ignore = null)
        {
            if (book == null) throw new ArgumentNullException(nameof(book));
            var site = book.Get(Key);
            if (site == null)
            {
                var missing = new PlacementResult
                {
                    Key = Key,
                    Kind = Kind,
                    Passed = false,
                    Authored = Vector3.Zero,
                    Resolved = Vector3.Zero,
                    ModelName = ModelName(),
                    DepartureMeters = DepartureMeters,
                    Reason = "location key is missing"
                };
                doctor?.Error("placement", Key, missing.Describe());
                return missing;
            }

            Vector3 resolved;
            bool passed;
            if (Kind == PlacementKind.Vehicle || Kind == PlacementKind.Aircraft)
            {
                passed = BoundedPlacement.TryVehicle(book, Key, _model, out resolved, ignore, _allowed, DepartureMeters);
            }
            else
            {
                passed = BoundedPlacement.TryPed(book, Key, out resolved, _allowed);
            }

            var result = new PlacementResult
            {
                Key = Key,
                Kind = Kind,
                Passed = passed,
                Authored = site.Position,
                Resolved = resolved,
                ModelName = ModelName(),
                DepartureMeters = DepartureMeters,
                Reason = passed ? null : "bounded geometry check refused the authored point; see the matching Bloodlines.log warning for the exact obstruction/floor reason"
            };
            if (passed) doctor?.Info("placement", Key, result.Describe());
            else doctor?.Warn("placement", Key, result.Describe());
            return result;
        }

        /// <summary>Resolve or fail with a contract-specific message. No alternate coordinate is invented.</summary>
        public Vector3 Require(LocationBook book, MissionDoctor doctor = null, Entity ignore = null)
        {
            var result = Inspect(book, doctor, ignore);
            if (result.Passed) return result.Resolved;
            string message = result.Describe();
            doctor?.Error("placement", Key, "required placement rejected: " + message);
            throw new InvalidOperationException(message);
        }

        private string ModelName()
        {
            if (Kind != PlacementKind.Vehicle && Kind != PlacementKind.Aircraft) return null;
            try { return _model.Name; }
            catch { return "unknown"; }
        }
    }
}
