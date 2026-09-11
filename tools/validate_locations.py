"""Check every mission coordinate against the game's real zone boundaries.

This cannot survey anything — that needs the game running. What it can do is catch
the estimates that are in the wrong place entirely: a warehouse coordinate that
lands in the ocean, a "Rockford Hills" position that is actually in Vespucci, a Z
value below sea level for a mission that happens on a roof.

Zone data is fetched from a public GTA V data dump at run time and cached under
build/; it is deliberately not committed, since it derives from the game's own
files.

    python3 tools/validate_locations.py                 # report only
    python3 tools/validate_locations.py --fix           # move wrong-district keys
                                                        # to the right zone's center
"""

import argparse
import csv
import io
import json
import os
import re
import urllib.request

# Every generated file in this repo is committed with CRLF, because they were
# all first written on Windows. Writing them with the platform default instead
# turns one regeneration on Linux into a whole-file diff on every line, which
# hides the handful of rows that actually changed. Pinning it makes the output
# the same artifact wherever the tool runs.
CRLF = '\r\n'

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
LOCATIONS = os.path.join(REPO, 'data', 'locations.tsv')
CACHE = os.path.join(REPO, 'build', 'zones.json')
REPORT = os.path.join(REPO, 'docs', 'LOCATION-AUDIT.md')
ZONES_URL = 'https://raw.githubusercontent.com/DurtyFree/gta-v-data-dumps/master/zones.json'

# Map bounds, generous: anything outside this is not on the island at all.
MAP_MIN_X, MAP_MAX_X = -4000.0, 4500.0
MAP_MIN_Y, MAP_MAX_Y = -4000.0, 8200.0
SEA_LEVEL = 0.0

# Kinds where a low or negative Z is the point rather than a mistake.
DEEP_KINDS = ('water', 'underground', 'interior')


def load_zones():
    if not os.path.exists(CACHE):
        os.makedirs(os.path.dirname(CACHE), exist_ok=True)
        print('fetching zone data...')
        with urllib.request.urlopen(ZONES_URL, timeout=60) as response:
            data = response.read()
        with open(CACHE, 'wb') as handle:
            handle.write(data)

    with io.open(CACHE, encoding='utf-8') as handle:
        return json.load(handle)


def zone_for(zones, x, y, z):
    """Innermost zone containing the point — the smallest matching box wins."""
    best, best_volume = None, None
    for zone in zones:
        for box in zone.get('Bounds', []):
            low, high = box['Minimum'], box['Maximum']
            if not (low['X'] <= x <= high['X'] and low['Y'] <= y <= high['Y']):
                continue
            if not (low['Z'] - 50 <= z <= high['Z']):
                continue
            volume = ((high['X'] - low['X']) * (high['Y'] - low['Y']))
            if best_volume is None or volume < best_volume:
                best, best_volume = zone, volume
    return best


def zone_center(zone):
    box = min(zone['Bounds'],
              key=lambda b: (b['Maximum']['X'] - b['Minimum']['X']) *
                            (b['Maximum']['Y'] - b['Minimum']['Y']))
    return ((box['Minimum']['X'] + box['Maximum']['X']) / 2,
            (box['Minimum']['Y'] + box['Maximum']['Y']) / 2)


# Words that appear in half the district names in Los Santos and so carry no
# information about which one is meant.
STOPWORDS = {'los', 'santos', 'city', 'county', 'north', 'south', 'east', 'west',
             'island', 'fwy', 'freeway', 'boulevard', 'blvd', 'area', 'zone', 'the',
             'international', 'grand'}


def significant_words(text):
    return {word.lower() for word in re.findall(r'[A-Za-z]{3,}', text or '')} - STOPWORDS


def find_zone_by_name(zones, hint):
    """
    The zone the bible's district text is naming.

    A zone whose whole name appears in the hint wins outright — "Terminal Island
    Dry-Docks" names Terminal, not Elysian Island, and word-overlap scoring alone
    gets that backwards.
    """
    if not hint:
        return None

    lowered = hint.lower()
    named = [zone for zone in zones
             if (zone.get('DisplayName') or '') and (zone['DisplayName'].lower() in lowered)]
    if named:
        return max(named, key=lambda zone: len(zone['DisplayName']))

    words = significant_words(hint)
    if not words:
        return None

    best, best_score = None, 0
    for zone in zones:
        score = len(words & significant_words(zone.get('DisplayName')))
        if score > best_score:
            best, best_score = zone, score
    return best if best_score else None


