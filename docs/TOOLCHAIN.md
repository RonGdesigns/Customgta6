# Toolchain

Section 5 of the design bible maps onto real tools like this.

| Layer | Tool | What it actually does here | State |
|---|---|---|---|
| Mission engine | **ScriptHookVDotNet 3** (C#) | everything in `src/Bloodlines` — switching, missions, abilities, HUD, fail conditions | in use |
| Interiors & nodes | **CodeWalker** | opening dock warehouses, penthouses and hangars as MLO interiors; placing AI cover and navmesh | not started |
| Entities & models | **OpenIV** | injecting custom ped models, liveries and weapon loads into `mods/update.rpf` | not started — the mod currently uses stock peds |
| Prototyping | **Map Editor / Menyoo** | fast route plotting and prop placement, exported and hand-carried into script data | optional |
| Cutscenes | scripted cameras + `PLAY_SYNCHRONIZED_*` | there is no usable cutscene *authoring* tool; cinematics are hand-built from camera interpolation and synced scene anims | not started |
| Dialogue | `tools/generate_voice.py` → WAV | 255 written lines batch-generated through ElevenLabs (bible Track 3) and played by `DialogueDirector`; subtitles work with no audio at all | tool ready, no lines generated |
| Ambient audio | OpenIV + custom `.awc` | in-world sound beyond dialogue | not started |

## Ped models

The bible names three stock gang peds, and the code uses them as-is so the mod runs
on an unmodified install:

| Character | Model | Note |
|---|---|---|
| Darius "Ice" Vance | `g_m_y_famca_01` | Families gang member, locs |
| Devin "Gohan" Mercer | `g_m_y_famdnf_01` | Families gang member, low fade |
| Ron "Guess" Ortiz | `g_m_y_ballaeast_01` | Ballas gang member, bald variant |

Swapping any of these for a custom rigged ped is a one-line change in
`Crew/Protagonist.cs` once the model is injected via OpenIV. Custom heads are the
right call eventually — a story built on three specific faces should not be wearing
ambient gang models — but nothing in the code depends on that happening first.

## The bible pipeline

`tools/parse_bible.py` reads a bible PDF and writes `data/*.tsv`;
`tools/render_campaign_doc.py` regenerates `docs/CAMPAIGN.md` from it;
`tools/generate_voice.py` turns `data/dialogue.tsv` into a voice pack. All three are
dependency-free Python 3 — no pip install, no venv — because a content pipeline that
breaks when a machine changes is a pipeline nobody re-runs.

```bash
python3 tools/parse_bible.py docs/bibles/omnibus_v2.pdf docs/bibles/solo_missions_v1.pdf
python3 tools/render_campaign_doc.py
python3 tools/generate_voice.py --mission M01 --dry-run
```

Pass every bible in one invocation — the parser merges them, normalizes the cast
names, and warns if any of the 70 main slots came back empty.

## Order of work that actually converges

1. **Vertical slice.** One mission, played until it is fun. (M01, M02 and the solo
   mission SM01 are drafted; none has been tuned in game. `docs/QA.md` is the audit
   protocol to run when you do.)
2. **Framework hardening.** Checkpoints, mid-mission saves, replay, mission fail
   flows — cheaper to fix at mission 1 than at mission 20.
3. **Interiors.** MLOs gate several bible missions (evidence depot, penthouses, the
   oil platform). Long lead time; start early.
4. **Missions in batches**, by set piece type — all vehicle escorts, then all
   breaches — because reusable mission components are where the schedule is won.
5. **Characters and audio last**, once the beats are locked.

Realistically: 70 missions of this scale is a multi-year effort for a team, and the
biggest costs are interiors, cutscenes and voice, not mission logic. The
architecture here is built so mission 2 costs a fraction of mission 1.
