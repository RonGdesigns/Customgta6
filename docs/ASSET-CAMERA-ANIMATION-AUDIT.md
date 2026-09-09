# Bloodlines — Stock Asset, Camera, Interior & Animation Audit

**Status:** Production planning / no mission implementation changed  
**Scope:** Prologue, M01–M70, SM01–SM09  
**Goal:** Keep the campaign achievable in GTA V using installed/stock game content wherever possible. When a described beat cannot be simulated directly, preserve the story with a verified stock substitute, camera cheat, prop attachment, staged animation, or off-camera state change.

---

## 1. Production rule: cutscenes must move

A cutscene is not just three frozen peds while dialogue plays. Unless a scene is intentionally still, every scene should use at least two of the following:

- character locomotion: walking, turning, pacing, approaching a vehicle, entering/exiting, taking cover;
- object interaction: phone, clipboard, laptop, door, weapon, crate, bag, vehicle hood/trunk, table, radio;
- environmental action: traffic, workers, aircraft, boats, weather, alarms, doors, lights, explosions;
- camera movement: track, dolly, orbit, over-shoulder, low vehicle angle, crane/high establishing shot, rack between speakers;
- a real gameplay handoff: scene ends on the actor already seated, already walking, already aiming, already driving, or already at the first objective.

The existing `CutsceneDirector` already supports scripted cameras, temporary actors, stock props/vehicles, scene actions, radio framing, skip, and control restoration. It should be expanded toward blocking/motion rather than replaced.

### Animation strategy

Prefer stock GTA tasks/scenarios before hunting exact bespoke animations:

- walk/run/follow/navigation tasks;
- enter/leave vehicle and seat changes;
- drive/fly/boat tasks;
- stand mobile / phone / clipboard / smoking / leaning / guard scenarios;
- weapon aim, cover, reload, combat and sniper tasks;
- parachute, swimming, diving and climbing/ladder movement;
- looped generic interaction animation for hacking, cutting, planting, repairing, checking equipment;
- prop-in-hand staging when the exact object interaction is not exposed;
- short fade/camera cut between the approach and the completed interaction when GTA has no reliable animation.

**Do not make the exact animation dictionary a story requirement.** The story should require a readable action—"Gohan installs the tap"—not a single irreplaceable animation clip.

---

## 2. Capability categories

| Code | Meaning |
| --- | --- |
| **S** | Stock/direct: GTA systems and locations can do the beat normally. |
| **SS** | Stock substitute: story works, but the exact described room/object should be represented by a verified existing GTA location/interior/prop. |
| **CF** | Camera fake: preserve the described event with camera cuts, attachments, off-camera teleports, prop swaps or scripted paths. |
| **RV** | Revise visual description if live testing shows the stock representation is not convincing. Story purpose can remain. |

**Important:** the existing `FEASIBILITY.md` is still the baseline technical classification (46 Green, 26 Yellow, 7 Red). This document adds production treatment: location, camera, interiors and character movement.

---

# PROLOGUE — Ron Returns to Los Santos

**Capability: S**

**Location:** LSIA exterior/terminal frontage → public road network → Ron's Burro Heights Chop Shop exterior/home marker.

**No new asset required.** Do not require a custom furnished apartment for the opening. The current home system already supports Ron's Burro Heights location as an exterior access point.

### Suggested moving cutscene

1. High establishing shot over LSIA / aircraft taxiing in the background.
2. Cut to Ron walking out with a small bag or phone in hand.
3. Ron stops, looks at Los Santos, checks his phone.
4. Camera tracks beside him as he walks to the parked car/rental/ride vehicle.
5. Ron opens the door and gets in using the stock vehicle-entry task.
6. Camera settles behind the car.
7. **Player control returns while Ron is already seated.** Objective: drive home.
8. At Burro Heights, player parks.
9. Short exterior scene: Ron exits, walks toward the shop door/garage area, checks the job message, then the M01 setup begins.

**Animations/tasks:** walking, phone use, vehicle entry, driving, vehicle exit, short idle/inspection. All stock-capable.

**Camera tricks:** none required beyond normal scripted camera placement and a clean handoff to gameplay.

**Verdict:** fully achievable stock-only and should be one of the first cinematic systems proven in game.

