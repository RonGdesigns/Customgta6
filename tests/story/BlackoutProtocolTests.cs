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

        // ---- The plant points keep their authored height. Asking the engine for walkable
        // ground beside a tank moves the marker off the tank, which is how M40's kits ended
        // up under the pier.
        foreach (var key in Enumerable.Range(1, 6).Select(i => "M51.Charge" + i).Concat(new[] { "M51.Control" }))
            Check(src.Contains("\"" + key + "\""), key + " is declared a fixed surface and keeps its archive height");
        Check(src.Contains("protected override string[] FixedSurfaces"),
            "and the mission actually overrides the surface list rather than relying on the default");

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
