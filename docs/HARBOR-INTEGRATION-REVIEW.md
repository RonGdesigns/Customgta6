# Integration review: preservation is not full gameplay certification

Reviewed main: `f8a37b9837b11f6661df081fd59616b7c06e6bb3` (through PR #32).
Reviewed harbor repair: `ea4c54667d60d10f8973bfeccdd9713c97499501`.
Combined source merge: `75274fd2f8e209cca1a86b6b7f871bd3c64b58d4`.

The integration deliberately preserves both agents' mission work. Its three-way source checks and static DLL comparison establish that the merge did not discard those implementations. They do not certify every inherited behavior. No GTA playthrough was available.

## Changes worth retaining

M03's road-placed Benson and depot return/ambush, M05's shoreline staging, M06's corrected pickup timing, M07's separated roof/street/driver roles and attack helicopter, and M08's no-button stationary forklift pickup and separated sentries all remain. They address the reported gameplay instead of removing it. The harbor branch retains the water geometry guard, visible tug/container, depth guidance, police reacquisition, on-foot score requests, final centered banner, letter map icons and the whole-attempt/no-checkpoint rule.

## Follow-up 1: M07 roof fallback reports success on an unverified estimate

Source: `src/Bloodlines/Missions/Campaign/Act1/M07WiretapWaltz.cs`, `Setup()` and `RoofTop()`.

`RoofTop()` returns the input estimate if its ground-height query fails or returns a surface too low. `Setup()` then sets `_roofFound` from `_roof.Z > estimate.Z - 3f`. Returning the estimate therefore satisfies the supposed success check. The new warning/fallback can be skipped precisely when no roof was verified. The deployment also continues to use `_roof + (0, -8, 0)` rather than an explicit verified street fallback.

Recommended bounded correction: have the roof resolver return a separate success value and a validated position; require the relevant roof/platform probes to succeed before rooftop deployment. When they fail, either use a genuinely verified, reachable street approach with clear guidance, or stop setup with the exact survey key. Never certify the estimate itself by comparing it to its own height. Preserve the rooftop role, Gohan's street laptop and Ron's pickup.

Acceptance: successful roof query; native query returns false; native query returns a street below the expected roof; neither roof nor safe street available. Check actual spawn selection and warning, not just the RoofFound property.

## Follow-up 2: M08 mixed-role stage inherits Guess for both objectives

Sources: `src/Bloodlines/Missions/Campaign/Act1/M08SupplyAndSever.cs`, `BuildStages()`; `src/Bloodlines/Missions/ComposedMission.cs`, `PrepareStages()`.

The stage named `Crate two, the technical` labels vehicle destruction as Ice's work and the second forklift pickup as Guess's work, but neither nonpassive objective has an explicit RequiredCharacter. The preceding stage is Guess-owned, and PrepareStages fills those missing owners with Guess. The existing test can destroy the technical as Guess, so it does not establish the intended split.

Recommended bounded correction: explicitly assign the technical objective to Ice and the forklift objective to Guess individually. Do not assign the whole mixed stage to one brother. Preserve the one-second stationary pickup, required forklift/driver checks, separated sentries and ability to complete the two jobs in either order.

Acceptance: inspect effective objective owners after stage preparation, then complete Ice-first and Guess-first attempts. Check that the HUD does not incorrectly demand Guess for Ice's objective and that neither job is lost during a switch.

## Separate live acceptance

Replay the preserved M03-M08 behaviors and the entire normal Port Heist. Check the real submarine launch, visible hull/cargo, depth spheres, boarding, attached load, road-team arrival, final deposit, wanted-state reacquisition and occlusion, music audibility, banner timing and map glyphs. Fail, abort and reload must restart the entire heist from M19. These remain engine/geometry tests rather than claims inferred from passing stand-ins.

These two findings predate the integration. No gameplay changes to them are folded into the preservation merge; a follow-up should be a separate reviewable correction with its own rebuilt DLL and tests. The existing wider campaign audit also remains separate.