---

# ACT I / EARLY CAMPAIGN

| Mission | Cap. | Location / interior plan | Moving cutscene + animation plan | Camera / substitution notes |
| --- | --- | --- | --- | --- |
| **M01 Ghost in the Dockyard** | S | Terminal Island docks, warehouse exterior/bay, yacht/boat area | Ice walks to overwatch and shoulders rifle; Gohan approaches terminal/laptop and works it; Ron remains in/near approach car, exits or drives into Bay 2; recognition happens with all three retaining real positions | Existing radio framing is correct. Four-seat prototype/getaway required. No conversational teleport. |
| **M02 Loose Strands** | S | Freeway/industrial roads | Start around/inside Granger; Ron drives, Gohan works device/laptop/phone from passenger seat, Ice exits to recover drives | Use moving vehicle side-window cameras and brief close-ups; HUD carries distance rules. |
| **M03 Cypress Foundry** | SS | Cypress Flats industrial yard/workshop; use exterior workshop shell if furnished interior is unavailable | Trio walks through site, points out entrances, moves crates/vehicle, Ron handles keys/garage area | Do not require a custom foundry interior. Dress a stock industrial yard with vehicles, tool props and work lights. |
| **M04 Severed Wire** | S | Garage + subway/tunnel route | Miller arrives/leaves, crew moves between car and pursuit positions; use weapon-ready transitions | Use real tunnel/garage geometry; no static briefing lineup. |
| **M05 Tidal Lock** | SS | Coast/cliff + existing cave/coastal recess as Mateo hideout substitute | Ice takes cliff position, Ron/Gohan move to boat/shore; Mateo walks/paces or checks table/phone before capture | If exact sea cave is unavailable, use a verified stock cave/coastal tunnel. Keep the chase exterior. |
| **M06 Clean Sweep** | S/SS | Vespucci/industrial depot; exterior yard can substitute interior | Gohan cuts feeder at panel, Ice breaches gate/door, Ron pulls extraction vehicle around | Interaction animations can be generic panel/plant loops. Camera follows the actor performing each task. |
| **M07 Wiretap Waltz** | CF | Radio/antenna tower + rooftop | Gohan/crew walk to mast base; ladder/climb represented by short climb + camera cut to platform; Ice jumps/BASE exits | GTA lacks a reliable bespoke mast-climb sequence. Cut from lower ladder to upper platform while camera looks away/up. |
| **M08 Supply & Sever** | S | Depot/warehouse yard | Ron drives forklift/vehicle, Ice covers, Gohan checks inventory/device | Crate pickup can be prop attach / hold-zone. Keep actors walking around equipment between lines. |
| **M09 Rolling Thunder** | CF | Highway convoy | Ice/crew moves between helicopter and convoy; boarding IFF vehicle staged after convoy slows/stops | Do not physically land skids on moving roof. Use off-camera slowdown/attach or short cut. |
| **M10 Open Throttle** | S | Freeway/bridge/tunnel | Ron in loaded flatbed; Ice/Gohan ride/cover; action dialogue happens in motion | One of best stock-direct missions. Camera should mostly remain gameplay with occasional short vehicle-mounted cuts. |
| **M11 Ironclad Dyno** | S | Workshop exterior/interior shell | Ron opens hood, circles vehicle, checks engine; Ice leans/inspects; Gohan enters with tablet/phone | Static dyno can be UI over stationary vehicle. Use hood/engine inspection movement so scene is alive. |
| **M12 Black Tide Recon** | CF | Coast/water | Gohan enters submersible/boat; camera switches to locked ROV-style view; Ron/Ice wait/reposition topside | Represent ROV as stock submersible camera or camera-only drone mode. |
| **M13 Smuggler's Cut** | S | Marina/boats | Crew walks docks, plants charges, moves away, boats react/explode | Generic plant animation + prop/charge; camera tracks along dock. |
| **M14 Airspace Blackout** | S | Airfield/hangar + low-altitude route | Ron enters aircraft, Ice/Gohan prep equipment, takeoff happens directly into gameplay | SAM fire = scripted rockets/fixed launch points. |
| **M15 Crawlspace** | SS | Service tunnel/maintenance room substitute | Gohan crouch/walks into utility space, uses panel; Ice performs quiet takedown/cover | Use stock service/utility interior or tunnel. Do not require bespoke maintenance vault. |
| **M16 The Heavy Lift** | S | Fort Zancudo / Cargobob route | Crew approaches under cover, Ron/selected pilot enters Cargobob, Ice covers | Stock hostile-base and Cargobob mechanics. |
| **M17 Sub-Zero Payload** | S | Workshop/staging area | Gohan/Ron/Ice walk around payload, attach/inspect parts, test release | Use prop attachments and generic repair/inspection animations. |
| **M18 The Staging Line** | S | Port staging yard | Each vehicle arrives/parks; trio walks between vehicles while checking plan | Use blocking around vehicles rather than three-person lineup. |
| **M19 Port Heist: Underwater Breach** | CF | Port water / submerged hull area | Gohan dives, swims to hull, performs looped cutting action | Hold-zone + particles/bubbles; container rise is scripted prop movement. |
| **M20 Port Heist: Sky Hook** | CF | Port/airspace | Ron/crew boards Cargobob; Ice/Gohan cover; sling moment shown with close camera | Attach container rather than simulate 30-ton physics. Script path after hook. |
| **M21 Port Heist: Open Water** | S | Ocean/coast | Boat escort, hostile launches, flares | Mostly gameplay; short moving boat cameras during transitions. |
| **M22 Scorched Bay** | CF | Port → Cypress | Crew watches strike from vehicle/shore position; actors move toward cover as impact occurs | Missile strike is camera event: distant projectile/sound/explosion/smoke, then pre-damaged/smoking site. Establish story limitation on repeat strikes. |

