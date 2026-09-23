using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

/// <summary>
/// The first half of the Act III audit (Ron, September 22): M49 to M59. Ron's report was that
/// Act III is where things did not spawn where they should and some missions would not start.
/// M50 failed twice in its first sixteen seconds. Each check here is one verified finding,
/// driven through the real mission where the harness can do it.
/// </summary>
public static partial class StoryTests
{
    /// <summary>M51.Crew as Ron surveyed it, from Bloodlines.Surveyed.ini. The harness loads no survey.</summary>
    static readonly Vector3 SurveyedM51Crew = new Vector3(2743.73f, 1393.00f, 24.60f);

    static void AuditActIIIAChecks()
    {
        AuditM49Checks();
        AuditM50Checks();
        AuditM51Checks();
        AuditM52Checks();
        AuditM53Checks();
        AuditM55Checks();
        AuditM56Checks();
        AuditM57Checks();
        AuditM58Checks();
        AuditM59Checks();
    }

    static MissionStage StageNamed(ComposedMission m, string name) => Flow(m).First(s => s.Name == name);

    static bool AnyBrotherStage(MissionStage stage) =>
        stage.AllowsAnyBrother && stage.Objectives.All(o => !o.RequiredCharacter.HasValue);

    static void AuditM49Checks()
    {
        Reset(); var crew = Roster(); var c = Context(crew); var m = new M49ReturnToTheConcrete();
        Check(m.Begin(c), "M49 starts");
        var guess = crew.PedFor(CrewSlot.Guess);
        Check(m.Granger.GetPedOnSeat(VehicleSeat.Driver) == guess,
            "M49 seats Guess at the wheel rather than creating the Granger on top of him at Ron's surveyed spot");

        // The barricade lies across the lane it was built for.
        float heading = Field<float>(m, "_heading");
        var barriers = Field<List<Prop>>(m, "_barriers");
        Check(barriers.Count > 0 && barriers.All(b => Math.Abs(b.Heading - heading) < .01f),
            "Every concrete block takes the lane's heading instead of whatever it was created with");

        // A generator the crew has hit is out, whether or not the frozen prop ever reports dead.
        var towersDown = typeof(M49ReturnToTheConcrete).GetProperty("TowersDestroyed", BindingFlags.Instance | BindingFlags.NonPublic);
        Check(!(bool)towersDown.GetValue(m), "The towers are up before anyone shoots");
        foreach (var tower in m.Towers) Function.TestDamage.Add(tower.Handle);
        Check((bool)towersDown.GetValue(m) && m.Towers.All(t => !t.IsDead),
            "Both towers count as out once a brother has hit them, with neither prop dead");

        Check(AnyBrotherStage(StageNamed(m, "Everyone in the Granger")),
            "The boarding stage belongs to whoever the player is holding, not to Ice from the stage before");
        string src = Source("src/Bloodlines/Missions/Campaign/Act3/M49ReturnToTheConcrete.cs");
        Check(src.Contains("Blips.Attach(guard, BlipColor.Red, \"Armed guard\");"),
            "The checkpoint detail is on the map like every other mission's hostiles");
        m.Abort();

        // A road node pointing back at the crew turns the checkpoint round. The harness node
        // heading is the seed's, so the crew is put north of the seed to make it point at them.
        Reset(); crew = Roster(); c = Context(crew);
        var seed = c.Locations.Position("M49.Checkpoint");
        c.Locations.Get("M49.Start").Position = seed + new Vector3(0f, 250f, 0f);
        m = new M49ReturnToTheConcrete();
        Check(m.Begin(c), "M49 starts with the crew coming from the other side");
        var forward = Field<Vector3>(m, "_forward");
        Check(forward.Y < 0f, "The checkpoint's forward points away from the crew, not along the node's heading");
        Check(m.Towers.All(t => t.Position.Y < m.Lane.Y),
            "and the defenses stand on the far side of the concrete from the crew");
        Check(Math.Abs(Field<float>(m, "_heading") - (c.Locations.Heading("M49.Checkpoint") + 180f) % 360f) < .01f,
            "The flipped heading is the one the concrete and the armored cars are laid out with");
        m.Abort();
    }

