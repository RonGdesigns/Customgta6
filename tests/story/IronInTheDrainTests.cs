using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Campaign;
using GTA;
using GTA.Math;

public static partial class StoryTests
{
    /// <summary>The channel centerline the archives gave: sp1_12_riv_08 to sp1_12_riv_09.</summary>
    static readonly Vector3 ChannelHead = new Vector3(-333.4f, -1709.4f, 0f);
    static readonly Vector3 ChannelUnit = new Vector3(0.771f, -0.637f, 0f);

    /// <summary>How far down the channel a point sits, in meters from riv_08.</summary>
    static float Along(Vector3 at) =>
        (at.X - ChannelHead.X) * ChannelUnit.X + (at.Y - ChannelHead.Y) * ChannelUnit.Y;

    /// <summary>How far across the channel a point sits. The floor is about 19 m either side.</summary>
    static float Across(Vector3 at) =>
        -(at.X - ChannelHead.X) * ChannelUnit.Y + (at.Y - ChannelHead.Y) * ChannelUnit.X;

    /// <summary>A location key's `kind` column, straight out of the shipped book.</summary>
    static string KeyKind(string key)
    {
        foreach (var line in File.ReadAllLines(Path.Combine(dataDir, "locations.tsv")))
        {
            var c = line.Split('\t');
            if (c.Length > 5 && c[0] == key) return c[5];
        }
        throw new Exception(key + " is not in the location book.");
    }

