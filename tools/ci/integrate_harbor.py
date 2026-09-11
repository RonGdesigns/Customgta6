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
PREVIEW = '.github/workflows/harbor-repair-preview.yml'
SHARED_SHA256 = {
    'data/locations.tsv': '4963bb1c0293bc5ab9017e422b6676d502eea1e9e8e8871042955a2846fcc83e',
    'docs/CHANGE-REGISTER.md': 'dd01a6d7745c4195b36c66a36eede1c7748baf4d540423730dee99c6e3d3e985',
    'docs/PLAYABLE-MISSION-MAP.md': '46ed5e42be64cbd3a4ef59175ec0629553ebc12e732bfe2176ce30f04dfbdc07',
    'src/Bloodlines/BloodlinesMain.cs': 'f5799c4cdd327412f984c083bc54e9a8bc4530c270f3e7ba8418ea048b816c29',
    'tests/story/CampaignFlowTests.cs': '992fe5bcbade47657a86289740ecb66a69e2c492c4ffb69d258bde2b270d503c',
    'tests/story/RuntimeStubs.cs': 'b516918d8a21e7433c115ad62d53d3649f5f733ed8c1ecca1f3b6d8548f05c7f',
}
METADATA = {PREVIEW, REPORT, 'docs/HARBOR-REPAIR-VERIFICATION.md', 'docs/HARBOR-PLAYTEST-REPAIR.md', 'docs/CHANGE-REGISTER.md'}


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
            raise RuntimeError('Shared resolution differs from independently reviewed result: ' + name)
        manifest['shared_reviewed'][name] = actual
    return manifest


def prepare():
    if os.environ.get('GITHUB_REF') != 'refs/heads/' + BRANCH:
        raise RuntimeError('This integration is comparison-branch only')
    if remote(MAIN_BRANCH) != MAIN or remote(BRANCH) != os.environ['GITHUB_SHA']:
        raise RuntimeError('A branch advanced; review the new commits before proceeding')
    run('git', 'config', 'core.autocrlf', 'false')
    # This is a newly checked-out disposable CI workspace, never a user's checkout.
    # Disable only local EOL conversion so both sides and the new DLL use exact blobs.
    Path('.git/info/attributes').write_text('* -text\n', encoding='ascii')
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
        # Main's new ground-placement helpers, repair's new music config fields.
        return theirs.split('\n')[0] + '\n' + ours.split('\n')[1]
    source, count = re.subn(r'<<<<<<< HEAD\n(.*?)\n=======\n(.*?)\n>>>>>>> [^\n]+', resolution, source, flags=re.S)
    assert count == 2, 'Expected exactly the two reviewed test-definition conflicts'
    f.write_bytes(source.encode('utf-8'))
    run('git', 'add', '--', str(f))
    # The binary conflict intentionally remains until a combined build replaces it.
    run(sys.executable, 'tools/audit_campaign.py')
    manifest = preserve()
    Path('build/harbor-preservation.json').write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    print('Verified all 14 main-only and 31 repair-only changes, plus six shared resolutions.')

    f = Path(PREVIEW)
    s = f.read_text(encoding='utf-8')
    s = s.replace("'base':'" + BASE + "','dll_sha256'", "'base':'" + BASE + "','integrated_main':'" + MAIN + "','dll_sha256'", 1)
    s = s.replace('Based on d295306: includes the main-branch integrated heist, desert missions and apartment tiers.',
                  'Integrated with main f8a37b9 through PR #32: retains the latest M03-M08 repairs, integrated heist, desert missions and apartment tiers.', 1)
    s = s.replace("'HARBOR-REPAIR-VERIFICATION.md','CONTINUOUS-PORT-HEIST.md']", "'HARBOR-REPAIR-VERIFICATION.md','CONTINUOUS-PORT-HEIST.md','HARBOR-INTEGRATION.md','HARBOR-INTEGRATION-PRESERVATION.json']", 1)
    assert MAIN in s and 'HARBOR-INTEGRATION.md' in s
    f.write_bytes(s.encode('utf-8'))

    f = Path('docs/HARBOR-PLAYTEST-REPAIR.md')
    s = f.read_text(encoding='utf-8')
    s = s.replace('## Owner report and scope',
        '## Integration with current main\n\nThe comparison now includes main `' + MAIN + '` through PR #32. M03-M08 and the main-only placement/vehicle helpers are byte-identical to that main snapshot. Shared definitions retain both changes; the DLL is rebuilt from the combined source. See HARBOR-INTEGRATION.md and HARBOR-INTEGRATION-PRESERVATION.json for the evidence. Main itself is not updated by this integration.\n\n## Owner report and scope', 1)
    f.write_bytes(s.encode('utf-8'))
    f = Path('docs/CHANGE-REGISTER.md')
    f.write_bytes(f.read_bytes() + ('\n## Harbor integration through PR #32\n\nMain `' + MAIN + '` is incorporated into the repair branch without rewriting M03-M08. The conflicting test definitions are combined, not replaced; the compiled DLL is rebuilt. Both sets of checks remain enabled. See HARBOR-INTEGRATION.md for verification and the preserved-file manifest. PR #28 remains a draft comparison, with live GTA acceptance pending.\n').encode('utf-8'))


