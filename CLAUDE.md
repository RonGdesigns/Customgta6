# Los Santos: Bloodlines

A single-player GTA V campaign mod: 70 main missions + 9 solo missions, three
playable brothers with switching and abilities, built from a set of design-bible
PDFs in `docs/bibles/`.

Current status: all 79 gameplay scripts exist. Dated sections below preserve the history of decisions and repairs; earlier statements that later missions are unimplemented are historical. See `docs/FEATURES-STILL-PLANNED.md` for current remaining work.

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

**All 79 missions have gameplay scripts** (M01–M70, SM01–SM09). Live-playtest corrections are documented below; automated coverage does not establish live acceptance of every mission. The code builds clean with `--warnaserror`.

Gameplay not implemented: interstitial
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

The user requested the next steps after M31–M35. M36–M40 now have scripts; see docs/MARINE-PREPARATION-M36-M40.md. CoastalOperation reuses owned crew roles and adds sub/pickup preparation. Seabed sensor/junction props and explicit depth guidance replace unseen offshore structures. M37 requires both smoke releases to be operated with their canisters attached, and records per aircraft whether a plume was actually seen; a particle call that returns false is reported, never a failed mission. AircraftSmoke is a finite non-looped effect with free-roam input gated by menus/missions and a 12-second cooldown after the M37 unlock. M38 transports four attached packages. M40 uses Tropics with finite hull reinforcement and carried passenger weapons, not fictional mounted guns. All five use full restart and first-completion rewards; no seamless handoff or persistent marine hangar/berth is claimed. M41 onward remains unimplemented.

## September 13: final preparation, M41–M43

The next-steps request now adds M41–M43, bringing the current runtime to 49 jobs. See docs/FINAL-PREPARATION-M41-M43.md. SubmarineAirdrop owns verified attachment, two canopies, drag, bounded descent, streamed deep-water validation and cleanup. M42 begins airborne on an external ventral cradle, not inside an unverified internal cargo-bay fit. Gohan remains in the sub throughout; switching opens only after stable splashdown. M43 validates prior completion, evidence and actual asset positions before recording readiness. It does not spawn a rig or pay the future vault haul. M44–M70 and SM07–SM09 remain unimplemented.

## Campaign phone (September 13)

`CampaignPhone` is a native overlay, not a physical handset replacement. See `docs/CAMPAIGN-PHONE.md`. It reads completed-job dispatches, current mission instructions, contacts and shared cash. `ControllerInput` respects its focus; the overlay alone reads raw disabled controls. The main tick handles input before consumers and resolves route requests after `ObjectiveMarkers.EndFrame`. Never use ped freezing, task clears or time-scale changes for this UI. Close it on recovery, cutscenes, required role handoff, other menus and stand-down; cancel queued actions. Keep story-call animations independent. Defaults enable it on existing INIs: D-pad Up / F9, D-pad and A/B navigation. F6 belongs to the DLSS 5 add-on; d-pad Up is ignored while a sniper scope is up, because the scope zooms on it. Existing saves require no migration.

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

## Continuous operations, and Paleto as the second one

An operation is several authored chapters the player experiences as one sitting:
one entry, one world, one loan, one result. `OperationSpec` names its chapters and
which one ends it, `MissionOperations` is the one table of them, and
`ContinuousOperation` plus `OperationWorld` carry the sitting itself — the phase
walk, the scene guard, the two validation gates, the single award and the
reverse-order teardown. `PortHeistOperation` (M19–M22) and `PaletoOperation`
(M44–M48) are thin subclasses. Adding another is a row in that table and a row in
`MissionOperations.CreateFor`, never a copy of the manager's special cases.

Never chain an operation's chapters in `MissionManager.Continuations`: an ordinary
continuation tears the world down and starts the next mission normally, which is
the opposite of one attempt. Record an operation's result only through
`CampaignState.CompleteOperation`. A chapter must also stage its own vehicles when
no parent carried them in, so it can still be opened alone in QA.

Paleto happens on the vessel anchored in Paleto Cove, because the authored
offshore rig does not exist in the installed game. `PaletoSite` holds the keel,
deck and seabed heights that were read out of the archives, and
`docs/story-to-play/pass-02/SITE-REPORT.md` records where each came from and what
is still unverified. Its map is script-loaded, so `ScriptedMap` owns the request
for the length of the attempt: release only names this instance turned on, and
release on pass, failure and abort alike. Interior standing points are estimates
derived from verified extents and still need an F11 pass; no automated check here
is live acceptance, and nobody has walked on that deck yet.

## September 14: the reported playtest failures

Six missions from Ron's September 13 reports are repaired; see
docs/CHANGE-REGISTER.md for each cause. The contracts worth keeping:

A vehicle has the seats the game gives it. `VehicleSeat.LeftRear` does not exist
on a two-seat Benson, and asking for it failed M38 outright. A third passenger
rides in the cargo box through `Core/CargoRide`, which is M03's solution made
shared. The story stand-in now reports real seat counts from a small table, so a
seat the game does not have can no longer pass this suite.

People who are going straight into a seat are created with `Occupant`, not the
guard spawner: a convoy driver needs a seat, not walkable ground, and asking for
standing space at two shared points is what made M35 refuse to load.

Never seat a ped with `Task.WarpIntoVehicle` and then immediately issue another
task. The warp is queued, the next task replaces it, and the ped is left loose —
in M26 that was two pilots and two aircraft falling out of the sky at 220 m. Use
`SetIntoVehicle` and verify the seat took.

Ground preparation moves points sideways: `MissionSites.Ground` calls
`GetSafeCoordForPed` and writes the result back. Never compare a location key to
its own earlier value and treat the difference as a survey error, which is what
stopped M43. Check the real footprint with `PlacementContract` and report it.

Cosmetic failure is not mission failure. Ask for a particle dictionary across
several frames, and if a plume still will not render, record it and carry on.

## September 14: guard awareness

`Core/GuardAwareness.cs` models what a mission guard knows: Unaware, Suspicious,
Investigating, Detected, Alarmed, moved by graded stimuli (sighting, heard shot,
suppressed shot, taking fire, a body found, a radio call, trespass) and decayed
when nothing feeds it. It owns mission hostiles only; police and military dispatch
stay with `TacticalResponse` and `MilitaryResponse`.

Two rules it exists to enforce. A hostile reacts to what happens to **him** whether
or not a mission has set `Fighting`: taking fire escalates him with no line of sight
required. And a combat task is issued **once**, on a state change or a genuinely
stale order — re-issuing `Task.FightAgainst` every tick restarts the task before the
ped can act on it, and that plus the `Fighting` gate is why the guards in M31, M33
and M37 stood still while Ice shot at them.

`PreparationOperation` owns one instance and updates it every frame, deliberately
above that class's 2.5-second order throttle: awareness bounds its own cost with a
review interval and a per-tick slice, so a guard cannot wait on an order clock to
notice he is being shot. A mission that sets `Fighting` reaches its hostiles as a
radio call rather than bypassing the model. Line of sight is an entity-to-entity
question: use `CanSee` for a person and `InView`, which tests facing and range only,
for a reported position.

`Suppressed` makes suspicion decay instead of climb and stops radio propagation,
while taking fire still lands. That is the hook Gohan's Blackout uses, and it is why
the framework had to exist before that ability could be built.

## September 14: the ability rework

Ron reassigned two abilities. **Ice** holds Thermal Pulse, the see-through sight:
he is the shooter and does the long-range work, so it is a marksman's instrument.
It replaced "Overwatch Focus", which described itself as a steadier gun and was in
fact a flat damage multiplier with slow motion, steadying nothing. **Gohan** holds
**Blackout** in exchange, and Guess keeps Slipstream Reflex.

Blackout's rule, which Ron specified: heat earned during a blackout is **deferred,
not canceled**. Enter with no wanted level and the dark conceals him; what he does
in it is held rather than charged. Leave nobody able to report it and, after
`ConcealmentGraceMs` unseen, the held heat is dropped. Be recognized in that window
and it lands in full. Entering already wanted means the lights still go out and
nothing is concealed. `GuardAwareness.SuppressAll` is how it reaches mission
hostiles it has no reference to, and a bullet still gets through.

An ability must be usable in free roam, not only inside a mission. Ron rejected
three mission-bound proposals for that reason; check free-roam usefulness before
proposing one.

`Core/WorldLights.cs` owns the artificial-lights switch by holder name, because the
engine has one switch and no getter: M04, M16 and Blackout can all want darkness at
once, and the lights now return only when the last holder lets go. Never call
`SET_ARTIFICIAL_LIGHTS_STATE` directly.

Two authored bible synopses, M04 and the solo SM02, still say Gohan uses Thermal
Pulse. The bible extraction is never edited to follow gameplay, so the adaptation is
recorded in `data/mission_gameplay.tsv`: in M04 his Blackout is the same breaker
beat from his own side, and in SM02 it kills the biometrics and holds the guards'
awareness down while he taps the node.

