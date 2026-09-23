using System;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

/// <summary>
/// BM01, Ron's September 22 bonus: drive north to the Paleto Forest bunker, take the gate,
/// and bring down the Osprey from a gun truck as it flies the coast road, with a hull meter
/// and the Osprey for sale afterward.
/// </summary>
public static partial class StoryTests
{
    static void BonusOspreyChecks()
    {
        ScriptedFlightChecks();
        HullMeterChecks();
        ClippedWingsFlowChecks();
        ClippedWingsFailureChecks();
        OspreyHangarChecks();
        BonusCatalogChecks();
    }

    static void ScriptedFlightChecks()
    {
        Check(ScriptedFlight.PacedSpeed(20f) > ScriptedFlight.PacedSpeed(ScriptedFlight.LeadMeters) &&
              ScriptedFlight.PacedSpeed(ScriptedFlight.LeadMeters) > ScriptedFlight.PacedSpeed(250f),
            "The Osprey opens the gap when the truck closes and waits when it drops back");
        Check(ScriptedFlight.PacedSpeed(-500f) == ScriptedFlight.MaxSpeed && ScriptedFlight.PacedSpeed(5000f) == ScriptedFlight.MinSpeed,
            "and never flies outside its bounds, so a truck that keeps up always has a shot");

        Reset();
        var craft = new Vehicle { Model = new Model("avenger"), Position = new Vector3(0, 0, 32) };
        var flight = new ScriptedFlight(craft, new[] { new Vector3(0, 100, 0), new Vector3(0, 200, 0) });
        Check(flight.Points.All(p => p.Z == ScriptedFlight.Altitude), "It flies a fixed height over each road point");
        flight.Begin();
        Check(Function.Calls.Any(x => x.Item1 == Hash.SET_ENTITY_HAS_GRAVITY && x.Item2[0] == craft && !(bool)x.Item2[1]),
            "The script takes the aircraft off physics, so it cannot wander off the road or fall out of the sky");
        Game.GameTime += 100; flight.Update(new Vector3(0, -40, 0));
        Check(craft.Velocity.Y > 0f && Math.Abs(craft.Velocity.X) < .01f, "It heads for the next road point");
        craft.Position = new Vector3(0, 100, 32); Game.GameTime += 100; flight.Update(new Vector3(0, 60, 0));
        Check(flight.Next == 1, "and moves on once it has passed one");
        craft.Position = new Vector3(0, 200, 32); Game.GameTime += 100; flight.Update(new Vector3(0, 160, 0));
        Check(flight.Finished, "The end of the line is the end of the route");
        flight.Release();
        Check(Function.Calls.Any(x => x.Item1 == Hash.SET_ENTITY_HAS_GRAVITY && x.Item2[0] == craft && (bool)x.Item2[1]),
            "Released, it falls like anything else, which is what a downed aircraft has to do");
    }

    static void HullMeterChecks()
    {
        Reset(); var crew = Roster(); var c = Context(crew);
        var target = new Vehicle { Model = new Model("avenger") };
        var gunner = crew.PedFor(CrewSlot.Ice);
        var hull = new ShootDownObjective("Bring it down", () => target, () => new Entity[] { gunner });
        hull.Enter(c); hull.Update(c);
        Check(hull.Hull == 1f && !hull.IsFinished, "A full meter while nobody is hitting it");
        Check(Function.Calls.Any(x => x.Item1 == Hash.SET_VEHICLE_EXPLODES_ON_HIGH_EXPLOSION_DAMAGE && x.Item2[0] == target && !(bool)x.Item2[1]),
            "One rocket cannot end it before the meter says so");
        Function.TestDamage.Add(target.Handle); hull.Update(c); Function.TestDamage.Remove(target.Handle);
        Check(Math.Abs(hull.Hull - (1f - ShootDownObjective.BulletHit)) < .0001f && hull.Hits == 1, "A frame of gunfire landing takes a sliver");
        target.EngineHealth = 200f; target.BodyHealth = 300f; hull.Update(c);
        Check(target.EngineHealth == 1000f && target.BodyHealth == 1000f, "and it is kept flying until the meter is empty");
        Function.ExplosiveDamage.Add(target.Handle); hull.Update(c); Function.ExplosiveDamage.Remove(target.Handle);
        Check(hull.HeavyHits == 1 && hull.Hull < 1f - ShootDownObjective.HeavyHit, "A rocket takes a chunk");
        int rockets = 0;
        while (hull.Hull > 0f && rockets < 20) { Function.ExplosiveDamage.Add(target.Handle); hull.Update(c); Function.ExplosiveDamage.Remove(target.Handle); rockets++; }
        Check(hull.Hull == 0f && target.IsDead && Function.Calls.Count(x => x.Item1 == Hash.EXPLODE_VEHICLE) == 1,
            "An empty meter blows it up, once");
        hull.Update(c);
        Check(hull.Status == ObjectiveStatus.Complete, "and the wreck completes the objective");
        Check(1f / ShootDownObjective.HeavyHit <= 7f && 1f / ShootDownObjective.BulletHit >= 150f,
            "Seven rockets or a long stretch of gunfire: it takes a lot, as Ron expected");
        var missing = new ShootDownObjective("Nothing", () => null, () => new Entity[0]); missing.Enter(c); missing.Update(c);
        Check(missing.Status == ObjectiveStatus.Failed, "A missing aircraft fails rather than hangs");
        string hud = Source("src/Bloodlines/Core/MissionHud.cs");
        Check(hud.Contains("var hull = current as ShootDownObjective;") && hud.Contains("frame.Progress = \"hull \""),
            "The HUD draws the hull meter under the objective");
    }

