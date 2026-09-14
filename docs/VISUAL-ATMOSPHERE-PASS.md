# Visual atmosphere: first runtime comparison pass

This is a grading-control and integration pass, not a new rendering engine or a finished photorealistic preset. It builds on main `6867c858d45e0a7d11369d33ac117f3f044a9b8a` and the Forests intake branch `8bb9dd3514314fc1ef9c7ab8a399b3dff434c125`. It preserves the campaign source/data, personal surveys, home/prologue, repairs and one-sitting Port Heist. No Forests payload, game archive, new model, texture, shader or license grant is part of this source change.

## What is implemented

`VisualAtmosphere` delegates its script timecycle slot to `TimecycleGrade`. Existing preset choices and strengths remain unchanged; there is no newly invented or silently substituted modifier name.

- The controller first checks both the active and incoming-transition indices. It acquires an empty slot only. After setting the requested name, it requires a non-negative active index before reporting engine acceptance or setting strength.
- Acquisition sets strength to zero in the same update. Strength follows a one-second smoothstep fade, based on bounded game-time deltas. A changed name fades out to neutral, releases, then fades the new name in on following updates. This is not direct two-modifier crossfading.
- An ungraded time band fades the owned effect out. The default dusk remains ungraded. No sunset saturation filter is added.
- Scenes, apartment streaming/interiors, abilities and recovery suspend the grade immediately. Returning to ordinary gameplay fades back in. The host releases it on early-return paths and after a scene that began later in the tick.
- Another active index or incoming transition causes the controller to relinquish ownership without changing that other state. Reset only clears an index the instance still observes as its own. It no longer unconditionally clears global grading or resets ocean/shadow settings it never touched.
- A name that produces no active index is reported and tried only once per script instance. A native error disables this instance's grading after a guarded cleanup attempt. This does not fail a mission or retry writes every frame.
- A session-only grading comparison lives in the existing developer menu: **World > Grading comparison**. Its status row distinguishes fading, active, suspended, waiting and rejected states. It does not write a configuration file or save.

Existing LOD, blur, shadow-distance, headlight and reflection options are not retuned in this pass. Existing mission/free-roam swell behavior is preserved. The comparison changes only our grade, not those options or the installed map packs. There are still only the original two `GTA.Script` entry points.

## Honest limits on ownership and validation

The native index check establishes that a script timecycle slot became active after our request. It does not verify that a particular modifier looks natural, that its appearance matches a reference video, or that the data is suitable for every weather/interior. The historical default names still need review against the local Enhanced timecycle data and visual A/B testing. No such XML export or GTA render was available for this pass.

The index does not identify an owner. Another mod selecting the SAME index or editing its strength is not distinguishable with this check. Extra-timecycle slots and post-effects are separate systems. This is not universal interoperability with every graphics injector. Disable overlapping Bloodlines options when another mod owns the same setting; a reset of no-getter settings can only restore documented defaults, not an unknowable prior value.

The one-second fade is the current runtime constant, not an INI setting. Small time-band adjustments do not retime missions. Large clock/load gaps are ignored rather than catching up instantly. Pause does not advance a fade. A required scene/ability suspension is immediate, not delayed for visual smoothness.

A technical failure during native cleanup cannot prove the game register was restored. The controller logs that limitation and stops further grading writes. Review the log and reload; do not label a failed native call successful restoration.

## How to compare, after the separate Forests test

Do not change the campaign DLL while measuring Forests A versus B. First finish that baseline using the same installed Bloodlines version and graphics settings.

The runtime grading pass needs the new matching Bloodlines DLL/data, not a second script DLL beside the old one. Close GTA, preserve the installed DLL and the entire Bloodlines folder outside the scripts tree, and use a copied campaign. The package does not contain Forests or register DLCs.

With the test build loaded, open the existing developer menu (`[Dev] Enabled=True` where needed) and choose **World > Grading comparison**. Compare configured and baseline grading at the same place/hour/weather/camera. Allow the fade to settle after closing the menu. The selected mode lasts for this script instance; reload starts with configured grading again. The menu's other existing clock/weather controls are not part of this A/B procedure and should not be changed during a mission.

"Baseline" here means Bloodlines grading off. It does NOT mean vanilla GTA, uninstall Forests, disable ray tracing, restore another mod's shader files, or reset all graphics options. Record upscaling, frame generation, resolution and game graphics settings separately and keep them constant.

## Required live checks

1. Compare daylight face/sky/concrete detail, nighttime objective readability and wet-car reflections at fixed viewpoints. Record the requested modifier and status in the log. No percentages or FPS claims without measurement.
2. Cross day/dusk/night/dawn boundaries. Existing grade should fade through neutral; dusk receives no replacement filter. Watch for color pops or invalid-name warnings.
3. Watch and skip a scene, enter/exit an apartment, activate/end each ability, switch brothers, and exercise death/recovery. Grading must yield and return without swallowing the scene/ability effect or blocking input.
4. Toggle grading comparison repeatedly in the World menu. Confirm that mission time/weather, water conditions, LOD, save and Forests installation remain unchanged.
5. With another grade owner present, confirm our status waits/yields and our reset does not clear its distinct index. Same-index interference is explicitly not solved.
6. Compare fast forest driving and an aircraft departure using the same scenery/graphics baseline. This source pass does not validate Forests collisions or mission clearance.

Automated tests exercise the actual C# controller against native stand-ins: fades, no-op ticks, invalid strengths/names, pause/time wrap/load gaps, foreign slot/transition, failure cleanup, adapter behavior and grade-only A/B. Host/menu wiring checks are source checks, not an executed GTA UI test. The old immediate-grade tests are updated to assert the new fade contract; vehicle-damage and unrelated mission tests are retained.

## Still next in the visual plan

Weather-specific art direction and a verified local modifier catalog, performance-tuned gameplay/capture profiles, material/road assets, scenery collision adaptation, and final GTA acceptance remain separate tasks. Forests mapping needs the local game PLUS the installed forest geometry; this pass does not relocate missions to accommodate scenery.

## Technical references

Only GTA native calls available through the pinned ScriptHookVDotNet API are used; no FiveM-only modifier editing functions or guessed memory offsets.

- https://github.com/citizenfx/natives/blob/master/GRAPHICS/GetTimecycleModifierIndex.md
- https://github.com/citizenfx/natives/blob/master/GRAPHICS/GetTimecycleTransitionModifierIndex.md
- https://github.com/citizenfx/natives/blob/master/GRAPHICS/SetTimecycleModifier.md
- https://github.com/citizenfx/natives/blob/master/GRAPHICS/SetTimecycleModifierStrength.md

These references describe callable operations, not performance or appearance guarantees for Ron's installation.
