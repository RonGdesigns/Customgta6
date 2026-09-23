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
| **Up / down** | **RT / LT** (analog) | Page Up / Page Down |
| **Pan left / right** | **LB / RB** | Q / E |
| Fast across a site | Hold sprint | Shift |
| Slow onto a spot | Hold crouch | Ctrl |
| Capture here | Capture key | F11 |
| Put the view back | Insert | Insert |

The shoulders carry the two movements a stick is clumsy at. The triggers are analog, so a
light pull is a slow climb and a full pull a fast one — which is how you settle on a
height for an air key. The bumpers pan the view at a steady rate, slower than the stick,
for lining a shot up rather than looking around.

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
| **Save this placement from any row** | **Y** | Select the row (second from the top) |
| **Place at my position / the camera** | **X** | Space with the menu closed |
| Back out of the stand-in or additions page | B (returns to the survey page) | Backspace |

### The survey page, reordered (September 22)

Ron's report: saving took a long scroll, because **Save this placement** was the tenth of
eighteen rows. The page now runs in the order a placement is actually done:

1. Current placement (and whether it has unsaved changes)
2. **Save this placement** (Y)
3. **Save and teleport to next**
4. **Place at my position** (X), which uses the camera while it is flying
5. Free camera, Teleport to this spot, Next spot, Previous spot
6. **Add enemies, vehicles, props**, described below
7. Facing, Height, Enemy count, Enemy radius
8. Stand-in (the flown ghost and its distance and facing, on a page of their own)
9. Accept this spot as correct, Discard unsaved changes, Finish survey

Y saves from anywhere on the page, so a save never needs a scroll. B on the stand-in or
additions page steps back to the survey page. Only B on the survey page itself ends the
survey, as it always has.

## Adding enemies, vehicles and props

A mission's own keys are the places its script already reads. Anything more (another
group of men, a car with men in it, a crate stack to fight behind) is an **addition**. Open
it from the mission's list (**Add enemies, vehicles, props**, near the top) or from inside
the survey page. The camera comes up, and the stand-in in front of it becomes the actual
thing you are about to place.

| Row | Left / Right changes | Notes |
| --- | --- | --- |
| Drop it here | (select or **X**) | Writes it at the stand-in, dropped onto the surface under it. A boat is dropped onto the water. |
| What | enemies on foot / a vehicle with men in it / a prop | |
| Model | the man, the vehicle, or the prop | **Y** loads the next model, and the stand-in changes to it |
| Crew | who rides in a vehicle | vehicles only |
| How many | 1 to 8 men, or men aboard | a vehicle takes no more than it has seats |
| Spread | 0 to 25 m | enemies on foot only; they stand in the same sunflower spread mission groups use |
| Weapon | pistol, SMG, carbine, rifle, shotgun, MG, sniper, RPG | |
| Side | cartel / Aegis | Aegis men get armor |
| Orders | hold this spot / patrol the area (or drive around) | |
| Facing, Distance from camera | turn the stand-in, push it out | |
| Placed in this mission | | each one: go to it, move it to the stand-in, remove it |

Keyboard on that page: Space drops, Tab loads the next model.

Each addition is saved **the moment it is dropped** to `Bloodlines.Additions.tsv`, beside
the survey ini (`.bak` kept). It spawns the next time that mission starts, including in
the staging preview. Placed things already in the file are drawn where they will spawn,
with their id over them: red for men (one dot per man), orange for vehicles and blue for
props.

In the mission they belong to the mission. They are tracked like anything it spawns, and
its cleanup takes them away on pass, failure or abort. A placed man holds his orders until
one of the crew comes within 60 m (90 m for a vehicle crew), then is told once to fight.
Additions never count toward an objective, and a mission passes or fails exactly as it did
without them. A continuous operation's chapters each take their own, so the entry chapter
is never spawned twice. Up to 40 per mission.

The pick lists are models the campaign already uses or that `build/vehicles.json` and
`build/peds.json` confirm. `tools/lint_missions.py` checks the men and vehicles against
those dumps, so a typo there fails the linter rather than silently spawning nothing.
Nobody has watched a placed addition fight in game yet.

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

