# AI, driving and mission update — 8 September 2026

This update changes the shared companion controller. Driving handover and separate-car catch-up work beyond M02. M02's van, hack and ambush remain specific to that mission.

## Crew behavior

- Free roam defaults to **Independent**: inactive characters wander locally instead of following or teleporting to the player. Seated passengers keep their seats and NPC drivers can carry on driving.
- In **F8 → Crew → Free roam crew**, choose **Travel together** to have the crew accompany you. This preference lasts for the current mod session.
- With Travel together, a companion left on foot around 200 metres behind looks for nearby road transport and enters normally. Beyond 500 metres, recovery can place them in a separate car about 100 metres behind you, only when both the companion and destination are outside the camera and collision is available. If those conditions are not met, recovery waits. Mission and player vehicles are excluded from nearby car acquisition.
- Required shared mission vehicles retain a boarding recovery fallback. Nearby entry uses the doors first. General free-roam boarding no longer immediately warps a companion into your passenger seat.
- Switching away from a driver transfers driving to shared AI. Assigned mission destinations take precedence over a map waypoint; without either, the driver cruises cautiously or follows the travelling crew. Mission-owned chases take precedence over generic driving.
- Boats and airborne aircraft use their respective navigation tasks. Grounded aircraft await player takeoff; planes circle without a destination, trains follow their rails and submarines hold their current depth/position. Safe pathfinding and obstacle clearance require live testing.

## Mission roles and scenes

- Character-owned foot objectives can direct inactive characters toward their own task location. Player-owned objectives still require the correct active character to finish them; explicitly autonomous work can continue in the background.
- M06 deploys Gohan at the culvert, Ice at the alley, and Guess in the extraction Granger. Gohan can work on the racks while the player handles Ice's siege. Extraction requires Guess driving and all three aboard.
- Mission cleanup releases AI ownership so it cannot leave companions stranded under a finished script.
- Regular scenes use the existing cast and preserve positions and vehicle seats. Distant/missing speakers use phone/radio framing; remote dialogue keeps the camera near the player. M01 retains its deliberate split cold open and later recognition meeting. KJ remains a supporting NPC in his written appearances.
- A clear objective gets a mission GPS route automatically. With parallel character assignments, the route follows the active character's destination. Enemy markers do not automatically become driving destinations. The debug menu now has a readable current-objective page.

## M01 and M02

- M01 starts Guess in an approach car farther from the prototype, places Gohan farther from the ledger, and expands the surveillance introduction to explain the jobs before their meeting.
- M02 starts Guess driving, with Gohan and Ice already seated. Catch the moving van, then remain within **35 metres** for **24 seconds of connected hacking**. You may keep driving as Guess or switch to passenger Gohan; leaving range pauses the hack.
- Van guards open fire at **50%** hack progress. At completion, the van is disabled without destroying its servers.
- Switch to Ice, get out and stand at the marked **rear doors for three seconds** to collect the drives. Killing the technician or stealing the van is not the retrieval objective.
- Return all three to the Granger and follow the GPS to the canal. Guess can continue driving while another character is active and waits for boarding before departing.

## Controller and appearance

- Ability activation uses **L3 + R3 together**, with a compact yellow meter near the minimap's normal ability-bar area.
- The character wheel is smaller and positioned at the lower right. Hold D-pad Down, select with the right stick and release Down to switch.
- Debug menu: F8 or Down + B; navigate with the D-pad or right stick, A to select and B to return.
- Default cast skin settings now use the shared dark skin parent, with Ice's locs, Gohan's short-hair default and bald Guess. Installation corrects Guess's legacy saved face/skin while preserving separately edited hair, outfits and Gohan's current custom face. These remain stock freemode approximations.

## Retest in this order

1. In free roam, choose **Travel together**, drive with both companions aboard, set a distant waypoint and switch to a passenger. Check that the driver keeps the road and your selected character keeps the same seat. Switch back and verify immediate manual control.
2. Leave a companion on foot, drive past 200 metres and then 500 metres. Check normal acquisition of a separate car, followed by unseen catch-up if necessary. Look back deliberately: no recovery should appear in view. Toggle Independent and confirm the crew stays local instead.
3. Replay M02 from the start. Test hacking while playing Guess, then Gohan; briefly exceed 35 metres; watch for shooting at halfway. Retrieve the drives as Ice, reboard and finish at the canal. Inspect the current-objective debug page if a stage is unclear.
4. Replay M01 and check the new approach distance, initial seating, surveillance framing, separate tasks and return to the player camera. If available, test M06's separate stations and Gohan's background work.
5. Test a briefing while seated, then a phone/radio scene with the crew separated. Skip one scene and let another finish. Verify seats, positions, camera and controls afterward.
6. Check L3 + R3, the minimap meter, smaller switch wheel, D-pad debug navigation and each character's appearance. Die during a mission and once in free roam; confirm movement returns after respawn.

## Verification and limits

Roslyn compiled 79 production source files against the pinned ScriptHookVDotNet reference. Passed: 121 story/runtime checks, 65 companion/recovery/survey checks and 3 parser tests. Mission lint reported no errors; all 123 locations passed the district audit. Story generation is fresh for all 79 missions (337 scene cues, 159 scenes).

Tests use GTA stand-ins, not a running game. The location audit checks district plausibility, not exact ground height or accessibility. Vehicle pathfinding, model appearance, HUD alignment, animated boarding and remote scene rendering still need the live retest above. The campaign still has 30 playable scripts and 49 missions with dialogue but no gameplay script; this update does not claim a full campaign playthrough or completion of those remaining scripts.
