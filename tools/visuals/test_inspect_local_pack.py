"""Synthetic-fixture tests; not a test of a third-party archive or GTA."""
import importlib.util
import json
from pathlib import Path
import stat
import tempfile
import unittest
from unittest.mock import patch
import zipfile

SPEC = importlib.util.spec_from_file_location("inspect_pack", Path(__file__).with_name("inspect_local_pack.py"))
pack = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(pack)


class InventoryTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.repo = self.root / "repo"
        self.repo.mkdir()
        (self.repo / ".git").mkdir()
        self.work = self.root / "private"
        self.work.mkdir()

    def archive(self, members, name="pack.zip", **options):
        path = self.work / name
        with zipfile.ZipFile(path, "w", **options) as archive:
            for member, content in members:
                archive.writestr(member, content)
        return path

    def scan(self, source):
        return pack.inspect(source, "Test pack", "test-1", repo=self.repo)

    def test_inventory_hashes_without_installing(self):
        source = self.archive([("Readme.txt", "personal installation"), ("pack/dlc.rpf", b"not-a-real-rpf"), ("scripts/helper.cs", "// inspect me")])
        before = source.read_bytes()
        report = self.scan(source)
        self.assertEqual(3, report["file_count"])
        self.assertEqual(["Readme.txt"], report["notice_candidates"])
        self.assertEqual(["pack/dlc.rpf"], report["opaque_members"])
        self.assertEqual(["scripts/helper.cs"], report["code_review_required"])
        self.assertFalse(report["assessment"]["installed"])
        self.assertEqual("not_determined", report["assessment"]["license_permission"])
        self.assertEqual(before, source.read_bytes())
        self.assertEqual([source], list(self.work.iterdir()))

    def test_traversal_rejected(self):
        with self.assertRaises(pack.InspectionError):
            self.scan(self.archive([("../outside.txt", "x")]))

    def test_backslash_traversal_rejected(self):
        with self.assertRaises(pack.InspectionError):
            self.scan(self.archive([("a\\..\\outside.txt", "x")]))

    def test_absolute_rejected(self):
        with self.assertRaises(pack.InspectionError):
            self.scan(self.archive([("/outside.txt", "x")]))

    def test_windows_drive_rejected(self):
        with self.assertRaises(pack.InspectionError):
            self.scan(self.archive([("C:/outside.txt", "x")]))

    def test_windows_aliases_rejected(self):
        for name in ("CON.txt", "x/file. ", "a:b.txt", "x/./b", "x//b", "lpt1"):
            with self.subTest(name=name), self.assertRaises(pack.InspectionError):
                pack.safe_name(name)

    def test_case_collision_rejected(self):
        with self.assertRaises(pack.InspectionError):
            self.scan(self.archive([("Data/X.ymap", "a"), ("data/x.ymap", "b")]))

    def test_zip_symlink_rejected(self):
        info = zipfile.ZipInfo("link")
        info.create_system = 3
        info.external_attr = (stat.S_IFLNK | 0o777) << 16
        with self.assertRaises(pack.InspectionError):
            self.scan(self.archive([(info, "outside")]))

    def test_source_repo_rejected(self):
        source = self.repo / "thirdparty"
        source.mkdir()
        with self.assertRaises(pack.InspectionError):
            self.scan(source)

    def test_game_folder_rejected(self):
        (self.work / "GTA5_Enhanced.exe").write_text("fixture")
        with self.assertRaises(pack.InspectionError):
            self.scan(self.archive([("a.txt", "x")]))

    def test_folder_inventory(self):
        source = self.work / "extracted"
        source.mkdir()
        (source / "test.ymap.xml").write_text("<CMapData/>")
        report = self.scan(source)
        self.assertEqual("placement_xml_export", report["entries"][0]["category"])
        self.assertIsNone(report["archive_sha256"])

    def test_folder_symlink_rejected(self):
        source = self.work / "extracted"
        source.mkdir()
        target = self.work / "outside.txt"
        target.write_text("x")
        try:
            (source / "link.txt").symlink_to(target)
        except OSError:
            self.skipTest("OS denies symlink creation")
        with self.assertRaises(pack.InspectionError):
            self.scan(source)

    def test_file_size_limit(self):
        source = self.archive([("a.txt", "12345")])
        with patch.object(pack, "MAX_ENTRY_BYTES", 4), self.assertRaises(pack.InspectionError):
            self.scan(source)

    def test_total_size_limit(self):
        source = self.work / "folder"
        source.mkdir()
        (source / "a").write_text("123")
        (source / "b").write_text("456")
        with patch.object(pack, "MAX_TOTAL_BYTES", 5), self.assertRaises(pack.InspectionError):
            self.scan(source)

    def test_file_count_limit(self):
        source = self.archive([("a", "x"), ("b", "y")])
        with patch.object(pack, "MAX_FILES", 1), self.assertRaises(pack.InspectionError):
            self.scan(source)

    def test_empty_archive_rejected(self):
        with self.assertRaises(pack.InspectionError):
            self.scan(self.archive([]))

    def test_not_a_supported_archive(self):
        source = self.work / "pack.rar"
        source.write_bytes(b"RAR fixture")
        with self.assertRaises(pack.InspectionError):
            self.scan(source)

    def test_nested_oiv_not_treated_as_decoded(self):
        report = self.scan(self.archive([("install.oiv", "opaque"), ("readme.txt", "x")]))
        self.assertEqual(["install.oiv"], report["opaque_members"])
        self.assertEqual("not_decoded", report["assessment"]["rpf_geometry"])

    def test_report_exclusive_and_outside_repo(self):
        source = self.archive([("a", "x")])
        report = self.scan(source)
        output = self.work / "report.json"
        pack.write_report(report, output, source, repo=self.repo)
        self.assertEqual(1, json.loads(output.read_text())["file_count"])
        with self.assertRaises(FileExistsError):
            pack.write_report(report, output, source, repo=self.repo)
        with self.assertRaises(pack.InspectionError):
            pack.write_report(report, self.repo / "report.json", source, repo=self.repo)

    def test_report_never_written_inside_input(self):
        source = self.work / "folder"
        source.mkdir()
        (source / "file.txt").write_text("x")
        with self.assertRaises(pack.InspectionError):
            pack.write_report(self.scan(source), source / "report.json", source, repo=self.repo)

    def test_missing_report_directory_is_not_created(self):
        source = self.archive([("a", "x")])
        with self.assertRaises(pack.InspectionError):
            pack.write_report(self.scan(source), self.work / "absent" / "report.json", source, repo=self.repo)
        self.assertFalse((self.work / "absent").exists())


if __name__ == "__main__":
    unittest.main()
