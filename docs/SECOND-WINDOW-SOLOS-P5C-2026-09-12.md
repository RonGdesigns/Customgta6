# P5c: second-window solos

Implemented September 12. Automated checks and compilation are required for the package; live acceptance remains pending. This advances the existing SM04-SM06 scripts, available after M28. It does not introduce M31-M70 or SM07-SM09, change main-story gates, or assume these optional jobs have already happened in main-story dialogue.

| Job | Playable sequence | First-completion reward |
|---|---|---|
| SM04 Dead Drop Quarry | See the two listening posts; reach Ice's overlook; kill both marksmen in either order; recover two physical radios at the posts with separate interactions; return to the approach and confirm both channel copies by radio. | $35,000 crew cash; Ice's Heavy Sniper Mk II at the locker; both radios recorded in the crew archive. |
| SM05 Black Box Estuary | Board Gohan's dinghy; drive to the monitoring buoy; stop within 8m; the boat stays moored while Gohan swims to the service box and holds a six-second interaction; see the fitted interceptor and verify its signal; board the waiting boat and drive back to the water pickup. | $35,000 crew cash; Gohan's Pistol Mk II at the locker; telemetry recorded. |
| SM06 Canyon Runner | Board Guess's coupled fuel rig; evade up to three occupied pursuit bikes; drive through the canyon checkpoint; deliver the tractor and attached tanker; hold both stopped in the bay for the six-second unload; see the receiving equipment and confirmation. | $45,000 crew cash; Guess's SMG Mk II at the locker; fuel recorded at McKenzie. |

The existing completion panel describes newly earned weapons and cash. Replays remain unpaid. The radios, telemetry and fuel are recorded story resources; this package does not claim universal police tracking, free aircraft fuel, explosive sniper ammunition or any unimplemented intelligence system.

## Scenes and positioning

Each solo has a short introduction anchored to its actual actors or equipment and a confirmation scene tied to the physical result. The other brothers speak remotely. No new scene gathers the cast or replaces their vehicle positions. Watching and skipping must produce the same verified result; canceling a required result cannot award it. Prompts carry the controls and distances; dialogue explains motive and consequence.

SM04's radios belong to the observation posts, so the objective names those posts even when a marksman moves during combat. Each collected radio disappears into Ice's inventory only after its interaction. A missing or destroyed set blocks collection.

SM05's mooring changes only its mission dinghy. Gohan can exit, swim and board normally. Entry restores the boat's previous frozen/engine flags; abort, failure and teardown also restore them. No swimming animation is forced into an on-foot work pose. The native swimming, climbing and boat physics still need live testing.

SM06's bend is a drive-through checkpoint. Unloading rechecks coupling, proximity and both vehicle speeds every frame; detaching or moving resets the timer and requires a fresh interaction. The final scene rechecks the delivered rig before recording fuel. Pursuit dialogue only plays if the bikes actually spawned. Unavailable optional road spawns do not invalidate the essential tanker delivery.

## Geometry and assets

The read-only Enhanced CodeWalker audit found that the old `prop_buoy_01` did not exist and the old estimated buoy point sat over ground above the water surface. The replacement is the installed `prop_dock_bouy_3`, with `prop_tool_box_04` as its service interceptor. Installed radio and receiving-equipment assets were also verified.

The estimated SM05 landing moves to the estuary mouth: shore (-2780, 2540, 2.015), dinghy (-2780, 2580, 0.2), buoy (-2900, 2580, 0.2). Downward collision samples every 20m along the direct boat route found depths from 3.862m to 13.720m. These are geometry checks, not live surveys. Existing saved survey overrides retain priority. If an old personal SM05 override still points inland, survey those three keys again.

Audit outputs are local under `build/codewalker-solo-map`, `build/codewalker-solo-water3-map` and `build/codewalker-solo-route-map`; no Rockstar asset is redistributed. SM04 and SM06 existing location keys remain unchanged.

## Failure and retest

All three jobs retain full-restart retry. No checkpoint restoration is promised. Hero death, essential asset loss and required-scene failure must fail explicitly; the manager discards provisional story resources. A first successful completion commits rewards once.

1. Run each solo after M28; watch once and skip once. Check returning camera and movement.
2. SM04: clear the marksmen in reverse order; collect both radios; inspect the two counts and copied-channel confirmation. Abort after one radio and retry.
3. SM05: stop near the buoy, exit and swim, finish installation, return aboard and drive home. Abort while moored and check movement. Confirm the actual water approach and service-box position.
4. SM06: pass the bend without stopping; arrive with the tanker; detach halfway through unloading, reconnect and restart. Verify the receiving scene, bike pursuit and final weapon/cash announcement.

## Included M05 and Foundry repairs

M05 now keeps Guess driving the chase. With Gohan alive in the dinghy, staying within 45m builds a 24-second remote engine disable. Leaving range pauses progress. Once disabled, stop both boats within 18m before the mission requests Gohan's boarding interaction. The earlier retry station fix stays in place; the pending optional mid-chase switch is superseded.

The Foundry entry log at 18:33 on September 12 timed out with interior=0 and an inactive IPL. The runtime request now uses `bkr_biker_interior_placement_interior_1_biker_dlc_int_02_milo`, without the internal archive name's trailing underscore. This matches the [primary clubhouse implementation](https://github.com/Bob74/bob74_ipl/blob/master/dlc_bikers/clubhouse2.lua). The old archive-name inference in the garage report is superseded. Collision/readiness checks, furnishing variants, return-on-timeout and saved controls remain intact. Repeating two wait-state messages every frame is corrected. Live entry, locker use and exit require a retest; a mock/native-call test cannot prove Enhanced streaming success.

Next: live acceptance of P5b/P5c and reported repairs, followed by an explicitly scoped M31-onward implementation package from `CAMPAIGN-REMAINDER.md`.
