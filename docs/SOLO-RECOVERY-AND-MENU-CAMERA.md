# Individual recovery and controller camera

This follow-up changes player death/arrest recovery and debug/home menu controls. It supersedes the whole-crew recovery and right-stick menu navigation descriptions in earlier release notes.

- Only the active hero is revived, healed and returned to the recorded safe recovery point. Surviving teammates keep their health, armor, positions and vehicle seats. In free roam their existing activities and personal wanted levels are preserved.
- Mission death still fails the current mission. Mission cleanup may release scripted assignments, but recovery does not regroup or heal the other heroes. Existing occupied-vehicle protection remains in effect.
- Catch-up teleport and boarding-warp recovery are suppressed for separated teammates after the player's death. They may travel naturally. Coming within 65 meters, issuing a new free-roam behavior order, or starting a new mission ends this restriction.
- Teammates who also died are not revived by the player's recovery, including the next automatic companion-update tick. A new behavior order or a fresh mission deployment can resume normal companion recovery.
- The left stick moves the player, the right stick controls the camera, and D-pad/A/B navigate the debug and home menus. Camera mode cycling and idle-camera changes remain blocked while a menu is open.

The production build, 106 behavior/recovery checks and 309 story/runtime checks pass. Tests include the actual active-recovery roster method, a surviving driver and distant teammate, separate health and heat, and prevention of post-death catch-up teleport. Live GTA testing is still required.

Retest: leave one teammate driving and another elsewhere, then die as the active hero. Confirm only that hero moves to recovery and that walking returns. With the debug menu open, move and turn using the sticks; verify only the D-pad changes the selected menu row. Close the menu and verify normal controls return.

This update replaces only Bloodlines.dll in the game. Existing progress, appearance, settings, dialogue and locations are preserved.
