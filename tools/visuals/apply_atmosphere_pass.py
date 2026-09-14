"""One-time bounded atmosphere edit transport, removed after verified publication.

Does not access GTA or third-party assets. Requires the exact reviewed source blobs;
all changes remain on the existing visuals branch. A failed build publishes no DLL.
"""
from pathlib import Path
import hashlib, json, os, subprocess
root=Path(__file__).resolve().parents[2]
os.chdir(root)
EXPECTED = {'src/Bloodlines/Core/VisualAtmosphere.cs': '04c02754a29dc6b90bfef05a52dd97bb2fe0f245', 'src/Bloodlines/Core/DevMenu.cs': 'eb39b653da39f39f8f727bc5063a52df9d84b85a', 'src/Bloodlines/BloodlinesMain.cs': '5bec1496d97128a054f02185628eb5bea76da5f3', 'tests/story/RuntimeStubs.cs': 'd8ebd979818c5e85bb3ee8a18fc5c39999dea5fa', 'tests/story/StoryTests.cs': 'bc1650a9b043db297fdce7e1116342bf09b46048', 'tests/story/VisualsDamageTests.cs': '90bbf95ffa60ed976f0b48b39b607722ce6495e6', 'tools/run_story_tests.py': 'ecbcf2594f9b36958e8cf96e28f3005cd8ce0ba7', 'docs/VISUALS.md': 'e716043b319b8fb1c58142d5a5063a9c20a327d3', 'CLAUDE.md': 'f243c47390c9ed0ff68dfb3d162702ab8d0afe4c'}
for name, digest in EXPECTED.items():
    raw=subprocess.check_output(['git','show','HEAD:'+name])
    actual=hashlib.sha1(('blob '+str(len(raw))+'\0').encode()+raw).hexdigest()
    if actual!=digest: raise SystemExit('Source changed; stop for review: '+name)
    (root/name).write_bytes(raw)

def write(p,s):
    f=root/p
    raw=f.read_bytes() if f.exists() else b''
    s=s.replace('\r\n','\n')
    f.write_bytes(s.replace('\n','\r\n').encode() if b'\r\n' in raw else s.encode())
def edit(p,a,b):
    s=(root/p).read_text(); assert s.count(a)==1,(p,s.count(a),a[:60]);write(p,s.replace(a,b,1))
p='src/Bloodlines/Core/VisualAtmosphere.cs'
s=(root/p).read_text()
s=s.replace('        private string _activeModifier;\n        private float _activeStrength;\n        private int _lastClockCheck;', '        private readonly TimecycleGrade _grade;\n        private bool _comparisonOff;')
s=s.replace('public string ActiveModifier => _activeModifier;', 'public string ActiveModifier => _grade.Name;').replace('public float ActiveStrength => _activeStrength;', 'public float ActiveStrength => _grade.Strength;')
s=s.replace('            _config = config ?? new ModConfig();', '            _config = config ?? new ModConfig();\n            _grade = new TimecycleGrade(new NativeTimecyclePort(), Logger.Info);')
s=s.replace('        /// <param name="suppressGrading">', '''        public bool GradingComparisonOff => _comparisonOff;
        public string GradingStatus => _comparisonOff ? "baseline grading / " + _grade.Status : _grade.Status;

        /// <summary>Session-only A/B. Does not alter time, weather, LOD, forests, config or saves.</summary>
        public void ToggleGradingComparison()
        {
            _comparisonOff = !_comparisonOff;
            Logger.Info("Visuals: grading comparison " + (_comparisonOff ? "baseline (only our grade fades out)" : "configured (fade back in)") + ". Other visual controls remain unchanged.");
        }

        /// <summary>Release our grade before host paths that bypass the ordinary visuals update.</summary>
        public void SuspendGrading() { _grade.Release(); }

        /// <param name="suppressGrading">''')
s=s.replace('if (_activeModifier != null || _shadowsConfigured || _lodConfigured || _oceanApplied) Reset();','if (ActiveModifier != null || _shadowsConfigured || _lodConfigured || _oceanApplied || _shadowVehicle != null || _reflecting.Count > 0) Reset();')
a=s.index('            if (suppressGrading) { ClearGrade(); return; }')
b=s.index('\n        }',a)
s=s[:a]+'''            string target = ModifierFor(World.CurrentTimeOfDay.TotalHours, out float strength);
            _grade.Update(_comparisonOff ? null : target, strength, Game.GameTime, suppressGrading, Game.IsPaused);'''+s[b:]
