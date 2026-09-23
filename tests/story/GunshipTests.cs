using System.Linq;
using Bloodlines.Crew;
using Bloodlines.Missions.Campaign;
using GTA;
using GTA.Math;
using GTA.Native;

public static partial class StoryTests
{
    /// <summary>
    /// Ron, September 23: M10's Buzzard "instantly shoots us every time". It should fire near
    /// the crew and only hit a target that sits still. Driven from AuditM10Run once the
    /// gunship is up.
    /// </summary>
    static void GunshipChecks(M10OpenThrottle m, CrewRoster crew, Ped pilot, Bloodlines.Missions.MissionContext c)
    {
        // The gunship's arrival is shown first; its gun waits for that moment to end.
        if (c.Cutscenes.IsActive) { Game.GameTime += Bloodlines.Core.CutsceneDirector.SkipGraceMs; c.Cutscenes.Skip(); }
        Check(pilot.Task.LastHeliMission == VehicleMissionType.Circle, "The gunship circles the crew rather than flying the engine's attack run");

        var truck = m.Flatbed;
        var guess = crew.PedFor(CrewSlot.Guess);
        m.Buzzard.Position = truck.Position + new Vector3(0f, -60f, 40f);
        Function.ClearLos = true;

        // Moving: every round lands near, none on anyone, and none does damage.
        truck.Velocity = new Vector3(0f, 20f, 0f);
        int before = m.GunshipRounds, first = before; bool allClear = true;
        for (int i = 0; i < 60; i++)
        {
            Game.GameTime += 100; truck.Position += new Vector3(0f, 2f, 0f); m.Tick();
            if (m.GunshipRounds == before) continue;
            before = m.GunshipRounds;
            var aim = m.LastGunshipAim;
            allClear &= !m.LastGunshipRoundHit && Protagonist.All.Select(h => crew.PedFor(h.Slot)).Where(p => p != null)
                .All(p => new Vector3(aim.X - p.Position.X, aim.Y - p.Position.Y, 0f).Length() >= M10OpenThrottle.MissNear - 0.01f);
        }
        Check(m.GunshipRounds - first >= M10OpenThrottle.BurstRounds && allClear,
            "A moving crew has the gunship's rounds landing beside them, not on them (" + (m.GunshipRounds - first) + " rounds)");
        var shots = Function.Calls.Where(x => x.Item1 == Hash.SHOOT_SINGLE_BULLET_BETWEEN_COORDS && x.Item2.Length > 9 && x.Item2[9] == pilot).ToList();
        Check(shots.Count > 0 && shots.All(x => (int)x.Item2[6] == 0), "It fires, and a near miss carries no damage");

        // Sitting still in its sight past the grace: the next burst lands.
        truck.Velocity = Vector3.Zero; truck.Speed = 0f;
        m.Buzzard.Position = truck.Position + new Vector3(0f, -60f, 40f);
        for (int i = 0; i < (M10OpenThrottle.StillMs + M10OpenThrottle.BurstIntervalMs) / 100 + 5 && !m.LastGunshipRoundHit; i++) { Game.GameTime += 100; m.Tick(); }
        Check(m.LastGunshipRoundHit && m.LastGunshipAim.DistanceTo(truck.Position - new Vector3(0f, 0f, 0.8f)) < 1.5f,
            "A crew that sits still for a few seconds in its sight gets hit");

        // Still, but in cover: no line of sight means no hit.
        Function.ClearLos = false;
        for (int i = 0; i < 40; i++) { Game.GameTime += 100; m.Tick(); }
        Check(!m.LastGunshipRoundHit, "Behind cover the gunship only fires near, however long they wait");
        Function.ClearLos = true;
    }
}
