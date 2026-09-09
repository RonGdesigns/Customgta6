# Progression, homes and unlocks

Current build: 36 playable missions, M01–M30 and SM01–SM06, plus Ron's arrival prologue before M01 on a fresh save. The full story outline contains 79 jobs. The next unimplemented main chapter is M31; its marker is intentionally absent. When every scripted job is complete the mission key says so instead of offering M01 again. Existing saves and earned items are retained; a save that already finished M01 never plays the prologue.

Before M01 is complete the crew key deploys Guess alone; Ice and Gohan join after the dockyard. Weapons a mission hands out are loans and are not captured into the locker while that mission runs.

## How progression actually works

Finish a mission to satisfy the next job's prerequisite. The three solo jobs of Act I become available after M03; the three Act II solo jobs open after M28. Optional solo jobs are never required to understand or unlock the next main mission. Replay is available through the mission menu. It does not pay the same reward twice.

This is currently a mission-milestone system, not an XP or character-level system. There are no skill points, weapon levels, recurring rent payments or simulated businesses. The cash and gold counters record the crew's resources. Ammu-Nation and vehicle shops now spend crew cash on weapons, ammunition, armor and car work; see MARKET-AND-TRAVEL-UPDATE.md for story locks, weapon prices, customization and Guess Customs discounts. The offshore escrow remains zero in the playable chapters.

## Starting equipment

| Hero | Starting rifle | Other starting gear retained |
|---|---|---|
| Guess / Ron | Service Carbine, the M16-style rifle | Micro SMG, Pistol, Sawed-Off Shotgun |
| Ice | Assault Rifle | RPG, Pistol .50, Sticky Bombs |
| Gohan | Bullpup Rifle | SMG, AP Pistol, Flashlight, Smoke Grenades |

Starting loadouts receive 250 rounds per weapon at mission deployment. Guess receives a stock Carbine Rifle fallback if the installed game does not expose the Service Carbine. Switching keeps each hero's current health, armor and weapons. All three use the same 900-health cap and 100 starting armor, without healing on every switch.

Acquired weapon ownership is saved separately for each hero. Free-roam health, armor and ammunition are captured every 30 seconds and on normal stand-down/reload, then restored when deploying through the ordinary crew hotkey. Mission deployments intentionally start fresh. Debug deployment remains a fresh QA spawn. Saves do not reconstruct a running mission, remote vehicles, personal wanted incidents, or the precise off-duty activity after restarting GTA. Downed heroes are handled by recovery, not re-created dead from a save.

## Earned weapon tiers

Each row lists Ice / Gohan / Guess. Rewards appear in each hero's personal locker and are supplied when absent; visiting home restocks owned equipment. Existing saves past a milestone receive its new weapon unlocks automatically. Mk II guns use ordinary ammunition here: specialist AP/explosive ammunition and component upgrades are not yet implemented.

| Milestone | Ice | Gohan | Guess |
|---|---|---|---|
| M03, foundry weapons shipment | Pump Shotgun | Stun Gun | Combat Pistol |
| M06, evidence-depot escape | Combat MG | Carbine Rifle | Assault SMG |
| M15, harbor access | Heavy Sniper | Special Carbine | Assault Shotgun |
| M23, desert bunker | Assault Rifle Mk II | Bullpup Rifle Mk II | Carbine Rifle Mk II |
| M27, charter ledger | Combat MG Mk II | Special Carbine Mk II | Tactical SMG |
| SM01, Ice's arms job | Pump Shotgun Mk II | — | — |
| SM04, quarry radios | Heavy Sniper Mk II | — | — |
| SM05, estuary interceptor | — | Pistol Mk II | — |
| SM06, airfield fuel | — | — | SMG Mk II |

DLC rewards are availability-checked. Unsupported hashes stay recorded as unlocks but are not passed to the give-weapon native. The debug DLC weapon menu remains a preview/testing shortcut, not a progression gate.

## Homes and practical upgrades

