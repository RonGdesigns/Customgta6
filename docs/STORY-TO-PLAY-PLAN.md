# From Story to Play: implementation plan

**Source:** `Bloodlines_From_Story_to_Play_Full_Proposal.html` (review draft v1, September 10, 2026).
**Reconciled against:** main at `d31b1b0` (the proposal pinned `24a23e9`; since then: round two, the reviewed visuals and deformation, and the M01 exit fix).
**Status (September 10):** P1 decisions recorded in `CHANGE-REGISTER.md` (Miller's fate is the player's; the rest are the marked defaults). P2a building blocks and the P2b M04 reference are merged. P2c (the interior call, M01's run, M02's stash beat, route, custody and endpoint) is built on branch `claude/s2p-open`, with Ron's September 10 corrections to M01 folded in: no clock, the run is seen, the yard must be clear. P2d (M03) is merged. P3 is in progress: P3a (M05, M06) is merged; P3b (M07, M08) is merged; P3c (M09, M10, M11) is merged; P3d (SM01, SM02, SM03) is built on branch `claude/s2p-solos`. P4a (M12–M15) is built on branch `claude/s2p-harbor`; P4b (M16–M18) on `claude/s2p-lift`; P4c (M19–M22, the Port Heist as one operation) on `claude/s2p-heist`; P5a (M23–M27, the desert) on `claude/s2p-sage`. Ron's September 11 review: the proposal covers M12–M30 and SM04–SM06 as well (Packages 4 and 5), which the first draft of this plan never scheduled; the P4 and P5 rows below do. M31–M70 have no runtime scripts and are not adaptations: they stay out of this plan until Ron says otherwise.

The proposal's principle is adopted whole: context before control, consequence before the next job, and a scene may only claim a result the world actually reached. This plan says how that gets built in this codebase, in what order, and what Ron has to decide first.

---

## 1. What already exists at HEAD

The proposal was written against an older pin, so several of its asks are already in. These are preserved, not redone.

| Proposal ask | State at `d31b1b0` | Where |
| --- | --- | --- |
| Watched and skipped scenes reach the same state; cancel is not success | Done. `SceneBlocking.Complete` vs `Cancel`; `CutsceneDirector.LastOutcome` (Completed / Skipped / Canceled / Failed) | `Core/SceneBlocking.cs`, `Core/CutsceneDirector.cs` |
| Right cast in briefings; Guess at the start, the others drive up | Done. `StageHost`, `StageArrival`, `DriveUpStep`, dialogue held until the car arrives | `Core/CutsceneDirector.cs` |
| Control diagnostics at scene end and mission start | Done. `ControlDiagnostics.Snapshot` | `Core/ControlDiagnostics.cs` |
| Map label with stable ID and title | Done. `M04 — Severed Wire` | `Core/MissionMarkers.cs` |
| Objective debug that runs exit effects | Done. `CompleteCurrentObjective`, dev menu entry | `Missions/ComposedMission.cs`, `Core/DevMenu.cs` |
| M04: Miller reacts as a driver, not a commuter; pickup is readable; cars survive | Partly. Flee mission at speed, reach-in animation, cars released on pass. Miller still waits for the escort stage | `Missions/Campaign/Act1/M04SeveredWire.cs` |
| M06 SWAT arrives by air, first arrival on camera | Done | `Missions/HeliInsertion.cs` |
| Road handling before speed; Guess ability planted | Done, calibration pending | `Core/RoadHandling.cs`, `Abilities/SlipstreamReflex.cs` |
| Prologue: self-drive, surveyed curb, arrival at the door, hand-off into M01 | Done, including the September 10 exit-point fix | `Core/PrologueSequence.cs` |
| M01 identification job, usable copy, recognition over the channel | Done in data. `opening_scene.txt` is the M01 source; the older assassination lines in `story_beats.txt` are superseded by the generator | `data/opening_scene.txt`, `tools/build_story.py` |
| Story gates SM01–03 → M19, SM04–06 → M44, SM07–08 → M63, SM09 → M68 | Done | `Missions/CampaignState.cs` |
| Weapon rewards that mean something | Done, 33 of 36 jobs | `Crew/WeaponProgression.cs` |

