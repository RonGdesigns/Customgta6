using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

public static partial class StoryTests
{
    /// <summary>
    /// The host's constructor cannot be run here - it is a GTA.Script - and that is exactly
    /// how a null dereference in it shipped: `_shops.OwnedRecord = ...` was written forty
    /// lines above `_shops = new ShopService(...)`, a text check confirmed the line existed,
    /// and in game the main script threw on instantiation. Only DevTools started. Ron loaded
    /// a game with no campaign, no debug menu and no way to tell why.
    ///
    /// So the order is checked as text: inside the constructor, no private field may be
    /// dereferenced on a line before the line that constructs it.
    /// </summary>
    static void HostConstructorChecks()
    {
        string host = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "BloodlinesMain.cs"));
        int start = host.IndexOf("public BloodlinesMain()", StringComparison.Ordinal);
        Check(start > 0, "The host constructor is where it has always been");
        // The constructor body ends at the first method declared after it.
        int end = host.IndexOf("\n        private void ", start, StringComparison.Ordinal);
        if (end < 0) end = host.IndexOf("\n        public void ", start, StringComparison.Ordinal);
        Check(end > start, "and it has an end");
        string[] lines = host.Substring(start, end - start).Split('\n');

        // Where each field is constructed: the first line that assigns it something.
        var built = new Dictionary<string, int>();
        for (int i = 0; i < lines.Length; i++)
            foreach (Match m in Regex.Matches(lines[i], @"^\s*(_[A-Za-z0-9]+)\s*=\s*"))
                if (!built.ContainsKey(m.Groups[1].Value)) built[m.Groups[1].Value] = i;

        // Every dereference, and the line it is on. A lambda body is skipped: it runs later,
        // when the field is there - the survey probes and the stage lookup are exactly that.
        var early = new List<string>();
        int lambdaDepth = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = Regex.Replace(lines[i], @"//.*$", "");   // a comment is not code
            if (lambdaDepth > 0)
            {
                // Still inside a lambda that opened on an earlier line. Its body runs later,
                // when the field is there; count parentheses until it closes.
                lambdaDepth += line.Count(ch => ch == '(') - line.Count(ch => ch == ')');
                continue;
            }
            int arrow = line.IndexOf("=>", StringComparison.Ordinal);
            if (arrow >= 0)
            {
                string body = line.Substring(arrow);
                lambdaDepth = Math.Max(0, body.Count(ch => ch == '(') - body.Count(ch => ch == ')'));
                line = line.Substring(0, arrow);                    // the lambda's body is deferred
            }
            foreach (Match m in Regex.Matches(line, @"(?<![A-Za-z0-9_.])(_[A-Za-z0-9]+)\."))
            {
                string field = m.Groups[1].Value;
                int at;
                if (built.TryGetValue(field, out at) && i < at)
                    early.Add(field + " is used on constructor line " + (i + 1) + " and built on line " + (at + 1));
            }
        }
        Check(early.Count == 0, "No field in the host constructor is dereferenced before it is built" +
            (early.Count == 0 ? "" : ": " + string.Join("; ", early.ToArray())));

        Check(built.ContainsKey("_shops") && built.ContainsKey("_garages"), "The shop and the garage are built in the constructor");
        int owned = Array.FindIndex(lines, l => l.Contains("_shops.OwnedRecord ="));
        Check(owned > built["_shops"] && owned > built["_garages"],
            "and the stage lookup is wired after both exist, which is the line that threw");
    }
}
