# Solo and harbor playtest repairs — September 13, 2026

This package retains 49 playable missions. It updates the first three solos and
M12–M18, shared mission boarding, Foundry entry and the shop display. It includes
the separately installed phone-text repair. No new campaign block is added.

## Mission changes

| Mission | Revised play |
|---|---|
| Ice solo, SM01 | Six separate guard positions around the warehouse. Spawn validation rejects nearby solid map/prop space instead of accepting a ped inside a crate. |
| Gohan solo, SM02 | A marked street entrance provides a faded maintenance-stair transition to the rooftop. Terminal and guards share a checked roof level. Use the roof access to descend before the trace expires. Failed streaming restores Gohan to the entrance and releases the fade/freeze. |
| Guess/KJ solo, SM03 | Bring an owned road car; buy one if needed, then retrieve it or call KJ through the Garage app. One northbound sprint follows 278 ordered street/freeway/trail gates to Chiliad's summit, about 19.5 km. Both rivals use the same route. No race cargo or post-race ambush. Keep the personal car and existing $25,000/transmission reward; cleanup does not delete it. |
| M12 | Two armed launches hunt Gohan after the hull scan. Return the sub to the jetty, switch freely and clear both boat crews before the final debrief. The pursuit warning uses radio so a camera does not interrupt escape. |
| M13 | Three tugs are spread along the channel. Their anchors hold position while buoyancy settles the hulls to the waterline; they are not artificially frozen above the water or blindly submerged five meters. Two armed boats pursue Ice. Board Guess's car before detonating. |
| M14 | HUD shows actual height and the 50-meter limit **above terrain**, with the existing six-second warning grace. This is not altitude above sea level. |
| M15 | All three arrive at the requested (1084.16, -3035.12, 5.53), heading 356.5°. Ice and Gohan exit; Guess waits for extraction. Guards have separate checked positions. After the splice, inactive brothers run back and board. Both must be aboard before completion; switching is free during extraction. |
| M16 | Gohan blacks out the base before the gate over radio. Guard spawns reject solid space. Armor does not start until the cleared-hangar pickup step: first response after 14 seconds, second at least 16 seconds later, with occupied-spawn checks. The blackout remains through delivery; local departure countermeasures protect the Cargobob from the independent vanilla base missiles. Abort/end restores the wanted ceiling, targeting and lighting. |
| M17 | After fitting the release, Gohan receives an explicit objective to board and collect the floating Kraken before the debrief. |
| M18 | Ground the hauler before holding it in place. Guess collects the jammer box, then walks to the helicopter rear to install it. Ice collects the launcher gear, then loads it at the truck rear. Collection alone no longer records a completed installation. |

## Shared systems

- Mission boarding accepts normal F/Y and the context button. For a passenger
  objective it chooses a free passenger seat and keeps the crew driver seated.
  It uses the door animation, rejects moving vehicles and throttles repeated
  entry requests. A driver-only objective still requires the driver seat.
- Foundry entry requests registration of multiplayer DLC interiors in Story
  Mode before requesting the biker clubhouse IPL. The guarded interior loading,
  collision checks and safe rollback remain. This does not join GTA Online.
- Car shops show native top-speed, acceleration, braking and traction ratings,
  with gain/loss bars against the pre-preview build. Cosmetic changes may have
  no performance effect. Weapons show native HUD ratings, fitted components and
  the selected component's rating delta; these are not measured DPS values.
- Recent purchase receipts occupy a bounded side panel. The normal notification
  feed is hidden while shopping so rapid purchases cannot cover menu rows.
- Phone art and text now share the external sprite layer, with a bounded glyph
  cache and a fully native readable fallback if font assets are missing.

## Placement evidence and limitations

Local CodeWalker collision probes informed warehouse guards, the annex roof and
street entrance, port watchmen and hangar guards. A connected, directed vehicle
node route informed the sprint gates, preserving bridge/tunnel elevations.
Raw extracted map/collision/road files remain local and are not packaged.

There are 790 authored placements and zero district/bounds flags. That static
check does **not** certify every position against live streamed geometry. Runtime
marine footprint/depth checks and actor clearance checks still apply. Existing
saved surveys/settings are preserved; no saved override was found for the
specific existing keys revised in this pass.

## Validation and next live pass

Automated walkthroughs cover all 49 mission classes, including the new collection,
boarding and extraction stages. Targeted tests cover passenger-seat selection,
repeat entry input, failed roof streaming, staged armor timing, tug buoyancy and
two pursuit boats. Tests use GTA stand-ins; physics, navigation and DLC streaming
still need the game.

Suggested live order:

1. Phone labels/details, then Foundry entry and exit.
2. Cosmetic preview/cancel/purchase and rapid ammo purchases; inspect the side panel.
3. Ice/Gohan solos; KJ sprint in a personally owned car, with both rivals following.
4. M12 pursuit and free switching; M13 tug waterline, pursuit and rear-seat boarding.
5. M15 arrival and full crew extraction; M16 delayed tanks and blackout departure.
6. M17 collect the Kraken; M18 collect each box, then install/load at the vehicle.

If a placement remains wrong, report its survey key and the live position. Saved
captures remain authoritative; runtime safety can reject blocked or shallow sites.
