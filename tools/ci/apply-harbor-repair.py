"""Temporary integrity-checked source transport; removed on successful publication.
No network code or game assets are in the patch. GitHub Actions tests before publishing.
"""
import base64, hashlib, json, lzma, pathlib, re, subprocess
ROOT=pathlib.Path(__file__).resolve().parents[2]
import os
os.chdir(ROOT)
BASE='d295306764fa137ceda3e1f3741e2a0d766bdb45'
expected=[
 '35c52a554656d66dd26c7a0c843d9b22e191a1f00f5bfe45b5447009ac0860ea',
 'c36cca59252848d9c455ab8e8fa5573e9836a80bcdc1cf91b230c5c94b4c8cfc',
 '841495d0a4a13ab0c30d6b1b51b705f0f10e16e36f9f0ec330645bf9378657ea',
 '0951e1222e20aafda108c62c55587b00b92f128ea9aa49b97717b2446c491c3a',
 '09bd6bf015674e62433924b779b974b46ede7eb1fc0f9560c145041b5eb39aa5',
 '3c662bba82e24a6688a7d8d4e3935fff08b5ba544e5d9a598c56d8422262e047',
 '2b75f7b8a6a8dd59f16a672cc5cf273a1ff9d8ea2f709aea60655649dd2bea58']
chunks=[]
for i,digest in enumerate(expected):
 chunk=pathlib.Path('tools/ci/harbor-repair.part'+str(i)).read_text(encoding='ascii').strip()
 got=hashlib.sha256(chunk.encode('ascii')).hexdigest()
 print('Transport',i,len(chunk),got)
 assert got==digest, 'Transport checksum mismatch; no implementation files touched'
 chunks.append(chunk)
patch=lzma.decompress(base64.b64decode(''.join(chunks),validate=True))
assert hashlib.sha256(patch).hexdigest()=='c22197ffb6ae925a2474f1d886fe26b044153fde33c300feab260dd25f67e314', 'Patch checksum mismatch'
paths=[]
for a,b in re.findall(rb'^diff --git a/(\S+) b/(\S+)$',patch,re.MULTILINE):
 assert a==b, 'Unexpected rename'
 path=b.decode('ascii')
 assert '..' not in path.split('/') and path.startswith(('src/Bloodlines/','tests/story/','docs/','tools/run_story_tests.py','config/Bloodlines.ini','data/locations.tsv')),path
 paths.append(path)
assert len(paths)==34, paths
subprocess.run(['git','fetch','--depth=1','origin',BASE],check=True)
extra='tests/story/CampaignFlowTests.cs'
subprocess.run(['git','diff','--exit-code',BASE,'HEAD','--']+paths+[extra],check=True)
subprocess.run(['git','config','core.autocrlf','false'],check=True)
# Compile committed source bytes, not a Windows checkout conversion. Normalize only
# the patch application scratch input; then restore the existing blob conventions.
crlf=set()
for name in paths:
 target=pathlib.Path(name)
 if not target.exists():continue
 raw=subprocess.check_output(['git','show',BASE+':'+name])
 if b'\r\n' in raw:crlf.add(name)
 target.write_bytes(raw.replace(b'\r\n',b'\n'))
pathlib.Path('build').mkdir(exist_ok=True)
out=pathlib.Path('build/harbor-source.patch')
out.write_bytes(patch.replace(b'\r\n',b'\n'))
subprocess.run(['git','apply','--unidiff-zero','--whitespace=nowarn','--check',str(out)],check=True)
subprocess.run(['git','apply','--unidiff-zero','--whitespace=nowarn',str(out)],check=True)
# Extend the existing generic flow driver for a new objective type. It physically
# places the test craft at its goal and lets production evaluate real 3D/seat checks.
# Dedicated HarborRepairChecks assert that being below the goal cannot complete it.
f=pathlib.Path(extra)
raw=subprocess.check_output(['git','show',BASE+':'+extra]);s=raw.decode('utf-8').replace('\r\n','\n')
anchor='     else if(name=="TechnicalChoiceObjective")'
assert s.count(anchor)==1, 'Unexpected generic flow driver'
s=s.replace(anchor,'''     // SurfaceSubObjective is a real 3D/driver check. Move the simulated craft
     // to the target; do not force-pass it or weaken the production depth rule.
     else if(name=="SurfaceSubObjective") PositionActor(c,objective,Field<Func<Vector3>>(objective,"_target")(),Field<Func<Vehicle>>(objective,"_sub")());
'''+anchor,1)
f.write_bytes(s.replace('\n','\r\n').encode('utf-8') if b'\r\n' in raw else s.encode('utf-8'))
paths.append(extra)
for name in crlf:
 target=pathlib.Path(name)
 target.write_bytes(target.read_bytes().replace(b'\r\n',b'\n').replace(b'\n',b'\r\n'))
# Unchanged tracked source also uses exact Git bytes for deterministic compilation.
for name in filter(None,subprocess.check_output(['git','ls-files','-z','--','src']).decode().split('\0')):
 if name not in paths:pathlib.Path(name).write_bytes(subprocess.check_output(['git','show','HEAD:'+name]))
pathlib.Path('build/harbor-changed.json').write_text(json.dumps(paths),encoding='utf-8')
print('Applied scoped source, tests, configuration and documentation. Compile and behavioral verification follow.')
