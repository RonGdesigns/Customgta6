# Vehicle and between-mission expansion

September 8, 2026. Prepared while the previous build is being tested. This package is staged, not installed into the running game.

## Vehicles

The vehicle menu now has 74 entries in six categories: 40 cars, 8 motorcycles, 10 off-road vehicles, 6 boats, 6 helicopters and 4 planes. The first 24 DLC cars remain. Additional examples include Sultan RS, Elegy Retro Custom, Banshee 900R, Gauntlet Hellfire, Hakuchou Drag, Shotaro, Shinobi, Kamacho, Nightshark, Insurgent, Longfin, SuperVolito, Swift, Vestra and Nimbus.

Only models available in the installed game are shown. Some entries are base-game specialty vehicles, not Online exclusives. The separate vehicle catalog lists every choice. Cars and motorcycles use nearby road positions. Boats require a clear water footprint within 75 meters of the player. Helicopters require a broad, flat open area and start grounded with engines off. Planes are requested near the Sandy Shores or McKenzie runway and start with engines off. These placement checks still need live testing for terrain, depth and nearby obstacles.

Requests remain capped at four retained vehicles. Release parked vehicles frees unoccupied requests without deleting an occupied vehicle. Vehicles are not added to random traffic or saved as personal garage cars across restarts in this update.

## Home workbenches

At the active character's home marker, press E / D-pad Right to open the home menu. Choose rest/save, the personal workbench, or crew messages. The menu uses the same controller navigation as the debugger. Rest no longer starts immediately when you press the home interaction button.

- Ice's bench restocks his unlocked weapons and replaces armor without advancing time.
- Gohan's workstation marks the next genuinely available playable mission, respecting completion and prerequisites. It does not claim to disable an unimplemented police-camera network.
- Guess's chop bay repairs a parked car or motorcycle within 15 meters. It refuses distant or moving vehicles. The unlocked turbine upgrade now supports both the original Granger and the DLC Granger 3600LX, and tuning indices are limited to the model's available parts. Sit in the Granger for the fleet upgrade to apply.

Home use is blocked during missions, scenes, recovery, survey, active pursuit or combat. Each hero uses their own home. Custom furnished interiors, advanced ammunition crafting and saved personal garages remain future additions.

## Crew messages and news

Eleven authored messages react to M01, M02, M03, M05, M06, M11, M15, M22, M23, M27 and SM03. They carry the relationship story between jobs: the recording after the dockyard, three keys at the foundry, the loss of Cypress, and Ice's reaction after the plane rescue. KJ follows up after Guess's solo race. One Weazel News bulletin reports the industrial incident.

The first eligible notification waits at least 45 seconds after loading. Further notifications are spaced by at least 90 seconds. They wait through missions, dialogue, menus, combat and police pursuits. Delivery history is saved in readDispatches so messages do not repeat every reload. The inbox remains readable from the home or debug menu. Completing a job reveals its corresponding message; unfinished missions do not reveal spoilers. Existing completed jobs can supply pending messages. A campaign reset clears delivery history.

This is an in-game notification and inbox system, not a replacement for GTA's full phone application or an audio news broadcast.

## Mission clarity follow-up

The reported Ice handoff was M03: Cypress Foundry. Its former "breach the gate" instruction was a walk-to-zone objective, not an ability or button interaction. The new objective and dialogue say to walk into the yellow ENTRY marker without using an ability, then clear the red-marked guards. Arrival markers now default to yellow to agree with the GPS instructions. Arrival objectives announce their instruction when the required hero takes control. Kill-target objectives retain the remaining target count in the persistent objective text.

M06: Clean Sweep receives the same explicit entrance wording. Its burn/defense stage is assigned to Ice: defeat the marked SWAT while Gohan works independently. The crew only extracts after both the work and waves finish.

M01 retains the installed split positions, western Ice approach, laptop/technician scene, seated Guess and recognition without regrouping. The quiet approach now names the character who blows the cover by firing. Regression checks specifically verify that Ice firing fails the quiet approach and recognition preserves the separated cast and driver's seat. The complete M01 and M03 flows pass the automated harness; actual visibility, vehicle routes and animation still require live testing.

## Retest after installation

1. Open Vehicles and browse each category. Request a bike beside a road, a boat beside open water, a helicopter on open ground, and a plane near a supported runway. Try unsuitable locations and confirm no misplaced vehicle is created.
2. Visit each home. Open the menu with E / D-pad Right and check controller navigation, resting, workbench actions and return to player control.
3. Check that Guess cannot repair a vehicle from across the map and that Gohan routes to an available mission.
4. After completing a relevant job, leave combat and wait for the follow-up. Read it in the inbox, reload, and verify it remains in history without repeated notification.
5. Replay M03: complete Guess's rail task, switch to Ice, enter the yellow ENTRY marker without using his ability, then clear the red targets. Check the remaining count.
6. Replay M01 once without shooting before recognition, and once firing as Ice during the quiet approach. Confirm the former preserves positions and the latter reports blown cover.
7. Continue the previous military crash retest separately; this expansion does not establish the cause or resolution of that engine failure.

## Verification

The 90-source production DLL compiles. 287 story/runtime checks, 85 behavior/recovery checks and 3 parser tests pass: 375 total. The added checks exercise water/airfield placement restrictions, home actions, message prerequisites, suppression, cooldown, save history and mission guidance. GTA stand-ins cannot verify live model rendering, native stability or pathfinding.

All 50 added model names were checked against the cached vehicle reference list. Mission lint reports no errors. District validation flags 0/126 locations. The existing M01 legacy recognition cue exception remains intentional.

The update archive contains the DLL, revised mission dialogue and documentation. Game files, saved progress and settings stay untouched while the current test is running.
