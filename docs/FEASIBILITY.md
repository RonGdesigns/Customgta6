# Technical feasibility pass

Every mission in the campaign, tiered by how hard it is to make *reliable* in GTA V
— not by how good it is. A Red mission is not a bad mission; it is a mission that
has to be faked convincingly rather than simulated honestly.

| Tier | Meaning | Approach |
|---|---|---|
| **Green** | Stock systems do it | Spawn, task, check. Build these first and build them fast. |
| **Yellow** | Needs a substitute or a piece of content | Usually an interior (MLO), a prop stand-in, or a mechanic expressed as a hold-zone objective. Schedule the content before the script. |
| **Red** | The engine will not simulate it | Fake it with camera cuts, attachments, teleports off-camera and pre-damaged swaps. Rockstar does this constantly; the player only needs to believe the event happened. |

**Counts: 46 Green · 26 Yellow · 7 Red.**

## The rules for faking

The techniques that cover almost every Red and most Yellows:

- **Attach, don't simulate.** A container "slung" from a Cargobob is attached to it.
  A truck "landing" on a flatbed is attached once it is close enough that nobody can
  tell. Physics between two moving objects is where set pieces go to die.
- **Cut to a static interior.** A cabin at 8,000 feet, a sinking bridge, a collapsing
  rig: fade, put the player in a static interior that does not move, play the beat,
  fade out. The plane outside can be scenery on a scripted path.
- **Swap for a pre-damaged version.** Nothing in GTA V deforms structurally. The
  version after the explosion is a different prop, placed while the screen shakes.
- **Move things off-camera.** Teleport the entities a stage needs into place during a
  fade or behind the player. Checkpoint restores do this already.
- **Spawn the traffic you need.** Do not rely on ambient aircraft, trains, or police
  to be where a set piece requires. Spawn them on scripted paths.
- **Express a process as a hold-zone.** Cutting a hull, splicing a cable, dredging
  gold: stand here, hold, particles, done. The objective library has this one.

## The five-mission slice

Before the remaining campaign, prove the machinery on these — one per system:

| Mission | Proves |
|---|---|
| **M01** Ghost in the Dockyard | switching, stealth, combat, companions, vehicles, checkpoints, pursuit |
| **SM03** Midnight Drift | Guess's driving identity and the race framework |
| **SM04** Dead Drop Quarry | Ice's long-range combat identity |
| **SM05** Black Box Estuary | Gohan's stealth/tech identity |
| **M55** Skyline Descent | rapid forced switching under a hard clock — the reason the whole switch system exists |

If those five feel like a shipped expansion, the rest is manufacturing. If they do
not, no amount of remaining missions will fix it.


## Red — 7 missions

