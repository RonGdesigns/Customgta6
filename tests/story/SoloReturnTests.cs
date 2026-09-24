using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public static partial class StoryTests
{
    /// <summary>
    /// Ron, September 23: after SM01 as Ice, and SM02 as Gohan, he was locked to that brother.
    /// Every solo mission deploys one man alone and nothing ever put the other two back.
    /// The roster is not compiled in either harness, so these read the source; each one
    /// matches a call, never a comment, and ignores line endings.
    /// </summary>
    static void SoloReturnChecks()
    {
        string Flat(string path) => Regex.Replace(Source(path), @"\s+", " ");

        string roster = Flat("src/Bloodlines/Crew/CrewRoster.cs");
        int at = roster.IndexOf("public bool EndSolo()", StringComparison.Ordinal);
        int next = at < 0 ? -1 : roster.IndexOf("public bool Deploy(", at, StringComparison.Ordinal);
        string endSolo = at < 0 || next < 0 ? "" : roster.Substring(at, next - at);
        Check(endSolo.Contains("if (!IsSolo || !IsDeployed) return false;") &&
              endSolo.Contains("if (protagonist.Slot == ActiveSlot) continue;") &&
              endSolo.Contains("World.CreatePed(model, at, lead.Heading)") &&
              endSolo.Contains("ConfigurePed(ped, protagonist);") && endSolo.Contains("_peds[protagonist.Slot] = ped;") &&
              endSolo.Contains("IsSolo = false;") && endSolo.Contains("AssignCompanionAI();"),
            "Ending a solo job rebuilds the two missing brothers and leaves the one who did it alone");

        string host = Flat("src/Bloodlines/BloodlinesMain.cs");
        int tick = host.IndexOf("private void OnTick(", StringComparison.Ordinal);
        int sceneReturn = host.IndexOf("if (_cutscenes.IsActive) {", tick, StringComparison.Ordinal);
        int step = host.IndexOf("Step(\"end solo job\"", tick, StringComparison.Ordinal);
        Check(step > sceneReturn && sceneReturn > tick,
            "The crew comes back only after the aftermath scene, which the tick returns early for");
        string call = step < 0 ? "" : host.Substring(step, Math.Min(400, host.Length - step));
        Check(call.Contains("_crew.IsSolo") && call.Contains("!_missions.IsRunning") &&
              call.Contains("_state.IsComplete(\"M01\")") && call.Contains("_crew.EndSolo()") && call.Contains("_memory.Restore(_crew)"),
            "It happens outside a mission, after the dockyard reunion, and restores their free-roam health and ammo");

        var solos = Directory.GetFiles(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Solo"), "SM0*.cs");
        Check(solos.Length == 9 && solos.All(f => File.ReadAllText(f).Contains(".DeploySolo(")),
            "All nine solo missions deploy alone, so all nine end with the crew back together");
    }
}
