# Act III opening: the map for M49–M53

Status: `PLANNED_NOT_IMPLEMENTED`. Nothing here is built. This is the implementation
map for the next block, written the way the offshore, marine and final preparation
blocks were mapped before they were coded.

The written treatment already exists in
[pass-02/PLAN.md](story-to-play/pass-02/PLAN.md) and the row-level intent in
[CAMPAIGN-REMAINDER.md](CAMPAIGN-REMAINDER.md). This document does not repeat them.
What it adds is the thing those two could not have: **what is actually at each site**,
which stages map onto systems that already exist, and the decisions that have to be
made before any of it is written.

## Why this block, and what it unlocks

M49–M53 is the whole opening movement of Act III: get back into the county, break the
warrant feed, prepare the blackout, remove Harrison, clear the tunnels. Five chapters,
the same size as the last two blocks.

It is also the block that opens the rest of the solo campaign. **SM07, SM08 and SM09
all carry `insert_after 52`** — they are gated behind Judicial Strike, and they are the
last three solo jobs in the game. Finishing M49–M53 therefore unlocks the work that
completes all nine solo missions.

| | before this block | after it |
|---|---:|---:|
| main missions with scripts | 48 of 70 | 53 of 70 |
| solo missions with scripts | 6 of 9 | 6 of 9, with the last 3 unlocked |
| playable jobs | 54 of 79 | 59 of 79 |

These five are **separate jobs, not an operation.** Paleto and the Port Heist are one
sitting each because the fiction is one continuous raid. M49–M53 happen on different
nights in different districts, and `CAMPAIGN-REMAINDER.md` already treats them as five
ordinary missions with their own briefings. Nothing here goes in
`MissionManager.Continuations` and nothing here needs `ContinuousOperation`.

## Where these places actually are

**Every coordinate below came out of the game's own zone table, not memory.** That
matters: my first attempt at Palmer-Taylor was a remembered coordinate, and it was
250 m north of the real station, in the Murrieta oil field. A survey of the wrong
place looks exactly like a survey of the right one.

| Mission | Site | Zone | Verified center |
|---|---|---|---|
| M49 | Chumash county line | `CHU` | (-3096, 1194) |
| M50 | Rockford Hills archives | `Rockf` | (-972, 335) |
| M51 | Palmer-Taylor Power Station | `Palmpow` | (2685, 1308) |
| M52 | City Hall plaza / Union Depository roof | `Downt` / `PBOX` | (-377, -617) / (5, -648) |
| M53 | Metro tunnels under Pillbox Hill | `PBOX` | (5, -648) |

Only **Palmer-Taylor has been surveyed** so far (1,528 entities within 200 m). The
other four are named and centered but not inspected. That survey work should happen
before any of their coordinates are authored, for the reason the last four playtest
failures all shared.

---

## M51 — Blackout Protocol (surveyed; the one with a real answer already)

Bible: *"planting timed EMP limpet charges across the six main 500kV step-down
transformers."* `CAMPAIGN-REMAINDER.md`: *"six EMP units in two separate transformer
sections."*

**There are no transformer props at Palmer-Taylor.** The switchyard is baked map
geometry. What the site carries in placeable props is four `prop_elecbox_02b`, a
couple of pylons, and a tank farm.

But the beat survives intact, because **six `prop_storagetank_02b` units exist on
site, in two clusters of three** — which is the two-section arrangement the plan
already asked for:

| section | unit | position |
|---|---|---|
| lower yard | 1 | (2653.61, 1459.84, 23.46) |
| | 2 | (2653.61, 1501.69, 23.46) |
| | 3 | (2653.61, 1504.84, 23.46) |
| upper yard | 4 | (2753.56, 1462.11, 29.79) |
| | 5 | (2756.40, 1471.54, 29.79) |
| | 6 | (2757.97, 1477.37, 29.79) |

109 m apart, two working heights (23.5 and 29.8). Six charges on six things that are
really there, in two sections a mission can ask two brothers to split between.

Stages:

