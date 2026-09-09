# GTA V: Bloodlines — Campaign Implementation Audit

**Status:** Read-only audit findings / implementation guidance  
**Scope reviewed:** campaign records M01–M70, SM01–SM09, currently scripted missions, mission lifecycle, save/progression systems, cutscene system, crew systems, homes/free-roam, rewards and selected objective implementations.  
**Audit snapshot:** `aa245bf3` on `claude/gta-v-custom-version-477edi`.

This document records the campaign-wide audit performed after the story/dialogue and asset-feasibility passes. It does **not** mean every issue has been reproduced in live GTA V Enhanced. Source-level findings are identified as such; anything depending on terrain, collision, animation playback, vehicle physics or live native behavior still requires in-game verification.

---

# 1. Executive summary

Bloodlines has a strong campaign foundation, enough mission variety on paper, a coherent central trio relationship, and several real setup/payoff chains already implemented.

The highest-priority risks are no longer a shortage of ideas. They are:

1. persistent campaign state being changed before a mission truly succeeds;
2. replay/retry behavior mutating campaign rewards or historical state;
3. lack of dependable checkpoint reconstruction for long missions;
4. weak handoff continuity between chapters of the same operation;
5. mission cast ownership and generic cutscene staging not yet matching the desired moving cinematic standard;
6. proposal documents and current code sometimes describing different versions of the same mission;
7. later campaign pacing risking too many consecutive preparation jobs;
8. Gohan's gameplay sometimes being represented by dialogue/timers rather than distinct playable contribution.

Do **not** expand the campaign blindly before reconciling these systems.

---

# 2. Current implementation scope

At the audited snapshot, the campaign data contains the full mission spine, but the playable C# implementation does not yet cover the full campaign.

Treat these as separate concepts:

- **authored campaign mission:** exists in campaign/dialogue/scene data;
- **scripted mission:** has actual runtime mission code;
- **live-verified mission:** has been tested successfully in GTA V Enhanced with terrain, collision, actors, animation and restart behavior.

The audit found the implemented gameplay concentrated in the early campaign and first solo missions. Later mission findings therefore concern their authored design and continuity unless runtime code exists.

Agents must not describe M28–M70 as already working simply because dialogue or feasibility entries exist.

---

# 3. Mission lifecycle audit

## 3.1 Basic framework is usable

The existing mission manager already provides a sensible foundation:

1. prerequisite check;
2. intro scene when available;
3. mission setup;
4. stage/objective updates;
5. pass/fail handling;
6. campaign completion save;
7. aftermath/outro.

Keep the architecture unless a specific implementation problem requires extension.

## 3.2 Actual completion order needs an explicit policy

The reviewed runtime effectively performs:

> final objective -> mission Pass / mission cleanup -> queued gameplay dialogue finishes -> campaign completion is saved / Mission Passed shown -> aftermath scene

The proposal documents previously described aftermath before cleanup/save. Neither order is automatically wrong, but the implementation must deliberately decide:

- which actors/vehicles/props survive into the aftermath;
- whether completion is committed before or after an essential story scene;
- what happens if the player skips the aftermath;
- what happens if the mod unloads during the aftermath.

Do not let cleanup delete something the outro expects to use.

## 3.3 Continuous operations need a chapter-handoff system

Some mission chains should not feel like independent jobs separated by a menu reset.

Priority chains:

- M19–M22 — Port Heist;
- M44–M47 — offshore-rig operation;
- M63–M66 — tower assault / evacuation;
- M68–M70 — finale escape.

For these, create an explicit handoff record that can preserve or intentionally reconstruct:

- active crew member;
- all three crew positions;
- occupied vehicle and seats;
- essential vehicles;
- cargo/props;
- damage state where relevant;
- world location;
- time/weather continuity;
- next objective/start point.

If actual entity persistence is unreliable, conceal a reconstruction with a fade/camera transition, but preserve what the player believes happened.

---

# 4. Concrete progression/state issues

## 4.1 M24 rewards before delivery completion — HIGH PRIORITY