a=s.index('        private void ClearGrade()');b=s.index('        /// <summary>The reflection-distance',a)
s=s[:a]+s[b:]
s=s.replace('            Function.Call(Hash.CLEAR_TIMECYCLE_MODIFIER);\n            Function.Call(Hash.CASCADE_SHADOWS_SET_CASCADE_BOUNDS_SCALE, 1.0f);', '            _grade.Release();\n            if (_shadowsConfigured) Function.Call(Hash.CASCADE_SHADOWS_SET_CASCADE_BOUNDS_SCALE, 1.0f);')
s=s.replace('            Function.Call(Hash.RESET_DEEP_OCEAN_SCALER);\n            foreach', '            if (_oceanApplied) Function.Call(Hash.RESET_DEEP_OCEAN_SCALER);\n            foreach')
s=s.replace('            _activeModifier = null;\n            _activeStrength = 0f;\n', '')
# Source comments must not claim these have no measurable cost or perfect external restoration.
s=s.replace('    /// Costs, stated honestly: the timecycle and blur settings are free. The','    /// Measure frame times for all options. The')
s=s.replace('    /// state across a reload and nothing else would ever clear it.', '    /// state across a reload. Registers without getters return to documented\n    /// defaults, not to an unknowable third-party value; disable overlapping features.')
write(p,s)
# Single controller and menu integration, no new Script subclasses.
edit('src/Bloodlines/Core/DevMenu.cs','        public Action ResetCampaign { get; set; }','        public Action ResetCampaign { get; set; }\n        public VisualAtmosphere Visuals { get; set; }')
edit('src/Bloodlines/Core/DevMenu.cs','            var page = new Page("World");','''            var page = new Page("World");
            if (Visuals != null)
            {
                page.Add("Grading comparison", () => Visuals.GradingComparisonOff ? "baseline (grade only)" : "configured",
                    () => Visuals.ToggleGradingComparison());
                page.Add("Grading status", () => Visuals.GradingStatus, null);
            }''')
edit('src/Bloodlines/BloodlinesMain.cs','                _state, _dialogue, _data, _survey, _death, _homes, _dispatches);','                _state, _dialogue, _data, _survey, _death, _homes, _dispatches);\n            _menu.Visuals = _visuals;')
edit('src/Bloodlines/BloodlinesMain.cs','            if (_death.IsHandling) { _phone.Close();', '''            if (_death.IsHandling || _homes.Apartment.Busy || _cutscenes.IsActive || _abilities.IsActive)
                Step("yield visual grade", _visuals.SuspendGrading);
            if (_death.IsHandling) { _phone.Close();''')
edit('src/Bloodlines/BloodlinesMain.cs','_visuals.Update(_cutscenes.IsActive || _homes.Apartment.Inside,','_visuals.Update(_cutscenes.IsActive || _homes.Apartment.Inside || _abilities.IsActive,')
# Both world/startup paths may begin a scene AFTER the main visuals tick.
edit('src/Bloodlines/BloodlinesMain.cs','            if (_cutscenes.IsActive) { _phone.Close(); _characterWheel.Close(); ObjectiveMarkers.Clear(); _missionMarkers.Clear(); return; }', '            if (_cutscenes.IsActive) { Step("yield new scene grade", _visuals.SuspendGrading); _phone.Close(); _characterWheel.Close(); ObjectiveMarkers.Clear(); _missionMarkers.Clear(); return; }')
# Native test state: getter parity, invalid-name probe, transitions and strengths.
p='tests/story/RuntimeStubs.cs';s=(root/p).read_text()
s=s.replace(' public enum Hash : ulong {', ' public enum Hash : ulong { GET_TIMECYCLE_MODIFIER_INDEX, GET_TIMECYCLE_TRANSITION_MODIFIER_INDEX,',1)
s=s.replace(' public static class Function {',''' public static class Function {
  public static int TimecycleIndex=-1, TimecycleTransition=-1;
  public static string TimecycleName;
  public static float TimecycleStrength;
  public static HashSet<string> RejectedTimecycles=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
  public static void ResetTimecycle(){TimecycleIndex=TimecycleTransition=-1;TimecycleName=null;TimecycleStrength=0;RejectedTimecycles.Clear();}
''',1)
old='  public static T Call<T>(Hash h,params object[] args){';assert s.count(old)==1
s=s.replace(old,old+'''
   if(h==Hash.GET_TIMECYCLE_MODIFIER_INDEX)return (T)(object)TimecycleIndex;
   if(h==Hash.GET_TIMECYCLE_TRANSITION_MODIFIER_INDEX)return (T)(object)TimecycleTransition;''',1)
