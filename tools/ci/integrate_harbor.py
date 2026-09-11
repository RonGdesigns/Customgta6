"""One-time, branch-only integration; removed after successful verification.
Never updates main, force-pushes, edits a game install, or chooses an old DLL.
"""
from pathlib import Path
import hashlib
import json
import os
import re
import subprocess
import sys

BASE = 'd295306764fa137ceda3e1f3741e2a0d766bdb45'
MAIN = 'f8a37b9837b11f6661df081fd59616b7c06e6bb3'
REPAIR = 'ea4c54667d60d10f8973bfeccdd9713c97499501'
BRANCH = 'codex/harbor-playtest-repair'
MAIN_BRANCH = 'claude/gta-v-custom-version-477edi'
SELF = 'tools/ci/integrate_harbor.py'
WORKFLOW = '.github/workflows/harbor-integration.yml'
REPORT = 'docs/HARBOR-INTEGRATION.md'
BINARY_REPORT = 'docs/HARBOR-INTEGRATION-BINARIES.json'
PREVIEW = '.github/workflows/harbor-repair-preview.yml'
SHARED_SHA256 = {
    'data/locations.tsv': '4963bb1c0293bc5ab9017e422b6676d502eea1e9e8e8871042955a2846fcc83e',
    'docs/CHANGE-REGISTER.md': 'dd01a6d7745c4195b36c66a36eede1c7748baf4d540423730dee99c6e3d3e985',
    'docs/PLAYABLE-MISSION-MAP.md': '46ed5e42be64cbd3a4ef59175ec0629553ebc12e732bfe2176ce30f04dfbdc07',
    'src/Bloodlines/BloodlinesMain.cs': 'f5799c4cdd327412f984c083bc54e9a8bc4530c270f3e7ba8418ea048b816c29',
    'tests/story/CampaignFlowTests.cs': '992fe5bcbade47657a86289740ecb66a69e2c492c4ffb69d258bde2b270d503c',
    'tests/story/RuntimeStubs.cs': 'b516918d8a21e7433c115ad62d53d3649f5f733ed8c1ecca1f3b6d8548f05c7f',
}
METADATA = {PREVIEW, REPORT, BINARY_REPORT, 'docs/LOCATION-AUDIT.md', 'docs/HARBOR-REPAIR-VERIFICATION.md', 'docs/HARBOR-PLAYTEST-REPAIR.md', 'docs/CHANGE-REGISTER.md'}


def git(*args):
    return subprocess.check_output(['git', *args])


def run(*args):
    subprocess.run(list(args), check=True)


def text(*args):
    return git(*args).decode('utf-8').strip()


def remote(branch):
    return text('ls-remote', 'origin', 'refs/heads/' + branch).split()[0]


def tree(ref):
    result = {}
    for line in git('ls-tree', '-r', '-z', ref).split(b'\0'):
        if not line:
            continue
        info, name = line.split(b'\t', 1)
        mode, kind, sha = info.decode().split()
        if kind != 'blob':
            raise RuntimeError('Unexpected tree entry: ' + name.decode())
        result[name.decode()] = (mode, sha)
    return result


def sides():
    b, m, r = (tree(ref) for ref in (BASE, MAIN, REPAIR))
    mc = {k for k in b.keys() | m.keys() if b.get(k) != m.get(k)}
    rc = {k for k in b.keys() | r.keys() if b.get(k) != r.get(k)}
    return m, r, mc, rc


def preserve(allow_metadata=False):
    m, r, mc, rc = sides()
    manifest = {'base': BASE, 'integrated_main': MAIN, 'repair_input': REPAIR,
                'main_exclusive': {}, 'repair_exclusive': {}, 'shared_reviewed': {}}
    for label, names, ref, entries in [('main_exclusive', mc - rc, MAIN, m), ('repair_exclusive', rc - mc, REPAIR, r)]:
        for name in sorted(names):
            expected = git('show', ref + ':' + name)
            actual = Path(name).read_bytes()
            if not (allow_metadata and name in METADATA) and actual != expected:
                raise RuntimeError('Integration lost changes from ' + ref + ': ' + name)
            manifest[label][name] = {'source_blob': entries[name][1], 'sha256': hashlib.sha256(actual).hexdigest(),
                                     'byte_identical': actual == expected}
    if mc & rc != set(SHARED_SHA256) | {'prebuilt/Bloodlines.dll'}:
        raise RuntimeError('Unreviewed shared file set')
    for name, expected in SHARED_SHA256.items():
        actual = hashlib.sha256(Path(name).read_bytes()).hexdigest()
        if not (allow_metadata and name in METADATA) and actual != expected:
            raise RuntimeError('Shared resolution differs from independently reviewed result: ' + name + ' actual=' + actual)
        manifest['shared_reviewed'][name] = actual
    return manifest


