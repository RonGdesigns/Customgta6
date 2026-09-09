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

This should **not** be framed as Ron resisting Ice as a superior or refusing to accept Ice as leader. The trio does not operate with Ice above Ron or Gohan.

The better interpretation is that Ron is challenging the plan, demanding transparency, and making sure one member of the trio is not making decisions that affect all three without the others understanding the consequences.

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

## 8. Trio Dynamic and Leadership Rule

This is a core story rule and should guide all future dialogue revisions.

### Equal-status trio
Ron, Ice, and Gohan are a **dynamic trio**. None of the three is permanently above the others, and no one should be written as the unquestioned boss of the group.

- Ron is not subordinate to Ice.
- Gohan is not subordinate to Ron or Ice.
- Ice's tactical personality does not automatically make him the leader.
- Ron's role as the campaign's opening point-of-view character does not automatically make him the permanent leader either.

### Ron and leadership
Ron may ultimately become the character who most clearly fills a leadership role, but that should be **earned and established later in the story**, not assumed in the early missions.

Even when that leadership becomes more visible, the trio should still function as equals. Leadership should mean responsibility, initiative, and the ability to bring the group together—not rank or authority over the other two.

### Mission-specific leadership
Different members should naturally take point depending on the job:

- Ron leads when driving, extraction, route choice, mobility, or improvisation is central.
- Ice takes tactical point when combat planning, overwatch, assault, or defensive positioning is central.
- Gohan takes technical point when intelligence, hacking, surveillance, electronics, or evidence analysis is central.

This is **situational leadership**, not hierarchy.

### Dialogue implication
Future rewrites should avoid language that accidentally establishes a chain of command unless the scene is intentionally exploring that tension.

The better pattern is:

- one character proposes the plan;
- the others question, refine, or approve it;
- whoever has the relevant expertise takes point;
- major decisions belong to all three.

---

## 9. Character Voice Priorities for the Early Game

A future rewrite pass should preserve and strengthen the differences between the three leads while respecting the equal-status trio rule above.

### Ron / Guess

- Most natural point-of-view character for the opening.
- Uses humor under pressure.
- Resents being left behind / losing contact.
- Strong instinct for movement, escape, vehicles, and improvisation.
- Can naturally pull the trio together without being written as a commanding superior.
- Any later leadership role should grow out of trust and responsibility rather than rank.
- His driving competence should come through in behavior and confidence rather than repeated exposition.

### Ice

- Controlled, tactical, restrained.
- Naturally speaks in decisive tactical language because that is his personality and expertise.
- This should not be mistaken for formal authority over Ron or Gohan.
- Should not sound like a HUD tutorial.
- Carries guilt about leaving and difficulty returning home.

### Gohan

- Analytical and technical without speaking like documentation.
- Should explain what matters, not every mechanic.
- Holds resentment about the fifteen-year separation.
- Often becomes the person who distinguishes evidence from assumption.
- Should have equal weight in major decisions, especially when his information changes the crew's understanding of a situation.

---

## 10. Batch 2 Review — M06 through M16

This section records the next review pass. The mission structures are generally strong; most proposed changes concern spoken dialogue and preserving the trio's relationship development.

### M06 — Clean Sweep

**Assessment:** Keep mission structure; substantial gameplay-dialogue cleanup.

The mission works as the immediate cleanup after Mateo: the mobile upload was only one copy, so the crew targets the Vespucci backup.

Current spoken lines include explicit HUD/controller language such as:

- yellow sally-port marker;
- switch to Ice;
- no ability needed;
- red-marked SWAT;
- orange getaway marker.

These should move to HUD/objective text.

**Proposed tone direction:**

**GOHAN**  
"Feeder's dead. Ice, entrance is yours."

**ICE**  
"Moving in. Gohan, get to those racks. I'll hold the alley."

**ICE**  
"SWAT's stacking up outside. Keep working, Gohan."

**GUESS**  
"Granger's ready. Finish the burn and get your asses back here."

