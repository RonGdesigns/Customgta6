using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using GTA;
using GTA.Math;

public static partial class StoryTests
{
    static void NitrousFlameChecks()
    {
        string flame = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "ExhaustFlame.cs"));
        string nitrous = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "Nitrous.cs"));
        string tuning = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "WorldTuning.cs"));

        // ---- It is the gear-change flame, which is what Ron pointed at. Same dictionary, same
        // effect name, asked for repeatedly instead of once.
        Check(ExhaustFlame.Asset == "core" && ExhaustFlame.Effect == "veh_backfire",
            "The flame is the game's own gear-change backfire out of the core dictionary");
        Check(ExhaustFlame.BurstIntervalMs > 0 && ExhaustFlame.BurstIntervalMs <= 120,
            "and it bursts often enough to read as continuous fire");

        // ---- It must never wait. This runs in the per-frame input step, and AircraftSmoke's
        // Script.Wait loop would stall the whole mod for seconds to stream a particle asset.
        // The call, not the word: the class comment explains why it does not wait, and an
        // assertion that matches prose fails on its own documentation.
        Check(!flame.Contains("Script.Wait("),
            "The flame never waits on the dictionary, because it runs every frame");
        Check(flame.Contains("asset.Request(0)"),
            "It asks with no timeout and emits on a later frame once the asset is in");

        // ---- Nitrous holds it while the bottle is open, and there is no handle to leak.
        Check(nitrous.Contains("ExhaustFlame.Hold(car);"),
            "Nitrous holds the flame on while boosting");
        Check(nitrous.Contains("ExhaustFlame.Reset();"),
            "and teardown drops the streamed state with everything else");
        Check(!flame.Contains("StartParticleFxLooped") && !flame.Contains("ParticleEffect ") ,
            "Nothing loops a particle effect, so there is no handle to survive a failed exit");

        // ---- The actual burst path, driven rather than read. The stub counts them.
        ExhaustFlame.Reset();
        World.FailSmoke = false;
        World.SmokeBursts = 0;
        var car = new Vehicle { Position = new Vector3(10f, 20f, 30f) };
        Game.GameTime = 100000;
        Check(ExhaustFlame.Hold(car), "A held flame emits on the frame the dictionary is ready");
        int first = World.SmokeBursts;
        Check(first >= 2, "and it emits from both pipes rather than one point");
        Check(!ExhaustFlame.Hold(car) && World.SmokeBursts == first,
            "A second call inside the interval does not double the bursts");
        Game.GameTime += ExhaustFlame.BurstIntervalMs + 1;
        Check(ExhaustFlame.Hold(car) && World.SmokeBursts > first,
            "and the next burst goes out once the interval has passed");
        Check(!ExhaustFlame.Hold(null), "A missing car is not a flame and not an exception");

        // ---- A dictionary that will not stream is reported, not retried forever in silence.
        ExhaustFlame.Reset();
        World.FailSmoke = true;
        World.SmokeBursts = 0;
        Check(!ExhaustFlame.Hold(car) && World.SmokeBursts == 0 && !ExhaustFlame.Ready,
            "No dictionary means no burst and no claim that it is ready");
        World.FailSmoke = false;

        // ---- The speed side. Torque alone was the whole boost, and the entity cap was the same
        // number open or shut, which is exactly what "it only feels like acceleration" means.
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
    }
}
