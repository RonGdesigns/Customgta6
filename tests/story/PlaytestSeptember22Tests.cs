using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using GTA;
using GTA.Math;
using GTA.Native;

/// <summary>
/// Ron's Act One run, September 22: scene cameras that never came back to the player, M05's
/// boarding that could not be held on a moving sea, Ice's ability unexplained, the hardened
/// crew cars priced like ordinary ones, and a survey editor he wanted to add enemies,
/// vehicles and props with.
/// </summary>
public static partial class StoryTests
{
    static void PlaytestSeptember22Checks()
    {
        SceneCameraHandbackChecks();
        M05AlongsideChecks();
        ThermalBriefingChecks();
        PlacedAdditionChecks();
        HardenedFleetPriceChecks();
    }

    static void SceneCameraHandbackChecks()
    {
        // The engine keeps rendering an eased hand-back's camera until the slide ends. That
        // window is the whole bug: every Act One briefing that ran straight into a staged
        // scene started the second scene inside it.
        Reset(); var crew = Roster(); var c = Context(crew); Function.EaseHoldsView = true;
        Check(c.Cutscenes.Play("M02", "intro", "Briefing"), "A briefing plays");
        var first = World.RenderingCamera;
        Check(first != null && first.Exists(), "The briefing renders its own camera");
        Game.GameTime += CutsceneDirector.SkipGraceMs; c.Cutscenes.Skip();
        Check(World.RenderingCamera == first && first.Exists(), "A skipped briefing slides home: its camera is still rendering during the hand-back");
        Game.GameTime += 250;
        Check(c.Cutscenes.Play("M02", "outro", "Next scene"), "The next scene starts inside that hand-back, a quarter second later, as M02's stash scene did");
        var second = World.RenderingCamera;
        Check(!first.Exists() && second != first && second != null && second.Exists(),
            "The sliding camera is retired when the next scene starts instead of being remembered as a view to restore");
        Game.GameTime += CutsceneDirector.SkipGraceMs; c.Cutscenes.Skip();
        Check(World.RenderingCamera != first, "Ending the second scene never puts the view back on the first scene's dead shot");
        Check(World.RenderingCamera == second, "It slides home from its own shot like any other scene");
        Game.GameTime += CutsceneDirector.HandoffMs + 300; c.Cutscenes.RetireCameras();
        Check(World.RenderingCamera == null && !second.Exists(), "Once the slide is over the view is the player's own and the scene camera is gone");
        Check(c.Cutscenes.CameraReport() == "gameplay", "The diagnostics line can say who has the view");

        // A camera another system owns is still handed back to it when a scene ends.
        Reset(); crew = Roster(); c = Context(crew);
        var theirs = new Camera(); World.RenderingCamera = theirs;
        c.Cutscenes.Play("M02", "outro", "Over another camera"); Game.GameTime += CutsceneDirector.SkipGraceMs; c.Cutscenes.Skip();
        Check(World.RenderingCamera == theirs, "A scene over somebody else's scripted camera still gives that camera back");

        // The host retires cameras before anything can return early from the tick: it used to
        // be the last step, so a running scene held the old camera alive for its whole length.
        string host = Source("src/Bloodlines/BloodlinesMain.cs");
        int retire = host.IndexOf("Step(\"scene cameras\", _cutscenes.RetireCameras);", StringComparison.Ordinal);
        int firstReturn = host.IndexOf("if (_death.IsHandling) {", StringComparison.Ordinal);
        int sceneReturn = host.IndexOf("if (_cutscenes.IsActive)\n", StringComparison.Ordinal);
        if (sceneReturn < 0) sceneReturn = host.IndexOf("if (_cutscenes.IsActive)\r\n", StringComparison.Ordinal);
        Check(retire > 0 && firstReturn > retire && sceneReturn > retire,
            "Scene cameras are retired ahead of the death, apartment and scene early returns");
        Check(Regex.Matches(host, @"Step\(""scene cameras"", _cutscenes\.RetireCameras\);").Count == 1, "and only once a tick");
        string director = Source("src/Bloodlines/Core/CutsceneDirector.cs");
        Check(director.Contains("Function.Call(Hash.RENDER_SCRIPT_CAMS, false, true, HandoffMs, true, false, 0);"),
            "The eased hand-back passes all six arguments the way the game's own scripts do");
    }