def publish():
    manifest = preserve(allow_metadata=True)
    binary = Path('src/Bloodlines/bin/Release/Bloodlines.dll').read_bytes()
    assert binary == Path('prebuilt/Bloodlines.dll').read_bytes(), 'Prebuilt is not the combined build'
    assert binary == Path('build/deploy/scripts/Bloodlines.dll').read_bytes(), 'Package is not the combined build'
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
        + 'The binary conflict is resolved by compiling combined source, not by choosing either branch\'s old DLL. HARBOR-INTEGRATION-PRESERVATION.json records all protected paths, blob IDs and SHA-256 hashes.\n\n'
        + '## Verified in CI\n\n'
        + '- Production warnings-as-errors build: passed.\n'
        + '- Story/runtime: ' + story_count[-1] + ' checks passed.\n'
        + '- Regression: ' + regression_count[-1] + ' checks passed.\n'
        + '- Dialogue parser, mission lint, coordinate validation, authored-scene freshness and regenerated mission-map freshness: passed.\n'
        + '- Roslyn rebuild and clean package: passed. Fresh/prebuilt/packaged DLLs match.\n\n'
        + 'Current combined DLL SHA-256: `' + digest + '`.\n\n'
        + '## What this does not do\n\n'
        + 'It does not merge PR #28 into main, edit a personal save/INI, change apartment unlocks or add a new story rewrite. No main ref is updated and no force-push is used. The temporary write-enabled integration runner self-removes after verification; the retained preview workflow is read-only.\n\n'
        + 'No GTA runtime was available. Use a copied save and matching DLL/data. Retest the other agent\'s M03-M08 fixes as well as dry sub fittings, actual-water launch, all underwater work points, the continuous M19-M22 run, police air/ground sighting, banner, music and map letters. Fail/abort/quit restarts M19; no checkpoints or mid-heist re-entry. Main may advance later, so compare exact refs before any future merge.\n')
    Path(REPORT).write_text(info, encoding='utf-8')
    old = Path('docs/HARBOR-REPAIR-VERIFICATION.md')
    old.write_text('# Current combined verification\n\nSee [HARBOR-INTEGRATION.md](HARBOR-INTEGRATION.md) for the current combined build through PR #32 and its preservation manifest. Current DLL SHA-256: `' + digest + '`. The original isolated-build evidence below is historical. Live GTA testing remains pending.\n\n---\n\n' + old.read_text(encoding='utf-8'), encoding='utf-8')
    m, r, mc, rc = sides()
    expected_changes = mc | METADATA | {'docs/HARBOR-INTEGRATION-PRESERVATION.json', SELF, WORKFLOW}
    changed = set(text('diff', '--name-only', 'HEAD').splitlines())
    assert changed <= expected_changes, 'Unexpected working changes: ' + repr(changed - expected_changes)
    Path(SELF).unlink()
    Path(WORKFLOW).unlink()
    run('git', 'add', '--', *sorted(expected_changes))
    assert not text('diff', '--name-only', '--diff-filter=U'), 'Unresolved conflict'
    staged = set(text('diff', '--cached', '--name-only').splitlines())
    assert staged <= expected_changes, 'Unexpected staged paths'
    assert text('rev-parse', 'MERGE_HEAD') == MAIN, 'Lost merge parent'
    run('git', '-c', 'core.whitespace=blank-at-eol,blank-at-eof,space-before-tab,cr-at-eol', 'diff', '--cached', '--check')
    if remote(BRANCH) != os.environ['GITHUB_SHA'] or remote(MAIN_BRANCH) != MAIN:
        raise RuntimeError('A branch advanced during testing; no changes published')
    run('git', 'commit', '-m', 'merge: integrate main through PR32 while preserving both agents\' mission fixes')
    run('git', 'merge-base', '--is-ancestor', MAIN, 'HEAD')
    run('git', 'merge-base', '--is-ancestor', REPAIR, 'HEAD')
    run('git', 'push', 'origin', 'HEAD:refs/heads/' + BRANCH)
    print('PUBLISHED COMBINED COMMIT: ' + text('rev-parse', 'HEAD'))
    print('COMBINED DLL SHA256: ' + digest)
    assert remote(MAIN_BRANCH) == MAIN, 'Main changed concurrently; inspect the new work'


if __name__ == '__main__':
    if sys.argv[1:] == ['prepare']:
        prepare()
    elif sys.argv[1:] == ['publish']:
        publish()
    else:
        raise SystemExit('Use prepare or publish in the guarded integration workflow.')
