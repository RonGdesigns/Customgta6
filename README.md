# Los Santos: Bloodlines

A single-player campaign overhaul for **Grand Theft Auto V (PC)**, built from the
*Los Santos: Bloodlines* design bible: three protagonists, a dynamic 3-way switch,
per-character abilities, and a 70-mission spine.

This repository is the **mission-script layer** — the C# mod that runs on top of a
legally-owned copy of GTA V. It ships no Rockstar assets.

---

## Status

| Layer | State |
|---|---|
| Mod bootstrap, config, logging | working |
| Crew roster — spawn, companion AI, blips, respawn | working |
| Dynamic 3-way character switch (bible §6) | working, rewritten off the bible's draft |
| Abilities — Overwatch Focus / Thermal Pulse / Slipstream Reflex | working, shared meter |
| Mission framework — stage machine, tracked entities, pass/fail, progress save | working |
| 70-mission campaign spine | registered; 1 of 70 scripted |
| **M01 "Ghost in the Dockyard"** | playable vertical slice |
| M06, M10, M19–22, M27, M34, M44–48, M55, M62, M68–70 | specified in the registry, unwritten |
| MLO interiors, custom peds, cutscenes, audio | not started — see `docs/TOOLCHAIN.md` |

The code compiles clean against ScriptHookVDotNet 3.6. It has **not been run in
game** — that requires a Windows machine with GTA V installed, which is the one
thing this repo cannot do for you. Treat M01 as a first draft to test and tune,
particularly its coordinates (see below).

## Quick start

```bash
dotnet build src/Bloodlines/Bloodlines.csproj -c Release
```

Then follow `docs/INSTALL.md` — in short: ScriptHookV + ScriptHookVDotNet in your
GTA V folder, `Bloodlines.dll` and the `Bloodlines/` config folder into `scripts/`.

Default keys: `1` `2` `3` switch character, `Q` ability, `F10` deploy the crew in
free roam, `J` start the next mission, hold `Backspace` to abort.

## Coordinates are approximate on purpose

Every world position lives in `config/Bloodlines.Locations.ini`, and the shipped
values put each beat in roughly the right corner of the Port of Los Santos without
claiming to be surveyed. Set `[Dev] Enabled = True`, fly to the real spot, press
`F11`, and paste the captured block into the locations file. That loop is the
intended way to author a mission here.

## What this is not

It is not a GTA Online mod, and it must never be loaded into GTA Online — see
`docs/INSTALL.md#staying-banned-free`. It also does not redistribute any Rockstar
model, script, audio or texture.

## Layout

```
src/Bloodlines/       the mod
  Core/               config, logging, coordinates, helpers
  Crew/               protagonists, roster, the switch controller
  Abilities/          three abilities + the shared meter
  Missions/           framework, 70-slot registry, progress
  Missions/Campaign/  the mission scripts themselves
config/               ini templates that ship to scripts/Bloodlines/
docs/                 install, toolchain, architecture, campaign
```