**Nitrous was torque and nothing else.** `SET_VEHICLE_CHEAT_POWER_INCREASE` raises
acceleration; it does not move a top speed. And `WorldTuning` already lifts every car's entity
cap to its doubled redline, so the ceiling was the same number with the bottle open or shut —
which is exactly why it felt like acceleration only. `WorldTuning.ApplyCeiling` now raises that
cap per instance while boosting and puts it straight back, written only when the number
changes. It never touches `InitialDriveMaxFlatVelocity`, which is shared handling data.
**Whether the terminal speed visibly moves is a road test, not a number to read off a file.**

**The exhaust flame is gone, and repeating a non-looped effect is not the way back.** Ron
asked for continuous fire out of the pipes and pointed at the gear-change backfire as the thing
to extend. `Core/ExhaustFlame` fired `core` / `veh_backfire` on a 70 ms cadence, and in play it
read as flashing rather than burning — which is what a one-shot puff repeated on a timer looks
like, at any interval, because each burst has its own fade. He asked for it removed. Anything
that tries again needs a genuinely looped effect and an owned handle released on every exit
path, not a faster cadence.

**A source-text assertion must match a call, not a word.** Three checks in a row failed on
their own documentation: `Contains("LaunchAirborne")`, `Contains("BoardBrothers")` and
`Contains("Script.Wait")` all matched the comment explaining why the code does *not* do that.
Match `Foo(` when the point is that something is called.

**A phone list row draws its title and its subtitle. Nothing else.** An entry with
`Children` can never show a body at all: selecting it pushes into the folder and returns, so
`LiveBody` on such an entry is dead code. The first attempt at vehicle specs put them exactly
there — which is why buying a car on the phone looked untouched even though the block existed
and was installed. Anything the player needs **while choosing** belongs in the subtitle
(`LiveSubtitle`, computed each frame so a streaming model fills itself in); anything he opens
for detail needs a leaf row of its own. A story test drives the real phone to both.

Positional assertions on hub rows are fragile for the same reason: a test that took
`Children().First()` broke the moment a row was inserted above it. Pick by what the row does
(`First(e => e.Action != null)`), not by where it sits.

**A car can be bought in two places, so both of them show the ratings.** `Core/VehicleSpecs`
reads the model-level `GET_VEHICLE_MODEL_*` natives, which answer without a car in the world,
and its `Rows` is the one place their wording and rounding live — `Block` composes them for a
phone page and the Premium Deluxe floor in the world menu adds them as its own rows. The first
pass wired only the phone, so Ron walked into the dealership, saw a name and a price exactly as
before, and reasonably concluded the work had never been done. **When a feature has more than
one surface, wire every surface or the feature does not exist.** A story test now refuses a
car-buying page that does not ask for the specs, so a third surface fails until it does too.

A model still streaming in yields no rows rather than a row of zeroes, and an all-zero answer is
never cached: a car permanently listed with no engine is worse than a page that says it is still
reading.

The mod-shop performance panel is translucent and sized to its content. It used to
be alpha 235 over a fixed 510 pixels, sitting exactly where the vehicle preview is,
so the part being fitted could not be seen.

## September 14: M26, M27 and M35 from the playtest

**A parked aircraft has to start when he gets in.** M26's Lazer was spawned with its
engine off so the scramble means something, and nothing ever started it: Ron could
fire its guns and never accelerate. It starts when he is aboard, checked every frame
so a re-entry behaves the same, and stays cold while nobody is in it.

**Check the numbers before asking for a chase.** M27 asked Ron to catch a Shamal
(91) in a Duster (69). It flies a Vestra now: two seats, 97, and a small private jet
belongs beside another one. Approach-aircraft choices come from build/vehicles.json,
not from what sounds right.

**Never seat a ped with a queued warp and then issue another task.** M27's Shamal
pilot had the same defect as M26's spotters: `Task.WarpIntoVehicle` followed by
`StartPlaneMission`, so nobody flew the jet, it came down, and the objective marker
showed the target on the ground. `SetIntoVehicle`, then verify the seat.

**M35's positioning.** The far-exit block sat 195 m past the kill zone, so Guess
drove away from the ambush and then back to it, and a roadblock that far down the
road closes nothing. It is at the south end of the zone: it actually shuts the road
and leaves him beside the truck he is about to take.

**A gun in the bed is for using.** M35 used to order every hostile out of their
vehicles at the trap, including the man on the technical's mount. The escort's crew
still dismount from a dead vehicle; the gunner stays and works the gun, on a
cooldown rather than every frame.

**The run home is a fight.** M35 set `Fighting = false` for the drive to the bunker,
which is what stopped Ice engaging anything from the gun seat. It stays on, a pursuit
follows from the convoy road, and switching is unlocked so either brother is playable
while Guess drives.

`PreparationOperation.KeepDriving` reissues a driver's route only on a changed
destination, a real stall, or a slow refresh. Handing `DriveTo` to a driver on a
fixed clock restarts the drive task and is a good way to make him hesitate short of
where he was sent.

**The save no longer fails silently.** `CampaignState.Save` wrote to a temporary file
and swapped it in with `File.Replace`, and swallowed `IOException`. Windows throws
that transiently often enough — an antivirus or the indexer holding the file for a
moment — and the campaign simply did not save, with one log line as the only trace.
It retries, then writes over the target directly, and records `LastSaveFailed`. This
was also the cause of an intermittent story-check failure that came and went between
runs.

## September 14: surfaces, held aircraft, and three placements

**Ground preparation corrects a point; it does not relocate it.** `MissionSites`
accepted the engine's walkable ground up to 35 meters away and 25 down. Over water
that answer is the sea beside the structure, which is where Ron found M40's hull kits
and navigation laptop: under the pier. The drift is 12 meters for an estimate and 3
for a point he surveyed himself.

**A built surface declares itself.** `Prepare` has always taken `fixedSurfaceKeys`
and almost nothing passed any. `PreparationOperation.FixedSurfaces` is the override;
M40 names its pier keys. Anything on a deck, platform or vessel belongs there,
because asking the engine for walkable ground near it gets the water.

**Nothing is stacked on nothing.** `PropPlacement.OnTop` read the surface model's
dimensions, and the spawn helpers release their model as soon as the prop exists, so
those dimensions can be zero: the item then sits at the surface's own origin, inside
it. That is M43's laptop, which was created every time and never visible. A surface
that will not report its height is assumed to be a working height, and it says so.

**An aircraft a brother is flying keeps flying.** `Core/AircraftHold` puts him in a
holding pattern while the player is somebody else, reissued on its own cadence, and
releases the instant the player takes it back. M42 had this for its Titan alone; M45
uses the shared one now. Switching away from a crew helicopter used to leave nobody
flying it.

Three placements moved on evidence rather than guesswork: M40's pier keys keep their
height, SM06's start and fuel tractor left the inside of the Ammu-Nation building
they were spawning in — the archives put its geometry at z 20 to 25 against their
authored 19 — for open ground verified empty within 16 meters, and M32's brothers
ride across the base with Guess instead of being left where the fight was.

## M51, and the charges that are not fired

Palmer-Taylor is wired and left that way. The owner chose to hold the outage for the
downtown tower offensive, so `M51BlackoutProtocol` records readiness as cargo
(`downtownBlackoutCharges`) and **never touches `Core/WorldLights`**. Do not "finish"
M51 by blacking the city out: the lights going out is a later mission's beat, and firing
it here spends it on an empty street. The authored line `M51_S1_03_GOHAN` counts the
charges down and calls the blackout, so M51 does not fire it; it belongs to whichever
mission actually triggers the sequence.

The bible's six 500kV step-down transformers are not props on that site — its switchyard
is baked map geometry. Six real plant units stand there in two clusters of three at yard
level, 125 m apart, which is the two-section arrangement the plan asked for; those six
are the charge points, declared in `FixedSurfaces` so ground preparation cannot move a
marker off the tank it belongs on. The divergence is recorded in
`data/mission_gameplay.tsv`, never by editing the extraction.

Its three jobs — west bank, east bank, interlocks — are parallel objectives in one stage
with a named brother on each. That is deliberate: the dispatcher only demands a switch
when the brother the player holds has nothing left to do in the stage, so all three are
open in any order. Three stages would have forced his hand three times.

Site geometry for the whole Act III opening block is in
`docs/ACT3-OPENING-MAP-M49-M53.md`; M49, M50, M52 and M53 remain unimplemented, and
M53's coordinates cannot be authored offline at all — nothing is placed under Pillbox Hill.

## M52, and where the shot is actually taken from

The bible puts Ice on the Union Depository roof "across the plaza" from City Hall. Those
two buildings are **716 m apart** — not a plaza, and not a shot. M52 uses the roof 62 m
from the steps and 15 m above them, which Rockstar gave a ladder named
`bh1_16_ladder_mission_fizz`: a ladder placed for a mission, so the climb and the way down
both already exist. `M52.Roost` is a fixed surface; ground preparation would put it on the
street.

Harrison comes out of the steps and walks 43 m **away** from the roost to his clear-shot
mark, not toward it. Check that when moving either key: a mark closer to the roof than the
steps turns a rooftop shot into a man walking into the muzzle.

Its machinery is M41's on purpose — identify, wait for a clear shot, eliminate, extract —
including the two failure paths M41 was given after a live report: shooting before the
confirmation, and firing into the escort before he is clear. Do not reimplement that
sequence; it would only find the same bugs again.

