using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using GTA;

public static partial class StoryTests
{
    /// <summary>
    /// Tuning past the last part the game sells. Two stages, not four, and the reason is
    /// the point of most of these checks: power and top speed have per-entity natives, and
    /// braking and grip do not.
    /// </summary>
    static void VehicleStageChecks()
    {
        var car = new OwnedVehicle { Id = 7, ModelName = "sultanrs" };

        // ---- Persistence. A stage is part of the build and survives a save.
        Check(VehicleStages.Level(car, VehicleStages.Stage.FinalDrive) == 0 &&
              VehicleStages.Level(car, VehicleStages.Stage.Engine) == 0, "A new car carries no stages");
        VehicleStages.Set(car, VehicleStages.Stage.FinalDrive, 1);
        Check(VehicleStages.Level(car, VehicleStages.Stage.FinalDrive) == 1, "One can be fitted");
        VehicleStages.Set(car, VehicleStages.Stage.FinalDrive, 5);
        Check(VehicleStages.Level(car, VehicleStages.Stage.FinalDrive) == 1, "and there is only one level of it");
        VehicleStages.Set(car, VehicleStages.Stage.FinalDrive, -3);
        Check(VehicleStages.Level(car, VehicleStages.Stage.FinalDrive) == 0, "which can be taken off but not gone below");

        VehicleStages.Set(car, VehicleStages.Stage.Engine, 1);
        // Through the text, which is what a save actually does. Handing FromJson the
        // dictionary ToJson just built skips the reader, and Json.Int only understands the
        // doubles the reader produces — so an in-memory round trip would read every number
        // as zero and prove nothing.
        var copy = OwnedVehicle.FromJson(Json.Read(Json.Write(car.ToJson())) as System.Collections.Generic.Dictionary<string, object>);
        Check(copy != null && VehicleStages.Level(copy, VehicleStages.Stage.Engine) == 1,
            "A fitted stage is written to the save and read back");
        Check(VehicleStages.Level(copy, VehicleStages.Stage.FinalDrive) == 0, "and one that is not fitted stays off");
        var old = OwnedVehicle.FromJson(Json.Read(Json.Write(
            new System.Collections.Generic.Dictionary<string, object> { { "id", 3 }, { "model", "granger" } }))
            as System.Collections.Generic.Dictionary<string, object>);
        Check(old != null && VehicleStages.Level(old, VehicleStages.Stage.Engine) == 0,
            "A save written before stages existed reads as none fitted rather than throwing");
        Check(car.Fingerprint() != new OwnedVehicle { Id = 7, ModelName = "sultanrs" }.Fingerprint(),
            "and fitting one marks the build changed, so the garage stores it");

        // ---- The multipliers, which are what the tuning loop applies.
        Check(Math.Abs(VehicleStages.DriveFactor(0) - 1f) < .001f && VehicleStages.DriveFactor(1) > 1f,
            "An unfitted final drive changes nothing and a fitted one raises the ceiling");
        Check(Math.Abs(VehicleStages.PowerFactor(0) - 1f) < .001f && VehicleStages.PowerFactor(1) > 1f,
            "and the same for the engine stage");
        Check(VehicleStages.DriveFactor(9) == VehicleStages.DriveFactor(1),
            "A level nobody can buy is clamped rather than multiplied");

        // ---- Nothing is fitted to a car nobody owns, and nothing reads a null.
        VehicleStages.Fitted = null;
        Check(VehicleStages.FittedOn(new Vehicle(), VehicleStages.Stage.Engine) == 0,
            "With no garage wired in, no car is carrying anything");
        VehicleStages.Fitted = (v, s) => throw new InvalidOperationException("no");
        Check(VehicleStages.FittedOn(new Vehicle(), VehicleStages.Stage.Engine) == 0,
            "and a lookup that throws is a zero, not a crash in the tuning loop");
        VehicleStages.Fitted = null;

        // ---- A stage is the step after the shop runs out, and it belongs to an owned car.
        Check(VehicleStages.Refusal(null, car, VehicleStages.Stage.Engine) != null,
            "There has to be a car here to work on");
        Check(VehicleStages.Requires(VehicleStages.Stage.FinalDrive) == VehicleStages.TransmissionSlot &&
              VehicleStages.Requires(VehicleStages.Stage.Engine) == VehicleStages.EngineSlot,
            "Each stage names the game's own slot that has to be maxed first");

        // ---- Where it is applied, and where it deliberately is not.
        string tuning = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "WorldTuning.cs"));
        Check(tuning.Contains("VehicleStages.DriveFactor(VehicleStages.FittedOn(car, VehicleStages.Stage.FinalDrive))"),
            "The final drive multiplies this car's own speed ceiling");
        Check(tuning.Contains("VehicleStages.PowerFactor(VehicleStages.FittedOn(car, VehicleStages.Stage.Engine))"),
            "and the engine stage multiplies this car's own power");
        int ceilingAt = tuning.IndexOf("VehicleStages.DriveFactor", StringComparison.Ordinal);
        int handlingAt = tuning.IndexOf("InitialDriveMaxFlatVelocity", StringComparison.Ordinal);
        Check(ceilingAt > 0 && (handlingAt < 0 || Math.Abs(ceilingAt - handlingAt) > 400),
            "It is nowhere near the shared handling field, which reaches every car of the model");
        string stages = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "VehicleStages.cs"));
        Check(!stages.Contains("InitialDriveMaxFlatVelocity") && !stages.Contains("HandlingData"),
            "and nothing in the stage module touches handling data at all");
        Check(stages.Contains("Braking and grip have none"),
            "The module records why there is no braking or grip stage");
        Check(stages.Contains("road test"),
            "and that a moved ceiling is not a measured top speed");

        // ---- The garage is the only thing that knows which live car is which owned one.
        string garages = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "Garages.cs"));
        Check(garages.Contains("public OwnedVehicle RecordFor(Vehicle vehicle)") &&
              garages.Contains("pair.Value.Handle != vehicle.Handle"),
            "A live car is matched to its record by handle, so a stage belongs to one car");
        string host = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "BloodlinesMain.cs"));
        Check(host.Contains("VehicleStages.Fitted = (vehicle, stage) => _garages.StageOn(vehicle, stage);"),
            "and the host is what tells the tuning loop where to read them");
    }
}
