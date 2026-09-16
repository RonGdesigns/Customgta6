# In-game mission placement editor

Open **F8 / debug menu > Survey coordinates > Mission placement editor**. Pick a mission (or a shared location group), then a named item. You can preview its position, place it at your current position, or visit it with the existing collision-checked teleport. Abort an active mission first. Changes apply when you start the next attempt; they do not move actors during a live mission or cutscene.

## The free camera

Press **Insert** (configurable as `SurveyCamera`) while a survey or the placement editor
is open, or pick **Free camera** in the menu. The view lifts off the character, who stays
frozen where he was standing, and you fly.

| Action | Controller | Keyboard |
| --- | --- | --- |
| Move | Left stick | WASD |
| Look | Right stick | Mouse |
| Up / down | RT / LT | Page Up / Page Down |
| Fast across a site | Hold sprint | Shift |
| Slow onto a spot | Hold crouch | Ctrl |
| Capture here | Capture key | F11 |
| Put the view back | Insert | Insert |

A capture taken while flying records the **camera's** position and the direction it is
looking, not the character's. A key whose kind is `land` is dropped onto the first surface
under the camera, which is what makes placing a marker on a roof or a deck a matter of
looking down at it. A key whose kind is `air`, `water`, `channel`, `interior` or
`underground` keeps exactly the height it was flown to — a helicopter hold is supposed to
be in the air, and pulling it to the sea floor would be worse than the estimate it
replaced.

Two things worth knowing. The camera is leashed to 600 m from the character, because the
game only streams so far and a point captured past that is a point over ground that does
not exist yet; the focus follows the camera so the world builds where you are looking. And
the camera is released on every exit — finishing the survey, dying, teardown — so a script
camera can never be left rendering.

## Controls

Select a mission, then **Survey all - visit placements in order** to review every
placement. Teleporting keeps the menu open. The current location and queue index
appear in the menu. Select **Place at my position**, adjust if needed, then
**Save and teleport to next**. Use Next/Previous to keep the existing position;
an unsaved draft must first be saved or explicitly discarded. The last point
stays open until Finish. Individual placements use the same persistent menu.

| Action | Controller | Keyboard |
| --- | --- | --- |
| Walk / look | Left / right sticks | WASD / mouse |
| Navigate menu | D-pad Up / Down | Up / Down |
| Select action | A | Enter |
| Finish / cancel unsaved draft | B | Backspace |
| Adjust selected facing, height, radius or count | D-pad Left / Right | Left / Right |
| Expand / shrink supported enemy circle | Hold RT / LT | Select radius row, Left / Right |
| Visit saved coordinate | Select Teleport to this spot | F7 |
| Save draft and remain here | Select Save this placement | F11 |
| Previous / next with teleport | Select Previous / Next | Home / End |

The orange circle and red dots preview supported enemy groups; the blue line
shows facing. Radius grows at 8m/s, bounded to 1-60m; quantity is bounded to 1-16.
The separate placement HUD hides while the menu is open so they do not overlap.
If you close the menu with F8, the earlier direct controls remain available:
X/Space places here, A/Enter saves and ends editing, B/Escape cancels, D-pad
adjusts count/facing, +/- changes radius and PgUp/PgDn changes height.

Selecting **Place at my current position** while seated captures the vehicle's origin and heading. On foot, it captures your character's position. The editor returns to the story character when necessary to release companion AI; the selected placement retains the pre-handover vehicle position. Use elevation adjustment for props or exact ground alignment. The existing game collision/navmesh code can still ground a prop or move an enemy slightly to a walkable spot when the mission starts.

## Implemented coverage

- All 219 authored location keys are available, including defined player starts, mission vehicles, terminal/prop anchors, objectives, homes and shared bases. Some keys describe a group or objective rather than one physical item. Moving a shared base affects every mission that uses it.
- M05's generator crew and M03's depot guards have working enemy **count and radius** controls. The mission's spawn loop, startup validation and kill objective use the configured group. Unedited groups retain their original arrangement and counts. Other keys do not display a working group-count control.
- M03 exposes each of its three incoming enemy cars' entry and destination points, plus each of its three cargo crates: nine additional editable slots. The last-crate loading scene walks to the actual crate position.
- M06 exposes both SWAT convoys' entry and destination points: four additional editable slots. The drivers travel to the saved destination and unload there. Edited long routes receive a bounded travel allowance of up to two minutes instead of always unloading after fifteen seconds.
- The 13 additional slots are optional: before you save one, the mission retains its original dynamic placement logic. Their initial preview points are estimates, not promises about where the dynamic script would spawn during a particular attempt.

This is an editor for wired mission placements, not a generic object spawner or a complete mission-authoring tool. Remaining hard-coded individual actors, other missions' enemy-count loops, helicopter approach routes, animation blocking and mission trigger areas need explicit bindings before they can be edited individually. The menu exposes the supported controls; an ordinary point edit does not secretly change enemy counts.

## Saves and recovery

Placements, heading, radius and count are written together to `scripts/Bloodlines/Bloodlines.Surveyed.ini`. The previous file is kept as `.bak`. Existing survey captures and group settings are preserved. The repository's authored data remains unchanged. Cancel writes no draft changes; a failed save restores the in-memory location and keeps the draft for retry. **Undo last saved placement** restores one edit during the current game session, including its saved override.

Begin with M05: place Ice's cliff start, the generator-crew circle, Mateo's boat and the crew dinghy. Start M05, inspect the scene, abort, then refine the placements. M05's separate location-test mode remains available to bypass placement validation while surveying; see M05-LOCATION-TEST-2026-09-12.md.

## Validation

Automated checks exercise draft isolation, cancellation, save failure, reload, undo, limits, circle points, and preservation of dynamic defaults. Mission integration checks exercise edited M05/M03 guard counts, kill-objective advancement, individual crate position/facing, M03 enemy-car routes and M06 convoy routes/unloading. The production build validates the controller/native API bindings. Live camera, collision and controller feel require an in-game test.
