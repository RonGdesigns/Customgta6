# Los Santos: Bloodlines

A single-player GTA V campaign mod: 70 main missions + 9 solo missions, three
playable brothers with switching and abilities, built from a set of design-bible
PDFs in `docs/bibles/`.

## The two rules

1. **Story Mode only. Never load GTA Online with this installed.** ScriptHookV
   presence in Online is a ban. Every helper in `tools/windows/` says so; do not
   add anything that could be read as making Online safe.
2. **Redistribute nothing Rockstar owns.** Extracted game data (zones, vehicles,
   peds) is fetched at runtime and cached under `build/`, never committed. The
   DLC asset pack ships as loose files for OpenIV to import, never as an `.rpf`.

## Build

```bash
python3 tools/package.py           # uses prebuilt/Bloodlines.dll, no SDK needed
python3 tools/package.py --build   # compiles first (needs the .NET SDK)
```

Targets .NET Framework 4.8 against `ScriptHookVDotNet3` 3.6.0 as a
compile-time-only reference. On Linux it cross-compiles via
`Microsoft.NETFramework.ReferenceAssemblies`; `DOTNET=/path/to/dotnet` points at
an SDK that is not on PATH.

**After changing C#, refresh the committed binary** or a machine without the SDK
installs a stale one:

```bash
dotnet build src/Bloodlines/Bloodlines.csproj -c Release
cp src/Bloodlines/bin/Release/Bloodlines.dll prebuilt/Bloodlines.dll
```

## Checks that must pass before pushing

```bash
python3 tools/lint_missions.py       # "No errors."
python3 tools/validate_locations.py  # "0 flagged"
```

The linter is the substitute for not having the game: it checks location keys,
dialogue cue ids against the firing mission, vehicle and ped model names against
public data dumps, catalog registration, crew deploys onto air or water, and
non-ASCII in the data files. It has caught real bugs that would have been
unfinishable missions in game (`new Model("mallard")` does not exist; the
Mallard's model name is `stunt`).

## How it fits together

```
BloodlinesMain (GTA.Script)   entry point, key handling, per-subsystem tick
DevTools       (GTA.Script)   QA harness, only with [Dev] Enabled = True
  ^ the ONLY two Script subclasses, on purpose
CrewRoster / SwitchController / CompanionController   the three brothers
MissionManager -> MissionCatalog -> Mission / ComposedMission
  ComposedMission = stages of Objectives (18 kinds, in Missions/Objectives/)
CampaignData    loads data/*.tsv at runtime
```

**Why one DLL and not one per act:** SHVDN instantiates and ticks every class
deriving from `Script` in `scripts/` *and its subfolders*. Missions are ordinary
classes the dispatcher constructs on demand, so only the running mission ticks.
Per-act assemblies would not change that and would add 70 assemblies to
version-match. External mission packs are still supported through the `assembly`
and `class_name` columns of `missions.tsv`.

**Passive objectives.** `Objective.IsPassive` marks fail-only objectives
(Protect, Timer, SpeedFloor, AvoidDetection, AltitudeCeiling). They never
complete, so `MissionStage.IsComplete` ignores them. A stage built only from
passive objectives can never finish and `ComposedMission.Validate()` refuses it.
Nine stages hung forever before this existed.

**Data is regenerated, not hand-edited.** `tools/parse_bible.py <pdfs...>`
rewrites `data/missions.tsv`, `dialogue.tsv`, `anchors.tsv` and
`campaign_registry.json`. Fix the parser, not the TSV. It folds typographic
characters to ASCII because GTA's text renderer is not dependable outside it.

## Conventions

- TSV at runtime, not JSON: .NET 4.8 has no built-in JSON reader. The one
  hand-rolled reader (`Core/Json.cs`) exists only for `savegame.json`.
- Coordinates in `data/locations.tsv` are estimates until surveyed in game with
  the F11 capture; 117 of 121 are still estimates.
- Log with `Logger`, not SHVDN's log. Identical repeated errors collapse to a
  count — a tick-rate fault produced 100,000 lines before that existed.
- Every subsystem is stepped separately in `OnTick`. One failure must not take
  the rest of the mod down with it.

## Verified environment

Confirmed working on the **Enhanced** build (`GTA5_Enhanced.exe`) with
ScriptHookV .Net Enhanced 3.9.0.6 running the 3.6.0 target API. Legacy is what
the toolchain grew up on; nothing in the mission code is build-specific. The
script hook itself is NOT interchangeable between builds.

## State

30 of 79 missions are playable (M01–M27, SM01–SM03); the rest are loaded as data
with no mission script yet. The code builds clean with `--warnaserror`.

Not started: M28–M70, SM04–SM09, the M55 switching prototype, interstitial
systems (safehouses, workbenches, Weazel News), MLO interiors, custom peds,
voice lines.

Read `docs/PLAYTEST.md` before a play session and `docs/INSTALL.md` for install,
the Legacy/Enhanced split, and what to do when `Bloodlines.log` never appears.
