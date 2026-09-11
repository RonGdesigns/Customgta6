"""Temporary checksum-verified comparison patch transport; removed after testing."""
from pathlib import Path
import base64, hashlib, json, os, re, subprocess, zlib
ROOT = Path(__file__).resolve().parents[2]
os.chdir(ROOT)
BASE = 'bb073aa9b73d4cb5421c922ad01f0e85d402bb3b'
EXPECTED = 'b8b56dd3be74a4e5dcb677e2e3679d3ce05f850aef188feebf228e07faf6db16'
parts = [Path('tools/ci/port_heist_patch.part' + str(i)) for i in range(4)]
text = [p.read_text(encoding='ascii').strip() for p in parts]
text[0] = text[0].replace('MzbrhzW3j', 'MzbrhzWz3j', 1)
text[1] = text[1].replace('BIHulZZtUb', 'BIHulZtUb', 1).replace('wp/OezZZ29', 'wp/OezZ29', 1).replace('RSkkTniTu', 'RSkkniTu', 1)
tail = Path('tools/ci/port_heist_patch.tail3')
text[3] = text[3][:4574] + tail.read_text(encoding='ascii').strip()
expected_parts = ['3cb90f5efeb1aedc99062cd1c969c48e5043ad24', '25defd40ae90e5593ca7451808d3cf6033314377', '2c4f33db8c928d57ad65224b2a3b550c2ae1b194', '722f6895150a2014c815c231b028f153799b4f7c']
for i, chunk in enumerate(text):
    raw = chunk.encode('ascii')
    digest = hashlib.sha1(('blob ' + str(len(raw)) + '\0').encode() + raw).hexdigest()
    print('Transport segment', i, len(raw), digest)
    if digest != expected_parts[i]: raise SystemExit('Transport mismatch; no implementation files touched.')
patch = zlib.decompress(base64.b64decode(''.join(text), validate=True))
if hashlib.sha256(patch).hexdigest() != EXPECTED: raise SystemExit('Full patch checksum mismatch.')
paths = []
for a, b in re.findall(rb'^diff --git a/(\S+) b/(\S+)$', patch, re.MULTILINE):
    if a != b: raise SystemExit('Unexpected rename.')
    path = b.decode('ascii')
    if '..' in path.split('/') or not path.startswith(('src/Bloodlines/', 'tests/story/', 'docs/', 'tools/run_story_tests.py')): raise SystemExit('Unexpected path: ' + path)
    paths.append(path)
if len(paths) != 27: raise SystemExit('Unexpected patch file count.')
subprocess.run(['git', 'fetch', '--depth=1', 'origin', BASE], check=True)
subprocess.run(['git', 'diff', '--exit-code', BASE, 'HEAD', '--'] + paths + ['tests/story/OpenSliceTests.cs'], check=True)
subprocess.run(['git', 'config', 'core.autocrlf', 'false'], check=True)
crlf = set()
for path in paths:
    target = Path(path)
    if not target.exists(): continue
    raw = subprocess.check_output(['git', 'show', BASE + ':' + path])
    if b'\r\n' in raw: crlf.add(path)
    target.write_bytes(raw.replace(b'\r\n', b'\n'))
Path('build').mkdir(exist_ok=True)
patch_path = Path('build/port-heist-implementation.diff')
patch_path.write_bytes(patch.replace(b'\r\n', b'\n'))
subprocess.run(['git', 'apply', '--unidiff-zero', '--whitespace=nowarn', '--check', str(patch_path)], check=True)
subprocess.run(['git', 'apply', '--unidiff-zero', '--whitespace=nowarn', str(patch_path)], check=True)
# The previous test matched the old display-title expression rather than ordering.
# Keep the ordering assertion and verify the current call passes the captured blocking.
f = Path('tests/story/OpenSliceTests.cs')
s = f.read_text(encoding='utf-8')
a = s.index('  int ask=manager.IndexOf(')
b = s.index('\n\n  // ---- Prologue:', a)
s = s[:a] + '''  int ask=manager.IndexOf("outro = _current.OutroBlocking()",StringComparison.Ordinal);
  int finish=ask<0?-1:manager.IndexOf("Finish();",ask,StringComparison.Ordinal);
  int play=manager.IndexOf("_context.Cutscenes.Play(outroId, \\"outro\\", \\"Aftermath: \\" + outcomeTitle, null, null, outro)",StringComparison.Ordinal);
  Check(ask>=0&&finish>ask&&play>finish,"The manager captures aftermath blocking before teardown and passes it to the final scene");''' + s[b:]
f.write_bytes(s.replace('\r\n','\n').replace('\n','\r\n').encode('utf-8'))
paths.append(f.as_posix())
# Run the new focused integration cases before the unchanged broader suite.
f = Path('tests/story/StoryTests.cs'); s=f.read_text(encoding='utf-8')
s=s.replace('HeistChecks();ContinuousPortHeistChecks();','HeistChecks();',1)
assert s.count('root=args[1];MarketChecks();')==1
s=s.replace('root=args[1];MarketChecks();','root=args[1];ContinuousPortHeistChecks();MarketChecks();',1)
f.write_bytes(s.encode('utf-8'))
for path in crlf:
    target = Path(path)
    if target.exists(): target.write_bytes(target.read_bytes().replace(b'\r\n', b'\n').replace(b'\n', b'\r\n'))
for p in parts + [tail]:
    p.unlink(); paths.append(p.as_posix())
Path('build/port-heist-changed.json').write_text(json.dumps(paths), encoding='utf-8')
print('Applied scoped source, tests and documentation. Production and behavior verification follow.')