    static void M05AlongsideChecks()
    {
        Reset(); var crew = Roster(); var c = Context(crew); var m = new M05TidalLock();
        Check(m.Begin(c), "M05 starts"); c.Cutscenes.Skip();
        foreach (var enemy in World.Created.Where(p => p != m.Mateo)) enemy.IsDead = true; m.Tick();
        Interact(m, c, CrewSlot.Guess, c.Locations.Position("M05.CoveAir"), 4, true); m.Tick();
        var target = m.Mateo.CurrentVehicle; m.Dinghy.Position = target.Position; Game.Player.Character.Position = m.Dinghy.Position; m.Tick();
        target.Speed = 10; m.Dinghy.Speed = 10;
        for (int i = 0; i < 10; i++) { Game.GameTime += 1000; m.Tick(); }
        for (int i = 0; i < 26; i++) { Game.GameTime += 500; target.Position += new Vector3(1, 0, 0); m.Dinghy.Position = target.Position; m.Tick(); }
        for (int i = 0; i < 15 && !m.BoatDisabled; i++) { Game.GameTime += 500; m.Tick(); }
        target.Speed = 0; m.Dinghy.Speed = 0;
        m.Dinghy.Position = target.Position + new Vector3(10, 0, 0); Game.Player.Character.Position = m.Dinghy.Position; m.Tick(); m.Tick();
        Check(m.CurrentStage == 4 && m.RequiredSwitch == CrewSlot.Gohan, "M05 reaches Gohan's boarding with the boats stopped (stage " + m.CurrentStage + ", disabled " + m.BoatDisabled + ")");
        Check(m.HoldingAlongside && Function.Calls.Any(x => x.Item1 == Hash.SET_BOAT_ANCHOR && x.Item2[0] == target && (bool)x.Item2[1]),
            "Mateo's stopped boat is anchored for the boarding, so only one boat can drift");

        // The stand-in does not carry a seated ped with his vehicle; the game does.
        m.Mateo.Position = target.Position;
        // Ron switches to Gohan and the swell has pushed the boats apart.
        var guess = crew.PedFor(CrewSlot.Guess);
        Use(crew, CrewSlot.Gohan);
        m.Dinghy.Position = target.Position + new Vector3(30, 0, 0);
        int before = guess.Task.BoatTasks; m.Tick();
        Check(guess.Task.BoatTasks == before + 1 && guess.Task.LastMissionPoint == target.Position,
            "With Gohan in play, Guess brings the dinghy back alongside Mateo's boat");
        m.Tick(); m.Tick();
        Check(guess.Task.BoatTasks == before + 1, "The approach is not re-issued every frame, which would restart it before the boat moved");
        m.Dinghy.Position = target.Position + new Vector3(8, 0, 0); m.Tick();
        Check(m.DinghyAnchored && Function.Calls.Any(x => x.Item1 == Hash.SET_BOAT_ANCHOR && x.Item2[0] == m.Dinghy && (bool)x.Item2[1]),
            "Alongside, Guess cuts the engine and anchors");

        // The hold itself: a wave is not the player letting go.
        m.Dinghy.Speed = 2.5f; Game.Accept = true; m.Tick(); Game.Accept = false;
        Game.GameTime += 1200; m.Tick();
        Check(m.CurrentStage == 4 && m.CurrentObjective.IndexOf("stop the vehicle", StringComparison.OrdinalIgnoreCase) < 0,
            "Swell of 2.5 m/s counts as stopped on a boat; the old 1 m/s rule never let Gohan start");
        m.Dinghy.Speed = 6f; Game.GameTime += 1000; m.Tick();
        Check(m.CurrentStage == 4 && m.CurrentObjective.Contains("swell"), "A bigger wave pauses the hold and says so rather than throwing the work away (" + m.CurrentObjective + ")");
        m.Dinghy.Speed = 0f; Game.GameTime += 900; m.Tick();
        Check(m.CurrentStage == 5, "Once the boat settles the hold finishes from where it paused, and Mateo is taken aboard");
        c.Cutscenes.Skip(); m.Tick();
        Check(m.Status == MissionStatus.Passed && Function.Calls.Any(x => x.Item1 == Hash.SET_BOAT_ANCHOR && x.Item2[0] == m.Dinghy && !(bool)x.Item2[1]),
            "M05 still finishes, and the dinghy's anchor is lifted so the crew can drive away");

        // The player at the wheel is never fought by an anchor.
        Reset(); crew = Roster(); c = Context(crew); m = new M05TidalLock(); m.Begin(c);
        string source = Source("src/Bloodlines/Missions/Campaign/Act1/M05TidalLock.cs");
        Check(source.Contains("if (player != null && driver.Handle == player.Handle) { SetAnchor(_dinghy, false, ref _dinghyAnchored); return; }"),
            "When the player drives the dinghy himself, it is never anchored under him");
        Check(!Regex.IsMatch(source, @"CurrentStage\s*==\s*4"), "The alongside rule is tied to the boarding stage's own enter and exit, not to a stage number");
        m.Abort();
    }

