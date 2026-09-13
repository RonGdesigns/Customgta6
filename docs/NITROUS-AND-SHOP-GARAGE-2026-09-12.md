# Nitrous and saving customized cars

This package includes the pending M05 chase, P5c solo-mission and Foundry loading fixes. Installation is separate from source/build validation; check the installation receipt before expecting new behavior in GTA.

## Nitrous

The previous shop enumerated every raw vehicle toggle, including the SDK's Nitrous entry (slot 17). It could install that flag but there was no Bloodlines boost input or runtime effect. That misleading entry is now connected to a working, bounded road-vehicle boost.

- Buy or enable **Nitrous boost** under compatible vehicle parts. Base price stays $1,200, with the shop's existing price adjustment.
- **Hold controller A / keyboard X while accelerating forward.** The button replaces duck/hydraulics/secondary vehicle fire for an equipped driver. Cars without nitrous and passengers keep their ordinary controls. The binding follows GTA's vehicle-duck action; the [primary control reference](https://docs.fivem.net/docs/game-references/controls/) lists its default A/X mapping.
- A full bottle supplies five seconds of boost at 1.65 times the existing torque assistance. It does not snap the car to a fixed speed or rewrite acceleration/handling entries. WorldTuning remains the single writer of per-frame torque.
- Release the button to recharge: a two-second delay, then twenty seconds from empty to full while using the equipped vehicle. A cyan meter and percentage show the charge. Holding an empty bottle cannot create repeated free boost pulses.
- Braking, handbraking, reverse, flight, engine-off, passenger seats, menus, previews, scenes, recovery, apartments, survey mode and required character handoffs block boost. Switching seats does not refill the same live vehicle's bottle. Charge is session-local; stored/recreated vehicles start with a fresh bottle.
- Installed nitrous now saves with vehicle finishes, garage records and the crew fleet. Preview cancellation restores the original toggle, and only confirmation charges for it. Unsupported raw toggle slots are filtered out and cannot take money. Nitrous adds $300 to the existing resale upgrade component.

Existing live cars with slot 17 enabled qualify automatically. Older garage records that never captured that flag cannot establish whether it was previously purchased; the new code does not invent a purchase history.

## Customs: Save car to garage

At Los Santos Customs, Beeker's or Guess Customs, choose **Save car to garage**, then select an owned garage. The stopped driver stays in the same car and can continue customizing or drive away. The garage reserves one bay and tracks that car as out, so KJ/retrieval cannot create a second copy while it remains out.

Confirmed paint, neon, wheels, indexed modifications, toggle upgrades and other supported finishes use the existing garage snapshot format. Re-saving updates the same record, including when its current garage is full. Choosing another owned garage transfers its reservation if space is available. A different car cannot overwrite a full garage's contents. The save itself is free.

The confirmation rechecks the shop, wanted state, free-roam availability, driver's seat, stopped vehicle, garage ownership and capacity. Unconfirmed previews cannot be saved. A failed native build capture leaves the previous record intact. The persistent crew car remains in its separate fleet system; confirmed shop changes to it are saved automatically there.

## Validation and live retest

Automated checks cover the input gates, finite bottle, recharge, torque composition, preview/cancel/purchase, save/reload, duplicate prevention, capacity, garage transfer, stale menu conditions and native capture failure. These checks use GTA stand-ins and do not establish road feel or native behavior in the live game.

1. Fit nitrous, cancel once, then confirm. Check the charge and menu price.
2. Accelerate and hold A/X: check visible boost, no duck/hydraulics animation, full depletion and recharge. Brake and verify boost stops.
3. Fit paint/neon and nitrous, save to a garage, and keep driving. Re-save changes to the same one-slot bay; it must remain one car.
4. Put the car away and retrieve it or request it from KJ. Verify all confirmed upgrades, including nitrous, remain fitted.
5. Retest the Foundry entry and the pending M05/P5c changes from their package notes.
