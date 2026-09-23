using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using GTA;
using GTA.Math;
using GTA.Native;

public static partial class StoryTests
{
    /// <summary>
    /// Actors who look at each other, faces that move, and shots that slide instead of
    /// cutting. Before this there was not one look-at or facial call anywhere in the
    /// director, the blocking or the steps: three men stood facing whichever way they
    /// arrived, staring through each other while the lines played.
    /// </summary>
    static void ScenePerformanceChecks()
    {
        var performance = new ScenePerformance();
        var ice = new Ped { Position = new Vector3(0f, 0f, 0f) };
        var gohan = new Ped { Position = new Vector3(2f, 0f, 0f) };
        var guess = new Ped { Position = new Vector3(0f, 2f, 0f) };
        var faraway = new Ped { Position = new Vector3(400f, 0f, 0f) };

        Check(!performance.IsActive, "Nothing is being performed until a scene takes a cast");
        performance.Speak(ice);
        Check(performance.Speaker == null, "and a line with no cast turns nobody's head");

        // ---- The room turns to whoever is talking.
        Function.Calls.Clear();
        performance.Begin(new[] { ice, gohan, guess, faraway });
        Check(performance.IsActive, "A scene takes its cast");
        Check(Function.Calls.Count(c => c.Item1 == Hash.SET_FACIAL_IDLE_ANIM_OVERRIDE) == 4,
            "and gives every one of them a face rather than the neutral idle");

        Function.Calls.Clear();
        performance.Speak(ice);
        var looks = Function.Calls.Where(c => c.Item1 == Hash.TASK_LOOK_AT_ENTITY).ToList();
        Check(looks.Count == 3, "Two listeners in the room look at the speaker, and he looks back at one of them");
        Check(looks.Any(c => ReferenceEquals(c.Item2[0], gohan) && ReferenceEquals(c.Item2[1], ice)) &&
              looks.Any(c => ReferenceEquals(c.Item2[0], guess) && ReferenceEquals(c.Item2[1], ice)),
            "Each of them is looking at him specifically");
        Check(looks.Any(c => ReferenceEquals(c.Item2[0], ice)),
            "and he is looking at one of them rather than into the middle distance");
        Check(!looks.Any(c => ReferenceEquals(c.Item2[0], faraway)),
            "Somebody four hundred meters away is not made to stare across the map");
        Check(performance.Speaker == ice, "The performance knows who is talking");

        // ---- Issued once. A head-track re-ordered every line restarts before the neck
        // has moved, which is the fault that made guards stand still in M31.
        Function.Calls.Clear();
        performance.Speak(ice);
        performance.Speak(ice);
        Check(!Function.Calls.Any(c => c.Item1 == Hash.TASK_LOOK_AT_ENTITY),
            "Six lines from one man are six lines, not six restarted head turns");

        // ---- A new speaker turns the room again, and the faces swap over.
        Function.Calls.Clear();
        performance.Speak(gohan);
        Check(Function.Calls.Count(c => c.Item1 == Hash.TASK_LOOK_AT_ENTITY) == 3,
            "A change of speaker turns the room again");
        Check(Function.Calls.Count(c => c.Item1 == Hash.SET_FACIAL_IDLE_ANIM_OVERRIDE) == 2,
            "and exactly two faces change: the one who stopped talking and the one who started");

        // ---- A scene tone overrides both, for a scene that is going badly.
        performance.End();
        Function.Calls.Clear();
        performance.Begin(new[] { ice, gohan }, "mood_stressed_1");
        Check(performance.Tone == "mood_stressed_1", "A scene can put one mood over the whole cast");
        Check(Function.Calls.Where(c => c.Item1 == Hash.SET_FACIAL_IDLE_ANIM_OVERRIDE)
                  .All(c => (string)c.Item2[1] == "mood_stressed_1"),
            "and everybody wears it instead of the talking and listening faces");

        // ---- Everything is put back. A brother left in a scene's angry face wears it for
        // the rest of the session.
        Function.Calls.Clear();
        performance.End();
        Check(Function.Calls.Count(c => c.Item1 == Hash.CLEAR_FACIAL_IDLE_ANIM_OVERRIDE) == 2 &&
              Function.Calls.Count(c => c.Item1 == Hash.TASK_CLEAR_LOOK_AT) == 2,
            "Every face and every head is released when the scene ends");
        Check(!performance.IsActive && performance.Speaker == null, "and the performance is over");
        Function.Calls.Clear();
        performance.End();
        Check(!Function.Calls.Any(), "Ending twice releases nothing twice");

        // ---- A dead or missing actor is skipped rather than thrown over.
        var gone = new Ped(); gone.Present = false;
        var dead = new Ped { IsDead = true };
        performance.Begin(new[] { ice, gone, dead, null });
        performance.Speak(ice);
        Check(performance.IsActive, "A cast with a missing and a dead actor still performs");
        performance.Speak(dead);
        Check(performance.Speaker == ice, "and a dead man does not become the speaker");
        performance.End();

        // ---- The director: shots slide, the view slides home, and the cast is taken.
        string director = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "CutsceneDirector.cs"));
        Check(director.Contains("Performance.Begin(_actors.Values.Concat(_support.Values));"),
            "The director hands the scene's whole cast to the performance");
        Check(director.Contains("Performance.Speak(actor);"), "and tells it who is speaking on every line");
        Check(director.Contains("Release(\"scene performance\", Performance.End);"),
            "and releases it with everything else, on every exit path");
        Check(!director.Contains("_camera.Position = actor.Position + offset;"),
            "No shot is a teleport any more");
        Check(director.Contains("private void Frame(Vector3 position, Vector3 lookAt, bool ease = true)") &&
              director.Contains("float e = t * t * (3f - 2f * t);"),
            "Shots slide, and they leave and arrive slowly rather than at a constant rate");
        Check(director.Contains("subject.Position + new Vector3(0f, 0f, 0.7f), ease: false);"),
            "except a tracking shot, which would lag the thing it is following");
        Check(director.Contains("Function.Call(Hash.RENDER_SCRIPT_CAMS, false, true, HandoffMs, true, false, 0);"),
            "The view eases back into the player's own camera instead of cutting to it");
        Check(director.Contains("bool smooth = _previousCamera == null"),
            "but only when nobody else owns a scripted camera to restore");
        Check(director.Contains("_retiring = _camera; _retireAt = Game.GameTime + HandoffMs + 250;"),
            "and the camera outlives the slide home, because deleting it mid-interpolation is a cut");
        string host = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "BloodlinesMain.cs"));
        Check(host.Contains("Step(\"scene cameras\", _cutscenes.RetireCameras);"),
            "with the host deleting it afterward, which is after the scene has ended");
    }
}