The superbike seats two and that is all this needs: only Ice and Guess are at the plaza,
Gohan is on the radio. An earlier planning pass read the bike as a conflict with a
three-man extraction; there is no third man there. The getaway is a `LoseWantedObjective`
rather than a coordinate, because an assassination ends when the response loses you.

The district text in `data/locations.tsv` is machine-read: City Hall's plaza is in
**Burton** and the roof is in **Rockford Hills**, whatever the building is called. A hint
that leads with anything else makes `validate_locations` resolve the wrong zone.

## M49 and M50: derived geometry, and two bounded results

**M49 does not author its checkpoint.** Roads are baked terrain, so no placed-entity
survey can find the Great Ocean Highway. `M49.Checkpoint` is a seed; the real lane comes
from `GameUtils.NearestRoadNode` at runtime, and the concrete, the APCs and the spotlight
towers are all offsets from that lane and its heading. One seed can be wrong; six
separately authored points can each be wrong on their own. A missing node is reported to
the doctor and the seed used — a checkpoint slightly off the road is recoverable, a refused
mission is not.

The seam is a **gap left in the concrete**, not a hole punched through it. The 90-mph ram
the bible describes is refused by `CAMPAIGN-REMAINDER` and stays refused: a barricade that
only yields to a collision is one the player cannot fail at honestly.

**M50 happens on a street.** There is no municipal archive in Rockford Hills — the zone is
mansions — and no walkable records interior in the installed game, but the synopsis is
already a conduit tap, so Gohan splices a real placed street cabinet. Its security is
**contained, not killed** (`NonlethalGuards` + `SubdueTargetsObjective`): the story wants
the crew to have been there without leaving bodies in Rockford Hills.

Its result is bounded and must stay bounded: **the coordinated municipal feed is
invalidated; local and physical copies remain, and nothing clears a wanted level.** The
authored line says it outright — "that buys us time, not an acquittal".

Three authored lines across these two describe places that do not exist — M50's vault
sub-level with turrets and a left corridor, and M51's detonation countdown. They are not
fired, because narrating a place the player is not standing in is worse than silence, and
each divergence is recorded in `data/mission_gameplay.tsv`. Never edit the extraction to
follow gameplay; an authored revision goes through `data/dialogue_edits.json`.

**M53 was called unbuildable here twice, and that was a wrong test, not a wrong answer.**
See the section below. Searching below z = 0 under Pillbox Hill finds nothing because there
is nothing below z = 0; the metro runs at z 13, twenty meters under a street at z 31.

## Three presentation and placement rules, learned the expensive way

**A placed prop's origin is not a floor.** M51 held its six limpet points and M52 its roof
roost at the archive heights of a tank and a ladder, and both produced markers floating in
the air that the player could not reach. A point a brother has to *stand at* belongs to
ground preparation; `FixedSurfaces` is only for a surface the engine's walkable query would
answer wrongly, and even then the height is probed down onto the real slab with
`MissionSites.OnSurface` rather than trusted. The same sentence already appears in this file
about the metro, and it was still got wrong twice.

**A blip belongs to its ped.** `ped.AddBlip()` creates an independent entity that outlives
the ped, so a dot sits over a corpse and tells the player there is still a fight. Attach
through `Core/TargetBlips` and call its `Update` each frame; `PreparationOperation` already
owns one as `Blips`. A story test refuses `Track(ped.AddBlip())` anywhere in the mission
sources.

**A brother's markers are his own.** A stage with parallel objectives drew every one of
them at once, so there was no telling which marker was being asked for.
`ComposedMission.OnUpdate` sets `ObjectiveMarkers.Suppressed` around any objective whose
`RequiredCharacter` is not the active slot: the objective still updates, only the drawing
waits.

## DLC map data has exactly one owner

Story Mode does not have the DLC map archives registered, and until one native has run,
`REQUEST_IPL` on DLC content silently does nothing. **Always go through `Core/DlcMaps`** —
`RequestIpl` for a map, `EnsureRegistered` before an interior.

Three separate places used to call that native themselves, and the bunker's map blip ran it
on the first frame of every session. Removing that startup call fixed a loading screen and
broke the Paleto heist, because the cove yacht is Cayo Perico map data that had been relying
on the bunker to register it. Nothing said so, in either direction. A story test now refuses
`Hash.REQUEST_IPL` and the raw registration hash anywhere outside `DlcMaps`.

**The registration is back on the first frame, and that is Ron's decision.** The pause cannot
be made free, so the choice is only where it lands: one he expects at startup, or one he does
not expect while driving up to the bunker. He chose startup. `BloodlinesMain` registers on its
first tick and loads the bunker exterior too when he owns it, so nothing pauses later. Every
call site still goes through `DlcMaps` and is still correct — they simply find the work already
done, which is what keeps M44's cove yacht working.

This loads map data the game already shipped with. It does not join or enable GTA Online.

## M54, and what the archives will and will not tell you about a roof

M54 was an express-elevator breach into a foreclosed penthouse. Ron replaced it with **roof
access by air**, and the rewrite is worth reading as a method rather than as one mission.

The penthouse version had to put three work positions inside an MLO nobody has walked: the
only two penthouse interiors the installed game loads are already crew homes, and the
`Apartment.Room.Luxury.*` keys were never surveyed, so the roosts were offsets from the
arrival point snapped with `GetSafeCoordForPed`. Honest, and unfalsifiable. The roof version
stands on placed geometry the whole way up — `dt1_02_helipad` at (-142.67, -593.35, 206.31)
on a Pillbox Hill tower, the parapet rail beside it, the `prop_elecbox_23` cabinet Gohan
taps, and a ring of `prop_wall_light_03a` at 209.15 that outlines the deck at roughly twelve
meters square. Every authored point is inside that rectangle and borrows its x and y from
something Rockstar put there.

**What a prop gives you is a position, never a floor.** Each roof point's height is a
downward probe onto the slab, the M52 fix applied before the bug rather than after it.

**A probe only answers where collision is loaded.** The roof is two hundred meters up and
sixteen hundred meters from the departure yard, so the probe runs when the crew has landed,
not in `Setup`. A story test asserts that no `OnSurface` call appears inside `Setup` in that
file.

**A roof point must be a fixed surface.** `FixedSurfaces` names all four. Ground preparation
asks the engine for walkable ground and would answer with the sidewalk two hundred meters
below — the same failure that put M40's hull kits under a pier.

**The tower is deliberately not Maze Bank.** Maze Bank's roof (`dt1_11_heliport`, 323.26) is
the only other helipad in the city core, and `M54_S1_02_ICE` claims a firing line *on* Maze
Bank Tower. Standing on a different Pillbox Hill roof keeps the authored line true. The same
building's lower tiers — `prop_radiomast01` on a deck at 199.13, three more pads at 175.52 —
are unused, because a radio mast would be a better antenna and nothing in the archives shows
a man can walk down to it.

`M54_S1_01_GUESS` clones an elevator keycard that no longer exists. It goes unplayed and
`M54_RADIO_01_GUESS` calls the roof approach instead; `data/mission_gameplay.tsv` records
that, the tower swap and the unreproduced balcony. Nobody has stood on that deck yet.

One trap worth keeping named: `MultiHoldObjective` **copies its site list in its
constructor**. `BuildStages` runs before a mission has been anywhere, so a list filled in
later is captured empty and the objective completes the instant its stage opens. Use
interactions with `Func<Vector3>` positions for anything discovered at runtime.

## M53, and the depth you measure from

Two planning passes wrote M53 off as impossible to author offline, both on the same
evidence: not one placed entity sits below z = 0 under Pillbox Hill. That was a correct
reading of a question that did not matter. **Downtown street level is about z 31 and the
metro runs at z 13** — twenty meters under the street and eighteen meters above the sea.
The tunnel was never hidden; the search floor was in the wrong place. When a site "does not
exist" in the archives, check what height you are searching from before believing it.

What is there is a whole line: `metro_station_3_seoul` (-497.73, -673.53, 13.64),
`metro_stat3join1` (-437.69, -675.41, 13.64) where platform becomes tunnel, ninety meters of
straight `metro_t_*` sections whose origins are every one of them at 13.03, the bend east at
`metro_t_stair` (-341.91, -682.70), `metro_newwalk1` (-470.15, -714.52, 22.51) for the walk
out and `kt1_09_seoul_subway` (-490.29, -714.59, 25.97) for the way in. The run is in the
**Downtown** zone and the platform end in **Little Seoul**; the district text is machine-read,
so it names those and not Pillbox Hill.

**A flat floor is measured once.** Every section origin on that run is at 13.03, so one
downward probe at the carriage describes the whole tunnel and its offset moves the other
sixteen points. Seventeen probes would be seventeen chances to fail and seconds of `Script.Wait`
inside `Setup`, which is exactly what put M45's helicopter in the sea.

**Underground, the walkable-ground query does not miss — it lies.** `Guard` accepts an answer
up to 35 meters away, and from a tunnel twenty meters down that answer is Vespucci Boulevard,
comfortably inside the tolerance. All eight contractors would have spawned in traffic. `Guard`
now takes `trustPoint` and `PreparationOperation.EnemyAt` passes it; `Enemy(key)` is untouched,
because sixty missions depend on the snap it does.

