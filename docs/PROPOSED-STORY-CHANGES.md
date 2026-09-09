# Bloodlines — Proposed Story & Dialogue Changes

**Status:** Proposal only — not implemented  
**Purpose:** Capture agreed story/dialogue revisions for later implementation without changing current mission scripts, campaign data, or runtime behavior.

---

## 1. Opening Prologue Direction

### Agreed concept
Ron / Guess should be the player's entry point into the campaign.

The campaign opens with **Ron arriving at Los Santos International Airport** after returning to the city. The player takes control of Ron at or immediately outside the airport and **drives from LSIA to his apartment**.

This drive serves as:

- the player's first playable introduction to Ron;
- the first reintroduction to Los Santos;
- a low-pressure driving/tutorial segment before M01;
- a quiet character beat establishing that Ron has returned after a long absence;
- the narrative bridge into the job that becomes **M01 — Ghost in the Dockyard**.

### Structural intent
The airport-to-apartment drive should happen **before M01 begins**.

The prologue should not replace the existing M01 recognition sequence. Its job is to establish Ron and his return to Los Santos, then hand off naturally into the existing dockyard setup where Ron, Ice, and Gohan arrive on separate jobs and discover one another during the mission.

### Current preferred flow

1. Opening cinematic / Ron arrives at LSIA.
2. Player gains control of Ron.
3. Player drives Ron from the airport to his apartment.
4. Short apartment/setup beat.
5. Ron receives or commits to the Terminal Island job.
6. M01 begins using the current separate-job structure.

No final prologue dialogue is locked by this document. This section records the agreed direction only.

---

## 2. M01 Vehicle Requirement

The vehicle Ron steals in **M01 — Ghost in the Dockyard** must be a **four-seat vehicle**.

### Reason
That stolen vehicle is also the mission's getaway vehicle. Ron, Ice, and Gohan must all be able to ride in it together during the escape.

### Required story/implementation rule

- Do **not** use a two-seat T20 for the final M01 vehicle.
- The stolen prototype should remain a desirable/high-value car, but it must support at least:
  - Driver: Ron / Guess
  - Passenger: Ice
  - Rear passenger: Gohan
- The current story wording referring to a **four-door prototype** should be treated as the intended direction.

### Important
This proposal does **not** select the final GTA V vehicle model yet. The exact model can be chosen later based on gameplay fit, seat count, handling, visual identity, and mod reliability.

---

## 3. Early Mission Dialogue Tightening — General Rule

The current scripted missions are mechanically clear, but several gameplay lines read like debug/tutorial instructions rather than natural dialogue.

### Proposed dialogue rule

**Characters should describe the situation. The HUD should describe the controls.**

Spoken dialogue should generally avoid lines such as:

- "Switch to me."
- "Walk into the yellow marker."
- "Press E or D-pad Right."
- "Take the orange-marked vehicle."
- "No ability needed."
- exact control prompts that are already shown by the HUD/objective system.

### Preferred split

**Character dialogue:**
> "I'll take the depot. Gohan, stay ready for the truck."

**HUD/objective:**
> SWITCH TO ICE  
> ENTER THE DEPOT

This preserves mechanical clarity while making the characters sound like people inside the story rather than tutorial narrators.

This should be applied selectively during a future dialogue pass. It is not a request to remove useful objective messaging from the game.

---

## 4. M02 — Loose Strands

### Current story function
M02 is a strong direct continuation of M01:

- the dockyard exposure creates an immediate problem;
- Gohan discovers the Aegis van carrying the recording/upload;
- Ron drives;
- Gohan hacks from the vehicle;
- Ice handles the physical recovery once the van is stopped.

The mission should remain structurally intact.

### Proposed tightening
The main issue is repeated tactical information.

The current dialogue explains the same distance/upload mechanics multiple times. The future pass should let the HUD carry the exact range requirement while spoken dialogue carries urgency and personality.

### Proposed tone target
Instead of repeatedly stating the exact 35-metre rule, use lines closer in spirit to:

**GOHAN**  
"The van has the dock footage. Keep me close enough to punch into their signal and I can kill the upload."

During the pursuit:

**GOHAN**  
"I've got the connection. Three minutes before our faces hit their network."

**GUESS**  
"Black Rumpo. Satellite dome. I see it. Just keep doing whatever the hell you're doing back there."

### Keep
The emotional M02 outro should remain a priority, especially Ron's line about Ice still giving orders like the school bell is about to ring and Ice finally asking Ron for a safe place.

That scene helps establish that the reunion is not automatically comfortable.

---

## 5. M03 — Cypress Foundry

### Current story function
M03 is where the accidental reunion starts becoming a permanent partnership.

- M01 puts them together by accident.
- M02 forces them to survive the immediate consequences together.
- M03 gives them a shared base.

That progression is strong and should be preserved.

### Keep
Ron's aftermath line should remain:

> "Three keys to the shop. Keep yours this time. I got tired of being the only one checking the door."

This line communicates the history between the three men without over-explaining it.

### Proposed tightening
Gameplay dialogue in M03 currently contains explicit player instructions such as switching characters, entering colored markers, and taking a specific marked vehicle.

Those instructions should move to HUD/objective text during a future pass.

### Example conversion

