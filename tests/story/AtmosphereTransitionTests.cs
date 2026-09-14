using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using GTA;
using GTA.Native;

public static partial class StoryTests
{
    private sealed class GradePortProbe : ITimecyclePort
    {
        public int Index = -1, Transition = -1, Sets, Writes, Clears;
        public string Current;
        public float Value;
        public bool Reject, FailStrength, FailRead;
        public int ActiveIndex { get { if (FailRead) throw new InvalidOperationException("read failure"); return Index; } }
        public int TransitionIndex => Transition;
        public void Set(string name) { Sets++; if (!Reject) { Current = name; Index = Sets + 10; } }
        public void SetStrength(float strength)
        {
            if (FailStrength) { FailStrength = false; throw new InvalidOperationException("strength failure"); }
            Writes++; Value = strength;
        }
        public void Clear() { Clears++; Index = -1; Current = null; Value = 0; }
    }

    private static void AdvanceGrade(TimecycleGrade grade, string name, float strength, ref int now, int steps = 10)
    {
        for (int i = 0; i < steps; i++) { now = unchecked(now + 100); grade.Update(name, strength, now, false, false); }
    }

    private static void AtmosphereTransitionChecks()
    {
        var port = new GradePortProbe(); var logs = new List<string>();
        var grade = new TimecycleGrade(port, logs.Add); int now = 100;
        grade.Update(null, .4f, now, false, false); grade.Release();
        Check(port.Sets == 0 && port.Writes == 0 && port.Clears == 0, "Unused grade controller makes no writes or clears");
        grade.Update("day", .4f, now, false, true);
        Check(port.Sets == 0, "Paused controller never acquires a new grade");
        grade.Update("day", .4f, now, false, false);
        Check(grade.Name == "day" && grade.Strength == 0 && port.Sets == 1 && port.Value == 0,
            "New grade is engine-index checked and initialized at neutral in the same update");
        AdvanceGrade(grade, "day", .4f, ref now, 5);
        Check(Math.Abs(grade.Strength - .2f) < .0001f && port.Sets == 1, "One-second smoothstep fade reaches half strength halfway through");
        int writes = port.Writes;
        grade.Update("day", .4f, now, false, false);
        Check(port.Writes == writes, "Repeated tick at the same time does not accelerate the grade");
        now += 100; grade.Update("day", .4f, now, false, true);
        Check(Math.Abs(grade.Strength - .2f) < .0001f, "Pause stops the fade without changing the active strength");
        AdvanceGrade(grade, "day", .4f, ref now, 5);
        Check(Math.Abs(grade.Strength - .4f) < .0001f, "Configured strength is reached rather than asymptotically approached");
        writes = port.Writes; AdvanceGrade(grade, "day", .4f, ref now);
        Check(port.Writes == writes && port.Sets == 1, "Settled grade avoids redundant set/strength calls");
        AdvanceGrade(grade, "night", .3f, ref now, 5);
        Check(grade.Name == "day" && grade.Strength > 0 && grade.Strength < .4f && port.Sets == 1,
            "Changed preset first fades the existing grade, rather than snapping names");
        AdvanceGrade(grade, "night", .3f, ref now, 5);
        Check(grade.Name == null && port.Index == -1 && port.Clears == 1, "Outgoing grade reaches neutral before releasing its slot");
        AdvanceGrade(grade, "night", .3f, ref now, 1);
        Check(grade.Name == "night" && grade.Strength == 0 && port.Sets == 2, "Incoming grade starts neutral after the previous one released");
        AdvanceGrade(grade, "night", .3f, ref now);
        Check(Math.Abs(grade.Strength - .3f) < .0001f, "Incoming night grade fades to its own configured strength");
        grade.Update("night", .3f, now, true, false);
        Check(grade.Name == null && port.Clears == 2, "High-priority scene or ability suspension releases our grade immediately");
        grade.Update("night", .3f, now, true, false); grade.Release();
        Check(port.Clears == 2, "Repeated suspension/teardown does not repeatedly clear the global slot");
        grade.Update("night", .3f, now, false, false);
        Check(grade.Strength == 0, "Returning from a suspended scene fades in from neutral");
        int mutations = port.Sets + port.Writes + port.Clears;
        port.Index = 700; port.Current = "foreign"; port.Value = .81f;
        grade.Update("night", .3f, now + 100, false, false); grade.Release();
        Check(grade.Name == null && port.Index == 700 && port.Value == .81f && mutations == port.Sets + port.Writes + port.Clears,
            "Another writer's active index survives both update and our cleanup");
        port.Index = -1; port.Transition = 701; mutations = port.Sets + port.Writes + port.Clears;
        grade.Update("day", .4f, now + 200, false, false);
        Check(mutations == port.Sets + port.Writes + port.Clears, "An incoming transition alone prevents grade acquisition");
        port.Transition = -1; grade.Update("day", .4f, now + 300, false, false); port.Transition = 702;
        mutations = port.Sets + port.Writes + port.Clears;
        grade.Update("day", .4f, now + 400, true, false); grade.Release();
        Check(mutations == port.Sets + port.Writes + port.Clears && port.Transition == 702,
            "Transition takeover is never interrupted by a suspension clear");

        port = new GradePortProbe { Reject = true }; logs.Clear(); grade = new TimecycleGrade(port, logs.Add); now = 100;
        grade.Update("unknown", .4f, now, false, false); AdvanceGrade(grade, "unknown", .4f, ref now, 40); grade.Release();
        grade.Update("unknown", .4f, now + 100, false, false);
        Check(port.Sets == 1 && port.Writes == 0 && port.Clears == 0 && grade.Name == null && logs.Count == 1,
            "Unknown names never claim success, receive strength, or retry/log every frame");
        port.Reject = false; grade.Update("different", .4f, now + 200, false, false);
        Check(grade.Name == "different", "A rejected name does not blacklist another configured name");
        grade.Release();
        grade.Update("day", float.NaN, now, false, false); grade.Update("day", float.PositiveInfinity, now, false, false);
        Check(grade.Name == null && port.Sets == 2, "Non-finite strengths never enter the native graphics register");
        grade.Update("day", 5f, now, false, false); AdvanceGrade(grade, "day", 5f, ref now);
        Check(grade.Strength == 1f, "Finite out-of-range strengths clamp to the supported unit interval");
        AdvanceGrade(grade, null, .4f, ref now);
        Check(grade.Name == null, "A no-grade band fades out and clears, instead of loading a replacement sunset");

        port = new GradePortProbe { FailStrength = true }; grade = new TimecycleGrade(port); now = 100;
        grade.Update("day", .4f, now, false, false); AdvanceGrade(grade, "day", .4f, ref now);
        Check(grade.Faulted && grade.Name == null && port.Clears == 1 && port.Sets == 1,
            "Native write failure releases verified ownership and disables repeated writes for this instance");
        port = new GradePortProbe { FailRead = true }; grade = new TimecycleGrade(port);
        grade.Update("day", .4f, now, false, false);
        Check(grade.Faulted && port.Sets == 0 && port.Clears == 0, "Unavailable ownership getter fails closed without clearing unknown state");
        port = new GradePortProbe(); grade = new TimecycleGrade(port); now = int.MaxValue - 50;
        grade.Update("day", .4f, now, false, false); AdvanceGrade(grade, "day", .4f, ref now);
        Check(Math.Abs(grade.Strength - .4f) < .0001f, "Normal GameTime signed wrap does not freeze or jump the fade");
        grade.Release(); now = 100; grade.Update("day", .4f, now, false, false);
        now += 20000; grade.Update("day", .4f, now, false, false);
        Check(grade.Strength == 0, "Loading-sized time gaps do not finish a fade invisibly");
        grade.FadeSeconds = float.NaN; AdvanceGrade(grade, "day", .4f, ref now);
        Check(Math.Abs(grade.Strength - .4f) < .0001f, "Invalid transition duration falls back to the documented duration");

        // Exercise the actual VisualAtmosphere and its native adapter, not only the isolated driver.
        Reset(); Function.Calls.Clear(); World.CurrentTimeOfDay = TimeSpan.FromHours(13);
        var visuals = new VisualAtmosphere(new ModConfig()); visuals.Update(false, true);
        for (int i = 0; i < 10; i++) { Game.GameTime += 100; visuals.Update(false, true); }
        Check(visuals.ActiveModifier == "cinema_default" && Math.Abs(visuals.ActiveStrength - .35f) < .0001f,
            "Production visual controller uses checked native grade and smooth strength");
        visuals.ToggleGradingComparison();
        for (int i = 0; i < 10; i++) { Game.GameTime += 100; visuals.Update(false, true); }
        Check(visuals.GradingComparisonOff && visuals.ActiveModifier == null && visuals.LodConfigured,
            "Session comparison removes only grading and retains the other visual settings");
        Check(GameUtils.ClockCalls == 0 && GameUtils.WeatherCalls == 0 && Calls(Hash.SET_DEEP_OCEAN_SCALER) == 0 && Game.Player.CanControlCharacter,
            "Grading comparison does not change time, weather, mission water or player control");
        visuals.ToggleGradingComparison(); Game.GameTime += 100; visuals.Update(false, true);
        Check(!visuals.GradingComparisonOff && visuals.ActiveModifier != null && visuals.ActiveStrength == 0,
            "A/B return re-acquires configured grading without rewriting config or save");
        Function.TimecycleIndex = 333; Function.TimecycleStrength = .77f; int clears = Calls(Hash.CLEAR_TIMECYCLE_MODIFIER);
        visuals.Reset(); visuals.Reset();
        Check(Function.TimecycleIndex == 333 && Function.TimecycleStrength == .77f && Calls(Hash.CLEAR_TIMECYCLE_MODIFIER) == clears && Calls(Hash.RESET_DEEP_OCEAN_SCALER) == 0,
            "Real adapter cleanup preserves foreign grading and water settings we never changed");
        Function.Calls.Clear(); var unused = new VisualAtmosphere(new ModConfig { VisualsEnabled = false }); unused.Reset();
        Check(Function.Calls.Count == 0, "Unused visual reset no longer touches global graphics registers");
        string host = File.ReadAllText(Path.Combine(Repo, "src/Bloodlines/BloodlinesMain.cs"));
        Check(host.Contains("_homes.Apartment.Inside || _abilities.IsActive") && host.Contains("Step(\"yield visual grade\", _visuals.SuspendGrading)") && host.Contains("Step(\"yield new scene grade\", _visuals.SuspendGrading)"),
            "Host wires suppression before early-return paths and after a newly started scene");
        string menu = File.ReadAllText(Path.Combine(Repo, "src/Bloodlines/Core/DevMenu.cs"));
        Check(menu.Contains("Grading comparison") && menu.Contains("Visuals.ToggleGradingComparison()") && host.Contains("_menu.Visuals = _visuals;"),
            "Existing World menu exposes the same controller's session-only grade comparison");
        Reset(); Function.Calls.Clear();
    }
}
