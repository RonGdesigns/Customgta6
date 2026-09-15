using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using GTA;
using GTA.Math;

public static partial class StoryTests
{
    /// <summary>A location key straight out of the shipped book on disk.</summary>
    static Vector3 AuthoredKey(string key)
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

    static void SurveyGuardChecks()
    {
        // Ron surveyed SM06 by walking to the start and pressing capture four times
        // without moving. Two of those keys belong kilometers away, so the mission's
        // canyon bend and its airfield delivery both landed outside a clothes shop and
        // the whole job collapsed into a thirty-meter circle. Nothing measured anything,
        // so the only symptom was "the surveyor is not saving".
        var standing = new Vector3(-1133f, 2666f, 18f);
        var captures = new[]
        {
            new { Key = "SM06.Approach", At = new Vector3(-1101.38f, 2678.64f, 19.10f), WrongPlace = false },
            new { Key = "SM06.Truck",    At = new Vector3(-1128.16f, 2677.00f, 18.35f), WrongPlace = false },
            new { Key = "SM06.Bend",     At = new Vector3(-1130.69f, 2649.31f, 17.10f), WrongPlace = true },
            new { Key = "SM06.Delivery", At = new Vector3(-1144.12f, 2662.14f, 18.04f), WrongPlace = true },
        };
        foreach (var capture in captures)
        {
            var location = new MissionLocation { Key = capture.Key, Authored = AuthoredKey(capture.Key) };
            bool refused = LocationBook.Displaced(location, capture.At);
            Check(refused == capture.WrongPlace,
                capture.Key + " captured " + (int)capture.At.DistanceTo2D(location.Authored) + " m from where it belongs is " +
                (capture.WrongPlace ? "refused" : "kept as a correction"));
        }

        // The threshold has to clear every real correction this project has made and
        // still catch a wrong key. The largest genuine one moved a point 36 meters.
        Check(LocationBook.MaxCorrectionMeters > 100f && LocationBook.MaxCorrectionMeters < 600f,
            "The line between a correction and a different place is drawn well clear of both");

        // A key with no authored value recorded cannot be judged, and is not refused on
        // a guess: a location built from an anchor offset carries its own authored copy.
        Check(!LocationBook.Displaced(null, standing), "There is nothing to compare an absent location against");

        // ---- The delivery point itself. The authored one sat in 1.8 meters of clearance
        // among paint cans and box piles; a tractor and a tanker are about twenty meters
        // of vehicle. Ron said it was wrong before the placement fix, and he was right.
        var delivery = AuthoredKey("SM06.Delivery");
        var runway = AuthoredKey("M26.RunwayStart");
        Check(delivery.DistanceTo2D(runway) < 60f,
            "The fuel bay is on McKenzie's apron, beside the runway the other jobs use");
        Check(delivery.Z > 35f && delivery.Z < 45f, "At the airfield's own height");
        Check(Math.Abs(delivery.X - 2126f) > 50f,
            "And not on the old point, which was wedged between a generator and a stack of boxes");

        // ---- Clearance is measured, because a survey is captured on foot and a man fits
        // where a rig does not. Ron's truck capture had 2.3 meters: a shop wall on one
        // side and a row of bollards on the other.
        Check(MissionSites.TightRoomMeters >= 4f && MissionSites.TightRoomMeters <= 8f,
            "A spot too tight for any vehicle the campaign spawns is called out");
        Check(MissionSites.RoomProbeMeters > MissionSites.TightRoomMeters,
            "The probe reaches past the point where cramped stops mattering");
        Reset();
        // A wall four meters out in one direction is what the probe is for.
        World.RaycastHandler = (from, to) =>
        {
            var along = new Vector3(to.X - from.X, to.Y - from.Y, 0f);
            if (along.X <= 0f) return new RaycastResult();
            return new RaycastResult { DidHit = true, HitPosition = new Vector3(from.X + 4f, from.Y, from.Z) };
        };
        float room = MissionSites.FreeRadius(new Vector3(0f, 0f, 0f), MissionSites.RoomProbeMeters);
        Check(room > 3.5f && room < 4.5f, "The clearance reported is the distance to the nearest thing, not the average");
        Check(room < MissionSites.TightRoomMeters, "And four meters of room is correctly called too tight for a vehicle");
        World.RaycastHandler = (from, to) => new RaycastResult();
        Check(Math.Abs(MissionSites.FreeRadius(Vector3.Zero, MissionSites.RoomProbeMeters) - MissionSites.RoomProbeMeters) < .01f,
            "Open ground reports the full reach of the probe rather than zero");
        World.RaycastHandler = null;

        // ---- And the capture path actually consults both, rather than recording whatever
        // the player was standing on.
        string survey = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "SurveyMode.cs"));
        Check(survey.Contains("LocationBook.Displaced(location, player.Position)"),
            "A capture is checked against where its key belongs before it is written");
        Check(survey.Contains("float room = Clearance(player.Position);"),
            "And the room at that spot is measured before the capture is written");
        string entry = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "BloodlinesMain.cs"));
        Check(entry.Contains("SurveyMode.ClearanceProbe = at => MissionSites.FreeRadius(at, MissionSites.RoomProbeMeters);") &&
              entry.Contains("SurveyMode.TightRoom = MissionSites.TightRoomMeters;"),
            "The real probe is wired in at startup, so the measurement is not quietly skipped in game");
        Check(survey.Contains("Confirming(location.Key)"),
            "A deliberate relocation is still possible, on a second press");
    }
}