def prepare():
    if os.environ.get('GITHUB_REF') != 'refs/heads/' + BRANCH:
        raise RuntimeError('This integration is comparison-branch only')
    if remote(MAIN_BRANCH) != MAIN or remote(BRANCH) != os.environ['GITHUB_SHA']:
        raise RuntimeError('A branch advanced; review the new commits before proceeding')
    run('git', 'config', 'core.autocrlf', 'false')
    Path('.git/info/attributes').write_text('* -text\n', encoding='ascii')
    # Fresh disposable CI checkout, never the user's working tree.
    run('git', 'reset', '--hard', 'HEAD')
    run('git', 'diff', '--exit-code')
    run('git', 'config', 'user.name', 'github-actions[bot]')
    run('git', 'config', 'user.email', '41898282+github-actions[bot]@users.noreply.github.com')
    assert text('merge-base', MAIN, REPAIR) == BASE
    result = subprocess.run(['git', 'merge', '--no-commit', '--no-ff', MAIN], capture_output=True)
    Path('build').mkdir(exist_ok=True)
    Path('build/harbor-merge.log').write_bytes(result.stdout + result.stderr)
    print(result.stdout.decode('utf-8', errors='replace'))
    conflicts = set(text('diff', '--name-only', '--diff-filter=U').splitlines())
    if result.returncode != 1 or conflicts != {'prebuilt/Bloodlines.dll', 'tests/story/RuntimeStubs.cs'}:
        raise RuntimeError('Unexpected merge outcome or conflicts: ' + repr(conflicts))
    f = Path('tests/story/RuntimeStubs.cs')
    source = f.read_text(encoding='utf-8')
    def resolution(match):
        ours, theirs = match.group(1), match.group(2)
        if 'public enum Hash' in ours:
            assert ours.startswith(' public enum Hash { GET_PED_RELATIONSHIP_GROUP_HASH,')
            assert theirs.startswith(' public enum Hash { SET_PED_FLEE_ATTRIBUTES,GET_CLOSEST_VEHICLE_NODE_WITH_HEADING,')
            assert 'SET_PED_FLEE_ATTRIBUTES' not in ours and 'GET_CLOSEST_VEHICLE_NODE_WITH_HEADING' not in ours
            return ours.replace(' public enum Hash { ', ' public enum Hash { SET_PED_FLEE_ATTRIBUTES,GET_CLOSEST_VEHICLE_NODE_WITH_HEADING,', 1)
        assert ours.count('\n') == 1 and theirs.count('\n') == 1
        assert ours.startswith(' public static class GameUtils ') and theirs.startswith(' public static class GameUtils ')
        return theirs.split('\n')[0] + '\n' + ours.split('\n')[1]
    source, count = re.subn(r'<<<<<<< HEAD\n(.*?)\n=======\n(.*?)\n>>>>>>> [^\n]+', resolution, source, flags=re.S)
    assert count == 2, 'Expected exactly the two reviewed test-definition conflicts'
    f.write_bytes(source.encode('utf-8'))
    run('git', 'add', '--', str(f))
    f = Path('data/locations.tsv')
    normalized = f.read_bytes().replace(b'\r\n', b'\n')
    assert hashlib.sha256(normalized).hexdigest() == 'a559add78bb7db16489b22834de36055b38ca959346696708e1285893806cab2', 'Unexpected merged location rows'
    f.write_bytes(normalized.replace(b'\n', b'\r\n'))
    run(sys.executable, 'tools/audit_campaign.py')
    f = Path('docs/PLAYABLE-MISSION-MAP.md')
    normalized = f.read_bytes().replace(b'\r\n', b'\n')
    assert hashlib.sha256(normalized).hexdigest() == SHARED_SHA256[str(f).replace('\\', '/')], 'Unexpected generated map content'
    f.write_bytes(normalized)
    manifest = preserve()
    Path('build/harbor-preservation.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    print('Verified all 14 main-only and 31 repair-only changes, plus six shared resolutions.')
    f = Path(PREVIEW)
    s = f.read_text(encoding='utf-8')
    s = s.replace("'base':'" + BASE + "','dll_sha256'", "'base':'" + BASE + "','integrated_main':'" + MAIN + "','dll_sha256'", 1)
    s = s.replace('Based on d295306: includes the main-branch integrated heist, desert missions and apartment tiers.',
                  'Integrated with main f8a37b9 through PR #32: retains the latest M03-M08 repairs, integrated heist, desert missions and apartment tiers.', 1)
    s = s.replace("'HARBOR-REPAIR-VERIFICATION.md','CONTINUOUS-PORT-HEIST.md']", "'HARBOR-REPAIR-VERIFICATION.md','CONTINUOUS-PORT-HEIST.md','HARBOR-INTEGRATION.md','HARBOR-INTEGRATION-PRESERVATION.json','HARBOR-INTEGRATION-BINARIES.json']", 1)
    assert MAIN in s and 'HARBOR-INTEGRATION.md' in s
    f.write_bytes(s.encode('utf-8'))
    f = Path('docs/HARBOR-PLAYTEST-REPAIR.md')
    s = f.read_text(encoding='utf-8')
    s = s.replace('## Owner report and scope',
        '## Integration with current main\n\nThe comparison now includes main `' + MAIN + '` through PR #32. M03-M08 and the main-only placement/vehicle helpers are byte-identical to that main snapshot. Shared definitions retain both changes; the DLL is rebuilt from the combined source. See HARBOR-INTEGRATION.md and HARBOR-INTEGRATION-PRESERVATION.json for the evidence. Main itself is not updated by this integration.\n\n## Owner report and scope', 1)
    f.write_bytes(s.encode('utf-8'))
    f = Path('docs/CHANGE-REGISTER.md')
    f.write_bytes(f.read_bytes() + ('\n## Harbor integration through PR #32\n\nMain `' + MAIN + '` is incorporated into the repair branch without rewriting M03-M08. The conflicting test definitions are combined, not replaced; the compiled DLL is rebuilt. Both sets of checks remain enabled. See HARBOR-INTEGRATION.md for verification and the preserved-file manifest. PR #28 remains a draft comparison, with live GTA acceptance pending.\n').encode('utf-8'))


def inspect_binaries():
    original_main, original_repair, combined = json.loads(Path('build/binary-comparison/dll-inspection.json').read_text(encoding='utf-8'))
    assert original_main['Sha256'] == 'ae8a81ed1cafae6c398fef59269d0d857910cae309ca914daf86548b0c9de7f6'
    assert original_repair['Sha256'] == 'df6d55276297dafef5c2c018d828f4db80316e7370ead561ca29ab0c0d553c6b'
    assert combined['Sha256'] == hashlib.sha256(Path('prebuilt/Bloodlines.dll').read_bytes()).hexdigest()
    assert combined['References'] == original_main['References'] == original_repair['References']
    report = {'method':'PE/CLI metadata plus symbolic-token-normalized IL, locals and exception regions; mod code not executed',
              'limitations':'A preservation check, not a mathematical proof of behavior or a GTA playthrough. Main source and original main DLL were not independently rebuilt in this job.',
              'inputs': [{k:a[k] for k in ('File','Length','Sha256','Assembly','Version','MetadataVersion','Mvid','Machine','CorFlags','References')} for a in (original_main,original_repair,combined)],
              'main_groups':{},'repair_groups':{}}
    groups = [(original_main,'main_groups',['M03CypressFoundry','M04SeveredWire','M05TidalLock','M06CleanSweep','M07WiretapWaltz','M08SupplyAndSever','ForksUnderCrateObjective','GameUtils','CrewVan','MissionSites']),
              (original_repair,'repair_groups',['MarineColumn','IMarineProbe','MarineSites','UnderwaterGuidance','MissionPresentation','TacticalResponse','PortHeistOperation','PortHeistWorld','Mission','MissionManager','CampaignState','MissionMarkers','ModConfig','M17SubZeroPayload','M18TheStagingLine','M19UnderwaterBreach','M20SkyHook','M21OpenWater','M22ScorchedBay'])]
    for original,label,names in groups:
        for name in names:
            selected={k:v for k,v in original['Methods'].items() if k.split('::')[0].split('+')[0].split('.')[-1]==name}
            assert selected, 'Missing expected compiled type: '+name
            for k,v in selected.items():
                current=combined['Methods'].get(k)
                assert current and all(v[a]==current[a] for a in ('NormalizedIlSha256','Attributes','Implementation')), 'Compiled method changed unexpectedly: '+k
            report[label][name]={'methods_checked':len(selected),'changed_or_missing':0}
    scripts=[n for n,v in combined['Types'].items() if v['Base']=='GTA.Script']
    assert set(scripts)=={'Bloodlines.BloodlinesMain','Bloodlines.DevTools'}, scripts
    report['script_entrypoints']=scripts
    Path(BINARY_REPORT).write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print('Compiled method preservation verified for both agents, not merely DLL filenames or sizes.')


def publish():
    manifest = preserve(allow_metadata=True)
    binary = Path('src/Bloodlines/bin/Release/Bloodlines.dll').read_bytes()
    assert binary == Path('prebuilt/Bloodlines.dll').read_bytes(), 'Prebuilt is not the combined build'
    assert binary == Path('build/deploy/scripts/Bloodlines.dll').read_bytes(), 'Package is not the combined build'
    inspect_binaries()
    digest = hashlib.sha256(binary).hexdigest()
    manifest['combined_dll_sha256'] = digest
    Path('docs/HARBOR-INTEGRATION-PRESERVATION.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    story = Path('build/story-integration.log').read_text(encoding='utf-8-sig', errors='replace')
    regression = Path('build/regression-integration.log').read_text(encoding='utf-8-sig', errors='replace')
    story_count = re.findall(r'(\d+) story/runtime checks passed', story)
    regression_count = re.findall(r'(\d+) checks passed', regression)
    assert story_count and regression_count, 'Missing passing check summaries'
    info = ('# Harbor integration - both agents preserved\n\n'
        + 'Main input: `' + MAIN + '` (through merged PR #32).\n\n'
        + 'Repair input: `' + REPAIR + '`. Common base: `' + BASE + '`.\n\n'
        + 'Integration input: `' + os.environ['GITHUB_SHA'] + '`; Actions run `' + os.environ['GITHUB_RUN_ID'] + '`.\n\n'
        + '## Preservation and resolution\n\n'
        + 'All 14 paths changed only on main remain byte-identical to main. This includes the complete M03, M04, M05, M06, M07 and M08 mission classes; CrewVan, GameUtils and MissionSites; and the other agent\'s exclusive tests, QA and handoff entries. No source edits were made to those mission implementations.\n\n'
        + 'All 31 paths changed only by the harbor repair were checked against the repair input before integration metadata was updated. They retain the marine fixes, one-sitting rule, police sighting, banner/music/map work and tests. The attached JSON identifies metadata-only documentation/workflow differences explicitly.\n\n'
        + 'The only source conflict was RuntimeStubs.cs: native names and ground-placement helpers from main are combined with the repair\'s police/music/geometry definitions. BloodlinesMain keeps the grounded-spawn tick AND presentation service. CampaignFlowTests keeps the driving fixture AND real simulated sub-surfacing check. The two M06 location rows and both new harbor keys are retained. The mission map is regenerated.\n\n'
        + 'The location validator now reports the combined 191 rows instead of the historical 189. LOCATION-AUDIT.md is retained as generated output, not hand-edited geometry. Campaign and feasibility documents were fully compared after generation, allowing only newline conversion, and retain their committed bytes. No freshness assertion was removed.\n\n'
        + '## Binary comparison\n\n'
        + 'Both input DLLs and the combined DLL were read with PEReader, not loaded as executable plugins. Assembly references, type/method inventories, IL instructions, local signatures and exception regions were inspected. Method references were resolved to symbolic names before comparison so metadata-token reordering is not confused with code changes. HARBOR-INTEGRATION-BINARIES.json records exact hashes and group counts. The 386 compiled methods (including generated helpers) of M03-M08 match main. Selected main placement/forklift helpers and the repair marine/police/presentation/operation classes also match their respective input DLLs. This is not live GTA validation.\n\n'
        + 'The DLL conflict is resolved with a fresh combined build, not ours/theirs. SHA-256: `' + digest + '`.\n\n'
        + '## Actual verification\n\n'
        + '- Production warnings-as-errors compilation passed.\n'
        + '- ' + story_count[-1] + ' story/runtime checks passed, retaining both suites.\n'
        + '- ' + regression_count[-1] + ' behavioral regression checks passed.\n'
        + '- 3 parser tests, mission lint, district-level location checks, authored-scene freshness, mission-map freshness, campaign/feasibility regeneration passed.\n'
        + '- Roslyn rebuild and clean packaging passed; fresh/prebuilt/packaged DLL bytes agree.\n'
        + '- Exactly two GTA.Script entrypoints remain.\n\n'
        + '## Scope and remaining live checks\n\n'
        + 'Main is not merged into or rewritten. This commit integrates main INTO the separate repair branch. PR #28 remains draft. No game installation, personal INI, survey, save or runtime dependency was changed.\n\n'
        + 'The whole Port Heist restarts M19 after failure/abort/quit; no phase checkpoints return. Test the actual water worksite, submarine launch, every reachable yellow sphere, cargo attachment, boat/road transfers, final deposit and one success result. Test aircraft police sight and roof/tunnel occlusion, score audibility and cleanup, letter map starts, apartment rendering, and the preserved M03/M05/M06/M07/M08 behaviors. Unit tests do not simulate GTA physics/streaming/pathfinding.\n\n'
        + 'A successful preservation check does not certify every inherited design decision. This integration does not rewrite M04, expand weapon policy or silently fix unrelated mission behavior. Any follow-up correction should be its own reviewable change.\n')
    Path(REPORT).write_text(info,encoding='utf-8')
    f=Path('docs/HARBOR-REPAIR-VERIFICATION.md')
    old=f.read_text(encoding='utf-8').replace('# Harbor repair source verification','# Historical isolated harbor repair verification',1)
    f.write_text('# Current integrated harbor verification\n\nMain input: `'+MAIN+'`. See HARBOR-INTEGRATION.md and its preservation/binary manifests.\n\nCombined DLL SHA-256: `'+digest+'`.\n\n'+story_count[-1]+' story/runtime checks and '+regression_count[-1]+' regression checks passed. No live GTA playthrough was performed.\n\n---\n\n'+old,encoding='utf-8')
    _,_,mc,rc=sides()
    expected_changes=mc|METADATA|{'docs/HARBOR-INTEGRATION-PRESERVATION.json',SELF,WORKFLOW}
    changed=set(text('diff','--name-only','HEAD').splitlines())
    assert changed<=expected_changes,'Unexpected working changes: '+repr(changed-expected_changes)
    Path(SELF).unlink();Path(WORKFLOW).unlink()
    staged=sorted(expected_changes & (set(tree('HEAD'))|{REPORT,BINARY_REPORT,'docs/HARBOR-INTEGRATION-PRESERVATION.json'}))
    run('git','add','--',*staged)
    assert not text('diff','--name-only','--diff-filter=U'), 'Unresolved conflict'
    assert text('rev-parse','MERGE_HEAD')==MAIN,'Wrong merge parent'
    run('git','-c','core.whitespace=blank-at-eol,blank-at-eof,space-before-tab,cr-at-eol','diff','--cached','--check')
    if remote(MAIN_BRANCH)!=MAIN or remote(BRANCH)!=os.environ['GITHUB_SHA']:
        raise RuntimeError('A branch advanced during verification; refusing to overwrite newer work')
    run('git','commit','-m','merge: preserve main through PR32 and harbor repairs in one verified build')
    run('git','push','origin','HEAD:refs/heads/'+BRANCH)
    print('Published combined repair branch:',text('rev-parse','HEAD'),'DLL SHA256:',digest)
    assert remote(MAIN_BRANCH)==MAIN,'Main changed independently during final publication; recheck before eventual merge'


if __name__=='__main__':
    if len(sys.argv)!=2 or sys.argv[1] not in {'prepare','publish'}:
        raise SystemExit('Use prepare or publish in the isolated comparison workflow')
    if sys.argv[1]=='prepare':prepare()
    else:publish()