    static void ThermalBriefingChecks()
    {
        var lines = M05TidalLock.ThermalBriefing(new ModConfig());
        Check(lines.Length == 2 && lines[0].Contains("Thermal Pulse") && lines[1].Contains("Caps Lock") && lines[1].Contains("L3 + R3"),
            "M05 tells the player what Ice's ability is and both ways to use it, with his configured key");
        Check(M05TidalLock.KeyName("Capital") == "Caps Lock" && M05TidalLock.KeyName("F5") == "F5",
            "Keys are named the way they are printed on the keyboard");
        string source = Source("src/Bloodlines/Missions/Campaign/Act1/M05TidalLock.cs");
        Check(source.Contains(".OnEnter(context => { Say(\"M05_S1_01_ICE\"); ExplainThermal(); });"), "The explanation arrives as Ice takes the cliff");

        // The explanation promises the whole generator crew from the perch, so the sight has
        // to reach that far.
        string pulse = Source("src/Bloodlines/Abilities/ThermalPulse.cs");
        var reach = Regex.Match(pulse, @"SniperRadius = (\d+)f");
        Reset(); var crew = Roster(); var c = Context(crew);
        float perchToCrew = c.Locations.Position("M05.CliffPerch").DistanceTo(c.Locations.Position("M05.LightCrew"));
        Check(reach.Success && float.Parse(reach.Groups[1].Value) >= perchToCrew + 10f,
            "Thermal Pulse marks hostiles out past the " + perchToCrew.ToString("0") + " m from Ice's perch to the generator crew");
        Check(pulse.Contains("World.GetNearbyPeds(player, SniperRadius)") && pulse.Contains("if (!hostile && away > ScanRadius) continue;"),
            "Past the old 60 m only hostiles are marked, so a street does not fill with cones");
    }

