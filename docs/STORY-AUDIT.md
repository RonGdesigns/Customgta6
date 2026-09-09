# Bloodlines — story, cutscene and runtime audit

This pass builds on the earlier death, companion and survey repairs. It audits the 79 mission records and the shared runtime, adds story scenes, and fixes the concrete issues listed below. Automated results are not a claim that the campaign has been played from beginning to end in GTA.

## What is implemented

- M01 has a cold open across three separate private jobs, followed by a recognition scene after the playable approaches succeed.
- All 30 implemented missions receive camera-cut briefings and aftermath scenes. Mission setup and objective clocks begin after the briefing, so reading dialogue does not consume the mission time limit.
- The authored expansion contains 335 new cues across 159 scenes for all 79 mission records. M28–M70 and SM04–SM09 still need gameplay scripts; their scenes are preparation for those scripts, not playable content.
- Scenes work without recordings. Optional WAV files use stable cue IDs in the existing per-mission audio folders. Recording length extends subtitle timing. KJ needs a voice casting entry before the voice-generation tool can generate his lines; manually supplied WAVs work without one.
- Enter or controller A skips a scene. Holding the configured abort key (default Backspace) aborts a pending briefing or active mission. Teardown restores the prior camera, player control, frozen/invincible flags and streaming focus, and deletes temporary actors independently even if another cleanup operation fails.
- Gameplay names, map labels, menus and recovery/switch notifications use Ice, Gohan and Guess. Full names remain available for deliberate story references.
- Nearby switches, within 80 metres in three dimensions, transfer control immediately. Distant switches use a 150 ms fade plus at most 1.5 seconds of collision checks, then a 200 ms fade-in. Unready collision leaves the existing hero active. These remove the old scripted sky-camera waits; actual streaming performance still depends on GTA and the machine.
- KJ is a supporting friend, separate from the playable roster. He has a speaking camera scene and a marked NPC at SM03's starting line. His second appearance is written for SM09, whose gameplay has not been implemented.
- Available, incomplete missions have named map icons and nearby start cylinders. Locked/unimplemented jobs have no start icon. Press the mission key (default J), or controller D-pad right, near a marker to start that specific job. Main jobs are yellow; solo map icons use the protagonist's color. SM03 is labeled as KJ's contact.
- Active destination cylinders now also have yellow map icons, including race checkpoints and multi-site objectives. These do not force a GPS route. Existing authored target blips remain in use.

## Relationship arc

The three are childhood friends who call one another brothers. Their different surnames are retained. The new material expands the canon; it is not presented as text extracted from the PDFs.

| Character | Habit that damages the friendship | Change shown in play and dialogue | Payoff |
|---|---|---|---|
| Ice | Treats responsibility as a reason to decide alone and hide fear | Shares files, accepts Guess's extraction calls, admits the foundry was not defensible | Leaves the final holdout when called instead of making a sacrifice nobody requested |
| Gohan | Makes himself indispensable, hides uncertainty and equates competence with belonging | Gives the others copies, admits constraints before deployment, asks for check-ins | Releases evidence beyond himself; escape no longer depends on saving the only copy |
| Guess | Covers hurt with jokes and lets the others mistake the driver for an accessory | Asks to be consulted about routes, cargo and abort windows; prepares exits before the score | Counts all three people before cargo; a shared breakfast replaces a promise of endless jobs |

The repeated preparations gain an immediate consequence: why a specific asset is needed, what the completed job makes possible, and what its risk costs the friendship. The foundry's loss pays off the early promise of three keys. Rescue of Ramos establishes that access and evidence do not make a person expendable. Davis tests whether escape matters more than the people left behind. The final ocean fallback is discussed before the aircraft is disabled.

KJ has a limited role: he checks a racing deal and later makes a shipping introduction. He has boundaries and incomplete knowledge. He does not replace the trio, become a fourth hero, or suddenly solve the conspiracy. No main scene requires completion of an optional solo mission.

## Concrete audit fixes

| Finding | Correction |
|---|---|
| Every switch started the aerial sequence, including adjacent passengers | Direct nearby handover; bounded fade/collision path at distance |
| M27 already owns a fade when transferring to Ice | Explicit scripted-transfer path preserves the mission's fade instead of rejecting or clearing it |
| Objective text could overwrite dialogue each frame | Dialogue now renders after mission updates; cutscenes suspend competing runtime/QA overlays |
| M01's two-seat T20 conflicted with three-person extraction | Four-seat Schafter V12 prototype test mule, preserving the high-performance theft premise |
| M01 could lose its required target before aiming/recognition | Mateo is protected as a required story actor; failed essential spawns reject/fail the mission |
| M01 could advance after clearing guards without ever triggering Mateo's escape | Escape must trigger before extraction becomes available; stalled boarding gets a bounded scripted fallback |
| M02's Guess had no driving task when the player switched into a passenger | Mission-controlled pursuit and escape driving while Guess remains at the wheel; control is released when the player takes him back |
| M08/M10 installed the turbine upgrade before M11 | Installation unlock remains with M11 |
| M09 unlocked the permanent desert bunker before the exile/M23 | M09 ends at a temporary drop point; M23 owns the bunker unlock |
| Mission teardown could delete a vehicle occupied by the player or a companion | Occupied transport is released to the world rather than deleted underneath them |
| SM03 rivals drove only to the first checkpoint | Rivals advance through the circuit and lap count; a rival finishing first fails the race |
| SM03 checkpoint progress did not require the race car or intended hero | Checkpoints require Guess in the mission coupe; wreck protection continues through the ambush |
| Campaign reset retained acquired upgrades/safehouses | Reset restores the initial unlocks, fleet flags and last-position fields |
| Save replacement deleted the old file before moving the new file | Same-directory atomic replacement retains the previous save as `.bak` |

