# Handoff implementation pass — September 9, 2026

Branch `claude/handoff-implementation-pass`. This implements the approved items
from `AGENT-HANDOFF-PROPOSED-CHANGES.md` and the priority list in
`CAMPAIGN-IMPLEMENTATION-AUDIT.md` §19, in that order, as far as they can be
built and checked without the game running. The per-item reconciliation is in
`CHANGE-REGISTER.md`. Nothing here has been live-verified.

## What changed

### Campaign state (audit priority 2)

- `CampaignState.Progress()` reports one of five states: next story mission, story gated on solo jobs, optional side content only, story blocked (an unmet prerequisite or required content this build lacks), or all implemented content complete. The mission key and the save menu use it. The end of the scripted campaign is reported as such; M01 is never a fallback.
- Failure retry: the mission key retries the failed job and says so; walking to another marker still chooses that job.
- `CampaignState.PrologueComplete` (saved) with `PrologueDue` for saves that have not finished M01.
- SM01's armor-piercing crates are a real supply line: `armorPiercingSupply` doubles Ice's rifle restock at any locker.
- SM03's racing transmission is consumed: cars and motorcycles repaired at Guess's chop bay receive the race transmission mod once.
- Mission-issued weapons are loans: capture is suspended while a mission runs and the loan is returned at teardown (see the correction pass below).
- Before M01 is complete, the crew key deploys Guess alone. `[Dev] Enabled = True` keeps the full sandbox.

### Failure reliability (priority 3)

- `Mission.RequireAsset(entity, reason)` fails the attempt the moment a required vehicle or prop is destroyed, undriveable or deleted, for the whole mission rather than only the stages that happened to carry a protect objective. Applied to the Port Heist vehicles and cargo and to M24's crane truck.

### Chapter handoff (priority 4)

- `OperationHandoff` records active hero, every hero position, vehicle model/position/heading/health, seats, cargo attachment and notes. `HandoffLedger` on `MissionContext` stores one record per operation; a record is addressed to one receiving mission and consumed when taken, so a retry rebuilds default staging.
- M19 → M20: Gohan starts in the surfaced Kraken instead of on the apron.
- M20 → M21: the escorted Cargobob starts where the climb-out ended and **always** carries a visible attached container, record or not.
- M21 → M22: the loaded lift is handed to the Alamo drop.
- Records are session memory, not save data. After a restart the next chapter logs that it started from default staging.

### Cinematic foundation (priority 5)

- `SceneBlocking` with `WalkToStep`, `EnterVehicleStep`, `ExitVehicleStep`, `UsePhoneStep`, `LookAtStep`, `WaitStep`. Each step has a completion signal, a timeout, and `Finish()` for the instant end state.
- `CutsceneDirector.Play(..., blocking)`: movers stay unfrozen, the camera tracks the running step's subject, the scene holds until dialogue and blocking are both done, and a deliberate skip completes the remaining steps while abort, error, watchdog and teardown cancel them (see the correction pass below). Watching and skipping produce the same seats, positions and control state.
- The prologue: LSIA arrival scene (phone, look, walk, enter car), player drive to the starter apartment entrance, homecoming scene (exit, walk to the door, read the job), fade, time-cut to Terminal Island, M01 cold open. Hold Backspace skips it and still marks it played. Missing locations or a failed car spawn fall back to starting M01 directly.
- New scene data: `M01:prologue` and `M01:arrival`, authored in `data/opening_scene.txt`, compiled by `tools/build_story.py` (which now requires both blocks and enforces that only Guess speaks in them).
- New locations: `Prologue.LSIACurb`, `Prologue.LSIACar` (estimates, airport zone validated).

### Dialogue and relationship pass (priority 6)

- Locked wording: M10, M25, M42. M17 loses "marked yellow". M22 explains why the strike happened and why it cannot simply repeat. M26 says where the Lazer came from. M45 is conversational.
- HUD = mechanics, dialogue = people for M02–M07, M10, M14, M16, M27, M30, SM02, SM03 through `data/dialogue_edits.json`. A story test fails if any of those missions' spoken lines mention switching, markers, button names, radii or "no ability needed".
- M05 reveals an allegation and a lead, not the conspiracy. M46 remains the hard-evidence payoff.
- SM05, SM06, SM07, SM08 aftermaths use the proposal's short forms; SM08 is about communicating, not belonging.
- M70 ends on the quiet boat: names in words, Gohan already on a device, the phone joke, breakfast.
- Dispatch vocabulary: "rattled", not "scared".

### Character gameplay (priority 7)

- `TechnicalChoiceObjective`: a reusable Gohan decision at a panel. G / D-pad Left cycles, E / D-pad Right commits, and each option applies a mission-supplied consequence. M28 uses it: cutting the shared feed first brings full squads later, cutting dispatch first brings smaller squads sooner, cutting the cameras first keeps the standard response. `SurviveWavesObjective.GapProvider` lets a wave clock read the decision after it is made.

## What was verified (source level)

```
python tools/lint_missions.py          No errors. 35 of 36 missions fire every written line.
python tools/validate_locations.py     0 of 161 flagged
python tools/build_story.py --check    79 missions, 352 unique cues, 161 scenes; freshness verified
python tools/audit_campaign.py --check Mission map freshness verified
python tools/run_story_tests.py        725 story/runtime checks passed (after the correction pass and final patch)
python tools/run_regression_tests.py   165 checks passed
python tools/build_roslyn.py           Bloodlines.dll built with warnings as errors; prebuilt/ refreshed
```

Correction-pass checks (`tests/story/CorrectionPassTests.cs`) cover: the four
gates, story-first ordering, gate refusal text, QA bypass, partial progress,
grandfathering by gate mission and by a later mission, and an unscripted required
solo; the full weapon-loan lifecycle across pass, rewarding pass, abort, fail and
starting loadouts, followed by free-roam capture ticks and a restock; the technical
choice's arrival frame and disabled-control cycling; skip vs cancel vs error vs
mission abort vs watchdog vs natural completion; deterministic exit/walk/phone/entry
finishes and cancel; M21's held lift, its release on the escort stage, and its
release on abort and failure; and the prologue, dispatch and M70 data.

