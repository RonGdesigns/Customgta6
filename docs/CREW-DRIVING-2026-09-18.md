# Crew driving, September 18, 2026

Ron: "make them drive a little bit faster and a little bit better and with a little bit
more intent. And if I tell them take the wheel they should just drive when police is on
them and you're not the driver. Have them drive like they have intent."

Three things, all in `Crew/CrewDriving` and `Crew/CompanionDriver`.

## Faster: the speed is the car's, not a constant

Every brother was told a flat number of meters per second: Guess 50 or 60, Ice 42 or 50,
Gohan 40 or 48, the second of each pair under pressure. Those were set when every car ran
to its stock redline. `WorldTuning` lifts every ceiling in the world to twice that now,
so a brother holding sixty in a car that will do a hundred and twenty looks like a man
who is not trying, and beside a player who can do the hundred and twenty he is one.

The commanded speed is a share of the car's own capability now, read with
`GET_VEHICLE_ESTIMATED_MAX_SPEED` so fitted parts count. Guess holds 95% of it under
pressure and 70% otherwise; Ice 85 and 60; Gohan 80 and 55. The old flat numbers are kept
as a floor, so a slow car never has him crawling, and a car the engine will not report a
speed for gets the floor. This is the same rule the SM03 rivals were given the day before,
for the same reason.

## Better: the escape style, and commitment in the corners

`TrafficFlags` is the everyday style: swerve around traffic, peds and objects, change
lanes around obstructions, keep road pathfinding. `EscapeFlags` adds the two things a
getaway needs and a commute does not: the wrong side of the road is allowed and the map's
shortcut links are used. A driver with the police on him who will not cross a median is
not escaping. The escape style is used whenever the drive is urgent: a wanted level with
the player aboard, or a fight.

`SET_DRIVER_RACING_MODIFIER` is the engine's own dial for how hard an AI driver commits
to a corner; at zero he brakes for every bend the way traffic does. Under pressure Guess
gets the full modifier, Ice 0.7, Gohan 0.6; on a commute it is off. Aggressiveness rises
with it. Ability was already at the engine's maximum.

## Intent: the getaway is the engine's flee mission, aimed at the nearest officer

The old urgent behavior with nowhere to be was "drive to a point 700 meters ahead on the
road, then pick another." That is a direction, not an escape: it has no idea where the
police are.

Now, with the police on the car, the player aboard in any seat but the wheel, and no
mission destination or map waypoint to drive to, the driver finds the nearest living
officer within `PoliceSearchMeters` and is given the engine's `Flee` vehicle mission
against him, at escape pace, in the escape style, wrong-way driving permitted. The flee
mission is what the game's own fleeing drivers use: it picks a direction away from the
threat and keeps picking it.

Four rules it keeps:

 * **A flee task is held, not re-issued.** Re-tasking restarts the drive task and makes
   the driver hesitate, the rule `PreparationOperation.KeepDriving` already keeps. The
   task stands for `FleeRefreshMs` against the same officer; once that is up, a nearer
   officer becomes the one he flees.
 * **A destination still wins.** A mission sending him somewhere, or a map waypoint the
   player set, is driven to under pursuit rather than fled from, at escape pace and in the
   escape style. "Drive to my waypoint" with the police on you means exactly that.
 * **Nobody aboard, nobody to get away.** With the player out of the car the driver does
   not flee on his own account.
 * **Heat gone, the task is replaced.** A flee task left running against an officer who
   has lost interest is a driver still fleeing an empty road; the moment the getaway
   condition drops, whatever comes next is issued fresh.

When the police are on the car but no officer is within range yet, the old drive-ahead
behavior still applies, in the escape style and at escape pace, until one is.

## What did not change

The order strip from the day before already puts a brother at the wheel; this is what he
does once he is there. The convoy, the free-roam commute and every mission driver use the
same speed rule and the same style choice, so all of them are a little quicker and a
little more committed when things are urgent. Nothing here writes the power or the entity
speed cap, which `WorldTuning` owns per car.

## Live questions

Nobody has driven behind this. Whether `Flee` against a single officer reads as a getaway
when a whole response is on the car, or whether the driver should flee the wanted center
instead; whether wrong-way driving in the escape style is exciting or alarming with the
player in the gun seat; and whether 95% of the car's capability is too much for Guess on
city streets, which is one constant. `Bloodlines.log` records every flee task issued.

## Checks

Build clean, 301 regression checks (19 new), 4,989 story checks, lint no errors, 0 of
1,091 flagged, mission map and story freshness verified, dialect at its baseline.
