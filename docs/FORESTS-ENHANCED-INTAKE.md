# Forests 6.0-SP: inspected archive and first local integration

Status: actual uploaded archive inspected; Enhanced-only private staging implemented and exercised on that archive. Nothing installed in GTA. No forest placements, models or collision have been edited. No Bloodlines C# or DLL change. No mission rewrite. No graphics benchmark or in-game acceptance is claimed.

Repository baseline: `6867c858d45e0a7d11369d33ac117f3f044a9b8a`. Visual work remains on `codex/private-visuals-foundation` / draft PR46, separate from mission repair. This intake supersedes the earlier plan's statement that the Forests archive was unavailable; its other safeguards remain in effect.

## 1. What was actually supplied and inspected

Original upload: `9f30c4-Forests of San Andreas Revised v6.0-SP.zip`.

SHA-256: `b8866ca90b2c3d9c33dc1086e91f1499897068613574d78d6286db4730edab69`.

The ZIP contains 17 files and 5 directories: separate Enhanced and Legacy distributions, creator notes and two coverage maps. All ZIP member streams were read and hashed successfully. This checks archive integrity, not malware safety or ownership.

| Enhanced input | Bytes | SHA-256 |
| --- | ---: | --- |
| `GTA V Enhanced/forest_n/dlc.rpf` | 25,811,456 | `52fea1336d5f683465022cabcfc7d11ffe291ec49d06e883059a9b0f9ab09db0` |
| `GTA V Enhanced/forest_s/dlc.rpf` | 39,506,944 | `1166e91efca4a5efa1211e8dc9e6aaf1cb2e252b4dee552111d4f8552d9da7eb` |

The two Enhanced DLCs total 65,318,400 bytes on disk. That is NOT a VRAM estimate.

Both have readable OPEN-format RPF directories. A bounded read-only table-of-contents inspection followed 13 nested RPF archives, read the four binary XML manifests, and indexed 755 non-container files:

| Indexed type | North | South | Total |
| --- | ---: | ---: | ---: |
| Placement maps `.ymap` | 69 | 104 | 173 |
| Collision resources `.ybn` | 173 | 252 | 425 |
| Drawable dictionaries `.ydd` | 46 | 58 | 104 |
| Archetypes `.ytyp` | 19 | 19 | 38 |
| Texture dictionaries `.ytd` | 2 | 2 | 4 |
| Map manifests `.ymf` | 3 | 3 | 6 |
| Drawable `.ydr` | 1 | 0 | 1 |
| Configuration XML | 2 | 2 | 4 |

These are file counts, NOT counts of individual trees or decoded collision triangles. Resource payloads were not decoded into world transforms. Every traversed directory was readable without game keys. No script/plugin/shader execution occurred.

The creator's `information.txt` reports 5,538 northern props and 8,228 southern props (13,766 total). Those are author-reported counts, not a recount from decoded YMAP entities.

## 2. What should and should not be used for Enhanced

Use the TWO DLC files under `GTA V Enhanced` and that directory's `installation.txt`.

Do not run the eight `.oiv` packages under `GTA V Legacy`. Their installation and compatibility manifests are for a different edition. In particular, the Legacy installation additionally describes a replacement `default.fxc`, scenario files, and the southern `FoSAShelter.3.cs` script. These are NOT part of the Enhanced instructions and are not staged by our helper.

No `.cs`, `.dll`, `.asi`, `.ysc`, `.fxc`, or `.ymt` file appears in the traversed Enhanced DLC index. This is an inventory observation, not a security certification of the assets or the game loader.

Do not assume the Legacy shelter-portal behavior is provided by the Enhanced package. Do not copy its script across editions to make that assumption true.

## 3. Two technical findings that affect the visual plan

### Reflections have a documented quality-setting caveat

`GTA V Enhanced/installation.txt`, under Known issues, says the dedicated reflection models can show rectangles around vegetation. The author's stated workaround is Enable Ray Tracing ON with Ray Traced Reflections set to Very High or Ultra, where those dedicated models are not used.

The README's explanation of shader adaptation is explicitly dated April 2025. Treat it as a limitation documented for this supplied release, NOT proof that all later Enhanced shader tooling is incapable of adaptation. The effect and workaround still require confirmation on Ron's actual installed build.