| # | Mission | Approach | Notes |
|---|---|---|---|
| M20 | The Port Heist: Sky Hook | set piece | Cargobob winch on a 30-ton container under AA fire. The winch works; the weight does not. Attach the container to the heli and script the flight path with the player in gunner role. |
| M27 | Flight Risk | set piece | Plane-to-plane boarding at 8,000 ft with zero-G interior combat. Fake in three cuts: matched flight (attach player plane to the jet's path), a cut to a static interior standing in for the cabin, then a scripted bail-out. Do not attempt physical zero-G AI. |
| M42 | Skyfall Delivery | set piece | Air-dropping a submarine from a Titan. Fake it: cargo ramp cutscene, cut to the sub already under parachutes, splashdown spawn. |
| M47 | Paleto Deep-Sea: Collapse | set piece | A collapsing, tilting rig. GTA V cannot deform a structure. Do it with a camera-shaken fade, swap to a pre-damaged version of the prop, and put the player in a BASE jump before the "collapse" resolves. |
| M55 | Skyline Descent | set piece | Three simultaneous penthouse breaches on a 300-second clock. This is the mission the whole switch system exists for, and it is worth building properly: three separate spawn sets, one shared timer, forced switching between them. Interiors can be substituted, the mechanic cannot. |
| M62 | Steel Horizon | set piece | Train, boat and helicopter converging with a sling-load pickup. Build it as three scripted lanes with forced switches; the container hook is an attach, not a physics winch. |
| M70 | Blood Brothers: Grounded Titan | set piece | Cargo-plane siege ending with a taxi off the seawall into the ocean. Waves and the holdout are Green; the seawall launch is a scripted vehicle path plus a camera cut into the water. |

## Yellow — 26 missions

| # | Mission | Approach | Notes |
|---|---|---|---|
| M03 | Cypress Foundry | fake | Uncoupling a 40-car freight train is not exposed to script. Freeze a train at a junction and blip it as "blockade"; the depot raid and hauler defence are Green. |
| M05 | Tidal Lock | fake | Sea-cave interior needs an MLO or a substitute (existing Paleto cave). Cliff sniping, flare drops and the boat chase are Green. |
| M07 | Wiretap Waltz | fake | Climbing a mast has no animation set; use a ladder-and-teleport with a camera cut. Drone is a Buzzard at altitude. BASE jump is Green. |
| M09 | Rolling Thunder | fake | Landing skids on a moving truck roof is unreliable physics. Slow the convoy to a stop off-camera, or attach the heli to the truck for the beat. |
| M12 | Black Tide Recon | fake | ROV drone: use a submersible with a locked camera, or a camera-only "drone mode" with no vehicle at all. |
| M15 | Crawlspace | fake | Maintenance vault interior needs an MLO; the tranquilliser takedowns and splice are Green. |
| M19 | The Port Heist: Underwater Breach | fake | Underwater cutting: hold-zone objective with particle effects. Container "rising" is a spawned prop moved on a curve. |
| M22 | The Port Heist: Scorched Bay | fake | Container drop is a prop teleport. The cruise-missile strike on the foundry is a cutscene: fade, explosion, smoke column prop. |
| M24 | Liquid Gold | fake | Crane dredging: hold-zone plus a prop rising out of the water on a timer. |
| M29 | Dust & Diesel | fake | Truck onto a moving flatbed: freeze the train at speed 0 with a moving-camera illusion, or attach the truck once it is close. |
| M32 | Black Site Zancudo | fake | Underwater intake infiltration needs an interior; the warhead cases are props. |
| M36 | Deep Well Recon | fake | Sub recon: same drone treatment as M12. |
| M39 | The Paleto Cable | fake | Deep-water cable cutting: hold-zone at depth with limited air as the tension. |
| M44 | Paleto Deep-Sea: Sub-Surface | fake | Underwater charges on rig pylons; the rig itself needs a prop or an existing platform. |
| M45 | Paleto Deep-Sea: Helipad Breach | fake | Helipad drop in a storm onto a structure that has to exist first. Everything above deck is Green once it does. |
| M46 | Paleto Deep-Sea: Vault Crack | fake | Vault interior — MLO or a substitute interior. |
| M50 | The Redacted Vault | fake | Archive interior needs an MLO; the sentry takedowns and the wipe are Green. |
| M54 | The Pillbox Redoubt | fake | Penthouse interior — MLO. The balcony roost and antenna are dressing. |
| M61 | The Black Box | fake | Flooded gunship interior needs a wreck prop; the underwater fight is Green. |
| M64 | The 80Th Floor | fake | Elevator-shaft ascent with falling cars. Stairwell fights are Green; the falling car is a scripted prop on a path. |
| M65 | Executive Privilege | fake | Boardroom confrontation needs the top-floor interior (an MLO exists in some map packs). |
| M69 | Blood Brothers: Runway 30L | fake | Runway rampage dodging landing airliners. Spawn planes on scripted approach paths rather than relying on ambient air traffic. |
| SM02 | Zero-Day Injection | fake | Server annex interior; laser sweeps are scripted trigger volumes with a visual effect. |
| SM05 | Black Box Estuary | fake | Swamp kayak and dive; the buoy is a prop, the splice a hold-zone. |
| SM07 | Blood Debt | fake | Hotel suite interior — MLO or a substitute. The floor-by-floor fight is Green. |
| SM08 | Burner Protocol | fake | Office tower interior; the incendiary burn is a particle-and-fade sequence. |

## Green — 46 missions

| # | Mission | Approach | Notes |
|---|---|---|---|
| M01 | Ghost In The Dockyard | direct | Ped/vehicle spawns, firefight, scripted escape. Built. |
| M02 | Loose Strands | direct | Freeway intercept on live traffic AI. Built. |
| M04 | Severed Wire | direct | Garage firefight plus subway pursuit. Trains on the line are a hazard, not a system. |
| M06 | Clean Sweep | direct | Depot breach, alley hold, reverse extraction. Interior is an MLO or a walled-off exterior yard. |
| M08 | Supply & Sever | direct | Forklift is drivable; crate load is a hold-zone objective plus a prop attach. |
| M10 | Open Throttle | direct | Escort with a speed floor, RPG from the bed, PIT manoeuvres. The campaign's best Green set piece. |
| M11 | Ironclad Dyno | direct | Static workshop scene; dyno is a UI meter over a frozen vehicle. |
| M13 | Smuggler'S Cut | direct | Limpet charges on boats, then detonate. Straight explosive work. |
| M14 | Airspace Blackout | direct | Hangar theft and a low-altitude run. SAMs are scripted rockets from fixed points. |
| M16 | The Heavy Lift | direct | Zancudo is already hostile territory; Cargobob theft is stock. |
| M17 | Sub-Zero Payload | direct | Static customisation scene, same pattern as M11. |
| M18 | The Staging Line | direct | Staging: park three vehicles, play the countdown speech. |
| M21 | The Port Heist: Open Water | direct | Boat escort with flares and hostile launches. |
| M23 | Ghost In The Sage | direct | Bunker clear-out. Interior is an MLO or the existing Senora structures. |
| M25 | Bounty Hunters' Canyon | direct | Bridge sniping and a river extraction. |
| M26 | The Alamo Scramble | direct | Duster dogfight; weaponised biplanes exist in stock content. |
| M28 | Off The Grid | direct | Tower climb and EMP splice; same ladder-and-cut technique as M07. |
| M30 | Redline Ridge | direct | Downhill trophy-truck run under helicopter fire. |
| M31 | The Iron Perimeter | direct | Mines and turrets: proximity checks plus scripted explosions. CIWS is a scripted turret. |
| M33 | The Informant'S Grave | direct | Salt-flat ambush and rescue. Smoke is a particle effect. |
| M34 | Mud & Iron | direct | Sandstorm escort with a turret. Weather is settable; the rockslide is a scripted explosion plus prop swap. |
| M35 | The Chianski Ambush | direct | Convoy ambush in a pass. Stock vehicles throughout. |
| M37 | The Grapeseed Harvest | direct | Aircraft theft and retrofit; retrofit is a static scene. |
| M38 | Blood In The Quarry | direct | Quarry firefight using haulers as cover. |
| M40 | The Phantom Rigging | direct | Static boat-armouring scene. |
| M41 | The General'S Wire | direct | Lodge infiltration in a blizzard. Weather is settable. |
| M43 | Staging Paleto | direct | Staging scene. |
| M48 | The Road Back South | direct | Highway blockade breakout. |
| M49 | Return To The Concrete | direct | Checkpoint ram with tower sniping. |
| M51 | Blackout Protocol | direct | Transformer yard sabotage; the blackout is a scripted light change over Downtown. |
| M52 | Judicial Strike | direct | Rooftop sniper shot on a scripted walk path, then a bike escape. |
| M53 | Subterranean Sweep | direct | Subway tunnel ambush with mines and night vision. |
| M56 | Iron In The Drain | direct | Canal armour clash. The aqueduct is one of the best drivable spaces in the game. |
| M57 | Vespucci Flak | direct | Jet-ski anti-air run. |
| M58 | Cartel Decapitation | direct | Compound assault in Mirror Park. |
| M59 | The Wire Cutters | direct | Radio-mast climb and broadcast, with sniper cover on the Vinewood sign. |
| M60 | Siege Of Davis | direct | Neighbourhood defence in waves. Reuses SurviveWavesObjective wholesale. |
| M63 | Tower Of Glass | direct | Lobby breach by ramming. Maze Bank's ground floor is enterable. |
| M66 | The Spire Evacuation | direct | Antenna BASE jump into a storm. Stock parachute mechanics. |
| M67 | Scorched Grid | direct | Dialogue scene inside a moving truck. |
| M68 | Blood Brothers: The Drain | direct | 18-wheeler running battle in the canal with a roof turret. |
| SM01 | Lead & Kevlar | direct | Warehouse assault. Built. |
| SM03 | Midnight Drift | direct | Street race under the freeway. Uses RaceCheckpointObjective. |
| SM04 | Dead Drop Quarry | direct | Counter-sniper duel in open terrain. Ideal Ice showcase, no interiors needed. |
| SM06 | Canyon Runner | direct | Canyon fuel-tanker run under bike attack. |
| SM09 | The Long Exit | direct | Impound theft, five-star pursuit, container delivery. |

---

Generated from `data/feasibility.tsv` by `tools/render_feasibility_doc.py`. The tiers are judgement calls made from the bible text, not from testing — revise them as missions are actually built, and treat a Green that turns out Yellow as information about the next twenty missions, not just this one.
