# GTA V: Bloodlines — Agent Handoff for Proposed Changes

## Purpose of this handoff

This document is for the implementation agent that will take the recent **proposal/review documents** and turn the approved ideas into the actual Bloodlines project.

The important distinction is:

> **The proposal documents are planning material, not the current implementation.**

They were created after reviewing the current mission dialogue, story progression, character relationships, technical feasibility, stock GTA V assets, interiors, cutscenes and animation requirements. They contain corrections and recommendations that should now be reconciled against the live repository before implementation.

Do **not** blindly copy every sentence from a proposal document into runtime data. Read the current source-of-truth files first, then implement the intent while preserving working mission logic.

---

# 1. Repository / working branch

Repository: `RonGdesigns/Customgta6`

Proposal work was written on branch:

`claude/gta-v-custom-version-477edi`

Before changing anything, inspect the current branch state and determine whether newer implementation work has superseded any proposal detail.

---

# 2. Documents to read first

Read these in this order:

1. `docs/PROPOSED-STORY-CHANGES.md`
   - opening/prologue direction;
   - M01–M16 review;
   - equal-status trio rule;
   - early dialogue cleanup;
   - M05 conspiracy pacing.

2. `docs/PROPOSED-STORY-CHANGES-BATCH3-4.md`
   - character/relationship rules carried forward;
   - M17–M49 review;
   - locked M10 and M42 wording;
   - clutch distribution;
   - continuity flags.

3. `docs/PROPOSED-SIDE-MISSION-AND-ASSET-PASS.md`
   - side-mission character corrections;
   - proposed additional character-development side missions;
   - stock-first philosophy for optional content.

4. `docs/ASSET-CAMERA-ANIMATION-AUDIT.md`
   - prologue through M70 plus side missions;
   - stock/direct vs substitute vs camera-fake treatment;
   - location/interior requirements;
   - moving cutscene blocking;
   - reusable animation/interaction library;
   - hard engine limits and implementation order.

Then compare those proposals against:

- `docs/COMPLETE-DIALOGUE.md`
- `docs/FEASIBILITY.md`
- `docs/HOMES-VEHICLES-SCENES-UPDATE.md`
- `docs/QA.md`
- `data/dialogue.tsv`
- `data/dialogue_edits.json`
- `data/scenes.tsv`
- `data/feasibility.tsv`
- current mission C# sources under `src/Bloodlines/Missions/`
- `src/Bloodlines/Core/CutsceneDirector.cs`
- current campaign/save/prerequisite code and story tests.

The repository/runtime state wins over old PDFs when they conflict unless the proposal explicitly calls for changing the current implementation.

---

# 3. Core character relationship — do not lose this

Ron / Guess, Ice and Gohan are based on a real-life friendship dynamic and should read as **nearly brothers**.

They are a dynamic trio of equals. Do not write a permanent military-style chain of command between them.

### Ron / Guess

Ron is the **glue of the group**.

He is generally dependable, gets the squad together, keeps everybody connected and usually has the group's best interest in mind. He is the most consistent clutch performer when plans go wrong.

Ron may gradually become the clearest leadership presence, but that should come from responsibility, trust, decision-making and keeping everybody together—not from him outranking Ice or Gohan.

Ron is slightly closer to Ice operationally because Ice is usually present and dependable.

### Ice

Ice is highly dependable and is the strongest tactical/combat anchor.

His decisive tactical speech must not accidentally make him the permanent boss. He naturally takes point when combat, overwatch, assault planning or defensive positioning is his expertise.

Ice clutches a decent amount, normally through controlled tactical action.

### Gohan

Gohan is equally part of the brotherhood, but he has a recurring real-life-inspired habit: he disappears, does his own thing and often does not answer his phone.

His character arc is **not** "I need to prove I am useful enough to belong."

The better arc is:

> Gohan already belongs. He needs to get better at communicating, sharing information and letting his brothers know what he is doing instead of disappearing into a problem by himself.

Gohan clutches less frequently than Ron or Ice, but when he does, the solution can initially sound awkward, over-technical, strange or almost cringe—and then it works exactly when the squad needs it.

### Situational leadership

- Ron takes point naturally on driving, extraction, improvisation and getting everyone home.
- Ice takes point naturally on tactical/combat phases.
- Gohan takes point naturally on hacking, surveillance, intelligence and technical phases.
- Major decisions still belong to all three.

Do not flatten this into "Ron gives every order."

---

# 4. Dialogue rule

A major problem identified during review is that some current spoken dialogue sounds like development/tutorial text.

Use this rule:

> **HUD = mechanics. Dialogue = people.**

Characters should generally not say:

- switch to me;
- enter the yellow marker;
- take the orange-marked vehicle;
- press E / D-pad Right;
- exact objective radii;
- "no ability needed";
- other controller/debug instructions already available through objective UI.

Move those instructions to HUD/objective text while preserving the mission mechanic.

Dialogue should carry:

- urgency;
- tactical information a person would actually say;
- personality;
- brotherhood;
- history;
- jokes;
- disagreement;
- story information.