New story checks (`tests/story/HandoffPassTests.cs`) cover: progress states and the
prologue flag; the weapon loan policy and Ice's supply line; required-asset
failure; ledger record/peek/take; M20 → M21 → M22 continuity through the real
mission classes, including the cold-start container; scene blocking watched vs
skipped, timeout, dead-player guard, and director integration; the prologue from
airport to handoff, skip, and missing-home refusal; the technical choice and its
M28 wiring; the chop-bay transmission; and the locked wording and HUD-free
dialogue in the generated data.

None of this proves live terrain, animation playback, task behavior, camera
clipping or native stability.

## Live-test list (in order)

1. **Prologue, watched.** Fresh save, press the mission key. Ron at the LSIA curb; phone, look at the car, walk, door, seated. Control returns seated. Drive to the apartment marker. Stop. Exit, walk, phone. Fade, "Terminal Island. Later that night.", M01 cold open. Check `Bloodlines.log` for `Prologue started` and `Prologue complete`.
2. **Prologue, skipped.** Same start; Enter during the arrival. Ron must be in the driver's seat with control. Skip the homecoming too; Ron must be out of the car at the door. Hold Backspace mid-drive: M01 must start and the save must show `prologueComplete: true`.
3. **Prologue failure paths.** Wreck the Primo mid-drive: "Any ride home will do", arrival by any vehicle or on foot. Die mid-drive: solo recovery, then the drive resumes.
4. **Pre-reunion deploy.** Fresh save, crew key: only Guess. Switch keys refused. After M01: full crew.
5. **End of content.** Mark everything complete from the dev menu, then press the mission key: the completion message, not M01.
6. **Retry.** Fail SM01, press the mission key away from any marker: SM01 restarts with the retry notice.
7. **Port Heist continuity.** Play M19 → M20 → M21 → M22 in one session. M20: Gohan starts in the Kraken on the surface. M21: container visibly under the Cargobob from the first frame, Cargobob starts near M20's climb-out point, escort AI still flies to the breakwater with the extra attached mass. M22: log shows the record consumed. Then start M21 cold (fresh session): container still present.
8. **Required assets.** In M21 destroy the launch during "Draw the locks": immediate failure with the launch reason. In M24 wreck the crane before the shallows: immediate failure.
9. **M28 choice.** Reach the cabinet as Gohan; G cycles, E commits; verify response timing and squad size follow the choice.
10. **SM01 / SM03 rewards.** Finish SM01, restock at Ice's locker: double rifle ammo. Finish SM03, repair a car at Guess's chop bay: race transmission notification and the mod on the vehicle.
11. **Weapon loans.** Start M20 (issues an MG), abort, check the save's `weaponLockers`: no MG for Guess unless he owned it before.
12. **Dialogue.** Read M02–M06 in play; nothing spoken should tell you which button to press.

## Pre-merge correction pass (same day)

Brought the branch to a merge-ready source-level state. Each item, and what changed:

1. **Solo story gates** — central in `CampaignState.StoryGates` (M19 ← SM01–SM03, M44 ← SM04–SM06, M63 ← SM07–SM08, M68 ← SM09). Story first: a newly unlocked solo is never "next" by catalog order; it becomes next only when a gate is waiting on it. A gated start is refused with the list of remaining jobs; the dev menu bypasses explicitly. **Migration:** a save that already completed the gate mission, or any later main mission, is grandfathered through that gate and keeps the solos as optional content. A required solo with no script never blocks.
2. **Weapon loans** — a real lifecycle. The locker is snapshotted at mission start; at teardown (pass after completion commits, fail, abort, failed start, shutdown) anything on a hero outside baseline ∪ standard loadout ∪ earned milestone rewards is removed from the hero and the locker. Tested through a full mission, six free-roam capture ticks and a locker restock.
3. **`TechnicalChoiceObjective`** — the arrival frame consumes both buttons and returns; Detonate is disabled while the panel is open and read as a disabled control, so cycling cannot throw a detonator.
4. **Skip vs cancel** — `CutsceneDirector.Skip()` completes unplayed blocking; `Stop()` cancels it. Enter / controller A skip; abort, error, watchdog, death and teardown cancel.
5. **`SceneStep.Finish`** — deterministic end states; exit-vehicle warps out onto a navmesh-checked spot beside the car. `Cancel` clears the running task and moves nobody.
6. **M21** — the loaded lift is held (frozen, rotors up) until the escort stage begins; cleanup and failure release it. Cold start still shows bullion.
7. **OperationHandoff** — documentation now states exactly which fields each receiver restores.
8. **Prologue** — Ron tries both numbers, gets voicemail; consistent with M01.
9. **M27 dispatch** — Ice speaks as the one extracted; other dispatches audited.
10. **Later dialogue** — M53, M55, M57, M64 and three M70 gameplay lines tightened; M67, M68 and the remaining M31–M70 character pass are marked deferred in `CHANGE-REGISTER.md`.
11. **HUD leaks** — M08, M09, M12 gameplay lines and M03/M04/M06 briefings fixed; the speech check now covers every scripted mission's gameplay and scene lines.
12. **Repository** — `tools/__pycache__` untracked; `__pycache__/` and `*.pyc` ignored.

Final pre-merge patch (same day): the locked M70 final line "Told y'all... Guess never misses an exit." is the last spoken line of the campaign, after the boat exchange; an unscripted required solo now blocks the story and names itself rather than being waived; the prologue cold-open hand-off and `ExitVehicleStep.Finish` share a fail-safe `ForceOut` and a failed exit cancels the rest of a skip; the progress-state comment says five.