    static BM01ClippedWings StartClippedWings(out CrewRoster crew, out MissionContext c, bool bunkerAbsent = false)
    {
        Reset(); crew = Roster(); c = Context(crew);
        // Whether the bunker's exterior was already in the world decides who owns it.
        Function.IplReady = !bunkerAbsent;
        c.State = CampaignState.Load(Path.Combine(root, "bm01-" + Guid.NewGuid().ToString("N") + ".json"));
        var m = new BM01ClippedWings();
        Check(m.Begin(c), "BM01 starts");
        if (c.Cutscenes.IsActive) c.Cutscenes.Skip();
        return m;
    }

    static void ClippedWingsFlowChecks()
    {
        var m = StartClippedWings(out var crew, out var c);
        var truck = m.Truck;
        Check(truck != null && truck.Model.Name == "technical", "The crew rides a gun truck, a Karin Technical");
        Check(crew.PedFor(CrewSlot.Guess).SeatIndex == VehicleSeat.Driver && crew.PedFor(CrewSlot.Gohan).SeatIndex == VehicleSeat.RightFront &&
              crew.PedFor(CrewSlot.Ice).IsInVehicle(truck) && crew.PedFor(CrewSlot.Ice).SeatIndex != VehicleSeat.Driver && crew.PedFor(CrewSlot.Ice).SeatIndex != VehicleSeat.RightFront,
            "Guess drives, Gohan rides beside him, and Ice is in the back on the gun");
        Check(crew.PedFor(CrewSlot.Ice).Weapons.Owned.Contains(WeaponHash.HomingLauncher), "Ice carries the launcher");
        Check(m.Guards.Count == 8 && m.Osprey.Model.Name == "avenger" && m.Osprey.IsPositionFrozen && m.Osprey.IsInvincible,
            "Eight men hold the gate and the yard, and the Osprey sits parked where they can be seen guarding it");
        Check(m.Pilot.IsInvincible && !m.Guards.Contains(m.Pilot), "The pilot is not one of the men to kill: he is the next beat");
        Check(Function.Calls.Any(x => x.Item1 == Hash.REQUEST_IPL && (string)x.Item2[0] == BM01ClippedWings.BunkerMap),
            "The Paleto Forest bunker's exterior is loaded for the attempt");

        // Up the coast, then the gate.
        Game.Player.Character = crew.PedFor(CrewSlot.Guess); truck.Position = c.Locations.Position("BM01.Approach"); Game.Player.Character.Position = truck.Position;
        m.Tick(); m.Tick();
        Check(m.CurrentStage == 1, "Arriving at the bunker opens the gate fight");
        foreach (var guard in m.Guards) guard.IsDead = true;
        m.Tick();
        Check(m.CurrentStage == 2 && c.Cutscenes.IsActive, "The last man down plays the takeoff");
        m.Tick();
        Check(m.Osprey.IsPositionFrozen, "Nothing lifts while the scene is still showing the pilot running for it");
        Game.GameTime += CutsceneDirector.SkipGraceMs; c.Cutscenes.Skip();
        Check(m.Pilot.IsInVehicle(m.Osprey), "Watched or skipped, the pilot ends in the seat");
        m.Tick();
        Check(!m.Osprey.IsPositionFrozen && m.Osprey.Position.Z > c.Locations.Position("BM01.Osprey").Z &&
              Function.Calls.Any(x => x.Item1 == Hash.SET_ENTITY_HAS_GRAVITY && x.Item2[0] == m.Osprey && !(bool)x.Item2[1]),
            "Gameplay resumes with the Osprey already off the ground");

        // Everyone back in the truck, and the chase.
        foreach (var seat in m.SeatPlan()) crew.PedFor(seat.Key).SetIntoVehicle(truck, seat.Value);
        m.Tick(); m.Tick();
        Check(m.CurrentStage == 3 && m.Hunting && m.Flight != null && m.Route.Gates.Count == BM01ClippedWings.FlightPoints + 1,
            "With all three aboard the Osprey flies the coast road: seventeen points and the end of the line");
        Check(!m.Osprey.IsInvincible && m.Pilot.IsInvincible, "The aircraft can be hurt now; the man in the seat stays a target for the gunners");
        int before = crew.PedFor(CrewSlot.Ice).Task.VehicleShots;
        Game.Player.Character = crew.PedFor(CrewSlot.Guess); crew.SetActive(CrewSlot.Guess);
        m.Osprey.Position = truck.Position + new Vector3(0, 60, 30);
        Game.GameTime += 3000; m.Tick();
        Check(crew.PedFor(CrewSlot.Ice).Task.VehicleShots == before + 1, "With Guess driving, Ice works the gun at the Osprey on his own");
        Game.GameTime += 100; m.Tick();
        Check(crew.PedFor(CrewSlot.Ice).Task.VehicleShots == before + 1, "on a slow clock, never every frame");

        // Bring it down.
        for (int i = 0; i < 20 && m.Hull.Hull > 0f; i++) { Function.ExplosiveDamage.Add(m.Osprey.Handle); m.Tick(); Function.ExplosiveDamage.Remove(m.Osprey.Handle); }
        // Then the crew's closing radio, which every mission from M07 on finishes with.
        for (int i = 0; i < 40 && m.Status == MissionStatus.Running; i++) { Game.GameTime += 500; c.Dialogue.Update(); m.Tick(); }
        Check(m.Status == MissionStatus.Passed && m.Osprey.IsDead, "An empty hull meter brings it down and passes the mission");
        Check(Function.Calls.Any(x => x.Item1 == Hash.SET_ENTITY_HAS_GRAVITY && x.Item2[0] == m.Osprey && (bool)x.Item2[1]), "and it falls");

        c.State.CashOnHand = 0; c.State.MarkComplete("BM01", new MissionCatalog());
        Check(c.State.CashOnHand == 150000 && OspreyHangar.ForSale(c.State) && !OspreyHangar.Owned(c.State),
            "The payoff is cash, and the Osprey goes up for sale");
    }

