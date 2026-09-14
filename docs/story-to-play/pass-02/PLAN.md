# Pass 02 — Continuous Paleto and M49-M53

## Owner-locked M44-M48 operation contract

The player experience is one uninterrupted mission:

**Start once at M44 → underwater sabotage → helicopter insertion → vault/evidence recovery → demolition/pickup → boat escape → shore transfer → outer-cordon road escape → one Mission Passed after M48.**

Internal IDs M44-M48 may remain for code organization, dialogue and tests, but normal gameplay must not use ordinary Start/Finish boundaries between them.

### Prohibited internal behavior

- No intermediate Mission Passed/Chapter Complete.
- No return to free roam or map-marker hunt.
- No repeated briefing that restages/teleports the crew.
- No saved midpoint checkpoint/resume.
- No fresh deployment, health/ammunition reset, loan reset, vehicle recreation or payout between phases.
- No silent time/weather/wanted reset merely because an internal phase changed.
- Failure, abort, quit or reload restarts at M44. Legitimate pre-operation preparations remain earned.

### Required cross-phase continuity

Carry the same required people, evidence/cargo and operation-owned vehicles/assets until their role genuinely retires. Important dialogue moves into radio/travel/live interactions so story context survives without stopping the operation.

The existing MissionManager continuation table is not equivalent to this contract because ordinary mission completion tears down state and starts the next mission normally. Implement a parent operation/world pattern analogous to PortHeistOperation, with one attempt and one final commit.

## Internal phase transitions

- **M44→M45:** underwater result is verified; Ron's insertion begins while radio continues. No success screen.
- **M45→M46:** Ice secures access; Gohan physically reaches the command/vault area with the credential.
- **M46→M47:** evidence is physically held; retreat begins; Ron's extraction transition is already in motion.
- **M47→M48:** all required brothers/evidence actually board the extraction craft and clear the demolition area before shore travel begins.
- **M48 end:** crew/evidence/cargo reach the real southbound endpoint; only then complete the operation.

## M49 — Return to the Concrete

Story purpose: Chumash is the **second** barricade after M48's outer cordon. The turbine Granger and the brothers' previous preparation must matter.

Playable treatment:
- Approach together in the actual Granger from the north.
- Establish the spotlight/generator and drivable gap before asking for the breach.
- Ice disables the generator/covering threat from a usable lookout.
- Gohan delays/complicates dispatch without claiming every patrol forgets them.
- Ron drives the Granger through the verified lane.
- Ice physically descends/boards the same vehicle; the mission does not complete with him left on the hillside.
- Consequence: they are inside Los Santos, but emergency warrant copies still create danger; municipal records is next.

## M50 — The Redacted Vault

Story purpose: disrupt the authenticated municipal warrant feed, not erase every witness/cop/database copy.

Playable treatment:
- Use verified records-room/service geometry or an explicit exterior/service-terminal adaptation.
- Gohan handles access and the administrative action.
- Ice contains private security; recommended requirement is nonlethal where the story calls for contained guards.
- Ron maintains the actual pickup vehicle outside.
- Result language must be bounded: coordinated municipal feed is invalidated/quarantined; existing physical/local copies can remain.
- Ice/Gohan physically return to Ron's van before the mission ends.

## M51 — Blackout Protocol

Story purpose: prepare a timed corporate-district blackout for the later downtown offensive while avoiding civilian-critical circuits.

Treatment:
- Place/verify all required charges/relays at supported switchyard positions.
- Gohan handles failover logic; Ice/Guess perform physical placement/security roles.
- Current authored consequence says **charges ready**. Do not silently make M51 a permanent citywide blackout if later timing needs the outage.
- Any actual global/local lighting override must restore on abort/fail/reload.
- The exact later activation moment remains a decision to reconcile with tower missions.

## M52 — Judicial Strike

Story purpose: remove Harrison's legal/renewal role without pretending one assassination erases all paperwork.

Treatment:
- Identify Harrison before the shot.
- Give Ice a usable, mapped firing position and safe descent/exit.
- Gohan confirms identity/intelligence remotely or from a supported nearby role.
- Ron uses a real extraction vehicle with enough supported seats. The older superbike wording conflicts with a three-person extraction; choose the supported version before coding.
- Harrison's death triggers the underground-sweep consequence, not an instant clean legal slate.

## M53 — Subterranean Sweep

Story purpose: clear Aegis sweep teams from the sheltered Pillbox routes needed for the tower plan.

Treatment:
- Use accessible subway/maintenance geometry.
- Establish both approaches and the maintenance exit before contact.
- Gohan locates/reads the sweep route; Ice sets the primary ambush; Ron has a real supporting position rather than appearing after the fight.
- First contact triggers the second response/encirclement.
- Do not place mandatory enemies or objectives inside an inaccessible train car.
- Recover the route information if required by later planning, then physically exit/regroup.
- Consequence points to the temporary Pillbox observation room, not a permanent new home.

## Acceptance emphasis

For M49-M53, every mission must answer before control: why this location, why this brother, what result matters, and what transport/exit is already planned. End states must include every required brother and evidence/vehicle needed by the next job.