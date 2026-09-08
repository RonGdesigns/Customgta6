"""Turn a Bloodlines design bible PDF into the data files the mod loads at runtime.

The omnibus bible is the source of record for mission order, titles, setting,
objectives and every line of dialogue. Rather than retyping any of that into C#,
this parses it into tab-separated data under data/, which BloodlinesCore reads on
load. Re-run it whenever a new revision of the bible arrives:

    python3 tools/parse_bible.py docs/bibles/omnibus_v2.pdf docs/bibles/solo_missions_v1.pdf

Outputs (tab-separated, one header row, no quoting — tabs are stripped from values):
    data/missions.tsv   id, number, kind, owner, insert_after, title, act, location, time,
                        weather, hud, synopsis
    data/dialogue.tsv   cue_id, mission, stage, speaker, direction, line, trigger
    data/anchors.tsv    key, description, entity, x, y, z, heading
    data/campaign_registry.json  the same mission list as JSON, for external tooling
"""

import json
import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pdf_text import extract  # noqa: E402

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA = os.path.join(REPO, 'data')

MISSION_HEADER = re.compile(r'^(SM|M)(\d{2}):\s*[""\'"]?(.+?)[""\'"]?$')
CUE_ID = re.compile(r'^((?:SM|M)\d{2})_S(\d+)_(\d+)_([A-Z ]+)$')
SOLO_OWNER = re.compile(r'^(ICE|GOHAN|GUESS)\b', re.I)
SOLO_WINDOW = re.compile(r'[Bb]etween [Mm]issions?\s+(\d+)\s+and\s+(\d+)')

# The cast was renamed during writing and one mission's prose kept the old names.
# Normalising here rather than in the mission scripts keeps the data honest for
# every consumer — the campaign doc, the voice generator and the mod alike.
NAME_ALIASES = [
    (re.compile(r'\bJules\b'), 'Gohan'),
    (re.compile(r'\bMalik\b'), 'Guess'),
]


def canonical_names(value):
    for pattern, replacement in NAME_ALIASES:
        value = pattern.sub(replacement, value)
    return value
VECTOR = re.compile(r'Vector3\(\s*(-?[\d.]+)f?,\s*(-?[\d.]+)f?,\s*(-?[\d.]+)f?\s*\)')
FOOTER = re.compile(r'^(GTA V: BLOODLINES|Page \d+ of \d+|•+$|=== PAGE)')


# The bible's PDF uses a filled box as its section bullet, so any text that runs
# past one has swallowed the start of the next section. It is also the only
# reliable marker of that boundary in the extracted stream.
SECTION_BULLET = '\u25a0'

# GTA V's text renderer is not dependable outside ASCII: an em dash comes back as
# a blank or a box in a subtitle. The typography is not worth a hole in the line.
ASCII_FOLD = {
    '\u2014': '-', '\u2013': '-', '\u2012': '-', '\u2212': '-',
    '\u2018': "'", '\u2019': "'", '\u201a': "'",
    '\u201c': '"', '\u201d': '"', '\u201e': '"',
    '\u2026': '...', '\u00a0': ' ', '\u2022': '-', '\u00b7': '-',
}


def fold_ascii(value):
    """Fold typographic characters to ASCII, then drop anything still non-ASCII."""
    for source, target in ASCII_FOLD.items():
        value = value.replace(source, target)
    return ''.join(ch for ch in value if ord(ch) < 128)


def clean(value):
    return fold_ascii(re.sub(r'\s+', ' ', value.replace('\t', ' ')).strip())


def join_wrapped(parts):
    """Join PDF-wrapped lines, keeping hyphenated words whole.

    A line ending in a hyphen is a word broken across the wrap ("forty-" / "eight"),
    so joining those with a space would produce "forty- eight" in the dialogue.
    """
    text = ''
    for part in parts:
        part = part.strip()
        if not part:
            continue
        if not text:
            text = part
        elif text.endswith('-') and part[:1].islower():
            text += part
        else:
            text += ' ' + part
    return text