What is **not** there and this plan builds: playable approach from a briefing, a transaction the player can see, enemy reactions as triggers instead of kill counts, inactive heroes with jobs and threat responses, a switch that is offered before it is forced, evidence and cargo that survive a camera cut, an interior call in the prologue, the M01 → M02 vehicle bridge, M03's ambush and real loading, the mission-context card for a skipped briefing, and a recap on retry.

---

## 2. Decisions Ron makes before code

Each has a recommended default. If a decision is not made, the default is used and marked as an assumption in the change register, never silently promoted to canon.

| ID | Question | Recommended default | What I will not do without a yes |
| --- | --- | --- | --- |
| O02 | Airport car and apartment | Keep the Primo at the surveyed curb and the current starter room. Say the car "was arranged" without naming who. | Adopt the novel's stall, address, $50,000, or the taxi. |
| O03 | M01 → M02 vehicle | Keep the Granger. Add a stash beat: the prototype is left at the foundry-side stash, the Granger is the crew's own from the same place. | Declare the prototype sold, or swap vehicles off camera. |
| O04 | Miller's fate and the buyer | Unnamed buyer. The drive is the required result. Killing Miller is allowed, not required; aftermath wording is neutral either way. | Imply a required killing. |
| D-M03a | Ambush affiliation at the rail junction | Local street crew hired for the night, not Aegis, so the depot's covert rule stays untouched. | Make it Aegis and tangle it with the depot threshold. |
| D-M03b | Dogs | Three, mission-owned, hostile to the crew, no kill requirement. Already decided in principle; confirming the count. | Add a dog-kill objective. |
| D-M04a | Where M04's briefing plays | At the foundry (Cypress base), then the crew drives to Pillbox Hill. | Start the mission already at the lot. |
| D-M04b | The switch when Miller runs | Offer the switch for 8 s while Ron's AI shadows Miller; force it with a protected hand-over if not taken. | Instant lock on the wrong character. |
| D-PRO | Prologue interior call | Yes, after the room is proven live (apartment fix is in; not yet live-verified). | Cut into a room that failed to load. |

Everything else in the proposal's O01–O10 list (Mateo's fate, smoke operators, money, Vance, M51/M60, the finale) is outside this slice and stays open.

---

## 3. Building blocks

These are engine pieces, each small, each tested in the story suite, each usable by every later mission. They are built first, with M04 as the first consumer, so the reference mission is not a pile of one-off code.

### B1. Scene specification and the context card
- `SceneSpec`: id, reason (what the player must learn), trigger, cast and roles, starting state, blocking, result, skip result, cancel result, resume state, cleanup. `CutsceneDirector.Play` gains an overload that takes it; the existing overload stays for current callers.
- `MissionContextCard`: target, reason, roles, immediate destination. Shown for 6 s after a skipped briefing and as a one-line recap when a mission is retried. Data comes from a new optional column in `mission_gameplay.tsv`; missions without it show nothing new.

### B2. Scene steps the slice needs
Added to `SceneBlocking.cs` beside the existing seven, each with `IsComplete`, `Finish`, `Cancel`, timeout:
- `CarryPropStep`: attach a prop to a hand (the evidence case, the drive, three keys). `Finish` attaches instantly.
- `InspectStep`: turn to a point and play a bounded animation (open the case, check the photograph). `Finish` clears it.
- `HandoverStep`: two peds face each other, the prop moves from one hand to the other. `Finish` moves it.
- `TakeCoverStep`: move to a cover point and stay. `Finish` places.
- `RadioLineStep`: a line with the camera left where it is, for calls during a playable approach.
- `InsertStep`: a 5–8 s camera look at an entity or point while gameplay continues underneath, with the safety rule below.
No animation dictionary or prop name goes in without a live proof first; the step logs which one it used.

