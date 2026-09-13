"""Stage the reviewed Forests 6.0-SP Enhanced files in a NEW private folder.

Never writes into GTA or Git, edits an RPF, runs an OIV, downloads dependencies,
changes Bloodlines, or copies assets to CI. Optional dlclist input must be an
exported UTF-8 XML file; only a new candidate is produced for manual review/import.
The reviewed pack's checksum is pinned: a different release needs fresh inspection.
"""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import shutil
import sys
import tempfile
import xml.etree.ElementTree as ET
import zipfile

from inspect_local_pack import InspectionError, inspect, private_location, within

REPO = Path(__file__).resolve().parents[2]
ARCHIVE_SHA256 = "b8866ca90b2c3d9c33dc1086e91f1499897068613574d78d6286db4730edab69"
PACKS = {
    "north": ("forest_n", 25811456, "52fea1336d5f683465022cabcfc7d11ffe291ec49d06e883059a9b0f9ab09db0"),
    "south": ("forest_s", 39506944, "1166e91efca4a5efa1211e8dc9e6aaf1cb2e252b4dee552111d4f8552d9da7eb"),
}
NOTICES = {
    "GTA V Enhanced/installation.txt": "Enhanced-installation.txt",
    "information.txt": "information.txt", "changelog.txt": "changelog.txt",
}
MAX_XML = 4 * 1024 * 1024


def selected(parts: str) -> list[str]:
    if parts not in ("north", "south", "both"):
        raise InspectionError("Select north, south or both.")
    return ["north", "south"] if parts == "both" else [parts]


def no_links(path: Path) -> None:
    # Resolve only AFTER checking lexical parents, so junctions cannot disguise a
    # destination inside the live game, input tree or repository.
    p = path.expanduser().absolute()
    for candidate in (p, *p.parents):
        if candidate.is_symlink() or (hasattr(candidate, "is_junction") and candidate.is_junction()):
            raise InspectionError("Links/junctions are not accepted in workspace paths.")


def new_destination(path: Path, *, repo: Path = REPO) -> Path:
    no_links(path)
    p = path.expanduser().resolve()
    private_location(p, repo)
    if p.exists():
        raise InspectionError("Output already exists; choose a NEW path.")
    if not p.parent.is_dir():
        raise InspectionError("Create the external output parent directory first.")
    return p


def _xml_items(raw: bytes) -> tuple[str, list[str]]:
    if len(raw) > MAX_XML:
        raise InspectionError("Exported dlclist is too large.")
    try:
        text = raw.decode("utf-8-sig")
    except UnicodeError as exc:
        raise InspectionError("Export dlclist.xml as UTF-8; no implicit encoding conversion.") from exc
    if re.search(r"<!\s*(DOCTYPE|ENTITY)\b", text, re.I):
        raise InspectionError("DTD/entity declarations are not accepted.")
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        raise InspectionError("Invalid exported XML: " + str(exc)) from exc
    paths = list(root.iter("Paths"))
    if len(paths) != 1 or len(re.findall(r"</Paths\s*>", text)) != 1:
        raise InspectionError("Expected exactly one non-self-closing, unnamespaced Paths element.")
    entries = []
    for child in paths[0]:
        if child.tag != "Item" or list(child) or child.attrib:
            raise InspectionError("Unsupported Paths child; review manually.")
        entries.append((child.text or "").strip())
    return text, entries


def add_dlc_entries(raw: bytes, parts: str) -> tuple[bytes, list[str]]:
    """Add only missing requested entries, preserving the original bytes otherwise.

    This is NOT a profile switch: requesting north never removes an existing south.
    Existing entries and comments are never deleted, reformatted or reordered.
    """
    sides = selected(parts)
    text, entries = _xml_items(raw)
    canon = lambda value: value.strip().lower().replace("\\", "/").rstrip("/")
    existing = [canon(value) for value in entries]
    additions = []
    for side in sides:
        name = PACKS[side][0]
        item = f"dlcpacks:/{name}/"
        hits = existing.count(canon(item))
        if hits > 1:
            raise InspectionError("Duplicate existing " + name + " registrations; review manually.")
        if not hits:
            additions.append(item)
    if not additions:
        return raw, []
    ending = "\r\n" if "\r\n" in text else "\n"
    close = re.search(r"</Paths\s*>", text)
    assert close is not None
    line = text.rfind("\n", 0, close.start()) + 1
    indent = text[line:close.start()]
    if not indent.strip():
        offset = line
        insertion = "".join(indent + "\t<Item>" + item + "</Item>" + ending for item in additions)
    else:
        offset = close.start()
        insertion = "".join("<Item>" + item + "</Item>" for item in additions)
    updated = text[:offset] + insertion + text[offset:]
    _, new_entries = _xml_items(updated.encode("utf-8"))
    if new_entries != entries + additions:
        raise InspectionError("Unexpected XML change; no candidate will be written.")
    bom = b"\xef\xbb\xbf" if raw.startswith(b"\xef\xbb\xbf") else b""
    return bom + updated.encode("utf-8"), additions


