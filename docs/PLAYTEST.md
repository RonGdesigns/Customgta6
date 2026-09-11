# Playtest script — first session

> **Current entry point (September 9, 2026).** The steps below this note are the
> original first-session script and are kept as history: they describe thirty
> missions, Numpad switching, an Ice-first deployment, a crane at 42 m, a yacht
> bilge and a T20, none of which match the build. For the build you have:
>
> - Read `HANDOFF-IMPLEMENTATION-UPDATE.md` (live-test list) and `QA.md` (harness table).
> - A fresh save opens on Ron's arrival: press `J` as your story character; do not
>   use the F8 menu, which starts M01 directly and skips the prologue by design.
> - Test the story path once with `[Dev] Enabled = False`: before M01, `F10` then
>   deploys Ron alone. With dev tools on it deploys the trio, which is QA behavior.
> - The QA checkpoint keys are `[` and `]`, not Insert/Delete.
> - Your `scripts\Bloodlines\Bloodlines.Locations.ini` from before September 9
>   carried template positions; the mod now ignores those exact values and says so
>   in `Bloodlines.log` ("stale template override").
> - The dev coordinate readout is at the top right (with speed in a car). Map icons
>   are named by mission id. The dev menu's "Running mission" page has "Complete
>   current objective". Driving: run the loop in `HANDLING-CALIBRATION.md` before judging
>   the speed target; the handling baseline and Guess's press are opening values.
> - Round two (September 10): briefings drive up to Guess, M04's Miller runs, M06's
>   later SWAT waves rappel from Mavericks, the apartment loads from inside the room,
>   most jobs pay a weapon, and the dev menu starts any job. Live-test items 31–38.
> - Visuals and vehicle damage are on by default (`[Visuals]`, `[VehicleDamage]`);
>   `VISUALS.md` says what each key costs and how to check a modifier name. The
>   package now ships the ini files as `.example`, so an install cannot replace yours.
> - The crew's Granger lives at the stash beside the Cypress base and is the van the
>   missions use; what you do to it at a shop is saved. The Port Heist runs M19–M22
>   as one operation; `OPERATIONS.md` lists the others.
> - M04 now starts at the Cypress base marker and is the reference for the story-to-play
>   work: drive in, see the sale, cut the power, take the wheel when offered, recover
>   the drive, lose the police. Live-test items 50–55.
> - The opening is the second slice: the message is read inside the starter
>   apartment; in M01 Mateo runs for the launch in the open with no clock, Gohan's
>   copy leaves you on Gohan, the laptop is on the table; M02 opens at the curb,
>   the drives ride in Ice's hand and the canal does not clear the police.
>   Live-test items 58–64.
> - M03 is the third slice: the split at the base, Ron's junction with its dogs and
>   street crew, Ice's quiet entry that wakes the yard, Gohan's crates carried into
>   the Benson on camera, the truck home with the police lost first and locked at
>   the foundry. Live-test items 65–70.
> - M05 and M06 are the fourth slice: the cove seen from the cliff and the boat, Ice
>   coming down, Mateo aboard for the questioning; the depot's three positions, a real
>   fire at the racks, and the rotors turning Ron's wait into a pickup at the alley
>   mouth. Also the control fix: you can move after the prologue and after Gohan's
>   terminal. Live-test items 71–74.
> - The starter room: it loads now (Story Mode had the Online room switched off), it
>   has named spots that appear once you survey them inside, and the dev menu can
>   write a floor map of it. A start from the mission menu begins at the job's
>   marker. Live-test items 75–78.

Thirty missions are written and none have been played. This is the script for the
first hour at the machine, in the order that finds the most for the least time.

The goal of session one is **not** to enjoy M01. It is to find out which of four
things is broken — the mod loading, the switch, companion AI, or the coordinates —
because almost every other bug in the campaign will be one of those four wearing a
different mission's clothes.

Budget: ~20 minutes of setup and smoke tests, ~25 minutes on M01, ~15 minutes of
survey. Stop when you have findings; do not push through a broken build.

---

## 0. Before you launch (5 min)

```bat
python tools\lint_missions.py        :: must print "No errors."
python tools\validate_locations.py   :: must print "0 flagged"
python tools\package.py --clean      :: no .NET SDK needed; add --build only if you have one
```

Confirm the game folder before touching it — read-only:

```bat
tools\windows\check-setup.bat "C:\Program Files\Rockstar Games\Grand Theft Auto V Enhanced"
```

It must report a build, `ScriptHookV: yes`, `ScriptHookVDotNet.asi: yes` and
`SHVDN v3 API: yes`. If any of those say NO, stop — nothing below can work, and
the hook has to match the build (Legacy vs Enhanced) it sits next to.

