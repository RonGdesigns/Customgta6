# Coordinate resources and surveying workflow

Reviewed September 12, 2026. No single list identifies every useful mission
position and the correct floor. These resources substantially reduce guessing.

| Resource | What it supplies | How it helps Bloodlines |
| --- | --- | --- |
| [CodeWalker](https://github.com/dexyfex/CodeWalker) | 3D world/RPF viewer, selectable entity transforms, YMAP placement and traffic-path inspection | Inspect the actual installed foundry, roads, rooftops and prop geometry instead of choosing district centers |
| [DurtyFree coordinate datasets](https://github.com/DurtyFree/gta-v-data-dumps) | Road nodes, interior placements, zone bounds and selected object categories such as cameras and electrical boxes | Find nearby candidate road entries, hacking targets, buildings and props programmatically |
| [Bob74 interior definitions](https://github.com/Bob74/bob74_ipl) | IPL anchors and configurable interior entity sets | Pair an interior coordinate with the assets required to make it appear; used for the Foundry shell |

CodeWalker's [current GTA folder implementation](https://github.com/dexyfex/CodeWalker/blob/master/CodeWalker/Utils/GTAFolder.cs)
recognizes `GTA5_Enhanced.exe` and Gen9. Use an Enhanced-capable version against
`C:\Program Files\Rockstar Games\Grand Theft Auto V Enhanced`. Its source supports
this mode; we have not installed/launched CodeWalker or validated every map asset
on this machine in this pass.

The coordinate-dump README reports 67,454 road nodes and interior placements,
plus object-specific dumps. Those are reference candidates: they may describe
different game content or an interior that is not currently loaded. Building and
prop origins are not necessarily a safe player standing position. A 2D point can
also identify a road, roof, tunnel and interior at different heights.

## What is already automated

`tools/validate_locations.py` compares authored coordinates to cached game zone
bounds. This catches district mistakes; it does not prove walkable floors.
The placement tour now queues all of a mission's locations, visits them one by
one, preserves drafts, saves atomically and keeps the menu open. Survey teleport
waits on collision and rolls back on timeout. The room mapper samples nearby
collision and records prop model hashes/positions in `Bloodlines.Room.txt`.
M09 road setup can request nearby road nodes; that is a candidate placement
correction, not proof that its whole driving route is clear.

## Next useful integration

1. Inspect/export the relevant area through CodeWalker, including vertical
   geometry and YMAP transforms, or fetch the relevant coordinate dataset.
2. Keep extracted game data under `build/` or the local install, not in source or
   release packages. Record source/version and distinguish world transforms from
   interior-local coordinates.
3. Join each authored mission key to nearby candidates by intended use: road
   vehicle entries to road nodes; hacking objectives to suitable props; crew
   standing positions to navigable floor near the intended interaction.
4. Present proposed changes as previews. Do not overwrite surveyed coordinates,
   snap every point to the nearest road, or mark imported points as surveyed.
5. Run the in-game tour to check collision, facing, cover, doors, actor spacing
   and approach routes, then explicitly save accepted positions.

The reference import/candidate-ranking and a unified visual map overlay remain
future work. This build adds the sequential in-game review loop, not an automatic
survey of the whole map. No broad batch relocation has been applied.
