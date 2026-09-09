# Bloodlines — build audit: what exists against what is left

**Date:** September 9, 2026  
**Branch:** `claude/handoff-implementation-pass`  
**Method:** counted from the repository, not from the design documents. Every number
below came from the data files, the mission scripts or the lint tools, and the
command that produced it is named. Nothing here was confirmed in a running game.

---

## 1. The shape of the project in one table

| | Written | Playable | Live verified |
| --- | ---: | ---: | ---: |
| Main missions | 70 | 30 | 0 |
| Solo missions | 9 | 6 | 0 |
| **Total** | **79** | **36** | **0** |

The campaign is **written end to end and playable to the middle**. Every one of the
79 missions has a row in `missions.tsv`, a briefing and an aftermath in
`scenes.tsv`, and a feasibility classification. Data is deliberately ahead of code;
the risk is not a shortage of design, it is that only 36 of those designs have ever
been executed by a machine and none by a player.

### Where the playable campaign stops

| Act | Range | Scripted | Remaining |
| --- | --- | ---: | ---: |
| I — The Bleeding Trail | M01–M22 | **22** | 0 |
| II — The Squeeze | M23–M48 | 8 | **18** |
| III — Scorched Earth | M49–M70 | 0 | **22** |
| Solo | SM01–SM09 | 6 | 3 |

Act I is complete through the Port Heist and its aftermath. Act II is playable to
M30 and stops at the desert build-up. Act III — the rig payoff, the return south,
Davis, the tower and the finale — has no runtime code at all.

**43 mission scripts remain: M31–M70 and SM07–SM09.**

---

## 2. What is actually built

Systems, all exercised by the source-level suites (646 story checks, 165
regression checks) and none by the game:

- **Crew:** three heroes, switching, companion AI, driving, travel modes, recovery, death handling, military pressure, personal wanted state, weapon progression, per-character free-roam memory.
- **Missions:** stage/objective framework, 24 objective kinds, catalog, prerequisites, markers, retry, failure cleanup, required-asset failure, chapter handoff.
- **Campaign:** save state, four-way progress reporting, first-completion rewards, safehouses, fleet upgrades, dispatches.
- **World:** homes and apartments, shops and customization, weapon market, fleet garage, world tuning, survey mode.
- **Scenes:** cutscene director, dialogue director, scene blocking, the prologue.

---

## 3. The gaps, measured

### 3.1 Cutscene motion — 2 scenes of 161

`data/scenes.tsv` holds 350 cues across 161 scenes. Exactly **two** of them —
the prologue and the homecoming — drive actors with `SceneBlocking`. The other 159
still play the way the handoff document objected to: dialogue over held actors.

The mechanism is built and tested. This is wiring, not invention, and it is the
largest single gap between what the handoff asked for and what the build does.

### 3.2 The dominant verb is a timer

Objective constructor uses across all 36 scripted missions:

| Objective | Uses |
| --- | ---: |
| MissionInteraction | **34** |
| ProtectObjective | 31 |
| EnterVehicleObjective | 28 |
| DeliverVehicleObjective | 27 |
| ReachZoneObjective | 20 |
| KillTargetsObjective | 17 |
| TechnicalChoiceObjective | 1 |
| *(18 other kinds)* | 1–5 each |

`MissionInteraction` is "stand in the circle, press the button, wait N seconds",
and it is the most common thing the player does. Calling it welding, hacking or
splicing does not make it look like any of those. This is the audit's §8.3 finding,
now with a number against it.

### 3.3 Required assets — 5 mission files of 37

`RequireAsset` is declared in 5 mission files, `RequireSurvivor` in 2, and
`ProtectObjective` appears in 13. Roughly 19 scripted missions still have no
explicit statement of what they cannot finish without. Each is a candidate softlock:
an objective waiting on a vehicle that no longer exists.

### 3.4 Coordinates are still guesses

`data/locations.tsv` holds 161 positions: **157 estimates, 4 from the bible, 0
confirmed in game.** Surveying writes to `Bloodlines.Surveyed.ini` at runtime rather
than back into the repository, so the shipped file is expected to read "estimate" —
but no capture session has happened, which means every marker, spawn and deployment
in the campaign is a district-level guess that `validate_locations.py` can only
check for being in the right neighborhood.

This is the cheapest large risk reduction available and it needs no code.

### 3.5 Dialogue leakage — 3 lines left in scripted missions

The HUD-language pass covered 13 missions and is enforced by a test. Three spoken
lines outside that set still carry marker language:

- `M08_S1_02_GOHAN` — "clear the marked sentries"
- `M12_S1_01_GOHAN` — "first the sonar marker"
- `M12_S1_02_ICE` — "keep the sub at the marked depth"

