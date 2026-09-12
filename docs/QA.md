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
| 42 | **Prologue room** | Fresh save; stop at the door; watch once, skip once; once with the room failing to load | Inside: cross, phone, three lines; skip lands the same; a failed room plays the call at the door with a notice; the cut to the dock follows |
| 43 | **Mateo's run** | Recognition, then watch the yard with guards alive; then clear it | He and the technician run in the open; no countdown; the launch leaves only after the last guard, and only once he is at the boat |
| 44 | **After the terminal** | Finish Gohan's copy last; finish it with a job still open | You stay on Gohan either way; "Ledger copied." and the objective line names the brother with the job; no automatic switch |
| 45 | **M02 opening and custody** | Start M02; breach; board with a wanted level | Curb scene, clock after it, van on the move; case in Ice's hand, stowed on boarding; canal refuses until the police are lost |
| 46 | **M03 junction** | Drive to Davis; roll through once; stop; sit; get out | No ejection at speed; nothing in the car; the hold starts on foot; dogs on arrival; the block after five seconds with a moment |
| 47 | **M03 depot rules** | As Ice, fire before the marker once; reach it quietly once; as Ron, fire at the junction during Ice's stage | Failure with the reason; the yard wakes on the marker; Ron's shots are not Ice's |
| 48 | **M03 loading and delivery** | Press E at the Benson's rear; skip once; deliver with stars, then without | Three crates carried in on camera, or in the bed on skip; no delivery with stars; the truck locks at the foundry |
| 49 | **Control after a hand-off** | Prologue into M01; Gohan's terminal into the recognition scene | The player can move as soon as gameplay starts or resumes |
| 50 | **M05 witness** | Stop Mateo; press E alongside; skip the account once | Ice comes down to the shore; Mateo climbs aboard and speaks over Gohan's shoulder; still aboard on skip; alive at the end |
| 51 | **M06 pickup** | Hold the alley through the first rotors as Ice; then take Guess | Ron's radio line; the Granger at the alley mouth by his own AI; a fire at the racks after the burn; boarding at the mouth; no wanted reset |
| 52 | **The room loads** | Enter the starter apartment; leave it | Inside with control within a few seconds; "was disabled"/"was enabled" and "entered" in the log; no timeout; put back on leaving |
| 53 | **Room map and survey** | Dev menu inside: "Map this room", then "Survey the room's spots" with F11 on each | `Bloodlines.Room.txt` beside the ini; next session each surveyed spot has its own marker and prompt |
| 54 | **Start from the menu** | Dev menu, Missions, any job, away from its marker | A short fade, the briefing at the job's marker, no second cut |
| 55 | **Helicopters stock** | Fly a helicopter with the crew deployed, then stood down | No difference; no "flight handling:" log line for it |
| 56 | **M07 clamp and helicopter** | Reach the mast, hold E; skip the clamp; watch the moment | A real case on the dish that survives the skip; the manifests read as a scene; the helicopter shown once, then Ron's radio |
| 57 | **M08 forks and stash** | Loop, clear, forklift to each crate, hold E; deliver | Each crate seen onto the bed and counted; the technical shown once; Ice aboard, Gohan in the Granger; the flatbed locked at the connector with both crates, still there after the mission |
| 58 | **M08 loop window** | Loop the cameras and wait it out | "Camera loop: Ns" on the HUD; the job fails when it drops |
| 59 | **M01 marks and escape** | Fight the yard | Red marks per hostile, gone as they drop; the escape plays the moment the last drops |
| 60 | **M02 boarding** | Take the drives, press E at the Granger's door | Seated; the drives stowed |
| 61 | **M03 order** | Arrive at the junction as Guess | Ambush fought as Guess; the switch asked only after; no guard in the truck; Gohan out after delivery |
| 62 | **M04 arrival** | Drive to the lot | Guess alone in the van; Ice and Gohan already in position |
| 63 | **Police sightings** | Grayed stars, drive past a patrol in view | The stars flash and they pursue; out of sight the search runs down |
| 64 | **M07/M08 placement** | Start each | Ice on the roof, the sedan on the street; the M08 yard spread out with the forklift in the open |
| 65 | **The meter** | Any hold or work | Name and a small yellow meter, no countdown text |
| 66 | **Three starter rooms** | Enter each brother's home | A studio, a one-bedroom, a house; nobody shares a layout |
| 67 | **The Diamond** | Preview the next residence past Eclipse, or finish M47 | Two floors, furnished; "Apartment: 16 entity set(s)" in the log |
| 68 | **M09 unit** | Take the driver; hold E at the cab; land on the flat | The colonel shown leaving; the unit in Ice's hand; Gohan's read; a landed pickup; a burned escort fails |
| 69 | **M10 stash to shop** | Start at the connector; deliver to Burro Heights | Both crates checked, the Buzzard shown, the window named, the truck stopped there by Ron's AI, the engines recorded at the shop |
| 70 | **M11 shop** | Start; dyno; Berth 44 | The shop mid-work instead of a briefing; the meter on the dyno; Gohan at the window; the Granger shown |
| 71 | **SM01 broker and crates** | Hold E at Sergei; then at the crates; drive home | Sergei alive and running; two crates into the car on camera; a dead Sergei fails; the reward said as the Mk II plus double rifle ammunition |
| 72 | **SM02 tap and trace** | Stun, tap, leave | The tap as a scene; camera archive named, not the dock recording; 75 s to the fire escape or the job fails |
| 73 | **SM03 grid and bay** | Watch KJ, race, ambush, deliver | KJ walks the prize; the guns shown once; the coupe and prize to the chop bay; $25,000 and the transmission named |
| 74 | **M12 survey** | Dive, scan, return | The scan as a scene; the launches shown once; "hullSurvey" recorded |
| 75 | **M13 basin** | Plant three, leave, board, trigger | The alarm launch shown; charges dark until aboard; the burn seen from the slipway; "harborPatrolsReduced" |
| 76 | **M14 pod** | Clear, take the plane, fly low, land | Ice and Gohan leave by road; the pod shown at McKenzie; "radarPod" recorded |
| 77 | **M15 splice** | Stun three, splice, leave | The splice as a scene; one gate answers; "harborGateAccess" recorded |
| 78 | **M16 lift** | Cross the pad, take the lift, canyon, land | The challenge on the net; Ice boards, Gohan by road; pursuit lost first; the lift scene and record |
| 79 | **M17 parts and handle** | Weld three, test, move the release | Three parts on the hull; the handle scene; the sub recorded ready |
| 80 | **M18 staging** | Deliver three, fit the pod, load | The plan scene; the pod on the lift; the roll call scene in seats; one clock recorded |
| 81 | **M19 breach** | Continue from M18, cut, clamp two, surface | The staged cut; the sub taken over at the channel; a float per clamp; the container surfaced as a scene beside the mark; sub and cargo recorded |
| 82 | **M20 hook** | Take off, clear the quay, hover, climb | The handoff cut; no duplicate lift, sub or container; the pod radio; the hook insert and heavy flight; the transfer scene with both in real seats |
| 83 | **M21 escort** | Board, escort, breakwater, shore | The escort cut; boats and gate per M13/M15 said and felt; the split; the road transfer scene into the Granger |
| 84 | **M22 bay** | Drop, land, regroup, replay | The arrivals cut; the drop insert; cargo recorded once; the strike learned from the phone and seen; the keys in Ron's hand then in the Granger; a replay leaves the ledger |
| 85 | **M23 sage** | Survey, clear, bays, generator, walk | The survey scene; cover on the approach; a subtitle per bay; the generator scene; the four limits; safehouse endpoint |
| 86 | **M24 gold** | Park, cable, ridge, board, deliver | The approach scene; cruisers by road, deputies out at the shore; the hoist scene; Gohan aboard before rolling; the unload scene; first portion recorded |
| 87 | **M25 canyon** | Deck, tanker, waves, jump, board | The approach scene with the boat running; the escape scene before the jump; Ron's radio on the way down |
| 88 | **M26 scramble** | Take off, kill, listen, kill, park | The approach scene with the parked Lazer and the Duster; the listen and the call sign; early kill fails; the park scene |
| 89 | **M27 flight** | Board, match, transfer, ledger, bail, boat | Ice in the Duster's second seat; the transfer cut; the case in hand; Ron home on his own route; the case in the boat |
| 90 | **M03 ambush** | Junction, yard, load, return, joint fight, ride | Truck on the road; four dogs attack; red markers on the block; the depot answers after loading; locked to Ron until close, then free switching; Ice rides with Ron |
| 91 | **M05 water** | Start, look at the boat and the crew | The dinghy floats with Ron and Gohan aboard; the generator crew at the cave mouth on the shore; no boat on the road; a refusal names the key |
| 92 | **Start cars** | Start M04 and M03, watch the van and Ron's car | Neither sinks through the ground; a held car is logged and released onto the ground |
| 93 | **M06 places** | Start, cut, burn, pickup | Panel and bench off the road; Ice's entrance at the surveyed point; Granger staged far off and safe until boarded; no trooper in a wall |
| 94 | **M07 roof** | Start, climb, clamp, jump | Ice on the roof, Gohan at the base with the laptop; a notice if the roof estimate is wrong; the Buzzard fires |
| 95 | **M08 forks** | Stop at each pad, sentries | Crate on the forks after a second stopped, no button; crates do not topple; sentries apart and alerted |
| 99 | **M03 car on the ground** | Start M03 | The Primo on the lot; a log line if it was lifted |
| 100 | **Yellow route on the radar** | Any mission, one destination | The route line on the radar while playing, same as on the pause map |
| 98 | **Ron's home** | Fresh save, prologue | The drive home ends at the Mission Row apartment door; the marker is there; the house interior is behind it; exit returns to Mission Row |
| 97 | **The Port Heist seamless** | M19 through M22 in one run | No scene or wait at the three joins; the escort, the Granger and the beach arrival crewed under AI while you play; the heist restarts at M19 on fail, abort or quit; the harbor sites resolve on real water |
| 96 | **M07 roof check, M08 jobs** | Start M07 with the roof key wrong; M08's second crate | M07 always starts: the nearest roof with a notice, or street level with the key named, never Ice in the air; in M08 the technical counts for whoever kills it and the HUD never demands Ron for it |
| 101 | **Harbor starts** | Start M18, M19, M20 | Each starts at Terminal Island; "open water assumed" in the log is fine; a refusal names a deck or shallows |
| 102 | **M03 Gohan in the back** | Run it home | Ice in the cab, Gohan into the box at the rear doors, doors closed, both required before the run; Gohan set down at the foundry |

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
