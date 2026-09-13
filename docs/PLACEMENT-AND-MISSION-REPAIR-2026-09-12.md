# Placement and mission repair — September 12, 2026

This build combines the pending Foundry/M09/survey-tour update with the placement repairs and the latest M11, M23, M29 and starter-home requests. The story-to-play package list describes implementation history; it does not establish that the live staging works. The user's M23 test found real gaps despite P5a's merged status.

## Changes to test

| Area | Result |
| --- | --- |
| M08 | Both turbine crates move onto the open loading apron, with room between them and the flatbed. Crates are placed onto the ground before being frozen. |
| M09 | The rear convoy stop moves onto a verified road surface. Front trucks stop separately along the road heading. Ice and Gohan have separate editable starting keys instead of unsafe offsets from the vehicle destinations. Pickup and final helicopter landing use supported ground. This includes the pending convoy-progress/watchdog and helicopter-hold repair. |
| M10/M11 | Engine delivery now leads to a portable workshop on the Foundry's north service apron. The old shop coordinates were high on terrain, not at a usable shop. Flatbed, mechanic and arriving Gohan have separate positions. A visible tool bench supports the fabrication interaction. |
| M11 controller calibration | RT/W raises the pressure setpoint at 8 PSI per second; LT/S lowers it; release both to hold. Keep 20–30 PSI for eight cumulative seconds. Overshooting pauses earned progress. The HUD states the controls, pressure, target band and progress. This is a calibration simulation, not a claim that GTA models turbine manifold pressure. |
| M12/M18/M19/M21 | Water reference points move out from solid Terminal quay geometry. M19 still derives the actual sub, clamps and cargo positions from the loaded hull dimensions. M21's water landing marker and nearby road pickup now share the southern quay. |
| M13 | Three visible mission-owned fuel boats replace imaginary barge targets. The charges are worked from beside the hull. Watercraft, patrol entry and shore vehicle have separate positions; workers stand on the dry side of the quay. Each fuel boat requires a clear, sufficiently deep footprint. Destroying one before the charge stage fails the mission. |
| M22/M24 | The Alamo cache and later dredge reference share one real water site. The sampled cache footprint has at least six meters of water. The recovery truck has a separate dry pad; Gohan operates the cable from land. Ice and the response vehicles use the road above the bank. |
| M23 | The radar yard now contains a visible generator, workbench, empty fuel drum, marked vehicle bay and powered gate. Guess inspects those objects; Gohan works at the generator. The gate visibly slides open, including a defined skip result. Abort removes the temporary site; success releases it into the loaded world. Objectives and authored dialogue say the exterior yard is usable and underground rooms remain sealed. No enterable bunker interior is claimed. |
| M25 | The fight moves to the actual elevated railway span. The tanker, approach, narrow enemy formation and downstream boat are placed together. Jump instructions now say to deploy early and steer east to the river mouth; the old unverified “110 meters”/late-pull directions are removed. |
| M28 | The cover position moves off steep rock, and pickup/response heights are corrected. This is a placement repair, not the full P5b scene rebuild. |
| M29 | Two depot response vehicles join gradually after Guess takes the fueled rig. Drivers drive; passengers shoot. Enemies cease fire and drive away once 25% of the initial delivery route remains. Invalid route queries do not dismiss the pursuit. If the initial road-distance query is unavailable, distance-to-destination is the approximation used for that attempt. Abort cleans up the pursuit. |
| M30 | The unsurveyed start moves onto a road approach. The user's saved Bend and Exit stay unchanged. |
| SM04/SM06 | The quarry approach and two nests move onto connected quarry service-track sites instead of floating roughly 90 meters above the map. SM06's bend uses supported road geometry. Full P5c scene/role treatment is still pending. |
| Guess's first home | The starter residence returns to the original modest house shell, using `Apartment.Starter.Interior.Guess` and `Apartment.Room.Guess`. Ice retains his studio and Gohan his one-bedroom. The street entrance stays at Mission Row; purchased luxury tiers are unchanged. |
| Pending features included | Foundry industrial hideout and weapon services; persistent placement menu; sequential visit/edit/save-and-next tour. See `M09-FOUNDRY-SURVEY-2026-09-12.md`. |

## Geometry evidence and protected work

69 authored location keys were changed or added. This includes 11 new independently editable role/work positions. M01–M07 and every location protected by the original saved-survey audit were kept unchanged. Personal survey and configuration files are not replaced during installation, so a saved override still takes precedence over a corrected default.

CodeWalker was actually run against the installed GTA V Enhanced archives: source commit `485d56bec00262ed7fa472261cce7bbc6202b96e`, selected map DLC `mp2026_01_g9ec`. This repair used 1,665 additional candidate/final queries after the earlier audit. All recorded queries completed; the runs reported no asset-reader errors. Final checks cover changed anchors and selected vehicle/boat footprints. The first fuel boat's original candidate failed footprint clearance against a floating dock; the accepted replacement is at (-875, -1435), with at least 4.15m depth at the nine tested footprint points.

The local, non-distributed geometry cache is under `build/codewalker-repair-2026-09-12/`. `changed-locations.tsv` records before/after defaults. Raw map data and CodeWalker binaries are excluded from the release package.

## What still requires a live pass

- **Water exits:** M13 and M21 have supported water and quay positions, but collision rays cannot prove that the player or companion AI can climb out and reach the car. Test boarding before treating either route as complete.
- **M11 workshop:** check vehicle doors, tool animation and room to walk around the Granger. The shop is an exterior work area.
- **M23:** verify gate travel, generator interaction, each inspection bay, enemy cover, and camera watch/skip. The released exterior props are not a persistent bunker-interior system across game restarts.
- **M25:** verify rail-deck navigation, aiming lines and parachute reach to the boat. Static geometry establishes the span and water, not AI pathfinding or parachute physics.
- **Saved surveys:** M05's saved boat/grotto points were shallow in the static audit; M07's platform centers were supported. These remain the user's placements and were not overwritten. Review recommendations are in `CODEWALKER-PLACEMENT-AUDIT-2026-09-12.md`.
- **Other audit flags:** SM02 roof/fire-escape routing, SM03 race access between road levels, other estimated garage doors, and interior furniture positions still need live layout work. Their flags were not turned into speculative automatic coordinate changes.

Build, automated objective execution and map collision checks are separate evidence. None is a claim that the entire campaign was played in GTA.

## Story-to-play follow-through

P1–P5a have scripts, but their live acceptance pass remains open. P5b/P5c still have first-pass mission scripts with selected repairs, not a completed scene/role rebuild. M31–M70 and SM07–SM09 remain outside this implementation.

For expanding M11 next, keep it a quiet character mission: let Guess select and install a cooling setup, let Ice perform this accessible calibration, then give Guess a short road test and a return inspection. Have Gohan verify the Berth 44 lead during the return. Award the upgrade only after the test succeeds. These are proposed additions, not implemented stages in this build.
