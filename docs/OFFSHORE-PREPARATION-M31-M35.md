# Offshore preparation: M31–M35

September 13, 2026. Built together on the user's explicit approval. This adds five scripted chapters after P5b/P5c, bringing the campaign to **41 jobs: M01–M35 and SM01–SM06**. M36 onward and SM07–SM09 remain plans. This package retains the preceding M13 startup repair; that repair still needs a live retest.

## What to play

Normal progression offers M31 after M30. Debug mission selection can launch each chapter independently. Follow the on-screen role and yellow objective marker; use the normal character wheel when prompted. Physical work uses **E / D-pad Right** at the marked object, with the displayed progress bar. Stop for boarding and cargo delivery. Combat stages allow the available brothers to fight; required work stages identify the responsible brother.

| Mission | How it plays | First-completion cash |
|---|---|---:|
| M31 — The Iron Perimeter | Ice inspects three perimeter approaches in any order. Gohan arms a visible charge at each barrier. Guess proves the withdrawal road in the crew car. Two response vehicles arrive only after preparation; defend the generator, then inspect it and lose any police pursuit. | $70,000 |
| M32 — Black Site Zancudo | Gohan pilots a dinghy to the coast and walks to an exterior electrical cabinet. Ice clears the marked service-post guards. Guess brings the car to the pickup. Gohan carries and stows two separate EMP cases. Board, escape, lose the police, return and unload both cases at the bunker work table. | $125,000 |
| M33 — The Informant's Grave | Ice identifies Ramos and stops four execution guards. Gohan cuts his restraints. Guess brings a four-seat extraction car; Ramos sits in front and the brothers in back. Escape to the wind-farm transfer point and wait for Ramos to enter the half-track. | $75,000 |
| M34 — Mud & Iron | Guess drives the half-track, Ramos takes its front seat, Ice uses its rear turret, and Gohan follows in the crew car. Clear two route sections under pursuit. Prepared barriers activate only after both friendly vehicles pass. Lose police, stop at the shelter, let Ramos get out, then use Gohan to prepare the medical kit. | $125,000 |
| M35 — The Chianski Ambush | Ice plants two charges and takes cover. Guess blocks the far exit. Gohan identifies the target at a visible field laptop. The convoy then approaches: stop its lead escort, defeat the armed crew, preserve the technical, board its real seats, lose pursuit and deliver it for a gun-mount inspection. | $140,000 |

**Block total: $535,000.** Replays do not repeat payouts. M31 adds Ice's Proximity Mine, Gohan's Grenade and Guess's Sticky Bomb entitlement; M34 adds Ice's Assault Shotgun, Gohan's Combat MG and Guess's Heavy Rifle entitlement. Previously owned weapons are not counted as new unlocks. All currently scripted first completions total $2,737,000.

## Story continuity and the physical scenes

The perimeter is preparation for holding the desert refuge, not an invincible base. Its generator, barriers and charges exist as mission objects. Charges only detonate once per placed charge, with an enemy in the lane and all brothers clear.

The Zancudo theft uses a temporary **exterior coastal ordnance service post west of the base**: a visible office, electrical cabinet, access barrier, guards and two cases. There is no walkable underground bunker in this build. Opening the remote barrier is shown before the crew collects its contents. Carrying, loading and unloading change the same physical case objects; the EMP hardware is recorded only after delivery.

Ramos's rescue earns trust before it earns intelligence. The rescue clock begins when the crew reaches the firing line or fires nearby, not during the trip there. He must be freed within the 90-second rescue window. M33 ends after he boards the half-track. M34 starts at that same transfer site, with a newly staged half-track and escort car: **these are separate chapter starts, not a seamless live-entity handoff**. Only reaching medical care in M34 records the rig access codes.

The half-track has three usable seats. Gohan therefore operates from a separate escort car. When the player switches to Ice or Gohan, the mission keeps Guess assigned to drive the patient vehicle along the current leg. Route completion follows the half-track even while the player is in the escort. Inactive passengers receive shooting tasks; NPC drivers keep driving. M34 uses the game's dust-haze weather preset and two physical road barriers, not a custom animated rockslide.

The Chianski capture protects the mission target from its own ambush. The convoy does not spawn until the trap, blocking car and identification work are ready. Its lead escort must actually enter the trap. Detonation waits if a brother or the technical is too close. The technical is a stock machine-gun vehicle suitable for engaging low aircraft; no missile launcher or automated anti-aircraft turret is implied.

