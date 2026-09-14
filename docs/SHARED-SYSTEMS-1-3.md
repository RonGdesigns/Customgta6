# Shared systems pass 1–3

September 14, 2026. Branch: `codex/shared-systems-1-3`.

This pass builds reusable infrastructure while the mission implementation agent works on campaign content. It deliberately does **not** edit campaign mission classes, mission coordinates, authored dialogue, campaign rewards, saves, or the existing Port Heist behavior.

The three systems are:

1. placement contracts plus structured mission diagnostics;
2. resumable companion role work;
3. a generic one-sitting operation parent for future continuous mission blocks.

These are foundations. A green automated suite means the contracts compile and their stand-in behavior works; it does not live-verify GTA collision, pathfinding, AI tasks, or a future M44–M48 implementation.

## 1. Placement contracts and Mission Doctor

`MissionDoctor` is a bounded, session-only diagnostic ledger. It records structured `Info`, `Warning`, and `Error` events with a category, subject and explanation. It never writes campaign progress, coordinates or a save. The ordinary Bloodlines log still receives each diagnostic event once.

`PlacementContract` gives an authored location an explicit purpose instead of treating every XYZ as interchangeable:

- `Ped(key)` — standing-space contract;
- `Interaction(key)` — standing-space contract intended for an interaction point;
- `Vehicle(key, model, departureMeters)` — full model footprint and optional initial departure corridor;
- `Aircraft(key, model, departureMeters)` — same footprint contract, with the intent recorded as aircraft staging.

The resolver delegates to the existing `BoundedPlacement` checks. Those checks remain authoritative for authored-floor preservation, model footprint, nearby-vehicle obstruction, and initial aircraft/vehicle departure clearance. A contract never searches for a different floor, deletes a blocker, or writes a guessed replacement coordinate.

`Inspect` returns a `PlacementResult` and records it in the doctor. `Require` throws if the contract fails. Callers that intentionally trust a surveyed point despite an unavailable runtime query can still use the existing `TryPed`/`TryVehicle` path and record a warning instead of turning every uncertain check into a hard mission-start failure.

### Next wiring step

The dev menu should eventually expose `MissionDoctor.Summary()` and recent entries so a playtester can see a named placement/asset/prerequisite reason in game. Existing mission-start code still has generic failure presentation in places; this pass does not rewrite those mission classes while another agent is editing them.

## 2. Companion Role AI 2.0

`RoleTrack.Work(point, cover)` remains backward-compatible and still means hold the work point when no action is supplied.

The new overload accepts a `RoleAction`. A role action owns its own progress and has explicit start/update/suspend/resume/cancel lifecycle hooks. `DelegateRoleAction` lets a mission plug in its physical task and progress source without inventing another global AI system.

The important contract is continuity:

- while an inactive brother is working, his action ticks;
- if he is threatened, the action suspends before the role enters `Threatened`;
- after six clear seconds, the same action resumes with its existing progress;
- if the player switches into that brother, AI work suspends instead of continuing underneath player control;
- when the player switches away, the same action resumes;
- mission/role teardown cancels unfinished work exactly once.

This is intended for later multi-character jobs such as M55: terminal progress can belong to the terminal action, rather than a generic `Working = stand still` task. Mission code still decides what physical action constitutes progress and what happens if the device/entity is lost.

## 3. Generic continuous-operation foundation

The existing Port Heist remains untouched in this pass. It is the proven reference: M19–M22 already run under one parent without ordinary `MissionManager.Start/Finish` calls at live phase boundaries.

`ContinuousOperation` generalizes that structure for future blocks such as the owner-approved uninterrupted M44–M48 Paleto operation:

- one parent mission/attempt;
- ordered internal phase factories;
- one shared `ContinuousOperationState`;
- no checkpoint capture/restore inside the operation;
- no ordinary mission completion, payout, loan teardown or free-roam gap at an internal boundary;
- final dialogue drains only at the final result; radio may continue across inner joins;
- operation-level active/end validation hooks;
- cleanup at the parent boundary.

`ContinuousOperationState` is session-only. It can own/bind named live entities, preserve selected entities on success, and carry operation-local typed values. A named live entity cannot silently be replaced by another object with the same key.

`ContinuousPhaseMission` is the phase base intended for new generic operations. Its tracked entities are registered with the shared operation state and preserved from child cleanup; the parent remains the final owner. On later phases, its `ApplyBibleSetting` does not reset time/weather merely because an internal mission id changed.

### Important scope boundary

This pass does **not** convert `PortHeistOperation` or its specialized `PortHeistWorld` to the new base. That would create unnecessary regression risk while the current heist works. The generic framework is for new operation code first. Once a second operation proves the abstraction, migrating the old heist can be considered separately.

`MissionContext.ActiveOperation` holds the generic world. The existing `MissionContext.PortHeist` remains for current M19–M22 code.

## Acceptance before merge

Automated gates must include:

- production build with warnings as errors;
- mission lint and location audit;
- story/runtime suite, including Mission Doctor bounds and resumable RoleAction behavior;
- existing regression and parser suites;
- generated story/campaign freshness checks.

No mission implementation should be changed merely to make these tests pass. If the branch conflicts with newer mission work, rebase/merge the newer campaign head and preserve both sets rather than overwriting the mission agent.

## Live acceptance later

When first consumed by a mission, test these separately in GTA V Enhanced:

1. a valid and invalid placement contract, confirming the exact authored key is reported and no alternate floor is chosen;
2. an inactive brother performing visible work, being interrupted by combat, resuming, switching into him, and handing him back to AI without losing progress;
3. M44–M48 running from one start through all phases with the same crew/assets, no intermediate pass screen, no loadout/health reset, no free-roam gap, and full retry from M44 after failure.