Additional live-test items from this pass:

13. **Story gates.** Finish M18 with SM01–SM03 unfinished: the marker for M19 shows, starting it reports the three jobs, Gohan's lead routes to SM01. Finish them; M19 starts. Load a save already past M19: no gate message.
14. **Loans.** Start M20 (issues an MG to Guess), abort, look at Guess's weapon wheel and `weaponLockers` in the save: no MG. Finish M03 as normal: the M03 reward rifles are in the lockers, the mission's temporary weapons are not.
15. **M28 panel.** Carry sticky bombs to the cabinet; cycling with G must not detonate them.
16. **Skip vs cancel.** Enter during the prologue arrival: Ron seated. Hold Backspace during it: Ron seated (skip), M01 starts. Die during the homecoming scene: no warp, recovery runs, the drive resumes.
17. **M21 hold.** The Cargobob hangs in place with rotors turning until Gohan is in the launch; then it flies. Watch the release for a drop or a snap.

## Post-merge fix branch

Addresses the verified findings in `POST-MERGE-AUDIT-VERIFICATION.md`: N1 (template overrides), R01/R03/R04 (host order and loan closure), R02 (scene-outcome contract), N2 (QA keys), R05/R06 (M28 option and map generator), R10/N3 (stale instructions) and R07 (CI `verify` job on Windows). Details per item are in `CHANGE-REGISTER.md`.

Live-test additions:

18. **Stale overrides.** Launch and read `Bloodlines.log`: one "stale template override" line per pre-September-9 key, then "157 still estimates". M01's lookout is now a short walk from Ice's start.
19. **Prologue GPS.** After the arrival, the map draws a route to the apartment, not just the cylinder.
20. **Refused start.** With SM01–SM03 unfinished, press J at M19's marker with the crew deployed: the crew stays, the gate message shows.
21. **Homecoming failure.** Die during the homecoming scene: after recovery the drive resumes at the door, the next stop places Ron at the door directly, then M01's cold open. No double hand-off.
22. **M28 cameras.** Choose "Yard cameras first": responders should walk in on guard and open fire only on sight.

## Packages A and B (playthrough repair)

From `PLAYTHROUGH-REPAIR-PLAN.md`, on Ron's direction: steering and weight before speed, the speed target unchanged, Guess's ability Franklin-style. Package A: briefing cast, control diagnostics, marker ids, QA objective completion, top-right readout. Package B: per-class road handling baseline, the Guess press-and-grip overlay, aircraft diagnostics and helicopter cruise assist, the calibration sheet in `HANDLING-CALIBRATION.md`. Details per item are in `CHANGE-REGISTER.md`. Packages C (M01, M02, police) and D (M03, M04, apartment call) are not started.

Live-test additions:

23. **Briefing cast.** Start M03 or M04 before deploying the crew (dev tools off): the two speakers stand in front of you facing the start point; Franklin is not in the shot; after the scene you are visible and in control. Deploy the crew, stand far from a brother and start a mission: still a radio call.
24. **Control lines.** After any scene and at every mission start, `Bloodlines.log` has a `CONTROL [...]` line. If control is missing, quote that line in the notes.
25. **Readout.** Dev tools on: coordinates and heading sit at the top right and stay visible under objective text; in a car the line adds km/h and mph. Capture P18's Y and Ice's checkpoint Y with it.
26. **Complete objective.** Dev menu → "Running mission" → "Complete current objective" during M02: the objective passes, the stage advances, the next objective's setup runs.
27. **Road loop, handling first.** Run the loop in `HANDLING-CALIBRATION.md` for the Primo, Schafter, Granger and Benson, ability off, and fill in the sheet. This is the decision on the speed target; nothing here pulls it down.
28. **Guess press.** Same loop, ability on for the two freeway curves; end the ability mid-corner once: no drop. Jump the Del Perro ramp with it on: the car still leaves the ground.
29. **Buzzard pair.** Stock versus this build, same altitude and route, both directions; note the `flight handling:` line from the log.
30. **Stand-down.** `F10` off after a loop: the same car drives stock again (grip, brakes, gearing) and `Bloodlines.log` shows no restore errors.

## Round two (September 10 playthrough)

From Ron's notes on the September 10 session: M04's chase and pickup, the M04 briefing arrival, the apartment interior, weapon unlocks through missions, M05 refusing to start, M06 SWAT arriving by helicopter, and the debug menu's mission order. Details per item are in `CHANGE-REGISTER.md`.

Live-test additions:

31. **Arrival.** Crew not deployed, start M04 at its marker: Guess stands where you stood, Ice and Gohan pull up in the four-door from behind you, the first line waits for the car, and Enter during the drive lands the car at the curb. Try the same at M03. On a start point with no street nearby they stand on foot instead.
32. **Chase.** In M04, when Miller pulls out he should run: through traffic, wrong way if it helps, not stopping at lights. Note whether the red marker gets away past the lose distance in a stock Buffalo.
33. **Pickup.** Get out and collect the drive: Guess turns to the car and reaches in for the three seconds. Afterward the Buffalo and Miller's Fugitive are both still on the street.
34. **M05.** Press J at the M05 marker: the mission starts. If it refuses, quote the "No walkable mission surface" line; it now carries the probed point.
35. **Air waves.** In M06 hold the alley through wave one; wave two brings two Mavericks over the roofs, a short shot on the first pilot, four troopers on ropes and one by the street; wave three the same without the shot. If an aircraft hangs, the troopers are placed after 20 s and the wave still ends.
36. **Apartment.** Enter the starter apartment: black screen, then inside with control. If it still times out, quote the "Apartment:" lines; they now say which check was waiting while you were already inside.
37. **Rewards.** Finish M02: Ice, Gohan and Guess each get a new weapon at the next locker restock, and the shop shows it locked before the job and unlocked after.
38. **QA order.** Dev menu → Missions → any job: it starts, with a yellow QA note about the unfinished prerequisite.

