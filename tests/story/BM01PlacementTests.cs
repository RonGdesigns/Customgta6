using System;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using GTA;
using GTA.Math;
using GTA.Native;

/// <summary>
/// Ron's September 22 report on BM01: "one enemy that is spawned in the bunker and the
/// helicopter is floating." Setup runs with the crew 800 m south, before any collision around
/// the bunker has streamed, so every guard post was stood at its authored height and the
/// Osprey was asked onto ground that did not exist yet and frozen in the air.
/// </summary>
public static partial class StoryTests
{
    // The Paleto Forest bunker entrance, read from the installed archives: the placed
    // gr_prop_gr_bunkeddoor_f in gr_case7_bunkerclosed.ymap, and the collision box in its
    // drawable. The box runs along the entrance ramp from 10 m behind the door to 28 m in
    // front of it.
    const float RampX = -782.51f, RampY = 5935.05f, RampHeading = 17.4f;
    const float RampBack = -10.05f, RampFront = 27.72f, RampLeft = -4.38f, RampRight = 4.32f;
    /// <summary>The terrain collision under BM01.Osprey, from cs1_08_32.ybn.</summary>
    const float MeasuredOspreyGround = 8.37f;

    /// <summary>
    /// Trees and rocks from the installed archives around the Osprey's old and new ground,
    /// each with the radius of its canopy or body from its own drawable.
    /// </summary>
    static readonly (string Model, float X, float Y, float Radius)[] SiteObstacles =
    {
        ("prop_tree_cedar_02", -726.74f, 5936.19f, 6.9f), ("prop_tree_pine_02", -724.17f, 5942.18f, 9.0f),
        ("prop_w_r_cedar_01", -733.10f, 5926.21f, 5.2f), ("prop_w_r_cedar_01", -739.32f, 5846.56f, 5.2f),
        ("prop_w_r_cedar_01", -756.76f, 5835.18f, 5.2f), ("prop_tree_pine_02", -738.88f, 5840.28f, 9.0f),
        ("prop_tree_cedar_03", -789.95f, 5912.05f, 6.0f), ("prop_tree_cedar_03", -783.66f, 5919.08f, 6.0f),
        ("prop_tree_birch_03b", -799.51f, 5908.59f, 3.2f), ("csx_coastboulder_02_", -793.86f, 5892.78f, 3.1f),
        ("csx_coastsmalrock_02_", -797.19f, 5889.78f, 1.5f), ("prop_w_r_cedar_01", -734.85f, 5912.25f, 5.2f),
        ("prop_w_r_cedar_01", -722.58f, 5850.04f, 5.2f),
    };

    /// <summary>
    /// The smallest gap between the avenger's footprint, parked at a point and heading, and
    /// any listed obstacle's edge. Negative is an obstacle inside the footprint.
    /// </summary>
    static float OspreyClearance(Vector3 at, float heading)
    {
        var dims = new Model("avenger").Dimensions;
        double h = heading * Math.PI / 180.0;
        float fx = (float)-Math.Sin(h), fy = (float)Math.Cos(h), rx = (float)Math.Cos(h), ry = (float)Math.Sin(h);
        float best = float.MaxValue;
        foreach (var o in SiteObstacles)
        {
            float dx = o.X - at.X, dy = o.Y - at.Y;
            float side = dx * rx + dy * ry, along = dx * fx + dy * fy;
            float cs = Math.Max(dims.Item1.X, Math.Min(dims.Item2.X, side));
            float ca = Math.Max(dims.Item1.Y, Math.Min(dims.Item2.Y, along));
            best = Math.Min(best, (float)Math.Sqrt((side - cs) * (side - cs) + (along - ca) * (along - ca)) - o.Radius);
        }
        return best;
    }

    static bool OnBunkerRamp(Vector3 p, float margin)
    {
        double h = RampHeading * Math.PI / 180.0;
        float dx = p.X - RampX, dy = p.Y - RampY;
        float along = (float)(dx * Math.Cos(h) + dy * Math.Sin(h));
        float across = (float)(-dx * Math.Sin(h) + dy * Math.Cos(h));
        return along >= RampBack - margin && along <= RampFront + margin && across >= RampLeft - margin && across <= RampRight + margin;
    }

