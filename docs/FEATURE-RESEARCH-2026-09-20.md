# Gameplay, interface and mod-platform feature research

Reviewed September 20, 2026 against the current Bloodlines runtime, active GTA V
single-player mods, Rockstar's current feature patterns and Microsoft's game
accessibility guidance.

## Decision

Bloodlines already has the broad systems that many script mods use as their entire
feature set: 79 campaign missions, three playable characters, crew AI and orders,
abilities, a campaign phone, a garage and vehicle market, persistent progression,
mission replay, mission results, and an external mission-assembly hook. The best
next work is therefore depth, recovery and clarity.

Build these in this order:

1. campaign-grade checkpoint recovery;
2. player assists, complete rebinding and scalable interface options;
3. race position, split times and persistent mission records;
4. preparation choices that visibly change later operations;
5. replayable crew contracts assembled from authored variations;
6. a stronger owned-vehicle lifecycle;
7. an in-game compatibility and diagnostics center;
8. a versioned mission-pack format and small creator SDK;
9. bounded neighborhood consequences;
10. post-story challenge tiers and rewards.

Custom interiors, recorded voices and bespoke character models remain valuable
presentation tracks. They should progress independently of the systems above because
they require asset production and live placement work rather than another general
gameplay framework.

## What the research shows

The durable GTA V mods do more than add a menu item. They create a loop with clear
states, variations and recovery:

