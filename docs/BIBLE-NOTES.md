# Reading notes on the bibles

Things worth knowing when the data files disagree with your memory of the PDFs, or
when the next revision arrives. Nothing here is a complaint about the writing —
these are the places where a script has to make a call, and it should be your call
rather than mine.

## The cast

Confirmed by the author, and now the only naming used anywhere in the code, the
data or the docs:

| Handle | Name | Role | Model |
|---|---|---|---|
| **Ice** | Darius Vance | The Tactician — heavy assault | `mp_m_freemode_01` (Ice appearance) |
| **Gohan** | Devin Mercer | The Inside Man — breaker | `mp_m_freemode_01` (Gohan appearance) |
| **Guess** | Ron Ortiz | The Wheelman — hotfoot | `mp_m_freemode_01` (Guess appearance) |

M62's prose in the omnibus used "Jules" and "Malik" for Gohan and Guess — leftovers
from an earlier draft. `tools/parse_bible.py` normalizes those to the current names
during extraction (see `NAME_ALIASES`), so the data files, the campaign doc and the
voice pack all carry one cast. If a future revision introduces a genuine rename,
change that table rather than the generated files.

## The four source documents

| Document | What it contributes | Used |
|---|---|---|
| `design_bible_v1.pdf` | original concept, three parts, the first switch listing | superseded, kept for history |
| `omnibus_v2.pdf` | all 70 missions, 255 cues, the engine architecture, Track 2 coordinates, voice track | **primary source** |
| `solo_missions_v1.pdf` | 9 solo missions (SM01–SM09), 37 cues, their insertion windows | **primary source** |
| `implementation_toolkit.pdf` | directory staging, config spec, M01 vertical-slice listing, QA audit protocol | absorbed into config defaults, M01 and `docs/QA.md` |

## Where the bibles differ

| Topic | v1 Design Bible | v2 Omnibus | What the code follows |
|---|---|---|---|
| Structure | 3 parts, named beats only | 70 missions, each with setting, objective and dialogue | v2 |
| M01 target | unnamed "cartel capo" | **Mateo Cifuentes**, escaping on an armored launch | v2 |
| M01 geography | "Terminal Island", no coordinates | crane **#4**, superyacht **The Mariana**, warehouse **bay 2**, plus surveyed coordinates | v2 |
| Ice's ability | stagger, penetration, recoil | stagger reduction, penetration | both — implemented as all three |
| Interstitials | not covered | safehouses, workbenches, ambient cycles, Weazel News | Exterior homes, basic workbenches, independent trips and mission follow-ups implemented; custom interiors and advanced crafting remain pending |

## Deliberate deviations from the toolkit

1. **One assembly, not five.** The toolkit and the scaling plan stage
   `BloodlinesCore.dll`, `BloodlinesLauncher.dll` and a DLL per act. This ships a
   single `Bloodlines.dll`.

   The problem those plans are solving is real: SHVDN instantiates and ticks every
   class deriving from `Script` in `scripts/` **and its subfolders**, so 79 mission
   scripts would mean 79 tickers. But splitting into assemblies does not fix that —
   SHVDN would still find and tick every `Script` subclass in every one of those
   DLLs. What fixes it is missions not being `Script` subclasses at all: they are
   ordinary classes the dispatcher constructs on demand, so exactly one mission
   ticks, and it costs 70 assemblies of version-matching less.

   The extensibility the plan wants is supported anyway: fill in `assembly` and
   `class_name` for a mission in `missions.tsv` and the dispatcher loads it from
   `scripts/Bloodlines/missions/` by reflection.
2. **Config lives in `scripts/Bloodlines/`,** not a loose `BloodlinesConfig.ini` in
   `scripts/`. Same keys, same meanings, but the mod keeps its files together —
   including the data folder, the save and the log.

   The registry is `missions.tsv` rather than `campaign_registry.json` for the same
   reason as the other data files: no JSON reader ships with .NET Framework 4.8.
   `campaign_registry.json` is still generated, from the same pass, for tooling that
   wants it. `savegame.json` *is* JSON as specified — that one needed nesting, so the
   mod carries a small purpose-built reader (`Core/Json.cs`).
3. **Guess's subtitle color is orange, not red.** The toolkit's audit lists
   `~r~ ~b~ ~g~`; red is reserved here for antagonists (Mateo, Sergei, Sterling,
   Vance), so all three protagonists stay visually distinct from the people shooting
   at them.
4. **Dialogue is queued rather than `Script.Wait`-ed.** The toolkit's listing waits
   3.5 seconds between lines inside the tick, which freezes the mission's own logic
   while people talk. `DialogueDirector` queues instead, so the firefight continues
   underneath the conversation.

## How the Red-tier set pieces were actually faked

The first one is built, so the technique is no longer theoretical. M20's thirty-ton
container airlift attaches the container to the Cargobob and drops the aircraft's
engine power to 55%, rather than slinging a weighted load the physics would have to
solve. Everything the player does — the hover, the suppression, the climb-out — is
real; the only faked part is the one the engine cannot do. M22's cruise-missile
strike on the foundry is a fade, two detonations at the foundry's real coordinates
and a smoke column, because what has to land is the loss, not the ordnance.

That is the pattern for the six Reds still ahead: find the one thing the engine
cannot do, fake exactly that, and leave everything the player touches real.

## Open questions

1. **Act III's header sits above M48.** M48 is labeled "Act II Finale" in its own
   header, so the code splits acts by number (I: 1–22, II: 23–48, III: 49–70), which
   matches both bibles' stated ranges.
3. **Two different scores.** M11 says Berth 44 holds "$3B in cartel bearer bonds and
   gold"; M46's rig vault is "$500M in bearer bonds". Read as two separate hauls —
   the port gold and the rig bonds — which is how the missions treat them.
4. **Solo mission coordinates are unsurveyed.** The expansion names locations
   ("Terminal Island Warehouse 4", "Lifeinvader Data Annex") but gives no
   coordinates — Track 2 only covers the prologue. SM01's positions are estimates in
   `Bloodlines.Locations.ini` and need an `F11` survey pass.
5. **Ability meter economics are unspecified.** Duration and recharge are currently
   8s / 25% per second, shared across all three characters, configurable in the ini.
   The shared pool is a design decision, not something the bible asks for: it makes
   the switch a resource choice. Say the word if each character should carry their own.
6. **Stage indices.** Cue ids carry a stage (`M01_S2_05_ICE` = stage 2). Missions in
   code use the same numbering, so stage 2 in the script fires the S2 block. Where a
   mission's cue table skips a stage number, the script keeps counting.

## What the data files contain

Generated by `tools/parse_bible.py` from the omnibus and the solo expansion:

- `data/missions.tsv` — 79 rows: id, number, kind (main/solo), owner, insert-after,
  title, act, location, time, weather, HUD objective, synopsis.
- `data/dialogue.tsv` — 292 rows: cue id, mission, stage, speaker, stage direction,
  line, trigger event.
- `data/anchors.tsv` — 6 rows: the bible's surveyed coordinate index (Track 2).
  These are the only coordinates in the project that were authored against the real
  map; everything else in `Bloodlines.Locations.ini` is an estimate to be surveyed.

Re-run the parser after any bible revision — it takes every PDF at once:

```bash
python3 tools/parse_bible.py docs/bibles/omnibus_v2.pdf docs/bibles/solo_missions_v1.pdf
python3 tools/render_campaign_doc.py
```

The extracted plain text of both PDFs is committed alongside them so a diff between
revisions is readable.
