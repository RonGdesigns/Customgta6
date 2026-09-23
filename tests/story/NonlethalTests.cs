using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions;
using Bloodlines.Missions.Campaign;
using Bloodlines.Missions.Objectives;
using GTA;
using GTA.Math;
using GTA.Native;

/// <summary>
/// Ron, September 22, on M50: "the guys had stun guns, some guards, and if they were killed the
/// mission was over, but they automatically shot. They didn't start with stun guns." Only Ice was
/// issued one, nobody had it in his hand, and a brother the player was not holding fought with
/// whatever he chose - a carbine. The same crew rule now covers M15. SM02 is a solo job with no
/// brother beside Gohan, and already starts him with the stun gun in his hand.
/// </summary>
public static partial class StoryTests
{
    static void NonlethalChecks()
    {
        NonlethalCrewUnitChecks();
        NonlethalM50Checks();
        NonlethalM50FailureChecks();
        NonlethalSupportChecks();
        NonlethalM15Checks();
        NonlethalSM02Checks();
        NonlethalSourceChecks();
    }

    static bool HoldsStunGun(Ped ped) => ped.Weapons.Owned.Contains(WeaponHash.StunGun) && ped.Weapons.Selected == WeaponHash.StunGun;

    static void NonlethalCrewUnitChecks()
    {
        Reset(); var crew = Roster(); Use(crew, CrewSlot.Ice);
        var ice = crew.PedFor(CrewSlot.Ice); var gohan = crew.PedFor(CrewSlot.Gohan); var guess = crew.PedFor(CrewSlot.Guess);
        foreach (var ped in new[] { ice, gohan, guess }) ped.Weapons.Give(WeaponHash.CarbineRifle, 250, true, true);

        var stun = new NonlethalCrew();
        stun.Begin(crew);
        Check(new[] { ice, gohan, guess }.All(HoldsStunGun), "Every brother is issued a stun gun and has it in his hand");
        Check(ice.CanSwitchWeapons && !gohan.CanSwitchWeapons && !guess.CanSwitchWeapons,
            "The two the player is not holding cannot change weapons; the player can");
        Check(crew.CompanionAI.StunGunOnly, "and the companion controller is told the job is nonlethal");

        stun.Update();
        ice.Weapons.Select(WeaponHash.CarbineRifle, true); stun.Update();
        Check(ice.Weapons.Selected == WeaponHash.CarbineRifle, "The player's own choice of weapon is left alone after gameplay starts");

        gohan.Weapons.Give(WeaponHash.MicroSMG, 300, true, true); stun.Update();
        Check(HoldsStunGun(gohan) && gohan.Weapons.Owned.Contains(WeaponHash.MicroSMG),
            "A gun put in an AI brother's hand is swapped back to the stun gun on the next tick");

        Use(crew, CrewSlot.Gohan); stun.Update();
        Check(gohan.CanSwitchWeapons && HoldsStunGun(gohan), "Whoever the player switches to has the stun gun in his hand");
        Check(!ice.CanSwitchWeapons && HoldsStunGun(ice), "and the brother he left is put back on it and locked");

        stun.End();
        Check(new[] { ice, gohan, guess }.All(p => p.CanSwitchWeapons), "Teardown unlocks every brother");
        Check(new[] { ice, gohan, guess }.All(p => p.Weapons.Selected == WeaponHash.CarbineRifle && p.Weapons.Owned.Contains(WeaponHash.CarbineRifle)),
            "and hands each back the weapon he was holding when the attempt began");
        Check(!crew.CompanionAI.StunGunOnly, "and clears the controller's flag");
        stun.End();
        Check(ice.CanSwitchWeapons && !crew.CompanionAI.StunGunOnly, "Ending twice is harmless");

        // A roster that answers every slot with the one deployed man is a solo job: he is the
        // player, and is never locked as if he were another brother.
        Reset(); crew = Roster(); var solo = crew.PedFor(CrewSlot.Gohan);
        crew.Peds.Clear(); crew.Peds[CrewSlot.Gohan] = solo; crew.ActivePed = solo; Use(crew, CrewSlot.Gohan);
        stun = new NonlethalCrew(); stun.Begin(crew); stun.Update();
        Check(solo.CanSwitchWeapons && HoldsStunGun(solo), "A lone brother is armed as the player and never locked as another slot");
        stun.End();

        crew.CompanionAI.StunGunOnly = true; crew.CompanionAI.ReleaseAll();
        Check(!crew.CompanionAI.StunGunOnly, "Releasing the whole crew clears the flag as a backstop");
    }

