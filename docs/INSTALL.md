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

### Legacy vs Enhanced — check this first

Rockstar's 2025 **Enhanced** release is a separate product from the original, now
listed as **Legacy**. They install to different folders and ship different
executables:

| Build | Default folder | Executable |
|---|---|---|
| Legacy | `...\Rockstar Games\Grand Theft Auto V` | `GTA5.exe` |
| Enhanced | `...\Rockstar Games\Grand Theft Auto V Enhanced` | `GTA5_Enhanced.exe` |

**The script hook is build-specific and the two are not interchangeable.** A
Legacy `ScriptHookV.dll` dropped next to `GTA5_Enhanced.exe` does not load, fails
silently, and takes every .NET script with it — including this one. Whatever
version of ScriptHookV and ScriptHookVDotNet you install, it has to be the one
built for the executable in that folder.

This project's toolchain assumptions — SHVDN 3 v3.6 API, OpenIV, CodeWalker — grew
up on **Legacy**. Nothing in the mission code is build-specific, and an Enhanced
setup has now been confirmed to satisfy every prerequisite:

```
ScriptHookV .Net Enhanced 3.9.0.6 (1.1.0.6)
  Loading API from .\ScriptHookVDotNet2.dll
  Loading API from .\ScriptHookVDotNet3.dll
```

That is **SHVDNE**, the Enhanced-build port of ScriptHookVDotNet, and its v3 API
is what this mod is compiled against (3.6 or newer). The Enhanced equivalents of
the other pieces: `RageOpenV.asi` in place of `OpenIV.asi`, and
`GTA5_Enhanced.exe` in place of `GTA5.exe`.

One caveat that only shows up at runtime: SHVDNE has its own version lineage, so
if its `ScriptHookVDotNet3.dll` has drifted from the official 3.6 reference
assembly this mod builds against, the failure is a `TypeLoadException` or
`MissingMethodException` — and it lands in `ScriptHookVDotNet.log`, **not** in
`Bloodlines.log`, because the script never gets far enough to open its own log.
See "When Bloodlines.log never appears" below.

### Where to run these from

Everything below runs from the **repo folder**, not the game folder:

```
C:\Users\<you>\Desktop\My work\Customgta6
```

Open a terminal there: in File Explorer, navigate to that folder, then either
**Shift + right-click in the empty space → "Open PowerShell window here" / "Open
in Terminal"**, or click the address bar, type `powershell`, and press Enter.

`check-setup.bat` and `install-bloodlines.bat` also work by double-clicking —
they hold the window open when launched that way, so the output does not flash
past. Double-clicked, `check-setup.bat` scans the usual install locations and
`install-bloodlines.bat` uses `BLOODLINES_GTA_PATH` if you have set it, and
otherwise tells you the path it needs.

Before installing anything, find out what you actually have:

```bat
tools\windows\check-setup.bat
```

