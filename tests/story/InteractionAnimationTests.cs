using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

public static partial class StoryTests
{
    /// <summary>
    /// Ron, September 23: "On Gohan's mission, when he interacted with the laptop there was no
    /// interaction." SM02's root terminal named no animation, so Gohan stood still while the
    /// eight-second bar filled. These checks drive that hold for real, prove the fallback and
    /// inference paths, and hold every on-foot hold in the campaign to having something to play.
    /// </summary>
    static void InteractionAnimationChecks()
    {
        ClearAnimationStubs();

        // ---- SM02's root terminal, driven the way the player plays it.
        Reset(); var crew = Roster(); var c = Context(crew);
        c.State = CampaignState.Load(Path.Combine(root, "interaction-sm02.json"));
        var s2 = new SM02ZeroDayInjection(); World.CollisionReady = true;
        Check(s2.Begin(c), "SM02 starts for the root terminal hold");
        c.Cutscenes.Skip(); s2.Tick();
        Use(crew, CrewSlot.Gohan);
        Interact(s2, c, CrewSlot.Gohan, c.Locations.Position("SM02.StairEntry"), 2);
        foreach (var g in World.Created.Where(p => p.Model.Name == "s_m_m_security_01")) g.IsBeingStunned = true;
        Function.Calls.Clear();
        s2.Tick();
        var hold = s2.CurrentStageObjectives?.OfType<MissionInteraction>().FirstOrDefault();
        Check(hold != null && hold.Animation == MissionInteraction.Typing && !hold.AnimationInferred,
            "SM02's root terminal hold names the typing animation");
        string typingDict = MissionInteraction.Typing.Split('|')[0], typingClip = MissionInteraction.Typing.Split('|')[1];
        Check(Function.Calls.Any(x => x.Item1 == Hash.REQUEST_ANIM_DICT && (string)x.Item2[0] == typingDict),
            "and asks for the typing dictionary when the stage opens, before he reaches the laptop");
        Check(MissionInteraction.Typing == M07WiretapWaltz.RelayAnimation,
            "Typing is the pair M07's relay and M01's shipping terminal already play, not a new guess");

        int stage = s2.CurrentStage;
        var gohan = Game.Player.Character;
        var terminal = c.Locations.Position("SM02.Terminal");
        var laptop = s2.TerminalProp != null && s2.TerminalProp.Exists() ? s2.TerminalProp.Position : terminal;
        var stand = terminal + new Vector3(1.6f, -0.9f, 0f);
        float facing = DriveUpStep.HeadingBetween(stand, laptop);
        gohan.Position = stand; gohan.Heading = (facing + 180f) % 360f;
        Game.Accept = false; s2.Tick();
        Check(gohan.Task.Playing == null, "Standing at the terminal without pressing plays nothing yet");
        Game.GameTime += CutsceneDirector.SkipGraceMs; Game.Accept = true; s2.Tick(); Game.Accept = false;
        Check(gohan.Task.Playing == MissionInteraction.Typing && hold.PlayingAnimation == MissionInteraction.Typing,
            "Pressing at the root terminal starts Gohan typing");
        Check(Math.Abs(gohan.Heading - facing) < 0.5f, "and turns him to face the laptop he is typing on");
        Game.GameTime += MissionInteraction.VerifyAfterMs + 10; s2.Tick();
        Check(gohan.Task.Playing == MissionInteraction.Typing && hold.FellBackFrom == null && s2.CurrentStage == stage,
            "The typing is confirmed playing a moment later, with no fallback, while the bar is still filling");

        Function.Calls.Clear();
        gohan.Position = terminal + new Vector3(12f, 0f, 0f); s2.Tick();
        Check(gohan.Task.Playing == null && hold.PlayingAnimation == null && s2.CurrentStage == stage,
            "Walking away from the terminal stops the typing and the hold");
        Check(Function.Calls.Any(x => x.Item1 == Hash.STOP_ANIM_TASK && x.Item2[0] == (object)gohan && (string)x.Item2[1] == typingDict && (string)x.Item2[2] == typingClip),
            "and stops it by name rather than clearing every task he has");

        gohan.Position = stand; Game.Accept = true; s2.Tick(); Game.Accept = false;
        Check(gohan.Task.Playing == MissionInteraction.Typing, "Coming back and pressing again types again");
        Function.Calls.Clear();
        Game.GameTime += 8000 + 1; s2.Tick();
        Check(s2.CurrentStage == stage + 1 && gohan.Task.Playing == null && hold.PlayingAnimation == null,
            "When the eight-second hold completes the typing stops and the fire escape opens");
        Check(Function.Calls.Any(x => x.Item1 == Hash.REMOVE_ANIM_DICT && (string)x.Item2[0] == typingDict),
            "and the typing dictionary is released with the stage");
        s2.Abort();

        ProbeChecks();
        InferenceChecks();
        CampaignAnimationSourceChecks();
        ClearAnimationStubs();
    }

