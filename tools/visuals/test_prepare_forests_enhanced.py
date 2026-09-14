"""Synthetic-fixture safety tests. No game or third-party binary is used in CI."""
from pathlib import Path
import hashlib
import tempfile
import unittest
from unittest.mock import patch
import zipfile

import prepare_forests_enhanced as m


class ForestsPreparationTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.root = Path(self.tmp.name)
        self.repo = self.root / "repo"
        self.repo.mkdir()
        (self.repo / ".git").mkdir()
        self.source = self.root / "fixture.zip"
        self.parts = {"north": b"synthetic north", "south": b"synthetic south"}
        with zipfile.ZipFile(self.source, "w") as z:
            for side, payload in self.parts.items():
                name = "forest_n" if side == "north" else "forest_s"
                z.writestr(f"GTA V Enhanced/{name}/dlc.rpf", payload)
            for name in m.NOTICES:
                z.writestr(name, "synthetic notice")
            z.writestr("GTA V Legacy/wrong.oiv", "do not stage")
            z.writestr("GTA V Legacy/FoSAShelter.3.cs", "do not run")
        self.pins = {side: ("forest_n" if side == "north" else "forest_s", len(payload), hashlib.sha256(payload).hexdigest()) for side, payload in self.parts.items()}
        self.hash = hashlib.sha256(self.source.read_bytes()).hexdigest()

    def tearDown(self):
        self.tmp.cleanup()

    def do_stage(self, parts="both", output=None):
        with patch.object(m, "ARCHIVE_SHA256", self.hash), patch.object(m, "PACKS", self.pins):
            return m.stage(self.source, output or self.root / "out", parts, repo=self.repo)

    def test_both_exact_files_no_legacy(self):
        source_before = self.source.read_bytes()
        report = self.do_stage()
        for side, payload in self.parts.items():
            name = self.pins[side][0]
            self.assertEqual((self.root / f"out/mods/update/x64/dlcpacks/{name}/dlc.rpf").read_bytes(), payload)
        self.assertFalse(any(p.suffix in (".oiv", ".cs", ".dll") for p in (self.root / "out").rglob("*")))
        self.assertFalse((self.root / "out/mods/update/update.rpf").exists())
        self.assertEqual(self.source.read_bytes(), source_before)
        self.assertFalse(report["installed"])

    def test_north_only(self):
        self.do_stage("north")
        self.assertTrue((self.root / "out/mods/update/x64/dlcpacks/forest_n").exists())
        self.assertFalse((self.root / "out/mods/update/x64/dlcpacks/forest_s").exists())

    def test_south_only(self):
        self.do_stage("south")
        self.assertFalse((self.root / "out/mods/update/x64/dlcpacks/forest_n").exists())
        self.assertTrue((self.root / "out/mods/update/x64/dlcpacks/forest_s").exists())

    def test_pins_reject_different_release(self):
        with self.assertRaises(m.InspectionError):
            m.stage(self.source, self.root / "out", "both", repo=self.repo)
        self.assertFalse((self.root / "out").exists())

    def test_member_failure_cleans_only_own_scratch(self):
        self.pins["south"] = ("forest_s", 10, "0" * 64)
        sentinel = self.root / "preserve.txt"
        sentinel.write_text("preserve")
        with self.assertRaises(m.InspectionError):
            self.do_stage()
        self.assertFalse((self.root / "out").exists())
        self.assertFalse(list(self.root.glob(".forests-stage-*")))
        self.assertEqual(sentinel.read_text(), "preserve")

    def test_existing_output_refused(self):
        out = self.root / "out"
        out.mkdir()
        (out / "savegame.json").write_text("preserve")
        with self.assertRaises(m.InspectionError):
            self.do_stage()
        self.assertEqual((out / "savegame.json").read_text(), "preserve")

    def test_git_output_refused(self):
        with self.assertRaises(m.InspectionError):
            self.do_stage(output=self.repo / "out")

    def test_live_gta_output_refused(self):
        game = self.root / "game"
        game.mkdir()
        (game / "GTA5_Enhanced.exe").write_text("fixture")
        with self.assertRaises(m.InspectionError):
            self.do_stage(output=game / "out")

    def test_missing_parent_refused(self):
        with self.assertRaises(m.InspectionError):
            self.do_stage(output=self.root / "missing" / "out")

    def test_directory_input_refused(self):
        with self.assertRaises(m.InspectionError):
            m.stage(self.root, self.root / "out", "both", repo=self.repo)

    def test_invalid_part_refused(self):
        with self.assertRaises(m.InspectionError):
            self.do_stage("legacy")

    def test_additive_xml_preserves_original_bytes(self):
        raw = b'\xef\xbb\xbf<?xml version="1.0"?>\r\n<SMandatoryPacksData>\r\n\t<Paths>\r\n\t\t<!-- my other mod -->\r\n\t\t<Item>dlcpacks:/mycar/</Item>\r\n\t</Paths>\r\n</SMandatoryPacksData>\r\n'
        result, added = m.add_dlc_entries(raw, "both")
        self.assertEqual(len(added), 2)
        for item in added:
            result = result.replace(("\t\t<Item>" + item + "</Item>\r\n").encode(), b"", 1)
        self.assertEqual(result, raw)

    def test_xml_idempotence(self):
        raw = b'<Root><Paths><Item>dlcpacks:/other/</Item></Paths></Root>'
        first, _ = m.add_dlc_entries(raw, "both")
        again, added = m.add_dlc_entries(first, "both")
        self.assertEqual(first, again)
        self.assertEqual(added, [])

    def test_xml_normalizes_existing_entry_for_duplicate_check(self):
        raw = b'<Root><Paths><Item> DLCPACKS:\\FOREST_N\\ </Item></Paths></Root>'
        out, added = m.add_dlc_entries(raw, "north")
        self.assertEqual(out, raw)
        self.assertEqual(added, [])

    def test_north_is_not_removal_of_south(self):
        raw = b'<Root><Paths><Item>dlcpacks:/forest_s/</Item><Item>dlcpacks:/other/</Item></Paths></Root>'
        out, added = m.add_dlc_entries(raw, "north")
        self.assertIn(b'<Item>dlcpacks:/forest_s/</Item>', out)
        self.assertEqual(added, ["dlcpacks:/forest_n/"])

    def test_xml_duplicate_registration_refused(self):
        raw = b'<Root><Paths><Item>dlcpacks:/forest_n/</Item><Item>dlcpacks:/forest_n/</Item></Paths></Root>'
        with self.assertRaises(m.InspectionError):
            m.add_dlc_entries(raw, "north")

    def test_xml_entity_or_doctype_refused(self):
        with self.assertRaises(m.InspectionError):
            m.add_dlc_entries(b'<!DOCTYPE Root><Root><Paths></Paths></Root>', "both")

    def test_xml_invalid_layout_refused(self):
        for raw in (b'<Root><Paths/><Paths/></Root>', b'not xml', b'<Root><Paths/></Root>', b'<Root><Paths><Different/></Paths></Root>', b'<Root><Paths><Item a="1">test</Item></Paths></Root>'):
            with self.subTest(raw=raw), self.assertRaises(m.InspectionError):
                m.add_dlc_entries(raw, "both")

    def test_xml_size_bound(self):
        with patch.object(m, "MAX_XML", 4), self.assertRaises(m.InspectionError):
            m.add_dlc_entries(b'<Root><Paths></Paths></Root>', "both")

    def test_xml_candidate_not_inplace(self):
        original = self.root / "dlclist.xml"
        raw = b'<Root><Paths><Item>dlcpacks:/mine/</Item></Paths></Root>'
        original.write_bytes(raw)
        result = m.dlclist(original, self.root / "candidate.xml", "both", repo=self.repo)
        self.assertEqual(original.read_bytes(), raw)
        self.assertFalse(result["installed"])
        with self.assertRaises(m.InspectionError):
            m.dlclist(original, original, "both", repo=self.repo)

    def test_xml_output_inside_game_refused(self):
        original = self.root / "dlclist.xml"
        original.write_bytes(b'<Root><Paths></Paths></Root>')
        game = self.root / "game"
        game.mkdir()
        (game / "GTA5_Enhanced.exe").write_text("fixture")
        with self.assertRaises(m.InspectionError):
            m.dlclist(original, game / "dlclist.xml", "both", repo=self.repo)

    def test_linked_output_refused(self):
        real = self.root / "real"
        real.mkdir()
        linked = self.root / "linked"
        try:
            linked.symlink_to(real, target_is_directory=True)
        except OSError:
            self.skipTest("Symlink privilege unavailable on this OS")
        with self.assertRaises(m.InspectionError):
            self.do_stage(output=linked / "out")


if __name__ == "__main__":
    unittest.main()
