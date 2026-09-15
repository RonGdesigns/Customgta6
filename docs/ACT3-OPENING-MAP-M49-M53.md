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

All five sites have now been surveyed against the installed archives. Status is
`OFFLINE_CHECKED`: named geometry checks ran with identified inputs. Nothing here has
been seen in game.

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

**Every site coordinate came out of the game's own zone table, not memory.** That
matters: my first attempt at Palmer-Taylor was a remembered coordinate, and it was
250 m north of the real station, in the Murrieta oil field. The survey came back clean
and detailed — of the wrong facility. A survey of the wrong place is indistinguishable
from a survey of the right one.

| Mission | Site | Zone | Verified center | Survey |
|---|---|---|---|---|
| M49 | Chumash county line | `CHU` | (-3096, 1194) | 2,014 entities |
| M50 | Rockford Hills | `Rockf` | (-972, 335) | 1,407 entities |
| M51 | Palmer-Taylor Power Station | `Palmpow` | (2685, 1308) | 1,528 entities |
| M52 | City Hall plaza | `Downt` | (-544, -204) | 1,346 entities |
| M52 | Union Depository | `PBOX` | (2, -667) | 1,384 entities |
| M53 | Metro under Pillbox Hill | `PBOX` | (0, -650) | 1,386 entities |

---

## M51 — Blackout Protocol

Bible: *"planting timed EMP limpet charges across the six main 500kV step-down
transformers."* `CAMPAIGN-REMAINDER.md`: *"six EMP units in two separate transformer
sections."*

**There are no transformer props at Palmer-Taylor.** The switchyard is baked map
geometry. What the site carries in placeable props is four `prop_elecbox_02b`, three
pylons, and a tank farm.

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

109 m apart, two working heights. Six charges on six things that are really there, in
two sections a mission can ask two brothers to split between.

Stages: reach the yard (`TravelObjective`); lower section (`MultiHoldObjective` over
1–3, `OwnedBy(Ice)`); upper section (`MultiHoldObjective` over 4–6,
`OwnedBy(Guess)`), switchable so either brother can take either section; failover
override (`MissionInteraction`, `OwnedBy(Gohan)`); regroup with `CrewBoarding` so
nobody is left in the yard, then arm.

The charge markers use `MissionSites.SurfaceHeight` / `OnSurface`, added for the Paleto
deck, so a marker sits on the real top of a real tank rather than on an authored guess.

**The blackout itself is a decision, not a detail.** The authored consequence is
*charges ready* — the outage fires later, for the downtown offensive. `Core/WorldLights`
already exists for this and already handles the hard part. What it needs from you is
when the lights go out. See the decisions below.

---

## M52 — Judicial Strike

**The bible's firing position does not work, and the survey found the one that does.**

The bible puts Ice *"on the roof of the Union Depository across the plaza"* from City
Hall. Those two buildings are **716 m apart** — not across a plaza, and far outside a
workable engagement. The Depository has plenty of roof (360 entities above z 40, up to
z 149) but it is the wrong building for this shot.

81 m from City Hall there is a roof at z 53.3 — **15.3 m above the plaza** — and
Rockstar put a ladder on it, named `bh1_16_ladder_mission_fizz`, which is a ladder
placed for a mission. The roof surface runs x −653…−580 by y −284…−203, so there is
room to move on it. Two further ladders sit closer in, at z 45:

| ladder | position | distance to City Hall |
|---|---|---|
| `bh1_21_ladder1` | (-528.9, -176.9, 45.0) | 31 m |
| `bh1_21_ladder2` | (-579.7, -206.2, 45.0) | 36 m |
| `bh1_16_ladder_mission_fizz` | (-615.9, -240.8, 53.3) | **81 m** |

So the roost is the 81 m roof, reached and left by a ladder that already exists — which
satisfies the treatment's requirement for *"a usable, mapped firing position and safe
descent"* without inventing either. The roof height still needs the runtime probe rather
than the authored 53.3, for the M45 reason.

