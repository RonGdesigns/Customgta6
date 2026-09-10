# Bloodlines — Playthrough Repair Plan: Prologue through M04

**Date:** September 9, 2026  
**Evidence:** Ron's ten pages of notes (`Notes_260909_230929.pdf`) from a playthrough of the `78b48c2` install, his follow-up direction (steering and weight before speed; Buzzard felt no faster; Guess should feel like Franklin), and the external repair plan. Every note is accounted for below.  
**Source checked:** `WorldTuning`, `TravelHandling`, `SlipstreamReflex`, `CutsceneDirector`, `MissionManager`, `BloodlinesMain`, `CrewRoster`, `TacticalResponse`, `PersonalWanted`, M01–M04, `DevTools`, `SurveyMode`, and the pinned ScriptHookVDotNet 3.6 reference assembly's public surface.  
**Status:** A plan. No code, data, save or game file changed by this document. Nothing here is live-verified; the notes are the only runtime evidence.

---

## 0. Owner direction, restated as rules for this work

1. **Handling before speed.** Steering, grip, weight, suspension and braking get fixed first. The 2x speed target stays until those are calibrated and Ron has driven them. A speed cut is not the fix.
2. **Baseline first, ability second.** Ordinary driving must be good without the ability. Guess's ability is a further layer of control, Franklin-inspired: more time to react, excellent usable steering, a car that stays planted.
3. **Faster still means faster.** The Buzzard must be measurably quicker in cruise, with controllable turns, hover and landing.
4. **Missions start at the marker.** Unless the story warrants a cut, the player drives from the start point to the objective.
5. **Nothing succeeds by accident.** A canceled scene, a failed exit, an off-screen death of a required actor, or a safehouse arrival under pursuit does not count as done.

---

## 1. What the notes say, mapped to causes