Current M24 behavior awards approximately:

- +5 recovered Alamo gold tons;
- +$200,000;

when the dredging stage finishes. The player still has to deliver the recovery truck to the bunker afterward.

If the truck is destroyed after the reward is granted, the mission can fail while the persistent state has already changed.

### Required correction

Permanent campaign rewards should either:

- commit once at actual successful mission completion; or
- use an explicit partial-success state that is intentionally preserved.

Do not let retrying the mission add the same five tons repeatedly.

## 4.2 M22/replay can mutate historical state — HIGH PRIORITY

M22 changes persistent gold/cash/safehouse state during mission stages. Replaying a completed story mission must not accidentally reset a later gold-recovery value, duplicate money, or reapply one-time historical changes.

### Required correction

Separate:

- first-time canonical completion;
- mission replay;
- dev/testing force-pass;
- checkpoint retry.

Story-state mutations should normally be idempotent or first-completion-only.

## 4.3 End-of-available-content falls back toward M01 — HIGH PRIORITY

`CampaignState.NextPlayable()` has a final fallback to the first playable mission. When all currently scripted missions are complete, this can point the campaign back toward M01 rather than clearly reporting that the current build has no further playable story mission.

### Required correction

Represent these states separately:

- next available story mission;
- optional side content available;
- all currently implemented story content complete;
- replay mode.

Do not use M01 as the generic fallback.

## 4.4 Retry path needs to retry the failed mission explicitly

The mission manager has a `Retry()` concept, but normal mission-start input can select the next/nearby playable mission rather than necessarily the mission that just failed.

### Required correction

After failure, expose a direct retry of `LastAttempted` before returning to ordinary mission selection.

---

# 5. Checkpoints and death recovery

## 5.1 Checkpoint records are not full checkpoint restoration

Current missions default to no checkpoint restoration because reconstructing a mission requires more than restoring a stage index.

A valid checkpoint restore must recreate:

- correct active hero;
- health/armor/ammo policy;
- required companion state;
- required vehicles and seats;
- required props/cargo;
- surviving/dead targets;
- completed interaction state;
- mission timer state;
- dialogue state;
- relevant wanted/world state;
- objective markers.

Do not enable restore globally until missions have explicit reconstruction logic.

## 5.2 Prioritize checkpoint boundaries, not every stage

For long missions, first implement reliable restore points at meaningful boundaries:

- after the approach;
- before a large combat encounter;
- after a major technical interaction;
- before a difficult extraction.

This is more valuable than attempting perfect restoration after every minor objective.

## 5.3 Companion death policy must be intentional

The free-roam crew system can respawn fallen companions depending on configuration. Mission-specific failure rules must override or suspend that behavior where the story requires a teammate to remain alive.

Each mission should declare whether a companion is:

- protected / effectively cannot die;
- recoverable/incapacitated;
- mission-fail critical.

Do not let generic companion respawn accidentally bypass a mission failure condition.

---

# 6. Required vehicle/actor failure audit

Mission-critical assets must be protected for the entire period in which the story requires them, not only during one local objective.

For each mission, audit:

- what if the required vehicle is destroyed before the player is told to enter it?
- what if the target NPC dies one stage early?
- what if the player abandons the required vehicle?
- what if an escort NPC gets stuck or falls out of the world?
- what if the player switches characters at the wrong moment?

Every required asset needs one of:

- mission failure;
- safe respawn/reconstruction;
- alternate route;
- explicit invulnerability until the appropriate stage.

Avoid softlocks where the objective waits forever on a destroyed vehicle or missing NPC.

---

# 7. Port Heist continuity finding

M20 currently creates a Cargobob and attached bullion container. The mission cleanup detaches/removes tracked props.

M21 then starts with its own Cargobob escort setup. The story treats this as the same bullion lift, but the runtime does not maintain the same container entity through the chapter boundary.

M22 later creates another lift/container state for the Alamo drop.

### Required production decision

Either:

