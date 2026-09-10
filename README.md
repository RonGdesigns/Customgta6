# Los Santos: Bloodlines

A single-player campaign overhaul for **Grand Theft Auto V (PC)**, built from the
*Los Santos: Bloodlines* omnibus production bible: three protagonists, a dynamic
3-way switch, per-character abilities, and a 70-main-mission and 9-solo campaign with 292 written
lines of dialogue.

This repository is the **mission-script layer** — the C# mod that runs on top of a
legally-owned copy of GTA V. It ships no Rockstar assets.

---

## Project Objective
Deliver a comprehensive, production-grade campaign overhaul and full literary omnibus publication for *Los Santos: Bloodlines*, novelizing the entire 70-mission narrative across three protagonists with complete artwork suite, character dossiers, and unified printable volume.

## Build Checklist
- [x] Novelize Genesis Arc (Prologue & Chapters I–VI) with canonical character dialogue and literary pacing (2026-09-09)
- [x] Novelize Turbine Arc (Chapters VII–XI) including the dyno room confrontation and convoy shadow (2026-09-09)
- [x] Novelize Port Heist Arc (Chapters XII–XXII) culminating in the Berth 44 drydock breach (2026-09-09)
- [x] Novelize Blaine County Exile & Desert Citadel Arc (Chapters XXIII–XXXV) (2026-09-09)
- [x] Novelize Deep-Sea Rig Sabotage & SAM Airspace Breach Arc (Chapters XXXVI–XLVIII) (2026-09-09)
- [x] Novelize Scorched Earth & The Historic Siege of Davis (Chapters XLIX–LX) (2026-09-09)
- [x] Novelize The Reckoning atop Maze Bank Tower, Spire BASE Jump & Epilogue (Chapters LXI–LXX) (2026-09-09)
- [x] Generate Official Book Cover matching Project Book trio art style without logos (2026-09-10)
- [x] Generate and curate full visual art suite: Act I Port Heist, Act II Chiliad Skyfall, Act III Siege of Davis, Climax Maze Bank Jump, Colonel Vance Dossier, Armored Turbine Granger, and Tactical Operations Map (2026-09-10)
- [x] Extract and curate canonical character dossier portraits for Guess (Ron Ortiz), Ice (Darius Vance), and Gohan (Devin Mercer) (2026-09-10)
- [x] Redo KJ character illustration as a light-skinned male tuner mechanic with stylish dreadlocks (`docs/Bloodlines_Dossier_KJ_Mechanic.jpg`) (2026-09-10)
- [x] Compile unified Master Omnibus PDF (`docs/Bloodlines_Novel_Complete_Omnibus_Edition.pdf`) unifying all 70 chapters, solo missions, epilogue, front matter, table of contents, and all 12 embedded illustrations (2026-09-10)
- [ ] In-game mission scripting for remaining Act II Blaine County missions (M31–M48)
- [ ] In-game mission scripting for Act III missions (M49–M70)

## Done Log
- **2026-09-09**: Completed literary novelization batches 1 through 6 covering all 70 missions, solo missions, prologue, and epilogue.
- **2026-09-10**: Generated and cleaned official book cover art, removed all Rockstar logos and "Grand Theft Auto" text.
- **2026-09-10**: Produced high-resolution illustrations for Act I, Act II, Act III, Climax, Colonel Vance, Turbine Granger, and San Andreas Tactical Map.
- **2026-09-10**: Extracted authentic Project Book portraits for Guess, Ice, and Gohan.
- **2026-09-10**: Redid KJ character dossier to accurately depict a light-skinned male mechanic tuner with authentic dreadlocks.
- **2026-09-10**: Built `tools/generate_novel_pdf_omnibus.py` and successfully compiled the 107-page complete Master Omnibus novel PDF (`docs/Bloodlines_Novel_Complete_Omnibus_Edition.pdf`) with all 12 photos and illustrations embedded.

## Notes & Decisions
- **2026-09-10 — KJ Character Canon**: Per user direction, KJ is canonically established as a light-skinned male tuner mechanic with dreadlocks. Base image composited with high-fidelity GTA-style dreadlocks, ambient lighting calibration, and smooth hairline transitions, saved to `docs/Bloodlines_Dossier_KJ_Mechanic.jpg`.
- **2026-09-10 — Logo & Trademark Sanitization**: Removed all Rockstar logos, star insignias, and "Grand Theft Auto" text from book cover, vehicle art, and climax illustration to maintain standalone original publication standards.
- **2026-09-10 — Master Omnibus Unification**: Aggregated all 7 separate story volumes into a single 107-page master volume with two-pass page numbering, Act frontispieces, intelligence dossiers, and technical schematics.


---

## Status

