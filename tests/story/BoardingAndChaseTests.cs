using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Campaign;
using GTA;
using GTA.Math;
using GTA.Native;

public static partial class StoryTests
{
    static void BoardingAndChaseChecks()
    {
        // ---------------------------------------------------------------- boarding
        // Nothing in the campaign ever ordered a brother into a vehicle. Both
        // EnterVehicleObjective(requireCrew) and a hand-written "everyone aboard"
        // condition only ask whether he is already in it, so the Paleto pickup waited
        // on two men treading water and the run south waited on a man on a beach.
        Reset();
        var crew = Roster();
        Use(crew, CrewSlot.Guess);
        var boat = new Vehicle { Model = new Model("tropic"), Position = new Vector3(0f, 0f, 0f) };
        var guess = crew.PedFor(CrewSlot.Guess);
        guess.SetIntoVehicle(boat, VehicleSeat.Driver);
        var ice = crew.PedFor(CrewSlot.Ice);
        var gohan = crew.PedFor(CrewSlot.Gohan);
        ice.Position = new Vector3(6f, 0f, 0f);
        gohan.Position = new Vector3(0f, 7f, 0f);

        var boarding = new CrewBoarding();
        var seats = CrewBoarding.Passengers(CrewSlot.Guess);
        Check(seats.Length == 2 && seats.All(pair => pair.Key != CrewSlot.Guess),
            "The driver is not one of the men being told to get in beside him");

        bool all = boarding.Update(crew, boat, seats, "TEST");
        Check(!all, "Two men in the water is not everybody aboard");
        Check(ice.Task.Enters == 1 && gohan.Task.Enters == 1,
            "Both of them are actually told to board rather than only counted");

        // Re-tasking a ped restarts the swim he is part way through, so the order is
        // repeated on its own cadence and not every frame.
        boarding.Update(crew, boat, seats, "TEST");
        Check(ice.Task.Enters == 1, "The order is not reissued every frame");
        Game.GameTime += CrewBoarding.OrderIntervalMs + 1;
        boarding.Update(crew, boat, seats, "TEST");
        Check(ice.Task.Enters == 2, "It is repeated on a cadence, so a cleared task recovers");

        // The player boards himself: ordering the active character would take the
        // controls out of his hands mid-swim.
        Use(crew, CrewSlot.Ice);
        int before = ice.Task.Enters;
        Game.GameTime += CrewBoarding.OrderIntervalMs + 1;
        boarding.Update(crew, boat, seats, "TEST");
        Check(ice.Task.Enters == before, "The player is never ordered into his own boat");
        Use(crew, CrewSlot.Guess);

        // A man who is in it is finished with; when both are, the boarding reports done.
        ice.SetIntoVehicle(boat, VehicleSeat.RightFront);
        gohan.SetIntoVehicle(boat, VehicleSeat.LeftRear);
        Check(boarding.Update(crew, boat, seats, "TEST"), "With both aboard the boarding is complete");

        // ---- The patience clock only runs while he is close enough to be boarding.
        // Without that split, the fallback would teleport two swimmers into a boat that
        // was still two hundred meters away, skipping the pickup Ron is driving.
        Reset();
        crew = Roster();
        Use(crew, CrewSlot.Guess);
        boat = new Vehicle { Model = new Model("tropic"), Position = Vector3.Zero };
        crew.PedFor(CrewSlot.Guess).SetIntoVehicle(boat, VehicleSeat.Driver);
        ice = crew.PedFor(CrewSlot.Ice);
        gohan = crew.PedFor(CrewSlot.Gohan);
        ice.Position = new Vector3(300f, 0f, 0f);
        gohan.Position = new Vector3(300f, 5f, 0f);
        var distant = new CrewBoarding();
        for (int i = 0; i < 40; i++) { Game.GameTime += 1000; distant.Update(crew, boat, seats, "TEST"); }
        Check(!ice.IsInVehicle(boat) && distant.Placed == 0,
            "A man still swimming toward the boat is not teleported into it");
        Check(ice.Task.Enters > 1, "He is still ordered while he is on his way — the swim is part of the task");

        // Alongside and still not in after the patience runs out, he is placed, because a
        // boarding step must never be able to strand the player.
        ice.Position = new Vector3(3f, 0f, 0f);
        gohan.Position = new Vector3(0f, 3f, 0f);
        distant.Update(crew, boat, seats, "TEST");
        Check(!ice.IsInVehicle(boat), "Arriving beside it starts his clock rather than seating him at once");
        Game.GameTime += CrewBoarding.DirectAfterMs + 1;
        distant.Update(crew, boat, seats, "TEST");
        Check(ice.IsInVehicle(boat) && gohan.IsInVehicle(boat) && distant.Placed == 2,
            "A man who never gets in on his own is put in, and it is recorded as that");

        // ---- Both unplayed chapters call it, and the run south cannot be finished
        // with a brother left at the roadblock.
        string m47 = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act2", "M47PaletoCollapse.cs"));
        Check(m47.Contains("_pickup.Update(Ctx.Crew, _boat"),
            "M47 tells the swimmers to board instead of waiting to notice that they did");
        string m48 = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act2", "M48TheRoadBackSouth.cs"));
        Check(m48.Contains("_boarding.Update(Ctx.Crew, _technical"),
            "M48 tells the crew to get into the technical");
        Check(m48.Contains("\"Everyone rides south in the technical\""),
            "And the drive south cannot complete with a brother still at the cordon, which OnPassed would have thrown on");

        // ---------------------------------------------------------------- the insertion
        // M45 put Ice in a helicopter at the 40 meter hold marker and then asked him to
        // reach a deck at 15.5: a 24 meter fall onto steel, every time the chapter ran.
        string m45 = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act2", "M45PaletoBreach.cs"));
        Check(M45PaletoBreach.InsertionHeight <= 6f,
            "Ice steps off from a height a man survives");
        float pad = KeyHeight("M45.Helipad"), hold = KeyHeight("M45.Hold");
        Check(hold - pad > 15f, "The hold marker really is far above the deck — the drop was the bug, not a misreading");
        Check(m45.Contains("() => Insertion"),
            "The helicopter is brought over the deck rather than to the circuit marker 50 meters away from it");
        Check(!m45.Contains("new ReachZoneObjective(\"Ice: get out onto the upper deck\""),
            "A zone on the pad is gone: the helicopter over it satisfied that while Ice was still seated");
        Check(m45.Contains("IceOnDeck"), "What the stage waits for is Ice standing on the deck");

        // The deck detail used to be placed with GameUtils.OnGround, and the ground
        // under a point 15 meters up on a vessel is the sea.
        Check(!m45.Contains("GameUtils.OnGround(post)"),
            "The deck guards are not snapped to the water under the hull");

        // ---- A hover holds the spot; a circuit does not, and an insertion needs the spot.
        Reset();
        crew = Roster();
        var chopper = new Vehicle { Model = new Model("annihilator"), Position = new Vector3(0f, 0f, 20f), IsInAir = true };
        guess = crew.PedFor(CrewSlot.Guess);
        guess.SetIntoVehicle(chopper, VehicleSeat.Driver);
        var hold2 = new AircraftHold();
        Use(crew, CrewSlot.Ice);
        hold2.Update(crew, CrewSlot.Guess, chopper, Vector3.Zero, 20, true);
        Check(hold2.Holding && hold2.Hovering, "Asked for a hover, he hovers");
        // A change of pattern is not made to wait for the next cadence: Ice is already
        // on the deck and the helicopter should be climbing away now.
        hold2.Update(crew, CrewSlot.Guess, chopper, Vector3.Zero, 40, false);
        Check(hold2.Holding && !hold2.Hovering, "Switching to the circuit takes effect at once");

        // A helicopter sitting on something is left sitting on it. Ordering a pattern
        // would lift it off a deck somebody is standing on.
        chopper.IsInAir = false;
        Game.GameTime += AircraftHold.OrderIntervalMs + 1;
        hold2.Update(crew, CrewSlot.Guess, chopper, Vector3.Zero, 40, false);
        Check(!hold2.Holding, "A parked aircraft is not ordered into the air — it cannot fall");

        // ---------------------------------------------------------------- the chase
        // Ron lost the Alamo spotters the moment he passed them. A 1.5 meter cylinder is
        // a dot at the range an air chase happens at, and a 60 meter orbit is a bank a
        // jet cannot turn inside.
        Check(M26AlamoScramble.PatrolRadius >= 300f,
            "The spotters quarter the lake widely enough for a jet to line up on them");
        string m26 = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act2", "M26AlamoScramble.cs"));
        Check(m26.Contains("blip.IsShortRange = false"),
            "Their blips survive the distance the chase is fought over");
        Reset();
        var target = new Vehicle { Model = new Model("mammatus"), Position = new Vector3(0f, 0f, 0f) };
        Game.Player.Character.Position = new Vector3(0f, 0f, 0f);
        float near = Bloodlines.Missions.Objectives.DestroyVehicleObjective.MarkerRadius(target);
        Game.Player.Character.Position = new Vector3(600f, 0f, 0f);
        float far = Bloodlines.Missions.Objectives.DestroyVehicleObjective.MarkerRadius(target);
        Check(near <= 2f, "Up close the marker does not cover the thing he is shooting at");
        Check(far >= 20f && far <= 45f, "At the range of an air chase it is big enough to turn back toward, and bounded");

        // ------------------------------------------------------------------ M27
        // The Shamal was told to cruise at 42 m/s and created at 45, both under what a
        // jet stays airborne on. It stalled out of its own patrol and hit the ground
        // within seconds of the mission opening, most times Ron started it.
        Check(M27FlightRisk.ShamalCruise > 55f && M27FlightRisk.ShamalLaunchSpeed >= M27FlightRisk.ShamalCruise,
            "The Shamal is given a cruise it can hold, and arrives with more than that on it");
        // It still has to be catchable: the Vestra was chosen over the Duster for this.
        Check(M27FlightRisk.ShamalCruise < 75f, "And it stays inside what the Vestra can run down");

        // The dive was set once at 20 degrees and 90 m/s with the pilot dead: a jet
        // nobody is flying noses over and keeps accelerating, so it reached the water
        // before Ice could get out of his seat.
        Check(M27FlightRisk.DivePitch > -15f && M27FlightRisk.DiveSpeed < 70f,
            "The dive is shallow and capped, so there is a jump in it");
        float fall = (float)(Math.Abs(Math.Sin(M27FlightRisk.DivePitch * Math.PI / 180.0)) * M27FlightRisk.DiveSpeed);
        Check((700f - 80f) / fall > 25f,
            "From the patrol altitude to the bail floor is more than twenty-five seconds of real descent");
        string m27 = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act2", "M27FlightRisk.cs"));
        Check(m27.Contains("HoldTheDive();") && m27.Contains("protected override void OnUpdate()"),
            "And the attitude is held every frame rather than set once and hoped for");

        // ---- The clock is shared, so a mission beat and Guess's ability cannot undo
        // each other, and the slowest holder is the one that applies.
        SlowMotion.Reset();
        Check(!SlowMotion.Slowed && Math.Abs(SlowMotion.Rate - SlowMotion.Normal) < .001f, "Nothing holds the clock to begin with");
        SlowMotion.Hold(M27FlightRisk.TimeOwner, M27FlightRisk.BailTimeScale);
        Check(Math.Abs(SlowMotion.Rate - M27FlightRisk.BailTimeScale) < .001f, "The bail-out slows time");
        SlowMotion.Hold(Bloodlines.Abilities.SlipstreamReflex.TimeOwner, .3f);
        Check(Math.Abs(SlowMotion.Rate - .3f) < .001f, "An ability asking for slower wins over the mission beat");
        SlowMotion.Release(M27FlightRisk.TimeOwner);
        Check(Math.Abs(SlowMotion.Rate - .3f) < .001f, "The beat ending does not speed the ability back up");
        SlowMotion.Release(Bloodlines.Abilities.SlipstreamReflex.TimeOwner);
        Check(!SlowMotion.Slowed && Math.Abs(SlowMotion.Rate - SlowMotion.Normal) < .001f,
            "Time comes back when the last holder lets go");
        SlowMotion.Hold("Test", .01f);
        Check(SlowMotion.Rate >= SlowMotion.Floor, "Nothing is allowed to slow the game past playable");
        SlowMotion.Reset();
        Check(!SlowMotion.Slowed, "Teardown drops every claim");

        // ------------------------------------------------- the deck where it really is
        // Ron played M45 and found the objective marker floating above the surface he was
        // standing on, so the zone under it never registered however he got aboard. Every
        // vertical number for that vessel came out of the archives, but which level a
        // standing point belongs to was a judgment, and that one was wrong. The geometry
        // knows; ask it rather than pick another number.
        Reset();
        const float realDeck = 12.4f;
        World.RaycastHandler = (from, to) => from.X == to.X && from.Y == to.Y && from.Z > realDeck && to.Z < realDeck
            ? new RaycastResult { DidHit = true, HitPosition = new Vector3(from.X, from.Y, realDeck) }
            : new RaycastResult();
        var authored = new Vector3(-1700f, 5325f, 15.5f);
        var corrected = PaletoSite.OnDeck(authored, "test deck");
        Check(Math.Abs(corrected.Z - realDeck) < .01f, "An authored deck point is moved onto the deck that is there");
        Check(Math.Abs(corrected.X - authored.X) < .01f && Math.Abs(corrected.Y - authored.Y) < .01f,
            "And only its height moves — where to stand on the ship is still Ron's to survey");

        // The probe starts just above the authored point, never at the hull top: a point
        // under the superstructure would otherwise be corrected onto its roof.
        Check(PaletoSite.DeckHeadroom < 4f, "It looks down from just above the point, not from over the whole vessel");
        var roofed = MissionSites.SurfaceHeight(authored, authored.Z + PaletoSite.DeckHeadroom, PaletoSite.WaterlineDeck - 1f);
        Check(roofed.HasValue && roofed.Value < authored.Z, "What it finds is below the authored point, which is where he was standing");

        // Nothing solid at all — a chapter opened alone in QA with no vessel streamed —
        // keeps the authored height rather than dropping the mission into the sea.
        World.RaycastHandler = (from, to) => new RaycastResult();
        var kept = PaletoSite.OnDeck(authored, "test deck with nothing under it");
        Check(Math.Abs(kept.Z - authored.Z) < .01f, "With no geometry to ask, the authored height stands");
        World.RaycastHandler = null;

        // And the chapter hangs everything off the probed deck, not the authored key.
        Check(m45.Contains("_deck = PaletoSite.OnDeck("), "M45 probes its deck before it puts anything on it");
        Check(!m45.Contains("ice.Position.DistanceTo2D(At(\"M45.Helipad\"))"),
            "The step-off check measures to the real deck");
        Check(m45.Contains("Marker = () => _deck"), "And Ice is shown where to land, on the surface rather than above it");
        Check(m47.Contains("PaletoSite.OnDeck(At(\"M47.Trigger\")"),
            "M47's charge rail is measured the same way, since it stands on the same vessel");
    }

    /// <summary>A location key's authored height, straight out of the book on disk.</summary>
    static float KeyHeight(string key)
    {
        foreach (var line in File.ReadAllLines(Path.Combine(dataDir, "locations.tsv")))
        {
            var columns = line.Split('	');
            if (columns.Length > 3 && columns[0] == key)
                return float.Parse(columns[3], System.Globalization.CultureInfo.InvariantCulture);
        }
        throw new Exception(key + " is not in the location book.");
    }
}