Read-only. With no argument it scans the usual install locations; give it a path
to inspect one folder. It reports the build, which hook DLLs are present, what is
in `scripts\`, and whether `ScriptHookV.log` exists — that log is the ground
truth for "has a script hook ever actually run here."

Either way: **every game update breaks ScriptHookV** until Alexander Blade ships an
update. That is normal, and it is the main reason to disable auto-updates on the
copy you mod.

## Install order

1. Install ScriptHookV **for your build**: copy `ScriptHookV.dll` and `dinput8.dll`
   next to the executable (`GTA5.exe` on Legacy, `GTA5_Enhanced.exe` on Enhanced).
2. Install ScriptHookVDotNet: copy `ScriptHookVDotNet.asi`, `ScriptHookVDotNet2.dll`
   and `ScriptHookVDotNet3.dll` into the same folder. Create a `scripts/` folder
   there if it doesn't exist. The `.asi` is the part that actually loads .NET
   scripts — the `.dll`s alone do nothing.
3. Package the mod:
   ```bash
   python tools/package.py
   ```
   That produces `build/deploy/`, laid out exactly as it installs, using the
   committed `prebuilt/Bloodlines.dll`. **No .NET SDK required.** Add `--build` to
   compile from source instead — that needs the SDK
   (<https://dotnet.microsoft.com/download>), and is only worth installing if you
   are going to change the C#. Visual Studio users: build `Bloodlines.sln` in
   Release/x64, then run `package.py` with no flags — a local build always wins
   over the prebuilt DLL.
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

## Installing into an existing modded setup

If you already run a modded GTA V with a batch file that toggles mods on and off,
Bloodlines slots in beside it rather than replacing anything.

**What it touches:** `scripts\Bloodlines.dll` and `scripts\Bloodlines\`. That is
all. It writes no `.rpf`, edits no game archive, and does not go near your `mods\`
folder — so an OpenIV-based toggle keeps working untouched. The optional asset pack
(`assets/`) is the only part that involves `mods\`, and it is not needed to play.

**Your Online-safety batch file still governs everything.** Whatever it does to
`dinput8.dll`, `ScriptHookV.dll` or the `scripts` folder disables Bloodlines with
the rest. Do not build a second safety mechanism; use the one you have.

Four helpers in `tools/windows/`. They are lightly tested on Windows — read them
before trusting them:

| Script | Does |
|---|---|
| `check-setup.bat ["<GTA V path>"]` | **Read-only.** Reports the build (Legacy/Enhanced), which hook DLLs are present, what is in `scripts\`, and whether a script hook has ever run there. Run this first when anything is unclear. |
| `install-bloodlines.bat "<GTA V path>"` | Copies the built DLL and campaign data in. Re-runnable: your `Bloodlines.ini`, surveyed coordinates and `savegame.json` are never overwritten once they exist. |
| `bloodlines-toggle.bat "<GTA V path>"` | Renames `Bloodlines.dll` on/off, for A/B testing against your other scripts. Not an Online-safety tool. |
| `playtest-isolate.bat "<GTA V path>"` | Parks every other script into `scripts\_parked` so a playtest is unambiguous; run again with `restore` to put them back. |

Set `BLOODLINES_GTA_PATH` once and you can run all three with no argument.

### Key collisions — read this before the first launch

Bloodlines uses a lot of keys, and so do trainers. These are the likely clashes:

| Our key | Default use | Commonly also |
|---|---|---|
| `F8` | dev menu | **Menyoo** opens on F8 |
| `NumPad 1/2/3` | switch character | **Simple Trainer** navigates on the numpad |
| `Caps Lock` | special ability | some ability and sprint mods |
| `F11` | survey capture | a few map/teleport mods |
| `Insert` / `Delete` | checkpoint commit / restore | Enhanced Native Trainer variants |

Every one is a line in `scripts\Bloodlines\Bloodlines.ini`. If you run a trainer,
a set that usually stays clear:

```ini
[Keys]
SwitchIce = D1
SwitchGohan = D2
SwitchGuess = D3
Ability = Q
DevMenu = F7
MissionStart = J
DeployCrew = F10
DevCapture = F11
```

(`D1`/`D2`/`D3` are the number row. They collide with weapon slots in a firefight,
which is why the numpad is the default when nothing else wants it.)

### For the first playtest, run alone

Park the other scripts (`playtest-isolate.bat`). With a trainer and a handful of
script mods loaded, any bug could belong to any of them — and several will be
fighting over the same keys, the same wanted level, or the same weather. Load
ScriptHookV, ScriptHookVDotNet and Bloodlines only, get a clean baseline, then put
your setup back. `docs/PLAYTEST.md` assumes you have done this.

### Things that will fight the mod

- **Trainers that lock weather or time.** Missions set both from the bible
  (`ApplyBibleSetting`); a trainer holding "always sunny" wins, and M01 stops being
  a night mission.
- **Anything that spawns or clears peds on a timer.** Mission peds are marked
  persistent, but a cleanup script can still delete them mid-mission.
- **Other mods using `CHANGE_PLAYER_PED`** — character swap mods especially. Two
  systems fighting over which ped is the player will not end well.
- **Wanted-level modifiers.** M16 suppresses the wanted ceiling and restores it on
  cleanup; a trainer holding "never wanted" makes several missions trivial.

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
| `F8` | open the developer menu — missions, stages, crew, world, dialogue, save state |
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

### When Bloodlines.log never appears

`scripts\Bloodlines\Bloodlines.log` is written on the first tick. If it does not
exist after a Story Mode launch, the script never started, and the reason is in
the game root's `ScriptHookVDotNet.log` rather than anywhere in this project:

| In ScriptHookVDotNet.log | Means |
|---|---|
| no `Loading scripts from ...` line at all | SHVDN itself did not initialise — a game update almost certainly broke the hook |
| `Loading scripts from ...` but no mention of `Bloodlines.dll` | the DLL is not in `scripts\`, or is named `.dll.off` |
| `TypeLoadException` / `MissingMethodException` naming a GTA type | API drift between the SHVDN build installed and the 3.6 API this mod compiles against |
| `BadImageFormatException` | wrong architecture or a corrupt copy — reinstall the DLL |
| `Bloodlines.dll` loads, then an exception in `BloodlinesMain` | the mod started and failed on its own terms; that one *is* ours |

The last row is the only one where reinstalling or rebuilding helps. The rest are
the hosting layer, and no change to this repo fixes them.

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
