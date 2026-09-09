"""Regression tests for speech boundaries in the real extracted design bibles."""
from pathlib import Path
import unittest
from parse_bible import parse_missions
ROOT=Path(__file__).resolve().parents[1]

class DialogueParserTests(unittest.TestCase):
    def test_bibles_and_final_cue(self):
        missions=[];cues=[]
        for source in ('omnibus_v2.txt','solo_missions_v1.txt'):
            m,c=parse_missions((ROOT/'docs/bibles'/source).read_text(encoding='utf-8').splitlines())
            missions+=m;cues+=c
        self.assertEqual((len(missions),len(cues)),(79,292))
        last=next(c for c in cues if c['cue_id']=='M70_S1_07_ICE')
        self.assertEqual(last['line'],"Let's go home.")
        self.assertEqual(last['trigger'],'Final cut to black: CAMPAIGN COMPLETE')
        self.assertTrue(all(len(c['line'])<400 for c in cues))
        self.assertFalse(any('TRACK 2' in c['line'] or 'API_KEY' in c['line'] for c in cues))

    def test_apostrophe_in_trigger_is_not_spoken(self):
        text="""M01: TEST
Act I • Terminal • 02:00 • Clear
STAGE MECHANICS
M01_S1_01_ICE
ICE
[Quiet] 'Don't move.
We're coming home.'
Player's car reaches the exit
TRACK 2: PRODUCTION
'Not speech'
"""
        _, cues=parse_missions(text.splitlines())
        self.assertEqual(cues[0]['line'],"Don't move. We're coming home.")
        self.assertEqual(cues[0]['trigger'],"Player's car reaches the exit")

    def test_section_boundary_before_next_mission(self):
        text="M01: ONE\nM01_S1_01_ICE\nICE\n'Home.'\nCar stops\n■ ACT II\nProduction editor's note\nM02: TWO\nM02_S1_01_GOHAN\nGOHAN\n'Go.'\nEngine starts"
        m,c=parse_missions(text.splitlines())
        self.assertEqual([r['id'] for r in m],['M01','M02'])
        self.assertEqual(c[0]['line'],'Home.')
        self.assertEqual(c[0]['trigger'],'Car stops')
        self.assertEqual(c[1]['line'],'Go.')

if __name__=='__main__':unittest.main()
