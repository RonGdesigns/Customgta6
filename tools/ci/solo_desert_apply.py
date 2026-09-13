"""One-time, checksum-verified patch transport. Removed after verified publication."""
from pathlib import Path
import base64, hashlib, json, os, re, subprocess, zlib
os.chdir(Path(__file__).resolve().parents[2])
BASE = 'de923e018203b76c55e1b0b475528e6b06aff3b6'
EXPECTED = 'b38cf324269d93ad1c81b2289b8efe29bf8cac534c5ace12cde6ed0a80527bb7'
expected_parts = ['b6da1ed50e11ba4c6cfdba779a343ea9e31a53ce74c8e3a976b3f5db49837df4','f418f86544de8290b36e8b926923c7dd488634ea57038866a73357cbadd6a3f2','44d540dccbdf5cdf5f700d1ce21cc5090e13ff33985a8aa39ecabe331077b220','fae5b6766f4c5eb976c3cef363179698cd0a665f68480c6dd198d100fa73e1c0','d5e8d3455a2ff5c053d7b64399a2d1acd97f124b61dff3a85a898b470a1e1492','4eecb7c18f2fb730ce07fa21a38efedf95e7b4d112fce82b06c3b82e39dd97ca','c5f4ad20bb8f46e7c721bcff2643603a078159460232a1cdae370650f04c8a66']
parts = [Path('tools/ci/solo_desert_payload.'+str(i)).read_text(encoding='ascii').strip() for i in range(7)]
# Correct one identified text-transport omission. Full segment and patch checksums
# still must match the locally generated source diff before anything is applied.
parts[0] = parts[0].replace('HuIAnb9qJeVc','HuIAnb9qFFtWZ35hIQCSRp/9qJeVc',1)
for i, part in enumerate(parts):
    digest = hashlib.sha256(part.encode('ascii')).hexdigest()
    print('Transport',i,len(part),digest)
    if digest != expected_parts[i]: raise SystemExit('Transport mismatch; no source files touched.')
patch = zlib.decompress(base64.b64decode(''.join(parts),validate=True))
if hashlib.sha256(patch).hexdigest() != EXPECTED: raise SystemExit('Patch integrity mismatch.')
paths=[]
for a,b in re.findall(rb'^diff --git a/(\S+) b/(\S+)$',patch,re.MULTILINE):
    name=b.decode('ascii')
    if a!=b or '..' in name.split('/') or not name.startswith(('src/Bloodlines/','tests/story/','data/','docs/','tools/run_story_tests.py')):
        raise SystemExit('Unexpected patch path: '+name)
    paths.append(name)
if len(paths)!=32 or len(set(paths))!=32: raise SystemExit('Unexpected patch scope.')
subprocess.run(['git','fetch','--depth=1','origin',BASE],check=True)
subprocess.run(['git','diff','--exit-code',BASE,'HEAD','--']+paths,check=True)
subprocess.run(['git','config','core.autocrlf','false'],check=True)
# Build every source from its exact Git bytes, not automatic checkout conversions.
for name in filter(None,subprocess.check_output(['git','ls-files','-z','--','src']).decode().split('\0')):
    Path(name).write_bytes(subprocess.check_output(['git','show','HEAD:'+name]))
crlf=set()
for name in paths:
    f=Path(name)
    if not f.exists(): continue
    raw=subprocess.check_output(['git','show',BASE+':'+name])
    if b'\r\n' in raw: crlf.add(name)
    f.write_bytes(raw.replace(b'\r\n',b'\n'))
Path('build').mkdir(exist_ok=True)
Path('build/repair-source.patch').write_bytes(patch)
subprocess.run(['git','apply','--unidiff-zero','--whitespace=nowarn','--check','build/repair-source.patch'],check=True)
subprocess.run(['git','apply','--unidiff-zero','--whitespace=nowarn','build/repair-source.patch'],check=True)
# SHVDN 3.6.0 has no named None member on BoatMissionFlags; zero is no flags.
f=Path('src/Bloodlines/Missions/Campaign/Act2/M25BountyHuntersCanyon.cs')
s=f.read_text(encoding='utf-8'); assert s.count('BoatMissionFlags.None')==1
f.write_text(s.replace('BoatMissionFlags.None','(BoatMissionFlags)0'),encoding='utf-8')
for name in crlf:
    f=Path(name);f.write_bytes(f.read_bytes().replace(b'\r\n',b'\n').replace(b'\n',b'\r\n'))
Path('build/repair-paths.json').write_text(json.dumps(paths+['docs/LOCATION-AUDIT.md']),encoding='utf-8')
print('Applied 32 bounded source/data/test/document changes. Verification follows; nothing is merged.')
