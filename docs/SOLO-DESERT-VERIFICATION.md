# Solo/desert repair - automated verification

Baseline: `de923e018203b76c55e1b0b475528e6b06aff3b6`.

Verified input: `461a1283f2873d5897c116de7a077daefb8df21c`; Actions run `34780313573`.

## story
```text
PASS: M27 passes
PASS: The approach aircraft has two seats and the transfer no longer blocks the script thread
PASS: Each desert chapter's approach has its authored lines
2760 story/runtime checks passed.
fetching Microsoft.NETFramework.ReferenceAssemblies.net48 1.0.3...
```

## regression
```text
PASS: Arrest enters the same recovery path
PASS: Cancelling recovery restores restart and control
PASS: Revive failure returns to story and opens the screen
PASS: Partial native failure still releases restart suppression
206 checks passed (stand-ins; live GTA validation still required).
```

## parser
```text
....
----------------------------------------------------------------------
Ran 4 tests in 0.015s

OK
```

## lint
```text
  ok    SM06  4/4 lines fired

  48 of 49 missions fire every written line.

No errors.
```

## locations
```text
fetching zone data...
wrote docs/LOCATION-AUDIT.md ï¿½ 1 of 824 flagged
```

Production warnings-as-errors compilation, generated scene/map freshness, Roslyn build and clean packaging passed.

DLL SHA-256: `a4d6671ba004dde303334f6f069f126d989977fee3b16f4efdb0b7f0f3f61ce6`.

No live GTA run or fresh CodeWalker archive query was performed. See SOLO-DESERT-PLAYTEST-REPAIRS.md for placement evidence, limits and the acceptance route.
