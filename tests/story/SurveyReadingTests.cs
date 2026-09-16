using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using GTA.Math;

public static partial class StoryTests
{
    /// <summary>
    /// What the survey sees at a point, judged against the kind of key it is.
    ///
    /// Every branch here is a bug this campaign shipped and found in game weeks later, so
    /// these checks are written as those bugs rather than as tolerances: a man inside the
    /// hull, hull kits under a pier, a laptop inside the crate it was stacked on, a tunnel
    /// searched below sea level, an interior key where no interior loads.
    /// </summary>
    static void SurveyReadingChecks()
    {
        // ---- The distinction the first version of this got wrong, and the reason it is
        // first: a probe that never ran is not a probe that found nothing. Conflating them
        // calls every point in a harness broken, which is exactly what happened.
        var unmeasured = new SurveyReading { Key = "M01.Dock", Kind = "land", Point = new Vector3(0f, 0f, 12f) }.Judge();
        Check(unmeasured.Fits, "A point nothing measured is not a point that failed");
        Check(unmeasured.Hud().Contains("not measured"), "and it says so rather than implying a clean bill");
        var probed = new SurveyReading
        { Key = "M01.Dock", Kind = "land", Point = new Vector3(0f, 0f, 12f), SurfaceProbed = true }.Judge();
        Check(!probed.Fits && probed.Complaint.Contains("Nothing solid"),
            "A probe that looked and found nothing is a fault");

        // ---- land. A point floating over the ground is M43's laptop; a point with a
        // deckhead on it is M45's man in the hull.
        var ground = new SurveyReading
        { Key = "M60.Wave1", Kind = "land", Point = new Vector3(110f, -1830f, 21f), SurfaceProbed = true, Surface = 20.8f,
          HeadroomProbed = true, Headroom = float.MaxValue, Room = 18f, Zone = "DAVIS" };
        Check(ground.Judge().Fits, "A land key standing on the street checks out");
        Check(ground.Hud().Contains("DAVIS"), "and the readout names the zone, which is machine-read offline");
        ground.Point = new Vector3(110f, -1830f, 31f);
        Check(!ground.Judge().Fits && ground.Complaint.Contains("floats"),
            "A land key ten meters over the ground does not");
        ground.Point = new Vector3(110f, -1830f, 21f);
        ground.Headroom = 1.2f;
        Check(!ground.Judge().Fits && ground.Complaint.Contains("deckhead"),
            "and neither does one nobody can stand up at, which is the man inside the hull");
        ground.Headroom = 2.6f;
        Check(ground.Judge().Fits,
            "Cover on its own is fine: plenty of these are in a warehouse or under a bridge");
        ground.Interior = true;
        Check(!ground.Judge().Fits && ground.Complaint.Contains("interior"),
            "A land key with an interior loaded at it is a room, not a street");

        // ---- interior. M55 was filed unbuildable twice because a man dropped where no MLO
        // loads is a man in open sky at that height.
        var room = new SurveyReading
        { Key = "M55.Suite", Kind = "interior", Point = new Vector3(-13f, -593f, 93f), SurfaceProbed = true,
          Surface = 93f, Interior = false };
        Check(!room.Judge().Fits && room.Complaint.Contains("open sky"),
            "An interior key where no interior loads is refused by name");
        room.Interior = true;
        Check(room.Judge().Fits, "and accepted when one does");
        room.Interior = null;
        Check(room.Judge().Fits, "An interior nothing asked about is not a complaint");

        // ---- water, air, channel, underground: the kinds the location book carries that a
        // downward probe alone says nothing useful about.
        var afloat = new SurveyReading
        { Key = "M05.Dinghy", Kind = "water", Point = new Vector3(2524f, -1228f, 1.2f), Water = 0.9f };
        Check(afloat.Judge().Fits, "A water key at the waterline checks out");
        afloat.Point = new Vector3(2524f, -1228f, 26f);
        Check(!afloat.Judge().Fits && afloat.Complaint.Contains("above the waterline"),
            "and one twenty-five meters over it does not");
        afloat.Water = null;
        Check(!afloat.Judge().Fits, "nor does one in a dry column");

        var hold = new SurveyReading
        { Key = "M45.Approach", Kind = "air", Point = new Vector3(-1600f, 5300f, 60f), SurfaceProbed = true, Surface = 0f };
        Check(hold.Judge().Fits, "An air key sixty meters up checks out");
        hold.Surface = 58f;
        Check(!hold.Judge().Fits && hold.Complaint.Contains("not an air spawn"),
            "and one two meters off a deck does not - a helicopter created there has nowhere to go");

        var drain = new SurveyReading
        { Key = "M56.Channel", Kind = "channel", Point = new Vector3(-400f, -1800f, -0.4f), SurfaceProbed = true, Surface = -0.5f };
        Check(drain.Judge().Fits, "A channel key on the floor of the drain checks out below sea level");

        var metro = new SurveyReading
        { Key = "M53.Carriage", Kind = "underground", Point = new Vector3(-497f, -673f, 13.6f), SurfaceProbed = true,
          Surface = 13.0f, HeadroomProbed = true, Headroom = 4f };
        Check(metro.Judge().Fits, "An underground key with a tunnel roof over it checks out");
        metro.Headroom = float.MaxValue;
        Check(!metro.Judge().Fits && metro.Complaint.Contains("open sky"),
            "and open sky over it means the point came up through the ceiling");

        // ---- The report and the readout.
        Check(metro.Line().StartsWith("CHECK"), "A spot needing a look is marked in the report");
        Check(drain.Line().StartsWith("ok"), "and one that does not is not");
        var far = new SurveyReading
        { Key = "M48.Cordon", Kind = "land", Point = Vector3.Zero, SurfaceProbed = true, Surface = 0f,
          NearestKey = "Technical", NearestMeters = 18f };
        Check(far.Hud().Contains("Technical") && far.Hud().Contains("18"),
            "The readout names the nearest key of the same mission, which is the shape an individual coordinate cannot show");

        // ---- Where it is used. The survey drew a key, a district and a target height and
        // said nothing about whether a man could stand there.
        string survey = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "SurveyMode.cs"));
        Check(survey.Contains("var reading = Reading(location, Camera.IsFlying ? Camera.Position : location.Position);"),
            "The survey HUD reads out the spot the camera is looking at");
        Check(survey.Contains("var check = Read(location, taken);") && survey.Contains("if (!check.Fits && !Confirming(location.Key))"),
            "and a capture that contradicts its own key waits for a deliberate second press");
        string check = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "SurveyMode.Check.cs"));
        Check(check.Contains("TeleportToCurrent();") && !check.Contains("Script.Wait"),
            "The sweep drives the ordinary teleport rather than a traversal of its own");
        Check(check.Contains("if (!reading.Fits)") && check.Contains("Fly to it and place it instead."),
            "Accepting in place is refused for a spot that has not checked out");
        string host = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "BloodlinesMain.cs"));
        Check(host.Contains("SurveyMode.HeadroomProbe = (at, height) => MissionSites.OpenAbove(at, height);") &&
              host.Contains("SurveyMode.InteriorProbe = at => MissionSites.InteriorAt(at);"),
            "and the host injects the probes beside the two that were already there");
    }
}
