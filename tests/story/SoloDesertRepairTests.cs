using System;
using System.Collections.Generic;
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

public static partial class StoryTests
{
    private static SM01LeadAndKevlar ReadySoloCases(out MissionContext c)
    {
        Reset(); var crew = Roster(); c = Context(crew);
        c.State = CampaignState.Load(Path.Combine(root, "cases-" + Guid.NewGuid() + ".json"));
        var mission = new SM01LeadAndKevlar();
        Check(mission.Begin(c), "The repaired SM01 exterior shipment starts");
        c.Cutscenes.Skip(); mission.Tick(); Use(crew, CrewSlot.Ice);
        Game.Player.Character.Position = mission.Sergei.Position; mission.Tick();
        foreach (var guard in mission.Guards) guard.IsDead = true;
        mission.Tick(); Interact(mission, c, CrewSlot.Ice, mission.Sergei.Position, 4);
        Check(mission.CurrentStage == 3 && mission.CodesGiven, "SM01 earns the codes through the actual objectives");
        return mission;
    }

    static void SoloDesertRepairChecks()
    {
        // Authored points are not synonyms for any navmesh within 35 meters.
        Reset(); var crew = Roster(); var c = Context(crew);
        var anchor = new Vector3(1038.41f, -3059.64f, 5.9f);
        Check(BoundedPlacement.OutsideSoloFreight(anchor), "Ron's SM01 anchor lies outside the excluded freight rows");
        Check(!BoundedPlacement.OutsideSoloFreight(new Vector3(1046, -3080, 5.9f)), "The old inaccessible broker point is explicitly excluded even without collision");
        World.SafeCoordHandler = p => p + new Vector3(0, -30, 0);
        World.RaycastHandler = (a, b) => new RaycastResult();
        bool refused = false;
        try { BoundedPlacement.PedAt(anchor, "wrong lane", BoundedPlacement.OutsideSoloFreight); }
        catch (InvalidOperationException) { refused = true; }
        Check(refused, "A distant navmesh cannot move a guard back into the freight grid");
        World.SafeCoordHandler = p => new Vector3(p.X, p.Y, 30f);
        try { BoundedPlacement.PedAt(new Vector3(-527, 4515, 89.034f), "rail deck"); refused = false; }
        catch (InvalidOperationException) { refused = true; }
        Check(refused, "Missing bridge support never accepts the lower gorge navmesh");
        World.SafeCoordHandler = null; World.RaycastHandler = null;
        var site = c.Locations.Get("M26.RunwayStart");
        Check(site.Heading == 110f && site.Position.DistanceTo(c.Locations.Position("M37.Duster2")) < .01f,
            "The Lazer runway anchor reuses the newer inspected M37 runway center, not the hangar");
        Vector3 origin = BoundedPlacement.Vehicle(c.Locations, "M26.RunwayStart", new Model("lazer"));
        Check(origin.X == site.Position.X && origin.Y == site.Position.Y && origin.Z > site.Position.Z,
            "Vehicle placement preserves authored XY and uses the model's underside for origin height");
        // A hangar roof/wing obstruction outside the center must also reject placement.
        World.RaycastHandler = (a, b) => new RaycastResult { DidHit = true, HitPosition = new Vector3(a.X, a.Y, site.Position.Z + 9f) };
        refused = false; try { BoundedPlacement.Vehicle(c.Locations, "M26.RunwayStart", new Model("lazer")); } catch (InvalidOperationException) { refused = true; }
        Check(refused, "A hangar roof is not accepted as the runway floor");
        World.RaycastHandler = (a, b) => a.Z == b.Z ? new RaycastResult { DidHit = true } : new RaycastResult { DidHit = true, HitPosition = (a + b) * .5f };
        refused = false; try { BoundedPlacement.Vehicle(c.Locations, "M26.RunwayStart", new Model("lazer")); } catch (InvalidOperationException) { refused = true; }
        Check(refused, "An obstruction anywhere in the wing/departure raster rejects the aircraft placement");
        Reset(); crew = Roster(); c = Context(crew);
        var blocker = new Vehicle { Position = c.Locations.Position("M26.RunwayStart"), Model = new Model("granger") };
        World.NearbyVehicles = new[] { blocker };
        refused = false; try { BoundedPlacement.Vehicle(c.Locations, "M26.RunwayStart", new Model("lazer")); } catch (InvalidOperationException) { refused = true; }
        Check(refused && blocker.Exists(), "A parked vehicle blocks the aircraft footprint without being deleted");

        // Actual mission layout, default keys, code exchange and required loading.
        var cases = ReadySoloCases(out c);
        Check(cases.Guards.All(p => GameUtils.IsWithinFlat(p.Position, anchor, 10.1f) && Math.Abs(p.Heading - 180.3f) < .01f),
            "All six SM01 guards use the user-anchored keys and headings, not the obsolete fallback offsets");
        Check(cases.Guards.All(p => BoundedPlacement.OutsideSoloFreight(p.Position)) &&
            cases.Crates.All(p => BoundedPlacement.OutsideSoloFreight(p.Position)) &&
            BoundedPlacement.OutsideSoloFreight(cases.Car.Position) && BoundedPlacement.OutsideSoloFreight(cases.Sergei.Position),
            "Guards, Sergei, cases and vehicle all stage outside the excluded container grid");
        Interact(cases, c, CrewSlot.Ice, c.Locations.Position("SM01.CrateLoad"), 2);
        Check(!cases.Loaded && c.Cutscenes.IsActive, "SM01 does not count cargo on scene startup");
        c.Cutscenes.Skip(); cases.Tick();
        Check(cases.Loaded && cases.Crates.All(p => p.AttachedTo == cases.Car), "Skipping loads both real cases and then confirms them");
        Check(BoundedPlacement.OutsideSoloFreight(Game.Player.Character.Position) && Game.Player.Character.Position.DistanceTo(cases.Car.Position) < 7f,
            "Loading finalization leaves Ice beside the car, never in the freight stack");
        Game.Player.Character.SetIntoVehicle(cases.Car, VehicleSeat.Driver);
        cases.Car.Position = c.Locations.Position("Apartment.Starter.Ice");
        Game.Player.WantedLevel = 2; c.Dialogue.Clear(); cases.Tick();
        Check(cases.Status == MissionStatus.Running && Game.Player.WantedLevel == 2, "Arriving wanted cannot erase pursuit and finish the shipment");
        cases.Car.Position += new Vector3(100, 0, 0); Game.Player.WantedLevel = 0; cases.Tick();
        Check(cases.Status == MissionStatus.Running, "An earlier visit cannot latch a remote completion after losing pursuit elsewhere");
        cases.Car.Position = c.Locations.Position("Apartment.Starter.Ice"); c.Dialogue.Clear(); cases.Tick(); c.Dialogue.Clear(); cases.Tick();
        Check(cases.Status == MissionStatus.Passed, "Both cases, Ice, car and safe arrival complete together");
        cases.Cleanup();

        cases = ReadySoloCases(out c);
        Interact(cases, c, CrewSlot.Ice, c.Locations.Position("SM01.CrateLoad"), 2);
        // Drive the watched director without Skip: emulate only completed walking,
        // not task animation or physics, and leave the real step ordering intact.
        int watchedTicks = 0;
        while (c.Cutscenes.IsActive && watchedTicks++ < 90)
        {
            var blocking = Field<SceneBlocking>(c.Cutscenes, "_blocking");
            var step = blocking?.Current;
            if (step is WalkToStep && step.HasStarted) step.Actor.Position = Field<Vector3>(step, "_point");
            Game.GameTime += 500; c.Dialogue.Clear(); c.Cutscenes.Update();
        }
        cases.Tick();
        Check(watchedTicks < 90 && cases.Loaded && cases.Crates.All(p => p.AttachedTo == cases.Car),
            "The watched loading sequence reaches the same two verified attachments without Skip");
        cases.Abort(); cases.Cleanup();

        cases = ReadySoloCases(out c);
        Interact(cases, c, CrewSlot.Ice, c.Locations.Position("SM01.CrateLoad"), 2);
        cases.Crates[1].RejectAttachments = true; c.Cutscenes.Skip(); cases.Tick();
        Check(!cases.Loaded && cases.Status == MissionStatus.Failed, "A refused case attachment fails without a false loaded flag");
        cases.Cleanup();
        cases = ReadySoloCases(out c);
        Interact(cases, c, CrewSlot.Ice, c.Locations.Position("SM01.CrateLoad"), 2);
        c.Cutscenes.Stop(); cases.Tick();
        Check(!cases.Loaded && cases.Status == MissionStatus.Failed, "Canceling the required loading scene cannot certify delivery");
        cases.Cleanup();

        // Nonlethal control is shared with M15 and persists after subdual ends.
        Reset(); crew = Roster(); c = Context(crew); c.State = CampaignState.Load(Path.Combine(root, "annex-repaired.json"));
        World.CollisionReady = true;
        var annex = new SM02ZeroDayInjection(); Check(annex.Begin(c), "SM02 repaired approach starts");
        Check(Game.Player.Character.Position.DistanceTo(c.Locations.Position("SM02.StairEntry")) >= 10f,
            "Gohan must actually run to the building instead of spawning on his first interaction");
        Check(annex.Guards.All(p => p.Health >= 1000 && !p.DiesWhenInjured && p.MinStunGroundTime == 600000 && !p.IsInvincible),
            "Before the first shot, annex guards have stun-safe health and ground time without disabling stun physics");
        Check(annex.Guards.All(p => Math.Abs(p.Task.ScenarioHeading - DriveUpStep.HeadingBetween(c.Locations.Position("SM02.RoofAccess"), p.Position)) < .01f),
            "Both guards initially face away from the roof approach");
        c.Cutscenes.Skip(); annex.Tick();
        Check(annex.CurrentStage == 0 && !annex.AlarmRaised, "Watching or skipping the introduction does not use the entrance or alert both guards");
        Interact(annex, c, CrewSlot.Gohan, c.Locations.Position("SM02.StairEntry"), 2);
        var first = annex.Guards[0]; var second = annex.Guards[1]; first.LastWeaponHit = (uint)WeaponHash.StunGun; annex.Tick();
        Check(first.IsCuffed && first.DamageProof && !first.IsInvincible && !first.IsDead, "The first actual stun is latched alive without native invincibility suppressing the reaction");
        Check(annex.AlarmRaised && second.Task.Fights == 1 && second.Task.LastTarget == Game.Player.Character,
            "The surviving guard drops his idle task and fights Gohan when his partner is stunned");
        int firstFights = first.Task.Fights;
        second.IsBeingStunned = true; annex.Tick(); first.LastWeaponHit = 0; second.IsBeingStunned = false;
        Check(annex.CurrentStage == 2 && annex.Guards.All(p => p.IsCuffed && !p.IsDead), "Both living guards advance the mission to its terminal");
        int falls = Function.Calls.Count(x => x.Item1 == Hash.SET_PED_TO_RAGDOLL);
        Game.GameTime += 20000; annex.Tick();
        Check(Function.Calls.Count(x => x.Item1 == Hash.SET_PED_TO_RAGDOLL) >= falls + 2 && first.Task.Fights == firstFights,
            "Both guards remain incapacitated after the subdue objective and never receive renewed combat tasks");
        annex.Abort(); annex.Cleanup();
        Check(!first.DamageProof && !first.IsCuffed && first.MinStunGroundTime == -1 && first.DiesWhenInjured,
            "Abort releases the mission's nonlethal settings without a global weapon modifier");
        Reset(); var dead = new Ped(); using (var control = new NonlethalGuards())
        {
            control.Add(dead); dead.IsDead = true; dead.LastWeaponHit = (uint)WeaponHash.StunGun; control.Update();
            Check(dead.IsDead && control.DownCount == 0, "Nonlethal handling never resurrects a genuinely dead guard or reports him successfully subdued");
        }

        // Restock owned weapons explicitly; the test emulates a Give call that does
        // not replenish a weapon already present, unlike the old forgiving stub.
        Reset(); crew = Roster(); Use(crew, CrewSlot.Ice); c = Context(crew);
        var state = CampaignState.Load(Path.Combine(root, "actual-locker-ammo.json"));
        var arsenal = new WeaponProgression(state); var ice = Game.Player.Character;
        ice.Weapons.Give(WeaponHash.CarbineRifle, 0, false, false);
        ice.Weapons.Give(WeaponHash.MG, 0, false, false);
        ice.Weapons.IgnoreRepeatGrant = true;
        ice.Weapons.AmmoTypes[(uint)WeaponHash.CarbineRifle] = 987u;
        ice.Weapons.AmmoTypes[(uint)WeaponHash.MG] = 987u;
        arsenal.Capture(CrewSlot.Ice, ice); state.Unlock("canalLogisticsLoft");
        var homes = new CrewHomes(crew, state, c.Locations, arsenal) { Allowed = () => true };
        ice.Position = homes.Position(CrewSlot.Ice).Value; homes.RestockLocker();
        int ammo = Math.Max(arsenal.RestockCount(CrewSlot.Ice, (uint)WeaponHash.MG, true), arsenal.RestockCount(CrewSlot.Ice, (uint)WeaponHash.CarbineRifle, true));
        ammo = Math.Min(240, ammo);
        Check(arsenal.LastRestockSucceeded && ice.Weapons.Ammo[(uint)WeaponHash.MG] == ammo && ice.Weapons.Ammo[(uint)WeaponHash.CarbineRifle] == ammo,
            "Home locker refills empty already-owned guns once per shared ammunition pool");
        homes.RestockLocker(); Check(ice.Weapons.Ammo[(uint)WeaponHash.MG] == ammo, "Repeated restocking tops up rather than stacking ammunition without limit");
        ice.Weapons.Ammo[(uint)WeaponHash.MG] = 400; ice.Weapons.Ammo[(uint)WeaponHash.CarbineRifle] = 400;
        homes.RestockLocker(); Check(ice.Weapons.Ammo[(uint)WeaponHash.MG] == 400, "Restocking never reduces ammunition already carried above its allowance");
        ice.Weapons.Ammo[(uint)WeaponHash.MG] = 0; ice.Weapons.Ammo[(uint)WeaponHash.CarbineRifle] = 0;
        state.Safehouses["cypressFoundry"] = true; ice.Position = homes.FoundryEntrance.Value;
        Function.InteriorReady = true; World.CollisionReady = true; homes.EnterFoundry(); Game.GameTime += 350; homes.UpdateTransition(); homes.UpdateTransition();
        homes.RestockLocker(); Check(homes.FoundryVisit && arsenal.LastRestockSucceeded && ice.Weapons.Ammo[(uint)WeaponHash.MG] == ammo,
            "The warehouse locker uses the same verified ammo refill, not just a weapon grant");
        ice.Weapons.RejectAmmoWrites = true; ice.Weapons.Ammo[(uint)WeaponHash.MG] = 0; ice.Weapons.Ammo[(uint)WeaponHash.CarbineRifle] = 0;
        homes.RestockLocker(); Check(!arsenal.LastRestockSucceeded && GameUtils.Message.Contains("could not"), "A rejected ammo native is not reported as a successful full restock");
        homes.StopApartment();

        // Two-sided elevated waves target Ice, not an unrelated actor below him.
        Reset(); crew = Roster(); c = Context(crew); c.State = CampaignState.Load(Path.Combine(root, "canyon-positions.json"));
        var canyon = new M25BountyHuntersCanyon(); Check(canyon.Begin(c), "M25 with two tunnel-side waves starts");
        c.Cutscenes.Skip(); canyon.Tick(); Game.Player.Character.Position = c.Locations.Position("M25.BridgeDeck"); canyon.Tick();
        canyon.Tanker.IsDriveable = false; c.Dialogue.Clear(); canyon.Tick();
        Game.GameTime += 7100; canyon.Tick(); Game.GameTime += 7100; canyon.Tick();
        var wave = Flow(canyon)[2].Objectives.OfType<SurviveWavesObjective>().Single();
        Check(wave.Spawned.Any(p => p.Position.Y > 4500) && wave.Spawned.Any(p => p.Position.Y < 4350), "The wave reaches both tunnel-side approaches");
        Check(wave.Spawned.All(p => p.Position.Z > 87f && p.Task.LastTarget == crew.PedFor(CrewSlot.Ice)), "Wave actors stay at rail level and fight Ice instead of chasing Gohan beneath the bridge");
        int waveTicks = 0;
        while (canyon.CurrentStage == 2 && waveTicks++ < 90)
        {
            foreach (var hunter in wave.Spawned) hunter.IsDead = true;
            c.Dialogue.Clear(); Game.GameTime += 1000; canyon.Tick();
        }
        c.Cutscenes.Skip(); canyon.Tick(); c.Dialogue.Clear();
        Game.Player.Character.Position = c.Locations.Position("M25.Riverbed"); canyon.Tick();
        Game.Player.Character.SetIntoVehicle(canyon.Boat, VehicleSeat.Passenger); canyon.Tick();
        c.Dialogue.Clear(); canyon.Tick();
        Check(canyon.Departing && canyon.Status == MissionStatus.Running, "M25 starts actual departure only after Ice boards");
        Game.GameTime += 10000; c.Dialogue.Clear(); canyon.Tick();
        Check(canyon.Status == MissionStatus.Running, "Elapsed time alone cannot pass the boat departure");
        canyon.Boat.Position = canyon.DepartureOrigin + new Vector3(99, 0, 0); canyon.Tick();
        Check(canyon.Status == MissionStatus.Running, "Ninety-nine meters is not the required hundred-meter departure");
        Game.Player.Character.Task.LeaveVehicle();
        canyon.Boat.Position = canyon.DepartureOrigin + new Vector3(110, 0, 0); canyon.Tick();
        Check(canyon.Status == MissionStatus.Running, "The boat cannot finish the mission after leaving Ice behind");
        crew.PedFor(CrewSlot.Ice).SetIntoVehicle(canyon.Boat, VehicleSeat.Passenger);
        Use(crew, CrewSlot.Guess); int orders = Game.Player.Character.Task.BoatTasks;
        Game.GameTime += 4000; c.Dialogue.Clear(); canyon.Tick(); c.Dialogue.Clear(); canyon.Tick();
        Check(canyon.Status == MissionStatus.Passed && Game.Player.Character.Task.BoatTasks == orders,
            "The real crew aboard beyond a hundred meters can pass while Ron is player-controlled, without an NPC drive order");
        canyon.Cleanup();

        // Aircraft spawn with a usable takeoff axis and crew outside their bounds.
        Reset(); crew = Roster(); c = Context(crew); c.State = CampaignState.Load(Path.Combine(root, "runway26.json"));
        var scramble = new M26AlamoScramble(); Check(scramble.Begin(c), "M26 uses the runway staging layout");
        Check(GameUtils.IsWithinFlat(scramble.Lazer.Position, c.Locations.Position("M26.RunwayStart"), .01f) && scramble.Lazer.Heading == 110f,
            "The Lazer spawns on the runway with its heading along the strip");
        Check(Game.Player.Character.Position.DistanceTo(scramble.Lazer.Position) > 10f, "Ron is not deployed into the jet's wing/cockpit geometry");
        scramble.Abort(); scramble.Cleanup();
        Reset(); crew = Roster(); c = Context(crew); c.State = CampaignState.Load(Path.Combine(root, "runway27.json"));
        var flight = new M27FlightRisk(); Check(flight.Begin(c), "M27 uses its own explicit runway start");
        Check(GameUtils.IsWithinFlat(flight.ApproachPlane.Position, c.Locations.Position("M27.RunwayStart"), .01f) && flight.ApproachPlane.Heading == 110f,
            "The Duster is on the runway without a towing or dragging stage");
        flight.Abort(); flight.Cleanup();
        Reset();
    }
}