    static void ClippedWingsFailureChecks()
    {
        var m = StartClippedWings(out var crew, out var c);
        var truck = m.Truck;
        Game.Player.Character = crew.PedFor(CrewSlot.Guess); truck.Position = c.Locations.Position("BM01.Approach"); Game.Player.Character.Position = truck.Position;
        m.Tick(); m.Tick();
        foreach (var guard in m.Guards) guard.IsDead = true;
        m.Tick(); Game.GameTime += CutsceneDirector.SkipGraceMs; c.Cutscenes.Skip(); m.Tick();
        foreach (var seat in m.SeatPlan()) crew.PedFor(seat.Key).SetIntoVehicle(truck, seat.Value);
        m.Tick(); m.Tick();
        foreach (var hero in Protagonist.All) crew.PedFor(hero.Slot).Position = m.Osprey.Position + new Vector3(0, -900, 0);
        m.Tick();
        Check(m.Status == MissionStatus.Running && m.Escaping, "Losing it is a warning first, with the escape clock running");
        Game.GameTime += BM01ClippedWings.EscapeGraceMs + 100; m.Tick();
        Check(m.Status == MissionStatus.Failed && (m.FailReason ?? "").Contains("got away"), "and a failure if the truck never gets back under it");

        m = StartClippedWings(out crew, out c, bunkerAbsent: true);
        Function.IplReady = true;
        string source = Source("src/Bloodlines/Missions/Campaign/Bonus/BM01ClippedWings.cs");
        Check(source.Contains("if (_flight.Finished) { Fail(\"The Osprey made it past the end of the coast road and got away.\"); return; }"),
            "Reaching the end of the coast road is it escaping");
        Check(!source.Contains("RequireAsset(_osprey"), "The Osprey is never a required asset: that would fail the mission at the moment it is won");
        Check(source.Contains(".OnEnter(c => { _boarding.Reset(); _liftPending = true; })") && source.Contains("if (_liftPending) { _liftPending = false; Lift(); }"),
            "The lift waits for the takeoff scene to end, because the next stage opens in the same tick the scene starts");
        m.Abort();
        Check(Function.Calls.Any(x => x.Item1 == Hash.REMOVE_IPL && (string)x.Item2[0] == BM01ClippedWings.BunkerMap), "Abort hands back the bunker exterior it loaded");
        Func<int> removals = () => Function.Calls.Count(x => x.Item1 == Hash.REMOVE_IPL && (string)x.Item2[0] == BM01ClippedWings.BunkerMap);
        m = StartClippedWings(out crew, out c); int removed = removals(); m.Abort();
        Check(removals() == removed,
            "and leaves it alone when something else had already loaded it, as the crew's own bunker might have");
    }

