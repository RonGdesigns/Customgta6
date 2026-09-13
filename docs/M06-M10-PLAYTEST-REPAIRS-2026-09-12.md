# M06 and M08-M10 playtest repairs

This package includes the pending nitrous, shop garage saving, M05 pursuit, P5c solo missions and Foundry IPL repair. Source/build completion is distinct from installation and live validation; check the installation receipt before retesting.

## M06 - Clean Sweep

The extra cyan work marker identified in the script belongs to Gohan's thermite task, not a second Ice destination. Assigned-work markers now display only when controlling the worker. Ice holds the alley while Gohan works.

The later ground reinforcements approach on foot through both alley mouths. Existing M06.ConvoySpawn1/2 and ConvoyStop1/2 surveys remain intact and act as foot entry/destination pairs where sufficiently close and on the alley level. Distant entries fall back to nearby approach points. Road trucks no longer determine whether a required wave reaches Ice. Foot orders refresh and explicitly target Ice inside the alley; unseen stragglers can recover to the entry after 25 seconds. The existing helicopter/rappel component remains.

## M08 - Supply & Sever

- Inactive brothers return fire against live mission threats while preserving the forklift driver's task when switching away.
- The default flatbed is placed 14m ahead of the actual forklift spawn on its side of the apron. The reference coordinate changes to 248, -2952, 6; manual HaulerSpawn surveys still take precedence. This proximity is an implementation choice, not a verified obstacle-free driving route.
- Drive the forklift under either assigned crate and stop for one second. The crate follows the forks visually with collision disabled and no forklift physics attachment. Drive it yourself to the marked rear of the flatbed and stop for two seconds. Both vehicles must remain stopped and Guess must be driving. Attachment is verified before counting the crate.
- There is no required loading/chain cutscene. The technical warning is radio, so incoming fire does not trap the player in a camera. Finish the second delivery or the technical fight first; both orders work.
- The camera loop is seven minutes to accommodate both player-driven deliveries. The objective text gives the current pickup or delivery action. Missing assets or a rejected cargo attachment fail clearly without counting the load.

## M09 - Rolling Thunder

The convoy drives through the ambush instead of stopping beside Ice. After switching to Ice, a nearby escort starts a single 0.3-speed shot window for 4.5 game seconds (about 15 real seconds). The escort remains destructible and its driver is the target. The window ends on expiry, the driver shot, switching away or mission cleanup. Character-wheel restoration also drops the expired mission slowdown.

The IFF prop is pocketed after its read so it no longer remains in Ice's hand during boarding. The landed Frogger is unlocked and the pilot is kept seated without reissuing the previous overhead hover. Ice has a passenger-seat context-entry option (E / D-pad Right) alongside ordinary vehicle entry. This does not warp him into the helicopter.

## M10 - Open Throttle

The loaded, unoccupied M08 flatbed is reused when it is still at the stash with its two attached turbine crates. Otherwise a fresh truck is placed upright and held for collision streaming. New crates disable collision before attachment, preventing cargo from rolling the truck. No occupied vehicle is taken for reuse.

Cartel riders spawn on same-level nearby road nodes, seat synchronously, start their engines, and receive pursuit orders after seating. Orders refresh every three seconds. A wholly failed pursuit spawn fails with a retry message rather than letting an empty wave count as a completed chase.

The gunship spawns above the terrain, starts its engine and rotors, verifies the pilot seat and receives a helicopter attack task. Its task refreshes through the encounter. Pilot/aircraft creation failures do not silently become a destroyed-helicopter objective.

## Verification and next live pass

The Windows build, story/runtime suite, regression suite, parser checks, mission lint, location audit and generated-document freshness checks must pass before promotion. These use stand-ins and cannot verify map collision, native pilot behavior, aiming visibility or door animations in GTA.

1. M06: remain as Ice after entering the alley. Confirm the thermite marker is absent and both foot approaches reach the fight. Complete all waves and extract.
2. M08: let a sentry start shooting and confirm the other brothers respond. Drive each crate to the flatbed with no camera takeover. Switch to Ice during the technical fight, then back to Guess. Test the two task orders and a full restart.
3. M09: track the convoy and switch to Ice. Confirm the aiming slowdown and a moving driver. Open/close the character wheel and retry to check normal-time recovery. Collect the unit, land and enter the Frogger normally or with the passenger prompt.
4. M10: enter from M08's persistent stash and also from a fresh debug attempt. Check the truck stays upright, the bikes pursue from street level, and the gunship flies and fights before reaching the tunnel.

Save data, appearance and manual survey/config files are preserved. No mission rewards, early progression gates or free-roam crime settings are changed by this repair pass.
