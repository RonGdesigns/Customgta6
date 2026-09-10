# Visuals and vehicle damage: what is on, what it costs, how to check it

Two native systems, both configured in `scripts\Bloodlines\Bloodlines.ini`, both
restored on stand-down and on a script reload. Neither downloads anything, neither
touches Rockstar's files, and neither is free of cost across the board. This page
says which parts are.

## Vehicle damage (`[VehicleDamage]`)

| Key | What it does | Where it lives |
| --- | --- | --- |
| `DeformationMultiplier` | Deeper crumple. Cars take the full value, SUVs and vans about 80% of the increase, trucks about 70%. | The model's shared handling, through the same ownership-and-restore path as the road profile. |
| `CollisionDamageMultiplier`, `EngineDamageMultiplier` | Softer body and engine damage on cars and SUVs; trucks keep stock. | Shared handling. |
| `CrewProtectionMultiplier` | Damage scale on the vehicle a brother is sitting in, and the player's own vehicle-damage modifier while they drive it. Removed when the car is empty of them. | Per vehicle instance, never the shared handling. |

This is a gameplay change. Crashes hurt less, so chases are easier. Heavy
deformation is physical: a badly bent car can pull to one side, which matters at
this project's 2x speeds. If Guess's chase car in M04 crabs after a front hit, lower
`DeformationMultiplier` before anything else.

## Visual atmosphere (`[Visuals]`)

| Key | What it does | Cost |
| --- | --- | --- |
| `DeSmog`, `ContrastStrength`, `Preset` | A timecycle modifier per time band: day (10:00 to 17:00), dusk (17:00 to 20:30), night (20:30 to 05:30), dawn. Released during scenes and inside the apartment. | None. |
| `DayModifier`, `DawnModifier`, `DuskModifier`, `NightModifier` | Override the modifier name for a band. Empty uses the preset's default. | None. |
| `RemoveBlur` | Camera motion blur and distance blur off, every frame. | None. |
| `WaterReflections` | Maximum water reflection distance on the player and the car they are in. | Small. |
| `OceanSwell` | Deeper ocean swell in free roam. Off during any mission and the prologue, so boat objectives keep their authored water. | None. |
| `ShadowDistanceScale` | Longer shadow cascades. | GPU. |
| `HeadlightShadows` | Headlights cast dynamic shadows at night. | GPU. |
| `LODBoost`, `LODScale` | Longer level-of-detail range: a per-frame scene override plus vehicle and ped multipliers, capped at 2.0. | Streaming. At 2x speeds this is the first key to lower if the game stutters or collision loads late at a mission start. |

There is no "ceramic clearcoat" key. The native that earlier notes proposed for it
controls the burnt-and-faded paint overlay, which the garage already uses to reset a
car's finish. No public native raises paint reflection strength, so the key was
removed rather than left doing the wrong thing.

## Verifying a timecycle modifier name

A modifier name the game does not know applies nothing and says nothing. The four
defaults in this build (`cinema_default`, `New_Chinatown_sky`, `color_neutral`,
`rply_saturation`, `cinema`) came from a research note, not from the game data, and
have not been checked. Before judging the grading:

1. In OpenIV, open `update\update.rpf\common\data\timecycle\` and the
   `timecycle_mods_*.xml` files (also `common.rpf\data\timecycle\` on older data).
2. Search each file for the name. The `<timecycle_modifier name="...">` entries are
   the only names that work.
3. Put a name that exists into the matching `*Modifier` key in `Bloodlines.ini` and
   reload with Insert. `Bloodlines.log` prints `Visuals: timecycle modifier '...'`
   each time a band changes, so you can see exactly what was requested.

## Night lighting and display settings (player-side, not shipped)

`visualsettings.dat` is Rockstar's file and stays out of this repository. If you
want brighter headlights and coronas at night, edit your own copy with OpenIV and
raise `car.headlight.intensity`, `car.taillight.intensity` and
`misc.coronas.intensity` a step at a time. Keep a backup; the game's own updater
may replace the file.

In the game's own graphics settings, for this build:

- Frame scaling 1.25x or 1.50x if the GPU has headroom. This is the game's own
  supersampling and does more for edge clarity than any post-process.
- Anisotropic filtering 16x.
- Motion blur 0%, to match `RemoveBlur`.
- Shadow quality very high with soft shadows on.

## What to log after a visuals session

- Which time bands looked right and which looked wrong, with the modifier names from
  the log.
- Whether stand-down (`F10`) and a reload (Insert) returned the world to vanilla: no
  lingering grade, shadow reach or swell.
- Frame time at speed on the Del Perro Freeway with `LODBoost` on and off.
- Any mission start where collision loaded late; note whether `LODBoost` was on.
