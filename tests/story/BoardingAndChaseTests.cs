using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bloodlines.Core;
using Bloodlines.Crew;
using Bloodlines.Missions.Campaign;
using GTA;
using GTA.Math;
using GTA.Native;

public static partial class StoryTests
{
    static void BoardingAndChaseChecks()
    {
        // ---------------------------------------------------------------- boarding
        // Nothing in the campaign ever ordered a brother into a vehicle. Both
        // EnterVehicleObjective(requireCrew) and a hand-written "everyone aboard"
        // condition only ask whether he is already in it, so the Paleto pickup waited
        // on two men treading water and the run south waited on a man on a beach.
        Reset();
        var crew = Roster();
        Use(crew, CrewSlot.Guess);
        var boat = new Vehicle { Model = new Model("tropic"), Position = new Vector3(0f, 0f, 0f) };
        var guess = crew.PedFor(CrewSlot.Guess);
        guess.SetIntoVehicle(boat, VehicleSeat.Driver);
        var ice = crew.PedFor(CrewSlot.Ice);
        var gohan = crew.PedFor(CrewSlot.Gohan);
        ice.Position = new Vector3(6f, 0f, 0f);
        gohan.Position = new Vector3(0f, 7f, 0f);

        var boarding = new CrewBoarding();
        var seats = CrewBoarding.Passengers(CrewSlot.Guess);
        Check(seats.Length == 2 && seats.All(pair => pair.Key != CrewSlot.Guess),
            "The driver is not one of the men being told to get in beside him");

        bool all = boarding.Update(crew, boat, seats, "TEST");
        Check(!all, "Two men in the water is not everybody aboard");
        Check(ice.Task.Enters == 1 && gohan.Task.Enters == 1,
            "Both of them are actually told to board rather than only counted");

        // Re-tasking a ped restarts the swim he is part way through, so the order is
        // repeated on its own cadence and not every frame.
        boarding.Update(crew, boat, seats, "TEST");
        Check(ice.Task.Enters == 1, "The order is not reissued every frame");
        Game.GameTime += CrewBoarding.OrderIntervalMs + 1;
        boarding.Update(crew, boat, seats, "TEST");
        Check(ice.Task.Enters == 2, "It is repeated on a cadence, so a cleared task recovers");

        // The player boards himself: ordering the active character would take the
        // controls out of his hands mid-swim.
        Use(crew, CrewSlot.Ice);
        int before = ice.Task.Enters;
        Game.GameTime += CrewBoarding.OrderIntervalMs + 1;
        boarding.Update(crew, boat, seats, "TEST");
        Check(ice.Task.Enters == before, "The player is never ordered into his own boat");
        Use(crew, CrewSlot.Guess);

        // A man who is in it is finished with; when both are, the boarding reports done.
        ice.SetIntoVehicle(boat, VehicleSeat.RightFront);
        gohan.SetIntoVehicle(boat, VehicleSeat.LeftRear);
        Check(boarding.Update(crew, boat, seats, "TEST"), "With both aboard the boarding is complete");

        // ---- The patience clock only runs while he is close enough to be boarding.
        // Without that split, the fallback would teleport two swimmers into a boat that
        // was still two hundred meters away, skipping the pickup Ron is driving.
        Reset();
        crew = Roster();
        Use(crew, CrewSlot.Guess);
        boat = new Vehicle { Model = new Model("tropic"), Position = Vector3.Zero };
        crew.PedFor(CrewSlot.Guess).SetIntoVehicle(boat, VehicleSeat.Driver);
        ice = crew.PedFor(CrewSlot.Ice);
        gohan = crew.PedFor(CrewSlot.Gohan);
        ice.Position = new Vector3(300f, 0f, 0f);
        gohan.Position = new Vector3(300f, 5f, 0f);
        var distant = new CrewBoarding();
        for (int i = 0; i < 40; i++) { Game.GameTime += 1000; distant.Update(crew, boat, seats, "TEST"); }
        Check(!ice.IsInVehicle(boat) && distant.Placed == 0,
            "A man still swimming toward the boat is not teleported into it");
        Check(ice.Task.Enters > 1, "He is still ordered while he is on his way — the swim is part of the task");

        // Alongside and still not in after the patience runs out, he is placed, because a
        // boarding step must never be able to strand the player.
        ice.Position = new Vector3(3f, 0f, 0f);
        gohan.Position = new Vector3(0f, 3f, 0f);
        distant.Update(crew, boat, seats, "TEST");
        Check(!ice.IsInVehicle(boat), "Arriving beside it starts his clock rather than seating him at once");
        Game.GameTime += CrewBoarding.DirectAfterMs + 1;
        distant.Update(crew, boat, seats, "TEST");
        Check(ice.IsInVehicle(boat) && gohan.IsInVehicle(boat) && distant.Placed == 2,
            "A man who never gets in on his own is put in, and it is recorded as that");

        // ---- Both unplayed chapters call it, and the run south cannot be finished
        // with a brother left at the roadblock.
        string m47 = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act2", "M47PaletoCollapse.cs"));
        Check(m47.Contains("_pickup.Update(Ctx.Crew, _boat"),
            "M47 tells the swimmers to board instead of waiting to notice that they did");
        string m48 = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act2", "M48TheRoadBackSouth.cs"));
        Check(m48.Contains("_boarding.Update(Ctx.Crew, _technical"),
            "M48 tells the crew to get into the technical");
        Check(m48.Contains("\"Everyone rides south in the technical\""),
            "And the drive south cannot complete with a brother still at the cordon, which OnPassed would have thrown on");

        // ---------------------------------------------------------------- the insertion
        // M45 put Ice in a helicopter at the 40 meter hold marker and then asked him to
        // reach a deck at 15.5: a 24 meter fall onto steel, every time the chapter ran.
        string m45 = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act2", "M45PaletoBreach.cs"));
        Check(M45PaletoBreach.InsertionHeight <= 6f,
            "Ice steps off from a height a man survives");
        float pad = KeyHeight("M45.Helipad"), hold = KeyHeight("M45.Hold");
        Check(hold - pad > 15f, "The hold marker really is far above the deck — the drop was the bug, not a misreading");
        Check(m45.Contains("() => Insertion"),
            "The helicopter is brought over the deck rather than to the circuit marker 50 meters away from it");
        Check(!m45.Contains("new ReachZoneObjective(\"Ice: get out onto the upper deck\""),
            "A zone on the pad is gone: the helicopter over it satisfied that while Ice was still seated");
        Check(m45.Contains("IceOnDeck"), "What the stage waits for is Ice standing on the deck");

        // The deck detail used to be placed with GameUtils.OnGround, and the ground
        // under a point 15 meters up on a vessel is the sea.
        Check(!m45.Contains("GameUtils.OnGround(post)"),
            "The deck guards are not snapped to the water under the hull");

        // ---- A hover holds the spot; a circuit does not, and an insertion needs the spot.
        Reset();
        crew = Roster();
        var chopper = new Vehicle { Model = new Model("annihilator"), Position = new Vector3(0f, 0f, 20f), IsInAir = true };
        guess = crew.PedFor(CrewSlot.Guess);
        guess.SetIntoVehicle(chopper, VehicleSeat.Driver);
        var hold2 = new AircraftHold();
        Use(crew, CrewSlot.Ice);
        hold2.Update(crew, CrewSlot.Guess, chopper, Vector3.Zero, 20, true);
        Check(hold2.Holding && hold2.Hovering, "Asked for a hover, he hovers");
        // A change of pattern is not made to wait for the next cadence: Ice is already
        // on the deck and the helicopter should be climbing away now.
        hold2.Update(crew, CrewSlot.Guess, chopper, Vector3.Zero, 40, false);
        Check(hold2.Holding && !hold2.Hovering, "Switching to the circuit takes effect at once");

        // A helicopter sitting on something is left sitting on it. Ordering a pattern
        // would lift it off a deck somebody is standing on.
        chopper.IsInAir = false;
        Game.GameTime += AircraftHold.OrderIntervalMs + 1;
        hold2.Update(crew, CrewSlot.Guess, chopper, Vector3.Zero, 40, false);
        Check(!hold2.Holding, "A parked aircraft is not ordered into the air — it cannot fall");

        // ---------------------------------------------------------------- the chase
        // Ron lost the Alamo spotters the moment he passed them. A 1.5 meter cylinder is
        // a dot at the range an air chase happens at, and a 60 meter orbit is a bank a
        // jet cannot turn inside.
        // Set by what the aircraft can fly, not by how easy it is to find. A circle needs
        // v^2/r of lateral acceleration, and a crop duster will not hold a bank much past
        // forty degrees: 220 m at cruise is forty-nine, which is how Ron watched one spin
        // into the lake. Finding them belongs to the tracking waypoint instead.
        double lateral = M26AlamoScramble.PatrolSpeed * M26AlamoScramble.PatrolSpeed / M26AlamoScramble.PatrolRadius;
        double bank = Math.Atan(lateral / 9.81) * 180.0 / Math.PI;
        Check(bank < 40.0, "The patrol circle is one a Mammatus can hold without stalling out of it");
        Check(M26AlamoScramble.PatrolSpeed >= 48f, "and it flies it above its stall, not just above zero");
        string m26 = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act2", "M26AlamoScramble.cs"));
        Check(m26.Contains("blip.IsShortRange = false"),
            "Their blips survive the distance the chase is fought over");
        // A Mammatus created at 220 m and told to descend must arrive flying - the same
        // lesson as M27's Shamal, which stalled out of its own patrol.
        Check(M26AlamoScramble.SpotterLaunchSpeed > M26AlamoScramble.PatrolSpeed &&
              M26AlamoScramble.PatrolSpeed >= 35f,
            "The spotters are created with flying speed on them and cruise above a stall");
        Check(m26.Contains("AircraftHold.LaunchAirborne(plane, SpotterLaunchSpeed)"),
            "And they use the same launch path as every other airborne spawn");
        Check(m26.Contains("WatchSpotters();"),
            "A spotter that goes down on its own is logged, not silently scored as a kill");

        // ---- An aircraft the player is flying has a running engine, wherever it came
        // from. The free-roam spawner creates planes and helicopters cold and nothing ever
        // started them, so a jet Ron requested from the dev menu fired its guns and never
        // accelerated - M26's Lazer bug in a second place.
        Reset();
        var roster2 = Roster();
        var jet = new Vehicle { Model = new Model("lazer"), IsEngineRunning = false };
        jet.Model.IsPlane = true;
        Game.Player.Character.SetIntoVehicle(jet, VehicleSeat.Driver);
        AircraftHold.KeepPlayerAircraftRunning();
        Check(jet.IsEngineRunning, "A cold aircraft the player is flying is started");
        // A parked one stays cold: that is what makes M26's scramble read as a scramble.
        var parked = new Vehicle { Model = new Model("lazer"), IsEngineRunning = false };
        parked.Model.IsPlane = true;
        AircraftHold.KeepPlayerAircraftRunning();
        Check(!parked.IsEngineRunning, "An aircraft nobody is sitting in stays cold");
        var car = new Vehicle { Model = new Model("granger"), IsEngineRunning = false };
        Game.Player.Character.SetIntoVehicle(car, VehicleSeat.Driver);
        AircraftHold.KeepPlayerAircraftRunning();
        Check(!car.IsEngineRunning, "And it only touches aircraft, not every car he sits in");
        string entry2 = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "BloodlinesMain.cs"));
        Check(entry2.Contains("AircraftHold.KeepPlayerAircraftRunning"),
            "And the check is actually stepped every frame rather than only existing");

        // A moving target carries a waypoint that follows it. A ground cylinder and a
        // minimap blip are not enough to reacquire an aircraft a kilometer out.
        string std = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Objectives", "StandardObjectives.cs"));
        Check(std.Contains("ObjectiveMarkers.Navigation(vehicle.Position, RequiredCharacter, null,"),
            "A destroy target is routed to, not only drawn on");
        Check(std.Contains("road: !flying"),
            "An airborne target is marked off-road, because a road route points at the water under it");
        Reset();
        var target = new Vehicle { Model = new Model("mammatus"), Position = new Vector3(0f, 0f, 0f) };
        Game.Player.Character.Position = new Vector3(0f, 0f, 0f);
        float near = Bloodlines.Missions.Objectives.DestroyVehicleObjective.MarkerRadius(target);
        Game.Player.Character.Position = new Vector3(600f, 0f, 0f);
        float far = Bloodlines.Missions.Objectives.DestroyVehicleObjective.MarkerRadius(target);
        Check(near <= 2f, "Up close the marker does not cover the thing he is shooting at");
        Check(far >= 20f && far <= 45f, "At the range of an air chase it is big enough to turn back toward, and bounded");

        // ------------------------------------------------------------------ M27
        // The Shamal was told to cruise at 42 m/s and created at 45, both under what a
        // jet stays airborne on. It stalled out of its own patrol and hit the ground
        // within seconds of the mission opening, most times Ron started it.
        Check(M27FlightRisk.ShamalCruise > 55f && M27FlightRisk.ShamalLaunchSpeed >= M27FlightRisk.ShamalCruise,
            "The Shamal is given a cruise it can hold, and arrives with more than that on it");
        // It still has to be catchable: the Vestra was chosen over the Duster for this.
        Check(M27FlightRisk.ShamalCruise < 75f, "And it stays inside what the Vestra can run down");

        // The dive was set once at 20 degrees and 90 m/s with the pilot dead: a jet
        // nobody is flying noses over and keeps accelerating, so it reached the water
        // before Ice could get out of his seat.
        // The dive begins directly above the sea pickup, so whatever it covers
        // horizontally before the bail floor is how far past the boat he lands. A shallow
        // dive bought reaction time and cost 2.9 km of glide; the slow motion buys the time
        // instead. Both halves are checked, because getting this wrong cost a playtest.
        Check(M27FlightRisk.DivePitch < -35f, "The dive is steep, so it comes down near the pickup");
        Check(M27FlightRisk.DriftMeters < 700f,
            "And it lands him inside a glide of the boat rather than kilometers past it");
        float realSeconds = (700f - 80f) / (float)(Math.Abs(Math.Sin(M27FlightRisk.DivePitch * Math.PI / 180.0)) * M27FlightRisk.DiveSpeed);
        Check(realSeconds > 10f, "There is still real time in the fall");
        Check(realSeconds / M27FlightRisk.BailTimeScale > 25f,
            "And the slow motion stretches it past twenty-five seconds as the player feels it");
        string m27 = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "Campaign", "Act2", "M27FlightRisk.cs"));
        Check(m27.Contains("HoldTheDive();") && m27.Contains("protected override void OnUpdate()"),
            "And the attitude is held every frame rather than set once and hoped for");

        // ---- Ron's pickup: the player may drive the boat himself rather than be told to
        // glide to wherever it happens to be. Offered, never forced.
        Check(m27.Contains(".AnyOf()") && !m27.Contains("new EnterVehicleObjective(\"Ice: parachute"),
            "The pickup stage is open to either brother, so switching to Gohan is allowed");
        Check(m27.Contains("() => IceAboard"),
            "and it waits for Ice to be in the boat, not for the player to be");
        Check(m27.Contains("M27_RADIO_02_GOHAN"),
            "Gohan says he will drive over if he is not close enough");
        Check(m27.Contains("Switch to Gohan and drive it to Ice yourself"),
            "and the player is told he may, when the boat is not in view");
        Check(m27.Contains("Hash.IS_SPHERE_VISIBLE"),
            "which is decided by whether the boat is actually on screen, not by a timer");
        Check(m27.Contains("_pickup.Update(Ctx.Crew, _dinghy,"),
            "and when Gohan brings it, Ice is ordered aboard rather than left treading water");

        // ---- A stage names the brother who is actually driving. Gohan is at the wheel of
        // the dinghy, and asking Ice to run it ashore forced a switch back to the passenger
        // the moment Ron took the man steering.
        Check(m27.Contains("new TravelObjective(\"Gohan: bring the boat in under the lighthouse\"") &&
              m27.Contains("Run the boat ashore"),
            "The boat leg belongs to the brother holding the wheel");
        Check(m27.Contains("_ride.Update(Ctx.Crew, _roadCar,"),
            "and Gohan is brought along on the road leg rather than left on the rocks");

        // ---- The bunker's map data is not loaded to draw a blip. Doing that on the first
        // free-roam frame gave Ron a loading screen at every startup.
        string bunkerHome = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "CrewHomes.Bunker.cs"));
        int blipAt = bunkerHome.IndexOf("_bunkerBlip=World.CreateBlip(");
        Check(blipAt > 0 && !bunkerHome.Substring(Math.Max(0, blipAt - 400), 400).Contains("BunkerSite.LoadMaps();"),
            "Creating the bunker blip does not register the DLC maps");
        Check(bunkerHome.Contains("IsWithinFlat(ped.Position,point.Value,BunkerSite.LoadRange))BunkerSite.LoadMaps()"),
            "The load happens when he is walking up to it instead");
        Check(BunkerSite.LoadRange > 60f, "and far enough out that the geometry is there before he arrives");

        // ---- The clock is shared, so a mission beat and Guess's ability cannot undo
        // each other, and the slowest holder is the one that applies.
        SlowMotion.Reset();
        Check(!SlowMotion.Slowed && Math.Abs(SlowMotion.Rate - SlowMotion.Normal) < .001f, "Nothing holds the clock to begin with");
        SlowMotion.Hold(M27FlightRisk.TimeOwner, M27FlightRisk.BailTimeScale);
        Check(Math.Abs(SlowMotion.Rate - M27FlightRisk.BailTimeScale) < .001f, "The bail-out slows time");
        SlowMotion.Hold(Bloodlines.Abilities.SlipstreamReflex.TimeOwner, .3f);
        Check(Math.Abs(SlowMotion.Rate - .3f) < .001f, "An ability asking for slower wins over the mission beat");
        SlowMotion.Release(M27FlightRisk.TimeOwner);
        Check(Math.Abs(SlowMotion.Rate - .3f) < .001f, "The beat ending does not speed the ability back up");
        SlowMotion.Release(Bloodlines.Abilities.SlipstreamReflex.TimeOwner);
        Check(!SlowMotion.Slowed && Math.Abs(SlowMotion.Rate - SlowMotion.Normal) < .001f,
            "Time comes back when the last holder lets go");
        SlowMotion.Hold("Test", .01f);
        Check(SlowMotion.Rate >= SlowMotion.Floor, "Nothing is allowed to slow the game past playable");
        SlowMotion.Reset();
        Check(!SlowMotion.Slowed, "Teardown drops every claim");

        // ------------------------------------------------- the deck where it really is
        // Ron played M45 and found the objective marker floating above the surface he was
        // standing on, so the zone under it never registered however he got aboard. Every
        // vertical number for that vessel came out of the archives, but which level a
        // standing point belongs to was a judgment, and that one was wrong. The geometry
        // knows; ask it rather than pick another number.
        Reset();
        const float realDeck = 12.4f;
        World.RaycastHandler = (from, to) => from.X == to.X && from.Y == to.Y && from.Z > realDeck && to.Z < realDeck
            ? new RaycastResult { DidHit = true, HitPosition = new Vector3(from.X, from.Y, realDeck) }
            : new RaycastResult();
        var authored = new Vector3(-1700f, 5325f, 15.5f);
        var corrected = PaletoSite.OnDeck(authored, "test deck");
        Check(Math.Abs(corrected.Z - realDeck) < .01f, "An authored deck point is moved onto the deck that is there");
        Check(Math.Abs(corrected.X - authored.X) < .01f && Math.Abs(corrected.Y - authored.Y) < .01f,
            "And only its height moves — where to stand on the ship is still Ron's to survey");

        // The probe starts just above the authored point, never at the hull top: a point
        // under the superstructure would otherwise be corrected onto its roof.
        Check(PaletoSite.DeckHeadroom < 4f, "It looks down from just above the point, not from over the whole vessel");
        var roofed = MissionSites.SurfaceHeight(authored, authored.Z + PaletoSite.DeckHeadroom, PaletoSite.WaterlineDeck - 1f);
        Check(roofed.HasValue && roofed.Value < authored.Z, "What it finds is below the authored point, which is where he was standing");

        // Nothing solid at all — a chapter opened alone in QA with no vessel streamed —
        // keeps the authored height rather than dropping the mission into the sea.
        World.RaycastHandler = (from, to) => new RaycastResult();
        var kept = PaletoSite.OnDeck(authored, "test deck with nothing under it");
        Check(Math.Abs(kept.Z - authored.Z) < .01f, "With no geometry to ask, the authored height stands");
        World.RaycastHandler = null;

        // And the chapter hangs everything off the probed deck, not the authored key.
        Check(m45.Contains("_deck = PaletoSite.OnDeck("), "M45 probes its deck before it puts anything on it");
        Check(!m45.Contains("ice.Position.DistanceTo2D(At(\"M45.Helipad\"))"),
            "The step-off check measures to the real deck");
        Check(m45.Contains("Marker = () => _deck"), "And Ice is shown where to land, on the surface rather than above it");
        Check(m47.Contains("PaletoSite.OnDeck(At(\"M47.Trigger\")"),
            "M47's charge rail is measured the same way, since it stands on the same vessel");

        // ---- The heist repairs Ron reported after playing M45 through M48.
        // Four deck guards were spawned and he found one. They were tasked to fight the
        // moment they were created, and a man closing on a target he cannot reach walks
        // off a deck fifteen meters above the sea.
        Check(M45PaletoBreach.DeckGuards >= 10, "The upper deck is held by ten men, because this is a heist");
        Check(!m45.Contains("guard.Task.FightAgainstHatedTargets"),
            "None of them are told to go and find somebody, which is how they went over the side");
        Check(m45.Contains("guard.Task.GuardCurrentPosition();"), "They hold the deck instead");
        // The probe answer is now checked instead of assumed. OnDeck hands back the authored
        // point when it finds nothing, and Ron found the consequence: four of the ten kept an
        // unverified height and one ended up inside the hull where he could not be shot.
        Check(m45.Contains("MissionSites.SurfaceHeight(tried, tried.Z + PaletoSite.DeckHeadroom"),
            "Each post asks whether there is any deck under it");
        Check(m45.Contains("for (int back = 0; back <= PostRetries && !deck.HasValue; back++)"),
            "and a refused post steps back toward the pad looking for deck that exists");
        Check(m45.Contains("post = new Vector3(post.X, post.Y, pad.Z);"),
            "falling back to the height the pad probe measured rather than the authored guess");
        Check(M45PaletoBreach.PadClearance >= 15f,
            "The nearest rank stands clear of the landing zone, not under the rotors");
        // Ten men who cannot react are not a fight. M48 clears this flag in WakeCordon and
        // M45 never did, which is why Ron was not being attacked.
        Check(m45.Contains("guard.BlockPermanentEvents = false;") && m45.Contains("private void WakeDeck()"),
            "The deck detail becomes reactive when the fight starts");
        Check(m45.Contains("if (Game.GameTime < _deckOrderAt) return;") && m45.Contains("guard.Task.FightAgainst(target);"),
            "and is kept shooting on a cadence rather than re-tasked every frame");
        // A stage exit that throws is a script error, not a failed mission: it ended a
        // five-chapter sitting. The condition belongs to the objective.
        Check(!m45.Contains("Gohan is not out of the water and on the structure"),
            "Nothing throws out of the boarding stage any more");
        Check(m45.Contains("private bool OnTheStructure()") && m45.Contains("gohan.IsInVehicle()) return false;"),
            "Boarding asks for a man out of the boat and above the waterline up front");

        // ---- Nothing slow may run between creating an airborne aircraft and the first
        // frame that flies it. Ten waiting deck probes stalled Setup for up to ten seconds
        // after the helicopter was created, and it flew itself into the sea — the exact
        // failure PR #64 fixed, reintroduced by raising the guard count.
        // Still true and now structural: MissionSites.SurfaceHeight is one shape test with no
        // retry loop and no Script.Wait in it at all, where OnSurface waits between attempts.
        // The per-guard probe uses the one that cannot wait.
        string siteSource = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "MissionSites.cs"));
        int heightAt = siteSource.IndexOf("public static float? SurfaceHeight", StringComparison.Ordinal);
        int nextAt = siteSource.IndexOf("public static", heightAt + 20, StringComparison.Ordinal);
        string heightBody = nextAt > heightAt ? siteSource.Substring(heightAt, nextAt - heightAt) : siteSource.Substring(heightAt);
        Check(!heightBody.Contains("Script.Wait("),
            "The repeated deck probes do not wait for collision the first one already paid for");
        Check(!m45.Contains("PaletoSite.OnDeck(post"),
            "and none of the ten goes through the waiting probe the helipad uses");
        int guardsAt = m45.IndexOf("SpawnDeckGuards();");
        int heliAt = m45.IndexOf("if (!SpawnHelicopter()) return false;");
        Check(guardsAt > 0 && heliAt > guardsAt,
            "and the aircraft is created after the slow work, not before it");

        // An engine running is not a rotor turning.
        string holdSrc = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "AircraftHold.cs"));
        // Checked on the ignition path specifically, not anywhere in the file: LaunchAirborne
        // has always spun blades, and the bug was the player-takeover path not doing it.
        int ignitionAt = holdSrc.IndexOf("KeepPlayerAircraftRunning");
        int launchAt = holdSrc.IndexOf("public static void LaunchAirborne");
        Check(ignitionAt > 0 && launchAt > ignitionAt &&
              holdSrc.Substring(ignitionAt, launchAt - ignitionAt).Contains("SET_HELI_BLADES_FULL_SPEED"),
            "A helicopter the player takes over cold gets its blades spun up, not just its engine");

        // M48's cordon opened fire 77 m from where the crew loads in, which killed Guess
        // at the wheel before the player had control.
        Check(!m48.Contains("guard.Task.FightAgainstHatedTargets"),
            "The cordon does not engage a crew that has only just arrived");
        Check(m48.Contains("guard.Task.GuardCurrentPosition();"), "It holds the roadblock until they drive into it");
        // Holding position stopped them walking into the sea in M45. It does not stop them
        // shooting: BLOODLINES_AEGIS hates the crew for the whole game, so four riflemen
        // with line of sight to a stationary driver 77 m away still killed Guess at the
        // wheel. Until the crew drives into the roadblock they stand in a group that hates
        // nobody.
        Check(m48.Contains("guard.RelationshipGroup = holding;"),
            "The cordon does not start in a group that hates the crew");
        Check(m48.Contains("private void WakeCordon()") && m48.Contains(".OnEnter(c => WakeCordon())"),
            "and it becomes Aegis on the stage where the fight belongs");
        Check(M48TheRoadBackSouth.HoldingGroup != "BLOODLINES_AEGIS",
            "which is a different group from the one the roster made hostile");

        // Opened needing its own boat, M48 used to deploy nobody: the deploy was the third
        // branch of an else-if chain about the boat, so staging one skipped it. Guess had no
        // ped, and ComposedMission reports a missing brother with the same words it uses for
        // a dead one — which is why this read as the cordon killing him.
        Check(!m48.Contains("else if (!Ctx.Crew.Deploy"),
            "Deploying the crew is not an alternative to staging a boat");
        Check(m48.Contains("if (!Paleto.IsContinuing(Ctx) &&") &&
              m48.Contains("!Ctx.Crew.Deploy(CrewSlot.Guess, At(\"M48.Technical\")"),
            "A chapter that is not continuing deploys the crew whatever it has to stage");
        // The same shape must not come back anywhere else.
        foreach (var file in ScriptFiles())
            Check(!File.ReadAllText(file).Contains("else if (!Ctx.Crew.Deploy"),
                Path.GetFileName(file) + " does not make a crew deploy the alternative to something else");

        // A throw out of Setup is the whole operation refusing to start, which is what Ron
        // got after arming the charges, five chapters in.
        Check(m47.Contains("try { return MarineSites.ResolveOrThrow(Ctx.Locations, key, 2f); }"),
            "M47 catches the water probe instead of letting it throw the sitting away");
        Check(m47.Contains("Afloat(key) ?? Afloat("), "It falls back to clear water and says so");

        // A brother already aboard walks to his post. Setting his position was read as a
        // teleport: Gohan climbed onto the vessel and then vanished inside.
        string composed = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Missions", "ComposedMission.cs"));
        Check(composed.Contains("bool walk = Ctx.Operation != null"),
            "Inside a live operation a station is walked to, not teleported to");
        Check(composed.Contains("WalkToStationMeters"), "And only when he is already close enough for the walk to be short");

        // A point Ron surveyed on foot is walkable by demonstration. Refusing the mission
        // when the navmesh disagrees is the tool overruling the survey, and it stopped SM06.
        string sites = File.ReadAllText(Path.Combine(Repo, "src", "Bloodlines", "Core", "MissionSites.cs"));
        Check(sites.Contains("if (surveyed)") && sites.Contains("Kept.Add(key);"),
            "A surveyed point is kept rather than refused, and recorded as kept");
        Check(sites.Contains("WasKeptOnTrust"),
            "so the location test and the doctor still hear about it");
    }

    /// <summary>A location key's authored height, straight out of the book on disk.</summary>
    static float KeyHeight(string key)
    {
        foreach (var line in File.ReadAllLines(Path.Combine(dataDir, "locations.tsv")))
        {
            var columns = line.Split('	');
            if (columns.Length > 3 && columns[0] == key)
                return float.Parse(columns[3], System.Globalization.CultureInfo.InvariantCulture);
        }
        throw new Exception(key + " is not in the location book.");
    }
}
