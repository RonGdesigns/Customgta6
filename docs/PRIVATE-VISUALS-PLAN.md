# Bloodlines private visuals: plan and intake starter

Status: planning and read-only intake tools only. No graphics assets installed, no C# changes, no new DLL, no mission modifications, and no local GTA/CodeWalker measurement performed in this pass.

Baseline reviewed: `6867c858d45e0a7d11369d33ac117f3f044a9b8a` on `claude/gta-v-custom-version-477edi`. This baseline already includes the PR45 solo/desert repair merge. Proposed separate work branch: `codex/private-visuals-foundation`.

## Goal and confirmed constraints

Build a richer, coherent visual presentation for Ron's own GTA V Enhanced Story Mode installation. Reuse suitable creator-distributed packs instead of rebuilding every asset. RTX 5080 is confirmed; the CPU, memory, screen resolution and FPS target are not confirmed by the pasted reference-video hardware list. No DLSS 5 dependency. Do not treat a frame-generation counter as evidence that the underlying simulation is responsive.

Keep the existing campaign and all mission repairs intact. Preserve Ron's home, prologue, current placement overrides, character relationships, rewards, working helicopter controls, and the one-sitting/no-checkpoint Port Heist. Do not relocate missions to accommodate scenery. Retain the existing VisualAtmosphere controller instead of introducing a second script that competes for grading or environmental state.

The working repository is public. A branch in it is also public. Local downloaded packs, unpacked third-party content, modified copies, game archives, decoded geometry, personal configuration and benchmark reports belong OUTSIDE every Git working tree and release directory. Not charging money does not by itself establish reuse or redistribution permission. This plan does not label unresolved rights as cleared. Preserve original README/license/credits; assess any actual restrictions found. Use legitimate creator downloads, with no paywall or access-control bypass. Keep unknown-rights binaries and derivatives out of uploads, public CI and releases. Reassess before sharing any combined package, even free or with a friend.

## Evidence, rather than assumptions

- Current source: `src/Bloodlines/Core/VisualAtmosphere.cs`, `tools/package.py`, `CLAUDE.md`, and `docs/COORDINATE-REFERENCE-WORKFLOW.md` at the baseline above. The atmosphere code controls existing native features; it is not a replacement renderer. The package script copies `assets/bloodlines_assets`, so that directory is NOT a private import folder.
- Forests of San Andreas: Revised author page: <https://www.gta5-mods.com/maps/forests-of-san-andreas-revised>. Checked September 13, 2026. Lists 6.0-SP with Enhanced Story Mode support. Its changelog describes coordinated placements, collision, distant models and reflections. Exact archive contents and included terms remain uninspected: the retrieval returned the release page, not the ZIP.
- US Copyright Office FAQ: <https://www.copyright.gov/help/faq/faq-fairuse.html>. Noncommercial intent is not a blanket permission rule.
- Existing coordinate tools distinguish candidate transforms, checked collision and live survey. Reuse the local CodeWalker workflow; do not pretend repository-side tests queried the installed map.

The rest of this document is a proposed implementation and acceptance plan, not a claim that these additions are already working.

## Workspace separation

Example local layout; choose a drive with suitable free space:

```text
D:\BloodlinesVisualLab\
    originals\       original downloaded ZIP/OIV files; retain checksums
    unpacked\        manually inspected local copies; preserve originals
    exports\         local CodeWalker exports and geometry evidence
    work\            private edited copies and generated assets
    reports\         intake, conflict, and performance reports
    backups\         owned installation files and rollback manifests
```

The public repository contains only our tooling, plan, tests and non-sensitive templates. It must not ingest any of the workspace as a Git subtree, symlink, CI artifact or package input. `.gitignore` alone is not a guarantee of privacy. Do not include the local workspace in the normal source ZIP or `build/deploy`.

## V0: establish the comparison and inspect the pack

Use the same installed Bloodlines DLL/data for all first-round visual comparisons. Record their hashes, GTA edition/build, script-hook version, other active mods and graphics settings. Back up the complete Bloodlines folder and DLL outside `scripts`. Use copied campaign saves. Do not simultaneously test a mission-repair DLL and a new graphics preset.