    static void ClearAnimationStubs()
    {
        Function.MissingAnimDicts.Clear(); Function.SlowAnimDicts.Clear();
        Function.MissingClips.Clear(); Function.LoadedAnimDicts.Clear();
    }

    /// <summary>One hold, outside any mission, pressed at its marker.</summary>
    static MissionInteraction PressProbe(MissionContext c, string action, Vector3 spot, string animation = null, Func<Vehicle> vehicle = null)
    {
        var probe = new MissionInteraction(action, () => spot, 3, 3f, vehicle, animation: animation);
        probe.Enter(c);
        Game.Player.Character.Position = spot + new Vector3(1f, 0f, 0f);
        Game.Accept = true; probe.Update(c); Game.Accept = false;
        return probe;
    }

    static void ProbeChecks()
    {
        ClearAnimationStubs();
        Reset(); var crew = Roster(); var c = Context(crew); Use(crew, CrewSlot.Gohan);
        var ped = Game.Player.Character; var spot = new Vector3(100f, 100f, 20f);

        // Inference, then play.
        var weld = PressProbe(c, "Weld the hatch shut", spot);
        Check(weld.AnimationInferred && weld.Animation == MissionInteraction.Welding && ped.Task.Playing == MissionInteraction.Welding,
            "A hold that names no animation infers one from its text and plays it");
        Game.GameTime += 3001; weld.Update(c);
        Check(weld.Status == ObjectiveStatus.Complete && ped.Task.Playing == null, "and stops it when the hold completes");
        Check(new MissionInteraction("Weld the hatch shut", () => spot, 3, 3f, animation: MissionInteraction.Typing).Animation == MissionInteraction.Typing,
            "An explicit animation always wins over the inference");

        // A dictionary the game does not have falls back to ReachInside, once, and says so.
        Function.MissingAnimDicts.Add(MissionInteraction.Welding.Split('|')[0]);
        var missing = PressProbe(c, "Weld the hatch shut", spot);
        Game.GameTime += 16; missing.Update(c);
        Check(missing.FellBackFrom == MissionInteraction.Welding && ped.Task.Playing == MissionInteraction.ReachInside,
            "A dictionary the game does not have falls back to ReachInside instead of a man standing still");
        missing.Exit(c);
        Check(ped.Task.Playing == null, "Exit stops the fallback too");
        ClearAnimationStubs();

        // A clip that does not take in a dictionary that does exist.
        Function.MissingClips.Add(MissionInteraction.Inspect);
        var badClip = PressProbe(c, "Inspect the seals", spot);
        Check(ped.Task.Playing == null && badClip.PlayingAnimation == MissionInteraction.Inspect, "A clip that does not take is issued and not yet judged");
        Game.GameTime += MissionInteraction.VerifyAfterMs + 10; badClip.Update(c);
        Check(badClip.FellBackFrom == null, "and is given the load window before it is called missing");
        Game.GameTime += MissionInteraction.LoadTimeoutMs; badClip.Update(c); Game.GameTime += 16; badClip.Update(c);
        Check(badClip.FellBackFrom == MissionInteraction.Inspect && ped.Task.Playing == MissionInteraction.ReachInside,
            "then falls back to ReachInside once the window has passed");
        badClip.Exit(c);
        ClearAnimationStubs();

        // A dictionary still streaming at the press starts once it arrives, without a fallback.
        string repairDict = MissionInteraction.Repair.Split('|')[0];
        Function.SlowAnimDicts.Add(repairDict);
        var late = PressProbe(c, "Hotwire the prototype", spot);
        Check(ped.Task.Playing == null && late.FellBackFrom == null, "A dictionary still streaming at the press does not start the clip yet");
        Function.SlowAnimDicts.Clear(); Function.LoadedAnimDicts.Add(repairDict);
        Game.GameTime += MissionInteraction.VerifyAfterMs + 10; late.Update(c);
        Check(ped.Task.Playing == MissionInteraction.Repair && late.FellBackFrom == null,
            "and plays the moment it has arrived, inside the load window, with no fallback");
        Game.GameTime += MissionInteraction.VerifyAfterMs + 10; late.Update(c);
        Check(late.PlayingAnimation == MissionInteraction.Repair && late.FellBackFrom == null, "and is then confirmed playing");
        Function.Calls.Clear(); late.Exit(c);
        Check(ped.Task.Playing == null && Function.Calls.Any(x => x.Item1 == Hash.REMOVE_ANIM_DICT && (string)x.Item2[0] == repairDict),
            "Exit stops the clip and releases the dictionary it asked for");
        ClearAnimationStubs();

        // A swimmer is never given a standing pose, and the work still completes.
        ped.IsSwimming = true;
        int played = ped.Task.Animations;
        var swim = PressProbe(c, "Fit the interceptor to the buoy", spot);
        Check(ped.Task.Animations == played && swim.PlayingAnimation == null, "A swimming man is given no standing animation, whatever the hold asked for");
        Game.GameTime += 3001; swim.Update(c);
        Check(swim.Status == ObjectiveStatus.Complete, "and the hold still completes");
        ped.IsSwimming = false;
        Check(new MissionInteraction("Cut the server out", () => spot, 3, 3f, animation: MissionInteraction.InWater).Animation == null,
            "InWater names no animation on purpose");

        // A hold made from a vehicle plays none, as before.
        var car = World.CreateVehicle(new Model("granger"), spot, 0f);
        Check(new MissionInteraction("Check the crates", () => spot, 3, 3f, () => car).Animation == null
              && new MissionInteraction("Check the crates", () => spot, 3, 3f, () => car, animation: MissionInteraction.Inspect).Animation == null,
            "A hold made from a vehicle plays nothing, explicit or inferred");

        // MultiHoldObjective runs through the same non-blocking path.
        ClearAnimationStubs(); Function.Calls.Clear();
        string kneelDict = MissionInteraction.Kneel.Split('|')[0];
        var sites = new[] { spot, spot + new Vector3(10f, 0f, 0f) };
        var multi = new MultiHoldObjective("Plant the charges on both pylons", sites, 2, 3f, "Planting");
        multi.Enter(c);
        Check(multi.ResolvedAnimation == MissionInteraction.Kneel && Function.Calls.Any(x => x.Item1 == Hash.REQUEST_ANIM_DICT && (string)x.Item2[0] == kneelDict),
            "A multi-site hold that names no animation infers Kneel for planting and asks for it before the first press");
        ped.Position = sites[0] + new Vector3(0.5f, 0f, 0f); Function.Calls.Clear();
        Game.Accept = true; multi.Update(c); Game.Accept = false;
        Check(ped.Task.Playing == MissionInteraction.Kneel && multi.PlayingAnimation == MissionInteraction.Kneel && Function.Calls.Any(x => x.Item1 == Hash.TASK_PLAY_ANIM && x.Item2[0] == (object)ped),
            "and kneels at the first site through the native task, not the wrapper that waits on the load");
        Game.GameTime += 2001; multi.Update(c);
        Check(ped.Task.Playing == null && multi.Remaining == 1, "The site completes and the clip stops");
        Function.MissingAnimDicts.Add(kneelDict);
        ped.Position = sites[1] + new Vector3(0.5f, 0f, 0f);
        Game.Accept = true; multi.Update(c); Game.Accept = false; Game.GameTime += 16; multi.Update(c);
        Check(ped.Task.Playing == MissionInteraction.ReachInside, "A missing dictionary falls back to ReachInside on a multi-site hold as well");
        Function.Calls.Clear(); multi.Exit(c);
        Check(ped.Task.Playing == null && Function.Calls.Any(x => x.Item1 == Hash.REMOVE_ANIM_DICT && (string)x.Item2[0] == kneelDict),
            "Exit stops a multi-site hold's clip and releases its dictionary");
        Check(new MultiHoldObjective("Clamp the floats", sites, 2, 3f, "Clamping", () => car) { Animation = MissionInteraction.Kneel }.ResolvedAnimation == null,
            "A multi-site hold worked from a vehicle plays nothing");
        ClearAnimationStubs();
    }

