# Bloodlines review — September 19, 2026

## Repair status — completed September 19, 2026

All five findings below are fixed in source and in the rebuilt DLL installed at
`C:\Program Files\Rockstar Games\Grand Theft Auto V Enhanced\scripts\Bloodlines.dll`.
The source build, committed prebuilt DLL, deployment package, and installed DLL have matching
SHA-256 `F64180FD76A445A47221CC78D4A814E49A12E1E78C8E0D27D33E389306540E2C`.
The prior installed DLL is backed up at `build/review/installed-before-repair.dll`.
Only the installed DLL was replaced; player saves, configuration, and survey overrides were not modified.

- Crew orders, the character wheel, M09's shot window, and Slipstream now share named time claims. An ability ending releases its own claim; emergency teardown resets the clock. The order strip blocks custom switching while still suppressing the vanilla wheel.
- Accepted phone hangout and travel choices clear the selected brother's old quick order. Refused changes and the other brother's orders remain intact.
- Getaway drivers hold a valid officer target for the full cooldown, but still react to target loss, changed destinations, and the end of pursuit.
- Continuous-heist HUD data follows the active chapter. Results inspect the existing operation world, including earlier chapter entities, with handle deduplication.
- The current-status documentation now reflects implemented missions and garages. The feature additions remain proposals.

Validation after repairs: **328 regression checks and 5,071 story/runtime checks passed**,
including **33 additional checks**. Release compilation, mission lint, all 847 district checks,
campaign-map freshness, and generated-story freshness passed. The story stand-in now makes
native time writes visible through the SDK clock, so the overlap checks observe the same state.
`tests/story/ReviewRepairTests.cs` covers both real heist dispatchers, a two-phase operation,
both menu-close orders, ability expiry, throwing ability cleanup, and emergency reset. The
regression additions cover phone replacements and flee target changes. Logs are in `build/review/`.

Live follow-up remains: try keyboard orders while pressing the controller switch, let Slipstream
expire under the order strip, replace a hold through the phone, drive a pursuit with multiple
nearby officers, and inspect HUD/results during both continuous heists. No live GTA session or
rendered HUD review was performed for this repair.

## Original review scope and assessment

Reviewed the September 17–19 changes from `d6f395b` through `d3df638`, with surrounding crew, mission, operation, phone, race, and packaging code. This was a focused integration review, not an exhaustive audit of all 79 mission scripts. At the original review stage, no production code, configuration, or installed game files were changed.

The current source builds and the existing automated suites pass. The five reproduced defects below describe the original findings; their repair status is recorded above.

## Findings

### P1 — Overlapping crew orders and character switching can leave time at 20%

Sources: `src/Bloodlines/BloodlinesMain.cs:448`, `src/Bloodlines/Core/CrewOrderStrip.cs:72`, and `src/Bloodlines/Core/CharacterWheel.cs`.

Hold the keyboard crew-order key, then hold the controller character wheel. The host calls the order handler before the switch handler, and the switch handler does not exclude an open order strip. The wheel captures the strip's 0.2 time scale. On the next tick the strip closes because the wheel is open, restoring 1.0; releasing the wheel then restores its captured 0.2. Both interfaces are closed, but time remains slowed.

The production UI classes reproduced a final scale of **0.2 instead of 1.0** for that sequence. Route these interfaces through shared time ownership and prevent overlapping input ownership. Also cover ability expiry and interruption by death or scenes; those transitions use the same global clock.

### P2 — Phone choices do not replace an existing quick order

Sources: `src/Bloodlines/Crew/CompanionController.cs:112` and `src/Bloodlines/Core/CampaignHub.cs:320`.

Give Gohan “Hold here,” then use the phone to end his hangout or choose “Drive alongside me.” `SetHangout` changes his invitation and refreshes his AI, but leaves `_orders` intact. `DecideOrdered` runs before ordinary hangout/travel decisions, so he keeps holding. The phone reports a successful change that he does not carry out.

The production controller reproduced **dismissal accepted, hanging out false, order HoldHere, actual state Hold**. The separate-car choice also left him holding. Define phone choices as replacements for incompatible standing orders and test both surfaces together.

### P2 — A different nearest officer bypasses the five-second flee cooldown

Source: `src/Bloodlines/Crew/CompanionDriver.cs:116`.

The cooldown only applies while `sameOfficer` is true. If another officer becomes nearer, the drive task is replaced at the next 750 ms review even while the original officer is alive and in range. Several nearby officers can therefore repeatedly restart the getaway task—the behavior the cooldown was intended to prevent.