## Staging a whole mission

**F8 > Survey coordinates > Mission placement editor > pick a mission > Stage this mission's
world.** The mission's own `Setup` runs and nothing else: no stages, no objectives, no
timers, no progression, no rewards. The vehicles, crates and guards stand where that
mission would put them, pacified, and the survey camera comes up on the same key press so
you can fly around the layout immediately.

Closing the dev menu closes the staged world with it. The teardown is the **mission's own
`Cleanup`** — the path every attempt already uses — so a preview cannot leave a second
Cargobob standing for the real attempt to collide with. Any brother the staging deployed is
taken out of a staged vehicle and put back where he was standing.

It also writes **`scripts/Bloodlines/Bloodlines.Staging.txt`**: every entity that was
staged, with its position, heading, and the nearest authored key. Anything marked
`derived — no key within 12 m` is a position the mission computed at runtime — M18's rear
work point off the hauler's dimensions, M45's probed deck height — which no data file
contains and nobody could previously see.

## The stand-in

With the camera up, **Stand-in** hangs a translucent person, car, truck, helicopter or boat
in front of the view (left/right cycles the shape; the key's kind picks a sensible one to
start). Fly it into place, adjust **Stand-in distance** and **Stand-in facing**, then
**Place from the stand-in** writes the draft. A `land` key drops onto the surface under it;
air, water and interior keys keep the height they were flown to.

It is a *shape*, not the exact model a mission spawns — nothing in the location book says
which vehicle belongs at a key. It answers the question that actually goes wrong: does
something roughly this size fit here, level, facing that way. For the real article, stage
the mission.

## Routes

A route is **ordered keys**: `M09.Convoy.01`, `M09.Convoy.02`, `M09.Convoy.03`. No second
file and no second format — a route is points that happen to be numbered, so every tool
that reads the location book reads routes for free. Survey them like any other key and the
connecting line is drawn while you are in that mission's survey or staging preview: amber
while any leg is still an estimate, green once every leg has been walked.

## September 16: the two faults Ron found editing M60

**Teleport in fly mode.** The teleport moved the character and left the camera behind, and
the camera holds the streaming focus, so the collision the teleport waits on loaded around
neither of them and the four-second gate rolled it back. It now sends the camera to the
destination one standoff back along its own line of sight - never below the point, which
underground or on a deck is inside the floor - and a rollback returns the camera too.

**Enemy count and radius.** Only two keys were ever declared as groups, so the count row
answered "not a group" everywhere else. `MissionPlacement.Groups` is now one table, and
M60's and M70's waves, M63's nests and M45's deck detail are in it. The rows say which of
three things a key is: a detail the editor sizes and arranges, a detail the editor sizes
while the mission arranges it (M45's deck, whose posts are probed for a deckhead), or one
man standing at a point.

A declared key changes nothing until it is edited: an unedited key has no count, so every
mission keeps the size and the offsets it was authored with. Nothing here has been used in
game yet.

## September 16: the survey reads out, and checks a mission at a time

**The readout.** The survey HUD now states what is at the spot the camera is looking at:
ground above or below, headroom, clear radius, whether an interior loads, water height, the
zone, and the nearest other key of the same mission. That last one is the site's shape,
which an individual coordinate cannot show - M48's cordon spawned on the crew because one
surveyed key ended up eighteen meters from another.

**The capture checks the key's kind.** A land key with nothing under it, an interior key
where no interior loads, a water key above the waterline, an air key on a deck: each is held
for a deliberate second press rather than written quietly. It is never refused outright.

**Check every spot in this mission.** The sweep teleports through a mission's keys, reads
each one and writes `Bloodlines.Survey-Check.txt` beside the survey ini - the spots that
need a look first. Then **Accept every spot that checked out** records those at the
coordinates they already have, which is what having looked at them means. A spot that did
not check out cannot be accepted; fly to it and place it.

A spot that checks out is placeable. Whether it is the right place for the beat is still a
judgment. None of this has been used in game yet.