    static void NonlethalM50Checks()
    {
        Reset(); var crew = Roster(); var c = Context(crew);
        c.State = CampaignState.Load(Path.Combine(root, "nonlethal-m50.json"));
        c.State.Weapons["Guess"] = new HashSet<uint> { (uint)WeaponHash.CarbineRifle };
        crew.Arsenal = new WeaponProgression(c.State);
        crew.Arsenal.BeginLoan(crew);
        var m = new M50TheRedactedVault();
        Check(m.Begin(c), "M50 starts");
        var ice = crew.PedFor(CrewSlot.Ice); var gohan = crew.PedFor(CrewSlot.Gohan); var guess = crew.PedFor(CrewSlot.Guess);
        guess.Weapons.Give(WeaponHash.CarbineRifle, 250, false, true);
        Check(m.Sentry.Count == M50TheRedactedVault.Sentries && m.Sentry.All(p => p.Weapons.Owned.Contains(WeaponHash.CarbineRifle)),
            "The watchmen keep their carbines (Ron's call)");

        DrainPreparation(m, c);
        Check(m.Status == MissionStatus.Running && m.CurrentStage == 0, "M50 is in its first stage after the scenes");
        Check(new[] { ice, gohan, guess }.All(HoldsStunGun),
            "At the start of gameplay all three brothers have a stun gun in hand, not only Ice");
        Check(crew.ActiveSlot == CrewSlot.Gohan && gohan.CanSwitchWeapons && !ice.CanSwitchWeapons && !guess.CanSwitchWeapons,
            "The player can still draw anything; the two he is not holding cannot");
        Check(crew.CompanionAI.StunGunOnly, "The companion controller holds the brothers to the stun gun for the attempt");

        // A watchman opens fire on Guess from three meters. The role track sends him to cover and
        // to fight hated targets, which the crew's neutrality keeps off the watchmen; he is never
        // sent at a watchman with a gun, never given a drive-by, and still has only the stun gun.
        var shooter = m.Sentry[0];
        int fights = guess.Task.Fights, driveBys = Function.DriveBys;
        shooter.Position = guess.Position + new Vector3(3f, 0f, 0f); shooter.IsShooting = true; shooter.IsInCombat = true; shooter.CombatTarget = guess;
        for (int i = 0; i < 6; i++) { guess.Health -= 40; Game.GameTime += 400; c.Dialogue.Clear(); m.Tick(); }
        Check(m.Status == MissionStatus.Running, "The mission is still running with Guess under fire");
        Check(!m.Sentry.Contains(guess.Task.LastTarget) && guess.Task.Fights == fights && Function.DriveBys == driveBys,
            "Guess under a watchman's fire gets no order to fight him and no drive-by");
        Check(HoldsStunGun(guess) && !guess.CanSwitchWeapons,
            "and whatever fight he is in, he is holding the stun gun and cannot draw the carbine he carries");

        // Something hands an AI brother a lethal gun mid-fight; the next tick takes it back.
        ice.Weapons.Give(WeaponHash.MicroSMG, 300, true, true);
        Game.GameTime += 100; m.Tick();
        Check(HoldsStunGun(ice), "A gun put in Ice's hand while he is not played goes back to the stun gun");

        // Switching: the brother he switches to is handed the stun gun, the one he leaves is locked.
        gohan.Weapons.Give(WeaponHash.CarbineRifle, 250, true, true);
        Game.GameTime += 100; m.Tick();
        Check(gohan.Weapons.Selected == WeaponHash.CarbineRifle, "The player may still choose to shoot");
        Use(crew, CrewSlot.Ice); Game.GameTime += 100; m.Tick();
        Check(ice.CanSwitchWeapons && HoldsStunGun(ice), "Switching to Ice puts the stun gun in his hand");
        Check(!gohan.CanSwitchWeapons && HoldsStunGun(gohan), "and Gohan, left behind, is back on the stun gun and locked");

        m.Abort();
        Check(new[] { ice, gohan, guess }.All(p => p.CanSwitchWeapons) && !crew.CompanionAI.StunGunOnly,
            "An abort gives every brother his weapons back");
        Check(guess.Weapons.Owned.Contains(WeaponHash.CarbineRifle), "Nothing lethal was taken off him in the first place");
        crew.Arsenal.EndLoan(crew);
        Check(!ice.Weapons.Owned.Contains(WeaponHash.StunGun) && !gohan.Weapons.Owned.Contains(WeaponHash.StunGun) && !guess.Weapons.Owned.Contains(WeaponHash.StunGun),
            "The stun guns were a loan and go back at teardown");
        Check(guess.Weapons.Owned.Contains(WeaponHash.CarbineRifle), "and the rifle Guess owned stays");
    }