---

# ACT II / BLAINE COUNTY & RIG BUILDUP

| Mission | Cap. | Location / interior plan | Moving cutscene + animation plan | Camera / substitution notes |
| --- | --- | --- | --- | --- |
| **M23 Ghost in the Sage** | S/SS | Senora bunker/outpost exterior or verified bunker substitute | Trio clears rooms/yard then physically walks through exits while discussing base | If a desired bunker interior is unavailable, use stock bunker-like exterior/underground space already loadable in install. |
| **M24 Liquid Gold** | CF | Alamo/shore/industrial crane area | Crew operates crane/control point, watches dredge rise | Hold-zone/control animation + prop moved upward; do not simulate real dredging. |
| **M25 Bounty Hunters' Canyon** | S | Canyon/bridge/river | Ice fights from position; Ron arrives with extraction; Gohan communicates | Keep rescue physical: Ron drives/boats in and Ice actually boards. |
| **M26 Alamo Scramble** | S/RV | McKenzie/Grapeseed airfield | Pilots run to aircraft, enter, take off | Stock aircraft dogfight works. Resolve Lazer continuity: either establish access or use a justified stock aircraft. |
| **M27 Flight Risk** | CF | Airfield → sky → static cabin substitute → water | Ron flies alongside; Ice moves to boarding point; cut to static jet-cabin substitute where Ice fights; cut to bailout; Gohan waits in boat | Never attempt true plane-to-plane walking or zero-G AI. Three-cut illusion preserves story. |
| **M28 Off the Grid** | CF | Repeater/tower | Gohan/Ice climb to service point, work panel, descend | Ladder + camera cut for unsupported climb sections. |
| **M29 Dust & Diesel** | CF | Desert rail corridor | Ron drives truck alongside train; boarding/loading beat staged | Freeze/slow train or attach truck during cut; do not rely on two-body moving physics. |
| **M30 Redline Ridge** | S | Mountain/desert roads | Ron drives; Gohan navigates from map/device; Ice covers | Natural moving vehicle dialogue and chase. |
| **M31 Iron Perimeter** | S | Bunker/outpost perimeter | Trio walks perimeter placing mines/turrets; Ron checks exit route | Scripted mines/turret logic; generic plant/inspect animation. |
| **M32 Black Site Zancudo** | SS | Zancudo exterior + stock tunnel/utility interior substitute | Gohan dives/enters intake substitute; Ice/Ron infiltrate; Ramos is physically escorted out | Warhead cases = props. Exact intake interior can be replaced by verified service tunnel/under-base space. |
| **M33 Informant's Grave** | S | Salt flats/desert | Ramos rescue: carry/escort or help-to-vehicle staging, smoke cover | Use standard injured/escort approximation; if carry animation is unreliable, cut from kneeling aid to Ramos seated in vehicle. |
| **M34 Mud & Iron** | S/CF | Desert/sandstorm | Half-track/vehicle escort; Gohan triggers technical solution/rockslide | Weather settable. Rockslide = explosions + falling/placed rock props + camera shake, not terrain deformation. |
| **M35 Chianski Ambush** | S | Mountain pass/convoy | Crew takes positions, convoy enters, ambush launches | Direct stock combat/vehicle tasks. |
| **M36 Deep Well Recon** | CF | Coast/rig approaches | Gohan enters sub/drone view, surveys; Ron/Ice reposition topside | Same camera-drone/substitute method as M12. |
| **M37 Grapeseed Harvest** | S | Grapeseed airfield/farms | Aircraft theft/retrofit; actors walk around aircraft and install smoke gear | Retrofit is staged inspection/repair animation + prop/config change. |
| **M38 Blood in the Quarry** | S | Davis Quartz quarry | Crew advances using haulers/cover; workers flee/duck | Existing quarry is ideal. Stage workers as ambient/fleeing peds to sell collateral concern. |
| **M39 Paleto Cable** | CF | Deep water off Paleto | Gohan dives to cable, clamps thermite, surfaces/returns | Hold-zone + particles/sparks/bubbles. Cable itself can be a placed prop/marker; exact seabed cable geometry not required. |
| **M40 Phantom Rigging** | S | Marina/shore staging | Trio physically boards/inspects boats, attaches armor props, starts engines for test | Stock boats + attached decorative props if safe; otherwise imply armor via camera and stats. |
| **M41 General's Wire** | S/SS | Mountain lodge / verified house-lodge substitute | Ice approaches, enters, stalks Bradley, retrieves card, exits to snowmobile | Exact lodge can be any verified stock house/lodge-like interior/exterior. Blizzard/weather is stock. |
| **M42 Skyfall Delivery** | CF | Airfield → Titan → ocean | Ron walks cargo bay/checks latches; Gohan enters Kraken; plane takes off; cargo ramp beat; cut to sub already under parachutes; splashdown | Do not simulate a heavy submarine falling cleanly out of Titan. Use three staged states. |
| **M43 Staging Paleto** | S | Paleto/shore staging | Each asset arrives; trio walks between sub/boat/chopper and checks readiness | Perfect place for a moving planning montage. |
| **M44 Rig Sub-Surface** | CF/SS | Existing large industrial platform/ship/deck substitute + water | Gohan dives/places charges; Ice waits in aircraft; Ron holds extraction position | If no convincing stock offshore rig is available, use a verified large marine/industrial structure and frame tightly. Story calls it the rig; camera avoids skyline giveaways. |
| **M45 Rig Helipad Breach** | SS | Same platform substitute | Ron flies approach; Ice lands/exits, moves through deck combat; Gohan surfaces from moonpool-equivalent access | Exact moonpool may be substituted by lower deck/ladder/water access. Storm/weather and helipad combat are stock-capable. |
| **M46 Rig Vault Crack** | SS | Stock secure-room/vault/industrial interior substitute | Gohan swipes card/uses terminal; bags/bond props are picked up; Ice covers; Ron coordinates exit | Use a verified secure interior already in game/install. The story needs "secure command vault," not a unique custom architecture. |
| **M47 Rig Collapse** | CF | Same rig/platform substitute → open water | Crew runs to jump point while alarms/explosions occur; jump/parachute/swim/boat boarding are real | Structure itself does not deform. Use camera shake, explosions, smoke, hidden swap/disable pieces, then force attention to jump. |
| **M48 Road Back South** | S | Coastal/highway route | Crew boards technicals and drives through blockade | Direct stock driving/combat. |
| **M49 Return to the Concrete** | S | Chumash roadblock/highway | Ron drives Granger, Ice snipes lights/towers, Gohan supports | Strong stock Ron clutch. Spawn roadblock components rather than depend on ambient traffic. |

