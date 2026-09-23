using System;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using GTA;
using GTA.Math;

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
    const float MeasuredOspreyGround = 16.72f;

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
    }

    static void BM01KeyChecks()
    {
        Reset(); var book = Context(Roster()).Locations;
        Check(OnBunkerRamp(new Vector3(-760f, 5944f, 19.2f), 0f),
            "The old BM01.Yard1 point is inside the bunker entrance ramp's collision, which is the man Ron found in the bunker");
        foreach (var key in new[] { "BM01.Guard1", "BM01.Guard2", "BM01.Guard3", "BM01.Guard4", "BM01.Guard5", "BM01.Yard1", "BM01.Yard2", "BM01.Yard3", "BM01.Pilot", "BM01.Osprey" })
            Check(!OnBunkerRamp(book.Position(key), 1.5f), key + " stands clear of the bunker entrance ramp");
        Check(Math.Abs(book.Position("BM01.Osprey").Z - MeasuredOspreyGround) < .1f,
            "BM01.Osprey's height is the terrain measured under it, not the old estimate 1.3 m above it");
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
}