1. preserve the same aircraft/container through M20 -> M21 -> M22; or
2. intentionally reconstruct the same apparent state during a hidden transition.

Do not allow M21 to visually escort a "bullion lift" that has no visible bullion container if the camera/player can reasonably notice it.

This chain should become the first prototype for multi-mission operation handoff.

---

# 8. Cutscene-system audit

## 8.1 Current generic cutscenes do not yet satisfy the moving-scene requirement

The existing director can frame speakers and manage scene cameras, but generic scenes still tend to freeze actors/vehicles while dialogue is delivered.

Delivery text such as "walking into the shop" does not automatically cause the character to walk.

### Required extension

Build reusable scene actions with completion signals:

- `WalkTo`;
- `RunTo`;
- `FaceEntity` / `LookAt`;
- `EnterVehicle` / `ExitVehicle`;
- `UsePhone`;
- `UseClipboard`;
- `UseLaptopOrPanel`;
- `PlantOrAttach`;
- `InspectVehicle`;
- `TakeCover` / `AimAt`;
- `OpenDoorOrTrunk`;
- `CameraTrack`;
- `CameraOrbit`;
- `DoorElevatorTransition`;
- `LadderTransition`;
- `WaitForTaskCompletion`;
- `HandoffToGameplay`.

A scene should be event-driven where possible, not just line-duration-driven.

## 8.2 Mission startup currently creates a cast-ownership problem

The normal mission-start path can stand down the free-roam crew before the intro. If the intro expects all three physical actors, the scene system may no longer have them.

### Required correction

Choose one consistent ownership flow:

**Option A:** mission creates/stages its actors before the intro;

or

**Option B:** cutscene layer owns temporary actor staging and hands those exact actors/state into gameplay;

or

**Option C:** preserve free-roam crew into the intro and transition them into mission-scripted state.

Do not use radio framing merely because the actors were deleted immediately before the scene.

## 8.3 `MissionInteraction` is presentation-light

The current interaction objective is essentially:

- go to marker;
- press context;
- remain in radius;
- wait for timer.

Calling the action "welding," "hacking," "splicing," or "loading" does not visually perform that action.

### Required correction

Create reusable interaction presentation wrappers that combine:

- position/orientation;
- tested stock animation/task;
- optional prop in hand;
- optional particles/sound;
- interruption/cancel behavior;
- timer/progress;
- cleanup.

If an exact animation is unavailable, use a generic believable interaction plus camera framing.

---

# 9. Prologue correction after deeper audit

The approved creative direction remains:

> Ron arrives at LSIA, physically enters a vehicle, and the player drives him to his apartment/home before M01.

Earlier planning substituted the Burro Heights chop-shop exterior because the home system was assumed to be exterior-only.

The newer implementation includes apartment entry infrastructure with interior/collision readiness checks.

### Agent instruction

Do not automatically discard the original airport-to-apartment idea.

Reconcile:

- the user's desired Ron apartment destination;
- the currently implemented starter-apartment system;
- Ron's Burro Heights chop-shop/home references;
- whichever interior actually proves stable in GTA V Enhanced.

Use Burro Heights exterior only if it is the best verified production choice—not because an older proposal mistakenly assumed apartments were unavailable.

---

# 10. Pacing and mission-variety audit

The campaign contains good mission-type variety on paper. The larger pacing risk is **too many preparation jobs**, not simply too many shootouts.

## M01–M06

Strong reunion/consequence chain.

Watch for repeated evidence-cleanup objectives.

## M07–M18

Good equipment progression, with M11 and M17 serving as valuable quieter beats.

Every acquisition should visibly matter later.

## M19–M22

Strong four-part heist concept.

Primary issue is operation continuity between chapters.

## M23–M27

Good environmental reset and mission variety.

Avoid making Act II feel like all Act I progress was erased.

## M28–M43

Largest pacing concern.

This is a long buildup to the offshore-rig operation. Keep individual missions only when each adds at least one of:

- a new usable capability;
- a meaningful character relationship beat;
- a new enemy complication;
- a major clue;
- a visible change to the plan;
- a distinct gameplay form.