## Visuals and vehicle damage (reviewed hand-over)

The visuals and deformation work from the other session, rebased onto the merged main and corrected per the review in `CHANGE-REGISTER.md`. Costs and keys are in `VISUALS.md`.

Live-test additions:

39. **Grade by time.** Free roam at 13:00, 18:00 and 23:00: the look changes at each band and `Bloodlines.log` prints the modifier name. If a band looks vanilla, that name is not in the game's timecycle data; verify it as `VISUALS.md` describes.
40. **Stand-down.** `F10` off, then Insert: no lingering grade, shadow reach, swell or reflection flag. The world should look vanilla.
41. **Streaming at speed.** Del Perro Freeway at top speed with `LODBoost` on, then off. Note stutter and any mission start where collision loaded late.
42. **Crew protection.** Get into a car, get out, get back in: the log's damage-scale lines follow you, and after stand-down a crash in the story character's car hurts as much as vanilla.
43. **Ini safety.** Copy `build\deploy\scripts` over the game's `scripts` folder by hand: your `Bloodlines.ini` is unchanged and a `.example` sits beside it.

## M01 after the prologue (September 10 live failure)

The 01:22 log: the hand-off placed Ron at the dock, then M01's placement check refused every start on `M01.ExitPoint`, whose data row pointed 838 m away. The row is corrected and the exit is no longer validated at the cold open. Details in `CHANGE-REGISTER.md`.

44. **Hand-off.** Fresh save, J, watch or skip both scenes: after the fade, "Terminal Island. Later that night." and the M01 cold open. If it refuses, quote the "M01 could not find" line; the key it names is the one to survey.

## Port Heist (September 10 live note)

M19's dive had no vessel and markers Gohan could not reach; the four chapters started one at a time. The site is now built from a hull and the chapters continue into one another. Details in `CHANGE-REGISTER.md`.

45. **The dive.** Start M19: a tug sits on the water with a "Titan Star" blip; the breach marker is under its keel and the two clamp marks are under its bow and stern; the Kraken fits under it. If the log says the site moved, note by how much.
46. **One operation.** Pass M19: after the aftermath lines, M20 starts on its own with "The operation continues"; the same Kraken is floating where Gohan surfaced. Fail M20: the mission key retries M20 alone.

## Crew van and the prologue car (September 10)

47. **Arrival car.** Fresh save, J: the Primo is on the street at the LSIA curb, not on the terminal roof. If it is still up there, quote the log's "No walkable mission surface" or the coordinates from the readout; the curb keys are `Prologue.LSIACurb` and `Prologue.LSIACar`.
48. **The van.** Deploy the crew near the Cypress base: a Granger with a blue "Crew van" blip sits at the stash. Drive it to a mod shop, fit a spoiler and paint it, get out, stand down, redeploy: the same spoiler and paint. Start M02: the chase car is that van.
49. **Upgrades.** After M11, get into the van: the fleet package applies as before; reinforce the tires at the chop bay; they stay reinforced on the next spawn.

## Story to play, first slice (P2a blocks and the M04 reference)

Built from `STORY-TO-PLAY-PLAN.md` with Ron's decisions and the marked defaults recorded in `CHANGE-REGISTER.md`. The M04 acceptance test is the proposal's own: a tester who has not read either book explains why Miller is at the lot, why Ron is in a separate car, why Gohan cuts power, why Miller flees, what Ice does during the pursuit, what object was recovered, and why Mateo is next.

50. **M04 from the base.** Start M04 at the Cypress base marker with the crew deployed: the briefing plays, then the three of you are in the crew van with Guess driving. Two radio calls on the way (Gohan at 60%, Ice at 30%). Stop in the yellow zone at the lot exit: Ice and Gohan get out and walk to their positions while you hold.
51. **The exchange.** Once they are in place the scene plays: wide on the lot, over Miller's shoulder at the buyer with the case in Miller's hand, the buyer looking it over, the van at the exit, Ice's angle. Enter skips it. After it you are asked to switch to Gohan for the breaker.
52. **The breaker and the flight.** Cut the breaker: the buyer runs, the escort turns hostile, Miller grabs the case and goes for his car, a short shot on him pulling out, and "Miller is running. Take Guess" with an eight-second count while Ron's van is already chasing. Let it run out once: the switch is then required, nobody freezes. Take it early once.
53. **The chase and the drive.** Disable the car or stop Miller. Reach in for the drive: "Drive secured. Stored in the van." A living Miller runs off; nothing asks you to kill him. Lose the police with Ice and Gohan walking to the exit; pick them up in the van. The van and the Fugitive are still there after the pass.
54. **Skip and retry.** Skip the briefing: a four-line card (target, why, roles, first stop) for seven seconds. Fail and retry: a one-line recap.
55. **Inactive brothers under fire.** During the chase, watch the map: Ice fights the escort from cover and Gohan holds his cover point; neither stands in the open.

## M01 escape and the companion shield (September 10)

56. **Mateo leaves on camera.** Clear the yard (or let the clock run): a short scene shows Mateo at the slipway, boarding the launch, and the launch pulling away with his line. Enter skips it; he is still aboard and gone. He cannot be killed at any point.
57. **Nobody dies off screen.** Play M01's firefight as Ice and watch Guess and Gohan: they take cover and fight and their health does not move. Switch into one: he can be hurt. Stand down: the shield is off.

## Story to play, second slice: the opening (P2c)

