# Architecture

## The loop

`BloodlinesMain` is the only real entry point (plus `DevTools`, which is inert
unless enabled). SHVDN builds it on load and rebuilds it on reload, so everything
it owns is re-creatable and everything it changes in the world is undone in
`OnAborted` — including the time scale, which is the one piece of global state that
ruins a session if it leaks.

```
BloodlinesMain (Script)
  ├─ ModConfig          ini-backed, every key has a working default
  ├─ LocationBook       coordinates from data/locations.tsv, ini-overridable
  ├─ SurveyMode         guided in-game survey that turns estimates into real positions
  ├─ CampaignData       the bible as data: 70 missions, 255 cues, 6 surveyed anchors
  ├─ MissionCatalog     the 70 slots, built from CampaignData; factories mark playable ones
  ├─ CampaignState      savegame.json: progress, economy, safehouses, fleet upgrades
  ├─ FleetGarage        applies unlocked vehicle upgrades to the crew's own rides
  ├─ CrewRoster         the three peds: spawn, companion AI, blips, respawn
  ├─ SwitchController   the 3-way switch
  ├─ AbilityController  one shared meter, three abilities
  ├─ DialogueDirector   the bible's AudioManager: queued, coloured, optionally voiced
  ├─ CheckpointManager  stage, positions, health, wreck purge
  └─ MissionManager     one running mission, pass/fail, progress
```

Per tick, in order: crew upkeep → abilities → fleet upgrades → dialogue → mission →
abort-hold check. Nothing
in that chain blocks except deliberately scripted transitions (a fade, a switch
camera), which are safe because SHVDN runs each script on its own fiber and
`Script.Wait` yields rather than stalling the game.

## Two corrections to the bible's draft script

The listing in section 6 of the design bible is a good sketch, and two things in it
would not survive contact with the game:

**1. `Player.ChangeModel` is not a character switch.** It rebuilds the player ped
from a model. The ped you were switching *to* is left standing there as an NPC, and
the ped you left behind — with its weapons, health, cover state and current task —
is gone. `CHANGE_PLAYER_PED` hands control of an *existing* ped to the player, which
is what a three-hander needs. That is what `SwitchController` uses, after letting
`START_PLAYER_SWITCH` get its camera airborne so the transition reads like GTA V's
own switch instead of a hard cut.

**2. Pinning companion health every frame is invincibility.** `ped.Health = 200` on
tick means damage never accumulates and the AI crew can never be threatened, which
quietly removes the tension from every firefight. `CrewRoster` instead tops a
companion back up only after it falls below a configurable floor, so hits land and
still matter, and the floor can be turned off entirely for a harder run.

**3. The player's story character has to come back.** Deploying the crew hands
control to a gang ped. If the mod simply left it there, the player would be a
`g_m_y_famca_01` for the rest of that save. `CrewRoster` stashes whichever
character the player was using — frozen, hidden, invincible, persistent — and hands
control back on stand-down, on abort, and on script teardown.

## The campaign is data, not code

No mission title, objective, location, or line of dialogue is typed into C#. The
bible PDF is parsed into three TSV files that ship next to the script and load at
startup:

| File | Rows | Used by |
|---|---|---|
| `data/missions.tsv` | 79 | `MissionCatalog` — titles, act, setting, HUD objective, synopsis, and for solo missions their owner and insertion point |
| `data/dialogue.tsv` | 292 | `DialogueDirector` — cue id, speaker, stage direction, line, trigger |
| `data/anchors.tsv` | 6 | missions, via `CampaignData.Anchor` — the bible's surveyed coordinates |
| `data/locations.tsv` | 53 | `LocationBook` — every mission coordinate, with its provenance |
| `data/campaign_registry.json` | 79 | nothing at runtime — the same registry in JSON, generated in the same pass, for external tooling |

TSV rather than JSON because .NET Framework 4.8 has no built-in JSON reader, and a
script mod that drags a serializer DLL along has to version-match it against every
other mod in the folder. Tabs are stripped at generation time, so `Split('\t')` is
a complete parse.

The practical consequence: a bible revision is `tools/parse_bible.py` plus a rebuild
of the data files. Rewriting M34's objective, or all 70 titles, touches no code.

## Solo missions

The expansion adds nine single-character missions (SM01–SM09, three per character).
They are not a separate mode: `MissionCatalog` interleaves them into campaign order
at the windows the expansion gives (Act I after M03, Act II after M28, Act III after
M52), so "start the next mission" plays them where they belong.

What makes them different in code is `CrewRoster.DeploySolo(slot, …)`: one character
spawns, the other two do not exist for the duration, and `SwitchController` refuses
with the character's own line rather than silently doing nothing. A solo mission
that left the crew standing around would undercut the entire reason these exist.

## What is checked without the game

Nothing here has been played, so the checks that do not need the game carry more
weight than usual:

| Check | Catches |
|---|---|
| `dotnet build --warnaserror` | types, signatures, unused results |
| `tools/lint_missions.py` | unknown location keys, unknown or misattributed dialogue cues, model names that are not real vehicles or peds, missions not registered in the catalog, deployments onto air or water, unrecognised scenarios, and dialogue coverage per mission |
| `tools/validate_locations.py` | coordinates in the wrong district or below sea level |
| `ComposedMission.Validate` | at runtime, a stage that could never finish — one built only from passive objectives |

