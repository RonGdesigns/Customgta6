# BM01: Clipped Wings (bonus mission)

Ron's design, September 22, 2026. It is the first bonus mission, authored after the
campaign and offered once the rest of it has been played. Nothing here has been played yet.

## What happens

1. **Up the coast.** Guess drives the crew north up the coast highway in a Karin Technical,
   a pickup truck with a gun in the bed. Gohan rides beside him. Ice is in the back on the gun.
2. **The gate.** At the Paleto Forest bunker, eight Aegis men hold the gate, the guard post
   and the yard. The Osprey sits parked on the apron where they can be seen guarding it. The
   pilot is not one of the eight.
3. **The takeoff.** When the last man falls, a cutscene shows the pilot run for the Osprey
   and climb in. Watched or skipped, it ends the same way. Gameplay resumes with the Osprey
   already off the ground, hovering over the apron while the crew gets back in the truck.
4. **Bring it down.** With all three aboard, the Osprey flies the coast road east, through
   Paleto Bay and along the north coast. The HUD shows a **hull meter** under the objective.
   Guess keeps the truck under it and Ice works the gun. If you play Guess, Ice fires the gun
   on his own and Gohan shoots from his window. When the meter empties it blows up and falls.
5. **The payoff.** $150,000 on the first pass, and the Osprey goes up for sale.

It fails if the Osprey reaches the end of the coast road, or if all three brothers are more
than 450 m from it for 15 seconds. You get a warning in the feed first.

## How it is built, and why

* **The Osprey is the Mammoth Avenger (`avenger`)**, a tilt-rotor in the installed game.
* **It is not flown by the AI.** `Core/ScriptedFlight` turns its gravity off and moves it
  from road point to road point, 32 m up, turned to face its heading with a little bank. AI
  pilots have already cost this campaign aircraft: M26's jet never started, and nobody flew
  M27's Shamal. A scripted line always follows the road, as Ron asked.
* **It paces itself to the truck.** It tries to stay about 110 m ahead, measured along the
  route. It speeds up when the truck closes and slows when the truck drops back, between
  12 and 40 m/s. A truck that keeps up always has a shot.
* **The hull meter counts hits, not damage.** The Avenger's own armor comes from an Online
  vehicle and is an unknown number, so `ShootDownObjective` does not rely on it.
  * Each frame the crew's gunfire lands takes 0.6%, roughly 170 hits to empty the meter.
  * A rocket or grenade takes 15%, so seven of them.
  * Until the meter is empty, the aircraft is topped back up each frame and cannot be killed
    by a single explosion.
  * Tuning is two constants in one file.
* **Ice's launcher.** GTA does not let a rocket launcher be fired from a vehicle seat. In the
  back of the truck Ice works the mounted gun, and the launcher (10 rockets) is his whenever
  he is on his feet.
* **The site is read from the archives.** The Paleto Forest bunker is `gr_case7_bunkerclosed`:
  * door at (-782.5, 5935.1, 22.0), apron at (-774.7, 5937.1, 19.9);
  * road signs on the coast road at (-690, 5902) and (-678, 5995).
  * The exterior is loaded for the attempt through `ScriptedMap` and handed back only if this
    mission turned it on. The bunker interior is not used.
* **The flight line is a pole line.** The telegraph poles beside the highway from Paleto Bay
  to the north coast were chained out of the archives. The poles run 1.5 km from the bunker
  to Paleto Bay, then 2.3 km along the coast to (1502, 6492). There are seventeen points plus
  `BM01.Getaway`. `RaceRoute` snaps each to the nearest lane when the chase starts, and
  refuses any snap that would step backward.
* **Every BM01 key is an estimate.** The start, the truck, the approach, the gate, the eight
  guard posts, the Osprey's pad, the pilot and the flight line all need a survey pass. The
  start is on the coast highway about 850 m south-west of the bunker. Move `BM01.Start` and
  `BM01.Truck` further south for a longer drive.

## Placement after Ron's first run (September 22)

Ron reported one guard spawned in the bunker and the Osprey floating. Both came from the same
cause: `Setup` runs with the crew about 800 m south, before any collision around the bunker
has streamed. Every guard post logged "no navmesh" and was stood at its authored height, and
the Osprey's ground call found nothing and was frozen where it was.

* **The guard in the bunker was `BM01.Yard1`.** The placed entrance model,
  `gr_prop_gr_bunkeddoor_f`, carries a collision box that runs along the entrance ramp from
  10 m behind the door to 28 m in front of it, at heading 17.4. The old point (-760, 5944, 19.2)
  sat inside it, 0.7 m under the ramp's top at 19.89. It now stands north of the ramp at
  (-764, 5952, 19.0), on terrain the archives put at 18.99, 5.6 m from the bunker signs. No
  other BM01 key is within 1.5 m of the ramp; a story check holds that.
* **The Osprey's height is measured.** The terrain collision under `BM01.Osprey` is 16.72 in
  `cs1_08_32.ybn`, so the key is 16.7 rather than 18.0. The aircraft is held with its gear on
  that height, using the model's own dimensions, instead of 2 m above an estimate.
* **It is set down once the ground exists.** Collision is streamed around it
  (`SET_ENTITY_LOAD_COLLISION_FLAG`). When `HAS_COLLISION_LOADED_AROUND_ENTITY` answers, the
  ground call runs and its result is checked: a downward probe must find the surface, and the
  model's lowest point must be within 1.5 m of it. Only then is it parked and frozen for the
  beat. A probe that finds nothing is not a pass. If the player is within 150 m for 4 seconds
  and it still has not verified, it is parked at the measured height and the log says so. The
  takeoff scene forces that decision before it shows the aircraft, and the lift rises from
  wherever it was parked.
* **Each guard post is checked again once the ground around him has loaded.** A man below the
  surface under his post, or more than 2.5 m above it, is stood on it. A post with anything
  within 2.5 m over it is walked outward in rings of 3, 6 and 9 m to open sky, with the gate
  road as the last resort. A post nothing answers under is left alone and reported. A man who
  has already moved 3 m from his post is never teleported.

`tests/story/BM01PlacementTests.cs` drives these paths. None of it has been seen in game.

## When it opens

* **Prerequisite:** M70.
* **Story gate:** SM01 to SM09, every solo job (`CampaignState.StoryGates`).
* **Where it is defined:** `data/bonus_missions.tsv`, an authored file in the same columns as
  `missions.tsv`. The bible extraction is never edited.
* **In the menus:** it comes after everything else in the catalog, under the act name "Bonus
  mission".
* **Scenes:** its briefing, takeoff and aftermath lines are in `data/story_beats.txt`.

## Buying the Osprey

After BM01, the phone's **Garage** page lists it at **$500,000**, the top of the price
ladder. After buying it, **Call the Osprey** sets it down on the nearest open ground around
you, within 35 to 80 m. It needs about 14 m of room and dry land. If there is none, it
refuses rather than dropping it into trees or water. Calling again moves it to you, unless
somebody is aboard. There is no hangar yet, so it keeps no customization between calls.
Ownership is saved.

## Checks

`tests/story/BonusOspreyTests.cs` covers:

* the flight's pacing, route stepping and release to physics;
* the hull meter (gunfire, rockets, keep-alive, one explosion, a missing target);
* the whole flow: seats, loadout, guards, the takeoff waiting for its scene, the lift, the
  chase, the gunner on a slow clock, the pass and the reward;
* both failures, and bunker-map ownership;
* the purchase, reload and delivery;
* the catalog and data wiring.

The campaign flow walker also drives BM01 from start to pass. These are stand-ins; none of
it has been played.
