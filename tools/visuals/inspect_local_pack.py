"""Read-only inventory of a local mod ZIP/OIV or manually extracted folder.

Writes a NEW JSON report outside the repository and GTA installation. No download,
extraction, execution, installation, archive modification, or network access.
An inventory is not permission, malware screening, RPF decoding, or a clearance test.
Python 3.9+; standard library only.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import os
from pathlib import Path, PurePosixPath
import stat
import sys
from typing import Any, BinaryIO
import zipfile

MAX_FILES = 50000
MAX_ENTRY_BYTES = 1024 ** 3
MAX_TOTAL_BYTES = 4 * 1024 ** 3
MAX_RATIO = 500
CHUNK = 1024 * 1024
REPO = Path(__file__).resolve().parents[2]
GAME_EXES = ("GTA5.exe", "GTA5_Enhanced.exe", "GTAV_Enhanced.exe")
OPAQUE = {".rpf", ".oiv", ".zip", ".7z", ".rar"}
CODE = {".exe", ".dll", ".asi", ".cs", ".lua", ".py", ".ps1", ".bat", ".cmd", ".js", ".vbs", ".lnk", ".url", ".scr", ".com", ".msi"}
CATEGORIES = {
    ".ymap": "placements", ".ytyp": "archetypes", ".ybn": "collision",
    ".ydr": "drawables", ".ydd": "drawable_dictionaries", ".ytd": "textures",
    ".ymt": "metadata", ".ynd": "paths", ".ycd": "animations",
    ".xml": "xml_requires_review", ".rpf": "opaque_game_archive",
}
WINDOWS_NAMES = {"CON", "PRN", "AUX", "NUL"} | {
    prefix + str(i) for prefix in ("COM", "LPT") for i in range(1, 10)
}


class InspectionError(ValueError):
    """Input or output is unsuitable for this bounded inventory."""


def within(path: Path, root: Path) -> bool:
    try:
        path.relative_to(root)
        return True
    except ValueError:
        return False


def private_location(path: Path, repo: Path) -> None:
    path = path.resolve()
    if within(path, repo.resolve()):
        raise InspectionError("Keep local packs and reports OUTSIDE the source repository.")
    for parent in (path, *path.parents):
        if (parent / ".git").exists():
            raise InspectionError("Local pack/report is inside a Git working tree.")
        if any((parent / name).exists() for name in GAME_EXES):
            raise InspectionError("Use a separate visual workspace, not the live GTA installation.")


def safe_name(raw: str) -> str:
    # Case-insensitive duplicate detection is performed separately for Windows.
    name = raw.replace("\\", "/")
    parts = name.rstrip("/").split("/")
    if not name or name.startswith("/") or any(
        p in ("", ".", "..") or p.endswith((" ", "."))
        or any(c in '<>:"|?*' or ord(c) < 32 for c in p)
        or p.split(".")[0].upper() in WINDOWS_NAMES for p in parts
    ):
        raise InspectionError("Unsafe or Windows-incompatible member name: " + repr(raw))
    return str(PurePosixPath(*parts))


def hash_stream(stream: BinaryIO, limit: int) -> tuple[str, int]:
    digest = hashlib.sha256()
    total = 0
    while True:
        block = stream.read(CHUNK)
        if not block:
            break
        total += len(block)
        if total > limit:
            raise InspectionError("Stream exceeds the configured byte limit.")
        digest.update(block)
    return digest.hexdigest(), total


def entry_record(name: str, size: int, digest: str) -> dict[str, Any]:
    low = name.lower()
    suffix = PurePosixPath(low).suffix
    # .ymap.xml is a placement export, not decoded by this inventory.
    category = CATEGORIES.get(suffix, "other")
    if low.endswith(".ymap.xml"):
        category = "placement_xml_export"
    notice = any(word in PurePosixPath(low).name for word in ("license", "licence", "readme", "credit", "permission", "terms", "install"))
    return {"path": name, "bytes": size, "sha256": digest,
            "category": category, "notice_candidate": notice,
            "code_requires_review": suffix in CODE,
            "contents_not_decoded": suffix in OPAQUE}


def inspect(source: Path, name: str, version: str, *, repo: Path = REPO) -> dict[str, Any]:
    source = source.expanduser().resolve(strict=True)
    private_location(source, repo)
    entries: list[dict[str, Any]] = []
    names: set[str] = set()
    total = 0

    def reserve(raw: str, size: int) -> str:
        nonlocal total
        normalized = safe_name(raw)
        key = normalized.casefold()
        if key in names:
            raise InspectionError("Duplicate/case-colliding member: " + normalized)
        if len(entries) >= MAX_FILES or size < 0 or size > MAX_ENTRY_BYTES:
            raise InspectionError("File count or individual file-size limit exceeded.")
        total += size
        if total > MAX_TOTAL_BYTES:
            raise InspectionError("Total uncompressed size limit exceeded.")
        names.add(key)
        return normalized

    archive_digest = None
    archive_bytes = None
    if source.is_file():
        if not zipfile.is_zipfile(source):
            raise InspectionError("Supported inputs: ZIP/OIV or an extracted folder. RAR/7z/RPF are not decoded.")
        with source.open("rb") as handle:
            archive_digest, archive_bytes = hash_stream(handle, MAX_TOTAL_BYTES)
        with zipfile.ZipFile(source) as archive:
            infos = archive.infolist()
            if len(infos) > MAX_FILES:
                raise InspectionError("Archive entry limit exceeded.")
            for info in infos:
                safe_name(info.filename)
                mode = (info.external_attr >> 16) & 0xFFFF
                if stat.S_ISLNK(mode) or (stat.S_IFMT(mode) not in (0, stat.S_IFREG, stat.S_IFDIR)):
                    raise InspectionError("Archive links and special files are not accepted.")
                if info.is_dir():
                    continue
                if info.flag_bits & 1:
                    raise InspectionError("Encrypted archive members require manual review.")
                path = reserve(info.filename, info.file_size)
                if info.file_size > 1024 * 1024 and info.file_size / max(1, info.compress_size) > MAX_RATIO:
                    raise InspectionError("Excessive compression ratio; inspect manually.")
                with archive.open(info, "r") as stream:
                    digest, actual = hash_stream(stream, min(MAX_ENTRY_BYTES, info.file_size))
                if actual != info.file_size:
                    raise InspectionError("Member size differs from its declaration.")
                entries.append(entry_record(path, actual, digest))
    elif source.is_dir():
        def walk_error(error: OSError) -> None:
            raise error
        for root, dirs, files in os.walk(source, followlinks=False, onerror=walk_error):
            for dirname in dirs:
                if (Path(root) / dirname).is_symlink():
                    raise InspectionError("Symlink directories are not accepted.")
            for filename in files:
                path = Path(root) / filename
                if path.is_symlink() or not stat.S_ISREG(path.lstat().st_mode):
                    raise InspectionError("Symlinks and special files are not accepted.")
                if not within(path.resolve(), source):
                    raise InspectionError("A resolved file escapes the selected folder.")
                relative = reserve(path.relative_to(source).as_posix(), path.stat().st_size)
                with path.open("rb") as stream:
                    digest, size = hash_stream(stream, min(MAX_ENTRY_BYTES, path.stat().st_size))
                entries.append(entry_record(relative, size, digest))
    else:
        raise InspectionError("Input is not a regular file or folder.")
    if not entries:
        raise InspectionError("No files found; no useful inventory can be produced.")
    entries.sort(key=lambda item: item["path"].casefold())
    return {
        "schema_version": 1, "pack_name_user_supplied": name,
        "pack_version_user_supplied": version, "input_name": source.name,
        "input_kind": "directory" if source.is_dir() else "zip_or_oiv",
        "archive_sha256": archive_digest, "archive_bytes": archive_bytes,
        "file_count": len(entries), "total_member_bytes": total,
        "assessment": {"inventory_only": True, "game_compatibility": "not_assessed",
                       "license_permission": "not_determined", "malware_scan": "not_performed",
                       "rpf_geometry": "not_decoded", "redistributable": False,
                       "installed": False, "live_verified": False},
        "notice_candidates": [e["path"] for e in entries if e["notice_candidate"]],
        "code_review_required": [e["path"] for e in entries if e["code_requires_review"]],
        "opaque_members": [e["path"] for e in entries if e["contents_not_decoded"]],
        "entries": entries,
    }


def write_report(report: dict[str, Any], destination: Path, source: Path, *, repo: Path = REPO) -> None:
    destination = destination.expanduser().resolve()
    source = source.expanduser().resolve()
    private_location(destination, repo)
    if destination == source or (source.is_dir() and within(destination, source)):
        raise InspectionError("Report must not overwrite or be written into the input pack.")
    if not destination.parent.is_dir():
        raise InspectionError("Create the external reports folder before running the tool.")
    # Exclusive creation: never replace a save, configuration, or prior report.
    with destination.open("x", encoding="utf-8", newline="\n") as handle:
        json.dump(report, handle, ensure_ascii=True, indent=2)
        handle.write("\n")


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input", type=Path, required=True, help="Local ZIP/OIV or extracted directory")
    parser.add_argument("--name", required=True, help="Human-supplied pack name, not verified automatically")
    parser.add_argument("--version", required=True, help="Version from the original distribution")
    parser.add_argument("--report", type=Path, required=True, help="NEW JSON file in an external reports folder")
    args = parser.parse_args(argv)
    try:
        report = inspect(args.input, args.name, args.version)
        write_report(report, args.report, args.input)
    except (InspectionError, OSError, zipfile.BadZipFile, RuntimeError, NotImplementedError) as exc:
        print("Inventory stopped: " + str(exc), file=sys.stderr)
        return 2
    print(f"Inventoried {report['file_count']} files. Input unchanged; nothing installed.")
    print("Read the listed notices and code files manually. Opaque archives still need inspection.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