def parse_missions(lines):
    """Walk the document once, splitting it into mission blocks."""
    missions, cues = [], []
    current, mode, cue = None, None, None
    body, cue_body = [], []
    pending_header = None

    def flush_cue():
        if not cue or not cue_body:
            return
        # The dialogue wraps across several lines and always ends on the line
        # carrying the closing quote; whatever follows it is the trigger event.
        end = next((i for i in range(len(cue_body) - 1, -1, -1) if "'" in cue_body[i]), None)
        if end is None:
            spoken, trigger = join_wrapped(cue_body), ''
        else:
            spoken = join_wrapped(cue_body[:end + 1])
            # A cue that is the last in its section runs straight into the next
            # section's heading, which the extractor cannot see as a break. The
            # bullet is the break.
            trigger = join_wrapped(cue_body[end + 1:]).split(SECTION_BULLET)[0]
        direction = ''
        bracket = re.match(r'^\s*\[(.+?)\]\s*(.*)$', spoken, re.S)
        if bracket:
            direction, spoken = bracket.group(1), bracket.group(2)
        cues.append({
            'cue_id': cue['id'], 'mission': cue['mission'], 'stage': cue['stage'],
            'speaker': cue['speaker'], 'direction': clean(direction),
            'line': canonical_names(clean(spoken).strip("'").strip()),
            'trigger': canonical_names(clean(trigger)),
        })

    def flush_mission():
        if not current:
            return
        text = [line for line in body if line]
        hud = next((line[4:].strip() for line in text if line.startswith('HUD:')), '')
        synopsis = [line for line in text
                    if not line.startswith('HUD:')
                    and 'STAGE MECHANICS' not in line
                    and line not in ('CUE ID', 'SPEAKER', 'DIALOGUE LINE', 'TRIGGER EVENT')]
        current['hud'] = canonical_names(clean(hud))
        current['synopsis'] = canonical_names(clean(join_wrapped(synopsis)))
        missions.append(current)

    for raw in lines:
        line = raw.strip()
        if not line or FOOTER.match(line):
            continue

        header = MISSION_HEADER.match(line)
        if header and line.count('(') > line.count(')'):
            # A long solo header wraps: SM08: "BURNER PROTOCOL" (GOHAN (DEVIN / MERCER))
            pending_header = line
            continue
        if pending_header:
            line = clean(pending_header + ' ' + line)
            pending_header = None
            header = MISSION_HEADER.match(line)
        if header:
            flush_cue()
            flush_mission()
            cue, cue_body, body = None, [], []
            prefix, number, title = header.group(1), header.group(2), header.group(3)

            # Solo headers carry their owner: SM01: "LEAD & KEVLAR" (ICE (DARIUS VANCE))
            owner = ''
            paren = re.search(r'\(([^()]*(?:\([^()]*\))?[^()]*)\)\s*$', title)
            if paren:
                inner = paren.group(1)
                title = title[:paren.start()].strip()
                match = SOLO_OWNER.match(inner.strip())
                if match:
                    owner = match.group(1).upper()

            current = {'number': int(number), 'id': prefix + number,
                       'kind': 'solo' if prefix == 'SM' else 'main',
                       'owner': owner,
                       'title': canonical_names(clean(title).strip('"')),
                       'act': '', 'location': '', 'time': '', 'weather': '',
                       'hud': '', 'synopsis': '', 'insert_after': ''}
            mode = 'meta'
            continue

        if not current:
            continue

        if mode == 'meta':
            # "Act I • Terminal Island Dry-Docks • 02:00 HRS • Clear / Night"
            parts = [clean(part) for part in line.split('•')]
            if len(parts) >= 2 and parts[0].upper().startswith('ACT'):
                current['act'] = parts[0]
                if len(parts) > 1 and parts[1].upper().endswith('SOLO'):
                    match = SOLO_OWNER.match(parts[1])
                    if match and not current['owner']:
                        current['owner'] = match.group(1).upper()
                    parts = [parts[0]] + parts[2:]
                current['location'] = parts[1] if len(parts) > 1 else ''
                current['time'] = parts[2] if len(parts) > 2 else ''
                current['weather'] = ' / '.join(parts[3:]) if len(parts) > 3 else ''
                mode = 'meta-tail'
                continue
            mode = 'body'

        if mode == 'meta-tail':
            # A wrapped weather description ("Interior" / "Darkness") lands on its own
            # short line before the stage-mechanics header.
            mode = 'body'
            if (len(line) < 30 and not line.upper().startswith(('HUD:', 'SOLO STAGE', 'STAGE MECHANICS'))
                    and not CUE_ID.match(line)):
                current['weather'] = clean(current['weather'] + ' ' + line)
                continue

        cue_header = CUE_ID.match(line)
        if cue_header:
            flush_cue()
            cue = {'id': line, 'mission': cue_header.group(1),
                   'stage': int(cue_header.group(2)), 'speaker': None}
            cue_body = []
            mode = 'cue-speaker'
            continue

        if mode == 'cue-speaker':
            cue['speaker'] = line
            mode = 'cue-body'
            continue

        if mode == 'cue-body':
            cue_body.append(line)
            continue

        body.append(line)

    flush_cue()
    flush_mission()
    return missions, cues