    static void NonlethalM50FailureChecks()
    {
        // The failure path releases the same way: a watchman dead fails the mission, and teardown
        // still unlocks every brother.
        Reset(); var crew = Roster(); var c = Context(crew); var m = new M50TheRedactedVault();
        Check(m.Begin(c), "M50 starts for the failure path");
        DrainPreparation(m, c);
        var brothers = Protagonist.All.Select(h => crew.PedFor(h.Slot)).ToArray();
        Check(brothers.Count(p => !p.CanSwitchWeapons) == 2, "Two brothers are locked to the stun gun while it runs");
        m.Sentry[0].IsDead = true; Game.GameTime += 100; m.Tick();
        Check(m.Status == MissionStatus.Failed, "A dead watchman still fails the mission, as designed");
        Check(brothers.All(p => p.CanSwitchWeapons) && !crew.CompanionAI.StunGunOnly, "and failure unlocks every brother");

        // A pass goes through the same cleanup.
        Reset(); crew = Roster(); c = Context(crew); m = new M50TheRedactedVault();
        Check(m.Begin(c), "M50 starts for the pass path");
        DrainPreparation(m, c);
        brothers = Protagonist.All.Select(h => crew.PedFor(h.Slot)).ToArray();
        foreach (var name in new[] { "_spliced", "_invalidated" })
            typeof(M50TheRedactedVault).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(m, true);
        m.Pass();
        Check(m.Status == MissionStatus.Passed, "M50 passes once the splice and the invalidation are recorded");
        Check(brothers.All(p => p.CanSwitchWeapons) && !crew.CompanionAI.StunGunOnly, "A pass unlocks every brother");
    }

    /// <summary>A preparation mission with a fight declared, one spared man and one ordinary hostile.</summary>
    sealed class SparedSupportProbe : PreparationOperation
    {
        public readonly HashSet<Ped> Protected = new HashSet<Ped>();
        public override string Id => "M50";
        public override string Title => "Spared support probe";
        protected override bool Setup() => BeginCrew(CrewSlot.Gohan);
        protected override IEnumerable<MissionStage> BuildStages()
        { yield return new MissionStage("Wait", new ConditionObjective("Wait", () => false)); }
        protected override bool Spared(Ped ped) => Protected.Contains(ped);
        public void Engage(Ped hostile) { Opposition.Add(hostile); Fighting = true; }
    }

    static void NonlethalSupportChecks()
    {
        Reset(); var crew = Roster(); var c = Context(crew); c.Config.CrewRejoinsWhenIdle = false;
        var probe = new SparedSupportProbe();
        Check(probe.Begin(c), "The support probe starts");
        DrainPreparation(probe, c);
        var guess = crew.PedFor(CrewSlot.Guess);
        var watchman = new Ped { Position = guess.Position + new Vector3(3f, 0f, 0f) };
        probe.Protected.Add(watchman); probe.Engage(watchman);
        int fights = guess.Task.Fights;
        Game.GameTime += 3000; probe.Tick();
        Check(guess.Task.Fights == fights && guess.Task.LastTarget != watchman,
            "A support order never picks a spared man, even with the mission fighting");
        var hostile = new Ped { Position = guess.Position + new Vector3(6f, 0f, 0f) };
        probe.Engage(hostile);
        Game.GameTime += 3000; probe.Tick();
        Check(guess.Task.LastTarget == hostile, "while an ordinary hostile beside him is still engaged");
        probe.Abort();
    }

