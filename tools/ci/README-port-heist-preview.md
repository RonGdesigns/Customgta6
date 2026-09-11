# Continuous Port Heist comparison packaging

The retained `port-heist-preview` workflow is read-only: it builds and uploads comparison artifacts but cannot publish commits, merge a pull request, change the working branch, or modify a GTA installation. The one-time implementation and binary-refresh publishers have been removed.

## Why the workflow restores source bytes

Git on Windows may convert line endings during checkout. Portable debug information includes source checksums, which also influence deterministic assembly identifiers. The original comparison publication used a mixture of checkout and exact-blob inputs; its later clean rebuild differed in those build/debug identifiers. The prebuilt was refreshed from the exact committed source, with two identical consecutive builds and a separate previous exact-blob build agreeing.

The preview workflow now reads each tracked `src/` file from Git before compilation and records its SHA-256 in `source-preview.json`. It does not edit the repository's source. A supported compiler/toolchain change may also require a reviewed prebuilt refresh; do not weaken the byte-equality assertion to hide a mismatch.

## Acceptance before uploading

The production warnings-as-errors build, story/runtime suite, behavioral regression suite, parser tests, mission lint, coordinate validation, authored-scene freshness and mission-map freshness must pass. After the Roslyn rebuild:

- the freshly built DLL, committed `prebuilt/Bloodlines.dll`, and packaged DLL must match byte-for-byte;
- the clean package must not contain a campaign save, personal surveyed positions, or an active user INI;
- `BUILD-PROVENANCE.json` identifies the tested commit, initial comparison base, Actions run, binary checksum and `live_verified: false`.

The artifact contains an install-layout preview ZIP, a source ZIP, provenance and verification logs. No Rockstar binaries or runtime dependencies are added. The install layout retains the repository's `.example` configuration convention.

## Scope and installation caution

This comparison was based on `bb073aa9`. The separate M23–M27/Sage changes subsequently added to the working branch are not included. Preserve those changes during a future integration and rebuild the combined source; do not resolve a DLL conflict by blindly taking this binary. PR #26 remains draft and unmerged.

Use Story Mode and a disposable copied campaign. Back up the installed DLL, full Bloodlines data/configuration, appearance files and surveyed overrides. Keep backup DLLs outside the scripts folder and its subfolders. Preserve the current runtime dependencies and audio bank.

To test the combined operation, start M19 through normal campaign progression with prerequisites and the required solos complete. Developer bypass starts an individual phase for QA. Follow `docs/CONTINUOUS-PORT-HEIST.md`: underwater work, helicopter pickup, boat escort, coastal road transfer, inland flight/drive, verified deposit, beach regroup and the single final result. Repeat with skip, failure, phase restart, fresh-session resume and replay on copied saves. Passing the automated checks does not establish real GTA physics, streaming, pathfinding, animation quality or playability.
