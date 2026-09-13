# Convoy, Foundry headquarters and placement tour

## M09: Rolling Thunder

The intended sequence is: Guess takes off in the Frogger, shadows the rear escort,
Ice shoots its driver without destroying the truck, Ice removes the transponder,
Guess lands at the pickup, Ice boards, and Guess flies both of them to the drop.
The transponder remains the setup for M16, not a permanent immunity upgrade.

Previously, the convoy drove to its stopping point independently of a continuous
25-second, 60-220m tracking objective. Reaching Ice did not finish tracking, and
generic objective text obscured the distance/countdown. That combination fits the
reported apparent stall; the report was not reproduced in a live game here.

Changes:

- Convoy movement starts after Guess is in the pilot seat and lifts at least 8m.
- Three trucks use separate stops and a column aligned with their route.
- The HUD shows range, accumulated progress and loss-of-contact warnings.
- Track within 60-350m for 25 cumulative seconds, or maintain three seconds of
  valid contact once the rear escort reaches the ambush area.
- Grounded aircraft and passengers cannot complete aerial tracking.
- A stalled convoy gets a new road task every 12 seconds, at most four retries;
  a permanently blocked route fails with the location keys needed for correction.
- Ice's next objective explicitly identifies the DRIVER and warns against
  destroying the transponder truck. Guess receives a helicopter hold task during
  Ice's work, then returns control when the player switches back.
- Extraction explicitly requires Guess to fly, Ice to remain aboard, and landing
  at the drop rather than merely flying over it.

## Shared Foundry headquarters

Foundry HQ is an enterable, furnished industrial clubhouse adaptation, using
assets already installed with GTA. It includes a visible weapon locker, meeting
area and home services: wardrobe, rest/save, personal weapon restock and next-lead
planning. No Rockstar assets or RPF modifications are shipped.

Access follows the existing `cypressFoundry` ownership flag: acquired after M03,
lost when the story removes ownership in M22. This is separate from the brothers'
personal homes and apartment progression. Route to it from the debug menu, then
enter on foot at the gold headquarters marker in the foundry yard. Lose police
and finish missions/scenes before entering. Exit returns to the actual entry
position; saving inside records that exterior, not the underground shell.

The visual shell and gun-locker variant come from the
[industrial clubhouse definition](https://github.com/Bob74/bob74_ipl/blob/master/dlc_bikers/clubhouse2.lua).
The loading path waits for the IPL, interior and collision, with a 12-second
rollback if unavailable. On departure, interior variants return to their prior
state, including variants another script had enabled.

First-build limitations: live furniture positions and entrance placement still
need verification on this Enhanced installation. All services work from the
interior entry menu; room-specific interaction points activate only after their
`Hideout.Foundry.Room.*` survey. Use the existing room survey/map from inside.
Companions retain their exterior positions while the active brother visits;
an interior companion activity/cutscene pass is not included in this build.

## Placement tour controls

F8 -> Survey coordinates -> Mission placement editor -> select mission ->
**Survey all - visit placements in order**. It visits the first point and keeps
the menu open, showing the current index and location key.

- Sticks still move the player and camera. D-pad navigates; A selects; B finishes
  the current editor page and discards only its unsaved draft.
- **Place at my position** copies the current ped or occupied vehicle origin.
- **Save this placement** writes the draft and stays on the same point.
- **Save and teleport to next** commits, then visits the next point.
- **Next spot - keep existing** and **Previous spot** visit without rewriting
  coordinates. Unsaved changes must be saved or explicitly discarded first.
- D-pad Left/Right adjusts the selected facing, height, radius or quantity row.
  Hold RT/LT to grow/shrink a supported enemy group's radius.
- F7 visits the saved coordinate; F11 saves the draft. Home/End go previous/next.
- The final point stays open until Finish; it does not silently close the menu.
- Teleport/load failure restores the old player position while preserving the
  selected draft. Navigation/save are blocked until teleport finishes.
- The older survey menu also stays open after teleporting or starting a survey.

Survey saves remain atomic with a backup; existing INIs and saves are not replaced
by installing this package. Only groups wired into gameplay expose count/radius.

## Retest order

1. M09: lift off, stay 60-350m from the yellow rear escort, watch the displayed
   timer. Reaching Ice must hand off even before 25 seconds of tracking.
2. Switch to Ice. Confirm Guess holds the helicopter, shoot the driver through
   the cab, take the unit, then complete pickup and landed delivery as Guess.
3. Between missions, tour several M09 placements. Teleport must keep the menu
   open. Adjust/save/next, revisit, discard a draft, and finish.
4. With Foundry ownership, enter HQ. Verify collision, walking, gun locker,
   wardrobe, rest/save, planning and return outside. Report any furniture overlap
   using Map this room and the room survey.

Automated stand-in tests cover the transition, early handoff, lost contact,
blocked-route retries, hover ownership, Foundry unlock/timeout/exit, interior
variant restoration, tour persistence and unsaved-draft protection. A successful
compile or simulated run does not establish live traffic, geometry or animation.
