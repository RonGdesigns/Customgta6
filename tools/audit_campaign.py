"""Generate a reviewable map from implemented stage declarations; no game claims.
Run after mission edits. --check verifies the checked-in guide is current.
"""
from pathlib import Path
import re,csv,argparse
ROOT=Path(__file__).resolve().parents[1]

def stages(source):
    """Each `yield return new MissionStage(...)...;` statement, cut at its own
    terminating semicolon with parentheses balanced. Splitting on the next yield
    instead attributed anything declared between two stages (an objective built
    into a field, say) to the stage before it."""
    blocks=[];i=0
    while True:
        i=source.find('yield return new MissionStage(',i)
        if i<0:break
        j=source.index('(',i);depth=0;quoted=False;escape=False;k=j
        while k<len(source):
            ch=source[k]
            if quoted:
                if escape:escape=False
                elif ch=='\\':escape=True
                elif ch=='"':quoted=False
            elif ch=='"':quoted=True
            elif ch=='(':depth+=1
            elif ch==')':depth-=1
            elif ch==';' and depth==0:break
            k+=1
        blocks.append(source[j+1:k]);i=k
    return blocks

def declared_objectives(source):
    """Objectives built into fields before their stage: `_choice = new X("label", ...)`."""
    return {name:(kind,text) for name,kind,text in re.findall(r'(\b_\w+)\s*=\s*new\s+(\w+(?:Objective|Interaction))\(\s*"([^"]+)"',source)}

def story_gates():
    """The central gate table, read from the source so the map cannot drift from it."""
    cs=(ROOT/'src/Bloodlines/Missions/CampaignState.cs').read_text(encoding='utf-8')
    table=cs[cs.index('StoryGates'):]
    return {gate:re.findall(r'"(SM\d\d)"',solos) for gate,solos in re.findall(r'\{\s*"(M\d\d)",\s*new\[\]\s*\{([^}]*)\}',table)}