---

# ACT III / LOS SANTOS ENDGAME

| Mission | Cap. | Location / interior plan | Moving cutscene + animation plan | Camera / substitution notes |
| --- | --- | --- | --- | --- |
| **M50 The Redacted Vault** | SS | Government/archive/office interior substitute | Crew walks archive aisles/rooms; Gohan works terminals, Ice clears sentries, Ron handles exit | Exact archive not required. Use verified office/storage/interior with shelves/servers dressed by stock props. |
| **M51 Blackout Protocol** | S | Transformer/power yard | Crew climbs/walks between banks, plants charges, moves to safe point before detonation | Downtown blackout can be scripted lighting/time/world effect; no custom asset. |
| **M52 Judicial Strike** | S | Courthouse exterior/rooftop sightline | Ice sets rifle, tracks Harrison walking; Ron waits/moves bike/car into extraction | Target walk path and sniper aim are stock-capable. |
| **M53 Subterranean Sweep** | S | Subway tunnels | Crew advances tactically, sets tripwires/mines, takes cover | Existing tunnels support combat; night vision stock. |
| **M54 Pillbox Redoubt** | SS | High-rise/penthouse substitute with rooftop/balcony sightline | Trio enters, walks room, sets transmitter, looks toward Maze Bank | Use any verified high-rise interior/roof combination with suitable sightline; do not block on a bespoke penthouse. |
| **M55 Skyline Descent** | CF/SS | Three separate verified high-rise interiors/rooftops | Each protagonist enters/breaches own space; forced switch happens while previous actor continues task/combat; then each exits to jump | This mission needs careful scripted staging more than new art. Interiors may be substitutes, but three distinct spaces and one timer are required. |
| **M56 Iron in the Drain** | S | LA River/flood-control canal | Ron drives armored vehicle, Ice uses turret/weapon, Gohan handles air threat | Excellent stock drivable set piece. |
| **M57 Vespucci Flak** | S | Beach/marina/coast | Jet-ski/boat movement, Stinger use, pier escape | Direct stock movement/combat. |
| **M58 Cartel Decapitation** | S/SS | Mirror Park house/compound exterior; use accessible house/compound substitute if needed | Crew approaches from street, breaches, enemies flee rear, tear gas/smoke | The exact "villa" architecture can be any convincing stock residential compound. |
| **M59 Wire Cutters** | S/CF | Relay/tower/Vinewood sign | Gohan installs broadcast hardware; Ice changes sniper positions; Ron tracks Davis threat | Tower climb may use same ladder-cut trick. Broadcast itself is audio/UI/world state. |
| **M60 Siege of Davis** | S | Davis streets | Neighbors run to cover; trio moves block to block; Ron/locals create roadblock; Gohan installs EMP; Ice relocates to overwatch/roof | Keep this highly kinetic and personal. Use stock Davis streets, vehicles, residents, cover and rooftops. No new asset needed. |
| **M61 The Black Box** | SS/CF | Downed gunship wreck in water + wreck/interior stand-in | Gohan swims/dives into wreck area, cuts server free; Ice fights divers nearby | Full walkable flooded gunship interior is unlikely stock. Use wreck prop/exterior cavity and tight underwater camera; cut from entry to server position if necessary. |
| **M62 Steel Horizon** | CF | Coastal rail line + boat lane + helicopter lane | Ice moves atop train, Gohan parallels by boat, Ron flies; forced switching sells simultaneity | Container pickup = attach after alignment. Script all three lanes; do not depend on free physics. |
| **M63 Tower of Glass** | S/SS | Maze Bank exterior/lobby or verified financial-tower substitute | Vehicle breach, trio exits, fights across lobby, Gohan accesses security core | Repo feasibility says Maze Bank ground floor is usable; still live-verify on Enhanced. Substitute a verified tower lobby if needed. |
| **M64 The 80th Floor** | CF/SS | Stairwell/elevator-shaft substitute | Trio actually climbs stairs/landings and fights; falling elevator car is a scripted prop event; continue upward | Do not require 80 unique floors. Reuse/loop a convincing stairwell section with camera/fade transitions and floor-number UI. |
| **M65 Executive Privilege** | SS | Boardroom/executive-floor substitute | Vance paces/stands at glass/window; Ice enters and advances; Gohan works terminal after confrontation; Ron stays near route back | Need a verified executive interior, not necessarily exact Maze Bank top floor. Camera can frame skyline tightly to sell location. |
| **M66 Spire Evacuation** | S | Rooftop/tower → parachute route → Del Perro | Trio runs to edge, verbally confirms, jumps, glides, lands near extraction | Stock parachute mechanics. Spawn extraction truck after landing approach. |
| **M67 Scorched Grid** | S | Moving semi/truck | Gohan works laptop/device while Ron drives and Ice watches rear/side | Use vehicle-interior/side cameras and seated animations; gameplay can continue under dialogue. |
| **M68 Blood Brothers: The Drain** | S | Canal | Ron drives 18-wheeler, Ice mans weapon, Gohan deploys mines/tech | Direct stock set piece with scripted enemies. |
| **M69 Blood Brothers: Runway 30L** | CF | LSIA runway/taxiways | Trio drives toward plane while scripted airliner crosses/lands; enters cargo aircraft | Spawn aircraft on exact paths/timing. Do not rely on ambient airport AI. If collision risk is unstable, cut around near-miss. |
| **M70 Blood Brothers: Grounded Titan** | CF | LSIA/tarmac → seawall → water | Ice physically holds rear ramp/cover, Gohan fights/works route, Ron moves to cockpit; Titan taxis under player/script control; final seawall event cuts to water/boats | Siege is direct stock. Seawall launch is staged path + camera cut + water-state swap. Preserve Ron's clutch reveal: "Plane ain't flying" / "I didn't say we flying." |

