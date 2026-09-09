# Recovery, personal customization and sixth-star update

September 8, 2026. Requires a fresh GTA launch. Existing saves and appearance settings are preserved.

## Test recovery first

The latest installed-build log showed military helicopters being created about every frame. `CompanionController.MissionActive` cleared the military manager on every assignment, including repeated `false` assignments from the main loop. This reset both the live-unit list and spawn timer. The setter now runs cleanup only when mission state actually changes. Military cleanup removes its own soldiers and unused vehicles, preserving vehicles occupied by someone else.

Windows recorded the latest crash at 22:32:15: an access violation in GTA5_Enhanced.exe, offset 0x1b4e911. That differs from the earlier breakpoint crash. Runaway spawning is confirmed by logs and source; the exact engine crash mechanism and resolution still require live testing.

Respawn clears military forces and crew heat before returning to play. It verifies a living player, enabled controls and loaded collision over the post-death frames. A five-second verification failure returns to the original Story Mode character instead of leaving recovery stuck. Revived characters receive the intended maximum health before their health is filled.

Character switching preserves both the departing and incoming hero's health and armor. It also restores both during a failed vehicle handover. Existing damage remains attached to its character.

1. In free roam, die without a wanted level. After respawn, walk, turn, aim and enter a car. Repeat once in a vehicle.
2. Injure one hero, switch to a healthy hero, then switch back. Health and armor should remain separate.
3. Test death during six-star pursuit only after the first two pass. Confirm military units clear and movement returns. If it fails, stop that test and retain Bloodlines.log.

## Debug controls and wardrobe

While the debug or home menu is open, the left stick / WASD moves the character. The right stick and D-pad navigate the menu; A accepts and B backs out. Sprint, attacks, camera cycling and idle-camera changes are blocked while the menu has focus. The view you opened it with is preserved; closing the menu restores ordinary camera controls.

Open **Player / crew → Appearance → character**, or **home → Wardrobe**. Select an item category, then use left/right to change the item or its color/texture. Options include shirt, undershirt, arms, pants, shoes, accessories, mask, bag, armor clothing, decals, hat, glasses, earrings, watch and bracelet. Hair, hair color, facial hair and beard color have separate controls. A beard or prop can be set to none. Use **Save looks** to retain choices across restarts.

Clothing choices are limited to the model's available variants. Some tops need a matching arms/undershirt selection to avoid clipping; those controls are included. Editing clothing turns off automatic outfit changes for that hero. Reset clothing preset clears individual clothing overrides. No new clothing assets are included.

## Enterable apartments and progression

Each hero has a furnished starter apartment entry, separate from the earlier workplace markers. The starter apartments use the same stock furnished interior layout, visited one at a time. **Route home** sets the destination. At the home marker, press E / D-pad Right and choose **Enter apartment**.

Walk around inside. The interior entry marker opens rest/save, wardrobe, personal weapon locker, crew messages and **Exit apartment**. Switch characters and start missions after exiting; companions remain outside instead of teleporting into the interior. Guess's vehicle repair is available outside beside a parked vehicle.

Completing **M27** unlocks three Eclipse Towers luxury suites, one floor per hero, and moves the home destination to Eclipse. The M27 follow-up explains the clean-name rentals; the radar bunker remains the operational base. Before M27, the dev-enabled home menu offers **Preview luxury apartment**, which returns to the original entry and does not grant campaign progress.

Entry waits for interior readiness and collision. A timeout returns you to the position where entry began. Saves made inside record the exterior return point so a fresh session does not spawn inside an unloaded room. These are stock GTA interiors, not custom MLOs. Rendering, door placement and navigation require live Enhanced testing; failed loading must decline entry rather than release the player into empty space.

## DLC weapons

The debug root has **DLC weapons**, with 28 choices divided into Mk II, DLC firearms and special weapons. Only valid installed weapons appear. A selection adds ammunition and saves that weapon to the active hero's personal locker. It does not grant the weapon to all three heroes or change mission rewards.

Twelve Mk II weapons are included. Other choices include Ceramic Pistol, Navy Revolver, Perico Pistol, Military Rifle, Heavy Rifle, Service Carbine, Precision Rifle, Combat Shotgun, WM 29 Pistol, Tactical SMG, Battle Rifle, Compact EMP Launcher, DLC Railgun, Up-n-Atomizer, Unholy Hellbringer and Widowmaker. Standard ammunition is used; special Mk II ammunition and component customization are not part of this update.

## Six stars

The custom sixth tier displays six white star icons in the upper-right wanted area, replacing the five-star row while active. The previous separate MILITARY label is removed. Safe-zone and aspect ratio are respected, with a flashing search state. GTA's normal wanted display returns below six.

After 90 continuous seconds at five stars, the sixth tier activates. First dispatch waits 10 seconds; subsequent dispatch opportunities are 20 seconds apart. The order is helicopter, rear-gunner Barracks truck, then Rhino tank. Spawn checks require distance, an off-camera position and clear ground space. A failed placement waits for the next opportunity; no teleport-to-player shortcut is used. Destroyed units wait at least 12 seconds before a replacement opportunity. At most three managed vehicles are active.

Helicopters pursue at a higher requested speed and smaller orbit distance. Available models rotate between Buzzard, FH-1 Hunter, Akula, Savage and Annihilator Stealth. One helicopter is active at a time. The Barracks driver pursues while its two rear passengers receive drive-by tasks.

The tank retains its vehicle attack task. With a living seated driver, a clear line of sight and a target 40–180 metres away, it selects the mounted tank cannon and requests a shot no more often than every 6.5 seconds, after an initial eight-second delay. This uses the vehicle weapon, not a generated explosion. Verify actual aiming, firing and stability in the live game. Switching between heroes sharing six-star heat preserves the pursuing units.

## Also included

The pending 74-vehicle catalog, home workbenches, eleven campaign messages, M03/M06 objective clarification and M01 quiet-cover feedback are included. See VEHICLES-WORKBENCHES-DISPATCHES.md for those changes; this document supersedes its exterior-only home description.

## Implementation references

Coordinates and IPL names were checked against [Bob74's apartment definitions](https://github.com/Bob74/bob74_ipl/tree/master/dlc_executive), the [starter interior definition](https://github.com/Bob74/bob74_ipl/blob/master/gta_online/house_mid_1.lua), and [QBCore's apartment entry definitions](https://github.com/qbcore-framework/qb-apartments/blob/main/config.lua). Model/weapon names were checked against installed-reference availability and [CitizenFX's weapon reference](https://github.com/citizenfx/fivem-docs/blob/master/content/docs/game-references/weapon-models.md). Native behavior was checked against the [CitizenFX native reference](https://github.com/citizenfx/natives), including head overlays, control flags and vehicle weapon selection. These references do not replace Enhanced playtesting.
