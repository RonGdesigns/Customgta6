# Bloodlines — Post-Merge Audit: Verification and Installed-Build Findings

**Date:** September 9, 2026  
**Audited snapshot:** `78b48c2` (merge of PR #1), tree identical to `6efbf8d`  
**Installed build:** `scripts\Bloodlines.dll` sha256 `567f8652…`, the committed `prebuilt/Bloodlines.dll` at `78b48c2`; loaded by ScriptHookV v3889.0/1158.13 (Enhanced, Jul 15 2026 build) and ScriptHookVDotNet Enhanced 3.9.0.6, two scripts resolved to API 3.9.0 against target 3.6.0.  
**Status:** Read-only. No repository, save, configuration or game file was changed by this audit.  
**Method:** every finding in the external post-merge audit was re-checked against the source at `78b48c2`; then the installed game folder (logs, hook versions, other mods, your INIs) was read for what a source-only review cannot see. Confirmed source observations are marked as such; nothing below is a live playtest result except the one log excerpt in §3.

---

## 1. Verdict on the external audit

| ID | External finding | My verification | Verdict |
| --- | --- | --- | --- |
| R01 | Prologue GPS route is cleared by the host tick | `Step("prologue", …)` runs at `BloodlinesMain.cs:158`; `ObjectiveMarkers.BeginFrame` at `:171` clears `Pending`; the prologue's `Navigation()` call therefore never reaches `EndFrame`. The drive shows the yellow cylinder and the subtitle but no GPS route. | **Confirmed.** One-line fix: move the prologue step inside the marker frame. |
| R02 | Scene failure does not propagate into prologue completion | `SceneBlocking.Update` finishes a timed-out step and advances without checking `Failed`; `PrologueSequence.Update` treats any inactive director in Homecoming as done; `StartAfterPrologue` ignores `PlaceForColdOpen`'s return. | **Confirmed.** Already open and scheduled as the next fix. |
| R03 | Refused start stands the crew down first | `StartMission` calls `StandDown()` before `MissionManager.Start` checks prerequisite, playability and the story gate. | **Confirmed.** Gates themselves are sound; the caller's order is wrong. |
| R04 | ForceFail during a pending intro leaves a loan open | `ForceFail` stops the scene, clears `_pending`, and `_current?.Fail()` is a no-op; `Finish()` (which closes the loan) never runs; `Update()` returns early with nothing running. | **Confirmed.** Practical harm: free-roam capture stays suppressed until the next mission's `BeginLoan` re-snapshots. Dev-menu path only, but real. |
| R05 | M28's camera option promises concealment it does not deliver | The third option's delegate sets the baseline gap and size and nothing else. | **Confirmed.** |
| R06 | Generated map misattributes the choice to Ice's stage | `PLAYABLE-MISSION-MAP.md` row 2 lists `TechnicalChoiceObjective` under "Clear the transformer yard"; row 3 ("Read the cabinet") has the generic fallback. Cause: the generator splits on `yield return new MissionStage(` and the `_choice = …` declaration sits in the previous chunk. | **Confirmed.** |
| R07 | CI is narrower than the local verification | Workflow steps: build, lint, coordinates, regenerate-and-diff two docs. Story suite, regression suite, parser tests, both freshness checks and prebuilt identity are not in it. | **Confirmed.** |
| R08 | Required assets: audit rules, not method counts | Agreed. `EnterVehicleObjective` already rejects missing, dead and undriveable transport. The "19 files" figure from the build audit is a review queue. | **Agreed as stated.** |
| R09 | Handoff continuity is selective | Agreed; the class documentation now says exactly what each receiver restores. | **Agreed.** |
| R10 | Playtest doc and two instructions are stale | `PLAYTEST.md` lines 3, 84, 104–115, 153, 190, 216 still describe thirty missions, Numpad switching, a crane at Z 42, a yacht bilge and a T20. `CLAUDE.md:204` still says to keep solo consequences out of mandatory prerequisites. `CAMPAIGN-REMAINDER.md:63` says the main story never assumes SM07 happened, which the M63 gate now contradicts. | **Confirmed.** |
| R11–R14 | Production and design | No disagreement. Nothing verifiable at source beyond what `BUILD-AUDIT.md` already measured. | **Agreed.** |

Nothing in the external audit was wrong. It missed three things only the installed folder shows.

---

## 2. Findings from the installed game folder

### N1 — The shipped locations template overrides the repository's M01 geometry on every install

**Source:** `config/Bloodlines.Locations.ini`, `tools/windows/install-bloodlines.bat`, `LocationBook.Load`, your live `scripts\Bloodlines\Bloodlines.Locations.ini` (dated September 8, 15:17).

`config/Bloodlines.Locations.ini` is not an empty template. It ships 34 override lines: eleven `M01.*` keys and `Base.CypressFlats`, all at the bible-era positions from before the M01 hotfix, including:

| Key | Shipped override | Repository `locations.tsv` |
| --- | --- | --- |
| `M01.CraneNest` | 1082.0, -3175.0, **Z 40.0** (the bible's crane platform) | 976.6, -3239.5, 6.0 |
| `M01.ExitPoint` | 1180.0, -2990.0, 5.9 | 720.5, -2400.1, 15.2 (bible) |
| `M01.LowerDeckLedger` | 1010.0, -3196.0, 5.0 (legacy key) | migrated to `M01.ServiceTerminal` by `LocationBook` |

The installer copies this file once and then, correctly, never touches it again. `LocationBook.Load` applies it after `locations.tsv`, so on every installation that has ever been made, these override the repository's positions permanently.

**Consequences on your machine, confirmed by reading your live INI:**

- The lookout you asked to move sits 16 m from Ice in the repository and **140 m from Ice** in your game, because your INI puts it back on the crane. `ProloguePlacement.TryLand` then snaps Z 40 down to dock level, so Ice's "lookout" is a 140 m walk to a spot under a crane.
- `M01.ExitPoint` is a different extraction target from the one the repository, the tests and `PLAYABLE-MISSION-MAP.md` describe.
- Nobody surveyed these. They are the September 8 template, but the mod counts them as "your surveyed coordinates" and the log reports 156 estimates instead of 157 because of them.

**Why this matters beyond M01:** every position fix committed to `locations.tsv` for a key that appears in the template is dead on arrival for every existing install. The lookout change in `b3cad2d` was verified by 646 tests and never reached your game.

**Correction:** ship the template with comments only; the file is the player's survey overlay, not a second copy of the defaults. Add a lint that fails if `config/Bloodlines.Locations.ini` contains any `[Positions]` key. For existing installs, provide a one-time cleanup that removes template-identical keys (values byte-equal to the September 8 template) and leaves anything the player actually captured. Your backup from 21:03 holds the current file.

**Acceptance:** after cleanup, `Bloodlines.log` reports 157 estimates on load, M01's lookout resolves within 20 m of `M01.IceApproach`, and the mission map's exit point matches the repository.

### N2 — The QA checkpoint key is ScriptHookVDotNet's reload key

**Source:** `ScriptHookVDotNet.ini` (`ReloadKeyBinding=Insert`), `BloodlinesMain.HandleQaKey` (`Keys.Insert` → `CommitCheckpoint`), `docs/QA.md` row 5 ("Insert then Delete in a current mission").

With `[Dev] Enabled = True` (your current setting), pressing Insert to commit a checkpoint also tells SHVDN to reload every script. That tears the mod down mid-mission and re-instantiates it. QA row 5 cannot be run as written; the "false restore" it looks for would be masked by a full reload.

**Correction:** rebind the QA checkpoint keys (or read them from `Bloodlines.ini` like the gameplay keys), and update QA.md. F5 (SHVDN console) is also free of conflicts today; keep it that way.

### N3 — Dev mode hides the story player's pre-reunion behavior

**Source:** your `Bloodlines.ini` (`[Dev] Enabled = True`), `BloodlinesMain.ToggleDeployment` (`preReunion = !IsComplete("M01") && !DevToolsEnabled`).

You reported that F10 "switches over" to the crew on a fresh campaign. That is the dev-build behavior: with dev tools on, the Guess-only deployment before M01 is bypassed by design. A story player sees Ron alone. Not a bug, but the QA build never exercises the story path, so it should be tested once with dev tools off.

### N4 — Other scripts and key collisions (low)

`scripts\` contains only `Bloodlines.dll`; there are no other .NET scripts to conflict. Root ASI mods: Simple Trainer (`TrainerV.asi`), `RageOpenV.asi`, `DirectStorageFix.asi`, `HeapAdjuster.asi`. Trainer bindings that overlap Bloodlines only while its menu is open: Backspace (trainer cancel / Bloodlines abort hold), Enter (trainer select / scene skip), PageUp and PageDown (trainer columns / QA stage warp). Nothing collides in ordinary play.

---

## 3. What the live log actually shows (the only runtime evidence so far)

`scripts\Bloodlines\Bloodlines.log`, session 22:10:19, on the installed `78b48c2` build:

```
Loaded 161 campaign locations (156 still estimates).
Catalog built: 70 main + 9 solo, 36 playable.
Save loaded: 0 missions complete, act 1.
Story character stashed at X:-14.87 Y:-1454.46 Z:30.47.
Solo deployment: Guess at X:-1035.754 Y:-2732.531 Z:13.85.
Scene started: M01:prologue
Prologue started at LSIA; home is X:291.517 Y:-1078.674 Z:29.405.
Cue M01_SCENE_PROLOGUE_01_GUESS: Los Santos. Same heat coming off the runway. ...
Scene skipped; finishing its blocking and restoring player camera and controls.
```

What this establishes, and no more:

- Pressing J on a fresh save started the prologue through the intended path (not the dev menu).
- The LSIA curb resolved to walkable ground on the first attempt; Ron deployed alone there.
- The first line played and the deliberate-skip path (`Skip()`, not `Stop()`) ran.

What it does not establish: whether Ron ended seated after the skip, whether the drive marker appeared, whether the homecoming or the M01 hand-off worked. Notifications are not logged, and the log is silent after 22:10:36. The earlier 21:31 session shows a loan opening followed by `M01:intro`, which is the dev-menu "Replay M01" path bypassing the prologue by design.

Because of N1, any M01 result from this install reflects the September 8 template positions, not the repository's.

---

## 4. What to tighten, in order

1. **N1 first.** It invalidates M01 geometry testing on this machine and on every install. Template to comments only, a lint against positions in the template, a one-time cleanup of template-identical keys in existing installs. Then re-run M01.
2. **R01 + R03 + R04** — three small host-order fixes with integration tests at the `OnTick`/`StartMission` boundary. R01 is the prologue's missing GPS route; R03 protects the deployed crew from a refused start; R04 closes the loan on a pending-intro fail.
3. **R02** — the scene-outcome contract: `Update` honors `Failed`, Homecoming distinguishes completion from cancellation, `StartAfterPrologue` acts on `PlaceForColdOpen`'s result, fade-in in a `finally`.
4. **N2** — rebind the QA checkpoint keys away from SHVDN's reload key; fix QA.md row 5.
5. **R05 / R06** — give M28's camera option a real, tested effect or rewrite its promise; make `audit_campaign.py` bind objectives to the stage that yields them.
6. **R10 + N3** — one current playtest entry point (the prologue, dev tools off for the story path), stale steps archived; fix `CLAUDE.md:204` and `CAMPAIGN-REMAINDER.md:63` to agree with the gates.
7. **R07** — CI jobs for the story and regression suites, parser tests, both freshness checks and a prebuilt identity check.

Items 1–4 are one bounded branch. None of them touches M31+, dialogue, or the merged decisions.

---

## 5. What I did not do

No fixes, commits to the merged branch, installs, or changes to your INI, save or game files. This document is the only artifact. The external audit's Package A remains the right next step, with N1 added to the front of it.
