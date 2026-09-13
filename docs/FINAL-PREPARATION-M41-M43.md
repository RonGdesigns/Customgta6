# Final preparation: M41–M43

September 13, 2026. This is the next implemented block after M36–M40: **49 playable jobs, M01–M43 and SM01–SM06**. These missions compile and have dedicated automated walkthroughs. Flight physics, AI pathfinding, controller feel and camera framing still need live acceptance in GTA V Enhanced.

## What each mission actually does

| Mission | Playable flow | What carries forward |
|---|---|---|
| **M41 — The General's Wire** | Ice walks to an observation position and identifies Bradley. Bradley walks clear of a lodge worker. Ice takes the shot, retrieves the physical card from the body and brings it to Gohan at the extraction car. Guess drives all three brothers and the card out. Gunfire or close exposure starts Bradley's visible run to an escape SUV and a bounded escape window. | Verified `bradleyKeycard` evidence and $110,000 on first completion. |
| **M42 — Skyfall Delivery** | Guess begins on an airborne coastal approach in a Titan. Gohan is seated in the Kraken attached to an external cradle. Fly into the offshore drop ring in level flight, 150–350m above the sea and below 85m/s. **E / D-pad Right** releases only after two visible canopy attachments succeed. Circle within 1.2km during descent. After stable splashdown, switch to Gohan and pilot that same sub to the cove. Guess holds the Titan offshore while inactive. | Delivered `offshoreSub` and $175,000 on first completion. |
| **M43 — Staging Paleto** | Gohan moves the Kraken to its holding point, Ice moves the surface launch to a separate position, and Guess flies an Annihilator from Sandy Shores to the Paleto coastal staging lot. Guess uses the nearby planning laptop after all positions and previous preparations check out. | `offshoreStaging`, the preparation-ready flag and $90,000 on first completion. |

The package adds **$375,000** in first-completion payouts. All current scripted missions together pay **$3,722,000**. Replays do not pay twice. No offshore vault haul is awarded here.

## Physical drop and story adaptation

The original bible proposed a 10,000ft cargo-hold drop. The stock Kraken is about 5.4m wide; no safe fit inside the Titan's cargo bay has been validated. This version uses a visible external ventral cradle and a shorter release altitude so the player can observe the actual descent. There is no claim of a custom cargo bay or an unseen high-altitude sequence.

`SubmarineAirdrop` verifies the real attachment, attaches two stock parachute props, detaches the sub and restores its collision. GTA moves the vehicle; the helper applies horizontal drag and caps downward velocity at 8m/s. It never changes the sub's position during successful descent. A full water footprint is checked at intervals, collision is streamed around the descending sub, and actual water contact must settle for 1.5 seconds before control can pass to Gohan. Time alone cannot complete the drop.

The first flight stages keep control with Guess. Gohan remains in the same submarine seat throughout; there is no airborne character swap or replacement submarine. After splashdown the switch opens and inactive Guess receives a plane holding task over the ocean. The release inspection shows the actual attached vehicles; it ends before live flight starts.

## Failure and retry

Every mission uses **full mission restart**. A retry rebuilds the actors, vehicles, objectives, attachments and provisional story state. Failed attempts do not commit evidence or cargo.

| Mission | Explicit failure conditions |
|---|---|
| M41 | Shooting Bradley before identification or the clear-shot signal; killing the lodge worker; Bradley escaping after alarm; an obstructed meeting walk; lost card, required crew or extraction vehicle; blocked required boarding. |
| M42 | Lost Titan/sub/crew, Guess leaving the Titan, Gohan leaving the sub before splashdown, failed cradle or canopy attachment, shallow/obstructed water, departure from the observation area, or descent not completed within 90 seconds. |
| M43 | Lost required asset or crew. Missing preparations hold the ledger stage with named reasons. A moved asset or missing prerequisite at final confirmation prevents success. |

Cleanup removes parachutes, clears streaming focus, restores collision and flight freeze state, and releases crew roles/switch locks. Aborting before splashdown first unseats Gohan and returns him to the safe shore. If the engine refuses to unseat him, **abort recovery only** moves his occupied sub to the already-validated cove; that path never records successful delivery.

M43 requires M31–M42 completion, their preparation flags, Bradley's verified card and the delivered sub. Selecting M43 directly in the debugger is useful for placement inspection, but its ledger will name missing preparations. Debug selection does not fabricate an assault-ready campaign.

## Placement and survey

There are 35 new location keys. All 466 existing location rows remain unchanged, including prior manual placements. Read-only CodeWalker queries inspect installed map collision, road nodes, water and the required props. No Rockstar archive is modified or packaged.

The initial lumber-yard helicopter proposal failed the wider rotor-clearance scan and was replaced with a coastal staging lot. The laptop interaction point was also moved off an underwater edge. M41 uses an exterior lodge perimeter. M43 uses the open coast and a surface Tropic; it does not imply a walkable sea cave or a mounted turret that the model lacks.

The surveyor reads the new location keys. The M42 attached cargo and canopy offsets are deliberately relative to the live aircraft/sub, not independently editable world spawn points. Survey the Titan approach, drop corridor, holding area and delivery point to change the flight layout.

## Live test order

1. **M41:** watch and skip the opening on separate attempts. Verify the worker is beside Bradley initially and Bradley walks clear. Trigger the alarm on another retry and check his actual run/boarding/escape. Recover the card, board all three brothers, and confirm extraction only passes with the card aboard.
2. **M42:** inspect the cradle and canopy appearance first. Try release outside the ring and below the altitude band; both must refuse. Make a valid release, circle the falling sub, and confirm the switch stays closed until stable water contact. Switch to Gohan, verify the seat/camera/control recovery, and drive to the cove. Separately abort during inspection and descent to check safe cleanup.
3. **M43:** position both marine assets, land at the center of the Paleto lot, and walk to the laptop. Confirm no brother gets pulled into the camera location. On a debug-skipped campaign, check that the ledger identifies missing preparation missions instead of passing.
4. Re-run the still-pending M13 live startup check when convenient. This package preserves the previous M13 startup repair; new automated success is not proof that the reported live problem is resolved.

## Next plan boundary

M44–M48 needs the actual offshore assault set: collision-supported underwater targets, a usable landing area, vault access, and an escape/destruction sequence. Those missions remain unregistered until those physical spaces and gameplay exist. M49–M70 and SM07–SM09 also remain plans.

## Automated validation for this package

- Production build: 170 sources, warnings treated as errors, pinned ScriptHookVDotNet3 3.6.0.
- 2,444 story/runtime checks, including physical drop and all three mission walkthroughs.
- 189 regression checks and three dialogue-parser tests.
- Mission lint: no errors; 48 of 49 scripts fire every authored cue. M01 retains its previously documented cue exception.
- Location validator: 0 of 501 flagged. Read-only map investigation: 638 samples including rejected candidates; see [placement results](FINAL-PREPARATION-PLACEMENTS.md).
- Generated story and playable-mission map freshness checks pass.

These checks use GTA stand-ins and offline geometry; they do not replace the live tests above.
