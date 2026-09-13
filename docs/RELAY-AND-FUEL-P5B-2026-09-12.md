# P5b: the relay, fuel, and receiver

September 12, 2026. Implementation and automated checks complete; GTA play acceptance pending. This package advances M28-M30 in STORY-TO-PLAY-PLAN.md. P5c (SM04-SM06 scene and role treatment) is next. M31-M70 and SM07-SM09 still have no runtime scripts.

## Mission flow

| Mission | What the player does | What the world shows | Completion and failure |
| --- | --- | --- | --- |
| M28 Off the Grid | Gohan approaches; Ice clears the yard; Gohan chooses the feed, dispatch, or cameras, then connects the surge case. Ice covers the continuing splice through two response teams. Guess collects everyone in the Granger and returns. | A laptop on a worktable and the physical surge case. The approach frames separated actors without gathering them. Feed gives 12-second response gaps; dispatch gives smaller squads with 3-second gaps; cameras gives searching guards with 6-second gaps. Ice reports the arriving team. | The connected case is verified in the finish scene. A missing/destroyed panel, case, extraction vehicle, or brother fails the attempt. An unconnected case cannot be skipped into a successful splice. All three must board the extraction vehicle. |
| M29 Dust & Diesel | Guess identifies the rig; Ice clears the guards and runs the remote transfer controls for 12 seconds; Guess takes the tractor and delivers the coupled tanker. Stop, unload, and receive the reserve drums. | The actual tractor, tanker, and laptop control table in the opening. Guess stages near the truck while Gohan watches the approach. Two staggered pursuit vehicles have armed passengers and unarmed drivers. They withdraw with 25% of the initial route remaining. Fuel drums appear in the receiving bay. | Loading does not bank fuel. Final receipt requires the correct tanker still attached, both vehicles in the unloading area and stopped, and the receiving drums present. Destroying either part of the rig fails. Canceling the receipt scene awards nothing. |
| M30 Redline Ridge | Guess follows the road bend and canyon exit with a secured satellite case, then parks at the radar yard. The case transfers to a worktable. Switch to Gohan, get out, and use the receiver laptop for six seconds. | The actual Dubsta 6x6 uses its authored heading; Ice and Gohan stay seated in its introduction. The case remains attached during the drive. Gunship warnings are radio; its red marker disappears when it withdraws at the canyon exit. The same case reaches the table, then Gohan checks reception. | A lost truck, detached/lost case, missing receiver, or downed brother fails. Receipt and fitting are separate from delivery. Watching or skipping the final scene verifies the case at the bench before completion. Canceling does not award reception. |

The scenes use the existing camera, inspection, work-animation and prop-transfer steps. The transfer is a staged move between carriers, not a new bespoke lifting animation. No voice recordings were added.

## Progression

Existing first-completion rewards remain $90,000 for M28, $100,000 for M29, and $110,000 for M30. The relay, fuel-reserve, and satellite-parts upgrade flags remain the existing campaign rewards. M28 already gates the selectable SM04-SM06 missions; this package does not present those solos as having received their P5c rebuild.

M29 records bunker fuel at M23.BayTwo after verified success. M30 records satellite parts at M23.BayOne after delivery and fitting. These records establish custody for later work; they do not implement new offshore missions, access codes, a real-time surveillance screen, or future defenses. The reception scene explains that distinction.

## Retry and placement limits

Failure, abort, or death uses the existing full-mission restart. No checkpoint restoration is claimed. Temporary equipment and task ownership are cleaned up; canceled scenes do not complete physical steps. Received equipment can remain in the current game world after success. Save continuity is the cargo/upgrade record; permanent interior reconstruction is separate work.

No location rows or personal survey overrides were changed in this package, including the protected M01-M07 placements and M30's surveyed route. Props use existing work locations and model bounds for tabletop/roof heights. Receiving equipment is separated from M23's original bench and drum; nearby matching service props are reused on repeat deliveries. Live verification is still required for prop clearance, character navigation, camera sightlines, and the entire M30 road route.

## Validation and next play pass

The package compiles 146 production sources against the pinned SHVDN3 3.6.0 API. 1,997 story/runtime checks, 187 regression checks, and three dialogue-parser checks pass. The story suite includes 48 new checks covering natural and skipped scene outcomes, cancellation, absent connections, a detached tanker, detached satellite cargo, seat/position preservation, and success-only cargo records. The existing full campaign flow harness still completes all 30 later/solo mission scripts through their objectives. Mission lint reports no errors; location audit reports 0 of 230 flagged; generated story and mission maps are current. These are stand-in checks, not GTA play results.

1. M28: try each technical choice on separate attempts; confirm the response differs and all three reach the Granger. Inspect the table and connected case.
2. M29: confirm filling is readable, both pursuit cars join, and the last quarter is quiet. Detach before unloading and confirm receipt refuses until recoupled.
3. M30: inspect the truck heading and secured case, drive the whole bend/exit route, and verify the gunship warning preserves control. At the yard, switch to Gohan, leave the truck, and fit the receiver at the table.
4. Watch one set of action scenes and skip another. Confirm both leave the same equipment, passenger seats, camera, and walking control. Abort once and retry from the beginning.
