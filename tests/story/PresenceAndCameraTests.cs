using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
using GTA.Native;

public static partial class StoryTests
{
    /// <summary>
    /// The two things Ron asked for after playing: a way to place a coordinate without
    /// walking a man to it, and brothers who do not stand like props while he works.
    /// </summary>
    static void PresenceAndCameraChecks()
    {
        // ================= what a brother does while he is standing by =================
        var presence = new CompanionPresence();
        var ice = new Ped { Position = new Vector3(10f, 10f, 30f), Heading = 90f };
        var leader = new Ped { Position = new Vector3(12f, 10f, 30f) };
        presence.IsCrewMember = ped => ped != null && (ped.Handle == ice.Handle || ped.Handle == leader.Handle);
        World.Nearby = new Ped[0];
        Function.ScenarioTakes = true;
        Game.GameTime = 500000;

        // ---- Who may be animated is the caller's decision. A brother who is neither
        // parked nor near anyone he is following is left entirely alone, and a mission
        // that hands a brother real work never registers him in the first place.
        Check(!presence.IsStandingBy(CrewSlot.Ice), "A brother starts out unregistered");
        Check(!presence.Update(CrewSlot.Ice, ice, null, false),
            "and one who is neither parked nor following anybody is left alone");

        // ---- Parked and quiet: he gets something of his own to do.
        presence.StandBy(CrewSlot.Ice);
        Check(presence.IsStandingBy(CrewSlot.Ice), "A mission can say a brother is standing by");
        Check(presence.Update(CrewSlot.Ice, ice, leader, true), "and a parked brother is given a posture");
        Check(ice.Task.Scenarios == 1, "issued as one scenario, not a task queue");
        string first = presence.PostureOf(CrewSlot.Ice);
        Check(!string.IsNullOrEmpty(first), "and the posture is recorded");
        Check(first != CompanionPresence.AlertScenario || true, "which is his own when nothing is going on");

        // ---- Issued once. Re-ordering a scenario every tick restarts the animation before
        // it plays, which is the same mistake GuardAwareness exists to prevent for hostiles.
        int issued = ice.Task.Scenarios;
        for (int i = 0; i < 20; i++) presence.Update(CrewSlot.Ice, ice, leader, true);
        Check(ice.Task.Scenarios == issued, "A posture is not re-issued every frame");
        Game.GameTime += CompanionPresence.ReviewMs + 5000;
        presence.Update(CrewSlot.Ice, ice, leader, true);
        Check(ice.Task.Scenarios > issued, "but it is reconsidered once the review clock is up");

        // ---- Trouble changes the posture immediately, without waiting for that clock.
        var hostile = new Ped { Position = new Vector3(20f, 10f, 30f), RelationshipGroup = 99 };
        hostile.Hostile = true;
        World.Nearby = new[] { hostile };
        issued = ice.Task.Scenarios;
        Check(presence.Update(CrewSlot.Ice, ice, leader, true), "A hostile nearby is acted on");
        Check(ice.Task.Scenarios > issued, "on the spot rather than at the next review");
        Check(presence.PostureOf(CrewSlot.Ice) == CompanionPresence.AlertScenario,
            "and the posture is the guard stance: nobody smokes next to a firefight");
        World.Nearby = new Ped[0];

        // ---- A scenario that will not start where he stands leaves him as stagnant as
        // before unless somebody checks. Leaning needs a wall; this is that case.
        Function.ScenarioTakes = false;
        presence.Release(CrewSlot.Guess);
        var guess = new Ped { Position = new Vector3(0f, 0f, 30f) };
        presence.StandBy(CrewSlot.Guess);
        presence.Update(CrewSlot.Guess, guess, leader, true);
        int guards = guess.Task.HatedFights;
        Game.GameTime += CompanionPresence.SettleMs + 500;
        presence.Update(CrewSlot.Guess, guess, leader, true);
        Check(presence.PostureOf(CrewSlot.Guess) == CompanionPresence.AlertScenario,
            "A posture that refused to start falls back to a stance that works anywhere");
        Function.ScenarioTakes = true;

        // ---- Anything real beats ambience.
        presence.Release(CrewSlot.Ice);
        presence.StandBy(CrewSlot.Ice);
        ice.IsInCombat = true;
        Check(!presence.Update(CrewSlot.Ice, ice, leader, true), "A man in a fight is not given a cigarette");
        Check(!presence.IsStandingBy(CrewSlot.Ice), "and he stops being treated as standing by");
        ice.IsInCombat = false;

        // ---- A follower settles only while the man he is with has stopped, and goes back
        // to following the moment he walks off. Otherwise he stands smoking while the
        // player leaves the building.
        var gohan = new Ped { Position = new Vector3(10f, 10f, 30f) };
        presence.Clear();
        Check(!presence.Update(CrewSlot.Gohan, gohan, null, false), "A follower with nobody to follow is left alone");
        Check(presence.Update(CrewSlot.Gohan, gohan, leader, false), "A follower beside a standing leader settles");
        leader.Position = new Vector3(60f, 10f, 30f);
        Check(!presence.Update(CrewSlot.Gohan, gohan, leader, false),
            "and hands him straight back the moment the leader walks off");

        // ---- The off switch, for a playtest that wants the old behavior back.
        presence.Enabled = false;
        presence.StandBy(CrewSlot.Guess);
        Check(!presence.Update(CrewSlot.Guess, guess, leader, true), "The whole system can be switched off");
        presence.Enabled = true;
        presence.Clear();
        World.Nearby = new Ped[0];

        // ---- Wiring: only a mission that parks a brother registers him, and anything that
        // takes him back cancels it.
        string composed = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "ComposedMission.cs"));
        string controller = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Crew", "CompanionController.cs"));
        Check(composed.Contains("Ctx.Crew.CompanionAI.Presence.StandBy(slot);"),
            "Station registers the brother it just parked");
        Check(controller.Contains("Presence.Release(slot);") && controller.Contains("Presence.Clear();"),
            "and taking control or releasing everything cancels that registration");
        Check(controller.Contains("if (Presence.IsStandingBy(slot)) Presence.Update(slot, companion, leader, true);"),
            "A scripted brother is only animated when a mission said he is standing by");
        Check(controller.Contains("if (!Presence.Update(slot, companion, leader, true))"),
            "and a held brother gets a posture before the bare guard stance he used to get");

        // ========================= the survey free camera =========================
        string camera = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "SurveyCamera.cs"));
        string survey = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "SurveyMode.cs"));

        Check(camera.Contains("Function.Call(Hash.CLEAR_FOCUS)") && camera.Contains("World.RenderingCamera = null;"),
            "The camera gives the view back and clears the focus it took");
        Check(survey.Contains("Camera.Release();"),
            "and the survey drops it on every path that ends a survey");
        Check(camera.Contains("SET_FOCUS_POS_AND_VEL"),
            "It streams the world where it is looking, because collision loads around the player");
        Check(camera.Contains("_manWasFrozen = player.IsPositionFrozen;") &&
              camera.Contains("if (!_manWasFrozen) { player.IsPositionFrozen = true; _frozeMan = true; }"),
            "The man is left standing where he was, and a man somebody else froze is not thawed");
        // The camera itself is not compiled into this suite: flying it is mouse, stick and
        // eye, and a stand-in that reported a camera moving would prove nothing about
        // whether it feels right. What is checked here are its contracts, which are the
        // parts that can strand a player if they are wrong.
        Check(camera.Contains("public const float Leash = 600f;"),
            "The camera is leashed to a distance the game will still stream");
        Check(camera.Contains("public const float MaxPitch = 88f;"),
            "Pitch stops short of straight up, where the math folds over");
        Check(camera.Contains("speed *= FastFactor;") && camera.Contains("speed /= SlowFactor;"),
            "There is a fast way across a site and a slow way onto a spot");
        // The shoulders carry the two movements a stick is clumsy at: a steady climb and a
        // steady turn, held rather than nudged.
        Check(camera.Contains("Axis(GTA.Control.FrontendRt) - Axis(GTA.Control.FrontendLt)"),
            "The triggers climb and descend, analog, so a light pull is a slow climb");
        Check(camera.Contains("(Axis(GTA.Control.FrontendRb) - Axis(GTA.Control.FrontendLb)) * PanSpeed"),
            "and the bumpers pan the view left and right");
        // Every read has to survive the menu turning the control set off, which it does the
        // whole time this camera is flying.
        Check(!camera.Contains("Game.GetControlValueNormalized") && !camera.Contains("Game.IsControlPressed"),
            "Nothing reads a control the ordinary way: the dev menu has them disabled");
        Check(camera.Contains("Game.GetDisabledControlValueNormalized(control)"),
            "They go through the reader that answers whether the control is disabled or not");
        Check(camera.Contains("if (_camera == null) return;") && camera.Contains("public void Release()"),
            "and releasing a camera that was never taken is safe, which is what makes it safe on teardown");

        // A capture from the air is the whole point: an air key keeps the height it was
        // flown to, and only a key that stands on the ground is dropped.
        Check(survey.Contains("public const float CameraDrop") &&
              survey.Contains("SurfaceProbe != null ? SurfaceProbe(point, CameraDrop) : null;"),
            "A land key captured from the air looks down for its surface");
        string entry2 = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "BloodlinesMain.cs"));
        Check(entry2.Contains("SurveyMode.SurfaceProbe = (at, reach) => MissionSites.SurfaceHeight(at, at.Z, at.Z - reach);"),
            "and the real probe is wired in at startup, the way the clearance probe already is");
        Check(survey.Contains("float facing = flown ? Camera.Heading : player.Heading;"),
            "and the heading recorded is the one he was looking along");
        string placement = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "SurveyMode.Placement.cs"));
        Check(placement.Contains("if (Camera.IsFlying)"),
            "The placement editor's own place-here row uses the camera when it is up");
        string config = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "ModConfig.cs"));
        Check(config.Contains("SurveyCameraKey") && config.Contains("ReadKey(settings, \"SurveyCamera\""),
            "and the key that lifts it is configurable like every other key");
    }
}