If two consecutive missions merely collect another item for the same future operation, consider combining or reframing them.

## M44–M49

Strong operational payoff and return south.

Make the rig heist mechanically distinct from the Port Heist.

## M50–M60

Evidence/publication/Davis is strong, but avoid repeatedly introducing "one more backup" or "one more network" after the player believes the evidence problem is solved.

M60 should be a major emotional peak.

## M61–M70

Finale buildup is strong, but avoid finale fatigue.

Major peaks should be deliberate:

- Davis aftermath / final escalation;
- Vance confrontation;
- failed airport escape;
- Ron's final clutch;
- quiet brothers' aftermath.

Do not make every mission sound like the campaign is ending.

---

# 11. Character gameplay balance audit

## Ron

Ron naturally owns driving, extraction, improvisation and many clutch moments.

Do not confuse "most driving time" with "most meaningful clutch moments." Give him decisions where he changes the plan when it fails.

## Ice

Protect meaningful tactical accomplishments and confident combat calls.

Ice taking tactical point does not violate the equal-trio rule.

## Gohan

Biggest gameplay-balance concern.

Several technical objectives are implemented as proximity/timer interactions. Dialogue may make Gohan sound crucial while the player experience is still "stand here for X seconds."

### Required direction

Build a small set of reusable Gohan-specific decision mechanics rather than dozens of bespoke minigames. Examples:

- choose which security system to disable first;
- choose between camera blackout / door unlock / weapon shutdown;
- trace a moving signal while Ron drives;
- redirect enemy information toward a decoy;
- preserve one data source while destroying another;
- complete a technical task under an interruption that forces a switch.

His awkward clutch moments should come from strange/over-technical solutions that are legitimately effective—not incompetence.

### Balance measurement

During playtesting record:

- active-character minutes;
- forced switches;
- optional switches;
- mission-winning actions per character;
- ability uses;
- character-specific mechanics;
- major clutch beats.

Do not force equal thirds; use data to prevent one character from becoming functionally passive.

---

# 12. Reward/progression audit

## Strong examples

Existing persistent equipment/fleet upgrades such as the Granger turbine and reinforced Kraken demonstrate the right pattern: an earlier mission creates a later gameplay effect.

## SM03 racing transmission

The save flag exists, but the reviewed fleet-upgrade logic did not demonstrate a corresponding runtime effect.

### Required action

Trace the flag. If nothing consumes it, either:

- implement the gameplay benefit;
- change the reward description;
- remove the unused flag.

## SM01 ammunition

The story says the AP ammunition matters. Decide whether this is:

- an actual persistent ammo/armor-penetration benefit;
- a weapon-locker restock unlock;
- merely narrative supply.

Do not advertise a mechanical reward that does not exist.

## Mission-granted weapons

The weapon-progression system captures weapons the crew owns. Temporary mission-issued weapons may therefore become permanent unless explicitly excluded.

### Required policy

Classify mission equipment as:

- permanent owned weapon;
- temporary loan;
- character-standard loadout;
- consumable mission equipment.

Reward milestones must not be invalidated because the same weapon was automatically captured earlier.

---

# 13. Economy/accounting audit

The story uses multiple forms of wealth:

- Port Heist bullion;
- gold left/recovered from the Alamo;
- campaign cash-on-hand;
- bearer bonds / rig loot;
- offshore escrow / electronic funds.

Create one explicit campaign ledger that explains how they relate.

At minimum track:

- total physical bullion taken;
- physical bullion currently hidden;
- physical bullion recovered/spent;
- liquid operating cash;
- bearer bonds/value not yet converted;
- electronic/offshore balance.

Do not let a mission simultaneously describe "thirty tons of gold," "$500 million," and separate escrow money in ways that make the player unsure whether these are the same score or different assets.

---

# 14. Story-continuity findings

## M39 communications language

Authored material alternates between:

- cutting the rig off from its normal cable and forcing a slower/noisier backup;
- saying the rig's external communications are completely flatlined.