- LSPDFR combines a contextual request, selectable assistance, varied outcomes and a
  complete resolution state. Its backup menu supports response types and a chosen
  location, while current callout packs use randomized behavior, investigation steps
  and contextual dispatch updates. Bloodlines can apply that structure to crew jobs
  without imitating police play. Sources: [LSPDFR assistance guide](https://www.lcpdfr.com/wiki/lspdfr/04/features/requesting-assistance/),
  [LSPDFR feature guide](https://www.lcpdfr.com/lspdfr/legacy/features/), and
  [Expanded Callouts](https://www.lcpdfr.com/downloads/gta5mods/scripts/55328-voice-interaction-expandedcallouts/).
- Persistence Pro makes ownership useful through per-character garages, named cars,
  GPS selection, repair respawn, unstuck recovery, map filtering and automatic
  recapture of modifications. Those service actions matter more to Bloodlines than
  adding another large vehicle catalog. Source: [Persistence Pro](https://www.gta5-mods.com/scripts/persistence-save-vehicles-enchanced).
- Menyoo 2.0's recent quality-of-life work centers on search and filters, sortable
  statistics, saved tuning favorites, snapping and autosaves. Its lesson for
  Bloodlines is that a large catalog becomes usable only after retrieval, comparison
  and recovery are excellent. Source: [MenyooSP releases](https://github.com/itsjustcurtis/MenyooSP/releases).
- Rockstar repeatedly uses a readable chain of target, preparation, finale and
  reward. Auto Shop contracts use two planning missions and a finale; Salvage Yard
  robberies use initial intel, preparations, free-roam tasks and a finale; LSA
  Operations add optional replay goals. Career Progress then exposes tiered
  challenges and rewards. Sources: [Los Santos Tuners update notes](https://support.rockstargames.com/articles/7a5MsGMCeLTCp3ek6LEWrA/gtav-title-update-1-54-notes-ps4-xbox-one-pc),
  [The Chop Shop overview](https://www.rockstargames.com/gta-online?info=2a1k4),
  [San Andreas Mercenaries](https://www.rockstargames.com/newswire/article/175k8294o31ooo/gta-online-san-andreas-mercenaries-now-available), and
  [Career Progress update notes](https://support.rockstargames.com/articles/6Dzat0mIjSMSSvLAoBZE9D/gtav-title-update-1-67-notes-ps5-ps4-xbox-series-x-s-xbox-one-pc).
- Microsoft's current guidance recommends multiple difficulty presets plus individual
  assists, complete input remapping with prompts that reflect the chosen bindings,
  configurable and scalable text, readable subtitles, consistent navigation and an
  always-available objective review. Sources: [difficulty options](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/108),
  [input](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/107),
  [text display](https://learn.microsoft.com/gaming/accessibility/xbox-accessibility-guidelines/101),
  [subtitles and captions](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/104), and
  [objective clarity](https://learn.microsoft.com/en-us/gaming/accessibility/xbox-accessibility-guidelines/109).
- ScriptHookVDotNet's own user guide warns that runtime pieces must come from the same
  release and records a compatibility break between stable 3.6.0 and newer GTA V game
  builds. A Bloodlines health screen should report the detected combination before a
  player has to interpret a crash log. Source: [ScriptHookVDotNet user guide](https://github.com/scripthookvdotnet/scripthookvdotnet/wiki/User-Guides).

## Priority features

### 1. Campaign-grade checkpoint recovery

**Player value:** Very high · **Cost:** High · **First release:** Three
representative missions, then expand

This is the largest usability gap in a 79-mission campaign. The current checkpoint
snapshot stores character transforms, health and wanted level, but it cannot recreate
mission actors, vehicles, timers or private state. Enabling it globally would create
unreliable retries.

Use semantic checkpoint recipes instead of restoring entity handles. A checkpoint
should record a stable state key, selected approach, important choices, required
inventory and the health/state of mission-critical assets. Restoration should tear
down the failed attempt, rebuild the mission from clean setup and apply that semantic
state before control returns. The failure screen should offer **Retry checkpoint**,
**Restart mission** and **Abandon** only when each action is actually valid.

Pilot the contract on one combat mission, one driving mission and one late multi-stage
mission. Keep the M19-M22 continuous Port Heist as an explicit one-sitting operation
until the new contract proves it can reconstruct joined operation state.

**Acceptance:** Repeated death, busted, destroyed-vehicle and manual-retry tests return
the player to the same playable state without duplicate actors, stale blips, skipped
rewards or inherited wreckage.

### 2. Player assists, complete rebinding and scalable interface

**Player value:** Very high · **Cost:** Medium · **First release:** Settings page
in the campaign phone

Add named presets such as **Story**, **Standard**, **Tactical** and **Custom**, then
let players adjust the parts that create different barriers:

- incoming damage and companion protection;
- timer leniency and interaction-hold duration;
- race opponent pace and catch-up behavior;
- aiming or marker assistance where GTA exposes a reliable control;
- camera shake, flashing feedback and slow-motion strength;
- HUD scale, subtitle scale, subtitle backing, objective-card duration and marker
  palette;
- hold/toggle behavior for abilities, crew orders and relevant interactions.

Every Bloodlines action should be rebindable in game for keyboard and controller.
Prompts must read the resolved binding instead of embedding `E`, `G`, `F9` or a
controller direction in prose. Show collisions between Bloodlines bindings and known
Bloodlines actions before saving.

Speaker identity should use a name label as well as color. Mission-critical markers
should use shape/icon differences as well as color. Subtitle sizing should reach at
least 200 percent of its default size without clipping the phone, mission HUD or
results card.

**Acceptance:** All campaign menus and mission actions can be completed on keyboard
or controller after rebinding; 100, 150 and 200 percent text settings remain readable
at 1080p and 4K safe-area layouts.

### 3. Race position, splits and persistent records

**Player value:** High · **Cost:** Small to medium · **First release:** SM03 and
every objective using `RaceCheckpointObjective`

The current race objective shows lap and checkpoint count. Add live position, total
racers, the distance or time gap to the next rival, current lap/split, personal best
and a clear wrong-way or missed-gate state. Save best mission time, best race time,
lowest damage and completed bonus goals.

Put those records in the existing journal and result-card flow. Make opponent
catch-up behavior a named setting. Rockstar's current race creator separates classic,
incremental-drag and boosted catch-up; Bloodlines needs only an honest **Off**,
**Balanced** and **Strong** choice with its current pacing limits documented. Source:
[The Chop Shop creator updates](https://www.rockstargames.com/newswire/article/4ko1oa3oo13593/learn-more-about-the-chop-shop).

**Acceptance:** The HUD agrees with the actual race order at every gate, records are
saved per mission, and replaying can improve a record without paying a first-completion
reward again.

### 4. Preparation choices with visible consequences

**Player value:** High · **Cost:** Medium to high · **First release:** One Act II
operation and one Act III operation

The planning board already reads campaign state. Turn selected preparations into
clear choices with specific downstream effects. Examples:

- jammer: delays helicopter response by a stated window;
- armor package: improves the assigned escape vehicle but adds weight;
- forged access: opens a quieter entrance but removes a heavy-weapon option;
- second driver: improves extraction reliability but reduces the payout;
- recon: reveals patrols, cameras or optional targets before deployment.

The board should show **Ready**, **Optional**, **Missing** and **Chosen**, identify the
mission or service that provides a missing item, and state the effect before the player
commits. Two or three meaningful choices per supported operation are enough.

**Acceptance:** Each choice changes a verifiable route, response, asset or objective;
the finale remains completable without optional preparation; retry preserves the
committed plan.

### 5. Replayable crew contracts with authored variation

**Player value:** High · **Cost:** Medium · **First release:** Six templates, two
per brother

Use the existing phone, objective library, locations and external mission dispatch to
build short contracts between story missions. Each template should contain authored
variations in location, target behavior, complication and resolution rather than
unrestricted procedural generation.

Good initial templates are a vehicle recovery for Guess, overwatch or protection for
Ice, and interception or disruption for Gohan. Vary time of day and one meaningful
outcome: cooperation, flight, ambush, decoy or evidence recovery. Give each contact a
cooldown and let the player decline without penalty.

The Gang's time-window jobs and current callout mods show why contextual variations
make a small set feel larger, while early procedural-mission mods also show the
stability cost of composing arbitrary objectives. Sources: [The Gang](https://www.gta5-mods.com/scripts/the-gang) and
[ProcedurallyGeneratedMissions](https://www.gta5-mods.com/scripts/procedurallygeneratedmissions-pgm-0-1-0).

**Acceptance:** Every variation has valid ground/road placement, full cleanup, a clear
resolution and no effect on campaign prerequisites. The contract loop cannot start
during a story mission or continuous operation.

### 6. Owned-vehicle lifecycle and build sheets

**Player value:** High · **Cost:** Medium · **First release:** Existing garage and
KJ phone app

Add a custom name, favorite state, assigned brother, current location, condition,
recovery state and saved build preset to each owned vehicle. From the garage app, the
player should be able to set a route, request delivery, repair or recover a lost car,
unstick a car, move it between owned garages and compare its current build with a
saved preset.

Guess's specialist packages should appear as readable build sheets with measured
tradeoffs and a short test route. Automatically recapture legal modifications when an
owned vehicle returns to a garage, and ask before overwriting a named preset.

**Acceptance:** Vehicle identity and modifications survive save/load, recovery never
duplicates an active car, and menu filters make a large collection faster to use than
scrolling the raw 202-model catalog.

The researched build sequence, shared neighborhood benefits and concrete save/phone
contracts are defined in `NEIGHBORHOOD-VEHICLE-IMPLEMENTATION-PLAN.md`.

### 7. Compatibility and diagnostics center

**Player value:** High · **Cost:** Medium · **First release:** Read-only startup
report plus export

Add a phone or pause-menu page that reports:

- GTA V edition and detected game build;
- ScriptHookV and ScriptHookVDotNet versions when discoverable;
- Bloodlines DLL, data and save schema versions;
- required data folders and missing/corrupt files;
- duplicate Bloodlines assemblies and incomplete upgrades;
- unresolved key collisions within Bloodlines;
- disabled subsystems, last script exception and log location;
- loaded external mission packs and their validation result.

Provide **Copy diagnostic report** and a temporary **Safe mode** that disables optional
visual, handling, external-pack and audio features for the next session. Do not label
another mod incompatible without a reproducible rule; report detected facts and the
subsystem that failed.

**Acceptance:** A mismatched or incomplete install produces a specific actionable
message before mission deployment, and the exported report contains no personal path
beyond the GTA install and Bloodlines data locations.

### 8. Versioned mission packs and creator SDK

**Player value:** Medium · **Cost:** Medium · **First release:** Manifest
validation and one sample pack

The runtime already resolves an external mission class from an assembly. Formalize
that hook with a small manifest containing pack ID, version, minimum Bloodlines API,
mission IDs, prerequisites, optional dependencies and supported game edition. Validate
the manifest before loading the DLL and show pack status in diagnostics.

Publish a narrow API around mission context, objective composition, cleanup ownership,
save namespaces and result reporting. Include a sample contract, schema documentation
and a validator command. A pack must receive its own save namespace and fail without
blocking the base campaign.

**Acceptance:** An incompatible, duplicate or broken pack is isolated with one clear
error; a valid sample pack installs without editing the Bloodlines assembly or base
campaign files.

### 9. Bounded neighborhood consequences

**Player value:** Medium to high · **Cost:** High · **First release:** Davis,
Cypress Flats and one Blaine County district

Store three authored pressure states per supported district rather than simulating a
citywide territory economy. Story choices and selected contracts can change local
patrol composition, friendly ambient presence, supply discounts, recovery-job
frequency and phone/news reactions. Every state change needs a reason the player can
see in the journal.

**Acceptance:** Consequences survive save/load, never create an inescapable wanted
loop and stop updating during authored missions that own the same area.

See `NEIGHBORHOOD-VEHICLE-IMPLEMENTATION-PLAN.md` for the selected Davis, Cypress
Flats and Grand Senora timelines and the phased implementation plan.

### 10. Post-story challenge tiers and rewards

**Player value:** Medium to high · **Cost:** Medium · **First release:** Ten
representative missions, then arc completion pages

Extend the existing tally and progression app with transparent replay goals: time,
damage, accuracy where measurement is reliable, character switches, optional
objectives and preparation-specific completions. Use three descriptive tiers and show
the requirement before replay. Reward garage builds, outfits, safehouse mementos and
alternate starting loadouts rather than another large cash payout.

**Acceptance:** Goals are computed from data the mod can measure accurately, first-run
story completion stays separate from challenge completion, and rewards cannot be
claimed twice.

## Delivery sequence

### Release A: clarity and resilience

- Race HUD and records.
- Settings page, scalable UI and subtitle treatment.
- Resolved-input prompts and binding conflict checks.
- Read-only compatibility report.
- Checkpoint contract plus one combat pilot.

### Release B: ownership and replay

- Two more checkpoint pilots.
- Vehicle naming, condition, recovery and build sheets.
- Mission record pages and the first challenge tiers.
- Diagnostic export and safe mode.

### Release C: living campaign

- Two prepared operations with visible consequences.
- Six authored crew-contract templates.
- Versioned pack manifest, validator and sample pack.
- First three bounded district states.

Live acceptance remains the gate between releases. Automated walkthroughs can prove
state transitions and cleanup, but only in-game play can validate road nodes, interiors,
camera clearance, vehicle physics and the readability of the HUD over GTA's changing
backgrounds.

## Features to defer

- A generic XP tree would duplicate mission milestones and equipment unlocks without
  strengthening the three-character campaign.
- A large passive-business economy would add upkeep and menus before the existing
  preparation, garage and contract loops have enough depth.
- A full citywide turf simulation would compete with authored mission ownership of the
  same peds, blips and districts. Start with bounded district states.
- More vehicle categories would increase catalog friction. Search, ownership and build
  retrieval should come first.
- A broad trainer menu would overlap established tools and weaken the campaign's
  intended state. Keep development and survey tools behind the existing dev flag.