    static void InferenceChecks()
    {
        var expected = new Dictionary<string, string>
        {
            // The holds this pass named by hand: the inference reaches the same choice from the text.
            { "Gohan: open the Benson and load the crates", MissionInteraction.ReachInside },
            { "Ice: get out, approach the stopped escort cab, and take its IFF transponder.", MissionInteraction.ReachInside },
            { "Guess: check the crates on the flatbed", MissionInteraction.Inspect },
            { "Gohan: inspect the bunker entry room at the yellow marker. Stay here while checking the lights and shelter; the crew waits outside.", MissionInteraction.Inspect },
            { "Ice: observe from the yellow lookout and identify the marine officer; spare the lodge worker", MissionInteraction.Watch },
            { "Ice: glass the steps and let Gohan confirm the man", MissionInteraction.Watch },
            { "Ice: take the service elevator down", MissionInteraction.Operate },
            { "Call the freight lift up to the service floor", MissionInteraction.Operate },
            { "Call the lift to the executive floor", MissionInteraction.Operate },
            { "Ice: approach Sergei and demand the crate codes", MissionInteraction.Handover },
            { "Ice: collect the two cases at the outside loading point", MissionInteraction.ReachInside },
            { "Gohan: run to the building's service door, then take the maintenance stairs to the roof.", MissionInteraction.Operate },
            { "Inject the worm at the root terminal.", MissionInteraction.Typing },
            { "Gohan: return to the roof access and take the maintenance stairs down before IT traces you.", MissionInteraction.Operate },
            { "Gohan: exit and swim beside the buoy; fit the interceptor to its marked service point", MissionInteraction.InWater },
            { "Ice: take the service elevator up to Sterling's floor", MissionInteraction.Operate },
            { "Gohan: back to the service elevator and down", MissionInteraction.Operate },
            // The kinds the set exists for.
            { "Gohan: use the laptop on the shore table", MissionInteraction.Typing },
            { "Plant the limpet charge on the hull", MissionInteraction.Kneel },
            { "Guess: cut through the fence", MissionInteraction.Welding },
            { "Hammer the brace into the frame", MissionInteraction.Hammering },
            { "Guess: hotwire the prototype", MissionInteraction.Repair },
            { "Text Mateo the route", MissionInteraction.Phone },
            { "Throw the breaker at the substation", MissionInteraction.Operate },
            { "Grab the ledger from the safe", MissionInteraction.ReachInside },
            { "Stand here a moment", MissionInteraction.Generic },
            { "", MissionInteraction.Generic },
        };
        foreach (var pair in expected)
            Check(MissionInteraction.Infer(pair.Key) == pair.Value, "Inference reads \"" + pair.Key + "\" as " + pair.Value);
        Check(MissionInteraction.Infer("Gohan: TASK the ASK") == MissionInteraction.Handover && MissionInteraction.Infer("a masked man") == MissionInteraction.Generic,
            "Inference matches whole words and stems, not letters inside other words");
    }