The linter's model check is the valuable one. `new Model("mallard")` compiles, spawns
nothing, and turns the mission's first objective into "the vehicle is gone" — which
reads as a mission bug rather than a typo. Vehicle and ped names are checked against
public data dumps fetched at run time and cached under `build/`.

## Coordinates and their provenance

Every coordinate carries a status, because the difference between them matters more
than the numbers: `bible` came from the omnibus Track 2 index or the toolkit,
`surveyed` was captured in game, `zone-centre` was moved into the right district by
the audit tool, and `estimate` is a hand-placed guess.

`tools/validate_locations.py` checks all of them against the game's real zone
boundaries — fetched from a public data dump at run time, never committed, since it
derives from Rockstar's own files. It catches the class of error that is invisible in
code review: a warehouse in the ocean, a rooftop below sea level, a "Rockford Hills"
position that is actually in Vespucci. It cannot check accuracy, only district; a
coordinate can pass the audit and still be inside a wall.

The rest is `SurveyMode`, which exists because the survey is otherwise the kind of
chore that never gets done: it teleports you to each estimate in turn, captures where
you actually stand, and writes the ini after every capture.

## Dispatching a mission

`missions.tsv` is the registry the dispatcher reads. Beyond the bible's own fields it
carries four that decide how a mission runs:

| Column | Effect |
|---|---|
| `type` | `Trio` or `Solo` — solo missions deploy one character and lock the switch |
| `prerequisite` | the mission that must be complete first; `J` never offers a mission out of order |
| `audio_dir` | that mission's audio bank, e.g. `audio/Act1/M01` |
| `assembly` + `class_name` | optional — dispatch this mission from an external DLL |

Missions built into the mod resolve by id from `MissionCatalog.Scripted`. A row that
names a class resolves by reflection instead, loading the assembly from
`scripts/Bloodlines/missions/`, which is how a mission pack can be added without
touching the core. A broken pack logs and is skipped; it never stops the rest of the
campaign from loading.

Only the running mission is constructed and ticked. That is the actual answer to
"79 scripts will overload the script thread": the mission classes are not SHVDN
`Script` subclasses at all, so the engine never sees them.

## Save state

`savegame.json` holds what has to survive between sessions: completed missions, the
current mission and act, where the crew was last, the economy (cash, gold dredged
from the Alamo, offshore escrow), which safehouses are open and which fleet upgrades
are installed. Missions read and write it through `Ctx.State`.

It is written next to the mod, never into the game's own save — a mod that writes to
a story save can cost someone a playthrough. Writes go to a temporary file and are
then moved into place, so a crash mid-write cannot leave a half-written save; an
unreadable save is copied aside as `.corrupt` and the campaign continues from
defaults rather than silently overwriting whatever went wrong.

## Dialogue

`DialogueDirector` plays a cue by its bible id — `Say("M01_S2_05_ICE")` — as a
speaker-coloured subtitle, with `scripts/Bloodlines/audio/<CUE_ID>.wav` underneath
it if that file exists. Three deliberate properties:

- **Audio is optional.** 255 lines are written and none are recorded. Every cue has
  to read correctly as text, and the mod must never require a voice pack that may
  never exist.
- **Lines queue, they never overlap.** Two characters talking over each other in a
  firefight is how scripted dialogue becomes unreadable.
- **Speaker colours match blip colours.** Ice is blue, Gohan green, Guess orange in
  the subtitles, on the map, and in the ability HUD.
- **The audio bank is partitioned** per act and mission (`audio/Act1/M01/…`), resolved
  from the registry's `audio_dir`, with a flat `audio/<CUE>.wav` fallback for quick
  tests. 292 files in one folder is a directory lookup nobody needs mid-chase.

## Checkpoints

`CheckpointManager` snapshots the stage index, each character's position, health and
armor, and the wanted level; `Mission.Advance()` commits one automatically on every
stage change. Restoring purges wrecked vehicles within 220m first — retrying a
vehicle mission otherwise leaves the canal full of every previous attempt's burnt-out
cars, and takes the frame rate with it.

A mission that needs world state rebuilt when a stage is entered out of order (a
restore, or a QA warp) overrides `OnStageEntered(int stage)`.

## Objectives: missions as composition

A mission written as a bespoke state machine costs 300–500 lines. M01 is 480. Written
76 more times that is the difference between a finished campaign and twelve good
missions and sixty-seven unfinished ones — so the routine parts are a library:

