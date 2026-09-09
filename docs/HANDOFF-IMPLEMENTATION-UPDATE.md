# Handoff implementation pass — September 9, 2026

Branch `claude/handoff-implementation-pass`. This implements the approved items
from `AGENT-HANDOFF-PROPOSED-CHANGES.md` and the priority list in
`CAMPAIGN-IMPLEMENTATION-AUDIT.md` §19, in that order, as far as they can be
built and checked without the game running. The per-item reconciliation is in
`CHANGE-REGISTER.md`. Nothing here has been live-verified.

## What changed

### Campaign state (audit priority 2)

- `CampaignState.Progress()` reports one of four states: next story mission, optional side content only, story blocked on an unmet prerequisite, or all implemented content complete. The mission key and the save menu use it. The end of the scripted campaign is reported as such; M01 is never a fallback.
- Failure retry: the mission key retries the failed job and says so; walking to another marker still chooses that job.
- `CampaignState.PrologueComplete` (saved) with `PrologueDue` for saves that have not finished M01.
- SM01's armor-piercing crates are a real supply line: `armorPiercingSupply` doubles Ice's rifle restock at any locker.
- SM03's racing transmission is consumed: cars and motorcycles repaired at Guess's chop bay receive the race transmission mod once.
- Mission-issued weapons are loans: locker capture is suspended while a mission runs.
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
- `CutsceneDirector.Play(..., blocking)`: movers stay unfrozen, the camera tracks the running step's subject, the scene holds until dialogue and blocking are both done, and a skip (or the watchdog, or teardown with a living player) completes the remaining steps. Watching and skipping produce the same seats, positions and control state.
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
python tools/build_story.py --check    79 missions, 350 unique cues, 161 scenes; freshness verified
python tools/audit_campaign.py         PLAYABLE-MISSION-MAP.md regenerated
python tools/run_story_tests.py        642 story/runtime checks passed
python tools/run_regression_tests.py   165 checks passed
python tools/build_roslyn.py           Bloodlines.dll built with warnings as errors; prebuilt/ refreshed
```

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

## Not done in this pass

- Checkpoint reconstruction (audit §5) remains off by design.
- Per-mission companion death tiers (§5.3) beyond the existing "named heroes are critical" rule.
- Cypress post-strike presentation (§15).
- A visible hangar Lazer between M16 and M26; the dialogue now claims it, the world does not show it yet.
- Optional SM10–SM13; not registered pending pacing review.
- M31–M70 and SM07–SM09 gameplay.
