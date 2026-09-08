# Los Santos: Bloodlines

A single-player campaign overhaul for **Grand Theft Auto V (PC)**, built from the
*Los Santos: Bloodlines* omnibus production bible: three protagonists, a dynamic
3-way switch, per-character abilities, and a 70-mission campaign with 255 written
lines of dialogue.

This repository is the **mission-script layer** — the C# mod that runs on top of a
legally-owned copy of GTA V. It ships no Rockstar assets.

---

## Status

| Layer | State |
|---|---|
| Mod bootstrap, config, logging | working |
| Campaign data pipeline — bible PDFs → TSV → runtime | working, 79 missions / 292 cues / 6 surveyed anchors |
| Crew roster — spawn, companion AI, blips, respawn, story-character restore | working |
| Dynamic 3-way switch (bible §3) | working, rewritten off the bible's draft |
| Abilities — Overwatch Focus / Thermal Pulse / Slipstream Reflex | working, shared meter |
| Dialogue director (bible's AudioManager) | working — speaker-coloured subtitles, WAV playback when present |
| Checkpoints (bible's CheckpointManager) | working — stage, positions, health, wreck purge |
| Mission framework — stage machine, tracked entities, pass/fail | working |
| Objective library — 18 reusable objectives, missions as composition | working |
| Companion AI — explicit state machine incl. vehicle boarding | working |
| Mission dispatcher — registry-driven, prerequisites, external mission packs | working |
| Save state — `savegame.json`: progress, economy, safehouses, fleet upgrades | working |
| Fleet upgrades applied at runtime (turbine Granger, reinforced Kraken) | working |
| Packaging — `tools/package.py` builds an install-ready tree | working |
| OpenIV DLC asset pack (handling metadata, interiors) | scaffolded, untested, no interiors yet |
| QA harness (bible Track 4) | working — stage warp, checkpoint commit/restore, forced switch |
| **M01 "Ghost in the Dockyard"** | playable, on the bible's surveyed coordinates |
| **M02 "Loose Strands"** | playable — freeway intercept under a hard upload clock |
| **M03 "Cypress Foundry"** | playable — first three-character joint operation |
| **M04 "Severed Wire"** | playable — garage hit into a pursuit |
| **M05 "Tidal Lock"** | playable — Act I finale, cliff overwatch into a boat chase |
| **M06 "Clean Sweep"** | playable — thermite burn under a three-wave SWAT siege |
| **M07 "Wiretap Waltz"** | playable — mast tap, drone, parachute to a moving pickup |
| **M08 "Supply & Sever"** | playable — the turbine engines the fleet is built on |
| **M09 "Rolling Thunder"** | playable — aerial convoy shadow, sniper handoff |
| **M10 "Open Throttle"** | playable — speed-floor escort, the campaign's purest set piece |
| **M11 "Ironclad Dyno"** | playable — the quiet one: a dyno hold and three men talking |
| **M12 "Black Tide Recon"** | playable — stealth recon, detection is the fail state |
| **M13 "Smuggler's Cut"** | playable — three limpet charges, one detonation |
| **M14 "Airspace Blackout"** | playable — plane theft under a SAM ceiling |
| **M15 "Crawlspace"** | playable — non-lethal infiltration, alarm is the fail state |
| **M16 "The Heavy Lift"** | playable — Zancudo on M09's stolen transponder |
| **M17 "Sub-Zero Payload"** | playable — the second quiet one: outfitting the Kraken |
| **M18 "The Staging Line"** | playable — three deliveries, one per character |
| **SM01 "Lead & Kevlar"** | playable — Ice solo, no crew, switch locked |
| **SM02 "Zero-Day Injection"** | playable — Gohan solo, non-lethal stealth |
| **SM03 "Midnight Drift"** | playable — Guess solo, three-lap circuit |
| Dev menu (missions, stages, crew, world, dialogue, save) | working, `F8` with `[Dev] Enabled` |
| M19–M70, SM04–SM09 | written and loaded as data; no mission scripts yet |
| Interstitials (safehouses, workbenches, Weazel News), MLO interiors, custom peds, voice | not started |

The code builds clean with `--warnaserror` against ScriptHookVDotNet 3.6. It has
**not been run in game** — that needs a Windows machine with GTA V, which is the one
thing this repo cannot do for you.

## Quick start

```bash
python3 tools/package.py --build
```

That builds Release and lays out `build/deploy/` exactly as it installs. Copy
`build/deploy/scripts/` into your GTA V `scripts/` folder — that is the whole
install. Full steps, including ScriptHookV itself, in `docs/INSTALL.md`.

Default keys (the toolkit's binds): `Numpad 1/2/3` switch character, `Caps Lock`
ability, `F10` deploy the crew in free roam, `J` start the next mission, hold
`Backspace` to abort.

## The bible is the source of record

Mission titles, settings, objectives, synopses and every line of dialogue live in
the PDFs under `docs/bibles/`, not in code. The pipeline is:

```
docs/bibles/omnibus_v2.pdf + solo_missions_v1.pdf
        │  tools/parse_bible.py   (merges every bible, normalises the cast names)
        ▼
data/missions.tsv · data/dialogue.tsv · data/anchors.tsv
        │  copied to scripts/Bloodlines/data/
        ▼
CampaignData → MissionCatalog, DialogueDirector
```

The catalog interleaves the solo missions into the main line at the windows the
expansion specifies (Act I after M03, Act II after M28, Act III after M52), so
"next mission" plays the campaign in its intended order.

When a new revision of the bible arrives, drop it in `docs/bibles/`, re-run
`tools/parse_bible.py` and `tools/render_campaign_doc.py`, and rebuild. No C#
changes are needed for new or rewritten missions — only for new mission *scripts*.

`docs/BIBLE-NOTES.md` records where the bibles disagree and the open questions the
code had to answer to move forward. `docs/FEASIBILITY.md` tiers all 79 missions
Green/Yellow/Red by how hard each is to make reliable in the engine, with the faking
technique for every Red.

## Coordinates

All 53 live in `data/locations.tsv` with their provenance attached — `bible`,
`surveyed`, `zone-centre` or `estimate`. `tools/validate_locations.py` checks every
one against the game's real zone boundaries and writes `docs/LOCATION-AUDIT.md`;
that catches wrong-district and below-sea-level errors but cannot check accuracy.

For accuracy there is no substitute for standing on the spot. The dev menu's Survey
mode walks the list, teleports you to each estimate, captures where you stand on
`F11`, and writes `Bloodlines.Surveyed.ini` as it goes.

## Voice

`tools/generate_voice.py` batch-generates the 292 lines through ElevenLabs (bible
Track 3) into `scripts/Bloodlines/audio/<CUE_ID>.wav`, which the dialogue director
picks up automatically. Every line is written to read correctly as a subtitle alone,
so the campaign is playable with no voice pack at all.

## What this is not

It is not a GTA Online mod, and it must never be loaded into GTA Online — see
`docs/INSTALL.md`. It redistributes no Rockstar model, script, audio or texture.

## Layout

```
src/Bloodlines/       the mod
  Core/               config, logging, campaign data, dialogue, coordinates, helpers
  Crew/               protagonists, roster, the switch controller
  Abilities/          three abilities + the shared meter
  Missions/           framework, catalog, checkpoints, progress
  Missions/Campaign/  the mission scripts themselves
data/                 generated campaign data (missions, dialogue, anchors, registry)
assets/               OpenIV DLC pack source — handling metadata, interiors to come
config/               ini templates that ship to scripts/Bloodlines/
tools/                bible parser, campaign doc renderer, voice generator, packager
docs/                 install, toolchain, architecture, campaign, feasibility, QA, bible notes
docs/bibles/          the source bibles and their extracted text
```