| # | Note (page) | Confirmed cause in source, or the leading candidate | Package |
| --- | --- | --- | --- |
| 1 | Prologue car is on the wrong side of the barrier (p1) | `Prologue.LSIACar` is an estimate. Ron's candidate: X −1041.02, Y −2719.06, Z 13.20, H 238.7. | C |
| 2 | No waypoint during the drive home (p1) | Confirmed: the prologue submitted its route before the marker frame opened, so it was discarded every tick. Fixed on `claude/fix-post-merge-audit` (R01). Needs the live retest. | A |
| 3 | Cars hard to steer at speed; fly on impact (p1–2) | `WorldTuning` doubles gearing and adds torque; nothing adjusts grip, suspension, center of mass or braking. Lateral force needed for the same corner scales with speed squared. | B |
| 4 | Ron in his apartment for the call (p2) | Design addition; the apartment entry system already exists. | D |
| 5 | Prologue → M01: Ron in the car, can't move, only a debug death frees him (p2) | Three candidates, none proven: the old hand-off issued an async leave-vehicle and then teleported a still-seated ped (replaced on the fix branch); M01's start puts every hero including the active one under companion "scripted" control; the briefing holds the player's vehicle frozen and restores flags afterward. Needs the diagnostic record in §3 to settle. | A |
| 6 | P18 prototype parking spot; Ice checkpoint (p2–3) | P18: X 1057.44, H 351.4, Z ~5.90, **Y missing**. Ice: X 964.25, Y −3248.37 (written "-3248 37"), Z 5.89, H 314.1. The Y for P18 must be captured; the survey HUD hid it. | C |
| 7 | Ice lowers the rifle when the switch prompt appears (p3) | Confirmed: the role gate disables `Control.Aim` on the frame it engages, so a player-controlled Ice drops his aim; when AI takes over he gets `AimAt`, but the gap between is visible. | C |
| 8 | Laptop floats above the table (p3) | Confirmed: `prop_laptop_01a` is placed at a fixed +0.82 Z from the worksite, not on the table prop's actual top. | C |
| 9 | Highlight attackers on the map; green marker on Mateo; crew must not shoot him (p3–4) | Guards have no blips during the firefight. Mateo's blip is yellow and he is mortal; the crew group hates the cartel group and Mateo is a cartel ped, so companion AI can legitimately target him. | C |
| 10 | Mateo dies; mission still says "get to the dock" (p4) | Confirmed: Mateo's flight starts only when the recognition clock runs out or guards drop to two, so he stands in the crossfire first; and `OnUpdate` fails the mission on his death only in some stages. Reorder: he leaves before the wave opens, protected while scripted. | C |
| 11 | M02 chase car start position; waypoint doesn't follow the van; should vanish when close (p4) | Navigation is re-submitted each tick with the van's position, but the route blip is a free blip whose position is moved, not a blip attached to the van; GTA recomputes routes lazily. No "near" state exists. | C |
| 12 | Van parks at a red light and never moves, even under fire (p5) | Confirmed: `CruiseWithVehicle(20, DrivingStyle.Normal)` obeys lights and traffic. | C |
| 13 | Ice can't get back in the chase car after the pickup unless you switch (p5) | The pickup interaction leaves Ice's role gate and scripted control engaged; the reboard objective is owned by Guess. | C |
| 14 | No clear waypoint per objective; safe endpoints should require losing the cops (p5) | M02 ends with `Game.Player.WantedLevel = 0` at the canal. Policy change: lose the pursuit before a safe arrival. | C |
| 15 | Mission markers should say M1, M2 (p5–6) | `MissionMarkers` names blips "Bloodlines: Title". Add the id. | A |
| 16 | M03 opens on the wrong character (p6, photo) | **Confirmed.** `StartMission` stands the crew down, then the briefing plays with no crew peds; the director falls back to radio framing on the current player, who is the story character. Same cause as the M04 Franklin shot (p9). | A |
| 17 | M03 teleports to the objective; should drive from the start (p6–7) | Confirmed: Guess is deployed in a Primo 25 m from the rail junction; Ice 35 m from the depot. | D |
| 18 | M03 "get out and reach the yellow marker" should trigger by driving up (p7) | `MissionInteraction` requires on foot plus a button press. Add an arrival mode: stop inside the zone → Ron gets out and the work starts. | D |
| 19 | Ice should start near his objective; Ice and Gohan together; entry trigger before the first guard (p7–8) | Design: their staging is a story cut, Ron's is a drive. Guards currently hostile from stage entry. | D |
| 20 | "no ability or button is needed" caption (p7) | **Decided:** it should *not* be there. Remove that wording from the M03 HUD and from any other objective label that carries it. | D |
| 21 | Fail if you shoot before the objective (p7–8) | Add a pre-breach quiet rule scoped to the depot actors, not to Ron's ambush. | D |
| 22 | Gohan "loads weapons" by standing in a field (p8) | Confirmed: the crane stage is a bare timer at `M03.CraneControls`; the Benson sits elsewhere. Needs a visible loading presentation at the truck. | D |
| 23 | The neighborhood ambush on Guess never happens; there is no ambush in the mission (p8) | Confirmed: no ambush, no dogs in `M03CypressFoundry.cs`. The dogs Ron met were ambient. | D |
| 24 | Guarantee at least three dogs; truck parked in the field; Gohan loads it (p9) | Mission-owned dogs and the loading site at the actual Benson. | D |
| 25 | Two-star wanted level, police drove past in plain view (p9) | Not from the crew's relationship groups (no relationship with COP is set) and not from dispatch (the mod shortens spawn intervals). Leading candidates: the stars were in the "search" state (greyed) after a restore by `PersonalWanted`, or police response was suppressed by a flag left set. Needs instrumentation before a fix. | C |
| 26 | Force Guess out of the car at the end and lock it (p9) | **Decided:** the truck Guess drives back to the hideout (the Benson). Guess is put out of it at the foundry and it locks; his approach car is not touched. | D |
| 27 | Franklin in the M04 cutscene (p9) | Same cause as #16. | A |
| 28 | Gohan cuts the power and is forced to switch to Ice while the shootout starts; objective sits inside the meeting; should drive there (p10) | Confirmed: "Kill the lights" is followed by "Take the ramp" owned by Ice, whose entry turns six bodyguards hostile. Gohan is deployed 15 m from the garage. | D |
| 29 | Debug: pass to next objective; retry failed objective (p10) | The dev menu warps a stage index and refuses when reconstruction is unsupported. Needs real "complete this objective" and "restart this stage" hooks plus a full-precision coordinate readout. | A |
| — | Buzzard felt no faster (message) | **Confirmed at the SDK level:** the pinned 3.6 reference assembly contains no `FlyingHandlingData` type. `TravelHandling.Apply` looks for it by reflection and returns silently when absent, so on any runtime that doesn't expose it, no aircraft or boat change is ever made, and the log never says so. `SET_VEHICLE_CHEAT_POWER_INCREASE` does not drive rotors. | B |

---

## 2. Package B — Handling first, then ability, then aircraft

### 2.1 What the SDK actually lets us change

