# In-engine QA protocol

Action Step 4 of the implementation toolkit, as a checklist you can actually run
with this build's keys. Everything here needs `[Dev] Enabled = True` in
`Bloodlines.ini`; none of it can be verified outside the game.

| # | Audit phase | How to run it | Pass criteria |
|---|---|---|---|
| 1 | **Model hashes** | `F10` to deploy the crew in free roam | All three peds render as `g_m_y_famca_01`, `g_m_y_famdnf_01`, `g_m_y_ballaeast_01` with no texture corruption, each with the right blip color |
| 2 | **Audio pipeline** | Generate M01's lines (`tools/generate_voice.py --mission M01`), drop them in `scripts/Bloodlines/audio/`, start M01 | WAVs play cleanly, subtitles carry the speaker's color (Ice `~b~`, Gohan `~g~`, Guess `~o~`), and lines never overlap |
| 3 | **Sky-cam switch** | `Numpad 1/2/3` during a deployment | Camera lifts and descends into the target ped without hitching or clipping; the ped you left keeps its weapons and starts fighting |
| 4 | **Companion leash** | Drive >180m from the crew | Companions reposition on the active character rather than pathfinding across the map; the leash distance is `CompanionLeashDistance` |
| 5 | **Checkpoint guard** | `[` then `]` in a current mission (not Insert: that is ScriptHookVDotNet's reload-all-scripts key) | A clear full-restart message; no false restore of destroyed vehicles or stale objectives |
| 5b | **Death recovery** | Die as each brother, including with an active ability | Visible screen, normal time, live controllable player, mission fails once and can be retried from the beginning |
| 5c | **Arrest** | Let police arrest a deployed brother | Recovery clears the arrest; no permanent loss of controls |
| 5d | **Death handed back** | Stand the crew down (`F10`), then die as your own story character | Vanilla Wasted screen and hospital respawn, exactly as without the mod. If dying does nothing, `ReleaseSuppression` did not run and that is a blocker |
| 6 | **Story character** | `F10` to deploy, `F10` again to stand down | The character you were playing before deployment comes back — same ped, same position, not a gang model |
| 7 | **Solo lock** | Start SM01, press `Numpad 2` | The switch is refused with a subtitle; only Ice is on the map |
| 8 | **Survey** | F8 > Survey; drive the yellow GPS route or F7 teleport; F11 capture on foot | Marker visible at any distance; saved coordinate survives reload and affects M01 spawning; End skips, Home returns |

| 9 | **Save state** | Finish M01, alt-F4, relaunch | `savegame.json` lists M01 in `completedMissions`, `currentMissionId` has moved on, and `J` offers the next mission rather than M01 again |
| 10 | **Prerequisites** | Press `J` on a fresh save | M01 comes first; SM01 is not offered until M03 is complete |
| 11 | **Fleet upgrade** | Set `grangerTurbineInstalled` true in `savegame.json`, get into a Granger | The turbine notification fires once, the vehicle pulls noticeably harder, and re-entering does not re-apply it |
| 12 | **Audio partitioning** | Put a cue in `audio/Act1/M01/` and another in flat `audio/` | Both play; the partitioned one wins when both exist |

| 13 | **Dev menu** | `F8` with `[Dev] Enabled = True` | Menu opens, arrows navigate, Enter starts a mission from any point, Backspace closes, and the player cannot fire while it is open |
| 14 | **Prologue watch vs skip** | Fresh save, mission key; once watching every line, once pressing Enter at the first line of each scene | Both runs end with Ron seated for the drive and standing at his door afterward; the save shows `prologueComplete: true`; M01's cold open follows the fade |
| 15 | **End of content** | Mark all 36 jobs complete in the dev menu, press the mission key | A completion message names the state; M01 is not offered |
| 16 | **Port Heist continuity** | M19 → M22 in one session, then M21 alone in a fresh session | M20 starts Gohan in the Kraken; M21's Cargobob carries a visible container in both cases; `Bloodlines.log` shows `Handoff recorded` / `Handoff consumed` |
| 17 | **Story gate** | Finish M18 with SM01–SM03 unfinished, press the mission key at M19's marker | The three jobs are named; M19 does not start; Gohan's lead routes to SM01; a save already past M19 shows no gate |
| 18 | **Weapon loan** | Start M20, abort, open Guess's weapon wheel and the save's `weaponLockers` | No MG on Guess or in the locker; his pre-mission guns and starting rifle are all still there |
| 19 | **Skip vs cancel** | Enter during the prologue arrival, then on a fresh save die during the homecoming | Enter: Ron seated. Death: no warp, recovery runs, the drive resumes |
| 20 | **Briefing cast** | Dev tools off, crew not deployed, start M03 | Both speakers stand facing the start point; the story character is hidden for the scene and visible with control afterward |
| 21 | **Control line** | Finish any scene, read `Bloodlines.log` | A `CONTROL [scene ended: ...]` line and a `CONTROL [gameplay begins]` line with control=True |
| 22 | **Road loop** | `HANDLING-CALIBRATION.md` loop, four cars, ability off, then `F10` off | Sheet filled per car; after stand-down the car drives stock and the log has no restore errors |
| 23 | **Guess press** | Same loop with the ability through the freeway curves; end it mid-corner; take the ramp with it on | No drop at the end; the car still leaves the ground on the ramp; a same-model car nearby returns to baseline within a second |
| 24 | **Briefing arrival** | Crew not deployed, mission key at M04's marker; once watching, once pressing Enter during the drive | The four-door pulls up beside Guess before the first line; the skip lands it at the curb; the story character is hidden during and visible after |
| 25 | **M04 chase and pickup** | Play M04 through | Miller runs at speed and ignores lights; Guess reaches into the car for the drive; both cars remain after the pass |
| 26 | **M06 air waves** | Play M06 through the siege | Waves two and three: two Mavericks each, ropes over the alley, one short shot on the first pilot only; the wave ends even if an aircraft never settles |
| 27 | **Apartment** | Enter the starter apartment from its door | Inside with control within a few seconds; no "Apartment timeout" in the log |
| 28 | **Rewards** | Finish M02, restock at the locker, open the shop | Three new weapons, one per hero; the shop lists each as locked until its job |
| 29 | **QA order** | Dev menu → Missions → a job whose prerequisite is unfinished | It starts with a QA note; the mission key on its marker still refuses |
| 30 | **Visuals restore** | Deploy, drive at 13:00 and 23:00, `F10` off, press Insert | Grade changes per band with a log line each; after stand-down and reload the world is vanilla |
| 31 | **Crew protection** | Enter and leave a car with the crew deployed, then stand down and crash the story character's car | Damage-scale calls follow entry and exit in the log; vanilla damage after stand-down |
| 32 | **Ini safety** | Copy the packaged `scripts` folder over the install by hand | `Bloodlines.ini` unchanged; `Bloodlines.ini.example` beside it |
| 33 | **Prologue to M01** | Fresh save, J, watch both scenes | The cold open starts after the time-cut with no "could not find a loaded walkable surface" line in the log |
| 34 | **Port Heist** | Start M19 from its marker, pass it | A hull with a "Titan Star" blip over the breach; all three marks reachable in the Kraken; M20 starts on its own after the aftermath, then M21, then M22 |
| 35 | **Arrival car** | Fresh save, J | The Primo is at the curb on the street, not on the terminal roof |
| 36 | **Crew van** | Deploy near the Cypress base, modify the van at a shop, stand down, redeploy, start M02 | The van is at the stash with a blue blip; the mods and paint persist; M02's chase car is the van |
| 37 | **M04 comprehension** | A tester who has not read the books plays M04 once, then answers the seven questions in live-test item 50 | All seven answered from what was shown |
| 38 | **M04 switch window** | Let the eight seconds run out once; take Guess early once | Required switch through the prompt with nobody frozen; early switch hands over cleanly |
| 39 | **M04 skip and retry** | Skip the briefing; fail and retry | Four-line card for seven seconds; one-line recap on the retry |
| 40 | **M01 escape** | Clear the guards; once watching, once pressing Enter | Mateo boards at the slipway on camera and the launch leaves; skipped, he is aboard and the launch leaves; the mission never fails on Mateo |
| 41 | **Companion shield** | Firefight as one brother while the other two are in it | Their health does not drop; switching into one makes him mortal |

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
deployed the crew over water or, in one case, seven hundred meters up.

## What to log

`Bloodlines.log` is written next to the inis. Set `[Dev] VerboseLogging = True`
before a QA pass — it adds stage transitions, cue playback and ability toggles,
which is what makes a bug report reproducible.

## Known unknowns

Every coordinate outside the bible's Track 2 index is an estimate. Audit 8 is
therefore not optional before judging any mission's pacing: a marker in the wrong
place reads as bad mission design when it is really bad surveying.

## Runtime regression checks

`python tools/run_regression_tests.py` runs source-level checks with GTA stand-ins.
Test normal companion door animations with a stopped four-door car, distinct seats,
combat response to player attackers, and delayed fallback for failed entry in game.
