# Los Santos: Bloodlines

A single-player GTA V campaign mod: 70 main missions + 9 solo missions, three
playable brothers with switching and abilities, built from a set of design-bible
PDFs in `docs/bibles/`.

## The two rules

1. **Story Mode only. Never load GTA Online with this installed.** ScriptHookV
   presence in Online is a ban. Every helper in `tools/windows/` says so; do not
   add anything that could be read as making Online safe.
2. **Redistribute nothing Rockstar owns.** Extracted game data (zones, vehicles,
   peds) is fetched at runtime and cached under `build/`, never committed. The
   DLC asset pack ships as loose files for OpenIV to import, never as an `.rpf`.

## Build

```bash
python3 tools/package.py           # uses prebuilt/Bloodlines.dll, no SDK needed
python3 tools/package.py --build   # compiles first (needs the .NET SDK)
```

Targets .NET Framework 4.8 against `ScriptHookVDotNet3` 3.6.0 as a
compile-time-only reference. On Linux it cross-compiles via
`Microsoft.NETFramework.ReferenceAssemblies`; `DOTNET=/path/to/dotnet` points at
an SDK that is not on PATH.

**After changing C#, refresh the committed binary** or a machine without the SDK
installs a stale one — and then reinstall it, or the game keeps running the old
one no matter what the repo says:

```bash
dotnet build src/Bloodlines/Bloodlines.csproj -c Release
cp src/Bloodlines/bin/Release/Bloodlines.dll prebuilt/Bloodlines.dll
python3 tools/package.py    # then copy build/deploy/scripts/ over <GTA V>/scripts/
```

**No .NET SDK, but Visual Studio Build Tools installed?** `python3
tools/build_roslyn.py` compiles the same sources against the same pinned
references straight through Roslyn's `csc`, fetching them into `build/refs/`. The
build is deterministic, so `cmp` between `prebuilt/Bloodlines.dll` and the DLL in
`<GTA V>/scripts/` is a real staleness check rather than a guess. A stale install
is the single most expensive failure on this project: every symptom you then chase
belongs to a build that is no longer in the repo.

## Checks that must pass before pushing

```bash
python3 tools/lint_missions.py       # "No errors."
python3 tools/validate_locations.py  # "0 flagged"
```