    static void BM01PlacementChecks()
    {
        BM01KeyChecks();
        BM01OspreyVerifiedChecks();
        BM01OspreyUnverifiedChecks();
        BM01TakeoffForcesParkChecks();
        BM01GuardPostChecks();
        BM01SpawnClearanceChecks();
        BM01ClimbBeforeChaseChecks();
        BM01CollisionProofChecks();
        BM01TakeoffCannotFailChecks();
    }

    static void BM01KeyChecks()
    {
        Reset(); var book = Context(Roster()).Locations;
        Check(OnBunkerRamp(new Vector3(-760f, 5944f, 19.2f), 0f),
            "The old BM01.Yard1 point is inside the bunker entrance ramp's collision, which is the man Ron found in the bunker");
        foreach (var key in new[] { "BM01.Guard1", "BM01.Guard2", "BM01.Guard3", "BM01.Guard4", "BM01.Guard5", "BM01.Yard1", "BM01.Yard2", "BM01.Yard3", "BM01.Pilot", "BM01.Osprey" })
            Check(!OnBunkerRamp(book.Position(key), 1.5f), key + " stands clear of the bunker entrance ramp");
        Check(Math.Abs(book.Position("BM01.Osprey").Z - MeasuredOspreyGround) < .1f,
            "BM01.Osprey's height is the terrain measured under it");
    }

    /// <summary>A downward probe answers <paramref name="ground"/>; nothing is overhead.</summary>
    static RaycastResult Ground(Vector3 from, Vector3 to, Func<Vector3, float?> ground, Func<Vector3, bool> roofed = null)
    {
        if (from.Z > to.Z)
        {
            var z = ground(from);
            return z.HasValue && z.Value <= from.Z && z.Value >= to.Z
                ? new RaycastResult { DidHit = true, HitPosition = new Vector3(from.X, from.Y, z.Value) }
                : new RaycastResult();
        }
        return roofed != null && roofed(from) ? new RaycastResult { DidHit = true, HitPosition = to } : new RaycastResult();
    }

    static void BM01OspreyVerifiedChecks()
    {
        var m = StartClippedWings(out var crew, out var c);
        var key = c.Locations.Position("BM01.Osprey");
        float origin = -new Model("avenger").Dimensions.Item1.Z;
        Check(m.Osprey.IsPositionFrozen && !m.OspreyParked && Math.Abs(m.Osprey.Position.Z - (key.Z + origin)) < .01f,
            "Before the ground streams in, the Osprey is held with its gear on the measured ground, not 2 m over it");
        Game.GameTime += 600; m.Tick();
        Check(!m.OspreyParked, "and it is not called parked while nothing under it has loaded");

        World.CollisionReady = true;
        World.RaycastHandler = (s, t) => Ground(s, t, p => key.Z);
        Game.GameTime += 600; m.Tick();
        Check(m.OspreyParked && m.OspreyVerified && m.Osprey.IsPositionFrozen && m.Pad == m.Osprey.Position,
            "Once collision has loaded and a probe finds it standing on its gear, it is parked and frozen there");

        // The takeoff still lifts from where it was parked.
        Game.Player.Character = crew.PedFor(CrewSlot.Guess); m.Truck.Position = c.Locations.Position("BM01.Approach"); Game.Player.Character.Position = m.Truck.Position;
        m.Tick(); m.Tick();
        foreach (var guard in m.Guards) guard.IsDead = true;
        m.Tick();
        Check(c.Cutscenes.IsActive && m.OspreyVerified && m.Osprey.IsPositionFrozen, "The takeoff scene shows it where it was verified");
        Game.GameTime += CutsceneDirector.SkipGraceMs; c.Cutscenes.Skip();
        var parked = m.Pad;
        m.Tick();
        Check(!m.Osprey.IsPositionFrozen && Math.Abs(m.Osprey.Position.Z - (parked.Z + 6f)) < .5f,
            "and the lift starts from the parked pad");
        m.Abort();
    }

