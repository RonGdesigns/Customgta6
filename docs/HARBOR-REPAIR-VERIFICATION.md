# Current integrated harbor verification

Main input: `f8a37b9837b11f6661df081fd59616b7c06e6bb3`. See HARBOR-INTEGRATION.md and manifests.

Combined DLL SHA-256: `75caf923fcb46d116c8ee840f7e8cc132c7d28465a5ce419ef58a2c86b819b72`.

1721 story/runtime checks and 165 regression checks passed. No live GTA playthrough performed.

---

# Historical isolated harbor repair verification

Base: `d295306764fa137ceda3e1f3741e2a0d766bdb45`.

Verified input: `a8f1e80d82c1e970b2c6a357c70aa13005c0ba8d`; Actions run: `34641503403`.

Production warnings-as-errors build, parser tests, mission lint, locations, story freshness, mission-map freshness, Roslyn rebuild and clean packaging passed.

## Story suite
```text
PASS: In the boat, the ledger is stowed where Gohan can see it
PASS: M27 passes
PASS: The approach aircraft has two seats and the transfer no longer blocks the script thread
PASS: Each desert chapter's approach has its authored lines
1698 story/runtime checks passed.
fetching Microsoft.NETFramework.ReferenceAssemblies.net48 1.0.3...
```

## Regression suite
```text
PASS: Revive failure returns to story and opens the screen
PASS: Partial native failure still releases restart suppression
165 checks passed (stand-ins; live GTA validation still required).
```

DLL SHA-256: `df6d55276297dafef5c2c018d828f4db80316e7370ead561ca29ab0c0d553c6b`.

No GTA runtime was available: these are source/stub checks, not proof of reachable sea geometry, police behavior, score audibility or apartment rendering. Read HARBOR-PLAYTEST-REPAIR.md for the live acceptance route. The old phase-resume preview is superseded: this revision restarts the whole heist.
