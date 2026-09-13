# Phone layout, shopping and recovery

The home screen now uses two pages of up to eight smaller tiles (34px icons).
The default first page is Garage, Crew orders, Journal, Progression, Alerts,
Planning, Vehicles and Properties. Messages, Crew, Current job, Wallet, Weazel
News, Help and Settings start on page two. Existing app behavior and the fixed text renderer
are retained.

## Rearrange apps

Open **Settings > Arrange home icons** and press **A** to begin. On keyboard,
**R** picks up the highlighted home app.
Move the selected icon with the D-pad/arrow keys. Movement swaps it with adjacent
positions; continue across the page boundary to move it between pages.
Press **A/Enter to save**, or **B/Backspace to cancel**. The layout is saved with
the campaign and shared by all three brothers. An interrupted or closed phone
cancels unconfirmed changes. Missing/invalid saved app IDs cannot hide the apps.

## Buy vehicles

Open Vehicles, choose a category and model, then a destination garage. Review the
price, garage occupancy and crew funds. Confirm the purchase to store the car at
that garage. Collect it normally or request KJ through Garage.

This uses the existing dealer's installed road-car/motorcycle catalog and prices.
It rechecks model availability, ownership, garage capacity, funds and free-roam
permissions at purchase time. It does not spawn a second copy beside the player.
Back navigation unwinds destination/model/category browsing without purchasing.

## Buy properties

Properties lists the existing purchasable storage garages with their prices and
bay counts. Confirm to buy; an owned listing becomes a map-location action.
These garage properties do not include residential interiors. The app also shows
each brother's current home and housing progression; homes remain story-earned.

## Recover an owned vehicle

Recover vehicle is available in both Garage and Vehicles. Selecting an out car
in Garage also opens its own Locate vehicle and Recover vehicle options. After
recovery, the same options folder offers Request KJ. An unoccupied owned car
that is out in the world can be returned to its garage for **$500**. Wreck recovery
includes repair using the existing scale: **10% of purchase price, minimum $500**.
It preserves the stored build and captures live upgrades from a surviving car.
Recovery removes the old live vehicle before charging and storing the record;
it does not create a replacement beside the player or simulate a tow-truck scene.

Occupied cars, active KJ deliveries, cosmetic previews, insufficient funds, changed
fees and already stored vehicles cannot be charged through recovery. Mission and
wanted-level restrictions still apply. KJ delivery is a separate request afterward.

## Validation

Targeted automated checks cover saved/recovered layouts, cancel/interruption,
controller input, nested shopping navigation, confirmation, full garages, funds,
property ownership, duplicate requests, occupied recovery, upgrade preservation
and save reload. Full story/runtime and recovery regression suites also run.
Visual previews use the shipped layout and glyph assets. In-game input, actual
vehicle models and KJ navigation still require a live retest.

Useful future UI additions, not implemented here: vehicle sorting/filtering and
favorites; property map preview before purchase; adjustable text size/contrast;
and a one-press return to the current objective.

## Individual hangouts

Crew lists each inactive brother separately. Settings contains phone preferences. Invite one or end his hangout while
the other keeps his current choice. The existing travel/ride-along AI handles his
journey. A dismissed shared passenger waits for a safe stop before leaving. These
choices last for the play session; a group order resets them. Mission assignments
and transition guards take priority. No extra teleport or cash charge is added.

## Readable descriptions

List cards show two title lines and two description lines, with four cards visible.
Text wraps using the shipped font metrics. Longer previews use an ellipsis; opening
the entry exposes the full text in its scrollable detail panel. Transaction results
also appear in that panel, rather than a clipped two-line footer. D-pad scroll hints
remain visible. Settings has a safe fallback icon if artwork is missing.

## Input and individual travel

While the phone is open, D-pad and A/B aliases are consumed in player, frontend
and script input groups. Walking, looking, steering, triggers, X, Y and shoulder
buttons remain available. Actions normally on A/B (including sprint, reload and
nitrous) are reserved until the phone closes. Controller icon arrangement starts
from Settings, so X remains a gameplay button. The native alias reference is
https://docs.fivem.net/docs/game-references/controls/ .

Crew > brother offers an individual invitation, end hangout, Ride with me and
Drive alongside me. Travel choices invite only that brother and can differ for
each companion. Changing from a shared car to separate transport waits for a safe
stop. Whole-crew orders deliberately reset individual preferences. Mission-owned
actors and required transport cannot be overridden.

## Weapon customization and variants

Selecting a fitted gun part now removes it for free and retains its ownership for
free refitting. The saved fitted set records removal and prevents the part being
reapplied on restore. Mk I and Mk II ownership and attachments use separate weapon
hashes. Every Mk II catalog entry now has a purchasable original counterpart;
SNS Pistol, Heavy Revolver and Marksman Rifle originals have been added at $4,000,
$12,000 and $22,000 respectively. Existing progression gates remain. No free second
weapon is granted. Own both, then use normal weapon-wheel category cycling (close
the phone before using D-pad cycling). Native weapon-wheel display needs live QA.

## Clear alerts

Alerts starts with Clear all alerts. Open it and confirm to clear the saved alert
and receipt list and unread badge. Cancel preserves the list. An alert arriving
during confirmation invalidates that request so it must be reviewed again. New
alerts continue normally, and a save reload preserves the cleared state.