    static void BM01OspreyUnverifiedChecks()
    {
        // The ground call claims success, but the aircraft is not on its gear.
        var m = StartClippedWings(out var crew, out var c);
        var key = c.Locations.Position("BM01.Osprey");
        var held = m.Osprey.Position;
        World.CollisionReady = true;
        World.RaycastHandler = (s, t) => Ground(s, t, p => key.Z - 5f);
        Game.GameTime += 600; m.Tick();
        Check(!m.OspreyParked && m.Osprey.IsPositionFrozen && m.Osprey.Position == held,
            "An aircraft 5 m off the ground it was probed over is not parked, and it goes back to its measured hold");

        // A probe that finds nothing is not a pass either.
        World.RaycastHandler = (s, t) => new RaycastResult();
        Game.GameTime += 600; m.Tick();
        Check(!m.OspreyParked, "A probe that finds nothing under it does not count as grounded");

        // With the player close and still nothing verified, it is parked at the measured height.
        Game.Player.Character.Position = key + new Vector3(60f, 0f, 0f);
        Game.GameTime += 600; m.Tick();
        Check(!m.OspreyParked, "The player coming close starts a grace period rather than parking it at once");
        Game.GameTime += BM01ClippedWings.SettleGraceMs + 600; m.Tick();
        Check(m.OspreyParked && !m.OspreyVerified && m.Osprey.IsPositionFrozen && m.Osprey.Position == held && held.Z - key.Z < 3f,
            "After the grace it is parked at the measured ground height, never the authored guess, and reported unverified");
        m.Abort();
    }

    static void BM01TakeoffForcesParkChecks()
    {
        // Collision never arrives in the harness: the takeoff scene still shows it on the ground.
        var m = StartClippedWings(out var crew, out var c);
        var key = c.Locations.Position("BM01.Osprey");
        Game.Player.Character = crew.PedFor(CrewSlot.Guess); m.Truck.Position = c.Locations.Position("BM01.Approach"); Game.Player.Character.Position = m.Truck.Position;
        m.Tick(); m.Tick();
        Check(!m.OspreyParked, "The crew at the approach, 76 m out, has not forced it yet");
        foreach (var guard in m.Guards) guard.IsDead = true;
        m.Tick();
        Check(c.Cutscenes.IsActive && m.OspreyParked && m.Osprey.IsPositionFrozen && m.Osprey.Position.Z - key.Z < 3f,
            "The takeoff scene parks it at the measured height before showing it");
        m.Abort();
    }

    static void BM01GuardPostChecks()
    {
        var m = StartClippedWings(out var crew, out var c);
        var guards = m.Guards.ToList();
        var before = guards.Select(g => g.Position).ToList();
        Game.GameTime += 600; m.Tick();
        Check(m.SettledPosts == 0 && guards.Select(g => g.Position).SequenceEqual(before),
            "Nobody is moved before the ground around the posts has streamed in");

        // One man has already left his post before the ground arrives.
        var wandered = guards[4];
        wandered.Position = before[4] + new Vector3(8f, 0f, 0f);
        var wanderedAt = wandered.Position;

        // The gate is on the road at about 14.4. The yard is higher than the authored heights,
        // so a man stood at his authored point is inside the ground there. Guard3's post has a
        // roof over it for 4 m. Nothing answers within 10 m of Guard1 at all.
        var guard1 = before[0]; var guard3 = before[2];
        Func<Vector3, float?> ground = p =>
            GameUtils.IsWithinFlat(p, guard1, 10f) ? (float?)null : p.X > -745f ? 14.4f : 20.5f;
        Func<Vector3, bool> roofed = p => GameUtils.IsWithinFlat(p, guard3, 4f);
        World.CollisionReady = true;
        World.RaycastHandler = (s, t) => Ground(s, t, ground, roofed);
        for (int i = 0; i < BM01ClippedWings.PostProbeTries + 2; i++) { Game.GameTime += 600; m.Tick(); }

        Check(m.SettledPosts == guards.Count, "Every post is checked once collision has loaded, or given up on and logged");
        Check(guards[1].Position == before[1] && guards[3].Position == before[3],
            "A man already standing on the road is left where he is");
        Check(guards.Skip(5).All(g => Math.Abs(g.Position.Z - 20.5f) < .01f),
            "A yard guard found inside the ground is stood on the surface above him");
        Check(!roofed(guards[2].Position) && Math.Abs(guards[2].Position.Z - 14.4f) < .01f && !GameUtils.IsWithinFlat(guards[2].Position, guard3, 4f),
            "A post with something over it is walked out to open sky, never left under the roof");
        Check(guards[0].Position == before[0], "A post nothing answers under is left at the authored point and reported, not guessed");
        Check(wandered.Position == wanderedAt, "A man who has left his post is never teleported back");
        Check(guards.All(g => !roofed(g.Position)), "No guard is left under a roof or inside the structure");
        m.Abort();
    }

