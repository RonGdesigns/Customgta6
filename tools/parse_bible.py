"""Turn a Bloodlines design bible PDF into the data files the mod loads at runtime.

The omnibus bible is the source of record for mission order, titles, setting,
objectives and every line of dialogue. Rather than retyping any of that into C#,
this parses it into tab-separated data under data/, which BloodlinesCore reads on
load. Re-run it whenever a new revision of the bible arrives:

    python3 tools/parse_bible.py docs/bibles/omnibus_v2.pdf

Outputs (tab-separated, one header row, no quoting — tabs are stripped from values):
    data/missions.tsv   number, id, title, act, location, time, weather, hud, synopsis
    data/dialogue.tsv   cue_id, mission, stage, speaker, direction, line, trigger
    data/anchors.tsv    key, description, entity, x, y, z, heading
"""

import os
import re
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from pdf_text import extract  # noqa: E402

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA = os.path.join(REPO, 'data')

MISSION_HEADER = re.compile(r'^M(\d{2}):\s*[""\'"]?(.+?)[""\'"]?$')
CUE_ID = re.compile(r'^(M\d{2})_S(\d+)_(\d+)_([A-Z]+)$')
VECTOR = re.compile(r'Vector3\(\s*(-?[\d.]+)f?,\s*(-?[\d.]+)f?,\s*(-?[\d.]+)f?\s*\)')
FOOTER = re.compile(r'^(GTA V: BLOODLINES|Page \d+ of \d+|•+$|=== PAGE)')


def clean(value):
    return re.sub(r'\s+', ' ', value.replace('\t', ' ')).strip()


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
            trigger = join_wrapped(cue_body[end + 1:])
        direction = ''
        bracket = re.match(r'^\s*\[(.+?)\]\s*(.*)$', spoken, re.S)
        if bracket:
            direction, spoken = bracket.group(1), bracket.group(2)
        cues.append({
            'cue_id': cue['id'], 'mission': cue['mission'], 'stage': cue['stage'],
            'speaker': cue['speaker'], 'direction': clean(direction),
            'line': clean(spoken).strip("'").strip(), 'trigger': clean(trigger),
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
        current['hud'] = clean(hud)
        current['synopsis'] = clean(join_wrapped(synopsis))
        missions.append(current)

    for raw in lines:
        line = raw.strip()
        if not line or FOOTER.match(line):
            continue

        header = MISSION_HEADER.match(line)
        if header:
            flush_cue()
            flush_mission()
            cue, cue_body, body = None, [], []
            current = {'number': int(header.group(1)), 'id': 'M' + header.group(1),
                       'title': clean(header.group(2)).strip('"'), 'act': '', 'location': '',
                       'time': '', 'weather': '', 'hud': '', 'synopsis': ''}
            mode = 'meta'
            continue

        if not current:
            continue

        if mode == 'meta':
            # "Act I • Terminal Island Dry-Docks • 02:00 HRS • Clear / Night"
            parts = [clean(part) for part in line.split('•')]
            if len(parts) >= 2 and parts[0].upper().startswith('ACT'):
                current['act'] = parts[0]
                current['location'] = parts[1] if len(parts) > 1 else ''
                current['time'] = parts[2] if len(parts) > 2 else ''
                current['weather'] = ' / '.join(parts[3:]) if len(parts) > 3 else ''
                mode = 'body'
                continue
            mode = 'body'

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


def write_tsv(path, columns, rows):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    with open(path, 'w') as handle:
        handle.write('\t'.join(columns) + '\n')
        for row in rows:
            handle.write('\t'.join(str(row[column]) for column in columns) + '\n')
    print('wrote {} ({} rows)'.format(os.path.relpath(path, REPO), len(rows)))


def main():
    if len(sys.argv) != 2:
        raise SystemExit(__doc__)

    source = sys.argv[1]
    text = extract(source) if source.lower().endswith('.pdf') else open(source).read()

    missions, cues = parse_missions(text.splitlines())
    anchors = parse_anchors(text)

    missions.sort(key=lambda mission: mission['number'])
    write_tsv(os.path.join(DATA, 'missions.tsv'),
              ['number', 'id', 'title', 'act', 'location', 'time', 'weather', 'hud', 'synopsis'],
              missions)
    write_tsv(os.path.join(DATA, 'dialogue.tsv'),
              ['cue_id', 'mission', 'stage', 'speaker', 'direction', 'line', 'trigger'], cues)
    write_tsv(os.path.join(DATA, 'anchors.tsv'),
              ['key', 'description', 'entity', 'x', 'y', 'z', 'heading'], anchors)

    missing = [n for n in range(1, 71) if n not in {m['number'] for m in missions}]
    if missing:
        print('WARNING: no block found for mission(s): ' +
              ', '.join('M%02d' % n for n in missing))


if __name__ == '__main__':
    main()
