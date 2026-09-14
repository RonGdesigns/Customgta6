using System;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using GTA;
using GTA.Math;

public static partial class StoryTests
{
    // Runs before Main; these checks deliberately need no campaign data directory.
    static StoryTests()
    {
        SharedSystemsFoundationChecks();
    }

    static void SharedSystemsFoundationChecks()
    {
        var doctor = new MissionDoctor();
        doctor.BeginMission("M55", "Skyline Descent");
        doctor.Warn("placement", "M55.IceRoof", "roof needs live proof");
        doctor.Error("operation", "M55", "one terminal missing");
        Check(doctor.WarningCount == 1 && doctor.ErrorCount == 1 && doctor.Summary().Contains("M55"),
            "Mission Doctor keeps structured warning/error counts without touching campaign state");
        for (int i = 0; i < MissionDoctor.Capacity + 5; i++) doctor.Info("probe", "row" + i, "bounded");
        Check(doctor.Entries.Count == MissionDoctor.Capacity && doctor.Entries[0].Sequence > 1,
            "Mission Doctor remains bounded instead of growing every tick for a long play session");

        var aircraft = PlacementContract.Aircraft("M26.RunwayStart", new Model("lazer"), 45f);
        Check(aircraft.Kind == PlacementKind.Aircraft && Math.Abs(aircraft.DepartureMeters - 45f) < .01f,
            "Placement contracts carry model-purpose and departure-clearance intent separately from raw coordinates");

        Reset();
        var actor = new Ped { Position = Vector3.Zero, Health = 300 };
        var enemy = new Ped { Position = new Vector3(100f, 0f, 0f), Health = 300 };
        float progress = 0f;
        int starts = 0, updates = 0, suspends = 0, resumes = 0, cancels = 0;
        var action = new DelegateRoleAction("terminal handshake",
            p => starts++,
            p => { updates++; progress += .20f; },
            () => progress >= 1f,
            () => progress,
            p => suspends++,
            p => resumes++,
            p => cancels++);
        var track = new RoleTrack(CrewSlot.Gohan, actor, () => new[] { enemy });
        track.Work(Vector3.Zero, new Vector3(10f, 0f, 0f), action);
        track.Update();
        Check(starts == 1 && updates == 1 && track.State == RoleState.Working && Math.Abs(track.ActionProgress - .20f) < .01f,
            "Inactive brother starts real role work and exposes its preserved progress");

        enemy.Position = new Vector3(1f, 0f, 0f);
        track.Update();
        Check(track.State == RoleState.Threatened && suspends == 1 && updates == 1,
            "Threat response suspends companion work instead of advancing it under fire");
        enemy.Position = new Vector3(100f, 0f, 0f);
        Game.GameTime += 1;
        track.Update();
        Game.GameTime += RoleTrack.ClearMs + 1;
        track.Update();
        Check(track.State == RoleState.Working && resumes == 1 && Math.Abs(track.ActionProgress - .20f) < .01f,
            "Clearing the threat resumes the same role action without resetting its progress");

        track.Update();
        track.PlayerTookControl();
        Check(suspends == 2 && Math.Abs(track.ActionProgress - .40f) < .01f,
            "Taking player control suspends inactive AI work at the current progress");
        track.Update();
        Check(resumes == 2 && Math.Abs(track.ActionProgress - .60f) < .01f,
            "Handing the brother back to AI resumes the existing work rather than starting over");
        track.Stop();
        Check(cancels == 1 && action.Canceled,
            "Mission teardown cancels unfinished role work exactly once");
    }
}