**`Core/NightVision` owns the goggles the way `WorldLights` owns the lights.** One switch, no
getter, held and released by name. It matters more than the lights did: night vision left on is
a green screen the player cannot clear from any menu, on a save he keeps playing, so the release
lives in `OnCleanup` where pass, failure, abort and death all pass through.

The authored line counts the enemy — "sweep team of eight" — so there are eight, two squads of
four. A spoken number is a contract. The one judgment in the placement is the three meters
across the bore between the carriage and the crew; nobody has walked that tunnel with F11.

## M56, and reading a site off what people threw into it

The Los Santos River channel is baked terrain, the same problem M49 has with the Great Ocean
Highway: no placed-entity survey finds a road or a riverbed. Two things made it authorable
anyway, and both generalize.

**The site's own section models draw its path.** `sp1_12_riv_01` through `riv_11` run from
(-825.0, -1614.0) to (54.1, -2135.4), which is the whole channel. **Their z values are
useless** — a section origin is the middle of a twenty-eight-meter box, so one reads 22.52 and
its neighbor -0.28. Use a section model for x and y and never for a height.

**Debris measures a floor.** A hundred and fifty pieces of `prop_rub_litter`,
`prop_rub_cardpile`, shopping trolleys and car wrecks sit on that floor between z -0.77 and
1.36, **median -0.40**, in a band nineteen meters either side of the centerline. That is the
floor height, the channel width and the fact that it is a canyon — all from trash. Where a
site has no geometry of its own, look for what is lying on it.

**A sunken channel gets its own kind.** Its floor is genuinely below sea level, so
`validate_locations` would call every key a mistake, and the engine's walkable query would
answer with the street twenty-eight meters up. `kind = channel` settles both:
`MissionSites.Prepare` only grounds `land`, and `DEEP_KINDS` exempts it from the sea-level
rule. Do not reach for `underground` instead — that label means an interior at z -99.

**`MissionSites.OffsetToSurface` is the shared form of the one-probe trick.** M53 and M56 both
author a flat floor from one datum and correct all of it with a single measurement. Use it
wherever a site's heights share an origin; do not probe each point.

The zones here are **La Puerta** and **Maze Bank Arena**, not the east-side river the name
suggests. `scarab` turned out to be a real model, so the bible named a vehicle the game ships;
check the dump before assuming an authored name is invented. And a half-track has three seats,
so `BoardBrothers` — which asks for `RightRear` — would refuse it: all three are seated
outright.

## The interior wall was never there, and the Maze Bank Tower proves it

This file used to say downtown interiors were unavailable, on the evidence that the mod's MP
apartment tiers all resolve to a single interior location. That is true **of those apartment
tiers** and false of everything else, and believing it cost M55, M64, M65, SM07 and SM08 a
place to happen. **Executive offices, tower garages and the vanilla high-end apartments are
placed at their own buildings, each with its own IPL**, and several can be live at once
because they are different names at different coordinates.

The Maze Bank Tower is walkable end to end, and `Missions/Campaign/Act3/MazeBankTower.cs` is
the one place that records it so four missions do not each re-derive it:

| floor | coordinate | IPL |
|---|---|---|
| plaza deck | (-76.6, -825.6, **36.77**) | `dt1_11_dt1_plaza`, baked |
| carpark | (-84.13, -821.35, 36.71) | `hei_dt1_11_carpark` |
| mid-tower | (-84.22, -823.09, **221.00**) | `imp_dt1_11_cargarage_a` |
| executive | (-73.80, -818.96, **242.39**) | `ex_dt1_11_office_01a` |
| upper | (-73.90, -821.62, 284.00) | `imp_dt1_11_modgarage` |
| roof | (-75.20, -818.95, **323.26**) | `dt1_11_heliport`, baked |

**The plaza is seven meters above the street it overlooks**, so every plaza key is a fixed
surface and one probe at the entrance moves the rest.

**Inside an MLO, exactly one coordinate is authored: the MLO's own placement.** Nobody has
walked those floors. Everything else — six defender posts, Vance, his terminal, the lift — is
an offset from where the crew actually arrives, resolved with `MazeBank.Nearby` and falling
back to the arrival point. Never hard-code a coordinate inside an interior nobody has walked;
a story test refuses a literal `new Vector3(-7…` or `(-8…` in those files.

**An MLO is opened through `MazeBank.Enter`**, which registers the DLC archives through
`DlcMaps` and hands the floor to the access service. Office and garage floors are DLC map
data: a plain `REQUEST_IPL` on those quietly does nothing until the archives are registered.

