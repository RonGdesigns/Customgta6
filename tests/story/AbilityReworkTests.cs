using System;
using System.IO;
using System.Linq;
using Bloodlines.Abilities;
using Bloodlines.Core;
using Bloodlines.Crew;
using GTA;
using GTA.Math;
using GTA.Native;

public static partial class StoryTests
{
    static string AbilitySource(string name) =>
        File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Abilities", name));

    static void AbilityReworkChecks()
    {
        // ---- Who owns what. ThermalPulse and the controller's table use natives the
        // stand-ins do not model, so the assignment is asserted against the source the
        // way ShieldTests and SoloTests already do for native-only code.
        string thermal = AbilitySource("ThermalPulse.cs");
        Check(thermal.Contains("Slot => CrewSlot.Ice"),
            "The thermal sight belongs to Ice, who does the shooting");
        Check(!File.Exists(Path.Combine(Repo, "src", "Bloodlines", "Abilities", "OverwatchFocus.cs")),
            "Overwatch Focus is gone rather than left behind unassigned");
        string controller = AbilitySource("AbilityController.cs");
        Check(controller.Contains("{ CrewSlot.Ice, new ThermalPulse() }") &&
              controller.Contains("{ CrewSlot.Gohan, new Blackout() }") &&
              controller.Contains("{ CrewSlot.Guess, new SlipstreamReflex() }"),
            "All three brothers are mapped, each to one ability");
        Check(!controller.Contains("OverwatchFocus"),
            "Nothing still reaches for the retired ability");

        // ---- The city lights are held by name, so one holder letting go does not
        // cancel another's darkness. M04 and M16 both black the city out.
        Reset();
        WorldLights.Reset();
        Check(!WorldLights.Dark, "The lights start on");
        WorldLights.Darken("M16.Blackout");
        Check(WorldLights.Dark && Convert.ToBoolean(Function.Values[Hash.SET_ARTIFICIAL_LIGHTS_STATE]),
            "A mission takes the lights down");
        WorldLights.Darken(Blackout.LightOwner);
        WorldLights.Restore(Blackout.LightOwner);
        Check(WorldLights.Dark && Convert.ToBoolean(Function.Values[Hash.SET_ARTIFICIAL_LIGHTS_STATE]),
            "The ability letting go does not switch the mission's blackout back on");
        WorldLights.Restore("M16.Blackout");
        Check(!WorldLights.Dark && !Convert.ToBoolean(Function.Values[Hash.SET_ARTIFICIAL_LIGHTS_STATE]),
            "The lights come back when the last holder lets go");
        WorldLights.Darken("stuck");
        WorldLights.Reset();
        Check(!WorldLights.Dark, "Teardown drops every claim");

        // ---- Blackout, entering clean: heat is held rather than earned.
        Reset();
        WorldLights.Reset();
        var crew = Roster();
        var gohan = crew.PedFor(CrewSlot.Gohan);
        var blackout = new Blackout();
        Check(blackout.Slot == CrewSlot.Gohan && blackout.Name == "Blackout", "Blackout is Gohan's");
        Game.Player.WantedLevel = 0;
        blackout.Activate(gohan);
        Check(WorldLights.Dark && blackout.Concealing && GuardAwareness.SuppressAll,
            "Walking into the dark clean conceals him, and mission hostiles stop resolving what they see");
        Game.Player.WantedLevel = 3;
        blackout.Update(gohan);
        Check(Game.Player.WantedLevel == 0 && blackout.DeferredWanted == 3,
            "Heat earned in the dark is held back rather than wiped");
        Game.Player.WantedLevel = 2;
        blackout.Update(gohan);
        Check(blackout.DeferredWanted == 3,
            "The held level is the worst it reached, not the latest reading");

        // Nobody is left to report it, so the grace runs out and it is dropped.
        blackout.Deactivate(gohan);
        Check(!WorldLights.Dark && !GuardAwareness.SuppressAll,
            "The lights and the guards come back when the dark lifts");
        World.Nearby = new Ped[0];
        Check(blackout.Settle(gohan), "While nobody can see him the held heat stays held");
        Check(Game.Player.WantedLevel == 0, "And he is clean for as long as that lasts");
        Game.GameTime += Blackout.ConcealmentGraceMs + 1;
        Check(!blackout.Settle(gohan) && Game.Player.WantedLevel == 0 && blackout.DeferredWanted == 0,
            "Surviving the grace unseen drops what the blackout earned");

        // ---- Blackout, seen afterwards: the held heat lands in full.
        Reset();
        WorldLights.Reset();
        crew = Roster();
        gohan = crew.PedFor(CrewSlot.Gohan);
        blackout = new Blackout();
        Game.Player.WantedLevel = 0;
        blackout.Activate(gohan);
        Game.Player.WantedLevel = 4;
        blackout.Update(gohan);
        blackout.Deactivate(gohan);
        var officer = new Ped { Position = gohan.Position + new Vector3(0f, 5f, 0f), IsCop = true };
        World.Nearby = new[] { officer };
        Function.ClearLos = true;
        Check(!blackout.Settle(gohan) && Game.Player.WantedLevel == 4,
            "Being recognized after the lights return brings all of it back at once");
        Check(blackout.DeferredWanted == 0, "And nothing is left held afterwards");

        // A body cannot report anything.
        Reset();
        WorldLights.Reset();
        crew = Roster();
        gohan = crew.PedFor(CrewSlot.Gohan);
        blackout = new Blackout();
        Game.Player.WantedLevel = 0;
        blackout.Activate(gohan);
        Game.Player.WantedLevel = 2;
        blackout.Update(gohan);
        blackout.Deactivate(gohan);
        var dead = new Ped { Position = gohan.Position + new Vector3(0f, 4f, 0f), IsCop = true, IsDead = true };
        World.Nearby = new[] { dead };
        Function.ClearLos = true;
        Check(blackout.Settle(gohan) && Game.Player.WantedLevel == 0,
            "A witness he cleared out reports nothing");

        // ---- Walking in already wanted: the lights go out, nothing is concealed.
        Reset();
        WorldLights.Reset();
        crew = Roster();
        gohan = crew.PedFor(CrewSlot.Gohan);
        blackout = new Blackout();
        Game.Player.WantedLevel = 2;
        blackout.Activate(gohan);
        Check(WorldLights.Dark && !blackout.Concealing,
            "Already wanted means the dark still falls but hides nothing");
        Game.Player.WantedLevel = 4;
        blackout.Update(gohan);
        Check(Game.Player.WantedLevel == 4 && blackout.DeferredWanted == 0,
            "The city does not forget what it is already chasing");
        blackout.Deactivate(gohan);
        Check(!blackout.Settle(gohan), "With nothing held there is nothing to settle");
        Check(!GuardAwareness.SuppressAll && !WorldLights.Dark,
            "Every exit path puts the world back, including this one");
        WorldLights.Reset();
    }
}