    static void AuditM50Checks()
    {
        // The watchman who stood four meters from the crew is posted down the street.
        var guard1 = KeyPoint("M50.Guard1");
        foreach (var key in new[] { "M50.Start", "M50.IceStart", "M50.GohanStart", "M50.GuessStart", "M50.Van" })
            Check(guard1.DistanceTo2D(KeyPoint(key)) >= 40f, "M50.Guard1 is at least 40 m from " + key);
        Check(guard1.DistanceTo2D(KeyPoint("M50.Conduit")) < 45f, "and still on the street the cabinet is on");

        Reset(); var crew = Roster(); var c = Context(crew); var m = new M50TheRedactedVault();
        Function.Calls.Clear();
        Check(m.Begin(c), "M50 starts");
        Check(crew.PedFor(CrewSlot.Ice).Weapons.Owned.Contains(WeaponHash.StunGun),
            "Ice is issued the stun gun the failure message tells him to use");
        var relations = Function.Calls.Where(x => x.Item1 == Hash.SET_RELATIONSHIP_BETWEEN_GROUPS).ToList();
        Check(relations.Any(x => (int)x.Item2[0] == 3 && x.Item2[1] is int && (int)x.Item2[1] == crew.CrewGroup),
            "The crew is neutral toward the watchmen");
        Check(!relations.Any(x => (int)x.Item2[0] == 5 && x.Item2[1] is int && (int)x.Item2[1] == crew.CrewGroup),
            "and nothing tells the crew to hate them, so fighting hated targets can never pick one");
        DrainPreparation(m, c);

        // The two brothers the player is not holding never go after a watchman, however close.
        foreach (var slot in new[] { CrewSlot.Ice, CrewSlot.Guess })
        {
            var brother = crew.PedFor(slot);
            int fights = brother.Task.HatedFights;
            m.Sentry[0].Position = brother.Position + new Vector3(3f, 0f, 0f);
            for (int i = 0; i < 8; i++) { Game.GameTime += 400; c.Dialogue.Clear(); m.Tick(); }
            Check(brother.Task.HatedFights == fights && m.Status == MissionStatus.Running,
                slot + " is not sent to fight a watchman standing three meters away");
        }
        Check(AnyBrotherStage(StageNamed(m, "Back to the van")),
            "Getting back in the van does not demand a switch to Gohan");
        m.Abort();
    }

    static void AuditM51Checks()
    {
        var guard2 = KeyPoint("M51.Guard2");
        Check(guard2.DistanceTo2D(SurveyedM51Crew) >= 40f,
            "M51.Guard2 is posted well away from the crew car Ron surveyed");
        Check(guard2.DistanceTo2D(KeyPoint("M51.Crew")) >= 35f, "and from the authored car spot");

        // A bank closed by QA force-complete has not counted its limpets. The stage exit used to
        // throw there, which is a script error and the end of the attempt.
        Reset(); var crew = Roster(); var c = Context(crew); var m = new M51BlackoutProtocol();
        Check(m.Begin(c), "M51 starts");
        DrainPreparation(m, c);
        foreach (var objective in Flow(m)[0].Objectives) objective.ForceComplete();
        m.Tick();
        Check(m.Status == MissionStatus.Running && m.CurrentStage == 1 && m.West == 3 && m.East == 3 && m.LockedOut,
            "A short limpet count at the wiring stage's exit is recorded, not thrown");
        Check(AnyBrotherStage(StageNamed(m, "Get out of the yard")),
            "Getting back in the car does not demand a switch to Gohan");
        var exit = StageNamed(m, "Clear the station");
        exit.Setup(c);
        Check(AnyBrotherStage(exit) && Field<Func<Vector3>>(m, "DrivingDestination") != null,
            "and the drive out belongs to whoever is in the car, with Guess given the destination when he is at the wheel");
        m.Abort();
    }