Acquire the creator's Enhanced-compatible single-player release. Do not use a FiveM bundle or assume an older Legacy installer has the same paths. Inspect its actual README, installer instructions, code files and dependencies before execution. Do not automatically install generic gameconfig/heap/shader files from an old guide. If a paid edition is selected later, obtain it legitimately.

Run the read-only tool delivered with this starter:

```powershell
# Run from the repository (or extracted starter) root. Python 3.9+ is required.
New-Item -ItemType Directory -Force 'D:\BloodlinesVisualLab\reports' | Out-Null
python tools\visuals\inspect_local_pack.py --input 'D:\BloodlinesVisualLab\originals\your-downloaded-pack.zip' --name 'Forests of San Andreas: Revised' --version '6.0-SP' --report 'D:\BloodlinesVisualLab\reports\forests-intake-001.json'
```

Replace the placeholder ZIP name with the actual file. The name/version are user-entered labels, not automatically authenticated metadata. If the distribution is RAR/7z, manually extract with a trusted local archive tool, inspect the extraction, and point `--input` at that folder. ZIP-compatible OIV files can be inventoried without running their installer.

The inventory lists file hashes, notice candidates, script/plugin files requiring review, and opaque nested archives. It reads input without extracting or executing it; it writes only a new external report. It rejects common unsafe archive paths, case-colliding Windows filenames, links and excessive input sizes. It does not certify malware safety or copyright permission, inspect a nested RPF, find tree positions, or approve the pack for gameplay. Read the original documents locally; the report contains names, not their complete copyrighted contents.

V0 acceptance: identify the exact pack/version and intended install changes, retain original checksums, account for optional scripts and dependencies, and confirm rollback. Missing or unknown contents remain explicit review items, never a silent success.

## V1: baseline the complete Forests package locally

First evaluate Forests as the creator packaged it. Do not cherry-pick a drawable or delete random YMAP files before understanding their dependent archetypes, textures, LOD parents and collision. Select one supported region at a time if the actual archive provides independent regions. Otherwise do not invent modularity: inspect the complete package in a backed-up test setup before making private compatibility edits.

Use the creator's current Enhanced instructions and the existing supported mod-archive installation mechanism. Asset archives do not belong beside `Bloodlines.dll` in `scripts`. Exact archive/dlc-list edits are pending inspection; this starter does not provide guessed RPF paths or auto-run an OIV. Keep original game archives untouched; work through the test installation's supported mod override mechanism.

Start with daytime and nighttime free-roam comparisons at identical positions. Do not stack a second forest, a road overhaul, a full shader package and a new grade in one pass. The goal of this first step is to identify what Forests changes and what it costs, not reach the final showcase look in one installation.

V1 acceptance: visibly loaded pack, matching near/distant appearances, no new startup errors, reproducible uninstall, and measured performance. This is not yet campaign-wide compatibility.

## V2: use the mapper to protect the mission layout

Load the installed game AND selected mod content into the local Enhanced-capable CodeWalker workflow. Original-game geometry alone is insufficient once new map objects are loaded. Inventory selected placements with model identity, full transforms, world-space bounds, collisions, streaming extents and dependencies. Keep those exports in the private workspace.

Join that information to current LocationBook positions plus personal surveyed overrides, mission-spawn offsets, vehicle routes, approach paths, camera shots and scene-blocking destinations. A point-to-point distance test is a first-pass candidate filter, not proof of usable geometry.

Protect full three-dimensional volumes: walking corridors and stairs; actor standing and interaction spaces; combat sightlines; boarding/door-swing areas; ground-vehicle paths; complete aircraft wings/tail/rotor envelopes and initial takeoff lanes; water launch and cargo-swing areas; and cinematic camera sightlines. Clearance depends on the actual model and action, not one magic radius for every mission.

Begin conflict review with SM01 freight loading, SM02 street/roof access, M07 rooftop work, M18-M22 harbor movement, M25's bridge/tunnels and boat departure, M26/M27/M37 runway staging, and M41-M43 northern/coastal preparation. Extend coverage as the catalog grows. Do not treat a check of only these sites as full-campaign acceptance.

