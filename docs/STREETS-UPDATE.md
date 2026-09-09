# Shops, pursuit and mission handoffs

Implemented and compiled for the installed Enhanced/SHVDN setup. Automated checks use stand-ins; physical doors, vehicle handling, police tactics and fire damage still require a GTA playtest.

## Shops

Eleven Ammu-Nation sites, the four Los Santos Customs sites, Beeker's Garage and **Guess Customs in Strawberry** now have short-range map icons and service markers. Approach a marker in free roam with no wanted level and press **E / D-pad Right**. Use the D-pad and A/B, or arrow keys and Enter/Backspace. The shop works even with the debug menu disabled. Leaving the service area, starting a mission or becoming wanted invalidates further purchases.

Nearby, recognized shop doors are unlocked while the shop is available. Existing registered garage doors are asked to open. Original door states are restored when leaving. This does not unlock unrelated buildings or install missing interiors. Door models and coordinates need live confirmation; the service menu also works near the entrance if the vanilla shop script does not recognize a custom protagonist. The shop presentation is Bloodlines' menu, not Rockstar's clerk/camera sequence.

Purchases use the shared **crew cash** balance in the mod save. Weapons belong to the purchasing hero and enter their personal locker. A new save starts with its existing balance; the first large cash payout remains M05. Home locker restocking remains available before that payout. Unsupported weapons/parts, already-owned weapons, failed applications and insufficient funds do not charge money.

| Item | Regular price |
|---|---:|
| Equipped-weapon ammunition | $60 / 60 rounds; heavy/thrown packs contain 3 |
| Armor to 100 | $250 |
| Stock firearms | $1,500 |
| DLC firearms | $6,000 |
| Mk II firearms | $12,000 |
| Special weapons | $25,000 |
| Vehicle repair | $500 |
| Paint, primary and secondary | $400 |
| Compatible body/wheel/brake/suspension/armor part | $1,000 |

**Guess Customs** is the crew's service bay at the Strawberry workshop, separate from Guess's home and the M11 story set. Parts and paint cost half price. Guess repairs cars himself for free; the brothers pay half the standard repair price. Completing M11 unlocks reinforced tires for $1,000. Garage work requires the current hero to be the driver of a stopped car. Engine, transmission and turbo boosts are deliberately excluded from purchases for this driving profile. Vehicle changes stay on that physical car; a persistent saved-vehicle collection is not implemented.

## Speed and damage

Power tuning has since been revised with the user's approval: see [Progressive power update](PROGRESSIVE-POWER-UPDATE.md). Cars now receive gradual speed-dependent torque assistance while retaining ordinary launch torque.

Road cars receive a **2x high-gear redline limit**, plus a raised entity speed ceiling, while the crew is deployed. Aircraft, boats and motorcycles are excluded. Shared handling is adjusted once, including later-spawning cars, and original live values are restored on stand-down/reload. Drive force, drive inertia, drag, clutch rates and traction are not increased by this profile. Old scripted car power boosts in the fleet reward, flatbed, drift coupe and Guess's ability have been removed; his ability retains slow motion and grip support.

**This is not a verified doubling of every car's attainable road speed or an identical acceleration curve.** Extending gearing can change acceleration, and drag/available power can prevent reaching the new ceiling. It also cannot guarantee exactly twice the time to maximum speed. The pinned [SHVDN handling source](https://github.com/scripthookvdotnet/scripthookvdotnet/blob/v3.6.0/source/scripting_v3/GTA/Entities/Vehicles/HandlingData.cs) explicitly separates high-gear redline from attainable speed. Matching the requested curve precisely requires measured runs before further calibration; the later progressive-power update adds bounded assistance, but still requires road testing to calibrate the final result.

Active-player running/sprinting uses 1.3x; NPC crew movement also receives a 1.3x movement-rate request. Player walking remains animation-driven. Crew setup and switching clear fire/explosion proofs and allow same-group damage. Friendly targeting prevention remains in the companion relationship/target-selection logic, but accidental friendly bullets and explosive splash are now possible too.

## Enemies and police

Active enemies gain cover, peeking, strafing, flanking and pursuit options. Bystanders and crew are excluded. Police keep native search/arrest/chase tasks, gain stronger driving skill, and receive moderate boxing/PIT behavior rather than constant aggressive ramming. These are engine behavior requests, not a guarantee of a particular formation. [Native chase behavior reference](https://github.com/citizenfx/natives/blob/master/TASK/SetTaskVehicleChaseBehaviorFlag.md).

Dispatch replenishment intervals scale from 1.00x at one star to 0.88/0.76/0.64/0.52x at two through five stars. Existing native population caps remain; six-star military scheduling is unchanged. No extra continuously spawned police fleet is added.

Every car occupied during an active pursuit is marked wanted. Changing cars therefore does not intentionally provide a clean-car disguise. The mod does **not** report fake sightings, reset the search clock or hold stars indefinitely; normal escape remains possible. Confirm the shaded-player-blip behavior during a live out-of-sight car swap, since the native wanted-vehicle flag and HUD are managed by the game.

## Mission changes and retest

Ice starts about 23 meters from his M01 lookout before navigation correction. He must walk to the lookout (within 6m), then identify Mateo without firing. The approach is separately validated on loaded ground. The intro and recognition retain separated actors and actual vehicle seats.

At a mandatory character handoff, movement, attack and interaction inputs are blocked until the correct hero is selected. The camera, character wheel and switch keys remain usable. A player driving a road car receives braking input; a passenger's handoff does not brake the AI driver's chase. This does not freeze the world or pause mission timers. Parallel stages allow any hero with unfinished work in that stage. No persistent ped freeze or player-control toggle is used, so the gate releases naturally after switching, aborting or death.

Test these first:

1. M01: walk Ice to the lookout; finish Guess's first job and verify movement/fire are blocked while the wheel still works. Switch, then abort/retry and verify control returns.
2. Ammu-Nation: enter/approach the marker, buy ammunition/armor, buy a missing gun, and verify balance/ownership after reload. An already-owned gun must not charge twice.
3. Guess Customs: stop in the driver's seat, repair, repaint and fit one wheel/body part. Compare the half-price menu with Los Santos Customs. Check the garage doors physically open.
4. Drive the same car on the same flat road before/after crew deployment, with no ability active. Compare time to the old speed and the eventual top speed; report the model and both results.
5. At three stars swap cars out of sight; verify the disguise does not activate, then escape normally. Compare three/five-star replenishment and pursuit. Confirm one-star officers can still arrest.
6. Verify self/friendly explosive and fire damage, companion target choice, then die, respawn, walk and switch. Prior movement recovery and military response remain regression-tested but need this live combination test.

Later update: [Market and travel](MARKET-AND-TRAVEL-UPDATE.md) supersedes the original flat weapon prices, limited car parts and road-only speed coverage. Story locks, DLC pricing, clothing stores and expanded native vehicle customization are now implemented.
