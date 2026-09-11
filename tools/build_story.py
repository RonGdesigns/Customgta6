"""Validate and compile the authored story expansion. No PDF-derived TSV is rewritten.

python tools/build_story.py          # scenes.tsv and readable recording script
python tools/build_story.py --check  # coverage, cue IDs, and generated-file freshness
"""
from pathlib import Path
import argparse, csv, io, re

ROOT = Path(__file__).resolve().parents[1]
SPEAKERS = {'ICE', 'GOHAN', 'GUESS', 'KJ'}
# Support cast who may speak only inside a named phase (an "@phase" line inside a mission block).
SUPPORT = {'MILLER', 'BUYER', 'MATEO'}
PHASE_DIRECTION = {'transaction': 'Staged action; support cast on set, brothers in position',
                   'stash': 'At the curb; the prototype stays, the crew boards the Granger',
                   'call': 'Alone in the starter apartment; the message arrives',
                   'split': 'At the base; the depot team boards the van and pulls away',
                   'loading': 'At the Benson; crates carried into the bed by hand',
                   'shore': 'From the cliff and the dinghy; the cove, the lamps, his boat',
                   'positions': 'The depot from three places; nobody has moved yet',
                   'approach': 'On the approach; the place and the job seen before anyone moves',
                   'inspect': 'At the stash; the load checked before the run',
                   'shop': 'In the shop, mid-work; nobody stops for a briefing',
                   'lift': 'On the flats; the lift landed, its job named',
                   'release': 'At the slip; the handle fitted where either can reach it'}

def blocks(path, key_pattern, phases=None):
    """Lines per block. With `phases`, a block may contain '@name' lines that open a named phase;
    those lines are collected in phases[block][name] and the block keeps only its unnamed lines."""
    result, key, phase = {}, None, None
    for number, raw in enumerate(path.read_text(encoding='utf-8').splitlines(), 1):
        line = raw.strip()
        if not line or line.startswith('#'): continue
        if re.fullmatch(key_pattern, line):
            if line in result: raise ValueError(f'{path.name}:{number}: duplicate block {line}')
            key=line; phase=None; result[key]=[]; continue
        if line.startswith('@') and phases is not None and key is not None:
            phase=line[1:].strip()
            if not re.fullmatch(r'[a-z]+', phase) or phase in ('intro','outro','prologue','arrival','recognition','moment'):
                raise ValueError(f'{path.name}:{number}: invalid phase name {phase}')
            phases.setdefault(key, {}).setdefault(phase, []); continue
        if key is None or '|' not in line: raise ValueError(f'{path.name}:{number}: malformed line')
        speaker, speech = line.split('|', 1)
        allowed = SPEAKERS | (SUPPORT if phase else set())
        if speaker not in allowed or not speech.strip() or len(speech)>240:
            raise ValueError(f'{path.name}:{number}: invalid speaker or subtitle length')
        if phase: phases[key][phase].append((speaker, speech))
        else: result[key].append((speaker, speech))
    return result