(`M54` has one more, but M54 has no script yet, and its "marking the patrols" is
Gohan describing his own display rather than the game's HUD.)

### 3.6 Known and correct

`M01` fires 6 of its 9 written gameplay lines. The three unfired cues
(`M01_S2_04/05/06`) are the old recognition lines, deliberately superseded by the
authored recognition scene. The linter reports it as "thin"; it is intentional.

---

## 4. What can be tightened, in the order I would do it

### 1. Put motion into the scenes that already exist
159 scenes still freeze their cast. Start with M01's intro and recognition, then the
M02 and M03 aftermaths, then work forward. Each conversion is a `SceneBlocking`
chain of steps that already exist and a check that skipping lands where watching
does. **Highest story payoff per hour of work in the project.**

### 2. Give `MissionInteraction` a body
One presentation wrapper — stock scenario or task, optional prop in hand, optional
particles, interruption behavior — applied to 34 existing call sites makes the whole
campaign look like people doing work instead of people standing still. No new
missions, no new design.

### 3. Declare required assets across the remaining 19 mission files
Mechanical, testable, and it removes a class of softlock before anyone plays.

### 4. Survey the map
One session with F11 converts 157 guesses into fact. Do it before scripting M31+,
so the new missions are placed against known ground instead of inheriting the same
uncertainty.

### 5. Move M01's staging out of the shared cutscene director
`CutsceneDirector` hardcodes M01: a `_dockIntro` flag, camera positions keyed to
line indices 1, 5 and 7, and the spawning of the prototype, Mateo and a dock worker.
That is mission content living in a system every mission uses. `SceneBlocking` now
gives it somewhere better to live.

### 6. Close the last three HUD lines
Fix M08 and M12, and widen the enforcing test's mission list so the next one cannot
slip in. The test is the point; the three lines are five minutes.

### 7. Make the radio-debrief rule explicit
`ComposedMission.PrepareStages` wraps its body in `if (true)` — a dead conditional —
and appends a "Radio debrief" stage when `Id.StartsWith("SM") || number >= 7`. A
mission silently gaining a stage because of its number is the kind of rule that
surprises whoever writes M31. Make it a property a mission opts into.

### 8. Tidy teardown
`OnAborted` calls "close character wheel" and "stop scene" twice each. Both are
idempotent so nothing is broken, but the duplication invites someone to trust one
copy and delete the wrong one.

### 9. Split `DevMenu.cs`
834 lines, the largest file in the project, almost entirely page builders. Low
priority — it works — but it is where a new debugging tool goes to get lost.

### 10. Consolidate the documentation
40 documents, 11,282 lines, against 18,917 lines of code. Some is generated
(`COMPLETE-DIALOGUE.md` alone is 3,809 lines) and correctly so. The rest overlaps:
the three superseded proposal documents, several update notes and two audits now say
overlapping things about the same missions. `CHANGE-REGISTER.md` is meant to be the
single reconciliation point; the others should point at it rather than restate it.

### 11. Finish the American English pass, carefully

<!-- dialect-ok-next: the British spelling below is the quoted finding, not prose -->
`dialect-lint.mjs` reports 25 findings across 12 files — mostly "metres" in older
update notes. Three of those files must **not** be bulk-fixed:

- `docs/VEHICLE-CATALOG.md` lists the GTA vehicle **Cypher** (model `cypher`). That is a proper noun and a model name the mission linter checks. Rewriting it to "Cipher" would falsify an asset name; it needs a `dialect-ok` suppression instead.
- `docs/bibles/omnibus_v2.txt` is extracted source material and is never hand-edited.
- `COMPLETE-DIALOGUE.md` and `PLAYABLE-MISSION-MAP.md` are generated; their findings have to be fixed in the source data and regenerated, or they come straight back.

The remaining files can take `--fix` directly. Doing this blind is how a vehicle
model name gets quietly corrupted, which is why it is a deliberate pass and not a
one-liner.

---

## 5. What I would not do yet

- **Checkpoint restoration.** Still correctly off. It needs entity, vehicle, cargo and timer reconstruction per mission, and enabling it globally before that exists would turn a clean failure into a corrupt one.
- **SM10–SM13.** Good concepts, but the campaign's pacing problem is the 18 unwritten Act II missions, not a shortage of optional content.
- **New systems from `FEATURES-STILL-PLANNED.md`** — garages, weapon benches, intel maps. Every one of them is a reason not to write M31.

---

## 6. The honest summary

The engineering is ahead of the content. Thirty-six missions run, the systems
underneath them are more complete than the mission count suggests, and the campaign
has a written ending it cannot yet reach. The three things standing between this
build and a playable campaign are, in order: **43 mission scripts**, **one survey
session**, and **motion in 159 scenes**. Everything else on this list is polish that
can happen alongside them.
