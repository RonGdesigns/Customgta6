using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public static partial class StoryTests
{
    /// <summary>
    /// Every mission and crew source file, so a contract can be checked across all of
    /// them rather than one at a time.
    /// </summary>
    static IEnumerable<string> ScriptFiles()
    {
        foreach (var folder in new[] { "Missions", "Crew", "Core", "Abilities" })
        {
            string root = Path.Combine(Repo, "src", "Bloodlines", folder);
            if (!Directory.Exists(root)) continue;
            foreach (var file in Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)) yield return file;
        }
    }

    static void SeatingContractChecks()
    {
        // Task.WarpIntoVehicle is queued. Any task issued to the same ped afterwards
        // replaces it, so the board never happens and the ped is left loose: in M26 that
        // was two pilots falling from 220 meters, in M27 a jet nobody flew and an
        // objective marker on the ground, in M02 a police helicopter, in M21 boats that
        // pursued nothing, and in M24 a driverless cruiser. Five separate missions, one
        // mistake. Seat a ped with SetIntoVehicle when anything follows.
        var offenders = new List<string>();
        var warp = new Regex(@"(?<ped>[A-Za-z_][A-Za-z0-9_\.\[\]]*)\s*\.\s*Task\s*\.\s*WarpIntoVehicle", RegexOptions.Compiled);
        foreach (var file in ScriptFiles())
        {
            var lines = File.ReadAllLines(file);
            for (int i = 0; i < lines.Length; i++)
            {
                var match = warp.Match(lines[i]);
                if (!match.Success) continue;
                string ped = match.Groups["ped"].Value;
                // Look at the next few statements for another order to the same ped.
                for (int j = i + 1; j < Math.Min(lines.Length, i + 4); j++)
                {
                    string next = lines[j];
                    if (next.Contains("}")) break;
                    if (Regex.IsMatch(next, Regex.Escape(ped) + @"\s*\.\s*Task\s*\."))
                    {
                        offenders.Add(Path.GetFileName(file) + ":" + (i + 1) + " (" + ped + ")");
                        break;
                    }
                }
            }
        }
        Check(offenders.Count == 0,
            "No ped is asked to board with a queued warp and then immediately given another order" +
            (offenders.Count == 0 ? "" : " [" + string.Join("; ", offenders) + "]"));

        // The seat check that refused M38 reads the model's real capacity, so a mission
        // must not ask for a seat index the vehicle has not got. The stand-in reports real
        // counts for the models the campaign seats people in, and reporting more than the
        // game does is how M38 shipped asking a two-seat Benson for a third seat.
        Check(GTA.Native.Function.ModelSeats.Count > 0 && GTA.Native.Function.ModelSeats["benson"] == 2,
            "The stand-in reports the seat counts the game reports, not more");
        Check(GTA.Native.Function.Seats(new GTA.Model("benson").Hash) == 2 &&
              GTA.Native.Function.Seats(new GTA.Model("granger").Hash) == 4,
            "A model in the table uses its real count and anything else falls back to an ordinary car");
    }
}
