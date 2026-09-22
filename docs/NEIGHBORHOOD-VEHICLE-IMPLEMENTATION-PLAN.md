# Neighborhood progression and owned-vehicle lifecycle

Implementation plan prepared September 20, 2026.

## Decision

Build these as one connected progression system. Story events change a small number
of authored neighborhoods. Those states are visible in the phone and change local
vehicle services, friendly presence, recovery work and reactions. The vehicle system
then gives the player useful reasons to care about the places the crew has secured.

The first release supports Davis, Cypress Flats and Grand Senora. It uses three named
states and explicit story events. It does not add a citywide territory score, passive
state decay or repeatable turf grinding.

The current garage system already saves owned cars and their modifications, returns
abandoned cars, recovers wrecks and lets KJ deliver a selected car. The work should
extend that foundation instead of replacing it.

## Research translated into decisions

- Far Cry 5 makes resistance progression readable by letting actions change both a
  regional meter and the surrounding world. Bloodlines should keep the visible world
  response, but use authored story milestones so random chores cannot advance the
  campaign. Source: [Far Cry 5 reveal roundup](https://news.ubisoft.com/en-us/article/4Oe69xH3cUdeUu60Wl0t6p/far-cry-5-reveal-roundup-coop-character-customization-and-more).
- Mafia III ties district control to story choices, information and lieutenant
  relationships. The useful lesson is that a district change should unlock a clear
  service or option and record who caused it. Source: [Mafia III FAQ](https://support.2k.com/hc/en-us/articles/229711387-Mafia-III-FAQ).
- The Gang Territory Mod exposes territory, respect, recruitment and missions, but
  Bloodlines already owns those story beats. Copying a free-running turf simulation
  would create conflicts with authored missions and dilute the campaign. Source:
  [Gang Territory Mod](https://www.gta5-mods.com/scripts/gang-territory-mod-respect-system).
- Persistence V4 makes a large garage usable through favorites, delivery tracking,
  recovery, spawn validation, garage transfers and stable vehicle identity. Those are
  the highest-value additions to the Bloodlines garage. Source:
  [Persistence V4](https://www.gta5-mods.com/scripts/persistence-v4-vehicle-persistence-mod).
- Persistence Pro and Advanced Persistence show that players expect a phone-managed
  vehicle to have a name, location, condition, GPS action, repair path and recovery
  action. Source: [Persistence Pro](https://www.gta5-mods.com/scripts/persistence-save-vehicles-enchanced)
  and [Advanced Persistence](https://www.gta5-mods.com/scripts/advanced-persistence-vehicle-management).
- Rockstar separates mechanic delivery, insurance recovery and impound retrieval.
  Bloodlines should use equally distinct action names and prices so the player knows
  whether a car is being delivered, returned, repaired or rebuilt. Source:
  [Rockstar lost-vehicle support](https://support.rockstargames.com/articles/375Mcz7Cc3wFa6YIG9gES0/finding-your-lost-or-misplaced-vehicles-in-grand-theft-auto).
- Open-world level design needs a controlled progression curve, gameplay focus and
  emotional purpose. Each Bloodlines district therefore gets a short authored arc
  with strict runtime boundaries rather than a universal simulation. Source:
  [GDC Level Design Workshop: A 360 Approach](https://www.gdcvault.com/play/1023140/Level-Design-Workshop-360-Approach).

## Experience contract

The player should always be able to answer four questions from the phone:

1. What is the crew's current position in this neighborhood?
2. Which story event caused it?
3. What changed in the world and services?
4. What can the player do here now?

Neighborhood progress must feel authored. A state can improve or regress when the
story supports it. Cypress Flats is the clearest example: the foundry becomes useful,
is destroyed, and is later reclaimed. The journal preserves that history instead of
showing only the latest tier.

## Neighborhood state model

Use the same three state names in every supported district:

| State | Meaning | Runtime rule |
|---|---|---|
| **Exposed** | The crew has no dependable local control. | Baseline prices; rare noncombat hostile surveillance; no friendly service presence. |
| **Holding** | The crew has a foothold, but it is incomplete or contested. | One visible friendly presence; 10 percent mapped service discount; local recovery work can appear. |
| **Secured** | A named story action established a durable local network. | Stronger friendly presence; 20 percent mapped service discount; full local vehicle-service benefits. |

These are authored outcomes, not points on a meter. No district loses progress over
time. Story events may deliberately set a lower state and must record the reason.

### Story timeline

| District | Event | Result | Player-facing reason |
|---|---|---|---|
| Davis | Campaign start | Exposed | The old block is watched and the crew has not earned local trust. |
| Davis | Complete M49, **Return to the Concrete** | Holding | The crew is back in Los Santos and local contacts begin helping again. |
| Davis | Complete M60, **Siege of Davis** | Secured | The neighborhood rallied and survived the Aegis attack. |
| Cypress Flats | Campaign start | Exposed | The foundry is not yet an operating base. |
| Cypress Flats | Complete M03, **Cypress Foundry** | Holding | The crew brought the foundry online. |
| Cypress Flats | Complete M22, **Port Heist: Scorched Bay** | Exposed | Aegis destroyed the foundry and forced the crew north. |
| Cypress Flats | Complete M49 | Holding | The crew returned south and reopened a limited city operation. |
| Cypress Flats | Complete the new **Foundry Reclamation** interstitial | Secured | The crew cleared surveillance, restored power and rebuilt the service bay. |
| Grand Senora | Campaign start | Exposed | The crew has no protected Blaine County foothold. |
| Grand Senora | Complete M23, **Ghost in the Sage** | Holding | The bunker is secured but lacks a working perimeter and supply network. |
| Grand Senora | Complete M31, **The Iron Perimeter** | Secured | The bunker perimeter and road access are fortified. |
| Grand Senora | Complete M43, **Staging Paleto** | Secured | Add a history entry showing that the secured base supported offshore staging. |

**Foundry Reclamation** should be a short authored aftermath between M49 and M54,
not a repeatable turf activity. Target 8 to 12 minutes: inspect the damaged site,
remove an Aegis observation crew without starting a police loop, restore power and
bring a service vehicle into the bay. It may be optional, but the phone must clearly
show that Cypress remains at Holding until it is completed.

### Bounded district effects

District effects use authored anchors and mapped services only.

| District | Holding | Secured |
|---|---|---|
| Davis | Local lookout messages; 10 percent discount at the mapped Rancho garage and nearby service site. | Friendly watch at the M60 block; 20 percent service discount; more frequent local recovery offers. |
| Cypress Flats | Workers and one crew vehicle at the foundry; 10 percent discount at mapped La Mesa and Popular Street services. | Rebuilt service-bay dressing; 20 percent discount; KJ return service available from the foundry. |
| Grand Senora | Unlock a small bunker garage after M23; 10 percent discount at mapped Sandy Shores or Harmony services. | Friendly perimeter presence; 20 percent service discount; local return and recovery service. |

Discounts apply to service labor, return and recovery. They do not discount vehicle
purchases or mission rewards. The quote must show the base price, neighborhood
benefit and final price.

World changes remain deliberately small:

- one active neighborhood encounter group at a time;
- no more than four ambient peds and one ambient vehicle per district;
- at least an eight-minute real-time cooldown between spawned events;
- distance cleanup with no persistent handle stored in the save;
- no automatic gunfight and no automatic wanted level;
- no change to GTA's global police population or relationship groups.

Exposed surveillance is a parked watcher or passing vehicle, not an ambush. Holding
uses workers, a lookout or a service vehicle. Secured uses a friendly watch group and
more complete site dressing. Hostile actors can appear only inside a selected contract
that owns its cleanup and resolution.

## Mission ownership and safety

Neighborhood runtime updates stop whenever any of these conditions is true:

- a story, solo or external mission is running;
- a cutscene, mission survey, death recovery or home interior transition is active;
- the player has a wanted level;
- the current authored mission owns the same district or anchor.

Entering one of those states removes neighborhood-created blips and releases or
cleans its ambient entities. Returning to free roam starts a cooldown; it does not
immediately respawn the previous scene.

The state itself can still change when a mission completes. Only the free-roam world
presentation is suspended. This separation prevents mission scripts and neighborhood
scripts from fighting over peds, vehicles, blips or wanted behavior.

## Persistent data contract

Add a static district catalog and a small event ledger. Save the result and the reason
instead of a numeric influence score.

```text
NeighborhoodState
  DistrictId       stable string: davis, cypress-flats, grand-senora
  State            Exposed, Holding or Secured
  LastEventId      stable idempotency key
  LastChangedUtc   timestamp for display only

NeighborhoodEvent
  EventId          unique key such as cypress.m22.scorched
  DistrictId
  State
  SourceId         mission or contract id
  Reason           player-facing American English copy
  OccurredUtc
```

`CampaignState.MarkComplete` is the single integration point for canonical mission
events. Applying an event checks `EventId` first, updates the district snapshot and
appends one history row. Replaying a mission must not repeat the change, notice,
discount or reward.

When an older save loads without neighborhood data, migrate it by replaying the known
completed-mission milestones in campaign order. The migration creates the same final
state and history without firing old notifications. A new save starts with the three
Exposed entries already present.

## Phone experience

Add **Neighborhoods** as a top-level app next to Journal and Progression. Three cards
are enough for the first release and make the system easy to discover.

Each district card shows:

- state name and a text label; color is secondary;
- the event that caused the current state;
- active services and their exact discount;
- the next known opportunity when revealing it does not spoil the story;
- a timeline containing every prior change;
- a route action to the district's current service anchor.

On a state change, create one persistent phone notice and one journal timeline entry.
The HUD toast is brief: `Cypress Flats: Holding — Foundry operations restored.` The
phone contains the full reason and benefit list.

## Owned-vehicle lifecycle

### Current foundation to preserve

The current implementation already provides:

- persistent identity, owner, garage, paint, livery, wheels, window tint, plate,
  tuning stages and modifications;
- store and retrieve actions;
- KJ delivery with tracking and cancellation;
- paid recovery for out or wrecked vehicles;
- automatic garage return when an abandoned owned car stays far away;
- automatic modification capture while driving;
- wreck detection through `InShop`.

The first lifecycle release should make those states explicit and add the missing
management actions.

### Saved fields

Add these fields to `OwnedVehicle`:

```text
Favorite           bool, default false
EngineHealth       float, default full for migrated saves
BodyHealth         float, default full for migrated saves
DirtLevel          float, optional presentation value
```

Keep `Garage`, `Owner`, `InShop`, `Label` and the saved build fields. Delivery and an
active world vehicle remain runtime state. Repair quotes are calculated when shown and
are not saved.

Use the saved vehicle ID as the authoritative identity. Model and plate are validation
hints, not a second ownership record. Before spawning or delivering, check that no
live vehicle already owns that ID.

Do not attempt to persist body deformation, broken doors, windows or individual tire
damage in the first release. Save engine and body health, restore them safely and let
the repair service represent the rest. This provides visible condition without making
the system depend on fragile damage recreation.

### Player-facing states

Every car has one derived status in the phone:

| Status | Meaning | Main action |
|---|---|---|
| Stored | In its assigned garage and ready. | Deliver or retrieve |
| Out | A live instance exists in the world. | Locate or return |
| Damaged | Driveable, with saved condition below the healthy threshold. | Repair |
| Repair required | Wrecked or below the safe engine threshold. | Rebuild |
| In service | Paid repair is being resolved. | View receipt |
| Delivery | KJ is bringing the car. | Track or cancel |

Status text must be the source of truth. A favorite star, owner color or map blip may
support it, but cannot replace the label.

### Phone actions

The Garage app sorts favorites first, then active/out cars, then stored cars. A row
reads `★ Buffalo STX — Out` or `Elegy RH8 — Repair required`.

The detail page shows garage, owner, condition percentage, saved build stage and the
current neighborhood benefit. It exposes only actions valid for that state:

1. **Deliver** — existing KJ flow for a stored, healthy car.
2. **Locate** — existing route/blip flow for an out car.
3. **Track delivery** or **Cancel delivery** — existing delivery flow.
4. **Return to garage** — new flow for an unoccupied car. A visible nearby car is
   picked up or allowed to leave naturally; it is never deleted in front of the
   player. A distant off-screen car is captured and stored after a short service delay.
5. **Repair** — new stored-car service using saved condition. It restores health and
   clears `InShop` after payment.
6. **Recover** — existing lost/wreck flow with clearer language. A wreck rebuild keeps
   the saved build and returns the car to its assigned garage.
7. **Move garage** — new action for a stored car when the destination is owned and has
   a free bay.
8. **Assign owner** — expose the existing owner tag for Ice, Gohan, Guess or Crew.
9. **Favorite** — new immediate toggle used for sorting.

Custom renaming and multiple named build presets can follow later. `Label` already
supports a display name, but reliable controller and keyboard text entry should be
solved before putting rename in the first release.

### Service pricing

Keep pricing simple and visible:

- return of a driveable car: $250 base;
- recovery of a lost driveable car: current $500 base;
- rebuild of a wreck: current maximum of $500 or 10 percent of purchase price;
- stored-car repair: $250 to $2,500 based on missing body and engine health.

Apply the mapped neighborhood service discount after the base quote. Round the final
price to a readable increment. Do not charge both recovery and repair when the rebuild
quote already includes restoration.

Capturing a damaged car must no longer repair it for free. Storage records health;
retrieval restores that health; the phone offers a repair. A migrated car without
health fields receives full health once so old saves remain usable.

## Technical shape

Add these focused components:

- `Core/Neighborhoods.cs`: district catalog, state/event types, mission-event table,
  migration and pure benefit calculation;
- `Core/NeighborhoodWorld.cs`: free-roam props, peds, vehicles, cooldowns and cleanup;
- `Core/VehicleCondition.cs`: condition normalization, status derivation and repair
  quote calculation.

Modify these integration points:

- `Missions/CampaignState.cs`: save district snapshots and event history; migrate old
  saves; apply events from `MarkComplete`;
- `Core/CampaignPhone.cs` and `Core/CampaignHub.cs`: Neighborhoods app, garage sorting,
  vehicle details, confirmations and receipts;
- `Core/OwnedVehicle.cs`: favorite and health fields with safe defaults;
- `Core/Garages.cs`: return, repair, transfer, owner and favorite actions; duplicate
  spawn protection; neighborhood discount callback;
- `Core/ShopService.cs`: show and apply mapped district discounts;
- `BloodlinesMain.cs`: construct and update `NeighborhoodWorld` under the existing
  free-roam gates and route activity to the phone log.

The pure state resolver must not reference GTA entity handles. `NeighborhoodWorld`
reads the current result and owns every entity it creates. Garage pricing receives a
benefit value from the resolver instead of reaching into mission state directly.

## Delivery plan

### Phase 1 — State, migration and phone visibility

1. Add district definitions, events and save migration.
2. Wire M03, M22, M23, M31, M43, M49 and M60 completion events.
3. Add Neighborhoods cards, timelines and idempotent notices.
4. Add focused tests for event order, Cypress regression, replay and old-save
   migration.

This phase proves story progression without spawning anything in the world.

### Phase 2 — Vehicle management and condition

1. Persist engine/body condition and favorites.
2. Add derived status, favorite sorting and detail pages.
3. Add Return, Repair, Move Garage and Assign Owner.
4. Add duplicate-spawn validation to retrieval, recovery and KJ delivery.
5. Add the Grand Senora bunker garage unlocked by M23.

This phase makes the phone useful even before neighborhood visuals are enabled.

### Phase 3 — Connected benefits

1. Map garages and shops to the three districts.
2. Add quote breakdowns and state-based discounts.
3. Add local recovery offers and Foundry Reclamation.
4. Confirm that mission replay cannot repeat cash, state changes or benefits.

### Phase 4 — Visible world progression

1. Survey the Davis, foundry and bunker anchors in the installed game build.
2. Add state-specific props and one bounded ambient group per district.
3. Add ownership suspension and cleanup around every mission/cutscene transition.
4. Run live road-node, pathing, wanted-level and save/load acceptance passes.

Props and ambient actors stay behind a configuration flag until all three anchors
pass live testing. Phone state and service benefits remain available if visual dressing
is disabled.

## Automated acceptance

- New and migrated saves resolve the correct state from completed missions.
- M22 can move Cypress from Holding to Exposed and the later events can restore it.
- Replaying any source mission creates no duplicate event, notice, discount or reward.
- Save/load preserves state, history, favorites, health and assigned garage.
- A missing new vehicle-health field migrates to a driveable full-health value.
- Return, delivery, retrieve and recovery cannot create two live instances of one ID.
- A stored damaged car returns damaged until Repair completes.
- Discounts apply only to mapped service costs and round consistently.
- A garage transfer fails cleanly when the destination is locked or full.
- Neighborhood runtime updates are blocked during missions, cutscenes, surveys, death
  recovery, home transitions and wanted states.

## Live acceptance

- Complete the M03/M22 Cypress sequence and verify that the physical and phone states
  visibly regress for the stated reason.
- Complete M23/M31 and verify bunker storage, road approach and friendly placement.
- Complete M49/M60 and verify the Davis transition without altering police behavior.
- Test each state at daytime and night, then at 1080p and 4K UI scale.
- Store and retrieve healthy, damaged, modified and wrecked cars for all three heroes.
- Request, track, cancel, return, repair, recover and transfer the same car in separate
  sessions.
- Start a mission while neighborhood actors are active and verify complete cleanup.
- Trigger wanted behavior near every anchor and verify that neighborhood scripts do
  not reinforce or prolong the pursuit.

## Release boundary

The first release is complete when all three district histories work, phone status is
clear, mapped service benefits apply, vehicle condition and management survive
save/load, and neighborhood presentation safely yields to authored missions.

Defer citywide territory capture, passive income, universal gang wars, exact damaged
body recreation, impound simulation, multiple build presets and free-form vehicle
renaming. Each can be evaluated after this loop proves that story progress is visible
and the garage is easier to manage.