## Continuity decisions

- M05's line about taking the war to Blaine is treated as Ice's intention, challenged by the outstanding evidence-vault threat. Actual exile remains M22. The source PDF's misleading M05 “Act I Finale” label is not a claim that Act I ends there.
- Stopping the comm-van upload, destroying the evidence-depot backup, spoofing some plate cameras and disrupting later emergency warrants are different operations. The new dialogue avoids claiming that a database operation erases all witnesses or copies.
- The port manifest's claimed $3B shipment is separate from the rig's $500M bonds/escrow. Recovering a portion is not the same as having the entire manifest value available to spend.
- The M48 coastal cordon and M49 county-line barricade are successive obstacles, not two unexplained returns to the same city.
- M55 constrains escrow access; M65 obtains master authorization; M67 routes it from outside the tower. Those jobs now have distinct purposes.
- Colonel Vance is treated in the new dialogue as unrelated to Ice. This is an authored clarification of a surname ambiguity, not a family connection inferred from a matching name.
- The full-name exchanges at the reunion and ending are intentional story beats. Routine references use nicknames.

## Validation and limits

Run `python tools/run_regression_tests.py` and `python tools/run_story_tests.py`: 33 existing checks plus 43 new story/runtime checks pass using test stand-ins. The new suite exercises production switch, scene, dialogue, dispatch, save, mission cleanup, marker and race-checkpoint code. It does not run GTA natives.

Release compilation succeeds against the configured SHVDN 3.6 reference. Mission lint reports no errors. Its M01 6/9 source-cue count is expected: the original three S2 recognition lines are replaced by the authored nine-line recognition scene. The other 29 implemented missions retain their source-cue coverage. Story validation checks all 79 records, unique cue IDs, generated-file freshness, KJ's two appearances, and all 30 start-marker mappings. Location validation flags 0 of 121 entries at district level using cached reference data.

Outstanding work requiring actual game observation or future implementation:

1. **Geometry and staging:** most positions remain estimates. District validation does not establish walkable ground, a usable hatch, a visible camera shot, boat depth, or a navigable race route. Game-folder access alone does not turn the bible's conceptual interiors into installed geometry. Use the survey route/F7 teleport/F11 capture and inspect the location in GTA.
2. **Scene presentation:** these are first-pass camera-cut scenes with held character poses, subtitles and optional audio. They do not yet have bespoke performance animation, facial/lip animation, recorded dialogue, or shot-by-shot collision-aware camera placement. Check framing at every surveyed location, especially rooftops and interiors.
3. **Gameplay coverage:** 49 mission scripts remain unwritten, including the ending and KJ's second gameplay appearance. Custom high-rise/offshore interiors and several major set pieces also remain future work.
4. **Recovery:** active-player recovery remains a full mission failure/retry unless that mission implements a genuine world-rebuilding checkpoint restore. Current scripts do not opt in. This avoids pretending that restoring positions reconstructs destroyed vehicles and objective state.
5. **AI and physics:** boarding animations, combat reactions, M02 driving, SM03 race competitiveness, distant switching in aircraft and the new cinematic camera must be playtested in Enhanced. Automated stand-ins cannot establish those native behaviors.
6. **Economy:** mission rewards still follow existing scripts and can be earned on deliberate replays. This pass fixes premature unlocks and reset behavior, not a complete replay/economy redesign.

## First live pass

1. Load Story Mode and find the yellow Bloodlines mission icon. Approach it and start with J or D-pad right. If the current save already completed M01, use the existing mission-selection menu to replay it; do not reset the save just to see the opening.
2. Watch and then separately skip the opening. Repeat with hold-Backspace abort and a script reload. Confirm camera, movement and HUD return each time.
3. Finish the M01 approaches, watch recognition, confirm Mateo escapes, and extract all three using the four-seat prototype. Read the aftermath before starting M02.
4. Switch between nearby crew on foot and in one vehicle. Test a distant target and a failed/unready target. In M02, switch to Gohan while Guess continues the pursuit.
5. After M03, visit the KJ race marker. Confirm KJ appears, speaks as KJ, is absent from the switch roster, and the rivals drive beyond checkpoint one.
6. Check that objective map icons move to the next race checkpoint and disappear on completion, abort, death and reload. Confirm previously surveyed coordinates still win over defaults.
7. Complete a boat/aircraft mission while still aboard. Confirm the vehicle survives mission completion and the aftermath releases it normally.

No live playthrough was performed during this pass. Installation preserves the existing save and INIs; rollback artifacts and a hash receipt accompany the build.