def render():
    with (ROOT/'data/missions.tsv').open(encoding='utf-8') as f: infos={r['id']:r for r in csv.DictReader(f,delimiter='\t')}
    catalog=(ROOT/'src/Bloodlines/Missions/MissionCatalog.cs').read_text(encoding='utf-8')
    implemented=set(re.findall(r'\{\s*"(S?M\d\d)",\s*\(\)\s*=>\s*new',catalog))
    out=['# Playable mission flow and retry map','',f'{len(implemented)} scripted missions. Generated from current production stage declarations. These are code-checked flows, not live playthrough results.','',
    'The quoted prompts below are the authored base instructions. Runtime text adds the required character, button, remaining work time, enemy count and passive rules. Yellow marks travel/work; red marks hostiles. E / D-pad Right starts timed work. Leaving its radius resets that work. Vehicle delivery requires the assigned hero aboard the actual vehicle. Aircraft landing also requires low height and speed.','',
    '## Failure and retry contract','',
    'Start unlocked job → skippable briefing → fresh actors/vehicles → objectives → final dialogue → commit completion and one-time reward → skippable aftermath.','',
    'A required hero going down, a required asset disappearing, or a failed objective ends the attempt. Active objective cleanup runs even if another cleanup throws. Markers, cameras and mission AI ownership are released. Player death uses the existing movement-verified recovery; it does not regroup surviving teammates. Retry creates a NEW mission at stage zero with new vehicles, actors, timers and work progress. It never restores a position-only checkpoint. The mission key retries the failed job; the debug mission page also offers Retry last attempt and retains the reason. Explicitly selecting another unlocked job changes the selection.','',
    'Abort or failure pays nothing. Committed first completion pays once; replay cannot duplicate cash, gold, or unlocks. The debug Force pass/Mark complete tools deliberately bypass gameplay and grant completion rewards.','',
    'Mandatory role handoffs block movement, attacks and interactions until switching, while the camera and character wheel remain usable. World simulation and mission timers continue. Parallel stages permit any character with unfinished work. The gate uses frame inputs, never a persistent player freeze.','',
    '## Reading the checks','',
    '| Objective | Completion / failure rule |','|---|---|',
    '| ReachZone | Required hero inside the configured marker radius; a vehicle only when explicitly required. |',
    '| MissionInteraction | Required hero close enough, on foot or aboard the required vehicle; press E / D-pad Right and stay. Unloading also requires stopping. Missing work vehicle fails. |',
    '| AssignedWork | Named NPC travels to the actual work site and accumulates work time there while you control the other role. Worker death fails. |',
    '| Kill / Subdue / Waves | All required targets resolved; wave spawns must succeed. Subdue requires stun/cuffs and fails if a target is killed. Missing unresolved actors fail. |',
    '| Enter / Deliver | Actual vehicle and required hero; extraction can additionally require all three aboard. Destroyed vehicles fail. Delivery checks the destination, not a replacement vehicle. |',
    '| TrailerDelivery | The specific tanker is coupled to the tractor, both stopped, and the tanker is within 35m of unloading. |',
    '| Shadow | Approach within the acquisition window, then hold the specified distance band. After acquisition, eight seconds outside the band fails. |',
    '| MultiHold | Visit every marked site, press the interaction button and finish each timed operation. Nearest unfinished site receives the route. |',
    '| Passive rules | Protect, detection, speed and altitude constrain the active stage. They never count as the action needed to finish it. |','']
    seen=set();gates=story_gates()
    for p in sorted((ROOT/'src/Bloodlines/Missions/Campaign').rglob('*.cs'),key=lambda p:p.name):
        s=p.read_text(encoding='utf-8');mid=re.search(r'override string Id\s*=>\s*"(S?M\d\d)"',s)
        if not mid:continue
        mid=mid[1];seen.add(mid);info=infos[mid]
        gate=gates.get(mid)
        gate_text=f' Story gate: {", ".join(gate)} must be complete first (QA may bypass).' if gate else ''
        out += [f'## {mid} — {info["title"]}','',f'Prerequisite: {info["prerequisite"] or "none"}.{gate_text} Retry: full mission restart.','']
        if mid=='M01':
            out += ['Guess begins in his approach car; Ice and Gohan have separate exterior approaches. Complete each opening role: Guess reaches the prototype, Gohan copies the ledger at the terminal, Ice identifies Mateo from overwatch. Shooting before recognition blows cover. The recognition call keeps everyone at their actual position. Mateo runs to a launch while the crew defeats the guards. Extract in the actual four-seat prototype and bring all three clear of the exit. Wrecking the required vehicle, losing a brother, missing escape assets or a blocked Mateo escape fails clearly.','']
        elif mid=='M02':
            out += ['Guess drives the occupied Granger to the moving van. Gohan hacks automatically while alive aboard the chase car and within the proximity band; you can drive as Guess or switch to Gohan. Leaving range pauses progress. Van occupants begin shooting halfway through. On completion the van stops: Ice exits and spends three seconds at the rear doors to collect the drives. Killing a driver or stealing the van is not the pickup trigger. Rejoin the Granger with all three and drive to the canal escape marker. Losing the van before the hack, the Granger, or a required brother fails.','']
        else:
            owner=(re.search(r'Crew\.Deploy(?:Solo)?\(CrewSlot\.(\w+)',s) or re.search(r'PlayedBy\(CrewSlot\.(\w+)',s))
            owner=owner[1] if owner else 'as assigned'
            out += ['| Step | Role | Stage and on-screen base instruction |','|---|---|---|']
            declared=declared_objectives(s)
            for n,block in enumerate(stages(s),1):
                name=re.match(r'"([^"]+)"',block)[1]
                role=re.search(r'(?:OwnedBy|PlayedBy)\(CrewSlot\.(\w+)\)',block) or re.search(r'RequiredCharacter\s*=\s*CrewSlot\.(\w+)',block)
                if role:owner=role[1]
                labels=re.findall(r'new (\w+(?:Objective|Interaction))\(\s*"([^"]+)"',block)
                labels=[f'{kind}: {text}' for kind,text in labels if text]
                # An objective handed in by field name was declared before the stage.
                for ident in re.findall(r'\b(_\w+)\b',block):
                    if ident in declared and f'{declared[ident][0]}: {declared[ident][1]}' not in labels:
                        labels.append(f'{declared[ident][0]}: {declared[ident][1]}')
                if 'new SurfaceSubObjective(' in block:labels.append('SurfaceSubObjective: Gohan: surface the Kraken at the support marker.')
                if not labels:labels=['Follow the current objective; detailed rule is defined by this stage’s objective type.']
                out += [f'| {n} | {owner} | **{name}** — '+ '<br>'.join(labels).replace('|','/')+' |']
            out += ['','Final gameplay dialogue drains before the pass/aftermath transition.','']
        keys=sorted(set(re.findall(r'"((?:S?M\d\d|Base)\.[A-Za-z0-9.]+)"',s)))
        if keys:out += ['Survey references: '+', '.join(keys)+'.','']
    assert seen==implemented,seen^implemented
    out+=['## Boundaries of this audit','',
    'The harness exercises objective progression, role ownership, fail/retry cleanup, scene release and persistence with GTA stand-ins. It does not establish road driveability, roof access, helicopter aim, swimming/streaming, trailer physics or camera framing. New desert positions remain estimates; safe-ground/water checks reject unavailable sites instead of deploying in the sky. Use the survey when you return. M31–M70 and SM07–SM09 are detailed in CAMPAIGN-REMAINDER.md and remain unimplemented gameplay.','']
    return '\n'.join(out)
if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--check',action='store_true');args=parser.parse_args()
    target=ROOT/'docs/PLAYABLE-MISSION-MAP.md';text=render()
    if args.check:
        if not target.exists() or target.read_text(encoding='utf-8')!=text:raise SystemExit('Mission map is stale: run tools/audit_campaign.py')
        print('Mission map freshness verified')
    else:target.write_text(text,encoding='utf-8');print('Wrote',target.name)