| Layer | State |
|---|---|
| Act I — 22 gameplay scripts | automated flow checks pass; live testing ongoing |
| Act II — 8 of 26 scripted (M23–M30) | in progress |
| Mod bootstrap, config, logging | working |
| Campaign data pipeline — bible PDFs → TSV → runtime | working, 79 missions / 292 cues / 6 surveyed anchors |
| Crew roster — spawn, companion AI, blips, respawn, story-character restore | working |
| Death and busted — the engine cannot restart a `CHANGE_PLAYER_PED` ped, so the mod does it | working |
| Dynamic 3-way switch (bible §3) | working, rewritten off the bible's draft |
| Abilities — Overwatch Focus / Thermal Pulse / Slipstream Reflex | working, shared meter |
| Dialogue director (bible's AudioManager) | working — speaker-colored subtitles, WAV playback when present |
| Checkpoints (bible's CheckpointManager) | full restart only; position-only checkpoint restore is blocked |
| Mission framework — stage machine, tracked entities, pass/fail | working |
| Objective library — 18 reusable objectives, missions as composition | working |
| Companion AI — explicit state machine incl. vehicle boarding | working |
| Mission dispatcher — registry-driven, prerequisites, external mission packs | working |
| Save state — `savegame.json`: progress, economy, safehouses, fleet upgrades | working |
| Fleet upgrades applied at runtime (turbine Granger, reinforced Kraken) | working |
| Packaging — `tools/package.py` builds an install-ready tree | working |
| Static checks — mission linter, coordinate audit, stage validation, all in CI | working |
| OpenIV DLC asset pack (handling metadata, interiors) | scaffolded, untested, no interiors yet |
| QA harness (bible Track 4) | failure/retry, force completion and survey; unsupported stage warps blocked |
| **M01 "Ghost in the Dockyard"** | scripted, using runtime ground checks and user survey overrides |
| **M02 "Loose Strands"** | scripted — moving-van proximity hack, rear-door drive pickup and crew extraction |
| **M03 "Cypress Foundry"** | playable — first three-character joint operation |
| **M04 "Severed Wire"** | playable — garage hit into a pursuit |
| **M05 "Tidal Lock"** | scripted — shore overwatch, boat capture and living-witness questioning |
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
| **M19–M22 The Port Heist** | playable — four-part arc: breach, airlift, escort, and the strike that ends Act I |
| **M23–M27** | playable — the Blaine County exile opens: new base, dredge, canyon hold, dogfight, and the mid-air boarding |
| **SM01 "Lead & Kevlar"** | playable — Ice solo, no crew, switch locked |
| **SM02 "Zero-Day Injection"** | playable — Gohan solo, non-lethal stealth |
| **SM03 "Midnight Drift"** | playable — Guess solo, three-lap circuit |
| Dev menu (missions, stages, crew, world, dialogue, save) | working, `F8` with `[Dev] Enabled` |
| M28–M30, SM04–SM06 | first-pass desert gameplay; live survey/physics checks pending |
| M31–M70, SM07–SM09 | dialogue and detailed gameplay/scene plans; no mission scripts yet |
| Interstitials (safehouses, workbenches, Weazel News), MLO interiors, custom peds, voice | not started |

The code builds clean with `--warnaserror` against ScriptHookVDotNet 3.6. It has
**not been run in game** — that needs a Windows machine with GTA V, which is the one
thing this repo cannot do for you.

## Quick start

```bash
python3 tools/package.py            # add --build to compile from source (needs the .NET SDK)
```

That lays out `build/deploy/` exactly as it installs, using the committed
`prebuilt/Bloodlines.dll` — no .NET SDK needed to play. Copy
`build/deploy/scripts/` into your GTA V `scripts/` folder, or on Windows run
`tools\windows\install-bloodlines.bat "<GTA V path>"` to do it. Full steps,
including ScriptHookV itself and the Legacy/Enhanced split, in `docs/INSTALL.md`.

Default keys (the toolkit's binds): `Numpad 1/2/3` switch character, `Caps Lock`
ability, `F10` deploy the crew in free roam, `J` start the next mission, hold
`Backspace` to abort.

## The bible is the source of record

Mission titles, settings, objectives, synopses and every line of dialogue live in
the PDFs under `docs/bibles/`, not in code. The pipeline is:

```
docs/bibles/omnibus_v2.pdf + solo_missions_v1.pdf
        │  tools/parse_bible.py   (merges every bible, normalizes the cast names)
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

`docs/PLAYTEST.md` is the script for the first session at the machine — what to test,
in what order, and what "wrong" looks like at each stage.

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
docs/                 install, toolchain, architecture, campaign, feasibility, playtest, QA, bible notes
docs/bibles/          the source bibles and their extracted text
```


## Recovery and survey validation update

Death/arrest recovery now runs as a bounded state machine with independent cleanup.
Current missions fail and can be retried from the beginning after death; complete
world checkpoint restoration is not implemented and is explicitly guarded.
The survey marks a yellow GPS route, offers F7 teleport, and saves F11 captures for
automatic reload. End skips and Home returns. M01 spawns use the surveyed positions.
Companions respond to attackers and use animated entry with separate seat reservations
for nearby cars. Catch-up warps remain a delayed/distance fallback.

Validation: build with warnings as errors, mission/location checks, and
`python tools/run_regression_tests.py`. Live Story Mode validation is still required.


## Story and mission discovery update

See [STORY-AUDIT.md](docs/STORY-AUDIT.md) for the implemented opening/aftermath scenes, KJ, nickname UI, mission markers, switch behavior, validation and live test sequence.


## M01 live-playtest correction

See `docs/M01-HOTFIX.md` (or `M01-HOTFIX.md` from this docs folder) for the
current exterior staging, camera fix, assigned M01 companion actions and controller
controls. The previous descriptions of bible anchors as surveyed geometry are
superseded: those proposed positions were not verified against installed assets.
`tools/test_dialogue_parser.py` protects speech extraction; authored revisions
live in `data/dialogue_edits.json`, applied by `parse_bible.py`.

Current release: see [campaign audit](docs/CAMPAIGN-AUDIT-UPDATE.md), [all playable mission flows](docs/PLAYABLE-MISSION-MAP.md), [progression and unlocks](docs/PROGRESSION-GUIDE.md), and [remaining mission treatments](docs/CAMPAIGN-REMAINDER.md).

## Shops and expanded vehicle travel
See [Market and travel](docs/MARKET-AND-TRAVEL-UPDATE.md) for story-gated weapon shopping, priced DLC extras, 13 clothing stores, compatible native vehicle upgrades and road/air/water speed tuning. See [Remaining features](docs/FEATURES-STILL-PLANNED.md) for the original plans still outstanding and recommended additions.