def parse_solo_windows(text):
    """Where the solo missions slot into the main campaign.

    The expansion states one window per act ("Taking place between Missions 03 and
    08"), which is what lets the catalog interleave solos with the 70 in the order
    they are meant to be played.
    """
    windows = {}
    for act, match in zip(['ACT I', 'ACT II', 'ACT III'], SOLO_WINDOW.finditer(text)):
        windows[act] = int(match.group(1))
    return windows


def parse_anchors(text):
    """Track 2's coordinate index — the only surveyed positions in the bible."""
    section = text.split('TRACK 2:')
    if len(section) < 2:
        return []
    section = section[1].split('TRACK 3:')[0]
    lines = [line.strip() for line in section.splitlines() if line.strip()]

    anchors = []
    for i, line in enumerate(lines):
        match = VECTOR.search(line)
        if not match or i < 3:
            continue
        heading = lines[i + 1] if i + 1 < len(lines) else '0'
        anchors.append({
            'key': clean(lines[i - 3]), 'description': clean(lines[i - 2]),
            'entity': clean(lines[i - 1]),
            'x': match.group(1), 'y': match.group(2), 'z': match.group(3),
            'heading': heading.rstrip('f').strip(),
        })
    return anchors


def act_folder(mission):
    """Audio bank each mission's cues belong to."""
    if mission['kind'] == 'solo':
        return 'Solo'
    number = mission['number']
    return 'Act1' if number <= 22 else 'Act2' if number <= 48 else 'Act3'


def decorate(missions):
    """Adds the dispatcher fields: play type, prerequisite and audio bank.

    A main mission is gated on the one before it; a solo mission is gated on the
    mission its act's window follows, which is what the expansion specifies.
    """
    main = [m for m in missions if m['kind'] == 'main']
    for mission in missions:
        mission['type'] = 'Solo' if mission['kind'] == 'solo' else 'Trio'
        mission['audio_dir'] = 'audio/{}/{}'.format(act_folder(mission), mission['id'])
        # class_name/assembly stay empty for missions built into the mod; fill them in
        # to dispatch a mission from an external assembly instead.
        mission.setdefault('class_name', '')
        mission.setdefault('assembly', '')

        if mission['kind'] == 'solo':
            mission['prerequisite'] = ('M%02d' % int(mission['insert_after'])
                                       if mission['insert_after'] else '')
        else:
            index = main.index(mission)
            mission['prerequisite'] = main[index - 1]['id'] if index > 0 else ''
    return missions


