# In-engine QA protocol

Action Step 4 of the implementation toolkit, as a checklist you can actually run
with this build's keys. Everything here needs `[Dev] Enabled = True` in
`Bloodlines.ini`; none of it can be verified outside the game.

| # | Audit phase | How to run it | Pass criteria |
|---|---|---|---|
| 1 | **Model hashes** | `F10` to deploy the crew in free roam | All three peds render as `g_m_y_famca_01`, `g_m_y_famdnf_01`, `g_m_y_ballaeast_01` with no texture corruption, each with the right blip colour |
| 2 | **Audio pipeline** | Generate M01's lines (`tools/generate_voice.py --mission M01`), drop them in `scripts/Bloodlines/audio/`, start M01 | WAVs play cleanly, subtitles carry the speaker's colour (Ice `~b~`, Gohan `~g~`, Guess `~o~`), and lines never overlap |
| 3 | **Sky-cam switch** | `Numpad 1/2/3` during a deployment | Camera lifts and descends into the target ped without hitching or clipping; the ped you left keeps its weapons and starts fighting |
| 4 | **Companion leash** | Drive >180m from the crew | Companions reposition on the active character rather than pathfinding across the map; the leash distance is `CompanionLeashDistance` |
| 5 | **Checkpoint engine** | `Insert` to commit, then die or press `Delete` on M01 stage 2 | Positions, health, armor and wanted level restore; wrecked vehicles within 220m are purged; the mission resumes at the committed stage |
| 6 | **Story character** | `F10` to deploy, `F10` again to stand down | The character you were playing before deployment comes back — same ped, same position, not a gang model |
| 7 | **Solo lock** | Start SM01, press `Numpad 2` | The switch is refused with a subtitle; only Ice is on the map |
| 8 | **Coordinate survey** | `F11` at each mission beat | Captured positions land in `Bloodlines.Captures.ini` and match the geometry the mission assumes |

## What to log

`Bloodlines.log` is written next to the inis. Set `[Dev] VerboseLogging = True`
before a QA pass — it adds stage transitions, cue playback and ability toggles,
which is what makes a bug report reproducible.

## Known unknowns

Every coordinate outside the bible's Track 2 index is an estimate. Audit 8 is
therefore not optional before judging any mission's pacing: a marker in the wrong
place reads as bad mission design when it is really bad surveying.