Proposed baseline: record the graphics settings, compare A and B at the same reflection settings, and evaluate performance on Ron's RTX 5080. Do not introduce DLSS 5, a Legacy shader, another lighting overhaul, or a new mission DLL for this first comparison. If the recommended reflection mode is too expensive, investigate this exact rendering issue instead of silently promising lower settings will look the same.

### North also overlays a shared tree model

The northern `content.xml` registers `x64/levels/gta5/props/vegetation/v_trees.rpf` with overlay enabled. That nested archive contains `test_tree_forest_trunk_01.ydr`. Consequently, North is not solely new placement data: it can also affect uses of that shared tree model.

Its dimensions/material behavior must be inspected with the local Enhanced-aware toolchain. Do not blindly remove the overlay or rename assets: references, textures, collision and distant models may depend on them. The four DLC XML manifests and all indexed paths are retained in the private intake report, not uploaded to public CI.

## 4. Rights and private handling

Read the original `information.txt`, installation documents, changelog and nested OIV descriptions. They acknowledge JRod's original work and permission to publish the extension. No separate broad downstream redistribution license was found in the inspected text/manifests. This does not clear rebundling and does not change the stated private local scope.

The repository remains public. Only OUR tooling/tests and this analysis belong there. Downloaded or modified RPFs, textures, models, original creator documents, geometry exports and personal reports remain outside every Git worktree and public package/CI input. Do not put these files into `assets/bloodlines_assets` or the normal `build/deploy` directory.

## 5. What has been implemented now

`tools/visuals/prepare_forests_enhanced.py` adds two bounded local operations:

- `stage`: validates the pinned original ZIP, checks each selected DLC against its exact size/hash, stages only North/South Enhanced packs and original notices into a NEW external folder. Supports north, south or both. No RPF editing and no game installation.
- `dlclist`: reads an exported UTF-8 DLC list and writes a NEW additive candidate. Preserves existing entries, order, comments, BOM and original text except new lines. Rejects duplicate selected registrations, unsupported XML/DTD inputs and overwrite attempts. It is NOT a profile switch: selecting North does not remove a previously installed South entry.

The helper rejects live GTA/Git destinations, existing outputs, links/junctions, unrecognized archive hashes and selected-file hash failures. It neither downloads nor executes third-party content, and it never edits saves, DLLs, config, the installed loader or the original game archives. The previous read-only inventory helper remains unchanged.

The local verification ran 43 synthetic tests (21 previous inventory tests plus 22 new preparation tests), then staged both REAL Enhanced payloads and rechecked their hashes. Synthetic tests cover edition filtering, one/both pack selection, input/output preservation, error cleanup, Git/game path refusal, additive XML, comments/BOM, idempotence and duplicate rejection. No actual user dlclist was supplied or changed. Windows CI should also run these fixtures before portability is accepted.

## 6. Local use

Create a private workspace, for example `D:\BloodlinesVisualLab`, OUTSIDE the game and repository. Keep the original ZIP in `originals` and the new outputs in `work`. Create those parent folders first.

From the source/starter tools folder:

```powershell
python tools\visuals\prepare_forests_enhanced.py stage --input "D:\BloodlinesVisualLab\originals\9f30c4-Forests of San Andreas Revised v6.0-SP.zip" --output "D:\BloodlinesVisualLab\work\forests-6.0-enhanced" --parts both
```

This creates the correct candidate layout:

```text
forests-6.0-enhanced/
  mods/update/x64/dlcpacks/forest_n/dlc.rpf
  mods/update/x64/dlcpacks/forest_s/dlc.rpf
  creator-notices/
  STAGE-MANIFEST.json
  ADD-TO-DLCLIST.txt
  READ-ME-FIRST.txt
```

The output is personal staging, NOT a distributable Bloodlines package. Keep it private. No `update.rpf` or Bloodlines DLL is provided or replaced.

### Install only after checking the existing loader and taking backups

Follow the supplied Enhanced installation document. It specifies OpenRPF and CodeWalker 30_dev48 or newer configured for the Enhanced game folder. If an Enhanced archive loader is already working (for example, a separately configured loader), inspect that setup first; do not stack or replace loaders automatically. The archive does not include those tools. Current dependency compatibility is a separate check against the actual game version.