Also avoid over-writing emotional scenes into therapy language. These are solid grown men. Let short lines, jokes, actions, silence and check-ins carry emotion.

Preferred vocabulary is **nervous / pressure / tense / rattled / adrenaline / concerned**, rather than repeatedly calling the trio scared.

---

# 5. Locked wording / decisions

These are approved proposal decisions and should be preserved unless implementation reveals a genuine conflict.

### M01 vehicle

The M01 prototype/getaway vehicle must have **at least four seats**.

Do not use a two-seat T20.

Ron, Ice and Gohan must be able to physically escape together in the actual stolen vehicle.

### M10

Use:

> **RON:** “I joke when I'm nervous too. Learn the difference.”

### M42

Use:

> **RON:** “Sub deployed. That one had my nerves up. Nobody put that in the flight log.”

### M25

Remove the "hear me scared" characterization. Use pressure/nerves language instead. The proposal's preferred direction is:

> **ICE:** “I nearly did it again. Went quiet so neither of you would hear the pressure in my voice. Next time, I'll make the call.”

### M46

M46 should remain the point where the conspiracy moves from allegation/investigation into **hard evidence**.

M05 should not reveal the entire conspiracy too early.

### M47 / M49 / M70

Treat these as important Ron clutch moments without having Ron constantly brag about being clutch.

M70 is the largest payoff: the plane cannot fly, but Ron realizes the engines still work and finds a ground/seawall escape.

---

# 6. New prologue to implement

The campaign needs a real opening before M01.

### Approved direction

Ron is the player's first point of view.

1. Establishing shot at LSIA.
2. Ron arrives/comes out of the airport.
3. Ron physically walks, checks phone/bag, approaches a vehicle and gets in.
4. Camera hands control to the player with Ron already seated.
5. Player drives from LSIA to Ron's Burro Heights home/chop-shop location.
6. Ron parks and physically exits.
7. Short moving exterior scene/job setup.
8. Transition naturally into M01.

Do not require a new furnished apartment asset. The current Burro Heights exterior/home system is enough for the first implementation.

The prologue should **not** replace M01's current separate-job reunion. M01 remains the accidental reunion where Ron, Ice and Gohan discover one another during their separate Terminal Island assignments.

---

# 7. Cutscene direction

Do not implement the campaign as stagnant dialogue tableaux.

Unless intentional, a cutscene should combine at least two of:

- walking/turning/pacing;
- entering/exiting vehicles;
- phone/clipboard/laptop interaction;
- weapon handling/overwatch;
- crate/bag/equipment interaction;
- environmental movement;
- moving/tracking/orbiting cameras;
- real transition directly into gameplay state.

Example: do not show Ron standing at LSIA while all dialogue plays and then teleport him into a car. Have him walk to the car, enter it, then return control.

### Preserve mission state

M01's current recognition work is the model:

- do not pull separated characters into a fake lineup;
- preserve actual seats/positions;
- use phone/radio framing when separated;
- the final line can launch the actual next action;
- skipping must produce the same gameplay state as watching.

Expand `CutsceneDirector` rather than replacing it if practical.

Useful reusable helpers to consider:

- walk-to;
- face/look-at;
- enter/exit vehicle;
- phone use;
- generic panel/laptop interaction;
- plant/clamp/splice;
- vehicle inspection;
- camera track/orbit;
- door/elevator transition;
- ladder transition;
- state-preserving radio framing.

---

# 8. Stock asset / interior philosophy

The target is to avoid requiring a custom asset pack if the same story function can be sold with installed GTA V content.

Do **not** interpret "stock-only" as "every described fictional room must literally exist."

Use:

- verified existing interiors;
- industrial yards;
- caves/tunnels;
- rooftops;
- office/server spaces;
- warehouses;
- existing vehicles/aircraft/boats;
- stock props;
- camera framing;
- prop attachments;
- particle effects;
- off-camera swaps;
- scripted paths.

If a specific fictional interior is unavailable, preserve its **narrative function** with a convincing stock substitute.

Example:

"Aegis secure archive" needs a convincing secure office/storage/server environment. It does not require a unique custom-built Aegis archive MLO.

### Do not promise unsupported physics

Use camera staging rather than literal simulation for:

- M20 heavy Cargobob cargo physics;
- M27 plane-to-plane boarding / zero-G interior;
- M42 submarine airdrop;
- M47 collapsing rig;
- M55 simultaneous multi-building set piece;
- M62 train/boat/helicopter convergence;
- M70 Titan seawall finale.

Read `docs/ASSET-CAMERA-ANIMATION-AUDIT.md` for the mission-by-mission treatment.

---

# 9. Interiors that need live verification

Before hardcoding final scene descriptions/coordinates, live-test stock candidates for:

- M05 sea-cave/hideout;
- M15 maintenance/utility area;
- M23 bunker if an interior is used;
- M32 Zancudo intake/black-site;
- M41 lodge;
- M44–M47 offshore rig/platform and secure room;
- M50 archive/vault;
- M54 penthouse/redoubt;
- M55 three breach interiors;
- M61 gunship wreck/cavity;
- M63 tower lobby;
- M64 stairwell/elevator shaft;
- M65 executive boardroom;
- SM02 server annex;
- SM07 hotel suite;
- SM08 office/archive.

Verify:

- actual availability in GTA V Enhanced;
- collision;
- entry/exit;
- AI navigation;
- combat behavior;
- camera behavior;
- mission restart/checkpoint behavior.

If a candidate fails, choose another stock substitute rather than forcing a broken scene.

---

# 10. Side mission corrections

SM01–SM09 were reviewed.

General direction:

- Ron's solo material is already fairly close to character.
- Ice's solo missions have strong premises but some aftermath dialogue over-explains his feelings.
- Gohan's side missions need the largest reframing away from "prove my worth" and toward "communicate / stop disappearing."

Specific examples are in `docs/PROPOSED-SIDE-MISSION-AND-ASSET-PASS.md`.

Four optional additional character missions were proposed:

- **SM10 — Missed Calls:** Gohan disappearing / awkward technical clutch.
- **SM11 — Backup Plan:** Ron + Ice dependable two-man chemistry / Ice clutch.
- **SM12 — Old Route:** lower-stakes trio history around Davis.
- **SM13 — Three Seats:** Ron physically getting everybody together for something normal; Gohan's availability habit becomes brotherly comedy.

These are proposals, not mandatory campaign additions. Evaluate pacing/prerequisites before registering them.

---

# 11. Mission structure / lifecycle

The basic mission lifecycle is considered correct and should be preserved:

1. prerequisite / mission availability;
2. start trigger;
3. short intro/briefing;
4. gameplay stage(s);
5. optional state-preserving mid-mission scene;
6. final objective/extraction;
7. verify the actual completion condition;
8. moving aftermath/outro;
9. cleanup;
10. save completion / unlock next content.

Do not fire Mission Passed merely because the biggest explosion or combat encounter occurred.

If the story says all three leave together, verify all three actually reach/board the extraction.

If evidence must be delivered, pickup alone is not necessarily completion.

If a target must remain alive, enforce it.

---

# 12. Important continuity flags

Resolve these during implementation:

### M22 Cypress strike

The story needs a believable limitation explaining why Aegis can perform the precision strike on Cypress but cannot simply missile every later safehouse.

### M26 aircraft

Establish how the crew obtains/accesses/stores the aircraft used in M26, or use an aircraft already justified by the campaign.

### M33

Check that the gameplay trigger for acquiring Ramos's information does not contradict dialogue saying his medical rescue comes first.

### M60 Davis

Make the mission more personal and less generic action-movie dialogue. Use memories/locations/people from their old neighborhood. Ron should respect current residents rather than act as though the trio owns Davis after fifteen years away.

### M65 Vance

The confrontation should attack something specific about the trio rather than rely on generic "you can't beat the system" villain dialogue. Keep Ice's response restrained.

### M70 ending

Avoid a long group-therapy confession after the finale. The preferred direction is a quieter boat scene where Ron checks on Ice and Gohan, Gohan is already distracted by a device, the phone-answering habit becomes a joke, and they decide to get breakfast.

The emotional resolution should be shown through familiarity and behavior rather than each character explaining his entire arc.

---

# 13. Validation requirements

Do not assume static tests prove GTA behavior.

The repository's QA documentation explicitly notes that source/runtime stand-ins do not prove:

- live terrain;
- rendered animations;
- native stability;
- exact in-game collision/navigation.

Keep existing lint/build/story tests passing, but also live-test important scenes.

At minimum preserve/run the project's existing:

- mission lint;
- location validation;
- build with warnings as errors;
- story/runtime regression tests;
- in-game model/audio/switch/checkpoint/death/save/prerequisite checks.

For any cutscene change, test both:

- watching the scene completely;
- skipping it.

Both paths must arrive at the same valid gameplay state.

---

# 14. Recommended implementation order

Do not attempt to rewrite all 70 missions in one uncontrolled pass.

### Phase 1 — reconcile

- Read all proposal docs and current implementation.
- Produce a change matrix: proposal item → source files affected → risk → test.
- Identify any proposal already implemented or superseded.

### Phase 2 — cinematic foundation

- Implement/prototype the LSIA → Burro Heights prologue.
- Expand reusable cutscene blocking helpers.
- Build/test reusable interaction animations.

### Phase 3 — early campaign

- Fix M01 four-seat prototype if still needed.
- Apply M02–M16 dialogue cleanup and M05 conspiracy pacing.
- Preserve working mechanics.

### Phase 4 — relationship/dialogue pass

- Apply approved M17–M70 character/dialogue changes in controlled batches.
- Keep the Ron/Ice/Gohan dynamic rules above visible during review.

### Phase 5 — side missions

- Correct SM01–SM09 character framing.
- Evaluate SM10–SM13 before adding them to progression.

###