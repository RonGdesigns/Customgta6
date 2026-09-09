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
    production = ['Crew/CrewDurability.cs','Crew/MilitaryResponse.cs','Crew/CompanionLife.cs','Crew/PersonalWanted.cs','Crew/CompanionConvoy.cs','Crew/CompanionController.cs','Crew/CompanionDriver.cs','Crew/DeathController.cs','Crew/RecoveryMobility.cs','Core/SurveyMode.cs','Core/LocationBook.cs','Core/DataTable.cs','Missions/Objectives/Objective.cs']
    roster=(ROOT/'src/Bloodlines/Crew/CrewRoster.cs').read_text(encoding='utf-8')
    first=roster.index('        public void SetActive(CrewSlot slot)');last=roster.index('        public void AssignCompanionAI()',first)
    handover=BUILD/'RosterHandover.cs'
    handover.write_text('using GTA;using Bloodlines.Core;using Bloodlines.Crew;public class RosterHandover{public CompanionController _companions=new CompanionController(new ModConfig());public CrewSlot ActiveSlot;public System.Collections.Generic.Dictionary<CrewSlot,Ped> Peds=new System.Collections.Generic.Dictionary<CrewSlot,Ped>();public Ped PedFor(CrewSlot slot)=>Peds[slot];private void ProtectCrew(Ped ped){}private void RefreshCompanionBlips(){}private void AssignCompanionAI(){}'+roster[first:last]+'}',encoding='utf-8')
    first=roster.index('        public bool ReviveActiveAt(');last=roster.index('        /// <summary>Explicit whole-crew',first)
    active_recovery=BUILD/'ActiveRecoveryRoster.cs'
    active_recovery.write_text('using GTA;using GTA.Math;using GTA.Native;using Bloodlines.Core;using Bloodlines.Crew;public class ActiveRecoveryRoster{public bool IsDeployed=true;public CompanionController _companions=new CompanionController(new ModConfig());public CrewSlot ActiveSlot;public Protagonist Active=>Protagonist.Of(ActiveSlot);public System.Collections.Generic.Dictionary<CrewSlot,Ped> Peds=new System.Collections.Generic.Dictionary<CrewSlot,Ped>();public Ped PedFor(CrewSlot slot)=>Peds[slot];private const int StartingArmor=100;private void ProtectCrew(Ped ped){}'+roster[first:last]+'}',encoding='utf-8')
    files = [ROOT/'src/Bloodlines'/p for p in production] + [generated,handover,active_recovery] + sorted((ROOT/'tests').glob('*.cs'))
    args.extend(f'"{p}"' for p in files)
    rsp = BUILD/'tests.rsp'; rsp.write_text('\n'.join(args),encoding='utf-8')
    subprocess.run([build_roslyn.find_csc(),'/noconfig','@'+str(rsp)],check=True)
    # A fresh directory per run prevents stale INIs from hiding persistence bugs.
    import tempfile
    with tempfile.TemporaryDirectory(prefix='survey-', dir=BUILD) as temp:
        subprocess.run([str(output),temp],check=True)

if __name__ == '__main__':
    main()