    static void NonlethalM15Checks()
    {
        Reset(); var crew = Roster(); var c = Context(crew); var m15 = new M15Crawlspace();
        Check(m15.Begin(c), "M15 starts");
        c.Cutscenes.Skip(); m15.Tick();
        var ice = crew.PedFor(CrewSlot.Ice); var gohan = crew.PedFor(CrewSlot.Gohan); var guess = crew.PedFor(CrewSlot.Guess);
        Check(m15.Status == MissionStatus.Running && crew.ActiveSlot == CrewSlot.Gohan, "M15 runs with Gohan played");
        Check(new[] { ice, gohan, guess }.All(HoldsStunGun),
            "M15 starts every brother with a stun gun in hand: Gohan's used to be a one-charge gun in his pocket, and Guess had none");
        Check(gohan.CanSwitchWeapons && !ice.CanSwitchWeapons && !guess.CanSwitchWeapons && crew.CompanionAI.StunGunOnly,
            "and the brothers the player is not holding cannot draw a gun on a watchman whose death fails the mission");
        m15.Abort();
        Check(new[] { ice, gohan, guess }.All(p => p.CanSwitchWeapons) && !crew.CompanionAI.StunGunOnly, "M15's teardown unlocks them");
    }

    static void NonlethalSM02Checks()
    {
        // SM02 is Gohan alone: DeploySolo dismisses the other two, so there is no AI brother to
        // shoot a guard, and the mission already puts the stun gun in his hand (its pistol is
        // given afterward, not equipped).
        Reset(); var crew = Roster(); var c = Context(crew);
        c.State = CampaignState.Load(Path.Combine(root, "nonlethal-sm02.json"));
        var s2 = new SM02ZeroDayInjection(); World.CollisionReady = true;
        Check(s2.Begin(c), "SM02 starts");
        c.Cutscenes.Skip(); s2.Tick();
        Check(crew.IsSolo && crew.Peds.Count == 1 && crew.ActiveSlot == CrewSlot.Gohan, "SM02 deploys Gohan alone");
        Check(HoldsStunGun(Game.Player.Character), "and he starts gameplay with the stun gun in hand");
        s2.Abort();
    }

    static void NonlethalSourceChecks()
    {
        // Text with line endings and runs of whitespace flattened, so a check never depends on
        // how the file was checked out.
        string Flat(string path) => Regex.Replace(Source(path), @"\s+", " ");
        string controller = Flat("src/Bloodlines/Crew/CompanionController.cs");
        int engage = controller.IndexOf("private void Engage(", StringComparison.Ordinal);
        int end = controller.IndexOf("private bool SeatShotStale(", engage, StringComparison.Ordinal);
        string body = controller.Substring(engage, end - engage);
        int guard = body.IndexOf("if (StunGunOnly) return;", StringComparison.Ordinal);
        Check(guard > 0 && guard < body.IndexOf("companion.Weapons.Give(WeaponHash.MicroSMG", StringComparison.Ordinal) &&
              guard < body.IndexOf("companion.Task.VehicleShootAtPed(target)", StringComparison.Ordinal),
            "The companion controller hands a seated brother no drive-by and no mounted gun on a nonlethal job");
        Check(Regex.IsMatch(controller, @"public void ReleaseAll\(\) \{ StunGunOnly = false;"),
            "and releasing the whole crew clears that");

        string prep = Flat("src/Bloodlines/Missions/Campaign/Act2/PreparationOperation.cs");
        Check(Regex.IsMatch(prep, @"var threat = Opposition\.Where\(p => [^;]*!Spared\(p\)"), "Support orders skip a spared man");

        foreach (var file in new[] { "Act3/M50TheRedactedVault.cs", "Act1/M15Crawlspace.cs" })
        {
            string text = Flat("src/Bloodlines/Missions/Campaign/" + file);
            Check(text.Contains("_stunOnly.Begin(Ctx.Crew, StunRounds);") && text.Contains("_stunOnly.Update();") &&
                  Regex.IsMatch(text, @"try \{ _stunOnly\.End\(\); \} catch"),
                file + " arms, keeps and releases the crew through the shared rule, the release protected on its own");
            Check(!Regex.IsMatch(text, @"Weapons\.Give\(WeaponHash\.StunGun"), file + " hands out no stun gun of its own on the side");
        }
    }
}
