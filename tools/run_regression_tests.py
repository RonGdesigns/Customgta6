"""Compile selected production classes against test stand-ins; never loads GTA."""
from pathlib import Path
import subprocess
import build_roslyn

ROOT = Path(__file__).resolve().parents[1]
BUILD = ROOT / 'build' / 'regression'

def main():
    BUILD.mkdir(parents=True, exist_ok=True)
    # This objective shares a source file with unrelated native-heavy objectives.
    # Extract its exact class text rather than maintaining a test implementation.
    source = (ROOT/'src/Bloodlines/Missions/Objectives/AdvancedObjectives.cs').read_text(encoding='utf-8')
    start = source.index('    public sealed class ShadowTargetObjective')
    brace = source.index('{', start)
    depth, end = 1, brace + 1
    while depth:
        depth += (source[end] == '{') - (source[end] == '}')
        end += 1
    generated = BUILD/'ShadowTarget.cs'
    generated.write_text('using System; using Bloodlines.Core; using GTA;\nnamespace Bloodlines.Missions.Objectives {\n'+source[start:end]+'\n}', encoding='utf-8')
    framework = Path(build_roslyn.fetch_package(*build_roslyn.REFERENCE_ASSEMBLIES))/'build/.NETFramework/v4.8'
    output = BUILD/'RegressionTests.exe'
    args = ['/nologo','/nostdlib+','/target:exe','/langversion:9.0','/warnaserror+',f'/out:"{output}"']
    for name in ['mscorlib.dll','System.dll','System.Core.dll','System.Drawing.dll']:
        args.append(f'/reference:"{framework/name}"')
    production = ['Crew/CompanionController.cs','Crew/DeathController.cs','Core/SurveyMode.cs','Core/LocationBook.cs','Core/DataTable.cs','Missions/Objectives/Objective.cs']
    files = [ROOT/'src/Bloodlines'/p for p in production] + [generated] + sorted((ROOT/'tests').glob('*.cs'))
    args.extend(f'"{p}"' for p in files)
    rsp = BUILD/'tests.rsp'; rsp.write_text('\n'.join(args),encoding='utf-8')
    subprocess.run([build_roslyn.find_csc(),'/noconfig','@'+str(rsp)],check=True)
    # A fresh directory per run prevents stale INIs from hiding persistence bugs.
    import tempfile
    with tempfile.TemporaryDirectory(prefix='survey-', dir=BUILD) as temp:
        subprocess.run([str(output),temp],check=True)

if __name__ == '__main__':
    main()
