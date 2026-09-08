"""Render docs/FEASIBILITY.md from data/feasibility.tsv joined to the campaign data.

    python3 tools/render_feasibility_doc.py
"""

import csv
import io
import os

# Every generated file in this repo is committed with CRLF, because they were
# all first written on Windows. Writing them with the platform default instead
# turns one regeneration on Linux into a whole-file diff on every line, which
# hides the handful of rows that actually changed. Pinning it makes the output
# the same artifact wherever the tool runs.
CRLF = '\r\n'

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(REPO, 'docs', 'FEASIBILITY.md')

HEADER = """# Technical feasibility pass

Every mission in the campaign, tiered by how hard it is to make *reliable* in GTA V
— not by how good it is. A Red mission is not a bad mission; it is a mission that
has to be faked convincingly rather than simulated honestly.

| Tier | Meaning | Approach |
|---|---|---|
| **Green** | Stock systems do it | Spawn, task, check. Build these first and build them fast. |
| **Yellow** | Needs a substitute or a piece of content | Usually an interior (MLO), a prop stand-in, or a mechanic expressed as a hold-zone objective. Schedule the content before the script. |
| **Red** | The engine will not simulate it | Fake it with camera cuts, attachments, teleports off-camera and pre-damaged swaps. Rockstar does this constantly; the player only needs to believe the event happened. |

**Counts: {green} Green · {yellow} Yellow · {red} Red.**

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

"""


def read(path):
    with io.open(path, encoding='utf-8') as handle:
        return list(csv.DictReader(handle, delimiter='\t'))


def main():
    missions = {row['id']: row for row in read(os.path.join(REPO, 'data', 'missions.tsv'))}
    tiers = read(os.path.join(REPO, 'data', 'feasibility.tsv'))

    counts = {'Green': 0, 'Yellow': 0, 'Red': 0}
    for row in tiers:
        counts[row['tier']] = counts.get(row['tier'], 0) + 1

    out = [HEADER.format(green=counts['Green'], yellow=counts['Yellow'], red=counts['Red'])]

    for tier in ('Red', 'Yellow', 'Green'):
        rows = [row for row in tiers if row['tier'] == tier]
        out.append('## {} — {} missions\n'.format(tier, len(rows)))
        out.append('| # | Mission | Approach | Notes |')
        out.append('|---|---|---|---|')
        for row in rows:
            mission = missions.get(row['id'], {})
            title = mission.get('title', '').title() or row['id']
            out.append('| {} | {} | {} | {} |'.format(row['id'], title, row['technique'], row['note']))
        out.append('')

    out.append('---\n')
    out.append('Generated from `data/feasibility.tsv` by `tools/render_feasibility_doc.py`. '
               'The tiers are judgment calls made from the bible text, not from testing — '
               'revise them as missions are actually built, and treat a Green that turns out '
               'Yellow as information about the next twenty missions, not just this one.')

    with io.open(OUT, 'w', encoding='utf-8', newline=CRLF) as handle:
        handle.write('\n'.join(out) + '\n')
    print('wrote docs/FEASIBILITY.md ({} missions)'.format(len(tiers)))


if __name__ == '__main__':
    main()
