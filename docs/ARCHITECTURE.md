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
  ├─ LocationBook       every world coordinate, ini-backed
  ├─ CampaignProgress   70 flags in their own ini, never in the game's save
  ├─ CrewRoster         the three peds: spawn, companion AI, blips, respawn
  ├─ SwitchController   the 3-way switch
  ├─ AbilityController  one shared meter, three abilities
  └─ MissionManager     one running mission, pass/fail, progress
```

Per tick, in order: crew upkeep → abilities → mission → abort-hold check. Nothing
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
    }

    protected override void OnCleanup()
    {
        // Undo anything that isn't a tracked entity: wanted level, weather,
        // switch locks, blips you own.
    }
}
```

Then in `MissionRegistry.Specified`, add the factory to that mission's definition:

```csharp
new MissionDefinition(6, "Clean Sweep", "...", () => new M06CleanSweep()),
```

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

## M01 as the pattern

`M01GhostInTheDockyard` is the reference implementation and deliberately teaches
the switch by requiring it: three stages, one per character, each solvable only by
that character's discipline, then a collision that puts all three in the same
firefight and hands the escape to the wheelman. Read it before writing mission two.