Choose one capability and make enemy behavior follow it.

If the rig still has backup communications, later preparation time is more believable.

## M51 blackout timing

One version says the blackout is detonated during M51; another implies charges are staged for later.

Choose one:

- actual blackout now;
- short test outage;
- charges planted for activation later.

The later tower sequence must respect the chosen timing.

## M60 civilian protection

M60 correctly frames residents as people to protect, but its large explosive events must not contradict that premise.

If using an underground gas-main explosion or similarly destructive event, establish evacuation, controlled placement, or another safety justification.

## Evidence chain

Keep the intended escalation:

- M05 = allegation / first lead;
- following missions = verification and widening evidence;
- M46 = hard proof;
- M59 = broad publication;
- later records = prevent Aegis from denying/containing the story.

Each evidence mission should visibly change the situation rather than merely produce another file.

---

# 15. World/free-roam continuity audit

The free-roam systems are more developed than older proposal documents implied.

Existing foundations include:

- apartments/home access;
- home interactions;
- companion travel/life behavior;
- dispatch/messages;
- persistent fleet/weapon state.

Build on these rather than creating a competing system.

## Pre-reunion gate

The generic crew-deployment system should not allow a normal story player to freely deploy Ron, Ice and Gohan together before M01's accidental reunion.

Developer/sandbox access can remain available separately.

## Cypress destruction

Changing a safehouse flag and playing explosions does not automatically make the world visibly remain destroyed.

Decide the post-M22 presentation:

- persistent smoke/damage props;
- blocked home access and a deliberately limited revisit view;
- a stock damaged-area substitute;
- another reliable visual state.

The player should not be told the shop is gone and then casually find it unchanged.

## Last-known location

Saving a last-known position is not enough unless startup/deployment actually consumes it reliably. Verify the complete resume path before describing the feature as working.

---

# 16. Villain/supporting-character audit

The trio has received much deeper development than the antagonists/support cast.

Create a presence map for:

- Vance;
- Ramos;
- Harrison;
- Bradley;
- Mateo;
- Sterling;
- KJ;
- any recurring Aegis/cartel contacts.

For each, record:

1. first mention;
2. first appearance;
3. first action that materially affects the player;
4. mid-campaign reminder/consequence;
5. final payoff.

Vance in particular should affect the trio before the final confrontation through decisions and consequences, not only through exposition about him.

Ramos should exist as a person after being rescued, not just a code-delivery device.

Do not turn every supporting character into a major arc. KJ can remain a smaller recurring contact.

---

# 17. Document/source reconciliation issues

The project now contains old bible language, proposal documents, current authored dialogue, and implemented mission code that do not always match.

Examples identified during audit include:

- M01 already uses a four-seat `schafter3` prototype in current code; an old T20 change instruction may already be resolved.
- M27 current implementation uses a passenger-seat ledger interaction rather than an impossible walkable jet-cabin fight.
- M29 newer authored dialogue describes a depot/tanker transfer while an older asset-planning version described a moving-train stunt.
- current apartment support is more advanced than older exterior-only home descriptions.
- some speed/route/mechanical details changed between the bible and runtime code.

### Required change register

Create a table with:

| Mission/system | Current implementation | Current authored data | Approved intended change | Old proposal/bible conflict | Live verified? |
| --- | --- | --- | --- | --- | --- |

The agent should update this as implementation progresses.

### Source priority

Use this priority unless the user explicitly says otherwise:

1. latest explicit user-approved creative decision;
2. current working implementation where it does not conflict with that decision;
3. latest authored mission/dialogue data;
4. proposal documents;
5. older bible/PDF descriptions.

Do not reintroduce a superseded mechanic because an old planning document sounds more cinematic.

---

# 18. Asset/cutscene verification standard

The stock-first campaign goal remains valid, but the asset audit is a production plan, not proof.

Before declaring a scene "verified," record:

- exact stock location/interior candidate;
- loading/IPL method if required;
- entry/exit points;
- collision test result;
- AI navigation test result;
- combat viability;
- exact stock actor task/animation used;
- prop model/attachment if required;
- camera positions/path;
- skip result;
- mission restart result;
- fallback substitute.

Hard set pieces requiring prototype proof before final writing lock:

- prologue LSIA -> apartment;
- M27 aircraft transfer;
- M42 submarine airdrop;
- M47 rig-collapse illusion;
- M55 multi-building switches;
- M62 train/boat/helicopter convergence;
- M70 grounded-Titan seawall escape.

Do not write final scene descriptions around an interior or animation that has not been tested.

---

# 19. Recommended implementation priority

## Priority 1 — reconcile documents/current code

- build the change register;
- mark already-resolved changes such as M01's four-seat car;
- resolve contradictory old/current mission descriptions;
- finish the agent handoff documentation.

## Priority 2 — protect persistent campaign state

- move/guard mission rewards;
- make story mutations replay-safe;
- fix end-of-current-content behavior;
- ensure retry targets the failed mission.

## Priority 3 — failure/checkpoint reliability

- audit required vehicles/actors;
- add mission-specific fail/alternate behavior;
- implement checkpoint reconstruction on the longest/difficult missions first.

## Priority 4 — chapter handoff

Prototype M19 -> M20 -> M21 -> M22 as one coherent operation with visible cargo/crew continuity.

Reuse the solution for later multi-part operations.

## Priority 5 — cinematic foundation

Build the LSIA prologue as the first proof of:

- moving actors;
- vehicle entry;
- camera choreography;
- state handoff to gameplay;
- apartment/home transition;
- skippable scene state equivalence.

Then build reusable action/interaction wrappers.

## Priority 6 — implement approved dialogue/relationship changes

Apply the existing proposal passes only after confirming which current source is authoritative for each mission.

## Priority 7 — pacing and character gameplay

- strengthen distinct payoff of long preparation chains;
- give Gohan more meaningful playable technical decisions;
- preserve Ron/Ice/Gohan situational expertise;
- add optional character missions only if pacing benefits.

## Priority 8 — live asset/set-piece verification

Verify interiors, animations and hard cinematic tricks in GTA V Enhanced before locking later mission implementation.

---

# 20. Minimum acceptance tests before large expansion

Before adding a large new block of missions, prove at least these cases:

1. **M24 reward rollback/commit:** fail after dredging; verify gold/cash do not duplicate incorrectly.
2. **Replay safety:** replay M22 after later gold recovery; verify historical state is not reset.
3. **End of current content:** complete every scripted mission; verify the game reports no next scripted story mission instead of sending the player to M01.
4. **Direct retry:** fail a mission with optional jobs available; verify Retry starts the same mission.
5. **Required vehicle loss:** destroy a required vehicle before its boarding stage; verify fail/recovery instead of softlock.
6. **Cutscene watch vs skip:** both paths produce the same cast/vehicle/objective state.
7. **M20 -> M21 cargo:** verify visible bullion continuity.
8. **Companion death:** verify the mission's intended fail/recovery policy is not bypassed by generic respawn.
9. **Apartment prologue:** verify LSIA scene -> car entry -> player drive -> chosen apartment/home loads with collision.
10. **Temporary weapon policy:** verify mission-issued weapons do not bypass intended unlock progression unless intended.

---

# Final direction to the implementation agent

Do not restart the project and do not rewrite the story from scratch.

The campaign's strongest pieces should be preserved:

- Ron as the glue without a formal hierarchy;
- Ice as the dependable tactical anchor;
- Gohan as an equal brother whose communication habit creates texture;
- equipment/intelligence that genuinely pays off later;
- Cypress as a meaningful loss;
- Davis as a personal return;
- the final escape as Ron's biggest clutch.

The current priority is to make the implementation **remember what happened, survive failure/replay, carry state cleanly across mission boundaries, and visibly perform the actions the script describes**.

Once those foundations are dependable, implement the remaining mission spine in controlled batches rather than adding more unverified content at once.