**Protect:** The aftermath where Ice credits Gohan for seeing something he could not. It develops mutual trust without establishing hierarchy.

---

### M07 — Wiretap Waltz

**Assessment:** Strong; minor polish only.

The mission correctly uses the Aegis tap to verify Mateo's story rather than treating his accusation as established fact.

**Protect:**

> "I used to trust a uniform to tell me whose side I was on. Give me something I can verify."

This develops Ice while advancing the plot.

**Tighten:** Remove phrases such as "marked antenna platform" and "your pickup is marked."

**Example direction:**

**GOHAN**  
"Get me that receiver on the antenna and I'll pull whatever Aegis is moving."

**GUESS**  
"I'm underneath you, Ice. Find a safe way down and I'll be there."

---

### M08 — Supply & Sever

**Assessment:** Strong; very light polish.

**Protect:**

> "Those turbines are the difference between hauling armor and dying inside it."

and

> "Take the time you need to secure them. I won't call you slow while I'm standing behind your armor."

These lines explain stakes through character interaction.

Remove "marked" from sentry instructions where the HUD already identifies them.

**Protect:** The outro where Ron notices Colonel Vance shares Ice's surname. The scene creates a useful trust/transparency beat without implying anyone outranks anyone else.

---

### M09 — Rolling Thunder

**Assessment:** Good story; moderate mechanical-language cleanup.

**Protect:**

> "Good. Tell me the part that might change your judgment before I put the Frogger over that convoy."

This is not Ron challenging a superior. It is Ron requiring full information from an equal before committing himself and the crew to a dangerous plan.

**Tighten examples:**

**RON**  
"I got the convoy. Ice, rear escort is yours."

**RON**  
"Escort's stopped. Get that IFF before somebody notices their truck isn't moving."

---

### M10 — Open Throttle

**Assessment:** Strong character material, but substantial gameplay-dialogue tightening is needed.

This mission should showcase Ron at his best as a driver and improviser.

Move exact mechanics such as minimum speed, weapon instructions, and tunnel markers into the HUD.

**Example direction:**

**RON**  
"Don't let me get boxed in. This truck stops, we're done."

**ICE**  
"Bikes coming up both sides. I've got right—take left!"

**RON**  
"Back in the truck. Tunnel's our way out."

**Protect:**

> **ICE:** "You stopped joking on that bridge. I should've noticed how close we were before you had to tell me."
>
> **RON:** "I joke when I'm scared too. Learn the difference."

This gives Ron emotional depth and shows Ice learning how to read him.

---

### M11 — Ironclad Dyno

**Assessment:** Protect. One of the strongest quieter missions in the early campaign.

The mission gives the trio room to reconnect through shared history rather than gunfire.

**Protect:** The Davis car-wash memory, Ice teasing Ron about the alternator, and Gohan interrupting the nostalgia with the Berth 44 intelligence.

**Protect:**

> "We survey it before we dream about it. And if we get rich, nobody buys the right to vanish without a goodbye."

The current clarification that the $3B figure is a manifest claim rather than immediately spendable money should remain unless later story planning changes it.

---

### M12 — Black Tide Recon

**Assessment:** Strong; minor cleanup.

The mission has good cause-and-effect: the manifest identifies the hold, then Gohan's recon determines whether the planned extraction can physically work.

Move "sonar marker" and "marked depth" language to HUD/objective text where possible.

**Protect:**

> "You're quiet when you're worried. Used to mean an exam. Now it means I should check my fuel twice."

This is a strong example of showing that Ron knows Gohan from before the campaign.

---

### M13 — Smuggler's Cut

**Assessment:** Strong; preserve most of it.

**Protect:**

> "A fuel worker isn't Aegis command just because he's working their slipway."

and

> "We're making an exit, not collecting explosions. Keep me honest about that."

This establishes Gohan as a moral/analytical check and shows Ice listening to him. It should be framed as mutual accountability within the trio, not one character controlling another.

---

### M14 — Airspace Blackout

