# Playthrough, garages and economy update — September 12

The prior validated prologue/phone/panel-damage package was installed first. This second package adds the following changes on top of it, preserving the PR40 LSIA car start and all personal save/config/survey files.

## Full change list

1. Shared briefings keep deployed actors and their actual arriving vehicle until dialogue finishes. Mission deployment happens afterward. Nearby seated brothers stay in the same car; absent/distant speakers use radio. The artificial gray Granger drive-up, which waited on traffic and could appear on a roof, is removed. M03/M04 start markers remain the foundry; the briefing uses the yard or arriving car, then required mission vehicles are introduced.
2. M01's prototype heading is rotated 90 degrees in gameplay and its opening shot, relative to the location book (including user surveys). The service laptop remains on its own table away from Mateo. Gohan approaches/faces the laptop and uses a sustained upper-body hacking loop during the copy. Moving, firing, switching or cleanup cancels the animation. The rest of the dock objectives and cover rules are retained.
3. M02/M03 boarding happens concurrently, with a shared 6.5-second bound rather than several sequential 15-second waits. Short establishing shots replace extended idle shots. Skip and normal completion reach the same assigned seats; abort cancels started actions.
4. M02's driver keeps steering until slowed before the mandatory Ice switch. Shared road handoffs use a modest service brake rather than a forced handbrake; passenger handoffs never brake the AI driver. Ice stows the held drives on reaching the car, freeing his hands before entry. Normal F/controller Y remains available, and E/D-pad Right requests a real entry task rather than a seat teleport.
5. M02 GPS first points back to the crew car while Ice is on foot, then to the canal when aboard. The shared route service refreshes GTA's cached route on a meaningful destination change, with bounded refreshes for moving targets.
6. M03 parking at the rail marker starts an exit-and-walk scene automatically. The dogs remain neutral and stationary before the ambush; when it begins they receive crew-hostile relationships and explicit attack orders. Their markers and teammate hostile lists are gated to that activation.
7. M03's three crates are placed separately on the ground and retried when the yard is streamed; no floating stack. Carrying/stowing explicitly releases prop freezing. Most initial guards defend the truck's upper yard, with nearby walkable placement; final heights and accessibility need a live check.
8. M03's loading beat shows the last crate without making Gohan enter a cab he must immediately leave. Rescue arrival is measured at the actual Benson, not a different depot anchor. Its combat stage explicitly accepts any brother, preventing inherited Guess ownership from relocking the switch. Incoming enemy cars use their own arrival points, allow time to drive in, and finish exiting before receiving combat orders.
9. M04 says meet the crew, retains separate approaches, and its foundry dialogue explains the exchange and breaker before departure.
10. Added an outlined high-contrast crosshair while aiming ordinary on-foot weapons; it stays off in scenes, menus, death, unarmed use and sniper scopes. `[HUD] VisibleCrosshair=false` disables it.
11. Added the garage/dealer/KJ system described in GARAGE-PLAN.md, with full saved vehicle finish data, capacity checks, occupied-vehicle safeguards and persistent identity. Existing crew-van ownership stays separate.
12. Added direct street-car sales at dealer/customs/garage menus and stored-car sales. Successful sales share ten slots per GTA day across the crew, saved with the cash. Failed/blocked sales do not count. Reopening the game does not clear the same day's allowance.
13. Rebalanced first-completion mission payouts and selected dealer prices against the existing shops. The 36 playable jobs now award $2,202,000 total. M01–M03 pay $12,000/$18,000/$25,000. Replays still do not pay again; old completions receive no automatic duplicate payment. Full reward table: PROGRESSION-GUIDE.md.
14. M06 releases the Guess-only role after Ice and Gohan board the Granger. Either passenger can be selected for the escape, with Guess retained in the driver's seat under shared companion driving. The objective explicitly requires all three aboard; losing the police can complete the mission from any brother's seat.

## Focused live test

1. M01: inspect prototype orientation; interact with Gohan's table laptop and try switching/moving during the copy.
2. M02: start in an existing car with passengers, watch and skip the briefing, complete the hack, slow down/switch to Ice, collect drives and enter with controller Y/F. Check GPS to the car, then canal.
3. M03: start at the foundry in your own car; verify no roof/gray stand-in. Park at the junction, watch the exit, check dogs only attack with the ambush. Inspect grounded crates and upper-yard enemies, load, rescue at the truck and switch among all three during the fight.
4. M04: check the foundry briefing and meet-the-crew objective.
5. Free roam: store/retrieve a customized car, buy a garage/car, call KJ, test a refused occupied sale. Sell ten cars; confirm the eleventh is refused, reload, then pass a GTA midnight and sell again.
6. M06: collect both brothers, switch from Guess to Ice and Gohan while escaping, and confirm Guess continues driving. Lose the police while controlling a passenger to finish.

Runtime tests exercise state transitions and native-call contracts using stand-ins; they cannot prove live collision, animation alignment, driving quality or camera framing. Garage interiors and berth/hangar storage are still planned.

Animation source reference: the GTA data/animation lists identify the humane-labs hack loop used for terminal interaction: https://github.com/DurtyFree/gta-v-data-dumps . The real clip and table alignment need the live test above.