**Current function:** Tell the player to switch to Ice and enter the depot marker.

**Proposed spoken line:**

**ICE**  
"Rail route's blocked. I'm moving on the depot. Gohan, stay ready for that truck."

**HUD:**

> SWITCH TO ICE  
> ENTER THE DEPOT

The mission mechanics do not need to change for this improvement.

---

## 6. M04 — Severed Wire

### Current story function
M04 has a strong place in the early conspiracy chain:

1. M01 — the trio is exposed.
2. M02 — they stop the upload and recover Aegis data.
3. M03 — they establish a base.
4. M04 — they follow the Aegis connection through Miller.
5. M05 — the trail leads to Mateo.

That structure should be preserved.

### Keep
Ron's intro challenge to Ice is valuable:

> "His car or him, we stop one. Tell me where this stops after that, Ice."

It shows that Ron has not simply accepted Ice as unquestioned leader after fifteen years apart.

### Proposed tightening
As with M03, remove spoken references to:

- yellow/orange markers;
- pressing E or D-pad Right;
- "switch to me" instructions;
- other explicit controller guidance.

Keep that guidance in the HUD/objective system.

---

## 7. M05 — Tidal Lock

### Current story function
M05 should remain the payoff to the first mystery arc: Mateo knows why three separate jobs converged on the same dockyard.

The crew needs him alive because he may be the first person who can explain the setup.

### Keep
Ice's line connecting the mission back to the reunion is important:

> "I missed my shot when I heard your voice. Don't make me regret it."

That keeps M01 emotionally relevant instead of treating the reunion as finished business.

### Proposed story tightening
The current conspiracy reveal happens too quickly.

At present, Mateo effectively reveals the Aegis/city-government conspiracy in one step, Gohan immediately expands it into a statewide conclusion, and Ice immediately declares war. The aftermath then partially walks that certainty back by saying Mateo's accusation is not proof yet.

The future revision should make Mateo provide:

- a disturbing allegation;
- proof that the three separate jobs were intentionally converged;
- evidence of official money or protection;
- a strong lead;
- **not** the entire conspiracy explanation.

### Proposed reveal shape
Example direction only:

**MATEO**  
"Aegis paid us to keep that dock hot. Three crews. Three contracts. They wanted all three of you there."

**GOHAN**  
"Why us?"

**MATEO**  
"I don't know. But whoever signed it has city money behind them. Police, port authority... all of it."

**GOHAN**  
"If he's telling the truth, somebody put us in that dock on purpose."

**ICE**  
"Then we find out who."

### Intent
The player should leave M05 with a **larger mystery**, not complete understanding of the conspiracy.

The larger Aegis/state-defense-contract reveal can land later once the crew has earned enough evidence for it to feel credible.

---

## 8. Character Voice Priorities for the Early Game

A future rewrite pass should preserve and strengthen the differences between the three leads.

### Ron / Guess

- Most natural point-of-view character for the opening.
- Uses humor under pressure.
- Resents being left behind / losing contact.
- Does not automatically defer to Ice.
- His driving competence should come through in behavior and confidence rather than repeated exposition.

### Ice

- Controlled, tactical, restrained.
- Naturally falls into leadership language.
- Should not sound like a HUD tutorial.
- Carries guilt about leaving and difficulty returning home.

### Gohan

- Analytical and technical without speaking like documentation.
- Should explain what matters, not every mechanic.
- Holds resentment about the fifteen-year separation.
- Often becomes the person who distinguishes evidence from assumption.

---

## 9. Non-Goals of This Proposal

This document does **not** authorize or request any immediate implementation changes.

Specifically, this proposal does not yet change:

- C# mission scripts;
- `data/dialogue.tsv`;
- `data/dialogue_edits.json`;
- `data/scenes.tsv`;
- mission triggers;
- objective logic;
- campaign order;
- coordinates;
- voice files;
- the original bible PDFs;
- the final M01 prototype vehicle model.

It is a planning document for a later implementation pass.

---

## 10. Suggested Future Implementation Order

When these changes are approved for implementation, the recommended order is:

1. Lock the airport-to-apartment prologue structure.
2. Select the final four-seat M01 prototype/getaway vehicle.
3. Finalize prologue dialogue and transitions into M01.
4. Rewrite M02–M04 gameplay dialogue to remove controller/HUD language.
5. Tighten the M05 conspiracy reveal.
6. Regenerate authored dialogue data from the approved source-of-record path.
7. Re-run story/parser/regression checks.
8. Live-test the revised M01–M05 sequence in Story Mode.

---

## 11. Current Decision Summary

The working direction is:

- **Keep:** Ron begins the campaign at the airport.
- **Keep:** Player drives Ron from LSIA to his apartment before M01.
- **Keep:** Existing M01 separate-job/reunion structure.
- **Change later:** M01 stolen prototype must be four-seat; no two-seat T20.
- **Keep:** M02–M04 mission story structure.
- **Tighten later:** Remove explicit control/marker language from spoken dialogue.
- **Keep:** strong relationship lines that expose the fifteen-year history.
- **Tighten later:** M05 should reveal a conspiracy lead, not the full conspiracy.

This file is intentionally a **proposal document only** so the current playable build remains unchanged until an implementation pass is explicitly approved.
