# Campaign phone — first playable version

The custom phone opens with **D-pad Up** (or the normal keyboard phone control) while the Bloodlines crew is deployed. **F9** also opens or closes it. Navigate with the D-pad or arrow keys; **A / Enter** selects and **B / Backspace** goes back. Back from the home screen closes it. Long pages scroll with Up/Down.

## Apps

| App | Connected behavior |
| --- | --- |
| Messages | Shared crew dispatch history, revealed only after each associated job is completed. Existing delivered notifications remain readable after reload. |
| Crew | Ice, Gohan and Guess's nicknames, specialties, abilities and current availability. KJ's contact appears after SM03 and explains where to use his existing delivery service. |
| Current job | The live mission title and complete current objective. In free roam, the next playable lead and campaign progress. |
| Destination button | Re-shows the active brother's current yellow mission route. Water objectives retain their water marker. Ambiguous objectives explain that there is no single destination. Free roam routes to the next playable lead through the existing story gates. |
| Wallet | Actual shared campaign spending balance, completed-job count, owned vehicles and garages. |
| Weazel News | Reports already unlocked by completed campaign events. |
| Help | Controls and an explanation that the world keeps moving while browsing. |

Guess uses amber, Ice blue and Gohan green. The refined interface uses a rounded handset frame, softly lit ribbon wallpapers, original line icons, translucent selection cards and darker reading panels. Larger headings, body text and app labels use the native Chalet London font; this is a typography/layout pass rather than a new external font. The 29 original textures are baked by `python tools/build_phone_art.py` (Pillow required for asset generation only), shipped in `assets/ui`, and loaded once per skin/icon. Missing artwork falls back to functional native panels. The clock follows GTA time. This version is a native on-screen overlay, with no external browser requirement or game-archive replacement. It does not yet project the apps onto an in-world handset, place optional voice calls or offer free-text messaging. The second page now includes KJ ordering for owned cars, crew requests, journal, progression, alerts and the preparation board; see `CAMPAIGN-HUB.md`. Scripted story calls and their animations continue using the existing scene system. Shop and car purchases keep their existing entry points; KJ can also be requested through the Garage app.

## Input and lifecycle

The phone uses per-frame input suppression. It never changes time scale, player control ownership, actor tasks, vehicle seats, frozen state or cameras. Walking, looking, normal throttle and braking remain available; combat, interactions, character selection and the menu's overlapping vehicle buttons are suppressed while browsing. Pause remains available.

Death/recovery, an active cutscene, a mandatory character handoff, character switching, a placement survey, another menu, the prologue or apartment access closes/blocks the phone. It can open again after those systems release control. Closing consumes trailing input for 180 ms so a B press does not also trigger another action. Disabling the feature or standing down restores the normal phone path after that brief input guard.

Mission updates continue while the screen is open. The destination button is processed after the current frame's objectives publish their navigation targets. It never routes to another brother's assignment or creates a persistent personal waypoint during a mission. The phone neither starts missions nor bypasses their prerequisites.

Existing saves and surveyed positions need no migration. Existing user INIs are preserved during installation; defaults enable the phone even when the new entries are absent. Optional settings:

```ini
[Phone]
Enabled = true

[Keys]
Phone = F9
```

## Verification and live test

The build compiles against the pinned SHVDN 3.6 API. The runtime harness exercises input ownership, opening/closing, live balance/objectives, message gating, save reload, route ownership, ambiguous/water targets, cancellation during transitions and scrolling. Layout previews use the production drawing commands with an approximate offline font renderer; they cannot verify GTA's native input, font scaling or another trainer's bindings.

1. Deploy the crew, open with D-pad Up, browse all apps, and close with B. Confirm movement and the camera work normally afterward.
2. Open while driving: throttle/braking should continue; A/B should operate the phone without shooting, changing camera mode or leaving the seat.
3. During a mission, read the full Current job page and select its destination. Confirm it matches the active brother's task. Advance an objective while the phone is open and check that the page updates.
4. Trigger a mandatory switch, scene or death/recovery with the phone open. Confirm it closes, the normal handoff/recovery completes, and it can reopen afterward.
5. Switch brothers and reopen to check the name/color. Verify Wallet agrees with a shop purchase and completed-job messages remain after reload.

Garage/KJ, crew commands, journal, progression, alerts and the Foundry planning board are now connected. The implementation and test checklist are in `CAMPAIGN-HUB.md`. Optional calls and in-world handset rendering remain future extensions.
