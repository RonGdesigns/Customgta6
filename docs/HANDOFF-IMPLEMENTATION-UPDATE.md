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

## Not done in this pass

- Checkpoint reconstruction (audit §5) remains off by design.
- Per-mission companion death tiers (§5.3) beyond the existing "named heroes are critical" rule.
- Cypress post-strike presentation (§15).
- A visible hangar Lazer between M16 and M26; the dialogue now claims it, the world does not show it yet.
- Optional SM10–SM13; not registered pending pacing review.
- Packages C and D of the playthrough repair plan (drive-from-start restructuring, M01/M02 staging, the police policy, M03 ambush and locked Benson, M04 forced switch, the apartment call).
- The M05 perch coordinate itself is still an estimate; the ground snap makes it start, the survey makes it right.
- Timecycle modifier names in the visuals defaults are unverified; they are overridable from the ini without a rebuild.
- The road-handling and Buzzard numbers: opening values only until the paired runs are logged.
- M31–M70 and SM07–SM09 gameplay.
