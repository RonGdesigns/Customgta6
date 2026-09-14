using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using GTA;
using GTA.Math;
using GTA.Native;

public static partial class StoryTests
{
    static void SurfaceAndHoldChecks()
    {
        // ---- A relocation of 35 meters is a different place, not a correction. That is
        // how M40's hull kits and navigation laptop ended up in the sea beside the pier.
        Check(MissionSites.EstimateDrift < 20f && MissionSites.SurveyedDrift <= 3f,
            "Ground preparation corrects an authored point by a stride, not by a block");
        Check(MissionSites.SurveyedDrift < MissionSites.EstimateDrift,
            "A point Ron surveyed himself is held tighter than a guess");

        // ---- The escape hatch exists and the pier mission now uses it.
        string m40 = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act2", "M40ThePhantomRigging.cs"));
        foreach (var key in new[] { "M40.Kit1", "M40.Kit2", "M40.Nav", "M40.NavWork" })
            Check(m40.Contains("\"" + key + "\""), key + " is declared as a built surface, so it keeps its pier height");
        string preparation = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act2", "PreparationOperation.cs"));
        Check(preparation.Contains("MissionSites.Prepare(Ctx.Locations, Id, FixedSurfaces)"),
            "Every preparation mission can declare the surfaces that must not snap to terrain");

        // ---- Nothing is stacked on nothing. A released model reports no dimensions, and
        // the item then sits inside the surface: an invisible laptop, which is what M43 had.
        Reset();
        var table = new Prop { Position = new Vector3(10f, 20f, 30f), Model = new Model("prop_table_03") };
        var bare = new Model("prop_table_03");
        var laptop = new Model("prop_laptop_01a");
        var spot = PropPlacement.OnTop(table, bare, laptop);
        Check(spot.Z >= table.Position.Z + PropPlacement.AssumedSurfaceHeight - 0.05f,
            "An item goes on top of a surface even when the surface will not report its height");
        Check(Math.Abs(spot.X - table.Position.X) < 0.5f && Math.Abs(spot.Y - table.Position.Y) < 0.5f,
            "And it stays over the surface rather than beside it");
        int refused = 0;
        try { PropPlacement.OnTop(null, bare, laptop); } catch (ArgumentException) { refused++; }
        Check(refused == 1, "Placing something on nothing is refused rather than producing a coordinate");

        // ---- A brother's aircraft keeps flying while the player is somebody else.
        Reset();
        var crew = Roster();
        var chopper = new Vehicle { Model = new Model("annihilator"), Position = new Vector3(0f, 0f, 40f) };
        var guess = crew.PedFor(CrewSlot.Guess);
        guess.SetIntoVehicle(chopper, VehicleSeat.Driver);
        var hold = new AircraftHold();
        Use(crew, CrewSlot.Guess);
        hold.Update(crew, CrewSlot.Guess, chopper, Vector3.Zero, 40);
        Check(!hold.Holding && guess.Task.HeliTasks == 0,
            "Nothing is ordered while the player is flying it himself");
        Use(crew, CrewSlot.Gohan);
        hold.Update(crew, CrewSlot.Guess, chopper, Vector3.Zero, 40);
        Check(hold.Holding && guess.Task.HeliTasks == 1 && chopper.IsEngineRunning,
            "Switching away puts him in a holding pattern instead of letting the helicopter come down");
        hold.Update(crew, CrewSlot.Guess, chopper, Vector3.Zero, 40);
        Check(guess.Task.HeliTasks == 1, "The pattern is not reissued every frame, which would restart the task");
        Game.GameTime += AircraftHold.OrderIntervalMs + 1;
        hold.Update(crew, CrewSlot.Guess, chopper, Vector3.Zero, 40);
        Check(guess.Task.HeliTasks == 2, "It is refreshed on its own cadence, so a cleared task recovers");
        Use(crew, CrewSlot.Guess);
        hold.Update(crew, CrewSlot.Guess, chopper, Vector3.Zero, 40);
        Check(!hold.Holding, "Taking it back releases the pattern at once");

        // A pilot who is not in it, or an aircraft that is gone, is not ordered anywhere.
        Use(crew, CrewSlot.Gohan);
        guess.Task.LeaveVehicle();
        chopper.Seats.Clear();
        int before = guess.Task.HeliTasks;
        Game.GameTime += AircraftHold.OrderIntervalMs + 1;
        hold.Update(crew, CrewSlot.Guess, chopper, Vector3.Zero, 40);
        Check(guess.Task.HeliTasks == before && !hold.Holding,
            "A brother who is not in the aircraft is not flying it");

        // ---- An aircraft created in the air has stopped rotors. The engine being on is
        // not lift: Ron's Annihilator fell into the sea on every run at the heist because
        // it was spawned 60 meters up and the blades were still spinning up.
        Reset();
        var fresh = new Vehicle { Model = new Model("annihilator"), Position = new Vector3(0f, 0f, 60f) };
        Check(!fresh.IsEngineRunning && fresh.ForwardSpeed == 0f, "A newly created aircraft is not flying");
        AircraftHold.LaunchAirborne(fresh);
        Check(fresh.IsEngineRunning && fresh.ForwardSpeed == AircraftHold.AirborneSpeed,
            "Launching it airborne runs the engine and gives it approach speed");
        Check(Function.Calls.Any(call => call.Item1 == Hash.SET_HELI_BLADES_FULL_SPEED && ReferenceEquals(call.Item2[0], fresh)),
            "And brings its rotors to speed, which is the part that keeps it up");
        AircraftHold.LaunchAirborne(null);
        Check(true, "Launching nothing is harmless rather than a crash during setup");

        string m47 = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act2", "M47PaletoCollapse.cs"));
        Check(m47.Contains("AircraftHold.LaunchAirborne"),
            "The collapse chapter stages its helicopter airborne the same way");

        // ---- M45 holds Guess over the vessel for the whole of the boarding.
        string m45 = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act2", "M45PaletoBreach.cs"));
        Check(m45.Contains("_hold.Update(Ctx.Crew, CrewSlot.Guess"),
            "M45 keeps its helicopter flown while the player is Ice or Gohan");
    }
}