| Objective | What it does |
|---|---|
| `ReachZoneObjective` | get somewhere, on foot or in a vehicle |
| `HoldZoneObjective` | stay in a zone for N seconds — data rips, thermite, dredging |
| `KillTargetsObjective` | clear a specific set of peds |
| `SurviveWavesObjective` | hold out against N waves, spawner supplied by the mission |
| `EnterVehicleObjective` | get into a specific vehicle and seat |
| `ProtectObjective` | fail if something dies or is wrecked |
| `LoseWantedObjective` | shake the police |
| `SwitchCharacterObjective` | the handoff as an explicit beat |
| `AimAtObjective` | hold a weapon on a target — sniper set-ups, EMP locks |
| `TimerObjective` | a countdown the stage runs under |
| `RaceCheckpointObjective` | checkpoints and laps |
| `SubdueTargetsObjective` | non-lethal takedowns count, for stun-gun stealth |
| `PursueTargetObjective` | run a target down; a grace period, not a hair trigger |
| `DestroyVehicleObjective` | wreck a specific vehicle |
| `SpeedFloorObjective` | hold a speed or the chase closes — escorts and convoys |
| `ShadowTargetObjective` | stay with a moving target without crowding it |
| `AvoidDetectionObjective` | do not be seen; uses the engine's own perception check |
| `MultiHoldObjective` | several sites, work at each, player picks the order |
| `AltitudeCeilingObjective` | fly under a ceiling or the SAMs get a lock |
| `DeliverVehicleObjective` | get a specific vehicle somewhere; survives switching away |

A `ComposedMission` implements two methods — `Setup()` spawns the world,
`BuildStages()` returns the mission:

```csharp
yield return new MissionStage("Clear the floor",
        new KillTargetsObjective("Clear Sergei's men.", () => _guards))
    .PlayedBy(CrewSlot.Ice)
    .WithDialogue(2);
```

`.PlayedBy` locks switching to a character for that stage; `.OwnedBy` marks the
objectives as theirs without locking, which is how a mission teaches switching by
making the work only progress for the right character. `.WithDialogue(n)` fires that
stage's cue block from the bible. Objective ticking, the on-screen objective line,
stage advance, checkpoints, failure with a reason and teardown are handled once in
the base class.

SM01 is the reference: 236 lines bespoke, 142 composed, and most of what is left is
spawning. Bespoke stays available — M01 uses it, and the Red-tier set pieces in
`docs/FEASIBILITY.md` will need it.

## Companion AI

Companions run an explicit state machine (`CompanionController`), not generic GTA
follower AI:

```
Follow ─┬─ Combat            in a fight, left alone
        ├─ Hold              split-approach missions
        ├─ Vehicle           boarding or riding the active character's car
        ├─ Scripted          a mission has taken direct control
        ├─ TeleportRecovery  beyond the leash, repositioned
        └─ Downed            dead; respawns if configured
```

Vehicle boarding is deliberate rather than left to the AI. A companion who will not
get into the getaway car ends a run faster than any firefight, so if the car is
already moving or they are too far to reach it, they are warped into a free seat.
Warping reads badly for a second; missing the extraction reads badly for a run.

## Writing a mission

Subclass `Mission`, implement three methods, register it in `MissionRegistry`.

```csharp
public sealed class M06CleanSweep : Mission
{
    public override string Id => "M06";
    public override string Title => "Clean Sweep";

    protected override bool OnStart()
    {
        // Spawn the world. Wrap everything spawned in Track(...) so it is
        // torn down on pass, fail, abort or reload.
        Objective("Cut the depot's power.");
        return true;
    }

    protected override void OnUpdate()
    {
        // Called every tick. Read Stage, call Advance() / Pass() / Fail().
        // Say("M06_S2_03_ICE") fires a written line by its cue id.
    }

    protected override void OnCleanup()
    {
        // Undo anything that isn't a tracked entity: wanted level, weather,
        // switch locks, blips you own.
    }
}
```

Then register the factory against the mission id in `MissionCatalog.Scripted`:

```csharp
{ "M06", () => new M06CleanSweep() },
```

Everything else about M06 — its title, its 01:30 fog, its HUD objective, its five
lines of dialogue — is already loaded from the data files.

Rules that keep missions from rotting:

- **Track everything you spawn.** A leaked mission ped is how a script mod turns
  someone's save into a warzone.
- **No hard-coded coordinates.** Put them in `Bloodlines.Locations.ini` and read
  them through `Ctx.Locations`. Survey them with `F11` rather than guessing.
- **Never block for long in `OnUpdate`.** The player must always be able to switch,
  pause or abort.
- **Lock the switch deliberately**, with `Ctx.Switching.SetLocked("reason")`, when a
  beat requires one specific character — and unlock it the moment the beat ends.
  `Mission.Cleanup` unlocks as a backstop.
- **Fail loudly.** `Fail("...")` with a reason the player can act on; the reason is
  shown on screen and written to the log.
- **Use the written lines.** `Say(cueId)` and `SayStage(n)` pull from the bible. If a
  beat needs a line that isn't written, that is a note for the bible, not a string
  literal in the mission.
- **Call `ApplyBibleSetting()` in `OnStart`.** Time of day and weather are load-bearing
  in this campaign — half these missions are written around darkness or fog doing the
  concealment work.

## M01 as the pattern

`M01GhostInTheDockyard` is the reference implementation and deliberately teaches
the switch by requiring it: three stages, one per character, each solvable only by
that character's discipline, then a collision that puts all three in the same
firefight and hands the escape to the wheelman. Read it before writing mission two.