| When | What changes |
|---|---|
| Start | Each brother has an exterior home marker and access to a furnished starter interior. Rest, wardrobe and personal locker are available between jobs after losing police. |
| M03 | Cypress foundry is recorded as the crew's base. This is a story/base flag, not a new walkable foundry interior. |
| SM01 | Ice's armor-piercing supply line: every locker restock issues his rifles double the usual ammunition. Ice only. |
| SM03 | Racing transmission: any car or motorcycle repaired at Guess's chop bay leaves with the race transmission mod fitted, once per vehicle. KJ remains a supporting NPC. |
| M11 | Granger turbine upgrade becomes available to the fleet system. The earlier engine-theft missions only acquire cargo. |
| M14 | McKenzie hangar access is recorded. |
| M17 | Reinforced Kraken upgrade is recorded. |
| M22 | Cypress foundry access is lost as part of the completed chapter; abandoning a failed attempt does not destroy the saved unlock. |
| M23 | Grand Senora radar bunker access is recorded. This does not add a custom MLO interior. |
| M27 | All three can enter their respective furnished Eclipse Towers luxury suites through the shared exterior entrance. |
| M28–M30 | Northern relay, bunker fuel and satellite-parts acquisitions are recorded; these are preparation/story flags, not global changes to native police or satellite behavior. |
| SM04–SM06 | Recovered radios, buoy telemetry and airfield fuel are recorded, with the weapon rewards above. |

At home, Ice's workbench restocks weapons and armor, Gohan's reviews the next verified lead, and Guess's repairs a nearby parked road vehicle. Those services already have useful effects. The housing progression currently has two furnished tiers; a third residence tier is a proposed later reward.

## One-time cash and haul

| Job | Cash | Other ledger effect |
|---|---:|---|
| M05 | $50,000 | — |
| M15 | $15,000 | — |
| M22 | $150,000 | Dredged gold resets to zero at the initial stash |
| M24 | $200,000 | Five tons dredged |
| M25 | $40,000 | — |
| M27 | $75,000 | — |
| M28 | $20,000 | Relay disabled flag |
| M29 | $35,000 | Bunker fuel flag |
| M30 | $45,000 | Satellite parts flag |
| SM03 | $25,000 | Racing transmission (chop bay) |
| SM04 | $15,000 | Quarry radios flag |
| SM05 | $15,000 | Estuary telemetry flag |
| SM06 | $25,000 | Airfield fuel flag |

Total available from these first-completion payouts: $710,000. Previously paid replays are not clawed back. Completion and its rewards are written in one save operation with a previous-save backup; aborting or failing before completion pays nothing. Debug force-completion intentionally bypasses gameplay.

## More unlocks worth implementing next

These are designed future rewards, not features installed in this build:

1. **M31: fortification workbench.** Spend some existing cash on armor caches and a limited emergency supply crate at the bunker. Avoid permanent invincibility or infinite ammunition.
2. **M34/M35: earned armored transport.** A replacement half-track and an armed technical become garage choices after their acquisition missions. Charge for replacement after destruction; keep wrecks in the world normally.
3. **M36/M40: marine storage and repairs.** A submarine berth, upgraded extraction launches and a coastal repair point make the offshore preparation jobs useful outside their cutscenes.
4. **M43: preparation board.** Show the actual acquired fuel, boats, aircraft and access codes. It should explain which mission supplies a missing item, using existing save flags.
5. **M54: a third home tier.** An enterable penthouse command room with a planning table, expanded vehicle storage and personal wardrobe presets. Choose installed, tested interior geometry first.
6. **M55–M59: team coordination rewards.** Faster assigned NPC work and longer intel-mark visibility after the crew learns to share information. Keep base objectives understandable without optional solo rewards.
7. **M60: Davis services.** A neighborhood repair contact and discounted supplies follow the defense mission. The reward reflects protecting people, not only acquiring stronger guns.
8. **M70: post-campaign free roam.** Keep homes, earned weapons and garage choices; expose mission replay without repeatedly awarding the final escrow. Cosmetic outfits and photo locations can reward completion without inflating enemy health.

Before adding prices, implement an explicit purchase/ownership transaction and save it atomically. Avoid deducting cash until the requested item has actually spawned or the upgrade has been applied.

