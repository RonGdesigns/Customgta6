using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using GTA;

public static partial class StoryTests
{
    /// <summary>
    /// What the nitrous does, now that what it looked like has been taken out.
    ///
    /// The exhaust flame was built because Ron asked for continuous fire out of the pipes.
    /// It fired the gear-change backfire on a cadence, and in play that read as flashing:
    /// a one-shot puff repeated on a timer flashes at any interval, because every burst
    /// has its own fade. He asked for it gone, so it is gone, and these checks keep it
    /// gone — the speed side is the part of that package that stayed.
    /// </summary>
    static void NitrousSpeedChecks()
    {
        string nitrous = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "Nitrous.cs"));
        string tuning = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "WorldTuning.cs"));

        // ---- The flame is out, and nothing is left calling into it.
        Check(!File.Exists(Path.Combine(Repo, "src", "Bloodlines", "Core", "ExhaustFlame.cs")),
            "The exhaust flame is removed rather than left switched off");
        var callers = Directory.GetFiles(Path.Combine(Repo, "src", "Bloodlines"), "*.cs", SearchOption.AllDirectories)
            .Where(file => File.ReadAllText(file).Contains("ExhaustFlame")).ToList();
        Check(callers.Count == 0, "and nothing in the mod still reaches for it");

        // ---- The speed side. Torque alone was the whole boost, and the entity cap was the
        // same number open or shut, which is exactly what "it only feels like acceleration"
        // means. That half of the package is the half that stayed.
        Check(Nitrous.SpeedMultiplier > 1f,
            "The boost raises the car's ceiling as well as its torque");
        Check(tuning.Contains("private void ApplyCeiling(Vehicle car, bool boosting)") &&
              tuning.Contains("ApplyCeiling(car, Nitrous.MultiplierFor(car) > 1f);"),
            "and the tuning loop raises that ceiling while the bottle is open");
        Check(tuning.Contains("_appliedLimits.TryGetValue(car.Handle, out float applied)"),
            "The ceiling is per instance, worked out from that car's own applied limit");
        Check(tuning.Contains("Math.Abs(current - want) < .01f) return;"),
            "and it is written only when the number changes, not every frame");
        Check(!tuning.Contains("InitialDriveMaxFlatVelocity = profile.Applied * Nitrous"),
            "Nothing multiplies the shared handling data per car");

        // ---- Teardown still puts the bottles back without reaching for anything else.
        Check(nitrous.Contains("public void Reset() { _boosted = null; _bottles.Clear(); Charge = 1f; }"),
            "Teardown is the bottles and the charge, with nothing left to tear down");
    }
}