Verified against the pinned 3.6 assembly. Shared per-model handling (affects every vehicle of that model, restore on stand-down, same ownership rules `WorldTuning` already uses): `TractionCurveMax`, `TractionCurveMin`, `TractionCurveLateral`, `TractionLossMultiplier`, `LowSpeedTractionLossMultiplier`, `TractionBiasFront`, `SuspensionForce`, `SuspensionCompressionDamping`, `SuspensionReboundDamping`, `SuspensionUpperLimit`, `SuspensionLowerLimit`, `CenterOfMassOffset`, `InertiaMultiplier`, `RollCenterHeightFront/Rear`, `CamberStiffness`, `BrakeForce`, `BrakeBiasFront`, `HandBrakeForce`, `SteeringLock`, `InitialDragCoefficient`, `Mass`. Not present in 3.6: any downforce field, any `FlyingHandlingData` or `BoatHandlingData`.

Per vehicle instance (current car only, frame-safe): `Vehicle.Gravity`, `ApplyForce` (`APPLY_FORCE_TO_ENTITY`), `SET_VEHICLE_REDUCE_GRIP`, `SET_VEHICLE_CHEAT_POWER_INCREASE`, `SET_VEHICLE_MAX_SPEED`.

### 2.2 Baseline road profile (everyone, no ability)

A new `RoadHandling` profile applied once per model alongside the existing gearing change, derived from the model's original values, never re-multiplied, restored with them. Starting values for calibration, by class:

| Field | Sedans / coupes (Primo, Schafter) | SUV / pickup (Granger) | Truck (Benson) | Why |
| --- | --- | --- | --- | --- |
| TractionCurveMax / Min | +15% / +15% | +12% / +12% | +8% / +8% | The grip the doubled speed needs for the same corner |
| TractionLossMultiplier | ×0.85 | ×0.85 | ×0.9 | Breakaway becomes recoverable instead of a snap |
| SuspensionCompressionDamping / ReboundDamping | +25% / +25% | +30% / +30% | +20% / +20% | Settles after bumps; stops the launch off crests |
| CenterOfMassOffset.Z | −0.12 | −0.15 | −0.10 | "Weighted to the ground" without touching mass |
| BrakeForce | +40% | +40% | +30% | A stopping envelope for the new speed |
| InertiaMultiplier.Z (yaw) | +10% | +10% | 0 | Calmer rotation at speed |
| SteeringLock | unchanged | unchanged | unchanged | The engine already reduces lock with speed; raising it makes twitch worse |
| Mass | unchanged in round one | unchanged | unchanged | Test last; mass alone does not fix steering |

Bikes, boats and aircraft get no road profile. These are opening numbers for a paired road loop, not final values.

**Calibration protocol:** same loop (Vespucci Blvd → Del Perro freeway → Rockford Hills), same weather, same four cars (Primo, Schafter, Granger, Benson), three runs each: vanilla, current speed tuning, handling-first at the same speed target. Log achieved speed, lap time, braking distance from 100 km/h, roll/airborne time, and Ron's verdict. Only after that: consider the speed target.

### 2.3 Guess — Slipstream Reflex, Franklin-style

Keep the time scale at 0.45 for round one so handling is the only variable. Add a per-instance overlay on the vehicle Guess is driving:

- `Vehicle.Gravity` ×1.25 while at least three wheels touch the road (planted; ramps and jumps unaffected because the check drops it airborne).
- Bounded downforce: `ApplyForce` straight down, proportional to speed squared, capped at 0.6 g equivalent, only while grounded and upright, ramped in over 0.5 s and out over 0.5 s so the ability ending mid-corner does not drop the car.
- `SET_VEHICLE_REDUCE_GRIP false` (already there), tires protected (already there).
- A temporary grip overlay on the shared handling (`TractionCurveMax/Lateral` +20%) with the same ownership tracking `WorldTuning` uses, restored on deactivate, vehicle change, switch, death, abort, stand-down and reload. **Tradeoff, stated plainly:** shared handling is per model, so a same-model NPC car nearby grips better for those seconds. Acceptable; the alternative (none) leaves steering untouched.
- Nothing assigns velocity, snaps heading, or pins an airborne car. Never applied to aircraft or boats.

**Acceptance:** at matched speed on the same loop, ability-on places the car through corners and traffic more easily than ability-off; ending it mid-corner is smooth; a second identical car nearby returns to baseline within a second of deactivation.

### 2.4 Buzzard and aircraft

