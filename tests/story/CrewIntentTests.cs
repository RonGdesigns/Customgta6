using System;
using System.Collections.Generic;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

/// <summary>Two brothers' jobs, one after the other, and a third man posted on purpose.</summary>
public sealed class IntentProbe : ComposedMission
{
    public bool GohanDone, IceDone;
    public override string Id => "M06";
    public override string Title => "Intent";
    protected override bool Setup() { Station(CrewSlot.Guess, new Vector3(30f, 0f, 0f)); return true; }
    protected override IEnumerable<MissionStage> BuildStages()
    {
        yield return new MissionStage("Gohan's job", new ConditionObjective("Gohan: the job", () => GohanDone)).OwnedBy(CrewSlot.Gohan);
        yield return new MissionStage("Ice's job", new ConditionObjective("Ice: the job", () => IceDone)).OwnedBy(CrewSlot.Ice);
    }
}

public static partial class StoryTests
{
    /// <summary>
    /// Ron, September 22: "when they're not doing anything they stand still ... if we finish what
    /// they're supposed to do they stand still and do nothing." A brother with no job left comes
    /// back to the player; one with a job, or a post, stays where the mission wants him.
    /// </summary>
    static void CrewIntentChecks()
    {
        FinishedJobRejoinChecks();
        StartPostRejoinChecks();
        RoleClaimChecks();
    }

    static void FinishedJobRejoinChecks()
    {
        Reset();
        var crew = Roster(); crew.PedFor(CrewSlot.Guess).Position = new Vector3(30f, 0f, 0f);
        var c = Context(crew);
        var m = new IntentProbe();
        Check(m.Begin(c), "The intent probe starts");
        var gohan = crew.PedFor(CrewSlot.Gohan);
        for (int i = 0; i < 3; i++) { Game.GameTime += ComposedMission.RejoinIdleMs; m.Tick(); }
        Check(!crew.CompanionAI.IsRejoining(CrewSlot.Gohan), "A brother with his own job open is left to it, however still he stands");

        m.GohanDone = true; m.Tick();
        Check(m.CurrentStage == 1, "His job done, the mission moves on to Ice's");
        m.Tick();
        Check(!crew.CompanionAI.IsRejoining(CrewSlot.Gohan), "He is given a moment before anything decides he is idle");
        Game.GameTime += ComposedMission.RejoinIdleMs + 1; m.Tick();
        Check(crew.CompanionAI.IsRejoining(CrewSlot.Gohan), "With nothing left to do, he comes back to the player instead of standing where the job ended");
        Check(!crew.CompanionAI.IsRejoining(CrewSlot.Guess), "A brother the mission posted holds his post");
        m.Abort();
        Check(!crew.CompanionAI.IsRejoining(CrewSlot.Gohan), "The mission's end hands everyone back to free roam");
    }

    static void StartPostRejoinChecks()
    {
        // BM01's start: the crew is seated, so Gohan is taken out of the truck and left standing
        // beside it while Guess drives the first leg.
        var m = StartClippedWings(out var crew, out var c);
        var gohan = crew.PedFor(CrewSlot.Gohan);
        gohan.Task.LeaveVehicle();
        gohan.Position = m.Truck.Position + new Vector3(3f, 0f, 0f);
        m.Tick();
        Game.GameTime += ComposedMission.RejoinIdleMs + 1; m.Tick();
        Check(crew.CompanionAI.IsRejoining(CrewSlot.Gohan),
            "Left waiting where the mission started him, with no job of his own, he comes to the player");
        for (int i = 0; i < 4; i++) { Game.GameTime += 2600; m.Tick(); }
        Check(crew.CompanionAI.IsRejoining(CrewSlot.Gohan) && !crew.CompanionAI.Controlled.Contains(CrewSlot.Gohan),
            "and the mission's support pass does not take him straight back");
        Check(!crew.CompanionAI.IsRejoining(CrewSlot.Ice), "A brother still in his seat is the vehicle's, not idle");
        m.Abort();
    }

    static void RoleClaimChecks()
    {
        Reset();
        var crew = Roster();
        var roles = new RoleTracks(crew, () => new Ped[0]);
        var track = roles.For(CrewSlot.Gohan);
        crew.CompanionAI.Rejoin(CrewSlot.Gohan);
        track.Approach(new Vector3(200f, 0f, 0f), new Vector3(210f, 0f, 0f));
        Check(!crew.CompanionAI.IsRejoining(CrewSlot.Gohan) && crew.CompanionAI.Controlled.Contains(CrewSlot.Gohan),
            "Anything the mission gives him next takes him back from the player");
        crew.CompanionAI.Rejoin(CrewSlot.Gohan);
        track.Stop();
        Check(crew.CompanionAI.IsRejoining(CrewSlot.Gohan), "Stopping his role is not a new order and does not take him back");
        roles.Release();
    }
}