    static void AuditM52Checks()
    {
        var scaffold = KeyPoint("M52.Scaffold");
        var roost = KeyPoint("M52.Roost");
        Check(scaffold.DistanceTo2D(roost) < 80f && roost.Z - scaffold.Z > 10f,
            "The way up starts at street level on the roost's own block");

        Reset(); var crew = Roster(); var c = Context(crew); var m = new M52JudicialStrike();
        Check(m.Begin(c), "M52 starts");
        var first = Flow(m)[0].Objectives.OfType<ReachZoneObjective>().Single();
        Check(Field<Func<Vector3>>(first, "_position")().DistanceTo(c.Locations.Position("M52.Scaffold")) < .01f &&
              Flow(m)[1].Name == "Get on the roof",
            "Ice is sent to the scaffold before he is asked for a point on a roof");

        var getaway = StageNamed(m, "Lose the response");
        Check(getaway.RequireAll && getaway.Objectives.OfType<LoseWantedObjective>().Any() &&
              getaway.Objectives.OfType<TravelObjective>().Any() && AnyBrotherStage(getaway),
            "The getaway needs the response lost and the plaza left behind, so it cannot pass the instant Ice sits down");
        var escape = (Vector3)typeof(M52JudicialStrike).GetMethod("Escape", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(m, null);
        var steps = c.Locations.Position("M52.Harrison");
        var bike = c.Locations.Position("M52.Bike");
        Check(Math.Abs(escape.DistanceTo2D(steps) - M52JudicialStrike.EscapeMeters) < 1f && escape.DistanceTo2D(bike) < escape.DistanceTo2D(steps),
            "The ride ends away from the plaza, on the bike's side of it");
        Check(Field<Func<Vector3>>(m, "DrivingDestination") == null, "Guess has no standing order before the getaway");
        getaway.Setup(c);
        Check(Field<Func<Vector3>>(m, "DrivingDestination") != null && Field<Func<Vector3>>(m, "DrivingDestination")().DistanceTo(escape) < .01f,
            "and has somewhere to ride once it starts, with Ice sitting behind him");
        m.Abort();
    }

    static void AuditM53Checks()
    {
        // Tunnel floor two meters under its origins, platform surface 1.24 m under its own.
        Reset(); var crew = Roster(); var c = Context(crew);
        World.RaycastHandler = (s, t) =>
        {
            if (s.X != t.X || s.Y != t.Y) return new RaycastResult();
            float surface = s.X > -440f ? 11.03f : 12.4f;
            return s.Z >= surface && t.Z <= surface
                ? new RaycastResult { DidHit = true, HitPosition = new Vector3(s.X, s.Y, surface) }
                : new RaycastResult();
        };
        var m = new M53SubterraneanSweep();
        try
        {
            Check(m.Begin(c), "M53 starts");
            Check(Math.Abs(m.FloorOffset + 2f) < .01f && Math.Abs(m.PlatformOffset + 1.24f) < .01f,
                "The tunnel and the platform are measured as two data, each by one probe");
            Check(m.Sweepers.Count == 8 && m.Sweepers.All(p => Math.Abs(p.Position.Z - 12.4f) < .05f),
                "The squads stand on the platform surface, not 2 m under their own origins inside the slab");
            Check(Protagonist.All.All(h => Math.Abs(crew.PedFor(h.Slot).Position.Z - 11.03f) < .05f),
                "The crew is put on the tunnel floor the probe found rather than left two meters above it");
            var exit = Flow(m).Last(s => s.Name == "Out through the station").Objectives.OfType<ReachZoneObjective>().Single();
            Check(Math.Abs(Field<Func<Vector3>>(exit, "_position")().Z - c.Locations.Position("M53.Exit").Z) < .01f,
                "The walkway exit keeps its own height instead of the tunnel's offset");
        }
        finally { World.RaycastHandler = null; m.Abort(); }
    }

    static void AuditM55Checks()
    {
        // The regroup is 290 m from the first penthouse. With no walkable ground near it at the
        // start, ground preparation used to refuse the whole mission.
        Reset(); var crew = Roster(); var c = Context(crew);
        World.FailNavigationNear = c.Locations.Position("M55.Regroup");
        var m = new M55SkylineDescent();
        Check(m.Begin(c), "M55 starts even when the street at the regroup has not streamed yet");
        m.Abort();

        // No walkable floor in any suite: every guard used to be stood on his brother.
        Reset(); crew = Roster(); c = Context(crew);
        var suites = new Dictionary<CrewSlot, Vector3>
        {
            { CrewSlot.Ice, c.Locations.Position("M55.IceStart") },
            { CrewSlot.Gohan, c.Locations.Position("M55.GohanStart") },
            { CrewSlot.Guess, c.Locations.Position("M55.GuessStart") },
        };
        World.SafeCoordHandler = p => suites.Values.Any(s => s.DistanceTo(p) < 20f) ? Vector3.Zero : p;
        m = new M55SkylineDescent();
        try
        {
            Check(m.Begin(c), "M55 starts with no navmesh inside the penthouses");
            var details = new[] { Tuple.Create(CrewSlot.Ice, m.SuiteGuardCrew), Tuple.Create(CrewSlot.Gohan, m.TerminalGuardCrew),
                                  Tuple.Create(CrewSlot.Guess, m.VaultGuardCrew) };
            foreach (var detail in details)
                Check(detail.Item2.Count > 0 && detail.Item2.All(g => g.Position.DistanceTo2D(suites[detail.Item1]) >= 2f),
                    detail.Item1 + "'s security stands off his arrival point rather than on top of him");
            var held = m.HeldSuites.ToList();
            var opposition = Field<List<Ped>>(m, "Opposition");
            Check(held.Contains(CrewSlot.Gohan) && held.Contains(CrewSlot.Guess) && !held.Contains(CrewSlot.Ice),
                "Only the suite the player is in starts the fight");
            Check(m.TerminalGuardCrew.All(g => !opposition.Contains(g)) && m.SuiteGuardCrew.All(g => opposition.Contains(g)),
                "and the far suites' security is out of the awareness model and the role tracks until then");
            DrainPreparation(m, c);
            Use(crew, CrewSlot.Gohan);
            c.Dialogue.Clear(); m.Tick();
            Check(!m.HeldSuites.Contains(CrewSlot.Gohan) && m.TerminalGuardCrew.All(g => opposition.Contains(g)),
                "Switching to Gohan brings his suite's security into the fight");
        }
        finally { World.SafeCoordHandler = null; m.Abort(); }
    }

    static void AuditM56Checks()
    {
        Reset(); var crew = Roster(); var c = Context(crew); var m = new M56IronInTheDrain();
        Check(m.Begin(c), "M56 starts");
        DrainPreparation(m, c);
        Check(m.Status == MissionStatus.Running && m.CurrentStage == 0,
            "The drive down the channel is not finished on the first frame by a zone that already contained the half-track");
        m.Abort();
    }

    static void AuditM57Checks()
    {
        Reset(); var crew = Roster(); var c = Context(crew); var m = new M57VespucciFlak();
        Check(m.Begin(c), "M57 starts");
        foreach (var slot in new[] { CrewSlot.Guess, CrewSlot.Ice })
        {
            var owned = crew.PedFor(slot).Weapons.Owned;
            Check(owned.Contains(WeaponHash.HomingLauncher) && owned.Contains(M57VespucciFlak.SeatWeapon),
                slot + " carries the launcher for the sand and a gun that fires from a jet ski seat");
        }
        Check(M57VespucciFlak.SeatWeapon == WeaponHash.MicroSMG, "The seat weapon is one-handed, which is all a Seashark rider can fire");
        m.Abort();
    }

    static void AuditM58Checks()
    {
        Reset(); var crew = Roster(); var c = Context(crew); var m = new M58CartelDecapitation();
        Check(m.Begin(c), "M58 starts");
        var awareness = (GuardAwareness)typeof(PreparationOperation).GetProperty("Awareness", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(m);
        DrainPreparation(m, c);
        Check(!m.Alert && awareness.Suppressed, "The compound is quiet while the crew waits at the mouth of the cul-de-sac");
        var player = Game.Player.Character;
        m.Security[0].Position = player.Position + new Vector3(M58CartelDecapitation.ApproachMeters - 5f, 0f, 0f);
        c.Dialogue.Clear(); m.Tick();
        Check(m.Alert && !awareness.Suppressed, "and wakes when a brother walks up on it, before the gate");
        m.Abort();

        string src = Source("src/Bloodlines/Missions/Campaign/Act3/M58CartelDecapitation.cs");
        Check(!src.Contains("The two vent keys keep their own archive heights"),
            "The note claiming the vents keep their heights is gone, because ground preparation has always stood Gohan beside them");
    }

    static void AuditM59Checks()
    {
        // No helicopter, nothing to clear from the sky: refuse at the start, not fail later.
        Reset(); var crew = Roster(); var c = Context(crew);
        World.FailVehicles = true;
        var m = new M59TheWireCutters();
        Check(!m.Begin(c), "M59 refuses to start when the Aegis helicopter cannot be put in the air");
        World.FailVehicles = false;

        // The roost is measured once the sign's collision is in around Ice.
        Reset(); crew = Roster(); c = Context(crew);
        var authored = c.Locations.Position("M59.Roost");
        World.RaycastHandler = (s, t) => s.X == t.X && s.Y == t.Y && s.Z >= 336.2f && t.Z <= 336.2f
            ? new RaycastResult { DidHit = true, HitPosition = new Vector3(s.X, s.Y, 336.2f) }
            : new RaycastResult();
        m = new M59TheWireCutters();
        try
        {
            Check(m.Begin(c), "M59 starts");
            Check(Math.Abs(m.Roost.Z - authored.Z) < .01f, "Setup does not probe a roof 522 m from the player");
            World.CollisionReady = true;
            DrainPreparation(m, c);
            var ice = crew.PedFor(CrewSlot.Ice);
            Check(Math.Abs(m.Roost.Z - 336.2f) < .01f && Math.Abs(ice.Position.Z - 336.2f) < .01f,
                "Once collision is loaded around Ice the roost is measured and he is stood on it");
        }
        finally { World.RaycastHandler = null; m.Abort(); }

        string src = Source("src/Bloodlines/Missions/Campaign/Act3/M59TheWireCutters.cs");
        int setup = src.IndexOf("protected override bool Setup()", StringComparison.Ordinal);
        int spawn = src.IndexOf("private void SpawnGunship()", StringComparison.Ordinal);
        Check(setup > 0 && spawn > setup && src.IndexOf("OnSurface(", setup, spawn - setup, StringComparison.Ordinal) < 0,
            "No roost probe runs inside Setup");
        Check(src.Contains("_pilot.Task.StartHeliMission(_gunship, at, VehicleMissionType.Circle,") &&
              src.Contains(".OnEnter(c => { Fighting = true; SendTheGunship(); })"),
            "The helicopter holds over its approach and comes for the sign when the override starts");
    }
}