1. **Reach the yard** — `TravelObjective` to the perimeter, crew in one vehicle.
2. **Lower section** — `MultiHoldObjective` over units 1–3, `OwnedBy(Ice)`.
   This is M44's mooring-clamp pattern exactly: hold position at each point until the
   charge bites.
3. **Upper section** — `MultiHoldObjective` over units 4–6, `OwnedBy(Guess)`.
   Switchable, so the player can do either section as either brother.
4. **Failover override** — `MissionInteraction` at the control room, `OwnedBy(Gohan)`.
5. **Regroup and arm** — `EnterVehicleObjective` with the new `CrewBoarding` so nobody
   is left in the yard, then the arming beat.

`GuardAwareness` owns the station detail; `NonlethalGuards` is available if the beat
should stay quiet.

**The blackout itself is a decision, not a detail.** The authored consequence is
*charges ready* — the outage is meant to fire later, for the downtown offensive.
`Core/WorldLights` already exists for exactly this and already handles the hard part
(the engine has one lights switch and no getter, so holders are named and the lights
return only when the last one lets go). What it needs from you is **when the lights go
out**: at the end of M51, or held until a later tower mission. Those are different
missions. See the decisions below.

**Charge placement uses the new surface probe.** `MissionSites.SurfaceHeight` and
`OnSurface` (added for the Paleto deck) put a marker on the real top of a real tank
instead of on an authored guess — the same class of bug as M45's floating marker.

---

## M49 — Return to the Concrete

Bible: Guess rams the roadblock at 90 mph, Ice snipes tower generators, Gohan jams
dispatch. `CAMPAIGN-REMAINDER.md` already refuses the ram: *"Avoid requiring a 90-mph
collision against an indestructible prop."*

The shape that works instead, and which the treatment already implies: **the lane is
opened, then driven.** Ice kills the towers, Gohan buys the window, and the gap becomes
drivable because the defense was beaten — not because the Granger is stronger than
concrete.

1. **Approach from the north** — `TravelObjective`, the crew in M48's preserved
   technical or a staged Granger. *(M48 calls `Preserve(_technical)`, so the vehicle
   carries over; whether M49 should open in it or in the turbine Granger the bible
   names is a decision.)*
2. **Take the towers** — `KillTargetsObjective` over two generator props,
   `OwnedBy(Ice)`, from a lookout. The distance-scaled marker added for M26 applies
   here: this is another long-range target set.
3. **Buy the window** — `MissionInteraction`, `OwnedBy(Gohan)`. This is a bounded
   delay on dispatch, not amnesty. `TacticalResponse` owns police timers today and is
   where the delay belongs.
4. **Run the lane** — `TravelObjective` through the gap, `OwnedBy(Guess)`.
5. **Everyone goes through** — Ice comes down off the hillside and boards. `CrewBoarding`
   exists now; before it, this stage is the M47/M48 bug again and would wait forever.

Needs surveying: the checkpoint road itself. A barricade needs a real lane with room
for concrete barriers, two APCs and a drivable seam — and the Great Ocean Highway at
Chumash is a specific piece of road, not a generic one.

---

## M52 — Judicial Strike (the one with an exact precedent)

**M41 already is this mission.** Bradley's chapter runs identify → wait for a clear shot
→ eliminate → recover → extract, with real failure paths for shooting too early and for
the target escaping after an alarm. M52 is the same machine with a different target:

| M41 | M52 |
|---|---|
| observe from the lookout, identify the officer | Ice identifies Harrison leaving City Hall |
| spare the lodge worker | do not shoot into the bodyguard detail or bystanders |
| wait until he walks clear | wait for the marked firing window |
| eliminate before he reaches transport | the two-shot strike |
| recover the card | *(no pickup — the death is the result)* |
| Guess drives the crew to the exit | Guess extracts from the plaza |

So M52 should be written against M41's file as the template, including its
`_identified` / `_safeShot` / `_alarmAt` guards, which already fail the mission for the
wrong target and for a premature shot.

Two things to settle. The firing position is a **roof** — Union Depository across the
plaza — which means a mapped roost and a usable way down, and the roof height has to
be probed rather than authored, for the M45 reason. And the bible says Guess extracts
Ice **on a superbike**, which pass-02 already flags as conflicting with a three-person
crew; a bike is two seats at most.

