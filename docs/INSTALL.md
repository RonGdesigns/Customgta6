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
3. Build this repo:
   ```bash
   dotnet build src/Bloodlines/Bloodlines.csproj -c Release
   ```
   (or open `Bloodlines.sln` in Visual Studio and build Release/x64).
4. Copy `src/Bloodlines/bin/Release/Bloodlines.dll` into `<GTA V>/scripts/`.
5. Copy the `config/` files into `<GTA V>/scripts/Bloodlines/` — the mod creates
   that folder and writes defaults on first run if you skip this, but the shipped
   inis are commented.
6. Copy the `data/` folder into `<GTA V>/scripts/Bloodlines/data/`. **This one is not
   optional**: without it there are no mission titles, no objectives and no dialogue,
   and the mod will say so on screen. It carries all 70 main missions, the 9 solo
   missions and 292 dialogue cues.
7. Optional: put generated voice lines in `<GTA V>/scripts/Bloodlines/audio/` as
   `<CUE_ID>.wav` (see `tools/generate_voice.py`). The campaign plays fine without them.
8. Launch in **Story Mode**. `Bloodlines/Bloodlines.log` appears next to the inis;
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

All of them are remappable in `Bloodlines.ini`.

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
  ScriptHookV.dll, dinput8.dll, ScriptHookVDotNet.asi, ScriptHookVDotNet3.dll
  scripts/
    Bloodlines.dll
    Bloodlines/
      Bloodlines.ini, Bloodlines.Locations.ini, Bloodlines.Progress.ini
      Bloodlines.log
      data/     missions.tsv, dialogue.tsv, anchors.tsv
      audio/    <CUE_ID>.wav  (optional)
```

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
