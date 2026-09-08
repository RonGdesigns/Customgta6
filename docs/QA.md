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
| 8 | **Coordinate survey** | dev menu → Survey, then `F11` at each beat | The survey teleports you to each estimate in turn; `F11` captures where you stand, `End` skips, `Home` goes back. It writes `Bloodlines.Surveyed.ini` after every capture — rename it over `Bloodlines.Locations.ini` when done |

| 9 | **Save state** | Finish M01, alt-F4, relaunch | `savegame.json` lists M01 in `completedMissions`, `currentMissionId` has moved on, and `J` offers the next mission rather than M01 again |
| 10 | **Prerequisites** | Press `J` on a fresh save | M01 comes first; SM01 is not offered until M03 is complete |
| 11 | **Fleet upgrade** | Set `grangerTurbineInstalled` true in `savegame.json`, get into a Granger | The turbine notification fires once, the vehicle pulls noticeably harder, and re-entering does not re-apply it |
| 12 | **Audio partitioning** | Put a cue in `audio/Act1/M01/` and another in flat `audio/` | Both play; the partitioned one wins when both exist |

| 13 | **Dev menu** | `F8` with `[Dev] Enabled = True` | Menu opens, arrows navigate, Enter starts a mission from any point, Backspace closes, and the player cannot fire while it is open |

## Before you launch: the static checks

Three things run without the game and should be clean before any play session —
they are also wired into CI:

```bash
python3 tools/lint_missions.py       # location keys, cue ids, model names, registration
python3 tools/validate_locations.py  # every coordinate against real zone boundaries
dotnet build src/Bloodlines/Bloodlines.csproj -c Release --warnaserror
```

The linter exists because a mission is held together by string literals that all
compile perfectly and then fail silently: a typo'd model spawns nothing, a wrong cue
id logs a warning nobody reads, a bad location key puts a marker at the map origin.
It has already caught a plane model that does not exist (`mallard`; the Mallard's
model is `stunt`) which would have made M27 unfinishable, and four missions that
deployed the crew over water or, in one case, seven hundred metres up.

## What to log

`Bloodlines.log` is written next to the inis. Set `[Dev] VerboseLogging = True`
before a QA pass — it adds stage transitions, cue playback and ability toggles,
which is what makes a bug report reproducible.

## Known unknowns

Every coordinate outside the bible's Track 2 index is an estimate. Audit 8 is
therefore not optional before judging any mission's pacing: a marker in the wrong
place reads as bad mission design when it is really bad surveying.