    static void PlacedAdditionChecks()
    {
        Reset(); var crew = Roster(); var c = Context(crew);
        string path = Path.Combine(root, "additions-" + Guid.NewGuid().ToString("N") + ".tsv");
        MissionAdditions.Load(path);
        Check(MissionAdditions.All.Count == 0, "No additions file is no additions, not an error");

        var editor = new AdditionEditor();
        editor.Open("M05");
        int version = editor.PreviewVersion;
        editor.AdjustCount(1); editor.CycleWeapon(1);
        var men = editor.Drop(new Vector3(10, 20, 3), 90f);
        Check(men != null && men.Id == "A1" && men.Kind == AdditionKind.Enemy && men.Count == 3 && men.Weapon == "rifle" && File.Exists(path),
            "Dropping enemies writes them to the file at once, with the count and weapon chosen");
        editor.CycleKind(1);
        Check(editor.Kind == AdditionKind.Vehicle && editor.PreviewVersion > version, "Changing what is placed changes the stand-in");
        while (editor.VehicleModel != "dinghy") editor.CycleModel(1);
        editor.TogglePatrol();
        var boat = editor.Drop(new Vector3(30, 20, 0), 180f);
        editor.CycleKind(1); editor.CycleModel(-1);
        var prop = editor.Drop(new Vector3(12, 22, 3), 45f);
        Check(boat != null && boat.Vehicle == "dinghy" && boat.Patrol && prop != null && prop.Kind == AdditionKind.Prop && prop.Model == MissionAdditions.PropModels.Last(),
            "A vehicle and a prop go in the same way; cycling backward from the first prop wraps to the last");

        MissionAdditions.Load(path);
        var reread = MissionAdditions.For("M05");
        Check(reread.Count == 3 && reread.Any(a => a.Id == "A1" && a.Count == 3 && a.Position == new Vector3(10, 20, 3) && a.Heading == 90f)
            && reread.Any(a => a.Vehicle == "dinghy" && a.Patrol) && MissionAdditions.For("m05").Count == 3,
            "Everything placed survives a reload exactly, and the mission id is not case-sensitive");
        Check(MissionAdditions.NextId("M05") == "A4" && MissionAdditions.For("M06").Count == 0, "Ids count up per mission; other missions are untouched");

        // In the world: owned by the mission, fighting only once somebody comes close.
        var m = new M05TidalLock();
        Check(m.Begin(c), "M05 starts with additions placed");
        var staged = m.Staged.ToList();
        var placedMen = World.Created.Where(p => p.Position.DistanceTo(new Vector3(10, 20, 3)) < 5f && p.Model.Name == MissionAdditions.EnemyModels[0]).ToList();
        Check(placedMen.Count == 3 && placedMen.All(p => staged.Contains(p) && !p.BlockPermanentEvents),
            "The placed men spawn with the mission, are its to clean up, and can react to the crew");
        var placedBoat = World.Vehicles.FirstOrDefault(v => v.Model.Name == "dinghy" && v.Position.DistanceTo(new Vector3(30, 20, 0)) < 1f);
        Check(placedBoat != null && staged.Contains(placedBoat) && placedBoat.GetPedOnSeat(VehicleSeat.Driver) != null,
            "The placed boat spawns with a man at the wheel, seated outright");
        Check(World.Props.Any(p => p.Model.Name == prop.Model && staged.Contains(p) && p.IsPositionFrozen), "The placed prop spawns where it was put and stays there");

        foreach (var hero in Protagonist.All) crew.PedFor(hero.Slot).Position = new Vector3(5000, 5000, 0);
        Game.Player.Character.Position = new Vector3(5000, 5000, 0);
        int fights() => Function.Calls.Count(x => x.Item1 == Hash.TASK_COMBAT_HATED_TARGETS_AROUND_PED);
        int calm = fights();
        MissionAdditions.Update(Protagonist.All.Select(p => crew.PedFor(p.Slot)));
        Check(fights() == calm, "Nobody near: the placed men hold their orders");
        crew.PedFor(CrewSlot.Ice).Position = new Vector3(10, 60, 3);
        MissionAdditions.Update(Protagonist.All.Select(p => crew.PedFor(p.Slot)));
        int engaged = fights();
        int boatCrew = placedBoat.Seats.Count;
        Check(boatCrew >= 1 && engaged == calm + 3 + boatCrew, "A brother within 60 m of the men and 90 m of the boat: every placed man is told to fight, once");
        MissionAdditions.Update(Protagonist.All.Select(p => crew.PedFor(p.Slot)));
        Check(fights() == engaged, "and not again next frame, which would restart the fight before he could act");
        m.Abort();
        Check(placedMen.All(p => !p.Present) && MissionAdditions.LiveCount == 0, "Ending the mission takes the placed men away with everything else it spawned");

        Check(MissionAdditions.Remove(reread.First(a => a.Id == "A1")) && MissionAdditions.For("M05").Count == 2, "An addition can be removed");
        MissionAdditions.Load(path);
        Check(MissionAdditions.For("M05").All(a => a.Id != "A1"), "and the removal is saved");

        string mission = Source("src/Bloodlines/Missions/Mission.cs");
        string operation = Source("src/Bloodlines/Missions/ContinuousOperation.cs");
        Check(mission.Contains("foreach (var entity in MissionAdditions.Spawn(Id)) Track(entity);") && operation.Contains("protected override bool TakesPlacedAdditions => false;"),
            "Additions are tracked by the mission that spawned them, and an operation leaves them to its chapters so none spawns twice");
        MissionAdditions.Load(null);
    }

    static void HardenedFleetPriceChecks()
    {
        var hardened = CrewVan.FleetChoices.Where(c => c.Hardened).ToList();
        var plain = CrewVan.FleetChoices.Where(c => !c.Hardened).ToList();
        Check(hardened.Select(c => c.Model).OrderBy(n => n).SequenceEqual(new[] { "buffalo4", "insurgent2", "jubilee", "kuruma2", "nightshark" }),
            "The armored and weaponized crew cars are the Kuruma, Buffalo STX, Jubilee, Nightshark and Insurgent");
        Func<string, int> dealer = model => VehiclePricing.Of(new StoryVehicles.Choice(model, model, "Cars"));
        Check(hardened.All(c => c.Price > dealer(c.Model) && c.Price <= dealer(c.Model) * 1.5f),
            "Each costs more as a crew car than the same model at the dealer, by a little rather than a lot");
        Check(hardened.Min(c => c.Price) > plain.Max(c => c.Price), "Every hardened crew car costs more than every ordinary one");
    }
}