The linter is the substitute for not having the game: it checks location keys,
dialogue cue ids against the firing mission, vehicle and ped model names against
public data dumps, catalog registration, crew deploys onto air or water, and
non-ASCII in the data files. It has caught real bugs that would have been
unfinishable missions in game (`new Model("mallard")` does not exist; the
Mallard's model name is `stunt`).

## How it fits together

```
BloodlinesMain (GTA.Script)   entry point, key handling, per-subsystem tick
DevTools       (GTA.Script)   QA harness, only with [Dev] Enabled = True
  ^ the ONLY two Script subclasses, on purpose
CrewRoster / SwitchController / CompanionController   the three brothers
DeathController the player going down; the engine cannot restart a custom ped
MissionManager -> MissionCatalog -> Mission / ComposedMission
  ComposedMission = stages of Objectives (18 kinds, in Missions/Objectives/)
CampaignData    loads data/*.tsv at runtime
```

**Why one DLL and not one per act:** SHVDN instantiates and ticks every class
deriving from `Script` in `scripts/` *and its subfolders*. Missions are ordinary
classes the dispatcher constructs on demand, so only the running mission ticks.
Per-act assemblies would not change that and would add 70 assemblies to
version-match. External mission packs are still supported through the `assembly`
and `class_name` columns of `missions.tsv`.

**Death is ours, not the engine's.** GTA's restart machine is written around
Michael, Franklin and Trevor. Hand it a ped that `CHANGE_PLAYER_PED` installed and
it fades out, finds no story character to restart into, and never fades back in —
a black screen that reads as a crash. `DeathController` pauses the engine's
restart for as long as the crew is deployed and runs the sequence itself, and
**hands it back on stand-down and on teardown**. Leave that release out and the
player's game is one where dying does nothing. Busted goes through the same path,
for the same reason: pausing the restart stops an arrest resolving too.

**Passive objectives.** `Objective.IsPassive` marks fail-only objectives
(Protect, Timer, SpeedFloor, AvoidDetection, AltitudeCeiling). They never
complete, so `MissionStage.IsComplete` ignores them. A stage built only from
passive objectives can never finish and `ComposedMission.Validate()` refuses it.
Nine stages hung forever before this existed.

**Data is regenerated, not hand-edited.** `tools/parse_bible.py <pdfs...>`
rewrites `data/missions.tsv`, `dialogue.tsv`, `anchors.tsv` and
`campaign_registry.json`. Fix the parser, not the TSV. It folds typographic
characters to ASCII because GTA's text renderer is not dependable outside it.

## Conventions

- TSV at runtime, not JSON: .NET 4.8 has no built-in JSON reader. The one
  hand-rolled reader (`Core/Json.cs`) exists only for `savegame.json`.
- Coordinates in `data/locations.tsv` are estimates until surveyed in game with
  the F11 capture; 154 of 159 are still estimates.
- Log with `Logger`, not SHVDN's log. Identical repeated errors collapse to a
  count — a tick-rate fault produced 100,000 lines before that existed.
- Every subsystem is stepped separately in `OnTick`. One failure must not take
  the rest of the mod down with it.

## Verified environment

Confirmed working on the **Enhanced** build (`GTA5_Enhanced.exe`) with
ScriptHookV .Net Enhanced 3.9.0.6 running the 3.6.0 target API. Legacy is what
the toolchain grew up on; nothing in the mission code is build-specific. The
script hook itself is NOT interchangeable between builds.

## State

49 of 79 missions have gameplay scripts (M01–M43, SM01–SM06); the rest are loaded as data
with no mission script yet. The code builds clean with `--warnaserror`.

Gameplay not implemented: M44–M70, SM07–SM09, the M55 switching prototype, interstitial
systems beyond the implemented homes/workbenches/dispatches, MLO interiors, custom peds,
voice lines.

Read `docs/PLAYTEST.md` before a play session and `docs/INSTALL.md` for install,
the Legacy/Enhanced split, and what to do when `Bloodlines.log` never appears.


## Recovery and survey contracts

Current missions have SupportsCheckpointRestore=false: a death fails the mission
and recovers only the active hero at the real pre-deployment position. Surviving
teammates retain their locations, health, transport and personal heat. Do not claim checkpoint recovery
until vehicles, entities, active character and private mission state can be rebuilt.
DeathController is frame-driven; gameplay must not tick while IsHandling is true.
Critical teardown steps must remain independently protected.

Recovery completion requires walking input and at least 0.5m of horizontal movement,
not just CanControlCharacter=true. While checking, normal gameplay and all switch
entry points stay blocked. After four seconds of attempted but blocked walking,
the single retry gives way to the existing emergency Story Mode return. Do not
reintroduce CHANGE_PLAYER_PED when the active ped already is the player.

Military dispatch is staggered at 4s / 12s / 20s (helicopter / convoy / tank),
subject to valid off-camera positions. Retry unavailable approaches after 3s.
Keep at most three managed units and replacement cooldowns; never clear dispatch
on every MissionActive=false assignment. See docs/MILITARY-PRESSURE-AND-RECOVERY.md.

Military losses now retire into bounded aftermath ownership, not entity deletion.
Keep explosions, falling aircraft and corpses physically intact; remove their
pursuit blips and release old wrecks to the engine. Custom law-enforcement groups
must be allied with COP and one another. Follow-mode threat checks defend every
crew member and refresh stale combat targets while preserving mission ownership.

All heroes share 900 health and 100 armor on spawn/recovery. Preserve actual damage
across switching. CompanionRecovery owns a separate 45-second free-roam downed timer,
including missing bodies; do not permanently suppress it after player death. Only
recover slots that were actually deployed, at off-camera navigable ground locations,
then let them travel back. Mission scripts retain their death/failure behavior.
See docs/MILITARY-INDICATORS-AND-TEAM-COMBAT.md for this combined update and retest.

Free-roam travel modes are Independent, Ride Along (seat sharing with overflow),
and Drive Alongside (separate transport). Never give overflow drivers a null
destination while the leader is in a vehicle: use the vehicle-follow task. Keep
mission-required shared rides and scripted drivers authoritative. CrewDriving
owns the role-specific road speed profiles. Transport acquisition may carjack
ordinary parked or slow-moving traffic, with crew/mission exclusions and bounded
interception attempts; never shoot a crew member to obtain their vehicle.

Survey destinations create a yellow GPS route; F7 explicitly teleports and F11 saves
on-foot captures. Bloodlines.Surveyed.ini loads automatically and overrides M01's
anchor-derived defaults. Mission and survey coordinates must share LocationBook.
Keep the raw DevTools capture from competing with the survey's capture key/HUD.
Companions reserve separate seats and attempt normal nearby entry before fallback.

Run `python tools/run_regression_tests.py` on Windows as well as the build/lint checks.
These source-level tests use GTA stand-ins and cannot establish live native behavior.


## Story, KJ and mission-marker update

Gameplay names are Ice, Gohan and Guess (`Protagonist.DisplayName`); full names are
reserved for deliberate story uses. KJ is a supporting NPC in SM03, with a second
appearance authored for the future SM09 script; never add him to `CrewSlot`.

`data/story_beats.txt` and `data/opening_scene.txt` are authored additions, distinct
from the original PDFs. `python tools/build_story.py` compiles `data/scenes.tsv` and
`docs/STORY-SCRIPT.md`; `--check` verifies freshness and coverage. Add scene data to
packages with the DLL. `mission_starts.tsv` maps implemented missions to existing
LocationBook keys, so surveyed overrides also move their markers.

CutsceneDirector runs before mission.Begin for briefings, and after completion for
aftermath. Never start objective clocks and then pause only their Update calls for
a briefing. M01's recognition explicitly starts its combat clock after the scene.
Scenes own temporary camera/control/entity flags and restore them on every exit.
Enter or controller A skips; the normal abort hold remains available. Exactly two
classes still derive from GTA.Script; the directors and marker services do not.

Switches within 80m avoid the aerial camera. Distant switches use a bounded collision
check and short fade; missionTransition preserves an already-owned mission fade.
Occupied crew transport must survive mission cleanup. M11 owns turbine installation;
M23 owns the permanent bunker unlock. Solo outcomes never gate a main mission's *scenes*;
progression gates live only in CampaignState.StoryGates (M19, M44, M63, M68).

Run the existing regression suite plus `python tools/run_story_tests.py`.
See `docs/STORY-AUDIT.md` for coverage, limitations and the live playtest sequence.


## M01 live-playtest correction

See `docs/M01-HOTFIX.md` (or `M01-HOTFIX.md` from this docs folder) for the
current exterior staging, camera fix, assigned M01 companion actions and controller
controls. The previous descriptions of bible anchors as surveyed geometry are
superseded: those proposed positions were not verified against installed assets.
`tools/test_dialogue_parser.py` protects speech extraction; authored revisions
live in `data/dialogue_edits.json`, applied by `parse_bible.py`.

Handoff implementation pass (September 9, 2026): docs/HANDOFF-IMPLEMENTATION-UPDATE.md is what was built from the two handoff documents and docs/CHANGE-REGISTER.md is the reconciliation table to keep current. Prologue (PrologueSequence), scene blocking (SceneBlocking), chapter handoff (OperationHandoff/HandoffLedger), TechnicalChoiceObjective and RequireAsset exist now; skipping a blocked scene must always leave the same state as watching it (CutsceneDirector.Skip completes blocking; Stop cancels it), and handoff records are session memory, never save data. Story gates live only in CampaignState.StoryGates; weapons a mission issues are loans returned by WeaponProgression.EndLoan at teardown.

Current campaign audit: see docs/CAMPAIGN-AUDIT-UPDATE.md, docs/PLAYABLE-MISSION-MAP.md, docs/PROGRESSION-GUIDE.md and docs/CAMPAIGN-REMAINDER.md. Rewards commit only in CampaignState.MarkComplete. Preserve per-character free-roam memory and full-restart retry semantics. Run tools/audit_campaign.py --check and tools/build_story.py --check after mission edits. data/mission_gameplay.tsv is an authored implementation overlay; do not overwrite the original bible extraction to describe adapted gameplay.


## Shops and street behavior
See docs/STREETS-UPDATE.md for the current service locations, prices, role handoffs and live-test limits. Shop transactions use crew cash and validate proximity, wanted state and actual application before charging. Do not present the mod shop as the vanilla clerk sequence. MissionHandoff uses frame inputs only; never reintroduce a persistent frozen ped to enforce a role switch. WorldTuning doubles the high-gear redline and now adds progressive speed-dependent torque per docs/PROGRESSIVE-POWER-UPDATE.md. The user permits extra power but wants gradual acceleration. Keep launches unboosted, cap and time-ramp assistance, and do not claim doubled measured top speed without road testing. Re-resolve shared handling through live vehicles/models before cleanup, and never multiply it per car. Keep police search timers and military dispatch ownership intact.

Current market/travel contracts: read docs/MARKET-AND-TRAVEL-UPDATE.md and docs/FEATURES-STILL-PLANNED.md. WeaponMarket derives shop locks from WeaponProgression.RewardMissions; never bypass milestone gates through regular shop purchases. Clothing stores preserve head hair. Wheel browsing restores both wheel state and cash; car mod slot 24 may be hydraulics. TravelHandling uses checked public Enhanced SDK properties and restores live data; never guess memory offsets or claim measured 2x speed from cap-setting tests.


## September 12 repair contracts

See docs/SHARED-SYSTEMS-REPAIR-2026-09-12.md. MissionManager opens CampaignState.BeginAttempt before a briefing, commits only on success and discards on abort/failure/teardown. Runtime evidence/cargo/access/fleet flags are provisional; Save serializes committed snapshots while the attempt is open. Early replays cannot rewind later state. MissionManager consumes CutsceneDirector.FinishedSequence because the host does not tick missions during scenes. Required scene failure must prevent the next gameplay tick.

MissionContextCard covers all 49 playable scripts; retry cards start after scenes. CompletionRewards compares pre-attempt entitlements, not just a hard-coded mission reward ID. ProgressionSnapshotTests exports build/progression-runtime.md and checks docs/PROGRESSION-GUIDE.md against runtime rewards.

UsePhoneStep binds to dialogue completion after its dialogue gate and includes a lowering beat. A short airport text check opts out; never make a pre-dialogue phone wait for dialogue that it gates. On-foot radio scenes use a phone; seated actors keep hands-free radio. Prologue enters the Mission Row shell through ApartmentAccess, makes the call inside and exits to the captured exterior before its dock handoff. New Apartment.MissionRow / Apartment.Room.MissionRow keys retire the Franklin room without overwriting old surveys. Preserve the surveyed LSIA car coordinates from PR #40.

VehiclePanelDamage owns no handling memory: sample at most 32 nearby live cars, require a collision and finite directional velocity change, exclude warps/large tick gaps, cool down per entity, and preserve the engine/body health already determined by the real impact. VisualAtmosphere's ped/vehicle LOD and vehicle shadow natives require the target entity plus the setting.


September 12 playthrough/garage follow-up: see docs/PLAYTHROUGH-GARAGE-UPDATE-2026-09-12.md (PLAYTHROUGH-GARAGE-UPDATE-2026-09-12.md from docs). Briefings preserve the actual arriving car until gameplay deployment; M03 rescue explicitly allows any brother. Garage/direct car sales share ten successful sales per GTA day, persisted with cash. Updated payout tables are generated from runtime rewards; replays remain unpaid. Walkable garage interiors and berth/hangar storage remain planned.

Mission placement editor: see docs/MISSION-PLACEMENT-EDITOR.md. SurveyMode keeps drafts separate until an atomic survey save. Only explicitly wired groups expose count/radius; optional editor slots preserve dynamic mission defaults until surveyed. Exclude unsaved editor-only slots from generic mission-ground preparation. M05 location-test mode is a user-controlled diagnostic override; it does not bypass missing essential assets.


September 12 convoy/HQ/tour follow-up: see docs/M09-FOUNDRY-SURVEY-2026-09-12.md and docs/COORDINATE-REFERENCE-WORKFLOW.md. M09 arrival is an explicit tracking completion path, with visible range/progress and bounded convoy route retries. Foundry HQ uses CrewHomes/ApartmentAccess and existing cypressFoundry ownership; restore prior entity-set states on exit, timeout or cancellation. Keep its furniture markers estimated until surveyed. Placement tours keep the menu and queue active through saves/teleports; navigation must not discard unsaved drafts. Imported map-reference candidates are not surveyed geometry.

## September 13: authorized M31–M35 package

The user explicitly approved building the five chapters together. See docs/OFFSHORE-PREPARATION-M31-M35.md for role flow, first-pass substitutions, rewards and live checks. PreparationOperation owns inactive crew during each chapter; ConvoyRouteObjective follows the patient vehicle while allowing the player in either escort vehicle. The half-track has three seats, so Gohan drives separately. Cargo and Ramos must reach physical delivery before their story state commits. M36 onward remains unimplemented. Automated completion is not live acceptance.

## September 13: next planned block, M36–M40

The user requested the next steps after M31–M35. M36–M40 now have scripts; see docs/MARINE-PREPARATION-M36-M40.md. CoastalOperation reuses owned crew roles and adds sub/pickup preparation. Seabed sensor/junction props and explicit depth guidance replace unseen offshore structures. M37 visibly tests both smoke releases; AircraftSmoke is a finite non-looped effect with free-roam input gated by menus/missions and a 12-second cooldown after the M37 unlock. M38 transports four attached packages. M40 uses Tropics with finite hull reinforcement and carried passenger weapons, not fictional mounted guns. All five use full restart and first-completion rewards; no seamless handoff or persistent marine hangar/berth is claimed. M41 onward remains unimplemented.

## September 13: final preparation, M41–M43

The next-steps request now adds M41–M43, bringing the current runtime to 49 jobs. See docs/FINAL-PREPARATION-M41-M43.md. SubmarineAirdrop owns verified attachment, two canopies, drag, bounded descent, streamed deep-water validation and cleanup. M42 begins airborne on an external ventral cradle, not inside an unverified internal cargo-bay fit. Gohan remains in the sub throughout; switching opens only after stable splashdown. M43 validates prior completion, evidence and actual asset positions before recording readiness. It does not spawn a rig or pay the future vault haul. M44–M70 and SM07–SM09 remain unimplemented.

## Campaign phone (September 13)

`CampaignPhone` is a native overlay, not a physical handset replacement. See `docs/CAMPAIGN-PHONE.md`. It reads completed-job dispatches, current mission instructions, contacts and shared cash. `ControllerInput` respects its focus; the overlay alone reads raw disabled controls. The main tick handles input before consumers and resolves route requests after `ObjectiveMarkers.EndFrame`. Never use ped freezing, task clears or time-scale changes for this UI. Close it on recovery, cutscenes, required role handoff, other menus and stand-down; cancel queued actions. Keep story-call animations independent. Defaults enable it on existing INIs: D-pad Up / F6, D-pad and A/B navigation. Existing saves require no migration.

## Campaign hub apps and Foundry board

`CampaignHub` supplies the second phone app page and the Foundry planning table. See `docs/CAMPAIGN-HUB.md`. Garage actions delegate to the existing GarageService; paid requests/cancellations retain a quote token and revalidate it after mission update. Never grant a vehicle from a UI record alone or replace mission-owned companion tasks with a free-roam order. The journal reads runtime context cards and completed synopses; planning checks recorded flags plus evidence/cargo and labels active attempts provisional. The existing Foundry table/menu opens `DevMenu.CampaignPlan`; it is not a new interior model. `CampaignState.PhoneHistory` stores up to 60 alerts with read flags. Report actual successful transactions only, never previews or duplicate confirmations. Eight additional dispatches are gated by their source jobs. No placements or mission scripts changed in this package.


## Visual atmosphere comparison pass

See docs/VISUAL-ATMOSPHERE-PASS.md. TimecycleGrade acquires only an empty script
grade slot, checks the resulting index, fades through neutral between names and
yields to observed foreign indices/transitions. Never clear an unowned grade or
claim index acceptance establishes visual quality. World menu grading A/B is
session-only; it does not change weather/time/water/LOD, maps, config or saves.
Preserve the default ungraded dusk. Higher-priority suspension is immediate, not
a delayed fade. No-getter graphics settings still have compatibility limitations.
