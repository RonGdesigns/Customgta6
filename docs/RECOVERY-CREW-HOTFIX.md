# Bloodlines: recovery, crew and character-wheel hotfix

Status: compiled and tested outside GTA; installation receipt records whether it has been installed. Live behavior still needs a retest.

## Changes

- Respawn clears frozen position, disabled collision, hidden state, retained NPC tasks, handcuffs and movement clipsets, restores player control with the death-reenable flag, and releases scripted camera/focus. Debug menu, survey and pending wheel input close during recovery. Vehicle recovery uses bounded exit; failure returns to the original story character.
- Companion target selection excludes all roster handles and the current player, including cached targets after switching/respawning. Existing combat against a now-friendly target is cancelled. Crew relationship membership is reapplied after handover, mutual friendship is explicit, and crew damage is disabled for the crew relationship group. Generic combat orders use the same target filter.
- M01 starts as Guess. Ice covers Mateo and Gohan works at the ledger under mission-owned AI at separate locations. Gohan must be on foot to copy the ledger. The opening scene starts with Guess's private channel. Exterior locations remain estimates until surveyed in game.
- The debug menu has a first-row M01 replay action. Mission ID/title remain visible during gameplay. Your previous log launched M02, with M01 already completed; campaign progress is preserved.
- Hold D-pad Down for the custom three-section wheel, select with the right stick (left Ice, up Gohan, right Guess), release to switch. The wheel slows time to 20% and restores the previous speed when closed. Nearby switches remain immediate; distant switches still wait for collision readiness.
- Stock male freemode looks: Ice dreads, Gohan short buzzcut, Guess bald, with distinct face settings. These are stock approximations; custom long locs/body proportions still require art assets. Debug Crew/Appearance can adjust and save each look. Clothes may change after two minutes away on a distant free-roam handover; identity is retained and mission/combat/vehicle transitions are excluded.

## Retest

1. Open debug (D-pad Down + B), choose **Replay M01: Ghost in the Dockyard** with right stick/A. B returns. Do not use Start Next for this test: your save has M01 complete.
2. Confirm Guess starts by the car, Ice is covering Mateo elsewhere, and Gohan is at the ledger. Check the wheel, complete all three jobs, and drive the crew out only after the confrontation.
3. In combat, switch among all three brothers and confirm they target enemies. Die on foot, then in a vehicle, and verify movement, aiming, jumping and camera control after each recovery. Also test a death with the debug menu open.

## Validation and scope

Production C# compilation against pinned SHVDN 3.6; automated stand-in tests cover switching, scenes, M01 flow, saves, identity-safe targeting, movement recovery and appearance state. These checks cannot prove animation, pathfinding, streamed terrain or GTA native behavior in a live session.

All 79 missions have dialogue drafts; 30 have gameplay scripts. M28–M30 and SM04–SM06 remain pending while these live blockers are repaired. Their unfinished helper classes are excluded from this build.

Native behavior references: [friendly targeting](https://github.com/citizenfx/natives/blob/master/PED/SetCanAttackFriendly.md), [relationship damage filtering](https://github.com/citizenfx/natives/blob/master/ENTITY/SetEntityCanBeDamagedByRelationshipGroup.md), [player-control flags](https://github.com/citizenfx/natives/blob/master/PLAYER/SetPlayerControl.md).