Close GTA. Back up the current `mods/update/update.rpf`, the current DLC list and any existing forest directories outside the game/repo. Preserve the current Bloodlines installation and use copied campaign saves. Record the initial enabled packs.

Copy the selected `forest_n` and/or `forest_s` folders into:

```text
<GTA V Enhanced>/mods/update/x64/dlcpacks/
```

If `mods/update/update.rpf` ALREADY exists, preserve it. Do not replace it with the stock archive: that could remove other installed mods. If it does not exist, the author instructs copying the installation's own current `update/update.rpf` there.

Using CodeWalker RPF Explorer, export THIS file from the MODS archive:

```text
<GTA V Enhanced>/mods/update/update.rpf/common/data/dlclist.xml
```

The portion after `update.rpf` is an internal archive path, not a Windows folder. Do not copy a loose `dlclist.xml` beside the RPF and expect it to load.

Add the missing pack entries before `</Paths>` while retaining everything already there, or generate a candidate from the exported file:

```powershell
python tools\visuals\prepare_forests_enhanced.py dlclist --input "D:\BloodlinesVisualLab\exports\dlclist.xml" --output "D:\BloodlinesVisualLab\exports\dlclist.forests.xml" --parts both
```

Review/import that candidate back as `common/data/dlclist.xml` inside the SAME mods archive. The required entries are:

```xml
<Item>dlcpacks:/forest_n/</Item>
<Item>dlcpacks:/forest_s/</Item>
```

Do not import Legacy OIV/shader/scenario/script payloads. Do not change `scripts/Bloodlines.dll`, Bloodlines data, saves or surveyed positions. Start Story Mode only. Keep lighting, time, weather, renderer settings and the campaign build constant for A/B comparison.

### Rollback

Remove ONLY the entries and folders newly added by this test. Do not remove a previously installed North/South version without restoring its backup. Restore the entire backed-up mods `update.rpf` only if no subsequent unrelated changes would be lost; otherwise remove only the two intended registrations through the archive editor. Do not delete the full mods folder, reset gameconfig, or restore an older mission DLL as a vegetation rollback.

## 7. Next compatibility pass: geography, not blind deletion

The supplied North coverage image concentrates additions around Paleto, Chiliad, Raton/Cassidy and north/east Alamo/Grapeseed approaches. South covers hills and countryside around Route 68, the prison/wind farm and the eastern hills. These images identify review regions, NOT exact 3D clearance or proof of individual mission conflicts.

Priority checks: M25's tunnel/deck/parachute/boat route; M26/M27/M37 McKenzie runway origins, spare aircraft and initial takeoff corridors; M41/M43 northern staging; M23/M28/M30 and other southern approaches. Test scenes and owned-property access too. North's shared tree overlay means limiting checks only to the added placement dots is insufficient.

Use CodeWalker against the installed ENHANCED game with the selected forest DLCs loaded. Export the relevant YMAP entity transforms AND associated YTYP bounds/collision/distant dependencies. Compare full spatial volumes and routes to the CURRENT location book plus personal survey overrides. A center point or planar dot is not a collision test. Camera sightlines and actor navigation need live confirmation.

No automatic tree deletion or mission relocation is authorized by the inspector. Propose conflicting scenery changes for review; keep collision, visible meshes, reflection and LOD/SLOD dependencies consistent. Never mark a location surveyed based on this archive index.

## Sources

Primary uploaded sources: `information.txt`; `GTA V Enhanced/installation.txt`; `GTA V Legacy/installation.txt`; `changelog.txt`; both coverage JPGs; the Enhanced DLCs' unencrypted directory tables and `content.xml` / `setup2.xml` manifests.

Outside technical checks (not replacements for the supplied instructions):
- https://www.gta5-mods.com/tools/openrpf-openiv-asi-for-gta-v-enhanced (author documentation and dated changelog; current installed-build compatibility still unverified).
- https://www.gta5-mods.com/maps/forests-of-san-andreas-revised (original distribution page).
- https://raw.githubusercontent.com/dexyfex/CodeWalker/master/CodeWalker.Core/GameFiles/RpfFile.cs (RPF record format reference; no game keys or resource decoding used for the directory inspection).