---

# SIDE MISSIONS

| Mission | Cap. | Location / interior plan | Moving cutscene + animation plan | Camera / substitution notes |
| --- | --- | --- | --- | --- |
| **SM01 Lead & Kevlar** | S | Warehouse | Ice walks/breaches, clears guards, loads crates into trunk | Stock warehouse combat and vehicle loading implication. |
| **SM02 Zero-Day Injection** | SS/CF | Lifeinvader/office/server-annex substitute | Gohan climbs/enters roof, stuns guards, uses terminal, exits fire escape | Exact annex/laser grid can be substituted. Laser sweeps = visual/trigger volumes. Remove marker language from speech. |
| **SM03 Midnight Drift** | S | Street race route | Ron/KJ walk around cars at start; drivers enter; race; post-race gun threat | Existing race framework. Keep KJ physically clear after start. |
| **SM04 Dead Drop Quarry** | S | Quarry/ridge | Ice hikes/positions, uses scope, relocates between nests, recovers radios | Excellent stock Ice showcase. |
| **SM05 Black Box Estuary** | S/CF | Swamp/estuary | Gohan launches boat, dives to buoy, works harness, returns to pickup | Buoy = stock prop/substitute; splice = hold-zone + generic interaction. |
| **SM06 Canyon Runner** | S | Raton/canyon → McKenzie | Ron boards tanker, drives under attack, unloads/parks | Direct stock driving. |
| **SM07 Blood Debt** | SS | Hotel/executive-suite substitute | Ice enters hotel, uses elevator/stair cut, breaches suite, advances on Sterling, exits into rain | Use any verified upscale apartment/hotel interior. Do not require a unique hotel. Keep aftermath terse rather than static confession scene. |
| **SM08 Burner Protocol** | SS/CF | Office-tower interior substitute | Gohan infiltrates, uses vault/terminal, places thermite strips, walks away as fire/smoke starts | Burn effect = particles, alarms, smoke, fade; do not simulate full structural fire. |
| **SM09 The Long Exit** | S | Impound/streets/port | Ron steals hypercar, drives pursuit, loads car into container/ship area | Container delivery can end with car parked inside/open container and door close/camera cut. |

