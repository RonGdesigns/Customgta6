# Garage and M05 water geometry evidence

The local read-only CodeWalker audit used the installed Enhanced RPF collision files and vehicle road nodes. No game archives were modified or copied into the package.

The detailed results are in untracked `build/codewalker-garages-2026-09-12`, `build/codewalker-garages-more-2026-09-12`, `build/codewalker-water-final-2026-09-12` and `build/codewalker-escape-2026-09-12`. `build/garage-placement-changes.json` records the selected property probes. Property choices require a complete floor hit, a near-horizontal surface, small neighboring height spread and no roof immediately over the probe. The purchase point is separate from the vehicle road node. These checks establish suitable geometry, not visual verification of a particular garage door.

Elgin/Pillbox: the old marker at (250,-400,44) sat below a floor/structure. The new pedestrian point is (258,-404,45.276). Popular Street: the old point was on road collision; the new point is (805,-1021,26.072), on the neighboring industrial property. The crew-car stash moves to (858,-2116,30.664) to avoid the roof at the old spot. All source rows marked surveyed remain unchanged.

M05: the initial estimated offshore point intersected an island and the old capture point was inland. Seven chase waypoints now make a loop through open water. The first escape leg uses (2600,-1300), avoiding the rock found on the rejected southbound leg. Guess approaches through (2700,-1200), (2700,-1300), then (2600,-1300), avoiding the rejected direct diagonal. The final capture area is (2700,-1200). The surveyed target boat position remains unchanged and is shallow; its starting clearance still needs the live boat test. Wave motion, native steering and dynamic objects are not simulated by the collision reader.

Foundry: the installed clubhouse YMAP reports internal name `bkr_biker_interior_placement_interior_1_biker_dlc_int_02_milo_`, hash 4068535654. The old request without the trailing underscore hashes to 3834780676. The source now requests the actual installed name; the regular loading protections remain in effect.

## Foundry runtime correction, September 12

The earlier inference that REQUEST_IPL accepts the internal YMAP name was incorrect. The live log timed out with interior=0 for the underscore request. The Foundry now requests the registered runtime IPL without that final underscore, matching the [primary clubhouse loader](https://github.com/Bob74/bob74_ipl/blob/master/dlc_bikers/clubhouse2.lua). The internal map hash above is archival evidence, not a runtime registration. Furniture variants, collision checks and rollback remain enabled. Live entry still requires a retest.