**M41 already is the rest of this mission.** Bradley's chapter runs identify → wait for
a clear shot → eliminate → recover → extract, with working failure paths for the wrong
target and a premature shot:

| M41 | M52 |
|---|---|
| observe from the lookout, identify the officer | Ice identifies Harrison leaving City Hall |
| spare the lodge worker | do not shoot into the detail or bystanders |
| wait until he walks clear | wait for the marked firing window |
| eliminate before he reaches transport | the strike |
| recover the card | *(no pickup — the death is the result)* |
| Guess drives the crew to the exit | Guess extracts from the plaza |

M52 should be written against that file, keeping its `_identified` / `_safeShot` /
`_alarmAt` guards.

---

## M49 — Return to the Concrete

Bible: Guess rams the roadblock at 90 mph, Ice snipes tower generators, Gohan jams
dispatch. `CAMPAIGN-REMAINDER.md` already refuses the ram: *"Avoid requiring a 90-mph
collision against an indestructible prop."* The shape that works: **the lane is opened,
then driven** — the gap becomes drivable because the defense was beaten.

1. **Approach from the north** — `TravelObjective`.
2. **Take the towers** — `KillTargetsObjective` over two generator props,
   `OwnedBy(Ice)`. The distance-scaled destroy marker added for M26 applies: another
   long-range target set.
3. **Buy the window** — `MissionInteraction`, `OwnedBy(Gohan)`. A bounded delay on
   dispatch, not amnesty; `TacticalResponse` owns police timers and is where it belongs.
4. **Run the lane** — `TravelObjective` through the gap, `OwnedBy(Guess)`.
5. **Everyone goes through** — Ice comes down and boards. `CrewBoarding` exists now;
   without it this stage is the M47/M48 bug and waits forever.

**What the survey can and cannot tell us here.** The site is Chumash village — houses,
roof vents, air conditioning units, rock clusters. There is exactly **one gate prop**
in 300 m and no existing barricade furniture, so every barrier, tower and APC is
something the mission spawns. That is fine, and it means the spawn positions must not
foul the village geometry.

It also means the **lane itself cannot be located from the archives.** Roads are baked
terrain, not placed entities, so a placed-entity survey cannot find the Great Ocean
Highway. The checkpoint position has to come from `GameUtils.NearestRoadNode` at
runtime or from an F11 capture — and F11 is now safe for this, because a capture in the
wrong place says so out loud. The inland spots that read as "open" in the survey are
open because nothing is placed there, not because they are road; do not mistake one for
the other.

---

## M50 — The Redacted Vault

**There is no municipal archive in Rockford Hills.** The zone is residential: patio
loungers, house gates, mansion walls, benches. No civic building, and therefore no
archive interior to wait for.

But read the bible again: *"Gohan hacks an **underground optical conduit** into the
Rockford Municipal Records archive."* The interaction that is written is a **conduit
tap**, not a walk through a records room — and **44 street electrical cabinets** are
placed across the zone. The exterior adaptation is not a downgrade; it is what the
synopsis says.

One cabinet sits 3 m from ground that is clear for 111 m:
**`prop_elecbox_06a` at (-872.95, 410.42, 86.10)** — a tap point and a van position in
the same place. Other candidates, all real:

| | position |
|---|---|
| `prop_elecbox_11` | (-1085.62, 318.04, 64.79) |
| `prop_elecbox_05a` | (-1073.92, 291.71, 62.93) |
| `prop_elecbox_11` | (-1064.69, 333.58, 65.75) |
| `prop_elecbox_06a` | (-976.52, 354.01, 71.46) |

Everything else is available today. Gohan's access work is `ProximityHack` plus
`MissionInteraction`. Ice containing private security is `NonlethalGuards` plus
`GuardAwareness` — and this is the mission where Gohan's **Blackout** is most at home,
since its rule is that what he does unseen is deferred rather than charged. Guess holds
the van with `ProtectObjective`, and `CrewBoarding` gets Ice and Gohan back into it.

