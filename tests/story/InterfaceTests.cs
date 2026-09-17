using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;

public static partial class StoryTests
{
    /// <summary>A mission built for the HUD: one stage with a gauge, a timer and a rule.</summary>
    private sealed class HudMission : ComposedMission
    {
        public override string Id => "M02";
        public override string Title => "Loose Strands";
        public float Push;
        public readonly List<Ped> Hostiles = new List<Ped>();
        protected override bool Setup()
        {
            var group = World.AddRelationshipGroup("BLOODLINES_AEGIS");
            for (int i = 0; i < 3; i++)
            {
                var ped = Track(World.CreatePed(new Model("s_m_y_blackops_01"), new Vector3(10f + i, 0f, 30f), 0f));
                ped.RelationshipGroup = group;
                Hostiles.Add(ped);
            }
            return true;
        }
        protected override IEnumerable<MissionStage> BuildStages()
        {
            yield return new MissionStage("Hold the line",
                new GaugeObjective("Gohan: hold the pressure", 6f, 9f, 2) { Maximum = 12f, Rate = 4f, Input = () => Push, Unit = "bar", RequiredCharacter = CrewSlot.Gohan },
                new TimerObjective(300, "Out of time."),
                new ProtectObjective("Keep the van alive", () => new Vehicle(), "The van was lost."));
        }
    }