Then install and set up the config:

```bat
tools\windows\install-bloodlines.bat "C:\Program Files\Rockstar Games\Grand Theft Auto V Enhanced"
```

In `scripts\Bloodlines\Bloodlines.ini`:

```ini
[Dev]
Enabled = True
VerboseLogging = True
```

Verbose logging is the difference between a useful report and "it didn't work".

**Launch in Story Mode.** Not Online. Ever.

---

## 1. Does it load at all? (2 min)

Open `scripts/Bloodlines/Bloodlines.log`. You are looking for, in order:

```
Los Santos: Bloodlines loading.
Loaded 121 rows from locations.tsv
Loaded 79 rows from missions.tsv
Loaded 292 rows from dialogue.tsv
Catalog built: 70 main + 9 solo, 30 playable.
Ready. 0/79 complete.
```

| What you see | What it means |
|---|---|
| No log file at all | SHVDN did not load the DLL. Check `ScriptHookVDotNet3.dll` version against the game build. |
| "No campaign data" on screen | `data/` did not get copied into `scripts/Bloodlines/data/`. |
| Row counts lower than above | A data file is truncated or stale — re-run `package.py`. |
| Loads, then an exception in SHVDN's own log | Copy the stack trace verbatim; that is the highest-value thing in the session. |

---

## 2. Smoke test before any mission (10 min)

Do these in free roam, in this order. Each one isolates a system that every mission
depends on. **If one fails, stop and report it — do not start M01.**

| # | Do this | Expect | If it fails |
|---|---|---|---|
| 1 | `F10` | Three peds spawn, you are Ice, two blips (green Gohan, orange Guess) | Model streaming — check the log for "Could not stream" |
| 2 | `Numpad 2` | Sky-cam swoops, you are Gohan, Ice becomes a blue-blipped companion | **The single most important test in this session.** Anything wrong here affects all 30 missions |
| 3 | Switch back and forth 3–4 times | Weapons, health and position persist per character; no ped duplicates left standing | Duplicate peds means `CHANGE_PLAYER_PED` is not taking |
| 4 | `Caps Lock` | Ability HUD bar drains; Gohan's Thermal Pulse shows see-through | Ability meter or `SET_SEETHROUGH` |
| 5 | Steal a car, drive off | **Companions board it within a few seconds — walking, or warped if you're moving** | This is the failure that ends missions. Log line: "Companion missed the boarding window" |
| 6 | Drive 300m away fast | Companions reposition on you rather than jogging forever | Leash — `CompanionLeashDistance` is 180 |
| 7 | `F10` again | You are back as your *original* story character, where you left them | Story-character restore. If you are still a gang ped, say so — that is a save-integrity bug |
| 8 | `F8` | Dev menu opens; arrows move; `Backspace` closes | — |

---

## 3. M01 — Ghost in the Dockyard (25 min)

Start it from the dev menu (`F8` → Missions → M01) rather than `J`, so you can
restart quickly.

M01 is the only mission built on the bible's **surveyed** coordinates, so if
positions are wrong *here*, they are wrong everywhere.

### Stage 0 — three approaches

The crew deploys split: Ice on the crane at `(978.25, -2988.42, 42.15)`, Gohan at
the yacht bilge `(962.11, -3035.80, -0.45)`, Guess by the T20 in bay 2
`(1015.60, -3002.15, 5.90)`.

| Check | Looks right | Looks wrong |
|---|---|---|
| Ice's spawn | Standing on a crane catwalk with sightlines over the dock | Inside geometry, on the ground, or falling |
| **Is there a yacht?** | Gohan is on/in a vessel at the bilge point | Gohan is treading water in open dock — **the bible assumes a superyacht that story mode may not place there.** Flag this; it changes the mission's design, not just its numbers |
| Guess's T20 | Black T20 sitting in a warehouse bay | Half-inside a wall, or on the roof |
| Aim at Mateo as Ice | Objective completes when you scope him within 260m | If it never completes, check line of sight, then `Page Up` to skip and note it |
| Ledger rip | Stand at the bilge point 8 seconds, counter on screen | Counter resets = you are drifting outside the 2.5m radius |
| Prototype | Get in the T20, stage advances | — |

**The thing to judge, not just check:** do the three jobs make you *want* to switch,
or do you just do them in the order the objective line tells you? That question is
the whole design of the campaign.

### Stage 1 — the recognition

Fade, all three teleport to the regroup point, the launch spawns, 8 cartel guards
spawn, four seconds of stillness, then they engage.

- Does the fade cover the teleport, or do you see people snap?
- Do all three arrive standing, not stacked or clipping?
- Guards spawn at four toolkit-authored posts plus a ring — is anyone spawning
  inside a container?