---

## M50 — The Redacted Vault

The honest blocker is stated in `CAMPAIGN-REMAINDER.md` and has not changed: there is
**no installed walkable municipal archive interior.** The choices are an exterior
service-terminal adaptation, or an installed MLO.

Everything else is available today. Gohan's access work is `ProximityHack` plus
`MissionInteraction`. Ice containing private security is `NonlethalGuards` plus
`GuardAwareness` — and this is the mission where Gohan's **Blackout** ability is most
at home, since its whole rule is that what he does unseen is deferred rather than
charged. Guess holding the van outside is `ProtectObjective` plus, at the end,
`CrewBoarding` so Ice and Gohan physically return to it.

The result language is already locked by the treatment and worth repeating because it
is easy to overclaim: **the coordinated municipal feed is invalidated. Local and
physical copies remain.** Not "the police forget every crime."

---

## M53 — Subterranean Sweep

Two ambushes on two tunnel junctions, two contractor squads, night vision and
proximity mines.

Night vision needs no new system: **Ice carries Thermal Pulse**, which is heat
signatures through geometry, and a dark tunnel is the best possible showcase for it.
That is a nice alignment — the ability Ron reassigned to Ice in this same session is
the one this mission was written around.

Mines are `prop_ld_bomb_01`, already used elsewhere in the campaign, placed with
`MultiHoldObjective` at two junctions the way M44 clamps charges. The squads are
`KillTargetsObjective` with `GuardAwareness` owning their escalation, and first contact
triggering the second squad is `ReactionTrigger`, which M26 already uses.

The blocker is geometry: **which subway sections are actually accessible**, and the
plan's own rule that no mandatory objective goes inside an inaccessible train car. The
metro tunnels under Pillbox Hill need a survey before a single coordinate is authored.

---

## What this block needs that does not exist yet

| Need | Status |
|---|---|
| Six charge points at a real switchyard | **Solved** — six `prop_storagetank_02b`, positions above |
| Ordering a brother into a vehicle | **Exists** — `CrewBoarding`, added this session |
| A marker on the real top of a structure | **Exists** — `MissionSites.SurfaceHeight` / `OnSurface` |
| A long-range target you can keep track of | **Exists** — distance-scaled destroy marker |
| Holding the city's lights off, safely shared | **Exists** — `Core/WorldLights` |
| Sniper identify / clear-shot / escape machinery | **Exists** — M41, to be used as the template |
| Thermal vision in a dark tunnel | **Exists** — Ice's Thermal Pulse |
| A walkable municipal archive interior | **Missing** — M50 needs an adaptation or an MLO |
| Accessible subway sections | **Unknown** — needs a survey |
| The Chumash checkpoint road | **Unknown** — needs a survey |
| A roof roost on the Union Depository | **Unknown** — needs a survey and a probed height |

## Decisions I need from you

1. **When do the lights actually go out?** M51 can end with the charges armed and the
   outage held for a later downtown mission, or it can black the district out at the
   end of M51. The authored consequence says armed-and-ready; the later tower missions
   are what it is for.
2. **M50's archive.** Exterior service terminal on real geometry, or wait for an
   installed interior? The exterior version is buildable now; the interior version is
   better and is blocked on an asset.
3. **M52's extraction.** The bible says a superbike, which seats two. Keep the bike and
   have Ice ride pillion while the others leave separately, or use a car so the crew
   leaves together?
4. **M49's vehicle.** Open in M48's preserved technical, or stage the turbine Granger
   the bible names?

## Before any of it is coded

Survey the four unsurveyed sites, in this order — each one gates coordinates that
cannot be honestly authored without it:

1. Metro tunnels under Pillbox Hill (M53 — accessibility is the whole question)
2. The Chumash county-line highway (M49 — the lane has to exist)
3. City Hall plaza and the Union Depository roof (M52 — two linked positions)
4. Rockford Hills archives (M50 — decides decision 2 above)

Nothing in this document has been run in game, and no coordinate here except the six
Palmer-Taylor positions has been checked against installed geometry.
