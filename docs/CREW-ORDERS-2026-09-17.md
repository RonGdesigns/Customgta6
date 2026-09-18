# Crew orders, September 17, 2026

Ron, after a fight: he got into the driver's seat of a truck and both brothers took the
cab seats, never the gun. He switched, took the gun seat himself, and the brother at the
wheel got out. "We need a way for our AI to be a little bit more useful when out in free
roam." Five pieces were built, plus a way out of M55 and a smaller mission HUD from the
same message.

## What was actually wrong

Neither fault was a missing feature. Both were rules in `Crew/CompanionController`:

 * **The seat picker took the lowest free index.** The driver's seat is index minus one and
   was never in the loop, so no brother could ever take the wheel of the player's vehicle.
   The turret is the highest index, so it was the last seat anyone took, filled only after
   every ordinary seat was.
 * **A brother at the wheel left the vehicle the moment the player was on foot within 65
   meters and it was stopped.** Stepping out to walk around to the gun bed is exactly
   that. Once he was out, the first rule put him back in as a passenger.

## 1. Seat sense

`FreeSeat` now chooses in this order: the seat an order names, or nothing (an ordered seat
is that seat or none); the wheel, when the player has been riding in a seat that is not the
driver's for `WantsDriverMs` (a second and a half, because shorter than that is the engine
shuffling him across from the passenger door); then a turret before a plain passenger seat,
because a gun in the bed is for using; then the rest by index. Turrets are read from the
vehicle with `IS_TURRET_SEAT`, never assumed from a seat number.

## 2. The driver holds the wheel

The controller keeps two clocks on the player: how long he has been on foot, and how long
he has been riding in a seat that is not the driver's. A player on foot within
`SeatShuffleMeters` of the vehicle he was last in, for less than `SeatShuffleMs`, is
changing seats: the driver holds still (`CompanionDriver.HoldStill`) and nobody gets out.
Past that, or further away, the driver gets out to meet him as he always did. The grace
names the vehicle he was last seen in, so a player walking up to a brother's car for a
reunion, never having been in it, still gets the driver out to meet him.

An ordered driver (take the wheel, drive to my waypoint, pull over) keeps the seat however
far the player walks, until told otherwise.

## 3. Quick orders

`Core/CrewOrderStrip`. Hold `G` (configurable, `[Keys] CrewOrders`) or, on a pad, hold
d-pad left past a tap; a strip comes up at the bottom of the screen with time slowed the
way the character wheel slows it. Left and right pick a brother, up and down the order,
release sends. The orders offered are read every frame from `CrewOrders.Available`, so
they change as he moves:

| about a vehicle | always |
|---|---|
| Take the wheel (the driver's seat is free and not the player's) | Follow me |
| Drive to my waypoint (a waypoint is set) | Hold here |
| Pull over (he is driving) | Do your own thing |
| Man the gun (a turret is free and he is not on it) | |
| Get in / Get out | |

"The vehicle" is the one the player is in, or the one he just got out of and is still
standing beside (`CrewOrders.Subject`). The phone's crew page is unchanged and is where a
plan is made; the strip is for the middle of a fight.

An order is a standing thing in the controller (`Order`, `OrderOf`) until it is satisfied,
replaced, or a mission starts. Every order is also an invitation: a brother told to do
something is hanging out with you. Orders are refused inside a mission, a hold, a required
shared ride, or while a script owns him, the same gates a phone hangout has. A group
travel request from the phone resets them the way it resets the individual choices.

The pad binding is a hold of d-pad left, which is the radio wheel's button in a vehicle.
The hold threshold is what keeps a tap for the game, and the control is disabled only
once the strip is actually up. If that feels wrong in play, `ControllerOrders = False`
leaves the pad alone and the key still works.

## 4. Drive orders with a destination

A driving brother already went to the map waypoint when one was set and cruised when
none was; "Drive to my waypoint" is offered only when one exists, so telling him to
drive means somewhere. "Pull over" stops him at the wheel with the engine on, and "get
out" from the wheel stops the vehicle first. An ordered driver with the player not aboard
holds still unless the order is the waypoint drive.

## 5. Order echo

The strip echoes what he says back, in his color, for two and a half seconds: "Gohan:
taking the wheel". The same line goes to the phone's alert log as a crew order. A refused
order echoes "can't right now".

## M55: a way out, and security on every node

The three penthouses are the game's blimp interiors: apartments shown through the windows
from the air, with floors and walls and no door. Ron finished the job and could not leave.
Each brother's own arrival point is now his service elevator, a three-second interaction;
whichever one runs first puts everybody at street level at the foot of his own tower.
Street level is not authored, because nobody knows which side of a tower the sidewalk is
on: `M55SkylineDescent.StreetBelow` probes down from under the suite for whatever the
building stands on and asks the engine for the nearest sidewalk to that, in one tick, with
a fade over it.

Every suite has security. Ice's four are the authored count; Gohan and Guess each get three
and are asked to put their own down, so the terminal and the vault are taken under fire.

## The mission HUD

Ron: too big, taking up too much of the screen, the information solid. Every size in
`Core/MissionHud` is about two thirds of what it was: 400 wide instead of 560, text scales
from .18 to .24 instead of .22 to .30, bars five pixels tall instead of eight. A story test
holds the width and the scales down.

## Live questions

Nobody has seen any of this on a screen. Whether d-pad left is the right pad binding;
whether a companion sent to the driver's seat while the player shuffles across from the
passenger door arrives in time; whether `GET_SAFE_COORD_FOR_PED` answers sixty meters
under a tower before the player has been down there; whether the strip's text is legible
at Ron's resolution. `Bloodlines.log` records every order taken and every street exit
found.

## Checks

Build clean, 282 regression checks (43 new), 4,989 story checks (43 new), lint no errors,
0 of 1,091 flagged, mission map and story freshness verified. The dialect lint reports 15
findings in 7 files, all pre-existing and outside this change: bible extractions, the novel
generator scripts, the Cypher vehicle name and a pack inspector. <!-- dialect-ok: Cypher is the game's vehicle name -->