### Stage 2 — the firefight

A 90-second window; Mateo runs for the Tropic when it expires. Stage ends when
2 or fewer guards remain, or 20 seconds past the window.

- **Pacing question:** is 90 seconds long enough to feel like a fight and short
  enough not to drag? Write down what it actually felt like.
- Companions fight rather than standing around?
- Mateo actually boards and drives away, rather than swimming or T-posing?

### Stage 3 — the exfil

Reach `(720.50, -2400.10, 15.20)` — the toolkit's Cypress drainage tunnel — with
all three crew within 50m. Three stars.

- **That is roughly 2.5 km from the dock.** Is that drive a good ending or dead
  time? This is the single most likely pacing problem in the mission.
- Do the companions get in the car with you? (Smoke test 5, under real pressure.)
- Does the "wait for the others — 2/3 clear" message resolve, or do you sit there?

### Then break it on purpose

- Die at each M01 stage, with an ability active and with/without a destroyed T20.
  Recovery should return a living controllable character to the pre-deployment area,
  fail the mission once, and let you retry from the beginning. Current missions do
  not support a complete checkpoint restore; Delete explains that restriction.
- Test arrest, death during a character switch, and repeated free-roam deaths.
- Stand down and die as the story character: vanilla recovery must work again.
- Abort/reload: no frozen or invincible player, stuck fade, or slowed time.
- Near a stopped four-door car, both companions should walk to separate doors and
  enter normally. Drive off only after watching the entry sequence. Test a blocked
  door and a companion more than 60m away to exercise the fallback.
- Fight a hostile who attacks the player: companions should actively engage. Nearby
  neutral pedestrians must not be attacked simply because they are nearby.

## 4. Survey with GPS or teleport

End the mission first. Open F8 > Survey > Survey M01 only. A yellow destination blip
and GPS route mark the next location. Drive there, or press F7 (SurveyTeleport) to
warp. If collision fails to load within four seconds, the warp returns you to your
previous position. Air/water/roof locations still require care and in-game inspection.

Exit the vehicle, stand on the intended surface and press F11. The named capture is
saved and the route advances. End skips; Home returns to the previous point. Open
the Survey menu to stop. Captures in Bloodlines.Surveyed.ini load automatically after
a restart and take precedence over M01 anchors; no rename is needed. Verify by
capturing a point, reloading, then starting the relevant mission.

Game archives do not establish which surface a narrative beat intends. Do not mark
an estimate verified solely because its district or asset name exists in the files.


## 5. Reporting back

Per finding, this is all I need:

```
MISSION   M01, stage 2
SEVERITY  blocker | wrong | rough
WHAT      Companions stayed on the dock when I drove off in the T20.
EXPECTED  They board within a few seconds or warp in.
LOG       17:42:03 [DEBUG] Devin -> Vehicle
          17:42:09 [DEBUG] Companion missed the boarding window
```

**Severity, plainly:**

- **blocker** — the mission cannot be finished. Everything else waits.
- **wrong** — it works but does the wrong thing (fires the wrong line, marker in the
  wrong place, a character does something out of character).
- **rough** — it works and is not fun yet. Pacing, difficulty, timing. Say what it
  felt like rather than what to change; the diagnosis is the valuable half.

Send the whole `Bloodlines.log` too. Verbose logging includes every stage
transition, cue and companion state change, which usually shows the cause without
having to reproduce anything.

---

## What I expect to be wrong

Written before the fact so it can be scored honestly afterwards:

1. **Coordinates.** Most likely category by far. Even M01's surveyed points may put
   people on the wrong side of a hull.
2. **The yacht.** If no vessel exists at the bilge point, Gohan's whole approach is
   swimming in a dock. Design fix, not a coordinate fix.
3. **Companion vehicle boarding.** Newest system, least exercised.
4. **The 2.5 km exfil.** Suspected dead time.
5. **Stage 0's aim check.** Requires line of sight the geometry may not give.

If all five are fine, the framework is in better shape than the evidence currently
justifies believing.


## Story and mission discovery update

See [STORY-AUDIT.md](STORY-AUDIT.md) for the implemented opening/aftermath scenes, KJ, nickname UI, mission markers, switch behavior, validation and live test sequence.


## M01 live-playtest correction

See `docs/M01-HOTFIX.md` (or `M01-HOTFIX.md` from this docs folder) for the
current exterior staging, camera fix, assigned M01 companion actions and controller
controls. The previous descriptions of bible anchors as surveyed geometry are
superseded: those proposed positions were not verified against installed assets.
`tools/test_dialogue_parser.py` protects speech extraction; authored revisions
live in `data/dialogue_edits.json`, applied by `parse_bible.py`.