Short establishing shots show actual actors, vehicles and equipment where they stand. Inactive brothers retain their assigned roles instead of adopting free-roam behavior. Work uses existing interaction animations, physical hand attachments, normal boarding and actual vehicle transfers. These scenes have subtitles and no generated voice track. Skipping a shot follows the same required physical result.

## Failure and retry

These five missions use **full mission restart**, not mid-stage checkpoints. Failure/abort discards provisional evidence and cargo state. Permanent cash and entitlements commit only on successful first completion. Required asset creation failures reject startup with an error instead of advancing an empty scene.

| Mission | Specific failures and protections |
|---|---|
| M31 | Destroyed generator or required extraction car; required crew loss. Probe response waits until all three charges are armed and Guess reaches the withdrawal marker. |
| M32 | Destroyed case/car, lost case attachment, required crew loss, unavailable boat/props. Both cases must be attached to the delivery car before unloading can succeed. |
| M33 | Ramos killed, rescue deadline missed, required extraction/transfer vehicle lost or crew loss. The final scene waits for actual passenger boarding. |
| M34 | Ramos leaves the half-track during the route, Ramos/half-track/escort loss, or required crew loss. Barriers wait for both friendly vehicles to clear; medical completion waits for Ramos at the work area. |
| M35 | Technical destroyed, required crew/vehicle loss, or convoy fails to reach the trap within 150 seconds after release. Preparation has no convoy timer. Capture cannot advance without the trap result. |

For NPC boarding, a stopped car must have the correct free seat. Missing or occupied required seats fail clearly. A nearby passenger unable to board for 45 seconds causes an explicit retry failure; driving away or walking from a distance does not consume that nearby boarding timer. Park away from fences, barriers and other cars before retrying. NPCs in the wrong seat are asked to leave and reboard normally.

If a mission cannot start, note the mission ID and location/asset named in the message; the mod log records the failure. Survey All exposes the new named mission locations so they can be inspected individually. Do not force-complete objectives when checking cargo or prisoner custody: that bypasses the thing being tested.

## Rewards with effects

M31 records perimeter readiness. M32 records the delivered EMP cases. M33 records Ramos at the armored transfer; M34 moves him to medical shelter and records his access codes. M35 records the captured technical at delivery.

After M34, the half-track receives the support profile when the fleet system sees it: available brake and armor upgrades plus reinforced tires. M35 unlocks the same profile for the technical. The half-track and technical used at successful delivery are released for continued use. **Use normal garage storage to save ownership**; retaining a live mission vehicle is not an automatic persistent garage purchase. Paid replacement purchase entries remain future work.

## Verification and first live pass

Automated coverage exercises all five complete mission flows, physical cargo attachment, rescue deadline, patient transfer, switching between convoy vehicles, turret task assignment, barrier clearance, trap safety, asset failure, blocked boarding, rewards and replay protection. Compilation uses the pinned ScriptHookVDotNet 3.6.0 reference with warnings treated as errors. Passing the test harness does not prove GTA navigation, physics or streaming behavior.

Read-only CodeWalker checks against the installed Enhanced archives covered **94 new named locations plus 36 footprint corners**. All land centers had supporting collision; the two boat points had water over seabed approximately 27 m and 5.8 m deep. Existing 295 source location rows and manual surveys were retained. New props were found in the installed archives. The raw map report stays in build/codewalker-preparation-27 and is not redistributed in the mod package.

Recommended live order:

1. M31: inspect and arm out of order, confirm response arrives after preparation, then deliberately destroy the generator and retry once.
2. M32: verify beach disembarkation, cabinet access, both hand/stow actions and the unloading table. Confirm the two cases survive the entire drive.
3. M33: watch the approach before starting the clock; then rescue, board all four people and confirm Ramos's transfer animation completes.
4. M34: switch to Ice's turret and Gohan's escort while moving. Check Guess keeps driving, both barriers wait for the escort and Ramos reaches the medical kit.
5. M35: inspect the two charges, watch the convoy approach, keep the technical intact and confirm all three seats remain usable after switching. Store the captured truck at a garage if you want it saved.

Also watch versus skip each opening at least once, and report the mission/stage plus any changed Survey All key. The new locations are estimates supported by offline checks; live survey acceptance is still pending.
