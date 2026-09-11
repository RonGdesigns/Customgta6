# Shops, customization and vehicle travel

This update replaces the original shop's flat pricing and road-car-only tuning. The campaign remains at 36 playable missions. Existing saves, character appearance settings and surveyed coordinates are retained.

## Gun shop rules

Ammu-Nation has three tabs:

- **Unlocked weapons:** the active character's starting equipment, acquired weapons and weapons released by completed story milestones.
- **Additional weapons:** ordinary/DLC guns that are outside the campaign reward gates. Buying one adds it to that character's saved ownership.
- **Story unlocks:** unavailable reward weapons, labeled with the actual mission needed. Selecting a locked gun does not deduct cash or give the weapon.

Mission rewards still grant the specified weapon free to the designated hero. Completing the milestone also makes that gun purchasable by the other heroes. Solo rewards use the actual solo beneficiary; filler entries in the old three-slot reward array do not unlock unrelated weapons. Previously acquired guns are retained, including purchases from the earlier build. Debug weapon grants remain explicit testing cheats.

Prices reflect Bloodlines' $710,000 currently available in one-time mission payouts, not the separate GTA Online economy. Examples:

| Gun | Crew cash |
|---|---:|
| Pistol | $1,500 |
| Assault Rifle | $10,000 |
| Service Carbine | $30,000 |
| Combat MG Mk II | $60,000, after M27 |
| Heavy Sniper Mk II | $75,000, after SM04 |
| Compact EMP Launcher | $45,000 |
| Up-n-Atomizer | $60,000 |
| Unholy Hellbringer | $100,000 |
| Widowmaker | $160,000 |
| Railgun | $180,000 |

Ordinary ammo remains $60 for 60 rounds. Heavy/explosive packs contain three rounds; railgun packs cost $1,800. Hellbringer/Widowmaker replenishment costs $600. Owned explosive weapons are also restored with small packs instead of receiving 60 explosive rounds from the generic locker path. Unsupported or already-owned purchases cost nothing. Homes still provide the established free restock service.

## Vehicle customization

All vehicle-reported indexed mod slots are offered, including engine, transmission, brakes, suspension, armor, horns, bodywork, interior trim, seats, steering wheels, engine-bay details, speakers, hydraulic parts, liveries and light bars. The menu hides indexed categories with no compatible parts. Car and motorcycle service requires the active hero to be driving and stopped.