1. **Say what happened.** `TravelHandling.Apply` logs one line per model: which properties were found, the original and applied values, or "unsupported on this runtime". Today it is silent.
2. **Measure.** A dev readout showing model, ground speed, air speed, altitude and assistance state, so "no faster" becomes a number.
3. **Helicopter cruise assist that exists in 3.6.** When airborne, above a small forward speed, with forward stick, apply a bounded forward `ApplyForce` along the aircraft's forward vector, ramped like the road torque, capped so the Buzzard's cruise rises by a measured percentage (target +30% for round one). Hover, takeoff, yaw and landing get nothing, so they keep their feel. Planes keep the existing rule until measured.
4. If the runtime does expose the flight handling type, keep the current property edits and log them; the force assist is the fallback, not a replacement.

**Acceptance:** paired runs, same altitude and route, stock versus assisted, in both directions: a measured cruise gain with controllable turns and stopping; safe hover and landing.

---

## 3. Package A — Shared start, scene and control ownership

**The wrong-cast bug (#16, #27).** `StartMission` stands the crew down, then `MissionManager.Start` plays the briefing. With no crew peds alive, `CutsceneDirector` can't find an actor for any speaker, marks the scene "radio", and frames the current player: your story character. Fix: when a speaking hero has no live ped, stage a temporary actor with the hero's appearance at the start marker (the M01 cold open already does exactly this for its three approaches), hide the story ped for the scene's duration, and dismiss the temporary cast when gameplay begins. Outros keep using the real crew.

**Drive from the start (#17, #28).** A `DeployAtStart` helper places the active hero (and any hero who is with him) at the mission's start marker, in the vehicle he arrived in when there is one, and the first stage becomes a travel objective. Story cuts (M01's three approaches, split staging) stay explicit per mission.

**Immobile car (#5).** Add the diagnostic record the external plan describes, written to the log when a mission begins and whenever control is restored: mission and phase, player handle and model, vehicle and seat, `CanControlCharacter`, frozen and invincible flags on ped and vehicle, driveable and engine state, role gate, companion state of the active slot, last scene outcome, apartment busy/inside, collision readiness. Then retest on the fix branch, which already replaced the async exit. Candidate causes to eliminate in order: the active slot under scripted companion control at M01 start, the frozen-flag restore of the held vehicle, the role gate.

**Marker names (#15).** Blips read "M01 — Ghost in the Dockyard".

**Debug tools (#29).** Two real actions, distinct from stage warping: *complete current objective* (calls the objective's declared completion, runs stage exit effects, validates the next stage's entities) and *restart this stage* (only when the stage declares it can rebuild; otherwise offers the full retry it already has). A full-precision coordinate line (X, Y, Z, heading, and the effective location key with its source: data file, your INI, or survey) drawn at the **top right of the screen**, where the objective text cannot cover it (Ron could not read P18's Y because the objective line sat on top of it), plus a copy to the log.

**Also in A:** the live retest of R01 (route) and R02 (scene outcomes) from the fix branch, since Ron's playthrough predates them.

---

## 4. Package C — M01, M02 and the police policy

### M01
- Prototype at P18 once the Y is captured; Ice's checkpoint at his coordinate. Both go into `locations.tsv`; the stale-template guard on the fix branch keeps his old INI from overriding them.
- Ice keeps aiming: the role gate stops disabling `Control.Aim` for a hero who has just completed an aim objective; AI Ice gets aim-only overwatch with no target selection.
- Laptop on the table: place it relative to the table prop's top bound, same helper for scene and gameplay.
- Red entity blips on required attackers; a green blip and marker on Mateo with "Keep him alive" text; pending, alive, dead and escaped states so nothing goes stale.
- Mateo cannot be a target: his own relationship group, neutral to the crew and hated by nobody the crew fights; explicit combat target lists exclude him.
- Escape order: recognition → Mateo starts his scripted run to the launch, protected while scripted (bounded invincibility until aboard) → the hostile wave opens → clear the required attackers → police pressure rises → all three board the prototype. His success is recorded and he is retired; the mission never points at the dock afterward. Watch and skip reach the same state.

### M02
- Chase car start and van route surveyed along the road direction.
- Van: mission-owned pursuit driving (rushed style, ignores lights, avoids traffic), a stuck watchdog that re-issues once after five seconds stationary, and it stops only when Gohan's hack disables it.
- Navigation: a blip attached to the van entity with a route while far; inside hack range the route drops and a target marker plus range readout remain; after the pickup the chase car; then the escape.
- Ice reboards: the pickup releases his scripted control and role gate; if AI, he is assigned a free seat; Guess waits for the required passengers with a visible reason if one cannot board.
- Safe arrival: the canal is a safe endpoint, so the stage requires the wanted level to be gone (a `LoseWantedObjective` before delivery) instead of zeroing it.

### Police policy (#25, #14)
Instrument first: on every switch and restore, log wanted level, greyed-stars state, and any ignore flags. Then: operational checkpoints are allowed under heat; safe arrivals require the pursuit lost, with the HUD saying why. No safehouse erases stars. `PersonalWanted` restores must not leave the police in a permanent search state; if a legitimate sighting happens, they respond.

---

## 5. Package D — M03 and M04 as the owner intends them

### M03
1. Briefing with the right cast at the start marker. Ron drives to the rail junction; Ice and Gohan are staged together near the depot by a short scripted cut with a line that explains it.
2. Arrival mode at the junction: stop inside the zone, Ron gets out and the work starts. No extra button. Rolling through at speed does not eject him.
3. The neighborhood ambush on Guess: a squad that comes out of the houses along the junction street, with cover, on a timer after his work starts.
4. At least three mission-owned dogs in that block, spawned before Ron arrives, aggressive to the crew, cleaned up with the mission.
5. Ice's entry trigger sits before the first guard; guards hold their patrol until it fires.
6. Pre-breach quiet rule: firing before the trigger fails with an explanation, scoped to the depot actors so Ron's ambush is unaffected.
7. Gohan loads the Benson where it is parked: walk to the rear, open, a looped carry/load task with crate props that appear in the bed, close. The stage completes on the visible sequence, not a timer in a field.
8. Ron collects the loaded truck and delivers it; parallel pressure is held while a switch is forced, so a switch never causes an unavoidable death.
9. Lose the pursuit before the foundry, deliver, Ron gets out of the Benson through a tested exit, and the Benson locks. Only the delivered truck.

### M04
Gohan drives to a breaker moved outside the meeting. After the cut, he takes cover and can fight; Ice is offered as the tactical switch, not forced. If a switch must be mandatory, it happens before the bodyguards turn hostile, or with a protected hand-over. The right cast appears in the briefing.

### Prologue apartment call (#4)
After Package A proves scene ownership: park → get out → concealed-door fade → Ron inside the starter apartment → sets his bag down / walks to the window → the phone message arrives → short call → time cut to M01. Reuses `ApartmentAccess`. If the interior fails to load, exterior control comes back with a clear retry; an unseen call is never marked done.

---

## 6. What I recommend against

- Cutting the speed target now (Ron's direction, and it would hide the handling gap).
- Faking Buzzard speed with FOV, a HUD multiplier or velocity assignment.
- Mutating every NPC copy of a model for the ability without ownership tracking.
- Making the taxi the opening by default; it stays an alternative until Ron picks it.
- Enabling checkpoint restoration globally to satisfy the debug request.

---

## 7. Order and gates

| Package | Contents | Gate to start the next |
| --- | --- | --- |
| **A** | Cast staging, drive-from-start helper, diagnostic record, marker ids, debug objective tools, live retest of R01/R02, immobile-car root cause | M01/M03/M04 show the right cast; Ron never needs a debug death; the prologue route draws |
| **B** | Road baseline profile, Guess overlay, aircraft diagnostics and heli cruise assist, calibration runs | Ron's verdict on the four-car loop and the Buzzard pair |
| **C** | M01 staging/targets/escape, M02 pursuit/navigation/boarding, police policy and instrumentation | M01 watched and skipped; M02 through the canal with the pursuit lost |
| **D** | M03 rebuilt as designed, M04 opening, apartment call | M03 chain playable end to end; M04 opening survivable as Gohan |

Each package: its own branch from the merged build, the full source suite plus CI, install with a receipt, and a live-test list with the note numbers it closes.

---

## 8. Evidence to return per package

Source SHA and installed DLL hash; tests run with counts; files changed; note numbers resolved and deferred; live tests actually performed with what happened; for B, the measured numbers and Ron's preference. No note is "fixed" because a helper exists.

---

## 9. Decisions only Ron can make

1. **P18's Y coordinate** and confirmation of Z 5.90. Open: the Package A readout at the top right of the screen exists to capture it.
2. **Ice's checkpoint Y:** the reading of "-3248 37" is -3248.37. Open: confirm or recapture with the same readout.
3. ~~The M03 caption~~ **Decided September 9:** the "no ability or button is needed" wording should not be there. Removed in Package D.
4. ~~Which vehicle is locked~~ **Decided:** the Benson Guess drives back to the hideout.
5. ~~Taxi opening~~ **Decided for now:** the self-driven opening stays as is. The taxi idea is parked until Ron has planned how Guess actually comes to own a car, since the taxi version removes the car he drives home in.
6. ~~Speed target~~ **Decided:** the target stays where it is. The work is making driving good at those top speeds, per the handling table in the external repair plan (its §4) and §2 here. No speed reduction.