    static void BM01SpawnClearanceChecks()
    {
        Reset(); var book = Context(Roster()).Locations;
        Check(OspreyClearance(new Vector3(-738f, 5942f, 16.7f), 90f) < 0f,
            "The old parking spot had trees inside the Osprey's footprint, which is what Ron saw it blow up against");
        var pad = book.Position("BM01.Osprey");
        Check(OspreyClearance(pad, book.Heading("BM01.Osprey")) >= 10f,
            "Parked where it is now, the avenger's whole footprint, wings and rotors, is at least 10 m from any tree or rock");
        Check(!OnBunkerRamp(pad, 25f), "and nowhere near the bunker's entrance ramp");
        Check(BM01ClippedWings.ClearAltitude > 51.9f,
            "The chase's floor is over the tallest canopy between the clearing and the first flight point");
    }

    /// <summary>A started attempt, played through to the takeoff with the crew still in the truck from the start.</summary>
    static BM01ClippedWings ToTakeoff(out CrewRoster crew, out MissionContext c)
    {
        var m = StartClippedWings(out crew, out c);
        Game.Player.Character = crew.PedFor(CrewSlot.Guess); m.Truck.Position = c.Locations.Position("BM01.Approach"); Game.Player.Character.Position = m.Truck.Position;
        m.Tick(); m.Tick();
        foreach (var guard in m.Guards) guard.IsDead = true;
        m.Tick();
        return m;
    }

    static void BM01ClimbBeforeChaseChecks()
    {
        // Ron's second attempt: everyone was already aboard, the stage ended in the same tick
        // the lift began, and the chase started 6 m off the ground among trees.
        var m = ToTakeoff(out var crew, out var c);
        Check(Protagonist.All.All(h => crew.PedFor(h.Slot).IsInVehicle(m.Truck)), "The crew is already aboard the truck when the takeoff plays");
        Game.GameTime += CutsceneDirector.SkipGraceMs; c.Cutscenes.Skip();
        m.Tick(); m.Tick(); m.Tick();
        Check(m.CurrentStage == 2 && !m.Hunting && !m.Climbed && m.Osprey.IsInvincible,
            "With the crew already aboard, the chase still waits while the Osprey is below the trees");
        Check((m.CurrentObjective ?? "").Contains("climbs clear of the trees"), "and the objective says what it is waiting for");
        Check(m.Osprey.Velocity.Z >= BM01ClippedWings.ClimbRate - .01f, "It is climbing meanwhile");
        Check(m.ClimbCeiling >= m.Pad.Z + BM01ClippedWings.HoverHeight && m.ClimbCeiling >= BM01ClippedWings.ClearAltitude,
            "The climb is at least the hover height off the pad and over the canopy");
        ClimbOut(m); m.Tick(); m.Tick();
        Check(m.CurrentStage == 3 && m.Hunting, "Once it is up, the chase begins");
        Check(m.Flight.Points.All(p => p.Z >= BM01ClippedWings.ClearAltitude), "and no point of the flight line is below the canopy floor");
        m.Abort();

        // A climb that stalls in the game is finished by the script instead of holding the chase forever.
        m = ToTakeoff(out crew, out c);
        Game.GameTime += CutsceneDirector.SkipGraceMs; c.Cutscenes.Skip(); m.Tick();
        Game.GameTime += BM01ClippedWings.ClimbTimeoutMs + 100; m.Tick(); m.Tick();
        Check(m.CurrentStage == 3 && m.Osprey.Position.Z >= m.ClimbCeiling - .01f, "A stalled climb is put at the ceiling and the chase goes on");
        m.Abort();
    }

