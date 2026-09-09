# Campaign audit and desert expansion — September 9, 2026

This update reviews the existing 30 playable jobs and adds six first-pass gameplay scripts: M28–M30 and SM04–SM06. The remaining 43 jobs have expanded implementation/scene plans; they are not falsely exposed as playable missions.

## What changed

- Failed/aborted attempts offer a full restart of that specific mission. The debug menu retains the failure reason and exposes Retry last attempt. A fresh attempt resets mission police heat. Unsupported stage skipping is blocked because it cannot recreate the required world state.
- Objective teardown runs on pass, failure, abort and failed setup. One objective's cleanup exception no longer prevents the next objective from releasing its state. Camera cleanup and objective-route cleanup run on mission teardown.
- Cash, gold and base/fleet flags move out of intermediate stages into first-completion processing. A failed delivery or replay no longer repeatedly awards the haul. Foundry destruction is committed with M22 completion.
- Required-character instructions now agree between the persistent HUD and subtitle. Action objectives take priority over passive guard labels; nonempty speed/altitude rules remain visible. Mission descriptions in the menu describe the implemented gameplay rather than unavailable bible set pieces.
- Required crew deaths fail composed missions clearly. Nonlethal watchmen must survive until the mission ends, not only until the stun objective turns green. Missing work vehicles fail instead of leaving an interaction waiting forever.
- Mission-assigned drivers can drive toward distant work sites before disembarking. They do not immediately leave their vehicle to walk the whole journey. Dead combat targets no longer trigger the friendly-fire task reset intended for living teammates.
- Guess starts with the M16-style Service Carbine, Ice with an Assault Rifle, Gohan with a Bullpup Rifle. New earned Mk II locker tiers follow the bunker, charter and optional desert jobs.
- Free-roam health, armor and ammunition have separate per-character save records. Normal hotkey redeployment restores those records. Mission and debug deployments remain fresh spawns. Existing switch/recovery behavior remains authoritative during play.
- Weapon-availability scans are cached and ownership capture is spread across the three heroes. Logs retain a previous session/file and rotate at 4 MiB. Mission wrecks/corpses are released intact; map-marker and entity-release helpers tolerate native cleanup exceptions.
- Miller's M04 getaway and M30's approaching gunship receive short, skippable camera observations of actual moving actors. These do not move the brothers for framing. Existing split-position radio scenes remain in place.

## New playable block

| Mission | Play |
|---|---|
| M28 Off the Grid | Approach and clear the relay yard; Gohan connects and works on the splice while Ice handles two response squads; Guess extracts everyone in the Granger. |
| M29 Dust & Diesel | Clear a rail transfer depot, fill the road tanker, deliver the Phantom **and its specific attached trailer**, then stop and unload at the bunker. |
| M30 Redline Ridge | Drive the loaded Dubsta 6x6 down the marked canyon-road route under a Buzzard pursuit; passengers remain aboard; deliver and unload the parts. |
| SM04 Dead Drop Quarry | Ice eliminates two marksmen, physically visits each nest to collect its radio, then returns to the approach. |
| SM05 Black Box Estuary | Gohan reaches the monitoring buoy by dinghy, swims to its service harness, installs the interceptor and returns to shore. |
| SM06 Canyon Runner | Guess transports aviation fuel through the canyon under bike pursuit, keeps the correct tanker attached and unloads at McKenzie. |

Adaptations are reflected in the gameplay descriptions: M29 uses a road tanker at a rail depot, M30 uses a crew-capable 6x6, SM05 uses a dinghy and a surface-accessible service harness, and SM06 uses an actual tractor/trailer. These do not claim a driveable freight-train decoupling system, a custom weaponized kayak, or unsupplied interior geometry.

## Verification

Production compilation targets the pinned ScriptHookVDotNet 3.6 API used with the installed Enhanced runtime. Required mission lint, location validation, behavior/recovery tests, story/runtime tests and generated-document freshness checks are run for the release. The installation receipt records exact counts and the DLL hash.

The behavioral suites use GTA stand-ins. They verify role ownership, actual objective transitions, failure/retry construction, save isolation, one-time rewards, correct-trailer delivery, stationary unloading and camera/control release. They do not prove live physics or native streaming. Location checks are district-level and most coordinates remain estimates; new sites also require runtime safe-ground/water checks.

## When you can test again

1. Check each starting rifle, then damage two heroes differently. Switch between them; stand down and redeploy through the normal crew hotkey. Their injuries/armor and captured ammunition should stay separate. A mission restart deliberately supplies a fresh mission loadout.
2. Fail a side mission, read the reason in the debug menu, then retry. It should restart that exact job at the beginning with fresh actors and vehicles. Abort during dialogue too; camera, movement and mission markers should release.
3. Recheck M01–M03 and one nonlethal job. The original mission paths remain; the common HUD and failure handling changed. M15/SM02 now fail if a protected watchman is killed after being stunned.
4. After M27, play M28. Confirm the remote work, two response waves and all-three pickup. The new sites are estimates: if setup rejects a location, survey it instead of treating the failed safe-ground check as a successful playable placement.
5. In M29 and SM06, disconnect the trailer deliberately. The tractor alone must not complete delivery; reconnect the specific tanker and stop at the destination. Check trailer clearance on the live road route.
6. Check M30's gunship scene both watched and skipped. Control must return to Guess in the same vehicle, with Ice and Gohan still passengers.
7. Check SM04's two radio pickups and SM05's swimming/boarding. These need actual GTA movement and water behavior to validate.
8. Let the crew run for an extended session with several fail/retry cycles. Watch companion response, camera release, stale markers and log size. The previously working player-death and military response still deserve a short regression check.

No save reset is needed. Installation preserves user INIs, appearance, surveyed overrides and the existing campaign save. Source, prebuilt/package and installed DLLs are hash-verified; backups accompany the receipts.

Read PLAYABLE-MISSION-MAP.md for all 36 stage maps, PROGRESSION-GUIDE.md for exact unlocks/payouts, and CAMPAIGN-REMAINDER.md for the remaining 43 mission and scene treatments.