58. **The message at home.** Fresh save, prologue: drive home, stop at the door. The door scene is Ron getting out and walking to the door, one line. Then the fade, the starter room, Ron crossing it, the phone, and three lines with the last on his face. Enter during the room skips to the same state. Then the cut to the dock as before. If the room never loads (12 s), the message plays at the door with a notice.
59. **Mateo runs.** M01: after the recognition scene, watch Mateo and the dock technician run for the launch through the yard. Shoot near them: Mateo takes nothing, the technician can drop. The HUD reads "Hostiles: N" with no countdown. Leave guards alive for two minutes: nothing happens.
60. **The launch waits for a clear yard.** Kill the last guard while Mateo is already in the boat: the escape plays at once (he is aboard, the launch pulls away). Kill the last guard while he is still running: "Mateo is running for the launch" until he gets there, then the escape.
61. **Back from the terminal.** Play Gohan's copy with Ice's and Guess's jobs already done: the moment it completes you are Ice, no wheel. Play it with Guess's job still open: you become Guess.
62. **The laptop.** Cold open and mission: the laptop sits on the table, not above it.
63. **M02 at the curb.** Start M02: the prototype (locked) and the Granger at the curb, the crew on foot, three lines while they board. The clock starts after. The van moves off on its own and does not sit at lights; if it stalls, the log says the route was re-issued.
64. **Custody and the canal.** After the rear doors: the case is in Ice's hand; it disappears when he boards ("Drives stowed"). Pick up a wanted level in the chase: the canal will not pass until it is gone; nothing clears it for you.

## Story to play, third slice: M03 as designed (P2d)

65. **The split.** Start M03 at the base: Ice and Gohan get into the crew van and pull away with two lines; skip once. The map then shows them outside the depot and you are in the Primo. Halfway to Davis, Ice's call.
66. **The junction.** Stop in the zone: nothing happens while you sit in the car. Get out onto the marker: a six-second hold with no button; the dogs come for you as you arrive; five seconds into the hold the block comes out of the houses with a short moment on the first of them. Drive through the zone at speed once: nothing ejects you.
67. **Ice's entry.** Switch to Ice: the guards are patrolling, not shooting. Fire a shot near the depot before the marker: the mission fails and says why. Reach the marker without firing: the yard wakes. As Ron, shoot at the junction with Ice's stage open: no failure.
68. **Loading, seen.** Clear the yard, switch to Gohan, press E at the rear of the Benson: the doors open and he carries three crates from the pallet into the bed, two lines, doors close after. Skip once: the crates are in the bed anyway.
69. **The truck home.** Take the Benson as Ron with two stars: arriving at the foundry with stars is not delivery; lose them first. On delivery the truck locks (try the door) and the aftermath plays with Ron out of it.
70. **What remains.** After the pass: the locked Benson at the foundry, the van outside the depot, the Primo at the junction.

## Control after a ped change, and the witness slice (P3a: M05, M06)

