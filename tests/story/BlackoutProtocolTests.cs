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
    static void BlackoutProtocolChecks()
    {
        string src = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act3", "M51BlackoutProtocol.cs"));

        // ---- Six charge points on things that are really there.
        // The bible asks for six 500kV step-down transformers. Palmer-Taylor has no
        // transformer props at all — its switchyard is baked map geometry — so the six are
        // six real plant units, and the whole mission rests on them being where the
        // archives said and reachable on foot.
        var charges = Enumerable.Range(1, 6).Select(i => KeyPoint("M51.Charge" + i)).ToArray();
        Check(charges.All(p => Math.Abs(p.Z - 23.5f) < 0.5f),
            "All six charge points sit at yard level, so none of them needs a platform nobody has verified");

        var west = charges.Take(3).ToArray();
        var east = charges.Skip(3).ToArray();
        float WorstGap(Vector3[] bank) => Enumerable.Range(0, bank.Length)
            .SelectMany(a => Enumerable.Range(a + 1, bank.Length - a - 1).Select(b => bank[a].DistanceTo2D(bank[b])))
            .Min();
        Check(WorstGap(west) > M51BlackoutProtocol.LimpetRadius * 2f &&
              WorstGap(east) > M51BlackoutProtocol.LimpetRadius * 2f,
            "No two charge points overlap at the hold radius, so each is a separate job");
        var westMiddle = new Vector3(west.Average(p => p.X), west.Average(p => p.Y), 0f);
        var eastMiddle = new Vector3(east.Average(p => p.X), east.Average(p => p.Y), 0f);
        Check(westMiddle.DistanceTo2D(eastMiddle) > 80f,
            "The two banks are far enough apart to be two sections rather than one");

        // ---- A limpet point is where Ice STANDS, not where the charge ends up, and a
        // placed prop's origin is not a floor. The first version of this mission held all
        // seven at their archive heights and Ron could not reach them: markers up in the air
        // on the side of a tank, with no way up. Ground preparation owns them now.
        Check(src.Contains("FixedSurfaces => new string[0]"),
            "Nothing in M51 is held at a prop's origin height");
        Check(WorstGap(west) > 12f && WorstGap(east) > 5f,
            "and the units are far enough apart that a stride of correction cannot make two ambiguous");

        // ---- Ron's decision: armed, not fired.
        Check(!src.Contains("WorldLights."),
            "M51 does not touch the city's lights: the outage was held for the downtown towers");
        Check(src.Contains("SetCargo(ReadyCargo"),
            "It records that Palmer-Taylor is wired instead, so a later mission can fire it");
        Check(M51BlackoutProtocol.ReadyCargo.Length > 0 && src.Contains("_armed = true;"),
            "and the sequence is armed");

        // The authored line counts the charges down and calls the blackout. Firing it here
        // would tell the player the city had gone dark while every light stayed on.
        Check(!src.Contains(".AfterCues(\"M51_S1_03_GOHAN\")") && src.Contains("M51_S1_03_GOHAN is deliberately not fired"),
            "The detonation line is withheld, and the reason is written down where the next reader will find it");
        string overlay = File.ReadAllText(Path.Combine(dataDir, "mission_gameplay.tsv"));
        Check(overlay.Contains("M51\t") && overlay.Contains("left ARMED"),
            "The divergence from the bible is recorded in the gameplay overlay, not by editing the extraction");

        // ---- Three jobs, one stage, so switching is the player's to spend. This is what
        // Ron asked for in M35 and it is the reason they are not three stages.
        Check(src.Contains("new MissionStage(\"Wire both banks\", west, east, interlocks)"),
            "The two banks and the interlocks are parallel in one stage");
        Check(src.Contains("RequiredCharacter = CrewSlot.Ice") &&
              src.Contains("RequiredCharacter = CrewSlot.Guess") &&
              src.Contains("RequiredCharacter = CrewSlot.Gohan"),
            "with a named brother on each, so the dispatcher only asks for a switch when the one he holds is finished");

        // ---- Nobody is left in the switchyard. The objective only ever asked whether they
        // were already in the car.
        Check(src.Contains("_boarding.Update(Ctx.Crew, CrewCar"),
            "The brothers are ordered into the car rather than waited on");

        // ---- It cannot pass half-done.
        Check(src.Contains("_west < 3 || _east < 3 || !_lockedOut || !_armed"),
            "Passing requires six limpets, the lockout and the arming, checked rather than assumed");
    }

    static void JudicialStrikeChecks()
    {
        string src = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act3", "M52JudicialStrike.cs"));

        // ---- The bible's firing position does not exist. It puts Ice on the Union
        // Depository roof "across the plaza" from City Hall; those are 716 m apart, which
        // is not a plaza and not a shot. This roof is 46 m out and 15 above it, and it has
        // a ladder Rockstar placed for a mission.
        var roost = KeyPoint("M52.Roost");
        var hall = KeyPoint("M52.Harrison");
        var clear = KeyPoint("M52.Walk");
        Check(roost.DistanceTo2D(hall) < 200f, "The roost is a shot away from the steps, not most of a kilometer");
        Check(roost.Z - hall.Z > 10f, "and it is high enough above the plaza to be a rooftop angle");
        Check(clear.DistanceTo2D(roost) > 30f && clear.DistanceTo2D(roost) < 120f,
            "The clear-shot mark is a rifle range from the roost");
        Check(clear.DistanceTo2D(hall) > 25f,
            "and far enough from the steps that walking to it takes him out of his escort");

        // ---- The roost is a roof; ground preparation would put the marker on the street.
        Check(src.Contains("FixedSurfaces => new[] { \"M52.Roost\" }"),
            "The roof keeps its authored height");

        // ---- M41's machinery, not a second implementation of it. Both failure paths it
        // was given after a live report have to be here.
        Check(src.Contains("Harrison was shot before Gohan confirmed him"),
            "Shooting the wrong man in a suit fails the mission");
        Check(src.Contains("Harrison was shot standing in his escort"),
            "and so does firing into the detail before he walks clear");
        Check(src.Contains("Harrison reached cover"),
            "and he escapes if he is left alive after the alarm");
        Check(src.Contains("WalkPatienceMs"),
            "An obstructed walk is reported as a placement problem rather than left hanging");

        // ---- Two seats is all this needs: only Ice and Guess are at the plaza, which is
        // what the synopsis says. An earlier planning pass read the superbike as a conflict
        // with a three-man extraction; there is no third man there.
        Check(M52JudicialStrike.BikeModel == "hakuchou2",
            "The extraction is the modified superbike the bible asks for");
        Check(src.Contains("Station(CrewSlot.Guess, _bike, VehicleSeat.Driver)"),
            "Guess is already on it, because the authored line is \"hop on\"");
        Check(src.Contains("VehicleSeat.Passenger"), "and Ice rides pillion");

        // ---- The getaway is a state, not a coordinate.
        Check(src.Contains("new LoseWantedObjective("),
            "It ends when the response loses them rather than at an invented map point");

        // ---- The detail is owned by the awareness model, not tasked to fight at spawn.
        Check(!src.Contains("FightAgainstHatedTargets"),
            "Harrison's escort is not sent looking for somebody the moment it is created");

        Check(src.Contains("SetEvidence(\"harrisonRemoved\""),
            "and the result is recorded for the missions that follow");
    }
    static void RedactedVaultChecks()
    {
        string src = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act3", "M50TheRedactedVault.cs"));

        // ---- The splice is on a street, not in a field. There is no archive in Rockford
        // Hills, but the synopsis is already a conduit tap, and the cabinet chosen is the one
        // standing in the most built-up part of the zone.
        var tap = KeyPoint("M50.Conduit");
        var van = KeyPoint("M50.Van");
        Check(tap.DistanceTo2D(van) > 10f && tap.DistanceTo2D(van) < 60f,
            "The van waits near the cabinet but not on top of it");
        // The conduit key is where Gohan stands to work the cabinet, and a prop origin is
        // not a floor — the same mistake that put M51's limpet markers in the air.
        Check(src.Contains("FixedSurfaces => new string[0]"),
            "The conduit point is placed on the pavement rather than held at the cabinet's origin");

        // ---- Contained, not killed. Private security outside a residential block.
        Check(src.Contains("new SubdueTargetsObjective(") && src.Contains("new NonlethalGuards()"),
            "The security is subdued rather than shot");
        Check(!src.Contains("KillTargetsObjective"), "and there is no objective that asks for bodies");
        Check(src.Contains("_contained?.Dispose();"), "and the nonlethal state is released on teardown");

        // ---- The result is bounded, and has to stay bounded.
        Check(src.Contains("local and physical copies") || src.Contains("Local copies still exist"),
            "The feed is invalidated and nothing is erased");
        Check(!src.Contains("WantedLevel = 0") && !src.Contains("LoseWanted"),
            "and nothing about it clears a wanted level");

        // Two authored lines describe a vault sub-level with turrets and a left corridor.
        // That interior does not exist; narrating it would be worse than silence.
        Check(!src.Contains(".AfterCues(\"M50_S1_01_GOHAN\")") && src.Contains("M50_S1_01_GOHAN and M50_S1_02_ICE are not fired"),
            "The interior lines are withheld, with the reason written down");
        Check(src.Contains("M50_S1_03_GOHAN"), "and the one that states the bounded result is fired");
        string overlay = File.ReadAllText(Path.Combine(dataDir, "mission_gameplay.tsv"));
        Check(overlay.Contains("M50	") && overlay.Contains("NONLETHALLY"),
            "and the adaptation is recorded in the overlay rather than by editing the extraction");
    }

    static void ReturnToTheConcreteChecks()
    {
        string src = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act3", "M49ReturnToTheConcrete.cs"));

        // ---- The ram is gone. A barricade that only yields to a 90-mph collision is one
        // the player cannot fail at honestly, and CAMPAIGN-REMAINDER refuses it outright.
        Check(src.Contains("The ram is deliberately gone"),
            "The checkpoint opens because the defense lost, not because the car is harder than concrete");
        Check(src.Contains("if (i == 1) continue;"),
            "The seam is a gap left in the concrete rather than a hole punched through it");

        // ---- Roads are baked terrain, so the lane cannot be authored. It is resolved from
        // a vehicle node and everything else is an offset from it: one seed can be wrong,
        // where six separate authored points can each be wrong on their own.
        Check(src.Contains("GameUtils.NearestRoadNode(seed, LaneSearch"),
            "The lane comes from a road node at runtime");
        Check(src.Contains("_forward = new Vector3(") && src.Contains("_right = new Vector3(_forward.Y, -_forward.X, 0f);"),
            "and the barricade is laid out along that lane's own heading");
        Check(src.Contains("Ctx.Doctor?.Warn(\"placement\", \"M49.Checkpoint\"") && src.Contains("_lane = seed;"),
            "A missing node is reported and the seed used, rather than refusing the mission");

        // ---- Ice comes down off the shoulder. This is the M47 and M48 bug.
        Check(src.Contains("_boarding.Update(Ctx.Crew, CrewCar"),
            "Ice is ordered into the Granger rather than left at his firing position");

        Check(src.Contains("!_towersDown || !_jammed || !_through"),
            "It cannot pass without the towers, the jam and the run through the seam");
    }
    static void PresentationRepairChecks()
    {
        // ---- A dot over a corpse. Ron saw this in mission after mission: a blip added with
        // AddBlip is its own entity and outlives the ped, so the radar kept telling him there
        // was a fight where there was not.
        Reset();
        var blips = new TargetBlips();
        var alive = new Ped { Position = new Vector3(5f, 0f, 0f) };
        var doomed = new Ped { Position = new Vector3(9f, 0f, 0f) };
        blips.Attach(alive, BlipColor.Red, "Armed guard");
        blips.Attach(doomed, BlipColor.Red, "Armed guard");
        Check(blips.Count == 2, "Two hostiles, two dots");
        blips.Update();
        Check(blips.Count == 2, "and they stay while both are on their feet");
        doomed.IsDead = true;
        blips.Update();
        Check(blips.Count == 1, "The dot goes the frame the man does");
        blips.Dispose();
        Check(blips.Count == 0, "and teardown takes the rest");

        // Every mission that spawns hostiles goes through the owner rather than adding its
        // own blips and forgetting them.
        string prep = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act2", "PreparationOperation.cs"));
        Check(prep.Contains("Blips.Attach(ped, BlipColor.Red") && prep.Contains("Blips.Update();"),
            "The shared guard spawner owns its dots and sweeps them every frame");
        foreach (var file in ScriptFiles())
        {
            string text = File.ReadAllText(file);
            Check(!text.Contains("Track(ped.AddBlip())") && !text.Contains("Track(Harrison.AddBlip())"),
                Path.GetFileName(file) + " does not add a ped blip it will never remove");
        }

        // ---- A brother's markers are his own. M51 drew six limpet points and an interlock
        // cabinet at once, so there was no telling which one was being asked for.
        string composed = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "ComposedMission.cs"));
        Check(composed.Contains("ObjectiveMarkers.Suppressed = !mine;") &&
              composed.Contains("finally { ObjectiveMarkers.Suppressed = false; }"),
            "Only the active brother's objective draws, and the flag is always cleared");
        string utils = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "GameUtils.cs"));
        Check(utils.Contains("if (ObjectiveMarkers.Suppressed) return;"),
            "and the suppression reaches the cylinder as well as the blip");

        // ---- DLC map registration has one owner. It used to happen as a side effect of the
        // bunker drawing a blip on the first frame of every session; moving that out fixed a
        // loading screen and broke the Paleto yacht, which is Cayo Perico map data.
        string dlc = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "DlcMaps.cs"));
        Check(dlc.Contains("0x0888C3502DBBEEF5UL"),
            "The registration native lives in one place");
        foreach (var file in Directory.GetFiles(Path.Combine(Repo, "src", "Bloodlines"), "*.cs", SearchOption.AllDirectories))
        {
            if (Path.GetFileName(file) == "DlcMaps.cs") continue;
            string text = File.ReadAllText(file);
            Check(!text.Contains("Hash.REQUEST_IPL"),
                Path.GetFileName(file) + " asks for maps through DlcMaps, so the archives are registered first");
            Check(!text.Contains("0x0888C3502DBBEEF5"),
                Path.GetFileName(file) + " does not register the DLC maps behind anyone's back");
        }
    }
    static void PillboxRedoubtChecks()
    {
        string src = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act3", "M54ThePillboxRedoubt.cs"));

        // ---- The pad and the three work positions are two hundred meters of air above the
        // street. Ground preparation would find the sidewalk under them and put the crew
        // there, which is exactly what it did to M40's hull kits under a pier.
        foreach (var key in new[] { "M54.Pad", "M54.RoostNorth", "M54.RoostSouth", "M54.Antenna" })
            Check(src.Contains("\"" + key + "\""), key + " is named by the mission");
        Check(src.Contains("protected override string[] FixedSurfaces =>") &&
              src.Contains("\"M54.Pad\", \"M54.RoostNorth\", \"M54.RoostSouth\", \"M54.Antenna\""),
            "and every one of them is declared a fixed surface rather than ground-snapped");

        // ---- The heights come off the geometry, not off a prop origin. That is the mistake
        // that left M51's marker in the air and M52's roost off the side of a building.
        Check(src.Contains("MissionSites.OnSurface(At(key), RoofHeadroom, RoofFloor"),
            "Each roof position is probed down onto the slab under it");
        int setup = src.IndexOf("protected override bool Setup()", StringComparison.Ordinal);
        int seated = src.IndexOf("private bool Seated(", StringComparison.Ordinal);
        Check(setup > 0 && seated > setup && src.IndexOf("OnSurface", setup, seated - setup, StringComparison.Ordinal) < 0,
            "and the probe runs on arrival, not in Setup where that collision is not streamed");

        // ---- A twelve-meter deck, whose outline is a ring of prop_wall_light_03a at 209.15
        // spanning x -150.47..-138.74 and y -599.24..-587.54. Every authored point sits
        // inside it, because outside it is a two-hundred-meter drop.
        foreach (var key in new[] { "M54.Pad", "M54.RoostNorth", "M54.RoostSouth", "M54.Antenna" })
        {
            var at = KeyPoint(key);
            Check(at.X > -150.5f && at.X < -138.7f && at.Y > -599.3f && at.Y < -587.5f,
                key + " is inside the roof deck the light ring outlines");
            Check(at.Z > 200f && at.Z < 212f, key + " is at the deck's own height, not the street's");
        }

        // ---- Three separate markers, not one pile. Ron could not tell M52's objectives
        // apart because they all drew at once; these are far enough apart to read.
        var pad = KeyPoint("M54.Pad");
        var north = KeyPoint("M54.RoostNorth");
        var south = KeyPoint("M54.RoostSouth");
        var antenna = KeyPoint("M54.Antenna");
        foreach (var pair in new[] { Tuple.Create(north, south), Tuple.Create(north, antenna), Tuple.Create(south, antenna) })
            Check(pair.Item1.DistanceTo(pair.Item2) > 6f, "the work positions are distinguishable on the roof");
        Check(pad.DistanceTo(north) > 4f && pad.DistanceTo(south) > 4f && pad.DistanceTo(antenna) > 4f,
            "and none of them is under the rotor on the pad itself");

        // ---- The roost positions are read every frame. MultiHoldObjective copies its site
        // list in its constructor, and BuildStages runs long before the roof is probed, so it
        // would have captured an empty list and completed the instant the stage opened.
        Check(!src.Contains("new MultiHoldObjective"),
            "The roosts are not a site list captured before the sites exist");
        Check(src.Contains("() => Roost(0)") && src.Contains("private Vector3 Roost(int index)"),
            "and each roost reads its position when it is drawn, never at construction");

        // ---- The helicopter is created on the ground, so it has nothing to fall out of
        // while its rotors spin up. LaunchAirborne is for a spawn in the air, and using it
        // here would lift a parked aircraft off a yard somebody is walking across.
        Check(!src.Contains("AircraftHold.LaunchAirborne("),
            "The Annihilator is parked, not created in the air");
        Check(src.Contains("_hold.Update(Ctx.Crew, CrewSlot.Guess"),
            "and switching away from Guess in the air leaves it flying rather than falling");

        // ---- No interior. This version claims no MLO and opens no map.
        Check(!src.Contains("Ctx.Interior") && !src.Contains("REQUEST_IPL") && !src.Contains("DlcMaps."),
            "The roof needs no interior service and no map request");

        // ---- The lift-off point is real open ground in the crew's own yard.
        var lift = KeyPoint("M54.Lift");
        Check(lift.Z < 40f, "The Annihilator waits on the ground, not on a roof");
        Check(lift.DistanceTo2D(KeyPoint("M54.Start")) < 25f,
            "and the crew starts within walking distance of it");

        Check(src.Contains("!_landed || !_fortified"),
            "It cannot pass without reaching the roof and fortifying the nest");
    }

    /// <summary>A location key's authored position, straight out of the shipped book.</summary>
    static Vector3 KeyPoint(string key)
    {
        foreach (var line in File.ReadAllLines(Path.Combine(dataDir, "locations.tsv")))
        {
            var c = line.Split('\t');
            if (c.Length > 3 && c[0] == key)
                return new Vector3(
                    float.Parse(c[1], CultureInfo.InvariantCulture),
                    float.Parse(c[2], CultureInfo.InvariantCulture),
                    float.Parse(c[3], CultureInfo.InvariantCulture));
        }
        throw new Exception(key + " is not in the location book.");
    }
}