For each conflict, prefer a private, version-pinned change to added scenery, not a mission relocation. Produce a proposed removal/move list and review it before applying locally. Recompute all affected draw distances/extents, parent/child counts, distant representations and collision data using the appropriate local tools. A removed visible tree must not leave a collision trunk or a distant ghost. Do not rely on a per-frame hide-entity trick to make a static map safe.

V2 acceptance: document unchanged mission coordinates and gameplay code, show the revised local scene at every affected path, and test the real actors/vehicles through it. A report that says 'near a mission' is a warning, not a validated fix. Automated geometry checks and live play remain distinct evidence.

## V3: refine the existing Bloodlines look

After the vegetation baseline is stable, improve the existing VisualAtmosphere profiles in a separate reviewed change. Verify modifier names against the installed game data, blend transitions deliberately, and give scene/interior/ability effects clear priority. Keep the approved ungraded sunset by default. No permanent wet-road trick, forced golden hour or mission weather/water change to improve screenshots.

Use Enhanced's available in-game rendering settings as an explicit player-side profile. Do not claim native C# calls add a replacement ray tracer, high-resolution materials, or arbitrary paint-reflection strength. Record actual settings instead of silently editing `settings.xml`. Add one material/road/vegetation-reflection module at a time only after its own compatibility and terms have been inspected.

V3 acceptance: readable faces and mission objectives, preserved highlights/shadow detail, no color-pop at time-band changes, correct return after scenes/abilities, and bounded performance. Restart/removal returns to the saved baseline. A toggle can release script effects; map-archive changes may require a game restart and separate uninstall.

## V4: performance and campaign acceptance

RTX 5080 is the confirmed target. Resolution and target FPS remain user decisions. Provisional priority: a stable underlying playable frame rate, not a promised 144 FPS or a percentage match to a video. Record upscaling and frame generation separately; keep both constant for each A/B comparison. No DLSS 5 dependency.

Use the supplied benchmark CSV. Compare A (current campaign and visual baseline), B (A plus Forests), and C (B plus reviewed Bloodlines visual changes). Repeat the same warmed route, weather, hour, traffic settings and camera. Record average/low FPS, frame-time spikes, VRAM usage, stutter/pop-in and mission failures. Add a cold-load check because warm caches can hide streaming problems. Hardware and shader caches can affect repeatability; do not label one short run definitive.

Required gameplay samples include a fast urban drive, wooded drive, rooftop arrival, apartment entry/exit, harbor work, bridge fight, and takeoff. Confirm that an apparent performance gain did not come from disabling collision, thinning required enemies or changing mission conditions. Keep the selected comparison DLL constant through this phase.

V4 acceptance: rollback works, no blocked mission action in the covered tests, visible improvement, and an agreed frame-time/memory budget. Record remaining untested missions and routes.

## V5: later local deployment automation

Not implemented in this starter. After inspecting the real pack, build an opt-in plan/apply/rollback tool. The plan lists exact source and destination files plus before/after hashes. Applying requires GTA closed, an explicit destination, and a fresh backup outside the game. Refuse changed source hashes, wrong editions or unexpected existing files. Rollback restores only files owned by that transaction, and refuses to overwrite newer changes without review. Never delete someone else's mods, copy saves from a release, or bundle third-party binaries into GitHub Actions artifacts.

Unknown-rights packs remain separately installed local dependencies. Future redistribution requires a separate permissions review, regardless of whether it is free. This workflow is practical separation, not a legal certification.

## Delivered now and still missing

Delivered: this plan; standard-library intake tool; synthetic tests; benchmark template. No third-party asset bytes, installer, graphics DLL or modified map is included. No source branches or game installations are silently replaced.

Missing input: the actual Forests ZIP/OIV (or locally extracted inventory and its README/install instructions). Missing environment: Ron's local game archives, current mod list and graphics-settings snapshot. Without these we cannot truthfully enumerate the pack's install paths, map conflicts, performance or exact reuse terms.

Run starter tests:

```powershell
python -m unittest discover -s tools\visuals -p 'test_*.py' -v
```

These tests check the inventory's behavior with synthetic files. They do not validate Forests, CodeWalker, the Bloodlines C# runtime, graphics quality or GTA gameplay. No C# files changed, so no new Bloodlines.dll is required for this starter.