71. **You can move.** Fresh save through the prologue to M01: control is yours the moment the dockyard starts. In M01, finish Gohan's copy: you stay on Gohan, and after the recognition scene you can move. The log says "Player control was off after …; restored" if the guard fired.
72. **The count.** In M01's firefight the HUD reads "Hostiles: N" and nothing about Mateo; his blip and the objective line say where he is.
73. **M05, the cove.** Start M05: three shots (the cove from the perch, the dinghy with Ron and Gohan, Mateo's boat under the lamps) with two lines; skip once. Clear the lamps, flare, close on him. When he stops: Ice's line and Ice running down to the shore. Bring the dinghy alongside and press E: Mateo climbs into the dinghy and says it over Gohan's shoulder; skip once and he is still aboard. The mission ends with him alive in the boat.
74. **M06, positions and the pickup.** Start M06: three shots on the three positions with two lines; skip once. Cut the feeder, walk Ice in, hold the alley. On the first rotors: Ron's radio line, and if you are Ice, watch the map: the Granger moves to the alley mouth on its own. When the burn ends a fire burns at the racks. Switch to Guess: bring the truck to the alley-mouth marker if it is not there; Ice and Gohan come to it; nothing clears the police for you.

## The starter room and a start from the menu

75. **The room loads.** Home marker, E, "Enter apartment": black screen, then inside the furnished room with control, facing into it. The log has "Apartment: interior N was disabled and capped; pinned for the visit" (or "was enabled") and then "Apartment: entered". No "Apartment timeout". Leave by the entry marker's menu; the log says the room was put back.
76. **Map the room.** With `[Dev] Enabled = True`, inside the room open the menu at the entry marker and choose "Map this room". `Bloodlines.Room.txt` appears beside `Bloodlines.ini`: a floor map with '@' where you stood. Send it, or read it, before surveying: it says where the floor, walls and furniture are.
77. **Survey the spots.** Same menu, "Survey the room's spots": stand where the door, the message spot (a window or the couch), the wardrobe, the bed and the locker belong, pressing F11 on each; the entry key comes first, so stand at the door facing into the room for that one. From the next session those spots have their own markers and prompts, and the prologue's message walks to the message spot.
78. **A start from the menu.** Open the dev menu somewhere away from a job, pick any mission from the list: a short fade, then the briefing plays at that job's marker with the crew's car pulling up to that street, and gameplay begins there without a second cut.
79. **Helicopters.** Deploy the crew and fly a Buzzard: it should feel exactly as it does with the crew stood down. The log has no "flight handling:" line for a helicopter any more (planes still get one).

## The dish and the engines (P3b: M07, M08)

80. **M07, the approach and the clamp.** Start M07: three shots (the dish from the roof edge, Ice, the sedan in the lane) with two lines; skip once. Reach the mast, hold E: a case appears on the dish and the manifests play over it as a scene (two engines, Paleto); skip once and the case is still there. Then the helicopter is shown coming, once, and Ron's radio follows. Get down, board the sedan: an aftermath shot on the car. The log has "Evidence aegisManifests -> CopyHeld".
81. **M08, the window and the forks.** Start M08: four shots (the crates, the flatbed, the camera room, the gate) with two lines; skip once. Loop the cameras: "Camera loop: Ns" counts down on the HUD. Clear the sentries as Ice, take the forklift as Guess, drive the forks to crate one and hold E: the crate rides the forks to the bed and sets down, counted "1 of 2". Then the technical is shown coming. Switch as you like: Ice puts it down, Guess loads crate two. Take the flatbed: Ice boards beside you, Gohan gets in the Granger and follows. Lose the police, deliver to the connector: the flatbed locks there with both crates on it and stays after the mission. The log has "Cargo turbineEngines -> M08.Connector".
82. **M08, the loop drops.** Loop the cameras and wait four minutes: the job fails with the loop message.
83. **The apartment fade.** Enter the apartment from the prologue's door: the screen stays black until the room is ready; never the void.

## Ron's third round (September 10)

84. **M01 marks and escape.** In the firefight every hostile has a red mark on the map that disappears when he drops. The moment the last one drops the escape plays; Mateo is at the boat for it even if he was short of it.
85. **M02 boarding.** Take the drives as Ice, walk to the stopped Granger, press E at its door: you are in. No "return to the Granger" loop.
86. **M03 junction.** Arrive as Guess, get out, hold the marker: the street crew comes; you fight them as Guess; only when they are down does the depot become Ice's and the switch is asked for. No freeze on arrival. At the depot no guard is inside the Benson. After delivery Gohan gets out of the truck on his own and follows you.
87. **M04 arrival.** Guess drives up alone. Ice and Gohan are already at their positions when you arrive and the meeting never saw them come.
88. **Police.** With grayed stars, drive past a patrol in plain view: the stars flash and they come. Break line of sight for a while: the search runs down as before.
89. **M07 and M08 placement.** M07: Ice starts on the roof of the building and the mast marker is up there; the sedan waits on the street below, not against the entrance. M08: crates a truck-length apart, the flatbed clear of them, the forklift on the open apron with room to turn.
90. **The meter.** Any hold or piece of work (the ledger copy, the junction, the breaker, the forks) shows its name and a small yellow meter above the subtitle line, no countdown text.

## Apartment tiers

91. **Three rooms.** Enter Ice's, Gohan's and Ron's homes in turn: a studio, a one-bedroom, a house; three different layouts. Ron's home marker is now the Forum Drive house door in Strawberry (the prologue's drive home goes there).
92. **The Diamond.** With the dev menu on, from any home marker choose "Preview the next residence" twice across the tiers (or complete M47): the casino penthouse, two floors, furnished (bar, lounge, spa, cinema). If rooms are bare, quote the "Apartment: N entity set(s)" log line; a set name the engine does not know is silently ignored and needs correcting.
93. **The luxury tier.** After M27 the home markers collapse to the Eclipse Towers door and each brother's penthouse is his own floor, as before.

## The IFF, the truck, the shop (P3c: M09, M10, M11)

94. **M09.** Start M09: three shots (the escort, Ice, the flat) with two lines; skip once. Fly the ridge, then as Ice take the driver: the truck stops, the other two pull away and the moment shows the colonel leaving. Hold E at the cab: the unit in Ice's hand as a scene, then Gohan's radio read. Land on the flat marker as Guess, board as Ice, fly to the drop. Blow the escort up instead: the job fails with the unit.
95. **M10.** Start M10 from the connector marker: both crates on the flatbed. Hold E at the bed: the inspection. Roll out with Ice beside you; bikes; the Buzzard moment and "tunnel mouth" named; get the truck there, step out as Ice, kill it; lose the police, deliver to Burro Heights: the truck shuts down with both crates on it and stays.
96. **M11.** Start M11: the shop mid-work, the flatbed with one crate, Gohan walking up; skip once. Mounts, dyno with the meter, then Gohan at the window with Berth 44 as a scene; skip once. The aftermath: Ice out, the Granger shown.

## The first solo window (P3d: SM01, SM02, SM03)

97. **SM01.** Start SM01: three shots (Sergei, a crate, the gate) with two lines, Ron over the radio; skip once. Clear the floor, hold E at Sergei: he gives the codes and runs. Shoot him first instead: the job fails. Hold E at the crates: two crates into Ice's car on camera, counted; skip once and they are in the car. Drive to Ice's door: the reward line names the Mk II and double rifle ammunition, and the next locker restock does exactly that.
98. **SM02.** Start SM02: three shots (the roof, the bay, a laptop on a table) with two lines; skip once. Stun both guards, hold E at the terminal: the tap as a scene, then "IT will trace this: 75 s". Wait it out on the roof: the job fails. Do it again and take the fire escape in time: Gohan's radio, the Marksman Rifle named.
99. **SM03.** Start SM03: KJ walks the crate on the coupe's tail and the coupe, then looks at the Elegys, with two lines; skip once. Win three laps; the first rival's guns are shown once; Ron's line; stop them or reach the finish; then drive the coupe to the Burro Heights chop bay: the prize recorded, $25,000 and the transmission named, KJ's check-in as the aftermath.

## The preparation jobs (P4a: M12–M15)

100. **M12.** Start M12: the sub, the freighter, Ice on the pier, two lines; skip once. Dive, scan the hull with the meter: the scan as a scene; skip once; then the launches shown once; back to the jetty. The log has "Evidence hullSurvey -> CopyHeld".
101. **M13.** Start M13: the barges, the slipway, the Granger, two lines; skip once. Plant all three with the meter; a launch turns into the basin and is shown once; reach the slipway; board the Granger and hold E: the basin burns, seen from the slipway. The log has "harborPatrolsReduced".
102. **M14.** Start M14: the Besra with the pod, the apron, the ridge, two lines; skip once. Clear the apron, take the plane: Ice's radio, and on the map Ice and Gohan walk to the Granger on the ridge road. Gohan's cue at the midpoint. Land at McKenzie and stop: the pod under the wing as a scene. The log has "Cargo radarPod -> M14.McKenzieHangar".
103. **M15.** Start M15: the access, a watchman, the cable point, the Granger at the exit, two lines; skip once. Stun three, splice with the meter: the splice as a scene; Gohan's radio that one gate answered; leave. The log has "harborGateAccess".

## The lift and the sub (P4b: M16–M18)

104. **M16.** Start M16: the unit on the Granger's dash, the Cargobob, Ice, two lines; skip once. Cross the pad: Gohan's radio challenge, then four stars. Clear it, take the Cargobob as Guess: Ice walks to it and boards, Gohan gets in the Granger. Fly the canyon low, lose the pursuit, land at Terminal: the lift as a scene. The log has "Cargo iffTransponder -> collected" and "Cargo cargobob -> M16.TerminalDrop".
105. **M17.** Start M17: the sub, Ron looking, Ice at the crate, two lines; skip once. Weld three points: a part appears on the hull each time, "Part N of 3". Ron's radio about the release; test the lock; hold E at the sub's rear: the handle scene; skip once and the handle is there. The log has "Cargo kraken -> M17.DrySlip".
106. **M18.** Start M18 with a solo job open: a notice names it. The plan scene, two lines; skip once. Deliver the sub, land the lift, hold E beside it: the pod on the airframe; bring the hauler, load it: the roll call scene with each man in his seat; skip once. The log has "Cargo heistClock -> M18".

## The Port Heist (P4c: M19–M22)

107. **M19.** Let M18 continue into M19 (or start M19 with M18 played): the cut shows the Kraken at the channel mark, the Titan Star, Ice on the pier, Ron in the lift at the hangar, two lines; skip once. The log has "Kraken taken over at M18.ChannelMark". Cut the breach with the meter; clamp both points: a float appears at each, "Float N of 2"; after the second, the float scene with Ice's line; skip once and the container is on the surface beside the mark with both floats on it. Surface at the mark. The log has "Cargo kraken -> M19.Surface" and "Cargo bullion -> M19.Surface"; the operation continues.
108. **M20.** The handoff cut: the container on the water, the gunners, Ice, the lift, two lines; skip once. The log has "Cargobob taken over on the apron" and "container taken over" (no second sub, lift or container on the map). Take off: Gohan's radio names the pod live (or none, if M18 was skipped) and his sub moves to the pier. Clear the quay as Ice; hold the hover as Guess: the hook scene with the cable line; skip once and the container hangs under the aircraft, which flies heavy. Climb to the marker: the transfer scene; skip once and Gohan is at the launch's helm with Ice in the other seat, both visible; Gohan's line about the Kraken at the pier. The log has "Cargo kraken -> M12.PierWatch".
109. **M21.** The escort cut: the launch with both aboard, the held lift, the breakwater, two lines; skip once. The log has "launch taken over from M20" and "container carried on". Board: Ice's radio says two launches (three if M13 was skipped) and Gohan's says the gate answers (or does not, if M15 was skipped); the boats come from beyond the gate or from the breakwater to match. Kill them, run the breakwater: Ron's line, the lift turns north on the map. Bring the launch to the shore marker: the transfer scene; skip once and both are in the Granger at the road, Gohan driving, pulling away. The operation continues.
110. **M22.** The arrivals cut: the drop point, the held lift, the Granger driving down to the beach with Gohan and Ice, two lines; skip once. Hover and release: the drop scene; skip once and the container sits in the shallows. The log has "Cargo bullion -> M22.AlamoDrop" the first time only. Land, shut down, walk to the others: the strike scene: the phone first, then the foundry blowing up at Cypress Flats, then the beach, then Ron with the keys; skip once and the keys are in the Granger. Four lines over it, Gohan's last line after. Then replay M22 from the menu: the log says the cargo record is left alone and the dredging ledger does not reset.

## The desert (P5a: M23–M27)

111. **M23.** Start M23: the Granger short of the dome, Ice on the approach, Ron walking the fence, Gohan on the generator house, two lines; skip once. Go up to the dome: on the map the other two move to cover, not into the yard. Clear it; check the three bays with the meter: a subtitle per bay. Start the generator with the meter: the scene with Gohan's line; skip once; the notice lists the four limits. Walk to the door. The log has "M23 limits:" and the bunker is a safehouse.
112. **M24.** Start M24: Ron at the truck, the container in the water, Ice on the ridge, Gohan in the shallows, two lines; skip once. Park at the marker, work the cable: Ice's warning, then one cruiser and then two come down the ridge road and the deputies get out at the shore, blipped "Bought deputies". The hoist scene: two crates onto the bed; skip once and they are on the bed. Gohan's radio; he walks to the cab and gets in before the truck may roll (20 seconds, then seated). Drive to the bunker: the unload scene; skip once and the crates stand in bay one. The log has "first portion in bay one" and "Cargo recoveredGold -> M23.BayOne".
113. **M25.** Start M25: Ice at the north end of the deck, Gohan on the rim, the boat under the span with its engine running, the tanker, two lines; skip once. Walk the deck, kill the tanker from 30 m, hold three waves. The escape scene: the closest hunter, then down to the boat, Ron's line; skip once. Ron's radio during the jump. Jump, land near the boat, board.
114. **M26.** Start M26: a spotter over the lake, the Lazer on the apron, Gohan at the laptop on the Granger's hood, Ice at the hangar, the Duster beside the Lazer, two lines; skip once. Take off, splash the first: Gohan's radio to hold the second; keep it in sight 12 seconds: his second radio and the log's "Evidence charterCallSign -> CopyHeld". Splash it early instead: the failure names the call sign. Splash the second, land at McKenzie and stop: the park scene with the Duster beside the Lazer; skip once. The log has "Cargo lazer -> M26.DusterPad".
115. **M27.** Start M27 after M26: the same Lazer parked, the Duster, Ice checking his parachute and boarding the second seat, the track, Gohan's boat, two lines; skip once and Ice is in the seat. Take off, hold station on the Shamal: the transfer scene, a fade in the middle; skip once and you are Ice in the Shamal with the screen lit. Take the ledger: a case in Ice's hand; Ron's radio and the Duster turns for McKenzie on the map. Bail, chute, board Gohan's boat: the case in the boat. The log has "Evidence flightLedger -> CopyHeld".

## M03 depot ambush (Ron, September 11)

116. **M03.** Start M03: the log has "M03 Benson at ... (road node near the key)" and the truck stands on the road, not in a wall. At the junction, four dogs come at Ron within seconds of him stopping; the street crew (six) and the dogs are red on the map while they are his fight. Ice wakes ten guards. After the loading scene Gohan is in the cab; then Ice's radio: the gate opens again, seven gunmen and three dogs come at the truck, Gohan gets out into cover. You are locked to Ron until you are within 45 m of the depot; then the switch is free and the targets are red. Clear them; take the Benson as Ron: Ice's radio, he walks to the passenger seat (20 s, then seated), Gohan takes the van behind you. Deliver as before; Ron and Ice get out before the keys go down.

## M05 water and shore (Ron, September 11)

117. **M05.** Start M05: the log has "M05 dinghy on the water at ..." and, if the estimate was on land, "Water key M05.DinghySpawn resolved N m from its estimate". The dinghy floats with Ron at the wheel and Gohan beside him; the log says if either had to be warped in. The generator crew stands at the cave mouth on the shore below the cliff (the log gives the first post), not on the road. If the coast has no water deep enough within 250 m the mission refuses with "No water deep enough at <key>"; survey the coast key with F11 and retry.
118. **Start cars.** Start M04 (and M03): the crew van and Ron's car no longer sink through the ground and pop back up. If the ground was not loaded at spawn the log has "Holding a fresh ..." and then "Ground loaded under ..."; the car sits still for that moment.
119. **M06.** Start M06: the marker and Ron's Granger are at Vespucci Beach, well away from the depot (the log gives the standoff). Gohan cuts a real panel on the sidewalk and, during the burn, works at a bench with two cases on it, not in the road. Ice's entrance marker is at the surveyed alley point. When the rotors come, Ron's truck drives in (or you do) and cannot be destroyed until the boarding stage opens at the alley mouth; from then it can. No SWAT trooper stands in a wall; a spawn with no walkable point near it is logged.
120. **M06 pickup.** During the siege Ron's Granger stays at the beach; his radio says he comes when the burn is done. When the pickup stage opens the truck drives in (or you do).
121. **M07.** Start M07: Ice on the roof, Gohan at the foot of the building at a laptop, Ron in the lane. If a notice names `M07.GarageRoof`, the roof estimate is wrong: stand on the real garage roof and survey it (F11), then retry; Ice will start up there. The Buzzard fires on the roof once the sniffer is clamped.
122. **M08.** Drive the forklift to a crate pad and stop: after a second the crate is on the forks with no button, then the loading scene. The crates cannot be knocked over. The five sentries stand apart; shooting one wakes the rest.

## Not done in this pass

- Checkpoint reconstruction (audit §5) remains off by design.
- Per-mission companion death tiers (§5.3) beyond the existing "named heroes are critical" rule.
- Cypress post-strike presentation (§15).
- A visible hangar Lazer between M16 and M26 in free roam; M26 and M27 now show it parked on the apron, free roam does not.
- Optional SM10–SM13; not registered pending pacing review.
- Packages C and D of the playthrough repair plan (drive-from-start restructuring, M01/M02 staging, the police policy, M03 ambush and locked Benson, M04 forced switch, the apartment call).
- The M05 perch coordinate itself is still an estimate; the ground snap makes it start, the survey makes it right.
- Timecycle modifier names in the visuals defaults are unverified; they are overridable from the ini without a rebuild.
- The van's stash coordinate is an estimate beside the base; survey it if the van lands in a wall. Wheel type and plate are not yet persisted.
- The room's spots are desk estimates until surveyed inside (items 76–77); the room map is the tool for laying them out.
- M08's bed slots, forklift approach and stash are estimates; M09's pickup flat and M10's window are estimates.
- P3 is built through P3d and P4a through M15; every slice's scenes still want the live watch/skip/cancel pass.
- M28–M30 and SM04–SM06 remain per the plan's P5b–P5c rows.
- The desert's new keys (`M23.SecondExit`, `M24.RidgeRoad`, `M25.DeckApproach`, `M25.RimPost`) are estimates; the deck approach must be on the bridge in game or Ice starts in the canyon. The Lazer is still not shown in free roam between M16 and M26.
- The Port Heist now runs as one continuous mission from its one marker (items 107–110 still describe each phase's content; the chapter passes between them are gone, and a dev-menu start runs a single phase on its own). The road team's drive from the coast to the Alamo in M22 is the live risk: a bounded ten-minute journey, then a retry of that phase. See `CONTINUOUS-PORT-HEIST.md`.
- The Port Heist's new shore keys (`M21.ShoreLanding`, `M21.RoadPickup`, `M22.RoadArrival`) are estimates; the road transfer and the arrival move with them when surveyed. M21's lift after the split and the launch on the coast are left to the engine; the keys prop `p_car_keys_01` is checked by eye in game, and both scenes play without it if it does not load.
- M16 still clears the wanted level at its end marker; the endpoint helper exists, it does not use it yet. M03's bed offsets, pallet, entry marker, watch points and the block's spawn offsets are offsets from the surveyed keys; survey `M03.HaulerSpawn`, `M03.DepotGate` and `M03.RailJunction` and the set moves with them. M02's mid-chase moments are lines, not camera cuts: a scene holds the player's car, and at freeway speed that is a crash.
- The M04 cover points and the buyer's car spot are offsets from the surveyed keys; if a brother stands in a wall, survey `M04.RampGuards` and `M04.Breaker` and the set moves with them.
- The road-handling and Buzzard numbers: opening values only until the paired runs are logged.
- M31–M70 and SM07–SM09 gameplay.