def stage(source: Path, output: Path, parts: str, *, repo: Path = REPO) -> dict:
    sides = selected(parts)
    no_links(source)
    source = source.expanduser().resolve(strict=True)
    if not source.is_file():
        raise InspectionError("Input must be the original reviewed ZIP, not an extracted folder.")
    output = new_destination(output, repo=repo)
    inventory = inspect(source, "Forests of San Andreas: Revised", "6.0-SP", repo=repo)
    if inventory["archive_sha256"] != ARCHIVE_SHA256:
        raise InspectionError("Archive differs from the inspected upload; re-inspect before staging.")
    # Work only under a newly allocated external scratch directory. A failed copy
    # removes that new scratch tree, never a preexisting installation or backup.
    scratch = Path(tempfile.mkdtemp(prefix=".forests-stage-", dir=output.parent))
    try:
        files = []
        with zipfile.ZipFile(source) as archive:
            for side in sides:
                name, size, digest = PACKS[side]
                member = f"GTA V Enhanced/{name}/dlc.rpf"
                relative = f"mods/update/x64/dlcpacks/{name}/dlc.rpf"
                dest = scratch / relative
                dest.parent.mkdir(parents=True, exist_ok=True)
                actual = hashlib.sha256()
                count = 0
                with archive.open(member) as src, dest.open("xb") as target:
                    while True:
                        chunk = src.read(1024 * 1024)
                        if not chunk:
                            break
                        count += len(chunk)
                        if count > size:
                            raise InspectionError("Selected pack exceeds reviewed size.")
                        actual.update(chunk)
                        target.write(chunk)
                if count != size or actual.hexdigest() != digest:
                    raise InspectionError("Selected pack differs from its reviewed bytes.")
                files.append({"source_member": member, "staged_path": relative,
                              "bytes": count, "sha256": digest})
            notices = scratch / "creator-notices"
            notices.mkdir()
            for member, filename in NOTICES.items():
                if archive.getinfo(member).file_size > 512 * 1024:
                    raise InspectionError("Unexpected notice size.")
                (notices / filename).write_bytes(archive.read(member))
        report = {"schema_version": 1, "pack": "Forests of San Andreas: Revised 6.0-SP",
                  "parts": parts, "source_sha256": ARCHIVE_SHA256, "files": files,
                  "staged_only": True, "installed": False, "live_verified": False,
                  "redistribution_permission": "not_established",
                  "rpf_payloads_modified": False, "mission_files_modified": False}
        (scratch / "STAGE-MANIFEST.json").write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
        entries = "\n".join("<Item>dlcpacks:/" + PACKS[side][0] + "/</Item>" for side in sides)
        (scratch / "ADD-TO-DLCLIST.txt").write_text(entries + "\n", encoding="utf-8")
        (scratch / "READ-ME-FIRST.txt").write_text(
            "PRIVATE LOCAL STAGING ONLY - NOTHING INSTALLED\n\n"
            "Use GTA V Enhanced Story Mode. Read creator-notices/Enhanced-installation.txt.\n"
            "Do not run the Legacy OIVs or install Legacy shaders/scripts.\n"
            "Verify a compatible Enhanced mods-folder loader; do not stack loaders blindly.\n"
            "Close GTA. Back up the current mods/update/update.rpf and existing forest folders outside GTA/Git.\n"
            "Copy only the reviewed forest_n and/or forest_s folders into the game's mods/update/x64/dlcpacks.\n"
            "If mods/update/update.rpf already exists, keep it; NEVER replace it with a stock or supplied archive.\n"
            "If absent, copy your own current update/update.rpf there using the author's instructions.\n"
            "Export common/data/dlclist.xml from that MODS archive with CodeWalker. Add only missing entries\n"
            "from ADD-TO-DLCLIST.txt inside Paths, or use this helper's dlclist subcommand on the export.\n"
            "Review and manually import the result back into the MODS archive. A loose XML alongside the RPF is not enough.\n"
            "Preserve all other DLC entries. Leave scripts/Bloodlines.dll, mission data, saves and surveys unchanged.\n"
            "The author warns of vegetation reflection rectangles unless RT reflections are Very High or Ultra.\n"
            "This is not a benchmark or a guarantee for your current game build.\n"
            "Rollback: remove only entries/folders newly added for this test; keep previously installed components.\n"
            "Restore the whole mods/update/update.rpf backup only when no later unrelated edits would be lost.\n"
            "Keep these assets and derived local files outside all Git trees and release/CI packaging.\n",
            encoding="utf-8")
        if output.exists():
            raise InspectionError("Output appeared during staging; refusing to replace it.")
        scratch.rename(output)
        return report
    finally:
        if scratch.exists():
            shutil.rmtree(scratch)


def dlclist(source: Path, output: Path, parts: str, *, repo: Path = REPO) -> dict:
    no_links(source)
    source = source.expanduser().resolve(strict=True)
    private_location(source, repo)
    output = new_destination(output, repo=repo)
    if source.stat().st_size > MAX_XML:
        raise InspectionError("Exported XML exceeds the size limit.")
    original = source.read_bytes()
    updated, additions = add_dlc_entries(original, parts)
    with output.open("xb") as handle:
        handle.write(updated)
    return {"added": additions, "source_sha256": hashlib.sha256(original).hexdigest(),
            "candidate_sha256": hashlib.sha256(updated).hexdigest(), "installed": False}


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    for name in ("stage", "dlclist"):
        command = commands.add_parser(name)
        command.add_argument("--input", type=Path, required=True)
        command.add_argument("--output", type=Path, required=True)
        command.add_argument("--parts", choices=("north", "south", "both"), required=True)
    args = parser.parse_args(argv)
    try:
        report = stage(args.input, args.output, args.parts) if args.command == "stage" else dlclist(args.input, args.output, args.parts)
    except (InspectionError, OSError, zipfile.BadZipFile, RuntimeError, NotImplementedError, KeyError) as exc:
        print("Preparation stopped: " + str(exc), file=sys.stderr)
        return 2
    print(json.dumps(report, indent=2))
    print("New private output prepared. Nothing installed; original inputs unchanged.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