### B3. Role tracks for inactive heroes
- `RoleTrack`: a per-hero, per-mission progression (`Approaching`, `Observing`, `Working`, `Threatened`, `Covering`, `Extracting`) that drives `CompanionController` tasks while the hero is not the player.
- A threat hook: when a tracked hero takes fire or an enemy closes within a radius, the track moves to `Threatened` (cover, return fire, resume work when clear) without dropping mission ownership. A switch into that hero picks up the track state; a switch away hands it back.
- This is a thin layer over the existing `TakeControl` / `StateOf` ownership, not a new AI architecture.

### B4. Evidence and custody
- `CampaignState.Evidence`: named items with a state each (`Alleged`, `CopyHeld`, `Proven`, `Distributed`). The slice writes `ledgerLead` (M01), `dockRecording` (M02), `millerDrive` (M04), `mateoAllegation` (M05). Later chapters read them; nothing in this slice reads ahead.
- `CustodyItem`: a prop plus its holder that a mission registers as required-through-cutscene. `Mission.Cleanup` releases it to the next chapter's session ledger instead of deleting it, the same pattern as `HandoffLedger`.

### B5. Reaction triggers
- `ReactionTrigger` objective: passive, fires a mission callback when a condition is met (the breaker is cut; the player is detected; a named enemy dies; a radius is entered). Stages stop waiting for "all escort dead" where the fiction does not require it.