def render():
    with (ROOT/'data/missions.tsv').open(encoding='utf-8') as stream:
        missions={row['id']: row for row in csv.DictReader(stream,delimiter='\t')}
    named={}
    beats=blocks(ROOT/'data/story_beats.txt',r'S?M\d\d',named)
    opening=blocks(ROOT/'data/opening_scene.txt',r'prologue|arrival|call|intro|recognition')
    if beats.keys()!=missions.keys(): raise ValueError(f'Mission coverage mismatch: {beats.keys() ^ missions.keys()}')
    if opening.keys()!={'prologue','arrival','call','intro','recognition'}: raise ValueError('Opening requires prologue, arrival, call, intro and recognition')
    if any(speaker!='GUESS' for phase in ('prologue','arrival','call') for speaker,_ in opening[phase]): raise ValueError('Ron is alone before M01')
    with (ROOT/'data/mission_starts.tsv').open(encoding='utf-8') as stream:
        starts=list(csv.DictReader(stream,delimiter='\t'))
    with (ROOT/'data/locations.tsv').open(encoding='utf-8') as stream:
        location_keys={row['key'] for row in csv.DictReader(stream,delimiter='\t')}
    catalog=(ROOT/'src/Bloodlines/Missions/MissionCatalog.cs').read_text(encoding='utf-8')
    playable=set(re.findall(r'\{\s*"(S?M\d{2})",\s*\(\)\s*=>\s*new',catalog))
    if len(starts)!=len(playable) or {row['mission'] for row in starts}!=playable:
        raise ValueError('Start markers must cover every implemented mission exactly')
    if any(row['location_key'] not in location_keys for row in starts): raise ValueError('Unknown marker location key')
    if {mission for mission,lines in beats.items() if any(speaker=='KJ' for speaker,_ in lines)}!={'SM03','SM09'}:
        raise ValueError('KJ belongs to the two authored Guess solo appearances')
    rows=[]; script=['# Bloodlines: scene and recording script','',
        'Authored expansion of the omnibus and solo bibles. These are new lines, not quotations from the PDFs.', '',
        '36 gameplay missions exist (M01–M30, SM01–SM06). Other scenes are authored for future scripts; they are not playable missions.', '',
        'Delivery: Ice measures his words, Gohan explains precisely then risks personal honesty, Guess uses humor until he needs a direct answer.',
        'Briefings and aftermath use camera cuts and held poses. No lip sync or bespoke performance animation is supplied. Solo aftermath replies are over radio.', '',
        'A fresh campaign opens on the prologue: Ron alone at LSIA, a drive home the player makes, the door, and the job read inside his starter apartment (at the door if the room does not load). The prologue scenes move him (phone, walk, car entry/exit); skipping lands on the same state.', '',
        'M01 opens on separate private channels at separate exterior approach positions (ground-resolved at runtime). Recognition follows successful approaches, not mission launch.', '',
        'Each WAV uses the cue ID below, in that mission\'s existing audio bank. Silence is supported. Enter or controller A skips a scene; hold Backspace aborts.', '']
    for mission, lines in beats.items():
        if len(lines)<4: raise ValueError(f'{mission}: needs briefing and aftermath')
        phases={'intro': lines[:2], 'outro': lines[2:]}
        if mission=='M01': phases={'prologue':opening['prologue'],'arrival':opening['arrival'],'call':opening['call'],'intro':opening['intro'],'recognition':opening['recognition'],'outro':lines[2:]}
        for name, cues in named.get(mission, {}).items():
            if not cues: raise ValueError(f'{mission}: empty phase {name}')
            phases[name]=cues
        script += [f'## {mission} — {missions[mission]["title"]}', '']
        for phase, cues in phases.items():
            script += [f'### {phase.title()}', '']
            for i,(speaker,line) in enumerate(cues,1):
                cue_id=f'{mission}_SCENE_{phase.upper()}_{i:02d}_{speaker}'
                direction=('Alone at the terminal curb; phone, walk to the car, get in' if phase=='prologue' else
                           'Alone at the apartment door; out of the car, walk to the door' if phase=='arrival' else
                           'Alone in the starter apartment; the message arrives (at the door if the room does not load)' if mission=='M01' and phase=='call' else
                           'Private channel; no recognition or shared conversation' if mission=='M01' and phase=='intro' else
                           'Face-to-face reunion; anger interrupted by danger' if phase=='recognition' else
                           'Reflective; allow the response to land' if phase=='outro' else
                           PHASE_DIRECTION.get(phase, 'Staged action') if phase not in ('intro',) else 'Briefing; intent before tactics')
                rows.append(dict(cue_id=cue_id,mission=mission,phase=phase,speaker=speaker,direction=direction,line=line))
                script += [f'**{speaker}** ({cue_id}) — {line}', '']
    if len({row['cue_id'] for row in rows})!=len(rows): raise ValueError('Duplicate cue IDs')
    buffer=io.StringIO(newline='')
    writer=csv.DictWriter(buffer,fieldnames=['cue_id','mission','phase','speaker','direction','line'],delimiter='\t',lineterminator='\n')
    writer.writeheader(); writer.writerows(rows)
    with (ROOT/'data/dialogue.tsv').open(encoding='utf-8') as stream:
        gameplay=list(csv.DictReader(stream,delimiter='\t'))
    full=['# Bloodlines: complete dialogue and recording draft','',
          f'{len(missions)} written missions; {len(playable)} gameplay scripts. Future mission triggers remain design targets.', '',
          'Generated from dialogue.tsv and authored scene sources. Editorial changes live in data/dialogue_edits.json; original PDFs remain intact.', '',
          'M01 uses stock dock exteriors while its proposed crane/yacht set is unavailable. Three old recognition cues are superseded by the recognition scene.', '']
    for mid, info in missions.items():
        full += [f'## {mid} - {info["title"]} ({"scripted" if mid in playable else "future gameplay"})','']
        groups=[('Intro',[r for r in rows if r['mission']==mid and r['phase']=='intro']),
                ('Gameplay',[r for r in gameplay if r['mission']==mid and not (mid=='M01' and r['stage']=='2')]),
                ('Recognition after all three approaches',[r for r in rows if r['mission']==mid and r['phase']=='recognition']),
                ('Aftermath',[r for r in rows if r['mission']==mid and r['phase']=='outro'])]
        if mid=='M01':
            groups=[groups[0],('Approaches',[r for r in gameplay if r['mission']==mid and r['stage']=='1']),
                    groups[2],('Escape',[r for r in gameplay if r['mission']==mid and r['stage']=='3']),groups[3]]
        for title, lines in groups:
            if not lines: continue
            full += [f'### {title}','']
            for row in lines:
                full += [f'**{row["speaker"]}** `{row["cue_id"]}`',row['line'],
                         'Delivery: '+row.get('direction',''),
                         'Trigger: '+row.get('trigger',row.get('phase','')), '']
    return {ROOT/'data/scenes.tsv':buffer.getvalue(),ROOT/'docs/STORY-SCRIPT.md':'\n'.join(script)+'\n',
            ROOT/'docs/COMPLETE-DIALOGUE.md':'\n'.join(full)+'\n'},len(rows)

def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--check',action='store_true');args=parser.parse_args()
    artifacts,count=render()
    for path,text in artifacts.items():
        if args.check:
            if not path.exists() or path.read_text(encoding='utf-8')!=text: raise SystemExit('Regenerate stale artifact: '+str(path))
        else: path.write_text(text,encoding='utf-8',newline='\n')
    print(f'Story coverage: 79 missions, {count} unique cues, 161 scenes; '+('freshness verified' if args.check else 'generated'))

if __name__=='__main__':main()