    static void BM01CollisionProofChecks()
    {
        var m = ToTakeoff(out var crew, out var c);
        Game.GameTime += CutsceneDirector.SkipGraceMs; c.Cutscenes.Skip(); m.Tick();
        // Whatever it took as a parked prop: a rocket fired at it during the gate fight, and a scraped hull.
        Function.ExplosiveDamage.Add(m.Osprey.Handle);
        m.Osprey.EngineHealth = 300f; m.Osprey.BodyHealth = 250f;
        int repairs = m.Osprey.Repairs;
        ClimbOut(m); m.Tick(); m.Tick();
        Check(m.Hunting && m.Hull.Hull == 1f && m.Hull.HeavyHits == 0 && m.Osprey.Repairs > repairs && m.Osprey.EngineHealth == 1000f,
            "The damage it took while parked is cleared before it becomes a target, and not credited to the chase");
        Check(m.Osprey.Proofs[3] && !m.Osprey.Proofs[0] && !m.Osprey.Proofs[2] && !m.Osprey.IsInvincible,
            "In the scripted flight it is proof against collisions only: bullets and explosions still land");

        // Clipping a treetop: damage with no weapon behind it.
        m.Osprey.EngineHealth = 40f; m.Osprey.BodyHealth = 60f;
        Game.GameTime += 100; m.Tick();
        Check(m.Status == MissionStatus.Running && !m.Osprey.IsDead && m.Hull.Hull == 1f && m.Osprey.EngineHealth == 1000f,
            "A collision costs no hull and does not bring it down");

        for (int i = 0; i < 20 && m.Hull.Hull > 0f; i++) { Function.ExplosiveDamage.Add(m.Osprey.Handle); m.Tick(); Function.ExplosiveDamage.Remove(m.Osprey.Handle); }
        for (int i = 0; i < 40 && m.Status == MissionStatus.Running; i++) { Game.GameTime += 500; c.Dialogue.Update(); m.Tick(); }
        Check(m.Status == MissionStatus.Passed && m.Osprey.IsDead && m.Hull.HeavyHits >= 6,
            "It still comes down to the hull meter, rocket by rocket");
        Check(!m.Osprey.Proofs.Any(p => p), "and released from the flight, it is proof against nothing");
    }

    static void BM01TakeoffCannotFailChecks()
    {
        // Ron's first attempt: skipped with the pilot short of the aircraft, and the mission failed.
        foreach (bool skip in new[] { true, false })
        {
            var m = StartClippedWings(out var crew, out var c);
            Game.Player.Character = crew.PedFor(CrewSlot.Guess); m.Truck.Position = c.Locations.Position("BM01.Approach"); Game.Player.Character.Position = m.Truck.Position;
            m.Tick(); m.Tick();
            // Somebody in the seat the pilot is going for: the scene's own step cannot seat him.
            var blocker = new Ped(); blocker.SetIntoVehicle(m.Osprey, VehicleSeat.Driver);
            foreach (var guard in m.Guards) guard.IsDead = true;
            m.Tick();
            Check(c.Cutscenes.IsActive, "The takeoff plays");
            if (skip) { Game.GameTime += CutsceneDirector.SkipGraceMs; c.Cutscenes.Skip(); }
            else for (int i = 0; i < 300 && c.Cutscenes.IsActive; i++) { Game.GameTime += 1000; c.Cutscenes.Update(); }
            string how = skip ? "Skipped" : "Watched";
            Check(!c.Cutscenes.IsActive && !m.Pilot.IsInVehicle(m.Osprey) && c.Cutscenes.LastOutcome != SceneOutcome.Completed,
                how + ", the scene ends with the pilot never having boarded");
            Check(!c.Cutscenes.LastRequired, how + ", the takeoff is not a required scene, so the mission is not failed for it");
            m.Tick();
            Check(m.Status == MissionStatus.Running && m.Pilot.IsInVehicle(m.Osprey) && !m.Osprey.IsPositionFrozen && m.CurrentStage == 2,
                how + ", gameplay resumes the same way: the pilot seated directly and the Osprey lifting");
            m.Abort();
        }
    }
}