The result language is locked by the treatment and is easy to overclaim: **the
coordinated municipal feed is invalidated. Local and physical copies remain.**

---

## M53 — Subterranean Sweep

Two ambushes at two tunnel junctions, two contractor squads, night vision and mines.

Night vision needs no new system: **Ice carries Thermal Pulse**, heat signatures
through geometry, and a dark tunnel is the best showcase it will get. Mines are
`prop_ld_bomb_01`, already used elsewhere, placed with `MultiHoldObjective` the way M44
clamps charges. Squads are `KillTargetsObjective` with `GuardAwareness` owning
escalation, and first contact triggering the second squad is `ReactionTrigger`, which
M26 already uses.

**The survey came back with a negative result that decides the shape of this mission:
there is not a single placed entity below z = 0 within 260 m of Pillbox Hill.** The
subway is not a set of placed props — it is a handful of large models whose origins sit
at street level:

| model | origin |
|---|---|
| `metro_30` | (0.7, -526.8, 20.5) |
| `metro_end` | (29.4, -539.6, 20.5) |
| `metro_sm` | (55.4, -554.6, 19.3) |
| `metrotest` | (-57.2, -524.5, 20.5) |
| `kt1_00_tunnel` | (-64.8, -551.1, 34.8) |

A model origin is not a floor. **There is nothing positional to survey here**, so every
M53 coordinate has to come from an in-game capture, and the plan's own rule — no
mandatory objective inside an inaccessible train car — cannot be verified offline
either. This is the one site in the block where the archives genuinely cannot answer the
question, and it is why M53 should be the last of the five to be written, not the first.

---

## What this block needs that does not exist yet

| Need | Status |
|---|---|
| Six charge points at a real switchyard | **Solved** — six `prop_storagetank_02b` |
| A mapped sniper roost with a real way down | **Solved** — 81 m roof, three ladders |
| A conduit tap point and a van spot together | **Solved** — cabinet at (-872.95, 410.42, 86.10) |
| Ordering a brother into a vehicle | **Exists** — `CrewBoarding` |
| A marker on the real top of a structure | **Exists** — `MissionSites.SurfaceHeight` |
| A long-range target you can keep track of | **Exists** — distance-scaled destroy marker |
| Holding the city's lights off, safely shared | **Exists** — `Core/WorldLights` |
| Identify / clear-shot / escape machinery | **Exists** — M41, to be used as the template |
| Thermal vision in a dark tunnel | **Exists** — Ice's Thermal Pulse |
| A walkable municipal archive interior | **Not needed** — the written beat is a conduit tap |
| The Chumash checkpoint lane | **Runtime** — road node or F11; not in the archives |
| Accessible subway track level | **F11 only** — nothing is placed underground |

## Decisions I need from you

1. **When do the lights actually go out?** M51 can end with the charges armed and the
   outage held for a later downtown mission, or black the district out at the end of
   M51. The authored consequence says armed and ready; the later tower missions are
   what it is for.
2. **M52's extraction.** The bible says a superbike, which seats two. Keep the bike with
   Ice riding pillion while the others leave separately, or a car so the crew leaves
   together?
3. **M49's vehicle.** Open in M48's preserved technical, or stage the turbine Granger
   the bible names?

M50's archive question is answered and no longer needs a decision: the conduit tap is
what the bible asked for, and the cabinets are there.

## Order to write them in

1. **M51** — fully grounded; six real positions, every system it needs already exists.
2. **M52** — roost found, and M41 is the template.
3. **M50** — tap points found; the only open work is choosing one and placing security.
4. **M49** — needs one F11 capture on the highway lane first.
5. **M53** — needs F11 for everything; the archives cannot answer it.

Nothing in this document has been run in game. The geometry claims are archive reads
with identified inputs, and no coordinate here has been walked on.
