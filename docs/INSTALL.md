# Install & build

## What you need

| Piece | Why | Where |
|---|---|---|
| **GTA V for PC**, legally owned | the mod runs inside it | Rockstar Store / Steam / Epic — any of them work |
| **ScriptHookV** (Alexander Blade) | lets native-code plugins talk to the game | dev-c.com |
| **ScriptHookVDotNet 3** | runs C# scripts on top of ScriptHookV | github.com/scripthookvdotnet/scripthookvdotnet |
| **.NET Framework 4.8** runtime | what SHVDN 3 targets | Windows Update, or Microsoft |
| **OpenIV** | editing/adding game archives (custom peds, vehicles) | openiv.com |
| **CodeWalker** | interiors (MLO), navmesh, cover nodes | github.com/dexyfex/CodeWalker |
| Visual Studio 2022 or the .NET SDK | building this repo | optional if you use `dotnet build` |

A Rockstar Store copy is fine. Nothing here needs Steam or a specific storefront.

### Legacy vs Enhanced

Rockstar's 2025 "Enhanced" release is a separate build from the original, now
listed as "Legacy". The modding stack — ScriptHookV, SHVDN, OpenIV, CodeWalker,
and the decade of mods this project borrows technique from — grew up on **Legacy**,
and that is what this project targets. Check your Rockstar Launcher library for
which builds you own before installing anything.

Either way: **every game update breaks ScriptHookV** until Alexander Blade ships an
update. That is normal, and it is the main reason to disable auto-updates on the
copy you mod.

## Install order

1. Install ScriptHookV: copy `ScriptHookV.dll` and `dinput8.dll` next to `GTA5.exe`.
2. Install ScriptHookVDotNet: copy `ScriptHookVDotNet.asi`, `ScriptHookVDotNet2.dll`
   and `ScriptHookVDotNet3.dll` next to `GTA5.exe`. Create a `scripts/` folder there
   if it doesn't exist.
3. Build and package in one step:
   ```bash
   python3 tools/package.py --build
   ```
   That produces `build/deploy/`, laid out exactly as it installs. (Visual Studio
   users: build `Bloodlines.sln` in Release/x64, then run `tools/package.py` without
   `--build`.)
4. Copy the contents of `build/deploy/scripts/` into `<GTA V>/scripts/`. That is the
   whole install — DLL, config, and the campaign data the mod cannot run without.
5. Optional: generate voice lines and fold them in:
   ```bash
   export ELEVENLABS_API_KEY=...
   python3 tools/generate_voice.py --out build/audio
   python3 tools/package.py --audio build/audio
   ```
   The campaign plays fine with no audio — every line is written to read as a subtitle.
6. Optional: import `build/deploy/_openiv_import/bloodlines_assets` with OpenIV for
   the custom handling pack. See `assets/README.md`.
7. Launch in **Story Mode**. `Bloodlines/Bloodlines.log` appears next to the inis;
   it is the first place to look when something doesn't happen.

## Controls

| Key | Action |
|---|---|
| `Numpad 1` / `2` / `3` | switch to Ice / Gohan / Guess |
| `Caps Lock` | activate the current character's ability (toggle) |
| `F10` | deploy or stand down the crew in free roam |
| `J` | start the next unfinished scripted mission |
| hold `Backspace` | abort the running mission, or stand the crew down |
| `F11` | dev: capture the current coordinates (needs `[Dev] Enabled = True`) |

All of them are remappable in `Bloodlines.ini`. `J` always starts the next mission
whose prerequisite is satisfied, which is how the solo missions arrive in the right
place in the campaign.

### QA harness

Track 4 of the bible, active only with `[Dev] Enabled = True` so a stray key never
rewinds a player who didn't ask for a QA build:

| Key | Action |
|---|---|
| `Page Up` / `Page Down` | warp the running mission forward / back one stage |
| `Insert` | commit a checkpoint |
| `Delete` | restore the last checkpoint |
| `F11` | capture coordinates to `Bloodlines.Captures.ini` |

`F9` reloads all SHVDN scripts without restarting the game — that one is SHVDN's own
binding, not this mod's.

### Folder layout when installed

```
Grand Theft Auto V/
├── ScriptHookV.dll, dinput8.dll
├── ScriptHookVDotNet.asi, ScriptHookVDotNet3.dll
├── mods/update/x64/dlcpacks/
│   └── bloodlines_assets/dlc.rpf      built with OpenIV from assets/ (optional)
└── scripts/
    ├── Bloodlines.dll                 the mod
    └── Bloodlines/
        ├── Bloodlines.ini             keybinds, crew, abilities, dev flags
        ├── Bloodlines.Locations.ini   coordinates (survey these with F11)
        ├── Bloodlines.log             written every session
        ├── data/
        │   ├── missions.tsv           79 missions — the runtime registry
        │   ├── dialogue.tsv           292 cues
        │   ├── anchors.tsv            the bible's surveyed coordinates
        │   ├── campaign_registry.json the same registry for external tooling
        │   └── savegame.json          progress, economy, safehouses, fleet
        ├── audio/
        │   ├── Act1/M01/M01_S1_01_ICE.wav ...
        │   ├── Act2/, Act3/, Solo/
        │   └── (a flat audio/<CUE>.wav also works for quick tests)
        └── missions/                  external mission-pack DLLs, if you add any
```

**Why one DLL and not one per act:** ScriptHookVDotNet instantiates and ticks every
class deriving from `Script` in `scripts/` *and its subfolders*. This mod has exactly
two such classes — the entry point and the dev tools — and missions are ordinary
classes the dispatcher constructs on demand, so only the running mission ticks no
matter how many are written. Splitting into per-act assemblies would not change that,
and would add 70 assemblies to version-match. If you do want a mission in its own
DLL, that is supported: drop it in `Bloodlines/missions/` and fill in the `assembly`
and `class_name` columns for that mission in `missions.tsv`.

## Staying banned-free

**Never load GTA Online with ScriptHookV, SHVDN or this mod present.** Rockstar
bans for modified files in Online, and it does not care that your mod is a
single-player campaign. ScriptHookV refuses to run in Online by design, but the
files themselves are still a risk and any `.asi` loader is detectable.

The safe habit, in order of preference:

1. Keep a second, offline copy of the game folder for modding and leave your online
   copy clean.
2. Or move `dinput8.dll`, `ScriptHookV.dll`, `*.asi` and `scripts/` out of the game
   folder before you play Online. A two-line batch file that renames a `mods/`
   folder in and out is the usual approach.

Also worth knowing:

- Single-player modding is tolerated by Rockstar; tools that touch Online are not,
  and have been taken down.
- Do not redistribute Rockstar assets. Ship scripts and your own content only.
- Back up your `GTA5.exe`, your `update.rpf`, and your save folder before you start.
  OpenIV's mods folder (`mods/`) exists precisely so you never edit originals.