# Match function signature from real stub.
idx=s.index('public static void Call(Hash h,params object[] args)')
brace=s.index('{',idx)
s=s[:brace+1]+'''
   if(h==Hash.SET_TIMECYCLE_MODIFIER&&!RejectedTimecycles.Contains((string)args[0])){TimecycleName=(string)args[0];TimecycleIndex=Game.GenerateHash(TimecycleName)&0x7fffffff;}
   if(h==Hash.SET_TIMECYCLE_MODIFIER_STRENGTH)TimecycleStrength=(float)args[0];
   if(h==Hash.CLEAR_TIMECYCLE_MODIFIER){TimecycleIndex=-1;TimecycleName=null;TimecycleStrength=0;}
'''+s[brace+1:]
write(p,s)
edit('tests/story/StoryTests.cs',' static void Reset(){',' static void Reset(){Function.ResetTimecycle();Game.IsPaused=false;')
edit('tools/run_story_tests.py',"'Core/VisualAtmosphere.cs'", "'Core/VisualAtmosphere.cs','Core/TimecycleGrade.cs'")
edit('tests/story/StoryTests.cs','root=args[1];','root=args[1];AtmosphereTransitionChecks();')

p='tests/story/VisualsDamageTests.cs';s=(root/p).read_text()
s=s.replace('  Reset();var visuals=new VisualAtmosphere(config);','  Reset();Function.Calls.Clear();var visuals=new VisualAtmosphere(config);',1)
s=s.replace('Check(visuals.ActiveModifier=="cinema_default"&&Math.Abs(visuals.ActiveStrength-.35f)<1e-4', 'Check(visuals.ActiveModifier=="cinema_default"&&visuals.ActiveStrength==0')
s=s.replace('At noon the daytime grade is set once at the configured strength','At noon the daytime grade is acquired once at neutral before its smooth fade')
s=s.replace('  Check(visuals.ActiveModifier==null,"Dusk', '  for(int i=0;i<10;i++){Game.GameTime+=100;visuals.Update(false,false);}\n  Check(visuals.ActiveModifier==null,"Dusk')
s=s.replace('  var sunny=new VisualAtmosphere', '  named.Reset();\n  var sunny=new VisualAtmosphere')
s=s.replace('  var capped=new VisualAtmosphere', '  sunny.Reset();\n  var capped=new VisualAtmosphere')
s=s.replace('  Function.Calls.Clear();visuals.Reset();','  capped.Reset();Function.Calls.Clear();visuals.Reset();')
s=s.replace('Check(Calls(Hash.CLEAR_TIMECYCLE_MODIFIER)==1&&', 'Check(Calls(Hash.CLEAR_TIMECYCLE_MODIFIER)==0&&')
s=s.replace('Reset puts back the grade, shadows, headlights, level of detail, ocean and both reflection flags','Reset restores owned shadows, headlights, level of detail, ocean and reflection flags without clearing an unowned grade')
write(p,s)
p='docs/VISUALS.md';s=(root/p).read_text()
s=s.replace('## Visual atmosphere (`[Visuals]`)','## Visual atmosphere (`[Visuals]`)\n\nThe current grading controller is described in `VISUAL-ATMOSPHERE-PASS.md`.\nIt checks the script timecycle index, fades through neutral between names, and\nreleases only observed ownership. The World menu exposes a session-only grading\ncomparison. This does not remove installed scenery or the other visual controls.\nPreset names below are historical candidates, NOT visually accepted profiles.\n')
s=s.replace('| None. |','| Measure on the target installation. |')
s=s.replace('have not been checked. Before judging the grading:', "have not been checked against Ron's current installed data. Runtime index acceptance\nis now checked, but that is not an artistic or collision test. Before judging the grading:")
write(p,s)
p='CLAUDE.md';s=(root/p).read_text()
s+='\n\n## Visual atmosphere comparison pass\n\nSee docs/VISUAL-ATMOSPHERE-PASS.md. TimecycleGrade acquires only an empty script\ngrade slot, checks the resulting index, fades through neutral between names and\nyields to observed foreign indices/transitions. Never clear an unowned grade or\nclaim index acceptance establishes visual quality. World menu grading A/B is\nsession-only; it does not change weather/time/water/LOD, maps, config or saves.\nPreserve the default ungraded dusk. Higher-priority suspension is immediate, not\na delayed fade. No-getter graphics settings still have compatibility limitations.\n'
write(p,s)
Path('build').mkdir(exist_ok=True)
Path('build/atmosphere-changes.json').write_text(json.dumps(list(EXPECTED)),encoding='utf-8')
print('Applied bounded atmosphere edits; compile and all behavior suites must pass before publication.')