    /// <summary>One hold as written in the source: which class, its label text, whether it is
    /// worked from a vehicle, and the animation it names (resolved to its value) or null.</summary>
    sealed class SourceHold
    {
        public string File, Kind, Label, Animation;
        public bool FromVehicle, LiteralLabel;
    }

    /// <summary>
    /// Every MissionInteraction and MultiHoldObjective in the source, read as code: comments and
    /// string contents are skipped, so a sentence that mentions either class is never counted as
    /// a call, and line endings do not matter.
    /// </summary>
    static List<SourceHold> SourceHolds(out List<string> faults)
    {
        faults = new List<string>();
        var consts = typeof(MissionInteraction).Assembly.GetTypes()
            .SelectMany(t => t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .GroupBy(f => f.Name).ToDictionary(g => g.Key, g => g.Select(f => (string)f.GetRawConstantValue()).Distinct().ToList());
        var holds = new List<SourceHold>();
        foreach (var file in Directory.GetFiles(Path.Combine(Repo, "src", "Bloodlines"), "*.cs", SearchOption.AllDirectories))
        {
            string code = CodeOnly(File.ReadAllText(file));
            string name = Path.GetFileNameWithoutExtension(file);
            int at = 0;
            while ((at = NextCall(code, at, out string kind)) >= 0)
            {
                int open = code.IndexOf('(', at);
                var args = Arguments(code, open, out int close);
                at = close;
                var positional = args.Where(a => !IsNamed(a)).ToList();
                string Named(string key) => args.Where(IsNamed).Select(a => a.Split(new[] { ':' }, 2)).Where(p => p[0].Trim() == key).Select(p => p[1].Trim()).FirstOrDefault();
                bool multi = kind == "MultiHoldObjective";
                string vehicle = Named("vehicle") ?? (positional.Count > (multi ? 5 : 4) ? positional[multi ? 5 : 4] : null);
                string animation;
                if (multi)
                {
                    // MultiHoldObjective takes its animation in an object initializer.
                    animation = null;
                    int j = close + 1; while (j < code.Length && char.IsWhiteSpace(code[j])) j++;
                    if (j < code.Length && code[j] == '{')
                    {
                        var init = Arguments(code, j, out int initClose);
                        at = initClose;
                        animation = init.Select(x => x.Split(new[] { '=' }, 2)).Where(x => x.Length == 2 && x[0].Trim() == "Animation").Select(x => x[1].Trim()).FirstOrDefault();
                    }
                }
                else animation = Named("animation") ?? (positional.Count > 6 ? positional[6] : null);
                if (animation == "null") animation = null;
                string value = null;
                if (animation != null)
                {
                    string id = animation.Split('.').Last().Trim();
                    if (!consts.TryGetValue(id, out var values) || values.Count != 1 || (values[0] != MissionInteraction.InWater && values[0].Split('|').Length != 2))
                        faults.Add(name + ": animation " + animation + " is not one named dictionary|clip constant");
                    else value = values[0];
                }
                string label = positional.Count > 0 ? positional[0] : "";
                holds.Add(new SourceHold
                {
                    File = name, Kind = kind, Label = label, Animation = value,
                    FromVehicle = vehicle != null && vehicle != "null",
                    LiteralLabel = label.StartsWith("\"", StringComparison.Ordinal),
                });
            }
        }
        return holds;
    }

    static bool Mentions(string label, params string[] keys)
    {
        var words = new string((label ?? "").ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ').ToArray())
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        return keys.Any(k => k.EndsWith("*", StringComparison.Ordinal)
            ? words.Any(w => w.StartsWith(k.Substring(0, k.Length - 1), StringComparison.Ordinal))
            : words.Contains(k));
    }

    static readonly string[] DeviceWords = { "laptop*", "terminal*", "console*", "keypad*", "computer*", "server*" };
    static readonly string[] ChargeWords = { "charge", "charges", "limpet*", "plant", "plants", "planted", "planting" };

    static void CampaignAnimationSourceChecks()
    {
        var holds = SourceHolds(out var faults);
        var interactions = holds.Where(h => h.Kind == "MissionInteraction").ToList();
        var multis = holds.Where(h => h.Kind == "MultiHoldObjective").ToList();
        var onFoot = holds.Where(h => !h.FromVehicle).ToList();
        Check(interactions.Count >= 99 && interactions.Count(h => !h.FromVehicle) >= 80 && multis.Count >= 12,
            "The source scan finds the campaign's holds (" + interactions.Count + " MissionInteraction, " + multis.Count + " MultiHoldObjective, " + onFoot.Count + " on foot)");
        Check(multis.Any(h => h.Label.Contains("clear the vault shelves") && h.Animation == MissionInteraction.ReachInside)
              && holds.Any(h => h.Label.Contains("Inject the worm") && h.Animation == MissionInteraction.Typing),
            "The scan reads a multi-site hold's animation out of its initializer and a hold's out of its arguments");
        foreach (var h in onFoot)
            if (MissionInteraction.Resolve(h.Animation, h.Label, false) == null && (h.Animation ?? MissionInteraction.Infer(h.Label)) != MissionInteraction.InWater)
                faults.Add(h.File + ": " + h.Label + " resolves no animation");
        Check(faults.Count == 0, "Every on-foot hold names an animation or resolves one through inference" + (faults.Count > 0 ? ": " + string.Join("; ", faults) : ""));

        // What a hold will actually play: its named animation, or what its label infers to.
        string Plays(SourceHold h) => h.Animation ?? MissionInteraction.Infer(h.Label);

        var devices = onFoot.Where(h => h.LiteralLabel && Mentions(h.Label, DeviceWords)).ToList();
        var bent = devices.Where(h => Plays(h) == MissionInteraction.ReachInside).Select(h => h.File + ": " + h.Label).ToList();
        Check(devices.Count >= 10 && bent.Count == 0,
            "No hold at a laptop, terminal, console, keypad, computer or server reaches into a bin (" + devices.Count + " checked)" + (bent.Count > 0 ? ": " + string.Join("; ", bent) : ""));
        Check(devices.Where(h => Plays(h) != MissionInteraction.InWater).All(h => Plays(h) == MissionInteraction.Typing),
            "and every one of them types, except a server cut out of a wreck underwater");

        // Charges, limpets and planting kneel. A charge armed from a console is worked at the
        // console, so it types; a charge worked from a sub or a kayak plays nothing at all.
        var charges = onFoot.Where(h => h.LiteralLabel && Mentions(h.Label, ChargeWords)).ToList();
        var standing = charges.Where(h =>
        {
            string plays = Plays(h);
            if (plays == MissionInteraction.Kneel || plays == MissionInteraction.InWater) return false;
            return !(Mentions(h.Label, DeviceWords) && plays == MissionInteraction.Typing);
        }).Select(h => h.File + ": " + h.Label + " plays " + Plays(h)).ToList();
        Check(charges.Count >= 6 && standing.Count == 0,
            "Every hold that plants a charge or seats a limpet kneels (" + charges.Count + " checked)" + (standing.Count > 0 ? ": " + string.Join("; ", standing) : ""));

        Check(CodeOnly("// new MissionInteraction(\"x\")\r\nvar s = \"new MultiHoldObjective(\";\n/* new MissionInteraction( */").IndexOf("new Mission", StringComparison.Ordinal) < 0
              && CodeOnly("var s = \"new MultiHoldObjective(\";").IndexOf("MultiHoldObjective", StringComparison.Ordinal) < 0,
            "The scan reads calls, not comments or text that mention either class");

        // Nothing that plays a hold's clip goes through SHVDN's blocking wrapper.
        foreach (var file in new[] { "MissionInteraction.cs", "AdvancedObjectives.cs" })
        {
            string code = CodeOnly(File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Objectives", file)));
            Check(!System.Text.RegularExpressions.Regex.IsMatch(code, @"\.Task\s*\.\s*PlayAnimation\s*\("),
                file + " never calls ped.Task.PlayAnimation, which pauses the script while the dictionary loads");
        }
    }

    static int NextCall(string code, int from, out string kind)
    {
        kind = null;
        for (int i = code.IndexOf("new", from, StringComparison.Ordinal); i >= 0; i = code.IndexOf("new", i + 3, StringComparison.Ordinal))
        {
            if (i > 0 && (char.IsLetterOrDigit(code[i - 1]) || code[i - 1] == '_')) continue;
            int j = i + 3; while (j < code.Length && char.IsWhiteSpace(code[j])) j++;
            foreach (var name in new[] { "MissionInteraction", "MultiHoldObjective" })
            {
                if (string.CompareOrdinal(code, j, name, 0, name.Length) != 0) continue;
                int k = j + name.Length; while (k < code.Length && char.IsWhiteSpace(code[k])) k++;
                if (k < code.Length && code[k] == '(') { kind = name; return i; }
            }
        }
        return -1;
    }

    static bool IsNamed(string arg)
    {
        int colon = arg.IndexOf(':');
        if (colon <= 0) return false;
        string key = arg.Substring(0, colon).Trim();
        return key.Length > 0 && key.All(ch => char.IsLetterOrDigit(ch) || ch == '_') && !char.IsDigit(key[0]);
    }

    /// <summary>The top-level arguments of the call whose '(' is at <paramref name="open"/>, whitespace collapsed.</summary>
    static List<string> Arguments(string code, int open, out int close)
    {
        var args = new List<string>(); var current = new StringBuilder(); int depth = 0; close = code.Length;
        for (int i = open + 1; i < code.Length; i++)
        {
            char ch = code[i];
            if (ch == '(' || ch == '[' || ch == '{') depth++;
            if (ch == ')' || ch == ']' || ch == '}')
            {
                if (depth == 0) { close = i; break; }
                depth--;
            }
            if (ch == ',' && depth == 0) { args.Add(Collapse(current.ToString())); current.Clear(); continue; }
            current.Append(ch);
        }
        if (current.Length > 0 || args.Count > 0) args.Add(Collapse(current.ToString()));
        return args;
    }

    static string Collapse(string s) => string.Join(" ", s.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

    /// <summary>
    /// The source with comments removed and every string literal's text kept but marked, so
    /// the action text survives for inference while a class name inside a string does not read
    /// as code. Literal quotes are kept; the contents are neutralized only for the call search.
    /// </summary>
    static string CodeOnly(string source)
    {
        var sb = new StringBuilder(source.Length);
        for (int i = 0; i < source.Length; i++)
        {
            char ch = source[i];
            if (ch == '/' && i + 1 < source.Length && source[i + 1] == '/') { while (i < source.Length && source[i] != '\n') i++; sb.Append('\n'); continue; }
            if (ch == '/' && i + 1 < source.Length && source[i + 1] == '*') { int end = source.IndexOf("*/", i + 2, StringComparison.Ordinal); i = end < 0 ? source.Length : end + 1; sb.Append(' '); continue; }
            if (ch == '\'')
            {
                int j = i + 1; while (j < source.Length && source[j] != '\'') { if (source[j] == '\\') j++; j++; }
                sb.Append("' '"); i = j; continue;
            }
            if (ch == '"' || (ch == '@' && i + 1 < source.Length && source[i + 1] == '"') || (ch == '$' && i + 1 < source.Length && source[i + 1] == '"'))
            {
                bool verbatim = ch == '@';
                if (ch != '"') i++;
                var text = new StringBuilder();
                int j = i + 1;
                for (; j < source.Length; j++)
                {
                    if (verbatim && source[j] == '"' && j + 1 < source.Length && source[j + 1] == '"') { text.Append('"'); j++; continue; }
                    if (!verbatim && source[j] == '\\') { j++; if (j < source.Length) text.Append(source[j]); continue; }
                    if (source[j] == '"') break;
                    text.Append(source[j]);
                }
                // Keep the words for inference; break any identifier that could read as a call.
                sb.Append('"').Append(text.ToString().Replace("MissionInteraction", "Mission Interaction").Replace("MultiHoldObjective", "MultiHold Objective").Replace("(", " ").Replace(")", " ").Replace(",", " ").Replace("\"", " ")).Append('"');
                i = j; continue;
            }
            sb.Append(ch);
        }
        return sb.ToString();
    }
}