---

# OPTIONAL CHARACTER SIDE MISSIONS — STOCK-FIRST DESIGN

These are optional additions from the character-development pass. All should remain low-asset so they increase personality without increasing art dependency.

## SM10 — Missed Calls (Gohan)

**Capability: S**  
**Location:** Little Seoul streets/parking structure/rooftop or utility alley.  
**Beat:** Ron and Ice have been trying to reach Gohan. Player finds him buried in a technical problem. A minor hostile/technical complication occurs. Gohan produces a weird over-engineered solution that works.

**Movement:** Gohan walks between equipment points, works phone/device, ducks into cover, triggers solution. Ron/Ice can arrive physically at the end rather than appearing in a static dialogue scene.

## SM11 — Backup Plan (Ron + Ice)

**Capability: S**  
**Location:** industrial road/garage/highway.  
**Beat:** Simple two-man recovery turns bad. Ron improvises route; Ice executes a controlled clutch that clears the opening Ron needs.

**Movement:** almost entirely vehicle/combat gameplay with short moving intro/outro around the car.

## SM12 — Old Route (Trio)

**Capability: S**  
**Location:** Davis → old neighborhood route → industrial destination.  
**Beat:** Low-stakes drive where old memories emerge naturally, interrupted by a small problem rather than a giant conspiracy event.

