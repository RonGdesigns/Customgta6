# Harbor integration - both agents preserved

Adopted into main on September 11, 2026 (PR #34), together with the seamless joins in `CONTINUOUS-PORT-HEIST.md`. The branch's JSON preservation and binary manifests and its Actions workflow were review evidence for the branch itself and were not carried; the DLL on main is rebuilt from the merged source with the repo's own toolchain.

Main input: `f8a37b9837b11f6661df081fd59616b7c06e6bb3` (through merged PR #32).

Repair input: `ea4c54667d60d10f8973bfeccdd9713c97499501`. Common base: `d295306764fa137ceda3e1f3741e2a0d766bdb45`.

Integration input: `65223c513a390dea21a3107d53aa084b42656b82`; Actions run `34650235039`.

## Preservation and resolution

All 14 paths changed only on main remain byte-identical to main. This includes complete M03, M04, M05, M06, M07 and M08 mission classes; CrewVan, GameUtils and MissionSites; and exclusive tests, QA and handoff entries. No source edits were made to those mission implementations.

All 31 paths changed only by the repair were checked against the repair input before metadata was updated. They retain marine fixes, the one-sitting rule, police sighting, banner/music/map work and tests. The manifest identifies metadata-only differences explicitly.

The only source conflict was RuntimeStubs.cs: native names and ground-placement helpers from main are combined with police/music/geometry definitions from the repair. BloodlinesMain keeps the grounded-spawn tick AND presentation. CampaignFlowTests keeps the driving fixture AND simulated sub-surfacing check. The two changed M06 location rows and both new harbor keys are retained. The mission map is regenerated.

The location audit is regenerated for 191 combined rows instead of its historical 189; it checks district, not physical reachability. Campaign and feasibility documents were fully compared after regeneration with only newline conversion allowed, then restored to committed bytes. No freshness assertion was removed. The eight-column location TSV retains empty final fields; exact-hash and per-field schema checks supplement strict source whitespace checks.

## Binary comparison

Both original DLLs and the combined DLL were inspected with PEReader, not executed as plugins. Assembly references, type/method inventories, IL instructions, local signatures and exception regions were read. Token operands were resolved to symbolic names so token reordering is not confused with code changes. HARBOR-INTEGRATION-BINARIES.json records input hashes and checked groups. The 386 compiled methods (including generated helpers) of M03-M08 match main. Selected main placement/forklift helpers and repair marine/police/presentation/operation classes match their own input DLLs. This is preservation evidence, not live GTA validation.

The DLL conflict is resolved by a fresh combined build, never ours/theirs. SHA-256: `75caf923fcb46d116c8ee840f7e8cc132c7d28465a5ce419ef58a2c86b819b72`.

## Actual verification

- Production warnings-as-errors compilation passed.
- 1721 story/runtime checks passed, retaining both suites.
- 165 behavioral regression checks passed.
- 3 parser tests, mission lint, location district checks, authored-scene and mission-map freshness, campaign/feasibility regeneration passed.
- Roslyn rebuild and clean packaging passed; fresh/prebuilt/packaged bytes agree.
- Exactly two GTA.Script entrypoints remain.

## Scope and live acceptance

Main was brought INTO the separate repair branch; the main branch itself was not changed. PR #28 remains draft. No game installation, personal INI, survey, save or runtime dependency changed. Workflow metadata updates and temporary-runner removal use separate authorized repository operations; this job never modifies workflows or expands its token permissions.

Failure, abort or quit still restarts the entire Port Heist at M19; no checkpoints return. Test the real water worksite, launch, every reachable work sphere, cargo attachment, boat/road transfers, final deposit and single result. Test police-helicopter sight and roof/tunnel occlusion, score audibility/cleanup, map glyphs, apartment rendering, and the preserved M03/M05/M06/M07/M08 behaviors. These tests do not simulate GTA physics, streaming or pathfinding.

Preserving source is not certifying every inherited design decision. This integration does not rewrite M04, expand weapon policy or silently alter other missions. Follow-up corrections should be independently reviewed.