def write_registry(path, missions, anchors):
    """campaign_registry.json — the same data as missions.tsv, for external tooling.

    The mod reads the TSV at runtime (no JSON reader in .NET Framework 4.8 without
    dragging in a serializer), so this is generated from the same pass to keep the
    two from drifting.
    """
    anchor_by_mission = {}
    for anchor in anchors:
        # "Ice: Roost 4" is M01's player start; only the prologue has surveyed points.
        if anchor['key'].startswith('Ice:'):
            anchor_by_mission['M01'] = anchor

    entries = []
    for mission in missions:
        anchor = anchor_by_mission.get(mission['id'])
        entry = {
            'id': mission['id'],
            'title': mission['title'],
            'act': int(mission['act'].replace('Act', '').strip().split()[0].replace('I' * 3, '3')
                       .replace('I' * 2, '2').replace('I', '1')) if mission['act'] else 0,
            'type': mission['type'],
            'assembly': mission['assembly'],
            'className': mission['class_name'],
            'prerequisiteId': mission['prerequisite'] or None,
            'startingCoords': ({'x': float(anchor['x']), 'y': float(anchor['y']),
                                'z': float(anchor['z']), 'heading': float(anchor['heading'])}
                               if anchor else None),
            'audioDirectory': mission['audio_dir'],
        }
        if mission['type'] == 'Solo':
            entry['focusHero'] = mission['owner'].title()
        entries.append(entry)

    with open(path, 'w') as handle:
        json.dump({'missions': entries}, handle, indent=2)
        handle.write('\n')
    print('wrote {} ({} entries)'.format(os.path.relpath(path, REPO), len(entries)))


def write_tsv(path, columns, rows):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'w') as handle:
        handle.write('\t'.join(columns) + '\n')
        for row in rows:
            handle.write('\t'.join(str(row[column]) for column in columns) + '\n')
    print('wrote {} ({} rows)'.format(os.path.relpath(path, REPO), len(rows)))


def main():
    if len(sys.argv) < 2:
        raise SystemExit(__doc__)

    all_missions, all_cues, all_anchors = [], [], []
    seen = set()

    for source in sys.argv[1:]:
        text = extract(source) if source.lower().endswith('.pdf') else open(source).read()
        missions, cues = parse_missions(text.splitlines())
        windows = parse_solo_windows(text)

        for mission in missions:
            if mission['kind'] == 'solo':
                mission['insert_after'] = windows.get(mission['act'].upper(), '')
            if mission['id'] in seen:
                print('skipping duplicate {} from {}'.format(mission['id'], os.path.basename(source)))
                continue
            seen.add(mission['id'])
            all_missions.append(mission)

        all_cues.extend(cues)
        all_anchors.extend(parse_anchors(text))
        print('{}: {} missions, {} cues'.format(os.path.basename(source), len(missions), len(cues)))

    all_missions.sort(key=lambda mission: (mission['kind'] == 'solo', mission['number']))
    decorate(all_missions)

    write_tsv(os.path.join(DATA, 'missions.tsv'),
              ['id', 'number', 'kind', 'type', 'owner', 'insert_after', 'prerequisite',
               'audio_dir', 'assembly', 'class_name', 'title', 'act', 'location',
               'time', 'weather', 'hud', 'synopsis'],
              all_missions)
    write_tsv(os.path.join(DATA, 'dialogue.tsv'),
              ['cue_id', 'mission', 'stage', 'speaker', 'direction', 'line', 'trigger'], all_cues)
    write_tsv(os.path.join(DATA, 'anchors.tsv'),
              ['key', 'description', 'entity', 'x', 'y', 'z', 'heading'], all_anchors)
    write_registry(os.path.join(DATA, 'campaign_registry.json'), all_missions, all_anchors)

    main_numbers = {m['number'] for m in all_missions if m['kind'] == 'main'}
    missing = [n for n in range(1, 71) if n not in main_numbers]
    if missing:
        print('WARNING: no block found for mission(s): ' +
              ', '.join('M%02d' % n for n in missing))


if __name__ == '__main__':
    main()