    static void OspreyHangarChecks()
    {
        Reset();
        var state = CampaignState.Load(Path.Combine(root, "osprey-" + Guid.NewGuid().ToString("N") + ".json"));
        Check(!OspreyHangar.ForSale(state) && OspreyHangar.Rows(state, () => true, p => 0f, p => 50f).Count == 0,
            "Nothing about the Osprey shows before BM01");
        state.Completed.Add("BM01"); state.CashOnHand = 100000;
        var rows = OspreyHangar.Rows(state, () => true, p => 0f, p => 50f);
        Check(rows.Count == 1 && rows[0].Subtitle == "$500,000", "After it, the phone's garage page offers it at the top of the price ladder");
        Check(OspreyHangar.Buy(state).StartsWith("Need") && state.CashOnHand == 100000 && !OspreyHangar.Owned(state), "Short of cash is no sale and no charge");
        state.CashOnHand = 600000;
        Check(OspreyHangar.Buy(state).Contains("crew's") && state.CashOnHand == 100000 && OspreyHangar.Owned(state), "Bought once, for $500,000");
        Check(OspreyHangar.Buy(state).Contains("already") && state.CashOnHand == 100000, "and never charged twice");
        string savePath = Path.Combine(root, "osprey-reload-" + Guid.NewGuid().ToString("N") + ".json");
        var saved = CampaignState.Load(savePath); saved.Completed.Add("BM01"); saved.CashOnHand = 600000; OspreyHangar.Buy(saved);
        var reloaded = CampaignState.Load(savePath);
        Check(OspreyHangar.Owned(reloaded), "Ownership survives a reload");

        Check(!OspreyHangar.FindPad(Vector3.Zero, p => 0f, p => 5f, out _), "No open ground, no landing: it is never dropped into the trees");
        Check(OspreyHangar.FindPad(Vector3.Zero, p => 0f, p => 30f, out var pad) && pad.DistanceTo(Vector3.Zero) >= 30f,
            "Open ground near the player takes it");
        Game.Player.Character.Position = Vector3.Zero;
        string delivered = OspreyHangar.Deliver(state, p => 0f, p => 30f);
        Check(delivered.StartsWith("The Osprey is down") && World.Vehicles.Any(v => v.Model.Name == "avenger") && OspreyHangar.IsOut,
            "Called in, it is set down near the player and marked");
        Check(OspreyHangar.Rows(state, () => false, p => 0f, p => 30f)[0].Action().Contains("Finish the mission"), "Not during a mission");
    }

    static void BonusCatalogChecks()
    {
        var data = CampaignData.Load(dataDir);
        var info = data.Mission("BM01");
        Check(info != null && info.IsBonus && !info.IsSolo && info.Prerequisite == "M70" && info.Title == "CLIPPED WINGS",
            "BM01 is an authored bonus mission that opens after M70");
        Check(data.MainMission(1) != null && data.SoloMissions.Count() == 9 && data.BonusMissions.Count() == 1,
            "The bible's 79 are unchanged; the bonus is loaded beside them");
        Check(!File.ReadAllText(Path.Combine(dataDir, "missions.tsv")).Contains("BM01"), "and never written into the bible extraction");
        Check(CampaignState.StoryGates["BM01"].SequenceEqual(new[] { "SM01", "SM02", "SM03", "SM04", "SM05", "SM06", "SM07", "SM08", "SM09" }),
            "It waits for every solo job too: the rest of the campaign has to be played first");
        string catalog = Source("src/Bloodlines/Missions/MissionCatalog.cs");
        Check(catalog.Contains("{ \"BM01\", () => new BM01ClippedWings() },") && catalog.Contains("_order.AddRange(_bonus);"),
            "It is registered, and offered after everything else");
        Check(MissionContextCard.Cards.ContainsKey("BM01") && File.ReadAllText(Path.Combine(dataDir, "mission_starts.tsv")).Contains("BM01\tBM01.Start"),
            "It has a briefing card and a start marker like any other mission");

        // The install is what the game loads, and the packager copies a fixed list. BM01 was
        // merged with its data file missing from that list, so the game would never have seen it.
        string package = Source("tools/package.py");
        string sources = string.Join("\n", Directory.GetFiles(Path.Combine(Repo, "src"), "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
        var loaded = System.Text.RegularExpressions.Regex.Matches(sources, @"Path\.Combine\(dataDirectory, ""([a-z_]+\.(?:tsv|json|txt))""")
            .Cast<System.Text.RegularExpressions.Match>().Select(x => x.Groups[1].Value).Distinct().Where(n => n != "savegame.json").ToList();
        Check(loaded.Count >= 8 && loaded.All(n => package.Contains("'" + n + "'")),
            "Every data file the mod reads at runtime is copied by the packager (missing: " + string.Join(", ", loaded.Where(n => !package.Contains("'" + n + "'"))) + ")");
    }
}