    /// <summary>
    /// What the player sees during and after a mission, and the car page on the phone.
    /// Every number here is read off the running mission; nothing is stored for the HUD.
    /// </summary>
    static void InterfaceChecks()
    {
        // ---- One color per brother, everywhere. The phone's literals are the source.
        string phone = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "CampaignPhone.cs"));
        foreach (var pair in new[] { new { Slot = CrewSlot.Ice, Rgb = "120, 191, 249" }, new { Slot = CrewSlot.Guess, Rgb = "242, 180, 110" }, new { Slot = CrewSlot.Gohan, Rgb = "110, 220, 185" } })
        {
            var color = CrewColors.Of(pair.Slot);
            Check(phone.Contains("Color.FromArgb(" + pair.Rgb + ")") && color.R + ", " + color.G + ", " + color.B == pair.Rgb,
                pair.Slot + " is drawn in the phone's own color on the HUD");
        }

        // ---- The HUD frame, composed from a running mission.
        Reset(); var crew = Roster(); var c = Context(crew);
        var state = CampaignState.Load(Path.Combine(root, "hud-save.json"));
        var mission = new HudMission();
        var def = new MissionDefinition { Info = new MissionInfo { Id = "M02", Title = "Loose Strands", Time = "05:30", Weather = "Clear" }, Factory = () => mission };
        var catalog = new MissionCatalog(); catalog.All.Add(def);
        var manager = new MissionManager(c, state, catalog);
        Use(crew, CrewSlot.Ice);
        Game.GameTime = 800000;
        MissionDefinition started = null;
        manager.Started += d => started = d;
        Check(manager.Start(def), "The HUD mission starts");
        Check(started == null, "The title card is not cued over the briefing");
        for (int i = 0; i < 4 && c.Cutscenes.IsActive; i++) { c.Cutscenes.Skip(); Game.GameTime += 500; manager.Update(); }
        Game.GameTime += 1000; manager.Update();
        Check(started == def, "It is cued when gameplay begins, with the definition");

        var frame = MissionHud.Compose(manager, crew);
        Check(frame.Heading.Contains("M02") && frame.Heading.Contains("Loose Strands") && frame.Heading.Contains("Hold the line"),
            "The heading names the mission and the stage");
        Check(frame.OwnerSlot == CrewSlot.Gohan && frame.Owner.StartsWith("Gohan"),
            "The beat is Gohan's and the chip says so while Ice holds the controller");
        Check(frame.Objective.StartsWith("Switch to Gohan"), "and the objective text asks for the switch");
        Check(frame.Rules.Count == 1 && frame.Rules[0].Contains("van"), "The stage's rule rides under the objective rather than inside it");
        Check(frame.TimerFraction > .97f && (frame.Timer == "5:00" || frame.Timer.StartsWith("4:5")) && !frame.TimerUrgent,
            "The five-minute clock is a nearly full bar with the minutes and seconds on it");
        Check(frame.ProgressFraction >= 0f && frame.Progress.Contains("bar"),
            "and the gauge shows its reading in its unit");

        Use(crew, CrewSlot.Gohan); mission.Push = 1f;
        for (int i = 0; i < 6; i++) { Game.GameTime += 200; manager.Update(); }
        frame = MissionHud.Compose(manager, crew);
        Check(frame.OwnerSlot == CrewSlot.Gohan && !frame.Objective.StartsWith("Switch"), "Held by Gohan, the chip is his and no switch is asked for");
        Check(frame.ProgressFraction > .3f, "and the gauge bar rises as the needle does");

        Game.GameTime += 275000; manager.Update();
        frame = MissionHud.Compose(manager, crew);
        Check(frame.TimerUrgent && frame.TimerFraction < .12f, "With half a minute left the clock is urgent");

        string timer = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Objectives", "StandardObjectives.cs"));
        Check(!timer.Contains("GameUtils.Subtitle(\"~s~\" + remaining / 60"),
            "TimerObjective no longer pushes its clock as a subtitle, where the objective text overwrote it every tick");
        string host = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "BloodlinesMain.cs"));
        Check(host.Contains("MissionHud.Draw(_missions, _crew);") && !host.Contains("MissionObjectiveHud.Draw("),
            "The host draws the HUD from the running mission rather than from one string");

        // ---- The tally: kills credited to whoever was in play, damage off the man in play,
        // switches counted, and what it paid. Then the result lines the panel draws.
        Check(manager.Tally.IsOpen, "The tally opened with gameplay");
        Use(crew, CrewSlot.Ice);
        Game.GameTime += 200; manager.Update();
        mission.Hostiles[0].IsDead = true; mission.Hostiles[1].IsDead = true;
        Game.GameTime += 200; manager.Update();
        Use(crew, CrewSlot.Guess);
        Game.GameTime += 200; manager.Update();
        mission.Hostiles[2].IsDead = true;
        Game.Player.Character.Health = 900; Game.GameTime += 200; manager.Update();
        Game.Player.Character.Health = 760; Game.GameTime += 200; manager.Update();
        mission.Push = 0f;
        var result = manager.Tally.Finish(state.CashOnHand + 12000, 3, 79, true);
        Check(result.Kills == 3 && result.KillsBy[CrewSlot.Ice] == 2 && result.KillsBy[CrewSlot.Guess] == 1,
            "Three hostiles down: two credited to Ice, one to Guess, by who was in play when each was noticed");
        Check(result.Switches >= 3, "Every change of brother is a switch");
        Check(result.DamageTaken == 140, "Damage is what came off the man in play, not the baseline after a switch");
        Check(result.Payout == 12000 && result.CampaignCompleted == 3 && result.CampaignTotal == 79,
            "and it records what the attempt paid and where the campaign stands");
        Check(result.Credits() == "Ice 2  ·  Guess 1", "The credit line names only brothers with a kill, in the crew's order");
        var lines = MissionPresentation.ResultLines(result);
        Check(lines.Any(l => l.Key == "TIME") && lines.Any(l => l.Key == "KILLS" && l.Value.StartsWith("3")) &&
              lines.Any(l => l.Key == "PAYOUT" && l.Value == "$12,000") && lines.Any(l => l.Key == "CAMPAIGN" && l.Value.StartsWith("3 / 79")),
            "The passed panel lists time, kills, payout and the campaign standing");
        var replay = new MissionTally.Result { Payout = 0, FirstCompletion = false };
        Check(MissionPresentation.ResultLines(replay).Any(l => l.Key == "PAYOUT" && l.Value.Contains("replay")),
            "and a replay says it paid nothing rather than showing a zero");
        Check(MissionPresentation.ResultLines(null).Count == 0, "No result draws no lines");
        manager.Abort();

        // ---- The presentation owns both cards and lets neither outlive its moment.
        var presentation = new MissionPresentation(new ModConfig());
        presentation.QueueTitle(def);
        Check(presentation.TitleCardShowing, "A title card is queued when gameplay begins");
        // Frames, not one jump: the presentation caps a frame at a quarter second so a
        // stall cannot skip its own fade, and a card has to be given the frames it needs.
        Game.GameTime += 100; presentation.Update(true, false);
        Check(presentation.TitleCardShowing, "It is still up a tenth of a second in");
        for (int i = 0; i < 30 && presentation.TitleCardShowing; i++) { Game.GameTime += 250; presentation.Update(true, false); }
        Check(!presentation.TitleCardShowing, "and it is gone after its five seconds");
        presentation.QueuePassed("Loose Strands", result);
        Check(presentation.BannerQueued, "The passed panel is queued with its result");
        presentation.Update(true, false);
        Check(!presentation.BannerQueued, "and a new mission's gameplay clears it rather than inheriting it");

        // ---- The car page: a class silhouette and the ratings as bars, with the text
        // carrying only what a bar cannot.
        string hub = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "CampaignHub.cs"));
        Check(hub.Contains("Art = VehicleSpecs.ArtFor(choice.Category)"), "The car page names its class silhouette");
        Check(phone.Contains("if (entry?.Art != null && sprite != null && sprite(entry.Art, 978, 266, 228, 72)) textTop += 76;"),
            "and the reading pane draws it at the top, moving the text down only when it drew");
        Check(phone.Contains("if (fill > 0f) box(1062, textTop + 2, 144 * fill, 8, accent);"),
            "The ratings are drawn as fills in the owner's color");
        foreach (var category in StoryVehicles.Catalog.Select(v => v.Category).Distinct())
            Check(File.Exists(Path.Combine(Repo, "assets", "ui", "phone-" + VehicleSpecs.ArtFor(category) + ".png")),
                "There is a silhouette shipped for the " + category + " class");
        var fast = new Model("adder") { IsCar = true };
        var bars = VehicleSpecs.Fractions(fast);
        Check(bars.Count == 4 && bars.All(b => b.Value >= 0f && b.Value <= 1f) && bars[0].Key == "Top speed",
            "Four bars, each a fraction, top speed first");
        string facts = VehicleSpecs.Facts(fast);
        Check(facts.Contains("Est. top speed") && !facts.Contains("[="),
            "and the words under them carry the mph without repeating the bars as text");
    }
}