Two authored details the archives overruled, both recorded in `data/mission_gameplay.tsv`:
**there is no antenna spire** (the tower stops at 325.17, a ring of parapet lights, so M66
jumps from the roof and Ice's "eight hundred feet" is a real number from a real height), and
**the executive floor is 242 m, not the hundredth**. M64's falling elevator cars are not
reproduced either — there is no shaft to drop one down — so that one line goes unplayed
rather than being faked.

One trap this block re-taught: **an objective built from a list that Setup has not filled yet
counts that list at zero.** `KillTargetsObjective`'s survivor count is captured at
construction, so an "if the list is empty" branch written in `BuildStages` always takes the
empty path. Read the list in the lambda and fail loudly in `Setup` instead.


**Five stock high-end apartment interiors sit at five real downtown buildings** —
`v_apartment_high` at (-13.08, -593.62, 93.03), (-32.17, -579.02, 82.91), (-260.88, -953.56,
70.02), (-282.30, -954.78, 85.30) and (-460.61, -691.56, 69.88) — and they are **base map**,
in `hw1_blimp_interior_*` ymaps with no DLC prefix. Nothing to request, nothing to swap when
the player changes brother, so M55 really is three penthouses at once. Ask
`MissionSites.InteriorAt` before placing anyone in one: a missing MLO is a man dropped into
open sky at that height, and one native call is cheaper than finding out the other way.

A solo has no crew, so it derives from `DesertOperation` and has **no `FixedSurfaces`** — that
override belongs to `PreparationOperation`. The location book carries the same fact better:
`kind = interior` and `MissionSites.Prepare` only grounds `land`. Nor does a solo have
`Establish`; it plays its own `SceneSpec`, the way SM05 and SM06 do.

**Never put `RequireAsset` on a man the mission exists to kill.** That contract fails the
mission when the entity dies and cannot tell the intended death from a despawn, so it fails at
the moment of success. SM07 lost a whole run to it.

## Finishing the mission structure, and what "finished" means here

All 79 missions now have scripts. That is a structural milestone and not a claim about
quality: **every coordinate written in the last pass is archive-derived and nobody has played
any of it.** 1,090 location keys, of which 29 are F11-surveyed.

Three habits did most of the work, and they generalize past this campaign:

**When a site "does not exist", check what you searched before believing it.** M53 was filed
unbuildable twice because nothing sits below z=0 under Pillbox Hill; the metro is at z 13. M55
was filed unbuildable because MP apartment tiers share one interior; the game's own high-end
apartments sit at five real buildings. Both notes were accurate readings of the wrong query.

**A site with no geometry of its own can still be measured by what is lying on it.** The storm
channel's floor came from a hundred and fifty pieces of trash; the Vinewood sign turned out to
have twelve maintenance ladders; LSIA's runway is a line of sixty-three road poles.

**A beat that cannot be built honestly is recorded, not faked.** Nine authored lines across the
campaign go unplayed, each with its reason in `data/mission_gameplay.tsv`: a falling elevator
car with no shaft, an EMP that does not exist, a gas main that is not under Davis, a 747 on an
unverifiable approach, a canal eight hundred meters from a ninety-meter roof, spike strips on a
route the player picks himself. The extraction is never edited to follow gameplay.

**M62 is the one to play first.** It is the only mission in the campaign standing on something
that cannot be checked offline — `CREATE_MISSION_TRAIN` — and it carries a fallback to standing
freight if the consist misbehaves.

## September 15: the M45 and M48 playtest

**A stage exit must not throw.** M45 lost a whole five-chapter sitting to
`OnExit` asserting that Gohan was out of the water after a `ReachZoneObjective` within four
meters of the boarding point had already completed — which it does while he is still sitting in
the Kraken below it. A throw there is not a failed mission, it is "Script error" and the end of
the attempt. **Put the condition in the objective**, where failing it simply means the stage is
not finished yet.

**`BlockPermanentEvents = true` is why a guard will not fight.** A ped with permanent events
blocked does not react to seeing an enemy. `PreparationOperation` gets away with it because
`GuardAwareness` orders its hostiles explicitly; a plain `ComposedMission` has nothing doing
that, so its guards stand there. M48 already cleared the flag in `WakeCordon` and M45 never
did — ten men who would not attack. Clear it when the fight starts and issue combat on a
cadence.

**A probe that fails hands back what you gave it.** `MissionSites.OnSurface` returns the
authored point when it finds nothing, so a caller that does not check is placing a man at a
height nobody measured. Four of M45's ten deck posts did that and one ended up inside the hull
where Ron could not shoot him. Check the answer: step the point back toward somewhere the deck
is known to exist, and only then fall back — to a height that was actually measured, never to
the authored guess.

**Nothing spawns in a landing zone.** M45's first guard rank was eight meters from the helipad,
which is under the rotors. `PadClearance` is twenty.

**`AnyOf()` on a stage whose other objective is already true skips the stage.** M48's run south
paired a travel leg with "everybody is in the truck", which is true the moment the cordon drops,
so the chapter ended at the roadblock and the drive never happened. The comment above it
described the opposite intent. If both have to be true, do not write `AnyOf`.

**A surveyed key moves everything derived from it.** Ron surveyed `M48.Technical` up onto the
road because the authored point was on a pier with no way back up, which left it eighteen meters
from the authored roadblock — so the cordon spawned on top of the crew. `CordonLine()` pushes the
line down the road until it is seventy meters from wherever the truck is, derived from the keys
rather than authored, so it survives the next survey too.

**A companion in a turret seat does not pick targets.** M35 learned that "a gun in the bed is
for using" and answered it with `TASK_VEHICLE_SHOOT_AT_PED` on a cooldown. M48 never got that,
so its gunner rode along and never fired.

## Aircraft created in the air

`World.CreateVehicle` at an altitude gives you a helicopter with stopped rotors.
Setting `IsEngineRunning` is not lift: the blades spin up from zero and the aircraft
falls while they do, which from 60 meters over water means it is in the sea before
they reach speed. That is what stopped Ron's heist at M45 on every attempt.

Call `AircraftHold.LaunchAirborne` immediately after creating one above the ground.
It runs the engine, brings the rotors to full speed and gives it approach airspeed.
Any key whose kind is `air` is a spawn that needs it.

## The customs card, and paint you can see

Ron brought a rental interface as a reference: make over model, performance bars with
numbers on them, paint as swatches. The information was the ask; none of the rental
machinery came with it, and a story test refuses the words "duration", "per hour",
"total due" and "rental" in anything the card draws.

**The card is on the right-hand edge.** It used to start at x 490, which in the 1280-wide
drawing space is the middle of the screen — sitting on the car being customized. Anything
new belongs at `CardX` or inside the left menu's 40-460, never between them.

**`Core/PaintPalette` reads the game's paints out of the game.** Every index is written to
the car and the rendered color read straight back, in one pass inside a single frame, with
the car's own finish — custom primary included — captured first and restored in a
`finally`. Never replace that with a typed table of RGB values: a hundred and sixty
hand-copied colors is a hundred and sixty chances to sell a blue the car will not be
painted. `GET_VEHICLE_COLOR` answering what `SET_VEHICLE_COLOURS` just wrote is an
assumption about the engine, so a sweep that comes back with fewer than
`MinimumDistinct` colors is refused, `Ready` stays false, and every paint page falls back
to the list of names it used to be. Nobody has watched this run in game yet.

**A menu page can be a grid.** `Page.Columns` asks for tiles; `Item.Swatch` is the color,
asked for each frame. Up and down move a row (`Page.Stride`), left and right move one
tile, and the drawing still refuses the grid when the colors are missing — so the request
is never a promise. A list row with a `Swatch` draws its chip beside the value, which is
how the channel list finally says which primary the car is wearing.

## September 15: the Paleto vessel, second pass

**A downward probe cannot tell a deck from a cabin floor.** `MissionSites.SurfaceHeight`
answers "something solid is under this point", which inside a hull is just as true as out
on the open deck. The first M45 repair checked only that, and Ron still found two men
standing inside the vessel where he could not shoot them. `MissionSites.OpenAbove` is the
missing half: fire the probe *upward* from just above the surface, and anything it hits is
a deckhead. Every standing point on a built structure needs both answers.

**Those are two different failures and they do not get the same fallback.** A post that is
covered the whole way out to the rail is left empty — eight men he can fight beats ten with
two he cannot reach. A post nothing answered for *at all* is a vessel whose collision has
not streamed, or a chapter opened alone in QA with no structure under it; leaving the deck
empty there is a mission that cannot be completed, so he is placed at the detail's own
measured height and the log calls the post unverified. Conflating them empties the deck in
QA and fails the flow suite with "a required hostile failed to load".

**A hostile detail gets its own surveyable key.** M45's ten men were measured backward from
`M45.Helipad`, so moving the landing zone moved the detail with it — to the bow and off the
front of the boat. `M45.Deck` is the detail's own anchor now. Ron asked for the arrival to
move to the far end of the vessel because a detail standing in the open, watching a
helicopter land and not firing on it, reads wrong however far away it is.

**A scripted map is not a scripted interior.** `ScriptedMap` asks for the ymap, which places
an MLO's shell; until the interior is pinned, the doorway into it is a wall.
`h4_islandx_yacht_03_int` had the map request and nothing else, which is why Ron could board
the vessel at the stern and never get inside, and why M46's first marker sat in a room he
could not reach. `Core/ScriptedInterior` is `ApartmentAccess`'s sequence — un-disable,
un-cap, pin, refresh, wait on `IS_INTERIOR_READY` — with the same ownership rule the map
already keeps: put back whatever state it was found in, on every exit path. Anything that
script-loads an interior goes through it.

## September 15: surveying from the air, and brothers who are not props

**A coordinate is placed by looking at it now, not by walking a man to it.**
`Core/SurveyCamera` lifts the view off the character — who stays frozen where he stood —
and flies. `Insert` by default, or the **Free camera** row in the placement editor. A
capture while flying records the camera's position and facing; a `land` key is dropped
onto the first surface under it, and `air`, `water`, `channel`, `interior` and
`underground` keys keep the height they were flown to. That distinction is the point: the
reason 1,061 of 1,091 keys are still estimates is that half of them are places a man
cannot stand.

Its three rules: **give the view back on every exit path** (a script camera left rendering
is a game nobody can play, and `Release` is safe when nothing was taken); **stream where it
is looking** with `SET_FOCUS_POS_AND_VEL`, because collision loads around the player and a
point captured 200 m away otherwise probes as nothing; and **leave the man alone** — frozen
where he was, and a man somebody else froze is not thawed on the way out.

`SurveyMode.SurfaceProbe` is injected at startup beside `ClearanceProbe` rather than calling
`MissionSites` directly, so the survey stays in the thin regression build.

**`Station` parked a brother and never spoke to him again.** It calls `TakeControl`, which
tells the companion controller to keep its hands off, then clears his tasks and walks him to
his post — and that is the last thing that ever happens to him. Ron reported the symptom:
teammates standing stagnant for most of a mission. `Crew/CompanionPresence` fills that
silence, and **ambience only ever replaces standing still**. Three ways in, all decided by
the caller: a mission registers a parked brother through `StandBy`, the controller animates
a slot it is itself holding (which used to get a bare `GuardCurrentPosition` forever), and a
follower settles only while the man he is with has stopped walking. A mission that hands a
brother real work never registers him, so nothing here can overwrite a scripted task.

A posture is issued **once**, on a change of mood or a stale order — the same rule
`GuardAwareness` enforces for hostiles, for the same reason. Hostiles within 50 m, or a
leader already in a fight, means the guard stance; nobody smokes next to a firefight.
A scenario that will not start where he is standing — leaning needs a wall — is caught by
`IS_PED_USING_ANY_SCENARIO` after two and a half seconds and replaced with a stance that
works anywhere, because a posture that silently refused leaves him exactly as stagnant as
before.

## The pre-flight placement report

`Core/PlacementPreflight` says, when a mission begins, which of the points it is about to
stand on have never been surveyed — by name, in the log and in the mission doctor. The
`status` column has existed as long as the location book has and only the offline linter
ever read it; 1,061 of 1,091 keys are estimates, every Act 3 key without exception, and
every placement fault of the last week was one of them going unmentioned.

It reports the keys the mission **used**, recorded through `LocationBook.Get` — the one
choke point every mission's `At()` funnels into — rather than the ones whose name starts
with its id. A prefix scan misses every shared point, which is exactly the kind of place
two missions disagree about. `BeginUse()` is called before `mission.Begin`; the report runs
after it returns.

**It is a warning, never a refusal.** An estimate is usually close enough to play, and a
mission that would not start until every point was surveyed would mean no campaign. The
prefix form is used for one thing only — `ProgressLabel`, the placement editor's mission
list, which is now the survey to-do list ("12 locations, none surveyed").

## Staging a mission without running it

`Core/ScenePreview` runs a mission's `Setup` and stops. `ComposedMission.OnStage() =>
Setup()` is the seam; `Mission.StageForPreview` never sets `Status` to Running, so `Tick`
refuses it and no objective can advance.

**The preview owns nothing.** The obvious design is a parallel entity pool the preview
deletes on the way out, and that is a second owner for the same peds — the exact class of
bug this project keeps finding. The mission tracks its own staging as it always does and
closing the preview calls the mission's own `Cleanup`, the path every attempt and every
teardown test already covers. The one addition is `Disembark`: teardown deliberately hands
back a vehicle a brother is sitting in rather than deleting it under him, which is right
for a mission that just passed and a leak here, so the crew come out of staged vehicles
first and are put back where they were standing.

It writes `Bloodlines.Staging.txt` — every staged entity with its position and the nearest
authored key. A line reading `derived — no key within 12 m` is a runtime-computed position
(M18's rear work point, M45's probed deck) that no data file holds.

`Core/PlacementGhost` hangs a translucent stand-in in front of the survey camera. **Not a
3D gizmo**: a script menu has no mouse cursor over the world, so axis handles would be
decoration over the same key-by-key adjustment. It is a *shape*, never a claim about which
model a mission will spawn, because the location book does not know.

`Core/RouteRibbons` draws multi-point paths from ordered keys — `M09.Convoy.01`, `.02`.
The convention is the whole format: no second file to keep in step with `locations.tsv`.
Nothing in the book is a route yet.

## The instrument objectives

Ninety-two of this campaign's beats are `MissionInteraction` — walk here, hold the button,
watch a bar fill — across fifty-nine of seventy-nine missions. The campaign audit's verdict
was right: Bloodlines does not need more shooting, it needs fewer beats where the player
watches a progress bar. Read its fifteen suggested minigames together and they are three
shapes, not fifteen designs, and two of the three already existed.

`Missions/Objectives/InstrumentObjectives.cs` holds the two new ones.

**`GaugeObjective`** is M11's dyno with the Granger taken out of it: a value, a band, a
hold. `Drift` is what separates a setpoint you can let go of from a job you keep your hand
on. **`AlignObjective`** is the other half — an optimum the player is never shown, a
control that moves toward it, and a signal that says warmer or colder. Its `Target` is
asked every frame, so the answer may move.

**Input is injected**, defaulting to the throttle-and-brake pair M11 has always used. That
is what makes them testable: a test drives the needle instead of pretending to hold a
trigger. **Neither can fail a mission** — overshooting costs time, never a sitting — and a
test refuses `Fail(` anywhere in that file.

The third shape needs no new code: `TechnicalChoiceObjective` has existed since M28 and is
used by exactly one mission. Every "give the player a choice that changes what happens
next" in the audit is that class plus authoring.

**A stage index in a condition is a bug waiting for an edit.** M07 read `Stage == 2` to
decide whether its parachute could be walked over. Inserting one stage earlier in the
mission silently turned the collection off, and the mission still passed every check that
did not walk that far. Ask the real question — the chute exists only while that stage is
open, because that stage's `OnEnter` drops it.

## Scenes: eye contact, faces, and shots that slide

`Core/ScenePerformance` is what the scenes were missing. There was not one
`TASK_LOOK_AT_ENTITY` or facial call anywhere in `CutsceneDirector`, `SceneBlocking` or
`SceneSteps`: three men stood facing whichever way they arrived, staring through each
other with the neutral idle face, while the lines played. Per line, everyone within
`LookRange` turns to the speaker, the speaker looks back at one of them — a different one
each time — and faces carry a talking or listening mood, or one tone laid over the whole
scene.

**A look is issued once per change of speaker**, never per line and never per frame:
re-ordering a head-track restarts it before the neck has moved, which is the same fault
that made the guards in M31 stand still. And every face and head is released on every exit
path, or a brother wears a scene's angry face for the rest of the session.

**The camera slides.** Every shot used to be an assignment to `Camera.Position`, which is a
cut on every line — three men talking looked like a security-camera montage. `Frame()`
eases position and look-at on a smoothstep over `ShotEaseMs`; a tracking shot passes
`ease: false`, because easing something that writes every frame makes the camera lag what
it is following.

**And the view is handed back rather than cut back.** `RENDER_SCRIPT_CAMS(false, true, …)`
interpolates into the player's own camera — but only when `_previousCamera` is null, since
handing an interpolation to a camera another system is about to restore leaves the player
looking at the sky. The scene camera then outlives its own hand-back: deleting a camera
mid-interpolation is a hard cut with extra steps, so `RetireCameras` deletes it afterward
and the next scene forces the retirement if that never ran.

## Tuning stages, and the two that do not exist

`Core/VehicleStages` sells one level past the last part the game has. GTA's slots are
fixed — engine 0 to 3, transmission and brakes 0 to 2, suspension 0 to 3 — and there is no
top-speed slot at all, which is why no mod shop has ever offered one.

**There are two stages and there will not be four.** Power and top speed have per-entity
natives: `SET_VEHICLE_CHEAT_POWER_INCREASE` and `SET_VEHICLE_MAX_SPEED` take a handle and
touch one car. Braking and grip have none — the SDK offers only `SET_VEHICLE_REDUCE_GRIP`
and `SET_REDUCED_SUSPENSION_FORCE`, which take capability away. Raising either means
writing the model's shared handling data, which reaches every car of that model in the
world including traffic, and that is already a recorded contract. Do not add a brake or
grip stage; add the reason to the refusal text instead.

A stage belongs to **one owned car**, not a model. `GarageService.RecordFor` matches a live
vehicle to its record by handle, the host injects that as `VehicleStages.Fitted`, and
`WorldTuning` multiplies that car's own applied ceiling and power. It never goes near
`InitialDriveMaxFlatVelocity`, and a test measures the distance between the two in the
source to keep it that way.

The stages live in explicit `OwnedVehicle` fields rather than in `Finish`, because
`VehicleFinish.Capture` clears that dictionary every time the build is read off the car.
A save written before stages existed reads as none fitted.

**A raised ceiling is not a measured top speed.** The shop card says the stage is fitted in
words rather than moving the performance bars, because those bars come from the game's own
natives and the game does not know about it.

**`Json.Int` only understands the doubles the reader produces.** Handing `FromJson` the
dictionary `ToJson` just built reads every number as zero — a test that round-trips in
memory proves nothing. Go through `Json.Read(Json.Write(...))`, which is what a save does.
## What a vehicle costs

`tools/price_vehicles.py` generates the catalog's price table; `--apply` writes it into
`Garages.cs`. **A price already in the table is an anchor and is never moved** — the tool
only fills gaps, so a number set by hand for a story reason survives every regeneration.

**A runtime performance formula was tried and measured, and it does not work.** Fitted
against the sixty-five hand-set prices it is 40 to 45 per cent out, and the misses are
systematic: the Virgo is $18,000 and the curve says $39,000; the Oppressor Mk II is
$300,000 and the curve says $44,000, because its worth is that it flies and shoots. Those
hand prices are an affordability ladder, not a performance curve, and a formula fights the
design every time a handling file changes.

The ladder is anchored to the campaign's own payouts, which the progression guide exports:
**$55,000 across the first three missions, $345,000 by M10, $3,722,000 in total.** So
something has to exist under $20,000, and nothing costs more than $500,000. Street sales
are capped at `min($30,000, price)`, which makes anything above that a one-way purchase.

Uniqueness is decided **per catalog page**, not per class: the phone sorts a category by
price, and two vehicles sharing a number is two vehicles in an arbitrary order. Ties
between two hand-set prices are left alone, because moving an anchor is the one thing the
tool will not do.

## The placement editor, after Ron used it

Two faults, both found by editing M60.

**A teleport has to take the camera.** `SurveyMode.TeleportToCurrent` moves the man; the
survey camera is a separate entity and stayed where it was, so in fly mode the key he asked
for never appeared and nothing looked like it had happened. The second half is worse and
was the actual timeout: the camera holds the streaming focus with
`SET_FOCUS_POS_AND_VEL`, and the teleport waits on `HAS_COLLISION_LOADED_AROUND_ENTITY`
for the man — so with the focus still back at the old view, the destination never streamed,
the four-second gate expired and it rolled him back. `SurveyCamera.MoveTo` parks the camera
one standoff back along its own line of sight, never below the point, and takes the focus
with it; `ReturnTo` puts it back when the teleport rolls back. **Anything that moves the man
while the camera is up has to move the camera, or it has not moved anything he can see.**

**A group is declared in a table, and the table is the contract.** `MissionPlacement.HasGroup`
was `key == "M05.LightCrew" || key == "M03.DepotGate"` — two keys out of one thousand and
ninety-one, so the editor's enemy-count row answered "not a group" on everything Ron tried
and he reasonably concluded the grouping was broken. `MissionPlacement.Groups` is one
dictionary of every key whose detail can be sized, the way `MissionOperations` is one table
of operations, and a story test holds it against the missions that read it so the next
detail cannot be written and forgotten.

Three rules that came out of wiring it:

 * **A declared key changes nothing until it is edited.** An unedited key has no count, so
   `HasFormation` is false and the mission falls back to the constant it was authored with.
   `MissionPlacement.PointFor` is the same idea for position: the edited formation when the
   key has one, and the exact offset the mission wrote when it does not. That is what makes
   adding a row to the table safe on an unsurveyed campaign.
 * **A radius of zero means the spread is the mission's own.** M45's deck detail stands in
   ranks across the beam and probes every post for a deckhead, because a downward probe
   cannot tell a deck from the cabin floor under it. Its count is editable and its shape is
   not, and the editor draws no ring and no dots for it — a circle it would not stand in is
   the editor promising a placement it does not make.
 * **A detail laid out as `index * spacing` is a queue, not a position.** Four missions each
   reached for that on their own. `MissionPlacement.GroupPoint` is a sunflower spiral that
   fills a circle evenly at any count, and it is now the one place that layout lives.

M60's and M70's waves, M63's nests and M45's deck detail read their size from their keys.
The posts in M49, M55, M64, M65, SM07 and SM08 deliberately do not: those come from a
runtime road node or from `MazeBank.Nearby` inside an interior nobody has walked, so there
is no surveyable key for the editor to put a number on.

## The survey says what it sees, and checks a mission at a time

The backlog was measured before any of this was built, and the measurement is the reason
for its shape: **1,062 of 1,091 keys are estimates, but 282 of those are SM03's race route
and the rest are a median of nine per mission.** Forty-two of eighty-five missions have
eight or fewer. So the cost was never the total — it was judging nine spots one at a time
with nothing on screen saying whether any of them was already right.

`Core/SurveyReading` is what the probes can say about a point, judged against the `kind` of
the key it belongs to. **Every branch in it is a bug this campaign shipped and found weeks
later in game**: a land key with nothing under it (M40's hull kits under the pier), one with
a deckhead on it (M45's two men inside the hull), an interior key where no interior loads
(M55, filed unbuildable twice), an air key a meter off a deck, an underground key with open
sky over it (M53's search floor in the wrong place).

Three rules it keeps:

 * **Not measured is not the same as measured and found nothing.** A null probe has to read
   as "did not look", and the first version of this conflated them — which called every
   point in the harness broken and failed a suite that had nothing wrong with it. Each
   reading carries whether the probe ran at all.
 * **A complaint is never a refusal.** An estimate is usually close enough to play, the same
   reason `PlacementPreflight` only ever warns. A capture that contradicts its own key waits
   for a deliberate second press — the same gate a displaced capture already used — because
   an author who knows better than a ray has to be able to say so.
 * **Judging is separate from probing.** `SurveyReading` holds numbers somebody else
   measured and decides what they mean, so the decision is tested without a game running.
   `SurveyMode.Read` gathers them through injected delegates, the way `SurfaceProbe` and
   `ClearanceProbe` already were.

**The sweep drives the ordinary teleport rather than a traversal of its own.** `BeginSweep`
opens the normal survey queue and supplies the presses; each key is read once its teleport
has landed and the collision has had `SweepDwellMs` to settle. Collision loads around the
focus and the man, so a point read from where he happens to be standing reads as nothing —
the teleport already moves both, waits properly and gives up cleanly when the terrain never
arrives. A second path through the same problem would be a second set of the same mistakes.
It writes `Bloodlines.Survey-Check.txt` beside the survey ini: the spots that need a look
first, then everything checked.

**Accepting in place is a capture that does not move the point.** The camera survey was
already a coordinate placed by looking at it rather than by standing on it, so a spot he has
looked at and found right is surveyed and the status stops calling it a guess. `AcceptClean`
does that for everything the last sweep passed. It refuses a spot that did not check out,
because recording one of those as verified is recording a guess as a measurement.

**A spot that checks out is placeable, not correct.** Whether it is the right place for the
beat is a judgment no probe makes, and the report says so in its own header.

## The Act III audit, and the walker that never walked it

**Every main mission from M49 derives from `PreparationOperation`, and the flow walker
aborted every `PreparationOperation` before its first stage** - on the grounds that M31 to
M40 had dedicated walkthroughs, which M49 to M70 never got. So no Act III mission had ever
been driven through its beats, even in the harness, and the walker's own Act III hooks were
dead code. The walker drives M49 onward now, producing each condition beat rather than
faking it, and **all 22 reach Pass through real objective updates**. See
`docs/ACT3-AUDIT-2026-09-17.md` for the three defects that were hiding behind the skip.

Three rules out of it:

 * **`AnyOf()` on a stage of targets is a stage that ends on the first one.** M57's three
   gunships were three `DestroyVehicleObjective`s under `AnyOf`, and `OnPassed` then threw
   about the two still flying - a script error, not a failure. If every target has to fall,
   do not write `AnyOf`, and never assert in `OnPassed` what the stage already guarantees.
 * **`RequireAsset` fails on death, so never put it on a man the mission ends by killing.**
   The file already said this about SM07; M65 had it on Vance. The check for "dead before the
   biometrics" belongs in `OnUpdate` with its own reason, which is where it was all along.
 * **`Enemy(key)` snaps to walkable ground, and over water that is the shore.** M61's four
   divers stood on the quay. A man who belongs in the water is resolved through
   `MarineSites` and created with `EnemyAt`, the way M53 places men the navmesh would move.

`Core/FarPlacement` keeps collision loaded around a brother created or stationed more than
150 m from the player (Ice on the sign in M59, the penthouses in M55). It never freezes him.

**A request is not a load.** `VehicleSpecs.Of` requested a model and released it in the
same call, every frame, so the phone's car rows read "reading ratings" forever: the release
canceled the request before it landed. Keep a request open until what it was for has been
read, and expect the harness stand-in to stream synchronously unless told otherwise -
`Model.StreamsNextFrame` is that switch, and a check that passes with it off has proved
nothing about streaming.

**The host constructor is checked as text.** It is a `GTA.Script`, nothing instantiates it in
a harness, and a field dereferenced before the line that builds it took the whole campaign
out of the game (PR #93). `HostConstructorChecks` refuses that ordering, lambda bodies
excepted.

## The interface pass: HUD state, a results card, the car page, a title card

See `docs/INTERFACE-2026-09-17.md`. The rules worth keeping:

**Two things cannot share the subtitle slot.** `TimerObjective` pushed its clock as a
subtitle and `ComposedMission` pushes the objective text into the same slot every tick, so
M55's five-minute clock never showed. Anything with a number to show belongs in
`Core/MissionHud`, which composes a `Frame` from `MissionManager.CurrentObjectives` each
tick and draws it; never push a second subtitle from an objective.

**The HUD is read, never stored.** `MissionHud.Compose` is pure over the running mission -
the owning brother, the distance, the gauge, the timer - and a test reads the `Frame` with
no screen. Add a kind of progress by teaching `Describe`, not by writing state anywhere.

**One color per brother, everywhere.** `Core/CrewColors` holds the phone's three accents
and the HUD, the title card and the results card draw from it. A story test holds the
phone's literals to those values.

**A kill is credited, never scored.** `Core/MissionTally` attributes a hostile's death to
whoever was in play when it was noticed, reading only from `Mission.Staged`; it holds no
entities. `Passed` carries the `Result`, the panel draws it, and a failed attempt is dropped.

**A phone page can draw a picture and bars.** `PhoneEntry.Art` names a `phone-<name>.png`
in the ui folder and `PhoneEntry.LiveBars` yields label and fill; the reading pane draws
them above the text through the sprite and box callbacks the home screen already uses.
Silhouettes are one per catalog class (`VehicleSpecs.ArtFor`), never one per model.

**A card is cued when gameplay begins, not over the briefing.** `MissionManager.Started`
fires from `BeginGameplay`, after the scene, and the presentation caps a frame at a quarter
second - a test that jumps five seconds in one frame has skipped nothing.

## Crew orders, and the two seat rules behind Ron's truck

See `docs/CREW-ORDERS-2026-09-17.md`. Ron got into a truck's driver's seat and both brothers
took the cab, never the gun; he switched into the gun seat and the driver got out. Neither
was a missing feature:

**The seat picker took the lowest free index.** The driver's seat is index minus one and
was never in the loop, so no brother could take the wheel of the player's vehicle; the
turret is the highest index and was always the last seat filled. `FreeSeat` now takes the
seat an order names or nothing, then the wheel when the player has settled into another
seat (`WantsDriverMs`, because shorter is the engine shuffling him across), then a turret
before a plain seat, then the rest. `IS_TURRET_SEAT` says which seats are guns; never a
seat number.

**A player on foot beside the vehicle he was just in is changing seats, not leaving.** The
driver used to get out the moment the player's feet touched the ground. For `SeatShuffleMs`
within `SeatShuffleMeters` of the vehicle he was last seen in, the driver holds still
(`CompanionDriver.HoldStill`) and passengers keep their seats. It has to be the vehicle he
was last in: a player walking up to a brother's car for a reunion was never in it, and the
first version of this grace broke that test.

**An order is a standing thing in the controller, and every order is an invitation.**
`CompanionController.Order` stores it; `DecideOrdered` answers it first, before the
hangout rules, and clears it when it is satisfied. It is refused by the same gates a phone
hangout is - a mission, a hold, a required shared ride, a script that owns him - and a
mission starting clears every order. `Core/CrewOrderStrip` is only the hold-pick-release
gesture and the echo; it decides nothing about what an order means, and asks
`CrewOrders.Available` every frame because the answer changes as he moves.

**The pad opens it on a hold of d-pad left, past a tap.** That is the radio wheel's button
in a vehicle; the hold threshold is what keeps a tap for the game, and the control is
disabled only once the strip is up. The keyboard path runs through key-down and key-up,
and while the key is held the pad path stays out of it, or releasing nothing would send.

**A blimp interior has no door.** M55's penthouses are the apartments the game shows through
the windows from the air; Ron finished the job and could not leave. Each brother's arrival
point is his service elevator, and the street under a tower is found at runtime
(`StreetBelow`), because nobody knows which side of it the sidewalk is on. Every suite has
security now, not only Ice's.

## SM03's sprint: a measured loop, and rivals who were obeying a slow order

See `docs/SM03-SPRINT-2026-09-17.md`. Ron reported the race looped instead of running at the
mountain, and that the rivals were no challenge. Both were true and both had causes that were
not the obvious ones.

**A route complaint is measurable, so measure it before redesigning.** 278 gates covered
18.97 km to travel 7.27 km of straight line; 69 of them moved away from the finish; the run
entered the Mount Chiliad zone four separate times with a Braddock Tunnel round trip in
between, and the climb was the last 8% of the list. That is the loop, in numbers, and the
same numbers say when it is gone: 34 gates, 12.00 km, not one gate further from the finish
than the gate before it.

**The corridor was cut out of the data that was already there.** Every one of those 278 keys
was a real road position off the offline graph, so the direct route is the shortest chain
through that same set: join any pair within a shortcut radius, refuse a join whose grade
would be a cliff, take the shortest path. Where the old route passed near itself the
shortcut exists and the loop is cut. Nothing was authored that was not already in the file,
and 244 estimate keys left the survey backlog with it (1,091 keys to 847).

**A race route is seeds plus runtime lanes, and a trail is not a lane.** `Core/RaceRoute` is
M49's answer applied to a race: roads are baked terrain, so each drivable gate is snapped to
the nearest vehicle node when the game is running. A **trail** gate is passed through
untouched, because the nearest node to a point halfway up Chiliad is the road at the bottom
of the mountain, and snapping there would not correct the climb, it would delete it. A node
further from the finish than the gate before it is refused outright: a correction may not
reintroduce the doubling back the route was rebuilt to remove. No node at all is a warning
and the seed is used, the way `PlacementPreflight` only ever warns.

**An AI driver that is not a challenge is usually obeying a slow order.** The rivals' ability
was already `1.0`, the engine's maximum, so there was nothing to raise. They were commanded a
flat 39 m/s on a route where the player's car runs to twice its stock redline, because
`WorldTuning` lifts every ceiling in the world including theirs. The speed comes from the
car's own `GET_VEHICLE_ESTIMATED_MAX_SPEED` now, they run the performance parts, and
aggressiveness is at 1.0.

**A fair grid is matched to the player's car, never copied from it.** The first pass gave both
rivals the player's own model and Ron rejected it outright: "I definitely don't want them to
have my car." Bringing your own car to a street race is the point of the mission, and two
copies of it on the grid hands that to the opposition. `Core/RivalGrid` is KJ's own roster of
twelve base-game street cars spanning 41 to 52 m/s, and he fields the two whose **model** top
speed is closest to the player's, with the player's model excluded so the matching cannot
land on it by coincidence. Compare model to model, because both sides get the parts fitted;
comparing his built car to their stock ones fields a grid that is always too slow. Whatever
gap is left is `RacePacing`'s to close, bounded.

**`Core/RacePacing` is bounded rubber-banding, and it only writes the commanded speed.** A
trailing rival is helped up to 1.35x, a runaway leader eases to 0.85x, both over a 400 m
band, both logged. It never touches `SET_VEHICLE_CHEAT_POWER_INCREASE` or the entity speed
cap: `WorldTuning` owns those per car, a second writer on them is the double-ownership bug
this project keeps finding, and it is not needed because a car whose ceiling is already
doubled can reach any speed this asks for. A story test measures the distance between the two
in the source.

**A source assertion must not depend on line endings.** A check written the day before
matched a literal `\n` inside a source file, so it passed in one worktree and failed in the
next. Strip the endings, or match with a pattern.

## Crew driving: the speed is the car's, and a getaway is the engine's flee mission

See `docs/CREW-DRIVING-2026-09-18.md`. Ron: "make them drive a little bit faster and a
little bit better and with a little bit more intent... if I tell them take the wheel they
should just drive when police is on them."

**A flat commanded speed is a slow order once the world's ceilings are doubled.** The
brothers held 40 to 60 m/s, numbers from when every car ran to its stock redline;
`WorldTuning` lifts every ceiling to twice that, so the same order now reads as a man not
trying. `CrewDriving.Speed(slot, urgent, vehicle)` is a share of the car's own
`GET_VEHICLE_ESTIMATED_MAX_SPEED`, Guess highest, with the old flat numbers kept as a floor.
The SM03 rivals got the same rule the day before, for the same reason.

**The escape style is a different style, not a faster one.** `CrewDriving.EscapeFlags`
allows the wrong side of the road and the map's shortcut links; the everyday
`TrafficFlags` does not. A driver with the police on him who will not cross a median is not
escaping. `SET_DRIVER_RACING_MODIFIER` is the engine's own dial for corner commitment and
is on only when the drive is urgent.

**Intent is the engine's `Flee` vehicle mission aimed at the nearest officer**, not a point
700 m ahead on the road. It applies with the police on the car, the player aboard in any
seat but the wheel, and nowhere in particular to be: a mission destination or a map
waypoint is still driven to, at escape pace, because "drive to my waypoint" under pursuit
means exactly that. A flee task is held for `FleeRefreshMs` against the same officer
rather than re-issued every review, which is the `KeepDriving` rule again; and when the
heat drops the task is replaced, because a flee task left running is a driver fleeing an
empty road.

**A relationship group is read through the native in code both harnesses compile.** The
story stand-in's `RelationshipGroup` is an `int` and the thin one's is a struct with a
`Hash`; `GET_PED_RELATIONSHIP_GROUP_HASH` is the one spelling that compiles in both and is
what the engine says anyway.


## September 19: integration repairs

See `docs/code-review-2026-09-19.md`. Crew orders, the character wheel, M09's shot window,
and Slipstream use `SlowMotion` claims. Closing one releases only its claim; ability shutdown
must not reset the scene or menu clock. Emergency teardown resets all claims. Suppress the
vanilla character switch even when the order strip blocks the custom wheel.

Phone hangout and travel choices replace the selected brother's quick order only after the
normal mission/ownership gates accept the change. A valid flee target survives the full
five-second cooldown even if another officer becomes nearer; death, loss of range, a changed
destination, or lost pursuit can still replace the task immediately.

MissionManager reads HUD objectives from a continuous operation's current phase. Its staged
entity view reads the existing OperationWorld collection, so results include hostiles from
all phases without taking ownership or counting a handle twice. Run both harnesses; the
review repair tests exercise time overlap, both heist dispatchers, and phase transitions.

## September 22: scene cameras, M05's boarding, and placed additions

See `docs/PLAYTEST-2026-09-22.md`. **A scene camera sliding home is this director's own,
never a view to restore.** The eased hand-back keeps it rendering for about a second, and
69 of 79 missions start a staged scene straight after the briefing. The second scene read
the sliding camera as another system's and restored it on exit, which left the view on a
dead shot. `CutsceneDirector.Play` retires any sliding camera before it looks at what is
rendering. The host retires cameras first in the tick, ahead of every early return. A camera
still rendering when its slide should be over is handed back before it is deleted.

**Nothing on a boat is ever stopped.** `MissionInteraction` holds from a boat measure reach
flat, accept 3.5 m/s as stopped, and pause (not restart) for up to 2.5 s when a wave moves
them. A hold owned by a passenger needs somebody steering. M05 anchors Mateo's boat, and
Guess brings the dinghy alongside and anchors it while the player is not at the wheel. Tie
that to the stage's own enter and exit, never to a stage number.

Thermal Pulse marks hostiles out to 300 m (everyone else stays at 60 m), because its
explanation in M05 promises the generator crew 270 m below Ice's perch.

**Placed additions are the mission's, not a second pool.** `Core/MissionAdditions` spawns an
author's extra men, crewed vehicles and props through `Mission.Track`, so the mission's own
cleanup owns them. A man is told to fight once, when the crew comes within range. Additions
never count toward an objective. `ContinuousOperation` opts out (`TakesPlacedAdditions`),
because it shares its entry chapter's id. The editor's pick lists are strings, so
`lint_missions.py` checks them against the dumps. The survey page keeps Save second and Y
saves from any row.

## Bonus missions, and BM01's Osprey

See `docs/BONUS-BM01-CLIPPED-WINGS.md`. **A bonus mission is authored, never extracted.**
Its row lives in `data/bonus_missions.tsv`, in the same columns as `missions.tsv`, and
`CampaignData` loads it after the bible. The catalog offers bonus missions after everything
else, under `CampaignAct.Bonus`. Ids are `BMnn`, so every tool that pattern-matches mission ids
accepts `(?:S|B)?M\d\d`. BM01 opens after M70 and every solo job.

**An aircraft the player has to shoot down is flown by the script, not the AI.**
`Core/ScriptedFlight` turns gravity off and moves it along road points snapped at runtime,
paced to stay ahead of the chase. `ShootDownObjective` wears out a hull meter by counting
hits, because the Avenger's own armor is an unknown Online number. Never put `RequireAsset`
on the aircraft: it dying is how the mission is won. A stage that opens in the same tick a
scene starts must not move the scene's subject; BM01's lift waits for the takeoff scene to end.

The Osprey is bought and called in through `Core/OspreyHangar`, on the phone's garage page.
It is the only aircraft the crew can buy, and it keeps no build between calls.