**Assessment:** Strong relationship writing; light mechanical cleanup.

**Protect:**

> "If the approach closes, call it off. I'd rather change the heist than put your name on a memorial."

and Ron's later response that hearing he could turn back made the run easier.

This shows trust and concern between equals.

Move "marked aircraft," exact altitude requirements, and "marked airfield" language into HUD/objective text.

---

### M15 — Crawlspace

**Assessment:** Protect. Strong trio-development mission.

**Protect:**

> "Talk me through the tap. I don't need every circuit. I need to know when you're committed and can't move."

**Protect:** Gohan placing harbor access on all three devices so the plan survives even if he goes down.

**Protect:**

> "You aren't a spare part, Gohan."

This supports the central rule that all three are equally important to the crew.

---

### M16 — The Heavy Lift

**Assessment:** Good; minor tightening.

The mission pays off the M09 IFF transponder and has strong continuity.

**Protect:**

> "You leave the fight then, even if there's somebody left to be angry at."

This is not Ron ordering a subordinate or superior. It is Ron checking Ice as an equal when Ice's tactical focus could keep him in the fight too long.

Move exact altitude numbers and delivery-marker language into the HUD.

---

## 11. Batch 2 Overall Pattern

The M06–M16 story spine is stronger than the raw gameplay dialogue sometimes makes it appear.

A useful emotional progression is already present:

- M06 — Ice gains trust in Gohan's judgment.
- M08–M09 — Ron pushes for transparency before dangerous commitments.
- M10 — Ice learns that Ron's humor can mask fear.
- M11 — the three reconnect through shared history.
- M12 — Ron recognizes Gohan's old habits.
- M13 — Gohan challenges unnecessary escalation and Ice listens.
- M14 — Ice shows Ron that aborting a bad run is acceptable.
- M15 — Gohan distributes critical knowledge across the trio.
- M16 — Ron pulls Ice back from overcommitting to a fight.

The important interpretation is that these are **three equals learning how to trust one another again**, not a leader giving orders to two subordinates.

The recurring weakness is mostly presentational:

**HUD = mechanics.**  
**Dialogue = people.**

---

## 12. Non-Goals of This Proposal

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

## 13. Suggested Future Implementation Order

When these changes are approved for implementation, the recommended order is:

1. Lock the airport-to-apartment prologue structure.
2. Select the final four-seat M01 prototype/getaway vehicle.
3. Finalize prologue dialogue and transitions into M01.
4. Rewrite M02–M06 gameplay dialogue to remove controller/HUD language.
5. Tighten the M05 conspiracy reveal.
6. Apply the M07–M16 dialogue-polish pass according to the Batch 2 notes.
7. Audit all revised lines against the equal-status trio / situational-leadership rule.
8. Regenerate authored dialogue data from the approved source-of-record path.
9. Re-run story/parser/regression checks.
10. Live-test the revised early-campaign sequence in Story Mode.

---

## 14. Current Decision Summary

The working direction is:

- **Keep:** Ron begins the campaign at the airport.
- **Keep:** Player drives Ron from LSIA to his apartment before M01.
- **Keep:** Existing M01 separate-job/reunion structure.
- **Change later:** M01 stolen prototype must be four-seat; no two-seat T20.
- **Keep:** M02–M16 mission story structure unless a later review identifies a specific problem.
- **Tighten later:** Remove explicit control/marker language from spoken dialogue.
- **Keep:** strong relationship lines that expose the fifteen-year history.
- **Tighten later:** M05 should reveal a conspiracy lead, not the full conspiracy.
- **Core rule:** Ron, Ice, and Gohan are a dynamic trio of equals. No permanent chain of command should be implied.
- **Leadership:** Ron may grow into the clearest leadership role later, but it should be earned and should never make Ice or Gohan lesser members of the trio.
- **Situational leadership:** whoever has the relevant expertise should naturally take point for that part of the job.

This file is intentionally a **proposal document only** so the current playable build remains unchanged until an implementation pass is explicitly approved.