The production driver issued **two flee tasks 750 ms apart**, despite `FleeRefreshMs = 5000`. Keep the current valid threat until the refresh deadline; allow immediate replacement when that target becomes invalid or a higher-priority destination changes.

### P2 — The new objective HUD cannot inspect continuous heist phases

Source: `src/Bloodlines/Missions/MissionManager.cs:39`.

`CurrentObjectives` and `CurrentStageName` cast the active mission directly to `ComposedMission`. Normal Port Heist and Paleto Deep-Sea runs are `ContinuousOperation` parents, whose current chapter lives in `Phase`. The cast returns null, so the HUD retains basic objective text but loses the phase's structured owner, distance, progress, and any objective timer.

A focused object-graph probe produced **one live phase objective and null manager objectives**. Expose the active composed phase through a common accessor and run presentation checks through both operation parents, not just standalone chapters.

### P2 — Continuous heist results do not count their hostile kills

Source: `src/Bloodlines/Core/MissionTally.cs:99`.

The manager passes the operation parent to the tally, which only enumerates `mission.Staged`. The parent does not track the hostiles: its chapter scripts and operation world own them. Consequently the new results card reports zero tracked kills for these heists even when their phases contain dead hostiles.

A focused probe counted **zero kills through the parent versus one through the same tracked phase**. Provide a read-only operation-wide entity view and preserve the tally's handle deduplication across chapter boundaries. Do not introduce another entity owner.

## Useful additions after those repairs

1. **A compact race display and saved personal records.** SM03 already tracks player checkpoints, rival checkpoints, and route distance. Show position, gates remaining, and an approximate distance gap; save best completion times for replay value. Keep records separate from one-time campaign payouts, and label distance gaps as estimates rather than inventing precise time gaps.
2. **Author more interactive technical work using the existing objectives.** Current mission code instantiates `GaugeObjective` in M29 and `TechnicalChoiceObjective` in M28; no campaign mission instantiates `AlignObjective`. Use alignment for an appropriate signal-search beat and a technical choice where either option has a clear consequence. These are existing tools that can replace selected hold-button waits without adding another framework.
3. **One visible crew-order status across the strip and phone.** Show each brother's current assignment, whether he is boarding or driving, and a clear cancel/resume action. Distinguish an order being accepted from the brother actually reaching the seat or destination.

## Tightening and validation priorities

- Add cross-system tests for the reproduced cases. Existing strip tests use a simplified companion controller, while controller tests exercise the real AI separately. Passing each suite does not establish that phone, orders, and AI agree.
- Refresh `docs/FEATURES-STILL-PLANNED.md` and the chronological status claims in `CLAUDE.md`. The former still lists M44–M70 and SM07–SM09 as unimplemented and recommends building persistent garages, although those systems now exist. Keep one current status table and retain historical notes as history.
- Prioritize live geometry checks by the next missions being played. The committed location table contains **847 entries: 817 estimates, 29 surveyed, and one bible entry**. The district validator's zero findings establish zone consistency, not reachable floors, road connectivity, or usable interiors. Local survey overrides may improve the actual installation beyond this committed table.
- Real mid-mission checkpoints remain a larger feature. Current mission defaults explicitly disable restoration; implementing it requires reconstructing actors, vehicles, timers, role ownership, and private state. A restart button alone would not deliver it.

## Checks performed

- Roslyn release build against the pinned ScriptHookVDotNet3 3.6.0 reference: passed, 255 C# source files, warnings treated as errors.
- Windows regression suite: **321 checks passed**.
- Story/runtime suite: **5,045 checks passed**.
- Mission linter: **no errors**. Incomplete authored-dialogue coverage is reported separately; some omissions are deliberate adaptations and should not be blindly restored.
- Location validator: **0 of 847 flagged**.
- Campaign-map and generated-story freshness: passed; story coverage reports 79 missions, 500 unique cues, and 236 scenes.
- Five focused defect probes: reproduced the findings above using production classes compiled into the existing stand-in harnesses. Local sources and output are under `build/review/`; rerun with `python build/review/run_review.py` after building both test harnesses. The operation probes assemble the parent/phase relationship through reflection; they are not full heist walkthroughs.
- The documented dialect-lint script was not present at its configured path or in the searched project/skill directories. This document received a manual American English review instead.

No GTA play session, native driving validation, or rendered HUD inspection was performed. These checks establish the code paths described above, not live handling quality, race difficulty, placement accuracy, or visual acceptance.
