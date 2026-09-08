# Toolchain

Section 5 of the design bible maps onto real tools like this.

| Layer | Tool | What it actually does here | State |
|---|---|---|---|
| Mission engine | **ScriptHookVDotNet 3** (C#) | everything in `src/Bloodlines` — switching, missions, abilities, HUD, fail conditions | in use |
| Interiors & nodes | **CodeWalker** | opening dock warehouses, penthouses and hangars as MLO interiors; placing AI cover and navmesh | not started |
| Entities & models | **OpenIV** | injecting custom ped models, liveries and weapon loads into `mods/update.rpf` | not started — the mod currently uses stock peds |
| Prototyping | **Map Editor / Menyoo** | fast route plotting and prop placement, exported and hand-carried into script data | optional |
| Cutscenes | scripted cameras + `PLAY_SYNCHRONIZED_*` | there is no usable cutscene *authoring* tool; cinematics are hand-built from camera interpolation and synced scene anims | not started |
| Audio | OpenIV + custom `.awc` | dialogue is the single most expensive unshipped piece of a campaign this size | not started |

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

## Order of work that actually converges

1. **Vertical slice.** One mission, played until it is fun. (M01 is drafted; it has
   not been tuned in game.)
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