The shop also supports turbo, tire smoke, xenon headlights/colors, all 13 wheel families (including Benny's, Open Wheel, Street and Track), custom tires, window tint, plate styles, native liveries, supported extras and neon sides. Paint channels are separate: primary, secondary, pearlescent, wheels, interior trim and dashboard. Custom primary/secondary, neon and tire-smoke colors have RGB controls. Unsupported applications do not charge cash.

Wheel browsing temporarily reads the selected family's options, then restores the existing wheel setup. A failed wheel purchase restores it as well. Motorcycle front/rear axles are handled separately. On cars, slot 24 can mean hydraulic equipment; wheel purchases must preserve that equipment.

Guess Customs retains its half-price parts and Guess's free repair labor. M11 unlocks reinforced tires there. Parts modify the actual current vehicle; **a persistent owned-vehicle garage and saved/replacement builds are still future work**.

This exposes native customization on installed Story Mode/DLC models. It does not implement every separate GTA Online service: Benny's model conversions, HSW conversion packages, Imani remote control/missile jammer services, Arena progression and custom mounted-weapon behavior still need dedicated systems. A reported mod slot is not proof that those service scripts exist in Story Mode. Native slot and wheel references: [vehicle modifications](https://github.com/citizenfx/natives/blob/master/VEHICLE/SetVehicleMod.md), [wheel types](https://github.com/citizenfx/natives/blob/master/VEHICLE/GetVehicleWheelType.md).

## Clothing stores

Thirteen Binco, Discount Store, Suburban and Ponsonbys locations have clothing icons and fitting-room access. Approach on foot, outside missions and with no wanted level; press **E / D-pad Right**, then **Choose outfit**. Pick shirts, undershirts, arms, pants, shoes, accessories, masks, bags, armor, decals, hats, glasses, earrings, watches and bracelets. Save the look to retain it after restarting. These currently reuse the wardrobe's free fitting service; there is no purchased-clothing inventory yet.

Head hairstyles and hair colors cannot be changed through stores, homes or the debug menu. Ice's locs, Gohan's normal hair and Guess's bald identity are retained. The previously requested facial-hair options remain in the home/debug wardrobe, not the clothing store. Existing appearance INI values are not overwritten during installation.

Recognized clothing doors are unlocked locally and their prior state restored when leaving. Service markers also work near the storefront if a native clerk script does not recognize the custom protagonist. Store coordinates and physical door behavior still need live confirmation.

## All vehicle categories

World tuning now registers loaded vehicles across road, air and water categories, including bikes, bicycles, planes, boats, submarines and trains. Helicopters are left stock since September 10: no ceiling, no cruise power, no handling edit. It sets a **2x speed ceiling** from the original valid gearing/model estimate, without assigning velocity or teleporting vehicles. A trailer has no propulsion to accelerate independently; existing train mission/cruise commands still determine commanded speed.

Road vehicles retain the gradual torque curve: ordinary launch, then speed-dependent assistance capped at 80%, with at least four seconds to ramp fully. Cars jumping into the air receive no extra torque. Aircraft receive cruise assistance only while airborne and already moving forward; taxi/takeoff runs and hovering receive no extra power. Marine assistance requires being in water. Stalled/destroyed vehicles receive none.

The installed Enhanced SDK exposes public flight/boat sub-handling APIs absent from the pinned 3.6 compile reference. A checked adapter uses these public properties without hard-coded memory addresses: plane thrust falloff and longitudinal resistance are reduced; boat water drag is reduced; helicopters are not touched. Base thrust, flight lift, vertical/lateral damping, steering and road drive force/inertia remain unchanged. Shared profiles are applied once and restored through live handles/model data on stand-down. Later edits from another handling owner are not overwritten. [Boat handling API](https://github.com/scripthookvdotnet/scripthookvdotnet/blob/main/source/scripting_v3/GTA/Entities/Vehicles/HandlingData/BoatHandlingData.cs), [native torque](https://github.com/citizenfx/natives/blob/master/VEHICLE/SetVehicleCheatPowerIncrease.md), [speed ceiling](https://github.com/citizenfx/natives/blob/master/VEHICLE/SetVehicleMaxSpeed.md).

**A configured 2x ceiling is not proof of exactly 2x measured top speed.** Aircraft/water drag and individual models require in-game calibration. The automated checks verify category coverage, smooth application, no hover boost, shared-profile behavior and cleanup; they cannot simulate GTA physics. Braking/coasting feel on boats and aircraft is a particular live-test item after reducing resistance.

## Focused retest

1. At Ammu-Nation before M03, Pump Shotgun should show its M03 lock. A purchasable extra should debit its displayed amount, stay owned for only the purchasing hero, and survive a reload. Expensive weapons must reject insufficient cash.
2. At Guess Customs, browse several wheel families without buying: wheels/hydraulics and cash must remain unchanged. Buy a compatible part, separate paint colors, turbo and a livery. Try the same purchases again and an unsupported extra; no duplicate/failed charge.
3. Enter a clothing-store marker on foot. Change shoes/shirt/accessories, save, switch away/back and restart. Confirm the outfit persists and head hair has no editable option. Check both controller camera and movement while browsing.
4. Compare a sedan, bike, Longfin and Vestra before/after deployment on the same route; a helicopter should be identical before and after. Accelerate normally, brake/coast and restart; test aircraft taxi, takeoff, hover and forward cruise separately. Record model, measured speed and handling observations. Stand down and confirm original behavior returns.

Build checks and installation receipts accompany the update. Live storefront, mod-kit and air/water handling validation remains required.