### B6. Playable approach
- `DeployAtStart` (from the repair plan's Package C): the active hero and whoever rides with him start at the briefing site, in the vehicle they arrived in, and a `TravelObjective` with distance-triggered radio lines takes them to the site. Arrival mode stops the car inside a zone without a button.

### B7. Offered switch
- `SwitchWindowObjective`: shows the role change, offers the switch for N seconds while the target hero's `RoleTrack` shadows the job, then forces it through the existing `MissionHandoff` with the target hero placed in cover first.

### B8. Endpoint policy
- Every mission's last stage declares its endpoint kind: `EscapeCheckpoint`, `SecuredDelivery`, `SafehouseArrival`, `ContinuousNext`. Only `SafehouseArrival` may clear the wanted level, and only after `LoseWantedObjective` has passed. M02 and M03 currently zero it at a marker; that changes.

### B9. Prologue interior call
- Reuse `ApartmentAccess`: park, exit, concealed-door fade, inside, set the bag down, cross to the window, the message arrives, read the job, keys, leave, time cut. Skip lands in the same "job accepted" state. A failed room load returns control outside with the job still readable at the door, as now.

### B10. Authoring and data
- New scene phases go in `story_beats.txt` under the mission with a phase tag (`transaction`, `flight`, `stash`, `call`); `build_story.py` already keys scenes by `mission:phase`. Existing cue IDs are never renamed; the proposal's planning keys (`M04-CS-B`) map to cue IDs in the change register, not in code.
- `parse_bible.py` is untouched. Nothing from the novel's chapter numbers enters any data file.

### B11. Tests
- Story suite: each scene's trigger, watch, skip, cancel; custody survives cleanup; role tracks respond to a threat; the offered switch forces after its window; the endpoint policy refuses a wanted reset on an escape checkpoint.
- Live checklist per scene: cast, positions, prop, animation, camera, skip, cancel, retry, and the comprehension questions from §5.

---

## 4. Mission change matrix for the slice

Keys: **keep** = already right; **adapt** = change existing code; **new** = new scene or objective; **decide** = waits on §2.

### Prologue — Home Before the Job
| Beat | State | Work |
| --- | --- | --- |
| LSIA arrival, two calls, the arranged car | keep | Line 4 stays neutral on who arranged it (O02). |
| Drive home | keep | Add two distance-triggered `RadioLineStep` observations at most. |
| Interior call | new (B9) | Requires D-PRO and one live apartment pass first. |
| Later-that-night cut to the dock | keep | Exit fix is in. |
Cues: `M01:prologue`, `M01:arrival` unchanged; new `M01:call` (two lines, Guess only, generator rule "Ron is alone" holds).

### M01 — Ghost in the Dockyard
| Beat | State | Work |
| --- | --- | --- |
| Three private approaches, table and laptop, lookout | keep | P18 survey still owed. |
| Recognition over the channel; Mateo runs before the fight | adapt | Move Mateo's escape start to the recognition trigger (B5), not a timer. Record `ledgerLead` (B4). Record Mateo's escape once so a distant despawn is not death. |
| Police converge, board the prototype, break the gate | keep | Exit is the gate. |
| Recording/upload problem shown on Gohan's device | new | One `InsertStep` on the laptop during the outro. |
| Prototype → Granger | new (O03) | Stash beat at the top of M02, not the bottom of M01. |

### M02 — Loose Strands
| Beat | State | Work |
| --- | --- | --- |
| Stash beat: prototype left, Granger taken | new (O03) | `M02:stash`, three lines, `EnterVehicleStep` into the Granger; countdown starts after it. |
| Van follows a route, does not sit at lights | adapt | Replace `CruiseWithVehicle(Normal)` with a vehicle mission on a route (same pattern as Miller's flee). |
| Van crew detects and shoots; Gohan's work disables the drivetrain | adapt | `ReactionTrigger` on detection; the disable is Gohan's objective result, shown by an `InsertStep`, never a forced collision. |
| Ice takes the drive from the rear doors | adapt | `CarryPropStep` on Ice; `dockRecording` → `CopyHeld` (B4). |
| Evade, then Gohan names the thread, Ron proposes Cypress | adapt | Endpoint `EscapeCheckpoint` (B8): no wanted reset at the marker. |

### M03 — Cypress Foundry
| Beat | State | Work |
| --- | --- | --- |
| Walk the empty foundry; keys are not the opening prop | new | Briefing at the base as a short walk with three `WalkToStep`s. |
| Ron drives to the junction, works, ambush and dogs | new (D-M03a/b) | Repair plan Package D items 2–4. |
| Ice's entry trigger, covert rule scoped to the depot | adapt | Package D items 5–6. |
| Gohan loads real crates into the Benson | new | Package D item 7; crates as `CustodyItem`s. |
| Run it home, lose pursuit, park, Benson locks, keys on the table | adapt/new | Package D items 8–9; `M03:keys` insert; endpoint `SafehouseArrival`. |

### M04 — Severed Wire (reference mission)
| Beat | Proposal key | State | Work |
| --- | --- | --- | --- |
| Recovered-drive briefing at the base | M04-CS-A | adapt | Existing intro cues stay; add the laptop prop and Ron picking up the keys (`CarryPropStep`). Ends with control at the base (D-M04a). |
| Drive to Pillbox Hill with drop-offs | M04-GP-A | new (B6) | `TravelObjective` with two radio lines; Gohan dropped at the breaker side, Ice at the cover angle, Ron parks nose-out. Unsuitable vehicle: explicit transfer to the Buffalo, old car released. |
| The transaction is real | M04-CS-B | new (B2) | Miller arrives with the case, buyer inspects, guards cover; 20–30 s; establishes seller's car, buyer's car, chase car. |
| Gohan works the breaker, then reaches cover | M04-GP-B | adapt | Existing interaction; on completion the `RoleTrack` moves Gohan to `Covering`. Local lights only. |
| Miller runs because of the breaker | M04-AI-C | adapt (B5) | `ReactionTrigger` on the breaker replaces "escort dead"; buyer ducks, guard covers, Miller boards and flees (flee mission is in). 5–8 s `InsertStep` if safe, else a radio line and the moving marker. |
| Two connected jobs | M04-GP-C | new (B3, B7) | Ron shadows Miller under AI; `SwitchWindowObjective` offers Guess (D-M04b); Ice's track keeps the escort off Gohan; both extract on their own. |
| Recover the copy | M04-GP-D | keep/adapt | Reach-in is in; add the case as a `CustodyItem`; neutral wording on Miller (O04); regroup through the Buffalo, not a run across the map. |
| The next lead | M04-CS-E | adapt | Existing outro cues; add the laptop insert; `millerDrive` → `CopyHeld`. No M46 material. |

Acceptance for M04 is the proposal's §4.3 test, verbatim: a tester who has not read either book explains the three roles, the breaker, the flight, the object, and why Mateo is next.

---

## 5. Order, branches, and gates

One branch per package, one PR each, CI green before review, no merge without Ron. Every package ends with the full suite (story, regression, lint, coordinates, freshness, parser, dialect) and a live checklist that names what was actually run.

| Package | Branch | Contents | Done when |
| --- | --- | --- | --- |
| P1 Canon and scene map | `claude/s2p-canon` | Change-register rows for every §4 line (preserved / adopted / adapted / deferred / blocked); cue crosswalk from proposal keys to cue IDs; §2 decisions recorded with Ron's answers or the marked defaults. Docs only. | No mission has two contradictory current plans. |
| P2a Building blocks | `claude/s2p-blocks` | B1–B8, B10, B11 with story tests. No mission behavior changes yet except where a block replaces an equivalent call. | 900+ story checks, all new blocks covered, existing missions unchanged in play. |
| P2b M04 reference | `claude/s2p-m04` | §4 M04 rows on top of P2a. | §4.3 comprehension test passed live by Ron or a second tester. |
| P2c Prologue, M01, M02 | `claude/s2p-open` | Interior call (B9), M01 adapt rows, M02 stash and chase rows. | Fresh playthrough to M03 with no wrong cast, freeze, route ambiguity, or invisible transfer. |
| P2d M03 | `claude/s2p-m03` | Repair plan Package D items 1–9 with B3/B4/B8. | Ambush, dogs, visible loading, locked Benson, keys on the table, all live. |
| Live gate | — | Fresh save through M04 on the installed build. | Proposal §10 Package 2 "done when", plus watch/skip/cancel per scene. |
| P3a Witness and backup | `claude/s2p-witness` | M05: the cove seen, Ice down to the shore, Mateo aboard for the staged questioning, the allegation recorded. M06: positions seen, visible burn with a real fire, the rotors turning Ron's wait into a pickup, the backup's destruction recorded. New block: `PlayStaged` (a gameplay stage's lines as a scene). | M05 does not spoil M46; the pickup is seen; both scenes pass watch/skip/cancel live. |
| P3b The dish and the engines | `claude/s2p-relay` | M07: the relay identified on the approach, the sniffer seen on the antenna, the helicopter response seen before control returns, a tested descent, Ron's pickup. M08: two identifiable crates, the loading seen and counted, the technical's approach seen, delivery to a temporary stash that persists. | The player can say how the tower job identified M08's cargo; the same crates are there at M09. |
| P3c The IFF, the truck, the shop | `claude/s2p-ironclad` | M09: the convoy on a device, the escort stopped without destroying the unit, the VIP withdrawal seen, the unit removed and its limits stated. M10: the stashed crates recovered and loaded, the gunship's firing window seen, actual engine delivery. M11: walk into the shop mid-work, the calibration kept, Gohan's Berth 44 turn in the room, the upgraded vehicle shown. | The engines physically connect M08, M10 and M11; the player understands the first heist's motive. |
| P4a Preparation (M12–M15) | `claude/s2p-harbor` | M12: the hold number and the craft seen, the scan as work, the launches shown as the next problem, the survey recorded. M13: the barges and the slipway pickup seen, workers named as workers, the alarm shown as the reason to leave, charges dark until aboard, the result view, reduced patrols recorded. M14: the pod and its destination seen, Ice and Gohan leaving the ridge by road, radio cues on the low route, the pod recorded at McKenzie. M15: the access, the rounds, the cable point and Ron's Granger seen, the splice as work, one gate answering as the acknowledgment, gate access recorded. | Each preparation leaves a recorded state (`hullSurvey`, `harborPatrolsReduced`, `radarPod`, `harborGateAccess`) for the heist chapters to consume; nothing claims a cleared harbor, a blind Aegis or a dark port. |
| P4b The lift and the sub (M16–M18) | `claude/s2p-lift` | M16: the IFF unit installed for the limited approach, the Cargobob theft, the loss of clearance as an alarm, the lift landed and recorded. M17: the Kraken at a staging point, distinct work points, the external release changed and tested. M18: the map-on-hood briefing, each asset delivered to a real endpoint, the roll call under one clock, SM01–SM03 resolved before staging. | The player can sketch the operation; no chapter needs an unseen operator. |
| P4c The Port Heist (M19–M22) | `claude/s2p-heist` | One continuous operation state: the sub at its staged location, the breach and float as work, the surfaced load and the hook insert, the escort launch with real seats and M13's reduced patrols, the coastal landing and the staged road transport, the drop and the foundry strike learned before it is explained; the key motif kept. | People, cargo, evidence, vehicles and time can be followed without invented transport; a replay does not rewrite the later ledger. |
| P5a The desert (M23–M27) | `claude/s2p-sage` | M23–M27 per their cards: the refuge earned and its limits shown, one finite portion of the haul recovered with the deputies on a visible route, Ice's bridge and the pickup established before it is the only answer, the Lazer parked and the aircraft change shown, the interception with a valid rescue. | Each set piece works as staged gameplay; the aircraft histories are intelligible. |
| P5b The relay and the fuel (M28–M30) | `claude/s2p-relay2` | M28's technical choice with visible consequence and SM04–SM06 opened, M29's readable transfer and the tanker delivered attached, M30's route reason, controllable truck and the parts' arrival enabling observed intelligence. | The consequence of the chosen system can be described; the truck arrives with the parts. |
| P5c Second-window solos (SM04–SM06) | `claude/s2p-solos2` | The two marksmen and both radios recovered, the buoy job with a boat that waits, the tanker delivered as a delivery; check-ins over radio only; rewards with visible effects. | Each reward's presentation matches its effect; no main scene assumes a solo is done. |
| P3d First-window solos | `claude/s2p-solos` | SM01–SM03 per the proposal's §7: entries that show the broker and crates, the roof and terminal, KJ and the coupe; check-ins over radio only; rewards with visible effects; availability understandable. | Each solo's reward presentation matches its effect; no main scene assumes a solo is done. |

Estimated size: P2a is the largest (six new step types, four new objectives, two state additions, tests). P2b through P2d are mission-local. Each is a day or two of work at the pace of the previous rounds, plus Ron's live passes between them.

---

## 6. What this plan refuses to do

From the proposal's own limits and the project's rules:
- No mission renumbering, prerequisites, or IDs derived from the novel's chapter numbers.
- No novel-only assets, addresses, payments, sponsors, relatives, or ability swaps. The dossier art is mood, not models.
- No taxi opening, no yacht interior, no underground garage, no exact PIT crash, no required Miller kill, no M46 proof in M05.
- No animation dictionary, prop, or interior named in code before a live proof. Each block logs what it used.
- No cinematic that steals a clutch, awards an unearned outcome, or starts a pursuit clock before control returns.
- No wanted reset at a marker the story does not call safe.
- No merge from any package without Ron's word.

---

## 7. What Ron does next

1. Answer the eight rows in §2, or say "defaults" and they are recorded as assumptions.
2. Play the current install through M04 once with the visuals defaults, so P1 starts from a fresh set of notes rather than the September 9 ones.
3. Say which package to start. The recommendation is P1 and P2a together, because P2a's blocks are needed by every later mission and P1 is documentation that can be written while the blocks are built.
