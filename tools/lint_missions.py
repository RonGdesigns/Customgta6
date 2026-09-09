"""Static checks on the mission scripts — the errors the compiler cannot see.

A mission is held together by string literals: location keys, dialogue cue ids and
model names. Every one of them compiles perfectly and then fails silently in game —
a typo'd model spawns nothing, a wrong cue id logs a warning nobody reads, a bad
location key puts an objective marker at the origin. With thirty missions written
and none played, these are the bugs most likely to be waiting.

Checks:
  * every Locations.Position/Heading key exists in data/locations.tsv
  * every Say(...) cue id exists in data/dialogue.tsv, and belongs to that mission
  * every new Model("...") is a real vehicle or ped (checked against public dumps,
    fetched at run time and cached under build/, never committed)
  * dialogue coverage: how many of a mission's written lines it actually fires

    python3 tools/lint_missions.py           # report, non-zero exit on errors
    python3 tools/lint_missions.py --quiet   # errors only
"""

import argparse
import csv
import json
import io
import os
import re
import sys
import urllib.request


def read_text(path):
    """Read a file as UTF-8, whatever the OS thinks the default is.

    Bare open() takes its encoding from the locale, which on a US Windows install
    is cp1252. Every file this tool reads -- the model dumps, the TSVs and the
    mission sources -- is UTF-8, so on Windows the linter died on the first curly
    quote in a Rockstar model dump before it checked a single mission. A check
    that only runs on one machine is not a check.
    """
    with io.open(path, encoding='utf-8') as handle:
        return handle.read()


REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MISSIONS_DIR = os.path.join(REPO, 'src', 'Bloodlines', 'Missions', 'Campaign')
CACHE_DIR = os.path.join(REPO, 'build')

DUMPS = {
    'vehicles': 'https://raw.githubusercontent.com/DurtyFree/gta-v-data-dumps/master/vehicles.json',
    'peds': 'https://raw.githubusercontent.com/DurtyFree/gta-v-data-dumps/master/peds.json',
}

LOCATION_KEY = re.compile(r'Locations\.(?:Position|Heading)\("([^"]+)"\)')
ANCHOR_KEY = re.compile(r'\.Anchor(?:Heading)?\("([^"]+)"')
CUE_ID = re.compile(r'Say\("([^"]+)"\)')
STAGE_CUE = re.compile(r'(?:SayStage|WithDialogue)\((\d+)\)')
MODEL_NAME = re.compile(r'new Model\("([^"]+)"\)')
MISSION_ID = re.compile(r'public override string Id => "([^"]+)"')
SCENARIO = re.compile(r'StartScenario\("([^"]+)"')
DEPLOY = re.compile(r'Crew\.Deploy(?:Solo)?\(CrewSlot\.\w+,\s*([^,]+?),', re.S)
DEPLOY_OFFSET = re.compile(r'new Vector3\([^,]+,\s*[^,]+,\s*(-?[\d.]+)f\)')
REGISTERED = re.compile(r'\{\s*"(S?M\d{2})",\s*\(\)\s*=>\s*new\s+(\w+)\(\)\s*\}')

# Scenarios used by the campaign. Not exhaustive for the game — exhaustive for us,
# so a typo in a new one is flagged rather than silently doing nothing.
KNOWN_SCENARIOS = {
    'WORLD_HUMAN_GUARD_STAND', 'WORLD_HUMAN_GUARD_PATROL', 'WORLD_HUMAN_SMOKING',
    'WORLD_HUMAN_DRINKING', 'WORLD_HUMAN_CLIPBOARD', 'WORLD_HUMAN_HAMMERING',
    'WORLD_HUMAN_WELDING', 'WORLD_HUMAN_STAND_MOBILE', 'WORLD_HUMAN_LEANING',
}


def load_dump(name):
    path = os.path.join(CACHE_DIR, name + '.json')
    if not os.path.exists(path):
        os.makedirs(CACHE_DIR, exist_ok=True)
        print('fetching {} reference data...'.format(name))
        with urllib.request.urlopen(DUMPS[name], timeout=120) as response:
            data = response.read()
        with open(path, 'wb') as handle:
            handle.write(data)

    with io.open(path, encoding='utf-8') as handle:
        return {entry['Name'].lower() for entry in json.load(handle) if entry.get('Name')}


def read_tsv(name):
    with io.open(os.path.join(REPO, 'data', name), encoding='utf-8') as handle:
        return list(csv.DictReader(handle, delimiter='\t'))


def check_ascii(errors):
    """Data files must be pure ASCII.

    GTA V's text renderer is not dependable outside ASCII, and the log is read
    back through tools that assume ANSI -- an em dash in a dialogue line shows up
    as a box in a subtitle and as mojibake in the log. The PDF extractor is the
    source of these: it emits typographic dashes and a section bullet that the
    parser now folds, and this catches any that get past it.
    """
    for name in ('missions.tsv', 'dialogue.tsv', 'locations.tsv', 'anchors.tsv',
                 'feasibility.tsv'):
        path = os.path.join(REPO, 'data', name)
        if not os.path.exists(path):
            continue
        with io.open(path, encoding='utf-8') as handle:
            for number, line in enumerate(handle, 1):
                bad = sorted({ch for ch in line if ord(ch) > 127})
                if bad:
                    errors.append('data/{}:{}: non-ASCII {}'.format(
                        name, number,
                        ', '.join('U+%04X (%s)' % (ord(ch), ch) for ch in bad)))


