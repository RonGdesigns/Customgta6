# Pass 02 — Paleto site report

Status: `OFFLINE_CHECKED`. Everything below was read out of the archives installed
on this machine (GTA V Enhanced, `gta5_enhanced.exe`, 5,407 archives, 19,941 map
files) with the survey tool described at the end. Nothing here has been walked to
in game. An entity origin is a model's own pivot, not a standing position, so
every coordinate is a candidate for an F11 capture rather than a surveyed one.

This answers the open decision `P01-D06` — "use a verified connected offshore,
marine or industrial adaptation before locking coordinates" — with what the
installed game actually has.

## The authored site does not exist

There is no offshore rig or platform in the installed content. A search of every
map file found oil derricks (`prop_oil_derrick_01` and siblings) as land props
only: no deck, no interior, no traversable structure over water. Building one out
of props would mean inventing geometry and collision, which the planning handoff
rules out and which is the same class of risk that made the vegetation pack
unplayable.

## Candidate A — Paleto Cove, the anchored yacht

A 122-meter multi-deck vessel with a real interior, anchored in Paleto Cove.

| Fact | Value |
| --- | --- |
| Hull entity | `h4_mp_apa_yacht` at (-1748.00, 5328.75, 5.43) |
| Hull file | `h4_islandx_yacht_03.ymap`, 5 entities |
| Hull extents | (-1810.10, 5312.54, -4.57) to (-1687.87, 5349.36, 16.93) |
| Interior | `h4_mpapa_yacht`, an MLO, at (-1769.67, 5334.14, 4.89) |
| Interior file | `h4_islandx_yacht_03_int.ymap` |
| Interior extents | (-1781.36, 5319.91, 4.67) to (-1727.63, 5343.95, 14.76) |
| Loading | **Script-loaded.** Both files carry the scripted flag (0x1) |
| Seabed nearby | about -10 m: `cs1_08_sea_uwb02` at (-1775.71, 5300.40, -10.43) and `marina_xr_rocks_06` at (-1689.48, 5292.96, -11.08) |

Why it fits the owner-locked flow: 4.6 m of hull below the waterline and about
10 m of water under it for the M44 diving work; roughly 12 m of structure above
water across three interior deck levels for the M45 insertion and the M46 secure
room; open water alongside for the M47 pickup; and the cove sits at the north end
of the road that M48 runs south, which is where M49 at Chumash expects the crew
to arrive.

What is unverified: whether the helipad and the interior rooms are furnished and
navigable in Story Mode, and what the deck heights actually are at the stern.
Only a live visit settles those.

The cost of choosing it: **the mission has to request the IPL.** The game does not
place this yacht on its own, so M44 asks for those two files by name and the
operation owns that request for its whole run — released on pass, failure, abort
and teardown, the same contract the atmosphere overrides already follow. Every
yacht anchorage in the game is script-loaded this way, including the older heist
yacht off Pacific Bluffs, so this cost is not specific to Paleto Cove.

The story cost: the target stops being an industrial rig and becomes a floating
command vessel. That is a change to the authored premise, not a staging detail.

## Candidate B — the west-coast sunken freighter

| Fact | Value |
| --- | --- |
| Entity | `cs3_06_sea_shipwreck` at (-3192.77, 3029.77, -35.85) |
| File | `cs3_06_sea_strm_0.ymap` / `hei_cs3_06_sea_strm_0.ymap` |
| Loading | **Always loaded.** No scripted flag, no request needed |
| Seabed in that tile | down to -103 m, with the surrounding file reaching -210 m |
| Distance from the cove | about 2.7 km |

This is real deep water on permanent geometry, and it needs nothing requested. It
suits M44's submarine work better than the cove does. It cannot host M45 and M46:
it is 36 m under the surface with nothing above it.

Three other wrecks exist at comparable depth — (3907, 3035, -22.58), (-2818, -581,
-65.96) and (3413, 6332, -50.81) — and all are further from Paleto.

## What this leaves to decide

1. Whether the operation accepts a script-requested yacht as its physical site,
   and with it the change from a rig to a command vessel.
2. Whether M44's diving phase happens under the yacht in about 10 m of water, or
   at the freighter in 36 m with a transit between the two.

Until those are settled, M44–M48 keep `PLANNED_NOT_IMPLEMENTED`. The shared
mechanics they will run on are done and independent of the answer: see
`ContinuousOperation` and `OperationWorld`, which carry a whole operation and are
proven against chapter ids the campaign does not contain.

## How to reproduce this

The survey tool lives outside this repository, with the other lab tooling, and
reads the installed archives through CodeWalker's library. It writes nothing.

```
MapSurvey <gameFolder> --near X Y [Z] --radius R [--name s] [--models]
MapSurvey <gameFolder> --info --ymap <substr>
```

`--info` is what settles whether a place exists in Story Mode without being
asked for: it reports each map file's scripted flag, entity count and extents.
