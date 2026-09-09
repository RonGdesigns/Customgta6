# Six-star pressure and verified recovery

This build includes the previously queued individual-respawn and controller-camera changes.

## Findings from the September 8 playtest

The installed build logged six-star authorization at 23:11:31, helicopter creation at 23:11:41, convoy at 23:12:01 and tank at 23:12:21. A tank existed but the log did not establish that it reached the player. The old dispatch log also incorrectly called the convoy a helicopter during loading and a tank during task assignment.

At 23:16:57 the old build revived and regrouped the whole crew. At 23:16:58 it declared success from control/collision flags. The player reported being unable to walk. The last mod log entry is the attempt to switch to Ice at 23:17:49; that sequence does not identify a precise crashing native.

## Changes

- Military dispatch starts after about four seconds, followed by the convoy at twelve and the tank at twenty. These are spawn targets, not guaranteed arrival times. Normal five-to-six progression still requires ninety seconds at five stars.
- Ground units try four streets, reject visibly occupied areas and substantially different road elevations, and retry unavailable approaches after three seconds. Aircraft have alternate off-camera approaches too.
- Tanks route toward the player before switching to attack within 125m with clear line of sight. Existing real-cannon selection, firing interval and line-of-sight checks remain. Convoy drivers retain their chase task instead of restarting it every four seconds.
- Ground units stationary for thirty seconds outside useful range can be replaced off camera, with at least four seconds before replacement. Visible or player/other-passenger-occupied vehicles are protected. The response remains capped at one helicopter, one convoy and one tank. No additional native police helicopter pool is added.
- Dispatch logs identify each actual unit and report distance, speed and positions every fifteen seconds, plus deferred placements and stranded replacements.
- Death revives only the active hero. Other heroes keep positions, vehicles, health and individual wanted levels; automatic catch-up teleport stays suppressed until reunion or a fresh behavior order/mission. Another dead hero is not revived alongside the player.
- Respawn no longer performs CHANGE_PLAYER_PED when that hero is already the player. This removes an unnecessary handover; live testing must establish whether it resolves the reported lock.
- After collision and control restoration, walk a few steps to finish recovery. Actual walking input and horizontal movement unlock gameplay and switching. Standing idle is allowed. If four seconds of attempted walking remain blocked, one control-repair attempt is followed by a return to the original Story Mode character. That exceptional fallback dismisses the custom crew and requires F10 to redeploy.
- Debug/home menus use D-pad and A/B. Left stick moves, right stick looks; perspective cycling stays blocked while browsing.

The self-handover removal and movement check are defensive changes based on the code and playtest sequence, not a confirmed crash diagnosis. Native control flags are documented in the [Cfx native reference](https://github.com/citizenfx/natives/blob/master/PLAYER/SetPlayerControl.md); those flags cannot alone prove that an actor can walk.

## Retest in this order

1. Deploy, leave teammates elsewhere, and die without military active. At respawn, hold the left stick and walk a few steps. Confirm teammates remain elsewhere, then switch heroes.
2. If walking is blocked, keep holding the movement stick for four seconds. The fallback should return you to the original Story Mode character; record whether movement works there. Do not use character switching to escape the recovery state.
3. On a broad surface road, set six stars, close the debug menu and remain in that area for at least thirty seconds. Check for the helicopter, convoy and tank entering the pursuit. Dispatch time pauses while the debug menu owns the update, so judge timing with it closed.
4. Drive several blocks and check pursuit, convoy passenger fire and occasional tank fire with unobstructed range. Destroy a unit and verify replacements remain staggered.
5. Repeat death during six stars, walk after respawn, then switch. Check D-pad browsing with both sticks available in the debug menu.

Production compilation, 311 story/runtime checks, 127 behavior/recovery checks, three dialogue parser tests, mission lint and location validation pass. Tests use stand-ins and do not establish native GTA stability or road navigation. No full in-game validation has been performed by Codex.

Installation replaces only scripts/Bloodlines.dll. Progress, INIs, wardrobe, dialogue and location data are preserved.
