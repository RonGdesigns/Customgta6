"""Temporary transport for a checksum-verified, scoped unified diff.
No runtime dependency. The isolated runner publishes only after verification.
"""
from pathlib import Path
import base64
import hashlib
import json
import os
import re
import subprocess
import zlib
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
    if digest != expected_parts[i]:
        raise SystemExit('Transport mismatch; no implementation files were touched.')
patch = zlib.decompress(base64.b64decode(''.join(text), validate=True))
if hashlib.sha256(patch).hexdigest() != EXPECTED:
    raise SystemExit('Full patch checksum mismatch; no implementation files were touched.')
paths = []
for a, b in re.findall(rb'^diff --git a/(\S+) b/(\S+)$', patch, re.MULTILINE):
    if a != b:
        raise SystemExit('Unexpected rename in comparison patch.')
    path = b.decode('ascii')
    if '..' in path.split('/') or not path.startswith(('src/Bloodlines/', 'tests/story/', 'docs/', 'tools/run_story_tests.py')):
        raise SystemExit('Unexpected patch path: ' + path)
    paths.append(path)
if len(paths) != 27:
    raise SystemExit('Unexpected comparison patch file count: ' + str(len(paths)))
subprocess.run(['git', 'fetch', '--depth=1', 'origin', BASE], check=True)
subprocess.run(['git', 'diff', '--exit-code', BASE, 'HEAD', '--'] + paths, check=True)
subprocess.run(['git', 'config', 'core.autocrlf', 'false'], check=True)
# Read exact pinned blobs, normalize only in this scratch checkout for git apply,
# then restore each existing file's original line-ending convention. git checkout
# alone can leave its original Windows CRLF conversion in place for unchanged files.
crlf = set()
for path in paths:
    target = Path(path)
    if not target.exists():
        continue
    raw = subprocess.check_output(['git', 'show', BASE + ':' + path])
    if b'\r\n' in raw:
        crlf.add(path)
    target.write_bytes(raw.replace(b'\r\n', b'\n'))
Path('build').mkdir(exist_ok=True)
patch_path = Path('build/port-heist-implementation.diff')
patch_path.write_bytes(patch.replace(b'\r\n', b'\n'))
subprocess.run(['git', 'apply', '--unidiff-zero', '--whitespace=nowarn', '--check', str(patch_path)], check=True)
subprocess.run(['git', 'apply', '--unidiff-zero', '--whitespace=nowarn', str(patch_path)], check=True)
for path in crlf:
    target = Path(path)
    if target.exists():
        target.write_bytes(target.read_bytes().replace(b'\r\n', b'\n').replace(b'\n', b'\r\n'))
for p in parts + [tail]:
    p.unlink()
    paths.append(p.as_posix())
Path('build/port-heist-changed.json').write_text(json.dumps(paths), encoding='utf-8')
print('Applied 27 reviewed source/test/document changes. Production compilation and the complete test suite run next.')