def main():
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument('--quiet', action='store_true', help='errors only')
    args = parser.parse_args()

    location_rows = read_tsv('locations.tsv')
    locations = {row['key'] for row in location_rows}
    location_kinds = {row['key']: row.get('kind', 'land') for row in location_rows}
    anchors = {row['key'] for row in read_tsv('anchors.tsv')}
    cues = read_tsv('dialogue.tsv')
    cue_ids = {row['cue_id'] for row in cues}

    cues_by_mission = {}
    for cue in cues:
        cues_by_mission.setdefault(cue['mission'], []).append(cue)

    try:
        vehicles = load_dump('vehicles')
        peds = load_dump('peds')
        models = vehicles | peds
    except OSError as error:
        print('ERROR: model reference data unavailable ({}); validation incomplete'.format(error))
        return 1

    errors, warnings, coverage = [], [], []

    # A mission class nobody registered is invisible; a registration with no class
    # does not compile, so only the first direction needs checking here.
    catalog = read_text(os.path.join(REPO, 'src', 'Bloodlines', 'Missions', 'MissionCatalog.cs'))
    registered = {mission_id: type_name for mission_id, type_name in REGISTERED.findall(catalog)}
    seen_ids = set()

    for base, _, files in os.walk(MISSIONS_DIR):
        for name in sorted(files):
            if not name.endswith('.cs'):
                continue

            path = os.path.join(base, name)
            source = read_text(path)
            relative = os.path.relpath(path, REPO)

            mission_id = MISSION_ID.search(source)
            mission_id = mission_id.group(1) if mission_id else None
            if mission_id:
                seen_ids.add(mission_id)
                if mission_id not in registered:
                    errors.append('{}: {} is not registered in MissionCatalog.Scripted'
                                  .format(relative, mission_id))

            for key in sorted(set(LOCATION_KEY.findall(source))):
                if key not in locations:
                    errors.append('{}: unknown location key "{}"'.format(relative, key))

            for key in sorted(set(ANCHOR_KEY.findall(source))):
                if key not in anchors:
                    errors.append('{}: unknown anchor "{}"'.format(relative, key))

            for cue in sorted(set(CUE_ID.findall(source))):
                if cue not in cue_ids:
                    errors.append('{}: unknown dialogue cue "{}"'.format(relative, cue))
                elif mission_id and not cue.startswith(mission_id + '_'):
                    warnings.append('{}: cue "{}" belongs to another mission'.format(relative, cue))

            # Deploying onto an airborne or water coordinate drops the crew out of
            # the sky or into the sea — invisible until someone plays it.
            for expression in DEPLOY.findall(source):
                for key in LOCATION_KEY.findall(expression):
                    kind = location_kinds.get(key)
                    if kind in ('air', 'water'):
                        errors.append('{}: deploys the crew onto {} ("{}"), which is {}'
                                      .format(relative, key, key, kind))

                for offset in DEPLOY_OFFSET.findall(expression):
                    if abs(float(offset)) > 5:
                        errors.append('{}: deploys the crew {}m off the ground'
                                      .format(relative, offset))

            for scenario in sorted(set(SCENARIO.findall(source))):
                if scenario not in KNOWN_SCENARIOS:
                    warnings.append('{}: scenario "{}" is not in the known set — verify it'
                                    .format(relative, scenario))

            if models is not None:
                for model in sorted(set(MODEL_NAME.findall(source))):
                    # Props are not in the dumps; they are prefixed and checked by eye.
                    if model.lower().startswith(('prop_', 'p_', 'v_')):
                        continue
                    if model.lower() not in models:
                        errors.append('{}: "{}" is not a known vehicle or ped model'
                                      .format(relative, model))

            # Dialogue coverage — a mission that fires two of its six written lines is
            # leaving the bible on the page.
            if mission_id and mission_id in cues_by_mission:
                written = cues_by_mission[mission_id]
                fired = set(CUE_ID.findall(source))
                fired.update(re.findall(r'"(S?M\d+_S\d+_\d+_[A-Z]+)"', source))
                stages = {int(stage) for stage in STAGE_CUE.findall(source)}
                for cue in written:
                    if cue['stage'] in {str(stage) for stage in stages}:
                        fired.add(cue['cue_id'])

                coverage.append((mission_id, len(fired & {c['cue_id'] for c in written}), len(written)))

    if not args.quiet and coverage:
        print('\nDialogue coverage')
        print('-----------------')
        for mission_id, fired, written in sorted(coverage):
            bar = 'ok  ' if fired >= written else 'thin'
            print('  {}  {}  {}/{} lines fired'.format(bar, mission_id, fired, written))

        thin = [c for c in coverage if c[1] < c[2]]
        print('\n  {} of {} missions fire every written line.'
              .format(len(coverage) - len(thin), len(coverage)))

    if warnings and not args.quiet:
        print('\nWarnings')
        print('--------')
        for warning in warnings:
            print('  ' + warning)

    for mission_id in sorted(set(registered) - seen_ids):
        errors.append('MissionCatalog registers {} but no mission class declares that id'
                      .format(mission_id))

    check_ascii(errors)

    print('\nErrors' if errors else '\nNo errors.')
    for error in errors:
        print('  ' + error)

    return 1 if errors else 0


if __name__ == '__main__':
    sys.exit(main())