**Movement:** drive, stop, get out, inspect an old spot, get back in; neighborhood ambience sells history.

## SM13 — Three Seats (Trio)

**Capability: S**  
**Location:** Ron's Burro Heights shop + city restaurant/diner exterior/parking area.  
**Beat:** Ron gets both men physically together for something normal. Gohan is late/non-responsive. Small practical problem becomes the mission.

**Movement:** Ron closes shop/garage, Ice arrives by car, Gohan eventually shows up, all three use one four-seat vehicle. This should feel like real-life friendship, not therapy.

---

# CUTSCENE BLOCKING STANDARDS

## A. Intro scenes

Every mission intro should answer four questions visually before the objective starts:

1. **Where are we?** Establishing shot or recognizable environment.
2. **What are the characters doing already?** Walking, driving, loading, checking gear, watching a target.
3. **Who is taking point for this phase?** Shown by action, not rank language.
4. **What state does gameplay inherit?** Seated in car, standing at breach, in boat, on roof, etc.

Avoid: fade in → three peds stand shoulder-to-shoulder → dialogue → fade out → teleport to gameplay.

## B. Mid-mission scenes

Mid-mission cutscenes should be short and preserve state:

- freeze only what must be frozen;
- leave actors in their actual seats/positions where possible;
- use radio framing when separated;
- start enemy escape/door opening/vehicle motion on the final line;
- skip must trigger the same action as watching the scene.

M01's current recognition implementation is the model for this.

## C. Outro scenes

Outros should happen **after the real completion condition**, not instead of it.

Good examples:

- everybody physically reaches the getaway zone;
- required evidence is actually delivered/copied;
- all three surviving protagonists are inside the extraction vehicle when the story says they leave together;
- the target is confirmed alive/dead as the narrative requires.

Then the camera can take over for a short moving aftermath.

---

# ANIMATION / INTERACTION LIBRARY TO BUILD ONCE

Rather than hunting a unique animation for every mission, build reusable wrappers and test them live once:

1. **Phone check / call** — standing and seated versions.
2. **Tablet/laptop hack** — standing-at-table, seated-at-desk, crouched-panel versions.
3. **Plant / clamp / splice** — kneel or crouch + hand interaction + optional prop/particles.
4. **Inspect / repair vehicle** — hood/trunk/side inspection, tool-in-hand if available.
5. **Load / unload** — carry small crate/bag; for heavy objects use two-stage cut rather than fake impossible lifting.
6. **Brief while walking** — actor walks to car/door/edge while line plays.
7. **Overwatch / sniper setup** — walk to point, crouch/prone substitute if reliable, aim, scope shot.
8. **Guard / lookout** — stand watch, turn/head-look, weapon low-ready.
9. **Injured assist** — kneel/check, then cut to passenger seat if full assisted-walk is unreliable.
10. **Vehicle handoff** — actor enters correct seat while camera tracks, then control returns.
11. **Dive / surface / boat board** — stock swimming/diving/vehicle entry.
12. **Parachute prep / jump** — run to edge, short verbal confirmation, real jump.

These reusable blocks will make scenes feel authored without needing bespoke animation assets.

---

# STOCK-ONLY INTERIOR POLICY

Do **not** commit story logic to an interior until that interior is live-verified on the user's GTA V Enhanced install.

For each interior-dependent mission:

1. survey a candidate stock/interior shell in game;
2. verify entry, collision, navmesh/AI movement, camera, combat and restart behavior;
3. record the chosen coordinate/interior in the location data;
4. if it fails, swap to another stock space while preserving the mission's narrative function.

### Highest-priority interior verification list

- M05 sea-cave/hideout substitute;
- M15 maintenance/utility vault;
- M23 bunker/base interior if used;
- M32 Zancudo intake/black-site substitute;
- M41 lodge;
- M44–M47 offshore-rig/platform and secure-room substitutes;
- M50 archive/vault;
- M54 penthouse/redoubt;
- M55 three breach interiors;
- M61 wreck cavity;
- M63 tower lobby;
- M64 stairwell/elevator shaft;
- M65 executive boardroom;
- SM02 server annex;
- SM07 hotel suite;
- SM08 office/archive.

If any of these cannot be made convincing stock-only, revise the **visual description**, not the mission purpose.

---

# CAMERA TRICK LIBRARY

Use these repeatedly and intentionally:

- **Foreground wipe:** pass behind vehicle/wall/pillar and relocate/attach entity while hidden.
- **Whip-pan:** fast pan + motion blur to hide spawn/swap.
- **Explosion shake:** hide damaged-prop swap during flash/smoke.
- **Door/elevator cut:** actor enters door/elevator; cut to verified substitute interior.
- **Ladder cut:** begin real climb, cut to upper landing, continue real climb/walk.
- **Vehicle-mounted shot:** camera fixed beside/behind moving car/boat/aircraft while dialogue plays.
- **Scope/optic insert:** Ice's POV while target path continues.
- **Device insert:** Gohan's screen/terminal close-up while world actors continue moving.
- **High establishing cut:** location clarity before control returns.
- **State-preserving radio cut:** camera stays with active player while distant brother speaks over comms.
- **Scripted-path near miss:** aircraft/train/vehicle follows authored path; camera angle sells proximity without requiring dangerous free AI.
- **Static-interior illusion:** exterior vehicle moves on scripted path while player scene occurs in a non-moving interior stand-in.

---

# HARD LIMITS — DESCRIPTION MUST NOT PROMISE MORE THAN GTA CAN SHOW

The following should always be described in production terms rather than literal engine simulation:

- deforming/collapsing large buildings or offshore structures;
- believable free-body physics between two moving vehicles for long durations;
- true zero-G combat inside a flying aircraft;
- a fully physical heavy submarine airdrop;
- realistic heavy cargo winch physics;
- unique bespoke interiors that are not verified to exist;
- exact complex mechanical maintenance animations;
- large-scale environmental destruction that permanently alters map geometry.

All of those can still appear in the story through staged states and camera language.

---

# IMPLEMENTATION ORDER

1. **Prologue proof:** LSIA moving cutscene → Ron enters car → player drives to Burro Heights → moving home/job scene → M01.
2. Expand `CutsceneDirector` with reusable blocking helpers: walk-to, enter-vehicle, face/look-at, phone, generic interact, camera track, camera orbit, door/elevator cut.
3. Live-test the 15 high-priority interior substitutes above.
4. Build the reusable animation/interaction library once.
5. Prove one mission from each hard category:
   - M01 state-preserving scene;
   - M27 static-interior aircraft illusion;
   - M42 heavy-cargo staged-state illusion;
   - M47 structural-collapse illusion;
   - M55 multi-location forced-switch scene;
   - M62 three-lane convergence;
   - M70 grounded-Titan seawall finale.
6. Only after those pass live testing, lock final cutscene descriptions for the rest of the campaign.

---

# Final assessment

**The campaign is achievable without requiring a custom asset pack, provided the production accepts stock substitutions and Rockstar-style camera cheats where the exact described geometry or physics does not exist.**

The biggest risk is not basic gameplay; it is over-promising exact interiors or physically simulating cinematic set pieces. The solution is to lock the narrative function first, then verify a stock location/animation/camera treatment before implementation.

The prologue, most street/vehicle/combat missions, Davis material, desert missions, airport material, canals, roads, boats, aircraft, parachuting and standard infiltration can all be built from stock systems. The offshore rig, specialized vaults, high-rise interiors, aircraft-interior boarding, large cargo physics and structural collapse require substitution or camera staging but do not require changing the story if handled deliberately.