def main():
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--fix', action='store_true',
                        help='rewrite wrong-district coordinates to the correct zone center')
    args = parser.parse_args()

    zones = load_zones()
    with io.open(LOCATIONS, encoding='utf-8') as handle:
        rows = list(csv.DictReader(handle, delimiter='\t'))

    findings = []
    for row in rows:
        x, y, z = float(row['x']), float(row['y']), float(row['z'])
        issues = []

        if not (MAP_MIN_X <= x <= MAP_MAX_X and MAP_MIN_Y <= y <= MAP_MAX_Y):
            issues.append('outside the map')

        if z < SEA_LEVEL and row.get('kind', 'land') not in DEEP_KINDS:
            issues.append('below sea level')

        actual = zone_for(zones, x, y, z)
        actual_name = (actual.get('DisplayName') or actual.get('Name')) if actual \
            else 'nowhere (off-map or unzoned)'
        expected = find_zone_by_name(zones, row['district_hint'])

        # A coordinate sitting in a zone the hint mentions at all is fine — districts
        # neighbor and overlap, and the bible names places loosely.
        hint_words = significant_words(row['district_hint'])
        actual_words = significant_words(actual_name) if actual else set()
        plausible = bool(hint_words & actual_words)

        # Only estimates are district-checked: a surveyed or authored coordinate is
        # allowed to be somewhere the mission's own district text does not name.
        checkable = row.get('status', 'estimate') == 'estimate'

        if checkable and expected and actual and expected['Name'] != actual['Name'] and not plausible:
            issues.append('in {} — the bible says {}'.format(actual_name, expected['DisplayName']))
        elif checkable and expected and not actual:
            issues.append('unzoned — the bible says ' + expected['DisplayName'])

        row['_zone'] = actual_name
        row['_expected'] = (expected.get('DisplayName') or expected.get('Name')) if expected else ''
        row['_issues'] = issues

        if issues:
            findings.append(row)

        if args.fix and expected and issues:
            center_x, center_y = zone_center(expected)
            row['x'] = '{:.2f}'.format(center_x)
            row['y'] = '{:.2f}'.format(center_y)
            row['status'] = 'zone-center'

    if args.fix:
        with io.open(LOCATIONS, 'w', encoding='utf-8', newline=CRLF) as handle:
            cols = ['key', 'x', 'y', 'z', 'heading', 'kind', 'status', 'district_hint']
            handle.write('\t'.join(cols) + '\n')
            for row in rows:
                handle.write('\t'.join(row[c] for c in cols) + '\n')
        print('rewrote {} of {} coordinates to their zone center'.format(
            sum(1 for r in rows if r['status'] == 'zone-center'), len(rows)))

    lines = ['# Location audit', '',
             'Generated by `tools/validate_locations.py` against the game\'s real zone '
             'boundaries. This checks *district*, not accuracy — a coordinate can be in the '
             'right zone and still be inside a wall. Only an in-game survey fixes that; see '
             '`docs/QA.md` audit 8.', '',
             '**{} coordinates checked, {} flagged.**'.format(len(rows), len(findings)), '']

    if findings:
        lines += ['| Key | Sits in | Bible says | Problem |', '|---|---|---|---|']
        for row in findings:
            lines.append('| `{}` | {} | {} | {} |'.format(
                row['key'], row['_zone'], row['_expected'] or '—', '; '.join(row['_issues'])))
    else:
        lines.append('No coordinate is in the wrong district.')

    lines += ['', '## Status values in `data/locations.tsv`', '',
              '| Status | Meaning |', '|---|---|',
              '| `bible` | from the omnibus Track 2 index — surveyed by the author |',
              '| `surveyed` | captured in game with the dev tools; trustworthy |',
              '| `zone-center` | moved here by `--fix`; the right district, an arbitrary spot in it |',
              '| `estimate` | hand-placed guess; treat every one as unverified |', '']

    with io.open(REPORT, 'w', encoding='utf-8', newline=CRLF) as handle:
        handle.write('\n'.join(lines) + '\n')
    print('wrote docs/LOCATION-AUDIT.md — {} of {} flagged'.format(len(findings), len(rows)))


if __name__ == '__main__':
    main()