    static void IronInTheDrainChecks()
    {
        string src = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act3", "M56IronInTheDrain.cs"));

        // ---- The channel floor is at -0.40 and the rim is at 28. A point that drifts up to
        // the street has left the arena, and the whole mission with it.
        var channelKeys = new[] { "M56.Start", "M56.IceStart", "M56.GohanStart", "M56.GuessStart",
                                  "M56.Halftrack", "M56.ApcOne", "M56.ApcTwo", "M56.Exit" };
        foreach (var key in channelKeys)
        {
            var at = KeyPoint(key);
            Check(at.Z > -3f && at.Z < 2f, key + " is on the channel floor, not up on the street");
            Check(Math.Abs(Across(at)) < 19f, key + " is inside the channel, within the width the debris marks out");
            // A sunken channel is neither water to swim in nor ground to snap to. The kind
            // keeps MissionSites.Prepare away from it and tells validate_locations that a
            // floor below sea level is the truth about this site rather than a slip.
            Check(KeyKind(key) == "channel", key + " is marked as channel floor, not land or water");
        }

        // ---- Fixed surfaces, because twenty-eight meters up is what the walkable query would
        // answer with. The two overpass gunners are the deliberate exception: they stand at
        // street level, where that query is right.
        Check(src.Contains("protected override string[] FixedSurfaces =>"), "The channel keys are declared fixed surfaces");
        foreach (var key in channelKeys)
            Check(src.Contains("\"" + key + "\""), key + " is named by the mission");
        Check(!src.Contains("\"M56.BridgeGunner1\", ") && src.Contains("Enemy(\"M56.BridgeGunner\""),
            "and the overpass gunners go through ordinary ground preparation at street level");
        foreach (var key in new[] { "M56.BridgeGunner1", "M56.BridgeGunner2" })
            Check(KeyPoint(key).Z > 25f, key + " is up on the bridge deck, not in the channel");

        // ---- One probe for a flat floor, through the shared helper rather than a copied trick.
        Check(src.Contains("MissionSites.OffsetToSurface(authored, FloorHeadroom, FloorSearch"),
            "The channel floor is measured once");
        Check(src.Contains("private Vector3 Floor(string key) => At(key) + new Vector3(0f, 0f, _floorOffset)"),
            "and every channel point moves by the offset it found");
        Check(src.Split(new[] { "OffsetToSurface(" }, StringSplitOptions.None).Length - 1 == 1 &&
              !src.Contains("MissionSites.OnSurface"),
            "There is exactly one probe in the mission");

        // ---- "Two armored APCs coming north under the 4th Street bridge." There is a bridge,
        // sp1_12_bridge_1fc at (-191.7, -1813.8, 29.63), and the Scarabs straddle it, so one of
        // them genuinely drives under it on the way up.
        var bridge = new Vector3(-191.7f, -1813.8f, 29.63f);
        float apcOne = Along(KeyPoint("M56.ApcOne")), apcTwo = Along(KeyPoint("M56.ApcTwo"));
        Check(apcOne < Along(bridge) && Along(bridge) < apcTwo,
            "The bridge sits between the two Scarabs, so the armor comes north under it");
        Check(Along(KeyPoint("M56.Halftrack")) < apcOne,
            "and the crew starts up-channel of both of them, which is the direction the line describes");
        string dialogue = File.ReadAllText(Path.Combine(dataDir, "dialogue.tsv"));
        Check(dialogue.Contains("under the 4th Street bridge"), "The authored line still names a bridge");
        Check(dialogue.Contains("overpass gunners"), "and still warns about the men on it");

        // ---- The Savage comes in above the rim. Below 29.63 it would be flying into the bridge.
        var chopper = KeyPoint("M56.Chopper");
        Check(KeyKind("M56.Chopper") == "air", "The Savage's key is an air spawn");
        Check(chopper.Z > bridge.Z + 10f, "and it comes in above the rim and clear of the bridge");
        Check(src.Contains("AircraftHold.LaunchAirborne(_chopper)"),
            "An aircraft created in the air is launched, not left to fall while its rotors spin up");
        Check(src.Contains("_chopperPilot.SetIntoVehicle(_chopper, VehicleSeat.Driver)") &&
              src.Contains("_chopper.GetPedOnSeat(VehicleSeat.Driver) != _chopperPilot"),
            "and its pilot is seated outright and the seat verified, never warped and then re-tasked");

        // ---- A half-track has the seats the game gives it: three. Asking for RightRear is
        // what failed M38 outright, and BoardBrothers would ask for exactly that.
        Check(src.Contains("Station(CrewSlot.Guess, _halftrack, VehicleSeat.Driver)") &&
              src.Contains("Station(CrewSlot.Ice, _halftrack, VehicleSeat.LeftRear)") &&
              src.Contains("Station(CrewSlot.Gohan, _halftrack, VehicleSeat.Passenger)"),
            "All three ride a three-seat half-track in seats it actually has");
        Check(!src.Contains("VehicleSeat.RightRear") && !src.Contains("BoardBrothers("),
            "and nothing asks a half-track for a fourth seat");

        // ---- The bible named a vehicle the game ships. Checking that against the dump is
        // lint_missions' job, not this suite's: build/vehicles.json is fetched at run time and
        // deliberately never committed, so reading it here passes on a developer's machine and
        // fails in CI, which is exactly what it did.
        Check(M56IronInTheDrain.ApcModel == "scarab", "The Scarab is the model the bible names");
        Check(M56IronInTheDrain.HalftrackModel == "halftrack" && M56IronInTheDrain.ChopperModel == "savage",
            "and the half-track and the Savage are named outright rather than built from strings");

        // ---- Orders on a cadence, never every frame.
        Check(src.Contains("if (Game.GameTime < _orderAt) return;") && M56IronInTheDrain.OrderMs >= 3000,
            "The armor is re-ordered on a slow cadence");
        Check(src.Contains("DrivingDestination ="),
            "and Guess keeps a standing destination so the half-track does not stop when the player takes the mount");

        // ---- The run-out is up-channel. The harbor end is wrecks and pallets: 0.7 m of clear
        // floor at the obvious spot, which is no place to put a half-track.
        Check(Along(KeyPoint("M56.Exit")) < Along(KeyPoint("M56.Start")),
            "The channel is cleared by backing out the way they came in");

        Check(src.Contains("!_engaged || !_armorDown || !_chopperDown"),
            "It cannot pass without both Scarabs and the Savage down");
    }
}
